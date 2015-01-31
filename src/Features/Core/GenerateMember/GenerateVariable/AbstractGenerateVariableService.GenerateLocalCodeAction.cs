// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
{
    internal partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private class GenerateLocalCodeAction : CodeAction
        {
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;

            public GenerateLocalCodeAction(TService service, Document document, State state)
            {
                _service = service;
                _document = document;
                _state = state;
            }

            public override string Title
            {
                get
                {
                    var text = FeaturesResources.GenerateLocal;

                    return string.Format(
                        text,
                        _state.IdentifierToken.ValueText);
                }
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newRoot = GetNewRoot(cancellationToken);
                var newDocument = _document.WithSyntaxRoot(newRoot);

                return Task.FromResult(newDocument);
            }

            private SyntaxNode GetNewRoot(CancellationToken cancellationToken)
            {
                SyntaxNode newRoot;
                if (_service.TryConvertToLocalDeclaration(_state.LocalType, _state.IdentifierToken, _document.Project.Solution.Workspace.Options, out newRoot))
                {
                    return newRoot;
                }

                var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();
                var initializer = _state.IsOnlyWrittenTo
                    ? null
                    : syntaxFactory.DefaultExpression(_state.LocalType);

                var type = _state.LocalType;
                var localStatement = syntaxFactory.LocalDeclarationStatement(type, _state.IdentifierToken.ValueText, initializer);
                localStatement = localStatement.WithAdditionalAnnotations(Formatter.Annotation);

                var codeGenService = _document.GetLanguageService<ICodeGenerationService>();
                var root = _state.IdentifierToken.GetAncestors<SyntaxNode>().Last();

                return codeGenService.AddStatements(
                    root,
                    SpecializedCollections.SingletonEnumerable(localStatement),
                    options: new CodeGenerationOptions(beforeThisLocation: _state.IdentifierToken.GetLocation()),
                    cancellationToken: cancellationToken);
            }
        }
    }
}
