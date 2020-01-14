// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <remarks>
    /// This type exists to share code between UsingStatementBinder and LockBinder.
    /// </remarks>
    internal abstract class LockOrUsingBinder : LocalScopeBinder
    {
        private ImmutableHashSet<Symbol> _lazyLockedOrDisposedVariables;
        private ExpressionAndDiagnostics _lazyExpressionAndDiagnostics;

        internal LockOrUsingBinder(Binder enclosing)
            : base(enclosing)
        {
        }

        protected abstract ExpressionSyntax TargetExpressionSyntax { get; }

        internal sealed override ImmutableHashSet<Symbol> LockedOrDisposedVariables
        {
            get
            {
                if (_lazyLockedOrDisposedVariables == null)
                {
                    ImmutableHashSet<Symbol> lockedOrDisposedVariables = this.Next.LockedOrDisposedVariables;

                    ExpressionSyntax targetExpressionSyntax = TargetExpressionSyntax;

                    if (targetExpressionSyntax != null)
                    {
                        // We rely on this in the GetBinder call below.
                        Debug.Assert(targetExpressionSyntax.Parent.Kind() == SyntaxKind.LockStatement ||
                                     targetExpressionSyntax.Parent.Kind() == SyntaxKind.UsingStatement);

                        // For some reason, dev11 only warnings about locals and parameters.  If you do the same thing
                        // with a field of a local or parameter (e.g. lock(p.x)), there's no warning when you modify
                        // the local/parameter or its field.  We're going to take advantage of this restriction to break
                        // a cycle: if the expression contains any lvalues, the binder is going to check LockedOrDisposedVariables,
                        // which is going to bind the expression, which is going to check LockedOrDisposedVariables, etc.
                        // Fortunately, SyntaxKind.IdentifierName includes local and parameter accesses, but no expressions
                        // that require lvalue checks.
                        if (targetExpressionSyntax.Kind() == SyntaxKind.IdentifierName)
                        {
                            BoundExpression expression = BindTargetExpression(diagnostics: null, // Diagnostics reported by BindUsingStatementParts.
                                                                              originalBinder: GetBinder(targetExpressionSyntax.Parent));

                            switch (expression.Kind)
                            {
                                case BoundKind.Local:
                                    lockedOrDisposedVariables = lockedOrDisposedVariables.Add(((BoundLocal)expression).LocalSymbol);
                                    break;
                                case BoundKind.Parameter:
                                    lockedOrDisposedVariables = lockedOrDisposedVariables.Add(((BoundParameter)expression).ParameterSymbol);
                                    break;
                            }
                        }
                    }

                    Interlocked.CompareExchange(ref _lazyLockedOrDisposedVariables, lockedOrDisposedVariables, null);
                }
                Debug.Assert(_lazyLockedOrDisposedVariables != null);
                return _lazyLockedOrDisposedVariables;
            }
        }

        protected BoundExpression BindTargetExpression(DiagnosticBag diagnostics, Binder originalBinder, TypeSymbol targetTypeOpt = null)
        {
            if (_lazyExpressionAndDiagnostics == null)
            {
                // Filter out method group in conversion.
                DiagnosticBag expressionDiagnostics = DiagnosticBag.GetInstance();
                BoundExpression boundExpression = originalBinder.BindValue(TargetExpressionSyntax, expressionDiagnostics, Binder.BindValueKind.RValueOrMethodGroup);
                if (targetTypeOpt is object)
                {
                    boundExpression = originalBinder.GenerateConversionForAssignment(targetTypeOpt, boundExpression, expressionDiagnostics);
                }
                else
                {
                    boundExpression = originalBinder.BindToNaturalType(boundExpression, diagnostics);
                }
                Interlocked.CompareExchange(ref _lazyExpressionAndDiagnostics, new ExpressionAndDiagnostics(boundExpression, expressionDiagnostics.ToReadOnlyAndFree()), null);
            }
            Debug.Assert(_lazyExpressionAndDiagnostics != null);

            if (diagnostics != null)
            {
                diagnostics.AddRange(_lazyExpressionAndDiagnostics.Diagnostics);
            }

            return _lazyExpressionAndDiagnostics.Expression;
        }

        /// <remarks>
        /// This class exists so these two fields can be set atomically.
        /// CONSIDER: If this causes too many allocations, we could use start and end flags plus spinlocking
        /// as for completion parts.
        /// </remarks>
        private class ExpressionAndDiagnostics
        {
            public readonly BoundExpression Expression;
            public readonly ImmutableArray<Diagnostic> Diagnostics;

            public ExpressionAndDiagnostics(BoundExpression expression, ImmutableArray<Diagnostic> diagnostics)
            {
                this.Expression = expression;
                this.Diagnostics = diagnostics;
            }
        }
    }
}
