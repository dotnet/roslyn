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
        public async Task ReturnStatementTypeMismatch()
        {
            await TestMissingInRegularAndScriptAsync(
            @"
class Program
{
    class Base {}
    class Derived : Base {}

    IEnumerable<Derived> returnBase() {
        Base b;
        return b[||];
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
        public async Task SimpleFunctionArgumentsWithObject()
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
using System.Threading.Tasks;

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
using System.Threading.Tasks;

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
using System.Threading.Tasks;

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
using System.Collections.Generic;

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
using System.Collections.Generic;

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
using System.Collections.Generic;

class Program
{
    class Base {}
    class Derived : Base {}
    class Test {
        Private void testing(Derived d) {}
    }

    void M() {
        Base b;
        Test t;
        t.testing(b[||]);
    }
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        public async Task BaseConstructorArgument()
        {
            await TestInRegularAndScriptAsync(
            @"
using System.Collections.Generic;

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
using System.Collections.Generic;

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
using System.Collections.Generic;

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
using System.Collections.Generic;

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
    }
}
