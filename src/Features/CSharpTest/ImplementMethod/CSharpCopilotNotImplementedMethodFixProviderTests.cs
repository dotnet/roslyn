// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.ImplementMethod;

public class CSharpCopilotNotImplementedMethodFixProviderTests
{
    public static IEnumerable<object[]> CodeBlockSuggestions()
    {
        yield return new object[] { "Console.WriteLine(\"This is a single statement\");" };
        yield return new object[] { "int x = 10;\nint y = 20;\nConsole.WriteLine(x + y);" };
        yield return new object[] { "/* Comment before */\nConsole.WriteLine(\"First line\");\n/* Comment after */\nConsole.WriteLine(\"Second line\");" };
        yield return new object[] { "return await Task.FromResult(42);" };
        yield return new object[] { "await Task.Delay(1000);\nreturn 42;" };
        yield return new object[] { "// Initialize variables\nint a = 5;\nint b = 10;\n// Perform calculation\nint result = a + b;\nConsole.WriteLine(result);" };
        yield return new object[] { "var list = new List<int> { 1, 2, 3 };\nforeach (var item in list)\n{\n    Console.WriteLine(item);\n}" };
        yield return new object[] { "try\n{\n    // Try block\n    int result = 10 / 0;\n}\ncatch (DivideByZeroException ex)\n{\n    Console.WriteLine(ex.Message);\n}" };
        yield return new object[] { "if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)\n{\n    Console.WriteLine(\"It's Friday!\");\n}\nelse\n{\n    Console.WriteLine(\"It's not Friday.\");\n}" };
        yield return new object[] { "Console.WriteLine(\"Start\"); // Comment at the end" };
        yield return new object[] { "/* Multi-line comment at the beginning */\nConsole.WriteLine(\"Middle\");" };
        yield return new object[] { "Console.WriteLine(\"End\"); /* Multi-line comment at the end */" };
        yield return new object[] { "// Single-line comment at the beginning\nConsole.WriteLine(\"Middle\");" };
        yield return new object[] { "Console.WriteLine(\"End\"); // Single-line comment at the end" };
        yield return new object[] { "Console.WriteLine(\"Hi\");\nthrow new InvalidOperationException();" };
        yield return new object[] { "return x > 0 ? x : throw new ArgumentException(\"x must be greater than 0\");" };
    }

    [Theory]
    [MemberData(nameof(CodeBlockSuggestions))]
    public void GenerateCode_ThrowStatement_ReplacesWithCodeBlockSuggestion(string codeBlockSuggestion)
    {
        var editor = new SyntaxEditor(SyntaxFactory.ParseSyntaxTree("class TestClass { public void TestMethod() { throw new NotImplementedException(); } }").GetRoot(), new AdhocWorkspace().Services);
        var throwNode = editor.OriginalRoot.DescendantNodes().OfType<ThrowStatementSyntax>().First();

        CSharpCopilotNotImplementedMethodFixProvider.CodeGenerator.GenerateCode(editor, throwNode, codeBlockSuggestion);

        var newRoot = editor.GetChangedRoot();
        Assert.NotNull(newRoot);
        Assert.DoesNotContain("throw new NotImplementedException();", newRoot.ToFullString());
        Assert.Contains(codeBlockSuggestion.Split('\n')[0].Trim(), newRoot.ToFullString());
    }

    [Theory]
    [MemberData(nameof(CodeBlockSuggestions))]
    public void GenerateCode_ThrowExpression_ReplacesWithCodeBlockSuggestion(string codeBlockSuggestion)
    {
        var editor = new SyntaxEditor(SyntaxFactory.ParseSyntaxTree(
            "class TestClass { public int TestMethod(int value) { return value >= 0 ? value : throw new NotImplementedException(); } }").GetRoot(), new AdhocWorkspace().Services);
        var throwNode = editor.OriginalRoot.DescendantNodes().OfType<ThrowExpressionSyntax>().First();

        CSharpCopilotNotImplementedMethodFixProvider.CodeGenerator.GenerateCode(editor, throwNode, codeBlockSuggestion);
        var newRoot = editor.GetChangedRoot();
        Assert.NotNull(newRoot);
        Assert.DoesNotContain("throw new NotImplementedException();", newRoot.ToFullString());
        Assert.Contains(codeBlockSuggestion.Split('\n')[0].Trim(), newRoot.ToFullString());
    }
}
