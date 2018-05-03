// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIndexExpression(BoundIndexExpression node)
        {
            MethodSymbol indexCtor = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Index__ctor);

            NamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            BoundExpression fromEnd = MakeLiteral(node.Syntax, ConstantValue.Create(true), booleanType);

            BoundExpression operand = VisitExpression(node.Operand);
            operand = NullableAlwaysHasValue(operand) ?? operand;

            if (!node.Type.IsNullableType())
            {
                return new BoundObjectCreationExpression(node.Syntax, indexCtor, binderOpt: null, operand, fromEnd);
            }

            ArrayBuilder<BoundExpression> sideeffects = ArrayBuilder<BoundExpression>.GetInstance();
            ArrayBuilder<LocalSymbol> locals = ArrayBuilder<LocalSymbol>.GetInstance();

            // operand.HasValue
            operand = CaptureExpressionInTempIfNeeded(operand, sideeffects, locals);
            BoundExpression condition = MakeOptimizedHasValue(operand.Syntax, operand);

            // new Index(operand, fromEnd: true)
            BoundExpression boundOperandGetValueOrDefault = MakeOptimizedGetValueOrDefault(operand.Syntax, operand);
            BoundExpression indexCreation = new BoundObjectCreationExpression(node.Syntax, indexCtor, binderOpt: null, boundOperandGetValueOrDefault, fromEnd);

            if (!TryGetNullableMethod(node.Syntax, node.Type, SpecialMember.System_Nullable_T__ctor, out MethodSymbol nullableCtor))
            {
                // PROTOTYPE: make sure this ctor exists in binding
                return BadExpression(node.Syntax, node.Type, operand);
            }

            // new Nullable(new Index(operand, fromEnd: true))
            BoundExpression consequence = new BoundObjectCreationExpression(node.Syntax, nullableCtor, binderOpt: null, indexCreation);

            // default
            BoundExpression alternative = new BoundDefaultExpression(node.Syntax, constantValueOpt: null, node.Type);

            // operand.HasValue ? new Nullable(new Index(operand, fromEnd: true)) : default
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: node.Syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: node.Type,
                isRef: false);

            // PROTOTYPE: comment from AlekseyTS: Because of this, lifted range operators should probably be disallowed in an expression tree context.
            return new BoundSequence(
                syntax: node.Syntax,
                locals: locals.ToImmutableAndFree(),
                sideEffects: sideeffects.ToImmutableAndFree(),
                value: conditionalExpression,
                type: node.Type);
        }
    }
}
