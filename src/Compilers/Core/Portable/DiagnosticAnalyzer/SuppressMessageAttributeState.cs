// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class SuppressMessageAttributeState
    {
        private static readonly SmallDictionary<string, TargetScope> s_suppressMessageScopeTypes = new SmallDictionary<string, TargetScope>()
            {
                { null, TargetScope.None },
                { "module", TargetScope.Module },
                { "namespace", TargetScope.Namespace },
                { "resource", TargetScope.Resource },
                { "type", TargetScope.Type },
                { "member", TargetScope.Member }
            };

        private readonly Compilation _compilation;
        private GlobalSuppressions _lazyGlobalSuppressions;
        private readonly ConcurrentDictionary<ISymbol, ImmutableArray<string>> _localSuppressionsBySymbol = new ConcurrentDictionary<ISymbol, ImmutableArray<string>>();
        private ISymbol _lazySuppressMessageAttribute;

        private class GlobalSuppressions
        {
            private readonly HashSet<string> _compilationWideSuppressions = new HashSet<string>();
            private readonly Dictionary<ISymbol, ImmutableArray<string>> _globalSymbolSuppressions = new Dictionary<ISymbol, ImmutableArray<string>>();

            public void AddCompilationWideSuppression(string id)
            {
                _compilationWideSuppressions.Add(id);
            }

            public void AddGlobalSymbolSuppression(ISymbol symbol, string id)
            {
                ImmutableArray<string> suppressions;
                if (_globalSymbolSuppressions.TryGetValue(symbol, out suppressions))
                {
                    if (!suppressions.Contains(id))
                    {
                        _globalSymbolSuppressions[symbol] = suppressions.Add(id);
                    }
                }
                else
                {
                    _globalSymbolSuppressions.Add(symbol, ImmutableArray.Create(id));
                }
            }

            public bool HasCompilationWideSuppression(string id)
            {
                return _compilationWideSuppressions.Contains(id);
            }

            public bool HasGlobalSymbolSuppression(ISymbol symbol, string id)
            {
                Debug.Assert(symbol != null);
                ImmutableArray<string> suppressions;
                return _globalSymbolSuppressions.TryGetValue(symbol, out suppressions) && suppressions.Contains(id);
            }
        }

        public SuppressMessageAttributeState(Compilation compilation)
        {
            _compilation = compilation;
        }

        public bool IsDiagnosticSuppressed(Diagnostic diagnostic, ISymbol symbolOpt = null)
        {
            if (symbolOpt != null && IsDiagnosticSuppressed(diagnostic.Id, symbolOpt))
            {
                return true;
            }

            return IsDiagnosticSuppressed(diagnostic.Id, diagnostic.Location);
        }

        private bool IsDiagnosticSuppressed(string id, ISymbol symbol)
        {
            Debug.Assert(id != null);
            Debug.Assert(symbol != null);

            if (symbol.Kind == SymbolKind.Namespace)
            {
                // Suppressions associated with namespace symbols only apply to namespace declarations themselves
                // and any syntax nodes immediately contained therein, not to nodes attached to any other symbols.
                // Diagnostics those nodes will be filtered by location, not by associated symbol.
                return false;
            }

            if (symbol.Kind == SymbolKind.Method)
            {
                var associated = ((IMethodSymbol)symbol).AssociatedSymbol;
                if (associated != null &&
                    (IsDiagnosticLocallySuppressed(id, associated) || IsDiagnosticGloballySuppressed(id, associated)))
                {
                    return true;
                }
            }

            if (IsDiagnosticLocallySuppressed(id, symbol) || IsDiagnosticGloballySuppressed(id, symbol))
            {
                return true;
            }

            // Check for suppression on parent symbol
            var parent = symbol.ContainingSymbol;
            return parent != null && IsDiagnosticSuppressed(id, parent);
        }

        private bool IsDiagnosticSuppressed(string id, Location location)
        {
            Debug.Assert(id != null);
            Debug.Assert(location != null);

            if (IsDiagnosticGloballySuppressed(id, symbolOpt: null))
            {
                return true;
            }

            // Walk up the syntax tree checking for suppression by any declared symbols encountered
            if (location.IsInSource)
            {
                var model = _compilation.GetSemanticModel(location.SourceTree);
                bool inImmediatelyContainingSymbol = true;

                for (var node = location.SourceTree.GetRoot().FindNode(location.SourceSpan, getInnermostNodeForTie: true);
                    node != null;
                    node = node.Parent)
                {
                    var declaredSymbols = model.GetDeclaredSymbolsForNode(node);
                    Debug.Assert(declaredSymbols != null);

                    foreach (var symbol in declaredSymbols)
                    {
                        if (symbol.Kind == SymbolKind.Namespace)
                        {
                            // Special case: Only suppress syntax diagnostics in namespace declarations if the namespace is the closest containing symbol.
                            // In other words, only apply suppression to the immediately containing namespace declaration and not to its children or parents.
                            return inImmediatelyContainingSymbol && IsDiagnosticGloballySuppressed(id, symbol);
                        }
                        else if (IsDiagnosticLocallySuppressed(id, symbol) || IsDiagnosticGloballySuppressed(id, symbol))
                        {
                            return true;
                        }

                        inImmediatelyContainingSymbol = false;
                    }
                }
            }

            return false;
        }

        private bool IsDiagnosticGloballySuppressed(string id, ISymbol symbolOpt)
        {
            this.DecodeGlobalSuppressMessageAttributes();
            return _lazyGlobalSuppressions.HasCompilationWideSuppression(id) ||
                symbolOpt != null && _lazyGlobalSuppressions.HasGlobalSymbolSuppression(symbolOpt, id);
        }

        private bool IsDiagnosticLocallySuppressed(string id, ISymbol symbol)
        {
            var suppressions = _localSuppressionsBySymbol.GetOrAdd(symbol, this.DecodeSuppressMessageAttributes);
            return suppressions.Contains(id);
        }

        private ISymbol SuppressMessageAttribute
        {
            get
            {
                if (_lazySuppressMessageAttribute == null)
                {
                    _lazySuppressMessageAttribute = _compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute");
                }

                return _lazySuppressMessageAttribute;
            }
        }

        private void DecodeGlobalSuppressMessageAttributes()
        {
            if (_lazyGlobalSuppressions == null)
            {
                var suppressions = new GlobalSuppressions();
                DecodeGlobalSuppressMessageAttributes(_compilation, _compilation.Assembly, this.SuppressMessageAttribute, suppressions);

                foreach (var module in _compilation.Assembly.Modules)
                {
                    DecodeGlobalSuppressMessageAttributes(_compilation, module, this.SuppressMessageAttribute, suppressions);
                }

                Interlocked.CompareExchange(ref _lazyGlobalSuppressions, suppressions, null);
            }
        }

        private ImmutableArray<string> DecodeSuppressMessageAttributes(ISymbol symbol)
        {
            var builder = new ArrayBuilder<string>();

            foreach (var attribute in symbol.GetAttributes().Where(a => a.AttributeClass == this.SuppressMessageAttribute))
            {
                SuppressMessageInfo info;
                if (!TryDecodeSuppressMessageAttributeData(attribute, out info))
                {
                    continue;
                }

                builder.Add(info.Id);
            }

            return builder.ToImmutableAndFree();
        }

        private static void DecodeGlobalSuppressMessageAttributes(Compilation compilation, ISymbol symbol, ISymbol suppressMessageAttribute, GlobalSuppressions globalSuppressions)
        {
            Debug.Assert(symbol is IAssemblySymbol || symbol is IModuleSymbol);

            var attributeInstances = symbol.GetAttributes().Where(a => a.AttributeClass == suppressMessageAttribute);

            foreach (var instance in attributeInstances)
            {
                SuppressMessageInfo info;
                if (!TryDecodeSuppressMessageAttributeData(instance, out info))
                {
                    continue;
                }

                string scopeString = info.Scope != null ? info.Scope.ToLowerInvariant() : null;
                TargetScope scope;

                if (s_suppressMessageScopeTypes.TryGetValue(scopeString, out scope))
                {
                    if ((scope == TargetScope.Module || scope == TargetScope.None) && info.Target == null)
                    {
                        // This suppression is applies to the entire compilation
                        globalSuppressions.AddCompilationWideSuppression(info.Id);
                        continue;
                    }
                }
                else
                {
                    // Invalid value for scope
                    continue;
                }

                // Decode Target
                if (info.Target == null)
                {
                    continue;
                }

                foreach (var target in ResolveTargetSymbols(compilation, info.Target, scope))
                {
                    globalSuppressions.AddGlobalSymbolSuppression(target, info.Id);
                }
            }
        }

        internal static IEnumerable<ISymbol> ResolveTargetSymbols(Compilation compilation, string target, TargetScope scope)
        {
            switch (scope)
            {
                case TargetScope.Namespace:
                case TargetScope.Type:
                case TargetScope.Member:
                    {
                        var results = new List<ISymbol>();
                        new TargetSymbolResolver(compilation, scope, target).Resolve(results);
                        return results;
                    }
                default:
                    return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }
        }

        private static bool TryDecodeSuppressMessageAttributeData(AttributeData attribute, out SuppressMessageInfo info)
        {
            info = default(SuppressMessageInfo);

            // We need at least the Category and Id to decode the diagnostic to suppress.
            // The only SuppressMessageAttribute constructor requires those two parameters.
            if (attribute.CommonConstructorArguments.Length < 2)
            {
                return false;
            }

            // Ignore the category parameter because it does not identify the diagnostic
            // and category information can be obtained from diagnostics themselves.
            info.Id = attribute.CommonConstructorArguments[1].Value as string;
            if (info.Id == null)
            {
                return false;
            }

            // Allow an optional human-readable descriptive name on the end of an Id.
            // See http://msdn.microsoft.com/en-us/library/ms244717.aspx
            var separatorIndex = info.Id.IndexOf(':');
            if (separatorIndex != -1)
            {
                info.Id = info.Id.Remove(separatorIndex);
            }

            info.Scope = attribute.DecodeNamedArgument<string>("Scope", SpecialType.System_String);
            info.Target = attribute.DecodeNamedArgument<string>("Target", SpecialType.System_String);
            info.MessageId = attribute.DecodeNamedArgument<string>("MessageId", SpecialType.System_String);

            return true;
        }

        internal enum TargetScope
        {
            None,
            Module,
            Namespace,
            Resource,
            Type,
            Member
        }

        private struct SuppressMessageInfo
        {
            public string Id;
            public string Scope;
            public string Target;
            public string MessageId;
        }
    }
}
