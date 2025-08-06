// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Diagnostics;

[DataContract]
internal sealed class DiagnosticDescriptorData(
    string id,
    string title,
    string messageFormat,
    string category,
    DiagnosticSeverity defaultSeverity,
    bool isEnabledByDefault,
    string? description,
    string? helpLinkUri,
    ImmutableArray<string> customTags)
{
    [DataMember(Order = 0)]
    public readonly string Id = id;
    [DataMember(Order = 1)]
    public readonly string Title = title;
    [DataMember(Order = 2)]
    public readonly string MessageFormat = messageFormat;
    [DataMember(Order = 3)]
    public readonly string Category = category;
    [DataMember(Order = 4)]
    public readonly DiagnosticSeverity DefaultSeverity = defaultSeverity;
    [DataMember(Order = 5)]
    public readonly bool IsEnabledByDefault = isEnabledByDefault;
    [DataMember(Order = 6)]
    public readonly string? Description = description;
    [DataMember(Order = 7)]
    public readonly string? HelpLinkUri = helpLinkUri;
    [DataMember(Order = 8)]
    public readonly ImmutableArray<string> CustomTags = customTags;

    public static DiagnosticDescriptorData Create(DiagnosticDescriptor descriptor)
    {
        return new DiagnosticDescriptorData(
            descriptor.Id,
            descriptor.Title.ToString(CultureInfo.CurrentUICulture),
            descriptor.MessageFormat.ToString(CultureInfo.CurrentUICulture),
            descriptor.Category,
            descriptor.DefaultSeverity,
            descriptor.IsEnabledByDefault,
            descriptor.Description?.ToString(CultureInfo.CurrentUICulture),
            descriptor.HelpLinkUri,
            customTags: descriptor.CustomTags.AsImmutableOrEmpty());
    }

    public DiagnosticDescriptor ToDiagnosticDescriptor()
    {
        return new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DefaultSeverity,
            IsEnabledByDefault,
            Description,
            HelpLinkUri,
            ImmutableCollectionsMarshal.AsArray(CustomTags)!);
    }
}
