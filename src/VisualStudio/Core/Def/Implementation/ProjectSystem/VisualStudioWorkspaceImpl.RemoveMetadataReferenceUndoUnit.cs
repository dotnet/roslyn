// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private class RemoveMetadataReferenceUndoUnit : AbstractAddRemoveUndoUnit
        {
            private readonly string _filePath;

            public RemoveMetadataReferenceUndoUnit(
                VisualStudioWorkspaceImpl workspace,
                ProjectId fromProjectId,
                string filePath)
                : base(workspace, fromProjectId)
            {
                _filePath = filePath;
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                var currentSolution = Workspace.CurrentSolution;
                var fromProject = currentSolution.GetProject(FromProjectId);
                var reference = fromProject?.MetadataReferences.OfType<PortableExecutableReference>()
                                            .FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.FilePath, _filePath));

                if (reference != null)
                {
                    var updatedProject = fromProject.RemoveMetadataReference(reference);
                    Workspace.TryApplyChanges(updatedProject.Solution);
                }
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(FeaturesResources.Remove_reference_to_0,
                    Path.GetFileName(_filePath));
            }
        }
    }
}
