using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    // If any field access refers to a constant field with a bad value, mark the access as an error.
    // This matters in the case where there is an invalid constant field (i.e. ConstantValue.Bad)
    // that is accessed in a non-constructor method.  We can't emit that method since there is no
    // value to inline, so we have to indicate that there's an error.

    internal sealed class BadConstantRewriter : BoundTreeRewriter
    {
        private static readonly BadConstantRewriter Instance = new BadConstantRewriter();

        private BadConstantRewriter() { }

        public static BoundStatement Rewrite(BoundStatement node)
        {
            Debug.Assert(node != null);
            return (BoundStatement)Instance.Visit(node);
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            Debug.Assert(node != null);
            var constantValue = node.ConstantValue;
            if (!node.HasErrors && constantValue != null && constantValue.IsBad)
            {
                //we depend on whoever set the value to Bad to have added an appropriate diagnostic
                return new BoundFieldAccess(node.Syntax, node.SyntaxTree, node.ReceiverOpt, node.FieldSymbol, constantValue, true);
            }
            return base.VisitFieldAccess(node);
        }
    }
}