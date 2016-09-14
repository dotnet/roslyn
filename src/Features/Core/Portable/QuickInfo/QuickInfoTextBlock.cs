// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class QuickInfoTextBlock
    {
        public string Kind { get; }
        public ImmutableArray<TaggedText> Text { get; }

        private QuickInfoTextBlock(string kind, ImmutableArray<TaggedText> text)
        {
            this.Kind = kind ?? string.Empty;
            this.Text = text.IsDefault ? ImmutableArray<TaggedText>.Empty : text;
        }

        /// <summary>
        /// Creates a new instance of <see cref="QuickInfoTextBlock"/>.
        /// </summary>
        /// <param name="kind">The kind of the text. Use <see cref="QuickInfoTextKinds"/> for the most common kinds.</param>
        /// <param name="text">The text</param>
        public static QuickInfoTextBlock Create(string kind, ImmutableArray<TaggedText> text)
        {
            return new QuickInfoTextBlock(kind, text);
        }

        private string _rawText;

        public string RawText
        {
            get
            {
                if (_rawText == null)
                {
                    if (this.Text.Length == 0)
                    {
                        _rawText = string.Empty;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _rawText, string.Concat(this.Text.Select(t => t.Text)), null);
                    }
                }

                return _rawText;
            }
        }
    }
}