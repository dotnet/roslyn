// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioWorkspaceImpl
{
    private sealed class AddMetadataReferenceUndoUnit : AbstractAddRemoveUndoUnit
    {
        private readonly string _filePath;

        public AddMetadataReferenceUndoUnit(
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
            if (fromProject != null)
            {
                var reference = fromProject.MetadataReferences.OfType<PortableExecutableReference>()
                                           .FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.FilePath, _filePath));

                if (reference == null)
                {
                    var documentationProvider = Workspace.Services.GetRequiredService<IDocumentationProviderService>();
                    try
                    {
                        reference = MetadataReference.CreateFromFile(_filePath, documentation: documentationProvider.GetDocumentationProvider(_filePath));
                    }
                    catch (IOException)
                    {
                        return;
                    }

                    var updatedProject = fromProject.AddMetadataReference(reference);
                    Workspace.TryApplyChanges(updatedProject.Solution);
                }
            }
        }

        public override void GetDescription(out string pBstr)
        {
            pBstr = string.Format(FeaturesResources.Add_reference_to_0,
                Path.GetFileName(_filePath));
        }
    }
}
