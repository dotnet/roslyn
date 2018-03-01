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
                    return new ImageId(KnownMonikers.Assembly.Guid, KnownMonikers.Assembly.Id);

                case Glyph.BasicFile:
                    return new ImageId(KnownMonikers.VBFileNode.Guid, KnownMonikers.VBFileNode.Id);
                case Glyph.BasicProject:
                    return new ImageId(KnownMonikers.VBProjectNode.Guid, KnownMonikers.VBProjectNode.Id);

                case Glyph.ClassPublic:
                    return new ImageId(KnownMonikers.ClassPublic.Guid, KnownMonikers.ClassPublic.Id);
                case Glyph.ClassProtected:
                    return new ImageId(KnownMonikers.ClassProtected.Guid, KnownMonikers.ClassProtected.Id);
                case Glyph.ClassPrivate:
                    return new ImageId(KnownMonikers.ClassPrivate.Guid, KnownMonikers.ClassPrivate.Id);
                case Glyph.ClassInternal:
                    return new ImageId(KnownMonikers.ClassInternal.Guid, KnownMonikers.ClassInternal.Id);

                case Glyph.CSharpFile:
                    return new ImageId(KnownMonikers.CSFileNode.Guid, KnownMonikers.CSFileNode.Id);
                case Glyph.CSharpProject:
                    return new ImageId(KnownMonikers.CSProjectNode.Guid, KnownMonikers.CSProjectNode.Id);

                case Glyph.ConstantPublic:
                    return new ImageId(KnownMonikers.ConstantPublic.Guid, KnownMonikers.ConstantPublic.Id);
                case Glyph.ConstantProtected:
                    return new ImageId(KnownMonikers.ConstantProtected.Guid, KnownMonikers.ConstantProtected.Id);
                case Glyph.ConstantPrivate:
                    return new ImageId(KnownMonikers.ConstantPrivate.Guid, KnownMonikers.ConstantPrivate.Id);
                case Glyph.ConstantInternal:
                    return new ImageId(KnownMonikers.ConstantInternal.Guid, KnownMonikers.ConstantInternal.Id);

                case Glyph.DelegatePublic:
                    return new ImageId(KnownMonikers.DelegatePublic.Guid, KnownMonikers.DelegatePublic.Id);
                case Glyph.DelegateProtected:
                    return new ImageId(KnownMonikers.DelegateProtected.Guid, KnownMonikers.DelegateProtected.Id);
                case Glyph.DelegatePrivate:
                    return new ImageId(KnownMonikers.DelegatePrivate.Guid, KnownMonikers.DelegatePrivate.Id);
                case Glyph.DelegateInternal:
                    return new ImageId(KnownMonikers.DelegateInternal.Guid, KnownMonikers.DelegateInternal.Id);

                case Glyph.EnumPublic:
                    return new ImageId(KnownMonikers.EnumerationPublic.Guid, KnownMonikers.EnumerationPublic.Id);
                case Glyph.EnumProtected:
                    return new ImageId(KnownMonikers.EnumerationProtected.Guid, KnownMonikers.EnumerationProtected.Id);
                case Glyph.EnumPrivate:
                    return new ImageId(KnownMonikers.EnumerationPrivate.Guid, KnownMonikers.EnumerationPrivate.Id);
                case Glyph.EnumInternal:
                    return new ImageId(KnownMonikers.EnumerationInternal.Guid, KnownMonikers.EnumerationInternal.Id);

                case Glyph.EnumMemberPublic:
                case Glyph.EnumMemberProtected:
                case Glyph.EnumMemberPrivate:
                case Glyph.EnumMemberInternal:
                    return new ImageId(KnownMonikers.EnumerationItemPublic.Guid, KnownMonikers.EnumerationItemPublic.Id);

                case Glyph.Error:
                    return new ImageId(KnownMonikers.StatusError.Guid, KnownMonikers.StatusError.Id);

                case Glyph.EventPublic:
                    return new ImageId(KnownMonikers.EventPublic.Guid, KnownMonikers.EventPublic.Id);
                case Glyph.EventProtected:
                    return new ImageId(KnownMonikers.EventProtected.Guid, KnownMonikers.EventProtected.Id);
                case Glyph.EventPrivate:
                    return new ImageId(KnownMonikers.EventPrivate.Guid, KnownMonikers.EventPrivate.Id);
                case Glyph.EventInternal:
                    return new ImageId(KnownMonikers.EventInternal.Guid, KnownMonikers.EventInternal.Id);

                // Extension methods have the same glyph regardless of accessibility.
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return new ImageId(KnownMonikers.ExtensionMethod.Guid, KnownMonikers.ExtensionMethod.Id);

                case Glyph.FieldPublic:
                    return new ImageId(KnownMonikers.FieldPublic.Guid, KnownMonikers.FieldPublic.Id);
                case Glyph.FieldProtected:
                    return new ImageId(KnownMonikers.FieldProtected.Guid, KnownMonikers.FieldProtected.Id);
                case Glyph.FieldPrivate:
                    return new ImageId(KnownMonikers.FieldPrivate.Guid, KnownMonikers.FieldPrivate.Id);
                case Glyph.FieldInternal:
                    return new ImageId(KnownMonikers.FieldInternal.Guid, KnownMonikers.FieldInternal.Id);

                case Glyph.InterfacePublic:
                    return new ImageId(KnownMonikers.InterfacePublic.Guid, KnownMonikers.InterfacePublic.Id);
                case Glyph.InterfaceProtected:
                    return new ImageId(KnownMonikers.InterfaceProtected.Guid, KnownMonikers.InterfaceProtected.Id);
                case Glyph.InterfacePrivate:
                    return new ImageId(KnownMonikers.InterfacePrivate.Guid, KnownMonikers.InterfacePrivate.Id);
                case Glyph.InterfaceInternal:
                    return new ImageId(KnownMonikers.InterfaceInternal.Guid, KnownMonikers.InterfaceInternal.Id);

                // TODO: Figure out the right thing to return here.
                case Glyph.Intrinsic:
                    return new ImageId(KnownMonikers.Type.Guid, KnownMonikers.Type.Id);

                case Glyph.Keyword:
                    return new ImageId(KnownMonikers.IntellisenseKeyword.Guid, KnownMonikers.IntellisenseKeyword.Id);

                case Glyph.Label:
                    return new ImageId(KnownMonikers.Label.Guid, KnownMonikers.Label.Id);

                case Glyph.Parameter:
                case Glyph.Local:
                    return new ImageId(KnownMonikers.LocalVariable.Guid, KnownMonikers.LocalVariable.Id);

                case Glyph.Namespace:
                    return new ImageId(KnownMonikers.Namespace.Guid, KnownMonikers.Namespace.Id);

                case Glyph.MethodPublic:
                    return new ImageId(KnownMonikers.MethodPublic.Guid, KnownMonikers.MethodPublic.Id);
                case Glyph.MethodProtected:
                    return new ImageId(KnownMonikers.MethodProtected.Guid, KnownMonikers.MethodProtected.Id);
                case Glyph.MethodPrivate:
                    return new ImageId(KnownMonikers.MethodPrivate.Guid, KnownMonikers.MethodPrivate.Id);
                case Glyph.MethodInternal:
                    return new ImageId(KnownMonikers.MethodInternal.Guid, KnownMonikers.MethodInternal.Id);

                case Glyph.ModulePublic:
                    return new ImageId(KnownMonikers.ModulePublic.Guid, KnownMonikers.ModulePublic.Id);
                case Glyph.ModuleProtected:
                    return new ImageId(KnownMonikers.ModuleProtected.Guid, KnownMonikers.ModuleProtected.Id);
                case Glyph.ModulePrivate:
                    return new ImageId(KnownMonikers.ModulePrivate.Guid, KnownMonikers.ModulePrivate.Id);
                case Glyph.ModuleInternal:
                    return new ImageId(KnownMonikers.ModuleInternal.Guid, KnownMonikers.ModuleInternal.Id);

                case Glyph.OpenFolder:
                    return new ImageId(KnownMonikers.OpenFolder.Guid, KnownMonikers.OpenFolder.Id);

                case Glyph.Operator:
                    return new ImageId(KnownMonikers.Operator.Guid, KnownMonikers.Operator.Id);

                case Glyph.PropertyPublic:
                    return new ImageId(KnownMonikers.PropertyPublic.Guid, KnownMonikers.PropertyPublic.Id);
                case Glyph.PropertyProtected:
                    return new ImageId(KnownMonikers.PropertyProtected.Guid, KnownMonikers.PropertyProtected.Id);
                case Glyph.PropertyPrivate:
                    return new ImageId(KnownMonikers.PropertyPrivate.Guid, KnownMonikers.PropertyPrivate.Id);
                case Glyph.PropertyInternal:
                    return new ImageId(KnownMonikers.PropertyInternal.Guid, KnownMonikers.PropertyInternal.Id);

                case Glyph.RangeVariable:
                    return new ImageId(KnownMonikers.FieldPublic.Guid, KnownMonikers.FieldPublic.Id);

                case Glyph.Reference:
                    return new ImageId(KnownMonikers.Reference.Guid, KnownMonikers.Reference.Id);

                case Glyph.StructurePublic:
                    return new ImageId(KnownMonikers.ValueTypePublic.Guid, KnownMonikers.ValueTypePublic.Id);
                case Glyph.StructureProtected:
                    return new ImageId(KnownMonikers.ValueTypeProtected.Guid, KnownMonikers.ValueTypeProtected.Id);
                case Glyph.StructurePrivate:
                    return new ImageId(KnownMonikers.ValueTypePrivate.Guid, KnownMonikers.ValueTypePrivate.Id);
                case Glyph.StructureInternal:
                    return new ImageId(KnownMonikers.ValueTypeInternal.Guid, KnownMonikers.ValueTypeInternal.Id);

                case Glyph.TypeParameter:
                    return new ImageId(KnownMonikers.Type.Guid, KnownMonikers.Type.Id);

                case Glyph.Snippet:
                    return new ImageId(KnownMonikers.Snippet.Guid, KnownMonikers.Snippet.Id);

                case Glyph.CompletionWarning:
                    return new ImageId(KnownMonikers.IntellisenseWarning.Guid, KnownMonikers.IntellisenseWarning.Id);

                case Glyph.StatusInformation:
                    return new ImageId(KnownMonikers.StatusInformation.Guid, KnownMonikers.StatusInformation.Id);

                case Glyph.NuGet:
                    return new ImageId(KnownMonikers.NuGet.Guid, KnownMonikers.NuGet.Id);

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
