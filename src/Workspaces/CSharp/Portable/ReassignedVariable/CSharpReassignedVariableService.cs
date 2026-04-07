// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ReassignedVariable;

[ExportLanguageService(typeof(IReassignedVariableService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpReassignedVariableService() : AbstractReassignedVariableService<
    ParameterSyntax,
    VariableDeclaratorSyntax,
    SingleVariableDesignationSyntax,
    IdentifierNameSyntax>
{
    protected override SyntaxToken GetIdentifierOfVariable(VariableDeclaratorSyntax variable)
        => variable.Identifier;

    protected override SyntaxToken GetIdentifierOfSingleVariableDesignation(SingleVariableDesignationSyntax variable)
        => variable.Identifier;

    protected override bool HasInitializer(SyntaxNode variable)
    {
        // For regular variable declarators like `var x = 0`
        if (variable is VariableDeclaratorSyntax declarator)
            return declarator.Initializer != null;

        // For deconstruction like `var (b, c) = (0, 0)`, pattern matching like `is var x`, or `out var x`
        if (variable is SingleVariableDesignationSyntax designation)
        {
            // Walk up the tree to find if this is part of an initialized declaration
            var current = designation.Parent;

            while (current != null)
            {
                // For out var, the variable is always initialized by the call
                if (current is ArgumentSyntax { RefOrOutKeyword.RawKind: (int)SyntaxKind.OutKeyword })
                    return true;

                // For deconstruction assignment like (x, y) = ... or var (x, y) = ...
                if (current is AssignmentExpressionSyntax)
                    return true;

                // For foreach (var (x, y) in ...) or similar
                if (current is ForEachVariableStatementSyntax)
                    return true;

                // Variables in patterns are always consider assigned by virtue off the pattern itself matching.
                if (current is PatternSyntax)
                    return true;

                // Don't search beyond statement boundaries
                if (current is StatementSyntax)
                    break;

                current = current.Parent;
            }
        }

        return false;
    }

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
