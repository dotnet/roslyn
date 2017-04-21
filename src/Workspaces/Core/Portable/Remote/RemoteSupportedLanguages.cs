// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteSupportedLanguages
    {
        public static bool IsSupported(this string language)
        {
            return language == LanguageNames.CSharp ||
                   language == LanguageNames.VisualBasic;
        }
    }
}
