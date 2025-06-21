// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
