// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CapturedSymbolReplacement
    {
        /// <summary>
        /// Rewrite the replacement expression for the hoisted local so all synthesized field are accessed as members
        /// of the appropriate frame.
        /// </summary>
        public abstract BoundExpression Replacement(CSharpSyntaxNode node, Func<NamedTypeSymbol, BoundExpression> makeFrame);

        public abstract bool IsReusable { get; }
    }

    internal sealed class CapturedToFrameSymbolReplacement : CapturedSymbolReplacement
    {
        public readonly SynthesizedFieldSymbolBase HoistedField;
        public readonly bool isReusable;

        public CapturedToFrameSymbolReplacement(SynthesizedFieldSymbolBase hoistedField, bool isReusable)
        {
            this.HoistedField = hoistedField;
            this.isReusable = isReusable;
        }

        public override bool IsReusable
        {
            get { return isReusable; }
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
        {
            this.replacement = replacement;
            this.HoistedFields = hoistedFields;
        }

        public override bool IsReusable
        {
            get { return true; }
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
