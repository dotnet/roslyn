// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

using FixAllScope = CodeAnalysis.CodeFixes.FixAllScope;

internal sealed partial class ConvertToExtensionCodeRefactoringProvider
{
    private sealed class ConvertToExtensionRefactorAllProvider()
        : DocumentBasedRefactorAllProvider(
            [RefactorAllScope.Document, RefactorAllScope.Project, RefactorAllScope.Solution, RefactorAllScope.ContainingType])
    {
        protected override async Task<Document?> RefactorAllAsync(
            RefactorAllContext refactorAllContext,
            Document document,
            Optional<ImmutableArray<TextSpan>> refactorAllSpans)
        {
            var cancellationToken = refactorAllContext.CancellationToken;

            var codeGenerationService = (CSharpCodeGenerationService)document.GetRequiredLanguageService<ICodeGenerationService>();

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            foreach (var declaration in GetTopLevelClassDeclarations(root, refactorAllSpans))
            {
                // We might hit partial parts that have no extension methods in them.  Just skip those.
                var extensionMethods = GetAllExtensionMethods(semanticModel, declaration, cancellationToken);
                if (extensionMethods.IsEmpty)
                    continue;

                // For each class declaration we hit that has extension methods in it, convert all the extension methods
                // to extensions and replace the old declaration with the new one.
                var newDeclaration = ConvertToExtension(
                    codeGenerationService, declaration, extensionMethods, specificExtension: null);
                editor.ReplaceNode(declaration, newDeclaration);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static IEnumerable<ClassDeclarationSyntax> GetTopLevelClassDeclarations(
            SyntaxNode root, Optional<ImmutableArray<TextSpan>> fixAllSpans)
        {
            if (fixAllSpans.HasValue)
            {
                // User selected 'fix all in containing type'.  Core code refactoring engine will return the spans of
                // the containing class.  Process each of those individually, converting all the extension methods in
                // each partial part to extension declarations.
                return fixAllSpans.Value
                    .Select(span => root.FindNode(span) as ClassDeclarationSyntax)
                    .WhereNotNull();
            }
            else
            {
                // Processing the whole file.  Return all top level classes in the file.
                return root
                    .DescendantNodes(descendIntoChildren: n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                    .OfType<ClassDeclarationSyntax>();
            }
        }
    }
}
