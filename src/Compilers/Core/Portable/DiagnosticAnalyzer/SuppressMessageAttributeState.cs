// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

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
        private readonly ConcurrentDictionary<ISymbol, ImmutableDictionary<string, SuppressMessageInfo>> _localSuppressionsBySymbol;
        private ISymbol _lazySuppressMessageAttribute;

        private class GlobalSuppressions
        {
            private readonly Dictionary<string, SuppressMessageInfo> _compilationWideSuppressions = new Dictionary<string, SuppressMessageInfo>();
            private readonly Dictionary<ISymbol, Dictionary<string, SuppressMessageInfo>> _globalSymbolSuppressions = new Dictionary<ISymbol, Dictionary<string, SuppressMessageInfo>>();

            public void AddCompilationWideSuppression(SuppressMessageInfo info)
            {
                AddOrUpdate(info, _compilationWideSuppressions);
            }

            public void AddGlobalSymbolSuppression(ISymbol symbol, SuppressMessageInfo info)
            {
                Dictionary<string, SuppressMessageInfo> suppressions;
                if (_globalSymbolSuppressions.TryGetValue(symbol, out suppressions))
                {
                    AddOrUpdate(info, suppressions);
                }
                else
                {
                    suppressions = new Dictionary<string, SuppressMessageInfo>() { { info.Id, info } };
                    _globalSymbolSuppressions.Add(symbol, suppressions);
                }
            }

            public bool HasCompilationWideSuppression(string id, out SuppressMessageInfo info)
            {
                return _compilationWideSuppressions.TryGetValue(id, out info);
            }

            public bool HasGlobalSymbolSuppression(ISymbol symbol, string id, out SuppressMessageInfo info)
            {
                Debug.Assert(symbol != null);
                Dictionary<string, SuppressMessageInfo> suppressions;
                if (_globalSymbolSuppressions.TryGetValue(symbol, out suppressions) &&
                    suppressions.TryGetValue(id, out info))
                {
                    return true;
                }

                info = default(SuppressMessageInfo);
                return false;
            }
        }

        internal SuppressMessageAttributeState(Compilation compilation)
        {
            _compilation = compilation;
            _localSuppressionsBySymbol = new ConcurrentDictionary<ISymbol, ImmutableDictionary<string, SuppressMessageInfo>>();
        }

        public static Diagnostic ApplySourceSuppressions(Diagnostic diagnostic, Compilation compilation, ISymbol symbolOpt = null)
        {
            if (diagnostic.IsSuppressed)
            {
                // Diagnostic already has a source suppression.
                return diagnostic;
            }

            SuppressMessageInfo info;
            if (IsDiagnosticSuppressed(diagnostic, compilation, out info))
            {
                // Attach the suppression info to the diagnostic.
                diagnostic = diagnostic.WithIsSuppressed(true);
            }

            return diagnostic;
        }

        public static bool IsDiagnosticSuppressed(Diagnostic diagnostic, Compilation compilation, out AttributeData suppressingAttribute)
        {
            SuppressMessageInfo info;
            if (IsDiagnosticSuppressed(diagnostic, compilation, out info))
            {
                suppressingAttribute = info.Attribute;
                return true;
            }

            suppressingAttribute = null;
            return false;
        }

        private static bool IsDiagnosticSuppressed(Diagnostic diagnostic, Compilation compilation, out SuppressMessageInfo info)
        {
            var suppressMessageState = AnalyzerDriver.GetOrCreateCachedCompilationData(compilation).SuppressMessageAttributeState;
            return suppressMessageState.IsDiagnosticSuppressed(diagnostic, out info);
        }

        private bool IsDiagnosticSuppressed(Diagnostic diagnostic, out SuppressMessageInfo info, ISymbol symbolOpt = null)
        {
            if (symbolOpt != null && IsDiagnosticSuppressed(diagnostic.Id, symbolOpt, out info))
            {
                return true;
            }

            return IsDiagnosticSuppressed(diagnostic.Id, diagnostic.Location, out info);
        }

        private bool IsDiagnosticSuppressed(string id, ISymbol symbol, out SuppressMessageInfo info)
        {
            Debug.Assert(id != null);
            Debug.Assert(symbol != null);

            if (symbol.Kind == SymbolKind.Namespace)
            {
                // Suppressions associated with namespace symbols only apply to namespace declarations themselves
                // and any syntax nodes immediately contained therein, not to nodes attached to any other symbols.
                // Diagnostics those nodes will be filtered by location, not by associated symbol.
                info = default(SuppressMessageInfo);
                return false;
            }

            if (symbol.Kind == SymbolKind.Method)
            {
                var associated = ((IMethodSymbol)symbol).AssociatedSymbol;
                if (associated != null &&
                    (IsDiagnosticLocallySuppressed(id, associated, out info) || IsDiagnosticGloballySuppressed(id, associated, out info)))
                {
                    return true;
                }
            }

            if (IsDiagnosticLocallySuppressed(id, symbol, out info) || IsDiagnosticGloballySuppressed(id, symbol, out info))
            {
                return true;
            }

            // Check for suppression on parent symbol
            var parent = symbol.ContainingSymbol;
            return parent != null && IsDiagnosticSuppressed(id, parent, out info);
        }

        private bool IsDiagnosticSuppressed(string id, Location location, out SuppressMessageInfo info)
        {
            Debug.Assert(id != null);
            Debug.Assert(location != null);

            info = default(SuppressMessageInfo);

            if (IsDiagnosticGloballySuppressed(id, symbolOpt: null, info: out info))
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
                            return inImmediatelyContainingSymbol && IsDiagnosticGloballySuppressed(id, symbol, out info);
                        }
                        else if (IsDiagnosticLocallySuppressed(id, symbol, out info) || IsDiagnosticGloballySuppressed(id, symbol, out info))
                        {
                            return true;
                        }

                        inImmediatelyContainingSymbol = false;
                    }
                }
            }

            return false;
        }

        private bool IsDiagnosticGloballySuppressed(string id, ISymbol symbolOpt, out SuppressMessageInfo info)
        {
            this.DecodeGlobalSuppressMessageAttributes();
            return _lazyGlobalSuppressions.HasCompilationWideSuppression(id, out info) ||
                symbolOpt != null && _lazyGlobalSuppressions.HasGlobalSymbolSuppression(symbolOpt, id, out info);
        }

        private bool IsDiagnosticLocallySuppressed(string id, ISymbol symbol, out SuppressMessageInfo info)
        {
            var suppressions = _localSuppressionsBySymbol.GetOrAdd(symbol, this.DecodeLocalSuppressMessageAttributes);
            return suppressions.TryGetValue(id, out info);
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
                DecodeGlobalSuppressMessageAttributes(_compilation, _compilation.Assembly, suppressions);

                foreach (var module in _compilation.Assembly.Modules)
                {
                    DecodeGlobalSuppressMessageAttributes(_compilation, module, suppressions);
                }

                Interlocked.CompareExchange(ref _lazyGlobalSuppressions, suppressions, null);
            }
        }

        private ImmutableDictionary<string, SuppressMessageInfo> DecodeLocalSuppressMessageAttributes(ISymbol symbol)
        {
            var attributes = symbol.GetAttributes().Where(a => a.AttributeClass == this.SuppressMessageAttribute);
            return DecodeLocalSuppressMessageAttributes(symbol, attributes);
        }

        private static ImmutableDictionary<string, SuppressMessageInfo> DecodeLocalSuppressMessageAttributes(ISymbol symbol, IEnumerable<AttributeData> attributes)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, SuppressMessageInfo>();
            foreach (var attribute in attributes)
            {
                SuppressMessageInfo info;
                if (!TryDecodeSuppressMessageAttributeData(attribute, out info))
                {
                    continue;
                }

                AddOrUpdate(info, builder);
            }

            return builder.ToImmutable();
        }

        private static void AddOrUpdate(SuppressMessageInfo info, IDictionary<string, SuppressMessageInfo> builder)
        {
            // TODO: How should we deal with multiple SuppressMessage attributes, with different suppression info/states?
            // For now, we just pick the last attribute, if not suppressed.
            SuppressMessageInfo currentInfo;
            if (!builder.TryGetValue(info.Id, out currentInfo))
            {
                builder[info.Id] = info;
            }
        }

        private void DecodeGlobalSuppressMessageAttributes(Compilation compilation, ISymbol symbol, GlobalSuppressions globalSuppressions)
        {
            Debug.Assert(symbol is IAssemblySymbol || symbol is IModuleSymbol);

            var attributes = symbol.GetAttributes().Where(a => a.AttributeClass == this.SuppressMessageAttribute);
            DecodeGlobalSuppressMessageAttributes(compilation, symbol, globalSuppressions, attributes);
        }

        private static void DecodeGlobalSuppressMessageAttributes(Compilation compilation, ISymbol symbol, GlobalSuppressions globalSuppressions, IEnumerable<AttributeData> attributes)
        {
            foreach (var instance in attributes)
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
                        globalSuppressions.AddCompilationWideSuppression(info);
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
                    globalSuppressions.AddGlobalSymbolSuppression(target, info);
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
            info.Attribute = attribute;

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
    }
}
