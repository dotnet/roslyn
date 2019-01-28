// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities
{
    public abstract class DoNotCatchGeneralUnlessRethrownAnalyzer : DiagnosticAnalyzer
    {
        private readonly bool _shouldCheckLambdas;

        protected DoNotCatchGeneralUnlessRethrownAnalyzer(bool shouldCheckLambdas)
        {
            _shouldCheckLambdas = shouldCheckLambdas;
        }

        protected virtual bool ShouldCheckCompilationUnit(Compilation compilation)
        {
            return true;
        }

        protected virtual bool ShouldCheckMethod(Compilation compilation, IMethodSymbol method)
        {
            return true;
        }

        protected abstract Diagnostic CreateDiagnostic(IMethodSymbol containingMethod, SyntaxNode catchNode);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            analysisContext.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                if (!ShouldCheckCompilationUnit(compilationStartAnalysisContext.Compilation))
                {
                    return;
                }

                var disallowedCatchTypes = GetDisallowedCatchTypes(compilationStartAnalysisContext.Compilation);

                compilationStartAnalysisContext.RegisterOperationBlockAction(operationBlockAnalysisContext =>
                {
                    if (operationBlockAnalysisContext.OwningSymbol.Kind != SymbolKind.Method)
                    {
                        return;
                    }

                    var method = (IMethodSymbol)operationBlockAnalysisContext.OwningSymbol;

                    if (!ShouldCheckMethod(operationBlockAnalysisContext.Compilation, method))
                    {
                        return;
                    }

                    foreach (var operation in operationBlockAnalysisContext.OperationBlocks)
                    {
                        var walker = new DisallowGeneralCatchUnlessRethrowWalker(disallowedCatchTypes, _shouldCheckLambdas);
                        walker.Visit(operation);

                        foreach (var catchClause in walker.CatchClausesForDisallowedTypesWithoutRethrow)
                        {
                            operationBlockAnalysisContext.ReportDiagnostic(CreateDiagnostic(method, catchClause.Syntax));
                        }
                    }
                });
            });
        }

        private static ICollection<INamedTypeSymbol> GetDisallowedCatchTypes(Compilation compilation)
        {
            var disallowed = new List<INamedTypeSymbol>();
            disallowed.Add(WellKnownTypes.Object(compilation));
            disallowed.Add(WellKnownTypes.Exception(compilation));
            disallowed.Add(WellKnownTypes.SystemException(compilation));
            return disallowed;
        }

        /// <summary>
        /// Walks an IOperation tree to find catch blocks that handle general types without rethrowing them.
        /// </summary>
        private class DisallowGeneralCatchUnlessRethrowWalker : OperationWalker
        {
            private readonly ICollection<INamedTypeSymbol> _disallowedCatchTypes;
            private readonly bool _checkAnonymousFunctions;
            private readonly Stack<bool> _seenRethrowInCatchClauses = new Stack<bool>();

            public ISet<ICatchClauseOperation> CatchClausesForDisallowedTypesWithoutRethrow { get; } = new HashSet<ICatchClauseOperation>();

            public DisallowGeneralCatchUnlessRethrowWalker(ICollection<INamedTypeSymbol> disallowedCatchTypes, bool checkAnonymousFunctions)
            {
                _disallowedCatchTypes = disallowedCatchTypes;
                _checkAnonymousFunctions = checkAnonymousFunctions;
            }

            public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
            {
                if (_checkAnonymousFunctions)
                {
                    base.VisitAnonymousFunction(operation);
                }
            }

            public override void VisitCatchClause(ICatchClauseOperation operation)
            {
                // No diagnostic if an exception filter is specified
                if (operation.Filter == null)
                {
                    _seenRethrowInCatchClauses.Push(false);

                    Visit(operation.Filter);
                    Visit(operation.Handler);

                    bool seenRethrow = _seenRethrowInCatchClauses.Pop();

                    if (IsCaughtTypeDisallowed(operation.ExceptionType) && !seenRethrow)
                    {
                        CatchClausesForDisallowedTypesWithoutRethrow.Add(operation);
                    }
                }
            }

            public override void VisitThrow(IThrowOperation operation)
            {
                if (operation.Exception == null && _seenRethrowInCatchClauses.Count > 0 && !_seenRethrowInCatchClauses.Peek())
                {
                    _seenRethrowInCatchClauses.Pop();
                    _seenRethrowInCatchClauses.Push(true);
                }

                base.VisitThrow(operation);
            }

            private bool IsCaughtTypeDisallowed(ITypeSymbol caughtType)
            {
                return caughtType == null || _disallowedCatchTypes.Any(type => caughtType == type);
            }
        }
    }
}
