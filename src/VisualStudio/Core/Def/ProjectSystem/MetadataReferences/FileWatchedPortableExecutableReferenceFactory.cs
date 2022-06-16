// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.MetadataReferences
{
    [Export]
    internal sealed class FileWatchedPortableExecutableReferenceFactory
    {
        private readonly object _gate = new();

        /// <summary>
        /// This right now acquires the entire VisualStudioWorkspace because right now the production
        /// of metadata references depends on other workspace services. See the comments on
        /// <see cref="VisualStudioMetadataReferenceManagerFactory"/> that this strictly shouldn't be necessary
        /// but for now is quite the tangle to fix.
        /// </summary>
        private readonly Lazy<VisualStudioWorkspace> _visualStudioWorkspace;

        /// <summary>
        /// A file change context used to watch metadata references.
        /// </summary>
        private readonly FileChangeWatcher.IContext _fileReferenceChangeContext;

        /// <summary>
        /// File watching tokens from <see cref="_fileReferenceChangeContext"/> that are watching metadata references. These are only created once we are actually applying a batch because
        /// we don't determine until the batch is applied if the file reference will actually be a file reference or it'll be a converted project reference.
        /// </summary>
        private readonly Dictionary<PortableExecutableReference, FileChangeWatcher.IFileWatchingToken> _metadataReferenceFileWatchingTokens = new();

        /// <summary>
        /// <see cref="CancellationTokenSource"/>s for in-flight refreshing of metadata references. When we see a file change, we wait a bit before trying to actually
        /// update the workspace. We need cancellation tokens for those so we can cancel them either when a flurry of events come in (so we only do the delay after the last
        /// modification), or when we know the project is going away entirely.
        /// </summary>
        private readonly Dictionary<string, CancellationTokenSource> _metadataReferenceRefreshCancellationTokenSources = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FileWatchedPortableExecutableReferenceFactory(
            Lazy<VisualStudioWorkspace> visualStudioWorkspace,
            FileChangeWatcherProvider fileChangeWatcherProvider)
        {
            _visualStudioWorkspace = visualStudioWorkspace;

            // We will do a single directory watch on the Reference Assemblies folder to avoid having to create separate file
            // watches on individual .dlls that effectively never change.
            var referenceAssembliesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework");
            var referenceAssemblies = new FileChangeWatcher.WatchedDirectory(referenceAssembliesPath, ".dll");

            // TODO: set this to watch the NuGet directory as well; there's some concern that watching the entire directory
            // might make restores take longer because we'll be watching changes that may not impact your project.

            _fileReferenceChangeContext = fileChangeWatcherProvider.Watcher.CreateContext(referenceAssemblies);
            _fileReferenceChangeContext.FileChanged += FileReferenceChangeContext_FileChanged;
        }

        public event EventHandler<string> ReferenceChanged;

        public PortableExecutableReference CreateReferenceAndStartWatchingFile(string fullFilePath, MetadataReferenceProperties properties)
        {
            lock (_gate)
            {
                var reference = _visualStudioWorkspace.Value.CreatePortableExecutableReference(fullFilePath, properties);
                var fileWatchingToken = _fileReferenceChangeContext.EnqueueWatchingFile(fullFilePath);

                _metadataReferenceFileWatchingTokens.Add(reference, fileWatchingToken);

                return reference;
            }
        }

        public void StopWatchingReference(PortableExecutableReference reference)
        {
            lock (_gate)
            {
                if (!_metadataReferenceFileWatchingTokens.TryGetValue(reference, out var token))
                {
                    throw new ArgumentException("The reference was already not being watched.");
                }

                _fileReferenceChangeContext.StopWatchingFile(token);
                _metadataReferenceFileWatchingTokens.Remove(reference);

                // Note we still potentially have an outstanding change that we haven't raised a notification
                // for due to the delay we use. We could cancel the notification for that file path,
                // but we may still have another outstanding PortableExecutableReference that isn't this one
                // that does want that notification. We're OK just leaving the delay still running for two
                // reasons:
                //
                // 1. Technically, we did see a file change before the call to StopWatchingReference, so
                //    arguably we should still raise it.
                // 2. Since we raise the notification for a file path, it's up to the consumer of this to still
                //    track down which actual reference needs to be changed. That'll automatically handle any
                //    race where the event comes late, which is a scenario this must always deal with no matter
                //    what -- another thread might already be gearing up to notify the caller of this reference
                //    and we can't stop it.
            }
        }

        private void FileReferenceChangeContext_FileChanged(object sender, string fullFilePath)
        {
            lock (_gate)
            {
                if (_metadataReferenceRefreshCancellationTokenSources.TryGetValue(fullFilePath, out var cancellationTokenSource))
                {
                    cancellationTokenSource.Cancel();
                    _metadataReferenceRefreshCancellationTokenSources.Remove(fullFilePath);
                }

                cancellationTokenSource = new CancellationTokenSource();
                _metadataReferenceRefreshCancellationTokenSources.Add(fullFilePath, cancellationTokenSource);

                Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token).ContinueWith(_ =>
                {
                    var needsNotification = false;

                    lock (_gate)
                    {
                        // We need to re-check the cancellation token source under the lock, since it might have been cancelled and restarted
                        // due to another event
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        needsNotification = true;

                        _metadataReferenceRefreshCancellationTokenSources.Remove(fullFilePath);
                    }

                    if (needsNotification)
                    {
                        ReferenceChanged?.Invoke(this, fullFilePath);
                    }
                }, cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
        }
    }
}
