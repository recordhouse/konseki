using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konseki
{
    public static class NightAlleyWorldBuilder
    {
        public sealed class BuildResult
        {
            public FirstPersonController Player { get; set; }
            public TargetWalker Target { get; set; }
            public AlleyVariant Variant { get; set; }
        }

        private sealed class Palette
        {
            public Material Asphalt;
            public Material Concrete;
            public Material BuildingDark;
            public Material BuildingMid;
            public Material BuildingWarm;
            public Material FacadeArt;
            public Material WindowWarm;
            public Material WindowCool;
            public Material AccentRed;
            public Material AccentBlue;
            public Material AccentCream;
            public Material Metal;
            public Material Puddle;
            public Material RoadMarking;
            public Material Tactile;
            public Material Coat;
            public Material Trousers;
            public Material Skin;
            public Material Shirt;
            public Color Fog;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public Color KeyLight;
            public Color WarmLight;
            public Color CoolLight;
            public Color UiAccent;
        }

        public static BuildResult Build(Transform root)
        {
            return Build(root, AlleyVariantInfo.Current, true);
        }

        public static BuildResult Build(Transform root, AlleyVariant variant, bool combineStaticGeometry)
        {
            Palette palette = CreatePalette(variant);
            ConfigureAtmosphere(palette, variant);
            CreateEnvironment(root, palette, variant, combineStaticGeometry);

            Vector3[] route = AlleyRouteCatalog.CreateTargetRoute();

            FirstPersonController player = CreatePlayer(root, palette, variant, new Vector3(0f, 0.05f, 2f));
            TargetWalker target = CreateTarget(root, palette, route);

            return new BuildResult
            {
                Player = player,
                Target = target,
                Variant = variant
            };
        }

        private static void ConfigureAtmosphere(Palette palette, AlleyVariant variant)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = palette.Fog;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = palette.AmbientSky;
            RenderSettings.ambientEquatorColor = palette.AmbientEquator;
            RenderSettings.ambientGroundColor = palette.AmbientGround;
            RenderSettings.ambientIntensity = 0.68f;
            Physics.gravity = new Vector3(0f, -20f, 0f);
            QualitySettings.shadowDistance = 32f;
        }

        private static Palette CreatePalette(AlleyVariant variant)
        {
            string texturePath;
            Color asphalt;
            Color concrete;
            Color dark;
            Color mid;
            Color warm;
            Color red;
            Color blue;
            Color cream;
            Color fog;
            Color sky;
            Color equator;
            Color ground;
            Color key;
            Color warmLight;
            Color coolLight;

            texturePath = "PixelArt/quiet-residential-wall";
            asphalt = Hex("#27343E");
            concrete = Hex("#4A575A");
            dark = Hex("#263239");
            mid = Hex("#53645F");
            warm = Hex("#766958");
            red = Hex("#A95E4B");
            blue = Hex("#55758A");
            cream = Hex("#E7C98C");
            fog = Hex("#111C29");
            sky = Hex("#304A65");
            equator = Hex("#243641");
            ground = Hex("#111820");
            key = Hex("#BCD4ED");
            warmLight = Hex("#FFD18A");
            coolLight = Hex("#83B7D4");

            Texture2D facadeTexture = Resources.Load<Texture2D>(texturePath);
            if (facadeTexture != null)
            {
                facadeTexture.filterMode = FilterMode.Bilinear;
                facadeTexture.wrapMode = TextureWrapMode.Clamp;
                facadeTexture.anisoLevel = 2;
            }

            Texture2D asphaltTexture = CreatePixelPatternTexture("Pixel asphalt", asphalt, Lighten(asphalt, 0.08f), 32, true);
            Texture2D concreteTexture = CreatePixelPatternTexture("Pixel concrete", concrete, Darken(concrete, 0.08f), 24, false);

            return new Palette
            {
                Asphalt = CreateTexturedMaterial("Pixel asphalt", asphalt, asphaltTexture, 0.42f, 0.05f, new Vector2(4f, 36f)),
                Concrete = CreateTexturedMaterial("Pixel concrete", concrete, concreteTexture, 0.2f, 0f, new Vector2(2f, 24f)),
                BuildingDark = CreateMaterial("Palette shadow wall", dark, 0.16f, 0f),
                BuildingMid = CreateMaterial("Palette middle wall", mid, 0.18f, 0f),
                BuildingWarm = CreateMaterial("Palette warm wall", warm, 0.18f, 0f),
                FacadeArt = CreateTexturedMaterial("Generated pixel facade", Hex("#FFFFFF"), facadeTexture, 0.12f, 0f, Vector2.one),
                WindowWarm = CreateEmissiveMaterial("Warm pixel windows", Darken(cream, 0.28f), warmLight * 0.65f),
                WindowCool = CreateEmissiveMaterial("Cool pixel windows", Darken(blue, 0.2f), coolLight * 0.65f),
                AccentRed = CreateEmissiveMaterial("Red pixel accent", red, red * 0.85f),
                AccentBlue = CreateEmissiveMaterial("Blue pixel accent", blue, coolLight * 0.8f),
                AccentCream = CreateEmissiveMaterial("Cream pixel accent", cream, warmLight * 0.75f),
                Metal = CreateMaterial("Ink metal", Darken(dark, 0.24f), 0.48f, 0.38f),
                Puddle = CreateMaterial("Pixel puddle", Darken(blue, 0.38f), 0.92f, 0.52f),
                RoadMarking = CreateMaterial("Modern road marking", Hex("#D8D9D2"), 0.32f, 0f),
                Tactile = CreateMaterial("Tactile paving", Hex("#D0B33F"), 0.2f, 0f),
                Coat = CreateMaterial("Target coat", Hex("#2A2D3D"), 0.12f, 0f),
                Trousers = CreateMaterial("Target trousers", Hex("#141722"), 0.1f, 0f),
                Skin = CreateMaterial("Skin pixels", Hex("#A66F54"), 0.12f, 0f),
                Shirt = CreateMaterial("Player jacket", Darken(blue, 0.26f), 0.12f, 0f),
                Fog = fog,
                AmbientSky = sky,
                AmbientEquator = equator,
                AmbientGround = ground,
                KeyLight = key,
                WarmLight = warmLight,
                CoolLight = coolLight,
                UiAccent = cream
            };
        }

        private static void CreateEnvironment(Transform root, Palette palette, AlleyVariant variant, bool combineStaticGeometry)
        {
            Transform environment = new GameObject($"{AlleyVariantInfo.DisplayName(variant)} Environment").transform;
            environment.SetParent(root);

            CreateCube("Modern wet main street", new Vector3(0f, -0.12f, 42f), new Vector3(7.5f, 0.24f, 94f), palette.Asphalt, environment, true);
            CreateSegmentedMainEdge(-1, new[] { 60.05f, 76.75f }, environment, palette);
            CreateSegmentedMainEdge(1, new[] { 26.65f, 51.7f }, environment, palette);
            CreateBranchNetwork(environment, palette);
            CreateModernRoadDetails(environment, palette);
            CreateDrainage(environment, palette);

            for (int i = 0; i < 11; i++)
            {
                float z = 1.6f + i * 8.35f;
                if (!IsMainJunctionOpening(-1, i)) CreateBuilding(-1, i, z, environment, palette, variant);
                if (!IsMainJunctionOpening(1, i)) CreateBuilding(1, i, z, environment, palette, variant);
            }

            CreateStreetLights(environment, palette, variant);
            CreateUtilityDetails(environment, palette);
            CreatePuddles(environment, palette, variant);
            CreateVariantSetDressing(environment, palette, variant);
            CreateDestination(environment, palette, variant);
            CreateMoonlight(environment, palette, variant);
            if (combineStaticGeometry)
            {
                CombineStaticGeometry(environment);
            }
        }

        private static bool IsMainJunctionOpening(int side, int index)
        {
            return side > 0 ? index == 3 || index == 6 : index == 7 || index == 9;
        }

        private static bool IsNearMainJunction(int side, float z, float clearance)
        {
            if (side > 0)
            {
                return Mathf.Abs(z - 26.65f) < clearance || Mathf.Abs(z - 51.7f) < clearance;
            }

            return Mathf.Abs(z - 60.05f) < clearance || Mathf.Abs(z - 76.75f) < clearance;
        }

        private static bool IsNearAnyJunction(float z, float clearance)
        {
            return IsNearMainJunction(1, z, clearance) || IsNearMainJunction(-1, z, clearance);
        }

        private static void CreateSegmentedMainEdge(int side, float[] openings, Transform parent, Palette palette)
        {
            CreateMainStripSegments("Raised modern curb", side * 4.15f, 0f, 0.82f, 0.22f, 3.55f, openings, palette.Concrete, parent, true);
            CreateMainStripSegments("Painted road edge", side * 3.28f, 0.018f, 0.08f, 0.02f, 3.25f, openings, palette.RoadMarking, parent, false);

            for (int i = 0; i < openings.Length; i++)
            {
                float z = openings[i];
                CreateCube("Tactile corner paving", new Vector3(side * 4.15f, 0.125f, z - 3.05f), new Vector3(0.68f, 0.03f, 0.52f), palette.Tactile, parent, false);
                CreateCube("Tactile corner paving", new Vector3(side * 4.15f, 0.125f, z + 3.05f), new Vector3(0.68f, 0.03f, 0.52f), palette.Tactile, parent, false);
            }
        }

        private static void CreateMainStripSegments(string name, float x, float y, float width, float height, float gapHalfWidth, float[] openings, Material material, Transform parent, bool keepCollider)
        {
            float cursor = -5f;
            const float roadEnd = 89f;

            for (int i = 0; i < openings.Length; i++)
            {
                float segmentEnd = openings[i] - gapHalfWidth;
                if (segmentEnd > cursor)
                {
                    CreateCube(name, new Vector3(x, y, (cursor + segmentEnd) * 0.5f), new Vector3(width, height, segmentEnd - cursor), material, parent, keepCollider);
                }

                cursor = openings[i] + gapHalfWidth;
            }

            if (cursor < roadEnd)
            {
                CreateCube(name, new Vector3(x, y, (cursor + roadEnd) * 0.5f), new Vector3(width, height, roadEnd - cursor), material, parent, keepCollider);
            }
        }

        private static void CreateBranchNetwork(Transform parent, Palette palette)
        {
            CreateBranchLoop(1, 26.65f, 51.7f, parent, palette);
            CreateBranchLoop(-1, 60.05f, 76.75f, parent, palette);
        }

        private static void CreateBranchLoop(int side, float startZ, float endZ, Transform parent, Palette palette)
        {
            const float outerLaneX = 13.2f;
            const float branchWidth = 6.4f;
            const float mainCurbX = 4.15f;
            const float innerCurbX = 9.6f;
            const float outerCurbX = 16.8f;
            float centerZ = (startZ + endZ) * 0.5f;
            float parallelLength = endZ - startZ + branchWidth;
            float connectorCenterX = side * 8.5f;

            CreateCube("Modern branch connector", new Vector3(connectorCenterX, -0.12f, startZ), new Vector3(10.7f, 0.24f, branchWidth), palette.Asphalt, parent, true);
            CreateCube("Modern branch connector", new Vector3(connectorCenterX, -0.12f, endZ), new Vector3(10.7f, 0.24f, branchWidth), palette.Asphalt, parent, true);
            CreateCube("Modern parallel alley", new Vector3(side * outerLaneX, -0.12f, centerZ), new Vector3(branchWidth, 0.24f, parallelLength), palette.Asphalt, parent, true);

            CreateCube("Outer branch curb", new Vector3(side * outerCurbX, 0f, centerZ), new Vector3(0.78f, 0.22f, parallelLength), palette.Concrete, parent, true);
            CreateCube("Outer branch sidewalk", new Vector3(side * 17.3f, 0.02f, centerZ), new Vector3(0.9f, 0.18f, parallelLength), palette.Concrete, parent, true);

            float innerLength = Mathf.Max(1f, endZ - startZ - 6.9f);
            CreateCube("Inner branch curb", new Vector3(side * innerCurbX, 0f, centerZ), new Vector3(0.78f, 0.22f, innerLength), palette.Concrete, parent, true);

            CreateHorizontalBranchCurb(side, startZ - 3.45f, mainCurbX, outerCurbX, parent, palette);
            CreateHorizontalBranchCurb(side, startZ + 3.45f, mainCurbX, innerCurbX, parent, palette);
            CreateHorizontalBranchCurb(side, endZ - 3.45f, mainCurbX, innerCurbX, parent, palette);
            CreateHorizontalBranchCurb(side, endZ + 3.45f, mainCurbX, outerCurbX, parent, palette);

            float blockLength = Mathf.Max(6.2f, endZ - startZ - 7.8f);
            Material innerWall = side > 0 ? palette.BuildingMid : palette.BuildingWarm;
            CreateModernBranchBlock("Inner modern block", side * 6.7f, centerZ, 4f, blockLength, 6.2f, side, innerWall, parent, palette);
            CreateModernBranchBlock("Outer modern block", side * 20.05f, centerZ, 5.2f, blockLength, 7.1f, -side, palette.BuildingDark, parent, palette);

            CreateBranchDrain(side, side * 15.8f, startZ + 4.8f, parent, palette);
            CreateBranchDrain(side, side * 10.6f, endZ - 4.8f, parent, palette);
        }

        private static void CreateHorizontalBranchCurb(int side, float z, float fromAbsoluteX, float toAbsoluteX, Transform parent, Palette palette)
        {
            float length = toAbsoluteX - fromAbsoluteX;
            float centerX = side * (fromAbsoluteX + toAbsoluteX) * 0.5f;
            CreateCube("Branch corner curb", new Vector3(centerX, 0f, z), new Vector3(length, 0.22f, 0.78f), palette.Concrete, parent, true);
        }

        private static void CreateModernBranchBlock(string name, float centerX, float centerZ, float depth, float length, float height, int roadDirection, Material wall, Transform parent, Palette palette)
        {
            CreateCube(name, new Vector3(centerX, height * 0.5f, centerZ), new Vector3(depth, height, length), wall, parent, true);

            float facadeX = centerX + roadDirection * (depth * 0.5f + 0.045f);
            float surfaceX = facadeX + roadDirection * 0.035f;
            CreateCube("Modern facade panel", new Vector3(facadeX, height * 0.5f, centerZ), new Vector3(0.08f, height * 0.94f, length - 0.24f), palette.BuildingMid, parent, false);
            CreateCube("Ground floor glass", new Vector3(surfaceX, 1.45f, centerZ), new Vector3(0.1f, 1.65f, length - 1f), palette.WindowCool, parent, false);
            CreateCube("Upper window ribbon", new Vector3(surfaceX, 4.15f, centerZ), new Vector3(0.1f, 0.95f, length - 1f), palette.WindowWarm, parent, false);
            CreateCube("Facade floor band", new Vector3(surfaceX + roadDirection * 0.01f, 2.55f, centerZ), new Vector3(0.12f, 0.18f, length - 0.42f), palette.Metal, parent, false);
            CreateCube("Modern recessed door", new Vector3(surfaceX + roadDirection * 0.02f, 1.12f, centerZ), new Vector3(0.12f, 2.15f, 1.25f), palette.Metal, parent, false);
            CreateCube("Entrance canopy", new Vector3(facadeX + roadDirection * 0.36f, 2.32f, centerZ), new Vector3(0.72f, 0.12f, 1.8f), palette.Concrete, parent, false);

            int mullionCount = length > 12f ? 5 : 3;
            for (int i = 0; i < mullionCount; i++)
            {
                float t = mullionCount == 1 ? 0.5f : i / (float)(mullionCount - 1);
                float z = centerZ - length * 0.4f + length * 0.8f * t;
                CreateCube("Modern window mullion", new Vector3(surfaceX + roadDirection * 0.015f, 3.1f, z), new Vector3(0.13f, 4.1f, 0.08f), palette.Metal, parent, false);
            }

            CreateCube("Rooftop equipment", new Vector3(centerX, height + 0.32f, centerZ + length * 0.18f), new Vector3(depth * 0.52f, 0.62f, Mathf.Min(2.4f, length * 0.28f)), palette.Concrete, parent, false);
        }

        private static void CreateBranchDrain(int side, float x, float z, Transform parent, Palette palette)
        {
            CreateCube("Branch storm drain", new Vector3(x, 0.015f, z), new Vector3(0.48f, 0.025f, 1.15f), palette.Metal, parent, false);
            for (int slot = -2; slot <= 2; slot++)
            {
                CreateCube("Branch drain slot", new Vector3(x, 0.035f, z + slot * 0.18f), new Vector3(0.36f, 0.02f, 0.045f), palette.Concrete, parent, false);
            }
        }

        private static void CreateModernRoadDetails(Transform parent, Palette palette)
        {
            for (float z = 3.5f; z <= 84f; z += 6.2f)
            {
                if (IsNearAnyJunction(z, 4.1f)) continue;
                CreateCube("Main dashed center marking", new Vector3(0f, 0.018f, z), new Vector3(0.09f, 0.02f, 2.45f), palette.RoadMarking, parent, false);
            }

            CreateCrosswalk(39.5f, parent, palette);
            CreateCrosswalk(80.8f, parent, palette);
            CreateManhole(new Vector3(1.15f, 0.01f, 15.2f), parent, palette);
            CreateManhole(new Vector3(-1.25f, 0.01f, 44.2f), parent, palette);
            CreateManhole(new Vector3(13.2f, 0.01f, 39.1f), parent, palette);
            CreateManhole(new Vector3(-13.2f, 0.01f, 68.4f), parent, palette);

            CreateBranchRoadMarkings(1, 26.65f, 51.7f, parent, palette);
            CreateBranchRoadMarkings(-1, 60.05f, 76.75f, parent, palette);
            CreateTrafficMirror(1, 26.65f, parent, palette);
            CreateTrafficMirror(-1, 60.05f, parent, palette);
        }

        private static void CreateCrosswalk(float z, Transform parent, Palette palette)
        {
            for (int stripe = -3; stripe <= 3; stripe++)
            {
                CreateCube("Modern zebra crossing", new Vector3(stripe * 0.82f, 0.02f, z), new Vector3(0.46f, 0.022f, 3.1f), palette.RoadMarking, parent, false);
            }
        }

        private static void CreateManhole(Vector3 position, Transform parent, Palette palette)
        {
            CreateCylinder("Japanese manhole cover", position, new Vector3(0.48f, 0.018f, 0.48f), palette.Metal, parent, false);
            CreateCylinder("Manhole inset", position + Vector3.up * 0.022f, new Vector3(0.34f, 0.012f, 0.34f), palette.Concrete, parent, false);
        }

        private static void CreateBranchRoadMarkings(int side, float startZ, float endZ, Transform parent, Palette palette)
        {
            float centerZ = (startZ + endZ) * 0.5f;
            float verticalLength = endZ - startZ + 5f;
            CreateCube("Branch outer edge line", new Vector3(side * 15.85f, 0.018f, centerZ), new Vector3(0.08f, 0.02f, verticalLength), palette.RoadMarking, parent, false);
            CreateCube("Branch inner edge line", new Vector3(side * 10.55f, 0.018f, centerZ), new Vector3(0.08f, 0.02f, Mathf.Max(1f, endZ - startZ - 5.4f)), palette.RoadMarking, parent, false);

            float connectorCenterX = side * 8.25f;
            CreateCube("Branch junction stop line", new Vector3(side * 5.05f, 0.02f, startZ), new Vector3(0.11f, 0.022f, 5.2f), palette.RoadMarking, parent, false);
            CreateCube("Branch junction stop line", new Vector3(side * 5.05f, 0.02f, endZ), new Vector3(0.11f, 0.022f, 5.2f), palette.RoadMarking, parent, false);

            for (int i = 0; i < 3; i++)
            {
                float x = Mathf.Lerp(Mathf.Abs(connectorCenterX) - 2.2f, Mathf.Abs(connectorCenterX) + 2.2f, i / 2f);
                CreateCube("Branch dashed center marking", new Vector3(side * x, 0.018f, startZ), new Vector3(1.15f, 0.02f, 0.08f), palette.RoadMarking, parent, false);
                CreateCube("Branch dashed center marking", new Vector3(side * x, 0.018f, endZ), new Vector3(1.15f, 0.02f, 0.08f), palette.RoadMarking, parent, false);
            }

            int dashCount = Mathf.Max(2, Mathf.FloorToInt((endZ - startZ) / 4.8f));
            for (int i = 0; i < dashCount; i++)
            {
                float t = (i + 0.5f) / dashCount;
                CreateCube("Branch dashed center marking", new Vector3(side * 13.2f, 0.018f, Mathf.Lerp(startZ + 3f, endZ - 3f, t)), new Vector3(0.08f, 0.02f, 1.45f), palette.RoadMarking, parent, false);
            }
        }

        private static void CreateTrafficMirror(int side, float junctionZ, Transform parent, Palette palette)
        {
            Vector3 polePosition = new Vector3(side * 4.55f, 1.25f, junctionZ - 3.75f);
            CreateCylinder("Japanese traffic mirror pole", polePosition, new Vector3(0.045f, 1.25f, 0.045f), palette.Metal, parent, false);
            Vector3 mirrorPosition = polePosition + Vector3.up * 1.27f;
            GameObject back = CreateCylinder("Traffic mirror yellow back", mirrorPosition, new Vector3(0.34f, 0.055f, 0.34f), palette.Tactile, parent, false);
            back.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject face = CreateCylinder("Traffic mirror face", mirrorPosition - Vector3.right * side * 0.06f, new Vector3(0.28f, 0.025f, 0.28f), palette.AccentCream, parent, false);
            face.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void CreateBuilding(int side, int index, float z, Transform parent, Palette palette, AlleyVariant variant)
        {
            const float baseHeight = 5.1f;
            float height = baseHeight + (index % 4) * 0.62f + (side > 0 ? 0.24f : 0f);
            float depth = 4.8f + (index % 3) * 0.65f;
            float facadeX = side * 4.72f;
            Material wall = index % 3 == 0 ? palette.BuildingWarm : index % 3 == 1 ? palette.BuildingMid : palette.BuildingDark;
            float centerX = side * (4.72f + depth * 0.5f);

            CreateCube($"Building shell {side} {index}", new Vector3(centerX, height * 0.5f, z), new Vector3(depth, height, 7.95f), wall, parent, true);
            CreateCube("Pixel facade art", new Vector3(facadeX - side * 0.055f, height * 0.5f, z), new Vector3(0.07f, height * 0.94f, 7.7f), palette.FacadeArt, parent, false);

            CreateCube("Facade lower trim", new Vector3(facadeX - side * 0.12f, 0.22f, z), new Vector3(0.15f, 0.24f, 7.9f), palette.Metal, parent, false);
            CreateCube("Facade upper trim", new Vector3(facadeX - side * 0.12f, height - 0.18f, z), new Vector3(0.15f, 0.22f, 7.9f), palette.Metal, parent, false);
            CreateCube("Facade edge trim", new Vector3(facadeX - side * 0.13f, height * 0.5f, z - 3.82f), new Vector3(0.16f, height, 0.16f), palette.Metal, parent, false);
            CreateCube("Facade edge trim", new Vector3(facadeX - side * 0.13f, height * 0.5f, z + 3.82f), new Vector3(0.16f, height, 0.16f), palette.Metal, parent, false);

            CreateWindow(side, facadeX, 2.55f, z - 2.18f, (index + side) % 2 == 0 ? palette.WindowWarm : palette.WindowCool, palette.Metal, parent);
            CreateWindow(side, facadeX, 4.45f, z + 2.12f, (index + side) % 3 == 0 ? palette.WindowWarm : palette.WindowCool, palette.Metal, parent);
            CreateCube("Recessed doorway", new Vector3(facadeX - side * 0.14f, 1.05f, z), new Vector3(0.16f, 2.05f, 1.18f), palette.Metal, parent, false);

            DecorateResidentialBuilding(side, index, z, facadeX, height, parent, palette);
        }

        private static void CreateWindow(int side, float facadeX, float y, float z, Material glass, Material frame, Transform parent)
        {
            float x = facadeX - side * 0.17f;
            CreateCube("Window glass", new Vector3(x, y, z), new Vector3(0.12f, 0.72f, 1.18f), glass, parent, false);
            CreateCube("Window frame top", new Vector3(x - side * 0.045f, y + 0.42f, z), new Vector3(0.16f, 0.09f, 1.42f), frame, parent, false);
            CreateCube("Window frame bottom", new Vector3(x - side * 0.045f, y - 0.42f, z), new Vector3(0.16f, 0.09f, 1.42f), frame, parent, false);
            CreateCube("Window frame side", new Vector3(x - side * 0.045f, y, z - 0.67f), new Vector3(0.16f, 0.9f, 0.09f), frame, parent, false);
            CreateCube("Window frame side", new Vector3(x - side * 0.045f, y, z + 0.67f), new Vector3(0.16f, 0.9f, 0.09f), frame, parent, false);
        }

        private static void DecorateResidentialBuilding(int side, int index, float z, float facadeX, float height, Transform parent, Palette palette)
        {
            GameObject roof = CreateCube("Sloped ceramic roof", new Vector3(facadeX + side * 0.16f, height + 0.05f, z), new Vector3(2.2f, 0.28f, 8.25f), palette.BuildingDark, parent, false);
            roof.transform.rotation = Quaternion.Euler(0f, 0f, side * 12f);

            CreateCube("Small balcony", new Vector3(facadeX - side * 0.48f, 3.55f, z), new Vector3(0.85f, 0.14f, 3.05f), palette.Metal, parent, false);
            for (int rail = -2; rail <= 2; rail++)
            {
                CreateCube("Balcony rail", new Vector3(facadeX - side * 0.82f, 3.98f, z + rail * 0.62f), new Vector3(0.08f, 0.76f, 0.08f), palette.Metal, parent, false);
            }

            CreateCube("Balcony top rail", new Vector3(facadeX - side * 0.82f, 4.38f, z), new Vector3(0.09f, 0.09f, 3.15f), palette.Metal, parent, false);
            CreatePorchLight(new Vector3(facadeX - side * 0.38f, 2.22f, z + 0.9f), parent, palette);

            if (index % 2 == 0)
            {
                CreatePlant(new Vector3(facadeX - side * 0.62f, 0.32f, z + 2.25f), parent, palette);
            }
            else
            {
                CreateMailbox(new Vector3(facadeX - side * 0.6f, 0.95f, z - 1.9f), parent, palette);
            }
        }

        private static void CreateVariantSetDressing(Transform parent, Palette palette, AlleyVariant variant)
        {
            CreateBicycle(new Vector3(-4.18f, 0.42f, 18f), 8f, parent, palette);
            CreateBicycle(new Vector3(4.15f, 0.42f, 49f), 176f, parent, palette);
            CreatePlant(new Vector3(-4.3f, 0.32f, 38f), parent, palette);
            CreatePlant(new Vector3(4.3f, 0.32f, 70f), parent, palette);
        }

        private static void CreateStreetLights(Transform parent, Palette palette, AlleyVariant variant)
        {
            const int count = 7;
            for (int i = 0; i < count; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float z = 8f + i * 12.2f;
                float x = side * 3.95f;

                if (IsNearMainJunction(side, z, 4.6f))
                {
                    continue;
                }

                CreateCylinder("Pixel lamp post", new Vector3(x, 2.2f, z), new Vector3(0.055f, 2.2f, 0.055f), palette.Metal, parent, false);
                CreateCube("Pixel lamp head", new Vector3(x - side * 0.34f, 4.33f, z), new Vector3(0.72f, 0.14f, 0.34f), palette.AccentCream, parent, false);

                GameObject lightObject = new GameObject("Palette street light");
                lightObject.transform.SetParent(parent);
                lightObject.transform.position = new Vector3(x - side * 0.42f, 4.05f, z);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = palette.WarmLight;
                light.range = 10.5f;
                light.intensity = 1.55f;
                light.shadows = LightShadows.None;
                lightObject.AddComponent<FlickeringLight>();
            }
        }

        private static void CreateUtilityDetails(Transform parent, Palette palette)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = 5f + i * 19f;
                if (!IsNearMainJunction(-1, z, 4.2f))
                {
                    CreateCylinder("Utility pole L", new Vector3(-4.48f, 3.5f, z), new Vector3(0.1f, 3.5f, 0.1f), palette.Metal, parent, false);
                }

                float rightZ = z + 7f;
                if (!IsNearMainJunction(1, rightZ, 4.2f))
                {
                    CreateCylinder("Utility pole R", new Vector3(4.48f, 3.5f, rightZ), new Vector3(0.1f, 3.5f, 0.1f), palette.Metal, parent, false);
                }
            }

            CreateRodBetween("Overhead cable", new Vector3(-4.48f, 6.6f, 5f), new Vector3(4.48f, 6.15f, 12f), 0.018f, palette.Metal, parent);
            CreateRodBetween("Overhead cable", new Vector3(4.48f, 6.2f, 31f), new Vector3(-4.48f, 6.6f, 43f), 0.018f, palette.Metal, parent);
            CreateRodBetween("Overhead cable", new Vector3(-4.48f, 6.6f, 62f), new Vector3(4.48f, 6.2f, 69f), 0.018f, palette.Metal, parent);

            for (int i = 0; i < 6; i++)
            {
                float z = 10f + i * 14f;
                int side = i % 2 == 0 ? 1 : -1;
                if (IsNearMainJunction(side, z, 4.6f)) continue;
                CreateCube("Air conditioner", new Vector3(side * 4.48f, 2.9f, z), new Vector3(0.46f, 0.7f, 1.1f), palette.Concrete, parent, false);
                CreateCube("AC pixel grille", new Vector3(side * 4.22f, 2.9f, z), new Vector3(0.08f, 0.5f, 0.74f), palette.Metal, parent, false);
            }
        }

        private static void CreateDrainage(Transform parent, Palette palette)
        {
            for (int i = 0; i < 12; i++)
            {
                float z = 3f + i * 7.2f;
                int side = i % 2 == 0 ? -1 : 1;
                CreateCube("Road drain", new Vector3(side * 3.7f, 0.015f, z), new Vector3(0.5f, 0.025f, 1.25f), palette.Metal, parent, false);
                for (int slot = -2; slot <= 2; slot++)
                {
                    CreateCube("Drain slot", new Vector3(side * 3.7f, 0.035f, z + slot * 0.2f), new Vector3(0.38f, 0.02f, 0.055f), palette.Concrete, parent, false);
                }
            }
        }

        private static void CreatePuddles(Transform parent, Palette palette, AlleyVariant variant)
        {
            CreateCube("Pixel puddle", new Vector3(-1.45f, 0.015f, 16f), new Vector3(1.25f, 0.018f, 3.2f), palette.Puddle, parent, false);
            CreateCube("Pixel puddle", new Vector3(1.85f, 0.015f, 41f), new Vector3(1.05f, 0.018f, 2.4f), palette.Puddle, parent, false);
        }

        private static void CreatePlant(Vector3 origin, Transform parent, Palette palette)
        {
            CreateCylinder("Plant pot", origin, new Vector3(0.25f, 0.28f, 0.25f), palette.BuildingWarm, parent, false);
            Material leaves = CreateMaterial("Muted pixel leaves", Hex("#52654A"), 0.08f, 0f);
            for (int i = 0; i < 5; i++)
            {
                Vector3 leafPosition = origin + new Vector3(Mathf.Sin(i * 2.1f) * 0.23f, 0.45f + (i % 2) * 0.18f, Mathf.Cos(i * 2.1f) * 0.23f);
                GameObject leaf = CreateCube("Blocky leaf", leafPosition, new Vector3(0.16f, 0.46f, 0.11f), leaves, parent, false);
                leaf.transform.rotation = Quaternion.Euler(i * 9f, i * 71f, i % 2 == 0 ? 24f : -24f);
            }
        }

        private static void CreateMailbox(Vector3 origin, Transform parent, Palette palette)
        {
            CreateCube("Japanese mailbox", origin, new Vector3(0.42f, 0.55f, 0.34f), palette.AccentRed, parent, false);
            CreateCube("Mailbox slot", origin + new Vector3(0f, 0.1f, -0.19f), new Vector3(0.25f, 0.045f, 0.03f), palette.Metal, parent, false);
            CreateCylinder("Mailbox post", origin + Vector3.down * 0.55f, new Vector3(0.05f, 0.42f, 0.05f), palette.Metal, parent, false);
        }

        private static void CreateBicycle(Vector3 origin, float yaw, Transform parent, Palette palette)
        {
            Transform bicycle = new GameObject("Pixel bicycle").transform;
            bicycle.SetParent(parent);
            bicycle.position = origin;
            bicycle.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject rearWheel = CreateLocalCylinder("Rear wheel", bicycle, new Vector3(0f, 0f, -0.65f), new Vector3(0.34f, 0.035f, 0.34f), palette.Metal);
            rearWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject frontWheel = CreateLocalCylinder("Front wheel", bicycle, new Vector3(0f, 0f, 0.65f), new Vector3(0.34f, 0.035f, 0.34f), palette.Metal);
            frontWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            CreateLocalRodBetween("Bike frame", bicycle, new Vector3(0f, 0f, -0.58f), new Vector3(0f, 0.5f, 0f), 0.035f, palette.AccentBlue);
            CreateLocalRodBetween("Bike frame", bicycle, new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, 0.58f), 0.035f, palette.AccentBlue);
            CreateLocalRodBetween("Bike frame", bicycle, new Vector3(0f, 0f, -0.58f), new Vector3(0f, 0f, 0.58f), 0.035f, palette.AccentBlue);
            CreateLocalRodBetween("Handlebar", bicycle, new Vector3(0f, 0.05f, 0.58f), new Vector3(0f, 0.72f, 0.72f), 0.025f, palette.Metal);
            CreateLocalCube("Bike seat", bicycle, new Vector3(0f, 0.63f, -0.15f), new Vector3(0.16f, 0.06f, 0.28f), palette.Metal, false);
        }

        private static void CreatePorchLight(Vector3 position, Transform parent, Palette palette)
        {
            CreateCube("Porch lamp", position, new Vector3(0.24f, 0.34f, 0.24f), palette.AccentCream, parent, false);
            GameObject glow = new GameObject("Porch light glow");
            glow.transform.SetParent(parent);
            glow.transform.position = position - Vector3.right * Mathf.Sign(position.x) * 0.22f;
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = palette.WarmLight;
            light.intensity = 0.85f;
            light.range = 4.2f;
        }

        private static void CreateDestination(Transform parent, Palette palette, AlleyVariant variant)
        {
            Vector3 doorway = new Vector3(0.2f, 1.5f, 85f);
            CreateCube("Destination wall", new Vector3(0f, 2.8f, 86.5f), new Vector3(10f, 5.6f, 1f), palette.BuildingDark, parent, true);
            CreateCube("Destination doorway", doorway, new Vector3(2.25f, 3f, 0.15f), palette.AccentCream, parent, false);
            CreateSignText("門", doorway + new Vector3(0f, 1.65f, -0.11f), 0, palette.UiAccent, parent, 0.085f);

            GameObject lightObject = new GameObject("Destination light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = new Vector3(0.2f, 2.1f, 83.5f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = palette.WarmLight;
            light.range = 9f;
            light.intensity = 2.2f;
        }

        private static void CreateMoonlight(Transform parent, Palette palette, AlleyVariant variant)
        {
            GameObject lightObject = new GameObject("Palette moonlight");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(44f, -32f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = palette.KeyLight;
            light.intensity = 0.56f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.62f;
        }

        private static FirstPersonController CreatePlayer(Transform parent, Palette palette, AlleyVariant variant, Vector3 position)
        {
            GameObject playerObject = new GameObject("Ren - Player");
            playerObject.transform.SetParent(parent);
            playerObject.transform.position = position;
            playerObject.AddComponent<ProtagonistIdentity>();

            CharacterController character = playerObject.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.32f;
            character.center = new Vector3(0f, 0.9f, 0f);
            character.stepOffset = 0.25f;
            character.skinWidth = 0.04f;

            GameObject cameraObject = new GameObject("Pixel First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 66f;
            camera.nearClipPlane = 0.04f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = palette.Fog;
            camera.allowHDR = false;
            cameraObject.AddComponent<AudioListener>();

            PixelArtRenderer pixelRenderer = cameraObject.AddComponent<PixelArtRenderer>();
            pixelRenderer.Configure(480, 48, 0.02f);

            GameObject fillObject = new GameObject("First person eye-adaptation fill");
            fillObject.transform.SetParent(cameraObject.transform, false);
            fillObject.transform.localPosition = new Vector3(0f, -0.45f, 1.2f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = palette.KeyLight;
            fill.intensity = 0.62f;
            fill.range = 15f;
            fill.shadows = LightShadows.None;

            CreateLocalCube("Torso", playerObject.transform, new Vector3(0f, 1.15f, -0.04f), new Vector3(0.52f, 0.66f, 0.28f), palette.Shirt, false);
            CreateLocalCube("Left leg", playerObject.transform, new Vector3(-0.14f, 0.48f, 0f), new Vector3(0.2f, 0.78f, 0.22f), palette.Trousers, false);
            CreateLocalCube("Right leg", playerObject.transform, new Vector3(0.14f, 0.48f, 0f), new Vector3(0.2f, 0.78f, 0.22f), palette.Trousers, false);
            CreateLocalCube("Left sleeve", cameraObject.transform, new Vector3(-0.34f, -0.38f, 0.58f), new Vector3(0.18f, 0.2f, 0.55f), palette.Shirt, false);
            CreateLocalCube("Right sleeve", cameraObject.transform, new Vector3(0.34f, -0.38f, 0.58f), new Vector3(0.18f, 0.2f, 0.55f), palette.Shirt, false);

            FirstPersonController controller = playerObject.AddComponent<FirstPersonController>();
            controller.Configure(camera);
            return controller;
        }

        private static TargetWalker CreateTarget(Transform parent, Palette palette, Vector3[] route)
        {
            GameObject targetObject = new GameObject("Target - Adult Woman");
            targetObject.transform.SetParent(parent);
            targetObject.transform.position = route[0];
            if (route.Length > 1)
            {
                Vector3 initialDirection = route[1] - route[0];
                initialDirection.y = 0f;
                if (initialDirection.sqrMagnitude > 0.001f)
                {
                    targetObject.transform.rotation = Quaternion.LookRotation(initialDirection.normalized, Vector3.up);
                }
            }

            CapsuleCollider collider = targetObject.AddComponent<CapsuleCollider>();
            collider.height = 1.82f;
            collider.radius = 0.3f;
            collider.center = new Vector3(0f, 0.91f, 0f);

            GameObject characterPrefab = Resources.Load<GameObject>("Characters/TargetWoman3D");
            if (characterPrefab != null)
            {
                GameObject character = Object.Instantiate(characterPrefab, targetObject.transform, false);
                character.name = "Konseki VRM Adult Woman";
                CartoonTargetAnimator characterAnimation = character.GetComponent<CartoonTargetAnimator>();
                if (characterAnimation != null)
                {
                    characterAnimation.SetMovementRoot(targetObject.transform);
                }
            }
            else
            {
                Debug.LogWarning("[Konseki] TargetWoman3D Prefab을 찾지 못해 단순 3D 실루엣을 사용합니다.");
                CreateLocalCube("Fallback body", targetObject.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.64f, 1.3f, 0.34f), palette.Coat, false);
                CreateLocalSphere("Fallback head", targetObject.transform, new Vector3(0f, 1.82f, 0f), new Vector3(0.4f, 0.44f, 0.4f), palette.Skin, false);
            }

            TargetWalker walker = targetObject.AddComponent<TargetWalker>();
            walker.Configure(route, null, null, null, null);
            return walker;
        }

        public static void CombineStaticGeometry(Transform environment)
        {
            MeshRenderer[] renderers = environment.GetComponentsInChildren<MeshRenderer>();
            List<GameObject> staticObjects = new List<GameObject>(renderers.Length);

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer.GetComponent<MeshFilter>() == null)
                {
                    continue;
                }

                staticObjects.Add(renderer.gameObject);
            }

            if (staticObjects.Count > 0)
            {
                StaticBatchingUtility.Combine(staticObjects.ToArray(), environment.gameObject);
            }
        }

        private static Texture2D CreatePixelPatternTexture(string name, Color baseColor, Color detailColor, int size, bool lines)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 0
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hash = (x * 17 + y * 31 + x * y * 3) % 23;
                    bool detail = hash < 3 || (lines && (x + y * 2) % 13 == 0);
                    pixels[y * size + x] = detail ? detailColor : baseColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader) { name = name, color = color };
            SetColor(material, "_BaseColor", color);
            SetFloat(material, "_Glossiness", smoothness);
            SetFloat(material, "_Smoothness", smoothness);
            SetFloat(material, "_Metallic", metallic);
            return material;
        }

        private static Material CreateTexturedMaterial(string name, Color color, Texture texture, float smoothness, float metallic, Vector2 scale)
        {
            Material material = CreateMaterial(name, color, smoothness, metallic);
            if (texture != null)
            {
                material.mainTexture = texture;
                material.mainTextureScale = scale;
                SetTexture(material, "_BaseMap", texture);
                if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", scale);
            }

            return material;
        }

        private static Material CreateEmissiveMaterial(string name, Color color, Color emission)
        {
            Material material = CreateMaterial(name, color, 0.46f, 0.04f);
            material.EnableKeyword("_EMISSION");
            SetColor(material, "_EmissionColor", emission);
            return material;
        }

        private static void SetColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property)) material.SetColor(property, value);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }

        private static void SetTexture(Material material, string property, Texture value)
        {
            if (material.HasProperty(property)) material.SetTexture(property, value);
        }

        private static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(value, out color) ? color : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(Mathf.Clamp01(color.r + amount), Mathf.Clamp01(color.g + amount), Mathf.Clamp01(color.b + amount), color.a);
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(Mathf.Clamp01(color.r - amount), Mathf.Clamp01(color.g - amount), Mathf.Clamp01(color.b - amount), color.a);
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, position, scale, material, parent, keepCollider);
        }

        private static GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            return CreatePrimitive(PrimitiveType.Sphere, name, position, scale, material, parent, keepCollider);
        }

        private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, position, scale, material, parent, keepCollider);
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;

            if (!keepCollider)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                    if (Application.isPlaying)
                    {
                        Object.Destroy(collider);
                    }
                    else
                    {
                        Object.DestroyImmediate(collider);
                    }
                }
            }

            return gameObject;
        }

        private static GameObject CreateLocalCube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject gameObject = CreateCube(name, Vector3.zero, scale, material, parent, keepCollider);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            return gameObject;
        }

        private static GameObject CreateLocalSphere(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject gameObject = CreateSphere(name, Vector3.zero, scale, material, parent, keepCollider);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            return gameObject;
        }

        private static GameObject CreateLocalCylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject gameObject = CreateCylinder(name, Vector3.zero, scale, material, parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            return gameObject;
        }

        private static void CreateRodBetween(string name, Vector3 from, Vector3 to, float thickness, Material material, Transform parent)
        {
            Vector3 direction = to - from;
            GameObject rod = CreateCylinder(name, (from + to) * 0.5f, new Vector3(thickness, direction.magnitude * 0.5f, thickness), material, parent, false);
            rod.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private static void CreateLocalRodBetween(string name, Transform parent, Vector3 from, Vector3 to, float thickness, Material material)
        {
            Vector3 direction = to - from;
            GameObject rod = CreateLocalCylinder(name, parent, (from + to) * 0.5f, new Vector3(thickness, direction.magnitude * 0.5f, thickness), material);
            rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private static void CreateSignText(string text, Vector3 position, int side, Color color, Transform parent, float size)
        {
            GameObject label = new GameObject("Pixel sign lettering");
            label.transform.SetParent(parent);
            label.transform.position = position;
            label.transform.rotation = side == 0 ? Quaternion.identity : Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = color;
        }
    }
}
