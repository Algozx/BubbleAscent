using System;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Ascent numbers: what the screen shows and how fast the camera rises on its own.
    /// </summary>
    [Serializable]
    public class AscentSettings
    {
        [Header("View")]
        [Tooltip("Visible half-height in world units. Sets the camera size and the pop line at the bottom.")]
        public float viewHalfHeight;
        [Tooltip("At the start, how far below the screen center the bubble sits.")]
        public float startBelowCenter;
        [Tooltip("How far above the screen center the bubble can go before the camera follows it.")]
        public float followAbove;

        [Header("Self-rising camera (units/s)")]
        [Tooltip("Camera speed at the start of a run.")]
        public float startClimbSpeed;
        [Tooltip("Camera speed gained per meter of height reached.")]
        public float climbSpeedPerMeter;
        [Tooltip("Top camera speed. Must stay below the bubble's top rise speed.")]
        public float maxClimbSpeed;
    }
}
