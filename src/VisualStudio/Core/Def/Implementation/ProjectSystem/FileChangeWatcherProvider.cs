using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(FileChangeWatcherProvider))]
    internal sealed class FileChangeWatcherProvider
    {
        private readonly TaskCompletionSource<IVsFileChangeEx> _fileChangeService = new TaskCompletionSource<IVsFileChangeEx>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Lazy<FileChangeWatcher> _fileChangeWatcher;

        public FileChangeWatcherProvider()
        {
            _fileChangeWatcher = new Lazy<FileChangeWatcher>(() => new FileChangeWatcher(_fileChangeService.Task));
        }

        public FileChangeWatcher Watcher => _fileChangeWatcher.Value;

        internal void SetFileChangeService(IVsFileChangeEx fileChangeService)
        {
            _fileChangeService.TrySetResult(fileChangeService);
        }
    }
}
