using System;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Obstacle numbers: spacing, crossers, gates and how everything hardens with height.
    /// </summary>
    [Serializable]
    public class ObstacleSettings
    {
        [Header("Difficulty curve")]
        [Tooltip("Height (m) where difficulty reaches its maximum.")]
        public float hardestAtHeight;

        [Header("Vertical spacing between obstacles")]
        [Tooltip("Height of the first obstacle above the start.")]
        public float firstObstacleHeight;
        [Tooltip("Spacing at the start of the climb.")]
        public float spacingEasy;
        [Tooltip("Spacing at maximum difficulty. Never below the fair minimum.")]
        public float spacingHard;
        [Tooltip("Smallest spacing considered fair (time to change size between obstacles).")]
        public float minSpacing;

        [Header("Mix")]
        [Tooltip("Chance (0-1) that an obstacle is a gate at the start of the climb.")]
        public float gateChanceEasy;
        [Tooltip("Chance (0-1) that an obstacle is a gate at maximum difficulty.")]
        public float gateChanceHard;

        [Header("Gate")]
        [Tooltip("Vertical thickness of the gate's walls.")]
        public float gateThickness;
        [Tooltip("Gap width at the start of the climb.")]
        public float gateGapEasy;
        [Tooltip("Gap width at maximum difficulty.")]
        public float gateGapHard;
        [Tooltip("Minimum clearance of the gap beyond the smallest bubble's diameter (fairness).")]
        public float gateMargin;

        [Header("Crosser")]
        [Tooltip("Width of the crosser.")]
        public float crosserWidth;
        [Tooltip("Vertical thickness of the crosser.")]
        public float crosserThickness;
        [Tooltip("How far (+-x) the crosser travels before turning back.")]
        public float crosserPatrolHalfWidth;
        [Tooltip("Minimum clearance between the crosser at its extreme and the largest bubble (fairness).")]
        public float crosserMargin;
        [Tooltip("Crosser speed at the start of the climb (units/s).")]
        public float crosserSpeedEasy;
        [Tooltip("Crosser speed at maximum difficulty (units/s).")]
        public float crosserSpeedHard;

        [Header("Generation")]
        [Tooltip("How far above the camera obstacles are kept generated.")]
        public float generateAbove;
        [Tooltip("How far below the camera obstacles are kept before being discarded.")]
        public float keepBelow;
    }
}
