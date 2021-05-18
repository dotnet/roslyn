// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReassignedVariable
{
    [ExportLanguageService(typeof(IReassignedVariableService), LanguageNames.CSharp), Shared]
    internal class CSharpReassignedVariableService : AbstractReassignedVariableService<
        ParameterSyntax,
        VariableDeclaratorSyntax,
        SingleVariableDesignationSyntax,
        IdentifierNameSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpReassignedVariableService()
        {
        }

        protected override SyntaxToken GetIdentifierOfVariable(VariableDeclaratorSyntax variable)
            => variable.Identifier;

        protected override SyntaxToken GetIdentifierOfSingleVariableDesignation(SingleVariableDesignationSyntax variable)
            => variable.Identifier;

        protected override bool HasInitializer(SyntaxNode variable)
            => (variable as VariableDeclaratorSyntax)?.Initializer != null;

        protected override SyntaxNode GetMemberBlock(SyntaxNode methodOrPropertyDeclaration)
            => methodOrPropertyDeclaration;

        protected override SyntaxNode GetParentScope(SyntaxNode localDeclaration)
        {
            var current = localDeclaration;
            while (current != null)
            {
                if (current is BlockSyntax or SwitchSectionSyntax or ArrowExpressionClauseSyntax or AnonymousMethodExpressionSyntax or MemberDeclarationSyntax)
                    break;

                current = current.Parent;
            }

            Contract.ThrowIfNull(current, "Couldn't find a suitable parent of this local declaration");
            return current is GlobalStatementSyntax
                ? current.GetRequiredParent()
                : current;
        }
    }
}
