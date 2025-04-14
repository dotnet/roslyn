// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.OnTheFlyDocs;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.OnTheFlyDocs)]
public sealed class OnTheFlyDocsUtilitiesTests
{
    [Fact]
    public async Task TestAdditionalContextNoContext()
    {
        var testCode = """
            class C
            {
                void AddMethod(int a, int b)
                {
                    return a + b;
                }
            }
            """;

        await TestMethodContextAsync(testCode, expectAllNull: true);
    }

    [Fact]
    public async Task TestAdditionalContextWithTypeParameters()
    {
        var testCode = """
            class C
            {
                int AddMethod(A a, int b)
                {
                    return a.x + b;
                }
            }

            class A
            {
                public int x;
            }
            """;

        var actualSymbolText = """
            class A
            {
                public int x;
            }
            """;

        await TestMethodContextAsync(testCode, actualSymbolText);
    }

    [Fact]
    public async Task TestAdditionalContextWithTypeArguments()
    {
        var testCode = """
            class C
            {
                void Method<T, U>(T t, U u) where T : class where U : struct
                {
                }

                void CallMethod()
                {
                    Method<CustomClass, CustomStruct>(new CustomClass(), new CustomStruct());
                }
            }

            class CustomClass
            {
                public string Name { get; set; }
            }

            struct CustomStruct
            {
                public int Value { get; set; }
            }
            """;

        var customTypeText = """
            class CustomClass
            {
                public string Name { get; set; }
            }
            """;

        var customStructText = """
            struct CustomStruct
            {
                public int Value { get; set; }
            }
            """;

        await TestInvocationContextAsync(testCode, customTypeText, resultIndex: 0);
        await TestInvocationContextAsync(testCode, customStructText, resultIndex: 1);
    }

    [Fact]
    public async Task TestAdditionalContextWithTypeArgumentsButNoParameters()
    {
        var testCode = """
        class C
        {
            Dictionary<string, int> dictionary;

            void Method()
            {
                var list = new List<CustomClass>();
            }
        }

        class CustomClass
        {
            public string Name { get; set; }
        }
        """;

        var customClassText = """
        class CustomClass
        {
            public string Name { get; set; }
        }
        """;

        await TestObjectCreationTypeContextAsync(testCode, customClassText);
    }

    [Fact]
    public async Task TestParametersWithRefModifier()
    {
        var testCode = """
        class C
        {
            void Method(ref CustomClass a, int b)
            {
                a.Name = "Modified";
            }
        }

        class CustomClass
        {
            public string Name { get; set; }
        }
        """;

        var customClassText = """
        class CustomClass
        {
            public string Name { get; set; }
        }
        """;

        await TestMethodContextAsync(testCode, customClassText);
    }

    [Fact]
    public async Task TestParametersWithOutModifier()
    {
        var testCode = """
        class C
        {
            bool TryGetValue(int key, out CustomClass result)
            {
                result = new CustomClass();
                return true;
            }
        }

        class CustomClass
        {
            public string Name { get; set; }
        }
        """;

        var customClassText = """
        class CustomClass
        {
            public string Name { get; set; }
        }
        """;

        await TestMethodContextAsync(testCode, customClassText, resultIndex: 1);
    }

    [Fact]
    public async Task TestParametersWithInModifier()
    {
        var testCode = """
        class C
        {
            void ProcessReadOnly(in CustomStruct data)
            {
                int value = data.Value;
            }
        }

        readonly struct CustomStruct
        {
            public readonly int Value;
            
            public CustomStruct(int value)
            {
                Value = value;
            }
        }
        """;

        var customStructText = """
        readonly struct CustomStruct
        {
            public readonly int Value;
            
            public CustomStruct(int value)
            {
                Value = value;
            }
        }
        """;

        await TestMethodContextAsync(testCode, customStructText);
    }

    [Fact]
    public async Task TestParametersWithParamsModifier()
    {
        var testCode = """
        class C
        {
            void LogMessages(string prefix, params CustomMessage[] messages)
            {
                foreach (var msg in messages)
                {
                    System.Console.WriteLine($"{prefix}: {msg.Text}");
                }
            }
        }

        class CustomMessage
        {
            public string Text { get; set; }
        }
        """;

        var customMessageText = """
        class CustomMessage
        {
            public string Text { get; set; }
        }
        """;

        await TestMethodContextAsync(testCode, customMessageText, resultIndex: 1);
    }

    [Fact]
    public async Task TestParametersWithOptionalValue()
    {
        var testCode = """
        class C
        {
            void ProcessWithDefaults(CustomConfig config = null)
            {
                config ??= new CustomConfig();
                System.Console.WriteLine(config.Name);
            }
        }

        class CustomConfig
        {
            public string Name { get; set; } = "Default";
        }
        """;

        var customConfigText = """
        class CustomConfig
        {
            public string Name { get; set; } = "Default";
        }
        """;

        await TestMethodContextAsync(testCode, customConfigText);
    }

    [Fact]
    public async Task TestParametersWithScopedModifier()
    {
        var testCode = """
        using System;

        class C
        {
            void ProcessScoped(scoped CustomRef reference)
            {
                Console.WriteLine(reference.Id);
            }
        }

        ref struct CustomRef
        {
            public int Id;
            
            public CustomRef(int id)
            {
                Id = id;
            }
        }
        """;

        var customRefText = """
        ref struct CustomRef
        {
            public int Id;
            
            public CustomRef(int id)
            {
                Id = id;
            }
        }
        """;

        await TestMethodContextAsync(testCode, customRefText);
    }

    [Fact]
    public async Task TestParametersWithNullableReferenceTypes()
    {
        var testCode = """
        #nullable enable
        
        class C
        {
            void ProcessName(CustomPerson? person)
            {
                if (person != null)
                {
                    System.Console.WriteLine(person.Name);
                }
            }
        }

        class CustomPerson
        {
            public string Name { get; set; } = "";
        }
        """;

        var customPersonText = """
        class CustomPerson
        {
            public string Name { get; set; } = "";
        }
        """;

        await TestMethodContextAsync(testCode, customPersonText);
    }

    [Fact]
    public async Task TestParametersWithNullableValueTypes()
    {
        var testCode = """
        class C
        {
            void ProcessValue(CustomValue? value)
            {
                if (value.HasValue)
                {
                    System.Console.WriteLine(value.Value.Data);
                }
            }
        }

        struct CustomValue
        {
            public int Data { get; set; }
        }
        """;

        var customValueText = """
        struct CustomValue
        {
            public int Data { get; set; }
        }
        """;

        await TestMethodContextAsync(testCode, customValueText);
    }

    [Fact]
    public async Task TestParametersWithGenericConstraints()
    {
        var testCode = """
        class C
        {
            void ProcessItems<T>(T item) where T : CustomBase
            {
                System.Console.WriteLine(item.Id);
            }
        }

        class CustomBase
        {
            public int Id { get; set; }
        }
        """;

        var customBaseText = """
        class CustomBase
        {
            public int Id { get; set; }
        }
        """;

        await TestMethodContextAsync(testCode, customBaseText);
    }

    [Fact]
    public async Task TestParametersWithTupleType()
    {
        var testCode = """
        class C
        {
            void ProcessTuple((CustomUser user, int count) data)
            {
                System.Console.WriteLine($"{data.user.Name}: {data.count}");
            }
        }

        class CustomUser
        {
            public string Name { get; set; }
        }
        """;

        var customUserText = """
        class CustomUser
        {
            public string Name { get; set; }
        }
        """;

        await TestMethodContextAsync(testCode, customUserText);
    }

    [Fact]
    public async Task TestParametersWithInterfaceTypes()
    {
        var testCode = """
        class C
        {
            void ProcessInterface(ICustomInterface item)
            {
                System.Console.WriteLine(item.Name);
            }
        }

        interface ICustomInterface
        {
            string Name { get; }
        }
        """;

        var interfaceText = """
        interface ICustomInterface
        {
            string Name { get; }
        }
        """;

        await TestMethodContextAsync(testCode, interfaceText);
    }

    [Fact]
    public async Task TestParametersWithDynamicType()
    {
        var testCode = """
        class C
        {
            void ProcessDynamic(dynamic item, CustomHelper helper)
            {
                helper.Process(item);
            }
        }

        class CustomHelper
        {
            public void Process(object item) { }
        }
        """;

        var helperText = """
        class CustomHelper
        {
            public void Process(object item) { }
        }
        """;

        await TestMethodContextAsync(testCode, helperText, resultIndex: 1);
    }

    [Fact]
    public async Task TestParametersWithMultipleLevelNestedTypes()
    {
        var testCode = """
        class C
        {
            void ProcessNested(CustomOuter.CustomInner.DeepNested nested)
            {
                System.Console.WriteLine(nested.Value);
            }
        }

        class CustomOuter
        {
            public class CustomInner
            {
                public class DeepNested
                {
                    public int Value { get; set; }
                }
            }
        }
        """;

        var nestedText = """
        public class DeepNested
                {
                    public int Value { get; set; }
                }
        """;

        await TestMethodContextAsync(testCode, nestedText);
    }

    [Fact]
    public async Task TestIndexerParameterTypes()
    {
        var testCode = """
        class C
        {
            CustomCollection collection = new CustomCollection();
            
            void UseCollection()
            {
                var item = collection[new CustomKey(5)];
            }
        }

        class CustomCollection
        {
            public string this[CustomKey key] => key.Id.ToString();
        }

        class CustomKey
        {
            public int Id { get; }
            
            public CustomKey(int id)
            {
                Id = id;
            }
        }
        """;

        var keyText = """
        class CustomKey
        {
            public int Id { get; }
            
            public CustomKey(int id)
            {
                Id = id;
            }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var classDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text == "CustomCollection");

                var indexerDeclaration = classDeclaration!.Members
                    .OfType<IndexerDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(indexerDeclaration)!;
            },
            keyText);
    }

    private static async Task<(Solution Solution, Document Document, SemanticModel SemanticModel, SyntaxTree SyntaxTree)>
        SetupWorkspaceAsync(string testCode)
    {
        var workspace = EditorTestWorkspace.CreateCSharp(testCode);
        var solution = workspace.CurrentSolution;
        var document = solution.Projects.First().Documents.First();

        var syntaxTree = await document.GetSyntaxTreeAsync();
        var semanticModel = await document.GetSemanticModelAsync();

        return (solution, document, semanticModel!, syntaxTree!);
    }

    private static ISymbol GetMethodSymbol(SyntaxTree syntaxTree, SemanticModel semanticModel)
    {
        var methodDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();

        return semanticModel.GetDeclaredSymbol(methodDeclaration)!;
    }

    private static async Task TestSymbolContextAsync(
        string testCode,
        Func<SyntaxTree, SemanticModel, ISymbol> getSymbol,
        string? expectedText = null,
        bool expectAllNull = false,
        int resultIndex = 0)
    {
        var (solution, _, semanticModel, syntaxTree) = await SetupWorkspaceAsync(testCode);

        var symbol = getSymbol(syntaxTree, semanticModel);
        var result = OnTheFlyDocsUtilities.GetAdditionalOnTheFlyDocsContext(solution, symbol);

        if (expectAllNull)
        {
            Assert.True(result.All(item => item == null));
            return;
        }

        var relevantInfo = result[resultIndex];
        Assert.NotNull(relevantInfo);

        if (expectedText != null)
        {
            await AssertSymbolTextMatchesAsync(relevantInfo!, expectedText);
        }
    }

    private static Task TestMethodContextAsync(string testCode, string? expectedText = null, bool expectAllNull = false, int resultIndex = 0)
    {
        return TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) => GetMethodSymbol(syntaxTree, semanticModel),
            expectedText,
            expectAllNull,
            resultIndex);
    }

    private static Task TestObjectCreationTypeContextAsync(string testCode, string expectedText)
    {
        return TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var objectCreation = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .First();

                return semanticModel.GetTypeInfo(objectCreation).Type!;
            },
            expectedText);
    }

    private static Task TestInvocationContextAsync(string testCode, string? expectedText = null, bool expectAllNull = false, int resultIndex = 0)
    {
        return TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var methodInvocation = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .First();

                return semanticModel.GetSymbolInfo(methodInvocation).Symbol!;
            },
            expectedText,
            expectAllNull,
            resultIndex);
    }

    private static async Task AssertSymbolTextMatchesAsync(OnTheFlyDocsRelevantFileInfo onTheFlyDocsRelevantFileInfo, string actualSymbolText)
    {
        var document = onTheFlyDocsRelevantFileInfo.Document;
        var textSpan = onTheFlyDocsRelevantFileInfo.TextSpan;

        var text = await document.GetTextAsync();
        var symbolText = text.GetSubText(textSpan).ToString();
        Assert.Equal(actualSymbolText, symbolText);
    }
}
