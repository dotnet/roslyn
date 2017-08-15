// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QualifyMemberAccess
{
    internal abstract class AbstractQualifyMemberAccessDiagnosticAnalyzer<TLanguageKindEnum> :
        AbstractCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
    {
        protected AbstractQualifyMemberAccessDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.AddQualificationDiagnosticId,
                   new LocalizableResourceString(nameof(WorkspacesResources.Member_access_should_be_qualified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Add_this_or_Me_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override bool OpenFileOnly(Workspace workspace)
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

        /// <summary>
        /// Reports on whether the specified member is suitable for qualification. Some member
        /// access expressions cannot be qualified; for instance if they begin with <c>base.</c>,
        /// <c>MyBase.</c>, or <c>MyClass.</c>.
        /// </summary>
        /// <returns>True if the member access can be qualified; otherwise, False.</returns>
        protected abstract bool CanMemberAccessBeQualified(SyntaxNode node);

        protected abstract bool IsAlreadyQualifiedMemberAccess(SyntaxNode node);

        private static MethodInfo s_registerMethod = typeof(AnalysisContext).GetTypeInfo().GetDeclaredMethod("RegisterOperationActionImmutableArrayInternal");

        protected override void InitializeWorker(AnalysisContext context)
            => s_registerMethod.Invoke(context, new object[]
               {
                   new Action<OperationAnalysisContext>(AnalyzeOperation),
                   ImmutableArray.Create(OperationKind.FieldReferenceExpression, OperationKind.PropertyReferenceExpression, OperationKind.MethodBindingExpression)
               });

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            if (context.ContainingSymbol.IsStatic)
            {
                return;
            }

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

            // If we can't be qualified (e.g., because we're already qualified with `base.`), we're done.
            if (!CanMemberAccessBeQualified(memberReference.Instance.Syntax))
            {
                return;
            }

            // if we can't find a member then we can't do anything.  Also, we shouldn't qualify
            // accesses to static members.  
            if (memberReference.Member == null ||
                memberReference.Member.IsStatic)
            {
                return;
            }

            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
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
                    context.ReportDiagnostic(Diagnostic.Create(
                        GetDescriptorWithSeverity(severity), 
                        context.Operation.Syntax.GetLocation()));
                }
            }
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
                    throw ExceptionUtilities.UnexpectedValue(symbolKind);
            }
        }
    }
}
