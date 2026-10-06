using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LevelUpUIView))]
public class LevelUpCoordinatorGameplay : MonoBehaviour
{
    private ExperienceProgressionDomain _progression;
    private UpgradeCandidateSelectionDomain _candidateSelection;
    private LevelUpUIView _view;
    private int _killCount;

    [SerializeField, Header("次レベルに必要な経験値")]
    private List<int> _requireExpList = new List<int>();

    private int[] _candidateIds;

    PlayerCoreGameplay _player;

    private bool _isMenuActivated;
    public bool IsMenuActivated { get => _isMenuActivated; }
    public int PickCount => _progression.AvailableUpgradeCount;

    public int RerollToken => _candidateSelection.RerollTokens;

    private void Awake()
    {
        _progression = new ExperienceProgressionDomain(_requireExpList);
        _candidateSelection = new UpgradeCandidateSelectionDomain();
        _view = GetComponent<LevelUpUIView>();
        if (_view == null) throw new System.InvalidOperationException($"{name}: LevelUpUIView が同じGameObjectに必要です。");
    }

    private void Start()
    {
        _player = SceneReferenceResolverInfrastructure.RequireUnique<PlayerCoreGameplay>(this);
        _view.Initialize(_isMenuActivated, _progression.Level, _progression.Progress,
            _killCount, RerollToken, PickCount);
        _candidateIds = new int[_view.CandidateCount];
        for (int i = 0; i < _candidateIds.Length; i++) _candidateIds[i] = i;
        Reroll();
    }
    public void GainExperience(int amount)
    {
        // 不正な獲得値なら、撃破数とトークンも途中まで更新しない。
        int levelsGained = _progression.Gain(amount);
        _killCount++;
        _candidateSelection.GrantRerollToken();
        _view.ShowRerollTokens(RerollToken);
        _view.ShowKillCount(_killCount);

        // 進行状態を先に確定し、バー演出にゲームルールの更新を委ねない。
        _view.ShowUpgradeChoices(PickCount);
        _view.ShowProgress(_progression.Level, _progression.Progress, levelsGained > 0);
    }
    public bool TrySpendUpgradeChoice()
    {
        if (!_progression.TrySpendUpgradeChoice()) return false;
        _view.ShowUpgradeChoices(PickCount);
        return true;
    }
    public void OpenCloseUpgradeMenu()
    {
        _isMenuActivated = !_isMenuActivated;
        _view.SetMenuVisible(_isMenuActivated);
    }

    public void ApplyPowerUp(UpgradeParametersConfiguration powerUp)
    {
        _player.ApplyPowerUp(powerUp);
        Reroll();
    }
    private bool Reroll()
    {
        if (!_candidateSelection.TryDraw(_candidateIds, UnityEngine.Random.Range, out var selectedIds))
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
            return false;
        }

        _view.ShowCandidates(selectedIds);
        return true;
    }
    public void TryReroll()
    {
        if (_candidateSelection.TryReroll(_candidateIds, UnityEngine.Random.Range, out var selectedIds))
        {
            _view.ShowCandidates(selectedIds);
            _view.ShowRerollTokens(RerollToken);
        }
        else if (RerollToken >= UpgradeCandidateSelectionDomain.RerollCost)
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
        }
    }
}
