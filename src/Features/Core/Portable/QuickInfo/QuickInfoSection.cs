// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Sections are used to make up a <see cref="QuickInfoItem"/>.
    /// </summary>
    public sealed class QuickInfoSection
    {
        /// <summary>
        /// The kind of this section. Use <see cref="QuickInfoSectionKinds"/> for the most common kinds.
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// The individual tagged parts of this section.
        /// </summary>
        public ImmutableArray<TaggedText> TaggedParts { get; }

        private QuickInfoSection(string kind, ImmutableArray<TaggedText> taggedParts)
        {
            Kind = kind ?? string.Empty;
            TaggedParts = taggedParts.NullToEmpty();
        }

        /// <summary>
        /// Creates a new instance of <see cref="QuickInfoSection"/>.
        /// </summary>
        /// <param name="kind">The kind of the section. Use <see cref="QuickInfoSectionKinds"/> for the most common kinds.</param>
        /// <param name="taggedParts">The individual tagged parts of the section.</param>
        public static QuickInfoSection Create(string kind, ImmutableArray<TaggedText> taggedParts)
        {
            return new QuickInfoSection(kind, taggedParts);
        }

        private string _text;

        /// <summary>
        /// The text of the section without tags.
        /// </summary>
        public string Text
        {
            get
            {
                if (_text == null)
                {
                    if (TaggedParts.Length == 0)
                    {
                        _text = string.Empty;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _text, string.Concat(TaggedParts.Select(t => t.Text)), null);
                    }
                }

                return _text;
            }
        }
    }
}
