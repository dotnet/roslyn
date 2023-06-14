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
        private sealed class GenerateLocalCodeAction(TService service, Document document, State state, CodeGenerationOptionsProvider fallbackOptions) : CodeAction
        {
            public override string Title
            {
                get
                {
                    var text = FeaturesResources.Generate_local_0;

                    return string.Format(
                        text,
                        state.IdentifierToken.ValueText);
                }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var newRoot = await GetNewRootAsync(cancellationToken).ConfigureAwait(false);
                var newDocument = document.WithSyntaxRoot(newRoot);

                return newDocument;
            }

            private async Task<SyntaxNode> GetNewRootAsync(CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                if (service.TryConvertToLocalDeclaration(state.LocalType, state.IdentifierToken, semanticModel, cancellationToken, out var newRoot))
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

                var root = state.IdentifierToken.GetAncestors<SyntaxNode>().Last();
                var context = new CodeGenerationContext(beforeThisLocation: state.IdentifierToken.GetLocation());
                var info = await document.GetCodeGenerationInfoAsync(context, fallbackOptions, cancellationToken).ConfigureAwait(false);

                return info.Service.AddStatements(
                    root,
                    SpecializedCollections.SingletonEnumerable(localStatement),
                    info,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
