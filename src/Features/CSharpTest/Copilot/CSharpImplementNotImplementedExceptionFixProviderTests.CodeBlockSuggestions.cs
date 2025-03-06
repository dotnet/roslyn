// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;
[Trait(Traits.Feature, Traits.Features.CopilotImplementNotImplementedException)]
public sealed partial class CSharpImplementNotImplementedExceptionFixProviderTests
{
    public static IEnumerable<object[]> TestMethodCodeBlockSuggestions()
    {
        foreach (var kvp in s_codeBlockSuggestions)
        {
            var notImplementedMember = kvp.Key;
            var codeBlocks = kvp.Value;
            foreach (var codeBlock in codeBlocks)
            {
                yield return new object[] { notImplementedMember, codeBlock };
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestMethodCodeBlockSuggestions))]
    public async Task FixerResponse_ReplacesCodeBlockCorrectly(string notImplementedCodeBlock, string replacementCodeBlock)
    {
        await new CustomCompositionCSharpTest
        {
            TestCode = $$"""
using System;
using System.Threading.Tasks;

public class TestService
{
    {{notImplementedCodeBlock}}
}
""",
            FixedCode = $$"""
using System;
using System.Threading.Tasks;

public class TestService
{
    {{replacementCodeBlock}}
}
""",
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.PrepareUsingSingleFakeResult = new()
            {
                ReplacementNode = SyntaxFactory.ParseMemberDeclaration(replacementCodeBlock),
                Message = "Successful",
            };
        })
        .RunAsync();
    }

    private static readonly Dictionary<string, object[]> s_codeBlockSuggestions = new()
    {
        // Single statement with NotImplementedException
        [
            "public void TestMethod()\n    {\n        {|IDE3000:throw new NotImplementedException();|}\n    }"
        ] =
        [
            "public void TestMethod() => Console.WriteLine(\"Hello, World!\");",
            "public void TestMethod()\n    {\n        Console.WriteLine(\"This is a single statement\");\n    }",
            "public void TestMethod()\n    {\n        int x = 10;\n        int y = 20;\n        Console.WriteLine(x + y);\n    }",
            "public void TestMethod()\n    {\n        /* Comment before */\n        Console.WriteLine(\"First line\");\n        /* Comment after */\n        Console.WriteLine(\"Second line\");\n    }",
            "public void TestMethod()\n    {\n        // Initialize variables\n        int a = 5;\n        int b = 10;\n        // Perform calculation\n        int result = a + b;\n        Console.WriteLine(result);\n    }",
            "public void TestMethod()\n    {\n        var list = new int[] { 1, 2, 3, 4, 5 };\n        foreach (var item in list)\n        {\n            Console.WriteLine(item);\n        }\n    }",
            "public void TestMethod()\n    {\n        try\n        {\n            // Try block\n            Console.WriteLine(\"This is a test method.\");\n        }\n        catch (Exception ex)\n        {\n            Console.WriteLine(ex.Message);\n        }\n    }",
            "public void TestMethod()\n    {\n        if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)\n        {\n            Console.WriteLine(\"It's Friday!\");\n        }\n        else\n        {\n            Console.WriteLine(\"It's not Friday.\");\n        }\n    }",
            "public void TestMethod()\n    {\n        Console.WriteLine(\"Start\"); // Comment at the end\n    }",
            "public void TestMethod()\n    {\n        /* Multi-line comment at the beginning */\n        Console.WriteLine(\"Middle\");\n    }",
            "public void TestMethod()\n    {\n        Console.WriteLine(\"End\"); /* Multi-line comment at the end */\n    }",
            "public void TestMethod()\n    {\n        // Single-line comment at the beginning\n        Console.WriteLine(\"Middle\");\n    }",
            "public void TestMethod()\n    {\n        Console.WriteLine(\"End\"); // Single-line comment at the end\n    }",
            "public void TestMethod()\n    {\n        Console.WriteLine(\"Hi\");\n        throw new InvalidOperationException();\n    }"
        ],
        // Async method with NotImplementedException
        [
            "public async Task TestMethodAsync()\n    {\n        {|IDE3000:throw new NotImplementedException();|}\n    }"
        ] =
        [
            "public async Task TestMethodAsync()\n    {\n        await Task.Delay(1000);\n        Console.WriteLine(\"Async operation completed\");\n    }",
            "public async Task TestMethodAsync()\n        => await Task.Run(() => Console.WriteLine(\"Running async task\"));"
        ],
        // Property with NotImplementedException in expression-bodied member
        [
            "public int TestProperty => {|IDE3000:throw new NotImplementedException()|};"
        ] =
        [
            "public int TestProperty => 42;",
            "public int TestProperty\n        => DateTime.Now.Year;"
        ]
    };
}
