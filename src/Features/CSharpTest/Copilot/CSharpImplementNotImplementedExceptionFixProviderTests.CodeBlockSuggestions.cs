// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Moq;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpImplementNotImplementedExceptionDiagnosticAnalyzer,
    CSharpImplementNotImplementedExceptionFixProvider>;

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
        MockCopilotService((copilotService) =>
        {
            copilotService
                .Setup(service => service.ImplementNotImplementedExceptionAsync(
                    It.IsAny<Document>(),
                    It.IsAny<TextSpan?>(),
                    It.IsAny<SyntaxNode>(),
                    It.IsAny<ISymbol>(),
                    It.IsAny<SemanticModel>(),
                    It.IsAny<ImmutableArray<ReferencedSymbol>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<(Dictionary<string, string>?, bool)>((new() { ["Implementation"] = replacementCodeBlock }, false)));
        });

        await new VerifyCS.Test
        {
            TestCode = $$"""
            using System;
            using System.Threading.Tasks;

            public class TestService
            {
                {{notImplementedCodeBlock.TrimStart()}}
            }
            """,
            FixedCode = $$"""
            using System;
            using System.Threading.Tasks;

            public class TestService
            {
                {{replacementCodeBlock.TrimStart()}}
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    private static readonly Dictionary<string, object[]> s_codeBlockSuggestions = new()
    {
        // Single statement with NotImplementedException
        [
            """
                public void TestMethod()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            """
        ] =
        [
            """
                public void TestMethod() => Console.WriteLine("Hello, World!");
            """,
            """
                public void TestMethod()
                {
                    Console.WriteLine("This is a single statement");
                }
            """,
            """
                public void TestMethod()
                {
                    int x = 10;
                    int y = 20;
                    Console.WriteLine(x + y);
                }
            """,
            """
                public void TestMethod()
                {
                    /* Comment before */
                    Console.WriteLine("First line");
                    /* Comment after */
                    Console.WriteLine("Second line");
                }
            """,
            """
                public void TestMethod()
                {
                    // Initialize variables
                    int a = 5;
                    int b = 10;
                    // Perform calculation
                    int result = a + b;
                    Console.WriteLine(result);
                }
            """,
            """
                public void TestMethod()
                {
                    var list = new List<int> { 1, 2, 3 };
                    foreach (var item in list)
                    {
                        Console.WriteLine(item);
                    }
                }
            """,
            """
                public void TestMethod()
                {
                    try
                    {
                        // Try block
                        int result = 10 / 0;
                    }
                    catch (DivideByZeroException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            """,
            """
                public void TestMethod()
                {
                    if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
                    {
                        Console.WriteLine("It's Friday!");
                    }
                    else
                    {
                        Console.WriteLine("It's not Friday.");
                    }
                }
            """,
            """
                public void TestMethod()
                {
                    Console.WriteLine("Start"); // Comment at the end
                }
            """,
            """
                public void TestMethod()
                {
                    /* Multi-line comment at the beginning */
                    Console.WriteLine("Middle");
                }
            """,
            """
                public void TestMethod()
                {
                    Console.WriteLine("End"); /* Multi-line comment at the end */
                }
            """,
            """
                public void TestMethod()
                {
                    // Single-line comment at the beginning
                    Console.WriteLine("Middle");
                }
            """,
            """
                public void TestMethod()
                {
                    Console.WriteLine("End"); // Single-line comment at the end
                }
            """,
            """
                public void TestMethod()
                {
                    Console.WriteLine("Hi");
                    throw new InvalidOperationException();
                }
            """
        ],
        // Async method with NotImplementedException
        [
            """
                public async Task TestMethodAsync()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            """
        ] =
        [
            """
                public async Task TestMethodAsync()
                {
                    await Task.Delay(1000);
                    Console.WriteLine("Async operation completed");
                }
            """,
            """
                public async Task TestMethodAsync()
                    => await Task.Run(() => Console.WriteLine("Running async task"));
            """,
        ],
        // Property with NotImplementedException in expression-bodied member
        [
            """
                public int TestProperty => {|IDE3000:throw new NotImplementedException()|};
            """
        ] =
        [
            """
                public int TestProperty => 42;
            """,
            """
                public int TestProperty
                    => DateTime.Now.Year;
            """,
        ],
    };
}
