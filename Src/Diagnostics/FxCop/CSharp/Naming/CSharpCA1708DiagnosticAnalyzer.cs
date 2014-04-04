// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Naming;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Naming
{
    /// <summary>
    /// CA1708: Identifier names should differ more than case
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(RuleId, LanguageNames.CSharp)]
    public class CSharpCA1708DiagnosticAnalyzer : CA1708DiagnosticAnalyzer
    {
    }
}
