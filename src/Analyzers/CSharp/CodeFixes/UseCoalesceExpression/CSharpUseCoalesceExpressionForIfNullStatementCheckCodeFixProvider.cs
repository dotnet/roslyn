// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCoalesceExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;

[ExtensionOrder(Before = PredefinedCodeFixProviderNames.AddBraces)]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCoalesceExpressionForIfNullStatementCheck), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider()
    : AbstractUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider
{
    protected override bool ShouldAddExplicitCast(
        ISyntaxFactsService syntaxFacts, SemanticModel semanticModel,
        SyntaxNode expressionToCoalesce, SyntaxNode whenTrueStatement,
        [NotNullWhen(true)] out ITypeSymbol? castTo,
        CancellationToken cancellationToken)
    {
        castTo = null;

        if (!syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
            return false;

        syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out var left, out var right);

        var leftPartTypeSymbol = semanticModel.GetTypeInfo(expressionToCoalesce, cancellationToken).Type;
        var rightPartTypeSymbol = semanticModel.GetTypeInfo(right, cancellationToken).Type;
        var finalDestinationTypeSymbol = semanticModel.GetTypeInfo(left, cancellationToken).Type;

        if (leftPartTypeSymbol == null || rightPartTypeSymbol == null || finalDestinationTypeSymbol == null)
            return false;

        if (leftPartTypeSymbol.Equals(rightPartTypeSymbol))
            return false;

        if (semanticModel.Compilation.HasImplicitConversion(leftPartTypeSymbol, rightPartTypeSymbol) ||
            semanticModel.Compilation.HasImplicitConversion(rightPartTypeSymbol, leftPartTypeSymbol))
        {
            return false;
        }

        castTo = finalDestinationTypeSymbol;
        return true;
    }
}
