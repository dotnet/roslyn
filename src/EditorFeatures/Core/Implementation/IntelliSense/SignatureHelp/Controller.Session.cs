// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal partial class Session : Session<Controller, Model, ISignatureHelpPresenterSession>
        {
            // When we issue compute tasks, provide them with a (monotonically increasing) id.  That
            // way, when they run we can bail on computation if they've been superseded by another
            // compute task.
            private int _computeId;

            public Session(Controller controller, ISignatureHelpPresenterSession presenterSession)
                : base(controller, new ModelComputation<Model>(controller, TaskScheduler.Default), presenterSession)
            {
                this.PresenterSession.ItemSelected += OnPresenterSessionItemSelected;
            }

            public override void Stop()
            {
                AssertIsForeground();
                this.PresenterSession.ItemSelected -= OnPresenterSessionItemSelected;
                base.Stop();
            }

            private void OnPresenterSessionItemSelected(object sender, SignatureHelpItemEventArgs e)
            {
                AssertIsForeground();
                Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));

                SetModelExplicitlySelectedItem(m => e.SignatureHelpItem);
            }
        }
    }
}
