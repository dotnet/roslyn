// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class DirectlyAwaitingTaskAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        internal const string NameForExportAttribute = "DirectlyAwaitingTaskAnalyzer";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DirectlyAwaitingTaskAnalyzerRule.Rule); }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    if (context.Compilation.AssemblyName != null &&
                        (context.Compilation.AssemblyName.Contains("FxCopAnalyzer") || context.Compilation.AssemblyName.Contains("FxCopDiagnosticFixers")))
                    {
                        return;
                    }

                    var taskTypes = new Lazy<ImmutableArray<INamedTypeSymbol>>(() => GetTaskTypes(context.Compilation));

                    context.RegisterCodeBlockStartAction<TLanguageKindEnum>(new CodeBlockAnalyzer(this, taskTypes).Initialize);
                });
        }

        private static ImmutableArray<INamedTypeSymbol> GetTaskTypes(Compilation compilation)
        {
            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            return ImmutableArray.Create(taskType, taskOfTType);
        }

        protected abstract SyntaxNode GetAwaitedExpression(SyntaxNode awaitNode);
        protected abstract TLanguageKindEnum AwaitSyntaxKind { get; }

        private sealed class CodeBlockAnalyzer
        {
            private readonly DirectlyAwaitingTaskAnalyzer<TLanguageKindEnum> _analyzer;
            private readonly Lazy<ImmutableArray<INamedTypeSymbol>> _taskTypes;

            public CodeBlockAnalyzer(DirectlyAwaitingTaskAnalyzer<TLanguageKindEnum> analyzer, Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes)
            {
                _analyzer = analyzer;
                _taskTypes = taskTypes;
            }

            public void Initialize(CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
            {
                context.RegisterSyntaxNodeAction(new SyntaxNodeAnalyzer(_analyzer, _taskTypes).AnalyzeNode, _analyzer.AwaitSyntaxKind);
            }
        }

        private sealed class SyntaxNodeAnalyzer
        {
            private readonly DirectlyAwaitingTaskAnalyzer<TLanguageKindEnum> _analyzer;
            private readonly Lazy<ImmutableArray<INamedTypeSymbol>> _taskTypes;

            public SyntaxNodeAnalyzer(DirectlyAwaitingTaskAnalyzer<TLanguageKindEnum> analyzer, Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes)
            {
                _analyzer = analyzer;
                _taskTypes = taskTypes;
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var expression = _analyzer.GetAwaitedExpression(context.Node);
                var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;

                if (type != null && _taskTypes.Value.Contains(type.OriginalDefinition))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DirectlyAwaitingTaskAnalyzerRule.Rule, expression.GetLocation()));
                }
            }
        }
    }
}
