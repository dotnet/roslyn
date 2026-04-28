// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;

public static class LightBulbHelper
{
    public static async Task<bool> WaitForLightBulbSessionAsync(ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.Now;

        var active = await Helper.RetryAsync(async cancellationToken =>
        {
            if (broker.IsLightBulbSessionActive(view))
            {
                return true;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("Expected a light bulb session to appear.");
            }

            if (broker.IsLightBulbSessionActive(view))
            {
                var session = broker.GetSession(view);
                var hasSuggestedActions = await broker.HasSuggestedActionsAsync(session.ActionCategories, view, cancellationToken);

                return hasSuggestedActions;
            }

            return false;
        }, TimeSpan.FromMilliseconds(1), cancellationToken);

        if (!active)
            return false;

        await WaitForItemsAsync(broker, view, cancellationToken);
        return true;
    }

    public static async Task<IEnumerable<SuggestedActionSet>> WaitForItemsAsync(ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
    {
        var activeSession = broker.GetSession(view);
        if (activeSession is null)
        {
            var bufferType = view.TextBuffer.ContentType.DisplayName;
            throw new InvalidOperationException($"No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={bufferType}");
        }

        var asyncSession = (IAsyncLightBulbSession)activeSession;
        var tcs = new TaskCompletionSource<List<SuggestedActionSet>>();

        void Handler(object s, SuggestedActionsUpdatedArgs e)
        {
            // ignore these.  we care about when the lightbulb items are all completed.
            if (e.Status == QuerySuggestedActionCompletionStatus.InProgress)
                return;

            if (e.Status == QuerySuggestedActionCompletionStatus.Completed || e.Status == QuerySuggestedActionCompletionStatus.CompletedWithoutData)
                tcs.SetResult(e.ActionSets.ToList());
            else
                tcs.SetException(new InvalidOperationException($"Light bulb transitioned to non-complete state: {e.Status}"));

            asyncSession.SuggestedActionsUpdated -= Handler;
        }

        asyncSession.SuggestedActionsUpdated += Handler;

        asyncSession.Dismissed += (_, _) => tcs.TrySetCanceled(new CancellationToken(true));

        if (asyncSession.IsDismissed)
            tcs.TrySetCanceled(new CancellationToken(true));

        // Calling PopulateWithDataAsync ensures the underlying session will call SuggestedActionsUpdated at least once
        // with the latest data computed.  This is needed so that if the lightbulb computation is already complete
        // that we hear about the results.
        await asyncSession.PopulateWithDataAsync(overrideRequestedActionCategories: null, operationContext: null).WithCancellation(cancellationToken).ConfigureAwait(false);

        return await tcs.Task.WithCancellation(cancellationToken);
    }
}
