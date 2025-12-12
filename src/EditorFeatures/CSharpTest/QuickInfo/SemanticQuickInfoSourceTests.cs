// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo;

[Trait(Traits.Feature, Traits.Features.QuickInfo)]
public sealed class SemanticQuickInfoSourceTests : AbstractSemanticQuickInfoSourceTests
{
    private static async Task TestWithOptionsAsync(
        CSharpParseOptions options,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(markup, options);
        await TestWithOptionsAsync(workspace, expectedResults);
    }

    private static async Task TestWithOptionsAsync(
        CSharpCompilationOptions options,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(markup, compilationOptions: options);
        await TestWithOptionsAsync(workspace, expectedResults);
    }

    private static async Task TestWithOptionsAsync(EditorTestWorkspace workspace, params Action<QuickInfoItem>[] expectedResults)
    {
        var testDocument = workspace.DocumentWithCursor;
        var position = testDocument.CursorPosition.GetValueOrDefault();
        var documentId = testDocument.Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId);

        var service = QuickInfoService.GetService(document);
        Contract.ThrowIfNull(service);

        await TestWithOptionsAsync(document, service, position, expectedResults);

        // speculative semantic model
        if (await CanUseSpeculativeSemanticModelAsync(document, position))
        {
            var buffer = testDocument.GetTextBuffer();
            using (var edit = buffer.CreateEdit())
            {
                var currentSnapshot = buffer.CurrentSnapshot;
                edit.Replace(0, currentSnapshot.Length, currentSnapshot.GetText());
                edit.Apply();
            }

            await TestWithOptionsAsync(document, service, position, expectedResults);
        }
    }

    private static async Task TestWithOptionsAsync(Document document, QuickInfoService service, int position, Action<QuickInfoItem>[] expectedResults)
    {
        var info = await service.GetQuickInfoAsync(document, position, SymbolDescriptionOptions.Default, CancellationToken.None);

        if (expectedResults.Length == 0)
        {
            Assert.Null(info);
        }
        else
        {
            AssertEx.NotNull(info);

            foreach (var expected in expectedResults)
            {
                expected(info);
            }
        }
    }

    private static async Task VerifyWithMscorlib45Async(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet45="true">
                    <Document FilePath="SourceDocument">
            {0}
                    </Document>
                </Project>
            </Workspace>
            """, SecurityElement.Escape(markup));

        await VerifyWithMarkupAsync(xmlString, expectedResults);
    }

    private static async Task VerifyWithNet8Async(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet8="true">
                    <Document FilePath="SourceDocument">
            {0}
                    </Document>
                </Project>
            </Workspace>
            """, SecurityElement.Escape(markup));

        await VerifyWithMarkupAsync(xmlString, expectedResults);
    }

    private static async Task VerifyWithMarkupAsync(string xmlString, Action<QuickInfoItem>[] expectedResults)
    {
        using var workspace = EditorTestWorkspace.Create(xmlString);
        var sourceDocument = workspace.Documents.Single(d => d.Name == "SourceDocument");
        var position = sourceDocument.CursorPosition!.Value;
        var documentId = sourceDocument.Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId);

        var service = QuickInfoService.GetService(document);
        Contract.ThrowIfNull(service);

        var info = await service.GetQuickInfoAsync(document, position, SymbolDescriptionOptions.Default, CancellationToken.None);

        if (expectedResults.Length == 0)
        {
            Assert.Null(info);
        }
        else
        {
            AssertEx.NotNull(info);

            foreach (var expected in expectedResults)
            {
                expected(info);
            }
        }
    }

    protected override async Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        await TestWithOptionsAsync(Options.Regular, markup, expectedResults);
        await TestWithOptionsAsync(Options.Script, markup, expectedResults);
    }

    private async Task TestWithUsingsAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var markupWithUsings =
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            """ + markup;

        await TestAsync(markupWithUsings, expectedResults);
    }

    private Task TestInClassAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var markupInClass = "class C { " + markup + " }";
        return TestWithUsingsAsync(markupInClass, expectedResults);
    }

    private Task TestInMethodAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var markupInMethod = "class C { void M() { " + markup + " } }";
        return TestWithUsingsAsync(markupInMethod, expectedResults);
    }

    private Task TestInMethodAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string extraSource,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var markupInMethod = "class C { void M() { " + markup + " } }" + extraSource;
        return TestWithUsingsAsync(markupInMethod, expectedResults);
    }

    private static async Task TestWithReferenceAsync(
        string sourceCode,
        string referencedCode,
        string sourceLanguage,
        string referencedLanguage,
        params Action<QuickInfoItem>[] expectedResults)
    {
        await TestWithMetadataReferenceHelperAsync(sourceCode, referencedCode, sourceLanguage, referencedLanguage, expectedResults);
        await TestWithProjectReferenceHelperAsync(sourceCode, referencedCode, sourceLanguage, referencedLanguage, expectedResults);

        // Multi-language projects are not supported.
        if (sourceLanguage == referencedLanguage)
        {
            await TestInSameProjectHelperAsync(sourceCode, referencedCode, sourceLanguage, expectedResults);
        }
    }

    private static async Task TestWithMetadataReferenceHelperAsync(
        string sourceCode,
        string referencedCode,
        string sourceLanguage,
        string referencedLanguage,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                    <MetadataReferenceFromSource Language="{2}" CommonReferences="true" IncludeXmlDocComments="true">
                        <Document FilePath="ReferencedDocument">
            {3}
                        </Document>
                    </MetadataReferenceFromSource>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode),
           referencedLanguage, SecurityElement.Escape(referencedCode));

        await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
    }

    private static async Task TestWithProjectReferenceHelperAsync(
        string sourceCode,
        string referencedCode,
        string sourceLanguage,
        string referencedLanguage,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <ProjectReference>ReferencedProject</ProjectReference>
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                </Project>
                <Project Language="{2}" CommonReferences="true" AssemblyName="ReferencedProject">
                    <Document FilePath="ReferencedDocument">
            {3}
                    </Document>
                </Project>
                
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode),
           referencedLanguage, SecurityElement.Escape(referencedCode));

        await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
    }

    private static async Task TestInSameProjectHelperAsync(
        string sourceCode,
        string referencedCode,
        string sourceLanguage,
        params Action<QuickInfoItem>[] expectedResults)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                    <Document FilePath="ReferencedDocument">
            {2}
                    </Document>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode), SecurityElement.Escape(referencedCode));

        await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
    }

    private static async Task VerifyWithReferenceWorkerAsync(string xmlString, params Action<QuickInfoItem>[] expectedResults)
    {
        using var workspace = EditorTestWorkspace.Create(xmlString);
        var sourceDocument = workspace.Documents.First(d => d.Name == "SourceDocument");
        var position = sourceDocument.CursorPosition!.Value;
        var documentId = sourceDocument.Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId);

        var service = QuickInfoService.GetService(document);
        Contract.ThrowIfNull(service);

        var info = await service.GetQuickInfoAsync(document, position, SymbolDescriptionOptions.Default, CancellationToken.None);

        if (expectedResults.Length == 0)
        {
            Assert.Null(info);
        }
        else
        {
            AssertEx.NotNull(info);

            foreach (var expected in expectedResults)
            {
                expected(info);
            }
        }
    }

    private async Task TestInvalidTypeInClassAsync(string code)
    {
        var codeInClass = "class C { " + code + " }";
        await TestAsync(codeInClass);
    }

    [Fact]
    public Task TestNamespaceInUsingDirective()
        => TestAsync(
            @"using $$System;",
            MainDescription("namespace System"));

    [Fact]
    public Task TestNamespaceInUsingDirective2()
        => TestAsync(
            @"using System.Coll$$ections.Generic;",
            MainDescription("namespace System.Collections"));

    [Fact]
    public Task TestNamespaceInUsingDirective3()
        => TestAsync(
            @"using System.L$$inq;",
            MainDescription("namespace System.Linq"));

    [Fact]
    public Task TestNamespaceInUsingDirectiveWithAlias()
        => TestAsync(
            @"using Goo = Sys$$tem.Console;",
            MainDescription("namespace System"));

    [Fact]
    public Task TestTypeInUsingDirectiveWithAlias()
        => TestAsync(
            @"using Goo = System.Con$$sole;",
            MainDescription("class System.Console"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")]
    public Task TestDocumentationInUsingDirectiveWithAlias()
        => TestAsync("""
            using I$$ = IGoo;
            ///<summary>summary for interface IGoo</summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation("summary for interface IGoo"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")]
    public Task TestDocumentationInUsingDirectiveWithAlias2()
        => TestAsync("""
            using I = IGoo;
            ///<summary>summary for interface IGoo</summary>
            interface IGoo {  }
            class C : I$$ { }
            """,
            MainDescription("interface IGoo"),
            Documentation("summary for interface IGoo"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")]
    public Task TestDocumentationInUsingDirectiveWithAlias3()
        => TestAsync("""
            using I = IGoo;
            ///<summary>summary for interface IGoo</summary>
            interface IGoo 
            {  
                void Goo();
            }
            class C : I$$ { }
            """,
            MainDescription("interface IGoo"),
            Documentation("summary for interface IGoo"));

    [Fact]
    public Task TestThis()
        => TestWithUsingsAsync("""

            ///<summary>summary for Class C</summary>
            class C { string M() {  return thi$$s.ToString(); } }
            """,
            MainDescription("class C"),
            Documentation("summary for Class C"));

    [Fact]
    public Task TestClassWithDocComment()
        => TestAsync("""

            ///<summary>Hello!</summary>
            class C { void M() { $$C obj; } }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

    [Fact]
    public async Task TestSingleLineDocComments()
    {
        // Tests chosen to maximize code coverage in DocumentationCommentCompiler.WriteFormattedSingleLineComment

        // SingleLine doc comment with leading whitespace
        await TestAsync(
            """
            ///<summary>Hello!</summary>
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // SingleLine doc comment with space before opening tag
        await TestAsync(
            """
            /// <summary>Hello!</summary>
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // SingleLine doc comment with space before opening tag and leading whitespace
        await TestAsync(
            """
            /// <summary>Hello!</summary>
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // SingleLine doc comment with leading whitespace and blank line
        await TestAsync(
            """
            ///<summary>Hello!
            ///</summary>

            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // SingleLine doc comment with '\r' line separators
        await TestAsync("""
            ///<summary>Hello!
            ///</summary>
            class C { void M() { $$C obj; } }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));
    }

    [Fact]
    public async Task TestMultiLineDocComments()
    {
        // Tests chosen to maximize code coverage in DocumentationCommentCompiler.WriteFormattedMultiLineComment

        // Multiline doc comment with leading whitespace
        await TestAsync(
            """
            /**<summary>Hello!</summary>*/
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // Multiline doc comment with space before opening tag
        await TestAsync(
            """
            /** <summary>Hello!</summary>
             **/
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // Multiline doc comment with space before opening tag and leading whitespace
        await TestAsync(
            """
            /**
             ** <summary>Hello!</summary>
             **/
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // Multiline doc comment with no per-line prefix
        await TestAsync(
            """
            /**
              <summary>
              Hello!
              </summary>
            */
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // Multiline doc comment with inconsistent per-line prefix
        await TestAsync(
            """
            /**
             ** <summary>
                Hello!</summary>
             **
             **/
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // Multiline doc comment with closing comment on final line
        await TestAsync(
            """
            /**
            <summary>Hello!
            </summary>*/
            class C
            {
                void M()
                {
                    $$C obj;
                }
            }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));

        // Multiline doc comment with '\r' line separators
        await TestAsync("""
            /**
            * <summary>
            * Hello!
            * </summary>
            */
            class C { void M() { $$C obj; } }
            """,
            MainDescription("class C"),
            Documentation("Hello!"));
    }

    [Fact]
    public Task TestMethodWithDocComment()
        => TestInClassAsync("""

            ///<summary>Hello!</summary>
            void M() { M$$() }
            """,
            MainDescription("void C.M()"),
            Documentation("Hello!"));

    [Fact]
    public Task TestInt32()
        => TestInClassAsync(
            @"$$Int32 i;",
            MainDescription("struct System.Int32"));

    [Fact]
    public Task TestBuiltInInt()
        => TestInClassAsync(
            @"$$int i;",
            MainDescription("struct System.Int32"));

    [Fact]
    public Task TestString()
        => TestInClassAsync(
            @"$$String s;",
            MainDescription("class System.String"));

    [Fact]
    public Task TestBuiltInString()
        => TestInClassAsync(
            @"$$string s;",
            MainDescription("class System.String"));

    [Fact]
    public Task TestBuiltInStringAtEndOfToken()
        => TestInClassAsync(
            @"string$$ s;",
            MainDescription("class System.String"));

    [Fact]
    public Task TestBoolean()
        => TestInClassAsync(
            @"$$Boolean b;",
            MainDescription("struct System.Boolean"));

    [Fact]
    public Task TestBuiltInBool()
        => TestInClassAsync(
            @"$$bool b;",
            MainDescription("struct System.Boolean"));

    [Fact]
    public Task TestSingle()
        => TestInClassAsync(
            @"$$Single s;",
            MainDescription("struct System.Single"));

    [Fact]
    public Task TestBuiltInFloat()
        => TestInClassAsync(
            @"$$float f;",
            MainDescription("struct System.Single"));

    [Fact]
    public Task TestVoidIsInvalid()
        => TestInvalidTypeInClassAsync(
            """
            $$void M()
            {
            }
            """);

    [Fact]
    public Task TestInvalidPointer1_931958()
        => TestInvalidTypeInClassAsync(
            @"$$T* i;");

    [Fact]
    public Task TestInvalidPointer2_931958()
        => TestInvalidTypeInClassAsync(
            @"T$$* i;");

    [Fact]
    public Task TestInvalidPointer3_931958()
        => TestInvalidTypeInClassAsync(
            @"T*$$ i;");

    [Fact]
    public Task TestListOfString()
        => TestInClassAsync(
            @"$$List<string> l;",
            MainDescription("class System.Collections.Generic.List<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} string
                """));

    [Fact]
    public Task TestListOfSomethingFromSource()
        => TestAsync("""

            ///<summary>Generic List</summary>
            public class GenericList<T> { Generic$$List<int> t; }
            """,
            MainDescription("class GenericList<T>"),
            Documentation("Generic List"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} int
                """));

    [Fact]
    public Task TestListOfT()
        => TestInMethodAsync(
            """
            class C<T>
            {
                $$List<T> l;
            }
            """,
            MainDescription("class System.Collections.Generic.List<T>"));

    [Fact]
    public Task TestDictionaryOfIntAndString()
        => TestInClassAsync(
            @"$$Dictionary<int, string> d;",
            MainDescription("class System.Collections.Generic.Dictionary<TKey, TValue>"),
            TypeParameterMap(
                Lines($"""

                    TKey {FeaturesResources.is_} int
                    """,
                      $"TValue {FeaturesResources.is_} string")));

    [Fact]
    public Task TestDictionaryOfTAndU()
        => TestInMethodAsync(
            """
            class C<T, U>
            {
                $$Dictionary<T, U> d;
            }
            """,
            MainDescription("class System.Collections.Generic.Dictionary<TKey, TValue>"),
            TypeParameterMap(
                Lines($"""

                    TKey {FeaturesResources.is_} T
                    """,
                      $"TValue {FeaturesResources.is_} U")));

    [Fact]
    public Task TestIEnumerableOfInt()
        => TestInClassAsync(
            """
            $$IEnumerable<int> M()
            {
                yield break;
            }
            """,
            MainDescription("interface System.Collections.Generic.IEnumerable<out T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} int
                """));

    [Fact]
    public Task TestEventHandler()
        => TestInClassAsync(
            @"event $$EventHandler e;",
            MainDescription("delegate void System.EventHandler(object sender, System.EventArgs e)"));

    [Fact]
    public Task TestTypeParameter()
        => TestAsync(
            """
            class C<T>
            {
                $$T t;
            }
            """,
            MainDescription($"T {FeaturesResources.in_} C<T>"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538636")]
    public Task TestTypeParameterWithDocComment()
        => TestAsync("""

            ///<summary>Hello!</summary>
            ///<typeparam name="T">T is Type Parameter</typeparam>
            class C<T> { $$T t; }
            """,
            MainDescription($"T {FeaturesResources.in_} C<T>"),
            Documentation("T is Type Parameter"));

    [Fact]
    public Task TestTypeParameter1_Bug931949()
        => TestAsync(
            """
            class T1<T11>
            {
                $$T11 t;
            }
            """,
            MainDescription($"T11 {FeaturesResources.in_} T1<T11>"));

    [Fact]
    public Task TestTypeParameter2_Bug931949()
        => TestAsync(
            """
            class T1<T11>
            {
                T$$11 t;
            }
            """,
            MainDescription($"T11 {FeaturesResources.in_} T1<T11>"));

    [Fact]
    public Task TestTypeParameter3_Bug931949()
        => TestAsync(
            """
            class T1<T11>
            {
                T1$$1 t;
            }
            """,
            MainDescription($"T11 {FeaturesResources.in_} T1<T11>"));

    [Fact]
    public Task TestTypeParameter4_Bug931949()
        => TestAsync(
            """
            class T1<T11>
            {
                T11$$ t;
            }
            """,
            MainDescription($"T11 {FeaturesResources.in_} T1<T11>"));

    [Fact]
    public Task TestNullableOfInt()
        => TestInClassAsync(@"$$Nullable<int> i; }",
            MainDescription("struct System.Nullable<T> where T : struct"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} int
                """));

    [Fact]
    public Task TestGenericTypeDeclaredOnMethod1_Bug1946()
        => TestAsync(
            """
            class C
            {
                static void Meth1<T1>($$T1 i) where T1 : struct
                {
                    T1 i;
                }
            }
            """,
            MainDescription($"T1 {FeaturesResources.in_} C.Meth1<T1> where T1 : struct"));

    [Fact]
    public Task TestGenericTypeDeclaredOnMethod2_Bug1946()
        => TestAsync(
            """
            class C
            {
                static void Meth1<T1>(T1 i) where $$T1 : struct
                {
                    T1 i;
                }
            }
            """,
            MainDescription($"T1 {FeaturesResources.in_} C.Meth1<T1> where T1 : struct"));

    [Fact]
    public Task TestGenericTypeDeclaredOnMethod3_Bug1946()
        => TestAsync(
            """
            class C
            {
                static void Meth1<T1>(T1 i) where T1 : struct
                {
                    $$T1 i;
                }
            }
            """,
            MainDescription($"T1 {FeaturesResources.in_} C.Meth1<T1> where T1 : struct"));

    [Fact]
    public Task TestGenericTypeParameterConstraint_Class()
        => TestAsync(
            """
            class C<T> where $$T : class
            {
            }
            """,
            MainDescription($"T {FeaturesResources.in_} C<T> where T : class"));

    [Fact]
    public Task TestGenericTypeParameterConstraint_Struct()
        => TestAsync(
            """
            struct S<T> where $$T : class
            {
            }
            """,
            MainDescription($"T {FeaturesResources.in_} S<T> where T : class"));

    [Fact]
    public Task TestGenericTypeParameterConstraint_Interface()
        => TestAsync(
            """
            interface I<T> where $$T : class
            {
            }
            """,
            MainDescription($"T {FeaturesResources.in_} I<T> where T : class"));

    [Fact]
    public Task TestGenericTypeParameterConstraint_Delegate()
        => TestAsync(
            @"delegate void D<T>() where $$T : class;",
            MainDescription($"T {FeaturesResources.in_} D<T> where T : class"));

    [Fact]
    public Task TestMinimallyQualifiedConstraint()
        => TestAsync(@"class C<T> where $$T : IEnumerable<int>",
            MainDescription($"T {FeaturesResources.in_} C<T> where T : IEnumerable<int>"));

    [Fact]
    public Task FullyQualifiedConstraint()
        => TestAsync(@"class C<T> where $$T : System.Collections.Generic.IEnumerable<int>",
            MainDescription($"T {FeaturesResources.in_} C<T> where T : System.Collections.Generic.IEnumerable<int>"));

    [Fact]
    public Task TestMethodReferenceInSameMethod()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    M$$();
                }
            }
            """,
            MainDescription("void C.M()"));

    [Fact]
    public Task TestMethodReferenceInSameMethodWithDocComment()
        => TestInClassAsync("""

            ///<summary>Hello World</summary>
            void M() { M$$(); }
            """,
            MainDescription("void C.M()"),
            Documentation("Hello World"));

    [Fact]
    public Task TestFieldInMethodBuiltIn()
        => TestInClassAsync("""
            int field;

            void M()
            {
                field$$
            }
            """,
            MainDescription($"({FeaturesResources.field}) int C.field"));

    [Fact]
    public Task TestFieldInMethodBuiltIn2()
        => TestInClassAsync(
            """
            int field;

            void M()
            {
                int f = field$$;
            }
            """,
            MainDescription($"({FeaturesResources.field}) int C.field"));

    [Fact]
    public Task TestFieldInMethodBuiltInWithFieldInitializer()
        => TestInClassAsync(
            """
            int field = 1;

            void M()
            {
                int f = field $$;
            }
            """);

    [Fact]
    public Task TestOperatorBuiltIn()
        => TestInMethodAsync(
            """
            int x;

            x = x$$+1;
            """,
            MainDescription("int int.operator +(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn1()
        => TestInMethodAsync(
            """
            int x;

            x = x$$ + 1;
            """,
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestOperatorBuiltIn2()
        => TestInMethodAsync(
            """
            int x;

            x = x+$$x;
            """,
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestOperatorBuiltIn3()
        => TestInMethodAsync(
            """
            int x;

            x = x +$$ x;
            """,
            MainDescription("int int.operator +(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn4()
        => TestInMethodAsync(
            """
            int x;

            x = x + $$x;
            """,
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestOperatorBuiltIn5()
        => TestInMethodAsync(
            """
            int x;

            x = unchecked (x$$+1);
            """,
            MainDescription("int int.operator +(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn6()
        => TestInMethodAsync(
            """
            int x;

            x = checked (x$$+1);
            """,
            MainDescription("int int.operator checked +(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn7()
        => TestInMethodAsync(
            """
            int x;

            x = unchecked (x +$$ x);
            """,
            MainDescription("int int.operator +(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn8()
        => TestInMethodAsync(
            """
            int x;

            x = checked (x +$$ x);
            """,
            MainDescription("int int.operator checked +(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn9()
        => TestInMethodAsync(
            """
            int x;

            x = $$-x;
            """,
            MainDescription("int int.operator -(int value)"));

    [Fact]
    public Task TestOperatorBuiltIn10()
        => TestInMethodAsync(
            """
            int x;

            x = unchecked ($$-x);
            """,
            MainDescription("int int.operator -(int value)"));

    [Fact]
    public Task TestOperatorBuiltIn11()
        => TestInMethodAsync(
            """
            int x;

            x = checked ($$-x);
            """,
            MainDescription("int int.operator checked -(int value)"));

    [Fact]
    public Task TestOperatorBuiltIn12()
        => TestInMethodAsync(
            """
            int x;

            x = x >>>$$ x;
            """,
            MainDescription("int int.operator >>>(int left, int right)"));

    [Fact]
    public Task TestOperatorBuiltIn13()
        => TestInMethodAsync(
            """
            int x;

            x >>>=$$ x;
            """,
            MainDescription("int int.operator >>>(int left, int right)"));

    [Fact]
    public Task TestOperatorCustomTypeBuiltIn_01()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = c +$$ c; }
            }
            """);

    [Fact]
    public Task TestOperatorCustomTypeBuiltIn_02()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = c >>>$$ c; }
            }
            """);

    [Fact]
    public Task TestOperatorCustomTypeOverload_01()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = c +$$ c; }
                static C operator+(C a, C b) { return a; }
            }
            """,
            MainDescription("C C.operator +(C a, C b)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_02()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = unchecked (c +$$ c); }
                static C operator+(C a, C b) { return a; }
            }
            """,
            MainDescription("C C.operator +(C a, C b)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_03()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = unchecked (c +$$ c); }
                static C operator+(C a, C b) { return a; }
                static C operator checked +(C a, C b) { return a; }
            }
            """,
            MainDescription("C C.operator +(C a, C b)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_04()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = checked (c +$$ c); }
                static C operator+(C a, C b) { return a; }
                static C operator checked +(C a, C b) { return a; }
            }
            """,
            MainDescription("C C.operator checked +(C a, C b)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_05()
        => TestAsync("""
            class C
            {
                static void M() { C c; c =  $$-c; }
                static C operator-(C a) { return a; }
            }
            """,
            MainDescription("C C.operator -(C a)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_06()
        => TestAsync("""
            class C
            {
                static void M() { C c; c =  unchecked ($$-c); }
                static C operator-(C a) { return a; }
            }
            """,
            MainDescription("C C.operator -(C a)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_07()
        => TestAsync("""
            class C
            {
                static void M() { C c; c =  unchecked ($$-c); }
                static C operator-(C a) { return a; }
                static C operator checked -(C a) { return a; }
            }
            """,
            MainDescription("C C.operator -(C a)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_08()
        => TestAsync("""
            class C
            {
                static void M() { C c; c =  checked ($$-c); }
                static C operator-(C a) { return a; }
                static C operator checked -(C a) { return a; }
            }
            """,
            MainDescription("C C.operator checked -(C a)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_09()
        => TestAsync("""
            class C
            {
                static void M() { C c; c = c >>>$$ c; }
                static C operator>>>(C a, C b) { return a; }
            }
            """,
            MainDescription("C C.operator >>>(C a, C b)"));

    [Fact]
    public Task TestOperatorCustomTypeOverload_10()
        => TestAsync("""
            class C
            {
                static void M() { C c; c >>>=$$ c; }
                static C operator>>>(C a, C b) { return a; }
            }
            """,
            MainDescription("C C.operator >>>(C a, C b)"));

    [Theory]
    [CombinatorialData]
    public Task TestInstanceIncrementOperators_Postfix([CombinatorialValues("++", "--")] string op)
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            $$$"""
            class C
            {
                static void M() { C c; c{{{op}}}$$; }
                public void operator {{{op}}}() {}
            }
            """,
            MainDescription($"void C.operator {op}()"));

    [Theory]
    [CombinatorialData]
    public Task TestInstanceIncrementOperators_Postfix_Checked([CombinatorialValues("++", "--")] string op)
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            $$$"""
            class C
            {
                static void M() { checked { C c; c{{{op}}}$$; } }
                public void operator checked {{{op}}}() {}
            }
            """,
            MainDescription($"void C.operator checked {op}()"));

    [Theory]
    [CombinatorialData]
    public Task TestInstanceIncrementOperators_Prefix([CombinatorialValues("++", "--")] string op)
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            $$$"""
            class C
            {
                static void M() { C c; {{{op}}}$$ c; }
                public void operator {{{op}}}() {}
            }
            """,
            MainDescription($"void C.operator {op}()"));

    [Theory]
    [CombinatorialData]
    public Task TestInstanceIncrementOperators_Prefix_Checked([CombinatorialValues("++", "--")] string op)
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            $$$"""
            class C
            {
                static void M() { checked { C c; {{{op}}}$$ c; } }
                public void operator checked {{{op}}}() {}
            }
            """,
            MainDescription($"void C.operator checked {op}()"));

    [Theory]
    [CombinatorialData]
    public Task TestInstanceCompoundAssignmentOperators([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            $$$"""
            class C
            {
                static void M() { C c; c {{{op}}}$$ 1; }
                public void operator {{{op}}}(int x) {}
            }
            """,
            MainDescription($"void C.operator {op}(int x)"));

    [Theory]
    [CombinatorialData]
    public Task TestInstanceCompoundAssignmentOperators_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            $$$"""
            class C
            {
                static void M() { checked { C c; c {{{op}}}$$ 1; } }
                public void operator checked {{{op}}}(int x) {}
            }
            """,
            MainDescription($"void C.operator checked {op}(int x)"));

    [Fact]
    public Task TestFieldInMethodMinimal()
        => TestInClassAsync("""
            DateTime field;

            void M()
            {
                field$$
            }
            """,
            MainDescription($"({FeaturesResources.field}) DateTime C.field"));

    [Fact]
    public Task TestFieldInMethodQualified()
        => TestInClassAsync("""
            System.IO.FileInfo file;

            void M()
            {
                file$$
            }
            """,
            MainDescription($"({FeaturesResources.field}) System.IO.FileInfo C.file"));

    [Fact]
    public Task TestMemberOfStructFromSource()
        => TestAsync("""
            struct MyStruct {
            public static int SomeField; }
            static class Test { int a = MyStruct.Some$$Field; }
            """,
            MainDescription($"({FeaturesResources.field}) static int MyStruct.SomeField"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestMemberOfStructFromSourceWithDocComment()
        => TestAsync("""
            struct MyStruct {
            ///<summary>My Field</summary>
            public static int SomeField; }
            static class Test { int a = MyStruct.Some$$Field; }
            """,
            MainDescription($"({FeaturesResources.field}) static int MyStruct.SomeField"),
            Documentation("My Field"));

    [Fact]
    public Task TestMemberOfStructInsideMethodFromSource()
        => TestAsync("""
            struct MyStruct {
            public static int SomeField; }
            static class Test { static void Method() { int a = MyStruct.Some$$Field; } }
            """,
            MainDescription($"({FeaturesResources.field}) static int MyStruct.SomeField"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestMemberOfStructInsideMethodFromSourceWithDocComment()
        => TestAsync("""
            struct MyStruct {
            ///<summary>My Field</summary>
            public static int SomeField; }
            static class Test { static void Method() { int a = MyStruct.Some$$Field; } }
            """,
            MainDescription($"({FeaturesResources.field}) static int MyStruct.SomeField"),
            Documentation("My Field"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestPartialMethodDocComment_01()
        => TestAsync("""
            partial class MyClass
            {
                ///<summary>My Method Definition</summary>
                public partial void MyMethod();

                ///<summary>My Method Implementation</summary>
                public partial void MyMethod()
                {
                }
            }
            static class Test { static void Method() { MyClass.My$$Method(); } }
            """,
            MainDescription($"void MyClass.MyMethod()"),
            Documentation("My Method Implementation"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestPartialMethodDocComment_02()
        => TestAsync("""
            partial class MyClass
            {
                ///<summary>My Method Definition</summary>
                public partial void MyMethod();

                public partial void MyMethod()
                {
                }
            }
            static class Test { static void Method() { MyClass.My$$Method(); } }
            """,
            MainDescription($"void MyClass.MyMethod()"),
            Documentation("My Method Definition"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestPartialMethodDocComment_03()
        => TestAsync("""
            partial class MyClass
            {
                public partial void MyMethod();

                ///<summary>My Method Implementation</summary>
                public partial void MyMethod()
                {
                }
            }
            static class Test { static void Method() { MyClass.My$$Method(); } }
            """,
            MainDescription($"void MyClass.MyMethod()"),
            Documentation("My Method Implementation"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestPartialMethodDocComment_04()
        => TestAsync("""
            partial class MyClass
            {
                ///<summary>My Method Definition</summary>
                public partial void MyMethod();
            }
            static class Test { static void Method() { MyClass.My$$Method(); } }
            """,
            MainDescription($"void MyClass.MyMethod()"),
            Documentation("My Method Definition"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestPartialMethodDocComment_05()
        => TestAsync("""
            partial class MyClass
            {
                ///<summary>My Method Implementation</summary>
                public partial void MyMethod() { }
            }
            static class Test { static void Method() { MyClass.My$$Method(); } }
            """,
            MainDescription($"void MyClass.MyMethod()"),
            Documentation("My Method Implementation"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538638")]
    public Task TestPartialMethodDocComment_06()
        => TestAsync("""
            partial class MyClass
            {
                ///<summary>My Method Definition</summary>
                partial void MyMethod();

                partial void MyMethod() { }
            }
            static class Test { static void Method() { MyClass.My$$Method(); } }
            """,
            MainDescription($"void MyClass.MyMethod()"),
            Documentation("My Method Definition"));

    [Fact]
    public Task TestMetadataFieldMinimal()
        => TestInMethodAsync(@"DateTime dt = DateTime.MaxValue$$",
            MainDescription($"({FeaturesResources.field}) static readonly DateTime DateTime.MaxValue"));

    [Fact]
    public Task TestMetadataFieldQualified1()
        => TestAsync("""
            class C {
                void M()
                {
                    DateTime dt = System.DateTime.MaxValue$$
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) static readonly System.DateTime System.DateTime.MaxValue"));

    [Fact]
    public Task TestMetadataFieldQualified2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    DateTime dt = System.DateTime.MaxValue$$
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) static readonly System.DateTime System.DateTime.MaxValue"));

    [Fact]
    public Task TestMetadataFieldQualified3()
        => TestAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    DateTime dt = System.DateTime.MaxValue$$
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) static readonly DateTime DateTime.MaxValue"));

    [Fact]
    public Task ConstructedGenericField()
        => TestAsync(
            """
            class C<T>
            {
                public T Field;
            }

            class D
            {
                void M()
                {
                    new C<int>().Fi$$eld.ToString();
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) int C<int>.Field"));

    [Fact]
    public Task UnconstructedGenericField()
        => TestAsync(
            """
            class C<T>
            {
                public T Field;

                void M()
                {
                    Fi$$eld.ToString();
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) T C<T>.Field"));

    [Fact]
    public Task TestIntegerLiteral()
        => TestInMethodAsync(@"int f = 37$$",
            MainDescription("struct System.Int32"));

    [Fact]
    public Task TestTrueKeyword()
        => TestInMethodAsync(@"bool f = true$$",
            MainDescription("struct System.Boolean"));

    [Fact]
    public Task TestFalseKeyword()
        => TestInMethodAsync(@"bool f = false$$",
            MainDescription("struct System.Boolean"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26027")]
    public Task TestNullLiteral()
        => TestInMethodAsync(@"string f = null$$",
            MainDescription("class System.String"));

    [Fact]
    public async Task TestNullLiteralWithVar()
        => await TestInMethodAsync(@"var f = null$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26027")]
    public Task TestDefaultLiteral()
        => TestInMethodAsync(@"string f = default$$",
            MainDescription("class System.String"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226")]
    public Task TestAwaitKeywordOnGenericTaskReturningAsync()
        => TestAsync("""
            using System.Threading.Tasks;
            class C
            {
                public async Task<int> Calc()
                {
                    aw$$ait Calc();
                    return 5;
                }
            }
            """, MainDescription(string.Format(FeaturesResources.Awaited_task_returns_0, "struct System.Int32")));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226")]
    public Task TestAwaitKeywordInDeclarationStatement()
        => TestAsync("""
            using System.Threading.Tasks;
            class C
            {
                public async Task<int> Calc()
                {
                    var x = $$await Calc();
                    return 5;
                }
            }
            """, MainDescription(string.Format(FeaturesResources.Awaited_task_returns_0, "struct System.Int32")));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226")]
    public Task TestAwaitKeywordOnTaskReturningAsync()
        => TestAsync("""
            using System.Threading.Tasks;
            class C
            {
                public async void Calc()
                {
                    aw$$ait Task.Delay(100);
                }
            }
            """, MainDescription(FeaturesResources.Awaited_task_returns_no_value));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337")]
    public Task TestNestedAwaitKeywords1()
        => TestAsync("""
            using System;
            using System.Threading.Tasks;
            class AsyncExample2
            {
                async Task<Task<int>> AsyncMethod()
                {
                    return NewMethod();
                }

                private static Task<int> NewMethod()
                {
                    int hours = 24;
                    return hours;
                }

                async Task UseAsync()
                {
                    Func<Task<int>> lambda = async () =>
                    {
                        return await await AsyncMethod();
                    };

                    int result = await await AsyncMethod();
                    Task<Task<int>> resultTask = AsyncMethod();
                    result = await awa$$it resultTask;
                    result = await lambda();
                }
            }
            """, MainDescription(string.Format(FeaturesResources.Awaited_task_returns_0, $"({CSharpFeaturesResources.awaitable}) class System.Threading.Tasks.Task<TResult>")),
                     TypeParameterMap($"""

                         TResult {FeaturesResources.is_} int
                         """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226")]
    public Task TestNestedAwaitKeywords2()
        => TestAsync("""
            using System;
            using System.Threading.Tasks;
            class AsyncExample2
            {
                async Task<Task<int>> AsyncMethod()
                {
                    return NewMethod();
                }

                private static Task<int> NewMethod()
                {
                    int hours = 24;
                    return hours;
                }

                async Task UseAsync()
                {
                    Func<Task<int>> lambda = async () =>
                    {
                        return await await AsyncMethod();
                    };

                    int result = await await AsyncMethod();
                    Task<Task<int>> resultTask = AsyncMethod();
                    result = awa$$it await resultTask;
                    result = await lambda();
                }
            }
            """, MainDescription(string.Format(FeaturesResources.Awaited_task_returns_0, "struct System.Int32")));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337")]
    public Task TestAwaitablePrefixOnCustomAwaiter()
        => TestAsync("""
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            using Z = $$C;

            class C
            {
                public MyAwaiter GetAwaiter() { throw new NotImplementedException(); }
            }

            class MyAwaiter : INotifyCompletion
            {
                public void OnCompleted(Action continuation)
                {
                    throw new NotImplementedException();
                }

                public bool IsCompleted { get { throw new NotImplementedException(); } }
                public void GetResult() { }
            }
            """, MainDescription($"({CSharpFeaturesResources.awaitable}) class C"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337")]
    public Task TestTaskType()
        => TestAsync("""
            using System.Threading.Tasks;
            class C
            {
                public void Calc()
                {
                    Task$$ v1;
                }
            }
            """, MainDescription($"({CSharpFeaturesResources.awaitable}) class System.Threading.Tasks.Task"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337")]
    public Task TestTaskOfTType()
        => TestAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                public void Calc()
                {
                    Task$$<int> v1;
                }
            }
            """, MainDescription($"({CSharpFeaturesResources.awaitable}) class System.Threading.Tasks.Task<TResult>"),
                     TypeParameterMap($"""

                         TResult {FeaturesResources.is_} int
                         """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7100")]
    public Task TestDynamicIsntAwaitable()
        => TestAsync("""

            class C
            {
                dynamic D() { return null; }
                void M()
                {
                    D$$();
                }
            }

            """, MainDescription("dynamic C.D()"));

    [Fact]
    public Task TestStringLiteral()
        => TestInMethodAsync(@"string f = ""Goo""$$",
            MainDescription("class System.String"));

    [Fact]
    public Task TestStringLiteralUtf8_01()
        => TestInMethodAsync(@"var f = ""Goo""u8$$",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact]
    public Task TestStringLiteralUtf8_02()
        => TestInMethodAsync(@"var f = ""Goo""U8$$",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1280")]
    public Task TestVerbatimStringLiteral()
        => TestInMethodAsync(@"string f = @""cat""$$",
            MainDescription("class System.String"));

    [Fact]
    public Task TestVerbatimStringLiteralUtf8_01()
        => TestInMethodAsync(@"string f = @""cat""u8$$",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact]
    public Task TestVerbatimStringLiteralUtf8_02()
        => TestInMethodAsync(@"string f = @""cat""U8$$",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact]
    public Task TestRawStringLiteral()
        => TestInMethodAsync(@"string f = """"""Goo""""""$$",
            MainDescription("class System.String"));

    [Fact]
    public Task TestRawStringLiteralUtf8_01()
        => TestInMethodAsync(@"string f = """"""Goo""""""u8$$",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact]
    public Task TestRawStringLiteralUtf8_02()
        => TestInMethodAsync(@"string f = """"""Goo""""""U8$$",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact]
    public Task TestRawStringLiteralMultiline()
        => TestInMethodAsync(""""
            string f = """
                            Goo
                """$$
            """",
            MainDescription("class System.String"));

    [Fact]
    public Task TestRawStringLiteralMultilineUtf8_01()
        => TestInMethodAsync(""""
            string f = """
                            Goo
                """u8$$
            """",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact]
    public Task TestRawStringLiteralMultilineUtf8_02()
        => TestInMethodAsync(""""
            string f = """
                            Goo
                """U8$$
            """",
            TestSources.Span,
            MainDescription("readonly ref struct System.ReadOnlySpan<T>"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} byte
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1280")]
    public async Task TestInterpolatedStringLiteral()
    {
        await TestInMethodAsync(@"string f = $""cat""$$", MainDescription("class System.String"));
        await TestInMethodAsync("""
            string f = $"c$$at"
            """, MainDescription("class System.String"));
        await TestInMethodAsync("""
            string f = $"$$cat"
            """, MainDescription("class System.String"));
        await TestInMethodAsync("""
            string f = $"cat {1$$ + 2} dog"
            """, MainDescription("struct System.Int32"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1280")]
    public async Task TestVerbatimInterpolatedStringLiteral()
    {
        await TestInMethodAsync(@"string f = $@""cat""$$", MainDescription("class System.String"));
        await TestInMethodAsync("""
            string f = $@"c$$at"
            """, MainDescription("class System.String"));
        await TestInMethodAsync("""
            string f = $@"$$cat"
            """, MainDescription("class System.String"));
        await TestInMethodAsync("""
            string f = $@"cat {1$$ + 2} dog"
            """, MainDescription("struct System.Int32"));
    }

    [Fact]
    public Task TestCharLiteral()
        => TestInMethodAsync(@"string f = 'x'$$",
            MainDescription("struct System.Char"));

    [Fact]
    public Task DynamicKeyword()
        => TestInMethodAsync(
            @"dyn$$amic dyn;",
            MainDescription("dynamic"),
            Documentation(FeaturesResources.Represents_an_object_whose_operations_will_be_resolved_at_runtime));

    [Fact]
    public Task DynamicField()
        => TestInClassAsync(
            """
            dynamic dyn;

            void M()
            {
                d$$yn.Goo();
            }
            """,
            MainDescription($"({FeaturesResources.field}) dynamic C.dyn"));

    [Fact]
    public Task LocalProperty_Minimal()
        => TestInClassAsync(
            """
            DateTime Prop { get; set; }

            void M()
            {
                P$$rop.ToString();
            }
            """,
            MainDescription("DateTime C.Prop { get; set; }"));

    [Fact]
    public Task LocalProperty_Minimal_PrivateSet()
        => TestInClassAsync(
            """
            public DateTime Prop { get; private set; }

            void M()
            {
                P$$rop.ToString();
            }
            """,
            MainDescription("DateTime C.Prop { get; private set; }"));

    [Fact]
    public Task LocalProperty_Minimal_PrivateSet1()
        => TestInClassAsync(
            """
            protected internal int Prop { get; private set; }

            void M()
            {
                P$$rop.ToString();
            }
            """,
            MainDescription("int C.Prop { get; private set; }"));

    [Fact]
    public Task LocalProperty_Qualified()
        => TestInClassAsync(
            """
            System.IO.FileInfo Prop { get; set; }

            void M()
            {
                P$$rop.ToString();
            }
            """,
            MainDescription("System.IO.FileInfo C.Prop { get; set; }"));

    [Fact]
    public Task NonLocalProperty_Minimal()
        => TestInMethodAsync(@"DateTime.No$$w.ToString();",
            MainDescription("DateTime DateTime.Now { get; }"));

    [Fact]
    public Task NonLocalProperty_Qualified()
        => TestInMethodAsync(
            """
            System.IO.FileInfo f;

            f.Att$$ributes.ToString();
            """,
            MainDescription("System.IO.FileAttributes System.IO.FileSystemInfo.Attributes { get; set; }"));

    [Fact]
    public Task ConstructedGenericProperty()
        => TestAsync(
            """
            class C<T>
            {
                public T Property { get; set }
            }

            class D
            {
                void M()
                {
                    new C<int>().Pro$$perty.ToString();
                }
            }
            """,
            MainDescription("int C<int>.Property { get; set; }"));

    [Fact]
    public Task UnconstructedGenericProperty()
        => TestAsync(
            """
            class C<T>
            {
                public T Property { get; set}

                void M()
                {
                    Pro$$perty.ToString();
                }
            }
            """,
            MainDescription("T C<T>.Property { get; set; }"));

    [Fact]
    public Task ValueInProperty()
        => TestInClassAsync(
            """
            public DateTime Property
            {
                set
                {
                    goo = val$$ue;
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) DateTime value"));

    [Fact]
    public Task EnumTypeName()
        => TestInMethodAsync(@"Consol$$eColor c",
            MainDescription("enum System.ConsoleColor"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_Definition()
        => TestInClassAsync(@"enum E$$ : byte { A, B }",
            MainDescription("enum C.E : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_AsField()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private E$$ _E;

            """,
            MainDescription("enum C.E : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_AsProperty()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private E$$ E{ get; set; };

            """,
            MainDescription("enum C.E : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_AsParameter()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private void M(E$$ e) { }

            """,
            MainDescription("enum C.E : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_AsReturnType()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private E$$ M() { }

            """,
            MainDescription("enum C.E : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_AsLocal()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private void M()
            {
                E$$ e = default;
            }

            """,
            MainDescription("enum C.E : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_OnMemberAccessOnType()
        => TestInClassAsync("""

            enum EN : byte { A, B }

            private void M()
            {
                var ea = E$$N.A;
            }

            """,
            MainDescription("enum C.EN : byte"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_OnMemberAccessOnType_OnDot()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private void M()
            {
                var ea = E$$.A;
            }

            """,
            MainDescription("E.A = 0"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    public Task EnumNonDefaultUnderlyingType_NotOnMemberAccessOnMember()
        => TestInClassAsync("""

            enum E : byte { A, B }

            private void M()
            {
                var ea = E.A$$;
            }

            """,
            MainDescription("E.A = 0"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    [InlineData("byte", "byte")]
    [InlineData("byte", "System.Byte")]
    [InlineData("sbyte", "sbyte")]
    [InlineData("sbyte", "System.SByte")]
    [InlineData("short", "short")]
    [InlineData("short", "System.Int16")]
    [InlineData("ushort", "ushort")]
    [InlineData("ushort", "System.UInt16")]
    // int is the default type and is not shown
    [InlineData("uint", "uint")]
    [InlineData("uint", "System.UInt32")]
    [InlineData("long", "long")]
    [InlineData("long", "System.Int64")]
    [InlineData("ulong", "ulong")]
    [InlineData("ulong", "System.UInt64")]
    public Task EnumNonDefaultUnderlyingType_ShowForNonDefaultTypes(string displayTypeName, string underlyingTypeName)
        => TestInClassAsync($$"""

            enum E$$ : {{underlyingTypeName}}
            {
                A, B
            }
            """,
            MainDescription($"enum C.E : {displayTypeName}"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/52490")]
    [InlineData("")]
    [InlineData(": int")]
    [InlineData(": System.Int32")]
    public Task EnumNonDefaultUnderlyingType_DoNotShowForDefaultType(string defaultType)
        => TestInClassAsync($$"""

            enum E$$ {{defaultType}}
            {
                A, B
            }
            """,
            MainDescription("enum C.E"));

    [Fact]
    public Task EnumMemberNameFromMetadata()
        => TestInMethodAsync(@"ConsoleColor c = ConsoleColor.Bla$$ck",
            MainDescription("ConsoleColor.Black = 0"));

    [Fact]
    public Task FlagsEnumMemberNameFromMetadata1()
        => TestInMethodAsync(@"AttributeTargets a = AttributeTargets.Cl$$ass",
            MainDescription("AttributeTargets.Class = 4"));

    [Fact]
    public Task FlagsEnumMemberNameFromMetadata2()
        => TestInMethodAsync(@"AttributeTargets a = AttributeTargets.A$$ll",
            MainDescription("AttributeTargets.All = AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter"));

    [Fact]
    public Task EnumMemberNameFromSource1()
        => TestAsync(
            """
            enum E
            {
                A = 1 << 0,
                B = 1 << 1,
                C = 1 << 2
            }

            class C
            {
                void M()
                {
                    var e = E.B$$;
                }
            }
            """,
            MainDescription("E.B = 1 << 1"));

    [Fact]
    public Task EnumMemberNameFromSource2()
        => TestAsync(
            """
            enum E
            {
                A,
                B,
                C
            }

            class C
            {
                void M()
                {
                    var e = E.B$$;
                }
            }
            """,
            MainDescription("E.B = 1"));

    [Fact]
    public Task Parameter_InMethod_Minimal()
        => TestInClassAsync(
            """
            void M(DateTime dt)
            {
                d$$t.ToString();
            """,
            MainDescription($"({FeaturesResources.parameter}) DateTime dt"));

    [Fact]
    public Task Parameter_InMethod_Qualified()
        => TestInClassAsync(
            """
            void M(System.IO.FileInfo fileInfo)
            {
                file$$Info.ToString();
            """,
            MainDescription($"({FeaturesResources.parameter}) System.IO.FileInfo fileInfo"));

    [Fact]
    public Task Parameter_FromReferenceToNamedParameter()
        => TestInMethodAsync(@"Console.WriteLine(va$$lue: ""Hi"");",
            MainDescription($"({FeaturesResources.parameter}) string value"));

    [Fact]
    public Task Parameter_DefaultValue()
        => TestInClassAsync(
            """
            void M(int param = 42)
            {
                para$$m.ToString();
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) int param = 42"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task Lambda_Parameter_DefaultValue_01()
        => TestInMethodAsync(
            """
            (int param = 42) => {
                return para$$m + 1;
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) int param = 42"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task Lambda_Parameter_DefaultValue_02()
        => TestInMethodAsync(
            """
            (int param = $$int.MaxValue) => {
                return param + 1;
            }
            """,
            MainDescription($"{FeaturesResources.struct_} System.Int32"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task Lambda_Parameter_DefaultValue_03()
        => TestInMethodAsync(
            """
            (int param = int.$$MaxValue) => {
                return param + 1;
            }
            """,
            MainDescription($"({FeaturesResources.constant}) const int int.MaxValue = 2147483647"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task Lambda_Parameter_ParamsArray()
        => TestInMethodAsync(
            """
            (params int[] xs) => {
                return x$$s.Length;
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) params int[] xs"));

    [Fact]
    public Task Parameter_Params()
        => TestInClassAsync(
            """
            void M(params DateTime[] arg)
            {
                ar$$g.ToString();
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) params DateTime[] arg"));

    [Fact]
    public Task Parameter_Ref()
        => TestInClassAsync(
            """
            void M(ref DateTime arg)
            {
                ar$$g.ToString();
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) ref DateTime arg"));

    [Fact]
    public Task Parameter_Out()
        => TestInClassAsync(
            """
            void M(out DateTime arg)
            {
                ar$$g.ToString();
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) out DateTime arg"));

    [Fact]
    public Task Local_Minimal()
        => TestInMethodAsync(
            """
            DateTime dt;

            d$$t.ToString();
            """,
            MainDescription($"({FeaturesResources.local_variable}) DateTime dt"));

    [Fact]
    public Task Local_Qualified()
        => TestInMethodAsync(
            """
            System.IO.FileInfo fileInfo;

            file$$Info.ToString();
            """,
            MainDescription($"({FeaturesResources.local_variable}) System.IO.FileInfo fileInfo"));

    [Fact]
    public Task Method_MetadataOverload()
        => TestInMethodAsync("Console.Write$$Line();",
            MainDescription($"void Console.WriteLine() (+ 18 {FeaturesResources.overloads_})"));

    [Fact]
    public Task Method_SimpleWithOverload()
        => TestInClassAsync(
            """
            void Method()
            {
                Met$$hod();
            }

            void Method(int i)
            {
            }
            """,
            MainDescription($"void C.Method() (+ 1 {FeaturesResources.overload})"));

    [Fact]
    public Task Method_MoreOverloads()
        => TestInClassAsync(
            """
            void Method()
            {
                Met$$hod(null);
            }

            void Method(int i)
            {
            }

            void Method(DateTime dt)
            {
            }

            void Method(System.IO.FileInfo fileInfo)
            {
            }
            """,
            MainDescription($"void C.Method(System.IO.FileInfo fileInfo) (+ 3 {FeaturesResources.overloads_})"));

    [Fact]
    public Task Method_SimpleInSameClass()
        => TestInClassAsync(
            """
            DateTime GetDate(System.IO.FileInfo ft)
            {
                Get$$Date(null);
            }
            """,
            MainDescription("DateTime C.GetDate(System.IO.FileInfo ft)"));

    [Fact]
    public Task Method_OptionalParameter()
        => TestInClassAsync(
            """
            void M()
            {
                Met$$hod();
            }

            void Method(int i = 0)
            {
            }
            """,
            MainDescription("void C.Method([int i = 0])"));

    [Fact]
    public Task Method_OptionalDecimalParameter()
        => TestInClassAsync(
            """
            void Goo(decimal x$$yz = 10)
            {
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) decimal xyz = 10"));

    [Fact]
    public Task Method_Generic()
        => TestInClassAsync(
            """
            TOut Goo<TIn, TOut>(TIn arg) where TIn : IEquatable<TIn>
            {
                Go$$o<int, DateTime>(37);
            }
            """,

        MainDescription("DateTime C.Goo<int, DateTime>(int arg)"));

    [Fact]
    public Task Method_UnconstructedGeneric()
        => TestInClassAsync(
            """
            TOut Goo<TIn, TOut>(TIn arg)
            {
                Go$$o<TIn, TOut>(default(TIn);
            }
            """,

            MainDescription("TOut C.Goo<TIn, TOut>(TIn arg)"));

    [Fact]
    public Task Method_Inferred()
        => TestInClassAsync(
            """
            void Goo<TIn>(TIn arg)
            {
                Go$$o(42);
            }
            """,
            MainDescription("void C.Goo<int>(int arg)"));

    [Fact]
    public Task Method_MultipleParams()
        => TestInClassAsync(
            """
            void Goo(DateTime dt, System.IO.FileInfo fi, int number)
            {
                Go$$o(DateTime.Now, null, 32);
            }
            """,
            MainDescription("void C.Goo(DateTime dt, System.IO.FileInfo fi, int number)"));

    [Fact]
    public Task Method_OptionalParam()
        => TestInClassAsync(
            """
            void Goo(int num = 42)
            {
                Go$$o();
            }
            """,
            MainDescription("void C.Goo([int num = 42])"));

    [Fact]
    public Task Method_ParameterModifiers()
        => TestInClassAsync(
            """
            void Goo(ref DateTime dt, out System.IO.FileInfo fi, params int[] numbers)
            {
                Go$$o(DateTime.Now, null, 32);
            }
            """,
            MainDescription("void C.Goo(ref DateTime dt, out System.IO.FileInfo fi, params int[] numbers)"));

    [Fact]
    public Task Method_RefReadonly()
        => TestInClassAsync(
            """
            void Goo(ref readonly DateTime dt, ref readonly System.IO.FileInfo fi, params int[] numbers)
            {
                Go$$o(in DateTime.Now, in fi, 32);
            }
            """,
            MainDescription("void C.Goo(ref readonly DateTime dt, ref readonly System.IO.FileInfo fi, params int[] numbers)"));

    [Fact]
    public Task Constructor()
        => TestInClassAsync(
            """
            public C()
            {
            }

            void M()
            {
                new C$$().ToString();
            }
            """,
            MainDescription("C.C()"));

    [Fact]
    public Task Constructor_Overloads()
        => TestInClassAsync(
            """
            public C()
            {
            }

            public C(DateTime dt)
            {
            }

            public C(int i)
            {
            }

            void M()
            {
                new C$$(DateTime.MaxValue).ToString();
            }
            """,
            MainDescription($"C.C(DateTime dt) (+ 2 {FeaturesResources.overloads_})"));

    /// <summary>
    /// Regression for 3923
    /// </summary>
    [Fact]
    public Task Constructor_OverloadFromStringLiteral()
        => TestInMethodAsync(
            @"new InvalidOperatio$$nException("""");",
            MainDescription($"InvalidOperationException.InvalidOperationException(string message) (+ 2 {FeaturesResources.overloads_})"));

    /// <summary>
    /// Regression for 3923
    /// </summary>
    [Fact]
    public Task Constructor_UnknownType()
        => TestInvalidTypeInClassAsync(
            """
            void M()
            {
                new G$$oo();
            }
            """);

    /// <summary>
    /// Regression for 3923
    /// </summary>
    [Fact]
    public Task Constructor_OverloadFromProperty()
        => TestInMethodAsync(
            @"new InvalidOperatio$$nException(this.GetType().Name);",
            MainDescription($"InvalidOperationException.InvalidOperationException(string message) (+ 2 {FeaturesResources.overloads_})"));

    [Fact]
    public Task Constructor_Metadata()
        => TestInMethodAsync(
            @"new Argument$$NullException();",
            MainDescription($"ArgumentNullException.ArgumentNullException() (+ 3 {FeaturesResources.overloads_})"));

    [Fact]
    public Task Constructor_MetadataQualified()
        => TestInMethodAsync(@"new System.IO.File$$Info(null);",
            MainDescription("System.IO.FileInfo.FileInfo(string fileName)"));

    [Fact]
    public Task InterfaceProperty()
        => TestInMethodAsync(
            """
            interface I
            {
                string Name$$ { get; set; }
            }
            """,
            MainDescription("string I.Name { get; set; }"));

    [Fact]
    public Task ExplicitInterfacePropertyImplementation()
        => TestInMethodAsync(
            """
            interface I
            {
                string Name { get; set; }
            }

            class C : I
            {
                string IEmployee.Name$$
                {
                    get
                    {
                        return "";
                    }

                    set
                    {
                    }
                }
            }
            """,
            MainDescription("string C.Name { get; set; }"));

    [Fact]
    public Task Operator()
        => TestInClassAsync(
            """
            public static C operator +(C left, C right)
            {
                return null;
            }

            void M(C left, C right)
            {
                return left +$$ right;
            }
            """,
            MainDescription("C C.operator +(C left, C right)"));

#pragma warning disable CA2243 // Attribute string literals should parse correctly
    [WorkItem(792629, "generic type parameter constraints for methods in quick info")]
#pragma warning restore CA2243 // Attribute string literals should parse correctly
    [Fact]
    public Task GenericMethodWithConstraintsAtDeclaration()
        => TestInClassAsync(
            """
            TOut G$$oo<TIn, TOut>(TIn arg) where TIn : IEquatable<TIn>
            {
            }
            """,

        MainDescription("TOut C.Goo<TIn, TOut>(TIn arg) where TIn : IEquatable<TIn>"));

#pragma warning disable CA2243 // Attribute string literals should parse correctly
    [WorkItem(792629, "generic type parameter constraints for methods in quick info")]
#pragma warning restore CA2243 // Attribute string literals should parse correctly
    [Fact]
    public Task GenericMethodWithMultipleConstraintsAtDeclaration()
        => TestInClassAsync(
            """
            TOut Goo<TIn, TOut>(TIn arg) where TIn : Employee, new()
            {
                Go$$o<TIn, TOut>(default(TIn);
            }
            """,

        MainDescription("TOut C.Goo<TIn, TOut>(TIn arg) where TIn : Employee, new()"));

#pragma warning disable CA2243 // Attribute string literals should parse correctly
    [WorkItem(792629, "generic type parameter constraints for methods in quick info")]
#pragma warning restore CA2243 // Attribute string literals should parse correctly
    [Fact]
    public Task UnConstructedGenericMethodWithConstraintsAtInvocation()
        => TestInClassAsync(
            """
            TOut Goo<TIn, TOut>(TIn arg) where TIn : Employee
            {
                Go$$o<TIn, TOut>(default(TIn);
            }
            """,

        MainDescription("TOut C.Goo<TIn, TOut>(TIn arg) where TIn : Employee"));

    [Fact]
    public Task GenericTypeWithConstraintsAtDeclaration()
        => TestAsync(
            """
            public class Employee : IComparable<Employee>
            {
                public int CompareTo(Employee other)
                {
                    throw new NotImplementedException();
                }
            }

            class Emplo$$yeeList<T> : IEnumerable<T> where T : Employee, System.IComparable<T>, new()
            {
            }
            """,

        MainDescription("class EmployeeList<T> where T : Employee, System.IComparable<T>, new()"));

    [Fact]
    public Task GenericType()
        => TestAsync(
            """
            class T1<T11>
            {
                $$T11 i;
            }
            """,
            MainDescription($"T11 {FeaturesResources.in_} T1<T11>"));

    [Fact]
    public Task GenericMethod()
        => TestInClassAsync(
            """
            static void Meth1<T1>(T1 i) where T1 : struct
            {
                $$T1 i;
            }
            """,
            MainDescription($"T1 {FeaturesResources.in_} C.Meth1<T1> where T1 : struct"));

    [Fact]
    public Task Var()
        => TestInMethodAsync(
            """
            var x = new Exception();
            var y = $$x;
            """,
            MainDescription($"({FeaturesResources.local_variable}) Exception x"));

    [Fact]
    public Task NullableReference()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp8),
            """
            class A<T>
            {
            }
            class B
            {
                static void M()
                {
                    A<B?>? x = null!;
                    var y = x;
                    $$y.ToString();
                }
            }
            """,
            // https://github.com/dotnet/roslyn/issues/26198 public API should show inferred nullability
            MainDescription($"({FeaturesResources.local_variable}) A<B?> y"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26648")]
    public Task NullableReference_InMethod()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp8),
            """

            class G
            {
                void M()
                {
                    C c;
                    c.Go$$o();
                }
            }
            public class C
            {
                public string? Goo(IEnumerable<object?> arg)
                {
                }
            }
            """, MainDescription("string? C.Goo(IEnumerable<object?> arg)"));

    [Fact]
    public Task NestedInGeneric()
        => TestInMethodAsync(
            @"List<int>.Enu$$merator e;",
            MainDescription("struct System.Collections.Generic.List<T>.Enumerator"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} int
                """));

    [Fact]
    public Task NestedGenericInGeneric()
        => TestAsync(
            """
            class Outer<T>
            {
                class Inner<U>
                {
                }

                static void M()
                {
                    Outer<int>.I$$nner<string> e;
                }
            }
            """,
            MainDescription("class Outer<T>.Inner<U>"),
            TypeParameterMap(
                Lines($"""

                    T {FeaturesResources.is_} int
                    """,
                      $"U {FeaturesResources.is_} string")));

    [Fact]
    public Task ObjectInitializer1()
        => TestInClassAsync(
            """
            void M()
            {
                var x = new test() { $$z = 5 };
            }

            class test
            {
                public int z;
            }
            """,
            MainDescription($"({FeaturesResources.field}) int test.z"));

    [Fact]
    public Task ObjectInitializer2()
        => TestInMethodAsync(
            """
            class C
            {
                void M()
                {
                    var x = new test() { z = $$5 };
                }

                class test
                {
                    public int z;
                }
            }
            """,
            MainDescription("struct System.Int32"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537880")]
    public Task TypeArgument()
        => TestAsync(
            """
            class C<T, Y>
            {
                void M()
                {
                    C<int, DateTime> variable;
                    $$variable = new C<int, DateTime>();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) C<int, DateTime> variable"));

    [Fact]
    public Task ForEachLoop_1()
        => TestInMethodAsync(
            """
            int bb = 555;

            bb = bb + 1;
            foreach (int cc in new int[]{ 1,2,3}){
            c$$c = 1;
            bb = bb + 21;
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int cc"));

    [Fact]
    public Task TryCatchFinally_1()
        => TestInMethodAsync(
            """
            try
                        {
                            int aa = 555;

            a$$a = aa + 1;
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                        }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int aa"));

    [Fact]
    public Task TryCatchFinally_2()
        => TestInMethodAsync(
            """
            try
                        {
                        }
                        catch (Exception ex)
                        {
                            var y = e$$x;
            var z = y;
                        }
                        finally
                        {
                        }
            """,
            MainDescription($"({FeaturesResources.local_variable}) Exception ex"));

    [Fact]
    public Task TryCatchFinally_3()
        => TestInMethodAsync(
            """
            try
                        {
                        }
                        catch (Exception ex)
                        {
                            var aa = 555;

            aa = a$$a + 1;
                        }
                        finally
                        {
                        }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int aa"));

    [Fact]
    public Task TryCatchFinally_4()
        => TestInMethodAsync(
            """
            try
                        {
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                            int aa = 555;

            aa = a$$a + 1;
                        }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int aa"));

    [Fact]
    public Task GenericVariable()
        => TestAsync(
            """
            class C<T, Y>
            {
                void M()
                {
                    C<int, DateTime> variable;
                    var$$iable = new C<int, DateTime>();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) C<int, DateTime> variable"));

    [Fact]
    public Task TestInstantiation()
        => TestAsync(
            """
            using System.Collections.Generic;

            class Program<T>
            {
                static void Main(string[] args)
                {
                    var p = new Dictio$$nary<int, string>();
                }
            }
            """,
            MainDescription($"Dictionary<int, string>.Dictionary() (+ 5 {FeaturesResources.overloads_})"));

    [Fact]
    public Task TestUsingAlias_Bug4141()
        => TestAsync(
            """
            using X = A.C;

            class A
            {
                public class C
                {
                }
            }

            class D : X$$
            {
            }
            """,
            MainDescription(@"class A.C"));

    [Fact]
    public Task TestFieldOnDeclaration()
        => TestInClassAsync(
            @"DateTime fie$$ld;",
            MainDescription($"({FeaturesResources.field}) DateTime C.field"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538767")]
    public Task TestGenericErrorFieldOnDeclaration()
        => TestInClassAsync(
            @"NonExistentType<int> fi$$eld;",
            MainDescription($"({FeaturesResources.field}) NonExistentType<int> C.field"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538822")]
    public Task TestDelegateType()
        => TestInClassAsync(
            @"Fun$$c<int, string> field;",
            MainDescription("delegate TResult System.Func<in T, out TResult>(T arg)"),
            TypeParameterMap(
                Lines($"""

                    T {FeaturesResources.is_} int
                    """,
                      $"TResult {FeaturesResources.is_} string")));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538824")]
    public Task TestOnDelegateInvocation()
        => TestAsync(
            """
            class Program
            {
                delegate void D1();

                static void Main()
                {
                    D1 d = Main;
                    $$d();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) D1 d"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539240")]
    public Task TestOnArrayCreation1()
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    int[] a = n$$ew int[0];
                }
            }
            """, MainDescription("int[]"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539240")]
    public Task TestOnArrayCreation2()
        => TestAsync(
            """
            class Program
            {
                static void Main()
                {
                    int[] a = new i$$nt[0];
                }
            }
            """,
            MainDescription("struct System.Int32"));

    [Fact]
    public Task Constructor_ImplicitObjectCreation()
        => TestAsync(
            """
            class C
            {
                static void Main()
                {
                    C c = ne$$w();
                }
            }

            """,
            MainDescription("C.C()"));

    [Fact]
    public Task Constructor_ImplicitObjectCreation_WithParameters()
        => TestAsync(
            """
            class C
            {
                C(int i) { }
                C(string s) { }
                static void Main()
                {
                    C c = ne$$w(1);
                }
            }

            """,
            MainDescription($"C.C(int i) (+ 1 {FeaturesResources.overload})"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539841")]
    public Task TestIsNamedTypeAccessibleForErrorTypes()
        => TestAsync(
            """
            sealed class B<T1, T2> : A<B<T1, T2>>
            {
                protected sealed override B<A<T>, A$$<T>> N()
                {
                }
            }

            internal class A<T>
            {
            }
            """,
            MainDescription("class A<T>"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540075")]
    public Task TestErrorType()
        => TestAsync(
            """
            using Goo = Goo;

            class C
            {
                void Main()
                {
                    $$Goo
                }
            }
            """,
            MainDescription("Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16662")]
    public Task TestShortDiscardInAssignment()
        => TestAsync(
            """
            class C
            {
                int M()
                {
                    $$_ = M();
                }
            }
            """,
            MainDescription($"({FeaturesResources.discard}) int _"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16662")]
    public Task TestUnderscoreLocalInAssignment()
        => TestAsync(
            """
            class C
            {
                int M()
                {
                    var $$_ = M();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int _"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16662")]
    public Task TestShortDiscardInOutVar()
        => TestAsync(
            """
            class C
            {
                void M(out int i)
                {
                    M(out $$_);
                    i = 0;
                }
            }
            """,
            MainDescription($"({FeaturesResources.discard}) int _"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16667")]
    public Task TestDiscardInOutVar()
        => TestAsync(
            """
            class C
            {
                void M(out int i)
                {
                    M(out var $$_);
                    i = 0;
                }
            }
            """); // No quick info (see issue #16667)

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16667")]
    public Task TestDiscardInIsPattern()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    if (3 is int $$_) { }
                }
            }
            """); // No quick info (see issue #16667)

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16667")]
    public Task TestDiscardInSwitchPattern()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case int $$_:
                            return;
                    }
                }
            }
            """); // No quick info (see issue #16667)

    [Fact]
    public Task TestLambdaDiscardParameter_FirstDiscard()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    System.Func<string, int, int> f = ($$_, _) => 1;
                }
            }
            """,
            MainDescription($"({FeaturesResources.discard}) string _"));

    [Fact]
    public Task TestLambdaDiscardParameter_SecondDiscard()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    System.Func<string, int, int> f = (_, $$_) => 1;
                }
            }
            """,
            MainDescription($"({FeaturesResources.discard}) int _"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540871")]
    public Task TestLiterals()
        => TestAsync(
            """
            class MyClass
            {
                MyClass() : this($$10)
                {
                    intI = 2;
                }

                public MyClass(int i)
                {
                }

                static int intI = 1;

                public static int Main()
                {
                    return 1;
                }
            }
            """,
            MainDescription("struct System.Int32"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541444")]
    public Task TestErrorInForeach()
        => TestAsync(
            """
            class C
            {
                void Main()
                {
                    foreach (int cc in null)
                    {
                        $$cc = 1;
                    }
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int cc"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541678")]
    public Task TestQuickInfoOnEvent()
        => TestAsync(
            """
            using System;

            public class SampleEventArgs
            {
                public SampleEventArgs(string s)
                {
                    Text = s;
                }

                public String Text { get; private set; }
            }

            public class Publisher
            {
                public delegate void SampleEventHandler(object sender, SampleEventArgs e);

                public event SampleEventHandler SampleEvent;

                protected virtual void RaiseSampleEvent()
                {
                    if (Sam$$pleEvent != null)
                        SampleEvent(this, new SampleEventArgs("Hello"));
                }
            }
            """,
            MainDescription("SampleEventHandler Publisher.SampleEvent"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542157")]
    public Task TestEvent()
        => TestInMethodAsync(@"System.Console.CancelKeyPres$$s += null;",
            MainDescription("ConsoleCancelEventHandler Console.CancelKeyPress"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542157")]
    public Task TestEventPlusEqualsOperator()
        => TestInMethodAsync(@"System.Console.CancelKeyPress +$$= null;",
            MainDescription("void Console.CancelKeyPress.add"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542157")]
    public Task TestEventMinusEqualsOperator()
        => TestInMethodAsync(@"System.Console.CancelKeyPress -$$= null;",
            MainDescription("void Console.CancelKeyPress.remove"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541885")]
    public Task TestQuickInfoOnExtensionMethod()
        => TestWithOptionsAsync(Options.Regular,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    int[] values = {
                        1
                    };
                    bool isArray = 7.I$$n(values);
                }
            }

            public static class MyExtensions
            {
                public static bool In<T>(this T o, IEnumerable<T> items)
                {
                    return true;
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.extension}) bool int.In<int>(IEnumerable<int> items)"));

    [Fact]
    public Task TestQuickInfoOnExtensionMethodOverloads()
        => TestWithOptionsAsync(Options.Regular,
            """
            using System;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    "1".Test$$Ext();
                }
            }

            public static class Ex
            {
                public static void TestExt<T>(this T ex)
                {
                }

                public static void TestExt<T>(this T ex, T arg)
                {
                }

                public static void TestExt(this string ex, int arg)
                {
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.extension}) void string.TestExt<string>() (+ 2 {FeaturesResources.overloads_})"));

    [Fact]
    public Task TestQuickInfoOnExtensionMethodOverloads2()
        => TestWithOptionsAsync(Options.Regular,
            """
            using System;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    "1".Test$$Ext();
                }
            }

            public static class Ex
            {
                public static void TestExt<T>(this T ex)
                {
                }

                public static void TestExt<T>(this T ex, T arg)
                {
                }

                public static void TestExt(this int ex, int arg)
                {
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.extension}) void string.TestExt<string>() (+ 1 {FeaturesResources.overload})"));

    [Fact]
    public Task Query1()
        => TestAsync(
            """
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from n in new int[] { 1, 2, 3, 4, 5 }

                            select $$n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) int n"));

    [Fact]
    public Task Query2()
        => TestAsync(
            """
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from n$$ in new int[] { 1, 2, 3, 4, 5 }

                            select n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) int n"));

    [Fact]
    public Task Query3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var q = from n in new int[] { 1, 2, 3, 4, 5 }

                            select $$n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) ? n"));

    [Fact]
    public Task Query4()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var q = from n$$ in new int[] { 1, 2, 3, 4, 5 }

                            select n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) ? n"));

    [Fact]
    public Task Query5()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from n in new List<object>()
                            select $$n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) object n"));

    [Fact]
    public Task Query6()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from n$$ in new List<object>()
                            select n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) object n"));

    [Fact]
    public Task Query7()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from int n in new List<object>()
                            select $$n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) int n"));

    [Fact]
    public Task Query8()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from int n$$ in new List<object>()
                            select n;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) int n"));

    [Fact]
    public Task Query9()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from x$$ in new List<List<int>>()
                            from y in x
                            select y;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) List<int> x"));

    [Fact]
    public Task Query10()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from x in new List<List<int>>()
                            from y in $$x
                            select y;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) List<int> x"));

    [Fact]
    public Task Query11()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from x in new List<List<int>>()
                            from y$$ in x
                            select y;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) int y"));

    [Fact]
    public Task Query12()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var q = from x in new List<List<int>>()
                            from y in x
                            select $$y;
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) int y"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoSelectMappedEnumerable()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Select<int, int>(Func<int, int> selector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoSelectMappedQueryable()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0].AsQueryable()
                            $$select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IQueryable<int> IQueryable<int>.Select<int, int>(System.Linq.Expressions.Expression<Func<int, int>> selector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoSelectMappedCustom()
        => TestAsync(
            """

            using System;
            using System.Linq;

            namespace N {
                public static class LazyExt
                {
                    public static Lazy<U> Select<T, U>(this Lazy<T> source, Func<T, U> selector) => new Lazy<U>(() => selector(source.Value));
                }
                public class C
                {
                    public void M()
                    {
                        var lazy = new Lazy<object>();
                        var q = from i in lazy
                                $$select i;
                    }
                }
            }

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) Lazy<object> Lazy<object>.Select<object, object>(Func<object, object> selector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoSelectNotMapped()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            where true
                            $$select i;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoLet()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$let j = true
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<'a> IEnumerable<int>.Select<int, 'a>(Func<int, 'a> selector)"),
            AnonymousTypes($$"""

            {{FeaturesResources.Types_colon}}
                'a {{FeaturesResources.is_}} new { int i, bool j }
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69830")]
    public Task QueryMethodinfoLet2()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            using System;
            using System.Collections.Generic;

            _ = from element in new List<int> { 1, 2, 3 }
                let elementInterim = element + 42
                $$let anotherElementInterim = elementInterim - 42 // Point to 'let' keyword
                select element;

            static class Extensions
            {
                /// <summary>
                /// Gets a list of <typeparamref name="TResult"/> elements.
                /// </summary>
                public static List<TResult> Select<T, TResult>(
                    this List<T> elements,
                    Func<T, TResult> selector
                )
                {
                    return null;
                }
            } 
            """,
            MainDescription($"({CSharpFeaturesResources.extension}) List<'b> List<'a>.Select<'a, 'b>(Func<'a, 'b> selector)"),
            AnonymousTypes($$"""

                {{FeaturesResources.Types_colon}}
                    'a is new { int element, int elementInterim }
                    'b is new { int anotherElementInterim }
                """),
            Documentation("""
                Gets a list of 'b elements.
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoWhere()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$where true
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Where<int>(Func<int, bool> predicate)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByOneProperty()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$orderby i
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IEnumerable<int>.OrderBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByOnePropertyWithOrdering1()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i $$ascending
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IEnumerable<int>.OrderBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByOnePropertyWithOrdering2()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$orderby i ascending
                            select i;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithComma1()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i$$, i
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IOrderedEnumerable<int>.ThenBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithComma2()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$orderby i, i
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IEnumerable<int>.OrderBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrdering1()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$orderby i, i ascending
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IEnumerable<int>.OrderBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrdering2()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i,$$ i ascending
                            select i;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrdering3()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i, i $$ascending
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IOrderedEnumerable<int>.ThenBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrderingOnEach1()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            $$orderby i ascending, i ascending
                            select i;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrderingOnEach2()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i $$ascending, i ascending
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IEnumerable<int>.OrderBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrderingOnEach3()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i ascending ,$$ i ascending
                            select i;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByTwoPropertiesWithOrderingOnEach4()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            orderby i ascending, i $$ascending
                            select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IOrderedEnumerable<int>.ThenBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoOrderByIncomplete()
        => TestInMethodAsync(
            """

                    var q = from i in new int[0]
                            where i > 0
                            orderby$$ 

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IOrderedEnumerable<int> IEnumerable<int>.OrderBy<int, ?>(Func<int, ?> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoSelectMany1()
        => TestInMethodAsync(
            """

                    var q = from i1 in new int[0]
                            $$from i2 in new int[0]
                            select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.SelectMany<int, int, int>(Func<int, IEnumerable<int>> collectionSelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoSelectMany2()
        => TestInMethodAsync(
            """

                    var q = from i1 in new int[0]
                            from i2 $$in new int[0]
                            select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.SelectMany<int, int, int>(Func<int, IEnumerable<int>> collectionSelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoGroupBy1()
        => TestInMethodAsync(
            """

                        var q = from i in new int[0]
                                $$group i by i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<IGrouping<int, int>> IEnumerable<int>.GroupBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoGroupBy2()
        => TestInMethodAsync(
            """

                        var q = from i in new int[0]
                                group i $$by i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<IGrouping<int, int>> IEnumerable<int>.GroupBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoGroupByInto()
        => TestInMethodAsync(
            """

                        var q = from i in new int[0]
                                $$group i by i into g
                                select g;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<IGrouping<int, int>> IEnumerable<int>.GroupBy<int, int>(Func<int, int> keySelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoJoin1()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                $$join i2 in new int[0] on i1 equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoJoin2()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join i2 $$in new int[0] on i1 equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoJoin3()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join i2 in new int[0] $$on i1 equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoJoin4()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join i2 in new int[0] on i1 $$equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoJoinInto1()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                $$join i2 in new int[0] on i1 equals i2 into g
                                select g;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<IEnumerable<int>> IEnumerable<int>.GroupJoin<int, int, int, IEnumerable<int>>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, IEnumerable<int>, IEnumerable<int>> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoJoinInto2()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join i2 in new int[0] on i1 equals i2 $$into g
                                select g;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoFromMissing()
        => TestInMethodAsync(
            """

                        var q = $$from i in new int[0]
                                select i;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableSimple1()
        => TestInMethodAsync(
            """

                        var q = $$from double i in new int[0]
                                select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<double> System.Collections.IEnumerable.Cast<double>()"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableSimple2()
        => TestInMethodAsync(
            """

                        var q = from double i $$in new int[0]
                                select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<double> System.Collections.IEnumerable.Cast<double>()"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableSelectMany1()
        => TestInMethodAsync(
            """

                        var q = from i in new int[0]
                                $$from double d in new int[0]
                                select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.SelectMany<int, double, int>(Func<int, IEnumerable<double>> collectionSelector, Func<int, double, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableSelectMany2()
        => TestInMethodAsync(
            """

                        var q = from i in new int[0]
                                from double d $$in new int[0]
                                select i;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<double> System.Collections.IEnumerable.Cast<double>()"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableJoin1()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                $$join int i2 in new double[0] on i1 equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableJoin2()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join int i2 $$in new double[0] on i1 equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> System.Collections.IEnumerable.Cast<int>()"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableJoin3()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join int i2 in new double[0] $$on i1 equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23394")]
    public Task QueryMethodinfoRangeVariableJoin4()
        => TestInMethodAsync(
            """

                        var q = from i1 in new int[0]
                                join int i2 in new double[0] on i1 $$equals i2
                                select i1;

            """,
            MainDescription($"({CSharpFeaturesResources.extension}) IEnumerable<int> IEnumerable<int>.Join<int, int, int, int>(IEnumerable<int> inner, Func<int, int> outerKeySelector, Func<int, int> innerKeySelector, Func<int, int, int> resultSelector)"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543205")]
    public Task TestErrorGlobal()
        => TestAsync(
            """
            extern alias global;

            class myClass
            {
                static int Main()
                {
                    $$global::otherClass oc = new global::otherClass();
                    return 0;
                }
            }
            """,
            MainDescription("<global namespace>"));

    [Fact]
    public Task DoNotRemoveAttributeSuffixAndProduceInvalidIdentifier1()
        => TestAsync(
            """
            using System;

            class classAttribute : Attribute
            {
                private classAttribute x$$;
            }
            """,
            MainDescription($"({FeaturesResources.field}) classAttribute classAttribute.x"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544026")]
    public Task DoNotRemoveAttributeSuffix2()
        => TestAsync(
            """
            using System;

            class class1Attribute : Attribute
            {
                private class1Attribute x$$;
            }
            """,
            MainDescription($"({FeaturesResources.field}) class1Attribute class1Attribute.x"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1696")]
    public Task AttributeQuickInfoBindsToClassTest()
        => TestAsync(
            """
            using System;

            /// <summary>
            /// class comment
            /// </summary>
            [Some$$]
            class SomeAttribute : Attribute
            {
                /// <summary>
                /// ctor comment
                /// </summary>
                public SomeAttribute()
                {
                }
            }
            """,
            Documentation("class comment"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1696")]
    public Task AttributeConstructorQuickInfo()
        => TestAsync(
            """
            using System;

            /// <summary>
            /// class comment
            /// </summary>
            class SomeAttribute : Attribute
            {
                /// <summary>
                /// ctor comment
                /// </summary>
                public SomeAttribute()
                {
                    var s = new Some$$Attribute();
                }
            }
            """,
            Documentation("ctor comment"));

    [Fact]
    public Task TestLabel()
        => TestInClassAsync(
            """
            void M()
            {
            Goo:
                int Goo;
                goto Goo$$;
            }
            """,
            MainDescription($"({FeaturesResources.label}) Goo"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542613")]
    public Task TestUnboundGeneric()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    Type t = typeof(L$$ist<>);
                }
            }
            """,
            MainDescription("class System.Collections.Generic.List<T>"),
            NoTypeParameterMap);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543113")]
    public Task TestAnonymousTypeNew1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var v = $$new { };
                }
            }
            """,
            MainDescription(@"AnonymousType 'a"),
            NoTypeParameterMap,
            AnonymousTypes(
                $$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new {  }
                """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543873")]
    public async Task TestNestedAnonymousType()
    {
        // verify nested anonymous types are listed in the same order for different properties
        // verify first property
        await TestInMethodAsync(
            """
            var x = new[] { new { Name = "BillG", Address = new { Street = "1 Microsoft Way", Zip = "98052" } } };

            x[0].$$Address
            """,
            MainDescription(@"'b 'a.Address { get; }"),
            NoTypeParameterMap,
            AnonymousTypes(
                $$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { string Name, 'b Address }
                    'b {{FeaturesResources.is_}} new { string Street, string Zip }
                """));

        // verify second property
        await TestInMethodAsync(
            """
            var x = new[] { new { Name = "BillG", Address = new { Street = "1 Microsoft Way", Zip = "98052" } } };

            x[0].$$Name
            """,
            MainDescription(@"string 'a.Name { get; }"),
            NoTypeParameterMap,
            AnonymousTypes(
                $$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { string Name, 'b Address }
                    'b {{FeaturesResources.is_}} new { string Street, string Zip }
                """));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543183")]
    public Task TestAssignmentOperatorInAnonymousType()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var a = new { A $$= 0 };
                }
            }
            """);

    [Fact, WorkItem(10731, "DevDiv_Projects/Roslyn")]
    public Task TestErrorAnonymousTypeDoesntShow()
        => TestInMethodAsync(
            @"var a = new { new { N = 0 }.N, new { } }.$$N;",
            MainDescription(@"int 'a.N { get; }"),
            NoTypeParameterMap,
            AnonymousTypes(
                $$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { int N }
                """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543553")]
    public Task TestArrayAssignedToVar()
        => TestAsync(
            """
            class C
            {
                static void M(string[] args)
                {
                    v$$ar a = args;
                }
            }
            """,
            MainDescription("string[]"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529139")]
    public Task ColorColorRangeVariable()
        => TestAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            namespace N1
            {
                class yield
                {
                    public static IEnumerable<yield> Bar()
                    {
                        foreach (yield yield in from yield in new yield[0]
                                                select y$$ield)
                        {
                            yield return yield;
                        }
                    }
                }
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) N1.yield yield"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
    public Task QuickInfoOnOperator()
        => TestAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var v = new Program() $$+ string.Empty;
                }

                public static implicit operator Program(string s)
                {
                    return null;
                }

                public static IEnumerable<Program> operator +(Program p1, Program p2)
                {
                    yield return p1;
                    yield return p2;
                }
            }
            """,
            MainDescription("IEnumerable<Program> Program.operator +(Program p1, Program p2)"));

    [Fact]
    public Task TestConstantField()
        => TestAsync(
            """
            class C
            {
                const int $$F = 1;
            """,
            MainDescription($"({FeaturesResources.constant}) int C.F = 1"));

    [Fact]
    public Task TestMultipleConstantFields()
        => TestAsync(
            """
            class C
            {
                public const double X = 1.0, Y = 2.0, $$Z = 3.5;
            """,
            MainDescription($"({FeaturesResources.constant}) double C.Z = 3.5"));

    [Fact]
    public Task TestConstantDependencies()
        => TestAsync(
            """
            class A
            {
                public const int $$X = B.Z + 1;
                public const int Y = 10;
            }

            class B
            {
                public const int Z = A.Y + 1;
            }
            """,
            MainDescription($"({FeaturesResources.constant}) int A.X = B.Z + 1"));

    [Fact]
    public Task TestConstantCircularDependencies()
        => TestAsync(
            """
            class A
            {
                public const int X = B.Z + 1;
            }

            class B
            {
                public const int Z$$ = A.X + 1;
            }
            """,
            MainDescription($"({FeaturesResources.constant}) int B.Z = A.X + 1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544620")]
    public Task TestConstantOverflow()
        => TestAsync(
            """
            class B
            {
                public const int Z$$ = int.MaxValue + 1;
            }
            """,
            MainDescription($"({FeaturesResources.constant}) int B.Z = int.MaxValue + 1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544620")]
    public Task TestConstantOverflowInUncheckedContext()
        => TestAsync(
            """
            class B
            {
                public const int Z$$ = unchecked(int.MaxValue + 1);
            }
            """,
            MainDescription($"({FeaturesResources.constant}) int B.Z = unchecked(int.MaxValue + 1)"));

    [Fact]
    public Task TestEnumInConstantField()
        => TestAsync(
            """
            public class EnumTest
            {
                enum Days
                {
                    Sun,
                    Mon,
                    Tue,
                    Wed,
                    Thu,
                    Fri,
                    Sat
                };

                static void Main()
                {
                    const int $$x = (int)Days.Sun;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_constant}) int x = (int)Days.Sun"));

    [Fact]
    public Task TestConstantInDefaultExpression()
        => TestAsync(
            """
            public class EnumTest
            {
                enum Days
                {
                    Sun,
                    Mon,
                    Tue,
                    Wed,
                    Thu,
                    Fri,
                    Sat
                };

                static void Main()
                {
                    const Days $$x = default(Days);
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_constant}) Days x = default(Days)"));

    [Fact]
    public Task TestConstantParameter()
        => TestAsync(
            """
            class C
            {
                void Bar(int $$b = 1);
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) int b = 1"));

    [Fact]
    public Task TestConstantLocal()
        => TestAsync(
            """
            class C
            {
                void Bar()
                {
                    const int $$loc = 1;
                }
            """,
            MainDescription($"({FeaturesResources.local_constant}) int loc = 1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType1()
        => TestInMethodAsync(
            @"var $$v1 = new Goo();",
            MainDescription($"({FeaturesResources.local_variable}) Goo v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType2()
        => TestInMethodAsync(
            @"var $$v1 = v1;",
            MainDescription($"({FeaturesResources.local_variable}) var v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType3()
        => TestInMethodAsync(
            @"var $$v1 = new Goo<Bar>();",
            MainDescription($"({FeaturesResources.local_variable}) Goo<Bar> v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType4()
        => TestInMethodAsync(
            @"var $$v1 = &(x => x);",
            MainDescription($"({FeaturesResources.local_variable}) ?* v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType5()
        => TestInMethodAsync("var $$v1 = &v1",
            MainDescription($"({FeaturesResources.local_variable}) var* v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType6()
        => TestInMethodAsync("var $$v1 = new Goo[1]",
            MainDescription($"({FeaturesResources.local_variable}) Goo[] v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType7()
        => TestInClassAsync(
            """
            class C
            {
                void Method()
                {
                }

                void Goo()
                {
                    var $$v1 = MethodGroup;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) ? v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544416")]
    public Task TestErrorType8()
        => TestInMethodAsync("var $$v1 = Unknown",
            MainDescription($"({FeaturesResources.local_variable}) ? v1"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545072")]
    public Task TestDelegateSpecialTypes()
        => TestAsync(
            @"delegate void $$F(int x);",
            MainDescription("delegate void F(int x)"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545108")]
    public Task TestNullPointerParameter()
        => TestAsync(
            """
            class C
            {
                unsafe void $$Goo(int* x = null)
                {
                }
            }
            """,
            MainDescription("void C.Goo([int* x = null])"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545098")]
    public Task TestLetIdentifier1()
        => TestInMethodAsync("var q = from e in \"\" let $$y = 1 let a = new { y } select a;",
            MainDescription($"({FeaturesResources.range_variable}) int y"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545295")]
    public Task TestNullableDefaultValue()
        => TestAsync(
            """
            class Test
            {
                void $$Method(int? t1 = null)
                {
                }
            }
            """,
            MainDescription("void Test.Method([int? t1 = null])"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529586")]
    public Task TestInvalidParameterInitializer()
        => TestAsync(
            """
            class Program
            {
                void M1(float $$j1 = "Hello"
            +
            "World")
                {
                }
            }
            """,
            MainDescription($"""
                ({FeaturesResources.parameter}) float j1 = "Hello" + "World"
                """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545230")]
    public Task TestComplexConstLocal()
        => TestAsync(
            """
            class Program
            {
                void Main()
                {
                    const int MEGABYTE = 1024 *
                        1024 + true;
                    Blah($$MEGABYTE);
                }
            }
            """,
            MainDescription($@"({FeaturesResources.local_constant}) int MEGABYTE = 1024 * 1024 + true"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545230")]
    public Task TestComplexConstField()
        => TestAsync(
            """
            class Program
            {
                const int a = true
                    -
                    false;

                void Main()
                {
                    Goo($$a);
                }
            }
            """,
            MainDescription($"({FeaturesResources.constant}) int Program.a = true - false"));

    [Fact]
    public Task TestTypeParameterCrefDoesNotHaveQuickInfo()
        => TestAsync(
            """
            class C<T>
            {
                ///  <see cref="C{X$$}"/>
                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact]
    public Task TestCref1()
        => TestAsync(
            """
            class Program
            {
                ///  <see cref="Mai$$n"/>
                static void Main(string[] args)
                {
                }
            }
            """,
            MainDescription(@"void Program.Main(string[] args)"));

    [Fact]
    public Task TestCref2()
        => TestAsync(
            """
            class Program
            {
                ///  <see cref="$$Main"/>
                static void Main(string[] args)
                {
                }
            }
            """,
            MainDescription(@"void Program.Main(string[] args)"));

    [Fact]
    public Task TestCref3()
        => TestAsync(
            """
            class Program
            {
                ///  <see cref="Main"$$/>
                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact]
    public Task TestCref4()
        => TestAsync(
            """
            class Program
            {
                ///  <see cref="Main$$"/>
                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact]
    public Task TestCref5()
        => TestAsync(
            """
            class Program
            {
                ///  <see cref="Main"$$/>
                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78171")]
    public Task TestPreprocessingSymbol()
        => TestAsync("""
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                async Task Process(CancellationToken cancellationToken = default)
                {
            #if N$$ET
                    // .NET requires 100ms delay in this fictional example
                    await Task.Delay(100, cancellationToken);
            #else
                    // .NET Framework requires 200ms delay in this fictional example, and we can't pass a CT on it
                    await Task.Delay(200);
            #endif
                }
            }
            """, MainDescription("NET"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546849")]
    public async Task TestIndexedProperty()
    {

        // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
        var referencedCode = """
            Imports System.Runtime.InteropServices
            <ComImport()>
            <GuidAttribute(CCC.ClassId)>
            Public Class CCC

            #Region "COM GUIDs"
                Public Const ClassId As String = "9d965fd2-1514-44f6-accd-257ce77c46b0"
                Public Const InterfaceId As String = "a9415060-fdf0-47e3-bc80-9c18f7f39cf6"
                Public Const EventsId As String = "c6a866a5-5f97-4b53-a5df-3739dc8ff1bb"
            # End Region

                ''' <summary>
                ''' An index property from VB
                ''' </summary>
                ''' <param name="p1">p1 is an integer index</param>
                ''' <returns>A string</returns>
                Public Property IndexProp(ByVal p1 As Integer, Optional ByVal p2 As Integer = 0) As String
                    Get
                        Return Nothing
                    End Get
                    Set(ByVal value As String)

                    End Set
                End Property
            End Class
            """;

        await TestWithReferenceAsync(sourceCode: """
            class Program
            {
                void M()
                {
                        CCC c = new CCC();
                        c.Index$$Prop[0] = "s";
                }
            }
            """,
            referencedCode: referencedCode,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.VisualBasic,
            expectedResults: MainDescription("string CCC.IndexProp[int p1, [int p2 = 0]] { get; set; }"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546918")]
    public Task TestUnconstructedGeneric()
        => TestAsync(
            """
            class A<T>
            {
                enum SortOrder
                {
                    Ascending,
                    Descending,
                    None
                }

                void Goo()
                {
                    var b = $$SortOrder.Ascending;
                }
            }
            """,
            MainDescription(@"enum A<T>.SortOrder"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546970")]
    public Task TestUnconstructedGenericInCRef()
        => TestAsync(
            """
            /// <see cref="$$C{T}" />
            class C<T>
            {
            }
            """,
            MainDescription(@"class C<T>"));

    [Fact]
    public Task TestAwaitableMethod()
        => VerifyWithMscorlib45Async("""
            using System.Threading.Tasks;	
            class C	
            {	
                async Task Goo()	
                {	
                    Go$$o();	
                }	
            }
            """, MainDescription($"({CSharpFeaturesResources.awaitable}) Task C.Goo()"));

    [Fact]
    public Task ObsoleteItem()
        => TestAsync("""

            using System;

            class Program
            {
                [Obsolete]
                public void goo()
                {
                    go$$o();
                }
            }
            """, MainDescription($"[{CSharpFeaturesResources.deprecated}] void Program.goo()"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751070")]
    public Task DynamicOperator()
        => TestAsync("""


            public class Test
            {
                public delegate void NoParam();

                static int Main()
                {
                    dynamic x = new object();
                    if (((System.Func<dynamic>)(() => (x =$$= null)))())
                        return 0;
                    return 1;
                }
            }
            """, MainDescription("dynamic dynamic.operator ==(dynamic left, dynamic right)"));

    [Fact]
    public Task TextOnlyDocComment()
        => TestAsync(
            """
            /// <summary>
            ///goo
            /// </summary>
            class C$$
            {
            }
            """, Documentation("goo"));

    [Fact]
    public Task TestTrimConcatMultiLine()
        => TestAsync(
            """
            /// <summary>
            /// goo
            /// bar
            /// </summary>
            class C$$
            {
            }
            """, Documentation("goo bar"));

    [Fact]
    public Task TestCref()
        => TestAsync(
            """
            /// <summary>
            /// <see cref="C"/>
            /// <seealso cref="C"/>
            /// </summary>
            class C$$
            {
            }
            """, Documentation("C C"));

    [Fact]
    public Task ExcludeTextOutsideSummaryBlock()
        => TestAsync(
            """
            /// red
            /// <summary>
            /// green
            /// </summary>
            /// yellow
            class C$$
            {
            }
            """, Documentation("green"));

    [Fact]
    public Task NewlineAfterPara()
        => TestAsync(
            """
            /// <summary>
            /// <para>goo</para>
            /// </summary>
            class C$$
            {
            }
            """, Documentation("goo"));

    [Fact]
    public Task TextOnlyDocComment_Metadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C$$ c;
                }
            }
            """, """

            /// <summary>
            ///goo
            /// </summary>
            public class C
            {
            }
            """, "C#", "C#", Documentation("goo"));

    [Fact]
    public Task TestTrimConcatMultiLine_Metadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C$$ c;
                }
            }
            """, """

            /// <summary>
            /// goo
            /// bar
            /// </summary>
            public class C
            {
            }
            """, "C#", "C#", Documentation("goo bar"));

    [Fact]
    public Task TestCref_Metadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C$$ c;
                }
            }
            """, """
            /// <summary>
            /// <see cref="C"/>
            /// <seealso cref="C"/>
            /// </summary>
            public class C
            {
            }
            """, "C#", "C#", Documentation("C C"));

    [Fact]
    public Task ExcludeTextOutsideSummaryBlock_Metadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C$$ c;
                }
            }
            """, """

            /// red
            /// <summary>
            /// green
            /// </summary>
            /// yellow
            public class C
            {
            }
            """, "C#", "C#", Documentation("green"));

    [Fact]
    public Task Param()
        => TestAsync(
            """
            /// <summary></summary>
            public class C
            {
                /// <typeparam name="T">A type parameter of <see cref="goo{ T} (string[], T)"/></typeparam>
                /// <param name="args">First parameter of <see cref="Goo{T} (string[], T)"/></param>
                /// <param name="otherParam">Another parameter of <see cref="Goo{T}(string[], T)"/></param>
                public void Goo<T>(string[] arg$$s, T otherParam)
                {
                }
            }
            """, Documentation("First parameter of C.Goo<T>(string[], T)"));

    [Fact]
    public Task Param_Metadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C c;
                    c.Goo<int>(arg$$s: new string[] { }, 1);
                }
            }
            """, """

            /// <summary></summary>
            public class C
            {
                /// <typeparam name="T">A type parameter of <see cref="goo{ T} (string[], T)"/></typeparam>
                /// <param name="args">First parameter of <see cref="Goo{T} (string[], T)"/></param>
                /// <param name="otherParam">Another parameter of <see cref="Goo{T}(string[], T)"/></param>
                public void Goo<T>(string[] args, T otherParam)
                {
                }
            }
            """, "C#", "C#", Documentation("First parameter of C.Goo<T>(string[], T)"));

    [Fact]
    public Task Param2()
        => TestAsync(
            """
            /// <summary></summary>
            public class C
            {
                /// <typeparam name="T">A type parameter of <see cref="goo{ T} (string[], T)"/></typeparam>
                /// <param name="args">First parameter of <see cref="Goo{T} (string[], T)"/></param>
                /// <param name="otherParam">Another parameter of <see cref="Goo{T}(string[], T)"/></param>
                public void Goo<T>(string[] args, T oth$$erParam)
                {
                }
            }
            """, Documentation("Another parameter of C.Goo<T>(string[], T)"));

    [Fact]
    public Task Param2_Metadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C c;
                    c.Goo<int>(args: new string[] { }, other$$Param: 1);
                }
            }
            """, """

            /// <summary></summary>
            public class C
            {
                /// <typeparam name="T">A type parameter of <see cref="goo{ T} (string[], T)"/></typeparam>
                /// <param name="args">First parameter of <see cref="Goo{T} (string[], T)"/></param>
                /// <param name="otherParam">Another parameter of <see cref="Goo{T}(string[], T)"/></param>
                public void Goo<T>(string[] args, T otherParam)
                {
                }
            }
            """, "C#", "C#", Documentation("Another parameter of C.Goo<T>(string[], T)"));

    [Fact]
    public Task TypeParam()
        => TestAsync(
            """
            /// <summary></summary>
            public class C
            {
                /// <typeparam name="T">A type parameter of <see cref="Goo{T} (string[], T)"/></typeparam>
                /// <param name="args">First parameter of <see cref="Goo{T} (string[], T)"/></param>
                /// <param name="otherParam">Another parameter of <see cref="Goo{T}(string[], T)"/></param>
                public void Goo<T$$>(string[] args, T otherParam)
                {
                }
            }
            """, Documentation("A type parameter of C.Goo<T>(string[], T)"));

    [Fact]
    public Task UnboundCref()
        => TestAsync(
            """
            /// <summary></summary>
            public class C
            {
                /// <typeparam name="T">A type parameter of <see cref="goo{T}(string[], T)"/></typeparam>
                /// <param name="args">First parameter of <see cref="Goo{T} (string[], T)"/></param>
                /// <param name="otherParam">Another parameter of <see cref="Goo{T}(string[], T)"/></param>
                public void Goo<T$$>(string[] args, T otherParam)
                {
                }
            }
            """, Documentation("A type parameter of goo<T>(string[], T)"));

    [Fact]
    public Task CrefInConstructor()
        => TestAsync(
            """
            public class TestClass
            {
                /// <summary> 
                /// This sample shows how to specify the <see cref="TestClass"/> constructor as a cref attribute.
                /// </summary> 
                public TestClass$$()
                {
                }
            }
            """, Documentation("This sample shows how to specify the TestClass constructor as a cref attribute."));

    [Fact]
    public Task CrefInConstructorOverloaded()
        => TestAsync(
            """
            public class TestClass
            {
                /// <summary> 
                /// This sample shows how to specify the <see cref="TestClass"/> constructor as a cref attribute.
                /// </summary> 
                public TestClass()
                {
                }

                /// <summary> 
                /// This sample shows how to specify the <see cref="TestClass(int)"/> constructor as a cref attribute.
                /// </summary> 
                public TestC$$lass(int value)
                {
                }
            }
            """, Documentation("This sample shows how to specify the TestClass(int) constructor as a cref attribute."));

    [Fact]
    public Task CrefInGenericMethod1()
        => TestAsync(
            """
            public class TestClass
            {
                /// <summary> 
                /// The GetGenericValue method. 
                /// <para>This sample shows how to specify the <see cref="GetGenericValue"/> method as a cref attribute.</para>
                /// </summary> 
                public static T GetGenericVa$$lue<T>(T para)
                {
                    return para;
                }
            }
            """, Documentation("""
                The GetGenericValue method.

                This sample shows how to specify the TestClass.GetGenericValue<T>(T) method as a cref attribute.
                """));

    [Fact]
    public Task CrefInGenericMethod2()
        => TestAsync(
            """
            public class TestClass
            {
                /// <summary> 
                /// The GetGenericValue method. 
                /// <para>This sample shows how to specify the <see cref="GetGenericValue{T}(T)"/> method as a cref attribute.</para>
                /// </summary> 
                public static T GetGenericVa$$lue<T>(T para)
                {
                    return para;
                }
            }
            """, Documentation("""
                The GetGenericValue method.

                This sample shows how to specify the TestClass.GetGenericValue<T>(T) method as a cref attribute.
                """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813350")]
    public Task CrefInMethodOverloading1()
        => TestAsync(
            """
            public class TestClass
            {
                public static int GetZero()
                {
                    GetGenericValu$$e();
                    GetGenericValue(5);
                }

                /// <summary> 
                /// This sample shows how to call the <see cref="GetGenericValue{T}(T)"/> method
                /// </summary> 
                public static T GetGenericValue<T>(T para)
                {
                    return para;
                }

                /// <summary> 
                /// This sample shows how to specify the <see cref="GetGenericValue"/> method as a cref attribute.
                /// </summary> 
                public static void GetGenericValue()
                {
                }
            }
            """, Documentation("This sample shows how to specify the TestClass.GetGenericValue() method as a cref attribute."));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813350")]
    public Task CrefInMethodOverloading2()
        => TestAsync(
            """
            public class TestClass
            {
                public static int GetZero()
                {
                    GetGenericValue();
                    GetGenericVal$$ue(5);
                }

                /// <summary> 
                /// This sample shows how to call the <see cref="GetGenericValue{T}(T)"/> method
                /// </summary> 
                public static T GetGenericValue<T>(T para)
                {
                    return para;
                }

                /// <summary> 
                /// This sample shows how to specify the <see cref="GetGenericValue"/> method as a cref attribute.
                /// </summary> 
                public static void GetGenericValue()
                {
                }
            }
            """, Documentation("This sample shows how to call the TestClass.GetGenericValue<T>(T) method"));

    [Fact]
    public Task CrefInGenericType()
        => TestAsync(
            """
            /// <summary> 
            /// <remarks>This example shows how to specify the <see cref="GenericClass{T}"/> cref.</remarks>
            /// </summary> 
            class Generic$$Class<T>
            {
            }
            """,
Documentation("This example shows how to specify the GenericClass<T> cref.",
    ExpectedClassifications(
        Text("This example shows how to specify the"),
        WhiteSpace(" "),
        Class("GenericClass"),
        Punctuation.OpenAngle,
        TypeParameter("T"),
        Punctuation.CloseAngle,
        WhiteSpace(" "),
        Text("cref."))));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/812720")]
    public Task ClassificationOfCrefsFromMetadata()
        => TestWithMetadataReferenceHelperAsync("""

            class G
            {
                void goo()
                {
                    C c;
                    c.Go$$o();
                }
            }
            """, """

            /// <summary></summary>
            public class C
            {
                /// <summary> 
                /// See <see cref="Goo"/> method
                /// </summary> 
                public void Goo()
                {
                }
            }
            """, "C#", "C#",
            Documentation("See C.Goo() method",
                ExpectedClassifications(
                    Text("See"),
                    WhiteSpace(" "),
                    Class("C"),
                    Punctuation.Text("."),
                    Identifier("Goo"),
                    Punctuation.OpenParen,
                    Punctuation.CloseParen,
                    WhiteSpace(" "),
                    Text("method"))));

    [Fact]
    public Task FieldAvailableInBothLinkedFiles()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
                int x;
                void goo()
                {
                    x$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [MainDescription($"({FeaturesResources.field}) int C.x"), Usage("")]);

    [Fact]
    public async Task FieldUnavailableInOneLinkedFile()
    {
        var expectedDescription = Usage($"""

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, expectsWarningGlyph: true);

        await VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif
                void goo()
                {
                    x$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [expectedDescription]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37097")]
    public async Task BindSymbolInOtherFile()
    {
        var expectedDescription = Usage($"""

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Not_Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, expectsWarningGlyph: true);

        await VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif
                void goo()
                {
                    x$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="GOO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [expectedDescription]);
    }

    [Fact]
    public async Task FieldUnavailableInTwoLinkedFiles()
    {
        var expectedDescription = Usage(
            $"""

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """,
            expectsWarningGlyph: true);

        await VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif
                void goo()
                {
                    x$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [expectedDescription]);
    }

    [Fact]
    public async Task ExcludeFilesWithInactiveRegions()
    {
        var expectedDescription = Usage($"""

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, expectsWarningGlyph: true);
        await VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO,BAR">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif

            #if BAR
                void goo()
                {
                    x$$
                }
            #endif
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument" />
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3" PreprocessorSymbols="BAR">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [expectedDescription]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/962353")]
    public Task NoValidSymbolsInLinkedDocuments()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
                void goo()
                {
                    B$$ar();
                }
            #if B
                void Bar() { }
            #endif
               
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task LocalsValidInLinkedDocuments()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
                void M()
                {
                    int x$$;
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [MainDescription($"({FeaturesResources.local_variable}) int x"), Usage("")]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task LocalWarningInLinkedDocuments()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="PROJ1">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
                void M()
                {
            #if PROJ1
                    int x;
            #endif

                    int y = x$$;
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [MainDescription($"({FeaturesResources.local_variable}) int x"), Usage($"""

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """, expectsWarningGlyph: true)]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task LabelsValidInLinkedDocuments()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {   
                void M()
                {
                    $$LABEL: goto LABEL;
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [MainDescription($"({FeaturesResources.label}) LABEL"), Usage("")]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task RangeVariablesValidInLinkedDocuments()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="SourceDocument"><![CDATA[
            using System.Linq;
            class C
            {
                void M()
                {
                    var x = from y in new[] {1, 2, 3} select $$y;
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, [MainDescription($"({FeaturesResources.range_variable}) int y"), Usage("")]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019766")]
    public Task PointerAccessibility()
        => TestAsync("""
            class C
            {
                unsafe static void Main()
                {
                    void* p = null;
                    void* q = null;
                    dynamic d = true;
                    var x = p =$$= q == d;
                }
            }
            """, MainDescription("bool void*.operator ==(void* left, void* right)"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114300")]
    public Task AwaitingTaskOfArrayType()
        => TestAsync("""

            using System.Threading.Tasks;

            class Program
            {
                async Task<int[]> M()
                {
                    awa$$it M();
                }
            }
            """, MainDescription(string.Format(FeaturesResources.Awaited_task_returns_0, "int[]")));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114300")]
    public Task AwaitingTaskOfDynamic()
        => TestAsync("""

            using System.Threading.Tasks;

            class Program
            {
                async Task<dynamic> M()
                {
                    awa$$it M();
                }
            }
            """, MainDescription(string.Format(FeaturesResources.Awaited_task_returns_0, "dynamic")));

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task MethodOverloadDifferencesIgnored()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if ONE
                void Do(int x){}
            #endif
            #if TWO
                void Do(string x){}
            #endif
                void Shared()
                {
                    this.Do$$
                }

            }]]></Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, MainDescription($"void C.Do(int x)"));

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task MethodOverloadDifferencesIgnored_ContainingType()
        => VerifyWithReferenceWorkerAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
                void Shared()
                {
                    var x = GetThing().Do$$();
                }

            #if ONE
                private Methods1 GetThing()
                {
                    return new Methods1();
                }
            #endif

            #if TWO
                private Methods2 GetThing()
                {
                    return new Methods2();
                }
            #endif
            }

            #if ONE
            public class Methods1
            {
                public void Do(string x) { }
            }
            #endif

            #if TWO
            public class Methods2
            {
                public void Do(string x) { }
            }
            #endif
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """, MainDescription($"void Methods1.Do(string x)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4868")]
    public Task QuickInfoExceptions()
        => TestAsync(
            """
            using System;

            namespace MyNs
            {
                class MyException1 : Exception
                {
                }

                class MyException2 : Exception
                {
                }

                class TestClass
                {
                    /// <exception cref="MyException1"></exception>
                    /// <exception cref="T:MyNs.MyException2"></exception>
                    /// <exception cref="System.Int32"></exception>
                    /// <exception cref="double"></exception>
                    /// <exception cref="Not_A_Class_But_Still_Displayed"></exception>
                    void M()
                    {
                        M$$();
                    }
                }
            }
            """,
            Exceptions($"""

                {WorkspacesResources.Exceptions_colon}
                  MyException1
                  MyException2
                  int
                  double
                  Not_A_Class_But_Still_Displayed
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLocalFunction()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    local$$();

                    void local() { i++; this.M(); }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLocalFunction2()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    local$$(i);

                    void local(int j) { j++; M(); }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLocalFunction3()
        => TestAsync("""

            class C
            {
                public void M(int @this)
                {
                    int i = 0;
                    local$$();

                    void local()
                    {
                        M(1);
                        i++;
                        @this++;
                    }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, @this, i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLocalFunction4()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    void OuterLocalFunction$$()
                    {
                        int local = 0;
                        int InnerLocalFunction() 
                        {
                            field++;
                            return local;
                        }
                    }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLocalFunction5()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    void OuterLocalFunction()
                    {
                        int local = 0;
                        int InnerLocalFunction$$() 
                        {
                            field++;
                            return local;
                        }
                    }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, local
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLocalFunction6()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    int local1 = 0;
                    int local2 = 0;

                    void OuterLocalFunction$$()
                    {
                        _ = local1;
                        void InnerLocalFunction() 
                        {
                            _ = local2;
                        }
                    }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} local1, local2
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLocalFunction7()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    int local1 = 0;
                    int local2 = 0;

                    void OuterLocalFunction()
                    {
                        _ = local1;
                        void InnerLocalFunction$$() 
                        {
                            _ = local2;
                        }
                    }
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} local2
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLambda()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    System.Action a = () =$$> { i++; M(); };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLambda2()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    System.Action<int> a = j =$$> { i++; j++; M(); };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLambda2_DifferentOrder()
        => TestAsync("""

            class C
            {
                void M(int j)
                {
                    int i;
                    System.Action a = () =$$> { M(); i++; j++; };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, j, i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLambda3()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    int @this;
                    N(() =$$> { M(); @this++; }, () => { i++; });
                }
                void N(System.Action x, System.Action y) { }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, @this
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnLambda4()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    N(() => { M(); }, () =$$> { i++; });
                }
                void N(System.Action x, System.Action y) { }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLambda5()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    System.Action a = () =$$>
                    {
                        int local = 0;
                        System.Func<int> b = () =>
                        {
                            field++;
                            return local;
                        };
                    };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLambda6()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    System.Action a = () =>
                    {
                        int local = 0;
                        System.Func<int> b = () =$$>
                        {
                            field++;
                            return local;
                        };
                    };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} this, local
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLambda7()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    int local1 = 0;
                    int local2 = 0;

                    System.Action a = () =$$>
                    {
                        _ = local1;
                        System.Action b = () =>
                        {
                            _ = local2;
                        };
                    };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} local1, local2
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26101")]
    public Task QuickInfoCapturesOnLambda8()
        => TestAsync("""

            class C
            {
                int field;
                void M()
                {
                    int local1 = 0;
                    int local2 = 0;

                    System.Action a = () =>
                    {
                        _ = local1;
                        System.Action b = () =$$>
                        {
                            _ = local2;
                        };
                    };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} local2
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23307")]
    public Task QuickInfoCapturesOnDelegate()
        => TestAsync("""

            class C
            {
                void M()
                {
                    int i;
                    System.Func<bool, int> f = dele$$gate(bool b) { i++; return 1; };
                }
            }
            """,
            Captures($"""

                {WorkspacesResources.Variables_captured_colon} i
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1516")]
    public Task QuickInfoWithNonStandardSeeAttributesAppear()
        => TestAsync(
            """
            class C
            {
                /// <summary>
                /// <see cref="System.String" />
                /// <see href="http://microsoft.com" />
                /// <see langword="null" />
                /// <see unsupported-attribute="cat" />
                /// </summary>
                void M()
                {
                    M$$();
                }
            }
            """,
            Documentation(@"string http://microsoft.com null cat"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6657")]
    public async Task OptionalParameterFromPreviousSubmission()
    {
        const string workspaceDefinition = """

            <Workspace>
                <Submission Language="C#" CommonReferences="true">
                    void M(int x = 1) { }
                </Submission>
                <Submission Language="C#" CommonReferences="true">
                    M(x$$: 2)
                </Submission>
            </Workspace>

            """;
        using var workspace = EditorTestWorkspace.Create(XElement.Parse(workspaceDefinition), workspaceKind: WorkspaceKind.Interactive);
        await TestWithOptionsAsync(workspace, MainDescription($"({FeaturesResources.parameter}) int x = 1"));
    }

    [Fact]
    public Task TupleProperty()
        => TestInMethodAsync(
            """
            interface I
            {
                (int, int) Name { get; set; }
            }

            class C : I
            {
                (int, int) I.Name$$
                {
                    get
                    {
                        throw new System.Exception();
                    }

                    set
                    {
                    }
                }
            }
            """,
            MainDescription("(int, int) C.Name { get; set; }"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task ValueTupleWithArity0VariableName()
        => TestAsync(
            """

            using System;
            public class C
            {
                void M()
                {
                    var y$$ = ValueTuple.Create();
                }
            }

            """,
            MainDescription($"({FeaturesResources.local_variable}) ValueTuple y"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task ValueTupleWithArity0ImplicitVar()
        => TestAsync(
            """

            using System;
            public class C
            {
                void M()
                {
                    var$$ y = ValueTuple.Create();
                }
            }

            """,
            MainDescription("struct System.ValueTuple"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task ValueTupleWithArity1VariableName()
        => TestAsync(
            """

            using System;
            public class C
            {
                void M()
                {
                    var y$$ = ValueTuple.Create(1);
                }
            }

            """,
            MainDescription($"({FeaturesResources.local_variable}) ValueTuple<int> y"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task ValueTupleWithArity1ImplicitVar()
        => TestAsync(
            """

            using System;
            public class C
            {
                void M()
                {
                    var$$ y = ValueTuple.Create(1);
                }
            }

            """,
            MainDescription("struct System.ValueTuple<System.Int32>"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task ValueTupleWithArity2VariableName()
        => TestAsync(
            """

            using System;
            public class C
            {
                void M()
                {
                    var y$$ = ValueTuple.Create(1, 1);
                }
            }

            """,
            MainDescription($"({FeaturesResources.local_variable}) (int, int) y"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task ValueTupleWithArity2ImplicitVar()
        => TestAsync(
            """

            using System;
            public class C
            {
                void M()
                {
                    var$$ y = ValueTuple.Create(1, 1);
                }
            }

            """,
            MainDescription("(int, int)"));

    [Fact]
    public Task TestRefMethod()
        => TestInMethodAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    ref int i = ref $$goo();
                }

                private static ref int goo()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            MainDescription("ref int Program.goo()"));

    [Fact]
    public Task TestRefLocal()
        => TestInMethodAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    ref int $$i = ref goo();
                }

                private static ref int goo()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) ref int i"));

    [Fact, WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=410932")]
    public Task TestGenericMethodInDocComment()
        => TestAsync(
            """

            class Test
            {
                T F<T>()
                {
                    F<T>();
                }

                /// <summary>
                /// <see cref="F$${T}()"/>
                /// </summary>
                void S()
                { }
            }

            """,
            MainDescription("T Test.F<T>()"));

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=403665&_a=edit")]
    public Task TestExceptionWithCrefToConstructorDoesNotCrash()
        => TestAsync(
            """

            class Test
            {
                /// <summary>
                /// </summary>
                /// <exception cref="Test.Test"/>
                public Test$$() {}
            }

            """,
            MainDescription("Test.Test()"));

    [Fact]
    public Task TestRefStruct()
        => TestAsync("ref struct X$$ {}", MainDescription("ref struct X"));

    [Fact]
    public Task TestRefStruct_Nested()
        => TestAsync("""

            namespace Nested
            {
                ref struct X$$ {}
            }
            """, MainDescription("ref struct Nested.X"));

    [Fact]
    public Task TestReadOnlyStruct()
        => TestAsync("readonly struct X$$ {}", MainDescription("readonly struct X"));

    [Fact]
    public Task TestReadOnlyStruct_Nested()
        => TestAsync("""

            namespace Nested
            {
                readonly struct X$$ {}
            }
            """, MainDescription("readonly struct Nested.X"));

    [Fact]
    public Task TestReadOnlyRefStruct()
        => TestAsync("readonly ref struct X$$ {}", MainDescription("readonly ref struct X"));

    [Fact]
    public Task TestReadOnlyRefStruct_Nested()
        => TestAsync("""

            namespace Nested
            {
                readonly ref struct X$$ {}
            }
            """, MainDescription("readonly ref struct Nested.X"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22450")]
    public Task TestRefLikeTypesNoDeprecated()
        => VerifyWithReferenceWorkerAsync("""

            <Workspace>
                <Project Language="C#" LanguageVersion="7.2" CommonReferences="true">
                    <MetadataReferenceFromSource Language="C#" LanguageVersion="7.2" CommonReferences="true">
                        <Document FilePath="ReferencedDocument">
            public ref struct TestRef
            {
            }
                        </Document>
                    </MetadataReferenceFromSource>
                    <Document FilePath="SourceDocument">
            ref struct Test
            {
                private $$TestRef _field;
            }
                    </Document>
                </Project>
            </Workspace>
            """, MainDescription($"ref struct TestRef"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public Task PropertyWithSameNameAsOtherType()
        => TestAsync(
            """
            namespace ConsoleApplication1
            {
                class Program
                {
                    static A B { get; set; }
                    static B A { get; set; }

                    static void Main(string[] args)
                    {
                        B = ConsoleApplication1.B$$.F();
                    }
                }
                class A { }
                class B
                {
                    public static A F() => null;
                }
            }
            """,
            MainDescription($"ConsoleApplication1.A ConsoleApplication1.B.F()"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2644")]
    public Task PropertyWithSameNameAsOtherType2()
        => TestAsync(
            """
            using System.Collections.Generic;

            namespace ConsoleApplication1
            {
                class Program
                {
                    public static List<Bar> Bar { get; set; }

                    static void Main(string[] args)
                    {
                        Tes$$t<Bar>();
                    }

                    static void Test<T>() { }
                }

                class Bar
                {
                }
            }
            """,
            MainDescription($"void Program.Test<Bar>()"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23883")]
    public Task InMalformedEmbeddedStatement_01()
        => TestAsync(
            """

            class Program
            {
                void method1()
                {
                    if (method2())
                        .Any(b => b.Content$$Type, out var chars)
                    {
                    }
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23883")]
    public Task InMalformedEmbeddedStatement_02()
        => TestAsync(
            """

            class Program
            {
                void method1()
                {
                    if (method2())
                        .Any(b => b$$.ContentType, out var chars)
                    {
                    }
                }
            }

            """,
            MainDescription($"({FeaturesResources.parameter}) ? b"));

    [Fact]
    public Task EnumConstraint()
        => TestInMethodAsync(
            """

            class X<T> where T : System.Enum
            {
                private $$T x;
            }
            """,
            MainDescription($"T {FeaturesResources.in_} X<T> where T : Enum"));

    [Fact]
    public Task DelegateConstraint()
        => TestInMethodAsync(
            """

            class X<T> where T : System.Delegate
            {
                private $$T x;
            }
            """,
            MainDescription($"T {FeaturesResources.in_} X<T> where T : Delegate"));

    [Fact]
    public Task MulticastDelegateConstraint()
        => TestInMethodAsync(
            """

            class X<T> where T : System.MulticastDelegate
            {
                private $$T x;
            }
            """,
            MainDescription($"T {FeaturesResources.in_} X<T> where T : MulticastDelegate"));

    [Fact]
    public Task UnmanagedConstraint_Type()
        => TestAsync(
            """

            class $$X<T> where T : unmanaged
            {
            }
            """,
            MainDescription("class X<T> where T : unmanaged"));

    [Fact]
    public Task UnmanagedConstraint_Method()
        => TestAsync(
            """

            class X
            {
                void $$M<T>() where T : unmanaged { }
            }
            """,
            MainDescription("void X.M<T>() where T : unmanaged"));

    [Fact]
    public Task UnmanagedConstraint_Delegate()
        => TestAsync(
            "delegate void $$D<T>() where T : unmanaged;",
            MainDescription("delegate void D<T>() where T : unmanaged"));

    [Fact]
    public Task UnmanagedConstraint_LocalFunction()
        => TestAsync(
            """

            class X
            {
                void N()
                {
                    void $$M<T>() where T : unmanaged { }
                }
            }
            """,
            MainDescription("void M<T>() where T : unmanaged"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29703")]
    public Task TestGetAccessorDocumentation()
        => TestAsync(
            """

            class X
            {
                /// <summary>Summary for property Goo</summary>
                int Goo { g$$et; set; }
            }
            """,
            Documentation("Summary for property Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29703")]
    public Task TestSetAccessorDocumentation()
        => TestAsync(
            """

            class X
            {
                /// <summary>Summary for property Goo</summary>
                int Goo { get; s$$et; }
            }
            """,
            Documentation("Summary for property Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29703")]
    public Task TestEventAddDocumentation1()
        => TestAsync(
            """

            using System;

            class X
            {
                /// <summary>Summary for event Goo</summary>
                event EventHandler<EventArgs> Goo
                {
                    a$$dd => throw null;
                    remove => throw null;
                }
            }
            """,
            Documentation("Summary for event Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29703")]
    public Task TestEventAddDocumentation2()
        => TestAsync(
            """

            using System;

            class X
            {
                /// <summary>Summary for event Goo</summary>
                event EventHandler<EventArgs> Goo;

                void M() => Goo +$$= null;
            }
            """,
            Documentation("Summary for event Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29703")]
    public Task TestEventRemoveDocumentation1()
        => TestAsync(
            """

            using System;

            class X
            {
                /// <summary>Summary for event Goo</summary>
                event EventHandler<EventArgs> Goo
                {
                    add => throw null;
                    r$$emove => throw null;
                }
            }
            """,
            Documentation("Summary for event Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29703")]
    public Task TestEventRemoveDocumentation2()
        => TestAsync(
            """

            using System;

            class X
            {
                /// <summary>Summary for event Goo</summary>
                event EventHandler<EventArgs> Goo;

                void M() => Goo -$$= null;
            }
            """,
            Documentation("Summary for event Goo"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30642")]
    public Task BuiltInOperatorWithUserDefinedEquivalent()
        => TestAsync(
            """

            class X
            {
                void N(string a, string b)
                {
                    var v = a $$== b;
                }
            }
            """,
            MainDescription("bool string.operator ==(string a, string b)"),
            SymbolGlyph(Glyph.OperatorPublic));

    [Fact]
    public Task NotNullConstraint_Type()
        => TestAsync(
            """

            class $$X<T> where T : notnull
            {
            }
            """,
            MainDescription("class X<T> where T : notnull"));

    [Fact]
    public Task NotNullConstraint_Method()
        => TestAsync(
            """

            class X
            {
                void $$M<T>() where T : notnull { }
            }
            """,
            MainDescription("void X.M<T>() where T : notnull"));

    [Fact]
    public Task MultipleConstraints_Type()
        => TestAsync(
            """

            class $$X<T, U> where T : notnull where U : notnull
            {
            }
            """,
            MainDescription("""
                class X<T, U>
                    where T : notnull
                    where U : notnull
                """));

    [Fact]
    public Task MultipleConstraints_Method()
        => TestAsync(
            """

            class X
            {
                void $$M<T, U>() where T : notnull where U : notnull { }
            }
            """,
            MainDescription("""
                void X.M<T, U>()
                    where T : notnull
                    where U : notnull
                """));

    [Fact]
    public Task NotNullConstraint_Delegate()
        => TestAsync(
            "delegate void $$D<T>() where T : notnull;",
            MainDescription("delegate void D<T>() where T : notnull"));

    [Fact]
    public Task NotNullConstraint_LocalFunction()
        => TestAsync(
            """

            class X
            {
                void N()
                {
                    void $$M<T>() where T : notnull { }
                }
            }
            """,
            MainDescription("void M<T>() where T : notnull"));

    [Fact]
    public Task NullableParameterThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                void N(string? s)
                {
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "s")));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42543")]
    public Task NullableParameterThatIsMaybeNull_Suppressed1()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                void N(string? s)
                {
                    string s2 = $$s!;
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "s")));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42543")]
    public Task NullableParameterThatIsMaybeNull_Suppressed2()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                void N(string? s)
                {
                    string s2 = $$s!!;
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "s")));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66854")]
    [InlineData("is null")]
    [InlineData("is not null")]
    [InlineData("is string")]
    [InlineData("is string s")]
    [InlineData("is object")]
    [InlineData("is object s")]
    [InlineData("is { }")]
    [InlineData("is 0")]
    [InlineData("== null")]
    [InlineData("!= null")]
    public Task NonNullValueCheckedAgainstNull_1(string test)
        => TestWithOptionsAsync(TestOptions.Regular8,
            $$"""
            #nullable enable

            public class Example
            {
                private void Main()
                {
                    var user = new object();

                    if ($$user {{test}})
                        throw new InvalidOperationException();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) object? user"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "user")));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66854")]
    [InlineData("is null", true)]
    [InlineData("is not null", true)]
    [InlineData("is string", false)]
    [InlineData("is string s", false)]
    [InlineData("is object", true)]
    [InlineData("is object s", false)]
    [InlineData("is { }", true)]
    [InlineData("is 0", false)]
    [InlineData("== null", true)]
    [InlineData("!= null", true)]
    public Task NonNullValueCheckedAgainstNull_2(string test, bool expectNullable)
        => TestWithOptionsAsync(TestOptions.Regular8,
            $$"""
            #nullable enable

            public class Example
            {
                private void Main()
                {
                    var user = new object();

                    if (user {{test}})
                        Console.WriteLine();

                    Console.WriteLine($$user);
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) object? user"),
            NullabilityAnalysis(expectNullable
                ? string.Format(FeaturesResources._0_may_be_null_here, "user")
                : string.Format(FeaturesResources._0_is_not_null_here, "user")));

    [Fact]
    public Task NullableParameterThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                void N(string? s)
                {
                    s = "";
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "s")));

    [Fact]
    public Task NullableParameterThatIsOblivious()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            class X
            {
                void N(string s)
                {
            #nullable enable
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) string s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "s")));

    [Fact]
    public Task NullableParameterThatIsOblivious_Propagated()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            class X
            {
                void N(string s)
                {
            #nullable enable
                    string s2 = s;
                    string s3 = $$s2;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s2"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "s2")));

    [Fact]
    public Task NullableFieldThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                string? s = null;

                void N()
                {
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) string? X.s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "s")));

    [Fact]
    public Task NullableFieldThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                string? s = null;

                void N()
                {
                    s = "";
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) string? X.s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "s")));

    [Fact]
    public Task NullableFieldThatIsOblivious()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            class X
            {
                string s = null;

                void N()
                {
                    s = "";
            #nullable enable
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) string X.s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "s")));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77219")]
    public Task NullableBackingFieldThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.RegularPreview,
            """
            #nullable enable

            class X
            {
                string? P
                {
                    get => $$field;
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) string? X.P.field"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "P.field")));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77219")]
    public Task NullableBackingFieldThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.RegularPreview,
            """
            #nullable enable

            class X
            {
                string P
                {
                    get => $$field;
                } = "a";
            }
            """,
            MainDescription($"({FeaturesResources.field}) string X.P.field"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "P.field")));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77219")]
    public Task NullableBackingFieldThatIsOblivious()
        => TestWithOptionsAsync(TestOptions.RegularPreview,
            """
            class X
            {
                string P
                {
            #nullable enable
                    get => $$field;
                } = "a";
            }
            """,
            MainDescription($"({FeaturesResources.field}) string X.P.field"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "P.field")));

    [Fact]
    public Task NullablePropertyThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                string? S { get; set; }

                void N()
                {
                    string s2 = $$S;
                }
            }
            """,
            MainDescription("string? X.S { get; set; }"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "S")));

    [Fact]
    public Task NullablePropertyThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            class X
            {
                string? S { get; set; }

                void N()
                {
                    S = "";
                    string s2 = $$S;
                }
            }
            """,
            MainDescription("string? X.S { get; set; }"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "S")));

    [Theory]
    [InlineData("")]
    [InlineData("S = null;")]
    [InlineData("S = string.Empty;")]
    public Task NullablePropertyThatIsOblivious(string code)
        => TestWithOptionsAsync(TestOptions.Regular8,
            $$"""
            class X
            {
                string S { get; set; }

                void N()
                {
                    {{code}}
            #nullable enable
                    string s2 = $$S;
                }
            }
            """,
            MainDescription("string X.S { get; set; }"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "S")));

    [Fact]
    public Task NullableRangeVariableThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    IEnumerable<string?> enumerable;

                    foreach (var s in enumerable)
                    {
                        string s2 = $$s;
                    }
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "s")));

    [Fact]
    public Task NullableRangeVariableThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    IEnumerable<string> enumerable;

                    foreach (var s in enumerable)
                    {
                        string s2 = $$s;
                    }
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "s")));

    [Fact]
    public Task NullableRangeVariableThatIsOblivious()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            using System.Collections.Generic;
            class X
            {
                void N()
                {
                    IEnumerable<string> enumerable;
                    foreach (string s in enumerable)
                    {
            #nullable enable
                        string s2 = $$s;
                    }
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "s")));

    [Fact]
    public Task NullableLocalThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    string? s = null;
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "s")));

    [Fact]
    public Task NullableLocalThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    string? s = "";
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string? s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "s")));

    [Fact]
    public Task NullableLocalThatIsOblivious()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            class X
            {
                void N()
                {
                    string s = null;
            #nullable enable
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "s")));

    [Theory]
    [InlineData("#nullable enable warnings")]
    [InlineData("#nullable enable annotations")]
    public Task NullableLocalThatIsOblivious_NotFullyNullableEnabled(string directive)
        => TestWithOptionsAsync(TestOptions.Regular8,
            $$"""
            class X
            {
                void N()
                {
                    string s = null;
            {{directive}}
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableMethodThatIsMaybeNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable
            class X
            {
                string? M() => null;
                void N()
                {
                    string? s = $$M();
                }
            }
            """,
            MainDescription("string? X.M()"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableMethodThatIsNotNull()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable
            class X
            {
                string M() => "";
                void N()
                {
                    string s = $$M();
                }
            }
            """,
            MainDescription("string X.M()"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableMethodThatIsOblivious()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            class X
            {
                string M() => "";
                void N()
                {
            #nullable enable
                    string s = $$M();
                }
            }
            """,
            MainDescription("string X.M()"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_nullable_aware, "M")));

    [Fact]
    public Task NullableMethodThatIsVoid()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable
            class X
            {
                void M() { }
                void N()
                {
                    string s = $$M();
                }
            }
            """,
            MainDescription("void X.M()"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableMethodThatIsVoidAndOblivious()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            class X
            {
                void M() { }
                void N()
                {
            #nullable enable
                    string s = $$M();
                }
            }
            """,
            MainDescription("void X.M()"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableNotShownPriorToLanguageVersion8()
        => TestWithOptionsAsync(TestOptions.Regular7_3,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    string s = "";
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableNotShownInNullableDisable()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable disable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    string s = "";
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableShownWhenEnabledGlobally()
        => TestWithOptionsAsync(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
            """
            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    string s = "";
                    string s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string s"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "s")));

    [Fact]
    public Task NullableNotShownForValueType()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    int a = 0;
                    int b = $$a;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int a"),
            NullabilityAnalysis(""));

    [Fact]
    public Task NullableNotShownForConst()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """
            #nullable enable

            using System.Collections.Generic;

            class X
            {
                void N()
                {
                    const string? s = null;
                    string? s2 = $$s;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_constant}) string? s = null"),
            NullabilityAnalysis(""));

    [Fact]
    public Task TestInheritdocInlineSummary()
        => TestInClassAsync("""

            /// <summary>Summary documentation</summary>
            /// <remarks>Remarks documentation</remarks>
            void M(int x) { }

            /// <summary><inheritdoc cref="M(int)"/></summary>
            void $$M(int x, int y) { }
            """,
            MainDescription("void C.M(int x, int y)"),
            Documentation("Summary documentation"));

    [Fact]
    public Task TestInheritdocTwoLevels1()
        => TestInClassAsync("""

            /// <summary>Summary documentation</summary>
            /// <remarks>Remarks documentation</remarks>
            void M() { }

            /// <inheritdoc cref="M()"/>
            void M(int x) { }

            /// <inheritdoc cref="M(int)"/>
            void $$M(int x, int y) { }
            """,
            MainDescription("void C.M(int x, int y)"),
            Documentation("Summary documentation"));

    [Fact]
    public Task TestInheritdocTwoLevels2()
        => TestInClassAsync("""

            /// <summary>Summary documentation</summary>
            /// <remarks>Remarks documentation</remarks>
            void M() { }

            /// <summary><inheritdoc cref="M()"/></summary>
            void M(int x) { }

            /// <summary><inheritdoc cref="M(int)"/></summary>
            void $$M(int x, int y) { }
            """,
            MainDescription("void C.M(int x, int y)"),
            Documentation("Summary documentation"));

    [Fact]
    public Task TestInheritdocWithTypeParamRef()
        => TestInClassAsync("""

            public class Program
            {
                public static void Main() => _ = new Test<int>().$$Clone();
            }

            public class Test<T> : ICloneable<Test<T>>
            {
            	/// <inheritdoc/>
            	public Test<T> Clone() => new();
            }

            /// <summary>A type that has clonable instances.</summary>
            /// <typeparam name="T">The type of instances that can be cloned.</typeparam>
            public interface ICloneable<T>
            {
                /// <summary>Clones a <typeparamref name="T"/>.</summary>
                /// <returns>A clone of the <typeparamref name="T"/>.</returns>
                public T Clone();
            }
            """,
            MainDescription("Test<int> Test<int>.Clone()"),
            Documentation("Clones a Test<T>."));

    [Fact]
    public Task TestInheritdocWithTypeParamRef1()
        => TestWithOptionsAsync(TestOptions.Regular8,
            """

            public interface ITest
            {
                /// <summary>
                /// A generic method <typeparamref name="T"/>.
                /// </summary>
                /// <typeparam name="T">A generic type.</typeparam>
                void Foo<T>();
            }

            public class Test : ITest
            {
                /// <inheritdoc/>
                public void $$Foo<T>() { }
            }
            """,
            MainDescription($"void Test.Foo<T>()"),
            Documentation("A generic method T."),
            item => Assert.Equal(
                item.Sections.First(section => section.Kind == QuickInfoSectionKinds.DocumentationComments).TaggedParts.Select(p => p.Tag).ToArray(),
                ["Text", "Space", "TypeParameter", "Text"]));

    [Fact]
    public Task TestInheritdocCycle1()
        => TestInClassAsync("""

            /// <inheritdoc cref="M(int, int)"/>
            void M(int x) { }

            /// <inheritdoc cref="M(int)"/>
            void $$M(int x, int y) { }
            """,
            MainDescription("void C.M(int x, int y)"),
            Documentation(""));

    [Fact]
    public Task TestInheritdocCycle2()
        => TestInClassAsync("""

            /// <inheritdoc cref="M(int)"/>
            void $$M(int x) { }
            """,
            MainDescription("void C.M(int x)"),
            Documentation(""));

    [Fact]
    public Task TestInheritdocCycle3()
        => TestInClassAsync("""

            /// <inheritdoc cref="M"/>
            void $$M(int x) { }
            """,
            MainDescription("void C.M(int x)"),
            Documentation(""));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38794")]
    public Task TestLinqGroupVariableDeclaration()
        => TestInClassAsync("""

            void M(string[] a)
            {
                var v = from x in a
                        group x by x.Length into $$g
                        select g;
            }
            """,
            MainDescription($"({FeaturesResources.range_variable}) IGrouping<int, string> g"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38283")]
    public Task QuickInfoOnIndexerCloseBracket()
        => TestAsync("""

            class C
            {
                public int this[int x] { get { return 1; } }

                void M()
                {
                    var x = new C()[5$$];
                }
            }
            """,
        MainDescription("int C.this[int x] { get; }"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38283")]
    public Task QuickInfoOnIndexerOpenBracket()
        => TestAsync("""

            class C
            {
                public int this[int x] { get { return 1; } }

                void M()
                {
                    var x = new C()$$[5];
                }
            }
            """,
        MainDescription("int C.this[int x] { get; }"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38283")]
    public Task QuickInfoOnIndexer_NotOnArrayAccess()
        => TestAsync("""

            class Program
            {
                void M()
                {
                    int[] x = new int[4];
                    int y = x[3$$];
                }
            }
            """,
            MainDescription("struct System.Int32"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31618")]
    public Task QuickInfoWithRemarksOnMethod()
        => TestAsync("""

            class Program
            {
                /// <summary>
                /// Summary text
                /// </summary>
                /// <remarks>
                /// Remarks text
                /// </remarks>
                int M()
                {
                    return $$M();
                }
            }
            """,
            MainDescription("int Program.M()"),
            Documentation("Summary text"),
            Remarks("""

                Remarks text
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31618")]
    public Task QuickInfoWithRemarksOnPropertyAccessor()
        => TestAsync("""

            class Program
            {
                /// <summary>
                /// Summary text
                /// </summary>
                /// <remarks>
                /// Remarks text
                /// </remarks>
                int M { $$get; }
            }
            """,
            MainDescription("int Program.M.get"),
            Documentation("Summary text"),
            Remarks("""

                Remarks text
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31618")]
    public Task QuickInfoWithReturnsOnMethod()
        => TestAsync("""

            class Program
            {
                /// <summary>
                /// Summary text
                /// </summary>
                /// <returns>
                /// Returns text
                /// </returns>
                int M()
                {
                    return $$M();
                }
            }
            """,
            MainDescription("int Program.M()"),
            Documentation("Summary text"),
            Returns($"""

                {FeaturesResources.Returns_colon}
                  Returns text
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31618")]
    public Task QuickInfoWithReturnsOnPropertyAccessor()
        => TestAsync("""

            class Program
            {
                /// <summary>
                /// Summary text
                /// </summary>
                /// <returns>
                /// Returns text
                /// </returns>
                int M { $$get; }
            }
            """,
            MainDescription("int Program.M.get"),
            Documentation("Summary text"),
            Returns($"""

                {FeaturesResources.Returns_colon}
                  Returns text
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31618")]
    public Task QuickInfoWithValueOnMethod()
        => TestAsync("""

            class Program
            {
                /// <summary>
                /// Summary text
                /// </summary>
                /// <value>
                /// Value text
                /// </value>
                int M()
                {
                    return $$M();
                }
            }
            """,
            MainDescription("int Program.M()"),
            Documentation("Summary text"),
            Value($"""

                {FeaturesResources.Value_colon}
                  Value text
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31618")]
    public Task QuickInfoWithValueOnPropertyAccessor()
        => TestAsync("""

            class Program
            {
                /// <summary>
                /// Summary text
                /// </summary>
                /// <value>
                /// Value text
                /// </value>
                int M { $$get; }
            }
            """,
            MainDescription("int Program.M.get"),
            Documentation("Summary text"),
            Value($"""

                {FeaturesResources.Value_colon}
                  Value text
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task QuickInfoNotPattern1()
        => TestAsync("""

            class Person
            {
                void Goo(object o)
                {
                    if (o is not $$Person p)
                    {
                    }
                }
            }
            """,
            MainDescription("class Person"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task QuickInfoNotPattern2()
        => TestAsync("""

            class Person
            {
                void Goo(object o)
                {
                    if (o is $$not Person p)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task QuickInfoOrPattern1()
        => TestAsync("""

            class Person
            {
                void Goo(object o)
                {
                    if (o is $$Person or int)
                    {
                    }
                }
            }
            """, MainDescription("class Person"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task QuickInfoOrPattern2()
        => TestAsync("""

            class Person
            {
                void Goo(object o)
                {
                    if (o is Person or $$int)
                    {
                    }
                }
            }
            """, MainDescription("struct System.Int32"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task QuickInfoOrPattern3()
        => TestAsync("""

            class Person
            {
                void Goo(object o)
                {
                    if (o is Person $$or int)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task QuickInfoRecord()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            record Person(string First, string Last)
            {
                void M($$Person p)
                {
                }
            }
            """, MainDescription("record Person"));

    [Fact]
    public Task QuickInfoDerivedRecord()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            record Person(string First, string Last)
            {
            }
            record Student(string Id)
            {
                void M($$Student p)
                {
                }
            }

            """, MainDescription("record Student"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44904")]
    public Task QuickInfoRecord_BaseTypeList()
        => TestAsync("""

            record Person(string First, string Last);
            record Student(int Id) : $$Person(null, null);

            """, MainDescription("Person.Person(string First, string Last)"));

    [Fact]
    public Task QuickInfoClass_BaseTypeList()
        => TestAsync("""

            class Person(string First, string Last);
            class Student(int Id) : $$Person(null, null);

            """, MainDescription("Person.Person(string First, string Last)"));

    [Fact]
    public Task QuickInfo_BaseConstructorInitializer()
        => TestAsync("""

            public class Person { public Person(int id) { } }
            public class Student : Person { public Student() : $$base(0) { } }

            """, MainDescription("Person.Person(int id)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57031")]
    public Task QuickInfo_DotInInvocation()
        => TestAsync("""

            public class C
            {
                public void M(int a) { }
                public void M(int a, params int[] b) { }
            }

            class Program
            {
                static void Main()
                {
                    var c = new C();
                    c$$.M(1, 2);
                }
            }
            """,
            MainDescription($"void C.M(int a, params int[] b) (+ 1 {FeaturesResources.overload})"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57031")]
    public Task QuickInfo_BeforeMemberNameInInvocation()
        => TestAsync("""

            public class C
            {
                public void M(int a) { }
                public void M(int a, params int[] b) { }
            }

            class Program
            {
                static void Main()
                {
                    var c = new C();
                    c.$$M(1, 2);
                }
            }
            """,
            MainDescription($"void C.M(int a, params int[] b) (+ 1 {FeaturesResources.overload})"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57031")]
    public Task QuickInfo_AfterMemberNameInInvocation()
        => TestAsync("""

            public class C
            {
                public void M(int a) { }
                public void M(int a, params int[] b) { }
            }

            class Program
            {
                static void Main()
                {
                    var c = new C();
                    c.M$$(1, 2);
                }
            }
            """,
            MainDescription($"void C.M(int a, params int[] b) (+ 1 {FeaturesResources.overload})"));

    [Fact]
    public Task QuickInfoRecordClass()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            record class Person(string First, string Last)
            {
                void M($$Person p)
                {
                }
            }
            """, MainDescription("record Person"));

    [Fact]
    public Task QuickInfoRecordStruct()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            record struct Person(string First, string Last)
            {
                void M($$Person p)
                {
                }
            }
            """, MainDescription("record struct Person"));

    [Fact]
    public Task QuickInfoReadOnlyRecordStruct()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            readonly record struct Person(string First, string Last)
            {
                void M($$Person p)
                {
                }
            }
            """, MainDescription("readonly record struct Person"));

    [Fact]
    public Task QuickInfoRecordProperty()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            /// <param name="First">The person's first name.</param>
            record Person(string First, string Last)
            {
                void M(Person p)
                {
                    _ = p.$$First;
                }
            }
            """,
            MainDescription("string Person.First { get; init; }"),
            Documentation("The person's first name."));

    [Fact]
    public Task QuickInfoFieldKeyword()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.Preview),
            """
            class C
            {
                int Prop
                {
                    get => $$field;
                    set => field = value;
                }
            }
            """,
            MainDescription("(field) int C.Prop.field"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51615")]
    public Task TestVarPatternOnVarKeyword()
        => TestAsync(
            """
            class C
            {
                string M() { }

                void M2()
                {
                  if (M() is va$$r x && x.Length > 0)
                  {
                  }
                }
            }
            """,
            MainDescription("class System.String"));

    [Fact]
    public Task TestVarPatternOnVariableItself()
        => TestAsync(
            """
            class C
            {
                string M() { }

                void M2()
                {
                  if (M() is var x$$ && x.Length > 0)
                  {
                  }
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string? x"));

    [Fact]
    public Task TestVarPatternOnVarKeyword_InListPattern()
        => TestAsync(
            """
            class C
            {
                void M(char[] array)
                {
                  if (array is [ va$$r one ])
                  {
                  }
                }
            }
            """,
            MainDescription("struct System.Char"));

    [Fact]
    public Task TestVarPatternOnVariableItself_InListPattern()
        => TestAsync(
            """
            class C
            {
                void M(char[] array)
                {
                  if (array is [ var o$$ne ])
                  {
                  }
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) char one"));

    [Fact]
    public Task TestVarPatternOnVarKeyword_InSlicePattern()
        => TestAsync(
            """
            class C
            {
                void M(char[] array)
                {
                  if (array is [..va$$r one ])
                  {
                  }
                }
            }
            """ + TestSources.Index + TestSources.Range,
            MainDescription("char[]"));

    [Fact]
    public Task TestVarPatternOnVariableItself_InSlicePattern()
        => TestAsync(
            """
            class C
            {
                void M(char[] array)
                {
                  if (array is [ ..var o$$ne ])
                  {
                  }
                }
            }
            """ + TestSources.Index + TestSources.Range,
            MainDescription($"({FeaturesResources.local_variable}) char[]? one"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53135")]
    public Task TestDocumentationCData()
        => TestAsync("""
            using I$$ = IGoo;
            /// <summary>
            /// summary for interface IGoo
            /// <code><![CDATA[
            /// List<string> y = null;
            /// ]]></code>
            /// </summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation("""
                summary for interface IGoo

                List<string> y = null;
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53384")]
    public Task TestDocumentationCData2()
        => TestAsync("""
            using I$$ = IGoo;
            /// <summary>
            /// summary for interface IGoo
            /// <code><![CDATA[
            /// void M()
            /// {
            ///     Console.WriteLine();
            /// }
            /// ]]></code>
            /// </summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation("""
                summary for interface IGoo

                void M()
                {
                    Console.WriteLine();
                }
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53384")]
    public Task TestDocumentationCData3()
        => TestAsync("""
            using I$$ = IGoo;
            /// <summary>
            /// summary for interface IGoo
            /// <![CDATA[
            /// void M()
            /// {
            ///     Console.WriteLine();
            /// }
            /// ]]>
            /// </summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation("""
                summary for interface IGoo

                void M()
                {
                    Console.WriteLine();
                }
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37503")]
    public Task DoNotNormalizeWhitespaceForCode()
        => TestAsync("""
            using I$$ = IGoo;
            /// <summary>
            /// Normalize    this, and <c>Also        this</c>
            /// <code>
            /// line 1
            /// line     2
            /// </code>
            /// </summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation("""
                Normalize this, and Also this

                line 1
                line     2
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57262")]
    public Task DoNotNormalizeLeadingWhitespaceForCode()
        => TestAsync("""
            using I$$ = IGoo;
            /// <summary>
            ///       Normalize    this, and <c>Also        this</c>
            /// <code>
            /// line 1
            ///     line     2
            /// </code>
            /// </summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation("""
                Normalize this, and Also this

                line 1
                    line     2
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57262")]
    public Task ParsesEmptySummary()
        => TestAsync("""
            using I$$ = IGoo;
            /// <summary></summary>
            interface IGoo {  }
            """,
            MainDescription("interface IGoo"),
            Documentation(""));

    [Fact]
    public Task TestStaticAbstract_ImplicitImplementation()
        => TestAsync(
            """

            interface I1
            {
                /// <summary>Summary text</summary>
                static abstract void M1();
            }

            class C1_1 : I1
            {
                public static void $$M1() { }
            }

            """,
            MainDescription("void C1_1.M1()"),
            Documentation("Summary text"));

    [Fact]
    public Task TestStaticAbstract_ImplicitImplementation_FromReference()
        => TestAsync(
            """

            interface I1
            {
                /// <summary>Summary text</summary>
                static abstract void M1();
            }

            class C1_1 : I1
            {
                public static void M1() { }
            }

            class R
            {
                public static void M() { C1_1.$$M1(); }
            }

            """,
            MainDescription("void C1_1.M1()"),
            Documentation("Summary text"));

    [Fact]
    public Task TestStaticAbstract_FromTypeParameterReference()
        => TestAsync(
            """

            interface I1
            {
                /// <summary>Summary text</summary>
                static abstract void M1();
            }

            class R
            {
                public static void M<T>() where T : I1 { T.$$M1(); }
            }

            """,
            MainDescription("void I1.M1()"),
            Documentation("Summary text"));

    [Fact]
    public Task TestStaticAbstract_ExplicitInheritdoc_ImplicitImplementation()
        => TestAsync(
            """

            interface I1
            {
                /// <summary>Summary text</summary>
                static abstract void M1();
            }

            class C1_1 : I1
            {
                /// <inheritdoc/>
                public static void $$M1() { }
            }

            """,
            MainDescription("void C1_1.M1()"),
            Documentation("Summary text"));

    [Fact]
    public Task TestStaticAbstract_ExplicitImplementation()
        => TestAsync(
            """

            interface I1
            {
                /// <summary>Summary text</summary>
                static abstract void M1();
            }

            class C1_1 : I1
            {
                static void I1.$$M1() { }
            }

            """,
            MainDescription("void C1_1.M1()"),
            Documentation("Summary text"));

    [Fact]
    public Task TestStaticAbstract_ExplicitInheritdoc_ExplicitImplementation()
        => TestAsync(
            """

            interface I1
            {
                /// <summary>Summary text</summary>
                static abstract void M1();
            }

            class C1_1 : I1
            {
                /// <inheritdoc/>
                static void I1.$$M1() { }
            }

            """,
            MainDescription("void C1_1.M1()"),
            Documentation("Summary text"));

    [Fact]
    public Task QuickInfoLambdaReturnType_01()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            class Program
            {
                System.Delegate D = bo$$ol () => true;
            }
            """,
            MainDescription("struct System.Boolean"));

    [Fact]
    public Task QuickInfoLambdaReturnType_02()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            class A
            {
                struct B { }
                System.Delegate D = A.B$$ () => null;
            }
            """,
            MainDescription("struct A.B"));

    [Fact]
    public Task QuickInfoLambdaReturnType_03()
        => TestWithOptionsAsync(
            Options.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
            """
            class A<T>
            {
            }
            struct B
            {
                System.Delegate D = A<B$$> () => null;
            }
            """,
            MainDescription("struct B"));

    [Fact]
    public Task TestNormalFuncSynthesizedLambdaType()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    $$var v = (int i) => i.ToString();
                }
            }
            """,
            MainDescription("delegate TResult System.Func<in T, out TResult>(T arg)"),
            TypeParameterMap($"""

                T {FeaturesResources.is_} int
                TResult {FeaturesResources.is_} string
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58871")]
    public Task TestInferredNonAnonymousDelegateType1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    $$var v = (int i) => i.ToString();
                }
            }
            """,
            MainDescription("delegate TResult System.Func<in T, out TResult>(T arg)"),
            AnonymousTypes(""));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58871")]
    public Task TestAnonymousSynthesizedLambdaType()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    $$var v = (ref int i) => i.ToString();
                }
            }
            """,
            MainDescription("delegate string <anonymous delegate>(ref int arg)"),
            AnonymousTypes(""));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58871")]
    public Task TestAnonymousSynthesizedLambdaType2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var $$v = (ref int i) => i.ToString();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) 'a v"),
            AnonymousTypes(
                $"""

                {FeaturesResources.Types_colon}
                    'a {FeaturesResources.is_} delegate string (ref int arg)
                """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58871")]
    public Task TestAnonymousSynthesizedLambdaType3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var v = (ref int i) => i.ToString();
                    $$Goo(v);
                }

                T Goo<T>(T t) => default;
            }
            """,
            MainDescription("'a C.Goo<'a>('a t)"),
            AnonymousTypes(
                $"""

                {FeaturesResources.Types_colon}
                    'a {FeaturesResources.is_} delegate string (ref int arg)
                """));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType4()
        => TestAsync(
            """

            class C
            {
                void M()
                {
                    var lam = (int param = 42) => param + 1;
                    $$lam();
                }
            }

            """,
            MainDescription($"({FeaturesResources.local_variable}) 'a lam"),
AnonymousTypes(
    $"""

    {FeaturesResources.Types_colon}
        'a {FeaturesResources.is_} delegate int (int arg = 42)
    """));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType5()
        => TestAsync(
            """

            class C
            {
                void M()
                {
                    $$var lam = (int param = 42) => param;
                }
            }

            """, MainDescription("delegate int <anonymous delegate>(int arg = 42)"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType6()
        => TestAsync(
            """

            class C
            {
                void M()
                {
                    var lam = (i$$nt param = 42) => param;
                }
            }

            """, MainDescription("struct System.Int32"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType7()
        => TestAsync(
            """

            class C
            {
                void M()
                {
                    var lam = (int pa$$ram = 42) => param;
                }
            }

            """, MainDescription($"({FeaturesResources.parameter}) int param = 42"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType8()
        => TestAsync(
            """

            class C
            {
                void M()
                {
                    var lam = (int param = 4$$2) => param;
                }
            }

            """, MainDescription("struct System.Int32"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType9()
        => TestAsync("""
            class C
            {
                void M()
                {
                    var lam = (params int[] xs) => xs.Length;
                    $$lam();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) 'a lam"),
            AnonymousTypes($"""

            {FeaturesResources.Types_colon}
                'a {FeaturesResources.is_} delegate int (params int[] arg)
            """));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType10()
        => TestAsync("""
            class C
            {
                void M()
                {
                    $$var lam = (params int[] xs) => xs.Length;
                }
            }
            """,
            MainDescription("delegate int <anonymous delegate>(params int[] arg)"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType11()
        => TestAsync("""
            class C
            {
                void M()
                {
                    var lam = (params i$$nt[] xs) => xs.Length;
                }
            }
            """,
            MainDescription("struct System.Int32"));

    [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public Task TestAnonymousSynthesizedLambdaType12()
        => TestAsync("""
        class C
        {
            void M()
            {
                var lam = (params int[] x$$s) => xs.Length;
            }
        }
        """,
        MainDescription($"({FeaturesResources.parameter}) params int[] xs"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61320")]
    public Task TestSingleTupleType()
        => TestInClassAsync(
            """
            void M((int x, string y) t) { }
              void N()
              {
                $$M(default);
              }
            """,
            MainDescription(@"void C.M((int x, string y) t)"),
            NoTypeParameterMap,
            AnonymousTypes(string.Empty));

    [Fact]
    public Task TestMultipleTupleTypesSameType()
        => TestInClassAsync(
            """
            void M((int x, string y) s, (int x, string y) t) { }
              void N()
              {
                $$M(default);
              }
            """,
            MainDescription(@"void C.M('a s, 'a t)"),
            NoTypeParameterMap,
            AnonymousTypes($"""

                {FeaturesResources.Types_colon}
                    'a {FeaturesResources.is_} (int x, string y)
                """));

    [Fact]
    public Task TestMultipleTupleTypesDifferentTypes1()
        => TestInClassAsync(
            """
            void M((int x, string y) s, (int a, string b) u) { }
              void N()
              {
                $$M(default);
              }
            """,
            MainDescription(@"void C.M((int x, string y) s, (int a, string b) u)"),
            NoTypeParameterMap);

    [Fact]
    public Task TestMultipleTupleTypesDifferentTypes2()
        => TestInClassAsync(
            """
            void M((int x, string y) s, (int x, string y) t, (int a, string b) u, (int a, string b) v) { }
              void N()
              {
                $$M(default);
              }
            """,
            MainDescription(@"void C.M('a s, 'a t, 'b u, 'b v)"),
            NoTypeParameterMap,
            AnonymousTypes($"""

                {FeaturesResources.Types_colon}
                    'a {FeaturesResources.is_} (int x, string y)
                    'b {FeaturesResources.is_} (int a, string b)
                """));

    [Fact]
    public Task TestMultipleTupleTypesDifferentTypes3()
        => TestInClassAsync(
            """
            void M((int x, string y) s, (int x, string y) t, (int a, string b) u) { }
              void N()
              {
                $$M(default);
              }
            """,
            MainDescription(@"void C.M('a s, 'a t, 'b u)"),
            NoTypeParameterMap,
            AnonymousTypes($"""

                {FeaturesResources.Types_colon}
                    'a {FeaturesResources.is_} (int x, string y)
                    'b {FeaturesResources.is_} (int a, string b)
                """));

    [Fact]
    public Task TestMultipleTupleTypesInference()
        => TestInClassAsync(
            """
            T M<T>(T t) { }
              void N()
              {
                (int a, string b) x = default;
                $$M(x);
              }
            """,
            MainDescription(@"'a C.M<'a>('a t)"),
            NoTypeParameterMap,
            AnonymousTypes($"""

                {FeaturesResources.Types_colon}
                    'a {FeaturesResources.is_} (int a, string b)
                """));

    [Fact]
    public Task TestAnonymousTypeWithTupleTypesInference1()
        => TestInClassAsync(
            """
            T M<T>(T t) { }
              void N()
              {
                var v = new { x = default((int a, string b)) };
                $$M(v);
              }
            """,
            MainDescription(@"'a C.M<'a>('a t)"),
            NoTypeParameterMap,
            AnonymousTypes($$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { (int a, string b) x }
                """));

    [Fact]
    public Task TestAnonymousTypeWithTupleTypesInference2()
        => TestInClassAsync(
            """
            T M<T>(T t) { }
              void N()
              {
                var v = new { x = default((int a, string b)), y = default((int a, string b)) };
                $$M(v);
              }
            """,
            MainDescription(@"'a C.M<'a>('a t)"),
            NoTypeParameterMap,
            AnonymousTypes($$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { 'b x, 'b y }
                    'b {{FeaturesResources.is_}} (int a, string b)
                """));

    [Fact]
    public Task TestInRawStringInterpolation_SingleLine()
        => TestInMethodAsync(
            """"
            var x = 1;
            var s = $"""Hello world {$$x}"""
            """",
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestInRawStringInterpolation_SingleLine_MultiBrace()
        => TestInMethodAsync(
            """"
            var x = 1;
            var s = ${|#0:|}$"""Hello world {{$$x}}"""
            """",
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestInRawStringLiteral_SingleLine_Const()
        => TestInClassAsync(
            """"
            const string $$s = """Hello world"""
            """",
            MainDescription($""""
                ({FeaturesResources.constant}) string C.s = """Hello world"""
                """"));

    [Fact]
    public Task TestInRawStringInterpolation_MultiLine()
        => TestInMethodAsync(
            """"
            var x = 1;
            var s = $"""
            Hello world {$$x}
            """
            """",
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestInRawStringInterpolation_MultiLine_MultiBrace()
        => TestInMethodAsync(
            """"
            var x = 1;
            var s = ${|#0:|}$"""
            Hello world {{$$x}}
            """
            """",
            MainDescription($"({FeaturesResources.local_variable}) int x"));

    [Fact]
    public Task TestInRawStringLiteral_MultiLine_Const()
        => TestInClassAsync(
            """"
            const string $$s = """
                    Hello world
                """
            """",
            MainDescription($""""
                ({FeaturesResources.constant}) string C.s = """
                        Hello world
                    """
                """"));

    [Fact]
    public Task TestArgsInTopLevel()
        => TestWithOptionsAsync(
            Options.Regular, """

            forach (var arg in $$args)
            {
            }

            """,
            MainDescription($"({FeaturesResources.parameter}) string[] args"));

    [Fact]
    public Task TestArgsInNormalProgram()
        => TestAsync("""

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var arg in $$args)
                    {
                    }
                }
            }

            """,
            MainDescription($"({FeaturesResources.parameter}) string[] args"));

    [Fact]
    public Task TestParameterInMethodAttributeNameof()
        => TestWithOptionsAsync(Options.Regular.WithLanguageVersion(LanguageVersion.CSharp11), """

            class Program
            {
                [My(nameof($$s))]
                void M(string s) { }
            }

            """,
            MainDescription($"({FeaturesResources.parameter}) string s"));

    [Fact]
    public Task TestParameterInMethodParameterAttributeNameof()
        => TestWithOptionsAsync(Options.Regular.WithLanguageVersion(LanguageVersion.CSharp11), """

            class Program
            {
                void M([My(nameof($$s))] string s) { }
            }

            """,
            MainDescription($"({FeaturesResources.parameter}) string s"));

    [Fact]
    public Task TestParameterInLocalFunctionAttributeNameof()
        => TestWithOptionsAsync(Options.Regular.WithLanguageVersion(LanguageVersion.CSharp11), """

            class Program
            {
                void M()
                {
                    [My(nameof($$s))]
                    void local(string s) { }
                }
            }

            """,
            MainDescription($"({FeaturesResources.parameter}) string s"));

    [Fact]
    public Task TestScopedParameter()
        => TestAsync("""
            ref struct R { }
            class Program
            {
                static void F(R r1, scoped R r2, ref R r3, scoped ref R r4, in R r5, scoped in R r6, out R r7, scoped out R r8)
                {
                    r7 = default;
                    r8 = default;
                }
                static void Main()
                {
                    R r = default;
                    $$F(r, r, ref r, ref r, r, r, out r, out r);
                }
            }
            """,
            MainDescription($"void Program.F(R r1, scoped R r2, ref R r3, scoped ref R r4, in R r5, scoped in R r6, out R r7, out R r8)"));

    [Fact]
    public Task TestScopedLocal()
        => TestAsync("""
            class Program
            {
                static void Main()
                {
                    int i = 0;
                    scoped ref int r = ref i;
                    i = $$r;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) scoped ref int r"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66854")]
    public Task TestNullableRefTypeVar1()
        => TestAsync("""
            #nullable enable

            class C
            {
                void M()
                {
                    object? o = null;
                    $$var s = (string?)o;
                }
            }
            """,
            MainDescription($"class System.String?"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66854")]
    public Task TestNullableRefTypeVar2()
        => TestAsync("""
            #nullable disable

            class C
            {
                void M()
                {
                    $$var s = GetNullableString();
                }

                #nullable enable

                string? GetNullableString() => null;

                #nullable restore
            }
            """,
            MainDescription($"class System.String"));

    [Fact]
    public Task TestUsingAliasToType1()
        => TestAsync(@"using X = $$int;",
            MainDescription($"struct System.Int32"));

    [Fact]
    public Task TestUsingAliasToType1_A()
        => TestAsync(@"using $$X = int;",
            MainDescription($"struct System.Int32"));

    [Fact]
    public Task TestUsingAliasToType2()
        => TestAsync(@"using X = ($$int a, int b);",
            MainDescription($"struct System.Int32"));

    [Fact]
    public Task TestUsingAliasToType2_A()
        => TestAsync(@"using $$X = (int a, int b);",
            MainDescription($"(int a, int b)"));

    [Fact]
    public Task TestUsingAliasToType3()
        => TestAsync(@"using X = $$(int a, int b);");

    [Fact]
    public Task TestUsingAliasToType4()
        => TestAsync(@"using unsafe X = $$delegate*<int,int>;");

    [Fact]
    public Task TestUsingAliasToType4_A()
        => TestAsync(@"using unsafe $$X = delegate*<int,int>;",
            MainDescription($"delegate*<int, int>"));

    [Fact]
    public Task TestUsingAliasToType5()
        => TestAsync(@"using unsafe X = $$int*;",
            MainDescription($"struct System.Int32"));

    [Fact]
    public Task TestUsingAliasToType5_A()
        => TestAsync(@"using unsafe $$X = int*;",
            MainDescription($"int*"));

    [Fact]
    public Task TestCollectionExpression_Start()
        => TestAsync("int[] x = $$[1, 2]",
            MainDescription($"int[]"));

    [Fact]
    public Task TestCollectionExpression_Middle()
        => TestAsync("int[] x = [1 $$, 2]");

    [Fact]
    public Task TestCollectionExpression_End()
        => TestAsync("int[] x = [1, 2]$$",
            MainDescription($"int[]"));

    [Fact]
    public Task TestCollectionExpression_Start_Typeless()
        => TestAsync("var x = $$[1, 2]");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71638")]
    public Task TestAnonymousType()
        => VerifyWithMscorlib45Async("""
            _ = new
            {
                @string = ""
            }.$$@string;
            """,
        [
            MainDescription($"string 'a.@string {{ get; }}"),
            AnonymousTypes(
                $$"""

                {{FeaturesResources.Types_colon}}
                    'a {{FeaturesResources.is_}} new { string @string }
                """)
        ]);

    [Theory, CombinatorialData]
    public async Task UsingStatement_Class(bool simpleUsing, bool implementsIDisposable)
    {
        var usingStatement = simpleUsing
            ? "$$using var c = new C();"
            : "$$using (var c = new C()) { }";

        // When class doesn't implement 'IDisposable' a compiler error is produced.
        // However, we still want to show a specific 'Dispose' method for error recovery
        // since that would be the picked method when user fixes the error
        await TestAsync($$"""
            using System;

            class C {{(implementsIDisposable ? ": IDisposable" : "")}}
            {
                public void Dispose()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void C.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Class_DisposeMethodInBaseType(bool simpleUsing, bool implementsIDisposable)
    {
        var usingStatement = simpleUsing
            ? "$$using var c = new C();"
            : "$$using (var c = new C()) { }";

        // When class doesn't implement 'IDisposable' a compiler error is produced.
        // However, we still want to show a specific 'Dispose' method for error recovery
        // since that would be the picked method when user fixes the error
        await TestAsync($$"""
            using System;

            class CBase {{(implementsIDisposable ? ": IDisposable" : "")}}
            {
                public void Dispose()
                {
                }
            }

            class C : CBase
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void CBase.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Class_ExplicitImplementationAndPattern(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "$$using var c = new C();"
            : "$$using (var c = new C()) { }";

        await TestAsync($$"""
            using System;

            class C : IDisposable
            {
                void IDisposable.Dispose()
                {
                }

                public void Dispose()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void C.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Class_ImplementsIDisposableButDoesNotHaveDisposeMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "$$using var c = new C();"
            : "$$using (var c = new C()) { }";

        await TestAsync($$"""
            using System;

            class C : IDisposable
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void IDisposable.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Class_DoesNotImplementIDisposableAndDoesNotHaveDisposeMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "$$using var c = new C();"
            : "$$using (var c = new C()) { }";

        await TestAsync($$"""
            using System;

            class C
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Struct(bool simpleUsing, bool isRefStruct, bool implementsIDisposable)
    {
        var usingStatement = simpleUsing
            ? "$$using var s = new S();"
            : "$$using (var s = new S()) { }";

        // When non-ref struct doesn't implement 'IDisposable' a compiler error is produced.
        // However, we still want to show a specific 'Dispose' method for error recovery
        // since that would be the picked method when user fixes the error
        await TestAsync($$"""
            using System;

            {{(isRefStruct ? "ref" : "")}} struct S {{(implementsIDisposable ? ": IDisposable" : "")}}
            {
                public void Dispose()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void S.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Struct_ExplicitImplementationAndPattern(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "$$using var s = new S();"
            : "$$using (var s = new S()) { }";

        await TestAsync($$"""
            using System;

            struct S : IDisposable
            {
                void IDisposable.Dispose()
                {
                }

                public void Dispose()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void S.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Struct_ImplementsIDisposableButDoesNotHaveDisposeMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "$$using var s = new S();"
            : "$$using (var s = new S()) { }";

        await TestAsync($$"""
            using System;

            struct S : IDisposable
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription("void IDisposable.Dispose()"));
    }

    [Theory, CombinatorialData]
    public async Task UsingStatement_Struct_DoesNotImplementIDisposableAndDoesNotHaveDisposeMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "$$using var s = new S();"
            : "$$using (var s = new S()) { }";

        await TestAsync($$"""
            using System;

            struct S
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """);
    }

    [Fact]
    public Task UsingStatement_Interface()
        => TestAsync("""
            using System;

            interface IMyInterface : IDisposable
            {
            }

            class C
            {
                void M(IMyInterface i)
                {
                    $$using (i)
                    {
                    }
                }
            }
            """,
            MainDescription("void IDisposable.Dispose()"));

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Class(bool simpleUsing, bool implementsIAsyncDisposable)
    {
        var usingStatement = simpleUsing
            ? "await $$using var c = new C();"
            : "await $$using (var c = new C()) { }";

        // When class doesn't implement 'IAsyncDisposable' a compiler error is produced.
        // However, we still want to show a specific 'DisposeAsync' method for error recovery
        // since that would be the picked method when user fixes the error
        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            class C {{(implementsIAsyncDisposable ? ": IAsyncDisposable" : "")}}
            {
                public ValueTask DisposeAsync()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask C.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Class_DisposeMethodInBaseType(bool simpleUsing, bool implementsIAsyncDisposable)
    {
        var usingStatement = simpleUsing
            ? "await $$using var c = new C();"
            : "await $$using (var c = new C()) { }";

        // When class doesn't implement 'IAsyncDisposable' a compiler error is produced.
        // However, we still want to show a specific 'DisposeAsync' method for error recovery
        // since that would be the picked method when user fixes the error
        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            class CBase {{(implementsIAsyncDisposable ? ": IAsyncDisposable" : "")}}
            {
                public ValueTask DisposeAsync()
                {
                }
            }

            class C : CBase
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask CBase.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Class_ExplicitImplementationAndPattern(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "await $$using var c = new C();"
            : "await $$using (var c = new C()) { }";

        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            class C : IAsyncDisposable
            {
                ValueTask IAsyncDisposable.DisposeAsync()
                {
                }

                public void DisposeAsync()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask C.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Class_ImplementsIAsyncDisposableButDoesNotHaveDisposeAsyncMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "await $$using var c = new C();"
            : "await $$using (var c = new C()) { }";

        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            class C : IAsyncDisposable
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask IAsyncDisposable.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Class_DoesNotImplementIAsyncDisposableAndDoesNotHaveDisposeAsyncMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "await $$using var c = new C();"
            : "await $$using (var c = new C()) { }";

        await VerifyWithNet8Async($$"""
            using System;

            class C
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Struct(bool simpleUsing, bool isRefStruct, bool implementsIAsyncDisposable)
    {
        var usingStatement = simpleUsing
            ? "await $$using var s = new S();"
            : "await $$using (var s = new S()) { }";

        // When non-ref struct doesn't implement 'IAsyncDisposable' a compiler error is produced.
        // However, we still want to show a specific 'DisposeAsync' method for error recovery
        // since that would be the picked method when user fixes the error
        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            {{(isRefStruct ? "ref" : "")}} struct S {{(implementsIAsyncDisposable ? ": IAsyncDisposable" : "")}}
            {
                public ValueTask DisposeAsync()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask S.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Struct_ExplicitImplementationAndPattern(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "await $$using var s = new S();"
            : "await $$using (var s = new S()) { }";

        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            struct S : IAsyncDisposable
            {
                ValueTask IAsyncDisposable.DisposeAsync()
                {
                }

                public ValueTask DisposeAsync()
                {
                }

                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask S.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Struct_ImplementsIAsyncDisposableButDoesNotHaveDisposeAsyncMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "await $$using var s = new S();"
            : "await $$using (var s = new S()) { }";

        await VerifyWithNet8Async($$"""
            using System;
            using System.Threading.Tasks;

            struct S : IAsyncDisposable
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask IAsyncDisposable.DisposeAsync()"));
    }

    [Theory, CombinatorialData]
    public async Task AwaitUsingStatement_Struct_DoesNotImplementIAsyncDisposableAndDoesNotHaveDisposeAsyncMethod(bool simpleUsing)
    {
        var usingStatement = simpleUsing
            ? "await $$using var s = new S();"
            : "await $$using (var s = new S()) { }";

        await VerifyWithNet8Async($$"""
            using System;

            struct S
            {
                void M()
                {
                    {{usingStatement}}
                }
            }
            """);
    }

    [Fact]
    public Task AwaitUsingStatement_Interface()
        => VerifyWithNet8Async("""
            using System;
            using System.Threading.Tasks;

            interface IMyInterface : IAsyncDisposable
            {
            }

            class C
            {
                void M(IMyInterface i)
                {
                    await $$using (i)
                    {
                    }
                }
            }
            """,
            MainDescription($"({CSharpFeaturesResources.awaitable}) ValueTask IAsyncDisposable.DisposeAsync()"));

    [Fact]
    public Task NullConditionalAssignment()
        => VerifyWithNet8Async("""
            class C
            {
                string s;

                void M(C c)
                {
                    c?.$$s = "";
                }
            }
            """,
            MainDescription($"({FeaturesResources.field}) string C.s"));

    [Fact]
    public Task TestModernExtension1()
        => TestWithOptionsAsync(
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            """
            using System;
            using System.Threading.Tasks;

            static class Extensions
            {
                extension(string s)
                {
                    public void Goo() { }
                }
            }

            class C
            {
                void M(string s)
                {
                    s.$$Goo();
                }
            }
            """,
            MainDescription($"void Extensions.extension(string).Goo()"));

    [Fact]
    public Task TestModernExtension2()
        => TestWithOptionsAsync(
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            """
            using System;
            using System.Threading.Tasks;

            static class Extensions
            {
                extension(string s)
                {
                    public void Goo() { }
                    public void Goo(int i) { }
                }
            }

            class C
            {
                void M(string s)
                {
                    s.$$Goo();
                }
            }
            """,
            MainDescription($"void Extensions.extension(string).Goo() (+ 1 {FeaturesResources.overload})"));

    [Fact]
    public Task TestModernExtension3()
        => TestWithOptionsAsync(
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            """
            using System;
            using System.Threading.Tasks;

            static class Extensions
            {
                extension(string s)
                {
                    public void Goo() { }
                    public void Goo(int i) { }
                }
            }

            class C
            {
                void M(string s)
                {
                    s.$$Goo(0);
                }
            }
            """,
            MainDescription($"void Extensions.extension(string).Goo(int i) (+ 1 {FeaturesResources.overload})"));

    [Fact]
    public Task TestModernExtension4()
        => TestWithOptionsAsync(
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            """
            using System;
            using System.Threading.Tasks;

            static class Extensions
            {
                extension(string s)
                {
                    public int Prop => 0;
                }
            }

            class C
            {
                void M(string s)
                {
                    var v = s.$$Prop;
                }
            }
            """,
            MainDescription($$"""int Extensions.extension(string).Prop { get; }"""));

    [Fact]
    public Task TestModernExtension5()
        => TestWithOptionsAsync(
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            """
            using System;
            using System.Threading.Tasks;

            static class Extensions
            {
                extension(string s)
                {
                    public void Goo()
                    {
                        Console.WriteLine($$s);
                    }
                }
            }
            """,
            MainDescription($"({FeaturesResources.parameter}) string s"));

    [Fact]
    public Task TestModernExtension6()
        => TestWithOptionsAsync(
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            """
            using System;
            using System.Threading.Tasks;

            static class Extensions
            {
                $$extension(string s)
                {
                    public void Goo()
                    {
                        Console.WriteLine(s);
                    }
                }
            }
            """,
            MainDescription($"Extensions.extension(System.String)"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72780")]
    public Task TestLocalVariableComment1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    // Comment on i
                    int i;
                    Console.WriteLine($$i);
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int i"),
            Documentation("Comment on i"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72780")]
    public Task TestLocalVariableComment2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    // Comment unrelated to i

                    int i;
                    Console.WriteLine($$i);
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int i"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72780")]
    public Task TestLocalVariableComment3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    // Multi
                    // line
                    // comment for i
                    int i;
                    Console.WriteLine($$i);
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int i"),
            Documentation("Multi line comment for i"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72780")]
    public Task TestLocalVariableComment4()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    // Comment for i.  It is > 0
                    int i;
                    Console.WriteLine($$i);
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) int i"),
            Documentation("Comment for i. It is > 0"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72780")]
    public Task TestLocalVariableComment5()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            // Comment for i.  It is > 0
            int i;
            Console.WriteLine($$i);
            """,
            MainDescription($"({FeaturesResources.local_variable}) int i"),
            Documentation("Comment for i. It is > 0"));

    [Fact]
    public Task TestLocalVariableComment6()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            // <summary>Comment for i. 
            // It is &gt; 0</summary>
            int i;
            Console.WriteLine($$i);
            """,
            MainDescription($"({FeaturesResources.local_variable}) int i"),
            Documentation("Comment for i. It is > 0"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/41245")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/42897")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/63959")]
    public Task TestLocalDeclarationNullable1()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            #nullable enable

            class Program
            {
                static void Main()
                {
                    Program? first = null;
                    var $$second = first;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) Program? second"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "second")));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/41245")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/42897")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/63959")]
    public Task TestLocalDeclarationNullable1_A()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            #nullable enable

            class Program
            {
                static void Main()
                {
                    Program? first = null;
                    $$var second = first;
                }
            }
            """,
            MainDescription($"class Program?"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "second")));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/41245")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/42897")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/63959")]
    public Task TestLocalDeclarationNullable2()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            #nullable enable

            class Program
            {
                static void Main()
                {
                    Program? first = new();
                    var $$second = first;
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) Program? second"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "second")));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/41245")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/42897")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/63959")]
    public Task TestLocalDeclarationNullable2_A()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            #nullable enable

            class Program
            {
                static void Main()
                {
                    Program? first = new();
                    $$var second = first;
                }
            }
            """,
            MainDescription($"class Program?"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_is_not_null_here, "second")));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/41245")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/42897")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/63959")]
    public Task TestLocalDeclarationNullable3()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            #nullable enable

            class Program
            {
                static void Main()
                {
                    Program? first = new();
                    var $$second = first?.ToString();
                }
            }
            """,
            MainDescription($"({FeaturesResources.local_variable}) string? second"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "second")));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/41245")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/42897")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/63959")]
    public Task TestLocalDeclarationNullable3_A()
        => TestWithOptionsAsync(
            Options.Regular,
            """
            #nullable enable

            class Program
            {
                static void Main()
                {
                    Program? first = new();
                    $$var second = first?.ToString();
                }
            }
            """,
            MainDescription($"class System.String?"),
            NullabilityAnalysis(string.Format(FeaturesResources._0_may_be_null_here, "second")));
}
