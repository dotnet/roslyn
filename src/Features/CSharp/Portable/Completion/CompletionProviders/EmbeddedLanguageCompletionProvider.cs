// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
