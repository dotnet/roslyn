namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticCustomTags
    {
        /// <summary>
        /// it is string[] because DiagnosticDescriptor expects string[]. 
        /// </summary>
        private static readonly string[] MicrosoftCustomTags = new string[] { WellKnownDiagnosticTags.Telemetry };

        public static string[] Microsoft
        {
            get
            {
                return MicrosoftCustomTags;
            }
        }
    }
}
