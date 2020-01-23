// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(LastBuiltInCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(EmbeddedLanguageCompletionProvider))]
    [Shared]
    internal sealed class LastBuiltInCompletionProvider : CompletionProvider
    {
        public override Task ProvideCompletionsAsync(CompletionContext context)
            => Task.CompletedTask;
    }
}
