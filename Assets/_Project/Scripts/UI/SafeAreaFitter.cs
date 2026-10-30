using UnityEngine;

namespace BubbleAscent.UI
{
    /// <summary>
    /// Fits its RectTransform to the device's safe area (notches, rounded corners, home indicator), so HUD elements
    /// anchored inside it are never hidden. Re-applies when the safe area or the screen size changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rect;
        Rect _appliedArea;
        Vector2Int _appliedScreen;

        void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                Apply();
        }

        void Apply()
        {
            _appliedArea = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            AnchorsFor(_appliedArea, _appliedScreen, out Vector2 min, out Vector2 max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }

        /// <summary>Safe area in pixels to normalized anchors; the full screen when the screen size is unknown.</summary>
        public static void AnchorsFor(Rect safeArea, Vector2 screen, out Vector2 min, out Vector2 max)
        {
            if (screen.x <= 0f || screen.y <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }
            min = new Vector2(Mathf.Clamp01(safeArea.xMin / screen.x), Mathf.Clamp01(safeArea.yMin / screen.y));
            max = new Vector2(Mathf.Clamp01(safeArea.xMax / screen.x), Mathf.Clamp01(safeArea.yMax / screen.y));
        }
    }
}
