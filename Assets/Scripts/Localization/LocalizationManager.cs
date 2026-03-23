using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private bool forceEnglish = false;

    private const string CsvPath = "Localization/Localization";
    private const string FallbackLanguage = "en";

    private Dictionary<string, string> table = new Dictionary<string, string>();

    public string CurrentLanguageCode { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        CurrentLanguageCode = forceEnglish ? "en" : (Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en");
#else
    CurrentLanguageCode = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en";
#endif

        LoadCSV();
    }

    private void LoadCSV()
    {
        table.Clear();
        var csvFile = Resources.Load<TextAsset>(CsvPath);
        if (csvFile == null)
        {
            Debug.LogError("[LocalizationManager] CSV 파일 없음: " + CsvPath);
            return;
        }

        var lines = csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var headers = ParseLine(lines[0]);
        int langIndex = -1;
        int fallbackIndex = -1;

        for (int i = 1; i < headers.Count; i++)
        {
            string h = headers[i].Trim();
            if (h == CurrentLanguageCode) langIndex = i;
            if (h == FallbackLanguage) fallbackIndex = i;
        }

        if (langIndex < 0) langIndex = fallbackIndex;
        if (langIndex < 0)
        {
            Debug.LogError("[LocalizationManager] 언어 컬럼 없음");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var cols = ParseLine(line);
            if (cols.Count <= langIndex) continue;

            string key = cols[0].Trim();
            string value = cols[langIndex].Trim().Replace("\\n", "\n");

            if (!string.IsNullOrEmpty(key))
                table[key] = value;
        }

        Debug.Log($"[LocalizationManager] 로드 완료 - 언어: {CurrentLanguageCode}, 키 수: {table.Count}");
    }

    private List<string> ParseLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    public string Get(string key)
    {
        if (table.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[LocalizationManager] 키 없음: {key}");
        return key;
    }

    public string Get(string key, params object[] args)
    {
        string format = Get(key);
        try { return string.Format(format, args); }
        catch { return format; }
    }
}