// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CapturedSymbolReplacement
    {
        public delegate BoundExpression FramePointerMaker(NamedTypeSymbol frameType);

        internal virtual SynthesizedFieldSymbolBase HoistedField
        {
            get { return null; }
        }

        /// <summary>
        /// Rewrite the replacement expression for the hoisted local so all synthesized field are accessed as members
        /// of the appropriate frame.
        /// </summary>
        internal abstract BoundExpression Replacement(CSharpSyntaxNode node, FramePointerMaker makeFrame);
    }

    internal class CapturedToFrameSymbolReplacement : CapturedSymbolReplacement
    {
        private readonly SynthesizedFieldSymbolBase field;

        internal override SynthesizedFieldSymbolBase HoistedField
        {
            get { return field; }
        }

        internal override BoundExpression Replacement(CSharpSyntaxNode node, FramePointerMaker makeFrame)
        {
            var frame = makeFrame(this.field.ContainingType);
            var field = this.field.AsMember((NamedTypeSymbol)frame.Type);
            return new BoundFieldAccess(node, frame, field, default(ConstantValue));
        }

        public CapturedToFrameSymbolReplacement(SynthesizedFieldSymbolBase field)
        {
            this.field = field;
        }
    }

    internal class CapturedToExpressionSymbolReplacement : CapturedSymbolReplacement
    {
        private readonly BoundExpression replacement;

        internal CapturedToExpressionSymbolReplacement(BoundExpression replacement)
        {
            this.replacement = replacement;
        }

        internal override BoundExpression Replacement(CSharpSyntaxNode node, FramePointerMaker makeFrame)
        {
            // By returning the same replacement each time, it is possible we
            // are constructing a DAG instead of a tree for the translation.
            // Because the bound trees are immutable that is usually harmless.
            return replacement;
        }
    }
}
