using System;
using System.Collections.Generic;

/// <summary>強化候補の抽選と、成功した再抽選に対するトークン消費を管理する。</summary>
public sealed class UpgradeCandidateSelection
{
    public const int ChoiceCount = 3;
    public const int RerollCost = 3;

    public int RerollTokens { get; private set; }

    public void GrantRerollToken()
    {
        RerollTokens = checked(RerollTokens + 1);
    }

    public bool TryDraw(IReadOnlyList<int> candidateIds, Func<int, int, int> nextIndex, out int[] selectedIds)
    {
        if (candidateIds == null) throw new ArgumentNullException(nameof(candidateIds));
        if (nextIndex == null) throw new ArgumentNullException(nameof(nextIndex));

        selectedIds = Array.Empty<int>();
        if (candidateIds.Count < ChoiceCount) return false;

        var seen = new HashSet<int>();
        var shuffled = new int[candidateIds.Count];
        for (int i = 0; i < candidateIds.Count; i++)
        {
            if (!seen.Add(candidateIds[i]))
                throw new ArgumentException("強化候補IDは重複させないでください。", nameof(candidateIds));
            shuffled[i] = candidateIds[i];
        }

        // 必要な3件だけを部分的にシャッフルし、候補元の配列は変更しない。
        selectedIds = new int[ChoiceCount];
        for (int i = 0; i < ChoiceCount; i++)
        {
            int chosenIndex = nextIndex(i, shuffled.Length);
            if (chosenIndex < i || chosenIndex >= shuffled.Length)
                throw new ArgumentOutOfRangeException(nameof(nextIndex), "乱数は指定した範囲内の添字を返してください。");
            (shuffled[i], shuffled[chosenIndex]) = (shuffled[chosenIndex], shuffled[i]);
            selectedIds[i] = shuffled[i];
        }
        return true;
    }

    public bool TryReroll(IReadOnlyList<int> candidateIds, Func<int, int, int> nextIndex, out int[] selectedIds)
    {
        selectedIds = Array.Empty<int>();
        if (RerollTokens < RerollCost || !TryDraw(candidateIds, nextIndex, out selectedIds)) return false;

        // 候補の決定に失敗したときは残高を変更しない。
        RerollTokens -= RerollCost;
        return true;
    }
}
