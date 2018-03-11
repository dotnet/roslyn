// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    /// <summary>
    /// A little helper to produce indexes when producing NavigationBarItems when we have multiple
    /// symbols with the same symbol ID.
    /// </summary>
    internal class NavigationBarSymbolIdIndexProvider
    {
        private readonly Dictionary<SymbolKey, int> _nextIds;

        public NavigationBarSymbolIdIndexProvider(bool caseSensitive)
        {
            _nextIds = new Dictionary<SymbolKey, int>(caseSensitive
                ? SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false)
                : SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false));
        }

        public int GetIndexForSymbolId(SymbolKey id)
        {
            _nextIds.TryGetValue(id, out var nextId);
            _nextIds[id] = nextId + 1;
            return nextId;
        }
    }
}
