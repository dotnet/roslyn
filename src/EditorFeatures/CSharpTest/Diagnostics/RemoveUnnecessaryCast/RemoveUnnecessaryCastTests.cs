// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveUnnecessaryCast
{
    public partial class RemoveUnnecessaryCastTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryCastDiagnosticAnalyzer(), new RemoveUnnecessaryCastCodeFixProvider());

        [WorkItem(545979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545979")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToErrorType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in [|(ErrorType)s|])
        {
        }
    }
}");
        }

        [WorkItem(545137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545137"), WorkItem(870550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/870550")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame1()
        {
            await TestInRegularAndScriptAsync(
            @"
class Program
{
    static void Main()
    {
        int x = 2;
        int i = 1;
        Goo(x < [|(int)i|], x > (2 + 3));
    }
 
    static void Goo(bool a, bool b) { }
}",

            @"
class Program
{
    static void Main()
    {
        int x = 2;
        int i = 1;
        Goo(x < (i), x > (2 + 3));
    }
 
    static void Goo(bool a, bool b) { }
}");
        }

        [WorkItem(545146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545146")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame2()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545160")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame3()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545138")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveTypeParameterCastToObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Ð¡
{
    void Goo<T>(T obj)
{
    int x = (int)[|(object)obj|];
}
}");
        }

        [WorkItem(545139, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545139")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastInIsTest()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Ð¡
{
    static void Main()
{
    DayOfWeek[] a = {
    };
    Console.WriteLine([|(object)a|] is int[]);
}
}");
        }

        [WorkItem(545142, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545142")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastNeedForUserDefinedOperator()
        {
            await TestMissingInRegularAndScriptAsync(
@"class A
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
}");
        }

        [WorkItem(545143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545143")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemovePointerCast1()
        {
            await TestMissingInRegularAndScriptAsync(
@"unsafe class C
{
    static unsafe void Main()
    {
        var x = (int)[|(int*)null|];
    }
}");
        }

        [WorkItem(545144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545144")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToObjectFromDelegateComparison()
        {
            // The cast below can't be removed because it would result in the Delegate
            // op_Equality operator overload being used over reference equality.

            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        Action a = Console.WriteLine;
        Action b = Console.WriteLine;
        Console.WriteLine(a == [|(object)b|]);
    }
}");
        }

        [WorkItem(545145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545145")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToAnonymousMethodWhenOnLeftOfAsCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        var x = [|(Action)delegate {
        }|]

        as Action;
    }
}");
        }

        [WorkItem(545147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastInFloatingPointOperation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void Main()
    {
        int x = 1;
        double y = [|(double)x|] / 2;
    }
}");
        }

        [WorkItem(545157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545157")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveIdentityCastWhichAffectsOverloadResolution1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        Goo(x => [|(int)x|]);
    }

    static void Goo(Func<int, object> x)
    {
    }

    static void Goo(Func<string, object> x)
    {
    }
}");
        }

        [WorkItem(545158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveIdentityCastWhichAffectsOverloadResolution2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        var x = [|(IComparable<int>)1|];
        Goo(x);
    }

    static void Goo(IComparable<int> x)
    {
    }

    static void Goo(int x)
    {
    }
}");
        }

        [WorkItem(545158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveIdentityCastWhichAffectsOverloadResolution3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        var x = [|(IComparable<int>)1|];
        var y = x;
        Goo(y);
    }

    static void Goo(IComparable<int> x)
    {
    }

    static void Goo(int x)
    {
    }
}");
        }

        [WorkItem(545747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545747")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhichChangesTypeOfInferredLocal()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void Main()
    {
        var x = [|(long)1|];
        x = long.MaxValue;
    }
}");
        }

        [WorkItem(545159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545159")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNeededCastToIListOfObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        Action<object>[] x = {
        };
        Goo(x);
    }

    static void Goo<T>(Action<T>[] x)
    {
        var y = (IList<Action<object>>)[|(IList<object>)x|];
        Console.WriteLine(y.Count);
    }
}");
        }

        [WorkItem(545287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545287"), WorkItem(880752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInParameterDefaultValue()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545289")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInReturnStatement()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda1()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda2()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda3()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda4()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression1()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression2()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression3()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNeededCastInConditionalExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Test
{
    public static void Main()
    {
        int b = 5;
        var f1 = (b == 5) ? 4 : [|(long)5|];
    }
}");
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression4()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545459")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideADelegateConstructor()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545419")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveTriviaWhenRemovingCast()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545422")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideCaseLabel()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545578")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideGotoCaseStatement()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545595")]
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInCollectionInitializer()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWhichInCollectionInitializer1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class X : List<int>
{
    void Add(object x)
    {
        Console.WriteLine(1);
    }

    void Add(string x)
    {
        Console.WriteLine(2);
    }

    static void Main()
    {
        var z = new X { [|(object)""""|] };
    }
}");
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWhichInCollectionInitializer2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class X : List<int>
{
    void Add(object x)
    {
        Console.WriteLine(1);
    }

    void Add(string x)
    {
        Console.WriteLine(2);
    }

    static void Main()
    {
        X z = new X { [|(object)""""|] };
    }
}");
        }

        [WorkItem(545607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545607")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInArrayInitializer()
        {
            await TestInRegularAndScriptAsync(
            @"
class X
{
    static void Goo()
    {
        string x = "";
        var s = new object[] { [|(object)x|] };
    }
}",

@"
class X
{
    static void Goo()
    {
        string x = "";
        var s = new object[] { x };
    }
}");
        }

        [WorkItem(545616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastWithOverloadedBinaryOperator()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;
class MyAction
{
    static void Goo()
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
    static void Goo()
    {
        MyAction x = null;
        var y = x + delegate { };
    }
 
    public static MyAction operator +(MyAction x, Action y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545822")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastShouldInsertWhitespaceWhereNeededToKeepCorrectParsing()
        {
            await TestInRegularAndScriptAsync(
            @"
using System;
 
class Program
{
    static void Goo<T>()
    {
        Action a = null;
        var x = [|(Action)(Goo<Guid>)|]==a;
    }
}",

@"
using System;
 
class Program
{
    static void Goo<T>()
    {
        Action a = null;
        var x = (Goo<Guid>) == a;
    }
}");
        }

        [WorkItem(545560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545560")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithExplicitUserDefinedConversion()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class A
{
    public static explicit operator long(A x)
    {
        return 1;
    }

    public static implicit operator int(A x)
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

        [WorkItem(545608, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545608")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithImplicitUserDefinedConversion()
        {
            await TestMissingInRegularAndScriptAsync(
@"class X
{
    static void Goo()
    {
        X x = null;
        object y = [|(string)x|];
    }

    public static implicit operator string(X x)
    {
        return "";
    }
}");
        }

        [WorkItem(545941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithImplicitConversionInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(545981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        object ex = new Exception();
        throw [|(Exception)ex|];
    }
}");
        }

        [WorkItem(545941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInThrow()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545945")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryDowncast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void Goo(object y)
    {
        int x = [|(int)y|];
    }
}");
        }

        [WorkItem(545591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545591")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastWithinLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        Boo(x => Goo(x, y => [|(int)x|]), null);
    }

    static void Boo(Action<int> x, object y)
    {
        Console.WriteLine(1);
    }

    static void Boo(Action<string> x, string y)
    {
        Console.WriteLine(2);
    }

    static void Goo(int x, Func<int, int> y)
    {
    }

    static void Goo(string x, Func<string, string> y)
    {
    }

    static void Goo(string x, Func<int, int> y)
    {
    }
}");
        }

        [WorkItem(545606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545606")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromNullToTypeParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class X
{
    static void Goo<T, S>() where T : class, S
    {
        S y = [|(T)null|];
    }
}");
        }

        [WorkItem(545744, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInImplicitlyTypedArray()
        {
            await TestMissingInRegularAndScriptAsync(
@"class X
{
    static void Goo()
    {
        string x = "";
        var s = new[] { [|(object)x|] };
        s[0] = 1;
    }
}");
        }

        [WorkItem(545750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545750")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToBaseType()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(545855, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545855")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
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
    parseOptions: null);
        }

        [WorkItem(529816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInQueryExpression()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(529816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInQueryExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class A
{
    int Select(Func<int, long> x)
    {
        return 1;
    }

    int Select(Func<int, int> x)
    {
        return 2;
    }

    static void Main()
    {
        Console.WriteLine(from y in new A()
                          select [|(long)0|]);
    }
}");
        }

        [WorkItem(545848, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInConstructorInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Goo(int x, Func<int, int> y)
    {
    }

    static void Goo(string x, Func<string, string> y)
    {
    }

    C(Action<int> x, object y)
    {
        Console.WriteLine(1);
    }

    C(Action<string> x, string y)
    {
        Console.WriteLine(2);
    }

    C() : this(x => Goo(x, y => [|(int)x|]), null)
    {
    }

    static void Main()
    {
        new C();
    }
}");
        }

        [WorkItem(529831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromTypeParameterToInterface()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

interface IIncrementable
{
    int Value { get; }

    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }
}

class C : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }
}

static class Program
{
    static void Main()
    {
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y)
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ([|(IIncrementable)x|]).Increment(); // False Unnecessary Cast
        ((IIncrementable)y).Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}");
        }

        [WorkItem(529831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastFromTypeParameterToInterface()
        {
            await TestInRegularAndScriptAsync(
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
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y) 
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
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y) 
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        y.Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}
");
        }

        [WorkItem(545877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545877")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontCrashOnIncompleteMethodDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class A
{
    static void Main()
    {
        byte
        Goo(x => 1, [|(byte)1|]);
    }

    static void Goo<T, S>(T x, )
    {
    }
}");
        }

        [WorkItem(545777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545777")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveImportantTrailingTrivia()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(529791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToNullable1()
        {
            await TestInRegularAndScriptAsync(
            @"
class X
{
    static void Goo()
    {
        object x = (string)null;
        object y = [|(int?)null|];
    }
}
",

            @"
class X
{
    static void Goo()
    {
        object x = (string)null;
        object y = null;
    }
}
");
        }

        [WorkItem(545842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToNullable2()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545850")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveSurroundingParentheses()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(529846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromTypeParameterToObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void Goo<T>(T x, object y)
    {
        if ([|(object)x|] == y)
        {
        }
    }
}");
        }

        [WorkItem(545858, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545858")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastFromDelegateTypeToMulticastDelegate()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        Action x = Console.WriteLine;
        Action y = Console.WriteLine;
        Console.WriteLine([|(MulticastDelegate)x|] == y);
    }
}");
        }

        [WorkItem(545857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545857")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInSizeOfArrayCreationExpression1()
        {
            // The cast below can't be removed because it would result in the implicit
            // conversion to int being called instead.

            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        Console.WriteLine(new int[[|(long)default(C)|]].Length);
    }

    public static implicit operator long(C x)
    {
        return 1;
    }

    public static implicit operator int(C x)
    {
        return 2;
    }
}");
        }

        [WorkItem(545980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInSizeOfArrayCreationExpression2()
        {
            // Array bounds must be an int, so the cast below can't be removed.

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void Main()
    {
        var a = new int[[|(int)decimal.Zero|]];
    }
}");
        }

        [WorkItem(529842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529842")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInTernaryExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class X
{
    public static implicit operator string(X x)
    {
        return x.ToString();
    }

    static void Main()
    {
        bool b = true;
        X x = new X();
        Console.WriteLine(b ? [|(string)null|] : x);
    }
}");
        }

        [WorkItem(545882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882"), WorkItem(880752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInConstructorInitializer1()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545958"), WorkItem(880752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInConstructorInitializer2()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545957")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastInConstructorInitializer3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    C(int x)
    {
    }

    C() : this([|(long)1|])
    {
    }
}");
        }

        [WorkItem(545842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToNullableInArithmeticExpression()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastFromValueTypeToObjectInReferenceEquality()
        {
            // Note: The cast below can't be removed because it would result in an
            // illegal reference equality test between object and a value type.

            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        object x = 1;
        Console.WriteLine(x == [|(object)1|]);
    }
}");
        }

        [WorkItem(545962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545962")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhenExpressionDoesntBind()
        {
            // Note: The cast below can't be removed because its expression doesn't bind.

            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        ([|(IDisposable)x|]).Dispose();
    }
}");
        }

        [WorkItem(545944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545944")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastBeforePointerDereference1()
        {
            // Note: The cast below can't be removed because it would result in *null,
            // which is illegal.

            await TestMissingInRegularAndScriptAsync(
@"unsafe class C
{
    int x = *[|(int*)null|];
}");
        }

        [WorkItem(545978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545978")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastBeforePointerDereference2()
        {
            // Note: The cast below can't be removed because it would result in dereferencing
            // void*, which is illegal.

            await TestMissingInRegularAndScriptAsync(
@"unsafe class C
{
    static void Main()
    {
        void* p = null;
        int x = *[|(int*)p|];
    }
}");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastBeforePointerDereference3()
        {
            // Conservatively disable cast simplifications for casts involving pointer conversions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public unsafe float ReadSingle(byte* ptr)
    {
        return *[|(float*)ptr|];
    }
}");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInUncheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        if (unchecked([|(uint)byteCount)|] > (_endPointer - _currentPointer))
        {
        }
    }
}");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInUncheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInCheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        if (checked([|(uint)byteCount)|] > (_endPointer - _currentPointer))
        {
        }
    }
}");
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNumericCastInCheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

[A([|(byte)0)|]]
class A : Attribute
{
    public A(object x)
    {
    }
}");
        }

        #region Interface Casts

        [WorkItem(545889, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545889")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForUnsealedType()
        {
            // Note: The cast below can't be removed because X is not sealed.

            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType1()
        {
            // Note: The cast below can be removed because C is sealed and the
            // unspecified optional parameters of I.Goo() and C.Goo() have the
            // same default values.

            await TestInRegularAndScriptAsync(
@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Goo();
    }
}
",

@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo();
    }
}
");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType2()
        {
            // Note: The cast below can be removed because C is sealed and the
            // interface member has no parameters.

            await TestInRegularAndScriptAsync(
@"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)new C()|]).Goo);
    }
}
",

@"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(new C().Goo);
    }
}
");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType3()
        {
            // Note: The cast below can be removed because C is sealed and the
            // interface member has no parameters.

            await TestInRegularAndScriptAsync(
@"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public C Instance { get { return new C(); } }

    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)Instance|]).Goo);
    }
}
",

@"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public C Instance { get { return new C(); } }

    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(Instance.Goo);
    }
}
");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType4()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the unspecified optional parameter default values differ.

            await TestMissingInRegularAndScriptAsync(
@"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Goo();
    }
}");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType5()
        {
            // Note: The cast below can be removed (even though C is sealed)
            // because the optional parameters whose default values differ are
            // specified.

            await TestInRegularAndScriptAsync(
@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Goo(2);
    }
}
",

@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo(2);
    }
}
");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType6()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            await TestMissingInRegularAndScriptAsync(
@"using System;

interface I
{
    void Goo(int x = 0, int y = 0);
}

sealed class C : I
{
    public void Goo(int y = 0, int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)new C()|]).Goo(x: 1);
    }
}");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForSealedType7()
        {
            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType8()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(545883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545883")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForSealedType9()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because it would result in binding to a Dispose method that doesn't
            // implement IDisposable.Dispose().

            await TestMissingInRegularAndScriptAsync(
@"using System;
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
}");
        }

        [WorkItem(545887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545887")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToInterfaceForStruct1()
        {
            // Note: The cast below can't be removed because the cast boxes 's' and
            // unboxing would change program behavior.

            await TestMissingInRegularAndScriptAsync(
@"using System;

interface IIncrementable
{
    int Value { get; }

    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }

    static void Main()
    {
        var s = new S();
        ([|(IIncrementable)s|]).Increment();
        Console.WriteLine(s.Value);
    }
}");
        }

        [WorkItem(545834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545834")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForStruct2()
        {
            // Note: The cast below can be removed because we are sure to have
            // a fresh copy of the struct from the GetEnumerator() method.

            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(544655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544655")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToICloneableForDelegate()
        {
            // Note: The cast below can be removed because delegates are implicitly
            // sealed.

            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(545926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545926")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToICloneableForArray()
        {
            // Note: The cast below can be removed because arrays are implicitly
            // sealed.

            await TestInRegularAndScriptAsync(
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
");
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleForEnum()
        {
            // Note: The cast below can be removed because enums are implicitly
            // sealed.

            await TestInRegularAndScriptAsync(
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
");
        }

        #endregion

        #region ParamArray Parameter Casts

        [WorkItem(545141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545141")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToObjectInParamArrayArg1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        Goo([|(object)null|]);
    }

    static void Goo(params object[] x)
    {
        Console.WriteLine(x.Length);
    }
}");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToIntArrayInParamArrayArg2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        Goo([|(int[])null|]);
    }

    static void Goo(params object[] x)
    {
        Console.WriteLine(x.Length);
    }
}");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToObjectArrayInParamArrayArg3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    static void Main()
    {
        Goo([|(object[])null|]);
    }

    static void Goo(params object[][] x)
    {
        Console.WriteLine(x.Length);
    }
}");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayArg1()
        {
            await TestInRegularAndScriptAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(object[])null|]);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToStringArrayInParamArrayArg2()
        {
            await TestInRegularAndScriptAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(string[])null|]);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIntArrayInParamArrayArg3()
        {
            await TestInRegularAndScriptAsync(
            @"
class C
{
    static void Goo(params int[] x) { }

    static void Main()
    {
        Goo([|(int[])null|]);
    }
}
",

@"
class C
{
    static void Goo(params int[] x) { }

    static void Main()
    {
        Goo(null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayArg4()
        {
            await TestInRegularAndScriptAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(object[])null|], null);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null, null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectInParamArrayArg5()
        {
            await TestInRegularAndScriptAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(object)null|], null);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null, null);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayWithNamedArgument()
        {
            await TestInRegularAndScriptAsync(
                @"
class C
{
    static void Main()
    {
        Goo(x: [|(object[])null|]);
    }

    static void Goo(params object[] x) { }
}
",
                @"
class C
{
    static void Main()
    {
        Goo(x: null);
    }

    static void Goo(params object[] x) { }
}
");
        }

        #endregion

        #region ForEach Statements

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach1()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            await TestMissingInRegularAndScriptAsync(
@"using System.Collections;

class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in [|(IEnumerable)s|])
        {
        }
    }
}");
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach2()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in [|(IEnumerable<char>)s|])
        {
        }
    }
}");
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach3()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement since C doesn't contain a GetEnumerator()
            // method.

            await TestMissingInRegularAndScriptAsync(
@"using System.Collections;

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
        foreach (object x in [|(D)new C()|])
        {
        }
    }
}");
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach4()
        {
            // The cast below can't be removed because it would result in
            // C.GetEnumerator() being called rather than D.GetEnumerator().

            await TestMissingInRegularAndScriptAsync(
@"using System;
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

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInForEach5()
        {
            // The cast below can't be removed because it would change the
            // type of 'x'.

            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        string[] s = {
            ""A""
        };
        foreach (var x in [|(Array)s|])
        {
            var y = x;
            y = 1;
        }
    }
}");
        }

        #endregion

        [WorkItem(545925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastIfOverriddenMethodHasIncompatibleParameterList()
        {
            // Note: The cast below can't be removed because the parameter list
            // of Goo and its override have different default values.

            await TestMissingInRegularAndScriptAsync(
@"using System;

abstract class Y
{
    public abstract void Goo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        ([|(Y)new X()|]).Goo();
    }

    public override void Goo(int x = 2)
    {
        Console.WriteLine(x);
    }
}");
        }

        [WorkItem(545925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastIfOverriddenMethodHaveCompatibleParameterList()
        {
            // Note: The cast below can be removed because the parameter list
            // of Goo and its override have the same default values.

            await TestInRegularAndScriptAsync(
@"
using System;

abstract class Y
{
    public abstract void Goo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        ([|(Y)new X()|]).Goo();
    }

    public override void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }
}
",

@"
using System;

abstract class Y
{
    public abstract void Goo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        new X().Goo();
    }

    public override void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }
}
");
        }

        [WorkItem(529916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529916")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInReceiverForMethodGroup()
        {
            // Note: The cast below can be removed because the it results in
            // the same method group.

            await TestInRegularAndScriptAsync(
@"
using System;

static class Program
{
    static void Main()
    {
        Action a = ([|(string)""""|]).Goo;
    }

    static void Goo(this string x) { }
}
",

@"
using System;

static class Program
{
    static void Main()
    {
        Action a = """".Goo;
    }

    static void Goo(this string x) { }
}
");
        }

        [WorkItem(609497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task Bugfix_609497()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        Goo().Wait();
    }

    static async Task Goo()
    {
        Task task = Task.FromResult(0);
        Console.WriteLine(await [|(dynamic)task|]);
    }
}");
        }

        [WorkItem(545995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545995")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToDifferentTypeWithSameName()
        {
            // Note: The cast below cannot be removed because the it results in
            // a different overload being picked.

            await TestMissingInRegularAndScriptAsync(
@"using System;
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
    static void Goo(int x)
    {
        Console.WriteLine(""int"");
    }

    static void Goo(MyInt x)
    {
        Console.WriteLine(""MyInt"");
    }

    static void Main()
    {
        Goo([|(MyInt)0|]);
    }
}");
        }

        [WorkItem(545921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545921")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhichWouldChangeAttributeOverloadResolution1()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            await TestMissingInRegularAndScriptAsync(
@"using System;

[Flags]
enum EEEnum
{
    Flag1 = 0x2,
    Flag2 = 0x1,
}

class MyAttributeAttribute : Attribute
{
    public MyAttributeAttribute(EEEnum e)
    {
    }

    public MyAttributeAttribute(short e)
    {
    }

    public void Goo(EEEnum e)
    {
    }

    public void Goo(short e)
    {
    }

    [MyAttribute([|(EEEnum)0x0|])]
    public void Bar()
    {
        Goo((EEEnum)0x0);
    }
}");
        }

        [WorkItem(608180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608180")]
        [WorkItem(624252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624252")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastIfArgumentIsRestricted_TypedReference()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithOtherDynamicArguments()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        C<string>.InvokeGoo(0);
    }
}

class C<T>
{
    public static void InvokeGoo(dynamic x)
    {
        Console.WriteLine(Goo(x, [|(object)""""|], """"));
    }

    static void Goo(int x, string y, T z)
    {
    }

    static bool Goo(int x, object y, object z)
    {
        return true;
    }
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithOtherDynamicArguments_Bracketed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C<T>
{
    int this[int x, T s, string d = ""abc""]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }

    int this[int x, object s, object d]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }

    void Goo(dynamic xx)
    {
        var y = this[x: xx, s: """", d: [|(object)""""|]];
    }
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static bool Goo(dynamic d)
    {
        d([|(object)""""|]);
        return true;
    }
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static bool Goo(dynamic d)
    {
        d.goo([|(object)""""|]);
        return true;
    }
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt_2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static bool Goo(dynamic d)
    {
        d.goo.bar.goo([|(object)""""|]);
        return true;
    }
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithDynamicReceiverOpt_3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static bool Goo(dynamic d)
    {
        d.goo().bar().goo([|(object)""""|]);
        return true;
    }
}");
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnArgumentsWithOtherDynamicArguments_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        C<string>.InvokeGoo(0);
    }
}

class C<T>
{
    public static void InvokeGoo(dynamic x)
    {
        Console.WriteLine(Goo([|(object)""""|], x, """"));
    }

    static void Goo(string y, int x, T z)
    {
    }

    static bool Goo(object y, int x, object z)
    {
        return true;
    }
}");
        }

        [WorkItem(545998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545998")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastWhichWouldChangeAttributeOverloadResolution2()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            await TestMissingInRegularAndScriptAsync(
@"using System;

[A(new[] { [|(long)0|] })]
class A : Attribute
{
    public A(long[] x)
    {
    }
}");
        }

        [WorkItem(529894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontUnnecessaryCastFromEnumToUint()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [WorkItem(529846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontUnnecessaryCastFromTypeParameterToObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void Goo<T>(T x, object y)
    {
        if ([|(object)|]x == y)
        {
        }
    }
}");
        }

        [WorkItem(640136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640136")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastAndParseCorrect()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
using System.Threading.Tasks;
 
class C
{
    void Goo(Task<Action> x)
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
    void Goo(Task<Action> x)
    {
        (x.Result)();
    }
}
");
        }

        [WorkItem(626026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/626026")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastIfUserDefinedExplicitCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
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
}");
        }

        [WorkItem(768895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768895")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInTernary()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        object x = null;
        int y = [|(bool)x|] ? 1 : 0;
    }
}");
        }

        [WorkItem(770187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770187")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveNecessaryCastInSwitchExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication23
{
    class Program
    {
        static void Main(string[] args)
        {
            int goo = 0;
            switch ([|(E)goo|])
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
}");
        }

        [WorkItem(844482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844482")]
        [WorkItem(2761, "https://github.com/dotnet/roslyn/issues/2761")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastFromBaseToDerivedWithExplicitReference()
        {
            await TestMissingInRegularAndScriptAsync(
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
        public async Task DontRemoveCastToTypeParameterWithExceptionConstraint()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : Exception
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
        public async Task DontRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : ArgumentException
    {
        if (!condition)
        {
            throw [|(TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition)|];
        }
    }
}");
        }

        [WorkItem(8111, "https://github.com/dotnet/roslyn/issues/8111")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastThatChangesShapeOfAnonymousTypeObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = [|(int)Directions.South|] };
    }

    public enum Directions
    {
        North,
        East,
        South,
        West
    }
}");
        }

        [WorkItem(8111, "https://github.com/dotnet/roslyn/issues/8111")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastThatDoesntChangeShapeOfAnonymousTypeObject()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = [|(Directions)Directions.South|] };
    }

    public enum Directions
    {
        North,
        East,
        South,
        West
    }
}",

@"class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = Directions.South };
    }

    public enum Directions
    {
        North,
        East,
        South,
        West
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task Tuple()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main()
    {
        (int, string) tuple = [|((int, string))(1, ""hello"")|];
    }
}",
@"class C
{
    void Main()
    {
        (int, string) tuple = (1, ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TupleWithDifferentNames()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void Main()
    {
        (int a, string) tuple = [|((int, string d))(1, f: ""hello"")|];
    }
}",
@"class C
{
    void Main()
    {
        (int a, string) tuple = (1, f: ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [WorkItem(24791, "https://github.com/dotnet/roslyn/issues/24791")]
        public async Task SimpleBoolCast()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        if (![|(bool)|]M()) throw null;
        throw null;
    }
}",
@"class C
{
    bool M()
    {
        if (!M()) throw null;
        throw null;
    }
}");
        }

        [WorkItem(12572, "https://github.com/dotnet/roslyn/issues/12572")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastThatUnboxes()
        {
            // The cast below can't be removed because it could throw a null ref exception.
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        object i = null;
        switch ([|(int)i|])
        {
            case 0:
                Console.WriteLine(0);
                break;
            case 1:
                Console.WriteLine(1);
                break;
            case 2:
                Console.WriteLine(2);
                break;
        }
    }
}");
        }

        [WorkItem(17029, "https://github.com/dotnet/roslyn/issues/17029")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnEnumComparison1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
enum TransferTypeKey
{
    Transfer,
    TransferToBeneficiary
}

class Program
{
    static void Main(dynamic p)
    {
        if (p.TYP != [|(int)TransferTypeKey.TransferToBeneficiary|])
          throw new InvalidOperationException();
    }
}");
        }

        [WorkItem(17029, "https://github.com/dotnet/roslyn/issues/17029")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnEnumComparison2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
enum TransferTypeKey
{
    Transfer,
    TransferToBeneficiary
}

class Program
{
    static void Main(dynamic p)
    {
        if ([|(int)TransferTypeKey.TransferToBeneficiary|] != p.TYP)
          throw new InvalidOperationException();
    }
}");
        }

        [WorkItem(18978, "https://github.com/dotnet/roslyn/issues/18978")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToMethodWithParamsArgs()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class Program
{
    public static void Main(string[] args)
    {
        var takesArgs = new[] { ""Hello"", ""World"" };
        TakesParams([|(object)|]takesArgs);
    }

    private static void TakesParams(params object[] goo)
    {
        Console.WriteLine(goo.Length);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToMethodWithParamsArgsWithIncorrectMethodDefintion()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class Program
{
    public static void Main(string[] args)
    {
        TakesParams([|(string)|]null);
    }

    private static void TakesParams(params string wrongDefined)
    {
        Console.WriteLine(wrongDefined.Length);
    }
}");
        }

        [WorkItem(18978, "https://github.com/dotnet/roslyn/issues/18978")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnCallToMethodWithParamsArgsIfImplicitConversionExists()
        {
            await TestInRegularAndScriptAsync(
@"
class Program
{
    public static void Main(string[] args)
    {
        var takesArgs = new[] { ""Hello"", ""World"" };
        TakesParams([|(System.IComparable[])|]takesArgs);
    }

    private static void TakesParams(params object[] goo)
    {
        System.Console.WriteLine(goo.Length);
    }
}",
@"
class Program
{
    public static void Main(string[] args)
    {
        var takesArgs = new[] { ""Hello"", ""World"" };
        TakesParams(takesArgs);
    }

    private static void TakesParams(params object[] goo)
    {
        System.Console.WriteLine(goo.Length);
    }
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToAttributeWithParamsArgs()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
using System.Reflection;

sealed class MarkAttribute : Attribute
{
  public readonly string[] Arr;

  public MarkAttribute(params string[] arr)
  {
    Arr = arr;
  }
}
[Mark([|(string)|]null)]   // wrong instance of: IDE0004 Cast is redundant.
static class Program
{
  static void Main()
  {
  }
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToAttributeWithParamsArgsAndProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(params string[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark([|(string)|]null, Prop = 1)] 
static class Program
{
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToAttributeWithParamsArgsPropertyAndOtherArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params string[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(true, [|(string)|]null, Prop = 1)] 
static class Program
{
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToAttributeWithParamsArgsNamedArgsAndProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params string[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(arr: [|(string)|]null, otherArg: true, Prop = 1)]
static class Program
{
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastOnCallToAttributeWithParamsArgsNamedArgsWithIncorrectMethodDefintion()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params string wrongDefined)
    {
    }
    public int Prop { get; set; }
}

[Mark(true, [|(string)|]null, Prop = 1)]
static class Program
{
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnCallToAttributeWithParamsArgsWithImplicitCast()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params object[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(arr: ([|object[])new[]|] { ""Hello"", ""World"" }, otherArg: true, Prop = 1)]
static class Program
{
}",
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params object[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(arr: (new[] { ""Hello"", ""World"" }), otherArg: true, Prop = 1)]
static class Program
{
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnCallToAttributeWithCastInPropertySetter()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute()
    {
    }
    public int Prop { get; set; }
}

[Mark(Prop = [|(int)1|])]
static class Program
{
}",
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute()
    {
    }
    public int Prop { get; set; }
}

[Mark(Prop = 1)]
static class Program
{
}");
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [InlineData("-")]
        [InlineData("+")]
        public async Task DontRemoveCastOnInvalidUnaryOperatorEnumValue1(string op)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
enum Sign
    {{
        Positive = 1,
        Negative = -1
    }}

    class T
    {{
        void Goo()
        {{
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( [|{op}((int) mySign)|] );
        }}
    }}");
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [InlineData("-")]
        [InlineData("+")]
        public async Task DontRemoveCastOnInvalidUnaryOperatorEnumValue2(string op)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
enum Sign
    {{
        Positive = 1,
        Negative = -1
    }}

    class T
    {{
        void Goo()
        {{
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( [|{op}(int) mySign|] );
        }}
    }}");
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnValidUnaryOperatorEnumValue()
        {
            await TestInRegularAndScriptAsync(
@"
enum Sign
    {
        Positive = 1,
        Negative = -1
    }

    class T
    {
        void Goo()
        {
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( [|~(int) mySign|] );
        }
    }",
@"
enum Sign
    {
        Positive = 1,
        Negative = -1
    }

    class T
    {
        void Goo()
        {
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( ~mySign);
        }
    }");
        }

        [WorkItem(25456, "https://github.com/dotnet/roslyn/issues/25456#issuecomment-373549735")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInSwitchCase()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case [|(bool)default|]:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInSwitchCase_CastInsideParentheses()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case ([|(bool)default|]):
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInSwitchCase_DefaultInsideParentheses()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case [|(bool)(default)|]:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInSwitchCase_RemoveDoubleCast()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)[|(bool)default|]:
                break;
        }
    }
}",
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternSwitchCase()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case [|(bool)default|] when true:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternSwitchCase_CastInsideParentheses()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case ([|(bool)default|]) when true:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternSwitchCase_DefaultInsideParentheses()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case [|(bool)(default)|] when true:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternSwitchCase_RemoveDoubleCast()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)[|(bool)default|] when true:
                break;
        }
    }
}",
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when true:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternSwitchCase_RemoveInsideWhenClause()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when [|(bool)default|]:
                break;
        }
    }
}",
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when default:
                break;
        }
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternIs()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        if (true is [|(bool)default|]);
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternIs_CastInsideParentheses()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        if (true is ([|(bool)default|]));
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternIs_DefaultInsideParentheses()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        if (true is [|(bool)(default)|]);
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontIntroduceDefaultLiteralInPatternIs_RemoveDoubleCast()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        if (true is (bool)[|(bool)default|]);
    }
}",
@"
class C
{
    void M()
    {
        if (true is (bool)default) ;
    }
}", parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));
        }

        [WorkItem(27239, "https://github.com/dotnet/roslyn/issues/27239")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontOfferToRemoveCastWhereNoConversionExists()
        {
            await TestMissingInRegularAndScriptAsync(
                @"
using System;

class C
{
    void M()
    {
        object o = null;
        TypedReference r2 = [|(TypedReference)o|];
    }
}");
        }
    }
}
