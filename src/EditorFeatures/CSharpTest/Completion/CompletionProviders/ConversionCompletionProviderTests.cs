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
using CompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class ConversionCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(UnnamedSymbolCompletionProvider);

        private static string FormatExplicitConversionDescription(string fromType, string toType)
            => string.Format(WorkspacesResources.Predefined_conversion_from_0_to_1, fromType, toType);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIndexerCompletionItemsShouldBePlacedLastInCompletionList()
        {
            var castCompletionItem = (await GetCompletionItemsAsync("""
                public class C
                {
                    public static explicit operator float(C c) => 0;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, SourceCodeKind.Regular)).Single();

            var completionList = new[] {
                CompletionItem.Create("SomeText0"),
                castCompletionItem,
                CompletionItem.Create("SomeText1"),
                CompletionItem.Create("\uffdcStartWith_FFDC_Identifier"), // http://www.fileformat.info/info/unicode/char/ffdc/index.htm
                CompletionItem.Create("SomeText2"),
                CompletionItem.Create("\uD884\uDF4AStartWith_3134A_Identifier"), // http://www.fileformat.info/info/unicode/char/3134a/index.htm
                CompletionItem.Create("SomeText3"),
            };
            Array.Sort(completionList);
            Assert.Collection(completionList,
                c => Assert.Equal("SomeText0", c.DisplayText),
                c => Assert.Equal("SomeText1", c.DisplayText),
                c => Assert.Equal("SomeText2", c.DisplayText),
                c => Assert.Equal("SomeText3", c.DisplayText),
                c => Assert.Equal("\uD884\uDF4AStartWith_3134A_Identifier", c.DisplayText),
                c => Assert.Equal("\uffdcStartWith_FFDC_Identifier", c.DisplayText),
                c =>
                {
                    Assert.Same(c, castCompletionItem);
                    Assert.Equal("float", c.DisplayText);
                    Assert.Equal("\uFFFD001_float", c.SortText);
                    Assert.Equal("float", c.FilterText);
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsNotOfferedAfterNumberLiteral()
        {
            // User may want to type a floating point literal.
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static explicit operator float(C c) => 0;
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
        public async Task ExplicitUserDefinedConversionIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync("""
                public class C
                {
                    public static explicit operator float(C c) => 0;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, "float", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: [FilterSet.OperatorFilter]);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedIfMemberNameIsPartiallyWritten()
        {
            await VerifyItemExistsAsync("""
                public class C
                {
                    public void fly() { }
                    public static explicit operator float(C c) => 0;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.fl$$
                    }
                }
                """, "float", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: [FilterSet.OperatorFilter]);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", true)]
        [InlineData("c.fl$$", true)]
        [InlineData("c.  $$", true)]
        [InlineData("c.fl  $$", false)]
        [InlineData("c.($$", false)]
        [InlineData("c$$", false)]
        [InlineData("""
            "c.$$
            """, false)]
        [InlineData("c?.$$", true)]
        [InlineData("((C)c).$$", true)]
        [InlineData("(true ? c : c).$$", true)]
        [InlineData("c.$$ var x=0;", true)]
        public async Task ExplicitUserDefinedConversionDifferentExpressions(string expression, bool shouldSuggestConversion)
        {
            Func<string, string, Task> verifyFunc = shouldSuggestConversion
                ? (markup, expectedItem) => VerifyItemExistsAsync(markup, expectedItem, displayTextPrefix: "(", displayTextSuffix: ")")
                : (markup, expectedItem) => VerifyItemIsAbsentAsync(markup, expectedItem, displayTextPrefix: "(", displayTextSuffix: ")");

            await verifyFunc(@$"
public class C
{{
    public static explicit operator float(C c) => 0;
}}

public class Program
{{
    public static void Main()
    {{
        var c = new C();
        {expression}
    }}
}}
", "float");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsNotSuggestedOnStaticAccess()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static explicit operator float(C c) => 0;
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
        public async Task ExplicitUserDefinedConversionIsNotSuggestedInNameofContext()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static explicit operator float(C c) => 0;
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

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("", "Nested1.C", "Nested2.C")]
        [InlineData("using N1.Nested1;", "C", "Nested2.C")]
        [InlineData("using N1.Nested2;", "C", "Nested1.C")]
        [InlineData("using N1.Nested1;using N1.Nested2;", "Nested1.C", "Nested2.C")]
        public async Task ExplicitUserDefinedConversionTypeDisplayStringIsMinimal(string usingDirective, string displayText1, string displayText2)
        {
            var items = await GetCompletionItemsAsync(@$"
namespace N1.Nested1
{{
    public class C
    {{
    }}
}}

namespace N1.Nested2
{{
    public class C
    {{
    }}
}}
namespace N2
{{
    public class Conversion
    {{
        public static explicit operator N1.Nested1.C(Conversion _) => new N1.Nested1.C();
        public static explicit operator N1.Nested2.C(Conversion _) => new N1.Nested2.C();
    }}
}}
namespace N1
{{
    {usingDirective}
    public class Test
    {{
        public void M()
        {{
            var conversion = new N2.Conversion();
            conversion.$$
        }}
    }}
}}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i => Assert.Equal(displayText1, i.DisplayText),
                i => Assert.Equal(displayText2, i.DisplayText));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedForAllExplicitConversionsToOtherTypesAndNotForImplicitConversions()
        {
            var items = await GetCompletionItemsAsync("""
                public class C
                {
                    public static explicit operator float(C c) => 0;
                    public static explicit operator int(C c) => 0;

                    public static explicit operator C(float f) => new C();
                    public static implicit operator C(string s) => new C();
                    public static implicit operator string(C c) => ";
                }

                public class Program
                {
                    public static void Main()
                    {
                        var c = new C();
                        c.$$
                    }
                }
                """, SourceCodeKind.Regular);
            Assert.Collection(items,
                i => Assert.Equal("float", i.DisplayText),
                i => Assert.Equal("int", i.DisplayText));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIgnoresConversionLikeMethods()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static bool op_Explicit(C c) => false;
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIgnoreMalformedOperators()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static explicit operator int() => 0;
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionFromOtherTypeToTargetIsNotSuggested()
        {
            await VerifyNoItemsExistAsync("""
                public class C
                {
                    public static explicit operator C(D d) => null;
                }
                public class D
                {
                }

                public class Program
                {
                    public static void Main()
                    {
                        var d = new D();
                        d.$$
                    }
                }
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitConversionOfNullableStructToNullableStructIsOffered()
        {
            // Lifted conversion https://docs.microsoft.com/hu-hu/dotnet/csharp/language-reference/language-specification/conversions#lifted-conversion-operators
            await VerifyItemExistsAsync("""
                public struct S {
                    public static explicit operator int(S _) => 0;
                }
                public class Program
                {
                    public static void Main()
                    {
                        S? s = null;
                        s.$$
                    }
                }
                """, "int?", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: [FilterSet.OperatorFilter]);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitConversionDescriptionIsIsGiven()
        {
            const string Markup = """
                public struct S {
                        /// <summary>
                        /// Explicit conversion of <see cref="S"/> to <see cref="int"/>.
                        /// </summary>
                        /// <param name="value">The <see cref="S"/> to convert</param>
                        public static explicit operator int(S value) => 0;
                }

                public class Program
                {
                    public static void Main()
                    {
                        var s = new S();
                        s.$$
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "int", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
                """
                S.explicit operator int(S value)
                Explicit conversion of S to int.
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitConversionDescriptionIsIsGivenLifted()
        {
            const string Markup = """
                public struct S {
                        /// <summary>
                        /// Explicit conversion of <see cref="S"/> to <see cref="int"/>.
                        /// </summary>
                        /// <param name="value">The <see cref="S"/> to convert</param>
                        public static explicit operator int(S value) => 0;
                }

                public class Program
                {
                    public static void Main()
                    {
                        S? s = new S();
                        s.$$
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "int?", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
                """
                S.explicit operator int?(S? value)
                Explicit conversion of S to int.
                """);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("sbyte", "byte", "char", "uint", "ulong", "ushort")]
        [InlineData("byte", "char", "sbyte")]
        [InlineData("short", "byte", "char", "sbyte", "uint", "ulong", "ushort")]
        [InlineData("ushort", "byte", "char", "sbyte", "short")]
        [InlineData("int", "byte", "char", "sbyte", "short", "uint", "ulong", "ushort")]
        [InlineData("uint", "byte", "char", "int", "sbyte", "short", "ushort")]
        [InlineData("long", "byte", "char", "int", "sbyte", "short", "uint", "ulong", "ushort")]
        [InlineData("ulong", "byte", "char", "int", "long", "sbyte", "short", "uint", "ushort")]
        [InlineData("char", "byte", "sbyte", "short")]
        [InlineData("float", "byte", "char", "decimal", "int", "long", "sbyte", "short", "uint", "ulong", "ushort")]
        [InlineData("double", "byte", "char", "decimal", "float", "int", "long", "sbyte", "short", "uint", "ulong", "ushort")]
        [InlineData("decimal", "byte", "char", "double", "float", "int", "long", "sbyte", "short", "uint", "ulong", "ushort")]
        public async Task ExplicitBuiltInNumericConversionsAreOfferedAcordingToSpec(string fromType, params string[] toTypes)
        {
            // built-in numeric conversions:
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions
            var items = await GetCompletionItemsAsync(@$"
public class Program
{{
    public static void Main()
    {{
        {fromType} i = default({fromType});
        i.$$
    }}
}}
", SourceCodeKind.Regular);
            AssertEx.SetEqual(items.Select(i => i.DisplayText), toTypes);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInNumericConversionDescriptionIsLikeAConversionOperatorDescription()
        {
            const string Markup = """
                public class Program
                {
                    public static void Main()
                    {
                        int i = 0;
                        i.$$
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "byte", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
$@"int.explicit operator byte(int value)
{(FormatExplicitConversionDescription(fromType: "int", toType: "byte"))}");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInNumericConversionDescriptionIsLikeAConversionOperatorDescriptionLifted()
        {
            const string Markup = """
                public class Program
                {
                    public static void Main()
                    {
                        int? i = 0;
                        i.$$
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "byte?", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
$@"int.explicit operator byte?(int? value)
{(FormatExplicitConversionDescription(fromType: "int", toType: "byte"))}");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionsAreSortedAndComplete()
        {
            // built-in enum conversions:
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            var items = await GetCompletionItemsAsync("""
                public enum E { One }
                public class Program
                {
                    public static void Main()
                    {
                        var e = E.One;
                        e.$$
                    }
                }
                """, SourceCodeKind.Regular);
            var expected = new[] { "byte", "char", "decimal", "double", "float", "int", "long", "sbyte", "short", "uint", "ulong", "ushort" };
            AssertEx.SetEqual(items.Select(i => i.DisplayText), expected);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionDescriptionIsLikeAConversionOperatorDescription()
        {
            const string Markup = """
                public enum E { One }
                public class Program
                {
                    public static void Main()
                    {
                        var e = E.One;
                        e.$$
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "int", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
$@"E.explicit operator int(E value)
{FormatExplicitConversionDescription("E", "int")}");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionDescriptionIsLikeAConversionOperatorDescriptionLifted()
        {
            const string Markup = """
                public enum E { One }
                public class Program
                {
                    public static void Main()
                    {
                        E? e = E.One;
                        e.$$
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "int?", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
$@"E.explicit operator int?(E? value)
{(FormatExplicitConversionDescription(fromType: "E", toType: "int"))}");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionDescriptionIsLikeAConversionOperatorDescriptionUnimportedNamespaceMinimalName()
        {
            const string Markup = """
                namespace A.B
                {
                    public enum E { One }
                }
                namespace A.C
                {
                    public class Program
                    {
                        public static void Main()
                        {
                            var e = B.E.One;
                            e.$$
                        }
                    }
                }
                """;
            await VerifyItemExistsAsync(Markup, "int", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: [FilterSet.OperatorFilter],
                expectedDescriptionOrNull:
@$"B.E.explicit operator int(B.E value)
{FormatExplicitConversionDescription("B.E", "int")}");
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("e.$$", true)]
        [InlineData("e. $$", true)]
        [InlineData("e.in$$", true)]
        [InlineData("E.$$", false)] // Don't infer with enum member suggestion 
        [InlineData("E.One.$$", true)]
        public async Task ExplicitBuiltInEnumConversionToIntAreOffered(string expression, bool conversionIsOffered)
        {
            Func<string, Task> verifyFunc = conversionIsOffered
                ? markup => VerifyItemExistsAsync(markup, "int", displayTextPrefix: "(", displayTextSuffix: ")")
                : markup => VerifyNoItemsExistAsync(markup);
            await verifyFunc(@$"
public enum E {{ One }}
public class Program
{{
    public static void Main()
    {{
        var e = E.One;
        {expression}
    }}
}}
");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionInheritedConversions()
        {
            // Base class lookup rule: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#processing-of-user-defined-explicit-conversions
            await VerifyItemExistsAsync("""
                public class Base {
                    public static explicit operator int(Base b) => 0;
                }
                public class Derived: Base
                {
                }
                public class Program
                {
                    public static void Main()
                    {
                        var d = new Derived();
                        var i = d.$$
                    }
                }
                """, "int", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: [FilterSet.OperatorFilter]);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("C", "byte")]
        [InlineData("byte", "C")]
        public async Task ExplicitBuiltinConversionWithAlias(string fromType, string expected)
        {
            await VerifyItemExistsAsync(@$"
using C = System.Char;
public class Program
{{
    public static void Main()
    {{
        var test = new {fromType}();
        var i = test.$$
    }}
}}
", expected, displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: [FilterSet.OperatorFilter]);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateNever()
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
                        public static explicit operator int(C _) => 0;
                    }
                }
                """;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateNever_InheritedConversion_1()
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
                    public class Base {
                        [EditorBrowsable(EditorBrowsableState.Never)]
                        public static explicit operator int(Base b) => 0;
                    }
                    public class Derived: Base
                    {
                    }
                }
                """;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateNever_InheritedConversion_2()
        {
            var markup = """
                namespace N
                {
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
                }
                """;
            var referencedCode = """
                using System.ComponentModel;

                namespace N
                {
                    public class Base {
                        [EditorBrowsable(EditorBrowsableState.Never)]
                        public static explicit operator int(Base b) => 0;
                    }
                }
                """;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateAdvanced()
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
                        public static explicit operator int(C _) => 0;
                    }
                }
                """;

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionOfDerefenrencedPointerIsNotOffered()
        {
            await VerifyNoItemsExistAsync("""
                public struct S {
                    public static explicit operator int(S s) => 0;
                }
                public class Program
                {
                    public static void Main()
                    {
                        unsafe{
                            var s = new S();
                            S* p = &s;
                            var i = p->$$;
                        }
                    }
                }
                """);
        }
    }
}
