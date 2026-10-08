using System;

namespace CrashBashRemake
{
    [Serializable]
    public struct ArkMotionSettings
    {
        public float speed, sprintSpeed, acceleration, sprintAcceleration, deceleration;
        public ArkMotionSettings(float normal, float sprint, float ramp, float sprintRamp, float coast)
        { speed = normal; sprintSpeed = sprint; acceleration = ramp; sprintAcceleration = sprintRamp; deceleration = coast; }
        public void Validate()
        {
            foreach (float value in new[] { speed, sprintSpeed, acceleration, sprintAcceleration, deceleration })
                if (!ArkMath.Finite(value) || value <= 0) throw new ArgumentException("Motion settings must be finite and positive.");
            if (sprintSpeed < speed) throw new ArgumentException("Sprint speed cannot be lower than normal speed.");
        }
    }

    [Serializable]
    public sealed class ArkCharacterMotion
    {
        public CharacterId character;
        public ArkMotionSettings motion;
        // Scope: NTSC-U Crashball, P1 Dingodile, Medium, no held ball or kick.
        // 30 logic ticks/s, 400 original coordinate units per Unity unit (chosen scale).
        // This is measured behavior, not a recovered AR_MoveHero function body.
        public static ArkCharacterMotion MeasuredDingodile() => new ArkCharacterMotion {
            character = CharacterId.Dingodile,
            motion = new ArkMotionSettings(7.8f, 12f, 76.5f, 126f, 40.5f)
        };
    }
}
