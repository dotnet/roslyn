// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References;
public class FindAllReferencesHandlerFeaturesTests : AbstractLanguageServerProtocolTests
{
    public FindAllReferencesHandlerFeaturesTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => EditorTestCompositions.LanguageServerProtocol
        .AddParts(typeof(TestDocumentTrackingService))
        .AddParts(typeof(TestWorkspaceRegistrationService));

    [Theory, CombinatorialData]
    public async Task TestFindAllReferencesAsync_DoesNotUseVSTypes(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    public int {|reference:someInt|} = 1;
    void M()
    {
        var i = {|reference:someInt|} + 1;
    }
}
class B
{
    int someInt = A.{|reference:someInt|} + 1;
    void M2()
    {
        var j = someInt + A.{|caret:|}{|reference:someInt|};
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.ClientCapabilities());

        var results = await FindAllReferencesHandlerTests.RunFindAllReferencesAsync<LSP.Location>(testLspServer, testLspServer.GetLocations("caret").First());
        AssertLocationsEqual(testLspServer.GetLocations("reference"), results.Select(result => result));
    }
}
