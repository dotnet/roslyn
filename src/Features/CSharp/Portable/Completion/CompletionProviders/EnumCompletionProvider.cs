// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(EnumCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ObjectAndWithInitializerCompletionProvider))]
    [Shared]
    internal class EnumCompletionProvider : AbstractEnumCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EnumCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            var ch = text[characterPosition];
            return ch is ' ' or '(' or '=' or ':' ||
                (SyntaxFacts.IsIdentifierStartCharacter(ch) && options.GetOption(CompletionOptions.TriggerOnTypingLetters2, LanguageNames.CSharp));
        }

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create(' ', '(', '=', ':');

        protected override async Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }

        protected override string GetInsertionText(CompletionItem item, char ch)
            => SymbolCompletionItem.GetInsertionText(item);

        protected override (string displayText, string suffix, string insertionText) GetDefaultDisplayAndSuffixAndInsertionText(ISymbol symbol,
            SyntaxContext context)
            => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);
    }
}
