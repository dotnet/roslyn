﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
