public static class AdConfig
{
    public static string AppKey => GetAppKey();
    public static string BannerAdUnitId => GetBannerAdUnitId();
    public static string InterstitalAdUnitId => GetInterstitialAdUnitId();
    public static string RewardedVideoAdUnitId => GetRewardedVideoAdUnitId();

    public static string AdMobBannerUnitId => GetAdMobBannerUnitId();
    public static string AdMobRewardedUnitId => GetAdMobRewardedUnitId();

    public static bool UseAdMob => true;

    static string GetAppKey()
    {
#if UNITY_ANDROID
        return "YOUR_ANDROID_APP_KEY";
#elif UNITY_IPHONE
        return "YOUR_IOS_APP_KEY";
#else
        return "unexpected_platform";
#endif
    }

    static string GetBannerAdUnitId()
    {
#if UNITY_ANDROID
        return "YOUR_ANDROID_BANNER_ID";
#elif UNITY_IPHONE
        return "YOUR_IOS_BANNER_ID";
#else
        return "unexpected_platform";
#endif
    }

    static string GetInterstitialAdUnitId()
    {
#if UNITY_ANDROID
        return "YOUR_ANDROID_INTERSTITIAL_ID";
#elif UNITY_IPHONE
        return "YOUR_IOS_INTERSTITIAL_ID";
#else
        return "unexpected_platform";
#endif
    }

    static string GetRewardedVideoAdUnitId()
    {
#if UNITY_ANDROID
        return "YOUR_ANDROID_REWARDED_ID";
#elif UNITY_IPHONE
        return "YOUR_IOS_REWARDED_ID";
#else
        return "unexpected_platform";
#endif
    }

    static string GetAdMobBannerUnitId()
    {
#if UNITY_EDITOR
        return "YOUR_ADMOB_TEST_BANNER_ID";
#elif UNITY_IPHONE
        return "YOUR_IOS_ADMOB_BANNER_ID";
#else
        return "YOUR_ANDROID_ADMOB_BANNER_ID";
#endif
    }

    static string GetAdMobRewardedUnitId()
    {
#if UNITY_EDITOR
        return "YOUR_ADMOB_TEST_REWARDED_ID";
#elif UNITY_IPHONE
        return "YOUR_IOS_ADMOB_REWARDED_ID";
#else
        return "YOUR_ANDROID_ADMOB_REWARDED_ID";
#endif
    }
}