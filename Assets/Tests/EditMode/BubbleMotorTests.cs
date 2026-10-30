using BubbleAscent.Gameplay;
using NUnit.Framework;

namespace BubbleAscent.Tests
{
    public class BubbleMotorTests
    {
        const float Dt = 1f / 60f;

        static BubbleSettings Settings() => new BubbleSettings
        {
            minRadius = 0.2f,
            maxRadius = 0.8f,
            startRadius = 0.4f,
            inflateRate = 0.5f,
            deflateRate = 0.5f,
            riseSpeedAtMin = 1f,
            riseSpeedAtMax = 5f,
            fallSpeed = 3f,
            verticalAcceleration = 1000f,
        };

        static void Run(BubbleMotor m, bool held, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) m.Step(held, Dt);
        }

        [Test]
        public void Reset_RestoresStartRadiusHeightAndVelocity()
        {
            var m = new BubbleMotor(Settings());
            Run(m, true, 1f);
            m.Reset(2f);
            Assert.AreEqual(0.4f, m.Radius, 1e-5f);
            Assert.AreEqual(2f, m.Y, 1e-5f);
            Assert.AreEqual(0f, m.VelocityY);
        }

        [Test]
        public void Holding_GrowsRadius_ClampedAtMax()
        {
            var m = new BubbleMotor(Settings());
            m.Step(true, Dt);
            Assert.Greater(m.Radius, 0.4f);
            Run(m, true, 5f);
            Assert.AreEqual(0.8f, m.Radius, 1e-5f);
            Assert.AreEqual(1f, m.NormalizedSize, 1e-5f);
        }

        [Test]
        public void Releasing_ShrinksRadius_ClampedAtMin()
        {
            var m = new BubbleMotor(Settings());
            m.Step(false, Dt);
            Assert.Less(m.Radius, 0.4f);
            Run(m, false, 5f);
            Assert.AreEqual(0.2f, m.Radius, 1e-5f);
            Assert.AreEqual(0f, m.NormalizedSize, 1e-5f);
        }

        [Test]
        public void Holding_Rises_Releasing_Falls()
        {
            var m = new BubbleMotor(Settings());
            Run(m, true, 0.5f);
            Assert.Greater(m.Y, 0f);
            Assert.Greater(m.VelocityY, 0f);

            float top = m.Y;
            Run(m, false, 0.5f);
            Assert.Less(m.Y, top);
            Assert.AreEqual(-3f, m.VelocityY, 1e-4f);
        }

        [Test]
        public void BiggerBubble_RisesFaster()
        {
            var s = Settings();
            s.startRadius = s.minRadius;
            var a = new BubbleMotor(s);
            a.Step(true, Dt);

            var b2 = Settings();
            b2.startRadius = b2.maxRadius;
            var b = new BubbleMotor(b2);
            b.Step(true, Dt);

            Assert.Greater(b.VelocityY, a.VelocityY);
            Assert.AreEqual(5f, b.VelocityY, 1e-4f);
        }

        [Test]
        public void Velocity_ApproachesTarget_AtVerticalAcceleration()
        {
            var s = Settings();
            s.verticalAcceleration = 6f;
            var m = new BubbleMotor(s);
            m.Step(true, Dt);
            Assert.AreEqual(6f * Dt, m.VelocityY, 1e-5f);
        }

        [Test]
        public void ZeroOrNegativeDt_DoesNothing()
        {
            var m = new BubbleMotor(Settings());
            m.Step(true, 0f);
            m.Step(true, -1f);
            Assert.AreEqual(0.4f, m.Radius, 1e-5f);
            Assert.AreEqual(0f, m.Y);
        }

        [Test]
        public void SelfInflate_GrowsTheBubbleFaster_WhenHeld()
        {
            var calm = new BubbleMotor(Settings());
            var thin = new BubbleMotor(Settings());
            calm.Step(true, 0.1f);
            thin.Step(true, 0.1f, 0.5f);
            Assert.Greater(thin.Radius, calm.Radius);
        }

        [Test]
        public void SelfInflate_NeverStopsReleaseFromShrinking_EvenWithBadValues()
        {
            BubbleSettings s = Settings();
            var m = new BubbleMotor(s);
            for (int i = 0; i < 50; i++) m.Step(true, 0.05f); // grow to max
            float before = m.Radius;
            m.Step(false, 0.1f, 1000f); // absurd thin air
            float shrink = before - m.Radius;
            Assert.Greater(shrink, 0f, "release must still shrink");
            Assert.AreEqual(s.deflateRate * (1f - BubbleMotor.MaxSelfInflateShare) * 0.1f, shrink, 1e-4f,
                "thin air is capped at half the deflate rate");
        }
    }
}
