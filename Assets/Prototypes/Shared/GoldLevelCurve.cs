using UnityEngine;

[CreateAssetMenu(menuName = "Stack Store/Gold Level Curve")]
public sealed class GoldLevelCurve : ScriptableObject
{
    [Min(1)] public int firstRequiredGold = 3;
    [Min(0)] public int additionalGoldPerLevel = 2;

    public long ThresholdForNextLevel(int currentLevel)
    {
        double n = System.Math.Max(1, currentLevel);
        double threshold = n * System.Math.Max(1, firstRequiredGold) +
            n * (n - 1) / 2 * System.Math.Max(0, additionalGoldPerLevel);
        return threshold >= long.MaxValue ? long.MaxValue : (long)threshold;
    }
}
