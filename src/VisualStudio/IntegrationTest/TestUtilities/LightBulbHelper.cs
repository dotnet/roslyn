// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

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
            using var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);
            var editor = Editor_InProc.Create();
            while (true)
            {
                var items = await TryWaitForItemsAsync(broker, view, cancellationTokenSource.Token);
                if (items is not null)
                    return items;

                // The session was dismissed unexpectedly. The editor might show it again.
                editor.WaitForEditorOperations(Helper.HangMitigatingTimeout);
            }
        }

        private static async Task<IEnumerable<SuggestedActionSet>?> TryWaitForItemsAsync(ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
        {
            var activeSession = broker.GetSession(view);
            if (activeSession == null)
            {
                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new InvalidOperationException($"No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={bufferType}");
            }

            var asyncSession = (IAsyncLightBulbSession)activeSession;
            var tcs = new TaskCompletionSource<List<SuggestedActionSet>>();

            EventHandler<SuggestedActionsUpdatedArgs>? handler = null;
            handler = (s, e) =>
            {
                // ignore these.  we care about when the lightbulb items are all completed.
                if (e.Status == QuerySuggestedActionCompletionStatus.InProgress)
                    return;

                if (e.Status == QuerySuggestedActionCompletionStatus.Completed)
                    tcs.SetResult(e.ActionSets.ToList());
                else if (e.Status == QuerySuggestedActionCompletionStatus.CompletedWithoutData)
                    tcs.SetResult(new List<SuggestedActionSet>());
                else if (e.Status == QuerySuggestedActionCompletionStatus.Canceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetException(new InvalidOperationException($"Light bulb transitioned to non-complete state: {e.Status}"));

                asyncSession.SuggestedActionsUpdated -= handler;
            };

            asyncSession.SuggestedActionsUpdated += handler;

            asyncSession.Dismissed += (_, _) => tcs.TrySetCanceled(new CancellationToken(true));

            if (asyncSession.IsDismissed)
                tcs.TrySetCanceled(new CancellationToken(true));

            // Calling PopulateWithData ensures the underlying session will call SuggestedActionsUpdated at least once
            // with the latest data computed.  This is needed so that if the lightbulb computation is already complete
            // that we hear about the results.
            await asyncSession.PopulateWithDataAsync(overrideRequestedActionCategories: null, operationContext: null).ConfigureAwait(false);

            try
            {
                return await tcs.Task.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var shell = Shell_InProc.Create();
                var version = shell.GetVersion();
                if (Version.Parse("17.2.32427.441") >= version)
                {
                    // Unexpected cancellation can occur when the editor dismisses the light bulb without request
                    return null;
                }

                throw new OperationCanceledException($"IDE version '{version}' unexpectedly dismissed the light bulb.");
            }
        }
    }
}
