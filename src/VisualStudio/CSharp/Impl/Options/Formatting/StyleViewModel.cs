// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    /// <summary>
    /// This is the view model for CodeStyle options page.
    /// </summary>
    /// <remarks>
    /// The codestyle options page is defined in <see cref="CodeStylePage"/>
    /// </remarks>
    internal class StyleViewModel : AbstractOptionPreviewViewModel
    {
        #region "Preview Text"

        private const string s_fieldDeclarationPreviewTrue = @"
class C{
    int capacity;
    void Method()
    {
//[
        this.capacity = 0;
//]
    }
}";

        private const string s_fieldDeclarationPreviewFalse = @"
class C{
    int capacity;
    void Method()
    {
//[
        capacity = 0;
//]
    }
}";

        private const string s_propertyDeclarationPreviewTrue = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        this.Id = 0;
//]
    }
}";

        private const string s_propertyDeclarationPreviewFalse = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        Id = 0;
//]
    }
}";

        private const string s_eventDeclarationPreviewTrue = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        this.Elapsed += Handler;
//]
    }
}";

        private const string s_eventDeclarationPreviewFalse = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        Elapsed += Handler;
//]
    }
}";

        private const string s_methodDeclarationPreviewTrue = @"
using System;
class C{
    void Display()
    {
//[
        this.Display();
//]
    }
}";

        private const string s_methodDeclarationPreviewFalse = @"
using System;
class C{
    void Display()
    {
//[
        Display();
//]
    }
}";

        private const string s_intrinsicPreviewDeclarationTrue = @"
class Program
{
//[
    private int _member;
    static void M(int argument)
    {
        int local;
    }
//]
}";

        private const string s_intrinsicPreviewDeclarationFalse = @"
using System;
class Program
{
//[
    private Int32 _member;
    static void M(Int32 argument)
    {
        Int32 local;
    }
//]
}";

        private const string s_intrinsicPreviewMemberAccessTrue = @"
class Program
{
//[
    static void M()
    {
        var local = int.MaxValue;
    }
//]
}";

        private const string s_intrinsicPreviewMemberAccessFalse = @"
using System;
class Program
{
//[
    static void M()
    {
        var local = Int32.MaxValue;
    }
//]
}";

        private static readonly string s_varForIntrinsicsPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        int x = 5; // {ServicesVSResources.built_in_types}
//]
    }}
}}";

        private static readonly string s_varForIntrinsicsPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var x = 5; // {ServicesVSResources.built_in_types}
//]
    }}
}}";

        private static readonly string s_varWhereApparentPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        C cobj = new C(); // {ServicesVSResources.type_is_apparent_from_assignment_expression}
//]
    }}
}}";

        private static readonly string s_varWhereApparentPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var cobj = new C(); // {ServicesVSResources.type_is_apparent_from_assignment_expression}
//]
    }}
}}";

        private static readonly string s_varWherePossiblePreviewFalse = $@"
using System;
class C{{
    void Init()
    {{
//[
        Action f = this.Init(); // {ServicesVSResources.everywhere_else}
//]
    }}
}}";

        private static readonly string s_varWherePossiblePreviewTrue = $@"
using System;
class C{{
    void Init()
    {{
//[
        var f = this.Init(); // {ServicesVSResources.everywhere_else}
//]
    }}
}}";

        private static readonly string s_preferThrowExpression = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        this.s = s ?? throw new ArgumentNullException(nameof(s));
//]
    }}
    void M2(string s)
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (s == null)
        {{
            throw new ArgumentNullException(nameof(s));
        }}

        this.s = s;
//]
    }}
}}
";

        private static readonly string s_preferCoalesceExpression = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = x ?? y;
//]
    }}
    void M2(string s)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var v = x != null ? x : y; // {ServicesVSResources.or}
        var v = x == null ? y : x;
//]
    }}
}}
";

        private static readonly string s_preferConditionalDelegateCall = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        func?.Invoke(args);
//]
    }}
    void M2(string s)
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (func != null)
        {{
            func(args);
        }}
//]
    }}
}}
";

        private static readonly string s_preferNullPropagation = $@"
using System;

class C
{{
    void M1(object o)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = o?.ToString();
//]
    }}
    void M2(object o)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var v = o == null ? null : o.ToString(); // {ServicesVSResources.or}
        var v = o != null ? o.ToString() : null;
//]
    }}
}}
";

        private static readonly string s_preferSwitchExpression = $@"
class C
{{
    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        return num switch
        {{
            1 => 1,
            _ => 2,
        }}
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        switch (num)
        {{
            case 1:
                return 1;
            default:
                return 2;
        }}
//]
    }}
}}
";

        private static readonly string s_preferPatternMatching = $@"
class C
{{
    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        return num is 1 or 2;
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        return num == 1 || num == 2;
//]
    }}
}}
";

        private static readonly string s_preferPatternMatchingOverAsWithNullCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (o is string s)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        var s = o as string;
        if (s != null)
        {{
        }}
//]
    }}
}}
";

        private static readonly string s_preferPatternMatchingOverMixedTypeCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (o is not string s)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (!(o is string s))
        {{
        }}
//]
    }}
}}
";

        private static readonly string s_preferConditionalExpressionOverIfWithAssignments = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        string s = expr ? ""hello"" : ""world"";

        // {ServicesVSResources.Over_colon}
        string s;
        if (expr)
        {{
            s = ""hello"";
        }}
        else
        {{
            s = ""world"";
        }}
//]
    }}
}}
";

        private static readonly string s_preferConditionalExpressionOverIfWithReturns = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        return expr ? ""hello"" : ""world"";

        // {ServicesVSResources.Over_colon}
        if (expr)
        {{
            return ""hello"";
        }}
        else
        {{
            return ""world"";
        }}
//]
    }}
}}
";

        private static readonly string s_preferPatternMatchingOverIsWithCastCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (o is int i)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (o is int)
        {{
            var i = (int)o;
        }}
//]
    }}
}}
";

        private static readonly string s_preferObjectInitializer = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var c = new Customer()
        {{
            Age = 21
        }};
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        var c = new Customer();
        c.Age = 21;
//]
    }}
}}
";

        private static readonly string s_preferCollectionInitializer = $@"
using System.Collections.Generic;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var list = new List<int>
        {{
            1,
            2,
            3
        }};
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        var list = new List<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
//]
    }}
}}
";

        private static readonly string s_preferExplicitTupleName = $@"
class Customer
{{
    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        (string name, int age) customer = GetCustomer();
        var name = customer.name;
        var age = customer.age;
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        (string name, int age) customer = GetCustomer();
        var name = customer.Item1;
        var age = customer.Item2;
//]
    }}
}}
";

        private static readonly string s_preferSimpleDefaultExpression = $@"
using System.Threading;

class Customer1
{{
//[
    // {ServicesVSResources.Prefer_colon}
    void DoWork(CancellationToken cancellationToken = default) {{ }}
//]
}}
class Customer2
{{
//[
    // {ServicesVSResources.Over_colon}
    void DoWork(CancellationToken cancellationToken = default(CancellationToken)) {{ }}
//]
}}
";

        private static readonly string s_preferSimplifiedConditionalExpression = $@"
using System.Threading;

class Customer1
{{
    bool A() => true;
    bool B() => true;

    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var x = A() && B();
//]
    }}
    
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        var x = A() && B() ? true : false
//]
    }}
}}
";

        private static readonly string s_preferInferredTupleName = $@"
using System.Threading;

class Customer
{{
    void M1(int age, string name)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var tuple = (age, name);
//]
    }}
    void M2(int age, string name)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var tuple = (age: age, name: name);
//]
    }}
}}
";

        private static readonly string s_preferInferredAnonymousTypeMemberName = $@"
using System.Threading;

class Customer
{{
    void M1(int age, string name)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var anon = new {{ age, name }};
//]
    }}
    void M2(int age, string name)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var anon = new {{ age = age, name = name }};
//]
    }}
}}
";

        private static readonly string s_preferInlinedVariableDeclaration = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (int.TryParse(value, out int i))
        {{
        }}
//]
    }}
    void M2(string value)
    {{
//[
        // {ServicesVSResources.Over_colon}
        int i;
        if (int.TryParse(value, out i))
        {{
        }}
//]
    }}
}}
";

        private static readonly string s_preferDeconstructedVariableDeclaration = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var (name, age) = GetPersonTuple();
        Console.WriteLine($""{{name}} {{age}}"");

        (int x, int y) = GetPointTuple();
        Console.WriteLine($""{{x}} {{y}}"");
//]
    }}
    void M2(string value)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var person = GetPersonTuple();
        Console.WriteLine($""{{person.name}} {{person.age}}"");

        (int x, int y) point = GetPointTuple();
        Console.WriteLine($""{{point.x}} {{point.y}}"");
//]
    }}
}}
";

        private static readonly string s_doNotPreferBraces = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {ServicesVSResources.Allow_colon}
        if (test) Console.WriteLine(""Text"");

        // {ServicesVSResources.Allow_colon}
        if (test)
            Console.WriteLine(""Text"");

        // {ServicesVSResources.Allow_colon}
        if (test)
            Console.WriteLine(
                ""Text"");

        // {ServicesVSResources.Allow_colon}
        if (test)
        {{
            Console.WriteLine(
                ""Text"");
        }}
//]
    }}
}}
";

        private static readonly string s_preferBracesWhenMultiline = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {ServicesVSResources.Allow_colon}
        if (test) Console.WriteLine(""Text"");

        // {ServicesVSResources.Allow_colon}
        if (test)
            Console.WriteLine(""Text"");

        // {ServicesVSResources.Prefer_colon}
        if (test)
        {{
            Console.WriteLine(
                ""Text"");
        }}
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (test)
            Console.WriteLine(
                ""Text"");
//]
    }}
}}
";

        private static readonly string s_preferBraces = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (test)
        {{
            Console.WriteLine(""Text"");
        }}
//]
    }}
    void M2()
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (test)
            Console.WriteLine(""Text"");
//]
    }}
}}
";

        private static readonly string s_preferAutoProperties = $@"
using System;

class Customer1
{{
//[
    // {ServicesVSResources.Prefer_colon}
    public int Age {{ get; }}
//]
}}
class Customer2
{{
//[
    // {ServicesVSResources.Over_colon}
    private int age;

    public int Age
    {{
        get
        {{
            return age;
        }}
    }}
//]
}}
";

        private static readonly string s_preferFileScopedNamespace = $@"
//[
// {ServicesVSResources.Prefer_colon}
namespace A.B.C;

public class Program
{{
}}
//]
//[
// {ServicesVSResources.Over_colon}
namespace A.B.C
{{
    public class Program
    {{
    }}
}}
//]
";

        private static readonly string s_preferBlockNamespace = $@"
//[
// {ServicesVSResources.Prefer_colon}
namespace A.B.C
{{
    public class Program
    {{
    }}
}}
//]
//[
// {ServicesVSResources.Over_colon}
namespace A.B.C;

public class Program
{{
}}
//]
";

        private static readonly string s_preferSimpleUsingStatement = $@"
using System;

class Customer1
{{
//[
    // {ServicesVSResources.Prefer_colon}
    void Method()
    {{
        using var resource = GetResource();
        ProcessResource(resource);
    }}
//]
}}
class Customer2
{{
//[
    // {ServicesVSResources.Over_colon}
    void Method()
    {{
        using (var resource = GetResource())
        {{
            ProcessResource(resource);
        }}
    }}
//]
}}
";

        private static readonly string s_preferSystemHashCode = $@"
using System;

class Customer1
{{
    int a, b, c;
//[
    // {ServicesVSResources.Prefer_colon}
    // {ServicesVSResources.Requires_System_HashCode_be_present_in_project}
    public override int GetHashCode()
    {{
        return System.HashCode.Combine(a, b, c);
    }}
//]
}}
class Customer2
{{
    int a, b, c;
//[
    // {ServicesVSResources.Over_colon}
    public override int GetHashCode()
    {{
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + a.GetHashCode();
        hashCode = hashCode * -1521134295 + b.GetHashCode();
        hashCode = hashCode * -1521134295 + c.GetHashCode();
        return hashCode;
    }}
//]
}}
";

        private static readonly string s_preferMethodGroupConversion = $@"
using System;

class Customer1
{{
    public void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        Action<object> writeObject = Console.Write;
//]
    }}
}}
class Customer2
{{
    public void M()
    {{
//[
        // {ServicesVSResources.Over_colon}
        Action<object> writeObject = obj => Console.Write(obj);
//]
    }}
//]
}}
";

        private static readonly string s_preferTopLevelStatements = $@"
using System;
//[
// {ServicesVSResources.Prefer_colon}
Console.WriteLine(""Hello, World!"");
//]

//[
// {ServicesVSResources.Over_colon}
internal class Program
{{
    private static void Main(string[] args)
    {{
        Console.WriteLine(""Hello, World!"");
    }}
}}
//]
";

        private static readonly string s_preferProgramMain = $@"
using System;

//[
// {ServicesVSResources.Prefer_colon}
internal class Program
{{
    private static void Main(string[] args)
    {{
        Console.WriteLine(""Hello, World!"");
    }}
}}
//]

//[
// {ServicesVSResources.Over_colon}
Console.WriteLine(""Hello, World!"");
//]
";

        private static readonly string s_preferLocalFunctionOverAnonymousFunction = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
//]
    }}
    void M2(string value)
    {{
//[
        // {ServicesVSResources.Over_colon}
        Func<int, int> fibonacci = null;
        fibonacci = (int n) =>
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }};
//]
    }}
}}
";

        private static readonly string s_preferCompoundAssignments = $@"
using System;
class Customer
{{
    void M1(int value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        value += 10;
//]
    }}
    void M2(int value)
    {{
//[
        // {ServicesVSResources.Over_colon}
        value = value + 10
//]
    }}
}}
";

        private static readonly string s_preferImplicitObjectCreationWhenTypeIsApparent = $@"
using System.Collections.Generic;
class Order {{}}

//[
class Customer
{{
    // {ServicesVSResources.Prefer_colon}
    private readonly List<Order> orders = new();

    // {ServicesVSResources.Over_colon}
    private readonly List<Order> orders = new List<Order>();
}}
//]
";

        private static readonly string s_preferIndexOperator = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var ch = value[^1];
//]
    }}
    void M2(string value)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var ch = value[value.Length - 1];
//]
    }}
}}
";

        private static readonly string s_preferRangeOperator = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var sub = value[1..^1];
//]
    }}
    void M2(string value)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var sub = value.Substring(1, value.Length - 2);
//]
    }}
}}
";

        private static readonly string s_preferTupleSwap = $@"
using System;

class Customer
{{
    void M1(string[] args)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        (args[1], args[0]) = (args[0], args[1]);
//]
    }}
    void M2(string[] args)
    {{
//[
        // {ServicesVSResources.Over_colon}
        var temp = args[0];
        args[0] = args[1];
        args[1] = temp;
//]
    }}
}}
";

        private static readonly string s_preferIsNullOverReferenceEquals = $@"
using System;

class Customer
{{
    void M1(string value1, string value2)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (value1 is null)
            return;

        if (value2 is null)
            return;
//]
    }}
    void M2(string value1, string value2)
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (object.ReferenceEquals(value1, null))
            return;

        if ((object)value2 == null)
            return;
//]
    }}
}}
";

        private static readonly string s_preferNullcheckOverTypeCheck = $@"
using System;

class Customer
{{
    void M1(string value1, string value2)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (value1 is null)
            return;

        if (value2 is not null)
            return;
//]
    }}
    void M2(string value1, string value2)
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (value1 is not object)
            return;

        if (value2 is object)
            return;
//]
    }}
}}
";

        #region expression and block bodies

        private const string s_preferExpressionBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge() => this.Age;
}
//]
";

        private const string s_preferBlockBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge()
    {
        return this.Age;
    }
}
//]
";

        private const string s_preferExpressionBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age) => Age = age;
}
//]
";

        private const string s_preferBlockBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age)
    {
        Age = age;
    }
}
//]
";

        private const string s_preferExpressionBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
        => new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
//]
}
";

        private const string s_preferBlockBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
    {
        return new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
    }
//]
}
";

        private const string s_preferExpressionBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age => _age;
}
//]
";

        private const string s_preferBlockBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age { get { return _age; } }
}
//]
";

        private const string s_preferExpressionBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get => _age;
        set => _age = value;
    }
}
//]
";

        private const string s_preferBlockBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get { return _age; }
        set { _age = value; }
    }
}
//]
";

        private const string s_preferExpressionBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] => _values[i];
}
//]
";

        private const string s_preferBlockBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] { get { return _values[i]; } }
}
//]
";

        private const string s_preferExpressionBodyForLambdas = @"

using System;

class Customer
{
    void Method()
    {
//[
        Func<int, string> f = a => a.ToString();
//]
    }
}
";

        private const string s_preferBlockBodyForLambdas = @"
using System;

class Customer
{
    void Method()
    {
//[
        Func<int, string> f = a =>
        {
            return a.ToString();
        };
//]
    }
}
";

        private const string s_preferExpressionBodyForLocalFunctions = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge() 
    {
        return GetAgeLocal();
        
        int GetAgeLocal() => this.Age;
    }
}
//]
";

        private const string s_preferBlockBodyForLocalFunctions = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge()
    {
        return GetAgeLocal();
        
        int GetAgeLocal()
        {
            return this.Age;
        }
    }
}
//]
";

        private static readonly string s_preferReadonly = $@"
class Customer1
{{
//[
        // {ServicesVSResources.Prefer_colon}
        // '_value' can only be assigned in constructor
        private readonly int _value = 0;
//]
}}
class Customer2
{{
//[
        // {ServicesVSResources.Over_colon}
        // '_value' can be assigned anywhere
        private int _value = 0;
//]
}}
";

        private static readonly string[] s_usingDirectivePlacement = [$@"
//[
    namespace Namespace
    {{
        // {CSharpVSResources.Inside_namespace}
        using System;
        using System.Linq;

        class Customer
        {{
        }}
    }}
//]", $@"
//[
    // {CSharpVSResources.Outside_namespace}
    using System;
    using System.Linq;

    namespace Namespace
    {{
        class Customer
        {{
        }}
    }}
//]
"];

        private static readonly string s_preferReadOnlyStruct = $@"
//[
// {ServicesVSResources.Prefer_colon}
readonly struct Point
{{
    public readonly int X, Y;
}}
//]
//[
// {ServicesVSResources.Over_colon}
struct Point
{{
    public readonly int X, Y;
}}
//]
";

        private static readonly string s_preferStaticLocalFunction = $@"
class Customer1
{{
//[
    void Method()
    {{
        // {ServicesVSResources.Prefer_colon}
        static int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
//]
}}
class Customer2
{{
//[
    void Method()
    {{
        // {ServicesVSResources.Over_colon}
        int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
    }}
//]
}}
";

        #endregion

        #region New Line Preferences

        private static readonly string s_allow_embedded_statements_on_same_line_true = $@"
class Class2
{{
    void Method(int a, int b)
    {{
//[
        // {ServicesVSResources.Allow_colon}
        if (a > b) return true;
//]
        return false;
    }}
}}
";

        private static readonly string s_allow_embedded_statements_on_same_line_false = $@"
class Class1
{{
    void Method(int a, int b)
    {{
//[
        // {ServicesVSResources.Require_colon}
        if (a > b)
            return true;
//]
        return false;
    }}
}}
class Class2
{{
    void Method(int a, int b)
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (a > b) return true;
//]
        return false;
    }}
}}
";

        private static readonly string s_allow_blank_line_between_consecutive_braces_true = $@"
class Class2
{{
//[
    // {ServicesVSResources.Allow_colon}
    void Method()
    {{
        if (true)
        {{
            DoWork();
        }}

    }}
//]
}}
";

        private static readonly string s_allow_blank_line_between_consecutive_braces_false = $@"
class Class1
{{
//[
    // {ServicesVSResources.Require_colon}
    void Method()
    {{
        if (true)
        {{
            DoWork();
        }}
    }}
//]
}}
class Class2
{{
//[
    // {ServicesVSResources.Over_colon}
    void Method()
    {{
        if (true)
        {{
            DoWork();
        }}

    }}
//]
}}
";

        private static readonly string s_allow_multiple_blank_lines_true = $@"
class Class2
{{
    void Method()
    {{
//[
        // {ServicesVSResources.Allow_colon}
        if (true)
        {{
            DoWork();
        }}


        return;
//]
    }}
}}
";

        private static readonly string s_allow_multiple_blank_lines_false = $@"
class Class1
{{
    void Method()
    {{
//[
        // {ServicesVSResources.Require_colon}
        if (true)
        {{
            DoWork();
        }}

        return;
//]
    }}
}}
class Class2
{{
    void Method()
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (true)
        {{
            DoWork();
        }}


        return;
//]
    }}
}}
";

        private static readonly string s_allow_statement_immediately_after_block_true = $@"
class Class2
{{
    void Method()
    {{
//[
        // {ServicesVSResources.Allow_colon}
        if (true)
        {{
            DoWork();
        }}
        return;
//]
    }}
}}
";

        private static readonly string s_allow_statement_immediately_after_block_false = $@"
class Class1
{{
    void Method()
    {{
//[
        // {ServicesVSResources.Require_colon}
        if (true)
        {{
            DoWork();
        }}

        return;
//]
    }}
}}
class Class2
{{
    void Method()
    {{
//[
        // {ServicesVSResources.Over_colon}
        if (true)
        {{
            DoWork();
        }}
        return;
//]
    }}
}}
";

        private static readonly string s_allow_blank_line_after_colon_in_constructor_initializer_true = $@"
class Class
{{
//[
    // {ServicesVSResources.Allow_colon}
    public Class() :
        base()
    {{
    }}
//]
}}
";

        private static readonly string s_allow_blank_line_after_colon_in_constructor_initializer_false = $@"
namespace NS1
{{
    class Class
    {{
    //[
        // {ServicesVSResources.Require_colon}
        public Class()
            : base()
        {{
        }}
    //]
    }}
}}
namespace NS2
{{
    class Class
    {{
    //[
        // {ServicesVSResources.Over_colon}
        public Class() :
            base()
        {{
        }}
    //]
    }}
}}
";

        private static readonly string s_allow_blank_line_after_token_in_conditional_expression_true = $@"
class Class
{{
//[
    // {ServicesVSResources.Allow_colon}
    void Method()
    {{
        var result = CheckCondition() ?
            WhenTrue :
            WhenFalse;
    }}
//]
}}
";

        private static readonly string s_allow_blank_line_after_token_in_conditional_expression_false = $@"
namespace NS1
{{
    class Class
    {{
    //[
        // {ServicesVSResources.Require_colon}
        void Method()
        {{
            var result = CheckCondition()
                ? WhenTrue
                : WhenFalse;
        }}
    //]
    }}
}}
namespace NS2
{{
    class Class
    {{
    //[
        // {ServicesVSResources.Over_colon}
        void Method()
        {{
            var result = CheckCondition() ?
                WhenTrue :
                WhenFalse;
        }}
    //]
    }}
}}
";

        private static readonly string s_allow_blank_line_after_token_in_arrow_expression_clause_true = $@"
class Class
{{
//[
    // {ServicesVSResources.Allow_colon}
    int Method() =>
        1 + 1;
//]
}}
";

        private static readonly string s_allow_blank_line_after_token_in_arrow_expression_clause_false = $@"
namespace NS1
{{
    class Class
    {{
    //[
        // {ServicesVSResources.Require_colon}
        int Method()
            => 1 + 1;
    //]
    }}
}}
namespace NS2
{{
    class Class
    {{
    //[
        // {ServicesVSResources.Over_colon}
        int Method() =>
            1 + 1;
    //]
    }}
}}
";

        #endregion

        #region arithmetic binary parentheses

        private readonly string s_arithmeticBinaryAlwaysForClarity = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = a + (b * c);

        // {ServicesVSResources.Over_colon}
        var v = a + b * c;
//]
    }}
}}
";

        private readonly string s_arithmeticBinaryNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = a + b * c;

        // {ServicesVSResources.Over_colon}
        var v = a + (b * c);
//]
    }}
}}
";

        #endregion

        #region relational binary parentheses

        private readonly string s_relationalBinaryAlwaysForClarity = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = (a < b) == (c > d);

        // {ServicesVSResources.Over_colon}
        var v = a < b == c > d;
//]
    }}
}}
";

        private readonly string s_relationalBinaryNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = a < b == c > d;

        // {ServicesVSResources.Over_colon}
        var v = (a < b) == (c > d);
//]
    }}
}}
";

        #endregion

        #region other binary parentheses

        private readonly string s_otherBinaryAlwaysForClarity = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = a || (b && c);

        // {ServicesVSResources.Over_colon}
        var v = a || b && c;
//]
    }}
}}
";

        private readonly string s_otherBinaryNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = a || b && c;

        // {ServicesVSResources.Over_colon}
        var v = a || (b && c);
//]
    }}
}}
";

        #endregion

        #region other parentheses

        private readonly string s_otherParenthesesAlwaysForClarity = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Keep_all_parentheses_in_colon}
        var v = (a.b).Length;
//]
    }}
}}
";

        private readonly string s_otherParenthesesNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = a.b.Length;

        // {ServicesVSResources.Over_colon}
        var v = (a.b).Length;
//]
    }}
}}
";

        #endregion

        #region unused parameters

        private static readonly string s_avoidUnusedParametersNonPublicMethods = $@"
class C1
{{
//[
    // {ServicesVSResources.Prefer_colon}
    private void M()
    {{
    }}
//]
}}
class C2
{{
//[
    // {ServicesVSResources.Over_colon}
    private void M(int param)
    {{
    }}
//]
}}
";

        private static readonly string s_avoidUnusedParametersAllMethods = $@"
class C1
{{
//[
    // {ServicesVSResources.Prefer_colon}
    public void M()
    {{
    }}
//]
}}
class C2
{{
//[
    // {ServicesVSResources.Over_colon}
    public void M(int param)
    {{
    }}
//]
}}
";
        #endregion

        #region unused values

        private static readonly string s_avoidUnusedValueAssignmentUnusedLocal = $@"
class C
{{
    int M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        int unused = Computation();  // {ServicesVSResources.Unused_value_is_explicitly_assigned_to_an_unused_local}
        int x = 1;
//]
        return x;
    }}

    int Computation() => 0;
}}
class C2
{{
    int M()
    {{
//[
        // {ServicesVSResources.Over_colon}
        int x = Computation();  // {ServicesVSResources.Value_assigned_here_is_never_used}
        x = 1;
//]
        return x;
    }}

    int Computation() => 0;
}}
";

        private static readonly string s_avoidUnusedValueAssignmentDiscard = $@"
class C
{{
    int M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        _ = Computation();      // {ServicesVSResources.Unused_value_is_explicitly_assigned_to_discard}
        int x = 1;
//]
        return x;
    }}

    int Computation() => 0;
}}
class C2
{{
    int M()
    {{
//[
        // {ServicesVSResources.Over_colon}
        int x = Computation();  // {ServicesVSResources.Value_assigned_here_is_never_used}
        x = 1;
//]
        return x;
    }}

    int Computation() => 0;
}}
";

        private static readonly string s_avoidUnusedValueExpressionStatementUnusedLocal = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        int unused = Computation();  //  {ServicesVSResources.Unused_value_is_explicitly_assigned_to_an_unused_local}
//]
    }}

    int Computation() => 0;
}}
class C2
{{
    void M()
    {{
//[
        // {ServicesVSResources.Over_colon}
        Computation();               // {ServicesVSResources.Value_returned_by_invocation_is_implicitly_ignored}
//]
    }}

    int Computation() => 0;
}}
";

        private static readonly string s_avoidUnusedValueExpressionStatementDiscard = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        _ = Computation();      // {ServicesVSResources.Unused_value_is_explicitly_assigned_to_discard}
//]
    }}

    int Computation() => 0;
}}
class C2
{{
    void M()
    {{
//[
        // {ServicesVSResources.Over_colon}
        Computation();          // {ServicesVSResources.Value_returned_by_invocation_is_implicitly_ignored}
//]
    }}

    int Computation() => 0;
}}
";
        #endregion
        #endregion

        internal StyleViewModel(OptionStore optionStore, IServiceProvider serviceProvider) : base(optionStore, serviceProvider, LanguageNames.CSharp)
        {
            var collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(CodeStyleItems);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AbstractCodeStyleOptionViewModel.GroupName)));

            var qualifyGroupTitle = CSharpVSResources.this_preferences_colon;
            var predefinedTypesGroupTitle = CSharpVSResources.predefined_type_preferences_colon;
            var varGroupTitle = CSharpVSResources.var_preferences_colon;
            var nullCheckingGroupTitle = CSharpVSResources.null_checking_colon;
            var usingsGroupTitle = CSharpVSResources.using_preferences_colon;
            var modifierGroupTitle = ServicesVSResources.Modifier_preferences_colon;
            var codeBlockPreferencesGroupTitle = ServicesVSResources.Code_block_preferences_colon;
            var expressionPreferencesGroupTitle = ServicesVSResources.Expression_preferences_colon;
            var patternMatchingPreferencesGroupTitle = CSharpVSResources.Pattern_matching_preferences_colon;
            var variablePreferencesGroupTitle = ServicesVSResources.Variable_preferences_colon;
            var parameterPreferencesGroupTitle = ServicesVSResources.Parameter_preferences_colon;
            var newLinePreferencesGroupTitle = ServicesVSResources.New_line_preferences_experimental_colon;

            var usingDirectivePlacementPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Inside_namespace, isChecked: false),
                new CodeStylePreference(CSharpVSResources.Outside_namespace, isChecked: false),
            };

            var qualifyMemberAccessPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Prefer_this, isChecked: true),
                new CodeStylePreference(CSharpVSResources.Do_not_prefer_this, isChecked: false),
            };

            var predefinedTypesPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(ServicesVSResources.Prefer_predefined_type, isChecked: true),
                new CodeStylePreference(ServicesVSResources.Prefer_framework_type, isChecked: false),
            };

            var typeStylePreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Prefer_var, isChecked: true),
                new CodeStylePreference(CSharpVSResources.Prefer_explicit_type, isChecked: false),
            };

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyFieldAccess, CSharpVSResources.Qualify_field_access_with_this, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, this, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyPropertyAccess, CSharpVSResources.Qualify_property_access_with_this, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, this, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyMethodAccess, CSharpVSResources.Qualify_method_access_with_this, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, this, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyEventAccess, CSharpVSResources.Qualify_event_access_with_this, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, this, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences));

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, ServicesVSResources.For_locals_parameters_and_members, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionStore, predefinedTypesGroupTitle, predefinedTypesPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, ServicesVSResources.For_member_access_expressions, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionStore, predefinedTypesGroupTitle, predefinedTypesPreferences));

            // Use var
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.VarForBuiltInTypes, CSharpVSResources.For_built_in_types, s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionStore, varGroupTitle, typeStylePreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CSharpVSResources.When_variable_type_is_apparent, s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionStore, varGroupTitle, typeStylePreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.VarElsewhere, CSharpVSResources.Elsewhere, s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionStore, varGroupTitle, typeStylePreferences));

            // Code block
            AddBracesOptions(optionStore, codeBlockPreferencesGroupTitle);
            AddNamespaceDeclarationsOptions(optionStore, codeBlockPreferencesGroupTitle);
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferAutoProperties, ServicesVSResources.analyzer_Prefer_auto_properties, s_preferAutoProperties, s_preferAutoProperties, this, optionStore, codeBlockPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferSimpleUsingStatement, ServicesVSResources.Prefer_simple_using_statement, s_preferSimpleUsingStatement, s_preferSimpleUsingStatement, this, optionStore, codeBlockPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferSystemHashCode, ServicesVSResources.Prefer_System_HashCode_in_GetHashCode, s_preferSystemHashCode, s_preferSystemHashCode, this, optionStore, codeBlockPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferMethodGroupConversion, ServicesVSResources.Prefer_method_group_conversion, s_preferMethodGroupConversion, s_preferMethodGroupConversion, this, optionStore, codeBlockPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferTopLevelStatements, ServicesVSResources.Prefer_top_level_statements, s_preferTopLevelStatements, s_preferProgramMain, this, optionStore, codeBlockPreferencesGroupTitle));

            AddParenthesesOptions(OptionStore);

            // Expression preferences
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferObjectInitializer, ServicesVSResources.Prefer_object_initializer, s_preferObjectInitializer, s_preferObjectInitializer, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferCollectionInitializer, ServicesVSResources.Prefer_collection_initializer, s_preferCollectionInitializer, s_preferCollectionInitializer, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, ServicesVSResources.Prefer_simplified_boolean_expressions, s_preferSimplifiedConditionalExpression, s_preferSimplifiedConditionalExpression, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferSwitchExpression, CSharpVSResources.Prefer_switch_expression, s_preferSwitchExpression, s_preferSwitchExpression, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, ServicesVSResources.Prefer_conditional_expression_over_if_with_assignments, s_preferConditionalExpressionOverIfWithAssignments, s_preferConditionalExpressionOverIfWithAssignments, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferConditionalExpressionOverReturn, ServicesVSResources.Prefer_conditional_expression_over_if_with_returns, s_preferConditionalExpressionOverIfWithReturns, s_preferConditionalExpressionOverIfWithReturns, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferExplicitTupleNames, ServicesVSResources.Prefer_explicit_tuple_name, s_preferExplicitTupleName, s_preferExplicitTupleName, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, ServicesVSResources.Prefer_simple_default_expression, s_preferSimpleDefaultExpression, s_preferSimpleDefaultExpression, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferInferredTupleNames, ServicesVSResources.Prefer_inferred_tuple_names, s_preferInferredTupleName, s_preferInferredTupleName, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, ServicesVSResources.Prefer_inferred_anonymous_type_member_names, s_preferInferredAnonymousTypeMemberName, s_preferInferredAnonymousTypeMemberName, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, ServicesVSResources.Prefer_local_function_over_anonymous_function, s_preferLocalFunctionOverAnonymousFunction, s_preferLocalFunctionOverAnonymousFunction, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferCompoundAssignment, ServicesVSResources.Prefer_compound_assignments, s_preferCompoundAssignments, s_preferCompoundAssignments, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, CSharpVSResources.Prefer_implicit_object_creation_when_type_is_apparent, s_preferImplicitObjectCreationWhenTypeIsApparent, s_preferImplicitObjectCreationWhenTypeIsApparent, this, optionStore, expressionPreferencesGroupTitle));

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferIndexOperator, ServicesVSResources.Prefer_index_operator, s_preferIndexOperator, s_preferIndexOperator, this, optionStore, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferRangeOperator, ServicesVSResources.Prefer_range_operator, s_preferRangeOperator, s_preferRangeOperator, this, optionStore, expressionPreferencesGroupTitle));

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferTupleSwap, ServicesVSResources.Prefer_tuple_swap, s_preferTupleSwap, s_preferTupleSwap, this, optionStore, expressionPreferencesGroupTitle));

            AddExpressionBodyOptions(optionStore, expressionPreferencesGroupTitle);
            AddUnusedValueOptions(optionStore, expressionPreferencesGroupTitle);

            // Pattern matching
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferPatternMatching, CSharpVSResources.Prefer_pattern_matching, s_preferPatternMatching, s_preferPatternMatching, this, optionStore, patternMatchingPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, CSharpVSResources.Prefer_pattern_matching_over_is_with_cast_check, s_preferPatternMatchingOverIsWithCastCheck, s_preferPatternMatchingOverIsWithCastCheck, this, optionStore, patternMatchingPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, CSharpVSResources.Prefer_pattern_matching_over_as_with_null_check, s_preferPatternMatchingOverAsWithNullCheck, s_preferPatternMatchingOverAsWithNullCheck, this, optionStore, patternMatchingPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferNotPattern, CSharpVSResources.Prefer_pattern_matching_over_mixed_type_check, s_preferPatternMatchingOverMixedTypeCheck, s_preferPatternMatchingOverMixedTypeCheck, this, optionStore, patternMatchingPreferencesGroupTitle));

            // Variable preferences
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, ServicesVSResources.Prefer_inlined_variable_declaration, s_preferInlinedVariableDeclaration, s_preferInlinedVariableDeclaration, this, optionStore, variablePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, ServicesVSResources.Prefer_deconstructed_variable_declaration, s_preferDeconstructedVariableDeclaration, s_preferDeconstructedVariableDeclaration, this, optionStore, variablePreferencesGroupTitle));

            // Null preferences.
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferThrowExpression, CSharpVSResources.Prefer_throw_expression, s_preferThrowExpression, s_preferThrowExpression, this, optionStore, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferConditionalDelegateCall, CSharpVSResources.Prefer_conditional_delegate_call, s_preferConditionalDelegateCall, s_preferConditionalDelegateCall, this, optionStore, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferCoalesceExpression, ServicesVSResources.Prefer_coalesce_expression, s_preferCoalesceExpression, s_preferCoalesceExpression, this, optionStore, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferNullPropagation, ServicesVSResources.Prefer_null_propagation, s_preferNullPropagation, s_preferNullPropagation, this, optionStore, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, CSharpVSResources.Prefer_is_null_for_reference_equality_checks, s_preferIsNullOverReferenceEquals, s_preferIsNullOverReferenceEquals, this, optionStore, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, CSharpVSResources.Prefer_null_check_over_type_check, s_preferNullcheckOverTypeCheck, s_preferNullcheckOverTypeCheck, this, optionStore, nullCheckingGroupTitle));

            // Using directive preferences.
            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<AddImportPlacement>(
                CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, CSharpVSResources.Preferred_using_directive_placement,
                [AddImportPlacement.InsideNamespace, AddImportPlacement.OutsideNamespace],
                s_usingDirectivePlacement, this, optionStore, usingsGroupTitle, usingDirectivePlacementPreferences));

            // Modifier preferences.
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferReadonly, ServicesVSResources.Prefer_readonly_fields, s_preferReadonly, s_preferReadonly, this, optionStore, modifierGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferReadOnlyStruct, ServicesVSResources.Prefer_read_only_struct, s_preferReadOnlyStruct, s_preferReadOnlyStruct, this, optionStore, modifierGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferStaticLocalFunction, ServicesVSResources.Prefer_static_local_functions, s_preferStaticLocalFunction, s_preferStaticLocalFunction, this, optionStore, modifierGroupTitle));

            // Parameter preferences
            AddParameterOptions(optionStore, parameterPreferencesGroupTitle);

            // New line preferences.
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.AllowMultipleBlankLines, ServicesVSResources.Allow_multiple_blank_lines, s_allow_multiple_blank_lines_true, s_allow_multiple_blank_lines_false, this, optionStore, newLinePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, CSharpVSResources.Allow_embedded_statements_on_same_line, s_allow_embedded_statements_on_same_line_true, s_allow_embedded_statements_on_same_line_false, this, optionStore, newLinePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, CSharpVSResources.Allow_blank_lines_between_consecutive_braces, s_allow_blank_line_between_consecutive_braces_true, s_allow_blank_line_between_consecutive_braces_false, this, optionStore, newLinePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, ServicesVSResources.Allow_statement_immediately_after_block, s_allow_statement_immediately_after_block_true, s_allow_statement_immediately_after_block_false, this, optionStore, newLinePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, CSharpVSResources.Allow_blank_line_after_colon_in_constructor_initializer, s_allow_blank_line_after_colon_in_constructor_initializer_true, s_allow_blank_line_after_colon_in_constructor_initializer_false, this, optionStore, newLinePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CSharpVSResources.Allow_blank_line_after_token_in_conditional_expression, s_allow_blank_line_after_token_in_conditional_expression_true, s_allow_blank_line_after_token_in_conditional_expression_false, this, optionStore, newLinePreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CSharpVSResources.Allow_blank_line_after_token_in_arrow_expression_clause, s_allow_blank_line_after_token_in_arrow_expression_clause_true, s_allow_blank_line_after_token_in_arrow_expression_clause_false, this, optionStore, newLinePreferencesGroupTitle));
        }

        private void AddParenthesesOptions(OptionStore optionStore)
        {
            AddParenthesesOption(
                optionStore, CodeStyleOptions2.ArithmeticBinaryParentheses,
                CSharpVSResources.In_arithmetic_binary_operators,
                [s_arithmeticBinaryAlwaysForClarity, s_arithmeticBinaryNeverIfUnnecessary],
                defaultAddForClarity: true);

            AddParenthesesOption(
                optionStore, CodeStyleOptions2.OtherBinaryParentheses,
                CSharpVSResources.In_other_binary_operators,
                [s_otherBinaryAlwaysForClarity, s_otherBinaryNeverIfUnnecessary],
                defaultAddForClarity: true);

            AddParenthesesOption(
                optionStore, CodeStyleOptions2.RelationalBinaryParentheses,
                CSharpVSResources.In_relational_binary_operators,
                [s_relationalBinaryAlwaysForClarity, s_relationalBinaryNeverIfUnnecessary],
                defaultAddForClarity: true);

            AddParenthesesOption(
                optionStore, CodeStyleOptions2.OtherParentheses,
                ServicesVSResources.In_other_operators,
                [s_otherParenthesesAlwaysForClarity, s_otherParenthesesNeverIfUnnecessary],
                defaultAddForClarity: false);
        }

        private void AddBracesOptions(OptionStore optionStore, string bracesPreferenceGroupTitle)
        {
            var bracesPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(ServicesVSResources.Yes, isChecked: false),
                new CodeStylePreference(ServicesVSResources.No, isChecked: false),
                new CodeStylePreference(CSharpVSResources.When_on_multiple_lines, isChecked: false),
            };

            var enumValues = new[] { PreferBracesPreference.Always, PreferBracesPreference.None, PreferBracesPreference.WhenMultiline };

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<PreferBracesPreference>(
                CSharpCodeStyleOptions.PreferBraces,
                ServicesVSResources.Prefer_braces,
                enumValues,
                [s_preferBraces, s_doNotPreferBraces, s_preferBracesWhenMultiline],
                this, optionStore, bracesPreferenceGroupTitle, bracesPreferences));
        }

        private void AddNamespaceDeclarationsOptions(OptionStore optionStore, string group)
        {
            var preferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Block_scoped, isChecked: false),
                new CodeStylePreference(CSharpVSResources.File_scoped, isChecked: false),
            };

            var enumValues = new[] { NamespaceDeclarationPreference.BlockScoped, NamespaceDeclarationPreference.FileScoped };

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<NamespaceDeclarationPreference>(
                CSharpCodeStyleOptions.NamespaceDeclarations,
                ServicesVSResources.Namespace_declarations,
                enumValues,
                [s_preferBlockNamespace, s_preferFileScopedNamespace],
                this, optionStore, group, preferences));
        }

        private void AddExpressionBodyOptions(OptionStore optionStore, string expressionPreferencesGroupTitle)
        {
            var expressionBodyPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Never, isChecked: false),
                new CodeStylePreference(CSharpVSResources.When_possible, isChecked: false),
                new CodeStylePreference(CSharpVSResources.When_on_single_line, isChecked: false),
            };

            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                ServicesVSResources.Use_expression_body_for_methods,
                enumValues,
                [s_preferBlockBodyForMethods, s_preferExpressionBodyForMethods, s_preferExpressionBodyForMethods],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                ServicesVSResources.Use_expression_body_for_constructors,
                enumValues,
                [s_preferBlockBodyForConstructors, s_preferExpressionBodyForConstructors, s_preferExpressionBodyForConstructors],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                ServicesVSResources.Use_expression_body_for_operators,
                enumValues,
                [s_preferBlockBodyForOperators, s_preferExpressionBodyForOperators, s_preferExpressionBodyForOperators],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                ServicesVSResources.Use_expression_body_for_properties,
                enumValues,
                [s_preferBlockBodyForProperties, s_preferExpressionBodyForProperties, s_preferExpressionBodyForProperties],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                ServicesVSResources.Use_expression_body_for_indexers,
                enumValues,
                [s_preferBlockBodyForIndexers, s_preferExpressionBodyForIndexers, s_preferExpressionBodyForIndexers],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                ServicesVSResources.Use_expression_body_for_accessors,
                enumValues,
                [s_preferBlockBodyForAccessors, s_preferExpressionBodyForAccessors, s_preferExpressionBodyForAccessors],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                ServicesVSResources.Use_expression_body_for_lambdas,
                enumValues,
                [s_preferBlockBodyForLambdas, s_preferExpressionBodyForLambdas, s_preferExpressionBodyForLambdas],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions,
                ServicesVSResources.Use_expression_body_for_local_functions,
                enumValues,
                [s_preferBlockBodyForLocalFunctions, s_preferExpressionBodyForLocalFunctions, s_preferExpressionBodyForLocalFunctions],
                this, optionStore, expressionPreferencesGroupTitle, expressionBodyPreferences));
        }

        private void AddUnusedValueOptions(OptionStore optionStore, string expressionPreferencesGroupTitle)
        {
            var unusedValuePreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Unused_local, isChecked: false),
                new CodeStylePreference(CSharpVSResources.Discard, isChecked: true),
            };

            var enumValues = new[]
            {
                UnusedValuePreference.UnusedLocalVariable,
                UnusedValuePreference.DiscardVariable
            };

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<UnusedValuePreference>(
                CSharpCodeStyleOptions.UnusedValueAssignment,
                ServicesVSResources.Avoid_unused_value_assignments,
                enumValues,
                [s_avoidUnusedValueAssignmentUnusedLocal, s_avoidUnusedValueAssignmentDiscard],
                this,
                optionStore,
                expressionPreferencesGroupTitle,
                unusedValuePreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<UnusedValuePreference>(
                CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                ServicesVSResources.Avoid_expression_statements_that_implicitly_ignore_value,
                enumValues,
                [s_avoidUnusedValueExpressionStatementUnusedLocal, s_avoidUnusedValueExpressionStatementDiscard],
                this,
                optionStore,
                expressionPreferencesGroupTitle,
                unusedValuePreferences));
        }

        private void AddParameterOptions(OptionStore optionStore, string parameterPreferencesGroupTitle)
        {
            var examples = new[]
            {
                s_avoidUnusedParametersNonPublicMethods,
                s_avoidUnusedParametersAllMethods
            };

            AddUnusedParameterOption(optionStore, parameterPreferencesGroupTitle, examples);
        }
    }
}
