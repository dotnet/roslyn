// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Roslyn.LanguageServer.Protocol;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.InlayHint
{
    public class VisualBasicInlayHintTests : AbstractInlayHintTests
    {
        public VisualBasicInlayHintTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestOneInlayParameterHintAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"Class A
    Sub M(x As Integer)
    End Sub

    Sub M2()
        M({|x:|}5)
    End Sub
End Class";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace);
        }

        [Theory, CombinatorialData]
        public async Task TestMultipleInlayParameterHintsAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"Class A
    Sub M(x As Integer, y As Boolean)
    End Sub

    Sub M2()
        M({|x:|}5, {|y:|}True)
    End Sub
End Class";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace);
        }

        private async Task RunVerifyInlayHintAsync(string markup, bool mutatingLspWorkspace)
        {
            await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions
            {
                ClientCapabilities = new LSP.VSInternalClientCapabilities
                {
                    SupportsVisualStudioExtensions = true,
                    Workspace = new WorkspaceClientCapabilities
                    {
                        InlayHint = new InlayHintWorkspaceSetting
                        {
                            RefreshSupport = true
                        }
                    }
                }
            });
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.VisualBasic, true);
            await VerifyInlayHintAsync(testLspServer);
        }
    }
}
