// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static class TextLineExtensions
    {
        /// <summary>
        /// Determines whether the specified line is empty or contains whitespace only.
        /// </summary>
        public static bool IsEmptyOrWhitespace(this TextLine line)
        {
            var text = line.Text;
            RoslynDebug.Assert(text is object);
            for (var i = line.Span.Start; i < line.Span.End; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
