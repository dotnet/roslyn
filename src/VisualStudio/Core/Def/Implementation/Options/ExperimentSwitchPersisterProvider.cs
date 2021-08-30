// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class ExperimentSwitchPersisterProvider : IOptionPersisterProvider
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private ExperimentSwitchPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExperimentSwitchPersisterProvider(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
        {
            if (_lazyPersister != null)
            {
                return _lazyPersister;
            }

            IVsExperimentationService? service;
            try
            {
                service = (IVsExperimentationService?)await _serviceProvider.GetServiceAsync(typeof(SVsExperimentationService)).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                service = null;
            }

            return _lazyPersister = new ExperimentSwitchPersister(service);
        }
    }
}
