// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class State
        {
            private readonly TService _service;
            public SemanticDocument SemanticDocument { get; }

            public TTypeDeclarationSyntax TypeNode { get; set; }
            public string TypeName { get; set; }
            public string DocumentName { get; set; }
            public bool IsDocumentNameAValidIdentifier { get; set; }

            private State(TService service, SemanticDocument document)
            {
                this._service = service;
                this.SemanticDocument = document;
            }

            internal static State Generate(TService service, SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken)
            {
                var state = new State(service, document);
                if (!state.TryInitialize(textSpan, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var tree = this.SemanticDocument.SyntaxTree;
                var root = this.SemanticDocument.Root;
                var syntaxFacts = this.SemanticDocument.Project.LanguageServices.GetService<ISyntaxFactsService>();

                var typeDeclaration = _service.GetNodeToAnalyze(root, textSpan) as TTypeDeclarationSyntax;
                if (typeDeclaration == null)
                {
                    return false;
                }

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
                DocumentName = Path.GetFileNameWithoutExtension(this.SemanticDocument.Document.Name);
                IsDocumentNameAValidIdentifier = syntaxFacts.IsValidIdentifier(DocumentName);

                // if type name matches document name, per style conventions, we have nothing to do.
                return !_service.TypeMatchesDocumentName(
                    TypeNode,
                    TypeName,
                    DocumentName,
                    SemanticDocument.SemanticModel,
                    cancellationToken);
            }
        }
    }
}