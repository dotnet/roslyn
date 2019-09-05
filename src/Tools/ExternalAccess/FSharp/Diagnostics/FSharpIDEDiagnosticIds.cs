// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    internal static class FSharpIDEDiagnosticIds
    {
        public static string SimplifyNamesDiagnosticId => IDEDiagnosticIds.SimplifyNamesDiagnosticId;
        public static string RemoveUnnecessaryImportsDiagnosticId => IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId;
    }
}
