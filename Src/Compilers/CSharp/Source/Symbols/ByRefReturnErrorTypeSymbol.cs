// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An error type, used to represent the a byref return in a metadata signature.
    /// </summary>
    /// <remarks>
    /// If we ever decide to support by-ref returns, don't just make this a non-error
    /// type.  For consistency with parameters and locals, we should have a bit on the
    /// signature (i.e. on the MethodSymbol).
    /// </remarks>
    internal sealed class ByRefReturnErrorTypeSymbol : ErrorTypeSymbol
    {
        private readonly TypeSymbol referencedType;

        internal ByRefReturnErrorTypeSymbol(TypeSymbol referencedType)
        {
            Debug.Assert((object)referencedType != null);
            this.referencedType = referencedType;
        }

        #region Defining characteristics of this type

        internal TypeSymbol ReferencedType
        {
            get { return this.referencedType; }
        }

        internal override ErrorTypeSymbol Substitute(AbstractTypeMap typeMap)
        {
            TypeSymbol substitutedReferencedType = typeMap.SubstituteType(this.referencedType);
            return substitutedReferencedType == this.referencedType ? this : new ByRefReturnErrorTypeSymbol(substitutedReferencedType);
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            if ((object)this == (object)t2)
            {
                return true;
            }

            ByRefReturnErrorTypeSymbol other = t2 as ByRefReturnErrorTypeSymbol;
            return (object)other != null && this.referencedType.Equals(other.referencedType, ignoreCustomModifiers, ignoreDynamic);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.referencedType.GetHashCode(), 13); // Reduce collisions with referencedType.
        }

        #endregion Defining characteristics of this type


        #region Abstract in ErrorTypeSymbol

        internal override DiagnosticInfo ErrorInfo
        {
            get { return new CSDiagnosticInfo(ErrorCode.ERR_ByRefReturnUnsupported, referencedType); }
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