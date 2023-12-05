// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractForLoopSnippetProvider : AbstractInlineStatementSnippetProvider
    {
        protected override bool IsValidAccessingType(ITypeSymbol type)
            => type.IsIntegralType() || type.IsNativeIntegerType;

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
            => syntaxFacts.IsForStatement;
    }
}
