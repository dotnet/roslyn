// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal partial class Session : Session<Controller, Model, ISignatureHelpPresenterSession>
        {
            public Session(Controller controller, ISignatureHelpPresenterSession presenterSession)
                : base(controller, new ModelComputation<Model>(controller.ThreadingContext, controller, TaskScheduler.Default), presenterSession)
            {
                this.PresenterSession.ItemSelected += OnPresenterSessionItemSelected;
            }

            public override void Stop()
            {
                this.Computation.ThreadingContext.ThrowIfNotOnUIThread();
                this.PresenterSession.ItemSelected -= OnPresenterSessionItemSelected;
                base.Stop();
            }

            private void OnPresenterSessionItemSelected(object sender, SignatureHelpItemEventArgs e)
            {
                this.Computation.ThreadingContext.ThrowIfNotOnUIThread();
                Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));

                SetModelExplicitlySelectedItem(m => e.SignatureHelpItem);
            }
        }
    }
}
