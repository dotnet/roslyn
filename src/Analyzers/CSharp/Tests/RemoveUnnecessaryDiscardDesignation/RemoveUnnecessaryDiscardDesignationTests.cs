// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryDiscardDesignation;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryDiscardDesignationDiagnosticAnalyzer,
    CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryDiscardDesignation)]
public class RemoveUnnecessaryDiscardDesignationTests
{
    [Fact]
    public async Task TestDeclarationPatternInSwitchStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        switch (o)
                        {
                            case int [|_|]:
                                break;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object o)
                    {
                        switch (o)
                        {
                            case int:
                                break;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        switch (o)
                        {
                            case int _:
                                break;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact]
    public async Task TestDeclarationPatternInSwitchExpression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            int [|_|] => 0,
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            int => 0,
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestDeclarationPatternInIfStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        if (o is int [|_|]) { }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object o)
                    {
                        if (o is int) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRecursivePropertyPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            { } [|_|] => 0,
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            { } => 0,
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEmptyRecursiveParameterPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            () [|_|] => 0,
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            () => 0,
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoElementRecursiveParameterPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            (int i, int j) [|_|] => 0,
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            (int i, int j) => 0,
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithOneElementRecursiveParameterPattern()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(object o)
                    {
                        var v = o switch
                        {
                            (int i) _ => 0,
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNestedFixAll()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(string o)
                    {
                        var v = o switch
                        {
                            { Length: int [|_|] } [|_|] => 1,
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(string o)
                    {
                        var v = o switch
                        {
                            { Length: int } => 1,
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestPropertyWithTheSameNameAsType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    string String { get; }

                    void M(object o)
                    {
                        if (o is String [|_|])
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
            
                class C
                {
                    string String { get; }
            
                    void M(object o)
                    {
                        if (o is String)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestNotWhenRemovingDiscardChangesMeaning1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    string String { get; }

                    void M(object o)
                    {
                        if (o is not String _)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestNotWhenRemovingDiscardChangesMeaning2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    string String { get; }

                    void M(object o)
                    {
                        var v = o switch
                        {
                            String _ => 0
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestNestedPropertyWithTheSameNameAsNestedType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class D
                {
                    public class Length
                    {
                    }
                }

                class C
                {
                    string D { get; }

                    void M(object o)
                    {
                        if (o is D.Length [|_|])
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class D
                {
                    public class Length
                    {
                    }
                }
                
                class C
                {
                    string D { get; }
                
                    void M(object o)
                    {
                        if (o is D.Length)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestNotWhenRemovingDiscardChangesMeaning3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                
                class D
                {
                    public class Length
                    {
                    }
                }
                
                class C
                {
                    string D { get; }
                
                    void M(object o)
                    {
                        if (o is not D.Length _)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestNotWhenRemovingDiscardChangesMeaning4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class D
                {
                    public class Length
                    {
                    }
                }

                class C
                {
                    string D { get; }

                    void M(object o)
                    {
                        var v = o switch
                        {
                            D.Length _ => 0
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestPropertyNamedGlobalAndAliasQualifiedName1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string global { get; }

                    void M(object o)
                    {
                        if (o is global::System.String [|_|])
                        {
                        }
                    }
                }
                """,
            FixedCode = """ 
                class C
                {
                    string global { get; }
                
                    void M(object o)
                    {
                        if (o is global::System.String)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestPropertyNamedGlobalAndAliasQualifiedName2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string global { get; }

                    void M(object o)
                    {
                        if (o is not global::System.String [|_|])
                        {
                        }
                    }
                }
                """,
            FixedCode = """ 
                class C
                {
                    string global { get; }
                
                    void M(object o)
                    {
                        if (o is not global::System.String)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public async Task TestPropertyNamedGlobalAndAliasQualifiedName3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string global { get; }

                    void M(object o)
                    {
                        var v = o switch
                        {
                            global::System.String [|_|] => 0
                        };
                    }
                }
                """,
            FixedCode = """ 
                class C
                {
                    string global { get; }
                
                    void M(object o)
                    {
                        var v = o switch
                        {
                            global::System.String => 0
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }
}
