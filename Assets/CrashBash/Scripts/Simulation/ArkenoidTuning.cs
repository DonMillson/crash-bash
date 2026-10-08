using System;

namespace CrashBashRemake
{
    /// <summary>
    /// TEMPORARY calibration values, not recovered PS1 constants. Geometry starts
    /// from the previous Unity scaffold; visuals must never mutate these values.
    /// </summary>
    [Serializable]
    public sealed class ArkenoidTuning
    {
        public float wallHalfExtent = 5.81f;
        public float wallThickness = .38f;
        public float goalHalfWidth = 2.6f;
        public float goalPlane = 6.35f;
        public float defenderLine = 5.45f;
        public float defenderTravel = 2.15f;
        public float defenderHalfWidth = .725f;
        public float defenderHalfDepth = .24f;
        public float ballRadius = .36f;
        public float ballHeight = .55f;
        public float moveSpeed = 7.5f;
        public float boostMultiplier = 1.5f;
        public float launchSpeed = 7f;
        public float maxBallSpeed = 16f;
        public float kickRadius = 1.8f;
        public float kickMultiplier = 1.35f;
        public float kickCooldown = .55f;
        public float kickAnimationSeconds = .22f;
        public float repulseRadius = 2.07f;
        public float repulseMultiplier = 1.55f;
        public float attractionRadius = 1.55f;
        public float attractionAcceleration = 34f;
        public float grabDistance = .84f;
        public float grabReleaseSpeed = 12.8f;
        public float contactOffsetInfluence = .55f;
        public float contactMovementInfluence = .035f;
        public float spawnInterval = 8f;
        public float launchWarningSeconds = .7f;
        public float scoredBallDelay = .65f;
        public float botReactionSeconds = .16f;
        public float countdownSeconds = 2f;
        public float roundResultSeconds = 2.5f;
        public float deathAnimationSeconds = .65f;
        public float tauntSeconds = .8f;
        public int startingScore = 15;
        public int winsNeeded = 3;
        public int maxBalls = 5;
        public int seed = 418;
        // Disabled until ball-to-ball contact and variant ownership are measured.
        public bool allowAttractForCalibration;
        public bool allowCornerPickupsForCalibration;

        public void Validate()
        {
            if (!(wallHalfExtent > goalHalfWidth && goalHalfWidth > ballRadius &&
                  goalPlane > wallHalfExtent && defenderLine < wallHalfExtent &&
                  defenderTravel > 0 && defenderHalfWidth > 0 && defenderHalfDepth > 0 &&
                  ballRadius > 0 && moveSpeed > 0 && boostMultiplier >= 1 &&
                  launchSpeed > 0 && maxBallSpeed >= launchSpeed && spawnInterval > 0 &&
                  startingScore > 0 && winsNeeded > 0 && maxBalls >= 1 && maxBalls <= 32))
                throw new ArgumentException("Invalid Arkenoid calibration geometry or match settings.");
            foreach (float value in new[] { kickRadius, kickCooldown, kickAnimationSeconds,
                         attractionRadius, grabDistance, launchWarningSeconds, scoredBallDelay,
                         botReactionSeconds, countdownSeconds, roundResultSeconds, deathAnimationSeconds })
                if (!ArkMath.Finite(value) || value < 0)
                    throw new ArgumentException("Arkenoid timings and ranges must be finite and nonnegative.");
        }
    }
}
