// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class IndexerCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(UnnamedSymbolCompletionProvider);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync("""
                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, "this", displayTextSuffix: "[]", matchingFilters: [FilterSet.PropertyFilter]);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsSuggestedAfterDotForString()
        {
            await VerifyItemExistsAsync("""
                public class Program
                {
                    public static void Main(string s)
                    {
                        s.$$
                    }
                }
                """, "this", displayTextSuffix: "[]", matchingFilters: [FilterSet.PropertyFilter]);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsNotSuggestedOnStaticAccess()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        C.$$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsNotSuggestedInNameOfContext()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        var name = nameof(c.$$
                    }
                }
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerSuggestionCommitsOpenAndClosingBraces()
        {
            await VerifyCustomCommitProviderAsync("""
                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, "this", """
                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c[$$]
                    }
                }
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerWithTwoParametersSuggestionCommitsOpenAndClosingBraces()
        {
            await VerifyCustomCommitProviderAsync("""
                public class C
                {
                    public int this[int x, int y] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, "this", """
                public class C
                {
                    public int this[int x, int y] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c[$$]
                    }
                }
                """);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$",
                    "c[$$]")]
        [InlineData("c. $$",
                    "c[$$] ")]
        [InlineData("c.$$;",
                    "c[$$];")]
        [InlineData("c.th$$",
                    "c[$$]")]
        [InlineData("c.this$$",
                    "c[$$]")]
        [InlineData("c.th$$;",
                    "c[$$];")]
        [InlineData("var f = c.$$;",
                    "var f = c[$$];")]
        [InlineData("var f = c.th$$;",
                    "var f = c[$$];")]
        [InlineData("c?.$$",
                    "c?[$$]")]
        [InlineData("c?.this$$",
                    "c?[$$]")]
        [InlineData("((C)c).$$",
                    "((C)c)[$$]")]
        [InlineData("(true ? c : c).$$",
                    "(true ? c : c)[$$]")]
        public async Task IndexerCompletionForDifferentExpressions(string expression, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
", "this", @$"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {fixedCode}
    }}
}}
");
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("/* Leading trivia */c.$$",
                    "/* Leading trivia */c[$$]")]
        [InlineData("c. $$ /* Trailing trivia */",
                    "c[$$]  /* Trailing trivia */")]
        [InlineData("c./* Trivia in between */$$",
                    "c[$$]/* Trivia in between */")]
        public async Task IndexerCompletionTriviaTest(string expression, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
", "this", @$"
public class C
{{
    public int this[int i] => i;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {fixedCode}
    }}
}}
");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerDescriptionIncludesDocCommentsAndOverloadsHint()
        {
            await VerifyItemExistsAsync("""
                public class C
                {
                    /// <summary>
                    /// Returns the index <paramref name="i"/>
                    /// </summary>
                    /// <param name="i">The index</param>
                    /// <returns>Returns the index <paramref name="i"/></returns>
                    public int this[int i] => i;

                    /// <summary>
                    /// Returns 1
                    /// </summary>
                    /// <param name="i">The index</param>
                    /// <returns>Returns 1</returns>
                    public int this[string s] => 1;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, "this", displayTextSuffix: "[]", expectedDescriptionOrNull: @$"int C.this[int i] {{ get; }} (+ 1 {FeaturesResources.overload})
Returns the index i");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerOfBaseTypeIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync("""
                public class Base
                {
                    public int this[int i] => i;
                }
                public class Derived : Base
                {
                }

                public class Program
                {
                    public static void Main()
                    {
                        var d = new Derived();
                        d.$$
                    }
                }
                """, "this", displayTextSuffix: "[]");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerOfBaseTypeIsNotSuggestedIfNotAccessible()
        {
            await VerifyNoItemsExistAsync("""
                public class Base
                {
                    protected int this[int i] => i;
                }
                public class Derived : Base
                {
                }

                public class Program
                {
                    public static void Main()
                    {
                        var d = new Derived();
                        d.$$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerIsSuggestedOnString()
        {
            await VerifyItemExistsAsync("""
                public class Program
                {
                    public static void Main()
                    {
                        var s = "Test";
                        s.$$
                    }
                }
                """, "this", displayTextSuffix: "[]");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnIndexerIsRespected_EditorBrowsableStateNever()
        {
            var markup = """
                namespace N
                {
                    public class Program
                    {
                        public static void Main()
                        {
                            var c = new C();
                            c.$$
                        }
                    }
                }
                """;
            var referencedCode = """
                using System.ComponentModel;

                namespace N
                {
                    public class C
                    {
                        [EditorBrowsable(EditorBrowsableState.Never)]
                        public int this[int i] => i;
                    }
                }
                """;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "this",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnIndexerIsRespected_EditorBrowsableStateAdvanced()
        {
            var markup = """
                namespace N
                {
                    public class Program
                    {
                        public static void Main()
                        {
                            var c = new C();
                            c.$$
                        }
                    }
                }
                """;
            var referencedCode = """
                using System.ComponentModel;

                namespace N
                {
                    public class C
                    {
                        [EditorBrowsable(EditorBrowsableState.Advanced)]
                        public int this[int i] => i;
                    }
                }
                """;
            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "this",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "this",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnIndexerIsRespected_EditorBrowsableStateNever_InheritedMember()
        {
            var markup = """
                namespace N
                {
                    public class Program
                    {
                        public static void Main()
                        {
                            var d = new Derived();
                            d.$$
                        }
                    }
                }
                """;
            var referencedCode = """
                using System.ComponentModel;

                namespace N
                {
                    public class Base
                    {
                        [EditorBrowsable(EditorBrowsableState.Never)]
                        public int this[int i] => i;
                    }

                    public class Derived: Base
                    {
                    }
                }
                """;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "this",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task IndexerNullForgivingOperatorHandling()
        {
            await VerifyCustomCommitProviderAsync("""
                #nullable enable

                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        C? c = null;
                        var i = c!.$$
                    }
                }
                """, "this", """
                #nullable enable

                public class C
                {
                    public int this[int i] => i;
                }

                public class Program
                {
                    public static void Main()
                    {
                        C? c = null;
                        var i = c![$$]
                    }
                }
                """);
        }
    }
}
