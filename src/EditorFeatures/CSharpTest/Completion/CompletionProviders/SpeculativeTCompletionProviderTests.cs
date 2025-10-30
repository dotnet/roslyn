// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

public sealed class SpeculativeTCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(SpeculativeTCompletionProvider);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task IsCommitCharacterTest()
        => VerifyCommonCommitCharactersAsync("""
            class C
            {
                $$
            }
            """, textTypedSoFar: "");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public void IsTextualTriggerCharacterTest()
        => TestCommonIsTextualTriggerCharacter();

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task SendEnterThroughToEditorTest()
    {
        const string markup = """
            class C
            {
                $$
            }
            """;

        await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterOption: EnterKeyRule.Never, expected: false);
        await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterOption: EnterKeyRule.AfterFullyTypedWord, expected: true);
        await VerifySendEnterThroughToEnterAsync(markup, "T", sendThroughEnterOption: EnterKeyRule.Always, expected: true);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InClass()
        => VerifyItemExistsAsync("""
            class C
            {
                $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InInterface()
        => VerifyItemExistsAsync("""
            interface I
            {
                $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InStruct()
        => VerifyItemExistsAsync("""
            struct S
            {
                $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotInNamespace()
        => VerifyItemIsAbsentAsync("""
            namespace N
            {
                $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotInEnum()
        => VerifyItemIsAbsentAsync("""
            enum E
            {
                $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task AfterDelegate()
        => VerifyItemExistsAsync("""
            class C
            {
                delegate $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotAfterVoid()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                void $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotAfterInt()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                int $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InGeneric()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRef0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRef1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref T$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRefGeneric0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRefGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<$$>
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRefGeneric2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<T$$>
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRefGeneric3()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<int, $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public Task InRefReadonlyGeneric()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref readonly Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InQualifiedGeneric0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                System.Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InQualifiedGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                System.Collections.Generic.List<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InRefAndQualifiedGeneric0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref System.Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InRefAndQualifiedGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                internal ref System.Func<int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InRefAndQualifiedNestedGeneric0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                partial ref System.Func<Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InRefAndQualifiedNestedGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                private ref Func<System.Func<int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InRefAndQualifiedNestedGeneric2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                public ref Func<int, System.Func<int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public Task InRefAndQualifiedNestedGeneric3()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                private protected ref Func<int, System.Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTuple0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                protected ($$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task TupleInMethod0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    ($$
                }
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task TupleInMethod1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    var a = 0;
                    ($$
                }
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task TupleInMethod2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    ($$)
                }
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task TupleInMethod3()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    var a = 0;

                    (T$$)

                    a = 1;
                }
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTupleNot0()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                protected sealed (int $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTuple1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                sealed (int, $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTupleNot1()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                virtual (int x, C $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTupleGeneric0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                (Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTupleGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                (int, Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InTupleGeneric2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                (int, Func<int, $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<($$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<int, ($$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple1Not()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Func<int, (T $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<(int, $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple2Not()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Func<(C c, int $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple3()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<int, (int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InGenericTuple3Not()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Func<C, (int, C $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric0()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (Func<System.Func<int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (C c, Func<System.Func<int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (C c, Func<int, System.Func<(int,T$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric3()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (C c, System.Func<Func<int,(T$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric4()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (System.Func<(int,C), (Func<int,T$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric5()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref readonly (System.Func<(int, (C, (Func<int,T$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public Task InRefTupleQualifiedNestedGeneric6()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref readonly (System.Collections.Generic.List<(int, (C, (Func<int,T$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InNestedGeneric1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<Func<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InNestedGeneric2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<Func<int,$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InScript()
        => VerifyItemExistsAsync(@"$$", "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotAfterVoidInScript()
        => VerifyItemIsAbsentAsync(@"void $$", "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotAfterIntInScript()
        => VerifyItemIsAbsentAsync(@"int $$", "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InGenericInScript()
        => VerifyItemExistsAsync("""
            using System;
            Func<$$
            """, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InNestedGenericInScript1()
        => VerifyItemExistsAsync("""
            using System;
            Func<Func<$$
            """, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task InNestedGenericInScript2()
        => VerifyItemExistsAsync("""
            using System;
            Func<Func<int,$$
            """, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotInComment()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                // $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotInXmlDocComment()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                void Goo() { }
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task AfterAsyncTask()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<$$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task NotOkAfterAsync()
        => VerifyItemIsAbsentAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async $$
            }
            """, "T");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")]
    public Task UnionOfItemsFromBothContexts()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
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
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "T", null);

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020654")]
    public Task AfterAsyncTaskWithBraceCompletion()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<$$>
            }
            """, "T");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13480")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task LocalFunctionReturnType()
        => VerifyItemExistsAsync("""
            class C
            {
                public void M()
                {
                    $$
                }
            }
            """, "T");

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task LocalFunctionAfterAyncTask()
        => VerifyItemExistsAsync("""
            class C
            {
                public void M()
                {
                    async Task<$$>
                }
            }
            """, "T");

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task LocalFunctionAfterAsync()
        => VerifyItemExistsAsync("""
            class C
            {
                public void M()
                {
                    async $$
                }
            }
            """, "T");
}
