// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddExplicitCast
{
    public partial class AddExplicitCastTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new AddExplicitCastCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task SimpleVariableDeclaration()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}
    void M()
    {
        Base b;
        Derived d = [|b|];
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}
    void M()
    {
        Base b;
        Derived d = (Derived)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task SimpleVariableDeclarationWithFunctionInnvocation()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ReturnStatementWithObject()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    Derived returnBase() {
        Base b;
        return b[||];
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    Derived returnBase() {
        Base b;
        return (Derived)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ReturnStatementWithIEnumerable()
        {
            await TestInRegularAndScriptAsync(
            @"
using System.Collections.Generic;
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerable<Derived> returnBase() {
        Base b;
        return b[||];
    }
}",
            @"
using System.Collections.Generic;
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerable<Derived> returnBase() {
        Base b;
        return (IEnumerable<Derived>)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ReturnStatementWithIEnumerator()
        {
            await TestInRegularAndScriptAsync(
            @"
using System.Collections.Generic;
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerator<Derived> returnBase() {
        Base b;
        return b[||];
    }
}",
            @"
using System.Collections.Generic;
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerator<Derived> returnBase() {
        Base b;
        return (IEnumerator<Derived>)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ReturnStatementWithFunctionInnvocation()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task SimpleFunctionArgumentsWithObject1()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task SimpleFunctionArgumentsWithObject2()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task SimpleFunctionArgumentsWithFunctionInvocation()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task YieldReturnStatementWithObject()
        {
            await TestInRegularAndScriptAsync(
            @"
using System.Collections.Generic;
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerable<Derived> returnDerived() {
        Base b;
        yield return [|b|];
    }
}",
            @"
using System.Collections.Generic;
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerable<Derived> returnDerived() {
        Base b;
        yield return (Derived)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task SimpleConstructorArgumentsWithObject()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ReturnTypeWithTask()
        {
            await TestInRegularAndScriptAsync(
            @"
using System.Threading.Tasks;

class Program
{
    class Base {}
    class Derived : Base {}

    async Task<Derived> M() {
        Base b;
        return [||]b;
    }
}",
            @"
using System.Threading.Tasks;

class Program
{
    class Base {}
    class Derived : Base {}

    async Task<Derived> M() {
        Base b;
        return (Derived)b;
    }
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task VariableDeclarationWithPublicFieldMember()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task VariableDeclarationWithPrivateFieldMember()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task PublicMemberFunctionArgument1()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task PublicMemberFunctionArgument2()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task PrivateMemberFunctionArgument()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MemberFunctions()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task BaseConstructorArgument()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ThisConstructorArgument()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}
    class Test {
        public Test(Derived d) {}
        public Test(Base b, int i) : this([||]b) {}
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}
    class Test {
        public Test(Derived d) {}
        public Test(Base b, int i) : this((Derived)b) {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction1()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}
    void M() {
        Func<Base, Derived> foo = d => [||]d;
    }
}",
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}
    void M() {
        Func<Base, Derived> foo = d => (Derived)d;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction2()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void Foo() {
        Func<Derived, Derived> func = d => d;
        Base b;
        Base b2 = func([||]b);
    }
}",
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void Foo() {
        Func<Derived, Derived> func = d => d;
        Base b;
        Base b2 = func((Derived)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction3()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void Foo() {
        Func<Base, Base> func = d => d;
        Base b;
        Derived b2 = [||]func(b);
    }
}",
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void Foo() {
        Func<Base, Base> func = d => d;
        Base b;
        Derived b2 = (Derived)func(b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction4()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    Derived Foo() {
        Func<Base, Base> func = d => d;
        Base b;
        return [||]func(b);
    }
}",
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    Derived Foo() {
        Func<Base, Base> func = d => d;
        Base b;
        return (Derived)func(b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task InheritInterfaces1()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    interface Base1 {}
    interface Base2 {}
    class Derived : Base1, Base2 {}

    void Foo(Base2 b) {
        Derived d = [||]b;
    }
}",
            @"
class Program
{
    interface Base1 {}
    interface Base2 {}
    class Derived : Base1, Base2 {}

    void Foo(Base2 b) {
        Derived d = (Derived)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task InheritInterfaces2()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    interface Base1 {}
    interface Base2 {}
    class Derived1 : Base1, Base2 {}
    class Derived2 : Derived1 {}

    void Foo(Base2 b) {
        Derived2 d = [||]b;
    }
}",
            @"
class Program
{
    interface Base1 {}
    interface Base2 {}
    class Derived1 : Base1, Base2 {}
    class Derived2 : Derived1 {}

    void Foo(Base2 b) {
        Derived2 d = (Derived2)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task InheritInterfaces3()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}

    Base2 Foo(Base1 b) {
        return [||]b;
    }
}",
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}

    Base2 Foo(Base1 b) {
        return (Base2)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task InheritInterfaces4()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}

    void Foo(Base1 b) {
        Base2 b2 = [||]b;
    }
}",
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}

    void Foo(Base1 b) {
        Base2 b2 = (Base2)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task InheritInterfaces5()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}

    void Foo(Base1 b) {
        Base2 b2 = [||]b;
    }
}",
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}

    void Foo(Base1 b) {
        Base2 b2 = (Base2)b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task InheritInterfaces6()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}
    interface Base3 {}
    class Derived1 : Base2, Base3 {}
    class Derived2 : Derived1 {}

    void Foo(Derived2 b) {}
    void M(Base1 b) {
        Foo([||]b);
    }
}",
            @"
class Program
{
    interface Base1 {}
    interface Base2 : Base1 {}
    interface Base3 {}
    class Derived1 : Base2, Base3 {}
    class Derived2 : Derived1 {}

    void Foo(Derived2 b) {}
    void M(Base1 b) {
        Foo((Derived2)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleCandidatesFunction1()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleCandidatesFunction2()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived1 : Base {}
    class Derived2 : Derived1 {}

    void Foo(Derived1 b1) {} 
    void Foo(Derived2 b2) {}

    void M()
    {
        Base b = new Base();
        Foo(b[||]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleCandidatesFunction3()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(Derived b1) {} 
    void Foo() {}

    void M()
    {
        Base b = new Base();
        Foo([||]b);
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(Derived b1) {} 
    void Foo() {}

    void M()
    {
        Base b = new Base();
        Foo((Derived)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleCandidatesFunction4()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(Derived b1) {} 
    void Foo(int i) {}

    void M()
    {
        Base b = new Base();
        Foo([||]b);
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(Derived b1) {} 
    void Foo(int i) {}

    void M()
    {
        Base b = new Base();
        Foo((Derived)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task GenericType()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task GenericType2()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;
class Program
{
    class Base { }
    class Derived : Base { }
    void Foo(Func<Derived, Derived> func) { }

    void M()
    {
        Func<Base, Base> func1 = b => b;
        Foo(func1[||]);
    }
}",
            @"
using System;
class Program
{
    class Base { }
    class Derived : Base { }
    void Foo(Func<Derived, Derived> func) { }

    void M()
    {
        Func<Base, Base> func1 = b => b;
        Foo((Func<Derived, Derived>)func1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task GenericType3()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;
class Program
{
    class Base { }
    class Derived : Base { }
    Func<Derived, Derived> Foo(Func<Derived, Derived> func)
    {
        Func<Base, Base> func1 = b => b;
        return func1[||];
    }
}",
            @"
using System;
class Program
{
    class Base { }
    class Derived : Base { }
    Func<Derived, Derived> Foo(Func<Derived, Derived> func)
    {
        Func<Base, Base> func1 = b => b;
        return (Func<Derived, Derived>)func1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task GenericType4()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    void Foo()
    {
        B<CB> b = null;
        A<IA> c1 = [||]b;
    }

    public interface IA { }
    public class CB : IA { }
    public interface A<T> where T : IA { }

    public class B<T> : A<T> where T : CB { }
}",
            @"
class Program
{
    void Foo()
    {
        B<CB> b = null;
        A<IA> c1 = (A<IA>)b;
    }

    public interface IA { }
    public class CB : IA { }
    public interface A<T> where T : IA { }

    public class B<T> : A<T> where T : CB { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task GenericType5()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    void Foo()
    {
        B<IB> b = null;
        A<IA> c1 = [||]b;
    }

    public interface IA { }
    public interface IB : IA { }
    public class A<T> where T : IA { }

    public class B<T> : A<T> where T : IB { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task GenericType6()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    void Foo()
    {
        B<IB, int> b = null;
        A<IA, string> c1 = [||]b;
    }

    public interface IA { }
    public class IB : IA { }
    public interface A<T, U> where T : IA { }

    public class B<T, U> : A<T, U> where T : IB { }
}",
            @"
class Program
{
    void Foo()
    {
        B<IB, int> b = null;
        A<IA, string> c1 = (A<IA, string>)b;
    }

    public interface IA { }
    public class IB : IA { }
    public interface A<T, U> where T : IA { }

    public class B<T, U> : A<T, U> where T : IB { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer2()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer3()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer4()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer5()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    class Test
	{
		static public explicit operator Derived(Test t) { return new Derived();  }
    }
    void M(Dervied d) { }
    void Foo() {
        M([||]new Base());
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer6()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    class Test
	{
		static public explicit operator Derived(Test t) { return new Derived();  }
    }
    void M(Derived d) { }
    void Foo() {
        M([||]new Test());
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    class Test
	{
		static public explicit operator Derived(Test t) { return new Derived();  }
    }
    void M(Derived d) { }
    void Foo() {
        M((Derived)new Test());
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41500")]
        public async Task RedundantCast1()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    void Foo() {
        Base b;
        Derived d = [||](Base)b;
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    void Foo() {
        Base b;
        Derived d = (Derived)b;
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41500")]
        public async Task RedundantCast2()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived1 : Base { }
    class Derived2 : Derived1 { }
    void Foo() {
        Base b;
        Derived2 d = [||](Derived1)b;
    }
}",
            @"
class Program
{
    class Base { }
    class Derived1 : Base { }
    class Derived2 : Derived1 { }
    void Foo() {
        Base b;
        Derived2 d = (Derived2)b;
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41500")]
        public async Task RedundantCast3()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    void M(Derived d) { }
    void Foo() {
        Base b;
        M([||](Base)b);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }
    void M(Derived d) { }
    void Foo() {
        Base b;
        M((Derived)b);
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41500")]
        public async Task RedundantCast4()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived1 : Base { }
    class Derived2 : Derived1 { }
    void M(Derived2 d) { }
    void Foo() {
        Base b;
        M([||](Derived1)b);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived1 : Base { }
    class Derived2 : Derived1 { }
    void M(Derived2 d) { }
    void Foo() {
        Base b;
        M((Derived2)b);
    }
}");
        }
    }
}
