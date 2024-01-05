// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEmbeddedNativeIntegerAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<FieldSymbol> _fields;
        private readonly ImmutableArray<MethodSymbol> _constructors;
        private readonly TypeSymbol _boolType;

        private const string FieldName = "TransformFlags";

        public SynthesizedEmbeddedNativeIntegerAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol systemAttributeType,
            TypeSymbol boolType)
            : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
        {
            _boolType = boolType;

            var boolArrayType = TypeWithAnnotations.Create(
                ArrayTypeSymbol.CreateSZArray(
                    boolType.ContainingAssembly,
                    TypeWithAnnotations.Create(boolType)));

            _fields = ImmutableArray.Create<FieldSymbol>(
                new SynthesizedFieldSymbol(
                    this,
                    boolArrayType.Type,
                    FieldName,
                    isPublic: true,
                    isReadOnly: true,
                    isStatic: false));

            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray<ParameterSymbol>.Empty,
                    (f, s, p) => GenerateParameterlessConstructorBody(f, s)),
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, boolArrayType, 0, RefKind.None)),
                    (f, s, p) => GenerateBoolArrayConstructorBody(f, s, p)));

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.NativeIntegerAttribute.Signatures.Length);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _fields;

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return new AttributeUsageInfo(
                AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue,
                allowMultiple: false,
                inherited: false);
        }

        private void GenerateParameterlessConstructorBody(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements)
        {
            statements.Add(
                factory.ExpressionStatement(
                    factory.AssignmentExpression(
                        factory.Field(
                            factory.This(),
                            _fields.Single()),
                        factory.Array(
                            _boolType,
                            ImmutableArray.Create<BoundExpression>(factory.Literal(true))
                        )
                    )
                )
            );
        }

        private void GenerateBoolArrayConstructorBody(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements, ImmutableArray<ParameterSymbol> parameters)
        {
            statements.Add(
                factory.ExpressionStatement(
                    factory.AssignmentExpression(
                        factory.Field(
                            factory.This(),
                            _fields.Single()),
                        factory.Parameter(parameters.Single())
                    )
                )
            );
        }
    }
}

