// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A metadata class describing a tag helper attribute.
/// </summary>
public sealed class BoundAttributeDescriptor : TagHelperObject<BoundAttributeDescriptor>
{
    private readonly BoundAttributeFlags _flags;

    private TagHelperDescriptor? _parent;

    public BoundAttributeFlags Flags => _flags;
    public string Name { get; }
    public string PropertyName { get; }
    public string TypeName => TypeNameObject.FullName.AssumeNotNull();
    public string DisplayName { get; }
    public string? ContainingType { get; }

    public string? Documentation => DocumentationObject.GetText();

    internal TypeNameObject TypeNameObject { get; }
    internal TypeNameObject IndexerTypeNameObject { get; }
    internal DocumentationObject DocumentationObject { get; }

    public string? IndexerNamePrefix { get; }
    public string? IndexerTypeName => IndexerTypeNameObject.FullName;

    public bool CaseSensitive => _flags.IsFlagSet(BoundAttributeFlags.CaseSensitive);
    public bool HasIndexer => _flags.IsFlagSet(BoundAttributeFlags.HasIndexer);
    public bool IsIndexerStringProperty => IndexerTypeNameObject.IsString;
    public bool IsIndexerBooleanProperty => IndexerTypeNameObject.IsBoolean;
    public bool IsEnum => _flags.IsFlagSet(BoundAttributeFlags.IsEnum);
    public bool IsStringProperty => TypeNameObject.IsString;
    public bool IsBooleanProperty => TypeNameObject.IsBoolean;
    internal bool IsEditorRequired => _flags.IsFlagSet(BoundAttributeFlags.IsEditorRequired);
    public bool IsDirectiveAttribute => _flags.IsFlagSet(BoundAttributeFlags.IsDirectiveAttribute);
    public bool IsWeaklyTyped => _flags.IsFlagSet(BoundAttributeFlags.IsWeaklyTyped);

    public ImmutableArray<BoundAttributeParameterDescriptor> Parameters { get; }
    public MetadataObject Metadata { get; }

    internal BoundAttributeDescriptor(
        BoundAttributeFlags flags,
        string name,
        string propertyName,
        TypeNameObject typeNameObject,
        string? indexerNamePrefix,
        TypeNameObject indexerTypeNameObject,
        DocumentationObject documentationObject,
        string displayName,
        string? containingType,
        ImmutableArray<BoundAttributeParameterDescriptor> parameters,
        MetadataObject metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        _flags = flags;

        Name = name;
        PropertyName = propertyName;
        TypeNameObject = typeNameObject;
        IndexerNamePrefix = indexerNamePrefix;
        IndexerTypeNameObject = indexerTypeNameObject;
        DocumentationObject = documentationObject;
        DisplayName = displayName;
        ContainingType = containingType;
        Parameters = parameters.NullToEmpty();
        Metadata = metadata;

        foreach (var parameter in Parameters)
        {
            parameter.SetParent(this);
        }
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append((byte)_flags);
        builder.Append(Name);
        builder.Append(PropertyName);
        builder.Append(IndexerNamePrefix);
        builder.Append(DisplayName);
        builder.Append(ContainingType);

        TypeNameObject.AppendToChecksum(in builder);
        IndexerTypeNameObject.AppendToChecksum(in builder);
        DocumentationObject.AppendToChecksum(in builder);

        foreach (var descriptor in Parameters)
        {
            builder.Append(descriptor.Checksum);
        }

        Metadata.AppendToChecksum(in builder);
    }

    public TagHelperDescriptor Parent
        => _parent ?? ThrowHelper.ThrowInvalidOperationException<TagHelperDescriptor>(Resources.Parent_has_not_been_set);

    internal void SetParent(TagHelperDescriptor parent)
    {
        Debug.Assert(parent != null);
        Debug.Assert(_parent == null);

        _parent = parent;
    }

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        foreach (var parameter in Parameters)
        {
            foreach (var diagnostic in parameter.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in Diagnostics)
        {
            yield return diagnostic;
        }
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString()!;
    }
}
