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
            SyntaxEditor editor, SyntaxToken nameToken, IFieldSymbol propertyBackingField);

        void ReplacePropertyWithMethod(
            SyntaxEditor editor, SemanticModel semanticModel,
            IPropertySymbol property, SyntaxNode declaration,
            IFieldSymbol propertyBackingField);
    }
}