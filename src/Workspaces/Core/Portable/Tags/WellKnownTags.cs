// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Tags
{
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

        internal const string StatusInformation = nameof(StatusInformation);

        internal const string AddReference = nameof(AddReference);
        internal const string NuGet = nameof(NuGet);
        internal const string TargetTypeMatch = nameof(TargetTypeMatch);
    }

    internal static class WellKnownTagArrays
    {
        internal static readonly ImmutableArray<string> Assembly = ImmutableArray.Create(WellKnownTags.Assembly);
        internal static readonly ImmutableArray<string> ClassPublic = ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> ClassProtected = ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> ClassPrivate = ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> ClassInternal = ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> ConstantPublic = ImmutableArray.Create(WellKnownTags.Constant, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> ConstantProtected = ImmutableArray.Create(WellKnownTags.Constant, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> ConstantPrivate = ImmutableArray.Create(WellKnownTags.Constant, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> ConstantInternal = ImmutableArray.Create(WellKnownTags.Constant, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> DelegatePublic = ImmutableArray.Create(WellKnownTags.Delegate, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> DelegateProtected = ImmutableArray.Create(WellKnownTags.Delegate, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> DelegatePrivate = ImmutableArray.Create(WellKnownTags.Delegate, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> DelegateInternal = ImmutableArray.Create(WellKnownTags.Delegate, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> EnumPublic = ImmutableArray.Create(WellKnownTags.Enum, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> EnumProtected = ImmutableArray.Create(WellKnownTags.Enum, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> EnumPrivate = ImmutableArray.Create(WellKnownTags.Enum, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> EnumInternal = ImmutableArray.Create(WellKnownTags.Enum, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> EnumMemberPublic = ImmutableArray.Create(WellKnownTags.EnumMember, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> EnumMemberProtected = ImmutableArray.Create(WellKnownTags.EnumMember, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> EnumMemberPrivate = ImmutableArray.Create(WellKnownTags.EnumMember, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> EnumMemberInternal = ImmutableArray.Create(WellKnownTags.EnumMember, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> EventPublic = ImmutableArray.Create(WellKnownTags.Event, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> EventProtected = ImmutableArray.Create(WellKnownTags.Event, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> EventPrivate = ImmutableArray.Create(WellKnownTags.Event, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> EventInternal = ImmutableArray.Create(WellKnownTags.Event, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> ExtensionMethodPublic = ImmutableArray.Create(WellKnownTags.ExtensionMethod, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> ExtensionMethodProtected = ImmutableArray.Create(WellKnownTags.ExtensionMethod, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> ExtensionMethodPrivate = ImmutableArray.Create(WellKnownTags.ExtensionMethod, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> ExtensionMethodInternal = ImmutableArray.Create(WellKnownTags.ExtensionMethod, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> FieldPublic = ImmutableArray.Create(WellKnownTags.Field, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> FieldProtected = ImmutableArray.Create(WellKnownTags.Field, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> FieldPrivate = ImmutableArray.Create(WellKnownTags.Field, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> FieldInternal = ImmutableArray.Create(WellKnownTags.Field, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> InterfacePublic = ImmutableArray.Create(WellKnownTags.Interface, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> InterfaceProtected = ImmutableArray.Create(WellKnownTags.Interface, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> InterfacePrivate = ImmutableArray.Create(WellKnownTags.Interface, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> InterfaceInternal = ImmutableArray.Create(WellKnownTags.Interface, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> Intrinsic = ImmutableArray.Create(WellKnownTags.Intrinsic);
        internal static readonly ImmutableArray<string> Keyword = ImmutableArray.Create(WellKnownTags.Keyword);
        internal static readonly ImmutableArray<string> Label = ImmutableArray.Create(WellKnownTags.Label);
        internal static readonly ImmutableArray<string> Local = ImmutableArray.Create(WellKnownTags.Local);
        internal static readonly ImmutableArray<string> Namespace = ImmutableArray.Create(WellKnownTags.Namespace);
        internal static readonly ImmutableArray<string> MethodPublic = ImmutableArray.Create(WellKnownTags.Method, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> MethodProtected = ImmutableArray.Create(WellKnownTags.Method, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> MethodPrivate = ImmutableArray.Create(WellKnownTags.Method, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> MethodInternal = ImmutableArray.Create(WellKnownTags.Method, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> ModulePublic = ImmutableArray.Create(WellKnownTags.Module, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> ModuleProtected = ImmutableArray.Create(WellKnownTags.Module, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> ModulePrivate = ImmutableArray.Create(WellKnownTags.Module, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> ModuleInternal = ImmutableArray.Create(WellKnownTags.Module, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> Folder = ImmutableArray.Create(WellKnownTags.Folder);
        internal static readonly ImmutableArray<string> Operator = ImmutableArray.Create(WellKnownTags.Operator);
        internal static readonly ImmutableArray<string> Parameter = ImmutableArray.Create(WellKnownTags.Parameter);
        internal static readonly ImmutableArray<string> PropertyPublic = ImmutableArray.Create(WellKnownTags.Property, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> PropertyProtected = ImmutableArray.Create(WellKnownTags.Property, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> PropertyPrivate = ImmutableArray.Create(WellKnownTags.Property, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> PropertyInternal = ImmutableArray.Create(WellKnownTags.Property, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> RangeVariable = ImmutableArray.Create(WellKnownTags.RangeVariable);
        internal static readonly ImmutableArray<string> Reference = ImmutableArray.Create(WellKnownTags.Reference);
        internal static readonly ImmutableArray<string> StructurePublic = ImmutableArray.Create(WellKnownTags.Structure, WellKnownTags.Public);
        internal static readonly ImmutableArray<string> StructureProtected = ImmutableArray.Create(WellKnownTags.Structure, WellKnownTags.Protected);
        internal static readonly ImmutableArray<string> StructurePrivate = ImmutableArray.Create(WellKnownTags.Structure, WellKnownTags.Private);
        internal static readonly ImmutableArray<string> StructureInternal = ImmutableArray.Create(WellKnownTags.Structure, WellKnownTags.Internal);
        internal static readonly ImmutableArray<string> TypeParameter = ImmutableArray.Create(WellKnownTags.TypeParameter);
        internal static readonly ImmutableArray<string> Snippet = ImmutableArray.Create(WellKnownTags.Snippet);

        internal static readonly ImmutableArray<string> Error = ImmutableArray.Create(WellKnownTags.Error);
        internal static readonly ImmutableArray<string> Warning = ImmutableArray.Create(WellKnownTags.Warning);
        internal static readonly ImmutableArray<string> StatusInformation = ImmutableArray.Create(WellKnownTags.StatusInformation);

        internal static readonly ImmutableArray<string> AddReference = ImmutableArray.Create(WellKnownTags.AddReference);
        internal static readonly ImmutableArray<string> TargetTypeMatch = ImmutableArray.Create(WellKnownTags.TargetTypeMatch);

        internal static readonly ImmutableArray<string> CSharpFile = ImmutableArray.Create(WellKnownTags.File, LanguageNames.CSharp);
        internal static readonly ImmutableArray<string> VisualBasicFile = ImmutableArray.Create(WellKnownTags.File, LanguageNames.VisualBasic);

        internal static readonly ImmutableArray<string> CSharpProject = ImmutableArray.Create(WellKnownTags.Project, LanguageNames.CSharp);
        internal static readonly ImmutableArray<string> VisualBasicProject = ImmutableArray.Create(WellKnownTags.Project, LanguageNames.VisualBasic);
    }
}
