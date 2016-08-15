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
                    return s_assembly;
                case Glyph.BasicFile:
                    return s_visualBasicFile;
                case Glyph.BasicProject:
                    return s_visualBasicProject;
                case Glyph.ClassPublic:
                    return s_classPublic;
                case Glyph.ClassProtected:
                    return s_classProtected;
                case Glyph.ClassPrivate:
                    return s_classPrivate;
                case Glyph.ClassInternal:
                    return s_classInternal;
                case Glyph.CSharpFile:
                    return s_CSharpFile;
                case Glyph.CSharpProject:
                    return s_CSharpProject;
                case Glyph.ConstantPublic:
                    return s_constantPublic;
                case Glyph.ConstantProtected:
                    return s_constantProtected;
                case Glyph.ConstantPrivate:
                    return s_constantPrivate;
                case Glyph.ConstantInternal:
                    return s_constantInternal;
                case Glyph.DelegatePublic:
                    return s_delegatePublic;
                case Glyph.DelegateProtected:
                    return s_delegateProtected;
                case Glyph.DelegatePrivate:
                    return s_delegatePrivate;
                case Glyph.DelegateInternal:
                    return s_delegateInternal;
                case Glyph.EnumPublic:
                    return s_enumPublic;
                case Glyph.EnumProtected:
                    return s_enumProtected;
                case Glyph.EnumPrivate:
                    return s_enumPrivate;
                case Glyph.EnumInternal:
                    return s_enumInternal;
                case Glyph.EnumMember:
                    return s_enumMember;
                case Glyph.Error:
                    return s_error;
                case Glyph.EventPublic:
                    return s_eventPublic;
                case Glyph.EventProtected:
                    return s_eventProtected;
                case Glyph.EventPrivate:
                    return s_eventPrivate;
                case Glyph.EventInternal:
                    return s_eventInternal;
                case Glyph.ExtensionMethodPublic:
                    return s_extensionMethodPublic;
                case Glyph.ExtensionMethodProtected:
                    return s_extensionMethodProtected;
                case Glyph.ExtensionMethodPrivate:
                    return s_extensionMethodPrivate;
                case Glyph.ExtensionMethodInternal:
                    return s_extensionMethodInternal;
                case Glyph.FieldPublic:
                    return s_fieldPublic;
                case Glyph.FieldProtected:
                    return s_fieldProtected;
                case Glyph.FieldPrivate:
                    return s_fieldPrivate;
                case Glyph.FieldInternal:
                    return s_fieldInternal;
                case Glyph.InterfacePublic:
                    return s_interfacePublic;
                case Glyph.InterfaceProtected:
                    return s_interfaceProtected;
                case Glyph.InterfacePrivate:
                    return s_interfacePrivate;
                case Glyph.InterfaceInternal:
                    return s_interfaceInternal;
                case Glyph.Intrinsic:
                    return s_intrinsic;
                case Glyph.Keyword:
                    return s_keyword;
                case Glyph.Label:
                    return s_label;
                case Glyph.Local:
                    return s_local;
                case Glyph.Namespace:
                    return s_namespace;
                case Glyph.MethodPublic:
                    return s_methodPublic;
                case Glyph.MethodProtected:
                    return s_methodProtected;
                case Glyph.MethodPrivate:
                    return s_methodPrivate;
                case Glyph.MethodInternal:
                    return s_methodInternal;
                case Glyph.ModulePublic:
                    return s_modulePublic;
                case Glyph.ModuleProtected:
                    return s_moduleProtected;
                case Glyph.ModulePrivate:
                    return s_modulePrivate;
                case Glyph.ModuleInternal:
                    return s_moduleInternal;
                case Glyph.OpenFolder:
                    return s_folder;
                case Glyph.Operator:
                    return s_operator;
                case Glyph.Parameter:
                    return s_parameter;
                case Glyph.PropertyPublic:
                    return s_propertyPublic;
                case Glyph.PropertyProtected:
                    return s_propertyProtected;
                case Glyph.PropertyPrivate:
                    return s_propertyPrivate;
                case Glyph.PropertyInternal:
                    return s_propertyInternal;
                case Glyph.RangeVariable:
                    return s_rangeVariable;
                case Glyph.Reference:
                    return s_reference;
                case Glyph.StructurePublic:
                    return s_structurePublic;
                case Glyph.StructureProtected:
                    return s_structureProtected;
                case Glyph.StructurePrivate:
                    return s_structurePrivate;
                case Glyph.StructureInternal:
                    return s_structureInternal;
                case Glyph.TypeParameter:
                    return s_typeParameter;
                case Glyph.Snippet:
                    return s_snippet;
                case Glyph.CompletionWarning:
                    return s_warning;
                case Glyph.StatusInformation:
                    return s_statusInformation;
                default:
                    return ImmutableArray<string>.Empty;
            }
        }

        private static readonly ImmutableArray<string> s_assembly = ImmutableArray.Create(CompletionTags.Assembly);
        private static readonly ImmutableArray<string> s_classPublic = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_classProtected = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_classPrivate = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_classInternal = ImmutableArray.Create(CompletionTags.Class, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_constantPublic = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_constantProtected = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_constantPrivate = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_constantInternal = ImmutableArray.Create(CompletionTags.Constant, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_delegatePublic = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_delegateProtected = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_delegatePrivate = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_delegateInternal = ImmutableArray.Create(CompletionTags.Delegate, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_enumPublic = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_enumProtected = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_enumPrivate = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_enumInternal = ImmutableArray.Create(CompletionTags.Enum, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_enumMember = ImmutableArray.Create(CompletionTags.EnumMember);
        private static readonly ImmutableArray<string> s_eventPublic = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_eventProtected = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_eventPrivate = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_eventInternal = ImmutableArray.Create(CompletionTags.Event, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_extensionMethodPublic = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_extensionMethodProtected = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_extensionMethodPrivate = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_extensionMethodInternal = ImmutableArray.Create(CompletionTags.ExtensionMethod, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_fieldPublic = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_fieldProtected = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_fieldPrivate = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_fieldInternal = ImmutableArray.Create(CompletionTags.Field, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_interfacePublic = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_interfaceProtected = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_interfacePrivate = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_interfaceInternal = ImmutableArray.Create(CompletionTags.Interface, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_intrinsic = ImmutableArray.Create(CompletionTags.Intrinsic);
        private static readonly ImmutableArray<string> s_keyword = ImmutableArray.Create(CompletionTags.Keyword);
        private static readonly ImmutableArray<string> s_label = ImmutableArray.Create(CompletionTags.Label);
        private static readonly ImmutableArray<string> s_local = ImmutableArray.Create(CompletionTags.Local);
        private static readonly ImmutableArray<string> s_namespace = ImmutableArray.Create(CompletionTags.Namespace);
        private static readonly ImmutableArray<string> s_methodPublic = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_methodProtected = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_methodPrivate = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_methodInternal = ImmutableArray.Create(CompletionTags.Method, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_modulePublic = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_moduleProtected = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_modulePrivate = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_moduleInternal = ImmutableArray.Create(CompletionTags.Module, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_folder = ImmutableArray.Create(CompletionTags.Folder);
        private static readonly ImmutableArray<string> s_operator = ImmutableArray.Create(CompletionTags.Operator);
        private static readonly ImmutableArray<string> s_parameter = ImmutableArray.Create(CompletionTags.Parameter);
        private static readonly ImmutableArray<string> s_propertyPublic = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_propertyProtected = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_propertyPrivate = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_propertyInternal = ImmutableArray.Create(CompletionTags.Property, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_rangeVariable = ImmutableArray.Create(CompletionTags.RangeVariable);
        private static readonly ImmutableArray<string> s_reference = ImmutableArray.Create(CompletionTags.Reference);
        private static readonly ImmutableArray<string> s_structurePublic = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Public);
        private static readonly ImmutableArray<string> s_structureProtected = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Protected);
        private static readonly ImmutableArray<string> s_structurePrivate = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Private);
        private static readonly ImmutableArray<string> s_structureInternal = ImmutableArray.Create(CompletionTags.Structure, CompletionTags.Internal);
        private static readonly ImmutableArray<string> s_typeParameter = ImmutableArray.Create(CompletionTags.TypeParameter);
        private static readonly ImmutableArray<string> s_snippet = ImmutableArray.Create(CompletionTags.Snippet);

        private static readonly ImmutableArray<string> s_error = ImmutableArray.Create(CompletionTags.Error);
        private static readonly ImmutableArray<string> s_warning = ImmutableArray.Create(CompletionTags.Warning);
        private static readonly ImmutableArray<string> s_statusInformation = ImmutableArray.Create(CompletionTags.StatusInformation);

        private static readonly ImmutableArray<string> s_CSharpFile = ImmutableArray.Create(CompletionTags.File, LanguageNames.CSharp);
        private static readonly ImmutableArray<string> s_visualBasicFile = ImmutableArray.Create(CompletionTags.File, LanguageNames.VisualBasic);

        private static readonly ImmutableArray<string> s_CSharpProject = ImmutableArray.Create(CompletionTags.Project, LanguageNames.CSharp);
        private static readonly ImmutableArray<string> s_visualBasicProject = ImmutableArray.Create(CompletionTags.Project, LanguageNames.VisualBasic);
    }
}