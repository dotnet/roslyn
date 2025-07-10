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
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;
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
    public Task FixAll_ParseSuccessfully()
        => new CustomCompositionCSharpTest
        {
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
            TestCode = """
            using System;
            using System.Threading.Tasks;
            using System.Linq;

            public class MathService : IMathService
            {
                public int {|IDE3000:Add|}(int a, int b)
                {
                    {|IDE3000:throw new NotImplementedException("Add method not implemented");|}
                }
        
                public int {|IDE3000:Subtract|}(int a, int b) => {|IDE3000:throw new NotImplementedException("Subtract method not implemented")|};
        
                public int {|IDE3000:Multiply|}(int a, int b) {
                    {|IDE3000:throw new NotImplementedException("Multiply method not implemented");|}
                }
        
                public double {|IDE3000:Divide|}(int a, int b)
                {
                    {|IDE3000:throw new NotImplementedException("Divide method not implemented");|}
                }
        
                public double {|IDE3000:CalculateSquareRoot|}(double number) => {|IDE3000:throw new NotImplementedException("CalculateSquareRoot method not implemented")|};
        
                public int {|IDE3000:Factorial|}(int number)
                {
                    {|IDE3000:throw new NotImplementedException("Factorial method not implemented");|}
                }
        
                public int ConstantValue => {|IDE3000:throw new NotImplementedException("Property not implemented")|};
        
                public {|IDE3000:MathService|}()
                {
                    {|IDE3000:throw new NotImplementedException("Constructor not implemented");|}
                }
        
                ~{|IDE3000:MathService|}()
                {
                    {|IDE3000:throw new NotImplementedException("Destructor not implemented");|}
                }
        
                public event EventHandler MyEvent
                {
                    {|IDE3000:add|} { {|IDE3000:throw new NotImplementedException("Event add not implemented");|} }
                    {|IDE3000:remove|} { {|IDE3000:throw new NotImplementedException("Event remove not implemented");|} }
                }
        
                public static MathService operator {|IDE3000:+|}(MathService a, MathService b)
                {
                    {|IDE3000:throw new NotImplementedException("Operator not implemented");|}
                }
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
                event EventHandler MyEvent;
            }
            """,
            FixedCode = """
            using System;
            using System.Threading.Tasks;
            using System.Linq;

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
        
                public MathService()
                {
                    // Constructor implementation
                }

                ~MathService()
                {
                    // Destructor implementation
                }
        
                public event EventHandler MyEvent
                {
                    add { /* Event add implementation */ }
                    remove { /* Event remove implementation */ }
                }
        
                public static MathService operator +(MathService a, MathService b)
                {
                    return new MathService(); // Operator implementation
                }
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
                event EventHandler MyEvent;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.SetupFixAll = (document, memberReferences, cancellationToken) =>
            {
                // Create a map of method/property implementations
                var implementationMap = new Dictionary<string, string>
                {
                    ["Add"] = "public int Add(int a, int b)\n{\n    return a + b;\n}\n",
                    ["Subtract"] = "public int Subtract(int a, int b) => a - b;\n",
                    ["Multiply"] = "public int Multiply(int a, int b)\n{\n    return a * b;\n}\n",
                    ["Divide"] = "public double Divide(int a, int b)\n{\n    if (b == 0) throw new DivideByZeroException(\"Division by zero is not allowed\");\n    return (double)a / b;\n}\n",
                    ["CalculateSquareRoot"] = "public double CalculateSquareRoot(double number) => Math.Sqrt(number);\n",
                    ["Factorial"] = "public int Factorial(int number)\n{\n    if (number < 0) throw new ArgumentException(\"Number must be non-negative\", nameof(number));\n    return number == 0 ? 1 : number * Factorial(number - 1);\n}\n",
                    ["ConstantValue"] = "public int ConstantValue => 42;\n",
                    ["MathService"] = "public MathService()\n{\n    // Constructor implementation\n}\n",
                    ["~MathService"] = "~MathService()\n{\n    // Destructor implementation\n}\n",
                    ["MyEvent"] = "public event EventHandler MyEvent\n{\n    add { /* Event add implementation */ }\n    remove { /* Event remove implementation */ }\n}\n",
                    ["operator +"] = "public static MathService operator +(MathService a, MathService b)\n{\n    return new MathService(); // Operator implementation\n}\n",
                };
                return BuildResult(memberReferences, implementationMap);
            };
        })
        .RunAsync();

    [Theory]
    [InlineData("Failed to receive implementation from Copilot service")]
    [InlineData("Generated implementation doesn't match the original method signature.")]
    [InlineData("The generated implementation isn't a valid method or property.")]
    public Task NullReplacementNode_MethodHasCommentsInVariousForms_PrintsMessageAsComment(string copilotErrorMessage)
        => new CustomCompositionCSharpTest
        {
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
            TestCode = """
            using System;
            using System.Threading.Tasks;

            public class DataService : IDataService
            {
                public void {|IDE3000:AddData|}(string data)
                {
                    {|IDE3000:throw new NotImplementedException("AddData method not implemented");|}
                }

                public string {|IDE3000:GetData|}(int id) => {|IDE3000:throw new NotImplementedException()|};

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
                public Task {|IDE3000:SaveChangesAsync|}()
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
            FixedCode = $$"""
            using System;
            using System.Threading.Tasks;

            public class DataService : IDataService
            {
                /* {{copilotErrorMessage}} */
                public void {|IDE3000:AddData|}(string data)
                {
                    {|IDE3000:throw new NotImplementedException("AddData method not implemented");|}
                }
            
                public string {|IDE3000:GetData|}(int id) => {|IDE3000:throw new NotImplementedException()|};
            
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
                public Task {|IDE3000:SaveChangesAsync|}()
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
            BatchFixedCode = $$"""
            using System;
            using System.Threading.Tasks;

            public class DataService : IDataService
            {
                /* {{copilotErrorMessage}} */
                public void {|IDE3000:AddData|}(string data)
                {
                    {|IDE3000:throw new NotImplementedException("AddData method not implemented");|}
                }
            
                /* {{copilotErrorMessage}} */
                public string {|IDE3000:GetData|}(int id) => {|IDE3000:throw new NotImplementedException()|};
            
                /* Updates the data for a given ID */
                /* {{copilotErrorMessage}} */
                public void UpdateData(int id, string data)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    {|IDE3000:throw new NotImplementedException("UpdateData method not implemented");|}
                }
            
                // Deletes data by ID
                /* {{copilotErrorMessage}} */
                public void DeleteData(int id)
                {
                    if (id <= 0) throw new ArgumentException("ID must be greater than zero", nameof(id));
                    {|IDE3000:throw new NotImplementedException();|}
                }
            
                /* {{copilotErrorMessage}} */
                /// <summary>
                /// Saves changes asynchronously
                /// </summary>
                /// <returns>A task representing the save operation</returns>
                public Task {|IDE3000:SaveChangesAsync|}()
                {
                    {|IDE3000:throw new NotImplementedException("SaveChangesAsync method not implemented");|}
                }
            
                /* {{copilotErrorMessage}} */
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
            FixedState =
            {
                MarkupHandling = MarkupMode.Allow,
            },
            BatchFixedState =
            {
                MarkupHandling = MarkupMode.Allow,
            },
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.PrepareUsingSingleFakeResult = new()
            {
                ReplacementNode = null,
                Message = copilotErrorMessage,
            };
        })
        .RunAsync();

    [Fact]
    public Task HandleInvalidCode_SuggestsAsComment()
        => new CustomCompositionCSharpTest
        {
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
            TestCode = """
            using System;

            class C
            {
                void {|IDE3000:M|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                /* The generated implementation isn't a valid method or property:
                using System;
                class C
                {
                    void M()
                    {
                        throw new NotImplementedException();
                    }
                } */
                void {|IDE3000:M|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            FixedState =
            {
                MarkupHandling = MarkupMode.Allow,
            },
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            var replacement = """
            using System;
            class C
            {
                void M()
                {
                    throw new NotImplementedException();
                }
            }
            """;
            copilotService.PrepareUsingSingleFakeResult = new()
            {
                ReplacementNode = SyntaxFactory.ParseCompilationUnit(replacement),
                Message = $"The generated implementation isn't a valid method or property:{Environment.NewLine}{replacement}",
            };
        })
        .RunAsync();

    [Fact]
    public Task ReplacementNode_Null_NotifiesWithComment()
        => TestHandlesInvalidReplacementNode(
            new()
            {
                ReplacementNode = null,
                Message = "Custom Error Message",
            });

    [Theory]
    [InlineData("class MyClass { }", typeof(ClassDeclarationSyntax))]
    [InlineData("struct MyStruct { }", typeof(StructDeclarationSyntax))]
    [InlineData("interface IMyInterface { }", typeof(InterfaceDeclarationSyntax))]
    [InlineData("enum MyEnum { Value1, Value2 }", typeof(EnumDeclarationSyntax))]
    [InlineData("delegate void MyDelegate();", typeof(DelegateDeclarationSyntax))]
    [InlineData("int myField;", typeof(FieldDeclarationSyntax))]
    [InlineData("event EventHandler MyEvent;", typeof(EventFieldDeclarationSyntax))]
    [InlineData("record MyRecord { }", typeof(RecordDeclarationSyntax))]
    public async Task TestInvalidNodeReplacement(string syntax, Type type)
    {
        await TestHandlesInvalidReplacementNode(
            new()
            {
                ReplacementNode = SyntaxFactory.ParseMemberDeclaration(syntax),
                Message = $"Copilot response is of type {type}, but expected method or property",
            })
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    public async Task TestHandlesEmptyReplacementNode(string emptyReplacement)
    {
        await TestHandlesInvalidReplacementNode(
            new()
            {
                ReplacementNode = SyntaxFactory.ParseMemberDeclaration(emptyReplacement),
                Message = "Custom Error Message",
            })
            .ConfigureAwait(false);
    }

    private static async Task TestHandlesInvalidReplacementNode(ImplementationDetails implementationDetails)
    {
        Assumes.False(string.IsNullOrWhiteSpace(implementationDetails.Message));
        await new CustomCompositionCSharpTest
        {
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
            TestCode = """
            using System;

            class C
            {
                void {|IDE3000:M|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            FixedCode = $$"""
            using System;

            class C
            {
                /* {{implementationDetails.Message}} */
                void {|IDE3000:M|}()
                {
                    {|IDE3000:throw new NotImplementedException();|}
                }
            }
            """,
            FixedState =
            {
                MarkupHandling = MarkupMode.Allow,
            },
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }
        .WithMockCopilotService(copilotService =>
        {
            copilotService.PrepareUsingSingleFakeResult = implementationDetails;
        })
        .RunAsync();
    }

    private static ImmutableDictionary<SyntaxNode, ImplementationDetails> BuildResult(ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> memberReferences, Dictionary<string, string> implementationMap)
    {
        // Process each member reference and create implementation details
        var resultsBuilder = ImmutableDictionary.CreateBuilder<SyntaxNode, ImplementationDetails>();
        foreach (var memberReference in memberReferences)
        {
            var memberNode = memberReference.Key;

            // Get the identifier based on node type
            var identifier = memberNode switch
            {
                MethodDeclarationSyntax method => method.Identifier.Text,
                PropertyDeclarationSyntax property => property.Identifier.Text,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
                DestructorDeclarationSyntax destructor => destructor.TildeToken.Text + destructor.Identifier.Text,
                EventDeclarationSyntax @event => @event.Identifier.Text,
                OperatorDeclarationSyntax @operator => "operator " + @operator.OperatorToken.Text,
                _ => string.Empty
            };

            // Look up implementation in our map
            Assumes.True(implementationMap.TryGetValue(identifier, out var implementation));
            resultsBuilder.Add(
                memberNode,
                new ImplementationDetails
                {
                    ReplacementNode = SyntaxFactory.ParseMemberDeclaration(implementation),
                });
        }

        return resultsBuilder.ToImmutable();
    }

    private sealed class CustomCompositionCSharpTest : VerifyCS.Test
    {
        private TestComposition? _testComposition;
        private TestWorkspace? _testWorkspace;
        private Action<TestCopilotCodeAnalysisService>? _copilotServiceSetupAction;

        protected override Task<Workspace> CreateWorkspaceImplAsync()
        {
            _testComposition = FeaturesTestCompositions.Features
                .AddParts([typeof(TestCopilotOptionsService), typeof(TestCopilotCodeAnalysisService)]);
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
    private sealed class TestCopilotOptionsService : ICopilotOptionsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCopilotOptionsService() { }

        public Task<bool> IsRefineOptionEnabledAsync()
            => Task.FromResult(true);

        public Task<bool> IsCodeAnalysisOptionEnabledAsync()
            => Task.FromResult(true);

        public Task<bool> IsOnTheFlyDocsOptionEnabledAsync()
            => Task.FromResult(true);

        public Task<bool> IsGenerateDocumentationCommentOptionEnabledAsync()
            => Task.FromResult(true);

        public Task<bool> IsImplementNotImplementedExceptionEnabledAsync()
            => Task.FromResult(true);
    }

    [ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private sealed class TestCopilotCodeAnalysisService : ICopilotCodeAnalysisService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCopilotCodeAnalysisService()
        {
        }

        public Func<Document, ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>>, CancellationToken, ImmutableDictionary<SyntaxNode, ImplementationDetails>>? SetupFixAll { get; internal set; }

        public ImplementationDetails? PrepareUsingSingleFakeResult { get; internal set; }

        public Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, TextSpan? span, ImmutableArray<string> promptTitles, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> ICopilotCodeAnalysisService.GetDocumentationCommentAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsAsync(
            Document document,
            ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
            CancellationToken cancellationToken)
        {
            if (SetupFixAll != null)
            {
                return Task.FromResult(SetupFixAll.Invoke(document, methodOrProperties, cancellationToken));
            }

            if (PrepareUsingSingleFakeResult != null)
            {
                return Task.FromResult(CreateSingleNodeResult(methodOrProperties, PrepareUsingSingleFakeResult));
            }

            return Task.FromResult(ImmutableDictionary<SyntaxNode, ImplementationDetails>.Empty);
        }

        private static ImmutableDictionary<SyntaxNode, ImplementationDetails> CreateSingleNodeResult(
            ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
            ImplementationDetails implementationDetails)
        {
            var resultsBuilder = ImmutableDictionary.CreateBuilder<SyntaxNode, ImplementationDetails>();
            foreach (var methodOrProperty in methodOrProperties)
            {
                resultsBuilder.Add(methodOrProperty.Key, implementationDetails);
            }

            return resultsBuilder.ToImmutable();
        }

        public Task<bool> IsImplementNotImplementedExceptionsAvailableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<string> GetOnTheFlyDocsPromptAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseAsync(string prompt, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
