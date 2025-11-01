// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Tags;

public static class WellKnownTags
{
    // accessibility
    public const string Public = nameof(Public);
    public const string Protected = nameof(Protected);
    public const string Private = nameof(Private);
    public const string Internal = nameof(Internal);

    // project elements
    public const string File = nameof(File);
    public const string Project = nameof(Project);
    public const string Folder = nameof(Folder);
    public const string Assembly = nameof(Assembly);

    // language elements
    public const string Class = nameof(Class);
    public const string Constant = nameof(Constant);
    public const string Delegate = nameof(Delegate);
    public const string Enum = nameof(Enum);
    public const string EnumMember = nameof(EnumMember);
    public const string Event = nameof(Event);
    public const string ExtensionMethod = nameof(ExtensionMethod);
    public const string Field = nameof(Field);
    public const string Interface = nameof(Interface);
    public const string Intrinsic = nameof(Intrinsic);
    public const string Keyword = nameof(Keyword);
    public const string Label = nameof(Label);
    public const string Local = nameof(Local);
    public const string Namespace = nameof(Namespace);
    public const string Method = nameof(Method);
    public const string Module = nameof(Module);
    public const string Operator = nameof(Operator);
    public const string Parameter = nameof(Parameter);
    public const string Property = nameof(Property);
    public const string RangeVariable = nameof(RangeVariable);
    public const string Reference = nameof(Reference);
    public const string Structure = nameof(Structure);
    public const string TypeParameter = nameof(TypeParameter);

    // other
    public const string Snippet = nameof(Snippet);
    public const string Error = nameof(Error);
    public const string Warning = nameof(Warning);

    internal const string Deprecated = nameof(Deprecated);

    internal const string StatusInformation = nameof(StatusInformation);

    internal const string AddReference = nameof(AddReference);
    internal const string NuGet = nameof(NuGet);
    internal const string TargetTypeMatch = nameof(TargetTypeMatch);

    internal const string Copilot = nameof(Copilot);
}

internal static class WellKnownTagArrays
{
    internal static readonly ImmutableArray<string> Assembly = [WellKnownTags.Assembly];
    internal static readonly ImmutableArray<string> ClassPublic = [WellKnownTags.Class, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> ClassProtected = [WellKnownTags.Class, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> ClassPrivate = [WellKnownTags.Class, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> ClassInternal = [WellKnownTags.Class, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> ConstantPublic = [WellKnownTags.Constant, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> ConstantProtected = [WellKnownTags.Constant, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> ConstantPrivate = [WellKnownTags.Constant, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> ConstantInternal = [WellKnownTags.Constant, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> DelegatePublic = [WellKnownTags.Delegate, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> DelegateProtected = [WellKnownTags.Delegate, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> DelegatePrivate = [WellKnownTags.Delegate, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> DelegateInternal = [WellKnownTags.Delegate, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> EnumPublic = [WellKnownTags.Enum, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> EnumProtected = [WellKnownTags.Enum, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> EnumPrivate = [WellKnownTags.Enum, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> EnumInternal = [WellKnownTags.Enum, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> EnumMemberPublic = [WellKnownTags.EnumMember, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> EnumMemberProtected = [WellKnownTags.EnumMember, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> EnumMemberPrivate = [WellKnownTags.EnumMember, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> EnumMemberInternal = [WellKnownTags.EnumMember, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> EventPublic = [WellKnownTags.Event, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> EventProtected = [WellKnownTags.Event, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> EventPrivate = [WellKnownTags.Event, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> EventInternal = [WellKnownTags.Event, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> ExtensionMethodPublic = [WellKnownTags.ExtensionMethod, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> ExtensionMethodProtected = [WellKnownTags.ExtensionMethod, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> ExtensionMethodPrivate = [WellKnownTags.ExtensionMethod, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> ExtensionMethodInternal = [WellKnownTags.ExtensionMethod, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> FieldPublic = [WellKnownTags.Field, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> FieldProtected = [WellKnownTags.Field, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> FieldPrivate = [WellKnownTags.Field, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> FieldInternal = [WellKnownTags.Field, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> InterfacePublic = [WellKnownTags.Interface, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> InterfaceProtected = [WellKnownTags.Interface, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> InterfacePrivate = [WellKnownTags.Interface, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> InterfaceInternal = [WellKnownTags.Interface, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> Intrinsic = [WellKnownTags.Intrinsic];
    internal static readonly ImmutableArray<string> Keyword = [WellKnownTags.Keyword];
    internal static readonly ImmutableArray<string> Label = [WellKnownTags.Label];
    internal static readonly ImmutableArray<string> Local = [WellKnownTags.Local];
    internal static readonly ImmutableArray<string> Namespace = [WellKnownTags.Namespace];
    internal static readonly ImmutableArray<string> MethodPublic = [WellKnownTags.Method, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> MethodProtected = [WellKnownTags.Method, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> MethodPrivate = [WellKnownTags.Method, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> MethodInternal = [WellKnownTags.Method, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> ModulePublic = [WellKnownTags.Module, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> ModuleProtected = [WellKnownTags.Module, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> ModulePrivate = [WellKnownTags.Module, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> ModuleInternal = [WellKnownTags.Module, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> Folder = [WellKnownTags.Folder];
    internal static readonly ImmutableArray<string> OperatorPublic = [WellKnownTags.Operator, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> OperatorProtected = [WellKnownTags.Operator, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> OperatorPrivate = [WellKnownTags.Operator, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> OperatorInternal = [WellKnownTags.Operator, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> Parameter = [WellKnownTags.Parameter];
    internal static readonly ImmutableArray<string> PropertyPublic = [WellKnownTags.Property, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> PropertyProtected = [WellKnownTags.Property, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> PropertyPrivate = [WellKnownTags.Property, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> PropertyInternal = [WellKnownTags.Property, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> RangeVariable = [WellKnownTags.RangeVariable];
    internal static readonly ImmutableArray<string> Reference = [WellKnownTags.Reference];
    internal static readonly ImmutableArray<string> StructurePublic = [WellKnownTags.Structure, WellKnownTags.Public];
    internal static readonly ImmutableArray<string> StructureProtected = [WellKnownTags.Structure, WellKnownTags.Protected];
    internal static readonly ImmutableArray<string> StructurePrivate = [WellKnownTags.Structure, WellKnownTags.Private];
    internal static readonly ImmutableArray<string> StructureInternal = [WellKnownTags.Structure, WellKnownTags.Internal];
    internal static readonly ImmutableArray<string> TypeParameter = [WellKnownTags.TypeParameter];
    internal static readonly ImmutableArray<string> Snippet = [WellKnownTags.Snippet];

    internal static readonly ImmutableArray<string> Error = [WellKnownTags.Error];
    internal static readonly ImmutableArray<string> Warning = [WellKnownTags.Warning];
    internal static readonly ImmutableArray<string> StatusInformation = [WellKnownTags.StatusInformation];

    internal static readonly ImmutableArray<string> AddReference = [WellKnownTags.AddReference];
    internal static readonly ImmutableArray<string> TargetTypeMatch = [WellKnownTags.TargetTypeMatch];

    internal static readonly ImmutableArray<string> CSharpFile = [WellKnownTags.File, LanguageNames.CSharp];
    internal static readonly ImmutableArray<string> VisualBasicFile = [WellKnownTags.File, LanguageNames.VisualBasic];

    internal static readonly ImmutableArray<string> CSharpProject = [WellKnownTags.Project, LanguageNames.CSharp];
    internal static readonly ImmutableArray<string> VisualBasicProject = [WellKnownTags.Project, LanguageNames.VisualBasic];
}
