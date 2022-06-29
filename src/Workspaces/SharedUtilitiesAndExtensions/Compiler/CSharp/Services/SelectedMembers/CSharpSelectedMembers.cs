// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal class CSharpSelectedMembers : AbstractSelectedMembers<
        MemberDeclarationSyntax,
        FieldDeclarationSyntax,
        PropertyDeclarationSyntax,
        TypeDeclarationSyntax,
        VariableDeclaratorSyntax>
    {
        public static readonly CSharpSelectedMembers Instance = new();

        private CSharpSelectedMembers()
        {
        }

        protected override IEnumerable<(SyntaxToken identifier, SyntaxNode declaration)> GetDeclarationsAndIdentifiers(MemberDeclarationSyntax member)
        {
            return member switch
            {
                FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Variables.Select(
                    v => (identifier: v.Identifier, declaration: v as SyntaxNode)),
                EventFieldDeclarationSyntax eventFieldDeclaration => eventFieldDeclaration.Declaration.Variables.Select(
                    v => (identifier: v.Identifier, declaration: v as SyntaxNode)),
                _ => ImmutableArray.Create((identifier: member.GetNameToken(), declaration: member as SyntaxNode)),
            };
        }

        protected override SyntaxList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax containingType)
            => containingType.Members;
    }
}
