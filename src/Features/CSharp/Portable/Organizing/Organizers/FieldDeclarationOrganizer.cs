// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class FieldDeclarationOrganizer : AbstractSyntaxNodeOrganizer<FieldDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FieldDeclarationOrganizer()
        {
        }

        protected override FieldDeclarationSyntax Organize(
            FieldDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(syntax.AttributeLists,
                ModifiersOrganizer.Organize(syntax.Modifiers),
                syntax.Declaration,
                syntax.SemicolonToken);
        }
    }
}
