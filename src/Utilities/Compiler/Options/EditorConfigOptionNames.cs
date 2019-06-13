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
    }
}
