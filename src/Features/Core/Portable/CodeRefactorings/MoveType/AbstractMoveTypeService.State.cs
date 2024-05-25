// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
{
    private sealed class State
    {
        public SemanticDocument SemanticDocument { get; }
        public CodeCleanupOptionsProvider FallbackOptions { get; }

        public TTypeDeclarationSyntax TypeNode { get; set; }
        public string TypeName { get; set; }
        public string DocumentNameWithoutExtension { get; set; }
        public bool IsDocumentNameAValidIdentifier { get; set; }

        private State(SemanticDocument document, CodeCleanupOptionsProvider fallbackOptions)
        {
            SemanticDocument = document;
            FallbackOptions = fallbackOptions;
        }

        internal static State Generate(
            SemanticDocument document, TTypeDeclarationSyntax typeDeclaration, CodeCleanupOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var state = new State(document, fallbackOptions);
            if (!state.TryInitialize(typeDeclaration, cancellationToken))
            {
                return null;
            }

            return state;
        }

        private bool TryInitialize(
            TTypeDeclarationSyntax typeDeclaration,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var tree = SemanticDocument.SyntaxTree;
            var root = SemanticDocument.Root;
            var syntaxFacts = SemanticDocument.Document.GetLanguageService<ISyntaxFactsService>();

            // compiler declared types, anonymous types, types defined in metadata should be filtered out.
            if (SemanticDocument.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol typeSymbol ||
                typeSymbol.Locations.Any(static loc => loc.IsInMetadata) ||
                typeSymbol.IsAnonymousType ||
                typeSymbol.IsImplicitlyDeclared ||
                typeSymbol.Name == string.Empty)
            {
                return false;
            }

            TypeNode = typeDeclaration;
            TypeName = typeSymbol.Name;
            DocumentNameWithoutExtension = Path.GetFileNameWithoutExtension(SemanticDocument.Document.Name);
            IsDocumentNameAValidIdentifier = syntaxFacts.IsValidIdentifier(DocumentNameWithoutExtension);

            return true;
        }
    }
}
