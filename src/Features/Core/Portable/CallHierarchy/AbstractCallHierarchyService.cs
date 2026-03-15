// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CallHierarchy;

internal abstract class AbstractCallHierarchyService : ICallHierarchyService
{
    public async Task<ImmutableArray<CallHierarchyItem>> PrepareCallHierarchyAsync(
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
            ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is null)
            return [];

        symbol = CallHierarchyHelpers.GetTargetSymbol(symbol);

        if (symbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field))
            return [];

        var item = await CreateItemAsync(symbol, document.Project, cancellationToken).ConfigureAwait(false);
        return item is null ? [] : [item];
    }

    public async Task<ImmutableArray<CallHierarchyIncomingCall>> GetIncomingCallsAsync(
        Solution solution,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var project = solution.GetProject(item.ProjectId);
        if (project is null)
            return [];

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
            return [];

        var resolution = item.SymbolKey.Resolve(compilation, cancellationToken: cancellationToken);
        var symbol = resolution.Symbol;
        if (symbol is null)
            return [];

        using var _ = ArrayBuilder<CallHierarchyIncomingCall>.GetInstance(out var results);

        // Find direct callers using shared helper
        var callers = await CallHierarchyHelpers.FindDirectCallersAsync(symbol, project.Solution, documents: null, cancellationToken).ConfigureAwait(false);
        foreach (var caller in callers.Where(c => c.IsDirect))
        {
            if (caller.CallingSymbol.Kind == SymbolKind.Field)
            {
                // Skip field initializers for now - they need special handling
                continue;
            }

            var callerItem = await CreateItemAsync(caller.CallingSymbol, project, cancellationToken).ConfigureAwait(false);
            if (callerItem is not null)
            {
                using var _1 = ArrayBuilder<(DocumentId DocumentId, TextSpan Span)>.GetInstance(out var callLocations);
                foreach (var location in caller.Locations)
                {
                    if (location.IsInSource)
                    {
                        var doc = project.Solution.GetDocument(location.SourceTree);
                        if (doc is not null)
                        {
                            callLocations.Add((doc.Id, location.SourceSpan));
                        }
                    }
                }

                if (callLocations.Count > 0)
                {
                    results.Add(new CallHierarchyIncomingCall(callerItem, callLocations.ToImmutableArray()));
                }
            }
        }

        // Find calls through overrides using shared helper
        var callersToOverrides = await CallHierarchyHelpers.FindCallersToOverridesAsync(symbol, project.Solution, documents: null, cancellationToken).ConfigureAwait(false);
        foreach (var caller in callersToOverrides.Where(c => c.IsDirect))
        {
            if (caller.CallingSymbol.Kind == SymbolKind.Field)
                continue;

            var callerItem = await CreateItemAsync(caller.CallingSymbol, project, cancellationToken).ConfigureAwait(false);
            if (callerItem is not null)
            {
                using var _2 = ArrayBuilder<(DocumentId DocumentId, TextSpan Span)>.GetInstance(out var callLocations);
                foreach (var location in caller.Locations)
                {
                    if (location.IsInSource)
                    {
                        var doc = project.Solution.GetDocument(location.SourceTree);
                        if (doc is not null)
                        {
                            callLocations.Add((doc.Id, location.SourceSpan));
                        }
                    }
                }

                if (callLocations.Count > 0)
                {
                    results.Add(new CallHierarchyIncomingCall(callerItem, callLocations.ToImmutableArray()));
                }
            }
        }

        // Find implementers if this is an interface member
        if (symbol.IsImplementableMember())
        {
            var implementations = await SymbolFinder.FindImplementationsAsync(symbol, project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var implementation in implementations)
            {
                var implCallers = await SymbolFinder.FindCallersAsync(implementation, project.Solution, cancellationToken).ConfigureAwait(false);
                foreach (var caller in implCallers.Where(c => c.IsDirect))
                {
                    if (caller.CallingSymbol.Kind == SymbolKind.Field)
                        continue;

                    var callerItem = await CreateItemAsync(caller.CallingSymbol, project, cancellationToken).ConfigureAwait(false);
                    if (callerItem is not null)
                    {
                        using var _3 = ArrayBuilder<(DocumentId DocumentId, TextSpan Span)>.GetInstance(out var callLocations);
                        foreach (var location in caller.Locations)
                        {
                            if (location.IsInSource)
                            {
                                var doc = project.Solution.GetDocument(location.SourceTree);
                                if (doc is not null)
                                {
                                    callLocations.Add((doc.Id, location.SourceSpan));
                                }
                            }
                        }

                        if (callLocations.Count > 0)
                        {
                            results.Add(new CallHierarchyIncomingCall(callerItem, callLocations.ToImmutableArray()));
                        }
                    }
                }
            }
        }

        return results.ToImmutableAndClear();
    }

    public async Task<ImmutableArray<CallHierarchyOutgoingCall>> GetOutgoingCallsAsync(
        Solution solution,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var project = solution.GetProject(item.ProjectId);
        if (project is null)
            return [];

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
            return [];

        var resolution = item.SymbolKey.Resolve(compilation, cancellationToken: cancellationToken);
        var symbol = resolution.Symbol;
        if (symbol is null)
            return [];

        var declarations = GetDeclarations(symbol);

        using var _ = ArrayBuilder<CallHierarchyOutgoingCall>.GetInstance(out var results);
        var callGroups = PooledDictionary<ISymbol, ArrayBuilder<(DocumentId, TextSpan)>>.GetInstance();

        try
        {
            foreach (var declaration in declarations)
            {
                var declarationDoc = project.Solution.GetDocument(declaration.SourceTree);
                if (declarationDoc is null)
                    continue;

                var declarationRoot = await declarationDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var declarationNode = declarationRoot.FindNode(declaration.SourceSpan);
                var declarationSemanticModel = await declarationDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Find all descendant nodes that could be invocations/references
                foreach (var node in declarationNode.DescendantNodes())
                {
                    var symbolInfo = declarationSemanticModel.GetSymbolInfo(node, cancellationToken);
                    var calledSymbol = symbolInfo.Symbol;

                    if (calledSymbol is null)
                        continue;

                    // Only include method, property, event, and field symbols
                    if (calledSymbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field))
                        continue;

                    // Skip constructors of the containing type (not interesting for call hierarchy)
                    if (calledSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } method &&
                        SymbolEqualityComparer.Default.Equals(method.ContainingType, symbol.ContainingType))
                        continue;

                    calledSymbol = CallHierarchyHelpers.GetTargetSymbol(calledSymbol);

                    if (!callGroups.TryGetValue(calledSymbol, out var locations))
                    {
                        locations = ArrayBuilder<(DocumentId, TextSpan)>.GetInstance();
                        callGroups[calledSymbol] = locations;
                    }

                    // Check if we already have an overlapping span for this symbol in the same document
                    // This handles cases like N() where both the invocation and identifier resolve to the same symbol
                    var alreadyHasOverlapping = false;
                    foreach (var (docId, existingSpan) in locations)
                    {
                        if (docId == declarationDoc.Id && existingSpan.OverlapsWith(node.Span))
                        {
                            alreadyHasOverlapping = true;
                            break;
                        }
                    }

                    if (!alreadyHasOverlapping)
                    {
                        locations.Add((declarationDoc.Id, node.Span));
                    }
                }
            }

            foreach (var kvp in callGroups)
            {
                var calledSymbol = kvp.Key;
                var locations = kvp.Value;

                var calledItem = await CreateItemAsync(calledSymbol, project, cancellationToken).ConfigureAwait(false);
                if (calledItem is not null)
                {
                    results.Add(new CallHierarchyOutgoingCall(calledItem, locations.ToImmutableArray()));
                }

                locations.Free();
            }
        }
        finally
        {
            callGroups.Free();
        }

        return results.ToImmutableAndClear();
    }

    private static ImmutableArray<Location> GetDeclarations(ISymbol symbol)
    {
        using var _ = ArrayBuilder<Location>.GetInstance(out var results);

        foreach (var location in symbol.Locations)
        {
            if (location.IsInSource)
                results.Add(location);
        }

        return results.ToImmutableAndClear();
    }

    private async Task<CallHierarchyItem?> CreateItemAsync(
        ISymbol symbol,
        Project project,
        CancellationToken cancellationToken)
    {
        symbol = CallHierarchyHelpers.GetTargetSymbol(symbol);

        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return null;

        var document = project.Solution.GetDocument(location.SourceTree);
        if (document is null)
            return null;

        var name = symbol.ToDisplayString(s_memberNameFormat);
        var detail = symbol.ContainingType?.ToDisplayString(s_containingTypeFormat) ?? string.Empty;
        var containingNamespace = symbol.ContainingNamespace?.ToDisplayString(s_containingNamespaceFormat) ?? string.Empty;

        return new CallHierarchyItem(
            symbol.GetSymbolKey(cancellationToken),
            name,
            symbol.Kind,
            detail,
            containingNamespace,
            project.Id,
            document.Id,
            location.SourceSpan);
    }

    private static readonly SymbolDisplayFormat s_memberNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeExtensionThis |
            SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat s_containingTypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat s_containingNamespaceFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
}
