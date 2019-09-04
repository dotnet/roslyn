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
    internal class UnitTestingExperimentationServiceAccessorFactory
    {
        private readonly IExperimentationService _experimentationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingExperimentationServiceAccessorFactory(IExperimentationService experimentationService)
            => _experimentationService = experimentationService;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var experimentationService = workspaceServices.GetRequiredService<IExperimentationService>();
            return new UnitTestingExperimentationServiceAccessor(experimentationService);
        }
    }
}
