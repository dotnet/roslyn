// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// If this field represents a tuple element (including the name match), 
        /// id is an index of the element (zero-based).
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        private readonly int _tupleFieldId;

        public TupleFieldSymbol(TupleTypeSymbol container, FieldSymbol underlyingField, int tupleFieldId)
            : base(underlyingField)
        {
            _containingTuple = container;
            _tupleFieldId = tupleFieldId;
        }

        public override bool IsTupleField
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// If this field represents a tuple element (including the name match), 
        /// id is an index of the element (zero-based).
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>i
        public override int TupleElementIndex
        {
            get
            {
                return _tupleFieldId;
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
            return Hash.Combine(_containingTuple.GetHashCode(), _tupleFieldId.GetHashCode());
        }

        public override sealed bool Equals(object obj)
        {
            return Equals(obj as TupleFieldSymbol);
        }

        public bool Equals(TupleFieldSymbol other)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null && _tupleFieldId == other._tupleFieldId && _containingTuple == other._containingTuple;
        }
    }

    /// <summary>
    /// Represents an element field of a tuple type (such as (int, byte).Item1)
    /// that is backed by a real field with the same name within the tuple underlying type.
    /// </summary>
    internal class TupleElementFieldSymbol : TupleFieldSymbol
    {
        private readonly ImmutableArray<Location> _locations;

        public TupleElementFieldSymbol(TupleTypeSymbol container, FieldSymbol underlyingField, int tupleFieldId, Location location)
            : base(container, underlyingField, tupleFieldId)
        {
            _locations = location == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(location);
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
                return GetDeclaringSyntaxReferenceHelper<CSharpSyntaxNode>(_locations);
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return false;
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
    /// Represents an element field of a tuple type (such as (int a, byte b).a, or (int a, byte b).b)
    /// that is backed by a real field with a different name within the tuple underlying type.
    /// </summary>
    internal sealed class TupleRenamedElementFieldSymbol : TupleElementFieldSymbol
    {
        private readonly string _name;

        public TupleRenamedElementFieldSymbol(TupleTypeSymbol container, FieldSymbol underlyingField, string name, int tupleElementOrdinal, Location location)
            : base(container, underlyingField, tupleElementOrdinal, location)
        {
            Debug.Assert(name != null);
            Debug.Assert(name != underlyingField.Name);
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
    }
}
