using System;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Small float helpers for the plain-C# simulation (kept free of UnityEngine so the rules stay engine-independent).
    /// </summary>
    static class Numeric
    {
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);

        /// <summary>Linear blend from a to b; t is clamped to [0, 1].</summary>
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        /// <summary>Moves current toward target by at most maxDelta.</summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float diff = target - current;
            return Math.Abs(diff) <= maxDelta ? target : current + Math.Sign(diff) * maxDelta;
        }
    }
}
