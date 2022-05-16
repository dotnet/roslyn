// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class CrefCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(CrefCompletionProvider);

        private protected override async Task VerifyWorkerAsync(
            string code, int position,
            string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
            List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, bool skipSpeculation = false)
        {
            await VerifyAtPositionAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags);

            await VerifyAtEndOfFileAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                    displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters);

                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                    displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NameCref()
        {
            var text = @"using System;
namespace Goo
{
    /// <see cref=""$$""/> 
    class Program
    {
    }
}";
            await VerifyItemExistsAsync(text, "AccessViolationException");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task QualifiedCref()
        {
            var text = @"using System;
namespace Goo
{

    class Program
    {
        /// <see cref=""Program.$$""/> 
        void goo() { }
    }
}";
            await VerifyItemExistsAsync(text, "goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CrefArgumentList()
        {
            var text = @"using System;
namespace Goo
{

    class Program
    {
        /// <see cref=""Program.goo($$""/> 
        void goo(int i) { }
    }
}";
            await VerifyItemIsAbsentAsync(text, "goo(int)");
            await VerifyItemExistsAsync(text, "int");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CrefTypeParameterInArgumentList()
        {
            var text = @"using System;
namespace Goo
{

    class Program<T>
    {
        /// <see cref=""Program{Q}.goo($$""/> 
        void goo(T i) { }
    }
}";
            await VerifyItemExistsAsync(text, "Q");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion), WorkItem(530887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530887")]
        public async Task PrivateMember()
        {
            var text = @"using System;
namespace Goo
{
    /// <see cref=""C.$$""/> 
    class Program<T>
    {
    }

    class C
    {
        private int Private;
        public int Public;
    }
}";
            await VerifyItemExistsAsync(text, "Private");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterSingleQuote()
        {
            var text = @"using System;
namespace Goo
{
    /// <see cref='$$'/> 
    class Program
    {
    }
}";
            await VerifyItemExistsAsync(text, "Exception");
        }

        [WorkItem(531315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531315")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EscapePredefinedTypeName()
        {
            var text = @"using System;
/// <see cref=""@vo$$""/>
class @void { }
";
            await VerifyItemExistsAsync(text, "@void");
        }

        [WorkItem(531345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531345")]
        [WorkItem(598159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598159")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowParameterNames()
        {
            var text = @"/// <see cref=""C.$$""/>
class C
{
    void M(int x) { }
    void M(ref long x) { }
    void M<T>(T x) { }
}

";
            await VerifyItemExistsAsync(text, "M(int)");
            await VerifyItemExistsAsync(text, "M(ref long)");
            await VerifyItemExistsAsync(text, "M{T}(T)");
        }

        [WorkItem(531345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531345")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowTypeParameterNames()
        {
            var text = @"/// <see cref=""C$$""/>
class C<TGoo>
{
    void M(int x) { }
    void M(long x) { }
    void M(string x) { }
}

";
            await VerifyItemExistsAsync(text, "C{TGoo}");
        }

        [WorkItem(531156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531156")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowConstructors()
        {
            var text = @"using System;

/// <see cref=""C.$$""/>
class C<T>
{
    public C(int x) { }

    public C() { }

    public C(T x) { }
}

";
            await VerifyItemExistsAsync(text, "C");
            await VerifyItemExistsAsync(text, "C(T)");
            await VerifyItemExistsAsync(text, "C(int)");
        }

        [WorkItem(598679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598679")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoParamsModifier()
        {
            var text = @"/// <summary>
/// <see cref=""C.$$""/>
/// </summary>
class C
        {
            void M(int x) { }
            void M(params long[] x) { }
        }


";
            await VerifyItemExistsAsync(text, "M(long[])");
        }

        [WorkItem(607773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607773")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UnqualifiedTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List{T}.$$""/>
class C { }
";
            await VerifyItemExistsAsync(text, "Enumerator");
        }

        [WorkItem(607773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607773")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitUnqualifiedTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List{T}.Enum$$""/>
class C { }
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""List{T}.Enumerator ""/>
class C { }
";
            await VerifyProviderCommitAsync(text, "Enumerator", expected, ' ');
        }

        [WorkItem(642285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642285")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SuggestOperators()
        {
            var text = @"
class Test
{
    /// <see cref=""$$""/>
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
";
            await VerifyItemExistsAsync(text, "operator !(Test)");
            await VerifyItemExistsAsync(text, "operator +(Test, Test)");
            await VerifyItemExistsAsync(text, "operator true(Test)");
            await VerifyItemExistsAsync(text, "operator false(Test)");
        }

        [WorkItem(641096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641096")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SuggestIndexers()
        {
            var text = @"
/// <see cref=""thi$$""/>
class Program
{
    int[] arr;

    public int this[int i]
    {
        get { return arr[i]; }
    }
}
";
            await VerifyItemExistsAsync(text, "this[int]");
        }

        [WorkItem(531315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531315")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitEscapedPredefinedTypeName()
        {
            var text = @"using System;
/// <see cref=""@vo$$""/>
class @void { }
";

            var expected = @"using System;
/// <see cref=""@void ""/>
class @void { }
";
            await VerifyProviderCommitAsync(text, "@void", expected, ' ');
        }

        [WorkItem(598159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598159")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RefOutModifiers()
        {
            var text = @"/// <summary>
/// <see cref=""C.$$""/>
/// </summary>
class C
{
    void M(ref int x) { }
    void M(out long x) { }
}

";
            await VerifyItemExistsAsync(text, "M(ref int)");
            await VerifyItemExistsAsync(text, "M(out long)");
        }

        [WorkItem(673587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673587")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedNamespaces()
        {
            var text = @"namespace N
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
    /// <see cref=""N.$$""/> // type N. here
    /// </summary>
    static void Main(string[] args)
    {

    }
}";
            await VerifyItemExistsAsync(text, "N");
            await VerifyItemExistsAsync(text, "C");
        }

        [WorkItem(730338, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730338")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PermitTypingTypeParameters()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List$$""/>
class C { }
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""List{""/>
class C { }
";
            await VerifyProviderCommitAsync(text, "List{T}", expected, '{');
        }

        [WorkItem(730338, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730338")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PermitTypingParameterTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""goo$$""/>
class C 
{ 
    public void goo(int x) { }
}
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""goo(""/>
class C 
{ 
    public void goo(int x) { }
}
";
            await VerifyProviderCommitAsync(text, "goo(int)", expected, '(');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CrefCompletionSpeculatesOutsideTrivia()
        {
            var text = @"
/// <see cref=""$$
class C
{
}";
            using var workspace = TestWorkspace.Create(LanguageNames.CSharp, new CSharpCompilationOptions(OutputKind.ConsoleApplication), new CSharpParseOptions(), new[] { text }, exportProvider: ExportProvider);
            var called = false;

            var hostDocument = workspace.DocumentWithCursor;
            var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            var service = GetCompletionService(document.Project);
            var provider = Assert.IsType<CrefCompletionProvider>(service.GetTestAccessor().GetAllProviders(ImmutableHashSet<string>.Empty).Single());
            provider.GetTestAccessor().SetSpeculativeNodeCallback(n =>
            {
                // asserts that we aren't be asked speculate on nodes inside documentation trivia.
                // This verifies that the provider is asking for a speculative SemanticModel
                // by walking to the node the documentation is attached to. 

                called = true;
                var parent = n.GetAncestor<DocumentationCommentTriviaSyntax>();
                Assert.Null(parent);
            });

            var completionList = await GetCompletionListAsync(service, document, hostDocument.CursorPosition.Value, RoslynTrigger.Invoke);

            Assert.True(called);
        }

        [WorkItem(16060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/16060")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SpecialTypeNames()
        {
            var text = @"
using System;
/// <see cref=""$$""/>
class C 
{ 
    public void goo(int x) { }
}
";

            await VerifyItemExistsAsync(text, "uint");
            await VerifyItemExistsAsync(text, "UInt32");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoSuggestionAfterEmptyCref()
        {
            var text = @"
using System;
/// <see cref="""" $$
class C 
{ 
    public void goo(int x) { }
}
";

            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(23957, "https://github.com/dotnet/roslyn/issues/23957")]
        public async Task CRef_InParameter()
        {
            var text = @"
using System;
class C 
{ 
    /// <see cref=""C.My$$
    public void MyMethod(in int x) { }
}
";

            await VerifyItemExistsAsync(text, "MyMethod(in int)");
        }
    }
}
