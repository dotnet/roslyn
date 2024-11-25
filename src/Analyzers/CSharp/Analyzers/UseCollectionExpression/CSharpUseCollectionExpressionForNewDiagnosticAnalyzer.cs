// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForNewDiagnosticAnalyzer()
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(
        IDEDiagnosticIds.UseCollectionExpressionForNewDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForNew)
{
    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType)
    {
        context.RegisterSyntaxNodeAction(context => AnalyzeObjectCreationExpression(context, expressionType), SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(context => AnalyzeImplicitObjectCreationExpression(context, expressionType), SyntaxKind.ImplicitObjectCreationExpression);
    }

    private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
        => AnalyzeBaseObjectCreationExpression(context, (BaseObjectCreationExpressionSyntax)context.Node, expressionType);

    private void AnalyzeImplicitObjectCreationExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
        => AnalyzeBaseObjectCreationExpression(context, (BaseObjectCreationExpressionSyntax)context.Node, expressionType);

    private void AnalyzeBaseObjectCreationExpression(
        SyntaxNodeAnalysisContext context, BaseObjectCreationExpressionSyntax objectCreationExpression, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        if (objectCreationExpression is not { ArgumentList.Arguments: [var argument], Initializer: null })
            return;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        var symbol = semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken).Symbol;
        if (symbol is not IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters: [var parameter] } ||
            parameter.Type.Name != nameof(IEnumerable<int>))
        {
            return;
        }

        if (!Equals(compilation.IEnumerableOfTType(), parameter.Type.OriginalDefinition))
            return;

        if (!IsArgumentCompatibleWithIEnumerableOfT(semanticModel, argument, out var unwrapArgument, out var useSpread, cancellationToken))
            return;

        // Make sure we can actually use a collection expression in place of the full invocation.
        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        if (!CanReplaceWithCollectionExpression(
                semanticModel, objectCreationExpression, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out var changesSemantics))
        {
            return;
        }

        var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());
        var properties = GetDiagnosticProperties(unwrapArgument, useSpread, changesSemantics);

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            objectCreationExpression.NewKeyword.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: locations,
            properties));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                objectCreationExpression.SpanStart,
                objectCreationExpression.ArgumentList.OpenParenToken.Span.End)),
            objectCreationExpression.ArgumentList.CloseParenToken.GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            NotificationOption2.ForSeverity(UnnecessaryCodeDescriptor.DefaultSeverity),
            context.Options,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties));
    }
}
