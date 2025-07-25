// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Shared.Extensions;

internal static class LanguageVersionExtensions
{
    extension(LanguageVersion languageVersion)
    {
        public bool IsCSharp14OrAbove()
        => languageVersion >= LanguageVersion.Preview;

        public bool IsCSharp13OrAbove()
            => languageVersion >= LanguageVersion.CSharp13;

        public bool IsCSharp12OrAbove()
            => languageVersion >= LanguageVersion.CSharp12;

        public bool IsCSharp11OrAbove()
            => languageVersion >= LanguageVersion.CSharp11;

        public bool HasConstantInterpolatedStrings()
            => languageVersion >= LanguageVersion.CSharp10;

        public bool SupportsCollectionExpressions()
            => languageVersion.IsCSharp12OrAbove();

        public bool SupportsPrimaryConstructors()
            => languageVersion.IsCSharp12OrAbove();

        public bool SupportsExtensions()
            => languageVersion.IsCSharp14OrAbove();
    }

    /// <remarks>
    /// Corresponds to Microsoft.CodeAnalysis.CSharp.LanguageVersionFacts.CSharpNext.
    /// </remarks>
    internal const LanguageVersion CSharpNext = LanguageVersion.Preview;
}
