// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    public partial class AddUsingTestsWithAddImportDiagnosticProvider : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public AddUsingTestsWithAddImportDiagnosticProvider(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUnboundIdentifiersDiagnosticAnalyzer(), new CSharpAddImportCodeFixProvider());

        [WorkItem(1239, @"https://github.com/dotnet/roslyn/issues/1239")]
        [Fact]
        public async Task TestIncompleteLambda1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class C
{
    C()
    {
        """".Select(() => {
        new [|Byte|]",
@"using System;
using System.Linq;

class C
{
    C()
    {
        """".Select(() => {
        new Byte");
        }

        [WorkItem(1239, @"https://github.com/dotnet/roslyn/issues/1239")]
        [Fact]
        public async Task TestIncompleteLambda2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class C
{
    C()
    {
        """".Select(() => {
            new [|Byte|]() }",
@"using System;
using System.Linq;

class C
{
    C()
    {
        """".Select(() => {
            new Byte() }");
        }

        [WorkItem(860648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860648")]
        [WorkItem(902014, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/902014")]
        [Fact]
        public async Task TestIncompleteSimpleLambdaExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        args[0].Any(x => [|IBindCtx|]
        string a;
    }
}",
@"using System.Linq;
using System.Runtime.InteropServices.ComTypes;

class Program
{
    static void Main(string[] args)
    {
        args[0].Any(x => IBindCtx
        string a;
    }
}");
        }

        [WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")]
        [Fact]
        public async Task TestUnknownIdentifierGenericName()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private [|List<int>|]
}",
@"using System.Collections.Generic;

class C
{
    private List<int>
}");
        }

        [WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")]
        [Fact]
        public async Task TestUnknownIdentifierInAttributeSyntaxWithoutTarget()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [[|Extension|]]
}",
@"using System.Runtime.CompilerServices;

class C
{
    [Extension]
}");
        }

        [Fact]
        public async Task TestOutsideOfMethodWithMalformedGenericParameters()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    Func<[|FlowControl|] x }",
@"using System;
using System.Reflection.Emit;

class Program
{
    Func<FlowControl x }");
        }

        [WorkItem(752640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752640")]
        [Fact]
        public async Task TestUnknownIdentifierWithSyntaxError()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [|Directory|] private int i;
}",
@"using System.IO;

class C
{
    Directory private int i;
}");
        }

        [WorkItem(855748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/855748")]
        [Fact]
        public async Task TestGenericNameWithBrackets()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|List|]
}",
@"using System.Collections.Generic;

class Class
{
    List
}");

            await TestInRegularAndScriptAsync(
@"class Class
{
    [|List<>|]
}",
@"using System.Collections.Generic;

class Class
{
    List<>
}");

            await TestInRegularAndScriptAsync(
@"class Class
{
    List[|<>|]
}",
@"using System.Collections.Generic;

class Class
{
    List<>
}");
        }

        [WorkItem(867496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867496")]
        [Fact]
        public async Task TestMalformedGenericParameters()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    [|List<|] }",
@"using System.Collections.Generic;

class Class
{
    List< }");

            await TestInRegularAndScriptAsync(
@"class Class
{
    [|List<Y x;|] }",
@"using System.Collections.Generic;

class Class
{
    List<Y x; }");
        }

        [WorkItem(18621, "https://github.com/dotnet/roslyn/issues/18621")]
        [Fact]
        public async Task TestIncompleteMemberWithAsyncTaskReturnType()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
using System.Threading.Tasks;

namespace X
{
    class ProjectConfiguration
    {
    }
}

namespace ConsoleApp282
{
    class Program
    {
        public async Task<IReadOnlyCollection<[|ProjectConfiguration|]>>
    }
}",
@"
using System.Collections.Generic;
using System.Threading.Tasks;
using X;

namespace X
{
    class ProjectConfiguration
    {
    }
}

namespace ConsoleApp282
{
    class Program
    {
        public async Task<IReadOnlyCollection<ProjectConfiguration>>
    }
}");
        }

        [WorkItem(23667, "https://github.com/dotnet/roslyn/issues/23667")]
        [Fact]
        public async Task TestMissingDiagnosticForNameOf()
        {
            await TestDiagnosticMissingAsync(
@"using System;

class C
{
    Action action = () => {
        var x = [|nameof|](System);
#warning xxx
    };
}");
        }
    }
}
