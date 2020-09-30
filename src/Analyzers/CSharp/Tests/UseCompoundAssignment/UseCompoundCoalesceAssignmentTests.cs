// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCompoundAssignment
{
    public class UseCompoundCoalesceAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public UseCompoundCoalesceAssignmentTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer(), new CSharpUseCompoundCoalesceAssignmentCodeFixProvider());

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestBaseCase()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo [||]?? (s_goo = new string('c', 42));
}",
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo ??= new string('c', 42);
}");
        }

        [WorkItem(44793, "https://github.com/dotnet/roslyn/issues/44793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestMissingBeforeCSharp8()
        {
            await TestMissingAsync(
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo [||]?? (s_goo = new string('c', 42));
}", new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3)));
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestRightMustBeParenthesized()
        {
            await TestMissingAsync(
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo [||]?? s_goo = new string('c', 42);
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestRightMustBeAssignment()
        {
            await TestMissingAsync(
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo [||]?? (s_goo == new string('c', 42));
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestRightMustBeSimpleAssignment()
        {
            await TestMissingAsync(
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo [||]?? (s_goo ??= new string('c', 42));
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestShapesMustBeTheSame()
        {
            await TestMissingAsync(
@"class Program
{
    private static string s_goo;
    private static string s_goo2;
    private static string Goo => s_goo [||]?? (s_goo2 = new string('c', 42));
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNoSideEffects1()
        {
            await TestMissingAsync(
@"class Program
{
    private static string s_goo;
    private static string Goo => s_goo.GetType() [||]?? (s_goo.GetType() = new string('c', 42));
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNoSideEffects2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    private string goo;
    private string Goo => this.goo [||]?? (this.goo = new string('c', 42));
}",
@"class Program
{
    private string goo;
    private string Goo => this.goo ??= new string('c', 42);
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestNullableValueType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Goo()
    {
        int? a = null;
        var x = a [||]?? (a = 1);
    }
}",
@"class Program
{
    void Goo()
    {
        int? a = null;
        var x = (int?)(a ??= 1);
    }
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestCastIfWouldAffectSemantics()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    static void M(int a) { }
    static void M(int? a) { }

    static void Main()
    {
        int? a = null;
        M(a [||]?? (a = 1));
    }
}",
@"using System;
class C
{
    static void M(int a) { }
    static void M(int? a) { }

    static void Main()
    {
        int? a = null;
        M((int?)(a ??= 1));
    }
}");
        }

        [WorkItem(38059, "https://github.com/dotnet/roslyn/issues/38059")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestDoNotCastIfNotNecessary()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    static void M(int? a) { }

    static void Main()
    {
        int? a = null;
        M(a [||]?? (a = 1));
    }
}",
@"using System;
class C
{
    static void M(int? a) { }

    static void Main()
    {
        int? a = null;
        M(a ??= 1);
    }
}");
        }
    }
}
