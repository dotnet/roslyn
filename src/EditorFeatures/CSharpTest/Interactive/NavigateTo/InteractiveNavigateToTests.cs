// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Moq;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo
{
    public class InteractiveNavigateToTests
    {
        private readonly Mock<IGlyphService> _glyphServiceMock = new Mock<IGlyphService>(MockBehavior.Strict);

        private INavigateToItemProvider _provider;
        private NavigateToTestAggregator _aggregator;

        private async Task<TestWorkspace> SetupWorkspaceAsync(params string[] files)
        {
            var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFilesAsync(files, parseOptions: Options.Script);
            var aggregateListener = AggregateAsynchronousOperationListener.CreateEmptyListener();

            _provider = new NavigateToItemProvider(
                workspace,
                _glyphServiceMock.Object,
                aggregateListener);
            _aggregator = new NavigateToTestAggregator(_provider);

            return workspace;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task NoItemsForEmptyFile()
        {
            using (var workspace = await SetupWorkspaceAsync())
            {
                Assert.Empty(_aggregator.GetItems("Hello"));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindClass()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("Foo").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindNestedClass()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { class Bar { internal class DogBed { } } }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend);
                var item = _aggregator.GetItems("DogBed").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DogBed", MatchKind.Exact, NavigateToItemKind.Class);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindMemberInANestedClass()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { class Bar { class DogBed { public void Method() { } } } }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("Method").Single();
                VerifyNavigateToResultItem(item, "Method", MatchKind.Exact, NavigateToItemKind.Method, "Method()", $"{EditorFeaturesResources.Type}Foo.Bar.DogBed");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindGenericClassWithConstraints()
        {
            using (var workspace = await SetupWorkspaceAsync("using System.Collections; class Foo<T> where T : IEnumerable { }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("Foo").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class, displayName: "Foo<T>");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindGenericMethodWithConstraints()
        {
            using (var workspace = await SetupWorkspaceAsync("using System; class Foo<U> { public void Bar<T>(T item) where T:IComparable<T> {} }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("Bar").Single();
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar<T>(T)", $"{EditorFeaturesResources.Type}Foo<U>");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindPartialClass()
        {
            using (var workspace = await SetupWorkspaceAsync("public partial class Foo { int a; } partial class Foo { int b; }"))
            {
                var expecteditem1 = new NavigateToItem("Foo", NavigateToItemKind.Class, "csharp", null, null, MatchKind.Exact, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = _aggregator.GetItems("Foo");

                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindTypesInMetadata()
        {
            using (var workspace = await SetupWorkspaceAsync("using System; Class Program {FileStyleUriParser f;}"))
            {
                var items = _aggregator.GetItems("FileStyleUriParser");
                Assert.Equal(items.Count(), 0);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindClassInNamespace()
        {
            using (var workspace = await SetupWorkspaceAsync("namespace Bar { class Foo { } }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend);
                var item = _aggregator.GetItems("Foo").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindStruct()
        {
            using (var workspace = await SetupWorkspaceAsync("struct Bar { }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("B").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Structure);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindEnum()
        {
            using (var workspace = await SetupWorkspaceAsync("enum Colors {Red, Green, Blue}"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnum, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("Colors").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Colors", MatchKind.Exact, NavigateToItemKind.Enum);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindEnumMember()
        {
            using (var workspace = await SetupWorkspaceAsync("enum Colors {Red, Green, Blue}"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("R").Single();
                VerifyNavigateToResultItem(item, "Red", MatchKind.Prefix, NavigateToItemKind.EnumItem);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindConstField()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { const int bar = 7;}"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("ba").Single();
                VerifyNavigateToResultItem(item, "bar", MatchKind.Prefix, NavigateToItemKind.Constant);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindVerbatimIdentifier()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { string @string; }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("string").Single();
                VerifyNavigateToResultItem(item, "string", MatchKind.Exact, NavigateToItemKind.Field, displayName: "@string", additionalInfo: $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindIndexer()
        {
            var program = @"class Foo { int[] arr; public int this[int i] { get { return arr[i]; } set { arr[i] = value; } } }";
            using (var workspace = await SetupWorkspaceAsync(program))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("this").Single();
                VerifyNavigateToResultItem(item, "this[]", MatchKind.Exact, NavigateToItemKind.Property, displayName: "this[int]", additionalInfo: $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindEvent()
        {
            var program = "class Foo { public event EventHandler ChangedEventHandler; }";
            using (var workspace = await SetupWorkspaceAsync(program))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEvent, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("CEH").Single();
                VerifyNavigateToResultItem(item, "ChangedEventHandler", MatchKind.Regular, NavigateToItemKind.Event, additionalInfo: $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindAutoProperty()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { int Bar { get; set; } }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("B").Single();
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Property, additionalInfo: $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindMethod()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { void DoSomething(); }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("DS").Single();
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething()", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindParameterizedMethod()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { void DoSomething(int a, string b) {} }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("DS").Single();
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething(int, string)", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindConstructor()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { public Foo(){} }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("Foo").Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "Foo()", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindParameterizedConstructor()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { public Foo(int i){} }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("Foo").Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "Foo(int)", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindStaticConstructor()
        {
            using (var workspace = await SetupWorkspaceAsync("class Foo { static Foo(){} }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("Foo").Single(t => t.Kind == NavigateToItemKind.Method && t.Name != ".ctor");
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "static Foo()", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindPartialMethods()
        {
            using (var workspace = await SetupWorkspaceAsync("partial class Foo { partial void Bar(); } partial class Foo { partial void Bar() { Console.Write(\"hello\"); } }"))
            {
                var expecteditem1 = new NavigateToItem("Bar", NavigateToItemKind.Method, "csharp", null, null, MatchKind.Exact, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = _aggregator.GetItems("Bar");

                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindPartialMethodDefinitionOnly()
        {
            using (var workspace = await SetupWorkspaceAsync("partial class Foo { partial void Bar(); }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("Bar").Single();
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar()", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindOverriddenMembers()
        {
            var program = "class Foo { public virtual string Name { get; set; } } class DogBed : Foo { public override string Name { get { return base.Name; } set {} } }";
            using (var workspace = await SetupWorkspaceAsync(program))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                var expecteditem1 = new NavigateToItem("Name", NavigateToItemKind.Property, "csharp", null, null, MatchKind.Exact, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = _aggregator.GetItems("Name");

                VerifyNavigateToResultItems(expecteditems, items);

                var item = items.ElementAt(0);
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                var unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal($"{EditorFeaturesResources.Type}DogBed", itemDisplay.AdditionalInformation);
                _glyphServiceMock.Verify();

                item = items.ElementAt(1);
                itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal($"{EditorFeaturesResources.Type}Foo", itemDisplay.AdditionalInformation);
                _glyphServiceMock.Verify();
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindInterface()
        {
            using (var workspace = await SetupWorkspaceAsync("public interface IFoo { }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupInterface, StandardGlyphItem.GlyphItemPublic);
                var item = _aggregator.GetItems("IF").Single();
                VerifyNavigateToResultItem(item, "IFoo", MatchKind.Prefix, NavigateToItemKind.Interface, displayName: "IFoo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindDelegateInNamespace()
        {
            using (var workspace = await SetupWorkspaceAsync("namespace Foo { delegate void DoStuff(); }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupDelegate, StandardGlyphItem.GlyphItemFriend);
                var item = _aggregator.GetItems("DoStuff").Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DoStuff", MatchKind.Exact, NavigateToItemKind.Delegate, displayName: "DoStuff");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task FindLambdaExpression()
        {
            using (var workspace = await SetupWorkspaceAsync("using System; class Foo { Func<int, int> sqr = x => x*x; }"))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("sqr").Single();
                VerifyNavigateToResultItem(item, "sqr", MatchKind.Exact, NavigateToItemKind.Field, "sqr", $"{EditorFeaturesResources.Type}Foo");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task OrderingOfConstructorsAndTypes()
        {
            using (var workspace = await SetupWorkspaceAsync(@"
                class C1
                {
                    C1(int i) {}
                }
                class C2
                {
                    C2(float f) {}
                    static C2() {}
                }"))
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("C1", NavigateToItemKind.Class, "csharp", "C1", null, MatchKind.Prefix, true, null),
                    new NavigateToItem("C1", NavigateToItemKind.Method, "csharp", "C1", null, MatchKind.Prefix, true, null),
                    new NavigateToItem("C2", NavigateToItemKind.Class, "csharp", "C2", null, MatchKind.Prefix, true, null),
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, MatchKind.Prefix, true, null), // this is the static ctor
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, MatchKind.Prefix, true, null),
                };
                var items = _aggregator.GetItems("C").ToList();
                items.Sort(CompareNavigateToItems);
                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task StartStopSanity()
        {
            // Verify that multiple calls to start/stop and dispose don't blow up
            using (var workspace = await SetupWorkspaceAsync("public class Foo { }"))
            {
                // Do one set of queries
                Assert.Single(_aggregator.GetItems("Foo").Where(x => x.Kind != "Method"));
                _provider.StopSearch();

                // Do the same query again, make sure nothing was left over
                Assert.Single(_aggregator.GetItems("Foo").Where(x => x.Kind != "Method"));
                _provider.StopSearch();

                // Dispose the provider
                _provider.Dispose();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task DescriptionItems()
        {
            var code = "public\r\nclass\r\nFoo\r\n{ }";
            using (var workspace = await SetupWorkspaceAsync(code))
            {
                var item = _aggregator.GetItems("F").Single(x => x.Kind != "Method");
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);

                var descriptionItems = itemDisplay.DescriptionItems;

                Action<string, string> assertDescription = (label, value) =>
                {
                    var descriptionItem = descriptionItems.Single(i => i.Category.Single().Text == label);
                    Assert.Equal(value, descriptionItem.Details.Single().Text);
                };

                assertDescription("File:", workspace.Documents.Single().Name);
                assertDescription("Line:", "3"); // one based line number
                assertDescription("Project:", "Test");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest1()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                var expecteditem1 = new NavigateToItem("get_keyword", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem3 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2, expecteditem3 };

                var items = _aggregator.GetItems("GK");

                Assert.Equal(expecteditems.Count(), items.Count());

                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest2()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = _aggregator.GetItems("GKW");

                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest3()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Substring, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = _aggregator.GetItems("K W");

                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest4()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                var items = _aggregator.GetItems("WKG");
                Assert.Empty(items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest5()
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate);
                var item = _aggregator.GetItems("G_K_W").Single();
                VerifyNavigateToResultItem(item, "get_key_word", MatchKind.Regular, NavigateToItemKind.Field);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest7()
        {
            ////Diff from dev10
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Regular, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, MatchKind.Substring, true, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = _aggregator.GetItems("K*W");

                VerifyNavigateToResultItems(expecteditems, items);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task TermSplittingTest8()
        {
            ////Diff from dev10
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            using (var workspace = await SetupWorkspaceAsync(source))
            {
                var items = _aggregator.GetItems("GTW");
                Assert.Empty(items);
            }
        }

        private void VerifyNavigateToResultItems(List<NavigateToItem> expecteditems, IEnumerable<NavigateToItem> items)
        {
            expecteditems = expecteditems.OrderBy(i => i.Name).ToList();
            items = items.OrderBy(i => i.Name).ToList();

            Assert.Equal(expecteditems.Count(), items.Count());

            for (int i = 0; i < expecteditems.Count; i++)
            {
                var expectedItem = expecteditems[i];
                var actualItem = items.ElementAt(i);
                Assert.Equal(expectedItem.Name, actualItem.Name);
                Assert.Equal(expectedItem.MatchKind, actualItem.MatchKind);
                Assert.Equal(expectedItem.Language, actualItem.Language);
                Assert.Equal(expectedItem.Kind, actualItem.Kind);
                Assert.Equal(expectedItem.IsCaseSensitive, actualItem.IsCaseSensitive);
                if (!string.IsNullOrEmpty(expectedItem.SecondarySort))
                {
                    Assert.Contains(expectedItem.SecondarySort, actualItem.SecondarySort, StringComparison.Ordinal);
                }
            }
        }

        private void VerifyNavigateToResultItem(NavigateToItem result, string name, MatchKind matchKind, string navigateToItemKind,
           string displayName = " ", string additionalInfo = " ")
        {
            string itemDisplayName;

            // Verify symbol information
            Assert.Equal(name, result.Name);
            Assert.Equal(matchKind, result.MatchKind);
            Assert.Equal("csharp", result.Language);
            Assert.Equal(navigateToItemKind, result.Kind);

            // Verify display
            var itemDisplay = result.DisplayFactory.CreateItemDisplay(result);

            itemDisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            Assert.Equal(itemDisplayName, itemDisplay.Name);

            if (!string.IsNullOrWhiteSpace(additionalInfo))
            {
                Assert.Equal(additionalInfo, itemDisplay.AdditionalInformation);
            }

            // Make sure to fetch the glyph
            var unused = itemDisplay.Glyph;
            _glyphServiceMock.Verify();
        }

        private void SetupVerifiableGlyph(StandardGlyphGroup standardGlyphGroup, StandardGlyphItem standardGlyphItem)
        {
            _glyphServiceMock.Setup(service => service.GetGlyph(standardGlyphGroup, standardGlyphItem))
                            .Returns(CreateIconBitmapSource())
                            .Verifiable();
        }

        private BitmapSource CreateIconBitmapSource()
        {
            int stride = PixelFormats.Bgr32.BitsPerPixel / 8 * 16;
            return BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgr32, null, new byte[16 * stride], stride);
        }

        // For ordering of NavigateToItems, see
        // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.language.navigateto.interfaces.navigatetoitem.aspx
        private static int CompareNavigateToItems(NavigateToItem a, NavigateToItem b)
        {
            int result = ((int)a.MatchKind) - ((int)b.MatchKind);
            if (result != 0)
            {
                return result;
            }

            result = a.Name.CompareTo(b.Name);
            if (result != 0)
            {
                return result;
            }

            result = a.Kind.CompareTo(b.Kind);
            if (result != 0)
            {
                return result;
            }

            result = a.SecondarySort.CompareTo(b.SecondarySort);
            return result;
        }
    }
}
