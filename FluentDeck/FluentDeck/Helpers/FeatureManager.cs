namespace FluentDeck.Helpers;

public static class FeatureManager
{
    public static bool IsDeveloperMode =>
#if DEV_BUILD
        true;
#else
        false;
#endif
}
