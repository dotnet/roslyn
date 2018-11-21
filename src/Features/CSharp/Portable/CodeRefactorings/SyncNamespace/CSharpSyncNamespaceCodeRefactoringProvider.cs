// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SyncNamespace), Shared]
    internal sealed class CSharpSyncNamespaceCodeRefactoringProvider 
        : AbstractSyncNamespaceCodeRefactoringProvider<NamespaceDeclarationSyntax, CompilationUnitSyntax, MemberDeclarationSyntax>
    {
        protected override async Task<SyntaxNode> TryGetApplicableInvocationNode(Document document, int position, CancellationToken cancellationToken)
        {
            var compilationUnit = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            // Here's conditions that trigger the refactoring (all have to be true in each scenario):
            // 
            // - There's only one namespace declaration in the document and all types are declared in it:
            //    1. No nested namespace declaration (even it's empty).
            //    2. The cursor is on the name of the namespace declaration.
            //    3. The name of the namespace is valid (i.e. no errors).
            //    4. No partial type declared in the namespace. Otherwise its multiple declaration will
            //       end up in different namespace.
            //
            // - There's no namespace declaration and all types in the document are declared in global namespace:
            //    1. The cursor is on the name of first declared type.
            //    2. No partial type declared in the document. Otherwise its multiple declaration will
            //       end up in different namespace.

            var triggeringNode = GetTriggeringNode(compilationUnit, position);
            if (triggeringNode != null)
            {
                var containsPartial = await ContainsPartialTypeWithMultipleDeclarationsAsync(document, triggeringNode, cancellationToken)
                    .ConfigureAwait(false);

                if (!containsPartial)
                {
                    return triggeringNode;
                }
            }

            return default;
        }

        private static SyntaxNode GetTriggeringNode(CompilationUnitSyntax compilationUnit, int position)
        {
            var namespaceDecls = compilationUnit.DescendantNodes(n => n is CompilationUnitSyntax || n is NamespaceDeclarationSyntax)
                .OfType<NamespaceDeclarationSyntax>().ToImmutableArray();

            if (namespaceDecls.Length == 1 && compilationUnit.Members.Count == 1)
            {
                var namespaceDeclaration = namespaceDecls[0];
                Debug.Assert(namespaceDeclaration == compilationUnit.Members[0]);

                if (namespaceDeclaration.Name.Span.IntersectsWith(position)
                    && namespaceDeclaration.Name.GetDiagnostics().All(diag => diag.DefaultSeverity != DiagnosticSeverity.Error))
                {
                    return namespaceDeclaration;
                }
            }
            else if (namespaceDecls.Length == 0)
            {
                var firstMemberDeclarationName = compilationUnit.Members.FirstOrDefault().GetNameToken();

                if (firstMemberDeclarationName != default
                    && firstMemberDeclarationName.Span.IntersectsWith(position))
                {
                    return compilationUnit;
                }
            }

            return null;
        }

        protected override SyntaxList<MemberDeclarationSyntax> GetMemberDeclarationsInContainer(SyntaxNode compilationUnitOrNamespaceDecl)
        {
            if (compilationUnitOrNamespaceDecl is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Members;
            }
            else if (compilationUnitOrNamespaceDecl is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit.Members;
            }

            throw ExceptionUtilities.Unreachable;
        }

        protected override string EscapeIdentifier(string identifier)
            => identifier.EscapeIdentifier();
    }
}
