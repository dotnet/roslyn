// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpSwitchSnippetProvider() : AbstractSwitchSnippetProvider<SwitchStatementSyntax>
    {
        public override string Identifier => CSharpSnippetIdentifiers.Switch;

        public override string Description => CSharpFeaturesResources.switch_statement;

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SwitchStatementSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var expression = node.Expression;
            return [new SnippetPlaceholder(expression.ToString(), expression.SpanStart)];
        }

        protected override int GetTargetCaretPosition(SwitchStatementSyntax switchStatement, SourceText sourceText)
        {
            var triviaSpan = switchStatement.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }
    }
}
