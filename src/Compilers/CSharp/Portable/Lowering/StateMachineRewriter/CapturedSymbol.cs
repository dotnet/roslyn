// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CapturedSymbolReplacement
    {
        public readonly bool IsReusable;

        public CapturedSymbolReplacement(bool isReusable)
        {
            this.IsReusable = isReusable;
        }

        /// <summary>
        /// Rewrite the replacement expression for the hoisted local so all synthesized field are accessed as members
        /// of the appropriate frame.
        /// </summary>
        public abstract BoundExpression Replacement<TArg>(SyntaxNode node, Func<NamedTypeSymbol, TArg, BoundExpression> makeFrame, TArg arg);
    }

    internal sealed class CapturedToFrameSymbolReplacement : CapturedSymbolReplacement
    {
        public readonly LambdaCapturedVariable HoistedField;

        public CapturedToFrameSymbolReplacement(LambdaCapturedVariable hoistedField, bool isReusable)
            : base(isReusable)
        {
            this.HoistedField = hoistedField;
        }

        public override BoundExpression Replacement<TArg>(SyntaxNode node, Func<NamedTypeSymbol, TArg, BoundExpression> makeFrame, TArg arg)
        {
            var frame = makeFrame(this.HoistedField.ContainingType, arg);
            var field = this.HoistedField.AsMember((NamedTypeSymbol)frame.Type);
            return new BoundFieldAccess(node, frame, field, constantValueOpt: null);
        }
    }

    internal sealed class CapturedToStateMachineFieldReplacement : CapturedSymbolReplacement
    {
        public readonly StateMachineFieldSymbol HoistedField;

        public CapturedToStateMachineFieldReplacement(StateMachineFieldSymbol hoistedField, bool isReusable)
            : base(isReusable)
        {
            this.HoistedField = hoistedField;
        }

        public override BoundExpression Replacement<TArg>(SyntaxNode node, Func<NamedTypeSymbol, TArg, BoundExpression> makeFrame, TArg arg)
        {
            var frame = makeFrame(this.HoistedField.ContainingType, arg);
            var field = this.HoistedField.AsMember((NamedTypeSymbol)frame.Type);
            return new BoundFieldAccess(node, frame, field, constantValueOpt: null);
        }
    }

    internal sealed class CapturedToExpressionSymbolReplacement : CapturedSymbolReplacement
    {
        private readonly BoundExpression _replacement;
        public readonly ImmutableArray<StateMachineFieldSymbol> HoistedFields;

        public CapturedToExpressionSymbolReplacement(BoundExpression replacement, ImmutableArray<StateMachineFieldSymbol> hoistedFields, bool isReusable)
            : base(isReusable)
        {
            _replacement = replacement;
            this.HoistedFields = hoistedFields;
        }

        public override BoundExpression Replacement<TArg>(SyntaxNode node, Func<NamedTypeSymbol, TArg, BoundExpression> makeFrame, TArg arg)
        {
            // By returning the same replacement each time, it is possible we
            // are constructing a DAG instead of a tree for the translation.
            // Because the bound trees are immutable that is usually harmless.
            return _replacement;
        }
    }
}
