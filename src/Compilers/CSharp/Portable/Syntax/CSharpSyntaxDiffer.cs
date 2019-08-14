namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CSharpSyntaxDiffer : SyntaxDiffer
    {
        internal CSharpSyntaxDiffer(SyntaxNode oldNode, SyntaxNode newNode, bool computeNewText)
            : base(oldNode, newNode, computeNewText) { }

        protected override bool AreSimilarCore(SyntaxNode node1, SyntaxNode node2)
        {
            int node1Group = kindGroup(node1);

            return node1Group != -1 && node1Group == kindGroup(node2);

            static int kindGroup(SyntaxNode node) => node.Kind() switch
            {
                SyntaxKind.ClassDeclaration => 1,
                SyntaxKind.StructDeclaration => 1,
                SyntaxKind.InterfaceDeclaration => 1,
                _ => -1
            };
        }
    }
}
