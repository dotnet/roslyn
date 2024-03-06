// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioWorkspaceImpl
{
    private abstract class AbstractAddRemoveUndoUnit : IOleUndoUnit
    {
        protected readonly ProjectId FromProjectId;
        protected readonly VisualStudioWorkspaceImpl Workspace;

        protected AbstractAddRemoveUndoUnit(
            VisualStudioWorkspaceImpl workspace,
            ProjectId fromProjectId)
        {
            Workspace = workspace;
            FromProjectId = fromProjectId;
        }

        public abstract void Do(IOleUndoManager pUndoManager);
        public abstract void GetDescription(out string pBstr);

        public void GetUnitType(out Guid pClsid, out int plID)
            => throw new NotImplementedException();

        public void OnNextAdd()
        {
        }
    }
}
