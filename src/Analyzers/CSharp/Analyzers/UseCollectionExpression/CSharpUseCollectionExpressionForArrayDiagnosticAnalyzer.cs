﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    public CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForArray)
    {
    }

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeArrayInitializerExpression, SyntaxKind.ArrayInitializerExpression);
        context.RegisterSyntaxNodeAction(AnalyzeArrayCreationExpression, SyntaxKind.ArrayCreationExpression);
    }

    private void AnalyzeArrayCreationExpression(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var arrayCreationExpression = (ArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // Don't analyze arrays with initializers here, they're handled in AnalyzeArrayInitializerExpression instead.
        if (arrayCreationExpression.Initializer != null)
            return;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        // Analyze the statements that follow to see if they can initialize this array.
        var matches = TryGetMatches(semanticModel, arrayCreationExpression, cancellationToken);
        if (matches.IsDefault)
            return;

        ReportArrayCreationDiagnostics(context, syntaxTree, option, arrayCreationExpression);
    }

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches(
        SemanticModel semanticModel,
        ArrayCreationExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        // we have `new T[...] ...;` defer to analyzer to find the items that follow that may need to
        // be added to the collection expression.
        var matches = UseCollectionExpressionHelpers.TryGetMatches(
            semanticModel,
            expression,
            static e => e.Type,
            static e => e.Initializer,
            cancellationToken);
        if (matches.IsDefault)
            return default;

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, expression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return default;
        }

        return matches;
    }

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches(
        SemanticModel semanticModel,
        ImplicitArrayCreationExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        // if we have `new[] { ... }` we have no subsequent matches to add to the collection. All values come
        // from within the initializer.
        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, expression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return default;
        }

        return ImmutableArray<CollectionExpressionMatch<StatementSyntax>>.Empty;
    }

    private void AnalyzeArrayInitializerExpression(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var initializer = (InitializerExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        var isConcreteOrImplicitArrayCreation = initializer.Parent is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax;

        // a naked `{ ... }` can only be converted to a collection expression when in the exact form `x = { ... }`
        if (!isConcreteOrImplicitArrayCreation && initializer.Parent is not EqualsValueClauseSyntax)
            return;

        var arrayCreationExpression = isConcreteOrImplicitArrayCreation
            ? (ExpressionSyntax)initializer.GetRequiredParent()
            : initializer;

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, arrayCreationExpression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return;
        }

        if (isConcreteOrImplicitArrayCreation)
        {
            var matches = initializer.Parent switch
            {
                ArrayCreationExpressionSyntax arrayCreation => TryGetMatches(semanticModel, arrayCreation, cancellationToken),
                ImplicitArrayCreationExpressionSyntax arrayCreation => TryGetMatches(semanticModel, arrayCreation, cancellationToken),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            if (matches.IsDefault)
                return;

            ReportArrayCreationDiagnostics(context, syntaxTree, option, arrayCreationExpression);
        }
        else
        {
            Debug.Assert(initializer.Parent is EqualsValueClauseSyntax);
            // int[] = { 1, 2, 3 };
            //
            // In this case, we always have a target type, so it should always be valid to convert this to a collection expression.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                initializer.OpenBraceToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(initializer.GetLocation()),
                properties: null));
        }
    }

    private void ReportArrayCreationDiagnostics(SyntaxNodeAnalysisContext context, SyntaxTree syntaxTree, CodeStyleOption2<bool> option, ExpressionSyntax expression)
    {
        var locations = ImmutableArray.Create(expression.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties: null));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression is ArrayCreationExpressionSyntax arrayCreationExpression
                    ? arrayCreationExpression.Type.Span.End
                    : ((ImplicitArrayCreationExpressionSyntax)expression).CloseBracketToken.Span.End)));

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    }
}
