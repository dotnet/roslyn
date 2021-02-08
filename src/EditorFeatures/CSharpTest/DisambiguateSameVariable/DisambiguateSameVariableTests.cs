// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DisambiguateSameVariable
{
    public class DisambiguateSameVariableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public DisambiguateSameVariableTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpDisambiguateSameVariableCodeFixProvider());

        [Fact]
        public async Task TestParamToParamWithNoMatch()
        {
            await TestMissingAsync(
@"class C
{
    void M(int a)
    {
        [|a = a|];
    }
}");
        }

        [Fact]
        public async Task TestLocalToLocalWithNoMatch()
        {
            await TestMissingAsync(
@"class C
{
    void M(int a)
    {
        [|a = a|];
    }
}");
        }

        [Fact]
        public async Task TestFieldToFieldWithNoMatch()
        {
            await TestMissingAsync(
@"class C
{
    int a;
    void M()
    {
        [|a = a|];
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithSameNamedField()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int a;
    void M(int a)
    {
        [|a = a|];
    }
}",
@"class C
{
    int a;
    void M(int a)
    {
        this.a = a;
    }
}");
        }

        [Fact]
        public async Task TestFieldToFieldWithNonMatchingField()
        {
            await TestMissingAsync(
@"class C
{
    int x;
    void M()
    {
        [|a = a|];
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithUnderscoreNamedField()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int _a;
    void M(int a)
    {
        [|a = a|];
    }
}",
@"class C
{
    int _a;
    void M(int a)
    {
        _a = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithCapitalizedField()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int A;
    void M(int a)
    {
        [|a = a|];
    }
}",
@"class C
{
    int A;
    void M(int a)
    {
        A = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithProperty()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int A { get; set; }
    void M(int a)
    {
        [|a = a|];
    }
}",
@"class C
{
    int A { get; set; }
    void M(int a)
    {
        A = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithReadOnlyFieldInConstructor()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    readonly int a;
    public C(int a)
    {
        [|a = a|];
    }
}",
@"class C
{
    readonly int a;
    public C(int a)
    {
        this.a = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithReadOnlyFieldOutsideOfConstructor()
        {
            // Not legal, but is at least something they might want.
            await TestInRegularAndScript1Async(
@"class C
{
    readonly int a;
    void M(int a)
    {
        [|a = a|];
    }
}",
@"class C
{
    readonly int a;
    void M(int a)
    {
        this.a = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamWithAccessibleFieldInBaseType()
        {
            await TestInRegularAndScript1Async(
@"
class Base
{
    protected int a;
}

class C : Base
{
    public C(int a)
    {
        [|a = a|];
    }
}",
@"
class Base
{
    protected int a;
}

class C : Base
{
    public C(int a)
    {
        this.a = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamNotWithInaccessibleFieldInBaseType()
        {
            await TestMissingAsync(
@"
class Base
{
    private int a;
}

class C : Base
{
    public C(int a)
    {
        [|a = a|];
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamNotWithStaticField()
        {
            await TestMissingAsync(
@"
class C
{
    static int a;
    public C(int a)
    {
        [|a = a|];
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestParamToParamCompareWithSameNamedField()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int a;
    void M(int a)
    {
        if ([|a == a|])
        {
        }
    }
}",
@"class C
{
    int a;
    void M(int a)
    {
        if (this.a == a)
        {
        }
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int a;
    void M(int a)
    {
        {|FixAllInDocument:a = a|};
        a = a;
    }
}",
@"class C
{
    int a;
    void M(int a)
    {
        this.a = a;
        this.a = a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestFieldToFieldWithPropAvailableOffOfThis()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int a;
    int A { get; set; };
    void M()
    {
        [|this.a = this.a|];
    }
}",
@"class C
{
    int a;
    int A { get; set; };
    void M()
    {
        this.A = this.a;
    }
}");
        }

        [Fact, WorkItem(28290, "https://github.com/dotnet/roslyn/issues/28290")]
        public async Task TestFieldToFieldWithPropAvailableOffOfOtherInstance()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int a;
    int A { get; set; };
    void M(C c)
    {
        [|c.a = c.a|];
    }
}",
@"class C
{
    int a;
    int A { get; set; };
    void M(C c)
    {
        c.A = c.a;
    }
}");
        }
    }
}
