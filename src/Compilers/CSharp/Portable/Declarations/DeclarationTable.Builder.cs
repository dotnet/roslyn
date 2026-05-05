// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class DeclarationTable
{
    internal sealed class Builder
    {
        private static readonly ObjectPool<Builder> s_builderPool = new ObjectPool<Builder>(() => new Builder());

        private DeclarationTable _table;
        private readonly List<Lazy<RootSingleNamespaceDeclaration>> _addedLazyRootDeclarations;
        private readonly List<Lazy<RootSingleNamespaceDeclaration>> _removedLazyRootDeclarations;

        private Builder()
        {
            _table = DeclarationTable.Empty;
            _addedLazyRootDeclarations = new List<Lazy<RootSingleNamespaceDeclaration>>();
            _removedLazyRootDeclarations = new List<Lazy<RootSingleNamespaceDeclaration>>();
        }

        public static Builder GetInstance(DeclarationTable table)
        {
            var builder = s_builderPool.Allocate();

            builder._table = table;

            return builder;
        }

        public void AddRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We don't allow intermingled add/remove operations. Realize any pending removes first.
            RealizeRemoves();

            _addedLazyRootDeclarations.Add(lazyRootDeclaration);
        }

        public void RemoveRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We don't allow intermingled add/remove operations. Realize any pending adds first.
            RealizeAdds();

            _removedLazyRootDeclarations.Add(lazyRootDeclaration);
        }

        public DeclarationTable ToDeclarationTableAndFree()
        {
            RealizeAdds();
            RealizeRemoves();

            var result = _table;

            _table = DeclarationTable.Empty;
            s_builderPool.Free(this);

            return result;
        }

        private void RealizeAdds()
        {
            if (_addedLazyRootDeclarations.Count == 0)
            {
                return;
            }

            var lastDeclaration = _addedLazyRootDeclarations[_addedLazyRootDeclarations.Count - 1];
            if (_addedLazyRootDeclarations.Count == 1)
            {
                // We can only re-use the cache if we don't already have a 'latest' item for the decl table.
                if (_table._latestLazyRootDeclaration == null)
                {
                    _table = new DeclarationTable(_table._allOlderRootDeclarations, lastDeclaration, _table._cache);
                }
                else
                {
                    // we already had a 'latest' item.  This means we're hearing about a change to a
                    // different tree.  Add old latest item to the 'oldest' collection
                    // and don't reuse the cache.
                    _table = new DeclarationTable(_table._allOlderRootDeclarations.Add(_table._latestLazyRootDeclaration), lastDeclaration, cache: null);
                }
            }
            else
            {
                _addedLazyRootDeclarations.RemoveAt(_addedLazyRootDeclarations.Count - 1);

                if (_table._latestLazyRootDeclaration != null)
                {
                    _addedLazyRootDeclarations.Insert(0, _table._latestLazyRootDeclaration);
                }

                var newOlderRootDeclarations = _table._allOlderRootDeclarations.AddRange(_addedLazyRootDeclarations);

                _table = new DeclarationTable(newOlderRootDeclarations, lastDeclaration, cache: null);
            }

            _addedLazyRootDeclarations.Clear();
        }

        private void RealizeRemoves()
        {
            if (_removedLazyRootDeclarations.Count == 0)
            {
                return;
            }

            if (_removedLazyRootDeclarations.Count == 1)
            {
                var firstDeclaration = _removedLazyRootDeclarations[0];

                // We can only reuse the cache if we're removing the decl that was just added.
                if (_table._latestLazyRootDeclaration == firstDeclaration)
                {
                    _table = new DeclarationTable(_table._allOlderRootDeclarations, latestLazyRootDeclaration: null, cache: _table._cache);
                }
                else
                {
                    // We're removing a different tree than the latest one added.  We need
                    // to remove the passed in root from our 'older' list.  We also can't reuse the
                    // cache.
                    //
                    // Note: we can keep around the 'latestLazyRootDeclaration'.
                    _table = new DeclarationTable(_table._allOlderRootDeclarations.Remove(firstDeclaration), _table._latestLazyRootDeclaration, cache: null);
                }
            }
            else
            {
                var isLatestRemoved = _table._latestLazyRootDeclaration != null && _removedLazyRootDeclarations.Contains(_table._latestLazyRootDeclaration);

                var newOlderRootDeclarations = _table._allOlderRootDeclarations.RemoveRange(_removedLazyRootDeclarations);
                var newLatestLazyRootDeclaration = isLatestRemoved ? null : _table._latestLazyRootDeclaration;

                _table = new DeclarationTable(newOlderRootDeclarations, newLatestLazyRootDeclaration, cache: null);
            }

            _removedLazyRootDeclarations.Clear();
        }
    }
}
