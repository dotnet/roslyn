// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal abstract class AbstractController<TSession, TModel, TPresenterSession, TEditorSession> : ForegroundThreadAffinitizedObject, IController<TModel>
        where TSession : class, ISession<TModel>
        where TPresenterSession : IIntelliSensePresenterSession
    {
        protected readonly ITextView TextView;
        protected readonly ITextBuffer SubjectBuffer;
        protected readonly IIntelliSensePresenter<TPresenterSession, TEditorSession> Presenter;
        protected readonly IDocumentProvider DocumentProvider;

        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly string _asyncOperationId;

        // Null when we absolutely know we don't have any sort of item computation going on. Non
        // null the moment we think we start computing state. Null again once we decide we can
        // stop.
        protected TSession sessionOpt;

        protected bool IsSessionActive { get { return sessionOpt != null; } }

        public AbstractController(ITextView textView, ITextBuffer subjectBuffer, IIntelliSensePresenter<TPresenterSession, TEditorSession> presenter, IAsynchronousOperationListener asyncListener, IDocumentProvider documentProvider, string asyncOperationId)
        {
            this.TextView = textView;
            this.SubjectBuffer = subjectBuffer;
            this.Presenter = presenter;
            _asyncListener = asyncListener;
            this.DocumentProvider = documentProvider;
            _asyncOperationId = asyncOperationId;

            this.TextView.Closed += OnTextViewClosed;

            // Caret position changed only fires if the caret is explicitly moved.  It doesn't fire
            // when the caret is moved because of text change events.
            this.TextView.Caret.PositionChanged += this.OnCaretPositionChanged;
            this.TextView.TextBuffer.PostChanged += this.OnTextViewBufferPostChanged;
        }

        internal abstract void OnModelUpdated(TModel result);
        internal abstract void OnTextViewBufferPostChanged(object sender, EventArgs e);
        internal abstract void OnCaretPositionChanged(object sender, EventArgs e);

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            AssertIsForeground();
            DismissSessionIfActive();

            this.TextView.Closed -= OnTextViewClosed;
            this.TextView.Caret.PositionChanged -= this.OnCaretPositionChanged;
            this.TextView.TextBuffer.PostChanged -= this.OnTextViewBufferPostChanged;
        }

        public TModel WaitForController()
        {
            AssertIsForeground();
            VerifySessionIsActive();
            return sessionOpt.WaitForController();
        }

        void IController<TModel>.OnModelUpdated(TModel result)
        {
            // This is only called from the model computation if it was not cancelled.  And if it was 
            // not cancelled then we must have a pointer to it (as well as the presenter session).
            AssertIsForeground();
            VerifySessionIsActive();

            this.OnModelUpdated(result);
        }

        IAsyncToken IController<TModel>.BeginAsyncOperation(string name, object tag, string filePath, int lineNumber)
        {
            AssertIsForeground();
            VerifySessionIsActive();
            name = String.IsNullOrEmpty(name)
                ? _asyncOperationId
                : $"{_asyncOperationId} - {name}";
            return _asyncListener.BeginAsyncOperation(name, tag, filePath: filePath, lineNumber: lineNumber);
        }

        protected void VerifySessionIsActive()
        {
            AssertIsForeground();
            Contract.ThrowIfFalse(IsSessionActive);
        }

        protected void VerifySessionIsInactive()
        {
            AssertIsForeground();
            Contract.ThrowIfTrue(IsSessionActive);
        }

        protected void DismissSessionIfActive()
        {
            AssertIsForeground();
            if (IsSessionActive)
            {
                this.StopModelComputation();
            }
        }

        public void StopModelComputation()
        {
            AssertIsForeground();
            VerifySessionIsActive();

            // Make a local copy so that we won't do anything that causes us to recurse and try to
            // dismiss this again.
            var localSession = sessionOpt;
            sessionOpt = null;
            localSession.Stop();
        }

        public bool TryHandleEscapeKey()
        {
            AssertIsForeground();

            // Escape simply dismissed a session if it's up. Otherwise let the next thing in the
            // chain handle us.
            if (!IsSessionActive)
            {
                return false;
            }

            // If we haven't even computed a model yet, then also send this command to anyone
            // listening.  It's unlikely that the command was intended for us (as we wouldn't
            // have even shown ui yet.
            var handledCommand = sessionOpt.InitialUnfilteredModel != null;

            // In the presence of an escape, we always stop what we're doing.
            this.StopModelComputation();

            return handledCommand;
        }
    }
}
