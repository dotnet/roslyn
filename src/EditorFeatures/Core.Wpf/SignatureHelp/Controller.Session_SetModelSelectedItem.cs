// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal partial class Session
        {
            private void SetModelExplicitlySelectedItem(Func<Model, SignatureHelpItem> selector)
            {
                this.Computation.ThreadingContext.ThrowIfNotOnUIThread();

                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    model => SetModelExplicitlySelectedItemInBackground(model, selector),
                    updateController: false);
            }

            private Model SetModelExplicitlySelectedItemInBackground(
                Model model,
                Func<Model, SignatureHelpItem> selector)
            {
                this.Computation.ThreadingContext.ThrowIfNotOnBackgroundThread();

                if (model == null)
                {
                    return null;
                }

                var selectedItem = selector(model);
                Contract.ThrowIfFalse(model.Items.Contains(selectedItem));

                return model.WithSelectedItem(selectedItem, userSelected: true);
            }
        }
    }
}
