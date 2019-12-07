// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpPopulateSwitchStatementDiagnosticAnalyzer :
        AbstractPopulateSwitchStatementDiagnosticAnalyzer<SwitchStatementSyntax>
    {
    }
}
