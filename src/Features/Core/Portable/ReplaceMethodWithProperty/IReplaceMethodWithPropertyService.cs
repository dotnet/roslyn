using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    interface IReplaceMethodWithPropertyService : ILanguageService
    {
        SyntaxNode GetMethodDeclaration(SyntaxToken token);
        SyntaxNode ConvertMethodToProperty(SyntaxNode method, string propertyName);
        void ReplaceReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);
        string GetMethodName(SyntaxNode methodDeclaration);
    }
}