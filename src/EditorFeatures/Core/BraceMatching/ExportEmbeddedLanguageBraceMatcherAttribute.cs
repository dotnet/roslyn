// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    /// <summary>
    /// Use this attribute to export a <see cref="IEmbeddedLanguageBraceMatcher"/>.
    /// </summary>
    internal class ExportEmbeddedLanguageBraceMatcherAttribute : ExportEmbeddedLanguageFeatureServiceAttribute
    {
        public ExportEmbeddedLanguageBraceMatcherAttribute(
            string name, string language, params string[] identifiers)
            : this(name, language, supportsUnannotatedAPIs: false, identifiers)
        {
        }

        public ExportEmbeddedLanguageBraceMatcherAttribute(
            string name, string language, bool supportsUnannotatedAPIs, params string[] identifiers)
            : base(typeof(IEmbeddedLanguageBraceMatcher), name, language, supportsUnannotatedAPIs, identifiers)
        {
        }
    }
}
