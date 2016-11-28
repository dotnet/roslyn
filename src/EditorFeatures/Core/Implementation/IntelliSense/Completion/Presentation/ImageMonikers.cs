// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                            case Accessibility.Protected:
                                return KnownMonikers.ClassProtected;
                            case Accessibility.Private:
                                return KnownMonikers.ClassPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.ClassInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.ClassPublic;
                        }

                    case CompletionTags.Constant:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.ConstantProtected;
                            case Accessibility.Private:
                                return KnownMonikers.ConstantPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.ConstantInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.ConstantPublic;
                        }

                    case CompletionTags.Delegate:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.DelegateProtected;
                            case Accessibility.Private:
                                return KnownMonikers.DelegatePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.DelegateInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.DelegatePublic;
                        }

                    case CompletionTags.Enum:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.EnumerationProtected;
                            case Accessibility.Private:
                                return KnownMonikers.EnumerationPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.EnumerationInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.EnumerationPublic;
                        }

                    case CompletionTags.EnumMember:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.EnumerationItemProtected;
                            case Accessibility.Private:
                                return KnownMonikers.EnumerationItemPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.EnumerationItemInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.EnumerationItemPublic;
                        }

                    case CompletionTags.Error:
                        return KnownMonikers.StatusError;

                    case CompletionTags.Event:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.EventProtected;
                            case Accessibility.Private:
                                return KnownMonikers.EventPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.EventInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.EventPublic;
                        }

                    case CompletionTags.ExtensionMethod:
                        // Extension methods have the same glyph regardless of accessibility.
                        return KnownMonikers.ExtensionMethod;

                    case CompletionTags.Field:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.FieldProtected;
                            case Accessibility.Private:
                                return KnownMonikers.FieldPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.FieldInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.FieldPublic;
                        }

                    case CompletionTags.Interface:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.InterfaceProtected;
                            case Accessibility.Private:
                                return KnownMonikers.InterfacePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.InterfaceInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.InterfacePublic;
                        }

                    // TODO: Figure out the right thing to return here.
                    case CompletionTags.Intrinsic:
                        return KnownMonikers.Type;

                    case CompletionTags.Keyword:
                        return KnownMonikers.IntellisenseKeyword;

                    case CompletionTags.Label:
                        return KnownMonikers.Label;

                    case CompletionTags.Local:
                        return KnownMonikers.LocalVariable;

                    case CompletionTags.Namespace:
                        return KnownMonikers.Namespace;

                    case CompletionTags.Method:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.MethodProtected;
                            case Accessibility.Private:
                                return KnownMonikers.MethodPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.MethodInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.MethodPublic;
                        }

                    case CompletionTags.Module:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.ModulePublic;
                            case Accessibility.Private:
                                return KnownMonikers.ModulePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.ModuleInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.ModulePublic;
                        }

                    case CompletionTags.Folder:
                        return KnownMonikers.OpenFolder;

                    case CompletionTags.Operator:
                        return KnownMonikers.Operator;

                    case CompletionTags.Parameter:
                        return KnownMonikers.LocalVariable;

                    case CompletionTags.Property:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.PropertyProtected;
                            case Accessibility.Private:
                                return KnownMonikers.PropertyPrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.PropertyInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.PropertyPublic;
                        }

                    case CompletionTags.RangeVariable:
                        return KnownMonikers.LocalVariable;

                    case CompletionTags.Reference:
                        return KnownMonikers.Reference;

                    case CompletionTags.Structure:
                        switch (GetAccessibility(tags))
                        {
                            case Accessibility.Protected:
                                return KnownMonikers.StructureProtected;
                            case Accessibility.Private:
                                return KnownMonikers.StructurePrivate;
                            case Accessibility.Internal:
                                return KnownMonikers.StructureInternal;
                            case Accessibility.Public:
                            default:
                                return KnownMonikers.StructurePublic;
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
