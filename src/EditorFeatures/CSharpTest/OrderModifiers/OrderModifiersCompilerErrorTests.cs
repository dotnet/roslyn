// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.OrderModifiers
{
    public sealed class OrderModifiersCompilerErrorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpOrderModifiersCodeFixProvider());

        [WorkItem(30352, "https://github.com/dotnet/roslyn/issues/30352")]
        [Fact(Skip = "PROTOTYPE(ref-partial)"), Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEnd()
        {
            // Verify that the code fix claims it fixes the compiler error (CS0267) in addition to the analyzer diagnostic.
            await TestInRegularAndScript1Async(
@"[|partial|] public class C { }",
@"public partial class C { }");
        }
    }
}
