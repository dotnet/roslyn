﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    [ExportWorkspaceServiceFactory(typeof(IUnitTestingSolutionCrawlerRegistrationServiceAccessor))]
    [Shared]
    internal sealed class UnitTestingSolutionCrawlerRegistrationServiceAccessorFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingSolutionCrawlerRegistrationServiceAccessorFactory() { }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var implementation = workspaceServices.GetRequiredService<ISolutionCrawlerRegistrationService>();
            return new UnitTestingSolutionCrawlerRegistrationServiceAccessor(implementation);
        }
    }
}
