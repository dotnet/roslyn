// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

internal sealed partial class RenameTrackingTaggerProvider
{
    /// <summary>
    /// Clears or restores the state machine on relevant undo/redo actions.
    /// 
    /// These may stay alive on the global undo stack well beyond the lifetime of the
    /// <see cref="ITextBuffer"/> on which they were created, so we must avoid strong
    /// references to anything that may hold that <see cref="ITextBuffer"/> alive.
    /// </summary>
    private class UndoPrimitive(ITextBuffer textBuffer, int trackingSessionId, bool shouldRestoreStateOnUndo) : ITextUndoPrimitive
    {
        private readonly WeakReference<ITextBuffer> _weakTextBuffer = new WeakReference<ITextBuffer>(textBuffer);
        private readonly int _trackingSessionId = trackingSessionId;
        private readonly bool _shouldRestoreStateOnUndo = shouldRestoreStateOnUndo;

        private ITextUndoTransaction _parent;
        public ITextUndoTransaction Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public bool CanRedo => true;

        public bool CanUndo => true;

        public void Do()
        {
            if (TryGetStateMachine(out var stateMachine))
            {
                stateMachine.ClearTrackingSession();
            }
        }

        public void Undo()
        {
            if (TryGetStateMachine(out var stateMachine))
            {
                if (_shouldRestoreStateOnUndo)
                {
                    stateMachine.RestoreTrackingSession(_trackingSessionId);
                }
                else
                {
                    stateMachine.ClearTrackingSession();
                }
            }
        }

        private bool TryGetStateMachine(out StateMachine stateMachine)
        {
            stateMachine = null;
            return _weakTextBuffer.TryGetTarget(out var textBuffer) &&
                textBuffer.Properties.TryGetProperty(typeof(StateMachine), out stateMachine);
        }

        public bool CanMerge(ITextUndoPrimitive older)
            => false;

        public ITextUndoPrimitive Merge(ITextUndoPrimitive older)
            => throw new NotImplementedException();
    }
}
