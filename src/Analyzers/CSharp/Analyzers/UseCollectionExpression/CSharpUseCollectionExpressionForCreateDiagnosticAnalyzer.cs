// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer()
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(
        IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForCreate)
{
    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType)
        => context.RegisterSyntaxNodeAction(context => AnalyzeInvocationExpression(context, expressionType), SyntaxKind.InvocationExpression);

    private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        if (!IsCollectionFactoryCreate(semanticModel, invocationExpression, out var memberAccess, out var unwrapArgument, out var useSpread, cancellationToken))
            return;

        // Make sure we can actually use a collection expression in place of the full invocation.
        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        if (!CanReplaceWithCollectionExpression(
                semanticModel, invocationExpression, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out var changesSemantics))
        {
            return;
        }

        var locations = ImmutableArray.Create(invocationExpression.GetLocation());
        var properties = GetDiagnosticProperties(unwrapArgument, useSpread, changesSemantics);

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: locations,
            properties));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                invocationExpression.SpanStart,
                invocationExpression.ArgumentList.OpenParenToken.Span.End)),
            invocationExpression.ArgumentList.CloseParenToken.GetLocation());

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
