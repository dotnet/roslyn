// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveUnnecessaryCast
{
    public partial class RemoveUnnecessaryCastTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpRemoveUnnecessaryCastDiagnosticAnalyzer(), new RemoveUnnecessaryCastCodeFixProvider());
        }

        [WorkItem(545979)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToErrorType()
        {
            TestMissing(
            @"
class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in [|(ErrorType)s|]) { }
    }
}
");
        }

        [WorkItem(545137), WorkItem(870550)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void ParenthesizeToKeepParseTheSame1()
        {
            Test(
            @"
class Program
{
    static void Main()
    {
        int x = 2;
        int i = 1;
        Foo(x < [|(int)i|], x > (2 + 3));
    }
 
    static void Foo(bool a, bool b) { }
}",

            @"
class Program
{
    static void Main()
    {
        int x = 2;
        int i = 1;
        Foo((x < i), x > (2 + 3));
    }
 
    static void Foo(bool a, bool b) { }
}",

            index: 0,
            compareTokens: false);
        }

        [WorkItem(545146)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void ParenthesizeToKeepParseTheSame2()
        {
            Test(
            @"
using System;
 
class C
{
    static void Main()
    {
        Action a = Console.WriteLine;
        ([|(Action)a|])();
    }
}",

            @"
using System;
 
class C
{
    static void Main()
    {
        Action a = Console.WriteLine;
        a();
    }
}",

            index: 0,
            compareTokens: false);
        }

        [WorkItem(545160)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void ParenthesizeToKeepParseTheSame3()
        {
            Test(
            @"
using System;
 
class Program
{
    static void Main()
    {
        var x = (Decimal)[|(int)-1|];
    }
}",

            @"
using System;
 
class Program
{
    static void Main()
    {
        var x = (Decimal)(-1);
    }
}",

            index: 0,
            compareTokens: false);
        }

        [WorkItem(545138)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveTypeParameterCastToObject()
        {
            TestMissing(
            @"
class Ð¡
{
    void Foo<T>(T obj)
    {
        int x = (int)[|(object)obj|];
    }
}
");
        }

        [WorkItem(545139)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastInIsTest()
        {
            TestMissing(
            @"
using System;

class Ð¡
{
    static void Main()
    {
        DayOfWeek[] a = { };
        Console.WriteLine([|(object)a|] is int[]);
    }
}
");
        }

        [WorkItem(545142)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastNeedForUserDefinedOperator()
        {
            TestMissing(
            @"
class A
{
    public static implicit operator A(string x)
    {
        return new A();
    }
}
 
class Program
{
    static void Main()
    {
        A x = [|(string)null|];
    }
}
");
        }

        [WorkItem(545143)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemovePointerCast1()
        {
            TestMissing(
            @"
unsafe class C
{
    static unsafe void Main()
    {
        var x = (int)[|(int*)null|];
    }
}
");
        }

        [WorkItem(545144)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToObjectFromDelegateComparison()
        {
            // The cast below can't be removed because it would result in the Delegate
            // op_Equality operator overload being used over reference equality.

            TestMissing(
            @"
using System;

class Program
{
    static void Main()
    {
        Action a = Console.WriteLine;
        Action b = Console.WriteLine;
        Console.WriteLine(a == [|(object)b|]);
    }
}
");
        }

        [WorkItem(545145)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToAnonymousMethodWhenOnLeftOfAsCast()
        {
            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        var x = [|(Action)delegate { }|] as Action;
    }
}
");
        }

        [WorkItem(545147)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastInFloatingPointOperation()
        {
            TestMissing(
            @"
class C
{
    static void Main()
    {
        int x = 1;
        double y = [|(double)x|] / 2;
    }
}
");
        }

        [WorkItem(545157)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveIdentityCastWhichAffectsOverloadResolution1()
        {
            TestMissing(
            @"
using System;
 
class Program
{
    static void Main()
    {
        Foo(x => [|(int)x|]);
    }
 
    static void Foo(Func<int, object> x) { }
    static void Foo(Func<string, object> x) { }
}
");
        }

        [WorkItem(545158)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveIdentityCastWhichAffectsOverloadResolution2()
        {
            TestMissing(
            @"
using System;
 
class Program
{
    static void Main()
    {
        var x = [|(IComparable<int>)1|];
        Foo(x);
    }
 
    static void Foo(IComparable<int> x) { }
    static void Foo(int x) {}
}
");
        }

        [WorkItem(545158)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveIdentityCastWhichAffectsOverloadResolution3()
        {
            TestMissing(
            @"
using System;
 
class Program
{
    static void Main()
    {
        var x = [|(IComparable<int>)1|];
        var y = x;
        Foo(y);
    }
 
    static void Foo(IComparable<int> x) { }
    static void Foo(int x) {}
}
");
        }

        [WorkItem(545747)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastWhichChangesTypeOfInferredLocal()
        {
            TestMissing(
            @"
class C
{
    static void Main()
    {
        var x = [|(long)1|];
        x = long.MaxValue;
    }
}
");
        }

        [WorkItem(545159)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNeededCastToIListOfObject()
        {
            TestMissing(
            @"
using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        Action<object>[] x = { };
        Foo(x);
    }
 
    static void Foo<T>(Action<T>[] x)
    {
        var y = (IList<Action<object>>)[|(IList<object>)x|];
        Console.WriteLine(y.Count);
    }
}
");
        }

        [WorkItem(545287), WorkItem(880752)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInParameterDefaultValue()
        {
            Test(
            @"
class Program
{
    static void M1(int? i1 = [|(int?)null|])
    {
    }
}",

@"
class Program
{
    static void M1(int? i1 = null)
    {
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545289)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInReturnStatement()
        {
            Test(
            @"
class Program
{
    static long M2()
    {
        return [|(long)5|];
    }
}",

@"
class Program
{
    static long M2()
    {
        return 5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545288)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInLambda1()
        {
            Test(
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => [|(long)5|];
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => 5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545288)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInLambda2()
        {
            Test(
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => { return [|(long)5|]; };
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => { return 5; };
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545288)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInLambda3()
        {
            Test(
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = _ => { return [|(long)5|]; };
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = _ => { return 5; };
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545288)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInLambda4()
        {
            Test(
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = _ => [|(long)5|];
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = _ => 5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545291)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInConditionalExpression1()
        {
            Test(
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? [|(long)4|] : (long)5;
    }
}",

@"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? 4 : (long)5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545291)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInConditionalExpression2()
        {
            Test(
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? (long)4 : [|(long)5|];
    }
}",

@"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? (long)4 : 5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545291)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInConditionalExpression3()
        {
            Test(
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? 4 : [|(long)5|];
    }
}",

@"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? 4 : 5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545291)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNeededCastInConditionalExpression()
        {
            TestMissing(
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        var f1 = (b == 5) ? 4 : [|(long)5|];
    }
}");
        }

        [WorkItem(545291)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInConditionalExpression4()
        {
            Test(
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        var f1 = (b == 5) ? (long)4 : [|(long)5|];
    }
}",

@"
class Test
{
    public static void Main()
    {
        int b = 5;

        var f1 = (b == 5) ? (long)4 : 5;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545459)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInsideADelegateConstructor()
        {
            Test(
            @"
using System;
class Test
{
    delegate void D(int x);

    static void Main(string[] args)
    {
        var cd1 = new D([|(Action<int>)M1|]);
    }

    public static void M1(int i) { }
}",

@"
using System;
class Test
{
    delegate void D(int x);

    static void Main(string[] args)
    {
        var cd1 = new D(M1);
    }

    public static void M1(int i) { }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545419)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveTriviaWhenRemovingCast()
        {
            Test(
            @"
using System;
class Test
{
    public static void Main()
    {
        Func<Func<int>> f2 = () =>
        {
            return [|(Func<int>)(/*Lambda returning int const*/() => 5 /*Const returned is 5*/)|];
        };
    }
}",

@"
using System;
class Test
{
    public static void Main()
    {
        Func<Func<int>> f2 = () =>
        {
            return /*Lambda returning int const*/() => 5 /*Const returned is 5*/;
        };
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545422)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInsideCaseLabel()
        {
            Test(
            @"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case [|(long)5|]:
                break;
        }
    }
}",

@"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case 5:
                break;
        }
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545578)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInsideGotoCaseStatement()
        {
            Test(
            @"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case 5:
                goto case [|(long)5|];
                break;
        }
    }
}",

@"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case 5:
                goto case 5;
                break;
        }
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545595)]
        [Fact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInCollectionInitializer()
        {
            Test(
            @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var z = new List<long> { [|(long)0|] };
    }
}",

@"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var z = new List<long> { 0 };
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(529787)]
        [Fact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastWhichInCollectionInitializer1()
        {
            TestMissing(
            @"
using System;
using System.Collections.Generic;

class X : List<int>
{
    void Add(object x) { Console.WriteLine(1); }
    void Add(string x) { Console.WriteLine(2); }
 
    static void Main()
    {
        var z = new X { [|(object)""""|] };
    }
}
");
        }

        [WorkItem(529787)]
        [Fact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastWhichInCollectionInitializer2()
        {
            TestMissing(
            @"
using System;
using System.Collections.Generic;

class X : List<int>
{
    void Add(object x) { Console.WriteLine(1); }
    void Add(string x) { Console.WriteLine(2); }

    static void Main()
    {
        X z = new X { [|(object)""""|] };
    }
}
");
        }

        [WorkItem(545607)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastInArrayInitializer()
        {
            Test(
            @"
class X
{
    static void Foo()
    {
        string x = "";
        var s = new object[] { [|(object)x|] };
    }
}",

@"
class X
{
    static void Foo()
    {
        string x = "";
        var s = new object[] { x };
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545616)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnneededCastWithOverloadedBinaryOperator()
        {
            Test(
            @"
using System;
class MyAction
{
    static void Foo()
    {
        MyAction x = null;
        var y = x + [|(Action)delegate { }|];
    }
 
    public static MyAction operator +(MyAction x, Action y)
    {
        throw new NotImplementedException();
    }
}",

@"
using System;
class MyAction
{
    static void Foo()
    {
        MyAction x = null;
        var y = x + delegate { };
    }
 
    public static MyAction operator +(MyAction x, Action y)
    {
        throw new NotImplementedException();
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545822)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastShouldInsertWhitespaceWhereNeededToKeepCorrectParsing()
        {
            Test(
            @"
using System;
 
class Program
{
    static void Foo<T>()
    {
        Action a = null;
        var x = [|(Action)(Foo<Guid>)|]==a;
    }
}",

@"
using System;
 
class Program
{
    static void Foo<T>()
    {
        Action a = null;
        var x = (Foo<Guid>) == a;
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545560)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastWithExplicitUserDefinedConversion()
        {
            TestMissing(
            @"
using System;
class A
{
    public static explicit operator long (A x)
    {
        return 1;
    }

    public static implicit operator int (A x)
    {
        return 2;
    }

    static void Main()
    {
        var a = new A();

        long x = [|(long)a|];
        long y = a;

        Console.WriteLine(x); // 1
        Console.WriteLine(y); // 2
    }
}");
        }

        [WorkItem(545608)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastWithImplicitUserDefinedConversion()
        {
            TestMissing(
            @"
class X
{
    static void Foo()
    {
        X x = null;
        object y = [|(string)x|];
    }
 
    public static implicit operator string (X x)
    {
        return "";
    }
}");
        }

        [WorkItem(545941)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastWithImplicitConversionInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            TestMissing(
            @"
using System;

class E
{
    public static implicit operator Exception(E e)
    {
        return new Exception();
    }

    static void Main()
    {
        throw [|(Exception)new E()|];
    }
}
");
        }

        [WorkItem(545981)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        object ex = new Exception();
        throw [|(Exception)ex|];
    }
}
");
        }

        [WorkItem(545941)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastInThrow()
        {
            Test(
            @"
using System;

class E
{
    static void Main()
    {
        throw [|(Exception)new Exception()|];
    }
}
",

@"
using System;

class E
{
    static void Main()
    {
        throw new Exception();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545945)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryDowncast()
        {
            TestMissing(
            @"
class C
{
    void Foo(object y)
    {
        int x = [|(int)y|];
    }
}
");
        }

        [WorkItem(545591)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastWithinLambda()
        {
            TestMissing(
            @"
using System;
class Program
{
    static void Main()
    {
        Boo(x => Foo(x, y => [|(int)x|]), null); 
    }
    static void Boo(Action<int> x, object y) { Console.WriteLine(1); }
    static void Boo(Action<string> x, string y) { Console.WriteLine(2); }
    static void Foo(int x, Func<int, int> y) { }
    static void Foo(string x, Func<string, string> y) { }
    static void Foo(string x, Func<int, int> y) { }
}
");
        }

        [WorkItem(545606)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastFromNullToTypeParameter()
        {
            TestMissing(
            @"
class X
{
    static void Foo<T, S>() where T : class, S
    {
        S y = [|(T) null|];
    }
}
");
        }

        [WorkItem(545744)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInImplicitlyTypedArray()
        {
            TestMissing(
            @"
class X
{
    static void Foo()
    {
        string x = "";
        var s = new [] { [|(object)x|] };
        s[0] = 1;
    }
}
");
        }

        [WorkItem(545750)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastToBaseType()
        {
            Test(
            @"
class X
{
    static void Main()
    {
        var s = ([|(object)new X()|]).ToString();
    }

    public override string ToString()
    {
        return "";
    }
}",

@"
class X
{
    static void Main()
    {
        var s = new X().ToString();
    }

    public override string ToString()
    {
        return "";
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545855)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryLambdaToDelegateCast()
        {
            Test(
            @"
using System;
using System.Collections.Generic;
using System.Reflection;

static class Program
{
    static void Main()
    {
        FieldInfo[] fields = typeof(Exception).GetFields();
        Console.WriteLine(fields.Any([|(Func<FieldInfo, bool>)(field => field.IsStatic)|]));
    }

    static bool Any<T>(this IEnumerable<T> s, Func<T, bool> predicate)
    {
        return false;
    }

    static bool Any<T>(this ICollection<T> s, Func<T, bool> predicate)
    {
        return true;
    }
}
",

@"
using System;
using System.Collections.Generic;
using System.Reflection;

static class Program
{
    static void Main()
    {
        FieldInfo[] fields = typeof(Exception).GetFields();
        Console.WriteLine(fields.Any(field => field.IsStatic));
    }

    static bool Any<T>(this IEnumerable<T> s, Func<T, bool> predicate)
    {
        return false;
    }

    static bool Any<T>(this ICollection<T> s, Func<T, bool> predicate)
    {
        return true;
    }
}
",
    parseOptions: null,
    index: 0,
    compareTokens: false);
        }

        [WorkItem(529816)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastInQueryExpression()
        {
            Test(
            @"
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select [|(long)0|]);
    }
}",

@"
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select 0);
    }
}",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(529816)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInQueryExpression()
        {
            TestMissing(
            @"
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }
    int Select(Func<int, int> x) { return 2; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select [|(long)0|]);
    }
}
");
        }

        [WorkItem(545848)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInConstructorInitializer()
        {
            TestMissing(
            @"
using System;

class C
{
    static void Foo(int x, Func<int, int> y) { }
    static void Foo(string x, Func<string, string> y) { }

    C(Action<int> x, object y) { Console.WriteLine(1); }
    C(Action<string> x, string y) { Console.WriteLine(2); }

    C() : this(x => Foo(x, y => [|(int)x|]), null) { }

    static void Main() { new C(); }
}
");
        }

        [WorkItem(529831)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastFromTypeParameterToInterface()
        {
            TestMissing(
            @"
using System;

interface IIncrementable
{
    int Value { get; }
    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

class C: IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

static class Program
{
    static void Main()
    {
        Foo(new S(), new C());
    }

    static void Foo<TAny, TClass>(TAny x, TClass y) 
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ([|(IIncrementable)x|]).Increment(); // False Unnecessary Cast
        ((IIncrementable)y).Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}
");
        }

        [WorkItem(529831)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastFromTypeParameterToInterface()
        {
            Test(
            @"
using System;

interface IIncrementable
{
    int Value { get; }
    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

class C: IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

static class Program
{
    static void Main()
    {
        Foo(new S(), new C());
    }

    static void Foo<TAny, TClass>(TAny x, TClass y) 
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        ([|(IIncrementable)y|]).Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}
",
 @"
using System;

interface IIncrementable
{
    int Value { get; }
    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

class C: IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

static class Program
{
    static void Main()
    {
        Foo(new S(), new C());
    }

    static void Foo<TAny, TClass>(TAny x, TClass y) 
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        y.Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545877)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontCrashOnIncompleteMethodDeclaration()
        {
            TestMissing(
            @"
using System;

class A
{
    static void Main()
    {
        byte
        Foo(x => 1, [|(byte)1|]);
    }

    static void Foo<T, S>(T x, ) { }
}
");
        }

        [WorkItem(545777)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveImportantTrailingTrivia()
        {
            Test(
            @"
class Program
{
    static void Main()
    {
        long x =
#if true
            [|(long) // Remove Unnecessary Cast
#endif
            1|];
    }
}
",

            @"
class Program
{
    static void Main()
    {
        long x =
#if true
            // Remove Unnecessary Cast
#endif
            1;
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(529791)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastToNullable1()
        {
            Test(
            @"
class X
{
    static void Foo()
    {
        object x = (string)null;
        object y = [|(int?)null|];
    }
}
",

            @"
class X
{
    static void Foo()
    {
        object x = (string)null;
        object y = null;
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545842)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastToNullable2()
        {
            Test(
            @"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + [|(long?) y|];
    }
}
",

            @"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + y;
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545850)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveSurroundingParentheses()
        {
            Test(
            @"
class Program
{
    static void Main()
    {
        int x = 1;
        ([|(int)x|]).ToString();
    }
}
",

            @"
class Program
{
    static void Main()
    {
        int x = 1;
        x.ToString();
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(529846)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastFromTypeParameterToObject()
        {
            TestMissing(
            @"
class C
{
    static void Foo<T>(T x, object y)
    {
        if ([|(object)x|] == y) { }
    }
}
");
        }

        [WorkItem(545858)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastFromDelegateTypeToMulticastDelegate()
        {
            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        Action x = Console.WriteLine;
        Action y = Console.WriteLine;
        Console.WriteLine([|(MulticastDelegate)x|] == y);
    }
}
");
        }

        [WorkItem(545857)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInSizeOfArrayCreationExpression1()
        {
            // The cast below can't be removed because it would result in the implicit
            // conversion to int being called instead.

            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(new int[[|(long)default(C)|]].Length);
    }

    public static implicit operator long (C x)
    {
        return 1;
    }

    public static implicit operator int (C x)
    {
        return 2;
    }
}
");
        }

        [WorkItem(545980)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInSizeOfArrayCreationExpression2()
        {
            // Array bounds must be an int, so the cast below can't be removed.

            TestMissing(
            @"
class C
{
    static void Main()
    {
        var a = new int[[|(int)decimal.Zero|]];
    }
}
");
        }

        [WorkItem(529842)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInTernaryExpression()
        {
            TestMissing(
            @"
using System;

class X
{
    public static implicit operator string (X x)
    {
        return x.ToString();
    }

    static void Main()
    {
        bool b = true;
        X x = new X();
        Console.WriteLine(b ? [|(string)null|] : x);
    }
}
");
        }

        [WorkItem(545882), WorkItem(880752)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastInConstructorInitializer1()
        {
            Test(
@"
class C
{
    C(int x) { }
    C() : this([|(int)1|]) { }
}
",

@"
class C
{
    C(int x) { }
    C() : this(1) { }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545958), WorkItem(880752)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastInConstructorInitializer2()
        {
            Test(
@"
using System.Collections;

class C
{
    C(int x) { }
    C(object x) { }
    C() : this([|(IEnumerable)""""|]) { }
}
",

@"
using System.Collections;

class C
{
    C(int x) { }
    C(object x) { }
    C() : this("""") { }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545957)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastInConstructorInitializer3()
        {
            TestMissing(
@"
class C
{
    C(int x) { }
    C() : this([|(long)1|]) { }
}
");
        }

        [WorkItem(545842)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToNullableInArithmeticExpression()
        {
            Test(
@"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + [|(long?)y|];
    }
}
",

@"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + y;
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545942)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastFromValueTypeToObjectInReferenceEquality()
        {
            // Note: The cast below can't be removed because it would result in an
            // illegal reference equality test between object and a value type.

            TestMissing(
            @"
using System;

class Program
{
    static void Main()
    {
        object x = 1;
        Console.WriteLine(x == [|(object)1|]);
    }
}
");
        }

        [WorkItem(545962)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastWhenExpressionDoesntBind()
        {
            // Note: The cast below can't be removed because its expression doesn't bind.

            TestMissing(
            @"
using System;
 
class Program
{
    static void Main()
    {
        ([|(IDisposable)x|]).Dispose();
    }
}

");
        }

        [WorkItem(545944)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastBeforePointerDereference1()
        {
            // Note: The cast below can't be removed because it would result in *null,
            // which is illegal.

            TestMissing(
            @"
unsafe class C
{
    int x = *[|(int*)null|];
}
");
        }

        [WorkItem(545978)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastBeforePointerDereference2()
        {
            // Note: The cast below can't be removed because it would result in dereferencing
            // void*, which is illegal.

            TestMissing(
            @"
unsafe class C
{
    static void Main()
    {
        void* p = null;
        int x = *[|(int*)p|];
    }
}
");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastBeforePointerDereference3()
        {
            // Conservatively disable cast simplifications for casts involving pointer conversions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            TestMissing(
            @"
class C
{
    public unsafe float ReadSingle(byte* ptr)
    {
        return *[|(float*)ptr|];
    }
}
");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNumericCastInUncheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            TestMissing(
            @"
class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        if (unchecked([|(uint)byteCount)|] > (_endPointer - _currentPointer))
        {
        }
    }
}
");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNumericCastInUncheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            TestMissing(
            @"
class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        unchecked
        {
            if (([|(uint)byteCount)|] > (_endPointer - _currentPointer))
            {
            }
        }
    }
}
");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNumericCastInCheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            TestMissing(
            @"
class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        if (checked([|(uint)byteCount)|] > (_endPointer - _currentPointer))
        {
        }
    }
}
");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNumericCastInCheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            TestMissing(
            @"
class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        checked
        {
            if (([|(uint)byteCount)|] > (_endPointer - _currentPointer))
            {
            }
        }
    }
}
");
        }

        [WorkItem(545894)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInAttribute()
        {
            TestMissing(
            @"
using System;

[A([|(byte)0)|]]
class A : Attribute
{
    public A(object x) {  }
}
");
        }

        #region Interface Casts

        [WorkItem(545889)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToInterfaceForUnsealedType()
        {
            // Note: The cast below can't be removed because X is not sealed.

            TestMissing(
            @"
using System;

class X : IDisposable
{
    static void Main()
    {
        X x = new Y();
        ([|(IDisposable)x|]).Dispose();
    }
    public void Dispose()
    {
        Console.WriteLine(""X.Dispose"");
    }
}

class Y : X, IDisposable
{
    void IDisposable.Dispose()
    {
        Console.WriteLine(""Y.Dispose"");
    }
}
");
        }

        [WorkItem(545890)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToInterfaceForSealedType1()
        {
            // Note: The cast below can be removed because C is sealed and the
            // unspecified optional parameters of I.Foo() and C.Foo() have the
            // same default values.

            Test(
@"
using System;

interface I
{
    void Foo(int x = 0);
}

sealed class C : I
{
    public void Foo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Foo();
    }
}
",

@"
using System;

interface I
{
    void Foo(int x = 0);
}

sealed class C : I
{
    public void Foo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Foo();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545890)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToInterfaceForSealedType2()
        {
            // Note: The cast below can be removed because C is sealed and the
            // interface member has no parameters.

            Test(
@"
using System;

interface I
{
    string Foo { get; }
}

sealed class C : I
{
    public string Foo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)new C()|]).Foo);
    }
}
",

@"
using System;

interface I
{
    string Foo { get; }
}

sealed class C : I
{
    public string Foo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(new C().Foo);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545890)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToInterfaceForSealedType3()
        {
            // Note: The cast below can be removed because C is sealed and the
            // interface member has no parameters.

            Test(
@"
using System;

interface I
{
    string Foo { get; }
}

sealed class C : I
{
    public C Instance { get { return new C(); } }

    public string Foo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)Instance|]).Foo);
    }
}
",

@"
using System;

interface I
{
    string Foo { get; }
}

sealed class C : I
{
    public C Instance { get { return new C(); } }

    public string Foo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(Instance.Foo);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545890)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToInterfaceForSealedType4()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the unspecified optional parameter default values differ.

            TestMissing(
            @"
using System;

interface I
{
    void Foo(int x = 0);
}

sealed class C : I
{
    public void Foo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Foo();
    }
}
");
        }

        [WorkItem(545890)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToInterfaceForSealedType5()
        {
            // Note: The cast below can be removed (even though C is sealed)
            // because the optional parameters whose default values differ are
            // specified.

            Test(
@"
using System;

interface I
{
    void Foo(int x = 0);
}

sealed class C : I
{
    public void Foo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Foo(2);
    }
}
",

@"
using System;

interface I
{
    void Foo(int x = 0);
}

sealed class C : I
{
    public void Foo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Foo(2);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545888)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToInterfaceForSealedType6()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            TestMissing(
            @"
using System;

interface I
{
    void Foo(int x = 0, int y = 0);
}

sealed class C : I
{
    public void Foo(int y = 0, int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Foo(x: 1);
    }
}
");
        }

        [WorkItem(545888)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToInterfaceForSealedType7()
        {
            Test(
@"
using System;

interface I
{
    int this[int x = 0, int y = 0] { get; }
}

sealed class C : I
{
    public int this[int x = 0, int y = 0]
    {
        get
        {
            return x * 2;
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)new C()|])[x: 1]);
    }
}
",

@"
using System;

interface I
{
    int this[int x = 0, int y = 0] { get; }
}

sealed class C : I
{
    public int this[int x = 0, int y = 0]
    {
        get
        {
            return x * 2;
        }
    }

    static void Main()
    {
        Console.WriteLine(new C()[x: 1]);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545888)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToInterfaceForSealedType8()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            TestMissing(
            @"
using System;

interface I
{
    int this[int x = 0, int y = 0] { get; }
}

sealed class C : I
{
    public int this(int y = 0, int x = 0)
    {
        get
        {
            return x * 2;
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)new C()|])[x: 1]);
    }
}
");
        }

        [WorkItem(545883)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToInterfaceForSealedType9()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because it would result in binding to a Dispose method that doesn't
            // implement IDisposable.Dispose().

            TestMissing(
            @"
using System;
using System.IO;

sealed class C : MemoryStream
{
    static void Main()
    {
        C s = new C();
        ([|(IDisposable)s|]).Dispose();
    }

    new public void Dispose()
    {
        Console.WriteLine(""new Dispose()"");
    }
}
");
        }

        [WorkItem(545887)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToInterfaceForStruct1()
        {
            // Note: The cast below can't be removed because the cast boxes 's' and
            // unboxing would change program behavior.

            TestMissing(
            @"
using System;

interface IIncrementable
{
    int Value { get; }
    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }

    static void Main()
    {
        var s = new S();
        ([|(IIncrementable)s|]).Increment();
        Console.WriteLine(s.Value);
    }
}
");
        }

        [WorkItem(545834)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToInterfaceForStruct2()
        {
            // Note: The cast below can be removed because we are sure to have
            // a fresh copy of the struct from the GetEnumerator() method.

            Test(
            @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        ([|(IDisposable)GetEnumerator()|]).Dispose();
    }

    static List<int>.Enumerator GetEnumerator()
    {
        var x = new List<int> { 1, 2, 3 };
        return x.GetEnumerator();
    }
}
",

@"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        GetEnumerator().Dispose();
    }

    static List<int>.Enumerator GetEnumerator()
    {
        var x = new List<int> { 1, 2, 3 };
        return x.GetEnumerator();
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(544655)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToICloneableForDelegate()
        {
            // Note: The cast below can be removed because delegates are implicitly
            // sealed.

            Test(
            @"
using System;

class C
{
    static void Main()
    {
        Action a = () => { };
        var c = ([|(ICloneable)a|]).Clone();
    }
}
",

@"
using System;

class C
{
    static void Main()
    {
        Action a = () => { };
        var c = a.Clone();
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(545926)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToICloneableForArray()
        {
            // Note: The cast below can be removed because arrays are implicitly
            // sealed.

            Test(
            @"
using System;

class C
{
    static void Main()
    {
        var a = new[] { 1, 2, 3 };
        var c = ([|(ICloneable)a|]).Clone(); 
    }
}
",

@"
using System;

class C
{
    static void Main()
    {
        var a = new[] { 1, 2, 3 };
        var c = a.Clone(); 
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WorkItem(529897)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToIConvertibleForEnum()
        {
            // Note: The cast below can be removed because enums are implicitly
            // sealed.

            Test(
            @"
using System;

class Program
{
    static void Main()
    {
        Enum e = DayOfWeek.Monday;
        var y = ([|(IConvertible)e|]).GetTypeCode();
    }
}
",

@"
using System;

class Program
{
    static void Main()
    {
        Enum e = DayOfWeek.Monday;
        var y = e.GetTypeCode();
    }
}
",
    index: 0,
    compareTokens: false);
        }

        #endregion

        #region ParamArray Parameter Casts

        [WorkItem(545141)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToObjectInParamArrayArg1()
        {
            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        Foo([|(object)null|]);
    }

    static void Foo(params object[] x)
    {
        Console.WriteLine(x.Length);
    }
}
");
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToIntArrayInParamArrayArg2()
        {
            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        Foo([|(int[])null|]);
    }

    static void Foo(params object[] x)
    {
        Console.WriteLine(x.Length);
    }
}
");
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToObjectArrayInParamArrayArg3()
        {
            TestMissing(
            @"
using System;

class C
{
    static void Main()
    {
        Foo([|(object[])null|]);
    }

    static void Foo(params object[][] x)
    {
        Console.WriteLine(x.Length);
    }
}
");
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToObjectArrayInParamArrayArg1()
        {
            Test(
            @"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo([|(object[])null|]);
    }
}
",

@"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo(null);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToStringArrayInParamArrayArg2()
        {
            Test(
            @"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo([|(string[])null|]);
    }
}
",

@"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo(null);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToIntArrayInParamArrayArg3()
        {
            Test(
            @"
class C
{
    static void Foo(params int[] x) { }

    static void Main()
    {
        Foo([|(int[])null|]);
    }
}
",

@"
class C
{
    static void Foo(params int[] x) { }

    static void Main()
    {
        Foo(null);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToObjectArrayInParamArrayArg4()
        {
            Test(
            @"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo([|(object[])null|], null);
    }
}
",

@"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo(null, null);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529911)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToObjectInParamArrayArg5()
        {
            Test(
            @"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo([|(object)null|], null);
    }
}
",

@"
class C
{
    static void Foo(params object[] x) { }

    static void Main()
    {
        Foo(null, null);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastToObjectArrayInParamArrayWithNamedArgument()
        {
            Test(
                @"
class C
{
    static void Main()
    {
        Foo(x: [|(object[])null|]);
    }

    static void Foo(params object[] x) { }
}
",
                @"
class C
{
    static void Main()
    {
        Foo(x: null);
    }

    static void Foo(params object[] x) { }
}
",
            index: 0,
            compareTokens: false);
        }

        #endregion

        #region ForEach Statements

        [WorkItem(545961)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInForEach1()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            TestMissing(
            @"
using System.Collections;

class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in [|(IEnumerable)s|]) { }
    }
}
");
        }

        [WorkItem(545961)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInForEach2()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            TestMissing(
            @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in [|(IEnumerable<char>)s|]) { }
    }
}
");
        }

        [WorkItem(545961)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInForEach3()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement since C doesn't contain a GetEnumerator()
            // method.

            TestMissing(
            @"
using System.Collections;

class D
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}

class C
{
    public static implicit operator D(C c)
    {
        return new D();
    }

    static void Main()
    {
        object s = "";
        foreach (object x in [|(D)new C()|]) { }
    }
}");
        }

        [WorkItem(545961)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInForEach4()
        {
            // The cast below can't be removed because it would result in
            // C.GetEnumerator() being called rather than D.GetEnumerator().

            TestMissing(
            @"
using System;
using System.Collections;

class D
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}

class C
{
    public IEnumerator GetEnumerator()
    {
        yield return 2;
    }

    public static implicit operator D(C c)
    {
        return new D();
    }

    static void Main()
    {
        object s = "";
        foreach (object x in [|(D)new C()|])
        {
            Console.WriteLine(x);
        }
    }
}");
        }

        [WorkItem(545961)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInForEach5()
        {
            // The cast below can't be removed because it would change the
            // type of 'x'.

            TestMissing(
            @"
using System;
 
class Program
{
    static void Main()
    {
        string[] s = { ""A"" };
        foreach (var x in [|(Array)s|])
        {
            var y = x;
            y = 1;
        }
    }
}
");
        }

        #endregion

        [WorkItem(545925)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastIfOverriddenMethodHasIncompatibleParameterList()
        {
            // Note: The cast below can't be removed because the parameter list
            // of Foo and its override have different default values.

            TestMissing(
            @"
using System;

abstract class Y
{
    public abstract void Foo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        ([|(Y)new X()|]).Foo();
    }

    public override void Foo(int x = 2)
    {
        Console.WriteLine(x);
    }
}
");
        }

        [WorkItem(545925)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastIfOverriddenMethodHaveCompatibleParameterList()
        {
            // Note: The cast below can be removed because the parameter list
            // of Foo and its override have the same default values.

            Test(
@"
using System;

abstract class Y
{
    public abstract void Foo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        ([|(Y)new X()|]).Foo();
    }

    public override void Foo(int x = 1)
    {
        Console.WriteLine(x);
    }
}
",

@"
using System;

abstract class Y
{
    public abstract void Foo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        new X().Foo();
    }

    public override void Foo(int x = 1)
    {
        Console.WriteLine(x);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529916)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveCastInReceiverForMethodGroup()
        {
            // Note: The cast below can be removed because the it results in
            // the same method group.

            Test(
@"
using System;

static class Program
{
    static void Main()
    {
        Action a = ([|(string)""""|]).Foo;
    }

    static void Foo(this string x) { }
}
",

@"
using System;

static class Program
{
    static void Main()
    {
        Action a = """".Foo;
    }

    static void Foo(this string x) { }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(609497)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void Bugfix_609497()
        {
            TestMissing(
@"
using System;
using System.Threading.Tasks;
 
class Program
{
    static void Main()
    {
        Foo().Wait();
    }
 
    static async Task Foo()
    {
        Task task = Task.FromResult(0);
        Console.WriteLine(await [|(dynamic)task|]);
    }
}

");
        }

        [WorkItem(545995)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToDifferentTypeWithSameName()
        {
            // Note: The cast below cannot be removed because the it results in
            // a different overload being picked.

            TestMissing(
@"
using System;
using MyInt = System.Int32;

namespace System
{
    public struct Int32
    {
        public static implicit operator Int32(int x)
        {
            return default(Int32);
        }
    }
}

class A
{
    static void Foo(int x) { Console.WriteLine(""int""); }
    static void Foo(MyInt x) { Console.WriteLine(""MyInt""); }
    static void Main()
    {
        Foo([|(MyInt)0|]);
    }
}
");
        }

        [WorkItem(545921)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastWhichWouldChangeAttributeOverloadResolution1()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            TestMissing(
@"
using System;

[Flags]
enum EEEnum
{
    Flag1 = 0x2,
    Flag2 = 0x1,
}

class MyAttributeAttribute : Attribute
{
    public MyAttributeAttribute(EEEnum e) { }
    public MyAttributeAttribute(short e) { }
 
    public void Foo(EEEnum e) { }
    public void Foo(short e) { }
 
    [MyAttribute([|(EEEnum)0x0|])]
    public void Bar()
    {
        Foo((EEEnum)0x0);
    }
}
");
        }

        [WorkItem(608180)]
        [WorkItem(624252)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastIfArgumentIsRestricted_TypedReference()
        {
            TestMissing(
@"
using System;

class Program
{
    static void Main(string[] args)
    {
        
    }

    static void v(dynamic x)
    {
        var y = default(TypedReference);
        dd([|(object)x|], y);
    }

    static void dd(object obj, TypedReference d)
    {

    }
}
");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithOtherDynamicArguments()
        {
            TestMissing(
@"
using System;
 
class Program
{
    static void Main()
    {
        C<string>.InvokeFoo(0);
    }
}
 
class C<T>
{
    public static void InvokeFoo(dynamic x)
    {
        Console.WriteLine(Foo(x, [|(object)""""|], """"));
    }
 
    static void Foo(int x, string y, T z) { }
    static bool Foo(int x, object y, object z) { return true; }
}

");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithOtherDynamicArguments_Bracketed()
        {
            TestMissing(
@"
class C<T>
{
    int this[int x, T s, string d = ""abc""] { get { return 0; } set { } }

    int this[int x, object s, object d] { get { return 0; } set { } }

    void Foo(dynamic xx)
    {
        var y = this[ x: xx, s: """", d: [|(object)""""|]];
    }
}
");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithDynamicReceiverOpt()
        {
            TestMissing(
@"
class C
{
    static bool Foo(dynamic d)
    {
        d([|(object)""""|]);
        return true;
    }
}
");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithDynamicReceiverOpt_1()
        {
            TestMissing(
@"
class C
{
    static bool Foo(dynamic d)
    {
        d.foo([|(object)""""|]);
        return true;
    }
}
");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithDynamicReceiverOpt_2()
        {
            TestMissing(
@"
class C
{
    static bool Foo(dynamic d)
    {
        d.foo.bar.foo([|(object)""""|]);
        return true;
    }
}
");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithDynamicReceiverOpt_3()
        {
            TestMissing(
@"
class C
{
    static bool Foo(dynamic d)
    {
        d.foo().bar().foo([|(object)""""|]);
        return true;
    }
}
");
        }

        [WorkItem(627107)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastOnArgumentsWithOtherDynamicArguments_1()
        {
            TestMissing(
@"
using System;
 
class Program
{
    static void Main()
    {
        C<string>.InvokeFoo(0);
    }
}

class C<T>
{
    public static void InvokeFoo(dynamic x)
    {
        Console.WriteLine(Foo([|(object)""""|], x, """"));
    }

    static void Foo(string y, int x,  T z) { }
    static bool Foo(object y, int x,  object z) { return true; }
}

");
        }

        [WorkItem(545998)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastWhichWouldChangeAttributeOverloadResolution2()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            TestMissing(
@"
using System;
 
[A(new[] { [|(long)0|] })]
class A : Attribute
{
    public A(long[] x) { }
}
");
        }

        [WorkItem(529894)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontUnnecessaryCastFromEnumToUint()
        {
            TestMissing(
@"
using System;

enum E
{
    X = -1
}

class C
{
    static void Main()
    {
        E x = E.X;
        Console.WriteLine([|(uint)|]x > 0);
    }
}
");
        }

        [WorkItem(529846)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontUnnecessaryCastFromTypeParameterToObject()
        {
            TestMissing(
@"
class C
{
    static void Foo<T>(T x, object y)
    {
        if ([|(object)|]x == y) { }
    }
}

");
        }

        [WorkItem(640136)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void RemoveUnnecessaryCastAndParseCorrect()
        {
            Test(
@"
using System;
using System.Threading.Tasks;
 
class C
{
    void Foo(Task<Action> x)
    {
        (([|(Task<Action>)x|]).Result)();
    }
}
",

@"
using System;
using System.Threading.Tasks;
 
class C
{
    void Foo(Task<Action> x)
    {
        x.Result();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(626026)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastIfUserDefinedExplicitCast()
        {
            TestMissing(
            @"
class Program
{
    static void Main(string[] args)
    {
        B bar = new B();
        A a = [|(A)bar|];
    }
}

public struct A
{
    public static explicit operator A(B b)
    {
        return new A();
    }
}

public struct B
{

}
");
        }

        [WorkItem(768895)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInTernary()
        {
            TestMissing(
            @"
class Program
{
    static void Main(string[] args)
    {
        object x = null;
        int y = [|(bool)x|] ? 1 : 0;
    }
}
");
        }

        [WorkItem(770187)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveNecessaryCastInSwitchExpression()
        {
            TestMissing(
            @"
namespace ConsoleApplication23
{
    class Program
    {
        static void Main(string[] args)
        {
            int foo = 0;
            switch ([|(E)foo|])
            {
                case E.A:
                case E.B:
                    return;
            }
        }
    }
    enum E
    {
        A,
        B,
        C
    }
}
");
        }

        [WorkItem(844482)]
        [WorkItem(2761, "https://github.com/dotnet/roslyn/issues/2761")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastFromBaseToDerivedWithExplicitReference()
        {
            TestMissing(
@"class Program
{
    static void Main(string[] args)
    {
        C x = null;
        C y = null;
        y = [|(D)x|];
    }
}

class C
{

}

class D : C
{

}");
        }

        [WorkItem(3254, "https://github.com/dotnet/roslyn/issues/3254")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToTypeParameterWithExceptionConstraint()
        {
            TestMissing(
@"using System;

class Program
{
    private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition)
            where TException : Exception
    {
        if (!condition)
        {
            throw [|(TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition)|];
        }
    }
}");
        }

        [WorkItem(3254, "https://github.com/dotnet/roslyn/issues/3254")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void DontRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
        {
            TestMissing(
@"using System;

class Program
{
    private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition)
            where TException : ArgumentException
    {
        if (!condition)
        {
            throw [|(TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition)|];
        }
    }
}");
        }
    }
}
