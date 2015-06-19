// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        /// <summary>
        /// Clears or restores the state machine on relevant undo/redo actions.
        /// 
        /// These may stay alive on the global undo stack well beyond the lifetime of the
        /// <see cref="ITextBuffer"/> on which they were created, so we must avoid strong
        /// references to anything that may hold that <see cref="ITextBuffer"/> alive.
        /// </summary>
        private class UndoPrimitive : ITextUndoPrimitive
        {
            private readonly WeakReference<ITextBuffer> _weakTextBuffer;
            private readonly int _trackingSessionId;
            private readonly bool _shouldRestoreStateOnUndo;

            private ITextUndoTransaction _parent;
            public ITextUndoTransaction Parent
            {
                get { return _parent; }
                set { _parent = value; }
            }

            public bool CanRedo
            {
                get { return true; }
            }

            public bool CanUndo
            {
                get { return true; }
            }

            public UndoPrimitive(ITextBuffer textBuffer, int trackingSessionId, bool shouldRestoreStateOnUndo)
            {
                _weakTextBuffer = new WeakReference<ITextBuffer>(textBuffer);
                _trackingSessionId = trackingSessionId;
                _shouldRestoreStateOnUndo = shouldRestoreStateOnUndo;
            }

            public void Do()
            {
                StateMachine stateMachine;
                if (TryGetStateMachine(out stateMachine))
                {
                    stateMachine.ClearTrackingSession();
                }
            }

            public void Undo()
            {
                StateMachine stateMachine;
                if (TryGetStateMachine(out stateMachine))
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
                ITextBuffer textBuffer;
                return _weakTextBuffer.TryGetTarget(out textBuffer) &&
                    textBuffer.Properties.TryGetProperty(typeof(StateMachine), out stateMachine);
            }

            public bool CanMerge(ITextUndoPrimitive older)
            {
                return false;
            }

            public ITextUndoPrimitive Merge(ITextUndoPrimitive older)
            {
                throw new NotImplementedException();
            }
        }
    }
}
