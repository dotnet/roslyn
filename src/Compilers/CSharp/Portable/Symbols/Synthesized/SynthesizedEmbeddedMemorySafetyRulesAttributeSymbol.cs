// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

// PROTOTYPE: Confirm the attribute shape in BCL API review.
/// <summary>
/// <code>
/// namespace System.Runtime.CompilerServices
/// {
///     [CompilerGenerated, Microsoft.CodeAnalysis.Embedded]
///     [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
///     public sealed class MemorySafetyRulesAttribute : Attribute
///     {
///         public int Version { get; }
///         public MemorySafetyRulesAttribute(int version) { Version = version; }
///     }
/// }
/// </code>
/// </summary>
internal sealed class SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
{
    private readonly ImmutableArray<FieldSymbol> _fields;
    private readonly ImmutableArray<PropertySymbol> _properties;
    private readonly ImmutableArray<MethodSymbol> _constructors;

    public SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol(
        string name,
        NamespaceSymbol containingNamespace,
        ModuleSymbol containingModule,
        NamedTypeSymbol systemAttributeType,
        TypeSymbol int32Type)
        : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
    {
        const string PropertyName = "Version";

        var field = new SynthesizedFieldSymbol(
            containingType: this,
            type: int32Type,
            name: GeneratedNames.MakeBackingFieldName(PropertyName),
            accessibility: DeclarationModifiers.Private,
            isReadOnly: true,
            isStatic: false);

        _fields = [field];

        _properties =
        [
            new SynthesizedPropertySymbol(PropertyName, field),
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

    private ArrayBuilder<T> GetMemberBuilder<T>(Func<Symbol, T> selector)
    {
        var builder = ArrayBuilder<T>.GetInstance(
            _fields.Length + _properties.Length * 2 + _constructors.Length);

        builder.AddRange(_fields, selector);

        builder.AddRange(_properties, selector);

        foreach (var property in _properties)
        {
            Debug.Assert(property.GetMethod is not null);
            Debug.Assert(property.SetMethod is null);
            builder.Add(selector(property.GetMethod));
        }

        builder.AddRange(_constructors, selector);

        return builder;
    }

    public override ImmutableArray<Symbol> GetMembers()
    {
        return GetMemberBuilder(static s => s).ToImmutableAndFree();
    }

    public override ImmutableArray<Symbol> GetMembers(string name)
    {
        var builder = GetMemberBuilder(static s => s);
        builder.RemoveAll(static (s, name) => s.Name != name, name);
        return builder.ToImmutableAndFree();
    }

    public override IEnumerable<string> MemberNames
    {
        get
        {
            return GetMemberBuilder(static s => s.Name).ToImmutableAndFree();
        }
    }
}
