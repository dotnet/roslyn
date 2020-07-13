// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.ConvertTypeofToNameof
{
    internal abstract class AbstractConvertTypeofToNameofDiagnosticAnalyzer<
        TLanguageKindEnum,
        TExpressionSyntax,
        TSimpleNameSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TExpressionSyntax : SyntaxNode
        where TSimpleNameSyntax : TExpressionSyntax
    {
        protected AbstractConvertTypeofToNameofDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddQualificationDiagnosticId,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOptions2.QualifyPropertyAccess, CodeStyleOptions2.QualifyMethodAccess, CodeStyleOptions2.QualifyEventAccess),
                   new LocalizableResourceString(nameof(AnalyzersResources.Member_access_should_be_qualified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Add_this_or_Me_qualification), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override bool OpenFileOnly(OptionSet options)
        {
            var qualifyFieldAccessOption = options.GetOption(CodeStyleOptions2.QualifyFieldAccess, GetLanguageName()).Notification;
            var qualifyPropertyAccessOption = options.GetOption(CodeStyleOptions2.QualifyPropertyAccess, GetLanguageName()).Notification;
            var qualifyMethodAccessOption = options.GetOption(CodeStyleOptions2.QualifyMethodAccess, GetLanguageName()).Notification;
            var qualifyEventAccessOption = options.GetOption(CodeStyleOptions2.QualifyEventAccess, GetLanguageName()).Notification;

            return !(qualifyFieldAccessOption == NotificationOption2.Warning || qualifyFieldAccessOption == NotificationOption2.Error ||
                     qualifyPropertyAccessOption == NotificationOption2.Warning || qualifyPropertyAccessOption == NotificationOption2.Error ||
                     qualifyMethodAccessOption == NotificationOption2.Warning || qualifyMethodAccessOption == NotificationOption2.Error ||
                     qualifyEventAccessOption == NotificationOption2.Warning || qualifyEventAccessOption == NotificationOption2.Error);
        }

        protected abstract string GetLanguageName();

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.FieldReference, OperationKind.PropertyReference, OperationKind.MethodReference, OperationKind.Invocation);


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

        }
    }
}
