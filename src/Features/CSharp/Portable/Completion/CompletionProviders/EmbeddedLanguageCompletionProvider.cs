﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(EmbeddedLanguageCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExtensionMethodImportCompletionProvider))]
    [Shared]
    internal class EmbeddedLanguageCompletionProvider : AbstractEmbeddedLanguageCompletionProvider
    {
        [ImportingConstructor]
        public EmbeddedLanguageCompletionProvider()
        {
        }
    }
}
