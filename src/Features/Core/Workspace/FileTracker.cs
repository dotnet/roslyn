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
        private readonly NonReentrantLock _guard = new NonReentrantLock();

        private readonly Dictionary<string, FileSystemWatcher> _watchers =
            new Dictionary<string, FileSystemWatcher>();

        private readonly Dictionary<string, FileActions> _fileActionsMap =
            new Dictionary<string, FileActions>();

        public FileTracker()
        {
        }

        private FileActions GetFileActions_NoLock(string path)
        {
            _guard.AssertHasLock();

            FileActions actions;

            if (!_fileActionsMap.TryGetValue(path, out actions))
            {
                actions = new FileActions(this, path);
                _fileActionsMap.Add(path, actions);
            }

            return actions;
        }

        public bool IsTracking(string path)
        {
            if (path == null)
            {
                return false;
            }

            using (_guard.DisposableWait())
            {
                return _fileActionsMap.ContainsKey(path);
            }
        }

        public void Track(string path, Action action)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (_guard.DisposableWait())
            {
                var fileActions = this.GetFileActions_NoLock(path);
                fileActions.AddAction_NoLock(action);

                var directory = Path.GetDirectoryName(path);
                if (!_watchers.ContainsKey(directory))
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
                using (_guard.DisposableWait())
                {
                    _fileActionsMap.Remove(path);
                }
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs args)
        {
            FileActions actions;

            using (_guard.DisposableWait())
            {
                actions = this.GetFileActions_NoLock(args.FullPath);
            }

            actions.InvokeActions();
        }

        public void Dispose()
        {
            using (_guard.DisposableWait())
            {
                foreach (var watcher in _watchers.Values)
                {
                    watcher.Dispose();
                }

                _watchers.Clear();
                _fileActionsMap.Clear();
            }
        }

        private class FileActions
        {
            private FileTracker _tracker;
            private readonly string _path;
            private ImmutableArray<Action> _actions;
            private Task _invokeTask;

            public FileActions(FileTracker tracker, string path)
            {
                _tracker = tracker;
                _path = path;
                _actions = ImmutableArray.Create<Action>();
            }

            public void AddAction_NoLock(Action action)
            {
                _tracker._guard.AssertHasLock();

                _actions = _actions.Add(action);
            }

            public void InvokeActions()
            {
                using (_tracker._guard.DisposableWait())
                {
                    // only start invoke task if one is not already running
                    if (_invokeTask == null)
                    {
                        _invokeTask = Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);
                        _invokeTask.ContinueWithAfterDelay(() => TryInvokeActions(_actions), CancellationToken.None, 100, TaskContinuationOptions.None, TaskScheduler.Current);
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
                using (var stream = Kernel32File.Open(_path, FileAccess.Read, FileMode.Open, throwException: false))
                {
                    if (stream != null)
                    {
                        stream.Close();

                        foreach (var action in actions)
                        {
                            action();
                        }

                        // clear invoke task so any following changes get additional invocations
                        using (_tracker._guard.DisposableWait())
                        {
                            _invokeTask = null;
                        }
                    }
                    else
                    {
                        // try again after a short delay
                        using (_tracker._guard.DisposableWait())
                        {
                            _invokeTask.ContinueWithAfterDelay(() => TryInvokeActions(_actions), CancellationToken.None, 100, TaskContinuationOptions.None, TaskScheduler.Current);
                        }
                    }
                }
            }
        }
    }
}
