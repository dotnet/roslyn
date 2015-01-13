using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class PrimaryWorkspace
    {
        private static readonly ReaderWriterLockSlim registryGate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private static Workspace primaryWorkspace;

        private static List<TaskCompletionSource<Workspace>> primaryWorkspaceTaskSourceList =
            new List<TaskCompletionSource<Workspace>>();

        /// <summary>
        /// The primary workspace, usually set by the host environment.
        /// </summary>
        public static Workspace Workspace
        {
            get
            {
                using (registryGate.DisposableRead())
                {
                    return primaryWorkspace;
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
                throw new ArgumentNullException("workspace");
            }

            using (registryGate.DisposableWrite())
            {
                primaryWorkspace = workspace;

                foreach (var taskSource in primaryWorkspaceTaskSourceList)
                {
                    try
                    {
                        taskSource.TrySetResult(workspace);
                    }
                    catch
                    {
                    }
                }

                primaryWorkspaceTaskSourceList.Clear();
            }
        }

        /// <summary>
        /// Get's the primary workspace asynchronously.
        /// </summary>
        public static Task<Workspace> GetWorkspaceAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (registryGate.DisposableWrite())
            {
                if (primaryWorkspace != null)
                {
                    return Task<Workspace>.FromResult(primaryWorkspace);
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

                    primaryWorkspaceTaskSourceList.Add(taskSource);
                    return taskSource.Task;
                }
            }
        }
    }
}