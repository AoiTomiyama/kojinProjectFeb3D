using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpSystemManager : MonoBehaviour
{
    private ExperienceProgression _progression;
    private UpgradeCandidateSelection _candidateSelection;
    private int _killCount;
    [SerializeField] private Image _expBar;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _killCountText;
    [SerializeField] private TextMeshProUGUI _tokenCountText;
    [SerializeField] private TextMeshProUGUI _pickUpgradeCountText;
    [SerializeField] private Image _upgradePanel;
    [SerializeField] private Image _hasPickupNotice;
    [SerializeField] private VerticalLayoutGroup _buttonLayoutGroup;

    [SerializeField, Header("次レベルに必要な経験値")]
    private List<int> _requireExpList = new List<int>();

    [SerializeField] private Button[] _buttons;
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
    }

    private void Start()
    {
        _player = SceneReferenceResolver.RequireUnique<PlayerCore>(this);
        _upgradePanel.gameObject.SetActive(_isMenuActivated);
        RefreshPickCount();

        // メニューが非表示でも、抽選対象の子ボタンをすべて保持する。
        _buttons = _buttonLayoutGroup.GetComponentsInChildren<Button>(true);
        _candidateIds = new int[_buttons.Length];
        for (int i = 0; i < _candidateIds.Length; i++) _candidateIds[i] = i;
        RefreshRerollToken();
        Reroll();
    }
    public void GainExperience(int amount)
    {
        // 不正な獲得値なら、撃破数とトークンも途中まで更新しない。
        int levelsGained = _progression.Gain(amount);
        _killCount++;
        _candidateSelection.GrantRerollToken();
        RefreshRerollToken();
        _killCountText.text = _killCount.ToString();

        // 進行状態を先に確定し、バー演出にゲームルールの更新を委ねない。
        RefreshPickCount();
        _levelText.text = _progression.Level.ToString();
        _expBar.DOKill();
        if (_progression.IsAtMaxLevel)
        {
            _expBar.fillAmount = 1f;
            return;
        }

        if (levelsGained > 0) _expBar.fillAmount = 0f;
        _expBar.DOFillAmount(_progression.Progress, 0.3f);
    }
    public bool TrySpendUpgradeChoice()
    {
        if (!_progression.TrySpendUpgradeChoice()) return false;
        RefreshPickCount();
        return true;
    }

    private void RefreshPickCount()
    {
        _hasPickupNotice.gameObject.SetActive(PickCount > 0);
        _pickUpgradeCountText.text = PickCount.ToString();
    }
    public void OpenCloseUpgradeMenu()
    {
        _isMenuActivated = !_isMenuActivated;
        _upgradePanel.gameObject.SetActive(_isMenuActivated);
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

        ShowCandidates(selectedIds);
        return true;
    }

    private void ShowCandidates(int[] selectedIds)
    {
        foreach (var button in _buttons)
        {
            button.gameObject.SetActive(false);
        }

        foreach (int id in selectedIds)
        {
            var button = _buttons[id];
            button.gameObject.SetActive(true);
            button.transform.SetAsFirstSibling();
        }
    }
    public void TryReroll()
    {
        if (_candidateSelection.TryReroll(_candidateIds, UnityEngine.Random.Range, out var selectedIds))
        {
            ShowCandidates(selectedIds);
            RefreshRerollToken();
        }
        else if (RerollToken >= UpgradeCandidateSelection.RerollCost)
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
        }
    }

    private void RefreshRerollToken()
    {
        _tokenCountText.text = RerollToken.ToString();
    }
    private void OnDisable()
    {
        if (_expBar != null) _expBar.DOKill();
    }
}
