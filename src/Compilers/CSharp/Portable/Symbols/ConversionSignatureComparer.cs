// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

            return member1.ReturnType.TypeSymbol.Equals(member2.ReturnType.TypeSymbol, TypeSymbolEqualityOptions.IgnoreDynamic)
                && member1.ParameterTypes[0].Equals(member2.ParameterTypes[0], TypeSymbolEqualityOptions.IgnoreDynamic);
        }

        public int GetHashCode(SourceUserDefinedConversionSymbol member)
        {
            if ((object)member == null)
            {
                return 0;
            }

            int hash = 1;
            hash = Hash.Combine(member.ReturnType.TypeSymbol.GetHashCode(), hash);
            if (member.ParameterCount != 1)
            {
                return hash;
            }
            hash = Hash.Combine(member.ParameterTypes[0].GetHashCode(), hash);
            return hash;
        }
    }
}
