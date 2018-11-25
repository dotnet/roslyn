﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class GlyphExtensions
    {
        public static ImageId GetImageId(this Glyph glyph)
        {
            // VS for mac cannot refer to ImageMoniker
            // so we need to expose ImageId instead of ImageMoniker here
            // and expose ImageMoniker in the EditorFeatures.wpf.dll
            switch (glyph)
            {
                case Glyph.None:
                    return default;

                case Glyph.Assembly:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Assembly);

                case Glyph.BasicFile:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.VBFileNode);
                case Glyph.BasicProject:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.VBProjectNode);

                case Glyph.ClassPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassPublic);
                case Glyph.ClassProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassProtected);
                case Glyph.ClassPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassPrivate);
                case Glyph.ClassInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal);

                case Glyph.CSharpFile:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.CSFileNode);
                case Glyph.CSharpProject:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.CSProjectNode);

                case Glyph.ConstantPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantPublic);
                case Glyph.ConstantProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantProtected);
                case Glyph.ConstantPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantPrivate);
                case Glyph.ConstantInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ConstantInternal);

                case Glyph.DelegatePublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePublic);
                case Glyph.DelegateProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegateProtected);
                case Glyph.DelegatePrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePrivate);
                case Glyph.DelegateInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegateInternal);

                case Glyph.EnumPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPublic);
                case Glyph.EnumProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationProtected);
                case Glyph.EnumPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPrivate);
                case Glyph.EnumInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationInternal);

                case Glyph.EnumMemberPublic:
                case Glyph.EnumMemberProtected:
                case Glyph.EnumMemberPrivate:
                case Glyph.EnumMemberInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationItemPublic);

                case Glyph.Error:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.StatusError);

                case Glyph.EventPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPublic);
                case Glyph.EventProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EventProtected);
                case Glyph.EventPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPrivate);
                case Glyph.EventInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EventInternal);

                // Extension methods have the same glyph regardless of accessibility.
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ExtensionMethod);

                case Glyph.FieldPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPublic);
                case Glyph.FieldProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldProtected);
                case Glyph.FieldPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPrivate);
                case Glyph.FieldInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldInternal);

                case Glyph.InterfacePublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfacePublic);
                case Glyph.InterfaceProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfaceProtected);
                case Glyph.InterfacePrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfacePrivate);
                case Glyph.InterfaceInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.InterfaceInternal);

                // TODO: Figure out the right thing to return here.
                case Glyph.Intrinsic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Type);

                case Glyph.Keyword:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.IntellisenseKeyword);

                case Glyph.Label:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Label);

                case Glyph.Parameter:
                case Glyph.Local:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.LocalVariable);

                case Glyph.Namespace:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Namespace);

                case Glyph.MethodPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic);
                case Glyph.MethodProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodProtected);
                case Glyph.MethodPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate);
                case Glyph.MethodInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodInternal);

                case Glyph.ModulePublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ModulePublic);
                case Glyph.ModuleProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ModuleProtected);
                case Glyph.ModulePrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ModulePrivate);
                case Glyph.ModuleInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ModuleInternal);

                case Glyph.OpenFolder:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.OpenFolder);

                case Glyph.Operator:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Operator);

                case Glyph.PropertyPublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPublic);
                case Glyph.PropertyProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyProtected);
                case Glyph.PropertyPrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPrivate);
                case Glyph.PropertyInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyInternal);

                case Glyph.RangeVariable:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.FieldPublic);

                case Glyph.Reference:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Reference);

                //// this is not a copy-paste mistake, we were using these before in the previous GetImageMoniker()
                //case Glyph.StructurePublic:
                //    return KnownMonikers.ValueTypePublic;
                //case Glyph.StructureProtected:
                //    return KnownMonikers.ValueTypeProtected;
                //case Glyph.StructurePrivate:
                //    return KnownMonikers.ValueTypePrivate;
                //case Glyph.StructureInternal:
                //    return KnownMonikers.ValueTypeInternal;

                case Glyph.StructurePublic:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypePublic);
                case Glyph.StructureProtected:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypeProtected);
                case Glyph.StructurePrivate:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypePrivate);
                case Glyph.StructureInternal:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ValueTypeInternal);

                case Glyph.TypeParameter:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Type);

                case Glyph.Snippet:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Snippet);

                case Glyph.CompletionWarning:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.IntellisenseWarning);

                case Glyph.StatusInformation:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.StatusInformation);

                case Glyph.NuGet:
                    return new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.NuGet);

                default:
                    throw new ArgumentException(nameof(glyph));
            }
        }

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
