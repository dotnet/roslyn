using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods
{
    internal abstract class AbstractReplacePropertyWithMethodsService : IReplacePropertyWithMethodsService
    {
        public abstract SyntaxNode GetPropertyDeclaration(SyntaxToken token);
        public abstract SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration);
        public abstract IList<SyntaxNode> GetReplacementMembers(Document document, IPropertySymbol property, SyntaxNode propertyDeclaration, IFieldSymbol propertyBackingField, string desiredGetMethodName, string desiredSetMethodName, CancellationToken cancellationToken);
        public abstract void ReplaceReference(SyntaxEditor editor, SyntaxToken nameToken, IPropertySymbol property, IFieldSymbol propertyBackingField, string desiredGetMethodName, string desiredSetMethodName);

        protected static SyntaxNode GetFieldReference(SyntaxGenerator generator, IFieldSymbol propertyBackingField)
        {
            var through = propertyBackingField.IsStatic
                ? generator.TypeExpression(propertyBackingField.ContainingType)
                : generator.ThisExpression();

            return generator.MemberAccessExpression(through, propertyBackingField.Name);
        }
    }
}
