// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a sequence of <see cref="NullableAnnotation"/> to apply to a type. The sequence of
    /// flags is specific to how the individual <see cref="TypeSymbol"/> walk their nested types.
    /// </summary>
    internal sealed class NullableTransformStream
    {
        private static readonly ObjectPool<NullableTransformStream> Pool = new ObjectPool<NullableTransformStream>(() => new NullableTransformStream(), Environment.ProcessorCount);

        private ImmutableArray<byte> _transforms;

        /// <summary>
        /// When <see cref="_transforms"/> is the default value this will contain the transform to yield an infinite sequence
        /// of. Otherwise it is the position in <see cref="_transforms"/> of the next value to yield.
        /// </summary>
        private int _positionOrDefault;

        private bool IsDefault => _transforms.IsDefault;

        /// <summary>
        /// Returns whether or not there is data remaining in the series. When a single flag is used this
        /// will always return false.
        /// </summary>
        public bool HasUnusedTransforms => !IsDefault && _positionOrDefault >= 0 && _positionOrDefault < _transforms.Length;
        public bool HasInsufficientData => _positionOrDefault < 0;
        public bool IsComplete => !HasUnusedTransforms && !HasInsufficientData;

        private NullableTransformStream()
        {

        }

        /// <summary>
        /// Returns an infinite stream of the given transform value.
        /// </summary>
        public static NullableTransformStream GetInstance(byte defaultTransform)
        {
            Debug.Assert(defaultTransform >= 0);
            var stream = Pool.Allocate();
            stream._positionOrDefault = defaultTransform;
            return stream;
        }

        /// <summary>
        /// Returns a finite stream of the given transform values.
        /// </summary>
        public static NullableTransformStream GetInstance(ImmutableArray<byte> transforms)
        {
            var stream = Pool.Allocate();
            stream._positionOrDefault = 0;
            stream._transforms = transforms;
            return stream;
        }

        public static NullableTransformStream GetInstance(byte defaultTransform, ImmutableArray<byte> transforms) =>
            transforms.IsDefault ? GetInstance(defaultTransform) : GetInstance(transforms);

        public static NullableTransformStream GetInstance(NullableAnnotation nullableAnnotation) =>
            GetInstance((byte)nullableAnnotation);

        public void Free()
        {
            _transforms = default;
            Pool.Free(this);
        }

        public void SetHasInsufficientData()
        {
            _positionOrDefault = -1;
            Debug.Assert(HasInsufficientData);
            Debug.Assert(!IsComplete);
        }

        public byte? GetNextTransform()
        {
            if (IsDefault)
            {
                return (byte)_positionOrDefault;
            }
            else if (HasUnusedTransforms)
            {
                return _transforms[_positionOrDefault++];
            }
            else
            {
                SetHasInsufficientData();
                return null;
            }
        }
    }
}
