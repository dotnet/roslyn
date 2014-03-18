// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Type layout information.
    /// </summary>
    internal struct TypeLayout : IEquatable<TypeLayout>
    {
        private readonly byte kind;
        private readonly short alignment;
        private readonly int size;

        public TypeLayout(LayoutKind kind, int size, byte alignment)
        {
            Debug.Assert(size >= 0 && alignment >= 0 && (int)kind >= 0 && (int)kind <= 3);

            // we want LayoutKind.Auto to be the default layout for default(TypeLayout):
            Debug.Assert(LayoutKind.Sequential == 0);
            this.kind = (byte)(kind + 1);

            this.size = size;
            this.alignment = alignment;
        }

        /// <summary>
        /// Layout kind (Layout flags in metadata).
        /// </summary>
        public LayoutKind Kind
        {
            get
            {
                // for convenience default(TypeLayout) should be auto-layout
                return kind == 0 ? LayoutKind.Auto : (LayoutKind)(kind - 1);
            }
        }

        /// <summary>
        /// Field alignment (PackingSize field in metadata).
        /// </summary>
        public short Alignment
        {
            get { return alignment; }
        }

        /// <summary>
        /// Size of the type.
        /// </summary>
        public int Size
        {
            get { return size; }
        }

        public bool Equals(TypeLayout other)
        {
            return this.size == other.size
                && this.alignment == other.alignment
                && this.kind == other.kind;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeLayout && Equals((TypeLayout)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Hash.Combine(this.Size, this.Alignment), this.kind);
        }
    }
}
