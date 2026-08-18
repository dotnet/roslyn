// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <para>
    /// Type layout information - this retrieves the <see cref="StructLayoutAttribute" /> information from the type definition, or the default that the compiler emits if absent.
    /// </para>
    /// <para>
    /// In particular, it has the layout kind, the packing size, and the size of the type, as defined in metadata or source - it does not compute the actual size of a type for example.
    /// </para>
    /// </summary>
    public readonly struct TypeLayout : IEquatable<TypeLayout>
    {
        private readonly byte _kind;
        private readonly ushort _packingSize;
        private readonly int _size;

        internal TypeLayout(LayoutKind kind, int size, byte alignment)
        {
            Debug.Assert(size >= 0 && (int)kind >= 0 && (int)kind <= 3);

            // we want LayoutKind.Auto to be the default layout for default(TypeLayout):
            Debug.Assert(LayoutKind.Sequential == 0);
            _kind = (byte)(kind + 1);

            _size = size;
            _packingSize = alignment;
        }

        /// <summary>
        /// Layout kind (Layout flags in metadata).
        /// </summary>
        public LayoutKind Kind
        {
            get
            {
                // for convenience default(TypeLayout) should be auto-layout
                return _kind == 0 ? LayoutKind.Auto : (LayoutKind)(_kind - 1);
            }
        }

        /// <summary>
        /// Packing size (PackingSize field in metadata).
        /// </summary>
        public ushort PackingSize
        {
            get { return _packingSize; }
        }

        /// <summary>
        /// Size of the type (Size field in metadata).
        /// </summary>
        public int Size
        {
            get { return _size; }
        }

        public bool Equals(TypeLayout other)
        {
            return _size == other._size
                && _packingSize == other._packingSize
                && Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeLayout && Equals((TypeLayout)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Hash.Combine(this.Size, this.PackingSize), (int)this.Kind);
        }

        /// <summary>
        /// Compares two <see cref="TypeLayout" /> instances for equality.
        /// </summary>
        public static bool operator ==(TypeLayout left, TypeLayout right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="TypeLayout" /> instances for inequality.
        /// </summary>
        public static bool operator !=(TypeLayout left, TypeLayout right)
        {
            return !left.Equals(right);
        }
    }
}
