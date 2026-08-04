using System;
using System.Collections.Generic;
using System.IO;

namespace SubnauticaSpeedrunningMod.Runtime.Practice
{
    internal static class ModPracticeSaveCatalog
    {
        public const string SaveFilesFolderName = "SaveFiles";
        public const string AnyPercentSurvivalGlitchedCategoryId = "Any% Survival Glitched";

        private const string GameInfoFileName = "gameinfo.json";
        private const string HotbarLayoutPreviewFileName = "HotbarLayout.png";
        private const string HealthPreviewFileName = "Health.png";

        private static readonly System.Random RandomSource = new System.Random();
        private static readonly object RandomSyncRoot = new object();

        private static readonly ModPracticeSaveDefinition[] Definitions =
        {
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "ASGEndSection",
                "End Section Practice",
                timerEnabled: true,
                startsWithSuperSeaglide: false,
                rootVariantKind: ModPracticeRootVariantKind.None,
                rootVariantDirectories: null),
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "QEPQuickdeath",
                "QEP Quickdeath",
                timerEnabled: false,
                startsWithSuperSeaglide: false,
                rootVariantKind: ModPracticeRootVariantKind.None,
                rootVariantDirectories: null),
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "CureClip",
                "Cure Clip",
                timerEnabled: false,
                startsWithSuperSeaglide: false,
                rootVariantKind: ModPracticeRootVariantKind.None,
                rootVariantDirectories: null),
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "AlienThermalPlantDeath",
                "ATP Death",
                timerEnabled: false,
                startsWithSuperSeaglide: false,
                rootVariantKind: ModPracticeRootVariantKind.Health,
                rootVariantDirectories: new[]
                {
                    "ATPD(Health 1)",
                    "ATPD(Health 2)"
                }),
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "MountainClip",
                "Mountain Clip",
                timerEnabled: false,
                startsWithSuperSeaglide: true,
                rootVariantKind: ModPracticeRootVariantKind.TimeOfDay,
                rootVariantDirectories: new[]
                {
                    "MountainClip(DayTime)",
                    "MountainClip(NightTime)"
                }),
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "AuroraClip",
                "Aurora Clip",
                timerEnabled: false,
                startsWithSuperSeaglide: true,
                rootVariantKind: ModPracticeRootVariantKind.TimeOfDay,
                rootVariantDirectories: new[]
                {
                    "AuroraClip(DayTime)",
                    "AuroraClip(NightTime)"
                }),
            new ModPracticeSaveDefinition(
                AnyPercentSurvivalGlitchedCategoryId,
                "PreAuroraCrafting",
                "Pre-Aurora Crafting",
                timerEnabled: false,
                startsWithSuperSeaglide: false,
                rootVariantKind: ModPracticeRootVariantKind.RandomChildDirectory,
                rootVariantDirectories: null)
        };

        public static IList<ModPracticeSaveDefinition> GetPrimaryCategoryDefinitions()
        {
            List<ModPracticeSaveDefinition> results = new List<ModPracticeSaveDefinition>();
            for (int i = 0; i < Definitions.Length; i++)
            {
                ModPracticeSaveDefinition definition = Definitions[i];
                if (Directory.Exists(GetInstalledSavePath(definition)))
                {
                    results.Add(definition);
                }
            }

            return results;
        }

        public static string GetPrimaryCategoryDisplayName()
        {
            return AnyPercentSurvivalGlitchedCategoryId;
        }

        public static bool TryGetDefinition(string categoryId, string saveId, out ModPracticeSaveDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].CategoryId, categoryId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Definitions[i].SaveId, saveId, StringComparison.OrdinalIgnoreCase))
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default(ModPracticeSaveDefinition);
            return false;
        }

        public static string GetInstalledSavePath(ModPracticeSaveDefinition definition)
        {
            string modRoot = PathLayout.GetModRoot();
            return Path.Combine(Path.Combine(modRoot, SaveFilesFolderName), definition.SaveId);
        }

        public static ModPracticeSaveTemplateLayout GetTemplateLayout(ModPracticeSaveDefinition definition, int layoutIndex)
        {
            string saveRootPath = GetInstalledSavePath(definition);
            string templateRootPath = ResolveTemplateRootPath(definition, saveRootPath);
            bool hasAnyVariantDirectories = false;
            string selectedVariantDirectoryPath = string.Empty;

            if (Directory.Exists(templateRootPath))
            {
                for (int i = 0; i < ModPracticeHotbarOptions.LayoutCountValue; i++)
                {
                    string candidatePath = Path.Combine(
                        templateRootPath,
                        ModPracticeHotbarOptions.GetVariantDirectoryName(i));
                    if (!Directory.Exists(candidatePath))
                    {
                        continue;
                    }

                    hasAnyVariantDirectories = true;
                    if (i == layoutIndex)
                    {
                        selectedVariantDirectoryPath = candidatePath;
                    }
                }
            }

            return new ModPracticeSaveTemplateLayout(
                templateRootPath,
                selectedVariantDirectoryPath,
                selectedVariantRequired: hasAnyVariantDirectories);
        }

        public static bool TryGetHotbarLayoutPreviewPath(int layoutIndex, out string previewPath)
        {
            return TryFindVariantPreviewPath(
                ModPracticeHotbarOptions.GetVariantDirectoryName(layoutIndex),
                HotbarLayoutPreviewFileName,
                out previewPath);
        }

        public static bool TryGetHealthPreviewPath(int healthIndex, out string previewPath)
        {
            previewPath = string.Empty;

            for (int i = 0; i < Definitions.Length; i++)
            {
                ModPracticeSaveDefinition definition = Definitions[i];
                if (definition.RootVariantKind != ModPracticeRootVariantKind.Health)
                {
                    continue;
                }

                string saveRootPath = GetInstalledSavePath(definition);
                string variantDirectoryName = GetConfiguredRootVariantDirectoryName(definition, healthIndex);
                if (string.IsNullOrEmpty(saveRootPath) || string.IsNullOrEmpty(variantDirectoryName))
                {
                    continue;
                }

                string candidatePath = Path.Combine(Path.Combine(saveRootPath, variantDirectoryName), HealthPreviewFileName);
                if (File.Exists(candidatePath))
                {
                    previewPath = candidatePath;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindVariantPreviewPath(string variantDirectoryName, string previewFileName, out string previewPath)
        {
            previewPath = string.Empty;
            if (string.IsNullOrEmpty(variantDirectoryName) || string.IsNullOrEmpty(previewFileName))
            {
                return false;
            }

            for (int i = 0; i < Definitions.Length; i++)
            {
                string saveRootPath = GetInstalledSavePath(Definitions[i]);
                if (!Directory.Exists(saveRootPath))
                {
                    continue;
                }

                string[] directories = Directory.GetDirectories(saveRootPath, variantDirectoryName, SearchOption.AllDirectories);
                for (int j = 0; j < directories.Length; j++)
                {
                    string candidatePath = Path.Combine(directories[j], previewFileName);
                    if (File.Exists(candidatePath))
                    {
                        previewPath = candidatePath;
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ResolveTemplateRootPath(ModPracticeSaveDefinition definition, string saveRootPath)
        {
            if (string.IsNullOrEmpty(saveRootPath))
            {
                return string.Empty;
            }

            switch (definition.RootVariantKind)
            {
                case ModPracticeRootVariantKind.Health:
                    return Path.Combine(saveRootPath, GetConfiguredRootVariantDirectoryName(definition, ModPracticeHealthOptions.GetSelectedHealthIndex()));

                case ModPracticeRootVariantKind.TimeOfDay:
                    return Path.Combine(saveRootPath, GetConfiguredRootVariantDirectoryName(definition, ModPracticeTimeOfDayOptions.GetSelectedIndex()));

                case ModPracticeRootVariantKind.RandomChildDirectory:
                    return ChooseRandomTemplateRoot(saveRootPath);

                default:
                    return saveRootPath;
            }
        }

        private static string GetConfiguredRootVariantDirectoryName(ModPracticeSaveDefinition definition, int selectedIndex)
        {
            string[] variantDirectories = definition.RootVariantDirectories;
            if (variantDirectories == null || variantDirectories.Length <= 0)
            {
                return string.Empty;
            }

            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }
            else if (selectedIndex >= variantDirectories.Length)
            {
                selectedIndex = variantDirectories.Length - 1;
            }

            return variantDirectories[selectedIndex] ?? string.Empty;
        }

        private static string ChooseRandomTemplateRoot(string saveRootPath)
        {
            string[] candidateDirectories = GetRandomTemplateCandidates(saveRootPath);
            if (candidateDirectories.Length <= 0)
            {
                return saveRootPath;
            }

            lock (RandomSyncRoot)
            {
                return candidateDirectories[RandomSource.Next(candidateDirectories.Length)];
            }
        }

        private static string[] GetRandomTemplateCandidates(string saveRootPath)
        {
            if (string.IsNullOrEmpty(saveRootPath) || !Directory.Exists(saveRootPath))
            {
                return new string[0];
            }

            string[] directories = Directory.GetDirectories(saveRootPath, "*", SearchOption.TopDirectoryOnly);
            List<string> candidates = new List<string>();
            for (int i = 0; i < directories.Length; i++)
            {
                string candidateGameInfoPath = Path.Combine(directories[i], GameInfoFileName);
                if (File.Exists(candidateGameInfoPath))
                {
                    candidates.Add(directories[i]);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates.ToArray();
            }

            return directories;
        }
    }

    internal enum ModPracticeRootVariantKind
    {
        None,
        Health,
        TimeOfDay,
        RandomChildDirectory
    }

    internal struct ModPracticeSaveDefinition
    {
        public ModPracticeSaveDefinition(
            string categoryId,
            string saveId,
            string displayName,
            bool timerEnabled,
            bool startsWithSuperSeaglide,
            ModPracticeRootVariantKind rootVariantKind,
            string[] rootVariantDirectories)
        {
            CategoryId = categoryId ?? string.Empty;
            SaveId = saveId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            TimerEnabled = timerEnabled;
            StartsWithSuperSeaglide = startsWithSuperSeaglide;
            RootVariantKind = rootVariantKind;
            RootVariantDirectories = rootVariantDirectories;
        }

        public string CategoryId { get; private set; }

        public string SaveId { get; private set; }

        public string DisplayName { get; private set; }

        public bool TimerEnabled { get; private set; }

        public bool StartsWithSuperSeaglide { get; private set; }

        public ModPracticeRootVariantKind RootVariantKind { get; private set; }

        public string[] RootVariantDirectories { get; private set; }
    }
}
