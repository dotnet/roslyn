// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseParameterNullChecking;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseParameterNullChecking
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpUseParameterNullCheckingDiagnosticAnalyzer, CSharpUseParameterNullCheckingCodeFixProvider>;

    public class UseParameterNullCheckingTests
    {
        [Theory]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("==")]
        [InlineData("is")]
        public async Task TestNoBraces(string @operator)
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string s)
    {
        [|if (s " + @operator + @" null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullableParameterType()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"#nullable enable
using System;

class C
{
    void M(string? s)
    {
        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"#nullable enable
using System;

class C
{
    void M(string? s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("==")]
        [InlineData("is")]
        public async Task TestWithBraces(string @operator)
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string s)
    {
        [|if (s " + @operator + @" null)
        {
            throw new ArgumentNullException(nameof(s));
        }|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Theory]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("==")]
        [InlineData("is")]
        public async Task TestLocalFunction(string @operator)
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M()
    {
        local("""");
        void local(string s)
        {
            [|if (s " + @operator + @" null)
            {
                throw new ArgumentNullException(nameof(s));
            }|]
        }
    }
}",
                FixedCode = @"
using System;

class C
{
    void M()
    {
        local("""");
        void local(string s!!)
        {
        }
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestEqualitySwapped()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string s)
    {
        [|if (null == (object)s)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotEquality()
        {
            var testCode = @"
using System;

class C
{
    void M(string s)
    {
        if ((object)s != null)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCoalescingThrow()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    private readonly string s;
    public C(string s)
    {
        this.s = s ?? [|throw new ArgumentNullException(nameof(s))|];
    }
}",
                FixedCode = @"
using System;

class C
{
    private readonly string s;
    public C(string s!!)
    {
        this.s = s;
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCoalescingThrow_AssignSameVariable()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    public void M(string s)
    {
        [|s = s ?? throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    public void M(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOperator()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    public static C operator +(C c, string s)
    {
        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]

        return new C();
    }
}",
                FixedCode = @"
using System;

class C
{
    public static C operator +(C c, string s!!)
    {
        return new C();
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestConversionOperator()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    public static implicit operator C(string s)
    {
        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]

        return new C();
    }
}",
                FixedCode = @"
using System;

class C
{
    public static implicit operator C(string s!!)
    {
        return new C();
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCoalescingThrowExpressionBody()
        {
            // We'd like to support this eventually. https://github.com/dotnet/roslyn/issues/58699
            var testCode = @"
using System;

class C
{
    private readonly string s;
    public C(string s)
        => this.s = s ?? throw new ArgumentNullException(nameof(s));
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCoalescingThrowBaseClause()
        {
            // We'd like to support this eventually. https://github.com/dotnet/roslyn/issues/58699
            var testCode = @"
using System;

class Base { public Base(string s) { } }
class C : Base
{
    public C(string s) : base(s ?? throw new ArgumentNullException(nameof(s))) { }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestAlreadyNullChecked()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(string s!!)
    {
        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"using System;

class C
{
    public C(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestSingleLineNoBraces()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(string s)
    {
        [|if (s is null) throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"using System;

class C
{
    public C(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestSingleLineWithBraces()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(string s)
    {
        [|if (s is null) { throw new ArgumentNullException(nameof(s)); }|]
    }
}",
                FixedCode = @"using System;

class C
{
    public C(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestLeadingAndTrailingTrivia1()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(string s)
    {
        // comment1
        [|if (s is null) { throw new ArgumentNullException(nameof(s)); }|] // comment2
    }
}",
                FixedCode = @"using System;

class C
{
    public C(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestLeadingAndTrailingTrivia2()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(
          string x // comment
        , string y // comment,
        , string z // comment
        )
    {
        [|if (x is null) throw new ArgumentNullException(nameof(x));|]
        [|if (y is null) throw new ArgumentNullException(nameof(y));|]
        [|if (z is null) throw new ArgumentNullException(nameof(z));|]
    }
}",
                FixedCode = @"using System;

class C
{
    public C(
          string x!! // comment
        , string y!! // comment,
        , string z!! // comment
        )
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestLeadingAndTrailingTrivia3()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(
          string x /* comment */ !!
        )
    {
        [|if (x is null) throw new ArgumentNullException(nameof(x));|]
    }
}",
                FixedCode = @"using System;

class C
{
    public C(
          string x /* comment */ !!
        )
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestAssignThenTest()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    public C(string s)
    {
        s = ""a"";
        [|if (s is null) { throw new ArgumentNullException(nameof(s)); }|]
    }
}",
                FixedCode = @"using System;

class C
{
    public C(string s!!)
    {
        s = ""a"";
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestRedundantNullChecks()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    public C(string s)
    {
        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]

        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode =
@"
using System;

class C
{
    public C(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("ref")]
        [InlineData("in")]
        public async Task TestRefParameter(string refKind)
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    public C(" + refKind + @" string s)
    {
        [|if (s is null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    public C(" + refKind + @" string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOutParameter()
        {
            var testCode = @"
using System;

class C
{
    public {|CS0177:C|}(out string s)
    {
        if ({|CS0269:s|} is null)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        [InlineData("object")]
        [InlineData("C")]
        public async Task TestReferenceEqualsCheck(string className)
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    private readonly string s;
    void M1(string s)
    {
        [|if (" + className + @".ReferenceEquals(s, null))
        {
            throw new ArgumentNullException(nameof(s));
        }|]
    }

    void M2(string s)
    {
        [|if (" + className + @".ReferenceEquals(null, s))
        {
            throw new ArgumentNullException(nameof(s));
        }|]
    }
}",
                FixedCode = @"
using System;

class C
{
    private readonly string s;
    void M1(string s!!)
    {
    }

    void M2(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotCustomReferenceEqualsCheck()
        {
            var testCode = @"
using System;

class C
{
    private readonly string s;
    void M(string s)
    {
        if (ReferenceEquals(s, null))
        {
            throw new ArgumentNullException(nameof(s));
        }
    }

    bool ReferenceEquals(object o1, object o2) => false;
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotEqualitySwapped()
        {
            // https://github.com/dotnet/roslyn/issues/58699
            var testCode = @"using System;

class C
{
    void M(string s)
    {
        if (null != (object)s)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestMissingPreCSharp11()
        {
            var testCode = @"
using System;

class C
{
    void M(string s)
    {
        if ((object)s == null)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOnlyForObjectCast()
        {
            var testCode = @"using System;

class C
{
    void M(string s)
    {
        if ((string)s == null)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFixAll1()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string s1, string s2)
    {
        [|if ((object)s1 == null)
            throw new ArgumentNullException(nameof(s1));|]

        [|if (null == (object)s2)
            throw new ArgumentNullException(nameof(s2));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string s1!!, string s2!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNested1()
        {
            var testCode = @"using System;

class C
{
    void M(string s2)
    {
        if ((object)((object)s2 == null) == null)
            throw new ArgumentNullException(nameof(s2));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNestedStatements()
        {
            var testCode = @"using System;

class C
{
    void M(string s2)
    {
        {
            if (s2 == null)
                throw new ArgumentNullException(nameof(s2));
        }
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestConstrainedTypeParameter()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M<T>(T s) where T : class
    {
        [|if ((object)s == null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M<T>(T s!!) where T : class
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOnStructConstrainedTypeParameter()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M1<T>(T s) where T : struct
    {
        if ((object)s == null)
            throw new ArgumentNullException(nameof(s));
    }

    void M2<T>(T? s) where T : struct
    {
        [|if ((object)s == null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M1<T>(T s) where T : struct
    {
        if ((object)s == null)
            throw new ArgumentNullException(nameof(s));
    }

    void M2<T>(T? s!!) where T : struct
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestUnconstrainedTypeParameter()
        {
            await new VerifyCS.Test()
            {
                TestCode =
@"using System;

class C
{
    void M<T>(T s)
    {
        [|if ((object)s == null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode =
@"using System;

class C
{
    void M<T>(T s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestComplexExpr()
        {
            var testCode = @"using System;

class C
{
    void M(string[] s)
    {
        if ((object)s[0] == null)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestComplexExpr2()
        {
            var testCode = @"using System;

class C
{
    void M(string s1, string s2)
    {
        if (s1 == null || s2 == null)
            throw new ArgumentNullException(nameof(s1));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotOnDefault()
        {
            var testCode = @"
using System;

class C
{
    void M(string s)
    {
        if ((object)s == default)
            throw new ArgumentNullException(nameof(s));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestOptionalParameter()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string s = ""a"")
    {
        [|if ((object)s == null)
            throw new ArgumentNullException(nameof(s));|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string s!! = ""a"")
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithStringExceptionArgument()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string s)
    {
        [|if (s == null)
            throw new ArgumentNullException(""s"");|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithStringExceptionArgument2()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    void M(string HelloWorld)
    {
        [|if (HelloWorld == null)
            throw new ArgumentNullException(""Hello"" + ""World"");|]
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(string HelloWorld!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotWithUnexpectedExceptionArgument()
        {
            var testCode = @"using System;

class C
{
    void M(string s)
    {
        if (s == null)
            throw new ArgumentNullException(""banana"");
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithExceptionNoArguments()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    void M(string s)
    {
        [|if (s == null)
            throw new ArgumentNullException();|]
    }
}",
                FixedCode = @"using System;

class C
{
    void M(string s!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestSimpleLambda()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    Action<string> lambda = x =>
    {
        [|if (x is null)
            throw new ArgumentNullException(nameof(x));|]
    };
}
",
                FixedCode = @"using System;

class C
{
    Action<string> lambda = x!! =>
    {
    };
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestParenthesizedLambdaNoType()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    Action<string> lambda = (x) =>
    {
        [|if (x is null)
            throw new ArgumentNullException(nameof(x));|]
    };
}
",
                FixedCode = @"using System;

class C
{
    Action<string> lambda = (x!!) =>
    {
    };
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestParenthesizedLambdaWithType()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    Action<string> lambda = (string x) =>
    {
        [|if (x is null)
            throw new ArgumentNullException(nameof(x));|]
    };
}
",
                FixedCode = @"using System;

class C
{
    Action<string> lambda = (string x!!) =>
    {
    };
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestDiscard1()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    Action<string> lambda = _ =>
    {
        [|if (_ is null)
            throw new ArgumentNullException(nameof(_));|]
    };
}
",
                FixedCode = @"using System;

class C
{
    Action<string> lambda = _!! =>
    {
    };
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestDiscard2()
        {
            var testCode = @"
using System;

class C
{
    Action<string, string> lambda = (_, _) =>
    {
        if ({|CS0103:_|} is null)
            throw new ArgumentNullException(nameof({|CS0103:_|}));
    };
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestAnonymousMethod()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;

class C
{
    Action<string> lambda = delegate (string x)
    {
        [|if (x is null)
            throw new ArgumentNullException(nameof(x));|]
    };
}
",
                FixedCode = @"using System;

class C
{
    Action<string> lambda = delegate (string x!!)
    {
    };
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestLocalFunctionWithinAccessor()
        {
            var testCode = @"using System;
class C
{
    private int _p;

    public int P
    {
        get => _p;
        set
        {
            local();

            void local()
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _p = value;
            }
        }
    }
}
";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestLocalFunctionCapturingParameter()
        {
            var testCode = @"using System;
class C
{
    void M(string param)
    {
        local();

        void local()
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));
        }
    }
}
";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotAfterReturn1()
        {
            var testCode = @"using System;
class C
{
    static void M(C c)
    {
        return;
        if (c == null)
            throw new ArgumentNullException(nameof(c));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotAfterReturn2()
        {
            var testCode = @"using System;
class C
{
    static void M(C c, bool b)
    {
        if (b)
            return;

        if (c == null)
            throw new ArgumentNullException(nameof(c));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotAfterReturn3()
        {
            var testCode = @"using System;
class C
{
    static void M(C c, bool b)
    {
        if (b)
        {
            return;
        }

        if (c == null)
            throw new ArgumentNullException(nameof(c));
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNotAfterReturn4()
        {
            var testCode = @"using System;
class C
{
    static int M(C c, bool b)
    {
        if (b)
        {
            return 0;
        }

        if (c == null)
            throw new ArgumentNullException(nameof(c));

        return 0;
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestBeforeReturn1()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static int M(C c)
    {
        [|if (c == null)
            throw new ArgumentNullException(nameof(c));|]

        return 0;
    }
}",
                FixedCode = @"using System;
class C
{
    static int M(C c!!)
    {
        return 0;
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestBeforeReturn2()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static void M(C c, bool b)
    {
        [|if (c == null)
            throw new ArgumentNullException(nameof(c));|]

        if (b)
            return;
    }
}",
                FixedCode = @"using System;
class C
{
    static void M(C c!!, bool b)
    {
        if (b)
            return;
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestAfterThrow1()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static void M(C c)
    {
        throw new ArgumentOutOfRangeException();

        [|if (c == null)
            throw new ArgumentNullException(nameof(c));|]
    }
}",
                FixedCode = @"using System;
class C
{
    static void M(C c!!)
    {
        throw new ArgumentOutOfRangeException();
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestAfterThrow2()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static void M(int i, C c)
    {
        if (i < 0)
            throw new ArgumentOutOfRangeException();

        [|if (c == null)
            throw new ArgumentNullException(nameof(c));|]
    }
}",
                FixedCode = @"using System;
class C
{
    static void M(int i, C c!!)
    {
        if (i < 0)
            throw new ArgumentOutOfRangeException();
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestExtensionMethod()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
interface I { }
static class C
{
    static void M(this I i)
    {
        [|if (i == null)
            throw new ArgumentNullException(nameof(i));|]
    }
}",
                FixedCode = @"using System;
interface I { }
static class C
{
    static void M(this I i!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckInBaseClause()
        {
            // Applying code fix introduces CS8602: Dereference of a possibly null reference.
            // In this case, the user needs to change '?.' to '.' in the base clause.
            await new VerifyCS.Test()
            {
                TestCode = @"#nullable enable
using System;

class Base { public Base(string? x) { } }
class C : Base
{
    public C(string x) : base(x?.ToString())
    {
        [|if (x == null) { throw new ArgumentNullException(); }|]
        x.ToString();
    }
}",
                FixedCode = @"#nullable enable
using System;

class Base { public Base(string? x) { } }
class C : Base
{
    public C(string x!!) : base(x?.ToString())
    {
        {|CS8602:x|}.ToString();
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestLabeledNullCheck()
        {
            // A label syntactically contains the following statement
            // and we don't offer the fix on nested statements within the body.
            var testCode = @"using System;
public class C
{
    public void M(string s)
    {
        label1:
        if (s == null) throw new ArgumentNullException();
    }
}";
            await new VerifyCS.Test()
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestReturnBackwardsBranchToNullCheck()
        {
            // In this scenario, applying the fix makes us throw instead of returning.
            // However, we think it's esoteric enough that we don't need to address it.
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s, int i = 0)
    {
        goto label2;
        
        label1:
        ;
        [|if (s == null) throw new ArgumentNullException();|]
        return;
        
        label2:
        if (i < 0) return;
        goto label1;
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s!!, int i = 0)
    {
        goto label2;
        
        label1:
        ;
        return;
        
        label2:
        if (i < 0) return;
        goto label1;
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse1()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s)
    {
        [|if (s == null) throw new ArgumentNullException();|]
        else Console.Write(1);
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s!!)
    {
        Console.Write(1);
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse2()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s)
    {
        [|if (s == null) throw new ArgumentNullException();|]
        else if (s.Length == 0) Console.Write(1);
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s!!)
    {
        if (s.Length == 0) Console.Write(1);
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse3()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s)
    {
        Console.Write(0);
        [|if (s == null) throw new ArgumentNullException();|]
        else
        {
            Console.Write(1);
            Console.Write(2);
        }
        Console.Write(3);
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s!!)
    {
        Console.Write(0);
        Console.Write(1);
        Console.Write(2);
        Console.Write(3);
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse4()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s)
    {
        [|if (s == null) throw new ArgumentNullException();|]
        else if (s.Length == 0)
        {
            Console.Write(1);
            Console.Write(2);
        }
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s!!)
    {
        if (s.Length == 0)
        {
            Console.Write(1);
            Console.Write(2);
        }
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse5()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s1, string s2)
    {
        [|if (s1 == null) throw new ArgumentNullException();|]
        else if (s2 == null) throw new ArgumentNullException();
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s1!!, string s2!!)
    {
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext,
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse6()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s1)
    {
        [|if (s1 == null) throw new ArgumentNullException();|]
        else /* comment1 */ s1.ToString() /* comment2 */; // comment3
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s1!!)
    {
        s1.ToString() /* comment2 */; // comment3
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullCheckWithElse7()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
public class C
{
    public void M(string s1)
    {
        [|if (s1 == null) throw new ArgumentNullException();|]
        else
        {
            /* comment1 */ s1.ToString() /* comment2 */; // comment3
        }
    }
}",
                FixedCode = @"using System;
public class C
{
    public void M(string s1!!)
    {
        /* comment1 */
        s1.ToString() /* comment2 */; // comment3
    }
}",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithUserDefinedOperator()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    public static bool operator ==(C c1, C c2) => true;
    public static bool operator !=(C c1, C c2) => false;

    static void M(C c)
    {
        [|if (c == null)
            throw new ArgumentNullException(nameof(c));|]
    }
}
",
                FixedCode = @"using System;
class C
{
    public static bool operator ==(C c1, C c2) => true;
    public static bool operator !=(C c1, C c2) => false;

    static void M(C c!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithUnusedUserDefinedOperator()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    public static bool operator ==(C c1, C c2) => true;
    public static bool operator !=(C c1, C c2) => false;

    static void M(C c)
    {
        [|if ((object)c == null)
            throw new ArgumentNullException(nameof(c));|]
    }
}
",
                FixedCode = @"using System;
class C
{
    public static bool operator ==(C c1, C c2) => true;
    public static bool operator !=(C c1, C c2) => false;

    static void M(C c!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestIsWithUnusedUserDefinedOperator()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    public static bool operator ==(C c1, C c2) => true;
    public static bool operator !=(C c1, C c2) => false;

    static void M(C c)
    {
        [|if (c is null)
            throw new ArgumentNullException(nameof(c));|]
    }
}
",
                FixedCode = @"using System;
class C
{
    public static bool operator ==(C c1, C c2) => true;
    public static bool operator !=(C c1, C c2) => false;

    static void M(C c!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithPreprocessorDirective()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static void M(C c)
    {
#nullable enable
        [|if ((object)c == null)
            throw new ArgumentNullException(nameof(c));|]
#nullable disable
    }
}
",
                FixedCode = @"using System;
class C
{
    static void M(C c!!)
    {
#nullable disable
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithIfPreprocessorDirective()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"#define DEBUG
using System;
class C
{
    static void M(C c)
    {
#if DEBUG
        [|if ((object)c == null)
            throw new ArgumentNullException(nameof(c));|]
#endif
    }
}
",
                FixedCode = @"#define DEBUG
using System;
class C
{
    static void M(C c!!)
    {

#if DEBUG
#endif
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestWithAlias()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using Ex = System.ArgumentNullException;
class C
{
    static void M(C c)
    {
        [|if ((object)c == null)
            throw new Ex(nameof(c));|]
    }
}
",
                FixedCode = @"using Ex = System.ArgumentNullException;
class C
{
    static void M(C c!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestPointer()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static unsafe void M(int* ptr)
    {
        [|if (ptr == null)
            throw new ArgumentNullException(nameof(ptr));|]
    }
}
",
                FixedCode = @"using System;
class C
{
    static unsafe void M(int* ptr!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestFunctionPointer()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static unsafe void M(delegate*<int, void> ptr)
    {
        [|if (ptr == null)
            throw new ArgumentNullException(nameof(ptr));|]
    }
}
",
                FixedCode = @"using System;
class C
{
    static unsafe void M(delegate*<int, void> ptr!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestNullableValueType()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"using System;
class C
{
    static void M(int? value)
    {
        [|if (value == null)
            throw new ArgumentNullException(nameof(value));|]
    }
}
",
                FixedCode = @"using System;
class C
{
    static void M(int? value!!)
    {
    }
}
",
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
        public async Task TestPartialMethod()
        {
            var partialDeclaration = @"
partial class C
{
    static partial void M(C c);
}
";

            await new VerifyCS.Test()
            {
                TestState =
                {
                    Sources =
                    {
                        partialDeclaration,
                        @"using System;
partial class C
{
    static partial void M(C c)
    {
        [|if ((object)c == null)
            throw new ArgumentNullException(nameof(c));|]
    }
}
"
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        partialDeclaration,
                        @"using System;
partial class C
{
    static partial void M(C c!!)
    {
    }
}
"
                    }
                },
                LanguageVersion = LanguageVersionExtensions.CSharpNext
            }.RunAsync();
        }
    }
}
