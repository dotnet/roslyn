// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCompoundAssignment;

internal abstract class AbstractUseCompoundAssignmentDiagnosticAnalyzer<
    TSyntaxKind,
    TAssignmentSyntax,
    TBinaryExpressionSyntax>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
    where TAssignmentSyntax : SyntaxNode
    where TBinaryExpressionSyntax : SyntaxNode
{
    private readonly ISyntaxFacts _syntaxFacts;

    /// <summary>
    /// Maps from a binary expression kind (like AddExpression) to the corresponding assignment
    /// form (like AddAssignmentExpression).
    /// </summary>
    private readonly ImmutableDictionary<TSyntaxKind, TSyntaxKind> _binaryToAssignmentMap;

    private readonly DiagnosticDescriptor _incrementDescriptor;

    private readonly DiagnosticDescriptor _decrementDescriptor;

    protected AbstractUseCompoundAssignmentDiagnosticAnalyzer(
        ISyntaxFacts syntaxFacts,
        ImmutableArray<(TSyntaxKind exprKind, TSyntaxKind assignmentKind, TSyntaxKind tokenKind)> kinds)
        : base(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
               EnforceOnBuildValues.UseCompoundAssignment,
               CodeStyleOptions2.PreferCompoundAssignment,
               new LocalizableResourceString(
                   nameof(AnalyzersResources.Use_compound_assignment), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
        _syntaxFacts = syntaxFacts;
        UseCompoundAssignmentUtilities.GenerateMaps(kinds, out _binaryToAssignmentMap, out _);

        var useIncrementMessage = new LocalizableResourceString(
            nameof(AnalyzersResources.Use_increment_operator), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        _incrementDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
            EnforceOnBuildValues.UseCompoundAssignment,
            hasAnyCodeStyleOption: true,
            useIncrementMessage, useIncrementMessage);

        var useDecrementMessage = new LocalizableResourceString(
            nameof(AnalyzersResources.Use_decrement_operator), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        _decrementDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
            EnforceOnBuildValues.UseCompoundAssignment,
            hasAnyCodeStyleOption: true,
            useDecrementMessage, useDecrementMessage);
    }

    protected abstract TSyntaxKind GetAnalysisKind();
    protected abstract bool IsSupported(TSyntaxKind assignmentKind, ParseOptions options);
    protected abstract int TryGetIncrementOrDecrement(TSyntaxKind opKind, object constantValue);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeAssignment, GetAnalysisKind());

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (TAssignmentSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        var syntaxTree = assignment.SyntaxTree;
        var option = context.GetAnalyzerOptions().PreferCompoundAssignment;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
        {
            // Bail immediately if the user has disabled this feature.
            return;
        }

        _syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(assignment,
            out var assignmentLeft, out var assignmentToken, out var assignmentRight);

        assignmentRight = _syntaxFacts.WalkDownParentheses(assignmentRight);

        // has to be of the form:  a = b op c
        // op has to be a form we could convert into op=
        if (assignmentRight is not TBinaryExpressionSyntax binaryExpression)
        {
            return;
        }

        var binaryKind = _syntaxFacts.SyntaxKinds.Convert<TSyntaxKind>(binaryExpression.RawKind);
        if (!_binaryToAssignmentMap.ContainsKey(binaryKind))
        {
            return;
        }

        // Requires at least C# 8 for Coalesce compound expression
        if (!IsSupported(binaryKind, syntaxTree.Options))
        {
            return;
        }

        _syntaxFacts.GetPartsOfBinaryExpression(binaryExpression,
            out var binaryLeft, out var binaryRight);

        // has to be of the form:   expr = expr op ...
        if (!_syntaxFacts.AreEquivalent(assignmentLeft, binaryLeft))
        {
            return;
        }

        // Don't offer if this is `x = x + 1` inside an obj initializer like:
        // `new Point { x = x + 1 }` or
        // `new () { x = x + 1 }` or
        // `p with { x = x + 1 }`
        if (_syntaxFacts.IsMemberInitializerNamedAssignmentIdentifier(assignmentLeft))
        {
            return;
        }

        // Don't offer if this is `x = x ?? throw new Exception()`
        if (_syntaxFacts.IsThrowExpression(binaryRight))
        {
            return;
        }

        // Syntactically looks promising.  But we can only safely do this if 'expr'
        // is side-effect-free since we will be changing the number of times it is
        // executed from twice to once.
        var semanticModel = context.SemanticModel;
        if (!UseCompoundAssignmentUtilities.IsSideEffectFree(
                _syntaxFacts, assignmentLeft, semanticModel, cancellationToken))
        {
            return;
        }

        var constant = semanticModel.GetConstantValue(binaryRight, cancellationToken).Value;
        if (constant != null)
        {
            var incrementOrDecrement = TryGetIncrementOrDecrement(binaryKind, constant);
            if (incrementOrDecrement == 1)
            {
                var operation = (IBinaryOperation)semanticModel.GetRequiredOperation(binaryExpression, cancellationToken);

                // We can suggest using increment operator only if it is a built-in one (in such case `OperatorMethod` is null)
                // or if increment operator is defined in the containing type
                if (operation.OperatorMethod is null ||
                    operation.OperatorMethod.ContainingType.GetMembers(WellKnownMemberNames.IncrementOperatorName).Length > 0)
                {
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        _incrementDescriptor,
                        assignmentToken.GetLocation(),
                        option.Notification,
                        context.Options,
            additionalLocations: ImmutableArray.Create(assignment.GetLocation()),
                        properties: ImmutableDictionary.Create<string, string?>()
                            .Add(UseCompoundAssignmentUtilities.Increment, UseCompoundAssignmentUtilities.Increment)));
                    return;
                }
            }
            else if (incrementOrDecrement == -1)
            {
                var operation = (IBinaryOperation)semanticModel.GetRequiredOperation(binaryExpression, cancellationToken);

                // We can suggest using decrement operator only if it is a built-in one (in such case `OperatorMethod` is null)
                // or if decrement operator is defined in the containing type
                if (operation.OperatorMethod is null ||
                    operation.OperatorMethod.ContainingType.GetMembers(WellKnownMemberNames.DecrementOperatorName).Length > 0)
                {
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        _decrementDescriptor,
                        assignmentToken.GetLocation(),
                        option.Notification,
                        context.Options,
                        additionalLocations: ImmutableArray.Create(assignment.GetLocation()),
                        properties: ImmutableDictionary.Create<string, string?>()
                            .Add(UseCompoundAssignmentUtilities.Decrement, UseCompoundAssignmentUtilities.Decrement)));
                    return;
                }
            }
        }

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            assignmentToken.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: ImmutableArray.Create(assignment.GetLocation()),
            properties: null));
    }
}
