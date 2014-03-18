// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class SuppressMessageAttributeState
    {
        private static readonly SmallDictionary<string, TargetScope> SuppressMessageScopeTypes = new SmallDictionary<string, TargetScope>()
            {
                { null, TargetScope.None },
                { "module", TargetScope.Module },
                { "namespace", TargetScope.Namespace },
                { "resource", TargetScope.Resource },
                { "type", TargetScope.Type },
                { "member", TargetScope.Member }
            };

        private readonly Compilation compilation;
        private GlobalSuppressions lazyGlobalSuppressions;
        private ConcurrentDictionary<ISymbol, ImmutableArray<string>> localSuppressionsBySymbol = new ConcurrentDictionary<ISymbol, ImmutableArray<string>>();
        private ConcurrentDictionary<SyntaxTree, WarningStateMap> allSuppressionsBySyntaxTree = new ConcurrentDictionary<SyntaxTree, WarningStateMap>();
        private ISymbol lazySuppressMessageAttribute;

        private class GlobalSuppressions
        {
            private readonly HashSet<string> compilationWideSuppressions = new HashSet<string>();
            private readonly Dictionary<ISymbol, string> globalSymbolSuppressions = new Dictionary<ISymbol, string>();

            public void AddCompilationWideSuppression(string id)
            {
                this.compilationWideSuppressions.Add(id);
            }

            public void AddGlobalSymbolSuppression(ISymbol symbol, string id)
            {
                this.globalSymbolSuppressions.Add(symbol, id);
            }

            public bool HasCompilationWideSuppression(string id)
            {
                return this.compilationWideSuppressions.Contains(id);
            }

            public bool HasGlobalSymbolSuppression(ISymbol symbol, string id)
            {
                Debug.Assert(symbol != null);
                return this.globalSymbolSuppressions.Contains(KeyValuePair.Create(symbol, id));
            }
        }

        public SuppressMessageAttributeState(Compilation compilation)
        {
            this.compilation = compilation;
        }

        public bool IsDiagnosticSuppressed(string id, ISymbol symbolOpt)
        {
            Debug.Assert(id != null);

            if (symbolOpt == null)
            {
                return IsDiagnosticGloballySuppressed(id, null);
            }

            // Check for local suppression on symbol and global suppresions.
            if (IsDiagnosticLocallySuppressed(id, symbolOpt) || IsDiagnosticGloballySuppressed(id, symbolOpt))
            {
                return true;
            }

            if (symbolOpt.Kind == SymbolKind.Method)
            {
                var associatedPropertyOrEvent = ((IMethodSymbol)symbolOpt).AssociatedPropertyOrEvent;
                if (associatedPropertyOrEvent != null &&
                    (IsDiagnosticLocallySuppressed(id, associatedPropertyOrEvent) || IsDiagnosticGloballySuppressed(id, associatedPropertyOrEvent)))
                {
                    return true;
                }
            }

            // Check for suppression on parent symbol, except for namespaces.
            // FxCop suppressions on namespaces only apply to the namespace declarations, not their contents.
            var parent = symbolOpt.ContainingSymbol;
            return parent != null && parent.Kind != SymbolKind.Namespace ?
                IsDiagnosticSuppressed(id, parent) :
                false;
        }

        private bool IsDiagnosticGloballySuppressed(string id, ISymbol symbolOpt)
        {
            this.DecodeGlobalSuppressMessageAttributes();
            return this.lazyGlobalSuppressions.HasCompilationWideSuppression(id) ||
                symbolOpt != null && this.lazyGlobalSuppressions.HasGlobalSymbolSuppression(symbolOpt, id);
        }

        private bool IsDiagnosticLocallySuppressed(string id, ISymbol symbol)
        {
            var suppressions = this.DecodeSuppressMessageAttributes(symbol);
            return suppressions.Any(s => s == id);
        }

        private ISymbol SuppressMessageAttribute
        {
            get
            {
                if (this.lazySuppressMessageAttribute == null)
                {
                    this.lazySuppressMessageAttribute = compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute");
                }

                return this.lazySuppressMessageAttribute;
            }
        }

        // NOTE: This API assumes that all the suppress message attributes for declared symbols in the source file have been cracked.
        // NOTE: We need to consider removing this assumption and actually walk up the syntax tree to crack open the relevant attributes that can suppress diagnostic at this location.
        internal bool IsDiagnosticSyntacticallySuppressed(string id, Location location)
        {
            Debug.Assert(id != null);
            Debug.Assert(location != null);
            
            // Check for global compilation wide suppression.
            if (IsDiagnosticGloballySuppressed(id, symbolOpt: null))
            {
                return true;
            }

            // Check for suppression by syntax tree.
            WarningStateMap warningStateMap;
            if (location.SourceTree != null && this.allSuppressionsBySyntaxTree.TryGetValue(location.SourceTree, out warningStateMap))
            {
                return warningStateMap.GetWarningState(id, location.SourceSpan.Start) == ReportDiagnostic.Suppress;
            }

            return false;
        }

        private void DecodeGlobalSuppressMessageAttributes()
        {
            if (this.lazyGlobalSuppressions == null)
            {
                var suppressions = new GlobalSuppressions();
                DecodeGlobalSuppressMessageAttributes(compilation, compilation.Assembly, this.SuppressMessageAttribute, suppressions, this.allSuppressionsBySyntaxTree);

                foreach (var module in compilation.Assembly.Modules)
                {
                    DecodeGlobalSuppressMessageAttributes(compilation, module, this.SuppressMessageAttribute, suppressions, this.allSuppressionsBySyntaxTree);
                }

                Interlocked.CompareExchange(ref this.lazyGlobalSuppressions, suppressions, null);
            }
        }

        internal ImmutableArray<string> DecodeSuppressMessageAttributes(ISymbol symbol)
        {
            if (!this.localSuppressionsBySymbol.ContainsKey(symbol))
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
                    AddSuppressionToSyntaxTrees(info.Id, symbol, this.allSuppressionsBySyntaxTree);
                }

                var suppressions = builder.ToImmutableAndFree();
                return this.localSuppressionsBySymbol.AddOrUpdate(symbol, suppressions, (s, a) => suppressions);
            }

            return this.localSuppressionsBySymbol[symbol];
        }

        private static void DecodeGlobalSuppressMessageAttributes(Compilation compilation, ISymbol symbol, ISymbol suppressMessageAttribute, GlobalSuppressions globalSuppressions, ConcurrentDictionary<SyntaxTree, WarningStateMap> localSuppressionsBySyntaxTree)
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

                // Decode Scope
                string scopeString = info.Scope != null ? info.Scope.ToLowerInvariant() : null;
                TargetScope scope;

                if (SuppressMessageScopeTypes.TryGetValue(scopeString, out scope))
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

                    AddSuppressionToSyntaxTrees(info.Id, target, localSuppressionsBySyntaxTree);
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

        private static void AddSuppressionToSyntaxTrees(string id, ISymbol symbol, ConcurrentDictionary<SyntaxTree, WarningStateMap> allSuppressionsBySyntaxTree)
        {
            // TODO(naslotto): Instead of (node.)location.SourceSpan, use GetDeclarationsInSpan to get the actual declaration node and use its span
            var namespaceSymbol = symbol as INamespaceSymbol;
            if (namespaceSymbol != null)
            {
                // FxCop suppressions on namespaces only apply to the namespace declarations, not their contents
                foreach (var location in symbol.Locations.Where(loc => loc.IsInSource))
                {
                    var warningStateMap = allSuppressionsBySyntaxTree.GetOrAdd(location.SourceTree, new WarningStateMap());
                    warningStateMap.AddSuppression(id, location.SourceSpan);
                }
            }
            else
            {
                // All other suppressions apply to the declaration and the contents
                foreach (var node in symbol.DeclaringSyntaxReferences)
                {
                    var warningStateMap = allSuppressionsBySyntaxTree.GetOrAdd(node.SyntaxTree, new WarningStateMap());
                    warningStateMap.AddSuppression(id, node.Location.SourceSpan);
                }
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
            info.Id = (string)attribute.CommonConstructorArguments[1].Value;

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
