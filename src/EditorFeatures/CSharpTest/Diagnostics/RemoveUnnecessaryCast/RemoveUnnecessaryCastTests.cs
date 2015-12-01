// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToErrorType()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame1()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame2()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame3()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveTypeParameterCastToObject()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastInIsTest()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastNeedForUserDefinedOperator()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemovePointerCast1()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToObjectFromDelegateComparison()
        {
            // The cast below can't be removed because it would result in the Delegate
            // op_Equality operator overload being used over reference equality.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToAnonymousMethodWhenOnLeftOfAsCast()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastInFloatingPointOperation()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveIdentityCastWhichAffectsOverloadResolution1()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveIdentityCastWhichAffectsOverloadResolution2()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveIdentityCastWhichAffectsOverloadResolution3()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhichChangesTypeOfInferredLocal()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNeededCastToIListOfObject()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInParameterDefaultValue()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInReturnStatement()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda1()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda2()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda3()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda4()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression1()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression2()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression3()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNeededCastInConditionalExpression()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression4()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideADelegateConstructor()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveTriviaWhenRemovingCast()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideCaseLabel()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideGotoCaseStatement()
        {
            await TestAsync(
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
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInCollectionInitializer()
        {
            await TestAsync(
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
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWhichInCollectionInitializer1()
        {
            await TestMissingAsync(
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
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWhichInCollectionInitializer2()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInArrayInitializer()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastWithOverloadedBinaryOperator()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastShouldInsertWhitespaceWhereNeededToKeepCorrectParsing()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithExplicitUserDefinedConversion()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithImplicitUserDefinedConversion()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithImplicitConversionInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInThrow()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryDowncast()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithinLambda()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromNullToTypeParameter()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInImplicitlyTypedArray()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToBaseType()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryLambdaToDelegateCast()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInQueryExpression()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInQueryExpression()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInConstructorInitializer()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromTypeParameterToInterface()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastFromTypeParameterToInterface()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontCrashOnIncompleteMethodDeclaration()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveImportantTrailingTrivia()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToNullable1()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToNullable2()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveSurroundingParentheses()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromTypeParameterToObject()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromDelegateTypeToMulticastDelegate()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInSizeOfArrayCreationExpression1()
        {
            // The cast below can't be removed because it would result in the implicit
            // conversion to int being called instead.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInSizeOfArrayCreationExpression2()
        {
            // Array bounds must be an int, so the cast below can't be removed.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInTernaryExpression()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInConstructorInitializer1()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInConstructorInitializer2()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastInConstructorInitializer3()
        {
            await TestMissingAsync(
@"
class C
{
    C(int x) { }
    C() : this([|(long)1|]) { }
}
");
        }

        [WorkItem(545842)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToNullableInArithmeticExpression()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastFromValueTypeToObjectInReferenceEquality()
        {
            // Note: The cast below can't be removed because it would result in an
            // illegal reference equality test between object and a value type.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhenExpressionDoesntBind()
        {
            // Note: The cast below can't be removed because its expression doesn't bind.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastBeforePointerDereference1()
        {
            // Note: The cast below can't be removed because it would result in *null,
            // which is illegal.

            await TestMissingAsync(
            @"
unsafe class C
{
    int x = *[|(int*)null|];
}
");
        }

        [WorkItem(545978)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastBeforePointerDereference2()
        {
            // Note: The cast below can't be removed because it would result in dereferencing
            // void*, which is illegal.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastBeforePointerDereference3()
        {
            // Conservatively disable cast simplifications for casts involving pointer conversions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInUncheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInUncheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInCheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInCheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInAttribute()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForUnsealedType()
        {
            // Note: The cast below can't be removed because X is not sealed.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType1()
        {
            // Note: The cast below can be removed because C is sealed and the
            // unspecified optional parameters of I.Foo() and C.Foo() have the
            // same default values.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType2()
        {
            // Note: The cast below can be removed because C is sealed and the
            // interface member has no parameters.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType3()
        {
            // Note: The cast below can be removed because C is sealed and the
            // interface member has no parameters.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType4()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the unspecified optional parameter default values differ.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType5()
        {
            // Note: The cast below can be removed (even though C is sealed)
            // because the optional parameters whose default values differ are
            // specified.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType6()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType7()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType8()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType9()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because it would result in binding to a Dispose method that doesn't
            // implement IDisposable.Dispose().

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForStruct1()
        {
            // Note: The cast below can't be removed because the cast boxes 's' and
            // unboxing would change program behavior.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForStruct2()
        {
            // Note: The cast below can be removed because we are sure to have
            // a fresh copy of the struct from the GetEnumerator() method.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToICloneableForDelegate()
        {
            // Note: The cast below can be removed because delegates are implicitly
            // sealed.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToICloneableForArray()
        {
            // Note: The cast below can be removed because arrays are implicitly
            // sealed.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleForEnum()
        {
            // Note: The cast below can be removed because enums are implicitly
            // sealed.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToObjectInParamArrayArg1()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToIntArrayInParamArrayArg2()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToObjectArrayInParamArrayArg3()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayArg1()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToStringArrayInParamArrayArg2()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIntArrayInParamArrayArg3()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayArg4()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectInParamArrayArg5()
        {
            await TestAsync(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayWithNamedArgument()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach1()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach2()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach3()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement since C doesn't contain a GetEnumerator()
            // method.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach4()
        {
            // The cast below can't be removed because it would result in
            // C.GetEnumerator() being called rather than D.GetEnumerator().

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach5()
        {
            // The cast below can't be removed because it would change the
            // type of 'x'.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastIfOverriddenMethodHasIncompatibleParameterList()
        {
            // Note: The cast below can't be removed because the parameter list
            // of Foo and its override have different default values.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastIfOverriddenMethodHaveCompatibleParameterList()
        {
            // Note: The cast below can be removed because the parameter list
            // of Foo and its override have the same default values.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInReceiverForMethodGroup()
        {
            // Note: The cast below can be removed because the it results in
            // the same method group.

            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task Bugfix_609497()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToDifferentTypeWithSameName()
        {
            // Note: The cast below cannot be removed because the it results in
            // a different overload being picked.

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhichWouldChangeAttributeOverloadResolution1()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastIfArgumentIsRestricted_TypedReference()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithOtherDynamicArguments()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithOtherDynamicArguments_Bracketed()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt_1()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt_2()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt_3()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithOtherDynamicArguments_1()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhichWouldChangeAttributeOverloadResolution2()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontUnnecessaryCastFromEnumToUint()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontUnnecessaryCastFromTypeParameterToObject()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastAndParseCorrect()
        {
            await TestAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastIfUserDefinedExplicitCast()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInTernary()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInSwitchExpression()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastFromBaseToDerivedWithExplicitReference()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToTypeParameterWithExceptionConstraint()
        {
            await TestMissingAsync(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
        {
            await TestMissingAsync(
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
