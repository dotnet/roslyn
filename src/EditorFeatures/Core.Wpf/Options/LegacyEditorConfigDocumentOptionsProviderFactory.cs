// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    [Export(typeof(IDocumentOptionsProviderFactory)), Shared]
    class LegacyEditorConfigDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        private readonly ICodingConventionsManager _codingConventionsManager;
        private readonly IFileWatcher _fileWatcher;
        private readonly IAsynchronousOperationListenerProvider _asynchronousOperationListenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyEditorConfigDocumentOptionsProviderFactory(
            ICodingConventionsManager codingConventionsManager,
            IFileWatcher fileWatcher,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
        {
            _codingConventionsManager = codingConventionsManager;
            _fileWatcher = fileWatcher;
            _asynchronousOperationListenerProvider = asynchronousOperationListenerProvider;
        }

        public IDocumentOptionsProvider TryCreate(Workspace workspace)
        {
            if (EditorConfigDocumentOptionsProviderFactory.ShouldUseNativeEditorConfigSupport(workspace))
            {
                // If the native support exists, then we'll simply disable this one
                return null;
            }

            ICodingConventionsManager codingConventionsManager;

            if (workspace.Kind == WorkspaceKind.RemoteWorkspace)
            {
                // If it's the remote workspace, it's our own implementation of the file watcher which is already doesn't have
                // UI thread dependencies.
                codingConventionsManager = _codingConventionsManager;
            }
            else
            {
                // The default file watcher implementation inside Visual Studio accientally depends on the UI thread
                // (sometimes!) when trying to add a watch to a file. This can cause us to deadlock, since our assumption is
                // consumption of a coding convention can be done freely without having to use a JTF-friendly wait.
                // So we'll wrap the standard file watcher with one that defers the file watches until later.
                var deferredFileWatcher = new DeferredFileWatcher(_fileWatcher, _asynchronousOperationListenerProvider);
                codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager(deferredFileWatcher);
            }

            return new LegacyEditorConfigDocumentOptionsProvider(workspace, codingConventionsManager, _asynchronousOperationListenerProvider);
        }

        /// <summary>
        /// An implementation of <see cref="IFileWatcher"/> that ensures we don't watch for a file synchronously to
        /// avoid deadlocks.
        /// </summary>
        internal class DeferredFileWatcher : IFileWatcher
        {
            private readonly IFileWatcher _fileWatcher;
            private readonly SimpleTaskQueue _taskQueue = new SimpleTaskQueue(TaskScheduler.Default);
            private readonly IAsynchronousOperationListener _listener;

            public DeferredFileWatcher(IFileWatcher fileWatcher, IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
            {
                _fileWatcher = fileWatcher;
                _fileWatcher.ConventionFileChanged += OnConventionFileChanged;

                _listener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Workspace);
            }

            private Task OnConventionFileChanged(object sender, ConventionsFileChangeEventArgs arg)
            {
                return ConventionFileChanged?.Invoke(this, arg) ?? Task.CompletedTask;
            }

            public event ConventionsFileChangedAsyncEventHandler ConventionFileChanged;

            public event ContextFileMovedAsyncEventHandler ContextFileMoved
            {
                add
                {
                    _fileWatcher.ContextFileMoved += value;
                }

                remove
                {
                    _fileWatcher.ContextFileMoved -= value;
                }
            }

            public void Dispose()
            {
                _fileWatcher.ConventionFileChanged -= OnConventionFileChanged;
                _fileWatcher.Dispose();
            }

            public void StartWatching(string fileName, string directoryPath)
            {
                var asyncToken = _listener.BeginAsyncOperation(nameof(DeferredFileWatcher) + "." + nameof(StartWatching));

                // Read the file time stamp right now; we want to know if it changes between now
                // and our ability to get the file watcher in place.
                var originalFileTimeStamp = TryGetFileTimeStamp(fileName, directoryPath);
                _taskQueue.ScheduleTask(() =>
                {
                    _fileWatcher.StartWatching(fileName, directoryPath);

                    var newFileTimeStamp = TryGetFileTimeStamp(fileName, directoryPath);

                    if (originalFileTimeStamp != newFileTimeStamp)
                    {
                        ChangeType changeType;

                        if (!originalFileTimeStamp.HasValue && newFileTimeStamp.HasValue)
                        {
                            changeType = ChangeType.FileCreated;
                        }
                        else if (originalFileTimeStamp.HasValue && !newFileTimeStamp.HasValue)
                        {
                            changeType = ChangeType.FileDeleted;
                        }
                        else
                        {
                            changeType = ChangeType.FileModified;
                        }

                        ConventionFileChanged?.Invoke(this,
                            new ConventionsFileChangeEventArgs(fileName, directoryPath, changeType));
                    }
                }).CompletesAsyncOperation(asyncToken);
            }

            private static DateTime? TryGetFileTimeStamp(string fileName, string directoryPath)
            {
                try
                {
                    var fullFilePath = Path.Combine(directoryPath, fileName);

                    // Avoid a first-chance exception if the file definitely doesn't exist
                    if (!File.Exists(fullFilePath))
                    {
                        return null;
                    }

                    return FileUtilities.GetFileTimeStamp(fullFilePath);
                }
                catch (IOException)
                {
                    return null;
                }
            }

            public void StopWatching(string fileName, string directoryPath)
            {
                _taskQueue.ScheduleTask(() => _fileWatcher.StopWatching(fileName, directoryPath));
            }
        }
    }
}
