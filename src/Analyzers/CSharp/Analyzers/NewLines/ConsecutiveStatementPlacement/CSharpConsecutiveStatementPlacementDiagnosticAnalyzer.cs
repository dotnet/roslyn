// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConsecutiveStatementPlacement
{
    /// <summary>
    /// Analyzer that finds code of the form:
    /// <code>
    /// if (cond)
    /// {
    /// }
    /// NextStatement();
    /// </code>
    /// 
    /// And requires it to be of the form:
    /// <code>
    /// if (cond)
    /// {
    /// }
    /// 
    /// NextStatement();
    /// </code>
    /// 
    /// Specifically, all blocks followed by another statement must have a blank line between them.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpConsecutiveStatementPlacementDiagnosticAnalyzer : AbstractConsecutiveStatementPlacementDiagnosticAnalyzer<StatementSyntax>
    {
        public CSharpConsecutiveStatementPlacementDiagnosticAnalyzer()
            : base(CSharpSyntaxFacts.Instance)
        {
        }

        protected override bool IsBlockLikeStatement(SyntaxNode node)
            => node is BlockSyntax or SwitchStatementSyntax;

        protected override Location GetDiagnosticLocation(SyntaxNode block)
            => block.GetLastToken().GetLocation();
    }
}
