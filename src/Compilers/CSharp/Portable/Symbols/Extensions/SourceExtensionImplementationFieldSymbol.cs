// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

internal sealed class SourceExtensionImplementationFieldSymbol : FieldSymbol
{
    private readonly FieldSymbol _underlyingField;
    private string? _lazyDocComment;
    private StrongBox<byte?>? _lazyNullableContext;

    public SourceExtensionImplementationFieldSymbol(FieldSymbol field)
    {
        Debug.Assert(field.IsExtensionBlockMember());
        Debug.Assert(field.IsConst);
        _underlyingField = field;
    }

    public override Symbol ContainingSymbol => _underlyingField.ContainingType.ContainingSymbol;
    public override bool IsImplicitlyDeclared => true;
    public override FlowAnalysisAnnotations FlowAnalysisAnnotations => _underlyingField.FlowAnalysisAnnotations;
    public override Accessibility DeclaredAccessibility => _underlyingField.DeclaredAccessibility;
    public override string Name => _underlyingField.Name;
    internal override bool HasSpecialName => _underlyingField.HasSpecialName;
    internal override bool HasRuntimeSpecialName => _underlyingField.HasRuntimeSpecialName;
    internal override bool IsNotSerialized => _underlyingField.IsNotSerialized;
    internal override bool HasPointerType => _underlyingField.HasPointerType;
    internal override bool IsMarshalledExplicitly => _underlyingField.IsMarshalledExplicitly;
    internal override MarshalPseudoCustomAttributeData? MarshallingInformation => _underlyingField.MarshallingInformation;
    internal override ImmutableArray<byte> MarshallingDescriptor => _underlyingField.MarshallingDescriptor;
    public override bool IsFixedSizeBuffer => _underlyingField.IsFixedSizeBuffer;
    internal override int? TypeLayoutOffset => _underlyingField.TypeLayoutOffset;
    public override bool IsReadOnly => _underlyingField.IsReadOnly;
    public override bool IsVolatile => _underlyingField.IsVolatile;
    public override bool IsConst => _underlyingField.IsConst;
    internal override ObsoleteAttributeData? ObsoleteAttributeData => _underlyingField.ObsoleteAttributeData;
    public override object? ConstantValue => _underlyingField.ConstantValue;
    public override bool IsStatic => true;
    internal override bool IsRequired => _underlyingField.IsRequired;
    public override RefKind RefKind => _underlyingField.RefKind;
    public override ImmutableArray<CustomModifier> RefCustomModifiers => _underlyingField.RefCustomModifiers;
    public override Symbol? AssociatedSymbol => null;
    internal override CallerUnsafeMode CallerUnsafeMode => _underlyingField.CallerUnsafeMode;

    internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
    {
        return _underlyingField.GetFieldType(fieldsBeingBound);
    }

    internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
    {
        return _underlyingField.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
    }

    public override ImmutableArray<CSharpAttributeData> GetAttributes()
    {
        return _underlyingField.GetAttributes();
    }

    public override ImmutableArray<Location> Locations => _underlyingField.Locations;
    public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _underlyingField.DeclaringSyntaxReferences;

    public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
    {
        // Neither the culture nor the expandIncludes affect the XML for extension implementation fields.
        string result = SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes: false, ref _lazyDocComment);

#if DEBUG
        string? ignored = null;
        string withIncludes = SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes: true, lazyXmlText: ref ignored);
        Debug.Assert(string.Equals(result, withIncludes, System.StringComparison.Ordinal));
#endif

        return result;
    }

    internal override byte? GetLocalNullableContextValue()
    {
        if (_lazyNullableContext is null)
        {
            byte? nullableContext = null;
            var compilation = DeclaringCompilation;
            if (compilation.ShouldEmitNullableAttributes(this))
            {
                var builder = new MostCommonNullableValueBuilder();
                builder.AddValue(TypeWithAnnotations);
                nullableContext = builder.MostCommonValue;
            }

            Interlocked.CompareExchange(ref _lazyNullableContext, new StrongBox<byte?>(nullableContext), null);
        }

        return _lazyNullableContext.Value;
    }

    internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
    {
        base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

        var compilation = DeclaringCompilation;
        var type = Type;
        var value = GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);

        SynthesizedFieldSymbolBase.AddSynthesizedAttributesForFieldType(this, TypeWithAnnotations, moduleBuilder, ref attributes, suppressDynamicAttribute: false);

        if (type.SpecialType == SpecialType.System_Decimal && value is not null)
        {
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDecimalConstantAttribute(value.DecimalValue));
        }
    }
}
