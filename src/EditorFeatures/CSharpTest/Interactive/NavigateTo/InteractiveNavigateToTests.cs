// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.CodeAnalysis.Editor.Extensibility.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Moq;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo
{
    public class InteractiveNavigateToTests : AbstractNavigateToTests
    {
        protected override string Language => "csharp";

        protected override TestWorkspace CreateWorkspace(string content, ExportProvider exportProvider)
            => TestWorkspace.CreateCSharp(content, parseOptions: Options.Script);

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task NoItemsForEmptyFile()
        {
            await TestAsync("", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("Hello"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindClass()
        {
            await TestAsync(
@"class Goo
{
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", MatchKind.Exact, NavigateToItemKind.Class);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindNestedClass()
        {
            await TestAsync(
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
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend);
                var item = (await _aggregator.GetItemsAsync("DogBed")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DogBed", "[|DogBed|]", MatchKind.Exact, NavigateToItemKind.Class);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindMemberInANestedClass()
        {
            await TestAsync(
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
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("Method")).Single();
                VerifyNavigateToResultItem(item, "Method", "[|Method|]()", MatchKind.Exact, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo.Bar.DogBed", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindGenericClassWithConstraints()
        {
            await TestAsync(
@"using System.Collections;

class Goo<T> where T : IEnumerable
{
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]<T>", MatchKind.Exact, NavigateToItemKind.Class);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindGenericMethodWithConstraints()
        {
            await TestAsync(
@"using System;

class Goo<U>
{
    public void Bar<T>(T item) where T : IComparable<T>
    {
    }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]<T>(T)", MatchKind.Exact, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo<U>", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindPartialClass()
        {
            await TestAsync(
@"public partial class Goo
{
    int a;
}

partial class Goo
{
    int b;
}", async w =>
            {
                var expecteditem1 = new NavigateToItem("Goo", NavigateToItemKind.Class, "csharp", null, null, MatchKind.Exact, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Goo");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindTypesInMetadata()
        {
            await TestAsync(
@"using System;

Class Program { FileStyleUriParser f; }", async w =>
            {
                var items = await _aggregator.GetItemsAsync("FileStyleUriParser");
                Assert.Equal(items.Count(), 0);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindClassInNamespace()
        {
            await TestAsync(
@"namespace Bar
{
    class Goo
    {
    }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend);
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", MatchKind.Exact, NavigateToItemKind.Class);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindStruct()
        {
            await TestAsync(
@"struct Bar
{
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("B")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", MatchKind.Prefix, NavigateToItemKind.Structure);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindEnum()
        {
            await TestAsync(
@"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnum, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("Colors")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Colors", "[|Colors|]", MatchKind.Exact, NavigateToItemKind.Enum);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindEnumMember()
        {
            await TestAsync(
@"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("R")).Single();
                VerifyNavigateToResultItem(item, "Red", "[|R|]ed", MatchKind.Prefix, NavigateToItemKind.EnumItem);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindConstField()
        {
            await TestAsync(
@"class Goo
{
    const int bar = 7;
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", MatchKind.Prefix, NavigateToItemKind.Constant);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindVerbatimIdentifier()
        {
            await TestAsync(
@"class Goo
{
    string @string;
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindIndexer()
        {
            var program = @"class Goo { int[] arr; public int this[int i] { get { return arr[i]; } set { arr[i] = value; } } }";
            await TestAsync(program, async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("this")).Single();
                VerifyNavigateToResultItem(item, "this", "[|this|][int]", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindEvent()
        {
            var program = "class Goo { public event EventHandler ChangedEventHandler; }";
            await TestAsync(program, async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEvent, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("CEH")).Single();
                VerifyNavigateToResultItem(item, "ChangedEventHandler", "[|C|]hanged[|E|]vent[|H|]andler", MatchKind.Regular, NavigateToItemKind.Event, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindAutoProperty()
        {
            await TestAsync(
@"class Goo
{
    int Bar { get; set; }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("B")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", MatchKind.Prefix, NavigateToItemKind.Property, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindMethod()
        {
            await TestAsync(
@"class Goo
{
    void DoSomething();
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", MatchKind.Regular, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindParameterizedMethod()
        {
            await TestAsync(
@"class Goo
{
    void DoSomething(int a, string b)
    {
    }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething(int, string)", MatchKind.Regular, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindConstructor()
        {
            await TestAsync(
@"class Goo
{
    public Goo()
    {
    }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", MatchKind.Exact, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindParameterizedConstructor()
        {
            await TestAsync(
@"class Goo
{
    public Goo(int i)
    {
    }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](int)", MatchKind.Exact, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindStaticConstructor()
        {
            await TestAsync(
@"class Goo
{
    static Goo()
    {
    }
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method && t.Name != ".ctor");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].static Goo()", MatchKind.Exact, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindPartialMethods()
        {
            await TestAsync("partial class Goo { partial void Bar(); } partial class Goo { partial void Bar() { Console.Write(\"hello\"); } }", async w =>
            {
                var expecteditem1 = new NavigateToItem("Bar", NavigateToItemKind.Method, "csharp", null, null, MatchKind.Exact, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Bar");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindPartialMethodDefinitionOnly()
        {
            await TestAsync(
@"partial class Goo
{
    partial void Bar();
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", MatchKind.Exact, NavigateToItemKind.Method, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindOverriddenMembers()
        {
            var program = "class Goo { public virtual string Name { get; set; } } class DogBed : Goo { public override string Name { get { return base.Name; } set {} } }";
            await TestAsync(program, async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                var expecteditem1 = new NavigateToItem("Name", NavigateToItemKind.Property, "csharp", null, null, MatchKind.Exact, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Name");

                VerifyNavigateToResultItems(expecteditems, items);

                var item = items.ElementAt(0);
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                var unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal(string.Format(FeaturesResources.in_0_project_1, "DogBed", "Test"), itemDisplay.AdditionalInformation);
                _glyphServiceMock.Verify();

                item = items.ElementAt(1);
                itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal(string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"), itemDisplay.AdditionalInformation);
                _glyphServiceMock.Verify();
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindInterface()
        {
            await TestAsync(
@"public interface IGoo
{
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupInterface, StandardGlyphItem.GlyphItemPublic);
                var item = (await _aggregator.GetItemsAsync("IG")).Single();
                VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", MatchKind.Prefix, NavigateToItemKind.Interface);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindDelegateInNamespace()
        {
            await TestAsync(
@"namespace Goo
{
    delegate void DoStuff();
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupDelegate, StandardGlyphItem.GlyphItemFriend);
                var item = (await _aggregator.GetItemsAsync("DoStuff")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", MatchKind.Exact, NavigateToItemKind.Delegate);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindLambdaExpression()
        {
            await TestAsync(
@"using System;

class Goo
{
    Func<int, int> sqr = x => x * x;
}", async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("sqr")).Single();
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", MatchKind.Exact, NavigateToItemKind.Field, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task OrderingOfConstructorsAndTypes()
        {
            await TestAsync(
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
                    new NavigateToItem("C1", NavigateToItemKind.Class, "csharp", "C1", null, MatchKind.Prefix, true, null),
                    new NavigateToItem("C1", NavigateToItemKind.Method, "csharp", "C1", null, MatchKind.Prefix, true, null),
                    new NavigateToItem("C2", NavigateToItemKind.Class, "csharp", "C2", null, MatchKind.Prefix, true, null),
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, MatchKind.Prefix, true, null), // this is the static ctor
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, MatchKind.Prefix, true, null),
                };
                var items = (await _aggregator.GetItemsAsync("C")).ToList();
                items.Sort(CompareNavigateToItems);
                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task StartStopSanity()
        {
            // Verify that multiple calls to start/stop and dispose don't blow up
            await TestAsync(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task DescriptionItems()
        {
            var code = "public\r\nclass\r\nGoo\r\n{ }";
            await TestAsync(code, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("G")).Single(x => x.Kind != "Method");
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);

                var descriptionItems = itemDisplay.DescriptionItems;

                Action<string, string> assertDescription = (label, value) =>
                {
                    var descriptionItem = descriptionItems.Single(i => i.Category.Single().Text == label);
                    Assert.Equal(value, descriptionItem.Details.Single().Text);
                };

                assertDescription("File:", w.Documents.Single().Name);
                assertDescription("Line:", "3"); // one based line number
                assertDescription("Project:", "Test");
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest1()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_keyword", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem3 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2, expecteditem3 };

                var items = await _aggregator.GetItemsAsync("GK");

                Assert.Equal(expecteditems.Count(), items.Count());

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest2()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("GKW");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest3()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Substring, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("K W");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest4()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("WKG");
                Assert.Empty(items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest5()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate);
                var item = (await _aggregator.GetItemsAsync("G_K_W")).Single();
                VerifyNavigateToResultItem(item, "get_key_word", "[|g|]et[|_k|]ey[|_w|]ord", MatchKind.Regular, NavigateToItemKind.Field);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest7()
        {
            ////Diff from dev10
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Substring, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("K*W");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest8()
        {
            ////Diff from dev10
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("GTW");
                Assert.Empty(items);
            });
        }
    }
}
