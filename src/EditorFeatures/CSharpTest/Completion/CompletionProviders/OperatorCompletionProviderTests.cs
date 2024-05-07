// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class OperatorCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(UnnamedSymbolCompletionProvider);

        // The suggestion is e.g. "+". If the user actually types "+" the completion list is closed. Operators therefore do not support partially written items.
        protected override string? ItemPartiallyWritten(string? expectedItemOrNull) => "";

        public static IEnumerable<object[]> BinaryArithmeticAndLogicalOperators()
        {
            yield return new[] { "+" };
            yield return new[] { "&" };
            yield return new[] { "|" };
            yield return new[] { "/" };
            yield return new[] { "^" };
            yield return new[] { "%" };
            yield return new[] { "*" };
            yield return new[] { ">>" };
            yield return new[] { ">>>" };
            yield return new[] { "<<" };
            yield return new[] { "-" };
        }

        public static IEnumerable<object[]> BinaryEqualityAndRelationalOperators()
        {
            yield return new[] { "==" };
            yield return new[] { ">" };
            yield return new[] { ">=" };
            yield return new[] { "!=" };
            yield return new[] { "<" };
            yield return new[] { "<=" };
        }

        public static IEnumerable<object[]> PostfixOperators()
        {
            yield return new[] { "++" };
            yield return new[] { "--" };
        }

        public static IEnumerable<object[]> PrefixOperators()
        {
            yield return new[] { "!" };
            yield return new[] { "~" };
            yield return new[] { "-" };
            yield return new[] { "+" };
        }

        public static IEnumerable<object[]> BinaryOperators()
            => BinaryArithmeticAndLogicalOperators().Union(BinaryEqualityAndRelationalOperators());

        public static IEnumerable<object[]> UnaryOperators()
            => PostfixOperators().Union(PrefixOperators());

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIsNotOfferedAfterNumberLiteral()
        {
            // User may want to type a floating point literal.
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static C operator +(C a, C b) => default;
                }

                public class Program
                {
                    public static void Main()
                    {
                        1.$$
                    }
                }
                """, SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync("""
                public class C
                {
                    public static C operator +(C a, C b) => default;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$;
                    }
                }
                """, "+", inlineDescription: "x + y", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", true)]
        [InlineData("c.$$;", true)]
        [InlineData("c.a$$", true)]
        [InlineData("c.ab$$", true)]
        [InlineData("c.abc$$", true)]
        [InlineData("c.abcd$$", true)]
        [InlineData("c. a$$", true)]
        [InlineData("c.$$a", true)]
        [InlineData("c.$$ a", true)]
        [InlineData("c?.$$", true)]
        public async Task OperatorSuggestionOnPartiallyWrittenMember(string expression, bool isOffered)
        {
            var verifyAction = isOffered
                ? new Func<string, Task>(markup => VerifyItemExistsAsync(markup, "+", inlineDescription: "x + y"))
                : new Func<string, Task>(markup => VerifyNoItemsExistAsync(markup));
            await verifyAction(@$"
public class C
{{
    public static C operator +(C a, C b) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIsNotSuggestedOnStaticAccess()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static C operator +(C a, C b) => default;
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
        public async Task OperatorIsNotSuggestedInNameoOfContext()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static C operator +(C a, C b) => default;
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorsAreSortedByImporttanceAndGroupedByTopic()
        {
            var items = await GetCompletionItemsAsync("""
                public class C
                {
                    public static C operator +(C a, C b) => null;
                    public static C operator -(C a, C b) => null;
                    public static C operator *(C a, C b) => null;
                    public static C operator /(C a, C b) => null;
                    public static C operator %(C a, C b) => null;
                    public static bool operator ==(C a, C b) => true;
                    public static bool operator !=(C a, C b) => false;
                    public static bool operator <(C a, C b) => true;
                    public static bool operator >(C a, C b) => false;
                    public static bool operator <=(C a, C b) => true;
                    public static bool operator >=(C a, C b) => false;
                    public static C operator +(C a) => null;
                    public static C operator -(C a) => null;
                    public static C operator ++(C a) => null;
                    public static C operator --(C a) => null;
                    public static bool operator true(C w) => true;
                    public static bool operator false(C w) => false;
                    public static bool operator &(C a, C b) => true;
                    public static bool operator |(C a, C b) => true;
                    public static C operator !(C a) => null;
                    public static C operator ^(C a, C b) => null;
                    public static C operator <<(C a, int b) => null;
                    public static C operator >>(C a, int b) => null;
                    public static C operator >>>(C a, int b) => null;
                    public static C operator ~(C a) => null;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$;
                    }
                }
                """, SourceCodeKind.Regular);
            // true and false operators are not listed
            Assert.Collection(items,
                i => Assert.Equal("==", i.DisplayText),
                i => Assert.Equal("!=", i.DisplayText),
                i => Assert.Equal(">", i.DisplayText),
                i => Assert.Equal(">=", i.DisplayText),
                i => Assert.Equal("<", i.DisplayText),
                i => Assert.Equal("<=", i.DisplayText),
                i => Assert.Equal("!", i.DisplayText),
                i => Assert.Equal("+", i.DisplayText), // Addition a+b
                i => Assert.Equal("-", i.DisplayText), // Subtraction a-b
                i => Assert.Equal("*", i.DisplayText),
                i => Assert.Equal("/", i.DisplayText),
                i => Assert.Equal("%", i.DisplayText),
                i => Assert.Equal("++", i.DisplayText),
                i => Assert.Equal("--", i.DisplayText),
                i => Assert.Equal("+", i.DisplayText), // Unary plus +a
                i => Assert.Equal("-", i.DisplayText), // Unary minus -a
                i => Assert.Equal("&", i.DisplayText),
                i => Assert.Equal("|", i.DisplayText),
                i => Assert.Equal("^", i.DisplayText),
                i => Assert.Equal("<<", i.DisplayText),
                i => Assert.Equal(">>", i.DisplayText),
                i => Assert.Equal(">>>", i.DisplayText),
                i => Assert.Equal("~", i.DisplayText)
            );
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("bool", 0)]
        [InlineData("System.Boolean", 0)]
        [InlineData("char", 0)]
        [InlineData("System.Char", 0)]
        [InlineData("string", 0)]
        [InlineData("System.String", 0)]
        [InlineData("sbyte", 0)]
        [InlineData("System.SByte", 0)]
        [InlineData("byte", 0)]
        [InlineData("System.Byte", 0)]
        [InlineData("short", 0)]
        [InlineData("System.Int16", 0)]
        [InlineData("ushort", 0)]
        [InlineData("System.UInt16", 0)]
        [InlineData("int", 0)]
        [InlineData("System.Int32", 0)]
        [InlineData("uint", 0)]
        [InlineData("System.UInt32", 0)]
        [InlineData("long", 0)]
        [InlineData("System.Int64", 0)]
        [InlineData("ulong", 0)]
        [InlineData("System.UInt64", 0)]
        [InlineData("float", 0)]
        [InlineData("System.Single", 0)]
        [InlineData("double", 0)]
        [InlineData("System.Double", 0)]
        [InlineData("decimal", 0)]
        [InlineData("System.Decimal", 0)]
        [InlineData("nint", 0)]
        [InlineData("System.IntPtr", 0)]
        [InlineData("nuint", 0)]
        [InlineData("System.UIntPtr", 0)]
        [InlineData("System.DateTime", 8)]
        [InlineData("System.TimeSpan", 10)]
        [InlineData("System.DateTimeOffset", 8)]
        [InlineData("System.Guid", 2)]
        public async Task OperatorSuggestionForSpecialTypes(string specialType, int numberOfSuggestions)
        {
            var completionItems = await GetCompletionItemsAsync(@$"
public class Program
{{
    public static void Main()
    {{
        {specialType} i = default({specialType});
        i.$$
    }}
}}
", SourceCodeKind.Regular);
            Assert.Equal(
                numberOfSuggestions,
                completionItems.Count(c => c.GetProperty(UnnamedSymbolCompletionProvider.KindName) == UnnamedSymbolCompletionProvider.OperatorKindName));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorNoSuggestionForTrueAndFalse()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static bool operator true(C _) => true;
                    public static bool operator false(C _) => true;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(BinaryOperators))]
        public async Task OperatorBinaryIsCompleted(string binaryOperator)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator {binaryOperator}(C a, C b) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", binaryOperator, @$"
public class C
{{
    public static C operator {binaryOperator}(C a, C b) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c {binaryOperator} $$
    }}
}}
");
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(PostfixOperators))]
        public async Task OperatorPostfixIsCompleted(string postfixOperator)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator {postfixOperator}(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", postfixOperator, @$"
public class C
{{
    public static C operator {postfixOperator}(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c{postfixOperator} $$
    }}
}}
");
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(PrefixOperators))]
        public async Task OperatorPrefixIsCompleted(string prefixOperator)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator {prefixOperator}(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", prefixOperator, @$"
public class C
{{
    public static C operator {prefixOperator}(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {prefixOperator}c$$
    }}
}}
");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorDuplicateOperatorsAreListedBoth()
        {
            var items = await GetCompletionItemsAsync($@"
public class C
{{
    public static C operator +(C a, C b) => default;
    public static C operator +(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i =>
                {
                    Assert.Equal("+", i.DisplayText);
                    Assert.EndsWith("002_007", i.SortText); // Addition
                },
                i =>
                {
                    Assert.Equal("+", i.DisplayText);
                    Assert.EndsWith("002_014", i.SortText); // unary plus
                });
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorDuplicateOperatorsAreCompleted()
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator +(C a, C b) => default;
    public static C operator +(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c.$$
    }}
}}
", "+", @$"
public class C
{{
    public static C operator +(C a, C b) => default;
    public static C operator +(C _) => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        c + $$
    }}
}}
");
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$",
                    "c + $$")]
        [InlineData("c. $$",
                    "c + $$ ")]
        [InlineData("c .$$",
                    "c  + $$")]
        [InlineData("c.$$;",
                    "c + $$;")]
        [InlineData("c.abc$$",
                    "c + $$")]
        [InlineData("c.a$$bc",
                    "c + $$")]
        [InlineData("c.$$abc",
                    "c + $$abc")]
        [InlineData("c.$$ abc",
                    "c + $$ abc")]
        [InlineData("(true ? c : c).$$",
                    "(true ? c : c) + $$")]
        [InlineData("c?.$$",
                    "c + $$")]
        [InlineData("(true ? c : c)?.$$",
                    "(true ? c : c) + $$")]
        [InlineData("c? .$$",
                    "c + $$")]
        [InlineData("c ? .$$",
                    "c  + $$")]
        [InlineData("c?.CProp.$$",
                    "c?.CProp + $$")]
        [InlineData("c?.CProp?.$$",
                    "c?.CProp + $$")]
        [InlineData("c.CProp.CProp?.$$",
                    "c.CProp.CProp + $$")]
        [InlineData("c?.CProp.CProp.$$",
                    "c?.CProp.CProp + $$")]
        [InlineData("c[0].$$",
                    "c[0] + $$")]
        [InlineData("c[0]?.$$",
                    "c[0] + $$")]
        [InlineData("c?.CProp[0].$$",
                    "c?.CProp[0] + $$")]
        [InlineData("c.CProp[0].CProp?.$$",
                    "c.CProp[0].CProp + $$")]
        public async Task OperatorInfixOfferingsAndCompletions(string expression, string completion)
        {
            await VerifyCustomCommitProviderAsync($@"
public class C
{{
    public static C operator +(C a, C b) => default;
    public C CProp {{ get; }}
    public C this[int _] => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
", "+", @$"
public class C
{{
    public static C operator +(C a, C b) => default;
    public C CProp {{ get; }}
    public C this[int _] => default;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {completion}
    }}
}}
");
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(UnaryOperators))]
        public async Task OperatorLiftingUnary(string operatorSign)
        {
            const string template = """
                public struct S
                {{
                    {0} => default;
                }}

                public class Program
                {{
                    public static void Main()
                    {{
                        S? s = null;
                        s.$$
                    }}
                }}
                """;
            var inlineDescription = operatorSign.Length == 1
                ? $"{operatorSign}x"
                : $"x{operatorSign}";
            await VerifyItemExistsAsync(string.Format(template, $"public static S operator {operatorSign}(S _)"), operatorSign, inlineDescription: inlineDescription);
            await VerifyItemExistsAsync(string.Format(template, $"public static bool operator {operatorSign}(S _)"), operatorSign, inlineDescription: inlineDescription);
            await VerifyNoItemsExistAsync(string.Format(template, $"public static object operator {operatorSign}(S _)"));
            await VerifyNoItemsExistAsync(string.Format(template, $"public static S operator {operatorSign}(S a, S b, S c)"));
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(BinaryArithmeticAndLogicalOperators))]
        public async Task OperatorLiftingBinary(string operatorSign)
        {
            const string template = """
                public struct S
                {{
                    {0} => default;
                }}

                public class Program
                {{
                    public static void Main()
                    {{
                        S? s = null;
                        s.$$
                    }}
                }}
                """;
            var inlineDescription = $"x {operatorSign} y";
            await VerifyItemExistsAsync(string.Format(template, $"public static S operator {operatorSign}(S a, S b)"), operatorSign, inlineDescription: inlineDescription);
            await VerifyItemExistsAsync(string.Format(template, $"public static int operator {operatorSign}(S a, S b)"), operatorSign, inlineDescription: inlineDescription);
            await VerifyNoItemsExistAsync(string.Format(template, $"public static object operator {operatorSign}(S a, S b)"));
            await VerifyNoItemsExistAsync(string.Format(template, $"public static S operator {operatorSign}(S a, object b)"));
            await VerifyNoItemsExistAsync(string.Format(template, $"public static S operator {operatorSign}(S a, S b, S c)"));
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [MemberData(nameof(BinaryEqualityAndRelationalOperators))]
        public async Task OperatorLiftingEqualityRelational(string operatorSign)
        {
            const string template = """
                public struct S
                {{
                    {0} => default;
                }}

                public class Program
                {{
                    public static void Main()
                    {{
                        S? s = null;
                        s.$$
                    }}
                }}
                """;
            await VerifyItemExistsAsync(string.Format(template, $"public static bool operator {operatorSign}(S a, S b)"), operatorSign, inlineDescription: $"x {operatorSign} y");
            await VerifyNoItemsExistAsync(string.Format(template, $"public static int operator {operatorSign}(S a, S b)"));
            await VerifyNoItemsExistAsync(string.Format(template, $"public static bool operator {operatorSign}(S a, S b, S c)"));
            await VerifyNoItemsExistAsync(string.Format(template, $"public static bool operator {operatorSign}(S a, object b)"));
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorLiftingIsApplied()
        {
            await VerifyCustomCommitProviderAsync("""
                public struct S
                {
                    public static bool operator ==(S a, S b) => default;
                }

                public class Program
                {
                    public static void Main()
                    {
                        S? s = null;
                        s.$$
                    }
                }
                """, "==", """
                public struct S
                {
                    public static bool operator ==(S a, S b) => default;
                }

                public class Program
                {
                    public static void Main()
                    {
                        S? s = null;
                        s == $$
                    }
                }
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorOfBaseTypeIsSuggested()
        {
            await VerifyItemExistsAsync("""
                public class Base {
                    public static int operator +(Base b, int a)=>0;
                }
                public class Derived: Base
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
                """, "+", inlineDescription: "x + y", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorForRecordsAreSuggested()
        {
            await VerifyItemExistsAsync("""
                public record R {
                }

                public class Program
                {
                    public static void Main()
                    {
                        var r = new R();
                        r.$$
                    }
                }
                """, "==", inlineDescription: "x == y", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnOperatorIsRespected_EditorBrowsableStateNever()
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
                        public static C operator -(C a, C b) => default;
                    }
                }
                """;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "-",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnOperatorIsRespected_EditorBrowsableStateAdvanced()
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
                        public static C operator -(C a, C b) => default;
                    }
                }
                """;

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "-",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "-",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorBinaryNullForgivingHandling()
        {
            await VerifyCustomCommitProviderAsync("""
                #nullable enable

                public class C
                {
                    public static C operator +(C a, C b) => default;
                }

                public class Program
                {
                    public static void Main()
                    {
                        C? c = null;
                        var _ = c!.$$
                    }
                }
                """, "+", """
                #nullable enable

                public class C
                {
                    public static C operator +(C a, C b) => default;
                }

                public class Program
                {
                    public static void Main()
                    {
                        C? c = null;
                        var _ = c! + $$
                    }
                }
                """);
        }
    }
}
