using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LevelUpUIView))]
public class LevelUpSystemManager : MonoBehaviour
{
    private ExperienceProgression _progression;
    private UpgradeCandidateSelection _candidateSelection;
    private LevelUpUIView _view;
    private int _killCount;

    [SerializeField, Header("次レベルに必要な経験値")]
    private List<int> _requireExpList = new List<int>();

    private int[] _candidateIds;

    PlayerCore _player;

    private bool _isMenuActivated;
    public bool IsMenuActivated { get => _isMenuActivated; }
    public int PickCount => _progression.AvailableUpgradeCount;

    public int RerollToken => _candidateSelection.RerollTokens;

    private void Awake()
    {
        _progression = new ExperienceProgression(_requireExpList);
        _candidateSelection = new UpgradeCandidateSelection();
        _view = GetComponent<LevelUpUIView>();
    }

    private void Start()
    {
        _player = SceneReferenceResolver.RequireUnique<PlayerCore>(this);
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

    public void ApplyPowerUp(PowerUpParameter powerUp)
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
        else if (RerollToken >= UpgradeCandidateSelection.RerollCost)
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
        }
    }
}
