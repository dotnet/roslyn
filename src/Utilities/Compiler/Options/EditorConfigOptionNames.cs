// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities
{
    /// <summary>
    /// Option names to configure analyzer execution through an .editorconfig file.
    /// </summary>
    internal static partial class EditorConfigOptionNames
    {
        // =============================================================================================================
        // NOTE: Keep this file in sync with documentation at '<%REPO_ROOT%>\docs\Analyzer Configuration.md'
        // =============================================================================================================

        /// <summary>
        /// Option to configure analyzed API surface.
        /// Allowed option values: One or more fields of flags enum <see cref="SymbolVisibilityGroup"/> as a comma separated list.
        /// </summary>
        public const string ApiSurface = "api_surface";

        /// <summary>
        /// Option to configure required modifiers for analyzed APIs.
        /// Allowed option values: One or more fields of flags enum <see cref="SymbolModifiers"/> as a comma separated list.
        /// </summary>
        public const string RequiredModifiers = "required_modifiers";

        /// <summary>
        /// Boolean option to exclude analysis of async void methods.
        /// </summary>
        public const string ExcludeAsyncVoidMethods = "exclude_async_void_methods";

        /// <summary>
        /// Option to configure analyzed output kinds, i.e. <see cref="Microsoft.CodeAnalysis.CompilationOptions.OutputKind"/> of the compilation.
        /// Allowed option values: One or more fields of <see cref="Microsoft.CodeAnalysis.CompilationOptions.OutputKind"/> as a comma separated list.
        /// </summary>
        public const string OutputKind = "output_kind";

        /// <summary>
        /// Boolean option to configure if single letter type parameter names are not flagged for CA1715 (https://docs.microsoft.com/visualstudio/code-quality/ca1715-identifiers-should-have-correct-prefix).
        /// </summary>
        public const string ExcludeSingleLetterTypeParameters = "exclude_single_letter_type_parameters";

        /// <summary>
        /// Integral option to configure sufficient IterationCount when using weak KDF algorithm.
        /// </summary>
        public const string SufficientIterationCountForWeakKDFAlgorithm = "sufficient_IterationCount_for_weak_KDF_algorithm";

        /// <summary>
        /// Boolean option to exclude analysis of 'this' parameter for extension methods.
        /// </summary>
        public const string ExcludeExtensionMethodThisParameter = "exclude_extension_method_this_parameter";

        /// <summary>
        /// String option to configure names of null check validation methods (separated by '|') that validate arguments passed to the method are non-null for CA1062 (https://docs.microsoft.com/visualstudio/code-quality/ca1062-validate-arguments-of-public-methods).
        /// Allowed method name formats:
        ///   1. Method name only (includes all methods with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
        ///      with an optional "M:" prefix.
        /// </summary>
        public const string NullCheckValidationMethods = "null_check_validation_methods";

        /// <summary>
        /// String option to configure names of additional string formatting methods (separated by '|') for CA2241 (https://docs.microsoft.com/visualstudio/code-quality/ca2241-provide-correct-arguments-to-formatting-methods).
        /// Allowed method name formats:
        ///   1. Method name only (includes all methods with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
        ///      with an optional "M:" prefix.
        /// </summary>
        public const string AdditionalStringFormattingMethods = "additional_string_formatting_methods";

        /// <summary>
        /// String option to configure names of symbols (separated by '|') that are excluded for analysis.
        /// Configurable rules: CA1303 (https://docs.microsoft.com/visualstudio/code-quality/ca1303-do-not-pass-literals-as-localized-parameters).
        /// Allowed method name formats:
        ///   1. Symbol name only (includes all symbols with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format.
        ///      Note that each symbol name requires a symbol kind prefix, such as "M:" prefix for methods, "T:" prefix for types, "N:" prefix for namespaces, etc.
        ///   3. ".ctor" for constructors and ".cctor" for static constructors
        /// </summary>
        public const string ExcludedSymbolNames = "excluded_symbol_names";

        /// <summary>
        /// String option to configure names of types (separated by '|'), so that the type and all its derived types are excluded for analysis.
        /// Configurable rules: CA1303 (https://docs.microsoft.com/visualstudio/code-quality/ca1303-do-not-pass-literals-as-localized-parameters).
        /// Allowed method name formats:
        ///   1. Type name only (includes all types with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
        ///      with an optional "T:" prefix.
        /// </summary>
        public const string ExcludedTypeNamesWithDerivedTypes = "excluded_type_names_with_derived_types";

        /// <summary>
        /// String option to configure names of symbols (separated by '|') that are disallowed in analysis.
        /// Configurable rules: CA1031 (https://docs.microsoft.com/visualstudio/code-quality/ca1031-do-not-catch-general-exception-types).
        /// Allowed method name formats:
        ///   1. Symbol name only (includes all symbols with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format.
        ///      Note that each symbol name requires a symbol kind prefix, such as "M:" prefix for methods, "T:" prefix for types, "N:" prefix for namespaces, etc.
        ///   3. ".ctor" for constructors and ".cctor" for static constructors
        /// </summary>
        public const string DisallowedSymbolNames = "disallowed_symbol_names";

        /// <summary>
        /// Enumeration option to configure unsafe DllImportSearchPath bits when using DefaultDllImportSearchPaths attribute.
        /// Do not use the OR operator to represent the bitwise combination of its member values, use the integeral value directly.
        /// </summary>
        public const string UnsafeDllImportSearchPathBits = "unsafe_DllImportSearchPath_bits";

        /// <summary>
        /// Boolean option to configure whether to exclude aspnet core mvc ControllerBase when considering CSRF.
        /// </summary>
        public const string ExcludeAspnetCoreMvcControllerBase = "exclude_aspnet_core_mvc_controllerbase";

        /// <summary>
        /// String option to configure how many enum values should be prefixed by the enum type name to trigger the rule.
        /// Configurable rules: CA1712 (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1712)
        /// Allowed method name formats:
        ///   1. Any of the enum values starts with the enum type name
        ///   2. All of the enum values starts with the enum type name
        ///   3. Default FxCop heuristic (75% of enum values)
        /// </summary>
        public const string EnumValuesPrefixTrigger = "enum_values_prefix_trigger";

        /// <summary>
        /// String option to configure names of types (separated by '|'), with their suffixes (separated by '->').
        /// Configurable rules: CA1710 (https://docs.microsoft.com/visualstudio/code-quality/ca1710-identifiers-should-have-correct-suffix).
        /// Allowed type name formats:
        ///   1. Type name only (includes all types with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
        ///      with an optional "T:" prefix.
        /// </summary>
        public const string AdditionalRequiredSuffixes = "additional_required_suffixes";

        /// <summary>
        /// Boolean option to prevent analyzing indirect base types (walking more than one level up) when suggesting suffixes.
        /// </summary>
        public const string ExcludeIndirectBaseTypes = "exclude_indirect_base_types";

        /// <summary>
        /// String option to configure names of interfaces (separated by '|'), with their required generic interfaces (separated by '->').
        /// Configurable rules: CA1010 (https://docs.microsoft.com/visualstudio/code-quality/ca1010)
        /// Allowed interface formats:
        ///   1. Interface name only(includes all interfaces with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format with an optional "T:" prefix.
        /// </summary>
        public const string AdditionalRequiredGenericInterfaces = "additional_required_generic_interfaces";

        /// <summary>
        /// Names of types or namespaces (separated by '|'), such that the type or type's namespace doesn't count in the inheritance hierarchy tree.
        /// Configurable rules: CA1501 (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1501)
        /// Allowed name formats:
        ///   1. Type or namespace name (includes all types with the name, regardless of the containing type or namespace and all types whose namespace contains the name)
        ///   2. Type or namespace name ending with a wildcard symbol (includes all types whose name starts with the given name, regardless of the containing type or namespace
        ///      and all types whose namespace contains the name)
        ///   3. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
        ///      with an optional "T:" prefix for types or "N:" prefix for namespaces.
        ///   4. Fully qualified wildcard names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
        ///      with an optional "T:" prefix for types or "N:" prefix for namespaces and ending with the wildcard symbol.
        /// </summary>
        public const string AdditionalInheritanceExcludedSymbolNames = "additional_inheritance_excluded_symbol_names";
    }
}
