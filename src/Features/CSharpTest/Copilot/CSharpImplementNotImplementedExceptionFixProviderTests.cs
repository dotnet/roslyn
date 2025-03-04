// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpImplementNotImplementedExceptionDiagnosticAnalyzer,
    CSharpImplementNotImplementedExceptionFixProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CopilotImplementNotImplementedException)]
public sealed partial class CSharpImplementNotImplementedExceptionFixProviderTests
{
    [Fact]
    public async Task FixAll_ParseSuccessfully()
    {
        await new CustomCompositionCSharpTest
        {
            TestCode = """
            using System;
            using System.Threading.Tasks;

            public class MathService : IMathService
            {
                public int Add(int a, int b)
                {
                    {|IDE3000:throw new NotImplementedException("Add method not implemented");|}
                }

                public int Subtract(int a, int b) => {|IDE3000:throw new NotImplementedException("Subtract method not implemented")|};

                public int Multiply(int a, int b) {
                    {|IDE3000:throw new NotImplementedException("Multiply method not implemented");|}
                }

                public double Divide(int a, int b)
                {
                    {|IDE3000:throw new NotImplementedException("Divide method not implemented");|}
                }

                public double CalculateSquareRoot(double number) => {|IDE3000:throw new NotImplementedException("CalculateSquareRoot method not implemented")|};

                public int Factorial(int number)
                {
                    {|IDE3000:throw new NotImplementedException("Factorial method not implemented");|}
                }

                public int ConstantValue => {|IDE3000:throw new NotImplementedException("Property not implemented")|};
            }

            public interface IMathService
            {
                int Add(int a, int b);
                int Subtract(int a, int b);
                int Multiply(int a, int b);
                double Divide(int a, int b);
                double CalculateSquareRoot(double number);
                int Factorial(int number);
                int ConstantValue { get; }
            }
            """,
            FixedCode = """
            using System;
            using System.Threading.Tasks;

            public class MathService : IMathService
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }

                public int Subtract(int a, int b) => a - b;

                public int Multiply(int a, int b) {
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

            public interface IMathService
            {
                int Add(int a, int b);
                int Subtract(int a, int b);
                int Multiply(int a, int b);
                double Divide(int a, int b);
                double CalculateSquareRoot(double number);
                int Factorial(int number);
                int ConstantValue { get; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.SetupFixAll = async (Document document, SyntaxNode node, CancellationToken cancellationToken) =>
            {
                var text = await document.GetTextAsync(cancellationToken);
                var replacementNode = node is MethodDeclarationSyntax methodDeclaration
                    ? methodDeclaration.Identifier.Text switch
                    {
                        "Add" => "public int Add(int a, int b)\n{\n    return a + b;\n}\n",
                        "Subtract" => "public int Subtract(int a, int b) => a - b;\n",
                        "Multiply" => "public int Multiply(int a, int b)\n{\n    return a * b;\n}\n",
                        "Divide" => "public double Divide(int a, int b)\n{\n    if (b == 0) throw new DivideByZeroException(\"Division by zero is not allowed\");\n    return (double)a / b;\n}\n",
                        "CalculateSquareRoot" => "public double CalculateSquareRoot(double number) => Math.Sqrt(number);\n",
                        "Factorial" => "public int Factorial(int number)\n{\n    if (number < 0) throw new ArgumentException(\"Number must be non-negative\", nameof(number));\n    return number == 0 ? 1 : number * Factorial(number - 1);\n}\n",
                        _ => string.Empty
                    }
                    : node is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Identifier.Text == "ConstantValue"
                    ? "public int ConstantValue => 42;\n"
                    : string.Empty;

                return new()
                {
                    IsQuotaExceeded = false,
                    ReplacementNode = SyntaxFactory.ParseMemberDeclaration(replacementNode),
                    Message = "Successful",
                };
            };
        })
        .RunAsync();
    }

    [Fact]
    public async Task QuotaExceeded_VariousForms_NotifiesAsComment()
    {
        await new CustomCompositionCSharpTest
        {
            TestCode = """
            using System;
            using System.Threading.Tasks;

            public class DataService : IDataService
            {
                public void AddData(string data)
                {
                    {|IDE3000:throw new NotImplementedException("AddData method not implemented");|}
                }

                public string GetData(int id) => {|IDE3000:throw new NotImplementedException()|};

                /* Updates the data for a given ID */
                public void UpdateData(int id, string data)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    {|IDE3000:throw new NotImplementedException("UpdateData method not implemented");|}
                }

                // Deletes data by ID
                public void DeleteData(int id)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    {|IDE3000:throw new NotImplementedException();|}
                }

                /// <summary>
                /// Saves changes asynchronously
                /// </summary>
                /// <returns>A task representing the save operation</returns>
                public Task SaveChangesAsync()
                {
                    {|IDE3000:throw new NotImplementedException("SaveChangesAsync method not implemented");|}
                }

                public int DataCount => {|IDE3000:throw new NotImplementedException("Property not implemented")|};
            }

            public interface IDataService
            {
                void AddData(string data);
                string GetData(int id);
                void UpdateData(int id, string data);
                void DeleteData(int id);
                Task SaveChangesAsync();
                int DataCount { get; }
            }
            """,
            FixedCode = """
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

            public interface IDataService
            {
                void AddData(string data);
                string GetData(int id);
                void UpdateData(int id, string data);
                void DeleteData(int id);
                Task SaveChangesAsync();
                int DataCount { get; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.PrepareFakeResult = new()
            {
                IsQuotaExceeded = true,
                ReplacementNode = null,
                Message = nameof(ImplementationDetails.IsQuotaExceeded),
            };
        })
        .RunAsync();
    }

    [Fact]
    public async Task ReceivesInvalidCode_NotifiesAsComment()
    {
        await new CustomCompositionCSharpTest
        {
            TestCode = """
        using System;

        class C
        {
            void M()
            {
                {|IDE3000:throw new NotImplementedException();|}
            }
        }
        """,
            FixedCode = """
        using System;

        class C
        {
            /* Error: Failed to parse into a method or property */
            void M()
            {
                throw new NotImplementedException();
            }
        }
        """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.PrepareFakeResult = new()
            {
                IsQuotaExceeded = false,
                ReplacementNode = null,
                Message = "Received invalid code.",
            };
        })
        .RunAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplacementNode_Null_NotifiesWithComment(bool withEmptyMessage)
    {
        await TestHandlesInvalidReplacementNode(
            new()
            {
                IsQuotaExceeded = false,
                ReplacementNode = null,
                Message = withEmptyMessage ? string.Empty : "Custom Error Message",
            });
    }

    [Theory]
    [InlineData("Invalid code")]
    [InlineData(" ")]
    [InlineData("")]
    public async Task ReplacementNode_Invalid_NotifiedWithDefault(string invalidCode)
    {
        await TestHandlesInvalidReplacementNode(
            new()
            {
                IsQuotaExceeded = false,
                ReplacementNode = SyntaxFactory.ParseMemberDeclaration(invalidCode),
                Message = "Custom Error Message",
            })
            .ConfigureAwait(false);
    }

    private static async Task TestHandlesInvalidReplacementNode(ImplementationDetails implementationDetails)
    {
        await new CustomCompositionCSharpTest
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            FixedCode = !string.IsNullOrWhiteSpace(implementationDetails.Message)
            ? $$"""
            using System;

            class C
            {
                /* {{implementationDetails.Message}} */
                void M()
                {
                    throw new NotImplementedException();
                }
            }
            """
            : """
            using System;

            class C
            {
                /* Error: Could not complete this request. */
                void M()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.PrepareFakeResult = implementationDetails;
        })
        .RunAsync();
    }

    private class CustomCompositionCSharpTest : VerifyCS.Test
    {
        private TestComposition? _testComposition;
        private TestWorkspace? _testWorkspace;
        private Action<TestCopilotCodeAnalysisService>? _copilotServiceSetupAction;

        protected override Task<Workspace> CreateWorkspaceImplAsync()
        {
            _testComposition = FeaturesTestCompositions.Features
                .AddParts(typeof(TestCopilotOptionsService))
                .AddParts(typeof(TestCopilotCodeAnalysisService));
            _testWorkspace = new TestWorkspace(_testComposition);

            // Trigger the action if it's set
            _copilotServiceSetupAction?.Invoke(GetCopilotService(_testWorkspace));
            return Task.FromResult<Workspace>(_testWorkspace);
        }

        public CustomCompositionCSharpTest WithMockCopilotService(Action<TestCopilotCodeAnalysisService> setup)
        {
            _copilotServiceSetupAction = setup;

            // If _testWorkspace is already set, trigger the action immediately
            if (_testWorkspace != null)
            {
                setup(GetCopilotService(_testWorkspace));
            }

            return this;
        }

        private static TestCopilotCodeAnalysisService GetCopilotService(TestWorkspace testWorkspace)
        {
            var copilotService = testWorkspace.Services.GetLanguageServices(LanguageNames.CSharp)
                .GetRequiredService<ICopilotCodeAnalysisService>() as TestCopilotCodeAnalysisService;
            Assert.NotNull(copilotService);
            return copilotService;
        }
    }

    [ExportLanguageService(typeof(ICopilotOptionsService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private class TestCopilotOptionsService : ICopilotOptionsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCopilotOptionsService() { }

        public Task<bool> IsRefineOptionEnabledAsync()
            => throw new NotImplementedException();

        public Task<bool> IsCodeAnalysisOptionEnabledAsync()
            => throw new NotImplementedException();

        public Task<bool> IsOnTheFlyDocsOptionEnabledAsync()
            => throw new NotImplementedException();

        public Task<bool> IsGenerateDocumentationCommentOptionEnabledAsync()
            => throw new NotImplementedException();

        public Task<bool> IsImplementNotImplementedExceptionEnabledAsync(CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    [ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private class TestCopilotCodeAnalysisService : ICopilotCodeAnalysisService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCopilotCodeAnalysisService()
        {
        }

        public Func<Document, SyntaxNode, CancellationToken, Task<ImplementationDetails>>? SetupFixAll { get; internal set; }

        public ImplementationDetails? PrepareFakeResult { get; internal set; }

        public Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, TextSpan? span, ImmutableArray<string> promptTitles, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> ICopilotCodeAnalysisService.GetDocumentationCommentAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImplementationDetails> ImplementNotImplementedExceptionAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            return SetupFixAll?.Invoke(document, node, cancellationToken)
                ?? Task.FromResult(PrepareFakeResult ?? new ImplementationDetails
                {
                    IsQuotaExceeded = false,
                    ReplacementNode = node,
                    Message = string.Empty,
                });
        }
    }
}
