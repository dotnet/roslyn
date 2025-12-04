// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

// PROTOTYPE: Confirm the attribute shape in BCL API review.
// PROTOTYPE: Use a property instead of a field (like SynthesizedEmbeddedExtensionMarkerAttributeSymbol).
/// <summary>
/// <code>
/// namespace System.Runtime.CompilerServices
/// {
///     [CompilerGenerated, Microsoft.CodeAnalysis.Embedded]
///     [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
///     public sealed class MemorySafetyRulesAttribute : Attribute
///     {
///         public readonly int Version;
///         public MemorySafetyRulesAttribute(int version) { Version = version; }
///     }
/// }
/// </code>
/// </summary>
internal sealed class SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
{
    private readonly ImmutableArray<FieldSymbol> _fields;
    private readonly ImmutableArray<MethodSymbol> _constructors;

    public SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol(
        string name,
        NamespaceSymbol containingNamespace,
        ModuleSymbol containingModule,
        NamedTypeSymbol systemAttributeType,
        TypeSymbol int32Type)
        : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
    {
        _fields =
        [
            new SynthesizedFieldSymbol(
                containingType: this,
                type: int32Type,
                name: "Version",
                accessibility: DeclarationModifiers.Public,
                isReadOnly: true,
                isStatic: false),
        ];

        _constructors =
        [
            new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(
                containingType: this,
                getParameters: m => [SynthesizedParameterSymbol.Create(
                    container: m,
                    type: TypeWithAnnotations.Create(int32Type),
                    ordinal: 0,
                    refKind: RefKind.None,
                    name: "version")],
                getConstructorBody: GenerateConstructorBody),
        ];
    }

    internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => _fields;

    public override ImmutableArray<MethodSymbol> Constructors => _constructors;

    internal override AttributeUsageInfo GetAttributeUsageInfo()
    {
        return new AttributeUsageInfo(AttributeTargets.Module, allowMultiple: false, inherited: false);
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
