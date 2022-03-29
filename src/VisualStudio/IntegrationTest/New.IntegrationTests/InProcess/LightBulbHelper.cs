// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal static class LightBulbHelper
    {
        public static async Task<bool> WaitForLightBulbSessionAsync(TestServices testServices, ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
        {
            await testServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
            var active = broker.IsLightBulbSessionActive(view);
            if (!active)
                return false;

            await WaitForItemsAsync(testServices, broker, view, cancellationToken);
            return true;
        }

        public static async Task<IEnumerable<SuggestedActionSet>> WaitForItemsAsync(TestServices testServices, ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
        {
            while (true)
            {
                var items = await TryWaitForItemsAsync(testServices, broker, view, cancellationToken);
                if (items is not null)
                    return items;

                // The session was dismissed unexpectedly. The editor might show it again.
                await testServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
            }
        }

        private static async Task<IEnumerable<SuggestedActionSet>?> TryWaitForItemsAsync(TestServices testServices, ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
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
            asyncSession.PopulateWithData(overrideRequestedActionCategories: null, operationContext: null);

            try
            {
                return await tcs.Task.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var version = await testServices.Shell.GetVersionAsync(cancellationToken);
                if (Version.Parse("17.2.32210.308") >= version)
                {
                    // Unexpected cancellation can occur when the editor dismisses the light bulb without request
                    return null;
                }

                throw new OperationCanceledException($"IDE version '{version}' unexpectedly dismissed the light bulb.");
            }
        }
    }
}
