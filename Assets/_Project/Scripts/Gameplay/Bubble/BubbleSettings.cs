using System;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Bubble physics numbers: size and vertical motion.
    /// </summary>
    [Serializable]
    public class BubbleSettings
    {
        [Header("Size (radius, world units)")]
        [Tooltip("Smallest possible radius: the size that fits through gates.")]
        public float minRadius;
        [Tooltip("Largest possible radius.")]
        public float maxRadius;
        [Tooltip("Radius at the start of a run.")]
        public float startRadius;
        [Tooltip("Radius gained per second while holding.")]
        public float inflateRate;
        [Tooltip("Radius lost per second while released.")]
        public float deflateRate;

        [Header("Vertical motion (units/s)")]
        [Tooltip("Rise speed at the smallest size.")]
        public float riseSpeedAtMin;
        [Tooltip("Rise speed at the largest size.")]
        public float riseSpeedAtMax;
        [Tooltip("Sink speed while released.")]
        public float fallSpeed;
        [Tooltip("How fast the velocity reaches its target (units/s^2). Higher = more responsive.")]
        public float verticalAcceleration;
    }
}
