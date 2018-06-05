// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics.PreferFrameworkType
{
    internal abstract class PreferFrameworkTypeDiagnosticAnalyzerBase<TSyntaxKind, TExpressionSyntax, TPredefinedTypeSyntax> :
        DiagnosticAnalyzer, IBuiltInAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TPredefinedTypeSyntax : TExpressionSyntax
    {
        private static readonly LocalizableString s_preferFrameworkTypeMessage =
            new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type),
                                          FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static readonly LocalizableString s_preferFrameworkTypeTitle =
            new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type),
                                          FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static readonly DiagnosticDescriptor s_descriptorPreferFrameworkTypeInDeclarations =
            new DiagnosticDescriptor(
                IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                s_preferFrameworkTypeTitle,
                s_preferFrameworkTypeMessage,
                DiagnosticCategory.Style,
                DiagnosticSeverity.Hidden,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_descriptorPreferFrameworkTypeInMemberAccess =
            new DiagnosticDescriptor(
                IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId,
                s_preferFrameworkTypeTitle,
                s_preferFrameworkTypeMessage,
                DiagnosticCategory.Style,
                DiagnosticSeverity.Hidden,
                isEnabledByDefault: true);

        private PerLanguageOption<CodeStyleOption<bool>> GetOptionForDeclarationContext =>
            CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration;

        private PerLanguageOption<CodeStyleOption<bool>> GetOptionForMemberAccessContext =>
            CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;

        public bool OpenFileOnly(Workspace workspace)
        {
            var preferTypeKeywordInDeclarationOption = workspace.Options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, GetLanguageName()).Notification;
            var preferTypeKeywordInMemberAccessOption = workspace.Options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, GetLanguageName()).Notification;

            return !(preferTypeKeywordInDeclarationOption == NotificationOption.Warning || preferTypeKeywordInDeclarationOption == NotificationOption.Error ||
                     preferTypeKeywordInMemberAccessOption == NotificationOption.Warning || preferTypeKeywordInMemberAccessOption == NotificationOption.Error);
        }

        protected abstract string GetLanguageName();

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            s_descriptorPreferFrameworkTypeInDeclarations, s_descriptorPreferFrameworkTypeInMemberAccess);

        protected abstract ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
        protected abstract bool IsPredefinedTypeReplaceableWithFrameworkType(TPredefinedTypeSyntax node);
        protected abstract bool IsInMemberAccessOrCrefReferenceContext(TExpressionSyntax node);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
        }

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
            var typeSymbol = semanticModel.GetSymbolInfo(predefinedTypeNode, cancellationToken).Symbol as ITypeSymbol;
            if (typeSymbol == null)
            {
                return;
            }
            // earlier we did a context insensitive check to see if this style was preferred in *any* context at all.
            // now, we have to make a context sensitive check to see if options settings for our context requires us to report a diagnostic.
            if (ShouldReportDiagnostic(predefinedTypeNode, optionSet, language, out var descriptor, out var severity))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(descriptor, predefinedTypeNode.GetLocation(), severity, additionalLocations: null, properties: null));
            }
        }

        /// <summary>
        /// Detects the context of this occurrence of predefined type and determines if we should report it.
        /// </summary>
        private bool ShouldReportDiagnostic(TPredefinedTypeSyntax predefinedTypeNode, OptionSet optionSet, string language,
            out DiagnosticDescriptor descriptor, out ReportDiagnostic severity)
        {
            CodeStyleOption<bool> optionValue;

            // we have a predefined type syntax that is either in a member access context or a declaration context. 
            // check the appropriate option and determine if we should report a diagnostic.
            if (IsInMemberAccessOrCrefReferenceContext(predefinedTypeNode))
            {
                descriptor = s_descriptorPreferFrameworkTypeInMemberAccess;
                optionValue = optionSet.GetOption(GetOptionForMemberAccessContext, language);
            }
            else
            {
                descriptor = s_descriptorPreferFrameworkTypeInDeclarations;
                optionValue = optionSet.GetOption(GetOptionForDeclarationContext, language);
            }

            severity = optionValue.Notification.Severity;
            return OptionSettingPrefersFrameworkType(optionValue, severity);
        }

        private bool IsStylePreferred(OptionSet optionSet, string language) =>
            IsFrameworkTypePreferred(optionSet, GetOptionForDeclarationContext, language) ||
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
        private static bool OptionSettingPrefersFrameworkType(CodeStyleOption<bool> optionValue, ReportDiagnostic severity) =>
            !optionValue.Value && severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) < ReportDiagnostic.Hidden;
    }
}
