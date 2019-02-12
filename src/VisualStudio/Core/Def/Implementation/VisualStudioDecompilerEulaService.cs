// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IDecompilerEulaService), ServiceLayer.Host)]
    [Shared]
    internal sealed class VisualStudioDecompilerEulaService : IDecompilerEulaService
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        private RoslynPackage _roslynPackage;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDecompilerEulaService(SVsServiceProvider serviceProvider, IThreadingContext threadingContext)
        {
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
            _threadingContext = threadingContext;
        }

        public async Task<bool> IsAcceptedAsync(CancellationToken cancellationToken)
        {
            var roslynPackage = await TryGetRoslynPackageAsync(cancellationToken).ConfigureAwait(false);
            return roslynPackage.IsDecompilerEulaAccepted;
        }

        public async Task MarkAcceptedAsync(CancellationToken cancellationToken)
        {
            var roslynPackage = await TryGetRoslynPackageAsync(cancellationToken).ConfigureAwait(false);
            roslynPackage.IsDecompilerEulaAccepted = true;
        }

        private async ValueTask<RoslynPackage> TryGetRoslynPackageAsync(CancellationToken cancellationToken)
        {
            if (_roslynPackage is null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                cancellationToken.ThrowIfCancellationRequested();

                var shell = (IVsShell7)await _serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
                await shell.LoadPackageAsync(typeof(RoslynPackage).GUID);

                if (ErrorHandler.Succeeded(((IVsShell)shell).IsPackageLoaded(typeof(RoslynPackage).GUID, out var package)))
                {
                    _roslynPackage = (RoslynPackage)package;
                }
            }

            return _roslynPackage;
        }
    }
}
