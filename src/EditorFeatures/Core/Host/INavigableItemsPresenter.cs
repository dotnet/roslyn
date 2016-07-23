// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface INavigableItemsPresenter
    {
        void DisplayResult(string title, ImmutableArray<INavigableItem> items);
    }

    internal static class INavigableItemsPresenterExtensions
    {
        public static void DisplayResult(
            this INavigableItemsPresenter presenter,
            ImmutableArray<INavigableItem> items)
        {
            var title = items.Length == 0 ? "None" : items[0].DisplayTaggedParts.JoinText();
            presenter.DisplayResult(title, items);
        }
    }
}