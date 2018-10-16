// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    using Verify = CSharpCodeFixVerifier<CSharpFormattingAnalyzer, CSharpFormattingCodeFixProvider, XUnitVerifier>;

    public class FormattingAnalyzerTests
    {
        [Fact]
        public async Task TestAlreadyFormatted()
        {
            var testCode = @"
class MyClass
{
    void MyMethod()
    {
    }
}
";

            await Verify.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task TestNeedsIndentation()
        {
            var testCode = @"
class MyClass
{
  $$void MyMethod()
  $${
  $$}
}
";
            var fixedCode = @"
class MyClass
{
    void MyMethod()
    {
    }
}
";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestWhitespaceBetweenMethods1()
        {
            var testCode = @"
class MyClass
{
    void MyMethod1()
    {
    }
[| |]
    void MyMethod2()
    {
    }
}
";
            var fixedCode = @"
class MyClass
{
    void MyMethod1()
    {
    }

    void MyMethod2()
    {
    }
}
";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestWhitespaceBetweenMethods2()
        {
            var testCode = @"
class MyClass
{
    void MyMethod1()
    {
    }[| |]

    void MyMethod2()
    {
    }
}
";
            var fixedCode = @"
class MyClass
{
    void MyMethod1()
    {
    }

    void MyMethod2()
    {
    }
}
";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestWhitespaceBetweenMethods3()
        {
            // This example has trailing whitespace on both lines preceding MyMethod2
            var testCode = @"
class MyClass
{
    void MyMethod1()
    {
    }[| 
 
    |]void MyMethod2()
    {
    }
}
";
            var fixedCode = @"
class MyClass
{
    void MyMethod1()
    {
    }

    void MyMethod2()
    {
    }
}
";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestOverIndentation()
        {
            var testCode = @"
class MyClass
{
    [|    |]void MyMethod()
    [|    |]{
    [|    |]}
}
";
            var fixedCode = @"
class MyClass
{
    void MyMethod()
    {
    }
}
";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestIncrementalFixesFullLine()
        {
            var testCode = @"
class MyClass
{
    int Property1$${$$get;$$set;$$}
    int Property2$${$$get;$$}
}
";
            var fixedCode = @"
class MyClass
{
    int Property1 { get; set; }
    int Property2 { get; }
}
";

            await new CSharpCodeFixTest<CSharpFormattingAnalyzer, CSharpFormattingCodeFixProvider, XUnitVerifier>
            {
                TestCode = testCode,
                FixedCode = fixedCode,

                // Each application of a single code fix covers all diagnostics on the same line. In total, two lines
                // require changes so the number of incremental iterations is exactly 2.
                NumberOfIncrementalIterations = 2,
            }.RunAsync();
        }

        [Fact]
        public async Task TestEditorConfigUsed()
        {
            var testCode = @"
class MyClass {
    void MyMethod()[| |]{
    }
}
";
            var fixedCode = @"
class MyClass {
    void MyMethod()
    {
    }
}
";
            var editorConfig = @"
root = true

[*.cs]
csharp_new_line_before_open_brace = methods
";

            var testDirectoryName = Path.GetRandomFileName();
            Directory.CreateDirectory(testDirectoryName);
            try
            {
                File.WriteAllText(Path.Combine(testDirectoryName, ".editorconfig"), editorConfig);

                // The contents of this file are ignored, but the coding conventions library checks for existence before
                // .editorconfig is used.
                File.WriteAllText(Path.Combine(testDirectoryName, "Test0.cs"), string.Empty);

                await new CSharpCodeFixTest<CSharpFormattingAnalyzer, CSharpFormattingCodeFixProvider, XUnitVerifier>
                {
                    TestState = { Sources = { (Path.GetFullPath(Path.Combine(testDirectoryName, "Test0.cs")), testCode) } },
                    FixedState = { Sources = { (Path.GetFullPath(Path.Combine(testDirectoryName, "Test0.cs")), fixedCode) } },
                }.RunAsync();
            }
            finally
            {
                Directory.Delete(testDirectoryName, true);
            }
        }
    }
}
