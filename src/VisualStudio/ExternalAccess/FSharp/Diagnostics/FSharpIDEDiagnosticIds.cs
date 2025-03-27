// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    internal static class FSharpIDEDiagnosticIds
    {
        public static string SimplifyNamesDiagnosticId => IDEDiagnosticIds.SimplifyNamesDiagnosticId;
        public static string RemoveUnnecessaryImportsDiagnosticId => IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId;
        public static string RemoveUnnecessaryParenthesesDiagnosticId => IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId;
    }
}
