﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseCoalesceExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;

[ExtensionOrder(Before = PredefinedCodeFixProviderNames.AddBraces)]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCoalesceExpressionForIfNullStatementCheck), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider()
    : AbstractUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider
{
    protected override ITypeSymbol? TryGetExplicitCast(
        SemanticModel semanticModel, SyntaxNode expressionToCoalesce,
        SyntaxNode leftAssignmentPart, SyntaxNode rightAssignmentPart,
        CancellationToken cancellationToken)
    {
        var leftPartTypeSymbol = semanticModel.GetTypeInfo(expressionToCoalesce, cancellationToken).Type;
        var rightPartTypeSymbol = semanticModel.GetTypeInfo(rightAssignmentPart, cancellationToken).Type;
        var finalDestinationTypeSymbol = semanticModel.GetTypeInfo(leftAssignmentPart, cancellationToken).Type;

        if (leftPartTypeSymbol == null || rightPartTypeSymbol == null || finalDestinationTypeSymbol == null)
            return null;

        if (leftPartTypeSymbol.Equals(rightPartTypeSymbol))
            return null;

        if (semanticModel.Compilation.HasImplicitConversion(leftPartTypeSymbol, rightPartTypeSymbol) ||
            semanticModel.Compilation.HasImplicitConversion(rightPartTypeSymbol, leftPartTypeSymbol))
        {
            return null;
        }

        return finalDestinationTypeSymbol;
    }
}
