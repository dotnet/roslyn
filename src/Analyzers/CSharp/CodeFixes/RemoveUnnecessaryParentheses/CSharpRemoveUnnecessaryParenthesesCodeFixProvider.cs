// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryParentheses), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpRemoveUnnecessaryParenthesesCodeFixProvider()
    : AbstractRemoveUnnecessaryParenthesesCodeFixProvider<SyntaxNode>
{
    protected override bool CanRemoveParentheses(SyntaxNode current, SemanticModel semanticModel, CancellationToken cancellationToken)
        => current switch
        {
            ParenthesizedExpressionSyntax p => CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(p, semanticModel, cancellationToken, out _, out _),
            ParenthesizedPatternSyntax p => CSharpRemoveUnnecessaryPatternParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(p, out _, out _),
            _ => false,
        };
}
