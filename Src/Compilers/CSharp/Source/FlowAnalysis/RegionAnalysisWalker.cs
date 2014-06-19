using System;
using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A base class for walkers that need to analyze a particular region of a method body.  Provides convenient hooks
    /// so that subclasses can gain control when the analyzer enters and exits the region of interest.
    /// </summary>
    internal abstract class RegionAnalysisWalker : FlowAnalysisWalker
    {
        protected enum RegionPlace { before, inside, after };
        protected readonly TextSpan region;
        protected RegionPlace regionPlace; // tells whether we are analyzing the region before, during, or after the region

        protected RegionAnalysisWalker(Compilation compilation, SyntaxTree tree, MethodSymbol method, BoundStatement block, TextSpan region, ThisSymbolCache thisSymbolCache, HashSet<Symbol> unassignedVariables = null, bool trackUnassignmentsInLoops = false)
            : base(compilation, tree, method, block, thisSymbolCache, unassignedVariables, trackUnassignmentsInLoops)
        {
            this.region = region;

            // assign slots to the parameters
            foreach (var parameter in method.Parameters)
            {
                MakeSlot(parameter);
            }
        }

        /// <summary>
        /// Used to keep track of whether we are currently within the region or not.
        /// </summary>
        /// <param name="currentLocation"></param>
        private void NoteLocation(TextSpan currentLocation)
        {
            switch (regionPlace)
            {
                case RegionPlace.before:
                    if (currentLocation.Start >= region.Start)
                    {
                        regionPlace = RegionPlace.inside;
                        EnterRegion();
                        goto case RegionPlace.inside;
                    }

                    break;

                case RegionPlace.inside:
                    if (currentLocation.Start > region.End)
                    {
                        regionPlace = RegionPlace.after;
                        LeaveRegion();
                        goto case RegionPlace.after;
                    }
                    else
                    {
                        // we have a location that spans inside and outside.
                        // probably the region is a subexpression and we are looking
                        // at an enclosing expression.  We depend on the positions coming from the
                        // subexpressions to properly compute the before/inside/after.
                    }

                    break;

                case RegionPlace.after:
                    // Should move monotonically forward through the text of the program.
                    // Note that this requires that all the basic flow analysis visitors handle their constructs in program text order.
                    if (currentLocation.Start <= region.End)
                        throw new ArgumentException();
                    break;
            }
        }

        /// <summary>
        /// Subclasses may override EnterRegion to perform any actions at the entry to the region.
        /// </summary>
        protected virtual void EnterRegion()
        {
        }

        /// <summary>
        /// Subclasses may override LeaveRegion to perform any action at the end of the region.
        /// </summary>
        protected virtual void LeaveRegion()
        {
        }

        protected override void Visit(BoundNode node)
        {
            if (node != null && node.Syntax != null)
            {
                NoteLocation(node.Syntax.Span);
            }

            base.Visit(node);
        }

        protected override void Assign(BoundNode node, BoundExpression value, bool assigned = true)
        {
            switch (node.Kind)
            {
                case BoundKind.LocalDeclaration:
                    var decl = node.Syntax as VariableDeclaratorSyntax;
                    if (this.regionPlace == RegionPlace.inside && decl != null && decl.Identifier.Span.End < region.Start)
                    {
                        this.regionPlace = RegionPlace.after;
                        LeaveRegion();
                    }

                    break;

                case BoundKind.Local:
                case BoundKind.Parameter:
                case BoundKind.ThisReference:
                    if (this.regionPlace == RegionPlace.inside && node.Syntax.Span.End < region.Start)
                    {
                        this.regionPlace = RegionPlace.after;
                        LeaveRegion();
                    }

                    break;
            }

            base.Assign(node, value, assigned);
        }

        protected override void StartBlock(BoundBlock block)
        {
            var blockSyntax = block.Syntax as BlockSyntax;
            if (blockSyntax == null)
            {
                return;
            }

            NoteLocation(blockSyntax.OpenBraceToken.Span);
        }

        protected override void EndBlock(BoundBlock block)
        {
            var blockSyntax = block.Syntax as BlockSyntax;
            if (blockSyntax == null)
            {
                return;
            }

            NoteLocation(blockSyntax.CloseBraceToken.Span);
        }

        /// <summary>
        /// To scan the whole body, we start outside (before) the region.
        /// </summary>
        protected override void Scan()
        {
            regionPlace = RegionPlace.before;
            base.Scan();
        }
    }
}