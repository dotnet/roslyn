// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8333")]
    public async Task TestNotInExpressionBody()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                void Goo(int a, int b) => [||]0;
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1905")]
    public async Task TestAfterSemicolonForInvocationInExpressionStatement_ViaCommand()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    M1(1, 2);$$
                    M2(1, 2, 3);
                }

                static void M1(int x, int y) { }

                static void M2(int x, int y, int z) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp,
            markup: markup,
            updatedSignature: [1, 0],
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                static void Main(string[] args)
                {
                    M1(2, 1);
                    M2(1, 2, 3);
                }

                static void M1(int y, int x) { }

                static void M2(int x, int y, int z) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75676")]
    public async Task TestForPrimaryConstructor_ViaCommand()
    {
        var markup = """
            public class Base {
                public $$Base(string Item2, string Item1)
                {
                }
            }

            public class Derived() : Base("Item2", "Item1")
            {
            }
            """;
        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp,
            markup: markup,
            updatedSignature: [1, 0],
            expectedUpdatedInvocationDocumentCode: """
            public class Base {
                public Base(string Item1, string Item2)
                {
                }
            }
            
            public class Derived() : Base("Item1", "Item2")
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75676")]
    public async Task TestForPrimaryConstructorParamsArray_ViaCommand()
    {
        var markup = """
        public class Base {
            public $$Base(string Item2, string Item1, params object[] items)
            {
                Console.WriteLine(items.Length);
            }
        }

        public class Derived() : Base("Item2", "Item1", 1, "test", true)
        {
        }
        """;
        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp,
            markup: markup,
            updatedSignature: [1, 0, 2],
            expectedUpdatedInvocationDocumentCode: """
        public class Base {
            public Base(string Item1, string Item2, params object[] items)
            {
                Console.WriteLine(items.Length);
            }
        }
        
        public class Derived() : Base("Item1", "Item2", 1, "test", true)
        {
        }
        """);
    }

    [Fact]
    public async Task TestOnLambdaWithTwoDiscardParameters_ViaCommand()
    {
        var markup = """
            class Program
            {
                static void M()
                {
                    System.Func<int, string, int> f = $$(int _, string _) => 1;
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp,
            markup: markup,
            updatedSignature: [1, 0],
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                static void M()
                {
                    System.Func<int, string, int> f = (string _, int _) => 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnAnonymousMethodWithTwoParameters_ViaCommand()
    {
        await TestMissingAsync("""
            class Program
            {
                static void M()
                {
                    System.Func<int, string, int> f = [||]delegate(int x, string y) { return 1; };
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnAnonymousMethodWithTwoDiscardParameters_ViaCommand()
    {
        await TestMissingAsync("""
            class Program
            {
                static void M()
                {
                    System.Func<int, string, int> f = [||]delegate(int _, string _) { return 1; };
                }
            }
            """);
    }

    [Fact]
    public async Task TestAfterSemicolonForInvocationInExpressionStatement_ViaCodeAction()
    {
        await TestMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    M1(1, 2);[||]
                    M2(1, 2, 3);
                }

                static void M1(int x, int y) { }

                static void M2(int x, int y, int z) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingWhitespace()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                [||]
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingTrivia()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                // [||]
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingTrivia2()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                [||]//
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingDocComment()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                /// [||]
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingDocComment2()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                [||]///
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingAttributes1()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                [||][X]
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingAttributes2()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                [[||]X]
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInLeadingAttributes3()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                [X][||]
                void Goo(int a, int b)
                {
                };
            }
            """, expectedCodeAction: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17309")]
    public async Task TestNotInConstraints()
    {
        await TestChangeSignatureViaCodeActionAsync("""
            class Ext
            {
                void Goo<T>(int a, int b) where [||]T : class
                {
                };
            }
            """, expectedCodeAction: false);
    }
}
