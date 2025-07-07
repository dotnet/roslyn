// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

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

    protected override bool TryGetCompletionPrefix(SyntaxTree tree, int position, [NotNullWhen(true)] out string? literalValue, out TextSpan textSpan, CancellationToken cancellationToken)
        => DirectiveCompletionProviderUtilities.TryGetStringLiteralToken(tree, position, SyntaxKind.ReferenceDirectiveTrivia, out literalValue, out textSpan, cancellationToken);
}
