// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

            public void SetModelIsHardSelection(bool isHardSelection)
            {
                AssertIsForeground();

                Computation.ChainTaskAndNotifyControllerWhenFinished(model => model?.WithHardSelection(isHardSelection));
            }

            private Model SetModelSelectedItemInBackground(
                Model model,
                Func<Model, CompletionItem> selector)
            {
                if (model == null)
                {
                    return null;
                }

                // Switch to hard selection.
                var selectedItem = selector(model);
                Contract.ThrowIfFalse(model.TotalItems.Contains(selectedItem) || model.DefaultBuilder == selectedItem);

                if (model.FilteredItems.Contains(selectedItem))
                {
                    // Easy case, just set the selected item that's already in the filtered items
                    // list.

                    return model.WithSelectedItem(selector(model))
                                .WithHardSelection(true);
                }
                else
                {
                    // Item wasn't in the filtered list, so we need to recreate the filtered list
                    // with that item in it.
                    var filteredItemsSet = new HashSet<CompletionItem>(model.FilteredItems,
                        ReferenceEqualityComparer.Instance);

                    var newFilteredItems = model.TotalItems.Where(
                        i => filteredItemsSet.Contains(i) || i == selectedItem).ToList();
                    return model.WithFilteredItems(newFilteredItems)
                                .WithSelectedItem(selectedItem)
                                .WithHardSelection(true);
                }
            }
        }
    }
}
