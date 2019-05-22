// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [ExportWorkspaceServiceFactory(typeof(IActiveStatementTrackingService), ServiceLayer.Editor), Shared]
    internal sealed class ActiveStatementTrackingServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public ActiveStatementTrackingServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new ActiveStatementTrackingService();
        }
    }
}
