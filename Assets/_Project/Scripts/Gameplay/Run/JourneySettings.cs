using System;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>Which part of the journey the run is in.</summary>
    public enum JourneyPhase
    {
        Sea,
        Sky,
    }

    /// <summary>
    /// Where the journey's border is (the sea surface) and how the sky's thin air behaves.
    /// </summary>
    [Serializable]
    public class JourneySettings
    {
        [Tooltip("Height (m) of the sea surface: the sky starts above it (0 = endless sea).")]
        public float surfaceHeight;

        [Header("Thin air (sky)")]
        [Tooltip("Self-inflation (radius/s) once the air is fully thin. Capped at half the deflate rate.")]
        public float thinAirInflate;
        [Tooltip("Meters above the surface where the air reaches full thinness.")]
        public float thinAirFullAbove;

        /// <summary>Self-inflation at a height: 0 in the sea, ramping up to thinAirInflate above the surface.</summary>
        public float ThinAirAt(float height)
        {
            if (surfaceHeight <= 0f || height <= surfaceHeight || thinAirInflate <= 0f) return 0f;
            if (thinAirFullAbove <= 0f) return thinAirInflate;
            float t = (height - surfaceHeight) / thinAirFullAbove;
            return thinAirInflate * (t > 1f ? 1f : t);
        }
    }
}
