// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISolutionExtensions
    {
        public static async Task<IEnumerable<INamespaceSymbol>> GetGlobalNamespacesAsync(
            this Solution solution,
            CancellationToken cancellationToken)
        {
            var results = new List<INamespaceSymbol>();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                results.Add(compilation.Assembly.GlobalNamespace);
            }

            return results;
        }

        public static IEnumerable<DocumentId> GetChangedDocuments(this Solution newSolution, Solution oldSolution)
        {
            if (newSolution != null)
            {
                var solutionChanges = newSolution.GetChanges(oldSolution);

                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    foreach (var documentId in projectChanges.GetChangedDocuments())
                    {
                        yield return documentId;
                    }
                }
            }
        }
    }
}