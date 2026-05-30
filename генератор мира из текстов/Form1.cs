using System;
using System.Drawing;
using System.Windows.Forms;

namespace генератор_мира_из_текстов
{
    public partial class Form1 : Form
    {
        private readonly TextWorldAnalyzer analyzer;
        private readonly WorldGenerator generator;
        private GameWorld world;

        private TextBox promptBox;
        private Button generateButton;
        private GameCanvas canvas;
        private Label statsLabel;
        private Label eventLabel;
        private Label themeLabel;
        private ListBox pipelineList;

        public Form1()
        {
            InitializeComponent();
            analyzer = new TextWorldAnalyzer();
            generator = new WorldGenerator();
            BuildInterface();
            GenerateWorld();
        }

        private void BuildInterface()
        {
            Text = "Генератор миров из текста";
            MinimumSize = new Size(1060, 680);
            Size = new Size(1180, 760);
            BackColor = Color.FromArgb(24, 26, 31);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BackColor,
                Padding = new Padding(14)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            Controls.Add(root);

            canvas = new GameCanvas
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 14, 0)
            };
            root.Controls.Add(canvas, 0, 0);

            var side = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 9,
                ColumnCount = 1,
                BackColor = Color.FromArgb(31, 34, 41),
                Padding = new Padding(16)
            };
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            root.Controls.Add(side, 1, 0);

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Текст -> уровень",
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 247, 251)
            };
            side.Controls.Add(title, 0, 0);

            promptBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(46, 50, 60),
                ForeColor = Color.White,
                Text = "заброшенная станция на Венере",
                Font = new Font("Segoe UI", 11f),
                Margin = new Padding(0, 8, 0, 8)
            };
            side.Controls.Add(promptBox, 0, 1);

            generateButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Сгенерировать",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 132, 214),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            generateButton.FlatAppearance.BorderSize = 0;
            generateButton.Click += delegate { GenerateWorld(); };
            side.Controls.Add(generateButton, 0, 2);

            themeLabel = CreateInfoLabel("Тема: -");
            side.Controls.Add(themeLabel, 0, 3);

            statsLabel = CreateInfoLabel("HP: -");
            side.Controls.Add(statsLabel, 0, 4);

            var pipelineTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Пайплайн ассетов",
                ForeColor = Color.FromArgb(208, 215, 224),
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            side.Controls.Add(pipelineTitle, 0, 5);

            pipelineList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 26, 31),
                ForeColor = Color.FromArgb(224, 229, 236),
                IntegralHeight = false,
                Font = new Font("Segoe UI", 9.5f)
            };
            side.Controls.Add(pipelineList, 0, 6);

            eventLabel = CreateInfoLabel("WASD или стрелки: движение. Цель: собрать лут и найти выход.");
            side.Controls.Add(eventLabel, 0, 7);
        }

        private Label CreateInfoLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = Color.FromArgb(224, 229, 236),
                BackColor = Color.FromArgb(38, 42, 50),
                Padding = new Padding(10),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private void GenerateWorld()
        {
            PromptProfile profile = analyzer.Analyze(promptBox == null ? string.Empty : promptBox.Text);
            world = generator.Generate(profile);
            canvas.World = world;
            pipelineList.Items.Clear();
            foreach (string item in profile.AssetPipeline)
            {
                pipelineList.Items.Add(item);
            }

            UpdateInfo();
            canvas.Invalidate();
            ActiveControl = canvas;
            canvas.Focus();
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (world == null || promptBox.Focused)
            {
                return;
            }

            int dx = 0;
            int dy = 0;

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) dx = -1;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) dx = 1;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) dy = -1;
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) dy = 1;

            if (dx == 0 && dy == 0)
            {
                return;
            }

            world.TryMovePlayer(dx, dy);
            UpdateInfo();
            canvas.Invalidate();
            e.Handled = true;
        }

        private void UpdateInfo()
        {
            if (world == null)
            {
                return;
            }

            themeLabel.Text = "Тема: " + world.Profile.ThemeName + Environment.NewLine +
                              "Свет: " + world.Profile.LightingPreset;

            statsLabel.Text = "HP: " + Math.Max(0, world.Player.Health) +
                              "    Очки: " + world.Player.Score + Environment.NewLine +
                              "Враги: " + world.Enemies.Count +
                              "    Лут: " + world.Loot.Count +
                              "    Ходы: " + world.Moves;

            eventLabel.Text = world.LastEvent;

            if (world.IsGameOver)
            {
                eventLabel.Text = "Игра окончена. Нажмите \"Сгенерировать\", чтобы создать новый мир.";
            }
        }
    }
}
