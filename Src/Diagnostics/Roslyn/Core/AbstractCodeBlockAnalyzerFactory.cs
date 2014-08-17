// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractCodeBlockAnalyzerFactory<TSyntaxKind> : ICodeBlockNestedAnalyzerFactory
    {
        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Descriptor);
            }
        }

        protected abstract DiagnosticDescriptor Descriptor { get; }
        protected abstract ExecutableNodeAnalyzer GetExecutableNodeAnalyzer();

        public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            return GetExecutableNodeAnalyzer();
        }

        protected abstract class ExecutableNodeAnalyzer : ISyntaxNodeAnalyzer<TSyntaxKind>
        {
            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Descriptor);
                }
            }

            protected abstract DiagnosticDescriptor Descriptor { get; }
            public abstract ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
            public abstract void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken);

            protected Diagnostic CreateDiagnostic(SyntaxNode node)
            {
                return Diagnostic.Create(Descriptor, node.GetLocation(), node.ToString());
            }
        }
    }
}
