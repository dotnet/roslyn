// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public class SemanticQuickInfoSourceTests : AbstractSemanticQuickInfoSourceTests
    {
        private async Task TestWithOptionsAsync(CSharpParseOptions options, string markup, params Action<object>[] expectedResults)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(markup, options))
            {
                var position = workspace.Documents.Single().CursorPosition.Value;

                var noListeners = SpecializedCollections.EmptyEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>>();

                var provider = new SemanticQuickInfoProvider(
                    workspace.GetService<ITextBufferFactoryService>(),
                    workspace.GetService<IContentTypeRegistryService>(),
                    workspace.GetService<IProjectionBufferFactoryService>(),
                    workspace.GetService<IEditorOptionsFactoryService>(),
                    workspace.GetService<ITextEditorFactoryService>(),
                    workspace.GetService<IGlyphService>(),
                    workspace.GetService<ClassificationTypeMap>());

                await TestWithOptionsAsync(workspace, provider, position, expectedResults);

                // speculative semantic model
                var document = workspace.CurrentSolution.Projects.First().Documents.First();
                if (CanUseSpeculativeSemanticModel(document, position))
                {
                    var buffer = workspace.Documents.Single().TextBuffer;
                    using (var edit = buffer.CreateEdit())
                    {
                        edit.Replace(0, buffer.CurrentSnapshot.Length, buffer.CurrentSnapshot.GetText());
                        edit.Apply();
                    }

                    await TestWithOptionsAsync(workspace, provider, position, expectedResults);
                }
            }
        }

        private async Task TestWithOptionsAsync(TestWorkspace workspace, SemanticQuickInfoProvider provider, int position, Action<object>[] expectedResults)
        {
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            var state = await provider.GetItemAsync(document, position, cancellationToken: CancellationToken.None);
            if (state != null)
            {
                WaitForDocumentationComment(state.Content);
            }

            if (expectedResults.Length == 0)
            {
                Assert.Null(state);
            }
            else
            {
                Assert.NotNull(state);

                foreach (var expected in expectedResults)
                {
                    expected(state.Content);
                }
            }
        }

        private async Task VerifyWithMscorlib45Async(string markup, Action<object>[] expectedResults)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferencesNet45=""true"">
        <Document FilePath=""SourceDocument"">
{0}
        </Document>
    </Project>
</Workspace>", SecurityElement.Escape(markup));

            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(xmlString))
            {
                var position = workspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var documentId = workspace.Documents.Where(d => d.Name == "SourceDocument").Single().Id;
                var document = workspace.CurrentSolution.GetDocument(documentId);

                var provider = new SemanticQuickInfoProvider(
                        workspace.GetService<ITextBufferFactoryService>(),
                        workspace.GetService<IContentTypeRegistryService>(),
                        workspace.GetService<IProjectionBufferFactoryService>(),
                        workspace.GetService<IEditorOptionsFactoryService>(),
                        workspace.GetService<ITextEditorFactoryService>(),
                        workspace.GetService<IGlyphService>(),
                        workspace.GetService<ClassificationTypeMap>());

                var state = provider.GetItemAsync(document, position, cancellationToken: CancellationToken.None).Result;
                if (state != null)
                {
                    WaitForDocumentationComment(state.Content);
                }

                if (expectedResults.Length == 0)
                {
                    Assert.Null(state);
                }
                else
                {
                    Assert.NotNull(state);

                    foreach (var expected in expectedResults)
                    {
                        expected(state.Content);
                    }
                }
            }
        }

        protected override async Task TestAsync(string markup, params Action<object>[] expectedResults)
        {
            await TestWithOptionsAsync(Options.Regular, markup, expectedResults);
            await TestWithOptionsAsync(Options.Script, markup, expectedResults);
        }

        protected async Task TestWithUsingsAsync(string markup, params Action<object>[] expectedResults)
        {
            var markupWithUsings =
@"using System;
using System.Collections.Generic;
using System.Linq;
" + markup;

            await TestAsync(markupWithUsings, expectedResults);
        }

        protected Task TestInClassAsync(string markup, params Action<object>[] expectedResults)
        {
            var markupInClass = "class C { " + markup + " }";
            return TestWithUsingsAsync(markupInClass, expectedResults);
        }

        protected Task TestInMethodAsync(string markup, params Action<object>[] expectedResults)
        {
            var markupInMethod = "class C { void M() { " + markup + " } }";
            return TestWithUsingsAsync(markupInMethod, expectedResults);
        }

        private async Task TestWithReferenceAsync(string sourceCode,
            string referencedCode,
            string sourceLanguage,
            string referencedLanguage,
            params Action<object>[] expectedResults)
        {
            await TestWithMetadataReferenceHelperAsync(sourceCode, referencedCode, sourceLanguage, referencedLanguage, expectedResults);
            await TestWithProjectReferenceHelperAsync(sourceCode, referencedCode, sourceLanguage, referencedLanguage, expectedResults);

            // Multi-language projects are not supported.
            if (sourceLanguage == referencedLanguage)
            {
                await TestInSameProjectHelperAsync(sourceCode, referencedCode, sourceLanguage, expectedResults);
            }
        }

        private async Task TestWithMetadataReferenceHelperAsync(
            string sourceCode,
            string referencedCode,
            string sourceLanguage,
            string referencedLanguage,
            params Action<object>[] expectedResults)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"" IncludeXmlDocComments=""true"">
            <Document FilePath=""ReferencedDocument"">
{3}
            </Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode),
               referencedLanguage, SecurityElement.Escape(referencedCode));

            await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
        }

        private async Task TestWithProjectReferenceHelperAsync(
            string sourceCode,
            string referencedCode,
            string sourceLanguage,
            string referencedLanguage,
            params Action<object>[] expectedResults)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <ProjectReference>ReferencedProject</ProjectReference>
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""ReferencedProject"">
        <Document FilePath=""ReferencedDocument"">
{3}
        </Document>
    </Project>
    
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode),
               referencedLanguage, SecurityElement.Escape(referencedCode));

            await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
        }

        private async Task TestInSameProjectHelperAsync(
            string sourceCode,
            string referencedCode,
            string sourceLanguage,
            params Action<object>[] expectedResults)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <Document FilePath=""ReferencedDocument"">
{2}
        </Document>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode), SecurityElement.Escape(referencedCode));

            await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
        }

        private async Task VerifyWithReferenceWorkerAsync(string xmlString, params Action<object>[] expectedResults)
        {
            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(xmlString))
            {
                var position = workspace.Documents.First(d => d.Name == "SourceDocument").CursorPosition.Value;
                var documentId = workspace.Documents.First(d => d.Name == "SourceDocument").Id;
                var document = workspace.CurrentSolution.GetDocument(documentId);

                var provider = new SemanticQuickInfoProvider(
                        workspace.GetService<ITextBufferFactoryService>(),
                        workspace.GetService<IContentTypeRegistryService>(),
                        workspace.GetService<IProjectionBufferFactoryService>(),
                        workspace.GetService<IEditorOptionsFactoryService>(),
                        workspace.GetService<ITextEditorFactoryService>(),
                        workspace.GetService<IGlyphService>(),
                        workspace.GetService<ClassificationTypeMap>());

                var state = provider.GetItemAsync(document, position, cancellationToken: CancellationToken.None).Result;
                if (state != null)
                {
                    WaitForDocumentationComment(state.Content);
                }

                if (expectedResults.Length == 0)
                {
                    Assert.Null(state);
                }
                else
                {
                    Assert.NotNull(state);

                    foreach (var expected in expectedResults)
                    {
                        expected(state.Content);
                    }
                }
            }
        }

        protected async Task TestInvalidTypeInClassAsync(string code)
        {
            var codeInClass = "class C { " + code + " }";
            await TestAsync(codeInClass);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNamespaceInUsingDirective()
        {
            await TestAsync("using $$System;",
                MainDescription("namespace System"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNamespaceInUsingDirective2()
        {
            await TestAsync("using System.Coll$$ections.Generic;",
                MainDescription("namespace System.Collections"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNamespaceInUsingDirective3()
        {
            await TestAsync("using System.L$$inq;",
                MainDescription("namespace System.Linq"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNamespaceInUsingDirectiveWithAlias()
        {
            await TestAsync("using Foo = Sys$$tem.Console;",
                MainDescription("namespace System"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeInUsingDirectiveWithAlias()
        {
            await TestAsync("using Foo = System.Con$$sole;",
                MainDescription("class System.Console"));
        }

        [WorkItem(991466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDocumentationInUsingDirectiveWithAlias()
        {
            var markup =
@"using I$$ = IFoo;
///<summary>summary for interface IFoo</summary>
interface IFoo {  }";

            await TestAsync(markup,
                MainDescription("interface IFoo"),
                Documentation("summary for interface IFoo"));
        }

        [WorkItem(991466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDocumentationInUsingDirectiveWithAlias2()
        {
            var markup =
@"using I = IFoo;
///<summary>summary for interface IFoo</summary>
interface IFoo {  }
class C : I$$ { }";

            await TestAsync(markup,
                MainDescription("interface IFoo"),
                Documentation("summary for interface IFoo"));
        }

        [WorkItem(991466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDocumentationInUsingDirectiveWithAlias3()
        {
            var markup =
@"using I = IFoo;
///<summary>summary for interface IFoo</summary>
interface IFoo 
{  
    void Foo();
}
class C : I$$ { }";

            await TestAsync(markup,
                MainDescription("interface IFoo"),
                Documentation("summary for interface IFoo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestThis()
        {
            var markup =
@"
///<summary>summary for Class C</summary>
class C { string M() {  return thi$$s.ToString(); } }";

            await TestInMethodAsync(markup,
                MainDescription("class C"),
                Documentation("summary for Class C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestClassWithDocComment()
        {
            var markup =
@"
///<summary>Hello!</summary>
class C { void M() { $$C obj; } }";

            await TestAsync(markup,
                MainDescription("class C"),
                Documentation("Hello!"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSingleLineDocComments()
        {
            // Tests chosen to maximize code coverage in DocumentationCommentCompiler.WriteFormattedSingleLineComment

            // SingleLine doc comment with leading whitespace
            await TestAsync(@"
    ///<summary>Hello!</summary>
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // SingleLine doc comment with space before opening tag
            await TestAsync(@"
/// <summary>Hello!</summary>
class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // SingleLine doc comment with space before opening tag and leading whitespace
            await TestAsync(@"
    /// <summary>Hello!</summary>
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // SingleLine doc comment with leading whitespace and blank line
            await TestAsync(@"
    ///<summary>Hello!
    ///</summary>

    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // SingleLine doc comment with '\r' line separators
            await TestAsync("///<summary>Hello!\r///</summary>\rclass C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMultiLineDocComments()
        {
            // Tests chosen to maximize code coverage in DocumentationCommentCompiler.WriteFormattedMultiLineComment

            // Multiline doc comment with leading whitespace
            await TestAsync(@"
    /**<summary>Hello!</summary>*/
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // Multiline doc comment with space before opening tag
            await TestAsync(@"
/** <summary>Hello!</summary>
 **/
class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // Multiline doc comment with space before opening tag and leading whitespace
            await TestAsync(@"
    /**
     ** <summary>Hello!</summary>
     **/
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // Multiline doc comment with no per-line prefix
            await TestAsync(@"
/**
  <summary>
  Hello!
  </summary>
*/
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // Multiline doc comment with inconsistent per-line prefix
            await TestAsync(@"
/**
 ** <summary>
    Hello!</summary>
 **
 **/
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // Multiline doc comment with closing comment on final line
            await TestAsync(@"
/**
<summary>Hello!
</summary>*/
    class C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));

            // Multiline doc comment with '\r' line separators
            await TestAsync("/**\r* <summary>\r* Hello!\r* </summary>\r*/\rclass C { void M() { $$C obj; } }",
                MainDescription("class C"),
                Documentation("Hello!"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMethodWithDocComment()
        {
            var markup =
@"
///<summary>Hello!</summary>
void M() { M$$() }";

            await TestInClassAsync(markup,
                MainDescription("void C.M()"),
                Documentation("Hello!"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInt32()
        {
            await TestInClassAsync(@"$$Int32 i;",
                MainDescription("struct System.Int32"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBuiltInInt()
        {
            await TestInClassAsync(@"$$int i;",
                MainDescription("struct System.Int32"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestString()
        {
            await TestInClassAsync(@"$$String s;",
                MainDescription("class System.String"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBuiltInString()
        {
            await TestInClassAsync(@"$$string s;",
                MainDescription("class System.String"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBuiltInStringAtEndOfToken()
        {
            await TestInClassAsync(@"string$$ s;",
                MainDescription("class System.String"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBoolean()
        {
            await TestInClassAsync(@"$$Boolean b;",
                MainDescription("struct System.Boolean"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBuiltInBool()
        {
            await TestInClassAsync(@"$$bool b;",
                MainDescription("struct System.Boolean"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSingle()
        {
            await TestInClassAsync(@"$$Single s;",
                MainDescription("struct System.Single"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBuiltInFloat()
        {
            await TestInClassAsync(@"$$float f;",
                MainDescription("struct System.Single"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestVoidIsInvalid()
        {
            await TestInvalidTypeInClassAsync(@"$$void M() { }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInvalidPointer1_931958()
        {
            await TestInvalidTypeInClassAsync(@"$$T* i;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInvalidPointer2_931958()
        {
            await TestInvalidTypeInClassAsync(@"T$$* i;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInvalidPointer3_931958()
        {
            await TestInvalidTypeInClassAsync(@"T*$$ i;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestListOfString()
        {
            await TestInClassAsync(@"$$List<string> l;",
                MainDescription("class System.Collections.Generic.List<T>"),
                TypeParameterMap($"\r\nT {FeaturesResources.Is} string"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestListOfSomethingFromSource()
        {
            var markup =
@"
///<summary>Generic List</summary>
public class GenericList<T> { Generic$$List<int> t; }";

            await TestAsync(markup,
                MainDescription("class GenericList<T>"),
                Documentation("Generic List"),
                TypeParameterMap($"\r\nT {FeaturesResources.Is} int"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestListOfT()
        {
            await TestInMethodAsync(@"class C<T> { $$List<T> l; }",
                MainDescription("class System.Collections.Generic.List<T>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDictionaryOfIntAndString()
        {
            await TestInClassAsync(@"$$Dictionary<int, string> d;",
                MainDescription("class System.Collections.Generic.Dictionary<TKey, TValue>"),
                TypeParameterMap(
                    Lines($"\r\nTKey {FeaturesResources.Is} int",
                          $"TValue {FeaturesResources.Is} string")));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDictionaryOfTAndU()
        {
            await TestInMethodAsync(@"class C<T, U> { $$Dictionary<T, U> d; }",
                MainDescription("class System.Collections.Generic.Dictionary<TKey, TValue>"),
                TypeParameterMap(
                    Lines($"\r\nTKey {FeaturesResources.Is} T",
                          $"TValue {FeaturesResources.Is} U")));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestIEnumerableOfInt()
        {
            await TestInClassAsync(@"$$IEnumerable<int> M() { yield break; }",
                MainDescription("interface System.Collections.Generic.IEnumerable<out T>"),
                TypeParameterMap($"\r\nT {FeaturesResources.Is} int"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEventHandler()
        {
            await TestInClassAsync(@"event $$EventHandler e;",
                MainDescription("delegate void System.EventHandler(object sender, System.EventArgs e)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameter()
        {
            await TestAsync(@"class C<T> { $$T t; }",
                MainDescription($"T {FeaturesResources.In} C<T>"));
        }

        [WorkItem(538636)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameterWithDocComment()
        {
            var markup =
@"
///<summary>Hello!</summary>
///<typeparam name=""T"">T is Type Parameter</typeparam>
class C<T> { $$T t; }";

            await TestAsync(markup,
                MainDescription($"T {FeaturesResources.In} C<T>"),
                Documentation("T is Type Parameter"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameter1_Bug931949()
        {
            await TestAsync(@"class T1<T11> { $$T11 t; }",
                MainDescription($"T11 {FeaturesResources.In} T1<T11>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameter2_Bug931949()
        {
            await TestAsync(@"class T1<T11> { T$$11 t; }",
                MainDescription($"T11 {FeaturesResources.In} T1<T11>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameter3_Bug931949()
        {
            await TestAsync(@"class T1<T11> { T1$$1 t; }",
                MainDescription($"T11 {FeaturesResources.In} T1<T11>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameter4_Bug931949()
        {
            await TestAsync(@"class T1<T11> { T11$$ t; }",
                MainDescription($"T11 {FeaturesResources.In} T1<T11>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNullableOfInt()
        {
            await TestInClassAsync(@"$$Nullable<int> i; }",
                MainDescription("struct System.Nullable<T> where T : struct"),
                TypeParameterMap($"\r\nT {FeaturesResources.Is} int"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeDeclaredOnMethod1_Bug1946()
        {
            await TestAsync(@"class C { static void Meth1<T1>($$T1 i) where T1 : struct { T1 i; } }",
                MainDescription($"T1 {FeaturesResources.In} C.Meth1<T1> where T1 : struct"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeDeclaredOnMethod2_Bug1946()
        {
            await TestAsync(@"class C { static void Meth1<T1>(T1 i) where $$T1 : struct { T1 i; } }",
                MainDescription($"T1 {FeaturesResources.In} C.Meth1<T1> where T1 : struct"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeDeclaredOnMethod3_Bug1946()
        {
            await TestAsync(@"class C { static void Meth1<T1>(T1 i) where T1 : struct { $$T1 i; } }",
                MainDescription($"T1 {FeaturesResources.In} C.Meth1<T1> where T1 : struct"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeParameterConstraint_Class()
        {
            await TestAsync(@"class C<T> where $$T : class { }",
                MainDescription($"T {FeaturesResources.In} C<T> where T : class"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeParameterConstraint_Struct()
        {
            await TestAsync(@"struct S<T> where $$T : class { }",
                MainDescription($"T {FeaturesResources.In} S<T> where T : class"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeParameterConstraint_Interface()
        {
            await TestAsync(@"interface I<T> where $$T : class { }",
                MainDescription($"T {FeaturesResources.In} I<T> where T : class"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericTypeParameterConstraint_Delegate()
        {
            await TestAsync(@"delegate void D<T>() where $$T : class;",
                MainDescription($"T {FeaturesResources.In} D<T> where T : class"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMinimallyQualifiedConstraint()
        {
            await TestAsync(@"class C<T> where $$T : IEnumerable<int>",
                MainDescription($"T {FeaturesResources.In} C<T> where T : IEnumerable<int>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task FullyQualifiedConstraint()
        {
            await TestAsync(@"class C<T> where $$T : System.Collections.Generic.IEnumerable<int>",
                MainDescription($"T {FeaturesResources.In} C<T> where T : System.Collections.Generic.IEnumerable<int>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMethodReferenceInSameMethod()
        {
            await TestAsync("class C { void M() { M$$(); } }",
                MainDescription("void C.M()"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMethodReferenceInSameMethodWithDocComment()
        {
            var markup =
@"
///<summary>Hello World</summary>
void M() { M$$(); }";

            await TestInClassAsync(markup,
                MainDescription("void C.M()"),
                Documentation("Hello World"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFieldInMethodBuiltIn()
        {
            var markup =
@"int field;

void M()
{
    field$$
}";

            await TestInClassAsync(markup,
                MainDescription($"({FeaturesResources.Field}) int C.field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFieldInMethodBuiltIn2()
        {
            await TestInClassAsync("int field; void M() { int f = field$$; }",
                MainDescription($"({FeaturesResources.Field}) int C.field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFieldInMethodBuiltInWithFieldInitializer()
        {
            await TestInClassAsync("int field = 1; void M() { int f = field $$; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorBuiltIn()
        {
            await TestInMethodAsync("int x; x = x$$+1;",
                MainDescription("int int.operator +(int left, int right)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorBuiltIn1()
        {
            await TestInMethodAsync("int x; x = x$$ + 1;",
                MainDescription($"({FeaturesResources.LocalVariable}) int x"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorBuiltIn2()
        {
            await TestInMethodAsync("int x; x = x+$$x;",
                MainDescription($"({FeaturesResources.LocalVariable}) int x"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorBuiltIn3()
        {
            await TestInMethodAsync("int x; x = x +$$ x;",
                MainDescription("int int.operator +(int left, int right)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorBuiltIn4()
        {
            await TestInMethodAsync("int x; x = x + $$x;",
                MainDescription($"({FeaturesResources.LocalVariable}) int x"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorCustomTypeBuiltIn()
        {
            var markup =
@"class C
{
    static void M() { C c; c = c +$$ c; }
}";

            await TestAsync(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOperatorCustomTypeOverload()
        {
            var markup =
@"class C
{
    static void M() { C c; c = c +$$ c; }
    static C operator+(C a, C b) { return a; }
}";

            await TestAsync(markup,
                MainDescription("C C.operator +(C a, C b)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFieldInMethodMinimal()
        {
            var markup =
@"DateTime field;

void M()
{
    field$$
}";

            await TestInClassAsync(markup,
                MainDescription($"({FeaturesResources.Field}) DateTime C.field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFieldInMethodQualified()
        {
            var markup =
@"System.IO.FileInfo file;

void M()
{
    file$$
}";

            await TestInClassAsync(markup,
                MainDescription($"({FeaturesResources.Field}) System.IO.FileInfo C.file"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMemberOfStructFromSource()
        {
            var markup =
@"struct MyStruct {
public static int SomeField; }
static class Test { int a = MyStruct.Some$$Field; }";

            await TestAsync(markup,
                MainDescription($"({FeaturesResources.Field}) int MyStruct.SomeField"));
        }

        [WorkItem(538638)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMemberOfStructFromSourceWithDocComment()
        {
            var markup =
@"struct MyStruct {
///<summary>My Field</summary>
public static int SomeField; }
static class Test { int a = MyStruct.Some$$Field; }";

            await TestAsync(markup,
                MainDescription($"({FeaturesResources.Field}) int MyStruct.SomeField"),
                Documentation("My Field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMemberOfStructInsideMethodFromSource()
        {
            var markup =
@"struct MyStruct {
public static int SomeField; }
static class Test { static void Method() { int a = MyStruct.Some$$Field; } }";

            await TestAsync(markup,
                MainDescription($"({FeaturesResources.Field}) int MyStruct.SomeField"));
        }

        [WorkItem(538638)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMemberOfStructInsideMethodFromSourceWithDocComment()
        {
            var markup =
@"struct MyStruct {
///<summary>My Field</summary>
public static int SomeField; }
static class Test { static void Method() { int a = MyStruct.Some$$Field; } }";

            await TestAsync(markup,
                MainDescription($"({FeaturesResources.Field}) int MyStruct.SomeField"),
                Documentation("My Field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMetadataFieldMinimal()
        {
            await TestInMethodAsync(@"DateTime dt = DateTime.MaxValue$$",
                MainDescription($"({FeaturesResources.Field}) DateTime DateTime.MaxValue"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMetadataFieldQualified1()
        {
            // NOTE: we qualify the field type, but not the type that contains the field in Dev10
            var markup =
@"class C {
    void M()
    {
        DateTime dt = System.DateTime.MaxValue$$
    }
}";
            await TestAsync(markup,
                MainDescription($"({FeaturesResources.Field}) System.DateTime System.DateTime.MaxValue"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMetadataFieldQualified2()
        {
            await TestAsync(@"
class C {
    void M()
    {
        DateTime dt = System.DateTime.MaxValue$$
    }
}",
                MainDescription($"({FeaturesResources.Field}) System.DateTime System.DateTime.MaxValue"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMetadataFieldQualified3()
        {
            await TestAsync(@"
using System;
class C {
    void M()
    {
        DateTime dt = System.DateTime.MaxValue$$
    }
}",
                MainDescription($"({FeaturesResources.Field}) DateTime DateTime.MaxValue"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ConstructedGenericField()
        {
            await TestAsync(@"class C<T> { public T Field; }

class D {
    void M() {
        new C<int>().Fi$$eld.ToString();
    }
}",
                MainDescription($"({FeaturesResources.Field}) int C<int>.Field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task UnconstructedGenericField()
        {
            await TestAsync(@"
class C<T> {
    public T Field;

    void M() {
        Fi$$eld.ToString();
    }
}",
                MainDescription($"({FeaturesResources.Field}) T C<T>.Field"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestIntegerLiteral()
        {
            await TestInMethodAsync(@"int f = 37$$",
                MainDescription("struct System.Int32"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTrueKeyword()
        {
            await TestInMethodAsync(@"bool f = true$$",
                MainDescription("struct System.Boolean"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFalseKeyword()
        {
            await TestInMethodAsync(@"bool f = false$$",
                MainDescription("struct System.Boolean"));
        }

        [WorkItem(756226)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAwaitKeywordOnGenericTaskReturningAsync()
        {
            var markup = @"using System.Threading.Tasks;
class C
{
    public async Task<int> Calc()
    {
        aw$$ait Calc();
        return 5;
    }
}";
            await TestAsync(markup, MainDescription($"{FeaturesResources.PrefixTextForAwaitKeyword} struct System.Int32"));
        }

        [WorkItem(756226)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAwaitKeywordInDeclarationStatement()
        {
            var markup = @"using System.Threading.Tasks;
class C
{
    public async Task<int> Calc()
    {
        var x = $$await Calc();
        return 5;
    }
}";
            await TestAsync(markup, MainDescription($"{FeaturesResources.PrefixTextForAwaitKeyword} struct System.Int32"));
        }

        [WorkItem(756226)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAwaitKeywordOnTaskReturningAsync()
        {
            var markup = @"using System.Threading.Tasks;
class C
{
    public async void Calc()
    {
        aw$$ait Task.Delay(100);
    }
}";
            await TestAsync(markup, MainDescription($"{FeaturesResources.PrefixTextForAwaitKeyword} {FeaturesResources.TextForSystemVoid}"));
        }

        [WorkItem(756226), WorkItem(756337)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNestedAwaitKeywords1()
        {
            var markup = @"using System;
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
}";
            await TestAsync(markup, MainDescription($"({CSharpFeaturesResources.Awaitable}) {FeaturesResources.PrefixTextForAwaitKeyword} class System.Threading.Tasks.Task<TResult>"),
                         TypeParameterMap($"\r\nTResult {FeaturesResources.Is} int"));
        }

        [WorkItem(756226)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNestedAwaitKeywords2()
        {
            var markup = @"using System;
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
}";
            await TestAsync(markup, MainDescription($"{FeaturesResources.PrefixTextForAwaitKeyword} struct System.Int32"));
        }

        [WorkItem(756226), WorkItem(756337)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAwaitablePrefixOnCustomAwaiter()
        {
            var markup = @"using System;
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
}";
            await TestAsync(markup, MainDescription($"({CSharpFeaturesResources.Awaitable}) class C"));
        }

        [WorkItem(756226), WorkItem(756337)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTaskType()
        {
            var markup = @"using System.Threading.Tasks;
class C
{
    public void Calc()
    {
        Task$$ v1;
    }
}";
            await TestAsync(markup, MainDescription($"({CSharpFeaturesResources.Awaitable}) class System.Threading.Tasks.Task"));
        }

        [WorkItem(756226), WorkItem(756337)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTaskOfTType()
        {
            var markup = @"using System;
using System.Threading.Tasks;
class C
{
    public void Calc()
    {
        Task$$<int> v1;
    }
}";
            await TestAsync(markup, MainDescription($"({CSharpFeaturesResources.Awaitable}) class System.Threading.Tasks.Task<TResult>"),
                         TypeParameterMap($"\r\nTResult {FeaturesResources.Is} int"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestStringLiteral()
        {
            await TestInMethodAsync(@"string f = ""Foo""$$",
                MainDescription("class System.String"));
        }

        [WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestVerbatimStringLiteral()
        {
            await TestInMethodAsync(@"string f = @""cat""$$",
                MainDescription("class System.String"));
        }

        [WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInterpolatedStringLiteral()
        {
            await TestInMethodAsync(@"string f = $""cat""$$", MainDescription("class System.String"));
            await TestInMethodAsync(@"string f = $""c$$at""", MainDescription("class System.String"));
            await TestInMethodAsync(@"string f = $""$$cat""", MainDescription("class System.String"));
            await TestInMethodAsync(@"string f = $""cat {1$$ + 2} dog""", MainDescription("struct System.Int32"));
        }

        [WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestVerbatimInterpolatedStringLiteral()
        {
            await TestInMethodAsync(@"string f = $@""cat""$$", MainDescription("class System.String"));
            await TestInMethodAsync(@"string f = $@""c$$at""", MainDescription("class System.String"));
            await TestInMethodAsync(@"string f = $@""$$cat""", MainDescription("class System.String"));
            await TestInMethodAsync(@"string f = $@""cat {1$$ + 2} dog""", MainDescription("struct System.Int32"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCharLiteral()
        {
            await TestInMethodAsync(@"string f = 'x'$$",
                MainDescription("struct System.Char"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DynamicKeyword()
        {
            await TestInMethodAsync(@"dyn$$amic dyn;",
                MainDescription("dynamic"),
                Documentation(FeaturesResources.RepresentsAnObjectWhoseOperations));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DynamicField()
        {
            await TestInClassAsync(@"dynamic dyn;
void M()
{
    d$$yn.Foo();
}",
                MainDescription($"({FeaturesResources.Field}) dynamic C.dyn"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LocalProperty_Minimal()
        {
            await TestInClassAsync(@"DateTime Prop { get; set; }
void M()
{
    P$$rop.ToString();
}",
                MainDescription("DateTime C.Prop { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LocalProperty_Minimal_PrivateSet()
        {
            await TestInClassAsync(@"public DateTime Prop { get; private set; }
void M()
{
    P$$rop.ToString();
}",
                MainDescription("DateTime C.Prop { get; private set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LocalProperty_Minimal_PrivateSet1()
        {
            await TestInClassAsync(@"protected internal int Prop { get; private set; }
void M()
{
    P$$rop.ToString();
}",
                MainDescription("int C.Prop { get; private set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LocalProperty_Qualified()
        {
            await TestInClassAsync(@"System.IO.FileInfo Prop { get; set; }
void M()
{
    P$$rop.ToString();
}",
                MainDescription("System.IO.FileInfo C.Prop { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task NonLocalProperty_Minimal()
        {
            await TestInMethodAsync(@"DateTime.No$$w.ToString();",
                MainDescription("DateTime DateTime.Now { get; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task NonLocalProperty_Qualified()
        {
            await TestInMethodAsync(@"System.IO.FileInfo f; f.Att$$ributes.ToString();",
                MainDescription("System.IO.FileAttributes System.IO.FileSystemInfo.Attributes { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ConstructedGenericProperty()
        {
            await TestAsync(@"
class C<T> {
    public T Property{ get; set }
}

class D {
    void M() {
        new C<int>().Pro$$perty.ToString();
    }
}",
                MainDescription("int C<int>.Property { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task UnconstructedGenericProperty()
        {
            await TestAsync(@"
class C<T> {
    public T Property { get; set}

    void M() {
        Pro$$perty.ToString();
    }
}",
                MainDescription("T C<T>.Property { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ValueInProperty()
        {
            await TestInClassAsync(@"public DateTime Property {set { foo = val$$ue; } }",
                MainDescription($"({FeaturesResources.Parameter}) DateTime value"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task EnumTypeName()
        {
            await TestInMethodAsync(@"Consol$$eColor c",
                MainDescription("enum System.ConsoleColor"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task EnumMemberNameFromMetadata()
        {
            await TestInMethodAsync(@"ConsoleColor c = ConsoleColor.Bla$$ck",
                MainDescription("ConsoleColor.Black = 0"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task FlagsEnumMemberNameFromMetadata1()
        {
            await TestInMethodAsync(@"AttributeTargets a = AttributeTargets.Cl$$ass",
                MainDescription("AttributeTargets.Class = 4"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task FlagsEnumMemberNameFromMetadata2()
        {
            await TestInMethodAsync(@"AttributeTargets a = AttributeTargets.A$$ll",
                MainDescription("AttributeTargets.All = AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task EnumMemberNameFromSource1()
        {
            await TestAsync(@"
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
}",
    MainDescription("E.B = 1 << 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task EnumMemberNameFromSource2()
        {
            await TestAsync(@"
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
}",
    MainDescription("E.B = 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_InMethod_Minimal()
        {
            await TestInClassAsync(@"void M(DateTime dt) { d$$t.ToString();",
                MainDescription($"({FeaturesResources.Parameter}) DateTime dt"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_InMethod_Qualified()
        {
            await TestInClassAsync(@"void M(System.IO.FileInfo fileInfo) { file$$Info.ToString();",
                MainDescription($"({FeaturesResources.Parameter}) System.IO.FileInfo fileInfo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_FromReferenceToNamedParameter()
        {
            await TestInMethodAsync(@"Console.WriteLine(va$$lue: ""Hi"");",
                MainDescription($"({FeaturesResources.Parameter}) string value"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_DefaultValue()
        {
            // NOTE: Dev10 doesn't show the default value, but it would be nice if we did.
            // NOTE: The "DefaultValue" property isn't implemented yet.
            await TestInClassAsync(@"void M(int param = 42) { para$$m.ToString(); }",
                MainDescription($"({FeaturesResources.Parameter}) int param = 42"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_Params()
        {
            await TestInClassAsync(@"void M(params DateTime[] arg) { ar$$g.ToString(); }",
                MainDescription($"({FeaturesResources.Parameter}) params DateTime[] arg"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_Ref()
        {
            await TestInClassAsync(@"void M(ref DateTime arg) { ar$$g.ToString(); }",
                MainDescription($"({FeaturesResources.Parameter}) ref DateTime arg"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Parameter_Out()
        {
            await TestInClassAsync(@"void M(out DateTime arg) { ar$$g.ToString(); }",
                MainDescription($"({FeaturesResources.Parameter}) out DateTime arg"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Local_Minimal()
        {
            await TestInMethodAsync(@"DateTime dt; d$$t.ToString();",
                MainDescription($"({FeaturesResources.LocalVariable}) DateTime dt"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Local_Qualified()
        {
            await TestInMethodAsync(@"System.IO.FileInfo fileInfo; file$$Info.ToString();",
                MainDescription($"({FeaturesResources.LocalVariable}) System.IO.FileInfo fileInfo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_MetadataOverload()
        {
            await TestInMethodAsync("Console.Write$$Line();",
                MainDescription($"void Console.WriteLine() (+ 18 {FeaturesResources.Overloads})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_SimpleWithOverload()
        {
            await TestInClassAsync(@"
void Method() { Met$$hod(); }
void Method(int i) { }",
                MainDescription($"void C.Method() (+ 1 {FeaturesResources.Overload})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_MoreOverloads()
        {
            await TestInClassAsync(@"
void Method() { Met$$hod(null); }
void Method(int i) { }
void Method(DateTime dt) { }
void Method(System.IO.FileInfo fileInfo) { }",
                MainDescription($"void C.Method(System.IO.FileInfo fileInfo) (+ 3 {FeaturesResources.Overloads})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_SimpleInSameClass()
        {
            await TestInClassAsync(@"DateTime GetDate(System.IO.FileInfo ft) { Get$$Date(null); }",
                MainDescription("DateTime C.GetDate(System.IO.FileInfo ft)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_OptionalParameter()
        {
            await TestInClassAsync(@"
void M() { Met$$hod(); }
void Method(int i = 0) { }",
                MainDescription("void C.Method([int i = 0])"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_OptionalDecimalParameter()
        {
            await TestInClassAsync(@"
void Foo(decimal x$$yz = 10) { }",
                MainDescription($"({FeaturesResources.Parameter}) decimal xyz = 10"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_Generic()
        {
            // Generic method don't get the instantiation info yet.  NOTE: We don't display
            // constraint info in Dev10. Should we?
            await TestInClassAsync(@"TOut Foo<TIn, TOut>(TIn arg) where TIn : IEquatable<TIn> {
    Fo$$o<int, DateTime>(37);
}",

            MainDescription("DateTime C.Foo<int, DateTime>(int arg)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_UnconstructedGeneric()
        {
            await TestInClassAsync(@"TOut Foo<TIn, TOut>(TIn arg) {
    Fo$$o<TIn, TOut>(default(TIn);
}",

                MainDescription("TOut C.Foo<TIn, TOut>(TIn arg)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_Inferred()
        {
            await TestInClassAsync(@"void Foo<TIn>(TIn arg) {
    Fo$$o(42);
}",
                MainDescription("void C.Foo<int>(int arg)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_MultipleParams()
        {
            await TestInClassAsync(@"void Foo(DateTime dt, System.IO.FileInfo fi, int number) {
    Fo$$o(DateTime.Now, null, 32);
}",
                MainDescription("void C.Foo(DateTime dt, System.IO.FileInfo fi, int number)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_OptionalParam()
        {
            // NOTE - Default values aren't actually returned by symbols yet.
            await TestInClassAsync(@"void Foo(int num = 42) {
    Fo$$o();
}",
                MainDescription("void C.Foo([int num = 42])"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Method_ParameterModifiers()
        {
            // NOTE - Default values aren't actually returned by symbols yet.
            await TestInClassAsync(@"void Foo(ref DateTime dt, out System.IO.FileInfo fi, params int[] numbers) {
    Fo$$o(DateTime.Now, null, 32);
}",
                MainDescription("void C.Foo(ref DateTime dt, out System.IO.FileInfo fi, params int[] numbers)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor()
        {
            await TestInClassAsync(@"public C() {} void M() { new C$$ ().ToString(); }",
                MainDescription("C.C()"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor_Overloads()
        {
            await TestInClassAsync(@"
public C() {}
public C(DateTime dt) {}
public C(int i) {}

void M()
{
    new C$$ (DateTime.MaxValue).ToString();
}",
                MainDescription($"C.C(DateTime dt) (+ 2 {FeaturesResources.Overloads})"));
        }

        /// <summary>
        /// Regression for 3923
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor_OverloadFromStringLiteral()
        {
            await TestInMethodAsync(@"new InvalidOperatio$$nException("""");",
                MainDescription($"InvalidOperationException.InvalidOperationException(string message) (+ 2 {FeaturesResources.Overloads})"));
        }

        /// <summary>
        /// Regression for 3923
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor_UnknownType()
        {
            await TestInvalidTypeInClassAsync(@"void M() { new F$$oo(); }");
        }

        /// <summary>
        /// Regression for 3923
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor_OverloadFromProperty()
        {
            await TestInMethodAsync(@"new InvalidOperatio$$nException(this.GetType().Name);",
                MainDescription($"InvalidOperationException.InvalidOperationException(string message) (+ 2 {FeaturesResources.Overloads})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor_Metadata()
        {
            await TestInMethodAsync(@"new Argument$$NullException();",
                MainDescription($"ArgumentNullException.ArgumentNullException() (+ 3 {FeaturesResources.Overloads})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Constructor_MetadataQualified()
        {
            await TestInMethodAsync(@"new System.IO.File$$Info(null);",
                MainDescription("System.IO.FileInfo.FileInfo(string fileName)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task InterfaceProperty()
        {
            await TestInMethodAsync(@"
interface I
{
    string Name$$ { get; set; }
}",
                MainDescription("string I.Name { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ExplicitInterfacePropertyImplementation()
        {
            await TestInMethodAsync(@"
interface I
{
    string Name { get; set; }
}

class C : I
{
    string IEmployee.Name$$
    {
       get { return """"; }
       set { }
    }
}",
                MainDescription("string C.Name { get; set; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Operator()
        {
            await TestInClassAsync(@"
public static C operator +(C left, C right) { return null; }
void M(C left, C right) { return left +$$ right; }
",
                MainDescription("C C.operator +(C left, C right)"));
        }

        [WorkItem(792629, "generic type parameter constraints for methods in quick info")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task GenericMethodWithConstraintsAtDeclaration()
        {
            await TestInClassAsync(@"TOut F$$oo<TIn, TOut>(TIn arg) where TIn : IEquatable<TIn> {
}",

            MainDescription("TOut C.Foo<TIn, TOut>(TIn arg) where TIn : IEquatable<TIn>"));
        }

        [WorkItem(792629, "generic type parameter constraints for methods in quick info")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task GenericMethodWithMultipleConstraintsAtDeclaration()
        {
            await TestInClassAsync(@"TOut Foo<TIn, TOut>(TIn arg) where TIn : Employee, new()
{
    Fo$$o<TIn, TOut>(default(TIn);
}
",

            MainDescription("TOut C.Foo<TIn, TOut>(TIn arg) where TIn : Employee, new()"));
        }

        [WorkItem(792629, "generic type parameter constraints for methods in quick info")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task UnConstructedGenericMethodWithConstraintsAtInvocation()
        {
            await TestInClassAsync(@"TOut Foo<TIn, TOut>(TIn arg) where TIn : Employee
{
    Fo$$o<TIn, TOut>(default(TIn);
}
",

            MainDescription("TOut C.Foo<TIn, TOut>(TIn arg) where TIn : Employee"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task GenericTypeWithConstraintsAtDeclaration()
        {
            await TestAsync(@"public class Employee : IComparable<Employee>
{
    public int CompareTo(Employee other)
    {
        throw new NotImplementedException();
    }
}
class Emplo$$yeeList<T> : IEnumerable<T> where T : Employee, System.IComparable<T>, new()
{
}",

            MainDescription("class EmployeeList<T> where T : Employee, System.IComparable<T>, new()"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task GenericType()
        {
            await TestAsync(@"
class T1<T11>
{
    $$T11 i;
}
",
                MainDescription($"T11 {FeaturesResources.In} T1<T11>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task GenericMethod()
        {
            await TestInClassAsync(@"
    static void Meth1<T1>(T1 i) where T1 : struct
    {
        $$T1 i;
    }
",
                MainDescription($"T1 {FeaturesResources.In} C.Meth1<T1> where T1 : struct"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Var()
        {
            await TestInMethodAsync(@"
var x = new Exception();
var y = $$x;
",
                MainDescription($"({FeaturesResources.LocalVariable}) Exception x"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task NestedInGeneric()
        {
            await TestInMethodAsync(@"
            List<int>.Enu$$merator e;
",
                MainDescription("struct System.Collections.Generic.List<T>.Enumerator"),
                TypeParameterMap($"\r\nT {FeaturesResources.Is} int"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task NestedGenericInGeneric()
        {
            await TestAsync(@"
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
",
                MainDescription("class Outer<T>.Inner<U>"),
                TypeParameterMap(
                    Lines($"\r\nT {FeaturesResources.Is} int",
                          $"U {FeaturesResources.Is} string")));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ObjectInitializer1()
        {
            await TestInClassAsync(@"
    void M()
    {
        var x = new test() { $$z = 5 };
    }

    class test
    {
        public int z;
    }
",
                MainDescription($"({FeaturesResources.Field}) int test.z"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ObjectInitializer2()
        {
            await TestInMethodAsync(@"
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
",
                MainDescription("struct System.Int32"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [WorkItem(537880)]
        public async Task TypeArgument()
        {
            await TestAsync(@"
class C<T, Y>
{
    void M()
    {
        C<int, DateTime> variable;
        $$variable = new C<int, DateTime>();
    }
}",
                MainDescription($"({FeaturesResources.LocalVariable}) C<int, DateTime> variable"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ForEachLoop_1()
        {
            await TestInMethodAsync(@"
int bb = 555;
bb = bb + 1;
foreach (int cc in new int[]{ 1,2,3}){
c$$c = 1;
bb = bb + 21;
}
",
                MainDescription($"({FeaturesResources.LocalVariable}) int cc"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TryCatchFinally_1()
        {
            await TestInMethodAsync(@"
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
            }",
                MainDescription($"({FeaturesResources.LocalVariable}) int aa"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TryCatchFinally_2()
        {
            await TestInMethodAsync(@"
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
",
                MainDescription($"({FeaturesResources.LocalVariable}) Exception ex"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TryCatchFinally_3()
        {
            await TestInMethodAsync(@"
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
",
                MainDescription($"({FeaturesResources.LocalVariable}) int aa"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TryCatchFinally_4()
        {
            await TestInMethodAsync(@"
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
",
                MainDescription($"({FeaturesResources.LocalVariable}) int aa"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task GenericVariable()
        {
            await TestAsync(@"
            class C<T, Y>
            {
                void M()
                {
                    C<int, DateTime> variable;
                    var$$iable = new C<int, DateTime>();
                }
            }
",
                MainDescription($"({FeaturesResources.LocalVariable}) C<int, DateTime> variable"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInstantiation()
        {
            await TestAsync(@"
using System.Collections.Generic;
class Program<T>
{
    static void Main(string[] args)
    {
        var p = new Dictio$$nary<int, string>();
    }
}",
                MainDescription($"Dictionary<int, string>.Dictionary() (+ 5 {FeaturesResources.Overloads})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUsingAlias_Bug4141()
        {
            await TestAsync(@"using X = A.C;
class A {
public class C { }
}
class D : X$$ { }
",
                MainDescription(@"class A.C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestFieldOnDeclaration()
        {
            await TestInClassAsync(@"
DateTime fie$$ld;",
                MainDescription($"({FeaturesResources.Field}) DateTime C.field"));
        }

        [WorkItem(538767)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGenericErrorFieldOnDeclaration()
        {
            await TestInClassAsync(@"
NonExistentType<int> fi$$eld;",
                MainDescription($"({FeaturesResources.Field}) NonExistentType<int> C.field"));
        }

        [WorkItem(538822)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDelegateType()
        {
            await TestInClassAsync(@"
Fun$$c<int, string> field;",
                MainDescription("delegate TResult System.Func<in T, out TResult>(T arg)"),
                TypeParameterMap(
                    Lines($"\r\nT {FeaturesResources.Is} int",
                          $"TResult {FeaturesResources.Is} string")));
        }

        [WorkItem(538824)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOnDelegateInvocation()
        {
            await TestAsync(@"
class Program
{
    delegate void D1();
    static void Main()
    {
        D1 d = Main;
        $$d(); 
    }
}
",
                MainDescription($"({FeaturesResources.LocalVariable}) D1 d"));
        }

        [WorkItem(539240)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOnArrayCreation1()
        {
            await TestAsync(@"
class Program
{
    static void Main()
    {
        int[] a = n$$ew int[0];
    }
}");
        }

        [WorkItem(539240)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestOnArrayCreation2()
        {
            await TestAsync(@"
class Program
{
    static void Main()
    {
        int[] a = new i$$nt[0];
    }
}",
                MainDescription("struct System.Int32"));
        }

        [WorkItem(539841)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestIsNamedTypeAccessibleForErrorTypes()
        {
            await TestAsync(@"sealed class B<T1, T2> : A<B<T1, T2>>{
    protected sealed override B<A<T>, A$$<T>> N() { }} internal class A<T>{}",
                MainDescription("class A<T>"));
        }

        [WorkItem(540075)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType()
        {
            await TestAsync(@"using Foo = Foo;
class C
{
    void Main()
    {
        $$Foo
    }
}",
                MainDescription("Foo"));
        }

        [WorkItem(540871)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLiterals()
        {
            await TestAsync(@"class MyClass
{
    MyClass()
        : this($$10)
    {
        intI = 2;
    }
 
    public MyClass(int i) { }
 
    static int intI = 1;
 
    public static int Main()
    {
        return 1;
    }
}",
                MainDescription("struct System.Int32"));
        }

        [WorkItem(541444)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorInForeach()
        {
            await TestAsync(@"
class C
{
    void Main()
    {
        foreach (int cc in null)
        {
            $$cc = 1;
        }
    }
}",
                MainDescription($"({FeaturesResources.LocalVariable}) int cc"));
        }

        [WorkItem(540438)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNoQuickInfoOnAnonymousDelegate()
        {
            await TestAsync(@"
using System;

class Program
{
    static void Main(string[] args)
    {
        Action a = $$delegate { };
    }
}");
        }

        [WorkItem(541678)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestQuickInfoOnEvent()
        {
            await TestAsync(@"
using System;
 
public class SampleEventArgs
{
    public SampleEventArgs(string s) { Text = s; }
    public String Text { get; private set; } 
}
public class Publisher
{
    public delegate void SampleEventHandler(object sender, SampleEventArgs e);
    public event SampleEventHandler SampleEvent;
 
    protected virtual void RaiseSampleEvent()
    {
        if (Sam$$pleEvent != null)
            SampleEvent(this, new SampleEventArgs(""Hello""));
    }
}
",
                MainDescription("SampleEventHandler Publisher.SampleEvent"));
        }

        [WorkItem(542157)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEvent()
        {
            await TestInMethodAsync(@"System.Console.CancelKeyPres$$s += null;",
                MainDescription("ConsoleCancelEventHandler Console.CancelKeyPress"));
        }

        [WorkItem(542157)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEventPlusEqualsOperator()
        {
            await TestInMethodAsync(@"System.Console.CancelKeyPress +$$= null;",
                MainDescription("void Console.CancelKeyPress.add"));
        }

        [WorkItem(542157)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEventMinusEqualsOperator()
        {
            await TestInMethodAsync(@"System.Console.CancelKeyPress -$$= null;",
                MainDescription("void Console.CancelKeyPress.remove"));
        }

        [WorkItem(541885)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestQuickInfoOnExtensionMethod()
        {
            await TestWithOptionsAsync(Options.Regular, @"
using System;
using System.Collections.Generic;
using System.Linq;
 
class Program
{
    static void Main(string[] args)
    {
        int[] values = { 1 };
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
",
                MainDescription($"({CSharpFeaturesResources.Extension}) bool int.In<int>(IEnumerable<int> items)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestQuickInfoOnExtensionMethodOverloads()
        {
            await TestWithOptionsAsync(Options.Regular, @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
       ""1"".Test$$Ext();
    }
}
 
public static class Ex
{
    public static void TestExt<T>(this T ex) { }
    public static void TestExt<T>(this T ex, T arg) { }
    public static void TestExt(this string ex, int arg) { }
}
",
                MainDescription($"({CSharpFeaturesResources.Extension}) void string.TestExt<string>() (+ 2 {FeaturesResources.Overloads})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestQuickInfoOnExtensionMethodOverloads2()
        {
            await TestWithOptionsAsync(Options.Regular, @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
       ""1"".Test$$Ext();
    }
}
 
public static class Ex
{
    public static void TestExt<T>(this T ex) { }
    public static void TestExt<T>(this T ex, T arg) { }
    public static void TestExt(this int ex, int arg) { }
}
",
                MainDescription($"({CSharpFeaturesResources.Extension}) void string.TestExt<string>() (+ 1 {FeaturesResources.Overload})"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query1()
        {
            await TestAsync(@"
using System.Linq;
class C
{
    void M()
    {
        var q = from n in new int[] { 1, 2, 3, 4, 5}
                select $$n;
    }
}
",
                MainDescription($"({FeaturesResources.RangeVariable}) int n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query2()
        {
            await TestAsync(@"
using System.Linq;
class C
{
    void M()
    {
        var q = from n$$ in new int[] { 1, 2, 3, 4, 5}
                select n;
    }
}
",
                MainDescription($"({FeaturesResources.RangeVariable}) int n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query3()
        {
            await TestAsync(@"
class C
{
    void M()
    {
        var q = from n in new int[] { 1, 2, 3, 4, 5}
                select $$n;
    }
}
",
                MainDescription($"({FeaturesResources.RangeVariable}) ? n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query4()
        {
            await TestAsync(@"
class C
{
    void M()
    {
        var q = from n$$ in new int[] { 1, 2, 3, 4, 5}
                select n;
    }
}
",
                MainDescription($"({FeaturesResources.RangeVariable}) ? n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query5()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) object n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query6()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) object n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query7()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) int n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query8()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) int n"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query9()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) List<int> x"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query10()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) List<int> x"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query11()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) int y"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Query12()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) int y"));
        }

        [WorkItem(543205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorGlobal()
        {
            await TestAsync(@"extern alias global;
 
class myClass
{
    static int Main()
    {
        $$global::otherClass oc = new global::otherClass();
        return 0;
    }
}",
                MainDescription("<global namespace>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DontRemoveAttributeSuffixAndProduceInvalidIdentifier1()
        {
            await TestAsync(@"
using System;
class classAttribute : Attribute
{
    private classAttribute x$$;
}",
                MainDescription($"({FeaturesResources.Field}) classAttribute classAttribute.x"));
        }

        [WorkItem(544026)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DontRemoveAttributeSuffix2()
        {
            await TestAsync(@"
using System;
class class1Attribute : Attribute
{
    private class1Attribute x$$;
}",
                MainDescription($"({FeaturesResources.Field}) class1Attribute class1Attribute.x"));
        }

        [WorkItem(1696, "https://github.com/dotnet/roslyn/issues/1696")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task AttributeQuickInfoBindsToClassTest()
        {
            await TestAsync(@"
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
",
                Documentation("class comment"));
        }

        [WorkItem(1696, "https://github.com/dotnet/roslyn/issues/1696")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task AttributeConstructorQuickInfo()
        {
            await TestAsync(@"
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
",
                Documentation("ctor comment"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLabel()
        {
            await TestInClassAsync(@"void M() { Foo: int Foo; goto Foo$$; }",
                MainDescription($"({FeaturesResources.Label}) Foo"));
        }

        [WorkItem(542613)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUnboundGeneric()
        {
            await TestAsync(@"
using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        Type t = typeof(L$$ist<>);
    }
}",
                MainDescription("class System.Collections.Generic.List<T>"),
                NoTypeParameterMap);
        }

        [WorkItem(543113)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAnonymousTypeNew1()
        {
            await TestAsync(@"
class C
{
    void M()
    {
        var v = $$new { };
    }
}",
                MainDescription(@"AnonymousType 'a"),
                NoTypeParameterMap,
                AnonymousTypes(
$@"
{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{  }}"));
        }

        [WorkItem(543873)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNestedAnonymousType()
        {
            // verify nested anonymous types are listed in the same order for different properties
            // verify first property
            await TestInMethodAsync(@"var x = new[] { new { Name = ""BillG"", Address = new { Street = ""1 Microsoft Way"", Zip = ""98052"" } } }; x[0].$$Address",
                MainDescription(@"'b 'a.Address { get; }"),
                NoTypeParameterMap,
                AnonymousTypes(
$@"
{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{ string Name, 'b Address }}
    'b {FeaturesResources.Is} new {{ string Street, string Zip }}"));

            // verify second property
            await TestInMethodAsync(@"var x = new[] { new { Name = ""BillG"", Address = new { Street = ""1 Microsoft Way"", Zip = ""98052"" } } }; x[0].$$Name",
                MainDescription(@"string 'a.Name { get; }"),
                NoTypeParameterMap,
                AnonymousTypes(
$@"
{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{ string Name, 'b Address }}
    'b {FeaturesResources.Is} new {{ string Street, string Zip }}"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [WorkItem(543183)]
        public async Task TestAssignmentOperatorInAnonymousType()
        {
            await TestAsync(@"class C
{
    void M()
    {
        var a = new { A $$= 0 };
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [WorkItem(10731, "DevDiv_Projects/Roslyn")]
        public async Task TestErrorAnonymousTypeDoesntShow()
        {
            await TestInMethodAsync(@"var a = new { new { N = 0 }.N, new { } }.$$N;",
                MainDescription(@"int 'a.N { get; }"),
                NoTypeParameterMap,
                AnonymousTypes(
$@"
{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{ int N }}"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [WorkItem(543553)]
        public async Task TestArrayAssignedToVar()
        {
            await TestAsync(@"class C
{
    static void M(string[] args)
    {
        v$$ar a = args;
    }
}
",
                MainDescription("string[]"));
        }

        [WorkItem(529139)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ColorColorRangeVariable()
        {
            await TestAsync(@"
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
",
                MainDescription($"({FeaturesResources.RangeVariable}) N1.yield yield"));
        }

        [WorkItem(543550)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task QuickInfoOnOperator()
        {
            await TestAsync(@"using System.Collections.Generic;
 
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
",
                MainDescription("IEnumerable<Program> Program.operator +(Program p1, Program p2)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantField()
        {
            await TestAsync("class C { const int $$F = 1;",
                MainDescription($"({FeaturesResources.Constant}) int C.F = 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMultipleConstantFields()
        {
            await TestAsync("class C { public const double X = 1.0, Y = 2.0, $$Z = 3.5;",
                MainDescription($"({FeaturesResources.Constant}) double C.Z = 3.5"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantDependencies()
        {
            await TestAsync(@"class A
{
    public const int $$X = B.Z + 1;
    public const int Y = 10;
}
class B
{
    public const int Z = A.Y + 1;
}",
                MainDescription($"({FeaturesResources.Constant}) int A.X = B.Z + 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantCircularDependencies()
        {
            await TestAsync(@"class A
{
    public const int X = B.Z + 1;
}
class B
{
    public const int Z$$ = A.X + 1;
}",
                MainDescription($"({FeaturesResources.Constant}) int B.Z = A.X + 1"));
        }

        [WorkItem(544620)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantOverflow()
        {
            await TestAsync(@"class B
{
    public const int Z$$ = int.MaxValue + 1;
}",
                MainDescription($"({FeaturesResources.Constant}) int B.Z = int.MaxValue + 1"));
        }

        [WorkItem(544620)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantOverflowInUncheckedContext()
        {
            await TestAsync(@"class B
{
    public const int Z$$ = unchecked(int.MaxValue + 1);
}",
                MainDescription($"({FeaturesResources.Constant}) int B.Z = unchecked(int.MaxValue + 1)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEnumInConstantField()
        {
            await TestAsync(@"public class EnumTest
{
    enum Days { Sun, Mon, Tue, Wed, Thu, Fri, Sat };
    static void Main()
    {
        const int $$x = (int)Days.Sun;
    }
}",
                MainDescription($"({FeaturesResources.LocalConstant}) int x = (int)Days.Sun"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantInDefaultExpression()
        {
            await TestAsync(@"public class EnumTest
{
    enum Days { Sun, Mon, Tue, Wed, Thu, Fri, Sat };
    static void Main()
    {
        const Days $$x = default(Days);
    }
}",
                MainDescription($"({FeaturesResources.LocalConstant}) Days x = default(Days)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantParameter()
        {
            await TestAsync("class C { void Bar(int $$b = 1); }",
                MainDescription($"({FeaturesResources.Parameter}) int b = 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantLocal()
        {
            await TestAsync("class C { void Bar() { const int $$loc = 1; }",
                MainDescription($"({FeaturesResources.LocalConstant}) int loc = 1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType1()
        {
            await TestInMethodAsync("var $$v1 = new Foo();",
                MainDescription($"({FeaturesResources.LocalVariable}) Foo v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType2()
        {
            await TestInMethodAsync("var $$v1 = v1;",
                MainDescription($"({FeaturesResources.LocalVariable}) var v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType3()
        {
            await TestInMethodAsync("var $$v1 = new Foo<Bar>();",
                MainDescription($"({FeaturesResources.LocalVariable}) Foo<Bar> v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType4()
        {
            await TestInMethodAsync("var $$v1 = &(x => x);",
                MainDescription($"({FeaturesResources.LocalVariable}) ?* v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType5()
        {
            await TestInMethodAsync("var $$v1 = &v1",
                MainDescription($"({FeaturesResources.LocalVariable}) var* v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType6()
        {
            await TestInMethodAsync("var $$v1 = new Foo[1]",
                MainDescription($"({FeaturesResources.LocalVariable}) Foo[] v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType7()
        {
            await TestInClassAsync("class C { void Method() { } void Foo() { var $$v1 = MethodGroup; } }",
                MainDescription($"({FeaturesResources.LocalVariable}) ? v1"));
        }

        [WorkItem(544416)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestErrorType8()
        {
            await TestInMethodAsync("var $$v1 = Unknown",
                MainDescription($"({FeaturesResources.LocalVariable}) ? v1"));
        }

        [WorkItem(545072)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDelegateSpecialTypes()
        {
            await TestAsync("delegate void $$F(int x);",
                MainDescription("delegate void F(int x)"));
        }

        [WorkItem(545108)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNullPointerParameter()
        {
            await TestAsync("class C { unsafe void $$Foo(int* x = null) { } }",
                MainDescription("void C.Foo([int* x = null])"));
        }

        [WorkItem(545098)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLetIdentifier1()
        {
            await TestInMethodAsync("var q = from e in \"\" let $$y = 1 let a = new { y } select a;",
                MainDescription($"({FeaturesResources.RangeVariable}) int y"));
        }

        [WorkItem(545295)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNullableDefaultValue()
        {
            await TestAsync("class Test { void $$Method(int? t1 = null) { } }",
                MainDescription("void Test.Method([int? t1 = null])"));
        }

        [WorkItem(529586)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInvalidParameterInitializer()
        {
            await TestAsync(
@"class Program { void M1(float $$j1 = ""Hello""
        + 
        ""World"") { } }",
                MainDescription($@"({FeaturesResources.Parameter}) float j1 = ""Hello"" + ""World"""));
        }

        [WorkItem(545230)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestComplexConstLocal()
        {
            await TestAsync(
@"class Program
{
    void Main()
    {
        const int MEGABYTE = 1024 *
            1024 + true;
        Blah($$MEGABYTE);
    }
}
",
                MainDescription($@"({FeaturesResources.LocalConstant}) int MEGABYTE = 1024 * 1024 + true"));
        }

        [WorkItem(545230)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestComplexConstField()
        {
            await TestAsync(
@"class Program
{
    const int a = true 
        - 
        false;
    void Main()
    {
        Foo($$a);
    }
}",
                MainDescription($"({FeaturesResources.Constant}) int Program.a = true - false"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTypeParameterCrefDoesNotHaveQuickInfo()
        {
            await TestAsync(
@"class C<T>
{
    ///  <see cref=""C{X$$}""/>
    static void Main(string[] args)
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref1()
        {
            await TestAsync(
@"class Program
{
    ///  <see cref=""Mai$$n""/>
    static void Main(string[] args)
    {
    }
}",
                MainDescription(@"void Program.Main(string[] args)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref2()
        {
            await TestAsync(
@"class Program
{
    ///  <see cref=""$$Main""/>
    static void Main(string[] args)
    {
    }
}",
                MainDescription(@"void Program.Main(string[] args)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref3()
        {
            await TestAsync(
@"class Program
{
    ///  <see cref=""Main""$$/>
    static void Main(string[] args)
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref4()
        {
            await TestAsync(
@"class Program
{
    ///  <see cref=""Main$$""/>
    static void Main(string[] args)
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref5()
        {
            await TestAsync(
@"class Program
{
    ///  <see cref=""Main""$$/>
    static void Main(string[] args)
    {
    }
}");
        }

        [WorkItem(546849)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestIndexedProperty()
        {
            var markup = @"class Program
{
    void M()
    {
            CCC c = new CCC();
            c.Index$$Prop[0] = ""s"";
    }
}";

            // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
            var referencedCode = @"Imports System.Runtime.InteropServices
<ComImport()>
<GuidAttribute(CCC.ClassId)>
Public Class CCC

#Region ""COM GUIDs""
    Public Const ClassId As String = ""9d965fd2-1514-44f6-accd-257ce77c46b0""
    Public Const InterfaceId As String = ""a9415060-fdf0-47e3-bc80-9c18f7f39cf6""
    Public Const EventsId As String = ""c6a866a5-5f97-4b53-a5df-3739dc8ff1bb""
# End Region

    ''' <summary>
    ''' An index property from VB
    ''' </summary>
    ''' <param name=""p1"">p1 is an integer index</param>
    ''' <returns>A string</returns>
    Public Property IndexProp(ByVal p1 As Integer, Optional ByVal p2 As Integer = 0) As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class";

            await TestWithReferenceAsync(sourceCode: markup,
                referencedCode: referencedCode,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic,
                expectedResults: MainDescription("string CCC.IndexProp[int p1, [int p2 = 0]] { get; set; }"));
        }

        [WorkItem(546918)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUnconstructedGeneric()
        {
            await TestAsync(
@"class A<T> {
    enum SortOrder {
        Ascending,
        Descending,
        None
    }
    void Foo() {
        var b = $$SortOrder.Ascending;
    }
}",
                MainDescription(@"enum A<T>.SortOrder"));
        }

        [WorkItem(546970)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUnconstructedGenericInCRef()
        {
            await TestAsync(
@"
/// <see cref=""$$C{T}"" />
class C<T> { }
",
                MainDescription(@"class C<T>"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAwaitableMethod()
        {
            var markup = @"using System.Threading.Tasks;
class C
{
    async Task Foo()
    {
        Fo$$o();
    }
}";
            var description = $"({CSharpFeaturesResources.Awaitable}) Task C.Foo()";

            var documentation = $@"
{WorkspacesResources.Usage}
  {CSharpFeaturesResources.Await} Foo();";

            await VerifyWithMscorlib45Async(markup, new[] { MainDescription(description), Usage(documentation) });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ObsoleteItem()
        {
            var markup = @"
using System;

class Program
{
    [Obsolete]
    public void foo()
    {
        fo$$o();
    }
}";
            await TestAsync(markup, MainDescription($"[{CSharpFeaturesResources.Deprecated}] void Program.foo()"));
        }

        [WorkItem(751070)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DynamicOperator()
        {
            var markup = @"

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
}";
            await TestAsync(markup, MainDescription("dynamic dynamic.operator ==(dynamic left, dynamic right)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TextOnlyDocComment()
        {
            await TestAsync(@"
/// <summary>
///foo
/// </summary>
class C$$
{
}", Documentation("foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTrimConcatMultiLine()
        {
            await TestAsync(@"
/// <summary>
/// foo
/// bar
/// </summary>
class C$$
{
}", Documentation("foo bar"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref()
        {
            await TestAsync(@"
/// <summary>
/// <see cref=""C""/>
/// <seealso cref=""C""/>
/// </summary>
class C$$
{
}", Documentation("C C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ExcludeTextOutsideSummaryBlock()
        {
            await TestAsync(@"
/// red
/// <summary>
/// green
/// </summary>
/// yellow
class C$$
{
}", Documentation("green"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task NewlineAfterPara()
        {
            await TestAsync(@"
/// <summary>
/// <para>foo</para>
/// </summary>
class C$$
{
}", Documentation("foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TextOnlyDocComment_Metadata()
        {
            var referenced = @"
/// <summary>
///foo
/// </summary>
public class C
{
}";

            var code = @"
class G
{
    void foo()
    {
        C$$ c;
    }
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#", Documentation("foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTrimConcatMultiLine_Metadata()
        {
            var referenced = @"
/// <summary>
/// foo
/// bar
/// </summary>
public class C
{
}";

            var code = @"
class G
{
    void foo()
    {
        C$$ c;
    }
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#", Documentation("foo bar"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCref_Metadata()
        {
            var code = @"
class G
{
    void foo()
    {
        C$$ c;
    }
}";

            var referenced = @"/// <summary>
/// <see cref=""C""/>
/// <seealso cref=""C""/>
/// </summary>
public class C
{
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#", Documentation("C C"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ExcludeTextOutsideSummaryBlock_Metadata()
        {
            var code = @"
class G
{
    void foo()
    {
        C$$ c;
    }
}";

            var referenced = @"
/// red
/// <summary>
/// green
/// </summary>
/// yellow
public class C
{
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#", Documentation("green"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Param()
        {
            await TestAsync(@"
/// <summary></summary>
public class C
{
    /// <typeparam name=""T"">A type parameter of <see cref=""foo{ T} (string[], T)""/></typeparam>
    /// <param name=""args"">First parameter of <see cref=""Foo{T} (string[], T)""/></param>
    /// <param name=""otherParam"">Another parameter of <see cref=""Foo{T}(string[], T)""/></param>
    public void Foo<T>(string[] arg$$s, T otherParam)
    {
    }
}", Documentation("First parameter of C.Foo<T>(string[], T)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Param_Metadata()
        {
            var code = @"
class G
{
    void foo()
    {
        C c;
        c.Foo<int>(arg$$s: new string[] { }, 1);
    }
}";
            var referenced = @"
/// <summary></summary>
public class C
{
    /// <typeparam name=""T"">A type parameter of <see cref=""foo{ T} (string[], T)""/></typeparam>
    /// <param name=""args"">First parameter of <see cref=""Foo{T} (string[], T)""/></param>
    /// <param name=""otherParam"">Another parameter of <see cref=""Foo{T}(string[], T)""/></param>
    public void Foo<T>(string[] args, T otherParam)
    {
    }
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#", Documentation("First parameter of C.Foo<T>(string[], T)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Param2()
        {
            await TestAsync(@"
/// <summary></summary>
public class C
{
    /// <typeparam name=""T"">A type parameter of <see cref=""foo{ T} (string[], T)""/></typeparam>
    /// <param name=""args"">First parameter of <see cref=""Foo{T} (string[], T)""/></param>
    /// <param name=""otherParam"">Another parameter of <see cref=""Foo{T}(string[], T)""/></param>
    public void Foo<T>(string[] args, T oth$$erParam)
    {
    }
}", Documentation("Another parameter of C.Foo<T>(string[], T)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Param2_Metadata()
        {
            var code = @"
class G
{
    void foo()
    {
        C c;
        c.Foo<int>(args: new string[] { }, other$$Param: 1);
    }
}";
            var referenced = @"
/// <summary></summary>
public class C
{
    /// <typeparam name=""T"">A type parameter of <see cref=""foo{ T} (string[], T)""/></typeparam>
    /// <param name=""args"">First parameter of <see cref=""Foo{T} (string[], T)""/></param>
    /// <param name=""otherParam"">Another parameter of <see cref=""Foo{T}(string[], T)""/></param>
        public async Task Foo<T>(string[] args, T otherParam)
    {
    }
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#", Documentation("Another parameter of C.Foo<T>(string[], T)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TypeParam()
        {
            await TestAsync(@"
/// <summary></summary>
public class C
{
    /// <typeparam name=""T"">A type parameter of <see cref=""Foo{T} (string[], T)""/></typeparam>
    /// <param name=""args"">First parameter of <see cref=""Foo{T} (string[], T)""/></param>
    /// <param name=""otherParam"">Another parameter of <see cref=""Foo{T}(string[], T)""/></param>
    public void Foo<T$$>(string[] args, T otherParam)
    {
    }
}", Documentation("A type parameter of C.Foo<T>(string[], T)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task UnboundCref()
        {
            await TestAsync(@"
/// <summary></summary>
public class C
{
    /// <typeparam name=""T"">A type parameter of <see cref=""foo{T}(string[], T)""/></typeparam>
    /// <param name=""args"">First parameter of <see cref=""Foo{T} (string[], T)""/></param>
    /// <param name=""otherParam"">Another parameter of <see cref=""Foo{T}(string[], T)""/></param>
    public void Foo<T$$>(string[] args, T otherParam)
    {
    }
}", Documentation("A type parameter of foo<T>(string[], T)"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInConstructor()
        {
            await TestAsync(@"
public class TestClass
{
    /// <summary> 
    /// This sample shows how to specify the <see cref=""TestClass""/> constructor as a cref attribute.
    /// </summary> 
    public TestClass$$()
    {
    }
}", Documentation("This sample shows how to specify the TestClass constructor as a cref attribute."));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInConstructorOverloaded()
        {
            await TestAsync(@"
public class TestClass
{
    /// <summary> 
    /// This sample shows how to specify the <see cref=""TestClass""/> constructor as a cref attribute.
    /// </summary> 
    public TestClass()
    {
    }

    /// <summary> 
    /// This sample shows how to specify the <see cref=""TestClass(int)""/> constructor as a cref attribute.
    /// </summary> 
    public TestC$$lass(int value)
    { }

    }", Documentation("This sample shows how to specify the TestClass(int) constructor as a cref attribute."));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInGenericMethod1()
        {
            await TestAsync(@"
public class TestClass
{
        /// <summary> 
        /// The GetGenericValue method. 
        /// <para>This sample shows how to specify the <see cref=""GetGenericValue""/> method as a cref attribute.</para>
        /// </summary> 
        public static T GetGenericVa$$lue<T>(T para) { return para; }

    }", Documentation("The GetGenericValue method.\r\n\r\nThis sample shows how to specify the TestClass.GetGenericValue<T>(T) method as a cref attribute."));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInGenericMethod2()
        {
            await TestAsync(@"
public class TestClass
{
        /// <summary> 
        /// The GetGenericValue method. 
        /// <para>This sample shows how to specify the <see cref=""GetGenericValue{T}(T)""/> method as a cref attribute.</para>
        /// </summary> 
        public static T GetGenericVa$$lue<T>(T para) { return para; }

    }", Documentation("The GetGenericValue method.\r\n\r\nThis sample shows how to specify the TestClass.GetGenericValue<T>(T) method as a cref attribute."));
        }

        [WorkItem(813350)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInMethodOverloading1()
        {
            await TestAsync(@"
public class TestClass
{
        public static int GetZero()
        {
            GetGenericValu$$e();
            GetGenericValue(5);
        }

        /// <summary> 
        /// This sample shows how to call the <see cref=""GetGenericValue{T}(T)""/> method
        /// </summary> 
        public static T GetGenericValue<T>(T para) { return para; }

        /// <summary> 
        /// This sample shows how to specify the <see cref=""GetGenericValue""/> method as a cref attribute.
        /// </summary> 
        public static void GetGenericValue() { }

    }", Documentation("This sample shows how to specify the TestClass.GetGenericValue() method as a cref attribute."));
        }

        [WorkItem(813350)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInMethodOverloading2()
        {
            await TestAsync(@"
public class TestClass
{
        public static int GetZero()
        {
            GetGenericValue();
            GetGenericVal$$ue(5);
        }

        /// <summary> 
        /// This sample shows how to call the <see cref=""GetGenericValue{T}(T)""/> method
        /// </summary> 
        public static T GetGenericValue<T>(T para) { return para; }

        /// <summary> 
        /// This sample shows how to specify the <see cref=""GetGenericValue""/> method as a cref attribute.
        /// </summary> 
        public static void GetGenericValue() { }

    }", Documentation("This sample shows how to call the TestClass.GetGenericValue<T>(T) method"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task CrefInGenericType()
        {
            await TestAsync(@"
    /// <summary> 
    /// <remarks>This example shows how to specify the <see cref=""GenericClass{T}""/> cref.</remarks>
    /// </summary> 
    class Generic$$Class<T> { }",
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
        }

        [WorkItem(812720)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ClassificationOfCrefsFromMetadata()
        {
            var code = @"
class G
{
    void foo()
    {
        C c;
        c.Fo$$o();
    }
}";
            var referenced = @"
/// <summary></summary>
public class C
{
    /// <summary> 
    /// See <see cref=""Foo""/> method
    /// </summary> 
    public void Foo()
    {
    }
}";
            await TestWithMetadataReferenceHelperAsync(code, referenced, "C#", "C#",
                Documentation("See C.Foo() method",
                    ExpectedClassifications(
                        Text("See"),
                        WhiteSpace(" "),
                        Class("C"),
                        Punctuation.Text("."),
                        Identifier("Foo"),
                        Punctuation.OpenParen,
                        Punctuation.CloseParen,
                        WhiteSpace(" "),
                        Text("method"))));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task FieldAvailableInBothLinkedFiles()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
    int x;
    void foo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            await VerifyWithReferenceWorkerAsync(markup, new[] { MainDescription($"({FeaturesResources.Field}) int C.x"), Usage("") });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    int x;
#endif
    void foo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = Usage($"\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", expectsWarningGlyph: true);

            await VerifyWithReferenceWorkerAsync(markup, new[] { expectedDescription });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task BindSymbolInOtherFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    int x;
#endif
    void foo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""FOO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = Usage($"\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.NotAvailable)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.Available)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", expectsWarningGlyph: true);

            await VerifyWithReferenceWorkerAsync(markup, new[] { expectedDescription });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task FieldUnavailableInTwoLinkedFiles()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    int x;
#endif
    void foo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = Usage(
                $"\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}",
                expectsWarningGlyph: true);

            await VerifyWithReferenceWorkerAsync(markup, new[] { expectedDescription });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
#if FOO
    int x;
#endif

#if BAR
    void foo()
    {
        x$$
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument"" />
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            var expectedDescription = Usage($"\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", expectsWarningGlyph: true);
            await VerifyWithReferenceWorkerAsync(markup, new[] { expectedDescription });
        }

        [WorkItem(962353)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task NoValidSymbolsInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""SourceDocument""><![CDATA[
class C
{
    void foo()
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";
            await VerifyWithReferenceWorkerAsync(markup);
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LocalsValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            await VerifyWithReferenceWorkerAsync(markup, new[] { MainDescription($"({FeaturesResources.LocalVariable}) int x"), Usage("") });
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LocalWarningInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""PROJ1"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            await VerifyWithReferenceWorkerAsync(markup, new[] { MainDescription($"({FeaturesResources.LocalVariable}) int x"), Usage($"\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}", expectsWarningGlyph: true) });
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task LabelsValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            await VerifyWithReferenceWorkerAsync(markup, new[] { MainDescription($"({FeaturesResources.Label}) LABEL"), Usage("") });
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task RangeVariablesValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            await VerifyWithReferenceWorkerAsync(markup, new[] { MainDescription($"({FeaturesResources.RangeVariable}) int y"), Usage("") });
        }

        [WorkItem(1019766)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task PointerAccessibility()
        {
            var markup = @"class C
{
    unsafe static void Main()
    {
        void* p = null;
        void* q = null;
        dynamic d = true;
        var x = p =$$= q == d;
    }
}";
            await TestAsync(markup, MainDescription("bool void*.operator ==(void* left, void* right)"));
        }

        [WorkItem(1114300)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task AwaitingTaskOfArrayType()
        {
            var markup = @"
using System.Threading.Tasks;

class Program
{
    async Task<int[]> M()
    {
        awa$$it M();
    }
}";
            await TestAsync(markup, MainDescription("int[]"));
        }

        [WorkItem(1114300)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task AwaitingTaskOfDynamic()
        {
            var markup = @"
using System.Threading.Tasks;

class Program
{
    async Task<dynamic> M()
    {
        awa$$it M();
    }
}";
            await TestAsync(markup, MainDescription("dynamic"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void C.Do(int x)";
            await VerifyWithReferenceWorkerAsync(markup, MainDescription(expectedDescription));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored_ContainingType()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""SourceDocument""><![CDATA[
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
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""SourceDocument""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void Methods1.Do(string x)";
            await VerifyWithReferenceWorkerAsync(markup, MainDescription(expectedDescription));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [WorkItem(4868, "https://github.com/dotnet/roslyn/issues/4868")]
        public async Task QuickInfoExceptions()
        {
            await TestAsync(@"
using System;
namespace MyNs
{
    class MyException1 : Exception { }
    class MyException2 : Exception { }
    class TestClass
    {
        /// <exception cref=""MyException1""></exception>
        /// <exception cref=""T:MyNs.MyException2""></exception>
        /// <exception cref=""System.Int32""></exception>
        /// <exception cref=""double""></exception>
        /// <exception cref=""Not_A_Class_But_Still_Displayed""></exception>
        void M()
        {
            M$$();
        }
    }
}
",
                Exceptions($"\r\n{WorkspacesResources.Exceptions}\r\n  MyException1\r\n  MyException2\r\n  int\r\n  double\r\n  Not_A_Class_But_Still_Displayed"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [WorkItem(1516, "https://github.com/dotnet/roslyn/issues/1516")]
        public async Task QuickInfoWithNonStandardSeeAttributesAppear()
        {
            await TestAsync(@"
class C
{
    /// <summary>
    /// <see cref=""System.String"" />
    /// <see href=""http://microsoft.com"" />
    /// <see langword=""null"" />
    /// <see unsupported-attribute=""cat"" />
    /// </summary>
    void M()
    {
        M$$();
    }
}
",
                Documentation(@"string http://microsoft.com null cat"));
        }
    }
}
