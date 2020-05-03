// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEmbeddedLanguage
    {
        /// <summary>
        /// A optional classifier that can produce <see cref="ClassifiedSpan"/>s for an embedded language string.
        /// </summary>
        ISyntaxClassifier Classifier { get; }
    }
}
