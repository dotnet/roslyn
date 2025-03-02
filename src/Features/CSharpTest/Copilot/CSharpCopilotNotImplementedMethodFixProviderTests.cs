// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpImplementNotImplementedExceptionDiagnosticAnalyzer,
    CSharpCopilotNotImplementedMethodFixProvider>;

public class CSharpCopilotNotImplementedMethodFixProviderTests
{
    [Fact]
    public async Task TestNotImplementedMethodFix_ReplicatesProperResponse()
    {
        var mockOptionsService = new Mock<ICopilotOptionsService>(MockBehavior.Strict);
        mockOptionsService
            .Setup(service => service.IsImplementNotImplementedExceptionEnabledAsync())
            .ReturnsAsync(true);

        var mockCopilotService = new Mock<ICopilotCodeAnalysisService>(MockBehavior.Strict);
        mockCopilotService
            .Setup(service => service.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockCopilotService
            .Setup(service => service.ImplementNotImplementedMethodAsync(
                It.IsAny<Document>(),
                It.IsAny<TextSpan?>(),
                It.IsAny<SyntaxNode>(),
                It.IsAny<ISymbol>(),
                It.IsAny<SemanticModel>(),
                It.IsAny<ImmutableArray<ReferencedSymbol>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Document document, TextSpan? textSpan, SyntaxNode node, ISymbol symbol, SemanticModel semanticModel, ImmutableArray<ReferencedSymbol> references, CancellationToken cancellationToken) =>
            {
                var text = await document.GetTextAsync(cancellationToken);
                var implementation = node is MethodDeclarationSyntax methodDeclaration
                    ? methodDeclaration.Identifier.Text switch
                    {
                        "Add" => "public int Add(int a, int b)\n{\n    return a + b;\n}\n",
                        "Subtract" => "public int Subtract(int a, int b) => a - b;\n",
                        "Multiply" => "public int Multiply(int a, int b)\n{\n    return a * b;\n}\n",
                        "Divide" => "public double Divide(int a, int b)\n{\n    if (b == 0) throw new DivideByZeroException(\"Division by zero is not allowed\");\n    return (double)a / b;\n}\n",
                        "CalculateSquareRoot" => "public double CalculateSquareRoot(double number) => Math.Sqrt(number);\n",
                        "Factorial" => "public int Factorial(int number)\n{\n    if (number < 0) throw new ArgumentException(\"Number must be non-negative\", nameof(number));\n    return number == 0 ? 1 : number * Factorial(number - 1);\n}\n",
                        _ => null
                    }
                    : node is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Identifier.Text == "ConstantValue"
                    ? "public int ConstantValue => 42;\n"
                    : null;

                return (new Dictionary<string, string> { { "Implementation", implementation ?? string.Empty } }, false);
            });

        var testCode = """
            using System;
            using System.Threading.Tasks;

            public class MathService : IMathService
            {
                public int Add(int a, int b)
                {
                    [|{|IDE3000:throw new NotImplementedException("Add method not implemented")|}|];
                }

                public int Subtract(int a, int b) => [|throw new NotImplementedException("Subtract method not implemented")|}|];

                public int Multiply(int a, int b)
                {
                    [|{|IDE3000:throw new NotImplementedException("Multiply method not implemented")|}|];
                }

                public double Divide(int a, int b)
                {
                    [|{|IDE3000:throw new NotImplementedException("Divide method not implemented")|}|];
                }

                public double CalculateSquareRoot(double number) => [|throw new NotImplementedException("CalculateSquareRoot method not implemented")|}|];

                public int Factorial(int number)
                {
                    [|{|IDE3000:throw new NotImplementedException("Factorial method not implemented")|}|];
                }

                public int ConstantValue => [|{|IDE3000:throw new NotImplementedException("Property not implemented")|}|];
            }
            """;

        var fixedCode = """
            using System;
            using System.Threading.Tasks;

            public class MathService : IMathService
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }

                public int Subtract(int a, int b) => a - b;

                public int Multiply(int a, int b)
                {
                    return a * b;
                }

                public double Divide(int a, int b)
                {
                    if (b == 0) throw new DivideByZeroException("Division by zero is not allowed");
                    return (double)a / b;
                }

                public double CalculateSquareRoot(double number) => Math.Sqrt(number);

                public int Factorial(int number)
                {
                    if (number < 0) throw new ArgumentException("Number must be non-negative", nameof(number));
                    return number == 0 ? 1 : number * Factorial(number - 1);
                }

                public int ConstantValue => 42;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotImplementedMethodFix_VariousForms_NotifiesWhenQuotaExceeded()
    {
        var mockOptionsService = new Mock<ICopilotOptionsService>(MockBehavior.Strict);
        mockOptionsService
            .Setup(service => service.IsImplementNotImplementedExceptionEnabledAsync())
            .ReturnsAsync(true);

        var mockCopilotService = new Mock<ICopilotCodeAnalysisService>(MockBehavior.Strict);
        mockCopilotService
            .Setup(service => service.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockCopilotService
            .Setup(service => service.ImplementNotImplementedMethodAsync(
                It.IsAny<Document>(),
                It.IsAny<TextSpan?>(),
                It.IsAny<SyntaxNode>(),
                It.IsAny<ISymbol>(),
                It.IsAny<SemanticModel>(),
                It.IsAny<ImmutableArray<ReferencedSymbol>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<(Dictionary<string, string>?, bool)>((null, true)));

        var testCode = """
            using System;
            using System.Threading.Tasks;

            public class DataService : IDataService
            {
                public void AddData(string data)
                {
                    [|{|IDE3000:throw new NotImplementedException("AddData method not implemented")|}|];
                }

                public string GetData(int id) => [|throw new NotImplementedException();|]

                /* Updates the data for a given ID */
                public void UpdateData(int id, string data)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    [|{|IDE3000:throw new NotImplementedException("UpdateData method not implemented")|}|];
                }

                // Deletes data by ID
                public void DeleteData(int id)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    [|{|IDE3000:throw new NotImplementedException();|]
                }

                /// <summary>
                /// Saves changes asynchronously
                /// </summary>
                /// <returns>A task representing the save operation</returns>
                public Task SaveChangesAsync()
                {
                    [|{|IDE3000:throw new NotImplementedException("SaveChangesAsync method not implemented")|}|];
                }

                public int DataCount => [|{|IDE3000:throw new NotImplementedException("Property not implemented")|}|];
            }
            """;

        var fixedCode = """
            using System;
            using System.Threading.Tasks;

            public class DataService : IDataService
            {
                /* Error: Quota exceeded. */
                public void AddData(string data)
                {
                    throw new NotImplementedException("AddData method not implemented");
                }
            
                /* Error: Quota exceeded. */
                public string GetData(int id) => throw new NotImplementedException();
            
                /* Error: Quota exceeded. */
                /* Updates the data for a given ID */
                public void UpdateData(int id, string data)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    throw new NotImplementedException("UpdateData method not implemented");
                }
            
                /* Error: Quota exceeded. */
                // Deletes data by ID
                public void DeleteData(int id)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    throw new NotImplementedException();
                }
            
                /* Error: Quota exceeded. */
                /// <summary>
                /// Saves changes asynchronously
                /// </summary>
                /// <returns>A task representing the save operation</returns>
                public Task SaveChangesAsync()
                {
                    throw new NotImplementedException("SaveChangesAsync method not implemented");
                }
            
                /* Error: Quota exceeded. */
                public int DataCount => throw new NotImplementedException("Property not implemented");
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotImplementedMethodFix_FailedToParseIntoMethodOrProperty()
    {
        var mockOptionsService = new Mock<ICopilotOptionsService>(MockBehavior.Strict);
        mockOptionsService
            .Setup(service => service.IsImplementNotImplementedExceptionEnabledAsync())
            .ReturnsAsync(true);

        var mockCopilotService = new Mock<ICopilotCodeAnalysisService>(MockBehavior.Strict);
        mockCopilotService
            .Setup(service => service.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockCopilotService
            .Setup(service => service.ImplementNotImplementedMethodAsync(
                It.IsAny<Document>(),
                It.IsAny<TextSpan?>(),
                It.IsAny<SyntaxNode>(),
                It.IsAny<ISymbol>(),
                It.IsAny<SemanticModel>(),
                It.IsAny<ImmutableArray<ReferencedSymbol>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<(Dictionary<string, string>?, bool)>((
                new Dictionary<string, string> { { "Implementation", "invalid code" } }, false)));

        var testCode = """
        using System;

        class C
        {
            void M()
            {
                [|{|IDE3000:throw new NotImplementedException()|}|];
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                /* Error: Failed to parse into a method or property */
                throw new NotImplementedException();
            }
        }
        """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable_NullDictionary()
    {
        await RunTestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable(null);
    }

    [Fact]
    public async Task TestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable_EmptyDictionary()
    {
        await RunTestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable([]);
    }

    [Theory]
    [InlineData("Implementation", " ")]
    [InlineData("Implementation", "")]
    public async Task TestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable_ImplementationKey(string key, string value)
    {
        await RunTestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable(
            new Dictionary<string, string> { { key, value } });
    }

    [Fact]
    public async Task TestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable_OtherKeyDictionary()
    {
        await RunTestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable(
            new Dictionary<string, string> { { "OtherKey", "SomeValue" } });
    }

    [Fact]
    public async Task TestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable_MessageDictionary()
    {
        await RunTestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable(
            new Dictionary<string, string> { { "Message", "Custom error message" } });
    }

    private static async Task RunTestNotImplementedMethodFix_NotifiesWhenImplementationNotAvailable(Dictionary<string, string>? implementationSuggestion)
    {
        var mockOptionsService = new Mock<ICopilotOptionsService>(MockBehavior.Strict);
        mockOptionsService
            .Setup(service => service.IsImplementNotImplementedExceptionEnabledAsync())
            .ReturnsAsync(true);

        var mockCopilotService = new Mock<ICopilotCodeAnalysisService>(MockBehavior.Strict);
        mockCopilotService
            .Setup(service => service.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockCopilotService
            .Setup(service => service.ImplementNotImplementedMethodAsync(
                It.IsAny<Document>(),
                It.IsAny<TextSpan?>(),
                It.IsAny<SyntaxNode>(),
                It.IsAny<ISymbol>(),
                It.IsAny<SemanticModel>(),
                It.IsAny<ImmutableArray<ReferencedSymbol>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<(Dictionary<string, string>?, bool)>((implementationSuggestion, false)));

        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    [|{|IDE3000:throw new NotImplementedException()|}|];
                }
            }
            """;

        var fixedCode = implementationSuggestion?.TryGetValue("Message", out var message) == true
            ? $$"""
            using System;

            class C
            {
                void M()
                {
                    /* {{message}} */
                    throw new NotImplementedException();
                }
            }
            """
            : """
            using System;

            class C
            {
                void M()
                {
                    /* Error: Could not complete this request. */
                    throw new NotImplementedException();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }
}
