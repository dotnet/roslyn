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

        protected override IEnumerable<VariableDeclaratorSyntax> GetAllDeclarators(FieldDeclarationSyntax field)
            => field.Declaration.Variables;

        protected override IEnumerable<SyntaxToken> GetMemberIdentifiers(MemberDeclarationSyntax member)
        {
            return member switch
            {
                FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Variables.Select(GetVariableIdentifier),
                EventFieldDeclarationSyntax eventFieldDeclaration => eventFieldDeclaration.Declaration.Variables.Select(GetVariableIdentifier),
                _ => ImmutableArray.Create(member.GetNameToken()),
            };
        }

        protected override SyntaxList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax containingType)
            => containingType.Members;

        protected override SyntaxToken GetPropertyIdentifier(PropertyDeclarationSyntax declarator)
            => declarator.Identifier;

        protected override SyntaxToken GetVariableIdentifier(VariableDeclaratorSyntax declarator)
            => declarator.Identifier;
    }
}
