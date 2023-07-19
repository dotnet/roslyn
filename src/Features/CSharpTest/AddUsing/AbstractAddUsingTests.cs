// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Remote.Testing;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing
{
    public abstract class AbstractAddUsingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        protected AbstractAddUsingTests(ITestOutputHelper logger = null)
            : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddImportCodeFixProvider());

        private protected OptionsCollection SeparateGroups => Option(GenerationOptions.SeparateImportDirectiveGroups, true);

        internal async Task TestAsync(
            string initialMarkup,
            string expectedMarkup,
            TestHost testHost,
            int index = 0,
            CodeActionPriority? priority = null,
            OptionsCollection options = null)
        {
            await TestInRegularAndScript1Async(
                initialMarkup,
                expectedMarkup,
                index,
                parameters: new TestParameters(options: options, testHost: testHost, priority: priority));
        }
    }
}
