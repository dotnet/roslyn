// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
