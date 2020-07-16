// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DocumentMap = MultiDictionary<Document, (ISymbol symbol, IReferenceFinder finder)>;

    internal partial class FindReferencesSearchEngine
    {
        private async Task ProcessProjectAsync(
            Project project,
            DocumentMap documentMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessProjectAsync, project.Name, _cancellationToken))
            {
                if (project.SupportsCompilation)
                {
                    // make sure we hold onto compilation while we search documents belong to this project
                    var compilation = await project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);

                    var documentTasks = new List<Task>();
                    foreach (var kvp in documentMap)
                    {
                        var document = kvp.Key;

                        if (document.Project == project)
                        {
                            var documentQueue = kvp.Value;

                            documentTasks.Add(Task.Run(() => ProcessDocumentQueueAsync(
                                document, documentQueue), _cancellationToken));
                        }
                    }

                    await Task.WhenAll(documentTasks).ConfigureAwait(false);

                    GC.KeepAlive(compilation);
                }
            }
        }
    }
}
