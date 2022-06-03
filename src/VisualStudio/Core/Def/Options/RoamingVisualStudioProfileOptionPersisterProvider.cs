// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using SAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class RoamingVisualStudioProfileOptionPersisterProvider : IOptionPersisterProvider
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IGlobalOptionService _optionService;
        private RoamingVisualStudioProfileOptionPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoamingVisualStudioProfileOptionPersisterProvider(
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
            IGlobalOptionService optionService)
        {
            _serviceProvider = serviceProvider;
            _optionService = optionService;
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
        {
            if (_lazyPersister is not null)
            {
                return _lazyPersister;
            }

            var settingsManager = (ISettingsManager?)await _serviceProvider.GetServiceAsync(typeof(SVsSettingsPersistenceManager)).ConfigureAwait(true);

            _lazyPersister ??= new RoamingVisualStudioProfileOptionPersister(_optionService, settingsManager);
            return _lazyPersister;
        }
    }
}
