using System;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Paints the background by height: the camera's clear color follows a list of height/color stops
    /// (deep blue, light blue at the surface, then sky), and a thin line marks the sea surface.
    /// </summary>
    public class BackgroundView : MonoBehaviour
    {
        [Serializable]
        public struct Stop
        {
            [Tooltip("World height of this stop.")]
            public float height;
            public Color color;
        }

        [SerializeField] Camera backgroundCamera;
        [Tooltip("Color stops sorted by ascending height; colors blend linearly between them.")]
        [SerializeField] Stop[] stops;
        [Tooltip("Thin strip drawn at the sea surface (optional).")]
        [SerializeField] SpriteRenderer surfaceLine;

        void Awake()
        {
            if (backgroundCamera == null || stops == null || stops.Length == 0)
            {
                Debug.LogError("BackgroundView: camera or stops not assigned.", this);
                enabled = false;
            }
        }

        /// <param name="surfaceHeight">Sea surface height; the line is hidden when 0 (endless sea).</param>
        public void Init(float surfaceHeight)
        {
            if (surfaceLine == null) return;
            surfaceLine.enabled = surfaceHeight > 0f;
            Transform t = surfaceLine.transform;
            t.position = new Vector3(0f, surfaceHeight, t.position.z);
        }

        /// <summary>Sets the background color for the camera's height.</summary>
        public void Present(float viewCenterY)
        {
            if (!enabled) return;
            backgroundCamera.backgroundColor = ColorAt(viewCenterY);
        }

        Color ColorAt(float height)
        {
            if (height <= stops[0].height) return stops[0].color;
            for (int i = 1; i < stops.Length; i++)
            {
                if (height > stops[i].height) continue;
                Stop a = stops[i - 1], b = stops[i];
                float span = b.height - a.height;
                return span <= 0f ? b.color : Color.Lerp(a.color, b.color, (height - a.height) / span);
            }
            return stops[stops.Length - 1].color;
        }
    }
}
