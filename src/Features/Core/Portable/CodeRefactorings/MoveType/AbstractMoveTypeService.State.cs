// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class State
        {
            public SemanticDocument SemanticDocument { get; }

            public TTypeDeclarationSyntax TypeNode { get; set; }
            public string TypeName { get; set; }
            public string DocumentNameWithoutExtension { get; set; }
            public bool IsDocumentNameAValidIdentifier { get; set; }

            private State(SemanticDocument document)
            {
                SemanticDocument = document;
            }

            internal static State Generate(
                SemanticDocument document, TTypeDeclarationSyntax typeDeclaration,
                CancellationToken cancellationToken)
            {
                var state = new State(document);
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
                if (!(SemanticDocument.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is INamedTypeSymbol typeSymbol) ||
                    typeSymbol.Locations.Any(loc => loc.IsInMetadata) ||
                    typeSymbol.IsAnonymousType ||
                    typeSymbol.IsImplicitlyDeclared)
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
}
