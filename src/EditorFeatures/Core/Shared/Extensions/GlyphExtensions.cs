﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class GlyphExtensions
    {
        public static Glyph GetGlyph(this ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                switch (tag)
                {
                    case WellKnownTags.Assembly:
                        return Glyph.Assembly;

                    case WellKnownTags.File:
                        return tags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicFile : Glyph.CSharpFile;

                    case WellKnownTags.Project:
                        return tags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicProject : Glyph.CSharpProject;

                    case WellKnownTags.Class:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Constant:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Delegate:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Enum:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.EnumMember:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return Glyph.EnumMemberProtected;
                            case Accessibility.Private:
                                return Glyph.EnumMemberPrivate;
                            case Accessibility.Internal:
                                return Glyph.EnumMemberInternal;
                            case Accessibility.Public:
                            default:
                                return Glyph.EnumMemberPublic;
                        }

                    case WellKnownTags.Error:
                        return Glyph.Error;

                    case WellKnownTags.Event:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.ExtensionMethod:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Field:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Interface:
                        switch (GetAccessibility(tags))
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
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Module:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.Folder:
                        return Glyph.OpenFolder;

                    case WellKnownTags.Operator:
                        return Glyph.Operator;

                    case WellKnownTags.Parameter:
                        return Glyph.Parameter;

                    case WellKnownTags.Property:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.RangeVariable:
                        return Glyph.RangeVariable;

                    case WellKnownTags.Reference:
                        return Glyph.Reference;

                    case WellKnownTags.NuGet:
                        return Glyph.NuGet;

                    case WellKnownTags.Structure:
                        switch (GetAccessibility(tags))
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

                    case WellKnownTags.TypeParameter:
                        return Glyph.TypeParameter;

                    case WellKnownTags.Snippet:
                        return Glyph.Snippet;

                    case WellKnownTags.Warning:
                        return Glyph.CompletionWarning;

                case WellKnownTags.StatusInformation:
                    return Glyph.StatusInformation;
                }
            }

            return Glyph.None;
        }

        private static Accessibility GetAccessibility(ImmutableArray<string> tags)
        {
            if (tags.Contains(WellKnownTags.Public))
            {
                return Accessibility.Public;
            }
            else if (tags.Contains(WellKnownTags.Protected))
            {
                return Accessibility.Protected;
            }
            else if (tags.Contains(WellKnownTags.Internal))
            {
                return Accessibility.Internal;
            }
            else if (tags.Contains(WellKnownTags.Private))
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
