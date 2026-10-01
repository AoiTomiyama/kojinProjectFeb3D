using System;
using System.Collections.Generic;

/// <summary>経験値、レベル、未使用の強化選択回数を管理するゲームルール。</summary>
public sealed class ExperienceProgression
{
    private readonly int[] _requiredExperience;

    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int AvailableUpgradeCount { get; private set; }
    public bool IsAtMaxLevel => Level == _requiredExperience.Length;
    public float Progress => IsAtMaxLevel ? 1f : (float)CurrentExperience / _requiredExperience[Level];

    public ExperienceProgression(IReadOnlyList<int> requiredExperience)
    {
        if (requiredExperience == null) throw new ArgumentNullException(nameof(requiredExperience));
        if (requiredExperience.Count == 0)
            throw new ArgumentException("必要経験値を1件以上設定してください。", nameof(requiredExperience));

        // Inspector の設定値から複製し、プレイ中に設定が変わっても進行中の状態を変えない。
        _requiredExperience = new int[requiredExperience.Count];
        for (int i = 0; i < requiredExperience.Count; i++)
        {
            if (requiredExperience[i] <= 0)
                throw new ArgumentException($"レベル {i + 1} の必要経験値は正の値で設定してください。", nameof(requiredExperience));
            _requiredExperience[i] = requiredExperience[i];
        }
    }

    /// <returns>今回上がったレベル数。</returns>
    public int Gain(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "獲得経験値は0以上にしてください。");
        if (IsAtMaxLevel) return 0;

        // 大量の経験値が一度に入っても、int の加算結果を溢れさせず全レベルを判定する。
        long remaining = (long)CurrentExperience + amount;
        int levelsGained = 0;
        while (Level < _requiredExperience.Length && remaining >= _requiredExperience[Level])
        {
            remaining -= _requiredExperience[Level];
            Level++;
            levelsGained++;
        }

        AvailableUpgradeCount += levelsGained;
        // 最終レベルでは余剰経験値を蓄積しない。バーは Progress で満タンになる。
        CurrentExperience = IsAtMaxLevel ? 0 : (int)remaining;
        return levelsGained;
    }

    public bool TrySpendUpgradeChoice()
    {
        if (AvailableUpgradeCount == 0) return false;
        AvailableUpgradeCount--;
        return true;
    }
}
