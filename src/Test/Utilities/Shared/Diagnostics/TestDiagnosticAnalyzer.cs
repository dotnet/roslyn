// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public abstract class TestDiagnosticAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        protected static readonly ImmutableArray<SymbolKind> AllSymbolKinds = GetAllEnumValues<SymbolKind>();

        protected static readonly ImmutableArray<TLanguageKindEnum> AllSyntaxKinds = GetAllEnumValues<TLanguageKindEnum>();

        protected static readonly ImmutableArray<string> AllAnalyzerMemberNames = new string[] { "AnalyzeCodeBlock", "AnalyzeCompilation", "AnalyzeNode", "AnalyzeSemanticModel", "AnalyzeSymbol", "AnalyzeSyntaxTree", "Initialize", "SupportedDiagnostics" }.ToImmutableArray();
        // protected static readonly ImmutableArray<string> AllAbstractMemberNames = ImmutableArray<string>.Empty.AddRange(GetAbstractMemberNames(typeof(CompilationStartAnalysisScope)).Distinct());

        protected static readonly DiagnosticDescriptor DefaultDiagnostic =
            new DiagnosticDescriptor("CA7777", "CA7777_AnalyzerTestDiagnostic", "I'm here for test purposes", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static ImmutableArray<T> GetAllEnumValues<T>()
        {
            return ImmutableArray.CreateRange(Enum.GetValues(typeof(T)).Cast<T>());
        }

        protected abstract void OnAbstractMember(string abstractMemberName, SyntaxNode node = null, ISymbol symbol = null, [CallerMemberName]string callerName = null);
        protected virtual void OnOptions(AnalyzerOptions options, [CallerMemberName]string callerName = null) { }

        #region Implementation

        public override void Initialize(AnalysisContext context)
        {
            OnAbstractMember("Initialize");

            context.RegisterCodeBlockStartAction<TLanguageKindEnum>(new NestedCodeBlockAnalyzer(this).Initialize);

            context.RegisterCompilationAction(this.AnalyzeCompilation);
            context.RegisterSemanticModelAction(this.AnalyzeSemanticModel);
            context.RegisterCodeBlockAction(this.AnalyzeCodeBlock);
            context.RegisterSymbolAction(this.AnalyzeSymbol, AllSymbolKinds.ToArray());
            context.RegisterSyntaxTreeAction(this.AnalyzeSyntaxTree);
            context.RegisterSyntaxNodeAction<TLanguageKindEnum>(this.AnalyzeNode, AllSyntaxKinds.ToArray());
        }

        private void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
        {
            OnAbstractMember("CodeBlock", context.CodeBlock, context.OwningSymbol);
            OnOptions(context.Options);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            OnAbstractMember("Compilation");
            OnOptions(context.Options);
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
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

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            OnAbstractMember("Symbol", symbol: context.Symbol);
            OnOptions(context.Options);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            OnAbstractMember("SyntaxTree");
            OnOptions(context.Options);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            OnAbstractMember("SyntaxNode", context.Node);
            OnOptions(context.Options);
        }

        private class NestedCodeBlockAnalyzer
        {
            private readonly TestDiagnosticAnalyzer<TLanguageKindEnum> _container;

            public NestedCodeBlockAnalyzer(TestDiagnosticAnalyzer<TLanguageKindEnum> container)
            {
                _container = container;
            }

            public void Initialize(CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
            {
                _container.OnAbstractMember("CodeBlockStart", context.CodeBlock, context.OwningSymbol);
                context.RegisterCodeBlockEndAction(_container.AnalyzeCodeBlock);
                context.RegisterSyntaxNodeAction(_container.AnalyzeNode, TestDiagnosticAnalyzer<TLanguageKindEnum>.AllSyntaxKinds.ToArray());
            }
        }

        #endregion
    }
}
