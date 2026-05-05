// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class TagHelperDescriptor : TagHelperObject<TagHelperDescriptor>
{
    private readonly TagHelperFlags _flags;

    private ImmutableArray<BoundAttributeDescriptor> _editorRequiredAttributes;

    public TagHelperFlags Flags => _flags;
    public TagHelperKind Kind { get; }
    public RuntimeKind RuntimeKind { get; }

    public string Name { get; }
    public string AssemblyName { get; }

    public string TypeName => TypeNameObject.FullName.AssumeNotNull();
    public string? TypeNamespace => TypeNameObject.Namespace;
    public string? TypeNameIdentifier => TypeNameObject.Name;

    public string? Documentation => DocumentationObject.GetText();

    internal TypeNameObject TypeNameObject { get; }
    internal DocumentationObject DocumentationObject { get; }

    public string DisplayName { get; }
    public string? TagOutputHint { get; }

    public bool CaseSensitive => _flags.IsFlagSet(TagHelperFlags.CaseSensitive);

    public ImmutableArray<AllowedChildTagDescriptor> AllowedChildTags { get; }
    public ImmutableArray<BoundAttributeDescriptor> BoundAttributes { get; }
    public ImmutableArray<TagMatchingRuleDescriptor> TagMatchingRules { get; }

    public MetadataObject Metadata { get; }

    /// <summary>
    /// Gets whether the component matches a tag with a fully qualified name.
    /// </summary>
    internal bool IsFullyQualifiedNameMatch => _flags.IsFlagSet(TagHelperFlags.IsFullyQualifiedNameMatch);

    public bool ClassifyAttributesOnly => _flags.IsFlagSet(TagHelperFlags.ClassifyAttributesOnly);

    internal TagHelperDescriptor(
        TagHelperFlags flags,
        TagHelperKind kind,
        RuntimeKind runtimeKind,
        string name,
        string assemblyName,
        string displayName,
        TypeNameObject typeNameObject,
        DocumentationObject documentationObject,
        string? tagOutputHint,
        ImmutableArray<TagMatchingRuleDescriptor> tagMatchingRules,
        ImmutableArray<BoundAttributeDescriptor> attributeDescriptors,
        ImmutableArray<AllowedChildTagDescriptor> allowedChildTags,
        MetadataObject metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        _flags = flags;
        RuntimeKind = runtimeKind;
        Kind = kind;
        Name = name;
        AssemblyName = assemblyName;
        DisplayName = displayName;
        TypeNameObject = typeNameObject;
        DocumentationObject = documentationObject;
        TagOutputHint = tagOutputHint;
        TagMatchingRules = tagMatchingRules.NullToEmpty();
        BoundAttributes = attributeDescriptors.NullToEmpty();
        AllowedChildTags = allowedChildTags.NullToEmpty();
        Metadata = metadata;

        foreach (var tagMatchingRule in TagMatchingRules)
        {
            tagMatchingRule.SetParent(this);
        }

        foreach (var boundAttribute in BoundAttributes)
        {
            boundAttribute.SetParent(this);
        }

        foreach (var allowedChildTag in AllowedChildTags)
        {
            allowedChildTag.SetParent(this);
        }
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append((byte)Flags);
        builder.Append((byte)Kind);
        builder.Append((byte)RuntimeKind);
        builder.Append(Name);
        builder.Append(AssemblyName);
        builder.Append(DisplayName);
        builder.Append(TagOutputHint);

        TypeNameObject.AppendToChecksum(in builder);
        DocumentationObject.AppendToChecksum(in builder);

        foreach (var descriptor in AllowedChildTags)
        {
            builder.Append(descriptor.Checksum);
        }

        foreach (var descriptor in BoundAttributes)
        {
            builder.Append(descriptor.Checksum);
        }

        foreach (var descriptor in TagMatchingRules)
        {
            builder.Append(descriptor.Checksum);
        }

        Metadata.AppendToChecksum(in builder);
    }

    internal ImmutableArray<BoundAttributeDescriptor> EditorRequiredAttributes
    {
        get
        {
            if (_editorRequiredAttributes.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _editorRequiredAttributes, GetEditorRequiredAttributes(BoundAttributes));
            }

            return _editorRequiredAttributes;

            static ImmutableArray<BoundAttributeDescriptor> GetEditorRequiredAttributes(ImmutableArray<BoundAttributeDescriptor> attributes)
            {
                if (attributes.Length == 0)
                {
                    return ImmutableArray<BoundAttributeDescriptor>.Empty;
                }

                using var results = new PooledArrayBuilder<BoundAttributeDescriptor>(capacity: attributes.Length);

                foreach (var attribute in attributes)
                {
                    if (attribute is { IsEditorRequired: true } editorRequiredAttribute)
                    {
                        results.Add(editorRequiredAttribute);
                    }
                }

                return results.ToImmutableAndClear();
            }
        }
    }

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        using var diagnostics = new PooledArrayBuilder<RazorDiagnostic>();

        AppendAllDiagnostics(ref diagnostics.AsRef());

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

    internal void AppendAllDiagnostics(ref PooledArrayBuilder<RazorDiagnostic> diagnostics)
    {
        foreach (var allowedChildTag in AllowedChildTags)
        {
            diagnostics.AddRange(allowedChildTag.Diagnostics);
        }

        foreach (var boundAttribute in BoundAttributes)
        {
            diagnostics.AddRange(boundAttribute.Diagnostics);
        }

        foreach (var tagMatchingRule in TagMatchingRules)
        {
            diagnostics.AddRange(tagMatchingRule.Diagnostics);
        }

        diagnostics.AddRange(Diagnostics);
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString()!;
    }

    private string GetDebuggerDisplay()
    {
        return $"{DisplayName} - {string.Join(" | ", TagMatchingRules.Select(r => r.GetDebuggerDisplay()))}";
    }

    internal TagHelperDescriptor WithName(string name)
    {
        return new(
            Flags, Kind, RuntimeKind, name, AssemblyName, DisplayName,
            TypeNameObject, DocumentationObject, TagOutputHint,
            TagMatchingRules, BoundAttributes, AllowedChildTags,
            Metadata, Diagnostics);
    }
}
