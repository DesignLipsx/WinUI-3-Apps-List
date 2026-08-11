namespace FluentDeck.Helpers;

public static class FeatureManager
{
    public static bool IsDeveloperMode =>
#if DEV_BUILD
        true;
#else
        false;
#endif

    public static bool IsStoreBuild =>
#if STORE_BUILD
        true;
#else
        !IsDeveloperMode;
#endif
}
