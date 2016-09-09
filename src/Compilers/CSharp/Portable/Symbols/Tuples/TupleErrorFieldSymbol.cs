// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field of a tuple type (such as (int, byte).Item1)
    /// that doesn't have a corresponding backing field within the tuple underlying type.
    /// Created in response to an error condition.
    /// </summary>
    internal sealed class TupleErrorFieldSymbol : SynthesizedFieldSymbolBase
    {
        private readonly TypeSymbol _type;

        /// <summary>
        /// If this field represents a tuple element with index X
        ///  2X      if this field represents Default-named element
        ///  2X + 1  if this field represents Friendly-named element
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        private readonly int _tupleElementIndex;

        private readonly ImmutableArray<Location> _locations;
        private readonly DiagnosticInfo _useSiteDiagnosticInfo;

        // default tuple elements like Item1 or Item20 could be provided by the user or
        // otherwise implicitly declared by compiler
        private readonly bool _isImplicitlyDeclared;

        public TupleErrorFieldSymbol(NamedTypeSymbol container, string name, int tupleElementIndex, Location location, TypeSymbol type, DiagnosticInfo useSiteDiagnosticInfo, bool isImplicitlyDeclared)
            : base(container, name, isPublic:true, isReadOnly:false, isStatic:false)
        {
            Debug.Assert(name != null);
            _type = type;
            _locations = location == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(location);
            _useSiteDiagnosticInfo = useSiteDiagnosticInfo;
            _tupleElementIndex = tupleElementIndex;
            _isImplicitlyDeclared = isImplicitlyDeclared;
        }

        public override bool IsTupleField
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// If this is a field representing a tuple element,
        /// returns the index of the element (zero-based).
        /// Otherwise returns -1
        /// </summary>
        public override int TupleElementIndex
        {
            get
            {
                if (_tupleElementIndex < 0)
                {
                    return -1;
                }

                return _tupleElementIndex >> 1;
            }
        }

        public override bool IsDefaultTupleElement
        {
            get
            {
                // not negative and even
                return (_tupleElementIndex & ((1 << 31) | 1)) == 0;
            }
        }

        public override FieldSymbol TupleUnderlyingField
        {
            get
            {
                // Failed to find one
                return null;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _isImplicitlyDeclared ?
                    ImmutableArray<SyntaxReference>.Empty :
                    GetDeclaringSyntaxReferenceHelper<CSharpSyntaxNode>(_locations);
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _isImplicitlyDeclared;
            }
        }

        internal override bool SuppressDynamicAttribute
        {
            get
            {
                return true;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return _useSiteDiagnosticInfo;
        }

        public override sealed int GetHashCode()
        {
            return Hash.Combine(ContainingType.GetHashCode(), _tupleElementIndex.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TupleErrorFieldSymbol);
        }

        public bool Equals(TupleErrorFieldSymbol other)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null &&
                _tupleElementIndex == other._tupleElementIndex &&
                ContainingType == other.ContainingType;
        }
    }
}