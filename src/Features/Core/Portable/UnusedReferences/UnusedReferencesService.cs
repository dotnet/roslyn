// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    [ExportWorkspaceService(typeof(IUnusedReferencesService)), Shared]
    internal class UnusedReferencesService : IUnusedReferencesService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnusedReferencesService()
        {
        }

        public async Task<ImmutableArray<UnusedReference>> GetUnusedReferencesAsync(Project project, Compilation compilation, CancellationToken cancellationToken)
        {
            var usedReferences = compilation.GetUsedAssemblyReferences(cancellationToken);
            var unusedReferences = compilation.References.Except(usedReferences).ToImmutableArray();

            var projectAssetsDocument = project.AdditionalDocuments.SingleOrDefault(document => document.Name == "project.assets.json");
            if (projectAssetsDocument is null)
            {
                return ImmutableArray<UnusedReference>.Empty;
            }

            var projectAssetsText = await projectAssetsDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);


            return ImmutableArray<UnusedReference>.Empty;
        }

        public Task<Project> UpdateReferencesAsync(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
