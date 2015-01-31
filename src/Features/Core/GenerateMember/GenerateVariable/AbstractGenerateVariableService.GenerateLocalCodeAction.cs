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
            private readonly TService service;
            private readonly Document document;
            private readonly State state;

            public GenerateLocalCodeAction(TService service, Document document, State state)
            {
                this.service = service;
                this.document = document;
                this.state = state;
            }

            public override string Title
            {
                get
                {
                    var text = FeaturesResources.GenerateLocal;

                    return string.Format(
                        text,
                        state.IdentifierToken.ValueText);
                }
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newRoot = GetNewRoot(cancellationToken);
                var newDocument = document.WithSyntaxRoot(newRoot);

                return Task.FromResult(newDocument);
            }

            private SyntaxNode GetNewRoot(CancellationToken cancellationToken)
            {
                SyntaxNode newRoot;
                if (service.TryConvertToLocalDeclaration(state.LocalType, state.IdentifierToken, document.Project.Solution.Workspace.Options, out newRoot))
                {
                    return newRoot;
                }

                var syntaxFactory = document.GetLanguageService<SyntaxGenerator>();
                var initializer = state.IsOnlyWrittenTo
                    ? null
                    : syntaxFactory.DefaultExpression(state.LocalType);

                var type = state.LocalType;
                var localStatement = syntaxFactory.LocalDeclarationStatement(type, state.IdentifierToken.ValueText, initializer);
                localStatement = localStatement.WithAdditionalAnnotations(Formatter.Annotation);

                var codeGenService = document.GetLanguageService<ICodeGenerationService>();
                var root = state.IdentifierToken.GetAncestors<SyntaxNode>().Last();

                return codeGenService.AddStatements(
                    root,
                    SpecializedCollections.SingletonEnumerable(localStatement),
                    options: new CodeGenerationOptions(beforeThisLocation: state.IdentifierToken.GetLocation()));
            }
        }
    }
}
