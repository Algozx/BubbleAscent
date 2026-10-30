using System;

namespace BubbleAscent.Gameplay
{
    public enum RunState
    {
        Ready,
        Playing,
        Popped,
        Paused,
    }

    /// <summary>
    /// A play session: consecutive climbs, the self-rising camera, obstacles, popping, restarting and the best height.
    /// Plain C#; the owner calls Tick once per frame.
    /// </summary>
    public class RunSession
    {
        // Unity caps a frame at Time.maximumDeltaTime (1/3 s); 16 steps keep each under ~0.021 s.
        const int MaxSubsteps = 16;

        readonly AscentSettings _ascent;
        readonly RunSettings _run;
        readonly ObstacleSettings _obstacles;
        readonly JourneySettings _journey;
        int _seed;
        float _sincePop;

        public BubbleMotor Bubble { get; }
        public ObstacleField Obstacles { get; }

        /// <summary>Seconds since the current run started (drives crosser motion).</summary>
        public float RunTime { get; private set; }
        public RunState State { get; private set; }

        /// <summary>World y of the camera center.</summary>
        public float ViewCenterY { get; private set; }
        public float ViewBottom => ViewCenterY - _ascent.viewHalfHeight;

        /// <summary>Highest bubble y in the current run.</summary>
        public float PeakHeight { get; private set; }
        public float Best { get; private set; }

        /// <summary>Sea until the bubble first rises past the surface in this run, then sky.</summary>
        public JourneyPhase Phase { get; private set; }

        /// <summary>True when the last pop beat an earlier record (never on the very first run).</summary>
        public bool LastPopWasNewBest { get; private set; }

        /// <summary>Camera climb speed: grows with the peak height, up to a cap.</summary>
        public float ClimbSpeed =>
            Math.Min(_ascent.maxClimbSpeed, _ascent.startClimbSpeed + _ascent.climbSpeedPerMeter * PeakHeight);

        public event Action Popped;

        /// <summary>True once a popped run may be restarted by a press (restartDelay has passed).</summary>
        public bool CanRestart => State == RunState.Popped && _sincePop >= _run.restartDelay;

        /// <param name="seed">First layout seed; each restart uses the next one.</param>
        /// <param name="best">Record carried over from the save (0 = none yet).</param>
        /// <param name="journey">Journey borders; null = an endless sea.</param>
        public RunSession(BubbleSettings bubble, AscentSettings ascent, ObstacleSettings obstacles,
            RunSettings run, int seed, float best = 0f, JourneySettings journey = null)
        {
            _journey = journey;
            Best = best > 0f ? best : 0f;
            _ascent = ascent ?? throw new ArgumentNullException(nameof(ascent));
            _obstacles = obstacles ?? throw new ArgumentNullException(nameof(obstacles));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            Bubble = new BubbleMotor(bubble);
            Obstacles = new ObstacleField(obstacles, bubble);
            _seed = seed;
            PrepareRun();
        }

        /// <param name="held">Is the player holding?</param>
        /// <param name="pressed">Did the hold begin this frame? (starts, resumes and restarts)</param>
        /// <param name="dt">Frame time in seconds.</param>
        public void Tick(bool held, bool pressed, float dt)
        {
            if (dt <= 0f) return;

            switch (State)
            {
                case RunState.Ready:
                    if (pressed) Start();
                    break;
                case RunState.Paused:
                    if (pressed) State = RunState.Playing;
                    break;
                case RunState.Popped:
                    _sincePop += dt;
                    if (pressed && CanRestart)
                    {
                        _seed++;
                        PrepareRun();
                        Start();
                    }
                    break;
            }

            if (State == RunState.Playing) Simulate(held, dt);
        }

        /// <summary>Freezes a run in progress (app went to background); a new press resumes it.</summary>
        public void Pause()
        {
            if (State == RunState.Playing) State = RunState.Paused;
        }

        void Simulate(bool held, float dt)
        {
            int steps = 1;
            if (_run.maxStepDt > 0f)
                steps = Math.Min(MaxSubsteps, (int)Math.Ceiling(dt / _run.maxStepDt));
            float h = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                RunTime += h;
                float thinAir = Phase == JourneyPhase.Sky && _journey != null ? _journey.ThinAirAt(Bubble.Y) : 0f;
                Bubble.Step(held, h, thinAir);
                if (Bubble.Y > PeakHeight) PeakHeight = Bubble.Y;
                UpdatePhase();
                ViewCenterY = Math.Max(ViewCenterY + ClimbSpeed * h, Bubble.Y - _ascent.followAbove);
                Obstacles.EnsureAbove(ViewCenterY + _ascent.viewHalfHeight + _obstacles.generateAbove);
                if (Bubble.Y < ViewBottom || Obstacles.Overlaps(0f, Bubble.Y, Bubble.Radius, RunTime))
                {
                    Pop();
                    break;
                }
            }
            Obstacles.TrimBelow(ViewBottom - _obstacles.keepBelow);
        }

        void UpdatePhase()
        {
            if (Phase != JourneyPhase.Sea || _journey == null || _journey.surfaceHeight <= 0f ||
                PeakHeight < _journey.surfaceHeight) return;
            Phase = JourneyPhase.Sky;
        }

        void PrepareRun()
        {
            Bubble.Reset(0f);
            ViewCenterY = _ascent.startBelowCenter;
            RunTime = 0f;
            Obstacles.Reset(_seed);
            Obstacles.EnsureAbove(ViewCenterY + _ascent.viewHalfHeight + _obstacles.generateAbove);
            PeakHeight = 0f;
            Phase = JourneyPhase.Sea;
            _sincePop = 0f;
            State = RunState.Ready;
        }

        void Start() => State = RunState.Playing;

        void Pop()
        {
            State = RunState.Popped;
            _sincePop = 0f;
            LastPopWasNewBest = Best > 0f && PeakHeight > Best;
            if (PeakHeight > Best) Best = PeakHeight;
            Popped?.Invoke();
        }
    }
}
