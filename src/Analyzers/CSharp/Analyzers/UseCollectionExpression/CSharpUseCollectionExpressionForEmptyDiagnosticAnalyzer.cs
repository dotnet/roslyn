// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

/// <summary>
/// Analyzer/fixer that looks for code of the form <c>X.Empty&lt;T&gt;()</c> or <c>X&lt;T&gt;.Empty</c> and offers to
/// replace with <c>[]</c> if legal to do so.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    private const string EmptyName = nameof(Array.Empty);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForEmptyDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForEmpty,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForEmptyDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForEmpty,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    public CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer()
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                .Add(s_descriptor, CodeStyleOptions2.PreferCollectionExpression)
                .Add(s_unnecessaryCodeDescriptor, CodeStyleOptions2.PreferCollectionExpression))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            if (!context.Compilation.LanguageVersion().SupportsCollectionExpressions())
                return;

            // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
            // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
            // and eventually report fading diagnostics with location outside this node.
            // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
            // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
            context.RegisterCodeBlockStartAction<SyntaxKind>(context =>
            {
                context.RegisterSyntaxNodeAction(
                    context => AnalyzeMemberAccess(context),
                    SyntaxKind.SimpleMemberAccessExpression);
            });
        });

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        // X.Empty<T>() or X<T>.Empty

        var nodeToReplace =
            IsEmptyProperty() ? memberAccess :
            IsEmptyMethodCall() ? (ExpressionSyntax)memberAccess.GetRequiredParent() : null;
        if (nodeToReplace is null)
            return;

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(semanticModel, nodeToReplace, skipVerificationForReplacedNode: true, cancellationToken))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: ImmutableArray.Create(nodeToReplace.GetLocation()),
            properties: null));

        return;

        // X<T>.Empty
        bool IsEmptyProperty()
        {
            if (!IsPossiblyDottedGenericName(memberAccess.Expression))
                return false;

            if (memberAccess.Name is not IdentifierNameSyntax { Identifier.ValueText: EmptyName })
                return false;

            var expressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            if (expressionSymbol is not INamedTypeSymbol)
                return false;

            var emptySymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (emptySymbol is not { IsStatic: true })
                return false;

            if (emptySymbol is not IFieldSymbol and not IPropertySymbol)
                return false;

            return true;
        }

        // X.Empty<T>()
        bool IsEmptyMethodCall()
        {
            if (memberAccess is not
                {
                    Parent: InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 },
                    Name: GenericNameSyntax
                    {
                        TypeArgumentList.Arguments.Count: 1,
                        Identifier.ValueText: EmptyName,
                    },
                })
            {
                return false;
            }

            if (!IsPossiblyDottedName(memberAccess.Expression))
                return false;

            var expressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            if (expressionSymbol is not INamedTypeSymbol)
                return false;

            var emptySymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (emptySymbol is not { IsStatic: true })
                return false;

            if (emptySymbol is not IMethodSymbol)
                return false;

            return true;
        }

        static bool IsPossiblyDottedGenericName(ExpressionSyntax expression)
        {
            if (expression is GenericNameSyntax)
                return true;

            if (expression is MemberAccessExpressionSyntax { Expression: ExpressionSyntax childName, Name: GenericNameSyntax } &&
                IsPossiblyDottedName(childName))
            {
                return true;
            }

            return false;
        }

        static bool IsPossiblyDottedName(ExpressionSyntax name)
        {
            if (name is IdentifierNameSyntax)
                return true;

            if (name is MemberAccessExpressionSyntax { Expression: ExpressionSyntax childName, Name: IdentifierNameSyntax } &&
                IsPossiblyDottedName(childName))
            {
                return true;
            }

            return false;
        }
    }
}
