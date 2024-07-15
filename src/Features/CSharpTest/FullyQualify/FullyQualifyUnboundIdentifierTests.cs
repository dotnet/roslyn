// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FullyQualify
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
    public class FullyQualifyUnboundIdentifierTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public FullyQualifyUnboundIdentifierTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUnboundIdentifiersDiagnosticAnalyzer(), new CSharpFullyQualifyCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26887")]
        public async Task TestFullyQualifyUnboundIdentifier1()
        {
            await TestInRegularAndScriptAsync(
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    [|Inner|]
}",
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    Program.Inner
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26887")]
        public async Task TestFullyQualifyUnboundIdentifier2()
        {
            await TestInRegularAndScriptAsync(
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    public [|Inner|]
}",
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    public Program.Inner
}");
        }
    }
}
