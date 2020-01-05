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
        [ImportingConstructor]
        public MethodDeclarationOrganizer()
        {
        }

        protected override MethodDeclarationSyntax Organize(
            MethodDeclarationSyntax syntax,
            CancellationToken cancellationToken)
        {
            return syntax.Update(
                attributeLists: syntax.AttributeLists,
                modifiers: ModifiersOrganizer.Organize(syntax.Modifiers),
                returnType: syntax.ReturnType,
                explicitInterfaceSpecifier: syntax.ExplicitInterfaceSpecifier,
                identifier: syntax.Identifier,
                typeParameterList: syntax.TypeParameterList,
                parameterList: syntax.ParameterList,
                constraintClauses: syntax.ConstraintClauses,
                body: syntax.Body,
                expressionBody: syntax.ExpressionBody,
                semicolonToken: syntax.SemicolonToken);
        }
    }
}
