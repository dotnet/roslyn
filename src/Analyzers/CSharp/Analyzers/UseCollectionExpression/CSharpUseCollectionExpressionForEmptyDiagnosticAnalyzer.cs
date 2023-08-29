// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

/// <summary>
/// Analyzer/fixer that looks for code of the form <c>X.Empty&lt;T&gt;()</c> or <c>X&lt;T&gt;.Empty</c> and offers to
/// replace with <c>[]</c> if legal to do so.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    public CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForEmptyDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForEmpty)
    {
    }

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
        => context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
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

        if (!CanReplaceWithCollectionExpression(semanticModel, nodeToReplace, skipVerificationForReplacedNode: true, cancellationToken))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: ImmutableArray.Create(nodeToReplace.GetLocation()),
            properties: null));

        return;
    }
}
