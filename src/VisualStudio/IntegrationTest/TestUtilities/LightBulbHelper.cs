// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class LightBulbHelper
    {
        public static Task<bool> WaitForLightBulbSessionAsync(ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.Now;

            return Helper.RetryAsync(async () =>
            {
                if (broker.IsLightBulbSessionActive(view))
                {
                    return true;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new InvalidOperationException("Expected a light bulb session to appear.");
                }

                // checking whether there is any suggested action is async up to editor layer and our waiter doesn't track up to that point.
                // so here, we have no other way than sleep (with timeout) to see LB is available.
                await Task.Delay(TimeSpan.FromSeconds(1));

                return broker.IsLightBulbSessionActive(view);
            }, TimeSpan.Zero);
        }
    }
}
