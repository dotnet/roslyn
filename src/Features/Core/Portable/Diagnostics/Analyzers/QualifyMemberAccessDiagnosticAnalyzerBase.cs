﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptorQualifyMemberAccess);

        protected abstract bool IsAlreadyQualifiedMemberAccess(SyntaxNode node);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.FieldReferenceExpression, OperationKind.PropertyReferenceExpression, OperationKind.MethodBindingExpression);
        }

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var memberReference = (IMemberReferenceExpression)context.Operation;

            // this is a static reference so we don't care if it's qualified
            if (memberReference.Instance == null)
            {
                return;
            }

            // if we're not referencing `this.` or `Me.` (e.g., a parameter, local, etc.)
            if (memberReference.Instance.Kind != OperationKind.InstanceReferenceExpression)
            {
                return;
            }

            // if there's already a `this.` or `Me.` qualification
            if (IsAlreadyQualifiedMemberAccess(memberReference.Instance.Syntax))
            {
                return;
            }

            var optionSet = context.Options.GetOptionSet();
            if (optionSet == null)
            {
                return;
            }

            if (memberReference.Member == null)
            {
                return;
            }

            var language = context.Operation.Syntax.Language;
            if (!SimplificationHelpers.ShouldSimplifyMemberAccessExpression(memberReference.Member, language, optionSet))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_descriptorQualifyMemberAccess, context.Operation.Syntax.GetLocation()));
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
