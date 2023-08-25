// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752640")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/855748")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867496")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18621")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23667")]
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
