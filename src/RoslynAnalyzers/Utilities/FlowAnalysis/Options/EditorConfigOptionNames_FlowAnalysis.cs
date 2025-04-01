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

        #region Flow analysis options

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

        /// <summary>
        /// String option to configure dispose analysis kind, primarily for CA2000 (DisposeObjectsBeforeLosingScope).
        /// Allowed option values: Fields from DisposeAnalysisKind enum.
        /// </summary>
        public const string DisposeAnalysisKind = "dispose_analysis_kind";

        /// <summary>
        /// Boolean option to configure if passing a disposable object as a constructor argument should be considered
        /// as a dispose ownership transfer, primarily for CA2000 (DisposeObjectsBeforeLosingScope).
        /// </summary>
        public const string DisposeOwnershipTransferAtConstructor = "dispose_ownership_transfer_at_constructor";

        /// <summary>
        /// Boolean option to configure if passing a disposable object as an argument to a method invocation should be considered
        /// as a dispose ownership transfer to the caller, primarily for CA2000 (DisposeObjectsBeforeLosingScope)
        /// </summary>
        public const string DisposeOwnershipTransferAtMethodCall = "dispose_ownership_transfer_at_method_call";

        /// <summary>
        /// Option to configure whether copy analysis should be executed during dataflow analysis.
        /// Copy analysis tracks value and reference copies. For example,
        /// <code>
        ///     void M(string str1, string str2)
        ///     {
        ///         if (str1 != null)
        ///         {
        ///             if (str1 == str2)
        ///             {
        ///                 if (str2 != null) // This is redundant as 'str1' and 'str2' are value copies on this code path. This requires copy analysis.
        ///                 {
        ///                 }
        ///             }
        ///         }
        ///     }
        /// </code>
        /// </summary>
        public const string CopyAnalysis = "copy_analysis";

        /// <summary>
        /// Option to configure points to analysis kind, i.e. <see cref="Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis.PointsToAnalysisKind"/>.
        /// Allowed option values: Fields from <see cref="Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis.PointsToAnalysisKind"/>.
        /// </summary>
        public const string PointsToAnalysisKind = "points_to_analysis_kind";

        #endregion
    }
}
