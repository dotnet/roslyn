// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpConsoleSnippetProvider() : AbstractConsoleSnippetProvider<
    ExpressionStatementSyntax,
    ExpressionSyntax,
    ArgumentListSyntax,
    LambdaExpressionSyntax>
{
    protected override bool IsValidSnippetLocationCore(SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxContext = context.SyntaxContext;
        var semanticModel = context.SemanticModel;

        var consoleSymbol = GetConsoleSymbolFromMetaDataName(semanticModel.Compilation);
        if (consoleSymbol is null)
            return false;

        // Console.WriteLine snippet is legal after an arrow token of a void-returning lambda, e.g.
        // Action a = () => Console.WriteLine("Action called");
        if (syntaxContext.TargetToken is { RawKind: (int)SyntaxKind.EqualsGreaterThanToken, Parent: LambdaExpressionSyntax lambda })
        {
            var lambdaSymbol = semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol;

            // Given that we are in a partially written lambda state compiler might not always infer return type correctly.
            // In such cases an error type is returned. We allow them to provide snippet in locations
            // where lambda return type isn't yet known, but it might be a void type after fully completing the lambda
            if (lambdaSymbol is IMethodSymbol { ReturnType: { SpecialType: SpecialType.System_Void } or { TypeKind: TypeKind.Error } })
                return true;
        }

        return syntaxContext.IsStatementContext || syntaxContext.IsGlobalStatementContext;
    }

    protected override ArgumentListSyntax GetArgumentList(ExpressionSyntax expression)
        => ((InvocationExpressionSyntax)expression).ArgumentList;

    protected override SyntaxToken GetOpenParenToken(ArgumentListSyntax argumentList)
        => argumentList.OpenParenToken;
}
