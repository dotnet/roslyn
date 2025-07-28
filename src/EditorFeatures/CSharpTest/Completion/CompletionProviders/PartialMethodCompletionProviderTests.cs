// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class PartialMethodCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(PartialMethodCompletionProvider);

    [Fact]
    public Task NoPartialMethods1()
        => VerifyNoItemsExistAsync("""
            class c
            {
                $$
            }
            """);

    [Fact]
    public Task NoPartialMethods2()
        => VerifyNoItemsExistAsync("""
            class c
            {
                private void goo() { };

                partial void $$
            }
            """);

    [Fact]
    public Task NoExtendedPartialMethods2()
        => VerifyNoItemsExistAsync("""
            class c
            {
                public void goo() { };

                public void $$
            }
            """);

    [Fact]
    public Task PartialMethodInPartialClass()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                partial void $$
            }
            """, "goo()");

    [Fact]
    public Task ExtendedPartialMethodInPartialClass()
        => VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                public partial void $$
            }
            """, "goo()");

    [Fact]
    public Task PartialMethodInPartialGenericClass()
        => VerifyItemExistsAsync("""
            partial class c<T>
            {
                partial void goo(T bar);

                partial void $$
            }
            """, "goo(T bar)");

    [Fact]
    public Task ExtendedPartialMethodInPartialGenericClass()
        => VerifyItemExistsAsync("""
            partial class c<T>
            {
                public partial void goo(T bar);

                public partial void $$
            }
            """, "goo(T bar)");

    [Fact]
    public Task PartialMethodInPartialStruct()
        => VerifyItemExistsAsync("""
            partial struct c
            {
                partial void goo();

                partial void $$
            }
            """, "goo()");

    [Fact]
    public Task ExtendedPartialMethodInPartialStruct()
        => VerifyItemExistsAsync("""
            partial struct c
            {
                public partial void goo();

                public partial void $$
            }
            """, "goo()");

    [Fact]
    public Task CompletionOnPartial1()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                partial $$
            }
            """, "goo()");

    [Fact]
    public Task CompletionOnExtendedPartial1()
        => VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                partial $$
            }
            """, "goo()");

    [Fact]
    public Task CompletionOnPartial2()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task CompletionOnExtendedPartial2()
        => VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task StaticUnsafePartial()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial static unsafe void goo();

                void static unsafe partial $$
            }
            """, "goo()");

    [Fact]
    public Task StaticUnsafeExtendedPartial()
        => VerifyItemExistsAsync("""
            partial class c
            {
                public partial static unsafe void goo();

                void static unsafe partial $$
            }
            """, "goo()");

    [Fact]
    public Task PartialCompletionWithPrivate()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial static unsafe void goo();

                private partial $$
            }
            """, "goo()");

    [Fact]
    public Task PartialCompletionWithPublic()
        => VerifyItemExistsAsync("""
            partial class c
            {
                public partial static unsafe void goo();

                public partial $$
            }
            """, "goo()");

    [Fact]
    public Task NotCompletionDespiteValidModifier()
        => VerifyNoItemsExistAsync("""
            partial class c
            {
                partial void goo();

                void partial unsafe $$
            }
            """);

    [Fact]
    public Task NoExtendedCompletionDespiteValidModifier()
        => VerifyNoItemsExistAsync("""
            partial class c
            {
                public partial void goo();

                void partial unsafe $$
            }
            """);

    [Fact]
    public Task YesIfPublic()
        => VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task YesIfInternal()
        => VerifyItemExistsAsync("""
            partial class c
            {
                internal partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task YesIfProtected()
        => VerifyItemExistsAsync("""
            partial class c
            {
                protected partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task YesIfProtectedInternal()
        => VerifyItemExistsAsync("""
            partial class c
            {
                protected internal partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task YesIfExtern()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                extern void partial $$
            }
            """, "goo()");

    [Fact]
    public Task YesIfVirtual()
        => VerifyItemExistsAsync("""
            partial class c
            {
                virtual partial void goo();

                void partial $$
            }
            """, "goo()");

    [Fact]
    public Task YesIfNonVoidReturnType()
        => VerifyItemExistsAsync("""
            partial class c
            {
                partial int goo();

                partial $$
            }
            """, "goo()");

    [Fact]
    public Task NotInsideInterface()
        => VerifyNoItemsExistAsync("""
            partial interface i
            {
                partial void goo();

                partial $$
            }
            """);

    [WpfFact]
    public Task CommitInPartialClass()
        => VerifyCustomCommitProviderAsync("""
            partial class c
            {
                partial void goo();

                partial $$
            }
            """, "goo()", """
            partial class c
            {
                partial void goo();

                partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitInExtendedPartialClass()
        => VerifyCustomCommitProviderAsync("""
            partial class c
            {
                public partial void goo();

                partial $$
            }
            """, "goo()", """
            partial class c
            {
                public partial void goo();

                public partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitGenericPartialMethod()
        => VerifyCustomCommitProviderAsync("""
            partial class c<T>
            {
                partial void goo(T bar);

                partial $$
            }
            """, "goo(T bar)", """
            partial class c<T>
            {
                partial void goo(T bar);

                partial void goo(T bar)
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitGenericExtendedPartialMethod()
        => VerifyCustomCommitProviderAsync("""
            partial class c<T>
            {
                public partial void goo(T bar);

                partial $$
            }
            """, "goo(T bar)", """
            partial class c<T>
            {
                public partial void goo(T bar);

                public partial void goo(T bar)
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitMethodErasesPrivate()
        => VerifyCustomCommitProviderAsync("""
            partial class c
            {
                partial void goo();

                private partial $$
            }
            """, "goo()", """
            partial class c
            {
                partial void goo();

                partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitMethodKeepsExtendedPrivate()
        => VerifyCustomCommitProviderAsync("""
            partial class c
            {
                private partial void goo();

                private partial $$
            }
            """, "goo()", """
            partial class c
            {
                private partial void goo();

                private partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitInPartialClassPart()
        => VerifyCustomCommitProviderAsync("""
            partial class c
            {
                partial void goo();
            }

            partial class c
            {
                partial $$
            }
            """, "goo()", """
            partial class c
            {
                partial void goo();
            }

            partial class c
            {
                partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitInExtendedPartialClassPart()
        => VerifyCustomCommitProviderAsync("""
            partial class c
            {
                public partial void goo();
            }

            partial class c
            {
                partial $$
            }
            """, "goo()", """
            partial class c
            {
                public partial void goo();
            }

            partial class c
            {
                public partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [WpfFact]
    public Task CommitInPartialStruct()
        => VerifyCustomCommitProviderAsync("""
            partial struct c
            {
                partial void goo();

                partial $$
            }
            """, "goo()", """
            partial struct c
            {
                partial void goo();

                partial void goo()
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    [Fact]
    public Task NotIfNoPartialKeyword()
        => VerifyNoItemsExistAsync("""
            partial class C
                {
                    partial void Goo();
                }

                partial class C
                {
                    void $$
                }
            """);

    [Fact]
    public Task NotIfNoExtendedPartialKeyword()
        => VerifyNoItemsExistAsync("""
            partial class C
                {
                    public partial void Goo();
                }

                partial class C
                {
                    void $$
                }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578757")]
    public Task DoNotConsiderFollowingDeclarationPartial()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                partial $$

                void Goo()
                {

                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578757")]
    public Task DoNotConsiderFollowingDeclarationExtendedPartial()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                public partial $$

                void Goo()
                {

                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
    public Task CommitAsync()
        => VerifyCustomCommitProviderAsync("""
            using System;

            partial class Bar
            {
                partial void Goo();

                async partial $$
            }
            """, "Goo()", """
            using System;

            partial class Bar
            {
                partial void Goo();

                async partial void Goo()
                {
                    [|throw new NotImplementedException();|]
                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
    public Task CommitAsyncExtended()
        => VerifyCustomCommitProviderAsync("""
            using System;

            partial class Bar
            {
                public partial void Goo();

                async partial $$
            }
            """, "Goo()", """
            using System;

            partial class Bar
            {
                public partial void Goo();

                public async partial void Goo()
                {
                    [|throw new NotImplementedException();|]
                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
    public Task AmbiguityCommittingWithParen()
        => VerifyCustomCommitProviderAsync("""
            using System;

            partial class Bar
            {
                partial void Goo();

                partial Goo$$
            }
            """, "Goo()", """
            using System;

            partial class Bar
            {
                partial void Goo();

                partial void Goo()
                {
                    [|throw new NotImplementedException();|]
                }
            }
            """, commitChar: '(');

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965677")]
    public Task NoDefaultParameterValues()
        => VerifyCustomCommitProviderAsync("""
            namespace PartialClass
            {
                partial class PClass
                {
                    partial void PMethod(int i = 0);

                    partial $$
                }
            }
            """, "PMethod(int i)", """
            namespace PartialClass
            {
                partial class PClass
                {
                    partial void PMethod(int i = 0);

                    partial void PMethod(int i)
                    {
                        [|throw new System.NotImplementedException();|]
                    }
                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965677")]
    public Task NoDefaultParameterValuesExtended()
        => VerifyCustomCommitProviderAsync("""
            namespace PartialClass
            {
                partial class PClass
                {
                    public partial void PMethod(int i = 0);

                    partial $$
                }
            }
            """, "PMethod(int i)", """
            namespace PartialClass
            {
                partial class PClass
                {
                    public partial void PMethod(int i = 0);

                    public partial void PMethod(int i)
                    {
                        [|throw new System.NotImplementedException();|]
                    }
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/26388")]
    public async Task ExpressionBodyMethod()
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());
        workspace.SetAnalyzerFallbackOptions(new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent) }
        });
        await VerifyCustomCommitProviderAsync("""
            using System;
            partial class Bar
            {
                partial void Foo();
                partial $$
            }
            """, "Foo()", """
            using System;
            partial class Bar
            {
                partial void Foo();
                partial void Foo() => [|throw new NotImplementedException()|];
            }
            """);
    }

    [WpfFact]
    public async Task ExpressionBodyMethodExtended()
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());
        workspace.SetAnalyzerFallbackOptions(new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent) }
        });
        await VerifyCustomCommitProviderAsync("""
            using System;
            partial class Bar
            {
                public partial void Foo();
                partial $$
            }
            """, "Foo()", """
            using System;
            partial class Bar
            {
                public partial void Foo();
                public partial void Foo() => [|throw new NotImplementedException()|];
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/68805")]
    public Task TestUnsafePartial()
        => VerifyCustomCommitProviderAsync("""
            unsafe partial class A
            {
                partial void Goo(void* p);
            }

            partial class A
            {
                partial $$
            }
            """, "Goo(void* p)", """
            unsafe partial class A
            {
                partial void Goo(void* p);
            }
            
            partial class A
            {
                unsafe partial void Goo(void* p)
                {
                    [|throw new System.NotImplementedException();|]
                }
            }
            """);

    private Task VerifyItemExistsAsync(string markup, string expectedItem)
    {
        return VerifyItemExistsAsync(markup, expectedItem, isComplexTextEdit: true);
    }
}
