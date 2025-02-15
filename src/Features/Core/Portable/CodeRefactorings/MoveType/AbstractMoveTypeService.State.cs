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
        public SemanticDocument SemanticDocument { get; }

        public TTypeDeclarationSyntax TypeNode { get; }
        public string DocumentNameWithoutExtension { get; }
        public bool IsDocumentNameAValidIdentifier { get; }

        private State(SemanticDocument document, TTypeDeclarationSyntax typeNode)
        {
            SemanticDocument = document;
            TypeNode = typeNode;

            DocumentNameWithoutExtension = Path.GetFileNameWithoutExtension(SemanticDocument.Document.Name);

            var syntaxFacts = SemanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            IsDocumentNameAValidIdentifier = syntaxFacts.IsValidIdentifier(DocumentNameWithoutExtension);
        }

        public static State? Generate(TService service, SemanticDocument document, TTypeDeclarationSyntax typeDeclaration)
            => service.GetSymbolName(typeDeclaration) is "" ? null : new State(document, typeDeclaration);
    }
}
