using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using IVsAsyncFileChangeEx = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(FileChangeWatcherProvider))]
    internal sealed class FileChangeWatcherProvider
    {
        private readonly TaskCompletionSource<IVsAsyncFileChangeEx> _fileChangeService = new TaskCompletionSource<IVsAsyncFileChangeEx>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Lazy<FileChangeWatcher> _fileChangeWatcher;

        public FileChangeWatcherProvider()
        {
            _fileChangeWatcher = new Lazy<FileChangeWatcher>(() => new FileChangeWatcher(_fileChangeService.Task));
        }

        public FileChangeWatcher Watcher => _fileChangeWatcher.Value;

        internal void SetFileChangeService(IVsAsyncFileChangeEx fileChangeService)
        {
            _fileChangeService.TrySetResult(fileChangeService);
        }
    }
}
