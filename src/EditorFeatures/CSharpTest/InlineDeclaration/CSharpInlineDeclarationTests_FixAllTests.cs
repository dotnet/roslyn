// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration
{
    public partial class CSharpInlineDeclarationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i|}, j;
        if (int.TryParse(v, out i, out j))
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i, out int j))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument2()
        {

            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        {|FixAllInDocument:int|} i;
        if (int.TryParse(v, out i))
        {
        }
    }

    void M1()
    {
        int i;
        if (int.TryParse(v, out i))
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i))
        {
        }
    }

    void M1()
    {
        if (int.TryParse(v, out int i))
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // Now get final exe and args. CTtrl-F5 wraps exe in cmd prompt
        string {|FixAllInDocument:finalExecutable|}, finalArguments;
        GetExeAndArguments(useCmdShell, executable, arguments, out finalExecutable, out finalArguments);
    }
}",
@"class C
{
    void M()
    {
        // Now get final exe and args. CTtrl-F5 wraps exe in cmd prompt
        GetExeAndArguments(useCmdShell, executable, arguments, out string finalExecutable, out string finalArguments);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(29935, "https://github.com/dotnet/roslyn/issues/29935")]
        public async Task FixAllInDocumentSymbolResolution()
        {
            await TestInRegularAndScriptAsync(
@"class C 
{
    void M()
    {
        string {|FixAllInDocument:s|};
        bool b;
        A(out s, out b);
    }

    void A(out string s, out bool b)
    {
        s = string.Empty;
        b = false;
    }

    void A(out string s, out string s2)
    {
        s = s2 = string.Empty;
    }
}",
@"class C 
{
    void M()
    {
        A(out string s, out bool b);
    }

    void A(out string s, out bool b)
    {
        s = string.Empty;
        b = false;
    }

    void A(out string s, out string s2)
    {
        s = s2 = string.Empty;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocument4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}; int i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
    }
}",
@"class C
{
    void M()
    {
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocument5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int dummy; int {|FixAllInDocument:i1|}; int i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        int dummy;  
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocument6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}; int dummy; int i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        int dummy;
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocument7()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}; int i2; int dummy;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        int dummy;
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int dummy, {|FixAllInDocument:i1|}, i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        int dummy;
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}, dummy, i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        int dummy;
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument10()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}, i2, dummy;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        int dummy;
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument11()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}, i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
    }
}",
@"class C
{
    void M()
    {
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocumentComments1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        /* leading */ int {|FixAllInDocument:i1|}; int i2; // trailing
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
    }
}",
@"class C
{
    void M()
    {
        /* leading */ // trailing
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocumentComments2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        /* leading */ int dummy; /* inbetween */ int {|FixAllInDocument:i1|}; int i2; // trailing
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        /* leading */ int dummy; /* inbetween */   // trailing
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        [WorkItem(28323, "https://github.com/dotnet/roslyn/issues/28323")]
        public async Task FixAllInDocumentComments3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i1|}; /* 0 */int /* 1 */ dummy /* 2 */; /* 3*/ int i2;
        int.TryParse(v, out i1);
        int.TryParse(v, out i2);
        dummy = 42;
    }
}",
@"class C
{
    void M()
    {
        /* 0 */
        int /* 1 */ dummy /* 2 */; /* 3*/
        int.TryParse(v, out int i1);
        int.TryParse(v, out int i2);
        dummy = 42;
    }
}");
        }
    }
}
