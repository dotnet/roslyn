// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts StatementSyntax nodes into BoundStatements
    /// </summary>
    internal partial class Binder
    {
        /// <summary>
        /// This is the set of parameters and local variables that were used as arguments to 
        /// lock or using statements in enclosing scopes.
        /// </summary>
        /// <remarks>
        /// using (x) { } // x counts
        /// using (IDisposable y = null) { } // y does not count
        /// </remarks>
        internal virtual ImmutableHashSet<Symbol> LockedOrDisposedVariables
        {
            get { return _next.LockedOrDisposedVariables; }
        }

        /// <remarks>
        /// Noteworthy override is in MemberSemanticModel.IncrementalBinder (used for caching).
        /// </remarks>
        public virtual BoundStatement BindStatement(StatementSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            BoundStatement result;
            switch (node.Kind())
            {
                case SyntaxKind.Block:
                    result = BindBlock((BlockSyntax)node, diagnostics);
                    break;
                case SyntaxKind.LocalDeclarationStatement:
                    result = BindLocalDeclarationStatement((LocalDeclarationStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.LocalFunctionStatement:
                    result = BindLocalFunctionStatement((LocalFunctionStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.ExpressionStatement:
                    result = BindExpressionStatement((ExpressionStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.IfStatement:
                    result = BindIfStatement((IfStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.SwitchStatement:
                    result = BindSwitchStatement((SwitchStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.DoStatement:
                    result = BindDo((DoStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.WhileStatement:
                    result = BindWhile((WhileStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.ForStatement:
                    result = BindFor((ForStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    result = BindForEach((CommonForEachStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.BreakStatement:
                    result = BindBreak((BreakStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.ContinueStatement:
                    result = BindContinue((ContinueStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.ReturnStatement:
                    result = BindReturn((ReturnStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.FixedStatement:
                    result = BindFixedStatement((FixedStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.LabeledStatement:
                    result = BindLabeled((LabeledStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.GotoStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoDefaultStatement:
                    result = BindGoto((GotoStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.TryStatement:
                    result = BindTryStatement((TryStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.EmptyStatement:
                    result = BindEmpty((EmptyStatementSyntax)node);
                    break;
                case SyntaxKind.ThrowStatement:
                    result = BindThrow((ThrowStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.UnsafeStatement:
                    result = BindUnsafeStatement((UnsafeStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.UncheckedStatement:
                case SyntaxKind.CheckedStatement:
                    result = BindCheckedStatement((CheckedStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.UsingStatement:
                    result = BindUsingStatement((UsingStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.YieldBreakStatement:
                    result = BindYieldBreakStatement((YieldStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.YieldReturnStatement:
                    result = BindYieldReturnStatement((YieldStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.LockStatement:
                    result = BindLockStatement((LockStatementSyntax)node, diagnostics);
                    break;
                default:
                    // NOTE: We could probably throw an exception here, but it's conceivable
                    // that a non-parser syntax tree could reach this point with an unexpected
                    // SyntaxKind and we don't want to throw if that occurs.
                    result = new BoundBadStatement(node, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                    break;
            }

            BoundBlock block;
            Debug.Assert(result.WasCompilerGenerated == false ||
                         (result.Kind == BoundKind.Block &&
                          (block = (BoundBlock)result).Statements.Length == 1 &&
                          block.Statements.Single().WasCompilerGenerated == false), "Synthetic node would not get cached");

            Debug.Assert(result.Syntax is StatementSyntax, "BoundStatement should be associated with a statement syntax.");

            Debug.Assert(System.Linq.Enumerable.Contains(result.Syntax.AncestorsAndSelf(), node), @"Bound statement (or one of its parents) 
                                                                            should have same syntax as the given syntax node. 
                                                                            Otherwise it may be confusing to the binder cache that uses syntax node as keys.");

            return result;
        }

        private BoundStatement BindCheckedStatement(CheckedStatementSyntax node, DiagnosticBag diagnostics)
        {
            return BindEmbeddedBlock(node.Block, diagnostics);
        }

        private BoundStatement BindUnsafeStatement(UnsafeStatementSyntax node, DiagnosticBag diagnostics)
        {
            var unsafeBinder = this.GetBinder(node);

            if (!this.Compilation.Options.AllowUnsafe)
            {
                Error(diagnostics, ErrorCode.ERR_IllegalUnsafe, node.UnsafeKeyword);
            }
            else if (this.IsIndirectlyInIterator) // called *after* we know the binder map has been created.
            {
                // Spec 8.2: "An iterator block always defines a safe context, even when its declaration
                // is nested in an unsafe context."
                Error(diagnostics, ErrorCode.ERR_IllegalInnerUnsafe, node.UnsafeKeyword);
            }

            return BindEmbeddedBlock(node.Block, diagnostics);
        }

        private BoundStatement BindFixedStatement(FixedStatementSyntax node, DiagnosticBag diagnostics)
        {
            var fixedBinder = this.GetBinder(node);
            Debug.Assert(fixedBinder != null);

            fixedBinder.ReportUnsafeIfNotAllowed(node, diagnostics);

            return fixedBinder.BindFixedStatementParts(node, diagnostics);
        }

        private BoundStatement BindFixedStatementParts(FixedStatementSyntax node, DiagnosticBag diagnostics)
        {
            VariableDeclarationSyntax declarationSyntax = node.Declaration;

            ImmutableArray<BoundLocalDeclaration> declarations;
            BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.FixedVariable, diagnostics, out declarations);

            Debug.Assert(!declarations.IsEmpty);

            BoundMultipleLocalDeclarations boundMultipleDeclarations = new BoundMultipleLocalDeclarations(declarationSyntax, declarations);

            BoundStatement boundBody = BindPossibleEmbeddedStatement(node.Statement, diagnostics);

            return new BoundFixedStatement(node,
                                           GetDeclaredLocalsForScope(node),
                                           boundMultipleDeclarations,
                                           boundBody);
        }

        private void CheckRequiredLangVersionForAsyncIteratorMethods(DiagnosticBag diagnostics)
        {
            var method = (MethodSymbol)this.ContainingMemberOrLambda;
            if (method.IsAsync)
            {
                MessageID.IDS_FeatureAsyncStreams.CheckFeatureAvailability(
                    diagnostics,
                    method.DeclaringCompilation,
                    method.Locations[0]);
            }
        }

        private BoundStatement BindYieldReturnStatement(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            var binder = this;

            TypeSymbol elementType = binder.GetIteratorElementType(node, diagnostics).Type;
            BoundExpression argument = (node.Expression == null)
                ? BadExpression(node).MakeCompilerGenerated()
                : binder.BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            argument = ValidateEscape(argument, ExternalScope, isByRef: false, diagnostics: diagnostics);

            if (!argument.HasAnyErrors)
            {
                argument = binder.GenerateConversionForAssignment(elementType, argument, diagnostics);
            }
            else
            {
                argument = BindToTypeForErrorRecovery(argument);
            }

            // NOTE: it's possible that more than one of these conditions is satisfied and that
            // we won't report the syntactically innermost.  However, dev11 appears to check
            // them in this order, regardless of syntactic nesting (StatementBinder::bindYield).
            if (this.Flags.Includes(BinderFlags.InFinallyBlock))
            {
                Error(diagnostics, ErrorCode.ERR_BadYieldInFinally, node.YieldKeyword);
            }
            else if (this.Flags.Includes(BinderFlags.InTryBlockOfTryCatch))
            {
                Error(diagnostics, ErrorCode.ERR_BadYieldInTryOfCatch, node.YieldKeyword);
            }
            else if (this.Flags.Includes(BinderFlags.InCatchBlock))
            {
                Error(diagnostics, ErrorCode.ERR_BadYieldInCatch, node.YieldKeyword);
            }
            else if (BindingTopLevelScriptCode)
            {
                Error(diagnostics, ErrorCode.ERR_YieldNotAllowedInScript, node.YieldKeyword);
            }

            CheckRequiredLangVersionForAsyncIteratorMethods(diagnostics);
            return new BoundYieldReturnStatement(node, argument);
        }

        private BoundStatement BindYieldBreakStatement(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            if (this.Flags.Includes(BinderFlags.InFinallyBlock))
            {
                Error(diagnostics, ErrorCode.ERR_BadYieldInFinally, node.YieldKeyword);
            }
            else if (BindingTopLevelScriptCode)
            {
                Error(diagnostics, ErrorCode.ERR_YieldNotAllowedInScript, node.YieldKeyword);
            }

            GetIteratorElementType(node, diagnostics);
            CheckRequiredLangVersionForAsyncIteratorMethods(diagnostics);
            return new BoundYieldBreakStatement(node);
        }

        private BoundStatement BindLockStatement(LockStatementSyntax node, DiagnosticBag diagnostics)
        {
            var lockBinder = this.GetBinder(node);
            Debug.Assert(lockBinder != null);
            return lockBinder.BindLockStatementParts(diagnostics, lockBinder);
        }

        internal virtual BoundStatement BindLockStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindLockStatementParts(diagnostics, originalBinder);
        }


        private BoundStatement BindUsingStatement(UsingStatementSyntax node, DiagnosticBag diagnostics)
        {
            var usingBinder = this.GetBinder(node);
            Debug.Assert(usingBinder != null);
            return usingBinder.BindUsingStatementParts(diagnostics, usingBinder);
        }

        internal virtual BoundStatement BindUsingStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindUsingStatementParts(diagnostics, originalBinder);
        }

        internal BoundStatement BindPossibleEmbeddedStatement(StatementSyntax node, DiagnosticBag diagnostics)
        {
            Binder binder;

            switch (node.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                    // Local declarations are not legal in contexts where we need embedded statements.
                    diagnostics.Add(ErrorCode.ERR_BadEmbeddedStmt, node.GetLocation());

                    // fall through
                    goto case SyntaxKind.ExpressionStatement;

                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.LockStatement:
                case SyntaxKind.IfStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                    binder = this.GetBinder(node);
                    Debug.Assert(binder != null);
                    return binder.WrapWithVariablesIfAny(node, binder.BindStatement(node, diagnostics));

                case SyntaxKind.LabeledStatement:
                case SyntaxKind.LocalFunctionStatement:
                    // Labeled statements and local function statements are not legal in contexts where we need embedded statements.
                    diagnostics.Add(ErrorCode.ERR_BadEmbeddedStmt, node.GetLocation());

                    binder = this.GetBinder(node);
                    Debug.Assert(binder != null);
                    return binder.WrapWithVariablesAndLocalFunctionsIfAny(node, binder.BindStatement(node, diagnostics));

                case SyntaxKind.SwitchStatement:
                    var switchStatement = (SwitchStatementSyntax)node;
                    binder = this.GetBinder(switchStatement.Expression);
                    Debug.Assert(binder != null);
                    return binder.WrapWithVariablesIfAny(switchStatement.Expression, binder.BindStatement(node, diagnostics));

                case SyntaxKind.EmptyStatement:
                    var emptyStatement = (EmptyStatementSyntax)node;
                    if (!emptyStatement.SemicolonToken.IsMissing)
                    {
                        switch (node.Parent.Kind())
                        {
                            case SyntaxKind.ForStatement:
                            case SyntaxKind.ForEachStatement:
                            case SyntaxKind.ForEachVariableStatement:
                            case SyntaxKind.WhileStatement:
                                // For loop constructs, only warn if we see a block following the statement.
                                // That indicates code like:  "while (x) ; { }"
                                // which is most likely a bug.
                                if (emptyStatement.SemicolonToken.GetNextToken().Kind() != SyntaxKind.OpenBraceToken)
                                {
                                    break;
                                }

                                goto default;

                            default:
                                // For non-loop constructs, always warn.  This is for code like:
                                // "if (x) ;" which is almost certainly a bug.
                                diagnostics.Add(ErrorCode.WRN_PossibleMistakenNullStatement, node.GetLocation());
                                break;
                        }
                    }

                    // fall through
                    goto default;

                default:
                    return BindStatement(node, diagnostics);
            }
        }

        private BoundExpression BindThrownExpression(ExpressionSyntax exprSyntax, DiagnosticBag diagnostics, ref bool hasErrors)
        {
            var boundExpr = BindValue(exprSyntax, diagnostics, BindValueKind.RValue);
            if (Compilation.LanguageVersion < MessageID.IDS_FeatureSwitchExpression.RequiredVersion())
            {
                // This is the pre-C# 8 algorithm for binding a thrown expression.
                // SPEC VIOLATION: The spec requires the thrown exception to have a type, and that the type
                // be System.Exception or derived from System.Exception. (Or, if a type parameter, to have
                // an effective base class that meets that criterion.) However, we allow the literal null
                // to be thrown, even though it does not meet that criterion and will at runtime always
                // produce a null reference exception.
                if (!boundExpr.IsLiteralNull())
                {
                    boundExpr = BindToNaturalType(boundExpr, diagnostics);
                    var type = boundExpr.Type;

                    // If the expression is a lambda, anonymous method, or method group then it will
                    // have no compile-time type; give the same error as if the type was wrong.
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                    if ((object)type == null || !type.IsErrorType() && !Compilation.IsExceptionType(type.EffectiveType(ref useSiteDiagnostics), ref useSiteDiagnostics))
                    {
                        diagnostics.Add(ErrorCode.ERR_BadExceptionType, exprSyntax.Location);
                        hasErrors = true;
                        diagnostics.Add(exprSyntax, useSiteDiagnostics);
                    }
                }
            }
            else
            {
                // In C# 8 and later we follow the ECMA specification, which neatly handles null and expressions of exception type.
                boundExpr = GenerateConversionForAssignment(Compilation.GetWellKnownType(WellKnownType.System_Exception), boundExpr, diagnostics);
            }

            return boundExpr;
        }

        private BoundStatement BindThrow(ThrowStatementSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression boundExpr = null;
            bool hasErrors = false;

            ExpressionSyntax exprSyntax = node.Expression;
            if (exprSyntax != null)
            {
                boundExpr = BindThrownExpression(exprSyntax, diagnostics, ref hasErrors);
            }
            else if (!this.Flags.Includes(BinderFlags.InCatchBlock))
            {
                diagnostics.Add(ErrorCode.ERR_BadEmptyThrow, node.ThrowKeyword.GetLocation());
                hasErrors = true;
            }
            else if (this.Flags.Includes(BinderFlags.InNestedFinallyBlock))
            {
                // There's a special error code for a rethrow in a finally clause in a catch clause.
                // Best guess interpretation: if an exception occurs within the nested try block
                // (i.e. the one in the catch clause, to which the finally clause is attached),
                // then it's not clear whether the runtime will try to rethrow the "inner" exception
                // or the "outer" exception. For this reason, the case is disallowed.

                diagnostics.Add(ErrorCode.ERR_BadEmptyThrowInFinally, node.ThrowKeyword.GetLocation());
                hasErrors = true;
            }

            return new BoundThrowStatement(node, boundExpr, hasErrors);
        }

        private static BoundStatement BindEmpty(EmptyStatementSyntax node)
        {
            return new BoundNoOpStatement(node, NoOpStatementFlavor.Default);
        }

        private BoundLabeledStatement BindLabeled(LabeledStatementSyntax node, DiagnosticBag diagnostics)
        {
            // TODO: verify that goto label lookup was valid (e.g. error checking of symbol resolution for labels)
            bool hasError = false;

            var result = LookupResult.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var binder = this.LookupSymbolsWithFallback(result, node.Identifier.ValueText, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics, options: LookupOptions.LabelsOnly);

            // result.Symbols can be empty in some malformed code, e.g. when a labeled statement is used an embedded statement in an if or foreach statement    
            // In this case we create new label symbol on the fly, and an error is reported by parser
            var symbol = result.Symbols.Count > 0 && result.IsMultiViable ?
                (LabelSymbol)result.Symbols.First() :
                new SourceLabelSymbol((MethodSymbol)ContainingMemberOrLambda, node.Identifier);

            if (!symbol.IdentifierNodeOrToken.IsToken || symbol.IdentifierNodeOrToken.AsToken() != node.Identifier)
            {
                Error(diagnostics, ErrorCode.ERR_DuplicateLabel, node.Identifier, node.Identifier.ValueText);
                hasError = true;
            }

            // check to see if this label (illegally) hides a label from an enclosing scope
            if (binder != null)
            {
                result.Clear();
                binder.Next.LookupSymbolsWithFallback(result, node.Identifier.ValueText, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics, options: LookupOptions.LabelsOnly);
                if (result.IsMultiViable)
                {
                    // The label '{0}' shadows another label by the same name in a contained scope
                    Error(diagnostics, ErrorCode.ERR_LabelShadow, node.Identifier, node.Identifier.ValueText);
                    hasError = true;
                }
            }

            diagnostics.Add(node, useSiteDiagnostics);
            result.Free();

            var body = BindStatement(node.Statement, diagnostics);
            return new BoundLabeledStatement(node, symbol, body, hasError);
        }

        private BoundStatement BindGoto(GotoStatementSyntax node, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.GotoStatement:
                    var expression = BindLabel(node.Expression, diagnostics);
                    var boundLabel = expression as BoundLabel;
                    if (boundLabel == null)
                    {
                        // diagnostics already reported
                        return new BoundBadStatement(node, ImmutableArray.Create<BoundNode>(expression), true);
                    }
                    var symbol = boundLabel.Label;
                    return new BoundGotoStatement(node, symbol, null, boundLabel);

                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoDefaultStatement:

                    // SPEC:    If the goto case statement is not enclosed by a switch statement, a compile-time error occurs.
                    // SPEC:    If the goto default statement is not enclosed by a switch statement, a compile-time error occurs.

                    SwitchBinder binder = GetSwitchBinder(this);
                    if (binder == null)
                    {
                        Error(diagnostics, ErrorCode.ERR_InvalidGotoCase, node);
                        ImmutableArray<BoundNode> childNodes;
                        if (node.Expression != null)
                        {
                            var dummyDiagnostics = DiagnosticBag.GetInstance();
                            var value = BindRValueWithoutTargetType(node.Expression, dummyDiagnostics);
                            childNodes = ImmutableArray.Create<BoundNode>(value);
                            dummyDiagnostics.Free();
                        }
                        else
                        {
                            childNodes = ImmutableArray<BoundNode>.Empty;
                        }
                        return new BoundBadStatement(node, childNodes, true);
                    }
                    return binder.BindGotoCaseOrDefault(node, this, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundStatement BindLocalFunctionStatement(LocalFunctionStatementSyntax node, DiagnosticBag diagnostics)
        {
            // already defined symbol in containing block
            var localSymbol = this.LookupLocalFunction(node.Identifier);

            var hasErrors = localSymbol.ScopeBinder
                .ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

            BoundBlock blockBody = null;
            BoundBlock expressionBody = null;
            if (node.Body != null)
            {
                blockBody = runAnalysis(BindEmbeddedBlock(node.Body, diagnostics), diagnostics);

                if (node.ExpressionBody != null)
                {
                    var expressionBodyDiagnostics = new DiagnosticBag();
                    expressionBody = runAnalysis(BindExpressionBodyAsBlock(node.ExpressionBody, expressionBodyDiagnostics), expressionBodyDiagnostics);
                }
            }
            else if (node.ExpressionBody != null)
            {
                expressionBody = runAnalysis(BindExpressionBodyAsBlock(node.ExpressionBody, diagnostics), diagnostics);
            }
            else
            {
                hasErrors = true;
                diagnostics.Add(ErrorCode.ERR_LocalFunctionMissingBody, localSymbol.Locations[0], localSymbol);
            }

            Debug.Assert(blockBody != null || expressionBody != null || hasErrors);

            localSymbol.GetDeclarationDiagnostics(diagnostics);

            Symbol.CheckForBlockAndExpressionBody(
                node.Body, node.ExpressionBody, node, diagnostics);

            return new BoundLocalFunctionStatement(node, localSymbol, blockBody, expressionBody, hasErrors);

            BoundBlock runAnalysis(BoundBlock block, DiagnosticBag blockDiagnostics)
            {
                if (block != null)
                {
                    // Have to do ControlFlowPass here because in MethodCompiler, we don't call this for synthed methods
                    // rather we go directly to LowerBodyOrInitializer, which skips over flow analysis (which is in CompileMethod)
                    // (the same thing - calling ControlFlowPass.Analyze in the lowering - is done for lambdas)
                    // It's a bit of code duplication, but refactoring would make things worse.
                    var endIsReachable = ControlFlowPass.Analyze(localSymbol.DeclaringCompilation, localSymbol, block, blockDiagnostics);
                    if (endIsReachable)
                    {
                        if (ImplicitReturnIsOkay(localSymbol))
                        {
                            block = FlowAnalysisPass.AppendImplicitReturn(block, localSymbol);
                        }
                        else
                        {
                            blockDiagnostics.Add(ErrorCode.ERR_ReturnExpected, localSymbol.Locations[0], localSymbol);
                        }
                    }
                }

                return block;
            }
        }

        private bool ImplicitReturnIsOkay(MethodSymbol method)
        {
            return method.ReturnsVoid || method.IsIterator || method.IsTaskReturningAsync(this.Compilation);
        }

        public BoundStatement BindExpressionStatement(ExpressionStatementSyntax node, DiagnosticBag diagnostics)
        {
            return BindExpressionStatement(node, node.Expression, node.AllowsAnyExpression, diagnostics);
        }

        private BoundExpressionStatement BindExpressionStatement(CSharpSyntaxNode node, ExpressionSyntax syntax, bool allowsAnyExpression, DiagnosticBag diagnostics)
        {
            BoundExpressionStatement expressionStatement;

            var expression = BindRValueWithoutTargetType(syntax, diagnostics);
            ReportSuppressionIfNeeded(expression, diagnostics);
            if (!allowsAnyExpression && !IsValidStatementExpression(syntax, expression))
            {
                if (!node.HasErrors)
                {
                    Error(diagnostics, ErrorCode.ERR_IllegalStatement, syntax);
                }

                expressionStatement = new BoundExpressionStatement(node, expression, hasErrors: true);
            }
            else
            {
                expressionStatement = new BoundExpressionStatement(node, expression);
            }

            CheckForUnobservedAwaitable(expression, diagnostics);

            return expressionStatement;
        }

        /// <summary>
        /// Report an error if this is an awaitable async method invocation that is not being awaited.
        /// </summary>
        /// <remarks>
        /// The checks here are equivalent to StatementBinder::CheckForUnobservedAwaitable() in the native compiler.
        /// </remarks>
        private void CheckForUnobservedAwaitable(BoundExpression expression, DiagnosticBag diagnostics)
        {
            if (CouldBeAwaited(expression))
            {
                Error(diagnostics, ErrorCode.WRN_UnobservedAwaitableExpression, expression.Syntax);
            }
        }

        internal BoundStatement BindLocalDeclarationStatement(LocalDeclarationStatementSyntax node, DiagnosticBag diagnostics)
        {
            if (node.UsingKeyword != default)
            {
                return BindUsingDeclarationStatementParts(node, diagnostics);
            }
            else
            {
                return BindDeclarationStatementParts(node, diagnostics);
            }
        }

        private BoundStatement BindUsingDeclarationStatementParts(LocalDeclarationStatementSyntax node, DiagnosticBag diagnostics)
        {
            var usingDeclaration = UsingStatementBinder.BindUsingStatementOrDeclarationFromParts(node, node.UsingKeyword, node.AwaitKeyword, originalBinder: this, usingBinderOpt: null, diagnostics);
            Debug.Assert(usingDeclaration is BoundUsingLocalDeclarations);
            return usingDeclaration;
        }

        private BoundStatement BindDeclarationStatementParts(LocalDeclarationStatementSyntax node, DiagnosticBag diagnostics)
        {
            var typeSyntax = node.Declaration.Type.SkipRef(out _);
            bool isConst = node.IsConst;

            bool isVar;
            AliasSymbol alias;
            TypeWithAnnotations declType = BindVariableTypeWithAnnotations(node.Declaration, diagnostics, typeSyntax, ref isConst, isVar: out isVar, alias: out alias);

            var kind = isConst ? LocalDeclarationKind.Constant : LocalDeclarationKind.RegularVariable;
            var variableList = node.Declaration.Variables;
            int variableCount = variableList.Count;
            if (variableCount == 1)
            {
                return BindVariableDeclaration(kind, isVar, variableList[0], typeSyntax, declType, alias, diagnostics, includeBoundType: true, associatedSyntaxNode: node);
            }
            else
            {
                BoundLocalDeclaration[] boundDeclarations = new BoundLocalDeclaration[variableCount];
                int i = 0;
                foreach (var variableDeclarationSyntax in variableList)
                {
                    bool includeBoundType = i == 0; //To avoid duplicated expressions, only the first declaration should contain the bound type.
                    boundDeclarations[i++] = BindVariableDeclaration(kind, isVar, variableDeclarationSyntax, typeSyntax, declType, alias, diagnostics, includeBoundType);
                }
                return new BoundMultipleLocalDeclarations(node, boundDeclarations.AsImmutableOrNull());
            }
        }

        /// <summary>
        /// Checks for a Dispose method on <paramref name="expr"/> and returns its <see cref="MethodSymbol"/> if found.
        /// </summary>
        /// <param name="expr">Expression on which to perform lookup</param>
        /// <param name="syntaxNode">The syntax node to perform lookup on</param>
        /// <param name="diagnostics">Populated with invocation errors, and warnings of near misses</param>
        /// <returns>The <see cref="MethodSymbol"/> of the Dispose method if one is found, otherwise null.</returns>
        internal MethodSymbol TryFindDisposePatternMethod(BoundExpression expr, SyntaxNode syntaxNode, bool hasAwait, DiagnosticBag diagnostics)
        {
            Debug.Assert(expr is object);
            Debug.Assert(expr.Type is object);
            Debug.Assert(expr.Type.IsRefLikeType || hasAwait); // pattern dispose lookup is only valid on ref structs or asynchronous usings

            // Don't try and lookup if we're not enabled
            if (MessageID.IDS_FeatureUsingDeclarations.RequiredVersion() > Compilation.LanguageVersion)
            {
                return null;
            }

            var result = PerformPatternMethodLookup(expr,
                                                    hasAwait ? WellKnownMemberNames.DisposeAsyncMethodName : WellKnownMemberNames.DisposeMethodName,
                                                    syntaxNode,
                                                    diagnostics,
                                                    out var disposeMethod);

            if (disposeMethod?.IsExtensionMethod == true)
            {
                // Extension methods should just be ignored, rather than rejected after-the-fact
                // Tracked by https://github.com/dotnet/roslyn/issues/32767

                // extension methods do not contribute to pattern-based disposal
                disposeMethod = null;
            }
            else if ((!hasAwait && disposeMethod?.ReturnsVoid == false)
                || result == PatternLookupResult.NotAMethod)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                if (this.IsAccessible(disposeMethod, ref useSiteDiagnostics))
                {
                    diagnostics.Add(ErrorCode.WRN_PatternBadSignature, syntaxNode.Location, expr.Type, MessageID.IDS_Disposable.Localize(), disposeMethod);
                }

                diagnostics.Add(syntaxNode, useSiteDiagnostics);
                disposeMethod = null;
            }

            return disposeMethod;
        }

        private TypeWithAnnotations BindVariableTypeWithAnnotations(CSharpSyntaxNode declarationNode, DiagnosticBag diagnostics, TypeSyntax typeSyntax, ref bool isConst, out bool isVar, out AliasSymbol alias)
        {
            Debug.Assert(
                declarationNode is VariableDesignationSyntax ||
                declarationNode.Kind() == SyntaxKind.VariableDeclaration ||
                declarationNode.Kind() == SyntaxKind.DeclarationExpression ||
                declarationNode.Kind() == SyntaxKind.DiscardDesignation);

            // If the type is "var" then suppress errors when binding it. "var" might be a legal type
            // or it might not; if it is not then we do not want to report an error. If it is, then
            // we want to treat the declaration as an explicitly typed declaration.

            TypeWithAnnotations declType = BindTypeWithAnnotationsOrVarKeyword(typeSyntax.SkipRef(out _), diagnostics, out isVar, out alias);
            Debug.Assert(declType.HasType || isVar);

            if (isVar)
            {
                // There are a number of ways in which a var decl can be illegal, but in these 
                // cases we should report an error and then keep right on going with the inference.

                if (isConst)
                {
                    Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, declarationNode);
                    // Keep processing it as a non-const local.
                    isConst = false;
                }

                // In the dev10 compiler the error recovery semantics for the illegal case
                // "var x = 10, y = 123.4;" are somewhat undesirable.
                //
                // First off, this is an error because a straw poll of language designers and
                // users showed that there was no consensus on whether the above should mean
                // "double x = 10, y = 123.4;", taking the best type available and substituting
                // that for "var", or treating it as "var x = 10; var y = 123.4;" -- since there
                // was no consensus we decided to simply make it illegal. 
                //
                // In dev10 for error recovery in the IDE we do an odd thing -- we simply take
                // the type of the first variable and use it. So that is "int x = 10, y = 123.4;".
                // 
                // This seems less than ideal. In the error recovery scenario it probably makes
                // more sense to treat that as "var x = 10; var y = 123.4;" and do each inference
                // separately.

                if (declarationNode.Parent.Kind() == SyntaxKind.LocalDeclarationStatement &&
                    ((VariableDeclarationSyntax)declarationNode).Variables.Count > 1 && !declarationNode.HasErrors)
                {
                    Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, declarationNode);
                }
            }
            else
            {
                // In the native compiler when given a situation like
                //
                // D[] x;
                // 
                // where D is a static type we report both that D cannot be an element type
                // of an array, and that D[] is not a valid type for a local variable.
                // This seems silly; the first error is entirely sufficient. We no longer
                // produce additional errors for local variables of arrays of static types.

                if (declType.IsStatic)
                {
                    Error(diagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, declType.Type);
                }

                if (isConst && !declType.Type.CanBeConst())
                {
                    Error(diagnostics, ErrorCode.ERR_BadConstType, typeSyntax, declType.Type);
                    // Keep processing it as a non-const local.
                    isConst = false;
                }
            }

            return declType;
        }

        internal BoundExpression BindInferredVariableInitializer(DiagnosticBag diagnostics, RefKind refKind, EqualsValueClauseSyntax initializer,
            CSharpSyntaxNode errorSyntax)
        {
            BindValueKind valueKind;
            ExpressionSyntax value;
            IsInitializerRefKindValid(initializer, initializer, refKind, diagnostics, out valueKind, out value); // The return value isn't important here; we just want the diagnostics and the BindValueKind
            return BindInferredVariableInitializer(diagnostics, value, valueKind, refKind, errorSyntax);
        }

        // The location where the error is reported might not be the initializer.
        protected BoundExpression BindInferredVariableInitializer(DiagnosticBag diagnostics, ExpressionSyntax initializer, BindValueKind valueKind, RefKind refKind, CSharpSyntaxNode errorSyntax)
        {
            if (initializer == null)
            {
                if (!errorSyntax.HasErrors)
                {
                    Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, errorSyntax);
                }

                return null;
            }

            if (initializer.Kind() == SyntaxKind.ArrayInitializerExpression)
            {
                var result = BindUnexpectedArrayInitializer((InitializerExpressionSyntax)initializer,
                    diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, errorSyntax);

                return CheckValue(result, valueKind, diagnostics);
            }

            BoundExpression expression = BindToNaturalType(BindValue(initializer, diagnostics, valueKind), diagnostics);

            if (expression is BoundStackAllocArrayCreation boundStackAlloc &&
                initializer.IsLocalVariableDeclarationInitializationForPointerStackalloc() &&
                (initializer.Kind() == SyntaxKind.StackAllocArrayCreationExpression || initializer.Kind() == SyntaxKind.ImplicitStackAllocArrayCreationExpression))
            {
                var type = new PointerTypeSymbol(TypeWithAnnotations.Create(boundStackAlloc.ElementType));
                expression = GenerateConversionForAssignment(type, boundStackAlloc, diagnostics, isRefAssignment: refKind != RefKind.None);
            }

            expression = BindToNaturalType(expression, diagnostics);

            // Certain expressions (null literals, method groups and anonymous functions) have no type of 
            // their own and therefore cannot be the initializer of an implicitly typed local.
            if (!expression.HasAnyErrors && !expression.HasExpressionType())
            {
                // Cannot assign {0} to an implicitly-typed local variable
                Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, errorSyntax, expression.Display);
            }

            return expression;
        }

        private static bool IsInitializerRefKindValid(
            EqualsValueClauseSyntax initializer,
            CSharpSyntaxNode node,
            RefKind variableRefKind,
            DiagnosticBag diagnostics,
            out BindValueKind valueKind,
            out ExpressionSyntax value)
        {
            RefKind expressionRefKind = RefKind.None;
            value = initializer?.Value.CheckAndUnwrapRefExpression(diagnostics, out expressionRefKind);
            if (variableRefKind == RefKind.None)
            {
                valueKind = BindValueKind.RValue;
                if (expressionRefKind == RefKind.Ref)
                {
                    Error(diagnostics, ErrorCode.ERR_InitializeByValueVariableWithReference, node);
                    return false;
                }
            }
            else
            {
                valueKind = variableRefKind == RefKind.RefReadOnly
                    ? BindValueKind.ReadonlyRef
                    : BindValueKind.RefOrOut;

                if (initializer == null)
                {
                    Error(diagnostics, ErrorCode.ERR_ByReferenceVariableMustBeInitialized, node);
                    return false;
                }
                else if (expressionRefKind != RefKind.Ref)
                {
                    Error(diagnostics, ErrorCode.ERR_InitializeByReferenceVariableWithValue, node);
                    return false;
                }
            }

            return true;
        }

        protected BoundLocalDeclaration BindVariableDeclaration(
            LocalDeclarationKind kind,
            bool isVar,
            VariableDeclaratorSyntax declarator,
            TypeSyntax typeSyntax,
            TypeWithAnnotations declTypeOpt,
            AliasSymbol aliasOpt,
            DiagnosticBag diagnostics,
            bool includeBoundType,
            CSharpSyntaxNode associatedSyntaxNode = null)
        {
            Debug.Assert(declarator != null);

            return BindVariableDeclaration(LocateDeclaredVariableSymbol(declarator, typeSyntax, kind),
                                           kind,
                                           isVar,
                                           declarator,
                                           typeSyntax,
                                           declTypeOpt,
                                           aliasOpt,
                                           diagnostics,
                                           includeBoundType,
                                           associatedSyntaxNode);
        }

        protected BoundLocalDeclaration BindVariableDeclaration(
            SourceLocalSymbol localSymbol,
            LocalDeclarationKind kind,
            bool isVar,
            VariableDeclaratorSyntax declarator,
            TypeSyntax typeSyntax,
            TypeWithAnnotations declTypeOpt,
            AliasSymbol aliasOpt,
            DiagnosticBag diagnostics,
            bool includeBoundType,
            CSharpSyntaxNode associatedSyntaxNode = null)
        {
            Debug.Assert(declarator != null);
            Debug.Assert(declTypeOpt.HasType || isVar);
            Debug.Assert(typeSyntax != null);

            var localDiagnostics = DiagnosticBag.GetInstance();
            // if we are not given desired syntax, we use declarator
            associatedSyntaxNode = associatedSyntaxNode ?? declarator;

            // Check for variable declaration errors.
            // Use the binder that owns the scope for the local because this (the current) binder
            // might own nested scope.
            bool hasErrors = localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

            var containingMethod = this.ContainingMemberOrLambda as MethodSymbol;
            if (containingMethod != null && containingMethod.IsAsync && localSymbol.RefKind != RefKind.None)
            {
                Error(diagnostics, ErrorCode.ERR_BadAsyncLocalType, declarator);
            }

            EqualsValueClauseSyntax equalsClauseSyntax = declarator.Initializer;

            BindValueKind valueKind;
            ExpressionSyntax value;
            if (!IsInitializerRefKindValid(equalsClauseSyntax, declarator, localSymbol.RefKind, diagnostics, out valueKind, out value))
            {
                hasErrors = true;
            }

            BoundExpression initializerOpt;
            if (isVar)
            {
                aliasOpt = null;

                initializerOpt = BindInferredVariableInitializer(diagnostics, value, valueKind, localSymbol.RefKind, declarator);

                // If we got a good result then swap the inferred type for the "var" 
                TypeSymbol initializerType = initializerOpt?.Type;
                if ((object)initializerType != null)
                {
                    declTypeOpt = TypeWithAnnotations.Create(initializerType);

                    if (declTypeOpt.IsVoidType())
                    {
                        Error(localDiagnostics, ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, declarator, declTypeOpt.Type);
                        declTypeOpt = TypeWithAnnotations.Create(CreateErrorType("var"));
                        hasErrors = true;
                    }

                    if (!declTypeOpt.Type.IsErrorType())
                    {
                        if (declTypeOpt.IsStatic)
                        {
                            Error(localDiagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, initializerType);
                            hasErrors = true;
                        }
                    }
                }
                else
                {
                    declTypeOpt = TypeWithAnnotations.Create(CreateErrorType("var"));
                    hasErrors = true;
                }
            }
            else
            {
                if (ReferenceEquals(equalsClauseSyntax, null))
                {
                    initializerOpt = null;
                }
                else
                {
                    // Basically inlined BindVariableInitializer, but with conversion optional.
                    initializerOpt = BindPossibleArrayInitializer(value, declTypeOpt.Type, valueKind, diagnostics);
                    if (kind != LocalDeclarationKind.FixedVariable)
                    {
                        // If this is for a fixed statement, we'll do our own conversion since there are some special cases.		
                        initializerOpt = GenerateConversionForAssignment(
                            declTypeOpt.Type,
                            initializerOpt,
                            localDiagnostics,
                            isRefAssignment: localSymbol.RefKind != RefKind.None);
                    }
                }
            }

            Debug.Assert(declTypeOpt.HasType);

            if (kind == LocalDeclarationKind.FixedVariable)
            {
                // NOTE: this is an error, but it won't prevent further binding.
                if (isVar)
                {
                    if (!hasErrors)
                    {
                        Error(localDiagnostics, ErrorCode.ERR_ImplicitlyTypedLocalCannotBeFixed, declarator);
                        hasErrors = true;
                    }
                }

                if (!declTypeOpt.Type.IsPointerType())
                {
                    if (!hasErrors)
                    {
                        Error(localDiagnostics, ErrorCode.ERR_BadFixedInitType, declarator);
                        hasErrors = true;
                    }
                }
                else if (!IsValidFixedVariableInitializer(declTypeOpt.Type, localSymbol, ref initializerOpt, localDiagnostics))
                {
                    hasErrors = true;
                }
            }

            if (CheckRestrictedTypeInAsync(this.ContainingMemberOrLambda, declTypeOpt.Type, localDiagnostics, typeSyntax))
            {
                hasErrors = true;
            }

            localSymbol.SetTypeWithAnnotations(declTypeOpt);

            if (initializerOpt != null)
            {
                var currentScope = LocalScopeDepth;

                localSymbol.SetValEscape(GetValEscape(initializerOpt, currentScope));

                if (localSymbol.RefKind != RefKind.None)
                {
                    localSymbol.SetRefEscape(GetRefEscape(initializerOpt, currentScope));
                }
            }

            ImmutableArray<BoundExpression> arguments = BindDeclaratorArguments(declarator, localDiagnostics);

            if (kind == LocalDeclarationKind.FixedVariable || kind == LocalDeclarationKind.UsingVariable)
            {
                // CONSIDER: The error message is "you must provide an initializer in a fixed 
                // CONSIDER: or using declaration". The error message could be targetted to 
                // CONSIDER: the actual situation. "you must provide an initializer in a 
                // CONSIDER: 'fixed' declaration."

                if (initializerOpt == null)
                {
                    Error(localDiagnostics, ErrorCode.ERR_FixedMustInit, declarator);
                    hasErrors = true;
                }
            }
            else if (kind == LocalDeclarationKind.Constant && initializerOpt != null && !localDiagnostics.HasAnyResolvedErrors())
            {
                var constantValueDiagnostics = localSymbol.GetConstantValueDiagnostics(initializerOpt);
                foreach (var diagnostic in constantValueDiagnostics)
                {
                    diagnostics.Add(diagnostic);
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        hasErrors = true;
                    }
                }
            }

            diagnostics.AddRangeAndFree(localDiagnostics);

            BoundTypeExpression boundDeclType = null;

            if (includeBoundType)
            {
                var invalidDimensions = ArrayBuilder<BoundExpression>.GetInstance();

                typeSyntax.VisitRankSpecifiers((rankSpecifier, args) =>
                {
                    bool _ = false;
                    foreach (var expressionSyntax in rankSpecifier.Sizes)
                    {
                        var size = args.binder.BindArrayDimension(expressionSyntax, args.diagnostics, ref _);
                        if (size != null)
                        {
                            args.invalidDimensions.Add(size);
                        }
                    }
                }, (binder: this, invalidDimensions: invalidDimensions, diagnostics: diagnostics));

                boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, dimensionsOpt: invalidDimensions.ToImmutableAndFree(), typeWithAnnotations: declTypeOpt);
            }

            return new BoundLocalDeclaration(
                syntax: associatedSyntaxNode,
                localSymbol: localSymbol,
                declaredTypeOpt: boundDeclType,
                initializerOpt: hasErrors ? BindToTypeForErrorRecovery(initializerOpt) : initializerOpt,
                argumentsOpt: arguments,
                inferredType: isVar,
                hasErrors: hasErrors);
        }

        internal ImmutableArray<BoundExpression> BindDeclaratorArguments(VariableDeclaratorSyntax declarator, DiagnosticBag diagnostics)
        {
            // It is possible that we have a bracketed argument list, like "int x[];" or "int x[123];" 
            // in a non-fixed-size-array declaration . This is a common error made by C++ programmers. 
            // We have already given a good error at parse time telling the user to either make it "fixed"
            // or to move the brackets to the type. However, we should still do semantic analysis of
            // the arguments, so that errors in them are discovered, hovering over them in the IDE
            // gives good results, and so on.

            var arguments = default(ImmutableArray<BoundExpression>);

            if (declarator.ArgumentList != null)
            {
                AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
                BindArgumentsAndNames(declarator.ArgumentList, diagnostics, analyzedArguments);
                arguments = BuildArgumentsForErrorRecovery(analyzedArguments);
                analyzedArguments.Free();
            }

            return arguments;
        }

        private SourceLocalSymbol LocateDeclaredVariableSymbol(VariableDeclaratorSyntax declarator, TypeSyntax typeSyntax, LocalDeclarationKind outerKind)
        {
            LocalDeclarationKind kind = outerKind == LocalDeclarationKind.UsingVariable ? LocalDeclarationKind.UsingVariable : LocalDeclarationKind.RegularVariable;
            return LocateDeclaredVariableSymbol(declarator.Identifier, typeSyntax, declarator.Initializer, kind);
        }

        private SourceLocalSymbol LocateDeclaredVariableSymbol(SyntaxToken identifier, TypeSyntax typeSyntax, EqualsValueClauseSyntax equalsValue, LocalDeclarationKind kind)
        {
            SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

            // In error scenarios with misplaced code, it is possible we can't bind the local declaration.
            // This occurs through the semantic model.  In that case concoct a plausible result.
            if ((object)localSymbol == null)
            {
                localSymbol = SourceLocalSymbol.MakeLocal(
                    ContainingMemberOrLambda,
                    this,
                    false, // do not allow ref
                    typeSyntax,
                    identifier,
                    kind,
                    equalsValue);
            }

            return localSymbol;
        }

        private bool IsValidFixedVariableInitializer(TypeSymbol declType, SourceLocalSymbol localSymbol, ref BoundExpression initializerOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(!ReferenceEquals(declType, null));
            Debug.Assert(declType.IsPointerType());

            if (initializerOpt?.HasAnyErrors != false)
            {
                return false;
            }

            TypeSymbol initializerType = initializerOpt.Type;
            SyntaxNode initializerSyntax = initializerOpt.Syntax;

            if ((object)initializerType == null)
            {
                Error(diagnostics, ErrorCode.ERR_ExprCannotBeFixed, initializerSyntax);
                return false;
            }

            TypeSymbol elementType;
            bool hasErrors = false;
            MethodSymbol fixedPatternMethod = null;

            switch (initializerOpt.Kind)
            {
                case BoundKind.AddressOfOperator:
                    elementType = ((BoundAddressOfOperator)initializerOpt).Operand.Type;
                    break;

                case BoundKind.FieldAccess:
                    var fa = (BoundFieldAccess)initializerOpt;
                    if (fa.FieldSymbol.IsFixedSizeBuffer)
                    {
                        elementType = ((PointerTypeSymbol)fa.Type).PointedAtType;
                        break;
                    }

                    goto default;

                default:
                    //  fixed (T* variable = <expr>) ...

                    // check for arrays
                    if (initializerType.IsArray())
                    {
                        // See ExpressionBinder::BindPtrToArray (though most of that functionality is now in LocalRewriter).
                        elementType = ((ArrayTypeSymbol)initializerType).ElementType;
                        break;
                    }

                    // check for a special ref-returning method
                    var additionalDiagnostics = DiagnosticBag.GetInstance();
                    fixedPatternMethod = GetFixedPatternMethodOpt(initializerOpt, additionalDiagnostics);

                    // check for String
                    // NOTE: We will allow the pattern method to take precedence, but only if it is an instance member of System.String
                    if (initializerType.SpecialType == SpecialType.System_String &&
                        ((object)fixedPatternMethod == null || fixedPatternMethod.ContainingType.SpecialType != SpecialType.System_String))
                    {
                        fixedPatternMethod = null;
                        elementType = this.GetSpecialType(SpecialType.System_Char, diagnostics, initializerSyntax);
                        additionalDiagnostics.Free();
                        break;
                    }

                    // if the feature was enabled, but something went wrong with the method, report that, otherwise don't. 
                    // If feature is not enabled, additional errors would be just noise.
                    bool extensibleFixedEnabled = ((CSharpParseOptions)initializerOpt.SyntaxTree.Options)?.IsFeatureEnabled(MessageID.IDS_FeatureExtensibleFixedStatement) != false;
                    if (extensibleFixedEnabled)
                    {
                        diagnostics.AddRange(additionalDiagnostics);
                    }

                    additionalDiagnostics.Free();

                    if ((object)fixedPatternMethod != null)
                    {
                        elementType = fixedPatternMethod.ReturnType;
                        CheckFeatureAvailability(initializerOpt.Syntax, MessageID.IDS_FeatureExtensibleFixedStatement, diagnostics);
                        break;
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_ExprCannotBeFixed, initializerSyntax);
                        return false;
                    }
            }

            if (CheckManagedAddr(Compilation, elementType, initializerSyntax.Location, diagnostics))
            {
                hasErrors = true;
            }

            initializerOpt = GetFixedLocalCollectionInitializer(initializerOpt, elementType, declType, fixedPatternMethod, hasErrors, diagnostics);
            return true;
        }

        private MethodSymbol GetFixedPatternMethodOpt(BoundExpression initializer, DiagnosticBag additionalDiagnostics)
        {
            if (initializer.Type.IsVoidType())
            {
                return null;
            }

            const string methodName = "GetPinnableReference";

            var result = PerformPatternMethodLookup(initializer, methodName, initializer.Syntax, additionalDiagnostics, out var patternMethodSymbol);

            if (patternMethodSymbol is null)
            {
                return null;
            }

            if (HasOptionalOrVariableParameters(patternMethodSymbol) ||
                patternMethodSymbol.ReturnsVoid ||
                !patternMethodSymbol.RefKind.IsManagedReference() ||
                !(patternMethodSymbol.ParameterCount == 0 || patternMethodSymbol.IsStatic && patternMethodSymbol.ParameterCount == 1))
            {
                // the method does not fit the pattern
                additionalDiagnostics.Add(ErrorCode.WRN_PatternBadSignature, initializer.Syntax.Location, initializer.Type, "fixed", patternMethodSymbol);
                return null;
            }

            return patternMethodSymbol;
        }

        /// <summary>
        /// Wrap the initializer in a BoundFixedLocalCollectionInitializer so that the rewriter will have the
        /// information it needs (e.g. conversions, helper methods).
        /// </summary>
        private BoundExpression GetFixedLocalCollectionInitializer(
            BoundExpression initializer,
            TypeSymbol elementType,
            TypeSymbol declType,
            MethodSymbol patternMethodOpt,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(initializer != null);

            SyntaxNode initializerSyntax = initializer.Syntax;

            TypeSymbol pointerType = new PointerTypeSymbol(TypeWithAnnotations.Create(elementType));
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion elementConversion = this.Conversions.ClassifyConversionFromType(pointerType, declType, ref useSiteDiagnostics);
            diagnostics.Add(initializerSyntax, useSiteDiagnostics);

            if (!elementConversion.IsValid || !elementConversion.IsImplicit)
            {
                GenerateImplicitConversionError(diagnostics, this.Compilation, initializerSyntax, elementConversion, pointerType, declType);
                hasErrors = true;
            }

            return new BoundFixedLocalCollectionInitializer(
                initializerSyntax,
                pointerType,
                elementConversion,
                initializer,
                patternMethodOpt,
                declType,
                hasErrors);
        }

        private BoundExpression BindAssignment(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.Left != null);
            Debug.Assert(node.Right != null);

            node.Left.CheckDeconstructionCompatibleArgument(diagnostics);

            if (node.Left.Kind() == SyntaxKind.TupleExpression || node.Left.Kind() == SyntaxKind.DeclarationExpression)
            {
                return BindDeconstruction(node, diagnostics);
            }

            BindValueKind lhsKind;
            BindValueKind rhsKind;
            ExpressionSyntax rhsExpr;
            bool isRef = false;

            if (node.Right.Kind() == SyntaxKind.RefExpression)
            {
                isRef = true;
                lhsKind = BindValueKind.RefAssignable;
                rhsKind = BindValueKind.RefersToLocation;
                rhsExpr = ((RefExpressionSyntax)node.Right).Expression;
            }
            else
            {
                lhsKind = BindValueKind.Assignable;
                rhsKind = BindValueKind.RValue;
                rhsExpr = node.Right;
            }

            var op1 = BindValue(node.Left, diagnostics, lhsKind);
            ReportSuppressionIfNeeded(op1, diagnostics);

            var lhsRefKind = RefKind.None;
            // If the LHS is a ref (not ref-readonly), the rhs
            // must also be value-assignable
            if (lhsKind == BindValueKind.RefAssignable && !op1.HasErrors)
            {
                // We should now know that op1 is a valid lvalue
                lhsRefKind = op1.GetRefKind();
                if (lhsRefKind == RefKind.Ref || lhsRefKind == RefKind.Out)
                {
                    rhsKind |= BindValueKind.Assignable;
                }
            }

            var op2 = BindValue(rhsExpr, diagnostics, rhsKind);

            if (op1.Kind == BoundKind.DiscardExpression)
            {
                op2 = BindToNaturalType(op2, diagnostics);
                op1 = InferTypeForDiscardAssignment((BoundDiscardExpression)op1, op2, diagnostics);
            }

            return BindAssignment(node, op1, op2, isRef, diagnostics);
        }

        private BoundExpression InferTypeForDiscardAssignment(BoundDiscardExpression op1, BoundExpression op2, DiagnosticBag diagnostics)
        {
            var inferredType = op2.Type;
            if ((object)inferredType == null)
            {
                return op1.FailInference(this, diagnostics);
            }

            if (inferredType.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_VoidAssignment, op1.Syntax.Location);
            }

            return op1.SetInferredTypeWithAnnotations(TypeWithAnnotations.Create(inferredType));
        }

        private BoundAssignmentOperator BindAssignment(
            SyntaxNode node,
            BoundExpression op1,
            BoundExpression op2,
            bool isRef,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(op1 != null);
            Debug.Assert(op2 != null);

            bool hasErrors = op1.HasAnyErrors || op2.HasAnyErrors;

            if (!op1.HasAnyErrors)
            {
                // Build bound conversion. The node might not be used if this is a dynamic conversion 
                // but diagnostics should be reported anyways.
                var conversion = GenerateConversionForAssignment(op1.Type, op2, diagnostics, isRefAssignment: isRef);

                // If the result is a dynamic assignment operation (SetMember or SetIndex), 
                // don't generate the boxing conversion to the dynamic type.
                // Leave the values as they are, and deal with the conversions at runtime.
                if (op1.Kind != BoundKind.DynamicIndexerAccess &&
                    op1.Kind != BoundKind.DynamicMemberAccess &&
                    op1.Kind != BoundKind.DynamicObjectInitializerMember)
                {
                    op2 = conversion;
                }
                else
                {
                    op2 = BindToNaturalType(op2, diagnostics);
                }

                if (isRef)
                {
                    var leftEscape = GetRefEscape(op1, LocalScopeDepth);
                    var rightEscape = GetRefEscape(op2, LocalScopeDepth);
                    if (leftEscape < rightEscape)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefAssignNarrower, node, op1.ExpressionSymbol.Name, op2.Syntax);
                        op2 = ToBadExpression(op2);
                    }
                }

                if (op1.Type.IsRefLikeType)
                {
                    var leftEscape = GetValEscape(op1, LocalScopeDepth);
                    op2 = ValidateEscape(op2, leftEscape, isByRef: false, diagnostics);
                }
            }
            else
            {
                op2 = BindToTypeForErrorRecovery(op2);
            }

            TypeSymbol type;
            if ((op1.Kind == BoundKind.EventAccess) &&
                ((BoundEventAccess)op1).EventSymbol.IsWindowsRuntimeEvent)
            {
                // Event assignment is a call to void WindowsRuntimeMarshal.AddEventHandler<T>().
                type = this.GetSpecialType(SpecialType.System_Void, diagnostics, node);
            }
            else
            {
                type = op1.Type;
            }

            return new BoundAssignmentOperator(node, op1, op2, isRef, type, hasErrors);
        }

        private static PropertySymbol GetPropertySymbol(BoundExpression expr, out BoundExpression receiver, out SyntaxNode propertySyntax)
        {
            PropertySymbol propertySymbol;
            switch (expr.Kind)
            {
                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)expr;
                        receiver = propertyAccess.ReceiverOpt;
                        propertySymbol = propertyAccess.PropertySymbol;
                    }
                    break;
                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        receiver = indexerAccess.ReceiverOpt;
                        propertySymbol = indexerAccess.Indexer;
                    }
                    break;
                case BoundKind.IndexOrRangePatternIndexerAccess:
                    {
                        var patternIndexer = (BoundIndexOrRangePatternIndexerAccess)expr;
                        receiver = patternIndexer.Receiver;
                        propertySymbol = (PropertySymbol)patternIndexer.PatternSymbol;
                    }
                    break;
                default:
                    receiver = null;
                    propertySymbol = null;
                    propertySyntax = null;
                    return null;
            }

            var syntax = expr.Syntax;
            switch (syntax.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    propertySyntax = ((MemberAccessExpressionSyntax)syntax).Name;
                    break;
                case SyntaxKind.IdentifierName:
                    propertySyntax = syntax;
                    break;
                case SyntaxKind.ElementAccessExpression:
                    propertySyntax = ((ElementAccessExpressionSyntax)syntax).ArgumentList;
                    break;
                default:
                    // Other syntax types, such as QualifiedName,
                    // might occur in invalid code.
                    propertySyntax = syntax;
                    break;
            }

            return propertySymbol;
        }

        private static SyntaxNode GetEventName(BoundEventAccess expr)
        {
            SyntaxNode syntax = expr.Syntax;
            switch (syntax.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)syntax).Name;
                case SyntaxKind.QualifiedName:
                    // This case is reachable only through SemanticModel
                    return ((QualifiedNameSyntax)syntax).Right;
                case SyntaxKind.IdentifierName:
                    return syntax;
                case SyntaxKind.MemberBindingExpression:
                    return ((MemberBindingExpressionSyntax)syntax).Name;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
            }
        }

        /// <summary>
        /// There are two BadEventUsage error codes and this method decides which one should
        /// be used for a given event.
        /// </summary>
        private DiagnosticInfo GetBadEventUsageDiagnosticInfo(EventSymbol eventSymbol)
        {
            var leastOverridden = (EventSymbol)eventSymbol.GetLeastOverriddenMember(this.ContainingType);
            return leastOverridden.HasAssociatedField ?
                new CSDiagnosticInfo(ErrorCode.ERR_BadEventUsage, leastOverridden, leastOverridden.ContainingType) :
                new CSDiagnosticInfo(ErrorCode.ERR_BadEventUsageNoField, leastOverridden);
        }

        internal static bool AccessingAutoPropertyFromConstructor(BoundPropertyAccess propertyAccess, Symbol fromMember)
        {
            return AccessingAutoPropertyFromConstructor(propertyAccess.ReceiverOpt, propertyAccess.PropertySymbol, fromMember);
        }

        private static bool AccessingAutoPropertyFromConstructor(BoundExpression receiver, PropertySymbol propertySymbol, Symbol fromMember)
        {
            if (!propertySymbol.IsDefinition && propertySymbol.ContainingType.Equals(propertySymbol.ContainingType.OriginalDefinition, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
            {
                propertySymbol = propertySymbol.OriginalDefinition;
            }

            var sourceProperty = propertySymbol as SourcePropertySymbol;
            var propertyIsStatic = propertySymbol.IsStatic;

            return (object)sourceProperty != null &&
                    sourceProperty.IsAutoProperty &&
                    TypeSymbol.Equals(sourceProperty.ContainingType, fromMember.ContainingType, TypeCompareKind.ConsiderEverything2) &&
                    IsConstructorOrField(fromMember, isStatic: propertyIsStatic) &&
                    (propertyIsStatic || receiver.Kind == BoundKind.ThisReference);
        }

        private static bool IsConstructorOrField(Symbol member, bool isStatic)
        {
            return (member as MethodSymbol)?.MethodKind == (isStatic ?
                                                                MethodKind.StaticConstructor :
                                                                MethodKind.Constructor) ||
                    (member as FieldSymbol)?.IsStatic == isStatic;
        }

        private TypeSymbol GetAccessThroughType(BoundExpression receiver)
        {
            if (receiver == null)
            {
                return this.ContainingType;
            }
            else if (receiver.Kind == BoundKind.BaseReference)
            {
                // Allow protected access to members defined
                // in base classes. See spec section 3.5.3.
                return null;
            }
            else
            {
                Debug.Assert((object)receiver.Type != null);
                return receiver.Type;
            }
        }

        private BoundExpression BindPossibleArrayInitializer(
            ExpressionSyntax node,
            TypeSymbol destinationType,
            BindValueKind valueKind,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

            if (node.Kind() != SyntaxKind.ArrayInitializerExpression)
            {
                return BindValue(node, diagnostics, valueKind);
            }

            BoundExpression result;
            if (destinationType.Kind == SymbolKind.ArrayType)
            {
                result = BindArrayCreationWithInitializer(diagnostics, null,
                    (InitializerExpressionSyntax)node, (ArrayTypeSymbol)destinationType,
                    ImmutableArray<BoundExpression>.Empty);
            }
            else
            {
                result = BindUnexpectedArrayInitializer((InitializerExpressionSyntax)node, diagnostics, ErrorCode.ERR_ArrayInitToNonArrayType);
            }

            return CheckValue(result, valueKind, diagnostics);
        }

        protected virtual SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return Next.LookupLocal(nameToken);
        }

        protected virtual LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            return Next.LookupLocalFunction(nameToken);
        }

        /// <summary>
        /// Returns a value that tells how many local scopes are visible, including the current.
        /// I.E. outside of any method will be 0
        ///      immediately inside a method - 1
        /// </summary>
        internal virtual uint LocalScopeDepth => Next.LocalScopeDepth;

        internal virtual BoundBlock BindEmbeddedBlock(BlockSyntax node, DiagnosticBag diagnostics)
        {
            return BindBlock(node, diagnostics);
        }

        private BoundBlock BindBlock(BlockSyntax node, DiagnosticBag diagnostics)
        {
            var binder = GetBinder(node);
            Debug.Assert(binder != null);

            return binder.BindBlockParts(node, diagnostics);
        }

        private BoundBlock BindBlockParts(BlockSyntax node, DiagnosticBag diagnostics)
        {
            var syntaxStatements = node.Statements;
            int nStatements = syntaxStatements.Count;

            ArrayBuilder<BoundStatement> boundStatements = ArrayBuilder<BoundStatement>.GetInstance(nStatements);

            for (int i = 0; i < nStatements; i++)
            {
                var boundStatement = BindStatement(syntaxStatements[i], diagnostics);
                boundStatements.Add(boundStatement);
            }

            ImmutableArray<LocalSymbol> locals = GetDeclaredLocalsForScope(node);

            if (IsDirectlyInIterator)
            {
                var method = ContainingMemberOrLambda as MethodSymbol;
                if ((object)method != null)
                {
                    method.IteratorElementTypeWithAnnotations = GetIteratorElementType(null, diagnostics);
                }
                else
                {
                    Debug.Assert(!diagnostics.IsEmptyWithoutResolution);
                }

                foreach (var local in locals)
                {
                    if (local.RefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadIteratorLocalType, local.Locations[0]);
                    }
                }
            }

            return new BoundBlock(
                node,
                locals,
                GetDeclaredLocalFunctionsForScope(node),
                boundStatements.ToImmutableAndFree());
        }

        internal BoundExpression GenerateConversionForAssignment(TypeSymbol targetType, BoundExpression expression, DiagnosticBag diagnostics, bool isDefaultParameter = false, bool isRefAssignment = false)
        {
            Debug.Assert((object)targetType != null);
            Debug.Assert(expression != null);

            // We wish to avoid "cascading" errors, so if the expression we are 
            // attempting to convert to a type had errors, suppress additional
            // diagnostics. However, if the expression 
            // with errors is an unbound lambda then the errors are almost certainly
            // syntax errors. For error recovery analysis purposes we wish to bind
            // error lambdas like "Action<int> f = x=>{ x. };" because IntelliSense
            // needs to know that x is of type int. 

            if (expression.HasAnyErrors && expression.Kind != BoundKind.UnboundLambda)
            {
                diagnostics = new DiagnosticBag();
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = this.Conversions.ClassifyConversionFromExpression(expression, targetType, ref useSiteDiagnostics);
            diagnostics.Add(expression.Syntax, useSiteDiagnostics);

            if (isRefAssignment)
            {
                if (conversion.Kind != ConversionKind.Identity)
                {
                    Error(diagnostics, ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, expression.Syntax, targetType);
                }
                else
                {
                    return expression;
                }
            }
            else if (!conversion.IsImplicit || !conversion.IsValid)
            {
                // We suppress conversion errors on default parameters; eg, 
                // if someone says "void M(string s = 123) {}". We will report
                // a special error in the default parameter binder.

                if (!isDefaultParameter)
                {
                    GenerateImplicitConversionError(diagnostics, expression.Syntax, conversion, expression, targetType);
                }

                // Suppress any additional diagnostics
                diagnostics = new DiagnosticBag();
            }

            return CreateConversion(expression.Syntax, expression, conversion, isCast: false, conversionGroupOpt: null, targetType, diagnostics);
        }

        internal void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, SyntaxNode syntax,
            UnboundLambda anonymousFunction, TypeSymbol targetType)
        {
            Debug.Assert((object)targetType != null);
            Debug.Assert(anonymousFunction != null);

            // Is the target type simply bad?

            // If the target type is an error then we've already reported a diagnostic. Don't bother
            // reporting the conversion error.
            if (targetType.IsErrorType())
            {
                return;
            }

            // CONSIDER: Instead of computing this again, cache the reason why the conversion failed in
            // CONSIDER: the Conversion result, and simply report that.

            var reason = Conversions.IsAnonymousFunctionCompatibleWithType(anonymousFunction, targetType);

            // It is possible that the conversion from lambda to delegate is just fine, and 
            // that we ended up here because the target type, though itself is not an error
            // type, contains a type argument which is an error type. For example, converting
            // (Goo goo)=>{} to Action<Goo> is a perfectly legal conversion even if Goo is undefined!
            // In that case we have already reported an error that Goo is undefined, so just bail out.

            if (reason == LambdaConversionResult.Success)
            {
                return;
            }

            var id = anonymousFunction.MessageID.Localize();

            if (reason == LambdaConversionResult.BadTargetType)
            {
                if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, targetType, node: syntax))
                {
                    return;
                }

                // Cannot convert {0} to type '{1}' because it is not a delegate type
                Error(diagnostics, ErrorCode.ERR_AnonMethToNonDel, syntax, id, targetType);
                return;
            }

            if (reason == LambdaConversionResult.ExpressionTreeMustHaveDelegateTypeArgument)
            {
                Debug.Assert(targetType.IsExpressionTree());
                Error(diagnostics, ErrorCode.ERR_ExpressionTreeMustHaveDelegate, syntax, ((NamedTypeSymbol)targetType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type);
                return;
            }

            if (reason == LambdaConversionResult.ExpressionTreeFromAnonymousMethod)
            {
                Debug.Assert(targetType.IsExpressionTree());
                Error(diagnostics, ErrorCode.ERR_AnonymousMethodToExpressionTree, syntax);
                return;
            }

            // At this point we know that we have either a delegate type or an expression type for the target.

            var delegateType = targetType.GetDelegateType();

            // The target type is a valid delegate or expression tree type. Is there something wrong with the 
            // parameter list?

            // First off, is there a parameter list at all?

            if (reason == LambdaConversionResult.MissingSignatureWithOutParameter)
            {
                // COMPATIBILITY: The C# 4 compiler produces two errors for:
                //
                // delegate void D (out int x);
                // ...
                // D d = delegate {};
                //
                // error CS1676: Parameter 1 must be declared with the 'out' keyword
                // error CS1688: Cannot convert anonymous method block without a parameter list 
                // to delegate type 'D' because it has one or more out parameters
                //
                // This seems redundant, (because there is no "parameter 1" in the source code)
                // and unnecessary. I propose that we eliminate the first error.

                Error(diagnostics, ErrorCode.ERR_CantConvAnonMethNoParams, syntax, targetType);
                return;
            }

            // There is a parameter list. Does it have the right number of elements?

            if (reason == LambdaConversionResult.BadParameterCount)
            {
                // Delegate '{0}' does not take {1} arguments
                Error(diagnostics, ErrorCode.ERR_BadDelArgCount, syntax, delegateType, anonymousFunction.ParameterCount);
                return;
            }

            // The parameter list exists and had the right number of parameters. Were any of its types bad?

            // If any parameter type of the lambda is an error type then suppress 
            // further errors. We've already reported errors on the bad type.
            if (anonymousFunction.HasExplicitlyTypedParameterList)
            {
                for (int i = 0; i < anonymousFunction.ParameterCount; ++i)
                {
                    if (anonymousFunction.ParameterType(i).IsErrorType())
                    {
                        return;
                    }
                }
            }

            // The parameter list exists and had the right number of parameters. Were any of its types
            // mismatched with the delegate parameter types?

            // The simplest possible case is (x, y, z)=>whatever where the target type has a ref or out parameter.

            var delegateParameters = delegateType.DelegateParameters();
            if (reason == LambdaConversionResult.RefInImplicitlyTypedLambda)
            {
                for (int i = 0; i < anonymousFunction.ParameterCount; ++i)
                {
                    var delegateRefKind = delegateParameters[i].RefKind;
                    if (delegateRefKind != RefKind.None)
                    {
                        // Parameter {0} must be declared with the '{1}' keyword
                        Error(diagnostics, ErrorCode.ERR_BadParamRef, anonymousFunction.ParameterLocation(i),
                            i + 1, delegateRefKind.ToParameterDisplayString());
                    }
                }
                return;
            }

            // See the comments in IsAnonymousFunctionCompatibleWithDelegate for an explanation of this one.
            if (reason == LambdaConversionResult.StaticTypeInImplicitlyTypedLambda)
            {
                for (int i = 0; i < anonymousFunction.ParameterCount; ++i)
                {
                    if (delegateParameters[i].TypeWithAnnotations.IsStatic)
                    {
                        // {0}: Static types cannot be used as parameter
                        Error(diagnostics, ErrorCode.ERR_ParameterIsStaticClass, anonymousFunction.ParameterLocation(i), delegateParameters[i].Type);
                    }
                }
                return;
            }

            // Otherwise, there might be a more complex reason why the parameter types are mismatched.

            if (reason == LambdaConversionResult.MismatchedParameterType)
            {
                // Cannot convert {0} to type '{1}' because the parameter types do not match the delegate parameter types
                Error(diagnostics, ErrorCode.ERR_CantConvAnonMethParams, syntax, id, targetType);
                Debug.Assert(anonymousFunction.ParameterCount == delegateParameters.Length);
                for (int i = 0; i < anonymousFunction.ParameterCount; ++i)
                {
                    var lambdaParameterType = anonymousFunction.ParameterType(i);
                    if (lambdaParameterType.IsErrorType())
                    {
                        continue;
                    }

                    var lambdaParameterLocation = anonymousFunction.ParameterLocation(i);
                    var lambdaRefKind = anonymousFunction.RefKind(i);
                    var delegateParameterType = delegateParameters[i].Type;
                    var delegateRefKind = delegateParameters[i].RefKind;

                    if (!lambdaParameterType.Equals(delegateParameterType, TypeCompareKind.AllIgnoreOptions))
                    {
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, lambdaParameterType, delegateParameterType);

                        // Parameter {0} is declared as type '{1}{2}' but should be '{3}{4}'
                        Error(diagnostics, ErrorCode.ERR_BadParamType, lambdaParameterLocation,
                            i + 1, lambdaRefKind.ToParameterPrefix(), distinguisher.First, delegateRefKind.ToParameterPrefix(), distinguisher.Second);
                    }
                    else if (lambdaRefKind != delegateRefKind)
                    {
                        if (delegateRefKind == RefKind.None)
                        {
                            // Parameter {0} should not be declared with the '{1}' keyword
                            Error(diagnostics, ErrorCode.ERR_BadParamExtraRef, lambdaParameterLocation, i + 1, lambdaRefKind.ToParameterDisplayString());
                        }
                        else
                        {
                            // Parameter {0} must be declared with the '{1}' keyword
                            Error(diagnostics, ErrorCode.ERR_BadParamRef, lambdaParameterLocation, i + 1, delegateRefKind.ToParameterDisplayString());
                        }
                    }
                }
                return;
            }

            if (reason == LambdaConversionResult.BindingFailed)
            {
                var bindingResult = anonymousFunction.Bind(delegateType);
                Debug.Assert(ErrorFacts.PreventsSuccessfulDelegateConversion(bindingResult.Diagnostics));
                diagnostics.AddRange(bindingResult.Diagnostics);
                return;
            }

            // UNDONE: LambdaConversionResult.VoidExpressionLambdaMustBeStatementExpression:

            Debug.Assert(false, "Missing case in lambda conversion error reporting");
            diagnostics.Add(ErrorCode.ERR_InternalError, syntax.Location);
        }

        protected static void GenerateImplicitConversionError(DiagnosticBag diagnostics, Compilation compilation, SyntaxNode syntax,
            Conversion conversion, TypeSymbol sourceType, TypeSymbol targetType, ConstantValue sourceConstantValueOpt = null)
        {
            Debug.Assert(!conversion.IsImplicit || !conversion.IsValid);

            // If the either type is an error then an error has already been reported
            // for some aspect of the analysis of this expression. (For example, something like
            // "garbage g = null; short s = g;" -- we don't want to report that g is not
            // convertible to short because we've already reported that g does not have a good type.
            if (!sourceType.IsErrorType() && !targetType.IsErrorType())
            {
                if (conversion.IsExplicit)
                {
                    if (sourceType.SpecialType == SpecialType.System_Double && syntax.Kind() == SyntaxKind.NumericLiteralExpression &&
                        (targetType.SpecialType == SpecialType.System_Single || targetType.SpecialType == SpecialType.System_Decimal))
                    {
                        Error(diagnostics, ErrorCode.ERR_LiteralDoubleCast, syntax, (targetType.SpecialType == SpecialType.System_Single) ? "F" : "M", targetType);
                    }
                    else if (conversion.Kind == ConversionKind.ExplicitNumeric && sourceConstantValueOpt != null && sourceConstantValueOpt != ConstantValue.Bad &&
                        ConversionsBase.HasImplicitConstantExpressionConversion(new BoundLiteral(syntax, ConstantValue.Bad, sourceType), targetType))
                    {
                        // CLEVERNESS: By passing ConstantValue.Bad, we tell HasImplicitConstantExpressionConversion to ignore the constant
                        // value and only consider the types.

                        // If there would be an implicit constant conversion for a different constant of the same type 
                        // (i.e. one that's not out of range), then it's more helpful to report the range check failure
                        // than to suggest inserting a cast.
                        Error(diagnostics, ErrorCode.ERR_ConstOutOfRange, syntax, sourceConstantValueOpt.Value, targetType);
                    }
                    else
                    {
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, sourceType, targetType);
                        Error(diagnostics, ErrorCode.ERR_NoImplicitConvCast, syntax, distinguisher.First, distinguisher.Second);
                    }
                }
                else if (conversion.ResultKind == LookupResultKind.OverloadResolutionFailure)
                {
                    Debug.Assert(conversion.IsUserDefined);

                    ImmutableArray<MethodSymbol> originalUserDefinedConversions = conversion.OriginalUserDefinedConversions;
                    if (originalUserDefinedConversions.Length > 1)
                    {
                        Error(diagnostics, ErrorCode.ERR_AmbigUDConv, syntax, originalUserDefinedConversions[0], originalUserDefinedConversions[1], sourceType, targetType);
                    }
                    else
                    {
                        Debug.Assert(originalUserDefinedConversions.Length == 0,
                            "How can there be exactly one applicable user-defined conversion if the conversion doesn't exist?");
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, sourceType, targetType);
                        Error(diagnostics, ErrorCode.ERR_NoImplicitConv, syntax, distinguisher.First, distinguisher.Second);
                    }
                }
                else if (TypeSymbol.Equals(sourceType, targetType, TypeCompareKind.ConsiderEverything2))
                {
                    // This occurs for `void`, which cannot even convert to itself. Since SymbolDistinguisher
                    // requires two distinct types, we preempt its use here. The diagnostic is strange, but correct.
                    // Though this diagnostic tends to be a cascaded one, we cannot suppress it until
                    // we have proven that it is always so.
                    Error(diagnostics, ErrorCode.ERR_NoImplicitConv, syntax, sourceType, targetType);
                }
                else
                {
                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, sourceType, targetType);
                    Error(diagnostics, ErrorCode.ERR_NoImplicitConv, syntax, distinguisher.First, distinguisher.Second);
                }
            }
        }

        protected void GenerateImplicitConversionError(
            DiagnosticBag diagnostics,
            SyntaxNode syntax,
            Conversion conversion,
            BoundExpression operand,
            TypeSymbol targetType)
        {
            Debug.Assert(operand != null);
            Debug.Assert((object)targetType != null);

            if (targetType.TypeKind == TypeKind.Error)
            {
                return;
            }

            switch (operand.Kind)
            {
                case BoundKind.BadExpression:
                    {
                        return;
                    }
                case BoundKind.UnboundLambda:
                    {
                        GenerateAnonymousFunctionConversionError(diagnostics, syntax, (UnboundLambda)operand, targetType);
                        return;
                    }
                case BoundKind.TupleLiteral:
                    {
                        var tuple = (BoundTupleLiteral)operand;
                        var targetElementTypes = default(ImmutableArray<TypeWithAnnotations>);

                        // If target is a tuple or compatible type with the same number of elements,
                        // report errors for tuple arguments that failed to convert, which would be more useful.
                        if (targetType.TryGetElementTypesWithAnnotationsIfTupleOrCompatible(out targetElementTypes) &&
                            targetElementTypes.Length == tuple.Arguments.Length)
                        {
                            GenerateImplicitConversionErrorsForTupleLiteralArguments(diagnostics, tuple.Arguments, targetElementTypes);
                            return;
                        }

                        // target is not compatible with source and source does not have a type
                        if ((object)tuple.Type == null)
                        {
                            Error(diagnostics, ErrorCode.ERR_ConversionNotTupleCompatible, syntax, tuple.Arguments.Length, targetType);
                            return;
                        }

                        // Otherwise it is just a regular conversion failure from T1 to T2.
                        break;
                    }
                case BoundKind.MethodGroup:
                    {
                        var methodGroup = (BoundMethodGroup)operand;
                        if (!Conversions.ReportDelegateMethodGroupDiagnostics(this, methodGroup, targetType, diagnostics))
                        {
                            var nodeForSquiggle = syntax;
                            while (nodeForSquiggle.Kind() == SyntaxKind.ParenthesizedExpression)
                            {
                                nodeForSquiggle = ((ParenthesizedExpressionSyntax)nodeForSquiggle).Expression;
                            }

                            if (nodeForSquiggle.Kind() == SyntaxKind.SimpleMemberAccessExpression || nodeForSquiggle.Kind() == SyntaxKind.PointerMemberAccessExpression)
                            {
                                nodeForSquiggle = ((MemberAccessExpressionSyntax)nodeForSquiggle).Name;
                            }

                            var location = nodeForSquiggle.Location;

                            if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, targetType, location))
                            {
                                return;
                            }

                            Error(diagnostics,
                                targetType.IsDelegateType() ? ErrorCode.ERR_MethDelegateMismatch : ErrorCode.ERR_MethGrpToNonDel,
                                location, methodGroup.Name, targetType);
                        }

                        return;
                    }
                case BoundKind.Literal:
                    {
                        if (operand.IsLiteralNull())
                        {
                            if (targetType.TypeKind == TypeKind.TypeParameter)
                            {
                                Error(diagnostics, ErrorCode.ERR_TypeVarCantBeNull, syntax, targetType);
                                return;
                            }
                            if (targetType.IsValueType)
                            {
                                Error(diagnostics, ErrorCode.ERR_ValueCantBeNull, syntax, targetType);
                                return;
                            }
                        }
                        break;
                    }
                case BoundKind.StackAllocArrayCreation:
                    {
                        var stackAllocExpression = (BoundStackAllocArrayCreation)operand;
                        Error(diagnostics, ErrorCode.ERR_StackAllocConversionNotPossible, syntax, stackAllocExpression.ElementType, targetType);
                        return;
                    }
                case BoundKind.UnconvertedSwitchExpression:
                    {
                        var switchExpression = (BoundUnconvertedSwitchExpression)operand;
                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        bool reportedError = false;
                        foreach (var arm in switchExpression.SwitchArms)
                        {
                            var armConversion = this.Conversions.ClassifyImplicitConversionFromExpression(arm.Value, targetType, ref useSiteDiagnostics);
                            if (!armConversion.IsImplicit)
                            {
                                GenerateImplicitConversionError(diagnostics, arm.Value.Syntax, conversion, arm.Value, targetType);
                                reportedError = true;
                            }
                        }

                        Debug.Assert(reportedError);
                        return;
                    }
            }

            var sourceType = operand.Type;
            if ((object)sourceType != null)
            {
                GenerateImplicitConversionError(diagnostics, this.Compilation, syntax, conversion, sourceType, targetType, operand.ConstantValue);
                return;
            }

            Debug.Assert(operand.HasAnyErrors && operand.Kind != BoundKind.UnboundLambda, "Missing a case in implicit conversion error reporting");
        }

        private void GenerateImplicitConversionErrorsForTupleLiteralArguments(
            DiagnosticBag diagnostics,
            ImmutableArray<BoundExpression> tupleArguments,
            ImmutableArray<TypeWithAnnotations> targetElementTypes)
        {
            var argLength = tupleArguments.Length;

            // report all leaf elements of the tuple literal that failed to convert
            // NOTE: we are not responsible for reporting use site errors here, just the failed leaf conversions.
            // By the time we get here we have done analysis and know we have failed the cast in general, and diagnostics collected in the process is already in the bag. 
            // The only thing left is to form a diagnostics about the actually failing conversion(s).
            // This whole method does not itself collect any usesite diagnostics. Its only purpose is to produce an error better than "conversion failed here"           
            HashSet<DiagnosticInfo> usDiagsUnused = null;

            for (int i = 0; i < targetElementTypes.Length; i++)
            {
                var argument = tupleArguments[i];
                var targetElementType = targetElementTypes[i].Type;

                var elementConversion = Conversions.ClassifyImplicitConversionFromExpression(argument, targetElementType, ref usDiagsUnused);
                if (!elementConversion.IsValid)
                {
                    GenerateImplicitConversionError(diagnostics, argument.Syntax, elementConversion, argument, targetElementType);
                }
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax node, DiagnosticBag diagnostics)
        {
            var condition = BindBooleanExpression(node.Condition, diagnostics);
            var consequence = BindPossibleEmbeddedStatement(node.Statement, diagnostics);
            BoundStatement alternative = (node.Else == null) ? null : BindPossibleEmbeddedStatement(node.Else.Statement, diagnostics);

            BoundStatement result = new BoundIfStatement(node, condition, consequence, alternative);
            return result;
        }

        internal BoundExpression BindBooleanExpression(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // SPEC: 
            // A boolean-expression is an expression that yields a result of type bool; 
            // either directly or through application of operator true in certain 
            // contexts as specified in the following.
            //
            // The controlling conditional expression of an if-statement, while-statement, 
            // do-statement, or for-statement is a boolean-expression. The controlling 
            // conditional expression of the ?: operator follows the same rules as a 
            // boolean-expression, but for reasons of operator precedence is classified
            // as a conditional-or-expression.
            //
            // A boolean-expression is required to be implicitly convertible to bool 
            // or of a type that implements operator true. If neither requirement 
            // is satisfied, a binding-time error occurs.
            //
            // When a boolean expression cannot be implicitly converted to bool but does 
            // implement operator true, then following evaluation of the expression, 
            // the operator true implementation provided by that type is invoked 
            // to produce a bool value.
            //
            // SPEC ERROR: The third paragraph above is obviously not correct; we need
            // SPEC ERROR: to do more than just check to see whether the type implements
            // SPEC ERROR: operator true. First off, the type could implement the operator
            // SPEC ERROR: several times: if it is a struct then it could implement it
            // SPEC ERROR: twice, to take both nullable and non-nullable arguments, and
            // SPEC ERROR: if it is a class or type parameter then it could have several
            // SPEC ERROR: implementations on its base classes or effective base classes.
            // SPEC ERROR: Second, the type of the argument could be S? where S implements
            // SPEC ERROR: operator true(S?); we want to look at S, not S?, when looking
            // SPEC ERROR: for applicable candidates.
            //
            // SPEC ERROR: Basically, the spec should say "use unary operator overload resolution
            // SPEC ERROR: to find the candidate set and choose a unique best operator true".

            var expr = BindValue(node, diagnostics, BindValueKind.RValue);
            var boolean = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);

            if (expr.HasAnyErrors)
            {
                // The expression could not be bound. Insert a fake conversion
                // around it to bool and keep on going.
                // NOTE: no user-defined conversion candidates.
                return BoundConversion.Synthesized(node, expr, Conversion.NoConversion, false, explicitCastInCode: false, conversionGroupOpt: null, ConstantValue.NotAvailable, boolean, hasErrors: true);
            }

            // Oddly enough, "if(dyn)" is bound not as a dynamic conversion to bool, but as a dynamic
            // invocation of operator true.

            if (expr.HasDynamicType())
            {
                return new BoundUnaryOperator(
                    node,
                    UnaryOperatorKind.DynamicTrue,
                    BindToNaturalType(expr, diagnostics),
                    ConstantValue.NotAvailable,
                    null,
                    LookupResultKind.Viable,
                    boolean)
                {
                    WasCompilerGenerated = true
                };
            }

            // Is the operand implicitly convertible to bool?

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = this.Conversions.ClassifyConversionFromExpression(expr, boolean, ref useSiteDiagnostics);
            diagnostics.Add(expr.Syntax, useSiteDiagnostics);

            if (conversion.IsImplicit)
            {
                if (conversion.Kind == ConversionKind.Identity)
                {
                    // Check to see if we're assigning a boolean literal in a place where an
                    // equality check would be more conventional.
                    // NOTE: Don't do this check unless the expression will be returned
                    // without being wrapped in another bound node (i.e. identity conversion).
                    if (expr.Kind == BoundKind.AssignmentOperator)
                    {
                        var assignment = (BoundAssignmentOperator)expr;
                        if (assignment.Right.Kind == BoundKind.Literal && assignment.Right.ConstantValue.Discriminator == ConstantValueTypeDiscriminator.Boolean)
                        {
                            Error(diagnostics, ErrorCode.WRN_IncorrectBooleanAssg, assignment.Syntax);
                        }
                    }
                }

                return CreateConversion(
                    syntax: expr.Syntax,
                    source: expr,
                    conversion: conversion,
                    isCast: false,
                    conversionGroupOpt: null,
                    wasCompilerGenerated: true,
                    destination: boolean,
                    diagnostics: diagnostics);
            }

            // It was not. Does it implement operator true?

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            var best = this.UnaryOperatorOverloadResolution(UnaryOperatorKind.True, expr, node, diagnostics, out resultKind, out originalUserDefinedOperators);
            if (!best.HasValue)
            {
                // No. Give a "not convertible to bool" error.
                Debug.Assert(resultKind == LookupResultKind.Empty, "How could overload resolution fail if a user-defined true operator was found?");
                Debug.Assert(originalUserDefinedOperators.IsEmpty, "How could overload resolution fail if a user-defined true operator was found?");
                GenerateImplicitConversionError(diagnostics, node, conversion, expr, boolean);
                return BoundConversion.Synthesized(node, expr, Conversion.NoConversion, false, explicitCastInCode: false, conversionGroupOpt: null, ConstantValue.NotAvailable, boolean, hasErrors: true);
            }

            UnaryOperatorSignature signature = best.Signature;

            BoundExpression resultOperand = CreateConversion(
                node,
                expr,
                best.Conversion,
                isCast: false,
                conversionGroupOpt: null,
                destination: best.Signature.OperandType,
                diagnostics: diagnostics);

            // Consider op_true to be compiler-generated so that it doesn't appear in the semantic model.
            // UNDONE: If we decide to expose the operator in the semantic model, we'll have to remove the 
            // WasCompilerGenerated flag (and possibly suppress the symbol in specific APIs).
            return new BoundUnaryOperator(node, signature.Kind, resultOperand, ConstantValue.NotAvailable, signature.Method, resultKind, originalUserDefinedOperators, signature.ReturnType)
            {
                WasCompilerGenerated = true
            };
        }

        private BoundStatement BindSwitchStatement(SwitchStatementSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Binder switchBinder = this.GetBinder(node);
            return switchBinder.BindSwitchStatementCore(node, switchBinder, diagnostics);
        }

        internal virtual BoundStatement BindSwitchStatementCore(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            return this.Next.BindSwitchStatementCore(node, originalBinder, diagnostics);
        }

        internal virtual void BindPatternSwitchLabelForInference(CasePatternSwitchLabelSyntax node, DiagnosticBag diagnostics)
        {
            this.Next.BindPatternSwitchLabelForInference(node, diagnostics);
        }

        private BoundStatement BindWhile(WhileStatementSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

            var loopBinder = this.GetBinder(node);
            Debug.Assert(loopBinder != null);
            return loopBinder.BindWhileParts(diagnostics, loopBinder);
        }

        internal virtual BoundWhileStatement BindWhileParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindWhileParts(diagnostics, originalBinder);
        }

        private BoundStatement BindDo(DoStatementSyntax node, DiagnosticBag diagnostics)
        {
            var loopBinder = this.GetBinder(node);
            Debug.Assert(loopBinder != null);

            return loopBinder.BindDoParts(diagnostics, loopBinder);
        }

        internal virtual BoundDoStatement BindDoParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindDoParts(diagnostics, originalBinder);
        }

        internal BoundForStatement BindFor(ForStatementSyntax node, DiagnosticBag diagnostics)
        {
            var loopBinder = this.GetBinder(node);
            Debug.Assert(loopBinder != null);
            return loopBinder.BindForParts(diagnostics, loopBinder);
        }

        internal virtual BoundForStatement BindForParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindForParts(diagnostics, originalBinder);
        }

        internal BoundStatement BindForOrUsingOrFixedDeclarations(VariableDeclarationSyntax nodeOpt, LocalDeclarationKind localKind, DiagnosticBag diagnostics, out ImmutableArray<BoundLocalDeclaration> declarations)
        {
            if (nodeOpt == null)
            {
                declarations = ImmutableArray<BoundLocalDeclaration>.Empty;
                return null;
            }

            var typeSyntax = nodeOpt.Type;
            // Fixed and using variables are not allowed to be ref-like, but regular variables are
            if (localKind == LocalDeclarationKind.RegularVariable)
            {
                typeSyntax = typeSyntax.SkipRef(out _);
            }

            AliasSymbol alias;
            bool isVar;
            TypeWithAnnotations declType = BindTypeWithAnnotationsOrVarKeyword(typeSyntax, diagnostics, out isVar, out alias);

            Debug.Assert(declType.HasType || isVar);

            var variables = nodeOpt.Variables;
            int count = variables.Count;
            Debug.Assert(count > 0);

            if (isVar && count > 1)
            {
                // There are a number of ways in which a var decl can be illegal, but in these 
                // cases we should report an error and then keep right on going with the inference.

                Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, nodeOpt);
            }

            var declarationArray = new BoundLocalDeclaration[count];

            for (int i = 0; i < count; i++)
            {
                var variableDeclarator = variables[i];
                bool includeBoundType = i == 0; //To avoid duplicated expressions, only the first declaration should contain the bound type.
                var declaration = BindVariableDeclaration(localKind, isVar, variableDeclarator, typeSyntax, declType, alias, diagnostics, includeBoundType);

                declarationArray[i] = declaration;
            }

            declarations = declarationArray.AsImmutableOrNull();

            return (count == 1) ?
                (BoundStatement)declarations[0] :
                new BoundMultipleLocalDeclarations(nodeOpt, declarations);
        }

        internal BoundStatement BindStatementExpressionList(SeparatedSyntaxList<ExpressionSyntax> statements, DiagnosticBag diagnostics)
        {
            int count = statements.Count;
            if (count == 0)
            {
                return null;
            }
            else if (count == 1)
            {
                var syntax = statements[0];
                return BindExpressionStatement(syntax, syntax, false, diagnostics);
            }
            else
            {
                var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                for (int i = 0; i < count; i++)
                {
                    var syntax = statements[i];
                    var statement = BindExpressionStatement(syntax, syntax, false, diagnostics);
                    statementBuilder.Add(statement);
                }
                return BoundStatementList.Synthesized(statements.Node, statementBuilder.ToImmutableAndFree());
            }
        }

        private BoundStatement BindForEach(CommonForEachStatementSyntax node, DiagnosticBag diagnostics)
        {
            Binder loopBinder = this.GetBinder(node);
            return this.GetBinder(node.Expression).WrapWithVariablesIfAny(node.Expression, loopBinder.BindForEachParts(diagnostics, loopBinder));
        }

        internal virtual BoundStatement BindForEachParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindForEachParts(diagnostics, originalBinder);
        }

        /// <summary>
        /// Like BindForEachParts, but only bind the deconstruction part of the foreach, for purpose of inferring the types of the declared locals.
        /// </summary>
        internal virtual BoundStatement BindForEachDeconstruction(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindForEachDeconstruction(diagnostics, originalBinder);
        }

        private BoundStatement BindBreak(BreakStatementSyntax node, DiagnosticBag diagnostics)
        {
            var target = this.BreakLabel;
            if ((object)target == null)
            {
                Error(diagnostics, ErrorCode.ERR_NoBreakOrCont, node);
                return new BoundBadStatement(node, ImmutableArray<BoundNode>.Empty, hasErrors: true);
            }
            return new BoundBreakStatement(node, target);
        }

        private BoundStatement BindContinue(ContinueStatementSyntax node, DiagnosticBag diagnostics)
        {
            var target = this.ContinueLabel;
            if ((object)target == null)
            {
                Error(diagnostics, ErrorCode.ERR_NoBreakOrCont, node);
                return new BoundBadStatement(node, ImmutableArray<BoundNode>.Empty, hasErrors: true);
            }
            return new BoundContinueStatement(node, target);
        }

        private static SwitchBinder GetSwitchBinder(Binder binder)
        {
            SwitchBinder switchBinder = binder as SwitchBinder;
            while (binder != null && switchBinder == null)
            {
                binder = binder.Next;
                switchBinder = binder as SwitchBinder;
            }
            return switchBinder;
        }

        protected static bool IsInAsyncMethod(MethodSymbol method)
        {
            return (object)method != null && method.IsAsync;
        }

        protected bool IsInAsyncMethod()
        {
            return IsInAsyncMethod(this.ContainingMemberOrLambda as MethodSymbol);
        }

        protected bool IsTaskReturningAsyncMethod()
        {
            var symbol = this.ContainingMemberOrLambda;
            return symbol?.Kind == SymbolKind.Method && ((MethodSymbol)symbol).IsTaskReturningAsync(this.Compilation);
        }

        protected bool IsGenericTaskReturningAsyncMethod()
        {
            var symbol = this.ContainingMemberOrLambda;
            return symbol?.Kind == SymbolKind.Method && ((MethodSymbol)symbol).IsGenericTaskReturningAsync(this.Compilation);
        }

        protected bool IsIAsyncEnumerableOrIAsyncEnumeratorReturningAsyncMethod()
        {
            var symbol = this.ContainingMemberOrLambda;
            if (symbol?.Kind == SymbolKind.Method)
            {
                var method = (MethodSymbol)symbol;
                return method.IsIAsyncEnumerableReturningAsync(this.Compilation) ||
                    method.IsIAsyncEnumeratorReturningAsync(this.Compilation);
            }
            return false;
        }

        protected virtual TypeSymbol GetCurrentReturnType(out RefKind refKind)
        {
            var symbol = this.ContainingMemberOrLambda as MethodSymbol;
            if ((object)symbol != null)
            {
                refKind = symbol.RefKind;

                TypeSymbol returnType = symbol.ReturnType;

                if ((object)returnType == LambdaSymbol.ReturnTypeIsBeingInferred)
                {
                    return null;
                }

                return returnType;
            }

            refKind = RefKind.None;
            return null;
        }

        private BoundStatement BindReturn(ReturnStatementSyntax syntax, DiagnosticBag diagnostics)
        {
            var refKind = RefKind.None;
            var expressionSyntax = syntax.Expression?.CheckAndUnwrapRefExpression(diagnostics, out refKind);
            BoundExpression arg = null;
            if (expressionSyntax != null)
            {
                BindValueKind requiredValueKind = GetRequiredReturnValueKind(refKind);
                arg = BindValue(expressionSyntax, diagnostics, requiredValueKind);
            }
            else
            {
                // If this is a void return statement in a script, return default(T).
                var interactiveInitializerMethod = this.ContainingMemberOrLambda as SynthesizedInteractiveInitializerMethod;
                if (interactiveInitializerMethod != null)
                {
                    arg = new BoundDefaultExpression(interactiveInitializerMethod.GetNonNullSyntaxNode(), interactiveInitializerMethod.ResultType);
                }
            }

            RefKind sigRefKind;
            TypeSymbol retType = GetCurrentReturnType(out sigRefKind);

            bool hasErrors = false;
            if (IsDirectlyInIterator)
            {
                diagnostics.Add(ErrorCode.ERR_ReturnInIterator, syntax.ReturnKeyword.GetLocation());
                hasErrors = true;
            }
            else if (IsInAsyncMethod())
            {
                if (refKind != RefKind.None)
                {
                    // This can happen if we are binding an async anonymous method to a delegate type.
                    diagnostics.Add(ErrorCode.ERR_MustNotHaveRefReturn, syntax.ReturnKeyword.GetLocation());
                    hasErrors = true;
                }
                else if (IsIAsyncEnumerableOrIAsyncEnumeratorReturningAsyncMethod())
                {
                    diagnostics.Add(ErrorCode.ERR_ReturnInIterator, syntax.ReturnKeyword.GetLocation());
                    hasErrors = true;
                }
            }
            else if ((object)retType != null && (refKind != RefKind.None) != (sigRefKind != RefKind.None))
            {
                var errorCode = refKind != RefKind.None
                    ? ErrorCode.ERR_MustNotHaveRefReturn
                    : ErrorCode.ERR_MustHaveRefReturn;
                diagnostics.Add(errorCode, syntax.ReturnKeyword.GetLocation());
                hasErrors = true;
            }

            if (arg != null)
            {
                hasErrors |= arg.HasErrors || ((object)arg.Type != null && arg.Type.IsErrorType());
            }

            if (hasErrors)
            {
                return new BoundReturnStatement(syntax, refKind, BindToTypeForErrorRecovery(arg), hasErrors: true);
            }

            // The return type could be null; we might be attempting to infer the return type either 
            // because of method type inference, or because we are attempting to do error analysis 
            // on a lambda expression of unknown return type.
            if ((object)retType != null)
            {
                if (retType.IsVoidType() || IsTaskReturningAsyncMethod())
                {
                    if (arg != null)
                    {
                        var container = this.ContainingMemberOrLambda;
                        var lambda = container as LambdaSymbol;
                        if ((object)lambda != null)
                        {
                            // Error case: void-returning or async task-returning method or lambda with "return x;" 
                            var errorCode = retType.IsVoidType()
                                ? ErrorCode.ERR_RetNoObjectRequiredLambda
                                : ErrorCode.ERR_TaskRetNoObjectRequiredLambda;

                            // Anonymous function converted to a void returning delegate cannot return a value
                            Error(diagnostics, errorCode, syntax.ReturnKeyword);
                            hasErrors = true;

                            // COMPATIBILITY: The native compiler also produced an error
                            // COMPATIBILITY: "Cannot convert lambda expression to delegate type 'Action' because some of the
                            // COMPATIBILITY: return types in the block are not implicitly convertible to the delegate return type"
                            // COMPATIBILITY: This error doesn't make sense in the "void" case because the whole idea of 
                            // COMPATIBILITY: "conversion to void" is a bit unusual, and we've already given a good error.
                        }
                        else
                        {
                            // Error case: void-returning or async task-returning method or lambda with "return x;" 
                            var errorCode = retType.IsVoidType()
                                ? ErrorCode.ERR_RetNoObjectRequired
                                : ErrorCode.ERR_TaskRetNoObjectRequired;

                            Error(diagnostics, errorCode, syntax.ReturnKeyword, container);
                            hasErrors = true;
                        }
                    }
                }
                else
                {
                    if (arg == null)
                    {
                        // Error case: non-void-returning or Task<T>-returning method or lambda but just have "return;"
                        var requiredType = IsGenericTaskReturningAsyncMethod()
                            ? retType.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single()
                            : retType;

                        Error(diagnostics, ErrorCode.ERR_RetObjectRequired, syntax.ReturnKeyword, requiredType);
                        hasErrors = true;
                    }
                    else
                    {
                        arg = CreateReturnConversion(syntax, diagnostics, arg, sigRefKind, retType);
                        arg = ValidateEscape(arg, Binder.ExternalScope, refKind != RefKind.None, diagnostics);
                    }
                }
            }
            else
            {
                // Check that the returned expression is not void.
                if ((object)arg?.Type != null && arg.Type.IsVoidType())
                {
                    Error(diagnostics, ErrorCode.ERR_CantReturnVoid, expressionSyntax);
                    hasErrors = true;
                }
            }

            return new BoundReturnStatement(syntax, refKind, hasErrors ? BindToTypeForErrorRecovery(arg) : arg, hasErrors);
        }

        internal BoundExpression CreateReturnConversion(
            SyntaxNode syntax,
            DiagnosticBag diagnostics,
            BoundExpression argument,
            RefKind returnRefKind,
            TypeSymbol returnType)
        {
            // If the return type is not void then the expression must be implicitly convertible.

            Conversion conversion;
            bool badAsyncReturnAlreadyReported = false;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (IsInAsyncMethod())
            {
                Debug.Assert(returnRefKind == RefKind.None);

                if (!IsGenericTaskReturningAsyncMethod())
                {
                    conversion = Conversion.NoConversion;
                    badAsyncReturnAlreadyReported = true;
                }
                else
                {
                    returnType = returnType.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
                    conversion = this.Conversions.ClassifyConversionFromExpression(argument, returnType, ref useSiteDiagnostics);
                }
            }
            else
            {
                conversion = this.Conversions.ClassifyConversionFromExpression(argument, returnType, ref useSiteDiagnostics);
            }

            diagnostics.Add(syntax, useSiteDiagnostics);

            if (!argument.HasAnyErrors)
            {
                if (returnRefKind != RefKind.None)
                {
                    if (conversion.Kind != ConversionKind.Identity)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnMustHaveIdentityConversion, argument.Syntax, returnType);
                        argument = argument.WithHasErrors();
                    }
                    else
                    {
                        return BindToNaturalType(argument, diagnostics);
                    }
                }
                else if (!conversion.IsImplicit || !conversion.IsValid)
                {
                    if (!badAsyncReturnAlreadyReported)
                    {
                        RefKind unusedRefKind;
                        if (IsGenericTaskReturningAsyncMethod() && TypeSymbol.Equals(argument.Type, this.GetCurrentReturnType(out unusedRefKind), TypeCompareKind.ConsiderEverything2))
                        {
                            // Since this is an async method, the return expression must be of type '{0}' rather than 'Task<{0}>'
                            Error(diagnostics, ErrorCode.ERR_BadAsyncReturnExpression, argument.Syntax, returnType);
                        }
                        else
                        {
                            GenerateImplicitConversionError(diagnostics, argument.Syntax, conversion, argument, returnType);
                            if (this.ContainingMemberOrLambda is LambdaSymbol)
                            {
                                ReportCantConvertLambdaReturn(argument.Syntax, diagnostics);
                            }
                        }
                    }
                }
            }

            return CreateConversion(argument.Syntax, argument, conversion, isCast: false, conversionGroupOpt: null, returnType, diagnostics);
        }

        private BoundTryStatement BindTryStatement(TryStatementSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

            var tryBlock = BindEmbeddedBlock(node.Block, diagnostics);
            var catchBlocks = BindCatchBlocks(node.Catches, diagnostics);
            var finallyBlockOpt = (node.Finally != null) ? BindEmbeddedBlock(node.Finally.Block, diagnostics) : null;
            return new BoundTryStatement(node, tryBlock, catchBlocks, finallyBlockOpt);
        }

        private ImmutableArray<BoundCatchBlock> BindCatchBlocks(SyntaxList<CatchClauseSyntax> catchClauses, DiagnosticBag diagnostics)
        {
            int n = catchClauses.Count;
            if (n == 0)
            {
                return ImmutableArray<BoundCatchBlock>.Empty;
            }

            var catchBlocks = ArrayBuilder<BoundCatchBlock>.GetInstance(n);
            var hasCatchAll = false;

            foreach (var catchSyntax in catchClauses)
            {
                if (hasCatchAll)
                {
                    diagnostics.Add(ErrorCode.ERR_TooManyCatches, catchSyntax.CatchKeyword.GetLocation());
                }

                var catchBinder = this.GetBinder(catchSyntax);
                var catchBlock = catchBinder.BindCatchBlock(catchSyntax, catchBlocks, diagnostics);
                catchBlocks.Add(catchBlock);

                hasCatchAll |= catchSyntax.Declaration == null && catchSyntax.Filter == null;
            }
            return catchBlocks.ToImmutableAndFree();
        }

        private BoundCatchBlock BindCatchBlock(CatchClauseSyntax node, ArrayBuilder<BoundCatchBlock> previousBlocks, DiagnosticBag diagnostics)
        {
            bool hasError = false;
            TypeSymbol type = null;
            BoundExpression boundFilter = null;
            var declaration = node.Declaration;
            if (declaration != null)
            {
                // Note: The type is being bound twice: here and in LocalSymbol.Type. Currently,
                // LocalSymbol.Type ignores diagnostics so it seems cleaner to bind the type here
                // as well. However, if LocalSymbol.Type is changed to report diagnostics, we'll
                // need to avoid binding here since that will result in duplicate diagnostics.
                type = this.BindType(declaration.Type, diagnostics).Type;
                Debug.Assert((object)type != null);

                if (type.IsErrorType())
                {
                    hasError = true;
                }
                else
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    TypeSymbol effectiveType = type.EffectiveType(ref useSiteDiagnostics);
                    if (!Compilation.IsExceptionType(effectiveType, ref useSiteDiagnostics))
                    {
                        // "The type caught or thrown must be derived from System.Exception"
                        Error(diagnostics, ErrorCode.ERR_BadExceptionType, declaration.Type);
                        hasError = true;
                        diagnostics.Add(declaration.Type, useSiteDiagnostics);
                    }
                }
            }

            var filter = node.Filter;
            if (filter != null)
            {
                var filterBinder = this.GetBinder(filter);
                boundFilter = filterBinder.BindCatchFilter(filter, diagnostics);
                hasError |= boundFilter.HasAnyErrors;
            }

            if (!hasError)
            {
                // TODO: Loop is O(n), caller is O(n^2).  Perhaps we could iterate in reverse order (since it's easier to find
                // base types than to find derived types).
                Debug.Assert(((object)type == null) || !type.IsErrorType());
                foreach (var previousBlock in previousBlocks)
                {
                    var previousType = previousBlock.ExceptionTypeOpt;

                    // If the previous type is a generic parameter we don't know what exception types it's gonna catch exactly.
                    // If it is a class-type we know it's gonna catch all exception types of its type and types that are derived from it.
                    // So if the current type is a class-type (or an effective base type of a generic parameter) 
                    // that derives from the previous type the current catch is unreachable.

                    if (previousBlock.ExceptionFilterOpt == null && (object)previousType != null && !previousType.IsErrorType())
                    {
                        if ((object)type != null)
                        {
                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                            if (Conversions.HasIdentityOrImplicitReferenceConversion(type, previousType, ref useSiteDiagnostics))
                            {
                                // "A previous catch clause already catches all exceptions of this or of a super type ('{0}')"
                                Error(diagnostics, ErrorCode.ERR_UnreachableCatch, declaration.Type, previousType);
                                diagnostics.Add(declaration.Type, useSiteDiagnostics);
                                hasError = true;
                                break;
                            }

                            diagnostics.Add(declaration.Type, useSiteDiagnostics);
                        }
                        else if (TypeSymbol.Equals(previousType, Compilation.GetWellKnownType(WellKnownType.System_Exception), TypeCompareKind.ConsiderEverything2) &&
                                 Compilation.SourceAssembly.RuntimeCompatibilityWrapNonExceptionThrows)
                        {
                            // If the RuntimeCompatibility(WrapNonExceptionThrows = false) is applied on the source assembly or any referenced netmodule.
                            // an empty catch may catch exceptions that don't derive from System.Exception.

                            // "A previous catch clause already catches all exceptions..."
                            Error(diagnostics, ErrorCode.WRN_UnreachableGeneralCatch, node.CatchKeyword);
                            break;
                        }
                    }
                }
            }

            var binder = GetBinder(node);
            Debug.Assert(binder != null);

            ImmutableArray<LocalSymbol> locals = binder.GetDeclaredLocalsForScope(node);
            BoundExpression exceptionSource = null;
            LocalSymbol local = locals.FirstOrDefault();

            if (local?.DeclarationKind == LocalDeclarationKind.CatchVariable)
            {
                Debug.Assert(local.Type.IsErrorType() || (TypeSymbol.Equals(local.Type, type, TypeCompareKind.ConsiderEverything2)));

                // Check for local variable conflicts in the *enclosing* binder, not the *current* binder;
                // obviously we will find a local of the given name in the current binder.
                hasError |= this.ValidateDeclarationNameConflictsInScope(local, diagnostics);

                exceptionSource = new BoundLocal(declaration, local, ConstantValue.NotAvailable, local.Type);
            }

            var block = BindEmbeddedBlock(node.Block, diagnostics);
            return new BoundCatchBlock(node, locals, exceptionSource, type, boundFilter, block, hasError);
        }

        private BoundExpression BindCatchFilter(CatchFilterClauseSyntax filter, DiagnosticBag diagnostics)
        {
            BoundExpression boundFilter = this.BindBooleanExpression(filter.FilterExpression, diagnostics);
            if (boundFilter.ConstantValue != ConstantValue.NotAvailable)
            {
                // Depending on whether the filter constant is true or false, and whether there are other catch clauses,
                // we suggest different actions
                var errorCode = boundFilter.ConstantValue.BooleanValue
                    ? ErrorCode.WRN_FilterIsConstantTrue
                    : (filter.Parent.Parent is TryStatementSyntax s && s.Catches.Count == 1 && s.Finally == null)
                        ? ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch
                        : ErrorCode.WRN_FilterIsConstantFalse;

                // Since the expression is a constant, the name can be retrieved from the first token
                Error(diagnostics, errorCode, filter.FilterExpression);
            }

            return boundFilter;
        }

        // Report an extra error on the return if we are in a lambda conversion.
        private void ReportCantConvertLambdaReturn(SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // Suppress this error if the lambda is a result of a query rewrite.
            if (syntax.Parent is QueryClauseSyntax || syntax.Parent is SelectOrGroupClauseSyntax)
                return;

            var lambda = this.ContainingMemberOrLambda as LambdaSymbol;
            if ((object)lambda != null)
            {
                Location location = getLocationForDiagnostics(syntax);
                if (IsInAsyncMethod())
                {
                    // Cannot convert async {0} to intended delegate type. An async {0} may return void, Task or Task<T>, none of which are convertible to '{1}'.
                    Error(diagnostics, ErrorCode.ERR_CantConvAsyncAnonFuncReturns,
                        location,
                        lambda.MessageID.Localize(), lambda.ReturnType);
                }
                else
                {
                    // Cannot convert {0} to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                    Error(diagnostics, ErrorCode.ERR_CantConvAnonMethReturns,
                        location,
                        lambda.MessageID.Localize());
                }
            }

            Location getLocationForDiagnostics(SyntaxNode node)
            {
                switch (node)
                {
                    case LambdaExpressionSyntax lambdaSyntax:
                        return Location.Create(lambdaSyntax.SyntaxTree,
                            Text.TextSpan.FromBounds(lambdaSyntax.SpanStart, lambdaSyntax.ArrowToken.Span.End));

                    case AnonymousMethodExpressionSyntax anonymousMethodSyntax:
                        return Location.Create(anonymousMethodSyntax.SyntaxTree,
                            Text.TextSpan.FromBounds(anonymousMethodSyntax.SpanStart,
                                anonymousMethodSyntax.ParameterList?.Span.End ?? anonymousMethodSyntax.DelegateKeyword.Span.End));
                }

                return node.Location;
            }
        }

        private static bool IsValidStatementExpression(SyntaxNode syntax, BoundExpression expression)
        {
            bool syntacticallyValid = SyntaxFacts.IsStatementExpression(syntax);
            if (!syntacticallyValid)
            {
                return false;
            }

            if (expression.IsSuppressed)
            {
                return false;
            }

            // It is possible that an expression is syntactically valid but semantic analysis
            // reveals it to be illegal in a statement expression: "new MyDelegate(M)" for example
            // is not legal because it is a delegate-creation-expression and not an
            // object-creation-expression, but of course we don't know that syntactically.

            if (expression.Kind == BoundKind.DelegateCreationExpression || expression.Kind == BoundKind.NameOfOperator)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Wrap a given expression e into a block as either { e; } or { return e; } 
        /// Shared between lambda and expression-bodied method binding.
        /// </summary>
        internal BoundBlock CreateBlockFromExpression(CSharpSyntaxNode node, ImmutableArray<LocalSymbol> locals, RefKind refKind, BoundExpression expression, ExpressionSyntax expressionSyntax, DiagnosticBag diagnostics)
        {
            RefKind returnRefKind;
            var returnType = GetCurrentReturnType(out returnRefKind);
            var syntax = expressionSyntax ?? expression.Syntax;

            BoundStatement statement;
            if (IsInAsyncMethod() && refKind != RefKind.None)
            {
                // This can happen if we are binding an async anonymous method to a delegate type.
                Error(diagnostics, ErrorCode.ERR_MustNotHaveRefReturn, syntax);
                statement = new BoundReturnStatement(syntax, refKind, expression) { WasCompilerGenerated = true };
            }
            else if ((object)returnType != null)
            {
                if ((refKind != RefKind.None) != (returnRefKind != RefKind.None) && expression.Kind != BoundKind.ThrowExpression)
                {
                    var errorCode = refKind != RefKind.None
                        ? ErrorCode.ERR_MustNotHaveRefReturn
                        : ErrorCode.ERR_MustHaveRefReturn;
                    Error(diagnostics, errorCode, syntax);
                    expression = BindToTypeForErrorRecovery(expression);
                    statement = new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
                }
                else if (returnType.IsVoidType() || IsTaskReturningAsyncMethod())
                {
                    // If the return type is void then the expression is required to be a legal
                    // statement expression.

                    Debug.Assert(expressionSyntax != null || !IsValidExpressionBody(expressionSyntax, expression));

                    bool errors = false;
                    if (expressionSyntax == null || !IsValidExpressionBody(expressionSyntax, expression))
                    {
                        expression = BindToTypeForErrorRecovery(expression);
                        Error(diagnostics, ErrorCode.ERR_IllegalStatement, syntax);
                        errors = true;
                    }

                    // Don't mark compiler generated so that the rewriter generates sequence points
                    var expressionStatement = new BoundExpressionStatement(syntax, expression, errors);

                    CheckForUnobservedAwaitable(expression, diagnostics);
                    statement = expressionStatement;
                }
                else if (IsIAsyncEnumerableOrIAsyncEnumeratorReturningAsyncMethod())
                {
                    Error(diagnostics, ErrorCode.ERR_ReturnInIterator, syntax);
                    expression = BindToTypeForErrorRecovery(expression);
                    statement = new BoundReturnStatement(syntax, returnRefKind, expression) { WasCompilerGenerated = true };
                }
                else
                {
                    expression = returnType.IsErrorType()
                        ? BindToTypeForErrorRecovery(expression)
                        : CreateReturnConversion(syntax, diagnostics, expression, refKind, returnType);
                    statement = new BoundReturnStatement(syntax, returnRefKind, expression) { WasCompilerGenerated = true };
                }
            }
            else if (expression.Type?.SpecialType == SpecialType.System_Void)
            {
                statement = new BoundExpressionStatement(syntax, expression) { WasCompilerGenerated = true };
            }
            else
            {
                // When binding for purpose of inferring the return type of a lambda, we do not require returned expressions (such as `default` or switch expressions) to have a natural type
                var inferringLambda = this.ContainingMemberOrLambda is MethodSymbol method && (object)method.ReturnType == LambdaSymbol.ReturnTypeIsBeingInferred;
                if (!inferringLambda)
                {
                    expression = BindToNaturalType(expression, diagnostics);
                }
                statement = new BoundReturnStatement(syntax, refKind, expression) { WasCompilerGenerated = true };
            }

            // Need to attach the tree for when we generate sequence points.
            return new BoundBlock(node, locals, ImmutableArray.Create(statement)) { WasCompilerGenerated = node.Kind() != SyntaxKind.ArrowExpressionClause };
        }

        private static bool IsValidExpressionBody(SyntaxNode expressionSyntax, BoundExpression expression)
        {
            return IsValidStatementExpression(expressionSyntax, expression) || expressionSyntax.Kind() == SyntaxKind.ThrowExpression;
        }

        /// <summary>
        /// Binds an expression-bodied member with expression e as either { return e;} or { e; }.
        /// </summary>
        internal virtual BoundBlock BindExpressionBodyAsBlock(ArrowExpressionClauseSyntax expressionBody,
                                                      DiagnosticBag diagnostics)
        {
            Binder bodyBinder = this.GetBinder(expressionBody);
            Debug.Assert(bodyBinder != null);

            RefKind refKind = RefKind.None;
            ExpressionSyntax expressionSyntax = expressionBody.Expression.CheckAndUnwrapRefExpression(diagnostics, out refKind);
            BindValueKind requiredValueKind = GetRequiredReturnValueKind(refKind);
            BoundExpression expression = bodyBinder.BindValue(expressionSyntax, diagnostics, requiredValueKind);
            expression = ValidateEscape(expression, Binder.ExternalScope, refKind != RefKind.None, diagnostics);

            return bodyBinder.CreateBlockFromExpression(expressionBody, bodyBinder.GetDeclaredLocalsForScope(expressionBody), refKind, expression, expressionSyntax, diagnostics);
        }

        /// <summary>
        /// Binds a lambda with expression e as either { return e;} or { e; }.
        /// </summary>
        public BoundBlock BindLambdaExpressionAsBlock(ExpressionSyntax body, DiagnosticBag diagnostics)
        {
            Binder bodyBinder = this.GetBinder(body);
            Debug.Assert(bodyBinder != null);

            RefKind refKind;
            var expressionSyntax = body.CheckAndUnwrapRefExpression(diagnostics, out refKind);
            BindValueKind requiredValueKind = GetRequiredReturnValueKind(refKind);
            BoundExpression expression = bodyBinder.BindValue(expressionSyntax, diagnostics, requiredValueKind);
            expression = ValidateEscape(expression, Binder.ExternalScope, refKind != RefKind.None, diagnostics);

            return bodyBinder.CreateBlockFromExpression(body, bodyBinder.GetDeclaredLocalsForScope(body), refKind, expression, expressionSyntax, diagnostics);
        }

        private BindValueKind GetRequiredReturnValueKind(RefKind refKind)
        {
            BindValueKind requiredValueKind = BindValueKind.RValue;
            if (refKind != RefKind.None)
            {
                GetCurrentReturnType(out var sigRefKind);
                requiredValueKind = sigRefKind == RefKind.Ref ?
                                        BindValueKind.RefReturn :
                                        BindValueKind.ReadonlyRef;
            }

            return requiredValueKind;
        }

        public virtual BoundNode BindMethodBody(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            switch (syntax)
            {
                case BaseMethodDeclarationSyntax method:
                    if (method.Kind() == SyntaxKind.ConstructorDeclaration)
                    {
                        return BindConstructorBody((ConstructorDeclarationSyntax)method, diagnostics);
                    }

                    return BindMethodBody(method, method.Body, method.ExpressionBody, diagnostics);

                case AccessorDeclarationSyntax accessor:
                    return BindMethodBody(accessor, accessor.Body, accessor.ExpressionBody, diagnostics);

                case ArrowExpressionClauseSyntax arrowExpression:
                    return BindExpressionBodyAsBlock(arrowExpression, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
            }
        }

        private BoundNode BindConstructorBody(ConstructorDeclarationSyntax constructor, DiagnosticBag diagnostics)
        {
            if (constructor.Initializer == null && constructor.Body == null && constructor.ExpressionBody == null)
            {
                return null;
            }

            Binder bodyBinder = this.GetBinder(constructor);
            Debug.Assert(bodyBinder != null);

            // Using BindStatement to bind block to make sure we are reusing results of partial binding in SemanticModel
            return new BoundConstructorMethodBody(constructor,
                                                  bodyBinder.GetDeclaredLocalsForScope(constructor),
                                                  constructor.Initializer == null ? null : bodyBinder.BindConstructorInitializer(constructor.Initializer, diagnostics),
                                                  constructor.Body == null ? null : (BoundBlock)bodyBinder.BindStatement(constructor.Body, diagnostics),
                                                  constructor.ExpressionBody == null ?
                                                      null :
                                                      bodyBinder.BindExpressionBodyAsBlock(constructor.ExpressionBody,
                                                                                           constructor.Body == null ? diagnostics : new DiagnosticBag()));
        }

        internal virtual BoundExpressionStatement BindConstructorInitializer(ConstructorInitializerSyntax initializer, DiagnosticBag diagnostics)
        {
            BoundExpression initializerInvocation = GetBinder(initializer).BindConstructorInitializer(initializer.ArgumentList, (MethodSymbol)this.ContainingMember(), diagnostics);
            //  Base WasCompilerGenerated state off of whether constructor is implicitly declared, this will ensure proper instrumentation.
            Debug.Assert(!this.ContainingMember().IsImplicitlyDeclared);
            var constructorInitializer = new BoundExpressionStatement(initializer, initializerInvocation);
            Debug.Assert(initializerInvocation.HasAnyErrors || constructorInitializer.IsConstructorInitializer(), "Please keep this bound node in sync with BoundNodeExtensions.IsConstructorInitializer.");
            return constructorInitializer;
        }

        private BoundNode BindMethodBody(CSharpSyntaxNode declaration, BlockSyntax blockBody, ArrowExpressionClauseSyntax expressionBody, DiagnosticBag diagnostics)
        {
            if (blockBody == null && expressionBody == null)
            {
                return null;
            }

            // Using BindStatement to bind block to make sure we are reusing results of partial binding in SemanticModel
            return new BoundNonConstructorMethodBody(declaration,
                                                     blockBody == null ? null : (BoundBlock)BindStatement(blockBody, diagnostics),
                                                     expressionBody == null ?
                                                         null :
                                                         BindExpressionBodyAsBlock(expressionBody,
                                                                                   blockBody == null ? diagnostics : new DiagnosticBag()));
        }

        internal virtual ImmutableArray<LocalSymbol> Locals
        {
            get
            {
                return ImmutableArray<LocalSymbol>.Empty;
            }
        }

        internal virtual ImmutableArray<LocalFunctionSymbol> LocalFunctions
        {
            get
            {
                return ImmutableArray<LocalFunctionSymbol>.Empty;
            }
        }

        internal virtual ImmutableArray<LabelSymbol> Labels
        {
            get
            {
                return ImmutableArray<LabelSymbol>.Empty;
            }
        }

        /// <summary>
        /// Perform a lookup for the specified method on the specified expression by attempting to invoke it 
        /// </summary>
        /// <param name="receiver">The expression to perform pattern lookup on</param>
        /// <param name="methodName">Method to search for.</param>
        /// <param name="syntaxNode">The expression for which lookup is being performed</param>
        /// <param name="diagnostics">Populated with binding diagnostics.</param>
        /// <param name="result">The method symbol that was looked up, or null</param>
        /// <returns>A <see cref="PatternLookupResult"/> value with the outcome of the lookup</returns>
        internal PatternLookupResult PerformPatternMethodLookup(BoundExpression receiver, string methodName,
                                                                SyntaxNode syntaxNode, DiagnosticBag diagnostics, out MethodSymbol result)
        {
            var bindingDiagnostics = DiagnosticBag.GetInstance();

            try
            {
                result = null;

                var boundAccess = BindInstanceMemberAccess(
                       syntaxNode,
                       syntaxNode,
                       receiver,
                       methodName,
                       rightArity: 0,
                       typeArgumentsSyntax: default,
                       typeArgumentsWithAnnotations: default,
                       invoked: true,
                       indexed: false,
                       bindingDiagnostics);

                if (boundAccess.Kind != BoundKind.MethodGroup)
                {
                    // the thing is not a method
                    return PatternLookupResult.NotAMethod;
                }

                var analyzedArguments = AnalyzedArguments.GetInstance();
                var patternMethodCall = BindMethodGroupInvocation(
                    syntaxNode,
                    syntaxNode,
                    methodName,
                    (BoundMethodGroup)boundAccess,
                    analyzedArguments,
                    bindingDiagnostics,
                    queryClause: null,
                    allowUnexpandedForm: false,
                    anyApplicableCandidates: out _);

                analyzedArguments.Free();

                if (patternMethodCall.Kind != BoundKind.Call)
                {
                    return PatternLookupResult.NotCallable;
                }

                var call = (BoundCall)patternMethodCall;
                if (call.ResultKind == LookupResultKind.Empty)
                {
                    return PatternLookupResult.NoResults;
                }

                // we have succeeded or almost succeeded to bind the method
                // report additional binding diagnostics that we have seen so far
                diagnostics.AddRange(bindingDiagnostics);

                var patternMethodSymbol = call.Method;
                if (patternMethodSymbol is ErrorMethodSymbol ||
                    patternMethodCall.HasAnyErrors)
                {
                    return PatternLookupResult.ResultHasErrors;
                }

                // Success!
                result = patternMethodSymbol;
                return PatternLookupResult.Success;
            }
            finally
            {
                bindingDiagnostics.Free();
            }
        }
    }
}
