// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

#if NETSTANDARD2_0
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class SuppressMessageAttributeState
    {
        internal const string SuppressMessageScope = "Scope";
        internal const string SuppressMessageTarget = "Target";

        private static readonly ImmutableDictionary<string, TargetScope> s_targetScopesMap = CreateTargetScopesMap();
        private static readonly ObjectPool<List<ISymbol>> s_listPool = new ObjectPool<List<ISymbol>>(() => new List<ISymbol>());

        private readonly Compilation _compilation;
        private readonly INamedTypeSymbol _suppressMessageAttributeType;
        private readonly Lazy<ImmutableDictionary<SyntaxNode, AttributeData>> _lazySuppressMessageAttributesBySyntax;

        public SuppressMessageAttributeState(Compilation compilation, INamedTypeSymbol suppressMessageAttributeType)
        {
            _compilation = compilation;
            _suppressMessageAttributeType = suppressMessageAttributeType;
            _lazySuppressMessageAttributesBySyntax = new Lazy<ImmutableDictionary<SyntaxNode, AttributeData>>(CreateAttributesBySyntaxMap);
        }

        private static ImmutableDictionary<string, TargetScope> CreateTargetScopesMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, TargetScope>(StringComparer.OrdinalIgnoreCase);

#pragma warning disable CS8605 // Unboxing a possibly null value.
            foreach (TargetScope targetScope in Enum.GetValues(typeof(TargetScope)))
#pragma warning restore CS8605 // Unboxing a possibly null value.
            {
                if (targetScope == TargetScope.None)
                {
                    continue;
                }

                builder.Add(targetScope.ToString(), targetScope);
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<SyntaxNode, AttributeData> CreateAttributesBySyntaxMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<SyntaxNode, AttributeData>();
            AddAttributes(_compilation.Assembly, _suppressMessageAttributeType, builder);
            foreach (var module in _compilation.Assembly.Modules)
            {
                AddAttributes(module, _suppressMessageAttributeType, builder);
            }

            return builder.ToImmutable();

            // Local functions.
            static void AddAttributes(ISymbol symbol, INamedTypeSymbol suppressMessageAttributeType, ImmutableDictionary<SyntaxNode, AttributeData>.Builder builder)
            {
                foreach (var attribute in symbol.GetAttributes())
                {
                    if (suppressMessageAttributeType.Equals(attribute.AttributeClass) &&
                        attribute.ApplicationSyntaxReference?.GetSyntax() is SyntaxNode node)
                    {
                        builder.Add(node, attribute);
                    }
                }
            }
        }

        public bool IsGlobalSuppressMessageAttribute(SyntaxNode attributeSyntax, [NotNullWhen(returnValue: true)] out AttributeData? attribute)
            => _lazySuppressMessageAttributesBySyntax.Value.TryGetValue(attributeSyntax, out attribute);

        public bool HasInvalidScope(AttributeData attribute, out TargetScope targetScope)
        {
            if (!TryGetNamedArgument(attribute, SuppressMessageScope, out var scopeString) ||
                string.IsNullOrEmpty(scopeString))
            {
                // Missing/Null/Empty scope values are treated equivalent to a compilation wide suppression.
                targetScope = TargetScope.Module;
            }
            else if (!s_targetScopesMap.TryGetValue(scopeString!, out targetScope))
            {
                targetScope = TargetScope.None;
                return true;
            }

            return false;
        }

        public bool HasInvalidOrMissingTarget(AttributeData attribute, TargetScope targetScope)
        {
            if (targetScope == TargetScope.Resource)
            {
                // Legacy scope which we do not handle.
                return false;
            }

            if (!TryGetNamedArgument(attribute, SuppressMessageTarget, out var targetSymbolString))
            {
                targetSymbolString = null;
            }

            if (targetScope == TargetScope.Module)
            {
                // Compilation wide suppression with a non-null target is considered invalid.
                return targetSymbolString != null;
            }

            var resolvedSymbols = s_listPool.Allocate();
            try
            {
                var resolver = new TargetSymbolResolver(_compilation, targetScope, targetSymbolString);
                resolvedSymbols.Clear();
                resolver.Resolve(resolvedSymbols);
                return resolvedSymbols.Count == 0;
            }
            finally
            {
                s_listPool.Free(resolvedSymbols);
            }
        }

        private static bool TryGetNamedArgument(AttributeData attribute, string argumentName, out string? argumentValue)
        {
            foreach (var (name, value) in attribute.NamedArguments)
            {
                if (argumentName.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    value.Kind == TypedConstantKind.Primitive &&
                    value.Value is string stringValue)
                {
                    argumentValue = stringValue;
                    return true;
                }
            }

            argumentValue = null;
            return false;
        }
    }
}
