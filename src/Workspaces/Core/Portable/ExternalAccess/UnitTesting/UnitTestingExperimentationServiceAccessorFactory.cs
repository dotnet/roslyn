// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    [ExportWorkspaceServiceFactory(typeof(IUnitTestingExperimentationServiceAccessor))]
    [Shared]
    internal class UnitTestingExperimentationServiceAccessorFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingExperimentationServiceAccessorFactory() { }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var experimentationService = workspaceServices.GetRequiredService<IExperimentationService>();
            return new UnitTestingExperimentationServiceAccessor(experimentationService);
        }
    }
}
