// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Boolean option to enable platform compatibility analyzer for TFMs with lower version than net5.0 (https://learn.microsoft.com/visualstudio/code-quality/ca1416).
        /// </summary>
        public const string EnablePlatformAnalyzerOnPreNet5Target = "enable_platform_analyzer_on_pre_net5_target";

        /// <summary>
        /// Option to configure analyzed output kinds, i.e. <see cref="Microsoft.CodeAnalysis.CompilationOptions.OutputKind"/> of the compilation.
        /// Allowed option values: One or more fields of <see cref="Microsoft.CodeAnalysis.CompilationOptions.OutputKind"/> as a comma separated list.
        /// </summary>
        public const string OutputKind = "output_kind";

        /// <summary>
        /// Boolean option to configure if single letter type parameter names are not flagged for CA1715 (https://learn.microsoft.com/visualstudio/code-quality/ca1715).
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
        /// String option to configure names of null check validation methods (separated by '|') that validate arguments passed to the method are non-null for CA1062 (https://learn.microsoft.com/visualstudio/code-quality/ca1062).
        /// Allowed method name formats:
        ///   1. Method name only (includes all methods with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format
        ///      with an optional "M:" prefix.
        /// </summary>
        public const string NullCheckValidationMethods = "null_check_validation_methods";

        /// <summary>
        /// String option to configure names of additional string formatting methods (separated by '|') for CA2241 (https://learn.microsoft.com/visualstudio/code-quality/ca2241).
        /// Allowed method name formats:
        ///   1. Method name only (includes all methods with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format
        ///      with an optional "M:" prefix.
        /// </summary>
        public const string AdditionalStringFormattingMethods = "additional_string_formatting_methods";

        /// <summary>
        /// Boolean option to enable heuristically detecting of additional string formatting methods for CA2241 (https://learn.microsoft.com/visualstudio/code-quality/ca2241).
        /// A method is considered a string formatting method if it has a '<see cref="string"/> <c>format</c>' parameter followed by a <see langword="params"/> <see cref="object"/>[]' parameter.
        /// The default value of this is <c>false</c>.
        /// </summary>
        public const string TryDetermineAdditionalStringFormattingMethodsAutomatically = "try_determine_additional_string_formatting_methods_automatically";

        /// <summary>
        /// String option to configure names of symbols (separated by '|') that are excluded for analysis.
        /// Configurable rules: CA1303 (https://learn.microsoft.com/visualstudio/code-quality/ca1303).
        /// Allowed method name formats:
        ///   1. Symbol name only (includes all symbols with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format.
        ///      Note that each symbol name requires a symbol kind prefix, such as "M:" prefix for methods, "T:" prefix for types, "N:" prefix for namespaces, etc.
        ///   3. ".ctor" for constructors and ".cctor" for static constructors
        /// </summary>
        public const string ExcludedSymbolNames = "excluded_symbol_names";

        /// <summary>
        /// String option to configure names of types (separated by '|'), so that the type and all its derived types are excluded for analysis.
        /// Configurable rules: CA1303 (https://learn.microsoft.com/visualstudio/code-quality/ca1303).
        /// Allowed method name formats:
        ///   1. Type name only (includes all types with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format
        ///      with an optional "T:" prefix.
        /// </summary>
        public const string ExcludedTypeNamesWithDerivedTypes = "excluded_type_names_with_derived_types";

        /// <summary>
        /// String option to configure names of symbols (separated by '|') that are disallowed in analysis.
        /// Configurable rules: CA1031 (https://learn.microsoft.com/visualstudio/code-quality/ca1031).
        /// Allowed method name formats:
        ///   1. Symbol name only (includes all symbols with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format.
        ///      Note that each symbol name requires a symbol kind prefix, such as "M:" prefix for methods, "T:" prefix for types, "N:" prefix for namespaces, etc.
        ///   3. ".ctor" for constructors and ".cctor" for static constructors
        /// </summary>
        public const string DisallowedSymbolNames = "disallowed_symbol_names";

        /// <summary>
        /// Enumeration option to configure unsafe DllImportSearchPath bits when using DefaultDllImportSearchPaths attribute.
        /// Do not use the OR operator to represent the bitwise combination of its member values, use the integral value directly.
        /// </summary>
        public const string UnsafeDllImportSearchPathBits = "unsafe_DllImportSearchPath_bits";

        /// <summary>
        /// Boolean option to configure whether to exclude aspnet core mvc ControllerBase when considering CSRF.
        /// </summary>
        public const string ExcludeAspnetCoreMvcControllerBase = "exclude_aspnet_core_mvc_controllerbase";

        /// <summary>
        /// String option to configure how many enum values should be prefixed by the enum type name to trigger the rule.
        /// Configurable rules: CA1712 (https://learn.microsoft.com/visualstudio/code-quality/ca1712)
        /// Allowed method name formats:
        ///   1. Any of the enum values starts with the enum type name
        ///   2. All of the enum values starts with the enum type name
        ///   3. Default FxCop heuristic (75% of enum values)
        /// </summary>
        public const string EnumValuesPrefixTrigger = "enum_values_prefix_trigger";

        /// <summary>
        /// String option to configure names of types (separated by '|'), with their suffixes (separated by '->').
        /// Configurable rules: CA1710 (https://learn.microsoft.com/visualstudio/code-quality/ca1710).
        /// Allowed type name formats:
        ///   1. Type name only (includes all types with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format
        ///      with an optional "T:" prefix.
        /// </summary>
        public const string AdditionalRequiredSuffixes = "additional_required_suffixes";

        /// <summary>
        /// Boolean option to prevent analyzing indirect base types (walking more than one level up) when suggesting suffixes.
        /// </summary>
        public const string ExcludeIndirectBaseTypes = "exclude_indirect_base_types";

        /// <summary>
        /// String option to configure names of interfaces (separated by '|'), with their required generic interfaces (separated by '->').
        /// Configurable rules: CA1010 (https://learn.microsoft.com/visualstudio/code-quality/ca1010)
        /// Allowed interface formats:
        ///   1. Interface name only(includes all interfaces with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format with an optional "T:" prefix.
        /// </summary>
        public const string AdditionalRequiredGenericInterfaces = "additional_required_generic_interfaces";

        /// <summary>
        /// Names of types or namespaces (separated by '|'), such that the type or type's namespace doesn't count in the inheritance hierarchy tree.
        /// Configurable rules: CA1501 (https://learn.microsoft.com/visualstudio/code-quality/ca1501)
        /// Allowed name formats:
        ///   1. Type or namespace name (includes all types with the name, regardless of the containing type or namespace and all types whose namespace contains the name)
        ///   2. Type or namespace name ending with a wildcard symbol (includes all types whose name starts with the given name, regardless of the containing type or namespace
        ///      and all types whose namespace contains the name)
        ///   3. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format with an optional "T:" prefix for types or "N:" prefix for namespaces. (includes all types with the exact type match or the exact containing namespace match)
        ///   4. Fully qualified type or namespace name with an optional "T:" prefix for type or "N:" prefix for namespace and ending with the wildcard symbol (includes all types whose fully qualified name starts with the given suffix)
        /// </summary>
        public const string AdditionalInheritanceExcludedSymbolNames = "additional_inheritance_excluded_symbol_names";

        /// <summary>
        /// Option to configure analyzed symbol kinds, i.e. <see cref="Microsoft.CodeAnalysis.SymbolKind"/>.
        /// Allowed option values: One or more fields of <see cref="Microsoft.CodeAnalysis.SymbolKind"/> as a comma separated list.
        /// </summary>
        public const string AnalyzedSymbolKinds = "analyzed_symbol_kinds";

        /// <summary>
        /// Boolean option to configure if the naming heuristic should be used for CA1303 (https://learn.microsoft.com/visualstudio/code-quality/ca1303).
        /// </summary>
        public const string UseNamingHeuristic = "use_naming_heuristic";

        /// <summary>
        /// String option to configure names of additional methods (separated by '|') for CA1806 (https://learn.microsoft.com/visualstudio/code-quality/ca1806).
        /// Allowed method name formats:
        ///   1. Method name only (includes all methods with the name, regardless of the containing type or namespace)
        ///   2. Fully qualified names in the symbol's documentation ID format: https://github.com/dotnet/csharplang/blob/main/spec/documentation-comments.md#id-string-format
        ///      with an optional "M:" prefix.
        /// </summary>
        public const string AdditionalUseResultsMethods = "additional_use_results_methods";

        /// <summary>
        /// String option to configure allowed suffixed (separated by '|').
        /// Configurable rule: CA1711 (https://learn.microsoft.com/visualstudio/code-quality/ca1711).
        /// </summary>
        public const string AllowedSuffixes = "allowed_suffixes";

        /// <summary>
        /// Boolean option to configure whether to exclude structs when considering public fields.
        /// </summary>
        public const string ExcludeStructs = "exclude_structs";

        /// <summary>
        /// Boolean option to configure whether to exclude 'FirstOrDefault' and 'LastOrDefault' methods for
        /// CA1826 (Do not use Enumerable methods on indexable collections. Instead use the collection directly).
        /// </summary>
        public const string ExcludeOrDefaultMethods = "exclude_ordefault_methods";

        /// <summary>
        /// String option to configure names of method symbols (separated by '|') that marks all of the parameters with IEnumerable type
        /// would be enumerated.
        /// Configurable rule: CA1851 (https://learn.microsoft.com/visualstudio/code-quality/ca1851).
        /// </summary>
        public const string EnumerationMethods = "enumeration_methods";

        /// <summary>
        /// String option to configure names of method symbols (separated by '|') that accepting parameter with IEnumerable type and return a new IEnumerable type, like 'Select' and 'Where'.
        /// Configurable rule: CA1851 (https://learn.microsoft.com/visualstudio/code-quality/ca1851).
        /// </summary>
        public const string LinqChainMethods = "linq_chain_methods";

        /// <summary>
        /// Boolean option to configure the assumption that IEnumerable type parameters would be enumerated by method invocation or not.
        /// It does not affect linq_chain_methods.
        /// Configurable rule: CA1851 (https://learn.microsoft.com/visualstudio/code-quality/ca1851).
        /// </summary>
        public const string AssumeMethodEnumeratesParameters = "assume_method_enumerates_parameters";

        /// <summary>
        /// String option to configure names of additional "None" enum case (separated by '|') for CA1008.
        /// </summary>
        public const string AdditionalEnumNoneNames = "additional_enum_none_names";

        /// <summary>
        /// Boolean option whether to perform the analysis even if the assembly exposes its internals.
        /// </summary>
        public const string IgnoreInternalsVisibleTo = "ignore_internalsvisibleto";

        /// <summary>
        /// Boolean option to exclude generated code from analysis by the BannedApiAnalyzer.
        /// Configurable rule: RS0030 (https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md).
        /// </summary>
        public const string BannedApiExcludeGeneratedCode = "banned_api_exclude_generated_code";
    }
}
