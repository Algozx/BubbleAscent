using BubbleAscent.Gameplay;
using NUnit.Framework;

namespace BubbleAscent.Tests
{
    public class ObstacleFieldTests
    {
        internal static ObstacleSettings Settings() => new ObstacleSettings
        {
            hardestAtHeight = 200f,
            firstObstacleHeight = 6f,
            spacingEasy = 5f,
            spacingHard = 3f,
            minSpacing = 2.5f,
            gateChanceEasy = 0.3f,
            gateChanceHard = 0.5f,
            gateThickness = 0.8f,
            gateGapEasy = 1.4f,
            gateGapHard = 0.75f,
            gateMargin = 0.2f,
            crosserWidth = 1.2f,
            crosserThickness = 0.5f,
            crosserPatrolHalfWidth = 2.6f,
            crosserMargin = 0.2f,
            crosserSpeedEasy = 1.5f,
            crosserSpeedHard = 3.5f,
            generateAbove = 2f,
            keepBelow = 2f,
        };

        internal static BubbleSettings Bubble() => new BubbleSettings
        {
            minRadius = 0.2f,
            maxRadius = 0.8f,
            startRadius = 0.35f,
            inflateRate = 0.5f,
            deflateRate = 2f,
            riseSpeedAtMin = 1.5f,
            riseSpeedAtMax = 5f,
            fallSpeed = 3.5f,
            verticalAcceleration = 8f,
        };

        [Test]
        public void Fairness_Holds_Over2000m()
        {
            var s = Settings();
            var b = Bubble();
            var f = new ObstacleField(s, b);
            f.Reset(123);
            f.EnsureAbove(2000f);
            Assert.Greater(f.Obstacles.Count, 400);

            int gates = 0, crossers = 0;
            for (int i = 0; i < f.Obstacles.Count; i++)
            {
                Obstacle o = f.Obstacles[i];
                if (i > 0)
                    Assert.GreaterOrEqual(o.Y - f.Obstacles[i - 1].Y, s.minSpacing - 1e-4f, "spacing");

                if (o.Kind == ObstacleKind.Gate)
                {
                    gates++;
                    Assert.GreaterOrEqual(o.GapHalf * 2f, 2f * b.minRadius + s.gateMargin - 1e-4f, "gate fits min bubble");
                }
                else
                {
                    crossers++;
                    Assert.GreaterOrEqual(o.PatrolHalf, o.HalfWidth + b.maxRadius + s.crosserMargin - 1e-4f,
                        "crosser leaves a window for the max bubble");
                }
            }
            Assert.Greater(gates, 0);
            Assert.Greater(crossers, 0);
        }

        [Test]
        public void Fairness_BubbleCapabilitiesAlwaysSuffice()
        {
            // The invariant, checked against what the bubble can actually do: the smallest bubble fits through
            // every gate, and the largest one has a moment when it clears every crosser.
            var b = Bubble();
            var f = new ObstacleField(Settings(), b);
            f.Reset(77);
            f.EnsureAbove(2000f);
            for (int i = 0; i < f.Obstacles.Count; i++)
            {
                Obstacle o = f.Obstacles[i];
                if (o.Kind == ObstacleKind.Gate)
                {
                    Assert.IsFalse(f.Overlaps(0f, o.Y, b.minRadius, 0f), $"min bubble passes gate {i}");
                    continue;
                }
                bool clears = false;
                float period = 4f * o.PatrolHalf / o.Speed;
                for (float t = 0f; t < period && !clears; t += period / 200f)
                    clears = !f.Overlaps(0f, o.Y, b.maxRadius, t);
                Assert.IsTrue(clears, $"max bubble clears crosser {i}");
            }
        }

        [Test]
        public void Fairness_HoldsEvenWithBadSettings()
        {
            var s = Settings();
            s.minSpacing = 0f;
            s.spacingHard = 0.01f;
            s.gateGapHard = 0f;
            s.crosserPatrolHalfWidth = 0f;
            var b = Bubble();
            var f = new ObstacleField(s, b);
            f.EnsureAbove(500f);
            float fairSpacing = System.Math.Max(s.gateThickness, s.crosserThickness) + 2f * b.maxRadius;
            for (int i = 0; i < f.Obstacles.Count; i++)
            {
                Obstacle o = f.Obstacles[i];
                if (i > 0)
                    Assert.GreaterOrEqual(o.Y - f.Obstacles[i - 1].Y, fairSpacing - 1e-4f, "room between obstacles");
                if (o.Kind == ObstacleKind.Gate)
                    Assert.GreaterOrEqual(o.GapHalf * 2f, 2f * b.minRadius + s.gateMargin - 1e-4f);
                else
                    Assert.GreaterOrEqual(o.PatrolHalf, o.HalfWidth + b.maxRadius + s.crosserMargin - 1e-4f);
            }
        }

        [Test]
        public void Curve_HardensWithHeight()
        {
            var f = new ObstacleField(Settings(), Bubble());
            Assert.AreEqual(0f, f.Difficulty(0f), 1e-5f);
            Assert.AreEqual(0.5f, f.Difficulty(100f), 1e-5f);
            Assert.AreEqual(1f, f.Difficulty(1000f), 1e-5f);
            Assert.AreEqual(5f, f.SpacingAt(0f), 1e-4f);
            Assert.AreEqual(3f, f.SpacingAt(500f), 1e-4f);
            Assert.AreEqual(1.4f, f.GateGapAt(0f), 1e-4f);
            Assert.AreEqual(0.75f, f.GateGapAt(500f), 1e-4f);
        }

        [Test]
        public void SameSeed_SameLayout()
        {
            var a = new ObstacleField(Settings(), Bubble());
            var b = new ObstacleField(Settings(), Bubble());
            a.Reset(9);
            b.Reset(9);
            a.EnsureAbove(300f);
            b.EnsureAbove(300f);
            Assert.AreEqual(a.Obstacles.Count, b.Obstacles.Count);
            for (int i = 0; i < a.Obstacles.Count; i++)
            {
                Assert.AreEqual(a.Obstacles[i].Kind, b.Obstacles[i].Kind);
                Assert.AreEqual(a.Obstacles[i].Phase, b.Obstacles[i].Phase);
            }
        }

        [Test]
        public void TrimBelow_DropsPassedObstacles()
        {
            var f = new ObstacleField(Settings(), Bubble());
            f.EnsureAbove(60f);
            int count = f.Obstacles.Count;
            f.EnsureAbove(60f);
            Assert.AreEqual(count, f.Obstacles.Count, "nothing new below the generated height");
            f.TrimBelow(30f);
            Assert.Less(f.Obstacles.Count, count);
            Assert.Greater(f.Obstacles[0].Y + f.Obstacles[0].HalfThickness, 30f);
        }

        [Test]
        public void Gate_SmallPasses_BigHits()
        {
            var s = Settings();
            s.gateChanceEasy = s.gateChanceHard = 1f;
            s.firstObstacleHeight = 10f;
            s.gateGapEasy = s.gateGapHard = 1f;
            var f = new ObstacleField(s, Bubble());
            f.EnsureAbove(10f);
            Assert.AreEqual(ObstacleKind.Gate, f.Obstacles[0].Kind);

            Assert.IsFalse(f.Overlaps(0f, 10f, 0.45f, 0f), "radius 0.45 < gap half 0.5");
            Assert.IsTrue(f.Overlaps(0f, 10f, 0.6f, 0f), "radius 0.6 touches the rocks");
            Assert.IsTrue(f.Overlaps(0f, 10f - 0.4f - 0.3f, 0.6f, 0f), "touching the rocks' underside corners");
            Assert.IsFalse(f.Overlaps(0f, 10f - 0.4f - 0.7f, 0.6f, 0f), "below the rocks");
        }

        [Test]
        public void Crosser_PatrolsAndHitsOnlyWhenInTheColumn()
        {
            var o = new Obstacle
            {
                Kind = ObstacleKind.Crosser, Y = 5f, HalfThickness = 0.25f, HalfWidth = 0.6f,
                PatrolHalf = 2f, Speed = 1f, Phase = 0f,
            };
            Assert.AreEqual(-2f, ObstacleField.CrosserX(o, 0f), 1e-4f);
            Assert.AreEqual(0f, ObstacleField.CrosserX(o, 2f), 1e-4f);
            Assert.AreEqual(2f, ObstacleField.CrosserX(o, 4f), 1e-4f);
            Assert.AreEqual(0f, ObstacleField.CrosserX(o, 6f), 1e-4f, "comes back");
            Assert.AreEqual(-2f, ObstacleField.CrosserX(o, 8f), 1e-4f);
            Assert.AreEqual(0f, ObstacleField.CrosserX(o, -2f), 1e-4f, "negative time wraps");
            Assert.AreEqual(2f, ObstacleField.CrosserX(o, 8f * 1000f + 4f), 1e-2f, "large time wraps");
            o.Phase = 16f + 2f; // more than one period ahead
            Assert.AreEqual(0f, ObstacleField.CrosserX(o, 0f), 1e-4f);

            var s = Settings();
            s.gateChanceEasy = s.gateChanceHard = 0f;
            s.firstObstacleHeight = 5f;
            var f = new ObstacleField(s, Bubble());
            f.EnsureAbove(5f);
            Obstacle c = f.Obstacles[0];
            // Find a time when the crosser is at the far edge, and one when it is centered.
            float period = 4f * c.PatrolHalf / c.Speed;
            float tEdge = 0f, tCenter = 0f;
            for (float t = 0f; t < period; t += 0.001f)
            {
                float x = ObstacleField.CrosserX(c, t);
                if (x > c.PatrolHalf - 0.01f) tEdge = t;
                if (System.Math.Abs(x) < 0.01f) tCenter = t;
            }
            Assert.IsFalse(f.Overlaps(0f, 5f, 0.8f, tEdge), "max bubble is safe while the crosser is at the edge");
            Assert.IsTrue(f.Overlaps(0f, 5f, 0.2f, tCenter), "even the min bubble is hit when it is centered");
        }
    }
}
