// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseTupleSwap;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseTupleSwap
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseTupleSwapDiagnosticAnalyzer,
        CSharpUseTupleSwapCodeFixProvider>;

    public partial class UseTupleSwapTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingBeforeCSharp7()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[0] = args[1];
        args[1] = temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp6,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestMissingWithFeatureOff()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[0] = args[1];
        args[1] = temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferTupleSwap, false, CodeStyle.NotificationOption2.Silent }
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestBasicCase()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(string[] args)
    {
        [|var|] temp = args[0];
        args[0] = args[1];
        args[1] = temp;
    }
}
",
@"
class C
{
    void M(string[] args)
    {
        (args[1], args[0]) = (args[0], args[1]);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotWithRef()
        {
            var code = @"
class C
{
    void M(ref int a, ref int b)
    {
        ref int temp = ref a;
        a = ref b;
        b = ref temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestArbitraryParens()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(string[] args)
    {
        [|var|] temp = (args[0]);
        ((args[0])) = (((args[1])));
        ((((args[1])))) = (((((temp)))));
    }
}
",
@"
class C
{
    void M(string[] args)
    {
        (args[1], args[0]) = (args[0], args[1]);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestTrivia1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(string[] args)
    {
        // Comment
        [|var|] temp = args[0];
        args[0] = args[1];
        args[1] = temp;
    }
}
",
@"
class C
{
    void M(string[] args)
    {
        // Comment
        (args[1], args[0]) = (args[0], args[1]);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestTrivia2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(string[] args)
    {
        [|var|] temp = args [ 0 ] ;
        args  [  0  ] = args   [   1   ];
        args    [    1    ] = temp;
    }
}
",
@"
class C
{
    void M(string[] args)
    {
        (args   [   1   ], args  [  0  ]) = (args  [  0  ], args   [   1   ]);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleAssignment1()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[0] += args[1];
        args[1] = temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestSimpleAssignment2()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[0] = args[1];
        args[1] += temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotSwap1()
        {
            var code = @"
class C
{
    void M(string[] args, string temp1)
    {
        var temp = args[0];
        args[0] = args[1];
        args[1] = temp1;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotSwap2()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[1] = temp;
        args[0] = args[1];
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotSwap3()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[0] = args[1];
        args[0] = temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotSwap4()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        var temp = args[0];
        args[1] = args[0];
        args[0] = temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotSwap5()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        string temp;
        args[0] = args[1];
        args[1] = {|CS0165:temp|};
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestInSwitch()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(string[] args, int x)
    {
        switch (x)
        {
            default:
                [|var|] temp = args[0];
                args[0] = args[1];
                args[1] = temp;
                break;
        }
    }
}
",
@"
class C
{
    void M(string[] args, int x)
    {
        switch (x)
        {
            default:
                (args[1], args[0]) = (args[0], args[1]);
                break;
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestFixAll1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M(string[] args)
    {
        // comment 1
        [|var|] temp1 = args[0];
        args[0] = args[1];
        args[1] = temp1;

        // comment 2
        [|var|] temp2 = args[2];
        args[2] = args[3];
        args[3] = temp2;
    }
}
",
@"
class C
{
    void M(string[] args)
    {
        // comment 1
        (args[1], args[0]) = (args[0], args[1]);

        // comment 2
        (args[3], args[2]) = (args[2], args[3]);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseLocalFunction)]
        public async Task TestNotWithMultipleVariables()
        {
            var code = @"
class C
{
    void M(string[] args)
    {
        string temp = args[0], temp2 = """";
        args[0] = args[1];
        args[1] = temp;
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }
    }
}
