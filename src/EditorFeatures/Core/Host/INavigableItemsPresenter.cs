// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface INavigableItemsPresenter
    {
        void DisplayResult(string title, IEnumerable<INavigableItem> items);
    }
}
