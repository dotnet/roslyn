using System.Diagnostics;
using Roslyn.Utilities;

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
                Assert(MicrosoftCustomTags, WellKnownDiagnosticTags.Telemetry);
                return MicrosoftCustomTags;
            }
        }

        [Conditional("DEBUG")]
        private static void Assert(string[] customTags, params string[] tags)
        {
            Contract.Requires(customTags.Length == tags.Length);

            for (int i = 0; i < tags.Length; i++)
            {
                Contract.Requires(customTags[i] == tags[i]);
            }
        }
    }
}
