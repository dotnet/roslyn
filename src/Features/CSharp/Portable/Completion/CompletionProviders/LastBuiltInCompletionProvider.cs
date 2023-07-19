// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    /// <summary>
    /// Provides a completion provider that always appears after all built-in completion providers. This completion
    /// provider does not provide any completions.
    /// </summary>
    [ExportCompletionProvider(nameof(LastBuiltInCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ReferenceDirectiveCompletionProvider))]
    [Shared]
    internal sealed class LastBuiltInCompletionProvider : CompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LastBuiltInCompletionProvider()
        {
        }

        public override Task ProvideCompletionsAsync(CompletionContext context)
            => Task.CompletedTask;
    }
}
