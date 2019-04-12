// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    internal enum Glyph
    {
        None,

        Assembly,

        BasicFile,
        BasicProject,

        ClassPublic,
        ClassProtected,
        ClassPrivate,
        ClassInternal,

        CSharpFile,
        CSharpProject,

        ConstantPublic,
        ConstantProtected,
        ConstantPrivate,
        ConstantInternal,

        DelegatePublic,
        DelegateProtected,
        DelegatePrivate,
        DelegateInternal,

        EnumPublic,
        EnumProtected,
        EnumPrivate,
        EnumInternal,

        EnumMemberPublic,
        EnumMemberProtected,
        EnumMemberPrivate,
        EnumMemberInternal,

        Error,
        StatusInformation,

        EventPublic,
        EventProtected,
        EventPrivate,
        EventInternal,

        ExtensionMethodPublic,
        ExtensionMethodProtected,
        ExtensionMethodPrivate,
        ExtensionMethodInternal,

        FieldPublic,
        FieldProtected,
        FieldPrivate,
        FieldInternal,

        InterfacePublic,
        InterfaceProtected,
        InterfacePrivate,
        InterfaceInternal,

        Intrinsic,

        Keyword,

        Label,

        Local,

        Namespace,

        MethodPublic,
        MethodProtected,
        MethodPrivate,
        MethodInternal,

        ModulePublic,
        ModuleProtected,
        ModulePrivate,
        ModuleInternal,

        OpenFolder,

        Operator,

        Parameter,

        PropertyPublic,
        PropertyProtected,
        PropertyPrivate,
        PropertyInternal,

        RangeVariable,

        Reference,

        StructurePublic,
        StructureProtected,
        StructurePrivate,
        StructureInternal,

        TypeParameter,

        Snippet,

        CompletionWarning,

        AddReference,
        NuGet
    }

    internal static class GlyphHelpers
    {
        public static Glyph Convert(Microsoft.CodeAnalysis.Glyph glyph)
        {
            switch (glyph)
            {
                case Microsoft.CodeAnalysis.Glyph.None:
                    {
                        return Glyph.None;
                    }
                case Microsoft.CodeAnalysis.Glyph.Assembly:
                    {
                        return Glyph.Assembly;
                    }
                case Microsoft.CodeAnalysis.Glyph.BasicFile:
                    {
                        return Glyph.BasicFile;
                    }
                case Microsoft.CodeAnalysis.Glyph.BasicProject:
                    {
                        return Glyph.BasicProject;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassPublic:
                    {
                        return Glyph.ClassPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassProtected:
                    {
                        return Glyph.ClassProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassPrivate:
                    {
                        return Glyph.ClassPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassInternal:
                    {
                        return Glyph.ClassInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.CSharpFile:
                    {
                        return Glyph.CSharpFile;
                    }
                case Microsoft.CodeAnalysis.Glyph.CSharpProject:
                    {
                        return Glyph.CSharpProject;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantPublic:
                    {
                        return Glyph.ConstantPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantProtected:
                    {
                        return Glyph.ConstantProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantPrivate:
                    {
                        return Glyph.ConstantPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantInternal:
                    {
                        return Glyph.ConstantInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegatePublic:
                    {
                        return Glyph.DelegatePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegateProtected:
                    {
                        return Glyph.DelegateProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegatePrivate:
                    {
                        return Glyph.DelegatePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegateInternal:
                    {
                        return Glyph.DelegateInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumPublic:
                    {
                        return Glyph.EnumPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumProtected:
                    {
                        return Glyph.EnumProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumPrivate:
                    {
                        return Glyph.EnumPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumInternal:
                    {
                        return Glyph.EnumInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberPublic:
                    {
                        return Glyph.EnumMemberPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberProtected:
                    {
                        return Glyph.EnumMemberProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberPrivate:
                    {
                        return Glyph.EnumMemberPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberInternal:
                    {
                        return Glyph.EnumMemberInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.Error:
                    {
                        return Glyph.Error;
                    }
                case Microsoft.CodeAnalysis.Glyph.StatusInformation:
                    {
                        return Glyph.StatusInformation;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventPublic:
                    {
                        return Glyph.EventPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventProtected:
                    {
                        return Glyph.EventProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventPrivate:
                    {
                        return Glyph.EventPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventInternal:
                    {
                        return Glyph.EventInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodPublic:
                    {
                        return Glyph.ExtensionMethodPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodProtected:
                    {
                        return Glyph.ExtensionMethodProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodPrivate:
                    {
                        return Glyph.ExtensionMethodPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodInternal:
                    {
                        return Glyph.ExtensionMethodInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldPublic:
                    {
                        return Glyph.FieldPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldProtected:
                    {
                        return Glyph.FieldProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldPrivate:
                    {
                        return Glyph.FieldPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldInternal:
                    {
                        return Glyph.FieldInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfacePublic:
                    {
                        return Glyph.InterfacePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfaceProtected:
                    {
                        return Glyph.InterfaceProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfacePrivate:
                    {
                        return Glyph.InterfacePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfaceInternal:
                    {
                        return Glyph.InterfaceInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.Intrinsic:
                    {
                        return Glyph.Intrinsic;
                    }
                case Microsoft.CodeAnalysis.Glyph.Keyword:
                    {
                        return Glyph.Keyword;
                    }
                case Microsoft.CodeAnalysis.Glyph.Label:
                    {
                        return Glyph.Label;
                    }
                case Microsoft.CodeAnalysis.Glyph.Local:
                    {
                        return Glyph.Local;
                    }
                case Microsoft.CodeAnalysis.Glyph.Namespace:
                    {
                        return Glyph.Namespace;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodPublic:
                    {
                        return Glyph.MethodPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodProtected:
                    {
                        return Glyph.MethodProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodPrivate:
                    {
                        return Glyph.MethodPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodInternal:
                    {
                        return Glyph.MethodInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModulePublic:
                    {
                        return Glyph.ModulePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModuleProtected:
                    {
                        return Glyph.ModuleProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModulePrivate:
                    {
                        return Glyph.ModulePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModuleInternal:
                    {
                        return Glyph.ModuleInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.OpenFolder:
                    {
                        return Glyph.OpenFolder;
                    }
                case Microsoft.CodeAnalysis.Glyph.Operator:
                    {
                        return Glyph.Operator;
                    }
                case Microsoft.CodeAnalysis.Glyph.Parameter:
                    {
                        return Glyph.Parameter;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyPublic:
                    {
                        return Glyph.PropertyPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyProtected:
                    {
                        return Glyph.PropertyProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyPrivate:
                    {
                        return Glyph.PropertyPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyInternal:
                    {
                        return Glyph.PropertyInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.RangeVariable:
                    {
                        return Glyph.RangeVariable;
                    }
                case Microsoft.CodeAnalysis.Glyph.Reference:
                    {
                        return Glyph.Reference;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructurePublic:
                    {
                        return Glyph.StructurePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructureProtected:
                    {
                        return Glyph.StructureProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructurePrivate:
                    {
                        return Glyph.StructurePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructureInternal:
                    {
                        return Glyph.StructureInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.TypeParameter:
                    {
                        return Glyph.TypeParameter;
                    }
                case Microsoft.CodeAnalysis.Glyph.Snippet:
                    {
                        return Glyph.Snippet;
                    }
                case Microsoft.CodeAnalysis.Glyph.CompletionWarning:
                    {
                        return Glyph.CompletionWarning;
                    }
                case Microsoft.CodeAnalysis.Glyph.AddReference:
                    {
                        return Glyph.AddReference;
                    }
                case Microsoft.CodeAnalysis.Glyph.NuGet:
                    {
                        return Glyph.NuGet;
                    }
                default:
                    {
                        return Glyph.None;
                    }
            }
        }
    }

    internal static class GlyphHelpersObsolete
    {
        [Obsolete("Only used to allow IVTs to work temporarily, will be removed when IVTs are fully removed.")]
        public static Microsoft.CodeAnalysis.Glyph Convert(Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.None:
                    {
                        return Microsoft.CodeAnalysis.Glyph.None;
                    }
                case Glyph.Assembly:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Assembly;
                    }
                case Glyph.BasicFile:
                    {
                        return Microsoft.CodeAnalysis.Glyph.BasicFile;
                    }
                case Glyph.BasicProject:
                    {
                        return Microsoft.CodeAnalysis.Glyph.BasicProject;
                    }
                case Glyph.ClassPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassPublic;
                    }
                case Glyph.ClassProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassProtected;
                    }
                case Glyph.ClassPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassPrivate;
                    }
                case Glyph.ClassInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassInternal;
                    }
                case Glyph.CSharpFile:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CSharpFile;
                    }
                case Glyph.CSharpProject:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CSharpProject;
                    }
                case Glyph.ConstantPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantPublic;
                    }
                case Glyph.ConstantProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantProtected;
                    }
                case Glyph.ConstantPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantPrivate;
                    }
                case Glyph.ConstantInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantInternal;
                    }
                case Glyph.DelegatePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegatePublic;
                    }
                case Glyph.DelegateProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegateProtected;
                    }
                case Glyph.DelegatePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegatePrivate;
                    }
                case Glyph.DelegateInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegateInternal;
                    }
                case Glyph.EnumPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumPublic;
                    }
                case Glyph.EnumProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumProtected;
                    }
                case Glyph.EnumPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumPrivate;
                    }
                case Glyph.EnumInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumInternal;
                    }
                case Glyph.EnumMemberPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberPublic;
                    }
                case Glyph.EnumMemberProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberProtected;
                    }
                case Glyph.EnumMemberPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberPrivate;
                    }
                case Glyph.EnumMemberInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberInternal;
                    }
                case Glyph.Error:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Error;
                    }
                case Glyph.StatusInformation:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StatusInformation;
                    }
                case Glyph.EventPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventPublic;
                    }
                case Glyph.EventProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventProtected;
                    }
                case Glyph.EventPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventPrivate;
                    }
                case Glyph.EventInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventInternal;
                    }
                case Glyph.ExtensionMethodPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodPublic;
                    }
                case Glyph.ExtensionMethodProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodProtected;
                    }
                case Glyph.ExtensionMethodPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodPrivate;
                    }
                case Glyph.ExtensionMethodInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodInternal;
                    }
                case Glyph.FieldPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldPublic;
                    }
                case Glyph.FieldProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldProtected;
                    }
                case Glyph.FieldPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldPrivate;
                    }
                case Glyph.FieldInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldInternal;
                    }
                case Glyph.InterfacePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfacePublic;
                    }
                case Glyph.InterfaceProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfaceProtected;
                    }
                case Glyph.InterfacePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfacePrivate;
                    }
                case Glyph.InterfaceInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfaceInternal;
                    }
                case Glyph.Intrinsic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Intrinsic;
                    }
                case Glyph.Keyword:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Keyword;
                    }
                case Glyph.Label:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Label;
                    }
                case Glyph.Local:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Local;
                    }
                case Glyph.Namespace:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Namespace;
                    }
                case Glyph.MethodPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodPublic;
                    }
                case Glyph.MethodProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodProtected;
                    }
                case Glyph.MethodPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodPrivate;
                    }
                case Glyph.MethodInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodInternal;
                    }
                case Glyph.ModulePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModulePublic;
                    }
                case Glyph.ModuleProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModuleProtected;
                    }
                case Glyph.ModulePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModulePrivate;
                    }
                case Glyph.ModuleInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModuleInternal;
                    }
                case Glyph.OpenFolder:
                    {
                        return Microsoft.CodeAnalysis.Glyph.OpenFolder;
                    }
                case Glyph.Operator:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Operator;
                    }
                case Glyph.Parameter:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Parameter;
                    }
                case Glyph.PropertyPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyPublic;
                    }
                case Glyph.PropertyProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyProtected;
                    }
                case Glyph.PropertyPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyPrivate;
                    }
                case Glyph.PropertyInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyInternal;
                    }
                case Glyph.RangeVariable:
                    {
                        return Microsoft.CodeAnalysis.Glyph.RangeVariable;
                    }
                case Glyph.Reference:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Reference;
                    }
                case Glyph.StructurePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructurePublic;
                    }
                case Glyph.StructureProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructureProtected;
                    }
                case Glyph.StructurePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructurePrivate;
                    }
                case Glyph.StructureInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructureInternal;
                    }
                case Glyph.TypeParameter:
                    {
                        return Microsoft.CodeAnalysis.Glyph.TypeParameter;
                    }
                case Glyph.Snippet:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Snippet;
                    }
                case Glyph.CompletionWarning:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CompletionWarning;
                    }
                case Glyph.AddReference:
                    {
                        return Microsoft.CodeAnalysis.Glyph.AddReference;
                    }
                case Glyph.NuGet:
                    {
                        return Microsoft.CodeAnalysis.Glyph.NuGet;
                    }
                default:
                    {
                        return Microsoft.CodeAnalysis.Glyph.None;
                    }
            }
        }
    }
}
