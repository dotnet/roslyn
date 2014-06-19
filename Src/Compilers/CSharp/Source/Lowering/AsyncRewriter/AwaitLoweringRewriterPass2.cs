using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AwaitLoweringRewriterPass2 : BoundTreeRewriter
    {
        // During the initial lowering, spill temp placeholders are used in place for the fields that will eventually
        // be constructed.
        //
        // The goal is to reuse the synthesized spill fields whenever possible - if two expression of the same type
        // need to be spilled in non-intersecting expressions, then the same spill field should be used for both.

        private readonly SyntheticBoundNodeFactory F;
        private readonly SpillFieldAllocator spillFieldAllocator;

        public AwaitLoweringRewriterPass2(SyntheticBoundNodeFactory F, TypeCompilationState CompilationState)
        {
            this.F = F;
            this.spillFieldAllocator = new SpillFieldAllocator(F, CompilationState);
        }

        public override BoundNode VisitSpillSequence(BoundSpillSequence node)
        {
            ReadOnlyArray<BoundStatement> statements = this.VisitList(node.Statements);
            BoundExpression value = (BoundExpression)this.Visit(node.Value);
            TypeSymbol type = this.VisitType(node.Type);
            return node.Update(node.Locals, node.SpillTemps, node.SpillFields, statements, value, type);
        }

        public override BoundNode VisitSpillBlock(BoundSpillBlock node)
        {
            var newStatements = ArrayBuilder<BoundStatement>.GetInstance();

            spillFieldAllocator.AllocateFields(node.SpillTemps);

            newStatements.AddRange(VisitList(node.Statements));

            // Release references held by the spill temps:
            foreach (var spill in node.SpillTemps)
            {
                if (spill.Type.IsManagedType)
                {
                    var field = spillFieldAllocator.GetField(spill);
                    newStatements.Add(F.Assignment(F.Field(F.This(), field), F.NullOrDefault(field.Type)));
                }

                spillFieldAllocator.Free(spill);
            }

            return F.Block(node.Locals, newStatements.ToReadOnlyAndFree());
        }

        public override BoundNode VisitSpillTemp(BoundSpillTemp node)
        {
            return F.Field(F.This(), spillFieldAllocator.GetField(node));
        }
    }
}