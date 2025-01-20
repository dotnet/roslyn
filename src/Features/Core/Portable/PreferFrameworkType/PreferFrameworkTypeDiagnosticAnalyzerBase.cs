// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PreferFrameworkType;

internal abstract class PreferFrameworkTypeDiagnosticAnalyzerBase<
    TSyntaxKind,
    TExpressionSyntax,
    TTypeSyntax,
    TIdentifierNameSyntax,
    TPredefinedTypeSyntax> :
    AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TTypeSyntax : TExpressionSyntax
    where TPredefinedTypeSyntax : TTypeSyntax
    where TIdentifierNameSyntax : TTypeSyntax
{
    protected PreferFrameworkTypeDiagnosticAnalyzerBase()
        : base(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId,
               EnforceOnBuildValues.PreferBuiltInOrFrameworkType,
               options:
               [
                   CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                   CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess,
               ],
               new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
               new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected abstract ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
    protected abstract bool IsPredefinedTypeReplaceableWithFrameworkType(TPredefinedTypeSyntax node);
    protected abstract bool IsIdentifierNameReplaceableWithFrameworkType(SemanticModel semanticModel, TIdentifierNameSyntax node);
    protected abstract bool IsInMemberAccessOrCrefReferenceContext(TExpressionSyntax node);

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);

    protected void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var options = context.GetAnalyzerOptions();
        var cancellationToken = context.CancellationToken;

        // if the user never prefers this style, do not analyze at all.
        // we don't know the context of the node yet, so check all predefined type option preferences and bail early.
        if (!IsFrameworkTypePreferred(options.PreferPredefinedTypeKeywordInDeclaration)
            && !IsFrameworkTypePreferred(options.PreferPredefinedTypeKeywordInMemberAccess)
            && ShouldSkipAnalysis(context.FilterTree, context.Options, context.Compilation.Options,
                [
                    options.PreferPredefinedTypeKeywordInDeclaration.Notification,
                    options.PreferPredefinedTypeKeywordInMemberAccess.Notification,
                ],
                context.CancellationToken))
        {
            return;
        }

        var typeNode = (TTypeSyntax)context.Node;

        // check if the predefined type is replaceable with an equivalent framework type.
        switch (typeNode)
        {
            case TPredefinedTypeSyntax predefinedType:
                if (!IsPredefinedTypeReplaceableWithFrameworkType(predefinedType))
                    return;
                break;
            case TIdentifierNameSyntax identifierName:
                if (!IsIdentifierNameReplaceableWithFrameworkType(semanticModel, identifierName))
                    return;
                break;
            default:
                return;
        }

        // check we have a symbol so that the fixer can generate the right type syntax from it.
        if (semanticModel.GetSymbolInfo(typeNode, cancellationToken).Symbol is not ITypeSymbol typeSymbol ||
            typeSymbol.SpecialType is SpecialType.None)
        {
            return;
        }

        // earlier we did a context insensitive check to see if this style was preferred in *any* context at all.
        // now, we have to make a context sensitive check to see if options settings for our context requires us to report a diagnostic.
        var optionValue = IsInMemberAccessOrCrefReferenceContext(typeNode)
            ? options.PreferPredefinedTypeKeywordInMemberAccess
            : options.PreferPredefinedTypeKeywordInDeclaration;

        if (IsFrameworkTypePreferred(optionValue))
        {
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor, typeNode.GetLocation(),
                optionValue.Notification, context.Options, additionalLocations: null,
                PreferFrameworkTypeConstants.Properties));
        }

        static bool IsFrameworkTypePreferred(CodeStyleOption2<bool> optionValue)
            => !optionValue.Value && optionValue.Notification.Severity != ReportDiagnostic.Suppress;
    }
}
