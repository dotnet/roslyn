// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CallHierarchy;

internal abstract class AbstractCallHierarchyService : ICallHierarchyService
{
    public async Task<CallHierarchyItem?> GetCallHierarchyItemAsync(
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
            semanticModel, position, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);

        if (symbol == null)
            return null;

        // Only support methods, properties, events, and fields
        if (symbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field))
            return null;

        symbol = GetTargetSymbol(symbol);

        return new CallHierarchyItem(symbol, document.Project, []);
    }

    public async Task<ImmutableArray<CallHierarchyIncomingCall>> FindIncomingCallsAsync(
        Document document,
        ISymbol symbol,
        CancellationToken cancellationToken)
    {
        var project = document.Project;
        var solution = project.Solution;

        using var _ = ArrayBuilder<CallHierarchyIncomingCall>.GetInstance(out var results);

        // Find direct callers
        var callers = await SymbolFinder.FindCallersAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

        foreach (var caller in callers.Where(c => c.IsDirect))
        {
            var callingSymbol = caller.CallingSymbol;

            // Skip field initializers - they'll be grouped separately
            if (callingSymbol.Kind == SymbolKind.Field)
                continue;

            var callingProject = solution.GetProject(callingSymbol.ContainingAssembly, cancellationToken);
            if (callingProject != null)
            {
                var item = new CallHierarchyItem(callingSymbol, callingProject, [.. caller.Locations]);
                results.Add(new CallHierarchyIncomingCall(item, [.. caller.Locations]));
            }
        }

        // If the symbol is virtual or abstract, find calls to overrides
        if (symbol.IsVirtual || symbol.IsAbstract)
        {
            var overrides = await SymbolFinder.FindOverridesAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var overrideSymbol in overrides)
            {
                var overrideCallers = await SymbolFinder.FindCallersAsync(overrideSymbol, solution, cancellationToken).ConfigureAwait(false);

                foreach (var caller in overrideCallers.Where(c => c.IsDirect))
                {
                    var callingSymbol = caller.CallingSymbol;
                    if (callingSymbol.Kind == SymbolKind.Field)
                        continue;

                    var callingProject = solution.GetProject(callingSymbol.ContainingAssembly, cancellationToken);
                    if (callingProject != null)
                    {
                        var item = new CallHierarchyItem(callingSymbol, callingProject, [.. caller.Locations]);
                        results.Add(new CallHierarchyIncomingCall(item, [.. caller.Locations]));
                    }
                }
            }
        }

        // If the symbol overrides something, find calls to the base
        if (symbol.GetOverriddenMember() is ISymbol overriddenMember)
        {
            var baseCallers = await SymbolFinder.FindCallersAsync(overriddenMember, solution, cancellationToken).ConfigureAwait(false);

            foreach (var caller in baseCallers.Where(c => c.IsDirect))
            {
                var callingSymbol = caller.CallingSymbol;
                if (callingSymbol.Kind == SymbolKind.Field)
                    continue;

                var callingProject = solution.GetProject(callingSymbol.ContainingAssembly, cancellationToken);
                if (callingProject != null)
                {
                    var item = new CallHierarchyItem(callingSymbol, callingProject, [.. caller.Locations]);
                    results.Add(new CallHierarchyIncomingCall(item, [.. caller.Locations]));
                }
            }
        }

        // If the symbol implements an interface, find calls to the interface member
        var implementedInterfaceMembers = await SymbolFinder.FindImplementedInterfaceMembersAsync(
            symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        foreach (var interfaceMember in implementedInterfaceMembers)
        {
            var interfaceCallers = await SymbolFinder.FindCallersAsync(interfaceMember, solution, cancellationToken).ConfigureAwait(false);

            foreach (var caller in interfaceCallers.Where(c => c.IsDirect))
            {
                var callingSymbol = caller.CallingSymbol;
                if (callingSymbol.Kind == SymbolKind.Field)
                    continue;

                var callingProject = solution.GetProject(callingSymbol.ContainingAssembly, cancellationToken);
                if (callingProject != null)
                {
                    var item = new CallHierarchyItem(callingSymbol, callingProject, [.. caller.Locations]);
                    results.Add(new CallHierarchyIncomingCall(item, [.. caller.Locations]));
                }
            }
        }

        return results.ToImmutableAndClear();
    }

    public async Task<ImmutableArray<CallHierarchyOutgoingCall>> FindOutgoingCallsAsync(
        Document document,
        ISymbol symbol,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CallHierarchyOutgoingCall>.GetInstance(out var results);

        // Get all the syntax references for this symbol
        foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
        {
            var syntaxTree = syntaxRef.SyntaxTree;
            var declaringDocument = document.Project.Solution.GetDocument(syntaxTree);

            if (declaringDocument == null)
                continue;

            var semanticModel = await declaringDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            // Get all descendant nodes and find invocations/member accesses
            var descendantNodes = node.DescendantNodes();

            // Group calls by the symbol being called
            var callsBySymbol = new Dictionary<ISymbol, List<Location>>(SymbolEqualityComparer.Default);

            foreach (var descendantNode in descendantNodes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(descendantNode, cancellationToken);
                var calledSymbol = symbolInfo.Symbol;

                if (calledSymbol != null &&
                    calledSymbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field)
                {
                    calledSymbol = GetTargetSymbol(calledSymbol);

                    if (!callsBySymbol.TryGetValue(calledSymbol, out var locations))
                    {
                        locations = [];
                        callsBySymbol[calledSymbol] = locations;
                    }

                    locations.Add(descendantNode.GetLocation());
                }
            }

            // Create call hierarchy items for each unique called symbol
            foreach (var (calledSymbol, locations) in callsBySymbol)
            {
                var calledProject = document.Project.Solution.GetProject(calledSymbol.ContainingAssembly, cancellationToken);
                if (calledProject != null)
                {
                    var item = new CallHierarchyItem(calledSymbol, calledProject, []);
                    results.Add(new CallHierarchyOutgoingCall(item, [.. locations]));
                }
            }
        }

        return results.ToImmutableAndClear();
    }

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
}
