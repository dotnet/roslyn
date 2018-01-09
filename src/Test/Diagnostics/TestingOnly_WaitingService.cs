// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    [Export, Shared]
    public class TestingOnly_WaitingService
    {
        private readonly IAsynchronousOperationListenerProvider _provider;

        [ImportingConstructor]
        private TestingOnly_WaitingService(IAsynchronousOperationListenerProvider provider)
        {
            _provider = provider;
        }

        public void WaitForAsyncOperations(string featureName, bool waitForWorkspaceFirst = true)
        {
            // FeatureMetadata is MEF's way to extract export metadata from the exported instance's
            // [Feature] attribute e.g. if the export defines an attribute like this:
            // 
            //     [Feature(FeatureAttribute.ErrorSquiggles)] 
            //
            // then all properties on FeatureAttribute ("FeatureName") are mapped to properties on
            // the FeatureMetadata ("FeatureName" as well). Types and names of properties must
            // match. Read more at http://mef.codeplex.com/wikipage?title=Exports%20and%20Metadata

            var workspaceWaiter = _provider.GetListener(FeatureAttribute.Workspace) as IAsynchronousOperationWaiter;
            var featureWaiter = _provider.GetListener(featureName) as IAsynchronousOperationWaiter;
            Contract.ThrowIfNull(featureWaiter);

            // wait for each of the features specified in the featuresToWaitFor string
            if (waitForWorkspaceFirst)
            {
                // at least wait for the workspace to finish processing everything.
                if (workspaceWaiter != null)
                {
                    var task = workspaceWaiter.CreateWaitTask();
                    task.Wait();
                }
            }

            var waitTask = featureWaiter.CreateWaitTask();

            while (!waitTask.Wait(100))
            {
                // set breakpoint here when debugging
                var tokens = ((AsynchronousOperationListenerProvider)_provider).GetTokens();

                GC.KeepAlive(tokens);
            }

            // Debugging trick: don't let the listeners collection get optimized away during execution.
            // This means if the process is killed during integration tests and the test was waiting, you can
            // easily look at the listeners and see what is going on. This is not actually needed to
            // affect the GC, nor is it needed for correctness.
            GC.KeepAlive(featureWaiter);
        }

        public void WaitForAllAsyncOperations()
        {
            ((AsynchronousOperationListenerProvider)_provider).WaitAll();
        }

        public void EnableActiveTokenTracking(bool enable)
        {
            ((AsynchronousOperationListenerProvider)_provider).Tracking(enable);
        }

        public void Enable(bool enable)
        {
            AsynchronousOperationListenerProvider.Enable(enable);
        }
    }
}
