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
public sealed class RemoveUnnecessaryDiscardDesignationTests
{
    [Fact]
    public Task TestDeclarationPatternInSwitchStatement()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotInCSharp8()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestDeclarationPatternInSwitchExpression()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestDeclarationPatternInIfStatement()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestRecursivePropertyPattern()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestEmptyRecursiveParameterPattern()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTwoElementRecursiveParameterPattern()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotWithOneElementRecursiveParameterPattern()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNestedFixAll()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestPropertyWithTheSameNameAsType()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestNotWhenRemovingDiscardChangesMeaning1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestNotWhenRemovingDiscardChangesMeaning2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestNestedPropertyWithTheSameNameAsNestedType()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestNotWhenRemovingDiscardChangesMeaning3()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestNotWhenRemovingDiscardChangesMeaning4()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestPropertyNamedGlobalAndAliasQualifiedName1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestPropertyNamedGlobalAndAliasQualifiedName2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66841")]
    public Task TestPropertyNamedGlobalAndAliasQualifiedName3()
        => new VerifyCS.Test
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
