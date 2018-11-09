// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEmbeddedLanguageEditorFeatures : IEmbeddedLanguageFeatures
    {
        /// <summary>
        /// A optional brace matcher that can match braces in an embedded language string.
        /// </summary>
        IBraceMatcher BraceMatcher { get; }
    }
}
