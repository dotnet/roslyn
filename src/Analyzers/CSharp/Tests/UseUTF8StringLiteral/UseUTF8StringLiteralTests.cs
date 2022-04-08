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
        public async Task TestConstant()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    private const byte B = 66;
    public void M()
    {
        var x = {|IDE0230:new byte[] { 65, B, 67 }|};
    }
}",
                FixedCode =
@"
public class C
{
    private const byte B = 66;
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
        public async Task TestImplicitArray()
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
        public async Task TestExplicitCast()
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
        public async Task TestHexLiteral()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 0x41, 0x42, 0x43 }|};
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
        public async Task TestBinaryExpression()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 60 + 5, 60 + 6, 60 + 7 }|};
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestTrivia1()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 65, 66, 67 }|}; // I wish this byte array was easier to read
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8; // I wish this byte array was easier to read
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestTrivia2()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(byte[] b)
    {
        M(/* arrays are */ {|IDE0230:new byte[] { 65, 66, 67 }|} /* cool */);
    }
}",
                FixedCode =
@"
public class C
{
    public void M(byte[] b)
    {
        M(/* arrays are */ ""ABC""u8 /* cool */);
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestMultiple()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 0x41, 0x42, 0x43 }|};
        var y = {|IDE0230:new byte[] { 0x44, 0x45, 0x46 }|};
        var z = {|IDE0230:new byte[] { 0x47, 0x48, 0x49 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8;
        var y = ""DEF""u8;
        var z = ""GHI""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestEscapeChars()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 34, 92, 0, 7, 8, 12, 10, 13, 9, 11 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""\""\\\0\a\b\f\n\r\t\v""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestEmoji()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 240, 159, 152, 128 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""😀""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestHalfEmoji1()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[] { 240, 159 };
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestHalfEmoji2()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[] { 152, 128 };
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestHalfEmoji3()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[] { 65, 152, 128, 66 };
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestUnicodeReplacementChar()
        {
            // The unicode replacement character is what is returned when, for example, an unpaired
            // surrogate is converted to a UTF8 string. This test just ensures that the presence of
            // that character isn't being used to detect a failure state of some kind.
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = {|IDE0230:new byte[] { 239, 191, 189 }|};
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""�""u8;
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }
    }
}
