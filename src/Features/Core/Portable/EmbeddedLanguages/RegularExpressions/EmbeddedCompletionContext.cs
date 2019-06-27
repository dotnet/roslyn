// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal partial class RegexEmbeddedCompletionProvider
    {
        private readonly struct EmbeddedCompletionContext
        {
            private readonly RegexEmbeddedLanguageFeatures _language;
            private readonly CompletionContext _context;
            private readonly HashSet<string> _names;

            public readonly RegexTree Tree;
            public readonly SyntaxToken StringToken;
            public readonly int Position;
            public readonly CompletionTrigger Trigger;
            public readonly List<RegexItem> Items;

            public EmbeddedCompletionContext(
                RegexEmbeddedLanguageFeatures language,
                CompletionContext context,
                RegexTree tree,
                SyntaxToken stringToken)
            {
                _language = language;
                _context = context;
                _names = new HashSet<string>();
                Tree = tree;
                StringToken = stringToken;
                Position = _context.Position;
                Trigger = _context.Trigger;
                Items = new List<RegexItem>();
            }

            public void AddIfMissing(
                string displayText, string suffix, string description,
                RegexNode parentOpt, int? positionOffset = null, string insertionText = null)
            {
                var replacementStart = parentOpt != null
                    ? parentOpt.GetSpan().Start
                    : Position;

                var replacementSpan = TextSpan.FromBounds(replacementStart, Position);
                var newPosition = replacementStart + positionOffset;

                insertionText ??= displayText;
                var escapedInsertionText = _language.EscapeText(insertionText, StringToken);

                if (escapedInsertionText != insertionText)
                {
                    newPosition += escapedInsertionText.Length - insertionText.Length;
                }

                AddIfMissing(new RegexItem(
                    displayText, suffix, description,
                    CompletionChange.Create(
                        new TextChange(replacementSpan, escapedInsertionText),
                        newPosition)));
            }

            public void AddIfMissing(RegexItem item)
            {
                if (_names.Add(item.DisplayText))
                {
                    Items.Add(item);
                }
            }
        }
    }
}
