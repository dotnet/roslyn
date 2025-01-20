// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.GenerateDefaultConstructors;

[ExportLanguageService(typeof(IGenerateDefaultConstructorsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpGenerateDefaultConstructorsService() : AbstractGenerateDefaultConstructorsService<CSharpGenerateDefaultConstructorsService>
{
    protected override bool TryInitializeState(
        SemanticDocument semanticDocument, TextSpan textSpan, CancellationToken cancellationToken,
        [NotNullWhen(true)] out INamedTypeSymbol? classType)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Offer the feature if we're on the header / between members of the class/struct,
        // or if we're on the first base-type of a class

        var helpers = semanticDocument.Document.GetRequiredLanguageService<IRefactoringHelpersService>();
        if (helpers.IsOnTypeHeader(semanticDocument.Root, textSpan.Start, out var typeDeclaration) ||
            helpers.IsBetweenTypeMembers(semanticDocument.Text, semanticDocument.Root, textSpan.Start, out typeDeclaration))
        {
            classType = semanticDocument.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;
            return classType?.TypeKind is TypeKind.Class or TypeKind.Struct;
        }

        var syntaxTree = semanticDocument.SyntaxTree;
        var node = semanticDocument.Root.FindToken(textSpan.Start).GetAncestor<TypeSyntax>();
        if (node is { Parent: BaseTypeSyntax { Parent: BaseListSyntax { Types: [var firstType, ..] } baseList } })
        {
            if (baseList.Parent is TypeDeclarationSyntax(SyntaxKind.ClassDeclaration or SyntaxKind.RecordDeclaration) parentTypeDecl &&
                firstType.Type == node)
            {
                classType = semanticDocument.SemanticModel.GetDeclaredSymbol(parentTypeDecl, cancellationToken);
                return classType != null;
            }
        }

        classType = null;
        return false;
    }
}
