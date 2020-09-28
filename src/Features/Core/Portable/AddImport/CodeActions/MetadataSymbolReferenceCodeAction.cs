// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private class MetadataSymbolReferenceCodeAction : SymbolReferenceCodeAction
        {
            public MetadataSymbolReferenceCodeAction(Document originalDocument, AddImportFixData fixData)
                : base(originalDocument, fixData)
            {
                Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.MetadataSymbol);
            }

            protected override Task<Project> UpdateProjectAsync(Project project, bool isPreview, CancellationToken cancellationToken)
            {
                var projectWithReference = project.Solution.GetProject(FixData.PortableExecutableReferenceProjectId);
                var reference = projectWithReference.MetadataReferences
                                                    .OfType<PortableExecutableReference>()
                                                    .First(pe => pe.FilePath == FixData.PortableExecutableReferenceFilePathToAdd);

                return Task.FromResult(project.AddMetadataReference(reference));
            }
        }
    }
}
