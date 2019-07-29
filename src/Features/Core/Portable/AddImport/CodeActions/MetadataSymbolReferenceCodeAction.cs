// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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

            protected override Project UpdateProject(Project project)
            {
                var projectWithReference = project.Solution.GetProject(FixData.PortableExecutableReferenceProjectId);
                var reference = projectWithReference.MetadataReferences
                                                    .OfType<PortableExecutableReference>()
                                                    .First(pe => pe.FilePath == FixData.PortableExecutableReferenceFilePathToAdd);

                return project.AddMetadataReference(reference);
            }
        }
    }
}
