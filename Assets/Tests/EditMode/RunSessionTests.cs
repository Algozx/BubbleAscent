using System;
using BubbleAscent.Gameplay;
using NUnit.Framework;

namespace BubbleAscent.Tests
{
    public class RunSessionTests
    {
        const float Dt = 1f / 60f;

        static BubbleSettings Bubble() => new BubbleSettings
        {
            minRadius = 0.2f,
            maxRadius = 0.8f,
            startRadius = 0.4f,
            inflateRate = 0.5f,
            deflateRate = 0.5f,
            riseSpeedAtMin = 1f,
            riseSpeedAtMax = 5f,
            fallSpeed = 3f,
            verticalAcceleration = 20f,
        };

        static AscentSettings Ascent() => new AscentSettings
        {
            viewHalfHeight = 5f,
            startBelowCenter = 2f,
            followAbove = 1f,
            startClimbSpeed = 1f,
            climbSpeedPerMeter = 0.1f,
            maxClimbSpeed = 2f,
        };

        /// <summary>Obstacles far above, so the camera/pop tests are not affected.</summary>
        static ObstacleSettings NoObstacles()
        {
            var o = ObstacleFieldTests.Settings();
            o.firstObstacleHeight = 1e6f;
            return o;
        }

        static RunSettings Run() => new RunSettings
        {
            restartDelay = 0.3f,
            maxStepDt = 0.02f,
        };

        static RunSession NewSession(AscentSettings ascent = null, ObstacleSettings obstacles = null) =>
            new RunSession(Bubble(), ascent ?? Ascent(), obstacles ?? NoObstacles(), Run(), 1);

        static void Tick(RunSession s, bool held, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) s.Tick(held, false, Dt);
        }

        static void TickUntilPopped(RunSession s)
        {
            for (int i = 0; i < 1200 && s.State != RunState.Popped; i++) s.Tick(false, false, Dt);
        }

        [Test]
        public void StartsReady_BubbleBelowCenter_PressStartsPlaying()
        {
            var s = NewSession();
            Assert.AreEqual(RunState.Ready, s.State);
            Assert.AreEqual(2f, s.ViewCenterY, 1e-5f);
            Assert.AreEqual(-3f, s.ViewBottom, 1e-5f);
            Assert.AreEqual(0f, s.Bubble.Y);

            s.Tick(false, false, 1f);
            Assert.AreEqual(RunState.Ready, s.State);
            Assert.AreEqual(2f, s.ViewCenterY, 1e-5f, "camera waits in Ready");

            s.Tick(true, true, Dt);
            Assert.AreEqual(RunState.Playing, s.State);
        }

        [Test]
        public void Camera_ClimbsOnItsOwn_AtStartSpeed()
        {
            var a = Ascent();
            a.climbSpeedPerMeter = 0f;
            var s = NewSession(a);
            s.Tick(false, true, 0.5f);
            Assert.AreEqual(2f + 0.5f, s.ViewCenterY, 1e-4f);
        }

        [Test]
        public void ClimbSpeed_GrowsWithPeakHeight_UpToMax()
        {
            var s = NewSession();
            Assert.AreEqual(1f, s.ClimbSpeed, 1e-5f);
            s.Tick(true, true, Dt);
            Tick(s, true, 3f);
            Assert.Greater(s.PeakHeight, 5f);
            Assert.AreEqual(2f, s.ClimbSpeed, 1e-5f, "1 + 0.1·peak, capped at 2");
        }

        [Test]
        public void Camera_FollowsBubble_WhenItGetsAhead()
        {
            var s = NewSession();
            s.Tick(true, true, Dt);
            Tick(s, true, 3f); // bubble rises at up to 5 u/s, faster than the camera's 2 u/s
            Assert.AreEqual(s.Bubble.Y - 1f, s.ViewCenterY, 1e-4f);
        }

        [Test]
        public void FallingBelowTheScreen_Pops_AndRecordsBest()
        {
            var s = NewSession();
            int popped = 0;
            s.Popped += () => popped++;
            s.Tick(true, true, Dt);
            Tick(s, true, 1f);
            TickUntilPopped(s);

            Assert.AreEqual(RunState.Popped, s.State);
            Assert.AreEqual(1, popped);
            Assert.Less(s.Bubble.Y, s.ViewBottom);
            Assert.Greater(s.PeakHeight, s.Bubble.Y + 1f, "peak is the highest point, not where it popped");
            Assert.AreEqual(s.PeakHeight, s.Best, 1e-5f);
        }

        [Test]
        public void StayingSmall_EventuallyFallsBehind()
        {
            // Released, the bubble sinks; the camera never waits.
            var s = NewSession();
            s.Tick(false, true, Dt);
            TickUntilPopped(s);
            Assert.AreEqual(RunState.Popped, s.State);
        }

        [Test]
        public void Popped_FreezesCamera()
        {
            var s = NewSession();
            s.Tick(false, true, Dt);
            TickUntilPopped(s);
            float view = s.ViewCenterY;
            Tick(s, true, 1f);
            Assert.AreEqual(view, s.ViewCenterY);
        }

        [Test]
        public void Restart_IgnoresPressBeforeDelay_ThenRestartsAtBottom()
        {
            var s = NewSession();
            s.Tick(true, true, Dt);
            Tick(s, true, 1f);
            TickUntilPopped(s);
            float best = s.Best;

            s.Tick(true, true, 0.1f);
            Assert.AreEqual(RunState.Popped, s.State, "press before restartDelay is ignored");

            Tick(s, false, 0.3f);
            s.Tick(true, true, Dt);
            Assert.AreEqual(RunState.Playing, s.State);
            Assert.Less(s.PeakHeight, 0.1f);
            Assert.AreEqual(2f, s.ViewCenterY, 0.1f);
            Assert.AreEqual(0.4f, s.Bubble.Radius, 0.02f);
            Assert.AreEqual(best, s.Best);
        }

        [Test]
        public void LongFrame_IsSubstepped_SoThePopHappensAtTheEdge()
        {
            var s = NewSession();
            s.Tick(false, true, Dt);
            while (s.Bubble.Y - s.ViewBottom > 0.3f) s.Tick(false, false, Dt);
            Assert.AreEqual(RunState.Playing, s.State);

            // One 0.3 s hitch: without substeps the bubble would end ~1 u below the edge.
            s.Tick(false, false, 0.3f);
            Assert.AreEqual(RunState.Popped, s.State);
            float overshoot = s.ViewBottom - s.Bubble.Y;
            Assert.Greater(overshoot, 0f);
            Assert.Less(overshoot, 0.15f, "popped within one substep of crossing the edge");
        }

        [Test]
        public void HittingAGate_WhileBig_Pops()
        {
            var o = ObstacleFieldTests.Settings();
            o.gateChanceEasy = o.gateChanceHard = 1f; // only gates
            o.firstObstacleHeight = 4f;
            var s = NewSession(null, o);
            s.Tick(true, true, Dt);
            Tick(s, true, 3f); // holding: grows past the gap and rises into the rocks
            Assert.AreEqual(RunState.Popped, s.State);
            Assert.Less(s.PeakHeight, 4f + 1f);
        }

        [Test]
        public void Restart_UsesANewObstacleLayout()
        {
            var o = ObstacleFieldTests.Settings();
            o.gateChanceEasy = o.gateChanceHard = 0f; // crossers only: the phase comes from the seed
            var s = NewSession(null, o);
            float before = Signature(s.Obstacles);
            s.Tick(false, true, Dt);
            TickUntilPopped(s);
            Tick(s, false, 0.5f);
            s.Tick(true, true, Dt);
            Assert.AreEqual(RunState.Playing, s.State);
            Assert.AreNotEqual(before, Signature(s.Obstacles));
        }

        [Test]
        public void Pause_FreezesTheRun_UntilTheNextPress()
        {
            var s = NewSession();
            s.Tick(true, true, Dt);
            Tick(s, true, 0.5f);
            float y = s.Bubble.Y, view = s.ViewCenterY;

            s.Pause();
            Assert.AreEqual(RunState.Paused, s.State);
            Tick(s, false, 5f); // long background: nothing moves, nothing pops
            Assert.AreEqual(RunState.Paused, s.State);
            Assert.AreEqual(y, s.Bubble.Y);
            Assert.AreEqual(view, s.ViewCenterY);

            s.Tick(true, true, Dt);
            Assert.AreEqual(RunState.Playing, s.State);
            Assert.Greater(s.Bubble.Y, y);
        }

        [Test]
        public void Pause_OutsideAPlayingRun_DoesNothing()
        {
            var s = NewSession();
            s.Pause();
            Assert.AreEqual(RunState.Ready, s.State);
            s.Tick(false, true, Dt);
            TickUntilPopped(s);
            s.Pause();
            Assert.AreEqual(RunState.Popped, s.State);
        }

        [Test]
        public void NewBest_OnlyWhenBeatingAnEarlierRecord()
        {
            var s = NewSession();
            s.Tick(true, true, Dt);
            Tick(s, true, 0.5f);
            TickUntilPopped(s);
            Assert.IsFalse(s.LastPopWasNewBest, "first run has nothing to beat");
            float first = s.Best;

            Tick(s, false, 0.5f);
            s.Tick(true, true, Dt);
            Tick(s, true, 2f); // climbs higher this time
            TickUntilPopped(s);
            Assert.Greater(s.Best, first);
            Assert.IsTrue(s.LastPopWasNewBest);

            Tick(s, false, 0.5f);
            s.Tick(false, true, Dt); // released at once: lower than the record
            TickUntilPopped(s);
            Assert.IsFalse(s.LastPopWasNewBest);
        }

        [Test]
        public void SavedRecord_IsTheBest_AndBeatingItCountsAsNew()
        {
            var s = new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, best: 3f);
            Assert.AreEqual(3f, s.Best);
            s.Tick(true, true, Dt);
            Tick(s, true, 2f); // well above 3 m
            TickUntilPopped(s);
            Assert.Greater(s.Best, 3f);
            Assert.IsTrue(s.LastPopWasNewBest, "beat the saved record on the first run of the session");

            var low = new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, best: 1000f);
            low.Tick(false, true, Dt);
            TickUntilPopped(low);
            Assert.AreEqual(1000f, low.Best);
            Assert.IsFalse(low.LastPopWasNewBest);

            Assert.AreEqual(0f, new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, best: -7f).Best);
        }

        [Test]
        public void CanRestart_OnlyAfterTheDelay()
        {
            var s = NewSession();
            Assert.IsFalse(s.CanRestart);
            s.Tick(false, true, Dt);
            TickUntilPopped(s);
            Assert.IsFalse(s.CanRestart);
            Tick(s, false, 0.31f);
            Assert.IsTrue(s.CanRestart);
            s.Tick(true, true, Dt);
            Assert.IsFalse(s.CanRestart);
        }

        static float Signature(ObstacleField field)
        {
            float sum = 0f;
            for (int i = 0; i < field.Obstacles.Count; i++)
            {
                Obstacle ob = field.Obstacles[i];
                sum += (i + 1) * (ob.Kind == ObstacleKind.Gate ? 100f : ob.Phase);
            }
            return sum;
        }

        [Test]
        public void CrossingTheSurface_SwitchesToSky_AndStaysThere()
        {
            var s = new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, 0f,
                new JourneySettings { surfaceHeight = 2f });
            s.Tick(true, true, Dt);
            Assert.AreEqual(JourneyPhase.Sea, s.Phase);
            for (int i = 0; i < 600 && s.PeakHeight < 4f; i++) s.Tick(true, false, Dt);
            Assert.GreaterOrEqual(s.PeakHeight, 4f, "the bubble should have climbed past the surface");
            Assert.AreEqual(JourneyPhase.Sky, s.Phase);
            Tick(s, false, 0.5f); // sinking back below the surface keeps the sky phase for this run
            Assert.AreEqual(JourneyPhase.Sky, s.Phase);
        }

        [Test]
        public void Restart_GoesBackToTheSea()
        {
            var s = new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, 0f,
                new JourneySettings { surfaceHeight = 2f });
            s.Tick(true, true, Dt);
            for (int i = 0; i < 600 && s.Phase == JourneyPhase.Sea; i++) s.Tick(true, false, Dt);
            Assert.AreEqual(JourneyPhase.Sky, s.Phase);
            TickUntilPopped(s);
            Assert.AreEqual(RunState.Popped, s.State);
            for (int i = 0; i < 100 && !s.CanRestart; i++) s.Tick(false, false, Dt);
            s.Tick(false, true, Dt);
            Assert.AreEqual(RunState.Playing, s.State);
            Assert.AreEqual(JourneyPhase.Sea, s.Phase);
            for (int i = 0; i < 600 && s.Phase == JourneyPhase.Sea; i++) s.Tick(true, false, Dt);
            Assert.AreEqual(JourneyPhase.Sky, s.Phase, "each run can surface again");
        }

        [Test]
        public void WithoutJourney_TheSeaNeverEnds()
        {
            var s = NewSession();
            s.Tick(true, true, Dt);
            Tick(s, true, 5f);
            Assert.Greater(s.PeakHeight, 5f);
            Assert.AreEqual(JourneyPhase.Sea, s.Phase);
        }

        [Test]
        public void SurfaceAtZero_MeansNoSurface()
        {
            var s = new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, 0f,
                new JourneySettings { surfaceHeight = 0f });
            s.Tick(true, true, Dt);
            Tick(s, true, 2f);
            Assert.AreEqual(JourneyPhase.Sea, s.Phase);
        }

        [Test]
        public void ThinAir_StartsAtTheSurface_AndRampsUp()
        {
            var j = new JourneySettings { surfaceHeight = 100f, thinAirInflate = 0.8f, thinAirFullAbove = 200f };
            Assert.AreEqual(0f, j.ThinAirAt(50f), "none in the sea");
            Assert.AreEqual(0f, j.ThinAirAt(100f));
            Assert.AreEqual(0.4f, j.ThinAirAt(200f), 1e-5f);
            Assert.AreEqual(0.8f, j.ThinAirAt(1000f), 1e-5f, "capped");
            Assert.AreEqual(0f, new JourneySettings { surfaceHeight = 0f, thinAirInflate = 0.8f }.ThinAirAt(50f), "no surface, no sky");
        }

        [Test]
        public void InTheSky_ThinAirSlowsTheShrinkWhenReleased()
        {
            float Drop(float thinAir)
            {
                var journey = new JourneySettings { surfaceHeight = 1f, thinAirInflate = thinAir, thinAirFullAbove = 0f };
                var s = new RunSession(Bubble(), Ascent(), NoObstacles(), Run(), 1, 0f, journey);
                s.Tick(true, true, Dt);
                for (int i = 0; i < 600 && s.Phase == JourneyPhase.Sea; i++) s.Tick(true, false, Dt);
                Assert.AreEqual(JourneyPhase.Sky, s.Phase);
                Tick(s, true, 1f); // grow toward max
                float before = s.Bubble.Radius;
                Tick(s, false, 0.2f);
                return before - s.Bubble.Radius;
            }

            float thin = Drop(0.2f), normal = Drop(0f);
            Assert.Greater(thin, 0f, "release still shrinks");
            Assert.Less(thin, normal * 0.75f, "thin air resists shrinking");
        }

        [Test]
        public void NewRun_StartsAtTheBottom()
        {
            var s = NewSession();
            Assert.AreEqual(0f, s.Bubble.Y);
            Assert.AreEqual(0f, s.PeakHeight);
        }
    }
}
