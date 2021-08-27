// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis
{
    internal static class GlyphExtensions
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
    }
}
