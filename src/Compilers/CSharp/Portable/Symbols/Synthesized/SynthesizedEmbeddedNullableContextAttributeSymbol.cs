// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEmbeddedNullableContextAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<FieldSymbol> _fields;
        private readonly ImmutableArray<MethodSymbol> _constructors;

        public SynthesizedEmbeddedNullableContextAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol systemAttributeType,
            TypeSymbol systemByteType)
            : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
        {
            _fields = ImmutableArray.Create<FieldSymbol>(
                new SynthesizedFieldSymbol(
                    this,
                    systemByteType,
                    "Flag",
                    isPublic: true,
                    isReadOnly: true,
                    isStatic: false));

            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, TypeWithAnnotations.Create(systemByteType), 0, RefKind.None)),
                    GenerateConstructorBody));

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.NullableContextAttribute.Signatures.Length);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _fields;

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return new AttributeUsageInfo(
                AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct,
                allowMultiple: false,
                inherited: false);
        }

        private void GenerateConstructorBody(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements, ImmutableArray<ParameterSymbol> parameters)
        {
            statements.Add(
                factory.ExpressionStatement(
                    factory.AssignmentExpression(
                        factory.Field(factory.This(), _fields.Single()),
                        factory.Parameter(parameters.Single()))));
        }
    }
}
