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
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class FeatureFlagPersisterProvider : IOptionPersisterProvider
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private FeatureFlagPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FeatureFlagPersisterProvider(
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
        {
            if (_lazyPersister != null)
            {
                return _lazyPersister;
            }

            IVsFeatureFlags? service;
            try
            {
                service = (IVsFeatureFlags?)await _serviceProvider.GetServiceAsync(typeof(SVsFeatureFlags)).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                service = null;
            }

            return _lazyPersister = new FeatureFlagPersister(service);
        }
    }
}
