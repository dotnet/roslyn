// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class StructDeclarationOrganizer : AbstractSyntaxNodeOrganizer<StructDeclarationSyntax>
    {
        [ImportingConstructor]
        public StructDeclarationOrganizer()
        {
        }

        protected override StructDeclarationSyntax Organize(
            StructDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(
                syntax.AttributeLists,
                ModifiersOrganizer.Organize(syntax.Modifiers),
                syntax.Keyword,
                syntax.Identifier,
                syntax.TypeParameterList,
                syntax.BaseList,
                syntax.ConstraintClauses,
                syntax.OpenBraceToken,
                MemberDeclarationsOrganizer.Organize(syntax.Members, cancellationToken),
                syntax.CloseBraceToken,
                syntax.SemicolonToken);
        }
    }
}
