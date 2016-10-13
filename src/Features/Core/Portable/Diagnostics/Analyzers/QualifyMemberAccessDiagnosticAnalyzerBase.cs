// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
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

        private static readonly DiagnosticDescriptor s_descriptorQualifyMemberAccess = new DiagnosticDescriptor(IDEDiagnosticIds.AddQualificationDiagnosticId,
                                                                    s_qualifyMembersTitle,
                                                                    s_shouldBeQualifiedMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptorQualifyMemberAccess);

        public bool OpenFileOnly(Workspace workspace)
        {
            var qualifyFieldAccessOption = workspace.Options.GetOption(CodeStyleOptions.QualifyFieldAccess, GetLanguageName()).Notification;
            var qualifyPropertyAccessOption = workspace.Options.GetOption(CodeStyleOptions.QualifyPropertyAccess, GetLanguageName()).Notification;
            var qualifyMethodAccessOption = workspace.Options.GetOption(CodeStyleOptions.QualifyMethodAccess, GetLanguageName()).Notification;
            var qualifyEventAccessOption = workspace.Options.GetOption(CodeStyleOptions.QualifyEventAccess, GetLanguageName()).Notification;

            return !(qualifyFieldAccessOption == NotificationOption.Warning || qualifyFieldAccessOption == NotificationOption.Error ||
                     qualifyPropertyAccessOption == NotificationOption.Warning || qualifyPropertyAccessOption == NotificationOption.Error ||
                     qualifyMethodAccessOption == NotificationOption.Warning || qualifyMethodAccessOption == NotificationOption.Error ||
                     qualifyEventAccessOption == NotificationOption.Warning || qualifyEventAccessOption == NotificationOption.Error);
        }

        protected abstract string GetLanguageName();

        protected abstract bool IsAlreadyQualifiedMemberAccess(SyntaxNode node);

        public override void Initialize(AnalysisContext context)
        {
            var internalMethod = typeof(AnalysisContext).GetTypeInfo().GetDeclaredMethod("RegisterOperationActionImmutableArrayInternal");
            internalMethod.Invoke(context, new object[] { new Action<OperationAnalysisContext>(AnalyzeOperation), ImmutableArray.Create(OperationKind.FieldReferenceExpression, OperationKind.PropertyReferenceExpression, OperationKind.MethodBindingExpression) });
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
                var severity = optionValue.Notification.Value;
                if (severity != DiagnosticSeverity.Hidden)
                {
                    var descriptor = new DiagnosticDescriptor(
                        IDEDiagnosticIds.AddQualificationDiagnosticId,
                        s_qualifyMembersTitle,
                        s_shouldBeQualifiedMessage,
                        DiagnosticCategory.Style,
                        severity,
                        isEnabledByDefault: true,
                        customTags: DiagnosticCustomTags.Unnecessary);

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
