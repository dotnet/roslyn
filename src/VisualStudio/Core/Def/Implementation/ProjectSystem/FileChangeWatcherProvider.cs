// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IVsAsyncFileChangeEx = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(FileChangeWatcherProvider))]
    internal sealed class FileChangeWatcherProvider
    {
        private readonly TaskCompletionSource<IVsAsyncFileChangeEx> _fileChangeService = new TaskCompletionSource<IVsAsyncFileChangeEx>(TaskCreationOptions.RunContinuationsAsynchronously);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FileChangeWatcherProvider(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] Shell.IAsyncServiceProvider serviceProvider)
        {
            // We do not want background work to implicitly block on the availability of the SVsFileChangeEx to avoid any deadlock risk,
            // since the first fetch for a file watcher might end up happening on the background.
            Watcher = new FileChangeWatcher(_fileChangeService.Task);

            System.Threading.Tasks.Task.Run(async () =>
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var fileChangeService = (IVsAsyncFileChangeEx)await serviceProvider.GetServiceAsync(typeof(SVsFileChangeEx)).ConfigureAwait(true);
                    _fileChangeService.SetResult(fileChangeService);
                });
        }

        public FileChangeWatcher Watcher { get; }

        // HACK HACK: this is to work around the SwitchToMainThread in the constructor above not
        // being practical to run in unit tests. That SwitchToMainThread is working around a now-fixed
        // bug in the shell where GetServiceAsync() might deadlock in the VS service manager
        // if the UI thread was also dealing with the service at the same time. I'd remove the
        // SwitchToMainThreadAsync right now instead of this doing this hack, but we're targeting this
        // fix for a preview release that's too risky to do it in. Other options involve more extensive
        // mocking or extracting of interfaces which is also just churn that will be immediately undone
        // once we clean up the constructor either.
        internal void TrySetFileChangeService_TestOnly(IVsAsyncFileChangeEx fileChange)
        {
            _fileChangeService.TrySetResult(fileChange);
        }
    }
}
