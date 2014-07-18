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
    public abstract class DirectlyAwaitingTaskAnalyzer<TSyntaxKind> : IDiagnosticAnalyzer, ICompilationNestedAnalyzerFactory
    {
        internal const string NameForExportAttribute = "DirectlyAwaitingTaskAnalyzer";

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DirectlyAwaitingTaskAnalyzerRule.Rule); }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCompilation(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (compilation.AssemblyName.Contains("FxCopAnalyzer") ||
                compilation.AssemblyName.Contains("FxCopDiagnosticFixers"))
            {
                return null;
            }

            var taskTypes = new Lazy<ImmutableArray<INamedTypeSymbol>>(() => GetTaskTypes(compilation));

            return new CodeBlockAnalyzer(this, taskTypes);
        }

        private static ImmutableArray<INamedTypeSymbol> GetTaskTypes(Compilation compilation)
        {
            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            return ImmutableArray.Create(taskType, taskOfTType);
        }

        protected abstract SyntaxNode GetAwaitedExpression(SyntaxNode awaitNode);
        protected abstract TSyntaxKind AwaitSyntaxKind { get; }

        private sealed class CodeBlockAnalyzer : ICodeBlockNestedAnalyzerFactory
        {
            private readonly DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer;
            private readonly Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes;

            public CodeBlockAnalyzer(DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer, Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes)
            {
                this.analyzer = analyzer;
                this.taskTypes = taskTypes;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(DirectlyAwaitingTaskAnalyzerRule.Rule); }
            }

            public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                return new SyntaxNodeAnalyzer(analyzer, taskTypes);
            }
        }

        private sealed class SyntaxNodeAnalyzer : ISyntaxNodeAnalyzer<TSyntaxKind>
        {
            private readonly DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer;
            private readonly Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes;

            public SyntaxNodeAnalyzer(DirectlyAwaitingTaskAnalyzer<TSyntaxKind> analyzer, Lazy<ImmutableArray<INamedTypeSymbol>> taskTypes)
            {
                this.analyzer = analyzer;
                this.taskTypes = taskTypes;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(DirectlyAwaitingTaskAnalyzerRule.Rule);
                }
            }

            public ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(analyzer.AwaitSyntaxKind);
                }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var expression = analyzer.GetAwaitedExpression(node);
                var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;

                if (type != null && taskTypes.Value.Contains(type.OriginalDefinition))
                {
                    addDiagnostic(Diagnostic.Create(DirectlyAwaitingTaskAnalyzerRule.Rule, expression.GetLocation()));
                }
            }
        }
    }
}
