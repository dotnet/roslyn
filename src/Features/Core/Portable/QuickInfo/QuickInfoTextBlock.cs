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
        public ImmutableArray<TaggedText> TaggedParts { get; }

        private QuickInfoTextBlock(string kind, ImmutableArray<TaggedText> taggedParts)
        {
            this.Kind = kind ?? string.Empty;
            this.TaggedParts = taggedParts.NullToEmpty();
        }

        /// <summary>
        /// Creates a new instance of <see cref="QuickInfoTextBlock"/>.
        /// </summary>
        /// <param name="kind">The kind of the text. Use <see cref="QuickInfoTextKinds"/> for the most common kinds.</param>
        /// <param name="taggedParts">The text</param>
        public static QuickInfoTextBlock Create(string kind, ImmutableArray<TaggedText> taggedParts)
        {
            return new QuickInfoTextBlock(kind, taggedParts);
        }

        private string _text;

        public string Text
        {
            get
            {
                if (_text == null)
                {
                    if (this.TaggedParts.Length == 0)
                    {
                        _text = string.Empty;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _text, string.Concat(this.TaggedParts.Select(t => t.Text)), null);
                    }
                }

                return _text;
            }
        }
    }
}
