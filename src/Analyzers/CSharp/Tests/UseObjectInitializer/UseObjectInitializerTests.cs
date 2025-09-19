﻿// Licensed to the .NET Foundation under one or more agreements.
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
                    var c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,19): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
                    c.i = c.i + 1;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(7,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
                    c.i = c.i + 1;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,19): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,13): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
                    c.i = 2;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(7,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    array[0] = {|#1:{|#0:new|} C()|};
                    {|#2:array[0].|}i = 1{|#3:;|}
                    {|#4:array[0].|}j = 2{|#5:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,20): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3).WithLocation(4).WithLocation(5),
            },
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
                    var c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
                    c.j += 1;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var c = {|#1:{|#0:new|} C() { i = 1 }|};
                    {|#2:c.|}j = 1{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var c = {|#1:{|#0:new|} C()
                    {
                        i = 1,
                    }|};
                    {|#2:c.|}j = 1{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var c = {|#1:{|#0:new|} C()
                    {
                        i = 1,
                    }|};
                    {|#2:c.|}j = 1{|#3:;|}
                    c.i = 2;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var v = {|#1:{|#0:new|} C(() => {
                        var v2 = {|#5:{|#4:new|} C()|};
                        {|#6:v2.|}i = 1{|#7:;|}
                    })|};
                    {|#2:v.|}j = 2{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(11,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
                // /0/Test0.cs(12,22): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(4).WithLocation(5).WithLocation(6).WithLocation(7),
            },
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
                    var v = {|#1:{|#0:new|} C()|};
                    {|#2:v.|}j = () => {
                        var v2 = {|#5:{|#4:new|} C()|};
                        {|#6:v2.|}i = 1{|#7:;|}
                    }{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
                // /0/Test0.cs(10,22): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(4).WithLocation(5).WithLocation(6).WithLocation(7),
            },
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
                    array[0] = {|#1:{|#0:new|} C()|};
                    {|#2:array[0].|}i = 1{|#3:;|}
                    {|#4:array[0].|}j = 2{|#5:;|}
                    array[1] = {|#7:{|#6:new|} C()|};
                    {|#8:array[1].|}i = 3{|#9:;|}
                    {|#10:array[1].|}j = 4{|#11:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(8,20): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3).WithLocation(4).WithLocation(5),
                // /0/Test0.cs(11,20): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(6).WithLocation(7).WithLocation(8).WithLocation(9).WithLocation(10).WithLocation(11),
            },
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
                    var c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|} // Goo
                    {|#4:c.|}j = 2{|#5:;|} // Bar
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(7,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3).WithLocation(4).WithLocation(5),
            },
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
                    var c = {|#1:{|#0:new|} C()|};

                    //Goo
                    {|#2:c.|}i = 1{|#3:;|}

                    //Bar
                    {|#4:c.|}j = 2{|#5:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(7,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3).WithLocation(4).WithLocation(5),
            },
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
                    var goo = {|#1:{|#0:new|} Goo()|};
                    {|#2:goo.|}Value = ""{|#3:;|}
            #endif
                }

                public string Value { get; set; }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,19): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    var goo = {|#1:{|#0:new|} Goo()|};
                    {|#2:goo.|}Bar = 1{|#3:;|}

                    int horse = 1;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,19): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    IExample e = {|#1:{|#0:new|} C()|};
                    {|#2:e.|}LastName = string.Empty{|#3:;|}
                    e.Name = string.Empty;
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(15,22): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                    C c = {|#1:{|#0:new|}()|};
                    {|#2:c.|}i = 1{|#3:;|}
                }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,19): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
            MyClass cl = {|#1:{|#0:new|}()|};
            {|#2:cl.|}MyProperty = 5{|#3:;|}

            class MyClass
            {
                public int MyProperty { get; set; }
            }
            """,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,19): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
            },
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
                        var c = {|#1:{|#0:new|} C()|};
                        {|#2:c.|}i = 1{|#3:;|}
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

        if (enabled)
        {
            test.ExpectedDiagnostics.Add(
                // /0/Test0.cs(7,17): info IDE0017: Object initialization can be simplified
                VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3));
        }

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
                    var c = {|#1:{|#0:new|} C()|};
                    {|#2:c.|}i = 1{|#3:;|}
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
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,17): warning IDE0017: Object initialization can be simplified
                    VerifyCS.Diagnostic().WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithLocation(1).WithLocation(2).WithLocation(3),
                },
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
                        c.S = i
                            .ToString();
                        c.T = i.
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
                        c.S = i
                            .ToString()
                            .ToString();
                        c.T = i.
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
                        c.S =
                            i.ToString();
                        c.T =
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
                        c.S =
                            i.ToString()
                             .ToString();
                        c.T =
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
