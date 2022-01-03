// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseParameterNullChecking;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseParameterNullChecking
{
    public partial class UseParameterNullCheckingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private static readonly CSharpParseOptions s_parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersionExtensions.CSharpNext);

        public UseParameterNullCheckingTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseParameterNullCheckingDiagnosticAnalyzer(), new CSharpUseParameterNullCheckingCodeFixProvider());

        [Theory]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("==")]
        [InlineData("is")]
        public async Task TestNoBraces(string @operator)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]s " + @operator + @" null)
            throw new ArgumentNullException(nameof(s));
    }
}",
@"using System;

class C
{
    void M(string s!!)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("==")]
        [InlineData("is")]
        public async Task TestWithBraces(string @operator)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]s " + @operator + @" null)
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}",
@"using System;

class C
{
    void M(string s!!)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("==")]
        [InlineData("is")]
        public async Task TestLocalFunction(string @operator)
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        local("""");
        void local(string s)
        {
            if ([||]s " + @operator + @" null)
            {
                throw new ArgumentNullException(nameof(s));
            }
        }
    }
}",
@"using System;

class C
{
    void M()
    {
        local("""");
        void local(string s!!)
        {
        }
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestEqualitySwapped()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]null == (object)s)
            throw new ArgumentNullException(nameof(s));
    }
}",
@"using System;

class C
{
    void M(string s!!)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotEquality()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](object)s != null)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCoalescingThrow()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    private readonly string s;
    void M(string s)
    {
        this.s = [||]s ?? throw new ArgumentNullException(nameof(s));
    }
}",
@"using System;

class C
{
    private readonly string s;
    void M(string s!!)
    {
        this.s = s;
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestReferenceEqualsCheck()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    private readonly string s;
    void M(string s)
    {
        if ([||]object.ReferenceEquals(s, null))
        {
            throw new ArgumentNullException(nameof(s));
        }
    }
}",
@"using System;

class C
{
    private readonly string s;
    void M(string s!!)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotCustomReferenceEqualsCheck()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    private readonly string s;
    void M(string s)
    {
        if ([||]ReferenceEquals(s, null))
        {
            throw new ArgumentNullException(nameof(s));
        }
    }

    bool ReferenceEquals(object o1, object o2) => false;
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotEqualitySwapped()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]null != (object)s)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestMissingPreCSharp11()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](object)s == null)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOnlyForObjectCast()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](string)s == null)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s1, string s2)
    {
        if ({|FixAllInDocument:|}(object)s1 == null)
            throw new ArgumentNullException(nameof(s1));

        if (null == (object)s2)
            throw new ArgumentNullException(nameof(s2));
    }
}",
@"using System;

class C
{
    void M(string s1!!, string s2!!)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAllNested1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s2)
    {
        if ({|FixAllInDocument:|}(object)((object)s2 == null) == null))
            throw new ArgumentNullException(nameof(s2));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestConstrainedTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M<T>(T s) where T : class
    {
        if ([||](object)s == null)
            throw new ArgumentNullException(nameof(s));
    }
}",
@"using System;

class C
{
    void M<T>(T s!!) where T : class
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestUnconstrainedTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M<T>(T s)
    {
        if ([||](object)s == null)
            throw new ArgumentNullException(nameof(s));
    }
}",
@"using System;

class C
{
    void M<T>(T s!!)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestComplexExpr()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string[] s)
    {
        if ([||](object)s[0] == null)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestComplexExpr2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s1, string s2)
    {
        if ([||]s1 == null || s2 == null)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotOnDefault()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||](object)s == default)
            throw new ArgumentNullException(nameof(s));
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithStringExceptionArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]s == null)
            throw new ArgumentNullException(""s"");
    }
}",
@"using System;

class C
{
    void M(string s!!)
    {
    }
}",
            parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithStringExceptionArgument2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string HelloWorld)
    {
        if ([||]HelloWorld == null)
            throw new ArgumentNullException(""Hello"" + ""World"");
    }
}",
@"using System;

class C
{
    void M(string HelloWorld!!)
    {
    }
}",
parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotWithUnexpectedExceptionArgument()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]s == null)
            throw new ArgumentNullException(""banana"");
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotWithExceptionNoArguments()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(string s)
    {
        if ([||]s == null)
            throw new ArgumentNullException();
    }
}", parameters: new TestParameters(parseOptions: s_parseOptions));
        }
    }
}
