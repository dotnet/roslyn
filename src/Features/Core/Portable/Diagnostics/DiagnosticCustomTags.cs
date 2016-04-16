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
        private static readonly string[] s_microsoftCustomTags = new string[] { WellKnownDiagnosticTags.Telemetry };
        private static readonly string[] s_editAndContinueCustomTags = new string[] { WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable };
        private static readonly string[] s_unnecessaryCustomTags = new string[] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry };

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
