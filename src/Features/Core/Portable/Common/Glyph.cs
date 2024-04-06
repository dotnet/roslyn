// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

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
    NuGet,
    TargetTypeMatch
}
