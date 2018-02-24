// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSequence
    {
        public BoundSequence(SyntaxNode syntax, ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundExpression> sideEffects, BoundExpression value, TypeSymbol type, bool hasErrors = false)
            : this(syntax, locals, sideEffects.CastArray<BoundNode>(), value, type, hasErrors)
        {
        }

        public BoundSequence(SyntaxNode syntax, ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundStatement> sideEffects, BoundExpression value, TypeSymbol type, bool hasErrors = false)
            : this(syntax, locals, sideEffects.CastArray<BoundNode>(), value, type, hasErrors)
        {
        }

        public void Validate()
        {
            Validate(this.SideEffects);
        }

        private static void Validate(ImmutableArray<BoundNode> sideEffects)
        {
#if DEBUG
            // Ensure nested side effects are of the permitted kinds only
            foreach (var node in sideEffects)
            {
                switch (node.Kind)
                {
                    case BoundKind.ExpressionStatement:
                    case BoundKind.GotoStatement:
                    case BoundKind.ConditionalGoto:
                    case BoundKind.SwitchStatement:
                    case BoundKind.SequencePoint:
                    case BoundKind.LabelStatement:
                    case BoundKind.SwitchDispatch:
                    case BoundKind.ThrowStatement:
                        break;
                    default:
                        Debug.Assert(node is BoundExpression);
                        break;
                }
            }
#endif
        }
    }
}
