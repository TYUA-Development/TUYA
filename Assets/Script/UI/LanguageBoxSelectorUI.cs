using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageBoxSelectorUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI valueText;
    public Button boxButton;

    [Header("Fonts By Language")]
    [Tooltip("언어별 폰트. Language enum 값을 인덱스로 사용 - 0=Korean, 1=English, 2=Japanese, 3=ChineseSimplified, 4=ChineseTraditional. 각 언어 이름 자체가 해당 언어 문자로 표시되므로(예: 日本語, 简体中文, 繁體中文) 지금 보여주는 언어의 폰트를 그때그때 적용한다. 특정 언어 칸이 비어있으면 0번(Korean) 폰트로 대체되고, 리스트 전체가 비어있으면 valueText에 원래 지정된 폰트를 그대로 쓴다.")]
    public List<TMP_FontAsset> fontsByLanguage = new List<TMP_FontAsset>();

    [Header("Start")]
    public int startIndex = 0;

    [Header("Apply")]
    public bool applyImmediately = true;

    private int currentIndex;
    private bool listenersReady;

    private void Awake()
    {
        SetupListeners();
    }

    private void OnEnable()
    {
        SyncFromSettings();
        UpdateUI();
    }

    private void SetupListeners()
    {
        if (listenersReady)
            return;

        listenersReady = true;

        if (boxButton != null)
            boxButton.onClick.AddListener(SelectNext);
    }

    private void SyncFromSettings()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.Settings != null)
            currentIndex = SettingsManager.Instance.Settings.languageIndex;
        else
            currentIndex = startIndex;

        currentIndex = Mathf.Clamp(currentIndex, 0, SettingsManager.LanguageCount - 1);
    }

    private void SelectNext()
    {
        currentIndex++;

        if (currentIndex >= SettingsManager.LanguageCount)
            currentIndex = 0;

        if (applyImmediately && SettingsManager.Instance != null)
            SettingsManager.Instance.SetLanguageIndex(currentIndex);

        UpdateUI();
    }

    private void UpdateUI()
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, SettingsManager.LanguageCount - 1);

        if (valueText != null)
        {
            valueText.text = GetCurrentLabel();

            TMP_FontAsset localizedFont = GetLocalizedFont(currentIndex);
            if (localizedFont != null)
                valueText.font = localizedFont;
        }
    }

    public string GetCurrentLabel()
    {
        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.GetLanguageString(currentIndex);

        return "한국어";
    }

    private TMP_FontAsset GetLocalizedFont(int index)
    {
        if (fontsByLanguage == null || fontsByLanguage.Count == 0)
            return null;

        if (index < 0 || index >= fontsByLanguage.Count || fontsByLanguage[index] == null)
            index = 0; // 해당 언어 폰트가 비어있으면 0번(Korean) 폰트로 대체

        if (index < 0 || index >= fontsByLanguage.Count)
            return null;

        return fontsByLanguage[index];
    }
}
