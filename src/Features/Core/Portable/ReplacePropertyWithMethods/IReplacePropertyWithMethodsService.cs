using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplacePropertyWithMethods
{
    internal interface IReplacePropertyWithMethodsService : ILanguageService
    {
        SyntaxNode GetPropertyDeclaration(SyntaxToken token);

        Task ReplaceReferenceAsync(
            Document document,
            SyntaxEditor editor, SyntaxToken nameToken,
            IPropertySymbol property, IFieldSymbol propertyBackingField,
            string desiredGetMethodName, string desiredSetMethodName,
            CancellationToken cancellationToken);

        IList<SyntaxNode> GetReplacementMembers(
            Document document,
            IPropertySymbol property, SyntaxNode propertyDeclaration,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName,
            CancellationToken cancellationToken);

        SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration);
    }
}