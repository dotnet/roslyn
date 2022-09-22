// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.TextManager.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using SAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class LanguageSettingsPersisterProvider : IOptionPersisterProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IGlobalOptionService _optionService;
        private LanguageSettingsPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageSettingsPersisterProvider(
            IThreadingContext threadingContext,
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
            IGlobalOptionService optionService)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _optionService = optionService;
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
        {
            if (_lazyPersister is not null)
            {
                return _lazyPersister;
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Asynchronously load dependent package prior to calling synchronous APIs on IVsTextManager4
            _ = await _serviceProvider.GetServiceAsync(typeof(SVsManagedFontAndColorInformation)).ConfigureAwait(true);

            var textManager = (IVsTextManager4?)await _serviceProvider.GetServiceAsync(typeof(SVsTextManager)).ConfigureAwait(true);
            Assumes.Present(textManager);

            _lazyPersister ??= new LanguageSettingsPersister(_threadingContext, textManager, _optionService);
            return _lazyPersister;
        }

        /// <summary>
        /// Shim to allow asynchronous loading of a package prior to calling synchronous (blocking) APIs that use it.
        /// </summary>
        [Guid("48d069e8-1993-4752-baf3-232236a3ea4f")]
        private class SVsManagedFontAndColorInformation
        {
        }
    }
}
