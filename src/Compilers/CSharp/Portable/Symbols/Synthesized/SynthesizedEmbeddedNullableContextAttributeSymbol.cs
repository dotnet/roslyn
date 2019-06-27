// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            CSharpCompilation compilation,
            DiagnosticBag diagnostics)
            : base(AttributeDescription.NullableContextAttribute, compilation, diagnostics)
        {
            var byteType = compilation.GetSpecialType(SpecialType.System_Byte);
            Binder.ReportUseSiteDiagnostics(byteType, diagnostics, Location.None);

            _fields = ImmutableArray.Create<FieldSymbol>(
                new SynthesizedFieldSymbol(
                    this,
                    byteType,
                    "Flag",
                    isPublic: true,
                    isReadOnly: true,
                    isStatic: false));

            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, TypeWithAnnotations.Create(byteType), 0, RefKind.None)),
                    GenerateConstructorBody));

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.NullableContextAttribute.Signatures.Length);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _fields;

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

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
