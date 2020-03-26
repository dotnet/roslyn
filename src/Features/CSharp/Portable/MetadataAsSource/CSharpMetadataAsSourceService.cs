// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource
{
    internal partial class CSharpMetadataAsSourceService : AbstractMetadataAsSourceService
    {
        public CSharpMetadataAsSourceService(HostLanguageServices languageServices)
            : base(languageServices.GetService<ICodeGenerationService>())
        {
        }

        protected override async Task<Document> AddAssemblyInfoRegionAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken)
        {
            var assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
            var assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(symbolCompilation, symbol.ContainingAssembly);

            var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
                .WithTrailingTrivia(new[] { SyntaxFactory.Space, SyntaxFactory.PreprocessingMessage(assemblyInfo) });

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.WithLeadingTrivia(new[]
                {
                    SyntaxFactory.Trivia(regionTrivia),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("// " + assemblyPath),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia(true)),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.CarriageReturnLineFeed
                });

            return document.WithSyntaxRoot(newRoot);
        }

        protected override IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document)
            => CSharpMetadataFormattingRule.Instance.Concat(Formatter.GetDefaultFormattingRules(document));

        protected override async Task<Document> ConvertDocCommentsToRegularCommentsAsync(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken);

            return document.WithSyntaxRoot(newSyntaxRoot);
        }

        protected override ImmutableArray<AbstractReducer> GetReducers()
            => ImmutableArray.Create<AbstractReducer>(
                new CSharpNameReducer(),
                new CSharpEscapingReducer(),
                new CSharpParenthesesReducer(),
                new CSharpDefaultExpressionReducer());
    }
}
