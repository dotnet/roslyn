// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            ImmutableArray<BoundLocalDeclaration> localDecls = node.Declarations.LocalDeclarations;
            int numFixedLocals = localDecls.Length;

            var localBuilder = ArrayBuilder<LocalSymbol>.GetInstance(node.Locals.Length);
            localBuilder.AddRange(node.Locals);

            var statementBuilder = ArrayBuilder<BoundStatement>.GetInstance(numFixedLocals + 1 + 1); //+1 for body, +1 for hidden seq point
            var cleanup = new BoundStatement[numFixedLocals];

            for (int i = 0; i < numFixedLocals; i++)
            {
                BoundLocalDeclaration localDecl = localDecls[i];
                LocalSymbol temp;
                LocalSymbol localToClear;
                statementBuilder.Add(InitializeFixedStatementLocal(localDecl, _factory, out temp, out localToClear));

                if (!ReferenceEquals(temp, null))
                {
                    localBuilder.Add(temp);
                }

                // NOTE: Dev10 nulls out the locals in declaration order (as opposed to "popping" them in reverse order).
                cleanup[i] = _factory.Assignment(_factory.Local(localToClear), _factory.Null(localToClear.Type));
            }

            BoundStatement rewrittenBody = VisitStatement(node.Body);
            statementBuilder.Add(rewrittenBody);
            statementBuilder.Add(_factory.HiddenSequencePoint());

            Debug.Assert(statementBuilder.Count == numFixedLocals + 1 + 1);

            // In principle, the cleanup code (i.e. nulling out the pinned variables) is always
            // in a finally block.  However, we can optimize finally away (keeping the cleanup
            // code) in cases where both of the following are true:
            //   1) there are no branches out of the fixed statement; and
            //   2) the fixed statement is not in a try block (syntactic or synthesized).
            if (IsInTryBlock(node) || HasGotoOut(rewrittenBody))
            {
                return _factory.Block(
                    localBuilder.ToImmutableAndFree(),
                    new BoundTryStatement(
                        _factory.Syntax,
                        _factory.Block(statementBuilder.ToImmutableAndFree()),
                        ImmutableArray<BoundCatchBlock>.Empty,
                        _factory.Block(cleanup)));
            }
            else
            {
                statementBuilder.AddRange(cleanup);
                return _factory.Block(localBuilder.ToImmutableAndFree(), statementBuilder.ToImmutableAndFree());
            }
        }

        /// <summary>
        /// Basically, what we need to know is, if an exception occurred within the fixed statement, would
        /// additional code in the current method be executed before its stack frame was popped?
        /// </summary>
        private static bool IsInTryBlock(BoundFixedStatement boundFixed)
        {
            CSharpSyntaxNode node = boundFixed.Syntax.Parent;
            while (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.TryStatement:
                        // NOTE: if we started in the catch or finally of this try statement,
                        // we will have bypassed this node.
                        return true;
                    case SyntaxKind.UsingStatement:
                        // ACASEY: In treating using statements as try-finally's, we're following
                        // Dev11.  The practical explanation for Dev11's behavior is that using 
                        // statements have already been lowered by the time the check is performed.
                        // A more thoughtful explanation is that user code could run between the 
                        // raising of an exception and the unwinding of the stack (via Dispose())
                        // and that user code would likely appreciate the reduced memory pressure 
                        // of having the fixed local unpinned.

                        // NOTE: As in Dev11, we're not emitting a try-finally if the fixed
                        // statement is nested within a lock statement.  Practically, dev11
                        // probably lowers locks after fixed statement, and so, does not see
                        // the try-finally.  More thoughtfully, no user code will run in the
                        // finally statement, so it's not necessary.

                        // BREAK: Takes into account whether an outer fixed statement will be
                        // lowered into a try-finally block and responds accordingly.  This is
                        // unnecessary since nothing will ever be allocated in the finally
                        // block of a lowered fixed statement, so memory pressure is not an
                        // issue.  Note that only nested fixed statements where the outer (but
                        // not the inner) fixed statement has an unmatched goto, but is not
                        // contained in a try-finally, will be affected.  e.g.
                        // fixed (...) { 
                        //   fixed (...) { }
                        //   goto L1: ; 
                        // }
                        return true;
                    case SyntaxKind.ForEachStatement:
                        // We're being conservative here - there's actually only
                        // a try block if the enumerator is disposable, but we
                        // can't tell that from the syntax.  Dev11 checks in the
                        // lowered tree, so it is more precise.
                        return true;
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                        // Stop looking.
                        return false;
                    case SyntaxKind.CatchClause:
                        // If we're in the catch of a try-catch-finally, then
                        // we're still in the scope of the try-finally handler.
                        if (((TryStatementSyntax)node.Parent).Finally != null)
                        {
                            return true;
                        }
                        goto case SyntaxKind.FinallyClause;
                    case SyntaxKind.FinallyClause:
                        // Skip past the enclosing try to avoid a false positive.
                        node = node.Parent;
                        Debug.Assert(node.Kind() == SyntaxKind.TryStatement);
                        node = node.Parent;
                        break;
                    default:
                        if (node is MemberDeclarationSyntax)
                        {
                            // Stop looking.
                            return false;
                        }
                        node = node.Parent;
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// If two (or more) fixed statements are nested, then we want to avoid having the outer
        /// fixed statement re-traverse the lowered bound tree of the inner one.  We accomplish
        /// this by having each fixed statement cache a set of unmatched gotos that can be
        /// reused by any containing fixed statements.
        /// </summary>
        private Dictionary<BoundNode, HashSet<LabelSymbol>> _lazyUnmatchedLabelCache;

        /// <summary>
        /// Look for gotos without corresponding labels in the lowered body of a fixed statement.
        /// </summary>
        /// <remarks>
        /// Assumes continue, break, etc have already been rewritten to gotos.
        /// </remarks>
        private bool HasGotoOut(BoundNode node)
        {
            if (_lazyUnmatchedLabelCache == null)
            {
                _lazyUnmatchedLabelCache = new Dictionary<BoundNode, HashSet<LabelSymbol>>();
            }

            HashSet<LabelSymbol> unmatched = UnmatchedGotoFinder.Find(node, _lazyUnmatchedLabelCache, RecursionDepth);

            _lazyUnmatchedLabelCache.Add(node, unmatched);

            return unmatched != null && unmatched.Count > 0;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            throw ExceptionUtilities.Unreachable; //Should be handled by VisitFixedStatement
        }

        private BoundStatement InitializeFixedStatementLocal(
            BoundLocalDeclaration localDecl,
            SyntheticBoundNodeFactory factory,
            out LocalSymbol temp,
            out LocalSymbol localToClear)
        {
            BoundExpression initializer = localDecl.InitializerOpt;
            Debug.Assert(!ReferenceEquals(initializer, null));

            LocalSymbol localSymbol = localDecl.LocalSymbol;

            if (initializer.Kind == BoundKind.FixedLocalCollectionInitializer)
            {
                var fixedInitializer = (BoundFixedLocalCollectionInitializer)initializer;

                if (fixedInitializer.Expression.Type.SpecialType == SpecialType.System_String)
                {
                    return InitializeFixedStatementStringLocal(localDecl, localSymbol, fixedInitializer, factory, out temp, out localToClear);
                }
                else
                {
                    Debug.Assert(fixedInitializer.Expression.Type.IsArray());

                    localToClear = localSymbol;
                    return InitializeFixedStatementArrayLocal(localDecl, localSymbol, fixedInitializer, factory, out temp);
                }
            }
            else
            {
                temp = null;
                localToClear = localSymbol;
                return RewriteLocalDeclaration(localDecl, localDecl.Syntax, localSymbol, VisitExpression(initializer));
            }
        }

        private BoundStatement InitializeFixedStatementStringLocal(
            BoundLocalDeclaration localDecl,
            LocalSymbol localSymbol,
            BoundFixedLocalCollectionInitializer fixedInitializer,
            SyntheticBoundNodeFactory factory,
            out LocalSymbol stringTemp,
            out LocalSymbol localToClear)
        {
            TypeSymbol localType = localSymbol.Type;
            BoundExpression initializerExpr = VisitExpression(fixedInitializer.Expression);
            TypeSymbol initializerType = initializerExpr.Type;

            // intervening parens may have been skipped by the binder; find the declarator
            VariableDeclaratorSyntax declarator = fixedInitializer.Syntax.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            Debug.Assert(declarator != null);

            stringTemp = factory.SynthesizedLocal(initializerType, syntax: declarator, isPinned: true, kind: SynthesizedLocalKind.FixedString);

            // NOTE: we pin the string, not the pointer.
            Debug.Assert(stringTemp.IsPinned);
            Debug.Assert(!localSymbol.IsPinned);

            BoundStatement stringTempInit = factory.Assignment(factory.Local(stringTemp), initializerExpr);

            var convertedStringTemp = factory.Convert(
                localType,
                factory.Local(stringTemp),
                fixedInitializer.ElementPointerTypeConversion);

            BoundStatement localInit = InstrumentLocalDeclarationIfNecessary(localDecl, localSymbol,
                factory.Assignment(factory.Local(localSymbol), convertedStringTemp));

            BoundExpression notNullCheck = MakeNullCheck(factory.Syntax, factory.Local(localSymbol), BinaryOperatorKind.NotEqual);
            BoundExpression helperCall;

            MethodSymbol offsetMethod;
            if (TryGetWellKnownTypeMember(fixedInitializer.Syntax, WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__get_OffsetToStringData, out offsetMethod))
            {
                helperCall = factory.Call(receiver: null, method: offsetMethod);
            }
            else
            {
                helperCall = new BoundBadExpression(fixedInitializer.Syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray<BoundNode>.Empty, ErrorTypeSymbol.UnknownResultType);
            }

            BoundExpression addition = factory.Binary(BinaryOperatorKind.PointerAndIntAddition, localType, factory.Local(localSymbol), helperCall);
            BoundStatement conditionalAdd = factory.If(notNullCheck, factory.Assignment(factory.Local(localSymbol), addition));

            localToClear = stringTemp;
            return factory.Block(stringTempInit, localInit, conditionalAdd);
        }

        private BoundStatement InitializeFixedStatementArrayLocal(
            BoundLocalDeclaration localDecl,
            LocalSymbol localSymbol,
            BoundFixedLocalCollectionInitializer fixedInitializer,
            SyntheticBoundNodeFactory factory,
            out LocalSymbol arrayTemp)
        {
            // From ExpressionBinder::BindPtrToArray:
            // (((temp = array) != null && temp.Length > 0) ? loc = &temp[0] : loc = null)
            // NOTE: The assignment needs to be inside the EK_QUESTIONMARK. See Whidbey bug #397859.
            // We can't do loc = (... ? ... : ...) since the CLR type of &temp[0] is a managed
            // pointer and null is a UIntPtr - which confuses the JIT. We can't just convert
            // &temp[0] to UIntPtr with a conv.u instruction because then if a GC occurs between
            // the time of the cast and the assignment to the local, we're toast.

            TypeSymbol localType = localSymbol.Type;
            BoundExpression initializerExpr = VisitExpression(fixedInitializer.Expression);
            TypeSymbol initializerType = initializerExpr.Type;

            arrayTemp = factory.SynthesizedLocal(initializerType);
            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)arrayTemp.Type;
            TypeSymbol arrayElementType = arrayType.ElementType;

            // NOTE: we pin the pointer, not the array.
            Debug.Assert(!arrayTemp.IsPinned);
            Debug.Assert(localSymbol.IsPinned);

            //(temp = array)
            BoundExpression arrayTempInit = factory.AssignmentExpression(factory.Local(arrayTemp), initializerExpr);

            //(temp = array) != null
            BoundExpression notNullCheck = MakeNullCheck(factory.Syntax, arrayTempInit, BinaryOperatorKind.NotEqual);

            BoundExpression lengthCall;

            if (arrayType.IsSZArray)
            {
                lengthCall = factory.ArrayLength(factory.Local(arrayTemp));
            }
            else
            {
                MethodSymbol lengthMethod;
                if (TryGetWellKnownTypeMember(fixedInitializer.Syntax, WellKnownMember.System_Array__get_Length, out lengthMethod))
                {
                    lengthCall = factory.Call(factory.Local(arrayTemp), lengthMethod);
                }
                else
                {
                    lengthCall = new BoundBadExpression(fixedInitializer.Syntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(factory.Local(arrayTemp)), ErrorTypeSymbol.UnknownResultType);
                }
            }

            // NOTE: dev10 comment says ">", but code actually checks "!="
            //temp.Length != 0
            BoundExpression lengthCheck = factory.Binary(BinaryOperatorKind.IntNotEqual, factory.SpecialType(SpecialType.System_Boolean), lengthCall, factory.Literal(0));

            //((temp = array) != null && temp.Length != 0)
            BoundExpression condition = factory.Binary(BinaryOperatorKind.LogicalBoolAnd, factory.SpecialType(SpecialType.System_Boolean), notNullCheck, lengthCheck);

            //temp[0]
            BoundExpression firstElement = factory.ArrayAccessFirstElement(factory.Local(arrayTemp));

            // NOTE: this is a fixed statement address-of in that it's the initial value of pinned local.
            //&temp[0]
            BoundExpression firstElementAddress = new BoundAddressOfOperator(factory.Syntax, firstElement, isFixedStatementAddressOf: true, type: new PointerTypeSymbol(arrayElementType));
            BoundExpression convertedFirstElementAddress = factory.Convert(
                localType,
                firstElementAddress,
                fixedInitializer.ElementPointerTypeConversion);

            //loc = &temp[0]
            BoundExpression consequenceAssignment = factory.AssignmentExpression(factory.Local(localSymbol), convertedFirstElementAddress);

            //loc = null
            BoundExpression alternativeAssignment = factory.AssignmentExpression(factory.Local(localSymbol), factory.Null(localType));

            //(((temp = array) != null && temp.Length != 0) ? loc = &temp[0] : loc = null)
            BoundStatement localInit = factory.ExpressionStatement(
                new BoundConditionalOperator(factory.Syntax, condition, consequenceAssignment, alternativeAssignment, ConstantValue.NotAvailable, localType));

            return InstrumentLocalDeclarationIfNecessary(localDecl, localSymbol, localInit);
        }
    }
}
