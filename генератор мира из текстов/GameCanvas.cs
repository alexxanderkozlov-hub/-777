using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace генератор_мира_из_текстов
{
    public sealed class GameCanvas : Control
    {
        private const int PaddingSize = 12;

        public GameCanvas()
        {
            DoubleBuffered = true;
            TabStop = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            BackColor = Color.FromArgb(18, 20, 24);
        }

        public GameWorld World { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (World == null)
            {
                DrawEmptyState(e.Graphics);
                return;
            }

            Rectangle mapRect = GetMapRectangle();
            float cell = Math.Min(mapRect.Width / (float)World.Width, mapRect.Height / (float)World.Height);
            float offsetX = mapRect.Left + (mapRect.Width - cell * World.Width) / 2f;
            float offsetY = mapRect.Top + (mapRect.Height - cell * World.Height) / 2f;

            DrawTiles(e.Graphics, cell, offsetX, offsetY);
            DrawObjects(e.Graphics, cell, offsetX, offsetY);
            DrawFrame(e.Graphics, cell, offsetX, offsetY);
        }

        private void DrawEmptyState(Graphics graphics)
        {
            using (var brush = new SolidBrush(Color.FromArgb(170, 178, 188)))
            using (var font = new Font("Segoe UI", 13f, FontStyle.Regular))
            {
                string text = "Введите описание мира и нажмите \"Сгенерировать\"";
                SizeF size = graphics.MeasureString(text, font);
                graphics.DrawString(text, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
            }
        }

        private void DrawTiles(Graphics graphics, float cell, float offsetX, float offsetY)
        {
            for (int y = 0; y < World.Height; y++)
            {
                for (int x = 0; x < World.Width; x++)
                {
                    Tile tile = World.Tiles[x, y];
                    Color lit = ApplyLighting(tile.Color, x, y);
                    using (var brush = new SolidBrush(lit))
                    {
                        graphics.FillRectangle(brush, offsetX + x * cell, offsetY + y * cell, cell + 1, cell + 1);
                    }

                    if (tile.Type == TileType.Hazard)
                    {
                        using (var pen = new Pen(Color.FromArgb(150, Color.White), Math.Max(1f, cell * 0.08f)))
                        {
                            graphics.DrawLine(pen, offsetX + x * cell + cell * 0.25f, offsetY + y * cell + cell * 0.25f,
                                offsetX + x * cell + cell * 0.75f, offsetY + y * cell + cell * 0.75f);
                        }
                    }
                }
            }
        }

        private void DrawObjects(Graphics graphics, float cell, float offsetX, float offsetY)
        {
            DrawExit(graphics, cell, offsetX, offsetY);

            foreach (LootItem item in World.Loot)
            {
                RectangleF rect = CellRect(item.Position, cell, offsetX, offsetY, 0.28f);
                using (var brush = new SolidBrush(Color.FromArgb(255, 218, 91)))
                {
                    graphics.FillEllipse(brush, rect);
                }
            }

            foreach (Enemy enemy in World.Enemies)
            {
                DrawEnemy(graphics, enemy, cell, offsetX, offsetY);
            }

            RectangleF player = CellRect(World.Player.Position, cell, offsetX, offsetY, 0.12f);
            using (var brush = new SolidBrush(Color.FromArgb(88, 181, 255)))
            using (var pen = new Pen(Color.White, Math.Max(1f, cell * 0.08f)))
            {
                graphics.FillEllipse(brush, player);
                graphics.DrawEllipse(pen, player);
            }
        }

        private void DrawEnemy(Graphics graphics, Enemy enemy, float cell, float offsetX, float offsetY)
        {
            float x = offsetX + enemy.Position.X * cell;
            float y = offsetY + enemy.Position.Y * cell;
            PointF[] triangle =
            {
                new PointF(x + cell * 0.5f, y + cell * 0.16f),
                new PointF(x + cell * 0.82f, y + cell * 0.78f),
                new PointF(x + cell * 0.18f, y + cell * 0.78f)
            };

            using (var brush = new SolidBrush(Color.FromArgb(229, 80, 76)))
            using (var pen = new Pen(Color.FromArgb(255, 205, 205), Math.Max(1f, cell * 0.06f)))
            {
                graphics.FillPolygon(brush, triangle);
                graphics.DrawPolygon(pen, triangle);
            }
        }

        private void DrawExit(Graphics graphics, float cell, float offsetX, float offsetY)
        {
            RectangleF rect = CellRect(World.Exit, cell, offsetX, offsetY, 0.2f);
            using (var brush = new SolidBrush(Color.FromArgb(93, 235, 211)))
            using (var pen = new Pen(Color.FromArgb(210, 255, 248), Math.Max(1f, cell * 0.08f)))
            {
                graphics.FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
                graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private void DrawFrame(Graphics graphics, float cell, float offsetX, float offsetY)
        {
            using (var pen = new Pen(Color.FromArgb(74, 80, 92), 2f))
            {
                graphics.DrawRectangle(pen, offsetX, offsetY, World.Width * cell, World.Height * cell);
            }
        }

        private Color ApplyLighting(Color baseColor, int x, int y)
        {
            double light = 0.28;
            foreach (LightSource source in World.Lights)
            {
                double dx = source.Position.X - x;
                double dy = source.Position.Y - y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance <= source.Radius)
                {
                    light += (1.0 - distance / source.Radius) * 0.55;
                }
            }

            light = Math.Min(1.25, light);
            return Color.FromArgb(
                Clamp((int)(baseColor.R * light), 0, 255),
                Clamp((int)(baseColor.G * light), 0, 255),
                Clamp((int)(baseColor.B * light), 0, 255));
        }

        private Rectangle GetMapRectangle()
        {
            return new Rectangle(PaddingSize, PaddingSize, Math.Max(1, Width - PaddingSize * 2), Math.Max(1, Height - PaddingSize * 2));
        }

        private static RectangleF CellRect(Point point, float cell, float offsetX, float offsetY, float inset)
        {
            return new RectangleF(
                offsetX + point.X * cell + cell * inset,
                offsetY + point.Y * cell + cell * inset,
                cell * (1f - inset * 2f),
                cell * (1f - inset * 2f));
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
