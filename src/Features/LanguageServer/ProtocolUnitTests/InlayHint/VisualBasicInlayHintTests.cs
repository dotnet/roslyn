﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.InlayHint
{
    public class VisualBasicInlayHintTests : AbstractInlayHintTests
    {
        public VisualBasicInlayHintTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task TestOneInlayParameterHintAsync()
        {
            var markup =
@"Class A
    Sub M(x As Integer)
    End Sub

    Sub M2()
        M({|x:|}5)
    End Sub
End Class";
            await RunVerifyInlayHintAsync(markup);
        }

        [Fact]
        public async Task TestMultipleInlayParameterHintsAsync()
        {
            var markup =
@"Class A
    Sub M(x As Integer, y As Boolean)
    End Sub

    Sub M2()
        M({|x:|}5, {|y:|}True)
    End Sub
End Class";
            await RunVerifyInlayHintAsync(markup);
        }

        private async Task RunVerifyInlayHintAsync(string markup)
        {
            await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.VisualBasic, true);
            await VerifyInlayHintAsync(testLspServer);
        }
    }
}
