// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
csharp_style_prefer_utf8_string_literals = false
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
        public async Task TestNotWhenMultidimensionalArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M()
    {
        var x = new byte[,] { { 65, 66 }, { 67, 68 }, { 69, 70 } };
    }
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
        var x = [|new|] byte[] { 65, 66, 67 };
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
        var x = [|new|] [] { (byte)65, (byte)66, (byte)67 };
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
        var x = [|new|] byte[] { 65, (byte)'B', 67 };
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
        var x = [|new|] byte[] { 0x41, 0x42, 0x43 };
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
        var x = [|new|] byte[] { 60 + 5, 60 + 6, 60 + 7 };
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
        var x = [|new|] byte[] { };
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
        var x = [|new|] byte[] { 65, 66, 67 }; // I wish this byte array was easier to read
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
        M(/* arrays are */ [|new|] byte[] { 65, 66, 67 } /* cool */);
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
        var x = [|new|] byte[] { 0x41, 0x42, 0x43 };
        var y = [|new|] byte[] { 0x44, 0x45, 0x46 };
        var z = [|new|] byte[] { 0x47, 0x48, 0x49 };
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
        var x = [|new|] byte[] { 34, 92, 0, 7, 8, 12, 10, 13, 9, 11 };
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
        var x = [|new|] byte[] { 240, 159, 152, 128 };
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
        var x = [|new|] byte[] { 239, 191, 189 };
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestCollectionInitializer()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    void M(C c)
    {
        // Each literal of the three is a separate IArrayCreationOperation
        // Lowered code is similar to:
        /*
            C c = new C();
            c.Add(new byte[] { 65 });
            c.Add(new byte[] { 66 });
            c.Add(new byte[] { 67 });
        */
        c = new() { [|65|], [|66|], [|67|] };
    }

    public void Add(params byte[] bytes)
    {
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}",
                FixedCode =
@"
using System.Collections;
using System.Collections.Generic;

class C : IEnumerable<int>
{
    void M(C c)
    {
        // Each literal of the three is a separate IArrayCreationOperation
        // Lowered code is similar to:
        /*
            C c = new C();
            c.Add(new byte[] { 65 });
            c.Add(new byte[] { 66 });
            c.Add(new byte[] { 67 });
        */
        c = new() { ""A""u8, ""B""u8, ""C""u8 };
    }

    public void Add(params byte[] bytes)
    {
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestUsingWithParamArray()
        {
            // From: https://github.com/dotnet/roslyn/blob/0c7c0b33f0871fc4308eb2d75d77b87fc9293290/src/Compilers/CSharp/Test/IOperation/IOperation/IOperationTests_IUsingStatement.cs#L1189-L1194
            // There is an array creation operation for the param array
            await new VerifyCS.Test
            {
                TestCode =
@"
class C
{
    public static void M1()
    {
        using(var s = new S())
        { 
        }
    }
}
ref struct S
{
    public void Dispose(int a = 1, bool b = true, params byte[] others) { }
}",
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
    private static readonly byte[] _bytes = ""{stringValue}""u8;
}}
",
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
            await new VerifyCS.Test
            {
                TestCode =
$@"
public class C
{{
    private static readonly byte[] _bytes = new byte[] {{ {string.Join(", ", bytes)} }};
}}
",
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
        M(""ABC""u8);
    }
}
",
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
        M(1, ""ABC""u8);
    }
}
",
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
    public void M(params byte[] b)
    {
        M([|65|]);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(""A""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray4()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(/* hi */ [|65|] /* there */);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(/* hi */ ""A""u8 /* there */);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray5()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M([|65, 66, 67|]);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray6()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(/* hi */ [|65, 66, 67|] /* there */);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(params byte[] b)
    {
        M(/* hi */ ""ABC""u8 /* there */);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray7()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1);
    }
}
",
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray8()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1, [|65, 66, 67|]);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1, ""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray9()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1, /* hi */ [|65|] /* there */);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1, /* hi */ ""A""u8 /* there */);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray10()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1, /* hi */ [|65, 66, 67|] /* there */);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int x, params byte[] b)
    {
        M(1, /* hi */ ""ABC""u8 /* there */);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray11()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int x, int y, int z, params byte[] b)
    {
        M( /* b1 */ 1 /* a1 */, /* b2 */ 2 /* a2 */, /* b3 */ 3 /* a3 */, /* b4 */ [|65, /* x1 */ 66, /* x2 */  67|] /* a4 */);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int x, int y, int z, params byte[] b)
    {
        M( /* b1 */ 1 /* a1 */, /* b2 */ 2 /* a2 */, /* b3 */ 3 /* a3 */, /* b4 */ ""ABC""u8 /* a4 */);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray12()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public C(params byte[] b)
    {
        new C([|65, 66, 67|]);
    }
}
",
                FixedCode =
@"
public class C
{
    public C(params byte[] b)
    {
        new C(""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray13()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public int this[params byte[] bytes]
    {
        get => 0;
    }

    public void M()
    {
        _ = this[[|65, 66, 67|]];
    }
}
",
                FixedCode =
@"
public class C
{
    public int this[params byte[] bytes]
    {
        get => 0;
    }

    public void M()
    {
        _ = this[""ABC""u8];
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray14()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public record C1(int x) : B([|65, 66, 67|]);

public record C2(params byte[] Bytes) : B(Bytes);

public record B(params byte[] Bytes)
{
    public void M()
    {
        new C1(1);
        new C2([|65, 66, 67|]);
        new B([|65, 66, 67|]);
    }
}
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}
",
                FixedCode =
@"
public record C1(int x) : B(""ABC""u8);

public record C2(params byte[] Bytes) : B(Bytes);

public record B(params byte[] Bytes)
{
    public void M()
    {
        new C1(1);
        new C2(""ABC""u8);
        new B(""ABC""u8);
    }
}
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray15()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C1 : B
{
    public C1(int x)
        : base([|65, 66, 67|])
    {
    }
}

public class C2 : B
{
    public C2(params byte[] Bytes)
        : base(Bytes)
    {
    }
}

public class B
{
    public B(string x, params byte[] bytes)
        : this(bytes)
    {
    }

    public B(int x)
        : this([|65, 66, 67|])
    {
    }

    public B(params byte[] bytes)
    {
        new C1(1);
        new C2([|65, 66, 67|]);
        new B([|65, 66, 67|]);
        new B(""a"", [|65, 66, 67|]);
    }
}
",
                FixedCode =
@"
public class C1 : B
{
    public C1(int x)
        : base(""ABC""u8)
    {
    }
}

public class C2 : B
{
    public C2(params byte[] Bytes)
        : base(Bytes)
    {
    }
}

public class B
{
    public B(string x, params byte[] bytes)
        : this(bytes)
    {
    }

    public B(int x)
        : this(""ABC""u8)
    {
    }

    public B(params byte[] bytes)
    {
        new C1(1);
        new C2(""ABC""u8);
        new B(""ABC""u8);
        new B(""a"", ""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray16()
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
        M(new int[] { 1 }, ""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestParamArray17()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(int[] i, params byte[] b)
    {
        M(new int[] { 1 }, [|65, 66, 67|]);
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(int[] i, params byte[] b)
    {
        M(new int[] { 1 }, ""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseUTF8StringLiteral)]
        public async Task TestMultidimensionalArray()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
public class C
{
    public void M(byte[][] i, byte[] b)
    {
        M(new byte[][] { [|new|] byte[] { 65, 66, 67 }, [|new|] byte[] { 65, 66, 67 } }, [|new|] byte[] { 65, 66, 67 });
    }
}
",
                FixedCode =
@"
public class C
{
    public void M(byte[][] i, byte[] b)
    {
        M(new byte[][] { ""ABC""u8, ""ABC""u8 }, ""ABC""u8);
    }
}
",
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.Preview
            }.RunAsync();
        }
    }
}
