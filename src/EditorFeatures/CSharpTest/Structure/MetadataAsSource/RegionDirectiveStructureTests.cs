// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource;

[Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
public class RegionDirectiveStructureTests : AbstractCSharpSyntaxNodeStructureTests<RegionDirectiveTriviaSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new RegionDirectiveStructureProvider();

    [Fact]
    public async Task FileHeader()
    {
        var code = """
                {|span:#re$$gion Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                // C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
                #endregion|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", autoCollapse: true, isDefaultCollapsed: true));
    }

    [Fact]
    public async Task EmptyFileHeader()
    {
        var code = """
                {|span:#re$$gion
                // C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
                #endregion|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "#region", autoCollapse: true, isDefaultCollapsed: true));
    }
}
