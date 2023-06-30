// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnusedMembers;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnusedMembersDiagnosticAnalyzer
        : AbstractRemoveUnusedMembersDiagnosticAnalyzer<DocumentationCommentTriviaSyntax, IdentifierNameSyntax>
    {
        protected override void AddAllDocumentationComments(
            INamedTypeSymbol namedType,
            ArrayBuilder<DocumentationCommentTriviaSyntax> documentationComments,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TypeDeclarationSyntax>.GetInstance(out var stack);

            foreach (var reference in namedType.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                    continue;

                stack.Clear();
                stack.Push(typeDeclaration);

                while (stack.Count > 0)
                {
                    var currentType = stack.Pop();

                    // Add the doc comments on the type itself.
                    AddDocumentationComments(currentType, documentationComments);

                    // Walk each member
                    foreach (var member in currentType.GetMembers())
                    {
                        if (member is TypeDeclarationSyntax childType)
                        {
                            // If the member is a nested type, recurse into it.
                            stack.Push(childType);
                        }
                        else
                        {
                            // Otherwise, add the doc comments on the member itself.
                            AddDocumentationComments(member, documentationComments);
                        }
                    }
                }
            }

            return;
        }
    }
}
