﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class InterfaceDeclarationOrganizer : AbstractSyntaxNodeOrganizer<InterfaceDeclarationSyntax>
    {
        [ImportingConstructor]
        public InterfaceDeclarationOrganizer()
        {
        }

        protected override InterfaceDeclarationSyntax Organize(
            InterfaceDeclarationSyntax syntax,
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
