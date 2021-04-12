﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    internal sealed class NavigationBarModel
    {
        public IList<NavigationBarItem> Types { get; }

        /// <summary>
        /// The VersionStamp of the project when this model was computed.
        /// </summary>
        public VersionStamp SemanticVersionStamp { get; }

        public INavigationBarItemService ItemService { get; }

        public NavigationBarModel(IList<NavigationBarItem> types, VersionStamp semanticVersionStamp, INavigationBarItemService itemService)
        {
            Contract.ThrowIfNull(types);

            this.Types = types;
            this.SemanticVersionStamp = semanticVersionStamp;
            this.ItemService = itemService;
        }
    }
}
