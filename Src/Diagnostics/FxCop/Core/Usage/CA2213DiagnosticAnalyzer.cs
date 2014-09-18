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
    public abstract class CA2213DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2213";
        internal const string Dispose = "Dispose";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.DisposableFieldsShouldBeDisposed,
                                                                         FxCopRulesResources.DisposableFieldsShouldBeDisposed,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182328.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }
        
        protected abstract AbstractAnalyzer GetAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol disposableType);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(
                (context) =>
                {
                    var disposableType = context.Compilation.GetSpecialType(SpecialType.System_IDisposable);
                    if (disposableType != null)
                    {
                        AbstractAnalyzer analyzer = GetAnalyzer(context, disposableType);
                        context.RegisterCompilationEndAction(analyzer.AnalyzeCompilation);
                        context.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.Field);
                    }
                });
        }

        protected abstract class AbstractAnalyzer
        {
            private INamedTypeSymbol disposableType;
            private ConcurrentDictionary<IFieldSymbol, bool> fieldDisposedMap = new ConcurrentDictionary<IFieldSymbol, bool>();

            public AbstractAnalyzer(INamedTypeSymbol disposableType)
            {
                this.disposableType = disposableType;
            }

            public void AnalyzeCompilation(CompilationEndAnalysisContext context)
            {
                foreach (var item in this.fieldDisposedMap)
                {
                    if (!item.Value)
                    {
                        context.ReportDiagnostic(item.Key.CreateDiagnostic(Rule));
                    }
                }
            }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                Debug.Assert(context.Symbol.Kind == SymbolKind.Field);
                var fieldSymbol = (IFieldSymbol)context.Symbol;
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
