using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods
{
    internal interface IReplacePropertyWithMethodsService : ILanguageService
    {
        SyntaxNode GetPropertyDeclaration(SyntaxToken token);

        void ReplaceReference(
            SyntaxEditor editor, SyntaxToken nameToken,
            IPropertySymbol property, IFieldSymbol propertyBackingField,
            string desiredGetMethodName, string desiredSetMethodName);

        IList<SyntaxNode> GetReplacementMembers(
            Document document,
            IPropertySymbol property, SyntaxNode propertyDeclaration,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName);

        SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration);
    }
}