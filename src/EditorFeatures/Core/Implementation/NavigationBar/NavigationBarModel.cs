// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    internal sealed class NavigationBarModel : IEquatable<NavigationBarModel>
    {
        public INavigationBarItemService ItemService { get; }
        public ImmutableArray<NavigationBarItem> Types { get; }

        public NavigationBarModel(INavigationBarItemService itemService, ImmutableArray<NavigationBarItem> types)
        {
            ItemService = itemService;
            Types = types;
        }

        public override bool Equals(object? obj)
            => Equals(obj as NavigationBarModel);

        public bool Equals(NavigationBarModel? other)
            => other != null && Types.SequenceEqual(other.Types);

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}
