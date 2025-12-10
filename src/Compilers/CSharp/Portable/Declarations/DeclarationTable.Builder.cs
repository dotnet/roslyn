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
    public Builder ToBuilder()
    {
        return Builder.GetInstance(this);
    }

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
            // We don't allow intermingled add/remove operations. Use the builder for one or the other.
            Debug.Assert(_removedLazyRootDeclarations.Count == 0);

            _addedLazyRootDeclarations.Add(lazyRootDeclaration);
        }

        public void RemoveRootDeclaration(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We don't allow intermingled add/remove operations. Use the builder for one or the other.
            Debug.Assert(_addedLazyRootDeclarations.Count == 0);

            _removedLazyRootDeclarations.Add(lazyRootDeclaration);
        }

        public DeclarationTable ToDeclarationTableAndFree()
        {
            var result = (_addedLazyRootDeclarations.Count > 0)
                ? Add(_addedLazyRootDeclarations)
                : Remove(_removedLazyRootDeclarations);

            _addedLazyRootDeclarations.Clear();
            _removedLazyRootDeclarations.Clear();
            _table = DeclarationTable.Empty;
            s_builderPool.Free(this);

            return result;
        }

        private DeclarationTable Add(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We can only re-use the cache if we don't already have a 'latest' item for the decl
            // table.
            if (_table._latestLazyRootDeclaration == null)
            {
                return new DeclarationTable(_table._allOlderRootDeclarations, lazyRootDeclaration, _table._cache);
            }
            else
            {
                // we already had a 'latest' item.  This means we're hearing about a change to a
                // different tree.  Add old latest item to the 'oldest' collection
                // and don't reuse the cache.
                return new DeclarationTable(_table._allOlderRootDeclarations.Add(_table._latestLazyRootDeclaration), lazyRootDeclaration, cache: null);
            }
        }

        private DeclarationTable Add(List<Lazy<RootSingleNamespaceDeclaration>> lazyRootDeclarations)
        {
            if (lazyRootDeclarations.Count == 0)
            {
                return _table;
            }

            if (lazyRootDeclarations.Count == 1)
            {
                return Add(lazyRootDeclarations[0]);
            }

            var lastDeclaration = lazyRootDeclarations[lazyRootDeclarations.Count - 1];
            lazyRootDeclarations.RemoveAt(lazyRootDeclarations.Count - 1);

            if (_table._latestLazyRootDeclaration != null)
            {
                lazyRootDeclarations.Insert(0, _table._latestLazyRootDeclaration);
            }

            var newOlderRootDeclarations = _table._allOlderRootDeclarations.AddRange(lazyRootDeclarations);

            return new DeclarationTable(newOlderRootDeclarations, lastDeclaration, cache: null);
        }

        private DeclarationTable Remove(Lazy<RootSingleNamespaceDeclaration> lazyRootDeclaration)
        {
            // We can only reuse the cache if we're removing the decl that was just added.
            if (_table._latestLazyRootDeclaration == lazyRootDeclaration)
            {
                return new DeclarationTable(_table._allOlderRootDeclarations, latestLazyRootDeclaration: null, cache: _table._cache);
            }
            else
            {
                // We're removing a different tree than the latest one added.  We need
                // to remove the passed in root from our 'older' list.  We also can't reuse the
                // cache.
                //
                // Note: we can keep around the 'latestLazyRootDeclaration'.
                return new DeclarationTable(_table._allOlderRootDeclarations.Remove(lazyRootDeclaration), _table._latestLazyRootDeclaration, cache: null);
            }
        }

        private DeclarationTable Remove(List<Lazy<RootSingleNamespaceDeclaration>> lazyRootDeclarations)
        {
            if (lazyRootDeclarations.Count == 0)
            {
                return _table;
            }

            if (lazyRootDeclarations.Count == 1)
            {
                return Remove(lazyRootDeclarations[0]);
            }

            var isLatestRemoved = _table._latestLazyRootDeclaration != null && lazyRootDeclarations.Contains(_table._latestLazyRootDeclaration);

            var newOlderRootDeclarations = _table._allOlderRootDeclarations.RemoveRange(lazyRootDeclarations);
            var newLatestLazyRootDeclaration = isLatestRemoved ? null : _table._latestLazyRootDeclaration;

            return new DeclarationTable(newOlderRootDeclarations, newLatestLazyRootDeclaration, cache: null);
        }
    }
}
