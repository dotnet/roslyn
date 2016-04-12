// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Lowers a lock statement to a try-finally block that calls Monitor.Enter and Monitor.Exit
        /// before and after the body, respectively.
        /// </summary>
        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            LockStatementSyntax lockSyntax = (LockStatementSyntax)node.Syntax;

            BoundExpression rewrittenArgument = VisitExpression(node.Argument);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            TypeSymbol argumentType = rewrittenArgument.Type;
            if ((object)argumentType == null)
            {
                // This isn't particularly elegant, but hopefully locking on null is
                // not very common.
                Debug.Assert(rewrittenArgument.ConstantValue == ConstantValue.Null);
                argumentType = _compilation.GetSpecialType(SpecialType.System_Object);
                rewrittenArgument = MakeLiteral(
                    rewrittenArgument.Syntax,
                    rewrittenArgument.ConstantValue,
                    argumentType); //need to have a non-null type here for TempHelpers.StoreToTemp.
            }

            if (argumentType.Kind == SymbolKind.TypeParameter)
            {
                // If the argument has a type parameter type, then we'll box it right away
                // so that the same object is passed to both Monitor.Enter and Monitor.Exit.
                argumentType = _compilation.GetSpecialType(SpecialType.System_Object);

                rewrittenArgument = MakeConversion(
                    rewrittenArgument.Syntax,
                    rewrittenArgument,
                    ConversionKind.Boxing,
                    argumentType,
                    @checked: false,
                    constantValueOpt: rewrittenArgument.ConstantValue);
            }

            BoundAssignmentOperator assignmentToLockTemp;
            BoundLocal boundLockTemp = _factory.StoreToTemp(rewrittenArgument, out assignmentToLockTemp, syntaxOpt: lockSyntax, kind: SynthesizedLocalKind.Lock);

            BoundStatement boundLockTempInit = new BoundExpressionStatement(lockSyntax, assignmentToLockTemp);
            BoundExpression exitCallExpr;

            MethodSymbol exitMethod;
            if (TryGetWellKnownTypeMember(lockSyntax, WellKnownMember.System_Threading_Monitor__Exit, out exitMethod))
            {
                exitCallExpr = BoundCall.Synthesized(
                    lockSyntax,
                    null,
                    exitMethod,
                    boundLockTemp);
            }
            else
            {
                exitCallExpr = new BoundBadExpression(lockSyntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(boundLockTemp), ErrorTypeSymbol.UnknownResultType);
            }

            BoundStatement exitCall = new BoundExpressionStatement(lockSyntax, exitCallExpr);

            MethodSymbol enterMethod;

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
                        null,
                        enterMethod,
                        boundLockTemp,
                        boundLockTakenTemp));

                exitCall = RewriteIfStatement(
                    lockSyntax,
                    boundLockTakenTemp,
                    exitCall,
                    null,
                    node.HasErrors);

                return new BoundBlock(
                    lockSyntax,
                    ImmutableArray.Create(boundLockTemp.LocalSymbol, boundLockTakenTemp.LocalSymbol),
                    ImmutableArray<LocalFunctionSymbol>.Empty,
                    ImmutableArray.Create(
                        InstrumentLockTargetCapture(node, boundLockTempInit),
                        boundLockTakenTempInit,
                        new BoundTryStatement(
                            lockSyntax,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, ImmutableArray.Create(
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

                if ((object)enterMethod != null)
                {
                    Debug.Assert(enterMethod.ParameterCount == 1);

                    enterCallExpr = BoundCall.Synthesized(
                        lockSyntax,
                        null,
                        enterMethod,
                        boundLockTemp);
                }
                else
                {
                    enterCallExpr = new BoundBadExpression(lockSyntax, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(boundLockTemp), ErrorTypeSymbol.UnknownResultType);
                }

                BoundStatement enterCall = new BoundExpressionStatement(
                    lockSyntax,
                    enterCallExpr);

                return new BoundBlock(
                    lockSyntax,
                    ImmutableArray.Create(boundLockTemp.LocalSymbol),
                    ImmutableArray<LocalFunctionSymbol>.Empty,
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
                _instrumenter.InstrumentLockTargetCapture(original, lockTargetCapture) :
                lockTargetCapture;
        }
    }
}
