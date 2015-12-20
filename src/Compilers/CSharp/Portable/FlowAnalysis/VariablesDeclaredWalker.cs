// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that records declared variables.
    /// </summary>
    internal class VariablesDeclaredWalker : AbstractRegionControlFlowPass
    {
        internal static IEnumerable<Symbol> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            var walker = new VariablesDeclaredWalker(compilation, member, node, firstInRegion, lastInRegion);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion);
                return badRegion ? SpecializedCollections.EmptyEnumerable<Symbol>() : walker._variablesDeclared;
            }
            finally
            {
                walker.Free();
            }
        }

        private HashSet<Symbol> _variablesDeclared = new HashSet<Symbol>();

        private void Analyze()
        {
            // only one pass needed.
            regionPlace = RegionPlace.Before;
            bool badRegion = false;
            SetState(ReachableState());
            Scan(ref badRegion);
            if (badRegion) _variablesDeclared.Clear();
        }

        internal VariablesDeclaredWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        protected override void Free()
        {
            base.Free();
            _variablesDeclared = null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            if (IsInside)
            {
                _variablesDeclared.Add(node.LocalSymbol);
            }

            return base.VisitLocalDeclaration(node);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            if (IsInside && !node.WasCompilerGenerated)
            {
                foreach (var parameter in node.Symbol.Parameters)
                {
                    _variablesDeclared.Add(parameter);
                }
            }

            return base.VisitLambda(node);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            if (IsInside && !node.WasCompilerGenerated)
            {
                foreach (var parameter in node.Symbol.Parameters)
                {
                    _variablesDeclared.Add(parameter);
                }
            }

            return base.VisitLocalFunctionStatement(node);
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            if (IsInside)
            {
                _variablesDeclared.Add(node.IterationVariable);
            }

            return base.VisitForEachStatement(node);
        }


        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            if (IsInside)
            {
                var local = catchBlock.LocalOpt;

                if ((object)local != null)
                {
                    Debug.Assert(local.DeclarationKind == LocalDeclarationKind.CatchVariable);
                    _variablesDeclared.Add(local);
                }
            }

            base.VisitCatchBlock(catchBlock, ref finallyState);
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            if (IsInside)
            {
                if ((object)node.DefinedSymbol != null)
                {
                    _variablesDeclared.Add(node.DefinedSymbol);
                }
            }

            return base.VisitQueryClause(node);
        }
    }
}
