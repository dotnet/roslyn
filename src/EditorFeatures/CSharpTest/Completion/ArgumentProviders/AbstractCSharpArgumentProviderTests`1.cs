// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.Completion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders;

public abstract class AbstractCSharpArgumentProviderTests<TWorkspaceFixture>
    : AbstractArgumentProviderTests<TWorkspaceFixture>
    where TWorkspaceFixture : TestWorkspaceFixture, new()
{
    protected override (SyntaxNode argumentList, ImmutableArray<SyntaxNode> arguments) GetArgumentList(SyntaxToken token)
    {
        var argumentList = token.GetRequiredParent().GetAncestorsOrThis<BaseArgumentListSyntax>().First();
        return (argumentList, argumentList.Arguments.ToImmutableArray<SyntaxNode>());
    }
}
