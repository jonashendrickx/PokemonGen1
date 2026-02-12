namespace PokemonGen1.Core.Battle;

public static class AccuracyCalculator
{
    /// <summary>
    /// Gen 1 accuracy check:
    /// The original game stores accuracy as 0-255 internally (255 = ~99.6% hit).
    /// Our move data stores accuracy as a percentage (0-100), so we scale it.
    /// Effective accuracy = baseAccuracy * AccuracyStageMultiplier / EvasionStageMultiplier
    /// Roll random 0-255, hit if roll &lt; effective accuracy (capped at 255).
    /// </summary>
    public static bool RollAccuracy(int moveAccuracy, int accuracyStage, int evasionStage, Random rng)
    {
        if (moveAccuracy == 0) return true; // Moves with 0 accuracy always hit (Swift, self-targeting)

        // Convert percentage (0-100) to Gen 1 internal scale (0-255)
        // 100% -> 255, 95% -> 242, 90% -> 229, 85% -> 216, etc.
        int baseAccuracy = moveAccuracy * 255 / 100;

        // Stage multipliers: ratio numerator/100 for each stage -6 to +6
        int[] stageNum = { 33, 36, 43, 50, 60, 75, 100, 133, 166, 200, 250, 266, 300 };
        int accIdx = Math.Clamp(accuracyStage + 6, 0, 12);
        int evaIdx = Math.Clamp(evasionStage + 6, 0, 12);

        // Apply stage modifiers
        int threshold = baseAccuracy * stageNum[accIdx] / stageNum[evaIdx];
        threshold = Math.Clamp(threshold, 1, 255);

        return rng.Next(256) < threshold;
    }
}
