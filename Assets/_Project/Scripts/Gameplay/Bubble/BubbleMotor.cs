using System;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// The bubble's rules: holding inflates it and it rises, releasing deflates it and it sinks.
    /// A bigger bubble rises faster. Plain C#: no scene, no input, no frame loop.
    /// </summary>
    public class BubbleMotor
    {
        /// <summary>Fairness: self-inflation (thin air) never takes more than this share of the deflate rate,
        /// so releasing always shrinks the bubble at a useful speed.</summary>
        public const float MaxSelfInflateShare = 0.5f;

        readonly BubbleSettings _settings;

        public float Radius { get; private set; }
        public float Y { get; private set; }
        public float VelocityY { get; private set; }

        /// <summary>0 at min radius, 1 at max radius.</summary>
        public float NormalizedSize
        {
            get
            {
                float range = _settings.maxRadius - _settings.minRadius;
                return range > 0f ? (Radius - _settings.minRadius) / range : 0f;
            }
        }

        public BubbleMotor(BubbleSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Reset(0f);
        }

        /// <summary>Restarts at height y with the start radius and no velocity.</summary>
        public void Reset(float y)
        {
            Radius = Numeric.Clamp(_settings.startRadius, _settings.minRadius, _settings.maxRadius);
            Y = y;
            VelocityY = 0f;
        }

        /// <param name="held">Whether the player is holding.</param>
        /// <param name="dt">Step length in seconds.</param>
        /// <param name="selfInflate">Extra growth per second whatever the input (thin air); capped for fairness.</param>
        public void Step(bool held, float dt, float selfInflate = 0f)
        {
            if (dt <= 0f) return;

            float extra = Numeric.Clamp(selfInflate, 0f, _settings.deflateRate * MaxSelfInflateShare);
            float radiusDelta = (held ? _settings.inflateRate : -_settings.deflateRate) + extra;
            Radius = Numeric.Clamp(Radius + radiusDelta * dt, _settings.minRadius, _settings.maxRadius);

            float target = held
                ? Numeric.Lerp(_settings.riseSpeedAtMin, _settings.riseSpeedAtMax, NormalizedSize)
                : -_settings.fallSpeed;
            VelocityY = Numeric.MoveTowards(VelocityY, target, _settings.verticalAcceleration * dt);
            Y += VelocityY * dt;
        }

    }
}
