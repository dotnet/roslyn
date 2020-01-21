// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveType
{
    [ExportLanguageService(typeof(IMoveTypeService), LanguageNames.CSharp), Shared]
    internal class CSharpMoveTypeService :
        AbstractMoveTypeService<CSharpMoveTypeService, BaseTypeDeclarationSyntax, NamespaceDeclarationSyntax, MemberDeclarationSyntax, CompilationUnitSyntax>
    {
        [ImportingConstructor]
        public CSharpMoveTypeService()
        {
        }

        protected override async Task<BaseTypeDeclarationSyntax> GetRelevantNodeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
            => await document.TryGetRelevantNodeAsync<BaseTypeDeclarationSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
    }
}
