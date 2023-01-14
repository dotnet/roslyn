// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeLocalFunctionStatic
{
    public partial class MakeLocalFunctionStaticTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public MakeLocalFunctionStaticTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new MakeLocalFunctionStaticDiagnosticAnalyzer(), GetMakeLocalFunctionStaticCodeFixProvider());

        private static readonly ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
        private static readonly ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestAboveCSharp8()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        static int fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestWithOptionOff()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
new TestParameters(
    parseOptions: CSharp8ParseOptions,
    options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOptions2.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfAlreadyStatic()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        static int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingPriorToCSharp8()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp72ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfCapturesValue()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(int i)
    {
        int [||]fibonacci(int n)
        {
            return i <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMissingIfCapturesThis()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        int [||]fibonacci(int n)
        {
            M();
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestAsyncFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        async Task<int> [||]fibonacci(int n)
        {
            return n <= 1 ? n : await fibonacci(n - 1) + await fibonacci(n - 2);
        }
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        static async Task<int> fibonacci(int n)
        {
            return n <= 1 ? n : await fibonacci(n - 1) + await fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task TestLeadingTriviaAfterSemicolon(string leadingTrivia)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class C
{{
    void M()
    {{
        int x;{leadingTrivia}
        int [||]fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
@"using System;

class C
{
    void M()
    {
        int x;

        static int fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task TestLeadingTriviaAfterOpenBrace(string leadingTrivia)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class C
{{
    void M()
    {{{leadingTrivia}
        int [||]fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
@"using System;

class C
{
    void M()
    {
        static int fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task TestLeadingTriviaAfterLocalFunction(string leadingTrivia)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class C
{{
    void M()
    {{
        bool otherFunction()
        {{
            return true;
        }}{leadingTrivia}
        int [||]fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
@"using System;

class C
{
    void M()
    {
        bool otherFunction()
        {
            return true;
        }

        static int fibonacci(int n)
        {
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task TestLeadingTriviaAfterExpressionBodyLocalFunction(string leadingTrivia)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class C
{{
    void M()
    {{
        bool otherFunction() => true;{leadingTrivia}
        int [||]fibonacci(int n) => n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
    }}
}}",
@"using System;

class C
{
    void M()
    {
        bool otherFunction() => true;

        static int fibonacci(int n) => n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task TestLeadingTriviaAfterComment(string leadingTrivia)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class C
{{
    void M()
    {{
        //Local function comment{leadingTrivia}
        int [||]fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
$@"using System;

class C
{{
    void M()
    {{
        //Local function comment{leadingTrivia}
        static int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
parseOptions: CSharp8ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task TestLeadingTriviaBeforeComment(string leadingTrivia)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class C
{{
    void M()
    {{{leadingTrivia}
        //Local function comment
        int [||]fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
$@"using System;

class C
{{
    void M()
    {{{leadingTrivia}
        //Local function comment
        static int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
}}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [WorkItem(46858, "https://github.com/dotnet/roslyn/issues/46858")]
        public async Task TestMissingIfAnotherLocalFunctionCalled()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
        void [||]A()
        {
            B();
        }

        void B()
        {
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestCallingStaticLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        void [||]A()
        {
            B();
        }

        static void B()
        {
        }
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        static void A()
        {
            B();
        }

        static void B()
        {
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestCallingNestedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        void [||]A()
        {
            B();

            void B()
            {
            }
        }
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        static void A()
        {
            B();

            void B()
            {
            }
        }
    }
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [WorkItem(53179, "https://github.com/dotnet/roslyn/issues/53179")]
        public async Task TestLocalFunctionAsTopLevelStatement()
        {
            await TestAsync(@"
void [||]A()
{
}", @"
static void A()
{
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        [WorkItem(59286, "https://github.com/dotnet/roslyn/issues/59286")]
        public async Task TestUnsafeLocalFunction()
        {
            await TestAsync(@"
unsafe void [||]A()
{
}", @"
static unsafe void A()
{
}",
parseOptions: CSharp8ParseOptions);
        }
    }
}
