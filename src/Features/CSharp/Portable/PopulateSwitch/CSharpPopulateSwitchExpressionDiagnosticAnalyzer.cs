// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal sealed class CSharpPopulateSwitchExpressionDiagnosticAnalyzer :
        AbstractPopulateSwitchExpressionDiagnosticAnalyzer<SwitchExpressionSyntax>
    {
        protected override Location GetDiagnosticLocation(SwitchExpressionSyntax switchBlock)
            => switchBlock.SwitchKeyword.GetLocation();
    }
}
