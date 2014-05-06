namespace Microsoft.CodeAnalysis
{
    public static class WellKnownDiagnosticTags
    {
        /// <summary>
        /// Indicates that the diagnostic cannot be suppressed and its default severity cannot be escalated.
        /// </summary>
        /// <remarks>
        /// Visual Studio Ruleset Editor doesn't display diagnostics with this tag as such diagnostics cannot be configured in the Ruleset Editor.
        /// </remarks>
        public const string CannotBeSuppressedOrEscalated = "CannotBeSuppressedOrEscalated";

        /// <summary>
        /// Indicates that the diagnostic is related to some unnecessary source code.
        /// </summary>
        public const string Unnecessary = "Unnecessary";

        /// <summary>
        /// Indicates that the diagnostic is related to edit and continue.
        /// </summary>
        public const string EditAndContinue = "EditAndContinue";
    }
}
