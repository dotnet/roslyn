﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class IsUsedByGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.GraphQuery_IsUsedBy, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

                foreach (var node in context.InputNodes)
                {
                    var symbol = graphBuilder.GetSymbol(node);
                    var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                    foreach (var reference in references)
                    {
                        var referencedSymbol = reference.Definition;
                        var projectId = graphBuilder.GetContextProject(node).Id;

                        var allLocations = referencedSymbol.Locations.Concat(reference.Locations.Select(r => r.Location))
                                                                     .Where(l => l != null && l.IsInSource);

                        foreach (var location in allLocations)
                        {
                            var locationNode = GetLocationNode(referencedSymbol, location, context, projectId, cancellationToken);
                            graphBuilder.AddLink(node, CodeLinkCategories.SourceReferences, locationNode);
                        }
                    }
                }

                return graphBuilder;
            }
        }

        internal GraphNode GetLocationNode(ISymbol symbol, Location location, IGraphContext context, ProjectId projectId, CancellationToken cancellationToken)
        {
            var span = location.GetLineSpan();
            var lineText = location.SourceTree.GetText(cancellationToken).Lines[span.StartLinePosition.Line].ToString();
            var filePath = location.SourceTree.FilePath;
            var sourceLocation = new SourceLocation(filePath,
                                        new Position(span.StartLinePosition.Line, span.StartLinePosition.Character),
                                        new Position(span.EndLinePosition.Line, span.EndLinePosition.Character));
            var label = string.Format("{0} ({1}, {2}): {3}",
                                        System.IO.Path.GetFileName(filePath),
                                        span.StartLinePosition.Line + 1,
                                        span.StartLinePosition.Character + 1,
                                        lineText.TrimStart());
            var locationNode = context.Graph.Nodes.GetOrCreate(sourceLocation.CreateGraphNodeId(), label, CodeNodeCategories.SourceLocation);
            locationNode[CodeNodeProperties.SourceLocation] = sourceLocation;
            locationNode[RoslynGraphProperties.ContextProjectId] = projectId;
            locationNode[DgmlNodeProperties.Icon] = IconHelper.GetIconName("Reference", Accessibility.NotApplicable);

            return locationNode;
        }
    }
}
