// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TupleFieldSymbol represents the field of a tuple type, such as (int, byte).Item2 or (int a, long b).a
    /// </summary>
    internal sealed class TupleFieldSymbol : FieldSymbol
    {
        private readonly string _name;
        private readonly TypeSymbol _type;
        private readonly TupleTypeSymbol _containingTuple;
        private readonly int _position;
        private readonly FieldSymbol _underlyingFieldOpt;

        /// <summary>
        /// Missing underlying field is handled for error recovery
        /// A tuple without backing fields is usable for binding purposes, since we know its name and type,
        /// but caller is supposed to report some kind of error at declaration.
        ///
        /// position is 1 for Item1.
        /// </summary>
        internal TupleFieldSymbol(string name, TupleTypeSymbol containingTuple, TypeSymbol type, int position, FieldSymbol underlyingFieldOpt)
        {
            _name = name;
            _containingTuple = containingTuple;
            _underlyingFieldOpt = underlyingFieldOpt;
            _type = type;
            _position = position;
        }

        /// <summary>
        /// Copy this tuple field, but modify it to use the new containing tuple, link type and field type.
        /// </summary>
        internal TupleFieldSymbol WithType(TupleTypeSymbol newContainingTuple, NamedTypeSymbol newlinkType, TypeSymbol newFieldType)
        {
            FieldSymbol newUnderlyingFieldOpt = _underlyingFieldOpt?.OriginalDefinition.AsMember(newlinkType);
            Debug.Assert(newUnderlyingFieldOpt == null || newUnderlyingFieldOpt.Type == newFieldType);

            return new TupleFieldSymbol(_name, newContainingTuple, newFieldType, _position, newUnderlyingFieldOpt);
        }

        internal static FieldSymbol GetUnderlyingField(int containingTupleArity, NamedTypeSymbol containingUnderlyingType, int fieldIndex, CSharpSyntaxNode syntax, Binder binder, DiagnosticBag diagnostics)
        {
            int tupleRemainder;
            int tupleChainLength = TupleTypeSymbol.NumberOfValueTuples(containingTupleArity, out tupleRemainder);

            int fieldRemainder;
            int fieldChainLength = TupleTypeSymbol.NumberOfValueTuples(fieldIndex + 1, out fieldRemainder);

            int containingLinkSize = TupleTypeSymbol.RestPosition;
            if (fieldChainLength == tupleChainLength)
            {
                containingLinkSize = tupleRemainder;
            }

            // PROTOTYPE(tuples) Constructing a TupleTypeSymbol with TupleFieldSymbols should not produce diagnostics for missing well-known members and type members.
            //                      The error handling should be shifted to lowering. This will help when we implement metadata loading.
            WellKnownMember wellKnownTypeMember = TupleTypeSymbol.GetTupleTypeMember(containingLinkSize, fieldRemainder);
            var linkField = (FieldSymbol)Binder.GetWellKnownTypeMember(binder.Compilation, wellKnownTypeMember, diagnostics, syntax: syntax);
            if ((object)linkField == null)
            {
                return null;
            }

            NamedTypeSymbol linkType = TupleTypeSymbol.GetNestedTupleType(containingUnderlyingType, fieldChainLength - 1);

            return linkField.AsMember(linkType);
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Returns the position of this field within the tuple.
        /// For instance, the position for Item1 is 1.
        /// </summary>
        public int Position => _position;

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
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
                return _underlyingFieldOpt?.CustomModifiers ?? ImmutableArray<CustomModifier>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingFieldOpt?.DeclaredAccessibility ?? Accessibility.Public;
            }
        }

        public override bool IsConst
        {
            get
            {
                return _underlyingFieldOpt?.IsConst ?? false;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return _underlyingFieldOpt?.IsReadOnly ?? false;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingFieldOpt?.IsStatic ?? false;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                return _underlyingFieldOpt?.IsVolatile ?? false;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingFieldOpt?.HasRuntimeSpecialName ?? false;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingFieldOpt?.HasSpecialName ?? false;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return _underlyingFieldOpt?.IsNotSerialized ?? false;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return _underlyingFieldOpt?.MarshallingInformation;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                // PROTOTYPE: need to figure what is the right behavior when underlying is obsolete
                return null;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _underlyingFieldOpt?.TypeLayoutOffset;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return _underlyingFieldOpt?.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }
    }
}
