// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class DirectlyAwaitingTaskAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
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
                    if (context.Compilation.AssemblyName.Contains("FxCopAnalyzer") ||
                        context.Compilation.AssemblyName.Contains("FxCopDiagnosticFixers"))
                    {
                        return;
                    }

                    var taskTypes = new Lazy<ImmutableArray<INamedTypeSymbol>>(() => GetTaskTypes(context.Compilation));

                    context.RegisterCodeBlockStartAction<TSyntaxKind>(new CodeBlockAnalyzer(this, taskTypes).Initialize);
                });
        }

        private static ImmutableArray<INamedTypeSymbol> GetTaskTypes(Compilation compilation)
        {
            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            return ImmutableArray.Create(taskType, taskOfTType);
        }

        protected abstract SyntaxNode GetAwaitedExpression(SyntaxNode awaitNode);
        protected abstract TSyntaxKind AwaitSyntaxKind { get; }

        private sealed class CodeBlockAnalyzer
        {
            private readonly DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer;
            private readonly Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes;

            public CodeBlockAnalyzer(DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer, Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes)
            {
                this.analyzer = analyzer;
                this.taskTypes = taskTypes;
            }

            public void Initialize(CodeBlockStartAnalysisContext<TSyntaxKind> context)
            {
                context.RegisterSyntaxNodeAction(new SyntaxNodeAnalyzer(analyzer, taskTypes).AnalyzeNode, analyzer.AwaitSyntaxKind);
            }
        }

        private sealed class SyntaxNodeAnalyzer
        {
            private readonly DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer;
            private readonly Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes;

            public SyntaxNodeAnalyzer(DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer, Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes)
            {
                this.analyzer = analyzer;
                this.taskTypes = taskTypes;
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var expression = analyzer.GetAwaitedExpression(context.Node);
                var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;

                if (type != null && taskTypes.Value.Contains(type.OriginalDefinition))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DirectlyAwaitingTaskAnalyzerRule.Rule, expression.GetLocation()));
                }
            }
        }
    }
}
