using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalizationService
{
    [Serializable] private class Entry
    {
        public string key;
        public string text;
        public string source;
        public string sourcePrefix;
    }

    [Serializable] private class Table { public Entry[] entries; }

    private const string PreferenceKey = "afterlight.language";
    private static readonly Dictionary<string, string> English = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> Portuguese = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> SourceKeys = new Dictionary<string, string>();
    private static readonly List<KeyValuePair<string, string>> SourcePrefixes = new List<KeyValuePair<string, string>>();
    private static bool _loaded;
    private static string _language;

    public static event Action LanguageChanged;

    public static string Language
    {
        get
        {
            EnsureLoaded();
            return _language;
        }
    }

    public static void SetLanguage(string language)
    {
        EnsureLoaded();
        language = language == "pt-BR" ? "pt-BR" : "en";
        if (_language == language) return;
        _language = language;
        PlayerPrefs.SetString(PreferenceKey, language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static string Get(string key, string fallback = null)
    {
        EnsureLoaded();
        if (_language == "pt-BR" && Portuguese.TryGetValue(key, out var translated))
            return translated;
        if (English.TryGetValue(key, out var english))
            return english;
        return fallback ?? key;
    }

    public static string QuestTitle(QuestData quest) => quest == null ? string.Empty : Get("quest." + quest.id + ".title", quest.title);
    public static string QuestDescription(QuestData quest) => quest == null ? string.Empty : Get("quest." + quest.id + ".description", quest.description);
    public static string ClueTitle(ClueData clue) => clue == null ? string.Empty : Get("clue." + clue.id + ".title", clue.title);
    public static string ClueDescription(ClueData clue) => clue == null ? string.Empty : Get("clue." + clue.id + ".description", clue.description);
    public static string QuestStateText(QuestState state) => Get("state." + state, state.ToString());

    public static bool TryGetSourceKey(string source, out string key)
    {
        EnsureLoaded();
        string normalized = Normalize(source);
        if (SourceKeys.TryGetValue(normalized, out key)) return true;
        foreach (var prefix in SourcePrefixes)
            if (normalized.StartsWith(prefix.Key, StringComparison.Ordinal))
            {
                key = prefix.Value;
                return true;
            }
        key = null;
        return false;
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _language = PlayerPrefs.GetString(PreferenceKey, "en") == "pt-BR" ? "pt-BR" : "en";
        ReadTable("Localization/en", English, true);
        ReadTable("Localization/pt-BR", Portuguese, false);
    }

    private static void ReadTable(string resourcePath, Dictionary<string, string> destination, bool isEnglish)
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError("Missing localization table: " + resourcePath);
            return;
        }
        var table = JsonUtility.FromJson<Table>(asset.text);
        if (table?.entries == null) return;
        foreach (var entry in table.entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
            destination[entry.key] = entry.text ?? string.Empty;
            if (!isEnglish) continue;
            if (!string.IsNullOrWhiteSpace(entry.source))
                SourceKeys[Normalize(entry.source)] = entry.key;
            if (!string.IsNullOrWhiteSpace(entry.sourcePrefix))
                SourcePrefixes.Add(new KeyValuePair<string, string>(Normalize(entry.sourcePrefix), entry.key));
        }
    }

    private static string Normalize(string value) => Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
}

public static class LocalizationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _ = LocalizationService.Language;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<LocalizedText>() != null) continue;
                if (LocalizationService.TryGetSourceKey(text.text, out var key))
                    text.gameObject.AddComponent<LocalizedText>().Initialize(text, key);
            }
        }
        LanguageSelector.CreateForScene(scene);
    }
}
