// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    [Obsolete]
    [ExportWorkspaceServiceFactory(typeof(IUnitTestingSolutionCrawlerServiceAccessor))]
    [Shared]
    internal sealed class UnitTestingSolutionCrawlerServiceAccessorFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingSolutionCrawlerServiceAccessorFactory() { }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var solutionCrawlerRegistrationService = workspaceServices.GetRequiredService<ISolutionCrawlerRegistrationService>();
            var solutionCrawlerService = workspaceServices.GetRequiredService<ISolutionCrawlerService>();
            return new UnitTestingSolutionCrawlerServiceAccessor(solutionCrawlerRegistrationService, solutionCrawlerService);
        }
    }
}
