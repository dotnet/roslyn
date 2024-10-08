// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// Lowers a lock statement to a try-finally block that calls (before and after the body, respectively):
        /// <list type="bullet">
        /// <item>Lock.EnterScope and Lock+Scope.Dispose if the argument is of type Lock, or</item>
        /// <item>Monitor.Enter and Monitor.Exit.</item>
        /// </list>
        /// </summary>
        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            LockStatementSyntax lockSyntax = (LockStatementSyntax)node.Syntax;

            BoundExpression rewrittenArgument = VisitExpression(node.Argument);
            BoundStatement? rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            TypeSymbol? argumentType = rewrittenArgument.Type;
            if (argumentType is null)
            {
                // This isn't particularly elegant, but hopefully locking on null is
                // not very common.
                Debug.Assert(rewrittenArgument.ConstantValueOpt == ConstantValue.Null);
                argumentType = _compilation.GetSpecialType(SpecialType.System_Object);
                rewrittenArgument = MakeLiteral(
                    rewrittenArgument.Syntax,
                    rewrittenArgument.ConstantValueOpt,
                    argumentType); //need to have a non-null type here for TempHelpers.StoreToTemp.
            }

            if (argumentType.IsWellKnownTypeLock())
            {
                if (LockBinder.TryFindLockTypeInfo(argumentType, _diagnostics, rewrittenArgument.Syntax) is not { } lockTypeInfo)
                {
                    Debug.Fail("We should have reported an error during binding if lock type info cannot be found.");
                    return node.Update(rewrittenArgument, rewrittenBody).WithHasErrors();
                }

                // lock (x) { body } -> using (x.EnterScope()) { body }

                var tryBlock = rewrittenBody is BoundBlock block ? block : BoundBlock.SynthesizedNoLocals(lockSyntax, rewrittenBody);

                var enterScopeCall = BoundCall.Synthesized(
                    rewrittenArgument.Syntax,
                    rewrittenArgument,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    lockTypeInfo.EnterScopeMethod);

                // the temp must be associated with the lock statement to support EnC slot mapping:
                BoundLocal boundTemp = _factory.StoreToTemp(enterScopeCall,
                    out BoundAssignmentOperator tempAssignment,
                    syntaxOpt: lockSyntax,
                    kind: SynthesizedLocalKind.Using);

                var expressionStatement = new BoundExpressionStatement(rewrittenArgument.Syntax, tempAssignment);

                BoundStatement tryFinally = RewriteUsingStatementTryFinally(
                    rewrittenArgument.Syntax,
                    rewrittenArgument.Syntax,
                    tryBlock,
                    boundTemp,
                    awaitKeywordOpt: default,
                    awaitOpt: null,
                    patternDisposeInfo: MethodArgumentInfo.CreateParameterlessMethod(lockTypeInfo.ScopeDisposeMethod));

                return new BoundBlock(
                    lockSyntax,
                    locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                    statements: ImmutableArray.Create(expressionStatement, tryFinally));
            }

            if (argumentType.Kind == SymbolKind.TypeParameter)
            {
                // If the argument has a type parameter type, then we'll box it right away
                // so that the same object is passed to both Monitor.Enter and Monitor.Exit.
                argumentType = _compilation.GetSpecialType(SpecialType.System_Object);

                rewrittenArgument = MakeConversionNode(
                    rewrittenArgument.Syntax,
                    rewrittenArgument,
                    Conversion.Boxing,
                    argumentType,
                    @checked: false,
                    constantValueOpt: rewrittenArgument.ConstantValueOpt);
            }

            BoundAssignmentOperator assignmentToLockTemp;
            BoundLocal boundLockTemp = _factory.StoreToTemp(rewrittenArgument, out assignmentToLockTemp, syntaxOpt: lockSyntax, kind: SynthesizedLocalKind.Lock);

            BoundStatement boundLockTempInit = new BoundExpressionStatement(lockSyntax, assignmentToLockTemp);
            BoundExpression exitCallExpr;

            MethodSymbol? exitMethod;
            if (TryGetWellKnownTypeMember(lockSyntax, WellKnownMember.System_Threading_Monitor__Exit, out exitMethod))
            {
                exitCallExpr = BoundCall.Synthesized(
                    lockSyntax,
                    receiverOpt: null,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    exitMethod,
                    boundLockTemp);
            }
            else
            {
                exitCallExpr = new BoundBadExpression(lockSyntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create<BoundExpression>(boundLockTemp), ErrorTypeSymbol.UnknownResultType);
            }

            BoundStatement exitCall = new BoundExpressionStatement(lockSyntax, exitCallExpr);

            MethodSymbol? enterMethod;

            if ((TryGetWellKnownTypeMember(lockSyntax, WellKnownMember.System_Threading_Monitor__Enter2, out enterMethod, isOptional: true) ||
                 TryGetWellKnownTypeMember(lockSyntax, WellKnownMember.System_Threading_Monitor__Enter, out enterMethod)) && // If we didn't find the overload introduced in .NET 4.0, then use the older one. 
                enterMethod.ParameterCount == 2)
            {
                // C# 4.0+ version
                // L $lock = `argument`;                      // sequence point
                // bool $lockTaken = false;                   
                // try
                // {
                //     Monitor.Enter($lock, ref $lockTaken);
                //     `body`                                 // sequence point  
                // }
                // finally
                // {                                          // hidden sequence point   
                //     if ($lockTaken) Monitor.Exit($lock);   
                // }

                TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                BoundAssignmentOperator assignmentToLockTakenTemp;

                BoundLocal boundLockTakenTemp = _factory.StoreToTemp(
                    MakeLiteral(rewrittenArgument.Syntax, ConstantValue.False, boolType),
                    store: out assignmentToLockTakenTemp,
                    syntaxOpt: lockSyntax,
                    kind: SynthesizedLocalKind.LockTaken);

                BoundStatement boundLockTakenTempInit = new BoundExpressionStatement(lockSyntax, assignmentToLockTakenTemp);

                BoundStatement enterCall = new BoundExpressionStatement(
                    lockSyntax,
                    BoundCall.Synthesized(
                        lockSyntax,
                        receiverOpt: null,
                        initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                        enterMethod,
                        boundLockTemp,
                        boundLockTakenTemp));

                exitCall = RewriteIfStatement(
                    lockSyntax,
                    boundLockTakenTemp,
                    exitCall,
                    node.HasErrors);

                return new BoundBlock(
                    lockSyntax,
                    ImmutableArray.Create(boundLockTemp.LocalSymbol, boundLockTakenTemp.LocalSymbol),
                    ImmutableArray.Create(
                        InstrumentLockTargetCapture(node, boundLockTempInit),
                        boundLockTakenTempInit,
                        new BoundTryStatement(
                            lockSyntax,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, ImmutableArray.Create<BoundStatement>(
                                enterCall,
                                rewrittenBody)),
                            ImmutableArray<BoundCatchBlock>.Empty,
                            BoundBlock.SynthesizedNoLocals(lockSyntax,
                                exitCall))));
            }
            else
            {
                // Pre-4.0 version
                // L $lock = `argument`;           // sequence point
                // Monitor.Enter($lock);           // NB: before try-finally so we don't Exit if an exception prevents us from acquiring the lock.
                // try 
                // {
                //     `body`                      // sequence point
                // } 
                // finally 
                // {
                //     Monitor.Exit($lock);        // hidden sequence point
                // }

                BoundExpression enterCallExpr;

                if ((object?)enterMethod != null)
                {
                    Debug.Assert(enterMethod.ParameterCount == 1);

                    enterCallExpr = BoundCall.Synthesized(
                        lockSyntax,
                        receiverOpt: null,
                        initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                        enterMethod,
                        boundLockTemp);
                }
                else
                {
                    enterCallExpr = new BoundBadExpression(lockSyntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol?>.Empty, ImmutableArray.Create<BoundExpression>(boundLockTemp), ErrorTypeSymbol.UnknownResultType);
                }

                BoundStatement enterCall = new BoundExpressionStatement(
                    lockSyntax,
                    enterCallExpr);

                return new BoundBlock(
                    lockSyntax,
                    ImmutableArray.Create(boundLockTemp.LocalSymbol),
                    ImmutableArray.Create(
                        InstrumentLockTargetCapture(node, boundLockTempInit),
                        enterCall,
                        new BoundTryStatement(
                            lockSyntax,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, rewrittenBody),
                            ImmutableArray<BoundCatchBlock>.Empty,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, exitCall))));
            }
        }

        private BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            return this.Instrument ?
                Instrumenter.InstrumentLockTargetCapture(original, lockTargetCapture) :
                lockTargetCapture;
        }
    }
}
