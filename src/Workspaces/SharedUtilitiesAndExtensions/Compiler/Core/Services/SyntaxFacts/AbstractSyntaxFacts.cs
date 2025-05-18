// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract class AbstractSyntaxFacts
{
    public void AddTopLevelAndMethodLevelMembers(SyntaxNode? root, ArrayBuilder<SyntaxNode> result)
        => AppendMembers(root, result, topLevel: true, methodLevel: true);

    public void AddTopLevelMembers(SyntaxNode? root, ArrayBuilder<SyntaxNode> result)
        => AppendMembers(root, result, topLevel: true, methodLevel: false);

    public void AddMethodLevelMembers(SyntaxNode? root, ArrayBuilder<SyntaxNode> result)
        => AppendMembers(root, result, topLevel: false, methodLevel: true);

    protected abstract void AppendMembers(SyntaxNode? node, ArrayBuilder<SyntaxNode> list, bool topLevel, bool methodLevel);
}
