// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateFromMembers
{
    internal static class GenerateFromMembersHelpers
    {
        internal static async Task<IList<MemberDeclarationSyntax>> GetSelectedMembersAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return tree.GetMembersInSpan(textSpan, cancellationToken);
        }

        internal static IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
        {
            if (memberDeclaration is FieldDeclarationSyntax)
            {
                return ((FieldDeclarationSyntax)memberDeclaration).Declaration.Variables.Select(
                    v => semanticModel.GetDeclaredSymbol(v, cancellationToken));
            }

            return SpecializedCollections.SingletonEnumerable(
                semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken));
        }
    }
}
