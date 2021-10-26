// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class LightBulbHelper
    {
        public static async Task<bool> WaitForLightBulbSessionAsync(ILightBulbBroker broker, IWpfTextView view)
        {
            var startTime = DateTimeOffset.Now;

            var active = await Helper.RetryAsync(async () =>
            {
                if (broker.IsLightBulbSessionActive(view))
                {
                    return true;
                }

                if (DateTimeOffset.Now > startTime + Helper.HangMitigatingTimeout)
                {
                    throw new InvalidOperationException("Expected a light bulb session to appear.");
                }

                // checking whether there is any suggested action is async up to editor layer and our waiter doesn't track up to that point.
                // so here, we have no other way than sleep (with timeout) to see LB is available.
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

                return broker.IsLightBulbSessionActive(view);
            }, TimeSpan.Zero);

            if (!active)
                return false;

            await WaitForItemsAsync(broker, view);
            return true;
        }

        public static async Task<IEnumerable<SuggestedActionSet>> WaitForItemsAsync(ILightBulbBroker broker, IWpfTextView view)
        {
            var activeSession = broker.GetSession(view);
            if (activeSession == null)
            {
                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new InvalidOperationException($"No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={bufferType}");
            }

            var start = DateTime.Now;
            while (DateTime.Now - start < Helper.HangMitigatingTimeout)
            {
                var status = activeSession.TryGetSuggestedActionSets(out var actionSets);
                if (status is not QuerySuggestedActionCompletionStatus.Completed and
                              not QuerySuggestedActionCompletionStatus.Canceled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                if (status != QuerySuggestedActionCompletionStatus.Completed)
                    throw new InvalidOperationException($"Querying light bulb for status produced: {status}");

                return actionSets;
            }

            throw new InvalidOperationException($"Light bulb never transitioned to completed state.");
        }
    }
}
