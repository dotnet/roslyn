// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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

                Computation.ChainTaskAndNotifyControllerWhenFinished(models => SetModelBuilderStateInBackground(models, includeBuilder));
            }

            private ImmutableArray<Model> SetModelBuilderStateInBackground(
                ImmutableArray<Model> models,
                bool includeBuilder)
            {
                if (models == default(ImmutableArray<Model>))
                {
                    return default(ImmutableArray<Model>);
                }

                var result = ImmutableArray.CreateBuilder<Model>(models.Length);
                for (int i = 0; i < models.Length; i++)
                {
                    var model = models[i];
                    // We want to soft select if the user is switching the builder on, or if we were
                    // already in soft select mode.
                    var softSelect = includeBuilder || model.IsSoftSelection;

                    // If the selected item is the builder, select the first filtered item instead.
                    if (model.SelectedItem == model.DefaultBuilder)
                    {
                        result.Add(model.WithSelectedItem(model.FilteredItems.First())
                                    .WithHardSelection(!softSelect)
                                    .WithUseSuggestionCompletionMode(includeBuilder));
                    }
                    else
                    {
                        result.Add(model.WithHardSelection(!softSelect).WithUseSuggestionCompletionMode(includeBuilder));
                    }
                }

                return result.ToImmutable();
            }
        }
    }
}
