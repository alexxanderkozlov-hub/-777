using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace генератор_мира_из_текстов
{
    public sealed class WorldGenerator
    {
        private const int Width = 44;
        private const int Height = 29;

        public GameWorld Generate(PromptProfile profile)
        {
            var random = new Random(profile.Seed);
            var world = new GameWorld(Width, Height, profile);

            FillWithWalls(world);
            CarveMainPaths(world, random);
            CarveRooms(world, random);
            AddHazards(world, random);
            SealBorders(world);
            PlaceGameObjects(world, random);

            return world;
        }

        private static void FillWithWalls(GameWorld world)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    world.Tiles[x, y] = new Tile(TileType.Wall, world.Profile.WallColor);
                }
            }
        }

        private static void CarveMainPaths(GameWorld world, Random random)
        {
            Point walker = new Point(world.Width / 2, world.Height / 2);
            int steps = world.Width * world.Height * 4;

            for (int i = 0; i < steps; i++)
            {
                CarveFloor(world, walker.X, walker.Y);

                int direction = random.Next(4);
                if (world.Profile.ThemeName.Contains("станция") && random.NextDouble() < 0.58)
                {
                    direction = (i / 10) % 4;
                }

                if (direction == 0) walker.X++;
                if (direction == 1) walker.X--;
                if (direction == 2) walker.Y++;
                if (direction == 3) walker.Y--;

                walker.X = Clamp(walker.X, 2, world.Width - 3);
                walker.Y = Clamp(walker.Y, 2, world.Height - 3);

                if (random.NextDouble() < 0.45)
                {
                    CarveFloor(world, walker.X + 1, walker.Y);
                    CarveFloor(world, walker.X, walker.Y + 1);
                }
            }
        }

        private static void CarveRooms(GameWorld world, Random random)
        {
            int roomCount = 10 + world.Profile.LootCount / 2;
            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth = random.Next(4, 9);
                int roomHeight = random.Next(3, 7);
                int left = random.Next(2, world.Width - roomWidth - 2);
                int top = random.Next(2, world.Height - roomHeight - 2);

                for (int y = top; y < top + roomHeight; y++)
                {
                    for (int x = left; x < left + roomWidth; x++)
                    {
                        CarveFloor(world, x, y);
                    }
                }
            }
        }

        private static void AddHazards(GameWorld world, Random random)
        {
            int hazards = world.Profile.DangerLevel * 10;
            List<Point> floors = GetFloorPoints(world);

            for (int i = 0; i < hazards && floors.Count > 0; i++)
            {
                Point point = floors[random.Next(floors.Count)];
                if (Distance(point, new Point(world.Width / 2, world.Height / 2)) < 5)
                {
                    continue;
                }

                world.Tiles[point.X, point.Y].Change(TileType.Hazard, world.Profile.HazardColor);
            }
        }

        private static void SealBorders(GameWorld world)
        {
            for (int x = 0; x < world.Width; x++)
            {
                world.Tiles[x, 0].Change(TileType.Wall, world.Profile.WallColor);
                world.Tiles[x, world.Height - 1].Change(TileType.Wall, world.Profile.WallColor);
            }

            for (int y = 0; y < world.Height; y++)
            {
                world.Tiles[0, y].Change(TileType.Wall, world.Profile.WallColor);
                world.Tiles[world.Width - 1, y].Change(TileType.Wall, world.Profile.WallColor);
            }
        }

        private static void PlaceGameObjects(GameWorld world, Random random)
        {
            List<Point> floors = GetFloorPoints(world);
            Point start = FindNearestFloor(world, new Point(world.Width / 2, world.Height / 2));
            world.Player.Position = start;

            world.Exit = floors.OrderByDescending(point => Distance(point, start)).First();
            AddLights(world, random, floors);

            PlaceLoot(world, random, floors);
            PlaceEnemies(world, random, floors, start);
            world.LastEvent = "Мир создан из текста: " + world.Profile.OriginalText;
        }

        private static void AddLights(GameWorld world, Random random, List<Point> floors)
        {
            world.Lights.Add(new LightSource(world.Player.Position, 9, world.Profile.LightColor));
            world.Lights.Add(new LightSource(world.Exit, 8, Color.FromArgb(120, 235, 220)));

            int count = 5 + world.Profile.DangerLevel;
            for (int i = 0; i < count; i++)
            {
                Point point = floors[random.Next(floors.Count)];
                world.Lights.Add(new LightSource(point, random.Next(5, 9), world.Profile.LightColor));
            }
        }

        private static void PlaceLoot(GameWorld world, Random random, List<Point> floors)
        {
            for (int i = 0; i < world.Profile.LootCount; i++)
            {
                Point point = PickFreePoint(world, random, floors);
                string name = world.Profile.LootNames[i % world.Profile.LootNames.Count];
                int value = 15 + random.Next(10, 45);
                world.Loot.Add(new LootItem(name, point, value));
            }
        }

        private static void PlaceEnemies(GameWorld world, Random random, List<Point> floors, Point start)
        {
            for (int i = 0; i < world.Profile.EnemyCount; i++)
            {
                Point point = PickFreePoint(world, random, floors);
                if (Distance(point, start) < 7)
                {
                    i--;
                    continue;
                }

                string name = world.Profile.EnemyNames[i % world.Profile.EnemyNames.Count];
                int power = 7 + world.Profile.DangerLevel + random.Next(0, 7);
                world.Enemies.Add(new Enemy(name, point, power));
            }
        }

        private static Point PickFreePoint(GameWorld world, Random random, List<Point> floors)
        {
            for (int attempt = 0; attempt < 500; attempt++)
            {
                Point point = floors[random.Next(floors.Count)];
                if (point != world.Player.Position &&
                    point != world.Exit &&
                    world.EnemyAt(point) == null &&
                    world.LootAt(point) == null)
                {
                    return point;
                }
            }

            return floors[random.Next(floors.Count)];
        }

        private static Point FindNearestFloor(GameWorld world, Point target)
        {
            return GetFloorPoints(world)
                .OrderBy(point => Distance(point, target))
                .First();
        }

        private static List<Point> GetFloorPoints(GameWorld world)
        {
            var points = new List<Point>();
            for (int y = 1; y < world.Height - 1; y++)
            {
                for (int x = 1; x < world.Width - 1; x++)
                {
                    if (world.Tiles[x, y].Type == TileType.Floor)
                    {
                        points.Add(new Point(x, y));
                    }
                }
            }

            return points;
        }

        private static void CarveFloor(GameWorld world, int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= world.Width - 1 || y >= world.Height - 1)
            {
                return;
            }

            world.Tiles[x, y].Change(TileType.Floor, world.Profile.FloorColor);
        }

        private static int Distance(Point first, Point second)
        {
            return Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
