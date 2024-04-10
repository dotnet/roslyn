// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.OrderModifiers
{
    public sealed class OrderModifiersCompilerErrorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public OrderModifiersCompilerErrorTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpOrderModifiersCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/30352")]
        public async Task PartialAtTheEnd()
        {
            // Verify that the code fix claims it fixes the compiler error (CS0267) in addition to the analyzer diagnostic.
            await TestInRegularAndScript1Async(
@"[|partial|] public class C { }",
@"public partial class C { }");
        }
    }
}
