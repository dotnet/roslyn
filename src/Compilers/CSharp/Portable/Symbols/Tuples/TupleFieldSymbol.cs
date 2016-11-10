﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a non-element field of a tuple type (such as (int, byte).Rest)
    /// that is backed by a real field within the tuple underlying type.
    /// </summary>
    internal class TupleFieldSymbol : WrappedFieldSymbol
    {
        protected readonly TupleTypeSymbol _containingTuple;

        /// <summary>
        /// If this field represents a tuple element with index X
        ///  2X      if this field represents Default-named element
        ///  2X + 1  if this field represents Friendly-named element
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        private readonly int _tupleElementIndex;

        public TupleFieldSymbol(TupleTypeSymbol container, FieldSymbol underlyingField, int tupleElementIndex)
            : base(underlyingField)
        {
            Debug.Assert(container.UnderlyingNamedType.Equals(underlyingField.ContainingType, TypeCompareKind.IgnoreDynamicAndTupleNames) || this is TupleVirtualElementFieldSymbol,
                                            "virtual fields should be represented by " + nameof(TupleVirtualElementFieldSymbol));

            _containingTuple = container;
            _tupleElementIndex = tupleElementIndex;
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
                return (_tupleElementIndex & ((1<<31) | 1)) == 0 ;
            }
        }

        public override FieldSymbol TupleUnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _containingTuple.GetTupleMemberSymbolForUnderlyingMember(_underlyingField.AssociatedSymbol);
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingTuple;
            }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return _underlyingField.CustomModifiers;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _underlyingField.GetFieldType(fieldsBeingBound);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingField.GetAttributes();
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = base.GetUseSiteDiagnostic();
            MergeUseSiteDiagnostics(ref result, _underlyingField.GetUseSiteDiagnostic());
            return result;
        }

        public override sealed int GetHashCode()
        {
            return Hash.Combine(_containingTuple.GetHashCode(), _tupleElementIndex.GetHashCode());
        }

        public override sealed bool Equals(object obj)
        {
            var other = obj as TupleFieldSymbol;

            if ((object)other == this)
            {
                return true;
            }

            // note we have to compare both index and name because 
            // in nameless tuple there could be fields that differ only by index
            // and in named tupoles there could be fields that differ only by name
            return (object)other != null &&
                _tupleElementIndex == other._tupleElementIndex &&
                _containingTuple == other._containingTuple;
        }
    }

    /// <summary>
    /// Represents an element field of a tuple type (such as (int, byte).Item1)
    /// that is backed by a real field with the same name within the tuple underlying type.
    /// </summary>
    internal class TupleElementFieldSymbol : TupleFieldSymbol
    {
        private readonly ImmutableArray<Location> _locations;

        // default tuple elements like Item1 or Item20 could be provided by the user or
        // otherwise implicitly declared by compiler
        private readonly bool _isImplicitlyDeclared;

        public TupleElementFieldSymbol(TupleTypeSymbol container, FieldSymbol underlyingField, int tupleElementIndex, Location location, bool isImplicitlyDeclared)
            : base(container, underlyingField, tupleElementIndex)
        {
            _locations = location == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(location);
            _isImplicitlyDeclared = isImplicitlyDeclared;
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

        internal override int? TypeLayoutOffset
        {
            get
            {
                if (_underlyingField.ContainingType != _containingTuple.TupleUnderlyingType)
                {
                    return null;
                }

                return base.TypeLayoutOffset;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                if (_underlyingField.ContainingType != _containingTuple.TupleUnderlyingType)
                {
                    return null;
                }

                return base.AssociatedSymbol;
            }
        }
    }

    /// <summary>
    /// Represents an element field of a tuple type that is not backed by a real field 
    /// with the same name within the tuple underlying type.
    /// 
    /// Examples
    ///     // alias to Item1 with a different name
    ///     (int a, byte b).a                           
    ///
    ///     // not backed directly by the underlying type
    ///     (int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8).i8
    ///     
    /// NOTE: For any virtual element, there is a nonvirtual way to access the same underlying field.
    ///       In scenarios where we need to enumerate actual fields of a struct, 
    ///       virtual fields should be ignored.
    /// </summary>
    internal sealed class TupleVirtualElementFieldSymbol : TupleElementFieldSymbol
    {
        private readonly string _name;

        public TupleVirtualElementFieldSymbol(TupleTypeSymbol container, FieldSymbol underlyingField, string name, int tupleElementIndex, Location location, bool isImplicitlyDeclared)
            : base(container, underlyingField, tupleElementIndex, location, isImplicitlyDeclared)
        {
            Debug.Assert(name != null);
            Debug.Assert(name != underlyingField.Name || !container.UnderlyingNamedType.Equals(underlyingField.ContainingType, TypeCompareKind.IgnoreDynamicAndTupleNames),
                                "fields that map directly to underlying should not be represented by " + nameof(TupleVirtualElementFieldSymbol));

            _name = name;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return null;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public override bool IsVirtualTupleField
        {
            get
            {
                return true;
            }
        }
    }
}
