// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateFromMembers.AddConstructorParameters;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateFromMembers.AddConstructorParameters
{
    [ExportLanguageService(typeof(IAddConstructorParametersService), LanguageNames.CSharp), Shared]
    internal class CSharpAddConstructorParametersService :
        AbstractAddConstructorParametersService<CSharpAddConstructorParametersService, MemberDeclarationSyntax>
    {
        protected override async Task<IList<MemberDeclarationSyntax>> GetSelectedMembersAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return SpecializedCollections.EmptyList<MemberDeclarationSyntax>();
            }
            else
            {
                return await GenerateFromMembersHelpers.GetSelectedMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
        {
            return GenerateFromMembersHelpers.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken);
        }
    }
}
