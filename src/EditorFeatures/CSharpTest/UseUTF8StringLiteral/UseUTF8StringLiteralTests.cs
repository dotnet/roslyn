// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseUTF8StringLiteral
{
    using VerifyCS = CSharpCodeRefactoringVerifier<UseUTF8StringLiteralRefactoringProvider>;

    public class UseUTF8StringLiteralTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotInAttribute()
        {
            var code = @"
public class MyAttribute : System.Attribute
{
    public MyAttribute(byte[] data)
    {
    }
}

public class C
{
    [MyAttribute([|new|] byte[] { 65, 66, 67 })]
    public void M()
    {
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhereNoReadOnlySpan()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[] { 65, 66, 67 };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net20.Default,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotInCSharp10()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[] { 65, 66, 67 };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWithoutInitializer()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[10];
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotInExpressionTree()
        {
            var code = @"
using System;
using System.Linq.Expressions;

public class C
{
    public void M()
    {
        N(() => [|new|] byte[] { 65, 66, 67 });
    }

    public void N(Expression<Func<byte[]>> f)
    {
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhenNotByteArray()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] int[] { 65, 66, 67 };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhenNonLiteralElement()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[] { 65, GetB(), 67 };
    }

    public byte GetB() => 66;
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotWhenMultidimensionalArray()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[,] { { 65, 66 }, { 67, 68 }, { 69, 70 } };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 65, 66, 67 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 65, B, 67 };
    }
}",
                FixedCode =
@"
public class C
{
    private const byte B = 66;
    public void M()
    {
        var x = ""ABC""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] [] { (byte)65, (byte)66, (byte)67 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 65, (byte)'B', 67 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 0x41, 0x42, 0x43 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 60 + 5, 60 + 6, 60 + 7 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = """"u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 65, 66, 67 }; // I wish this byte array was easier to read
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""ABC""u8.ToArray(); // I wish this byte array was easier to read
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        M(/* arrays are */ [|new|] byte[] { 65, 66, 67 } /* cool */);
    }
}",
                FixedCode =
@"
public class C
{
    public void M(byte[] b)
    {
        M(/* arrays are */ ""ABC""u8.ToArray() /* cool */);
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 34, 92, 0, 7, 8, 12, 10, 13, 9, 11 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""\""\\\0\a\b\f\n\r\t\v""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 240, 159, 152, 128 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""😀""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotForHalfEmoji1()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[] { 240, 159 };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotForHalfEmoji2()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[] { 152, 128 };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestNotForHalfEmoji3()
        {
            var code = @"
public class C
{
    public void M()
    {
        var x = [|new|] byte[] { 65, 152, 128, 66 };
    }
}";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
        var x = [|new|] byte[] { 239, 191, 189 };
    }
}",
                FixedCode =
@"
public class C
{
    public void M()
    {
        var x = ""�""u8.ToArray();
    }
}",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        // Various cases copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/Tests/System/Net/aspnetcore/Http3/QPackDecoderTest.cs
        [InlineData(new byte[] { 0x37, 0x02, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x6c, 0x61, 0x74, 0x65 }, "7translate")]
        [InlineData(new byte[] { 0x3f, 0x01 }, "?")]
        public async Task TestValidUTF8Strings(byte[] bytes, string stringValue)
        {
            await new VerifyCS.Test
            {
                TestCode =
$@"
public class C
{{
    private static readonly byte[] _bytes = [|new|] byte[] {{ {string.Join(", ", bytes)} }};
}}
",
                FixedCode =
$@"
public class C
{{
    private static readonly byte[] _bytes = ""{stringValue}""u8.ToArray();
}}
",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();

            // Lets make sure there aren't any false positives here, and make sure the byte array actually
            // correctly round-trips via UTF8
            var newStringValue = Encoding.UTF8.GetString(bytes);
            Assert.Equal(stringValue, newStringValue);
            var newBytes = Encoding.UTF8.GetBytes(stringValue);
            Assert.Equal(bytes, newBytes);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        // Various cases copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/Tests/System/Net/aspnetcore/Http2/HuffmanDecodingTests.cs
        [InlineData(new byte[] { 0xff, 0xcf })]
        [InlineData(new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1111 })]
        [InlineData(new byte[] { 0xb6, 0xb9, 0xac, 0x1c, 0x85, 0x58, 0xd5, 0x20, 0xa4, 0xb6, 0xc2, 0xad, 0x61, 0x7b, 0x5a, 0x54, 0x25, 0x1f })]
        [InlineData(new byte[] { 0xfe, 0x53 })]
        [InlineData(new byte[] { 0xff, 0xff, 0xf6, 0xff, 0xff, 0xfd, 0x68 })]
        [InlineData(new byte[] { 0xff, 0xff, 0xf9, 0xff, 0xff, 0xfd, 0x86 })]
        // _headerNameHuffmanBytes from https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/Tests/System/Net/aspnetcore/Http3/QPackDecoderTest.cs
        [InlineData(new byte[] { 0xa8, 0xbe, 0x16, 0x9c, 0xa3, 0x90, 0xb6, 0x7f })]
        public async Task TestInvalidUTF8Strings(byte[] bytes)
        {
            var code = $@"
public class C
{{
    private static readonly byte[] _bytes = [|new|] byte[] {{ {string.Join(", ", bytes)} }};
}}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();

            // Lets make sure there aren't any false negatives here, and see if the byte array would actually
            // correctly round-trip via UTF8
            var stringValue = Encoding.UTF8.GetString(bytes);
            var newBytes = Encoding.UTF8.GetBytes(stringValue);
            Assert.NotEqual(bytes, newBytes);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray1()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M([|new|] byte[] { 65, 66, 67 });
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(""ABC""u8.ToArray());
    }
}
",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray2()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int i, params byte[] b)
    {
        M(1, [|new|] byte[] { 65, 66, 67 });
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int i, params byte[] b)
    {
        M(1, ""ABC""u8.ToArray());
    }
}
",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray3()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int[] i, byte[] b)
    {
        M(new int[] { 1 }, [|new|] byte[] { 65, 66, 67 });
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int[] i, byte[] b)
    {
        M(new int[] { 1 }, ""ABC""u8.ToArray());
    }
}
",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestInMultidimensionalArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(byte[][] i, byte[] b)
    {
        M(new byte[][] { [|new|] byte[] { 65, 66, 67 }, new byte[] { 65, 66, 67 } }, new byte[] { 65, 66, 67 });
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(byte[][] i, byte[] b)
    {
        M(new byte[][] { ""ABC""u8.ToArray(), new byte[] { 65, 66, 67 } }, new byte[] { 65, 66, 67 });
    }
}
",
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }
    }
}
