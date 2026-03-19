// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DeclarationTable
    {
        // The structure of the DeclarationTable provides us with a set of 'old' declarations that
        // stay relatively unchanged and a 'new' declaration that is repeatedly added and removed.
        // This mimics the expected usage pattern of a user repeatedly typing in a single file.
        // Because of this usage pattern, we can cache information about these 'old' declarations
        // and keep that around as long as they do not change.  For example, we keep a single 'merged
        // declaration' for all those root declarations as well as sets of interesting information
        // (like the type names in those decls). 
        private class Cache
        {
            private readonly DeclarationTable _table;

            // The merged root declaration for all the 'old' declarations.
            private MergedNamespaceDeclaration? _mergedRoot;

            // All the simple type names for all the types in the 'old' declarations.
            private ISet<string>? _typeNames;
            private ISet<string>? _namespaceNames;
            private ImmutableArray<ReferenceDirective> _referenceDirectives;

            public Cache(DeclarationTable table)
            {
                _table = table;
            }

            public MergedNamespaceDeclaration MergedRoot
            {
                get
                {
                    if (_mergedRoot is null)
                    {
                        Interlocked.CompareExchange(
                            ref _mergedRoot,
                            MergedNamespaceDeclaration.Create(_table._allOlderRootDeclarations.InInsertionOrder.Select(static lazyRoot => lazyRoot.Value).AsImmutable<SingleNamespaceDeclaration>()),
                            comparand: null);
                    }

                    return _mergedRoot;
                }
            }

            public ISet<string> TypeNames
            {
                get
                {
                    if (_typeNames is null)
                        Interlocked.CompareExchange(ref _typeNames, GetTypeNames(this.MergedRoot), comparand: null);

                    return _typeNames;
                }
            }

            public ISet<string> NamespaceNames
            {
                get
                {
                    if (_namespaceNames is null)
                        Interlocked.CompareExchange(ref _namespaceNames, GetNamespaceNames(this.MergedRoot), comparand: null);

                    return _namespaceNames;
                }
            }

            public ImmutableArray<ReferenceDirective> ReferenceDirectives
            {
                get
                {
                    if (_referenceDirectives.IsDefault)
                    {
                        ImmutableInterlocked.InterlockedInitialize(
                            ref _referenceDirectives,
                            MergedRoot.Declarations.OfType<RootSingleNamespaceDeclaration>().SelectMany(r => r.ReferenceDirectives).AsImmutable());
                    }

                    return _referenceDirectives;
                }
            }
        }
    }
}
