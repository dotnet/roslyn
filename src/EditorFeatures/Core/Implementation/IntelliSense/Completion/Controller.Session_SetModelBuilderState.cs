// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            public void SetModelBuilderState(bool includeBuilder)
            {
                AssertIsForeground();

                Computation.ChainTaskAndNotifyControllerWhenFinished(model => SetModelBuilderStateInBackground(model, includeBuilder));
            }

            private Model SetModelBuilderStateInBackground(
                Model model,
                bool includeBuilder)
            {
                if (model == null)
                {
                    return null;
                }

                // We want to soft select if the user is switching the builder on, or if we were
                // already in soft select mode.
                var softSelect = includeBuilder || model.IsSoftSelection;

                // If the selected item is the builder, select the first filtered item instead.
                if (model.SelectedItem == model.DefaultSuggestionModeItem)
                {
                    return model.WithSelectedItem(model.FilteredItems.First())
                                .WithHardSelection(!softSelect);
                }

                return model.WithHardSelection(!softSelect).WithUseSuggestionMode(includeBuilder);
            }
        }
    }
}
