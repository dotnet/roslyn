// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            private void SetModelSelectedItem(Func<Model, CompletionItem> selector)
            {
                AssertIsForeground();

                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    model => SetModelSelectedItemInBackground(model, selector),
                    updateController: false);
            }

            private ImmutableArray<Model> SetModelSelectedItemInBackground(
                ImmutableArray<Model> models,
                Func<Model, CompletionItem> selector)
            {
                if (models == default(ImmutableArray<Model>))
                {
                    return default(ImmutableArray<Model>);
                }

                var result = ImmutableArray.CreateBuilder<Model>(models.Length);
                for (int i = 0; i < models.Length; i++)
                {
                    var model = models[i];

                    if (!model.IsSelected)
                    {
                        result.Add(models[i]);
                        continue;
                    }

                    // Switch to hard selection.
                    var selectedItem = selector(model);
                    Contract.ThrowIfFalse(model.TotalItems.Contains(selectedItem) || model.DefaultBuilder == selectedItem);

                    if (model.FilteredItems.Contains(selectedItem))
                    {
                        // Easy case, just set the selected item that's already in the filtered items
                        // list.

                        result.Add(model.WithSelectedItem(selector(model))
                                    .WithHardSelection(true));
                    }
                    else
                    {
                        // Item wasn't in the filtered list, so we need to recreate the filtered list
                        // with that item in it.
                        var filteredItemsSet = new HashSet<CompletionItem>(model.FilteredItems,
                            ReferenceEqualityComparer.Instance);

                        var newFilteredItems = model.TotalItems.Where(
                            j => filteredItemsSet.Contains(j) || j == selectedItem).ToList();
                        result.Add(model.WithFilteredItems(newFilteredItems)
                                    .WithSelectedItem(selectedItem)
                                    .WithHardSelection(true));
                    }
                }

                return result.ToImmutable();
            }
        }
    }
}
