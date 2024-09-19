// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class GlyphExtensions
{
    // hardcode ImageCatalogGuid locally rather than calling KnownImageIds.ImageCatalogGuid
    // So it does not have dependency for Microsoft.VisualStudio.ImageCatalog.dll
    // https://github.com/dotnet/roslyn/issues/26642
    private static readonly Guid ImageCatalogGuid = Guid.Parse("ae27a6b0-e345-4288-96df-5eaf394ee369");

    public static ImageId GetImageCatalogImageId(int imageId)
        => new(ImageCatalogGuid, imageId);

    public static ImageId GetImageId(this Glyph glyph)
    {
        // VS for mac cannot refer to ImageMoniker
        // so we need to expose ImageId instead of ImageMoniker here
        // and expose ImageMoniker in the EditorFeatures.wpf.dll
        // The use of constants here is okay because the compiler inlines their values, so no runtime reference is needed.
        // There are tests in src\EditorFeatures\Test\AssemblyReferenceTests.cs to ensure we don't regress that.
        switch (glyph)
        {
            case Glyph.None:
                return default;

            case Glyph.Assembly:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Assembly);

            case Glyph.BasicFile:
                return new ImageId(ImageCatalogGuid, KnownImageIds.VBFileNode);
            case Glyph.BasicProject:
                return new ImageId(ImageCatalogGuid, KnownImageIds.VBProjectNode);

            case Glyph.ClassPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ClassPublic);
            case Glyph.ClassProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ClassProtected);
            case Glyph.ClassPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ClassPrivate);
            case Glyph.ClassInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ClassInternal);

            case Glyph.CSharpFile:
                return new ImageId(ImageCatalogGuid, KnownImageIds.CSFileNode);
            case Glyph.CSharpProject:
                return new ImageId(ImageCatalogGuid, KnownImageIds.CSProjectNode);

            case Glyph.ConstantPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ConstantPublic);
            case Glyph.ConstantProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ConstantProtected);
            case Glyph.ConstantPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ConstantPrivate);
            case Glyph.ConstantInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ConstantInternal);

            case Glyph.DelegatePublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.DelegatePublic);
            case Glyph.DelegateProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.DelegateProtected);
            case Glyph.DelegatePrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.DelegatePrivate);
            case Glyph.DelegateInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.DelegateInternal);

            case Glyph.EnumPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EnumerationPublic);
            case Glyph.EnumProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EnumerationProtected);
            case Glyph.EnumPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EnumerationPrivate);
            case Glyph.EnumInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EnumerationInternal);

            case Glyph.EnumMemberPublic:
            case Glyph.EnumMemberProtected:
            case Glyph.EnumMemberPrivate:
            case Glyph.EnumMemberInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EnumerationItemPublic);

            case Glyph.Error:
                return new ImageId(ImageCatalogGuid, KnownImageIds.StatusError);

            case Glyph.EventPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EventPublic);
            case Glyph.EventProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EventProtected);
            case Glyph.EventPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EventPrivate);
            case Glyph.EventInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.EventInternal);

            // Extension methods have the same glyph regardless of accessibility.
            case Glyph.ExtensionMethodPublic:
            case Glyph.ExtensionMethodProtected:
            case Glyph.ExtensionMethodPrivate:
            case Glyph.ExtensionMethodInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ExtensionMethod);

            case Glyph.FieldPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.FieldPublic);
            case Glyph.FieldProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.FieldProtected);
            case Glyph.FieldPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.FieldPrivate);
            case Glyph.FieldInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.FieldInternal);

            case Glyph.InterfacePublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.InterfacePublic);
            case Glyph.InterfaceProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.InterfaceProtected);
            case Glyph.InterfacePrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.InterfacePrivate);
            case Glyph.InterfaceInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.InterfaceInternal);

            // TODO: Figure out the right thing to return here.
            case Glyph.Intrinsic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Type);

            case Glyph.Keyword:
                return new ImageId(ImageCatalogGuid, KnownImageIds.IntellisenseKeyword);

            case Glyph.Label:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Label);

            case Glyph.Parameter:
            case Glyph.Local:
                return new ImageId(ImageCatalogGuid, KnownImageIds.LocalVariable);

            case Glyph.Namespace:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Namespace);

            case Glyph.MethodPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.MethodPublic);
            case Glyph.MethodProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.MethodProtected);
            case Glyph.MethodPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.MethodPrivate);
            case Glyph.MethodInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.MethodInternal);

            case Glyph.ModulePublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ModulePublic);
            case Glyph.ModuleProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ModuleProtected);
            case Glyph.ModulePrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ModulePrivate);
            case Glyph.ModuleInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ModuleInternal);

            case Glyph.OpenFolder:
                return new ImageId(ImageCatalogGuid, KnownImageIds.OpenFolder);

            case Glyph.Operator:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Operator);

            case Glyph.PropertyPublic:
                return new ImageId(ImageCatalogGuid, KnownImageIds.PropertyPublic);
            case Glyph.PropertyProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.PropertyProtected);
            case Glyph.PropertyPrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.PropertyPrivate);
            case Glyph.PropertyInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.PropertyInternal);

            case Glyph.RangeVariable:
                return new ImageId(ImageCatalogGuid, KnownImageIds.FieldPublic);

            case Glyph.Reference:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Reference);

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
                return new ImageId(ImageCatalogGuid, KnownImageIds.ValueTypePublic);
            case Glyph.StructureProtected:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ValueTypeProtected);
            case Glyph.StructurePrivate:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ValueTypePrivate);
            case Glyph.StructureInternal:
                return new ImageId(ImageCatalogGuid, KnownImageIds.ValueTypeInternal);

            case Glyph.TypeParameter:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Type);

            case Glyph.Snippet:
                return new ImageId(ImageCatalogGuid, KnownImageIds.Snippet);

            case Glyph.CompletionWarning:
                return new ImageId(ImageCatalogGuid, KnownImageIds.IntellisenseWarning);

            case Glyph.StatusInformation:
                return new ImageId(ImageCatalogGuid, KnownImageIds.StatusInformation);

            case Glyph.NuGet:
                return new ImageId(ImageCatalogGuid, KnownImageIds.NuGet);

            case Glyph.TargetTypeMatch:
                return new ImageId(ImageCatalogGuid, KnownImageIds.MatchType);

            default:
                throw new ArgumentException(nameof(glyph));
        }
    }

    public static ImageElement GetImageElement(this Glyph glyph)
        => new ImageElement(glyph.GetImageId());
}
