// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.FixAnalyzers
{
    /// <summary>
    /// A <see cref="CodeFixProvider"/> that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer.
    /// This enables the <see cref="FixAllProvider"/> to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.
    /// This analyzer catches violations of this requirement in the code actions registered by a <see cref="CodeFixProvider"/> that supports <see cref="FixAllProvider"/>.
    /// </summary>
    public abstract class FixerWithFixAllAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer
        where TLanguageKindEnum : struct
    {
        private static readonly string s_codeFixProviderMetadataName = "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider";
        private static readonly string s_codeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        private const string GetFixAllProviderMethodName = "GetFixAllProvider";
        private const string CreateMethodName = "Create";
        private const string EquivalenceKeyPropertyName = "EquivalenceKey";
        private const string EquivalenceKeyParameterName = "equivalenceKey";

        private static readonly LocalizableString s_localizableCreateCodeActionWithEquivalenceKeyTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.CreateCodeActionWithEquivalenceKeyTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableCreateCodeActionWithEquivalenceKeyMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.CreateCodeActionWithEquivalenceKeyMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableOverrideCodeActionEquivalenceKeyTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.OverrideCodeActionEquivalenceKeyTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableOverrideCodeActionEquivalenceKeyMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.OverrideCodeActionEquivalenceKeyMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableCodeActionNeedsEquivalenceKeyDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.CodeActionNeedsEquivalenceKeyDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        internal static readonly DiagnosticDescriptor CreateCodeActionEquivalenceKeyRule = new DiagnosticDescriptor(
            DiagnosticIds.CreateCodeActionWithEquivalenceKeyRuleId,
            s_localizableCreateCodeActionWithEquivalenceKeyTitle,
            s_localizableCreateCodeActionWithEquivalenceKeyMessage,
            "Correctness",
            DiagnosticSeverity.Warning,
            description: s_localizableCodeActionNeedsEquivalenceKeyDescription,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor OverrideCodeActionEquivalenceKeyRule = new DiagnosticDescriptor(
            DiagnosticIds.OverrideCodeActionEquivalenceKeyRuleId,
            s_localizableOverrideCodeActionEquivalenceKeyTitle,
            s_localizableOverrideCodeActionEquivalenceKeyMessage,
            "Correctness",
            DiagnosticSeverity.Warning,
            description: s_localizableCodeActionNeedsEquivalenceKeyDescription,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(CreateCodeActionEquivalenceKeyRule, OverrideCodeActionEquivalenceKeyRule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);
        }

        private void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            INamedTypeSymbol codeFixProviderSymbol = context.Compilation.GetTypeByMetadataName(s_codeFixProviderMetadataName);
            if (codeFixProviderSymbol == null)
            {
                return;
            }

            IMethodSymbol getFixAllProviderMethod = codeFixProviderSymbol.GetMembers(GetFixAllProviderMethodName).OfType<IMethodSymbol>().SingleOrDefault();
            if (getFixAllProviderMethod == null)
            {
                return;
            }

            INamedTypeSymbol codeActionSymbol = context.Compilation.GetTypeByMetadataName(s_codeActionMetadataName);
            if (codeActionSymbol == null)
            {
                return;
            }

            IEnumerable<IMethodSymbol> createSymbols = codeActionSymbol.GetMembers(CreateMethodName).OfType<IMethodSymbol>();
            if (createSymbols == null)
            {
                return;
            }

            IPropertySymbol equivalenceKeyProperty = codeActionSymbol.GetMembers(EquivalenceKeyPropertyName).OfType<IPropertySymbol>().SingleOrDefault();
            if (equivalenceKeyProperty == null)
            {
                return;
            }

            CompilationAnalyzer compilationAnalyzer = GetCompilationAnalyzer(codeFixProviderSymbol, getFixAllProviderMethod,
                codeActionSymbol, ImmutableHashSet.CreateRange(createSymbols), equivalenceKeyProperty);

            context.RegisterSymbolAction(compilationAnalyzer.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            context.RegisterCodeBlockStartAction<TLanguageKindEnum>(compilationAnalyzer.CodeBlockStart);
            context.RegisterCompilationEndAction(compilationAnalyzer.CompilationEnd);
        }

        protected abstract CompilationAnalyzer GetCompilationAnalyzer(
            INamedTypeSymbol codeFixProviderSymbol,
            IMethodSymbol getFixAllProvider,
            INamedTypeSymbol codeActionSymbol,
            ImmutableHashSet<IMethodSymbol> createMethods,
            IPropertySymbol equivalenceKeyProperty);

        protected abstract class CompilationAnalyzer
        {
            private readonly INamedTypeSymbol _codeFixProviderSymbol;
            private readonly IMethodSymbol _getFixAllProvider;

            private readonly INamedTypeSymbol _codeActionSymbol;
            private readonly ImmutableHashSet<IMethodSymbol> _createMethods;
            private readonly IPropertySymbol _equivalenceKeyProperty;

            /// <summary>
            /// Set of all non-abstract sub-types of <see cref="CodeFixProvider"/> in this compilation.
            /// </summary>
            private HashSet<INamedTypeSymbol> _codeFixProviders;

            /// <summary>
            /// Set of all non-abstract sub-types of <see cref="CodeAction"/> which override <see cref="CodeAction.EquivalenceKey"/> in this compilation.
            /// </summary>
            private HashSet<INamedTypeSymbol> _codeActionsWithEquivalenceKey;

            /// <summary>
            /// Map of invocations from code fix providers to invocation nodes (and symbols) that create a code action using the static "Create" methods on <see cref="CodeAction"/>.
            /// </summary>
            private Dictionary<INamedTypeSymbol, HashSet<NodeAndSymbol>> _codeActionCreateInvocations;

            /// <summary>
            /// Map of invocations from code fix providers to object creation nodes (and symbols) that create a code action using sub-types of <see cref="CodeAction"/>.
            /// </summary>
            private Dictionary<INamedTypeSymbol, HashSet<NodeAndSymbol>> _codeActionObjectCreations;

            private struct NodeAndSymbol
            {
                public SyntaxNode Node { get; set; }
                public IMethodSymbol Symbol { get; set; }
            }

            protected CompilationAnalyzer(
                INamedTypeSymbol codeFixProviderSymbol,
                IMethodSymbol getFixAllProvider,
                INamedTypeSymbol codeActionSymbol,
                ImmutableHashSet<IMethodSymbol> createMethods,
                IPropertySymbol equivalenceKeyProperty)
            {
                _codeFixProviderSymbol = codeFixProviderSymbol;
                _getFixAllProvider = getFixAllProvider;
                _codeActionSymbol = codeActionSymbol;
                _createMethods = createMethods;
                _equivalenceKeyProperty = equivalenceKeyProperty;

                _codeFixProviders = null;
                _codeActionsWithEquivalenceKey = null;
                _codeActionCreateInvocations = null;
                _codeActionObjectCreations = null;
            }

            internal void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
            {
                var namedType = (INamedTypeSymbol)context.Symbol;

                if (namedType.IsAbstract)
                {
                    return;
                }

                if (namedType.DerivesFrom(_codeFixProviderSymbol))
                {
                    _codeFixProviders = _codeFixProviders ?? new HashSet<INamedTypeSymbol>();
                    _codeFixProviders.Add(namedType);
                }
                else if (namedType.DerivesFrom(_codeActionSymbol))
                {
                    IPropertySymbol equivalenceKeyProperty = namedType.GetMembers(EquivalenceKeyPropertyName).OfType<IPropertySymbol>().SingleOrDefault();
                    if (equivalenceKeyProperty != null && equivalenceKeyProperty.IsOverride)
                    {
                        _codeActionsWithEquivalenceKey = _codeActionsWithEquivalenceKey ?? new HashSet<INamedTypeSymbol>();
                        _codeActionsWithEquivalenceKey.Add(namedType);
                    }
                }
            }

            protected abstract TLanguageKindEnum GetInvocationKind { get; }
            protected abstract TLanguageKindEnum GetObjectCreationKind { get; }
            protected abstract bool HasNonNullArgumentForParameter(SyntaxNode invocation, IParameterSymbol parameter, int indexOfParameter, SemanticModel semanticModel, CancellationToken cancellationToken);

            protected bool HasNullConstantValue(SyntaxNode expression, SemanticModel model, CancellationToken cancellationToken)
            {
                if (expression == null)
                {
                    return false;
                }

                Optional<object> constantValue = model.GetConstantValue(expression, cancellationToken);
                return constantValue.HasValue && constantValue.Value == null;
            }

            internal void CodeBlockStart(CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
            {
                var method = context.OwningSymbol as IMethodSymbol;
                if (method == null)
                {
                    return;
                }

                INamedTypeSymbol namedType = method.ContainingType;
                if (!namedType.DerivesFrom(_codeFixProviderSymbol))
                {
                    return;
                }

                context.RegisterSyntaxNodeAction(invocationContext =>
                {
                    var invocationSym = invocationContext.SemanticModel.GetSymbolInfo(invocationContext.Node).Symbol as IMethodSymbol;
                    if (invocationSym != null && _createMethods.Contains(invocationSym))
                    {
                        _codeActionCreateInvocations = _codeActionCreateInvocations ?? new Dictionary<INamedTypeSymbol, HashSet<NodeAndSymbol>>();
                        AddNodeAndSymbol(namedType, invocationContext.Node, invocationSym, _codeActionCreateInvocations);
                    }
                },
                GetInvocationKind);

                context.RegisterSyntaxNodeAction(objectCreationContext =>
                {
                    var constructor = objectCreationContext.SemanticModel.GetSymbolInfo(objectCreationContext.Node).Symbol as IMethodSymbol;
                    if (constructor != null && constructor.ContainingType.DerivesFrom(_codeActionSymbol))
                    {
                        _codeActionObjectCreations = _codeActionObjectCreations ?? new Dictionary<INamedTypeSymbol, HashSet<NodeAndSymbol>>();
                        AddNodeAndSymbol(namedType, objectCreationContext.Node, constructor, _codeActionObjectCreations);
                    }
                },
                GetObjectCreationKind);
            }

            private static void AddNodeAndSymbol(INamedTypeSymbol namedType, SyntaxNode node, IMethodSymbol symbol, Dictionary<INamedTypeSymbol, HashSet<NodeAndSymbol>> map)
            {
                HashSet<NodeAndSymbol> value;
                if (!map.TryGetValue(namedType, out value))
                {
                    value = new HashSet<NodeAndSymbol>();
                    map[namedType] = value;
                }

                value.Add(new NodeAndSymbol { Node = node, Symbol = symbol });
            }

            internal void CompilationEnd(CompilationAnalysisContext context)
            {
                if (_codeFixProviders == null)
                {
                    // No fixers.
                    return;
                }

                if (_codeActionCreateInvocations == null && _codeActionObjectCreations == null)
                {
                    // No registered fixes.
                    return;
                }

                // Analyze all fixers that have FixAll support.
                foreach (INamedTypeSymbol fixer in _codeFixProviders)
                {
                    if (OverridesGetFixAllProvider(fixer))
                    {
                        AnalyzeFixerWithFixAll(fixer, context);
                    }
                }
            }

            private bool OverridesGetFixAllProvider(INamedTypeSymbol fixer)
            {
                foreach (INamedTypeSymbol type in fixer.GetBaseTypesAndThis())
                {
                    if (!type.Equals(_codeFixProviderSymbol))
                    {
                        IMethodSymbol getFixAllProviderProperty = type.GetMembers(GetFixAllProviderMethodName).OfType<IMethodSymbol>().SingleOrDefault();
                        if (getFixAllProviderProperty != null && getFixAllProviderProperty.IsOverride)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private void AnalyzeFixerWithFixAll(INamedTypeSymbol fixer, CompilationAnalysisContext context)
            {
                if (_codeActionCreateInvocations != null)
                {
                    HashSet<NodeAndSymbol> nodeAndSymbolSet;
                    if (_codeActionCreateInvocations.TryGetValue(fixer, out nodeAndSymbolSet))
                    {
                        foreach (NodeAndSymbol nodeAndSymbol in nodeAndSymbolSet)
                        {
                            SemanticModel model = context.Compilation.GetSemanticModel(nodeAndSymbol.Node.SyntaxTree);
                            if (IsViolatingCodeActionCreateInvocation(nodeAndSymbol.Node, nodeAndSymbol.Symbol, model, context.CancellationToken))
                            {
                                Diagnostic diagnostic = Diagnostic.Create(CreateCodeActionEquivalenceKeyRule, nodeAndSymbol.Node.GetLocation(), EquivalenceKeyParameterName);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }

                if (_codeActionObjectCreations != null)
                {
                    HashSet<NodeAndSymbol> nodeAndSymbolSet;
                    if (_codeActionObjectCreations.TryGetValue(fixer, out nodeAndSymbolSet))
                    {
                        foreach (NodeAndSymbol nodeAndSymbol in nodeAndSymbolSet)
                        {
                            if (IsViolatingCodeActionObjectCreation(nodeAndSymbol.Node, nodeAndSymbol.Symbol))
                            {
                                Diagnostic diagnostic = Diagnostic.Create(OverrideCodeActionEquivalenceKeyRule, nodeAndSymbol.Node.GetLocation(), nodeAndSymbol.Symbol.ContainingType, EquivalenceKeyPropertyName);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }

            private bool IsViolatingCodeActionCreateInvocation(SyntaxNode invocation, IMethodSymbol invocationSym, SemanticModel model, CancellationToken cancellationToken)
            {
                IParameterSymbol param = invocationSym.Parameters.SingleOrDefault(p => p.Name == EquivalenceKeyParameterName);
                if (param == null)
                {
                    return true;
                }

                int index = invocationSym.Parameters.IndexOf(param);
                return !HasNonNullArgumentForParameter(invocation, param, index, model, cancellationToken);
            }

            private bool IsViolatingCodeActionObjectCreation(SyntaxNode objectCreation, IMethodSymbol constructor)
            {
                return _codeActionsWithEquivalenceKey == null ||
                    !constructor.ContainingType.GetBaseTypesAndThis().Any(_codeActionsWithEquivalenceKey.Contains);
            }
        }
    }
}
