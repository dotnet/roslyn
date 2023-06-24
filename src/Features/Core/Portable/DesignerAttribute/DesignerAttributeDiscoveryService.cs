// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    [ExportWorkspaceService(typeof(IDesignerAttributeDiscoveryService)), Shared]
    internal sealed partial class DesignerAttributeDiscoveryService : IDesignerAttributeDiscoveryService
    {
        /// <summary>
        /// Cache from the individual references a project has, to a boolean specifying if reference knows about the
        /// System.ComponentModel.DesignerCategoryAttribute attribute.
        /// </summary>
        private static readonly ConditionalWeakTable<MetadataId, AsyncLazy<bool>> s_metadataIdToDesignerAttributeInfo = new();

        private readonly IAsynchronousOperationListener _listener;

        /// <summary>
        /// Protects mutable state in this type.
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// Keep track of the last information we reported.  We will avoid notifying the host if we recompute and these
        /// don't change.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, (string? category, VersionStamp projectVersion)> _documentToLastReportedInformation = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesignerAttributeDiscoveryService(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.DesignerAttributes);
        }

        private static async ValueTask<bool> HasDesignerCategoryTypeAsync(Project project, CancellationToken cancellationToken)
        {
            var solutionServices = project.Solution.Services;
            var solutionKey = SolutionKey.ToSolutionKey(project.Solution);
            foreach (var reference in project.MetadataReferences)
            {
                if (reference is PortableExecutableReference peReference)
                {
                    if (await HasDesignerCategoryTypeAsync(
                            solutionServices, solutionKey, peReference, cancellationToken).ConfigureAwait(false))
                    {
                        return true;
                    }
                }
            }

            return false;

            static async Task<bool> HasDesignerCategoryTypeAsync(
               SolutionServices solutionServices,
               SolutionKey solutionKey,
               PortableExecutableReference peReference,
               CancellationToken cancellationToken)
            {
                MetadataId metadataId;
                try
                {
                    metadataId = peReference.GetMetadataId();
                }
                catch (Exception ex) when (ex is BadImageFormatException or IOException)
                {
                    return false;
                }

                var asyncLazy = s_metadataIdToDesignerAttributeInfo.GetValue(
                    metadataId, _ => AsyncLazy.Create(cancellationToken =>
                        ComputeHasDesignerCategoryTypeAsync(solutionServices, solutionKey, peReference, cancellationToken)));
                return await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }

            static async Task<bool> ComputeHasDesignerCategoryTypeAsync(
                SolutionServices solutionServices,
                SolutionKey solutionKey,
                PortableExecutableReference peReference,
                CancellationToken cancellationToken)
            {
                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    solutionServices, solutionKey, peReference, checksum: null, cancellationToken).ConfigureAwait(false);
                var result =
                    info.ContainsSymbolWithName(nameof(System)) &&
                    info.ContainsSymbolWithName(nameof(System.ComponentModel)) &&
                    info.ContainsSymbolWithName(nameof(System.ComponentModel.DesignerCategoryAttribute));
                return result;
            }
        }

        public async ValueTask ProcessSolutionAsync(
            Solution solution,
            DocumentId? priorityDocumentId,
            bool useFrozenSnapshots,
            IDesignerAttributeDiscoveryService.ICallback callback,
            CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Remove any documents that are now gone.
                foreach (var docId in _documentToLastReportedInformation.Keys)
                {
                    if (!solution.ContainsDocument(docId))
                        _documentToLastReportedInformation.TryRemove(docId, out _);
                }

                // Handle the priority doc first.
                var priorityDocument = solution.GetDocument(priorityDocumentId);
                if (priorityDocument != null)
                    await ProcessProjectAsync(priorityDocument.Project, priorityDocument, useFrozenSnapshots, callback, cancellationToken).ConfigureAwait(false);

                // Wait a little after the priority document and process the rest at a lower priority.
                await _listener.Delay(DelayTimeSpan.NonFocus, cancellationToken).ConfigureAwait(false);

                // Process the rest of the projects in dependency order so that their data is ready when we hit the 
                // projects that depend on them.
                var dependencyGraph = solution.GetProjectDependencyGraph();
                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    if (projectId != priorityDocumentId?.ProjectId)
                        await ProcessProjectAsync(solution.GetRequiredProject(projectId), specificDocument: null, useFrozenSnapshots, callback, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessProjectAsync(
            Project project,
            Document? specificDocument,
            bool useFrozenSnapshots,
            IDesignerAttributeDiscoveryService.ICallback callback,
            CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return;

            // Defer expensive work until it's actually needed.

            // The top level project version for this project.  We only care if anything top level changes here.
            // Downstream impact will already happen due to us keying off of the references a project has (which will
            // change if anything it depends on changes).
            var lazyProjectVersion = AsyncLazy.Create(project.GetSemanticVersionAsync);

            // Switch to frozen semantics if requested.  We don't need to wait on generators to run here as we want to
            // be lightweight.  We'll also continue running in the future.  So if any changes to happen that are
            // important to pickup, then we'll see it in the future.  But this avoids constant churn here trying to do
            // things like building skeletons.
            if (useFrozenSnapshots)
            {
                var document = specificDocument ?? project.Documents.FirstOrDefault();
                if (document is null)
                    return;

                project = document.WithFrozenPartialSemantics(cancellationToken).Project;
                specificDocument = specificDocument is null ? null : project.GetRequiredDocument(specificDocument.Id);
            }

            await ScanForDesignerCategoryUsageAsync(
                project, specificDocument, callback, lazyProjectVersion, cancellationToken).ConfigureAwait(false);

            // If we scanned just a specific document in the project, now scan the rest of the files.
            if (specificDocument != null)
                await ScanForDesignerCategoryUsageAsync(project, specificDocument: null, callback, lazyProjectVersion, cancellationToken).ConfigureAwait(false);
        }

        private async Task ScanForDesignerCategoryUsageAsync(
            Project project,
            Document? specificDocument,
            IDesignerAttributeDiscoveryService.ICallback callback,
            AsyncLazy<VersionStamp> lazyProjectVersion,
            CancellationToken cancellationToken)
        {
            // Now get all the values that actually changed and notify VS about them. We don't need
            // to tell it about the ones that didn't change since that will have no effect on the
            // user experience.
            var changedData = await ComputeChangedDataAsync(
                project, specificDocument, lazyProjectVersion, cancellationToken).ConfigureAwait(false);

            // Only bother reporting non-empty information to save an unnecessary RPC.
            if (!changedData.IsEmpty)
                await callback.ReportDesignerAttributeDataAsync(changedData.SelectAsArray(d => d.data), cancellationToken).ConfigureAwait(false);

            // Now, keep track of what we've reported to the host so we won't report unchanged files in the future. We
            // do this after the report has gone through as we want to make sure that if it cancels for any reason we
            // don't hold onto values that may not have made it all the way to the project system.
            foreach (var (data, projectVersion) in changedData)
                _documentToLastReportedInformation[data.DocumentId] = (data.Category, projectVersion);
        }

        private async Task<ImmutableArray<(DesignerAttributeData data, VersionStamp version)>> ComputeChangedDataAsync(
            Project project,
            Document? specificDocument,
            AsyncLazy<VersionStamp> lazyProjectVersion,
            CancellationToken cancellationToken)
        {
            // NOTE: While we could potentially process the documents in a project in parallel, we intentionally do not.
            // That's because this runs automatically in the BG in response to *any* change in the workspace.  So it's
            // very often going to be running, and it will be potentially competing against explicitly invoked actions
            // by the user.  Processing only one doc at a time, means we're not saturating the TPL with this work at the
            // expense of other features.

            bool? hasDesignerCategoryType = null;

            using var _ = ArrayBuilder<(DesignerAttributeData data, VersionStamp version)>.GetInstance(out var results);

            // Avoid realizing document instances until needed.
            foreach (var documentId in project.DocumentIds)
            {
                // If we're only analyzing a specific document, then skip the rest.
                if (specificDocument != null && documentId != specificDocument.Id)
                    continue;

                // If we don't have a path for this document, we cant proceed with it.
                // We need that path to inform the project system which file we're referring to.
                var filePath = project.State.DocumentStates.GetRequiredState(documentId).FilePath;
                if (filePath is null)
                    continue;

                // If nothing has changed at the top level between the last time we analyzed this document and now, then
                // no need to analyze again.
                var projectVersion = await lazyProjectVersion.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (_documentToLastReportedInformation.TryGetValue(documentId, out var existingInfo) &&
                    existingInfo.projectVersion == projectVersion)
                {
                    continue;
                }

                hasDesignerCategoryType ??= await HasDesignerCategoryTypeAsync(project, cancellationToken).ConfigureAwait(false);
                var data = await ComputeDesignerAttributeDataAsync(project, documentId, filePath, hasDesignerCategoryType.Value).ConfigureAwait(false);
                if (data.Category != existingInfo.category)
                    results.Add((data, projectVersion));
            }

            return results.ToImmutable();

            async Task<DesignerAttributeData> ComputeDesignerAttributeDataAsync(
                Project project, DocumentId documentId, string filePath, bool hasDesignerCategoryType)
            {
                // We either haven't computed the designer info, or our data was out of date.  We need
                // So recompute here.  Figure out what the current category is, and if that's different
                // from what we previously stored.
                var category = await ComputeDesignerAttributeCategoryAsync(
                    hasDesignerCategoryType, project, documentId, cancellationToken).ConfigureAwait(false);

                return new DesignerAttributeData
                {
                    Category = category,
                    DocumentId = documentId,
                    FilePath = filePath,
                };
            }
        }

        public static async Task<string?> ComputeDesignerAttributeCategoryAsync(
            bool hasDesignerCategoryType, Project project, DocumentId documentId, CancellationToken cancellationToken)
        {
            // simple case.  If there's no DesignerCategory type in this compilation, then there's definitely no
            // designable types.
            if (!hasDesignerCategoryType)
                return null;

            // Wait to realize the document to avoid unnecessary allocations when indexing documents.
            var document = project.GetRequiredDocument(documentId);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Legacy behavior.  We only register the designer info for the first non-nested class
            // in the file.
            var firstClass = FindFirstNonNestedClass(syntaxFacts.GetMembersOfCompilationUnit(root));
            if (firstClass == null)
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var firstClassType = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(firstClass, cancellationToken);

            foreach (var type in firstClassType.GetBaseTypesAndThis())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // See if it has the designer attribute on it. Use symbol-equivalence instead of direct equality
                // as the symbol we have 
                var attribute = type.GetAttributes().FirstOrDefault(d => IsDesignerAttribute(d.AttributeClass));
                if (attribute is { ConstructorArguments: [{ Type.SpecialType: SpecialType.System_String, Value: string stringValue }] })
                    return stringValue.Trim();
            }

            return null;

            static bool IsDesignerAttribute(INamedTypeSymbol? attributeClass)
                => attributeClass is
                {
                    Name: nameof(DesignerCategoryAttribute),
                    ContainingNamespace.Name: nameof(System.ComponentModel),
                    ContainingNamespace.ContainingNamespace.Name: nameof(System),
                    ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
                };

            SyntaxNode? FindFirstNonNestedClass(SyntaxList<SyntaxNode> members)
            {
                foreach (var member in members)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (syntaxFacts.IsBaseNamespaceDeclaration(member))
                    {
                        var firstClass = FindFirstNonNestedClass(syntaxFacts.GetMembersOfBaseNamespaceDeclaration(member));
                        if (firstClass != null)
                            return firstClass;
                    }
                    else if (syntaxFacts.IsClassDeclaration(member))
                    {
                        return member;
                    }
                }

                return null;
            }
        }
    }
}
