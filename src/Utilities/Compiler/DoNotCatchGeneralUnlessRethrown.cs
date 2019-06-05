// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities
{
    internal abstract class DoNotCatchGeneralUnlessRethrownAnalyzer : DiagnosticAnalyzer
    {
        private readonly bool _shouldCheckLambdas;
        private readonly string _enablingMethodAttributeFullyQualifiedName;

        private bool RequiresAttributeOnMethod => !string.IsNullOrEmpty(_enablingMethodAttributeFullyQualifiedName);

        protected DoNotCatchGeneralUnlessRethrownAnalyzer(bool shouldCheckLambdas, string enablingMethodAttributeFullyQualifiedName = null)
        {
            _shouldCheckLambdas = shouldCheckLambdas;
            _enablingMethodAttributeFullyQualifiedName = enablingMethodAttributeFullyQualifiedName;
        }

        protected abstract Diagnostic CreateDiagnostic(IMethodSymbol containingMethod, SyntaxToken catchKeyword);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            analysisContext.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                INamedTypeSymbol requiredAttributeType = null;
                if (RequiresAttributeOnMethod && (requiredAttributeType = GetRequiredAttributeType(compilationStartAnalysisContext.Compilation)) == null)
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

                    if (RequiresAttributeOnMethod && !MethodHasAttribute(method, requiredAttributeType))
                    {
                        return;
                    }

                    foreach (var operation in operationBlockAnalysisContext.OperationBlocks)
                    {
                        var walker = new DisallowGeneralCatchUnlessRethrowWalker(disallowedCatchTypes, _shouldCheckLambdas);
                        walker.Visit(operation);

                        foreach (var catchClause in walker.CatchClausesForDisallowedTypesWithoutRethrow)
                        {
                            operationBlockAnalysisContext.ReportDiagnostic(CreateDiagnostic(method, catchClause.Syntax.GetFirstToken()));
                        }
                    }
                });
            });
        }

        private INamedTypeSymbol GetRequiredAttributeType(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(_enablingMethodAttributeFullyQualifiedName);
        }

        private bool MethodHasAttribute(IMethodSymbol method, INamedTypeSymbol attributeType)
        {
            return method.GetAttributes().Any(attribute => attribute.AttributeClass.Equals(attributeType));
        }

        private static IReadOnlyCollection<INamedTypeSymbol> GetDisallowedCatchTypes(Compilation compilation)
        {
            return ImmutableHashSet.CreateRange(
                new[] {
                    WellKnownTypes.Object(compilation),
                    WellKnownTypes.Exception(compilation),
                    WellKnownTypes.SystemException(compilation)
                }.Where(x => x != null));
        }

        /// <summary>
        /// Walks an IOperation tree to find catch blocks that handle general types without rethrowing them.
        /// </summary>
        private class DisallowGeneralCatchUnlessRethrowWalker : OperationWalker
        {
            private readonly IReadOnlyCollection<INamedTypeSymbol> _disallowedCatchTypes;
            private readonly bool _checkAnonymousFunctions;
            private readonly Stack<bool> _seenRethrowInCatchClauses = new Stack<bool>();

            public ISet<ICatchClauseOperation> CatchClausesForDisallowedTypesWithoutRethrow { get; } = new HashSet<ICatchClauseOperation>();

            public DisallowGeneralCatchUnlessRethrowWalker(IReadOnlyCollection<INamedTypeSymbol> disallowedCatchTypes, bool checkAnonymousFunctions)
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
                _seenRethrowInCatchClauses.Push(false);

                Visit(operation.Filter);
                Visit(operation.Handler);

                bool seenRethrow = _seenRethrowInCatchClauses.Pop();

                if (!seenRethrow && IsCatchTooGeneral(operation) && !MightBeFilteringBasedOnTheCaughtException(operation))
                {
                    CatchClausesForDisallowedTypesWithoutRethrow.Add(operation);
                }
            }

            public override void VisitThrow(IThrowOperation operation)
            {
                if (_seenRethrowInCatchClauses.Count > 0 && !_seenRethrowInCatchClauses.Peek())
                {
                    _seenRethrowInCatchClauses.Pop();
                    _seenRethrowInCatchClauses.Push(true);
                }

                base.VisitThrow(operation);
            }

            private bool IsCatchTooGeneral(ICatchClauseOperation operation)
            {
                return _disallowedCatchTypes.Any(type => operation.ExceptionType.Equals(type));
            }

            private static bool MightBeFilteringBasedOnTheCaughtException(ICatchClauseOperation operation)
            {
                return operation.ExceptionDeclarationOrExpression != null && operation.Filter != null;
            }
        }
    }
}

#endif
