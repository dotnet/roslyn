// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
