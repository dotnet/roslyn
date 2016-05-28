// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal static class ImageMonikers
    {
        public static ImageMoniker GetImageMoniker(ImmutableArray<string> tags, string language)
        {
            foreach (var tag in tags)
            {
                switch (tag)
                {
                    case CompletionTags.Assembly:
                        return KnownMonikers.Assembly;

                    case CompletionTags.File:
                        return (language == LanguageNames.VisualBasic) ? KnownMonikers.VBFileNode : KnownMonikers.CSFileNode;

                    case CompletionTags.Project:
                        return (language == LanguageNames.VisualBasic) ? KnownMonikers.VBProjectNode : KnownMonikers.CSProjectNode;

                    case CompletionTags.Class:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.ClassPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.ClassProtected;
                            case Accessibility.Private:
                                return KnownMonikers.ClassPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.ClassInternal;
                            default:
                                return KnownMonikers.Class;
                        }

                    case CompletionTags.Constant:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.ConstantPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.ConstantProtected;
                            case Accessibility.Private:
                                return KnownMonikers.ConstantPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.ConstantInternal;
                            default:
                                return KnownMonikers.Constant;
                        }

                    case CompletionTags.Delegate:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.DelegatePublic;
                            case Accessibility.Protected:
                                return KnownMonikers.DelegateProtected;
                            case Accessibility.Private:
                                return KnownMonikers.DelegatePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.DelegateInternal;
                            default:
                                return KnownMonikers.Delegate;
                        }

                    case CompletionTags.Enum:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.EnumerationPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.EnumerationProtected;
                            case Accessibility.Private:
                                return KnownMonikers.EnumerationPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.EnumerationInternal;
                            default:
                                return KnownMonikers.Enumeration;
                        }

                    case CompletionTags.EnumMember:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.EnumerationItemPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.EnumerationItemProtected;
                            case Accessibility.Private:
                                return KnownMonikers.EnumerationItemPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.EnumerationItemInternal;
                            default:
                                return KnownMonikers.EnumerationItemPublic;
                        }

                    case CompletionTags.Error:
                        return KnownMonikers.StatusError;

                    case CompletionTags.Event:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.EventPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.EventProtected;
                            case Accessibility.Private:
                                return KnownMonikers.EventPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.EventInternal;
                            default:
                                return KnownMonikers.Event;
                        }

                    case CompletionTags.ExtensionMethod:
                        // Extension methods have the same glyph regardless of accessibility.
                        return KnownMonikers.ExtensionMethod;

                    case CompletionTags.Field:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.FieldPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.FieldProtected;
                            case Accessibility.Private:
                                return KnownMonikers.FieldPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.FieldInternal;
                            default:
                                return KnownMonikers.Field;
                        }

                    case CompletionTags.Interface:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.InterfacePublic;
                            case Accessibility.Protected:
                                return KnownMonikers.InterfaceProtected;
                            case Accessibility.Private:
                                return KnownMonikers.InterfacePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.InterfaceInternal;
                            default:
                                return KnownMonikers.Interface;
                        }

                    // TODO: Figure out the right thing to return here.
                    case CompletionTags.Intrinsic:
                        return KnownMonikers.Type;

                    case CompletionTags.Keyword:
                        return KnownMonikers.IntellisenseKeyword;

                    case CompletionTags.Label:
                        return KnownMonikers.Label;

                    case CompletionTags.Local:
                        return KnownMonikers.FieldPublic;

                    case CompletionTags.Namespace:
                        return KnownMonikers.Namespace;

                    case CompletionTags.Method:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.MethodPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.MethodProtected;
                            case Accessibility.Private:
                                return KnownMonikers.MethodPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.MethodInternal;
                            default:
                                return KnownMonikers.Method;
                        }

                    case CompletionTags.Module:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.ModulePublic;
                            case Accessibility.Protected:
                                return KnownMonikers.ModulePublic;
                            case Accessibility.Private:
                                return KnownMonikers.ModulePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.ModuleInternal;
                            default:
                                return KnownMonikers.Module;
                        }

                    case CompletionTags.Folder:
                        return KnownMonikers.OpenFolder;

                    case CompletionTags.Operator:
                        return KnownMonikers.Operator;

                    case CompletionTags.Parameter:
                        return KnownMonikers.FieldPublic;

                    case CompletionTags.Property:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.PropertyPublic;
                            case Accessibility.Protected:
                                return KnownMonikers.PropertyProtected;
                            case Accessibility.Private:
                                return KnownMonikers.PropertyPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.PropertyInternal;
                            default:
                                return KnownMonikers.Property;
                        }

                    case CompletionTags.RangeVariable:
                        return KnownMonikers.FieldPublic;

                    case CompletionTags.Reference:
                        return KnownMonikers.Reference;

                    case CompletionTags.Structure:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Public:
                                return KnownMonikers.StructurePublic;
                            case Accessibility.Protected:
                                return KnownMonikers.StructureProtected;
                            case Accessibility.Private:
                                return KnownMonikers.StructurePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.StructureInternal;
                            default:
                                return KnownMonikers.Structure;
                        }

                    case CompletionTags.TypeParameter:
                        return KnownMonikers.Type;

                    case CompletionTags.Snippet:
                        return KnownMonikers.Snippet;

                    case CompletionTags.Warning:
                        return KnownMonikers.IntellisenseWarning;
                }
            }

            return default(ImageMoniker);
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
