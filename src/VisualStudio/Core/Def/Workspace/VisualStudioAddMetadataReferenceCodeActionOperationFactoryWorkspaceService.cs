// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[ExportWorkspaceService(typeof(IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioAddMetadataReferenceCodeActionOperationFactoryWorkspaceService : IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioAddMetadataReferenceCodeActionOperationFactoryWorkspaceService()
    {
    }

    public CodeActionOperation CreateAddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (assemblyIdentity == null)
        {
            throw new ArgumentNullException(nameof(assemblyIdentity));
        }

        return new AddMetadataReferenceOperation(projectId, assemblyIdentity);
    }

    private class AddMetadataReferenceOperation : CodeActionOperation
    {
        private readonly AssemblyIdentity _assemblyIdentity;
        private readonly ProjectId _projectId;

        public AddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
        {
            _projectId = projectId;
            _assemblyIdentity = assemblyIdentity;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken = default)
        {
            var visualStudioWorkspace = (VisualStudioWorkspaceImpl)workspace;
            if (!visualStudioWorkspace.TryAddReferenceToProject(_projectId, "*" + _assemblyIdentity.GetDisplayName()))
            {
                // We failed to add the reference, which means the project system wasn't able to bind.
                // We'll pop up the Add Reference dialog to let the user figure this out themselves.
                // This is the same approach done in CVBErrorFixApply::ApplyAddMetaReferenceFix

                if (visualStudioWorkspace.GetHierarchy(_projectId) is IVsUIHierarchy uiHierarchy)
                {
                    var command = new OLECMD[1];
                    command[0].cmdID = (uint)VSConstants.VSStd2KCmdID.ADDREFERENCE;

                    if (ErrorHandler.Succeeded(uiHierarchy.QueryStatusCommand((uint)VSConstants.VSITEMID.Root, VSConstants.VSStd2K, 1, command, IntPtr.Zero)))
                    {
                        if ((((OLECMDF)command[0].cmdf) & OLECMDF.OLECMDF_ENABLED) != 0)
                        {
                            uiHierarchy.ExecCommand((uint)VSConstants.VSITEMID.Root, VSConstants.VSStd2K, (uint)VSConstants.VSStd2KCmdID.ADDREFERENCE, 0, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                }
            }
        }

        public override string Title
            => string.Format(ServicesVSResources.Add_a_reference_to_0, _assemblyIdentity.GetDisplayName());
    }
}
