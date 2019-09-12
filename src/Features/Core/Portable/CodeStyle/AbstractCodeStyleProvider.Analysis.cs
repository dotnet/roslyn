// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    // This part contains all the logic for hooking up the DiagnosticAnalyzer to the CodeStyleProvider.
    // All the code in this part is an implementation detail and is intentionally private so that
    // subclasses cannot change anything.  All code relevant to subclasses relating to analysis
    // is contained in AbstractCodeStyleProvider.cs

    internal abstract partial class AbstractCodeStyleProvider<TOptionKind, TCodeStyleProvider>
    {
        public abstract class DiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        {
            public readonly TCodeStyleProvider _codeStyleProvider;

            protected DiagnosticAnalyzer(bool configurable = true)
                : this(new TCodeStyleProvider(), configurable)
            {
            }

            private DiagnosticAnalyzer(TCodeStyleProvider codeStyleProvider, bool configurable)
                : base(codeStyleProvider._descriptorId,
                       codeStyleProvider._option,
                       codeStyleProvider._language,
                       codeStyleProvider._title,
                       codeStyleProvider._message,
                       configurable)
            {
                _codeStyleProvider = codeStyleProvider;
            }

            protected sealed override void InitializeWorker(Diagnostics.AnalysisContext context)
                => _codeStyleProvider.DiagnosticAnalyzerInitialize(new AnalysisContext(_codeStyleProvider, context));

            public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
                => _codeStyleProvider.GetAnalyzerCategory();
        }

        /// <summary>
        /// Critically, we want to consolidate the logic about checking if the analyzer should run
        /// at all.  i.e. if the user has their option set to 'none' or 'refactoring only' then we
        /// do not want the analyzer to run at all.
        ///
        /// To that end, we don't let the subclass have direct access to the real <see
        /// cref="Diagnostics.AnalysisContext"/>. Instead, we pass this type to the subclass for it
        /// register with.  We then check if the registration should proceed given the <see
        /// cref="CodeStyleOption{T}"/>
        /// and the current <see cref="SyntaxTree"/> being processed.  If not, we don't do the
        /// actual registration.
        /// </summary>
        protected struct AnalysisContext
        {
            private readonly TCodeStyleProvider _codeStyleProvider;
            private readonly Diagnostics.AnalysisContext _context;

            public AnalysisContext(TCodeStyleProvider codeStyleProvider, Diagnostics.AnalysisContext context)
            {
                _codeStyleProvider = codeStyleProvider;
                _context = context;
            }

            public void RegisterCompilationStartAction(Action<Compilation, AnalysisContext> analyze)
            {
                var _this = this;
                _context.RegisterCompilationStartAction(
                    c => analyze(c.Compilation, _this));
            }

            public void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            {
                var provider = _codeStyleProvider;
                _context.RegisterCodeBlockAction(
                    c => AnalyzeIfEnabled(provider, c, analyze, c.Options, c.SemanticModel.SyntaxTree, c.CancellationToken));
            }

            public void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            {
                var provider = _codeStyleProvider;
                _context.RegisterSemanticModelAction(
                    c => AnalyzeIfEnabled(provider, c, analyze, c.Options, c.SemanticModel.SyntaxTree, c.CancellationToken));
            }

            public void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext, CodeStyleOption<TOptionKind>> analyze)
            {
                var provider = _codeStyleProvider;
                _context.RegisterSyntaxTreeAction(
                    c => AnalyzeIfEnabled(provider, c, analyze, c.Options, c.Tree, c.CancellationToken));
            }

            public void RegisterOperationAction(
                Action<OperationAnalysisContext, CodeStyleOption<TOptionKind>> analyze,
                params OperationKind[] operationKinds)
            {
                var provider = _codeStyleProvider;
                _context.RegisterOperationAction(
                    c => AnalyzeIfEnabled(provider, c, analyze, c.Options, c.Operation.SemanticModel.SyntaxTree, c.CancellationToken),
                    operationKinds);
            }

            public void RegisterSyntaxNodeAction<TSyntaxKind>(
                Action<SyntaxNodeAnalysisContext, CodeStyleOption<TOptionKind>> analyze,
                params TSyntaxKind[] syntaxKinds) where TSyntaxKind : struct
            {
                var provider = _codeStyleProvider;
                _context.RegisterSyntaxNodeAction(
                    c => AnalyzeIfEnabled(provider, c, analyze, c.Options, c.SemanticModel.SyntaxTree, c.CancellationToken),
                    syntaxKinds);
            }

            private static void AnalyzeIfEnabled<TContext>(
                TCodeStyleProvider provider, TContext context, Action<TContext, CodeStyleOption<TOptionKind>> analyze,
                AnalyzerOptions options, SyntaxTree syntaxTree, CancellationToken cancellationToken)
            {
                var optionSet = options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
                if (optionSet == null)
                {
                    return;
                }

                var optionValue = optionSet.GetOption(provider._option);
                var severity = GetOptionSeverity(optionValue);
                switch (severity)
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
        }
    }
}
