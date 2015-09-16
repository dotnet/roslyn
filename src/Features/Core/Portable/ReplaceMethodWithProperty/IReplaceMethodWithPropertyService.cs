using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    interface IReplaceMethodWithPropertyService : ILanguageService
    {
        SyntaxNode GetMethodDeclaration(SyntaxToken token);
        SyntaxNode ConvertMethodsToProperty(SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods, string propertyName, bool nameChanged);
        string GetMethodName(SyntaxNode methodDeclaration);
        void ReplaceGetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);
        void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);
    }

    internal struct GetAndSetMethods
    {
        public readonly IMethodSymbol GetMethod;
        public readonly IMethodSymbol SetMethod;
        public readonly SyntaxNode GetMethodDeclaration;
        public readonly SyntaxNode SetMethodDeclaration;

        public GetAndSetMethods(
            IMethodSymbol getMethod, IMethodSymbol setMethod,
            SyntaxNode getMethodDeclaration, SyntaxNode setMethodDeclaration)
        {
            GetMethod = getMethod;
            SetMethod = setMethod;
            GetMethodDeclaration = getMethodDeclaration;
            SetMethodDeclaration = setMethodDeclaration;
        }
    }

}