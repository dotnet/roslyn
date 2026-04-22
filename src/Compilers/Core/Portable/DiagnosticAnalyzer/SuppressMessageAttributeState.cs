// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class SuppressMessageAttributeState
    {
        private static readonly SmallDictionary<string, TargetScope> s_suppressMessageScopeTypes = new SmallDictionary<string, TargetScope>(StringComparer.OrdinalIgnoreCase)
            {
                { string.Empty, TargetScope.None },
                { "module", TargetScope.Module },
                { "namespace", TargetScope.Namespace },
                { "resource", TargetScope.Resource },
                { "type", TargetScope.Type },
                { "member", TargetScope.Member },
                { "namespaceanddescendants", TargetScope.NamespaceAndDescendants }
            };

        private readonly Compilation _compilation;
        private GlobalSuppressions? _lazyGlobalSuppressions;
        private readonly ConcurrentDictionary<ISymbol, ImmutableDictionary<string, SuppressMessageInfo>> _localSuppressionsBySymbol;

        // These are StrongBoxes because 'null' is a valid symbol value to compute for these, and as such, we can't use
        // the null value to indicate 'not yet computed'.

        private StrongBox<ISymbol?>? _lazySuppressMessageAttribute;
        private StrongBox<ISymbol?>? _lazyUnconditionalSuppressMessageAttribute;

        private sealed class GlobalSuppressions(Compilation compilation)
        {
            private readonly Compilation _compilation = compilation;
            private readonly Dictionary<string, SuppressMessageInfo> _compilationWideSuppressions = new Dictionary<string, SuppressMessageInfo>();
            // Keep targeted suppressions grouped by diagnostic ID so we only resolve target symbols
            // for IDs that are actually queried.
            private readonly Dictionary<string, List<SuppressMessageInfo>> _unresolvedSuppressionsById = new Dictionary<string, List<SuppressMessageInfo>>(StringComparer.Ordinal);
            private readonly ConcurrentDictionary<string, FrozenDictionary<ISymbol, SuppressMessageInfo>> _lazyResolvedSuppressionsById = new ConcurrentDictionary<string, FrozenDictionary<ISymbol, SuppressMessageInfo>>(StringComparer.Ordinal);

            public void AddCompilationWideSuppression(SuppressMessageInfo info)
            {
                AddOrUpdate(info, _compilationWideSuppressions);
            }

            public void AddGlobalSymbolSuppression(SuppressMessageInfo info)
            {
                Debug.Assert(_lazyResolvedSuppressionsById.IsEmpty);
                if (!_unresolvedSuppressionsById.TryGetValue(info.Id, out var suppressions))
                {
                    suppressions = [];
                    _unresolvedSuppressionsById.Add(info.Id, suppressions);
                }

                suppressions.Add(info);
            }

            public bool HasCompilationWideSuppression(string id, out SuppressMessageInfo info)
            {
                return _compilationWideSuppressions.TryGetValue(id, out info);
            }

            public bool HasGlobalSymbolSuppression(ISymbol symbol, string id, bool isImmediatelyContainingSymbol, out SuppressMessageInfo info)
            {
                Debug.Assert(symbol != null);
                var suppressions = _lazyResolvedSuppressionsById.GetOrAdd(
                    id,
                    ResolveGlobalSymbolSuppressions);

                Debug.Assert(symbol.IsDefinition);
                if (suppressions.TryGetValue(symbol, out info))
                {
                    if (symbol.Kind != SymbolKind.Namespace)
                    {
                        return true;
                    }

                    switch (info.Scope)
                    {
                        case TargetScope.Namespace:
                            // Special case: Only suppress syntax diagnostics in namespace declarations if the namespace is the closest containing symbol.
                            // In other words, only apply suppression to the immediately containing namespace declaration and not to its children or parents.
                            return isImmediatelyContainingSymbol;

                        case TargetScope.NamespaceAndDescendants:
                            return true;
                    }
                }

                info = default(SuppressMessageInfo);
                return false;
            }

            private FrozenDictionary<ISymbol, SuppressMessageInfo> ResolveGlobalSymbolSuppressions(string id)
            {
                if (!_unresolvedSuppressionsById.TryGetValue(id, out var suppressions))
                {
                    return FrozenDictionary<ISymbol, SuppressMessageInfo>.Empty;
                }

                var resolvedSuppressions = new Dictionary<ISymbol, SuppressMessageInfo>();
                foreach (SuppressMessageInfo info in suppressions)
                {
                    foreach (ISymbol target in ResolveTargetSymbols(_compilation, info.Target, info.Scope))
                    {
                        Debug.Assert(target.IsDefinition);
                        if (!resolvedSuppressions.ContainsKey(target))
                        {
                            resolvedSuppressions.Add(target, info);
                        }
                    }
                }

                return resolvedSuppressions.ToFrozenDictionary();
            }
        }

        internal SuppressMessageAttributeState(Compilation compilation)
        {
            _compilation = compilation;
            _localSuppressionsBySymbol = new ConcurrentDictionary<ISymbol, ImmutableDictionary<string, SuppressMessageInfo>>();
        }

        public Diagnostic ApplySourceSuppressions(Diagnostic diagnostic)
        {
            if (diagnostic.IsSuppressed)
            {
                // Diagnostic already has a source suppression.
                return diagnostic;
            }

            SuppressMessageInfo info;
            if (IsDiagnosticSuppressed(diagnostic, out info))
            {
                // Attach the suppression info to the diagnostic.
                diagnostic = diagnostic.WithIsSuppressed(true);
            }

            return diagnostic;
        }

        public bool IsDiagnosticSuppressed(Diagnostic diagnostic, [NotNullWhen(true)] out AttributeData? suppressingAttribute)
        {
            SuppressMessageInfo info;
            if (IsDiagnosticSuppressed(diagnostic, out info))
            {
                suppressingAttribute = info.Attribute;
                return true;
            }

            suppressingAttribute = null;
            return false;
        }

        private bool IsDiagnosticSuppressed(Diagnostic diagnostic, out SuppressMessageInfo info)
        {
            info = default;

            if (diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.Compiler))
            {
                // SuppressMessage attributes do not apply to compiler diagnostics.
                return false;
            }

            var id = diagnostic.Id;
            var location = diagnostic.Location;

            if (IsDiagnosticGloballySuppressed(id, symbolOpt: null, isImmediatelyContainingSymbol: false, info: out info))
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
                        if (!symbol.IsDefinition || symbol.Kind == SymbolKind.Local)
                        {
                            // This symbol cannot be a target of a SuppressMessageAttribute.
                            continue;
                        }

                        if (symbol.Kind == SymbolKind.Namespace)
                        {
                            return hasNamespaceSuppression((INamespaceSymbol)symbol, inImmediatelyContainingSymbol);
                        }
                        else if (IsDiagnosticLocallySuppressed(id, symbol, out info) || IsDiagnosticGloballySuppressed(id, symbol, inImmediatelyContainingSymbol, out info))
                        {
                            return true;
                        }
                    }

                    if (!declaredSymbols.IsEmpty)
                    {
                        inImmediatelyContainingSymbol = false;
                    }
                }
            }

            return false;

            bool hasNamespaceSuppression(INamespaceSymbol namespaceSymbol, bool inImmediatelyContainingSymbol)
            {
                do
                {
                    if (IsDiagnosticGloballySuppressed(id, namespaceSymbol, inImmediatelyContainingSymbol, out _))
                    {
                        return true;
                    }

                    namespaceSymbol = namespaceSymbol.ContainingNamespace;
                    inImmediatelyContainingSymbol = false;
                }
                while (namespaceSymbol != null);

                return false;
            }
        }

        private bool IsDiagnosticGloballySuppressed(string id, ISymbol? symbolOpt, bool isImmediatelyContainingSymbol, out SuppressMessageInfo info)
        {
            var globalSuppressions = this.DecodeGlobalSuppressMessageAttributes();
            return globalSuppressions.HasCompilationWideSuppression(id, out info) ||
                symbolOpt != null && globalSuppressions.HasGlobalSymbolSuppression(symbolOpt, id, isImmediatelyContainingSymbol, out info);
        }

        private bool IsDiagnosticLocallySuppressed(string id, ISymbol symbol, out SuppressMessageInfo info)
        {
            var suppressions = _localSuppressionsBySymbol.GetOrAdd(symbol, this.DecodeLocalSuppressMessageAttributes);
            return suppressions.TryGetValue(id, out info);
        }

        private ISymbol? SuppressMessageAttribute
        {
            get
            {
                if (_lazySuppressMessageAttribute is null)
                {
                    Interlocked.CompareExchange(
                        ref _lazySuppressMessageAttribute,
                        new StrongBox<ISymbol?>(_compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute")),
                        null);
                }

                return _lazySuppressMessageAttribute.Value;
            }
        }

        private ISymbol? UnconditionalSuppressMessageAttribute
        {
            get
            {
                if (_lazyUnconditionalSuppressMessageAttribute is null)
                {
                    Interlocked.CompareExchange(
                        ref _lazyUnconditionalSuppressMessageAttribute,
                        new StrongBox<ISymbol?>(_compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute")),
                        null);
                }

                return _lazyUnconditionalSuppressMessageAttribute.Value;
            }
        }

        private GlobalSuppressions DecodeGlobalSuppressMessageAttributes()
        {
            if (_lazyGlobalSuppressions == null)
            {
                var suppressions = new GlobalSuppressions(_compilation);
                DecodeGlobalSuppressMessageAttributes(_compilation.Assembly, suppressions);

                foreach (var module in _compilation.Assembly.Modules)
                {
                    DecodeGlobalSuppressMessageAttributes(module, suppressions);
                }

                Interlocked.CompareExchange(ref _lazyGlobalSuppressions, suppressions, null);
            }
            return _lazyGlobalSuppressions;
        }

        private bool IsSuppressionAttribute(AttributeData a)
            => a.AttributeClass == SuppressMessageAttribute || a.AttributeClass == UnconditionalSuppressMessageAttribute;

        private ImmutableDictionary<string, SuppressMessageInfo> DecodeLocalSuppressMessageAttributes(ISymbol symbol)
        {
            var attributes = symbol.GetAttributes().Where(a => IsSuppressionAttribute(a));
            return DecodeLocalSuppressMessageAttributes(attributes);
        }

        private static ImmutableDictionary<string, SuppressMessageInfo> DecodeLocalSuppressMessageAttributes(IEnumerable<AttributeData> attributes)
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

        private void DecodeGlobalSuppressMessageAttributes(ISymbol symbol, GlobalSuppressions globalSuppressions)
        {
            Debug.Assert(symbol is IAssemblySymbol || symbol is IModuleSymbol);

            var attributes = symbol.GetAttributes().Where(a => IsSuppressionAttribute(a));
            DecodeGlobalSuppressMessageAttributes(globalSuppressions, attributes);
        }

        private static void DecodeGlobalSuppressMessageAttributes(GlobalSuppressions globalSuppressions, IEnumerable<AttributeData> attributes)
        {
            foreach (var instance in attributes)
            {
                SuppressMessageInfo info;
                if (!TryDecodeSuppressMessageAttributeData(instance, out info))
                {
                    continue;
                }

                if (info.Scope == TargetScope.Invalid)
                {
                    // Invalid value for scope
                    continue;
                }

                if (info is { Scope: TargetScope.Module or TargetScope.None, Target: null })
                {
                    // This suppression is applies to the entire compilation
                    globalSuppressions.AddCompilationWideSuppression(info);
                    continue;
                }

                // Decode Target
                if (info.Target == null)
                {
                    continue;
                }

                globalSuppressions.AddGlobalSymbolSuppression(info);
            }
        }

        internal static ImmutableArray<ISymbol> ResolveTargetSymbols(Compilation compilation, string target, TargetScope scope)
        {
            switch (scope)
            {
                case TargetScope.Namespace:
                case TargetScope.Type:
                case TargetScope.Member:
                    return new TargetSymbolResolver(compilation, scope, target).Resolve(out _);

                case TargetScope.NamespaceAndDescendants:
                    return ResolveTargetSymbols(compilation, target, TargetScope.Namespace);

                default:
                    return ImmutableArray<ISymbol>.Empty;
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
            info.Id = attribute.CommonConstructorArguments[1].ValueInternal as string;
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

            var scopeString = attribute.DecodeNamedArgument<string>("Scope", SpecialType.System_String);
            if (!tryGetTargetScope(scopeString, out var scope))
            {
                scope = TargetScope.Invalid;
            }

            info.Scope = scope;
            info.Target = attribute.DecodeNamedArgument<string>("Target", SpecialType.System_String);
            info.MessageId = attribute.DecodeNamedArgument<string>("MessageId", SpecialType.System_String);
            info.Attribute = attribute;

            return true;

            static bool tryGetTargetScope(string? scopeString, out TargetScope scope)
                => s_suppressMessageScopeTypes.TryGetValue(scopeString ?? string.Empty, out scope);
        }
    }
}
