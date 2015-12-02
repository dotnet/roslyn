// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{                          
    /// <summary>
    /// RS0004: Invoke the correct property to ensure correct use site diagnostics.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpInvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsAnalyzer : InvokeTheCorrectPropertyToEnsureCorrectUseSiteDiagnosticsAnalyzer
    {

    }
}