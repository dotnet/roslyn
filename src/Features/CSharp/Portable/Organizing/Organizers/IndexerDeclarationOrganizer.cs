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

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers;

[ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
internal sealed class IndexerDeclarationOrganizer : AbstractSyntaxNodeOrganizer<IndexerDeclarationSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
