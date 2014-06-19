using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Below are the unimplemented parts of flow analysis.
    /// </summary>
    partial class FlowAnalysisWalker
    {
        protected object Unimplemented(BoundNode node, String feature)
        {
            Diagnostics.Add(ErrorCode.ERR_NotYetImplementedInRoslyn, new SourceLocation(tree, node.Syntax), feature);
            return null;
        }

        public override object DefaultVisit(BoundNode node, object arg)
        {
            return Unimplemented(node, "node kind '" + node.Kind + "' for flow analysis");
        }

        public override object VisitArgListOperator(BoundArgListOperator node, object arg)
        {
            return Unimplemented(node, "arglist");
        }
        
        public override object VisitEventAccess(BoundEventAccess node, object arg)
        {
            return Unimplemented(node, "event access");
        }
        
        public override object VisitFalseOperator(BoundFalseOperator node, object arg)
        {
            return Unimplemented(node, "false operator");
        }
        
        public override object VisitIndexerAccess(BoundIndexerAccess node, object arg)
        {
            return Unimplemented(node, "indexer access");
        }
        
        public override object VisitLoadTemporary(BoundLoadTemporary node, object arg)
        {
            return Unimplemented(node, "load temporary");
        }
        
        public override object VisitMakeRefOperator(BoundMakeRefOperator node, object arg)
        {
            return Unimplemented(node, "ref");
        }

        public override object VisitPropertyAccess(BoundPropertyAccess node, object arg)
        {
            return Unimplemented(node, "property access");
        }
        
        public override object VisitRefTypeOperator(BoundRefTypeOperator node, object arg)
        {
            return Unimplemented(node, "ref type");
        }
        
        public override object VisitRefValueOperator(BoundRefValueOperator node, object arg)
        {
            return Unimplemented(node, "ref value");
        }
        
        public override object VisitStoreTemporary(BoundStoreTemporary node, object arg)
        {
            return Unimplemented(node, "store temporary");
        }
        
        public override object VisitTrueOperator(BoundTrueOperator node, object arg)
        {
            return Unimplemented(node, "true operator");
        }
        
        public override object VisitTypeOrName(BoundTypeOrName node, object arg)
        {
            return Unimplemented(node, "'type or name'");
        }
    }
}
