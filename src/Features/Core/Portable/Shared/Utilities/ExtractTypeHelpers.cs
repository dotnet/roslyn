// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class ExtractTypeHelpers
    {
        public static async Task<Document> AddTypeToExistingFileAsync(Document document, INamedTypeSymbol newType, AnnotatedSymbolMapping symbolMapping, CancellationToken cancellationToken)
        {
            var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclaration = originalRoot.GetAnnotatedNodes(symbolMapping.TypeNodeAnnotation).Single();
            var editor = new SyntaxEditor(originalRoot, symbolMapping.AnnotatedSolution.Workspace);

            var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var newTypeNode = codeGenService.CreateNamedTypeDeclaration(newType, cancellationToken: cancellationToken)
                .WithAdditionalAnnotations(SimplificationHelpers.SimplifyModuleNameAnnotation);

            editor.InsertBefore(typeDeclaration, newTypeNode);

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        public static async Task<Document> AddTypeToNewFileAsync(
            Solution solution,
            string containingNamespaceDisplay,
            string fileName,
            ProjectId projectId,
            IEnumerable<string> folders,
            INamedTypeSymbol newSymbol,
            ImmutableArray<SyntaxTrivia> fileBanner,
            CancellationToken cancellationToken)
        {
            var newDocumentId = DocumentId.CreateNewId(projectId, debugName: fileName);
            var solutionWithInterfaceDocument = solution.AddDocument(newDocumentId, fileName, text: "", folders: folders);
            var newDocument = solutionWithInterfaceDocument.GetRequiredDocument(newDocumentId);
            var newSemanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var namespaceParts = containingNamespaceDisplay.Split('.').Where(s => !string.IsNullOrEmpty(s));
            var newTypeDocument = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync(
                newDocument.Project.Solution,
                newSemanticModel.GetEnclosingNamespace(0, cancellationToken),
                newSymbol.GenerateRootNamespaceOrType(namespaceParts.ToArray()),
                options: new CodeGenerationOptions(contextLocation: newSemanticModel.SyntaxTree.GetLocation(new TextSpan()), generateMethodBodies: true),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var syntaxRoot = await newTypeDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rootWithBanner = syntaxRoot.WithPrependedLeadingTrivia(fileBanner);

            newTypeDocument = newTypeDocument.WithSyntaxRoot(rootWithBanner);

            var simplified = await Simplifier.ReduceAsync(newTypeDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await Formatter.FormatAsync(simplified).ConfigureAwait(false);
        }
    }
}
