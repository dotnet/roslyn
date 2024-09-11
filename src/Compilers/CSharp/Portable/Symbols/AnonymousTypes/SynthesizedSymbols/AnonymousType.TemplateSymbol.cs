// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
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
                        new AnonymousTypeParameterSymbol(this, fieldIndex, GeneratedNames.MakeAnonymousTypeParameterName(field.Name), allowsRefLikeType: false);
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

            internal override bool HasDeclaredRequiredMembers => false;

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
                        sb.Append('}');
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
