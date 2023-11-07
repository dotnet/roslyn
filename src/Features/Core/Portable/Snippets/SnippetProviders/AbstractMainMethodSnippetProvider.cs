// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractMainMethodSnippetProvider : AbstractSingleChangeSnippetProvider
    {
        protected abstract SyntaxNode GenerateReturnType(SyntaxGenerator generator);

        protected abstract IEnumerable<SyntaxNode> GenerateInnerStatements(SyntaxGenerator generator);

        protected override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var method = generator.MethodDeclaration(
                name: WellKnownMemberNames.EntryPointMethodName,
                parameters: SpecializedCollections.SingletonEnumerable(generator.ParameterDeclaration(
                    name: "args",
                    type: generator.ArrayTypeExpression(generator.TypeExpression(SpecialType.System_String)))),
                returnType: GenerateReturnType(generator),
                modifiers: DeclarationModifiers.Static,
                statements: GenerateInnerStatements(generator));

            return Task.FromResult(new TextChange(TextSpan.FromBounds(position, position), method.NormalizeWhitespace().ToFullString()));
        }

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
            => ImmutableArray<SnippetPlaceholder>.Empty;

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
            => syntaxFacts.IsMethodDeclaration;
    }
}
