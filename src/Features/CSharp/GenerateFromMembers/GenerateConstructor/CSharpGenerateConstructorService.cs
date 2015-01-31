// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.GenerateFromMembers.GenerateConstructor
{
    [ExportLanguageService(typeof(IGenerateConstructorService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateConstructorService :
        AbstractGenerateConstructorService<CSharpGenerateConstructorService, MemberDeclarationSyntax>
    {
        protected override Task<IList<MemberDeclarationSyntax>> GetSelectedMembersAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return GenerateFromMembersHelpers.GetSelectedMembersAsync(document, textSpan, cancellationToken);
        }

        protected override IEnumerable<ISymbol> GetDeclaredSymbols(
            SemanticModel semanticModel, MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
        {
            return GenerateFromMembersHelpers.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken);
        }
    }
}
