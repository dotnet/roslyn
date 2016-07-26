// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class GlyphExtensions
    {
        public static StandardGlyphGroup GetStandardGlyphGroup(this Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.Assembly:
                    return StandardGlyphGroup.GlyphAssembly;

                case Glyph.BasicFile:
                case Glyph.BasicProject:
                    return StandardGlyphGroup.GlyphVBProject;

                case Glyph.ClassPublic:
                case Glyph.ClassProtected:
                case Glyph.ClassPrivate:
                case Glyph.ClassInternal:
                    return StandardGlyphGroup.GlyphGroupClass;

                case Glyph.ConstantPublic:
                case Glyph.ConstantProtected:
                case Glyph.ConstantPrivate:
                case Glyph.ConstantInternal:
                    return StandardGlyphGroup.GlyphGroupConstant;

                case Glyph.CSharpFile:
                    return StandardGlyphGroup.GlyphCSharpFile;

                case Glyph.CSharpProject:
                    return StandardGlyphGroup.GlyphCoolProject;

                case Glyph.DelegatePublic:
                case Glyph.DelegateProtected:
                case Glyph.DelegatePrivate:
                case Glyph.DelegateInternal:
                    return StandardGlyphGroup.GlyphGroupDelegate;

                case Glyph.EnumPublic:
                case Glyph.EnumProtected:
                case Glyph.EnumPrivate:
                case Glyph.EnumInternal:
                    return StandardGlyphGroup.GlyphGroupEnum;

                case Glyph.EnumMember:
                    return StandardGlyphGroup.GlyphGroupEnumMember;

                case Glyph.Error:
                    return StandardGlyphGroup.GlyphGroupError;

                case Glyph.ExtensionMethodPublic:
                    return StandardGlyphGroup.GlyphExtensionMethod;

                case Glyph.ExtensionMethodProtected:
                    return StandardGlyphGroup.GlyphExtensionMethodProtected;

                case Glyph.ExtensionMethodPrivate:
                    return StandardGlyphGroup.GlyphExtensionMethodPrivate;

                case Glyph.ExtensionMethodInternal:
                    return StandardGlyphGroup.GlyphExtensionMethodInternal;

                case Glyph.EventPublic:
                case Glyph.EventProtected:
                case Glyph.EventPrivate:
                case Glyph.EventInternal:
                    return StandardGlyphGroup.GlyphGroupEvent;

                case Glyph.FieldPublic:
                case Glyph.FieldProtected:
                case Glyph.FieldPrivate:
                case Glyph.FieldInternal:
                    return StandardGlyphGroup.GlyphGroupField;

                case Glyph.InterfacePublic:
                case Glyph.InterfaceProtected:
                case Glyph.InterfacePrivate:
                case Glyph.InterfaceInternal:
                    return StandardGlyphGroup.GlyphGroupInterface;

                case Glyph.Intrinsic:
                    return StandardGlyphGroup.GlyphGroupIntrinsic;

                case Glyph.Keyword:
                    return StandardGlyphGroup.GlyphKeyword;

                case Glyph.Label:
                    return StandardGlyphGroup.GlyphGroupIntrinsic;

                case Glyph.Local:
                    return StandardGlyphGroup.GlyphGroupVariable;

                case Glyph.Namespace:
                    return StandardGlyphGroup.GlyphGroupNamespace;

                case Glyph.MethodPublic:
                case Glyph.MethodProtected:
                case Glyph.MethodPrivate:
                case Glyph.MethodInternal:
                    return StandardGlyphGroup.GlyphGroupMethod;

                case Glyph.ModulePublic:
                case Glyph.ModuleProtected:
                case Glyph.ModulePrivate:
                case Glyph.ModuleInternal:
                    return StandardGlyphGroup.GlyphGroupModule;

                case Glyph.OpenFolder:
                    return StandardGlyphGroup.GlyphOpenFolder;

                case Glyph.Operator:
                    return StandardGlyphGroup.GlyphGroupOperator;

                case Glyph.Parameter:
                    return StandardGlyphGroup.GlyphGroupVariable;

                case Glyph.PropertyPublic:
                case Glyph.PropertyProtected:
                case Glyph.PropertyPrivate:
                case Glyph.PropertyInternal:
                    return StandardGlyphGroup.GlyphGroupProperty;

                case Glyph.RangeVariable:
                    return StandardGlyphGroup.GlyphGroupVariable;

                case Glyph.Reference:
                    return StandardGlyphGroup.GlyphReference;

                case Glyph.StructurePublic:
                case Glyph.StructureProtected:
                case Glyph.StructurePrivate:
                case Glyph.StructureInternal:
                    return StandardGlyphGroup.GlyphGroupStruct;

                case Glyph.TypeParameter:
                    return StandardGlyphGroup.GlyphGroupType;

                case Glyph.Snippet:
                    return StandardGlyphGroup.GlyphCSharpExpansion;

                case Glyph.CompletionWarning:
                    return StandardGlyphGroup.GlyphCompletionWarning;

                default:
                    throw new ArgumentException("glyph");
            }
        }

        public static StandardGlyphItem GetStandardGlyphItem(this Glyph icon)
        {
            switch (icon)
            {
                case Glyph.ClassProtected:
                case Glyph.ConstantProtected:
                case Glyph.DelegateProtected:
                case Glyph.EnumProtected:
                case Glyph.EventProtected:
                case Glyph.FieldProtected:
                case Glyph.InterfaceProtected:
                case Glyph.MethodProtected:
                case Glyph.ModuleProtected:
                case Glyph.PropertyProtected:
                case Glyph.StructureProtected:
                    return StandardGlyphItem.GlyphItemProtected;

                case Glyph.ClassPrivate:
                case Glyph.ConstantPrivate:
                case Glyph.DelegatePrivate:
                case Glyph.EnumPrivate:
                case Glyph.EventPrivate:
                case Glyph.FieldPrivate:
                case Glyph.InterfacePrivate:
                case Glyph.MethodPrivate:
                case Glyph.ModulePrivate:
                case Glyph.PropertyPrivate:
                case Glyph.StructurePrivate:
                    return StandardGlyphItem.GlyphItemPrivate;

                case Glyph.ClassInternal:
                case Glyph.ConstantInternal:
                case Glyph.DelegateInternal:
                case Glyph.EnumInternal:
                case Glyph.EventInternal:
                case Glyph.FieldInternal:
                case Glyph.InterfaceInternal:
                case Glyph.MethodInternal:
                case Glyph.ModuleInternal:
                case Glyph.PropertyInternal:
                case Glyph.StructureInternal:
                    return StandardGlyphItem.GlyphItemFriend;

                default:
                    // We don't want any overlays
                    return StandardGlyphItem.GlyphItemPublic;
            }
        }

        public static ImageSource GetImageSource(this Glyph? glyph, IGlyphService glyphService)
        {
            return glyph.HasValue ? glyph.Value.GetImageSource(glyphService) : null;
        }

        public static ImageSource GetImageSource(this Glyph glyph, IGlyphService glyphService)
        {
            return glyphService.GetGlyph(glyph.GetStandardGlyphGroup(), glyph.GetStandardGlyphItem());
        }

        public static ImageMoniker GetImageMoniker(this Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.Assembly:
                    return KnownMonikers.Assembly;

                case Glyph.BasicFile:
                    return KnownMonikers.VBFileNode;
                case Glyph.BasicProject:
                    return KnownMonikers.VBProjectNode;

                case Glyph.ClassPublic:
                    return KnownMonikers.ClassPublic;
                case Glyph.ClassProtected:
                    return KnownMonikers.ClassProtected;
                case Glyph.ClassPrivate:
                    return KnownMonikers.ClassPrivate;
                case Glyph.ClassInternal:
                    return KnownMonikers.ClassInternal;

                case Glyph.CSharpFile:
                    return KnownMonikers.CSFileNode;
                case Glyph.CSharpProject:
                    return KnownMonikers.CSProjectNode;

                case Glyph.ConstantPublic:
                    return KnownMonikers.ConstantPublic;
                case Glyph.ConstantProtected:
                    return KnownMonikers.ConstantProtected;
                case Glyph.ConstantPrivate:
                    return KnownMonikers.ConstantPrivate;
                case Glyph.ConstantInternal:
                    return KnownMonikers.ConstantInternal;

                case Glyph.DelegatePublic:
                    return KnownMonikers.DelegatePublic;
                case Glyph.DelegateProtected:
                    return KnownMonikers.DelegateProtected;
                case Glyph.DelegatePrivate:
                    return KnownMonikers.DelegatePrivate;
                case Glyph.DelegateInternal:
                    return KnownMonikers.DelegateInternal;

                case Glyph.EnumPublic:
                    return KnownMonikers.EnumerationPublic;
                case Glyph.EnumProtected:
                    return KnownMonikers.EnumerationProtected;
                case Glyph.EnumPrivate:
                    return KnownMonikers.EnumerationPrivate;
                case Glyph.EnumInternal:
                    return KnownMonikers.EnumerationInternal;

                case Glyph.EnumMember:
                    return KnownMonikers.EnumerationItemPublic;

                case Glyph.Error:
                    return KnownMonikers.StatusError;

                case Glyph.EventPublic:
                    return KnownMonikers.EventPublic;
                case Glyph.EventProtected:
                    return KnownMonikers.EventProtected;
                case Glyph.EventPrivate:
                    return KnownMonikers.EventPrivate;
                case Glyph.EventInternal:
                    return KnownMonikers.EventInternal;

                // Extension methods have the same glyph regardless of accessibility.
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return KnownMonikers.ExtensionMethod;

                case Glyph.FieldPublic:
                    return KnownMonikers.FieldPublic;
                case Glyph.FieldProtected:
                    return KnownMonikers.FieldProtected;
                case Glyph.FieldPrivate:
                    return KnownMonikers.FieldPrivate;
                case Glyph.FieldInternal:
                    return KnownMonikers.FieldInternal;

                case Glyph.InterfacePublic:
                    return KnownMonikers.InterfacePublic;
                case Glyph.InterfaceProtected:
                    return KnownMonikers.InterfaceProtected;
                case Glyph.InterfacePrivate:
                    return KnownMonikers.InterfacePrivate;
                case Glyph.InterfaceInternal:
                    return KnownMonikers.InterfaceInternal;

                // TODO: Figure out the right thing to return here.
                case Glyph.Intrinsic:
                    return KnownMonikers.Type;

                case Glyph.Keyword:
                    return KnownMonikers.IntellisenseKeyword;

                case Glyph.Label:
                    return KnownMonikers.Label;

                case Glyph.Parameter:
                case Glyph.Local:
                    return KnownMonikers.LocalVariable;

                case Glyph.Namespace:
                    return KnownMonikers.Namespace;

                case Glyph.MethodPublic:
                    return KnownMonikers.MethodPublic;
                case Glyph.MethodProtected:
                    return KnownMonikers.MethodProtected;
                case Glyph.MethodPrivate:
                    return KnownMonikers.MethodPrivate;
                case Glyph.MethodInternal:
                    return KnownMonikers.MethodInternal;

                case Glyph.ModulePublic:
                    return KnownMonikers.ModulePublic;
                case Glyph.ModuleProtected:
                    return KnownMonikers.ModuleProtected;
                case Glyph.ModulePrivate:
                    return KnownMonikers.ModulePrivate;
                case Glyph.ModuleInternal:
                    return KnownMonikers.ModuleInternal;

                case Glyph.OpenFolder:
                    return KnownMonikers.OpenFolder;

                case Glyph.Operator:
                    return KnownMonikers.Operator;

                case Glyph.PropertyPublic:
                    return KnownMonikers.PropertyPublic;
                case Glyph.PropertyProtected:
                    return KnownMonikers.PropertyProtected;
                case Glyph.PropertyPrivate:
                    return KnownMonikers.PropertyPrivate;
                case Glyph.PropertyInternal:
                    return KnownMonikers.PropertyInternal;

                case Glyph.RangeVariable:
                    return KnownMonikers.FieldPublic;

                case Glyph.Reference:
                    return KnownMonikers.Reference;

                case Glyph.StructurePublic:
                    return KnownMonikers.ValueTypePublic;
                case Glyph.StructureProtected:
                    return KnownMonikers.ValueTypeProtected;
                case Glyph.StructurePrivate:
                    return KnownMonikers.ValueTypePrivate;
                case Glyph.StructureInternal:
                    return KnownMonikers.ValueTypeInternal;

                case Glyph.TypeParameter:
                    return KnownMonikers.Type;

                case Glyph.Snippet:
                    return KnownMonikers.Snippet;

                case Glyph.CompletionWarning:
                    return KnownMonikers.IntellisenseWarning;

                case Glyph.NuGet:
                    return KnownMonikers.NuGet;

                default:
                    throw new ArgumentException("glyph");
            }
        }

        public static Glyph GetGlyph(this ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                switch (tag)
                {
                    case CompletionTags.Assembly:
                        return Glyph.Assembly;

                    case CompletionTags.File:
                        return tags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicFile : Glyph.CSharpFile;

                    case CompletionTags.Project:
                        return tags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicProject : Glyph.CSharpProject;

                    case CompletionTags.Class:
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

                    case CompletionTags.Constant:
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

                    case CompletionTags.Delegate:
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

                    case CompletionTags.Enum:
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

                    case CompletionTags.EnumMember:
                        switch (GetAccessibility(tags))
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

                    case CompletionTags.ExtensionMethod:
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

                    case CompletionTags.Field:
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

                    case CompletionTags.Interface:
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

                    case CompletionTags.Module:
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

                    case CompletionTags.Folder:
                        return Glyph.OpenFolder;

                    case CompletionTags.Operator:
                        return Glyph.Operator;

                    case CompletionTags.Parameter:
                        return Glyph.Parameter;

                    case CompletionTags.Property:
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

                    case CompletionTags.RangeVariable:
                        return Glyph.RangeVariable;

                    case CompletionTags.Reference:
                        return Glyph.Reference;

                    case CompletionTags.Structure:
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

                    case CompletionTags.TypeParameter:
                        return Glyph.TypeParameter;

                    case CompletionTags.Snippet:
                        return Glyph.Snippet;

                    case CompletionTags.Warning:
                        return Glyph.CompletionWarning;
                }
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