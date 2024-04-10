// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ReferenceDirectiveCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(LoadDirectiveCompletionProvider))]
[Shared]
internal sealed class ReferenceDirectiveCompletionProvider : AbstractReferenceDirectiveCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ReferenceDirectiveCompletionProvider()
    {
    }

    protected override string DirectiveName => "r";

    protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
        => DirectiveCompletionProviderUtilities.TryGetStringLiteralToken(tree, position, SyntaxKind.ReferenceDirectiveTrivia, out stringLiteral, cancellationToken);
}
