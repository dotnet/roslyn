// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        private sealed class NameAndIndex
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
        private sealed class AnonymousTypeTemplateSymbol : NamedTypeSymbol
        {
            /// <summary> Name to be used as metadata name during emit </summary>
            private NameAndIndex nameAndIndex;

            private readonly ImmutableArray<TypeParameterSymbol> typeParameters;
            private readonly ImmutableArray<Symbol> members;

            /// <summary> This list consists of synthesized method symbols for ToString, 
            /// Equals and GetHashCode which are not part of symbol table </summary>
            internal readonly ImmutableArray<MethodSymbol> SpecialMembers;

            /// <summary> Properties defined in the template </summary>
            internal readonly ImmutableArray<AnonymousTypePropertySymbol> Properties;

            /// <summary> Maps member names to symbol(s) </summary>
            private readonly MultiDictionary<string, Symbol> name2symbol = new MultiDictionary<string, Symbol>();

            /// <summary> Anonymous type manager owning this template </summary>
            internal readonly AnonymousTypeManager Manager;

            /// <summary> Smallest location of the template, actually contains the smallest location 
            /// of all the anonymous type instances created using this template during EMIT </summary>
            private Location smallestLocation;

            /// <summary> Key pf the anonymous type descriptor </summary>
            internal readonly string TypeDescriptorKey;

            internal AnonymousTypeTemplateSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr)
            {
                this.Manager = manager;
                this.TypeDescriptorKey = typeDescr.Key;
                this.smallestLocation = typeDescr.Location;

                // Will be set when the type's metadata is ready to be emitted, 
                // <anonymous-type>.Name will throw exception if requested
                // before that moment.
                this.nameAndIndex = null;

                int fieldsCount = typeDescr.Fields.Length;

                // members
                Symbol[] members = new Symbol[fieldsCount * 3 + 1];
                int memberIndex = 0;

                // The array storing property symbols to be used in 
                // generation of constructor and other methods
                if (fieldsCount > 0)
                {
                    AnonymousTypePropertySymbol[] propertiesArray = new AnonymousTypePropertySymbol[fieldsCount];
                    TypeParameterSymbol[] typeParametersArray = new TypeParameterSymbol[fieldsCount];

                    // Process fields
                    for (int fieldIndex = 0; fieldIndex < fieldsCount; fieldIndex++)
                    {
                        AnonymousTypeField field = typeDescr.Fields[fieldIndex];

                        // Add a type parameter
                        AnonymousTypeParameterSymbol typeParameter =
                            new AnonymousTypeParameterSymbol(this, fieldIndex, GeneratedNames.MakeAnonymousTypeParameterName(field.Name));
                        typeParametersArray[fieldIndex] = typeParameter;

                        // Add a property
                        AnonymousTypePropertySymbol property = new AnonymousTypePropertySymbol(this, field, typeParameter);
                        propertiesArray[fieldIndex] = property;

                        // Property related symbols
                        members[memberIndex++] = property;
                        members[memberIndex++] = property.BackingField;
                        members[memberIndex++] = property.GetMethod;
                    }

                    this.typeParameters = typeParametersArray.AsImmutable();
                    this.Properties = propertiesArray.AsImmutable();
                }
                else
                {
                    this.typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                    this.Properties = ImmutableArray<AnonymousTypePropertySymbol>.Empty;
                }

                // Add a constructor
                members[memberIndex++] = new AnonymousTypeConstructorSymbol(this, this.Properties);
                this.members = members.AsImmutable();

                Debug.Assert(memberIndex == this.members.Length);

                // fill name2symbol map
                foreach (var symbol in this.members)
                {
                    this.name2symbol.Add(symbol.Name, symbol);
                }

                // special members: Equals, GetHashCode, ToString
                MethodSymbol[] specialMembers = new MethodSymbol[3];
                specialMembers[0] = new AnonymousTypeEqualsMethodSymbol(this);
                specialMembers[1] = new AnonymousTypeGetHashCodeMethodSymbol(this);
                specialMembers[2] = new AnonymousTypeToStringMethodSymbol(this);
                this.SpecialMembers = specialMembers.AsImmutable();
            }

            internal ImmutableArray<string> GetPropertyNames()
            {
                return this.Properties.SelectAsArray(p => p.Name);
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
                    Debug.Assert(this.smallestLocation != null);
                    return this.smallestLocation;
                }
            }

            internal NameAndIndex NameAndIndex
            {
                get
                {
                    return this.nameAndIndex;
                }
                set
                {
                    var oldValue = Interlocked.CompareExchange(ref this.nameAndIndex, value, null);
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

                    Location currentSmallestLocation = this.smallestLocation;
                    if (currentSmallestLocation != null && this.Manager.Compilation.CompareSourceLocations(currentSmallestLocation, location) < 0)
                    {
                        // The template's smallest location do not need to be changed
                        return;
                    }

                    if (ReferenceEquals(Interlocked.CompareExchange(ref this.smallestLocation, location, currentSmallestLocation), currentSmallestLocation))
                    {
                        // Changed successfully, proceed to updating the fields
                        return;
                    }
                }
            }

            public override ImmutableArray<Symbol> GetMembers()
            {
                return this.members;
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

            internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
            {
                get { return StaticCast<TypeSymbol>.From(this.TypeParameters); }
            }

            public override ImmutableArray<Symbol> GetMembers(string name)
            {
                int count = this.name2symbol.GetCountForKey(name);

                if (count == 0)
                {
                    return ImmutableArray<Symbol>.Empty;
                }
                else if (count == 1)
                {
                    Symbol symbol = null;
                    this.name2symbol.TryGetSingleValue(name, out symbol);
                    return ImmutableArray.Create<Symbol>(symbol);
                }
                else
                {
                    return ImmutableArray.CreateRange<Symbol>(this.name2symbol[name]);
                }
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
                get { return this.name2symbol.Keys; }
            }

            public override Symbol ContainingSymbol
            {
                get { return this.Manager.Compilation.SourceModule.GlobalNamespace; }
            }

            public override string Name
            {
                get { return this.nameAndIndex.Name; }
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
                get { return this.typeParameters.Length; }
            }

            public override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            public override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return this.typeParameters; }
            }

            public override bool IsAbstract
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

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
            {
                get { return ImmutableArray<NamedTypeSymbol>.Empty; }
            }

            internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
            {
                get { return this.Manager.System_Object; }
            }

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

            internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
            {
                return this.Manager.System_Object;
            }

            internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
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

            internal override bool IsSerializable
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

            internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(compilationState, ref attributes);

                AddSynthesizedAttribute(ref attributes, Manager.Compilation.SynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

                if (Manager.Compilation.Options.DebugInformationKind == DebugInformationKind.Full)
                {
                    AddSynthesizedAttribute(ref attributes, SynthesizeDebuggerDisplayAttribute());
                }
            }

            private SynthesizedAttributeData SynthesizeDebuggerDisplayAttribute()
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

                return Manager.Compilation.SynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor,
                    arguments: ImmutableArray.Create(new TypedConstant(Manager.System_String, TypedConstantKind.Primitive, displayString)),
                    namedArguments: ImmutableArray.Create(new KeyValuePair<string, TypedConstant>(
                                        "Type", new TypedConstant(Manager.System_String, TypedConstantKind.Primitive, "<Anonymous Type>"))));
            }
        }
    }
}
