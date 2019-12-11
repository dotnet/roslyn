// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        internal sealed class NameAndIndex
        {
            public NameAndIndex(string name, int index)
            {
                this.Name = name;
                this.Index = index;
            }

            public readonly string Name;
            public readonly int Index;
        }

        /// <summary>
        /// Represents an anonymous type 'template' which is a generic type to be used for all 
        /// anonymous type having the same structure, i.e. the same number of fields and field names.
        /// </summary>
        internal sealed class AnonymousTypeTemplateSymbol : NamedTypeSymbol
        {
            /// <summary> Name to be used as metadata name during emit </summary>
            private NameAndIndex _nameAndIndex;

            private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
            private readonly ImmutableArray<Symbol> _members;

            /// <summary> This list consists of synthesized method symbols for ToString, 
            /// Equals and GetHashCode which are not part of symbol table </summary>
            internal readonly ImmutableArray<MethodSymbol> SpecialMembers;

            /// <summary> Properties defined in the template </summary>
            internal readonly ImmutableArray<AnonymousTypePropertySymbol> Properties;

            /// <summary> Maps member names to symbol(s) </summary>
            private readonly MultiDictionary<string, Symbol> _nameToSymbols = new MultiDictionary<string, Symbol>();

            /// <summary> Anonymous type manager owning this template </summary>
            internal readonly AnonymousTypeManager Manager;

            /// <summary> Smallest location of the template, actually contains the smallest location 
            /// of all the anonymous type instances created using this template during EMIT </summary>
            private Location _smallestLocation;

            /// <summary> Key pf the anonymous type descriptor </summary>
            internal readonly string TypeDescriptorKey;

            internal AnonymousTypeTemplateSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr)
            {
                this.Manager = manager;
                this.TypeDescriptorKey = typeDescr.Key;
                _smallestLocation = typeDescr.Location;

                // Will be set when the type's metadata is ready to be emitted, 
                // <anonymous-type>.Name will throw exception if requested
                // before that moment.
                _nameAndIndex = null;

                int fieldsCount = typeDescr.Fields.Length;
                int membersCount = fieldsCount * 3 + 1;

                // members
                var membersBuilder = ArrayBuilder<Symbol>.GetInstance(membersCount);
                var propertiesBuilder = ArrayBuilder<AnonymousTypePropertySymbol>.GetInstance(fieldsCount);
                var typeParametersBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance(fieldsCount);

                // Process fields
                for (int fieldIndex = 0; fieldIndex < fieldsCount; fieldIndex++)
                {
                    AnonymousTypeField field = typeDescr.Fields[fieldIndex];

                    // Add a type parameter
                    AnonymousTypeParameterSymbol typeParameter =
                        new AnonymousTypeParameterSymbol(this, fieldIndex, GeneratedNames.MakeAnonymousTypeParameterName(field.Name));
                    typeParametersBuilder.Add(typeParameter);

                    // Add a property
                    AnonymousTypePropertySymbol property = new AnonymousTypePropertySymbol(this, field, TypeWithAnnotations.Create(typeParameter), fieldIndex);
                    propertiesBuilder.Add(property);

                    // Property related symbols
                    membersBuilder.Add(property);
                    membersBuilder.Add(property.BackingField);
                    membersBuilder.Add(property.GetMethod);
                }

                _typeParameters = typeParametersBuilder.ToImmutableAndFree();
                this.Properties = propertiesBuilder.ToImmutableAndFree();

                // Add a constructor
                membersBuilder.Add(new AnonymousTypeConstructorSymbol(this, this.Properties));
                _members = membersBuilder.ToImmutableAndFree();
                Debug.Assert(membersCount == _members.Length);

                // fill nameToSymbols map
                foreach (var symbol in _members)
                {
                    _nameToSymbols.Add(symbol.Name, symbol);
                }

                // special members: Equals, GetHashCode, ToString
                this.SpecialMembers = ImmutableArray.Create<MethodSymbol>(
                    new AnonymousTypeEqualsMethodSymbol(this),
                    new AnonymousTypeGetHashCodeMethodSymbol(this),
                    new AnonymousTypeToStringMethodSymbol(this));
            }

            internal AnonymousTypeKey GetAnonymousTypeKey()
            {
                var properties = Properties.SelectAsArray(p => new AnonymousTypeKeyField(p.Name, isKey: false, ignoreCase: false));
                return new AnonymousTypeKey(properties);
            }

            /// <summary>
            /// Smallest location of the template, actually contains the smallest location 
            /// of all the anonymous type instances created using this template during EMIT;
            /// 
            /// NOTE: if this property is queried, smallest location must not be null.
            /// </summary>
            internal Location SmallestLocation
            {
                get
                {
                    Debug.Assert(_smallestLocation != null);
                    return _smallestLocation;
                }
            }

            internal NameAndIndex NameAndIndex
            {
                get
                {
                    return _nameAndIndex;
                }
                set
                {
                    var oldValue = Interlocked.CompareExchange(ref _nameAndIndex, value, null);
                    Debug.Assert(oldValue == null ||
                        ((oldValue.Name == value.Name) && (oldValue.Index == value.Index)));
                }
            }

            /// <summary>
            /// In emit phase every time a created anonymous type is referenced we try to store the lowest 
            /// location of the template. It will be used for ordering templates and assigning emitted type names.
            /// </summary>
            internal void AdjustLocation(Location location)
            {
                Debug.Assert(location.IsInSource);

                while (true)
                {
                    // Loop until we managed to set location OR we detected that we don't need to set it 
                    // in case 'location' in type descriptor is bigger that the one in smallestLocation

                    Location currentSmallestLocation = _smallestLocation;
                    if (currentSmallestLocation != null && this.Manager.Compilation.CompareSourceLocations(currentSmallestLocation, location) < 0)
                    {
                        // The template's smallest location do not need to be changed
                        return;
                    }

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _smallestLocation, location, currentSmallestLocation), currentSmallestLocation))
                    {
                        // Changed successfully, proceed to updating the fields
                        return;
                    }
                }
            }

            public override ImmutableArray<Symbol> GetMembers()
            {
                return _members;
            }

            internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
            {
                foreach (var m in this.GetMembers())
                {
                    switch (m.Kind)
                    {
                        case SymbolKind.Field:
                            yield return (FieldSymbol)m;
                            break;
                    }
                }
            }

            internal override bool HasCodeAnalysisEmbeddedAttribute => false;

            internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
            {
                get { return GetTypeParametersAsTypeArguments(); }
            }

            public override ImmutableArray<Symbol> GetMembers(string name)
            {
                var symbols = _nameToSymbols[name];
                var builder = ArrayBuilder<Symbol>.GetInstance(symbols.Count);
                foreach (var symbol in symbols)
                {
                    builder.Add(symbol);
                }

                return builder.ToImmutableAndFree();
            }

            internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
            {
                return this.GetMembersUnordered();
            }

            internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
            {
                return this.GetMembers(name);
            }

            public override IEnumerable<string> MemberNames
            {
                get { return _nameToSymbols.Keys; }
            }

            public override Symbol ContainingSymbol
            {
                get { return this.Manager.Compilation.SourceModule.GlobalNamespace; }
            }

            public override string Name
            {
                get { return _nameAndIndex.Name; }
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }

            internal override bool MangleName
            {
                get { return this.Arity > 0; }
            }

            public override int Arity
            {
                get { return _typeParameters.Length; }
            }

            public override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            public override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return _typeParameters; }
            }

            public override bool IsAbstract
            {
                get { return false; }
            }

            public sealed override bool IsRefLikeType
            {
                get { return false; }
            }

            public sealed override bool IsReadOnly
            {
                get { return false; }
            }

            public override bool IsSealed
            {
                get { return true; }
            }

            public override bool MightContainExtensionMethods
            {
                get { return false; }
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

            public override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Internal; }
            }

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => this.Manager.System_Object;

            public override TypeKind TypeKind
            {
                get { return TypeKind.Class; }
            }

            internal override bool IsInterface
            {
                get { return false; }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public override bool IsStatic
            {
                get { return false; }
            }

            public override NamedTypeSymbol ConstructedFrom
            {
                get { return this; }
            }

            internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
            {
                return this.Manager.System_Object;
            }

            internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override bool ShouldAddWinRTMembers
            {
                get { return false; }
            }

            internal override bool IsWindowsRuntimeImport
            {
                get { return false; }
            }

            internal override bool IsComImport
            {
                get { return false; }
            }

            internal sealed override ObsoleteAttributeData ObsoleteAttributeData
            {
                get { return null; }
            }

            internal override TypeLayout Layout
            {
                get { return default(TypeLayout); }
            }

            internal override CharSet MarshallingCharSet
            {
                get { return DefaultMarshallingCharSet; }
            }

            public override bool IsSerializable
            {
                get { return false; }
            }

            internal override bool HasDeclarativeSecurity
            {
                get { return false; }
            }

            internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                return ImmutableArray<string>.Empty;
            }

            internal override AttributeUsageInfo GetAttributeUsageInfo()
            {
                return AttributeUsageInfo.Null;
            }

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

                AddSynthesizedAttribute(ref attributes, Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

                if (Manager.Compilation.Options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    AddSynthesizedAttribute(ref attributes, TrySynthesizeDebuggerDisplayAttribute());
                }
            }

            /// <summary>
            /// Returns a synthesized debugger display attribute or null if one
            /// could not be synthesized.
            /// </summary>
            private SynthesizedAttributeData TrySynthesizeDebuggerDisplayAttribute()
            {
                string displayString;
                if (this.Properties.Length == 0)
                {
                    displayString = "\\{ }";
                }
                else
                {
                    var builder = PooledStringBuilder.GetInstance();
                    var sb = builder.Builder;

                    sb.Append("\\{ ");
                    int displayCount = Math.Min(this.Properties.Length, 10);

                    for (var fieldIndex = 0; fieldIndex < displayCount; fieldIndex++)
                    {
                        string fieldName = this.Properties[fieldIndex].Name;

                        if (fieldIndex > 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(fieldName);
                        sb.Append(" = {");
                        sb.Append(fieldName);
                        sb.Append("}");
                    }

                    if (this.Properties.Length > displayCount)
                    {
                        sb.Append(" ...");
                    }

                    sb.Append(" }");
                    displayString = builder.ToStringAndFree();
                }

                return Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor,
                    arguments: ImmutableArray.Create(new TypedConstant(Manager.System_String, TypedConstantKind.Primitive, displayString)),
                    namedArguments: ImmutableArray.Create(new KeyValuePair<WellKnownMember, TypedConstant>(
                                        WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type,
                                        new TypedConstant(Manager.System_String, TypedConstantKind.Primitive, "<Anonymous Type>"))));
            }
        }
    }
}
