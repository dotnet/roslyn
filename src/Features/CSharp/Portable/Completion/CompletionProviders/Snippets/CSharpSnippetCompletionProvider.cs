// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;

[ExportCompletionProvider(nameof(CSharpSnippetCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(FunctionPointerUnmanagedCallingConventionCompletionProvider))]
[Shared]
internal class CSharpSnippetCompletionProvider : AbstractSnippetCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSnippetCompletionProvider()
    {
    }
}
