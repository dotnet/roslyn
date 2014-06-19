using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class LocalRewriter
    {
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
