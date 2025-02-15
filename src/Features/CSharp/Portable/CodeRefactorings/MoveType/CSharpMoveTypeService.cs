// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveType;

[ExportLanguageService(typeof(IMoveTypeService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMoveTypeService() :
    AbstractMoveTypeService<CSharpMoveTypeService, BaseTypeDeclarationSyntax, BaseNamespaceDeclarationSyntax, CompilationUnitSyntax>
{
    protected override (string name, int arity) GetSymbolNameAndArity(BaseTypeDeclarationSyntax syntax)
        => (syntax.Identifier.ValueText, syntax is TypeDeclarationSyntax { TypeParameterList.Parameters.Count: var arity } ? arity : 0);

    protected override bool IsMemberDeclaration(SyntaxNode syntaxNode)
        => syntaxNode is MemberDeclarationSyntax;

    protected override async Task<BaseTypeDeclarationSyntax?> GetRelevantNodeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        => await document.TryGetRelevantNodeAsync<BaseTypeDeclarationSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
}
