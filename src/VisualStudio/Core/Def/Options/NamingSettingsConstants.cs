// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal static class NamingSettingsConstants
{
    internal static class SettingNames
    {
        public const string Rules = "rules";
        public const string SymbolSpecifications = "symbolSpecifications";
        public const string NamingStyles = "namingStyles";
        public const string CSharpPrefix = "languages.csharp.codeStyle.naming.";
        public const string VisualBasicPrefix = "languages.basic.codeStyle.naming.";
    }

    internal static class Properties
    {
        public const string Id = "id";
        public const string Name = "name";
        public const string DefaultName = "defaultName";
        public const string LegacyId = "legacyId";
        public const string SymbolKinds = "symbolKinds";
        public const string Accessibilities = "accessibilities";
        public const string RequiredModifiers = "requiredModifiers";
        public const string Prefix = "prefix";
        public const string Suffix = "suffix";
        public const string WordSeparator = "wordSeparator";
        public const string Capitalization = "capitalization";
        public const string SymbolSpecificationId = "symbolSpecificationId";
        public const string NamingStyleId = "namingStyleId";
        public const string Severity = "severity";
    }

    internal static class Monikers
    {
        public const string RuleSymbolSpecificationId = SettingNames.Rules + "." + Properties.SymbolSpecificationId;
        public const string RuleNamingStyleId = SettingNames.Rules + "." + Properties.NamingStyleId;
        public const string RuleSeverity = SettingNames.Rules + "." + Properties.Severity;
        public const string SpecificationSymbolKinds = SettingNames.SymbolSpecifications + "." + Properties.SymbolKinds;
        public const string SpecificationAccessibilities = SettingNames.SymbolSpecifications + "." + Properties.Accessibilities;
        public const string SpecificationRequiredModifiers = SettingNames.SymbolSpecifications + "." + Properties.RequiredModifiers;
        public const string StyleCapitalization = SettingNames.NamingStyles + "." + Properties.Capitalization;
    }

    internal static class IdentityKinds
    {
        public const string SymbolSpecification = "symbolSpecification";
        public const string NamingStyle = "namingStyle";
    }

    internal static class SymbolKinds
    {
        public const string Namespace = "namespace";
        public const string Property = "property";
        public const string Field = "field";
        public const string Event = "event";
        public const string Parameter = "parameter";
        public const string TypeParameter = "typeParameter";
        public const string Local = "local";
        public const string Class = "class";
        public const string Struct = "struct";
        public const string Interface = "interface";
        public const string Delegate = "delegate";
        public const string Enum = "enum";
        public const string Module = "module";
        public const string Pointer = "pointer";
        public const string Method = "method";
        public const string LocalFunction = "localFunction";
        public const string SymbolPrefix = "symbol:";
        public const string TypePrefix = "type:";
        public const string MethodPrefix = "method:";
    }

    internal static class Accessibilities
    {
        public const string Local = "local";
        public const string Public = "public";
        public const string Private = "private";
        public const string Internal = "internal";
        public const string Protected = "protected";
        public const string PrivateProtected = "privateProtected";
        public const string ProtectedInternal = "protectedInternal";
    }

    internal static class Modifiers
    {
        public const string Abstract = "abstract";
        public const string Static = "static";
        public const string Async = "async";
        public const string ReadOnly = "readonly";
        public const string Const = "const";
    }

    internal static class Capitalizations
    {
        public const string PascalCase = "pascalCase";
        public const string CamelCase = "camelCase";
        public const string FirstUpper = "firstUpper";
        public const string AllUpper = "allUpper";
        public const string AllLower = "allLower";
    }

    internal static class Severities
    {
        public const string Silent = "silent";
        public const string Suggestion = "suggestion";
        public const string Warning = "warning";
        public const string Error = "error";
    }
}
