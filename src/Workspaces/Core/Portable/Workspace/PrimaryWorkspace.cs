// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class PrimaryWorkspace
    {
        private static readonly ReaderWriterLockSlim s_registryGate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private static Workspace s_primaryWorkspace;

        private static readonly List<TaskCompletionSource<Workspace>> s_primaryWorkspaceTaskSourceList =
            new List<TaskCompletionSource<Workspace>>();

        /// <summary>
        /// The primary workspace, usually set by the host environment.
        /// </summary>
        public static Workspace Workspace
        {
            get
            {
                using (s_registryGate.DisposableRead())
                {
                    return s_primaryWorkspace;
                }
            }
        }

        /// <summary>
        /// Register a workspace as the primary workspace. Only one workspace can be the primary.
        /// </summary>
        public static void Register(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            using (s_registryGate.DisposableWrite())
            {
                s_primaryWorkspace = workspace;

                foreach (var taskSource in s_primaryWorkspaceTaskSourceList)
                {
                    try
                    {
                        taskSource.TrySetResult(workspace);
                    }
                    catch
                    {
                    }
                }

                s_primaryWorkspaceTaskSourceList.Clear();
            }
        }

        /// <summary>
        /// Get's the primary workspace asynchronously.
        /// </summary>
        public static Task<Workspace> GetWorkspaceAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (s_registryGate.DisposableWrite())
            {
                if (s_primaryWorkspace != null)
                {
                    return Task<Workspace>.FromResult(s_primaryWorkspace);
                }
                else
                {
                    var taskSource = new TaskCompletionSource<Workspace>();

                    if (cancellationToken.CanBeCanceled)
                    {
                        try
                        {
                            var registration = cancellationToken.Register(() =>
                            {
                                taskSource.TrySetCanceled();
                            });

                            taskSource.Task.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        }
                        catch
                        {
                        }
                    }

                    s_primaryWorkspaceTaskSourceList.Add(taskSource);
                    return taskSource.Task;
                }
            }
        }
    }
}