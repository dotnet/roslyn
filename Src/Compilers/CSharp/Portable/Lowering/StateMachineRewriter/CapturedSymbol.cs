// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public abstract BoundExpression Replacement(CSharpSyntaxNode node, Func<NamedTypeSymbol, BoundExpression> makeFrame);
    }

    internal sealed class CapturedToFrameSymbolReplacement : CapturedSymbolReplacement
    {
        public readonly SynthesizedFieldSymbolBase HoistedField;

        public CapturedToFrameSymbolReplacement(SynthesizedFieldSymbolBase hoistedField, bool isReusable)
            : base(isReusable)
        {
            this.HoistedField = hoistedField;
        }

        public override BoundExpression Replacement(CSharpSyntaxNode node, Func<NamedTypeSymbol, BoundExpression> makeFrame)
        {
            var frame = makeFrame(this.HoistedField.ContainingType);
            var field = this.HoistedField.AsMember((NamedTypeSymbol)frame.Type);
            return new BoundFieldAccess(node, frame, field, default(ConstantValue));
        }
    }

    internal sealed class CapturedToExpressionSymbolReplacement : CapturedSymbolReplacement
    {
        private readonly BoundExpression replacement;
        public readonly ImmutableArray<SynthesizedFieldSymbolBase> HoistedFields;

        public CapturedToExpressionSymbolReplacement(BoundExpression replacement, ImmutableArray<SynthesizedFieldSymbolBase> hoistedFields)
            : base(isReusable: false)
        {
            this.replacement = replacement;
            this.HoistedFields = hoistedFields;
        }

        public override BoundExpression Replacement(CSharpSyntaxNode node, Func<NamedTypeSymbol, BoundExpression> makeFrame)
        {
            // By returning the same replacement each time, it is possible we
            // are constructing a DAG instead of a tree for the translation.
            // Because the bound trees are immutable that is usually harmless.
            return replacement;
        }
    }
}
