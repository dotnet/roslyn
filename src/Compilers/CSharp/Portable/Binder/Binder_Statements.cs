// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            Binder statementBinder = this.GetBinder(node);
            return (statementBinder ?? this).BindStatementCore(node, diagnostics);
        }

        private BoundStatement BindStatementCore(StatementSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            BoundStatement result;
            switch (node.Kind())
            {
                case SyntaxKind.Block:
                    result = BindBlock((BlockSyntax)node, diagnostics);
                    break;
                case SyntaxKind.LocalDeclarationStatement:
                    result = BindDeclarationStatement((LocalDeclarationStatementSyntax)node, diagnostics);
                    break;
                case SyntaxKind.LocalFunctionStatement:
                    // The binder in the map is for the method body, so we use the *enclosing* binder for the block
                    result = Next.BindLocalFunctionStatement((LocalFunctionStatementSyntax)node, diagnostics);
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
                    result = BindForEach((ForEachStatementSyntax)node, diagnostics);
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
                case SyntaxKind.LetStatement:
                    result = BindLetStatement((LetStatementSyntax)node, diagnostics);
                    break;
                default:
                    // NOTE: We could probably throw an exception here, but it's conceivable
                    // that a non-parser syntax tree could reach this point with an unexpected
                    // SyntaxKind and we don't want to throw if that occurs.
                    result = new BoundBadStatement(node, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                    break;
            }

            Debug.Assert(result.WasCompilerGenerated == false, "Synthetic node would not get cached");

            Debug.Assert(result.Syntax is StatementSyntax, "BoundStatement should be associated with a statement syntax.");

            Debug.Assert(System.Linq.Enumerable.Contains(result.Syntax.AncestorsAndSelf(), node), @"Bound statement (or one of its parents) 
                                                                            should have same syntax as the given syntax node. 
                                                                            Otherwise it may be confusing to the binder cache that uses syntax node as keys.");

            // An if statement already has its pattern variables in the resulting bound node.
            // This is necessary because it has its own scoping rules.
            if (node.Kind() != SyntaxKind.IfStatement)
            {
                // Other statements do not, and require an enclosing bound node to store them.
                PatternVariableBinder patternBinder = this as PatternVariableBinder ?? Next as PatternVariableBinder;
                if (patternBinder != null && patternBinder.Syntax == node && !patternBinder.Locals.IsDefaultOrEmpty)
                {
                    result = new BoundBlock(node, patternBinder.Locals, ImmutableArray<LocalFunctionSymbol>.Empty, ImmutableArray.Create(result), result.HasErrors);
                }
            }

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

        private BoundStatement BindYieldReturnStatement(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            TypeSymbol elementType = this.GetIteratorElementType(node, diagnostics);
            BoundExpression argument = (node.Expression == null)
                ? BadExpression(node)
                : BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            if (!argument.HasAnyErrors)
            {
                argument = GenerateConversionForAssignment(elementType, argument, diagnostics);
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
            return BindStatement(node, diagnostics);
        }

        private BoundExpression BindThrownExpression(ExpressionSyntax exprSyntax, DiagnosticBag diagnostics, ref bool hasErrors)
        {
            var boundExpr = BindValue(exprSyntax, diagnostics, BindValueKind.RValue);

            // SPEC VIOLATION: The spec requires the thrown exception to have a type, and that the type
            // be System.Exception or derived from System.Exception. (Or, if a type parameter, to have
            // an effective base class that meets that criterion.) However, we allow the literal null 
            // to be thrown, even though it does not meet that criterion and will at runtime always
            // produce a null reference exception.

            if (!boundExpr.IsLiteralNull())
            {
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

            return boundExpr;
        }

        private BoundThrowStatement BindThrow(ThrowStatementSyntax node, DiagnosticBag diagnostics)
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

        private BoundStatement BindEmpty(EmptyStatementSyntax node)
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
                        return new BoundBadStatement(node, ImmutableArray<BoundNode>.Empty, true);
                    }
                    return binder.BindGotoCaseOrDefault(node, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundStatement BindLocalFunctionStatement(LocalFunctionStatementSyntax node, DiagnosticBag diagnostics)
        {
            // already defined symbol in containing block
            var localSymbol = this.LookupLocalFunction(node.Identifier);

            var hasErrors = false;

            // In error scenarios with misplaced code, it is possible we can't bind the local declaration.
            // This occurs through the semantic model.  In that case concoct a plausible result.
            if (localSymbol == null)
            {
                localSymbol = new LocalFunctionSymbol(this, this.ContainingType, this.ContainingMemberOrLambda, node);
            }
            else
            {
                hasErrors |= this.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);
            }

            var binder = this.GetBinder(node);

            // Binder could be null in error scenarios (as above)
            BoundBlock block;
            if (binder != null)
            {
                if (node.Body != null)
                {
                    block = binder.BindEmbeddedBlock(node.Body, diagnostics);
                }
                else if (node.ExpressionBody != null)
                {
                    block = binder.GetBinder(node.ExpressionBody).BindExpressionBodyAsBlock(node.ExpressionBody, diagnostics);
                }
                else
                {
                    block = null;
                    hasErrors = true;
                    // TODO: add a message for this?
                    diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, localSymbol.Locations[0], localSymbol);
                }

                if (block != null)
                {
                    localSymbol.ComputeReturnType();

                    // Have to do ControlFlowPass here because in MethodCompiler, we don't call this for synthed methods
                    // rather we go directly to LowerBodyOrInitializer, which skips over flow analysis (which is in CompileMethod)
                    // (the same thing - calling ControlFlowPass.Analyze in the lowering - is done for lambdas)
                    // It's a bit of code duplication, but refactoring would make things worse.
                    var endIsReachable = ControlFlowPass.Analyze(localSymbol.DeclaringCompilation, localSymbol, block, diagnostics);
                    if (endIsReachable)
                    {
                        if (ImplicitReturnIsOkay(localSymbol))
                        {
                            block = FlowAnalysisPass.AppendImplicitReturn(block, localSymbol, node.Body);
                        }
                        else
                        {
                            diagnostics.Add(ErrorCode.ERR_ReturnExpected, localSymbol.Locations[0], localSymbol);
                        }
                    }
                }
            }
            else
            {
                block = null;
                hasErrors = true;
            }

            localSymbol.GrabDiagnostics(diagnostics);

            return new BoundLocalFunctionStatement(node, localSymbol, block, hasErrors);
        }

        private bool ImplicitReturnIsOkay(MethodSymbol method)
        {
            return method.ReturnsVoid || method.IsIterator ||
                (method.IsAsync && method.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task) == method.ReturnType);
        }

        public BoundExpressionStatement BindExpressionStatement(ExpressionStatementSyntax node, DiagnosticBag diagnostics)
        {
            return BindExpressionStatement(node, node.Expression, node.AllowsAnyExpression, diagnostics);
        }

        private BoundExpressionStatement BindExpressionStatement(CSharpSyntaxNode node, ExpressionSyntax syntax, bool allowsAnyExpression, DiagnosticBag diagnostics)
        {
            BoundExpressionStatement expressionStatement;

            var expression = BindValue(syntax, diagnostics, BindValueKind.RValue);
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

        private BoundStatement BindDeclarationStatement(LocalDeclarationStatementSyntax node, DiagnosticBag diagnostics)
        {
            var typeSyntax = node.Declaration.Type;
            bool isConst = node.IsConst;

            bool isVar;
            AliasSymbol alias;
            TypeSymbol declType = BindVariableType(node, diagnostics, typeSyntax, ref isConst, isVar: out isVar, alias: out alias);

            // UNDONE: "possible expression" feature for IDE

            LocalDeclarationKind kind = LocalDeclarationKind.RegularVariable;
            if (isConst)
            {
                kind = LocalDeclarationKind.Constant;
            }

            var variableList = node.Declaration.Variables;
            int variableCount = variableList.Count;

            if (variableCount == 1)
            {
                return BindVariableDeclaration(kind, isVar, variableList[0], typeSyntax, declType, alias, diagnostics, node);
            }
            else
            {
                BoundLocalDeclaration[] boundDeclarations = new BoundLocalDeclaration[variableCount];

                int i = 0;
                foreach (var variableDeclaratorSyntax in variableList)
                {
                    boundDeclarations[i++] = BindVariableDeclaration(kind, isVar, variableDeclaratorSyntax, typeSyntax, declType, alias, diagnostics);
                }

                return new BoundMultipleLocalDeclarations(node, boundDeclarations.AsImmutableOrNull());
            }
        }

        private TypeSymbol BindVariableType(CSharpSyntaxNode declarationNode, DiagnosticBag diagnostics, TypeSyntax typeSyntax, ref bool isConst, out bool isVar, out AliasSymbol alias)
        {
            Debug.Assert(declarationNode.Kind() == SyntaxKind.LocalDeclarationStatement);

            // If the type is "var" then suppress errors when binding it. "var" might be a legal type
            // or it might not; if it is not then we do not want to report an error. If it is, then
            // we want to treat the declaration as an explicitly typed declaration.

            TypeSymbol declType = BindType(typeSyntax, diagnostics, out isVar, out alias);
            Debug.Assert((object)declType != null || isVar);

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

                if (declarationNode.Kind() == SyntaxKind.LocalDeclarationStatement && ((LocalDeclarationStatementSyntax)declarationNode).Declaration.Variables.Count > 1 && !declarationNode.HasErrors)
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
                    Error(diagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, declType);
                }

                if (isConst && !declType.CanBeConst())
                {
                    Error(diagnostics, ErrorCode.ERR_BadConstType, typeSyntax, declType);
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
            IsInitializerRefKindValid(initializer, initializer, refKind, diagnostics, out valueKind); // The return value isn't important here; we just want the diagnostics and the BindValueKind
            return BindInferredVariableInitializer(diagnostics, initializer, valueKind, errorSyntax);
        }

        // The location where the error is reported might not be the initializer.
        protected BoundExpression BindInferredVariableInitializer(DiagnosticBag diagnostics, EqualsValueClauseSyntax initializer, BindValueKind valueKind,
            CSharpSyntaxNode errorSyntax)
        {
            if (initializer == null)
            {
                if (!errorSyntax.HasErrors)
                {
                    Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, errorSyntax);
                }

                return null;
            }

            if (initializer.Value.Kind() == SyntaxKind.ArrayInitializerExpression)
            {
                return BindUnexpectedArrayInitializer((InitializerExpressionSyntax)initializer.Value,
                    diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, errorSyntax);
            }

            BoundExpression expression = BindValue(initializer.Value, diagnostics, valueKind);

            // Certain expressions (null literals, method groups and anonymous functions) have no type of 
            // their own and therefore cannot be the initializer of an implicitly typed local.
            if (!expression.HasAnyErrors && !expression.HasExpressionType())
            {
                MessageID id = MessageID.IDS_NULL;
                if (expression.Kind == BoundKind.UnboundLambda)
                {
                    id = ((UnboundLambda)expression).MessageID;
                }
                else if (expression.Kind == BoundKind.MethodGroup)
                {
                    id = MessageID.IDS_MethodGroup;
                }
                else
                {
                    Debug.Assert(expression.IsLiteralNull(), "How did we successfully bind an expression without a type?");
                }

                // Cannot assign {0} to an implicitly-typed local variable
                Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, errorSyntax, id.Localize());
            }

            return expression;
        }

        protected bool IsInitializerRefKindValid(
            EqualsValueClauseSyntax initializer, 
            CSharpSyntaxNode node, 
            RefKind variableRefKind, 
            DiagnosticBag diagnostics, 
            out BindValueKind valueKind)
        {
            if (variableRefKind == RefKind.None)
            {
                valueKind = BindValueKind.RValue;
                if (initializer != null && initializer.RefKeyword.Kind() != SyntaxKind.None)
                {
                    Error(diagnostics, ErrorCode.ERR_InitializeByValueVariableWithReference, node);
                    return false;
                }
            }
            else
            {
                valueKind = BindValueKind.RefOrOut;

                if (initializer == null)
                {
                    Error(diagnostics, ErrorCode.ERR_ByReferenceVariableMustBeInitialized, node);
                    return false;
                }
                else if (initializer.RefKeyword.Kind() == SyntaxKind.None)
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
            TypeSymbol declTypeOpt,
            AliasSymbol aliasOpt,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode associatedSyntaxNode = null)
        {
            Debug.Assert(declarator != null);

            return BindVariableDeclaration(LocateDeclaredVariableSymbol(declarator, typeSyntax),
                                           kind,
                                           isVar,
                                           declarator,
                                           typeSyntax,
                                           declTypeOpt,
                                           aliasOpt,
                                           diagnostics,
                                           associatedSyntaxNode);
        }

        protected BoundLocalDeclaration BindVariableDeclaration(
            SourceLocalSymbol localSymbol,
            LocalDeclarationKind kind,
            bool isVar,
            VariableDeclaratorSyntax declarator,
            TypeSyntax typeSyntax,
            TypeSymbol declTypeOpt,
            AliasSymbol aliasOpt,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode associatedSyntaxNode = null)
        {
            Debug.Assert(declarator != null);
            Debug.Assert((object)declTypeOpt != null || isVar);
            Debug.Assert(typeSyntax != null);

            var localDiagnostics = DiagnosticBag.GetInstance();
            // if we are not given desired syntax, we use declarator
            associatedSyntaxNode = associatedSyntaxNode ?? declarator;

            // Check for variable declaration errors.
            Binder nameConflictChecker = this;

            // Step out of the PatternVariableBinder for locals declared in variable declaration statement
            if (this is PatternVariableBinder)
            {
                CSharpSyntaxNode parent;
                if ((parent = declarator.Parent)?.Kind() == SyntaxKind.VariableDeclaration &&
                     parent.Parent?.Kind() == SyntaxKind.LocalDeclarationStatement)
                {
                    nameConflictChecker = this.Next;
                }
            }

            bool hasErrors = nameConflictChecker.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

            var containingMethod = this.ContainingMemberOrLambda as MethodSymbol;
            if (containingMethod != null && containingMethod.IsAsync && localSymbol.RefKind != RefKind.None)
            {
                Error(diagnostics, ErrorCode.ERR_BadAsyncLocalType, declarator);
            }

            EqualsValueClauseSyntax equalsClauseSyntax = declarator.Initializer;

            BindValueKind valueKind;
            if (!IsInitializerRefKindValid(equalsClauseSyntax, declarator, localSymbol.RefKind, diagnostics, out valueKind))
            {
                hasErrors = true;
            }

            BoundExpression initializerOpt;
            if (isVar)
            {
                aliasOpt = null;

                var binder = new ImplicitlyTypedLocalBinder(this, localSymbol);
                initializerOpt = binder.BindInferredVariableInitializer(diagnostics, equalsClauseSyntax, valueKind, declarator);

                // If we got a good result then swap the inferred type for the "var" 
                if ((object)initializerOpt?.Type != null)
                {
                    declTypeOpt = initializerOpt.Type;

                    if (declTypeOpt.SpecialType == SpecialType.System_Void)
                    {
                        Error(localDiagnostics, ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, declarator, declTypeOpt);
                        declTypeOpt = CreateErrorType("var");
                        hasErrors = true;
                    }

                    if (!declTypeOpt.IsErrorType())
                    {
                        if (declTypeOpt.IsStatic)
                        {
                            Error(localDiagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, initializerOpt.Type);
                            hasErrors = true;
                        }
                    }
                }
                else
                {
                    declTypeOpt = CreateErrorType("var");
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
                    initializerOpt = BindPossibleArrayInitializer(equalsClauseSyntax.Value, declTypeOpt, valueKind, diagnostics);
                    if (kind != LocalDeclarationKind.FixedVariable)
                    {
                        // If this is for a fixed statement, we'll do our own conversion since there are some special cases.
                        initializerOpt = GenerateConversionForAssignment(declTypeOpt, initializerOpt, diagnostics, refKind: localSymbol.RefKind);
                        initializerOpt = GenerateConversionForAssignment(declTypeOpt, initializerOpt, localDiagnostics);
                    }
                }
            }

            Debug.Assert((object)declTypeOpt != null);

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

                if (!declTypeOpt.IsPointerType())
                {
                    if (!hasErrors)
                    {
                        Error(localDiagnostics, ErrorCode.ERR_BadFixedInitType, declarator);
                        hasErrors = true;
                    }
                }
                else if (!IsValidFixedVariableInitializer(declTypeOpt, localSymbol, ref initializerOpt, localDiagnostics))
                {
                    hasErrors = true;
                }
            }

            if (this.ContainingMemberOrLambda.Kind == SymbolKind.Method
                && ((MethodSymbol)this.ContainingMemberOrLambda).IsAsync
                && declTypeOpt.IsRestrictedType())
            {
                Error(localDiagnostics, ErrorCode.ERR_BadSpecialByRefLocal, typeSyntax, declTypeOpt);
                hasErrors = true;
            }

            Debug.Assert((object)localSymbol != null);

            DeclareLocalVariable(
                localSymbol,
                declarator.Identifier,
                declTypeOpt);

            if (localSymbol.RefKind != RefKind.None && initializerOpt != null)
            {
                var ignoredDiagnostics = DiagnosticBag.GetInstance();
                if (this.CheckValueKind(initializerOpt, BindValueKind.RefReturn, ignoredDiagnostics))
                {
                    localSymbol.SetReturnable();
                }
                ignoredDiagnostics.Free();
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
                    hasErrors = true;
                }
            }

            diagnostics.AddRangeAndFree(localDiagnostics);
            var boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declTypeOpt);
            return new BoundLocalDeclaration(associatedSyntaxNode, localSymbol, boundDeclType, initializerOpt, arguments, hasErrors);
        }

        private ImmutableArray<BoundExpression> BindDeclaratorArguments(VariableDeclaratorSyntax declarator, DiagnosticBag diagnostics)
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
                var builder = ArrayBuilder<BoundExpression>.GetInstance();
                foreach (var argument in declarator.ArgumentList.Arguments)
                {
                    var boundArgument = BindValue(argument.Expression, diagnostics, BindValueKind.RValue);
                    builder.Add(boundArgument);
                }
                arguments = builder.ToImmutableAndFree();
            }

            return arguments;
        }

        private SourceLocalSymbol LocateDeclaredVariableSymbol(VariableDeclaratorSyntax declarator, TypeSyntax typeSyntax)
        {
            SourceLocalSymbol localSymbol = this.LookupLocal(declarator.Identifier);

            // In error scenarios with misplaced code, it is possible we can't bind the local declaration.
            // This occurs through the semantic model.  In that case concoct a plausible result.
            if ((object)localSymbol == null)
            {
                localSymbol = SourceLocalSymbol.MakeLocal(
                    ContainingMemberOrLambda,
                    this,
                    RefKind.None,
                    typeSyntax,
                    declarator.Identifier,
                    LocalDeclarationKind.RegularVariable,
                    declarator.Initializer);
            }

            return localSymbol;
        }

        private bool IsValidFixedVariableInitializer(TypeSymbol declType, SourceLocalSymbol localSymbol, ref BoundExpression initializerOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(!ReferenceEquals(declType, null));
            Debug.Assert(declType.IsPointerType());

            if (ReferenceEquals(initializerOpt, null))
            {
                return false;
            }

            TypeSymbol initializerType = initializerOpt.Type;
            CSharpSyntaxNode initializerSyntax = initializerOpt.Syntax;

            if (ReferenceEquals(initializerType, null))
            {
                // Dev10 just reports the assignment conversion error (which must occur, unless the initializer is a null literal).
                initializerOpt = GenerateConversionForAssignment(declType, initializerOpt, diagnostics);
                if (!initializerOpt.HasAnyErrors)
                {
                    Debug.Assert(initializerOpt.Kind == BoundKind.Conversion && ((BoundConversion)initializerOpt).Operand.IsLiteralNull(),
                        "All other typeless expressions should have conversion errors");
                    // CONSIDER: this is a very confusing error message, but it's what Dev10 reports.
                    Error(diagnostics, ErrorCode.ERR_FixedNotNeeded, initializerSyntax);
                }
            }
            else if (initializerType.SpecialType == SpecialType.System_String)
            {
                // See ExpressionBinder::bindPtrToString

                TypeSymbol elementType = this.GetSpecialType(SpecialType.System_Char, diagnostics, initializerSyntax);
                Debug.Assert(!elementType.IsManagedType);

                initializerOpt = GetFixedLocalCollectionInitializer(initializerOpt, elementType, declType, false, diagnostics);

                // The string case is special - we'll pin a synthesized string temp, rather than the pointer local.
                localSymbol.SetSpecificallyNotPinned();

                // UNDONE: ExpressionBinder::CheckFieldUse (something about MarshalByRef)
            }
            else if (initializerType.IsArray())
            {
                // See ExpressionBinder::BindPtrToArray (though most of that functionality is now in LocalRewriter).

                var arrayType = (ArrayTypeSymbol)initializerType;
                TypeSymbol elementType = arrayType.ElementType;

                bool hasErrors = false;
                if (elementType.IsManagedType)
                {
                    Error(diagnostics, ErrorCode.ERR_ManagedAddr, initializerSyntax, elementType);
                    hasErrors = true;
                }

                initializerOpt = GetFixedLocalCollectionInitializer(initializerOpt, elementType, declType, hasErrors, diagnostics);
            }
            else
            {
                if (!initializerOpt.HasAnyErrors)
                {
                    switch (initializerOpt.Kind)
                    {
                        case BoundKind.AddressOfOperator:
                            // OK
                            break;
                        case BoundKind.Conversion:
                            // The following assertion would not be correct because there might be an implicit conversion after (above) the explicit one.
                            //Debug.Assert(((BoundConversion)initializerOpt).ExplicitCastInCode, "The assignment conversion hasn't been applied yet, so this must be from source.");

                            // NOTE: Dev10 specifically doesn't report this error for the array or string cases.
                            Error(diagnostics, ErrorCode.ERR_BadCastInFixed, initializerSyntax);
                            break;
                        case BoundKind.FieldAccess:
                            var fa = (BoundFieldAccess)initializerOpt;
                            if (!fa.FieldSymbol.IsFixed)
                            {
                                Error(diagnostics, ErrorCode.ERR_FixedNotNeeded, initializerSyntax);
                            }
                            break;
                        default:
                            // CONSIDER: this is a very confusing error message, but it's what Dev10 reports.
                            Error(diagnostics, ErrorCode.ERR_FixedNotNeeded, initializerSyntax);
                            break;
                    }
                }

                initializerOpt = GenerateConversionForAssignment(declType, initializerOpt, diagnostics);
            }

            return true;
        }

        /// <summary>
        /// Wrap the initializer in a BoundFixedLocalCollectionInitializer so that the rewriter will have the
        /// information it needs (e.g. conversions, helper methods).
        /// </summary>
        private BoundExpression GetFixedLocalCollectionInitializer(BoundExpression initializer, TypeSymbol elementType, TypeSymbol declType, bool hasErrors, DiagnosticBag diagnostics)
        {
            Debug.Assert(initializer != null);

            CSharpSyntaxNode initializerSyntax = initializer.Syntax;

            TypeSymbol pointerType = new PointerTypeSymbol(elementType);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion elementConversion = this.Conversions.ClassifyConversion(pointerType, declType, ref useSiteDiagnostics);
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
                initializer.Type,
                hasErrors);
        }

        static private ErrorCode GetStandardLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignment:
                    return ErrorCode.ERR_AssgLvalueExpected;

                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_RefLvalueExpected;

                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;

                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_IncrementLvalueExpected;

                case BindValueKind.FixedReceiver:
                    return ErrorCode.ERR_FixedNeedsLvalue;

                case BindValueKind.RefReturn:
                    return ErrorCode.ERR_RefReturnLvalueExpected;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        static private ErrorCode GetThisLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignment:
                    return ErrorCode.ERR_AssgReadonlyLocal;
                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_RefReadonlyLocal;
                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_AddrOnReadOnlyLocal;
                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_IncrementLvalueExpected;
                case BindValueKind.RefReturn:
                    return ErrorCode.ERR_RefReturnStructThis;
            }
        }

        private static ErrorCode GetRangeLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.Assignment:
                case BindValueKind.CompoundAssignment:
                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_QueryRangeVariableReadOnly;
                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_QueryOutRefRangeVariable;
                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;
                case BindValueKind.RefReturn:
                    return ErrorCode.ERR_RefReturnRangeVariable;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        // Check to see if a local symbol is to be treated as a variable. Returns true if yes, reports an
        // error and returns false if no.
        private static bool CheckLocalVariable(CSharpSyntaxNode tree, LocalSymbol local, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)local != null);
            Debug.Assert(kind != BindValueKind.RValue);

            if (local.IsWritable)
            {
                return true;
            }

            MessageID cause;
            if (local.IsForEach)
            {
                cause = MessageID.IDS_FOREACHLOCAL;
            }
            else if (local.IsUsing)
            {
                cause = MessageID.IDS_USINGLOCAL;
            }
            else if (local.IsFixed)
            {
                cause = MessageID.IDS_FIXEDLOCAL;
            }
            else
            {
                Error(diagnostics, GetStandardLvalueError(kind), tree);
                return false;
            }

            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_AddrOnReadOnlyLocal, tree);
                return false;
            }

            ErrorCode[] ReadOnlyLocalErrors =
            {
                ErrorCode.ERR_RefReadonlyLocalCause,
                // impossible since readonly locals are never byref, but would be a reasonable error otherwise
                ErrorCode.ERR_RefReadonlyLocalCause, 
                ErrorCode.ERR_AssgReadonlyLocalCause,

                ErrorCode.ERR_RefReadonlyLocal2Cause,
                // impossible since readonly locals are never byref, but would be a reasonable error otherwise
                ErrorCode.ERR_RefReadonlyLocal2Cause,
                ErrorCode.ERR_AssgReadonlyLocal2Cause
            };

            int index = (checkingReceiver ? 3 : 0) + (kind == BindValueKind.RefOrOut ? 0 : (kind == BindValueKind.RefReturn ? 1 : 2));

            Error(diagnostics, ReadOnlyLocalErrors[index], tree, local, cause.Localize());

            return false;
        }

        private bool CheckIsCallVariable(BoundCall call, CSharpSyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
            {
            // A call can only be a variable if it returns by reference. If this is the case,
            // whether or not it is a valid variable depends on whether or not the call is the
            // RHS of a return or an assign by reference:
            // - If call is used in a context demanding ref-returnable reference all of its ref
            //   inputs must be ref-returnable

            var methodSymbol = call.Method;
            if (methodSymbol.RefKind != RefKind.None)
            {
                if (kind == BindValueKind.RefReturn)
                {
                    var args = call.Arguments;
                    var argRefKinds = call.ArgumentRefKindsOpt;
                    if (!argRefKinds.IsDefault)
                    {
                        for (var i = 0; i < args.Length; i++)
                        {
                            if (argRefKinds[i] != RefKind.None && !CheckIsVariable(args[i].Syntax, args[i], kind, false, diagnostics))
                            {
                                var errorCode = checkingReceiver ? ErrorCode.ERR_RefReturnCall2 : ErrorCode.ERR_RefReturnCall;
                                var parameterIndex = call.ArgsToParamsOpt.IsDefault ? i : call.ArgsToParamsOpt[i];
                                var parameterName = methodSymbol.Parameters[parameterIndex].Name;
                                Error(diagnostics, errorCode, call.Syntax, methodSymbol, parameterName);
                                return false;
            }
                        }
                    }
                }

                return true;
            }

            if (checkingReceiver)
            {
                // Error is associated with expression, not node which may be distinct.
                Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, call.Syntax, methodSymbol);
            }
            else
            {
                Error(diagnostics, GetStandardLvalueError(kind), node);
            }

            return false;
        }

        private static void ReportReadOnlyError(FieldSymbol field, CSharpSyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)field != null);
            Debug.Assert(kind != BindValueKind.RValue);
            Debug.Assert((object)field.Type != null);

            // It's clearer to say that the address can't be taken than to say that the field can't be modified
            // (even though the latter message gives more explanation of why).
            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_InvalidAddrOp, node);
                return;
            }

            ErrorCode[] ReadOnlyErrors =
            {
                ErrorCode.ERR_RefReadonly,
                ErrorCode.ERR_RefReturnReadonly,
                ErrorCode.ERR_AssgReadonly,
                ErrorCode.ERR_RefReadonlyStatic,
                ErrorCode.ERR_RefReturnReadonlyStatic,
                ErrorCode.ERR_AssgReadonlyStatic,
                ErrorCode.ERR_RefReadonly2,
                ErrorCode.ERR_RefReturnReadonly2,
                ErrorCode.ERR_AssgReadonly2,
                ErrorCode.ERR_RefReadonlyStatic2,
                ErrorCode.ERR_RefReturnReadonlyStatic2,
                ErrorCode.ERR_AssgReadonlyStatic2
            };
            int index = (checkingReceiver ? 6 : 0) + (field.IsStatic ? 3 : 0) + (kind == BindValueKind.RefOrOut ? 0 : (kind == BindValueKind.RefReturn ? 1 : 2));
            if (checkingReceiver)
            {
                Error(diagnostics, ReadOnlyErrors[index], node, field);
            }
            else
            {
                Error(diagnostics, ReadOnlyErrors[index], node);
            }
        }

        /// <summary>
        /// The purpose of this method is to determine if the expression is classified by the 
        /// specification as a *variable*. If it is not then this code gives an appropriate error message.
        ///
        /// To determine the appropriate error message we need to know two things:
        ///
        /// (1) why do we want to know if this is a variable? Because we are trying to assign it,
        ///     increment it, or pass it by reference?
        ///
        /// (2) Are we trying to determine if the left hand side of a dot is a variable in order
        ///     to determine if the field or property on the right hand side of a dot is assignable?
        /// </summary>
        private bool CheckIsVariable(CSharpSyntaxNode node, BoundExpression expr, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert(expr != null);
            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            // Every expression is classified as one of:
            // 1. a namespace
            // 2. a type
            // 3. an anonymous function
            // 4. a literal
            // 5. an event access
            // 6. a call to a void-returning method
            // 7. a method group
            // 8. a property access
            // 9. an indexer access
            // 10. a variable
            // 11. a value

            // We wish to give an error and return false for all of those except case 10.

            // case 0: We've already reported an error:

            if (expr.HasAnyErrors)
            {
                return false;
            }

            // Case 1: a namespace:
            var ns = expr as BoundNamespaceExpression;
            if (ns != null)
            {
                Error(diagnostics, ErrorCode.ERR_BadSKknown, node, ns.NamespaceSymbol, MessageID.IDS_SK_NAMESPACE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                return false;
            }

            // Case 2: a type:
            var type = expr as BoundTypeExpression;
            if (type != null)
            {
                Error(diagnostics, ErrorCode.ERR_BadSKknown, node, type.Type, MessageID.IDS_SK_TYPE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                return false;
            }

            // Cases 3, 4, 6:
            if ((expr.Kind == BoundKind.Lambda) ||
                (expr.Kind == BoundKind.UnboundLambda) ||
                (expr.ConstantValue != null) ||
                (expr.Type.GetSpecialTypeSafe() == SpecialType.System_Void))
            {
                Error(diagnostics, GetStandardLvalueError(kind), node);
                return false;
            }

            // Case 5: field-like events are variables

            var eventAccess = expr as BoundEventAccess;
            if (eventAccess != null)
            {
                EventSymbol eventSymbol = eventAccess.EventSymbol;
                if (!eventAccess.IsUsableAsField)
                {
                    Error(diagnostics, GetBadEventUsageDiagnosticInfo(eventSymbol), node);
                    return false;
                }
                else if (eventSymbol.IsWindowsRuntimeEvent)
                {
                    switch (kind)
                    {
                        case BindValueKind.RValue:
                        case BindValueKind.RValueOrMethodGroup:
                            Debug.Assert(false, "Why call CheckIsVariable if you want an RValue?");
                            goto case BindValueKind.Assignment;
                        case BindValueKind.Assignment:
                        case BindValueKind.CompoundAssignment:
                            return true;
                    }

                    // NOTE: Dev11 reports ERR_RefProperty, as if this were a property access (since that's how it will be lowered).
                    // Roslyn reports a new, more specific, error code.
                    Error(diagnostics, kind == BindValueKind.RefOrOut ? ErrorCode.ERR_WinRtEventPassedByRef : GetStandardLvalueError(kind), node, eventSymbol);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            // Case 7: method group gets a nicer error message depending on whether this is M(out F) or F = x.

            var methodGroup = expr as BoundMethodGroup;
            if (methodGroup != null)
            {
                ErrorCode errorCode;
                switch (kind)
                {
                    case BindValueKind.RefOrOut:
                    case BindValueKind.RefReturn:
                        errorCode = ErrorCode.ERR_RefReadonlyLocalCause;
                        break;
                    case BindValueKind.AddressOf:
                        errorCode = ErrorCode.ERR_InvalidAddrOp;
                        break;
                    default:
                        errorCode = ErrorCode.ERR_AssgReadonlyLocalCause;
                        break;
                }
                Error(diagnostics, errorCode, node, methodGroup.Name, MessageID.IDS_MethodGroup.Localize());
                return false;
            }

            // Cases 8 and 9: Properties and indexer accesses are variables iff they return by reference
            //                or the receiver is also a variable. Otherwise, they get special error messages.

            BoundExpression receiver;
            CSharpSyntaxNode propertySyntax;
            var propertySymbol = GetPropertySymbol(expr, out receiver, out propertySyntax);
            if ((object)propertySymbol != null)
            {
                if (propertySymbol.RefKind != RefKind.None)
                {
                    return true;
                }
                else if (checkingReceiver)
                {
                    // Error is associated with expression, not node which may be distinct.
                    // This error is reported for all values types. That is a breaking
                    // change from Dev10 which reports this error for struct types only,
                    // not for type parameters constrained to "struct".

                    Debug.Assert((object)propertySymbol.Type != null);
                    Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, expr.Syntax, propertySymbol);
                }
                else
                {
                    Error(diagnostics, kind == BindValueKind.RefOrOut ? ErrorCode.ERR_RefProperty : GetStandardLvalueError(kind), node, propertySymbol);
                }

                return false;
            }

            // That then leaves variables and values. There are several things that look like variables that nevertheless are
            // to be treated as values.

            // The undocumented __refvalue(tr, T) expression results in a variable of type T.
            var refvalue = expr as BoundRefValueOperator;
            if (refvalue != null && kind != BindValueKind.RefReturn)
            {
                return true;
            }

            // All parameters are variables unless they are the RHS of a ref return,
            // in which case only ref and out parameters are variables.
            var parameter = expr as BoundParameter;
            if (parameter != null)
            {
                ParameterSymbol parameterSymbol = parameter.ParameterSymbol;
                if (kind == BindValueKind.RefReturn && parameterSymbol.RefKind == RefKind.None)
                {
                    if (checkingReceiver)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnParameter2, expr.Syntax, parameterSymbol.Name);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnParameter, node, parameterSymbol.Name);
                    }
                    return false;
                }
                if (this.LockedOrDisposedVariables.Contains(parameterSymbol))
                {
                    // Consider: It would be more conventional to pass "symbol" rather than "symbol.Name".
                    // The issue is that the error SymbolDisplayFormat doesn't display parameter
                    // names - only their types - which works great in signatures, but not at all
                    // at the top level.
                    diagnostics.Add(ErrorCode.WRN_AssignmentToLockOrDispose, parameter.Syntax.Location, parameterSymbol.Name);
                }
                return true;
            }


            if (expr is BoundArrayAccess  // Array accesses are always variables
                || expr is BoundPointerIndirectionOperator // Pointer dereferences are always variables
                || expr is BoundPointerElementAccess) // Pointer element access is just sugar for pointer dereference
            {
                return true;
            }

            // Local constants are never variables. Local variables are sometimes
            // not to be treated as variables, if they are fixed, declared in a using, 
            // or declared in a foreach.

            // UNDONE: give good errors for range variables and transparent identifiers

            var local = expr as BoundLocal;
            if (local != null)
            {
                LocalSymbol localSymbol = local.LocalSymbol;
                if (kind == BindValueKind.RefReturn)
                {
                    if (localSymbol.RefKind == RefKind.None)
                    {
                        if (checkingReceiver)
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnLocal2, expr.Syntax, localSymbol);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnLocal, node, localSymbol);
                        }

                        return false;
                    }

                    if (!localSymbol.IsReturnable)
                    {
                        if (checkingReceiver)
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnNonreturnableLocal2, expr.Syntax, localSymbol);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnNonreturnableLocal, node, localSymbol);
                        }
                        return false;
                    }
                }

                if (this.LockedOrDisposedVariables.Contains(localSymbol))
                {
                    diagnostics.Add(ErrorCode.WRN_AssignmentToLockOrDispose, local.Syntax.Location, localSymbol);
                }

                return CheckLocalVariable(node, localSymbol, kind, checkingReceiver, diagnostics);
            }

            // SPEC: when this is used in a primary-expression within an instance constructor of a struct, 
            // SPEC: it is classified as a variable. 

            // SPEC: When this is used in a primary-expression within an instance method or instance accessor
            // SPEC: of a struct, it is classified as a variable. 

            var thisref = expr as BoundThisReference;
            if (thisref != null)
            {
                // We will already have given an error for "this" used outside of a constructor, 
                // instance method, or instance accessor. Assume that "this" is a variable if it is in a struct.
                if (!thisref.Type.IsValueType || kind == BindValueKind.RefReturn)
                {
                    // CONSIDER: the Dev10 name has angle brackets (i.e. "<this>")
                    Error(diagnostics, GetThisLvalueError(kind), node, ThisParameterSymbol.SymbolName);
                    return false;
                }
                return true;
            }

            var queryref = expr as BoundRangeVariable;
            if (queryref != null)
            {
                Error(diagnostics, GetRangeLvalueError(kind), node, queryref.RangeVariableSymbol.Name);
                return false;
            }

            // A field is a variable unless 
            // (1) it is readonly and we are not in a constructor or field initializer
            // (2) the receiver of the field is of value type and is not a variable or object creation expression.
            // For example, if you have a class C with readonly field f of type S, and
            // S has a mutable field x, then c.f.x is not a variable because c.f is not
            // writable.

            var fieldAccess = expr as BoundFieldAccess;
            if (fieldAccess != null)
            {
                // NOTE: only the expression part of a field initializer is bound, not the assignment.
                // As a result, it is okay to see that fields are not variables unless they are in
                // constructors.

                var fieldSymbol = fieldAccess.FieldSymbol;
                var fieldIsStatic = fieldSymbol.IsStatic;
                if (fieldSymbol.IsReadOnly)
                {
                    var canModifyReadonly = false;

                    Symbol containing = this.ContainingMemberOrLambda;
                    if ((object)containing != null &&
                        fieldIsStatic == containing.IsStatic &&
                        (fieldIsStatic || fieldAccess.ReceiverOpt.Kind == BoundKind.ThisReference) &&
                        (Compilation.FeatureStrictEnabled
                            ? fieldSymbol.ContainingType == containing.ContainingType
                            // We duplicate a bug in the native compiler for compatibility in non-strict mode
                            : fieldSymbol.ContainingType.OriginalDefinition == containing.ContainingType.OriginalDefinition))
                    {
                        if (containing.Kind == SymbolKind.Method)
                        {
                            MethodSymbol containingMethod = (MethodSymbol)containing;
                            MethodKind desiredMethodKind = fieldIsStatic ? MethodKind.StaticConstructor : MethodKind.Constructor;
                            canModifyReadonly = containingMethod.MethodKind == desiredMethodKind;
                        }
                        else if (containing.Kind == SymbolKind.Field)
                        {
                            canModifyReadonly = true;
                        }
                    }

                    if (!canModifyReadonly)
                    {
                        ReportReadOnlyError(fieldSymbol, node, kind, checkingReceiver, diagnostics);
                    }
                }

                if (fieldSymbol.IsFixed)
                {
                    Error(diagnostics, GetStandardLvalueError(kind), node);
                    return false;
                }

                if (fieldSymbol.ContainingType.IsValueType && 
                    !fieldIsStatic &&
                    !CheckIsValidReceiverForVariable(node, fieldAccess.ReceiverOpt, kind, diagnostics))
                {
                    return false;
                }

                return true;
            }

            var call = expr as BoundCall;
            if (call != null)
            {
                return CheckIsCallVariable(call, node, kind, checkingReceiver, diagnostics);
            }

            var assign = expr as BoundAssignmentOperator;
            if (assign != null && assign.RefKind != RefKind.None)
            {
                return true;
            }

            // At this point we should have covered all the possible cases for variables.

            if ((expr as BoundConversion)?.ConversionKind == ConversionKind.Unboxing)
            {
                Error(diagnostics, ErrorCode.ERR_UnboxNotLValue, node);
                return false;
            }

            Error(diagnostics, GetStandardLvalueError(kind), node);
            return false;
        }

        private bool CheckIsValidReceiverForVariable(CSharpSyntaxNode node, BoundExpression receiver, BindValueKind kind, DiagnosticBag diagnostics)
        {
            Debug.Assert(receiver != null);
            return Flags.Includes(BinderFlags.ObjectInitializerMember) && receiver.Kind == BoundKind.ImplicitReceiver ||
                CheckIsVariable(node, receiver, kind, true, diagnostics);
        }

        private static bool CheckNotNamespaceOrType(BoundExpression expr, DiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.NamespaceExpression:
                    Error(diagnostics, ErrorCode.ERR_BadSKknown, expr.Syntax, ((BoundNamespaceExpression)expr).NamespaceSymbol, MessageID.IDS_SK_NAMESPACE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                    return false;
                case BoundKind.TypeExpression:
                    Error(diagnostics, ErrorCode.ERR_BadSKunknown, expr.Syntax, expr.Type, MessageID.IDS_SK_TYPE.Localize());
                    return false;
                default:
                    return true;
            }
        }

        private BoundAssignmentOperator BindAssignment(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.Left != null);
            Debug.Assert(node.Right != null);

            var op1 = BindValue(node.Left, diagnostics, BindValueKind.Assignment); // , BIND_MEMBERSET);
            var op2 = BindValue(node.Right, diagnostics, BindValueKind.RValue); // , BIND_RVALUEREQUIRED);

            return BindAssignment(node, op1, op2, diagnostics);
        }

        private BoundAssignmentOperator BindAssignment(AssignmentExpressionSyntax node, BoundExpression op1, BoundExpression op2, DiagnosticBag diagnostics)
        {
            Debug.Assert(op1 != null);
            Debug.Assert(op2 != null);

            bool hasErrors = op1.HasAnyErrors || op2.HasAnyErrors;

            if (!op1.HasAnyErrors)
            {
                // Build bound conversion. The node might not be used if this is a dynamic conversion 
                // but diagnostics should be reported anyways.
                var conversion = GenerateConversionForAssignment(op1.Type, op2, diagnostics);

                // If the result is a dynamic assignment operation (SetMember or SetIndex), 
                // don't generate the boxing conversion to the dynamic type.
                // Leave the values as they are, and deal with the conversions at runtime.
                if (op1.Kind != BoundKind.DynamicIndexerAccess &&
                    op1.Kind != BoundKind.DynamicMemberAccess &&
                    op1.Kind != BoundKind.DynamicObjectInitializerMember)
                {
                    op2 = conversion;
                }
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

            return new BoundAssignmentOperator(node, op1, op2, type, hasErrors: hasErrors);
        }

        private bool CheckIsRefAssignable(CSharpSyntaxNode node, BoundExpression expr, DiagnosticBag diagnostics)
        {
            Debug.Assert(expr != null);

            if (expr.HasAnyErrors)
            {
                return false;
            }

            var boundLocal = expr as BoundLocal;
            if (boundLocal != null)
            {
                if (boundLocal.LocalSymbol.RefKind != RefKind.None)
                {
                    return true;
                }
                Error(diagnostics, ErrorCode.ERR_MustBeRefAssignable, node, boundLocal.LocalSymbol);
            }
            else
            {
                Error(diagnostics, ErrorCode.ERR_MustBeRefAssignableLocal, node);
            }

            return false;
        }

        private static PropertySymbol GetPropertySymbol(BoundExpression expr, out BoundExpression receiver, out CSharpSyntaxNode propertySyntax)
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

        private static EventSymbol GetEventSymbol(BoundExpression expr, out BoundExpression receiver, out CSharpSyntaxNode eventSyntax)
        {
            if (expr.Kind != BoundKind.EventAccess)
            {
                receiver = null;
                eventSyntax = null;
                return null;
            }

            CSharpSyntaxNode syntax = expr.Syntax;
            switch (syntax.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    eventSyntax = ((MemberAccessExpressionSyntax)syntax).Name;
                    break;
                case SyntaxKind.QualifiedName:
                    // This case is reachable only through SemanticModel
                    eventSyntax = ((QualifiedNameSyntax)syntax).Right;
                    break;
                case SyntaxKind.IdentifierName:
                    eventSyntax = syntax;
                    break;
                case SyntaxKind.MemberBindingExpression:
                    eventSyntax = ((MemberBindingExpressionSyntax)syntax).Name;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
            }

            BoundEventAccess eventAccess = (BoundEventAccess)expr;
            receiver = eventAccess.ReceiverOpt;

            return eventAccess.EventSymbol;
        }

        /// <summary>
        /// Check the expression is of the required lvalue and rvalue specified by valueKind.
        /// The method returns the original expression if the expression is of the required
        /// type. Otherwise, an appropriate error is added to the diagnostics bag and the
        /// method returns a BoundBadExpression node. The method returns the original
        /// expression without generating any error if the expression has errors.
        /// </summary>
        private BoundExpression CheckValue(BoundExpression expr, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.PropertyGroup:
                    expr = BindIndexedPropertyAccess((BoundPropertyGroup)expr, mustHaveAllOptionalParameters: false, diagnostics: diagnostics);
                    break;
            }

            bool hasResolutionErrors = false;

            // If this a MethodGroup where an rvalue is not expected or where the caller will not explicitly handle
            // (and resolve) MethodGroups (in short, cases where valueKind != BindValueKind.RValueOrMethodGroup),
            // resolve the MethodGroup here to generate the appropriate errors, otherwise resolution errors (such as
            // "member is inaccessible") will be dropped.
            if (expr.Kind == BoundKind.MethodGroup && valueKind != BindValueKind.RValueOrMethodGroup)
            {
                var methodGroup = (BoundMethodGroup)expr;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteDiagnostics: ref useSiteDiagnostics);
                diagnostics.Add(expr.Syntax, useSiteDiagnostics);
                Symbol otherSymbol = null;
                bool resolvedToMethodGroup = resolution.MethodGroup != null;
                if (!expr.HasAnyErrors) diagnostics.AddRange(resolution.Diagnostics); // Suppress cascading.
                hasResolutionErrors = resolution.HasAnyErrors;
                if (hasResolutionErrors)
                {
                    otherSymbol = resolution.OtherSymbol;
                }
                resolution.Free();

                // It's possible the method group is not a method group at all, but simply a
                // delayed lookup that resolved to a non-method member (perhaps an inaccessible
                // field or property), or nothing at all. In those cases, the member should not be exposed as a
                // method group, not even within a BoundBadExpression. Instead, the
                // BoundBadExpression simply refers to the receiver and the resolved symbol (if any).
                if (!resolvedToMethodGroup)
                {
                    Debug.Assert(methodGroup.ResultKind != LookupResultKind.Viable);
                    BoundNode receiver = methodGroup.ReceiverOpt;
                    if ((object)otherSymbol != null && receiver?.Kind == BoundKind.TypeOrValueExpression)
                    {
                        // Since we're not accessing a method, this can't be a Color Color case, so TypeOrValueExpression should not have been used.
                        // CAVEAT: otherSymbol could be invalid in some way (e.g. inaccessible), in which case we would have fallen back on a
                        // method group lookup (to allow for extension methods), which would have required a TypeOrValueExpression.
                        Debug.Assert(methodGroup.LookupError != null);

                        // Since we have a concrete member in hand, we can resolve the receiver.
                        var typeOrValue = (BoundTypeOrValueExpression)receiver;
                        receiver = otherSymbol.IsStatic
                            ? null // no receiver required
                            : typeOrValue.Data.ValueExpression;
                    }
                    return new BoundBadExpression(
                        expr.Syntax,
                        methodGroup.ResultKind,
                        (object)otherSymbol == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create(otherSymbol),
                        receiver == null ? ImmutableArray<BoundNode>.Empty : ImmutableArray.Create(receiver),
                        GetNonMethodMemberType(otherSymbol));
                }
            }

            if (!hasResolutionErrors && CheckValueKind(expr, valueKind, diagnostics) ||
                expr.HasAnyErrors && valueKind == BindValueKind.RValueOrMethodGroup)
            {
                return expr;
            }

            var resultKind = (valueKind == BindValueKind.RValue || valueKind == BindValueKind.RValueOrMethodGroup) ?
                LookupResultKind.NotAValue :
                LookupResultKind.NotAVariable;

            return ToBadExpression(expr, resultKind);
        }

        internal bool CheckValueKind(BoundExpression expr, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            if (expr.HasAnyErrors)
            {
                return false;
            }

            switch (expr.Kind)
            {
                case BoundKind.PropertyAccess:
                case BoundKind.IndexerAccess:
                    return CheckPropertyValueKind(expr, valueKind, diagnostics);
                case BoundKind.EventAccess:
                    return CheckEventValueKind((BoundEventAccess)expr, valueKind, diagnostics);
                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    return true;
                default:
                    {
                        if (RequiresSettingValue(valueKind))
                        {
                            if (!CheckIsVariable(expr.Syntax, expr, valueKind, false, diagnostics))
                            {
                                return false;
                            }
                        }

                        if (RequiresGettingValue(valueKind))
                        {
                            if (!CheckNotNamespaceOrType(expr, diagnostics))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
            }
        }

        private bool CheckEventValueKind(BoundEventAccess boundEvent, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            // Compound assignment (actually "event assignment") is allowed "everywhere", subject to the restrictions of
            // accessibility, use site errors, and receiver variable-ness (for structs).
            // Other operations are allowed only for field-like events and only where the backing field is accessible
            // (i.e. in the declaring type) - subject to use site errors and receiver variable-ness.

            BoundExpression receiver;
            CSharpSyntaxNode eventSyntax; //does not include receiver
            EventSymbol eventSymbol = GetEventSymbol(boundEvent, out receiver, out eventSyntax);

            switch (valueKind)
            {
                case BindValueKind.CompoundAssignment:
                    {
                        // NOTE: accessibility has already been checked by lookup.
                        // NOTE: availability of well-known members is checked in BindEventAssignment because
                        // we don't have the context to determine whether addition or subtraction is being performed.

                        if (receiver?.Kind == BoundKind.BaseReference && eventSymbol.IsAbstract)
                        {
                            Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, boundEvent.Syntax, eventSymbol);
                            return false;
                        }
                        else if (ReportUseSiteDiagnostics(eventSymbol, diagnostics, eventSyntax))
                        {
                            // NOTE: BindEventAssignment checks use site errors on the specific accessor 
                            // (since we don't know which is being used).
                            return false;
                        }

                        Debug.Assert(!RequiresVariableReceiver(receiver, eventSymbol));
                        return true;
                    }
                case BindValueKind.Assignment:
                case BindValueKind.RValue:
                case BindValueKind.RValueOrMethodGroup:
                case BindValueKind.RefOrOut:
                case BindValueKind.RefReturn:
                case BindValueKind.IncrementDecrement:
                case BindValueKind.AddressOf:
                    {
                        if (!boundEvent.IsUsableAsField)
                        {
                            // Dev10 reports this in addition to ERR_BadAccess, but we won't even reach this point if the event isn't accessible (caught by lookup).
                            Error(diagnostics, GetBadEventUsageDiagnosticInfo(eventSymbol), eventSyntax);
                            return false;
                        }
                        else if (ReportUseSiteDiagnostics(eventSymbol, diagnostics, eventSyntax))
                        {
                            if (valueKind == BindValueKind.RefReturn && !CheckIsValidReceiverForVariable(eventSyntax, receiver, BindValueKind.RefReturn, diagnostics))
                            {
                                return false;
                            }
                            else if (!CheckIsValidReceiverForVariable(eventSyntax, receiver, BindValueKind.Assignment, diagnostics))
                            {
                            return false;
                        }
                        }
                        else if (RequiresSettingValue(valueKind))
                        {
                            if (eventSymbol.IsWindowsRuntimeEvent && valueKind != BindValueKind.Assignment)
                            {
                                // NOTE: Dev11 reports ERR_RefProperty, as if this were a property access (since that's how it will be lowered).
                                // Roslyn reports a new, more specific, error code.
                                ErrorCode errorCode = valueKind == BindValueKind.RefOrOut ? ErrorCode.ERR_WinRtEventPassedByRef : GetStandardLvalueError(valueKind);
                                Error(diagnostics, errorCode, eventSyntax, eventSymbol);

                                return false;
                            }
                            else if (RequiresVariableReceiver(receiver, eventSymbol.AssociatedField) && // NOTE: using field, not event
                                !CheckIsValidReceiverForVariable(eventSyntax, receiver, BindValueKind.Assignment, diagnostics))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(valueKind);
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

        private bool CheckPropertyValueKind(BoundExpression expr, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            // SPEC: If the left operand is a property or indexer access, the property or indexer must
            // SPEC: have a set accessor. If this is not the case, a compile-time error occurs.

            // Addendum: Assignment is also allowed for get-only autoprops in their constructor

            BoundExpression receiver;
            CSharpSyntaxNode propertySyntax;
            var propertySymbol = GetPropertySymbol(expr, out receiver, out propertySyntax);

            Debug.Assert((object)propertySymbol != null);
            Debug.Assert(propertySyntax != null);

            var node = expr.Syntax;

            if (RequiresAddressableValue(valueKind) && propertySymbol.RefKind == RefKind.None)
            {
                // We know the outcome, we just want the diagnostics.
                bool isVariable = CheckIsVariable(node, expr, valueKind, false, diagnostics);
                return false;
            }

            if (RequiresSettingValue(valueKind) && propertySymbol.RefKind == RefKind.None)
            {
                var setMethod = propertySymbol.GetOwnOrInheritedSetMethod();

                if ((object)setMethod == null)
                {
                    var containing = this.ContainingMemberOrLambda;
                    if (!AccessingAutopropertyFromConstructor(receiver, propertySymbol, containing))
                    {
                        Error(diagnostics, ErrorCode.ERR_AssgReadonlyProp, node, propertySymbol);
                        return false;
                    }
                }
                else if (receiver?.Kind == BoundKind.BaseReference && setMethod.IsAbstract)
                {
                    Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, node, propertySymbol);
                    return false;
                }
                else if (!object.Equals(setMethod.GetUseSiteDiagnostic(), propertySymbol.GetUseSiteDiagnostic()) && ReportUseSiteDiagnostics(setMethod, diagnostics, propertySyntax))
                {
                    return false;
                }
                else
                {
                    var accessThroughType = this.GetAccessThroughType(receiver);
                    bool failedThroughTypeCheck;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bool isAccessible = this.IsAccessible(setMethod, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);

                    if (!isAccessible)
                    {
                        if (failedThroughTypeCheck)
                        {
                            Error(diagnostics, ErrorCode.ERR_BadProtectedAccess, node, propertySymbol, accessThroughType, this.ContainingType);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_InaccessibleSetter, node, propertySymbol);
                        }
                        return false;
                    }

                    ReportDiagnosticsIfObsolete(diagnostics, setMethod, node, receiver?.Kind == BoundKind.BaseReference);

                    if (RequiresVariableReceiver(receiver, setMethod) && !CheckIsValidReceiverForVariable(node, receiver, BindValueKind.Assignment, diagnostics))
                        {
                            return false;
                        }
                    }
                }

            var isIndirectSet = RequiresSettingValue(valueKind) && propertySymbol.RefKind != RefKind.None;
            if (RequiresGettingValue(valueKind) || isIndirectSet)
            {
                var getMethod = propertySymbol.GetOwnOrInheritedGetMethod();

                if ((object)getMethod == null)
                {
                    Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, node, propertySymbol);
                    return false;
                }
                else if (receiver?.Kind == BoundKind.BaseReference && getMethod.IsAbstract)
                {
                    Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, node, propertySymbol);
                    return false;
                }
                else if (!object.Equals(getMethod.GetUseSiteDiagnostic(), propertySymbol.GetUseSiteDiagnostic()) && ReportUseSiteDiagnostics(getMethod, diagnostics, propertySyntax))
                {
                    return false;
                }
                else
                {
                    var accessThroughType = this.GetAccessThroughType(receiver);
                    bool failedThroughTypeCheck;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bool isAccessible = this.IsAccessible(getMethod, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);

                    if (!isAccessible)
                    {
                        if (failedThroughTypeCheck)
                        {
                            Error(diagnostics, ErrorCode.ERR_BadProtectedAccess, node, propertySymbol, accessThroughType, this.ContainingType);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_InaccessibleGetter, node, propertySymbol);
                        }
                        return false;
                    }

                    ReportDiagnosticsIfObsolete(diagnostics, getMethod, node, receiver?.Kind == BoundKind.BaseReference);
                }
            }

            return true;
        }

        internal static bool AccessingAutopropertyFromConstructor(BoundPropertyAccess propertyAccess, Symbol fromMember)
        {
            return AccessingAutopropertyFromConstructor(propertyAccess.ReceiverOpt, propertyAccess.PropertySymbol, fromMember);
        }

        internal static bool AccessingAutopropertyFromConstructor(BoundExpression receiver, PropertySymbol propertySymbol, Symbol fromMember)
        {
            var sourceProperty = propertySymbol as SourcePropertySymbol;
            var propertyIsStatic = propertySymbol.IsStatic;

            return (object)sourceProperty != null &&
                    sourceProperty.IsAutoProperty &&
                    sourceProperty.ContainingType == fromMember.ContainingType &&
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

        /// <summary>
        /// SPEC: When a property or indexer declared in a struct-type is the target of an 
        /// SPEC: assignment, the instance expression associated with the property or indexer 
        /// SPEC: access must be classified as a variable. If the instance expression is 
        /// SPEC: classified as a value, a compile-time error occurs. Because of 7.6.4, 
        /// SPEC: the same rule also applies to fields.
        /// </summary>
        /// <remarks>
        /// NOTE: The spec fails to impose the restriction that the receiver must be classified
        /// as a variable (unlike for properties - 7.17.1).  This seems like a bug, but we have
        /// production code that won't build with the restriction in place (see DevDiv #15674).
        /// </remarks>
        private static bool RequiresVariableReceiver(BoundExpression receiver, Symbol symbol)
        {
            return !symbol.IsStatic
                && symbol.Kind != SymbolKind.Event
                && receiver?.Type?.IsValueType == true;
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

            if (destinationType.Kind == SymbolKind.ArrayType)
            {
                return BindArrayCreationWithInitializer(diagnostics, null,
                    (InitializerExpressionSyntax)node, (ArrayTypeSymbol)destinationType,
                    ImmutableArray<BoundExpression>.Empty);
            }

            return BindUnexpectedArrayInitializer((InitializerExpressionSyntax)node, diagnostics, ErrorCode.ERR_ArrayInitToNonArrayType);
        }

        internal static void DeclareLocalVariable(
            SourceLocalSymbol symbol,
            SyntaxToken identifierToken,
            TypeSymbol type)
        {
            // In the original compiler this
            // method has many side effects; it sets the type
            // of the local symbol, it gives errors if the local
            // is a duplicate, it creates new symbols for lambda
            // expressions, puts stuff in caches, and so on.

            Debug.Assert((object)symbol != null);
            Debug.Assert(symbol.IdentifierToken == identifierToken);
            symbol.SetTypeSymbol(type);
            // UNDONE: Can we come up with a way to set the type of a local which does
            // UNDONE: not duplicate work and does not mutate the symbol?
        }

        protected virtual SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return Next.LookupLocal(nameToken);
        }

        protected virtual LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            return Next.LookupLocalFunction(nameToken);
        }

        internal BoundBlock BindEmbeddedBlock(BlockSyntax node, DiagnosticBag diagnostics)
        {
            return this.GetBinder(node).BindBlock(node, diagnostics);
        }

        private BoundBlock BindBlock(BlockSyntax node, DiagnosticBag diagnostics)
        {
            var syntaxStatements = node.Statements;
            int nStatements = syntaxStatements.Count;

            ArrayBuilder<BoundStatement> boundStatements = ArrayBuilder<BoundStatement>.GetInstance(nStatements);

            for (int i = 0; i < nStatements; i++)
            {
                var boundStatement = BindStatement(syntaxStatements[i], diagnostics);
                boundStatements.Add(boundStatement);
            }

            if (IsDirectlyInIterator)
            {
                var method = ContainingMemberOrLambda as MethodSymbol;
                if ((object)method != null)
                {
                    method.IteratorElementType = GetIteratorElementType(null, diagnostics);
                }
                else
                {
                    Debug.Assert(!diagnostics.IsEmptyWithoutResolution);
                }

                foreach (var local in Locals)
                {
                    if (local.RefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadIteratorLocalType, local.Locations[0]);
                    }
                }
            }

            return new BoundBlock(
                node,
                GetDeclaredLocalsForScope(node),
                GetDeclaredLocalFunctionsForScope(node),
                boundStatements.ToImmutableAndFree());
        }

        internal BoundExpression GenerateConversionForAssignment(TypeSymbol targetType, BoundExpression expression, DiagnosticBag diagnostics, bool isDefaultParameter = false, RefKind refKind = RefKind.None)
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

            // UNDONE: cast in code
            if (refKind != RefKind.None)
            {
                if (conversion.Kind != ConversionKind.Identity)
                {   
                    Error(diagnostics, ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, expression.Syntax, targetType);
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

                return new BoundConversion(
                    expression.Syntax,
                    expression,
                    conversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: false,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: targetType,
                    hasErrors: true);
            }

            return CreateConversion(expression.Syntax, expression, conversion, false, targetType, diagnostics);
        }

        internal void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, CSharpSyntaxNode syntax,
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
            // (Foo foo)=>{} to Action<Foo> is a perfectly legal conversion even if Foo is undefined!
            // In that case we have already reported an error that Foo is undefined, so just bail out.

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
                Error(diagnostics, ErrorCode.ERR_ExpressionTreeMustHaveDelegate, syntax, ((NamedTypeSymbol)targetType).TypeArgumentsNoUseSiteDiagnostics[0]);
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
                Error(diagnostics, ErrorCode.ERR_BadDelArgCount, syntax, targetType, anonymousFunction.ParameterCount);
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
                            i + 1, delegateRefKind.ToDisplayString());
                    }
                }
                return;
            }

            // See the comments in IsAnonymousFunctionCompatibleWithDelegate for an explanation of this one.
            if (reason == LambdaConversionResult.StaticTypeInImplicitlyTypedLambda)
            {
                for (int i = 0; i < anonymousFunction.ParameterCount; ++i)
                {
                    if (delegateParameters[i].Type.IsStatic)
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
                // Cannot convert {0} to delegate type '{1}' because the parameter types do not match the delegate parameter types
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

                    if (!lambdaParameterType.Equals(delegateParameterType, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true))
                    {
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, lambdaParameterType, delegateParameterType);

                        // Parameter {0} is declared as type '{1}{2}' but should be '{3}{4}'
                        Error(diagnostics, ErrorCode.ERR_BadParamType, lambdaParameterLocation,
                            i + 1, lambdaRefKind.ToPrefix(), distinguisher.First, delegateRefKind.ToPrefix(), distinguisher.Second);
                    }
                    else if (lambdaRefKind != delegateRefKind)
                    {
                        if (delegateRefKind == RefKind.None)
                        {
                            // Parameter {0} should not be declared with the '{1}' keyword
                            Error(diagnostics, ErrorCode.ERR_BadParamExtraRef, lambdaParameterLocation, i + 1, lambdaRefKind.ToDisplayString());
                        }
                        else
                        {
                            // Parameter {0} must be declared with the '{1}' keyword
                            Error(diagnostics, ErrorCode.ERR_BadParamRef, lambdaParameterLocation, i + 1, delegateRefKind.ToDisplayString());
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
        }

        protected static void GenerateImplicitConversionError(DiagnosticBag diagnostics, Compilation compilation, CSharpSyntaxNode syntax,
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
                else
                {
                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, sourceType, targetType);
                    Error(diagnostics, ErrorCode.ERR_NoImplicitConv, syntax, distinguisher.First, distinguisher.Second);
                }
            }
        }

        protected void GenerateImplicitConversionError(DiagnosticBag diagnostics, CSharpSyntaxNode syntax,
            Conversion conversion, BoundExpression expression, TypeSymbol targetType)
        {
            Debug.Assert(expression != null);
            Debug.Assert((object)targetType != null);

            if (targetType.TypeKind == TypeKind.Error)
            {
                return;
            }

            if (expression.Kind == BoundKind.BadExpression)
            {
                return;
            }

            if (expression.Kind == BoundKind.UnboundLambda)
            {
                GenerateAnonymousFunctionConversionError(diagnostics, syntax, (UnboundLambda)expression, targetType);
                return;
            }

            var sourceType = expression.Type;
            if ((object)sourceType != null)
            {
                GenerateImplicitConversionError(diagnostics, this.Compilation, syntax, conversion, sourceType, targetType, expression.ConstantValue);
                return;
            }

            if (expression.IsLiteralNull())
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

            if (expression.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)expression;
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

            Debug.Assert(expression.HasAnyErrors && expression.Kind != BoundKind.UnboundLambda, "Missing a case in implicit conversion error reporting");
        }

        private BoundStatement BindIfStatement(IfStatementSyntax node, DiagnosticBag diagnostics)
        {
            var condition = BindBooleanExpression(node.Condition, diagnostics);
            var consequence = BindPossibleEmbeddedStatement(node.Statement, diagnostics);

            // Note that the else clause does not use the pattern variable binder;
            // pattern variables from the condition are not in scope in the else statement.
            BoundStatement alternative = (node.Else == null) ? null : BindPossibleEmbeddedStatement(node.Else.Statement, diagnostics);

            // If there were patterns in the condition, their variables are in scope within the condition and then clause, but not the else clause
            PatternVariableBinder patternBinder = this as PatternVariableBinder;
            ImmutableArray<LocalSymbol> patternVariables = (patternBinder != null && patternBinder.Syntax == node) ? patternBinder.Locals : ImmutableArray<LocalSymbol>.Empty;
            BoundStatement result = new BoundIfStatement(node, patternVariables, condition, consequence, alternative);
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
                return BoundConversion.Synthesized(node, expr, Conversion.NoConversion, false, false, ConstantValue.NotAvailable, boolean, hasErrors: true);
            }

            // Oddly enough, "if(dyn)" is bound not as a dynamic conversion to bool, but as a dynamic
            // invocation of operator true.

            if (expr.HasDynamicType())
            {
                return new BoundUnaryOperator(
                    node,
                    UnaryOperatorKind.DynamicTrue,
                    expr,
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

                    return expr;
                }
                else
                {
                    return CreateConversion(
                        syntax: expr.Syntax,
                        source: expr,
                        conversion: conversion,
                        isCast: false,
                        wasCompilerGenerated: true,
                        destination: boolean,
                        diagnostics: diagnostics);
                }
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
                return BoundConversion.Synthesized(node, expr, Conversion.NoConversion, false, false, ConstantValue.NotAvailable, boolean, hasErrors: true);
            }

            UnaryOperatorSignature signature = best.Signature;

            BoundExpression resultOperand = CreateConversion(
                node,
                expr,
                best.Conversion,
                isCast: false,
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
            return switchBinder.BindSwitchExpressionAndSections(node, switchBinder, diagnostics);
        }

        internal virtual BoundStatement BindSwitchExpressionAndSections(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            return this.Next.BindSwitchExpressionAndSections(node, originalBinder, diagnostics);
        }

        private BoundWhileStatement BindWhile(WhileStatementSyntax node, DiagnosticBag diagnostics)
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

        private BoundDoStatement BindDo(DoStatementSyntax node, DiagnosticBag diagnostics)
        {
            var loopBinder = this.GetBinder(node);
            Debug.Assert(loopBinder != null);
            return loopBinder.BindDoParts(diagnostics, loopBinder);
        }

        internal virtual BoundDoStatement BindDoParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindDoParts(diagnostics, originalBinder);
        }

        private BoundForStatement BindFor(ForStatementSyntax node, DiagnosticBag diagnostics)
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

            AliasSymbol alias;
            bool isVar;
            TypeSymbol declType = BindType(typeSyntax, diagnostics, out isVar, out alias);

            Debug.Assert((object)declType != null || isVar);

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
                var declaration = BindVariableDeclaration(localKind, isVar, variableDeclarator, typeSyntax, declType, alias, diagnostics);

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
                return BoundStatementList.Synthesized((CSharpSyntaxNode)statements.Node, statementBuilder.ToImmutableAndFree());
            }
        }

        private BoundStatement BindForEach(ForEachStatementSyntax node, DiagnosticBag diagnostics)
        {
            Binder loopBinder = this.GetBinder(node);
            return loopBinder.BindForEachParts(diagnostics, loopBinder);
        }

        internal virtual BoundStatement BindForEachParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            return this.Next.BindForEachParts(diagnostics, originalBinder);
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

        protected virtual TypeSymbol GetCurrentReturnType(out RefKind refKind)
        {
            var symbol = this.ContainingMemberOrLambda as MethodSymbol;
            if ((object)symbol != null)
        {
                refKind = symbol.RefKind;
                return symbol.ReturnType;
            }
            refKind = RefKind.None;
            return null;
        }

        private BoundReturnStatement BindReturn(ReturnStatementSyntax syntax, DiagnosticBag diagnostics)
        {
            var refKind = syntax.RefKeyword.Kind().GetRefKind();

            var expressionSyntax = syntax.Expression;
            BoundExpression arg = null;
            if (expressionSyntax != null)
            {
                arg = BindValue(expressionSyntax, diagnostics, refKind != RefKind.None ? BindValueKind.RefReturn : BindValueKind.RValue);
            }
            else
            {
                // If this is a void return statement in a script, return default(T).
                var interactiveInitializerMethod = this.ContainingMemberOrLambda as SynthesizedInteractiveInitializerMethod;
                if (interactiveInitializerMethod != null)
                {
                    arg = new BoundDefaultOperator(interactiveInitializerMethod.GetNonNullSyntaxNode(), interactiveInitializerMethod.ResultType);
                }
            }

            RefKind sigRefKind;
            TypeSymbol retType = GetCurrentReturnType(out sigRefKind);

            bool hasErrors;
            if (IsDirectlyInIterator)
            {
                diagnostics.Add(ErrorCode.ERR_ReturnInIterator, syntax.ReturnKeyword.GetLocation());
                hasErrors = true;
            }
            else if (IsInAsyncMethod() && refKind != RefKind.None)
            {
                // This can happen if we are binding an async anonymous method to a delegate type.
                diagnostics.Add(ErrorCode.ERR_MustNotHaveRefReturn, syntax.ReturnKeyword.GetLocation());
                hasErrors = true;
            }
            else if ((object)retType != null && (refKind != RefKind.None) != (sigRefKind != RefKind.None))
            {
                var errorCode = refKind != RefKind.None
                    ? ErrorCode.ERR_MustNotHaveRefReturn
                    : ErrorCode.ERR_MustHaveRefReturn;
                diagnostics.Add(errorCode, syntax.ReturnKeyword.GetLocation());
                hasErrors = true;
            }
            else if (arg != null)
            {
                hasErrors = arg.HasErrors || ((object)arg.Type != null && arg.Type.IsErrorType());
            }
            else
            {
                hasErrors = false;
            }

            if (hasErrors)
            {
                return new BoundReturnStatement(syntax, refKind, arg, hasErrors: true);
            }

            // The return type could be null; we might be attempting to infer the return type either 
            // because of method type inference, or because we are attempting to do error analysis 
            // on a lambda expression of unknown return type.
            if ((object)retType != null)
            {
                if (retType.SpecialType == SpecialType.System_Void || IsTaskReturningAsyncMethod())
                {
                    if (arg != null)
                    {
                        var container = this.ContainingMemberOrLambda;
                        var lambda = container as LambdaSymbol;
                        if ((object)lambda != null)
                        {
                            // Error case: void-returning or async task-returning method or lambda with "return x;" 
                            var errorCode = retType.SpecialType == SpecialType.System_Void
                                ? ErrorCode.ERR_RetNoObjectRequiredLambda
                                : ErrorCode.ERR_TaskRetNoObjectRequiredLambda;

                            // Anonymous function converted to a void returning delegate cannot return a value
                            Error(diagnostics, errorCode, syntax.ReturnKeyword);

                            // COMPATIBILITY: The native compiler also produced an error
                            // COMPATIBILITY: "Cannot convert lambda expression to delegate type 'Action' because some of the
                            // COMPATIBILITY: return types in the block are not implicitly convertible to the delegate return type"
                            // COMPATIBILITY: This error doesn't make sense in the "void" case because the whole idea of 
                            // COMPATIBILITY: "conversion to void" is a bit unusual, and we've already given a good error.
                        }
                        else
                        {
                            // Error case: void-returning or async task-returning method or lambda with "return x;" 
                            var errorCode = retType.SpecialType == SpecialType.System_Void
                                ? ErrorCode.ERR_RetNoObjectRequired
                                : ErrorCode.ERR_TaskRetNoObjectRequired;

                            Error(diagnostics, errorCode, syntax.ReturnKeyword, container);
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
                    }
                    else
                    {
                        arg = CreateReturnConversion(syntax, diagnostics, arg, sigRefKind, retType);
                    }
                }
            }
            else
            {
                // Check that the returned expression is not void.
                if ((object)arg?.Type != null && arg.Type.SpecialType == SpecialType.System_Void)
                {
                    Error(diagnostics, ErrorCode.ERR_CantReturnVoid, expressionSyntax);
                }
            }

            return new BoundReturnStatement(syntax, refKind, arg);
        }

        internal BoundExpression CreateReturnConversion(
            CSharpSyntaxNode syntax,
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
                    }
                }
                else if (!conversion.IsImplicit || !conversion.IsValid)
            {
                if (!badAsyncReturnAlreadyReported)
                {
                        RefKind unusedRefKind;
                        if (IsGenericTaskReturningAsyncMethod() && argument.Type == this.GetCurrentReturnType(out unusedRefKind))
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

            return CreateConversion(argument.Syntax, argument, conversion, false, returnType, diagnostics);
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
            foreach (var catchSyntax in catchClauses)
            {
                var catchBinder = this.GetBinder(catchSyntax);
                var catchBlock = catchBinder.BindCatchBlock(catchSyntax, catchBlocks, diagnostics);
                catchBlocks.Add(catchBlock);
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
                type = this.BindType(declaration.Type, diagnostics);
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
                        else if (previousType == Compilation.GetWellKnownType(WellKnownType.System_Exception) &&
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

            BoundExpression exceptionSource = null;
            LocalSymbol local = this.Locals.FirstOrDefault();
            if ((object)local != null)
            {
                Debug.Assert(this.Locals.Length == 1);

                // Check for local variable conflicts in the *enclosing* binder, not the *current* binder;
                // obviously we will find a local of the given name in the current binder.
                hasError |= this.ValidateDeclarationNameConflictsInScope(local, diagnostics);

                exceptionSource = new BoundLocal(declaration, local, ConstantValue.NotAvailable, local.Type);
            }

            var block = BindEmbeddedBlock(node.Block, diagnostics);
            Debug.Assert((object)local == null || local.DeclarationKind == LocalDeclarationKind.CatchVariable);
            Debug.Assert((object)local == null || local.Type.IsErrorType() || (local.Type == type));
            return new BoundCatchBlock(node, local, exceptionSource, type, boundFilter, block, hasError);
        }

        private BoundExpression BindCatchFilter(CatchFilterClauseSyntax filter, DiagnosticBag diagnostics)
        {
            // TODO: should pattern variables declared in a catch filter be available in the catch block?
            PatternVariableBinder patternBinder = new PatternVariableBinder(filter, filter.FilterExpression, this);
            BoundExpression boundFilter = patternBinder.BindBooleanExpression(filter.FilterExpression, diagnostics);
            if (boundFilter.ConstantValue != ConstantValue.NotAvailable)
            {
                Error(diagnostics, ErrorCode.WRN_FilterIsConstant, filter.FilterExpression);
            }

            boundFilter = patternBinder.WrapWithVariablesIfAny(boundFilter);
            boundFilter = new BoundSequencePointExpression(filter, boundFilter, boundFilter.Type);
            return boundFilter;
        }


        // Report an extra error on the return if we are in a lambda conversion.
        private void ReportCantConvertLambdaReturn(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // UNDONE: Suppress this error if the lambda is a result of a query rewrite.

            var lambda = this.ContainingMemberOrLambda as LambdaSymbol;
            if ((object)lambda != null)
            {
                if (IsInAsyncMethod())
                {
                    // Cannot convert async {0} to intended delegate type. An async {0} may return void, Task or Task<T>, none of which are convertible to '{1}'.
                    Error(diagnostics, ErrorCode.ERR_CantConvAsyncAnonFuncReturns,
                        syntax,
                        lambda.MessageID.Localize(), lambda.ReturnType);
                }
                else
                {
                    // Cannot convert {0} to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                    Error(diagnostics, ErrorCode.ERR_CantConvAnonMethReturns,
                        syntax,
                        lambda.MessageID.Localize());
                }
            }
        }

        private static bool IsValidStatementExpression(CSharpSyntaxNode syntax, BoundExpression expression)
        {
            bool syntacticallyValid = SyntaxFacts.IsStatementExpression(syntax);
            if (!syntacticallyValid)
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
                if ((refKind != RefKind.None) != (returnRefKind != RefKind.None))
            {
                    var errorCode = refKind != RefKind.None
                        ? ErrorCode.ERR_MustNotHaveRefReturn
                        : ErrorCode.ERR_MustHaveRefReturn;
                    Error(diagnostics, errorCode, syntax);
                    statement = new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
                }
                else if (returnType.SpecialType == SpecialType.System_Void || IsTaskReturningAsyncMethod())
                {
                    // If the return type is void then the expression is required to be a legal
                    // statement expression.

                    Debug.Assert(expressionSyntax != null || !IsValidStatementExpression(expression.Syntax, expression));

                    bool errors = false;
                    if (expressionSyntax == null || !IsValidStatementExpression(expressionSyntax, expression))
                    {
                        Error(diagnostics, ErrorCode.ERR_IllegalStatement, syntax);
                        errors = true;
                    }

                    // Don't mark compiler generated so that the rewriter generates sequence points
                    var expressionStatement = new BoundExpressionStatement(syntax, expression, errors);

                    CheckForUnobservedAwaitable(expression, diagnostics);
                    statement = expressionStatement;
                }
                else
                {
                    expression = CreateReturnConversion(syntax, diagnostics, expression, refKind, returnType);
                    statement = new BoundReturnStatement(syntax, returnRefKind, expression) { WasCompilerGenerated = true };
                }
            }
            else if (expression.Type?.SpecialType == SpecialType.System_Void)
            {
                statement = new BoundExpressionStatement(syntax, expression) { WasCompilerGenerated = true };
            }
            else
            {
                statement = new BoundReturnStatement(syntax, refKind, expression) { WasCompilerGenerated = true };
            }

            // Need to attach the tree for when we generate sequence points.
            return new BoundBlock(node, locals, ImmutableArray<LocalFunctionSymbol>.Empty, ImmutableArray.Create(statement)) { WasCompilerGenerated = node.Kind() != SyntaxKind.ArrowExpressionClause };
        }

        /// <summary>
        /// Binds an expression-bodied member with expression e as either { return e;} or { e; }.
        /// </summary>
        internal BoundBlock BindExpressionBodyAsBlock(ArrowExpressionClauseSyntax expressionBody,
                                                      DiagnosticBag diagnostics)
        {
            RefKind refKind = expressionBody.RefKeyword.Kind().GetRefKind();
            BoundExpression expression = BindValue(expressionBody.Expression, diagnostics, refKind != RefKind.None ? BindValueKind.RefReturn : BindValueKind.RValue);
            return CreateBlockFromExpression(expressionBody, this.Locals, refKind, expression, expressionBody.Expression, diagnostics);
        }

        /// <summary>
        /// Binds a lambda with expression e as either { return e;} or { e; }.
        /// </summary>
        public BoundBlock BindLambdaExpressionAsBlock(RefKind refKind, ExpressionSyntax body, DiagnosticBag diagnostics)
        {
            BoundExpression expression = BindValue(body, diagnostics, refKind != RefKind.None ? BindValueKind.RefReturn : BindValueKind.RValue);
            return CreateBlockFromExpression(body, this.Locals, refKind, expression, body, diagnostics);
        }

        internal virtual ImmutableArray<LocalSymbol> Locals
        {
            get
            {
                return this.Next.Locals;
            }
        }

        internal virtual ImmutableArray<LocalFunctionSymbol> LocalFunctions
        {
            get
            {
                return this.Next.LocalFunctions;
            }
        }

        internal virtual ImmutableArray<LabelSymbol> Labels
        {
            get
            {
                return this.Next.Labels;
            }
        }
    }
}
