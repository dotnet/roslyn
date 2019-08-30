// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// Rewrite a using statement into a try finally statement.  Four forms are possible:
        ///   1) using (expr) stmt
        ///   2) await using (expr) stmt
        ///   3) using (C c = expr) stmt
        ///   4) await using (C c = expr) stmt
        ///
        /// The first two are handled by RewriteExpressionUsingStatement and the latter two are handled by
        /// RewriteDeclarationUsingStatement (called in a loop, once for each local declared).
        ///
        /// For the async variants, `IAsyncDisposable` is used instead of `IDisposable` and we produce
        /// `... await expr.DisposeAsync() ...` instead of `... expr.Dispose() ...`.
        /// </summary>
        /// <remarks>
        /// It would be more in line with our usual pattern to rewrite using to try-finally
        /// in the ControlFlowRewriter, but if we don't do it here the BoundMultipleLocalDeclarations
        /// will be rewritten into a form that makes them harder to separate.
        /// </remarks>
        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            BoundBlock tryBlock = rewrittenBody.Kind == BoundKind.Block
                ? (BoundBlock)rewrittenBody
                : BoundBlock.SynthesizedNoLocals(node.Syntax, rewrittenBody);

            if (node.ExpressionOpt != null)
            {
                return MakeExpressionUsingStatement(node, tryBlock);
            }
            else
            {
                SyntaxToken awaitKeyword = node.Syntax.Kind() == SyntaxKind.UsingStatement ? ((UsingStatementSyntax)node.Syntax).AwaitKeyword : default;
                return MakeDeclarationUsingStatement(node.Syntax,
                                                     tryBlock,
                                                     node.Locals,
                                                     node.DeclarationsOpt.LocalDeclarations,
                                                     node.IDisposableConversion,
                                                     node.DisposeMethodOpt,
                                                     node.AwaitOpt,
                                                     awaitKeyword);
            }
        }

        private BoundStatement MakeDeclarationUsingStatement(SyntaxNode syntax,
                                                       BoundBlock body,
                                                       ImmutableArray<LocalSymbol> locals,
                                                       ImmutableArray<BoundLocalDeclaration> declarations,
                                                       Conversion iDisposableConversion,
                                                       MethodSymbol disposeMethodOpt,
                                                       AwaitableInfo awaitOpt,
                                                       SyntaxToken awaitKeyword)
        {
            Debug.Assert(declarations != null);

            BoundBlock result = body;
            for (int i = declarations.Length - 1; i >= 0; i--) //NB: inner-to-outer = right-to-left
            {
                result = RewriteDeclarationUsingStatement(syntax, declarations[i], result, iDisposableConversion, awaitKeyword, awaitOpt, disposeMethodOpt);
            }

            // Declare all locals in a single, top-level block so that the scope is correct in the debugger
            // (Dev10 has them all come into scope at once, not per-declaration.)
            return new BoundBlock(
                syntax,
                locals,
                ImmutableArray.Create<BoundStatement>(result));
        }

        /// <summary>
        /// Lower "[await] using var x = (expression)" to a try-finally block.
        /// </summary>
        private BoundStatement MakeLocalUsingDeclarationStatement(BoundUsingLocalDeclarations usingDeclarations, ImmutableArray<BoundStatement> statements)
        {
            LocalDeclarationStatementSyntax syntax = (LocalDeclarationStatementSyntax)usingDeclarations.Syntax;
            BoundBlock body = new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, statements);

            var usingStatement = MakeDeclarationUsingStatement(syntax,
                                                               body,
                                                               ImmutableArray<LocalSymbol>.Empty,
                                                               usingDeclarations.LocalDeclarations,
                                                               usingDeclarations.IDisposableConversion,
                                                               usingDeclarations.DisposeMethodOpt,
                                                               awaitOpt: usingDeclarations.AwaitOpt,
                                                               awaitKeyword: syntax.AwaitKeyword);

            return usingStatement;
        }

        /// <summary>
        /// Lower "using [await] (expression) statement" to a try-finally block.
        /// </summary>
        private BoundBlock MakeExpressionUsingStatement(BoundUsingStatement node, BoundBlock tryBlock)
        {
            Debug.Assert(node.ExpressionOpt != null);
            Debug.Assert(node.DeclarationsOpt == null);

            // See comments in BuildUsingTryFinally for the details of the lowering to try-finally.
            //
            // SPEC: A using statement of the form "using (expression) statement; " has the 
            // SPEC: same three possible expansions [ as "using (ResourceType r = expression) statement; ]
            // SPEC: but in this case ResourceType is implicitly the compile-time type of the expression,
            // SPEC: and the resource variable is inaccessible to and invisible to the embedded statement.
            //
            // DELIBERATE SPEC VIOLATION: 
            //
            // The spec quote above implies that the expression must have a type; in fact we allow
            // the expression to be null.
            //
            // If expr is the constant null then we can elide the whole thing and simply generate the statement. 

            BoundExpression rewrittenExpression = (BoundExpression)Visit(node.ExpressionOpt);
            if (rewrittenExpression.ConstantValue == ConstantValue.Null)
            {
                Debug.Assert(node.Locals.IsEmpty); // TODO: This might not be a valid assumption in presence of semicolon operator.
                return tryBlock;
            }

            // Otherwise, we lower "using(expression) statement;" as follows:
            //
            // * If the expression is of type dynamic then we lower as though the user had written
            //
            //   using(IDisposable temp = (IDisposable)expression) statement;
            //
            //   Note that we have to do the conversion early, not in the finally block, because
            //   if the conversion fails at runtime with an exception then the exception must happen
            //   before the statement runs.
            //
            // * Otherwise we lower as though the user had written
            // 
            //   using(ResourceType temp = expression) statement;
            //

            TypeSymbol expressionType = rewrittenExpression.Type;
            SyntaxNode expressionSyntax = rewrittenExpression.Syntax;
            UsingStatementSyntax usingSyntax = (UsingStatementSyntax)node.Syntax;

            BoundAssignmentOperator tempAssignment;
            BoundLocal boundTemp;
            if ((object)expressionType == null || expressionType.IsDynamic())
            {
                // IDisposable temp = (IDisposable) expr;
                // or
                // IAsyncDisposable temp = (IAsyncDisposable) expr;
                TypeSymbol iDisposableType = node.AwaitOpt is null ?
                    _compilation.GetSpecialType(SpecialType.System_IDisposable) :
                    _compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable);

                BoundExpression tempInit = MakeConversionNode(
                    expressionSyntax,
                    rewrittenExpression,
                    Conversion.GetTrivialConversion(node.IDisposableConversion.Kind),
                    iDisposableType,
                    @checked: false,
                    constantValueOpt: rewrittenExpression.ConstantValue);

                boundTemp = _factory.StoreToTemp(tempInit, out tempAssignment, kind: SynthesizedLocalKind.Using);
            }
            else
            {
                // ResourceType temp = expr;
                boundTemp = _factory.StoreToTemp(rewrittenExpression, out tempAssignment, syntaxOpt: usingSyntax, kind: SynthesizedLocalKind.Using);
            }

            BoundStatement expressionStatement = new BoundExpressionStatement(expressionSyntax, tempAssignment);
            if (this.Instrument)
            {
                expressionStatement = _instrumenter.InstrumentUsingTargetCapture(node, expressionStatement);
            }

            BoundStatement tryFinally = RewriteUsingStatementTryFinally(usingSyntax, tryBlock, boundTemp, usingSyntax.AwaitKeyword, node.AwaitOpt, node.DisposeMethodOpt);

            // { ResourceType temp = expr; try { ... } finally { ... } }
            return new BoundBlock(
                syntax: usingSyntax,
                locals: node.Locals.Add(boundTemp.LocalSymbol),
                statements: ImmutableArray.Create<BoundStatement>(expressionStatement, tryFinally));
        }

        /// <summary>
        /// Lower "using [await] (ResourceType resource = expression) statement" to a try-finally block.
        /// </summary>
        /// <remarks>
        /// Assumes that the local symbol will be declared (i.e. in the LocalsOpt array) of an enclosing block.
        /// Assumes that using statements with multiple locals have already been split up into multiple using statements.
        /// </remarks>
        private BoundBlock RewriteDeclarationUsingStatement(SyntaxNode usingSyntax, BoundLocalDeclaration localDeclaration, BoundBlock tryBlock, Conversion iDisposableConversion, SyntaxToken awaitKeywordOpt, AwaitableInfo awaitOpt, MethodSymbol methodSymbol)
        {
            SyntaxNode declarationSyntax = localDeclaration.Syntax;

            LocalSymbol localSymbol = localDeclaration.LocalSymbol;
            TypeSymbol localType = localSymbol.Type;
            Debug.Assert((object)localType != null); //otherwise, there wouldn't be a conversion to IDisposable

            BoundLocal boundLocal = new BoundLocal(declarationSyntax, localSymbol, localDeclaration.InitializerOpt.ConstantValue, localType);

            BoundStatement rewrittenDeclaration = (BoundStatement)Visit(localDeclaration);

            // If we know that the expression is null, then we know that the null check in the finally block
            // will fail, and the Dispose call will never happen.  That is, the finally block will have no effect.
            // Consequently, we can simply skip the whole try-finally construct and just create a block containing
            // the new declaration.
            if (boundLocal.ConstantValue == ConstantValue.Null)
            {
                //localSymbol will be declared by an enclosing block
                return BoundBlock.SynthesizedNoLocals(usingSyntax, rewrittenDeclaration, tryBlock);
            }

            if (localType.IsDynamic())
            {
                TypeSymbol iDisposableType = awaitOpt is null ?
                    _compilation.GetSpecialType(SpecialType.System_IDisposable) :
                    _compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable);

                BoundExpression tempInit = MakeConversionNode(
                    declarationSyntax,
                    boundLocal,
                    iDisposableConversion,
                    iDisposableType,
                    @checked: false);

                BoundAssignmentOperator tempAssignment;
                BoundLocal boundTemp = _factory.StoreToTemp(tempInit, out tempAssignment, kind: SynthesizedLocalKind.Using);

                BoundStatement tryFinally = RewriteUsingStatementTryFinally(usingSyntax, tryBlock, boundTemp, awaitKeywordOpt, awaitOpt, methodSymbol);

                return new BoundBlock(
                    syntax: usingSyntax,
                    locals: ImmutableArray.Create<LocalSymbol>(boundTemp.LocalSymbol), //localSymbol will be declared by an enclosing block
                    statements: ImmutableArray.Create<BoundStatement>(
                        rewrittenDeclaration,
                        new BoundExpressionStatement(declarationSyntax, tempAssignment),
                        tryFinally));
            }
            else
            {
                BoundStatement tryFinally = RewriteUsingStatementTryFinally(usingSyntax, tryBlock, boundLocal, awaitKeywordOpt, awaitOpt, methodSymbol);

                // localSymbol will be declared by an enclosing block
                return BoundBlock.SynthesizedNoLocals(usingSyntax, rewrittenDeclaration, tryFinally);
            }
        }

        private BoundStatement RewriteUsingStatementTryFinally(SyntaxNode syntax, BoundBlock tryBlock, BoundLocal local, SyntaxToken awaitKeywordOpt, AwaitableInfo awaitOpt, MethodSymbol methodOpt)
        {
            // SPEC: When ResourceType is a non-nullable value type, the expansion is:
            // SPEC: 
            // SPEC: { 
            // SPEC:   ResourceType resource = expr; 
            // SPEC:   try { statement; } 
            // SPEC:   finally { ((IDisposable)resource).Dispose(); }
            // SPEC: }
            // SPEC:
            // SPEC: Otherwise, when Resource type is a nullable value type or
            // SPEC: a reference type other than dynamic, the expansion is:
            // SPEC: 
            // SPEC: { 
            // SPEC:   ResourceType resource = expr; 
            // SPEC:   try { statement; } 
            // SPEC:   finally { if (resource != null) ((IDisposable)resource).Dispose(); }
            // SPEC: }
            // SPEC: 
            // SPEC: Otherwise, when ResourceType is dynamic, the expansion is:
            // SPEC: { 
            // SPEC:   dynamic resource = expr; 
            // SPEC:   IDisposable d = (IDisposable)resource;
            // SPEC:   try { statement; } 
            // SPEC:   finally { if (d != null) d.Dispose(); }
            // SPEC: }
            // SPEC: 
            // SPEC: An implementation is permitted to implement a given using statement 
            // SPEC: differently -- for example, for performance reasons -- as long as the 
            // SPEC: behavior is consistent with the above expansion.
            //
            // In the case of using-await statement, we'll use "IAsyncDisposable" instead of "IDisposable", "await DisposeAsync()" instead of "Dispose()"
            //
            // And we do in fact generate the code slightly differently than precisely how it is 
            // described above.
            //
            // First: if the type is a non-nullable value type then we do not do the 
            // *boxing conversion* from the resource to IDisposable. Rather, we do
            // a *constrained virtual call* that elides the boxing if possible. 
            //
            // Now, you might wonder if that is legal; isn't skipping the boxing producing
            // an observable difference? Because if the value type is mutable and the Dispose
            // mutates it, then skipping the boxing means that we are now mutating the original,
            // not the boxed copy. But this is never observable. Either (1) we have "using(R r = x){}"
            // and r is out of scope after the finally, so it is not possible to observe the mutation,
            // or (2) we have "using(x) {}". But that has the semantics of "using(R temp = x){}",
            // so again, we are not mutating x to begin with; we're always mutating a copy. Therefore
            // it doesn't matter if we skip making *a copy of the copy*.
            //
            // This is what the dev10 compiler does, and we do so as well.
            //
            // Second: if the type is a nullable value type then we can similarly elide the boxing.
            // We can generate
            //
            // { 
            //   ResourceType resource = expr; 
            //   try { statement; } 
            //   finally { if (resource.HasValue) resource.GetValueOrDefault().Dispose(); }
            // }
            //
            // Where again we do a constrained virtual call to Dispose, rather than boxing
            // the value to IDisposable.
            //
            // Note that this optimization is *not* what the native compiler does; in this case
            // the native compiler behavior is to test for HasValue, then *box* and convert
            // the boxed value to IDisposable. There's no need to do that.
            //
            // Third: if we have "using(x)" and x is dynamic then obviously we need not generate
            // "{ dynamic temp1 = x; IDisposable temp2 = (IDisposable) temp1; ... }". Rather, we elide
            // the completely unnecessary first temporary. 

            Debug.Assert((awaitKeywordOpt == default) == (awaitOpt == default(AwaitableInfo)));
            BoundExpression disposedExpression;
            bool isNullableValueType = local.Type.IsNullableType();

            if (isNullableValueType)
            {
                MethodSymbol getValueOrDefault = UnsafeGetNullableMethod(syntax, local.Type, SpecialMember.System_Nullable_T_GetValueOrDefault);
                // local.GetValueOrDefault()
                disposedExpression = BoundCall.Synthesized(syntax, local, getValueOrDefault);
            }
            else
            {
                // local
                disposedExpression = local;
            }

            BoundExpression disposeCall = GenerateDisposeCall(syntax, disposedExpression, methodOpt, awaitOpt, awaitKeywordOpt);

            // local.Dispose(); or await variant
            BoundStatement disposeStatement = new BoundExpressionStatement(syntax, disposeCall);

            BoundExpression ifCondition;

            if (isNullableValueType)
            {
                // local.HasValue
                ifCondition = MakeNullableHasValue(syntax, local);
            }
            else if (local.Type.IsValueType)
            {
                ifCondition = null;
            }
            else
            {
                // local != null
                ifCondition = MakeNullCheck(syntax, local, BinaryOperatorKind.NotEqual);
            }

            BoundStatement finallyStatement;

            if (ifCondition == null)
            {
                // local.Dispose(); or await variant
                finallyStatement = disposeStatement;
            }
            else
            {
                // if (local != null) local.Dispose();
                // or
                // if (local.HasValue) local.GetValueOrDefault().Dispose();
                // or
                // await variants
                finallyStatement = RewriteIfStatement(
                    syntax: syntax,
                    rewrittenCondition: ifCondition,
                    rewrittenConsequence: disposeStatement,
                    rewrittenAlternativeOpt: null,
                    hasErrors: false);
            }

            // try { ... } finally { if (local != null) local.Dispose(); }
            // or
            // nullable or await variants
            BoundStatement tryFinally = new BoundTryStatement(
                syntax: syntax,
                tryBlock: tryBlock,
                catchBlocks: ImmutableArray<BoundCatchBlock>.Empty,
                finallyBlockOpt: BoundBlock.SynthesizedNoLocals(syntax, finallyStatement));

            return tryFinally;
        }

        private BoundExpression GenerateDisposeCall(SyntaxNode syntax, BoundExpression disposedExpression, MethodSymbol methodOpt, AwaitableInfo awaitOpt, SyntaxToken awaitKeyword)
        {
            Debug.Assert(awaitOpt is null || awaitKeyword != default);

            // If we don't have an explicit dispose method, try and get the special member for IDiposable/IAsyncDisposable
            if (methodOpt is null)
            {
                if (awaitOpt is null)
                {
                    // IDisposable.Dispose()
                    Binder.TryGetSpecialTypeMember(_compilation, SpecialMember.System_IDisposable__Dispose, syntax, _diagnostics, out methodOpt);
                }
                else
                {
                    // IAsyncDisposable.DisposeAsync()
                    TryGetWellKnownTypeMember(syntax: null, WellKnownMember.System_IAsyncDisposable__DisposeAsync, out methodOpt, location: awaitKeyword.GetLocation());
                }
            }

            BoundExpression disposeCall;
            if (methodOpt is null)
            {
                disposeCall = new BoundBadExpression(syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create(disposedExpression), ErrorTypeSymbol.UnknownResultType);
            }
            else
            {
                disposeCall = MakeCallWithNoExplicitArgument(syntax, disposedExpression, methodOpt);

                if (awaitOpt is object)
                {
                    // await local.DisposeAsync()
                    _sawAwaitInExceptionHandler = true;

                    TypeSymbol awaitExpressionType = awaitOpt.GetResult?.ReturnType ?? _compilation.DynamicType;
                    disposeCall = RewriteAwaitExpression(syntax, disposeCall, awaitOpt, awaitExpressionType, false);
                }
            }

            return disposeCall;
        }

        /// <summary>
        /// Synthesize a call `expression.Method()`, but with some extra smarts to handle extension methods, and to fill-in optional and params parameters.
        /// </summary>
        private BoundExpression MakeCallWithNoExplicitArgument(SyntaxNode syntax, BoundExpression expression, MethodSymbol method)
        {
            var receiver = method.IsExtensionMethod ? null : expression;

            var args = method.IsExtensionMethod
                ? ImmutableArray.Create(expression)
                : ImmutableArray<BoundExpression>.Empty;

            var refKinds = method.IsExtensionMethod && !method.ParameterRefKinds.IsDefaultOrEmpty
                ? ImmutableArray.Create(method.ParameterRefKinds[0])
                : default;

            BoundExpression disposeCall = MakeCall(syntax: syntax,
                rewrittenReceiver: receiver,
                method: method,
                rewrittenArguments: args,
                argumentRefKindsOpt: refKinds,
                expanded: method.HasParamsParameter(),
                invokedAsExtensionMethod: method.IsExtensionMethod,
                argsToParamsOpt: default,
                resultKind: LookupResultKind.Viable,
                type: method.ReturnType);

            return disposeCall;
        }
    }
}
