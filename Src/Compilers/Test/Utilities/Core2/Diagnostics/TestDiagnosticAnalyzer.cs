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
    public abstract class TestDiagnosticAnalyzer<TSyntaxKind> :
        ICodeBlockStartedAnalyzer,
        ICodeBlockEndedAnalyzer,
        ICompilationStartedAnalyzer,
        ICompilationEndedAnalyzer,
        ISemanticModelAnalyzer,
        IDiagnosticAnalyzer,
        ISymbolAnalyzer,
        ISyntaxTreeAnalyzer,
        ISyntaxNodeAnalyzer<TSyntaxKind>
    {
        protected static readonly ImmutableArray<SymbolKind> AllSymbolKinds = GetAllEnumValues<SymbolKind>();

        protected static readonly ImmutableArray<TSyntaxKind> AllSyntaxKinds = GetAllEnumValues<TSyntaxKind>();

        protected static readonly ImmutableArray<string> AllInterfaceMemberNames = ImmutableArray<string>.Empty.AddRange(typeof(TestDiagnosticAnalyzer<TSyntaxKind>).GetInterfaces().SelectMany(i => GetInterfaceMemberNames(i)));

        protected static readonly DiagnosticDescriptor DefaultDiagnostic =
            new DiagnosticDescriptor("CA7777", "CA7777_AnalyzerTestDiagnostic", "I'm here for test purposes", "Test", DiagnosticSeverity.Warning);

        private static ImmutableArray<T> GetAllEnumValues<T>()
        {
            return ImmutableArray<T>.Empty.AddRange(typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => (T)f.GetRawConstantValue()));
        }

        protected static IEnumerable<string> GetInterfaceMemberNames(Type interfaceType)
        {
            return interfaceType.GetMembers().Where(m => !(m is MethodInfo) || !((MethodInfo)m).IsSpecialName).Select(m => m.Name);
        }

        protected abstract void OnInterfaceMember(SyntaxNode node = null, ISymbol symbol = null, [CallerMemberName]string callerName = null);

        #region Implementation

        ICodeBlockEndedAnalyzer ICodeBlockStartedAnalyzer.OnCodeBlockStarted(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember(codeBlock, ownerSymbol);
            return this;
        }

        void ICodeBlockEndedAnalyzer.OnCodeBlockEnded(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember(codeBlock, ownerSymbol);
        }

        ICompilationEndedAnalyzer ICompilationStartedAnalyzer.OnCompilationStarted(Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember();
            return this;
        }

        void ICompilationEndedAnalyzer.OnCompilationEnded(Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember();
        }

        void ISemanticModelAnalyzer.AnalyzeSemanticModel(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember();
        }

        ImmutableArray<DiagnosticDescriptor> IDiagnosticAnalyzer.SupportedDiagnostics
        {
            get
            {
                OnInterfaceMember();
                return ImmutableArray.Create(DefaultDiagnostic);
            }
        }

        ImmutableArray<SymbolKind> ISymbolAnalyzer.SymbolKindsOfInterest
        {
            get
            {
                OnInterfaceMember();
                return AllSymbolKinds;
            }
        }

        void ISymbolAnalyzer.AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember(symbol: symbol);
        }

        void ISyntaxTreeAnalyzer.AnalyzeSyntaxTree(SyntaxTree tree, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember();
        }

        ImmutableArray<TSyntaxKind> ISyntaxNodeAnalyzer<TSyntaxKind>.SyntaxKindsOfInterest
        {
            get
            {
                OnInterfaceMember();
                return AllSyntaxKinds;
            }
        }

        void ISyntaxNodeAnalyzer<TSyntaxKind>.AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            OnInterfaceMember(node);
        }

        #endregion
    }
}
