// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// Exception indicates the text span in string or comment of a syntax tree is being renamed to two different texts.
    /// </summary>
    internal class StringOrCommentReplacementTextConflictException : Exception
    {
        public TextSpan TextSpan { get; }
        public DocumentId DocumentId { get; }
        public string FirstReplacementText { get; }
        public string SecondReplacementText { get; }
        public override string Message => $"{TextSpan} of document: {DocumentId} is being renamed to {FirstReplacementText} and {SecondReplacementText}.";

        public StringOrCommentReplacementTextConflictException(
            TextSpan textSpan,
            DocumentId documentId,
            string firstReplacementText,
            string secondReplacementText)
        {
            TextSpan = textSpan;
            DocumentId = documentId;
            FirstReplacementText = firstReplacementText;
            SecondReplacementText = secondReplacementText;
        }
    }
}
