// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(LastBuiltInCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExtensionMethodImportCompletionProvider))]
    [Shared]
    internal sealed class LastBuiltInCompletionProvider : CompletionProvider
    {
        public override Task ProvideCompletionsAsync(CompletionContext context)
            => Task.CompletedTask;
    }
}
