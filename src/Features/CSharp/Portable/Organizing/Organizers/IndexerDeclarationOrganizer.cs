// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class IndexerDeclarationOrganizer : AbstractSyntaxNodeOrganizer<IndexerDeclarationSyntax>
    {
        [ImportingConstructor]
        public IndexerDeclarationOrganizer()
        {
        }

        protected override IndexerDeclarationSyntax Organize(
            IndexerDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(
                attributeLists: syntax.AttributeLists,
                modifiers: ModifiersOrganizer.Organize(syntax.Modifiers),
                type: syntax.Type,
                explicitInterfaceSpecifier: syntax.ExplicitInterfaceSpecifier,
                thisKeyword: syntax.ThisKeyword,
                parameterList: syntax.ParameterList,
                accessorList: syntax.AccessorList,
                expressionBody: syntax.ExpressionBody,
                semicolonToken: syntax.SemicolonToken);
        }
    }
}
