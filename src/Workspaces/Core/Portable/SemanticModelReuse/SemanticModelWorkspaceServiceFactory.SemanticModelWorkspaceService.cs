// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    internal readonly struct SemanticModelReuseInfo
    {
        /// <summary>
        /// The original *non-speculative* semantic model we retrieved for this document at some point.
        /// </summary>
        public readonly SemanticModel PreviousNonSpeculativeSemanticModel;

        /// <summary>
        /// The current semantic model we retrieved <see cref="SemanticModel"/> for the <see cref="BodyNode"/>.  Could
        /// be speculative or non-speculative.
        /// </summary>
        public readonly SemanticModel CurrentSemanticModel;

        /// <summary>
        /// The current method body we retrieved the <see cref="CurrentSemanticModel"/> for.
        /// </summary>
        public readonly SyntaxNode BodyNode;

        /// <summary>
        /// The top level version of the project when we retrieved <see cref="SemanticModel"/>.  As long as this is the
        /// same we can continue getting speculative models to use.
        /// </summary>
        public readonly VersionStamp TopLevelSementicVersion;

        public SemanticModelReuseInfo(SemanticModel previousNonSpeculativeSemanticModel, SemanticModel currentSemanticModel, SyntaxNode bodyNode, VersionStamp topLevelSementicVersion)
        {
            PreviousNonSpeculativeSemanticModel = previousNonSpeculativeSemanticModel;
            CurrentSemanticModel = currentSemanticModel;
            BodyNode = bodyNode;
            TopLevelSementicVersion = topLevelSementicVersion;
        }
    }

    internal partial class SemanticModelReuseWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private sealed class SemanticModelReuseWorkspaceService : ISemanticModelReuseWorkspaceService
        {
            /// <summary>
            /// A mapping from a document id to the last semantic model we produced for it, along with the top level
            /// semantic version that that semantic model corresponds to.  We can continue reusing the semantic model as
            /// long as no top level changes occur.
            /// <para/>
            /// In general this dictionary will only contain a single key-value pair.  However, in the case of linked
            /// documents, there will be a key-value pair for each of the independent document links that a document
            /// has.
            /// </summary>
            private ImmutableDictionary<DocumentId, SemanticModelReuseInfo?> _semanticModelMap = ImmutableDictionary<DocumentId, SemanticModelReuseInfo?>.Empty;

            public async Task<SemanticModel> ReuseExistingSpeculativeModelAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
            {
                var reuseService = document.GetRequiredLanguageService<ISemanticModelReuseLanguageService>();

                // See if we're asking about a node actually in a method body.  If so, see if we can reuse the
                // existing semantic model.  If not, return the current semantic model for the file.
                var bodyNode = reuseService.TryGetContainingMethodBodyForSpeculation(node);
                if (bodyNode == null)
                    return await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // We were in a method body. Compute the updated map that will contain the appropriate semantic model
                // for this document.
                var originalMap = _semanticModelMap;

                // If we already have a cached *real* semantic model for this body, then just provide that. Note: this
                // is also a requirement as you cannot speculate on a semantic model using a node from that same
                // semantic model.
                if (originalMap.TryGetValue(document.Id, out var reuseInfoOpt) &&
                    reuseInfoOpt.HasValue &&
                    reuseInfoOpt.Value.PreviousNonSpeculativeSemanticModel.SyntaxTree == bodyNode.SyntaxTree)
                {
                    return reuseInfoOpt.Value.PreviousNonSpeculativeSemanticModel;
                }

                var updatedMap = await ComputeUpdatedMapAsync(originalMap, document, bodyNode, cancellationToken).ConfigureAwait(false);

                // Grab the resultant semantic model and then overwrite the existing map.
                var info = updatedMap[document.Id]!.Value;
                var semanticModel = info.CurrentSemanticModel;
                Interlocked.CompareExchange(ref _semanticModelMap, updatedMap, originalMap);

                return semanticModel;
            }

            private static async Task<ImmutableDictionary<DocumentId, SemanticModelReuseInfo?>> ComputeUpdatedMapAsync(
                ImmutableDictionary<DocumentId, SemanticModelReuseInfo?> map, Document document, SyntaxNode bodyNode, CancellationToken cancellationToken)
            {
                var linkedIds = document.GetLinkedDocumentIds();

                // Get the current top level version for this document's project.  If it has changed, then we cannot
                // reuse any existing cached data for it.  This also ensures that we can do things like find the same
                // method body node prior to an edit just by counting it's top-level index in the file.
                var topLevelSemanticVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                // If we are able to reuse a semantic model, then ensure that this is now the semantic model we're now
                // pointing at for this document.
                var reuseInfo = await TryReuseCachedSemanticModelAsync(
                    map, document, bodyNode, linkedIds, topLevelSemanticVersion, cancellationToken).ConfigureAwait(false);
                if (reuseInfo != null)
                    return map.SetItem(document.Id, reuseInfo.Value);

                // Otherwise, we couldn't reuse anything from the cache.  Return a fresh map with just the real semantic
                // model value in it.
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var builder = ImmutableDictionary.CreateBuilder<DocumentId, SemanticModelReuseInfo?>();

                // Note: we are intentionally storing the semanticmodel instance in the speculative location as well.
                builder.Add(document.Id, new SemanticModelReuseInfo(semanticModel, semanticModel, bodyNode, topLevelSemanticVersion));

                foreach (var linkedId in linkedIds)
                    builder.Add(linkedId, null);

                return builder.ToImmutable();
            }

            private static async Task<SemanticModelReuseInfo?> TryReuseCachedSemanticModelAsync(
                ImmutableDictionary<DocumentId, SemanticModelReuseInfo?> map,
                Document document,
                SyntaxNode bodyNode,
                ImmutableArray<DocumentId> linkedIds,
                VersionStamp topLevelSemanticVersion,
                CancellationToken cancellationToken)
            {
                // Get the clique (i.e. all the ids for this and the other documents it is linked to) corresponds to
                // matches the clique we're caching.
                using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentIdClique);
                documentIdClique.Add(document.Id);
                documentIdClique.AddRange(linkedIds);

                // if this is asking about a different doc or clique, we can't reuse anything.
                if (!map.Keys.SetEquals(documentIdClique))
                    return null;

                // see if this doc matches the docs we're caching information for.
                if (!map.TryGetValue(document.Id, out var reuseInfoOpt) || !reuseInfoOpt.HasValue)
                    return null;

                var reuseInfo = reuseInfoOpt.Value;

                // can only reuse the cache if nothing top level changed.
                if (reuseInfo.TopLevelSementicVersion != topLevelSemanticVersion)
                    return null;

                // If multiple callers are asking for the exact same body, they can share the exact same semantic model.
                // This is valuable when several clients (like completion providers) get called at the same time on the
                // same method body edit.
                if (reuseInfo.BodyNode == bodyNode)
                    return reuseInfo;

                var reuseService = document.GetRequiredLanguageService<ISemanticModelReuseLanguageService>();
                var semanticModel = await reuseService.TryGetSpeculativeSemanticModelAsync(reuseInfo.PreviousNonSpeculativeSemanticModel, bodyNode, cancellationToken).ConfigureAwait(false);
                if (semanticModel == null)
                    return null;

                return new SemanticModelReuseInfo(reuseInfo.PreviousNonSpeculativeSemanticModel, semanticModel, bodyNode, topLevelSemanticVersion);
            }
        }
    }
}
