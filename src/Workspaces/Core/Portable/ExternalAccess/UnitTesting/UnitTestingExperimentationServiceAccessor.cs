using System;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal sealed class UnitTestingExperimentationServiceAccessor : IUnitTestingExperimentationServiceAccessor
    {
        private readonly IExperimentationService _experimentationService;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingExperimentationServiceAccessor(IExperimentationService experimentationService)
            => _experimentationService = experimentationService;

        public bool IsExperimentEnabled(string experimentName)
            => _experimentationService.IsExperimentEnabled(experimentName);
    }
}
