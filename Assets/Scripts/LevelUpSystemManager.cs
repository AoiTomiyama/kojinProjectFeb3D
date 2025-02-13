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

        _buttons = _buttonLayoutGroup.GetComponentsInChildren<Button>();
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
    private void Reroll()
    {
        foreach (var button in _buttons)
        {
            button.gameObject.SetActive(false);
        }
        const int MAX_LOOPCOUNT = 100;
        int loopCount = 0;
        var list = new HashSet<int>();
        while (list.Count < 4)
        {
            int index = Random.Range(0, _buttons.Length);
            list.Add(index);
            loopCount++;
            if (loopCount > MAX_LOOPCOUNT)
            {
                Debug.LogError("登録済みのボタンが4つ以下によりリロールが完了しませんでした。");
                return;
            }
        }
        Debug.Log(string.Join(", ", list));
        foreach (var i in list)
        {
            _buttons[i].gameObject.SetActive(true);
            _buttons[i].transform.SetAsFirstSibling();
        }
    }
    public void TryReroll()
    {
        if (RerollToken >= 3)
        {
            RerollToken -= 3;
            Reroll();
        }
    }
    private void OnDisable()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
