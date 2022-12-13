// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractStatementSnippetProvider : AbstractSnippetProvider
    {
        protected override bool IsValidSnippetLocation(SyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsStatementContext || context.IsGlobalStatementContext;
        }
    }
}
