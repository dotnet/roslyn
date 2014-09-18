// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractCodeBlockAnalyzerFactory<TSyntaxKind> : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Descriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCodeBlockStartAction<TSyntaxKind>(CreateAnalyzerWithinCodeBlock);
        }

        protected abstract DiagnosticDescriptor Descriptor { get; }
        protected abstract ExecutableNodeAnalyzer GetExecutableNodeAnalyzer();

        private void CreateAnalyzerWithinCodeBlock(CodeBlockStartAnalysisContext<TSyntaxKind> context)
        {
            var analyzer = GetExecutableNodeAnalyzer();
            context.RegisterSyntaxNodeAction(analyzer.AnalyzeNode, analyzer.SyntaxKindsOfInterest.ToArray());
        }

        protected abstract class ExecutableNodeAnalyzer
        {
            protected abstract DiagnosticDescriptor Descriptor { get; }
            public abstract ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
            public abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

            protected Diagnostic CreateDiagnostic(SyntaxNode node)
            {
                return Diagnostic.Create(Descriptor, node.GetLocation(), node.ToString());
            }
        }
    }
}
