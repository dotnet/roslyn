// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEmbeddedNullablePublicOnlyAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<FieldSymbol> _fields;
        private readonly ImmutableArray<MethodSymbol> _constructors;

        public SynthesizedEmbeddedNullablePublicOnlyAttributeSymbol(
            CSharpCompilation compilation,
            DiagnosticBag diagnostics)
            : base(AttributeDescription.NullablePublicOnlyAttribute, compilation, diagnostics)
        {
            var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
            Binder.ReportUseSiteDiagnostics(boolType, diagnostics, Location.None);

            _fields = ImmutableArray.Create<FieldSymbol>(
                new SynthesizedFieldSymbol(
                    this,
                    boolType,
                    "IncludesInternals",
                    isPublic: true,
                    isReadOnly: true,
                    isStatic: false));

            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, TypeWithAnnotations.Create(boolType), 0, RefKind.None)),
                    GenerateConstructorBody));

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.NullablePublicOnlyAttribute.Signatures.Length);
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
