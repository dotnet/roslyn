// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CallHierarchy;

internal abstract class AbstractCallHierarchyService : ICallHierarchyService
{
    public async Task<CallHierarchyItemDescriptor?> CreateItemAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        if (!SupportsCallHierarchy(symbol))
            return null;

        symbol = GetTargetSymbol(symbol);

        if (!CallHierarchyItemId.TryCreate(symbol, project, cancellationToken, out var itemId))
            return null;

        var searchDescriptors = await CreateSearchDescriptorsAsync(symbol, project, cancellationToken).ConfigureAwait(false);
        return new CallHierarchyItemDescriptor(
            itemId,
            symbol.ToDisplayString(CallHierarchyDisplayFormats.MemberNameFormat),
            symbol.ContainingType?.ToDisplayString(CallHierarchyDisplayFormats.ContainingTypeFormat) ?? string.Empty,
            symbol.ContainingNamespace?.ToDisplayString(CallHierarchyDisplayFormats.ContainingNamespaceFormat) ?? string.Empty,
            symbol.GetGlyph(),
            searchDescriptors);
    }

    public async Task<ImmutableArray<CallHierarchySearchResult>> SearchIncomingCallsAsync(
        Solution solution,
        CallHierarchySearchDescriptor searchDescriptor,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var resolved = await searchDescriptor.ItemId.TryResolveAsync(solution, cancellationToken).ConfigureAwait(false);
        if (resolved == null)
            return [];

        var (symbol, project) = resolved.Value;
        return searchDescriptor.Relationship switch
        {
            CallHierarchyRelationshipKind.Callers or
            CallHierarchyRelationshipKind.BaseMember or
            CallHierarchyRelationshipKind.InterfaceImplementations or
            CallHierarchyRelationshipKind.FieldReferences => await SearchCallersAsync(symbol, project, documents, cancellationToken).ConfigureAwait(false),
            CallHierarchyRelationshipKind.CallsToOverrides => await SearchCallsToOverridesAsync(symbol, project, documents, cancellationToken).ConfigureAwait(false),
            CallHierarchyRelationshipKind.Implementations => await SearchImplementationsAsync(symbol, project, documents, cancellationToken).ConfigureAwait(false),
            CallHierarchyRelationshipKind.Overrides => await SearchOverridesAsync(symbol, project, documents, cancellationToken).ConfigureAwait(false),
            _ => [],
        };
    }

    public async Task<ImmutableArray<CallHierarchySearchResult>> SearchOutgoingCallsAsync(
        Solution solution,
        CallHierarchyItemId itemId,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var resolved = await itemId.TryResolveAsync(solution, cancellationToken).ConfigureAwait(false);
        if (resolved == null)
            return [];

        var (symbol, project) = resolved.Value;
        return await SearchOutgoingCallsAsync(symbol, project, documents, cancellationToken).ConfigureAwait(false);
    }

    private static bool SupportsCallHierarchy(ISymbol symbol)
        => symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field;

    private static ISymbol GetTargetSymbol(ISymbol symbol)
    {
        if (symbol is IMethodSymbol methodSymbol)
        {
            methodSymbol = methodSymbol.ReducedFrom ?? methodSymbol;
            methodSymbol = methodSymbol.ConstructedFrom ?? methodSymbol;
            return methodSymbol;
        }

        return symbol;
    }

    private static async Task<ImmutableArray<CallHierarchySearchDescriptor>> CreateSearchDescriptorsAsync(
        ISymbol symbol,
        Project project,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CallHierarchySearchDescriptor>.GetInstance(out var descriptors);

        if (symbol.Kind is SymbolKind.Property or SymbolKind.Event or SymbolKind.Method)
        {
            descriptors.Add(new CallHierarchySearchDescriptor(
                CallHierarchyRelationshipKind.Callers,
                CallHierarchyItemId.Create(symbol, project, cancellationToken)));

            if (symbol.IsVirtual || symbol.IsAbstract)
            {
                descriptors.Add(new CallHierarchySearchDescriptor(
                    CallHierarchyRelationshipKind.Overrides,
                    CallHierarchyItemId.Create(symbol, project, cancellationToken)));
            }

            var overrides = await SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (overrides.Any())
            {
                descriptors.Add(new CallHierarchySearchDescriptor(
                    CallHierarchyRelationshipKind.CallsToOverrides,
                    CallHierarchyItemId.Create(symbol, project, cancellationToken)));
            }

            if (symbol.GetOverriddenMember() is ISymbol overriddenMember &&
                CallHierarchyItemId.TryCreate(overriddenMember, project, cancellationToken, out var overriddenItemId))
            {
                descriptors.Add(new CallHierarchySearchDescriptor(
                    CallHierarchyRelationshipKind.BaseMember,
                    overriddenItemId));
            }

            // An override may not directly implement an interface member, but its base method
            // might.  Walk the override chain so we still surface the "Calls To Interface
            // Implementation" category for such overrides.
            using var __ = PooledHashSet<CallHierarchyItemId>.GetInstance(out var seenInterfaces);
            for (var current = symbol; current != null; current = current.GetOverriddenMember())
            {
                var implementedInterfaceMembers = await SymbolFinder.FindImplementedInterfaceMembersAsync(current, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var implementedInterfaceMember in implementedInterfaceMembers)
                {
                    if (!CallHierarchyItemId.TryCreate(implementedInterfaceMember, project, cancellationToken, out var interfaceItemId))
                        continue;

                    if (!seenInterfaces.Add(interfaceItemId))
                        continue;

                    descriptors.Add(new CallHierarchySearchDescriptor(
                        CallHierarchyRelationshipKind.InterfaceImplementations,
                        interfaceItemId));
                }
            }

            if (symbol.IsImplementableMember())
            {
                descriptors.Add(new CallHierarchySearchDescriptor(
                    CallHierarchyRelationshipKind.Implementations,
                    CallHierarchyItemId.Create(symbol, project, cancellationToken)));
            }

            return descriptors.ToImmutableAndClear();
        }

        if (symbol.Kind == SymbolKind.Field)
        {
            descriptors.Add(new CallHierarchySearchDescriptor(
                CallHierarchyRelationshipKind.FieldReferences,
                CallHierarchyItemId.Create(symbol, project, cancellationToken)));
        }

        return descriptors.ToImmutableAndClear();
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> SearchCallersAsync(
        ISymbol symbol,
        Project project,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var callers = await SymbolFinder.FindCallersAsync(symbol, project.Solution, documents, cancellationToken).ConfigureAwait(false);
        return await CreateCallerResultsAsync(callers.Where(static c => c.IsDirect), project, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> SearchCallsToOverridesAsync(
        ISymbol symbol,
        Project project,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var overrides = await SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<SymbolCallerInfo>.GetInstance(out var callers);

        foreach (var @override in overrides)
        {
            var calls = await SymbolFinder.FindCallersAsync(@override, project.Solution, documents, cancellationToken).ConfigureAwait(false);
            callers.AddRange(calls.Where(static c => c.IsDirect));
            cancellationToken.ThrowIfCancellationRequested();
        }

        return await CreateCallerResultsAsync(callers, project, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> SearchImplementationsAsync(
        ISymbol symbol,
        Project project,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var implementations = await SymbolFinder.FindImplementationsAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await CreateSourceDeclarationResultsAsync(implementations, project, documents, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> SearchOverridesAsync(
        ISymbol symbol,
        Project project,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var overrides = await SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await CreateSourceDeclarationResultsAsync(overrides, project, documents, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> SearchOutgoingCallsAsync(
        ISymbol symbol,
        Project project,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        using var _ = PooledDictionary<CallHierarchyItemId, (CallHierarchyItemDescriptor Item, ArrayBuilder<Location> Locations)>.GetInstance(out var groupedResults);

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var document = project.Solution.GetDocument(syntaxReference.SyntaxTree);
            if (document == null || (documents != null && !documents.Contains(document)))
                continue;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operationRoots = await GetOperationRootsAsync(syntaxReference, semanticModel, cancellationToken).ConfigureAwait(false);

            foreach (var operationRoot in operationRoots)
            {
                foreach (var operation in operationRoot.Descendants())
                {
                    var referencedSymbol = GetReferencedSymbol(operation);
                    if (referencedSymbol == null || !SupportsCallHierarchy(referencedSymbol))
                        continue;

                    var referencedProject = project.Solution.GetProject(referencedSymbol.ContainingAssembly, cancellationToken);
                    if (referencedProject == null)
                        continue;

                    var item = await CreateItemAsync(referencedSymbol, referencedProject, cancellationToken).ConfigureAwait(false);
                    if (item == null)
                        continue;

                    if (!groupedResults.TryGetValue(item.ItemId, out var entry))
                    {
                        entry = (item, ArrayBuilder<Location>.GetInstance());
                        groupedResults.Add(item.ItemId, entry);
                    }

                    entry.Locations.Add(operation.Syntax.GetLocation());
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        using var __ = ArrayBuilder<CallHierarchySearchResult>.GetInstance(out var results);
        foreach (var (_, value) in groupedResults)
        {
            results.Add(new CallHierarchySearchResult(value.Item, value.Locations.ToImmutableAndClear()));
        }

        return results.ToImmutableAndClear();
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> CreateCallerResultsAsync(
        IEnumerable<SymbolCallerInfo> callers,
        Project project,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CallHierarchySearchResult>.GetInstance(out var results);
        using var __ = ArrayBuilder<Location>.GetInstance(out var initializerLocations);

        foreach (var caller in callers)
        {
            if (caller.CallingSymbol.Kind == SymbolKind.Field)
            {
                initializerLocations.AddRange(caller.Locations);
                continue;
            }

            var callingProject = project.Solution.GetProject(caller.CallingSymbol.ContainingAssembly, cancellationToken);
            if (callingProject == null)
                continue;

            var item = await CreateItemAsync(caller.CallingSymbol, callingProject, cancellationToken).ConfigureAwait(false);
            if (item != null)
                results.Add(new CallHierarchySearchResult(item, [.. caller.Locations]));

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (initializerLocations.Count > 0)
            results.Add(new CallHierarchySearchResult(Item: null, initializerLocations.ToImmutable()));

        return results.ToImmutableAndClear();
    }

    private async Task<ImmutableArray<CallHierarchySearchResult>> CreateSourceDeclarationResultsAsync(
        IEnumerable<ISymbol> symbols,
        Project project,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CallHierarchySearchResult>.GetInstance(out var results);

        foreach (var symbol in symbols)
        {
            var sourceLocations = symbol.DeclaringSyntaxReferences.Select(static d => d.SyntaxTree)
                .Select(project.Solution.GetDocument)
                .Where(static d => d != null)
                .Select(static d => d!);
            var bestLocation = sourceLocations.FirstOrDefault(d => documents == null || documents.Contains(d));
            if (bestLocation == null)
                continue;

            var item = await CreateItemAsync(symbol, bestLocation.Project, cancellationToken).ConfigureAwait(false);
            if (item != null)
                results.Add(new CallHierarchySearchResult(item, []));

            cancellationToken.ThrowIfCancellationRequested();
        }

        return results.ToImmutableAndClear();
    }

    private static IOperation? GetReferencedSymbolOperationRoot(IOperation operation)
    {
        while (operation.Parent != null)
            operation = operation.Parent;

        return operation;
    }

    private async Task<ImmutableArray<IOperation>> GetOperationRootsAsync(
        SyntaxReference syntaxReference,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var syntax = await GetOperationRootSyntaxAsync(syntaxReference, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<IOperation>.GetInstance(out var operations);
        using var __ = PooledHashSet<SyntaxNode>.GetInstance(out var seenRootSyntaxes);

        AddOperation(semanticModel.GetOperation(syntax, cancellationToken));

        foreach (var descendant in syntax.DescendantNodesAndSelf())
        {
            AddOperation(semanticModel.GetOperation(descendant, cancellationToken));
        }

        return operations.ToImmutableAndClear();

        void AddOperation(IOperation? operation)
        {
            if (operation == null)
                return;

            var root = GetReferencedSymbolOperationRoot(operation);
            if (root != null && seenRootSyntaxes.Add(root.Syntax))
                operations.Add(root);
        }
    }

    protected virtual Task<SyntaxNode> GetOperationRootSyntaxAsync(SyntaxReference syntaxReference, CancellationToken cancellationToken)
        => syntaxReference.GetSyntaxAsync(cancellationToken);

    private static ISymbol? GetReferencedSymbol(IOperation operation)
        => operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IObjectCreationOperation objectCreation => objectCreation.Constructor,
            IPropertyReferenceOperation propertyReference => propertyReference.Property,
            IEventReferenceOperation eventReference => eventReference.Event,
            IFieldReferenceOperation fieldReference => fieldReference.Field,
            _ => null,
        };
}
