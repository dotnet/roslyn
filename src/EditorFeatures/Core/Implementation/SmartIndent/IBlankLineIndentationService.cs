// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IBlankLineIndentationService
    {
        /// <summary>
        /// Determines indentation for a blank line (i.e. after hitting enter at the end of a line,
        /// or after moving to a blank line).
        /// 
        /// Specifically, this function operates as if the line specified by <paramref name="lineNumber"/>
        /// is blank.  The actual contents of the line do not matter.  All indentation information is
        /// determined from the previous lines in the document.
        /// 
        /// This function will always succeed.
        /// </summary>
        IndentationResult GetBlankLineIndentation(
            Document document, int lineNumber, FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken);
    }
}
