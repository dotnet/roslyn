// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal static class FSharpCommonCompletionUtilities
    {
        public static bool IsStartingNewWord(SourceText text, int characterPosition, Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter)
        {
            return CommonCompletionUtilities.IsStartingNewWord(text, characterPosition, isWordStartCharacter, isWordCharacter);
        }
    }
}
