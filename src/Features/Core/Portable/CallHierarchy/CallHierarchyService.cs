// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Implementation of <see cref="ICallHierarchyService"/> that provides call hierarchy functionality.
/// </summary>
internal sealed class CallHierarchyService : ICallHierarchyService
{
    public static readonly CallHierarchyService Instance = new();

    private CallHierarchyService()
    {
    }

    /// <summary>
    /// Symbol display format for member names in call hierarchy.
    /// </summary>
    private static readonly SymbolDisplayFormat s_memberNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeExtensionThis |
            SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Symbol display format for containing type names.
    /// </summary>
    private static readonly SymbolDisplayFormat s_containingTypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Symbol display format for containing namespace names.
    /// </summary>
    private static readonly SymbolDisplayFormat s_containingNamespaceFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public async Task<CallHierarchyItem?> PrepareCallHierarchyAsync(
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
            document, position, preferPrimaryConstructor: true, cancellationToken).ConfigureAwait(false);

        if (symbolAndProject is not var (symbol, project))
            return null;

        // Only support methods, properties, events, and fields
        if (symbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field))
            return null;

        symbol = GetTargetSymbol(symbol);

        return await CreateCallHierarchyItemAsync(symbol, project, cancellationToken).ConfigureAwait(false);
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

        var symbol = item.SymbolKey.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
        if (symbol is null)
            return [];

        var callers = await SymbolFinder.FindCallersAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<CallHierarchyIncomingCall>.GetInstance(out var results);

        // Group callers by their calling symbol to combine call sites
        var directCallers = callers.Where(c => c.IsDirect);
        foreach (var caller in directCallers)
        {
            // Skip field initializers - they're handled separately in VS
            if (caller.CallingSymbol.Kind == SymbolKind.Field)
                continue;

            var callingProject = solution.GetProject(caller.CallingSymbol.ContainingAssembly, cancellationToken);
            if (callingProject is null)
                continue;

            var callerItem = await CreateCallHierarchyItemAsync(caller.CallingSymbol, callingProject, cancellationToken).ConfigureAwait(false);
            if (callerItem is null)
                continue;

            // Convert caller locations to text spans
            var callSites = caller.Locations
                .Where(loc => loc.IsInSource)
                .Select(loc => loc.SourceSpan)
                .ToImmutableArray();

            if (!callSites.IsEmpty)
            {
                results.Add(new CallHierarchyIncomingCall(callerItem.Value, callSites));
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

        var symbol = item.SymbolKey.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
        if (symbol is null)
            return [];

        // Get the document containing the symbol definition
        var document = solution.GetDocument(item.DocumentId);
        if (document is null)
            return [];

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return [];

        // Find all method/property/etc. references within the symbol's body
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return [];

        // Get the syntax node for the symbol
        var syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault(
            sr => sr.SyntaxTree == syntaxRoot.SyntaxTree && item.Span.IntersectsWith(sr.Span));

        if (syntaxReference is null)
            return [];

        var node = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

        // Find all invocations within this node
        using var _ = PooledDictionary<ISymbol, ArrayBuilder<TextSpan>>.GetInstance(out var calleeToCallSites);

        // Track seen spans to avoid duplicate entries (e.g., from both invocation and its identifier child)
        using var seenSpansPooledObject = PooledHashSet<TextSpan>.GetInstance(out var seenSpans);

        foreach (var descendant in node.DescendantNodes())
        {
            var symbolInfo = semanticModel.GetSymbolInfo(descendant, cancellationToken);
            var calledSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (calledSymbol is null)
                continue;

            // Only include method, property, and event invocations
            if (calledSymbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field))
                continue;

            // Skip constructors for the same type (base/this calls are less interesting)
            if (calledSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
            {
                if (SymbolEqualityComparer.Default.Equals(constructor.ContainingType, symbol.ContainingType))
                    continue;
            }

            // Skip accessors - we'll show the property instead
            if (calledSymbol is IMethodSymbol { AssociatedSymbol: not null } accessor)
                calledSymbol = accessor.AssociatedSymbol;

            // Check if we've already seen a call site that contains this one
            // (e.g., an invocation expression containing an identifier)
            var span = descendant.Span;
            var alreadyCovered = false;
            foreach (var seenSpan in seenSpans)
            {
                if (seenSpan.Contains(span) || span.Contains(seenSpan))
                {
                    alreadyCovered = true;
                    break;
                }
            }

            if (alreadyCovered)
                continue;

            seenSpans.Add(span);

            if (!calleeToCallSites.TryGetValue(calledSymbol, out var sites))
            {
                sites = ArrayBuilder<TextSpan>.GetInstance();
                calleeToCallSites.Add(calledSymbol, sites);
            }

            sites.Add(span);
        }

        using var resultBuilder = ArrayBuilder<CallHierarchyOutgoingCall>.GetInstance(out var results);

        foreach (var kvp in calleeToCallSites)
        {
            var callee = kvp.Key;
            var callSitesBuilder = kvp.Value;
            var calleeProject = solution.GetProject(callee.ContainingAssembly, cancellationToken) ?? project;
            var calleeItem = await CreateCallHierarchyItemAsync(callee, calleeProject, cancellationToken).ConfigureAwait(false);

            if (calleeItem is not null)
            {
                results.Add(new CallHierarchyOutgoingCall(calleeItem.Value, callSitesBuilder.ToImmutableAndFree()));
            }
            else
            {
                callSitesBuilder.Free();
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

    private static async Task<CallHierarchyItem?> CreateCallHierarchyItemAsync(
        ISymbol symbol,
        Project project,
        CancellationToken cancellationToken)
    {
        // Find the primary location of the symbol
        var location = symbol.Locations.FirstOrDefault(loc => loc.IsInSource);
        if (location is null)
        {
            // For metadata symbols, try to find the declaring syntax
            var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef is not null)
            {
                var node = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                location = node.GetLocation();
            }
        }

        if (location?.SourceTree is null)
            return null;

        var document = project.Solution.GetDocument(location.SourceTree);
        if (document is null)
            return null;

        var syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault(
            sr => sr.SyntaxTree == location.SourceTree);

        var span = syntaxReference?.Span ?? location.SourceSpan;
        var selectionSpan = location.SourceSpan;

        return new CallHierarchyItem(
            name: symbol.ToDisplayString(s_memberNameFormat),
            kind: symbol.Kind,
            detail: GetDetail(symbol),
            glyph: symbol.GetGlyph(),
            documentId: document.Id,
            span: span,
            selectionSpan: selectionSpan,
            symbolKey: symbol.GetSymbolKey(cancellationToken),
            projectId: project.Id,
            containingTypeName: symbol.ContainingType?.ToDisplayString(s_containingTypeFormat),
            containingNamespaceName: symbol.ContainingNamespace?.ToDisplayString(s_containingNamespaceFormat));
    }

    private static string? GetDetail(ISymbol symbol)
    {
        // Return the full signature as the detail
        return symbol switch
        {
            IMethodSymbol method => method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            IPropertySymbol property => property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            IEventSymbol eventSymbol => eventSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            IFieldSymbol field => field.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            _ => null
        };
    }
}
