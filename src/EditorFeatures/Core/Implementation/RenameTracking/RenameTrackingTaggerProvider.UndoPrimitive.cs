// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        /// <summary>
        /// Clears the state machine on relevant undo/redo actions.
        /// </summary>
        private class UndoPrimitive : ITextUndoPrimitive
        {
            private readonly StateMachine _stateMachine;
            private readonly TrackingSession _trackingSession;
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

            public UndoPrimitive(StateMachine stateMachine, bool shouldRestoreStateOnUndo)
            {
                _stateMachine = stateMachine;
                _trackingSession = shouldRestoreStateOnUndo ? stateMachine.TrackingSession : null;
            }

            public void Do()
            {
                _stateMachine.ClearTrackingSession();
            }

            public void Undo()
            {
                if (_trackingSession != null)
                {
                    _stateMachine.RestoreTrackingSession(_trackingSession);
                }
                else
                {
                    _stateMachine.ClearTrackingSession();
                }
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
