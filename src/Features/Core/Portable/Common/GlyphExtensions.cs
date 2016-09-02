// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis
{
    internal static class GlyphExtensions
    {
        public static ImmutableArray<Glyph> GetGlyphs(this ImmutableArray<string> tags)
        {
            return tags.Select(t => GetGlyph(t, tags)).Where(t => t != default(Glyph)).ToImmutableArray();
        }

        public static Glyph GetFirstGlyph(this ImmutableArray<string> tags)
        {
            return tags.Select(t => GetGlyph(t, tags)).FirstOrDefault(t => t != default(Glyph));
        }

        private static Glyph GetGlyph(string tag, ImmutableArray<string> allTags)
        {
            switch (tag)
            {
                case CompletionTags.Assembly:
                    return Glyph.Assembly;

                case CompletionTags.File:
                    return allTags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicFile : Glyph.CSharpFile;

                case CompletionTags.Project:
                    return allTags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicProject : Glyph.CSharpProject;

                case CompletionTags.Class:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.ClassProtected;
                        case Accessibility.Private:
                            return Glyph.ClassPrivate;
                        case Accessibility.Internal:
                            return Glyph.ClassInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.ClassPublic;
                    }

                case CompletionTags.Constant:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.ConstantProtected;
                        case Accessibility.Private:
                            return Glyph.ConstantPrivate;
                        case Accessibility.Internal:
                            return Glyph.ConstantInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.ConstantPublic;
                    }

                case CompletionTags.Delegate:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.DelegateProtected;
                        case Accessibility.Private:
                            return Glyph.DelegatePrivate;
                        case Accessibility.Internal:
                            return Glyph.DelegateInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.DelegatePublic;
                    }

                case CompletionTags.Enum:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.EnumProtected;
                        case Accessibility.Private:
                            return Glyph.EnumPrivate;
                        case Accessibility.Internal:
                            return Glyph.EnumInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.EnumPublic;
                    }

                case CompletionTags.EnumMember:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.EnumMember;
                        case Accessibility.Private:
                            return Glyph.EnumMember;
                        case Accessibility.Internal:
                            return Glyph.EnumMember;
                        case Accessibility.Public:
                        default:
                            return Glyph.EnumMember;
                    }

                case CompletionTags.Error:
                    return Glyph.Error;

                case CompletionTags.Event:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.EventProtected;
                        case Accessibility.Private:
                            return Glyph.EventPrivate;
                        case Accessibility.Internal:
                            return Glyph.EventInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.EventPublic;
                    }

                case CompletionTags.ExtensionMethod:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.ExtensionMethodProtected;
                        case Accessibility.Private:
                            return Glyph.ExtensionMethodPrivate;
                        case Accessibility.Internal:
                            return Glyph.ExtensionMethodInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.ExtensionMethodPublic;
                    }

                case CompletionTags.Field:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.FieldProtected;
                        case Accessibility.Private:
                            return Glyph.FieldPrivate;
                        case Accessibility.Internal:
                            return Glyph.FieldInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.FieldPublic;
                    }

                case CompletionTags.Interface:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.InterfaceProtected;
                        case Accessibility.Private:
                            return Glyph.InterfacePrivate;
                        case Accessibility.Internal:
                            return Glyph.InterfaceInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.InterfacePublic;
                    }

                case CompletionTags.Intrinsic:
                    return Glyph.Intrinsic;

                case CompletionTags.Keyword:
                    return Glyph.Keyword;

                case CompletionTags.Label:
                    return Glyph.Label;

                case CompletionTags.Local:
                    return Glyph.Local;

                case CompletionTags.Namespace:
                    return Glyph.Namespace;

                case CompletionTags.Method:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.MethodProtected;
                        case Accessibility.Private:
                            return Glyph.MethodPrivate;
                        case Accessibility.Internal:
                            return Glyph.MethodInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.MethodPublic;
                    }

                case CompletionTags.Module:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.ModulePublic;
                        case Accessibility.Private:
                            return Glyph.ModulePrivate;
                        case Accessibility.Internal:
                            return Glyph.ModuleInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.ModulePublic;
                    }

                case CompletionTags.Folder:
                    return Glyph.OpenFolder;

                case CompletionTags.Operator:
                    return Glyph.Operator;

                case CompletionTags.Parameter:
                    return Glyph.Parameter;

                case CompletionTags.Property:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.PropertyProtected;
                        case Accessibility.Private:
                            return Glyph.PropertyPrivate;
                        case Accessibility.Internal:
                            return Glyph.PropertyInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.PropertyPublic;
                    }

                case CompletionTags.RangeVariable:
                    return Glyph.RangeVariable;

                case CompletionTags.Reference:
                    return Glyph.Reference;

                case CompletionTags.Structure:
                    switch (GetAccessibility(allTags))
                    {
                        case Accessibility.Protected:
                            return Glyph.StructureProtected;
                        case Accessibility.Private:
                            return Glyph.StructurePrivate;
                        case Accessibility.Internal:
                            return Glyph.StructureInternal;
                        case Accessibility.Public:
                        default:
                            return Glyph.StructurePublic;
                    }

                case CompletionTags.TypeParameter:
                    return Glyph.TypeParameter;

                case CompletionTags.Snippet:
                    return Glyph.Snippet;

                case CompletionTags.Warning:
                    return Glyph.CompletionWarning;
            }

            return default(Glyph);
        }

        private static Accessibility GetAccessibility(ImmutableArray<string> tags)
        {
            if (tags.Contains(CompletionTags.Public))
            {
                return Accessibility.Public;
            }
            else if (tags.Contains(CompletionTags.Protected))
            {
                return Accessibility.Protected;
            }
            else if (tags.Contains(CompletionTags.Internal))
            {
                return Accessibility.Internal;
            }
            else if (tags.Contains(CompletionTags.Private))
            {
                return Accessibility.Private;
            }
            else
            {
                return Accessibility.NotApplicable;
            }
        }
    }
}
