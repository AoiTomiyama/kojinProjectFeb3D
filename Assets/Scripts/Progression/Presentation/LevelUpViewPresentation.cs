using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>確定済みの経験値・強化状態をシーンのUIへ描画する。</summary>
public class LevelUpViewPresentation : MonoBehaviour
{
    [SerializeField] private Image _expBar;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _killCountText;
    [SerializeField] private TextMeshProUGUI _tokenCountText;
    [SerializeField] private TextMeshProUGUI _pickUpgradeCountText;
    [SerializeField] private Image _upgradePanel;
    [SerializeField] private Image _hasPickupNotice;
    [SerializeField] private VerticalLayoutGroup _buttonLayoutGroup;
    [SerializeField] private UpgradeButtonPresentation _buttonPrefab;
    [SerializeField] private AudioSource _buttonClickSource;
    [SerializeField] private AudioClip _buttonClickClip;
    private readonly Dictionary<int, Button> _buttonsById = new Dictionary<int, Button>();

    private Button[] _buttons;
    private float _targetProgress;
    private bool _hasProgress;

    public int CandidateCount => _buttons.Length;

    public void Initialize(bool menuVisible, int level, float progress, int kills, int tokens, int availableChoices)
    {
        if (_expBar == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._expBar が設定されていません。");
        if (_levelText == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._levelText が設定されていません。");
        if (_killCountText == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._killCountText が設定されていません。");
        if (_tokenCountText == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._tokenCountText が設定されていません。");
        if (_pickUpgradeCountText == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._pickUpgradeCountText が設定されていません。");
        if (_upgradePanel == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._upgradePanel が設定されていません。");
        if (_hasPickupNotice == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._hasPickupNotice が設定されていません。");
        if (_buttonLayoutGroup == null)
            throw new System.InvalidOperationException($"{name}: LevelUpViewPresentation._buttonLayoutGroup が設定されていません。");
        SetMenuVisible(menuVisible);
        _targetProgress = progress;
        _hasProgress = true;
        _levelText.text = level.ToString();
        _expBar.fillAmount = progress;
        ShowKillCount(kills);
        ShowRerollTokens(tokens);
        ShowUpgradeChoices(availableChoices);
    }

    public void SetMenuVisible(bool visible) => _upgradePanel.gameObject.SetActive(visible);

    public void CreateButtons(UpgradeDefinitionConfiguration[] definitions, Action<int> onSelected)
    {
        if (_buttonPrefab == null) throw new InvalidOperationException($"{name}: 強化ボタンPrefabが設定されていません。");
        // Destroyはフレーム末尾まで遅れるため、先に無効化・接続解除・親から分離する。
        foreach (var button in _buttonsById.Values)
        {
            button.GetComponent<UpgradeButtonPresentation>().Unbind();
            button.gameObject.SetActive(false);
            button.transform.SetParent(null);
            Destroy(button.gameObject);
        }
        _buttonsById.Clear();
        _buttons = new Button[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            var item = Instantiate(_buttonPrefab, _buttonLayoutGroup.transform, false);
            item.gameObject.SetActive(false);
            item.Bind(definitions[i], id =>
            {
                onSelected(id);
                // 元のシーンのButton.onClickにあった効果音接続を動的ボタンでも保つ。
                if (_buttonClickSource != null && _buttonClickClip != null) _buttonClickSource.PlayOneShot(_buttonClickClip);
            });
            var button = item.GetComponent<Button>();
            if (button == null) throw new InvalidOperationException($"{item.name}: Buttonが必要です。");
            _buttons[i] = button;
            _buttonsById.Add(definitions[i].Id, button);
        }
    }
    public void ShowKillCount(int count) => _killCountText.text = count.ToString();
    public void ShowRerollTokens(int count) => _tokenCountText.text = count.ToString();

    public void ShowUpgradeChoices(int count)
    {
        _hasPickupNotice.gameObject.SetActive(count > 0);
        _pickUpgradeCountText.text = count.ToString();
    }

    public void ShowProgress(int level, float progress, bool levelChanged)
    {
        _targetProgress = progress;
        _hasProgress = true;
        _levelText.text = level.ToString();
        _expBar.DOKill();
        if (progress >= 1f)
        {
            _expBar.fillAmount = 1f;
            return;
        }

        if (levelChanged) _expBar.fillAmount = 0f;
        _expBar.DOFillAmount(progress, 0.3f);
    }

    public void ShowCandidates(int[] selectedIds)
    {
        foreach (var button in _buttons) button.gameObject.SetActive(false);
        foreach (int id in selectedIds)
        {
            var button = _buttonsById[id];
            button.gameObject.SetActive(true);
            button.transform.SetAsFirstSibling();
        }
    }

    public void HideCandidate(int id) => _buttonsById[id].gameObject.SetActive(false);

    private void OnDisable()
    {
        if (_expBar == null) return;
        _expBar.DOKill();
        // 演出が途切れても表示は最後に確定した進捗へ合わせる。
        if (_hasProgress) _expBar.fillAmount = _targetProgress;
    }
}
