// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal abstract partial class AbstractIndentation<TSyntaxRoot>
        where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract IHeaderFacts HeaderFacts { get; }
        protected abstract ISyntaxFormatting SyntaxFormatting { get; }

        protected abstract AbstractFormattingRule GetSpecializedIndentationFormattingRule(FormattingOptions2.IndentStyle indentStyle);

        /// <summary>
        /// Returns <see langword="true"/> if the language specific <see
        /// cref="ISmartTokenFormatter"/> should be deferred to figure out indentation.  If so, it
        /// will be asked to <see cref="ISmartTokenFormatter.FormatTokenAsync"/> the resultant
        /// <paramref name="token"/> provided by this method.
        /// </summary>
        protected abstract bool ShouldUseTokenIndenter(Indenter indenter, out SyntaxToken token);
        protected abstract ISmartTokenFormatter CreateSmartTokenFormatter(
            TSyntaxRoot root, TextLine lineToBeIndented, IndentationOptions options, AbstractFormattingRule baseFormattingRule);

        protected abstract IndentationResult? GetDesiredIndentationWorker(
            Indenter indenter, SyntaxToken? token, SyntaxTrivia? trivia);
    }
}
