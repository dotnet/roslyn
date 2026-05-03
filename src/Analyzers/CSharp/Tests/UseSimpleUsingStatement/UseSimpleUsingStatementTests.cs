// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement;

using VerifyCS = CSharpCodeFixVerifier<
    UseSimpleUsingStatementDiagnosticAnalyzer,
    UseSimpleUsingStatementCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
public sealed class UseSimpleUsingStatementTests
{
    [Fact]
    public Task TestAboveCSharp8()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    {
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                }
            }
            """);

    [Fact]
    public Task TestWithOptionOff()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        using (var a = {|CS0103:b|})
                        {
                        }
                    }
                }
                """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferSimpleUsingStatement, CodeStyleOption2.FalseWithSilentEnforcement }
            }
        }.RunAsync();

    [Fact]
    public Task TestMultiDeclaration()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] ({|CS0819:var a = {|CS0103:b|}, c = {|CS0103:d|}|})
                    {
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using {|CS0819:var a = {|CS0103:b|}, c = {|CS0103:d|}|};
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingIfOnSimpleUsingStatement()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        using var a = {|CS0103:b|};
                    }
                }
                """
        }.RunAsync();

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingPriorToCSharp8()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        using (var a = {|CS0103:b|})
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_2
        }.RunAsync();

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingIfExpressionUsing()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        using ({|CS0103:a|})
                        {
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestMissingIfCodeFollows()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        using (var a = {|CS0103:b|})
                        {
                        }
                        Console.WriteLine();
                    }
                }
                """
        }.RunAsync();

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestAsyncUsing()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    {|CS0103:async|} {|CS1002:[|using|]|} (var a = {|CS0103:b|})
                    {
                    }
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    {|CS0103:async|} {|CS1002:using|} var a = {|CS0103:b|};
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public Task TestAwaitUsing()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    {|CS4033:await|} [|using|] (var a = {|CS0103:b|})
                    {
                    }
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    {|CS4033:await|} using var a = {|CS0103:b|};
                }
            }
            """);

    [Fact]
    public Task TestWithBlockBodyWithContents()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task TestWithNonBlockBody()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                        Console.WriteLine(a);
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task TestMultiUsing()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    using (var c = {|CS0103:d|})
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    using var c = {|CS0103:d|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    {
                        [|using|] (var c = {|CS0103:d|})
                        {
                            Console.WriteLine(a);
                        }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    using var c = {|CS0103:d|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    using (var c = {|CS0103:d|})
                    {
                        [|using|] (var e = {|CS0103:f|})
                        using (var g = {|CS0103:h|})
                        {
                            Console.WriteLine(a);
                        }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    using var c = {|CS0103:d|};
                    using var e = {|CS0103:f|};
                    using var g = {|CS0103:h|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task TestFixAll3()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    using (var c = {|CS0103:d|})
                    {
                        using ({|CS0103:e|})
                        using ({|CS0103:f|})
                        {
                            Console.WriteLine(a);
                        }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    using var c = {|CS0103:d|};
                    using ({|CS0103:e|})
                    using ({|CS0103:f|})
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact]
    public Task TestFixAll4()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    using (var a = {|CS0103:b|}) { }
                    [|using|] (var c = {|CS0103:d|}) { }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using (var a = {|CS0103:b|}) { }
                    using var c = {|CS0103:d|};
                }
            }
            """);

    [Fact]
    public Task TestWithFollowingReturn()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    {
                    }
                    return;
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    return;
                }
            }
            """);

    [Fact]
    public Task TestWithFollowingBreak()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    switch (0)
                    {
                        case 0:
                            {
                                [|using|] (var a = {|CS0103:b|})
                                {
                                }
                                break;
                            }
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    switch (0)
                    {
                        case 0:
                            {
                                using var a = {|CS0103:b|};
                                break;
                            }
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingInSwitchSection()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        switch (0)
                        {
                            case 0:
                                using (var a = {|CS0103:b|})
                                {
                                }
                                break;
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact]
    public Task TestMissingWithJumpInsideToOutside()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        label:
                        using (var a = {|CS0103:b|})
                        {
                            goto label;
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact]
    public Task TestMissingWithJumpBeforeToAfter()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        {
                            goto label;
                            using (var a = {|CS0103:b|})
                            {
                            }
                        }
                        label:;
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestCollision1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class Program
                {
                    static void Main()
                    {
                        using (Stream stream = File.OpenRead("test"))
                        {
                        }
                        using (Stream stream = File.OpenRead("test"))
                        {
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestNoCollision1()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    [|using|] (Stream stream1 = File.OpenRead("test"))
                    {
                    }
                }
            }
            """, """
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    using Stream stream1 = File.OpenRead("test");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestCollision2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class Program
                {
                    static void Main()
                    {
                        using (Stream stream = File.OpenRead("test"))
                        {
                        }
                        using (Stream stream1 = File.OpenRead("test"))
                        {
                            Stream stream;
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestNoCollision2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    [|using|] (Stream stream1 = File.OpenRead("test"))
                    {
                        Stream stream2;
                    }
                }
            }
            """, """
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    using Stream stream1 = File.OpenRead("test");
                    Stream stream2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestCollision3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class Program
                {
                    static void Main()
                    {
                        using (Stream stream = File.OpenRead("test"))
                        {
                        }
                        using (Stream stream1 = File.OpenRead("test"))
                        {
                            {|CS0103:Goo|}(out var stream);
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestNoCollision3()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    [|using|] (Stream stream1 = File.OpenRead("test"))
                    {
                        {|CS0103:Goo|}(out var stream2);
                    }
                }
            }
            """, """
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    using Stream stream1 = File.OpenRead("test");
                    {|CS0103:Goo|}(out var stream2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestCollision4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class Program
                {
                    static void Main()
                    {
                        using (Stream stream = File.OpenRead("test"))
                        {
                        }
                        using (Stream stream1 = File.OpenRead("test"))
                            {|CS0103:Goo|}(out var stream);
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestNoCollision4()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    [|using|] (Stream stream1 = File.OpenRead("test"))
                        {|CS0103:Goo|}(out var stream2);
                }
            }
            """, """
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                    }
                    using Stream stream1 = File.OpenRead("test");
                    {|CS0103:Goo|}(out var stream2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestCollision5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class Program
                {
                    static void Main()
                    {
                        using (Stream stream = File.OpenRead("test"))
                        {
                            Stream stream1;
                        }
                        using (Stream stream1 = File.OpenRead("test"))
                        {
                        }
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public Task TestNoCollision5()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                        Stream stream1;
                    }
                    [|using|] (Stream stream2 = File.OpenRead("test"))
                    {
                    }
                }
            }
            """, """
            using System.IO;

            class Program
            {
                static void Main()
                {
                    using (Stream stream = File.OpenRead("test"))
                    {
                        Stream stream1;
                    }
                    using Stream stream2 = File.OpenRead("test");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37678")]
    public Task TestCopyTrivia()
        => VerifyCS.VerifyCodeFixAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|using|] (var x = {|CS0103:y|})
                    {
                        // comment
                    }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    using var x = {|CS0103:y|};
                    // comment
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37678")]
    public Task TestMultiCopyTrivia()
        => VerifyCS.VerifyCodeFixAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|using|] (var x = {|CS0103:y|})
                    using (var a = {|CS0103:b|})
                    {
                        // comment
                    }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    using var x = {|CS0103:y|};
                    using var a = {|CS0103:b|};
                    // comment
                }
            }
            """);

    [Fact]
    public Task TestFixAll_WithTrivia()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    {
                        [|using|] (var c = {|CS0103:d|})
                        {
                            Console.WriteLine(a);
                            // comment1
                        }
                        // comment2
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    using var c = {|CS0103:d|};
                    Console.WriteLine(a);
                    // comment1
                    // comment2
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public Task TestCopyCompilerDirectiveTrivia()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                static void M()
                {
                    [|using|] (var obj = Dummy())
                    {
            #pragma warning disable CS0618, CS0612
            #if !FOO
                        LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                    }
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """, """
            using System;

            class C
            {
                static void M()
                {
                    using var obj = Dummy();
            #pragma warning disable CS0618, CS0612
            #if !FOO
                    LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public Task TestCopyCompilerDirectiveAndCommentTrivia_AfterRestore()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                static void M()
                {
                    [|using|] (var obj = Dummy())
                    {
            #pragma warning disable CS0618, CS0612
            #if !FOO
                        LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                    // comment
                    }
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """, """
            using System;

            class C
            {
                static void M()
                {
                    using var obj = Dummy();
            #pragma warning disable CS0618, CS0612
            #if !FOO
                    LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                    // comment
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public Task TestCopyCompilerDirectiveAndCommentTrivia_BeforeRestore()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                static void M()
                {
                    [|using|] (var obj = Dummy())
                    {
            #pragma warning disable CS0618, CS0612
            #if !FOO
                        LegacyMethod();
                        // comment
            #endif
            #pragma warning restore CS0618, CS0612
                    }
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """, """
            using System;

            class C
            {
                static void M()
                {
                    using var obj = Dummy();
            #pragma warning disable CS0618, CS0612
            #if !FOO
                    LegacyMethod();
                    // comment
            #endif
            #pragma warning restore CS0618, CS0612
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public Task TestCopyCompilerDirectiveAndCommentTrivia_AfterDisable()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                static void M()
                {
                    [|using|] (var obj = Dummy())
                    {
            #pragma warning disable CS0618, CS0612
            #if !FOO
                        // comment
                        LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                    }
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """, """
            using System;

            class C
            {
                static void M()
                {
                    using var obj = Dummy();
            #pragma warning disable CS0618, CS0612
            #if !FOO
                    // comment
                    LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public Task TestCopyCompilerDirectiveAndCommentTrivia_BeforeDisable()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                static void M()
                {
                    [|using|] (var obj = Dummy())
                    {
                        // comment
            #pragma warning disable CS0618, CS0612
            #if !FOO
                        LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                    }
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """, """
            using System;

            class C
            {
                static void M()
                {
                    using var obj = Dummy();
                    // comment
            #pragma warning disable CS0618, CS0612
            #if !FOO
                    LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public Task TestCopyCompilerDirectiveTrivia_PreserveCodeBeforeAndAfterDirective()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                static void M()
                {
                    [|using|] (var obj = Dummy())
                    {
                        LegacyMethod();
            #pragma warning disable CS0618, CS0612
            #if !FOO
                        LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                        LegacyMethod();
                    }
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """, """
            using System;

            class C
            {
                static void M()
                {
                    using var obj = Dummy();
                    LegacyMethod();
            #pragma warning disable CS0618, CS0612
            #if !FOO
                    LegacyMethod();
            #endif
            #pragma warning restore CS0618, CS0612
                    LegacyMethod();
                }

                static IDisposable Dummy() => throw new NotImplementedException();

                [Obsolete]
                static void LegacyMethod() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38842")]
    public Task TestNextLineIndentation1()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void Goo(IDisposable disposable)
                {
                    [|using|] (var v = disposable)
                    {
                        {|CS0103:Bar|}(1,
                            2,
                            3);
                        {|CS1501:Goo|}(1,
                            2,
                            3);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void Goo(IDisposable disposable)
                {
                    using var v = disposable;
                    {|CS0103:Bar|}(1,
                        2,
                        3);
                    {|CS1501:Goo|}(1,
                        2,
                        3);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38842")]
    public Task TestNextLineIndentation2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.IO;

            class C
            {
                static void Main()
                {
                    [|using|] (var stream = new MemoryStream())
                    {
                        _ = new Action(
                                () => { }
                            );
                    }
                }
            }
            """, """
            using System;
            using System.IO;

            class C
            {
                static void Main()
                {
                    using var stream = new MemoryStream();
                    _ = new Action(
                            () => { }
                        );
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48586")]
    public Task TestKeepSurroundingComments()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|})
                    { // Make sure that...
                        Console.WriteLine({|CS0103:s|}.CanRead);
                    } // ...all comments remain
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    // Make sure that...
                    Console.WriteLine({|CS0103:s|}.CanRead);
                    // ...all comments remain
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48586")]
    public Task TestKeepSurroundingComments2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    // Make...
                    [|using|] (var a = {|CS0103:b|}) // ...sure...
                    { // ...that...
                        Console.WriteLine({|CS0103:s|}.CanRead); // ...all...
                    } // ...comments...
                    // ...remain
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    // Make...
                    using var a = {|CS0103:b|}; // ...sure...
                                     // ...that...
                    Console.WriteLine({|CS0103:s|}.CanRead); // ...all...
                                                  // ...comments...
                                                  // ...remain
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48586")]
    public Task TestKeepSurroundingComments3()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    // Make...
                    [|using|] (var a = {|CS0103:b|}) // ...sure...
                    using (var c = {|CS0103:d|}) // ...that...
                    // ...really...
                    using (var e = {|CS0103:f|}) // ...all...
                    { // ...comments...
                        Console.WriteLine({|CS0103:s|}.CanRead); // ...are...
                    } // ...kept...
                    // ...during...
                    // ...transformation
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    // Make...
                    using var a = {|CS0103:b|}; // ...sure...
                    using var c = {|CS0103:d|}; // ...that...
                    // ...really...
                    using var e = {|CS0103:f|}; // ...all...
                                     // ...comments...
                    Console.WriteLine({|CS0103:s|}.CanRead); // ...are...
                                                  // ...kept...
                                                  // ...during...
                                                  // ...transformation
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public Task TestWithBlockBodyWithOpeningBracketOnSameLine()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|}){
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public Task TestWithBlockBodyWithOpeningBracketOnSameLine2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|}) {
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public Task TestWithBlockBodyWithOpeningBracketAndCommentOnSameLine()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|}) { //comment
                        Console.WriteLine(a);
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};  //comment
                    Console.WriteLine(a);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public Task TestWithBlockBodyWithOpeningBracketOnSameLineWithNoStatements()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|}) {
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public Task TestWithBlockBodyWithOpeningBracketOnSameLineAndCommentInBlock()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class C
            {
                void M()
                {
                    [|using|] (var a = {|CS0103:b|}) {
                        // intentionally empty
                    }
                }
            }
            """, """
            using System;

            class C
            {
                void M()
                {
                    using var a = {|CS0103:b|};
                    // intentionally empty
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58911")]
    public Task TestUsingWithoutSpace()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;

            public class Test
            {
                public IEnumerable<Test> Collection { get; } = new[]
                {
                    new Test()
                    {
                        Prop = () =>
                        {
                            [|using|](var x = Get())
                            {
                                int i = 0;
                            }
                        }
                    }
                };

                public Action Prop { get; set; }
                public static IDisposable Get() => throw new NotImplementedException();
            }
            """, """
            using System;
            using System.Collections.Generic;

            public class Test
            {
                public IEnumerable<Test> Collection { get; } = new[]
                {
                    new Test()
                    {
                        Prop = () =>
                        {
                            using var x = Get();
                                int i = 0;
                        }
                    }
                };

                public Action Prop { get; set; }
                public static IDisposable Get() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public Task TestWithConstantReturn1()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class C
            {
                bool M()
                {
                    [|using|] (var foo = new MemoryStream())
                    {
                    }

                    return true;
                }
            }
            """, """
            using System.IO;

            class C
            {
                bool M()
                {
                    using var foo = new MemoryStream();

                    return true;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public Task TestWithNonConstantReturn1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class C
                {
                    bool M(int a, int b)
                    {
                        using (var foo = new MemoryStream())
                        {
                        }

                        return a > b;
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public Task TestWithLocalFunctions1()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    [|using|] (var foo = new MemoryStream())
                    {
                    }

                    void Inner1() { }
                    void Inner2() { }
                }
            }
            """, """
            using System.IO;

            class C
            {
                void M()
                {
                    using var foo = new MemoryStream();

                    void Inner1() { }
                    void Inner2() { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public Task TestWithLocalFunctions2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.IO;

                class C
                {
                    bool M(int a, int b)
                    {
                        using (var foo = new MemoryStream())
                        {
                        }

                        void Inner1() { }
                        void Inner2() { }

                        return a > b;
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public Task TestWithLocalFunctionsAndConstantReturn()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.IO;

            class C
            {
                bool M(int a, int b)
                {
                    [|using|] (var foo = new MemoryStream())
                    {
                    }

                    void Inner1() { }
                    void Inner2() { }

                    return true;
                }
            }
            """, """
            using System.IO;

            class C
            {
                bool M(int a, int b)
                {
                    using var foo = new MemoryStream();

                    void Inner1() { }
                    void Inner2() { }

                    return true;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58861")]
    public Task TestOpenBraceTrivia1()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.Security.Cryptography;

            class C
            {
                public static byte[] ComputeMD5Hash(byte[] source)
                {
            #pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
                    [|using|] (var md5 = MD5.Create())
            #pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
                    {
                        return md5.ComputeHash(source);
                    }
                }
            }
            """, """
            using System.Security.Cryptography;

            class C
            {
                public static byte[] ComputeMD5Hash(byte[] source)
                {
            #pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
                    using var md5 = MD5.Create();
            #pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
                    return md5.ComputeHash(source);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58861")]
    public Task TestOpenBraceTrivia2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.Security.Cryptography;

            class C
            {
                public static byte[] ComputeMD5Hash(byte[] source)
                {
            #pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
                    [|using|] (var md5 = MD5.Create())
            #pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
                    { // comment
                        return md5.ComputeHash(source);
                    }
                }
            }
            """, """
            using System.Security.Cryptography;

            class C
            {
                public static byte[] ComputeMD5Hash(byte[] source)
                {
            #pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
                    using var md5 = MD5.Create();
            #pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
                    // comment
                    return md5.ComputeHash(source);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75917")]
    public Task TestGlobalStatement1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                [|using|] (var c = (IDisposable)null)
                {
                }

                class C
                {
                }
                """,
            FixedCode = """
                using System;
            
                using var c = (IDisposable)null;
            
                class C
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75917")]
    public Task TestGlobalStatement2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                [|using|] (var c = (IDisposable)null)
                {
                    Console.WriteLine();
                }

                class C
                {
                }
                """,
            FixedCode = """
                using System;
            
                using var c = (IDisposable)null;
                Console.WriteLine();
            
                class C
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75917")]
    public Task TestGlobalStatement3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                using (var c = (IDisposable)null)
                {
                }

                Console.WriteLine();

                class C
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75917")]
    public Task TestGlobalStatement4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                [|using|] (var c = (IDisposable)null)
                {
                    Console.WriteLine();
                }

                int LocalFunction() => 0;

                class C
                {
                }
                """,
            FixedCode = """
                using System;
            
                using var c = (IDisposable)null;
                Console.WriteLine();

                int LocalFunction() => 0;
            
                class C
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75917")]
    public Task TestGlobalStatement5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                [|using|] (var c = (IDisposable)null)
                using (var d = (IDisposable)null)
                {
                }

                class C
                {
                }
                """,
            FixedCode = """
                using System;
            
                using var c = (IDisposable)null;
                using var d = (IDisposable)null;
            
                class C
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75917")]
    public Task TestGlobalStatement6()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                [|using|] (var c = (IDisposable)null)
                {
                    [|using|] (var d = (IDisposable)null)
                    {
                    }
                }

                class C
                {
                }
                """,
            FixedCode = """
                using System;
            
                using var c = (IDisposable)null;
                using var d = (IDisposable)null;
            
                class C
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();
}
