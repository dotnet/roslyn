// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [Export(typeof(VisualStudioExperimentationService))]
    [ExportWorkspaceService(typeof(IExperimentationServiceFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioExperimentationService : IExperimentationServiceFactory
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private IExperimentationService _experimentationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExperimentationService(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public async ValueTask<IExperimentationService> GetExperimentationServiceAsync(CancellationToken cancellationToken)
        {
            if (_experimentationService is null)
            {
                var featureFlags = (IVsFeatureFlags)await _serviceProvider.GetServiceAsync(typeof(SVsFeatureFlags)).ConfigureAwait(false);
                var shellExperimentationService = (IVsExperimentationService)await _serviceProvider.GetServiceAsync(typeof(SVsExperimentationService)).ConfigureAwait(false);
                var experimentationService = new ExperimentationService(featureFlags, shellExperimentationService);
                Interlocked.CompareExchange(ref _experimentationService, experimentationService, null);
            }

            return _experimentationService;
        }

        private class ExperimentationService : IExperimentationService
        {
            private readonly IVsFeatureFlags _featureFlags;
            private readonly IVsExperimentationService _experimentationService;

            public ExperimentationService(IVsFeatureFlags featureFlags, IVsExperimentationService experimentationService)
            {
                _featureFlags = featureFlags;
                _experimentationService = experimentationService;
            }

            public bool IsExperimentEnabled(string experimentName)
            {
                try
                {
                    // check whether "." exist in the experimentName since it is requirement for featureflag service.
                    // we do this since RPS complains about resource file being loaded for invalid name exception
                    // we are not testing all rules but just simple "." check
                    if (experimentName.IndexOf(".") > 0)
                    {
                        var enabled = _featureFlags.IsFeatureEnabled(experimentName, defaultValue: false);
                        if (enabled)
                        {
                            return enabled;
                        }
                    }
                }
                catch
                {
                    // featureFlags can throw if given name is in incorrect format which can happen for us
                    // since we use this for experimentation service as well
                }

                return _experimentationService.IsCachedFlightEnabled(experimentName);
            }
        }
    }
}
