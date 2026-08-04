using System;
using System.Collections.Generic;

namespace SubnauticaSpeedrunningMod.Runtime.Seeds
{
    internal static class ModBetterRngPresetCatalog
    {
        public const float SpawnMinX = -98f;
        public const float SpawnMaxX = -100f;
        public const float SpawnMinZ = 93f;
        public const float SpawnMaxZ = 95f;
        public static readonly Dictionary<string, float> BiomeDistributionOverrides =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                { "SafeShallows_ShellTunnelHuge", 999f },
                { "SafeShallows_ShellTunnel", 999f },
                { "SafeShallows_Wall", 999f },
                { "SafeShallows_CaveWall", 999f },
                { "InactiveLavaZone_Chamber_Floor", 999f },
                { "InactiveLavaZone_CastleTunnel_Wall", 999f },
                { "Kelp_CaveFloor", 3f },
                { "SafeShallows_SandFlat", 1.7f },
                { "Kelp_CaveWall", 5f },
                { "Kelp_Sand", 5f },
                { "Kelp_GrassSparse", 5f },
                { "Kelp_GrassDense", 5f },
                { "Kelp_VineBase", 5f }
            };

        public static readonly Dictionary<string, Dictionary<string, float>> EntityBiomeDistributionOverrides =
            new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Sulphur",
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "ActiveLavaZone_Chamber_Floor", 999f },
                        { "ActiveLavaZone_Chamber_Wall", 999f },
                        { "ActiveLavaZone_Chamber_Floor_Far", 999f },
                        { "ActiveLavaZone_Falls_Floor", 999f },
                        { "ActiveLavaZone_Falls_Wall", 999f }
                    }
                }
            };

        // Hardcore uses the normal BetterRNG preset plus these Grassy Plateaus wreck guarantees.
        public static readonly Dictionary<string, Dictionary<string, float>> HardcoreEntityBiomeDistributionOverrides =
            new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "ConstructorFragment",
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "GrassyPlateaus_TechSite", 999f },
                        { "GrassyPlateaus_TechSite_Barrier", 999f }
                    }
                },
                {
                    "ScrapMetal",
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "GrassyPlateaus_TechSite_Scattered", 999f }
                    }
                }
            };

        // Target the exact 2018 junk-fragment distribution entry. Its WorldEntityInfo
        // TechType is not reliable enough to use as the override key.
        public static readonly Dictionary<string, Dictionary<string, float>> HardcoreClassIdBiomeDistributionOverrides =
            new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "ac997abf-659f-4fbe-a318-64606a161d7e",
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "GrassyPlateaus_TechSite", 999f },
                        { "GrassyPlateaus_TechSite_Barrier", 999f }
                    }
                }
            };

        public static readonly Dictionary<string, ModBetterRngEntityOverrideDefinition> EntityDistributionOverrides =
            new Dictionary<string, ModBetterRngEntityOverrideDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                { "MoonpoolFragment", new ModBetterRngEntityOverrideDefinition("MoonpoolFragment", 0.3f) },
                { "CyclopsEngineFragment", new ModBetterRngEntityOverrideDefinition("CyclopsEngineFragment", 0.1f) },
                { "CyclopsBridgeFragment", new ModBetterRngEntityOverrideDefinition("CyclopsBridgeFragment", 0.13f) },
                { "CyclopsHullFragment", new ModBetterRngEntityOverrideDefinition("CyclopsHullFragment", 0.13f) },
                { "Stalker", new ModBetterRngEntityOverrideDefinition("Stalker", 1.3f, 2) },
                { "ScrapMetal", new ModBetterRngEntityOverrideDefinition("ScrapMetal", 0.15f) },
                { "Lithium", new ModBetterRngEntityOverrideDefinition("Lithium", 1f) },
                { "AluminumOxide", new ModBetterRngEntityOverrideDefinition("AluminumOxide", 1f) },
                { "JellyPlant", new ModBetterRngEntityOverrideDefinition("JellyPlant", 1f) },
                { "ShaleChunk", new ModBetterRngEntityOverrideDefinition("ShaleChunk", 0.5f) },
                { "Magnetite", new ModBetterRngEntityOverrideDefinition("Magnetite", 0f) },
                { "Bleeder", new ModBetterRngEntityOverrideDefinition("Bleeder", 0f) }
            };

        public static readonly string[] BlockedCreatureBiomeNames =
        {
            "SafeShallows_ShellTunnelHuge",
            "SafeShallows_ShellTunnel",
            "SafeShallows_Wall",
            "SafeShallows_CaveWall",
            "SafeShallows_SandFlat",
            "InactiveLavaZone_Chamber_Floor",
            "InactiveLavaZone_CastleTunnel_Wall",
            "Kelp_CaveFloor",
            "Kelp_GrassSparse",
            "Kelp_GrassDense",
            "Kelp_VineBase",
            "Kelp_CaveWall",
            "Kelp_Sand",
            "PrisonAquarium_Open_CreatureOnly"
        };

    }

    internal sealed class ModBetterRngEntityOverrideDefinition
    {
        public ModBetterRngEntityOverrideDefinition(string techTypeName, float chance)
            : this(techTypeName, chance, 0, false, null)
        {
        }

        public ModBetterRngEntityOverrideDefinition(string techTypeName, float chance, string[] biomeNames)
            : this(techTypeName, chance, 0, false, biomeNames)
        {
        }

        public ModBetterRngEntityOverrideDefinition(string techTypeName, float chance, int count)
            : this(techTypeName, chance, count, true, null)
        {
        }

        public ModBetterRngEntityOverrideDefinition(string techTypeName, float chance, int count, string[] biomeNames)
            : this(techTypeName, chance, count, true, biomeNames)
        {
        }

        private ModBetterRngEntityOverrideDefinition(string techTypeName, float chance, int count, bool hasCount, string[] biomeNames)
        {
            TechTypeName = techTypeName;
            Chance = chance;
            Count = count;
            HasCount = hasCount;
            BiomeNames = biomeNames ?? new string[0];
        }

        public string TechTypeName { get; private set; }

        public float Chance { get; private set; }

        public int Count { get; private set; }

        public bool HasCount { get; private set; }

        public string[] BiomeNames { get; private set; }
    }
}
