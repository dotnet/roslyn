// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    internal interface IReplaceMethodWithPropertyService : ILanguageService
    {
        Task<SyntaxNode> GetMethodDeclarationAsync(CodeRefactoringContext context);

        void ReplaceGetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);
        void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);

        void ReplaceGetMethodWithProperty(
            DocumentOptionSet documentOptions, ParseOptions parseOptions,
            SyntaxEditor editor, SemanticModel semanticModel,
            GetAndSetMethods getAndSetMethods, string propertyName, bool nameChanged);

        void RemoveSetMethod(SyntaxEditor editor, SyntaxNode setMethodDeclaration);
    }

    internal readonly struct GetAndSetMethods
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
