// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private readonly TypeWithAnnotations _type;

        /// <summary>
        /// If this field represents a tuple element with index X
        ///  2X      if this field represents Default-named element
        ///  2X + 1  if this field represents Friendly-named element
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        private readonly int _tupleElementIndex;

        private readonly ImmutableArray<Location> _locations;
        private readonly DiagnosticInfo _useSiteDiagnosticInfo;
        private readonly TupleErrorFieldSymbol _correspondingDefaultField;

        // default tuple elements like Item1 or Item20 could be provided by the user or
        // otherwise implicitly declared by compiler
        private readonly bool _isImplicitlyDeclared;

        public TupleErrorFieldSymbol(
            NamedTypeSymbol container,
            string name,
            int tupleElementIndex,
            Location location,
            TypeWithAnnotations type,
            DiagnosticInfo useSiteDiagnosticInfo,
            bool isImplicitlyDeclared,
            TupleErrorFieldSymbol correspondingDefaultFieldOpt)

            : base(container, name, isPublic: true, isReadOnly: false, isStatic: false)
        {
            Debug.Assert(name != null);
            _type = type;
            _locations = location == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(location);
            _useSiteDiagnosticInfo = useSiteDiagnosticInfo;
            _tupleElementIndex = (object)correspondingDefaultFieldOpt == null ? tupleElementIndex << 1 : (tupleElementIndex << 1) + 1;
            _isImplicitlyDeclared = isImplicitlyDeclared;

            Debug.Assert((correspondingDefaultFieldOpt == null) == this.IsDefaultTupleElement);
            Debug.Assert(correspondingDefaultFieldOpt == null || correspondingDefaultFieldOpt.IsDefaultTupleElement);

            _correspondingDefaultField = correspondingDefaultFieldOpt ?? this;
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

        public override bool IsExplicitlyNamedTupleElement
        {
            get
            {
                return _tupleElementIndex >= 0 && !_isImplicitlyDeclared;
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

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                return this;
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

        public override FieldSymbol CorrespondingTupleField
        {
            get
            {
                return _correspondingDefaultField;
            }
        }

        internal override bool SuppressDynamicAttribute
        {
            get
            {
                return true;
            }
        }

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return new UseSiteInfo<AssemblySymbol>(_useSiteDiagnosticInfo);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(ContainingType.GetHashCode(), _tupleElementIndex.GetHashCode());
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            return Equals(obj as TupleErrorFieldSymbol, compareKind);
        }

        public bool Equals(TupleErrorFieldSymbol other, TypeCompareKind compareKind)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null &&
                _tupleElementIndex == other._tupleElementIndex &&
                TypeSymbol.Equals(ContainingType, other.ContainingType, compareKind);
        }

        internal override FieldSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(newOwner.IsTupleType && newOwner.TupleElementTypesWithAnnotations.Length > TupleElementIndex);
            if (ReferenceEquals(newOwner, ContainingType))
            {
                return this;
            }

            TupleErrorFieldSymbol newCorrespondingField = null;
            if (!ReferenceEquals(_correspondingDefaultField, this))
            {
                newCorrespondingField = (TupleErrorFieldSymbol)_correspondingDefaultField.AsMember(newOwner);
            }

            return new TupleErrorFieldSymbol(
                newOwner,
                Name,
                TupleElementIndex,
                _locations.IsEmpty ? null : GetFirstLocation(),
                newOwner.TupleElementTypesWithAnnotations[TupleElementIndex],
                _useSiteDiagnosticInfo,
                _isImplicitlyDeclared,
                newCorrespondingField);
        }
    }
}
