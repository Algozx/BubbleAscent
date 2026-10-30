using BubbleAscent.Gameplay;
using UnityEngine;

namespace BubbleAscent.Data
{
    /// <summary>
    /// Every balance number of the game in one asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Ascent/Game Tuning", fileName = "GameTuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("Bubble")]
        public BubbleSettings bubble = new BubbleSettings();

        [Header("Ascent")]
        public AscentSettings ascent = new AscentSettings();

        [Header("Obstacles")]
        public ObstacleSettings obstacles = new ObstacleSettings();

        [Header("Run")]
        public RunSettings run = new RunSettings();

        [Header("Journey")]
        public JourneySettings journey = new JourneySettings();

        // Keeps designer edits inside the ranges the rules assume.
        void OnValidate()
        {
            var b = bubble;
            b.minRadius = Mathf.Max(0f, b.minRadius);
            b.maxRadius = Mathf.Max(b.minRadius, b.maxRadius);
            b.startRadius = Mathf.Clamp(b.startRadius, b.minRadius, b.maxRadius);
            b.inflateRate = Mathf.Max(0f, b.inflateRate);
            b.deflateRate = Mathf.Max(0f, b.deflateRate);
            b.fallSpeed = Mathf.Max(0f, b.fallSpeed);
            b.verticalAcceleration = Mathf.Max(0f, b.verticalAcceleration);

            var a = ascent;
            a.viewHalfHeight = Mathf.Max(1f, a.viewHalfHeight);
            float room = Mathf.Max(0f, a.viewHalfHeight - b.maxRadius);
            a.startBelowCenter = Mathf.Clamp(a.startBelowCenter, 0f, room);
            a.followAbove = Mathf.Clamp(a.followAbove, 0f, room);
            a.startClimbSpeed = Mathf.Max(0f, a.startClimbSpeed);
            a.climbSpeedPerMeter = Mathf.Max(0f, a.climbSpeedPerMeter);
            a.maxClimbSpeed = Mathf.Max(a.startClimbSpeed, a.maxClimbSpeed);
            if (a.maxClimbSpeed >= b.riseSpeedAtMax)
                Debug.LogWarning("GameTuning: maxClimbSpeed >= riseSpeedAtMax: the camera can outrun the bubble.", this);

            var o = obstacles;
            o.hardestAtHeight = Mathf.Max(0f, o.hardestAtHeight);
            o.firstObstacleHeight = Mathf.Max(0f, o.firstObstacleHeight);
            o.gateChanceEasy = Mathf.Clamp01(o.gateChanceEasy);
            o.gateChanceHard = Mathf.Clamp01(o.gateChanceHard);
            o.gateThickness = Mathf.Max(0.05f, o.gateThickness);
            o.gateMargin = Mathf.Max(0f, o.gateMargin);
            o.gateGapHard = Mathf.Max(ObstacleField.FairGap(o, b), o.gateGapHard);
            o.gateGapEasy = Mathf.Max(o.gateGapHard, o.gateGapEasy);
            o.crosserWidth = Mathf.Max(0.05f, o.crosserWidth);
            o.crosserThickness = Mathf.Max(0.05f, o.crosserThickness);
            o.crosserMargin = Mathf.Max(0f, o.crosserMargin);
            o.minSpacing = Mathf.Max(ObstacleField.FairSpacing(o, b), o.minSpacing);
            o.spacingHard = Mathf.Max(o.minSpacing, o.spacingHard);
            o.spacingEasy = Mathf.Max(o.spacingHard, o.spacingEasy);
            o.crosserPatrolHalfWidth = Mathf.Max(ObstacleField.FairPatrolFloor(o, b), o.crosserPatrolHalfWidth);
            o.crosserSpeedEasy = Mathf.Max(0f, o.crosserSpeedEasy);
            o.crosserSpeedHard = Mathf.Max(o.crosserSpeedEasy, o.crosserSpeedHard);
            o.generateAbove = Mathf.Max(0f, o.generateAbove);
            o.keepBelow = Mathf.Max(0f, o.keepBelow);

            var r = run;
            r.restartDelay = Mathf.Max(0f, r.restartDelay);
            r.maxStepDt = Mathf.Max(0.001f, r.maxStepDt);

            journey.surfaceHeight = Mathf.Max(0f, journey.surfaceHeight);
            journey.thinAirFullAbove = Mathf.Max(0f, journey.thinAirFullAbove);
            journey.thinAirInflate = Mathf.Max(0f, journey.thinAirInflate); // the fairness cap is applied at runtime
        }
    }
}
