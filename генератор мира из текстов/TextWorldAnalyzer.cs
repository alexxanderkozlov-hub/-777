using System;
using System.Drawing;
using System.Linq;

namespace генератор_мира_из_текстов
{
    public sealed class TextWorldAnalyzer
    {
        public PromptProfile Analyze(string text)
        {
            string source = string.IsNullOrWhiteSpace(text)
                ? "заброшенная станция на Венере"
                : text.Trim();

            string lower = source.ToLowerInvariant();
            PromptProfile profile = CreateBaseProfile(source);

            if (HasAny(lower, "венер", "venus", "станц", "station", "космос", "space"))
            {
                ApplyVenusStation(profile);
            }
            else if (HasAny(lower, "лес", "forest", "джунг", "болот", "moss"))
            {
                ApplyAncientForest(profile);
            }
            else if (HasAny(lower, "лед", "ice", "снег", "арктик", "frost"))
            {
                ApplyIceCavern(profile);
            }
            else if (HasAny(lower, "город", "city", "кибер", "cyber", "неон", "метро"))
            {
                ApplyNeonCity(profile);
            }
            else
            {
                ApplyDefaultRuins(profile);
            }

            if (HasAny(lower, "заброш", "abandoned", "руин", "темн", "опас", "danger", "ад"))
            {
                profile.DangerLevel += 2;
                profile.EnemyCount += 2;
            }

            if (HasAny(lower, "богат", "клад", "treasure", "loot", "артефакт"))
            {
                profile.LootCount += 3;
            }

            profile.DangerLevel = Clamp(profile.DangerLevel, 1, 8);
            profile.EnemyCount = Clamp(profile.EnemyCount, 3, 14);
            profile.LootCount = Clamp(profile.LootCount, 4, 16);
            BuildAssetPipeline(profile);
            return profile;
        }

        private static PromptProfile CreateBaseProfile(string source)
        {
            return new PromptProfile
            {
                OriginalText = source,
                ThemeName = "неизвестный мир",
                TileSetName = "procedural/base",
                LightingPreset = "dynamic soft light",
                WallColor = Color.FromArgb(50, 52, 58),
                FloorColor = Color.FromArgb(84, 88, 98),
                HazardColor = Color.FromArgb(178, 77, 55),
                LightColor = Color.FromArgb(255, 220, 150),
                EnemyCount = 6,
                LootCount = 7,
                DangerLevel = 3,
                Seed = MakeStableSeed(source)
            };
        }

        private static void ApplyVenusStation(PromptProfile profile)
        {
            profile.ThemeName = "заброшенная станция на Венере";
            profile.TileSetName = "asset-pipeline/venus-station";
            profile.LightingPreset = "кислотный туман + аварийные лампы";
            profile.WallColor = Color.FromArgb(68, 62, 70);
            profile.FloorColor = Color.FromArgb(113, 92, 74);
            profile.HazardColor = Color.FromArgb(226, 125, 54);
            profile.LightColor = Color.FromArgb(255, 178, 86);
            profile.EnemyNames.AddRange(new[] { "ремонтный дрон", "кислотный мутант", "охранная турель" });
            profile.LootNames.AddRange(new[] { "энергокристалл", "кислородный модуль", "ключ-карта" });
            profile.EnemyCount = 8;
            profile.LootCount = 8;
            profile.DangerLevel = 5;
        }

        private static void ApplyAncientForest(PromptProfile profile)
        {
            profile.ThemeName = "заросшие руины";
            profile.TileSetName = "asset-pipeline/overgrown-ruins";
            profile.LightingPreset = "лучи сквозь кроны";
            profile.WallColor = Color.FromArgb(43, 71, 54);
            profile.FloorColor = Color.FromArgb(74, 103, 68);
            profile.HazardColor = Color.FromArgb(92, 135, 47);
            profile.LightColor = Color.FromArgb(178, 226, 132);
            profile.EnemyNames.AddRange(new[] { "страж-корень", "дикая химера", "лоза-ловушка" });
            profile.LootNames.AddRange(new[] { "древняя монета", "семя энергии", "осколок идола" });
            profile.EnemyCount = 7;
            profile.LootCount = 10;
            profile.DangerLevel = 4;
        }

        private static void ApplyIceCavern(PromptProfile profile)
        {
            profile.ThemeName = "ледяная пещера";
            profile.TileSetName = "asset-pipeline/ice-cavern";
            profile.LightingPreset = "холодное отраженное свечение";
            profile.WallColor = Color.FromArgb(43, 67, 85);
            profile.FloorColor = Color.FromArgb(109, 144, 158);
            profile.HazardColor = Color.FromArgb(166, 223, 231);
            profile.LightColor = Color.FromArgb(172, 226, 255);
            profile.EnemyNames.AddRange(new[] { "ледяной голем", "пещерный сканер", "осколочный дух" });
            profile.LootNames.AddRange(new[] { "тепловая батарея", "кристалл льда", "навигатор" });
            profile.EnemyCount = 6;
            profile.LootCount = 7;
            profile.DangerLevel = 4;
        }

        private static void ApplyNeonCity(PromptProfile profile)
        {
            profile.ThemeName = "неоновый город";
            profile.TileSetName = "asset-pipeline/neon-city";
            profile.LightingPreset = "контрастные рекламные панели";
            profile.WallColor = Color.FromArgb(37, 43, 62);
            profile.FloorColor = Color.FromArgb(61, 72, 92);
            profile.HazardColor = Color.FromArgb(210, 57, 119);
            profile.LightColor = Color.FromArgb(85, 211, 255);
            profile.EnemyNames.AddRange(new[] { "боевой андроид", "дрон-патруль", "сбойный аватар" });
            profile.LootNames.AddRange(new[] { "чип доступа", "пакет данных", "энергоячейка" });
            profile.EnemyCount = 9;
            profile.LootCount = 9;
            profile.DangerLevel = 5;
        }

        private static void ApplyDefaultRuins(PromptProfile profile)
        {
            profile.ThemeName = "процедурные руины";
            profile.TileSetName = "asset-pipeline/mixed-ruins";
            profile.LightingPreset = "локальные факелы";
            profile.EnemyNames.AddRange(new[] { "бродячий страж", "аномалия", "малый голем" });
            profile.LootNames.AddRange(new[] { "артефакт", "ремкомплект", "карта сектора" });
        }

        private static void BuildAssetPipeline(PromptProfile profile)
        {
            profile.AssetPipeline.Clear();
            profile.AssetPipeline.Add("AI-анализ: выделена тема \"" + profile.ThemeName + "\"");
            profile.AssetPipeline.Add("Tileset: " + profile.TileSetName);
            profile.AssetPipeline.Add("Lighting: " + profile.LightingPreset);
            profile.AssetPipeline.Add("Spawn: враги " + profile.EnemyCount + ", лут " + profile.LootCount);
            profile.AssetPipeline.Add("Seed: " + profile.Seed);
        }

        private static bool HasAny(string source, params string[] needles)
        {
            return needles.Any(source.Contains);
        }

        private static int MakeStableSeed(string source)
        {
            unchecked
            {
                int hash = 23;
                foreach (char symbol in source)
                {
                    hash = hash * 31 + symbol;
                }

                return hash == int.MinValue ? 42 : Math.Abs(hash);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
