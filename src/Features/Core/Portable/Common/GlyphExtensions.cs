// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis;

internal static partial class GlyphExtensions
{
    public static ImmutableArray<Glyph> GetGlyphs(this ImmutableArray<string> tags)
    {
        var builder = ImmutableArray.CreateBuilder<Glyph>(initialCapacity: tags.Length);

        foreach (var tag in tags)
        {
            var glyph = GetGlyph(tag, tags);
            if (glyph != Glyph.None)
            {
                builder.Add(glyph);
            }
        }

        return builder.ToImmutable();
    }

    public static Glyph GetFirstGlyph(this ImmutableArray<string> tags)
    {
        var glyphs = GetGlyphs(tags);

        return !glyphs.IsEmpty
            ? glyphs[0]
            : Glyph.None;
    }

    private static Glyph GetGlyph(string tag, ImmutableArray<string> allTags)
    {
        switch (tag)
        {
            case WellKnownTags.Assembly:
                return Glyph.Assembly;

            case WellKnownTags.File:
                return allTags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicFile : Glyph.CSharpFile;

            case WellKnownTags.Project:
                return allTags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicProject : Glyph.CSharpProject;

            case WellKnownTags.Class:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.ClassProtected,
                    Accessibility.Private => Glyph.ClassPrivate,
                    Accessibility.Internal => Glyph.ClassInternal,
                    _ => Glyph.ClassPublic,
                };
            case WellKnownTags.Constant:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.ConstantProtected,
                    Accessibility.Private => Glyph.ConstantPrivate,
                    Accessibility.Internal => Glyph.ConstantInternal,
                    _ => Glyph.ConstantPublic,
                };
            case WellKnownTags.Delegate:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.DelegateProtected,
                    Accessibility.Private => Glyph.DelegatePrivate,
                    Accessibility.Internal => Glyph.DelegateInternal,
                    _ => Glyph.DelegatePublic,
                };
            case WellKnownTags.Enum:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.EnumProtected,
                    Accessibility.Private => Glyph.EnumPrivate,
                    Accessibility.Internal => Glyph.EnumInternal,
                    _ => Glyph.EnumPublic,
                };
            case WellKnownTags.EnumMember:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.EnumMemberProtected,
                    Accessibility.Private => Glyph.EnumMemberPrivate,
                    Accessibility.Internal => Glyph.EnumMemberInternal,
                    _ => Glyph.EnumMemberPublic,
                };
            case WellKnownTags.Error:
                return Glyph.Error;

            case WellKnownTags.Event:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.EventProtected,
                    Accessibility.Private => Glyph.EventPrivate,
                    Accessibility.Internal => Glyph.EventInternal,
                    _ => Glyph.EventPublic,
                };
            case WellKnownTags.ExtensionMethod:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.ExtensionMethodProtected,
                    Accessibility.Private => Glyph.ExtensionMethodPrivate,
                    Accessibility.Internal => Glyph.ExtensionMethodInternal,
                    _ => Glyph.ExtensionMethodPublic,
                };
            case WellKnownTags.Field:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.FieldProtected,
                    Accessibility.Private => Glyph.FieldPrivate,
                    Accessibility.Internal => Glyph.FieldInternal,
                    _ => Glyph.FieldPublic,
                };
            case WellKnownTags.Interface:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.InterfaceProtected,
                    Accessibility.Private => Glyph.InterfacePrivate,
                    Accessibility.Internal => Glyph.InterfaceInternal,
                    _ => Glyph.InterfacePublic,
                };
            case WellKnownTags.TargetTypeMatch:
                return Glyph.TargetTypeMatch;

            case WellKnownTags.Intrinsic:
                return Glyph.Intrinsic;

            case WellKnownTags.Keyword:
                return Glyph.Keyword;

            case WellKnownTags.Label:
                return Glyph.Label;

            case WellKnownTags.Local:
                return Glyph.Local;

            case WellKnownTags.Namespace:
                return Glyph.Namespace;

            case WellKnownTags.Method:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.MethodProtected,
                    Accessibility.Private => Glyph.MethodPrivate,
                    Accessibility.Internal => Glyph.MethodInternal,
                    _ => Glyph.MethodPublic,
                };
            case WellKnownTags.Module:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.ModulePublic,
                    Accessibility.Private => Glyph.ModulePrivate,
                    Accessibility.Internal => Glyph.ModuleInternal,
                    _ => Glyph.ModulePublic,
                };
            case WellKnownTags.Folder:
                return Glyph.OpenFolder;

            case WellKnownTags.Operator:
                return Glyph.Operator;

            case WellKnownTags.Parameter:
                return Glyph.Parameter;

            case WellKnownTags.Property:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.PropertyProtected,
                    Accessibility.Private => Glyph.PropertyPrivate,
                    Accessibility.Internal => Glyph.PropertyInternal,
                    _ => Glyph.PropertyPublic,
                };
            case WellKnownTags.RangeVariable:
                return Glyph.RangeVariable;

            case WellKnownTags.Reference:
                return Glyph.Reference;

            case WellKnownTags.NuGet:
                return Glyph.NuGet;

            case WellKnownTags.Structure:
                return (GetAccessibility(allTags)) switch
                {
                    Accessibility.Protected => Glyph.StructureProtected,
                    Accessibility.Private => Glyph.StructurePrivate,
                    Accessibility.Internal => Glyph.StructureInternal,
                    _ => Glyph.StructurePublic,
                };
            case WellKnownTags.TypeParameter:
                return Glyph.TypeParameter;

            case WellKnownTags.Snippet:
                return Glyph.Snippet;

            case WellKnownTags.Warning:
                return Glyph.CompletionWarning;

            case WellKnownTags.StatusInformation:
                return Glyph.StatusInformation;
        }

        return Glyph.None;
    }

    public static Accessibility GetAccessibility(ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case WellKnownTags.Public:
                    return Accessibility.Public;
                case WellKnownTags.Protected:
                    return Accessibility.Protected;
                case WellKnownTags.Internal:
                    return Accessibility.Internal;
                case WellKnownTags.Private:
                    return Accessibility.Private;
            }
        }

        return Accessibility.NotApplicable;
    }

    public static (Guid guid, int id) GetVsImageData(this Glyph glyph)
    {
        return glyph switch
        {
            Glyph.None => default,

            Glyph.Assembly => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Assembly),

            Glyph.BasicFile => (KnownImageIds.ImageCatalogGuid, KnownImageIds.VBFileNode),
            Glyph.BasicProject => (KnownImageIds.ImageCatalogGuid, KnownImageIds.VBProjectNode),

            Glyph.ClassPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassPublic),
            Glyph.ClassProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassProtected),
            Glyph.ClassPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassPrivate),
            Glyph.ClassInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal),

            Glyph.CSharpFile => (KnownImageIds.ImageCatalogGuid, KnownImageIds.CSFileNode),
            Glyph.CSharpProject => (KnownImageIds.ImageCatalogGuid, KnownImageIds.CSProjectNode),

            Glyph.CompletionWarning => (KnownImageIds.ImageCatalogGuid, KnownImageIds.IntellisenseWarning),

            Glyph.ConstantPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantPublic),
            Glyph.ConstantProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantProtected),
            Glyph.ConstantPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantPrivate),
            Glyph.ConstantInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantInternal),

            Glyph.DelegatePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePublic),
            Glyph.DelegateProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegateProtected),
            Glyph.DelegatePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePrivate),
            Glyph.DelegateInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegateInternal),

            Glyph.EnumPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPublic),
            Glyph.EnumProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationProtected),
            Glyph.EnumPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPrivate),
            Glyph.EnumInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationInternal),

            Glyph.EnumMemberPublic or
            Glyph.EnumMemberProtected or
            Glyph.EnumMemberPrivate or
            Glyph.EnumMemberInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationItemPublic),

            Glyph.Error => (KnownImageIds.ImageCatalogGuid, KnownImageIds.StatusError),

            Glyph.EventPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPublic),
            Glyph.EventProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventProtected),
            Glyph.EventPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPrivate),
            Glyph.EventInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.EventInternal),

            // Extension methods have the same glyph regardless of accessibility.
            Glyph.ExtensionMethodPublic or
            Glyph.ExtensionMethodProtected or
            Glyph.ExtensionMethodPrivate or
            Glyph.ExtensionMethodInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ExtensionMethod),

            Glyph.FieldPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPublic),
            Glyph.FieldProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldProtected),
            Glyph.FieldPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPrivate),
            Glyph.FieldInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldInternal),

            Glyph.InterfacePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfacePublic),
            Glyph.InterfaceProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfaceProtected),
            Glyph.InterfacePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfacePrivate),
            Glyph.InterfaceInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfaceInternal),

            // TODO: Figure out the right thing to return here.
            Glyph.Intrinsic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Type),

            Glyph.Keyword => (KnownImageIds.ImageCatalogGuid, KnownImageIds.IntellisenseKeyword),

            Glyph.Label => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Label),

            Glyph.MethodPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic),
            Glyph.MethodProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodProtected),
            Glyph.MethodPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate),
            Glyph.MethodInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodInternal),

            Glyph.ModulePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModulePublic),
            Glyph.ModuleProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModuleProtected),
            Glyph.ModulePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModulePrivate),
            Glyph.ModuleInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ModuleInternal),

            Glyph.Namespace => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Namespace),

            Glyph.NuGet => (KnownImageIds.ImageCatalogGuid, KnownImageIds.NuGet),

            Glyph.OpenFolder => (KnownImageIds.ImageCatalogGuid, KnownImageIds.OpenFolder),

            Glyph.Operator => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Operator),

            Glyph.Parameter or Glyph.Local => (KnownImageIds.ImageCatalogGuid, KnownImageIds.LocalVariable),

            Glyph.PropertyPublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPublic),
            Glyph.PropertyProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyProtected),
            Glyph.PropertyPrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPrivate),
            Glyph.PropertyInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyInternal),

            Glyph.RangeVariable => (KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPublic),

            Glyph.Reference => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Reference),

            Glyph.Snippet => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Snippet),

            Glyph.StatusInformation => (KnownImageIds.ImageCatalogGuid, KnownImageIds.StatusInformation),

            Glyph.StructurePublic => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypePublic),
            Glyph.StructureProtected => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypeProtected),
            Glyph.StructurePrivate => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypePrivate),
            Glyph.StructureInternal => (KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypeInternal),

            Glyph.TargetTypeMatch => (KnownImageIds.ImageCatalogGuid, KnownImageIds.MatchType),

            Glyph.TypeParameter => (KnownImageIds.ImageCatalogGuid, KnownImageIds.Type),

            _ => throw new ArgumentException($"Unknown glyph value: {glyph}", nameof(glyph)),
        };
    }
}
