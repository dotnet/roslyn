﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QualifyMemberAccess
{
    internal abstract class AbstractQualifyMemberAccessDiagnosticAnalyzer<
        TLanguageKindEnum,
        TExpressionSyntax,
        TSimpleNameSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TExpressionSyntax : SyntaxNode
        where TSimpleNameSyntax : TExpressionSyntax
    {
        protected AbstractQualifyMemberAccessDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddQualificationDiagnosticId,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.QualifyPropertyAccess, CodeStyleOptions.QualifyMethodAccess, CodeStyleOptions.QualifyEventAccess),
                   new LocalizableResourceString(nameof(WorkspacesResources.Member_access_should_be_qualified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Add_this_or_Me_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override bool OpenFileOnly(OptionSet options)
        {
            var qualifyFieldAccessOption = options.GetOption(CodeStyleOptions.QualifyFieldAccess, GetLanguageName()).Notification;
            var qualifyPropertyAccessOption = options.GetOption(CodeStyleOptions.QualifyPropertyAccess, GetLanguageName()).Notification;
            var qualifyMethodAccessOption = options.GetOption(CodeStyleOptions.QualifyMethodAccess, GetLanguageName()).Notification;
            var qualifyEventAccessOption = options.GetOption(CodeStyleOptions.QualifyEventAccess, GetLanguageName()).Notification;

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
        protected abstract bool CanMemberAccessBeQualified(ISymbol containingSymbol, SyntaxNode node);

        protected abstract bool IsAlreadyQualifiedMemberAccess(TExpressionSyntax node);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.FieldReference, OperationKind.PropertyReference, OperationKind.MethodReference, OperationKind.Invocation);

        protected abstract Location GetLocation(IOperation operation);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            if (context.ContainingSymbol.IsStatic)
            {
                return;
            }

            switch (context.Operation)
            {
                case IMemberReferenceOperation memberReferenceOperation:
                    AnalyzeOperation(context, memberReferenceOperation, memberReferenceOperation.Instance);
                    break;
                case IInvocationOperation invocationOperation:
                    AnalyzeOperation(context, invocationOperation, invocationOperation.Instance);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(context.Operation);
            }
        }

        private void AnalyzeOperation(OperationAnalysisContext context, IOperation operation, IOperation instanceOperation)
        {
            // this is a static reference so we don't care if it's qualified
            if (instanceOperation == null)
            {
                return;
            }

            // if we're not referencing `this.` or `Me.` (e.g., a parameter, local, etc.)
            if (instanceOperation.Kind != OperationKind.InstanceReference)
            {
                return;
            }

            // We shouldn't qualify if it is inside a property pattern
            if (context.Operation.Parent.Kind == OperationKind.PropertySubpattern)
            {
                return;
            }

            // Initializer lists are IInvocationOperation which if passed to GetApplicableOptionFromSymbolKind
            // will incorrectly fetch the options for method call.
            // We still want to handle InstanceReferenceKind.ContainingTypeInstance
            if ((instanceOperation as IInstanceReferenceOperation)?.ReferenceKind == InstanceReferenceKind.ImplicitReceiver)
            {
                return;
            }

            // If we can't be qualified (e.g., because we're already qualified with `base.`), we're done.
            if (!CanMemberAccessBeQualified(context.ContainingSymbol, instanceOperation.Syntax))
            {
                return;
            }

            // if we can't find a member then we can't do anything.  Also, we shouldn't qualify
            // accesses to static members.  
            if (IsStaticMemberOrIsLocalFunction(operation))
            {
                return;
            }

            if (!(instanceOperation.Syntax is TSimpleNameSyntax simpleName))
            {
                return;
            }

            var applicableOption = QualifyMembersHelpers.GetApplicableOptionFromSymbolKind(operation);
            var optionValue = context.GetOption(applicableOption, context.Operation.Syntax.Language);

            var shouldOptionBePresent = optionValue.Value;
            var severity = optionValue.Notification.Severity;
            if (!shouldOptionBePresent || severity == ReportDiagnostic.Suppress)
            {
                return;
            }

            if (!IsAlreadyQualifiedMemberAccess(simpleName))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    GetLocation(operation),
                    severity,
                    additionalLocations: null,
                    properties: null));
            }
        }

        private bool IsStaticMemberOrIsLocalFunction(IOperation operation)
        {
            switch (operation)
            {
                case IMemberReferenceOperation memberReferenceOperation:
                    return IsStaticMemberOrIsLocalFunctionHelper(memberReferenceOperation.Member);
                case IInvocationOperation invocationOperation:
                    return IsStaticMemberOrIsLocalFunctionHelper(invocationOperation.TargetMethod);
                default:
                    throw ExceptionUtilities.UnexpectedValue(operation);
            }

            static bool IsStaticMemberOrIsLocalFunctionHelper(ISymbol symbol)
            {
                return symbol == null || symbol.IsStatic || symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction };
            }
        }
    }

    internal static class QualifyMembersHelpers
    {
        public static PerLanguageOption<CodeStyleOption<bool>> GetApplicableOptionFromSymbolKind(SymbolKind symbolKind)
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

        internal static PerLanguageOption<CodeStyleOption<bool>> GetApplicableOptionFromSymbolKind(IOperation operation)
        {
            switch (operation)
            {
                case IMemberReferenceOperation memberReferenceOperation:
                    return GetApplicableOptionFromSymbolKind(memberReferenceOperation.Member.Kind);
                case IInvocationOperation invocationOperation:
                    return GetApplicableOptionFromSymbolKind(invocationOperation.TargetMethod.Kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(operation);
            }
        }
    }
}
