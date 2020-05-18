// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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

        public SuppressMessageAttributeState(Compilation compilation, INamedTypeSymbol suppressMessageAttributeType)
        {
            _compilation = compilation;
            _suppressMessageAttributeType = suppressMessageAttributeType;
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

        public bool IsSuppressMessageAttributeWithNamedArguments(
            SyntaxNode attributeSyntax,
            SemanticModel model,
            CancellationToken cancellationToken,
            out ImmutableArray<(string name, IOperation value)> namedAttributeArguments)
        {
            var attribute = model.GetOperation(attributeSyntax, cancellationToken);
            if (attribute == null)
            {
                namedAttributeArguments = ImmutableArray<(string name, IOperation value)>.Empty;
                return false;
            }

            // Workaround for https://github.com/dotnet/roslyn/issues/18198
            // Use 'IOperation.Children' to get named attribute arguments.
            // Each named attribute argument is represented as an 'ISimpleAssignmentOperation'
            // with a constant value assignment to an 'IPropertyReferenceOperation' in the operation tree.
            using var _ = ArrayBuilder<(string name, IOperation value)>.GetInstance(out var builder);
            foreach (var childOperation in attribute.Children)
            {
                if (childOperation is ISimpleAssignmentOperation simpleAssignment &&
                    simpleAssignment.Target is IPropertyReferenceOperation propertyReference &&
                    _suppressMessageAttributeType.Equals(propertyReference.Property.ContainingType))
                {
                    builder.Add((propertyReference.Property.Name, simpleAssignment.Value));
                }
            }

            namedAttributeArguments = builder.ToImmutable();
            return namedAttributeArguments.Length > 0;
        }

        public bool HasInvalidScope(ImmutableArray<(string name, IOperation value)> namedAttributeArguments, out TargetScope targetScope)
        {
            if (!TryGetNamedArgument(namedAttributeArguments, SuppressMessageScope, out var scopeString) ||
                RoslynString.IsNullOrEmpty(scopeString))
            {
                // Missing/Null/Empty scope values are treated equivalent to a compilation wide suppression.
                targetScope = TargetScope.Module;
            }
            else if (!s_targetScopesMap.TryGetValue(scopeString, out targetScope))
            {
                targetScope = TargetScope.None;
                return true;
            }

            return false;
        }

        public bool HasInvalidOrMissingTarget(ImmutableArray<(string name, IOperation value)> namedAttributeArguments, TargetScope targetScope)
        {
            if (targetScope == TargetScope.Resource)
            {
                // Legacy scope which we do not handle.
                return false;
            }

            if (!TryGetNamedArgument(namedAttributeArguments, SuppressMessageTarget, out var targetSymbolString))
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

        private bool TryGetNamedArgument(ImmutableArray<(string name, IOperation value)> namedAttributeArguments, string argumentName, out string? argumentValue)
        {
            foreach (var (name, value) in namedAttributeArguments)
            {
                if (name == argumentName &&
                    value.ConstantValue.HasValue &&
                    value.ConstantValue.Value is string stringValue)
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
