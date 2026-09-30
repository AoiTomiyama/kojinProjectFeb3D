using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
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

    CancellationTokenSource _cts;
    PlayerAttack _attack;

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
        _cts = new CancellationTokenSource();
        _attack = FindAnyObjectByType<PlayerAttack>();
        _upgradePanel.gameObject.SetActive(_isMenuActivated);
        _hasPickupNotice.gameObject.SetActive(_pickCount > 0);

        _buttons = _buttonLayoutGroup.GetComponentsInChildren<Button>(true);
        Reroll();
    }
    public void GainExperience(int amount)
    {
        GainExpAsync(amount, _cts.Token);
    }

    private async void GainExpAsync(int amount, CancellationToken token)
    {
        _killCount++;
        RerollToken++;

        _killCountText.text = _killCount.ToString();

        _currentExp += amount;

        while (_currentExp >= _requireExpList[_currentLevel])
        {
            _expBar.DOFillAmount(1, 0.3f).OnComplete(() =>
            {
                _expBar.fillAmount = 0;
                _currentExp -= _requireExpList[_currentLevel];
                _currentLevel++;
                PickCount++;
                _levelText.text = _currentLevel.ToString();
            });
            await UniTask.Delay(300, cancellationToken: token);
        }

        _expBar.DOFillAmount(1f * _currentExp / _requireExpList[_currentLevel], 0.3f);
    }
    public void OpenCloseUpgradeMenu()
    {
        _isMenuActivated = !_isMenuActivated;
        _upgradePanel.gameObject.SetActive(_isMenuActivated);
    }

    public void ApplyPowerUp(PowerUpParameter powerUp)
    {
        _attack.ApplyPowerUp(powerUp);
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
        _cts.Cancel();
        _cts.Dispose();
    }
}
