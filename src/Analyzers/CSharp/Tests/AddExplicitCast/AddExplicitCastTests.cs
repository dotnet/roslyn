// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddExplicitCast
{
    public partial class AddExplicitCastTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public AddExplicitCastTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddExplicitCastCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

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
        public async Task LambdaFunction5_ReturnStatement()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    Action<Derived> Foo() {
        return [||](Base b) => { };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction6_Arguments()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void M(Derived d, Action<Derived> action) { }
    void Foo() {
        Base b = new Derived();
        M([||]b, (Derived d) => { });
    }
}",
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void M(Derived d, Action<Derived> action) { }
    void Foo() {
        Base b = new Derived();
        M((Derived)b, (Derived d) => { });
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction7_Arguments()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void M(Derived d, Action<Derived> action) { }
    void Foo() {
        Base b = new Derived();
        M([||]b, (Base base) => { });
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction8_Arguments()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void M(Derived d, params Action<Derived>[] action) { }
    void Foo() {
        Base b1 = new Derived();
        M([||]b1, (Derived d) => { }, (Derived d) => { });
    }
}",
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void M(Derived d, params Action<Derived>[] action) { }
    void Foo() {
        Base b1 = new Derived();
        M((Derived)b1, (Derived d) => { }, (Derived d) => { });
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task LambdaFunction9_Arguments()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
using System;

class Program
{
    class Base {}
    class Derived : Base {}

    void M(Derived d, params Action<Derived>[] action) { }
    void Foo() {
        Base b1 = new Derived();
        M([||]b1, action: new Action<Derived>[0], (Derived d) => { }, (Derived d) => { });
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
    void M(Derived d) { }
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ObjectInitializer7()
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
    void M(Derveed d) { }
    void Foo() {
        M([||]new Base());
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ExactMethodCandidate()
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
        public async Task MethodCandidates1_ArgumentsInOrder_NoLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(string s, Derived d) {} 
    void Foo(string s, int i) {}

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b);
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(string s, Derived d) {} 
    void Foo(string s, int i) {}

    void M()
    {
        Base b = new Base();
        Foo("""", (Derived)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates2_ArgumentsInOrder_NoLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(string s, Derived d, out int i) {
        i = 1;
    } 
    void Foo(string s, Derived d) {}

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b, out var i);
    }
}",
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(string s, Derived d, out int i) {
        i = 1;
    } 
    void Foo(string s, Derived d) {}

    void M()
    {
        Base b = new Base();
        Foo("""", (Derived)b, out var i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates3_ArgumentsInOrder_NoLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(string s, Derived d, out int i, params object[] list)
    {
        i = 1;
    }

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b, out var i);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(string s, Derived d, out int i, params object[] list)
    {
        i = 1;
    }

    void M()
    {
        Base b = new Base();
        Foo("""", (Derived)b, out var i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates4_ArgumentsInOrder_NoLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(string s, Derived d, out int i, params object[] list)
    {
        i = 1;
    }

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b, out var i, 1);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(string s, Derived d, out int i, params object[] list)
    {
        i = 1;
    }

    void M()
    {
        Base b = new Base();
        Foo("""", (Derived)b, out var i, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates5_ArgumentsInOrder_NoLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(string s, Derived d, out int i, params object[] list)
    {
        i = 1;
    }

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b, out var i, 1, 2, 3);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(string s, Derived d, out int i, params object[] list)
    {
        i = 1;
    }

    void M()
    {
        Base b = new Base();
        Foo("""", (Derived)b, out var i, 1, 2, 3);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates6_ArgumentsInOrder_NoLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, params Derived2[] list) { }

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, params Derived2[] list) { }

    void M()
    {
        Base b = new Base();
        Foo("""", (Derived)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates7_ArgumentsInOrder_NoLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, params Derived2[] list) { }

    void M()
    {
        Base b = new Base();
        Foo("""", b, [||]b);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, params Derived2[] list) { }

    void M()
    {
        Base b = new Base();
        Foo("""", b, (Derived2)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates8_ArgumentsInOrder_NoLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
namespace ExtensionMethods
{
    public class Base { }
    public class Derived : Base { }

    class Program
    {
        Program()
        {
            string s = """";
            Base b = new Derived();
            Derived d = new Derived();
            s.Foo([||]b, d);
        }
    }
    public static class MyExtensions
    {
        public static void Foo(this string str, Derived d, Derived d2) { }
    }
}",
            @"
namespace ExtensionMethods
{
    public class Base { }
    public class Derived : Base { }

    class Program
    {
        Program()
        {
            string s = """";
            Base b = new Derived();
            Derived d = new Derived();
            s.Foo((Derived)b, d);
        }
    }
    public static class MyExtensions
    {
        public static void Foo(this string str, Derived d, Derived d2) { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates9_ArgumentsOutOfOrder_NoLabels()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived1 : Base {}
    class Derived2 : Derived1 {}

    void Foo(string s, Derived d) {} 
    void Foo(string s, int i) {}

    void M()
    {
        Base b = new Base();
        Foo(b[||], """");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates10_ArgumentsInOrder_SomeLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }

    void M()
    {
        Base b = new Base();
        Foo("""", d: [||]b, 1);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }

    void M()
    {
        Base b = new Base();
        Foo("""", d: (Derived)b, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates11_ArgumentsInOrder_SomeLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: [||]b, 1, list: strlist);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (Derived)b, 1, list: strlist);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates12_ArgumentsInOrder_SomeLabels_Params()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: [||]b, list: strlist, i: 1);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (Derived)b, list: strlist, i: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates13_ArgumentsOutOfOrder_SomeLabels()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo(d: [||]b, """", 1, list: strlist);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates14_ArgumentsOutOfOrder_AllLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo(d: [||]b, s: """", list: strlist, i: 1);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo(d: (Derived)b, s: """", list: strlist, i: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates15_ArgumentsOutOfOrder_AllLabels()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo(d: """", s: [||]b, list: strlist, i: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates17_ArgumentsInOrder_SomeLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, int j = 1) { }

    void M()
    {
        Base b = new Base();
        Foo("""", d: [||]b, 1);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, int j = 1) { }

    void M()
    {
        Base b = new Base();
        Foo("""", d: (Derived)b, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates18_ArgumentsInOrder_SomeLabels()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, params Derived2[] d2list) { }

    void M()
    {
        Base b = new Base();
        var dlist = new Derived[] {};
        Foo("""", d: b, [||]dlist);
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, params Derived2[] d2list) { }

    void M()
    {
        Base b = new Base();
        var dlist = new Derived[] {};
        Foo("""", d: b, (Derived2[])dlist);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates19_ArgumentsInOrder_NoLabels_Params()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(params Derived2[] d2list) { }

    void M()
    {
        Base b = new Base();
        var dlist = new Derived[] {};
        Foo([||]dlist, new Derived2());
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates20_ArgumentsInOrder_NoLabels_Params()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(params Derived2[] d2list) { }

    void M()
    {
        Base b = new Base();
        var dlist = new Derived[] {};
        Foo([||]dlist, dlist);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates21_ArgumentsInOrder_Labels()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    void Foo(Derived d, int i) { }

    void M()
    {
        Base b = new Base();
        Foo([||]b, i:1, i:1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MethodCandidates22_ArgumentsInOrder()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    void Foo(string s, Derived d, out Derived i) {
        i = new Derived();
    } 
    void Foo(string s, Derived d) {}

    void M()
    {
        Base b = new Base();
        Foo("""", [||]b, out Base i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ConstructorCandidates1()
        {
            await TestInRegularAndScriptAsync(
            @"
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
        Test t = new Test(d: [||]b, s:"""", i:1, list : strlist);
    }
}",
            @"
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
        Test t = new Test(d: (Derived)b, s:"""", i:1, list : strlist);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ConstructorCandidates2()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    class Test
    {
        public Test(string s, Derived d, int i, params object[] list) { }
    }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Test t = new Test("""", d: [||]b, i:1, ""1"", ""2"", ""3"");
    }
}",
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    class Test
    {
        public Test(string s, Derived d, int i, params object[] list) { }
    }

    void Foo(string s, Derived d, int i, params object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Test t = new Test("""", d: (Derived)b, i:1, ""1"", ""2"", ""3"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ConstructorCandidates3()
        {
            await TestInRegularAndScriptAsync(
            @"
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
}",
            @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions1()
        {
            var initialMarkup =
                @"
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
}";
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, new TestParameters()))
            {
                var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, new TestParameters());
                Assert.Equal(2, actions.Length);
            }

            var expect_0 =
@"
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
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_0, index: 0,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived"));

            var expect_1 =
    @"
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
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_1, index: 1,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions2()
        {
            var initialMarkup =
                @"
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
}";

            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, new TestParameters()))
            {
                var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, new TestParameters());
                Assert.Equal(2, actions.Length);
            }

            var expect_0 =
@"
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
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_0, index: 0,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived"));

            var expect_1 =
    @"
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
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_1, index: 1,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2"));

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions3()
        {
            var initialMarkup =
                @"
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
}";
            var expected =
                @"
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
}";
            await TestInRegularAndScriptAsync(initialMarkup, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions4()
        {
            var initialMarkup =
                @"
class Program
{
    class Base { }
    class Derived : Base { }
    class Derived2 : Derived { }

    void Foo(string s, int j, int i, Derived d) { }

    void Foo(string s, int i, Derived2 d) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", 1, i:1, d: [||]b);
    }
}";
            var expected =
                @"
class Program
{
    class Base { }
    class Derived : Base { }
    class Derived2 : Derived { }

    void Foo(string s, int j, int i, Derived d) { }

    void Foo(string s, int i, Derived2 d) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", 1, i:1, d: (Derived)b);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions5()
        {
            var initialMarkup =
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }
    void Foo(string s, Derived2 d, int i, object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: [||]b, list: strlist, i: 1);
    }
}";
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, new TestParameters()))
            {
                var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, new TestParameters());
                Assert.Equal(2, actions.Length);
            }

            var expect_0 =
@"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }
    void Foo(string s, Derived2 d, int i, object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (Derived)b, list: strlist, i: 1);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_0, index: 0,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived"));

            var expect_1 =
    @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i, params object[] list) { }
    void Foo(string s, Derived2 d, int i, object[] list) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (Derived2)b, list: strlist, i: 1);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_1, index: 1,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions6()
        {
            var initialMarkup =
            @"
class Program
{
    class Base { 
        static public explicit operator string(Base b) { return """";  }
    }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }
    void Foo(string s, Derived2 d, int i) { }
    void Foo(string s, string d, int i) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: [||]b, i: 1);
    }
}";
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, new TestParameters()))
            {
                var (actions, actionToInvoke) = await GetCodeActionsAsync(workspace, new TestParameters());
                Assert.Equal(3, actions.Length);
            }

            var expect_0 =
@"
class Program
{
    class Base { 
        static public explicit operator string(Base b) { return """";  }
    }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }
    void Foo(string s, Derived2 d, int i) { }
    void Foo(string s, string d, int i) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (string)b, i: 1);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_0, index: 0,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "string"));

            var expect_1 =
@"
class Program
{
    class Base { 
        static public explicit operator string(Base b) { return """";  }
    }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }
    void Foo(string s, Derived2 d, int i) { }
    void Foo(string s, string d, int i) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (Derived)b, i: 1);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_1, index: 1,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived"));

            var expect_2 =
@"
class Program
{
    class Base { 
        static public explicit operator string(Base b) { return """";  }
    }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s, Derived d, int i) { }
    void Foo(string s, Derived2 d, int i) { }
    void Foo(string s, string d, int i) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo("""", d: (Derived2)b, i: 1);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expect_2, index: 2,
                title: string.Format(CodeFixesResources.Convert_type_to_0, "Derived2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions7()
        {
            var initialMarkup =
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s1, int i, Derived d) { }

    void Foo(string s2, int i, Derived2 d) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo(s1:"""", 1, d: [||]b);
    }
}";
            var expected =
            @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(string s1, int i, Derived d) { }

    void Foo(string s2, int i, Derived2 d) { }

    void M()
    {
        Base b = new Base();
        var strlist = new string[1];
        Foo(s1:"""", 1, d: (Derived)b);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions8()
        {
            var initialMarkup =
            @"
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
        Foo4([||]b, ""1"", ""2"", list: strlist);
    }
}";
            var expected =
            @"
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
        Foo4((Derived)b, ""1"", ""2"", list: strlist);
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleOptions9()
        {
            var initialMarkup =
                @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void Foo(Derived d1) { }
    void Foo(Derived2 d2) { }

    void M() {
        Foo([||]new Base());
    }
}";
            await TestMissingInRegularAndScriptAsync(initialMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleErrors1()
        {
            await TestInRegularAndScriptAsync(
                @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void M(Derived2 d2) { }

    void Foo(Base b) {
        Derived d;
        M(d = [|b|]);
    }
}",
                @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void M(Derived2 d2) { }

    void Foo(Base b) {
        Derived d;
        M(d = (Derived)b);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task MultipleErrors2()
        {
            await TestInRegularAndScriptAsync(
                @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void M(Derived2 d2) { }

    void Foo(Base b) {
        Derived d;
        M([||]d = b);
    }
}",
                @"
class Program
{
    class Base { }
    class Derived : Base { }

    class Derived2 : Derived { }

    void M(Derived2 d2) { }

    void Foo(Base b) {
        Derived d;
        M((Derived2)(d = b));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task ErrorType()
        {
            await TestMissingInRegularAndScriptAsync(
                @"
class C 
{
    void M(C c) 
    { 
        TypeThatDoesntExist t = new TypeThatDoesntExist();
        M([||]t);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task AttributeArgument()
        {
            await TestInRegularAndScriptAsync(
                @"
using System;
class C 
{
    static object str = """";

    [Obsolete([||]str, false)]
    void M() 
    {
    }
}",
                @"
using System;
class C 
{
    static object str = """";

    [Obsolete((string)str, false)]
    void M() 
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [WorkItem(50493, "https://github.com/dotnet/roslyn/issues/50493")]
        public async Task ArrayAccess()
        {
            await TestInRegularAndScriptAsync(
                @"
class C
{
    public void M(object o)
    {
        var array = new int[10];

        if (array[[||]o] > 0) {}
    }
}",
                @"
class C
{
    public void M(object o)
    {
        var array = new int[10];

        if (array[(int)o] > 0) {}
    }
}");
        }
    }
}
