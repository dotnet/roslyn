// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
    internal abstract class DoNotCatchGeneralUnlessRethrownAnalyzer : DiagnosticAnalyzer
    {
        private readonly bool _shouldCheckLambdas;
        private readonly string? _enablingMethodAttributeFullyQualifiedName;
        private readonly bool _allowExcludedSymbolNames;

        private bool RequiresAttributeOnMethod => !string.IsNullOrEmpty(_enablingMethodAttributeFullyQualifiedName);

        protected DoNotCatchGeneralUnlessRethrownAnalyzer(bool shouldCheckLambdas, string? enablingMethodAttributeFullyQualifiedName = null,
            bool allowExcludedSymbolNames = false)
        {
            _shouldCheckLambdas = shouldCheckLambdas;
            _enablingMethodAttributeFullyQualifiedName = enablingMethodAttributeFullyQualifiedName;
            _allowExcludedSymbolNames = allowExcludedSymbolNames;
        }

        protected abstract Diagnostic CreateDiagnostic(IMethodSymbol containingMethod, SyntaxToken catchKeyword);
        protected virtual bool IsConfiguredDisallowedExceptionType(INamedTypeSymbol namedTypeSymbol, IMethodSymbol containingMethod, Compilation compilation, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
        {
            return false;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                INamedTypeSymbol? requiredAttributeType = null;
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

                    if (RequiresAttributeOnMethod && !method.HasAnyAttribute(requiredAttributeType))
                    {
                        return;
                    }

                    if (_allowExcludedSymbolNames &&
                        AnalyzerOptionsExtensions.IsConfiguredToSkipAnalysis(operationBlockAnalysisContext.Options, SupportedDiagnostics[0], method, operationBlockAnalysisContext.Compilation))
                    {
                        return;
                    }

                    foreach (var operation in operationBlockAnalysisContext.OperationBlocks)
                    {
                        var walker = new DisallowGeneralCatchUnlessRethrowWalker(IsDisallowedCatchType, _shouldCheckLambdas);
                        walker.Visit(operation);

                        foreach (var catchClause in walker.CatchClausesForDisallowedTypesWithoutRethrow)
                        {
                            operationBlockAnalysisContext.ReportDiagnostic(CreateDiagnostic(method, catchClause.Syntax.GetFirstToken()));
                        }
                    }

                    bool IsDisallowedCatchType(INamedTypeSymbol type) =>
                        disallowedCatchTypes.Contains(type) ||
                        IsConfiguredDisallowedExceptionType(type, method, compilationStartAnalysisContext.Compilation,
                            compilationStartAnalysisContext.Options, compilationStartAnalysisContext.CancellationToken);
                });
            });
        }

        private INamedTypeSymbol? GetRequiredAttributeType(Compilation compilation)
        {
            RoslynDebug.Assert(_enablingMethodAttributeFullyQualifiedName != null);
            return compilation.GetOrCreateTypeByMetadataName(_enablingMethodAttributeFullyQualifiedName);
        }

        private static IReadOnlyCollection<INamedTypeSymbol> GetDisallowedCatchTypes(Compilation compilation)
        {
            return ImmutableHashSet.CreateRange(
                new[] {
                    compilation.GetSpecialType(SpecialType.System_Object),
                    compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemException),
                    compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemSystemException)
                }.WhereNotNull());
        }

        /// <summary>
        /// Walks an IOperation tree to find catch blocks that handle general types without rethrowing them.
        /// </summary>
        private sealed class DisallowGeneralCatchUnlessRethrowWalker : OperationWalker
        {
            private readonly Func<INamedTypeSymbol, bool> _isDisallowedCatchType;
            private readonly bool _checkAnonymousFunctions;
            private readonly Stack<bool> _seenRethrowInCatchClauses = new();

            public ISet<ICatchClauseOperation> CatchClausesForDisallowedTypesWithoutRethrow { get; } = new HashSet<ICatchClauseOperation>();

            public DisallowGeneralCatchUnlessRethrowWalker(Func<INamedTypeSymbol, bool> isDisallowedCatchType, bool checkAnonymousFunctions)
            {
                _isDisallowedCatchType = isDisallowedCatchType;
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

                if (!seenRethrow && IsDisallowedCatch(operation) && !MightBeFilteringBasedOnTheCaughtException(operation))
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

            private bool IsDisallowedCatch(ICatchClauseOperation operation)
            {
                return operation.ExceptionType is INamedTypeSymbol exceptionType &&
                    _isDisallowedCatchType(exceptionType);
            }

            private static bool MightBeFilteringBasedOnTheCaughtException(ICatchClauseOperation operation)
            {
                return operation is { ExceptionDeclarationOrExpression: not null, Filter: not null };
            }
        }
    }
}
