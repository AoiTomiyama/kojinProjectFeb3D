using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LevelUpViewPresentation))]
public class LevelUpCoordinatorGameplay : MonoBehaviour
{
    private ExperienceProgressionDomain _progression;
    private UpgradeCandidateSelectionDomain _candidateSelection;
    private LevelUpViewPresentation _view;
    private int _killCount;

    [SerializeField, Header("次レベルに必要な経験値")]
    private List<int> _requireExpList = new List<int>();

    private int[] _candidateIds;
    [SerializeField] private UpgradeCatalogConfiguration _catalog;
    private UpgradeDefinitionConfiguration[] _definitions;
    private readonly Dictionary<int, UpgradeDefinitionConfiguration> _definitionsById = new Dictionary<int, UpgradeDefinitionConfiguration>();
    private int[] _selectedIds = System.Array.Empty<int>();

    PlayerCoreGameplay _player;

    private bool _isMenuActivated;
    public bool IsMenuActivated { get => _isMenuActivated; }
    public int PickCount => _progression.AvailableUpgradeCount;

    public int RerollToken => _candidateSelection.RerollTokens;

    private void Awake()
    {
        _progression = new ExperienceProgressionDomain(_requireExpList);
        _candidateSelection = new UpgradeCandidateSelectionDomain();
        if (_catalog == null) throw new System.InvalidOperationException($"{name}: 強化一覧が設定されていません。");
        _definitions = _catalog.CreateSnapshot();
        foreach (var definition in _definitions) _definitionsById.Add(definition.Id, definition);
        _view = GetComponent<LevelUpViewPresentation>();
        if (_view == null) throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation が同じGameObjectに必要です。");
    }

    private void Start()
    {
        _player = SceneReferenceResolverInfrastructure.RequireUnique<PlayerCoreGameplay>(this);
        _view.Initialize(_isMenuActivated, _progression.Level, _progression.Progress,
            _killCount, RerollToken, PickCount);
        RefreshUpgradeButtons();
        UpdateCandidateIds();
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
    public void RefreshUpgradeButtons()
    {
        _view.CreateButtons(_definitions, SelectUpgrade);
        _view.ShowCandidates(_selectedIds);
    }
    private void UpdateCandidateIds() => _candidateIds = _definitions.Where(item => item.IsEligible(_progression.Level)).Select(item => item.Id).ToArray();
    private void SelectUpgrade(int id)
    {
        // 古い/非選出ボタンから呼ばれても、現在の候補と残回数をゲーム側で検証する。
        if (!_selectedIds.Contains(id) || !_definitionsById.TryGetValue(id, out var definition)
            || !definition.IsEligible(_progression.Level) || !TrySpendUpgradeChoice()) return;
        // 効果の通知中に同じ候補から再入されないよう、適用前に選出状態を解除する。
        _selectedIds = _selectedIds.Where(selected => selected != id).ToArray();
        _view.HideCandidate(id);
        _player.ApplyPowerUp(definition.Effect);
        Reroll();
    }
    private bool Reroll()
    {
        UpdateCandidateIds();
        if (!_candidateSelection.TryDraw(_candidateIds, UnityEngine.Random.Range, out var selectedIds))
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
            return false;
        }

        _selectedIds = selectedIds;
        _view.ShowCandidates(selectedIds);
        return true;
    }
    public void TryReroll()
    {
        UpdateCandidateIds();
        if (_candidateSelection.TryReroll(_candidateIds, UnityEngine.Random.Range, out var selectedIds))
        {
            _selectedIds = selectedIds;
            _view.ShowCandidates(selectedIds);
            _view.ShowRerollTokens(RerollToken);
        }
        else if (RerollToken >= UpgradeCandidateSelectionDomain.RerollCost)
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
        }
    }
}
