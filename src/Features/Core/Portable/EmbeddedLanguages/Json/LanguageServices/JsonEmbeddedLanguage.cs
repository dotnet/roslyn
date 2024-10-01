// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

internal sealed class JsonEmbeddedLanguage : IEmbeddedLanguage
{
    // No completion for embedded json currently.
    public EmbeddedLanguageCompletionProvider? CompletionProvider => null;
}
