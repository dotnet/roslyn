// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                    return new ImageId(KnownMonikers.LocalVariable.Guid, KnownMonikers.LocalVariable.Id);

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
    }
}
