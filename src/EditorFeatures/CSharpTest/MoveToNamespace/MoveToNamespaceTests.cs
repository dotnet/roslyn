// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveToNamespace;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
public sealed class MoveToNamespaceTests : AbstractMoveToNamespaceTests
{
    private static readonly TestComposition s_compositionWithoutOptions = FeaturesTestCompositions.Features
        .AddParts(typeof(TestSymbolRenamedCodeActionOperationFactoryWorkspaceService));

    private static readonly TestComposition s_composition = s_compositionWithoutOptions
        .AddParts(typeof(TestMoveToNamespaceOptionsService));

    protected override TestComposition GetComposition() => s_composition;

    protected override ParseOptions GetScriptOptions() => Options.Script;

    protected internal override string GetLanguage() => LanguageNames.CSharp;

    public static IEnumerable<object[]> SupportedKeywords
        => [
            ["class"],
            ["enum"],
            ["interface"]
        ];

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretAboveNamespace()
        => TestMoveToNamespaceAsync(
            """
            using System;
            [||]
            namespace A
            {
                class MyClass
                {
                }
            }
            """,
            expectedSuccess: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59716")]
    public Task MoveToNamespace_MoveItems_CaretAboveNamespace_FileScopedNamespace()
        => TestMoveToNamespaceAsync(
            """
            using System;
            [||]
            namespace A;

            class MyClass
            {
            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretAboveNamespace2()
        => TestMoveToNamespaceAsync(
            """
            using System;[||]

            namespace A
            {
                class MyClass
                {
                }
            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveItems_WeirdNamespace()
        => TestMoveToNamespaceAsync(
            """
            namespace A  [||].    B   .   C
            {
                class MyClass
                {
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:A|}
            {
                class MyClass
                {
                }
            }
            """,
            targetNamespace: "A",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.B.C.MyClass", "A.MyClass" }
            });

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretOnNamespaceName()
        => TestMoveToNamespaceAsync(
            """
            namespace A[||]
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|}
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyClass", "B.MyClass" }
            });

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretOnNamespaceName2()
        => TestMoveToNamespaceAsync(
            """
            namespace A[||].B.C
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|}
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.B.C.MyClass", "B.MyClass" }
            });

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword()
        => TestMoveToNamespaceAsync(
            """
            namespace[||] A
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|}
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyClass", "B.MyClass"}
            }
            );

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword2()
        => TestMoveToNamespaceAsync(
            """
            [||]namespace A
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|}
            {
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyClass", "B.MyClass"}
            });

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretOnNamespaceBrace()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            [||]{
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            expectedSuccess: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59716")]
    public Task MoveToNamespace_MoveItems_CaretAfterFileScopedNamespaceSemicolon()
        => TestMoveToNamespaceAsync(
            """
            namespace A;  [||]

            class MyClass
            {
                void Method() { }
            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveItems_CaretOnNamespaceBrace2()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {[||]
                class MyClass
                {
                    void Method() { }
                }
            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveItems_MultipleDeclarations()
        => TestMoveToNamespaceAsync(
            """
            namespace A[||]
            {
                class MyClass
                {
                    void Method() { }
                }

                class MyOtherClass
                {
                    void Method() { }
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|}
            {
                class MyClass
                {
                    void Method() { }
                }

                class MyOtherClass
                {
                    void Method() { }
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyClass", "B.MyClass" },
                {"A.MyOtherClass", "B.MyOtherClass" }
            });

    [Fact]
    public Task MoveToNamespace_MoveItems_WithVariousSymbols()
        => TestMoveToNamespaceAsync(
            """
            namespace A[||]
            {
                public delegate void MyDelegate();

                public enum MyEnum
                {
                    One,
                    Two,
                    Three
                }

                public struct MyStruct
                { }

                public interface MyInterface
                { }

                class MyClass
                {
                    void Method() { }
                }

                class MyOtherClass
                {
                    void Method() { }
                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|}
            {
                public delegate void MyDelegate();

                public enum MyEnum
                {
                    One,
                    Two,
                    Three
                }

                public struct MyStruct
                { }

                public interface MyInterface
                { }

                class MyClass
                {
                    void Method() { }
                }

                class MyOtherClass
                {
                    void Method() { }
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyDelegate", "B.MyDelegate" },
                {"A.MyEnum", "B.MyEnum" },
                {"A.MyStruct", "B.MyStruct" },
                {"A.MyInterface", "B.MyInterface" },
                {"A.MyClass", "B.MyClass" },
                {"A.MyOtherClass", "B.MyOtherClass" }
            });

    [Fact]
    public Task MoveToNamespace_MoveItems_NestedNamespace()
        => TestMoveToNamespaceAsync(
            """
            namespace A[||]
            {
                namespace C 
                {
                    class MyClass
                    {
                        void Method() { }
                    }
                }
            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveItems_NestedNamespace2()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                namespace C[||]
                {
                    class MyClass
                    {
                        void Method() { }
                    }
                }
            }
            """,
            expectedSuccess: false);

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Nested(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                class MyClass
                {
                    {{typeKeyword}} NestedType[||]
                    {
                    }
                }
            }
            """,
            expectedSuccess: false);

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Single(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType[||]
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyType", "B.MyType" }
            });

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/59716")]
    [MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Single_FileScopedNamespace(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A;

            {{typeKeyword}} MyType[||]
            {
            }

            """,
            expectedMarkup: $$"""
            namespace {|Warning:B|};

            {{typeKeyword}} MyType
            {
            }

            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyType", "B.MyType" }
            });

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_SingleTop(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType[||]
                {
                }

                {{typeKeyword}} MyType2
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType
                {
                }
            }

            namespace A
            {
                {{typeKeyword}} MyType2
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyType", "B.MyType" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_TopWithReference()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                class MyClass[||] : IMyClass
                {
                }

                interface IMyClass
                {
                }
            }
            """,
            expectedMarkup: """
            using A;

            namespace {|Warning:B|}
            {
                class MyClass : IMyClass
                {
                }
            }

            namespace A
            {
                interface IMyClass
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyClass", "B.MyClass" }
            });

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Bottom(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }

                {{typeKeyword}} MyType2[||]
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }
            }

            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType2
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyType2", "B.MyType2" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_BottomReference()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                class MyClass : IMyClass
                {
                }

                interface IMyClass[||]
                {
                }
            }
            """,
            expectedMarkup: """
            using B;

            namespace A
            {
                class MyClass : IMyClass
                {
                }
            }

            namespace {|Warning:B|}
            {
                interface IMyClass
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.IMyClass", "B.IMyClass" }
            });

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Middle(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }

                {{typeKeyword}} MyType2[||]
                {
                }

                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }
            }

            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType2
                {
                }
            }

            namespace A
            {
                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyType2", "B.MyType2" }
            });

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Middle_CaretBeforeKeyword(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }

                [||]{{typeKeyword}} MyType2
                {
                }

                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }
            }

            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType2
                {
                }
            }

            namespace A
            {
                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyType2", "B.MyType2" }
            });

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Middle_CaretAfterTypeKeyword(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }

                {{typeKeyword}}[||] MyType2
                {
                }

                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }
            }

            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType2
                {
                }
            }

            namespace A
            {
                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyType2", "B.MyType2" }
            });

    [Theory, MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveType_Middle_CaretBeforeTypeName(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }

                {{typeKeyword}} [||]MyType2
                {
                }

                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }
            }

            namespace {|Warning:B|}
            {
                {{typeKeyword}} MyType2
                {
                }
            }

            namespace A
            {
                {{typeKeyword}} MyType3
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyType2", "B.MyType2" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_CaretInMethod()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                class MyClass
                {
                    public string [||]MyMethod
                    {
                        return ";
                    }
                }

            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveType_MiddleReference()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                class MyClass : IMyClass
                {
                }

                interface IMyClass[||]
                {
                }

                class MyClass3 : IMyClass
                {
                }
            }
            """,
            expectedMarkup: """
            using B;

            namespace A
            {
                class MyClass : IMyClass
                {
                }
            }

            namespace {|Warning:B|}
            {
                interface IMyClass
                {
                }
            }

            namespace A
            {
                class MyClass3 : IMyClass
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.IMyClass", "B.IMyClass" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_MiddleReference2()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                class MyClass : IMyClass
                {
                }

                interface IMyClass
                {
                }

                class [||]MyClass3 : IMyClass
                {
                }

                class MyClass4
                {
                }
            }
            """,
            expectedMarkup: """
            using A;

            namespace A
            {
                class MyClass : IMyClass
                {
                }

                interface IMyClass
                {
                }
            }

            namespace {|Warning:B|}
            {
                class MyClass3 : IMyClass
                {
                }
            }

            namespace A
            {
                class MyClass4
                {
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyClass3", "B.MyClass3" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_NestedInNamespace()
        => TestMoveToNamespaceAsync(
            """
            namespace A
            {
                class MyClass
                {
                }

                namespace B
                {
                    interface [||]IMyClass
                    {
                    }
                }

                class MyClass2 : B.IMyClass
                {
                }
            }
            """,
            expectedSuccess: false);

    [Fact]
    public Task MoveToNamespace_MoveType_Cancelled()
        => TestCancelledOption(
            """
            namespace A
            {
                class MyClass
                {
                }

                interface [||]IMyClass
                {
                }

                class MyClass2 : B.IMyClass
                {
                }
            }
            """);

    [Fact]
    public Task MoveToNamespace_MoveItems_Cancelled()
        => TestCancelledOption(
            """
            namespace A[||]
            {
                class MyClass
                {
                }

                interface IMyClass
                {
                }

                class MyClass2 : B.IMyClass
                {
                }
            }
            """);

    [Fact]
    public Task MoveToNamespace_MoveType_MiddleReference_ComplexName()
        => TestMoveToNamespaceAsync(
            """
            namespace A.B.C
            {
                class MyClass : IMyClass
                {
                }

                interface IMyClass
                {
                }

                class [||]MyClass3 : IMyClass
                {
                }

                class MyClass4
                {
                }
            }
            """,
            expectedMarkup: """
            using A.B.C;

            namespace A.B.C
            {
                class MyClass : IMyClass
                {
                }

                interface IMyClass
                {
                }
            }

            namespace {|Warning:My.New.Namespace|}
            {
                class MyClass3 : IMyClass
                {
                }
            }

            namespace A.B.C
            {
                class MyClass4
                {
                }
            }
            """,
            targetNamespace: "My.New.Namespace",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.B.C.MyClass3", "My.New.Namespace.MyClass3" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_MiddleReference_ComplexName2()
       => TestMoveToNamespaceAsync(
           """
           namespace A
           {
               class MyClass : IMyClass
               {
               }

               interface IMyClass
               {
               }

               class [||]MyClass3 : IMyClass
               {
               }

               class MyClass4
               {
               }
           }
           """,
           expectedMarkup: """
           using A;

           namespace A
           {
               class MyClass : IMyClass
               {
               }

               interface IMyClass
               {
               }
           }

           namespace {|Warning:My.New.Namespace|}
           {
               class MyClass3 : IMyClass
               {
               }
           }

           namespace A
           {
               class MyClass4
               {
               }
           }
           """,
           targetNamespace: "My.New.Namespace",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.MyClass3", "My.New.Namespace.MyClass3" }
            });

    [Fact]
    public Task MoveToNamespace_MoveType_MiddleReference_ComplexName3()
        => TestMoveToNamespaceAsync(
           """
           namespace A.B.C
           {
               class MyClass : IMyClass
               {
               }

               interface IMyClass
               {
               }

               class [||]MyClass3 : IMyClass
               {
               }

               class MyClass4
               {
               }
           }
           """,
           expectedMarkup: """
           using A.B.C;

           namespace A.B.C
           {
               class MyClass : IMyClass
               {
               }

               interface IMyClass
               {
               }
           }

           namespace {|Warning:B|}
           {
               class MyClass3 : IMyClass
               {
               }
           }

           namespace A.B.C
           {
               class MyClass4
               {
               }
           }
           """,
           targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"A.B.C.MyClass3", "B.MyClass3" }
            });

    [Fact]
    public Task MoveToNamespace_Analysis_MoveItems_ComplexNamespace()
       => TestMoveToNamespaceAnalysisAsync(
           """
           namespace [||]A.Complex.Namespace
           {
               class MyClass
               {
               }
           }
           """,
           expectedNamespaceName: "A.Complex.Namespace");

    [Fact]
    public Task MoveToNamespace_Analysis_MoveType_ComplexNamespace()
        => TestMoveToNamespaceAnalysisAsync(
            """
            namespace A.Complex.Namespace
            {
                class [||]MyClass
                {
                }
            }
            """,
            expectedNamespaceName: "A.Complex.Namespace");

    [Fact]
    public Task MoveToNamespace_Analysis_MoveItems_WeirdNamespace()
        => TestMoveToNamespaceAnalysisAsync(
            """
            namespace A  [||].    B   .   C
            {
                class MyClass
                {
                }
            }
            """,
            expectedNamespaceName: "A  .    B   .   C");

    [Fact]
    public Task MoveToNamespace_Analysis_MoveType_WeirdNamespace()
        => TestMoveToNamespaceAnalysisAsync(
            """
            namespace A  .    B   .   C
            {
                class MyClass[||]
                {
                }
            }
            """,
            expectedNamespaceName: "A  .    B   .   C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34736")]
    public Task MoveToNamespace_MoveType_Usings()
        => TestMoveToNamespaceAsync(
            """
            namespace One
            {
                using Two;
                class C1
                {
                    private C2 c2;
                }
            }

            namespace [||]Two
            {
                class C2
                {

                }
            }
            """,
            expectedMarkup: """
            namespace One
            {
                using Three;
                class C1
                {
                    private C2 c2;
                }
            }

            namespace {|Warning:Three|}
            {
                class C2
                {

                }
            }
            """,
            targetNamespace: "Three",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"Two.C2", "Three.C2" }
            });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35577")]
    public async Task MoveToNamespace_WithoutOptionsService()
    {
        var code = """
            namespace A[||]
            {
            class MyClass
            {
                void Method() { }
            }
            }
            """;

        using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: s_compositionWithoutOptions);
        using var testState = new TestState(workspace);
        Assert.Null(testState.TestMoveToNamespaceOptionsService);

        var actions = await testState.MoveToNamespaceService.GetCodeActionsAsync(
            testState.InvocationDocument,
            testState.TestInvocationDocument.SelectedSpans.Single(),
            CancellationToken.None);

        Assert.Empty(actions);
    }

    [Theory, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
    [MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveOnlyTypeInGlobalNamespace(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            {{typeKeyword}} MyType[||]
            {
            }
            """,
            expectedMarkup: $$"""
            namespace {|Warning:A|}
            {
                {{typeKeyword}} MyType
                {
                }
            }
            """,
            targetNamespace: "A",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
            {"MyType", "A.MyType" }
            });

    [Theory, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
    [MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveOnlyTypeToGlobalNamespace(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                {{typeKeyword}} MyType[||]
                {
                }
            }
            """,
            expectedMarkup: $$"""
            namespace A
            {
                {{typeKeyword}} MyType
                {
                }
            }
            """,
            targetNamespace: "");

    [Theory, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
    [MemberData(nameof(SupportedKeywords))]
    public Task MoveToNamespace_MoveOneTypeInGlobalNamespace(string typeKeyword)
        => TestMoveToNamespaceAsync(
            $$"""
            {{typeKeyword}} MyType1[||]
            {
            }

            {{typeKeyword}} MyType2
            {
            }
            """,
            expectedSuccess: false);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
    public Task MoveToNamespace_PartialTypesInNamesapce_SelectType()
        => TestMoveToNamespaceAsync(
            """
            namespace NS
            {
                partial class MyClass[||]
                {
                }

                partial class MyClass
                {
                }
            }
            """,
            expectedSuccess: false);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
    public Task MoveToNamespace_PartialTypesInNamesapce_SelectNamespace()
        => TestMoveToNamespaceAsync(
            """
            namespace NS[||]
            {
                partial class MyClass
                {
                }

                partial class MyClass
                {
                }
            }
            """,
            expectedSuccess: false);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
    public Task MoveToNamespace_PartialTypesInGlobalNamesapce()
        => TestMoveToNamespaceAsync(
            """
            partial class MyClass[||]
            {
            }
            partial class MyClass
            {
            }
            """,
            expectedSuccess: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39234")]
    public async Task TestMultiTargetingProject()
    {
        // Create two projects with same project file path and single linked document to simulate a multi-targeting project.
        var input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" FilePath="SharedProj.csproj">
                    <Document FilePath="CurrentDocument.cs">
            namespace A
            {
                public class Class1
                {
                }

                public class Class2[||]
                {
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" FilePath="SharedProj.csproj">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """;
        using var workspace = EditorTestWorkspace.Create(System.Xml.Linq.XElement.Parse(input), composition: s_composition, openDocuments: false);

        // Set the target namespace to "B"
        var testDocument = workspace.Projects.Single(p => p.Name == "Proj1").Documents.Single();
        var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);
        var movenamespaceService = document.GetRequiredLanguageService<IMoveToNamespaceService>();
        var moveToNamespaceOptions = new MoveToNamespaceOptionsResult("B");
        ((TestMoveToNamespaceOptionsService)movenamespaceService.OptionsService).SetOptions(moveToNamespaceOptions);

        var (_, action) = await GetCodeActionsAsync(workspace);
        var operations = await VerifyActionAndGetOperationsAsync(workspace, action);
        var result = await ApplyOperationsAndGetSolutionAsync(workspace, operations);

        // Make sure both linked documents are changed.
        foreach (var id in workspace.Documents.Select(d => d.Id))
        {
            var changedDocument = result.Item2.GetRequiredDocument(id);
            var changedRoot = await changedDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var actualText = changedRoot.ToFullString();
            AssertEx.Equal("""
            namespace A
            {
                public class Class1
                {
                }
            }

            namespace B
            {
                public class Class2
                {
                }
            }
            """, actualText);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35507")]
    public Task MoveToNamespace_MoveTypeFromSystemNamespace()
        => TestMoveToNamespaceAsync(
            """
            namespace System
            {
                [||]class A
                {

                }
            }
            """,
            expectedMarkup: """
            namespace {|Warning:Test|}
            {
                [||]class A
                {

                }
            }
            """,
            targetNamespace: "Test",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"System.A", "Test.A" }
            });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54889")]
    public Task MoveToNamespace_MoveType_NoFormat()
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A
            {
                class MyType[||]
                {
                    public const string ShortName       = "";
                    public const string VeryLongName    = "";
                }

                struct MyType2
                {
                    public const string ShortName       = "";
                    public const string VeryLongName    = "";
                }
            }
            """,
            expectedMarkup: $$"""
            namespace {|Warning:B|}
            {
                class MyType
                {
                    public const string ShortName       = "";
                    public const string VeryLongName    = "";
                }
            }

            namespace A
            {
                struct MyType2
                {
                    public const string ShortName       = "";
                    public const string VeryLongName    = "";
                }
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyType", "B.MyType" }
            });

    [Theory]
    [InlineData("class MyClass[||](int x, int y)")]
    [InlineData("class [||]MyClass(int x, int y)")]
    [InlineData("class MyC[||]lass(int x, int y)")]
    public Task MoveToNamespace_PrimaryConstructor(string decl)
        => TestMoveToNamespaceAsync(
            $$"""
            namespace A;

            {{decl}}
            {
                public int X => x;
                public int Y => y;
            }
            """,
            expectedMarkup: """
            namespace {|Warning:B|};
            
            class MyClass(int x, int y)
            {
                public int X => x;
                public int Y => y;
            }
            """,
            targetNamespace: "B",
            expectedSymbolChanges: new Dictionary<string, string>()
            {
                {"A.MyClass", "B.MyClass" }
            });
}
