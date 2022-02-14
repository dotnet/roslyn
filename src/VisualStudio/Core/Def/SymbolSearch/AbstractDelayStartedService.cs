// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    /// <summary>
    /// Base type for services that we want to delay running until certain criteria is met.
    /// For example, we don't want to run the <see cref="VisualStudioSymbolSearchService"/> core codepath
    /// if the user has not enabled the features that need it.  That helps us avoid loading
    /// dlls unnecessarily and bloating the VS memory space.
    /// </summary>
    internal abstract class AbstractDelayStartedService : ForegroundThreadAffinitizedObject
    {
        private readonly IGlobalOptionService _globalOptions;

        // Option that controls if this service is enabled or not (regardless of language).
        private readonly Option2<bool> _featureEnabledOption;

        private bool _enabled = false;

        protected CancellationToken DisposalToken => ThreadingContext.DisposalToken;

        protected AbstractDelayStartedService(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            Option2<bool> featureEnabledOption)
            : base(threadingContext)
        {
            _globalOptions = globalOptions;
            _featureEnabledOption = featureEnabledOption;
        }

        protected abstract Task EnableServiceAsync(CancellationToken cancellationToken);
        protected abstract void StartWorking();

        public async Task StartAsync()
        {
            // If feature is totally disabled.  Do nothing.
            if (!_globalOptions.GetOption(_featureEnabledOption))
                return;

            if (_enabled)
                return;

            _enabled = true;
            await this.EnableServiceAsync(this.DisposalToken).ConfigureAwait(false);
            this.StartWorking();
        }
    }
}
