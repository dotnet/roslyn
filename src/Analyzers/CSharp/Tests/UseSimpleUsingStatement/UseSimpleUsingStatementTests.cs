// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement;

using VerifyCS = CSharpCodeFixVerifier<
    UseSimpleUsingStatementDiagnosticAnalyzer,
    UseSimpleUsingStatementCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
public class UseSimpleUsingStatementTests
{
    [Fact]
    public async Task TestAboveCSharp8()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestWithOptionOff()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiDeclaration()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public async Task TestMissingIfOnSimpleUsingStatement()
    {
        await new VerifyCS.Test
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public async Task TestMissingPriorToCSharp8()
    {
        await new VerifyCS.Test
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public async Task TestMissingIfExpressionUsing()
    {
        await new VerifyCS.Test
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public async Task TestMissingIfCodeFollows()
    {
        await new VerifyCS.Test
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public async Task TestAsyncUsing()
    {
        // not actually legal code.
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
    public async Task TestAwaitUsing()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestWithBlockBodyWithContents()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestWithNonBlockBody()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestMultiUsing()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestFixAll1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestFixAll2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestFixAll3()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestFixAll4()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestWithFollowingReturn()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestWithFollowingBreak()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestMissingInSwitchSection()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMissingWithJumpInsideToOutside()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMissingWithJumpBeforeToAfter()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestCollision1()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestNoCollision1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestCollision2()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestNoCollision2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestCollision3()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestNoCollision3()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestCollision4()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestNoCollision4()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestCollision5()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35879")]
    public async Task TestNoCollision5()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37678")]
    public async Task TestCopyTrivia()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37678")]
    public async Task TestMultiCopyTrivia()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact]
    public async Task TestFixAll_WithTrivia()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public async Task TestCopyCompilerDirectiveTrivia()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public async Task TestCopyCompilerDirectiveAndCommentTrivia_AfterRestore()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public async Task TestCopyCompilerDirectiveAndCommentTrivia_BeforeRestore()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public async Task TestCopyCompilerDirectiveAndCommentTrivia_AfterDisable()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public async Task TestCopyCompilerDirectiveAndCommentTrivia_BeforeDisable()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38737")]
    public async Task TestCopyCompilerDirectiveTrivia_PreserveCodeBeforeAndAfterDirective()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38842")]
    public async Task TestNextLineIndentation1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38842")]
    public async Task TestNextLineIndentation2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48586")]
    public async Task TestKeepSurroundingComments()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48586")]
    public async Task TestKeepSurroundingComments2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48586")]
    public async Task TestKeepSurroundingComments3()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public async Task TestWithBlockBodyWithOpeningBracketOnSameLine()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public async Task TestWithBlockBodyWithOpeningBracketOnSameLine2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public async Task TestWithBlockBodyWithOpeningBracketAndCommentOnSameLine()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public async Task TestWithBlockBodyWithOpeningBracketOnSameLineWithNoStatements()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52970")]
    public async Task TestWithBlockBodyWithOpeningBracketOnSameLineAndCommentInBlock()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58911")]
    public async Task TestUsingWithoutSpace()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public async Task TestWithConstantReturn1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public async Task TestWithNonConstantReturn1()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public async Task TestWithLocalFunctions1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public async Task TestWithLocalFunctions2()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42194")]
    public async Task TestWithLocalFunctionsAndConstantReturn()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58861")]
    public async Task TestOpenBraceTrivia1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58861")]
    public async Task TestOpenBraceTrivia2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
    }
}
