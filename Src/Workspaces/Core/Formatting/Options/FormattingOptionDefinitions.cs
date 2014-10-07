namespace Roslyn.Services.Formatting.Options
{
    internal static class FormattingOptionDefinitions
    {
        public const string FeatureName = "Formatting";

        public static OptionKey<bool> UseDebugMode =
            new OptionKey<bool>(FeatureName, "Debug Mode - No Parallel");
    }
}