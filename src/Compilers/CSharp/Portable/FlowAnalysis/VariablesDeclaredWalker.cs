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

        internal VariablesDeclaredWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        protected override void Free()
        {
            base.Free();
            _variablesDeclared = null;
        }

        public override void VisitPattern(BoundPattern pattern)
        {
            base.VisitPattern(pattern);
            NoteDeclaredPatternVariables(pattern);
        }

        protected override void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            foreach (var label in node.SwitchLabels)
            {
                NoteDeclaredPatternVariables(label.Pattern);
            }

            base.VisitSwitchSection(node, isLastSection);
        }

        /// <summary>
        /// Record declared variables in the pattern.
        /// </summary>
        private void NoteDeclaredPatternVariables(BoundPattern pattern)
        {
            if (IsInside)
            {
                switch (pattern)
                {
                    case BoundDeclarationPattern decl:
                        {
                            // The variable may be null if it is a discard designation `_`.
                            if (decl.Variable?.Kind == SymbolKind.Local)
                            {
                                // Because this API only returns local symbols and parameters,
                                // we exclude pattern variables that have become fields in scripts.
                                _variablesDeclared.Add(decl.Variable);
                            }
                        }
                        break;
                    case BoundRecursivePattern recur:
                        {
                            if (recur.Variable?.Kind == SymbolKind.Local)
                            {
                                _variablesDeclared.Add(recur.Variable);
                            }
                        }
                        break;
                }
            }
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

        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            if (IsInside)
            {
                var deconstructionAssignment = node.DeconstructionOpt?.DeconstructionAssignment;

                if (deconstructionAssignment == null)
                {
                    _variablesDeclared.AddAll(node.IterationVariables);
                }
                else
                {
                    // Deconstruction foreach declares multiple variables.
                    ((BoundTupleExpression)deconstructionAssignment.Left).VisitAllElements((x, self) => self.Visit(x), this);
                }
            }
        }

        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            if (IsInside)
            {
                var local = catchBlock.Locals.FirstOrDefault();

                if (local?.DeclarationKind == LocalDeclarationKind.CatchVariable)
                {
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

        protected override void VisitLvalue(BoundLocal node)
        {
            VisitLocal(node);
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            if (IsInside && node.DeclarationKind != BoundLocalDeclarationKind.None)
            {
                _variablesDeclared.Add(node.LocalSymbol);
            }

            return null;
        }
    }
}
