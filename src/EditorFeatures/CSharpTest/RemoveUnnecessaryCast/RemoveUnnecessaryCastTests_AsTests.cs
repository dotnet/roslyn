﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryCast
{
    public partial class RemoveUnnecessaryCastTests_AsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public RemoveUnnecessaryCastTests_AsTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryCastDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryCastCodeFixProvider());

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
        foreach (object x in ([|s as ErrorType|]))
        {
        }
    }
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
        ([|a as Action|])();
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

        [WorkItem(545138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545138")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveTypeParameterCastToObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Ð¡
{
    void Goo<T>(T obj)
{
    int x = (int)([|obj as object|]);
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
    Console.WriteLine([|a as object|] is int[]);
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
        A x = [|null as string|];
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
        var x = (int)([|null as int*|]);
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
        Console.WriteLine(a == ([|b as object|]));
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
        var x = [|delegate {
        } as Action|]

        as Action;
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
        Goo(x => [|x as string|]);
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
        var x = [|1 as IComparable<int>|];
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
        var x = [|1 as IComparable<int>|];
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
        var x = [|"""" as object|];
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
        var y = (IList<Action<object>>)([|x as IList<object>|]);
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
    static void M1(string i1 = [|null as string|])
    {
    }
}",

@"
class Program
{
    static void M1(string i1 = null)
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
    static string M2()
    {
        return [|"""" as string|];
    }
}",

@"
class Program
{
    static string M2()
    {
        return """";
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
        Func<string> f1 = () => [|"""" as string|];
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<string> f1 = () => """";
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
        Func<string> f1 = () => { return [|"""" as string|]; };
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<string> f1 = () => { return """"; };
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
        Func<string> f1 = _ => { return [|"""" as string|]; };
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<string> f1 = _ => { return """"; };
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
        Func<string> f1 = _ => [|"""" as string|];
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<string> f1 = _ => """";
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

        string f1 = (b == 5) ? [|""a"" as string|] : ""b"" as string;
    }
}",

@"
class Test
{
    public static void Main()
    {
        int b = 5;

        string f1 = (b == 5) ? ""a"" : ""b"" as string;
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

        string f1 = (b == 5) ? ""a"" as string : [|""b"" as string|];
    }
}",

@"
class Test
{
    public static void Main()
    {
        int b = 5;

        string f1 = (b == 5) ? ""a"" as string : ""b"";
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
        var f1 = (b == 5) ? """" : [|"""" as object|];
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
        var cd1 = new D([|M1 as Action<int>|]);
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
        switch ("""")
        {
            case [|"""" as string|]:
                break;
        }
    }
}",

@"
class Test
{
    static void Main()
    {
        switch ("""")
        {
            case """":
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
        var z = new List<string> { [|"""" as string|] };
    }
}",

@"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var z = new List<string> { """" };
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
        var z = new X { [|"""" as object|] };
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
        X z = new X { [|"""" as object|] };
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
        var s = new object[] { [|x as object|] };
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
        object y = [|x as string|];
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
        throw [|new E() as Exception|];
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
        throw [|ex as Exception|];
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
        throw [|new Exception() as Exception|];
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
        var x = [|y as string|];
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
        S y = [|null as T|];
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
        var s = new[] { [|x as object|] };
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
        var s = ([|new X() as object|]).ToString();
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
        Console.WriteLine(fields.Any([|(field => field.IsStatic) as Func<FieldInfo, bool>|]));
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
    int Select(Func<int, string> x) { return 1; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select [|"""" as string|]);
    }
}",

@"
using System;

class A
{
    int Select(Func<int, string> x) { return 1; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select """");
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
    int Select(Func<int, string> x)
    {
        return 1;
    }

    int Select(Func<int, object> x)
    {
        return 2;
    }

    static void Main()
    {
        Console.WriteLine(from y in new A()
                          select [|"""" as object|]);
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
        ([|x as IIncrementable|]).Increment(); // False Unnecessary Cast
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
        ([|y as IIncrementable|]).Increment(); // Unnecessary Cast - OK

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
        string
        Goo(x => 1, [|"""" as string|]);
    }

    static void Goo<T, S>(T x, )
    {
    }
}");
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
        object y = [|null as int?|];
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
        string x = """";
        ([|x as string|]).ToString();
    }
}
",

            @"
class Program
{
    static void Main()
    {
        string x = """";
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
        if (([|x as object|]) == y)
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
        Console.WriteLine(([|x as MulticastDelegate|]) == y);
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
        Console.WriteLine(b ? [|null as string|] : x);
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
    C(string x) { }
    C() : this([|"""" as string|]) { }
}
",

@"
class C
{
    C(string x) { }
    C() : this("""") { }
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
    C() : this([|"""" as IEnumerable|]) { }
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
    C(string x)
    {
    }

    C() : this([|"""" as object|])
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
        long? z = x + ([|y as long?|]);
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
        object x = """";
        Console.WriteLine(x == ([|"""" as object|]));
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
        ([|x as IDisposable|]).Dispose();
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
    int x = *([|null as int*|]);
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
        int x = *([|p as int*|]);
    }
}");
        }

        [WorkItem(26640, "https://github.com/dotnet/roslyn/issues/26640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontRemoveCastToByteFromIntInConditionalExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    object M1(bool b)
    {
        return [|b ? (1 as byte?) : (0 as byte?)|];
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
        ([|x as IDisposable|]).Dispose();
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
        public async Task DoNotRemoveCastToInterfaceForSealedType1()
        {
            await TestMissingAsync(
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
        ([|new C() as I|]).Goo();
    }
}
");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType2()
        {
            await TestMissingAsync(
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
        Console.WriteLine(([|new C() as I|]).Goo);
    }
}
");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType3()
        {
            await TestMissingAsync(
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
        Console.WriteLine(([|Instance as I|]).Goo);
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
        ([|new C() as I|]).Goo();
    }
}");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType5()
        {
            await TestMissingAsync(
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
        ([|new C() as I|]).Goo(2);
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
        ([|new C() as I|]).Goo(x: 1);
    }
}");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType7()
        {
            await TestMissingAsync(
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
        Console.WriteLine(([|new C() as I|])[x: 1]);
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
        Console.WriteLine(([|new C() as I|])[x: 1]);
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
        ([|s as IDisposable|]).Dispose();
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
        ([|s as IIncrementable|]).Increment();
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
        ([|GetEnumerator() as IDisposable|]).Dispose();
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
        var c = ([|a as ICloneable|]).Clone();
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
        var c = ([|a as ICloneable|]).Clone(); 
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
        var y = ([|e as IConvertible|]).GetTypeCode();
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
        Goo([|null as object|]);
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
        Goo([|null as int[]|]);
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
        Goo([|null as object[]|]);
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
        Goo([|null as object[]|]);
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
        Goo([|null as string[]|]);
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
        Goo([|null as int[]|]);
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
        Goo([|null as object[]|], null);
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
        Goo([|null as object|], null);
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
        Goo(x: [|null as object[]|]);
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
        foreach (object x in [|s as IEnumerable|])
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
        foreach (object x in [|s as IEnumerable<char>|])
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
        foreach (object x in [|new C() as D|])
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
        foreach (object x in [|new C() as D|])
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
        foreach (var x in [|s as Array|])
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
        ([|new X() as Y|]).Goo();
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
        ([|new X() as Y|]).Goo();
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
        Action a = ([|"""" as string|]).Goo;
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
        Console.WriteLine(await ([|task as dynamic|]));
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
        dd([|x as object|], y);
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
        Console.WriteLine(Goo(x, [|"""" as object|], """"));
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
        var y = this[x: xx, s: """", d: [|"""" as object|]];
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
        d([|"""" as object|]);
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
        d.goo([|"""" as object|]);
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
        d.goo.bar.goo([|"""" as object|]);
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
        d.goo().bar().goo([|"""" as object|]);
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
        Console.WriteLine(Goo([|"""" as object|], x, """"));
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

        [WorkItem(529846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontUnnecessaryCastFromTypeParameterToObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    static void Goo<T>(T x, object y)
    {
        if (([|x as object|]) == y)
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
        (([|x as Task<Action>|]).Result)();
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
        A a = [|bar as A|];
    }
}

public class A
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
            switch ([|goo as E?|])
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
        y = [|x as D|];
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
            throw [|Activator.CreateInstance(typeof(TException), messageOnFalseCondition) as TException|];
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
            throw [|Activator.CreateInstance(typeof(TException), messageOnFalseCondition) as TException|];
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
    static void Main(object o)
    {
        object thing = new { shouldBeAString = [|o as string|] };
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
    static void Main(string o)
    {
        object thing = new { shouldBeAString = [|o as string|] };
    }
}",

@"class Program
{
    static void Main(string o)
    {
        object thing = new { shouldBeAString = o };
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
        TakesParams([|takesArgs as object|]);
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
        TakesParams([|null as string|]);
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
        TakesParams([|takesArgs as System.IComparable[]|]);
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
[Mark([|null as string|])]   // wrong instance of: IDE0004 Cast is redundant.
static class Program
{
  static void Main()
  {
  }
}");
        }

        [WorkItem(29264, "https://github.com/dotnet/roslyn/issues/29264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnDictionaryIndexer()
        {
            await TestInRegularAndScriptAsync(
                @"
using System;
using System.Reflection;
using System.Collections.Generic;

static class Program
{
    static void Main()
    {
        Dictionary<string, string> Icons = new Dictionary<string, string>
        {
            [[|"""" as string|]] = null,
        };
    }
}",
                @"
using System;
using System.Reflection;
using System.Collections.Generic;

static class Program
{
    static void Main()
    {
        Dictionary<string, string> Icons = new Dictionary<string, string>
        {
            [""""] = null,
        };
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

[Mark([|null as string|], Prop = 1)] 
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

[Mark(true, [|null as string|], Prop = 1)] 
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

[Mark(arr: [|null as string|], otherArg: true, Prop = 1)]
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

[Mark(true, [|null as string|], Prop = 1)]
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

[Mark(arr: [|new[] { ""Hello"", ""World"" } as object[]|], otherArg: true, Prop = 1)]
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

[Mark(arr: new[] { ""Hello"", ""World"" }, otherArg: true, Prop = 1)]
static class Program
{
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
        switch ("""")
        {
            case [|default as string|]:
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
        switch ("""")
        {
            case ([|default as string|]):
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
        switch ("""")
        {
            case [|(default) as string|]:
                break;
        }
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
        TypedReference r2 = [|o as TypedReference|];
    }
}");
        }

        [WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DontOfferToRemoveCastWhenAccessingHiddenProperty()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System.Collections.Generic;
class Fruit
{
    public IDictionary<string, object> Properties { get; set; }
}
class Apple : Fruit
{
    public new IDictionary<string, object> Properties { get; }
}
class Tester
{
    public void Test()
    {
        var a = new Apple();
        ([|a as Fruit|]).Properties[""Color""] = ""Red"";
    }
}");
        }
    }
}
