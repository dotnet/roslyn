// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An error type, used to represent a byref return in a metadata signature.
    /// </summary>
    /// <remarks>
    /// If we ever decide to support by-ref returns, don't just make this a non-error
    /// type.  For consistency with parameters and locals, we should have a bit on the
    /// signature (i.e. on the MethodSymbol).
    /// </remarks>
    internal sealed class ByRefReturnErrorTypeSymbol : ErrorTypeSymbol
    {
        private readonly TypeSymbol _referencedType;
        private readonly ushort _countOfCustomModifiersPrecedingByRef;

        internal ByRefReturnErrorTypeSymbol(TypeSymbol referencedType, ushort countOfCustomModifiersPrecedingByRef)
        {
            Debug.Assert((object)referencedType != null && !(referencedType is ByRefReturnErrorTypeSymbol));
            _referencedType = referencedType;
            _countOfCustomModifiersPrecedingByRef = countOfCustomModifiersPrecedingByRef;
        }

        #region Defining characteristics of this type

        internal TypeSymbol ReferencedType
        {
            get { return _referencedType; }
        }

        internal override TypeWithModifiers Substitute(AbstractTypeMap typeMap)
        {
            TypeWithModifiers substitutedReferencedType = typeMap.SubstituteType(_referencedType);
            return substitutedReferencedType.Is(_referencedType) ?
                       new TypeWithModifiers(this) :
                       new TypeWithModifiers(new ByRefReturnErrorTypeSymbol(substitutedReferencedType.Type, _countOfCustomModifiersPrecedingByRef),
                                             substitutedReferencedType.CustomModifiers);
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds, bool ignoreDynamic)
        {
            if ((object)this == (object)t2)
            {
                return true;
            }

            ByRefReturnErrorTypeSymbol other = t2 as ByRefReturnErrorTypeSymbol;
            return (object)other != null && _referencedType.Equals(other._referencedType, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic) &&
                   (ignoreCustomModifiersAndArraySizesAndLowerBounds || _countOfCustomModifiersPrecedingByRef == other._countOfCustomModifiersPrecedingByRef);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Hash.Combine(_referencedType.GetHashCode(), _countOfCustomModifiersPrecedingByRef), 13); // Reduce collisions with referencedType.
        }

        #endregion Defining characteristics of this type


        #region Abstract in ErrorTypeSymbol

        internal override DiagnosticInfo ErrorInfo
        {
            get { return new CSDiagnosticInfo(ErrorCode.ERR_ByRefReturnUnsupported, _referencedType); }
        }

        internal override bool MangleName
        {
            get { return false; }
        }

        #endregion Abstract in ErrorTypeSymbol


        #region Fleshing out NamedTypeSymbol members

        public override bool IsReferenceType
        {
            get
            {
                return true;
            }
        }

        public override bool IsValueType
        {
            get
            {
                return false;
            }
        }

        internal override bool IsManagedType
        {
            get
            {
                return false;
            }
        }

        #endregion Fleshing out NamedTypeSymbol members
    }
}
