// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal readonly struct SemanticModelReuseInfo(SemanticModel previousNonSpeculativeSemanticModel, SemanticModel currentSemanticModel, SyntaxNode bodyNode, VersionStamp topLevelSementicVersion)
    {
        /// <summary>
        /// The original <em>non-speculative</em> semantic model we retrieved for this document at some point.
        /// </summary>
        public readonly SemanticModel PreviousNonSpeculativeSemanticModel = previousNonSpeculativeSemanticModel;

        /// <summary>
        /// The current semantic model we retrieved <see cref="SemanticModel"/> for the <see cref="BodyNode"/>.  Could
        /// be speculative or non-speculative.
        /// </summary>
        public readonly SemanticModel CurrentSemanticModel = currentSemanticModel;

        /// <summary>
        /// The current method body we retrieved the <see cref="CurrentSemanticModel"/> for.
        /// </summary>
        public readonly SyntaxNode BodyNode = bodyNode;

        /// <summary>
        /// The top level version of the project when we retrieved <see cref="SemanticModel"/>.  As long as this is the
        /// same we can continue getting speculative models to use.
        /// </summary>
        public readonly VersionStamp TopLevelSemanticVersion = topLevelSementicVersion;
    }

    internal partial class SemanticModelReuseWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private sealed class SemanticModelReuseWorkspaceService : ISemanticModelReuseWorkspaceService
        {
            private readonly Workspace _workspace;

            /// <summary>
            /// A mapping from a document id to the last semantic model we produced for it, along with the top level
            /// semantic version that that semantic model corresponds to.  We can continue reusing the semantic model as
            /// long as no top level changes occur.
            /// <para>
            /// In general this dictionary will only contain a single key-value pair.  However, in the case of linked
            /// documents, there will be a key-value pair for each of the independent document links that a document
            /// has.
            /// </para>
            /// <para>
            /// A <see langword="null"/> value simply means we haven't cached any information for that particular id.
            /// </para>
            /// </summary>
            private ImmutableDictionary<DocumentId, SemanticModelReuseInfo?> _semanticModelMap = ImmutableDictionary<DocumentId, SemanticModelReuseInfo?>.Empty;

            public SemanticModelReuseWorkspaceService(Workspace workspace)
            {
                _workspace = workspace;
                _workspace.WorkspaceChanged += (_, e) =>
                {
                    // if our map points at documents not in the current solution, then we want to clear things out.
                    // this way we don't hold onto semantic models past, say, the c#/vb solutions closing.
                    var map = _semanticModelMap;
                    if (map.IsEmpty)
                        return;

                    var solution = e.NewSolution;
                    foreach (var (docId, _) in map)
                    {
                        if (!solution.ContainsDocument(docId))
                        {
                            _semanticModelMap = ImmutableDictionary<DocumentId, SemanticModelReuseInfo?>.Empty;
                            return;
                        }
                    }
                };
            }

            public async ValueTask<SemanticModel> ReuseExistingSpeculativeModelAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
            {
                var reuseService = document.GetRequiredLanguageService<ISemanticModelReuseLanguageService>();

                // See if we're asking about a node actually in a method body.  If so, see if we can reuse the
                // existing semantic model.  If not, return the current semantic model for the file.
                var bodyNode = reuseService.TryGetContainingMethodBodyForSpeculation(node);
                if (bodyNode == null)
                    return await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // We were in a method body. Compute the updated map that will contain the appropriate semantic model
                // for this document.
                //
                // In terms of concurrency we the map so that we can operate on it independently of other threads.  When
                // we compute the final map, we'll grab the semantic model out of it to return (which must be correct
                // since we're the thread that created that map).  Then, we overwrite the instance map with our final
                // map. This map may be stomped on by another thread, but that's fine.  We don't have any sort of
                // ordering requirement. We just want someone to win and place the new map so that it's there for the
                // next caller (which is likely to use the same body node).
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

                // Grab the resultant semantic model and then overwrite the existing map.  We return the semantic model
                // from the map *we* computed so that we're isolated from other threads writing to the map stored in the
                // field.
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
                    map, document, bodyNode, topLevelSemanticVersion, cancellationToken).ConfigureAwait(false);
                if (reuseInfo != null)
                    return map.SetItem(document.Id, reuseInfo.Value);

                // Otherwise, we couldn't reuse that doc's cached info.  Create a fresh map with that doc's real
                // semantic model value in it.  Note: we still reuse the values stored with the other links for that
                // doc as they may still be valid to use.
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var builder = ImmutableDictionary.CreateBuilder<DocumentId, SemanticModelReuseInfo?>();

                // Note: we are intentionally storing the semantic model instance in the speculative location as well.
                builder.Add(document.Id, new SemanticModelReuseInfo(semanticModel, semanticModel, bodyNode, topLevelSemanticVersion));

                foreach (var linkedId in linkedIds)
                {
                    // Reuse the existing cached data for any links we have as well
                    var linkedReuseInfo = map.TryGetValue(linkedId, out var info) ? info : null;
                    builder.Add(linkedId, linkedReuseInfo);
                }

                return builder.ToImmutable();
            }

            private static async Task<SemanticModelReuseInfo?> TryReuseCachedSemanticModelAsync(
                ImmutableDictionary<DocumentId, SemanticModelReuseInfo?> map,
                Document document,
                SyntaxNode bodyNode,
                VersionStamp topLevelSemanticVersion,
                CancellationToken cancellationToken)
            {
                // if this is asking about a doc we don't know about, we can't reuse anything.
                if (!map.ContainsKey(document.Id))
                    return null;

                // see if this doc matches the docs we're caching information for.
                if (!map.TryGetValue(document.Id, out var reuseInfoOpt) || !reuseInfoOpt.HasValue)
                    return null;

                var reuseInfo = reuseInfoOpt.Value;

                // can only reuse the cache if nothing top level changed.
                if (reuseInfo.TopLevelSemanticVersion != topLevelSemanticVersion)
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
