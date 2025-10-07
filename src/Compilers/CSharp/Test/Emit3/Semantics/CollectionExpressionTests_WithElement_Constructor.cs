// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class CollectionExpressionTests_WithElement_Constructors : CSharpTestBase
{
    private static string IncludeExpectedOutput(string expectedOutput) => expectedOutput;

    #region Position and Multiple With Tests

    [Fact]
    public void WithElement_MustBeFirstElement()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [1, with(capacity: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,30): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [1, with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 30));
    }

    [Fact]
    public void WithElement_CannotAppearMultipleTimes()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(capacity: 10), with(capacity: 20)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,47): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [with(capacity: 10), with(capacity: 20)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 47));
    }

    [Fact]
    public void WithElement_CannotAppearAfterElements()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [1, 2, with(capacity: 10), 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,33): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [1, 2, with(capacity: 10), 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 33));
    }

    [Fact]
    public void WithElement_CannotAppearAfterSpread()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    var other = new int[] { 1, 2 };
                    List<int> list = [.. other, with(capacity: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (8,37): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [.. other, with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 37));
    }

    [Fact]
    public void WithElement_ValidAsFirstElement()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(capacity: 10), 1, 2, 3];
                    Console.WriteLine(list.Capacity);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10")).VerifyIL(
            "C.Main",
            """
            {
              // Code size       39 (0x27)
              .maxstack  3
              IL_0000:  ldc.i4.s   10
              IL_0002:  newobj     "System.Collections.Generic.List<int>..ctor(int)"
              IL_0007:  dup
              IL_0008:  ldc.i4.1
              IL_0009:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_000e:  dup
              IL_000f:  ldc.i4.2
              IL_0010:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_0015:  dup
              IL_0016:  ldc.i4.3
              IL_0017:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_001c:  callvirt   "int System.Collections.Generic.List<int>.Capacity.get"
              IL_0021:  call       "void System.Console.WriteLine(int)"
              IL_0026:  ret
            }
            """);
    }

    #endregion

    #region Basic Constructor Tests

    [Fact]
    public void WithElement_ParameterlessConstructor()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(), 1, 2, 3];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("3"));
    }

    [Fact]
    public void WithElement_SingleParameterConstructor()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(capacity: 100), 1, 2, 3];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("3"));
    }

    [Fact]
    public void WithElement_MultipleParameterConstructor()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int CustomProperty { get; }
                
                public MyList(int capacity, int customValue) : base(capacity)
                {
                    CustomProperty = customValue;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(capacity: 100, customValue: 42), 1, 2];
                    Console.WriteLine($"{list.Count},{list.CustomProperty}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2,42"));
    }

    #endregion

    #region Argument Count Tests

    [Fact]
    public void WithElement_NonExistentNamedParameter()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(capacity: 10, extraArg: 20)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,46): error CS1739: The best overload for 'List' does not have a parameter named 'extraArg'
            //         List<int> list = [with(capacity: 10, extraArg: 20)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "extraArg").WithArguments("List", "extraArg").WithLocation(7, 46));
    }

    [Fact]
    public void WithElement_TooFewArguments_RequiredParameter()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int capacity, string name) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(capacity: 10)];
                }
            }
            """;

        // PROTOTYPE: This error is not correct.
        CreateCompilation(source).VerifyDiagnostics(
            // (12,28): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
            //         MyList<int> list = [with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with(capacity: 10)]").WithLocation(12, 28));
    }

    [Fact]
    public void WithElement_OptionalParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                
                public MyList(int capacity = 0, string name = "default") : base(capacity)
                {
                    Name = name;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list1 = [with(), 1];
                    MyList<int> list2 = [with(capacity: 10), 2];
                    MyList<int> list3 = [with(name: "custom"), 3];
                    MyList<int> list4 = [with(capacity: 20, name: "both"), 4];
                    
                    Console.WriteLine($"{list1.Name}-{list1.Capacity},{list2.Name}-{list2.Capacity},{list3.Name}-{list3.Capacity},{list4.Name}-{list4.Capacity}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("default-4,default-10,custom-4,both-20"));
    }

    #endregion

    #region Named Arguments Tests

    [Fact]
    public void WithElement_CorrectNamedArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value1 { get; }
                public int Value2 { get; }
                
                public MyList(int first, int second) : base()
                {
                    Value1 = first;
                    Value2 = second;
                }
            }
            
            class C
            {
                static int GetSecond()
                {
                    Console.Write("GetSecond called. ");
                    return 20;
                }
            
                static int GetFirst()
                {
                    Console.Write("GetFirst called. ");
                    return 10;
                }

                static void Main()
                {
                    MyList<int> list = [with(second: GetSecond(), first: GetFirst()), 1];
                    Console.WriteLine($"{list.Value1},{list.Value2}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("GetSecond called. GetFirst called. 10,20"))
            .VerifyIL("C.Main", """
            {
              // Code size       63 (0x3f)
              .maxstack  3
              .locals init (MyList<int> V_0, //list
                            int V_1)
              IL_0000:  call       "int C.GetSecond()"
              IL_0005:  stloc.1
              IL_0006:  call       "int C.GetFirst()"
              IL_000b:  ldloc.1
              IL_000c:  newobj     "MyList<int>..ctor(int, int)"
              IL_0011:  dup
              IL_0012:  ldc.i4.1
              IL_0013:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_0018:  stloc.0
              IL_0019:  ldstr      "{0},{1}"
              IL_001e:  ldloc.0
              IL_001f:  callvirt   "int MyList<int>.Value1.get"
              IL_0024:  box        "int"
              IL_0029:  ldloc.0
              IL_002a:  callvirt   "int MyList<int>.Value2.get"
              IL_002f:  box        "int"
              IL_0034:  call       "string string.Format(string, object, object)"
              IL_0039:  call       "void System.Console.WriteLine(string)"
              IL_003e:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_IncorrectNamedArguments()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(wrongName: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,32): error CS1739: The best overload for 'List' does not have a parameter named 'wrongName'
            //         List<int> list = [with(wrongName: 10)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "wrongName").WithArguments("List", "wrongName").WithLocation(7, 32));
    }

    [Fact]
    public void WithElement_MixedPositionalAndNamedArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value1 { get; }
                public int Value2 { get; }
                public string Name { get; }
                
                public MyList(int first, int second, string name = "default") : base()
                {
                    Value1 = first;
                    Value2 = second;
                    Name = name;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(10, name: "test", second: 20), 1];
                    Console.WriteLine($"{list.Value1},{list.Value2},{list.Name}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10,20,test"));
    }

    [Fact]
    public void WithElement_NamedArgumentAfterPositional_Invalid()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int first, int second) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10, first: 20)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,38): error CS1744: Named argument 'first' specifies a parameter for which a positional argument has already been given
            //         MyList<int> list = [with(10, first: 20)];
            Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "first").WithArguments("first").WithLocation(12, 38));
    }

    [Fact]
    public void WithElement_NamedArgumentBeforePositional()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int first, int second) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(first: 20, 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_NamedArgumentInWrongPosition()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int first, int second) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(second: 20, 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,34): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
            //         MyList<int> list = [with(second: 20, 10)];
            Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(12, 34));
    }

    #endregion

    #region Ref/In/Out Parameter Tests

    [Fact]
    public void WithElement_RefParameters()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(ref int value) : base() { }
            }
            
            class C
            {
                void M()
                {
                    int x = 10;
                    MyList<int> list = [with(ref x)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("in ")]
    [InlineData("")]
    public void WithElement_InParameters(string modifier)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            class MyList<T> : List<T>
            {
                public int Value { get; }
                public MyList(in int value) : base() 
                { 
                    Console.Write(value + " ");
                    Value = value;
                    Unsafe.AsRef(value) = 10;
                }
            }
            
            class C
            {
                static void Main()
                {
                    int x = 42;
                    MyList<int> list = [with({{modifier}}x), 1];
                    Console.WriteLine(x);
                }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net90, expectedOutput: IncludeExpectedOutput("42 10"));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_OutParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(out int value) : base() 
                { 
                    value = 42;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(out var x), x, x + 1];
                    Console.WriteLine($"{list.Count},{x}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2,42"));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_OutVar_UsedInLaterElements()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(out int value, int capacity = 0) : base(capacity) 
                { 
                    value = 100;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(out var x), x, x * 2, x * 3];
                    Console.WriteLine($"{list[0]},{list[1]},{list[2]}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("100,200,300"));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_MultipleOutParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(out int value1, out int value2) : base() 
                { 
                    value1 = 10;
                    value2 = 20;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(out var x, out var y), x, y, x + y];
                    Console.WriteLine($"{list[0]},{list[1]},{list[2]}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10,20,30"));
    }

    #endregion

    #region Params Tests

    [Fact]
    public void WithElement_ParamsArray()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            class MyList<T> : List<T>
            {
                public int[] Values { get; }
                
                public MyList(params int[] values) : base()
                {
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list1 = [with(), 1];
                    MyList<int> list2 = [with(10), 2];
                    MyList<int> list3 = [with(10, 20, 30), 3];
                    
                    Console.WriteLine($"{list1.Values.Length},{list2.Values.Length},{list3.Values.Length}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("0,1,3"));
    }

    [Fact]
    public void WithElement_ParamsWithNamedArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                public int[] Values { get; }
                
                public MyList(string name, params int[] values) : base()
                {
                    Name = name;
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(name: "test", values: new int[] { 1, 2, 3 }), 4];
                    Console.WriteLine($"{list.Name},{list.Values.Length}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("test,3"));
    }

    [Fact]
    public void WithElement_ParamsCollection()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            class MyList<T> : List<T>
            {
                public IEnumerable<int> Values { get; }
                
                public MyList(params IEnumerable<int> values) : base()
                {
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(new int[] { 1, 2, 3 }), 4];
                    Console.WriteLine($"{list.Values.Count()}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("3"));
    }

    #endregion

    #region Dynamic Tests

    [Fact]
    public void WithElement_DynamicArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public object Value { get; }
                
                public MyList(object value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    dynamic d = 42;
                    MyList<int> list = [with(d), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CreateCompilation(source, references: [CSharpRef]).VerifyDiagnostics(
            // (19,34): error CS9337: Collection arguments cannot be dynamic
            //         MyList<int> list = [with(d), 1];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(19, 34));
    }

    [Fact]
    public void WithElement_DynamicNamedArguments()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(object value) : base() { }
            }
            
            class C
            {
                void M()
                {
                    dynamic d = 42;
                    MyList<int> list = [with(value: d)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (13,41): error CS9337: Collection arguments cannot be dynamic
            //         MyList<int> list = [with(value: d)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(13, 41));
    }

    [Fact]
    public void WithElement_DynamicType()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    dynamic d = new List<int>();
                    var list = [with(capacity: 10)] as dynamic;
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (8,20): error CS9176: There is no target type for the collection expression.
            //         var list = [with(capacity: 10)] as dynamic;
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[with(capacity: 10)]").WithLocation(8, 20));
    }

    #endregion

    #region ArgList Tests

    [ConditionalFact(typeof(WindowsOnly))]
    public void WithElement_ArgList()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList : List<int>
            {
                public MyList(__arglist) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList list = [with(__arglist(10, "test"))];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void WithElement_ArgList_Empty()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList : List<int>
            {
                public MyList(__arglist) : base() 
                {
                    Console.WriteLine("ArgList constructor called");
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList list = [with(__arglist()), 1];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("ArgList constructor called\r\n1"));
    }

    #endregion

    #region Constructor Overload Resolution Tests

    [Fact]
    public void WithElement_OverloadResolution_ExactMatch()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string ConstructorUsed { get; }
                
                public MyList(int capacity) : base(capacity)
                {
                    ConstructorUsed = "int";
                }
                
                public MyList(long capacity) : base((int)capacity)
                {
                    ConstructorUsed = "long";
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list1 = [with(10), 1];
                    MyList<int> list2 = [with(10L), 2];
                    Console.WriteLine($"{list1.ConstructorUsed},{list2.ConstructorUsed}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("int,long"));
    }

    [Fact]
    public void WithElement_OverloadResolution_Ambiguous()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int value) : base() { }
                public MyList(long value) : base() { }
            }
            
            class C
            {
                void M()
                {
                    short s = 10;
                    MyList<int> list = [with(s)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_OverloadResolution_BestMatch()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string ConstructorUsed { get; }
                
                public MyList(object value) : base()
                {
                    ConstructorUsed = "object";
                }
                
                public MyList(int value) : base()
                {
                    ConstructorUsed = "int";
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(42), 1];
                    Console.WriteLine(list.ConstructorUsed);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("int"));
    }

    [Fact]
    public void WithElement_NoMatchingConstructor()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string value) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(42)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,34): error CS1503: Argument 1: cannot convert from 'int' to 'string'
            //         MyList<int> list = [with(42)];
            Diagnostic(ErrorCode.ERR_BadArgType, "42").WithArguments("1", "int", "string").WithLocation(12, 34));
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void WithElement_PrivateConstructor()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                private MyList(int capacity) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,28): error CS1729: 'MyList<int>' does not contain a constructor that takes 0 arguments
            //         MyList<int> list = [with(10)];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[with(10)]").WithArguments("MyList<int>", "0").WithLocation(12, 28));
    }

    [Fact]
    public void WithElement_ProtectedConstructor()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                protected MyList(int capacity) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,28): error CS1729: 'MyList<int>' does not contain a constructor that takes 0 arguments
            //         MyList<int> list = [with(10)];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[with(10)]").WithArguments("MyList<int>", "0").WithLocation(12, 28));
    }

    [Fact]
    public void WithElement_InternalConstructor_SameAssembly()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                internal MyList(int capacity) : base(capacity) 
                {
                    Console.Write("Internal constructor ");
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(10), 1];
                    Console.WriteLine(list.Capacity + " " + list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("Internal constructor 10 1"));
    }

    #endregion

    #region Constraint Tests

    [Fact]
    public void WithElement_TypeConstraints()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T> where T : class
            {
                public MyList(T item) : base()
                {
                    Add(item);
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<string> list = [with("first"), "second"];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2"));
    }

    [Fact]
    public void WithElement_TypeConstraints_Violation()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T> where T : class
            {
                public MyList(T item) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(42)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyList<T>'
            //         MyList<int> list = [with(42)];
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("MyList<T>", "T", "int").WithLocation(12, 16));
    }

    #endregion

    #region Null and Default Tests

    [Fact]
    public void WithElement_NullArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Value { get; }
                
                public MyList(string value) : base()
                {
                    Value = value ?? "null";
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with((string)null), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("null"));
    }

    [Fact]
    public void WithElement_DefaultArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value { get; }
                
                public MyList(int value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(default(int)), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("0"));
    }

    #endregion

    #region Error Recovery Tests

    [Fact]
    public void WithElement_SyntaxError_MissingCloseParen()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(capacity: 10];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,44): error CS1026: ) expected
            //         List<int> list = [with(capacity: 10];
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "]").WithLocation(7, 44),
            // (7,45): error CS1003: Syntax error, ']' expected
            //         List<int> list = [with(capacity: 10];
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(7, 45));
    }

    [Fact]
    public void WithElement_Error_MissingArguments()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,27): error CS0103: The name 'with' does not exist in the current context
            //         List<int> list = [with];
            Diagnostic(ErrorCode.ERR_NameNotInContext, "with").WithArguments("with").WithLocation(7, 27));
    }

    [Fact]
    public void WithElement_EmptyCollection()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(capacity: 100)];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("0"));
    }

    #endregion

    #region Nested Types Tests

    [Fact]
    public void WithElement_NestedType()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class Outer
            {
                public class MyList<T> : List<T>
                {
                    public int Value { get; }
                    
                    public MyList(int value) : base()
                    {
                        Value = value;
                    }
                }
            }
            
            class C
            {
                static void Main()
                {
                    Outer.MyList<int> list = [with(42), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void WithElement_WithExpressionInArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value { get; }
                
                public MyList(int value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    int x = 10;
                    MyList<int> list = [with(x + 32), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    [Fact]
    public void WithElement_WithMethodCall()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Value { get; }
                
                public MyList(string value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static string GetValue() => "test";
                
                static void Main()
                {
                    MyList<int> list = [with(GetValue()), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("test"));
    }

    [Fact]
    public void WithElement_WithLambda()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public Func<int> ValueFunc { get; }
                
                public MyList(Func<int> func) : base()
                {
                    ValueFunc = func;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(() => 42), 1];
                    Console.WriteLine(list.ValueFunc());
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    #endregion
}
