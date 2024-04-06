// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IVsAsyncFileChangeEx2 = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx2;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

[Export(typeof(FileChangeWatcherProvider))]
internal sealed class FileChangeWatcherProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FileChangeWatcherProvider(
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider,
        [Import(typeof(SVsServiceProvider))] Shell.IAsyncServiceProvider serviceProvider)
    {
        var fileChangeService = Task.Factory.StartNew(
            async () =>
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(threadingContext.DisposalToken);

                var fileChangeService = (IVsAsyncFileChangeEx2?)await serviceProvider.GetServiceAsync(typeof(SVsFileChangeEx)).ConfigureAwait(true);
                Assumes.Present(fileChangeService);
                return fileChangeService;
            },
            threadingContext.DisposalToken,
            TaskCreationOptions.RunContinuationsAsynchronously,
            TaskScheduler.Default)
            .Unwrap();

        // We do not want background work to implicitly block on the availability of the SVsFileChangeEx to avoid any deadlock risk,
        // since the first fetch for a file watcher might end up happening on the background.
        Watcher = new FileChangeWatcher(listenerProvider, fileChangeService);
    }

    public IFileChangeWatcher Watcher { get; }
}
