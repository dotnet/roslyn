// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

[Export(typeof(CallHierarchyProvider))]
internal sealed partial class CallHierarchyProvider
{
    public readonly IAsynchronousOperationListener AsyncListener;
    public readonly IUIThreadOperationExecutor ThreadOperationExecutor;
    private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

    public IThreadingContext ThreadingContext { get; }
    public IGlyphService GlyphService { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyProvider(
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor threadOperationExecutor,
        IAsynchronousOperationListenerProvider listenerProvider,
        IGlyphService glyphService,
        Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
    {
        AsyncListener = listenerProvider.GetListener(FeatureAttribute.CallHierarchy);
        ThreadingContext = threadingContext;
        ThreadOperationExecutor = threadOperationExecutor;
        GlyphService = glyphService;
        _streamingPresenter = streamingPresenter;
    }

    public async Task<CallHierarchyItem?> CreateItemAsync(
        ISymbol symbol, Project project, ImmutableArray<Location> callsites, CancellationToken cancellationToken)
    {
        var service = project.GetRequiredLanguageService<ICallHierarchyService>();
        var descriptor = await service.CreateItemAsync(symbol, project, cancellationToken).ConfigureAwait(false);
        return descriptor != null
            ? await CreateItemAsync(descriptor, project.Solution.Workspace, callsites, cancellationToken).ConfigureAwait(false)
            : null;
    }

    public async Task<CallHierarchyItem?> CreateItemAsync(
        CallHierarchyItemDescriptor descriptor, Workspace workspace, ImmutableArray<Location> callsites, CancellationToken cancellationToken)
    {
        var resolved = await descriptor.ItemId.TryResolveAsync(workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
        if (resolved == null)
            return null;

        var (symbol, project) = resolved.Value;
        var location = await GoToDefinitionHelpers.GetDefinitionLocationAsync(
            symbol, project.Solution, this.ThreadingContext, _streamingPresenter.Value, cancellationToken).ConfigureAwait(false);
        return new CallHierarchyItem(
            this,
            descriptor,
            location,
            await CreateSearchCategoryEntriesAsync(descriptor, symbol, workspace.CurrentSolution, cancellationToken).ConfigureAwait(false),
            () => descriptor.Glyph.GetImageSource(GlyphService),
            symbol.ToDisplayString(),
            project.Name,
            callsites,
            project);
    }

    public FieldInitializerItem CreateInitializerItem(IEnumerable<CallHierarchyDetail> details)
    {
        return new FieldInitializerItem(EditorFeaturesResources.Initializers,
                                        "__" + EditorFeaturesResources.Initializers,
                                        Glyph.FieldPublic.GetImageSource(GlyphService),
                                        details);
    }

    public async Task<ImmutableArray<CallHierarchySearchResult>> SearchAsync(
        Workspace workspace,
        CallHierarchySearchDescriptor searchDescriptor,
        CallHierarchySearchScope searchScope,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken)
    {
        var project = workspace.CurrentSolution.GetProject(searchDescriptor.ItemId.ProjectId);
        if (project == null)
            return [];

        documents ??= IncludeDocuments(searchScope, project);
        var service = project.GetRequiredLanguageService<ICallHierarchyService>();
        return await service.SearchIncomingCallsAsync(workspace.CurrentSolution, searchDescriptor, documents, cancellationToken).ConfigureAwait(false);
    }

    private static IImmutableSet<Document>? IncludeDocuments(CallHierarchySearchScope scope, Project project)
    {
        if (scope is CallHierarchySearchScope.CurrentDocument or CallHierarchySearchScope.CurrentProject)
        {
            var documentTrackingService = project.Solution.Services.GetRequiredService<IDocumentTrackingService>();
            var activeDocument = documentTrackingService.TryGetActiveDocument();
            if (activeDocument != null)
            {
                if (scope == CallHierarchySearchScope.CurrentProject)
                {
                    var currentProject = project.Solution.GetProject(activeDocument.ProjectId);
                    if (currentProject != null)
                        return ImmutableHashSet.CreateRange(currentProject.Documents);
                }
                else
                {
                    var currentDocument = project.Solution.GetDocument(activeDocument);
                    if (currentDocument != null)
                        return ImmutableHashSet.Create(currentDocument);
                }

                return ImmutableHashSet<Document>.Empty;
            }
        }

        return null;
    }

    private static async Task<ImmutableArray<CallHierarchySearchCategoryEntry>> CreateSearchCategoryEntriesAsync(
        CallHierarchyItemDescriptor descriptor,
        ISymbol symbol,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<CallHierarchySearchCategoryEntry>(descriptor.SupportedSearchDescriptors.Length);
        foreach (var searchDescriptor in descriptor.SupportedSearchDescriptors)
        {
            builder.Add(await CreateSearchCategoryEntryAsync(searchDescriptor, symbol, solution, cancellationToken).ConfigureAwait(false));
        }

        return builder.MoveToImmutable();
    }

    private static async Task<CallHierarchySearchCategoryEntry> CreateSearchCategoryEntryAsync(
        CallHierarchySearchDescriptor descriptor,
        ISymbol symbol,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var relatedSymbol = descriptor.Relationship switch
        {
            CallHierarchyRelationshipKind.BaseMember or CallHierarchyRelationshipKind.InterfaceImplementations
                => (await descriptor.ItemId.TryResolveAsync(solution, cancellationToken).ConfigureAwait(false))?.Symbol,
            _ => null,
        };

        var displayName = descriptor.Relationship switch
        {
            CallHierarchyRelationshipKind.Callers => string.Format(EditorFeaturesResources.Calls_To_0, symbol.Name),
            CallHierarchyRelationshipKind.CallsToOverrides => EditorFeaturesResources.Calls_To_Overrides,
            CallHierarchyRelationshipKind.BaseMember => string.Format(EditorFeaturesResources.Calls_To_Base_Member_0, relatedSymbol?.ToDisplayString() ?? symbol.ToDisplayString()),
            CallHierarchyRelationshipKind.InterfaceImplementations => string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, relatedSymbol?.ToDisplayString() ?? symbol.ToDisplayString()),
            CallHierarchyRelationshipKind.Implementations => string.Format(EditorFeaturesResources.Implements_0, symbol.Name),
            CallHierarchyRelationshipKind.Overrides => EditorFeaturesResources.Overrides_,
            CallHierarchyRelationshipKind.FieldReferences => string.Format(EditorFeaturesResources.References_To_Field_0, symbol.Name),
            _ => throw new InvalidOperationException(),
        };

        var searchCategory = descriptor.Relationship switch
        {
            CallHierarchyRelationshipKind.Callers => CallHierarchyPredefinedSearchCategoryNames.Callers,
            CallHierarchyRelationshipKind.InterfaceImplementations => CallHierarchyPredefinedSearchCategoryNames.InterfaceImplementations,
            CallHierarchyRelationshipKind.Overrides => CallHierarchyPredefinedSearchCategoryNames.Overrides,
            _ => displayName,
        };

        return new CallHierarchySearchCategoryEntry(descriptor, searchCategory, displayName);
    }
}
