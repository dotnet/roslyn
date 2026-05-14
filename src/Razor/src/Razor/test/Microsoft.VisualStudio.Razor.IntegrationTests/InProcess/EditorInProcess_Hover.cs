// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    public async Task<IEnumerable<object>> HoverAsync(int position, CancellationToken cancellationToken)
    {
        var hoverService = await GetComponentModelServiceAsync<IAsyncQuickInfoBroker>(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var trackingPoint = view.TextSnapshot.CreateTrackingPoint(position, PointTrackingMode.Positive);
        var quickInfoSession = await hoverService.TriggerQuickInfoAsync(view, trackingPoint, QuickInfoSessionOptions.None, cancellationToken);

        using var semaphore = new SemaphoreSlim(1);
        await semaphore.WaitAsync(cancellationToken);

        if (quickInfoSession is null)
        {
            throw new InvalidOperationException("Quick Info Session failed to launch.");
        }

        quickInfoSession.StateChanged += QuickInfoSession_StateChanged;

        if (QuickInfoResolved(quickInfoSession))
        {
            semaphore.Release();
            quickInfoSession.StateChanged -= QuickInfoSession_StateChanged;
        }

        try
        {
            await semaphore.WaitAsync(cancellationToken);
        }
        finally
        {
            quickInfoSession.StateChanged -= QuickInfoSession_StateChanged;
        }

        return quickInfoSession.Content;

        static bool QuickInfoResolved(IAsyncQuickInfoSession quickInfoSession)
        {
            return quickInfoSession.State == QuickInfoSessionState.Visible;
        }

        void QuickInfoSession_StateChanged(object sender, QuickInfoSessionStateChangedEventArgs e)
        {
            if (e.NewState == QuickInfoSessionState.Visible)
            {
                semaphore.Release();
            }
        }
    }

    public async Task<string> GetHoverStringAsync(int position, CancellationToken cancellationToken)
    {
        var hoverContent = await HoverAsync(position, cancellationToken);

        using var _ = StringBuilderPool.GetPooledObject(out var sb);

        TraverseContent(hoverContent, sb);
        return sb.ToString();

        static void TraverseContent(IEnumerable<object> objects, StringBuilder stringBuilder)
        {
            foreach (var obj in objects)
            {
                switch (obj)
                {
                    case ImageElement imageElement:
                        break;
                    case ClassifiedTextElement textElement:
                        foreach (var run in textElement.Runs)
                        {
                            stringBuilder.Append(run.Text);
                        }

                        break;
                    case ContainerElement containerElement:
                        TraverseContent(containerElement.Elements, stringBuilder);
                        break;
                    default:
                        throw new NotImplementedException("Unknown QuickInfo element encountered");
                }
            }
        }
    }
}

