// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                AssertIsForeground();

                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    model => SetModelExplicitlySelectedItemInBackground(model, selector),
                    updateController: false);
            }

            private Model SetModelExplicitlySelectedItemInBackground(
                Model model,
                Func<Model, SignatureHelpItem> selector)
            {
                AssertIsBackground();

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
