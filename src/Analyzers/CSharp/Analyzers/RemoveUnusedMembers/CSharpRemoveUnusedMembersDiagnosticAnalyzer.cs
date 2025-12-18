// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnusedMembersDiagnosticAnalyzer
    : AbstractRemoveUnusedMembersDiagnosticAnalyzer<
        DocumentationCommentTriviaSyntax,
        IdentifierNameSyntax,
        TypeDeclarationSyntax,
        MemberDeclarationSyntax>
{
    protected override ISemanticFacts SemanticFacts => CSharpSemanticFacts.Instance;

    protected override IEnumerable<TypeDeclarationSyntax> GetTypeDeclarations(INamedTypeSymbol namedType, CancellationToken cancellationToken)
    {
        return namedType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(cancellationToken))
            .OfType<TypeDeclarationSyntax>();
    }

    protected override IEnumerable<MemberDeclarationSyntax> GetMembersIncludingExtensionBlockMembers(TypeDeclarationSyntax typeDeclaration)
    {
        foreach (var member in typeDeclaration.Members)
        {
            if (member is ExtensionBlockDeclarationSyntax extensionBlock)
            {
                foreach (var extensionMember in extensionBlock.Members)
                    yield return extensionMember;
            }
            else
            {
                yield return member;
            }
        }
    }

    protected override SyntaxNode GetParentIfSoleDeclarator(SyntaxNode node)
    {
        return node switch
        {
            VariableDeclaratorSyntax variableDeclarator
                => variableDeclarator.Parent is VariableDeclarationSyntax
                {
                    Parent: FieldDeclarationSyntax { Declaration.Variables.Count: 0 } or
                            EventFieldDeclarationSyntax { Declaration.Variables.Count: 0 }
                } declaration ? declaration.GetRequiredParent() : node,
            _ => node,
        };
    }
}
