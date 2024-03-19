// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Progression;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

[GraphProvider(Name = nameof(RoslynGraphProvider), ProjectCapability = "(CSharp | VB)")]
internal sealed class RoslynGraphProvider : IGraphProvider
{
    private readonly IThreadingContext _threadingContext;
    private readonly IGlyphService _glyphService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly Workspace _workspace;
    private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;
    private readonly GraphQueryManager _graphQueryManager;

    private bool _initialized = false;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynGraphProvider(
        IThreadingContext threadingContext,
        IGlyphService glyphService,
        SVsServiceProvider serviceProvider,
        VisualStudioWorkspace workspace,
        Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _glyphService = glyphService;
        _serviceProvider = serviceProvider;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.GraphProvider);
        _workspace = workspace;
        _streamingPresenter = streamingPresenter;
        _graphQueryManager = new GraphQueryManager(workspace, threadingContext, _asyncListener);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        var iconService = (IIconService)_serviceProvider.GetService(typeof(IIconService));
        IconHelper.Initialize(_glyphService, iconService);
        _initialized = true;
    }

    public static ImmutableArray<IGraphQuery> GetGraphQueries(
        IGraphContext context,
        IAsynchronousOperationListener asyncListener)
    {
        using var _ = ArrayBuilder<IGraphQuery>.GetInstance(out var graphQueries);

        if (context.Direction == GraphContextDirection.Self && context.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren))
        {
            graphQueries.Add(new ContainsChildrenGraphQuery());
        }

        if (context.Direction == GraphContextDirection.Contains ||
            (context.Direction == GraphContextDirection.Target && context.LinkCategories.Contains(CodeLinkCategories.Contains)))
        {
            graphQueries.Add(new ContainsGraphQuery());
        }

        if (context.LinkCategories.Contains(CodeLinkCategories.InheritsFrom))
        {
            if (context.Direction == GraphContextDirection.Target)
            {
                graphQueries.Add(new InheritsGraphQuery());
            }
            else if (context.Direction == GraphContextDirection.Source)
            {
                graphQueries.Add(new InheritedByGraphQuery());
            }
        }

        if (context.LinkCategories.Contains(CodeLinkCategories.SourceReferences))
        {
            graphQueries.Add(new IsUsedByGraphQuery());
        }

        if (context.LinkCategories.Contains(CodeLinkCategories.Calls))
        {
            if (context.Direction == GraphContextDirection.Target)
            {
                graphQueries.Add(new CallsGraphQuery());
            }
            else if (context.Direction == GraphContextDirection.Source)
            {
                graphQueries.Add(new IsCalledByGraphQuery());
            }
        }

        if (context.LinkCategories.Contains(CodeLinkCategories.Implements))
        {
            if (context.Direction == GraphContextDirection.Target)
            {
                graphQueries.Add(new ImplementsGraphQuery());
            }
            else if (context.Direction == GraphContextDirection.Source)
            {
                graphQueries.Add(new ImplementedByGraphQuery());
            }
        }

        if (context.LinkCategories.Contains(RoslynGraphCategories.Overrides))
        {
            if (context.Direction == GraphContextDirection.Source)
            {
                graphQueries.Add(new OverridesGraphQuery());
            }
            else if (context.Direction == GraphContextDirection.Target)
            {
                graphQueries.Add(new OverriddenByGraphQuery());
            }
        }

        if (context.Direction == GraphContextDirection.Custom)
        {
            var searchParameters = context.GetValue<ISolutionSearchParameters>(typeof(ISolutionSearchParameters).GUID.ToString());

            if (searchParameters != null)
            {
                // WARNING: searchParameters.SearchQuery returns an IVsSearchQuery object, which is a COM type.
                // Therefore, it's probably best to grab the values we want now rather than get surprised by COM
                // marshalling later.
                //
                // Create two queries.  One to find results in normal docs, and one to find results in generated
                // docs.  That way if the generated docs take a long time we can still report the regular doc
                // results immediately.
                graphQueries.Add(new SearchGraphQuery(searchParameters.SearchQuery.SearchString, NavigateToSearchScope.RegularDocuments, asyncListener));
                graphQueries.Add(new SearchGraphQuery(searchParameters.SearchQuery.SearchString, NavigateToSearchScope.GeneratedDocuments, asyncListener));
            }
        }

        return graphQueries.ToImmutable();
    }

    public void BeginGetGraphData(IGraphContext context)
    {
        EnsureInitialized();

        var graphQueries = GetGraphQueries(context, _asyncListener);

        // Perform the queries asynchronously  in a fire-and-forget fashion.  This helper will be responsible
        // for always completing the context. AddQueriesAsync is `async`, so it always returns a task and will never
        // bubble out an exception synchronously (so CompletesAsyncOperation is safe).
        var asyncToken = _asyncListener.BeginAsyncOperation(nameof(BeginGetGraphData));
        _ = _graphQueryManager
            .AddQueriesAsync(context, graphQueries, _threadingContext.DisposalToken)
            .CompletesAsyncOperation(asyncToken);
    }

    public IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes)
    {
        EnsureInitialized();

        // Only nodes that explicitly state that they contain children (e.g., source files) and named types should
        // be expandable.
        if (nodes.Any(n => n.Properties.Any(p => p.Key == DgmlNodeProperties.ContainsChildren)) ||
            nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType)))
        {
            yield return new GraphCommand(
                GraphCommandDefinition.Contains,
                targetCategories: null,
                linkCategories: new[] { GraphCommonSchema.Contains },
                trackChanges: true);
        }

        // All graph commands below this point apply only to Roslyn-owned nodes.
        if (!nodes.All(n => IsRoslynNode(n)))
        {
            yield break;
        }

        // Only show 'Base Types' and 'Derived Types' on a class or interface.
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType) &&
                           IsAnyTypeKind(n, TypeKind.Class, TypeKind.Interface, TypeKind.Struct, TypeKind.Enum, TypeKind.Delegate)))
        {
            yield return new GraphCommand(
                GraphCommandDefinition.BaseTypes,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.InheritsFrom },
                trackChanges: true);

            yield return new GraphCommand(
                GraphCommandDefinition.DerivedTypes,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.InheritsFrom },
                trackChanges: true);
        }

        // Only show 'Calls' on an applicable member in a class or struct
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field)))
        {
            yield return new GraphCommand(
                GraphCommandDefinition.Calls,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.Calls },
                trackChanges: true);
        }

        // Only show 'Is Called By' on an applicable member in a class or struct
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
                           IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct)))
        {
            yield return new GraphCommand(
                GraphCommandDefinition.IsCalledBy,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.Calls },
                trackChanges: true);
        }

        // Show 'Is Used By'
        yield return new GraphCommand(
            GraphCommandDefinition.IsUsedBy,
            targetCategories: new[] { CodeNodeCategories.SourceLocation },
            linkCategories: new[] { CodeLinkCategories.SourceReferences },
            trackChanges: true);

        // Show 'Implements' on a class or struct, or an applicable member in a class or struct.
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType) &&
                           IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct)))
        {
            yield return new GraphCommand(
                s_implementsCommandDefinition,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.Implements },
                trackChanges: true);
        }

        // Show 'Implements' on public, non-static members of a class or struct.  Note: we should
        // also show it on explicit interface impls in C#.
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
                           IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct) &&
                           !GetModifiers(n).IsStatic))
        {
            if (nodes.Any(n => CheckAccessibility(n, Accessibility.Public) ||
                               HasExplicitInterfaces(n)))
            {
                yield return new GraphCommand(
                    s_implementsCommandDefinition,
                    targetCategories: null,
                    linkCategories: new[] { CodeLinkCategories.Implements },
                    trackChanges: true);
            }
        }

        // Show 'Implemented By' on an interface.
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType) &&
                           IsAnyTypeKind(n, TypeKind.Interface)))
        {
            yield return new GraphCommand(
                s_implementedByCommandDefinition,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.Implements },
                trackChanges: true);
        }

        // Show 'Implemented By' on any member of an interface.
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
                           IsAnyTypeKind(n, TypeKind.Interface)))
        {
            yield return new GraphCommand(
                s_implementedByCommandDefinition,
                targetCategories: null,
                linkCategories: new[] { CodeLinkCategories.Implements },
                trackChanges: true);
        }

        // Show 'Overrides' on any applicable member of a class or struct
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
                           IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct) &&
                           GetModifiers(n).IsOverride))
        {
            yield return new GraphCommand(
                s_overridesCommandDefinition,
                targetCategories: null,
                linkCategories: new[] { RoslynGraphCategories.Overrides },
                trackChanges: true);
        }

        // Show 'Overridden By' on any applicable member of a class or struct
        if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
                           IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct) &&
                           IsOverridable(n)))
        {
            yield return new GraphCommand(
                s_overriddenByCommandDefinition,
                targetCategories: null,
                linkCategories: new[] { RoslynGraphCategories.Overrides },
                trackChanges: true);
        }
    }

    private static bool IsOverridable(GraphNode node)
    {
        var modifiers = GetModifiers(node);
        return (modifiers.IsVirtual || modifiers.IsAbstract || modifiers.IsOverride) &&
            !modifiers.IsSealed;
    }

    private static DeclarationModifiers GetModifiers(GraphNode node)
        => (DeclarationModifiers)node[RoslynGraphProperties.SymbolModifiers];

    private static bool CheckAccessibility(GraphNode node, Accessibility accessibility)
        => node[RoslynGraphProperties.DeclaredAccessibility].Equals(accessibility);

    private static bool HasExplicitInterfaces(GraphNode node)
        => ((IList<SymbolKey>)node[RoslynGraphProperties.ExplicitInterfaceImplementations]).Count > 0;

    private static bool IsRoslynNode(GraphNode node)
    {
        return node[RoslynGraphProperties.SymbolKind] != null
            && node[RoslynGraphProperties.TypeKind] != null;
    }

    private static bool IsAnySymbolKind(GraphNode node, params SymbolKind[] symbolKinds)
        => symbolKinds.Any(k => k.Equals(node[RoslynGraphProperties.SymbolKind]));

    private static bool IsAnyTypeKind(GraphNode node, params TypeKind[] typeKinds)
        => typeKinds.Any(k => node[RoslynGraphProperties.TypeKind].Equals(k));

    private static readonly GraphCommandDefinition s_overridesCommandDefinition =
        new("Overrides", ServicesVSResources.Overrides_, GraphContextDirection.Target, 700);

    private static readonly GraphCommandDefinition s_overriddenByCommandDefinition =
        new("OverriddenBy", ServicesVSResources.Overridden_By, GraphContextDirection.Source, 700);

    private static readonly GraphCommandDefinition s_implementsCommandDefinition =
        new("Implements", ServicesVSResources.Implements_, GraphContextDirection.Target, 600);

    private static readonly GraphCommandDefinition s_implementedByCommandDefinition =
        new("ImplementedBy", ServicesVSResources.Implemented_By, GraphContextDirection.Source, 600);

    public T? GetExtension<T>(GraphObject graphObject, T previous) where T : class
    {
        if (graphObject is GraphNode graphNode)
        {
            // If this is not a Roslyn node, bail out.
            if (graphNode.GetValue(RoslynGraphProperties.ContextProjectId) == null)
                return null;

            // Has to have at least a symbolid, or source location to navigate to.
            if (graphNode.GetValue<SymbolKey?>(RoslynGraphProperties.SymbolId) == null &&
                graphNode.GetValue<SourceLocation>(CodeNodeProperties.SourceLocation).FileName == null)
            {
                return null;
            }

            if (typeof(T) == typeof(IGraphNavigateToItem))
                return new GraphNavigatorExtension(_threadingContext, _workspace, _streamingPresenter) as T;

            if (typeof(T) == typeof(IGraphFormattedLabel))
                return new GraphFormattedLabelExtension() as T;
        }

        return null;
    }

    public Graph? Schema
    {
        get { return null; }
    }
}
