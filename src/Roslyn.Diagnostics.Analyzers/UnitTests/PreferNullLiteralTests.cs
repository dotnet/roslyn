// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.PreferNullLiteral,
    Roslyn.Diagnostics.CSharp.Analyzers.PreferNullLiteralCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class PreferNullLiteralTests
    {
        [Theory]
        [InlineData("default")]
        [InlineData("default(object)")]
        public async Task PreferNullLiteral_Class(string defaultValueExpression)
        {
            var source = $@"
class Type
{{
    object Method()
    {{
        return [|{defaultValueExpression}|];
    }}
}}
";
            var fixedSource = @"
class Type
{
    object Method()
    {
        return null;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Theory]
        [InlineData("default")]
        [InlineData("default(object)")]
        public async Task PreferNullLiteral_DefaultParameterValue(string defaultValueExpression)
        {
            var source = $@"
class Type
{{
    void Method(object value = [|{defaultValueExpression}|])
    {{
    }}
}}
";
            var fixedSource = @"
class Type
{
    void Method(object value = null)
    {
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task PreferNullLiteral_Struct()
        {
            var source = @"
class Type
{
    int Method()
    {
        return default;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_StructConvertedToReferenceType()
        {
            var source = @"
class Type
{
    object Method()
    {
        return default(int);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_UnconstrainedGeneric()
        {
            var source = @"
class Type
{
    T Method<T>()
    {
        return default;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_GenericConstrainedToReferenceType()
        {
            var source = @"
class Type
{
    T Method<T>()
        where T : class
    {
        return [|default|];
    }
}
";
            var fixedSource = @"
class Type
{
    T Method<T>()
        where T : class
    {
        return null;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task PreferNullLiteral_GenericConstrainedToInterface()
        {
            var source = @"
class Type
{
    T Method<T>()
        where T : System.IComparable
    {
        return default;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task PreferNullLiteral_GenericConstrainedToValueType()
        {
            var source = @"
class Type
{
    T Method<T>()
        where T : struct
    {
        return default;
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }
    }
}
