// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
                throw new InvalidOperationException(string.Format("No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={0}", bufferType));
            }

            var asyncSession = (IAsyncLightBulbSession)activeSession;
            var tcs = new TaskCompletionSource<List<SuggestedActionSet>>();

            var delayTask = Task.Delay(Helper.HangMitigatingTimeout);

            EventHandler<SuggestedActionsUpdatedArgs>? handler = null;
            handler = (s, e) =>
            {
                // ignore these.  we care about when the lightbulb items are all completed.
                if (e.Status == QuerySuggestedActionCompletionStatus.InProgress)
                    return;

                if (e.Status == QuerySuggestedActionCompletionStatus.Completed)
                    tcs.SetResult(e.ActionSets.ToList());
                else
                    tcs.SetException(new InvalidOperationException($"Light bulb transitioned to non-complete state: {e.Status}"));

                asyncSession.SuggestedActionsUpdated -= handler;
            };

            asyncSession.SuggestedActionsUpdated += handler;

            asyncSession.PopulateWithData(overrideRequestedActionCategories: null, operationContext: null);
            var completedTask = await Task.WhenAny(tcs.Task, delayTask).ConfigureAwait(false);

            if (completedTask == delayTask)
                throw new InvalidOperationException("Timeout before getting all lightbulb items populated");

            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
