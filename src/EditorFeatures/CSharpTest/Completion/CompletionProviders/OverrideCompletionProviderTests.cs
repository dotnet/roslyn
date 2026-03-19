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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class OverrideCompletionProviderTests : AbstractCSharpCompletionProviderTests
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
    public Task InheritedVirtualPublicMethod()
        => VerifyItemExistsAsync("""
            public class a
            {
                public virtual void goo() { }
            }

            public class b : a
            {
                override $$
            }
            """, "goo()");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543799")]
    public Task InheritedParameterDefaultValue1()
        => VerifyItemExistsAsync("""
            public class a
            {
                public virtual void goo(int x = 42) { }
            }

            public class b : a
            {
                override $$
            }
            """, "goo(int x = 42)", "void a.goo([int x = 42])");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543799")]
    public Task InheritedParameterDefaultValue2()
        => VerifyItemExistsAsync("""
            public class a
            {
                public virtual void goo(int x, int y = 42) { }
            }

            public class b : a
            {
                override $$
            }
            """, "goo(int x, int y = 42)", "void a.goo(int x, [int y = 42])");

    [WpfFact]
    public Task InheritedAbstractPublicMethod()
        => VerifyItemExistsAsync("""
            public class a
            {
                public abstract void goo();
            }

            public class b : a
            {
                override $$
            }
            """, "goo()");

    [WpfFact]
    public Task NotPrivateInheritedMethod()
        => VerifyItemIsAbsentAsync("""
            public class a
            {
                private virtual void goo() { }
            }

            public class b : a
            {
                override $$
            }
            """, "goo()");

    [WpfFact]
    public async Task MatchReturnType()
    {
        var markup = """
            public class a
            {
                public virtual void goo() { }

                public virtual string bar() {return null;}
            }

            public class b : a
            {
                override void $$
            }
            """;
        await VerifyItemIsAbsentAsync(markup, "bar()");
        await VerifyItemExistsAsync(markup, "goo()");
    }

    [WpfFact]
    public async Task InvalidReturnType()
    {
        var markup = """
            public class a
            {
                public virtual void goo() { }

                public virtual string bar() {return null;}
            }

            public class b : a
            {
                override badtype $$
            }
            """;

        await VerifyItemExistsAsync(markup, "goo()");
        await VerifyItemExistsAsync(markup, "bar()");
    }

    [WpfFact]
    public Task NotAlreadyImplementedMethods()
        => VerifyItemIsAbsentAsync("""
            public class a
            {
                protected virtual void goo() { }

                protected virtual string bar() {return null;}
            }

            public class b : a
            {
                protected override void goo() { }

                override $$
            }
            """, "goo()");

    [WpfFact]
    public Task NotSealed()
        => VerifyItemIsAbsentAsync("""
            public class a
            {
                protected sealed void goo() { }
            }

            public class b : a
            {
                public override $$
            }
            """, "goo()");

    [WpfFact]
    public Task ShowEvent()
        => VerifyItemExistsAsync("""
            using System;
            public class a
            {
                public virtual event EventHandler goo;
            }

            public class b : a
            {
                public override $$
            }
            """, "goo");

    [WpfFact]
    public Task NotIfTokensAfterPosition()
        => VerifyNoItemsExistAsync("""
            public class a
            {
                public virtual void goo() { }
            }

            public class b : a
            {
                public override $$ void
            }
            """);

    [WpfFact]
    public Task NotIfNameAfterPosition()
        => VerifyNoItemsExistAsync("""
            public class a
            {
                public virtual void goo() { }
            }

            public class b : a
            {
                public override void $$ bar
            }
            """);

    [WpfFact]
    public Task NotIfStatic()
        => VerifyNoItemsExistAsync("""
            public class a
            {
                public virtual void goo() { }
            }

            public class b : a
            {
                public static override $$
            }
            """);

    [WpfFact]
    public Task AfterSingleLineMethodDeclaration()
        => VerifyNoItemsExistAsync("""
            public class a
            {
                public virtual void goo() { }
            }

            public class b : a
            {
                void bar() { } override $$
            }
            """);

    [WpfFact]
    public Task SuggestProperty()
        => VerifyItemExistsAsync("""
            public class a
            {
                public virtual int goo { }
            }

            public class b : a
            {
                 override $$
            }
            """, "goo");

    [WpfFact]
    public Task NotSuggestSealed()
        => VerifyItemIsAbsentAsync("""
            public class a
            {
                public sealed int goo { }
            }

            public class b : a
            {
                 override $$
            }
            """, "goo");

    [WpfFact]
    public Task GatherModifiers()
        => VerifyItemExistsAsync("""
            public class a
            {
                public abstract extern unsafe int goo { }
            }

            public class b : a
            {
                 override $$
            }
            """, "goo");

    [WpfFact]
    public Task IgnorePartial()
        => VerifyNoItemsExistAsync("""
            public class a
            {
                public virtual partial goo() { }
            }

            public class b : a
            {
                 override partial $$
            }
            """);

    [WpfFact]
    public Task IgnoreSealed()
        => VerifyItemIsAbsentAsync("""
            public class a
            {
                public virtual sealed int goo() { }
            }

            public class b : a
            {
                 override $$
            }
            """, "goo()");

    [WpfFact]
    public Task IgnoreIfTokenAfter()
        => VerifyNoItemsExistAsync("""
            public class a
            {
                public virtual int goo() { }
            }

            public class b : a
            {
                 override $$ int
            }
            """);

    [WpfFact]
    public Task SuggestAfterUnsafeAbstractExtern()
        => VerifyItemExistsAsync("""
            public class a
            {
                public virtual int goo() { }
            }

            public class b : a
            {
                 unsafe abstract extern override $$
            }
            """, "goo()");

    [WpfFact]
    public Task SuggestAfterSealed()
        => VerifyItemExistsAsync("""
            public class a
            {
                public virtual int goo() { }
            }

            public class b : a
            {
                 sealed override $$
            }
            """, "goo()");

    [WpfFact]
    public async Task NoAccessibility()
    {
        var markup = """
            public class a
            {
                public virtual int goo() { }
                protected virtual int bar() { }
            }

            public class b : a
            {
                 override $$
            }
            """;

        await VerifyItemExistsAsync(markup, "goo()");
        await VerifyItemExistsAsync(markup, "bar()");
    }

    [WpfFact]
    public async Task FilterAccessibility()
    {
        var markup = """
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
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "goo()");
        await VerifyItemIsAbsentAsync(markup, "bar()");
        await VerifyItemIsAbsentAsync(markup, "bor()");

        await VerifyItemExistsAsync(markup, "far()");

        await VerifyItemExistsAsync("""
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
            }
            """, "bar()");
    }

    [WpfFact]
    public async Task FilterPublicInternal()
    {
        var protectedinternal = """
            public class a
            {
                protected internal virtual void goo() { }
                public virtual void bar() { }
            }

            public class b : a
            {
                 protected internal override $$
            }
            """;

        await VerifyItemIsAbsentAsync(protectedinternal, "bar()");
        await VerifyItemExistsAsync(protectedinternal, "goo()");

        var internalprotected = """
            public class a
            {
                protected internal virtual void goo() { }
                public virtual void bar() { }
            }

            public class b : a
            {
                internal protected override $$ 
            }
            """;

        await VerifyItemIsAbsentAsync(internalprotected, "bar()");
        await VerifyItemExistsAsync(internalprotected, "goo()");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64821")]
    public async Task FilterAccessibility1()
    {
        var test1 = """
            public class a
            {
                private protected virtual void goo() { }
            }

            public class b : a
            {
                private override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");

        test1 = """
            public class a
            {
                private protected virtual void goo() { }
            }

            public class b : a
            {
                protected override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");

        test1 = """
            public class a
            {
                private protected virtual void goo() { }
            }

            public class b : a
            {
                private protected override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");

        test1 = """
            public class a
            {
                private protected virtual void goo() { }
            }

            public class b : a
            {
                protected private override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64821")]
    public async Task FilterAccessibility2()
    {
        var test1 = """
            public class a
            {
                protected internal virtual void goo() { }
            }

            public class b : a
            {
                protected override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");

        test1 = """
            public class a
            {
                protected internal virtual void goo() { }
            }

            public class b : a
            {
                internal override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");

        test1 = """
            public class a
            {
                protected internal virtual void goo() { }
            }

            public class b : a
            {
                protected internal override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");

        test1 = """
            public class a
            {
                protected internal virtual void goo() { }
            }

            public class b : a
            {
                internal protected override $$
            }
            """;

        await VerifyItemExistsAsync(test1, "goo()");
    }

    [WpfFact]
    public Task VerifySignatureFormat()
        => VerifyItemExistsAsync("""
            public class a
            {
                override $$
            }
            """, "Equals(object obj)");

    [WpfFact]
    public Task PrivateNoFilter()
        => VerifyNoItemsExistAsync("""
            public class c
            {
                public virtual void goo() { }
            }

            public class a : c
            {
                private override $$
            }
            """);

    [WpfFact]
    public Task NotOfferedOnFirstLine()
        => VerifyNoItemsExistAsync(@"class c { override $$");

    [WpfFact]
    public Task NotOfferedOverrideAlone()
        => VerifyNoItemsExistAsync(@"override $$");

    [WpfFact]
    public Task IntermediateClassOverriddenMember()
        => VerifyItemExistsAsync("""
            abstract class Base
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
            }
            """, "Goo()", "void Derived.Goo()");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543748")]
    public Task NotOfferedBaseClassMember()
        => VerifyItemIsAbsentAsync("""
            abstract class Base
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
            }
            """, "Goo()", "void Base.Goo()");

    [WpfFact]
    public Task NotOfferedOnNonVirtual()
        => VerifyItemIsAbsentAsync("""
            class Base
            {
                public void Goo();
            }

            class SomeClass : Base
            {
                override $$
            }
            """, "Goo()", "void Base.Goo()");

    [WpfFact]
    public async Task GenericTypeNameSubstitutedForGenericInDerivedClass1()
    {
        var markup = """
            public abstract class Base<T>
            {
                public abstract void Goo(T t);
            }

            public class SomeClass<X> : Base<X>
            {
                override $$
            }
            """;
        await VerifyItemExistsAsync(markup, "Goo(X t)");
        await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
    }

    [WpfFact]
    public async Task GenericTypeNameSubstitutedForGenericInDerivedClass2()
    {
        var markup = """
            public abstract class Base<T>
            {
                public abstract void Goo(T t);
            }

            public class SomeClass<X, Y, Z> : Base<Y>
            {
                override $$
            }
            """;
        await VerifyItemExistsAsync(markup, "Goo(Y t)");
        await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
    }

    [WpfFact]
    public async Task GenericTypeNameSubstitutedForGenericInDerivedClass3()
    {
        var markup = """
            public abstract class Base<T, S>
            {
                public abstract void Goo(T t, S s);
            }

            public class SomeClass<X, Y, Z> : Base<Y, Z>
            {
                override $$
            }
            """;
        await VerifyItemExistsAsync(markup, "Goo(Y t, Z s)");
        await VerifyItemIsAbsentAsync(markup, "Goo(T t, S s)");
    }

    [WpfFact]
    public async Task GenericTypeNameSubstitutedForNonGenericInDerivedClass1()
    {
        var markup = """
            public abstract class Base<T>
            {
                public abstract void Goo(T t);
            }

            public class SomeClass : Base<int>
            {
                override $$
            }
            """;
        await VerifyItemExistsAsync(markup, "Goo(int t)");
        await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
    }

    [WpfFact]
    public async Task GenericTypeNameSubstitutedForNonGenericInDerivedClass2()
    {
        var markup = """
            public abstract class Base<T>
            {
                public abstract void Goo(T t);
            }

            public class SomeClass<X, Y, Z> : Base<int>
            {
                override $$
            }
            """;
        await VerifyItemExistsAsync(markup, "Goo(int t)");
        await VerifyItemIsAbsentAsync(markup, "Goo(T t)");
    }

    [WpfFact]
    public async Task GenericTypeNameSubstitutedForNonGenericInDerivedClass3()
    {
        var markup = """
            using System;

            public abstract class Base<T, S>
            {
                public abstract void Goo(T t, S s);
            }

            public class SomeClass : Base<int, Exception>
            {
                override $$
            }
            """;
        await VerifyItemExistsAsync(markup, "Goo(int t, Exception s)");
        await VerifyItemIsAbsentAsync(markup, "Goo(T t, S s)");
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543756")]
    public Task ParameterTypeSimplified()
        => VerifyItemExistsAsync("""
            using System;

            public abstract class Base
            {
                public abstract void Goo(System.Exception e);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "Goo(Exception e)");

    [WpfFact]
    public Task NullableAnnotationsIncluded()
        => VerifyItemExistsAsync("""
            #nullable enable

            public abstract class Base
            {
                public abstract void Goo(string? s);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "Goo(string? s)");

    [WpfFact]
    public async Task EscapedMethodNameInIntelliSenseList()
    {
        MarkupTestFile.GetPosition("""
            public abstract class Base
            {
                public abstract void @class();
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, out var code, out int position);

        await BaseVerifyWorkerAsync(code, position, "@class()", "void Base.@class()", SourceCodeKind.Regular, false, deletedCharTrigger: null, false, null, null, null, null, null, null);
        await BaseVerifyWorkerAsync(code, position, "@class()", "void Base.@class()", SourceCodeKind.Script, false, deletedCharTrigger: null, false, null, null, null, null, null, null);
    }

    [WpfFact]
    public async Task EscapedPropertyNameInIntelliSenseList()
    {
        MarkupTestFile.GetPosition("""
            public abstract class Base
            {
                public virtual int @class { get; set; }
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, out var code, out int position);

        await BaseVerifyWorkerAsync(code, position, "@class", "int Base.@class { get; set; }", SourceCodeKind.Regular, false, deletedCharTrigger: null, false, null, null, null, null, null, null);
        await BaseVerifyWorkerAsync(code, position, "@class", "int Base.@class { get; set; }", SourceCodeKind.Script, false, deletedCharTrigger: null, false, null, null, null, null, null, null);
    }

    [WpfFact]
    public Task EscapedParameterNameInIntelliSenseList()
        => VerifyItemExistsAsync("""
            public abstract class Base
            {
                public abstract void goo(int @class);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(int @class)", "void Base.goo(int @class)");

    [WpfFact]
    public Task RefParameter()
        => VerifyItemExistsAsync("""
            public abstract class Base
            {
                public abstract void goo(int x, ref string y);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(int x, ref string y)", "void Base.goo(int x, ref string y)");

    [WpfFact]
    public Task OutParameter()
        => VerifyItemExistsAsync("""
            public abstract class Base
            {
                public abstract void goo(int x, out string y);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(int x, out string y)", "void Base.goo(int x, out string y)");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")]
    public Task GenericMethodTypeParametersNotRenamed()
        => VerifyItemExistsAsync("""
            abstract class CGoo    
            {    
               public virtual X Something<X>(X arg)    
               {    
                   return default(X);    
                }    
            }    
            class Derived<X> : CGoo    
            {    
                override $$    
            }
            """, "Something<X>(X arg)");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute1()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    override Eq$$
            
                    [That]
                    public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
            
                    [That]
                    public int Disregard = 34;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute2()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    override Eq$$
            
                    // This is a comment
                    [That]
                    public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
            
                    // This is a comment
                    [That]
                    public int Disregard = 34;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute3()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    override Eq$$
            
                    [That] public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
            
                    [That] public int Disregard = 34;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute4()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    override Eq$$
            
                    [That]
                    // Comment after attribute
                    public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
            
                    [That]
                    // Comment after attribute
                    public int Disregard = 34;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute5()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    override Eq$$
            
                    [That, System.Obsolete("")]
                    public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
            
                    [That, System.Obsolete("")]
                    public int Disregard = 34;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute6()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    // Comment before the override.
                    override Eq$$
                    // Comment after the override.

                    [That]
                    public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    // Comment before the override.
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
                    // Comment after the override.

                    [That]
                    public int Disregard = 34;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77193")]
    public Task CommitBeforeAttribute7()
        => VerifyCustomCommitProviderAsync("""
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    override Eq$$
            
                    [That] /* inline comment */ public int Disregard = 34;
                }
            }
            """, "Equals(object obj)", """
            namespace InteliSenseIssue
            {
                [AttributeUsage(AttributeTargets.All)]
                public class ThatAttribute : Attribute {}
            
                internal class Program
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);$$
                    }
            
                    [That] /* inline comment */ public int Disregard = 34;
                }
            }
            """);
    #endregion

    #region "Commit tests"

    [WpfFact]
    public Task CommitInEmptyClass()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                    override $$
            }
            """, "Equals(object obj)", """
            class c
            {
                public override bool Equals(object obj)
                {
                    return base.Equals(obj);$$
                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529714")]
    public Task CommitGenericMethodTypeParametersNotRenamed()
        => VerifyCustomCommitProviderAsync("""
            abstract class CGoo    
            {    
                public virtual X Something<X>(X arg)    
                {    
                    return default(X);    
                }    
            }    
            class Derived<X> : CGoo    
            {    
                override $$    
            }
            """, "Something<X>(X arg)", """
            abstract class CGoo    
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
            }
            """);

    [WpfFact]
    public Task CommitMethodBeforeMethod()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                override $$

                public void goo() { }
            }
            """, "Equals(object obj)", """
            class c
            {
                public override bool Equals(object obj)
                {
                    return base.Equals(obj);$$
                }

                public void goo() { }
            }
            """);

    [WpfFact]
    public Task CommitMethodAfterMethod()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public void goo() { }

                override $$
            }
            """, "Equals(object obj)", """
            class c
            {
                public void goo() { }

                public override bool Equals(object obj)
                {
                    return base.Equals(obj);$$
                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543798")]
    public Task CommitOptionalParameterValuesAreGenerated()
        => VerifyCustomCommitProviderAsync("""
            using System;

            abstract public class Base
            {
                public abstract void goo(int x = 42);
            }

            public class Derived : Base
            {
                override $$
            }
            """, "goo(int x = 42)", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitAttributesAreNotGenerated()
        => VerifyCustomCommitProviderAsync("""
            using System;

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
            }
            """, "goo()", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitInaccessibleParameterAttributesAreNotGenerated()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public class Class1
            {
                private class MyPrivate : Attribute { }
                public class MyPublic : Attribute { }
                public virtual void M([MyPrivate, MyPublic] int i) { }
            }

            public class Class2 : Class1
            {
                public override void $$
            }
            """, "M(int i)", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitVoidMethod()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public virtual void goo() { }
            }

            class d : c
            {
               override $$
            }
            """, "goo()", """
            class c
            {
                public virtual void goo() { }
            }

            class d : c
            {
                public override void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitVoidMethodWithParams()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public virtual void goo(int bar, int quux) { }
            }

            class d : c
            {
               override $$
            }
            """, "goo(int bar, int quux)", """
            class c
            {
                public virtual void goo(int bar, int quux) { }
            }

            class d : c
            {
                public override void goo(int bar, int quux)
                {
                    base.goo(bar, quux);$$
                }
            }
            """);

    [WpfFact]
    public Task CommitNonVoidMethod()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public virtual int goo() { }
            }

            class d : c
            {
               override $$
            }
            """, "goo()", """
            class c
            {
                public virtual int goo() { }
            }

            class d : c
            {
                public override int goo()
                {
                    return base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitNonVoidMethodWithParams()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public virtual int goo(int bar, int quux) { }
            }

            class d : c
            {
               override $$
            }
            """, "goo(int bar, int quux)", """
            class c
            {
                public virtual int goo(int bar, int quux) { }
            }

            class d : c
            {
                public override int goo(int bar, int quux)
                {
                    return base.goo(bar, quux);$$
                }
            }
            """);

    [WpfFact]
    public Task CommitProtectedMethod()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                protected virtual void goo() { }
            }

            class d : c
            {
               override $$
            }
            """, "goo()", """
            class c
            {
                protected virtual void goo() { }
            }

            class d : c
            {
                protected override void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitInternalMethod()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                internal virtual void goo() { }
            }

            class d : c
            {
               override $$
            }
            """, "goo()", """
            class c
            {
                internal virtual void goo() { }
            }

            class d : c
            {
                internal override void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitProtectedInternalMethod()
        => VerifyCustomCommitProviderAsync("""
            public class c
            {
                protected internal virtual void goo() { }
            }

            class d : c
            {
               override $$
            }
            """, "goo()", """
            public class c
            {
                protected internal virtual void goo() { }
            }

            class d : c
            {
                protected internal override void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitAbstractMethodThrows()
        => VerifyCustomCommitProviderAsync("""
            using System;

            abstract class c
            {
                public abstract void goo();
            }

            class d : c
            {
               override $$
            }
            """, "goo()", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitOverrideAsAbstract()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public virtual void goo() { };
            }

            class d : c
            {
               abstract override $$
            }
            """, "goo()", """
            class c
            {
                public virtual void goo() { };
            }

            class d : c
            {
                public abstract override void goo();$$
            }
            """);

    [WpfFact]
    public Task CommitOverrideAsUnsafeSealed()
        => VerifyCustomCommitProviderAsync("""
            class c
            {
                public virtual void goo() { };
            }

            class d : c
            {
               unsafe sealed override $$
            }
            """, "goo()", """
            class c
            {
                public virtual void goo() { };
            }

            class d : c
            {
                public sealed override unsafe void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitInsertProperty()
        => VerifyCustomCommitProviderAsync("""
            public class c
            {
                public virtual int goo { get; set; }
            }

            public class d : c
            {
                override $$
            }
            """, "goo", """
            public class c
            {
                public virtual int goo { get; set; }
            }

            public class d : c
            {
                public override int goo
                {
                    get
                    {
                        [|return base.goo;|]
                    }

                    set
                    {
                        base.goo = value;
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitInsertPropertyAfterMethod()
        => VerifyCustomCommitProviderAsync("""
            public class c
            {
                public virtual int goo { get; set; }
            }

            public class d : c
            {
                public void a() { }
                override $$
            }
            """, "goo", """
            public class c
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
                        [|return base.goo;|]
                    }

                    set
                    {
                        base.goo = value;
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitInsertPropertyBeforeMethod()
        => VerifyCustomCommitProviderAsync("""
            public class c
            {
                public virtual int goo { get; set; }
            }

            public class d : c
            {
                override $$
                public void a() { }
            }
            """, "goo", """
            public class c
            {
                public virtual int goo { get; set; }
            }

            public class d : c
            {
                public override int goo
                {
                    get
                    {
                        [|return base.goo;|]
                    }

                    set
                    {
                        base.goo = value;
                    }
                }
                public void a() { }
            }
            """);

    [WpfFact]
    public Task CommitPropertyInaccessibleGet()
        => VerifyCustomCommitProviderAsync("""
            public class c
            {
                public virtual int goo { private get; set; }
            }

            public class d : c
            {
                override $$
            }
            """, "goo", """
            public class c
            {
                public virtual int goo { private get; set; }
            }

            public class d : c
            {
                public override int goo
                {
                    set
                    {
                        [|base.goo = value;|]
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitPropertyInaccessibleSet()
        => VerifyCustomCommitProviderAsync("""
            public class c
            {
                public virtual int goo { private set; get; }
            }

            public class d : c
            {
                override $$
            }
            """, "goo", """
            public class c
            {
                public virtual int goo { private set; get; }
            }

            public class d : c
            {
                public override int goo
                {
                    get
                    {
                        [|return base.goo;|]
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitInsertPropertyInaccessibleParameterAttributesAreNotGenerated()
        => VerifyCustomCommitProviderAsync("""
            using System;

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
            }
            """, "this[int i]", """
            using System;

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
                            [|return base[i];|]
                        }

                        set
                        {
                            base[i] = value;
                        }
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitAccessibleEvent()
        => VerifyCustomCommitProviderAsync("""
            using System;
            public class a
            {
                public virtual event EventHandler goo;
            }

            public class b : a
            {
                override $$
            }
            """, "goo", """
            using System;
            public class a
            {
                public virtual event EventHandler goo;
            }

            public class b : a
            {
                public override event EventHandler goo;$$
            }
            """);

    [WpfFact]
    public Task CommitEventAfterMethod()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public class a
            {
                public virtual event EventHandler goo;
            }

            public class b : a
            {
                void bar() { }
                override $$
            }
            """, "goo", """
            using System;

            public class a
            {
                public virtual event EventHandler goo;
            }

            public class b : a
            {
                void bar() { }
                public override event EventHandler goo;$$
            }
            """);

    [WpfFact]
    public Task CommitGenericMethod()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public class a
            {
                public virtual void goo<T>() { }
            }

            public class b : a
            {
                override $$
            }
            """, "goo<T>()", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitMethodWithNullableAttributes()
        => VerifyCustomCommitProviderAsync("""
            #nullable enable

            class C
            {
                public virtual string? Goo(string? s) { }
            }

            class D : C
            {
                override $$
            }
            """, "Goo(string? s)", """
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
            }
            """);

    [WpfFact]
    public Task CommitMethodInNullableDisableContext()
        => VerifyCustomCommitProviderAsync("""
            #nullable enable

            class C
            {
                public virtual string? Goo(string? s) { }
            }

            #nullable disable

            class D : C
            {
                override $$
            }
            """, "Goo(string? s)", """
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
            }
            """);

    [WpfFact]
    public Task CommitToStringIsExplicitlyNonNullReturning()
        => VerifyCustomCommitProviderAsync("""
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
            }
            """, "ToString()", """
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
            }
            """);

    [WpfFact]
    public Task CommitInsertIndexer()
        => VerifyCustomCommitProviderAsync("""
            public class MyIndexer<T>
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
            }
            """, "this[int i]", """
            public class MyIndexer<T>
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
                        [|return base[i];|]
                    }

                    set
                    {
                        base[i] = value;
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitAbstractIndexer()
        => VerifyCustomCommitProviderAsync("""
            public class MyIndexer<T>
            {
                private T[] arr = new T[100];
                public abstract T this[int i] { get; set; }
            }

            class d : MyIndexer<T>
            {
                override $$
            }
            """, "this[int i]", """
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
                        [|throw new System.NotImplementedException();|]
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    // The following two scenarios are already verified through 'VerifyCommit',
    // which also tests everything at the end of the file (truncating input markup at $$)
    // public void CommitInsertAtEndOfFile()
    // public void CommitInsertAtEndOfFileAfterMethod()

    [WpfFact]
    public Task CommitFormats()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            override $$
            }
            """, "goo()", """
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                public override void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitSimplifiesParameterTypes()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public abstract class Base
            {
                public abstract void goo(System.Exception e);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(Exception e)", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitSimplifiesReturnType()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public abstract class Base
            {
                public abstract System.ArgumentException goo(System.Exception e);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(Exception e)", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitEscapedMethodName()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public abstract class Base
            {
                public abstract void @class();
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "@class()", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitEscapedPropertyName()
        => VerifyCustomCommitProviderAsync("""
            public abstract class Base
            {
                public virtual int @class { get; set; }
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "@class", """
            public abstract class Base
            {
                public virtual int @class { get; set; }
            }

            public class SomeClass : Base
            {
                public override int @class
                {
                    get
                    {
                        [|return base.@class;|]
                    }

                    set
                    {
                        base.@class = value;
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitEscapedParameterName()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public abstract class Base
            {
                public abstract void goo(int @class);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(int @class)", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitRefParameter()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public abstract class Base
            {
                public abstract void goo(int x, ref string y);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(int x, ref string y)", """
            using System;

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
            }
            """);

    [WpfFact]
    public Task CommitOutParameter()
        => VerifyCustomCommitProviderAsync("""
            using System;

            public abstract class Base
            {
                public abstract void goo(int x, out string y);
            }

            public class SomeClass : Base
            {
                override $$
            }
            """, "goo(int x, out string y)", """
            using System;

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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
    public Task TestUnsafe1()
        => VerifyCustomCommitProviderAsync("""
            public class A
            {
                public unsafe virtual void F()
                {
                }
            }

            public class B : A
            {
                override $$
            }
            """, "F()", """
            public class A
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
    public Task TestUnsafe2()
        => VerifyCustomCommitProviderAsync("""
            public class A
            {
                public unsafe virtual void F()
                {
                }
            }

            public class B : A
            {
                override unsafe $$
            }
            """, "F()", """
            public class A
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
    public Task TestUnsafe3()
        => VerifyCustomCommitProviderAsync("""
            public class A
            {
                public unsafe virtual void F()
                {
                }
            }

            public class B : A
            {
                unsafe override $$
            }
            """, "F()", """
            public class A
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544560")]
    public Task TestUnsafe4()
        => VerifyCustomCommitProviderAsync("""
            public class A
            {
                public virtual void F(int* i)
                {
                }
            }

            public class B : A
            {
                override $$
            }
            """, "F(int* i)", """
            public class A
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545534")]
    public Task TestPrivateVirtualProperty()
        => VerifyCustomCommitProviderAsync("""
            public class B
            {
                public virtual int Goo
                {
                    get; private set;
                }

                class C : B
                {
                    override $$
                }
            }
            """, "Goo", """
            public class B
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
                            [|return base.Goo;|]
                        }
                    }
                }
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636706")]
    public async Task CrossLanguageParameterizedPropertyOverride()
    {
        var vbFile = """
            Public Class Goo
                Public Overridable Property Bar(bay As Integer) As Integer
                    Get
                        Return 23
                    End Get
                    Set(value As Integer)

                    End Set
                End Property
            End Class
            """;
        var csharpFile = """
            class Program : Goo
            {
                override $$
            }
            """;
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <ProjectReference>VBProject</ProjectReference>
                    <Document FilePath="CSharpDocument">{1}</Document>
                </Project>
                <Project Language="{2}" CommonReferences="true" AssemblyName="VBProject">
                    <Document FilePath="VBDocument">
            {3}
                    </Document>
                </Project>

            </Workspace>
            """, LanguageNames.CSharp, csharpFile, LanguageNames.VisualBasic, vbFile);

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
            MarkupTestFile.GetPosition("""
            class Program : Goo
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
            """, out var actualExpectedCode, out int expectedCaretPosition);

            Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
            Assert.Equal(expectedCaretPosition, caretPosition);
        }
    }

    #endregion

    #region "Commit: With Trivia"

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529199")]
    public Task CommitSurroundingTriviaDirective()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            #if true
            override $$
            #endif
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529199")]
    public Task CommitBeforeTriviaDirective()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            override $$
                #if true
                #endif
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact]
    public Task CommitAfterTriviaDirective()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            #if true
            #endif
            override $$
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529199")]
    public Task CommitBeforeComment1()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            override $$
                /* comment */
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact]
    public Task CommitBeforeComment2()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            override $$/* comment */
            }
            """, "goo()", """
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                public override void goo()
                {
                    base.goo();$$
                } /* comment */
            }
            """);

    [WpfFact]
    public Task CommitBeforeComment3()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
            override go$$/* comment */
            }
            """, "goo()", """
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                public override void goo()
                {
                    base.goo();$$
                } /* comment */
            }
            """);

    [WpfFact]
    public Task CommitAfterComment1()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                /* comment */
            override $$
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact]
    public Task CommitAfterComment2()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                /* comment */
                // another comment
            override $$
            }
            """, "goo()", """
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                /* comment */
                // another comment
                public override void goo()
                {
                    base.goo();$$
                }
            }
            """);

    [WpfFact]
    public Task CommitAfterComment3()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                /* comment */ override $$
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact]
    public Task CommitAfterComment4()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                /* comment */ override go$$
            }
            """, "goo()", """
            class Base
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
            }
            """);

    [WpfFact]
    public Task CommitBeforeAndAfterComment()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                // Comment
            override $$
                /* comment */
            }
            """, "goo()", """
            class Base
            {
                public virtual void goo() { }
            }

            class Derived : Base
            {
                // Comment
                public override void goo()
                {
                    base.goo();$$
                }
                /* comment */
            }
            """);

    [WpfFact]
    public Task DoNotFormatFile()
        => VerifyCustomCommitProviderAsync("""
            class Program
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
            }
            """, "goo()", """
            class Program
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
            }
            """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736742")]
    public async Task AcrossPartialTypes1()
    {
        var file1 = """
            partial class c
            {
            }
            """;
        var file2 = """
            partial class c
            {
                override $$
            }
            """;
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="CSharpDocument">{1}</Document>
                    <Document FilePath="CSharpDocument2">{2}</Document>
                </Project>
            </Workspace>
            """, LanguageNames.CSharp, file1, file2);

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
            MarkupTestFile.GetPosition("""
            partial class c
            {
                public override bool Equals(object obj)
                {
                    return base.Equals(obj);$$
                }
            }
            """, out var actualExpectedCode, out int expectedCaretPosition);

            Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
            Assert.Equal(expectedCaretPosition, caretPosition);
        }
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736742")]
    public async Task AcrossPartialTypes2()
    {
        var file1 = """
            partial class c
            {
            }
            """;
        var file2 = """
            partial class c
            {
                override $$
            }
            """;
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="CSharpDocument">{1}</Document>
                    <Document FilePath="CSharpDocument2">{2}</Document>
                </Project>
            </Workspace>
            """, LanguageNames.CSharp, file2, file1);

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
            MarkupTestFile.GetPosition("""
            partial class c
            {
                public override bool Equals(object obj)
                {
                    return base.Equals(obj);$$
                }
            }
            """, out var actualExpectedCode, out int expectedCaretPosition);

            Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
            Assert.Equal(expectedCaretPosition, caretPosition);
        }
    }

    [WpfFact]
    public Task CommitRequiredKeywordAdded()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual required int Prop { get; }
            }

            class Derived : Base
            {
                override $$
            }
            """, "Prop", """
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
                        [|return base.Prop;|]
                    }
                }
            }
            """);

    [WpfTheory]
    [InlineData("required override")]
    [InlineData("override required")]
    public Task CommitRequiredKeywordPreserved(string ordering)
        => VerifyCustomCommitProviderAsync($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" LanguageVersion="{{TestOptions.Regular11.LanguageVersion.ToDisplayString()}}">
                    <Document>class Base
            {
                public virtual required int Prop { get; }
            }

            class Derived : Base
            {
                {{ordering}} $$
            }</Document>
                </Project>
            </Workspace>
            """, "Prop", """
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
                        [|return base.Prop;|]
                    }
                }
            }
            """);

    [WpfTheory]
    [InlineData("required override")]
    [InlineData("override required")]
    public Task CommitRequiredKeywordPreservedWhenBaseIsNotRequired(string ordering)
        => VerifyCustomCommitProviderAsync($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" LanguageVersion="{{TestOptions.Regular11.LanguageVersion.ToDisplayString()}}">
                    <Document>class Base
            {
                public virtual int Prop { get; }
            }

            class Derived : Base
            {
                {{ordering}} $$
            }</Document>
                </Project>
            </Workspace>
            """, "Prop", """
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
                        [|return base.Prop;|]
                    }
                }
            }
            """);

    [WpfFact]
    public Task CommitRequiredKeywordRemovedForMethods()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual void M() { }
            }

            class Derived : Base
            {
                required override $$
            }
            """, "M()", """
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
            """);

    [WpfFact]
    public Task CommitRequiredKeywordRemovedForIndexers()
        => VerifyCustomCommitProviderAsync("""
            class Base
            {
                public virtual int this[int i] { get { } set { } }
            }

            class Derived : Base
            {
                required override $$
            }
            """, "this[int i]", """
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
                        [|return base[i];|]
                    }

                    set
                    {
                        base[i] = value;
                    }
                }
            }
            """);

    #endregion

    #region "EditorBrowsable should be ignored"

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")]
    public async Task EditorBrowsable_IgnoredWhenOverridingMethods()
    {
        var markup = """
            class D : B
            {
                override $$
            }
            """;
        var referencedCode = """
            public class B
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public virtual void Goo() {}
            }
            """;
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
    public Task DuplicateMember()
        => VerifyCustomCommitProviderAsync("""
            class Program
            {
                public virtual void goo() {}
                public virtual void goo() {}
            }

            class C : Program
            {
                override $$
            }
            """, "goo()", """
            class Program
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
            }
            """);

    [WpfFact]
    public async Task LeaveTrailingTriviaAlone()
    {
        var text = """

            namespace ConsoleApplication46
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                    }

                    override $$
                }
            }
            """;
        using var workspace = EditorTestWorkspace.Create(LanguageNames.CSharp, new CSharpCompilationOptions(OutputKind.ConsoleApplication), new CSharpParseOptions(), [text], composition: GetComposition());
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
    public Task NotImplementedQualifiedWhenSystemUsingNotPresent_Property()
        => VerifyCustomCommitProviderAsync("""
            abstract class C
            {
                public abstract int goo { get; set; };
            }

            class Program : C
            {
                override $$
            }
            """, "goo", """
            abstract class C
            {
                public abstract int goo { get; set; };
            }

            class Program : C
            {
                public override int goo
                {
                    get
                    {
                        [|throw new System.NotImplementedException();|]
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/8257")]
    public Task NotImplementedQualifiedWhenSystemUsingNotPresent_Method()
        => VerifyCustomCommitProviderAsync("""
            abstract class C
            {
                public abstract void goo();
            }

            class Program : C
            {
                override $$
            }
            """, "goo()", """
            abstract class C
            {
                public abstract void goo();
            }

            class Program : C
            {
                public override void goo()
                {
                    throw new System.NotImplementedException();$$
                }
            }
            """);

    [Fact]
    public async Task FilterOutMethodsWithNonRoundTrippableSymbolKeys()
    {
        var text = XElement.Parse("""
            <Workspace>
                <Project Name="P1" Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C : ClassLibrary7.Class1
            {
                override $$
            }
            ]]>
                    </Document>
                </Project>
                <Project Name="P2" Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document>
            namespace ClassLibrary2
            {
                public class Missing {}
            }
                    </Document>
                </Project>
                <Project Name="P3" Language="C#" CommonReferences="true" AssemblyName="Proj3.dll">
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
            </Workspace>
            """);

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
        var markup = """
            public class SomeClass : Base
            {
                override $$
            }
            """;
        var source = XElement.Parse($"""
            <Workspace>
                <Project Name="P1" Language="C#" LanguageVersion="Latest" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[{markup}]]>
                    </Document>
                </Project>
            </Workspace>
            """);

        using var workspace = EditorTestWorkspace.Create(source, composition: GetComposition());
        var before = """
            public abstract class Base
            {
                public abstract void M(in int x);
            }
            """;
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

        Assert.Equal("""
            public class SomeClass : Base
            {
                public override void M(in int x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """, actualCodeAfterCommit);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39909")]
    public Task CommitAddsMissingImports()
        => VerifyCustomCommitProviderAsync("""
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
            }
            """, "Bar(NS2.Baz baz)", """
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
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47941")]
    public Task OverrideInRecordWithoutExplicitOverriddenMember()
        => VerifyItemExistsAsync("""
            record Program
            {
                override $$
            }
            """, "ToString()");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47941")]
    public Task OverrideInRecordWithExplicitOverriddenMember()
        => VerifyItemIsAbsentAsync("""
            record Program
            {
                public override string ToString() => ";

                override $$
            }
            """, "ToString()");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47973")]
    public async Task NoCloneInOverriddenRecord()
    {
        // Currently WellKnownMemberNames.CloneMethodName is not public, so we can't reference it directly.  We
        // could hardcode in the value "<Clone>$", however if the compiler ever changed the name and we somehow
        // started showing it in completion, this test would continue to pass.  So this allows us to at least go
        // back and explicitly validate this scenario even in that event.
        var cloneMemberName = (string)typeof(WellKnownMemberNames).GetField("CloneMethodName", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Assert.Equal("<Clone>$", cloneMemberName);

        await VerifyItemIsAbsentAsync("""
            record Base();

            record Program : Base
            {
                override $$
            }
            """, cloneMemberName);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48640")]
    public Task ObjectEqualsInClass()
        => VerifyItemExistsAsync("""
            class Program 
            {
                override $$
            }
            """, "Equals(object obj)");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48640")]
    public async Task NoObjectEqualsInOverriddenRecord1()
    {
        await VerifyItemIsAbsentAsync("""
            record Program 
            {
                override $$
            }
            """, "Equals(object obj)");

        await VerifyItemExistsAsync("""
            record Program 
            {
                override $$
            }
            """, "ToString()");

    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/48640")]
    public async Task NoObjectEqualsInOverriddenRecord()
    {
        await VerifyItemIsAbsentAsync("""
            record Base();

            record Program : Base
            {
                override $$
            }
            """, "Equals(object obj)");

        await VerifyItemExistsAsync("""
            record Base();

            record Program : Base
            {
                override $$
            }
            """, "ToString()");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
    public Task WithAttribute1()
        => VerifyItemExistsAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
    public Task WithAttribute2()
        => VerifyItemExistsAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
    public Task NotWhenMultilineModifiers()
        => VerifyItemIsAbsentAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64887")]
    public Task NotWhenMultilineModifiersAndAttribute()
        => VerifyItemIsAbsentAsync("""
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

    [WpfFact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6308")]
    public Task NoOverrideItemsWhenNotInTypeDeclaration()
        => VerifyNoItemsExistAsync("""
            namespace NS
            {
                override $$
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6308")]
    public Task NoOverrideItemsAtTopLevel()
        => VerifyNoItemsExistAsync("""
            System.Console.WriteLine();
            
            override $$
            """);

    private Task VerifyItemExistsAsync(string markup, string expectedItem)
    {
        return VerifyItemExistsAsync(markup, expectedItem, isComplexTextEdit: true);
    }
}
