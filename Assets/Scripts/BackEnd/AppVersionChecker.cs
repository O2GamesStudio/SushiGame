using UnityEngine;
using Firebase.RemoteConfig;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

public class AppVersionChecker : MonoBehaviour
{
    public static AppVersionChecker Instance { get; private set; }

    [SerializeField] private StoreUpdatePanel storeUpdatePanel;

    private void Awake() => Instance = this;

    public void CheckVersion(Action onPassed = null, Action onFailed = null)
    {
        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;

        var defaults = new Dictionary<string, object>
        {
            { "min_version", "1.0.0" },
            { "store_url", "" }
        };

        remoteConfig.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task =>
        {
            var settings = new ConfigSettings
            {
#if UNITY_EDITOR
                MinimumFetchIntervalInMilliseconds = 0
#else
                MinimumFetchIntervalInMilliseconds = 43200000
#endif
            };

            remoteConfig.SetConfigSettingsAsync(settings).ContinueWithOnMainThread(settingsTask =>
            {
                remoteConfig.FetchAndActivateAsync().ContinueWithOnMainThread(fetchTask =>
                {
                    if (fetchTask.IsFaulted || fetchTask.IsCanceled)
                    {
                        onPassed?.Invoke();
                        return;
                    }

                    string minVersion = remoteConfig.GetValue("min_version").StringValue;
                    string storeUrl = remoteConfig.GetValue("store_url").StringValue;
                    string currentVersion = Application.version;

                    if (IsUpdateRequired(currentVersion, minVersion))
                    {
                        storeUpdatePanel?.Show(storeUrl, $"최신 버전으로 업데이트가 필요합니다.\n현재: {currentVersion} / 최소: {minVersion}");
                        onFailed?.Invoke();
                    }
                    else
                    {
                        onPassed?.Invoke();
                    }
                });
            });
        });
    }

    private bool IsUpdateRequired(string current, string minimum)
    {
        var currentParts = current.Split('.');
        var minimumParts = minimum.Split('.');

        int maxLength = Mathf.Max(currentParts.Length, minimumParts.Length);
        for (int i = 0; i < maxLength; i++)
        {
            int cur = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;
            int min = i < minimumParts.Length ? int.Parse(minimumParts[i]) : 0;

            if (cur < min) return true;
            if (cur > min) return false;
        }
        return false;
    }
}