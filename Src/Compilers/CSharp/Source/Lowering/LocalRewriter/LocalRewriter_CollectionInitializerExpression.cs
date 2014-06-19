using System.Diagnostics;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            Debug.Fail("BoundCollectionInitializerExpression must have been rewritten while rewriting the outer BoundObjectCreationExpression");
            return null;
        }
    }
}