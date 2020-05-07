// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {

        private static SyntaxList<MemberDeclarationSyntax> GenerateMembers(IEnumerable<INamespaceOrTypeSymbol> members)
        {
            var builder = ArrayBuilder<MemberDeclarationSyntax>.GetInstance();

            foreach (var member in members)
                builder.Add(GenerateMember(member));

            return List(builder.ToImmutableAndFree());
        }

        private static MemberDeclarationSyntax GenerateMember(INamespaceOrTypeSymbol member)
            => (MemberDeclarationSyntax)GenerateSyntaxWorker(member);

        private static SyntaxList<UsingDirectiveSyntax> GenerateUsings(
            ImmutableArray<INamespaceOrTypeSymbol> imports)
        {
            var builder = ArrayBuilder<UsingDirectiveSyntax>.GetInstance();

            foreach (var import in imports)
            {
                if (import is INamespaceSymbol nsSymbol)
                    builder.Add(UsingDirective(ParseName(nsSymbol.Name)));
                else if (import is ITypeSymbol typeSymbol)
                    builder.Add(UsingDirective(Token(SyntaxKind.StaticKeyword), alias: null, GenerateName(typeSymbol)));
            }

            return List(builder.ToImmutableAndFree());
        }
    }
}
