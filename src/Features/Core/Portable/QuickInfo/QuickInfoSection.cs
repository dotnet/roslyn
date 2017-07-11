// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class QuickInfoSection
    {
        public string Kind { get; }
        public ImmutableArray<TaggedText> TaggedParts { get; }

        private QuickInfoSection(string kind, ImmutableArray<TaggedText> taggedParts)
        {
            this.Kind = kind ?? string.Empty;
            this.TaggedParts = taggedParts.NullToEmpty();
        }

        /// <summary>
        /// Creates a new instance of <see cref="QuickInfoSection"/>.
        /// </summary>
        /// <param name="kind">The kind of the text. Use <see cref="QuickInfoSectionKinds"/> for the most common kinds.</param>
        /// <param name="taggedParts">The text</param>
        public static QuickInfoSection Create(string kind, ImmutableArray<TaggedText> taggedParts)
        {
            return new QuickInfoSection(kind, taggedParts);
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
