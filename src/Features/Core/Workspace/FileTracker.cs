// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A class that tracks file changes on disk and invokes user actions
    /// when changes happen.
    /// </summary>
    internal class FileTracker
    {
        // guards watchers and actions
        private readonly NonReentrantLock guard = new NonReentrantLock();

        private readonly Dictionary<string, FileSystemWatcher> watchers =
            new Dictionary<string, FileSystemWatcher>();

        private readonly Dictionary<string, FileActions> fileActionsMap =
            new Dictionary<string, FileActions>();

        public FileTracker()
        {
        }

        private FileActions GetFileActions_NoLock(string path)
        {
            guard.AssertHasLock();

            FileActions actions;

            if (!this.fileActionsMap.TryGetValue(path, out actions))
            {
                actions = new FileActions(this, path);
                this.fileActionsMap.Add(path, actions);
            }

            return actions;
        }

        public bool IsTracking(string path)
        {
            if (path == null)
            {
                return false;
            }

            using (guard.DisposableWait())
            {
                return this.fileActionsMap.ContainsKey(path);
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
                var fileActions = this.GetFileActions_NoLock(path);
                fileActions.AddAction_NoLock(action);

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
                    this.fileActionsMap.Remove(path);
                }
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs args)
        {
            FileActions actions;

            using (this.guard.DisposableWait())
            {
                actions = this.GetFileActions_NoLock(args.FullPath);
            }

            actions.InvokeActions();
        }

        public void Dispose()
        {
            using (this.guard.DisposableWait())
            {
                foreach (var watcher in this.watchers.Values)
                {
                    watcher.Dispose();
                }

                this.watchers.Clear();
                this.fileActionsMap.Clear();
            }
        }

        private class FileActions
        {
            private FileTracker tracker;
            private readonly string path;
            private ImmutableArray<Action> actions;
            private Task invokeTask;

            public FileActions(FileTracker tracker, string path)
            {
                this.tracker = tracker;
                this.path = path;
                this.actions = ImmutableArray.Create<Action>();
            }

            public void AddAction_NoLock(Action action)
            {
                this.tracker.guard.AssertHasLock();

                this.actions = this.actions.Add(action);
            }

            public void InvokeActions()
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

            private void TryInvokeActions(ImmutableArray<Action> actions)
            {
                if (actions.Length == 0)
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
