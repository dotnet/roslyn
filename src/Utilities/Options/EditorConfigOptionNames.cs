// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities
{
    /// <summary>
    /// Option names to configure analyzer execution through an .editorconfig file.
    /// </summary>
    public static class EditorConfigOptionNames
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
        /// Boolean option to skip analysis of async void methods.
        /// </summary>
        public const string SkipAsyncVoidMethods = "skip_async_void_methods";

        /// <summary>
        /// Option to configure analyzed output kinds, i.e. <see cref="Microsoft.CodeAnalysis.CompilationOptions.OutputKind"/> of the compilation.
        /// Allowed option values: One or more fields of <see cref="Microsoft.CodeAnalysis.CompilationOptions.OutputKind"/> as a comma separated list.
        /// </summary>
        public const string OutputKind = "output_kind";

        #region Dataflow analysis options

        /// <summary>
        /// Option to configure interprocedural dataflow analysis kind, i.e. <see cref="Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.InterproceduralAnalysisKind"/>.
        /// Allowed option values: Fields from <see cref="Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.InterproceduralAnalysisKind"/>.
        /// </summary>
        public const string InterproceduralAnalysisKind = "interprocedural_analysis_kind";

        /// <summary>
        /// Integral option to configure maximum method call chain for interprocedural dataflow analysis.
        /// </summary>
        public const string MaxInterproceduralMethodCallChain = "max_interprocedural_method_call_chain";

        /// <summary>
        /// Integral option to configure maximum lambda or local function call chain for interprocedural dataflow analysis.
        /// </summary>
        public const string MaxInterproceduralLambdaOrLocalFunctionCallChain = "max_interprocedural_lambda_or_local_function_call_chain";

        #endregion
    }
}
