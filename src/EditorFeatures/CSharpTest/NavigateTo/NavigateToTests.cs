// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote.Testing;
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
    [Trait(Traits.Feature, Traits.Features.NavigateTo)]
    public class NavigateToTests : AbstractNavigateToTests
    {
        protected override string Language => "csharp";

        protected override TestWorkspace CreateWorkspace(string content, ExportProvider exportProvider)
            => TestWorkspace.CreateCSharp(content, exportProvider: exportProvider);

        [Theory]
        [CombinatorialData]
        public async Task NoItemsForEmptyFile(TestHost testHost, Composition composition)
        {
            await TestAsync(testHost, composition, "", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("Hello"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindClass(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindRecord(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"record Goo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindRecordClass(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"record class Goo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindRecordStruct(TestHost testHost, Composition composition)
        {
            var content = XElement.Parse(@"
<Workspace>
    <Project Language=""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
record struct Goo
{
}
        </Document>
    </Project>
</Workspace>
");
            await TestAsync(testHost, composition, content, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Structure, Glyph.StructureInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindClassInFileScopedNamespace(TestHost testHost, Composition composition)
        {
            var content = XElement.Parse(@"
<Workspace>
    <Project Language=""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
namespace FileScopedNS;
class Goo { }
        </Document>
    </Project>
</Workspace>
");
            await TestAsync(testHost, composition, content, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindVerbatimClass(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class @static
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("static")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "static", "[|static|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);

                // Check searching for @static too
                item = (await _aggregator.GetItemsAsync("@static")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "static", "[|static|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindNestedClass(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindMemberInANestedClass(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindGenericClassWithConstraints(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"using System.Collections;

class Goo<T> where T : IEnumerable
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]<T>", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindGenericMethodWithConstraints(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"using System;

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

        [Theory]
        [CombinatorialData]
        public async Task FindPartialClass(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"public partial class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindTypesInMetadata(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"using System;

Class Program { FileStyleUriParser f; }", async w =>
            {
                var items = await _aggregator.GetItemsAsync("FileStyleUriParser");
                Assert.Equal(0, items.Count());
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindClassInNamespace(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"namespace Bar
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

        [Theory]
        [CombinatorialData]
        public async Task FindStruct(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"struct Bar
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Structure, Glyph.StructureInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindEnum(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"enum Colors
{
    Red,
    Green,
    Blue
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Colors")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Colors", "[|Colors|]", PatternMatchKind.Exact, NavigateToItemKind.Enum, Glyph.EnumInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindEnumMember(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"enum Colors
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

        [Theory]
        [CombinatorialData]
        public async Task FindField1(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int bar;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("b")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|b|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindField2(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int bar;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindField3(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int bar;
}", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("ar"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindVerbatimField(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int @string;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));

                // Check searching for@string too
                item = (await _aggregator.GetItemsAsync("@string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindPtrField1(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int* bar;
}", async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("ar"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindPtrField2(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int* bar;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("b")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|b|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindConstField(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    const int bar = 7;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Constant, Glyph.ConstantPrivate);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindIndexer(TestHost testHost, Composition composition)
        {
            var program = @"class Goo { int[] arr; public int this[int i] { get { return arr[i]; } set { arr[i] = value; } } }";
            await TestAsync(testHost, composition, program, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("this")).Single();
                VerifyNavigateToResultItem(item, "this", "[|this|][int]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindEvent(TestHost testHost, Composition composition)
        {
            var program = "class Goo { public event EventHandler ChangedEventHandler; }";
            await TestAsync(testHost, composition, program, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("CEH")).Single();
                VerifyNavigateToResultItem(item, "ChangedEventHandler", "[|C|]hanged[|E|]vent[|H|]andler", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Event, Glyph.EventPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindAutoProperty(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    int Bar { get; set; }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Property, Glyph.PropertyPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindMethod(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    void DoSomething();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindVerbatimMethod(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    void @static();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("static")).Single();
                VerifyNavigateToResultItem(item, "static", "[|static|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));

                // Verify if we search for @static too
                item = (await _aggregator.GetItemsAsync("@static")).Single();
                VerifyNavigateToResultItem(item, "static", "[|static|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindParameterizedMethod(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindConstructor(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindParameterizedConstructor(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindStaticConstructor(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task FindPartialMethods(TestHost testHost, Composition composition)
        {
            await TestAsync(testHost, composition, "partial class Goo { partial void Bar(); } partial class Goo { partial void Bar() { Console.Write(\"hello\"); } }", async w =>
            {
                var expecteditem1 = new NavigateToItem("Bar", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Bar");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindPartialMethodDefinitionOnly(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"partial class Goo
{
    partial void Bar();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_1_2, "Goo", "test1.cs", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindPartialMethodImplementationOnly(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"partial class Goo
{
    partial void Bar()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_1_2, "Goo", "test1.cs", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindOverriddenMembers(TestHost testHost, Composition composition)
        {
            var program = "class Goo { public virtual string Name { get; set; } } class DogBed : Goo { public override string Name { get { return base.Name; } set {} } }";
            await TestAsync(testHost, composition, program, async w =>
            {
                var expecteditem1 = new NavigateToItem("Name", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem1 };

                var items = await _aggregator.GetItemsAsync("Name");

                VerifyNavigateToResultItems(expecteditems, items);

                var item = items.ElementAt(1);
                var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                var unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal(string.Format(FeaturesResources.in_0_project_1, "DogBed", "Test"), itemDisplay.AdditionalInformation);

                item = items.ElementAt(0);
                itemDisplay = item.DisplayFactory.CreateItemDisplay(item);
                unused = itemDisplay.Glyph;

                Assert.Equal("Name", itemDisplay.Name);
                Assert.Equal(string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"), itemDisplay.AdditionalInformation);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindInterface(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"public interface IGoo
{
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("IG")).Single();
                VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", PatternMatchKind.Prefix, NavigateToItemKind.Interface, Glyph.InterfacePublic);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindDelegateInNamespace(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"namespace Goo
{
    delegate void DoStuff();
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DoStuff")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", PatternMatchKind.Exact, NavigateToItemKind.Delegate, Glyph.DelegateInternal);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindLambdaExpression(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"using System;

class Goo
{
    Func<int, int> sqr = x => x * x;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("sqr")).Single();
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindArray(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    object[] itemArray;
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("itemArray")).Single();
                VerifyNavigateToResultItem(item, "itemArray", "[|itemArray|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindClassAndMethodWithSameName(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
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
                    new NavigateToItem("Goo", NavigateToItemKind.Class, "csharp", "Goo", null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("Goo", NavigateToItemKind.Method, "csharp", "Goo", null, s_emptyExactPatternMatch, null),
                };
                var items = await _aggregator.GetItemsAsync("Goo");
                VerifyNavigateToResultItems(expectedItems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task FindMethodNestedInGenericTypes(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class A<T>
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
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task OrderingOfConstructorsAndTypes(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class C1
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

        [Theory]
        [CombinatorialData]
        public async Task NavigateToMethodWithNullableParameter(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class C
{
    void M(object? o)
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("M")).Single();
                VerifyNavigateToResultItem(item, "M", "[|M|](object?)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task StartStopSanity(TestHost testHost, Composition composition)
        {
            // Verify that multiple calls to start/stop and dispose don't blow up
            await TestAsync(
testHost, composition, @"public class Goo
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

        [Theory]
        [CombinatorialData]
        public async Task DescriptionItems(TestHost testHost, Composition composition)
        {
            await TestAsync(testHost, composition, "public\r\nclass\r\nGoo\r\n{ }", async w =>
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

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest1(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
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

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest2(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseExactPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("GKW");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest3(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive, null);
                var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch, null);
                var expecteditems = new List<NavigateToItem> { expecteditem1, expecteditem2 };

                var items = await _aggregator.GetItemsAsync("K W");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest4(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("WKG");
                Assert.Empty(items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest5(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("G_K_W")).Single();
                VerifyNavigateToResultItem(item, "get_key_word", "[|g|]et[|_k|]ey[|_w|]ord", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Field, Glyph.FieldPrivate);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest6(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("getkeyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyFuzzyPatternMatch, null),
                    new NavigateToItem("get_keyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyFuzzyPatternMatch, null),
                    new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null,s_emptySubstringPatternMatch, null),
                    new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch_NotCaseSensitive, null)
                };

                var items = await _aggregator.GetItemsAsync("get word");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task TermSplittingTest7(TestHost testHost, Composition composition)
        {
            var source = "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}";
            await TestAsync(testHost, composition, source, async w =>
            {
                var items = await _aggregator.GetItemsAsync("GTW");
                Assert.Empty(items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task TestIndexer1(TestHost testHost, Composition composition)
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
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("this", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null),
                };

                var items = await _aggregator.GetItemsAsync("this");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task DottedPattern1(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("B.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task DottedPattern2(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                };

                var items = await _aggregator.GetItemsAsync("C.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task DottedPattern3(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("B.B.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task DottedPattern4(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("Baz.Quux");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task DottedPattern5(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("G.B.B.Quux");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task DottedPattern6(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                };

                var items = await _aggregator.GetItemsAsync("F.F.B.B.Quux");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(7855, "https://github.com/dotnet/Roslyn/issues/7855")]
        public async Task DottedPattern7(TestHost testHost, Composition composition)
        {
            var source = "namespace Goo { namespace Bar { class Baz<X,Y,Z> { void Quux() { } } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("Baz.Q");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Theory, WorkItem(46267, "https://github.com/dotnet/roslyn/issues/46267")]
        [CombinatorialData]
        public async Task DottedPatternMatchKind(TestHost testHost, Composition composition)
        {
            var source = "namespace System { class Console { void Write(string s) { } void WriteLine(string s) { } } }";
            await TestAsync(testHost, composition, source, async w =>
            {
                var expecteditems = new List<NavigateToItem>
                {
                    new NavigateToItem("Write", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("WriteLine", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)
                };

                var items = await _aggregator.GetItemsAsync("Console.Write");

                VerifyNavigateToResultItems(expecteditems, items);
            });
        }

        [Fact]
        [WorkItem(1174255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174255")]
        [WorkItem(8009, "https://github.com/dotnet/roslyn/issues/8009")]
        public async Task NavigateToGeneratedFiles()
        {
            using var workspace = TestWorkspace.Create(@"
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
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
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

        [WorkItem(11474, "https://github.com/dotnet/roslyn/pull/11474")]
        [Theory]
        [CombinatorialData]
        public async Task FindFuzzy1(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class C
{
    public void ToError()
    {
    }
}", async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ToEror")).Single();
                VerifyNavigateToResultItem(item, "ToError", "ToError()", PatternMatchKind.Fuzzy, NavigateToItemKind.Method, Glyph.MethodPublic);
            });
        }

        [WorkItem(18843, "https://github.com/dotnet/roslyn/issues/18843")]
        [Theory]
        [CombinatorialData]
        public async Task Test__arglist(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class C
{
    public void ToError(__arglist)
    {
    }
}", async w =>
{
    var item = (await _aggregator.GetItemsAsync("ToError")).Single();
    VerifyNavigateToResultItem(item, "ToError", "[|ToError|](__arglist)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic);
});
        }

        [Fact]
        public async Task DoNotIncludeTrivialPartialContainer()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public partial class Outer
            {
                public void VisibleMethod() { }
            }
        </Document>
        <Document FilePath=""File2.cs"">
            public partial class Outer
            {
                public partial class Inner { }
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoNotIncludeTrivialPartialContainerWithMultipleNestedTypes()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public partial class Outer
            {
                public void VisibleMethod() { }
            }
        </Document>
        <Document FilePath=""File2.cs"">
            public partial class Outer
            {
                public partial class Inner1 { }
                public partial class Inner2 { }
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoNotIncludeWhenAllAreTrivialPartialContainer()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public partial class Outer
            {
                public partial class Inner1 { }
            }
        </Document>
        <Document FilePath=""File2.cs"">
            public partial class Outer
            {
                public partial class Inner2 { }
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new() { },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoIncludeNonTrivialPartialContainer()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public partial class Outer
            {
                public void VisibleMethod() { }
            }
        </Document>
        <Document FilePath=""File2.cs"">
            public partial class Outer
            {
                public void VisibleMethod2() { }
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoIncludeNonTrivialPartialContainerWithNestedType()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public partial class Outer
            {
                public void VisibleMethod() { }
            }
        </Document>
        <Document FilePath=""File2.cs"">
            public partial class Outer
            {
                public void VisibleMethod2() { }
                public class Inner { }
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoIncludePartialWithNoContents()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public partial class Outer
            {
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoIncludeNonPartialOnlyContainingNestedTypes()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
            public class Outer
            {
                public class Inner {}
            }
        </Document>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("Outer"));
        }

        [Fact]
        public async Task DoIncludeSymbolsFromSourceGeneratedFiles()
        {
            using var workspace = TestWorkspace.Create(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <DocumentFromSourceGenerator>
            public class C
            {
            }
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
", composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("C", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("C"));
        }

        [Fact]
        public async Task DoIncludeSymbolsFromMultipleSourceGeneratedFiles()
        {
            using var workspace = TestWorkspace.CreateCSharp(
                files: Array.Empty<string>(),
                sourceGeneratedFiles: new[]
                {
                    @"
public partial class C
{
}",
                    @"
public partial class C
{
}",
                },
                composition: EditorTestCompositions.EditorFeatures);

            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            VerifyNavigateToResultItems(
                new()
                {
                    new NavigateToItem("C", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                    new NavigateToItem("C", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                },
                await _aggregator.GetItemsAsync("C"));
        }

        [Theory, CombinatorialData]
        [WorkItem(59231, "https://github.com/dotnet/roslyn/issues/59231")]
        public async Task FindMethodWithTuple(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"class Goo
{
    public void Method(
        (int x, Dictionary<int,string> y) t1,
        (bool b, global::System.Int32 c) t2)
    {
    }
}", async w =>
{
    var item = (await _aggregator.GetItemsAsync("Method")).Single();
    VerifyNavigateToResultItem(item, "Method", "[|Method|]((int x, Dictionary<int,string> y), (bool b, global::System.Int32 c))", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
});
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(57873, "https://github.com/dotnet/roslyn/issues/57873")]
        public async Task FindRecordMember1(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"record Goo(int Member)
{
}", async w =>
{
    var item = (await _aggregator.GetItemsAsync("Member")).Single(x => x.Kind == NavigateToItemKind.Property);
    VerifyNavigateToResultItem(item, "Member", "[|Member|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic);
});
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(57873, "https://github.com/dotnet/roslyn/issues/57873")]
        public async Task FindRecordMember2(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"record Goo(int Member)
{
    public int Member { get; } = Member;
}", async w =>
{
    var item = (await _aggregator.GetItemsAsync("Member")).Single(x => x.Kind == NavigateToItemKind.Property);
    VerifyNavigateToResultItem(item, "Member", "[|Member|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic);
});
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(57873, "https://github.com/dotnet/roslyn/issues/57873")]
        public async Task FindRecordMember3(TestHost testHost, Composition composition)
        {
            await TestAsync(
testHost, composition, @"record Goo(int Member)
{
    public int Member = Member;
}", async w =>
{
    var item = (await _aggregator.GetItemsAsync("Member")).Single(x => x.Kind == NavigateToItemKind.Field);
    VerifyNavigateToResultItem(item, "Member", "[|Member|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPublic);
});
        }
    }
}
#pragma warning restore CS0618 // MatchKind is obsolete
