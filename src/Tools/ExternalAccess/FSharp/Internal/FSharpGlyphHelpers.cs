// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal
{
    internal static class FSharpGlyphHelpers
    {
        public static FSharpGlyph ConvertFrom(Microsoft.CodeAnalysis.Glyph glyph)
        {
            switch (glyph)
            {
                case Microsoft.CodeAnalysis.Glyph.None:
                    {
                        return FSharpGlyph.None;
                    }
                case Microsoft.CodeAnalysis.Glyph.Assembly:
                    {
                        return FSharpGlyph.Assembly;
                    }
                case Microsoft.CodeAnalysis.Glyph.BasicFile:
                    {
                        return FSharpGlyph.BasicFile;
                    }
                case Microsoft.CodeAnalysis.Glyph.BasicProject:
                    {
                        return FSharpGlyph.BasicProject;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassPublic:
                    {
                        return FSharpGlyph.ClassPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassProtected:
                    {
                        return FSharpGlyph.ClassProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassPrivate:
                    {
                        return FSharpGlyph.ClassPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassInternal:
                    {
                        return FSharpGlyph.ClassInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.CSharpFile:
                    {
                        return FSharpGlyph.CSharpFile;
                    }
                case Microsoft.CodeAnalysis.Glyph.CSharpProject:
                    {
                        return FSharpGlyph.CSharpProject;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantPublic:
                    {
                        return FSharpGlyph.ConstantPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantProtected:
                    {
                        return FSharpGlyph.ConstantProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantPrivate:
                    {
                        return FSharpGlyph.ConstantPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantInternal:
                    {
                        return FSharpGlyph.ConstantInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegatePublic:
                    {
                        return FSharpGlyph.DelegatePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegateProtected:
                    {
                        return FSharpGlyph.DelegateProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegatePrivate:
                    {
                        return FSharpGlyph.DelegatePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegateInternal:
                    {
                        return FSharpGlyph.DelegateInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumPublic:
                    {
                        return FSharpGlyph.EnumPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumProtected:
                    {
                        return FSharpGlyph.EnumProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumPrivate:
                    {
                        return FSharpGlyph.EnumPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumInternal:
                    {
                        return FSharpGlyph.EnumInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberPublic:
                    {
                        return FSharpGlyph.EnumMemberPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberProtected:
                    {
                        return FSharpGlyph.EnumMemberProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberPrivate:
                    {
                        return FSharpGlyph.EnumMemberPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberInternal:
                    {
                        return FSharpGlyph.EnumMemberInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.Error:
                    {
                        return FSharpGlyph.Error;
                    }
                case Microsoft.CodeAnalysis.Glyph.StatusInformation:
                    {
                        return FSharpGlyph.StatusInformation;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventPublic:
                    {
                        return FSharpGlyph.EventPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventProtected:
                    {
                        return FSharpGlyph.EventProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventPrivate:
                    {
                        return FSharpGlyph.EventPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventInternal:
                    {
                        return FSharpGlyph.EventInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodPublic:
                    {
                        return FSharpGlyph.ExtensionMethodPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodProtected:
                    {
                        return FSharpGlyph.ExtensionMethodProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodPrivate:
                    {
                        return FSharpGlyph.ExtensionMethodPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodInternal:
                    {
                        return FSharpGlyph.ExtensionMethodInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldPublic:
                    {
                        return FSharpGlyph.FieldPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldProtected:
                    {
                        return FSharpGlyph.FieldProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldPrivate:
                    {
                        return FSharpGlyph.FieldPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldInternal:
                    {
                        return FSharpGlyph.FieldInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfacePublic:
                    {
                        return FSharpGlyph.InterfacePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfaceProtected:
                    {
                        return FSharpGlyph.InterfaceProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfacePrivate:
                    {
                        return FSharpGlyph.InterfacePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfaceInternal:
                    {
                        return FSharpGlyph.InterfaceInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.Intrinsic:
                    {
                        return FSharpGlyph.Intrinsic;
                    }
                case Microsoft.CodeAnalysis.Glyph.Keyword:
                    {
                        return FSharpGlyph.Keyword;
                    }
                case Microsoft.CodeAnalysis.Glyph.Label:
                    {
                        return FSharpGlyph.Label;
                    }
                case Microsoft.CodeAnalysis.Glyph.Local:
                    {
                        return FSharpGlyph.Local;
                    }
                case Microsoft.CodeAnalysis.Glyph.Namespace:
                    {
                        return FSharpGlyph.Namespace;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodPublic:
                    {
                        return FSharpGlyph.MethodPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodProtected:
                    {
                        return FSharpGlyph.MethodProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodPrivate:
                    {
                        return FSharpGlyph.MethodPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodInternal:
                    {
                        return FSharpGlyph.MethodInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModulePublic:
                    {
                        return FSharpGlyph.ModulePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModuleProtected:
                    {
                        return FSharpGlyph.ModuleProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModulePrivate:
                    {
                        return FSharpGlyph.ModulePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModuleInternal:
                    {
                        return FSharpGlyph.ModuleInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.OpenFolder:
                    {
                        return FSharpGlyph.OpenFolder;
                    }
                case Microsoft.CodeAnalysis.Glyph.Operator:
                    {
                        return FSharpGlyph.Operator;
                    }
                case Microsoft.CodeAnalysis.Glyph.Parameter:
                    {
                        return FSharpGlyph.Parameter;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyPublic:
                    {
                        return FSharpGlyph.PropertyPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyProtected:
                    {
                        return FSharpGlyph.PropertyProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyPrivate:
                    {
                        return FSharpGlyph.PropertyPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyInternal:
                    {
                        return FSharpGlyph.PropertyInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.RangeVariable:
                    {
                        return FSharpGlyph.RangeVariable;
                    }
                case Microsoft.CodeAnalysis.Glyph.Reference:
                    {
                        return FSharpGlyph.Reference;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructurePublic:
                    {
                        return FSharpGlyph.StructurePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructureProtected:
                    {
                        return FSharpGlyph.StructureProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructurePrivate:
                    {
                        return FSharpGlyph.StructurePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructureInternal:
                    {
                        return FSharpGlyph.StructureInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.TypeParameter:
                    {
                        return FSharpGlyph.TypeParameter;
                    }
                case Microsoft.CodeAnalysis.Glyph.Snippet:
                    {
                        return FSharpGlyph.Snippet;
                    }
                case Microsoft.CodeAnalysis.Glyph.CompletionWarning:
                    {
                        return FSharpGlyph.CompletionWarning;
                    }
                case Microsoft.CodeAnalysis.Glyph.AddReference:
                    {
                        return FSharpGlyph.AddReference;
                    }
                case Microsoft.CodeAnalysis.Glyph.NuGet:
                    {
                        return FSharpGlyph.NuGet;
                    }
                case Microsoft.CodeAnalysis.Glyph.TargetTypeMatch:
                    {
                        return FSharpGlyph.TargetTypeMatch;
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(glyph);
                    }
            }
        }

        public static Microsoft.CodeAnalysis.Glyph ConvertTo(FSharpGlyph glyph)
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
                case FSharpGlyph.TargetTypeMatch:
                    {
                        return Microsoft.CodeAnalysis.Glyph.TargetTypeMatch;
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(glyph);
                    }
            }
        }
    }
}
