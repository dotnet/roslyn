// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis
{
    internal static class GlyphTags
    {
        public static ImmutableArray<string> GetTags(Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.Assembly: return WellKnownTagArrays.Assembly;
                case Glyph.BasicFile: return WellKnownTagArrays.VisualBasicFile;
                case Glyph.BasicProject: return WellKnownTagArrays.VisualBasicProject;
                case Glyph.ClassPublic: return WellKnownTagArrays.ClassPublic;
                case Glyph.ClassProtected: return WellKnownTagArrays.ClassProtected;
                case Glyph.ClassPrivate: return WellKnownTagArrays.ClassPrivate;
                case Glyph.ClassInternal: return WellKnownTagArrays.ClassInternal;
                case Glyph.CSharpFile: return WellKnownTagArrays.CSharpFile;
                case Glyph.CSharpProject: return WellKnownTagArrays.CSharpProject;
                case Glyph.ConstantPublic: return WellKnownTagArrays.ConstantPublic;
                case Glyph.ConstantProtected: return WellKnownTagArrays.ConstantProtected;
                case Glyph.ConstantPrivate: return WellKnownTagArrays.ConstantPrivate;
                case Glyph.ConstantInternal: return WellKnownTagArrays.ConstantInternal;
                case Glyph.DelegatePublic: return WellKnownTagArrays.DelegatePublic;
                case Glyph.DelegateProtected: return WellKnownTagArrays.DelegateProtected;
                case Glyph.DelegatePrivate: return WellKnownTagArrays.DelegatePrivate;
                case Glyph.DelegateInternal: return WellKnownTagArrays.DelegateInternal;
                case Glyph.EnumPublic: return WellKnownTagArrays.EnumPublic;
                case Glyph.EnumProtected: return WellKnownTagArrays.EnumProtected;
                case Glyph.EnumPrivate: return WellKnownTagArrays.EnumPrivate;
                case Glyph.EnumInternal: return WellKnownTagArrays.EnumInternal;
                case Glyph.EnumMemberPublic: return WellKnownTagArrays.EnumMemberPublic;
                case Glyph.EnumMemberProtected: return WellKnownTagArrays.EnumMemberProtected;
                case Glyph.EnumMemberPrivate: return WellKnownTagArrays.EnumMemberPrivate;
                case Glyph.EnumMemberInternal: return WellKnownTagArrays.EnumMemberInternal;
                case Glyph.Error: return WellKnownTagArrays.Error;
                case Glyph.EventPublic: return WellKnownTagArrays.EventPublic;
                case Glyph.EventProtected: return WellKnownTagArrays.EventProtected;
                case Glyph.EventPrivate: return WellKnownTagArrays.EventPrivate;
                case Glyph.EventInternal: return WellKnownTagArrays.EventInternal;
                case Glyph.ExtensionMethodPublic: return WellKnownTagArrays.ExtensionMethodPublic;
                case Glyph.ExtensionMethodProtected: return WellKnownTagArrays.ExtensionMethodProtected;
                case Glyph.ExtensionMethodPrivate: return WellKnownTagArrays.ExtensionMethodPrivate;
                case Glyph.ExtensionMethodInternal: return WellKnownTagArrays.ExtensionMethodInternal;
                case Glyph.FieldPublic: return WellKnownTagArrays.FieldPublic;
                case Glyph.FieldProtected: return WellKnownTagArrays.FieldProtected;
                case Glyph.FieldPrivate: return WellKnownTagArrays.FieldPrivate;
                case Glyph.FieldInternal: return WellKnownTagArrays.FieldInternal;
                case Glyph.InterfacePublic: return WellKnownTagArrays.InterfacePublic;
                case Glyph.InterfaceProtected: return WellKnownTagArrays.InterfaceProtected;
                case Glyph.InterfacePrivate: return WellKnownTagArrays.InterfacePrivate;
                case Glyph.InterfaceInternal: return WellKnownTagArrays.InterfaceInternal;
                case Glyph.Intrinsic: return WellKnownTagArrays.Intrinsic;
                case Glyph.Keyword: return WellKnownTagArrays.Keyword;
                case Glyph.Label: return WellKnownTagArrays.Label;
                case Glyph.Local: return WellKnownTagArrays.Local;
                case Glyph.Namespace: return WellKnownTagArrays.Namespace;
                case Glyph.MethodPublic: return WellKnownTagArrays.MethodPublic;
                case Glyph.MethodProtected: return WellKnownTagArrays.MethodProtected;
                case Glyph.MethodPrivate: return WellKnownTagArrays.MethodPrivate;
                case Glyph.MethodInternal: return WellKnownTagArrays.MethodInternal;
                case Glyph.ModulePublic: return WellKnownTagArrays.ModulePublic;
                case Glyph.ModuleProtected: return WellKnownTagArrays.ModuleProtected;
                case Glyph.ModulePrivate: return WellKnownTagArrays.ModulePrivate;
                case Glyph.ModuleInternal: return WellKnownTagArrays.ModuleInternal;
                case Glyph.OpenFolder: return WellKnownTagArrays.Folder;
                case Glyph.Operator: return WellKnownTagArrays.Operator;
                case Glyph.Parameter: return WellKnownTagArrays.Parameter;
                case Glyph.PropertyPublic: return WellKnownTagArrays.PropertyPublic;
                case Glyph.PropertyProtected: return WellKnownTagArrays.PropertyProtected;
                case Glyph.PropertyPrivate: return WellKnownTagArrays.PropertyPrivate;
                case Glyph.PropertyInternal: return WellKnownTagArrays.PropertyInternal;
                case Glyph.RangeVariable: return WellKnownTagArrays.RangeVariable;
                case Glyph.Reference: return WellKnownTagArrays.Reference;
                case Glyph.StructurePublic: return WellKnownTagArrays.StructurePublic;
                case Glyph.StructureProtected: return WellKnownTagArrays.StructureProtected;
                case Glyph.StructurePrivate: return WellKnownTagArrays.StructurePrivate;
                case Glyph.StructureInternal: return WellKnownTagArrays.StructureInternal;
                case Glyph.TypeParameter: return WellKnownTagArrays.TypeParameter;
                case Glyph.Snippet: return WellKnownTagArrays.Snippet;
                case Glyph.CompletionWarning: return WellKnownTagArrays.Warning;
                case Glyph.StatusInformation: return WellKnownTagArrays.StatusInformation;
                default: return ImmutableArray<string>.Empty;
            }
        }
    }
}
