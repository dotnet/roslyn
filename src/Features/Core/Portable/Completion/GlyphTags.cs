// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class GlyphTags
    {
        public static ImmutableArray<string> GetTags(Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.Assembly:
                    return Assembly;
                case Glyph.BasicFile:
                    return VisualBasicFile;
                case Glyph.BasicProject:
                    return VisualBasicProject;
                case Glyph.ClassPublic:
                    return ClassPublic;
                case Glyph.ClassProtected:
                    return ClassProtected;
                case Glyph.ClassPrivate:
                    return ClassPrivate;
                case Glyph.ClassInternal:
                    return ClassInternal;
                case Glyph.CSharpFile:
                    return CSharpFile;
                case Glyph.CSharpProject:
                    return CSharpProject;
                case Glyph.ConstantPublic:
                    return ConstantPublic;
                case Glyph.ConstantProtected:
                    return ConstantProtected;
                case Glyph.ConstantPrivate:
                    return ConstantPrivate;
                case Glyph.ConstantInternal:
                    return ConstantInternal;
                case Glyph.DelegatePublic:
                    return DelegatePublic;
                case Glyph.DelegateProtected:
                    return DelegateProtected;
                case Glyph.DelegatePrivate:
                    return DelegatePrivate;
                case Glyph.DelegateInternal:
                    return DelegateInternal;
                case Glyph.EnumPublic:
                    return EnumPublic;
                case Glyph.EnumProtected:
                    return EnumProtected;
                case Glyph.EnumPrivate:
                    return EnumPrivate;
                case Glyph.EnumInternal:
                    return EnumInternal;
                case Glyph.EnumMember:
                    return EnumMember;
                case Glyph.Error:
                    return Error;
                case Glyph.EventPublic:
                    return EventPublic;
                case Glyph.EventProtected:
                    return EventProtected;
                case Glyph.EventPrivate:
                    return EventPrivate;
                case Glyph.EventInternal:
                    return EventInternal;
                case Glyph.ExtensionMethodPublic:
                    return ExtensionMethodPublic;
                case Glyph.ExtensionMethodProtected:
                    return ExtensionMethodProtected;
                case Glyph.ExtensionMethodPrivate:
                    return ExtensionMethodPrivate;
                case Glyph.ExtensionMethodInternal:
                    return ExtensionMethodInternal;
                case Glyph.FieldPublic:
                    return FieldPublic;
                case Glyph.FieldProtected:
                    return FieldProtected;
                case Glyph.FieldPrivate:
                    return FieldPrivate;
                case Glyph.FieldInternal:
                    return FieldInternal;
                case Glyph.InterfacePublic:
                    return InterfacePublic;
                case Glyph.InterfaceProtected:
                    return InterfaceProtected;
                case Glyph.InterfacePrivate:
                    return InterfacePrivate;
                case Glyph.InterfaceInternal:
                    return InterfaceInternal;
                case Glyph.Intrinsic:
                    return Intrinsic;
                case Glyph.Keyword:
                    return Keyword;
                case Glyph.Label:
                    return Label;
                case Glyph.Local:
                    return Local;
                case Glyph.Namespace:
                    return Namespace;
                case Glyph.MethodPublic:
                    return MethodPublic;
                case Glyph.MethodProtected:
                    return MethodProtected;
                case Glyph.MethodPrivate:
                    return MethodPrivate;
                case Glyph.MethodInternal:
                    return MethodInternal;
                case Glyph.ModulePublic:
                    return ModulePublic;
                case Glyph.ModuleProtected:
                    return ModuleProtected;
                case Glyph.ModulePrivate:
                    return ModulePrivate;
                case Glyph.ModuleInternal:
                    return ModuleInternal;
                case Glyph.OpenFolder:
                    return Folder;
                case Glyph.Operator:
                    return Operator;
                case Glyph.Parameter:
                    return Parameter;
                case Glyph.PropertyPublic:
                    return PropertyPublic;
                case Glyph.PropertyProtected:
                    return PropertyProtected;
                case Glyph.PropertyPrivate:
                    return PropertyPrivate;
                case Glyph.PropertyInternal:
                    return PropertyInternal;
                case Glyph.RangeVariable:
                    return RangeVariable;
                case Glyph.Reference:
                    return Reference;
                case Glyph.StructurePublic:
                    return StructurePublic;
                case Glyph.StructureProtected:
                    return StructureProtected;
                case Glyph.StructurePrivate:
                    return StructurePrivate;
                case Glyph.StructureInternal:
                    return StructureInternal;
                case Glyph.TypeParameter:
                    return TypeParameter;
                case Glyph.Snippet:
                    return Snippet;
                case Glyph.CompletionWarning:
                    return Warning;
                default:
                    return ImmutableArray<string>.Empty;
            }
        }

        private static readonly ImmutableArray<string> Assembly = ImmutableArray.Create(CompletionTags.Assembly);
        private static readonly ImmutableArray<string> ClassPublic = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Public);
        private static readonly ImmutableArray<string> ClassProtected = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Protected);
        private static readonly ImmutableArray<string> ClassPrivate = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Private);
        private static readonly ImmutableArray<string> ClassInternal = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Internal);
        private static readonly ImmutableArray<string> ConstantPublic = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Public);
        private static readonly ImmutableArray<string> ConstantProtected = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Protected);
        private static readonly ImmutableArray<string> ConstantPrivate = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Private);
        private static readonly ImmutableArray<string> ConstantInternal = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Internal);
        private static readonly ImmutableArray<string> DelegatePublic = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Public);
        private static readonly ImmutableArray<string> DelegateProtected = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Protected);
        private static readonly ImmutableArray<string> DelegatePrivate = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Private);
        private static readonly ImmutableArray<string> DelegateInternal = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Internal);
        private static readonly ImmutableArray<string> EnumPublic = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Public);
        private static readonly ImmutableArray<string> EnumProtected = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Public);
        private static readonly ImmutableArray<string> EnumPrivate = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Private);
        private static readonly ImmutableArray<string> EnumInternal = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Internal);
        private static readonly ImmutableArray<string> EnumMember = ImmutableArray.Create(CompletionTags.EnumMember);
        private static readonly ImmutableArray<string> EventPublic = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Public);
        private static readonly ImmutableArray<string> EventProtected = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Protected);
        private static readonly ImmutableArray<string> EventPrivate = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Private);
        private static readonly ImmutableArray<string> EventInternal = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Internal);
        private static readonly ImmutableArray<string> ExtensionMethodPublic = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Public);
        private static readonly ImmutableArray<string> ExtensionMethodProtected = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Protected);
        private static readonly ImmutableArray<string> ExtensionMethodPrivate = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Private);
        private static readonly ImmutableArray<string> ExtensionMethodInternal = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Internal);
        private static readonly ImmutableArray<string> FieldPublic = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Public);
        private static readonly ImmutableArray<string> FieldProtected = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Protected);
        private static readonly ImmutableArray<string> FieldPrivate = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Private);
        private static readonly ImmutableArray<string> FieldInternal = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Internal);
        private static readonly ImmutableArray<string> InterfacePublic = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Public);
        private static readonly ImmutableArray<string> InterfaceProtected = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Protected);
        private static readonly ImmutableArray<string> InterfacePrivate = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Private);
        private static readonly ImmutableArray<string> InterfaceInternal = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Internal);
        private static readonly ImmutableArray<string> Intrinsic = ImmutableArray.Create(CompletionTags.Intrinsic);
        private static readonly ImmutableArray<string> Keyword = ImmutableArray.Create(CompletionTags.Keyword);
        private static readonly ImmutableArray<string> Label = ImmutableArray.Create(CompletionTags.Label);
        private static readonly ImmutableArray<string> Local = ImmutableArray.Create(CompletionTags.Local);
        private static readonly ImmutableArray<string> Namespace = ImmutableArray.Create(CompletionTags.Namespace);
        private static readonly ImmutableArray<string> MethodPublic = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Public);
        private static readonly ImmutableArray<string> MethodProtected = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Protected);
        private static readonly ImmutableArray<string> MethodPrivate = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Private);
        private static readonly ImmutableArray<string> MethodInternal = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Internal);
        private static readonly ImmutableArray<string> ModulePublic = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Public);
        private static readonly ImmutableArray<string> ModuleProtected = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Protected);
        private static readonly ImmutableArray<string> ModulePrivate = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Private);
        private static readonly ImmutableArray<string> ModuleInternal = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Internal);
        private static readonly ImmutableArray<string> Folder = ImmutableArray.Create(CompletionTags.Folder);
        private static readonly ImmutableArray<string> Operator = ImmutableArray.Create(CompletionTags.Operator);
        private static readonly ImmutableArray<string> Parameter = ImmutableArray.Create(CompletionTags.Parameter);
        private static readonly ImmutableArray<string> PropertyPublic = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Public);
        private static readonly ImmutableArray<string> PropertyProtected = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Protected);
        private static readonly ImmutableArray<string> PropertyPrivate = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Private);
        private static readonly ImmutableArray<string> PropertyInternal = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Internal);
        private static readonly ImmutableArray<string> RangeVariable = ImmutableArray.Create(CompletionTags.RangeVariable);
        private static readonly ImmutableArray<string> Reference = ImmutableArray.Create(CompletionTags.Reference);
        private static readonly ImmutableArray<string> StructurePublic = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Public);
        private static readonly ImmutableArray<string> StructureProtected = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Protected);
        private static readonly ImmutableArray<string> StructurePrivate = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Private);
        private static readonly ImmutableArray<string> StructureInternal = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Internal);
        private static readonly ImmutableArray<string> TypeParameter = ImmutableArray.Create(CompletionTags.TypeParameter);
        private static readonly ImmutableArray<string> Snippet =ImmutableArray.Create(CompletionTags.Snippet);

        private static readonly ImmutableArray<string> Error = ImmutableArray.Create(CompletionTags.Error);
        private static readonly ImmutableArray<string> Warning = ImmutableArray.Create(CompletionTags.Warning);

        private static readonly ImmutableArray<string> CSharpFile = ImmutableArray.Create(CompletionTags.File, LanguageNames.CSharp);
        private static readonly ImmutableArray<string> VisualBasicFile = ImmutableArray.Create(CompletionTags.File, LanguageNames.VisualBasic);

        private static readonly ImmutableArray<string> CSharpProject = ImmutableArray.Create(CompletionTags.Project, LanguageNames.CSharp);
        private static readonly ImmutableArray<string> VisualBasicProject = ImmutableArray.Create(CompletionTags.Project, LanguageNames.VisualBasic);

    }
}