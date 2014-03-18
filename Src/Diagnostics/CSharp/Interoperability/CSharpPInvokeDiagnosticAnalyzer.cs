// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Interoperability;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Interoperability
{
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(PInvokeInteroperabilityRuleName, LanguageNames.CSharp)]
    public class CSharpPInvokeDiagnosticAnalyzer : PInvokeDiagnosticAnalyzer
    {
    }
}
