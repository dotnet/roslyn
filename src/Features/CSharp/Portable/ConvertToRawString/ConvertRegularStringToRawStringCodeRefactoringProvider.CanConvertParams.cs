// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString
{
    internal partial class ConvertRegularStringToRawStringCodeRefactoringProvider
    {
        private readonly struct CanConvertParams
        {
            public CanConvertParams(VirtualCharSequence characters, bool canBeSingleLine, bool canBeMultiLineWithoutLeadingWhiteSpaces)
            {
                Characters = characters;
                CanBeSingleLine = canBeSingleLine;
                CanBeMultiLineWithoutLeadingWhiteSpaces = canBeMultiLineWithoutLeadingWhiteSpaces;
            }

            public VirtualCharSequence Characters { get; }
            public bool CanBeSingleLine { get; }
            public bool CanBeMultiLineWithoutLeadingWhiteSpaces { get; }
        }
    }
}
