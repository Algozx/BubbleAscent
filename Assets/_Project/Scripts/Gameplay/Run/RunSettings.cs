using System;
using UnityEngine;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// Run numbers: restart delay and simulation step.
    /// </summary>
    [Serializable]
    public class RunSettings
    {
        [Tooltip("Time after popping before a press can restart (prevents accidental restarts).")]
        public float restartDelay;
        [Tooltip("Largest simulation step; slow frames are split into smaller steps.")]
        public float maxStepDt;
    }
}
