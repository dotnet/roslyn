// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Remote;

internal static class RemoteSupportedLanguages
{
    extension([NotNullWhen(true)] string? language)
    {
        public bool IsSupported()
        => language is LanguageNames.CSharp or LanguageNames.VisualBasic;
    }
}
