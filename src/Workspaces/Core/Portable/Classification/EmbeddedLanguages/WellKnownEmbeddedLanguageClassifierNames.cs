// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class WellKnownEmbeddedLanguageClassifierNames
    {
        /// <summary>
        /// A special built-in classifier for classifying escapes in strings and character literals if no other embedded
        /// language classifiers handle it.
        /// </summary>
        public const string Fallback = nameof(Fallback);

        public const string Regex = nameof(Regex);

        public const string Json = nameof(Json);
    }
}
