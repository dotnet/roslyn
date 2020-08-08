// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
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
        public async Task TestNeedsIndentationButSuppressed()
        {
            var testCode = @"
class MyClass
{
  $$void MyMethod1()
  $${
  $$}

#pragma warning disable format
		void MyMethod2()
  {
  }
#pragma warning restore format

  void MyMethod3()
  $${
  $$}
}
";
            var fixedCode = @"
class MyClass
{
    void MyMethod1()
    {
    }

#pragma warning disable format
		void MyMethod2()
  {
  }
#pragma warning restore format

  void MyMethod3()
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

            await new CSharpCodeFixTest<CSharpFormattingAnalyzer, CSharpFormattingCodeFixProvider, XUnitVerifier>
            {
                TestState = { Sources = { (Path.GetFullPath("Test0.cs"), testCode) } },
                FixedState = { Sources = { (Path.GetFullPath("Test0.cs"), fixedCode) } },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var documentId = DocumentId.CreateNewId(projectId, ".editorconfig");
                        return solution.AddAnalyzerConfigDocument(documentId, ".editorconfig", SourceText.From(editorConfig, Encoding.UTF8), filePath: Path.GetFullPath(".editorconfig"));
                    },
                },
            }.RunAsync();
        }
    }
}
