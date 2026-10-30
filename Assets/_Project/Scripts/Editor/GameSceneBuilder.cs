using System.IO;
using BubbleAscent.Data;
using BubbleAscent.Gameplay;
using BubbleAscent.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BubbleAscent.EditorTools
{
    /// <summary>
    /// Builds the game scene and its generated sprites from scratch (reproducible, no hand-edited YAML).
    /// Run it from the menu: Bubble Ascent / Build Game Scene.
    /// </summary>
    public static class GameSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        const string TuningPath = "Assets/_Project/Data/GameTuning.asset";
        const string BubbleMaterialPath = "Assets/_Project/Art/Materials/Bubble.mat";
        const string SpriteDir = "Assets/_Project/Art/Sprites";
        const string WhitePath = SpriteDir + "/White.png";
        const string RoundedPath = SpriteDir + "/RoundedRect.png";
        const string UnlitSpriteMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // Rounded rectangle sprite: size and corner radius in pixels, 100 pixels per world unit.
        const int RoundedSize = 48, RoundedRadius = 16, RoundedPixelsPerUnit = 100;

        // Sorting: background < obstacles < bubble.
        const int SurfaceLineOrder = -10, ObstacleOrder = 0, BubbleOrder = 1;

        [MenuItem("Bubble Ascent/Build Game Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameTuning>(TuningPath) == null)
            {
                Debug.LogError($"GameSceneBuilder: tuning asset not found at {TuningPath}");
                return;
            }

            // Make sure the generated sprites and the HUD prefab exist before the scene is created.
            EnsureSprites();
            if (!HudPrefabBuilder.EnsurePrefab()) return;

            // NewScene(Single) unloads unused assets, so load what the scene references afterwards.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(TuningPath);
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
            var bubbleMaterial = AssetDatabase.LoadAssetAtPath<Material>(BubbleMaterialPath);

            Camera cam = BuildCamera(tuning.ascent.viewHalfHeight);
            BackgroundView background = BuildBackground(cam, tuning.journey.surfaceHeight, rounded, unlit);
            BubbleView bubble = BuildBubble(white, bubbleMaterial);
            ObstaclesView obstacles = BuildObstacles(rounded, unlit);
            HudView hud = InstantiateHud();

            var gameGo = new GameObject("Game");
            var input = gameGo.AddComponent<HoldInput>();
            var controller = gameGo.AddComponent<GameController>();
            var so = new SerializedObject(controller);
            so.FindProperty("tuning").objectReferenceValue = tuning;
            so.FindProperty("input").objectReferenceValue = input;
            so.FindProperty("bubbleView").objectReferenceValue = bubble;
            so.FindProperty("obstaclesView").objectReferenceValue = obstacles;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("gameCamera").objectReferenceValue = cam;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"GameSceneBuilder: built {ScenePath}");
        }

        static Camera BuildCamera(float halfHeight)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = halfHeight;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
        }

        static BackgroundView BuildBackground(Camera cam, float surface, Sprite rounded, Material unlit)
        {
            var go = new GameObject("Background");

            var line = new GameObject("SurfaceLine").AddComponent<SpriteRenderer>();
            line.transform.SetParent(go.transform, false);
            line.sprite = rounded;
            line.drawMode = SpriteDrawMode.Sliced;
            line.size = new Vector2(40f, 0.16f);
            line.color = new Color(1f, 1f, 1f, 0.7f);
            line.sharedMaterial = unlit;
            line.sortingOrder = SurfaceLineOrder;

            // Deep blue at the bottom, light blue at the surface, then the sky fading toward dusk.
            float s = surface;
            var stops = new[]
            {
                Stop(0f, "#0f2a63"), Stop(s * 0.35f, "#16408a"), Stop(s * 0.7f, "#2a78c4"), Stop(s, "#8fd8f0"),
                Stop(s + 30f, "#bfe8ff"), Stop(s + 200f, "#6fb2f0"), Stop(s + 400f, "#2f4a8a"), Stop(s + 600f, "#101744"),
            };

            var view = go.AddComponent<BackgroundView>();
            var so = new SerializedObject(view);
            so.FindProperty("backgroundCamera").objectReferenceValue = cam;
            so.FindProperty("surfaceLine").objectReferenceValue = line;
            SerializedProperty list = so.FindProperty("stops");
            list.arraySize = stops.Length;
            for (int i = 0; i < stops.Length; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("height").floatValue = stops[i].height;
                item.FindPropertyRelative("color").colorValue = stops[i].color;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static BackgroundView.Stop Stop(float height, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return new BackgroundView.Stop { height = height, color = color };
        }

        static BubbleView BuildBubble(Sprite white, Material material)
        {
            var go = new GameObject("Bubble");
            var body = new GameObject("Soap").AddComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false);
            body.sprite = white;
            body.sharedMaterial = material;
            body.sortingOrder = BubbleOrder;

            var view = go.AddComponent<BubbleView>();
            var so = new SerializedObject(view);
            so.FindProperty("body").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static ObstaclesView BuildObstacles(Sprite rounded, Material unlit)
        {
            var go = new GameObject("Obstacles");
            var piece = new GameObject("Piece").AddComponent<SpriteRenderer>();
            piece.transform.SetParent(go.transform, false);
            piece.sprite = rounded;
            piece.drawMode = SpriteDrawMode.Sliced;
            piece.sharedMaterial = unlit;
            piece.sortingOrder = ObstacleOrder;

            var view = go.AddComponent<ObstaclesView>();
            var so = new SerializedObject(view);
            so.FindProperty("template").objectReferenceValue = piece;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---- HUD (the layout lives in the prefab; see HudPrefabBuilder) ----

        static HudView InstantiateHud()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabBuilder.PrefabPath);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return hud.GetComponent<HudView>();
        }

        // ---- Generated sprites ----

        static void EnsureSprites()
        {
            Directory.CreateDirectory(SpriteDir);
            if (!File.Exists(WhitePath)) WritePng(WhitePath, 4, 4, (x, y) => 1f);
            ImportSprite(WhitePath, 4, Vector4.zero);

            if (!File.Exists(RoundedPath)) WritePng(RoundedPath, RoundedSize, RoundedSize, RoundedAlpha);
            ImportSprite(RoundedPath, RoundedPixelsPerUnit, Vector4.one * RoundedRadius);
        }

        // Alpha of a rounded square, antialiased over one pixel.
        static float RoundedAlpha(int x, int y)
        {
            float px = x + 0.5f, py = y + 0.5f, r = RoundedRadius, n = RoundedSize;
            float dx = Mathf.Max(r - px, px - (n - r), 0f);
            float dy = Mathf.Max(r - py, py - (n - r), 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy) - r;
            return Mathf.Clamp01(0.5f - distance);
        }

        static void WritePng(string path, int width, int height, System.Func<int, int, float> alpha)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y)));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        static void ImportSprite(string path, int pixelsPerUnit, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
