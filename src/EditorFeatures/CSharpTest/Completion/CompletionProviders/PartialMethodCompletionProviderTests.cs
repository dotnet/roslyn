﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class PartialMethodCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(PartialMethodCompletionProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoPartialMethods1()
        {
            var text = @"class c
{
    $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoPartialMethods2()
        {
            var text = @"class c
{
    private void goo() { };

    partial void $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoExtendedPartialMethods2()
        {
            var text = @"class c
{
    public void goo() { };

    public void $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethodInPartialClass()
        {
            var text = @"partial class c
{
    partial void goo();

    partial void $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtendedPartialMethodInPartialClass()
        {
            var text = @"partial class c
{
    public partial void goo();

    public partial void $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethodInPartialGenericClass()
        {
            var text = @"partial class c<T>
{
    partial void goo(T bar);

    partial void $$
}";
            await VerifyItemExistsAsync(text, "goo(T bar)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtendedPartialMethodInPartialGenericClass()
        {
            var text = @"partial class c<T>
{
    public partial void goo(T bar);

    public partial void $$
}";
            await VerifyItemExistsAsync(text, "goo(T bar)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethodInPartialStruct()
        {
            var text = @"partial struct c
{
    partial void goo();

    partial void $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtendedPartialMethodInPartialStruct()
        {
            var text = @"partial struct c
{
    public partial void goo();

    public partial void $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionOnPartial1()
        {
            var text = @"partial class c
{
    partial void goo();

    partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionOnExtendedPartial1()
        {
            var text = @"partial class c
{
    public partial void goo();

    partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionOnPartial2()
        {
            var text = @"partial class c
{
    partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionOnExtendedPartial2()
        {
            var text = @"partial class c
{
    public partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticUnsafePartial()
        {
            var text = @"partial class c
{
    partial static unsafe void goo();

    void static unsafe partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticUnsafeExtendedPartial()
        {
            var text = @"partial class c
{
    public partial static unsafe void goo();

    void static unsafe partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialCompletionWithPrivate()
        {
            var text = @"partial class c
{
    partial static unsafe void goo();

    private partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialCompletionWithPublic()
        {
            var text = @"partial class c
{
    public partial static unsafe void goo();

    public partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotCompletionDespiteValidModifier()
        {
            var text = @"partial class c
{
    partial void goo();

    void partial unsafe $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoExtendedCompletionDespiteValidModifier()
        {
            var text = @"partial class c
{
    public partial void goo();

    void partial unsafe $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfPublic()
        {
            var text = @"partial class c
{
    public partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfInternal()
        {
            var text = @"partial class c
{
    internal partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfProtected()
        {
            var text = @"partial class c
{
    protected partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfProtectedInternal()
        {
            var text = @"partial class c
{
    protected internal partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfExtern()
        {
            var text = @"partial class c
{
    partial void goo();

    extern void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfVirtual()
        {
            var text = @"partial class c
{
    virtual partial void goo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YesIfNonVoidReturnType()
        {
            var text = @"partial class c
{
    partial int goo();

    partial $$
}";
            await VerifyItemExistsAsync(text, "goo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInsideInterface()
        {
            var text = @"partial interface i
{
    partial void goo();

    partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInPartialClass()
        {
            var markupBeforeCommit = @"partial class c
{
    partial void goo();

    partial $$
}";

            var expectedCodeAfterCommit = @"partial class c
{
    partial void goo();

    partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInExtendedPartialClass()
        {
            var markupBeforeCommit = @"partial class c
{
    public partial void goo();

    partial $$
}";

            var expectedCodeAfterCommit = @"partial class c
{
    public partial void goo();

    public partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitGenericPartialMethod()
        {
            var markupBeforeCommit = @"partial class c<T>
{
    partial void goo(T bar);

    partial $$
}";

            var expectedCodeAfterCommit = @"partial class c<T>
{
    partial void goo(T bar);

    partial void goo(T bar)
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(T bar)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitGenericExtendedPartialMethod()
        {
            var markupBeforeCommit = @"partial class c<T>
{
    public partial void goo(T bar);

    partial $$
}";

            var expectedCodeAfterCommit = @"partial class c<T>
{
    public partial void goo(T bar);

    public partial void goo(T bar)
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(T bar)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitMethodErasesPrivate()
        {
            var markupBeforeCommit = @"partial class c
{
    partial void goo();

    private partial $$
}";

            var expectedCodeAfterCommit = @"partial class c
{
    partial void goo();

    partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitMethodKeepsExtendedPrivate()
        {
            var markupBeforeCommit = @"partial class c
{
    private partial void goo();

    private partial $$
}";

            var expectedCodeAfterCommit = @"partial class c
{
    private partial void goo();

    private partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInPartialClassPart()
        {
            var markupBeforeCommit = @"partial class c
{
    partial void goo();
}

partial class c
{
    partial $$
}";

            var expectedCodeAfterCommit = @"partial class c
{
    partial void goo();
}

partial class c
{
    partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInExtendedPartialClassPart()
        {
            var markupBeforeCommit = @"partial class c
{
    public partial void goo();
}

partial class c
{
    partial $$
}";

            var expectedCodeAfterCommit = @"partial class c
{
    public partial void goo();
}

partial class c
{
    public partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInPartialStruct()
        {
            var markupBeforeCommit = @"partial struct c
{
    partial void goo();

    partial $$
}";

            var expectedCodeAfterCommit = @"partial struct c
{
    partial void goo();

    partial void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfNoPartialKeyword()
        {
            var text = @"partial class C
    {
        partial void Goo();
    }
 
    partial class C
    {
        void $$
    }
";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfNoExtendedPartialKeyword()
        {
            var text = @"partial class C
    {
        public partial void Goo();
    }
 
    partial class C
    {
        void $$
    }
";
            await VerifyNoItemsExistAsync(text);
        }

        [WorkItem(578757, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578757")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotConsiderFollowingDeclarationPartial()
        {
            var text = @"class Program
{
    partial $$
 
    void Goo()
    {
        
    }
}
";
            await VerifyNoItemsExistAsync(text);
        }

        [WorkItem(578757, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578757")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotConsiderFollowingDeclarationExtendedPartial()
        {
            var text = @"class Program
{
    public partial $$
 
    void Goo()
    {
        
    }
}
";
            await VerifyNoItemsExistAsync(text);
        }

        [WorkItem(578078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitAsync()
        {
            var markupBeforeCommit = @"using System;

partial class Bar
{
    partial void Goo();

    async partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class Bar
{
    partial void Goo();

    async partial void Goo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Goo()", expectedCodeAfterCommit);
        }

        [WorkItem(578078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitAsyncExtended()
        {
            var markupBeforeCommit = @"using System;

partial class Bar
{
    public partial void Goo();

    async partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class Bar
{
    public partial void Goo();

    public async partial void Goo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Goo()", expectedCodeAfterCommit);
        }

        [WorkItem(578078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AmbiguityCommittingWithParen()
        {
            var markupBeforeCommit = @"using System;

partial class Bar
{
    partial void Goo();

    partial Goo$$
}";

            var expectedCodeAfterCommit = @"using System;

partial class Bar
{
    partial void Goo();

    partial void Goo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Goo()", expectedCodeAfterCommit, commitChar: '(');
        }

        [WorkItem(965677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965677")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoDefaultParameterValues()
        {
            var text = @"namespace PartialClass
{
    partial class PClass
    {
        partial void PMethod(int i = 0);

        partial $$
    }
}
";

            var expected = @"namespace PartialClass
{
    partial class PClass
    {
        partial void PMethod(int i = 0);

        partial void PMethod(int i)
        {
            throw new System.NotImplementedException();$$
        }
    }
}
";
            await VerifyCustomCommitProviderAsync(text, "PMethod(int i)", expected);
        }

        [WorkItem(965677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965677")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoDefaultParameterValuesExtended()
        {
            var text = @"namespace PartialClass
{
    partial class PClass
    {
        public partial void PMethod(int i = 0);

        partial $$
    }
}
";

            var expected = @"namespace PartialClass
{
    partial class PClass
    {
        public partial void PMethod(int i = 0);

        public partial void PMethod(int i)
        {
            throw new System.NotImplementedException();$$
        }
    }
}
";
            await VerifyCustomCommitProviderAsync(text, "PMethod(int i)", expected);
        }

        [WorkItem(26388, "https://github.com/dotnet/roslyn/issues/26388")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExpressionBodyMethod()
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(ExportProvider);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options.WithChangedOption(
                CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent))));

            var text = @"using System;
partial class Bar
{
    partial void Foo();
    partial $$
}
"
;

            var expected = @"using System;
partial class Bar
{
    partial void Foo();
    partial void Foo() => throw new NotImplementedException();$$
}
"
;

            await VerifyCustomCommitProviderAsync(text, "Foo()", expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExpressionBodyMethodExtended()
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(ExportProvider);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options.WithChangedOption(
                CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent))));

            var text = @"using System;
partial class Bar
{
    public partial void Foo();
    partial $$
}
"
;

            var expected = @"using System;
partial class Bar
{
    public partial void Foo();
    public partial void Foo() => throw new NotImplementedException();$$
}
"
;

            await VerifyCustomCommitProviderAsync(text, "Foo()", expected);
        }
    }
}
