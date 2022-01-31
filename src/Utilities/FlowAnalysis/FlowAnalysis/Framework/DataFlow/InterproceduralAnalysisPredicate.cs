// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Interprocedural analysis predicate to decide whether interprocedural analysis can be skipped.
    /// </summary>
    public sealed class InterproceduralAnalysisPredicate
    {
        private readonly Func<IMethodSymbol, bool>? _skipAnalysisForInvokedMethodPredicate;
        private readonly Func<IMethodSymbol, bool>? _skipAnalysisForInvokedLambdaOrLocalFunctionPredicate;
        private readonly Func<IDataFlowAnalysisContext, bool>? _skipAnalysisForInvokedContextPredicate;

        public InterproceduralAnalysisPredicate(
            Func<IMethodSymbol, bool>? skipAnalysisForInvokedMethodPredicate,
            Func<IMethodSymbol, bool>? skipAnalysisForInvokedLambdaOrLocalFunctionPredicate,
            Func<IDataFlowAnalysisContext, bool>? skipAnalysisForInvokedContextPredicate)
        {
            if (skipAnalysisForInvokedMethodPredicate == null &&
                skipAnalysisForInvokedLambdaOrLocalFunctionPredicate == null &&
                skipAnalysisForInvokedContextPredicate == null)
            {
                throw new ArgumentException("You must provide at least one non-null predicate argument");
            }

            _skipAnalysisForInvokedMethodPredicate = skipAnalysisForInvokedMethodPredicate;
            _skipAnalysisForInvokedLambdaOrLocalFunctionPredicate = skipAnalysisForInvokedLambdaOrLocalFunctionPredicate;
            _skipAnalysisForInvokedContextPredicate = skipAnalysisForInvokedContextPredicate;
        }

        public bool SkipInterproceduralAnalysis(IMethodSymbol invokedMethod, bool isLambdaOrLocalFunction)
        {
            var predicate = isLambdaOrLocalFunction ?
                _skipAnalysisForInvokedLambdaOrLocalFunctionPredicate :
                _skipAnalysisForInvokedMethodPredicate;
            return predicate?.Invoke(invokedMethod) == true;
        }

        public bool SkipInterproceduralAnalysis(IDataFlowAnalysisContext interproceduralAnalysisContext)
           => _skipAnalysisForInvokedContextPredicate?.Invoke(interproceduralAnalysisContext) == true;
    }
}
