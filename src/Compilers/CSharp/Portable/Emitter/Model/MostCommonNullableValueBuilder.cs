// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct MostCommonNullableValueBuilder
    {
        /// <see cref="NullableAnnotationExtensions.ObliviousAttributeValue"/>
        private int _value0;

        /// <see cref="NullableAnnotationExtensions.NotAnnotatedAttributeValue"/>
        private int _value1;

        /// <see cref="NullableAnnotationExtensions.AnnotatedAttributeValue"/>
        private int _value2;

        internal byte? MostCommonValue
        {
            get
            {
                int max;
                byte b;
                if (_value1 > _value0)
                {
                    max = _value1;
                    b = 1;
                }
                else
                {
                    max = _value0;
                    b = 0;
                }
                if (_value2 > max)
                {
                    return 2;
                }
                return max == 0 ? (byte?)null : b;
            }
        }

        internal void AddValue(byte value)
        {
            switch (value)
            {
                case 0:
                    _value0++;
                    break;
                case 1:
                    _value1++;
                    break;
                case 2:
                    _value2++;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(value);
            }
        }

        internal void AddValue(byte? value)
        {
            if (value != null)
            {
                AddValue(value.GetValueOrDefault());
            }
        }

        internal void AddValue(TypeWithAnnotations type)
        {
            var builder = ArrayBuilder<byte>.GetInstance();
            type.AddNullableTransforms(builder);
            AddValue(GetCommonValue(builder));
            builder.Free();
        }

        /// <summary>
        /// Returns the common value if all bytes are the same value.
        /// Otherwise returns null.
        /// </summary>
        internal static byte? GetCommonValue(ArrayBuilder<byte> builder)
        {
            int n = builder.Count;
            if (n == 0)
            {
                return null;
            }
            byte b = builder[0];
            for (int i = 1; i < n; i++)
            {
                if (builder[i] != b)
                {
                    return null;
                }
            }
            return b;
        }
    }
}
