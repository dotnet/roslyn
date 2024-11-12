// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages;

/// <summary>
/// Service that returns all the embedded languages supported.  Each embedded language can expose
/// individual language services through the <see cref="IEmbeddedLanguage"/> interface.
/// </summary>
internal interface IEmbeddedLanguagesProvider : ILanguageService
{
    EmbeddedLanguageInfo EmbeddedLanguageInfo { get; }
    ImmutableArray<IEmbeddedLanguage> Languages { get; }
}
