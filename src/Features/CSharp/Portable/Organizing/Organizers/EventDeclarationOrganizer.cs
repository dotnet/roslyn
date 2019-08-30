// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class EventDeclarationOrganizer : AbstractSyntaxNodeOrganizer<EventDeclarationSyntax>
    {
        [ImportingConstructor]
        public EventDeclarationOrganizer()
        {
        }

        protected override EventDeclarationSyntax Organize(
            EventDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(syntax.AttributeLists,
                ModifiersOrganizer.Organize(syntax.Modifiers),
                syntax.EventKeyword,
                syntax.Type,
                syntax.ExplicitInterfaceSpecifier,
                syntax.Identifier,
                syntax.AccessorList,
                syntax.SemicolonToken);
        }
    }
}
