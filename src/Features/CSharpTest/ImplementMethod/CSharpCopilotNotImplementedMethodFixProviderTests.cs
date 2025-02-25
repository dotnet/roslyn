// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.ImplementMethod;

public class CSharpCopilotNotImplementedMethodFixProviderTests
{
    [Theory]
    [InlineData("Console.WriteLine(\"This is a single statement\");")]
    [InlineData("int x = 10;\nint y = 20;\nConsole.WriteLine(x + y);")]
    [InlineData("/* Comment before */\nConsole.WriteLine(\"First line\");\n/* Comment after */\nConsole.WriteLine(\"Second line\");")]
    [InlineData("return await Task.FromResult(42);")]
    [InlineData("await Task.Delay(1000);\nreturn 42;")]
    [InlineData("// Initialize variables\nint a = 5;\nint b = 10;\n// Perform calculation\nint result = a + b;\nConsole.WriteLine(result);")]
    [InlineData("var list = new List<int> { 1, 2, 3 };\nforeach (var item in list)\n{\n    Console.WriteLine(item);\n}")]
    [InlineData("try\n{\n    // Try block\n    int result = 10 / 0;\n}\ncatch (DivideByZeroException ex)\n{\n    Console.WriteLine(ex.Message);\n}")]
    [InlineData("if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)\n{\n    Console.WriteLine(\"It's Friday!\");\n}\nelse\n{\n    Console.WriteLine(\"It's not Friday.\");\n}")]
    [InlineData("Console.WriteLine(\"Start\"); // Comment at the end")]
    [InlineData("/* Multi-line comment at the beginning */\nConsole.WriteLine(\"Middle\");")]
    [InlineData("Console.WriteLine(\"End\"); /* Multi-line comment at the end */")]
    [InlineData("// Single-line comment at the beginning\nConsole.WriteLine(\"Middle\");")]
    [InlineData("Console.WriteLine(\"End\"); // Single-line comment at the end")]
    [InlineData("Console.WriteLine(\"Hi\");\nthrow new InvalidOperationException();")]
    public void TestGenerateCode(string codeBlockSuggestion)
    {
        var editor = new SyntaxEditor(SyntaxFactory.ParseSyntaxTree("class TestClass { public void TestMethod() { throw new NotImplementedException(); } }").GetRoot(), new AdhocWorkspace().Services);
        var throwNode = editor.OriginalRoot.DescendantNodes().OfType<ThrowStatementSyntax>().First();

        CSharpCopilotNotImplementedMethodFixProvider.CodeGenerator.GenerateCode(editor, throwNode, codeBlockSuggestion);

        var newRoot = editor.GetChangedRoot();
        Assert.NotNull(newRoot);
        Assert.DoesNotContain("throw new NotImplementedException();", newRoot.ToFullString());
        Assert.Contains(codeBlockSuggestion.Split('\n')[0].Trim(), newRoot.ToFullString());
    }
}
