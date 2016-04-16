// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    [ExportSyntaxNodeOrganizer(LanguageNames.CSharp), Shared]
    internal class MethodDeclarationOrganizer : AbstractSyntaxNodeOrganizer<MethodDeclarationSyntax>
    {
        protected override MethodDeclarationSyntax Organize(
            MethodDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(syntax.AttributeLists,
                ModifiersOrganizer.Organize(syntax.Modifiers),
                syntax.ReturnType,
                syntax.ExplicitInterfaceSpecifier,
                syntax.Identifier,
                syntax.TypeParameterList,
                syntax.ParameterList,
                syntax.ConstraintClauses,
                syntax.Body,
                syntax.SemicolonToken);
        }
    }
}
