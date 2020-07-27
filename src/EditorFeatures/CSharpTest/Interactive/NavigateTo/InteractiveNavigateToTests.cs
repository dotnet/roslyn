// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo
{
    [Trait(Traits.Feature, Traits.Features.NavigateTo)]
    public class InteractiveNavigateToTests : AbstractNavigateToTests
    {
        protected override string Language => "csharp";

        protected override TestWorkspace CreateWorkspace(string content, ExportProvider exportProvider)
            => TestWorkspace.CreateCSharp(content, parseOptions: Options.Script, exportProvider: exportProvider);

        [WpfTheory]
        [CombinatorialData]
        public async Task NoItemsForEmptyFile(TestHost testHost)
        {
            await TestAsync(testHost, "", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("Hello"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindClass(TestHost testHost)
        {
            await TestAsync(testHost,
@"class Goo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassPrivate);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindNestedClass(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    class Bar
    {
        internal class DogBed
        {
        }
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DogBed")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DogBed", "[|DogBed|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindMemberInANestedClass(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    class Bar
    {
        class DogBed
        {
            public void Method()
            {
            }
        }
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Method")).Single();
                VerifyNavigateToResultItem(item, "Method", "[|Method|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo.Bar.DogBed", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindGenericClassWithConstraints(TestHost testHost)
        {
            await TestAsync(testHost,
 @"using System.Collections;

class Goo<T> where T : IEnumerable
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]<T>", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassPrivate);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindGenericMethodWithConstraints(TestHost testHost)
        {
            await TestAsync(testHost,
 @"using System;

class Goo<U>
{
    public void Bar<T>(T item) where T : IComparable<T>
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]<T>(T)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo<U>", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindPartialClass(TestHost testHost)
        {
            await TestAsync(testHost,
 @"public partial class Goo
{
    int a;
}

partial class Goo
{
    int b;
}", async w =>
            {
                var expecteditem1 = new NavigateToItem("Goo", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Goo");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindTypesInMetadata(TestHost testHost)
        {
            await TestAsync(testHost,
 @"using System;

Class Program { FileStyleUriParser f; }", async w =>
            {
                var items = await _aggregator.GetItemsAsync("FileStyleUriParser");
                Assert.Equal(0, items.Count());
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindClassInNamespace(TestHost testHost)
        {
            await TestAsync(testHost,
 @"namespace Bar
{
    class Goo
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindStruct(TestHost testHost)
        {
            await TestAsync(testHost,
 @"struct Bar
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Structure, Glyph.StructurePrivate);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindEnum(TestHost testHost)
        {
            await TestAsync(testHost,
 @"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Colors")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Colors", "[|Colors|]", PatternMatchKind.Exact, NavigateToItemKind.Enum, Glyph.EnumPrivate);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindEnumMember(TestHost testHost)
        {
            await TestAsync(testHost,
 @"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("R")).Single();
                VerifyNavigateToResultItem(item, "Red", "[|R|]ed", PatternMatchKind.Prefix, NavigateToItemKind.EnumItem, Glyph.EnumMemberPublic);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindConstField(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    const int bar = 7;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Constant, Glyph.ConstantPrivate);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindVerbatimIdentifier(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    string @string;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindIndexer(TestHost testHost)
        {
            var program = @"class Goo { int[] arr; public int this[int i] { get { return arr[i]; } set { arr[i] = value; } } }";
            await TestAsync(testHost, program, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("this")).Single();
                VerifyNavigateToResultItem(item, "this", "[|this|][int]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindEvent(TestHost testHost)
        {
            var program = "class Goo { public event EventHandler ChangedEventHandler; }";
            await TestAsync(testHost, program, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("CEH")).Single();
                VerifyNavigateToResultItem(item, "ChangedEventHandler", "[|C|]hanged[|E|]vent[|H|]andler", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Event, Glyph.EventPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindAutoProperty(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    int Bar { get; set; }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Property, Glyph.PropertyPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindMethod(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    void DoSomething();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindParameterizedMethod(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    void DoSomething(int a, string b)
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething(int, string)", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindConstructor(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    public Goo()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindParameterizedConstructor(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    public Goo(int i)
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](int)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindStaticConstructor(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class Goo
{
    static Goo()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method && t.Name != ".ctor");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].static Goo()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindPartialMethods(TestHost testHost)
        {
            await TestAsync(testHost, "partial class Goo { partial void Bar(); } partial class Goo { partial void Bar() { Console.Write(\"hello\"); } }", async w =>
            {
                var expecteditem1 = new NavigateToItem("Bar", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Bar");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindPartialMethodDefinitionOnly(TestHost testHost)
        {
            await TestAsync(testHost,
 @"partial class Goo
{
    partial void Bar();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindOverriddenMembers(TestHost testHost)
        {
            var program = "class Goo { public virtual string Name { get; set; } } class DogBed : Goo { public override string Name { get { return base.Name; } set {} } }";
            await TestAsync(testHost, program, async w =>
            {
                var expecteditem1 = new NavigateToItem("Name", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Name");

                VerifyNavigateToResultItems(expecteditems, items);

                var item = items.ElementAt(0);
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                var unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal(string.Format(FeaturesResources.in_0_project_1, "DogBed", "Test"), itemDisplay.AdditionalInformation);

                item = items.ElementAt(1);
                itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal(string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"), itemDisplay.AdditionalInformation);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindInterface(TestHost testHost)
        {
            await TestAsync(testHost,
 @"public interface IGoo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("IG")).Single();
                VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", PatternMatchKind.Prefix, NavigateToItemKind.Interface, Glyph.InterfacePublic);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindDelegateInNamespace(TestHost testHost)
        {
            await TestAsync(testHost,
 @"namespace Goo
{
    delegate void DoStuff();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DoStuff")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", PatternMatchKind.Exact, NavigateToItemKind.Delegate, Glyph.DelegateInternal);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task FindLambdaExpression(TestHost testHost)
        {
            await TestAsync(testHost,
 @"using System;

class Goo
{
    Func<int, int> sqr = x => x * x;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("sqr")).Single();
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task OrderingOfConstructorsAndTypes(TestHost testHost)
        {
            await TestAsync(testHost,
 @"class C1
{
    C1(int i)
    {
    }
}

class C2
{
    C2(float f)
    {
    }

    static C2()
    {
    }
}", async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("C1", NavigateToItemKind.Class, "csharp", "C1", null, s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("C1", NavigateToItemKind.Method, "csharp", "C1", null, s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("C2", NavigateToItemKind.Class, "csharp", "C2", null, s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, s_emptyPrefixPatternMatch, null), // this is the static ctor
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, s_emptyPrefixPatternMatch, null),
                };
                var items = (await _aggregator.GetItemsAsync("C")).ToList();
                items.Sort(CompareNavigateToItems);
                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task StartStopSanity(TestHost testHost)
        {
            // Verify that multiple calls to start/stop and dispose don't blow up
            await TestAsync(testHost,
 @"public class Goo
{
}", async w =>
            {
                // Do one set of queries
                Assert.Single((await _aggregator.GetItemsAsync("Goo")).Where(x => x.Kind != "Method"));
                _provider.StopSearch();

                // Do the same query again, make sure nothing was left over
                Assert.Single((await _aggregator.GetItemsAsync("Goo")).Where(x => x.Kind != "Method"));
                _provider.StopSearch();

                // Dispose the provider
                _provider.Dispose();
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task DescriptionItems(TestHost testHost)
        {
            var code = "public\r\nclass\r\nGoo\r\n{ }";
            await TestAsync(testHost, code, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("G")).Single(x => x.Kind != "Method");
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);

                var descriptionItems = itemDisplay.DescriptionItems;

                void assertDescription(string label, string value)
                {
                    var descriptionItem = descriptionItems.Single(i => i.Category.Single().Text == label);
                    Assert.Equal(value, descriptionItem.Details.Single().Text);
                }

                assertDescription("File:", w.Documents.Single().Name);
                assertDescription("Line:", "3"); // one based line number
                assertDescription("Project:", "Test");
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest1(TestHost testHost)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_keyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem3 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCasePrefixPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2, expecteditem3 };

                var items = await _aggregator.GetItemsAsync("GK");

                Assert.Equal(expecteditems.Count(), items.Count());

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest2(TestHost testHost)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("GKW");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest3(TestHost testHost)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("K W");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest4(TestHost testHost)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("WKG");
                Assert.Empty(items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest5(TestHost testHost)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("G_K_W")).Single();
                VerifyNavigateToResultItem(item, "get_key_word", "[|g|]et[|_k|]ey[|_w|]ord", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Field, Glyph.FieldPrivate);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest7(TestHost testHost)
        {
            ////Diff from dev10
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("K*W");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task TermSplittingTest8(TestHost testHost)
        {
            ////Diff from dev10
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("GTW");
                Assert.Empty(items);
            });
        }
    }
}
