// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

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

            case Glyph.EnumMemberPublic:
            case Glyph.EnumMemberProtected:
            case Glyph.EnumMemberPrivate:
            case Glyph.EnumMemberInternal:
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

            case Glyph.OperatorPublic:
            case Glyph.OperatorProtected:
            case Glyph.OperatorPrivate:
            case Glyph.OperatorInternal:
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

    public static ImageSource GetImageSource(this Glyph glyph, IGlyphService glyphService)
        => glyphService.GetGlyph(glyph.GetStandardGlyphGroup(), glyph.GetStandardGlyphItem());

    public static ushort GetGlyphIndex(this Glyph glyph)
    {
        var glyphGroup = glyph.GetStandardGlyphGroup();
        var glyphItem = glyph.GetStandardGlyphItem();

        return glyphGroup < StandardGlyphGroup.GlyphGroupError
            ? (ushort)((int)glyphGroup + (int)glyphItem)
            : (ushort)glyphGroup;
    }
}
