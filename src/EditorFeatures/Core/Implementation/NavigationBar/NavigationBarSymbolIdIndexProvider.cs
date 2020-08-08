// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
