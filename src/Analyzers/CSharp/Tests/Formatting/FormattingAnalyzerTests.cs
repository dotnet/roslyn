// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    using Verify = CSharpCodeFixVerifier<CodeStyle.CSharpFormattingAnalyzer, CodeStyle.CSharpFormattingCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class FormattingAnalyzerTests
    {
        [Fact]
        public async Task TrailingWhitespace()
        {
            var testCode =
                "class X[| |]" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;
            var expected =
                "class X" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;
            await Verify.VerifyCodeFixAsync(testCode, expected);
        }

        [Fact]
        public async Task TestMissingSpace()
        {
            var testCode = @"
class TypeName
{
    void Method()
    {
        if[||](true)[||]return;
    }
}
";
            var expected = @"
class TypeName
{
    void Method()
    {
        if (true) return;
    }
}
";
            await Verify.VerifyCodeFixAsync(testCode, expected);
        }

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

            await new Verify.Test
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

            await new Verify.Test
            {
                TestState =
                {
                    Sources = { testCode },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", editorConfig),
                    },
                },
                FixedState = { Sources = { fixedCode } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestRegion()
        {
            var testCode = @"
class MyClass
{
#if true
    public void M()
    {
        #region ABC1
        System.Console.WriteLine();
        #endregion
    }
#else
    public void M()
    {
        #region ABC2
        System.Console.WriteLine();
        #endregion
    }
#endif
}
";
            await Verify.VerifyCodeFixAsync(testCode, testCode);
        }

        [Fact]
        public async Task TestRegion2()
        {
            var testCode = @"
class MyClass
{
#if true
    public void M()
    {
[||]#region OUTER1
        #region ABC1
        System.Console.WriteLine();
        #endregion
[||]#endregion
    }
#else
    public void M()
    {
#region OUTER2
        #region ABC2
        System.Console.WriteLine();
        #endregion
#endregion
    }
#endif
}
";

            var fixedCode = @"
class MyClass
{
#if true
    public void M()
    {
        #region OUTER1
        #region ABC1
        System.Console.WriteLine();
        #endregion
        #endregion
    }
#else
    public void M()
    {
#region OUTER2
        #region ABC2
        System.Console.WriteLine();
        #endregion
#endregion
    }
#endif
}
";
            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestRegion3()
        {
            var testCode = @"
class MyClass
{
#if true
    public void M()
    {
[||]#region ABC1
        System.Console.WriteLine();
[||]#endregion
    }
#else
    public void M()
    {
#region ABC2
        System.Console.WriteLine();
#endregion
    }
#endif
}
";
            var fixedCode = @"
class MyClass
{
#if true
    public void M()
    {
        #region ABC1
        System.Console.WriteLine();
        #endregion
    }
#else
    public void M()
    {
#region ABC2
        System.Console.WriteLine();
#endregion
    }
#endif
}
";
            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task TestRegion4()
        {
            var testCode = @"
class MyClass
{
#if true
    public void M()
    {
[||]#region ABC1
        System.Console.WriteLine();
[||]#endregion
    }
#else
                        #region ABC2
        public void M() { }
                        #endregion
#endif
}
";
            var fixedCode = @"
class MyClass
{
#if true
    public void M()
    {
        #region ABC1
        System.Console.WriteLine();
        #endregion
    }
#else
                        #region ABC2
        public void M() { }
                        #endregion
#endif
}
";
            await Verify.VerifyCodeFixAsync(testCode, fixedCode);
        }
    }
}
