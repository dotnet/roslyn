// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [ExportWorkspaceService(typeof(IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    public class TestAddMetadataReferenceCodeActionOperationFactoryWorkspaceService : IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestAddMetadataReferenceCodeActionOperationFactoryWorkspaceService()
        {
        }

        public CodeActionOperation CreateAddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
            => new Operation(projectId, assemblyIdentity);

        public class Operation : CodeActionOperation
        {
            public readonly ProjectId ProjectId;
            public readonly AssemblyIdentity AssemblyIdentity;

            public Operation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
            {
                this.ProjectId = projectId;
                this.AssemblyIdentity = assemblyIdentity;
            }
        }
    }
}
