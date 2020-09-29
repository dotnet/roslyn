// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Holds information required to display and navigate to individual references
    /// </summary>
    [DataContract]
    internal sealed class ReferenceLocationDescriptor
    {
        /// <summary>
        /// Fully qualified name of the symbol containing the reference location
        /// </summary>
        [DataMember(Order = 0)]
        public string LongDescription { get; }

        /// <summary>
        /// Language of the reference location
        /// </summary>
        [DataMember(Order = 1)]
        public string Language { get; }

        /// <summary>
        /// The kind of symbol containing the reference location (such as type, method, property, etc.)
        /// </summary>
        [DataMember(Order = 2)]
        public Glyph? Glyph { get; }

        /// <summary>
        /// Reference's span start based on the document content
        /// </summary>
        [DataMember(Order = 3)]
        public int SpanStart { get; }

        /// <summary>
        /// Reference's span length based on the document content
        /// </summary>
        [DataMember(Order = 4)]
        public int SpanLength { get; }

        /// <summary>
        /// Reference's line based on the document content
        /// </summary>
        [DataMember(Order = 5)]
        public int LineNumber { get; }

        /// <summary>
        /// Reference's character based on the document content
        /// </summary>
        [DataMember(Order = 6)]
        public int ColumnNumber { get; }

        [DataMember(Order = 7)]
        public Guid ProjectGuid { get; }

        [DataMember(Order = 8)]
        public Guid DocumentGuid { get; }

        /// <summary>
        /// Document's file path
        /// </summary>
        [DataMember(Order = 9)]
        public string FilePath { get; }

        /// <summary>
        /// the full line of source that contained the reference
        /// </summary>
        [DataMember(Order = 10)]
        public string ReferenceLineText { get; }

        /// <summary>
        /// the beginning of the span within reference text that was the use of the reference
        /// </summary>
        [DataMember(Order = 11)]
        public int ReferenceStart { get; }

        /// <summary>
        /// the length of the span of the reference
        /// </summary>
        [DataMember(Order = 12)]
        public int ReferenceLength { get; }

        /// <summary>
        /// Text above the line with the reference
        /// </summary>
        [DataMember(Order = 13)]
        public string BeforeReferenceText1 { get; }

        /// <summary>
        /// Text above the line with the reference
        /// </summary>
        [DataMember(Order = 14)]
        public string BeforeReferenceText2 { get; }

        /// <summary>
        /// Text below the line with the reference
        /// </summary>
        [DataMember(Order = 15)]
        public string AfterReferenceText1 { get; }

        /// <summary>
        /// Text below the line with the reference
        /// </summary>
        [DataMember(Order = 16)]
        public string AfterReferenceText2 { get; }

        public ReferenceLocationDescriptor(
            string longDescription,
            string language,
            Glyph? glyph,
            int spanStart,
            int spanLength,
            int lineNumber,
            int columnNumber,
            Guid projectGuid,
            Guid documentGuid,
            string filePath,
            string referenceLineText,
            int referenceStart,
            int referenceLength,
            string beforeReferenceText1,
            string beforeReferenceText2,
            string afterReferenceText1,
            string afterReferenceText2)
        {
            LongDescription = longDescription;
            Language = language;
            Glyph = glyph;
            SpanStart = spanStart;
            SpanLength = spanLength;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            // We want to keep track of the location's document if it comes from a file in your solution.
            ProjectGuid = projectGuid;
            DocumentGuid = documentGuid;
            FilePath = filePath;
            ReferenceLineText = referenceLineText;
            ReferenceStart = referenceStart;
            ReferenceLength = referenceLength;
            BeforeReferenceText1 = beforeReferenceText1;
            BeforeReferenceText2 = beforeReferenceText2;
            AfterReferenceText1 = afterReferenceText1;
            AfterReferenceText2 = afterReferenceText2;
        }
    }
}
