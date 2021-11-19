// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class ModuleScopedDelegateCacheContainerField : SynthesizedFieldSymbol
    {
        /// Why the _name is not readonly? Why the _sortKey?
        /// See the comments above <see cref="ModuleScopedDelegateCacheContainer._name"/>
        private string _name;

        private Location _sortKey;

        /// Not readonly because container have no idea if this symbol was simply returned or added prior calling <see cref="AddLocation(Location)"/>.
        private Location _firstFoundLocation;

        private ConcurrentBag<Location> _lazyOtherLocations;

        public ModuleScopedDelegateCacheContainerField(DelegateCacheContainer container, string targetMethodName, TypeSymbol type)
            : base(container, type, targetMethodName, isPublic: true, isReadOnly: false, isStatic: true)
        {
            TargetMethodName = targetMethodName;
        }

        public override string Name => _name;

        public Location SortKey => _sortKey;

        // Save this value for a debug friendly name.
        public string TargetMethodName { get; }

        /// <summary>
        /// Adds the location where the converion happens for later calculation of the sort key.
        /// </summary>
        internal void AddLocation(Location location)
        {
            Debug.Assert(location != null);

            var orgFirstFoundLocation = Interlocked.CompareExchange(ref _firstFoundLocation, location, null);
            if ( orgFirstFoundLocation == null)
            {
                return;
            }

            // WinRT event field assignments may hit this.
            if (location == orgFirstFoundLocation)
            {
                return;
            }

            if (_lazyOtherLocations == null)
            {
                Interlocked.CompareExchange(ref _lazyOtherLocations, new ConcurrentBag<Location>(), null);
            }

            _lazyOtherLocations.Add(location);
        }

        internal void EnsureSortKey()
        {
            if (_sortKey != null)
            {
                return;
            }

            Debug.Assert(_firstFoundLocation != null);

            var sortKey = _firstFoundLocation;

            if (_lazyOtherLocations != null)
            {
                foreach (var location in _lazyOtherLocations)
                {
                    if (DeclaringCompilation.CompareSourceLocations(sortKey, location) > 0)
                    {
                        sortKey = location;
                    }
                }
            }

            _sortKey = sortKey;
        }

        /// <remarks>This method is only intended to be called from <see cref="ModuleScopedDelegateCacheContainer"/></remarks>.
        internal void AssignName(string name)
        {
            Debug.Assert(name != null && _name == null, "Name should only be assigned once.");

            _name = name;
        }
    }
}
