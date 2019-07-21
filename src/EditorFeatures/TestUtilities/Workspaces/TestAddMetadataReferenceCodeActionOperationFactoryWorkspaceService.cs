// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    [ExportWorkspaceService(typeof(IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService), WorkspaceKind.Test), Shared]
    public class TestAddMetadataReferenceCodeActionOperationFactoryWorkspaceService : IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService
    {
        [ImportingConstructor]
        public TestAddMetadataReferenceCodeActionOperationFactoryWorkspaceService()
        {
        }

        public CodeActionOperation CreateAddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
        {
            return new Operation(projectId, assemblyIdentity);
        }

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
