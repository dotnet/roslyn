// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal sealed class CodeModelEventQueue
{
    private readonly Queue<CodeModelEvent> _eventQueue;

    public CodeModelEventQueue(Queue<CodeModelEvent> eventQueue)
        => _eventQueue = eventQueue;

    private void EnqueueEvent(CodeModelEvent @event)
    {
        // Don't bother with events that are already queued.
        foreach (var queuedEvent in _eventQueue)
        {
            if (queuedEvent.Equals(@event))
            {
                return;
            }
        }

        // Events are added to the end of the queue, so we only check the
        // last event to see if we can combine it with the new event. The
        // other events will be for prior edits, and should remain distinct.
        // In order to combine the events, they must both be for the same node, 
        // and they must both be change events.

        if (_eventQueue.Count > 0)
        {
            var priorEvent = _eventQueue.Peek();
            if (priorEvent.Node == @event.Node &&
                priorEvent.ParentNode == @event.ParentNode &&
                priorEvent.Type.IsChange() &&
                @event.Type.IsChange())
            {
                priorEvent.Type |= @event.Type;
                return;
            }
        }

        _eventQueue.Enqueue(@event);
    }

    public void EnqueueAddEvent(SyntaxNode node, SyntaxNode parent)
        => EnqueueEvent(new CodeModelEvent(node, parent, CodeModelEventType.Add));

    public void EnqueueRemoveEvent(SyntaxNode node, SyntaxNode parent)
        => EnqueueEvent(new CodeModelEvent(node, parent, CodeModelEventType.Remove));

    public void EnqueueChangeEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventType eventType)
        => EnqueueEvent(new CodeModelEvent(node, parent, eventType));

    public void Discard()
        => _eventQueue.Dequeue();

    public int Count
    {
        get { return _eventQueue.Count; }
    }
}
