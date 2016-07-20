// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess
{
    internal abstract class QualifyMemberAccessDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_shouldBeQualifiedMessage = new LocalizableResourceString(nameof(WorkspacesResources.Member_access_should_be_qualified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly LocalizableString s_qualifyMembersTitle = new LocalizableResourceString(nameof(FeaturesResources.Add_this_or_Me_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static readonly DiagnosticDescriptor s_descriptorQualifyMemberAccessInfo = new DiagnosticDescriptor(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                                                    s_qualifyMembersTitle,
                                                                    s_shouldBeQualifiedMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);
        private static readonly DiagnosticDescriptor s_descriptorQualifyMemberAccessWarning = new DiagnosticDescriptor(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                                                    s_qualifyMembersTitle,
                                                                    s_shouldBeQualifiedMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);
        private static readonly DiagnosticDescriptor s_descriptorQualifyMemberAccessError = new DiagnosticDescriptor(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                                                    s_qualifyMembersTitle,
                                                                    s_shouldBeQualifiedMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Error,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            s_descriptorQualifyMemberAccessInfo,
            s_descriptorQualifyMemberAccessWarning,
            s_descriptorQualifyMemberAccessError);

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

            // if we can't find a member then we can't do anything
            if (memberReference.Member == null)
            {
                return;
            }

            // get the option
            var optionSet = context.Options.GetOptionSet();
            if (optionSet == null)
            {
                return;
            }

            var language = context.Operation.Syntax.Language;
            var applicableOption = GetApplicableOptionFromSymbolKind(memberReference.Member.Kind);
            var optionValue = optionSet.GetOption(applicableOption, language);

            var shouldOptionBePresent = optionValue.Value;
            var isQualificationPresent = IsAlreadyQualifiedMemberAccess(memberReference.Instance.Syntax);
            if (shouldOptionBePresent && !isQualificationPresent)
            {
                DiagnosticDescriptor descriptor;
                switch (optionValue.Notification.Value)
                {
                    case DiagnosticSeverity.Hidden:
                        descriptor = null;
                        break;
                    case DiagnosticSeverity.Info:
                        descriptor = s_descriptorQualifyMemberAccessInfo;
                        break;
                    case DiagnosticSeverity.Warning:
                        descriptor = s_descriptorQualifyMemberAccessWarning;
                        break;
                    case DiagnosticSeverity.Error:
                        descriptor = s_descriptorQualifyMemberAccessError;
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                if (descriptor != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, context.Operation.Syntax.GetLocation()));
                }
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }

        internal static PerLanguageOption<CodeStyleOption<bool>> GetApplicableOptionFromSymbolKind(SymbolKind symbolKind)
        {
            switch (symbolKind)
            {
                case SymbolKind.Field:
                    return CodeStyleOptions.QualifyFieldAccess;
                case SymbolKind.Property:
                    return CodeStyleOptions.QualifyPropertyAccess;
                case SymbolKind.Method:
                    return CodeStyleOptions.QualifyMethodAccess;
                case SymbolKind.Event:
                    return CodeStyleOptions.QualifyEventAccess;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
