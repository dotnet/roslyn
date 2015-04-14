// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    internal abstract partial class UnusedDeclarationsAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        internal const string Category = "Maintainability";

        private static readonly LocalizableString s_title = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UnusedDeclarationsTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        private static readonly LocalizableString s_messageFormat = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UnusedDeclarationsMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(RoslynDiagnosticIds.DeadCodeRuleId, s_title, s_messageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: false);

        internal static readonly DiagnosticDescriptor s_triggerRule = new DiagnosticDescriptor(RoslynDiagnosticIds.DeadCodeTriggerRuleId, title: "", messageFormat: "", category: "", defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: false,
                                                                                               customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(s_rule, s_triggerRule); }
        }

        protected abstract TLanguageKindEnum IdentifierSyntaxKind { get; }
        protected abstract TLanguageKindEnum LocalDeclarationStatementSyntaxKind { get; }

        protected abstract IEnumerable<SyntaxNode> GetLocalDeclarationNodes(SyntaxNode node, CancellationToken cancellationToken);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(c =>
            {
                var tracker = new UnusedDeclarationsTracker(this);

                c.RegisterCompilationEndAction(tracker.OnCompilationEnd);
                c.RegisterSyntaxNodeAction<TLanguageKindEnum>(tracker.OnIdentifier, IdentifierSyntaxKind);
                c.RegisterSyntaxNodeAction<TLanguageKindEnum>(tracker.OnLocalDeclaration, LocalDeclarationStatementSyntaxKind);
                c.RegisterSymbolAction(
                    tracker.OnSymbol,
                    SymbolKind.NamedType,
                    SymbolKind.Method,
                    SymbolKind.Property,
                    SymbolKind.Event,
                    SymbolKind.Field);
            });
        }

        private class UnusedDeclarationsTracker
        {
            private readonly ConcurrentDictionary<ISymbol, bool> _used = new ConcurrentDictionary<ISymbol, bool>(concurrencyLevel: 2, capacity: 100);
            private readonly UnusedDeclarationsAnalyzer<TLanguageKindEnum> _owner;

            public UnusedDeclarationsTracker(UnusedDeclarationsAnalyzer<TLanguageKindEnum> owner)
            {
                _owner = owner;
            }

            public void OnIdentifier(SyntaxNodeAnalysisContext context)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var info = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken);
                if (info.Symbol?.Kind == SymbolKind.Namespace)
                {
                    // Avoid getting Locations for namespaces. That can be very expensive.
                    return;
                }

                var hasLocations = info.Symbol?.OriginalDefinition?.Locations.Length > 0;
                if (!hasLocations)
                {
                    return;
                }

                var inSource = info.Symbol?.OriginalDefinition?.Locations[0].IsInSource == true;
                if (!inSource || AccessibleFromOutside(info.Symbol.OriginalDefinition))
                {
                    return;
                }

                _used.AddOrUpdate(info.Symbol.OriginalDefinition, true, (k, v) => true);
            }

            public void OnLocalDeclaration(SyntaxNodeAnalysisContext context)
            {
                foreach (var node in _owner.GetLocalDeclarationNodes(context.Node, context.CancellationToken))
                {
                    var local = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                    if (local == null)
                    {
                        continue;
                    }

                    _used.TryAdd(local, false);
                }
            }

            public void OnSymbol(SymbolAnalysisContext context)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                var symbol = context.Symbol;

                if (!AccessibleFromOutside(symbol))
                {
                    _used.TryAdd(symbol, false);
                }

                var type = symbol as INamedTypeSymbol;
                if (type != null)
                {
                    AddSymbolDeclarations(type.TypeParameters);
                }

                var method = symbol as IMethodSymbol;
                if (method != null)
                {
                    AddParameters(method.DeclaredAccessibility, method.TypeParameters);
                    AddParameters(method.DeclaredAccessibility, method.Parameters);
                }

                var property = symbol as IPropertySymbol;
                if (property != null)
                {
                    AddParameters(property.DeclaredAccessibility, property.Parameters);

                    if (!AccessibleFromOutside(property.GetMethod))
                    {
                        _used.TryAdd(property.GetMethod, false);
                    }

                    if (!AccessibleFromOutside(property.SetMethod))
                    {
                        _used.TryAdd(property.SetMethod, false);

                        AddParameters(property.SetMethod.DeclaredAccessibility, property.SetMethod.Parameters);
                    }
                }
            }

            private void AddParameters(Accessibility accessibility, IEnumerable<ISymbol> parameters)
            {
                // only add parameters if accessibility is explicitly set to private.
                if (accessibility != Accessibility.Private)
                {
                    return;
                }

                AddSymbolDeclarations(parameters);
            }

            private void AddSymbolDeclarations(IEnumerable<ISymbol> symbols)
            {
                if (symbols == null)
                {
                    return;
                }

                foreach (var symbol in symbols)
                {
                    _used.TryAdd(symbol, false);
                }
            }

            public void OnCompilationEnd(CompilationAnalysisContext context)
            {
                foreach (var kv in _used.Where(kv => !kv.Value && (kv.Key.Locations.FirstOrDefault()?.IsInSource == true)))
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    var symbol = kv.Key;

                    // report visible error only if symbol is not local symbol
                    if (!(symbol is ILocalSymbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, symbol.Locations[0], symbol.Locations.Skip(1), symbol.Name));
                    }

                    // where code fix works
                    foreach (var reference in symbol.DeclaringSyntaxReferences)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        context.ReportDiagnostic(Diagnostic.Create(s_triggerRule, Location.Create(reference.SyntaxTree, reference.Span)));
                    }
                }
            }

            private static bool AccessibleFromOutside(ISymbol symbol)
            {
                if (symbol == null ||
                    symbol.Kind == SymbolKind.Namespace)
                {
                    return true;
                }

                if (symbol.DeclaredAccessibility == Accessibility.Private ||
                    symbol.DeclaredAccessibility == Accessibility.NotApplicable)
                {
                    return false;
                }

                if (symbol.ContainingSymbol == null)
                {
                    return true;
                }

                return AccessibleFromOutside(symbol.ContainingSymbol.OriginalDefinition);
            }
        }
    }
}
