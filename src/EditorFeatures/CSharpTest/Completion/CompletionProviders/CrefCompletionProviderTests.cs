// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class CrefCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(CrefCompletionProvider);

    private protected override async Task VerifyWorkerAsync(string code, int position, string expectedItemOrNull,
        string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        bool checkForAbsence, Glyph? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string? inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter>? matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions? options = null, bool skipSpeculation = false)
    {
        await VerifyAtPositionAsync(
            code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
            isComplexTextEdit, matchingFilters, flags, options);

        await VerifyAtEndOfFileAsync(
            code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
            isComplexTextEdit, matchingFilters, flags, options);

        // Items cannot be partially written if we're checking for their absence,
        // or if we're verifying that the list will show up (without specifying an actual item)
        if (!checkForAbsence && expectedItemOrNull != null)
        {
            await VerifyAtPosition_ItemPartiallyWrittenAsync(
                code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags: null, options);

            await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
                code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags: null, options);
        }
    }

    [Fact]
    public Task NameCref()
        => VerifyItemExistsAsync("""
            using System;
            namespace Goo
            {
                /// <see cref="$$"/> 
                class Program
                {
                }
            }
            """, "AccessViolationException");

    [Fact]
    public Task QualifiedCref()
        => VerifyItemExistsAsync("""
            using System;
            namespace Goo
            {

                class Program
                {
                    /// <see cref="Program.$$"/> 
                    void goo() { }
                }
            }
            """, "goo");

    [Fact]
    public async Task CrefArgumentList()
    {
        var text = """
            using System;
            namespace Goo
            {

                class Program
                {
                    /// <see cref="Program.goo($$"/> 
                    void goo(int i) { }
                }
            }
            """;
        await VerifyItemIsAbsentAsync(text, "goo(int)");
        await VerifyItemExistsAsync(text, "int");
    }

    [Fact]
    public Task CrefTypeParameterInArgumentList()
        => VerifyItemExistsAsync("""
            using System;
            namespace Goo
            {

                class Program<T>
                {
                    /// <see cref="Program{Q}.goo($$"/> 
                    void goo(T i) { }
                }
            }
            """, "Q");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530887")]
    public Task PrivateMember()
        => VerifyItemExistsAsync("""
            using System;
            namespace Goo
            {
                /// <see cref="C.$$"/> 
                class Program<T>
                {
                }

                class C
                {
                    private int Private;
                    public int Public;
                }
            }
            """, "Private");

    [Fact]
    public Task AfterSingleQuote()
        => VerifyItemExistsAsync("""
            using System;
            namespace Goo
            {
                /// <see cref='$$'/> 
                class Program
                {
                }
            }
            """, "Exception");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531315")]
    public Task EscapePredefinedTypeName()
        => VerifyItemExistsAsync("""
            using System;
            /// <see cref="@vo$$"/>
            class @void { }
            """, "@void");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598159")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531345")]
    public async Task ShowParameterNames()
    {
        var text = """
            /// <see cref="C.$$"/>
            class C
            {
                void M(int x) { }
                void M(ref long x) { }
                void M<T>(T x) { }
            }
            """;
        await VerifyItemExistsAsync(text, "M(int)");
        await VerifyItemExistsAsync(text, "M(ref long)");
        await VerifyItemExistsAsync(text, "M{T}(T)");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531345")]
    public Task ShowTypeParameterNames()
        => VerifyItemExistsAsync("""
            /// <see cref="C$$"/>
            class C<TGoo>
            {
                void M(int x) { }
                void M(long x) { }
                void M(string x) { }
            }
            """, "C{TGoo}");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531156")]
    public async Task ShowConstructors()
    {
        var text = """
            using System;

            /// <see cref="C.$$"/>
            class C<T>
            {
                public C(int x) { }

                public C() { }

                public C(T x) { }
            }
            """;
        await VerifyItemExistsAsync(text, "C()");
        await VerifyItemExistsAsync(text, "C(T)");
        await VerifyItemExistsAsync(text, "C(int)");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598679")]
    public Task NoParamsModifier()
        => VerifyItemExistsAsync("""
            /// <summary>
            /// <see cref="C.$$"/>
            /// </summary>
            class C
                    {
                        void M(int x) { }
                        void M(params long[] x) { }
                    }
            """, "M(long[])");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607773")]
    public Task UnqualifiedTypes()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;
            /// <see cref="List{T}.$$"/>
            class C { }
            """, "Enumerator");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607773")]
    public Task CommitUnqualifiedTypes()
        => VerifyProviderCommitAsync("""
            using System.Collections.Generic;
            /// <see cref="List{T}.Enum$$"/>
            class C { }
            """, "Enumerator", """
            using System.Collections.Generic;
            /// <see cref="List{T}.Enumerator "/>
            class C { }
            """, ' ');

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642285")]
    public async Task SuggestOperators()
    {
        var text = """
            class Test
            {
                /// <see cref="$$"/>
                public static Test operator !(Test t)
                {
                    return new Test();
                }
                public static int operator +(Test t1, Test t2) // Invoke FAR here on operator
                {
                    return 1;
                }
                public static bool operator true(Test t)
                {
                    return true;
                }
                public static bool operator false(Test t)
                {
                    return false;
                }
            }
            """;
        await VerifyItemExistsAsync(text, "operator !(Test)");
        await VerifyItemExistsAsync(text, "operator +(Test, Test)");
        await VerifyItemExistsAsync(text, "operator true(Test)");
        await VerifyItemExistsAsync(text, "operator false(Test)");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641096")]
    public Task SuggestIndexers()
        => VerifyItemExistsAsync("""
            /// <see cref="thi$$"/>
            class Program
            {
                int[] arr;

                public int this[int i]
                {
                    get { return arr[i]; }
                }
            }
            """, "this[int]");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531315")]
    public Task CommitEscapedPredefinedTypeName()
        => VerifyProviderCommitAsync("""
            using System;
            /// <see cref="@vo$$"/>
            class @void { }
            """, "@void", """
            using System;
            /// <see cref="@void "/>
            class @void { }
            """, ' ');

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598159")]
    public async Task RefOutModifiers()
    {
        var text = """
            /// <summary>
            /// <see cref="C.$$"/>
            /// </summary>
            class C
            {
                void M(ref int x) { }
                void M(out long x) { }
            }
            """;
        await VerifyItemExistsAsync(text, "M(ref int)");
        await VerifyItemExistsAsync(text, "M(out long)");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673587")]
    public async Task NestedNamespaces()
    {
        var text = """
            namespace N
            {
                class C
                {
                    void sub() { }
                }
                namespace N
                {
                    class C
                    { }
                }
            }
            class Program
            {
                /// <summary>
                /// <see cref="N.$$"/> // type N. here
                /// </summary>
                static void Main(string[] args)
                {

                }
            }
            """;
        await VerifyItemExistsAsync(text, "N");
        await VerifyItemExistsAsync(text, "C");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730338")]
    public Task PermitTypingTypeParameters()
        => VerifyProviderCommitAsync("""
            using System.Collections.Generic;
            /// <see cref="List$$"/>
            class C { }
            """, "List{T}", """
            using System.Collections.Generic;
            /// <see cref="List{"/>
            class C { }
            """, '{');

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730338")]
    public Task PermitTypingParameterTypes()
        => VerifyProviderCommitAsync("""
            using System.Collections.Generic;
            /// <see cref="goo$$"/>
            class C 
            { 
                public void goo(int x) { }
            }
            """, "goo(int)", """
            using System.Collections.Generic;
            /// <see cref="goo("/>
            class C 
            { 
                public void goo(int x) { }
            }
            """, '(');

    [Fact]
    public async Task CrefCompletionSpeculatesOutsideTrivia()
    {
        var text = """
            /// <see cref="$$
            class C
            {
            }
            """;
        using var workspace = EditorTestWorkspace.Create(LanguageNames.CSharp, new CSharpCompilationOptions(OutputKind.ConsoleApplication), new CSharpParseOptions(), [text], composition: GetComposition());
        var called = false;

        var hostDocument = workspace.DocumentWithCursor;
        var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
        var service = GetCompletionService(document.Project);
        var provider = Assert.IsType<CrefCompletionProvider>(service.GetTestAccessor().GetImportedAndBuiltInProviders([]).Single());
        provider.GetTestAccessor().SetSpeculativeNodeCallback(n =>
        {
            // asserts that we aren't be asked speculate on nodes inside documentation trivia.
            // This verifies that the provider is asking for a speculative SemanticModel
            // by walking to the node the documentation is attached to. 
            Contract.ThrowIfNull(n);
            called = true;
            var parent = n.GetAncestor<DocumentationCommentTriviaSyntax>();
            Assert.Null(parent);
        });

        var completionList = await GetCompletionListAsync(service, document, hostDocument.CursorPosition!.Value, RoslynTrigger.Invoke);

        Assert.True(called);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/16060")]
    public async Task SpecialTypeNames()
    {
        var text = """
            using System;
            /// <see cref="$$"/>
            class C 
            { 
                public void goo(int x) { }
            }
            """;

        await VerifyItemExistsAsync(text, "uint");
        await VerifyItemExistsAsync(text, "UInt32");
    }

    [Fact]
    public Task NoSuggestionAfterEmptyCref()
        => VerifyNoItemsExistAsync("""
            using System;
            /// <see cref="" $$
            class C 
            { 
                public void goo(int x) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23957")]
    public Task CRef_InParameter()
        => VerifyItemExistsAsync("""
            using System;
            class C 
            { 
                /// <see cref="C.My$$
                public void MyMethod(in int x) { }
            }
            """, "MyMethod(in int)");

    [Fact]
    public Task CRef_RefReadonlyParameter()
        => VerifyItemExistsAsync($$"""
            using System;
            class C 
            { 
                /// <see cref="C.My$$
                public void MyMethod(ref readonly int x) { }
            }
            """, "MyMethod(ref readonly int)");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22626")]
    public Task ValueTuple1()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <seealso cref="M$$"/>
                /// </summary>
                public void M((string, int) stringAndInt) { }
            }
            """, "M(ValueTuple{string, int})");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22626")]
    public Task ValueTuple2()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <seealso cref="M$$"/>
                /// </summary>
                public void M((string s, int i) stringAndInt) { }
            }
            """, "M(ValueTuple{string, int})");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43139")]
    public Task TestNonOverload1()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <seealso cref="C.$$"/>
                /// </summary>
                public void M() { }

                void Dispose() { }
            }
            """, "Dispose");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43139")]
    public Task TestNonOverload2()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <seealso cref="$$"/>
                /// </summary>
                public void M() { }

                void Dispose() { }
            }
            """, "Dispose");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43139")]
    public async Task TestOverload1()
    {
        var text = """
            class C
            {
                /// <summary>
                /// <seealso cref="C.$$"/>
                /// </summary>
                public void M() { }

                void Dispose() { }
                void Dispose(bool b) { }
            }
            """;

        await VerifyItemExistsAsync(text, "Dispose()");
        await VerifyItemExistsAsync(text, "Dispose(bool)");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43139")]
    public async Task TestOverload2()
    {
        var text = """
            class C
            {
                /// <summary>
                /// <seealso cref="$$"/>
                /// </summary>
                public void M() { }

                void Dispose() { }
                void Dispose(bool b) { }
            }
            """;

        await VerifyItemExistsAsync(text, "Dispose()");
        await VerifyItemExistsAsync(text, "Dispose(bool)");
    }
}
