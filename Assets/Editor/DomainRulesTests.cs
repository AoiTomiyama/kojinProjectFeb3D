using System;
using NUnit.Framework;

/// <summary>Editorや入力に依存しないゲームルールの境界と失敗時の状態を検証する。</summary>
public class DomainRulesTests
{
    [Test]
    public void ExperienceCommitsMultipleLevelsAndSaturatesWithoutOverflow()
    {
        var thresholds = new[] { 10, 20, int.MaxValue };
        var state = new ExperienceProgressionDomain(thresholds);
        thresholds[0] = 1;
        Assert.AreEqual(0, state.Gain(9));
        Assert.AreEqual(0.9f, state.Progress, 0.0001f);
        Assert.AreEqual(2, state.Gain(21));
        Assert.AreEqual(2, state.AvailableUpgradeCount);
        Assert.AreEqual(0, state.CurrentExperience);
        state.Gain(int.MaxValue - 1);
        Assert.AreEqual(1, state.Gain(int.MaxValue));
        Assert.IsTrue(state.IsAtMaxLevel);
        Assert.AreEqual(1f, state.Progress);
        Assert.AreEqual(0, state.CurrentExperience);
        Assert.AreEqual(0, state.Gain(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Gain(-1));
        for (int i = 0; i < 3; i++) Assert.IsTrue(state.TrySpendUpgradeChoice());
        Assert.IsFalse(state.TrySpendUpgradeChoice());
    }

    [Test]
    public void InvalidExperienceSettingsFailBeforeCreatingState()
    {
        Assert.Throws<ArgumentNullException>(() => new ExperienceProgressionDomain(null));
        Assert.Throws<ArgumentException>(() => new ExperienceProgressionDomain(Array.Empty<int>()));
        Assert.Throws<ArgumentException>(() => new ExperienceProgressionDomain(new[] { 1, 0 }));
        Assert.Throws<ArgumentException>(() => new ExperienceProgressionDomain(new[] { -1 }));
        var state = new ExperienceProgressionDomain(new[] { 10 });
        Assert.IsFalse(state.TrySpendUpgradeChoice());
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Gain(-1));
        Assert.AreEqual(0, state.Level);
        Assert.AreEqual(0, state.Gain(0));
        Assert.AreEqual(1, state.Gain(10));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void TooFewCandidatesProduceNoSelection(int count)
    {
        var state = new UpgradeCandidateSelectionDomain();
        Assert.IsFalse(state.TryDraw(new int[count], (min, max) => min, out var result));
        Assert.IsEmpty(result);
    }

    [Test]
    public void DrawingIsDistinctAndPreservesCandidateSource()
    {
        var state = new UpgradeCandidateSelectionDomain();
        var ids = new[] { 10, 20, 30, 40 };
        Assert.IsTrue(state.TryDraw(ids, (min, max) => max - 1, out var result));
        CollectionAssert.AreEquivalent(new[] { 40, 10, 20 }, result);
        CollectionAssert.AreEqual(new[] { 10, 20, 30, 40 }, ids);
        Assert.Throws<ArgumentNullException>(() => state.TryDraw(null, (min, max) => min, out _));
        Assert.Throws<ArgumentNullException>(() => state.TryDraw(ids, null, out _));
        Assert.Throws<ArgumentException>(() => state.TryDraw(new[] { 1, 1, 2 }, (min, max) => min, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.TryDraw(ids, (min, max) => max, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.TryDraw(ids, (min, max) => min - 1, out _));
    }

    [Test]
    public void RerollChargesOnlyAfterSuccessfulSelection()
    {
        var state = new UpgradeCandidateSelectionDomain();
        var ids = new[] { 1, 2, 3 };
        for (int i = 0; i < 2; i++) state.GrantRerollToken();
        Assert.IsFalse(state.TryReroll(ids, (min, max) => throw new Exception("抽選してはいけない"), out _));
        Assert.AreEqual(2, state.RerollTokens);
        state.GrantRerollToken();
        Assert.IsFalse(state.TryReroll(new[] { 1, 2 }, (min, max) => min, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.TryReroll(ids, (min, max) => max, out _));
        Assert.AreEqual(3, state.RerollTokens);
        Assert.IsTrue(state.TryReroll(ids, (min, max) => min, out var result));
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual(0, state.RerollTokens);
    }

    [TestCase(-3, 1)] [TestCase(0, 1)] [TestCase(5, 5)]
    public void AmmoCapacityAndRemainingAreClamped(int capacity, int expected)
    {
        var state = new WeaponAmmoStateDomain(capacity, 0.4f, 2f);
        Assert.AreEqual(expected, state.Capacity);
        Assert.AreEqual(expected, state.RemainingAmmo);
        state.SetRemainingAmmo(int.MaxValue);
        Assert.AreEqual(expected, state.RemainingAmmo);
        state.SetRemainingAmmo(-1);
        Assert.AreEqual(0, state.RemainingAmmo);
        Assert.IsFalse(state.TryConsumeShot());
    }

    [Test]
    public void AmmoWaitRestartAndUpgradeUseCurrentState()
    {
        var state = new WeaponAmmoStateDomain(2, 0.4f, 2f);
        Assert.Throws<InvalidOperationException>(() => state.CompleteWait());
        Assert.IsTrue(state.TryConsumeShot());
        Assert.AreEqual(WeaponWaitKindDomain.Cooldown, state.StartWait());
        Assert.IsFalse(state.IsReady);
        Assert.AreEqual(0.4f, state.PendingWaitSeconds);
        Assert.IsFalse(state.CompleteWait());
        Assert.AreEqual(1, state.RemainingAmmo);
        state.TryConsumeShot();
        Assert.AreEqual(WeaponWaitKindDomain.Reload, state.StartWait());
        Assert.AreEqual(WeaponWaitKindDomain.Reload, state.StartWait());
        Assert.AreEqual(2f, state.PendingWaitSeconds);
        Assert.IsTrue(state.CompleteWait());
        Assert.AreEqual(2, state.RemainingAmmo);
        Assert.IsTrue(state.IsReady);
        state.SetRemainingAmmo(0);
        state.StartWait();
        state.SetCapacity(4);
        Assert.IsFalse(state.CompleteWait());
        Assert.AreEqual(4, state.RemainingAmmo);
        Assert.AreEqual(WeaponWaitKindDomain.None, state.PendingWait);
        Assert.AreEqual(0f, state.PendingWaitSeconds);
        var other = new WeaponAmmoStateDomain(2, 1f, 3f);
        state.TryConsumeShot();
        Assert.AreEqual(2, other.RemainingAmmo);
    }

    [Test]
    public void SpreadClampsShotCountAndKeepsRequestedPatternWhenAmmoRunsOut()
    {
        Assert.AreEqual(0, BulletSpreadCalculatorDomain.GetShotCount(-1, 5));
        Assert.AreEqual(0, BulletSpreadCalculatorDomain.GetShotCount(5, -1));
        Assert.AreEqual(2, BulletSpreadCalculatorDomain.GetShotCount(3, 2));
        Assert.AreEqual(0f, BulletSpreadCalculatorDomain.GetAngle(1, 90f, 0));
        Assert.AreEqual(22.5f, BulletSpreadCalculatorDomain.GetAngle(3, 90f, 0));
        Assert.AreEqual(0f, BulletSpreadCalculatorDomain.GetAngle(3, 90f, 1));
        Assert.AreEqual(-22.5f, BulletSpreadCalculatorDomain.GetAngle(3, 90f, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => BulletSpreadCalculatorDomain.GetAngle(0, 90f, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BulletSpreadCalculatorDomain.GetAngle(3, 90f, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BulletSpreadCalculatorDomain.GetAngle(3, 90f, 3));
    }
}
