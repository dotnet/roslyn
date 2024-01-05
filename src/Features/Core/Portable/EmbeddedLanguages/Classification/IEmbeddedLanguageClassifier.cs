// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IEmbeddedLanguageClassifier : IEmbeddedLanguageFeatureService
    {
        /// <summary>
        /// This method will be called for all string and character tokens in a file to determine if there are special
        /// embedded language strings to classify.
        /// </summary>
        void RegisterClassifications(EmbeddedLanguageClassificationContext context);
    }
}
