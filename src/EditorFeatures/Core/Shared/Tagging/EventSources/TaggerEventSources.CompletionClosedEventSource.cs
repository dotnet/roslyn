// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class CompletionClosedEventSource : AbstractTaggerEventSource
        {
            private readonly IIntellisenseSessionStack _sessionStack;
            private readonly HashSet<ICompletionSession> _trackedSessions = new HashSet<ICompletionSession>();

            public CompletionClosedEventSource(
                IIntellisenseSessionStack sessionStack,
                TaggerDelay delay)
                : base(delay)
            {
                Contract.ThrowIfNull(sessionStack);

                _sessionStack = sessionStack;
            }

            public override void Connect()
            {
                _sessionStack.Sessions.OfType<ICompletionSession>().Do(HookCompletionSessionEvents);
                ((INotifyCollectionChanged)_sessionStack.Sessions).CollectionChanged += OnSessionStackCollectionChanged;
            }

            public override void Disconnect()
            {
                ((INotifyCollectionChanged)_sessionStack.Sessions).CollectionChanged -= OnSessionStackCollectionChanged;
                _sessionStack.Sessions.OfType<ICompletionSession>().Do(UnhookCompletionSessionEvents);
            }

            private void OnSessionStackCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        e.NewItems.OfType<ICompletionSession>().Do(HookCompletionSessionEvents);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        e.OldItems.OfType<ICompletionSession>().Do(UnhookCompletionSessionEvents);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        e.OldItems.OfType<ICompletionSession>().Do(UnhookCompletionSessionEvents);
                        e.NewItems.OfType<ICompletionSession>().Do(HookCompletionSessionEvents);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        _trackedSessions.Do(UnhookCompletionSessionEvents);
                        _sessionStack.Sessions.OfType<ICompletionSession>().Do(HookCompletionSessionEvents);
                        break;
                }
            }

            private void HookCompletionSessionEvents(ICompletionSession session)
            {
                if (_trackedSessions.Add(session))
                {
                    session.Committed += OnIntellisenseSessionCommitted;
                    session.Dismissed += OnIntellisenseSessionDismissed;

                    // If this is the first session that came up, then pause UI updates.
                    if (_trackedSessions.Count == 1)
                    {
                        this.RaiseUIUpdatesPaused();
                    }
                }
            }

            private void UnhookCompletionSessionEvents(ICompletionSession session)
            {
                if (_trackedSessions.Remove(session))
                {
                    session.Committed -= OnIntellisenseSessionCommitted;
                    session.Dismissed -= OnIntellisenseSessionDismissed;

                    // If the last session goes away, then we can resume UI updates.
                    if (_trackedSessions.Count == 0)
                    {
                        this.RaiseUIUpdatesResumed();
                    }
                }
            }

            private void OnIntellisenseSessionCommitted(object sender, EventArgs e)
            {
                UnhookCompletionSessionEvents((ICompletionSession)sender);
            }

            private void OnIntellisenseSessionDismissed(object sender, EventArgs e)
            {
                UnhookCompletionSessionEvents((ICompletionSession)sender);
            }
        }
    }
}
