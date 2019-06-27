// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEmbeddedNullableAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<FieldSymbol> _fields;

        private readonly ImmutableArray<MethodSymbol> _constructors;

        private readonly TypeSymbol _byteTypeSymbol;

        private const string NullableFlagsFieldName = "NullableFlags";

        public SynthesizedEmbeddedNullableAttributeSymbol(
            CSharpCompilation compilation,
            DiagnosticBag diagnostics)
            : base(AttributeDescription.NullableAttribute, compilation, diagnostics)
        {
            var byteType = TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Byte));
            _byteTypeSymbol = byteType.Type;
            Binder.ReportUseSiteDiagnostics(_byteTypeSymbol, diagnostics, Location.None);

            var byteArrayType = TypeWithAnnotations.Create(
                ArrayTypeSymbol.CreateSZArray(
                    _byteTypeSymbol.ContainingAssembly,
                    byteType));

            _fields = ImmutableArray.Create<FieldSymbol>(
                new SynthesizedFieldSymbol(
                    this,
                    byteArrayType.Type,
                    NullableFlagsFieldName,
                    isPublic: true,
                    isReadOnly: true,
                    isStatic: false));

            _constructors = ImmutableArray.Create<MethodSymbol>(
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, byteType, 0, RefKind.None)),
                    GenerateSingleByteConstructorBody
                    ),
                new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                    this,
                    m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, byteArrayType, 0, RefKind.None)),
                    GenerateByteArrayConstructorBody
                    )
                );

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.NullableAttribute.Signatures.Length);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _fields;

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        private void GenerateByteArrayConstructorBody(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements, ImmutableArray<ParameterSymbol> parameters)
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

        private void GenerateSingleByteConstructorBody(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements, ImmutableArray<ParameterSymbol> parameters)
        {
            statements.Add(
                factory.ExpressionStatement(
                    factory.AssignmentExpression(
                        factory.Field(
                            factory.This(),
                            _fields.Single()),
                        factory.Array(
                            _byteTypeSymbol,
                            ImmutableArray.Create<BoundExpression>(
                                factory.Parameter(parameters.Single())
                            )
                        )
                    )
                )
            );
        }
    }

    internal sealed class SynthesizedEmbeddedAttributeConstructorWithBodySymbol : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        private readonly Action<SyntheticBoundNodeFactory, ArrayBuilder<BoundStatement>, ImmutableArray<ParameterSymbol>> _getConstructorBody;

        internal SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
            NamedTypeSymbol containingType,
            Func<MethodSymbol, ImmutableArray<ParameterSymbol>> getParameters,
            Action<SyntheticBoundNodeFactory, ArrayBuilder<BoundStatement>, ImmutableArray<ParameterSymbol>> getConstructorBody) :
            base(containingType)
        {
            _parameters = getParameters(this);
            _getConstructorBody = getConstructorBody;
        }

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            GenerateMethodBodyCore(compilationState, diagnostics);
        }

        protected override void GenerateMethodBodyStatements(SyntheticBoundNodeFactory factory, ArrayBuilder<BoundStatement> statements, DiagnosticBag diagnostics) => _getConstructorBody(factory, statements, _parameters);
    }
}

