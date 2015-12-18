// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

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
        protected abstract bool IsAlreadyQualifiedMemberAccess(SyntaxNode node);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.FieldReferenceExpression, OperationKind.PropertyReferenceExpression, OperationKind.MethodBindingExpression);
        }

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var memberReference = (IMemberReferenceExpression)context.Operation;
            if (memberReference.Instance == null)
            {
                return;
            }

            if (IsAlreadyQualifiedMemberAccess(memberReference.Instance.Syntax))
            {
                return;
            }

            var optionSet = GetOptionSet(context.Options);
            if (optionSet == null)
            {
                return;
            }

            if (memberReference.Member == null)
            {
                return;
            }

            if (memberReference.Member.ContainingSymbol != memberReference.Instance.ResultType)
            {
                return;
            }

            var language = context.Operation.Syntax.Language;
            if (!SimplificationHelpers.ShouldSimplifyMemberAccessExpression(memberReference.Member, language, optionSet))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_descriptorQualifyMemberAccess, context.Operation.Syntax.GetLocation()));
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
