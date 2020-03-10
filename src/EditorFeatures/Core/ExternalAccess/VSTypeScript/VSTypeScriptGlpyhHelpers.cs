// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    internal static class VSTypeScriptGlyphHelpers
    {
        public static Microsoft.CodeAnalysis.Glyph ConvertTo(VSTypeScriptGlyph glyph)
        {
            switch (glyph)
            {
                case VSTypeScriptGlyph.None:
                    {
                        return Microsoft.CodeAnalysis.Glyph.None;
                    }
                case VSTypeScriptGlyph.Assembly:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Assembly;
                    }
                case VSTypeScriptGlyph.BasicFile:
                    {
                        return Microsoft.CodeAnalysis.Glyph.BasicFile;
                    }
                case VSTypeScriptGlyph.BasicProject:
                    {
                        return Microsoft.CodeAnalysis.Glyph.BasicProject;
                    }
                case VSTypeScriptGlyph.ClassPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassPublic;
                    }
                case VSTypeScriptGlyph.ClassProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassProtected;
                    }
                case VSTypeScriptGlyph.ClassPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassPrivate;
                    }
                case VSTypeScriptGlyph.ClassInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ClassInternal;
                    }
                case VSTypeScriptGlyph.CSharpFile:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CSharpFile;
                    }
                case VSTypeScriptGlyph.CSharpProject:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CSharpProject;
                    }
                case VSTypeScriptGlyph.ConstantPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantPublic;
                    }
                case VSTypeScriptGlyph.ConstantProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantProtected;
                    }
                case VSTypeScriptGlyph.ConstantPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantPrivate;
                    }
                case VSTypeScriptGlyph.ConstantInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ConstantInternal;
                    }
                case VSTypeScriptGlyph.DelegatePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegatePublic;
                    }
                case VSTypeScriptGlyph.DelegateProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegateProtected;
                    }
                case VSTypeScriptGlyph.DelegatePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegatePrivate;
                    }
                case VSTypeScriptGlyph.DelegateInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.DelegateInternal;
                    }
                case VSTypeScriptGlyph.EnumPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumPublic;
                    }
                case VSTypeScriptGlyph.EnumProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumProtected;
                    }
                case VSTypeScriptGlyph.EnumPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumPrivate;
                    }
                case VSTypeScriptGlyph.EnumInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumInternal;
                    }
                case VSTypeScriptGlyph.EnumMemberPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberPublic;
                    }
                case VSTypeScriptGlyph.EnumMemberProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberProtected;
                    }
                case VSTypeScriptGlyph.EnumMemberPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberPrivate;
                    }
                case VSTypeScriptGlyph.EnumMemberInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EnumMemberInternal;
                    }
                case VSTypeScriptGlyph.Error:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Error;
                    }
                case VSTypeScriptGlyph.StatusInformation:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StatusInformation;
                    }
                case VSTypeScriptGlyph.EventPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventPublic;
                    }
                case VSTypeScriptGlyph.EventProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventProtected;
                    }
                case VSTypeScriptGlyph.EventPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventPrivate;
                    }
                case VSTypeScriptGlyph.EventInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.EventInternal;
                    }
                case VSTypeScriptGlyph.ExtensionMethodPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodPublic;
                    }
                case VSTypeScriptGlyph.ExtensionMethodProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodProtected;
                    }
                case VSTypeScriptGlyph.ExtensionMethodPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodPrivate;
                    }
                case VSTypeScriptGlyph.ExtensionMethodInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ExtensionMethodInternal;
                    }
                case VSTypeScriptGlyph.FieldPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldPublic;
                    }
                case VSTypeScriptGlyph.FieldProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldProtected;
                    }
                case VSTypeScriptGlyph.FieldPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldPrivate;
                    }
                case VSTypeScriptGlyph.FieldInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.FieldInternal;
                    }
                case VSTypeScriptGlyph.InterfacePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfacePublic;
                    }
                case VSTypeScriptGlyph.InterfaceProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfaceProtected;
                    }
                case VSTypeScriptGlyph.InterfacePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfacePrivate;
                    }
                case VSTypeScriptGlyph.InterfaceInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.InterfaceInternal;
                    }
                case VSTypeScriptGlyph.Intrinsic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Intrinsic;
                    }
                case VSTypeScriptGlyph.Keyword:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Keyword;
                    }
                case VSTypeScriptGlyph.Label:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Label;
                    }
                case VSTypeScriptGlyph.Local:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Local;
                    }
                case VSTypeScriptGlyph.Namespace:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Namespace;
                    }
                case VSTypeScriptGlyph.MethodPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodPublic;
                    }
                case VSTypeScriptGlyph.MethodProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodProtected;
                    }
                case VSTypeScriptGlyph.MethodPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodPrivate;
                    }
                case VSTypeScriptGlyph.MethodInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.MethodInternal;
                    }
                case VSTypeScriptGlyph.ModulePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModulePublic;
                    }
                case VSTypeScriptGlyph.ModuleProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModuleProtected;
                    }
                case VSTypeScriptGlyph.ModulePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModulePrivate;
                    }
                case VSTypeScriptGlyph.ModuleInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.ModuleInternal;
                    }
                case VSTypeScriptGlyph.OpenFolder:
                    {
                        return Microsoft.CodeAnalysis.Glyph.OpenFolder;
                    }
                case VSTypeScriptGlyph.Operator:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Operator;
                    }
                case VSTypeScriptGlyph.Parameter:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Parameter;
                    }
                case VSTypeScriptGlyph.PropertyPublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyPublic;
                    }
                case VSTypeScriptGlyph.PropertyProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyProtected;
                    }
                case VSTypeScriptGlyph.PropertyPrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyPrivate;
                    }
                case VSTypeScriptGlyph.PropertyInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.PropertyInternal;
                    }
                case VSTypeScriptGlyph.RangeVariable:
                    {
                        return Microsoft.CodeAnalysis.Glyph.RangeVariable;
                    }
                case VSTypeScriptGlyph.Reference:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Reference;
                    }
                case VSTypeScriptGlyph.StructurePublic:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructurePublic;
                    }
                case VSTypeScriptGlyph.StructureProtected:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructureProtected;
                    }
                case VSTypeScriptGlyph.StructurePrivate:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructurePrivate;
                    }
                case VSTypeScriptGlyph.StructureInternal:
                    {
                        return Microsoft.CodeAnalysis.Glyph.StructureInternal;
                    }
                case VSTypeScriptGlyph.TypeParameter:
                    {
                        return Microsoft.CodeAnalysis.Glyph.TypeParameter;
                    }
                case VSTypeScriptGlyph.Snippet:
                    {
                        return Microsoft.CodeAnalysis.Glyph.Snippet;
                    }
                case VSTypeScriptGlyph.CompletionWarning:
                    {
                        return Microsoft.CodeAnalysis.Glyph.CompletionWarning;
                    }
                case VSTypeScriptGlyph.AddReference:
                    {
                        return Microsoft.CodeAnalysis.Glyph.AddReference;
                    }
                case VSTypeScriptGlyph.NuGet:
                    {
                        return Microsoft.CodeAnalysis.Glyph.NuGet;
                    }
                case VSTypeScriptGlyph.TargetTypeMatch:
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
