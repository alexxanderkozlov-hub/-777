using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace генератор_мира_из_текстов
{
    public enum TileType
    {
        Wall,
        Floor,
        Hazard
    }

    public sealed class Tile
    {
        public Tile(TileType type, Color color)
        {
            Type = type;
            Color = color;
        }

        public TileType Type { get; private set; }
        public Color Color { get; private set; }
        public bool IsWalkable { get { return Type == TileType.Floor || Type == TileType.Hazard; } }

        public void Change(TileType type, Color color)
        {
            Type = type;
            Color = color;
        }
    }

    public sealed class PromptProfile
    {
        public string OriginalText { get; set; }
        public string ThemeName { get; set; }
        public string TileSetName { get; set; }
        public string LightingPreset { get; set; }
        public Color WallColor { get; set; }
        public Color FloorColor { get; set; }
        public Color HazardColor { get; set; }
        public Color LightColor { get; set; }
        public int EnemyCount { get; set; }
        public int LootCount { get; set; }
        public int DangerLevel { get; set; }
        public int Seed { get; set; }
        public List<string> EnemyNames { get; private set; }
        public List<string> LootNames { get; private set; }
        public List<string> AssetPipeline { get; private set; }

        public PromptProfile()
        {
            EnemyNames = new List<string>();
            LootNames = new List<string>();
            AssetPipeline = new List<string>();
        }
    }

    public sealed class Player
    {
        public Point Position { get; set; }
        public int Health { get; set; }
        public int Score { get; set; }
    }

    public abstract class WorldEntity
    {
        protected WorldEntity(string name, Point position)
        {
            Name = name;
            Position = position;
        }

        public string Name { get; private set; }
        public Point Position { get; set; }
    }

    public sealed class Enemy : WorldEntity
    {
        public Enemy(string name, Point position, int power)
            : base(name, position)
        {
            Power = power;
        }

        public int Power { get; private set; }
    }

    public sealed class LootItem : WorldEntity
    {
        public LootItem(string name, Point position, int value)
            : base(name, position)
        {
            Value = value;
        }

        public int Value { get; private set; }
    }

    public sealed class LightSource
    {
        public LightSource(Point position, int radius, Color color)
        {
            Position = position;
            Radius = radius;
            Color = color;
        }

        public Point Position { get; private set; }
        public int Radius { get; private set; }
        public Color Color { get; private set; }
    }

    public sealed class GameWorld
    {
        private readonly Random runtimeRandom;

        public GameWorld(int width, int height, PromptProfile profile)
        {
            Width = width;
            Height = height;
            Profile = profile;
            Tiles = new Tile[width, height];
            Enemies = new List<Enemy>();
            Loot = new List<LootItem>();
            Lights = new List<LightSource>();
            Player = new Player { Health = 100, Score = 0 };
            runtimeRandom = new Random(profile.Seed ^ 0x5F3759DF);
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public Tile[,] Tiles { get; private set; }
        public Player Player { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public List<LootItem> Loot { get; private set; }
        public List<LightSource> Lights { get; private set; }
        public PromptProfile Profile { get; private set; }
        public Point Exit { get; set; }
        public int Moves { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsGameOver { get { return Player.Health <= 0; } }
        public string LastEvent { get; internal set; }

        public bool TryMovePlayer(int dx, int dy)
        {
            if (IsCompleted || IsGameOver)
            {
                return false;
            }

            var next = new Point(Player.Position.X + dx, Player.Position.Y + dy);
            if (!IsWalkable(next))
            {
                LastEvent = "Путь закрыт: генератор поставил здесь стену.";
                return false;
            }

            Player.Position = next;
            Moves++;

            if (Tiles[next.X, next.Y].Type == TileType.Hazard)
            {
                Player.Health -= 6 + Profile.DangerLevel;
                LastEvent = "Опасная зона повредила скафандр.";
            }
            else
            {
                LastEvent = "Шаг по сгенерированному уровню.";
            }

            CollectLootAt(next);
            FightEnemyAt(next);

            if (next == Exit)
            {
                IsCompleted = true;
                Player.Score += 150;
                LastEvent = "Вы нашли выход и закрепили сгенерированный мир.";
                return true;
            }

            MoveEnemies();
            return true;
        }

        public bool InBounds(Point point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height;
        }

        public bool IsWalkable(Point point)
        {
            return InBounds(point) && Tiles[point.X, point.Y].IsWalkable;
        }

        public Enemy EnemyAt(Point point)
        {
            return Enemies.FirstOrDefault(enemy => enemy.Position == point);
        }

        public LootItem LootAt(Point point)
        {
            return Loot.FirstOrDefault(item => item.Position == point);
        }

        private void CollectLootAt(Point point)
        {
            LootItem item = LootAt(point);
            if (item == null)
            {
                return;
            }

            Loot.Remove(item);
            Player.Score += item.Value;
            LastEvent = "Лут найден: " + item.Name + " (+" + item.Value + ").";
        }

        private void FightEnemyAt(Point point)
        {
            Enemy enemy = EnemyAt(point);
            if (enemy == null)
            {
                return;
            }

            Player.Health -= enemy.Power;
            Player.Score += 25;
            Enemies.Remove(enemy);
            LastEvent = "Столкновение: " + enemy.Name + " нанес " + enemy.Power + " урона.";
        }

        private void MoveEnemies()
        {
            foreach (Enemy enemy in Enemies.ToArray())
            {
                Point next = GetEnemyNextPoint(enemy);
                if (!IsWalkable(next) || next == Exit || Enemies.Any(other => other != enemy && other.Position == next))
                {
                    continue;
                }

                enemy.Position = next;
                if (enemy.Position == Player.Position)
                {
                    Player.Health -= enemy.Power;
                    LastEvent = enemy.Name + " догнал игрока.";
                }
            }
        }

        private Point GetEnemyNextPoint(Enemy enemy)
        {
            int distance = Math.Abs(enemy.Position.X - Player.Position.X) + Math.Abs(enemy.Position.Y - Player.Position.Y);
            if (distance <= 7)
            {
                int dx = Math.Sign(Player.Position.X - enemy.Position.X);
                int dy = Math.Sign(Player.Position.Y - enemy.Position.Y);

                if (Math.Abs(Player.Position.X - enemy.Position.X) > Math.Abs(Player.Position.Y - enemy.Position.Y))
                {
                    return new Point(enemy.Position.X + dx, enemy.Position.Y);
                }

                return new Point(enemy.Position.X, enemy.Position.Y + dy);
            }

            Point[] options =
            {
                new Point(enemy.Position.X + 1, enemy.Position.Y),
                new Point(enemy.Position.X - 1, enemy.Position.Y),
                new Point(enemy.Position.X, enemy.Position.Y + 1),
                new Point(enemy.Position.X, enemy.Position.Y - 1)
            };

            return options[runtimeRandom.Next(options.Length)];
        }
    }
}
