//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Immutable structure containting information about an edit to an IText source.
    /// 
    /// This data in information is not normalized with respect to other edits.  
    /// </summary>
    public struct TextReplace
    {
        private readonly IText text;
        private readonly TextSpan oldSpan;
        private readonly string newText;

        /// <summary>
        /// The IText instance to which this edit was applied
        /// </summary>
        public IText Text
        {
            get { return text; }
        }

        /// <summary>
        /// Span in <see cref="Text" /> which was replaced
        /// </summary>
        public TextSpan OldSpan
        {
            get { return oldSpan; }
        }

        /// <summary>
        /// The new text which was added at the <see cref="OldSpan"/> span
        /// </summary>
        public string NewText
        {
            get { return newText; }
        }

        /// <summary>
        /// Create a new TextReplace structure over the specified <paramref name="oldSpan"/> and having
        /// the text <paramref name="newText"/>
        /// <param name="oldSpan">Span in the previous version of IText where the replace is occuring</param>
        /// <param name="newText">Text being added to the IText</param>
        /// <param name="text">Version of the IText on which the replace occurs</param>
        /// </summary>
        public TextReplace(IText text, TextSpan oldSpan, string newText)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (newText == null)
            {
                throw new ArgumentNullException("newText");
            }

            this.text = text;
            this.oldSpan = oldSpan;
            this.newText = newText;
        }

        public override string ToString()
        {
            return String.Format("{0}: -> {1}", oldSpan, newText);
        }
    }
}
