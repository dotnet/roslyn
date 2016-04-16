// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class PartialCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public PartialCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new PartialCompletionProvider(TestWaitIndicator.Default);
        }

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
    private void foo() { };

    partial void $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethodInPartialClass()
        {
            var text = @"partial class c
{
    partial void foo();

    partial void $$
}";
            await VerifyItemExistsAsync(text, "foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethodInPartialGenericClass()
        {
            var text = @"partial class c<T>
{
    partial void foo(T bar);

    partial void $$
}";
            await VerifyItemExistsAsync(text, "foo(T bar)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialMethodInPartialStruct()
        {
            var text = @"partial struct c
{
    partial void foo();

    partial void $$
}";
            await VerifyItemExistsAsync(text, "foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionOnPartial1()
        {
            var text = @"partial class c
{
    partial void foo();

    partial $$
}";
            await VerifyItemExistsAsync(text, "foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionOnPartial2()
        {
            var text = @"partial class c
{
    partial void foo();

    void partial $$
}";
            await VerifyItemExistsAsync(text, "foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticUnsafePartial()
        {
            var text = @"partial class c
{
    partial static unsafe void foo();

    void static unsafe partial $$
}";
            await VerifyItemExistsAsync(text, "foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialCompletionWithPrivate()
        {
            var text = @"partial class c
{
    partial static unsafe void foo();

    private partial $$
}";
            await VerifyItemExistsAsync(text, "foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotCompletionDespiteValidModifier()
        {
            var text = @"partial class c
{
    partial void foo();

    void partial unsafe $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfPublic()
        {
            var text = @"partial class c
{
    public partial void foo();

    void partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfInternal()
        {
            var text = @"partial class c
{
    internal partial void foo();

    void partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfProtected()
        {
            var text = @"partial class c
{
    protected partial void foo();

    void partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfProtectedInternal()
        {
            var text = @"partial class c
{
    protected internal partial void foo();

    void partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfExtern()
        {
            var text = @"partial class c
{
    partial void foo();

    extern void partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfVirtual()
        {
            var text = @"partial class c
{
    virtual partial void foo();

    void partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfNonVoidReturnType()
        {
            var text = @"partial class c
{
    partial int foo();

    partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInsideInterface()
        {
            var text = @"partial interface i
{
    partial void foo();

    partial $$
}";
            await VerifyNoItemsExistAsync(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInPartialClass()
        {
            var markupBeforeCommit = @"partial class c
{
    partial void foo();

    partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class c
{
    partial void foo();

    partial void foo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitGenericPartialMethod()
        {
            var markupBeforeCommit = @"partial class c<T>
{
    partial void foo(T bar);

    partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class c<T>
{
    partial void foo(T bar);

    partial void foo(T bar)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "foo(T bar)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitMethodErasesPrivate()
        {
            var markupBeforeCommit = @"partial class c
{
    partial void foo();

    private partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class c
{
    partial void foo();

    partial void foo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInPartialClassPart()
        {
            var markupBeforeCommit = @"partial class c
{
    partial void foo();
}

partial class c
{
    partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class c
{
    partial void foo();
}

partial class c
{
    partial void foo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitInPartialStruct()
        {
            var markupBeforeCommit = @"partial struct c
{
    partial void foo();

    partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial struct c
{
    partial void foo();

    partial void foo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotIfNoPartialKeyword()
        {
            var text = @"partial class C
    {
        partial void Foo();
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
 
    void Foo()
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
    partial void Foo();

    async partial $$
}";

            var expectedCodeAfterCommit = @"using System;

partial class Bar
{
    partial void Foo();

    async partial void Foo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Foo()", expectedCodeAfterCommit);
        }

        [WorkItem(578078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578078")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AmbiguityCommittingWithParen()
        {
            var markupBeforeCommit = @"using System;

partial class Bar
{
    partial void Foo();

    partial Foo$$
}";

            var expectedCodeAfterCommit = @"using System;

partial class Bar
{
    partial void Foo();

    partial void Foo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Foo()", expectedCodeAfterCommit, commitChar: '(');
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

            var expected = @"using System;

namespace PartialClass
{
    partial class PClass
    {
        partial void PMethod(int i = 0);

        partial void PMethod(int i)
        {
            throw new NotImplementedException();$$
        }
    }
}
";
            await VerifyCustomCommitProviderAsync(text, "PMethod(int i)", expected);
        }
    }
}
