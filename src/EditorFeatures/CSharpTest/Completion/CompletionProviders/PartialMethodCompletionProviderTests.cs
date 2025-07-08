// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    public async Task NoPartialMethods1()
    {
        await VerifyNoItemsExistAsync("""
            class c
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoPartialMethods2()
    {
        await VerifyNoItemsExistAsync("""
            class c
            {
                private void goo() { };

                partial void $$
            }
            """);
    }

    [Fact]
    public async Task NoExtendedPartialMethods2()
    {
        await VerifyNoItemsExistAsync("""
            class c
            {
                public void goo() { };

                public void $$
            }
            """);
    }

    [Fact]
    public async Task PartialMethodInPartialClass()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                partial void $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task ExtendedPartialMethodInPartialClass()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                public partial void $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task PartialMethodInPartialGenericClass()
    {
        await VerifyItemExistsAsync("""
            partial class c<T>
            {
                partial void goo(T bar);

                partial void $$
            }
            """, "goo(T bar)");
    }

    [Fact]
    public async Task ExtendedPartialMethodInPartialGenericClass()
    {
        await VerifyItemExistsAsync("""
            partial class c<T>
            {
                public partial void goo(T bar);

                public partial void $$
            }
            """, "goo(T bar)");
    }

    [Fact]
    public async Task PartialMethodInPartialStruct()
    {
        await VerifyItemExistsAsync("""
            partial struct c
            {
                partial void goo();

                partial void $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task ExtendedPartialMethodInPartialStruct()
    {
        await VerifyItemExistsAsync("""
            partial struct c
            {
                public partial void goo();

                public partial void $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task CompletionOnPartial1()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task CompletionOnExtendedPartial1()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task CompletionOnPartial2()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task CompletionOnExtendedPartial2()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task StaticUnsafePartial()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial static unsafe void goo();

                void static unsafe partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task StaticUnsafeExtendedPartial()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                public partial static unsafe void goo();

                void static unsafe partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task PartialCompletionWithPrivate()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial static unsafe void goo();

                private partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task PartialCompletionWithPublic()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                public partial static unsafe void goo();

                public partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task NotCompletionDespiteValidModifier()
    {
        await VerifyNoItemsExistAsync("""
            partial class c
            {
                partial void goo();

                void partial unsafe $$
            }
            """);
    }

    [Fact]
    public async Task NoExtendedCompletionDespiteValidModifier()
    {
        await VerifyNoItemsExistAsync("""
            partial class c
            {
                public partial void goo();

                void partial unsafe $$
            }
            """);
    }

    [Fact]
    public async Task YesIfPublic()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                public partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task YesIfInternal()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                internal partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task YesIfProtected()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                protected partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task YesIfProtectedInternal()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                protected internal partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task YesIfExtern()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial void goo();

                extern void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task YesIfVirtual()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                virtual partial void goo();

                void partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task YesIfNonVoidReturnType()
    {
        await VerifyItemExistsAsync("""
            partial class c
            {
                partial int goo();

                partial $$
            }
            """, "goo()");
    }

    [Fact]
    public async Task NotInsideInterface()
    {
        await VerifyNoItemsExistAsync("""
            partial interface i
            {
                partial void goo();

                partial $$
            }
            """);
    }

    [WpfFact]
    public async Task CommitInPartialClass()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitInExtendedPartialClass()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitGenericPartialMethod()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitGenericExtendedPartialMethod()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitMethodErasesPrivate()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitMethodKeepsExtendedPrivate()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitInPartialClassPart()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitInExtendedPartialClassPart()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact]
    public async Task CommitInPartialStruct()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [Fact]
    public async Task NotIfNoPartialKeyword()
    {
        await VerifyNoItemsExistAsync("""
            partial class C
                {
                    partial void Goo();
                }

                partial class C
                {
                    void $$
                }
            """);
    }

    [Fact]
    public async Task NotIfNoExtendedPartialKeyword()
    {
        await VerifyNoItemsExistAsync("""
            partial class C
                {
                    public partial void Goo();
                }

                partial class C
                {
                    void $$
                }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578757")]
    public async Task DoNotConsiderFollowingDeclarationPartial()
    {
        await VerifyNoItemsExistAsync("""
            class Program
            {
                partial $$

                void Goo()
                {

                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578757")]
    public async Task DoNotConsiderFollowingDeclarationExtendedPartial()
    {
        await VerifyNoItemsExistAsync("""
            class Program
            {
                public partial $$

                void Goo()
                {

                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
    public async Task CommitAsync()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
    public async Task CommitAsyncExtended()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
    public async Task AmbiguityCommittingWithParen()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965677")]
    public async Task NoDefaultParameterValues()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965677")]
    public async Task NoDefaultParameterValuesExtended()
    {
        await VerifyCustomCommitProviderAsync("""
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
    }

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

    private Task VerifyItemExistsAsync(string markup, string expectedItem)
    {
        return VerifyItemExistsAsync(markup, expectedItem, isComplexTextEdit: true);
    }
}
