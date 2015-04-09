// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

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
        private static LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(FxCopRulesResources.DisposableFieldsShouldBeDisposed), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableMessageAndTitle,
                                                                         s_localizableMessageAndTitle,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182328.aspx",
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
            private INamedTypeSymbol _disposableType;
            private ConcurrentDictionary<IFieldSymbol, bool> _fieldDisposedMap = new ConcurrentDictionary<IFieldSymbol, bool>();

            public AbstractAnalyzer(INamedTypeSymbol disposableType)
            {
                _disposableType = disposableType;
            }

            public void AnalyzeCompilation(CompilationAnalysisContext context)
            {
                foreach (var item in _fieldDisposedMap)
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
                if (fieldSymbol.Type.Inherits(_disposableType))
                {
                    // Note that we found a disposable field declaration and it has not yet been disposed
                    // If a call to dispose this field is found in a method body, that will be noted by the language specific INodeInCodeBodyAnalyzer
                    _fieldDisposedMap.AddOrUpdate(fieldSymbol, false, (s, alreadyDisposed) => alreadyDisposed);
                }
            }

            protected void NoteFieldDisposed(IFieldSymbol fieldSymbol)
            {
                _fieldDisposedMap.AddOrUpdate(fieldSymbol, true, (s, alreadyDisposed) => true);
            }
        }
    }
}
