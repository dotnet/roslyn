// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
{
    internal class MockNavigableItemsPresenter : INavigableItemsPresenter
    {
        public ImmutableArray<INavigableItem> Items { get; private set; }

        public void DisplayResult(string title, ImmutableArray<INavigableItem> items)
        {
            this.Items = items;
        }
    }
}