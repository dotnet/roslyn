// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
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

        await TestFailureAsync<MethodDeclarationSyntax>(testCode);
    }

    [Fact]
    public async Task TestAdditionalContextWithTypeAsParameter()
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: actualSymbolText);
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

        await TestSuccessAsync<InvocationExpressionSyntax>(testCode, customTypeText, customStructText);
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

        await TestSuccessAsync<ObjectCreationExpressionSyntax>(testCode, customClassText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customClassText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customClassText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customStructText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customMessageText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customConfigText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customRefText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customPersonText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customValueText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customBaseText);
    }

    [Fact]
    public async Task TestParametersWithTupleType()
    {
        var testCode = """
        class C
        {
            void ProcessTuple((CustomUser user, CustomUser2 user2) data)
            {
                System.Console.WriteLine($"{data.user.Name}: {data.user2.Name}");
            }
        }

        class CustomUser
        {
            public string Name { get; set; }
        }

        class CustomUser2
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

        var customUser2Text = """
        class CustomUser2
        {
            public string Name { get; set; }
        }
        """;

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, customUserText, customUser2Text);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: interfaceText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: helperText);
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: nestedText);
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
            expectedText: keyText);
    }

    [Fact]
    public async Task TestParametersWithPointerType()
    {
        var testCode = """
        unsafe class C
            
        {
            void ProcessPointer(CustomStruct* ptr)
            {
                System.Console.WriteLine(ptr->Value);
            }
        }

        struct CustomStruct
        {
            public int Value;
        }
        """;

        var customStructText = """
        struct CustomStruct
        {
            public int Value;
        }
        """;

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customStructText);
    }

    [Fact]
    public async Task TestParametersWithPartialType()
    {
        var testCode = """
        class C
        {
            void ProcessPartial(PartialType data)
            {
                System.Console.WriteLine(data.Name);
                System.Console.WriteLine(data.Id);
            }
        }

        partial class PartialType
        {
            public string Name { get; set; }
        }

        partial class PartialType
        {
            public int Id { get; set; }
        }
        """;

        var partialTypeFirstPart = """
        partial class PartialType
        {
            public string Name { get; set; }
        }
        """;

        var partialTypeSecondPart = """
        partial class PartialType
        {
            public int Id { get; set; }
        }
        """;

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, partialTypeFirstPart, partialTypeSecondPart);
    }

    [Fact]
    public async Task TestPropertyInitializers()
    {
        var testCode = """
        class C
        {
            public CustomType PropertyWithInitializer { get; set; } = new CustomType();
        }

        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        var customTypeText = """
        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var propertyDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(propertyDeclaration)!;
            },
            expectedText: customTypeText);
    }

    [Fact]
    public async Task TestPropertyAccessors()
    {
        var testCode = """
        class C
        {
            private CustomType _value;
            
            public CustomType Property 
            { 
                get => _value; 
                set => _value = value; 
            }
        }

        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        var customTypeText = """
        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var propertyDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(propertyDeclaration)!;
            },
            expectedText: customTypeText);
    }

    [Fact]
    public async Task TestConstructors()
    {
        var testCode = """
        class C
        {
            public C(CustomParam param)
            {
                Console.WriteLine(param.Value);
            }
        }

        class CustomParam
        {
            public int Value { get; set; }
        }
        """;

        var customParamText = """
        class CustomParam
        {
            public int Value { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var constructorDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<ConstructorDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(constructorDeclaration)!;
            },
            expectedText: customParamText);
    }

    [Fact]
    public async Task TestOperators()
    {
        var testCode = """
        class CustomNumeric
        {
            public int Value { get; set; }
            
            public static CustomNumeric operator +(CustomNumeric a, CustomNumeric b)
            {
                return new CustomNumeric { Value = a.Value + b.Value };
            }
        }
        """;

        var customNumericText = """
        class CustomNumeric
        {
            public int Value { get; set; }
            
            public static CustomNumeric operator +(CustomNumeric a, CustomNumeric b)
            {
                return new CustomNumeric { Value = a.Value + b.Value };
            }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var operatorDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<OperatorDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(operatorDeclaration)!;
            },
            expectedText: customNumericText);
    }

    [Fact]
    public async Task TestIndexerParameters()
    {
        var testCode = """
        class CustomDictionary
        {
            private Dictionary<CustomKey, string> _dict = new Dictionary<CustomKey, string>();
            
            public string this[CustomKey key]
            {
                get => _dict.TryGetValue(key, out var value) ? value : string.Empty;
                set => _dict[key] = value;
            }
        }

        class CustomKey
        {
            public string Id { get; set; }
            public override int GetHashCode() => Id.GetHashCode();
            public override bool Equals(object obj) => obj is CustomKey key && Id == key.Id;
        }
        """;

        var customKeyText = """
        class CustomKey
        {
            public string Id { get; set; }
            public override int GetHashCode() => Id.GetHashCode();
            public override bool Equals(object obj) => obj is CustomKey key && Id == key.Id;
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var indexerDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<IndexerDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(indexerDeclaration)!;
            },
            expectedText: customKeyText);
    }

    [Fact]
    public async Task TestLocalFunctions()
    {
        var testCode = """
        class C
        {
            public void Method()
            {
                LocalFunction(new CustomType());
                
                void LocalFunction(CustomType param)
                {
                    System.Console.WriteLine(param.Name);
                }
            }
        }

        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        var customTypeText = """
        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var localFunctionStatement = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<LocalFunctionStatementSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(localFunctionStatement)!;
            },
            expectedText: customTypeText);
    }

    [Fact]
    public async Task TestTopLevelFunctions()
    {
        var testCode = """
        using System;
        
        void TopLevelFunction(CustomParam param)
        {
            Console.WriteLine(param.Value);
        }

        class CustomParam
        {
            public int Value { get; set; }
        }
        """;

        var customParamText = """
        class CustomParam
        {
            public int Value { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var functionDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<GlobalStatementSyntax>()
                    .First()
                    .DescendantNodes()
                    .OfType<LocalFunctionStatementSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(functionDeclaration)!;
            },
            expectedText: customParamText);
    }

    [Fact]
    public async Task TestLambdaExpressions()
    {
        var testCode = """
        using System;
        
        class C
        {
            public void Method()
            {
                Action<CustomType> lambda = (CustomType param) => 
                {
                    Console.WriteLine(param.Name);
                };
                
                lambda(new CustomType());
            }
        }

        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        var customTypeText = """
        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var lambdaExpression = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<ParenthesizedLambdaExpressionSyntax>()
                    .First();

                return semanticModel.GetSymbolInfo(lambdaExpression).Symbol!;
            },
            expectedText: customTypeText);
    }

    [Fact]
    public async Task TestAnonymousMethodExpressions()
    {
        var testCode = """
        using System;
        
        class C
        {
            public void Method()
            {
                Action<CustomType> anonymousMethod = delegate(CustomType param)
                {
                    Console.WriteLine(param.Name);
                };
                
                anonymousMethod(new CustomType());
            }
        }

        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        var customTypeText = """
        class CustomType
        {
            public string Name { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var anonymousMethod = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<AnonymousMethodExpressionSyntax>()
                    .First();

                return semanticModel.GetSymbolInfo(anonymousMethod).Symbol!;
            },
            expectedText: customTypeText);
    }

    [Fact]
    public async Task TestGenericMethodWithConstraints()
    {
        var testCode = """
        class C
        {
            public void Process<T>(T item) where T : CustomBase
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

        await TestSuccessAsync<MethodDeclarationSyntax>(testCode, expectedText: customBaseText);
    }

    [Fact]
    public async Task TestDelegateDeclaration()
    {
        var testCode = """
        using System;

        public delegate void CustomCallback(CustomEventArgs args);

        class C
        {
            public event CustomCallback OnSomething;
            
            public void RaiseEvent()
            {
                OnSomething?.Invoke(new CustomEventArgs { Message = "Event raised" });
            }
        }

        class CustomEventArgs
        {
            public string Message { get; set; }
        }
        """;

        var customEventArgsText = """
        class CustomEventArgs
        {
            public string Message { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var delegateDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<DelegateDeclarationSyntax>()
                    .First();

                return semanticModel.GetDeclaredSymbol(delegateDeclaration)!;
            },
            expectedText: customEventArgsText);
    }

    [Fact]
    public async Task TestEventDeclaration()
    {
        var testCode = """
        using System;

        public delegate void CustomCallback(CustomEventArgs args);

        class C
        {
            public event CustomCallback OnSomething;
        }

        class CustomEventArgs
        {
            public string Message { get; set; }
        }
        """;

        var customEventArgsText = """
        class CustomEventArgs
        {
            public string Message { get; set; }
        }
        """;

        await TestSymbolContextAsync(
            testCode,
            (syntaxTree, semanticModel) =>
            {
                var eventDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<EventFieldDeclarationSyntax>()
                    .First()
                    .Declaration
                    .Variables
                    .First();

                return semanticModel.GetDeclaredSymbol(eventDeclaration)!;
            },
            expectedText: customEventArgsText);
    }

    private static async Task<(EditorTestWorkspace workspace, Document document, SemanticModel semanticModel)> SetupWorkspaceAsync(string testCode)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(testCode);
        var solution = workspace.CurrentSolution;
        var document = solution.Projects.First().Documents.First();

        var semanticModel = await document.GetSemanticModelAsync();

        return (workspace, document, semanticModel!);
    }

    private static async Task TestFailureAsync<TSyntaxNode>([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode) where TSyntaxNode : SyntaxNode
    {
        var results = await GetOnTheFlyDocsResults(testCode, GetSymbol<TSyntaxNode>);
        Assert.True(results.All(item => item == null));
    }

    private static Task TestSuccessAsync<TSyntaxNode>(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode, [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] params string[] expectedText) where TSyntaxNode : SyntaxNode
    {
        return TestSymbolContextAsync(
            testCode,
            GetSymbol<TSyntaxNode>,
            expectedText);
    }

    private static ISymbol? GetSymbol<TSyntaxNode>(SyntaxTree syntaxTree, SemanticModel semanticModel) where TSyntaxNode : SyntaxNode
    {
        var node = syntaxTree.GetRoot().DescendantNodes().OfType<TSyntaxNode>().FirstOrDefault();

        if (node == null)
        {
            return null;
        }

        if (node is InvocationExpressionSyntax invocation)
        {
            return semanticModel.GetSymbolInfo(invocation).Symbol!;
        }
        else if (node is ObjectCreationExpressionSyntax objectCreation)
        {
            return semanticModel.GetTypeInfo(objectCreation).Type!;
        }
        else if (node is MethodDeclarationSyntax methodDeclaration)
        {
            return semanticModel.GetDeclaredSymbol(methodDeclaration)!;
        }
        else
        {
            return null;
        }
    }

    private static async Task TestSymbolContextAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        Func<SyntaxTree, SemanticModel, ISymbol?> getSymbol,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] params string[] expectedText)
    {
        var results = await GetOnTheFlyDocsResults(testCode, getSymbol);

        Assert.Equal(expectedText.Length, results.Count());
        for (var i = 0; i < expectedText.Length; i++)
        {
            if (!expectedText[i].IsNullOrEmpty() && results[i] != null)
            {
                await AssertSymbolTextMatchesAsync(results[i]!, expectedText[i]);
            }
        }
    }

    private static async Task<ImmutableArray<OnTheFlyDocsRelevantFileInfo>> GetOnTheFlyDocsResults(string testCode, Func<SyntaxTree, SemanticModel, ISymbol?> getSymbol)
    {
        var (_, document, semanticModel) = await SetupWorkspaceAsync(testCode);

        var symbol = getSymbol(semanticModel.SyntaxTree, semanticModel);
        var results = OnTheFlyDocsUtilities.GetAdditionalOnTheFlyDocsContext(document.Project.Solution, symbol!);
        return results;
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
