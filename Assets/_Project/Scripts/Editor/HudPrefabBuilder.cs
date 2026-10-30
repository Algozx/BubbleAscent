using System.IO;
using BubbleAscent.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleAscent.EditorTools
{
    /// <summary>
    /// Builds the Hud prefab: a canvas that scales with the screen, a safe-area panel, a top bar split in two halves
    /// (height on the left, best on the right, so they can never overlap) and a centered prompt block that grows
    /// with its text. The prefab is only generated when missing, so hand edits to it are kept;
    /// Bubble Ascent > Rebuild HUD Prefab regenerates it from here.
    /// </summary>
    public static class HudPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Hud.prefab";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string TextMaterialPath = "Assets/_Project/Art/Materials/HudText.mat";

        // Layout in reference pixels (1080 x 1920 portrait).
        static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        const float SideMargin = 56f, TopMargin = 36f;
        const float CaptionHeight = 44f, TopBarHeight = 190f;
        const float HeightShare = 0.6f; // width of the top bar that belongs to the height; the rest to the best
        const float PromptAnchorY = 0.72f; // prompt block center, as a share of the safe area height: above the bubble

        static readonly Color CaptionColor = new Color(1f, 1f, 1f, 0.75f);
        static readonly Color HintColor = new Color(1f, 1f, 1f, 0.9f);

        [MenuItem("Bubble Ascent/Rebuild HUD Prefab")]
        public static void Rebuild()
        {
            if (Build()) Debug.Log($"HudPrefabBuilder: built {PrefabPath}");
        }

        /// <summary>Builds the prefab if it does not exist yet. Returns false if it could not be built.</summary>
        public static bool EnsurePrefab() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null || Build();

        static bool Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError($"HudPrefabBuilder: TMP font not found at {FontPath} " +
                               "(Window > TextMeshPro > Import TMP Essential Resources).");
                return false;
            }
            Material material = EnsureTextMaterial(font);

            var root = new GameObject("Hud", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // the whole reference area always fits

            RectTransform safe = Child(root.transform, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform top = Child(safe, "TopBar");
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(0.5f, 1f);
            top.offsetMin = new Vector2(SideMargin, -TopMargin - TopBarHeight);
            top.offsetMax = new Vector2(-SideMargin, -TopMargin);

            TMP_Text height = Stat(top, "Height", "HEIGHT", 0f, HeightShare, TextAlignmentOptions.TopLeft, 120f, 60f,
                font, material);
            TMP_Text best = Stat(top, "Best", "BEST", HeightShare, 1f, TextAlignmentOptions.TopRight, 64f, 36f,
                font, material);

            RectTransform block = Child(safe, "PromptBlock");
            block.anchorMin = new Vector2(0f, PromptAnchorY);
            block.anchorMax = new Vector2(1f, PromptAnchorY);
            block.pivot = new Vector2(0.5f, 0.5f);
            block.offsetMin = new Vector2(SideMargin, 0f);
            block.offsetMax = new Vector2(-SideMargin, 0f);
            var layout = block.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            block.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text prompt = Text(block, "Prompt", "HOLD TO START", 104f, FontStyles.Bold, TextAlignmentOptions.Center,
                Color.white, font, material);
            prompt.textWrappingMode = TextWrappingModes.Normal;
            TMP_Text hint = Text(block, "Hint", "hold: grow and rise\nrelease: shrink and sink", 48f, FontStyles.Normal,
                TextAlignmentOptions.Center, HintColor, font, material);
            hint.textWrappingMode = TextWrappingModes.Normal;
            hint.lineSpacing = 10f;

            var view = root.AddComponent<HudView>();
            var so = new SerializedObject(view);
            so.FindProperty("heightText").objectReferenceValue = height;
            so.FindProperty("bestText").objectReferenceValue = best;
            so.FindProperty("promptText").objectReferenceValue = prompt;
            so.FindProperty("hintText").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return true;
        }

        // Font material with a soft drop shadow and a thin outline, so white text reads on any background color.
        static Material EnsureTextMaterial(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(TextMaterialPath);
            if (existing != null) return existing;

            var material = new Material(font.material) { name = "HudText" };
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.04f, 0.1f, 0.24f, 0.8f));
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.02f, 0.06f, 0.18f, 0.5f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.4f);
            AssetDatabase.CreateAsset(material, TextMaterialPath);
            return material;
        }

        // A caption over a value, filling [xMin, xMax] of the top bar; the value shrinks to fit instead of spilling over.
        static TMP_Text Stat(RectTransform bar, string name, string caption, float xMin, float xMax,
            TextAlignmentOptions alignment, float size, float minSize, TMP_FontAsset font, Material material)
        {
            RectTransform group = Child(bar, name);
            group.anchorMin = new Vector2(xMin, 0f);
            group.anchorMax = new Vector2(xMax, 1f);
            group.offsetMin = group.offsetMax = Vector2.zero;

            TMP_Text label = Text(group, "Caption", caption, 34f, FontStyles.Bold, alignment, CaptionColor, font, material);
            label.characterSpacing = 12f;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.offsetMin = new Vector2(0f, -CaptionHeight);
            labelRect.offsetMax = Vector2.zero;

            TMP_Text value = Text(group, "Value", "0", size, FontStyles.Bold, alignment, Color.white, font, material);
            value.enableAutoSizing = true;
            value.fontSizeMin = minSize;
            value.fontSizeMax = size;
            Stretch(value.rectTransform);
            value.rectTransform.offsetMax = new Vector2(0f, -CaptionHeight);
            return value;
        }

        static TMP_Text Text(Transform parent, string name, string content, float size, FontStyles style,
            TextAlignmentOptions alignment, Color color, TMP_FontAsset font, Material material)
        {
            RectTransform rect = Child(parent, name);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = material;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
