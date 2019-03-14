using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    public static class DiagnosticCustomTags
    {
        /// <summary>
        /// it is string[] because DiagnosticDescriptor expects string[]. 
        /// </summary>
        private static readonly string[] s_microsoftCustomTags = new string[] { WellKnownDiagnosticTags.Telemetry };
        private static readonly string[] s_editAndContinueCustomTags = new string[] { WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable };
        private static readonly string[] s_unnecessaryCustomTags = new string[] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry };

#pragma warning disable CA1819 // Properties should not return arrays
        public static string[] Microsoft
        {
            get
            {
                Assert(s_microsoftCustomTags, WellKnownDiagnosticTags.Telemetry);
                return s_microsoftCustomTags;
            }
        }

        public static string[] EditAndContinue
        {
            get
            {
                Assert(s_editAndContinueCustomTags, WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable);
                return s_editAndContinueCustomTags;
            }
        }

        public static string[] Unnecessary
        {
            get
            {
                Assert(s_unnecessaryCustomTags, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry);
                return s_unnecessaryCustomTags;
            }
        }
#pragma warning restore CA1819 // Properties should not return arrays

        [Conditional("DEBUG")]
        private static void Assert(string[] customTags, params string[] tags)
        {
            Debug.Assert(customTags.Length == tags.Length);

            for (int i = 0; i < tags.Length; i++)
            {
                Debug.Assert(customTags[i] == tags[i]);
            }
        }

        internal static string[] Create(bool isUnneccessary, bool isConfigurable, params string[] customTags)
        {
            if (customTags.Length == 0 && isConfigurable)
            {
                return isUnneccessary ? Unnecessary : Microsoft;
            }

            var customTagsBuilder = ImmutableArray.CreateBuilder<string>();
            customTagsBuilder.AddRange(customTags.Concat(Microsoft));

            if (!isConfigurable)
            {
                customTagsBuilder.Add(WellKnownDiagnosticTags.NotConfigurable);
            }

            if (isUnneccessary)
            {
                customTagsBuilder.Add(WellKnownDiagnosticTags.Unnecessary);
            }

            return customTagsBuilder.ToArray();
        }
    }
}
