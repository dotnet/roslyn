// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// NOTE: This is a temporary internal copy of code that will be cut from System.Reflection.Metadata v1.1 and
//       ship in System.Reflection.Metadata v1.2 (with breaking changes). Remove and use the public API when
//       a v1.2 prerelease is available and code flow is such that we can start to depend on it.

using System.Collections.Immutable;

namespace Roslyn.Reflection.Metadata.Decoding
{
    internal struct ArrayShape
    {
        private readonly int _rank;
        private readonly ImmutableArray<int> _sizes;
        private readonly ImmutableArray<int> _lowerBounds;

        public ArrayShape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            _rank = rank;
            _sizes = sizes;
            _lowerBounds = lowerBounds;
        }

        public int Rank
        {
            get { return _rank; }
        }

        public ImmutableArray<int> Sizes
        {
            get { return _sizes; }
        }

        public ImmutableArray<int> LowerBounds
        {
            get { return _lowerBounds; }
        }
    }
}
