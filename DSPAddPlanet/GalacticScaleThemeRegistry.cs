using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DSPAddPlanet
{
    static class GalacticScaleThemeRegistry
    {
        private const int MEDITERRANEAN = 1;
        private const int GAS_GIANT = 2;
        private const int ICE_GIANT = 4;
        private const int LAVA = 9;
        private const int ICE_GELISOL = 10;
        private const int BARREN = 11;
        private const int GOBI = 12;
        private const int VOLCANIC_ASH = 13;
        private const int RED_STONE = 14;
        private const int SAVANNA = 22;
        private const int OCEAN_WORLD = 16;
        private const int OCEANIC_JUNGLE = 8;
        private const int PANDORA_SWAMP = 25;

        private static bool registered;
        private static readonly Dictionary<string, int> themeNameToId = new Dictionary<string, int>();
        private static readonly Dictionary<int, ThemeSpec> themeSpecsById = new Dictionary<int, ThemeSpec>();
        private static readonly Dictionary<string, ThemeSpec> themeSpecsByName = new Dictionary<string, ThemeSpec>();
        private static readonly List<RegisteredThemeInfo> registeredThemeInfos = new List<RegisteredThemeInfo>();
        private static readonly HashSet<int> materialReapplyLogged = new HashSet<int>();
        private static readonly HashSet<int> materialDiagnosticLogged = new HashSet<int>();

        public static IReadOnlyDictionary<string, int> ThemeNameToId => themeNameToId;

        public static bool IsGiganticForestTheme(int themeId)
        {
            EnsureRegistered();
            ThemeProto theme = LDB.themes.Select(themeId);
            if (theme == null)
            {
                return false;
            }
            string normalized = NormalizeName(theme.Name);
            return normalized == "giganticforest" || normalized == "giganticforestcold";
        }

        public static bool IsBeachTheme(int themeId)
        {
            EnsureRegistered();
            ThemeProto theme = LDB.themes.Select(themeId);
            if (theme == null)
            {
                return false;
            }
            string normalized = NormalizeName(theme.Name);
            return normalized == "beach" || normalized == "beachcold";
        }

        public static bool TryGenerateGalacticScaleVeins(PlanetData planet)
        {
            EnsureRegistered();
            if (planet == null || planet.data == null)
            {
                return false;
            }

            ThemeSpec spec = GetSpecForThemeId(planet.theme);
            if (spec == null || spec.VeinSettings == null)
            {
                return false;
            }

            if (!string.Equals(spec.VeinSettings.Algorithm, "GS2", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            GenerateGs2Veins(planet, spec.VeinSettings);
            return true;
        }

        public static bool UsesGalacticScaleTerrain(int themeId)
        {
            EnsureRegistered();
            ThemeSpec spec = GetSpecForThemeId(themeId);
            return spec != null &&
                   spec.TerrainSettings != null &&
                   !string.IsNullOrWhiteSpace(spec.TerrainSettings.Algorithm) &&
                   !string.Equals(spec.TerrainSettings.Algorithm, "Vanilla", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGenerateGalacticScaleTerrain(PlanetData planet, double modX, double modY)
        {
            EnsureRegistered();
            if (planet == null || planet.data == null)
            {
                return false;
            }
            ThemeSpec spec = GetSpecForThemeId(planet.theme);
            if (spec == null || spec.TerrainSettings == null)
            {
                return false;
            }

            string algorithm = spec.TerrainSettings.Algorithm ?? "";
            if (string.Equals(algorithm, "GSTA1", StringComparison.OrdinalIgnoreCase))
            {
                GenerateTerrain1(planet, spec.TerrainSettings);
                return true;
            }
            if (string.Equals(algorithm, "GSTA3", StringComparison.OrdinalIgnoreCase))
            {
                GenerateTerrain3(planet, spec.TerrainSettings);
                return true;
            }
            if (string.Equals(algorithm, "GSTA6", StringComparison.OrdinalIgnoreCase))
            {
                GenerateTerrain6(planet, spec.TerrainSettings);
                return true;
            }

            return false;
        }

        static ThemeSpec GetSpecForThemeId(int themeId)
        {
            if (themeSpecsById.TryGetValue(themeId, out ThemeSpec spec))
            {
                return spec;
            }

            ThemeProto theme = LDB.themes.Select(themeId);
            if (theme == null)
            {
                return null;
            }
            if (themeSpecsByName.TryGetValue(NormalizeName(theme.Name), out spec) ||
                themeSpecsByName.TryGetValue(NormalizeName(theme.name), out spec) ||
                themeSpecsByName.TryGetValue(NormalizeName(theme.DisplayName), out spec) ||
                themeSpecsByName.TryGetValue(NormalizeName(theme.displayName), out spec))
            {
                return spec;
            }
            return null;
        }

        static void GenerateTerrain1(PlanetData planet, ThemeTerrainSettings terrainSettings)
        {
            PlanetRawData data = planet.data;
            if (data == null || data.vertices == null || data.heightData == null || data.biomoData == null)
            {
                return;
            }

            DotNet35Random random = new DotNet35Random(planet.seed);
            SimplexNoise primaryNoise = new SimplexNoise(random.Next());
            SimplexNoise secondaryNoise = new SimplexNoise(random.Next());

            for (int vertexIndex = 0; vertexIndex < data.dataLength; vertexIndex++)
            {
                double worldX = data.vertices[vertexIndex].x * (double)planet.radius;
                double worldY = data.vertices[vertexIndex].y * (double)planet.radius;
                double worldZ = data.vertices[vertexIndex].z * (double)planet.radius;

                double primaryTerrainNoise = primaryNoise.Noise3DFBM(
                    worldX * (terrainSettings.xFactor + 0.01),
                    worldY * (terrainSettings.yFactor + 0.012),
                    worldZ * (terrainSettings.zFactor + 0.01),
                    6
                ) * 3.0 * terrainSettings.HeightMulti + (-0.2 + terrainSettings.BaseHeight);

                double secondaryTerrainNoise = secondaryNoise.Noise3DFBM(
                    worldX * (1.0 / 400.0),
                    worldY * (1.0 / 400.0),
                    worldZ * (1.0 / 400.0),
                    3
                ) * 3.0 * terrainSettings.HeightMulti * (terrainSettings.RandomFactor + 0.9) + (terrainSettings.LandModifier + 0.5);

                secondaryTerrainNoise = secondaryTerrainNoise <= 0.0 ? secondaryTerrainNoise : secondaryTerrainNoise * 0.5;
                double combinedTerrainNoise = primaryTerrainNoise + secondaryTerrainNoise;
                double scaledTerrainValue = combinedTerrainNoise <= 0.0 ? combinedTerrainNoise * 1.6 : combinedTerrainNoise * 0.5;
                double leveledTerrainHeight = scaledTerrainValue <= 0.0
                    ? Maths.Levelize2(scaledTerrainValue, 0.5)
                    : Maths.Levelize3(scaledTerrainValue, 0.7);

                double biomeDetailNoise = secondaryNoise.Noise3DFBM(
                    worldX * (terrainSettings.xFactor + 0.01) * 2.5,
                    worldY * (terrainSettings.yFactor + 0.012) * 8.0,
                    worldZ * (terrainSettings.zFactor + 0.01) * 2.5,
                    2
                ) * 0.6 - 0.3;

                double rawBiomeHeight = scaledTerrainValue * terrainSettings.BiomeHeightMulti +
                                        biomeDetailNoise +
                                        terrainSettings.BiomeHeightModifier * 2.5 + 0.3;
                double scaledBiomeHeight = rawBiomeHeight >= 1.0
                    ? (rawBiomeHeight - 1.0) * 0.8 + 1.0
                    : rawBiomeHeight;

                int heightDataValue = (int)((planet.radius + leveledTerrainHeight + 0.2) * 100.0);
                data.heightData[vertexIndex] = (ushort)Mathf.Clamp(heightDataValue, ushort.MinValue, ushort.MaxValue);
                data.biomoData[vertexIndex] = (byte)Mathf.Clamp((float)(scaledBiomeHeight * 100.0), 0.0f, 200f);
            }
        }

        static void GenerateTerrain3(PlanetData planet, ThemeTerrainSettings terrainSettings)
        {
            PlanetRawData data = planet.data;
            if (data == null || data.vertices == null || data.heightData == null || data.biomoData == null)
            {
                return;
            }

            DotNet35Random random = new DotNet35Random(planet.seed);
            SimplexNoise simplexNoise1 = new SimplexNoise(random.Next());
            SimplexNoise simplexNoise2 = new SimplexNoise(random.Next());

            for (int index = 0; index < data.dataLength; index++)
            {
                double num4 = data.vertices[index].x * (double)planet.radius;
                double num5 = data.vertices[index].y * (double)planet.radius;
                double num6 = data.vertices[index].z * (double)planet.radius;
                double num7 = num4 + Math.Sin(num5 * 0.15) * 3.0;
                double num8 = num5 + Math.Sin(num6 * 0.15) * 3.0;
                double num9 = num6 + Math.Sin(num7 * 0.15) * 3.0;
                double num10 = simplexNoise1.Noise3DFBM(num7 * 0.007, num8 * 0.0077, num9 * 0.007, 6, deltaWLen: 1.8);
                double num11 = simplexNoise2.Noise3DFBM(num7 * 0.0091 + 0.5, num8 * 0.0196 + 0.2, num9 * 0.0091 + 0.7, 3) * 2.0;
                double num12 = simplexNoise2.Noise3DFBM(num7 * 0.042, num8 * 0.084, num9 * 0.042, 2) * 2.0;
                double num13 = simplexNoise2.Noise3DFBM(num7 * 0.0056, num8 * 0.0056, num9 * 0.0056, 2) * 2.0;
                double f = num10 * 2.0 + 0.92 + Mathf.Clamp01((float)(num11 * Mathf.Abs((float)num13 + 0.5f) - 0.35) * 1f);
                if (f < 0.0)
                {
                    f *= 2.0;
                }

                double num14 = Maths.Levelize2(f);
                if (num14 > 0.0)
                {
                    num14 = Maths.Levelize4(Maths.Levelize2(f));
                }

                double num15 = num14 <= 0.0
                    ? Mathf.Lerp(-4f, 0.0f, (float)num14 + 1f)
                    : num14 <= 1.0
                        ? Mathf.Lerp(0.0f, 0.3f, (float)num14) + num12 * 0.1
                        : num14 <= 2.0
                            ? Mathf.Lerp(0.3f, 1.4f, (float)num14 - 1f) + num12 * 0.12
                            : Mathf.Lerp(1.4f, 2.7f, (float)num14 - 2f) + num12 * 0.12;
                if (f < 0.0)
                {
                    f *= 2.0;
                }
                if (f < 1.0)
                {
                    f = Maths.Levelize(f);
                }

                double num17 = Mathf.Abs((float)f);
                double num18 = num17 <= 0.0 ? 0.0 : num17 <= 2.0 ? num17 : 2.0;
                double num19 = num18 + (num18 <= 1.8 ? num12 * 0.2 : -num12 * 0.8);
                int heightDataValue = (int)((planet.radius + num15 * terrainSettings.HeightMulti + 0.2 + terrainSettings.BaseHeight) * 100.0);
                data.heightData[index] = (ushort)Mathf.Clamp(heightDataValue, ushort.MinValue, ushort.MaxValue);
                data.biomoData[index] = (byte)Mathf.Clamp((float)(num19 * 100.0 * terrainSettings.BiomeHeightMulti + terrainSettings.BiomeHeightModifier), 0.0f, 200f);
            }
        }

        static void GenerateTerrain6(PlanetData planet, ThemeTerrainSettings terrainSettings)
        {
            PlanetRawData data = planet.data;
            if (data == null || data.vertices == null || data.heightData == null || data.biomoData == null)
            {
                return;
            }

            DotNet35Random random = new DotNet35Random(planet.seed);
            SimplexNoise simplexNoise1 = new SimplexNoise(random.Next());
            SimplexNoise simplexNoise2 = new SimplexNoise(random.Next());

            for (int i = 0; i < data.dataLength; i++)
            {
                double num1 = data.vertices[i].x * (double)planet.radius;
                double num2 = data.vertices[i].y * (double)planet.radius;
                double num3 = data.vertices[i].z * (double)planet.radius;
                double num5 = Maths.Levelize(num1 * 0.007);
                double num6 = Maths.Levelize(num2 * 0.007);
                double num7 = Maths.Levelize(num3 * 0.007);
                double xin = num5 + simplexNoise1.Noise(num1 * terrainSettings.xFactor, num2 * terrainSettings.xFactor, num3 * terrainSettings.xFactor) * 0.04 * terrainSettings.RandomFactor;
                double yin = num6 + simplexNoise1.Noise(num2 * terrainSettings.yFactor, num3 * terrainSettings.yFactor, num1 * terrainSettings.yFactor) * 0.04 * terrainSettings.RandomFactor;
                double zin = num7 + simplexNoise1.Noise(num3 * terrainSettings.zFactor, num1 * terrainSettings.zFactor, num2 * terrainSettings.zFactor) * 0.04 * terrainSettings.RandomFactor;
                double num8 = Math.Abs(simplexNoise2.Noise(xin, yin, zin));
                double num9 = (0.16 - num8) * 10.0 * (1 + terrainSettings.LandModifier);
                double num10 = num9 <= 0.0 ? 0.0 : num9 <= 1.0 ? num9 : 1.0;
                double num11 = num10 * num10;
                double num12 = (simplexNoise1.Noise3DFBM(num2 * 0.005, num3 * 0.005, num1 * 0.005, 4) + 0.22) * 5.0;
                double num13 = num12 <= 0.0 ? 0.0 : num12 <= 1.0 ? num12 : 1.0;
                double num14 = Math.Abs(simplexNoise2.Noise3DFBM(xin * 1.5, yin * 1.5, zin * 1.5, 2));
                double num15 = 0.0 - num11 * 1.2 * num13;
                if (num15 >= 0.0)
                {
                    num15 += num8 * 0.25 + num14 * 0.6;
                }

                double num16 = num15 - 0.1;
                double num17 = -0.3 - num16;
                if (num17 > 0.0)
                {
                    double num18 = num17 <= 1.0 ? num17 : 1.0;
                    num16 = -0.3 - (3.0 - num18 - num18) * num18 * num18 * 3.70000004768372;
                }

                double num19 = Maths.Levelize(num11 <= 0.300000011920929 ? 0.300000011920929 : num11, 0.7);
                double num20 = num16 <= -0.800000011920929 ? (-num19 - num8) * 0.899999976158142 : num16;
                double heightValue = num20 <= -1.20000004768372 ? -1.20000004768372 : num20;
                heightValue = heightValue * terrainSettings.HeightMulti + terrainSettings.BaseHeight;
                double biomeValue = heightValue * num11 + (num8 * 2.1 * terrainSettings.BiomeHeightMulti + 0.800000011920929 + terrainSettings.BiomeHeightModifier);
                if (biomeValue > 1.70000004768372 && biomeValue < 2.0)
                {
                    biomeValue = 2.0;
                }

                int heightDataValue = (int)((planet.radius + heightValue + 0.2) * 100.0);
                data.heightData[i] = (ushort)Mathf.Clamp(heightDataValue, ushort.MinValue, ushort.MaxValue);
                data.biomoData[i] = (byte)Mathf.Clamp((float)(biomeValue * 100.0), 0.0f, 200f);
            }
        }

        static void GenerateGs2Veins(PlanetData planet, ThemeVeinSettings veinSettings)
        {
            if (planet.data == null)
            {
                return;
            }

            DotNet35Random random = new DotNet35Random(planet.seed);
            List<VeinDescriptor> veinGroups = BuildGs2VeinDescriptors(veinSettings, random);
            if (veinGroups.Count == 0)
            {
                planet.data.veinCursor = 1;
                planet.veinGroups = new VeinGroup[1];
                planet.veinGroups[0].SetNull();
                return;
            }

            double randomFactor = 0.5 + random.NextDouble() / 2.0;
            float planetRadiusFactor = (float)Math.Pow(2.1 / Math.Max(planet.radius, 0.0001f), 2.0);
            Vector3 groupVector = RandomDirection(random);
            groupVector.Normalize();
            groupVector *= (float)(random.NextDouble() * 0.4 + 0.2);

            Dictionary<EVeinType, int> veinTotals = new Dictionary<EVeinType, int>();
            for (int i = 0; i < veinGroups.Count; i++)
            {
                VeinDescriptor veinGroup = veinGroups[i];
                if (random.NextDouble() > randomFactor && veinTotals.ContainsKey(veinGroup.Type))
                {
                    continue;
                }
                if (veinGroup.Rare && planet.star != null && planet.star.level + 0.1 < random.NextDouble() * random.NextDouble())
                {
                    continue;
                }

                bool oreVein = veinGroup.Type != EVeinType.Oil;
                bool succeeded = false;
                Vector3 potentialVector = Vector3.zero;
                for (int repeats = 0; repeats < 99; repeats++)
                {
                    potentialVector = RandomDirection(random);
                    if (oreVein)
                    {
                        potentialVector += groupVector;
                    }
                    potentialVector.Normalize();

                    float height = planet.data.QueryHeight(potentialVector);
                    if (height < planet.radius || (!oreVein && height < planet.radius + 0.5f))
                    {
                        continue;
                    }

                    float padding = planetRadiusFactor * (oreVein ? veinSettings.VeinPadding * 196f : 100f);
                    if (SurfaceVectorCollision(potentialVector, veinGroups, i, padding))
                    {
                        continue;
                    }

                    succeeded = true;
                    break;
                }

                if (!succeeded)
                {
                    continue;
                }

                if (!veinTotals.ContainsKey(veinGroup.Type))
                {
                    veinTotals.Add(veinGroup.Type, 1);
                }
                else
                {
                    veinTotals[veinGroup.Type]++;
                }
                veinGroup.Position = potentialVector;
                veinGroups[i] = veinGroup;
            }

            List<VeinDescriptor> placedGroups = veinGroups.Where(group => group.Position != Vector3.zero).ToList();
            InitializePlanetVeins(planet, placedGroups.Count);
            for (int groupIndex = 0; groupIndex < placedGroups.Count; groupIndex++)
            {
                AddGs2VeinGroupToPlanet(planet, placedGroups[groupIndex], (short)groupIndex, random);
            }
        }

        static List<VeinDescriptor> BuildGs2VeinDescriptors(ThemeVeinSettings veinSettings, DotNet35Random random)
        {
            List<ThemeVeinType> veinTypes = new List<ThemeVeinType>(veinSettings.VeinTypes ?? new List<ThemeVeinType>());
            CutVeinTypes(ref veinTypes, random);
            int maxCount = veinTypes.Count == 0 ? 0 : veinTypes.Max(type => type.Veins.Count);
            List<VeinDescriptor> distributed = new List<VeinDescriptor>();
            for (int i = 0; i < maxCount; i++)
            {
                foreach (ThemeVeinType veinType in veinTypes)
                {
                    if (veinType.Veins.Count <= i)
                    {
                        continue;
                    }
                    ThemeVein vein = veinType.Veins[i];
                    distributed.Add(new VeinDescriptor
                    {
                        Count = vein.Count < 0 ? random.Next(5, 25) : vein.Count,
                        Type = veinType.Type,
                        Position = Vector3.zero,
                        Rare = veinType.Rare,
                        Richness = vein.Richness < 0f ? (float)random.NextDouble() : vein.Richness
                    });
                }
            }
            return distributed;
        }

        static void AddGs2VeinGroupToPlanet(PlanetData planet, VeinDescriptor veinGroup, short groupIndex, DotNet35Random random)
        {
            Vector3 normalized = veinGroup.Position.normalized;
            Quaternion quaternion = Quaternion.FromToRotation(Vector3.up, normalized);
            Vector3 vectorRight = quaternion * Vector3.right;
            Vector3 vectorForward = quaternion * Vector3.forward;
            List<Vector2> nodeVectors = new List<Vector2> { Vector2.zero };
            int nodeCount = veinGroup.Type == EVeinType.Oil ? 1 : Mathf.Max(1, veinGroup.Count);
            GenerateNodeVectors(nodeVectors, nodeCount, random);

            int veinAmount = Mathf.RoundToInt(veinGroup.Richness * 100000f * (planet.star?.resourceCoef ?? 1f));
            veinAmount = (int)(veinAmount * (random.NextDouble() + 0.5));
            if (veinGroup.Type == EVeinType.Oil)
            {
                veinAmount *= 2;
            }
            if (veinAmount < 20)
            {
                veinAmount = 20;
            }
            if (veinGroup.Type != EVeinType.Oil)
            {
                veinAmount = Mathf.RoundToInt(veinAmount * DSPGame.GameDesc.resourceMultiplier);
            }
            if (DSPGame.GameDesc.resourceMultiplier >= 99.5f && veinGroup.Type != EVeinType.Oil)
            {
                veinAmount = 1000000000;
            }

            InitializeVeinGroup(planet, groupIndex, veinGroup.Type, normalized);
            float planetRadiusFactor = 2.1f / Math.Max(planet.radius, 0.0001f);
            foreach (Vector2 nodeVector in nodeVectors)
            {
                Vector3 veinPosition = normalized + (nodeVector.x * vectorRight + nodeVector.y * vectorForward) * planetRadiusFactor;
                planet.data.EraseVegetableAtPoint(veinPosition);
                veinPosition = veinPosition.normalized * planet.data.QueryHeight(veinPosition);
                AddVeinToPlanet(planet, veinAmount, veinGroup.Type, veinPosition, groupIndex, random);
            }
        }

        static void InitializePlanetVeins(PlanetData planet, int veinVectorCount)
        {
            planet.data.veinCursor = 1;
            planet.veinGroups = new VeinGroup[veinVectorCount + 1];
            planet.veinGroups[0].SetNull();
        }

        static void InitializeVeinGroup(PlanetData planet, short groupIndex, EVeinType veinType, Vector3 position)
        {
            int index = groupIndex + 1;
            planet.veinGroups[index].type = veinType;
            planet.veinGroups[index].pos = position;
            planet.veinGroups[index].count = 0;
            planet.veinGroups[index].amount = 0L;
        }

        static void AddVeinToPlanet(PlanetData planet, int amount, EVeinType veinType, Vector3 position, short groupIndex, DotNet35Random random)
        {
            int typeIndex = (int)veinType;
            int modelStart = PlanetModelingManager.veinModelIndexs[typeIndex];
            int modelCount = PlanetModelingManager.veinModelCounts[typeIndex];
            short realGroupIndex = (short)(groupIndex + 1);
            VeinData vein = new VeinData
            {
                amount = amount,
                pos = position,
                type = veinType,
                groupIndex = realGroupIndex,
                minerCount = 0,
                modelIndex = (short)random.Next(modelStart, modelStart + modelCount),
                productId = PlanetModelingManager.veinProducts[typeIndex]
            };
            planet.veinGroups[realGroupIndex].count++;
            planet.veinGroups[realGroupIndex].amount += vein.amount;
            planet.data.AddVeinData(vein);
        }

        static void GenerateNodeVectors(List<Vector2> nodeVectors, int maxCount, DotNet35Random random)
        {
            int attempts = 0;
            while (attempts++ < 20)
            {
                int existingCount = nodeVectors.Count;
                for (int i = 0; i < existingCount; i++)
                {
                    if (nodeVectors.Count >= maxCount)
                    {
                        break;
                    }
                    if (nodeVectors[i].sqrMagnitude > 36f)
                    {
                        continue;
                    }

                    double angle = random.NextDouble() * Math.PI * 2.0;
                    Vector2 randomVector = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    randomVector += nodeVectors[i] * 0.2f;
                    randomVector.Normalize();
                    Vector2 candidate = nodeVectors[i] + randomVector;
                    bool collides = nodeVectors.Any(existing => (existing - candidate).sqrMagnitude < 0.85f);
                    if (!collides)
                    {
                        nodeVectors.Add(candidate);
                    }
                }

                if (nodeVectors.Count >= maxCount)
                {
                    break;
                }
            }
        }

        static Vector3 RandomDirection(DotNet35Random random)
        {
            return new Vector3(
                (float)random.NextDouble() * 2f - 1f,
                (float)random.NextDouble() * 2f - 1f,
                (float)random.NextDouble() * 2f - 1f
            );
        }

        static bool SurfaceVectorCollision(Vector3 vector, List<VeinDescriptor> vectors, int processedVectorCount, float padding)
        {
            for (int i = 0; i < processedVectorCount; i++)
            {
                if (vectors[i].Position != Vector3.zero && (vectors[i].Position - vector).sqrMagnitude < padding)
                {
                    return true;
                }
            }
            return false;
        }

        static void CutVeinTypes(ref List<ThemeVeinType> veinTypes, DotNet35Random random)
        {
            if (veinTypes.Count < 1)
            {
                return;
            }
            int start = random.Next(veinTypes.Count);
            List<ThemeVeinType> reordered = new List<ThemeVeinType>();
            for (int i = start; i < veinTypes.Count; i++)
            {
                reordered.Add(veinTypes[i]);
            }
            for (int i = 0; i < start; i++)
            {
                reordered.Add(veinTypes[i]);
            }
            veinTypes = reordered;
        }

        public static void EnsureRegistered()
        {
            if (registered)
            {
                return;
            }
            if (LDB.themes == null || LDB.themes.dataArray == null || LDB.themes.dataArray.Length == 0)
            {
                return;
            }

            try
            {
                ThemeSpec giganticForest = CreateGiganticForest();
                ThemeSpec redForest = CreateRedForest();
                ThemeSpec beach = CreateBeach();
                ThemeSpec pandora = CreatePandora();

                RegisterTheme(giganticForest);
                RegisterTheme(redForest);
                RegisterTheme(CreateSulfurSea());
                RegisterTheme(CreateMoltenWorld());
                RegisterTheme(beach);
                RegisterTheme(pandora);
                RegisterTheme(CreateObsidian());
                RegisterTheme(CreateHotObsidian());
                RegisterHiddenTheme(CreateRemovedSmallPlanetSlot("RemovedSmallPlanetTheme34", BARREN));
                RegisterHiddenTheme(CreateRemovedSmallPlanetSlot("RemovedSmallPlanetTheme35", BARREN));
                RegisterHiddenTheme(CreateRemovedSmallPlanetSlot("RemovedSmallPlanetTheme36", ICE_GELISOL));
                RegisterTheme(CreateInferno());
                RegisterTheme(CreateOilGiant());
                RegisterTheme(CreateColdVariant(giganticForest, "GiganticForestCold"));
                RegisterTheme(CreateColdVariant(redForest, "RedForestCold"));
                RegisterTheme(CreateColdVariant(beach, "BeachCold"));
                RegisterTheme(CreateColdVariant(pandora, "PandoraCold"));

                registered = true;
                SetRandomGenerationEnabled(ConfigUtility.EnableGalacticScaleThemesInRandomGeneration);
                Plugin.Instance.Logger.LogInfo($"Registered {themeNameToId.Count} GalacticScale-style themes");
            }
            catch (Exception e)
            {
                Plugin.Instance.Logger.LogError("Failed to register GalacticScale-style themes: " + e.Message);
                Plugin.Instance.Logger.LogError(e.StackTrace);
            }
        }

        public static bool TryResolveThemeName(string name, out int themeId)
        {
            EnsureRegistered();
            string normalized = NormalizeName(name);
            if (themeNameToId.TryGetValue(normalized, out themeId))
            {
                return true;
            }

            foreach (ThemeProto theme in LDB.themes.dataArray)
            {
                if (theme == null)
                {
                    continue;
                }
                if (NormalizeName(theme.Name) == normalized ||
                    NormalizeName(theme.name) == normalized ||
                    NormalizeName(theme.DisplayName) == normalized ||
                    NormalizeName(theme.displayName) == normalized)
                {
                    themeId = theme.ID;
                    return true;
                }
            }

            themeId = default;
            return false;
        }

        public static string GetKnownThemeNames()
        {
            EnsureRegistered();
            List<string> names = new List<string>();
            foreach (ThemeProto theme in LDB.themes.dataArray)
            {
                if (theme != null && !string.IsNullOrWhiteSpace(theme.Name))
                {
                    if (theme.Name.StartsWith("RemovedSmallPlanetTheme", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    names.Add(theme.Name);
                }
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", names.ToArray());
        }

        public static void SetRandomGenerationEnabled(bool enabled)
        {
            EnsureRegistered();
            foreach (RegisteredThemeInfo info in registeredThemeInfos)
            {
                ThemeProto theme = LDB.themes.Select(info.Id);
                if (theme == null)
                {
                    continue;
                }
                theme.Distribute = enabled && info.IncludeInRandomGeneration
                    ? info.RandomDistribute
                    : EThemeDistribute.Rare;
            }
        }

        public static void ApplyRandomGenerationToGameDesc(GameDesc gameDesc, bool enabled)
        {
            EnsureRegistered();
            SetRandomGenerationEnabled(enabled);
            if (gameDesc == null || gameDesc.savedThemeIds == null)
            {
                return;
            }

            HashSet<int> gsThemeIds = new HashSet<int>(registeredThemeInfos.Select(info => info.Id));
            HashSet<int> weightedVanillaThemeIds = new HashSet<int>(
                registeredThemeInfos
                    .Where(info => info.ExtraRandomThemeIds != null)
                    .SelectMany(info => info.ExtraRandomThemeIds)
            );
            HashSet<int> seenWeightedVanillaThemeIds = new HashSet<int>();
            List<int> themeIds = new List<int>();

            foreach (int themeId in gameDesc.savedThemeIds)
            {
                if (gsThemeIds.Contains(themeId))
                {
                    continue;
                }
                if (weightedVanillaThemeIds.Contains(themeId))
                {
                    if (!seenWeightedVanillaThemeIds.Add(themeId))
                    {
                        continue;
                    }
                }
                themeIds.Add(themeId);
            }

            if (enabled)
            {
                foreach (RegisteredThemeInfo info in registeredThemeInfos)
                {
                    if (!info.IncludeInRandomGeneration)
                    {
                        continue;
                    }
                    if (info.ExtraRandomThemeIds != null)
                    {
                        themeIds.AddRange(info.ExtraRandomThemeIds);
                    }
                    for (int i = 0; i < Math.Max(1, info.RandomWeight); i++)
                    {
                        themeIds.Add(info.Id);
                    }
                }
            }

            gameDesc.savedThemeIds = themeIds.ToArray();
        }

        public static bool ReapplyRegisteredThemeMaterials(ThemeProto proto)
        {
            if (proto == null || !TryGetRegisteredThemeSpec(proto, out ThemeSpec spec))
            {
                return false;
            }
            try
            {
                ApplySpecAssets(proto, spec);
                if (materialReapplyLogged.Add(proto.ID))
                {
                    Plugin.Instance.Logger.LogInfo($"Applied GalacticScale-style material fix for theme {proto.ID}: {proto.Name}");
                }
                return true;
            }
            catch (Exception e)
            {
                Plugin.Instance.Logger.LogWarning($"Failed to reapply GalacticScale-style theme materials for {proto.Name}: {e.Message}");
                return false;
            }
        }

        public static bool ApplyRegisteredThemeToPlanet(PlanetData planet)
        {
            if (planet == null || !TryGetRegisteredThemeSpec(planet.theme, out ThemeSpec spec))
            {
                return false;
            }

            ThemeProto proto = LDB.themes.Select(planet.theme);
            if (proto == null)
            {
                return false;
            }

            try
            {
                ApplySpecAssets(proto, spec);
                ApplyThemeProtoToPlanetMaterials(planet, proto, spec);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Instance.Logger.LogWarning($"Failed to apply GalacticScale-style planet materials for {planet.displayName}: {e.Message}");
                return false;
            }
        }

        public static bool PrepareRegisteredThemeForPlanet(PlanetData planet)
        {
            if (planet == null || !TryGetRegisteredThemeSpec(planet.theme, out _))
            {
                return false;
            }
            return ReapplyRegisteredThemeMaterials(LDB.themes.Select(planet.theme));
        }

        public static bool ApplyRegisteredThemeToLoadedPlanet(PlanetData planet)
        {
            if (!ApplyRegisteredThemeToPlanet(planet))
            {
                return false;
            }

            SyncPlanetSimulatorMaterials(planet);
            return true;
        }

        public static void RefreshRegisteredThemeSimulator(PlanetSimulator simulator, Transform lookCamera, StarSimulator star)
        {
            if (simulator == null || simulator.planetData == null || !IsRegisteredTheme(simulator.planetData.theme))
            {
                return;
            }

            PlanetData planet = simulator.planetData;
            if (planet.loading || planet.factoryLoading || simulator.atmoTrans0 == null || simulator.atmoTrans1 == null ||
                simulator.atmoMat == null || simulator.atmoMatLate == null || lookCamera == null)
            {
                return;
            }

            Camera mainCamera = GameCamera.main;
            if (mainCamera == null)
            {
                return;
            }

            PlanetData localPlanet = GameMain.localPlanet;
            Quaternion rotation = localPlanet?.runtimeRotation ?? Quaternion.identity;
            Vector3 sunDir = Quaternion.Inverse(rotation) * (planet.star.uPosition - planet.uPosition).normalized;
            if (FactoryModel.whiteMode0 && GameCamera.instance?.camLight != null)
            {
                sunDir = -GameCamera.instance.camLight.transform.forward;
            }

            simulator.atmoTrans0.rotation = lookCamera.localRotation;
            Vector4 localPos = GameCamera.generalTarget == null ? Vector4.zero : (Vector4)GameCamera.generalTarget.position;
            Vector3 cameraPosition = mainCamera.transform.position;
            if (localPos.sqrMagnitude == 0f)
            {
                if (!GameCamera.instance.isPlanetMode && GameMain.mainPlayer != null)
                {
                    localPos = (Vector4)GameMain.mainPlayer.position;
                }
                else
                {
                    localPos = ((Vector4)(cameraPosition + mainCamera.transform.forward * 30f)).normalized * planet.realRadius;
                }
            }

            Vector3 posToCam = lookCamera.localPosition - simulator.transform.localPosition;
            float distanceToCam = Mathf.Max(posToCam.magnitude, 0.0001f);
            VectorLF3 uPos = planet.uPosition;
            if (localPlanet != null)
            {
                rotation = localPlanet.runtimeRotation;
                if (localPlanet == planet)
                {
                    uPos = VectorLF3.zero;
                }
                else
                {
                    uPos -= localPlanet.uPosition;
                    uPos = Maths.QInvRotateLF(rotation, uPos);
                }
            }
            else if (GameMain.mainPlayer != null)
            {
                uPos -= GameMain.mainPlayer.uPosition;
            }

            UniverseSimulator.VirtualMapping(uPos.x, uPos.y, uPos.z, cameraPosition, out _, out float vscale);
            float scaleFactor = GetPlanetScaleFactor(planet);
            simulator.atmoTrans1.localPosition = new Vector3(
                0f,
                0f,
                Mathf.Clamp(
                    Vector3.Dot(posToCam, lookCamera.forward) + 10f / scaleFactor,
                    0f,
                    Math.Max(320f, 320f * scaleFactor)
                )
            );

            float fov = mainCamera.fieldOfView;
            float aspect = mainCamera.aspect;
            float horizontalFov = Mathf.Atan(Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * aspect) * 2f * Mathf.Rad2Deg;
            if (horizontalFov > 90f)
            {
                simulator.atmoTrans1.localScale = Vector3.one * (planet.realRadius * 5f * Mathf.Tan(horizontalFov * 0.5f * Mathf.Deg2Rad));
            }
            else if (!Mathf.Approximately(simulator.atmoTrans1.localScale.x, planet.realRadius * 5f))
            {
                simulator.atmoTrans1.localScale = Vector3.one * (planet.realRadius * 5f);
            }

            float intensityControl = Mathf.Clamp01(8000f / distanceToCam);
            float radiusControl = Mathf.Clamp01(4000f / distanceToCam);
            float distanceControl = Mathf.Max(0f, distanceToCam / 6000f - 1f);
            Vector4 radiusParam = simulator.atmoMatRadiusParam;
            radiusParam.z = radiusParam.x + (radiusParam.z - radiusParam.x) * (2.7f - radiusControl * 1.7f);
            radiusParam *= vscale * scaleFactor;

            StarSimulator activeStar = star ?? GameMain.universeSimulator?.FindStarSimulator(planet.star);
            Color sunAtmosColor = activeStar != null ? activeStar.sunAtmosColor : Color.white;
            Color sunriseAtmosColor = activeStar != null ? activeStar.sunriseAtmosColor : Color.white;
            float scatterPower = Mathf.Max(60f * scaleFactor, (distanceToCam - planet.realRadius * 2f) * 0.18f);

            RefreshAtmosphereMaterial(simulator.atmoMat, simulator.transform.localPosition, sunDir, radiusParam, sunAtmosColor, sunriseAtmosColor, localPos, scatterPower, intensityControl, distanceControl);
            RefreshAtmosphereMaterial(simulator.atmoMatLate, simulator.transform.localPosition, sunDir, radiusParam, sunAtmosColor, sunriseAtmosColor, localPos, scatterPower, intensityControl, distanceControl);

            simulator.atmoMat.renderQueue = planet == localPlanet ? 2991 : 2989;
            if (planet == localPlanet)
            {
                simulator.atmoMatLate.renderQueue = 3200;
                SetIntIfExists(simulator.atmoMatLate, "_StencilRef", 2);
                SetIntIfExists(simulator.atmoMatLate, "_StencilComp", 3);
            }
            else
            {
                simulator.atmoMatLate.renderQueue = 2989;
                SetIntIfExists(simulator.atmoMatLate, "_StencilRef", 0);
                SetIntIfExists(simulator.atmoMatLate, "_StencilComp", 1);
            }
        }

        public static bool IsRegisteredTheme(int themeId)
        {
            EnsureRegistered();
            return TryGetRegisteredThemeSpec(themeId, out _);
        }

        static ThemeSpec CreateGiganticForest()
        {
            return new ThemeSpec
            {
                Name = "GiganticForest",
                DisplayName = "Gigantic Forest",
                BaseThemeId = OCEANIC_JUNGLE,
                OceanThemeId = MEDITERRANEAN,
                Algo = 1,
                Vegetables1 = new[]
                {
                    42, 42, 42, 46, 101, 101, 101, 101, 101, 101, 102, 102, 102, 102, 102, 102, 103, 103, 103, 103, 103,
                    103, 104, 104, 104, 104, 104, 104, 125, 125, 125, 125, 125, 125, 601, 601, 601, 601, 601, 601, 602, 602,
                    602, 602, 602, 602, 603, 603, 603, 603, 603, 603, 604, 604, 604, 604, 604, 604, 605, 605, 605, 605, 605,
                    605
                },
                Vegetables2 = new[] { 1001, 1002, 1003, 1005, 1006, 1007 },
                Vegetables3 = new[] { 43, 46, 47, 47, 101, 102, 103, 104, 106, 601, 602, 604 },
                Vegetables4 = new int[] { },
                Vegetables5 = new[]
                {
                    42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 43, 43, 43, 43, 43, 43, 46, 46, 47, 47, 47, 47, 47, 47, 47,
                    102, 103, 103, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 104, 125, 125,
                    125, 125, 125, 125, 125, 604, 604, 604, 604, 604, 604, 604, 605, 605, 605, 605, 605, 605, 605, 1001,
                    1001, 1001, 1001, 1001, 1001, 1001, 1002, 1002, 1002, 1002, 1002, 1002, 1002, 1002, 1002
                },
                Wind = 1.15f,
                Temperature = 0f,
                WaterHeight = 0f,
                WaterItemId = 1000,
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA6"
                }
            };
        }

        static ThemeSpec CreateRedForest()
        {
            ThemeProto redStone = LDB.themes.Select(RED_STONE);
            return new ThemeSpec
            {
                Name = "RedForest",
                DisplayName = "Red Forest",
                BaseThemeId = OCEANIC_JUNGLE,
                Algo = 1,
                Vegetables0 = new[] { 26, 26, 45, 603, 604 },
                Vegetables1 = new[] { 1001, 1001, 1001, 1001, 1001, 1001, 45, 26, 26 },
                Vegetables2 = new[] { 1001 },
                Vegetables3 = new[] { 26, 26, 26, 26, 45, 602, 603, 604 },
                Vegetables4 = new[] { 1001, 26, 602, 603, 604 },
                Vegetables5 = new[] { 25, 32, 36, 37, 39, 41 },
                VeinSpot = Clone(redStone.VeinSpot),
                VeinCount = Clone(redStone.VeinCount),
                VeinOpacity = Clone(redStone.VeinOpacity),
                RareVeins = Clone(redStone.RareVeins),
                RareSettings = Clone(redStone.RareSettings),
                TerrainTint = new Color(0.35f, 0.08f, 0.06f, 1f),
                AtmosphereTint = new Color(0.45f, 0.12f, 0.1f, 0.75f),
                OceanTint = new Color(0.45f, 0.03f, 0.02f, 0.4f),
                Wind = 1f,
                Temperature = 0f,
                WaterHeight = 0f,
                WaterItemId = 1000,
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA1",
                    BaseHeight = -0.7,
                    xFactor = 0.01,
                    yFactor = 0.012,
                    zFactor = 0.01,
                    HeightMulti = 1.2,
                    LandModifier = 1.3,
                    RandomFactor = 0.3,
                    BiomeHeightMulti = 2.0,
                    BiomeHeightModifier = 0.2
                }
            };
        }

        static ThemeSpec CreateSulfurSea()
        {
            return new ThemeSpec
            {
                Name = "SulfurSea",
                DisplayName = "Sulfurous Sea",
                BaseThemeId = OCEAN_WORLD,
                TerrainThemeId = GOBI,
                OceanThemeId = VOLCANIC_ASH,
                AtmosphereThemeId = VOLCANIC_ASH,
                Algo = 7,
                PlanetType = EPlanetType.Ocean,
                Vegetables0 = new int[] { },
                Vegetables1 = new int[] { },
                Vegetables2 = new int[] { },
                Vegetables3 = new[] { 601, 602, 603, 604, 605 },
                Vegetables4 = new[] { 601, 602, 603, 604, 605 },
                Vegetables5 = new int[] { },
                AmbientThemeId = VOLCANIC_ASH,
                OceanParams = new Dictionary<string, float>
                {
                    ["_GIGloss"] = 1f,
                    ["_GISaturate"] = 0.8f,
                    ["_GIStrengthDay"] = 1f,
                    ["_GIStrengthNight"] = 0f
                },
                Wind = 1f,
                IonHeight = 60f,
                Temperature = 1f,
                WaterHeight = -1.3f,
                WaterItemId = 1116,
                SFXPath = "SFX/sfx-amb-lava-1",
                AtmosphereTint = new Color(0.3f, 0.3f, 0f, 1f),
                TerrainColors = SulfurSeaTerrainColors(),
                TerrainParams = SulfurSeaTerrainParams(),
                OceanColors = SulfurSeaOceanColors(),
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA1",
                    HeightMulti = 1.0,
                    BaseHeight = -1.3,
                    LandModifier = -0.7,
                    RandomFactor = 0.1,
                    BiomeHeightMulti = 2.9,
                    BiomeHeightModifier = 1.0
                }
            };
        }

        static Dictionary<string, Color> SulfurSeaTerrainColors()
        {
            return new Dictionary<string, Color>
            {
                ["_AmbientColor0"] = new Color(0.18f, 0.145f, 0.08f, 1f),
                ["_AmbientColor1"] = new Color(0.105f, 0.085f, 0.055f, 1f),
                ["_AmbientColor2"] = new Color(0.045f, 0.043f, 0.035f, 1f),
                ["_Color"] = new Color(0.62f, 0.54f, 0.36f, 1f),
                ["_EmissionColor"] = new Color(0f, 0f, 0f, 1f),
                ["_HeightEmissionColor"] = new Color(0f, 0f, 0f, 0f),
                ["_LightColorScreen"] = new Color(0.12f, 0.10f, 0.06f, 1f),
                ["_SpeclColor"] = new Color(0.12f, 0.10f, 0.08f, 1f),
                ["_Rotation"] = new Color(0f, 0f, 0f, 1f),
                ["_SunDir"] = new Color(0.6943676f, -0.1823572f, 0.6961318f, 0f)
            };
        }

        static Dictionary<string, float> SulfurSeaTerrainParams()
        {
            return new Dictionary<string, float>
            {
                ["_AmbientInc"] = 0.25f,
                ["_BioFuzzMask"] = 1f,
                ["_BioFuzzStrength"] = 0f,
                ["_BumpScale"] = 1f,
                ["_Cutoff"] = 0.5f,
                ["_DetailNormalMapScale"] = 1f,
                ["_Distance"] = 290.6332f,
                ["_DstBlend"] = 0f,
                ["_EmissionStrength"] = 0f,
                ["_GlossMapScale"] = 1f,
                ["_Glossiness"] = 0.5f,
                ["_GlossyReflections"] = 1f,
                ["_HeightEmissionRadius"] = 50f,
                ["_Metallic"] = 0f,
                ["_Mode"] = 0f,
                ["_Multiplier"] = 0.8f,
                ["_NormalStrength"] = 0.55f,
                ["_OcclusionStrength"] = 1f,
                ["_Parallax"] = 0.02f,
                ["_Radius"] = 200f,
                ["_SmoothnessTextureChannel"] = 0f,
                ["_SpecularHighlights"] = 0f,
                ["_SrcBlend"] = 1f,
                ["_StepBlend"] = 0.35f,
                ["_UVSec"] = 0f,
                ["_ZWrite"] = 1f
            };
        }

        static Dictionary<string, Color> SulfurSeaOceanColors()
        {
            return new Dictionary<string, Color>
            {
                ["_BumpDirection"] = new Color(1f, 1f, -1f, 1f),
                ["_BumpTiling"] = new Color(1f, 1f, -2f, 3f),
                ["_CausticsColor"] = new Color(0.3867925f, 0.3267751f, 0.08940012f, 1f),
                ["_Color"] = new Color(0.745283f, 0.6000856f, 0.4745906f, 1f),
                ["_Color0"] = new Color(0f, 0.1574037f, 0.2352941f, 1f),
                ["_Color1"] = new Color(0.7264151f, 0.7264151f, 0.7264151f, 1f),
                ["_Color2"] = new Color(0.6763465f, 0.6792453f, 0.6183695f, 1f),
                ["_Color3"] = new Color(0.6037736f, 0.523596f, 0.4243503f, 1f),
                ["_DensityParams"] = new Color(0.02f, 0.1f, 0f, 0f),
                ["_DepthColor"] = new Color(0f, 0.06095791f, 0.1132075f, 1f),
                ["_DepthFactor"] = new Color(0.4f, 0.4f, 0.5f, 0.1f),
                ["_Foam"] = new Color(15f, 1f, 5f, 1.5f),
                ["_FoamColor"] = new Color(0.3925616f, 0.4150942f, 0.2581885f, 1f),
                ["_FoamParams"] = new Color(12f, 0.2f, 0.15f, 0.7f),
                ["_FresnelColor"] = new Color(0.383585f, 0.2812832f, 0.4056602f, 1f),
                ["_InvFadeParemeter"] = new Color(0.9f, 0.25f, 0.5f, 0.08f),
                ["_PLColor1"] = new Color(0f, 0f, 0f, 1f),
                ["_PLColor2"] = new Color(0f, 0f, 0f, 1f),
                ["_PLColor3"] = new Color(1f, 1f, 1f, 1f),
                ["_PLParam1"] = new Color(0f, 0f, 0f, 0f),
                ["_PLParam2"] = new Color(0f, 0f, 0f, 0f),
                ["_PLPos1"] = new Color(0f, 0f, 0f, 0f),
                ["_PLPos2"] = new Color(0f, 0f, 0f, 0f),
                ["_PLPos3"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_Paremeters1"] = new Color(0.02f, 0.1f, 0f, 0f),
                ["_PointAtten"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_PointLightPos"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_ReflectionColor"] = new Color(0.1933962f, 0.5064065f, 1f, 1f),
                ["_SLColor1"] = new Color(1f, 1f, 1f, 1f),
                ["_SLDir1"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_SLPos1"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_SpecColor"] = new Color(1f, 1f, 1f, 1f),
                ["_SpeclColor"] = new Color(0.4100249f, 0.4411012f, 0.497f, 1f),
                ["_SpeclColor1"] = new Color(0.5257207f, 0.5331763f, 0.6226414f, 1f),
                ["_Specular"] = new Color(0.9573934f, 0.8672858f, 0.5744361f, 0.9573934f),
                ["_SunDirection"] = new Color(-0.6f, 0.8f, 0f, 0f),
                ["_WorldLightDir"] = new Color(-0.6525278f, -0.6042119f, -0.4573132f, 0f)
            };
        }

        static ThemeSpec CreateMoltenWorld()
        {
            return new ThemeSpec
            {
                Name = "MoltenWorld",
                DisplayName = "Molten World",
                BaseThemeId = LAVA,
                Algo = 3,
                ExtraRandomThemeIds = new[] { LAVA, LAVA, LAVA, LAVA },
                AmbientThemeId = LAVA,
                WaterHeight = -1.5f,
                WaterItemId = 1116,
                Wind = 0.8f,
                IonHeight = 70f,
                SFXPath = "SFX/sfx-amb-lava-1",
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA1",
                    BaseHeight = -1.5,
                    xFactor = 0.01,
                    yFactor = 0.012,
                    zFactor = 0.01,
                    HeightMulti = 0.4,
                    LandModifier = -0.9,
                    RandomFactor = -1.0,
                    BiomeHeightMulti = 2.0,
                    BiomeHeightModifier = 1.2
                }
            };
        }

        static ThemeSpec CreateBeach()
        {
            return new ThemeSpec
            {
                Name = "Beach",
                DisplayName = "Beach",
                BaseThemeId = OCEAN_WORLD,
                Algo = 1,
                RandomDistribute = EThemeDistribute.Interstellar,
                RandomWeight = 3,
                Vegetables0 = new int[] { },
                Vegetables1 = new int[] { },
                Vegetables2 = new[] { 1001, 1002, 1003 },
                Vegetables3 = new[] { 1001, 1002, 1003 },
                Vegetables4 = new[] { 1004 },
                Vegetables5 = new int[] { },
                Temperature = 0.4f,
                WaterHeight = 0f,
                WaterItemId = 1000,
                Wind = 1.1f,
                AmbientLutContribution = 0.25f,
                AmbientOverride = ApplyGsOceanWorldAmbient,
                TerrainColors = GsOceanWorldTerrainColors(),
                TerrainParams = GsOceanWorldTerrainParams(),
                OceanColors = GsOceanWorldOceanColors(),
                OceanParams = GsOceanWorldOceanParams(),
                AtmosphereColors = GsOceanWorldAtmosphereColors(),
                AtmosphereParams = GsOceanWorldAtmosphereParams(),
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA1"
                },
                VeinSettings = CreateBeachVeinSettings()
            };
        }

        static ThemeVeinSettings CreateBeachVeinSettings()
        {
            return new ThemeVeinSettings
            {
                Algorithm = "GS2",
                VeinPadding = 1f,
                VeinTypes = new List<ThemeVeinType>
                {
                    ThemeVeinType.Generate(EVeinType.Silicium, 10, 30, 1f, 10f, 5, 25, false, 1001),
                    ThemeVeinType.Generate(EVeinType.Bamboo, 2, 6, 1f, 10f, 5, 25, true, 1002),
                    ThemeVeinType.Generate(EVeinType.Fractal, 2, 6, 1f, 10f, 5, 25, false, 1003),
                    ThemeVeinType.Generate(EVeinType.Grat, 2, 6, 1f, 10f, 5, 25, false, 1004)
                }
            };
        }

        static Dictionary<string, Color> GsOceanWorldTerrainColors()
        {
            return new Dictionary<string, Color>
            {
                ["_AmbientColor0"] = new Color(0.1098038f, 0.1415093f, 0.1333332f, 1f),
                ["_AmbientColor1"] = new Color(0.06666655f, 0.03519787f, 0.03137255f, 1f),
                ["_AmbientColor2"] = new Color(0.03921569f, 0.03921569f, 0.1764704f, 1f),
                ["_Color"] = new Color(1f, 1f, 1f, 1f),
                ["_EmissionColor"] = new Color(0f, 0f, 0f, 1f),
                ["_HeightEmissionColor"] = new Color(0f, 0f, 0f, 0f),
                ["_LightColorScreen"] = new Color(0f, 0f, 0f, 1f),
                ["_Rotation"] = new Color(0f, 0f, 0f, 1f),
                ["_SunDir"] = new Color(0.3786571f, 0.01833941f, 0.9253553f, 0f)
            };
        }

        static void ApplyGsOceanWorldAmbient(AmbientDesc ambient)
        {
            ambient.ambientColor0 = new Color(0.1098039f, 0.1415094f, 0.1333333f, 1f);
            ambient.ambientColor1 = new Color(0.03137255f, 0.05890403f, 0.06666667f, 1f);
            ambient.ambientColor2 = new Color(0.03921569f, 0.03921569f, 0.1764706f, 1f);
            ambient.waterAmbientColor0 = new Color(0f, 0f, 0f, 1f);
            ambient.waterAmbientColor1 = new Color(0.1354839f, 0.1806452f, 0.2f, 1f);
            ambient.waterAmbientColor2 = new Color(0.03888888f, 0.1444444f, 0.2f, 1f);
            ambient.biomoColor0 = new Color(0.8745098f, 0.6923354f, 0.3960784f, 1f);
            ambient.biomoColor1 = new Color(0.8745098f, 0.6941177f, 0.3960784f, 1f);
            ambient.biomoColor2 = new Color(0.8745098f, 0.6941177f, 0.3960784f, 1f);
            ambient.biomoDustColor0 = new Color(1f, 0.8645418f, 0.6812749f, 1f);
            ambient.biomoDustColor1 = new Color(1f, 0.8627452f, 0.682353f, 1f);
            ambient.biomoDustColor2 = new Color(1f, 0.8627452f, 0.682353f, 1f);
            ambient.biomoDustStrength0 = 4f;
            ambient.biomoDustStrength1 = 4f;
            ambient.biomoDustStrength2 = 4f;
            ambient.biomoSound0 = 0;
            ambient.biomoSound1 = 0;
            ambient.biomoSound2 = 0;
            ambient.lutContribution = 0.25f;
        }

        static Dictionary<string, float> GsOceanWorldTerrainParams()
        {
            return new Dictionary<string, float>
            {
                ["_AmbientInc"] = 0.9f,
                ["_BioFuzzMask"] = 1f,
                ["_BioFuzzStrength"] = 0.1f,
                ["_BumpScale"] = 1f,
                ["_Cutoff"] = 0.5f,
                ["_DetailNormalMapScale"] = 1f,
                ["_Distance"] = 314.7685f,
                ["_DstBlend"] = 0f,
                ["_EmissionStrength"] = 0f,
                ["_GlossMapScale"] = 1f,
                ["_Glossiness"] = 0.5f,
                ["_GlossyReflections"] = 1f,
                ["_HeightEmissionRadius"] = 50f,
                ["_Metallic"] = 0f,
                ["_Mode"] = 0f,
                ["_Multiplier"] = 1.6f,
                ["_NormalStrength"] = 1f,
                ["_OcclusionStrength"] = 1f,
                ["_Parallax"] = 0.02f,
                ["_Radius"] = 200f,
                ["_SmoothnessTextureChannel"] = 0f,
                ["_SpecularHighlights"] = 1f,
                ["_SrcBlend"] = 1f,
                ["_StepBlend"] = 0.55f,
                ["_UVSec"] = 0f,
                ["_ZWrite"] = 1f
            };
        }

        static Dictionary<string, Color> GsOceanWorldOceanColors()
        {
            return new Dictionary<string, Color>
            {
                ["_BumpDirection"] = new Color(1f, 1f, -1f, 1f),
                ["_BumpTiling"] = new Color(1f, 1f, -2f, 3f),
                ["_CausticsColor"] = new Color(0.5506853f, 0.7075471f, 0.6751246f, 1f),
                ["_Color"] = new Color(1f, 0.9647059f, 0.7686275f, 1f),
                ["_Color0"] = new Color(0f, 0.1574037f, 0.2352941f, 1f),
                ["_Color1"] = new Color(0.5764706f, 0.8313726f, 0.7921569f, 1f),
                ["_Color2"] = new Color(0.3551085f, 0.6892567f, 0.7169812f, 1f),
                ["_Color3"] = new Color(0.2731398f, 0.504477f, 0.5849056f, 1f),
                ["_DensityParams"] = new Color(0.02f, 0.1f, 0f, 0f),
                ["_DepthColor"] = new Color(0f, 0.06095791f, 0.1132075f, 1f),
                ["_DepthFactor"] = new Color(0.4f, 0.35f, 0.15f, 0.1f),
                ["_Foam"] = new Color(15f, 1f, 5f, 1.5f),
                ["_FoamColor"] = new Color(1f, 1f, 1f, 1f),
                ["_FoamParams"] = new Color(12f, 0.2f, 0.15f, 0.7f),
                ["_FresnelColor"] = new Color(0.2470588f, 0.6588235f, 0.8862746f, 1f),
                ["_InvFadeParemeter"] = new Color(0.9f, 0.25f, 0.5f, 0.08f),
                ["_PLColor1"] = new Color(0f, 0f, 0f, 1f),
                ["_PLColor2"] = new Color(0f, 0f, 0f, 1f),
                ["_PLColor3"] = new Color(1f, 1f, 1f, 1f),
                ["_PLParam1"] = new Color(0f, 0f, 0f, 0f),
                ["_PLParam2"] = new Color(0f, 0f, 0f, 0f),
                ["_PLPos1"] = new Color(0f, 0f, 0f, 0f),
                ["_PLPos2"] = new Color(0f, 0f, 0f, 0f),
                ["_PLPos3"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_Paremeters1"] = new Color(0.02f, 0.1f, 0f, 0f),
                ["_PointAtten"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_PointLightPos"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_ReflectionColor"] = new Color(0.1933962f, 0.5064065f, 1f, 1f),
                ["_SLColor1"] = new Color(1f, 1f, 1f, 1f),
                ["_SLDir1"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_SLPos1"] = new Color(0f, 0.1f, -0.5f, 0f),
                ["_SpecColor"] = new Color(1f, 1f, 1f, 1f),
                ["_SpeclColor"] = new Color(1f, 1f, 1f, 1f),
                ["_SpeclColor1"] = new Color(0.8962264f, 0.614061f, 0.1733267f, 1f),
                ["_Specular"] = new Color(0.9573934f, 0.8672858f, 0.5744361f, 0.9573934f),
                ["_SunDirection"] = new Color(-0.6f, 0.8f, 0f, 0f),
                ["_WorldLightDir"] = new Color(-0.6525278f, -0.6042119f, -0.4573132f, 0f)
            };
        }

        static Dictionary<string, float> GsOceanWorldOceanParams()
        {
            return new Dictionary<string, float>
            {
                ["_CausticsTiling"] = 0.03f,
                ["_DistortionStrength"] = 1f,
                ["_FoamInvThickness"] = 6f,
                ["_FoamSpeed"] = 0.15f,
                ["_FoamSync"] = 4f,
                ["_GIGloss"] = 0.6f,
                ["_GISaturate"] = 1f,
                ["_GIStrengthDay"] = 1f,
                ["_GIStrengthNight"] = 0.03f,
                ["_NormalSpeed"] = 0.6f,
                ["_NormalStrength"] = 0.4f,
                ["_NormalTiling"] = 0.06f,
                ["_PLEdgeAtten"] = 0.5f,
                ["_PLIntensity2"] = 0f,
                ["_PLIntensity3"] = 0f,
                ["_PLRange2"] = 10f,
                ["_PLRange3"] = 10f,
                ["_PointLightK"] = 0.01f,
                ["_PointLightRange"] = 10f,
                ["_Radius"] = 200f,
                ["_ReflectionBlend"] = 0.86f,
                ["_ReflectionTint"] = 0f,
                ["_RefractionAmt"] = 1000f,
                ["_RefractionStrength"] = 0.3f,
                ["_SLCosCutoff1"] = 0.3f,
                ["_SLIntensity1"] = 1f,
                ["_SLRange1"] = 10f,
                ["_Shininess"] = 40f,
                ["_ShoreIntens"] = 1.4f,
                ["_SpeclColorDayStrength"] = 0f,
                ["_SpotExp"] = 2f,
                ["_Tile"] = 0.05f
            };
        }

        static Dictionary<string, Color> GsOceanWorldAtmosphereColors()
        {
            return new Dictionary<string, Color>
            {
                ["_Color"] = new Color(0.3443396f, 0.734796f, 1f, 1f),
                ["_Color0"] = new Color(0.3882353f, 0.7017767f, 1f, 1f),
                ["_Color1"] = new Color(0.28854f, 0.7906604f, 0.916f, 1f),
                ["_Color2"] = new Color(0.4466639f, 0.84536f, 0.888f, 1f),
                ["_Color3"] = new Color(0.7357877f, 0.923f, 0.9083642f, 1f),
                ["_Color4"] = new Color(1f, 0.7438396f, 0.4147364f, 1f),
                ["_Color5"] = new Color(0.2392156f, 0.8758385f, 1f, 1f),
                ["_Color6"] = new Color(0.3529411f, 0.7509555f, 1f, 1f),
                ["_Color7"] = new Color(0.2078431f, 0.4966022f, 0.6980392f, 1f),
                ["_Color8"] = new Color(1f, 1f, 1f, 1f),
                ["_ColorF"] = new Color(0.4669811f, 0.9485019f, 1f, 1f),
                ["_EmissionColor"] = new Color(0f, 0f, 0f, 1f),
                ["_LocalPos"] = new Color(78.1805f, 142.5101f, 140.7908f, 0f),
                ["_PlanetPos"] = new Color(0f, 0f, 0f, 0f),
                ["_PlanetRadius"] = new Color(200f, 199.98f, 270f, 0f),
                ["_Sky0"] = new Color(0.4198112f, 0.6650285f, 1f, 0.1607843f),
                ["_Sky1"] = new Color(0.485849f, 0.5557498f, 1f, 0.09803922f),
                ["_Sky2"] = new Color(0.839f, 1f, 0.9984235f, 0.9176471f),
                ["_Sky3"] = new Color(0.2666666f, 0.7909663f, 1f, 0.6705883f),
                ["_Sky4"] = new Color(1f, 0.7368349f, 0.3171324f, 1f)
            };
        }

        static Dictionary<string, float> GsOceanWorldAtmosphereParams()
        {
            return new Dictionary<string, float>
            {
                ["_AtmoDensity"] = 1f,
                ["_AtmoThickness"] = 70f,
                ["_BumpScale"] = 1f,
                ["_Cutoff"] = 0.5f,
                ["_Density"] = 0.005f,
                ["_DetailNormalMapScale"] = 1f,
                ["_DistanceControl"] = 0f,
                ["_DstBlend"] = 0f,
                ["_FarFogDensity"] = 0.03f,
                ["_FogDensity"] = 0.3f,
                ["_FogSaturate"] = 1.5f,
                ["_GlossMapScale"] = 1f,
                ["_Glossiness"] = 0.5f,
                ["_GlossyReflections"] = 1f,
                ["_GroundAtmosPower"] = 3f,
                ["_Intensity"] = 1f,
                ["_IntensityControl"] = 1f,
                ["_Metallic"] = 0f,
                ["_Mode"] = 0f,
                ["_OcclusionStrength"] = 1f,
                ["_Parallax"] = 0.02f,
                ["_RimFogExp"] = 1.35f,
                ["_RimFogPower"] = 3.2f,
                ["_SkyAtmosPower"] = 7f,
                ["_SmoothnessTextureChannel"] = 0f,
                ["_SpecularHighlights"] = 1f,
                ["_SrcBlend"] = 1f,
                ["_StencilComp"] = 8f,
                ["_StencilRef"] = 0f,
                ["_SunColorAdd"] = 0f,
                ["_SunColorSkyUse"] = 0.2f,
                ["_SunColorUse"] = 0.6f,
                ["_SunRiseScatterPower"] = 60f,
                ["_UVSec"] = 0f,
                ["_ZWrite"] = 1f
            };
        }

        static ThemeSpec CreatePandora()
        {
            return new ThemeSpec
            {
                Name = "Pandora",
                DisplayName = "Pandora",
                BaseThemeId = PANDORA_SWAMP,
                OceanThemeId = MEDITERRANEAN,
                Algo = 13,
                OceanTint = new Color(0.15f, 0f, 0.3f, 0.5f),
                AtmosphereTint = new Color(0.25f, 0.05f, 0.4f, 0.65f),
                Temperature = 0f,
                WaterHeight = 0f,
                WaterItemId = 1000,
                Wind = 1f,
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "Vanilla"
                }
            };
        }

        static ThemeSpec CreateObsidian()
        {
            return new ThemeSpec
            {
                Name = "Obsidian",
                DisplayName = "Obsidian",
                BaseThemeId = ICE_GELISOL,
                Algo = 2,
                PlanetType = EPlanetType.Ice,
                RandomDistribute = EThemeDistribute.Interstellar,
                RandomWeight = 3,
                TerrainTint = new Color(0.15f, 0.15f, 0.15f, 1f),
                OceanTint = new Color(0f, 0f, 0f, 0.5f),
                AtmosphereTint = new Color(0f, 0f, 0f, 1f),
                AmbientThemeId = ICE_GELISOL,
                AmbientLutContribution = 1f,
                AmbientReflectionColor = new Color(0f, 0f, 0f, 1f),
                TerrainParams = new Dictionary<string, float>
                {
                    ["_AmbientInc"] = 0f,
                    ["_GISaturate"] = 0.0f,
                    ["_GIStrengthDay"] = 0.01505f,
                    ["_GIStrengthNight"] = 0.013f,
                    ["_Multiplier"] = 0.0018f,
                    ["_NormalStrength"] = 0.21010f,
                    ["_SpecularHighlights"] = 110.10f
                },
                TerrainColors = new Dictionary<string, Color>
                {
                    ["_SpeclColor"] = new Color(0.14f, 0.14f, 0.14f, 1f)
                },
                AtmosphereColors = new Dictionary<string, Color>
                {
                    ["_Color"] = new Color(1f, 1f, 1f, 1f)
                },
                Temperature = 2f,
                Wind = 0.4f,
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA3",
                    BiomeHeightMulti = 0.0,
                    BiomeHeightModifier = -111.0,
                    HeightMulti = 1.0,
                    RandomFactor = 1.0
                }
            };
        }

        static ThemeSpec CreateHotObsidian()
        {
            return new ThemeSpec
            {
                Name = "HotObsidian",
                DisplayName = "Hot Obsidian",
                BaseThemeId = ICE_GELISOL,
                Algo = 2,
                PlanetType = EPlanetType.Ice,
                RandomDistribute = EThemeDistribute.Interstellar,
                RandomWeight = 2,
                TerrainTint = new Color(0.2f, 0.05f, 0.05f, 1f),
                OceanTint = new Color(0f, 0f, 0f, 0.5f),
                AtmosphereTint = new Color(0f, 0f, 0f, 1f),
                AmbientThemeId = ICE_GELISOL,
                AmbientLutContribution = 1f,
                AmbientReflectionColor = new Color(0f, 0f, 0f, 1f),
                TerrainParams = ObsidianTerrainParams(),
                TerrainColors = new Dictionary<string, Color>
                {
                    ["_SpeclColor"] = new Color(0.14f, 0.14f, 0.14f, 1f)
                },
                AtmosphereColors = new Dictionary<string, Color>
                {
                    ["_Color"] = new Color(1f, 1f, 1f, 1f)
                },
                Temperature = 5f,
                Wind = 0.3f,
                TerrainSettings = new ThemeTerrainSettings
                {
                    Algorithm = "GSTA3",
                    BiomeHeightMulti = 0.0,
                    BiomeHeightModifier = -111.0,
                    HeightMulti = 1.0,
                    RandomFactor = 1.0
                }
            };
        }

        static Dictionary<string, float> ObsidianTerrainParams()
        {
            return new Dictionary<string, float>
            {
                ["_AmbientInc"] = 0f,
                ["_GISaturate"] = 0.0f,
                ["_GIStrengthDay"] = 0.01505f,
                ["_GIStrengthNight"] = 0.013f,
                ["_Multiplier"] = 0.0018f,
                ["_NormalStrength"] = 0.21010f,
                ["_SpecularHighlights"] = 110.10f
            };
        }

        static ThemeSpec CreateInferno()
        {
            return new ThemeSpec
            {
                Name = "Inferno",
                DisplayName = "Infernal Gas Giant",
                BaseThemeId = GAS_GIANT,
                TerrainThemeId = GAS_GIANT,
                OceanThemeId = GAS_GIANT,
                Algo = 0,
                PlanetType = EPlanetType.Gas,
                GasItems = new[] { 1120, 1121 },
                GasSpeeds = new[] { 0.3f, 0.15f },
                TerrainTint = new Color(1f, 0.45f, 0.04f, 0.75f),
                OceanTint = new Color(1f, 0.2f, 0.03f, 0.7f),
                AmbientThemeId = MEDITERRANEAN,
                TerrainParams = new Dictionary<string, float>
                {
                    ["_SkyAtmosPower"] = 10f,
                    ["_Intensity"] = 0.5f,
                    ["_Multiplier"] = 0.5f,
                    ["_AtmoThickness"] = 3f
                },
                TerrainColors = new Dictionary<string, Color>
                {
                    ["_Color1"] = new Color(0f, 0f, 0f, 1f),
                    ["_Color2"] = new Color(0f, 0f, 0f, 1f),
                    ["_Color3"] = new Color(0f, 0f, 0f, 1f),
                    ["_Color4"] = new Color(0f, 0f, 0f, 1f)
                },
                OceanColors = new Dictionary<string, Color>
                {
                    ["_Color"] = new Color(0.288f, 0.14f, 0.03f, 1f)
                },
                Temperature = 4f,
                WaterItemId = 1000,
                SFXPath = "SFX/sfx-amb-massive"
            };
        }

        static ThemeSpec CreateOilGiant()
        {
            return new ThemeSpec
            {
                Name = "OilGiant",
                DisplayName = "Oil Giant",
                BaseThemeId = ICE_GIANT,
                Algo = 0,
                PlanetType = EPlanetType.Gas,
                GasItems = new[] { 1114, 1120 },
                GasSpeeds = new[] { 0.1f, 10f },
                TerrainTint = new Color(0.08f, 0.08f, 0.08f, 0.95f),
                AtmosphereTint = new Color(0f, 0f, 0f, 0.85f),
                Temperature = -1f,
                WaterItemId = 0,
                SFXPath = "SFX/sfx-amb-massive"
            };
        }

        static ThemeSpec CreateColdVariant(ThemeSpec source, string name)
        {
            ThemeSpec spec = source.Clone();
            spec.Name = name;
            spec.PlanetType = EPlanetType.Ice;
            spec.Temperature = -1f;
            return spec;
        }

        static ThemeSpec CreateRemovedSmallPlanetSlot(string name, int baseThemeId)
        {
            return new ThemeSpec
            {
                Name = name,
                DisplayName = "Removed Small Planet Theme",
                BaseThemeId = baseThemeId,
                IncludeInRandomGeneration = false,
                RandomWeight = 0
            };
        }

        static void RegisterTheme(ThemeSpec spec)
        {
            if (TryResolveAlreadyRegistered(spec.Name, out int existingId))
            {
                AddAliases(spec, existingId);
                RememberRegisteredTheme(spec, existingId, LDB.themes.Select(existingId)?.Distribute ?? EThemeDistribute.Interstellar);
                return;
            }

            ThemeProto proto = BuildThemeProto(spec);
            EThemeDistribute randomDistribute = spec.RandomDistribute ?? proto.Distribute;
            proto.Distribute = EThemeDistribute.Rare;
            ThemeProtoSet themes = LDB.themes;
            int newIndex = themes.dataArray.Length;
            Array.Resize(ref themes.dataArray, newIndex + 1);
            int newId = themes.dataArray.Length;
            proto.ID = newId;
            themes.dataArray[newIndex] = proto;
            themes.OnAfterDeserialize();
            AddAliases(spec, newId);
            RememberRegisteredTheme(spec, newId, randomDistribute);
        }

        static void RegisterHiddenTheme(ThemeSpec spec)
        {
            if (TryResolveAlreadyRegistered(spec.Name, out int existingId))
            {
                RememberRegisteredTheme(spec, existingId, LDB.themes.Select(existingId)?.Distribute ?? EThemeDistribute.Rare);
                return;
            }

            ThemeProto proto = BuildThemeProto(spec);
            EThemeDistribute randomDistribute = spec.RandomDistribute ?? proto.Distribute;
            proto.Distribute = EThemeDistribute.Rare;
            ThemeProtoSet themes = LDB.themes;
            int newIndex = themes.dataArray.Length;
            Array.Resize(ref themes.dataArray, newIndex + 1);
            int newId = themes.dataArray.Length;
            proto.ID = newId;
            themes.dataArray[newIndex] = proto;
            themes.OnAfterDeserialize();
            RememberRegisteredTheme(spec, newId, randomDistribute);
        }

        static bool TryResolveAlreadyRegistered(string name, out int themeId)
        {
            string normalized = NormalizeName(name);
            foreach (ThemeProto theme in LDB.themes.dataArray)
            {
                if (theme == null)
                {
                    continue;
                }
                if (NormalizeName(theme.Name) == normalized || NormalizeName(theme.name) == normalized)
                {
                    themeId = theme.ID;
                    return true;
                }
            }
            themeId = default;
            return false;
        }

        static void AddAliases(ThemeSpec spec, int id)
        {
            AddAlias(spec.Name, id);
            AddAlias(spec.DisplayName, id);
        }

        static void AddAlias(string name, int id)
        {
            string normalized = NormalizeName(name);
            if (!string.IsNullOrWhiteSpace(normalized) && !themeNameToId.ContainsKey(normalized))
            {
                themeNameToId[normalized] = id;
            }
        }

        static void RememberRegisteredTheme(ThemeSpec spec, int id, EThemeDistribute randomDistribute)
        {
            themeSpecsById[id] = spec;
            string normalizedName = NormalizeName(spec.Name);
            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                themeSpecsByName[normalizedName] = spec;
            }
            string normalizedDisplayName = NormalizeName(spec.DisplayName);
            if (!string.IsNullOrWhiteSpace(normalizedDisplayName))
            {
                themeSpecsByName[normalizedDisplayName] = spec;
            }
            if (registeredThemeInfos.Any(info => info.Id == id))
            {
                return;
            }
            registeredThemeInfos.Add(new RegisteredThemeInfo
            {
                Id = id,
                IncludeInRandomGeneration = spec.IncludeInRandomGeneration,
                RandomWeight = spec.RandomWeight,
                RandomDistribute = randomDistribute,
                ExtraRandomThemeIds = Clone(spec.ExtraRandomThemeIds)
            });
        }

        static bool TryGetRegisteredThemeSpec(ThemeProto proto, out ThemeSpec spec)
        {
            if (themeSpecsById.TryGetValue(proto.ID, out spec))
            {
                return true;
            }
            if (themeSpecsByName.TryGetValue(NormalizeName(proto.Name), out spec) ||
                themeSpecsByName.TryGetValue(NormalizeName(proto.name), out spec) ||
                themeSpecsByName.TryGetValue(NormalizeName(proto.DisplayName), out spec) ||
                themeSpecsByName.TryGetValue(NormalizeName(proto.displayName), out spec))
            {
                return true;
            }
            spec = null;
            return false;
        }

        static bool TryGetRegisteredThemeSpec(int themeId, out ThemeSpec spec)
        {
            EnsureRegistered();
            if (themeSpecsById.TryGetValue(themeId, out spec))
            {
                return true;
            }
            ThemeProto proto = LDB.themes.Select(themeId);
            if (proto != null)
            {
                return TryGetRegisteredThemeSpec(proto, out spec);
            }
            spec = null;
            return false;
        }

        static ThemeProto BuildThemeProto(ThemeSpec spec)
        {
            ThemeProto source = RequireTheme(spec.BaseThemeId);

            EnsureThemePreloaded(source);

            ThemeProto proto = new ThemeProto
            {
                name = spec.Name,
                Name = spec.Name,
                sid = "",
                SID = "",
                PlanetType = spec.PlanetType ?? source.PlanetType,
                DisplayName = spec.DisplayName,
                displayName = spec.DisplayName,
                Algos = new[] { spec.Algo ?? FirstAlgo(source) },
                MaterialPath = source.MaterialPath,
                Temperature = spec.Temperature ?? source.Temperature,
                Distribute = source.Distribute,
                ModX = source.ModX,
                ModY = source.ModY,
                Vegetables0 = spec.Vegetables0 ?? Clone(source.Vegetables0),
                Vegetables1 = spec.Vegetables1 ?? Clone(source.Vegetables1),
                Vegetables2 = spec.Vegetables2 ?? Clone(source.Vegetables2),
                Vegetables3 = spec.Vegetables3 ?? Clone(source.Vegetables3),
                Vegetables4 = spec.Vegetables4 ?? Clone(source.Vegetables4),
                Vegetables5 = spec.Vegetables5 ?? Clone(source.Vegetables5),
                VeinSpot = spec.VeinSpot ?? Clone(source.VeinSpot),
                VeinCount = spec.VeinCount ?? Clone(source.VeinCount),
                VeinOpacity = spec.VeinOpacity ?? Clone(source.VeinOpacity),
                RareVeins = spec.RareVeins ?? Clone(source.RareVeins),
                RareSettings = spec.RareSettings ?? Clone(source.RareSettings),
                GasItems = spec.GasItems ?? Clone(source.GasItems),
                GasSpeeds = spec.GasSpeeds ?? Clone(source.GasSpeeds),
                UseHeightForBuild = source.UseHeightForBuild,
                Wind = spec.Wind ?? source.Wind,
                IonHeight = spec.IonHeight ?? source.IonHeight,
                WaterHeight = spec.WaterHeight ?? source.WaterHeight,
                WaterItemId = spec.WaterItemId ?? source.WaterItemId,
                IceFlag = source.IceFlag,
                Musics = Clone(source.Musics),
                SFXPath = spec.SFXPath ?? source.SFXPath,
                SFXVolume = source.SFXVolume,
                CullingRadius = source.CullingRadius
            };

            ApplySpecVeinArrays(proto, spec);

            CopyOptionalField(source, proto, "BriefIntroduction", $"{spec.DisplayName};{spec.DisplayName}");
            CopyOptionalField(source, proto, "EigenBit", null);

            CloneOptionalObjectArrayField(source, proto, "uiIconSprite");
            CloneOptionalObjectArrayField(source, proto, "uiScifiDecoTex");

            ApplySpecAssets(proto, spec);
            return proto;
        }

        static void ApplySpecVeinArrays(ThemeProto proto, ThemeSpec spec)
        {
            if (proto == null || spec == null || spec.VeinSettings == null || spec.VeinSettings.VeinTypes == null)
            {
                return;
            }

            int veinArrayLength = PlanetModelingManager.veinProtos != null ? PlanetModelingManager.veinProtos.Length : 15;
            proto.VeinSpot = new int[veinArrayLength];
            proto.VeinOpacity = new float[veinArrayLength];
            proto.VeinCount = new float[veinArrayLength];
            List<int> rareVeins = new List<int>();
            List<float> rareSettings = new List<float>();

            foreach (ThemeVeinType veinType in spec.VeinSettings.VeinTypes)
            {
                if (veinType == null || veinType.Veins == null || veinType.Veins.Count == 0)
                {
                    continue;
                }

                int typeIndex = (int)veinType.Type;
                if (typeIndex <= 0 || typeIndex >= veinArrayLength)
                {
                    continue;
                }

                int veinGroupCount = veinType.Veins.Count;
                float totalCount = 0f;
                float totalRichness = 0f;
                foreach (ThemeVein vein in veinType.Veins)
                {
                    totalCount += vein.Count;
                    totalRichness += vein.Richness;
                }

                if (veinType.Rare)
                {
                    rareVeins.Add(typeIndex);
                    rareSettings.Add(0f);
                    rareSettings.Add(1f);
                    rareSettings.Add(veinGroupCount / 25f);
                    rareSettings.Add(totalRichness / veinGroupCount);
                }
                else
                {
                    proto.VeinSpot[typeIndex - 1] = veinGroupCount;
                    proto.VeinCount[typeIndex - 1] = totalCount / 25f / veinGroupCount;
                    proto.VeinOpacity[typeIndex - 1] = totalRichness / veinGroupCount;
                }
            }

            proto.RareVeins = rareVeins.ToArray();
            proto.RareSettings = rareSettings.ToArray();
        }

        static void ApplySpecAssets(ThemeProto proto, ThemeSpec spec)
        {
            ThemeProto source = RequireTheme(spec.BaseThemeId);
            ThemeProto terrainSource = SelectThemeOrDefault(spec.TerrainThemeId, source);
            ThemeProto oceanSource = SelectThemeOrDefault(spec.OceanThemeId, source);
            ThemeProto atmosphereSource = SelectThemeOrDefault(spec.AtmosphereThemeId, source);
            ThemeProto ambientSource = SelectThemeOrDefault(spec.AmbientThemeId, source);

            EnsureThemePreloaded(source);
            EnsureThemePreloaded(terrainSource);
            EnsureThemePreloaded(oceanSource);
            EnsureThemePreloaded(atmosphereSource);
            EnsureThemePreloaded(ambientSource);

            proto.terrainMat = CloneThemeMaterials(terrainSource.terrainMat, spec);
            proto.oceanMat = CloneThemeMaterials(oceanSource.oceanMat, spec);
            proto.atmosMat = CloneThemeMaterials(atmosphereSource.atmosMat, spec);
            proto.nephogramMat = CloneThemeMaterials(atmosphereSource.nephogramMat, spec);
            proto.cloudMat = CloneThemeMaterials(atmosphereSource.cloudMat, spec);
            proto.lowMat = CloneThemeMaterials(terrainSource.lowMat, spec);
            proto.thumbMat = CloneThemeMaterials(source.thumbMat, spec);
            proto.minimapMat = CloneThemeMaterials(source.minimapMat, spec);
            proto.ambientDesc = CloneThemeObjects(ambientSource.ambientDesc, spec);
            proto.ambientSfx = CloneThemeObjects(source.ambientSfx, spec);

            EnsureUsableMaterials(proto, source);
            EnsureThemeText(proto, spec);
            ApplyAmbientOverrides(proto.ambientDesc, spec);

            ApplyTint(proto.terrainMat, spec.TerrainTint, MaterialRole.Terrain);
            ApplyTint(proto.oceanMat, spec.OceanTint, MaterialRole.Ocean);
            ApplyTint(proto.atmosMat, spec.AtmosphereTint, MaterialRole.Atmosphere);
            ApplyTint(proto.thumbMat, spec.TerrainTint, MaterialRole.Thumb);
            ApplyTint(proto.minimapMat, spec.TerrainTint, MaterialRole.Minimap);
            ApplyColors(proto.terrainMat, spec.TerrainColors);
            ApplyColors(proto.oceanMat, spec.OceanColors);
            ApplyColors(proto.atmosMat, spec.AtmosphereColors);
            ApplyParams(proto.terrainMat, spec.TerrainParams);
            ApplyParams(proto.oceanMat, spec.OceanParams);
            ApplyParams(proto.atmosMat, spec.AtmosphereParams);
        }

        static void ApplyThemeProtoToPlanetMaterials(PlanetData planet, ThemeProto proto, ThemeSpec spec)
        {
            int style = Math.Abs(planet.style);
            planet.terrainMaterial = CopyStyleMaterial(proto.terrainMat, style, planet.terrainMaterial, planet.displayName + " Terrain");
            planet.oceanMaterial = CopyStyleMaterial(proto.oceanMat, style, planet.oceanMaterial, planet.displayName + " Ocean");
            planet.atmosMaterial = CopyStyleMaterial(proto.atmosMat, style, planet.atmosMaterial, planet.displayName + " Atmos");
            planet.atmosMaterialLate = CopyStyleMaterial(proto.atmosMat, style, planet.atmosMaterialLate, planet.displayName + " Atmos Late");
            planet.nephogramMaterial = CopyStyleMaterial(proto.nephogramMat, style, planet.nephogramMaterial, planet.displayName + " Nephogram");
            planet.cloudMaterial = CopyStyleMaterial(proto.cloudMat, style, planet.cloudMaterial, planet.displayName + " Cloud");
            planet.minimapMaterial = CopyStyleMaterial(proto.minimapMat, style, planet.minimapMaterial, planet.displayName + " Minimap");

            if (HasUsable(proto.ambientDesc))
            {
                AmbientDesc ambient = proto.ambientDesc[style % proto.ambientDesc.Length];
                if (ambient != null)
                {
                    planet.ambientDesc = Object.Instantiate(ambient);
                    ApplyAmbientOverrides(new[] { planet.ambientDesc }, spec);
                }
            }
            if (HasUsable(proto.ambientSfx))
            {
                planet.ambientSfx = proto.ambientSfx[style % proto.ambientSfx.Length];
            }

            ApplyPlanetMaterialRuntimeValues(planet);
            LogMaterialDiagnosticsOnce(planet, spec);
        }

        static Material CopyStyleMaterial(Material[] materials, int style, Material current, string name)
        {
            if (!HasUsable(materials))
            {
                return current;
            }
            Material source = materials[Math.Abs(style) % materials.Length];
            if (source == null)
            {
                source = FirstUsable(materials);
            }
            if (source == null)
            {
                return current;
            }

            if (current == null)
            {
                Material created = Object.Instantiate(source);
                created.name = name;
                return created;
            }

            current.shader = source.shader;
            current.CopyPropertiesFromMaterial(source);
            current.renderQueue = source.renderQueue;
            current.name = name;
            return current;
        }

        static void ApplyPlanetMaterialRuntimeValues(PlanetData planet)
        {
            if (planet == null)
            {
                return;
            }

            SetRadius(planet.terrainMaterial, planet.realRadius);
            SetRadius(planet.oceanMaterial, planet.realRadius);
            SetRadius(planet.minimapMaterial, planet.realRadius);

            if (planet.minimapMaterial != null && planet.heightmap != null && planet.minimapMaterial.HasProperty("_HeightMap"))
            {
                planet.minimapMaterial.SetTexture("_HeightMap", planet.heightmap);
            }

            if (planet.terrainMaterial != null && planet.terrainMaterial.HasProperty("_LightColorScreen"))
            {
                planet.groundScreenColor = planet.terrainMaterial.GetColor("_LightColorScreen");
            }
        }

        static void SetRadius(Material material, float radius)
        {
            if (material != null && material.HasProperty("_Radius"))
            {
                material.SetFloat("_Radius", radius);
            }
        }

        static float GetPlanetScaleFactor(PlanetData planet)
        {
            if (planet == null)
            {
                return 1f;
            }
            return planet.type == EPlanetType.Gas
                ? Mathf.Max(planet.radius / 80f, 0.0001f)
                : Mathf.Max(planet.radius / 200f, 0.0001f);
        }

        static void RefreshAtmosphereMaterial(
            Material material,
            Vector3 planetPos,
            Vector3 sunDir,
            Vector4 planetRadius,
            Color sunAtmosColor,
            Color sunriseAtmosColor,
            Vector4 localPos,
            float scatterPower,
            float intensityControl,
            float distanceControl
        )
        {
            if (material == null)
            {
                return;
            }

            SetVectorIfExists(material, "_PlanetPos", planetPos);
            SetVectorIfExists(material, "_SunDir", sunDir);
            SetVectorIfExists(material, "_PlanetRadius", planetRadius);
            SetColorIfExists(material, "_Color4", sunAtmosColor);
            SetColorIfExists(material, "_Sky4", sunriseAtmosColor);
            SetVectorIfExists(material, "_LocalPos", localPos);
            SetFloatIfExists(material, "_SunRiseScatterPower", scatterPower);
            SetFloatIfExists(material, "_IntensityControl", intensityControl);
            SetFloatIfExists(material, "_DistanceControl", distanceControl);
        }

        static void SetVectorIfExists(Material material, string property, Vector4 value)
        {
            if (material.HasProperty(property))
            {
                material.SetVector(property, value);
            }
        }

        static void SetColorIfExists(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        static void SetFloatIfExists(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        static void SetIntIfExists(Material material, string property, int value)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetInt(property, value);
            }
        }

        static void SyncPlanetSimulatorMaterials(PlanetData planet)
        {
            if (planet == null)
            {
                return;
            }

            PlanetSimulator simulator = null;
            try
            {
                simulator = GameMain.universeSimulator?.FindPlanetSimulator(planet);
            }
            catch
            {
                simulator = null;
            }
            if (simulator == null && planet.gameObject != null)
            {
                simulator = planet.gameObject.GetComponent<PlanetSimulator>();
            }
            if (simulator == null)
            {
                return;
            }

            if (simulator.surfaceRenderer != null)
            {
                foreach (Renderer renderer in simulator.surfaceRenderer)
                {
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = planet.terrainMaterial;
                    }
                }
            }

            if (simulator.oceanCollider != null)
            {
                Renderer oceanRenderer = simulator.oceanCollider.GetComponent<Renderer>();
                if (oceanRenderer != null)
                {
                    oceanRenderer.enabled = planet.oceanMaterial != null;
                    oceanRenderer.sharedMaterial = planet.oceanMaterial;
                }
            }
            simulator.oceanMat = planet.oceanMaterial;
            if (simulator.oceanMat != null)
            {
                simulator.oceanRenderQueue = simulator.oceanMat.renderQueue;
            }

            simulator.atmoMat = planet.atmosMaterial;
            simulator.atmoMatLate = planet.atmosMaterialLate;
            if (simulator.atmoTrans1 != null)
            {
                Renderer atmosphereRenderer = simulator.atmoTrans1.GetComponent<Renderer>();
                if (atmosphereRenderer != null && simulator.atmoMat != null && simulator.atmoMatLate != null)
                {
                    atmosphereRenderer.sharedMaterials = new[] { simulator.atmoMat, simulator.atmoMatLate };
                }
            }
            if (simulator.atmoMat != null)
            {
                simulator.atmoMatRadiusParam = simulator.atmoMat.GetVector("_PlanetRadius");
            }

            CopyTerrainLightingToReform(planet, simulator);
        }

        static void CopyTerrainLightingToReform(PlanetData planet, PlanetSimulator simulator)
        {
            if (planet == null || simulator == null || planet.type == EPlanetType.Gas || planet.terrainMaterial == null)
            {
                return;
            }

            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat0, "_AmbientColor0");
            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat0, "_AmbientColor1");
            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat0, "_AmbientColor2");
            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat0, "_LightColorScreen");
            CopyMaterialFloat(planet.terrainMaterial, simulator.reformMat0, "_Multiplier");
            CopyMaterialFloat(planet.terrainMaterial, simulator.reformMat0, "_AmbientInc");

            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat1, "_AmbientColor0");
            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat1, "_AmbientColor1");
            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat1, "_AmbientColor2");
            CopyMaterialColor(planet.terrainMaterial, simulator.reformMat1, "_LightColorScreen");
            CopyMaterialFloat(planet.terrainMaterial, simulator.reformMat1, "_Multiplier");
            CopyMaterialFloat(planet.terrainMaterial, simulator.reformMat1, "_AmbientInc");
        }

        static void CopyMaterialColor(Material source, Material target, string property)
        {
            if (source != null && target != null && source.HasProperty(property) && target.HasProperty(property))
            {
                target.SetColor(property, source.GetColor(property));
            }
        }

        static void CopyMaterialFloat(Material source, Material target, string property)
        {
            if (source != null && target != null && source.HasProperty(property) && target.HasProperty(property))
            {
                target.SetFloat(property, source.GetFloat(property));
            }
        }

        static ThemeProto RequireTheme(int id)
        {
            ThemeProto theme = LDB.themes.Select(id);
            if (theme == null)
            {
                throw new Exception($"Theme #{id} is missing from game data");
            }
            return theme;
        }

        static ThemeProto SelectThemeOrDefault(int? id, ThemeProto defaultTheme)
        {
            return id.HasValue ? RequireTheme(id.Value) : defaultTheme;
        }

        static int FirstAlgo(ThemeProto theme)
        {
            return theme.Algos != null && theme.Algos.Length > 0 ? theme.Algos[0] : 1;
        }

        static void EnsureThemePreloaded(ThemeProto theme)
        {
            if (theme == null)
            {
                return;
            }
            if (HasUsable(theme.terrainMat) &&
                HasUsable(theme.nephogramMat) &&
                HasUsable(theme.cloudMat) &&
                HasUsable(theme.ambientDesc))
            {
                return;
            }
            try
            {
                theme.Preload();
            }
            catch (Exception e)
            {
                Plugin.Instance.Logger.LogWarning($"Failed to preload theme {theme.Name}: {e.Message}");
            }
        }

        static void EnsureUsableMaterials(ThemeProto proto, ThemeProto fallback)
        {
            proto.terrainMat = EnsureMaterials(proto.terrainMat, fallback.terrainMat);
            proto.oceanMat = EnsureMaterials(proto.oceanMat, fallback.oceanMat);
            proto.atmosMat = EnsureMaterials(proto.atmosMat, fallback.atmosMat);
            proto.nephogramMat = EnsureMaterials(proto.nephogramMat, fallback.nephogramMat);
            proto.cloudMat = EnsureMaterials(proto.cloudMat, fallback.cloudMat);
            proto.lowMat = EnsureMaterials(proto.lowMat, fallback.lowMat);
            proto.thumbMat = EnsureMaterials(proto.thumbMat, fallback.thumbMat);
            proto.minimapMat = EnsureMaterials(proto.minimapMat, fallback.minimapMat);
            proto.ambientDesc = EnsureObjects(proto.ambientDesc, fallback.ambientDesc, CreateDefaultAmbientDesc);
            proto.ambientSfx = EnsureObjects(proto.ambientSfx, fallback.ambientSfx, null);
        }

        static Material[] EnsureMaterials(Material[] materials, Material[] fallback)
        {
            if (HasUsable(materials))
            {
                return FillMissingMaterials(CloneMaterials(materials), fallback);
            }
            Material[] clonedFallback = CloneMaterials(fallback);
            if (HasUsable(clonedFallback))
            {
                return FillMissingMaterials(clonedFallback, fallback);
            }
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            return new[] { new Material(shader) };
        }

        static Material[] FillMissingMaterials(Material[] materials, Material[] fallback)
        {
            if (materials == null)
            {
                return null;
            }

            Material defaultMaterial = FirstUsable(fallback);
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    continue;
                }
                if (defaultMaterial != null)
                {
                    materials[i] = Object.Instantiate(defaultMaterial);
                }
                else
                {
                    Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    materials[i] = new Material(shader);
                }
            }
            return materials;
        }

        static T[] EnsureObjects<T>(T[] objects, T[] fallback, Func<T> createDefault) where T : Object
        {
            T[] result = CloneUsableObjects(objects);
            if (HasUsable(result))
            {
                return FillMissingObjects(result, fallback, createDefault);
            }

            result = CloneUsableObjects(fallback);
            if (HasUsable(result))
            {
                return FillMissingObjects(result, fallback, createDefault);
            }

            return createDefault == null ? null : new[] { createDefault() };
        }

        static T[] FillMissingObjects<T>(T[] objects, T[] fallback, Func<T> createDefault) where T : Object
        {
            if (objects == null)
            {
                return null;
            }

            T defaultObject = FirstUsable(fallback);
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    continue;
                }
                if (defaultObject != null)
                {
                    objects[i] = Object.Instantiate(defaultObject);
                }
                else if (createDefault != null)
                {
                    objects[i] = createDefault();
                }
            }
            return objects;
        }

        static T[] CloneUsableObjects<T>(T[] source) where T : Object
        {
            if (source == null)
            {
                return null;
            }

            T[] result = new T[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i] == null ? null : Object.Instantiate(source[i]);
            }
            return result;
        }

        static bool HasUsable<T>(T[] objects) where T : Object
        {
            return objects != null && objects.Any(item => item != null);
        }

        static T FirstUsable<T>(T[] objects) where T : Object
        {
            return objects == null ? null : objects.FirstOrDefault(item => item != null);
        }

        static AmbientDesc CreateDefaultAmbientDesc()
        {
            ThemeProto fallback = LDB.themes.Select(OCEAN_WORLD) ?? LDB.themes.Select(MEDITERRANEAN);
            EnsureThemePreloaded(fallback);
            AmbientDesc source = FirstUsable(fallback?.ambientDesc);
            if (source != null)
            {
                return Object.Instantiate(source);
            }

            GameObject holder = new GameObject("DSPAddPlanet Default AmbientDesc");
            Object.DontDestroyOnLoad(holder);
            holder.hideFlags = HideFlags.HideAndDontSave;
            AmbientDesc ambient = holder.AddComponent<AmbientDesc>();
            ambient.ambientColor0 = new Color(0.22f, 0.24f, 0.28f, 1f);
            ambient.ambientColor1 = new Color(0.12f, 0.14f, 0.18f, 1f);
            ambient.ambientColor2 = new Color(0.04f, 0.05f, 0.07f, 1f);
            ambient.waterAmbientColor0 = ambient.ambientColor0;
            ambient.waterAmbientColor1 = ambient.ambientColor1;
            ambient.waterAmbientColor2 = ambient.ambientColor2;
            return ambient;
        }

        static void EnsureThemeText(ThemeProto proto, ThemeSpec spec)
        {
            if (string.IsNullOrWhiteSpace(proto.displayName))
            {
                proto.displayName = spec.DisplayName;
            }
            if (string.IsNullOrWhiteSpace(proto.DisplayName))
            {
                proto.DisplayName = spec.DisplayName;
            }
            FieldInfo briefField = typeof(ThemeProto).GetField("BriefIntroduction");
            if (briefField != null && string.IsNullOrWhiteSpace(briefField.GetValue(proto) as string))
            {
                briefField.SetValue(proto, $"{spec.DisplayName};{spec.DisplayName}");
            }
        }

        static void CopyOptionalField(ThemeProto source, ThemeProto target, string fieldName, object defaultValue)
        {
            FieldInfo field = typeof(ThemeProto).GetField(fieldName);
            if (field == null)
            {
                return;
            }
            object value = field.GetValue(source) ?? defaultValue;
            field.SetValue(target, value);
        }

        static void CloneOptionalObjectArrayField(ThemeProto source, ThemeProto target, string fieldName)
        {
            FieldInfo field = typeof(ThemeProto).GetField(fieldName);
            if (field == null)
            {
                return;
            }
            Array sourceArray = field.GetValue(source) as Array;
            if (sourceArray == null)
            {
                return;
            }

            Array targetArray = Array.CreateInstance(field.FieldType.GetElementType(), sourceArray.Length);
            for (int i = 0; i < sourceArray.Length; i++)
            {
                Object item = sourceArray.GetValue(i) as Object;
                targetArray.SetValue(item == null ? null : Object.Instantiate(item), i);
            }
            field.SetValue(target, targetArray);
        }

        static Material[] CloneMaterials(Material[] source)
        {
            if (source == null)
            {
                return null;
            }
            Material[] result = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i] == null ? null : Object.Instantiate(source[i]);
            }
            return result;
        }

        static Material[] CloneThemeMaterials(Material[] source, ThemeSpec spec)
        {
            if (!spec.UseSingleMaterial || source == null || source.Length <= 1)
            {
                return CloneMaterials(source);
            }

            Material first = FirstUsable(source);
            return first == null ? null : new[] { Object.Instantiate(first) };
        }

        static T[] CloneThemeObjects<T>(T[] source, ThemeSpec spec) where T : Object
        {
            if (!spec.UseSingleMaterial || source == null || source.Length <= 1)
            {
                return CloneObjects(source);
            }

            T first = FirstUsable(source);
            return first == null ? null : new[] { Object.Instantiate(first) };
        }

        static T[] CloneObjects<T>(T[] source) where T : Object
        {
            if (source == null)
            {
                return null;
            }
            T[] result = new T[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i] == null ? null : Object.Instantiate(source[i]);
            }
            return result;
        }

        static int[] Clone(int[] source)
        {
            return source == null ? null : (int[])source.Clone();
        }

        static float[] Clone(float[] source)
        {
            return source == null ? null : (float[])source.Clone();
        }

        static Dictionary<string, Color> CloneColorDictionary(Dictionary<string, Color> source)
        {
            return source == null
                ? new Dictionary<string, Color>()
                : new Dictionary<string, Color>(source);
        }

        static void ApplyGsTint(Dictionary<string, Color> colors, string property, Color tint)
        {
            if (colors == null || !colors.TryGetValue(property, out Color original))
            {
                return;
            }
            float gray = original.grayscale;
            Color grayColor = new Color(gray, gray, gray, original.a);
            Color target = new Color(tint.r, tint.g, tint.b, original.a);
            colors[property] = Color.Lerp(grayColor, target, Mathf.Clamp01(tint.a));
        }

        static void ApplyTint(Material[] materials, Color? tint, MaterialRole role)
        {
            if (!tint.HasValue || IsClear(tint.Value) || materials == null)
            {
                return;
            }
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }
                switch (role)
                {
                    case MaterialRole.Terrain:
                        BlendColor(material, "_Color", tint.Value);
                        BlendColor(material, "_AmbientColor0", tint.Value);
                        BlendColor(material, "_AmbientColor1", tint.Value);
                        BlendColor(material, "_AmbientColor2", tint.Value);
                        BlendColor(material, "_LightColorScreen", tint.Value);
                        BlendColor(material, "_HeightEmissionColor", tint.Value);
                        BlendColor(material, "_SpeclColor", tint.Value);
                        if (material.HasProperty("_EmissionStrength"))
                        {
                            material.SetFloat("_EmissionStrength", 0f);
                        }
                        break;
                    case MaterialRole.Ocean:
                        BlendColor(material, "_Color", tint.Value);
                        BlendColor(material, "_Color0", tint.Value);
                        BlendColor(material, "_Color1", tint.Value);
                        BlendColor(material, "_Color2", tint.Value);
                        BlendColor(material, "_Color3", tint.Value);
                        BlendColor(material, "_FoamColor", tint.Value);
                        BlendColor(material, "_FresnelColor", tint.Value);
                        BlendColor(material, "_CausticsColor", tint.Value);
                        BlendColor(material, "_SpeclColor", tint.Value);
                        BlendColor(material, "_SpeclColor1", tint.Value);
                        BlendColor(material, "_ReflectionColor", tint.Value);
                        break;
                    case MaterialRole.Atmosphere:
                        BlendColor(material, "_CausticsColor", tint.Value);
                        BlendColor(material, "_Color", tint.Value);
                        BlendColor(material, "_Color0", tint.Value);
                        BlendColor(material, "_Color1", tint.Value);
                        BlendColor(material, "_Color2", tint.Value);
                        BlendColor(material, "_Color3", tint.Value);
                        BlendColor(material, "_Color4", tint.Value);
                        BlendColor(material, "_Color5", tint.Value);
                        BlendColor(material, "_Color6", tint.Value);
                        BlendColor(material, "_Color7", tint.Value);
                        BlendColor(material, "_Color8", tint.Value);
                        BlendColor(material, "_ColorF", tint.Value);
                        BlendColor(material, "_Sky0", tint.Value);
                        BlendColor(material, "_Sky1", tint.Value);
                        BlendColor(material, "_Sky2", tint.Value);
                        BlendColor(material, "_Sky3", tint.Value);
                        BlendColor(material, "_Sky4", tint.Value);
                        BlendColor(material, "_EmissionColor", tint.Value);
                        break;
                    case MaterialRole.Thumb:
                    case MaterialRole.Minimap:
                        BlendColor(material, "_Color", tint.Value);
                        material.color = Color.Lerp(material.color, tint.Value, Mathf.Clamp01(tint.Value.a));
                        break;
                }
            }
        }

        static void BlendColor(Material material, string property, Color tint)
        {
            if (!material.HasProperty(property))
            {
                return;
            }
            Color original = material.GetColor(property);
            float gray = original.grayscale;
            Color grayColor = new Color(gray, gray, gray, original.a);
            Color target = new Color(tint.r, tint.g, tint.b, original.a);
            material.SetColor(property, Color.Lerp(grayColor, target, Mathf.Clamp01(tint.a)));
        }

        static bool IsClear(Color color)
        {
            return Mathf.Approximately(color.r, 0f) &&
                   Mathf.Approximately(color.g, 0f) &&
                   Mathf.Approximately(color.b, 0f) &&
                   Mathf.Approximately(color.a, 0f);
        }

        static void ApplyParams(Material[] materials, Dictionary<string, float> parameters)
        {
            if (materials == null || parameters == null)
            {
                return;
            }
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }
                foreach (KeyValuePair<string, float> pair in parameters)
                {
                    if (material.HasProperty(pair.Key))
                    {
                        material.SetFloat(pair.Key, pair.Value);
                    }
                }
            }
        }

        static void ApplyColors(Material[] materials, Dictionary<string, Color> colors)
        {
            if (materials == null || colors == null)
            {
                return;
            }
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }
                foreach (KeyValuePair<string, Color> pair in colors)
                {
                    if (material.HasProperty(pair.Key))
                    {
                        material.SetColor(pair.Key, pair.Value);
                    }
                }
            }
        }

        static void ApplyAmbientOverrides(AmbientDesc[] ambientDescs, ThemeSpec spec)
        {
            if (ambientDescs == null)
            {
                return;
            }
            foreach (AmbientDesc ambientDesc in ambientDescs)
            {
                if (ambientDesc == null)
                {
                    continue;
                }
                if (spec.AmbientLutContribution.HasValue)
                {
                    ambientDesc.lutContribution = spec.AmbientLutContribution.Value;
                }
                if (spec.AmbientReflectionColor.HasValue && ambientDesc.reflectionMap != null)
                {
                    ambientDesc.customColor0 = spec.AmbientReflectionColor.Value;
                }
                spec.AmbientOverride?.Invoke(ambientDesc);
            }
        }

        static void LogMaterialDiagnosticsOnce(PlanetData planet, ThemeSpec spec)
        {
            if (planet == null || spec == null || !materialDiagnosticLogged.Add(planet.theme))
            {
                return;
            }

            Material material = planet.terrainMaterial;
            string materialName = material == null ? "<null>" : material.name;
            string shaderName = material == null || material.shader == null ? "<null>" : material.shader.name;
            Plugin.Instance.Logger.LogInfo(
                $"GalacticScale-style terrain material for theme {planet.theme} {spec.Name}: " +
                $"material={materialName}, shader={shaderName}, " +
                $"_Color={ReadColor(material, "_Color")}, " +
                $"_AmbientColor0={ReadColor(material, "_AmbientColor0")}, " +
                $"_LightColorScreen={ReadColor(material, "_LightColorScreen")}, " +
                $"_HeightEmissionColor={ReadColor(material, "_HeightEmissionColor")}, " +
                $"_Multiplier={ReadFloat(material, "_Multiplier")}, " +
                $"_AmbientInc={ReadFloat(material, "_AmbientInc")}, " +
                $"_HeightEmissionRadius={ReadFloat(material, "_HeightEmissionRadius")}"
            );
        }

        static string ReadColor(Material material, string property)
        {
            if (material == null || !material.HasProperty(property))
            {
                return "n/a";
            }
            Color color = material.GetColor(property);
            return $"({color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3})";
        }

        static string ReadFloat(Material material, string property)
        {
            if (material == null || !material.HasProperty(property))
            {
                return "n/a";
            }
            return material.GetFloat(property).ToString("F3");
        }

        static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "";
            }
            char[] chars = name
                .Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_' && c != '.')
                .Select(char.ToLowerInvariant)
                .ToArray();
            return new string(chars);
        }

        enum MaterialRole
        {
            Terrain,
            Ocean,
            Atmosphere,
            Thumb,
            Minimap
        }

        class ThemeSpec
        {
            public string Name;
            public string DisplayName;
            public int BaseThemeId;
            public int? TerrainThemeId;
            public int? OceanThemeId;
            public int? AtmosphereThemeId;
            public int? AmbientThemeId;
            public int? Algo;
            public EPlanetType? PlanetType;
            public float? Temperature;
            public int[] Vegetables0;
            public int[] Vegetables1;
            public int[] Vegetables2;
            public int[] Vegetables3;
            public int[] Vegetables4;
            public int[] Vegetables5;
            public int[] VeinSpot;
            public float[] VeinCount;
            public float[] VeinOpacity;
            public int[] RareVeins;
            public float[] RareSettings;
            public int[] GasItems;
            public float[] GasSpeeds;
            public float? Wind;
            public float? IonHeight;
            public float? WaterHeight;
            public int? WaterItemId;
            public string SFXPath;
            public bool IncludeInRandomGeneration = true;
            public EThemeDistribute? RandomDistribute;
            public int RandomWeight = 1;
            public int[] ExtraRandomThemeIds;
            public Color? TerrainTint;
            public Color? OceanTint;
            public Color? AtmosphereTint;
            public float? AmbientLutContribution;
            public Color? AmbientReflectionColor;
            public Action<AmbientDesc> AmbientOverride;
            public Dictionary<string, Color> TerrainColors;
            public Dictionary<string, Color> OceanColors;
            public Dictionary<string, Color> AtmosphereColors;
            public Dictionary<string, float> TerrainParams;
            public Dictionary<string, float> OceanParams;
            public Dictionary<string, float> AtmosphereParams;
            public ThemeTerrainSettings TerrainSettings;
            public ThemeVeinSettings VeinSettings;
            public bool UseSingleMaterial = true;

            public ThemeSpec Clone()
            {
                return (ThemeSpec)MemberwiseClone();
            }
        }

        class ThemeTerrainSettings
        {
            public string Algorithm = "Vanilla";
            public double BaseHeight = 0.0;
            public double BiomeHeightModifier = 0.0;
            public double BiomeHeightMulti = 1.0;
            public double HeightMulti = 1.0;
            public double LandModifier = 0.0;
            public double RandomFactor = 1.0;
            public double xFactor = 0.0;
            public double yFactor = 0.0;
            public double zFactor = 0.0;
        }

        class ThemeVeinSettings
        {
            public string Algorithm = "Vanilla";
            public float VeinPadding = 1f;
            public List<ThemeVeinType> VeinTypes = new List<ThemeVeinType>();
        }

        class ThemeVeinType
        {
            public EVeinType Type;
            public bool Rare;
            public List<ThemeVein> Veins = new List<ThemeVein>();

            public static ThemeVeinType Generate(EVeinType type, int min, int max, float minRichness, float maxRichness, int minPatchSize, int maxPatchSize, bool rare, int seed)
            {
                DotNet35Random random = new DotNet35Random(seed);
                ThemeVeinType veinType = new ThemeVeinType
                {
                    Type = type,
                    Rare = rare
                };
                int amount = Mathf.RoundToInt(Mathf.Clamp(random.Next(min, max + 1), 0, 3000));
                for (int i = 0; i < amount; i++)
                {
                    veinType.Veins.Add(new ThemeVein
                    {
                        Count = random.Next(minPatchSize, maxPatchSize + 1),
                        Richness = Mathf.Lerp(minRichness, maxRichness, (float)random.NextDouble())
                    });
                }
                return veinType;
            }
        }

        class ThemeVein
        {
            public int Count;
            public float Richness;
        }

        struct VeinDescriptor
        {
            public int Count;
            public EVeinType Type;
            public Vector3 Position;
            public bool Rare;
            public float Richness;
        }

        class RegisteredThemeInfo
        {
            public int Id;
            public bool IncludeInRandomGeneration;
            public int RandomWeight;
            public EThemeDistribute RandomDistribute;
            public int[] ExtraRandomThemeIds;
        }
    }
}
