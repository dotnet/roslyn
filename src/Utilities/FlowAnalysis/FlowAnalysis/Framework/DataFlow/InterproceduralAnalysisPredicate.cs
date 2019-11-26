// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Interprocedural analysis predicate to decide whether interprocedural analysis can be skipped.
    /// </summary>
    public sealed class InterproceduralAnalysisPredicate
    {
        private readonly Func<IMethodSymbol, bool>? _skipAnalysisForInvokedMethodPredicateOpt;
        private readonly Func<IMethodSymbol, bool>? _skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt;
        private readonly Func<IDataFlowAnalysisContext, bool>? _skipAnalysisForInvokedContextPredicateOpt;

        public InterproceduralAnalysisPredicate(
            Func<IMethodSymbol, bool>? skipAnalysisForInvokedMethodPredicateOpt,
            Func<IMethodSymbol, bool>? skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt,
            Func<IDataFlowAnalysisContext, bool>? skipAnalysisForInvokedContextPredicateOpt)
        {
            if (skipAnalysisForInvokedMethodPredicateOpt == null &&
                skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt == null &&
                skipAnalysisForInvokedContextPredicateOpt == null)
            {
                throw new ArgumentException("You must provide at least one non-null predicate argument");
            }

            _skipAnalysisForInvokedMethodPredicateOpt = skipAnalysisForInvokedMethodPredicateOpt;
            _skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt = skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt;
            _skipAnalysisForInvokedContextPredicateOpt = skipAnalysisForInvokedContextPredicateOpt;
        }

        public bool SkipInterproceduralAnalysis(IMethodSymbol invokedMethod, bool isLambdaOrLocalFunction)
        {
            var predicateOpt = isLambdaOrLocalFunction ?
                _skipAnalysisForInvokedLambdaOrLocalFunctionPredicateOpt :
                _skipAnalysisForInvokedMethodPredicateOpt;
            return predicateOpt?.Invoke(invokedMethod) == true;
        }

        public bool SkipInterproceduralAnalysis(IDataFlowAnalysisContext interproceduralAnalysisContext)
           => _skipAnalysisForInvokedContextPredicateOpt?.Invoke(interproceduralAnalysisContext) == true;
    }
}
