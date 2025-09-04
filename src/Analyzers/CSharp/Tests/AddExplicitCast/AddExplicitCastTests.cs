// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddExplicitCast;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
public sealed partial class AddExplicitCastTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpAddExplicitCastCodeFixProvider());

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public Task SimpleVariableDeclaration()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            void M()
            {
                Base b;
                Derived d = [|b|];
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            void M()
            {
                Base b;
                Derived d = (Derived)b;
            }
        }
        """);

    [Fact]
    public Task SimpleVariableDeclarationWithFunctionInnvocation()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void M()
            {
                Derived d = [|returnBase()|];
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void M()
            {
                Derived d = (Derived)returnBase();
            }
        }
        """);

    [Fact]
    public Task ReturnStatementWithObject()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Derived returnBase() {
                Base b;
                return b[||];
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Derived returnBase() {
                Base b;
                return (Derived)b;
            }
        }
        """);

    [Fact]
    public Task ReturnStatementWithIEnumerable()
        => TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;
        class Program
        {
            class Base {}
            class Derived : Base {}

            IEnumerable<Derived> returnBase() {
                Base b;
                return b[||];
            }
        }
        """,
        """
        using System.Collections.Generic;
        class Program
        {
            class Base {}
            class Derived : Base {}

            IEnumerable<Derived> returnBase() {
                Base b;
                return (IEnumerable<Derived>)b;
            }
        }
        """);

    [Fact]
    public Task ReturnStatementWithIEnumerator()
        => TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;
        class Program
        {
            class Base {}
            class Derived : Base {}

            IEnumerator<Derived> returnBase() {
                Base b;
                return b[||];
            }
        }
        """,
        """
        using System.Collections.Generic;
        class Program
        {
            class Base {}
            class Derived : Base {}

            IEnumerator<Derived> returnBase() {
                Base b;
                return (IEnumerator<Derived>)b;
            }
        }
        """);

    [Fact]
    public Task ReturnStatementWithFunctionInnvocation()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            Derived returnDerived() {
                return [|returnBase()|];
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            Derived returnDerived() {
                return (Derived)returnBase();
            }
        }
        """);

    [Fact]
    public Task SimpleFunctionArgumentsWithObject1()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void passDerived(Derived d) {}

            void M() {
                Base b;
                passDerived([|b|]);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void passDerived(Derived d) {}

            void M() {
                Base b;
                passDerived((Derived)b);
            }
        }
        """);

    [Fact]
    public Task SimpleFunctionArgumentsWithObject2()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void passDerived(int i, Derived d) {}

            void M() {
                Base b;
                passDerived(1, [|b|]);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void passDerived(int i, Derived d) {}

            void M() {
                Base b;
                passDerived(1, (Derived)b);
            }
        }
        """);

    [Fact]
    public Task SimpleFunctionArgumentsWithFunctionInvocation()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void passDerived(Derived d) {}

            void M() {
                passDerived([|returnBase()|]);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            Base returnBase() {
                Base b;
                return b;
            }

            void passDerived(Derived d) {}

            void M() {
                passDerived((Derived)returnBase());
            }
        }
        """);

    [Fact]
    public Task YieldReturnStatementWithObject()
        => TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;
        class Program
        {
            class Base {}
            class Derived : Base {}

            IEnumerable<Derived> returnDerived() {
                Base b;
                yield return [|b|];
            }
        }
        """,
        """
        using System.Collections.Generic;
        class Program
        {
            class Base {}
            class Derived : Base {}

            IEnumerable<Derived> returnDerived() {
                Base b;
                yield return (Derived)b;
            }
        }
        """);

    [Fact]
    public Task SimpleConstructorArgumentsWithObject()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public Test(Derived d) {}
            }

            void M() {
                Base b;
                Test t = new Test(b[||]);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public Test(Derived d) {}
            }

            void M() {
                Base b;
                Test t = new Test((Derived)b);
            }
        }
        """);

    [Fact]
    public Task ReturnTypeWithTask()
        => TestInRegularAndScriptAsync(
        """
        using System.Threading.Tasks;

        class Program
        {
            class Base {}
            class Derived : Base {}

            async Task<Derived> M() {
                Base b;
                return [||]b;
            }
        }
        """,
        """
        using System.Threading.Tasks;

        class Program
        {
            class Base {}
            class Derived : Base {}

            async Task<Derived> M() {
                Base b;
                return (Derived)b;
            }
        }
        """);

    [Fact]
    public Task VariableDeclarationWithPublicFieldMember()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test { 
                public Base b;
                public Test(Base b) { this.b = b; }
            }

            void M() {
                Base b;
                Test t = new Test(b);
                Derived d = [||]t.b;
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test { 
                public Base b;
                public Test(Base b) { this.b = b; }
            }

            void M() {
                Base b;
                Test t = new Test(b);
                Derived d = (Derived)t.b;
            }
        }
        """);

    [Fact]
    public Task VariableDeclarationWithPrivateFieldMember()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test { 
                Base b;
                public Test(Base b) { this.b = b; }
            }

            void M() {
                Base b;
                Test t = new Test(b);
                Derived d = [||]t.b;
            }
        }
        """);

    [Fact]
    public Task PublicMemberFunctionArgument1()
        => TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M() {
                Base b;
                List<Derived> list = new List<Derived>();
                list.Add(b[||]);
            }
        }
        """,
        """
        using System.Collections.Generic;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M() {
                Base b;
                List<Derived> list = new List<Derived>();
                list.Add((Derived)b);
            }
        }
        """);

    [Fact]
    public Task PublicMemberFunctionArgument2()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public void testing(Derived d) {}
            }

            void M() {
                Base b;
                Test t;
                t.testing(b[||]);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public void testing(Derived d) {}
            }

            void M() {
                Base b;
                Test t;
                t.testing((Derived)b);
            }
        }
        """);

    [Fact]
    public Task PrivateMemberFunctionArgument()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                private void testing(Derived d) {}
            }

            void M() {
                Base b;
                Test t;
                t.testing(b[||]);
            }
        }
        """);

    [Fact]
    public Task MemberFunctions()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public void testing(Derived d) {}
                private void testing(Base b) {}
            }

            void M() {
                Base b;
                Test t;
                t.testing(b[||]);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public void testing(Derived d) {}
                private void testing(Base b) {}
            }

            void M() {
                Base b;
                Test t;
                t.testing((Derived)b);
            }
        }
        """);

    [Fact]
    public Task BaseConstructorArgument()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public Test(Derived d) {}
            }
            class Derived_Test : Test  {
                public Derived_Test (Base b) : base([||]b) {}
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public Test(Derived d) {}
            }
            class Derived_Test : Test  {
                public Derived_Test (Base b) : base((Derived)b) {}
            }
        }
        """);

    [Fact]
    public Task ThisConstructorArgument()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public Test(Derived d) {}
                public Test(Base b, int i) : this([||]b) {}
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}
            class Test {
                public Test(Derived d) {}
                public Test(Base b, int i) : this((Derived)b) {}
            }
        }
        """);

    [Fact]
    public Task LambdaFunction1()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}
            void M() {
                Func<Base, Derived> foo = d => [||]d;
            }
        }
        """,
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}
            void M() {
                Func<Base, Derived> foo = d => (Derived)d;
            }
        }
        """);

    [Fact]
    public Task LambdaFunction2()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo() {
                Func<Derived, Derived> func = d => d;
                Base b;
                Base b2 = func([||]b);
            }
        }
        """,
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo() {
                Func<Derived, Derived> func = d => d;
                Base b;
                Base b2 = func((Derived)b);
            }
        }
        """);

    [Fact]
    public Task LambdaFunction3()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo() {
                Func<Base, Base> func = d => d;
                Base b;
                Derived b2 = [||]func(b);
            }
        }
        """,
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo() {
                Func<Base, Base> func = d => d;
                Base b;
                Derived b2 = (Derived)func(b);
            }
        }
        """);

    [Fact]
    public Task LambdaFunction4()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            Derived Goo() {
                Func<Base, Base> func = d => d;
                Base b;
                return [||]func(b);
            }
        }
        """,
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            Derived Goo() {
                Func<Base, Base> func = d => d;
                Base b;
                return (Derived)func(b);
            }
        }
        """);

    [Fact]
    public Task LambdaFunction5_ReturnStatement()
        => TestMissingInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            Action<Derived> Goo() {
                return [||](Base b) => { };
            }
        }
        """);

    [Fact]
    public Task LambdaFunction6_Arguments()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M(Derived d, Action<Derived> action) { }
            void Goo() {
                Base b = new Derived();
                M([||]b, (Derived d) => { });
            }
        }
        """,
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M(Derived d, Action<Derived> action) { }
            void Goo() {
                Base b = new Derived();
                M((Derived)b, (Derived d) => { });
            }
        }
        """);

    [Fact]
    public Task LambdaFunction7_Arguments()
        => TestMissingInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M(Derived d, Action<Derived> action) { }
            void Goo() {
                Base b = new Derived();
                M([||]b, (Base base) => { });
            }
        }
        """);

    [Fact]
    public Task LambdaFunction8_Arguments()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M(Derived d, params Action<Derived>[] action) { }
            void Goo() {
                Base b1 = new Derived();
                M([||]b1, (Derived d) => { }, (Derived d) => { });
            }
        }
        """,
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M(Derived d, params Action<Derived>[] action) { }
            void Goo() {
                Base b1 = new Derived();
                M((Derived)b1, (Derived d) => { }, (Derived d) => { });
            }
        }
        """);

    [Fact]
    public Task LambdaFunction9_Arguments()
        => TestMissingInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            class Base {}
            class Derived : Base {}

            void M(Derived d, params Action<Derived>[] action) { }
            void Goo() {
                Base b1 = new Derived();
                M([||]b1, action: new Action<Derived>[0], (Derived d) => { }, (Derived d) => { });
            }
        }
        """);

    [Fact]
    public Task InheritInterfaces1()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            interface Base1 {}
            interface Base2 {}
            class Derived : Base1, Base2 {}

            void Goo(Base2 b) {
                Derived d = [||]b;
            }
        }
        """,
        """
        class Program
        {
            interface Base1 {}
            interface Base2 {}
            class Derived : Base1, Base2 {}

            void Goo(Base2 b) {
                Derived d = (Derived)b;
            }
        }
        """);

    [Fact]
    public Task InheritInterfaces2()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            interface Base1 {}
            interface Base2 {}
            class Derived1 : Base1, Base2 {}
            class Derived2 : Derived1 {}

            void Goo(Base2 b) {
                Derived2 d = [||]b;
            }
        }
        """,
        """
        class Program
        {
            interface Base1 {}
            interface Base2 {}
            class Derived1 : Base1, Base2 {}
            class Derived2 : Derived1 {}

            void Goo(Base2 b) {
                Derived2 d = (Derived2)b;
            }
        }
        """);

    [Fact]
    public Task InheritInterfaces3()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            interface Base1 {}
            interface Base2 : Base1 {}

            Base2 Goo(Base1 b) {
                return [||]b;
            }
        }
        """,
        """
        class Program
        {
            interface Base1 {}
            interface Base2 : Base1 {}

            Base2 Goo(Base1 b) {
                return (Base2)b;
            }
        }
        """);

    [Fact]
    public Task InheritInterfaces4()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            interface Base1 {}
            interface Base2 : Base1 {}

            void Goo(Base1 b) {
                Base2 b2 = [||]b;
            }
        }
        """,
        """
        class Program
        {
            interface Base1 {}
            interface Base2 : Base1 {}

            void Goo(Base1 b) {
                Base2 b2 = (Base2)b;
            }
        }
        """);

    [Fact]
    public Task InheritInterfaces5()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            interface Base1 {}
            interface Base2 : Base1 {}
            interface Base3 {}
            class Derived1 : Base2, Base3 {}
            class Derived2 : Derived1 {}

            void Goo(Derived2 b) {}
            void M(Base1 b) {
                Goo([||]b);
            }
        }
        """,
        """
        class Program
        {
            interface Base1 {}
            interface Base2 : Base1 {}
            interface Base3 {}
            class Derived1 : Base2, Base3 {}
            class Derived2 : Derived1 {}

            void Goo(Derived2 b) {}
            void M(Base1 b) {
                Goo((Derived2)b);
            }
        }
        """);

    [Fact]
    public Task GenericType()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            class Base {}
            class Derived : Base {}

            void M()
            {
                Func<Base, Base> func1 = b => b;
                Func<Derived, Derived> func2 = [||]func1;
            }
        }
        """,
        """
        using System;
        class Program
        {
            class Base {}
            class Derived : Base {}

            void M()
            {
                Func<Base, Base> func1 = b => b;
                Func<Derived, Derived> func2 = (Func<Derived, Derived>)func1;
            }
        }
        """);

    [Fact]
    public Task GenericType2()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            class Base { }
            class Derived : Base { }
            void Goo(Func<Derived, Derived> func) { }

            void M()
            {
                Func<Base, Base> func1 = b => b;
                Goo(func1[||]);
            }
        }
        """,
        """
        using System;
        class Program
        {
            class Base { }
            class Derived : Base { }
            void Goo(Func<Derived, Derived> func) { }

            void M()
            {
                Func<Base, Base> func1 = b => b;
                Goo((Func<Derived, Derived>)func1);
            }
        }
        """);

    [Fact]
    public Task GenericType3()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            class Base { }
            class Derived : Base { }
            Func<Derived, Derived> Goo(Func<Derived, Derived> func)
            {
                Func<Base, Base> func1 = b => b;
                return func1[||];
            }
        }
        """,
        """
        using System;
        class Program
        {
            class Base { }
            class Derived : Base { }
            Func<Derived, Derived> Goo(Func<Derived, Derived> func)
            {
                Func<Base, Base> func1 = b => b;
                return (Func<Derived, Derived>)func1;
            }
        }
        """);

    [Fact]
    public Task GenericType4()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            void Goo()
            {
                B<CB> b = null;
                A<IA> c1 = [||]b;
            }

            public interface IA { }
            public class CB : IA { }
            public interface A<T> where T : IA { }

            public class B<T> : A<T> where T : CB { }
        }
        """,
        """
        class Program
        {
            void Goo()
            {
                B<CB> b = null;
                A<IA> c1 = (A<IA>)b;
            }

            public interface IA { }
            public class CB : IA { }
            public interface A<T> where T : IA { }

            public class B<T> : A<T> where T : CB { }
        }
        """);

    [Fact]
    public Task GenericType5()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            void Goo()
            {
                B<IB> b = null;
                A<IA> c1 = [||]b;
            }

            public interface IA { }
            public interface IB : IA { }
            public class A<T> where T : IA { }

            public class B<T> : A<T> where T : IB { }
        }
        """);

    [Fact]
    public Task GenericType6()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            void Goo()
            {
                B<IB, int> b = null;
                A<IA, string> c1 = [||]b;
            }

            public interface IA { }
            public class IB : IA { }
            public interface A<T, U> where T : IA { }

            public class B<T, U> : A<T, U> where T : IB { }
        }
        """,
        """
        class Program
        {
            void Goo()
            {
                B<IB, int> b = null;
                A<IA, string> c1 = (A<IA, string>)b;
            }

            public interface IA { }
            public class IB : IA { }
            public interface A<T, U> where T : IA { }

            public class B<T, U> : A<T, U> where T : IB { }
        }
        """);

    [Fact]
    public Task ObjectInitializer()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M() {
                Derived d = [||]new Base();
                Derived d2 = new Test();
            }
        }
        """);

    [Fact]
    public Task ObjectInitializer2()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M() {
                Derived d = new Base();
                Derived d2 = [||]new Test();
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M() {
                Derived d = new Base();
                Derived d2 = (Derived)new Test();
            }
        }
        """);

    [Fact]
    public Task ObjectInitializer3()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            Derived returnDerived() {
                return [||]new Base();
            }
        }
        """);

    [Fact]
    public Task ObjectInitializer4()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            Derived returnDerived() {
                return [||]new Test();
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            Derived returnDerived() {
                return (Derived)new Test();
            }
        }
        """);

    [Fact]
    public Task ObjectInitializer5()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M(Derived d) { }
            void Goo() {
                M([||]new Base());
            }
        }
        """);

    [Fact]
    public Task ObjectInitializer6()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M(Derived d) { }
            void Goo() {
                M([||]new Test());
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M(Derived d) { }
            void Goo() {
                M((Derived)new Test());
            }
        }
        """);

    [Fact]
    public Task ObjectInitializer7()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            class Test
            {
                static public explicit operator Derived(Test t) { return new Derived();  }
            }
            void M(Derived d) { }
            void Goo() {
                M([||]new Base());
            }
        }
        """);

    [Fact]
    public Task RedundantCast1()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            void Goo() {
                Base b;
                Derived d = [||](Base)b;
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            void Goo() {
                Base b;
                Derived d = (Derived)b;
            }
        }
        """);

    [Fact]
    public Task RedundantCast2()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived1 : Base { }
            class Derived2 : Derived1 { }
            void Goo() {
                Base b;
                Derived2 d = [||](Derived1)b;
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived1 : Base { }
            class Derived2 : Derived1 { }
            void Goo() {
                Base b;
                Derived2 d = (Derived2)b;
            }
        }
        """);

    [Fact]
    public Task RedundantCast3()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            void M(Derived d) { }
            void Goo() {
                Base b;
                M([||](Base)b);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }
            void M(Derived d) { }
            void Goo() {
                Base b;
                M((Derived)b);
            }
        }
        """);

    [Fact]
    public Task RedundantCast4()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived1 : Base { }
            class Derived2 : Derived1 { }
            void M(Derived2 d) { }
            void Goo() {
                Base b;
                M([||](Derived1)b);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived1 : Base { }
            class Derived2 : Derived1 { }
            void M(Derived2 d) { }
            void Goo() {
                Base b;
                M((Derived2)b);
            }
        }
        """);

    [Fact]
    public Task ExactMethodCandidate()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base
            {
                public void Testing(Base d) { }
            }
            class Derived : Base
            {
                public void Testing(Derived d) { }
            }

            void M()
            {
                Base b = new Base();
                Derived d = new Derived();
                d.Testing([||]b);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates1_ArgumentsInOrder_NoLabels()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo(string s, Derived d) {} 
            void Goo(string s, int i) {}

            void M()
            {
                Base b = new Base();
                Goo("", [||]b);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo(string s, Derived d) {} 
            void Goo(string s, int i) {}

            void M()
            {
                Base b = new Base();
                Goo("", (Derived)b);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates2_ArgumentsInOrder_NoLabels()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo(string s, Derived d, out int i) {
                i = 1;
            } 
            void Goo(string s, Derived d) {}

            void M()
            {
                Base b = new Base();
                Goo("", [||]b, out var i);
            }
        }
        """,
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo(string s, Derived d, out int i) {
                i = 1;
            } 
            void Goo(string s, Derived d) {}

            void M()
            {
                Base b = new Base();
                Goo("", (Derived)b, out var i);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates3_ArgumentsInOrder_NoLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(string s, Derived d, out int i, params object[] list)
            {
                i = 1;
            }

            void M()
            {
                Base b = new Base();
                Goo("", [||]b, out var i);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(string s, Derived d, out int i, params object[] list)
            {
                i = 1;
            }

            void M()
            {
                Base b = new Base();
                Goo("", (Derived)b, out var i);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates4_ArgumentsInOrder_NoLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(string s, Derived d, out int i, params object[] list)
            {
                i = 1;
            }

            void M()
            {
                Base b = new Base();
                Goo("", [||]b, out var i, 1);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(string s, Derived d, out int i, params object[] list)
            {
                i = 1;
            }

            void M()
            {
                Base b = new Base();
                Goo("", (Derived)b, out var i, 1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates5_ArgumentsInOrder_NoLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(string s, Derived d, out int i, params object[] list)
            {
                i = 1;
            }

            void M()
            {
                Base b = new Base();
                Goo("", [||]b, out var i, 1, 2, 3);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(string s, Derived d, out int i, params object[] list)
            {
                i = 1;
            }

            void M()
            {
                Base b = new Base();
                Goo("", (Derived)b, out var i, 1, 2, 3);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates6_ArgumentsInOrder_NoLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, params Derived2[] list) { }

            void M()
            {
                Base b = new Base();
                Goo("", [||]b);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, params Derived2[] list) { }

            void M()
            {
                Base b = new Base();
                Goo("", (Derived)b);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates7_ArgumentsInOrder_NoLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, params Derived2[] list) { }

            void M()
            {
                Base b = new Base();
                Goo("", b, [||]b);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, params Derived2[] list) { }

            void M()
            {
                Base b = new Base();
                Goo("", b, (Derived2)b);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates8_ArgumentsInOrder_NoLabels()
        => TestInRegularAndScriptAsync(
        """
        namespace ExtensionMethods
        {
            public class Base { }
            public class Derived : Base { }

            class Program
            {
                Program()
                {
                    string s = "";
                    Base b = new Derived();
                    Derived d = new Derived();
                    s.Goo([||]b, d);
                }
            }
            public static class MyExtensions
            {
                public static void Goo(this string str, Derived d, Derived d2) { }
            }
        }
        """,
        """
        namespace ExtensionMethods
        {
            public class Base { }
            public class Derived : Base { }

            class Program
            {
                Program()
                {
                    string s = "";
                    Base b = new Derived();
                    Derived d = new Derived();
                    s.Goo((Derived)b, d);
                }
            }
            public static class MyExtensions
            {
                public static void Goo(this string str, Derived d, Derived d2) { }
            }
        }
        """);

    [Fact]
    public Task MethodCandidates9_ArgumentsOutOfOrder_NoLabels()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived1 : Base {}
            class Derived2 : Derived1 {}

            void Goo(string s, Derived d) {} 
            void Goo(string s, int i) {}

            void M()
            {
                Base b = new Base();
                Goo(b[||], "");
            }
        }
        """);

    [Fact]
    public Task MethodCandidates10_ArgumentsInOrder_SomeLabels()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i) { }

            void M()
            {
                Base b = new Base();
                Goo("", d: [||]b, 1);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i) { }

            void M()
            {
                Base b = new Base();
                Goo("", d: (Derived)b, 1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates11_ArgumentsInOrder_SomeLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo("", d: [||]b, 1, list: strlist);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo("", d: (Derived)b, 1, list: strlist);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates12_ArgumentsInOrder_SomeLabels_Params()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo("", d: [||]b, list: strlist, i: 1);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo("", d: (Derived)b, list: strlist, i: 1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates13_ArgumentsOutOfOrder_SomeLabels()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo(d: [||]b, "", 1, list: strlist);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates14_ArgumentsOutOfOrder_AllLabels()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo(d: [||]b, s: "", list: strlist, i: 1);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo(d: (Derived)b, s: "", list: strlist, i: 1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates15_ArgumentsOutOfOrder_AllLabels()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo(d: "", s: [||]b, list: strlist, i: 1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates17_ArgumentsInOrder_SomeLabels()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, int j = 1) { }

            void M()
            {
                Base b = new Base();
                Goo("", d: [||]b, 1);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, int j = 1) { }

            void M()
            {
                Base b = new Base();
                Goo("", d: (Derived)b, 1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates18_ArgumentsInOrder_SomeLabels()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, params Derived2[] d2list) { }

            void M()
            {
                Base b = new Base();
                var dlist = new Derived[] {};
                Goo("", d: b, [||]dlist);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, params Derived2[] d2list) { }

            void M()
            {
                Base b = new Base();
                var dlist = new Derived[] {};
                Goo("", d: b, (Derived2[])dlist);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates19_ArgumentsInOrder_NoLabels_Params()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(params Derived2[] d2list) { }

            void M()
            {
                Base b = new Base();
                var dlist = new Derived[] {};
                Goo([||]dlist, new Derived2());
            }
        }
        """);

    [Fact]
    public Task MethodCandidates20_ArgumentsInOrder_NoLabels_Params()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(params Derived2[] d2list) { }

            void M()
            {
                Base b = new Base();
                var dlist = new Derived[] {};
                Goo([||]dlist, dlist);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates21_ArgumentsInOrder_Labels()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            void Goo(Derived d, int i) { }

            void M()
            {
                Base b = new Base();
                Goo([||]b, i:1, i:1);
            }
        }
        """);

    [Fact]
    public Task MethodCandidates22_ArgumentsInOrder()
        => TestMissingInRegularAndScriptAsync(
        """
        class Program
        {
            class Base {}
            class Derived : Base {}

            void Goo(string s, Derived d, out Derived i) {
                i = new Derived();
            } 
            void Goo(string s, Derived d) {}

            void M()
            {
                Base b = new Base();
                Goo("", [||]b, out Base i);
            }
        }
        """);

    [Fact]
    public Task ConstructorCandidates1()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            class Test
            {
                public Test(string s, Derived d, int i, params object[] list) { }
            }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Test t = new Test(d: [||]b, s:"", i:1, list : strlist);
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            class Test
            {
                public Test(string s, Derived d, int i, params object[] list) { }
            }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Test t = new Test(d: (Derived)b, s:"", i:1, list : strlist);
            }
        }
        """);

    [Fact]
    public Task ConstructorCandidates2()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            class Test
            {
                public Test(string s, Derived d, int i, params object[] list) { }
            }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Test t = new Test("", d: [||]b, i:1, "1", "2", "3");
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            class Test
            {
                public Test(string s, Derived d, int i, params object[] list) { }
            }

            void Goo(string s, Derived d, int i, params object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Test t = new Test("", d: (Derived)b, i:1, "1", "2", "3");
            }
        }
        """);

    [Fact]
    public Task ConstructorCandidates3()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            class Test
            {
                public Test(string s, Base b, int i, params object[] list) : this(d : [||]b, s : s, i : i) { }
                Test(string s, Derived d, int i) { }
            }
        }
        """,
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            class Test
            {
                public Test(string s, Base b, int i, params object[] list) : this(d : (Derived)b, s : s, i : i) { }
                Test(string s, Derived d, int i) { }
            }
        }
        """);

    [Fact]
    public async Task MultipleOptions1()
    {
        var initialMarkup =
            """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : [||]b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, Derived2 d, int i) { }
                }
            }
            """;
        using (var workspace = CreateWorkspaceFromOptions(initialMarkup, TestParameters.Default))
        {
            var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, TestParameters.Default);
            Assert.Equal(2, actions.Length);
        }

        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : (Derived)b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, Derived2 d, int i) { }
                }
            }
            """, index: 0,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived")));
        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : (Derived2)b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, Derived2 d, int i) { }
                }
            }
            """, index: 1,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2")));
    }

    [Fact]
    public async Task MultipleOptions2()
    {
        var initialMarkup =
            """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : [||]b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, int i, Derived2 d) { }
                }
            }
            """;

        using (var workspace = CreateWorkspaceFromOptions(initialMarkup, TestParameters.Default))
        {
            var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, TestParameters.Default);
            Assert.Equal(2, actions.Length);
        }

        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : (Derived)b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, int i, Derived2 d) { }
                }
            }
            """, index: 0,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived")));
        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : (Derived2)b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, int i, Derived2 d) { }
                }
            }
            """, index: 1,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2")));

    }

    [Fact]
    public Task MultipleOptions3()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : [||]b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, Derived d, int i, params object[] list)) { }
                }
            }
            """, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                class Test
                {
                    public Test(string s, Base b, int i, params object[] list) : this(d : (Derived)b, s : s, i : i) { }
                    Test(string s, Derived d, int i) { }
                    Test(string s, Derived d, int i, params object[] list)) { }
                }
            }
            """);

    [Fact]
    public Task MultipleOptions4()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                void Goo(string s, int j, int i, Derived d) { }

                void Goo(string s, int i, Derived2 d) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", 1, i:1, d: [||]b);
                }
            }
            """, """
            class Program
            {
                class Base { }
                class Derived : Base { }
                class Derived2 : Derived { }

                void Goo(string s, int j, int i, Derived d) { }

                void Goo(string s, int i, Derived2 d) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", 1, i:1, d: (Derived)b);
                }
            }
            """);

    [Fact]
    public async Task MultipleOptions5()
    {
        var initialMarkup =
        """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i, params object[] list) { }
            void Goo(string s, Derived2 d, int i, object[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo("", d: [||]b, list: strlist, i: 1);
            }
        }
        """;
        using (var workspace = CreateWorkspaceFromOptions(initialMarkup, TestParameters.Default))
        {
            var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, TestParameters.Default);
            Assert.Equal(2, actions.Length);
        }

        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void Goo(string s, Derived d, int i, params object[] list) { }
                void Goo(string s, Derived2 d, int i, object[] list) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", d: (Derived)b, list: strlist, i: 1);
                }
            }
            """, index: 0,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived")));
        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void Goo(string s, Derived d, int i, params object[] list) { }
                void Goo(string s, Derived2 d, int i, object[] list) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", d: (Derived2)b, list: strlist, i: 1);
                }
            }
            """, index: 1,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2")));
    }

    [Fact]
    public async Task MultipleOptions6()
    {
        var initialMarkup =
        """
        class Program
        {
            class Base { 
                static public explicit operator string(Base b) { return "";  }
            }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s, Derived d, int i) { }
            void Goo(string s, Derived2 d, int i) { }
            void Goo(string s, string d, int i) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo("", d: [||]b, i: 1);
            }
        }
        """;
        using (var workspace = CreateWorkspaceFromOptions(initialMarkup, TestParameters.Default))
        {
            var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, TestParameters.Default);
            Assert.Equal(3, actions.Length);
        }

        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { 
                    static public explicit operator string(Base b) { return "";  }
                }
                class Derived : Base { }

                class Derived2 : Derived { }

                void Goo(string s, Derived d, int i) { }
                void Goo(string s, Derived2 d, int i) { }
                void Goo(string s, string d, int i) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", d: (string)b, i: 1);
                }
            }
            """, index: 0,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "string")));
        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { 
                    static public explicit operator string(Base b) { return "";  }
                }
                class Derived : Base { }

                class Derived2 : Derived { }

                void Goo(string s, Derived d, int i) { }
                void Goo(string s, Derived2 d, int i) { }
                void Goo(string s, string d, int i) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", d: (Derived)b, i: 1);
                }
            }
            """, index: 1,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived")));
        await TestInRegularAndScriptAsync(initialMarkup, """
            class Program
            {
                class Base { 
                    static public explicit operator string(Base b) { return "";  }
                }
                class Derived : Base { }

                class Derived2 : Derived { }

                void Goo(string s, Derived d, int i) { }
                void Goo(string s, Derived2 d, int i) { }
                void Goo(string s, string d, int i) { }

                void M()
                {
                    Base b = new Base();
                    var strlist = new string[1];
                    Goo("", d: (Derived2)b, i: 1);
                }
            }
            """, index: 2,
            new(title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2")));
    }

    [Fact]
    public Task MultipleOptions7()
        => TestInRegularAndScriptAsync("""
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s1, int i, Derived d) { }

            void Goo(string s2, int i, Derived2 d) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo(s1:"", 1, d: [||]b);
            }
        }
        """, """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Goo(string s1, int i, Derived d) { }

            void Goo(string s2, int i, Derived2 d) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[1];
                Goo(s1:"", 1, d: (Derived)b);
            }
        }
        """);

    [Fact]
    public Task MultipleOptions8()
        => TestInRegularAndScriptAsync("""
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Foo4(Derived d, string a, string b, params string[] list) { }
            void Foo4(Derived2 d, params string[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[] { };
                Foo4([||]b, "1", "2", list: strlist);
            }
        }
        """, """
        class Program
        {
            class Base { }
            class Derived : Base { }

            class Derived2 : Derived { }

            void Foo4(Derived d, string a, string b, params string[] list) { }
            void Foo4(Derived2 d, params string[] list) { }

            void M()
            {
                Base b = new Base();
                var strlist = new string[] { };
                Foo4((Derived)b, "1", "2", list: strlist);
            }
        }
        """);

    [Fact]
    public Task MultipleOptions9()
        => TestMissingInRegularAndScriptAsync("""
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void Goo(Derived d1) { }
                void Goo(Derived2 d2) { }

                void M() {
                    Goo([||]new Base());
                }
            }
            """);

    [Fact]
    public Task MultipleErrors1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void M(Derived2 d2) { }

                void Goo(Base b) {
                    Derived d;
                    M(d = [|b|]);
                }
            }
            """,
            """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void M(Derived2 d2) { }

                void Goo(Base b) {
                    Derived d;
                    M(d = (Derived)b);
                }
            }
            """);

    [Fact]
    public Task MultipleErrors2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void M(Derived2 d2) { }

                void Goo(Base b) {
                    Derived d;
                    M([||]d = b);
                }
            }
            """,
            """
            class Program
            {
                class Base { }
                class Derived : Base { }

                class Derived2 : Derived { }

                void M(Derived2 d2) { }

                void Goo(Base b) {
                    Derived d;
                    M((Derived2)(d = b));
                }
            }
            """);

    [Fact]
    public Task ErrorType()
        => TestMissingInRegularAndScriptAsync(
            """
            class C 
            {
                void M(C c) 
                { 
                    TypeThatDoesntExist t = new TypeThatDoesntExist();
                    M([||]t);
                }
            }
            """);

    [Fact]
    public Task AttributeArgument()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C 
            {
                static object str = "";

                [Obsolete([||]str, false)]
                void M() 
                {
                }
            }
            """,
            """
            using System;
            class C 
            {
                static object str = "";

                [Obsolete((string)str, false)]
                void M() 
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50493")]
    public Task ArrayAccess()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M(object o)
                {
                    var array = new int[10];

                    if (array[[||]o] > 0) {}
                }
            }
            """,
            """
            class C
            {
                public void M(object o)
                {
                    var array = new int[10];

                    if (array[(int)o] > 0) {}
                }
            }
            """);

    [Fact]
    public Task RemoveExistingCast1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                class Base { }
                class Derived1 : Base { }
                class Derived2 : Derived1 { }
                void Goo() {
                    Base b;
                    Derived2 d = [||](Derived1)b;
                }
            }
            """,
            """
            class Program
            {
                class Base { }
                class Derived1 : Base { }
                class Derived2 : Derived1 { }
                void Goo() {
                    Base b;
                    Derived2 d = (Derived2)b;
                }
            }
            """);

    [Fact, WorkItem(56141, "https://github.com/dotnet/roslyn/issues/56141")]
    public Task CompoundAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    [||]a += b;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    a += (int)b;
                }
            }
            """);

    [Fact, WorkItem(56141, "https://github.com/dotnet/roslyn/issues/56141")]
    public Task CompoundAssignment2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    a = ([||]b += 1);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    a = (int)(b += 1);
                }
            }
            """);

    [Fact, WorkItem(56141, "https://github.com/dotnet/roslyn/issues/56141")]
    public Task CompoundAssignment3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    a = [||]b += 1;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    a = (int)(b += 1);
                }
            }
            """);

    [Fact, WorkItem(56141, "https://github.com/dotnet/roslyn/issues/56141")]
    public Task CompoundAssignment4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    b = ([||]a += b);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    b = (a += (int)b);
                }
            }
            """);

    [Fact, WorkItem(56141, "https://github.com/dotnet/roslyn/issues/56141")]
    public Task CompoundAssignment5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    b = [||]a += b;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int a = 0;
                    double b = 0;
                    b = a += (int)b;
                }
            }
            """);
}
