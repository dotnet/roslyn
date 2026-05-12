// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperDescriptorBuilder : TagHelperObjectBuilder<TagHelperDescriptor>
{
    private TagHelperFlags _flags;
    private TagHelperKind _kind;
    private string? _name;
    private string? _assemblyName;
    private TypeNameObject _typeNameObject;
    private DocumentationObject _documentationObject;
    private MetadataObject? _metadataObject;

    private TagHelperDescriptorBuilder()
    {
    }

    internal TagHelperDescriptorBuilder(TagHelperKind kind, string name, string assemblyName)
        : this()
    {
        _kind = kind;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _assemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
    }

    public static TagHelperDescriptorBuilder Create(string name, string assemblyName)
        => new(TagHelperKind.ITagHelper, name, assemblyName);

    public static TagHelperDescriptorBuilder Create(TagHelperKind kind, string name, string assemblyName)
        => new(kind, name, assemblyName);

    public TagHelperKind Kind => _kind;
    public RuntimeKind RuntimeKind { get; set; }

    public string Name => _name.AssumeNotNull();
    public string AssemblyName => _assemblyName.AssumeNotNull();
    public string? DisplayName { get; set; }
    public string? TagOutputHint { get; set; }

    public string? TypeName
    {
        get => _typeNameObject.FullName;
        set => _typeNameObject = TypeNameObject.From(value);
    }

    public string? TypeNamespace => _typeNameObject.Namespace;
    public string? TypeNameIdentifier => _typeNameObject.Name;

    public bool CaseSensitive
    {
        get => _flags.IsFlagSet(TagHelperFlags.CaseSensitive);
        set => _flags.UpdateFlag(TagHelperFlags.CaseSensitive, value);
    }

    public bool IsFullyQualifiedNameMatch
    {
        get => _flags.IsFlagSet(TagHelperFlags.IsFullyQualifiedNameMatch);
        set => _flags.UpdateFlag(TagHelperFlags.IsFullyQualifiedNameMatch, value);
    }

    public bool ClassifyAttributesOnly
    {
        get => _flags.IsFlagSet(TagHelperFlags.ClassifyAttributesOnly);
        set => _flags.UpdateFlag(TagHelperFlags.ClassifyAttributesOnly, value);
    }

    public string? Documentation
    {
        get => _documentationObject.GetText();
        set => _documentationObject = new(value);
    }

    public void SetMetadata(MetadataObject metadataObject)
    {
        _metadataObject = metadataObject;
    }

    public MetadataObject MetadataObject => _metadataObject ?? MetadataObject.None;

    internal void SetTypeName(TypeNameObject typeName)
    {
        _typeNameObject = typeName;
    }

    public void SetTypeName(string fullName, string? typeNamespace, string? typeNameIdentifier)
    {
        _typeNameObject = TypeNameObject.From(fullName, typeNamespace, typeNameIdentifier);
    }

    public void SetTypeName(INamedTypeSymbol namedType)
    {
        _typeNameObject = TypeNameObject.From(namedType);
    }

    public TagHelperObjectBuilderCollection<AllowedChildTagDescriptor, AllowedChildTagDescriptorBuilder> AllowedChildTags { get; }
        = new(AllowedChildTagDescriptorBuilder.Pool);

    public TagHelperObjectBuilderCollection<BoundAttributeDescriptor, BoundAttributeDescriptorBuilder> BoundAttributes { get; }
        = new(BoundAttributeDescriptorBuilder.Pool);

    public TagHelperObjectBuilderCollection<TagMatchingRuleDescriptor, TagMatchingRuleDescriptorBuilder> TagMatchingRules { get; }
        = new(TagMatchingRuleDescriptorBuilder.Pool);

    public void AllowChildTag(Action<AllowedChildTagDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = AllowedChildTagDescriptorBuilder.GetInstance(this);
        configure(builder);
        AllowedChildTags.Add(builder);
    }

    public void BindAttribute(Action<BoundAttributeDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = BoundAttributeDescriptorBuilder.GetInstance(this);
        configure(builder);
        BoundAttributes.Add(builder);
    }

    public void TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = TagMatchingRuleDescriptorBuilder.GetInstance(this);
        configure(builder);
        TagMatchingRules.Add(builder);
    }

    internal void SetDocumentation(string? text)
    {
        _documentationObject = new(text);
    }

    internal void SetDocumentation(DocumentationDescriptor? documentation)
    {
        _documentationObject = new(documentation);
    }

    private protected override TagHelperDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        return new TagHelperDescriptor(
            _flags,
            Kind,
            RuntimeKind,
            Name,
            AssemblyName,
            GetDisplayName(),
            _typeNameObject,
            _documentationObject,
            TagOutputHint,
            TagMatchingRules.ToImmutable(),
            BoundAttributes.ToImmutable(),
            AllowedChildTags.ToImmutable(),
            MetadataObject,
            diagnostics);
    }

    internal string GetDisplayName()
    {
        return DisplayName ?? TypeName ?? Name;
    }
}
