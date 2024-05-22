// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractForEachLoopSnippetProvider : AbstractInlineStatementSnippetProvider
{
    public override string Identifier => "foreach";

    public override string Description => FeaturesResources.foreach_loop;

    protected override bool IsValidAccessingType(ITypeSymbol type, Compilation compilation)
        => type.CanBeEnumerated() || type.CanBeAsynchronouslyEnumerated(compilation);

    protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
    {
        return syntaxFacts.IsForEachStatement;
    }
}
