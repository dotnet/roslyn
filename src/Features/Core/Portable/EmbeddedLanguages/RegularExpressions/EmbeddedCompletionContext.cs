// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal partial class RegexEmbeddedCompletionProvider
    {
        private class EmbeddedCompletionContext
        {
            private readonly RegexEmbeddedCompletionProvider _provider;
            private readonly CompletionContext _context;

            public readonly List<RegexItem> Items = new List<RegexItem>();
            public readonly HashSet<string> Names = new HashSet<string>();

            public readonly RegexTree Tree;
            public readonly SyntaxToken StringToken;

            public EmbeddedCompletionContext(
                RegexEmbeddedCompletionProvider provider,
                CompletionContext context,
                RegexTree tree,
                SyntaxToken stringToken)
            {
                _provider = provider;
                _context = context;
                Tree = tree;
                StringToken = stringToken;
            }

            public int Position => _context.Position;
            public OptionSet Options => _context.Options;
            public Document Document => _context.Document;
            public CompletionTrigger Trigger => _context.Trigger;
            public CancellationToken CancellationToken => _context.CancellationToken;

            public void AddIfMissing(
                string displayText, string suffix, string description,
                RegexNode parentOpt, int? positionOffset = null, string insertionText = null)
            {
                var replacementStart = parentOpt != null
                    ? parentOpt.GetSpan().Start
                    : Position;

                var replacementSpan = TextSpan.FromBounds(replacementStart, Position);
                var item = _provider.CreateItem(
                    StringToken, displayText, suffix, description,
                    replacementSpan, positionOffset, insertionText);

                AddIfMissing(item);
            }

            public void AddIfMissing(RegexItem item)
            {
                if (this.Names.Add(item.DisplayText))
                {
                    this.Items.Add(item);
                }
            }
        }
    }
}
