// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class OverrideCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public OverrideCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new OverrideCompletionProvider(TestWaitIndicator.Default);
        }

        #region "CompletionItem tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedVirtualPublicMethod()
        {
            VerifyItemExists(@"
public class a
{
    public virtual void foo() { }
}

public class b : a
{
    override $$
}", "foo()");
        }

        [WorkItem(543799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedParameterDefaultValue1()
        {
            VerifyItemExists(@"public class a
{
    public virtual void foo(int x = 42) { }
}

public class b : a
{
    override $$
}", "foo(int x = 42)", "void a.foo([int x = 42])");
        }

        [WorkItem(543799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedParameterDefaultValue2()
        {
            VerifyItemExists(@"public class a
{
    public virtual void foo(int x, int y = 42) { }
}

public class b : a
{
    override $$
}", "foo(int x, int y = 42)", "void a.foo(int x, [int y = 42])");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedAbstractPublicMethod()
        {
            VerifyItemExists(@"
public class a
{
    public abstract void foo();
}

public class b : a
{
    override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotPrivateInheritedMethod()
        {
            VerifyItemIsAbsent(@"
public class a
{
    private virtual void foo() { }
}

public class b : a
{
    override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MatchReturnType()
        {
            var markup = @"
public class a
{
    public virtual void foo() { }

    public virtual string bar() {return null;}
}

public class b : a
{
    override void $$
}";
            VerifyItemIsAbsent(markup, "bar()");
            VerifyItemExists(markup, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvalidReturnType()
        {
            var markup = @"
public class a
{
    public virtual void foo() { }

    public virtual string bar() {return null;}
}

public class b : a
{
    override badtype $$
}";

            VerifyItemExists(markup, "foo()");
            VerifyItemExists(markup, "bar()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAlreadyImplementedMethods()
        {
            VerifyItemIsAbsent(@"
public class a
{
    protected virtual void foo() { }

    protected virtual string bar() {return null;}
}

public class b : a
{
    protected override foo(){ }

    override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotSealed()
        {
            VerifyItemIsAbsent(@"
public class a
{
    protected sealed void foo() { }
}

public class b : a
{
    public override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowEvent()
        {
            VerifyItemExists(@"
using System;
public class a
{
    public virtual event EventHandler foo;
}

public class b : a
{
    public override $$
}", "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfTokensAfterPosition()
        {
            VerifyNoItemsExist(@"
public class a
{
    public virtual void foo() { }
}

public class b : a
{
    public override $$ void
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfNameAfterPosition()
        {
            VerifyNoItemsExist(@"
public class a
{
    public virtual void foo() { }
}

public class b : a
{
    public override void $$ bar
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotIfStatic()
        {
            VerifyNoItemsExist(@"
public class a
{
    public virtual void foo() { }
}

public class b : a
{
    public static override $$ 
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterSingleLineMethodDeclaration()
        {
            VerifyNoItemsExist(@"
public class a
{
    public virtual void foo() { }
}

public class b : a
{
    void bar() { } override $$
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestProperty()
        {
            VerifyItemExists(@"
public class a
{
    public virtual int foo { }
}

public class b : a
{
     override $$
}", "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotSuggestSealed()
        {
            VerifyItemIsAbsent(@"
public class a
{
    public sealed int foo { }
}

public class b : a
{
     override $$
}", "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GatherModifiers()
        {
            VerifyItemExists(@"
public class a
{
    public abstract extern unsafe int foo { }
}

public class b : a
{
     override $$
}", "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IgnorePartial()
        {
            VerifyNoItemsExist(@"
public class a
{
    public virtual partial foo() { }
}

public class b : a
{
     override partial $$
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IgnoreSealed()
        {
            VerifyItemIsAbsent(@"
public class a
{
    public virtual sealed int foo() { }
}

public class b : a
{
     override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IgnoreIfTokenAfter()
        {
            VerifyNoItemsExist(@"
public class a
{
    public virtual int foo() { }
}

public class b : a
{
     override $$ int
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAfterUnsafeAbstractExtern()
        {
            VerifyItemExists(@"
public class a
{
    public virtual int foo() { }
}

public class b : a
{
     unsafe abstract extern override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestAfterSealed()
        {
            VerifyItemExists(@"
public class a
{
    public virtual int foo() { }
}

public class b : a
{
     sealed override $$
}", "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoAccessibility()
        {
            var markup = @"
public class a
{
    public virtual int foo() { }
    protected virtual int bar() { }
}

public class b : a
{
     override $$
}";

            VerifyItemExists(markup, "foo()");
            VerifyItemExists(markup, "bar()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FilterAccessibility()
        {
            var markup = @"
public class a
{
    public virtual int foo() { }
    protected virtual int bar() { }
    internal virtual int far() { }
    private virtual int bor() { }
}

public class b : a
{
     override internal $$
}";

            VerifyItemIsAbsent(markup, "foo()");
            VerifyItemIsAbsent(markup, "bar()");
            VerifyItemIsAbsent(markup, "bor()");

            VerifyItemExists(markup, "far()");

            VerifyItemExists(@"
public class a
{
    public virtual int foo() { }
    protected virtual int bar() { }
    internal virtual int far() { }
    private virtual int bor() { }
}

public class b : a
{
     override protected $$
}", "bar()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FilterPublicInternal()
        {
            var protectedinternal = @"
public class a
{
    protected internal virtual void foo() { }
    public virtual void bar() { }
}

public class b : a
{
     protected internal override $$
}";

            VerifyItemIsAbsent(protectedinternal, "bar()");
            VerifyItemExists(protectedinternal, "foo()");

            var internalprotected = @"
public class a
{
    protected internal virtual void foo() { }
    public virtual void bar() { }
}

public class b : a
{
     internal protected override $$ 
}";

            VerifyItemIsAbsent(internalprotected, "bar()");
            VerifyItemExists(internalprotected, "foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void VerifySignatureFormat()
        {
            var markup = @"
public class a
{
    override $$
}";

            VerifyItemExists(markup, "Equals(object obj)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PrivateNoFilter()
        {
            var markup = @"
public class c
{
    public virtual void foo() { }
}

public class a : c
{
    private override $$
}";

            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotOfferedOnFirstLine()
        {
            var markup = @"class c { override $$";

            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotOfferedOverrideAlone()
        {
            var markup = @"override $$";

            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IntermediateClassOverriddenMember()
        {
            var markup = @"abstract class Base
{
    public abstract void Foo();
}

class Derived : Base
{
    public override void Foo() { }
}

class SomeClass : Derived
{
    override $$
}";

            VerifyItemExists(markup, "Foo()", "void Derived.Foo()");
        }

        [WorkItem(543748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotOfferedBaseClassMember()
        {
            var markup = @"abstract class Base
{
    public abstract void Foo();
}

class Derived : Base
{
    public override void Foo() { }
}

class SomeClass : Derived
{
    override $$
}";

            VerifyItemIsAbsent(markup, "Foo()", "void Base.Foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotOfferedOnNonVirtual()
        {
            var markup = @"class Base
{
    public void Foo();
}

class SomeClass : Base
{
    override $$
}";

            VerifyItemIsAbsent(markup, "Foo()", "void Base.Foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericTypeNameSubstitutedForGenericInDerivedClass1()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Foo(T t);
}

public class SomeClass<X> : Base<X>
{
    override $$
}";
            VerifyItemExists(markup, "Foo(X t)");
            VerifyItemIsAbsent(markup, "Foo(T t)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericTypeNameSubstitutedForGenericInDerivedClass2()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Foo(T t);
}

public class SomeClass<X, Y, Z> : Base<Y>
{
    override $$
}";
            VerifyItemExists(markup, "Foo(Y t)");
            VerifyItemIsAbsent(markup, "Foo(T t)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericTypeNameSubstitutedForGenericInDerivedClass3()
        {
            var markup = @"public abstract class Base<T, S>
{
    public abstract void Foo(T t, S s);
}

public class SomeClass<X, Y, Z> : Base<Y, Z>
{
    override $$
}";
            VerifyItemExists(markup, "Foo(Y t, Z s)");
            VerifyItemIsAbsent(markup, "Foo(T t, S s)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericTypeNameSubstitutedForNonGenericInDerivedClass1()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Foo(T t);
}

public class SomeClass : Base<int>
{
    override $$
}";
            VerifyItemExists(markup, "Foo(int t)");
            VerifyItemIsAbsent(markup, "Foo(T t)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericTypeNameSubstitutedForNonGenericInDerivedClass2()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Foo(T t);
}

public class SomeClass<X, Y, Z> : Base<int>
{
    override $$
}";
            VerifyItemExists(markup, "Foo(int t)");
            VerifyItemIsAbsent(markup, "Foo(T t)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericTypeNameSubstitutedForNonGenericInDerivedClass3()
        {
            var markup = @"using System;

public abstract class Base<T, S>
{
    public abstract void Foo(T t, S s);
}

public class SomeClass : Base<int, Exception>
{
    override $$
}";
            VerifyItemExists(markup, "Foo(int t, Exception s)");
            VerifyItemIsAbsent(markup, "Foo(T t, S s)");
        }

        [WorkItem(543756)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ParameterTypeSimplified()
        {
            var markup = @"using System;

public abstract class Base
{
    public abstract void Foo(System.Exception e);
}

public class SomeClass : Base
{
    override $$
}";

            VerifyItemExists(markup, "Foo(Exception e)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EscapedMethodNameInIntelliSenseList()
        {
            var markup = @"public abstract class Base
{
    public abstract void @class();
}

public class SomeClass : Base
{
    override $$
}";

            string code;
            int position;
            MarkupTestFile.GetPosition(markup, out code, out position);

            BaseVerifyWorker(code, position, "@class()", "void Base.@class()", SourceCodeKind.Regular, false, false, null);
            BaseVerifyWorker(code, position, "@class()", "void Base.@class()", SourceCodeKind.Script, false, false, null);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EscapedPropertyNameInIntelliSenseList()
        {
            var markup = @"public abstract class Base
{
    public virtual int @class { get; set; }
}

public class SomeClass : Base
{
    override $$
}";

            string code;
            int position;
            MarkupTestFile.GetPosition(markup, out code, out position);

            BaseVerifyWorker(code, position, "@class", "int Base.@class { get; set; }", SourceCodeKind.Regular, false, false, null);
            BaseVerifyWorker(code, position, "@class", "int Base.@class { get; set; }", SourceCodeKind.Script, false, false, null);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EscapedParameterNameInIntelliSenseList()
        {
            var markup = @"public abstract class Base
{
    public abstract void foo(int @class);
}

public class SomeClass : Base
{
    override $$
}";

            VerifyItemExists(markup, "foo(int @class)", "void Base.foo(int @class)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void RefParameter()
        {
            var markup = @"public abstract class Base
{
    public abstract void foo(int x, ref string y);
}

public class SomeClass : Base
{
    override $$
}";

            VerifyItemExists(markup, "foo(int x, ref string y)", "void Base.foo(int x, ref string y)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OutParameter()
        {
            var markup = @"public abstract class Base
{
    public abstract void foo(int x, out string y);
}

public class SomeClass : Base
{
    override $$
}";

            VerifyItemExists(markup, "foo(int x, out string y)", "void Base.foo(int x, out string y)");
        }

        [WorkItem(529714)]
        [WpfFact(Skip = "529714"), Trait(Traits.Feature, Traits.Features.Completion)]
        public void GenericMethodTypeParametersRenamed()
        {
            var markup = @"abstract class CFoo
{
    public virtual X Something<X>(X arg)
    {
        return default(X);
    }
}

class Derived<X> : CFoo
{
    override $$
}";

            VerifyItemExists(markup, "Something<X1>(X1 arg)");
            VerifyItemIsAbsent(markup, "Something<X>(X arg)");
        }

        #endregion

        #region "Commit tests"

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInEmptyClass()
        {
            var markupBeforeCommit = @"class c
{
        override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "Equals(object obj)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitMethodBeforeMethod()
        {
            var markupBeforeCommit = @"class c
{
    override $$

    public void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }

    public void foo() { }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "Equals(object obj)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitMethodAfterMethod()
        {
            var markupBeforeCommit = @"class c
{
    public void foo() { }

    override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public void foo() { }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "Equals(object obj)", expectedCodeAfterCommit);
        }

        [WorkItem(543798)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOptionalParameterValuesAreGenerated()
        {
            var markupBeforeCommit = @"using System;

abstract public class Base
{
    public abstract void foo(int x = 42);
}

public class Derived : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

abstract public class Base
{
    public abstract void foo(int x = 42);
}

public class Derived : Base
{
    public override void foo(int x = 42)
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(int x = 42)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAttributesAreNotGenerated()
        {
            var markupBeforeCommit = @"using System;

public class Base
{
    [Obsolete]
    public virtual void foo()
    {
    }
}

public class Derived : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public class Base
{
    [Obsolete]
    public virtual void foo()
    {
    }
}

public class Derived : Base
{
    public override void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitVoidMethod()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void foo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void foo() { }
}

class d : c
{
    public override void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitVoidMethodWithParams()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void foo(int bar, int quux) { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void foo(int bar, int quux) { }
}

class d : c
{
    public override void foo(int bar, int quux)
    {
        base.foo(bar, quux);$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(int bar, int quux)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitNonVoidMethod()
        {
            var markupBeforeCommit = @"class c
{
    public virtual int foo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual int foo() { }
}

class d : c
{
    public override int foo()
    {
        return base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitNonVoidMethodWithParams()
        {
            var markupBeforeCommit = @"class c
{
    public virtual int foo(int bar, int quux) { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual int foo(int bar, int quux) { }
}

class d : c
{
    public override int foo(int bar, int quux)
    {
        return base.foo(bar, quux);$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(int bar, int quux)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitProtectedMethod()
        {
            var markupBeforeCommit = @"class c
{
    protected virtual void foo() { }
}

class d : c
{
   override $$
}";
            var expectedCodeAfterCommit = @"class c
{
    protected virtual void foo() { }
}

class d : c
{
    protected override void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInternalMethod()
        {
            var markupBeforeCommit = @"class c
{
    internal virtual void foo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    internal virtual void foo() { }
}

class d : c
{
    internal override void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitProtectedInternalMethod()
        {
            var markupBeforeCommit = @"public class c
{
    protected internal virtual void foo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    protected internal virtual void foo() { }
}

class d : c
{
    protected internal override void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAbstractMethodThrows()
        {
            var markupBeforeCommit = @"abstract class c
{
    public abstract void foo();
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"using System;

abstract class c
{
    public abstract void foo();
}

class d : c
{
    public override void foo()
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOverrideAsAbstract()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void foo() { };
}

class d : c
{
   abstract override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void foo() { };
}

class d : c
{
    public abstract override void foo();$$
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOverrideAsUnsafeSealed()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void foo() { };
}

class d : c
{
   unsafe sealed override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void foo() { };
}

class d : c
{
    public sealed override unsafe void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInsertProperty()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int foo { get; set; }
}

public class d : c
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int foo { get; set; }
}

public class d : c
{
    public override int foo
    {
        get
        {
            return base.foo;$$
        }

        set
        {
            base.foo = value;
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInsertPropertyAfterMethod()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int foo { get; set; }
}

public class d : c
{
    public void a() { }
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int foo { get; set; }
}

public class d : c
{
    public void a() { }
    public override int foo
    {
        get
        {
            return base.foo;$$
        }

        set
        {
            base.foo = value;
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInsertPropertyBeforeMethod()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int foo { get; set; }
}

public class d : c
{
    override $$
    public void a() { }
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int foo { get; set; }
}

public class d : c
{
    public override int foo
    {
        get
        {
            return base.foo;$$
        }

        set
        {
            base.foo = value;
        }
    }
    public void a() { }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitPropertyInaccessibleGet()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int foo { private get; set; }
}

public class d : c
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int foo { private get; set; }
}

public class d : c
{
    public override int foo
    {
        set
        {
            base.foo = value;$$
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitPropertyInaccessibleSet()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int foo { private set; get; }
}

public class d : c
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int foo { private set; get; }
}

public class d : c
{
    public override int foo
    {
        get
        {
            return base.foo;$$
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAccessibleEvent()
        {
            var markupBeforeCommit = @"using System;
public class a
{
    public virtual event EventHandler foo;
}

public class b : a
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;
public class a
{
    public virtual event EventHandler foo;
}

public class b : a
{
    public override event EventHandler foo;$$
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitEventAfterMethod()
        {
            var markupBeforeCommit = @"using System;

public class a
{
    public virtual event EventHandler foo;
}

public class b : a
{
    void bar() { }
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public class a
{
    public virtual event EventHandler foo;
}

public class b : a
{
    void bar() { }
    public override event EventHandler foo;$$
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitGenericMethod()
        {
            var markupBeforeCommit = @"using System;

public class a
{
    public virtual void foo<T>() { }
}

public class b : a
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public class a
{
    public virtual void foo<T>() { }
}

public class b : a
{
    public override void foo<T>()
    {
        base.foo<T>();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo<T>()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitInsertIndexer()
        {
            var markupBeforeCommit = @"public class MyIndexer<T>
{
    private T[] arr = new T[100];
    public virtual T this[int i]
    {
        get
        {
            return arr[i];
        }
        set
        {
            arr[i] = value;
        }
    }
}

class d : MyIndexer<T>
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class MyIndexer<T>
{
    private T[] arr = new T[100];
    public virtual T this[int i]
    {
        get
        {
            return arr[i];
        }
        set
        {
            arr[i] = value;
        }
    }
}

class d : MyIndexer<T>
{
    public override T this[int i]
    {
        get
        {
            return base[i];$$
        }

        set
        {
            base[i] = value;
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "this[int i]", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAbstractIndexer()
        {
            var markupBeforeCommit = @"public class MyIndexer<T>
{
    private T[] arr = new T[100];
    public abstract T this[int i] { get; set; }
}

class d : MyIndexer<T>
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public class MyIndexer<T>
{
    private T[] arr = new T[100];
    public abstract T this[int i] { get; set; }
}

class d : MyIndexer<T>
{
    public override T this[int i]
    {
        get
        {
            throw new NotImplementedException();$$
        }

        set
        {
            throw new NotImplementedException();
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "this[int i]", expectedCodeAfterCommit);
        }

        // The following two scenarios are already verified through 'VerifyCommit',
        // which also tests everything at the end of the file (truncating input markup at $$)
        // public void CommitInsertAtEndOfFile()
        // public void CommitInsertAtEndOfFileAfterMethod()

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitFormats()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
override $$
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
    public override void foo()
    {
        base.foo();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSimplifiesParameterTypes()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract void foo(System.Exception e);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void foo(System.Exception e);
}

public class SomeClass : Base
{
    public override void foo(Exception e)
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(Exception e)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSimplifiesReturnType()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract System.ArgumentException foo(System.Exception e);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract System.ArgumentException foo(System.Exception e);
}

public class SomeClass : Base
{
    public override ArgumentException foo(Exception e)
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(Exception e)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitEscapedMethodName()
        {
            var markupBeforeCommit = @"public abstract class Base
{
    public abstract void @class();
}

public class SomeClass : Base
{
    override $$
}";
            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void @class();
}

public class SomeClass : Base
{
    public override void @class()
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "@class()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitEscapedPropertyName()
        {
            var markupBeforeCommit = @"public abstract class Base
{
    public virtual int @class { get; set; }
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"public abstract class Base
{
    public virtual int @class { get; set; }
}

public class SomeClass : Base
{
    public override int @class
    {
        get
        {
            return base.@class;$$
        }

        set
        {
            base.@class = value;
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "@class", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitEscapedParameterName()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract void foo(int @class);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void foo(int @class);
}

public class SomeClass : Base
{
    public override void foo(int @class)
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(int @class)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitRefParameter()
        {
            var markupBeforeCommit = @"public abstract class Base
{
    public abstract void foo(int x, ref string y);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void foo(int x, ref string y);
}

public class SomeClass : Base
{
    public override void foo(int x, ref string y)
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(int x, ref string y)", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitOutParameter()
        {
            var markupBeforeCommit = @"public abstract class Base
{
    public abstract void foo(int x, out string y);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void foo(int x, out string y);
}

public class SomeClass : Base
{
    public override void foo(int x, out string y)
    {
        throw new NotImplementedException();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "foo(int x, out string y)", expectedCodeAfterCommit);
        }

        [WorkItem(529714)]
        [WpfFact(Skip = "529714"), Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitGenericMethodTypeParametersRenamed()
        {
            var markupBeforeCommit = @"abstract class CFoo
{
    public virtual X Something<X>(X arg)
    {
        return default(X);
    }
}

class Derived<X> : CFoo
{
    override $$
}";

            var expectedCodeAfterCommit = @"abstract class CFoo
{
    public virtual X Something<X>(X arg)
    {
        return default(X);
    }
}

class Derived<X> : CFoo
{
    public override X1 Something<X1>(X1 arg)
    {
        return base.Something<X1>(arg);
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "Something<X1>(X1 arg)", expectedCodeAfterCommit);
        }

        [WorkItem(544560)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestUnsafe1()
        {
            var markupBeforeCommit =
@"public class A
{
    public unsafe virtual void F()
    {
    }
}

public class B : A
{
    override $$
}";

            var expectedCodeAfterCommit =
@"public class A
{
    public unsafe virtual void F()
    {
    }
}

public class B : A
{
    public override void F()
    {
        base.F();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "F()", expectedCodeAfterCommit);
        }

        [WorkItem(544560)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestUnsafe2()
        {
            var markupBeforeCommit =
@"public class A
{
    public unsafe virtual void F()
    {
    }
}

public class B : A
{
    override unsafe $$
}";

            var expectedCodeAfterCommit =
@"public class A
{
    public unsafe virtual void F()
    {
    }
}

public class B : A
{
    public override unsafe void F()
    {
        base.F();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "F()", expectedCodeAfterCommit);
        }

        [WorkItem(544560)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestUnsafe3()
        {
            var markupBeforeCommit =
@"public class A
{
    public unsafe virtual void F()
    {
    }
}

public class B : A
{
    unsafe override $$
}";

            var expectedCodeAfterCommit =
@"public class A
{
    public unsafe virtual void F()
    {
    }
}

public class B : A
{
    public override unsafe void F()
    {
        base.F();$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "F()", expectedCodeAfterCommit);
        }

        [WorkItem(544560)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestUnsafe4()
        {
            var markupBeforeCommit =
@"public class A
{
    public virtual void F(int* i)
    {
    }
}

public class B : A
{
    override $$
}";

            var expectedCodeAfterCommit =
@"public class A
{
    public virtual void F(int* i)
    {
    }
}

public class B : A
{
    public override unsafe void F(int* i)
    {
        base.F(i);$$
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "F(int* i)", expectedCodeAfterCommit);
        }

        [WorkItem(545534)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestPrivateVirtualProperty()
        {
            var markupBeforeCommit =
@"public class B
{
    public virtual int Foo
    {
        get; private set;
    }

    class C : B
    {
        override $$
    }
}";

            var expectedCodeAfterCommit =
@"public class B
{
    public virtual int Foo
    {
        get; private set;
    }

    class C : B
    {
        public override int Foo
        {
            get
            {
                return base.Foo;$$
            }
        }
    }
}";

            VerifyCustomCommitProvider(markupBeforeCommit, "Foo", expectedCodeAfterCommit);
        }

        [WorkItem(636706)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CrossLanguageParameterizedPropertyOverride()
        {
            var vbFile = @"Public Class Foo
    Public Overridable Property Bar(bay As Integer) As Integer
        Get
            Return 23
        End Get
        Set(value As Integer)
 
        End Set
    End Property
End Class
";
            var csharpFile = @"class Program : Foo
{
    override $$
}
";
            var csharpFileAfterCommit = @"class Program : Foo
{
    public override int get_Bar(int bay)
    {
        return base.get_Bar(bay);$$
    }
    public override void set_Bar(int bay, int value)
    {
        base.set_Bar(bay, value);
    }
}
";
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <ProjectReference>VBProject</ProjectReference>
        <Document FilePath=""CSharpDocument"">{1}</Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""VBProject"">
        <Document FilePath=""VBDocument"">
{3}
        </Document>
    </Project>
    
</Workspace>", LanguageNames.CSharp, csharpFile, LanguageNames.VisualBasic, vbFile);

            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var position = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument").Id;
                var document = solution.GetDocument(documentId);
                var triggerInfo = new CompletionTriggerInfo();

                var completionList = GetCompletionList(document, position, triggerInfo);
                var completionItem = completionList.Items.First(i => CompareItems(i.DisplayText, "Bar[int bay]"));

                var customCommitCompletionProvider = CompletionProvider as ICustomCommitCompletionProvider;
                if (customCommitCompletionProvider != null)
                {
                    var textView = testWorkspace.GetTestDocument(documentId).GetTextView();
                    customCommitCompletionProvider.Commit(completionItem, textView, textView.TextBuffer, textView.TextSnapshot, '\t');
                    string actualCodeAfterCommit = textView.TextBuffer.CurrentSnapshot.AsText().ToString();
                    var caretPosition = textView.Caret.Position.BufferPosition.Position;

                    int expectedCaretPosition;
                    string actualExpectedCode = null;
                    MarkupTestFile.GetPosition(csharpFileAfterCommit, out actualExpectedCode, out expectedCaretPosition);

                    Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
                    Assert.Equal(expectedCaretPosition, caretPosition);
                }
            }
        }

        #endregion

        #region "Commit: With Trivia"

        [WorkItem(529199)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSurroundingTriviaDirective()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
#if true
override $$
#endif
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
#if true
    public override void foo()
    {
        base.foo();$$
    }
#endif
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WorkItem(529199)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitBeforeTriviaDirective()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
override $$
    #if true
    #endif
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
    public override void foo()
    {
        base.foo();$$
    }
#if true
#endif
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAfterTriviaDirective()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
#if true
#endif
override $$
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
#if true
#endif
    public override void foo()
    {
        base.foo();$$
    }
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WorkItem(529199)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitBeforeComment()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
override $$
    /* comment */
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
    public override void foo()
    {
        base.foo();$$
    }
    /* comment */
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitAfterComment()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
    /* comment */
override $$
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void foo() { }
}

class Derived : Base
{
    /* comment */
    public override void foo()
    {
        base.foo();$$
    }
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoNotFormatFile()
        {
            var markupBeforeCommit = @"class Program
{
int zip;
    public virtual void foo()
    {
        
    }
}

class C : Program
{
int bar;
    override $$
}";

            var expectedCodeAfterCommit = @"class Program
{
int zip;
    public virtual void foo()
    {
        
    }
}

class C : Program
{
int bar;
    public override void foo()
    {
        base.foo();$$
    }
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WorkItem(736742)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AcrossPartialTypes1()
        {
            var file1 = @"partial class c
{
}
";
            var file2 = @"partial class c
{
    override $$
}
";
            var csharpFileAfterCommit = @"partial class c
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }
}
";
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""CSharpDocument"">{1}</Document>
        <Document FilePath=""CSharpDocument2"">{2}</Document>
    </Project>
</Workspace>", LanguageNames.CSharp, file1, file2);

            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var position = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument2").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument2").Id;
                var document = solution.GetDocument(documentId);
                var triggerInfo = new CompletionTriggerInfo();

                var completionList = GetCompletionList(document, position, triggerInfo);
                var completionItem = completionList.Items.First(i => CompareItems(i.DisplayText, "Equals(object obj)"));

                var customCommitCompletionProvider = CompletionProvider as ICustomCommitCompletionProvider;
                if (customCommitCompletionProvider != null)
                {
                    var textView = testWorkspace.GetTestDocument(documentId).GetTextView();
                    customCommitCompletionProvider.Commit(completionItem, textView, textView.TextBuffer, textView.TextSnapshot, '\t');
                    string actualCodeAfterCommit = textView.TextBuffer.CurrentSnapshot.AsText().ToString();
                    var caretPosition = textView.Caret.Position.BufferPosition.Position;

                    int expectedCaretPosition;
                    string actualExpectedCode = null;
                    MarkupTestFile.GetPosition(csharpFileAfterCommit, out actualExpectedCode, out expectedCaretPosition);

                    Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
                    Assert.Equal(expectedCaretPosition, caretPosition);
                }
            }
        }

        [WorkItem(736742)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AcrossPartialTypes2()
        {
            var file1 = @"partial class c
{
}
";
            var file2 = @"partial class c
{
    override $$
}
";
            var csharpFileAfterCommit = @"partial class c
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }
}
";
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""CSharpDocument"">{1}</Document>
        <Document FilePath=""CSharpDocument2"">{2}</Document>
    </Project>
</Workspace>", LanguageNames.CSharp, file2, file1);

            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var cursorPosition = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var documentId = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument").Id;
                var document = solution.GetDocument(documentId);
                var triggerInfo = new CompletionTriggerInfo();

                var completionList = GetCompletionList(document, cursorPosition, triggerInfo);
                var completionItem = completionList.Items.First(i => CompareItems(i.DisplayText, "Equals(object obj)"));

                var customCommitCompletionProvider = CompletionProvider as ICustomCommitCompletionProvider;
                if (customCommitCompletionProvider != null)
                {
                    var textView = testWorkspace.GetTestDocument(documentId).GetTextView();
                    customCommitCompletionProvider.Commit(completionItem, textView, textView.TextBuffer, textView.TextSnapshot, '\t');
                    string actualCodeAfterCommit = textView.TextBuffer.CurrentSnapshot.AsText().ToString();
                    var caretPosition = textView.Caret.Position.BufferPosition.Position;

                    int expectedCaretPosition;
                    string actualExpectedCode = null;
                    MarkupTestFile.GetPosition(csharpFileAfterCommit, out actualExpectedCode, out expectedCaretPosition);

                    Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
                    Assert.Equal(expectedCaretPosition, caretPosition);
                }
            }
        }

        #endregion

        #region "EditorBrowsable should be ignored"

        [WpfFact]
        [WorkItem(545678)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_IgnoredWhenOverridingMethods()
        {
            var markup = @"
class D : B
{
    override $$
}";
            var referencedCode = @"
public class B
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public virtual void Foo() {}
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo()",
                expectedSymbolsMetadataReference: 1,
                expectedSymbolsSameSolution: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DuplicateMember()
        {
            var markupBeforeCommit = @"class Program
{
    public virtual void foo() {}
    public virtual void foo() {}
}

class C : Program
{
    override $$
}";

            var expectedCodeAfterCommit = @"class Program
{
    public virtual void foo() {}
    public virtual void foo() {}
}

class C : Program
{
    public override void foo()
    {
        base.foo();$$
    }
}";
            VerifyCustomCommitProvider(markupBeforeCommit, "foo()", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LeaveTrailingTriviaAlone()
        {
            var text = @"
namespace ConsoleApplication46
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        override $$
    }
}";
            var workspace = TestWorkspaceFactory.CreateWorkspaceFromFiles(LanguageNames.CSharp, new CSharpCompilationOptions(OutputKind.ConsoleApplication), new CSharpParseOptions(), text);
            var provider = new OverrideCompletionProvider(TestWaitIndicator.Default);
            var testDocument = workspace.Documents.Single();
            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            var completionList = GetCompletionList(provider, document, testDocument.CursorPosition.Value, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());

            var oldTree = document.GetSyntaxTreeAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            provider.Commit(completionList.Items.First(i => i.DisplayText == "ToString()"), testDocument.GetTextView(), testDocument.GetTextBuffer(), testDocument.TextBuffer.CurrentSnapshot, ' ');
            var newTree = workspace.CurrentSolution.GetDocument(testDocument.Id).GetSyntaxTreeAsync().WaitAndGetResult(CancellationToken.None);
            var changes = newTree.GetChanges(oldTree);

            // If we left the trailing trivia of the close curly of Main alone,
            // there should only be one change: the replacement of "override " with a method.
            Assert.Equal(changes.Single().Span, TextSpan.FromBounds(136, 145));
        }
    }
}
