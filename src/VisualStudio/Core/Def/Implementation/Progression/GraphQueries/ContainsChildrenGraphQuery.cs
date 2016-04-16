// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class ContainsChildrenGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    var symbol = graphBuilder.GetSymbol(node);

                    if (symbol != null)
                    {
                        bool containsChildren = SymbolContainment.GetContainedSymbols(symbol).Any();
                        graphBuilder.AddDeferredPropertySet(node, DgmlNodeProperties.ContainsChildren, containsChildren);
                    }
                    else if (node.HasCategory(CodeNodeCategories.File))
                    {
                        var document = graphBuilder.GetContextDocument(node);

                        if (document != null)
                        {
                            var childNodes = await SymbolContainment.GetContainedSyntaxNodesAsync(document, cancellationToken).ConfigureAwait(false);
                            graphBuilder.AddDeferredPropertySet(node, DgmlNodeProperties.ContainsChildren, childNodes.Any());
                        }
                        else
                        {
                            var uri = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);

                            if (uri != null)
                            {
                                // Since a solution load is not yet completed, there is no document available to answer this query.
                                // The solution explorer presumes that if somebody doesn't answer for a file, they never will. 
                                // See Providers\GraphContextAttachedCollectionSource.cs for more. Therefore we should answer by setting
                                // ContainsChildren property to either true or false, so any following updates will be tractable.
                                // We will set it to false since the solution explorer assumes the default for this query response is 'false'.

                                // Todo: we may need fallback to check if this node actually represents a C# or VB language 
                                // even when its extension fails to say so. One option would be to call DTEWrapper.IsRegisteredForLangService,
                                // which may not be called here however since deadlock could happen.

                                // The Uri returned by `GetNestedValueByName()` above isn't necessarily absolute and the `OriginalString` is
                                // the only property that doesn't throw if the UriKind is relative, so `OriginalString` must be used instead
                                // of `AbsolutePath`.
                                string ext = Path.GetExtension(uri.OriginalString);
                                if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase) || ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
                                {
                                    graphBuilder.AddDeferredPropertySet(node, DgmlNodeProperties.ContainsChildren, false);
                                }
                            }
                        }
                    }
                }
            }

            return graphBuilder;
        }
    }
}
