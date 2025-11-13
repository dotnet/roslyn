// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting;

using Verify = CSharpCodeFixVerifier<CodeStyle.CSharpFormattingAnalyzer, CodeStyle.CSharpFormattingCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingAnalyzerTests
{
    [Fact]
    public async Task TrailingWhitespace()
    {
        var testCode =
            "class X[| |]" + Environment.NewLine +
            "{" + Environment.NewLine +
            "}" + Environment.NewLine;
        var expected =
            "class X" + Environment.NewLine +
            "{" + Environment.NewLine +
            "}" + Environment.NewLine;
        await Verify.VerifyCodeFixAsync(testCode, expected);
    }

    [Fact]
    public Task TestMissingSpace()
        => Verify.VerifyCodeFixAsync("""
            class TypeName
            {
                void Method()
                {
                    if[||](true)[||]return;
                }
            }
            """, """
            class TypeName
            {
                void Method()
                {
                    if (true) return;
                }
            }
            """);

    [Fact]
    public Task TestAlreadyFormatted()
        => Verify.VerifyAnalyzerAsync("""
            class MyClass
            {
                void MyMethod()
                {
                }
            }
            """);

    [Fact]
    public Task TestNeedsIndentation()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
              $$void MyMethod()
              $${
              $$}
            }
            """, """
            class MyClass
            {
                void MyMethod()
                {
                }
            }
            """);

    [Fact]
    public Task TestNeedsIndentationButSuppressed()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
              $$void MyMethod1()
              $${
              $$}

            #pragma warning disable format
            		void MyMethod2()
              {
              }
            #pragma warning restore format

              void MyMethod3()
              $${
              $$}
            }
            """, """
            class MyClass
            {
                void MyMethod1()
                {
                }

            #pragma warning disable format
            		void MyMethod2()
              {
              }
            #pragma warning restore format

              void MyMethod3()
                {
                }
            }
            """);

    [Fact]
    public Task TestWhitespaceBetweenMethods1()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
                void MyMethod1()
                {
                }
            [| |]
                void MyMethod2()
                {
                }
            }
            """, """
            class MyClass
            {
                void MyMethod1()
                {
                }

                void MyMethod2()
                {
                }
            }
            """);

    [Fact]
    public Task TestWhitespaceBetweenMethods2()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
                void MyMethod1()
                {
                }[| |]

                void MyMethod2()
                {
                }
            }
            """, """
            class MyClass
            {
                void MyMethod1()
                {
                }

                void MyMethod2()
                {
                }
            }
            """);

    [Fact]
    public Task TestWhitespaceBetweenMethods3()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
                void MyMethod1()
                {
                }[| 
                
                |]void MyMethod2()
                {
                }
            }
            """, """
            class MyClass
            {
                void MyMethod1()
                {
                }

                void MyMethod2()
                {
                }
            }
            """);

    [Fact]
    public Task TestOverIndentation()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
                [|    |]void MyMethod()
                [|    |]{
                [|    |]}
            }
            """, """
            class MyClass
            {
                void MyMethod()
                {
                }
            }
            """);

    [Fact]
    public Task TestIncrementalFixesFullLine()
        => new Verify.Test
        {
            TestCode = """
            class MyClass
            {
                int Property1$${$$get;$$set;$$}
                int Property2$${$$get;$$}
            }
            """,
            FixedCode = """
            class MyClass
            {
                int Property1 { get; set; }
                int Property2 { get; }
            }
            """,

            // Each application of a single code fix covers all diagnostics on the same line. In total, two lines
            // require changes so the number of incremental iterations is exactly 2.
            NumberOfIncrementalIterations = 2,
        }.RunAsync();

    [Fact]
    public async Task TestEditorConfigUsed()
    {
        var testCode = """
            class MyClass {
                void MyMethod()[| |]{
                }
            }
            """;
        var fixedCode = """
            class MyClass {
                void MyMethod()
                {
                }
            }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", """
                    root = true
                    [*.cs]
                    csharp_new_line_before_open_brace = methods
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }

    [Fact]
    public async Task TestRegion()
    {
        var testCode = """
            class MyClass
            {
            #if true
                public void M()
                {
                    #region ABC1
                    System.Console.WriteLine();
                    #endregion
                }
            #else
                public void M()
                {
                    #region ABC2
                    System.Console.WriteLine();
                    #endregion
                }
            #endif
            }
            """;
        await Verify.VerifyCodeFixAsync(testCode, testCode);
    }

    [Fact]
    public Task TestRegion2()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
            #if true
                public void M()
                {
            [||]#region OUTER1
                    #region ABC1
                    System.Console.WriteLine();
                    #endregion
            [||]#endregion
                }
            #else
                public void M()
                {
            #region OUTER2
                    #region ABC2
                    System.Console.WriteLine();
                    #endregion
            #endregion
                }
            #endif
            }
            """, """
            class MyClass
            {
            #if true
                public void M()
                {
                    #region OUTER1
                    #region ABC1
                    System.Console.WriteLine();
                    #endregion
                    #endregion
                }
            #else
                public void M()
                {
            #region OUTER2
                    #region ABC2
                    System.Console.WriteLine();
                    #endregion
            #endregion
                }
            #endif
            }
            """);

    [Fact]
    public Task TestRegion3()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
            #if true
                public void M()
                {
            [||]#region ABC1
                    System.Console.WriteLine();
            [||]#endregion
                }
            #else
                public void M()
                {
            #region ABC2
                    System.Console.WriteLine();
            #endregion
                }
            #endif
            }
            """, """
            class MyClass
            {
            #if true
                public void M()
                {
                    #region ABC1
                    System.Console.WriteLine();
                    #endregion
                }
            #else
                public void M()
                {
            #region ABC2
                    System.Console.WriteLine();
            #endregion
                }
            #endif
            }
            """);

    [Fact]
    public Task TestRegion4()
        => Verify.VerifyCodeFixAsync("""
            class MyClass
            {
            #if true
                public void M()
                {
            [||]#region ABC1
                    System.Console.WriteLine();
            [||]#endregion
                }
            #else
                                    #region ABC2
                    public void M() { }
                                    #endregion
            #endif
            }
            """, """
            class MyClass
            {
            #if true
                public void M()
                {
                    #region ABC1
                    System.Console.WriteLine();
                    #endregion
                }
            #else
                                    #region ABC2
                    public void M() { }
                                    #endregion
            #endif
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithGroupedButNotGloballySortedUsings(bool sortSystemDirectivesFirst)
    {
        // Test that usings that are grouped correctly (each group sorted) but not globally sorted
        // still trigger the separator when dotnet_separate_import_directive_groups is true
        // This should work the same regardless of dotnet_sort_system_directives_first setting
        var testCode = """
            namespace TestNamespace;

            using Azure.Storage.Blobs;
            using Azure.Storage.Sas;[||]
            using System.Diagnostics;
            using NuGet.Versioning;

            class TestClass { }
            """;
        var fixedCode = """
            namespace TestNamespace;

            using Azure.Storage.Blobs;
            using Azure.Storage.Sas;

            using System.Diagnostics;
            using NuGet.Versioning;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithAlphabeticallySortedUsings(bool sortSystemDirectivesFirst)
    {
        // Test that usings that are alphabetically sorted trigger the separator
        // This should work the same regardless of dotnet_sort_system_directives_first setting
        var testCode = """
            namespace TestNamespace;

            using Azure.Storage.Blobs;
            using Azure.Storage.Sas;[||]
            using NuGet.Versioning;
            using System.Diagnostics;

            class TestClass { }
            """;
        var fixedCode = """
            namespace TestNamespace;

            using Azure.Storage.Blobs;
            using Azure.Storage.Sas;

            using NuGet.Versioning;
            using System.Diagnostics;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithUnsortedButGroupedUsings(bool sortSystemDirectivesFirst)
    {
        // Test that usings are properly grouped even if not sorted within groups
        // Azure group is not sorted (Sas before Blobs), but still properly grouped
        // This should work the same regardless of dotnet_sort_system_directives_first setting
        var testCode = """
            namespace TestNamespace;

            using Azure.Storage.Sas;
            using Azure.Storage.Blobs;[||]
            using System.Diagnostics;[||]
            using NuGet.Versioning;

            class TestClass { }
            """;
        var fixedCode = """
            namespace TestNamespace;

            using Azure.Storage.Sas;
            using Azure.Storage.Blobs;

            using System.Diagnostics;

            using NuGet.Versioning;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithUngroupedUsingsNoSeparator(bool sortSystemDirectivesFirst)
    {
        // Test that usings that are not grouped don't trigger separator
        // Azure usings at start and end means they're not properly grouped
        // This should work the same regardless of dotnet_sort_system_directives_first setting
        var testCode = """
            namespace TestNamespace;

            using Azure.Storage.Blobs;
            using System.Diagnostics;
            using NuGet.Versioning;
            using Azure.Storage.Sas;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { testCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithGroupedAliases(bool sortSystemDirectivesFirst)
    {
        // Test that aliases are properly grouped
        var testCode = """
            namespace TestNamespace;

            using A = System.String;
            using B = System.Int32;[||]
            using System.Collections.Generic;

            class TestClass { }
            """;
        var fixedCode = """
            namespace TestNamespace;

            using A = System.String;
            using B = System.Int32;

            using System.Collections.Generic;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithUngroupedAliases(bool sortSystemDirectivesFirst)
    {
        // Test that ungrouped aliases (aliases appearing in multiple places) don't trigger separators
        var testCode = """
            namespace TestNamespace;

            using A = System.String;
            using System.Collections.Generic;
            using B = System.Int32;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { testCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithGroupedStaticUsings(bool sortSystemDirectivesFirst)
    {
        // Test that static usings are properly grouped
        var testCode = """
            namespace TestNamespace;

            using static System.Math;
            using static System.Console;[||]
            using System.Collections.Generic;

            class TestClass { }
            """;
        var fixedCode = """
            namespace TestNamespace;

            using static System.Math;
            using static System.Console;

            using System.Collections.Generic;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithUngroupedStaticUsings(bool sortSystemDirectivesFirst)
    {
        // Test that ungrouped static usings don't trigger separators
        var testCode = """
            namespace TestNamespace;

            using static System.Math;
            using System.Collections.Generic;
            using static System.Console;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { testCode } },
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77831")]
    public async Task TestSeparateImportDirectiveGroups_WithMixedAliasesStaticsAndNamespaces(bool sortSystemDirectivesFirst)
    {
        // Test combinations of aliases, static usings, and regular namespaces
        var testCode = """
            namespace TestNamespace;

            using A = System.String;
            using B = System.Int32;[||]
            using static System.Math;
            using static System.Console;[||]
            using System.Collections.Generic;
            using System.Linq;

            class TestClass { }
            """;
        var fixedCode = """
            namespace TestNamespace;

            using A = System.String;
            using B = System.Int32;

            using static System.Math;
            using static System.Console;

            using System.Collections.Generic;
            using System.Linq;

            class TestClass { }
            """;
        await new Verify.Test
        {
            TestState =
            {
                Sources = { testCode },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                    root = true
                    [*.cs]
                    dotnet_separate_import_directive_groups = true
                    dotnet_sort_system_directives_first = {{sortSystemDirectivesFirst}}
                    """),
                },
            },
            FixedState = { Sources = { fixedCode } },
        }.RunAsync();
    }
}
