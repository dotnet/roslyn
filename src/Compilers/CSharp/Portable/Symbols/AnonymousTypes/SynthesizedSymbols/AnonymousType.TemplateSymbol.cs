// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
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

        internal abstract class AnonymousTypeOrDelegateTemplateSymbol : NamedTypeSymbol
        {
            /// <summary> Name to be used as metadata name during emit </summary>
            private NameAndIndex _nameAndIndex;

            /// <summary> Smallest location of the template, actually contains the smallest location 
            /// of all the anonymous type instances created using this template during EMIT </summary>
            private Location _smallestLocation;

            /// <summary> Anonymous type manager owning this template </summary>
            internal readonly AnonymousTypeManager Manager;

            internal AnonymousTypeOrDelegateTemplateSymbol(AnonymousTypeManager manager, Location location)
            {
                this.Manager = manager;
                _smallestLocation = location;

                // Will be set when the type's metadata is ready to be emitted, 
                // <anonymous-type>.Name will throw exception if requested
                // before that moment.
                _nameAndIndex = null;
            }

            internal abstract string TypeDescriptorKey { get; }

            protected sealed override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
                => throw ExceptionUtilities.Unreachable;

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

            internal sealed override bool HasCodeAnalysisEmbeddedAttribute => false;

            internal sealed override bool IsInterpolatedStringHandlerType => false;

            internal sealed override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
            {
                return this.GetMembersUnordered();
            }

            internal sealed override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
            {
                return this.GetMembers(name);
            }

            public sealed override Symbol ContainingSymbol
            {
                get { return this.Manager.Compilation.SourceModule.GlobalNamespace; }
            }

            public sealed override string Name
            {
                get { return _nameAndIndex.Name; }
            }

            internal sealed override bool HasSpecialName
            {
                get { return false; }
            }

            public sealed override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            public sealed override bool IsAbstract
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

            public sealed override bool IsSealed
            {
                get { return true; }
            }

            public sealed override bool MightContainExtensionMethods
            {
                get { return false; }
            }

            public sealed override bool AreLocalsZeroed
            {
                get { return ContainingModule.AreLocalsZeroed; }
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Internal; }
            }

            internal sealed override bool IsInterface
            {
                get { return false; }
            }

            public sealed override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public sealed override bool IsStatic
            {
                get { return false; }
            }

            public sealed override NamedTypeSymbol ConstructedFrom
            {
                get { return this; }
            }

            internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
            {
                return this.Manager.System_Object;
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal sealed override bool MangleName
            {
                get { return this.Arity > 0; }
            }

            internal sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
            {
                get { return GetTypeParametersAsTypeArguments(); }
            }

            public sealed override int Arity
            {
                get { return TypeParameters.Length; }
            }

            internal sealed override bool ShouldAddWinRTMembers
            {
                get { return false; }
            }

            internal sealed override bool IsWindowsRuntimeImport
            {
                get { return false; }
            }

            internal sealed override bool IsComImport
            {
                get { return false; }
            }

            internal sealed override ObsoleteAttributeData ObsoleteAttributeData
            {
                get { return null; }
            }

            internal sealed override TypeLayout Layout
            {
                get { return default(TypeLayout); }
            }

            internal sealed override CharSet MarshallingCharSet
            {
                get { return DefaultMarshallingCharSet; }
            }

            public sealed override bool IsSerializable
            {
                get { return false; }
            }

            internal sealed override bool HasDeclarativeSecurity
            {
                get { return false; }
            }

            internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                return ImmutableArray<string>.Empty;
            }

            internal sealed override AttributeUsageInfo GetAttributeUsageInfo()
            {
                return AttributeUsageInfo.Null;
            }

            internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable;

            internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;

            internal sealed override bool IsRecord => false;

            internal sealed override bool IsRecordStruct => false;

            internal sealed override bool HasPossibleWellKnownCloneMethod() => false;

            internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
            {
                return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
            }
        }

        /// <summary>
        /// Represents an anonymous type 'template' which is a generic type to be used for all 
        /// anonymous types having the same structure, i.e. the same number of fields and field names.
        /// </summary>
        internal sealed class AnonymousTypeTemplateSymbol : AnonymousTypeOrDelegateTemplateSymbol
        {
            private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
            private readonly ImmutableArray<Symbol> _members;

            /// <summary> This list consists of synthesized method symbols for ToString, 
            /// Equals and GetHashCode which are not part of symbol table </summary>
            internal readonly ImmutableArray<MethodSymbol> SpecialMembers;

            /// <summary> Properties defined in the template </summary>
            internal readonly ImmutableArray<AnonymousTypePropertySymbol> Properties;

            /// <summary> Maps member names to symbol(s) </summary>
            private readonly MultiDictionary<string, Symbol> _nameToSymbols = new MultiDictionary<string, Symbol>();

            internal AnonymousTypeTemplateSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr) :
                base(manager, typeDescr.Location)
            {
                this.TypeDescriptorKey = typeDescr.Key;

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

            internal override string TypeDescriptorKey { get; }

            public override TypeKind TypeKind
            {
                get { return TypeKind.Class; }
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

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => this.Manager.System_Object;

            public override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return _typeParameters; }
            }

            public override IEnumerable<string> MemberNames
            {
                get { return _nameToSymbols.Keys; }
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
                // Escape open '{' with '\' to avoid parsing it as an embedded expression.

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
