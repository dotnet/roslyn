// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly string[] EditAndContinueCustomTags = new string[] { WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable };
        private static readonly string[] UnnecessaryCustomTags = new string[] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry };

        public static string[] Microsoft
        {
            get
            {
                Assert(MicrosoftCustomTags, WellKnownDiagnosticTags.Telemetry);
                return MicrosoftCustomTags;
            }
        }

        public static string[] EditAndContinue
        {
            get
            {
                Assert(EditAndContinueCustomTags, WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable);
                return EditAndContinueCustomTags;
            }
        }

        public static string[] Unnecessary
        {
            get
            {
                Assert(UnnecessaryCustomTags, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry);
                return UnnecessaryCustomTags;
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
