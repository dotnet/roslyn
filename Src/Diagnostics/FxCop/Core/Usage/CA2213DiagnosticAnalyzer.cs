// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// 
    /// TODO: this needs to be re-implemented because it requires flow analysis to determine if the
    /// dispose operations occur on every path through the dispose method. Flow analysis
    /// is not yet implemented.
    /// </summary>
    public abstract class CA2213DiagnosticAnalyzer : ICompilationNestedAnalyzerFactory
    {
        internal const string RuleId = "CA2213";
        internal const string Dispose = "Dispose";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.DisposableFieldsShouldBeDisposed,
                                                                         FxCopRulesResources.DisposableFieldsShouldBeDisposed,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }
        
        protected abstract AbstractAnalyzer GetAnalyzer(INamedTypeSymbol disposableType);

        public IDiagnosticAnalyzer CreateAnalyzerWithinCompilation(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var disposableType = compilation.GetSpecialType(SpecialType.System_IDisposable);
            return disposableType != null ? GetAnalyzer(disposableType) : null;
        }

        protected abstract class AbstractAnalyzer : ICompilationAnalyzer, ISymbolAnalyzer
        {
            private INamedTypeSymbol disposableType;
            private ConcurrentDictionary<IFieldSymbol, bool> fieldDisposedMap = new ConcurrentDictionary<IFieldSymbol, bool>();

            public AbstractAnalyzer(INamedTypeSymbol disposableType)
            {
                this.disposableType = disposableType;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public void AnalyzeCompilation(Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                foreach (var item in this.fieldDisposedMap)
                {
                    if (!item.Value)
                    {
                        addDiagnostic(item.Key.CreateDiagnostic(Rule));
                    }
                }
            }

            public ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(SymbolKind.Field);
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                Debug.Assert(symbol.Kind == SymbolKind.Field);
                var fieldSymbol = (IFieldSymbol)symbol;
                if (fieldSymbol.Type.Inherits(this.disposableType))
                {
                    // Note that we found a disposable field declaration and it has not yet been disposed
                    // If a call to dispose this field is found in a method body, that will be noted by the language specific INodeInCodeBodyAnalyzer
                    this.fieldDisposedMap.AddOrUpdate(fieldSymbol, false, (s, alreadyDisposed) => alreadyDisposed);
                }
            }

            protected void NoteFieldDisposed(IFieldSymbol fieldSymbol)
            {
                this.fieldDisposedMap.AddOrUpdate(fieldSymbol, true, (s, alreadyDisposed) => true);
            }
        }
    }
}
