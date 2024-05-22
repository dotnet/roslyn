// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class OverrideCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(OverrideCompletionProvider);

        internal override OptionsCollection NonCompletionOptions
            => new(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement }
            };

        #region "CompletionItem tests"

        [WpfFact]
        public async Task InheritedVirtualPublicMethod()
        {
            await VerifyItemExistsAsync(@"
public class a
{
    public virtual void goo() { }
}

public class b : a
{
    override $$
}", "goo()");
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543799")]
        public async Task InheritedParameterDefaultValue1()
        {
            await VerifyItemExistsAsync(@"public class a
{
    public virtual void goo(int x = 42) { }
}

public class b : a
{
    override $$
}", "goo(int x = 42)", "void a.goo([int x = 42])");
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543799")]
        public async Task InheritedParameterDefaultValue2()
        {
            await VerifyItemExistsAsync(@"public class a
{
    public virtual void goo(int x, int y = 42) { }
}

public class b : a
{
    override $$
}", "goo(int x, int y = 42)", "void a.goo(int x, [int y = 42])");
        }

        [WpfFact]
        public async Task InheritedAbstractPublicMethod()
        {
            await VerifyItemExistsAsync(@"
public class a
{
    public abstract void goo();
}

public class b : a
{
    override $$
}", "goo()");
        }

        [WpfFact]
        public async Task NotPrivateInheritedMethod()
        {
            await VerifyItemIsAbsentAsync(@"
public class a
{
    private virtual void goo() { }
}

public class b : a
{
    override $$
}", "goo()");
        }

        [WpfFact]
        public async Task MatchReturnType()
        {
            var markup = @"
public class a
{
    public virtual void goo() { }

    public virtual string bar() {return null;}
}

public class b : a
{
    override void $$
}";
            await VerifyItemIsAbsentAsync(markup, "bar()");
            await VerifyItemExistsAsync(markup, "goo()");
        }

        [WpfFact]
        public async Task InvalidReturnType()
        {
            var markup = @"
public class a
{
    public virtual void goo() { }

    public virtual string bar() {return null;}
}

public class b : a
{
    override badtype $$
}";

            await VerifyItemExistsAsync(markup, "goo()");
            await VerifyItemExistsAsync(markup, "bar()");
        }

        [WpfFact]
        public async Task NotAlreadyImplementedMethods()
        {
            await VerifyItemIsAbsentAsync(@"
public class a
{
    protected virtual void goo() { }

    protected virtual string bar() {return null;}
}

public class b : a
{
    protected override void goo() { }

    override $$
}", "goo()");
        }

        [WpfFact]
        public async Task NotSealed()
        {
            await VerifyItemIsAbsentAsync(@"
public class a
{
    protected sealed void goo() { }
}

public class b : a
{
    public override $$
}", "goo()");
        }

        [WpfFact]
        public async Task ShowEvent()
        {
            await VerifyItemExistsAsync(@"
using System;
public class a
{
    public virtual event EventHandler goo;
}

public class b : a
{
    public override $$
}", "goo");
        }

        [WpfFact]
        public async Task NotIfTokensAfterPosition()
        {
            await VerifyNoItemsExistAsync(@"
public class a
{
    public virtual void goo() { }
}

public class b : a
{
    public override $$ void
}");
        }

        [WpfFact]
        public async Task NotIfNameAfterPosition()
        {
            await VerifyNoItemsExistAsync(@"
public class a
{
    public virtual void goo() { }
}

public class b : a
{
    public override void $$ bar
}");
        }

        [WpfFact]
        public async Task NotIfStatic()
        {
            await VerifyNoItemsExistAsync(@"
public class a
{
    public virtual void goo() { }
}

public class b : a
{
    public static override $$
}");
        }

        [WpfFact]
        public async Task AfterSingleLineMethodDeclaration()
        {
            await VerifyNoItemsExistAsync(@"
public class a
{
    public virtual void goo() { }
}

public class b : a
{
    void bar() { } override $$
}");
        }

        [WpfFact]
        public async Task SuggestProperty()
        {
            await VerifyItemExistsAsync(@"
public class a
{
    public virtual int goo { }
}

public class b : a
{
     override $$
}", "goo");
        }

        [WpfFact]
        public async Task NotSuggestSealed()
        {
            await VerifyItemIsAbsentAsync(@"
public class a
{
    public sealed int goo { }
}

public class b : a
{
     override $$
}", "goo");
        }

        [WpfFact]
        public async Task GatherModifiers()
        {
            await VerifyItemExistsAsync(@"
public class a
{
    public abstract extern unsafe int goo { }
}

public class b : a
{
     override $$
}", "goo");
        }

        [WpfFact]
        public async Task IgnorePartial()
        {
            await VerifyNoItemsExistAsync(@"
public class a
{
    public virtual partial goo() { }
}

public class b : a
{
     override partial $$
}");
        }

        [WpfFact]
        public async Task IgnoreSealed()
        {
            await VerifyItemIsAbsentAsync(@"
public class a
{
    public virtual sealed int goo() { }
}

public class b : a
{
     override $$
}", "goo()");
        }

        [WpfFact]
        public async Task IgnoreIfTokenAfter()
        {
            await VerifyNoItemsExistAsync(@"
public class a
{
    public virtual int goo() { }
}

public class b : a
{
     override $$ int
}");
        }

        [WpfFact]
        public async Task SuggestAfterUnsafeAbstractExtern()
        {
            await VerifyItemExistsAsync(@"
public class a
{
    public virtual int goo() { }
}

public class b : a
{
     unsafe abstract extern override $$
}", "goo()");
        }

        [WpfFact]
        public async Task SuggestAfterSealed()
        {
            await VerifyItemExistsAsync(@"
public class a
{
    public virtual int goo() { }
}

public class b : a
{
     sealed override $$
}", "goo()");
        }

        [WpfFact]
        public async Task NoAccessibility()
        {
            var markup = @"
public class a
{
    public virtual int goo() { }
    protected virtual int bar() { }
}

public class b : a
{
     override $$
}";

            await VerifyItemExistsAsync(markup, "goo()");
            await VerifyItemExistsAsync(markup, "bar()");
        }

        [WpfFact]
        public async Task FilterAccessibility()
        {
            var markup = @"
public class a
{
    public virtual int goo() { }
    protected virtual int bar() { }
    internal virtual int far() { }
    private virtual int bor() { }
}

public class b : a
{
     override internal $$
}";

            await VerifyItemIsAbsentAsync(markup, "goo()");
            await VerifyItemIsAbsentAsync(markup, "bar()");
            await VerifyItemIsAbsentAsync(markup, "bor()");

            await VerifyItemExistsAsync(markup, "far()");

            await VerifyItemExistsAsync(@"
public class a
{
    public virtual int goo() { }
    protected virtual int bar() { }
    internal virtual int far() { }
    private virtual int bor() { }
}

public class b : a
{
     override protected $$
}", "bar()");
        }

        [WpfFact]
        public async Task FilterPublicInternal()
        {
            var protectedinternal = @"
public class a
{
    protected internal virtual void goo() { }
    public virtual void bar() { }
}

public class b : a
{
     protected internal override $$
}";

            await VerifyItemIsAbsentAsync(protectedinternal, "bar()");
            await VerifyItemExistsAsync(protectedinternal, "goo()");

            var internalprotected = @"
public class a
{
    protected internal virtual void goo() { }
    public virtual void bar() { }
}

public class b : a
{
    internal protected override $$ 
}";

            await VerifyItemIsAbsentAsync(internalprotected, "bar()");
            await VerifyItemExistsAsync(internalprotected, "goo()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64821")]
        public async Task FilterAccessibility1()
        {
            var test1 = @"
public class a
{
    private protected virtual void goo() { }
}

public class b : a
{
    private override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");

            test1 = @"
public class a
{
    private protected virtual void goo() { }
}

public class b : a
{
    protected override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");

            test1 = @"
public class a
{
    private protected virtual void goo() { }
}

public class b : a
{
    private protected override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");

            test1 = @"
public class a
{
    private protected virtual void goo() { }
}

public class b : a
{
    protected private override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64821")]
        public async Task FilterAccessibility2()
        {
            var test1 = @"
public class a
{
    protected internal virtual void goo() { }
}

public class b : a
{
    protected override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");

            test1 = @"
public class a
{
    protected internal virtual void goo() { }
}

public class b : a
{
    internal override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");

            test1 = @"
public class a
{
    protected internal virtual void goo() { }
}

public class b : a
{
    protected internal override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");

            test1 = @"
public class a
{
    protected internal virtual void goo() { }
}

public class b : a
{
    internal protected override $$
}";

            await VerifyItemExistsAsync(test1, "goo()");
        }

        [WpfFact]
        public async Task VerifySignatureFormat()
        {
            var markup = @"
public class a
{
    override $$
}";

            await VerifyItemExistsAsync(markup, "Equals(object obj)");
        }

        [WpfFact]
        public async Task PrivateNoFilter()
        {
            var markup = @"
public class c
{
    public virtual void goo() { }
}

public class a : c
{
    private override $$
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [WpfFact]
        public async Task NotOfferedOnFirstLine()
        {
            var markup = @"class c { override $$";

            await VerifyNoItemsExistAsync(markup);
        }

        [WpfFact]
        public async Task NotOfferedOverrideAlone()
        {
            var markup = @"override $$";

            await VerifyNoItemsExistAsync(markup);
        }

        [WpfFact]
        public async Task IntermediateClassOverriddenMember()
        {
            var markup = @"abstract class Base
{
    public abstract void Goo();
}

class Derived : Base
{
    public override void Goo() { }
}

class SomeClass : Derived
{
    override $$
}";

            await VerifyItemExistsAsync(markup, "Goo()", "void Derived.Goo()");
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543748")]
        public async Task NotOfferedBaseClassMember()
        {
            var markup = @"abstract class Base
{
    public abstract void Goo();
}

class Derived : Base
{
    public override void Goo() { }
}

class SomeClass : Derived
{
    override $$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo()", "void Base.Goo()");
        }

        [WpfFact]
        public async Task NotOfferedOnNonVirtual()
        {
            var markup = @"class Base
{
    public void Goo();
}

class SomeClass : Base
{
    override $$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo()", "void Base.Goo()");
        }

        [WpfFact]
        public async Task GenericTypeNameSubstitutedForGenericInDerivedClass1()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Goo(T t);
}

public class SomeClass<X> : Base<X>
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(X t)");
            await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
        }

        [WpfFact]
        public async Task GenericTypeNameSubstitutedForGenericInDerivedClass2()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Goo(T t);
}

public class SomeClass<X, Y, Z> : Base<Y>
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(Y t)");
            await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
        }

        [WpfFact]
        public async Task GenericTypeNameSubstitutedForGenericInDerivedClass3()
        {
            var markup = @"public abstract class Base<T, S>
{
    public abstract void Goo(T t, S s);
}

public class SomeClass<X, Y, Z> : Base<Y, Z>
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(Y t, Z s)");
            await VerifyItemIsAbsentAsync(markup, "Goo(T t, S s)");
        }

        [WpfFact]
        public async Task GenericTypeNameSubstitutedForNonGenericInDerivedClass1()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Goo(T t);
}

public class SomeClass : Base<int>
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(int t)");
            await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
        }

        [WpfFact]
        public async Task GenericTypeNameSubstitutedForNonGenericInDerivedClass2()
        {
            var markup = @"public abstract class Base<T>
{
    public abstract void Goo(T t);
}

public class SomeClass<X, Y, Z> : Base<int>
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(int t)");
            await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
        }

        [WpfFact]
        public async Task GenericTypeNameSubstitutedForNonGenericInDerivedClass3()
        {
            var markup = @"using System;

public abstract class Base<T, S>
{
    public abstract void Goo(T t, S s);
}

public class SomeClass : Base<int, Exception>
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(int t, Exception s)");
            await VerifyItemIsAbsentAsync(markup, "Goo(T t, S s)");
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543756")]
        public async Task ParameterTypeSimplified()
        {
            var markup = @"using System;

public abstract class Base
{
    public abstract void Goo(System.Exception e);
}

public class SomeClass : Base
{
    override $$
}";

            await VerifyItemExistsAsync(markup, "Goo(Exception e)");
        }

        [WpfFact]
        public async Task NullableAnnotationsIncluded()
        {
            var markup = @"#nullable enable

public abstract class Base
{
    public abstract void Goo(string? s);
}

public class SomeClass : Base
{
    override $$
}";
            await VerifyItemExistsAsync(markup, "Goo(string? s)");
        }

        [WpfFact]
        public async Task EscapedMethodNameInIntelliSenseList()
        {
            var markup = @"public abstract class Base
{
    public abstract void @class();
}

public class SomeClass : Base
{
    override $$
}";
            MarkupTestFile.GetPosition(markup, out var code, out int position);

            await BaseVerifyWorkerAsync(code, position, "@class()", "void Base.@class()", SourceCodeKind.Regular, false, false, null, null, null, null, null, null);
            await BaseVerifyWorkerAsync(code, position, "@class()", "void Base.@class()", SourceCodeKind.Script, false, false, null, null, null, null, null, null);
        }

        [WpfFact]
        public async Task EscapedPropertyNameInIntelliSenseList()
        {
            var markup = @"public abstract class Base
{
    public virtual int @class { get; set; }
}

public class SomeClass : Base
{
    override $$
}";
            MarkupTestFile.GetPosition(markup, out var code, out int position);

            await BaseVerifyWorkerAsync(code, position, "@class", "int Base.@class { get; set; }", SourceCodeKind.Regular, false, false, null, null, null, null, null, null);
            await BaseVerifyWorkerAsync(code, position, "@class", "int Base.@class { get; set; }", SourceCodeKind.Script, false, false, null, null, null, null, null, null);
        }

        [WpfFact]
        public async Task EscapedParameterNameInIntelliSenseList()
        {
            var markup = @"public abstract class Base
{
    public abstract void goo(int @class);
}

public class SomeClass : Base
{
    override $$
}";

            await VerifyItemExistsAsync(markup, "goo(int @class)", "void Base.goo(int @class)");
        }

        [WpfFact]
        public async Task RefParameter()
        {
            var markup = @"public abstract class Base
{
    public abstract void goo(int x, ref string y);
}

public class SomeClass : Base
{
    override $$
}";

            await VerifyItemExistsAsync(markup, "goo(int x, ref string y)", "void Base.goo(int x, ref string y)");
        }

        [WpfFact]
        public async Task OutParameter()
        {
            var markup = @"public abstract class Base
{
    public abstract void goo(int x, out string y);
}

public class SomeClass : Base
{
    override $$
}";

            await VerifyItemExistsAsync(markup, "goo(int x, out string y)", "void Base.goo(int x, out string y)");
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")]
        public async Task GenericMethodTypeParametersNotRenamed()
        {
            var markup = @"abstract class CGoo    
{    
   public virtual X Something<X>(X arg)    
   {    
       return default(X);    
    }    
}    
class Derived<X> : CGoo    
{    
    override $$    
}";
            await VerifyItemExistsAsync(markup, "Something<X>(X arg)");
        }
        #endregion

        #region "Commit tests"

        [WpfFact]
        public async Task CommitInEmptyClass()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Equals(object obj)", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")]
        public async Task CommitGenericMethodTypeParametersNotRenamed()
        {
            var markupBeforeCommit = @"abstract class CGoo    
{    
    public virtual X Something<X>(X arg)    
    {    
        return default(X);    
    }    
}    
class Derived<X> : CGoo    
{    
    override $$    
}";

            var expectedCodeAfterCommit = @"abstract class CGoo    
{    
    public virtual X Something<X>(X arg)    
    {    
        return default(X);    
    }    
}    
class Derived<X> : CGoo    
{
    public override X Something<X>(X arg)
    {
        return base.Something(arg);$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Something<X>(X arg)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitMethodBeforeMethod()
        {
            var markupBeforeCommit = @"class c
{
    override $$

    public void goo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }

    public void goo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Equals(object obj)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitMethodAfterMethod()
        {
            var markupBeforeCommit = @"class c
{
    public void goo() { }

    override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public void goo() { }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Equals(object obj)", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543798")]
        public async Task CommitOptionalParameterValuesAreGenerated()
        {
            var markupBeforeCommit = @"using System;

abstract public class Base
{
    public abstract void goo(int x = 42);
}

public class Derived : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

abstract public class Base
{
    public abstract void goo(int x = 42);
}

public class Derived : Base
{
    public override void goo(int x = 42)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(int x = 42)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitAttributesAreNotGenerated()
        {
            var markupBeforeCommit = @"using System;

public class Base
{
    [Obsolete]
    public virtual void goo()
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
    public virtual void goo()
    {
    }
}

public class Derived : Base
{
    public override void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInaccessibleParameterAttributesAreNotGenerated()
        {
            var markupBeforeCommit = @"using System;

public class Class1
{
    private class MyPrivate : Attribute { }
    public class MyPublic : Attribute { }
    public virtual void M([MyPrivate, MyPublic] int i) { }
}

public class Class2 : Class1
{
    public override void $$
}";

            var expectedCodeAfterCommit = @"using System;

public class Class1
{
    private class MyPrivate : Attribute { }
    public class MyPublic : Attribute { }
    public virtual void M([MyPrivate, MyPublic] int i) { }
}

public class Class2 : Class1
{
    public override void M([MyPublic] int i)
    {
        base.M(i);$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "M(int i)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitVoidMethod()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void goo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void goo() { }
}

class d : c
{
    public override void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitVoidMethodWithParams()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void goo(int bar, int quux) { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void goo(int bar, int quux) { }
}

class d : c
{
    public override void goo(int bar, int quux)
    {
        base.goo(bar, quux);$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(int bar, int quux)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitNonVoidMethod()
        {
            var markupBeforeCommit = @"class c
{
    public virtual int goo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual int goo() { }
}

class d : c
{
    public override int goo()
    {
        return base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitNonVoidMethodWithParams()
        {
            var markupBeforeCommit = @"class c
{
    public virtual int goo(int bar, int quux) { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual int goo(int bar, int quux) { }
}

class d : c
{
    public override int goo(int bar, int quux)
    {
        return base.goo(bar, quux);$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(int bar, int quux)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitProtectedMethod()
        {
            var markupBeforeCommit = @"class c
{
    protected virtual void goo() { }
}

class d : c
{
   override $$
}";
            var expectedCodeAfterCommit = @"class c
{
    protected virtual void goo() { }
}

class d : c
{
    protected override void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInternalMethod()
        {
            var markupBeforeCommit = @"class c
{
    internal virtual void goo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    internal virtual void goo() { }
}

class d : c
{
    internal override void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitProtectedInternalMethod()
        {
            var markupBeforeCommit = @"public class c
{
    protected internal virtual void goo() { }
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    protected internal virtual void goo() { }
}

class d : c
{
    protected internal override void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitAbstractMethodThrows()
        {
            var markupBeforeCommit = @"using System;

abstract class c
{
    public abstract void goo();
}

class d : c
{
   override $$
}";

            var expectedCodeAfterCommit = @"using System;

abstract class c
{
    public abstract void goo();
}

class d : c
{
    public override void goo()
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitOverrideAsAbstract()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void goo() { };
}

class d : c
{
   abstract override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void goo() { };
}

class d : c
{
    public abstract override void goo();$$
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitOverrideAsUnsafeSealed()
        {
            var markupBeforeCommit = @"class c
{
    public virtual void goo() { };
}

class d : c
{
   unsafe sealed override $$
}";

            var expectedCodeAfterCommit = @"class c
{
    public virtual void goo() { };
}

class d : c
{
    public sealed override unsafe void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInsertProperty()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int goo { get; set; }
}

public class d : c
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int goo { get; set; }
}

public class d : c
{
    public override int goo
    {
        get
        {
            return base.goo;$$
        }

        set
        {
            base.goo = value;
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInsertPropertyAfterMethod()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int goo { get; set; }
}

public class d : c
{
    public void a() { }
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int goo { get; set; }
}

public class d : c
{
    public void a() { }
    public override int goo
    {
        get
        {
            return base.goo;$$
        }

        set
        {
            base.goo = value;
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInsertPropertyBeforeMethod()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int goo { get; set; }
}

public class d : c
{
    override $$
    public void a() { }
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int goo { get; set; }
}

public class d : c
{
    public override int goo
    {
        get
        {
            return base.goo;$$
        }

        set
        {
            base.goo = value;
        }
    }
    public void a() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitPropertyInaccessibleGet()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int goo { private get; set; }
}

public class d : c
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int goo { private get; set; }
}

public class d : c
{
    public override int goo
    {
        set
        {
            base.goo = value;$$
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitPropertyInaccessibleSet()
        {
            var markupBeforeCommit = @"public class c
{
    public virtual int goo { private set; get; }
}

public class d : c
{
    override $$
}";

            var expectedCodeAfterCommit = @"public class c
{
    public virtual int goo { private set; get; }
}

public class d : c
{
    public override int goo
    {
        get
        {
            return base.goo;$$
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInsertPropertyInaccessibleParameterAttributesAreNotGenerated()
        {
            var markupBeforeCommit = @"using System;

namespace ClassLibrary1
{
    public class Class1
    {
        private class MyPrivate : Attribute { }

        public class MyPublic : Attribute { }

        public virtual int this[[MyPrivate, MyPublic]int i]
        {
            get { return 0; }
            set { }
        }
    }

    public class Class2 : Class1
    {
        public override int $$
    }
}";

            var expectedCodeAfterCommit = @"using System;

namespace ClassLibrary1
{
    public class Class1
    {
        private class MyPrivate : Attribute { }

        public class MyPublic : Attribute { }

        public virtual int this[[MyPrivate, MyPublic]int i]
        {
            get { return 0; }
            set { }
        }
    }

    public class Class2 : Class1
    {
        public override int this[[MyPublic] int i]
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
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "this[int i]", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitAccessibleEvent()
        {
            var markupBeforeCommit = @"using System;
public class a
{
    public virtual event EventHandler goo;
}

public class b : a
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;
public class a
{
    public virtual event EventHandler goo;
}

public class b : a
{
    public override event EventHandler goo;$$
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitEventAfterMethod()
        {
            var markupBeforeCommit = @"using System;

public class a
{
    public virtual event EventHandler goo;
}

public class b : a
{
    void bar() { }
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public class a
{
    public virtual event EventHandler goo;
}

public class b : a
{
    void bar() { }
    public override event EventHandler goo;$$
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitGenericMethod()
        {
            var markupBeforeCommit = @"using System;

public class a
{
    public virtual void goo<T>() { }
}

public class b : a
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public class a
{
    public virtual void goo<T>() { }
}

public class b : a
{
    public override void goo<T>()
    {
        base.goo<T>();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo<T>()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitMethodWithNullableAttributes()
        {
            var markupBeforeCommit = @"
#nullable enable

class C
{
    public virtual string? Goo(string? s) { }
}

class D : C
{
    override $$
}";

            var expectedCodeAfterCommit = @"
#nullable enable

class C
{
    public virtual string? Goo(string? s) { }
}

class D : C
{
    public override string? Goo(string? s)
    {
        return base.Goo(s);$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Goo(string? s)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitMethodInNullableDisableContext()
        {
            var markupBeforeCommit = @"
#nullable enable

class C
{
    public virtual string? Goo(string? s) { }
}

#nullable disable

class D : C
{
    override $$
}";

            var expectedCodeAfterCommit = @"
#nullable enable

class C
{
    public virtual string? Goo(string? s) { }
}

#nullable disable

class D : C
{
    public override string Goo(string s)
    {
        return base.Goo(s);$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Goo(string? s)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitToStringIsExplicitlyNonNullReturning()
        {
            var markupBeforeCommit = @"
#nullable enable

namespace System
{
    public class Object
    {
        public virtual string? ToString() { }
    }
}

class D : System.Object
{
    override $$
}";

            var expectedCodeAfterCommit = @"
#nullable enable

namespace System
{
    public class Object
    {
        public virtual string? ToString() { }
    }
}

class D : System.Object
{
    public override string ToString()
    {
        return base.ToString();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "ToString()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitInsertIndexer()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "this[int i]", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitAbstractIndexer()
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

            var expectedCodeAfterCommit = @"public class MyIndexer<T>
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
            throw new System.NotImplementedException();$$
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "this[int i]", expectedCodeAfterCommit);
        }

        // The following two scenarios are already verified through 'VerifyCommit',
        // which also tests everything at the end of the file (truncating input markup at $$)
        // public void CommitInsertAtEndOfFile()
        // public void CommitInsertAtEndOfFileAfterMethod()

        [WpfFact]
        public async Task CommitFormats()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
override $$
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
    public override void goo()
    {
        base.goo();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitSimplifiesParameterTypes()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract void goo(System.Exception e);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void goo(System.Exception e);
}

public class SomeClass : Base
{
    public override void goo(Exception e)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(Exception e)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitSimplifiesReturnType()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract System.ArgumentException goo(System.Exception e);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract System.ArgumentException goo(System.Exception e);
}

public class SomeClass : Base
{
    public override ArgumentException goo(Exception e)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(Exception e)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitEscapedMethodName()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "@class()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitEscapedPropertyName()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "@class", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitEscapedParameterName()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract void goo(int @class);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void goo(int @class);
}

public class SomeClass : Base
{
    public override void goo(int @class)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(int @class)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitRefParameter()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract void goo(int x, ref string y);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void goo(int x, ref string y);
}

public class SomeClass : Base
{
    public override void goo(int x, ref string y)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(int x, ref string y)", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitOutParameter()
        {
            var markupBeforeCommit = @"using System;

public abstract class Base
{
    public abstract void goo(int x, out string y);
}

public class SomeClass : Base
{
    override $$
}";

            var expectedCodeAfterCommit = @"using System;

public abstract class Base
{
    public abstract void goo(int x, out string y);
}

public class SomeClass : Base
{
    public override void goo(int x, out string y)
    {
        throw new NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo(int x, out string y)", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
        public async Task TestUnsafe1()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "F()", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
        public async Task TestUnsafe2()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "F()", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
        public async Task TestUnsafe3()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "F()", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
        public async Task TestUnsafe4()
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

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "F(int* i)", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545534")]
        public async Task TestPrivateVirtualProperty()
        {
            var markupBeforeCommit =
@"public class B
{
    public virtual int Goo
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
    public virtual int Goo
    {
        get; private set;
    }

    class C : B
    {
        public override int Goo
        {
            get
            {
                return base.Goo;$$
            }
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Goo", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636706")]
        public async Task CrossLanguageParameterizedPropertyOverride()
        {
            var vbFile = @"Public Class Goo
    Public Overridable Property Bar(bay As Integer) As Integer
        Get
            Return 23
        End Get
        Set(value As Integer)
 
        End Set
    End Property
End Class
";
            var csharpFile = @"class Program : Goo
{
    override $$
}
";
            var csharpFileAfterCommit = @"class Program : Goo
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

            using var testWorkspace = EditorTestWorkspace.Create(xmlString, composition: GetComposition());
            var testDocument = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument");

            Contract.ThrowIfNull(testDocument.CursorPosition);
            var position = testDocument.CursorPosition.Value;
            var solution = testWorkspace.CurrentSolution;
            var documentId = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument").Id;
            var document = solution.GetRequiredDocument(documentId);
            var triggerInfo = CompletionTrigger.Invoke;

            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, position, triggerInfo);
            var completionItem = completionList.ItemsList.First(i => CompareItems(i.DisplayText, "Bar[int bay]"));

            if (service.GetProvider(completionItem, document.Project) is ICustomCommitCompletionProvider customCommitCompletionProvider)
            {
                var textView = testDocument.GetTextView();
                customCommitCompletionProvider.Commit(completionItem, document, textView, textView.TextBuffer, textView.TextSnapshot, '\t');
                var actualCodeAfterCommit = textView.TextBuffer.CurrentSnapshot.AsText().ToString();
                var caretPosition = textView.Caret.Position.BufferPosition.Position;
                MarkupTestFile.GetPosition(csharpFileAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

                Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
                Assert.Equal(expectedCaretPosition, caretPosition);
            }
        }

        #endregion

        #region "Commit: With Trivia"

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529199")]
        public async Task CommitSurroundingTriviaDirective()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
#if true
override $$
#endif
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
#if true
    public override void goo()
    {
        base.goo();$$
    }
#endif
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529199")]
        public async Task CommitBeforeTriviaDirective()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
override $$
    #if true
    #endif
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
    public override void goo()
    {
        base.goo();$$
    }
#if true
#endif
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitAfterTriviaDirective()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
#if true
#endif
override $$
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
#if true
#endif
    public override void goo()
    {
        base.goo();$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529199")]
        public async Task CommitBeforeComment()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
override $$
    /* comment */
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
    public override void goo()
    {
        base.goo();$$
    }
    /* comment */
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitAfterComment()
        {
            var markupBeforeCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
    /* comment */
override $$
}";

            var expectedCodeAfterCommit = @"class Base
{
    public virtual void goo() { }
}

class Derived : Base
{
    /* comment */
    public override void goo()
    {
        base.goo();$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task DoNotFormatFile()
        {
            var markupBeforeCommit = @"class Program
{
int zip;
    public virtual void goo()
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
    public virtual void goo()
    {
        
    }
}

class C : Program
{
int bar;
    public override void goo()
    {
        base.goo();$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736742")]
        public async Task AcrossPartialTypes1()
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

            using var testWorkspace = EditorTestWorkspace.Create(xmlString, composition: GetComposition());
            var testDocument = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument2");

            Contract.ThrowIfNull(testDocument.CursorPosition);
            var position = testDocument.CursorPosition.Value;
            var solution = testWorkspace.CurrentSolution;
            var documentId = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument2").Id;
            var document = solution.GetRequiredDocument(documentId);
            var triggerInfo = CompletionTrigger.Invoke;

            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, position, triggerInfo);
            var completionItem = completionList.ItemsList.First(i => CompareItems(i.DisplayText, "Equals(object obj)"));

            if (service.GetProvider(completionItem, document.Project) is ICustomCommitCompletionProvider customCommitCompletionProvider)
            {
                var textView = testDocument.GetTextView();
                customCommitCompletionProvider.Commit(completionItem, document, textView, textView.TextBuffer, textView.TextSnapshot, '\t');
                var actualCodeAfterCommit = textView.TextBuffer.CurrentSnapshot.AsText().ToString();
                var caretPosition = textView.Caret.Position.BufferPosition.Position;
                MarkupTestFile.GetPosition(csharpFileAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

                Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
                Assert.Equal(expectedCaretPosition, caretPosition);
            }
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736742")]
        public async Task AcrossPartialTypes2()
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

            using var testWorkspace = EditorTestWorkspace.Create(xmlString, composition: GetComposition());
            var testDocument = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument");

            Contract.ThrowIfNull(testDocument.CursorPosition);
            var cursorPosition = testDocument.CursorPosition.Value;
            var solution = testWorkspace.CurrentSolution;
            var documentId = testWorkspace.Documents.Single(d => d.Name == "CSharpDocument").Id;
            var document = solution.GetRequiredDocument(documentId);
            var triggerInfo = CompletionTrigger.Invoke;

            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, cursorPosition, triggerInfo);
            var completionItem = completionList.ItemsList.First(i => CompareItems(i.DisplayText, "Equals(object obj)"));

            if (service.GetProvider(completionItem, document.Project) is ICustomCommitCompletionProvider customCommitCompletionProvider)
            {
                var textView = testDocument.GetTextView();
                customCommitCompletionProvider.Commit(completionItem, document, textView, textView.TextBuffer, textView.TextSnapshot, '\t');
                var actualCodeAfterCommit = textView.TextBuffer.CurrentSnapshot.AsText().ToString();
                var caretPosition = textView.Caret.Position.BufferPosition.Position;
                MarkupTestFile.GetPosition(csharpFileAfterCommit, out var actualExpectedCode, out int expectedCaretPosition);

                Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
                Assert.Equal(expectedCaretPosition, caretPosition);
            }
        }

        [WpfFact]
        public async Task CommitRequiredKeywordAdded()
        {
            var markupBeforeCommit = """
                class Base
                {
                    public virtual required int Prop { get; }
                }

                class Derived : Base
                {
                    override $$
                }
                """;

            var expectedCodeAfterCommit = """
                class Base
                {
                    public virtual required int Prop { get; }
                }

                class Derived : Base
                {
                    public override required int Prop
                    {
                        get
                        {
                            return base.Prop;$$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Prop", expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("required override")]
        [InlineData("override required")]
        public async Task CommitRequiredKeywordPreserved(string ordering)
        {
            var markupBeforeCommit = $@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" LanguageVersion=""{TestOptions.Regular11.LanguageVersion.ToDisplayString()}"">
        <Document>class Base
{{
    public virtual required int Prop {{ get; }}
}}

class Derived : Base
{{
    {ordering} $$
}}</Document>
    </Project>
</Workspace>";

            var expectedCodeAfterCommit = """
                class Base
                {
                    public virtual required int Prop { get; }
                }

                class Derived : Base
                {
                    public override required int Prop
                    {
                        get
                        {
                            return base.Prop;$$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Prop", expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("required override")]
        [InlineData("override required")]
        public async Task CommitRequiredKeywordPreservedWhenBaseIsNotRequired(string ordering)
        {
            var markupBeforeCommit = $@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" LanguageVersion=""{TestOptions.Regular11.LanguageVersion.ToDisplayString()}"">
        <Document>class Base
{{
    public virtual int Prop {{ get; }}
}}

class Derived : Base
{{
    {ordering} $$
}}</Document>
    </Project>
</Workspace>";

            var expectedCodeAfterCommit = """
                class Base
                {
                    public virtual int Prop { get; }
                }

                class Derived : Base
                {
                    public override required int Prop
                    {
                        get
                        {
                            return base.Prop;$$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Prop", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitRequiredKeywordRemovedForMethods()
        {
            var markupBeforeCommit = """
                class Base
                {
                    public virtual void M() { }
                }

                class Derived : Base
                {
                    required override $$
                }
                """;

            var expectedCodeAfterCommit = """
                class Base
                {
                    public virtual void M() { }
                }

                class Derived : Base
                {
                    public override void M()
                    {
                        base.M();$$
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "M()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task CommitRequiredKeywordRemovedForIndexers()
        {
            var markupBeforeCommit = """
                class Base
                {
                    public virtual int this[int i] { get { } set { } }
                }

                class Derived : Base
                {
                    required override $$
                }
                """;

            var expectedCodeAfterCommit = """
                class Base
                {
                    public virtual int this[int i] { get { } set { } }
                }

                class Derived : Base
                {
                    public override int this[int i]
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
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "this[int i]", expectedCodeAfterCommit);
        }

        #endregion

        #region "EditorBrowsable should be ignored"

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
        public async Task EditorBrowsable_IgnoredWhenOverridingMethods()
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
    public virtual void Goo() {}
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo()",
                expectedSymbolsMetadataReference: 1,
                expectedSymbolsSameSolution: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        #endregion

        [WpfFact]
        public async Task DuplicateMember()
        {
            var markupBeforeCommit = @"class Program
{
    public virtual void goo() {}
    public virtual void goo() {}
}

class C : Program
{
    override $$
}";

            var expectedCodeAfterCommit = @"class Program
{
    public virtual void goo() {}
    public virtual void goo() {}
}

class C : Program
{
    public override void goo()
    {
        base.goo();$$
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task LeaveTrailingTriviaAlone()
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
            using var workspace = EditorTestWorkspace.Create(LanguageNames.CSharp, new CSharpCompilationOptions(OutputKind.ConsoleApplication), new CSharpParseOptions(), new[] { text }, composition: GetComposition());
            var provider = new OverrideCompletionProvider();
            var testDocument = workspace.Documents.Single();
            var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);

            var service = GetCompletionService(document.Project);
            Contract.ThrowIfNull(testDocument.CursorPosition);
            var completionList = await GetCompletionListAsync(service, document, testDocument.CursorPosition.Value, CompletionTrigger.Invoke);

            var oldTree = await document.GetSyntaxTreeAsync();

            var commit = await provider.GetChangeAsync(document, completionList.ItemsList.First(i => i.DisplayText == "ToString()"), ' ');
            var change = commit.TextChange;

            // If we left the trailing trivia of the close curly of Main alone,
            // there should only be one change: the replacement of "override " with a method.
            Assert.Equal(change.Span, TextSpan.FromBounds(136, 145));
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/8257")]
        public async Task NotImplementedQualifiedWhenSystemUsingNotPresent_Property()
        {
            var markupBeforeCommit = @"abstract class C
{
    public abstract int goo { get; set; };
}

class Program : C
{
    override $$
}";

            var expectedCodeAfterCommit = @"abstract class C
{
    public abstract int goo { get; set; };
}

class Program : C
{
    public override int goo
    {
        get
        {
            throw new System.NotImplementedException();$$
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/8257")]
        public async Task NotImplementedQualifiedWhenSystemUsingNotPresent_Method()
        {
            var markupBeforeCommit = @"abstract class C
{
    public abstract void goo();
}

class Program : C
{
    override $$
}";

            var expectedCodeAfterCommit = @"abstract class C
{
    public abstract void goo();
}

class Program : C
{
    public override void goo()
    {
        throw new System.NotImplementedException();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "goo()", expectedCodeAfterCommit);
        }

        [Fact]
        public async Task FilterOutMethodsWithNonRoundTrippableSymbolKeys()
        {
            var text = XElement.Parse(@"<Workspace>
    <Project Name=""P1"" Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C : ClassLibrary7.Class1
{
    override $$
}
]]>
        </Document>
    </Project>
    <Project Name=""P2"" Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document>
namespace ClassLibrary2
{
    public class Missing {}
}
        </Document>
    </Project>
    <Project Name=""P3"" Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3.dll"">
        <ProjectReference>P2</ProjectReference>
        <Document>
namespace ClassLibrary7
{
    public class Class1
    {
        public virtual void Goo(ClassLibrary2.Missing m) {}
        public virtual void Bar() {}
    }
}
        </Document>
    </Project>
</Workspace>");

            // P3 has a project ref to Project P2 and uses the type "Missing" from P2
            // as the return type of a virtual method.
            // P1 has a metadata reference to P3 and therefore doesn't get the transitive
            // reference to P2. If we try to override Goo, the missing "Missing" type will
            // prevent round tripping the symbolkey.
            using var workspace = EditorTestWorkspace.Create(text, composition: GetComposition());
            var compilation = await workspace.CurrentSolution.Projects.First(p => p.Name == "P3").GetCompilationAsync();

            // CompilationExtensions is in the Microsoft.CodeAnalysis.Test.Utilities namespace 
            // which has a "Traits" type that conflicts with the one in Roslyn.Test.Utilities
            var reference = MetadataReference.CreateFromImage(compilation.EmitToArray());
            var p1 = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            var updatedP1 = p1.AddMetadataReference(reference);
            await workspace.ChangeSolutionAsync(updatedP1.Solution);

            var provider = new OverrideCompletionProvider();
            var testDocument = workspace.Documents.First(d => d.Name == "CurrentDocument.cs");
            var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);

            var service = GetCompletionService(document.Project);

            Contract.ThrowIfNull(testDocument.CursorPosition);
            var completionList = await GetCompletionListAsync(service, document, testDocument.CursorPosition.Value, CompletionTrigger.Invoke);

            Assert.True(completionList.ItemsList.Any(c => c.DisplayText == "Bar()"));
            Assert.False(completionList.ItemsList.Any(c => c.DisplayText == "Goo()"));
        }

        [WpfFact]
        public async Task TestInParameter()
        {
            var source = XElement.Parse(@"<Workspace>
    <Project Name=""P1"" Language=""C#"" LanguageVersion=""Latest"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class SomeClass : Base
{
    override $$
}
]]>
        </Document>
    </Project>
</Workspace>");

            using var workspace = EditorTestWorkspace.Create(source, composition: GetComposition());
            var before = @"
public abstract class Base
{
    public abstract void M(in int x);
}";

            var after = @"
public class SomeClass : Base
{
    public override void M(in int x)
    {
        throw new System.NotImplementedException();
    }
}
";

            var origComp = await workspace.CurrentSolution.Projects.Single().GetRequiredCompilationAsync(CancellationToken.None);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            var libComp = origComp.RemoveAllSyntaxTrees().AddSyntaxTrees(CSharpSyntaxTree.ParseText(before, options: options));
            var libRef = MetadataReference.CreateFromImage(libComp.EmitToArray());

            var project = workspace.CurrentSolution.Projects.Single();
            var updatedProject = project.AddMetadataReference(libRef);
            await workspace.ChangeSolutionAsync(updatedProject.Solution);

            var provider = new OverrideCompletionProvider();
            var testDocument = workspace.Documents.First(d => d.Name == "CurrentDocument.cs");
            var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);

            var service = GetCompletionService(document.Project);

            Contract.ThrowIfNull(testDocument.CursorPosition);
            var completionList = await GetCompletionListAsync(service, document, testDocument.CursorPosition.Value, CompletionTrigger.Invoke);
            var completionItem = completionList.ItemsList.Where(c => c.DisplayText == "M(in int x)").Single();

            var commit = await service.GetChangeAsync(document, completionItem, commitCharacter: null, CancellationToken.None);

            var text = await document.GetTextAsync();
            var newText = text.WithChanges(commit.TextChange);
            var newDoc = document.WithText(newText);
            document.Project.Solution.Workspace.TryApplyChanges(newDoc.Project.Solution);

            var textBuffer = workspace.Documents.Single().GetTextBuffer();
            var actualCodeAfterCommit = textBuffer.CurrentSnapshot.AsText().ToString();

            Assert.Equal(after, actualCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39909")]
        public async Task CommitAddsMissingImports()
        {
            var markupBeforeCommit = @"
namespace NS1
{
    using NS2;

    public class Goo
    {
        public virtual bool Bar(Baz baz) => true;
    }
}

namespace NS2
{
    public class Baz {}
}

namespace NS3
{
    using NS1;

    class D : Goo
    {
        override $$
    }
}";

            var expectedCodeAfterCommit = @"
namespace NS1
{
    using NS2;

    public class Goo
    {
        public virtual bool Bar(Baz baz) => true;
    }
}

namespace NS2
{
    public class Baz {}
}

namespace NS3
{
    using NS1;
    using NS2;

    class D : Goo
    {
        public override bool Bar(Baz baz)
        {
            return base.Bar(baz);$$
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Bar(NS2.Baz baz)", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47941")]
        public async Task OverrideInRecordWithoutExplicitOverriddenMember()
        {
            await VerifyItemExistsAsync(@"record Program
{
    override $$
}", "ToString()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47941")]
        public async Task OverrideInRecordWithExplicitOverriddenMember()
        {
            await VerifyItemIsAbsentAsync(@"record Program
{
    public override string ToString() => "";

    override $$
}", "ToString()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47973")]
        public async Task NoCloneInOverriddenRecord()
        {
            // Currently WellKnownMemberNames.CloneMethodName is not public, so we can't reference it directly.  We
            // could hardcode in the value "<Clone>$", however if the compiler ever changed the name and we somehow
            // started showing it in completion, this test would continue to pass.  So this allows us to at least go
            // back and explicitly validate this scenario even in that event.
            var cloneMemberName = (string)typeof(WellKnownMemberNames).GetField("CloneMethodName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.Equal("<Clone>$", cloneMemberName);

            await VerifyItemIsAbsentAsync(@"
record Base();

record Program : Base
{
    override $$
}", cloneMemberName);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48640")]
        public async Task ObjectEqualsInClass()
        {
            await VerifyItemExistsAsync(@"
class Program 
{
    override $$
}", "Equals(object obj)");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48640")]
        public async Task NoObjectEqualsInOverriddenRecord1()
        {
            await VerifyItemIsAbsentAsync(@"
record Program 
{
    override $$
}", "Equals(object obj)");

            await VerifyItemExistsAsync(@"
record Program 
{
    override $$
}", "ToString()");

        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48640")]
        public async Task NoObjectEqualsInOverriddenRecord()
        {
            await VerifyItemIsAbsentAsync(@"
record Base();

record Program : Base
{
    override $$
}", "Equals(object obj)");

            await VerifyItemExistsAsync(@"
record Base();

record Program : Base
{
    override $$
}", "ToString()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
        public async Task WithAttribute1()
        {
            await VerifyItemExistsAsync("""
                abstract class C
                {
                    public abstract void M();
                }

                class D : C
                {
                    [SomeAttribute]
                    override $$;
                }
                """, "M()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
        public async Task WithAttribute2()
        {
            await VerifyItemExistsAsync("""
                abstract class C
                {
                    public abstract void M();
                }

                class D : C
                {
                    [SomeAttribute]
                    [SomeOtherAttribute]
                    override $$;
                }
                """, "M()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
        public async Task NotWhenMultilineModifiers()
        {
            await VerifyItemIsAbsentAsync("""
                abstract class C
                {
                    public abstract void M();
                }

                class D : C
                {
                    public
                    override $$;
                }
                """, "M()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
        public async Task NotWhenMultilineModifiersAndAttribute()
        {
            await VerifyItemIsAbsentAsync("""
                abstract class C
                {
                    public abstract void M();
                }

                class D : C
                {
                    [SomeAttribute]
                    public
                    override $$;
                }
                """, "M()");
        }

        [WpfFact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6308")]
        public async Task NoOverrideItemsWhenNotInTypeDeclaration()
        {
            await VerifyNoItemsExistAsync("""
                namespace NS
                {
                    override $$
                }
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6308")]
        public async Task NoOverrideItemsAtTopLevel()
        {
            await VerifyNoItemsExistAsync("""
                System.Console.WriteLine();
                
                override $$
                """);
        }

        private Task VerifyItemExistsAsync(string markup, string expectedItem)
        {
            return VerifyItemExistsAsync(markup, expectedItem, isComplexTextEdit: true);
        }
    }
}
