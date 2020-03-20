// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        public static readonly CSharpSelectedMembers Instance = new CSharpSelectedMembers();

        private CSharpSelectedMembers()
        {
        }

        protected override IEnumerable<VariableDeclaratorSyntax> GetAllDeclarators(FieldDeclarationSyntax field)
            => field.Declaration.Variables;

        protected override SyntaxList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax containingType)
            => containingType.Members;

        protected override SyntaxToken GetPropertyIdentifier(PropertyDeclarationSyntax declarator)
            => declarator.Identifier;

        protected override SyntaxToken GetVariableIdentifier(VariableDeclaratorSyntax declarator)
            => declarator.Identifier;
    }
}
