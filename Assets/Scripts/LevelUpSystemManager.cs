using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpSystemManager : MonoBehaviour
{
    private int _currentExp;
    private int _currentLevel;
    private int _killCount;
    private int _pickCount;
    private int _rerollToken;
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

    PlayerCore _player;

    private bool _isMenuActivated;
    public bool IsMenuActivated { get => _isMenuActivated; }
    public int PickCount
    {
        get => _pickCount;
        set
        {
            _pickCount = value;
            _hasPickupNotice.gameObject.SetActive(_pickCount > 0);
            _pickUpgradeCountText.text = PickCount.ToString();
        }
    }

    public int RerollToken 
    {
        get => _rerollToken;
        set
        {
            _rerollToken = value;
            _tokenCountText.text = RerollToken.ToString();
        }
    }

    private void Start()
    {
        _player = FindAnyObjectByType<PlayerCore>();
        _upgradePanel.gameObject.SetActive(_isMenuActivated);
        _hasPickupNotice.gameObject.SetActive(_pickCount > 0);

        _buttons = _buttonLayoutGroup.GetComponentsInChildren<Button>(true);
        Reroll();
    }
    public void GainExperience(int amount)
    {
        _killCount++;
        RerollToken++;
        _killCountText.text = _killCount.ToString();

        if (_requireExpList.Count == 0)
        {
            Debug.LogError("必要経験値が設定されていません。", this);
            return;
        }

        // 経験値とレベルを先に確定し、表示アニメーションにゲーム状態を委ねない。
        _currentExp += amount;
        bool leveledUp = false;
        while (_currentLevel < _requireExpList.Count)
        {
            int requiredExp = _requireExpList[_currentLevel];
            if (requiredExp <= 0)
            {
                Debug.LogError("必要経験値は正の値で設定してください。", this);
                return;
            }
            if (_currentExp < requiredExp) break;

            _currentExp -= requiredExp;
            _currentLevel++;
            PickCount++;
            leveledUp = true;
        }

        _levelText.text = _currentLevel.ToString();
        _expBar.DOKill();
        if (_currentLevel >= _requireExpList.Count)
        {
            // 最終レベル到達後は経験値を蓄積せず、バーを満タンに固定する。
            _currentExp = 0;
            _expBar.fillAmount = 1f;
            return;
        }

        if (leveledUp) _expBar.fillAmount = 0f;
        _expBar.DOFillAmount((float)_currentExp / _requireExpList[_currentLevel], 0.3f);
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
        if (_buttons == null || _buttons.Length < 3)
        {
            Debug.LogError("強化候補のボタンが3件未満のため抽選できません。", this);
            return false;
        }

        foreach (var button in _buttons)
        {
            button.gameObject.SetActive(false);
        }

        // 先頭3件だけを部分的にシャッフルし、重複のない候補を選ぶ。
        var indices = new int[_buttons.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(i, indices.Length);
            (indices[i], indices[randomIndex]) = (indices[randomIndex], indices[i]);
            var button = _buttons[indices[i]];
            button.gameObject.SetActive(true);
            button.transform.SetAsFirstSibling();
        }
        return true;
    }
    public void TryReroll()
    {
        if (RerollToken >= 3 && Reroll())
        {
            RerollToken -= 3;
        }
    }
    private void OnDisable()
    {
        if (_expBar != null) _expBar.DOKill();
    }
}
