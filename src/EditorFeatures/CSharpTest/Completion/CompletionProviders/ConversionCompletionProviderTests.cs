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
    public class CSharpCompletionCommandHandlerTests_Conversions : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(UnnamedSymbolCompletionProvider);

        private static string FormatExplicitConversionDescription(string fromType, string toType)
            => string.Format(WorkspacesResources.Predefined_conversion_from_0_to_1, fromType, toType);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task OperatorIndexerCompletionItemsShouldBePlacedLastInCompletionList()
        {
            var castCompletionItem = (await GetCompletionItemsAsync(@"
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
", SourceCodeKind.Regular)).Single();

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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsNotOfferedAfterNumberLiteral()
        {
            // User may want to type a floating point literal.
            await VerifyNoItemsExistAsync(@"
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
", SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedAfterDot()
        {
            await VerifyItemExistsAsync(@"
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
", "float", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedIfMemberNameIsPartiallyWritten()
        {
            await VerifyItemExistsAsync(@"
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
", "float", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("c.$$", true)]
        [InlineData("c.fl$$", true)]
        [InlineData("c.  $$", true)]
        [InlineData("c.fl  $$", false)]
        [InlineData("c.($$", false)]
        [InlineData("c$$", false)]
        [InlineData(@"""c.$$", false)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsNotSuggestedOnStaticAccess()
        {
            await VerifyNoItemsExistAsync(@"
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsNotSuggestedInNameofContext()
        {
            await VerifyNoItemsExistAsync(@"
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
");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsSuggestedForAllExplicitConversionsToOtherTypesAndNotForImplicitConversions()
        {
            var items = await GetCompletionItemsAsync(@"
public class C
{
    public static explicit operator float(C c) => 0;
    public static explicit operator int(C c) => 0;
    
    public static explicit operator C(float f) => new C();
    public static implicit operator C(string s) => new C();
    public static implicit operator string(C c) => "";
}

public class Program
{
    public static void Main()
    {
        var c = new C();
        c.$$
    }
}
", SourceCodeKind.Regular);
            Assert.Collection(items,
                i => Assert.Equal("float", i.DisplayText),
                i => Assert.Equal("int", i.DisplayText));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIgnoresConversionLikeMethods()
        {
            await VerifyNoItemsExistAsync(@"
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIgnoreMalformedOperators()
        {
            await VerifyNoItemsExistAsync(@"
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionFromOtherTypeToTargetIsNotSuggested()
        {
            await VerifyNoItemsExistAsync(@"
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
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionIsApplied()
        {
            await VerifyCustomCommitProviderAsync(@"
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
", "float", @"
public class C
{
    public static explicit operator float(C c) => 0;
}

public class Program
{
    public static void Main()
    {
        var c = new C();
        ((float)c)$$
    }
}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("white.$$", "Black",
                    "((Black)white)$$")]
        [InlineData("white.$$;", "Black",
                    "((Black)white)$$;")]
        [InlineData("white.Bl$$", "Black",
                    "((Black)white)$$")]
        [InlineData("white.Bl$$;", "Black",
                    "((Black)white)$$;")]
        [InlineData("white?.Bl$$;", "Black",
                    "((Black)white)?$$;")]
        [InlineData("white.$$Bl;", "Black",
                    "((Black)white)$$Bl;")]
        [InlineData("var f = white.$$;", "Black",
                    "var f = ((Black)white)$$;")]
        [InlineData("white?.$$", "Black",
                    "((Black)white)?$$")]
        [InlineData("white?.$$b", "Black",
                    "((Black)white)?$$b")]
        [InlineData("white?.$$b.c()", "Black",
                    "((Black)white)?$$b.c()")]
        [InlineData("white?.$$b()", "Black",
                    "((Black)white)?$$b()")]
        [InlineData("white.Black?.$$", "White",
                    "((White)white.Black)?$$")]
        [InlineData("white.Black.$$", "White",
                    "((White)white.Black)$$")]
        [InlineData("white?.Black?.$$", "White",
                    "((White)white?.Black)?$$")]
        [InlineData("white?.Black?.fl$$", "White",
                    "((White)white?.Black)?$$")]
        [InlineData("white?.Black.fl$$", "White",
                    "((White)white?.Black)$$")]
        [InlineData("white?.Black.White.Bl$$ack?.White", "Black",
                    "((Black)white?.Black.White)$$?.White")]
        [InlineData("((White)white).$$", "Black",
                    "((Black)((White)white))$$")]
        [InlineData("(true ? white : white).$$", "Black",
                    "((Black)(true ? white : white))$$")]
        public async Task ExplicitUserDefinedConversionIsAppliedForDifferentExpressions(string expression, string conversionOffering, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
namespace N
{{
    public class Black
    {{
        public White White {{ get; }}
        public static explicit operator White(Black _) => new White();
    }}
    public class White
    {{
        public Black Black {{ get; }}
        public static explicit operator Black(White _) => new Black();
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var white = new White();
            {expression}
        }}
    }}
}}
", conversionOffering, @$"
namespace N
{{
    public class Black
    {{
        public White White {{ get; }}
        public static explicit operator White(Black _) => new White();
    }}
    public class White
    {{
        public Black Black {{ get; }}
        public static explicit operator Black(White _) => new Black();
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var white = new White();
            {fixedCode}
        }}
    }}
}}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        // Source: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
        [InlineData("bool")]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("char")]
        [InlineData("decimal")]
        [InlineData("double")]
        [InlineData("float")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("object")] //not valid: https://docs.microsoft.com/de-de/dotnet/csharp/misc/cs0553
        [InlineData("string")]
        [InlineData("dynamic")] //not valid: CS1964 conversion to or from dynamic type is not allowed
        public async Task ExplicitUserDefinedConversionIsAppliedForBuiltinTypeKeywords(string builtinType)
        {
            await VerifyCustomCommitProviderAsync($@"
namespace N
{{
    public class C
    {{
        public static explicit operator {builtinType}(C _) => default;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            c.{builtinType}$$
        }}
    }}
}}
", $"{builtinType}", @$"
namespace N
{{
    public class C
    {{
        public static explicit operator {builtinType}(C _) => default;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            (({builtinType})c)$$
        }}
    }}
}}
");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        // List derived from https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
        // Includes all keywords and contextual keywords
        [InlineData("abstract")]
        [InlineData("as")]
        [InlineData("base")]
        [InlineData("bool")]
        [InlineData("break")]
        [InlineData("byte")]
        [InlineData("case")]
        [InlineData("catch")]
        [InlineData("char")]
        [InlineData("checked")]
        [InlineData("class")]
        [InlineData("const")]
        [InlineData("continue")]
        [InlineData("decimal")]
        [InlineData("default")]
        [InlineData("delegate")]
        [InlineData("do")]
        [InlineData("double")]
        [InlineData("else")]
        [InlineData("enum")]
        [InlineData("event")]
        [InlineData("explicit")]
        [InlineData("extern")]
        [InlineData("false")]
        [InlineData("finally")]
        [InlineData("fixed")]
        [InlineData("float")]
        [InlineData("for")]
        [InlineData("foreach")]
        [InlineData("goto")]
        [InlineData("if")]
        [InlineData("implicit")]
        [InlineData("in")]
        [InlineData("int")]
        [InlineData("interface")]
        [InlineData("internal")]
        [InlineData("is")]
        [InlineData("lock")]
        [InlineData("long")]
        [InlineData("namespace")]
        [InlineData("new")]
        [InlineData("null")]
        [InlineData("object")]
        [InlineData("operator")]
        [InlineData("out")]
        [InlineData("override")]
        [InlineData("params")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("public")]
        [InlineData("readonly")]
        [InlineData("ref")]
        [InlineData("return")]
        [InlineData("sbyte")]
        [InlineData("sealed")]
        [InlineData("short")]
        [InlineData("sizeof")]
        [InlineData("stackalloc")]
        [InlineData("static")]
        [InlineData("string")]
        [InlineData("struct")]
        [InlineData("switch")]
        [InlineData("this")]
        [InlineData("throw")]
        [InlineData("true")]
        [InlineData("try")]
        [InlineData("typeof")]
        [InlineData("uint")]
        [InlineData("ulong")]
        [InlineData("unchecked")]
        [InlineData("unsafe")]
        [InlineData("ushort")]
        [InlineData("using")]
        [InlineData("virtual")]
        [InlineData("void")]
        [InlineData("volatile")]
        [InlineData("while")]
        [InlineData("add")]
        [InlineData("alias")]
        [InlineData("ascending")]
        [InlineData("async")]
        [InlineData("await")]
        [InlineData("by")]
        [InlineData("descending")]
        [InlineData("dynamic")]
        [InlineData("equals")]
        [InlineData("from")]
        [InlineData("get")]
        [InlineData("global")]
        [InlineData("group")]
        [InlineData("into")]
        [InlineData("join")]
        [InlineData("let")]
        [InlineData("nameof")]
        [InlineData("notnull")]
        [InlineData("on")]
        [InlineData("orderby")]
        [InlineData("partial")]
        [InlineData("remove")]
        [InlineData("select")]
        [InlineData("set")]
        [InlineData("unmanaged")]
        [InlineData("value")]
        [InlineData("var")]
        [InlineData("when")]
        [InlineData("where")]
        [InlineData("yield")]
        public async Task ExplicitUserDefinedConversionIsAppliedForOtherKeywords(string keyword)
        {
            await VerifyCustomCommitProviderAsync($@"
namespace N
{{
    public class {keyword}Class
    {{
    }}
    public class C
    {{
        public static explicit operator {keyword}Class(C _) => new {keyword}Class;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            c.{keyword}$$
        }}
    }}
}}
", $"{keyword}Class", @$"
namespace N
{{
    public class {keyword}Class
    {{
    }}
    public class C
    {{
        public static explicit operator {keyword}Class(C _) => new {keyword}Class;
    }}
    
    public class Program
    {{
        public static void Main()
        {{
            var c = new C();
            (({keyword}Class)c)$$
        }}
    }}
}}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionToGenericType()
        {
            await VerifyCustomCommitProviderAsync(
@"
public class C<T>
{
    public static explicit operator D<T>(C<T> _) => default;
}
public class D<T>
{
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C<int>();
            c.$$
        }
    }
}
", "D<int>",
@"
public class C<T>
{
    public static explicit operator D<T>(C<T> _) => default;
}
public class D<T>
{
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C<int>();
            ((D<int>)c)$$
        }
    }
}
"
            );
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionToArray()
        {
            await VerifyCustomCommitProviderAsync(
@"
public class C
{
    public static explicit operator int[](C _) => default;
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C();
            c.$$
        }
    }
}
", "int[]",
@"
public class C
{
    public static explicit operator int[](C _) => default;
}
public class Program
{
    public static void Main()
    {
        {
            var c = new C();
            ((int[])c)$$
        }
    }
}
"
            );
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [InlineData("/* Leading */c.$$",
                    "/* Leading */((float)c)$$")]
        [InlineData("/* Leading */c.fl$$",
                    "/* Leading */((float)c)$$")]
        [InlineData("c.  $$",
                    "((float)c)$$  ")]
        [InlineData("(true ? /* Inline */ c : c).$$",
                    "((float)(true ? /* Inline */ c : c))$$")]
        [InlineData("c.fl$$ /* Trailing */",
                    "((float)c)$$ /* Trailing */")]
        public async Task ExplicitUserDefinedConversionTriviaHandling(string expression, string fixedCode)
        {
            await VerifyCustomCommitProviderAsync($@"
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
", "float", @$"
public class C
{{
    public static explicit operator float(C c) => 0;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitConversionOfNullableStructToNullableStructIsOffered()
        {
            // Lifted conversion https://docs.microsoft.com/hu-hu/dotnet/csharp/language-reference/language-specification/conversions#lifted-conversion-operators
            await VerifyItemExistsAsync(@"
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
", "int?", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        [CombinatorialData]
        public async Task ExplicitConversionOfConditionalAccessFromClassOrStructToClassOrStruct(
            [CombinatorialValues("struct", "class")] string fromClassOrStruct,
            [CombinatorialValues("struct", "class")] string toClassOrStruct,
            bool propertyIsNullable,
            bool conditionalAccess)
        {
            if (fromClassOrStruct == "class" && propertyIsNullable)
            {
                // This test is solely about lifting of nullable value types. The CombinatorialData also 
                // adds cases for nullable reference types: public class From ... public From? From { get; }
                // We don't want to test NRT cases here.
                return;
            }

            var assertShouldBeNullable =
                fromClassOrStruct == "struct" &&
                toClassOrStruct == "struct" &&
                (propertyIsNullable || conditionalAccess);

            var propertyNullableQuestionMark = propertyIsNullable ? "?" : "";
            var conditionalAccessQuestionMark = conditionalAccess ? "?" : "";
            var shouldBeNullableQuestionMark = assertShouldBeNullable ? "?" : "";
            await VerifyCustomCommitProviderAsync(@$"
public {fromClassOrStruct} From {{
    public static explicit operator To(From _) => default;
}}
public {toClassOrStruct} To {{
}}
public class C {{
    public From{propertyNullableQuestionMark} From {{ get; }} = default;
}}
public class Program
{{
    public static void Main()
    {{
        C c = null;
        c{conditionalAccessQuestionMark}.From.$$
    }}
}}
", $"To{shouldBeNullableQuestionMark}", @$"
public {fromClassOrStruct} From {{
    public static explicit operator To(From _) => default;
}}
public {toClassOrStruct} To {{
}}
public class C {{
    public From{propertyNullableQuestionMark} From {{ get; }} = default;
}}
public class Program
{{
    public static void Main()
    {{
        C c = null;
        ((To{shouldBeNullableQuestionMark})c{conditionalAccessQuestionMark}.From)$$
    }}
}}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitConversionDescriptionIsIsGiven()
        {
            const string Markup = @"
public struct S {
        /// <summary>
        /// Explicit conversion of <see cref=""S""/> to <see cref=""int""/>.
        /// </summary>
        /// <param name=""value"">The <see cref=""S""/> to convert</param>
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
";
            await VerifyItemExistsAsync(Markup, "int", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
@"S.explicit operator int(S value)
Explicit conversion of S to int.");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitConversionDescriptionIsIsGivenLifted()
        {
            const string Markup = @"
public struct S {
        /// <summary>
        /// Explicit conversion of <see cref=""S""/> to <see cref=""int""/>.
        /// </summary>
        /// <param name=""value"">The <see cref=""S""/> to convert</param>
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
";
            await VerifyItemExistsAsync(Markup, "int?", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
@"S.explicit operator int?(S? value)
Explicit conversion of S to int.");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInNumericConversionDescriptionIsLikeAConversionOperatorDescription()
        {
            const string Markup = @"
public class Program
{
    public static void Main()
    {
        int i = 0;
        i.$$
    }
}
";
            await VerifyItemExistsAsync(Markup, "byte", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
$@"int.explicit operator byte(int value)
{(FormatExplicitConversionDescription(fromType: "int", toType: "byte"))}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInNumericConversionDescriptionIsLikeAConversionOperatorDescriptionLifted()
        {
            const string Markup = @"
public class Program
{
    public static void Main()
    {
        int? i = 0;
        i.$$
    }
}
";
            await VerifyItemExistsAsync(Markup, "byte?", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
$@"int.explicit operator byte?(int? value)
{(FormatExplicitConversionDescription(fromType: "int", toType: "byte"))}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionsAreSortedAndComplete()
        {
            // built-in enum conversions:
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            var items = await GetCompletionItemsAsync(@"
public enum E { One }
public class Program
{
    public static void Main()
    {
        var e = E.One;
        e.$$
    }
}
", SourceCodeKind.Regular);
            var expected = new[] { "byte", "char", "decimal", "double", "float", "int", "long", "sbyte", "short", "uint", "ulong", "ushort" };
            AssertEx.SetEqual(items.Select(i => i.DisplayText), expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionDescriptionIsLikeAConversionOperatorDescription()
        {
            const string Markup = @"
public enum E { One }
public class Program
{
    public static void Main()
    {
        var e = E.One;
        e.$$
    }
}
";
            await VerifyItemExistsAsync(Markup, "int", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
$@"E.explicit operator int(E value)
{FormatExplicitConversionDescription("E", "int")}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionDescriptionIsLikeAConversionOperatorDescriptionLifted()
        {
            const string Markup = @"
public enum E { One }
public class Program
{
    public static void Main()
    {
        E? e = E.One;
        e.$$
    }
}
";
            await VerifyItemExistsAsync(Markup, "int?", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
$@"E.explicit operator int?(E? value)
{(FormatExplicitConversionDescription(fromType: "E", toType: "int"))}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitBuiltInEnumConversionDescriptionIsLikeAConversionOperatorDescriptionUnimportedNamespaceMinimalName()
        {
            const string Markup = @"
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
";
            await VerifyItemExistsAsync(Markup, "int", displayTextPrefix: "(", displayTextSuffix: ")",
                glyph: (int)Glyph.Operator,
                matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter },
                expectedDescriptionOrNull:
@$"B.E.explicit operator int(B.E value)
{FormatExplicitConversionDescription("B.E", "int")}");
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionInheritedConversions()
        {
            // Base class lookup rule: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#processing-of-user-defined-explicit-conversions
            await VerifyItemExistsAsync(@"
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
", "int", displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
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
", expected, displayTextPrefix: "(", displayTextSuffix: ")", glyph: (int)Glyph.Operator, matchingFilters: new List<CompletionFilter> { FilterSet.OperatorFilter });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateNever()
        {
            var markup = @"
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
";
            var referencedCode = @"
using System.ComponentModel;

namespace N
{
    public class C
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static explicit operator int(C _) => 0;
    }
}
";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateNever_InheritedConversion_1()
        {
            var markup = @"
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
";
            var referencedCode = @"
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
";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateNever_InheritedConversion_2()
        {
            var markup = @"
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
";
            var referencedCode = @"
using System.ComponentModel;

namespace N
{
    public class Base {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static explicit operator int(Base b) => 0;
    }
}
";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "int",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task TestEditorBrowsableOnConversionIsRespected_EditorBrowsableStateAdvanced()
        {
            var markup = @"
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
";
            var referencedCode = @"
using System.ComponentModel;

namespace N
{
    public class C
    {
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static explicit operator int(C _) => 0;
    }
}
";

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionOfNullableStructAccessViaNullcondionalOffersLiftedConversion()
        {
            await VerifyCustomCommitProviderAsync(@"
public struct S {
    public static explicit operator int(S s) => 0;
}
public class Program
{
    public static void Main()
    {
        S? s = null;
        var i = ((S?)s)?.$$
    }
}
", "int?", @"
public struct S {
    public static explicit operator int(S s) => 0;
}
public class Program
{
    public static void Main()
    {
        S? s = null;
        var i = ((int?)((S?)s))?$$
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionOfPropertyNamedLikeItsTypeIsHandled()
        {
            await VerifyCustomCommitProviderAsync(@"
public struct S {
    public static explicit operator int(S s) => 0;
}
public class C {
    public S S { get; }
}
public class Program
{
    public static void Main()
    {
        var c = new C();
        var i = c.S.$$
    }
}
", "int", @"
public struct S {
    public static explicit operator int(S s) => 0;
}
public class C {
    public S S { get; }
}
public class Program
{
    public static void Main()
    {
        var c = new C();
        var i = ((int)c.S)$$
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(47511, "https://github.com/dotnet/roslyn/issues/47511")]
        public async Task ExplicitUserDefinedConversionOfDerefenrencedPointerIsNotOffered()
        {
            await VerifyNoItemsExistAsync(@"
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
");
        }
    }
}
