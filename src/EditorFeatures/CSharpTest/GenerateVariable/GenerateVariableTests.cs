﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.GenerateVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateVariable
{
    public class GenerateVariableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpGenerateVariableCodeFixProvider());

        private readonly CodeStyleOption<bool> onWithInfo = new CodeStyleOption<bool>(true, NotificationOption.Suggestion);

        // specify all options explicitly to override defaults.
        private IDictionary<OptionKey, object> ImplicitTypingEverywhere() => OptionsSet(
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, onWithInfo),
            SingleOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, onWithInfo));

        internal IDictionary<OptionKey, object> OptionSet(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleLowercaseIdentifier1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    private object foo;

    void Method()
    {
        foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleLowercaseIdentifier2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    private readonly object foo;

    void Method()
    {
        foo;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestTestSimpleLowercaseIdentifier3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    public object foo { get; private set; }

    void Method()
    {
        foo;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleUppercaseIdentifier1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    public object Foo { get; private set; }

    void Method()
    {
        Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleUppercaseIdentifier2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    private object Foo;

    void Method()
    {
        Foo;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleUppercaseIdentifier3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    private readonly object Foo;

    void Method()
    {
        Foo;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleRead1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        Method([|foo|]);
    }
}",
@"class Class
{
    private int foo;

    void Method(int i)
    {
        Method(foo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWriteCount()
        {
            await TestExactActionSetOfferedAsync(
@"class Class
{
    void Method(int i)
    {
        [|foo|] = 1;
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_1_0, "foo", "Class"), string.Format(FeaturesResources.Generate_property_1_0, "foo", "Class"), string.Format(FeaturesResources.Generate_local_0, "foo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWrite1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|foo|] = 1;
    }
}",
@"class Class
{
    private int foo;

    void Method(int i)
    {
        foo = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSimpleWrite2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|foo|] = 1;
    }
}",
@"class Class
{
    public int foo { get; private set; }

    void Method(int i)
    {
        foo = 1;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInRef()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(ref int i)
    {
        Method(ref this.[|foo|]);
    }
}",
@"class Class
{
    private int foo;

    void Method(ref int i)
    {
        Method(ref this.[|foo|]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInRef()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class Class
{
    void Method(ref int i)
    {
        Method(ref this.[|foo|]);
    }
}",
@"
using System;
class Class
{
    public ref int foo => throw new NotImplementedException();

    void Method(ref int i)
    {
        Method(ref this.foo);
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInRef1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(ref int i)
    {
        Method(ref [|foo|]);
    }
}",
@"class Class
{
    private int foo;

    void Method(ref int i)
    {
        Method(ref foo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInOutCodeActionCount()
        {
            await TestExactActionSetOfferedAsync(
@"class Class
{
    void Method(out int i)
    {
        Method(out [|foo|]);
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_1_0, "foo", "Class"), string.Format(FeaturesResources.Generate_local_0, "foo") });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInOut1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(out int i)
    {
        Method(out [|foo|]);
    }
}",
@"class Class
{
    private int foo;

    void Method(out int i)
    {
        Method(out foo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInStaticMember1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    static void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    private static object foo;

    static void Method()
    {
        foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInStaticMember2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    static void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    private static readonly object foo;

    static void Method()
    {
        foo;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInStaticMember3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    static void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    public static object foo { get; private set; }

    static void Method()
    {
        foo;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffInstance1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|foo|];
    }
}",
@"class Class
{
    private object foo;

    void Method()
    {
        this.foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffInstance2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|foo|];
    }
}",
@"class Class
{
    private readonly object foo;

    void Method()
    {
        this.foo;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffInstance3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|foo|];
    }
}",
@"class Class
{
    public object foo { get; private set; }

    void Method()
    {
        this.foo;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenInstance1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|foo|] = 1;
    }
}",
@"class Class
{
    private int foo;

    void Method()
    {
        this.foo = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenInstance2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|foo|] = 1;
    }
}",
@"class Class
{
    public int foo { get; private set; }

    void Method()
    {
        this.foo = 1;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffStatic1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|foo|];
    }
}",
@"class Class
{
    private static object foo;

    void Method()
    {
        Class.foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffStatic2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|foo|];
    }
}",
@"class Class
{
    private static readonly object foo;

    void Method()
    {
        Class.foo;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffStatic3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|foo|];
    }
}",
@"class Class
{
    public static object foo { get; private set; }

    void Method()
    {
        Class.foo;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenStatic1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|foo|] = 1;
    }
}",
@"class Class
{
    private static int foo;

    void Method()
    {
        Class.foo = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateOffWrittenStatic2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|foo|] = 1;
    }
}",
@"class Class
{
    public static int foo { get; private set; }

    void Method()
    {
        Class.foo = 1;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInstanceIntoSibling1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        new D().[|foo|];
    }
}

class D
{
}",
@"class Class
{
    void Method()
    {
        new D().foo;
    }
}

class D
{
    internal object foo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInstanceIntoOuter1()
        {
            await TestInRegularAndScriptAsync(
@"class Outer
{
    class Class
    {
        void Method()
        {
            new Outer().[|foo|];
        }
    }
}",
@"class Outer
{
    private object foo;

    class Class
    {
        void Method()
        {
            new Outer().foo;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInstanceIntoDerived1()
        {
            await TestInRegularAndScriptAsync(
@"class Class : Base
{
    void Method(Base b)
    {
        b.[|foo|];
    }
}

class Base
{
}",
@"class Class : Base
{
    void Method(Base b)
    {
        b.foo;
    }
}

class Base
{
    internal object foo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateStaticIntoDerived1()
        {
            await TestInRegularAndScriptAsync(
@"class Class : Base
{
    void Method(Base b)
    {
        Base.[|foo|];
    }
}

class Base
{
}",
@"class Class : Base
{
    void Method(Base b)
    {
        Base.foo;
    }
}

class Base
{
    protected static object foo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoInterfaceFixCount()
        {
            await TestActionCountAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|foo|];
    }
}

interface I
{
}",
count: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoInterface1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Foo|];
    }
}

interface I
{
}",
@"class Class
{
    void Method(I i)
    {
        i.Foo;
    }
}

interface I
{
    object Foo { get; set; }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoInterface2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Foo|];
    }
}

interface I
{
}",
@"class Class
{
    void Method(I i)
    {
        i.Foo;
    }
}

interface I
{
    object Foo { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateStaticIntoInterfaceMissing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        I.[|Foo|];
    }
}

interface I
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateWriteIntoInterfaceFixCount()
        {
            await TestActionCountAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Foo|] = 1;
    }
}

interface I
{
}",
count: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateWriteIntoInterface1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(I i)
    {
        i.[|Foo|] = 1;
    }
}

interface I
{
}",
@"class Class
{
    void Method(I i)
    {
        i.Foo = 1;
    }
}

interface I
{
    int Foo { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericType()
        {
            await TestInRegularAndScriptAsync(
@"class Class<T>
{
    void Method(T t)
    {
        [|foo|] = t;
    }
}",
@"class Class<T>
{
    private T foo;

    void Method(T t)
    {
        foo = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method<T>(T t)
    {
        [|foo|] = t;
    }
}",
@"class Class
{
    private object foo;

    void Method<T>(T t)
    {
        foo = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method<T>(IList<T> t)
    {
        [|foo|] = t;
    }
}",
@"class Class
{
    private IList<object> foo;

    void Method<T>(IList<T> t)
    {
        foo = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldBeforeFirstField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int i;

    void Method()
    {
        [|foo|];
    }
}",
@"class Class
{
    int i;
    private object foo;

    void Method()
    {
        foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldAfterLastField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|foo|];
    }

    int i;
}",
@"class Class
{
    void Method()
    {
        foo;
    }

    int i;
    private object foo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyAfterLastField1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Bar;

    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    int Bar;

    public object Foo { get; private set; }

    void Method()
    {
        Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyAfterLastField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Foo|];
    }

    int Bar;
}",
@"class Class
{
    void Method()
    {
        Foo;
    }

    int Bar;

    public object Foo { get; private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyBeforeFirstProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Quux { get; }

    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    public object Foo { get; private set; }
    int Quux { get; }

    void Method()
    {
        Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyBeforeFirstPropertyEvenWithField1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Bar;

    int Quux { get; }

    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    int Bar;

    public object Foo { get; private set; }
    int Quux { get; }

    void Method()
    {
        Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyAfterLastPropertyEvenWithField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    int Quux { get; }

    int Bar;

    void Method()
    {
        [|Foo|];
    }
}",
@"class Class
{
    int Quux { get; }
    public object Foo { get; private set; }

    int Bar;

    void Method()
    {
        Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInInvocation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Foo|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInObjectCreation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        new [|Foo|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInTypeDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A.B|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A|].B a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        A.[|B|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A.B.C|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A.B|].C a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        A.B.[|C|] a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|A|].B.C a;
    }
}");

            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        A.[|B|].C a;
    }
}");
        }

        [WorkItem(539336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539336")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingInAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"[[|A|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A.B|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A|].B]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[A.[|B|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A.B.C|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A.B|].C]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[A.B.[|C|]]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[[|A|].B.C]
class Class
{
}");

            await TestMissingInRegularAndScriptAsync(
@"[A.B.[|C|]]
class Class
{
}");
        }

        [WorkItem(539340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539340")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSpansField()
        {
            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|] }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|];
    }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|] = 1 }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|] = 1 + 2 }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|] = 1 + 2;
    }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|] += Bar() }");

            await TestSpansAsync(
@"class C
{
    void M()
    {
        this.[|Foo|] += Bar();
    }");
        }

        [WorkItem(539427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539427")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromLambda()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|foo|] = () => {
            return 2 };
    }
}",
@"using System;

class Class
{
    private Func<int> foo;

    void Method(int i)
    {
        foo = () => {
            return 2 };
    }
}");
        }

        // TODO: Move to TypeInferrer.InferTypes, or something
        [WorkItem(539466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539466")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInMethodOverload1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        System.Console.WriteLine([|foo|]);
    }
}",
@"class Class
{
    private bool foo;

    void Method(int i)
    {
        System.Console.WriteLine(foo);
    }
}");
        }

        // TODO: Move to TypeInferrer.InferTypes, or something
        [WorkItem(539466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539466")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInMethodOverload2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        System.Console.WriteLine(this.[|foo|]);
    }
}",
@"class Class
{
    private bool foo;

    void Method(int i)
    {
        System.Console.WriteLine(this.foo);
    }
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty1()
        {
            await TestInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { get; set; }
}

interface ITest
{
}",
@"class Class : ITest
{
    bool ITest.SomeProp { get; set; }
}

interface ITest
{
    bool SomeProp { get; set; }
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
}",
@"class Class : ITest
{
    bool ITest.SomeProp { }
}

interface ITest
{
    bool SomeProp { get; set; }
}", index: 1);
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty3()
        {
            await TestInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
}",
@"class Class : ITest
{
    bool ITest.SomeProp { }
}

interface ITest
{
    bool SomeProp { get; }
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
}");
        }

        [WorkItem(539468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExplicitProperty5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class : ITest
{
    bool ITest.[|SomeProp|] { }
}

interface ITest
{
    bool SomeProp { get; }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestEscapedName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|@foo|];
    }
}",
@"class Class
{
    private object foo;

    void Method()
    {
        @foo;
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestEscapedKeyword()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|@int|];
    }
}",
@"class Class
{
    private object @int;

    void Method()
    {
        @int;
    }
}");
        }

        [WorkItem(539529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539529")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestRefLambda()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|test|] = (ref int x) => x = 10;
    }
}",
@"class Class
{
    private object test;

    void Method()
    {
        test = (ref int x) => x = 10;
    }
}");
        }

        [WorkItem(539595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539595")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnError()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void F<U, V>(U u1, V v1)
    {
        Foo<string, int>([|u1|], u2);
    }
}");
        }

        [WorkItem(539571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539571")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNameSimplification()
        {
            await TestInRegularAndScriptAsync(
@"namespace TestNs
{
    class Program
    {
        class Test
        {
            void Meth()
            {
                Program.[|blah|] = new Test();
            }
        }
    }
}",
@"namespace TestNs
{
    class Program
    {
        private static Test blah;

        class Test
        {
            void Meth()
            {
                Program.blah = new Test();
            }
        }
    }
}");
        }

        [WorkItem(539717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539717")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPostIncrement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|i|]++;
    }
}",
@"class Program
{
    private static int i;

    static void Main(string[] args)
    {
        i++;
    }
}");
        }

        [WorkItem(539717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539717")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPreDecrement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        --[|i|];
    }
}",
@"class Program
{
    private static int i;

    static void Main(string[] args)
    {
        --i;
    }
}");
        }

        [WorkItem(539738, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539738")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoScript()
        {
            await TestAsync(
@"using C;

static class C
{
}

C.[|i|] ++ ;",
@"using C;

static class C
{
    internal static int i;
}

C.i ++ ;",
parseOptions: Options.Script);
        }

        [WorkItem(539558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539558")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task BugFix5565()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|Foo|]#();
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static object Foo { get; private set; }

    static void Main(string[] args)
    {
        Foo#();
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(539536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539536")]
        [Fact(Skip = "Tuples"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task BugFix5538()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        new([|foo|])();
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static object foo { get; private set; }

    static void Main(string[] args)
    {
        new(foo)();
    }
}",
index: 2);
        }

        [WorkItem(539665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task BugFix5697()
        {
            await TestInRegularAndScriptAsync(
@"class C { }
class D
{
    void M()
    {
        C.[|P|] = 10;
    }
}
",
@"class C
{
    public static int P { get; internal set; }
}
class D
{
    void M()
    {
        C.P = 10;
    }
}
",
ignoreTrivia: false);
        }

        [WorkItem(539793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIncrement()
        {
            await TestExactActionSetOfferedAsync(
@"class Program
{
    static void Main()
    {
        [|p|]++;
    }
}",
new[] { string.Format(FeaturesResources.Generate_field_1_0, "p", "Program"), string.Format(FeaturesResources.Generate_property_1_0, "p", "Program"), string.Format(FeaturesResources.Generate_local_0, "p") });

            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        [|p|]++;
    }
}",
@"class Program
{
    private static int p;

    static void Main()
    {
        p++;
    }
}");
        }

        [WorkItem(539834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539834")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task TestNotInGoto()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        goto [|foo|];
    }
}");
        }

        [WorkItem(539826, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539826")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnLeftOfDot()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        [|foo|].ToString();
    }
}",
@"class Program
{
    private static object foo;

    static void Main()
    {
        foo.ToString();
    }
}");
        }

        [WorkItem(539840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539840")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotBeforeAlias()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|global|]::System.String s;
    }
}");
        }

        [WorkItem(539871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539871")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnGenericName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C<T>
{
    public delegate void Foo<R>(R r);

    static void M()
    {
        Foo<T> r = [|Goo<T>|];
    }
}");
        }

        [WorkItem(539934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539934")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnDelegateAddition()
        {
            await TestAsync(
@"class C
{
    delegate void D();

    void M()
    {
        D d = [|M1|] + M2;
    }
}",
@"class C
{
    private D M1 { get; set; }

    delegate void D();

    void M()
    {
        D d = M1 + M2;
    }
}",
parseOptions: null);
        }

        [WorkItem(539986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestReferenceTypeParameter1()
        {
            await TestInRegularAndScriptAsync(
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.[|M|];
    }
}

class A
{
}",
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.M;
    }
}

class A
{
    public static C<object> M { get; internal set; }
}");
        }

        [WorkItem(539986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestReferenceTypeParameter2()
        {
            await TestInRegularAndScriptAsync(
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.[|M|];
    }

    class A
    {
    }
}",
@"class C<T>
{
    public void Test()
    {
        C<T> c = A.M;
    }

    class A
    {
        public static C<T> M { get; internal set; }
    }
}");
        }

        [WorkItem(540159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540159")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestEmptyIdentifierName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        int i = [|@|] }
}");
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        int i = [|@|]}
}");
        }

        [WorkItem(541194, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541194")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestForeachVar()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var v in [|list|])
        {
        }
    }
}",
@"using System.Collections.Generic;

class C
{
    private IEnumerable<object> list;

    void M()
    {
        foreach (var v in list)
        {
        }
    }
}");
        }

        [WorkItem(541265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541265")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExtensionMethodUsedAsInstance()
        {
            await TestAsync(
@"using System;

class C
{
    public static void Main()
    {
        string s = ""Hello"";
        [|f|] = s.ExtensionMethod;
    }
}

public static class MyExtension
{
    public static int ExtensionMethod(this String s)
    {
        return s.Length;
    }
}",
@"using System;

class C
{
    private static Func<int> f;

    public static void Main()
    {
        string s = ""Hello"";
        f = s.ExtensionMethod;
    }
}

public static class MyExtension
{
    public static int ExtensionMethod(this String s)
    {
        return s.Length;
    }
}",
parseOptions: null);
        }

        [WorkItem(541549, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541549")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestDelegateInvoke()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = x => x + 1;
        f([|x|]);
    }
}",
@"using System;

class Program
{
    private static int x;

    static void Main(string[] args)
    {
        Func<int, int> f = x => x + 1;
        f(x);
    }
}");
        }

        [WorkItem(541597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541597")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestComplexAssign1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|a|] = a + 10;
    }
}",
@"class Program
{
    private static int a;

    static void Main(string[] args)
    {
        a = a + 10;
    }
}");
        }

        [WorkItem(541597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541597")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestComplexAssign2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        a = [|a|] + 10;
    }
}",
@"class Program
{
    private static int a;

    static void Main(string[] args)
    {
        a = a + 10;
    }
}");
        }

        [WorkItem(541659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541659")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestTypeNamedVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    public static void Main()
    {
        var v = [|p|];
    }
}

class var
{
}",
@"using System;

class Program
{
    private static var p;

    public static void Main()
    {
        var v = p;
    }
}

class var
{
}");
        }

        [WorkItem(541675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestStaticExtensionMethodArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        MyExtension.ExMethod([|ss|]);
    }
}

static class MyExtension
{
    public static int ExMethod(this string s)
    {
        return s.Length;
    }
}",
@"using System;

class Program
{
    private static string ss;

    static void Main(string[] args)
    {
        MyExtension.ExMethod(ss);
    }
}

static class MyExtension
{
    public static int ExMethod(this string s)
    {
        return s.Length;
    }
}");
        }

        [WorkItem(539675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBeforeCommentBetweenMembers1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    //method
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    public static int P { get; private set; }

    //method
    static void Main(string[] args)
    {
        P = 10;
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(539675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539675")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBeforeCommentBetweenMembers2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    //method
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    private static int P;

    //method
    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 1,
ignoreTrivia: false);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBetweenMembers1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 1,
ignoreTrivia: false);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task AddBlankLineBetweenMembers2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 0,
ignoreTrivia: false);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task DontAddBlankLineBetweenFields()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
        [|A|] = 9;
    }
}",
@"class Program
{
    private static int P;
    private static int A;

    static void Main(string[] args)
    {
        P = 10;
        A = 9;
    }
}",
index: 1,
ignoreTrivia: false);
        }

        [WorkItem(543813, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task DontAddBlankLineBetweenAutoProperties()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
        [|A|] = 9;
    }
}",
@"class Program
{
    public static int P { get; private set; }
    public static int A { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
        A = 9;
    }
}",
index: 0,
ignoreTrivia: false);
        }

        [WorkItem(539665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestIntoEmptyClass()
        {
            await TestInRegularAndScriptAsync(
@"class C { }
class D
{
    void M()
    {
        C.[|P|] = 10;
    }
}",
@"class C
{
    public static int P { get; internal set; }
}
class D
{
    void M()
    {
        C.P = 10;
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(540595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540595")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInScript()
        {
            await TestAsync(
@"[|Foo|]",
@"object Foo { get; private set; }

Foo",
parseOptions: Options.Script,
ignoreTrivia: false);
        }

        [WorkItem(542535, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542535")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConstantInParameterValue()
        {
            const string Initial =
@"class C
{   
    const int y = 1 ; 
    public void Foo ( bool x = [|undeclared|] ) { }
} ";

            await TestActionCountAsync(
Initial,
count: 1);

            await TestInRegularAndScriptAsync(
Initial,
@"class C
{
    const int y = 1;
    private const bool undeclared;

    public void Foo(bool x = undeclared)
    {
    }
}");
        }

        [WorkItem(542900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542900")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAttributeNamedArgument1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class ProgramAttribute : Attribute
{
    [Program([|Name|] = 0)]
    static void Main(string[] args)
    {
    }
}",
@"using System;

class ProgramAttribute : Attribute
{
    public int Name { get; set; }

    [Program(Name = 0)]
    static void Main(string[] args)
    {
    }
}");
        }

        [WorkItem(542900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542900")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAttributeNamedArgument2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class ProgramAttribute : Attribute
{
    [Program([|Name|] = 0)]
    static void Main(string[] args)
    {
    }
}",
@"using System;

class ProgramAttribute : Attribute
{
    public int Name;

    [Program(Name = 0)]
    static void Main(string[] args)
    {
    }
}",
index: 1);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility1_InternalPrivate()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    private class C
    {
    }
}",
@"class Program
{
    private static C P { get; set; }

    public static void Main()
    {
        C c = P;
    }

    private class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility2_InternalProtected()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected class C
    {
    }
}",
@"class Program
{
    protected static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility3_InternalInternal()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    internal class C
    {
    }
}",
@"class Program
{
    public static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility4_InternalProtectedInternal()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected internal class C
    {
    }
}",
@"class Program
{
    public static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility5_InternalPublic()
        {
            await TestAsync(
@"class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    public class C
    {
    }
}",
@"class Program
{
    public static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    public class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility6_PublicInternal()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    internal class C
    {
    }
}",
@"public class Program
{
    internal static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility7_PublicProtectedInternal()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected internal class C
    {
    }
}",
@"public class Program
{
    protected internal static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected internal class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility8_PublicProtected()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    protected class C
    {
    }
}",
@"public class Program
{
    protected static C P { get; private set; }

    public static void Main()
    {
        C c = P;
    }

    protected class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility9_PublicPrivate()
        {
            await TestAsync(
@"public class Program
{
    public static void Main()
    {
        C c = [|P|];
    }

    private class C
    {
    }
}",
@"public class Program
{
    private static C P { get; set; }

    public static void Main()
    {
        C c = P;
    }

    private class C
    {
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility10_PrivatePrivate()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        private class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        private class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility11_PrivateProtected()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        protected class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        protected class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility12_PrivateProtectedInternal()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        protected internal class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        protected internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility13_PrivateInternal()
        {
            await TestAsync(
@"class outer
{
    private class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        internal class C
        {
        }
    }
}",
@"class outer
{
    private class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility14_ProtectedPrivate()
        {
            await TestAsync(
@"class outer
{
    protected class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        private class C
        {
        }
    }
}",
@"class outer
{
    protected class Program
    {
        private static C P { get; set; }

        public static void Main()
        {
            C c = P;
        }

        private class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility15_ProtectedInternal()
        {
            await TestAsync(
@"class outer
{
    protected class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        internal class C
        {
        }
    }
}",
@"class outer
{
    protected class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility16_ProtectedInternalProtected()
        {
            await TestAsync(
@"class outer
{
    protected internal class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        protected class C
        {
        }
    }
}",
@"class outer
{
    protected internal class Program
    {
        protected static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        protected class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(541698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMinimalAccessibility17_ProtectedInternalInternal()
        {
            await TestAsync(
@"class outer
{
    protected internal class Program
    {
        public static void Main()
        {
            C c = [|P|];
        }

        internal class C
        {
        }
    }
}",
@"class outer
{
    protected internal class Program
    {
        public static C P { get; private set; }

        public static void Main()
        {
            C c = P;
        }

        internal class C
        {
        }
    }
}",
parseOptions: null);
        }

        [WorkItem(543153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543153")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestAnonymousObjectInitializer1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var a = new { x = 5 };
        a = new { x = [|HERE|] };
    }
}",
@"class C
{
    private int HERE;

    void M()
    {
        var a = new { x = 5 };
        a = new { x = HERE };
    }
}",
index: 1);
        }

        [WorkItem(543124, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543124")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNoGenerationIntoAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        var v = new { };
        bool b = v.[|Bar|];
    }
}");
        }

        [WorkItem(543543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543543")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOfferedForBoundParametersOfOperators()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    public Program(string s)
    {
    }

    static void Main(string[] args)
    {
        Program p = """";
    }

    public static implicit operator Program(string str)
    {
        return new Program([|str|]);
    }
}");
        }

        [WorkItem(544175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544175")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnNamedParameterName1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class class1
{
    public void Test()
    {
        Foo([|x|]: x);
    }

    public string Foo(int x)
    {
    }
}");
        }

        [WorkItem(544271, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544271")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnNamedParameterName2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Foo
{
    public Foo(int a = 42)
    {
    }
}

class DogBed : Foo
{
    public DogBed(int b) : base([|a|]: b)
    {
    }
}");
        }

        [WorkItem(544164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544164")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { [|Gibberish|] = 24 };
    }
}",
@"class Foo
{
    public int Gibberish { get; internal set; }
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = 24 };
    }
}");
        }

        [WorkItem(13166, "https://github.com/dotnet/roslyn/issues/13166")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnNestedObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Inner
{
}

public class Outer
{
    public Inner Inner { get; set; } = new Inner();

    public static Outer X() => new Outer { Inner = { [|InnerValue|] = 5 } };
}",
@"public class Inner
{
    public int InnerValue { get; internal set; }
}

public class Outer
{
    public Inner Inner { get; set; } = new Inner();

    public static Outer X() => new Outer { Inner = { InnerValue = 5 } };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnObjectInitializer1()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { [|Gibberish|] = Gibberish };
    }
}",
@"class Foo
{
    public object Gibberish { get; internal set; }
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = Gibberish };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPropertyOnObjectInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = [|Gibberish|] };
    }
}",
@"class Foo
{
}

class Bar
{
    public object Gibberish { get; private set; }

    void foo()
    {
        var c = new Foo { Gibberish = Gibberish };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFieldOnObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { [|Gibberish|] = 24 };
    }
}",
@"class Foo
{
    internal int Gibberish;
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = 24 };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFieldOnObjectInitializer1()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { [|Gibberish|] = Gibberish };
    }
}",
@"class Foo
{
    internal object Gibberish;
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = Gibberish };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFieldOnObjectInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = [|Gibberish|] };
    }
}",
@"class Foo
{
}

class Bar
{
    private object Gibberish;

    void foo()
    {
        var c = new Foo { Gibberish = Gibberish };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnlyPropertyAndFieldOfferedForObjectInitializer()
        {
            await TestActionCountAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { . [|Gibberish|] = 24 };
    }
}",
2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInObjectInitializerValue()
        {
            await TestInRegularAndScriptAsync(
@"class Foo
{
}

class Bar
{
    void foo()
    {
        var c = new Foo { Gibberish = [|blah|] };
    }
}",
@"class Foo
{
}

class Bar
{
    void foo()
    {
        object blah = null;
        var c = new Foo { Gibberish = blah };
    }
}",
index: 3);
        }

        [WorkItem(544319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnIncompleteMember1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class1
{
    Console.[|WriteLine|](); }");
        }

        [WorkItem(544319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnIncompleteMember2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class1
{ [|WriteLine|]();
}");
        }

        [WorkItem(544319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnIncompleteMember3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Class1
{
    [|WriteLine|]
}");
        }

        [WorkItem(544384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544384")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPointerType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static int x;

    unsafe static void F(int* p)
    {
        *p = 1;
    }

    static unsafe void Main(string[] args)
    {
        int[] a = new int[10];
        fixed (int* p2 = &x, int* p3 = ) F(GetP2([|p2|]));
    }

    unsafe private static int* GetP2(int* p2)
    {
        return p2;
    }
}",
@"class Program
{
    static int x;
    private static unsafe int* p2;

    unsafe static void F(int* p)
    {
        *p = 1;
    }

    static unsafe void Main(string[] args)
    {
        int[] a = new int[10];
        fixed (int* p2 = &x, int* p3 = ) F(GetP2(p2));
    }

    unsafe private static int* GetP2(int* p2)
    {
        return p2;
    }
}");
        }

        [WorkItem(544510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnUsingAlias()
        {
            await TestMissingInRegularAndScriptAsync(
@"using [|S|] = System ; S . Console . WriteLine ( ""hello world"" ) ; ");
        }

        [WorkItem(544907, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544907")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestExpressionTLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Linq.Expressions;

class C
{
    static void Main()
    {
        Expression<Func<int, int>> e = x => [|Foo|];
    }
}",
@"using System;
using System.Linq.Expressions;

class C
{
    public static int Foo { get; private set; }

    static void Main()
    {
        Expression<Func<int, int>> e = x => Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNoGenerationIntoEntirelyHiddenType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void Foo()
    {
        int i = D.[|Bar|];
    }
}

#line hidden
class D
{
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInReturnStatement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        return [|foo|];
    }
}",
@"class Program
{
    private object foo;

    void Main()
    {
        return foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocal1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Foo([|bar|]);
    }

    static void Foo(int i)
    {
    }
}",
@"class Program
{
    void Main()
    {
        int bar = 0;
        Foo(bar);
    }

    static void Foo(int i)
    {
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalMissingForVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var x = [|var|];
    }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOutLocal1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Foo(out [|bar|]);
    }

    static void Foo(out int i)
    {
    }
}",
@"class Program
{
    void Main()
    {
        int bar;
        Foo(out bar);
    }

    static void Foo(out int i)
    {
    }
}",
index: 1);
        }

        [WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalBeforeComment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
#if true
        // Banner Line 1
        // Banner Line 2
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
@"class Program
{
    void Main()
    {
#if true
        int local;
        // Banner Line 1
        // Banner Line 2
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
index: 1);
        }

        [WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestLocalAfterComment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
#if true
        // Banner Line 1
        // Banner Line 2

        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
@"class Program
{
    void Main()
    {
#if true
        // Banner Line 1
        // Banner Line 2
        int local;
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateIntoVisiblePortion()
        {
            await TestInRegularAndScriptAsync(
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Foo(Program.[|X|])
    }
}",
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Foo(Program.X)
    }

    public static object X { get; private set; }
}",
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingWhenNoAvailableRegionToGenerateInto()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Foo(Program.[|X|])


#line hidden
    }
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalAvailableIfBlockIsNotHidden()
        {
            await TestInRegularAndScriptAsync(
@"using System;

#line hidden
class Program
{
#line default
    void Main()
    {
        Foo([|x|]);
    }
#line hidden
}
#line default",
@"using System;

#line hidden
class Program
{
#line default
    void Main()
    {
        object x = null;
        Foo(x);
    }
#line hidden
}
#line default",
ignoreTrivia: false);
        }

        [WorkItem(545217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545217")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalNameSimplification()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void foo()
    {
        bar([|xyz|]);
    }

    struct sfoo
    {
    }

    void bar(sfoo x)
    {
    }
}",
@"class Program
{
    void foo()
    {
        sfoo xyz = default(sfoo);
        bar(xyz);
    }

    struct sfoo
    {
    }

    void bar(sfoo x)
    {
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestParenthesizedExpression()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        int v = 1 + ([|k|]);
    }
}",
@"class Program
{
    private int k;

    void Main()
    {
        int v = 1 + (k);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInSelect()
        {
            await TestInRegularAndScriptAsync(
@"using System.Linq;

class Program
{
    void Main(string[] args)
    {
        var q = from a in args
                select [|v|];
    }
}",
@"using System.Linq;

class Program
{
    private object v;

    void Main(string[] args)
    {
        var q = from a in args
                select v;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInChecked()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        int[] a = null;
        int[] temp = checked([|foo|]);
    }
}",
@"class Program
{
    private int[] foo;

    void Main()
    {
        int[] a = null;
        int[] temp = checked(foo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInArrayRankSpecifier()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var v = new int[[|k|]];
    }
}",
@"class Program
{
    private int k;

    void Main()
    {
        var v = new int[k];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInConditional1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        int i = [|foo|] ? bar : baz;
    }
}",
@"class Program
{
    private static bool foo;

    static void Main()
    {
        int i = foo ? bar : baz;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInConditional2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        int i = foo ? [|bar|] : baz;
    }
}",
@"class Program
{
    private static int bar;

    static void Main()
    {
        int i = foo ? bar : baz;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInConditional3()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        int i = foo ? bar : [|baz|];
    }
}",
@"class Program
{
    private static int baz;

    static void Main()
    {
        int i = foo ? bar : baz;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInCast()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        var x = (int)[|y|];
    }
}",
@"class Program
{
    private int y;

    void Main()
    {
        var x = (int)y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInIf()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        if ([|foo|])
        {
        }
    }
}",
@"class Program
{
    private bool foo;

    void Main()
    {
        if (foo)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInSwitch()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        switch ([|foo|])
        {
        }
    }
}",
@"class Program
{
    private int foo;

    void Main()
    {
        switch (foo)
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnNamespace()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System|].Console.WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System.Console|].WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestMissingOnBase()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|base|].ToString();
    }
}");
        }

        [WorkItem(545273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAssign1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = 1;
    }
}",
@"class Program
{
    void Main()
    {
        var undefined = 1;
    }
}",
index: 2, options: ImplicitTypingEverywhere());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestFuncAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = (x) => 2;
    }
}",
@"class Program
{
    void Main()
    {
        System.Func<object, int> undefined = (x) => 2;
    }
}",
index: 2);
        }

        [WorkItem(545273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAssign1NotAsVar()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = 1;
    }
}",
@"class Program
{
    void Main()
    {
        int undefined = 1;
    }
}",
index: 2);
        }

        [WorkItem(545273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFromAssign2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|undefined|] = new { P = ""1"" };
    }
}",
@"class Program
{
    void Main()
    {
        var undefined = new { P = ""1"" };
    }
}",
index: 2);
        }

        [WorkItem(545269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInVenus1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
#line 1 ""foo""
    void Foo()
    {
        this.[|Bar|] = 1;
    }
#line default
#line hidden
}");
        }

        [WorkItem(545269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInVenus2()
        {
            var code = @"
class C
{
#line 1 ""foo""
    void Foo()
    {
        [|Bar|] = 1;
    }
#line default
#line hidden
}
";
            await TestExactActionSetOfferedAsync(code, new[] { string.Format(FeaturesResources.Generate_local_0, "Bar") });

            await TestInRegularAndScriptAsync(code,
@"class C
{
#line 1 ""foo""
    void Foo()
    {
        var [|Bar|] = 1;
    }
#line default
#line hidden
}", options: ImplicitTypingEverywhere());
        }

        [WorkItem(546027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546027")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyFromAttribute()
        {
            await TestInRegularAndScriptAsync(
@"using System;

[AttributeUsage(AttributeTargets.Class)]
class MyAttrAttribute : Attribute
{
}

[MyAttr(123, [|Version|] = 1)]
class D
{
}",
@"using System;

[AttributeUsage(AttributeTargets.Class)]
class MyAttrAttribute : Attribute
{
    public int Version { get; set; }
}

[MyAttr(123, Version = 1)]
class D
{
}");
        }

        [WorkItem(545232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545232")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNewLinePreservationBeforeInsertingLocal()
        {
            await TestInRegularAndScriptAsync(
@"using System;
namespace CSharpDemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MEGABYTE = 1024 * 1024;
            Console.WriteLine(MEGABYTE);
 
            Calculate([|multiplier|]);
        }
        static void Calculate(double multiplier = Math.PI)
        {
        }
    }
}
",
@"using System;
namespace CSharpDemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MEGABYTE = 1024 * 1024;
            Console.WriteLine(MEGABYTE);

            double multiplier = 0;
            Calculate(multiplier);
        }
        static void Calculate(double multiplier = Math.PI)
        {
        }
    }
}
",
index: 3,
ignoreTrivia: false);
        }

        [WorkItem(863346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863346")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod_Local()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>([|NewLocal|]);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        Func<T1, T> NewLocal = null;
        T t = WrapFunc<T>(NewLocal);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
index: 3,
ignoreTrivia: false);
        }

        [WorkItem(863346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863346")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateInGenericMethod_Property()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>([|NewLocal|]);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
@"using System;
class TestClass<T1>
{
    public static Func<T1, object> NewLocal { get; private set; }

    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>(NewLocal);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
ignoreTrivia: false);
        }

        [WorkItem(865067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865067")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithYieldReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<DayOfWeek> Foo()
    {
        yield return [|abc|];
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    private DayOfWeek abc;

    IEnumerable<DayOfWeek> Foo()
    {
        yield return abc;
    }
}");
        }

        [WorkItem(877580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/877580")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithThrow()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    void Foo()
    {
        throw [|MyExp|];
    }
}",
@"using System;

class Program
{
    private Exception MyExp;

    void Foo()
    {
        throw MyExp;
    }
}", index: 1);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int* a = foo|];
    }
}",
@"class Class
{
    private unsafe int* foo;

    void Method()
    {
        int* a = foo;
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int*[] a = foo|];
    }
}",
@"class Class
{
    private unsafe int*[] foo;

    void Method()
    {
        int*[] a = foo;
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeFieldInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    void Method()
    {
        [|int* a = foo|];
    }
}",
@"unsafe class Class
{
    private int* foo;

    void Method()
    {
        int* a = foo;
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeFieldInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    class MyClass
    {
        void Method()
        {
            [|int* a = foo|];
        }
    }
}",
@"unsafe class Class
{
    class MyClass
    {
        private int* foo;

        void Method()
        {
            int* a = foo;
        }
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeFieldInNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    unsafe class MyClass
    {
        void Method()
        {
            [|int* a = Class.foo|];
        }
    }
}",
@"class Class
{
    private static unsafe int* foo;

    unsafe class MyClass
    {
        void Method()
        {
            int* a = Class.foo;
        }
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyField()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int* a = foo|];
    }
}",
@"class Class
{
    private readonly unsafe int* foo;

    void Method()
    {
        int* a = foo;
    }
}",
index: 1);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyField2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int*[] a = foo|];
    }
}",
@"class Class
{
    private readonly unsafe int*[] foo;

    void Method()
    {
        int*[] a = foo;
    }
}",
index: 1);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyFieldInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    void Method()
    {
        [|int* a = foo|];
    }
}",
@"unsafe class Class
{
    private readonly int* foo;

    void Method()
    {
        int* a = foo;
    }
}",
index: 1);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyFieldInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    class MyClass
    {
        void Method()
        {
            [|int* a = foo|];
        }
    }
}",
@"unsafe class Class
{
    class MyClass
    {
        private readonly int* foo;

        void Method()
        {
            int* a = foo;
        }
    }
}",
index: 1);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeReadOnlyFieldInNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    unsafe class MyClass
    {
        void Method()
        {
            [|int* a = Class.foo|];
        }
    }
}",
@"class Class
{
    private static readonly unsafe int* foo;

    unsafe class MyClass
    {
        void Method()
        {
            int* a = Class.foo;
        }
    }
}",
index: 1);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int* a = foo|];
    }
}",
@"class Class
{
    public unsafe int* foo { get; private set; }

    void Method()
    {
        int* a = foo;
    }
}",
index: 2);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafeProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int*[] a = foo|];
    }
}",
@"class Class
{
    public unsafe int*[] foo { get; private set; }

    void Method()
    {
        int*[] a = foo;
    }
}",
index: 2);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafePropertyInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    void Method()
    {
        [|int* a = foo|];
    }
}",
@"unsafe class Class
{
    public int* foo { get; private set; }

    void Method()
    {
        int* a = foo;
    }
}",
index: 2);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafePropertyInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"unsafe class Class
{
    class MyClass
    {
        void Method()
        {
            [|int* a = foo|];
        }
    }
}",
@"unsafe class Class
{
    class MyClass
    {
        public int* foo { get; private set; }

        void Method()
        {
            int* a = foo;
        }
    }
}",
index: 2);
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestUnsafePropertyInNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    unsafe class MyClass
    {
        void Method()
        {
            [|int* a = Class.foo|];
        }
    }
}",
@"class Class
{
    public static unsafe int* foo { get; private set; }

    unsafe class MyClass
    {
        void Method()
        {
            int* a = Class.foo;
        }
    }
}",
index: 2);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    public object Z { get; private set; }

    void M()
    {
        var x = nameof(Z);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    private object Z;

    void M()
    {
        var x = nameof(Z);
    }
}",
index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    private readonly object Z;

    void M()
    {
        var x = nameof(Z);
    }
}",
index: 2);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"class C
{
    void M()
    {
        object Z = null;
        var x = nameof(Z);
    }
}",
index: 3);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    public object Z { get; private set; }

    void M()
    {
        var x = nameof(Z.X);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    private object Z;

    void M()
    {
        var x = nameof(Z.X);
    }
}",
index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    private readonly object Z;

    void M()
    {
        var x = nameof(Z.X);
    }
}",
index: 2);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"class C
{
    void M()
    {
        object Z = null;
        var x = nameof(Z.X);
    }
}",
index: 3);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    public object Z { get; private set; }

    void M()
    {
        var x = nameof(Z.X.Y);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    private object Z;

    void M()
    {
        var x = nameof(Z.X.Y);
    }
}",
index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    private readonly object Z;

    void M()
    {
        var x = nameof(Z.X.Y);
    }
}",
index: 2);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"class C
{
    void M()
    {
        object Z = null;
        var x = nameof(Z.X.Y);
    }
}",
index: 3);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfMissing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = [|nameof(1 + 2)|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfMissing2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var y = 1 + 2;
        var x = [|nameof(y)|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfMissing3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var y = 1 + 2;
        var z = """";
        var x = [|nameof(y, z)|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    public object y { get; private set; }

    void M()
    {
        var x = nameof(y, z);
    }
}",
index: 2);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    private object y;

    void M()
    {
        var x = nameof(y, z);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    private readonly object y;

    void M()
    {
        var x = nameof(y, z);
    }
}",
index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"class C
{
    void M()
    {
        object y = null;
        var x = nameof(y, z);
    }
}",
index: 3);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfProperty5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    public object y { get; private set; }

    void M()
    {
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
index: 2);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfField5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    private object y;

    void M()
    {
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfReadonlyField5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    private readonly object y;

    void M()
    {
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
index: 1);
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestInsideNameOfLocal5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|]);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
@"class C
{
    void M()
    {
        object y = null;
        var x = nameof(y);
    }

    private object nameof(object y)
    {
        return null;
    }
}",
index: 3);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = a?[|.Instance|];
    }
}",
@"class C
{
    public C Instance { get; private set; }

    void Main(C a)
    {
        C x = a?.Instance;
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = a?[|.Instance|];
    }
}",
@"class C
{
    private C Instance;

    void Main(C a)
    {
        C x = a?.Instance;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = a?[|.Instance|];
    }
}",
@"class C
{
    private readonly C Instance;

    void Main(C a)
    {
        C x = a?.Instance;
    }
}",
index: 2);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessVarProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        var x = a?[|.Instance|];
    }
}",
@"class C
{
    public object Instance { get; private set; }

    void Main(C a)
    {
        var x = a?.Instance;
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessVarField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        var x = a?[|.Instance|];
    }
}",
@"class C
{
    private object Instance;

    void Main(C a)
    {
        var x = a?.Instance;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessVarReadOnlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        var x = a?[|.Instance|];
    }
}",
@"class C
{
    private readonly object Instance;

    void Main(C a)
    {
        var x = a?.Instance;
    }
}",
index: 2);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessNullableProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        int? x = a?[|.B|];
    }
}",
@"class C
{
    public int B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B;
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessNullableField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        int? x = a?[|.B|];
    }
}",
@"class C
{
    private int B;

    void Main(C a)
    {
        int? x = a?.B;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestConditionalAccessNullableReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        int? x = a?[|.B|];
    }
}",
@"class C
{
    private readonly int B;

    void Main(C a)
    {
        int? x = a?.B;
    }
}",
index: 2);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C;
    }

    public class E
    {
        public C C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C;
    }

    public class E
    {
        public int C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C;
    }

    public class E
    {
        public int C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInConditionalAccessExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C;
    }

    public class E
    {
        public object C { get; internal set; }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C;
    }

    public class E
    {
        internal C C;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C;
    }

    public class E
    {
        internal int C;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C;
    }

    public class E
    {
        internal int C;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInConditionalAccessExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C;
    }

    public class E
    {
        internal object C;
    }
}",
index: 1);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C;
    }

    public class E
    {
        internal readonly C C;
    }
}",
index: 2);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C;
    }

    public class E
    {
        internal readonly int C;
    }
}",
index: 2);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C;
    }

    public class E
    {
        internal readonly int C;
    }
}",
index: 2);
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInConditionalAccessExpression4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|];
    }

    public class E
    {
    }
}",
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C;
    }

    public class E
    {
        internal readonly object C;
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInPropertyInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public int MyProperty { get; } = [|y|];
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static int y;

    public int MyProperty { get; } = y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInPropertyInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public int MyProperty { get; } = [|y|];
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static readonly int y;

    public int MyProperty { get; } = y;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInPropertyInitializers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public int MyProperty { get; } = [|y|];
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public static int y { get; private set; }
    public int MyProperty { get; } = y;
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodyMember()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|];
}",
@"class Program
{
    private int y;

    public int Y => y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadonlyFieldInExpressionBodyMember()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|];
}",
@"class Program
{
    private readonly int y;

    public int Y => y;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodyMember()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|];
}",
@"class Program
{
    public int Y => y;

    public int y { get; private set; }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodyMember2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|];
}",
@"class C
{
    private static C x;

    public static C operator --(C p) => x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInExpressionBodyMember2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|];
}",
@"class C
{
    private static readonly C x;

    public static C operator --(C p) => x;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodyMember2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|];
}",
@"class C
{
    public static C x { get; private set; }

    public static C operator --(C p) => x;
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInExpressionBodyMember3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|];
}",
@"class C
{
    private static C x;

    public static C GetValue(C p) => x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInExpressionBodyMember3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|];
}",
@"class C
{
    private static readonly C x;

    public static C GetValue(C p) => x;
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInExpressionBodyMember3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|];
}",
@"class C
{
    public static C x { get; private set; }

    public static C GetValue(C p) => x;
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static string key;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    public static string One { get; private set; }

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static int i;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static readonly string key;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateFieldInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static string One;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static readonly int i;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    public static string key { get; private set; }

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyFieldInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    private static readonly string One;

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGeneratePropertyInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    public static int i { get; private set; }

    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]] = 0 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string key = null;
        var x = new Dictionary<string, int> { [key] = 0 };
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]] = 1, [""Two""] = 2 };
    }
}",
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string One = null;
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One] = 1, [""Two""] = 2 };
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateLocalInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|] };
    }
}",
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        var x = new Dictionary<string, int> { [""Zero""] = i };
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateVariableFromLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|foo|] = () => {
            return 0;
        };
    }
}",
@"using System;

class Program
{
    private static Func<int> foo;

    static void Main(string[] args)
    {
        foo = () => {
            return 0;
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateVariableFromLambda2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|foo|] = () => {
            return 0;
        };
    }
}",
@"using System;

class Program
{
    public static Func<int> foo { get; private set; }

    static void Main(string[] args)
    {
        foo = () => {
            return 0;
        };
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateVariableFromLambda3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|foo|] = () => {
            return 0;
        };
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int> foo = () => {
            return 0;
        };
    }
}",
index: 2);
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_Field()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|_field|];
        }
    }
}",
@"using System;

public class Test
{
    private static int _field;

    public static int Property1
    {
        get
        {
            return _field;
        }
    }
}");
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_ReadonlyField()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|_field|];
        }
    }
}",
@"using System;

public class Test
{
    private static readonly int _field;

    public static int Property1
    {
        get
        {
            return _field;
        }
    }
}",
index: 1);
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_Property()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|_field|];
        }
    }
}",
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return _field;
        }
    }

    public static int _field { get; private set; }
}",
index: 2);
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerationFromStaticProperty_Local()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            return [|_field|];
        }
    }
}",
@"using System;

public class Test
{
    public static int Property1
    {
        get
        {
            int _field = 0;
            return _field;
        }
    }
}",
index: 3);
        }

        [WorkItem(8358, "https://github.com/dotnet/roslyn/issues/8358")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestSameNameAsInstanceVariableInContainingType()
        {
            await TestInRegularAndScriptAsync(
@"class Outer
{
    int _field;

    class Inner
    {
        public Inner(int field)
        {
            [|_field|] = field;
        }
    }
}",
@"class Outer
{
    int _field;

    class Inner
    {
        private int _field;

        public Inner(int field)
        {
            _field = field;
        }
    }
}");
        }

        [WorkItem(8358, "https://github.com/dotnet/roslyn/issues/8358")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnStaticWithExistingInstance1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int _field;

    void M()
    {
        C.[|_field|] = 42;
    }
}");
        }

        [WorkItem(8358, "https://github.com/dotnet/roslyn/issues/8358")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnStaticWithExistingInstance2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int _field;

    static C()
    {
        [|_field|] = 42;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleRead()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method((int, string) i)
    {
        Method([|tuple|]);
    }
}",
@"class Class
{
    private (int, string) tuple;

    void Method((int, string) i)
    {
        Method(tuple);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleWithOneNameRead()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method((int a, string) i)
    {
        Method([|tuple|]);
    }
}",
@"class Class
{
    private (int a, string) tuple;

    void Method((int a, string) i)
    {
        Method(tuple);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleWrite()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|tuple|] = (1, ""hello"");
    }
}",
@"class Class
{
    private (int, string) tuple;

    void Method()
    {
        tuple = (1, ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleWithOneNameWrite()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|tuple|] = (a: 1, ""hello"");
    }
}",
@"class Class
{
    private (int a, string) tuple;

    void Method()
    {
        tuple = (a: 1, ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleRefReturnProperties()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C
{
    public void Foo()
    {
        ref int i = ref this.[|Bar|];
    }
}",
@"
using System;
class C
{
    public ref int Bar => throw new NotImplementedException();

    public void Foo()
    {
        ref int i = ref this.Bar;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TupleRefWithField()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C
{
    public void Foo()
    {
        ref int i = ref this.[|bar|];
    }
}",
@"
using System;
class C
{
    private int bar;

    public void Foo()
    {
        ref int i = ref this.bar;
    }
}");
        }

        [WorkItem(17621, "https://github.com/dotnet/roslyn/issues/17621")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithMatchingTypeName1()
        {
            await TestInRegularAndScript1Async(
@"
using System;

public class Foo
{
    public Foo(String foo)
    {
        [|String|] = foo;
    }
}",
@"using System;

public class Foo
{
    public Foo(String foo)
    {
        String = foo;
    }

    public string String { get; }
}");
        }

        [WorkItem(17621, "https://github.com/dotnet/roslyn/issues/17621")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithMatchingTypeName2()
        {
            await TestInRegularAndScript1Async(
@"
using System;

public class Foo
{
    public Foo(String foo)
    {
        [|String|] = foo;
    }
}",
@"using System;

public class Foo
{
    public Foo(String foo)
    {
        String = foo;
    }

    public string String { get; private set; }
}", index: 1);
        }

        [WorkItem(18275, "https://github.com/dotnet/roslyn/issues/18275")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestContextualKeyword1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
namespace N
{
    class nameof
    {
    }
}

class C
{
    void M()
    {
        [|nameof|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPreferReadOnlyIfAfterReadOnlyAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private readonly int _foo;

    public Class()
    {
        _foo = 0;
        [|_bar|] = 1;
    }
}",
@"class Class
{
    private readonly int _foo;
    private readonly int _bar;

    public Class()
    {
        _foo = 0;
        _bar = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPreferReadOnlyIfBeforeReadOnlyAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private readonly int _foo;

    public Class()
    {
        [|_bar|] = 1;
        _foo = 0;
    }
}",
@"class Class
{
    private readonly int _bar;
    private readonly int _foo;

    public Class()
    {
        _bar = 1;
        _foo = 0;
    }
}");
        }

        [WorkItem(19239, "https://github.com/dotnet/roslyn/issues/19239")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestGenerateReadOnlyPropertyInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    public Class()
    {
        [|Bar|] = 1;
    }
}",
@"class Class
{
    public Class()
    {
        Bar = 1;
    }

    public int Bar { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPlaceFieldBasedOnSurroundingStatements()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private int _foo;
    private int _quux;

    public Class()
    {
        _foo = 0;
        [|_bar|] = 1;
        _quux = 2;
    }
}",
@"class Class
{
    private int _foo;
    private int _bar;
    private int _quux;

    public Class()
    {
        _foo = 0;
        _bar = 1;
        _quux = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPlaceFieldBasedOnSurroundingStatements2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    private int foo;
    private int quux;

    public Class()
    {
        this.foo = 0;
        this.[|bar|] = 1;
        this.quux = 2;
    }
}",
@"class Class
{
    private int foo;
    private int bar;
    private int quux;

    public Class()
    {
        this.foo = 0;
        this.bar = 1;
        this.quux = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestPlacePropertyBasedOnSurroundingStatements()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    public int Foo { get; }
    public int Quuz { get; }

    public Class()
    {
        Foo = 0;
        [|Bar|] = 1;
        Quux = 2;
    }
}",
@"class Class
{
    public int Foo { get; }
    public int Bar { get; }
    public int Quuz { get; }

    public Class()
    {
        Foo = 0;
        Bar = 1;
        Quux = 2;
    }
}");
        }

        [WorkItem(19575, "https://github.com/dotnet/roslyn/issues/19575")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestNotOnGenericCodeParsedAsExpression()
        {
            await TestMissingAsync(@"
class C
{
    private void GetEvaluationRuleNames()
    {
        [|IEnumerable|] < Int32 >
        return ImmutableArray.CreateRange();
    }
}");
        }

        [WorkItem(19575, "https://github.com/dotnet/roslyn/issues/19575")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestOnNonGenericExpressionWithLessThan()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    private void GetEvaluationRuleNames()
    {
        [|IEnumerable|] < Int32
        return ImmutableArray.CreateRange();
    }
}",
@"
class C
{
    public int IEnumerable { get; private set; }

    private void GetEvaluationRuleNames()
    {
        IEnumerable < Int32
        return ImmutableArray.CreateRange();
    }
}");
        }

        [WorkItem(18988, "https://github.com/dotnet/roslyn/issues/18988")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task GroupNonReadonlyFieldsTogether()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public bool isDisposed;

    public readonly int x;
    public readonly int m;

    public C()
    {
        this.[|y|] = 0;
    }
}",
@"
class C
{
    public bool isDisposed;
    private int y;

    public readonly int x;
    public readonly int m;

    public C()
    {
        this.y = 0;
    }
}");
        }

        [WorkItem(18988, "https://github.com/dotnet/roslyn/issues/18988")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task GroupReadonlyFieldsTogether()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public readonly int x;
    public readonly int m;

    public bool isDisposed;

    public C()
    {
        this.[|y|] = 0;
    }
}",
@"
class C
{
    public readonly int x;
    public readonly int m;
    private readonly int y;

    public bool isDisposed;

    public C()
    {
        this.y = 0;
    }
}", index: 1);
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithOutOverload1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Foo(out [|foo|]);
    }

    void Foo(int i) { }
    void Foo(out bool b) { }
}",
@"class Class
{
    private bool foo;

    void Method()
    {
        Foo(out foo);
    }

    void Foo(int i) { }
    void Foo(out bool b) { }
}");
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithOutOverload2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Foo([|foo|]);
    }

    void Foo(out bool b) { }
    void Foo(int i) { }
}",
@"class Class
{
    private int foo;

    void Method()
    {
        Foo(foo);
    }

    void Foo(out bool b) { }
    void Foo(int i) { }
}");
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithRefOverload1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Foo(ref [|foo|]);
    }

    void Foo(int i) { }
    void Foo(ref bool b) { }
}",
@"class Class
{
    private bool foo;

    void Method()
    {
        Foo(ref foo);
    }

    void Foo(int i) { }
    void Foo(ref bool b) { }
}");
        }

        [WorkItem(20791, "https://github.com/dotnet/roslyn/issues/20791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public async Task TestWithRefOverload2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Foo([|foo|]);
    }

    void Foo(ref bool b) { }
    void Foo(int i) { }
}",
@"class Class
{
    private int foo;

    void Method()
    {
        Foo(foo);
    }

    void Foo(ref bool b) { }
    void Foo(int i) { }
}");
        }
    }
}
