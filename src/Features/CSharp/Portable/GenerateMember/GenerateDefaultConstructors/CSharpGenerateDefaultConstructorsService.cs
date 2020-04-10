// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateDefaultConstructors
{
    [ExportLanguageService(typeof(IGenerateDefaultConstructorsService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateDefaultConstructorsService : AbstractGenerateDefaultConstructorsService<CSharpGenerateDefaultConstructorsService>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateDefaultConstructorsService()
        {
        }

        protected override bool TryInitializeState(
            SemanticDocument semanticDocument, TextSpan textSpan, CancellationToken cancellationToken,
            out INamedTypeSymbol classType)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Offer the feature if we're on the header for the class/struct, or if we're on the 
            // first base-type of a class.

            var syntaxFacts = semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsOnTypeHeader(semanticDocument.Root, textSpan.Start, out var typeDeclaration))
            {
                classType = semanticDocument.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;
                return classType?.TypeKind == TypeKind.Class;
            }

            var syntaxTree = semanticDocument.SyntaxTree;
            var node = semanticDocument.Root.FindToken(textSpan.Start).GetAncestor<TypeSyntax>();
            if (node != null)
            {
                if (node.Parent is BaseTypeSyntax && node.Parent.IsParentKind(SyntaxKind.BaseList, out BaseListSyntax baseList))
                {
                    if (baseList.Types.Count > 0 &&
                        baseList.Types[0].Type == node &&
                        baseList.IsParentKind(SyntaxKind.ClassDeclaration))
                    {
                        var semanticModel = semanticDocument.SemanticModel;
                        classType = semanticModel.GetDeclaredSymbol(baseList.Parent, cancellationToken) as INamedTypeSymbol;
                        return classType != null;
                    }
                }
            }

            classType = null;
            return false;
        }
    }
}
