// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private class MetadataSymbolReferenceCodeAction : SymbolReferenceCodeAction
    {
        /// <summary>
        /// This code action only works by adding a reference to a metadata dll.  As such, it requires a non
        /// document change (and is thus restricted in which hosts it can run).
        /// </summary>
        public MetadataSymbolReferenceCodeAction(Document originalDocument, AddImportFixData fixData)
            : base(originalDocument, fixData, RequiresNonDocumentChangeTags)
        {
            Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.MetadataSymbol);
        }

        protected override Task<CodeActionOperation?> UpdateProjectAsync(Project project, bool isPreview, CancellationToken cancellationToken)
        {
            var projectWithReference = project.Solution.GetRequiredProject(FixData.PortableExecutableReferenceProjectId);
            var reference = projectWithReference.MetadataReferences
                                                .OfType<PortableExecutableReference>()
                                                .First(pe => pe.FilePath == FixData.PortableExecutableReferenceFilePathToAdd);

            return Task.FromResult<CodeActionOperation?>(new ApplyChangesOperation(project.AddMetadataReference(reference).Solution));
        }
    }
}
