using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(FileChangeWatcherProvider))]
    internal sealed class FileChangeWatcherProvider
    {
        private readonly TaskCompletionSource<IVsFileChangeEx> _fileChangeService = new TaskCompletionSource<IVsFileChangeEx>(TaskCreationOptions.RunContinuationsAsynchronously);

        [ImportingConstructor]
        public FileChangeWatcherProvider(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] Shell.IAsyncServiceProvider serviceProvider)
        {
            // We do not want background work to implicitly block on the availability of the SVsFileChangeEx to avoid any deadlock risk,
            // since the first fetch for a file watcher might end up happening on the background.
            Watcher = new FileChangeWatcher(_fileChangeService.Task);

            System.Threading.Tasks.Task.Run(async () =>
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var fileChangeService = (IVsFileChangeEx)await serviceProvider.GetServiceAsync(typeof(SVsFileChangeEx)).ConfigureAwait(true);
                    _fileChangeService.SetResult(fileChangeService);
                });
        }

        public FileChangeWatcher Watcher { get; }
    }
}
