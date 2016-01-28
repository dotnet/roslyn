// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    internal interface IReplaceMethodWithPropertyService : ILanguageService
    {
        SyntaxNode GetMethodDeclaration(SyntaxToken token);
        string GetMethodName(SyntaxNode methodDeclaration);
        void ReplaceGetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);
        void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged);

        void ReplaceGetMethodWithProperty(SyntaxEditor editor, SemanticModel semanticModel, GetAndSetMethods getAndSetMethods, string propertyName, bool nameChanged);
        void RemoveSetMethod(SyntaxEditor editor, SyntaxNode setMethodDeclaration);
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