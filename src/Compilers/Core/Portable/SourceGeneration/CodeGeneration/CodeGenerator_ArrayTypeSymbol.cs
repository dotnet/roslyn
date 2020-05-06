// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal static partial class CodeGenerator
    {
        public static IArrayTypeSymbol ArrayType(ITypeSymbol elementType)
            => new ArrayTypeSymbol(elementType, rank: 1);

        public static IArrayTypeSymbol ArrayType(
            ITypeSymbol elementType, int rank)
        {
            return new ArrayTypeSymbol(elementType, rank);
        }

        private class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
        {
            public ITypeSymbol ElementType { get; }
            public int Rank { get; }

            public ArrayTypeSymbol(ITypeSymbol elementType, int rank)
            {
                ElementType = elementType;
                Rank = rank;
            }

            #region default implementation

            public bool IsSZArray => throw new NotImplementedException();

            public ImmutableArray<int> LowerBounds => throw new NotImplementedException();

            public ImmutableArray<int> Sizes => throw new NotImplementedException();

            public NullableAnnotation ElementNullableAnnotation => throw new NotImplementedException();

            public ImmutableArray<CustomModifier> CustomModifiers => throw new NotImplementedException();

            public bool Equals(IArrayTypeSymbol other)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}
