// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
    /// <summary>
    /// Represents the shape of an array type.
    /// </summary>
#if SRM && FUTURE
    public
#endif
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

        /// <summary>
        /// Gets the number of dimensions in the array.
        /// </summary>
        public int Rank
        {
            get { return _rank; }
        }

        /// <summary>
        /// Gets the sizes of each dimension. Length may be smaller than rank, in which case the trailing dimensions have unspecified sizes.
        /// </summary>
        public ImmutableArray<int> Sizes
        {
            get { return _sizes; }
        }

        /// <summary>
        /// Gets the lower-bounds of each dimension. Length may be smaller than rank, in which case the trailing dimensions have unspecified lower bounds.
        /// </summary>
        public ImmutableArray<int> LowerBounds
        {
            get { return _lowerBounds; }
        }
    }
}
