// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    // This part contains all the logic for hooking up the DiagnosticAnalyzer to the CodeStyleProvider.
    // All the code in this part is an implementation detail and is intentionally private so that
    // subclasses cannot change anything.  All code relevant to subclasses relating to analysis
    // is contained in AbstractCodeStyleProvider.cs

    internal abstract partial class AbstractCodeStyleProvider<TOptionKind, TSyntaxKind, TCodeStyleProvider>
    {
        private void DiagnosticAnalyzerInitialize(AnalysisContext context)
        {
            var analyzeCodeBlock = GetCodeBlockAction();
            var analyzeSemanticModel = GetSemanticModelAction();
            var analyzeSyntaxTree = GetSyntaxTreeAction();
            var (syntaxKinds, analyzeSyntax) = GetSyntaxNodeAction();
            var (operationKinds, analyzeOperation) = GetOperationAction();

            if (analyzeCodeBlock != null) { context.RegisterCodeBlockAction(c => AnalyzeIfEnabled(c, analyzeCodeBlock)); }
            if (analyzeSemanticModel != null) { context.RegisterSemanticModelAction(c => AnalyzeIfEnabled(c, analyzeSemanticModel)); }
            if (analyzeSyntaxTree != null) { context.RegisterSyntaxTreeAction(c => AnalyzeIfEnabled(c, analyzeSyntaxTree)); }
            if (analyzeSyntax != null) { context.RegisterSyntaxNodeAction(c => AnalyzeIfEnabled(c, analyzeSyntax), syntaxKinds); }
            if (analyzeOperation != null) { context.RegisterOperationAction(c => AnalyzeIfEnabled(c, analyzeOperation), operationKinds); }

            // check for all the other things that can be registered as well.
        }

        private void AnalyzeIfEnabled(CodeBlockAnalysisContext context, Action<CodeBlockAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            => AnalyzeIfEnabled(context, analyze, context.Options, context.SemanticModel.SyntaxTree, context.CancellationToken);

        private void AnalyzeIfEnabled(OperationAnalysisContext context, Action<OperationAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            => AnalyzeIfEnabled(context, analyze, context.Options, context.Operation.SemanticModel.SyntaxTree, context.CancellationToken);

        private void AnalyzeIfEnabled(SemanticModelAnalysisContext context, Action<SemanticModelAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            => AnalyzeIfEnabled(context, analyze, context.Options, context.SemanticModel.SyntaxTree, context.CancellationToken);

        private void AnalyzeIfEnabled(SyntaxNodeAnalysisContext context, Action<SyntaxNodeAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            => AnalyzeIfEnabled(context, analyze, context.Options, context.SemanticModel.SyntaxTree, context.CancellationToken);

        private void AnalyzeIfEnabled(SyntaxTreeAnalysisContext context, Action<SyntaxTreeAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            => AnalyzeIfEnabled(context, analyze, context.Options, context.Tree, context.CancellationToken);

        private void AnalyzeIfEnabled<TContext>(
            TContext context, Action<TContext, CodeStyleOption<TOptionKind>> analyze,
            AnalyzerOptions options, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var optionValue = optionSet.GetOption(_option);
            switch (optionValue.Notification.Severity)
            {
                case ReportDiagnostic.Error:
                case ReportDiagnostic.Warn:
                case ReportDiagnostic.Info:
                    break;
                default:
                    // don't analyze if it's any other value.
                    return;
            }

            analyze(context, optionValue);
        }

        public abstract class DiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
        {
            public readonly TCodeStyleProvider _codeStyleProvider;

            protected DiagnosticAnalyzer(bool configurable = true) 
                : this(new TCodeStyleProvider(), configurable)
            {
            }

            private DiagnosticAnalyzer(TCodeStyleProvider codeStyleProvider, bool configurable)
                : base(codeStyleProvider._descriptorId, codeStyleProvider._title, codeStyleProvider._message, configurable)
            {
                _codeStyleProvider = codeStyleProvider;
            }

            protected sealed override void InitializeWorker(AnalysisContext context)
                => _codeStyleProvider.DiagnosticAnalyzerInitialize(context);

            public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
                => _codeStyleProvider.GetDiagnosticAnalyzerCategory();

            public sealed override bool OpenFileOnly(Workspace workspace)
                => _codeStyleProvider.DiagnosticsForOpenFileOnly(workspace);
        }
    }
}
