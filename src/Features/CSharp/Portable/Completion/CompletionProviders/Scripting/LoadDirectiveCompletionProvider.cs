// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(LoadDirectiveCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(CSharpSnippetCompletionProvider))]
[Shared]
internal sealed class LoadDirectiveCompletionProvider : AbstractLoadDirectiveCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LoadDirectiveCompletionProvider()
    {
    }

    protected override string DirectiveName => "load";

    protected override bool TryGetCompletionPrefix(SyntaxTree tree, int position, [NotNullWhen(true)] out string? literalValue, out TextSpan textSpan, CancellationToken cancellationToken)
        => DirectiveCompletionProviderUtilities.TryGetStringLiteralToken(tree, position, SyntaxKind.LoadDirectiveTrivia, out literalValue, out textSpan, cancellationToken);
}
