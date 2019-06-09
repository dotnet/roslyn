// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class EnumDeclarationOrganizer : AbstractSyntaxNodeOrganizer<EnumDeclarationSyntax>
    {
        [ImportingConstructor]
        public EnumDeclarationOrganizer()
        {
        }

        protected override EnumDeclarationSyntax Organize(
            EnumDeclarationSyntax syntax,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            return syntax.Update(
                syntax.AttributeLists,
                ModifiersOrganizer.ForCodeStyle(optionSet).Organize(syntax.Modifiers),
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
