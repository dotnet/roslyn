// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace;

using VerifyCS = CSharpCodeFixVerifier<ConvertToFileScopedNamespaceDiagnosticAnalyzer, ConvertNamespaceCodeFixProvider>;

public sealed class ConvertToFileScopedNamespaceAnalyzerTests
{
    [Fact]
    public Task TestNoConvertToFileScopedInCSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertToFileScopedInCSharp10WithBlockScopedPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedInCSharp10WithBlockScopedPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
            }
            """,
            FixedCode = """
            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedInCSharp10WithBlockScopedPreference_NotSilent()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace [|N|]
            {
            }
            """,
            FixedCode = """
            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Suggestion }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithMultipleNamespaces()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
            }

            namespace N2
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithNestedNamespaces1()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                namespace N2
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithTopLevelStatement1()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8805:int i = 0;|}

            namespace N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithTopLevelStatement2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
            }

            int i = 0;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                DiagnosticResult.CompilerError("CS8803").WithSpan(5, 1, 5, 11),
                // /0/Test0.cs(6,1): error CS8805: Program using top-level statements must be an executable.
                DiagnosticResult.CompilerError("CS8805").WithSpan(5, 1, 5, 11),
            },
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithUsing1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            [|namespace N|]
            {
            }
            """,
            FixedCode = """
            using System;

            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithUsing2()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                using System;
            }
            """,
            FixedCode = """
            namespace $$N;

            using System;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithClass()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithClassWithDocComment()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                /// <summary/>
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            /// <summary/>
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithMissingCloseBrace()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                /// <summary/>
                class C
                {
                }{|CS1513:|}
            """,
            FixedCode = """
            namespace N;

            /// <summary/>
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithCommentOnOpenCurly()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            { // comment
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;
            // comment
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithLeadingComment()
        => new VerifyCS.Test
        {
            TestCode = """
            // copyright
            [|namespace N|]
            {
                class C
                {
                }
            }
            """,
            FixedCode = """
            // copyright
            namespace $$N;

            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithDocComment()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                /// <summary/>
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            /// <summary/>
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithPPDirective1()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
            #if X
                class C
                {
                }
            #else
                class C
                {
                }
            #endif
            }
            """,
            FixedCode = """
            namespace $$N;

            #if X
            class C
            {
            }
            #else
            class C
            {
            }
            #endif

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithBlockComment()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                /* x
                 * x
                 */
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            /* x
             * x
             */
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithBlockComment2()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                /* x
                   x
                 */
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            /* x
               x
             */
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithMultilineString()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                class C
                {
                    void M()
                    {
                        System.Console.WriteLine(@"
                a
                    b
                        c
                            d
                                e
                                    ");
                    }
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
                void M()
                {
                    System.Console.WriteLine(@"
                a
                    b
                        c
                            d
                                e
                                    ");
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithMultilineString2()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                class C
                {
                    void M()
                    {
                        System.Console.WriteLine($@"
                a
                    b
                        c{1 + 1}
                            d
                                e
                                    ");
                    }
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
                void M()
                {
                    System.Console.WriteLine($@"
                a
                    b
                        c{1 + 1}
                            d
                                e
                                    ");
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithMultilineString3()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                class C
                {
                    void M()
                    {
                        System.Console.WriteLine($@"
                a
                    b
                        c{
                            1 + 1
                         }d
                                e
                                    ");
                    }
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
                void M()
                {
                    System.Console.WriteLine($@"
                a
                    b
                        c{
                        1 + 1
                     }d
                                e
                                    ");
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Theory, InlineData(""), InlineData("u8")]
    public Task TestConvertToFileScopedWithMultiLineRawString1(string suffix)
        => new VerifyCS.Test
        {
            TestCode = $$""""
            [|namespace N|]
            {
                class C
                {
                    void M()
                    {
                        WriteLine("""
                a
                    b
                        c
                            d
                                e
                """{{suffix}});
                    }

                    void WriteLine(string s) { }
                    void WriteLine(System.ReadOnlySpan<byte> s) { } 
                }
            }
            """",
            FixedCode = $$""""
            namespace $$N;

            class C
            {
                void M()
                {
                    WriteLine("""
            a
                b
                    c
                        d
                            e
            """{{suffix}});
                }
            
                void WriteLine(string s) { }
                void WriteLine(System.ReadOnlySpan<byte> s) { } 
            }
            """",
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Theory, InlineData(""), InlineData("u8")]
    public Task TestConvertToFileScopedWithMultiLineRawString2(string suffix)
        => new VerifyCS.Test
        {
            TestCode = $$""""
            [|namespace N|]
            {
                class C
                {
                    void M()
                    {
                        WriteLine("""
            a
                b
                    c
                        d
                            e
            """{{suffix}});
                    }
            
                    void WriteLine(string s) { }
                    void WriteLine(System.ReadOnlySpan<byte> s) { } 
                }
            }
            """",
            FixedCode = $$""""
            namespace $$N;

            class C
            {
                void M()
                {
                    WriteLine("""
            a
                b
                    c
                        d
                            e
            """{{suffix}});
                }
            
                void WriteLine(string s) { }
                void WriteLine(System.ReadOnlySpan<byte> s) { } 
            }
            """",
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Theory, InlineData(""), InlineData("u8")]
    public Task TestConvertToFileScopedWithMultiLineRawString3(string suffix)
        => new VerifyCS.Test
        {
            TestCode = $$""""
            [|namespace N|]
            {
                class C
                {
                    void M()
                    {
                        System.Console.WriteLine("""
            {|CS8999:|}a // error
                    b
                        c
                            d
                                e
                """{{suffix}});
                    }
            
                    void WriteLine(string s) { }
                    void WriteLine(System.ReadOnlySpan<byte> s) { } 
                }
            }
            """",
            FixedCode = $$""""
            namespace $$N;

            class C
            {
                void M()
                {
                    System.Console.WriteLine("""
            {|CS8999:|}a // error
                    b
                        c
                            d
                                e
                """{{suffix}});
                }
            
                void WriteLine(string s) { }
                void WriteLine(System.ReadOnlySpan<byte> s) { } 
            }
            """",
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedSingleLineNamespace1()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|] { class C { } }
            """,
            FixedCode = """
            namespace $$N; class C { } 
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedSingleLineNamespace2()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            { class C { } }
            """,
            FixedCode = """
            namespace $$N;
            class C { } 
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedWithNoNewlineAtEnd()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedWithNoMembersAndNoNewlineAtEnd()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
            }
            """,
            FixedCode = """
            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedPreserveNewlineAtEnd()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedWithNoMembersPreserveNewlineAtEnd()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace N|]
            {
            }
            """,
            FixedCode = """
            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedPPDirective1()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace Goo|]
            {
            #if true
                class goobar { }
            #endif
            // There must be no CR, LF, or other character after the brace on the following line!
            }
            """,
            FixedCode = """
            namespace $$Goo;

            #if true
            class goobar { }
            #endif
            // There must be no CR, LF, or other character after the brace on the following line!
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedPPDirective2()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace Goo|]
            {
            #if true
                class goobar { }
            #endif
            // There must be no CR, LF, or other character after the brace on the following line!
            }

            """,
            FixedCode = """
            namespace $$Goo;

            #if true
            class goobar { }
            #endif
            // There must be no CR, LF, or other character after the brace on the following line!

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedPPDirective3()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace Goo|]
            {
            #if false
                class goobar { }
            #endif
            }
            """,
            FixedCode = """
            namespace $$Goo;

            #if false
            class goobar { }
            #endif

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59728")]
    public Task TestConvertToFileScopedPPDirective4()
        => new VerifyCS.Test
        {
            TestCode = """
            [|namespace Goo|]
            {
            #if false
                class goobar { }
            #endif
            }

            """,
            FixedCode = """
            namespace $$Goo;

            #if false
            class goobar { }
            #endif

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestInterpolatedRawString1()
        => new VerifyCS.Test
        {
            TestCode = """"
                [|namespace Microsoft.CodeAnalysis.SQLite.v2|]
                {
                    internal partial class SQLitePersistentStorage
                    {
                        private abstract class Accessor<TKey, TDatabaseKey>
                            where TDatabaseKey : struct
                        {
                            private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;

                            public Accessor()
                            {
                                _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                    insert or replace into {0}.{1}
                                    ({2})
                                    """;

                                return;

                                string GetSelectRowIdQuery(string database)
                                    => $"""
                                        select rowid from {0}.{1} where
                                        {2}
                                        limit 1
                                        """;
                            }
                        }
                    }
                }
                """",
            FixedCode = """"
                namespace $$Microsoft.CodeAnalysis.SQLite.v2;

                internal partial class SQLitePersistentStorage
                {
                    private abstract class Accessor<TKey, TDatabaseKey>
                        where TDatabaseKey : struct
                    {
                        private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;
                
                        public Accessor()
                        {
                            _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                insert or replace into {0}.{1}
                                ({2})
                                """;
                
                            return;
                
                            string GetSelectRowIdQuery(string database)
                                => $"""
                                    select rowid from {0}.{1} where
                                    {2}
                                    limit 1
                                    """;
                        }
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestInterpolatedRawString2()
        => new VerifyCS.Test
        {
            TestCode = """"
                [|namespace Microsoft.CodeAnalysis.SQLite.v2|]
                {
                    internal partial class SQLitePersistentStorage
                    {
                        private abstract class Accessor<TKey, TDatabaseKey>
                            where TDatabaseKey : struct
                        {
                            private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;

                            public Accessor()
                            {
                                _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                    insert or replace into {0}.{1}
                                    ({2})
                                    """;

                                return;

                                string GetSelectRowIdQuery(string database) => $"""
                                        select rowid from {0}.{1} where
                                        {2}
                                        limit 1
                                        """;
                            }
                        }
                    }
                }
                """",
            FixedCode = """"
                namespace $$Microsoft.CodeAnalysis.SQLite.v2;

                internal partial class SQLitePersistentStorage
                {
                    private abstract class Accessor<TKey, TDatabaseKey>
                        where TDatabaseKey : struct
                    {
                        private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;
                
                        public Accessor()
                        {
                            _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                insert or replace into {0}.{1}
                                ({2})
                                """;
                
                            return;
                
                            string GetSelectRowIdQuery(string database) => $"""
                                    select rowid from {0}.{1} where
                                    {2}
                                    limit 1
                                    """;
                        }
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestInterpolatedRawString3()
        => new VerifyCS.Test
        {
            TestCode = """"
                [|namespace Microsoft.CodeAnalysis.SQLite.v2|]
                {
                    internal partial class SQLitePersistentStorage
                    {
                        private abstract class Accessor<TKey, TDatabaseKey>
                            where TDatabaseKey : struct
                        {
                            private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;

                            public Accessor()
                            {
                                _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                    insert or replace into {0}.{1}
                                    ({2})
                                    """;

                                return;

                                string GetSelectRowIdQuery(string database) =>
                                    $"""
                                    select rowid from {0}.{1} where
                                    {2}
                                    limit 1
                                    """;
                            }
                        }
                    }
                }
                """",
            FixedCode = """"
                namespace $$Microsoft.CodeAnalysis.SQLite.v2;

                internal partial class SQLitePersistentStorage
                {
                    private abstract class Accessor<TKey, TDatabaseKey>
                        where TDatabaseKey : struct
                    {
                        private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;
                
                        public Accessor()
                        {
                            _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                insert or replace into {0}.{1}
                                ({2})
                                """;
                
                            return;
                
                            string GetSelectRowIdQuery(string database) =>
                                $"""
                                select rowid from {0}.{1} where
                                {2}
                                limit 1
                                """;
                        }
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestInterpolatedRawString4()
        => new VerifyCS.Test
        {
            TestCode = """"
                [|namespace Microsoft.CodeAnalysis.SQLite.v2|]
                {
                    internal partial class SQLitePersistentStorage
                    {
                        private abstract class Accessor<TKey, TDatabaseKey>
                            where TDatabaseKey : struct
                        {
                            private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;

                            public Accessor()
                            {
                                _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""

                                    insert or replace into {0}.{1}

                                    ({2})

                                    """;

                                return;

                                string GetSelectRowIdQuery(string database)
                                    => $"""

                                        select rowid from {0}.{1} where

                                        {2}

                                        limit 1

                                        """;
                            }
                        }
                    }
                }
                """",
            FixedCode = """"
                namespace $$Microsoft.CodeAnalysis.SQLite.v2;

                internal partial class SQLitePersistentStorage
                {
                    private abstract class Accessor<TKey, TDatabaseKey>
                        where TDatabaseKey : struct
                    {
                        private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;
                
                        public Accessor()
                        {
                            _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""

                                insert or replace into {0}.{1}

                                ({2})

                                """;
                
                            return;
                
                            string GetSelectRowIdQuery(string database)
                                => $"""

                                    select rowid from {0}.{1} where

                                    {2}

                                    limit 1

                                    """;
                        }
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestInterpolatedRawString5()
        => new VerifyCS.Test
        {
            TestCode = """"
                [|namespace Microsoft.CodeAnalysis.SQLite.v2|]
                {
                    internal partial class SQLitePersistentStorage
                    {
                        private abstract class Accessor<TKey, TDatabaseKey>
                            where TDatabaseKey : struct
                        {
                            private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;

                            public Accessor()
                            {
                                _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                        insert or replace into {0}.{1}
                                        ({2})
                                    """;

                                return;

                                string GetSelectRowIdQuery(string database)
                                    => $"""
                                            select rowid from {0}.{1} where
                                            {2}
                                            limit 1
                                        """;
                            }
                        }
                    }
                }
                """",
            FixedCode = """"
                namespace $$Microsoft.CodeAnalysis.SQLite.v2;

                internal partial class SQLitePersistentStorage
                {
                    private abstract class Accessor<TKey, TDatabaseKey>
                        where TDatabaseKey : struct
                    {
                        private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;
                
                        public Accessor()
                        {
                            _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                                    insert or replace into {0}.{1}
                                    ({2})
                                """;
                
                            return;
                
                            string GetSelectRowIdQuery(string database)
                                => $"""
                                        select rowid from {0}.{1} where
                                        {2}
                                        limit 1
                                    """;
                        }
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestInterpolatedRawString6()
        => new VerifyCS.Test
        {
            TestCode = """"
                [|namespace Microsoft.CodeAnalysis.SQLite.v2|]
                {
                    internal partial class SQLitePersistentStorage
                    {
                        private abstract class Accessor<TKey, TDatabaseKey>
                            where TDatabaseKey : struct
                        {
                            private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;

                            public Accessor()
                            {
                                _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                    insert or replace into {0}.{1}
                    ({2})
                """;

                                return;

                                string GetSelectRowIdQuery(string database)
                                    => $"""
                    select rowid from {0}.{1} where
                    {2}
                    limit 1
                """;
                            }
                        }
                    }
                }
                """",
            FixedCode = """"
                namespace $$Microsoft.CodeAnalysis.SQLite.v2;

                internal partial class SQLitePersistentStorage
                {
                    private abstract class Accessor<TKey, TDatabaseKey>
                        where TDatabaseKey : struct
                    {
                        private string _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data;
                
                        public Accessor()
                        {
                            _insert_or_replace_into_writecache_table_values_0primarykey_1checksum_2data = $"""
                    insert or replace into {0}.{1}
                    ({2})
                """;
                
                            return;
                
                            string GetSelectRowIdQuery(string database)
                                => $"""
                    select rowid from {0}.{1} where
                    {2}
                    limit 1
                """;
                        }
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74214")]
    public Task TestNotWithClassAfter()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                class Inner { }
            }

            class Outer { }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74214")]
    public Task TestNotWithClassBefore()
        => new VerifyCS.Test
        {
            TestCode = """
            class Outer { }

            namespace N
            {
                class Inner { }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();
}
