// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PreferFrameworkType
{
    internal abstract class PreferFrameworkTypeDiagnosticAnalyzerBase<TSyntaxKind, TExpressionSyntax, TPredefinedTypeSyntax> :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TPredefinedTypeSyntax : TExpressionSyntax
    {
        protected PreferFrameworkTypeDiagnosticAnalyzerBase()
            : base(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId,
                   EnforceOnBuildValues.PreferBuiltInOrFrameworkType,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        private static PerLanguageOption2<CodeStyleOption2<bool>> GetOptionForDeclarationContext
            => CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration;

        private static PerLanguageOption2<CodeStyleOption2<bool>> GetOptionForMemberAccessContext
            => CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;

        public override bool OpenFileOnly(SimplifierOptions? options)
        {
            // analyzer is only active in C# and VB projects
            Contract.ThrowIfNull(options);

            return
                !(options.PreferPredefinedTypeKeywordInDeclaration.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error ||
                  options.PreferPredefinedTypeKeywordInMemberAccess.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error);
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract string GetLanguageName();
        protected abstract ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
        protected abstract bool IsPredefinedTypeReplaceableWithFrameworkType(TPredefinedTypeSyntax node);
        protected abstract bool IsInMemberAccessOrCrefReferenceContext(TExpressionSyntax node);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);

        protected void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var language = semanticModel.Language;

            // if the user never prefers this style, do not analyze at all.
            // we don't know the context of the node yet, so check all predefined type option preferences and bail early.
            if (!IsStylePreferred(context, language))
            {
                return;
            }

            var predefinedTypeNode = (TPredefinedTypeSyntax)context.Node;

            // check if the predefined type is replaceable with an equivalent framework type.
            if (!IsPredefinedTypeReplaceableWithFrameworkType(predefinedTypeNode))
            {
                return;
            }

            // check we have a symbol so that the fixer can generate the right type syntax from it.
            if (semanticModel.GetSymbolInfo(predefinedTypeNode, context.CancellationToken).Symbol is not ITypeSymbol)
            {
                return;
            }

            // earlier we did a context insensitive check to see if this style was preferred in *any* context at all.
            // now, we have to make a context sensitive check to see if options settings for our context requires us to report a diagnostic.
            if (ShouldReportDiagnostic(predefinedTypeNode, context, language,
                    out var diagnosticSeverity))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor, predefinedTypeNode.GetLocation(),
                    diagnosticSeverity, additionalLocations: null,
                    PreferFrameworkTypeConstants.Properties));
            }
        }

        /// <summary>
        /// Detects the context of this occurrence of predefined type and determines if we should report it.
        /// </summary>
        private bool ShouldReportDiagnostic(
            TPredefinedTypeSyntax predefinedTypeNode,
            SyntaxNodeAnalysisContext context,
            string language,
            out ReportDiagnostic severity)
        {
            // we have a predefined type syntax that is either in a member access context or a declaration context. 
            // check the appropriate option and determine if we should report a diagnostic.
            var isMemberAccessOrCref = IsInMemberAccessOrCrefReferenceContext(predefinedTypeNode);

            var option = isMemberAccessOrCref ? GetOptionForMemberAccessContext : GetOptionForDeclarationContext;
            var optionValue = context.GetOption(option, language);

            severity = optionValue.Notification.Severity;
            return OptionSettingPrefersFrameworkType(optionValue, severity);
        }

        private static bool IsStylePreferred(
            SyntaxNodeAnalysisContext context,
            string language)
            => IsFrameworkTypePreferred(context, GetOptionForDeclarationContext, language) ||
               IsFrameworkTypePreferred(context, GetOptionForMemberAccessContext, language);

        private static bool IsFrameworkTypePreferred(
            SyntaxNodeAnalysisContext context,
            PerLanguageOption2<CodeStyleOption2<bool>> option,
            string language)
        {
            var optionValue = context.GetOption(option, language);
            return OptionSettingPrefersFrameworkType(optionValue, optionValue.Notification.Severity);
        }

        /// <summary>
        /// checks if style is preferred and the enforcement is not None.
        /// </summary>
        /// <remarks>if predefined type is not preferred, it implies the preference is framework type.</remarks>
        private static bool OptionSettingPrefersFrameworkType(CodeStyleOption2<bool> optionValue, ReportDiagnostic severity)
            => !optionValue.Value && severity != ReportDiagnostic.Suppress;
    }
}
