// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseUTF8StringLiteral
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseUTF8StringLiteralDiagnosticAnalyzer,
        UseUTF8StringLiteralCodeFixProvider>;

    public class UseUTF8StringLiteralTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotInAttribute()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class MyAttribute : System.Attribute
{
    public MyAttribute(byte[] data)
    {
    }
}

public class C
{
    [MyAttribute(new byte[] { 65, 66, 67 })]
    public void M()
    {
    }
}",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotInCSharp10()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[] { 65, 66, 67 };
    }
}",
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWithoutInitializer()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[10];
    }
}",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotInExpressionTree()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
using System;
using System.Linq.Expressions;

public class C
{
    public void M()
    {
        N(() => new byte[] { 65, 66, 67 });
    }

    public void N(Expression<Func<byte[]>> f)
    {
    }
}",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhenNotByteArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new int[] { 65, 66, 67 };
    }
}",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhenOptionNotSet()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[] { 65, 66, 67 };
    }
}",
                EditorConfig = @"
[*.cs]
csharp_style_prefer_utf8_string_literal = false
",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhenNonLiteralElement()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[] { 65, GetB(), 67 };
    }

    public byte GetB() => 66;
}",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestSimpleByteArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 65, 66, 67 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestImplicitTypedArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new [] { (byte)65, (byte)66, (byte)67 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestWithExplicitCasts()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 65, (byte)'B', 67 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestHexLiterals()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 0x40, 0x41, 0x42 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestEmptyArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = """"u8;
    }
}",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }
    }
}
