// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    internal static class VSTypeScriptGlyphHelpers
    {
        public static Glyph ConvertTo(VSTypeScriptGlyph glyph)
        {
            return glyph switch
            {
                VSTypeScriptGlyph.None => Glyph.None,
                VSTypeScriptGlyph.Assembly => Glyph.Assembly,
                VSTypeScriptGlyph.BasicFile => Glyph.BasicFile,
                VSTypeScriptGlyph.BasicProject => Glyph.BasicProject,
                VSTypeScriptGlyph.ClassPublic => Glyph.ClassPublic,
                VSTypeScriptGlyph.ClassProtected => Glyph.ClassProtected,
                VSTypeScriptGlyph.ClassPrivate => Glyph.ClassPrivate,
                VSTypeScriptGlyph.ClassInternal => Glyph.ClassInternal,
                VSTypeScriptGlyph.CSharpFile => Glyph.CSharpFile,
                VSTypeScriptGlyph.CSharpProject => Glyph.CSharpProject,
                VSTypeScriptGlyph.ConstantPublic => Glyph.ConstantPublic,
                VSTypeScriptGlyph.ConstantProtected => Glyph.ConstantProtected,
                VSTypeScriptGlyph.ConstantPrivate => Glyph.ConstantPrivate,
                VSTypeScriptGlyph.ConstantInternal => Glyph.ConstantInternal,
                VSTypeScriptGlyph.DelegatePublic => Glyph.DelegatePublic,
                VSTypeScriptGlyph.DelegateProtected => Glyph.DelegateProtected,
                VSTypeScriptGlyph.DelegatePrivate => Glyph.DelegatePrivate,
                VSTypeScriptGlyph.DelegateInternal => Glyph.DelegateInternal,
                VSTypeScriptGlyph.EnumPublic => Glyph.EnumPublic,
                VSTypeScriptGlyph.EnumProtected => Glyph.EnumProtected,
                VSTypeScriptGlyph.EnumPrivate => Glyph.EnumPrivate,
                VSTypeScriptGlyph.EnumInternal => Glyph.EnumInternal,
                VSTypeScriptGlyph.EnumMemberPublic => Glyph.EnumMemberPublic,
                VSTypeScriptGlyph.EnumMemberProtected => Glyph.EnumMemberProtected,
                VSTypeScriptGlyph.EnumMemberPrivate => Glyph.EnumMemberPrivate,
                VSTypeScriptGlyph.EnumMemberInternal => Glyph.EnumMemberInternal,
                VSTypeScriptGlyph.Error => Glyph.Error,
                VSTypeScriptGlyph.StatusInformation => Glyph.StatusInformation,
                VSTypeScriptGlyph.EventPublic => Glyph.EventPublic,
                VSTypeScriptGlyph.EventProtected => Glyph.EventProtected,
                VSTypeScriptGlyph.EventPrivate => Glyph.EventPrivate,
                VSTypeScriptGlyph.EventInternal => Glyph.EventInternal,
                VSTypeScriptGlyph.ExtensionMethodPublic => Glyph.ExtensionMethodPublic,
                VSTypeScriptGlyph.ExtensionMethodProtected => Glyph.ExtensionMethodProtected,
                VSTypeScriptGlyph.ExtensionMethodPrivate => Glyph.ExtensionMethodPrivate,
                VSTypeScriptGlyph.ExtensionMethodInternal => Glyph.ExtensionMethodInternal,
                VSTypeScriptGlyph.FieldPublic => Glyph.FieldPublic,
                VSTypeScriptGlyph.FieldProtected => Glyph.FieldProtected,
                VSTypeScriptGlyph.FieldPrivate => Glyph.FieldPrivate,
                VSTypeScriptGlyph.FieldInternal => Glyph.FieldInternal,
                VSTypeScriptGlyph.InterfacePublic => Glyph.InterfacePublic,
                VSTypeScriptGlyph.InterfaceProtected => Glyph.InterfaceProtected,
                VSTypeScriptGlyph.InterfacePrivate => Glyph.InterfacePrivate,
                VSTypeScriptGlyph.InterfaceInternal => Glyph.InterfaceInternal,
                VSTypeScriptGlyph.Intrinsic => Glyph.Intrinsic,
                VSTypeScriptGlyph.Keyword => Glyph.Keyword,
                VSTypeScriptGlyph.Label => Glyph.Label,
                VSTypeScriptGlyph.Local => Glyph.Local,
                VSTypeScriptGlyph.Namespace => Glyph.Namespace,
                VSTypeScriptGlyph.MethodPublic => Glyph.MethodPublic,
                VSTypeScriptGlyph.MethodProtected => Glyph.MethodProtected,
                VSTypeScriptGlyph.MethodPrivate => Glyph.MethodPrivate,
                VSTypeScriptGlyph.MethodInternal => Glyph.MethodInternal,
                VSTypeScriptGlyph.ModulePublic => Glyph.ModulePublic,
                VSTypeScriptGlyph.ModuleProtected => Glyph.ModuleProtected,
                VSTypeScriptGlyph.ModulePrivate => Glyph.ModulePrivate,
                VSTypeScriptGlyph.ModuleInternal => Glyph.ModuleInternal,
                VSTypeScriptGlyph.OpenFolder => Glyph.OpenFolder,
                VSTypeScriptGlyph.Operator => Glyph.Operator,
                VSTypeScriptGlyph.Parameter => Glyph.Parameter,
                VSTypeScriptGlyph.PropertyPublic => Glyph.PropertyPublic,
                VSTypeScriptGlyph.PropertyProtected => Glyph.PropertyProtected,
                VSTypeScriptGlyph.PropertyPrivate => Glyph.PropertyPrivate,
                VSTypeScriptGlyph.PropertyInternal => Glyph.PropertyInternal,
                VSTypeScriptGlyph.RangeVariable => Glyph.RangeVariable,
                VSTypeScriptGlyph.Reference => Glyph.Reference,
                VSTypeScriptGlyph.StructurePublic => Glyph.StructurePublic,
                VSTypeScriptGlyph.StructureProtected => Glyph.StructureProtected,
                VSTypeScriptGlyph.StructurePrivate => Glyph.StructurePrivate,
                VSTypeScriptGlyph.StructureInternal => Glyph.StructureInternal,
                VSTypeScriptGlyph.TypeParameter => Glyph.TypeParameter,
                VSTypeScriptGlyph.Snippet => Glyph.Snippet,
                VSTypeScriptGlyph.CompletionWarning => Glyph.CompletionWarning,
                VSTypeScriptGlyph.AddReference => Glyph.AddReference,
                VSTypeScriptGlyph.NuGet => Glyph.NuGet,
                VSTypeScriptGlyph.TargetTypeMatch => Glyph.TargetTypeMatch,
                _ => throw ExceptionUtilities.UnexpectedValue(glyph),
            };
        }
    }
}
