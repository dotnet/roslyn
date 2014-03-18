// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(IFileTrackingService), WorkspaceKind.Any)]
#endif
    internal class FileTrackingServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new FileTrackingService();
        }

        private class FileTrackingService : IFileTrackingService
        {
            public IFileTracker CreateFileTracker()
            {
                return new FileSetTracker();
            }
        }

        private class FileSetTracker : IFileTracker
        {
            // guards watchers and actions
            private readonly NonReentrantLock guard = new NonReentrantLock();

            private readonly Dictionary<string, FileSystemWatcher> watchers =
                new Dictionary<string, FileSystemWatcher>();

            private readonly Dictionary<string, FileTracker> fileTrackers =
                new Dictionary<string, FileTracker>();

            public FileSetTracker()
            {
            }

            private FileTracker GetFileTracker_NoLock(string path)
            {
                guard.AssertHasLock();

                FileTracker tracker;

                if (!this.fileTrackers.TryGetValue(path, out tracker))
                {
                    tracker = new FileTracker(this, path);
                    this.fileTrackers.Add(path, tracker);
                }

                return tracker;
            }

            public bool IsTracking(string path)
            {
                if (path == null)
                {
                    return false;
                }

                using (guard.DisposableWait())
                {
                    return this.fileTrackers.ContainsKey(path);
                }
            }

            public void Track(string path, Action action)
            {
                if (path == null)
                {
                    throw new ArgumentNullException("path");
                }

                using (guard.DisposableWait())
                {
                    var tracker = this.GetFileTracker_NoLock(path);
                    tracker.AddAction_NoLock(action);

                    var directory = Path.GetDirectoryName(path);
                    if (!watchers.ContainsKey(directory))
                    {
                        var watcher = new FileSystemWatcher(directory);
                        watcher.Changed += OnFileChanged;
                        watcher.EnableRaisingEvents = true;
                    }
                }
            }

            public void StopTracking(string path)
            {
                if (path != null)
                {
                    using (guard.DisposableWait())
                    {
                        this.fileTrackers.Remove(path);
                    }
                }
            }

            private void OnFileChanged(object sender, FileSystemEventArgs args)
            {
                FileTracker tracker;

                using (this.guard.DisposableWait())
                {
                    tracker = this.GetFileTracker_NoLock(args.FullPath);
                }

                tracker.OnFileChanged();
            }

            public void Dispose()
            {
                using (this.guard.DisposableWait())
                {
                    foreach (var watcher in watchers.Values)
                    {
                        watcher.Dispose();
                    }

                    watchers.Clear();
                    fileTrackers.Clear();
                }
            }

            private class FileTracker
            {
                private FileSetTracker tracker;
                private readonly string path;
                private ImmutableList<Action> actions;
                private Task invokeTask;

                public FileTracker(FileSetTracker tracker, string path)
                {
                    this.tracker = tracker;
                    this.path = path;
                    this.actions = ImmutableList.Create<Action>();
                }

                public void AddAction_NoLock(Action action)
                {
                    this.tracker.guard.AssertHasLock();

                    this.actions = this.actions.Add(action);
                }

                public void OnFileChanged()
                {
                    using (this.tracker.guard.DisposableWait())
                    {
                        // only start invoke task if one is not already running
                        if (this.invokeTask == null)
                        {
                            this.invokeTask = Task.Factory.StartNew(() => { });
                            this.invokeTask.ContinueWithAfterDelay(() => TryInvokeActions(this.actions), CancellationToken.None, 100, TaskContinuationOptions.None, TaskScheduler.Current);
                        }
                    }
                }

                private void TryInvokeActions(ImmutableList<Action> actions)
                {
                    if (actions.Count == 0)
                    {
                        return;
                    }

                    // only invoke actions if the writer that caused the event is done 
                    // determine this by checking to see if we can read the file
                    using (var stream = Kernel32File.Open(this.path, FileAccess.Read, FileMode.Open, throwException: false))
                    {
                        if (stream != null)
                        {
                            stream.Close();

                            foreach (var action in actions)
                            {
                                action();
                            }

                            // clear invoke task so any following changes get additional invocations
                            using (this.tracker.guard.DisposableWait())
                            {
                                this.invokeTask = null;
                            }
                        }
                        else
                        {
                            // try again after a short delay
                            using (this.tracker.guard.DisposableWait())
                            {
                                this.invokeTask.ContinueWithAfterDelay(() => TryInvokeActions(this.actions), CancellationToken.None, 100, TaskContinuationOptions.None, TaskScheduler.Current);
                            }
                        }
                    }
                }
            }
        }
    }
}