using System;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    [ExportWorkspaceServiceFactory(typeof(IUnitTestingRemoteHostClientServiceAccessor))]
    [Shared]
    internal sealed class UnitTestingRemoteHostClientServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IRemoteHostClientService _implementation;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public UnitTestingRemoteHostClientServiceFactory(IRemoteHostClientService implementation)
            => this._implementation = implementation;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => workspaceServices.GetService<IRemoteHostClientService>();
    }
}
