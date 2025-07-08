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
    public async Task IsCommitCharacterTest()
    {
        const string markup = """
            class C
            {
                $$
            }
            """;

        await VerifyCommonCommitCharactersAsync(markup, textTypedSoFar: "");
    }

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
    public async Task InClass()
    {
        await VerifyItemExistsAsync("""
            class C
            {
                $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InInterface()
    {
        await VerifyItemExistsAsync("""
            interface I
            {
                $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InStruct()
    {
        await VerifyItemExistsAsync("""
            struct S
            {
                $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotInNamespace()
    {
        await VerifyItemIsAbsentAsync("""
            namespace N
            {
                $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotInEnum()
    {
        await VerifyItemIsAbsentAsync("""
            enum E
            {
                $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task AfterDelegate()
    {
        await VerifyItemExistsAsync("""
            class C
            {
                delegate $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotAfterVoid()
    {
        await VerifyItemIsAbsentAsync("""
            class C
            {
                void $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotAfterInt()
    {
        await VerifyItemIsAbsentAsync("""
            class C
            {
                int $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InGeneric()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRef0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRef1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref T$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRefGeneric0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRefGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<$$>
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRefGeneric2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<T$$>
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRefGeneric3()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref Func<int, $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    public async Task InRefReadonlyGeneric()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref readonly Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InQualifiedGeneric0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                System.Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InQualifiedGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                System.Collections.Generic.List<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InRefAndQualifiedGeneric0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref System.Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InRefAndQualifiedGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                internal ref System.Func<int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InRefAndQualifiedNestedGeneric0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                partial ref System.Func<Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InRefAndQualifiedNestedGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                private ref Func<System.Func<int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InRefAndQualifiedNestedGeneric2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                public ref Func<int, System.Func<int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37224")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37268")]
    public async Task InRefAndQualifiedNestedGeneric3()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                private protected ref Func<int, System.Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTuple0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                protected ($$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task TupleInMethod0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    ($$
                }
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task TupleInMethod1()
    {
        await VerifyItemExistsAsync("""
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task TupleInMethod2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    ($$)
                }
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task TupleInMethod3()
    {
        await VerifyItemExistsAsync("""
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTupleNot0()
    {
        await VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                protected sealed (int $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTuple1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                sealed (int, $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTupleNot1()
    {
        await VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                virtual (int x, C $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTupleGeneric0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                (Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTupleGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                (int, Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InTupleGeneric2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                (int, Func<int, $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<($$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<int, ($$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple1Not()
    {
        await VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Func<int, (T $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<(int, $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple2Not()
    {
        await VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Func<(C c, int $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple3()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<int, (int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InGenericTuple3Not()
    {
        await VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Func<C, (int, C $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric0()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (Func<System.Func<int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (C c, Func<System.Func<int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (C c, Func<int, System.Func<(int,T$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric3()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (C c, System.Func<Func<int,(T$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric4()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref (System.Func<(int,C), (Func<int,T$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric5()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref readonly (System.Func<(int, (C, (Func<int,T$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37361")]
    public async Task InRefTupleQualifiedNestedGeneric6()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                ref readonly (System.Collections.Generic.List<(int, (C, (Func<int,T$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InNestedGeneric1()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<Func<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InNestedGeneric2()
    {
        await VerifyItemExistsAsync("""
            using System;
            class C
            {
                Func<Func<int,$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InScript()
    {
        await VerifyItemExistsAsync(@"$$", "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotAfterVoidInScript()
    {
        await VerifyItemIsAbsentAsync(@"void $$", "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotAfterIntInScript()
    {
        await VerifyItemIsAbsentAsync(@"int $$", "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InGenericInScript()
    {
        await VerifyItemExistsAsync("""
            using System;
            Func<$$
            """, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InNestedGenericInScript1()
    {
        await VerifyItemExistsAsync("""
            using System;
            Func<Func<$$
            """, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task InNestedGenericInScript2()
    {
        await VerifyItemExistsAsync("""
            using System;
            Func<Func<int,$$
            """, "T", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotInComment()
    {
        await VerifyItemIsAbsentAsync("""
            class C
            {
                // $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotInXmlDocComment()
    {
        await VerifyItemIsAbsentAsync("""
            class C
            {
                /// <summary>
                /// $$
                /// </summary>
                void Goo() { }
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task AfterAsyncTask()
    {
        await VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<$$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public async Task NotOkAfterAsync()
    {
        await VerifyItemIsAbsentAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async $$
            }
            """, "T");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")]
    public async Task UnionOfItemsFromBothContexts()
    {
        await VerifyItemInLinkedFilesAsync("""
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020654")]
    public async Task AfterAsyncTaskWithBraceCompletion()
    {
        await VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<$$>
            }
            """, "T");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13480")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public async Task LocalFunctionReturnType()
    {
        await VerifyItemExistsAsync("""
            class C
            {
                public void M()
                {
                    $$
                }
            }
            """, "T");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public async Task LocalFunctionAfterAyncTask()
    {
        await VerifyItemExistsAsync("""
            class C
            {
                public void M()
                {
                    async Task<$$>
                }
            }
            """, "T");
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public async Task LocalFunctionAfterAsync()
    {
        await VerifyItemExistsAsync("""
            class C
            {
                public void M()
                {
                    async $$
                }
            }
            """, "T");
    }
}
