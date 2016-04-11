// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        protected class State
        {
            public SemanticDocument Document { get; }
            public string DocumentName { get; set; }

            public bool IsNestedType { get; private set; }
            public bool TypeNameMatchesFileName { get; set; }
            public bool MakeTypePartial { get; set; }
            public bool MakeContainingTypePartial { get; set; }
            public bool OnlyTypeInFile { get; set; }
            public bool TargetFileNameAlreadyExists { get; set; }
            public string TargetFileNameCandidate { get; set; }
            public string TargetFileExtension { get; set; }
            public INamedTypeSymbol TypeSymbol { get; set; } //BalajiK: This can be removed.
            public TTypeDeclarationSyntax TypeNode { get; set;}

            private State(SemanticDocument document)
            {
                this.Document = document;
            }

            internal static State Generate(SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken)
            {
                var state = new State(document);
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

                var tree = this.Document.SyntaxTree;
                var root = this.Document.Root;
                var syntaxFacts = this.Document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                var typeDeclaration = root.FindNode(textSpan) as TTypeDeclarationSyntax;
                if (typeDeclaration == null)
                {
                    return false;
                }

                var typeSymbol = this.Document.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;

                // compiler declared types, anonymous types, types defined in metadata should be filtered out.
                if (typeSymbol == null ||
                    typeSymbol.Locations.Any(loc => loc.IsInMetadata) ||
                    typeSymbol.IsAnonymousType ||
                    typeSymbol.IsImplicitlyDeclared)
                {
                    return false;
                }

                TypeNode = typeDeclaration;
                TypeSymbol = typeSymbol;

                IsNestedType = typeDeclaration.Parent is TTypeDeclarationSyntax;
                OnlyTypeInFile = this.Document.Root.DescendantNodes().OfType<TTypeDeclarationSyntax>().Count() == 1;

                DocumentName = Path.GetFileNameWithoutExtension(this.Document.Document.Name);
                TypeNameMatchesFileName = string.Equals(DocumentName, typeSymbol.Name, StringComparison.CurrentCultureIgnoreCase);
                TargetFileNameCandidate = typeSymbol.Name;
                TargetFileExtension = this.Document.Document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb";

                if (!TypeNameMatchesFileName)
                {
                    var destinationDocumentId = DocumentId.CreateNewId(this.Document.Project.Id, TargetFileNameCandidate + TargetFileExtension);
                    TargetFileNameAlreadyExists = this.Document.Project.ContainsDocument(destinationDocumentId);
                }
                else
                {
                    TargetFileNameAlreadyExists = true;
                }

                return true;
            }
        }
    }
}