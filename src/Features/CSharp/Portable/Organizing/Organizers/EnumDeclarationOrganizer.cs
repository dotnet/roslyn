// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class EnumDeclarationOrganizer : AbstractSyntaxNodeOrganizer<EnumDeclarationSyntax>
    {
        protected override EnumDeclarationSyntax Organize(
            EnumDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(
                syntax.AttributeLists,
                ModifiersOrganizer.Organize(syntax.Modifiers),
                syntax.EnumKeyword,
                syntax.Identifier,
                syntax.BaseList,
                syntax.OpenBraceToken,
                syntax.Members,
                syntax.CloseBraceToken,
                syntax.SemicolonToken);
        }
    }
}
