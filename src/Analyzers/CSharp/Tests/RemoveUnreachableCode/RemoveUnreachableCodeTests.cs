// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnreachableCode
{
    public class RemoveUnreachableCodeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public RemoveUnreachableCodeTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnreachableCodeDiagnosticAnalyzer(), new CSharpRemoveUnreachableCodeCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestSingleUnreachableStatement()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestInUnreachableIfBody()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        if (false)
        {
            [|var v = 0;|]
        }
    }
}",
@"
class C
{
    void M()
    {
        if (false)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestInIfWithNoBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        if (false)
            [|var v = 0;|]
    }
}",
@"
class C
{
    void M()
    {
        if (false)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestRemoveSubsequentStatements()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]
        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestFromSubsequentStatement()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        var v = 0;
        [|var y = 1;|]
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestRemoveSubsequentStatementsExcludingLocalFunction()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]

        void Local() {}

        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        void Local() {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestRemoveSubsequentStatementsExcludingMultipleLocalFunctions()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]

        void Local() {}
        void Local2() {}

        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        void Local() {}
        void Local2() {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestRemoveSubsequentStatementsInterspersedWithMultipleLocalFunctions()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]

        void Local() {}

        var z = 2;

        void Local2() {}

        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        void Local() {}

        void Local2() {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestRemoveSubsequentStatementsInterspersedWithMultipleLocalFunctions2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]

        void Local() {}

        var z = 2;
        var z2 = 2;

        void Local2() {}

        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        void Local() {}

        void Local2() {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestRemoveSubsequentStatementsUpToNextLabel()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]

        label:
            Console.WriteLine();

        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        label:
            Console.WriteLine();

        var y = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestOnUnreachableLabel()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        var v = 0;

        [|label|]:
            Console.WriteLine();

        var y = 1;
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();
        var v = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestMissingOnReachableLabel()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M(object o)
    {
        if (o != null)
        {
            goto label;
        }

        throw new System.Exception();
        var v = 0;

        [|label|]:
            Console.WriteLine();

        var y = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestInLambda()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    void M()
    {
        Action a = () => {
            if (true)
                return;
            
            [|Console.WriteLine();|]
        };
    }
}",
@"
using System;

class C
{
    void M()
    {
        Action a = () => {
            if (true)
                return;
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestInLambdaInExpressionBody()
        {
            await TestInRegularAndScript1Async(
@"
using System;

class C
{
    Action M()
        => () => {
            if (true)
                return;
            
            [|Console.WriteLine();|]
        };
}",
@"
using System;

class C
{
    Action M()
        => () => {
            if (true)
                return;
        };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestSingleRemovalDoesNotTouchCodeInUnrelatedLocalFunction()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        [|var v = 0;|]

        void Local()
        {
            throw new System.Exception();
            var x = 0;
        }
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        void Local()
        {
            throw new System.Exception();
            var x = 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();
        {|FixAllInDocument:var v = 0;|}

        void Local()
        {
            throw new System.Exception();
            var x = 0;
        }
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

        void Local()
        {
            throw new System.Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(object o)
    {
        if (o == null)
        {
            goto ReachableLabel;
        }

        throw new System.Exception();
        {|FixAllInDocument:var v = 0;|}

        UnreachableLabel:
            Console.WriteLine(x);

        ReachableLabel:
            Console.WriteLine(y);
    }
}",
@"
class C
{
    void M(object o)
    {
        if (o == null)
        {
            goto ReachableLabel;
        }

        throw new System.Exception();

        ReachableLabel:
            Console.WriteLine(y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestFixAll3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(object o)
    {
        if (o == null)
        {
            goto ReachableLabel2;
        }

        throw new System.Exception();
        {|FixAllInDocument:var v = 0;|}

        ReachableLabel1:
            Console.WriteLine(x);

        ReachableLabel2:
        {
            Console.WriteLine(y);
            goto ReachableLabel1;
        }

        var x = 1;
    }
}",
@"
class C
{
    void M(object o)
    {
        if (o == null)
        {
            goto ReachableLabel2;
        }

        throw new System.Exception();

        ReachableLabel1:
            Console.WriteLine(x);

        ReachableLabel2:
        {
            Console.WriteLine(y);
            goto ReachableLabel1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestFixAll4()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(object o)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                goto stop;
                {|FixAllInDocument:goto outerLoop;|}
            }
        outerLoop:
            return;
        }
    stop:
        return;
    }
    }
}",
@"
class C
{
    void M(object o)
    {
        for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                goto stop;
            }
        outerLoop:
            return;
        }
    stop:
        return;
    }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestFixAll5()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(object o)
    {
        if (false)
            throw new Exception();

        throw new Exception();
        {|FixAllInDocument:return;|}
    }
}",
@"
class C
{
    void M(object o)
    {
        if (false)
            throw new Exception();

        throw new Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestInUnreachableInSwitchSection1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M(int i)
    {
        switch (i)
        {
            case 0:
                throw new Exception();
                [|var v = 0;|]
                break;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        switch (i)
        {
            case 0:
                throw new Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestDirectives1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        throw new System.Exception();

#if true
        [|var v = 0;|]
#endif
    }
}",
@"
class C
{
    void M()
    {
        throw new System.Exception();

#if true
#endif
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestDirectives2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
#if true
        throw new System.Exception();
        [|var v = 0;|]
#endif
    }
}",
@"
class C
{
    void M()
    {
#if true
        throw new System.Exception();
#endif
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestDirectives3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
#if true
        throw new System.Exception();
#endif
        [|var v = 0;|]
    }
}",
@"
class C
{
    void M()
    {
#if true
        throw new System.Exception();

#endif
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestForLoop1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        for (int i = 0; i < 5;)
        {
            i = 2;
            goto Lab2;
            [|i = 1;|]
            break;
        Lab2:
            return ;
        }
    }
}",
@"
class C
{
    void M()
    {
        for (int i = 0; i < 5;)
        {
            i = 2;
            goto Lab2;
        Lab2:
            return ;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnreachableCode)]
        public async Task TestInfiniteForLoop()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        for (;;) { }
        [|return;|]
    }
}",
@"
class C
{
    void M()
    {
        for (;;) { }
    }
}");
        }
    }
}
