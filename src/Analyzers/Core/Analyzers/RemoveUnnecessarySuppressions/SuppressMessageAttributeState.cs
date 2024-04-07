// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class SuppressMessageAttributeState(Compilation compilation, INamedTypeSymbol suppressMessageAttributeType)
{
    internal const string SuppressMessageScope = "Scope";
    internal const string SuppressMessageTarget = "Target";

    private static readonly ImmutableDictionary<string, TargetScope> s_targetScopesMap = CreateTargetScopesMap();

    private readonly Compilation _compilation = compilation;
    private readonly INamedTypeSymbol _suppressMessageAttributeType = suppressMessageAttributeType;

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
        var operation = (model.GetOperation(attributeSyntax, cancellationToken) as IAttributeOperation)?.Operation;
        if (operation is not IObjectCreationOperation { Initializer: { } initializerOperation })
        {
            namedAttributeArguments = [];
            return false;
        }

        using var _ = ArrayBuilder<(string name, IOperation value)>.GetInstance(out var builder);
        foreach (var initializer in initializerOperation.Initializers)
        {
            var simpleAssignment = (ISimpleAssignmentOperation)initializer;
            if (simpleAssignment.Target is IPropertyReferenceOperation propertyReference &&
                _suppressMessageAttributeType.Equals(propertyReference.Property.ContainingType))
            {
                builder.Add((propertyReference.Property.Name, simpleAssignment.Value));
            }
        }

        namedAttributeArguments = builder.ToImmutable();
        return namedAttributeArguments.Length > 0;
    }

    public static bool HasValidScope(ImmutableArray<(string name, IOperation value)> namedAttributeArguments, out TargetScope targetScope)
    {
        if (!TryGetNamedArgument(namedAttributeArguments, SuppressMessageScope, out var scopeString, out _) ||
            RoslynString.IsNullOrEmpty(scopeString))
        {
            // Missing/Null/Empty scope values are treated equivalent to a compilation wide suppression.
            targetScope = TargetScope.Module;
        }
        else if (!s_targetScopesMap.TryGetValue(scopeString, out targetScope))
        {
            targetScope = TargetScope.None;
            return false;
        }

        return true;
    }

    public bool HasValidTarget(
        ImmutableArray<(string name, IOperation value)> namedAttributeArguments,
        TargetScope targetScope,
        out bool targetHasDocCommentIdFormat,
        out string? targetSymbolString,
        out IOperation? targetValueOperation,
        out ImmutableArray<ISymbol> resolvedSymbols)
    {
        targetHasDocCommentIdFormat = false;
        targetSymbolString = null;
        targetValueOperation = null;
        resolvedSymbols = [];

        if (targetScope == TargetScope.Resource)
        {
            // Legacy scope which we do not handle.
            return true;
        }

        if (!TryGetNamedArgument(namedAttributeArguments, SuppressMessageTarget, out targetSymbolString, out targetValueOperation))
        {
            targetSymbolString = null;
        }

        if (targetScope == TargetScope.Module)
        {
            // Compilation wide suppression with a non-null target is considered invalid.
            return targetSymbolString == null;
        }
        else if (targetScope == TargetScope.NamespaceAndDescendants)
        {
            // TargetSymbolResolver expects the callers to normalize 'NamespaceAndDescendants' and 'Namespace' scopes to 'Namespace' scope.
            targetScope = TargetScope.Namespace;
        }

        var resolver = new TargetSymbolResolver(_compilation, targetScope, targetSymbolString);
        resolvedSymbols = resolver.Resolve(out targetHasDocCommentIdFormat);
        return !resolvedSymbols.IsEmpty;
    }

    private static bool TryGetNamedArgument(
        ImmutableArray<(string name, IOperation value)> namedAttributeArguments,
        string argumentName,
        out string? argumentValue,
        [NotNullWhen(returnValue: true)] out IOperation? argumentValueOperation)
    {
        foreach (var (name, value) in namedAttributeArguments)
        {
            if (name == argumentName &&
                value.ConstantValue.HasValue &&
                value.ConstantValue.Value is string stringValue)
            {
                argumentValue = stringValue;
                argumentValueOperation = value;
                return true;
            }
        }

        argumentValue = null;
        argumentValueOperation = null;
        return false;
    }
}
