// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    [Export, Shared]
    public class TestingOnly_WaitingService
    {
        private readonly IEnumerable<Lazy<IAsynchronousOperationWaiter, FeatureMetadata>> _waiters;

        [ImportingConstructor]
        private TestingOnly_WaitingService([ImportMany] IEnumerable<Lazy<IAsynchronousOperationWaiter, FeatureMetadata>> waiters)
        {
            this._waiters = waiters;
        }

        private void WaitForAsyncOperations(
            Func<FeatureMetadata, bool> predicate,
            bool waitForWorkspaceFirst = true)
        {
            // FeatureMetadata is MEF's way to extract export metadata from the exported instance's
            // [Feature] attribute e.g. if the export defines an attribute like this:
            // 
            //     [Feature(FeatureAttribute.ErrorSquiggles)] 
            //
            // then all properties on FeatureAttribute ("FeatureName") are mapped to properties on
            // the FeatureMetadata ("FeatureName" as well). Types and names of properties must
            // match. Read more at http://mef.codeplex.com/wikipage?title=Exports%20and%20Metadata

            var workspaceListener =
                (from export in _waiters
                 where export.Metadata.FeatureName == FeatureAttribute.Workspace
                 select export.Value).FirstOrDefault();

            var listeners =
                (from export in _waiters
                 where predicate(export.Metadata)
                 select export.Value).ToArray();

            if (!listeners.Any())
            {
                throw new InvalidOperationException("There is no waiter that matches your condition!");
            }

            // wait for each of the features specified in the featuresToWaitFor string
            if (waitForWorkspaceFirst)
            {
                // at least wait for the workspace to finish processing everything.
                if (workspaceListener != null)
                {
                    var task = workspaceListener.CreateWaitTask();
                    task.Wait();
                }
            }

            var waitTasks = listeners.Select(l =>
            {
                var task = l.CreateWaitTask();
                return task;
            }).ToArray();

            while (!Task.WaitAll(waitTasks, 100))
            {
                // set breakpoint here when debugging
                var tokens = listeners.Where(l => l.TrackActiveTokens).SelectMany(l => l.ActiveDiagnosticTokens).ToArray();

                GC.KeepAlive(tokens);
            }

            // Debugging trick: don't let the listeners collection get optimized away during execution.
            // This means if the process is killed during integration tests and the test was waiting, you can
            // easily look at the listeners and see what is going on. This is not actually needed to
            // affect the GC, nor is it needed for correctness.
            GC.KeepAlive(listeners);
        }

        public void WaitForAsyncOperations(
            string featuresToWaitFor,
            bool waitForWorkspaceFirst = true)
        {
            WaitForAsyncOperations(featureMetadata => featuresToWaitFor.Contains(featureMetadata.FeatureName), waitForWorkspaceFirst);
        }

        public void WaitForAllAsyncOperations()
        {
            WaitForAsyncOperations(_ => true, true);
        }

        public void SetTrackActiveTokens(bool trackTokens)
        {
            foreach (var waiter in _waiters)
            {
                waiter.Value.TrackActiveTokens = trackTokens;
            }
        }

        public IEnumerable<string> GetTotalFeatures()
        {
            return _waiters.Select(w => w.Metadata.FeatureName);
        }

        public IEnumerable<string> GetActiveFeatures()
        {
            return from w in _waiters where w.Value.HasPendingWork select w.Metadata.FeatureName;
        }

        public void EnableActiveTokenTracking(bool enable)
        {
            foreach (var waiter in _waiters)
            {
                waiter.Value.TrackActiveTokens = enable;
            }
        }
    }
}
