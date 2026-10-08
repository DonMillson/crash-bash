using System;

namespace CrashBashRemake
{
    /// <summary>
    /// See Docs/PS1_Arkenoid_Runtime_Measurements.json for the measured Dingodile
    /// movement override. Other character motion and all unmarked fields remain
    /// provisional. Visuals must never mutate gameplay geometry.
    /// </summary>
    [Serializable]
    public sealed class ArkenoidTuning
    {
        public float wallHalfExtent = 5.81f;
        public float wallThickness = .38f;
        public float goalHalfWidth = 4.1f; // Provisional: must accommodate the measured travel lane.
        public float goalPlane = 6.35f;
        public float defenderLine = 5.44f; // Four observed PS1 defender lines: 2176 / chosen scale 400.
        public float defenderTravel = 3f; // Dingodile observation: -1200..1201; symmetric approximation.
        public float defenderHalfWidth = .725f;
        public float defenderHalfDepth = .24f;
        public float ballRadius = .36f;
        public float ballHeight = .55f;
        public float moveSpeed = 7.5f;
        public float boostMultiplier = 1.5f;
        public float moveAcceleration = 76.5f;
        public float boostAcceleration = 126f;
        public float moveDeceleration = 40.5f;
        public float simulationTickSeconds = 1f / 30f;
        // Default motion above is an unverified fallback for other characters.
        public ArkCharacterMotion[] characterMotion = { ArkCharacterMotion.MeasuredDingodile() };
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

        public ArkMotionSettings MotionFor(CharacterId character)
        {
            if (characterMotion != null)
                foreach (ArkCharacterMotion item in characterMotion)
                    if (item != null && item.character == character) return item.motion;
            return new ArkMotionSettings(moveSpeed, moveSpeed * boostMultiplier,
                moveAcceleration, boostAcceleration, moveDeceleration);
        }

        public void Validate()
        {
            foreach (float value in new[] { wallHalfExtent, wallThickness, goalHalfWidth, goalPlane,
                         defenderLine, defenderTravel, defenderHalfWidth, defenderHalfDepth, ballRadius,
                         ballHeight, moveSpeed, boostMultiplier, launchSpeed, maxBallSpeed, spawnInterval,
                         contactOffsetInfluence, contactMovementInfluence, kickMultiplier, repulseMultiplier,
                         attractionAcceleration, grabReleaseSpeed, tauntSeconds, simulationTickSeconds })
                if (!ArkMath.Finite(value)) throw new ArgumentException("Arkenoid settings must be finite.");
            if (!(wallHalfExtent > goalHalfWidth && goalHalfWidth > defenderTravel + defenderHalfWidth &&
                  goalPlane > wallHalfExtent && defenderLine < wallHalfExtent &&
                  defenderTravel > 0 && defenderHalfWidth > 0 && defenderHalfDepth > 0 &&
                  ballRadius > 0 && wallThickness > 0 && ballHeight >= 0 && moveSpeed > 0 && boostMultiplier >= 1 &&
                  launchSpeed > 0 && maxBallSpeed >= launchSpeed && spawnInterval > 0 &&
                  kickMultiplier > 0 && repulseMultiplier > 0 && attractionAcceleration >= 0 &&
                  grabReleaseSpeed > 0 && tauntSeconds >= 0 && simulationTickSeconds > 0 && simulationTickSeconds <= .1f &&
                  startingScore > 0 && winsNeeded > 0 && maxBalls >= 1 && maxBalls <= 32))
                throw new ArgumentException("Invalid Arkenoid calibration geometry or match settings.");
            MotionFor((CharacterId)(-1)).Validate();
            var characters = new System.Collections.Generic.HashSet<CharacterId>();
            if (characterMotion != null)
                foreach (ArkCharacterMotion item in characterMotion)
                {
                    if (item == null || (int)item.character < 0 || (int)item.character >= 8 || !characters.Add(item.character))
                        throw new ArgumentException("Character movement overrides must have unique valid IDs.");
                    item.motion.Validate();
                }
            foreach (float value in new[] { kickRadius, kickCooldown, kickAnimationSeconds,
                         attractionRadius, grabDistance, launchWarningSeconds, scoredBallDelay,
                         botReactionSeconds, countdownSeconds, roundResultSeconds, deathAnimationSeconds })
                if (!ArkMath.Finite(value) || value < 0)
                    throw new ArgumentException("Arkenoid timings and ranges must be finite and nonnegative.");
        }
    }
}
