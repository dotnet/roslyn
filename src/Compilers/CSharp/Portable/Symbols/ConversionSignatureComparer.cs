﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ConversionSignatureComparer : IEqualityComparer<SourceUserDefinedConversionSymbol>
    {
        private static readonly ConversionSignatureComparer s_comparer = new ConversionSignatureComparer();
        public static ConversionSignatureComparer Comparer
        {
            get
            {
                return s_comparer;
            }
        }

        private ConversionSignatureComparer()
        {
        }

        public bool Equals(SourceUserDefinedConversionSymbol member1, SourceUserDefinedConversionSymbol member2)
        {
            if (ReferenceEquals(member1, member2))
            {
                return true;
            }

            if (ReferenceEquals(member1, null) || ReferenceEquals(member2, null))
            {
                return false;
            }

            // SPEC: The signature of a conversion operator consists of the source type and the
            // SPEC: target type. The implicit or explicit classification of a conversion operator
            // SPEC: is not part of the operator's signature. 

            // We might be in an error recovery situation in which there are too many or too
            // few formal parameters declared. If we are, just say that they are unequal.

            if (member1.ParameterCount != 1 || member2.ParameterCount != 1)
            {
                return false;
            }

            return member1.ReturnType.Equals(member2.ReturnType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes)
                && member1.ParameterTypesWithAnnotations[0].Equals(member2.ParameterTypesWithAnnotations[0], TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
        }

        public int GetHashCode(SourceUserDefinedConversionSymbol member)
        {
            if ((object)member == null)
            {
                return 0;
            }

            int hash = 1;
            hash = Hash.Combine(member.ReturnType.GetHashCode(), hash);
            if (member.ParameterCount != 1)
            {
                return hash;
            }
            hash = Hash.Combine(member.GetParameterType(0).GetHashCode(), hash);
            return hash;
        }
    }
}
