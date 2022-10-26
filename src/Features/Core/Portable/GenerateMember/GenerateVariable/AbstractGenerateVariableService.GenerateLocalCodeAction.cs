// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private sealed class GenerateLocalCodeAction : CodeAction
        {
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly CodeGenerationOptionsProvider _fallbackOptions;

            public GenerateLocalCodeAction(TService service, Document document, State state, CodeGenerationOptionsProvider fallbackOptions)
            {
                _service = service;
                _document = document;
                _state = state;
                _fallbackOptions = fallbackOptions;
            }

            public override string Title
            {
                get
                {
                    var text = FeaturesResources.Generate_local_0;

                    return string.Format(
                        text,
                        _state.IdentifierToken.ValueText);
                }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newRoot = await GetNewRootAsync(cancellationToken).ConfigureAwait(false);
                var newDocument = _document.WithSyntaxRoot(newRoot);

                return newDocument;
            }

            private async Task<SyntaxNode> GetNewRootAsync(CancellationToken cancellationToken)
            {
                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                if (_service.TryConvertToLocalDeclaration(_state.LocalType, _state.IdentifierToken, semanticModel, cancellationToken, out var newRoot))
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

                var options = await _document.GetCodeGenerationOptionsAsync(_fallbackOptions, cancellationToken).ConfigureAwait(false);

                var info = options.GetInfo(
                    new CodeGenerationContext(
                        beforeThisLocation: _state.IdentifierToken.GetLocation()),
                    _document.Project);

                return codeGenService.AddStatements(
                    root,
                    SpecializedCollections.SingletonEnumerable(localStatement),
                    info,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
