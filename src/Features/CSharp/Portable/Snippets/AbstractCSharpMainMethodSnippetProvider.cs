// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

internal abstract class AbstractCSharpMainMethodSnippetProvider
    : AbstractMainMethodSnippetProvider<MethodDeclarationSyntax, StatementSyntax, TypeSyntax>
{
    protected override bool IsValidSnippetLocation(SnippetContext context, CancellationToken cancellationToken)
    {
        var semanticModel = context.SyntaxContext.SemanticModel;
        var syntaxContext = (CSharpSyntaxContext)context.SyntaxContext;

        if (!syntaxContext.IsMemberDeclarationContext(
            validModifiers: SyntaxKindSet.AccessibilityModifiers,
            validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
            canBePartial: true,
            cancellationToken: cancellationToken))
        {
            return false;
        }

        // Syntactically correct position, now semantic checks

        var enclosingTypeSymbol = semanticModel.GetDeclaredSymbol(syntaxContext.ContainingTypeDeclaration!, cancellationToken);

        // If there are any members with name `Main` in enclosing type, inserting `Main` method will create an error
        if (enclosingTypeSymbol is not null &&
            !semanticModel.LookupSymbols(context.Position, container: enclosingTypeSymbol, name: WellKnownMemberNames.EntryPointMethodName).IsEmpty)
        {
            return false;
        }

        // If compilation already has top-level statements, suppress showing `Main` method snippets
        return semanticModel.Compilation.GetTopLevelStatementsMethod() is null;
    }
}
