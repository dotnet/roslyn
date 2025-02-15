// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
{
    private sealed class State
    {
        private readonly TService _service;

        public SemanticDocument SemanticDocument { get; }

        public TTypeDeclarationSyntax TypeNode { get; set; } = null!;
        public string DocumentNameWithoutExtension { get; set; } = null!;
        public bool IsDocumentNameAValidIdentifier { get; set; }

        public string TypeName => _service.GetDeclaredSymbolName(this.TypeNode);

        private State(TService service, SemanticDocument document)
        {
            _service = service;
            SemanticDocument = document;
        }

        internal static State? Generate(
            TService service, SemanticDocument document, TTypeDeclarationSyntax typeDeclaration)
        {
            var state = new State(service, document);
            return state.TryInitialize(typeDeclaration) ? state : null;
        }

        private bool TryInitialize(TTypeDeclarationSyntax typeDeclaration)
        {
            TypeNode = typeDeclaration;

            // compiler declared types, anonymous types, types defined in metadata should be filtered out.
            var typeName = this.TypeName;
            if (typeName == string.Empty)
                return false;

            var syntaxFacts = SemanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            DocumentNameWithoutExtension = Path.GetFileNameWithoutExtension(SemanticDocument.Document.Name);
            IsDocumentNameAValidIdentifier = syntaxFacts.IsValidIdentifier(DocumentNameWithoutExtension);

            return true;
        }
    }
}
