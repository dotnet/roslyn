// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass
{
    [ExportLanguageService(typeof(IImplementAbstractClassService), LanguageNames.CSharp), Shared]
    internal class CSharpImplementAbstractClassService : AbstractImplementAbstractClassService
    {
        protected override bool TryInitializeState(
            Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken,
            out INamedTypeSymbol classType, out INamedTypeSymbol abstractClassType)
        {
            var baseClassNode = node as TypeSyntax;
            if (baseClassNode != null && baseClassNode.Parent is BaseTypeSyntax &&
                baseClassNode.Parent.IsParentKind(SyntaxKind.BaseList) &&
                ((BaseTypeSyntax)baseClassNode.Parent).Type == baseClassNode)
            {
                if (baseClassNode.Parent.Parent.IsParentKind(SyntaxKind.ClassDeclaration))
                {
                    abstractClassType = model.GetTypeInfo(baseClassNode, cancellationToken).Type as INamedTypeSymbol;
                    cancellationToken.ThrowIfCancellationRequested();

                    if (abstractClassType.IsAbstractClass())
                    {
                        var classDecl = baseClassNode.Parent.Parent.Parent as ClassDeclarationSyntax;
                        classType = model.GetDeclaredSymbol(classDecl, cancellationToken) as INamedTypeSymbol;

                        return classType != null && abstractClassType != null;
                    }
                }
            }

            classType = null;
            abstractClassType = null;
            return false;
        }
    }
}
