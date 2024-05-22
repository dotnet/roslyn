// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.CodeStyle;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

/// <summary>
/// Analyzer/fixer that looks for code of the form <c>X.Empty&lt;T&gt;()</c> or <c>X&lt;T&gt;.Empty</c> and offers to
/// replace with <c>[]</c> if legal to do so.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer()
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(
        IDEDiagnosticIds.UseCollectionExpressionForEmptyDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForEmpty)
{
    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType)
        => context.RegisterSyntaxNodeAction(context => AnalyzeMemberAccess(context, expressionType), SyntaxKind.SimpleMemberAccessExpression);

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        var nodeToReplace =
            IsCollectionEmptyAccess(semanticModel, memberAccess, cancellationToken)
                ? memberAccess
                : memberAccess.Parent is InvocationExpressionSyntax invocation && IsCollectionEmptyAccess(semanticModel, invocation, cancellationToken)
                    ? (ExpressionSyntax)invocation
                    : null;
        if (nodeToReplace is null)
            return;

        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        if (!CanReplaceWithCollectionExpression(
                semanticModel, nodeToReplace, expressionType, isSingletonInstance: true, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out var changesSemantics))
        {
            return;
        }

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: ImmutableArray.Create(nodeToReplace.GetLocation()),
            properties: changesSemantics ? ChangesSemantics : null));
    }
}
