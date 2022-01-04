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
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseParameterNullChecking
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpUseParameterNullCheckingDiagnosticAnalyzer, CSharpUseParameterNullCheckingCodeFixProvider>;

    public partial class UseParameterNullCheckingTests
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
        [|this.s = s ?? throw new ArgumentNullException(nameof(s));|]
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
        public async Task TestReferenceEqualsCheck()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"
using System;

class C
{
    private readonly string s;
    void M(string s)
    {
        [|if (object.ReferenceEquals(s, null))
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
    void M(string s!!)
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
        public async Task TestNotWithExceptionNoArguments()
        {
            var testCode = @"using System;

class C
{
    void M(string s)
    {
        if (s == null)
            throw new ArgumentNullException();
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
    }
}
