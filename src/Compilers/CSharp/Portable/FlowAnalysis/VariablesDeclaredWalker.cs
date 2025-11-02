// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
            _variablesDeclared = null!;
        }

        public override void VisitPattern(BoundPattern pattern)
        {
            NoteDeclaredPatternVariables(pattern);
            base.VisitPattern(pattern);
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
            switch (pattern)
            {
                case BoundDeclarationPattern declarationPattern:
                    noteOneVariable(declarationPattern.Variable);
                    break;

                case BoundRecursivePattern recursivePattern:
                    foreach (var subpattern in recursivePattern.Deconstruction.NullToEmpty())
                        NoteDeclaredPatternVariables(subpattern.Pattern);

                    foreach (var subpattern in recursivePattern.Properties.NullToEmpty())
                        NoteDeclaredPatternVariables(subpattern.Pattern);

                    noteOneVariable(recursivePattern.Variable);
                    break;

                case BoundITuplePattern ituplePattern:
                    foreach (var subpattern in ituplePattern.Subpatterns)
                        NoteDeclaredPatternVariables(subpattern.Pattern);

                    break;

                case BoundListPattern listPattern:
                    foreach (var elementPattern in listPattern.Subpatterns)
                        NoteDeclaredPatternVariables(elementPattern);

                    noteOneVariable(listPattern.Variable);
                    break;

                case BoundConstantPattern constantPattern:
                    // It is possible for the region to be the expression within a pattern.
                    VisitRvalue(constantPattern.Value);
                    break;

                case BoundRelationalPattern relationalPattern:
                    // It is possible for the region to be the expression within a pattern.
                    VisitRvalue(relationalPattern.Value);
                    break;

                case BoundNegatedPattern negatedPattern:
                    NoteDeclaredPatternVariables(negatedPattern.Negated);
                    break;

                case BoundSlicePattern slicePattern:
                    if (slicePattern.Pattern != null)
                        NoteDeclaredPatternVariables(slicePattern.Pattern);

                    break;

                case BoundDiscardPattern or BoundTypePattern:
                    // Does not contain variables or expressions. Nothing to visit.
                    break;

                case BoundBinaryPattern:
                    {
                        var binaryPattern = (BoundBinaryPattern)pattern;
                        if (binaryPattern.Left is not BoundBinaryPattern)
                        {
                            NoteDeclaredPatternVariables(binaryPattern.Left);
                            NoteDeclaredPatternVariables(binaryPattern.Right);
                            break;
                        }

                        // Users (such as ourselves) can have many, many nested binary patterns. To avoid crashing, do left recursion manually.
                        var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
                        do
                        {
                            stack.Push(binaryPattern);
                            binaryPattern = binaryPattern.Left as BoundBinaryPattern;
                        } while (binaryPattern is not null);

                        binaryPattern = stack.Pop();
                        NoteDeclaredPatternVariables(binaryPattern.Left);

                        do
                        {
                            NoteDeclaredPatternVariables(binaryPattern.Right);
                        } while (stack.TryPop(out binaryPattern));

                        stack.Free();
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }

            void noteOneVariable(Symbol? symbol)
            {
                if (IsInside && symbol?.Kind == SymbolKind.Local)
                {
                    // Because this API only returns local symbols and parameters,
                    // we exclude pattern variables that have become fields in scripts.
                    _variablesDeclared.Add(symbol);
                }
            }
        }

        public override BoundNode? VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            if (IsInside)
            {
                _variablesDeclared.Add(node.LocalSymbol);
            }

            return base.VisitLocalDeclaration(node);
        }

        public override BoundNode? VisitLambda(BoundLambda node)
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

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
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

        public override BoundNode? VisitQueryClause(BoundQueryClause node)
        {
            if (IsInside)
            {
                if ((object?)node.DefinedSymbol != null)
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

        public override BoundNode? VisitLocal(BoundLocal node)
        {
            if (IsInside && node.DeclarationKind != BoundLocalDeclarationKind.None)
            {
                _variablesDeclared.Add(node.LocalSymbol);
            }

            return null;
        }
    }
}