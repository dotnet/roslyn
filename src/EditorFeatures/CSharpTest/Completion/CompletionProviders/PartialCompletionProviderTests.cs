// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoPartialMethods1()
        {
            var text = @"class c
{
    $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoPartialMethods2()
        {
            var text = @"class c
{
    private void foo() { };

    partial void $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PartialMethodInPartialClass()
        {
            var text = @"partial class c
{
    partial void foo();

    partial void $$
}";
            VerifyItemExists(text, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PartialMethodInPartialGenericClass()
        {
            var text = @"partial class c<T>
{
    partial void foo(T bar);

    partial void $$
}";
            VerifyItemExists(text, "foo(T bar)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PartialMethodInPartialStruct()
        {
            var text = @"partial struct c
{
    partial void foo();

    partial void $$
}";
            VerifyItemExists(text, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionOnPartial1()
        {
            var text = @"partial class c
{
    partial void foo();

    partial $$
}";
            VerifyItemExists(text, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionOnPartial2()
        {
            var text = @"partial class c
{
    partial void foo();

    void partial $$
}";
            VerifyItemExists(text, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticUnsafePartial()
        {
            var text = @"partial class c
{
    partial static unsafe void foo();

    void static unsafe partial $$
}";
            VerifyItemExists(text, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PartialCompletionWithPrivate()
        {
            var text = @"partial class c
{
    partial static unsafe void foo();

    private partial $$
}";
            VerifyItemExists(text, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotCompletionDespiteValidModifier()
        {
            var text = @"partial class c
{
    partial void foo();

    void partial unsafe $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfPublic()
        {
            var text = @"partial class c
{
    public partial void foo();

    void partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfInternal()
        {
            var text = @"partial class c
{
    internal partial void foo();

    void partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfProtected()
        {
            var text = @"partial class c
{
    protected partial void foo();

    void partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfProtectedInternal()
        {
            var text = @"partial class c
{
    protected internal partial void foo();

    void partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfExtern()
        {
            var text = @"partial class c
{
    partial void foo();

    extern void partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfVirtual()
        {
            var text = @"partial class c
{
    virtual partial void foo();

    void partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfNonVoidReturnType()
        {
            var text = @"partial class c
{
    partial int foo();

    partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotInsideInterface()
        {
            var text = @"partial interface i
{
    partial void foo();

    partial $$
}";
            VerifyNoItemsExist(text);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInPartialClass()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitGenericPartialMethod()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(T bar)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitMethodErasesPrivate()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInPartialClassPart()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInPartialStruct()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfNoPartialKeyword()
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
            VerifyNoItemsExist(text);
        }

        [WorkItem(578757)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoNotConsiderFollowingDeclarationPartial()
        {
            var text = @"class Program
{
    partial $$
 
    void Foo()
    {
        
    }
}
";
            VerifyNoItemsExist(text);
        }

        [WorkItem(578078)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAsync()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "Foo()", expectedCodeAfterCommit);
        }

        [WorkItem(578078)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AmbiguityCommittingWithParen()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "Foo()", expectedCodeAfterCommit, commitChar: '(');
        }

        [WorkItem(965677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoDefaultParameterValues()
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
            VerifyCustomCommitProvider(text, "PMethod(int i)", expected);
        }
    }
}
