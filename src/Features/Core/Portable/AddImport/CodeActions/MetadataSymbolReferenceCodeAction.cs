// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class MetadataSymbolReferenceCodeAction : SymbolReferenceCodeAction
        {
            /// <summary>
            /// If we're adding <see cref="_portableExecutableReferenceFilePathToAdd"/> then this
            /// is the id for the <see cref="Project"/> we can find that <see cref="PortableExecutableReference"/>
            /// referenced from.
            /// </summary>
            private readonly ProjectId _portableExecutableReferenceProjectId;

            /// <summary>
            /// If we want to add a <see cref="PortableExecutableReference"/> metadata reference, this 
            /// is the <see cref="PortableExecutableReference.FilePath"/> for it.
            /// </summary>
            private readonly string _portableExecutableReferenceFilePathToAdd;

            public MetadataSymbolReferenceCodeAction(
                Document contextDocument,
                ImmutableArray<TextChange> textChanges,
                string title, ImmutableArray<string> tags,
                CodeActionPriority priority,
                ProjectId portableExecutableReferenceProjectId,
                string portableExecutableReferenceFilePathToAdd)
                    : base(contextDocument, textChanges, title, tags, priority)
            {
                _portableExecutableReferenceProjectId = portableExecutableReferenceProjectId;
                _portableExecutableReferenceFilePathToAdd = portableExecutableReferenceFilePathToAdd;
            }

            protected override Project UpdateProject(Project project)
            {
                var projectWithReference = project.Solution.GetProject(_portableExecutableReferenceProjectId);
                var reference = projectWithReference.MetadataReferences
                                                    .OfType<PortableExecutableReference>()
                                                    .First(pe => pe.FilePath == _portableExecutableReferenceFilePathToAdd);

                return project.AddMetadataReference(reference);
            }
        }
    }
}