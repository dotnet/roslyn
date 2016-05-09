// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A piece of text with a descriptive tag.
    /// </summary>
    public struct TaggedText
    {
        /// <summary>
        /// A descriptive tag from <see cref="TextTags"/>.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// The actual text to be displayed.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates a new instance of <see cref="TaggedText"/>
        /// </summary>
        /// <param name="tag">A descriptive tag from <see cref="TextTags"/>.</param>
        /// <param name="text">The actual text to be displayed.</param>
        public TaggedText(string tag, string text)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            this.Tag = tag;
            this.Text = text;
        }

        public override string ToString()
        {
            return this.Text;
        }
    }
}