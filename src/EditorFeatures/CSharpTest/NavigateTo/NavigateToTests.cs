// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

#pragma warning disable CS0618 // MatchKind is obsolete

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo;

[Trait(Traits.Feature, Traits.Features.NavigateTo)]
public sealed class NavigateToTests : AbstractNavigateToTests
{
    protected override string Language => "csharp";

    protected override EditorTestWorkspace CreateWorkspace(string content, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(content, composition: composition);

    [Theory, CombinatorialData]
    public Task NoItemsForEmptyFile(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "", async w =>
        {
            Assert.Empty(await _aggregator.GetItemsAsync("Hello"));
        });

    [Theory, CombinatorialData]
    public Task FindClass(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public Task FindRecord(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            record Goo
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public Task FindRecordClass(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            record class Goo
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public async Task FindRecordStruct(TestHost testHost, Composition composition)
    {
        var content = XElement.Parse("""
            <Workspace>
                <Project Language="C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath="File1.cs">
            record struct Goo
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """);
        await TestAsync(testHost, composition, content, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
            VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Structure, Glyph.StructureInternal);
        });
    }

    [Theory, CombinatorialData]
    public async Task FindClassInFileScopedNamespace(TestHost testHost, Composition composition)
    {
        var content = XElement.Parse("""
            <Workspace>
                <Project Language="C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath="File1.cs">
            namespace FileScopedNS;
            class Goo { }
                    </Document>
                </Project>
            </Workspace>
            """);
        await TestAsync(testHost, composition, content, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
            VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
        });
    }

    [Theory, CombinatorialData]
    public Task FindVerbatimClass(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class @static
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("static")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "static", "[|static|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);

                // Check searching for @static too
                item = (await _aggregator.GetItemsAsync("@static")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "static", "[|static|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public Task FindNestedClass(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                class Bar
                {
                    internal class DogBed
                    {
                    }
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DogBed")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DogBed", "[|DogBed|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public Task FindMemberInANestedClass(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
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
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Method")).Single();
                VerifyNavigateToResultItem(item, "Method", "[|Method|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo.Bar.DogBed", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindGenericClassWithConstraints(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            using System.Collections;

            class Goo<T> where T : IEnumerable
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]<T>", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public Task FindGenericMethodWithConstraints(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            using System;

            class Goo<U>
            {
                public void Bar<T>(T item) where T : IComparable<T>
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]<T>(T)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo<U>", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindPartialClass(TestHost testHost, Composition composition)
        => TestAsync(
        testHost, composition, """
            public partial class Goo
            {
                int a;
            }

            partial class Goo
            {
                int b;
            }
            """, async w =>
            {
                var items = await _aggregator.GetItemsAsync("Goo");

                var expecteditem1 = new NavigateToItem("Goo", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null);
                VerifyNavigateToResultItems([expecteditem1, expecteditem1], items);
            });

    [Theory, CombinatorialData]
    public Task FindTypesInMetadata(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            using System;

            Class Program { FileStyleUriParser f; }
            """, async w =>
            {
                var items = await _aggregator.GetItemsAsync("FileStyleUriParser");
                Assert.Equal(0, items.Count());
            });

    [Theory, CombinatorialData]
    public Task FindClassInNamespace(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            namespace Bar
            {
                class Goo
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
            });

    [Theory, CombinatorialData]
    public Task FindStruct(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            struct Bar
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Structure, Glyph.StructureInternal);
            });

    [Theory, CombinatorialData]
    public Task FindEnum(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            enum Colors
            {
                Red,
                Green,
                Blue
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Colors")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "Colors", "[|Colors|]", PatternMatchKind.Exact, NavigateToItemKind.Enum, Glyph.EnumInternal);
            });

    [Theory, CombinatorialData]
    public Task FindEnumMember(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            enum Colors
            {
                Red,
                Green,
                Blue
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("R")).Single();
                VerifyNavigateToResultItem(item, "Red", "[|R|]ed", PatternMatchKind.Prefix, NavigateToItemKind.EnumItem, Glyph.EnumMemberPublic);
            });

    [Theory, CombinatorialData]
    public Task FindField1(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                int bar;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("b")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|b|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindField2(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                int bar;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindField3(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                int bar;
            }
            """, async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("ar"));
            });

    [Theory, CombinatorialData]
    public Task FindVerbatimField(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                int @string;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));

                // Check searching for@string too
                item = (await _aggregator.GetItemsAsync("@string")).Single();
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindPtrField1(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                int* bar;
            }
            """, async w =>
            {
                Assert.Empty(await _aggregator.GetItemsAsync("ar"));
            });

    [Theory, CombinatorialData]
    public Task FindPtrField2(TestHost testHost, Composition composition)
        => TestAsync(
        testHost, composition, """
            class Goo
            {
                int* bar;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("b")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|b|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindConstField(TestHost testHost, Composition composition)
        => TestAsync(
        testHost, composition, """
            class Goo
            {
                const int bar = 7;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ba")).Single();
                VerifyNavigateToResultItem(item, "bar", "[|ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Constant, Glyph.ConstantPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindIndexer(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, @"class Goo { int[] arr; public int this[int i] { get { return arr[i]; } set { arr[i] = value; } } }", async w =>
        {
            var item = (await _aggregator.GetItemsAsync("this")).Single();
            VerifyNavigateToResultItem(item, "this", "[|this|][int]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
        });

    [Theory, CombinatorialData]
    public Task FindEvent(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class Goo { public event EventHandler ChangedEventHandler; }", async w =>
        {
            var item = (await _aggregator.GetItemsAsync("CEH")).Single();
            VerifyNavigateToResultItem(item, "ChangedEventHandler", "[|C|]hanged[|E|]vent[|H|]andler", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Event, Glyph.EventPublic, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
        });

    [Theory, CombinatorialData]
    public Task FindAutoProperty(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                int Bar { get; set; }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("B")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Property, Glyph.PropertyPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindMethod(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                void DoSomething();
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindVerbatimMethod(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                void @static();
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("static")).Single();
                VerifyNavigateToResultItem(item, "static", "[|static|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));

                // Verify if we search for @static too
                item = (await _aggregator.GetItemsAsync("@static")).Single();
                VerifyNavigateToResultItem(item, "static", "[|static|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindParameterizedMethod(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                void DoSomething(int a, string b)
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DS")).Single();
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething(int, string)", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindConstructor(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                public Goo()
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindParameterizedConstructor(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                public Goo(int i)
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method);
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](int)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindStaticConstructor(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                static Goo()
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single(t => t.Kind == NavigateToItemKind.Method && t.Name != ".ctor");
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].static Goo()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindPartialMethods(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "partial class Goo { partial void Bar(); } partial class Goo { partial void Bar() { Console.Write(\"hello\"); } }", async w =>
        {
            var expecteditem1 = new NavigateToItem("Bar", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null);

            var items = await _aggregator.GetItemsAsync("Bar");

            VerifyNavigateToResultItems([expecteditem1, expecteditem1], items);
        });

    [Theory, CombinatorialData]
    public Task FindPartialProperties(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "partial class Goo { partial int Prop { get; set; } } partial class Goo { partial int Prop { get => 1; set { } } }", async w =>
        {
            var expecteditem1 = new NavigateToItem("Prop", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null);

            var items = await _aggregator.GetItemsAsync("Prop");

            VerifyNavigateToResultItems([expecteditem1, expecteditem1], items);
        });

    [Theory, CombinatorialData]
    public Task FindPartialEvents(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
        partial class C
        {
            partial event System.Action E;
            partial event System.Action E { add { } remove { } }
        }
        """, async w =>
        {
            var expecteditem1 = new NavigateToItem("E", NavigateToItemKind.Event, "csharp", null, null, s_emptyExactPatternMatch, null);
            var items = await _aggregator.GetItemsAsync("E");

            VerifyNavigateToResultItems([expecteditem1, expecteditem1], items);
        });

    [Theory, CombinatorialData]
    public Task FindPartialConstructors(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
        partial class C
        {
            public partial C();
            public partial C() { }
        }
        """, async w =>
        {
            var expecteditem1 = new NavigateToItem("C", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null);
            var items = (await _aggregator.GetItemsAsync("C")).Where(t => t.Kind == NavigateToItemKind.Method);

            VerifyNavigateToResultItems([expecteditem1, expecteditem1], items);
        });

    [Theory, CombinatorialData]
    public Task FindPartialMethodDefinitionOnly(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            partial class Goo
            {
                partial void Bar();
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_1_2, "Goo", "test1.cs", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindPartialMethodImplementationOnly(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            partial class Goo
            {
                partial void Bar()
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, string.Format(FeaturesResources.in_0_1_2, "Goo", "test1.cs", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindOverriddenMembers(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class Goo { public virtual string Name { get; set; } } class DogBed : Goo { public override string Name { get { return base.Name; } set {} } }", async w =>
        {
            var expecteditem1 = new NavigateToItem("Name", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null);

            var items = await _aggregator.GetItemsAsync("Name");

            VerifyNavigateToResultItems([expecteditem1, expecteditem1], items);

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

    [Theory, CombinatorialData]
    public Task FindInterface(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            public interface IGoo
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("IG")).Single();
                VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", PatternMatchKind.Prefix, NavigateToItemKind.Interface, Glyph.InterfacePublic);
            });

    [Theory, CombinatorialData]
    public Task FindTopLevelLocalFunction(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            void Goo()
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single();
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindTopLevelLocalFunction_WithParameters(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            void Goo(int i)
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single();
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](int)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindTopLevelLocalFunction_WithTypeArgumentsAndParameters(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            void Goo<T>(int i)
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single();
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]<T>(int)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindNestedLocalFunctionTopLevelStatements(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            void Goo()
            {
                void Bar()
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindLocalFunctionInMethod(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class C
            {
                void M()
                {
                    void Goo()
                    {
                        void Bar()
                        {
                        }
                    }
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Goo")).Single();
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindNestedLocalFunctionInMethod(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class C
            {
                void M()
                {
                    void Goo()
                    {
                        void Bar()
                        {
                        }
                    }
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Bar")).Single();
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task FindDelegateInNamespace(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            namespace Goo
            {
                delegate void DoStuff();
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("DoStuff")).Single(x => x.Kind != "Method");
                VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", PatternMatchKind.Exact, NavigateToItemKind.Delegate, Glyph.DelegateInternal);
            });

    [Theory, CombinatorialData]
    public Task FindLambdaExpression(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            using System;

            class Goo
            {
                Func<int, int> sqr = x => x * x;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("sqr")).Single();
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindArray(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                object[] itemArray;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("itemArray")).Single();
                VerifyNavigateToResultItem(item, "itemArray", "[|itemArray|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    public Task FindClassAndMethodWithSameName(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
            }

            class Test
            {
                void Goo()
                {
                }
            }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("Goo");
            VerifyNavigateToResultItems([
                new("Goo", NavigateToItemKind.Class, "csharp", "Goo", null, s_emptyExactPatternMatch, null),
                new("Goo", NavigateToItemKind.Method, "csharp", "Goo", null, s_emptyExactPatternMatch, null)], items);
        });

    [Theory, CombinatorialData]
    public Task FindMethodNestedInGenericTypes(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class A<T>
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
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("M")).Single();
                VerifyNavigateToResultItem(item, "M", "[|M|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, additionalInfo: string.Format(FeaturesResources.in_0_project_1, "A<T>.B.C<U>", "Test"));
            });

    [Theory, CombinatorialData]
    public Task OrderingOfConstructorsAndTypes(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class C1
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
            }
            """, async w =>
            {
                var items = (await _aggregator.GetItemsAsync("C")).ToList();
                items.Sort(CompareNavigateToItems);
                VerifyNavigateToResultItems([
                    new("C1", NavigateToItemKind.Class, "csharp", "C1", null, s_emptyPrefixPatternMatch, null),
                    new("C1", NavigateToItemKind.Method, "csharp", "C1", null, s_emptyPrefixPatternMatch, null),
                    new("C2", NavigateToItemKind.Class, "csharp", "C2", null, s_emptyPrefixPatternMatch, null),
                    new("C2", NavigateToItemKind.Method, "csharp", "C2", null, s_emptyPrefixPatternMatch, null), // this is the static ctor
                    new("C2", NavigateToItemKind.Method, "csharp", "C2", null, s_emptyPrefixPatternMatch, null)], items);
            });

    [Theory, CombinatorialData]
    public Task NavigateToMethodWithNullableParameter(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class C
            {
                void M(object? o)
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("M")).Single();
                VerifyNavigateToResultItem(item, "M", "[|M|](object?)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate);
            });

    [Theory, CombinatorialData]
    public Task StartStopSanity(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            public class Goo
            {
            }
            """, async w =>
            {
                // Do one set of queries
                Assert.Single((await _aggregator.GetItemsAsync("Goo")), x => x.Kind != "Method");
                _provider.StopSearch();

                // Do the same query again, make sure nothing was left over
                Assert.Single((await _aggregator.GetItemsAsync("Goo")), x => x.Kind != "Method");
                _provider.StopSearch();

                // Dispose the provider
                _provider.Dispose();
            });

    [Theory, CombinatorialData]
    public Task DescriptionItems(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            public
            class
            Goo
            { }
            """, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("G")).Single(x => x.Kind != "Method");
            var itemDisplay = item.DisplayFactory.CreateItemDisplay(item);

            var descriptionItems = itemDisplay.DescriptionItems;

            void assertDescription(string label, string? value)
            {
                var descriptionItem = descriptionItems.Single(i => i.Category.Single().Text == label);
                Assert.Equal(value, descriptionItem.Details.Single().Text);
            }

            assertDescription("File:", w.Documents.Single().FilePath);
            assertDescription("Line:", "3"); // one based line number
            assertDescription("Project:", "Test");
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest1(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var expecteditem1 = new NavigateToItem("get_keyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
            var expecteditem2 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
            var expecteditem3 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCasePrefixPatternMatch, null);

            var items = await _aggregator.GetItemsAsync("GK");

            VerifyNavigateToResultItems([expecteditem1, expecteditem2, expecteditem3], items);
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest2(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive, null);
            var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseExactPatternMatch, null);

            var items = await _aggregator.GetItemsAsync("GKW");

            VerifyNavigateToResultItems([expecteditem1, expecteditem2], items);
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest3(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var expecteditem1 = new NavigateToItem("get_key_word", NavigateToItemKind.Field, "csharp", null, null, s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive, null);
            var expecteditem2 = new NavigateToItem("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch, null);

            var items = await _aggregator.GetItemsAsync("K W");

            VerifyNavigateToResultItems([expecteditem1, expecteditem2], items);
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest4(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var items = await _aggregator.GetItemsAsync("WKG");
            Assert.Empty(items);
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest5(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var item = (await _aggregator.GetItemsAsync("G_K_W")).Single();
            VerifyNavigateToResultItem(item, "get_key_word", "[|g|]et[|_k|]ey[|_w|]ord", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Field, Glyph.FieldPrivate);
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest6(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var items = await _aggregator.GetItemsAsync("get word");

            VerifyNavigateToResultItems([
                new("getkeyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyFuzzyPatternMatch, null),
                new("get_keyword", NavigateToItemKind.Field, "csharp", null, null, s_emptyFuzzyPatternMatch, null),
                new("get_key_word", NavigateToItemKind.Field, "csharp", null, null,s_emptySubstringPatternMatch, null),
                new("GetKeyWord", NavigateToItemKind.Field, "csharp", null, null, s_emptySubstringPatternMatch_NotCaseSensitive, null)], items);
        });

    [Theory, CombinatorialData]
    public Task TermSplittingTest7(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "class SyllableBreaking {int GetKeyWord; int get_key_word; string get_keyword; int getkeyword; int wake;}", async w =>
        {
            var items = await _aggregator.GetItemsAsync("GTW");
            Assert.Empty(items);
        });

    [Theory, CombinatorialData]
    public Task TestIndexer1(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            class C
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
            }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("this");

            VerifyNavigateToResultItems([new("this", NavigateToItemKind.Property, "csharp", null, null, s_emptyExactPatternMatch, null)], items);
        });

    [Theory, CombinatorialData]
    public Task DottedPattern1(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("B.Q");

            VerifyNavigateToResultItems([new("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)], items);
        });

    [Theory, CombinatorialData]
    public Task DottedPattern2(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("C.Q");

            VerifyNavigateToResultItems([], items);
        });

    [Theory, CombinatorialData]
    public Task DottedPattern3(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("B.B.Q");

            VerifyNavigateToResultItems([new("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)], items);
        });

    [Theory, CombinatorialData]
    public Task DottedPattern4(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("Baz.Quux");

            VerifyNavigateToResultItems([new("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);
        });

    [Theory, CombinatorialData]
    public Task DottedPattern5(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("G.B.B.Quux");

            VerifyNavigateToResultItems([new("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);
        });

    [Theory, CombinatorialData]
    public Task DottedPattern6(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("F.F.B.B.Quux");

            VerifyNavigateToResultItems([], items);
        });

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/Roslyn/issues/7855")]
    public Task DottedPattern7(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace Goo { namespace Bar { class Baz<X,Y,Z> { void Quux() { } } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("Baz.Q");

            VerifyNavigateToResultItems([new("Quux", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)], items);
        });

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/46267")]
    [CombinatorialData]
    public Task DottedPatternMatchKind(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, "namespace System { class Console { void Write(string s) { } void WriteLine(string s) { } } }", async w =>
        {
            var items = await _aggregator.GetItemsAsync("Console.Write");

            VerifyNavigateToResultItems([
                new("Write", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                new("WriteLine", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)], items);
        });

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174255")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8009")]
    public async Task NavigateToGeneratedFiles()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        namespace N
                        {
                            public partial class C
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                    <Document FilePath="File1.g.cs">
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
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        var items = await _aggregator.GetItemsAsync("VisibleMethod");

        // The pattern matcher should match 'VisibleMethod' to both 'VisibleMethod' and 'VisibleMethod_Not', except that
        // the _Not method is declared in a generated file.
        VerifyNavigateToResultItems([
            new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
            new("VisibleMethod_Generated", NavigateToItemKind.Method, "csharp", null, null, s_emptyPrefixPatternMatch, null)], items);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/pull/11474")]
    [CombinatorialData]
    public Task FindFuzzy1(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class C
            {
                public void ToError()
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ToEror")).Single();
                VerifyNavigateToResultItem(item, "ToError", "ToError()", PatternMatchKind.Fuzzy, NavigateToItemKind.Method, Glyph.MethodPublic);
            });

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/18843")]
    [CombinatorialData]
    public Task Test__arglist(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class C
            {
                public void ToError(__arglist)
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("ToError")).Single();
                VerifyNavigateToResultItem(item, "ToError", "[|ToError|](__arglist)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic);
            });

    [Fact]
    public async Task DoNotIncludeTrivialPartialContainer()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public partial class Outer
                        {
                            public void VisibleMethod() { }
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        public partial class Outer
                        {
                            public partial class Inner { }
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoNotIncludeTrivialPartialContainerWithMultipleNestedTypes()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public partial class Outer
                        {
                            public void VisibleMethod() { }
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        public partial class Outer
                        {
                            public partial class Inner1 { }
                            public partial class Inner2 { }
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoNotIncludeWhenAllAreTrivialPartialContainer()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public partial class Outer
                        {
                            public partial class Inner1 { }
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        public partial class Outer
                        {
                            public partial class Inner2 { }
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoIncludeNonTrivialPartialContainer()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public partial class Outer
                        {
                            public void VisibleMethod() { }
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        public partial class Outer
                        {
                            public void VisibleMethod2() { }
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoIncludeNonTrivialPartialContainerWithNestedType()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public partial class Outer
                        {
                            public void VisibleMethod() { }
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        public partial class Outer
                        {
                            public void VisibleMethod2() { }
                            public class Inner { }
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoIncludePartialWithNoContents()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public partial class Outer
                        {
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoIncludeNonPartialOnlyContainingNestedTypes()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public class Outer
                        {
                            public class Inner {}
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("Outer", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("Outer"));
    }

    [Fact]
    public async Task DoIncludeSymbolsFromSourceGeneratedFiles()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <DocumentFromSourceGenerator>
                        public class C
                        {
                        }
                    </DocumentFromSourceGenerator>
                </Project>
            </Workspace>
            """, composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("C", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("C"));
    }

    [Fact]
    public async Task DoIncludeSymbolsFromMultipleSourceGeneratedFiles()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(
            files: [],
            sourceGeneratedFiles:
            [
                """
                public partial class C
                {
                }
                """,
                """
                public partial class C
                {
                }
                """,
            ],
            composition: DefaultComposition);

        _provider = CreateProvider(workspace);
        _aggregator = new NavigateToTestAggregator(_provider);

        VerifyNavigateToResultItems(
            [
                new NavigateToItem("C", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
                new NavigateToItem("C", NavigateToItemKind.Class, "csharp", null, null, s_emptyExactPatternMatch, null),
            ],
            await _aggregator.GetItemsAsync("C"));
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/59231")]
    public Task FindMethodWithTuple(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            class Goo
            {
                public void Method(
                    (int x, Dictionary<int,string> y) t1,
                    (bool b, global::System.Int32 c) t2)
                {
                }
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Method")).Single();
                VerifyNavigateToResultItem(item, "Method", "[|Method|]((int x, Dictionary<int,string> y), (bool b, global::System.Int32 c))", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, string.Format(FeaturesResources.in_0_project_1, "Goo", "Test"));
            });

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/57873")]
    public Task FindRecordMember1(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            record Goo(int Member)
            {
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Member")).Single(x => x.Kind == NavigateToItemKind.Property);
                VerifyNavigateToResultItem(item, "Member", "[|Member|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic);
            });

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/57873")]
    public Task FindRecordMember2(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            record Goo(int Member)
            {
                public int Member { get; } = Member;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Member")).Single(x => x.Kind == NavigateToItemKind.Property);
                VerifyNavigateToResultItem(item, "Member", "[|Member|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic);
            });

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/57873")]
    public Task FindRecordMember3(TestHost testHost, Composition composition)
        => TestAsync(
            testHost, composition, """
            record Goo(int Member)
            {
                public int Member = Member;
            }
            """, async w =>
            {
                var item = (await _aggregator.GetItemsAsync("Member")).Single(x => x.Kind == NavigateToItemKind.Field);
                VerifyNavigateToResultItem(item, "Member", "[|Member|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPublic);
            });

    private static bool IsFromFile(NavigateToItem item, string fileName)
    {
        return ((CodeAnalysis.NavigateTo.INavigateToSearchResult)item.Tag).NavigableItem.Document.Name == fileName;
    }

    [Theory, CombinatorialData]
    public Task NavigateToPrioritizeResultInCurrentDocument1(TestHost testHost)
        => TestAsync(testHost, Composition.FirstActiveAndVisible, XElement.Parse("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        namespace N
                        {
                            public class C
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        namespace N
                        {
                            public class D
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                </Project>
            </Workspace>
            """), async workspace =>
        {
            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            var items = await _aggregator.GetItemsAsync("VisibleMethod");

            VerifyNavigateToResultItems([
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);

            Assert.Single(items, i => i.SecondarySort.StartsWith("0000") && IsFromFile(i, "File1.cs"));
            Assert.Single(items, i => i.SecondarySort.StartsWith("0001") && IsFromFile(i, "File2.cs"));
        });

    [Theory, CombinatorialData]
    public Task NavigateToPrioritizeResultInCurrentDocument2(TestHost testHost)
        => TestAsync(testHost, Composition.FirstActiveAndVisible, XElement.Parse("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs" Folders="A\B\C">
                        namespace N
                        {
                            public class C
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                    <Document FilePath="File2.cs" Folders="A\B\C">
                        namespace N
                        {
                            public class D
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                </Project>
            </Workspace>
            """), async workspace =>
        {
            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            var items = await _aggregator.GetItemsAsync("VisibleMethod");

            VerifyNavigateToResultItems([
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);

            Assert.Single(items, i => i.SecondarySort.StartsWith("0000") && IsFromFile(i, "File1.cs"));
            Assert.Single(items, i => i.SecondarySort.StartsWith("0001") && IsFromFile(i, "File2.cs"));
        });

    [Theory, CombinatorialData]
    public Task NavigateToPrioritizeResultInCurrentDocument3(TestHost testHost)
        => TestAsync(testHost, Composition.FirstActiveAndVisible, XElement.Parse("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs" Folders="A\B\C\D">
                        namespace N
                        {
                            public class C
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                    <Document FilePath="File2.cs" Folders="A\B\C">
                        namespace N
                        {
                            public class D
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                </Project>
            </Workspace>
            """), async workspace =>
        {
            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            var items = await _aggregator.GetItemsAsync("VisibleMethod");

            VerifyNavigateToResultItems([
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);

            Assert.Single(items, i => i.SecondarySort.StartsWith("0000") && IsFromFile(i, "File1.cs"));
            Assert.Single(items, i => i.SecondarySort.StartsWith("0002") && IsFromFile(i, "File2.cs"));
        });

    [Theory, CombinatorialData]
    public Task NavigateToPrioritizeResultInCurrentDocument4(TestHost testHost)
        => TestAsync(testHost, Composition.FirstActiveAndVisible, XElement.Parse("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs" Folders="A\B\C">
                        namespace N
                        {
                            public class C
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                    <Document FilePath="File2.cs" Folders="A\B\C\D">
                        namespace N
                        {
                            public class D
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                </Project>
            </Workspace>
            """), async workspace =>
        {
            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            var items = await _aggregator.GetItemsAsync("VisibleMethod");

            VerifyNavigateToResultItems([
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);

            Assert.Single(items, i => i.SecondarySort.StartsWith("0000") && IsFromFile(i, "File1.cs"));
            Assert.Single(items, i => i.SecondarySort.StartsWith("0002") && IsFromFile(i, "File2.cs"));
        });

    [Theory, CombinatorialData]
    public Task NavigateToPrioritizeResultInCurrentDocument5(TestHost testHost)
        => TestAsync(testHost, Composition.FirstActiveAndVisible, XElement.Parse("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs" Folders="A\B\C\D1">
                        namespace N
                        {
                            public class C
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                    <Document FilePath="File2.cs" Folders="A\B\C\D2">
                        namespace N
                        {
                            public class D
                            {
                                public void VisibleMethod() { }
                            }
                        }
                    </Document>
                </Project>
            </Workspace>
            """), async workspace =>
        {
            _provider = CreateProvider(workspace);
            _aggregator = new NavigateToTestAggregator(_provider);

            var items = await _aggregator.GetItemsAsync("VisibleMethod");

            VerifyNavigateToResultItems([
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null),
                new("VisibleMethod", NavigateToItemKind.Method, "csharp", null, null, s_emptyExactPatternMatch, null)], items);

            Assert.Single(items, i => i.SecondarySort.StartsWith("0000") && IsFromFile(i, "File1.cs"));
            Assert.Single(items, i => i.SecondarySort.StartsWith("0003") && IsFromFile(i, "File2.cs"));
        });

    [Theory, CombinatorialData]
    public async Task FindModernExtensionMethod1(TestHost testHost, Composition composition)
    {
        var content = XElement.Parse("""
            <Workspace>
                <Project Language="C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath="File1.cs">
            static class Class
            {
                extension(string s)
                {
                    public void Goo()
                    {
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);
        await TestAsync(testHost, composition, content, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("Goo")).Single();
            VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic);
        });
    }

    [Theory, CombinatorialData]
    public async Task FindModernExtensionMethod2(TestHost testHost, Composition composition)
    {
        var content = XElement.Parse("""
            <Workspace>
                <Project Language="C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath="File1.cs">
            static class Class
            {
                extension(string s)
                {
                    public static void Goo()
                    {
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);
        await TestAsync(testHost, composition, content, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("Goo")).Single();
            VerifyNavigateToResultItem(item, "Goo", "[|Goo|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic);
        });
    }

    [Theory, CombinatorialData]
    public async Task FindModernExtensionProperty(TestHost testHost, Composition composition)
    {
        var content = XElement.Parse("""
            <Workspace>
                <Project Language="C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath="File1.cs">
            static class Class
            {
                extension(string s)
                {
                    public int Goo => 0;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);
        await TestAsync(testHost, composition, content, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("Goo")).Single();
            VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic);
        });
    }
}
#pragma warning restore CS0618 // MatchKind is obsolete
