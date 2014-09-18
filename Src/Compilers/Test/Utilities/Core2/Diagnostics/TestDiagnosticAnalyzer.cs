// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract class TestDiagnosticAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
    {
        protected static readonly ImmutableArray<SymbolKind> AllSymbolKinds = GetAllEnumValues<SymbolKind>();

        protected static readonly ImmutableArray<TSyntaxKind> AllSyntaxKinds = GetAllEnumValues<TSyntaxKind>();

        protected static readonly ImmutableArray<string> AllAnalyzerMemberNames = new string[] { "AnalyzeCodeBlock", "AnalyzeCompilation", "AnalyzeNode", "AnalyzeSemanticModel", "AnalyzeSymbol", "AnalyzeSyntaxTree", "Initialize", "SupportedDiagnostics" }.ToImmutableArray();
        // protected static readonly ImmutableArray<string> AllAbstractMemberNames = ImmutableArray<string>.Empty.AddRange(GetAbstractMemberNames(typeof(CompilationStartAnalysisScope)).Distinct());

        protected static readonly DiagnosticDescriptor DefaultDiagnostic =
            new DiagnosticDescriptor("CA7777", "CA7777_AnalyzerTestDiagnostic", "I'm here for test purposes", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static ImmutableArray<T> GetAllEnumValues<T>()
        {
            return ImmutableArray<T>.Empty.AddRange(typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => (T)f.GetRawConstantValue()));
        }

        protected static IEnumerable<string> GetAbstractMemberNames(Type abstractType)
        {
            return abstractType.GetMembers().Where(m => !(m is MethodInfo) || !((MethodInfo)m).IsSpecialName).Select(m => m.Name);
        }

        protected abstract void OnAbstractMember(string abstractMemberName, SyntaxNode node = null, ISymbol symbol = null, [CallerMemberName]string callerName = null);
        protected virtual void OnOptions(AnalyzerOptions options, [CallerMemberName]string callerName=null) { }

        #region Implementation

        public override void Initialize(AnalysisContext context)
        {
            OnAbstractMember("Initialize");

            context.RegisterCodeBlockStartAction<TSyntaxKind>(new NestedCodeBlockAnalyzer(this).Initialize);

            context.RegisterCompilationEndAction(this.AnalyzeCompilation);
            context.RegisterSemanticModelAction(this.AnalyzeSemanticModel);
            context.RegisterCodeBlockEndAction<TSyntaxKind>(this.AnalyzeCodeBlock);
            context.RegisterSymbolAction(this.AnalyzeSymbol, AllSymbolKinds.ToArray());
            context.RegisterSyntaxTreeAction(this.AnalyzeSyntaxTree);
            context.RegisterSyntaxNodeAction<TSyntaxKind>(this.AnalyzeNode, AllSyntaxKinds.ToArray());
        }

        void AnalyzeCodeBlock(CodeBlockEndAnalysisContext context)
        {
            OnAbstractMember("CodeBlock", context.CodeBlock, context.OwningSymbol);
            OnOptions(context.Options);
        }

        void AnalyzeCompilation(CompilationEndAnalysisContext context)
        {
            OnAbstractMember("Compilation");
            OnOptions(context.Options);
        }

        void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            OnAbstractMember("SemanticModel");
            OnOptions(context.Options);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                OnAbstractMember("SupportedDiagnostics");
                return ImmutableArray.Create(DefaultDiagnostic);
            }
        }

        void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            OnAbstractMember("Symbol", symbol: context.Symbol);
            OnOptions(context.Options);
        }

        void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            OnAbstractMember("SyntaxTree");
            OnOptions(context.Options);
        }

        void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            OnAbstractMember("SyntaxNode", context.Node);
            OnOptions(context.Options);
        }

        private class NestedCodeBlockAnalyzer
        {
            private TestDiagnosticAnalyzer<TSyntaxKind> container;

            public NestedCodeBlockAnalyzer(TestDiagnosticAnalyzer<TSyntaxKind> container)
            {
                this.container = container;
            }

            public void Initialize(CodeBlockStartAnalysisContext<TSyntaxKind> context)
            {
                this.container.OnAbstractMember("CodeBlockStart", context.CodeBlock, context.OwningSymbol);
                context.RegisterCodeBlockEndAction(this.container.AnalyzeCodeBlock);
                context.RegisterSyntaxNodeAction(this.container.AnalyzeNode, TestDiagnosticAnalyzer<TSyntaxKind>.AllSyntaxKinds.ToArray());
            }
        }

        #endregion
    }
}
