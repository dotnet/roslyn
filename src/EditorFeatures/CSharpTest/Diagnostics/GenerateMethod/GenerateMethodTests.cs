﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateMethod
{
    public class GenerateMethodTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new GenerateMethodCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationIntoSameType()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo();
    }

    private void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationIntoSameType_CodeStyle1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo();
    }

    private void Goo() => throw new NotImplementedException();
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(11518, "https://github.com/dotnet/roslyn/issues/11518")]
        public async Task NameMatchesNamespaceName()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    class Class
    {
        void Method()
        {
            [|N|]();
        }
    }
}",
@"using System;

namespace N
{
    class Class
    {
        void Method()
        {
            N();
        }

        private void N()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationOffOfThis()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        this.[|Goo|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        this.Goo();
    }

    private void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationOffOfType()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Class.[|Goo|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Class.Goo();
    }

    private static void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationValueExpressionArg()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|](0);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo(0);
    }

    private void Goo(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationMultipleValueExpressionArg()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|](0, 0);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo(0, 0);
    }

    private void Goo(int v1, int v2)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationValueArg()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](i);
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(i);
    }

    private void Goo(int i)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleInvocationNamedValueArg()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](bar: i);
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(bar: i);
    }

    private void Goo(int bar)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateAfterMethod()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]();
    }

    void NextMethod()
    {
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo();
    }

    private void Goo()
    {
        throw new NotImplementedException();
    }

    void NextMethod()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInterfaceNaming()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](NextMethod());
    }

    IGoo NextMethod()
    {
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(NextMethod());
    }

    private void Goo(IGoo goo)
    {
        throw new NotImplementedException();
    }

    IGoo NextMethod()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestFuncArg0()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](NextMethod);
    }

    string NextMethod()
    {
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(NextMethod);
    }

    private void Goo(Func<string> nextMethod)
    {
        throw new NotImplementedException();
    }

    string NextMethod()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestFuncArg1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](NextMethod);
    }

    string NextMethod(int i)
    {
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(NextMethod);
    }

    private void Goo(Func<int, string> nextMethod)
    {
        throw new NotImplementedException();
    }

    string NextMethod(int i)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestActionArg()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](NextMethod);
    }

    void NextMethod()
    {
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(NextMethod);
    }

    private void Goo(Action nextMethod)
    {
        throw new NotImplementedException();
    }

    void NextMethod()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestActionArg1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        [|Goo|](NextMethod);
    }

    void NextMethod(int i)
    {
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        Goo(NextMethod);
    }

    private void Goo(Action<int> nextMethod)
    {
        throw new NotImplementedException();
    }

    void NextMethod(int i)
    {
    }
}");
        }

        // Note: we only test type inference once.  This is just to verify that it's being used
        // properly by Generate Method.  The full wealth of type inference tests can be found
        // elsewhere and don't need to be repeated here.
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestTypeInference()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        if ([|Goo|]())
        {
        }
    }
}",
@"using System;

class Class
{
    void Method()
    {
        if (Goo())
        {
        }
    }

    private bool Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestOutRefArguments()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|](out a, ref b);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo(out a, ref b);
    }

    private void Goo(out object a, ref object b)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMemberAccessArgumentName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|](this.Bar);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo(this.Bar);
    }

    private void Goo(object bar)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestParenthesizedArgumentName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]((Bar));
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo((Bar));
    }

    private void Goo(object bar)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestCastedArgumentName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]((Bar)this.Baz);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo((Bar)this.Baz);
    }

    private void Goo(Bar baz)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNullableArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        [|Goo|]((int?)1);
    }
}",
@"using System;

class C
{
    void Method()
    {
        Goo((int?)1);
    }

    private void Goo(int? v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNullArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        [|Goo|](null);
    }
}",
@"using System;

class C
{
    void Method()
    {
        Goo(null);
    }

    private void Goo(object p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestTypeofArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        [|Goo|](typeof(int));
    }
}",
@"using System;

class C
{
    void Method()
    {
        Goo(typeof(int));
    }

    private void Goo(Type type)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDefaultArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        [|Goo|](default(int));
    }
}",
@"using System;

class C
{
    void Method()
    {
        Goo(default(int));
    }

    private void Goo(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestAsArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        [|Goo|](1 as int?);
    }
}",
@"using System;

class C
{
    void Method()
    {
        Goo(1 as int?);
    }

    private void Goo(int? v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestPointArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        int* p;
        [|Goo|](p);
    }
}",
@"using System;

class C
{
    void Method()
    {
        int* p;
        Goo(p);
    }

    private unsafe void Goo(int* p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestArgumentWithPointerName()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        int* p;
        [|Goo|](p);
    }
}",
@"using System;

class C
{
    void Method()
    {
        int* p;
        Goo(p);
    }

    private unsafe void Goo(int* p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestArgumentWithPointTo()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        int* p;
        [|Goo|](*p);
    }
}",
@"using System;

class C
{
    void Method()
    {
        int* p;
        Goo(*p);
    }

    private void Goo(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestArgumentWithAddress()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    unsafe void Method()
    {
        int a = 10;
        [|Goo|](&a);
    }
}",
@"using System;

class C
{
    unsafe void Method()
    {
        int a = 10;
        Goo(&a);
    }

    private unsafe void Goo(int* v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateWithPointerReturn()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Method()
    {
        int* p = [|Goo|]();
    }
}",
@"using System;

class C
{
    void Method()
    {
        int* p = Goo();
    }

    private unsafe int* Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDuplicateNames()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]((Bar)this.Baz, this.Baz);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo((Bar)this.Baz, this.Baz);
    }

    private void Goo(Bar baz1, object baz2)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDuplicateNamesWithNamedArgument()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|]((Bar)this.Baz, this.Baz, baz: this.Baz);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo((Bar)this.Baz, this.Baz, baz: this.Baz);
    }

    private void Goo(Bar baz1, object baz2, object baz)
    {
        throw new NotImplementedException();
    }
}");
        }

        // Note: we do not test the range of places where a delegate type can be inferred.  This is
        // just to verify that it's being used properly by Generate Method.  The full wealth of
        // delegate inference tests can be found elsewhere and don't need to be repeated here.
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimpleDelegate()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method()
    {
        Func<int, string, bool> f = [|Goo|];
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Func<int, string, bool> f = Goo;
    }

    private bool Goo(int arg1, string arg2)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDelegateWithRefParameter()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        Goo f = [|Bar|];
    }
}

delegate void Goo(ref int i);",
@"using System;

class Class
{
    void Method()
    {
        Goo f = Bar;
    }

    private void Bar(ref int i)
    {
        throw new NotImplementedException();
    }
}

delegate void Goo(ref int i);");
        }

        // TODO(cyrusn): Add delegate tests that cover delegates with interesting signatures (i.e.
        // out/ref).
        //
        // add negative tests to verify that Generate Method doesn't show up in unexpected places.

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenericArgs1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method()
    {
        [|Goo<int>|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo<int>();
    }

    private void Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenericArgs2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method()
    {
        [|Goo<int, string>|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Goo<int, string>();
    }

    private void Goo<T1, T2>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenericArgsFromMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method<X, Y>(X x, Y y)
    {
        [|Goo|](x);
    }
}",
@"using System;

class Class
{
    void Method<X, Y>(X x, Y y)
    {
        Goo(x);
    }

    private void Goo<X>(X x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMultipleGenericArgsFromMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method<X, Y>(X x, Y y)
    {
        [|Goo|](x, y);
    }
}",
@"using System;

class Class
{
    void Method<X, Y>(X x, Y y)
    {
        Goo(x, y);
    }

    private void Goo<X, Y>(X x, Y y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMultipleGenericArgsFromMethod2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method<X, Y>(Func<X> x, Y[] y)
    {
        [|Goo|](y, x);
    }
}",
@"using System;

class Class
{
    void Method<X, Y>(Func<X> x, Y[] y)
    {
        Goo(y, x);
    }

    private void Goo<Y, X>(Y[] y, Func<X> x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenericArgThatIsTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main<T>(T t)
    {
        [|Goo<T>|](t);
    }
}",
@"using System;

class Program
{
    void Main<T>(T t)
    {
        Goo<T>(t);
    }

    private void Goo<T>(T t)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMultipleGenericArgsThatAreTypeParameters()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main<T, U>(T t, U u)
    {
        [|Goo<T, U>|](t, u);
    }
}",
@"using System;

class Program
{
    void Main<T, U>(T t, U u)
    {
        Goo<T, U>(t, u);
    }

    private void Goo<T, U>(T t, U u)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoOuterThroughInstance()
        {
            await TestInRegularAndScriptAsync(
@"class Outer
{
    class Class
    {
        void Method(Outer o)
        {
            o.[|Goo|]();
        }
    }
}",
@"using System;

class Outer
{
    class Class
    {
        void Method(Outer o)
        {
            o.Goo();
        }
    }

    private void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoOuterThroughClass()
        {
            await TestInRegularAndScriptAsync(
@"class Outer
{
    class Class
    {
        void Method(Outer o)
        {
            Outer.[|Goo|]();
        }
    }
}",
@"using System;

class Outer
{
    class Class
    {
        void Method(Outer o)
        {
            Outer.Goo();
        }
    }

    private static void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoSiblingThroughInstance()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(Sibling s)
    {
        s.[|Goo|]();
    }
}

class Sibling
{
}",
@"using System;

class Class
{
    void Method(Sibling s)
    {
        s.Goo();
    }
}

class Sibling
{
    internal void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoSiblingThroughClass()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(Sibling s)
    {
        Sibling.[|Goo|]();
    }
}

class Sibling
{
}",
@"using System;

class Class
{
    void Method(Sibling s)
    {
        Sibling.Goo();
    }
}

class Sibling
{
    internal static void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoInterfaceThroughInstance()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(ISibling s)
    {
        s.[|Goo|]();
    }
}

interface ISibling
{
}",
@"class Class
{
    void Method(ISibling s)
    {
        s.Goo();
    }
}

interface ISibling
{
    void Goo();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoInterfaceThroughInstanceWithDelegate()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    void Method(ISibling s)
    {
        Func<int, string> f = s.[|Goo|];
    }
}

interface ISibling
{
}",
@"using System;

class Class
{
    void Method(ISibling s)
    {
        Func<int, string> f = s.Goo;
    }
}

interface ISibling
{
    string Goo(int arg);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateAbstractIntoSameType()
        {
            await TestInRegularAndScriptAsync(
@"abstract class Class
{
    void Method()
    {
        [|Goo|]();
    }
}",
@"abstract class Class
{
    void Method()
    {
        Goo();
    }

    internal abstract void Goo();
}",
index: 1);
        }

        [WorkItem(537906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537906")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMethodReturningDynamic()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        dynamic d = [|Goo|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        dynamic d = Goo();
    }

    private dynamic Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(537906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537906")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMethodTakingDynamicArg()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(dynamic d)
    {
        [|Goo|](d);
    }
}",
@"using System;

class Class
{
    void Method(dynamic d)
    {
        Goo(d);
    }

    private void Goo(dynamic d)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(3203, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNegativeWithNamedOptionalArg1()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace SyntaxError
{
    class C1
    {
        public void Method(int num, string str)
        {
        }
    }

    class C2
    {
        static void Method2()
        {
            (new C1()).[|Method|](num: 5, ""hi"");
        }
    }
}");
        }

        [WorkItem(537972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537972")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestWithNamedOptionalArg2()
        {
            await TestInRegularAndScriptAsync(
@"namespace SyntaxError
{
    class C1
    {
        void Method(int num, string str)
        {
        }
    }

    class C2
    {
        static void Method2()
        {
            (new C1()).[|Method|](num: 5, ""hi"");
        }
    }
}",
@"using System;

namespace SyntaxError
{
    class C1
    {
        void Method(int num, string str)
        {
        }

        internal void Method(int num, string v)
        {
            throw new NotImplementedException();
        }
    }

    class C2
    {
        static void Method2()
        {
            (new C1()).Method(num: 5, ""hi"");
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestArgOrderInNamedArgs()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
    static void Test()
    {
        (new Goo()).[|Method|](3, 4, n1: 5, n3: 6, n2: 7, n0: 8);
    }
}",
@"using System;

class Goo
{
    static void Test()
    {
        (new Goo()).Method(3, 4, n1: 5, n3: 6, n2: 7, n0: 8);
    }

    private void Method(int v1, int v2, int n1, int n3, int n2, int n0)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestForMissingOptionalArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Goo
{
    static void Test()
    {
        (new Goo()).[|Method|](s: ""hello"", b: true);
    }

    private void Method(double n = 3.14, string s, bool b)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNamingOfArgWithClashes()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
    static int i = 32;

    static void Test()
    {
        (new Goo()).[|Method|](s: ""hello"", i: 52);
    }
}",
@"using System;

class Goo
{
    static int i = 32;

    static void Test()
    {
        (new Goo()).Method(s: ""hello"", i: 52);
    }

    private void Method(string s, int i)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestFixCountGeneratingIntoInterface()
        {
            await TestActionCountAsync(
@"interface I2
{
}

class C2 : I2
{
    public void Meth()
    {
        I2 i = (I2)this;
        i.[|M|]();
    }
}",
count: 1);
        }

        [WorkItem(527278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527278")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationOffOfBase()
        {
            await TestInRegularAndScriptAsync(
@"class C3A
{
}

class C3 : C3A
{
    public void C4()
    {
        base.[|M|]();
    }
}",
@"using System;

class C3A
{
    internal void M()
    {
        throw new NotImplementedException();
    }
}

class C3 : C3A
{
    public void C4()
    {
        base.M();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationWithinCtor()
        {
            await TestInRegularAndScriptAsync(
@"class C1
{
    C1()
    {
        [|M|]();
    }
}",
@"using System;

class C1
{
    C1()
    {
        M();
    }

    private void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationWithinBaseCtor()
        {
            await TestInRegularAndScriptAsync(
@"class C1
{
    C1()
    {
        [|M|]();
    }
}",
@"using System;

class C1
{
    C1()
    {
        M();
    }

    private void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(3095, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestForMultipleSmartTagsInvokingWithinCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C1
{
    C1()
    {
        [|M|]();
    }

    private void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationWithinDestructor()
        {
            await TestInRegularAndScriptAsync(
@"class C1
{
    ~C1()
    {
        [|M|]();
    }
}",
@"using System;

class C1
{
    ~C1()
    {
        M();
    }

    private void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationWithinConditional()
        {
            await TestInRegularAndScriptAsync(
@"class C4
{
    void A()
    {
        string s;
        if ((s = [|M|]()) == null)
        {
        }
    }
}",
@"using System;

class C4
{
    void A()
    {
        string s;
        if ((s = M()) == null)
        {
        }
    }

    private string M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoStaticClass()
        {
            await TestInRegularAndScriptAsync(
@"class Bar
{
    void Test()
    {
        Goo.[|M|]();
    }
}

static class Goo
{
}",
@"using System;

class Bar
{
    void Test()
    {
        Goo.M();
    }
}

static class Goo
{
    internal static void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoAbstractClass()
        {
            await TestInRegularAndScriptAsync(
@"class Bar
{
    void Test()
    {
        Goo.[|M|]();
    }
}

abstract class Goo
{
}",
@"using System;

class Bar
{
    void Test()
    {
        Goo.M();
    }
}

abstract class Goo
{
    internal static void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoAbstractClassThoughInstance1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Test(Goo f)
    {
        f.[|M|]();
    }
}

abstract class Goo
{
}",
@"using System;

class C
{
    void Test(Goo f)
    {
        f.M();
    }
}

abstract class Goo
{
    internal void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoAbstractClassThoughInstance2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Test(Goo f)
    {
        f.[|M|]();
    }
}

abstract class Goo
{
}",
@"class C
{
    void Test(Goo f)
    {
        f.M();
    }
}

abstract class Goo
{
    internal abstract void M();
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoPartialClass1()
        {
            await TestInRegularAndScriptAsync(
@"class Bar
{
    void Test()
    {
        Goo.[|M|]();
    }
}

partial class Goo
{
}

partial class Goo
{
}",
@"using System;

class Bar
{
    void Test()
    {
        Goo.M();
    }
}

partial class Goo
{
    internal static void M()
    {
        throw new NotImplementedException();
    }
}

partial class Goo
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoPartialClass2()
        {
            await TestInRegularAndScriptAsync(
@"partial class Goo
{
    void Test()
    {
        Goo.[|M|]();
    }
}

partial class Goo
{
}",
@"using System;

partial class Goo
{
    void Test()
    {
        Goo.M();
    }

    private static void M()
    {
        throw new NotImplementedException();
    }
}

partial class Goo
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoStruct()
        {
            await TestInRegularAndScriptAsync(
@"class Goo
{
    void Test()
    {
        (new S()).[|M|]();
    }
}

struct S
{
}",
@"using System;

class Goo
{
    void Test()
    {
        (new S()).M();
    }
}

struct S
{
    internal void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(527291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationOffOfIndexer()
        {
            await TestInRegularAndScriptAsync(
@"class Bar
{
    Goo f = new Goo();

    void Test()
    {
        this[1].[|M|]();
    }

    Goo this[int i]
    {
        get
        {
            return f;
        }

        set
        {
            f = value;
        }
    }
}

class Goo
{
}",
@"using System;

class Bar
{
    Goo f = new Goo();

    void Test()
    {
        this[1].M();
    }

    Goo this[int i]
    {
        get
        {
            return f;
        }

        set
        {
            f = value;
        }
    }
}

class Goo
{
    internal void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(527292, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527292")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationWithinForEach()
        {
            await TestInRegularAndScriptAsync(
@"class C8
{
    C8A[] items = {
        new C8A(),
        new C8A()
    };

    public IEnumerable GetItems()
    {
        for (int i = items.Length - 1; i >= 0; --i)
        {
            yield return items[i];
        }
    }

    void Test()
    {
        foreach (C8A c8a in this.GetItems())
        {
            c8a.[|M|]();
        }
    }
}

class C8A
{
}",
@"using System;

class C8
{
    C8A[] items = {
        new C8A(),
        new C8A()
    };

    public IEnumerable GetItems()
    {
        for (int i = items.Length - 1; i >= 0; --i)
        {
            yield return items[i];
        }
    }

    void Test()
    {
        foreach (C8A c8a in this.GetItems())
        {
            c8a.M();
        }
    }
}

class C8A
{
    internal void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationOffOfAnotherMethodCall()
        {
            await TestInRegularAndScriptAsync(
@"class C9
{
    C9A m_item = new C9A();

    C9A GetItem()
    {
        return m_item;
    }

    void Test()
    {
        GetItem().[|M|]();
    }
}

struct C9A
{
}",
@"using System;

class C9
{
    C9A m_item = new C9A();

    C9A GetItem()
    {
        return m_item;
    }

    void Test()
    {
        GetItem().M();
    }
}

struct C9A
{
    internal void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationIntoNestedNamespaces()
        {
            await TestInRegularAndScriptAsync(
@"namespace NS11X
{
    namespace NS11Y
    {
        class C11
        {
            void Test()
            {
                NS11A.NS11B.C11A.[|M|]();
            }
        }
    }
}

namespace NS11A
{
    namespace NS11B
    {
        class C11A
        {
        }
    }
}",
@"using System;

namespace NS11X
{
    namespace NS11Y
    {
        class C11
        {
            void Test()
            {
                NS11A.NS11B.C11A.M();
            }
        }
    }
}

namespace NS11A
{
    namespace NS11B
    {
        class C11A
        {
            internal static void M()
            {
                throw new NotImplementedException();
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationIntoAliasedNamespaces()
        {
            await TestInRegularAndScriptAsync(
@"namespace NS11X
{
    using NS = NS11A.NS11B;

    class C11
    {
        void Test()
        {
            NS.C11A.[|M|]();
        }
    }

    namespace NS11A
    {
        namespace NS11B
        {
            class C11A
            {
            }
        }
    }
}",
@"namespace NS11X
{
    using System;
    using NS = NS11A.NS11B;

    class C11
    {
        void Test()
        {
            NS.C11A.M();
        }
    }

    namespace NS11A
    {
        namespace NS11B
        {
            class C11A
            {
                internal static void M()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInvocationOnGlobalNamespace()
        {
            await TestInRegularAndScriptAsync(
@"namespace NS13X
{
    namespace NS13A
    {
        namespace NS13B
        {
            struct S13B
            {
            }
        }
    }

    class C13
    {
        void Test()
        {
            global::NS13A.NS13B.S13A.[|M|]();
        }
    }
}

namespace NS13A
{
    namespace NS13B
    {
        struct S13A
        {
        }
    }
}",
@"using System;

namespace NS13X
{
    namespace NS13A
    {
        namespace NS13B
        {
            struct S13B
            {
            }
        }
    }

    class C13
    {
        void Test()
        {
            global::NS13A.NS13B.S13A.M();
        }
    }
}

namespace NS13A
{
    namespace NS13B
    {
        struct S13A
        {
            internal static void M()
            {
                throw new NotImplementedException();
            }
        }
    }
}");
        }

        [WorkItem(538353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538353")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoAppropriatePart()
        {
            await TestInRegularAndScriptAsync(
@"public partial class C
{
}

public partial class C
{
    void Method()
    {
        [|Test|]();
    }
}",
@"using System;

public partial class C
{
}

public partial class C
{
    void Method()
    {
        Test();
    }

    private void Test()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(538541, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538541")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateWithVoidArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void VoidMethod()
    {
    }

    void Method()
    {
        [|Test|](VoidMethod());
    }
}",
@"using System;

class C
{
    void VoidMethod()
    {
    }

    void Method()
    {
        Test(VoidMethod());
    }

    private void Test(object v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(538993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538993")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateInLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = x => [|Goo|](x);
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int, int> f = x => Goo(x);
    }

    private static int Goo(int x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateInAnonymousMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        System.Action<int> v = delegate (int x) {
            x = [|Goo|](x);
        };
    }
}",
@"using System;

class C
{
    void M()
    {
        System.Action<int> v = delegate (int x) {
            x = Goo(x);
        };
    }

    private int Goo(int x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface1()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
}

class A : I
{
    [|void I.Goo()
    {
    }|]
}",
@"interface I
{
    void Goo();
}

class A : I
{
    void I.Goo()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface2()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
}

class A : I
{
    [|int I.Goo()
    {
    }|]
}",
@"interface I
{
    int Goo();
}

class A : I
{
    int I.Goo()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface3()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
}

class A : I
{
    [|void I.Goo(int i)
    {
    }|]
}",
@"interface I
{
    void Goo(int i);
}

class A : I
{
    void I.Goo(int i)
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface4()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
}

class A : I
{
    void I.[|Goo|]<T>()
    {
    }
}",
@"interface I
{
    void Goo<T>();
}

class A : I
{
    void I.Goo<T>()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface5()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
}

class A : I
{
    void I.[|Goo|]<in T>()
    {
    }
}",
@"interface I
{
    void Goo<T>();
}

class A : I
{
    void I.Goo<in T>()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface6()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
    void Goo();
}

class A : I
{
    void I.[|Goo|]()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface7()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
}

class A
{
    void I.[|Goo|]()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface8()
        {
            await TestInRegularAndScriptAsync(
@"interface I<T>
{
}

class A : I<int>
{
    void I<int>.[|Goo|]()
    {
    }
}",
@"interface I<T>
{
    void Goo();
}

class A : I<int>
{
    void I<int>.Goo()
    {
    }
}");
        }

        [WorkItem(539024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539024")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOffOfExplicitInterface9()
        {
            // TODO(cyrusn): It might be nice if we generated "Goo(T i)" here in the future.
            await TestInRegularAndScriptAsync(
@"interface I<T>
{
}

class A : I<int>
{
    void I<int>.[|Goo|](int i)
    {
    }
}",
@"interface I<T>
{
    void Goo(int i);
}

class A : I<int>
{
    void I<int>.Goo(int i)
    {
    }
}");
        }

        [WorkItem(5016, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithArgumentFromBaseConstructorsArgument()
        {
            await TestInRegularAndScriptAsync(
@"class A
{
    public A(string s)
    {
    }
}

class B : A
{
    B(string s) : base([|M|](s))
    {
    }
}",
@"using System;

class A
{
    public A(string s)
    {
    }
}

class B : A
{
    B(string s) : base(M(s))
    {
    }

    private static string M(string s)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(5016, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithArgumentFromGenericConstructorsArgument()
        {
            await TestInRegularAndScriptAsync(
@"class A<T>
{
    public A(T t)
    {
    }
}

class B : A<int>
{
    B() : base([|M|]())
    {
    }
}",
@"using System;

class A<T>
{
    public A(T t)
    {
    }
}

class B : A<int>
{
    B() : base(M())
    {
    }

    private static int M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithVar()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var v = 10;
        v = [|Goo|](v);
    }
}",
@"using System;

class C
{
    void M()
    {
        var v = 10;
        v = Goo(v);
    }

    private int Goo(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestEscapedName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|@Goo|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        @Goo();
    }

    private void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestEscapedKeyword()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|@int|]();
    }
}",
@"using System;

class Class
{
    void Method()
    {
        @int();
    }

    private void @int()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539527")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter1()
        {
            await TestInRegularAndScriptAsync(
@"class Class<A>
{
    void Method(A a)
    {
        B.[|C|](a);
    }
}

class B
{
}",
@"using System;

class Class<A>
{
    void Method(A a)
    {
        B.C(a);
    }
}

class B
{
    internal static void C<A>(A a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539527")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter2()
        {
            await TestInRegularAndScriptAsync(
@"class Class<A>
{
    void Method(A a)
    {
        [|C|](a);
    }
}",
@"using System;

class Class<A>
{
    void Method(A a)
    {
        C(a);
    }

    private void C(A a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539527")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter3()
        {
            await TestInRegularAndScriptAsync(
@"class Class<A>
{
    class Internal
    {
        void Method(A a)
        {
            [|C|](a);
        }
    }
}",
@"using System;

class Class<A>
{
    class Internal
    {
        void Method(A a)
        {
            C(a);
        }

        private void C(A a)
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(539527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539527")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter4()
        {
            await TestInRegularAndScriptAsync(
@"class Class<A>
{
    class Internal
    {
        void Method(Class<A> c, A a)
        {
            c.[|M|](a);
        }
    }
}",
@"using System;

class Class<A>
{
    class Internal
    {
        void Method(Class<A> c, A a)
        {
            c.M(a);
        }
    }

    private void M(A a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539527")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter5()
        {
            await TestInRegularAndScriptAsync(
@"class Class<A>
{
    class Internal
    {
        void Method(Class<int> c, A a)
        {
            c.[|M|](a);
        }
    }
}",
@"using System;

class Class<A>
{
    class Internal
    {
        void Method(Class<int> c, A a)
        {
            c.M(a);
        }
    }

    private void M(A a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539596")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter6()
        {
            await TestInRegularAndScriptAsync(
@"class Test
{
    void F<U, V>(U u1, V v1)
    {
        [|Goo<int, string>|](u1, v1);
    }
}",
@"using System;

class Test
{
    void F<U, V>(U u1, V v1)
    {
        Goo<int, string>(u1, v1);
    }

    private void Goo<T1, T2>(object u1, object v1)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539593")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter7()
        {
            await TestInRegularAndScriptAsync(
@"class H<T>
{
    void A(T t1)
    {
        t1 = [|Goo<T>|](t1);
    }
}",
@"using System;

class H<T>
{
    void A(T t1)
    {
        t1 = Goo<T>(t1);
    }

    private T1 Goo<T1>(T1 t1)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539593")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestUnmentionableTypeParameter8()
        {
            await TestInRegularAndScriptAsync(
@"class H<T1, T2>
{
    void A(T1 t1)
    {
        t1 = [|Goo<int, string>|](t1);
    }
}",
@"using System;

class H<T1, T2>
{
    void A(T1 t1)
    {
        t1 = Goo<int, string>(t1);
    }

    private T1 Goo<T3, T4>(T1 t1)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539597")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestOddErrorType()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        @public c = [|F|]();
    }
}",
@"using System;

public class C
{
    void M()
    {
        @public c = F();
    }

    private @public F()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539594, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539594")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenericOverloads()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public C()
    {
        CA.[|M<char, bool>|]();
    }
}

class CA
{
    public static void M<V>()
    {
    }

    public static void M<V, W, X>()
    {
    }
}",
@"using System;

class C
{
    public C()
    {
        CA.M<char, bool>();
    }
}

class CA
{
    public static void M<V>()
    {
    }

    public static void M<V, W, X>()
    {
    }

    internal static void M<T1, T2>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(537929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537929")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInScript1()
        {
            await TestAsync(
@"using System;

static void Main(string[] args)
{
    [|Goo|]();
}",
@"using System;

static void Main(string[] args)
{
    Goo();
}

void Goo()
{
    throw new NotImplementedException();
}",
parseOptions: GetScriptOptions());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInTopLevelImplicitClass1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

static void Main(string[] args)
{
    [|Goo|]();
}",
@"using System;

static void Main(string[] args)
{
    Goo();
}

void Goo()
{
    throw new NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInNamespaceImplicitClass1()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    using System;

    static void Main(string[] args)
    {
        [|Goo|]();
    }
}",
@"namespace N
{
    using System;

    static void Main(string[] args)
    {
        Goo();
    }

    void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInNamespaceImplicitClass_FieldInitializer()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    using System;

    int f = [|Goo|]();
}",
@"namespace N
{
    using System;

    int f = Goo();

    int Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539571")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimplification1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        [|Bar|]();
    }

    private static void Goo()
    {
        throw new System.NotImplementedException();
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Bar();
    }

    private static void Bar()
    {
        throw new NotImplementedException();
    }

    private static void Goo()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539571")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestSimplification2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        System.Action a = [|Bar|](DateTime.Now);
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        System.Action a = Bar(DateTime.Now);
    }

    private static Action Bar(DateTime now)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539618")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestClashesWithMethod1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    void Main()
    {
        [|Goo|](x: 1, true);
    }

    private void Goo(int x, bool b);
}");
        }

        [WorkItem(539618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539618")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestClashesWithMethod2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program : IGoo
{
    [|bool IGoo.Goo()
    {
    }|]
} } interface IGoo
{
    void Goo();
}");
        }

        [WorkItem(539637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539637")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestReservedParametername1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public void Method()
    {
        long Long = 10;
        [|M|](Long);
    }
}",
@"using System;

class C
{
    public void Method()
    {
        long Long = 10;
        M(Long);
    }

    private void M(long @long)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539751")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestShadows1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        int Name;
        Name = [|Name|]();
    }
}");
        }

        [WorkItem(539769, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539769")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestShadows2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    delegate void Func(int i, int j);

    static void Main(string[] args)
    {
        Func myExp = (x, y) => Console.WriteLine(x == y);
        myExp(10, 20);
        [|myExp|](10, 20, 10);
    }
}");
        }

        [WorkItem(539781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539781")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInTopLevelMethod()
        {
            await TestInRegularAndScriptAsync(
@"void M()
{
    [|Goo|]();
}",
@"using System;

void M()
{
    Goo();
}

void Goo()
{
    throw new NotImplementedException();
}");
        }

        [WorkItem(539823, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539823")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestLambdaReturnType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C<T, R>
{
    private static Func<T, R> g = null;
    private static Func<T, R> f = (T) => {
        return [|Goo<T, R>|](g);
    };
}",
@"using System;

class C<T, R>
{
    private static Func<T, R> g = null;
    private static Func<T, R> f = (T) => {
        return Goo<T, R>(g);
    };

    private static T2 Goo<T1, T2>(Func<T1, T2> g)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateWithThrow()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M()
    {
        throw [|F|]();
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M()
    {
        throw F();
    }

    private Exception F()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInDelegateConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System;

delegate void D(int x);

class C
{
    void M()
    {
        D d = new D([|Test|]);
    }
}",
@"using System;

delegate void D(int x);

class C
{
    void M()
    {
        D d = new D(Test);
    }

    private void Test(int x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(539871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539871")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDelegateScenario()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C<T>
{
    public delegate void Goo<R>(R r);

    static void M()
    {
        Goo<T> r = [|Goo<T>|];
    }
}");
        }

        [WorkItem(539928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539928")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInheritedTypeParameters1()
        {
            await TestInRegularAndScriptAsync(
@"class C<T, R>
{
    void M()
    {
        I<T, R> i1;
        I<T, R> i2 = i1.[|Goo|]();
    }
}

interface I<T, R>
{
}",
@"class C<T, R>
{
    void M()
    {
        I<T, R> i1;
        I<T, R> i2 = i1.Goo();
    }
}

interface I<T, R>
{
    I<T, R> Goo();
}");
        }

        [WorkItem(539928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539928")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInheritedTypeParameters2()
        {
            await TestInRegularAndScriptAsync(
@"class C<T>
{
    void M()
    {
        I<T> i1;
        I<T> i2 = i1.[|Goo|]();
    }
}

interface I<T>
{
}",
@"class C<T>
{
    void M()
    {
        I<T> i1;
        I<T> i2 = i1.Goo();
    }
}

interface I<T>
{
    I<T> Goo();
}");
        }

        [WorkItem(539928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539928")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInheritedTypeParameters3()
        {
            await TestInRegularAndScriptAsync(
@"class C<T>
{
    void M()
    {
        I<T> i1;
        I<T> i2 = i1.[|Goo|]();
    }
}

interface I<X>
{
}",
@"class C<T>
{
    void M()
    {
        I<T> i1;
        I<T> i2 = i1.Goo();
    }
}

interface I<X>
{
    I<object> Goo();
}");
        }

        [WorkItem(538995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538995")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestBug4777()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        F([|123.4|]);
    }

    void F(int x)
    {
    }
}",
@"using System;

class C
{
    void M()
    {
        F(123.4);
    }

    private void F(double v)
    {
        throw new NotImplementedException();
    }

    void F(int x)
    {
    }
}");
        }

        [WorkItem(539856, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539856")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateOnInvalidInvocation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public delegate int Func(ref int i);

    public int Goo { get; set; }

    public Func Goo()
    {
        return [|Goo|](ref Goo);
    }
}");
        }

        [WorkItem(539752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMissingOnMultipleLambdaInferences()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        C<int> c = new C<int>();
        c.[|Sum|]((arg) => {
            return 2;
        });
    }
}

class C<T> : List<T>
{
    public int Sum(Func<T, int> selector)
    {
        return 2;
    }

    public int Sum(Func<T, double> selector)
    {
        return 3;
    }
}");
        }

        [WorkItem(540505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestParameterTypeAmbiguity()
        {
            await TestInRegularAndScriptAsync(
@"namespace N
{
    class N
    {
        static void Main(string[] args)
        {
            C c;
            [|Goo|](c);
        }
    }

    class C
    {
    }
}",
@"using System;

namespace N
{
    class N
    {
        static void Main(string[] args)
        {
            C c;
            Goo(c);
        }

        private static void Goo(C c)
        {
            throw new NotImplementedException();
        }
    }

    class C
    {
    }
}");
        }

        [WorkItem(541176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestTernaryWithBodySidesBroken1()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void Method()
    {
        int a = 5, b = 10;
        int x = a > b ? [|M|](a) : M(b);
    }
}",
@"using System;

public class C
{
    void Method()
    {
        int a = 5, b = 10;
        int x = a > b ? M(a) : M(b);
    }

    private int M(int a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(541176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestTernaryWithBodySidesBroken2()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void Method()
    {
        int a = 5, b = 10;
        int x = a > b ? M(a) : [|M|](b);
    }
}",
@"using System;

public class C
{
    void Method()
    {
        int a = 5, b = 10;
        int x = a > b ? M(a) : M(b);
    }

    private int M(int b)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNotOnLeftOfAssign()
        {
            await TestMissingInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(541405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541405")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMissingOnImplementedInterfaceMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program<T> : ITest
{
    [|void ITest.Method(T t)
    {
    }|]
}

interface ITest
{
    void Method(object t);
}");
        }

        [WorkItem(541660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541660")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDelegateNamedVar()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    public static void Main()
    {
        var v = [|M|];
    }

    delegate void var(int x);
}",
@"using System;

class Program
{
    public static void Main()
    {
        var v = M;
    }

    private static void M(int x)
    {
        throw new NotImplementedException();
    }

    delegate void var(int x);
}");
        }

        [WorkItem(540991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540991")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestErrorVersusNamedTypeInSignature()
        {
            await TestMissingAsync(
@"using System;

class Outer
{
    class Inner
    {
    }

    void M()
    {
        A.[|Test|](new Inner());
    }
}

class A
{
    internal static void Test(global::Outer.Inner inner)
    {
        throw new NotImplementedException();
    }
}",
new TestParameters(Options.Regular));
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("new()")]
        [InlineData("unmanaged")]
        [WorkItem(542529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542529")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestTypeParameterConstraints(string constraint)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class A<T> where T : {constraint}
{{
}}

class Program
{{
    static void Goo<T>(A<T> x) where T : {constraint}
    {{
        [|Bar|](x);
    }}
}}",
$@"using System;

class A<T> where T : {constraint}
{{
}}

class Program
{{
    static void Goo<T>(A<T> x) where T : {constraint}
    {{
        Bar(x);
    }}

    private static void Bar<T>(A<T> x) where T : {constraint}
    {{
        throw new NotImplementedException();
    }}
}}");
        }

        [WorkItem(542622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542622")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestLambdaTypeParameters()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Goo<T>(List<T> x)
    {
        [|Bar|](() => x);
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    static void Goo<T>(List<T> x)
    {
        Bar(() => x);
    }

    private static void Bar<T>(Func<List<T>> p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("new()")]
        [InlineData("unmanaged")]
        [WorkItem(542626, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542626")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMethodConstraints(string constraint)
        {
            await TestInRegularAndScriptAsync(
$@"using System;

class A<T> where T : {constraint}
{{
}}

class Program
{{
    static void Goo<T>(A<T> x) where T : {constraint}
    {{
        [|Bar<T>|](x);
    }}
}}",
$@"using System;

class A<T> where T : {constraint}
{{
}}

class Program
{{
    static void Goo<T>(A<T> x) where T : {constraint}
    {{
        Bar<T>(x);
    }}

    private static void Bar<T>(A<T> x) where T : {constraint}
    {{
        throw new NotImplementedException();
    }}
}}");
        }

        [WorkItem(542627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542627")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestCaptureMethodTypeParametersReferencedInOuterType1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Goo<T>(List<T>.Enumerator x)
    {
        [|Bar|](x);
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    static void Goo<T>(List<T>.Enumerator x)
    {
        Bar(x);
    }

    private static void Bar<T>(List<T>.Enumerator x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542658")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestCaptureTypeParametersInConstraints()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Goo<T, S>(List<T> x) where T : S
    {
        [|Bar|](x);
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    static void Goo<T, S>(List<T> x) where T : S
    {
        Bar(x);
    }

    private static void Bar<T, S>(List<T> x) where T : S
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542659")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestConstraintOrder1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class A<T, S> where T : ICloneable, S
{
}

class B<S>
{
    public virtual void Goo<T>(A<T, S> x) where T : ICloneable, S
    {
    }
}

class C : B<Exception>
{
    public override void Goo<T>(A<T, Exception> x)
    {
        [|Bar|](x);
    }
}",
@"using System;

class A<T, S> where T : ICloneable, S
{
}

class B<S>
{
    public virtual void Goo<T>(A<T, S> x) where T : ICloneable, S
    {
    }
}

class C : B<Exception>
{
    public override void Goo<T>(A<T, Exception> x)
    {
        Bar(x);
    }

    private void Bar<T>(A<T, Exception> x) where T : Exception, ICloneable
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542678")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestConstraintOrder2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class A<T, S, U> where T : U, S
{
}

class B<S, U>
{
    public virtual void Goo<T>(A<T, S, U> x) where T : U, S
    {
    }
}

class C<U> : B<Exception, U>
{
    public override void Goo<T>(A<T, Exception, U> x)
    {
        [|Bar|](x);
    }
}",
@"using System;

class A<T, S, U> where T : U, S
{
}

class B<S, U>
{
    public virtual void Goo<T>(A<T, S, U> x) where T : U, S
    {
    }
}

class C<U> : B<Exception, U>
{
    public override void Goo<T>(A<T, Exception, U> x)
    {
        Bar(x);
    }

    private void Bar<T>(A<T, Exception, U> x) where T : Exception, U
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542674")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateStaticMethodInField()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    int x = [|Goo|]();
}",
@"using System;

class C
{
    int x = Goo();

    private static int Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542680")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateIntoConstrainedTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
}

class Program
{
    static void Goo<T>(T x) where T : I
    {
        x.[|Bar|]();
    }
}",
@"interface I
{
    void Bar();
}

class Program
{
    static void Goo<T>(T x) where T : I
    {
        x.Bar();
    }
}");
        }

        [WorkItem(542750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542750")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestCaptureOuterTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C<T>
{
    void Bar()
    {
        D d = new D();
        List<T> y;
        d.[|Goo|](y);
    }
}

class D
{
}",
@"using System;
using System.Collections.Generic;

class C<T>
{
    void Bar()
    {
        D d = new D();
        List<T> y;
        d.Goo(y);
    }
}

class D
{
    internal void Goo<T>(List<T> y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542744, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestMostDerivedTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class A<T, U> where T : U
{
}

class B<U>
{
    public virtual void Goo<T>(A<T, U> x) where T : Exception, U
    {
    }
}

class C<U> : B<ArgumentException>
{
    public override void Goo<T>(A<T, ArgumentException> x)
    {
        [|Bar|](x);
    }
}",
@"using System;

class A<T, U> where T : U
{
}

class B<U>
{
    public virtual void Goo<T>(A<T, U> x) where T : Exception, U
    {
    }
}

class C<U> : B<ArgumentException>
{
    public override void Goo<T>(A<T, ArgumentException> x)
    {
        Bar(x);
    }

    private void Bar<T>(A<T, ArgumentException> x) where T : ArgumentException
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(543152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543152")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestAnonymousTypeArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|M|](new { x = 1 });
    }
}",
@"using System;

class C
{
    void M()
    {
        M(new { x = 1 });
    }

    private void M(object p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestListOfAnonymousTypesArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var v = new { };
        var u = Goo(v);
        [|M|](u);
    }

    private List<T> Goo<T>(T v)
    {
        return new List<T>();
    }
}",
@"using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var v = new { };
        var u = Goo(v);
        M(u);
    }

    private void M(List<object> u)
    {
        throw new NotImplementedException();
    }

    private List<T> Goo<T>(T v)
    {
        return new List<T>();
    }
}");
        }

        [WorkItem(543336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543336")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateImplicitlyTypedArrays()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var a = new[] { [|goo|](2), 2, 3 };
    }
}",
@"using System;

class C
{
    void M()
    {
        var a = new[] { goo(2), 2, 3 };
    }

    private int goo(int v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(543510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenericArgWithMissingTypeParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    public static int goo(ref int i)
    {
        return checked([|goo|]<>(ref i) * i);
    }

    public static int goo<T>(ref int i)
    {
        return i;
    }
}");
        }

        [WorkItem(544334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544334")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDuplicateWithErrorType()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class class1
{
    public void Test()
    {
        [|Goo|](x);
    }

    private void Goo(object x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestNoGenerationIntoEntirelyHiddenType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void Goo()
    {
        D.[|Bar|]();
    }
}

#line hidden
class D
{
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDoNotGenerateIntoHiddenRegion1()
        {
            await TestInRegularAndScriptAsync(
@"#line default
class C
{
    void Goo()
    {
        [|Bar|]();
#line hidden
    }
#line default
}",
@"#line default
class C
{
    private void Bar()
    {
        throw new System.NotImplementedException();
    }

    void Goo()
    {
        Bar();
#line hidden
    }
#line default
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDoNotGenerateIntoHiddenRegion2()
        {
            await TestInRegularAndScriptAsync(
@"#line default
class C
{
    void Goo()
    {
        [|Bar|]();
#line hidden
    }

    void Baz()
    {
#line default
    }
}",
@"#line default
class C
{
    void Goo()
    {
        Bar();
#line hidden
    }

    void Baz()
    {
#line default
    }

    private void Bar()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDoNotGenerateIntoHiddenRegion3()
        {
            await TestInRegularAndScriptAsync(
@"#line default
class C
{
    void Goo()
    {
        [|Bar|]();
#line hidden
    }

    void Baz()
    {
#line default
    }

    void Quux()
    {
    }
}",
@"#line default
class C
{
    void Goo()
    {
        Bar();
#line hidden
    }

    void Baz()
    {
#line default
    }

    private void Bar()
    {
        throw new System.NotImplementedException();
    }

    void Quux()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDoNotAddImportsIntoHiddenRegion()
        {
            await TestInRegularAndScriptAsync(
@"
#line hidden
class C
#line default
{
    void Goo()
    {
        [|Bar|]();
#line hidden
    }
#line default
}",
@"
#line hidden
class C
#line default
{
    private void Bar()
    {
        throw new System.NotImplementedException();
    }

    void Goo()
    {
        Bar();
#line hidden
    }
#line default
}");
        }

        [WorkItem(545397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545397")]
        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestVarParameterTypeName()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    void Main()
    {
        var x;
        [|goo|](out x);
    }
}",
@"using System;

class Program
{
    void Main()
    {
        var x;
        goo(out x);
    }

    private void goo(out object x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateInVenus1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
#line 1 ""goo""
    void Goo()
    {
        this.[|Bar|]();
    }
#line default
#line hidden
}");
        }

        [WorkItem(538521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538521")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInIterator1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    IEnumerable<int> Goo()
    {
        yield return [|Bar|]();
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    IEnumerable<int> Goo()
    {
        yield return Bar();
    }

    private int Bar()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(784793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodMissingForAnyArgumentInInvocationHavingErrorTypeAndNotBelongingToEnclosingNamedType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|Main(args.Goo())|];
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Main(args.Goo());
    }

    private static void Main(object p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(907612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithLambda()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class Program
{
    void Baz(string[] args)
    {
        Baz([|() => { return true; }|]);
    }
}",
@"
using System;

class Program
{
    void Baz(string[] args)
    {
        Baz(() => { return true; });
    }

    private void Baz(Func<bool> p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodForDifferentParameterName()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        M([|x: 42|]);
    }

    void M(int y) { }
}",
@"
using System;

class C
{
    void M()
    {
        M(x: 42);
    }

    private void M(int x)
    {
        throw new NotImplementedException();
    }

    void M(int y) { }
}");
        }

        [WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodForDifferentParameterNameCaseSensitive()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        M([|Y: 42|]);
    }

    void M(int y) { }
}",
@"
using System;

class C
{
    void M()
    {
        M(Y: 42);
    }

    private void M(int Y)
    {
        throw new NotImplementedException();
    }

    void M(int y) { }
}");
        }

        [WorkItem(769760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769760")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodForSameNamedButGenericUsage()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Goo();
        [|Goo<int>|]();
    }

    private static void Goo()
    {
        throw new NotImplementedException();
    }
}",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Goo();
        Goo<int>();
    }

    private static void Goo<T>()
    {
        throw new NotImplementedException();
    }

    private static void Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(910589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910589")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodForNewErrorCodeCS7036()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C
{
    void M(int x)
    {
        [|M|]();
    }
}",
@"using System;
class C
{
    void M(int x)
    {
        M();
    }

    private void M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(934729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/934729")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodUnknownReturnTypeInLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic; 
class C
{
    void TestMethod(IEnumerable<C> c)
    {
       new C().[|TestMethod((a,b) => c.Add)|]
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void TestMethod(IEnumerable<C> c)
    {
       new C().TestMethod((a,b) => c.Add)
    }

    private void TestMethod(Func<object, object, object> p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInUnsafeMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C {
    unsafe void Method(he) {
        int a = 10; [|TestMethod(&a)|];
    }
}",
@"using System;
class C {
    unsafe void Method(he) {
        int a = 10; TestMethod(&a);
    }

    private unsafe void TestMethod(int* v)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInUnsafeMethodWithPointerArray()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    unsafe static void M1(int *[] o)
    {
        [|M2(o)|];
    }
}",
@"using System;

class C
{
    unsafe static void M1(int *[] o)
    {
        M2(o);
    }

    private static unsafe void M2(int*[] o)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInUnsafeBlock()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    static void Main()
    {
        unsafe
        {
            fixed (char* value = ""sam"")
            {
                [|TestMethod(value)|];
            }
        }
    }
}",
@"using System;
class Program
{
    static void Main()
    {
        unsafe
        {
            fixed (char* value = ""sam"")
            {
                TestMethod(value);
            }
        }
    }

    private static unsafe void TestMethod(char* value)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInUnsafeMethodNoPointersInParameterList()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class C {
    unsafe void Method(he) {
        int a = 10; [|TestMethod(a)|];
    }
}",
@"using System;
class C {
    unsafe void Method(he) {
        int a = 10; TestMethod(a);
    }

    private void TestMethod(int a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInUnsafeBlockNoPointers()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    static void Main()
    {
        unsafe
        {
            fixed (char value = ""sam"")
            {
                [|TestMethod(value)|];
            }
        }
    }
}",
@"using System;
class Program
{
    static void Main()
    {
        unsafe
        {
            fixed (char value = ""sam"")
            {
                TestMethod(value);
            }
        }
    }

    private static void TestMethod(char value)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodUnsafeReturnType()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    static void Main()
    {
        int* a = [|Test()|];
    }
}",
@"using System;
class Program
{
    static void Main()
    {
        int* a = Test();
    }

    private static unsafe int* Test()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
@"using System;
unsafe class Program
{
    static void Main()
    {
        int* a = [|Test()|];
    }
}",
@"using System;
unsafe class Program
{
    static void Main()
    {
        int* a = Test();
    }

    private static int* Test()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodUnsafeNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"using System;
unsafe class Program
{
    class MyClass
    {
        static void Main()
        {
            int* a = [|Test()|];
        }
    }
}",
@"using System;
unsafe class Program
{
    class MyClass
    {
        static void Main()
        {
            int* a = [|Test()|];
        }

        private static int* Test()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(530177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodUnsafeNestedClass2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    unsafe class MyClass
    {
        static void Main(string[] args)
        {
            int* a = [|Program.Test()|];
        }
    }
}",
@"using System;
class Program
{
    unsafe class MyClass
    {
        static void Main(string[] args)
        {
            int* a = Program.Test();
        }
    }

    private static unsafe int* Test()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestDoNotOfferMethodWithoutParenthesis()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Goo|];
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z|]);
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(Z);
    }

    private object Z()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X|]);
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(Z.X);
    }

    private object nameof(object x)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(Z.X.Y);
    }

    private object nameof(object y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = nameof([|Z.X.Y|]);
    }
}

namespace Z
{
    class X
    {
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(Z.X.Y);
    }
}

namespace Z
{
    class X
    {
        internal static object Y()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|1 + 2|]);
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf6()
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf7()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var y = 1 + 2;
        var z = "";
        var x = [|nameof(y, z)|];
    }
}",
@"using System;

class C
{
    void M()
    {
        var y = 1 + 2;
        var z = "";
        var x = nameof(y, z);
    }

    private object nameof(int y, string z)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf8()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = [|nameof|](1 + 2, """");
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(1 + 2, """");
    }

    private object nameof(int v1, string v2)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = [|nameof|](y, z);
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(y, z);
    }

    private object nameof(object y, object z)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf10()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(y, z);
    }

    private object y()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf11()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = nameof(y, [|z|]);
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(y, z);
    }

    private object z()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf12()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = [|nameof|](y, z);
    }

    private object nameof(object y, object z)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf13()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = nameof([|y|], z);
    }

    private object nameof(object y, object z)
    {
        throw new NotImplementedException();
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(y, z);
    }

    private object y()
    {
        throw new NotImplementedException();
    }

    private object nameof(object y, object z)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf14()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = nameof(y, [|z|]);
    }

    private object nameof(object y, object z)
    {
        throw new NotImplementedException();
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(y, z);
    }

    private object z()
    {
        throw new NotImplementedException();
    }

    private object nameof(object y, object z)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf15()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = [|nameof()|];
    }

    private object nameof(object y)
    {
        throw new NotImplementedException();
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof();
    }

    private object nameof()
    {
        throw new NotImplementedException();
    }

    private object nameof(object y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInsideNameOf16()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        var x = [|nameof(1 + 2, 5)|];
    }

    private object nameof(object y)
    {
        throw new NotImplementedException();
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = nameof(1 + 2, 5);
    }

    private object nameof(int v1, int v2)
    {
        throw new NotImplementedException();
    }

    private object nameof(object y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1075289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075289")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodForInaccessibleMethod()
        {
            await TestInRegularAndScriptAsync(
@"namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        private void Test()
        {
        }
    }

    class Program2 : Program
    {
        public Program2()
        {
            [|Test()|];
        }
    }
}",
@"using System;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        private void Test()
        {
        }
    }

    class Program2 : Program
    {
        public Program2()
        {
            Test();
        }

        private void Test()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccessMissing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void Main(C a)
    {
        C x = new C? [|.B|]();
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void Main(C a)
    {
        C x = a?[|.B|]();
    }
}",
@"using System;

public class C
{
    void Main(C a)
    {
        C x = a?.B();
    }

    private C B()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess2()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void Main(C a)
    {
        int x = a?[|.B|]();
    }
}",
@"using System;

public class C
{
    void Main(C a)
    {
        int x = a?.B();
    }

    private int B()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess3()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void Main(C a)
    {
        int? x = a?[|.B|]();
    }
}",
@"using System;

public class C
{
    void Main(C a)
    {
        int? x = a?.B();
    }

    private int B()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess4()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void Main(C a)
    {
        MyStruct? x = a?[|.B|]();
    }
}",
@"using System;

public class C
{
    void Main(C a)
    {
        MyStruct? x = a?.B();
    }

    private MyStruct B()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestTestGenerateMethodInConditionalAccess5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.[|C|]();
    }

    public class E
    {
    }
}",
@"using System;

class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        C x = a?.B.C();
    }

    public class E
    {
        internal C C()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.[|C|]();
    }

    public class E
    {
    }
}",
@"using System;

class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int x = a?.B.C();
    }

    public class E
    {
        internal int C()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess7()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.[|C|]();
    }

    public class E
    {
    }
}",
@"using System;

class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        int? x = a?.B.C();
    }

    public class E
    {
        internal int C()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(1064748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInConditionalAccess8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.[|C|]();
    }

    public class E
    {
    }
}",
@"using System;

class C
{
    public E B { get; private set; }

    void Main(C a)
    {
        var x = a?.B.C();
    }

    public class E
    {
        internal object C()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInPropertyInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int MyProperty { get; } = [|y|]();
}",
@"using System;

class Program
{
    public int MyProperty { get; } = y();

    private static int y()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInExpressionBodiedMember()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    public int Y => [|y|]();
}",
@"using System;

class Program
{
    public int Y => y();

    private int y()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInExpressionBodiedMember2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C GetValue(C p) => [|x|]();
}",
@"using System;

class C
{
    public static C GetValue(C p) => x();

    private static C x()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInExpressionBodiedMember3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public static C operator --(C p) => [|x|]();
}",
@"using System;

class C
{
    public static C operator --(C p) => x();

    private static C x()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInDictionaryInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [[|key|]()] = 0 };
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [key()] = 0 };
    }

    private static string key()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInDictionaryInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [[|One|]()] = 1, [""Two""] = 2 };
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = 0, [One()] = 1, [""Two""] = 2 };
    }

    private static string One()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodInDictionaryInitializer3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = [|i|]() };
    }
}",
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var x = new Dictionary<string, int> { [""Zero""] = i() };
    }

    private static int i()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithConfigureAwaitFalse()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        bool x = await [|Goo|]().ConfigureAwait(false);
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        bool x = await Goo().ConfigureAwait(false);
    }

    private static Task<bool> Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithMethodChaining()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        bool x = await [|Goo|]().ConfigureAwait(false);
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        bool x = await Goo().ConfigureAwait(false);
    }

    private static Task<bool> Goo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithMethodChaining2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    static async void T()
    {
        bool x = await [|M|]().ContinueWith(a => {
            return true;
        }).ContinueWith(a => {
            return false;
        });
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    static async void T()
    {
        bool x = await M().ContinueWith(a => {
            return true;
        }).ContinueWith(a => {
            return false;
        });
    }

    private static Task<object> M()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInCollectionInitializers1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = new System.Collections.Generic.List<int> { [|T|]() };
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = new System.Collections.Generic.List<int> { T() };
    }

    private int T()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInCollectionInitializers2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var x = new System.Collections.Generic.Dictionary<int, bool> { { 1, [|T|]() } };
    }
}",
@"using System;

class C
{
    void M()
    {
        var x = new System.Collections.Generic.Dictionary<int, bool> { { 1, T() } };
    }

    private bool T()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodEquivalenceKey()
        {
            await TestEquivalenceKeyAsync(
@"class C
{
    void M()
    {
        this.[|M1|](System.Exception.M2());
    }
}",
string.Format(FeaturesResources.Generate_method_1_0, "M1", "C"));
        }

        [WorkItem(5338, "https://github.com/dotnet/roslyn/issues/5338")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodLambdaOverload1()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Concurrent;

class JToken
{
}

class Class1
{
    private static readonly ConcurrentDictionary<Type, Func<JToken, object>> _deserializeHelpers = new ConcurrentDictionary<Type, Func<JToken, object>>();

    private static object DeserializeObject(JToken token, Type type)
    {
        _deserializeHelpers.GetOrAdd(type, key => [|CreateDeserializeDelegate|](key));
    }
}",
@"using System;
using System.Collections.Concurrent;

class JToken
{
}

class Class1
{
    private static readonly ConcurrentDictionary<Type, Func<JToken, object>> _deserializeHelpers = new ConcurrentDictionary<Type, Func<JToken, object>>();

    private static object DeserializeObject(JToken token, Type type)
    {
        _deserializeHelpers.GetOrAdd(type, key => CreateDeserializeDelegate(key));
    }

    private static Func<JToken, object> CreateDeserializeDelegate(JToken key)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodFromStaticProperty()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class Test
{
    public static int Property
    {
        get
        {
            return [|Method|]();
        }
    }
}",
@"using System;

public class Test
{
    public static int Property
    {
        get
        {
            return Method();
        }
    }

    private static int Method()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(8010, "https://github.com/dotnet/roslyn/issues/8010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodFromStaticProperty_FieldInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class OtherClass
{
}

public class Test
{
    public static OtherClass Property
    {
        get
        {
            if (s_field == null)
                s_field = [|InitializeProperty|]();
            return s_field;
        }
    }

    private static OtherClass s_field;
}",
@"using System;

public class OtherClass
{
}

public class Test
{
    public static OtherClass Property
    {
        get
        {
            if (s_field == null)
                s_field = InitializeProperty();
            return s_field;
        }
    }

    private static OtherClass InitializeProperty()
    {
        throw new NotImplementedException();
    }

    private static OtherClass s_field;
}");
        }

        [WorkItem(8230, "https://github.com/dotnet/roslyn/issues/8230")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodForOverloadedSignatureWithDelegateType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class PropertyMetadata
{
    public PropertyMetadata(object defaultValue)
    {
    }

    public PropertyMetadata(EventHandler changedHandler)
    {
    }
}

class Program
{
    static void Main()
    {
        new PropertyMetadata([|OnChanged|]);
    }
}",
@"using System;

class PropertyMetadata
{
    public PropertyMetadata(object defaultValue)
    {
    }

    public PropertyMetadata(EventHandler changedHandler)
    {
    }
}

class Program
{
    static void Main()
    {
        new PropertyMetadata(OnChanged);
    }

    private static void OnChanged(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(10004, "https://github.com/dotnet/roslyn/issues/10004")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateMethodWithMultipleOfSameGenericType()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class C
{
}

public static class Ex
{
    public static T M1<T>(this T t) where T : C
    {
        return [|t.M<T, T>()|];
    }
}",
@"using System;

public class C
{
    internal T2 M<T1, T2>()
        where T1 : C
        where T2 : C
    {
    }
}

public static class Ex
{
    public static T M1<T>(this T t) where T : C
    {
        return t.M<T, T>();
    }
}");
        }

        [WorkItem(11141, "https://github.com/dotnet/roslyn/issues/11141")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task InferTypeParameters1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
        List<int> list = null;
        int i = [|First<int>(list)|];
    }
}",
@"using System;

class C
{
    void M()
    {
        List<int> list = null;
        int i = First<int>(list);
    }

    private T First<T>(List<T> list)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task MethodWithTuple()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int, string) d = [|NewMethod|]((1, ""hello""));
    }
}",
@"using System;

class Class
{
    void Method()
    {
        (int, string) d = NewMethod((1, ""hello""));
    }

    private (int, string) NewMethod((int, string) p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task MethodWithTupleWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int a, string b) d = [|NewMethod|]((c: 1, d: ""hello""));
    }
}",
@"using System;

class Class
{
    void Method()
    {
        (int a, string b) d = NewMethod((c: 1, d: ""hello""));
    }

    private (int a, string b) NewMethod((int c, string d) p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task MethodWithTupleWithOneName()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        (int a, string) d = [|NewMethod|]((c: 1, ""hello""));
    }
}",
@"using System;

class Class
{
    void Method()
    {
        (int a, string) d = NewMethod((c: 1, ""hello""));
    }

    private (int a, string) NewMethod((int c, string) p)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(12147, "https://github.com/dotnet/roslyn/issues/12147")]
        public async Task TestOutVariableDeclaration_ImplicitlyTyped()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](out var c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(out var c);
    }

    private void Undefined(out object c)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(12147, "https://github.com/dotnet/roslyn/issues/12147")]
        public async Task TestOutVariableDeclaration_ExplicitlyTyped()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](out int c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(out int c);
    }

    private void Undefined(out int c)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(12147, "https://github.com/dotnet/roslyn/issues/12147")]
        public async Task TestOutVariableDeclaration_ImplicitlyTyped_NamedArgument()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](a: out var c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(a: out var c);
    }

    private void Undefined(out object a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(12147, "https://github.com/dotnet/roslyn/issues/12147")]
        public async Task TestOutVariableDeclaration_ExplicitlyTyped_NamedArgument()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](a: out int c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(a: out int c);
    }

    private void Undefined(out int a)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestOutVariableDeclaration_ImplicitlyTyped_CSharp6()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](out var c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(out var c);
    }

    private void Undefined(out object c)
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular.WithLanguageVersion(CodeAnalysis.CSharp.LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestOutVariableDeclaration_ExplicitlyTyped_CSharp6()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](out int c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(out int c);
    }

    private void Undefined(out int c)
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular.WithLanguageVersion(CodeAnalysis.CSharp.LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestOutVariableDeclaration_ImplicitlyTyped_NamedArgument_CSharp6()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](a: out var c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(a: out var c);
    }

    private void Undefined(out object a)
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular.WithLanguageVersion(CodeAnalysis.CSharp.LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestOutVariableDeclaration_ExplicitlyTyped_NamedArgument_CSharp6()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|Undefined|](a: out int c);
    }
}",
@"using System;

class Class
{
    void Method()
    {
        Undefined(a: out int c);
    }

    private void Undefined(out int a)
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular.WithLanguageVersion(CodeAnalysis.CSharp.LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(14136, "https://github.com/dotnet/roslyn/issues/14136")]
        public async Task TestDeconstruction1()
        {
            await TestAsync(
@"using System;

class C
{
    public void M1()
    {
        (int x, int y) = [|Method|]();
    }
}",
@"using System;

class C
{
    public void M1()
    {
        (int x, int y) = Method();
    }

    private (int x, int y) Method()
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(14136, "https://github.com/dotnet/roslyn/issues/14136")]
        public async Task TestDeconstruction2()
        {
            await TestAsync(
@"using System;

class C
{
    public void M1()
    {
        (int x, (int y, int z)) = [|Method|]();
    }
}",
@"using System;

class C
{
    public void M1()
    {
        (int x, (int y, int z)) = Method();
    }

    private (int x, (int y, int z)) Method()
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular);
        }

        [Fact/*(Skip = "https://github.com/dotnet/roslyn/issues/15508")*/, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(14136, "https://github.com/dotnet/roslyn/issues/14136")]
        public async Task TestDeconstruction3()
        {
            await TestAsync(
@"using System;

class C
{
    public void M1()
    {
        (int x, (int, int)) = [|Method|]();
    }
}",
@"using System;

class C
{
    public void M1()
    {
        (int x, (int, int)) = Method();
    }

    private object Method()
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(14136, "https://github.com/dotnet/roslyn/issues/14136")]
        public async Task TestDeconstruction4()
        {
            await TestAsync(
@"using System;

class C
{
    public void M1()
    {
        (int x, int) = [|Method|]();
    }
}",
@"using System;

class C
{
    public void M1()
    {
        (int x, int) = Method();
    }

    private object Method()
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular);
        }

        [WorkItem(15315, "https://github.com/dotnet/roslyn/issues/15315")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInferBooleanTypeBasedOnName1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        var v = [|IsPrime|](i);
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        var v = IsPrime(i);
    }

    private bool IsPrime(int i)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(15315, "https://github.com/dotnet/roslyn/issues/15315")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestInferBooleanTypeBasedOnName2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method(int i)
    {
        var v = [|Issue|](i);
    }
}",
@"using System;

class Class
{
    void Method(int i)
    {
        var v = Issue(i);
    }

    private object Issue(int i)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(16398, "https://github.com/dotnet/roslyn/issues/16398")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestReturnsByRef()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C 
{
    public void Goo()
    {
        ref int i = ref [|Bar|]();
    }
}",
@"
using System;

class C 
{
    public void Goo()
    {
        ref int i = ref Bar();
    }

    private ref int Bar()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(18969, "https://github.com/dotnet/roslyn/issues/18969")]
        public async Task TestTupleElement1()
        {
            await TestAsync(
@"using System;

class C
{
    public void M1()
    {
        (int x, string y) t = ([|Method|](), null);
    }
}",
@"using System;

class C
{
    public void M1()
    {
        (int x, string y) t = (Method(), null);
    }

    private int Method()
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        [WorkItem(18969, "https://github.com/dotnet/roslyn/issues/18969")]
        public async Task TestTupleElement2()
        {
            await TestAsync(
@"using System;

class C
{
    public void M1()
    {
        (int x, string y) t = (0, [|Method|]());
    }
}",
@"using System;

class C
{
    public void M1()
    {
        (int x, string y) t = (0, Method());
    }

    private string Method()
    {
        throw new NotImplementedException();
    }
}",
parseOptions: TestOptions.Regular);
        }
    }
}
