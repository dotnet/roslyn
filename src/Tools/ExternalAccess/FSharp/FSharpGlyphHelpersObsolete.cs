// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    internal static class FSharpGlyphHelpersObsolete
    {
        [Obsolete("Only used to allow IVTs to work temporarily, will be removed when IVTs are fully removed.")]
        public static Microsoft.CodeAnalysis.Glyph Convert(FSharpGlyph glyph)
        {
            switch (glyph)
            {
                case FSharpGlyph.None:
                    {
                        return Microsoft.CodeAnalysis.Glyph.None;
                    }
                case FSharpGlyph.Assembly:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Assembly;
                    }
                case FSharpGlyph.BasicFile:
                    {
                        return Microsoft.CodeAnalysis.Glyph.BasicFile;
                    }
                case FSharpGlyph.BasicProject:
                    {
                        return Microsoft.CodeAnalysis.Glyph.BasicProject;
                    }
                case FSharpGlyph.ClassPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassPublic;
                    }
                case FSharpGlyph.ClassProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassProtected;
                    }
                case FSharpGlyph.ClassPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassPrivate;
                    }
                case FSharpGlyph.ClassInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassInternal;
                    }
                case FSharpGlyph.CSharpFile:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CSharpFile;
                    }
                case FSharpGlyph.CSharpProject:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CSharpProject;
                    }
                case FSharpGlyph.ConstantPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantPublic;
                    }
                case FSharpGlyph.ConstantProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantProtected;
                    }
                case FSharpGlyph.ConstantPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantPrivate;
                    }
                case FSharpGlyph.ConstantInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantInternal;
                    }
                case FSharpGlyph.DelegatePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegatePublic;
                    }
                case FSharpGlyph.DelegateProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegateProtected;
                    }
                case FSharpGlyph.DelegatePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegatePrivate;
                    }
                case FSharpGlyph.DelegateInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegateInternal;
                    }
                case FSharpGlyph.EnumPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumPublic;
                    }
                case FSharpGlyph.EnumProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumProtected;
                    }
                case FSharpGlyph.EnumPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumPrivate;
                    }
                case FSharpGlyph.EnumInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumInternal;
                    }
                case FSharpGlyph.EnumMemberPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberPublic;
                    }
                case FSharpGlyph.EnumMemberProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberProtected;
                    }
                case FSharpGlyph.EnumMemberPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberPrivate;
                    }
                case FSharpGlyph.EnumMemberInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberInternal;
                    }
                case FSharpGlyph.Error:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Error;
                    }
                case FSharpGlyph.StatusInformation:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StatusInformation;
                    }
                case FSharpGlyph.EventPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventPublic;
                    }
                case FSharpGlyph.EventProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventProtected;
                    }
                case FSharpGlyph.EventPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventPrivate;
                    }
                case FSharpGlyph.EventInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventInternal;
                    }
                case FSharpGlyph.ExtensionMethodPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodPublic;
                    }
                case FSharpGlyph.ExtensionMethodProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodProtected;
                    }
                case FSharpGlyph.ExtensionMethodPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodPrivate;
                    }
                case FSharpGlyph.ExtensionMethodInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodInternal;
                    }
                case FSharpGlyph.FieldPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldPublic;
                    }
                case FSharpGlyph.FieldProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldProtected;
                    }
                case FSharpGlyph.FieldPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldPrivate;
                    }
                case FSharpGlyph.FieldInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldInternal;
                    }
                case FSharpGlyph.InterfacePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfacePublic;
                    }
                case FSharpGlyph.InterfaceProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfaceProtected;
                    }
                case FSharpGlyph.InterfacePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfacePrivate;
                    }
                case FSharpGlyph.InterfaceInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfaceInternal;
                    }
                case FSharpGlyph.Intrinsic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Intrinsic;
                    }
                case FSharpGlyph.Keyword:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Keyword;
                    }
                case FSharpGlyph.Label:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Label;
                    }
                case FSharpGlyph.Local:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Local;
                    }
                case FSharpGlyph.Namespace:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Namespace;
                    }
                case FSharpGlyph.MethodPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodPublic;
                    }
                case FSharpGlyph.MethodProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodProtected;
                    }
                case FSharpGlyph.MethodPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodPrivate;
                    }
                case FSharpGlyph.MethodInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodInternal;
                    }
                case FSharpGlyph.ModulePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModulePublic;
                    }
                case FSharpGlyph.ModuleProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModuleProtected;
                    }
                case FSharpGlyph.ModulePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModulePrivate;
                    }
                case FSharpGlyph.ModuleInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModuleInternal;
                    }
                case FSharpGlyph.OpenFolder:
                    {
                        return Microsoft.CodeAnalysis.Glyph.OpenFolder;
                    }
                case FSharpGlyph.Operator:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Operator;
                    }
                case FSharpGlyph.Parameter:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Parameter;
                    }
                case FSharpGlyph.PropertyPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyPublic;
                    }
                case FSharpGlyph.PropertyProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyProtected;
                    }
                case FSharpGlyph.PropertyPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyPrivate;
                    }
                case FSharpGlyph.PropertyInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyInternal;
                    }
                case FSharpGlyph.RangeVariable:
                    {
                        return Microsoft.CodeAnalysis.Glyph.RangeVariable;
                    }
                case FSharpGlyph.Reference:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Reference;
                    }
                case FSharpGlyph.StructurePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructurePublic;
                    }
                case FSharpGlyph.StructureProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructureProtected;
                    }
                case FSharpGlyph.StructurePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructurePrivate;
                    }
                case FSharpGlyph.StructureInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructureInternal;
                    }
                case FSharpGlyph.TypeParameter:
                    {
                        return Microsoft.CodeAnalysis.Glyph.TypeParameter;
                    }
                case FSharpGlyph.Snippet:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Snippet;
                    }
                case FSharpGlyph.CompletionWarning:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CompletionWarning;
                    }
                case FSharpGlyph.AddReference:
                    {
                        return Microsoft.CodeAnalysis.Glyph.AddReference;
                    }
                case FSharpGlyph.NuGet:
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
