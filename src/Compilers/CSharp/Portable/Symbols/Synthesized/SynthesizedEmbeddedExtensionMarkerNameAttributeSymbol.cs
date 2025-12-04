// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class SynthesizedEmbeddedExtensionMarkerAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<MethodSymbol> _constructors;
        private readonly SynthesizedFieldSymbol _nameField;
        private readonly SynthesizedPropertySymbol _nameProperty;

        private const string PropertyName = "Name";
        private const string FieldName = "<Name>k__BackingField";

        public SynthesizedEmbeddedExtensionMarkerAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol systemAttributeType,
            TypeSymbol systemStringType)
            : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
        {
            Debug.Assert(FieldName == GeneratedNames.MakeBackingFieldName(PropertyName));

            _nameField = new SynthesizedFieldSymbol(this, systemStringType, FieldName, isReadOnly: true);
            _nameProperty = new SynthesizedPropertySymbol(PropertyName, _nameField);
            _constructors = [new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(this, getConstructorParameters, getConstructorBody)];

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.ExtensionMarkerAttribute.Signatures.Length);

            ImmutableArray<ParameterSymbol> getConstructorParameters(MethodSymbol ctor)
            {
                return [SynthesizedParameterSymbol.Create(ctor, TypeWithAnnotations.Create(systemStringType), ordinal: 0, RefKind.None, name: "name")];
            }

            void getConstructorBody(SyntheticBoundNodeFactory f, ArrayBuilder<BoundStatement> statements, ImmutableArray<ParameterSymbol> parameters)
            {
                // this._namedField = name;
                statements.Add(f.Assignment(
                    f.Field(f.This(), this._nameField),
                    f.Parameter(parameters[0])));
            }
        }

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return new AttributeUsageInfo(
                AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property |
                AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
                allowMultiple: false, inherited: false);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            return [_nameField];
        }

        public override ImmutableArray<Symbol> GetMembers()
            => [_nameField, _nameProperty, _nameProperty.GetMethod, _constructors[0]];

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return name switch
            {
                FieldName => [_nameField],
                PropertyName => [_nameProperty],
                WellKnownMemberNames.InstanceConstructorName => ImmutableArray<Symbol>.CastUp(_constructors),
                _ => []
            };
        }

        public override IEnumerable<string> MemberNames
            => [FieldName, PropertyName, WellKnownMemberNames.InstanceConstructorName];
    }
}
