// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess
{
    internal abstract class QualifyMemberAccessDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.MemberAccessShouldBeQualified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly LocalizableString s_localizableTitleQualifyMembers = new LocalizableResourceString(nameof(FeaturesResources.AddThisOrMeQualification), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorQualifyMemberAccess = new DiagnosticDescriptor(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                                                    s_localizableTitleQualifyMembers,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        private OptionSet _lazyDefaultOptionSet;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptorQualifyMemberAccess);

        protected abstract ImmutableArray<TLanguageKindEnum> GetSupportedSyntaxKinds();
        protected abstract string GetLanguageName();
        protected abstract bool IsCandidate(SyntaxNode node);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, GetSupportedSyntaxKinds());
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!IsCandidate(context.Node))
            {
                return;
            }

            var optionSet = GetOptionSet(context.Options);
            if (optionSet == null)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;
            if (symbol == null)
            {
                return;
            }

            if (!SimplificationHelpers.ShouldSimplifyMemberAccessExpression(symbol, GetLanguageName(), optionSet))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_descriptorQualifyMemberAccess, context.Node.GetLocation()));
            }
        }

        private OptionSet GetOptionSet(AnalyzerOptions analyzerOptions)
        {
            var workspaceOptions = analyzerOptions as WorkspaceAnalyzerOptions;
            if (workspaceOptions != null)
            {
                return workspaceOptions.Workspace.Options;
            }

            if (_lazyDefaultOptionSet == null)
            {
                Interlocked.CompareExchange(ref _lazyDefaultOptionSet, new OptionSet(null), null);
            }

            return _lazyDefaultOptionSet;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
