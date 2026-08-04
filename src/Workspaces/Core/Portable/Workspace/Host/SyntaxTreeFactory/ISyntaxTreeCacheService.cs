// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

internal interface ISyntaxTreeCacheService : IWorkspaceService
{
    SyntaxTree GetOrCreateSyntaxTree<TArg>(
        SourceText text,
        ParseOptions options,
        Func<TArg, CancellationToken, SyntaxTree> parseSyntaxTree,
        Func<SyntaxNode, TArg, SyntaxTree> createSyntaxTreeFromRoot,
        TArg arg,
        CancellationToken cancellationToken);
}
