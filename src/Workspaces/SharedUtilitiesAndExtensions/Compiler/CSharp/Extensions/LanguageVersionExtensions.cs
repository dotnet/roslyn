// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Shared.Extensions
{
    internal static class LanguageVersionExtensions
    {
        // https://github.com/dotnet/roslyn/issues/43348
        //
        // This value should be removed when LanguageVersion.CSharp9 is available, and all callers should just
        // reference that constant instead.
        public const LanguageVersion CSharp9 = LanguageVersion.Preview;

        public static bool IsCSharp9OrAbove(this LanguageVersion languageVersion)
            => languageVersion >= CSharp9;
    }
}
