// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
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

            private ImmutableArray<Model> SetModelExplicitlySelectedItemInBackground(
                ImmutableArray<Model> models,
                Func<Model, SignatureHelpItem> selector)
            {
                AssertIsBackground();

                if (models == default(ImmutableArray<Model>))
                {
                    return default(ImmutableArray<Model>);
                }

                var model = models[0];
                var selectedItem = selector(model);
                Contract.ThrowIfFalse(model.Items.Contains(selectedItem));

                return new[] { model.WithSelectedItem(selectedItem) }.ToImmutableArray();
            }
        }
    }
}
