namespace Roslyn.Diagnostics.Analyzers
{
    internal static class RoslynDiagnosticIds
    {
        public const string UseEmptyEnumerableRuleId = "RS0001";
        public const string UseSingletonEnumerableRuleId = "RS0002";
        public const string DirectlyAwaitingTaskAnalyzerRuleId = "RS0003";
        public const string UseSiteDiagnosticsCheckerRuleId = "RS0004";
        public const string DontUseCodeActionCreateRuleId = "RS0005";
        public const string MixedVersionsOfMefAttributesRuleId = "RS0006";
        public const string UseArrayEmptyRuleId = "RS0007";
        public const string ImplementIEquatable = "RS0008";
        public const string OverrideObjectEquals = "RS0009";
        public const string MissingSharedAttributeRuleId = "RS0010";
    }
}