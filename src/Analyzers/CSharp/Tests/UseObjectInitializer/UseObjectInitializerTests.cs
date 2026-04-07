// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseObjectInitializer;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseObjectInitializerDiagnosticAnalyzer,
    CSharpUseObjectInitializerCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
public sealed partial class UseObjectInitializerTests
{
    private static async Task TestMissingInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        LanguageVersion? languageVersion = null)
    {
        var test = new VerifyCS.Test
        {
            TestCode = testCode,
        };

        if (languageVersion != null)
            test.LanguageVersion = languageVersion.Value;

        await test.RunAsync();
    }

    [Fact]
    public Task TestOnVariableDeclarator()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestNotForField1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C c = new C();
            }
            """);

    [Fact]
    public Task TestNotForField2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C c = new C() { };
            }
            """);

    [Fact]
    public Task TestNotForField3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C c = new C { };
            }
            """);

    [Fact]
    public Task TestNotForField4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int P;
                C c = new C() { P = 1 };
            }
            """);

    [Fact]
    public Task TestNotForField5()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int P;
                C c = new C { P = 1 };
            }
            """);

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue1Async()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.i = c.i + 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.i = c.i + 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue2Async()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int i;

                void M()
                {
                    var c = new C();
                    c.i = c.i + 1;
                }
            }
            """);

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue3Async()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    C c;
                    c = [|new|] C();
                    [|c.|]i = 1;
                    c.i = c.i + 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    C c;
                    c = new C
                    {
                        i = 1
                    };
                    c.i = c.i + 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestDoNotUpdateAssignmentThatReferencesInitializedValue4Async()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int i;

                void M()
                {
                    C c;
                    c = new C();
                    c.i = c.i + 1;
                }
            }
            """);

    [Fact]
    public Task TestOnAssignmentExpression()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = null;
                    c = [|new|] C();
                    [|c.|]i = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = null;
                    c = new C
                    {
                        i = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestStopOnDuplicateMember()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.i = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.i = 2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestComplexInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = [|new|] C();
                    [|array[0].|]i = 1;
                    [|array[0].|]j = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = new C
                    {
                        i = 1,
                        j = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestNotOnCompoundAssignment()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                    c.j += 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                    c.j += 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C() { i = 1 };
                    [|c.|]j = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializerComma()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C()
                    {
                        i = 1,
                    };
                    [|c.|]j = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializerNotIfAlreadyInitialized()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = [|new|] C()
                    {
                        i = 1,
                    };
                    [|c.|]j = 1;
                    c.i = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M()
                {
                    var c = new C
                    {
                        i = 1,
                        j = 1
                    };
                    c.i = 2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestMissingBeforeCSharp3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int i;
                int j;

                void M()
                {
                    C c = new C();
                    c.j = 1;
                }
            }
            """, LanguageVersion.CSharp2);

    [Fact]
    public Task TestFixAllInDocument1()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                public C() { }
                public C(System.Action a) { }

                void M()
                {
                    var v = [|new|] C(() => {
                        var v2 = [|new|] C();
                        [|v2.|]i = 1;
                    });
                    [|v.|]j = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                public C() { }
                public C(System.Action a) { }

                void M()
                {
                    var v = new C(() =>
                    {
                        var v2 = new C
                        {
                            i = 1
                        };
                    })
                    {
                        j = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestFixAllInDocument2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                System.Action j;

                void M()
                {
                    var v = [|new|] C();
                    [|v.|]j = () => {
                        var v2 = [|new|] C();
                        [|v2.|]i = 1;
                    };
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                System.Action j;

                void M()
                {
                    var v = new C
                    {
                        j = () =>
                        {
                            var v2 = new C
                            {
                                i = 1
                            };
                        }
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestFixAllInDocument3()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = [|new|] C();
                    [|array[0].|]i = 1;
                    [|array[0].|]j = 2;
                    array[1] = [|new|] C();
                    [|array[1].|]i = 3;
                    [|array[1].|]j = 4;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;

                void M(C[] array)
                {
                    array[0] = new C
                    {
                        i = 1,
                        j = 2
                    };
                    array[1] = new C
                    {
                        i = 3,
                        j = 4
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestTrivia1()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1; // Goo
                    [|c.|]j = 2; // Bar
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = new C
                    {
                        i = 1, // Goo
                        j = 2 // Bar
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46670")]
    public Task TestTriviaRemoveLeadingBlankLinesForFirstProperty()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = [|new|] C();

                    //Goo
                    [|c.|]i = 1;

                    //Bar
                    [|c.|]j = 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;
                int j;
                void M()
                {
                    var c = new C
                    {
                        //Goo
                        i = 1,

                        //Bar
                        j = 2
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15459")]
    public Task TestMissingInNonTopLevelObjectInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            class C {
            	int a;
            	C Add(int x) {
            		var c = Add(new int());
            		c.a = 1;
            		return c;
            	}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17853")]
    public Task TestMissingForDynamic()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Dynamic;

            class C
            {
                void Goo()
                {
                    dynamic body = new ExpandoObject();
                    body.content = new ExpandoObject();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public Task TestMissingAcrossPreprocessorDirective()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public void M()
                {
                    var goo = new Goo();
            #if true
                    goo.Value = "";
            #endif
                }

                public string Value { get; set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public Task TestAvailableInsidePreprocessorDirective()
        => new VerifyCS.Test
        {
            TestCode = """
            public class Goo
            {
                public void M()
                {
            #if true
                    var goo = [|new|] Goo();
                    [|goo.|]Value = "";
            #endif
                }

                public string Value { get; set; }
            }
            """,
            FixedCode = """
            public class Goo
            {
                public void M()
                {
            #if true
                    var goo = new Goo
                    {
                        Value = ""
                    };
            #endif
                }

                public string Value { get; set; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19253")]
    public Task TestKeepBlankLinesAfter()
        => new VerifyCS.Test
        {
            TestCode = """
            class Goo
            {
                public int Bar { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    var goo = [|new|] Goo();
                    [|goo.|]Bar = 1;

                    int horse = 1;
                }
            }
            """,
            FixedCode = """
            class Goo
            {
                public int Bar { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    var goo = new Goo
                    {
                        Bar = 1
                    };

                    int horse = 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")]
    public Task TestWithExplicitImplementedInterfaceMembers1()
        => TestMissingInRegularAndScriptAsync(
            """
            interface IExample {
                string Name { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = new C();
                    e.Name = string.Empty;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")]
    public Task TestWithExplicitImplementedInterfaceMembers2()
        => TestMissingInRegularAndScriptAsync(
            """
            interface IExample {
                string Name { get; set; }
                string LastName { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
                public string LastName { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = new C();
                    e.Name = string.Empty;
                    e.LastName = string.Empty;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23368")]
    public Task TestWithExplicitImplementedInterfaceMembers3()
        => new VerifyCS.Test
        {
            TestCode = """
            interface IExample {
                string Name { get; set; }
                string LastName { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
                public string LastName { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = [|new|] C();
                    [|e.|]LastName = string.Empty;
                    e.Name = string.Empty;
                }
            }
            """,
            FixedCode = """
            interface IExample {
                string Name { get; set; }
                string LastName { get; set; }
            }

            class C : IExample {
                string IExample.Name { get; set; }
                public string LastName { get; set; }
            }

            class MyClass
            {
                public void Main()
                {
                    IExample e = new C
                    {
                        LastName = string.Empty
                    };
                    e.Name = string.Empty;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37675")]
    public Task TestDoNotOfferForUsingDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            class C : System.IDisposable
            {
                int i;

                void M()
                {
                    using var c = new C();
                    c.i = 1;
                }

                public void Dispose()
                {
                }
            }
            """);

    [Fact]
    public Task TestImplicitObject()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = [|new|]();
                    [|c.|]i = 1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                int i;

                void M()
                {
                    C c = new()
                    {
                        i = 1
                    };
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61066")]
    public Task TestInTopLevelStatements()
        => new VerifyCS.Test
        {
            TestCode = """
            MyClass cl = [|new|]();
            [|cl.|]MyProperty = 5;

            class MyClass
            {
                public int MyProperty { get; set; }
            }
            """,
            FixedCode = """
            MyClass cl = new()
            {
                MyProperty = 5
            };

            class MyClass
            {
                public int MyProperty { get; set; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/72094")]
    public async Task TestWithConflictingSeverityConfigurationEntries(bool enabled)
    {
        string testCode, fixedCode;
        if (enabled)
        {
            testCode =
                """
                class C
                {
                    int i;
            
                    void M()
                    {
                        var c = [|new|] C();
                        c.i = 1;
                    }
                }
                """;

            fixedCode =
                """
                class C
                {
                    int i;
            
                    void M()
                    {
                        var c = new C
                        {
                            i = 1
                        };
                    }
                }
                """;
        }
        else
        {
            testCode =
                """
                class C
                {
                    int i;
            
                    void M()
                    {
                        var c = new C();
                        c.i = 1;
                    }
                }
                """;
            fixedCode = testCode;
        }

        var globalConfig =
            $"""
            is_global = true

            dotnet_style_object_initializer = true:suggestion
            dotnet_diagnostic.IDE0017.severity = none

            build_property.EnableCodeStyleSeverity = {enabled}
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", globalConfig),
                }
            },
            FixedState = { Sources = { fixedCode } },
            LanguageVersion = LanguageVersion.CSharp12,
        };

        await test.RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task TestFallbackSeverityConfiguration(bool enabled)
    {
        var testCode =
            """
            class C
            {
                int i;
            
                void M()
                {
                    var c = [|new|] C();
                    [|c.|]i = 1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                int i;
            
                void M()
                {
                    var c = new C
                    {
                        i = 1
                    };
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
            is_global = true

            dotnet_style_object_initializer = true
            dotnet_diagnostic.IDE0017.severity = warning

            build_property.EnableCodeStyleSeverity = {enabled}
            """),
                }
            },
            FixedState = { Sources = { fixedCode } },
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S = i
                            .ToString();
                        [|c.|]T = i.
                            ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S = i
                                .ToString(),
                            T = i.
                                ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S = i
                            .ToString()
                            .ToString();
                        [|c.|]T = i.
                            ToString().
                            ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S = i
                                .ToString()
                                .ToString(),
                            T = i.
                                ToString().
                                ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions3()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S =
                            i.ToString();
                        [|c.|]T =
                            i.ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S =
                                i.ToString(),
                            T =
                                i.ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46665")]
    public Task TestIndentationOfMultiLineExpressions4()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string S;
                    string T;

                    void M(int i)
                    {
                        var c = [|new|] C();
                        [|c.|]S =
                            i.ToString()
                             .ToString();
                        [|c.|]T =
                            i.ToString()
                             .ToString();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string S;
                    string T;
                
                    void M(int i)
                    {
                        var c = [|new|] C
                        {
                            S =
                                i.ToString()
                                 .ToString(),
                            T =
                                i.ToString()
                                 .ToString()
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
}
