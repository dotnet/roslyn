// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TupleTypeSymbol represents a tuple type, such as (int, byte) or (int a, long b).
    /// </summary>
    internal sealed class TupleTypeSymbol : TypeSymbol, ITupleTypeSymbol
    {
        private readonly NamedTypeSymbol _underlyingType;
        private readonly ImmutableArray<TupleFieldSymbol> _fields;

        /// <summary>
        /// Create a new TupleTypeSymbol from its declaration in source.
        /// </summary>
        internal TupleTypeSymbol(
            ImmutableArray<TypeSymbol> elementTypes,
            ImmutableArray<string> elementNames,
            CSharpSyntaxNode syntax,
            Binder binder,
            DiagnosticBag diagnostics)
        {
            this._underlyingType = GetTupleUnderlyingTypeAndFields(
                elementTypes, 
                elementNames,
                syntax,
                binder,
                diagnostics,
                out this._fields);
        }

        private NamedTypeSymbol GetTupleUnderlyingTypeAndFields(
            ImmutableArray<TypeSymbol> elementTypes,
            ImmutableArray<string> elementNames,
            CSharpSyntaxNode syntax,
            Binder binder,
            DiagnosticBag diagnostics,
            out ImmutableArray<TupleFieldSymbol> fields
            )
        {
            NamedTypeSymbol underlyingType;
            fields = ImmutableArray<TupleFieldSymbol>.Empty;

            switch (elementTypes.Length)
            {
                case 2:
                    {
                        var tupleType = binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_Tuple_T1_T2, diagnostics, syntax);
                        underlyingType = tupleType.Construct(elementTypes);

                        var underlyingField1 = Binder.GetWellKnownTypeMember(binder.Compilation, WellKnownMember.System_Runtime_CompilerServices_Tuple_T1_T2__Item1, diagnostics, syntax: syntax) as FieldSymbol;
                        var underlyingField2 = Binder.GetWellKnownTypeMember(binder.Compilation, WellKnownMember.System_Runtime_CompilerServices_Tuple_T1_T2__Item2, diagnostics, syntax: syntax) as FieldSymbol;

                        fields = ImmutableArray.Create(
                                    new TupleFieldSymbol(elementNames.IsEmpty ? "Item1" : elementNames[0],
                                        this,
                                        elementTypes[0],
                                        underlyingField1?.AsMember(underlyingType)),
                                    new TupleFieldSymbol(elementNames.IsEmpty ? "Item2" : elementNames[1],
                                        this,
                                        elementTypes[1],
                                        underlyingField2?.AsMember(underlyingType))
                                );

                        return underlyingType;
                    }

                default:
                    {
                        // TODO: VS if this eventually still stays reachable, need to make some error type symbol
                        var tupleType = binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_Tuple_T1_T2, diagnostics, syntax);
                        underlyingType = tupleType.Construct(elementTypes);
                        break;
                    }

            }

            return underlyingType;
        }

        // TODO: VS does it make sense to this this two-stage?
        // get underlying type (above),
        // then if all ok
        // get underlying fields here.
        // NOTE: underlying fields do not need the underlying type and in case of an erro still can be created.
        private ImmutableArray<TupleFieldSymbol> GetTupleFields(
            ImmutableArray<string> elementNames, 
            NamedTypeSymbol _underlyingType)
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return _underlyingType.BaseTypeNoUseSiteDiagnostics;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null)
        {
            return _underlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved);
        }

        public override bool IsReferenceType
        {
            get
            {
                return _underlyingType.IsReferenceType;
            }
        }

        public override bool IsValueType
        {
            get
            {
                return _underlyingType.IsReferenceType;
            }
        }

        internal sealed override bool IsManagedType
        {
            get
            {
                return _underlyingType.IsManagedType;
            }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _underlyingType.ObsoleteAttributeData; }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            // TODO: members
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            // TODO: members
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.TupleType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return TypeKind.Tuple;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return null;
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

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTupleType(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitTupleType(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTupleType(this);
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            return this.Equals(t2 as TupleTypeSymbol, ignoreCustomModifiers, ignoreDynamic);
        }

        internal bool Equals(TupleTypeSymbol other)
        {
            return Equals(other, false, false);
        }

        private bool Equals(TupleTypeSymbol other, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !other._underlyingType.Equals(_underlyingType, ignoreCustomModifiers, ignoreDynamic))
            {
                return false;
            }

            // Make sure field names are the same.
            if (!ignoreDynamic)
            {
                var fields = this._fields;
                var otherFields = other._fields;
                var count = fields.Length;

                for (int i = 0; i < count; i++)
                {
                    if (fields[i].Name != otherFields[i].Name)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _underlyingType.GetHashCode();
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return true;
            }
        }

        #region Use-Site Diagnostics

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = null;

            // check element type
            if (DeriveUseSiteDiagnosticFromType(ref result, this._underlyingType))
            {
                return result;
            }

            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _underlyingType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        #endregion

        #region ITupleTypeSymbol Members

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitTupleType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTupleType(this);
        }

        #endregion
    }

    internal sealed class TupleFieldSymbol : FieldSymbol
    {
        private readonly string _name;
        private readonly FieldSymbol _underlyingFieldOpt;
        private readonly TypeSymbol _type;
        private readonly TupleTypeSymbol _containingTuple;

        /// <summary>
        /// Missing underlying field is handled for error recovery
        /// A tuple without backing fields is usable for binding purposes, since we know its name and tyoe,
        /// but caller is supposed to report some kind of error at declaration.
        /// </summary>
        public TupleFieldSymbol(string name, TupleTypeSymbol containingTuple, TypeSymbol type, FieldSymbol underlyingFieldOpt)
        {
            this._name = name;
            this._containingTuple = containingTuple;
            this._type = type;
            this._underlyingFieldOpt = underlyingFieldOpt;
        }

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
                return _underlyingFieldOpt?.ObsoleteAttributeData;
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
