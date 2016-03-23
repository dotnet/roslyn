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
using Microsoft.Cci;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TupleTypeSymbol represents a tuple type, such as (int, byte) or (int a, long b).
    /// </summary>
    internal sealed class TupleTypeSymbol : NamedTypeSymbol, ITupleTypeSymbol
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

        private TupleTypeSymbol(TupleTypeSymbol originalTuple,
                                NamedTypeSymbol newUnderlyingType)
        {
            _underlyingType = newUnderlyingType;
            _fields = originalTuple._fields.SelectAsArray(field =>
                                                          {
                                                              var newUnderlyingField = field.UnderlyingTypleFieldSymbol.OriginalDefinition.AsMember(_underlyingType);
                                                              return new TupleFieldSymbol(field.Name, this, newUnderlyingField.Type, newUnderlyingField);
                                                          });
        }

        internal static TupleTypeSymbol ConstructTupleTypeSymbol(TupleTypeSymbol originalTupleType, NamedTypeSymbol newUnderlyingType)
        {
            Debug.Assert((object)newUnderlyingType.OriginalDefinition == (object)originalTupleType.UnderlyingTupleType.OriginalDefinition);
            return new TupleTypeSymbol(originalTupleType, newUnderlyingType);
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
                        var tupleType = binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2, diagnostics, syntax);
                        underlyingType = tupleType.Construct(elementTypes);

                        var underlyingField1 = (FieldSymbol) Binder.GetWellKnownTypeMember(binder.Compilation, WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2__Item1, diagnostics, syntax: syntax);
                        var underlyingField2 = (FieldSymbol) Binder.GetWellKnownTypeMember(binder.Compilation, WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2__Item2, diagnostics, syntax: syntax);

                        fields = ImmutableArray.Create(
                                    new TupleFieldSymbol(elementNames.IsDefault ? "Item1" : elementNames[0],
                                        this,
                                        elementTypes[0],
                                        underlyingField1?.AsMember(underlyingType)),
                                    new TupleFieldSymbol(elementNames.IsDefault ? "Item2" : elementNames[1],
                                        this,
                                        elementTypes[1],
                                        underlyingField2?.AsMember(underlyingType))
                                );

                        return underlyingType;
                    }

                default:
                    {
                        // TODO: VS if this eventually still stays reachable, need to make some error type symbol
                        var tupleType = binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2, diagnostics, syntax);
                        underlyingType = tupleType.Construct(elementTypes);
                        break;
                    }

            }

            return underlyingType;
        }

        internal NamedTypeSymbol UnderlyingTupleType
        {
            get
            {
                return _underlyingType;
            }
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
                return _underlyingType.IsValueType;
            }
        }

        internal sealed override bool IsManagedType
        {
            get
            {
                return _underlyingType.IsManagedType;
            }
        }

        public override bool IsTupleType
        {
            get
            {
                return true;
            }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _underlyingType.ObsoleteAttributeData; }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.CastUp(_fields);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            //PROTOTYPE: PERF do we need to have a dictionary here?
            //      tuples will be typically small 2 or 3 elements only
            return ImmutableArray<Symbol>.CastUp(_fields).WhereAsArray(field => field.Name == name);
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
                return SymbolKind.NamedType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                // only classes and structs can have instance fields as tuple requires.
                // we need to have some support for classes, but most common case will be struct
                // in broken scenarios (ErrorType, Enum, Delegate, whatever..) we will just default to struct.
                return _underlyingType.TypeKind == TypeKind.Class ? TypeKind.Class : TypeKind.Struct;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _underlyingType.ContainingSymbol;
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
            if (ignoreDynamic)
            {
                //PROTOTYPE: rename ignoreDynamic or introduce another "ignoreTuple" flag
                // if ignoring dynamic, compare underlying tuple types
                if (t2.IsTupleType)
                {
                    t2 = (t2 as TupleTypeSymbol).UnderlyingTupleType;
                }
                return _underlyingType.Equals(t2, ignoreCustomModifiers, ignoreDynamic);
            }

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
                return _underlyingType.DeclaredAccessibility;
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

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers
        {
            get
            {
                return ImmutableArray<ImmutableArray<CustomModifier>>.Empty;
            }
        }

        internal override bool HasTypeArgumentsCustomModifiers
        {
            get
            {
                return false;
            }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray<TypeSymbol>.Empty;
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return false;
            }
        }

        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return _fields.Select(f => f.Name);
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return false;
            }
        }

        internal override bool IsComImport
        {
            get
            {
                return false;
            }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get
            {
                return false;
            }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get
            {
                return false;
            }
        }

        internal override bool IsSerializable
        {
            get
            {
                return _underlyingType.IsSerializable;
            }
        }

        internal override TypeLayout Layout
        {
            get
            {
                return _underlyingType.Layout;
            }
        }

        internal override CharSet MarshallingCharSet
        {
            get
            {
                return _underlyingType.MarshallingCharSet;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get
            {
                return _underlyingType.HasDeclarativeSecurity;
            }
        }

        internal override bool IsInterface
        {
            get
            {
                return false;
            }
        }

        #region Use-Site Diagnostics

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return _underlyingType.GetUseSiteDiagnostic();
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _underlyingType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        #endregion

        #region ITupleTypeSymbol Members

        #endregion

        #region ISymbol Members

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return AttributeUsageInfo.Null;
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembers();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return _underlyingType.GetDeclaredBaseType(basesBeingResolved);
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return _underlyingType.GetDeclaredInterfaces(basesBeingResolved);
        }

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw ExceptionUtilities.Unreachable;
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
        /// A tuple without backing fields is usable for binding purposes, since we know its name and type,
        /// but caller is supposed to report some kind of error at declaration.
        /// </summary>
        public TupleFieldSymbol(string name, TupleTypeSymbol containingTuple, TypeSymbol type, FieldSymbol underlyingFieldOpt)
        {
            _name = name;
            _containingTuple = containingTuple;
            _type = type;
            _underlyingFieldOpt = underlyingFieldOpt;
        }

        public FieldSymbol UnderlyingTypleFieldSymbol
        {
            get
            {
                return _underlyingFieldOpt;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
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
