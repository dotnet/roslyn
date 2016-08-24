// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics.PreferFrameworkType
{
    internal abstract class PreferFrameworkTypeDiagnosticAnalyzerBase<TSyntaxKind> : DiagnosticAnalyzer, IBuiltInAnalyzer where TSyntaxKind : struct
    {
        private static readonly LocalizableString s_preferFrameworkTypeMessage = new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static readonly LocalizableString s_preferFrameworkTypeTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_framework_type), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static readonly DiagnosticDescriptor s_descriptorPreferFrameworkTypeInDeclarations = new DiagnosticDescriptor(IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                                                                    s_preferFrameworkTypeTitle,
                                                                    s_preferFrameworkTypeMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_descriptorPreferFrameworkTypeInMemberAccess = new DiagnosticDescriptor(IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId,
                                                                    s_preferFrameworkTypeTitle,
                                                                    s_preferFrameworkTypeMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true);

        public bool RunInProcess => true;

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            s_descriptorPreferFrameworkTypeInDeclarations, s_descriptorPreferFrameworkTypeInMemberAccess);

        protected abstract ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
        protected abstract bool IsPredefinedTypeAndReplaceableWithFrameworkType(SyntaxNode node);
        protected abstract bool IsInMemberAccessOrCrefReferenceContext(SyntaxNode node, SemanticModel semanticModel);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);
        }

        protected void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var predefinedTypeNode = context.Node;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetOptionSet();
            if (optionSet == null)
            {
                return;
            }

            string applicableDiagnosticId = null;
            PerLanguageOption<CodeStyleOption<bool>> applicableOption = null;

            // check if the predefined type is replaceable with an equivalent framework type.
            if (!IsPredefinedTypeAndReplaceableWithFrameworkType(predefinedTypeNode))
            {
                return;
            }

            // we have a predefined type syntax that is either in a member access context or a declaration context. 
            if (IsInMemberAccessOrCrefReferenceContext(predefinedTypeNode, semanticModel))
            {
                applicableOption = CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;
                applicableDiagnosticId = IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId;
            }
            else
            {
                applicableOption = CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration;
                applicableDiagnosticId = IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId;
            }

            var optionValue = optionSet.GetOption(applicableOption, semanticModel.Language);
            var preferFrameworkType = !optionValue.Value;

            if (preferFrameworkType && optionValue.Notification.Value != DiagnosticSeverity.Hidden)
            {
                var descriptor = new DiagnosticDescriptor(applicableDiagnosticId,
                        s_preferFrameworkTypeTitle,
                        s_preferFrameworkTypeMessage,
                        DiagnosticCategory.Style,
                        optionValue.Notification.Value,
                        isEnabledByDefault: true);

                context.ReportDiagnostic(Diagnostic.Create(descriptor, predefinedTypeNode.GetLocation()));
            }
        }
    }
}
