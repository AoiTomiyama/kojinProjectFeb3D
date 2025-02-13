using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.Experimental.GraphView;
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

    [SerializeField, Header("次レベルに必要な経験値")]
    private List<int> _requireExpList = new List<int>();

    CancellationTokenSource _cts;
    private void Start()
    {
        _cts = new CancellationTokenSource();
    }
    public void GainExperience(int amount)
    {
        GainExpAsync(amount, _cts.Token);
    }

    private async void GainExpAsync(int amount, CancellationToken token)
    {
        _killCount++;
        _rerollToken++;

        _killCountText.text = _killCount.ToString();
        _tokenCountText.text = _rerollToken.ToString();

        _currentExp += amount;

        while (_currentExp >= _requireExpList[_currentLevel])
        {
            _expBar.DOFillAmount(1, 0.3f).OnComplete(() =>
            {
                _expBar.fillAmount = 0;
                _currentExp -= _requireExpList[_currentLevel];
                _currentLevel++;
                _pickCount++;
                
                _pickUpgradeCountText.text = _pickCount.ToString();
                _levelText.text = _currentLevel.ToString();
            });
            await UniTask.Delay(300, cancellationToken: token);
        }

        _expBar.DOFillAmount(1f * _currentExp / _requireExpList[_currentLevel], 0.3f);
    }

    private void OnDisable()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
