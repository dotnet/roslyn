// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateFromMembers
{
    [ExportLanguageService(typeof(IGenerateFromMembersHelperService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateFromMembersHelpersService : IGenerateFromMembersHelperService
    {
        public async Task<ImmutableArray<SyntaxNode>> GetSelectedMembersAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return ImmutableArray<SyntaxNode>.CastUp(tree.GetMembersInSpan(textSpan, cancellationToken));
        }

        public IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, SyntaxNode memberDeclaration, CancellationToken cancellationToken)
        {
            if (memberDeclaration is FieldDeclarationSyntax field)
            {
                return field.Declaration.Variables.Select(
                    v => semanticModel.GetDeclaredSymbol(v, cancellationToken));
            }

            return SpecializedCollections.SingletonEnumerable(
                semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken));
        }
    }
}