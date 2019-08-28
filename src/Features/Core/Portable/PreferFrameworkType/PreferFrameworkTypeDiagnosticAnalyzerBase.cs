// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

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
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        private static PerLanguageOption<CodeStyleOption<bool>> GetOptionForDeclarationContext
            => CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration;

        private static PerLanguageOption<CodeStyleOption<bool>> GetOptionForMemberAccessContext
            => CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;

        public override bool OpenFileOnly(Workspace workspace)
        {
            var preferTypeKeywordInDeclarationOption = workspace.Options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, GetLanguageName()).Notification;
            var preferTypeKeywordInMemberAccessOption = workspace.Options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, GetLanguageName()).Notification;

            return !(preferTypeKeywordInDeclarationOption == NotificationOption.Warning || preferTypeKeywordInDeclarationOption == NotificationOption.Error ||
                     preferTypeKeywordInMemberAccessOption == NotificationOption.Warning || preferTypeKeywordInMemberAccessOption == NotificationOption.Error);
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
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var language = semanticModel.Language;

            // if the user never prefers this style, do not analyze at all.
            // we don't know the context of the node yet, so check all predefined type option preferences and bail early.
            if (!IsStylePreferred(optionSet, language))
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
            if (!(semanticModel.GetSymbolInfo(predefinedTypeNode, cancellationToken).Symbol is ITypeSymbol typeSymbol))
            {
                return;
            }

            // earlier we did a context insensitive check to see if this style was preferred in *any* context at all.
            // now, we have to make a context sensitive check to see if options settings for our context requires us to report a diagnostic.
            if (ShouldReportDiagnostic(predefinedTypeNode, optionSet, language,
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
            TPredefinedTypeSyntax predefinedTypeNode, OptionSet optionSet,
            string language, out ReportDiagnostic severity)
        {
            // we have a predefined type syntax that is either in a member access context or a declaration context. 
            // check the appropriate option and determine if we should report a diagnostic.
            var isMemberAccessOrCref = IsInMemberAccessOrCrefReferenceContext(predefinedTypeNode);

            var option = isMemberAccessOrCref ? GetOptionForMemberAccessContext : GetOptionForDeclarationContext;
            var optionValue = optionSet.GetOption(option, language);

            severity = optionValue.Notification.Severity;
            return OptionSettingPrefersFrameworkType(optionValue, severity);
        }

        private bool IsStylePreferred(OptionSet optionSet, string language)
            => IsFrameworkTypePreferred(optionSet, GetOptionForDeclarationContext, language) ||
               IsFrameworkTypePreferred(optionSet, GetOptionForMemberAccessContext, language);

        private bool IsFrameworkTypePreferred(OptionSet optionSet, PerLanguageOption<CodeStyleOption<bool>> option, string language)
        {
            var optionValue = optionSet.GetOption(option, language);
            return OptionSettingPrefersFrameworkType(optionValue, optionValue.Notification.Severity);
        }

        /// <summary>
        /// checks if style is preferred and the enforcement is not None.
        /// </summary>
        /// <remarks>if predefined type is not preferred, it implies the preference is framework type.</remarks>
        private static bool OptionSettingPrefersFrameworkType(CodeStyleOption<bool> optionValue, ReportDiagnostic severity)
            => !optionValue.Value && severity != ReportDiagnostic.Suppress;
    }
}
