// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// 
        /// C# 4.0 version
        /// 
        /// L locked;
        /// bool flag = false;
        /// try {
        ///     locked = `argument`;
        ///     Monitor.Enter(locked, ref flag);
        ///     `body`
        /// } finally {
        ///     if (flag) Monitor.Exit(locked);
        /// }
        ///
        /// Pre-4.0 version
        /// 
        /// L locked = `argument`;
        /// Monitor.Enter(locked, ref flag);
        /// try {
        ///     `body`
        /// } finally {
        ///     Monitor.Exit(locked);
        /// }
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
                argumentType = this.compilation.GetSpecialType(SpecialType.System_Object);
                rewrittenArgument = MakeLiteral(
                    rewrittenArgument.Syntax,
                    rewrittenArgument.ConstantValue,
                    argumentType); //need to have a non-null type here for TempHelpers.StoreToTemp.
            }
            if (argumentType.Kind == SymbolKind.TypeParameter)
            {
                // If the argument has a type parameter type, then we'll box it right away
                // so that the same object is passed to both Monitor.Enter and Monitor.Exit.
                argumentType = this.compilation.GetSpecialType(SpecialType.System_Object);

                rewrittenArgument = MakeConversion(
                    rewrittenArgument.Syntax,
                    rewrittenArgument,
                    ConversionKind.Boxing,
                    argumentType,
                    @checked: false,
                    constantValueOpt: rewrittenArgument.ConstantValue);
            }

            BoundAssignmentOperator assignmentToLockTemp;
            BoundLocal boundLockTemp = this.factory.StoreToTemp(rewrittenArgument, out assignmentToLockTemp, kind: SynthesizedLocalKind.Lock);

            BoundStatement boundLockTempInit = new BoundExpressionStatement(lockSyntax, assignmentToLockTemp);
            if (this.generateDebugInfo)
            {
                boundLockTempInit = new BoundSequencePointWithSpan( // NOTE: the lock temp is uninitialized at this sequence point.
                    lockSyntax,
                    boundLockTempInit,
                    TextSpan.FromBounds(lockSyntax.LockKeyword.SpanStart, lockSyntax.CloseParenToken.Span.End));
            }

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

            BoundStatement exitCall = new BoundExpressionStatement(
                lockSyntax,
                exitCallExpr);

            MethodSymbol enterMethod;

            if ((TryGetWellKnownTypeMember(lockSyntax, WellKnownMember.System_Threading_Monitor__Enter2, out enterMethod, isOptional: true) ||
                 TryGetWellKnownTypeMember(lockSyntax, WellKnownMember.System_Threading_Monitor__Enter, out enterMethod)) && // If we didn't find the overload introduced in .NET 4.0, then use the older one. 
                enterMethod.ParameterCount == 2)
            {
                // C# 4.0 version
                // L locked;
                // bool flag = false;
                // try {
                //     locked = `argument`;
                //     Monitor.Enter(locked, ref flag);
                //     `body`
                // } finally {
                //     if (flag) Monitor.Exit(locked);
                // }

                TypeSymbol boolType = this.compilation.GetSpecialType(SpecialType.System_Boolean);
                BoundAssignmentOperator assignmentToTemp;
                BoundLocal boundFlagTemp = this.factory.StoreToTemp(
                    MakeLiteral(rewrittenArgument.Syntax, ConstantValue.False, boolType),
                    kind: SynthesizedLocalKind.LockTaken,
                    store: out assignmentToTemp);

                BoundStatement boundFlagTempInit = new BoundExpressionStatement(lockSyntax, assignmentToTemp);
                if (this.generateDebugInfo)
                {
                    // hide the preamble code, we should not stop until we get to " locked = `argument`; "
                    boundFlagTempInit = new BoundSequencePoint(null, boundFlagTempInit);
                }

                BoundStatement enterCall = new BoundExpressionStatement(
                    lockSyntax,
                    BoundCall.Synthesized(
                        lockSyntax,
                        null,
                        enterMethod,
                        boundLockTemp,
                        boundFlagTemp));

                exitCall = RewriteIfStatement(
                    lockSyntax,
                    ImmutableArray<LocalSymbol>.Empty,
                    boundFlagTemp,
                    exitCall,
                    null,
                    node.HasErrors);

                return new BoundBlock(
                    lockSyntax,
                    node.Locals.Concat(ImmutableArray.Create<LocalSymbol>(boundLockTemp.LocalSymbol, boundFlagTemp.LocalSymbol)),
                    ImmutableArray.Create<BoundStatement>(
                        boundFlagTempInit,
                        new BoundTryStatement(
                            lockSyntax,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, boundLockTempInit, enterCall, rewrittenBody),
                            ImmutableArray<BoundCatchBlock>.Empty,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, exitCall))));
            }
            else
            {
                BoundExpression enterCallExpr;

                if ((object)enterMethod != null)
                {
                    Debug.Assert(enterMethod.ParameterCount == 1);
                    // Pre-4.0 version
                    // L locked = `argument`;
                    // Monitor.Enter(locked, ref flag); //NB: before try-finally so we don't Exit if an exception prevents us from acquiring the lock.
                    // try {
                    //     `body`
                    // } finally {
                    //     Monitor.Exit(locked);
                    // }

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
                    node.Locals.Add(boundLockTemp.LocalSymbol),
                    ImmutableArray.Create<BoundStatement>(
                        boundLockTempInit,
                        enterCall,
                        new BoundTryStatement(
                            lockSyntax,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, rewrittenBody),
                            ImmutableArray<BoundCatchBlock>.Empty,
                            BoundBlock.SynthesizedNoLocals(lockSyntax, exitCall))));
            }
        }
    }
}