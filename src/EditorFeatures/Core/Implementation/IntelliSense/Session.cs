﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal class Session<TController, TModel, TPresenterSession> : ForegroundThreadAffinitizedObject, ISession<TModel>
        where TPresenterSession : IIntelliSensePresenterSession
        where TController : IController<TModel>
        where TModel : class
    {
        public TController Controller { get; }
        public ModelComputation<TModel> Computation { get; }

        // The presenter session for the computation we've got going.  It's lifetime is tied 1:1 with
        // the computation.  When the computation starts we make a presenter (note: this does not
        // mean that the user will ever see any UI), and when the computation is stopped, we will
        // end the presentation session.
        public TPresenterSession PresenterSession { get; }

        public Session(TController controller, ModelComputation<TModel> computation, TPresenterSession presenterSession)
            : base(computation.ThreadingContext)
        {
            this.Controller = controller;
            this.Computation = computation;
            this.PresenterSession = presenterSession;

            // If the UI layer dismisses the presenter, then we want to know about it so we can stop
            // doing whatever it is we're doing.
            this.PresenterSession.Dismissed += OnPresenterSessionDismissed;
        }

        public TModel InitialUnfilteredModel { get { return this.Computation.InitialUnfilteredModel; } }

        private void OnPresenterSessionDismissed(object sender, EventArgs e)
        {
            AssertIsForeground();
            Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));
            Controller.StopModelComputation();
        }

        public virtual void Stop()
        {
            AssertIsForeground();
            this.Computation.Stop();
            this.PresenterSession.Dismissed -= OnPresenterSessionDismissed;
            this.PresenterSession.Dismiss();
        }

        public TModel WaitForController()
        {
            AssertIsForeground();
            return Computation.WaitForController();
        }
    }
}
