// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Media;
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
    }
}
