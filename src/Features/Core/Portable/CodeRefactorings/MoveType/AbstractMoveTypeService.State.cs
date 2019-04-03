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
            public bool IsSelectionOnTypeHeader { get; set; }

            private State(SemanticDocument document)
            {
                this.SemanticDocument = document;
            }

            internal static State Generate(
                SemanticDocument document, TextSpan textSpan,
                TTypeDeclarationSyntax typeDeclaration, bool isSelectionOnTypeHeader,
                CancellationToken cancellationToken)
            {
                var state = new State(document);
                if (!state.TryInitialize(textSpan, typeDeclaration, isSelectionOnTypeHeader, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                TextSpan textSpan,
                TTypeDeclarationSyntax typeDeclaration,
                bool isSelectionOnTypeHeader,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var tree = this.SemanticDocument.SyntaxTree;
                var root = this.SemanticDocument.Root;
                var syntaxFacts = this.SemanticDocument.Document.GetLanguageService<ISyntaxFactsService>();

                var typeSymbol = this.SemanticDocument.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;

                // compiler declared types, anonymous types, types defined in metadata should be filtered out.
                if (typeSymbol == null ||
                    typeSymbol.Locations.Any(loc => loc.IsInMetadata) ||
                    typeSymbol.IsAnonymousType ||
                    typeSymbol.IsImplicitlyDeclared)
                {
                    return false;
                }

                TypeNode = typeDeclaration;
                TypeName = typeSymbol.Name;
                IsSelectionOnTypeHeader = isSelectionOnTypeHeader;
                DocumentNameWithoutExtension = Path.GetFileNameWithoutExtension(this.SemanticDocument.Document.Name);
                IsDocumentNameAValidIdentifier = syntaxFacts.IsValidIdentifier(DocumentNameWithoutExtension);

                return true;
            }
        }
    }
}
