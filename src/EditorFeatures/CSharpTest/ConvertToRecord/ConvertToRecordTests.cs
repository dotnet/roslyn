// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord.CSharpConvertToRecordRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertToRecord)]
    public class ConvertToRecordTests
    {
        [Fact]
        public async Task TestNoProperties_NoAction()
        {
            var initialMarkup = @"
namespace N {
    public class C {
        // field, not property
        public int f = 0;
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        private class Test : VerifyCS.Test
        {
            public Test() { }

            protected override Workspace CreateWorkspaceImpl()
            {

                var workspace = new AdhocWorkspace();

                return workspace;
            }
        }

        private static async Task TestRefactoringAsync(
            string initialMarkup,
            string changedMarkup)
        {
            await new Test()
            {
                TestCode = initialMarkup,
                FixedCode = changedMarkup,
            }.RunAsync().ConfigureAwait(false);
        }

        private static async Task TestNoRefactoringAsync(
            string initialMarkup)
        {
            await new Test()
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }
    }
}
