// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpConsoleSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "cw";

        [WpfFact]
        public async Task InsertConsoleSnippetInMethodTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        Wr$$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public void Method()
                    {
                        Console.WriteLine($$);
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertNormalConsoleSnippetInAsyncContextTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public async Task MethodAsync()
                    {
                        Wr$$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public async Task MethodAsync()
                    {
                        Console.WriteLine($$);
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetGlobalTest()
        {
            var markupBeforeCommit =
                """
                $$
                class Program
                {
                    public async Task MethodAsync()
                    {
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                Console.WriteLine($$);
                class Program
                {
                    public async Task MethodAsync()
                    {
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInBlockNamespaceTest()
        {
            var markupBeforeCommit =
                """
                namespace Namespace
                {
                    $$
                    class Program
                    {
                        public async Task MethodAsync()
                        {
                        }
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
                """
                namespace Namespace;
                $$
                class Program
                {
                    public async Task MethodAsync()
                    {
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetInConstructorTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public Program()
                    {
                        var x = 5;
                        $$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public Program()
                    {
                        var x = 5;
                        Console.WriteLine($$);
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var x = 5;
                        void LocalMethod()
                        {
                            $$
                        }
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public void Method()
                    {
                        var x = 5;
                        void LocalMethod()
                        {
                            Console.WriteLine($$);
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit =
                """
                public delegate void Print(int value);

                static void Main(string[] args)
                {
                    Print print = delegate(int val) {
                        $$
                    };

                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                public delegate void Print(int value);

                static void Main(string[] args)
                {
                    Print print = delegate(int val) {
                        Console.WriteLine($$);
                    };

                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetInParenthesizedLambdaExpressionTest()
        {
            var markupBeforeCommit =
                """
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    $$
                    return x == y;
                };
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    Console.WriteLine($$);
                    return x == y;
                };
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInSwitchExpression()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                       var operation = 2;  

                        var result = operation switch  
                        {
                            $$
                            1 => "Case 1",  
                            2 => "Case 2",  
                            3 => "Case 3",  
                            4 => "Case 4",  
                        };
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInSingleLambdaExpression()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                       Func<int, int> f = x => $$;
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInStringTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var str = "$$";
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInObjectInitializerTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var str = new Test($$);
                    }
                }

                class Test
                {
                    private string val;

                    public Test(string val)
                    {
                        this.val = val;
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInParameterListTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method(int x, $$)
                    {
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInRecordDeclarationTest()
        {
            var markupBeforeCommit =
                """
                public record Person
                {
                    $$
                    public string FirstName { get; init; } = default!;
                    public string LastName { get; init; } = default!;
                };
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoConsoleSnippetInVariableDeclarationTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var x = $$
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetWithInvocationBeforeAndAfterCursorTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        Wr$$Blah
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public void Method()
                    {
                        Console.WriteLine($$);
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertConsoleSnippetWithInvocationUnderscoreBeforeAndAfterCursorTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        _Wr$$Blah_
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public void Method()
                    {
                        Console.WriteLine($$);
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        /// <summary>
        /// We want to fix this case and insert the fully qualified namespace
        /// in a future fix.
        /// </summary>
        [WpfFact]
        public async Task InsertConsoleSnippetWithPropertyNamedConsoleTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public int Console { get; set; }

                    public void Method()
                    {
                        $$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;

                class Program
                {
                    public int Console { get; set; }

                    public void Method()
                    {
                        Console.WriteLine($$);
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
