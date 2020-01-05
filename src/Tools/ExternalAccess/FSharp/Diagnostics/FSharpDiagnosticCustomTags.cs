// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    internal static class FSharpDiagnosticCustomTags
    {
#pragma warning disable CA1819 // Properties should not return arrays
        public static string[] Microsoft => CodeAnalysis.Diagnostics.DiagnosticCustomTags.Microsoft;

        public static string[] EditAndContinue => CodeAnalysis.Diagnostics.DiagnosticCustomTags.EditAndContinue;

        public static string[] Unnecessary => CodeAnalysis.Diagnostics.DiagnosticCustomTags.Unnecessary;
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
