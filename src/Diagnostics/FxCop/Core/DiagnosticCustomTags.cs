// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticCustomTags
    {
        /// <summary>
        /// it is string[] because DiagnosticDescriptor expects string[]. 
        /// </summary>
        private static readonly string[] s_microsoftCustomTags = new string[] { WellKnownDiagnosticTags.Telemetry };

        public static string[] Microsoft
        {
            get
            {
                return s_microsoftCustomTags;
            }
        }
    }
}
