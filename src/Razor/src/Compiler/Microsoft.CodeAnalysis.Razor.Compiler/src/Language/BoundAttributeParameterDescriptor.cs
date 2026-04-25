// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class BoundAttributeParameterDescriptor : TagHelperObject<BoundAttributeParameterDescriptor>
{
    private readonly BoundAttributeParameterFlags _flags;
    private BoundAttributeDescriptor? _parent;
    private string? _displayName;

    public BoundAttributeParameterFlags Flags => _flags;
    public string Name { get; }
    public string PropertyName { get; }
    public string TypeName => TypeNameObject.FullName.AssumeNotNull();
    public string DisplayName => _displayName ??= ":" + Name;

    public string? Documentation => DocumentationObject.GetText();

    internal TypeNameObject TypeNameObject { get; }
    internal DocumentationObject DocumentationObject { get; }

    public bool CaseSensitive => _flags.IsFlagSet(BoundAttributeParameterFlags.CaseSensitive);
    public bool IsEnum => _flags.IsFlagSet(BoundAttributeParameterFlags.IsEnum);
    public bool IsStringProperty => TypeNameObject.IsString;
    public bool IsBooleanProperty => TypeNameObject.IsBoolean;
    public bool BindAttributeGetSet => _flags.IsFlagSet(BoundAttributeParameterFlags.BindAttributeGetSet);

    internal BoundAttributeParameterDescriptor(
        BoundAttributeParameterFlags flags,
        string name,
        string propertyName,
        TypeNameObject typeNameObject,
        DocumentationObject documentationObject,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        _flags = flags;

        Name = name;
        PropertyName = propertyName;
        TypeNameObject = typeNameObject;
        DocumentationObject = documentationObject;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append((byte)_flags);
        builder.Append(Name);
        builder.Append(PropertyName);

        TypeNameObject.AppendToChecksum(in builder);
        DocumentationObject.AppendToChecksum(in builder);
    }

    public BoundAttributeDescriptor Parent
        => _parent ?? ThrowHelper.ThrowInvalidOperationException<BoundAttributeDescriptor>(Resources.Parent_has_not_been_set);

    internal void SetParent(BoundAttributeDescriptor parent)
    {
        Debug.Assert(parent != null);
        Debug.Assert(_parent == null);

        _parent = parent;
    }

    public override string ToString()
        => DisplayName;
}
