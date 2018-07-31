// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

#pragma warning disable CS0618 // MatchKind is obsolete
namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo
{
    public class NavigateToTests : AbstractNavigateToTests
    {
        protected override string Language => "csharp";

        protected override TestWorkspace CreateWorkspace(string content, ExportProvider exportProvider)
            => TestWorkspace.CreateCSharp(content, exportProvider: exportProvider);

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task NoItemsForEmptyFile(Type trackingServiceType)
        {
            await TestAsync("", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("Hello"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindClass(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindVerbatimClass(Type trackingServiceType)
        {
            await TestAsync(
@"class @static
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("static")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "static", "[|static|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);

                // Check searching for @static too
                item = (await _aggregator.GetItemsAsync("@static")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "static", "[|static|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindNestedClass(Type trackingServiceType)
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
                var item = (await _aggregator.GetItemsAsync("DogBed")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DogBed", "[|DogBed|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindMemberInANestedClass(Type trackingServiceType)
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
                var item = (await _aggregator.GetItemsAsync("Method")).Single();
                VerifyNavigateToResultItem(item, "Method", "[|Method|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo.Bar.DogBed", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindGenericClassWithConstraints(Type trackingServiceType)
        {
            await TestAsync(
@"using System.Collections;

class Goo<T> where T : IEnumerable
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]<T>", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindGenericMethodWithConstraints(Type trackingServiceType)
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
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]<T>(T)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo<U>", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindPartialClass(Type trackingServiceType)
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
                var expecteditem1 = new NavigateToItem("Goo", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Goo");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindTypesInMetadata(Type trackingServiceType)
        {
            await TestAsync(
@"using System;

Class Program { FileStyleUriParser f; }", async w =>
            {
                var items = await _aggregator.GetItemsAsync("FileStyleUriParser");
                Assert.Equal(items.Count(), 0);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindClassInNamespace(Type trackingServiceType)
        {
            await TestAsync(
@"namespace Bar
{
    class Goo
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindStruct(Type trackingServiceType)
        {
            await TestAsync(
@"struct Bar
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Structure, Glyph.StructureInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindEnum(Type trackingServiceType)
        {
            await TestAsync(
@"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Colors")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Colors", "[|Colors|]", PatternMatchKind.Exact, NavigateToItemKind.Enum, Glyph.EnumInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindEnumMember(Type trackingServiceType)
        {
            await TestAsync(
@"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("R")).Single();
                VerifyNavigateToResultItem(item, "Red", "[|R|]ed", PatternMatchKind.Prefix, NavigateToItemKind.EnumItem, Glyph.EnumMemberPublic);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindField1(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int bar;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("b")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|b|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindField2(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int bar;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindField3(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int bar;
}", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("ar"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindVerbatimField(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int @string;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));

                // Check searching for@string too
                item = (await _aggregator.GetItemsAsync("@string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindPtrField1(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int* bar;
}", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("ar"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindPtrField2(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int* bar;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("b")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|b|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindConstField(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    const int bar = 7;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Constant, Glyph.ConstantPrivate);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindIndexer(Type trackingServiceType)
        {
            var program = @"class Goo { int[] arr; public int this[int i] { get { return arr[i]; } set { arr[i] = value; } } }";
            await TestAsync(program, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("this")).Single();
                VerifyNavigateToResultItem(item, "this", "[|this|][int]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindEvent(Type trackingServiceType)
        {
            var program = "class Goo { public event EventHandler ChangedEventHandler; }";
            await TestAsync(program, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("CEH")).Single();
                VerifyNavigateToResultItem(item, "ChangedEventHandler", "[|C|]hanged[|E|]vent[|H|]andler", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Event, Glyph.EventPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindAutoProperty(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    int Bar { get; set; }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Property, Glyph.PropertyPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindMethod(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    void DoSomething();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindVerbatimMethod(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    void @static();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("static")).Single();
                VerifyNavigateToResultItem(item, "static", "[|static|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));

                // Verify if we search for @static too
                item = (await _aggregator.GetItemsAsync("@static")).Single();
                VerifyNavigateToResultItem(item, "static", "[|static|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindParameterizedMethod(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    void DoSomething(int a, string b)
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething(int, string)", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindConstructor(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    public Goo()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindParameterizedConstructor(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    public Goo(int i)
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](int)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindStaticConstructor(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    static Goo()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method && t.Name != ".ctor");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].static Goo()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindPartialMethods(Type trackingServiceType)
        {
            await TestAsync("partial class Goo { partial void Bar(); } partial class Goo { partial void Bar() { Console.Write(\"hello\"); } }", async w =>
            {
                var expecteditem1 = new NavigateToItem("Bar", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Bar");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindPartialMethodDefinitionOnly(Type trackingServiceType)
        {
            await TestAsync(
@"partial class Goo
{
    partial void Bar();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindPartialMethodImplementationOnly(Type trackingServiceType)
        {
            await TestAsync(
@"partial class Goo
{
    partial void Bar()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindOverriddenMembers(Type trackingServiceType)
        {
            var program = "class Goo { public virtual string Name { get; set; } } class DogBed : Goo { public override string Name { get { return base.Name; } set {} } }";
            await TestAsync(program, async w =>
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
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindInterface(Type trackingServiceType)
        {
            await TestAsync(
@"public interface IGoo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("IG")).Single();
                VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", PatternMatchKind.Prefix, NavigateToItemKind.Interface, Glyph.InterfacePublic);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindDelegateInNamespace(Type trackingServiceType)
        {
            await TestAsync(
@"namespace Goo
{
    delegate void DoStuff();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DoStuff")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", PatternMatchKind.Exact, NavigateToItemKind.Delegate, Glyph.DelegateInternal);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindLambdaExpression(Type trackingServiceType)
        {
            await TestAsync(
@"using System;

class Goo
{
    Func<int, int> sqr = x => x * x;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("sqr")).Single();
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindArray(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
    object[] itemArray;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("itemArray")).Single();
                VerifyNavigateToResultItem(item, "itemArray", "[|itemArray|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindClassAndMethodWithSameName(Type trackingServiceType)
        {
            await TestAsync(
@"class Goo
{
}

class Test
{
    void Goo()
    {
    }
}", async w =>
            {
                var expectedItems = new List<NavigateToItem>
                {
                    new NavigateToItem("Goo", NavigateToItemKind.Method, "csharp", "Goo", null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("Goo", NavigateToItemKind.Class, "csharp", "Goo", null, s_emptyExactPatternMatch, null)
                };
                var items = await _aggregator.GetItemsAsync("Goo");
                VerifyNavigateToResultItems(expectedItems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindMethodNestedInGenericTypes(Type trackingServiceType)
        {
            await TestAsync(
@"class A<T>
{
    class B
    {
        struct C<U>
        {
            void M()
            {
            }
        }
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("M")).Single();
                VerifyNavigateToResultItem(item, "M", "[|M|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "A<T>.B.C<U>", "Test"));
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task OrderingOfConstructorsAndTypes(Type trackingServiceType)
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
                    new NavigateToItem("C1", NavigateToItemKind.Class, "csharp", "C1", null, s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("C1", NavigateToItemKind.Method, "csharp", "C1", null, s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("C2", NavigateToItemKind.Class, "csharp", "C2", null, s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, s_emptyPrefixPatternMatch, null), // this is the static ctor
                    new NavigateToItem("C2", NavigateToItemKind.Method, "csharp", "C2", null, s_emptyPrefixPatternMatch, null),
                };
                var items = (await _aggregator.GetItemsAsync("C")).ToList();
                items.Sort(CompareNavigateToItems);
                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task NavigateToMethodWithNullableParameter(Type trackingServiceType)
        {
            await TestAsync(
@"class C
{
    void M(object? o)
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("M")).Single();
                VerifyNavigateToResultItem(item, "M", "[|M|](object?)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task StartStopSanity(Type trackingServiceType)
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
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DescriptionItems(Type trackingServiceType)
        {
            await TestAsync("public\r\nclass\r\nGoo\r\n{ }", async w =>
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
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest1(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_keyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem3 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCasePrefixPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2, expecteditem3 };

                var items = await _aggregator.GetItemsAsync("GK");

                Assert.Equal(expecteditems.Count(), items.Count());

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest2(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("GKW");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest3(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("K W");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest4(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("WKG");
                Assert.Empty(items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest5(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("G_K_W")).Single();
                VerifyNavigateToResultItem(item, "get_key_word", "[|g|]et[|_k|]ey[|_w|]ord", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Field, Glyph.FieldPrivate);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest6(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null,s_emptyPrefixPatternMatch, null),
                    new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyPrefixPatternMatch_NotCaseSensitive, null)
                };

                var items = await _aggregator.GetItemsAsync("get word");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TermSplittingTest7(Type trackingServiceType)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("GTW");
                Assert.Empty(items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task TestIndexer1(Type trackingServiceType)
        {
            var source =
@"class C
{
    public int this[int y] { get { } }
}

class D
{
    void Goo()
    {
        var q = new C();
        var b = q[4];
    }
}";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("this", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null),
                };

                var items = await _aggregator.GetItemsAsync("this");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern1(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("B.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern2(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                };

                var items = await _aggregator.GetItemsAsync("C.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern3(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("B.B.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern4(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("Baz.Quux");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern5(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("G.B.B.Quux");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern6(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                };

                var items = await _aggregator.GetItemsAsync("F.F.B.B.Quux");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [WorkItem(7855, "https://github.com/dotnet/Roslyn/issues/7855")]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task DottedPattern7(Type trackingServiceType)
        {
            var source = "namespace Goo { namespace Bar { class Baz<X,Y,Z> { void Quux() { } } } }";
            await TestAsync(source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("Baz.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            }, trackingServiceType);
        }

        [WorkItem(1174255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174255")]
        [WorkItem(8009, "https://github.com/dotnet/roslyn/issues/8009")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task NavigateToGeneratedFiles()
        {
            using (var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            namespace N
            {
                public partial class C
                {
                    public void VisibleMethod() { }
                }
            }
        </Document>
        <Document FilePath=""File1.g.cs"">
            namespace N
            {
                public partial class C
                {
                    public void VisibleMethod_Generated() { }
                }
            }
        </Document>
    </Project>
</Workspace>
", exportProvider: TestExportProvider.ExportProviderWithCSharpAndVisualBasic))
            {
                _provider = new NavigateToItemProvider(workspace, AsynchronousOperationListenerProvider.NullListener);
                _aggregator = new NavigateToTestAggregator(_provider);

                var items = await _aggregator.GetItemsAsync("VisibleMethod");
                var expectedItems = new List<NavigateToItem>()
                {
                    new NavigateToItem("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("VisibleMethod_Generated", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                // The pattern matcher should match 'VisibleMethod' to both 'VisibleMethod' and 'VisibleMethod_Not', except that
                // the _Not method is declared in a generated file.
                VerifyNavigateToResultItems(expectedItems, items);
            }
        }

        [WorkItem(11474, "https://github.com/dotnet/roslyn/pull/11474")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task FindFuzzy1(Type trackingServiceType)
        {
            await TestAsync(
@"class C
{
    public void ToError()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ToEror")).Single();
                VerifyNavigateToResultItem(item, "ToError", "ToError()", PatternMatchKind.Fuzzy, NavigateToItemKind.Method, Glyph.MethodPublic);
            }, trackingServiceType);
        }

        [WorkItem(18843, "https://github.com/dotnet/roslyn/issues/18843")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        [MemberData(nameof(TrackingServiceTypes))]
        public async Task Test__arglist(Type trackingServiceType)
        {
            await TestAsync(
@"class C
{
    public void ToError(__arglist)
    {
    }
}", async w =>
{
    var item = (await _aggregator.GetItemsAsync("ToError")).Single();
    VerifyNavigateToResultItem(item, "ToError", "[|ToError|](__arglist)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic);
}, trackingServiceType);
        }
    }
}
#pragma warning restore CS0618 // MatchKind is obsolete
