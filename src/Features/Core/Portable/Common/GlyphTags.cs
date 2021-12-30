// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis
{
    internal static class GlyphTags
    {
        public static ImmutableArray<string> GetTags(Glyph glyph)
            => glyph switch
            {
                Glyph.Assembly => WellKnownTagArrays.Assembly,
                Glyph.BasicFile => WellKnownTagArrays.VisualBasicFile,
                Glyph.BasicProject => WellKnownTagArrays.VisualBasicProject,
                Glyph.ClassPublic => WellKnownTagArrays.ClassPublic,
                Glyph.ClassProtected => WellKnownTagArrays.ClassProtected,
                Glyph.ClassPrivate => WellKnownTagArrays.ClassPrivate,
                Glyph.ClassInternal => WellKnownTagArrays.ClassInternal,
                Glyph.CSharpFile => WellKnownTagArrays.CSharpFile,
                Glyph.CSharpProject => WellKnownTagArrays.CSharpProject,
                Glyph.ConstantPublic => WellKnownTagArrays.ConstantPublic,
                Glyph.ConstantProtected => WellKnownTagArrays.ConstantProtected,
                Glyph.ConstantPrivate => WellKnownTagArrays.ConstantPrivate,
                Glyph.ConstantInternal => WellKnownTagArrays.ConstantInternal,
                Glyph.DelegatePublic => WellKnownTagArrays.DelegatePublic,
                Glyph.DelegateProtected => WellKnownTagArrays.DelegateProtected,
                Glyph.DelegatePrivate => WellKnownTagArrays.DelegatePrivate,
                Glyph.DelegateInternal => WellKnownTagArrays.DelegateInternal,
                Glyph.EnumPublic => WellKnownTagArrays.EnumPublic,
                Glyph.EnumProtected => WellKnownTagArrays.EnumProtected,
                Glyph.EnumPrivate => WellKnownTagArrays.EnumPrivate,
                Glyph.EnumInternal => WellKnownTagArrays.EnumInternal,
                Glyph.EnumMemberPublic => WellKnownTagArrays.EnumMemberPublic,
                Glyph.EnumMemberProtected => WellKnownTagArrays.EnumMemberProtected,
                Glyph.EnumMemberPrivate => WellKnownTagArrays.EnumMemberPrivate,
                Glyph.EnumMemberInternal => WellKnownTagArrays.EnumMemberInternal,
                Glyph.Error => WellKnownTagArrays.Error,
                Glyph.EventPublic => WellKnownTagArrays.EventPublic,
                Glyph.EventProtected => WellKnownTagArrays.EventProtected,
                Glyph.EventPrivate => WellKnownTagArrays.EventPrivate,
                Glyph.EventInternal => WellKnownTagArrays.EventInternal,
                Glyph.ExtensionMethodPublic => WellKnownTagArrays.ExtensionMethodPublic,
                Glyph.ExtensionMethodProtected => WellKnownTagArrays.ExtensionMethodProtected,
                Glyph.ExtensionMethodPrivate => WellKnownTagArrays.ExtensionMethodPrivate,
                Glyph.ExtensionMethodInternal => WellKnownTagArrays.ExtensionMethodInternal,
                Glyph.FieldPublic => WellKnownTagArrays.FieldPublic,
                Glyph.FieldProtected => WellKnownTagArrays.FieldProtected,
                Glyph.FieldPrivate => WellKnownTagArrays.FieldPrivate,
                Glyph.FieldInternal => WellKnownTagArrays.FieldInternal,
                Glyph.InterfacePublic => WellKnownTagArrays.InterfacePublic,
                Glyph.InterfaceProtected => WellKnownTagArrays.InterfaceProtected,
                Glyph.InterfacePrivate => WellKnownTagArrays.InterfacePrivate,
                Glyph.InterfaceInternal => WellKnownTagArrays.InterfaceInternal,
                Glyph.Intrinsic => WellKnownTagArrays.Intrinsic,
                Glyph.Keyword => WellKnownTagArrays.Keyword,
                Glyph.Label => WellKnownTagArrays.Label,
                Glyph.Local => WellKnownTagArrays.Local,
                Glyph.Namespace => WellKnownTagArrays.Namespace,
                Glyph.MethodPublic => WellKnownTagArrays.MethodPublic,
                Glyph.MethodProtected => WellKnownTagArrays.MethodProtected,
                Glyph.MethodPrivate => WellKnownTagArrays.MethodPrivate,
                Glyph.MethodInternal => WellKnownTagArrays.MethodInternal,
                Glyph.ModulePublic => WellKnownTagArrays.ModulePublic,
                Glyph.ModuleProtected => WellKnownTagArrays.ModuleProtected,
                Glyph.ModulePrivate => WellKnownTagArrays.ModulePrivate,
                Glyph.ModuleInternal => WellKnownTagArrays.ModuleInternal,
                Glyph.OpenFolder => WellKnownTagArrays.Folder,
                Glyph.Operator => WellKnownTagArrays.Operator,
                Glyph.Parameter => WellKnownTagArrays.Parameter,
                Glyph.PropertyPublic => WellKnownTagArrays.PropertyPublic,
                Glyph.PropertyProtected => WellKnownTagArrays.PropertyProtected,
                Glyph.PropertyPrivate => WellKnownTagArrays.PropertyPrivate,
                Glyph.PropertyInternal => WellKnownTagArrays.PropertyInternal,
                Glyph.RangeVariable => WellKnownTagArrays.RangeVariable,
                Glyph.Reference => WellKnownTagArrays.Reference,
                Glyph.StructurePublic => WellKnownTagArrays.StructurePublic,
                Glyph.StructureProtected => WellKnownTagArrays.StructureProtected,
                Glyph.StructurePrivate => WellKnownTagArrays.StructurePrivate,
                Glyph.StructureInternal => WellKnownTagArrays.StructureInternal,
                Glyph.TypeParameter => WellKnownTagArrays.TypeParameter,
                Glyph.Snippet => WellKnownTagArrays.Snippet,
                Glyph.CompletionWarning => WellKnownTagArrays.Warning,
                Glyph.StatusInformation => WellKnownTagArrays.StatusInformation,
                _ => ImmutableArray<string>.Empty,
            };
    }
}
