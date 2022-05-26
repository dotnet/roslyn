// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class SpeculativeTCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(SpeculativeTCompletionProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IsCommitCharacterTest()
        {
            const string markup = @"
class C
{
    $$
}";

            await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IsTextualTriggerCharacterTest()
            => TestCommonIsTextualTriggerCharacter();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SendEnterThroughToEditorTest()
        {
            const string markup = @"
class C
{
    $$
}";

            await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
            await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
            await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InClass()
        {
            var markup = @"
class C
{
    $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InInterface()
        {
            var markup = @"
interface I
{
    $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InStruct()
        {
            var markup = @"
struct S
{
    $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInNamespace()
        {
            var markup = @"
namespace N
{
    $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInEnum()
        {
            var markup = @"
enum E
{
    $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterDelegate()
        {
            var markup = @"
class C
{
    delegate $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterVoid()
        {
            var markup = @"
class C
{
    void $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterInt()
        {
            var markup = @"
class C
{
    int $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InGeneric()
        {
            var markup = @"
using System;
class C
{
    Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRef0()
        {
            var markup = @"
using System;
class C
{
    ref $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRef1()
        {
            var markup = @"
using System;
class C
{
    ref T$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRefGeneric0()
        {
            var markup = @"
using System;
class C
{
    ref Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRefGeneric1()
        {
            var markup = @"
using System;
class C
{
    ref Func<$$>
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRefGeneric2()
        {
            var markup = @"
using System;
class C
{
    ref Func<T$$>
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRefGeneric3()
        {
            var markup = @"
using System;
class C
{
    ref Func<int, $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        public async Task InRefReadonlyGeneric()
        {
            var markup = @"
using System;
class C
{
    ref readonly Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InQualifiedGeneric0()
        {
            var markup = @"
using System;
class C
{
    System.Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InQualifiedGeneric1()
        {
            var markup = @"
using System;
class C
{
    System.Collections.Generic.List<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InRefAndQualifiedGeneric0()
        {
            var markup = @"
using System;
class C
{
    ref System.Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InRefAndQualifiedGeneric1()
        {
            var markup = @"
using System;
class C
{
    internal ref System.Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InRefAndQualifiedNestedGeneric0()
        {
            var markup = @"
using System;
class C
{
    partial ref System.Func<Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InRefAndQualifiedNestedGeneric1()
        {
            var markup = @"
using System;
class C
{
    private ref Func<System.Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InRefAndQualifiedNestedGeneric2()
        {
            var markup = @"
using System;
class C
{
    public ref Func<int, System.Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37224, "https://github.com/dotnet/roslyn/issues/37224")]
        [WorkItem(37268, "https://github.com/dotnet/roslyn/issues/37268")]
        public async Task InRefAndQualifiedNestedGeneric3()
        {
            var markup = @"
using System;
class C
{
    private protected ref Func<int, System.Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTuple0()
        {
            var markup = @"
using System;
class C
{
    protected ($$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task TupleInMethod0()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        ($$
    }
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task TupleInMethod1()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        var a = 0;
        ($$
    }
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task TupleInMethod2()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        ($$)
    }
}";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task TupleInMethod3()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        var a = 0;

        (T$$)

        a = 1;
    }
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTupleNot0()
        {
            var markup = @"
using System;
class C
{
    protected sealed (int $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTuple1()
        {
            var markup = @"
using System;
class C
{
    sealed (int, $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTupleNot1()
        {
            var markup = @"
using System;
class C
{
    virtual (int x, C $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTupleGeneric0()
        {
            var markup = @"
using System;
class C
{
    (Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTupleGeneric1()
        {
            var markup = @"
using System;
class C
{
    (int, Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InTupleGeneric2()
        {
            var markup = @"
using System;
class C
{
    (int, Func<int, $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple0()
        {
            var markup = @"
using System;
class C
{
    Func<($$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple1()
        {
            var markup = @"
using System;
class C
{
    Func<int, ($$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple1Not()
        {
            var markup = @"
using System;
class C
{
    Func<int, (T $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple2()
        {
            var markup = @"
using System;
class C
{
    Func<(int, $$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple2Not()
        {
            var markup = @"
using System;
class C
{
    Func<(C c, int $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple3()
        {
            var markup = @"
using System;
class C
{
    Func<int, (int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InGenericTuple3Not()
        {
            var markup = @"
using System;
class C
{
    Func<C, (int, C $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric0()
        {
            var markup = @"
using System;
class C
{
    ref (Func<System.Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric1()
        {
            var markup = @"
using System;
class C
{
    ref (C c, Func<System.Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric2()
        {
            var markup = @"
using System;
class C
{
    ref (C c, Func<int, System.Func<(int,T$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric3()
        {
            var markup = @"
using System;
class C
{
    ref (C c, System.Func<Func<int,(T$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric4()
        {
            var markup = @"
using System;
class C
{
    ref (System.Func<(int,C), (Func<int,T$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric5()
        {
            var markup = @"
using System;
class C
{
    ref readonly (System.Func<(int, (C, (Func<int,T$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37361, "https://github.com/dotnet/roslyn/issues/37361")]
        public async Task InRefTupleQualifiedNestedGeneric6()
        {
            var markup = @"
using System;
class C
{
    ref readonly (System.Collections.Generic.List<(int, (C, (Func<int,T$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGeneric1()
        {
            var markup = @"
using System;
class C
{
    Func<Func<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGeneric2()
        {
            var markup = @"
using System;
class C
{
    Func<Func<int,$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InScript()
        {
            var markup = @"$$";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterVoidInScript()
        {
            var markup = @"void $$";

            await VerifyItemIsAbsentAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterIntInScript()
        {
            var markup = @"int $$";

            await VerifyItemIsAbsentAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InGenericInScript()
        {
            var markup = @"
using System;
Func<$$
";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGenericInScript1()
        {
            var markup = @"
using System;
Func<Func<$$
";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InNestedGenericInScript2()
        {
            var markup = @"
using System;
Func<Func<int,$$
";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInComment()
        {
            var markup = @"
class C
{
    // $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInXmlDocComment()
        {
            var markup = @"
class C
{
    /// <summary>
    /// $$
    /// </summary>
    void Goo() { }
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsyncTask()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async Task<$$
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotOkAfterAsync()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async $$
}";

            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [WorkItem(968256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task UnionOfItemsFromBothContexts()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    void goo() {
#endif

$$

#if GOO
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            await VerifyItemInLinkedFilesAsync(markup, "T", null);
        }

        [WorkItem(1020654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020654")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsyncTaskWithBraceCompletion()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async Task<$$>
}";

            await VerifyItemExistsAsync(markup, "T");
        }

        [WorkItem(13480, "https://github.com/dotnet/roslyn/issues/13480")]
        [Fact]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task LocalFunctionReturnType()
        {
            var markup = @"
class C
{
    public void M()
    {
        $$
    }
}";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task LocalFunctionAfterAyncTask()
        {
            var markup = @"
class C
{
    public void M()
    {
        async Task<$$>
    }
}";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task LocalFunctionAfterAsync()
        {
            var markup = @"
class C
{
    public void M()
    {
        async $$
    }
}";
            await VerifyItemExistsAsync(markup, "T");
        }
    }
}
