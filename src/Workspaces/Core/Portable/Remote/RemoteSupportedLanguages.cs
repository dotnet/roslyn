﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
