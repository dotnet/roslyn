// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Structure;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

public abstract class AbstractCSharpSyntaxNodeStructureTests<TSyntaxNode> :
    AbstractSyntaxNodeStructureProviderTests<TSyntaxNode>
    where TSyntaxNode : SyntaxNode
{
    protected sealed override string LanguageName => LanguageNames.CSharp;

    private protected new Task VerifyBlockSpansAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markupCode,
        params RegionData[] expectedRegionData)
        => base.VerifyBlockSpansAsync(markupCode, expectedRegionData);
}
