// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal sealed class DisplayInfo
    {
        public DisplayInfo(string longName,
                           string language,
                           Glyph? glyph,
                           string referenceLineText,
                           int referenceStart,
                           int referenceLength,
                           string beforeReferenceText1,
                           string beforeReferenceText2,
                           string afterReferenceText1,
                           string afterReferenceText2)
        {
            LongName = longName;
            Language = language;
            Glyph = glyph;
            ReferenceLineText = referenceLineText;
            ReferenceStart = referenceStart;
            ReferenceLength = referenceLength;
            BeforeReferenceText1 = beforeReferenceText1;
            BeforeReferenceText2 = beforeReferenceText2;
            AfterReferenceText1 = afterReferenceText1;
            AfterReferenceText2 = afterReferenceText2;
        }

        /// <summary>
        /// Fully qualified name of the symbol containing the reference location
        /// </summary>
        public string LongName { get; private set; }

        /// <summary>
        /// Language of the reference location
        /// </summary>
        public string Language { get; private set; }

        /// <summary>
        /// The kind of symbol containing the reference location (such as type, method, property, etc.)
        /// </summary>
        public Glyph? Glyph { get; private set; }

        /// <summary>
        /// the full line of source that contained the reference
        /// </summary>
        public string ReferenceLineText { get; private set; }

        /// <summary>
        /// the beginning of the span within reference text that was the use of the reference
        /// </summary>
        public int ReferenceStart { get; private set; }

        /// <summary>
        /// the length of the span of the reference
        /// </summary>
        public int ReferenceLength { get; private set; }

        /// <summary>
        /// Text above the line with the reference
        /// </summary>
        public string BeforeReferenceText1 { get; private set; }

        /// <summary>
        /// Text above the line with the reference
        /// </summary>
        public string BeforeReferenceText2 { get; private set; }

        /// <summary>
        /// Text below the line with the reference
        /// </summary>
        public string AfterReferenceText1 { get; private set; }

        /// <summary>
        /// Text below the line with the reference
        /// </summary>
        public string AfterReferenceText2 { get; private set; }
    }
}
