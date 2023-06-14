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
    internal sealed class ReferenceLocationDescriptor(
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
        /// <summary>
        /// Fully qualified name of the symbol containing the reference location
        /// </summary>
        [DataMember(Order = 0)]
        public string LongDescription { get; } = longDescription;

        /// <summary>
        /// Language of the reference location
        /// </summary>
        [DataMember(Order = 1)]
        public string Language { get; } = language;

        /// <summary>
        /// The kind of symbol containing the reference location (such as type, method, property, etc.)
        /// </summary>
        [DataMember(Order = 2)]
        public Glyph? Glyph { get; } = glyph;

        /// <summary>
        /// Reference's span start based on the document content
        /// </summary>
        [DataMember(Order = 3)]
        public int SpanStart { get; } = spanStart;

        /// <summary>
        /// Reference's span length based on the document content
        /// </summary>
        [DataMember(Order = 4)]
        public int SpanLength { get; } = spanLength;

        /// <summary>
        /// Reference's line based on the document content
        /// </summary>
        [DataMember(Order = 5)]
        public int LineNumber { get; } = lineNumber;

        /// <summary>
        /// Reference's character based on the document content
        /// </summary>
        [DataMember(Order = 6)]
        public int ColumnNumber { get; } = columnNumber;

        [DataMember(Order = 7)]
        public Guid ProjectGuid { get; } = projectGuid;

        [DataMember(Order = 8)]
        public Guid DocumentGuid { get; } = documentGuid;

        /// <summary>
        /// Document's file path
        /// </summary>
        [DataMember(Order = 9)]
        public string FilePath { get; } = filePath;

        /// <summary>
        /// the full line of source that contained the reference
        /// </summary>
        [DataMember(Order = 10)]
        public string ReferenceLineText { get; } = referenceLineText;

        /// <summary>
        /// the beginning of the span within reference text that was the use of the reference
        /// </summary>
        [DataMember(Order = 11)]
        public int ReferenceStart { get; } = referenceStart;

        /// <summary>
        /// the length of the span of the reference
        /// </summary>
        [DataMember(Order = 12)]
        public int ReferenceLength { get; } = referenceLength;

        /// <summary>
        /// Text above the line with the reference
        /// </summary>
        [DataMember(Order = 13)]
        public string BeforeReferenceText1 { get; } = beforeReferenceText1;

        /// <summary>
        /// Text above the line with the reference
        /// </summary>
        [DataMember(Order = 14)]
        public string BeforeReferenceText2 { get; } = beforeReferenceText2;

        /// <summary>
        /// Text below the line with the reference
        /// </summary>
        [DataMember(Order = 15)]
        public string AfterReferenceText1 { get; } = afterReferenceText1;

        /// <summary>
        /// Text below the line with the reference
        /// </summary>
        [DataMember(Order = 16)]
        public string AfterReferenceText2 { get; } = afterReferenceText2;
    }
}
