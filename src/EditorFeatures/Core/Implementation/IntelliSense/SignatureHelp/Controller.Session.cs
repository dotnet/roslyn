// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal partial class Session : Session<Controller, Model, ISignatureHelpPresenterSession>
        {
            /// <summary>
            /// When ther user moves the caret we issue retrigger commands.  There may be a long
            /// chain of these, and they may take time for each to process.  This can be visible 
            /// to the user as a long delay before the signature help items update.  To avoid this
            /// we keep track if there is new outstanding retrigger command and we bail on the
            /// computation if another is in the queue.
            /// </summary>
            private int _retriggerId;

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
