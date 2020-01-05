// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_CallerInfoAttributes : WellKnownAttributesTestBase
    {
        [Fact]
        public void TestCallerInfoAttributesWithSaneDefaultValues()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Test {
    static void LogCallerLineNumber([CallerLineNumber] int lineNumber = -1) { }

    static void LogCallerFilePath([CallerFilePath] string filePath = """") { }

    static void LogCallerMemberName([CallerMemberName] string memberName = """") { }
}";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestBadCallerInfoAttributesWithoutDefaultValues()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Test {
    static void LogCallerLineNumber([CallerLineNumber] int lineNumber) { }

    static void LogCallerFilePath([CallerFilePath] string filePath) { }

    static void LogCallerMemberName([CallerMemberName] string memberName) { }
}";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,38): error CS4020: The CallerLineNumberAttribute may only be applied to parameters with default values
                //     static void LogCallerLineNumber([CallerLineNumber] int lineNumber) { }
                Diagnostic(ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue, @"CallerLineNumber").WithLocation(5, 38),

                // (7,36): error CS4021: The CallerFilePathAttribute may only be applied to parameters with default values
                //     static void LogCallerFilePath([CallerFilePath] string filePath) { }
                Diagnostic(ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue, @"CallerFilePath").WithLocation(7, 36),

                // (9,38): error CS4022: The CallerMemberNameAttribute may only be applied to parameters with default values
                //     static void LogCallerMemberName([CallerMemberName] string memberName) { }
                Diagnostic(ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue, @"CallerMemberName").WithLocation(9, 38));
        }

        [Fact]
        public void TestConversionForCallerLineNumber()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

class Test {
    static void LogCallerLineNumber1([CallerLineNumber, Optional, DefaultParameterValue(1 )] decimal lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber2([CallerLineNumber, Optional, DefaultParameterValue(2 )] double  lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber3([CallerLineNumber, Optional, DefaultParameterValue(3 )] float   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber4([CallerLineNumber, Optional, DefaultParameterValue(4 )] int     lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber5([CallerLineNumber, Optional, DefaultParameterValue(5u)] uint    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber6([CallerLineNumber, Optional, DefaultParameterValue(6 )] long    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber7([CallerLineNumber, Optional, DefaultParameterValue(7u)] ulong   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber8([CallerLineNumber, Optional, DefaultParameterValue(8 )] object  lineNumber) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerLineNumber9 ([CallerLineNumber] decimal lineNumber =  9  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber10([CallerLineNumber] double  lineNumber = 10  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber11([CallerLineNumber] float   lineNumber = 11  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber12([CallerLineNumber] int     lineNumber = 12  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber13([CallerLineNumber] uint    lineNumber = 13  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber14([CallerLineNumber] long    lineNumber = 14  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber15([CallerLineNumber] ulong   lineNumber = 15  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber16([CallerLineNumber] object  lineNumber = null) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerLineNumber17([CallerLineNumber, Optional, DefaultParameterValue(17 )] decimal? lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber18([CallerLineNumber, Optional, DefaultParameterValue(18 )] double?  lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber19([CallerLineNumber, Optional, DefaultParameterValue(19 )] float?   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber20([CallerLineNumber, Optional, DefaultParameterValue(20 )] int?     lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber21([CallerLineNumber, Optional, DefaultParameterValue(21u)] uint?    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber22([CallerLineNumber, Optional, DefaultParameterValue(22 )] long?    lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber23([CallerLineNumber, Optional, DefaultParameterValue(23u)] ulong?   lineNumber) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerLineNumber25([CallerLineNumber] decimal? lineNumber = 25  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber26([CallerLineNumber] double?  lineNumber = 26  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber27([CallerLineNumber] float?   lineNumber = 27  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber28([CallerLineNumber] int?     lineNumber = 28  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber29([CallerLineNumber] uint?    lineNumber = 29  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber30([CallerLineNumber] long?    lineNumber = 30  ) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber31([CallerLineNumber] ulong?   lineNumber = 31  ) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerFilePath1([CallerFilePath] string filePath = """") { }
    static void LogCallerFilePath2([CallerFilePath] object filePath = null) { }
    static void LogCallerFilePath3([CallerFilePath] IComparable filePath = null) { }

    static void LogCallerMemberName1([CallerMemberName] string memberName = """") { }
    static void LogCallerMemberName2([CallerMemberName] object memberName = null) { }
    static void LogCallerMemberName3([CallerMemberName] IComparable memberName = null) { }

    public static void Main() {
        LogCallerLineNumber1();
        LogCallerLineNumber2();
        LogCallerLineNumber3();
        LogCallerLineNumber4();
        LogCallerLineNumber5();
        LogCallerLineNumber6();
        LogCallerLineNumber7();
        LogCallerLineNumber8();
        LogCallerLineNumber9();
        LogCallerLineNumber10();
        LogCallerLineNumber11();
        LogCallerLineNumber12();
        LogCallerLineNumber13();
        LogCallerLineNumber14();
        LogCallerLineNumber15();
        LogCallerLineNumber16();
        LogCallerLineNumber17();
        LogCallerLineNumber18();
        LogCallerLineNumber19();
        LogCallerLineNumber20();
        LogCallerLineNumber21();
        LogCallerLineNumber22();
        LogCallerLineNumber23();
        LogCallerLineNumber25();
        LogCallerLineNumber26();
        LogCallerLineNumber27();
        LogCallerLineNumber28();
        LogCallerLineNumber29();
        LogCallerLineNumber30();
        LogCallerLineNumber31();
    }
}";

            string expected = @"
line: 50
line: 51
line: 52
line: 53
line: 54
line: 55
line: 56
line: 57
line: 58
line: 59
line: 60
line: 61
line: 62
line: 63
line: 64
line: 65
line: 66
line: 67
line: 68
line: 69
line: 70
line: 71
line: 72
line: 73
line: 74
line: 75
line: 76
line: 77
line: 78
line: 79
";
            var compilation = CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestDelegateInvoke()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

class Test {
    static void LogCallerLineNumber1(int lineNumber = -1) { Console.WriteLine(""line: "" + lineNumber); }

    public static void Main() {
        List<Action> list = new List<Action>();
        list.Add(() => LogCallerLineNumber1());
        list.Add(() => LogCallerLineNumber1());
        list.Add(() => LogCallerLineNumber1());
        
        foreach (var x in list) {
            x();
        }
    }
}";

            string expected = @"
line: -1
line: -1
line: -1
";

            var compilation = CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestConversionForCallerInfoAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

class Test {
    static void LogCallerLineNumber1([CallerLineNumber, Optional, DefaultParameterValue(1)] int lineNumber) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber2([CallerLineNumber] long lineNumber   = 2) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber3([CallerLineNumber] double lineNumber = 3) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber4([CallerLineNumber] float lineNumber  = 4) { Console.WriteLine(""line: "" + lineNumber); }
    static void LogCallerLineNumber5([CallerLineNumber] int? lineNumber   = 5) { Console.WriteLine(""line: "" + lineNumber); }

    static void LogCallerFilePath1([CallerFilePath] string filePath = """") { }
    static void LogCallerFilePath2([CallerFilePath] object filePath = null) { }
    static void LogCallerFilePath3([CallerFilePath] IComparable filePath = null) { }

    static void LogCallerMemberName1([CallerMemberName] string memberName = """") { }
    static void LogCallerMemberName2([CallerMemberName] object memberName = null) { }
    static void LogCallerMemberName3([CallerMemberName] IComparable memberName = null) { }

    public static void Main() {
        LogCallerLineNumber1();
        LogCallerLineNumber2();
        LogCallerLineNumber3();
        LogCallerLineNumber4();
        LogCallerLineNumber5();
    }
}";

            string expected = @"
line: 22
line: 23
line: 24
line: 25
line: 26
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestBadConversionForCallerInfoAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Test {
    static void LogCallerLineNumber1([CallerLineNumber] string lineNumber = """") { }
    static void LogCallerLineNumber2([CallerLineNumber] char lineNumber = '\0') { }
    static void LogCallerLineNumber3([CallerLineNumber] bool lineNumber = false) { }
    static void LogCallerLineNumber3([CallerLineNumber] short lineNumber = 0) { }
    static void LogCallerLineNumber3([CallerLineNumber] ushort lineNumber = 0) { }

    static void LogCallerFilePath1([CallerFilePath] int filePath = 0) { }
    static void LogCallerFilePath2([CallerFilePath] long filePath = 0) { }
    static void LogCallerFilePath3([CallerFilePath] double filePath = 0) { }
    static void LogCallerFilePath4([CallerFilePath] float filePath = 0) { }
    static void LogCallerFilePath5([CallerFilePath] int? filePath = 0) { }

    static void LogCallerMemberName1([CallerMemberName] int memberName = 0) { }
    static void LogCallerMemberName2([CallerMemberName] long memberName = 0) { }
    static void LogCallerMemberName3([CallerMemberName] double memberName = 0) { }
    static void LogCallerMemberName4([CallerMemberName] float memberName = 0) { }
    static void LogCallerMemberName5([CallerMemberName] int? memberName = 0) { }
}";

            CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(5, 39).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(6, 39).WithArguments("int", "char"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(7, 39).WithArguments("int", "bool"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(8, 39).WithArguments("int", "short"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(9, 39).WithArguments("int", "ushort"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(11, 37).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(12, 37).WithArguments("string", "long"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(13, 37).WithArguments("string", "double"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(14, 37).WithArguments("string", "float"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(15, 37).WithArguments("string", "int?"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(17, 39).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(18, 39).WithArguments("string", "long"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(19, 39).WithArguments("string", "double"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(20, 39).WithArguments("string", "float"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(21, 39).WithArguments("string", "int?"));
        }

        [Fact]
        public void TestCallerLineNumber()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    static void Log(
        string message,
        [CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""message: "" + message);
        Console.WriteLine(""line: "" + lineNumber);
    }

    public static void Main()
    {
        Log(""something happened"");
        // comment
        Log
            // comment
            (
            // comment
            ""something happened""
            // comment
            )
            // comment
            ;
        // comment
    }
}";

            var expected = @"
message: something happened
line: 17
message: something happened
line: 21
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberImplicitCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class A
{
    public A([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

class B : A
{
}

class Test
{
    public static void Main()
    {
        new B();
    }
}";

            var expected = @"
line: -1
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberConstructorCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class A
{
    public A([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

class Test
{
    public static void Main()
    {
        new A();
    }
}";

            var expected = @"
line: 17
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberCustomAttributeConstructorCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Reflection;
using System;

class MyCallerInfoAttribute : Attribute
{
    public MyCallerInfoAttribute(
        string message,
        [CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""message: "" + message);
        Console.WriteLine(""line: "" + lineNumber);
    }
}

class MyCallerInfo2Attribute : Attribute
{
    public MyCallerInfo2Attribute([CallerLineNumber] int lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

[MyCallerInfo(""this is a message"")]
class A
{
}

// comment
[
// comment
MyCallerInfo
    // comment
    (
    // comment
    ""this is a message""
    // comment
    )
// comment
]
// comment
class B
{
}

[MyCallerInfo2]
class C
{
}

// comment
[
// comment
MyCallerInfo2
// comment
]
// comment
class D
{
}

class Test
{
    public static void Main()
    {
        typeof(A).GetCustomAttribute(typeof(MyCallerInfoAttribute), false);
        typeof(B).GetCustomAttribute(typeof(MyCallerInfoAttribute), false);
        typeof(C).GetCustomAttribute(typeof(MyCallerInfo2Attribute), false);
        typeof(D).GetCustomAttribute(typeof(MyCallerInfo2Attribute), false);
    }
}";

            var expected = @"
message: this is a message
line: 25
message: this is a message
line: 33
line: 47
line: 55
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberMemberCall()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

public class B
{
    public void Log([CallerLineNumber] int a = -1)
    {
        Console.WriteLine(""line: "" + a);
    }
}

class Test
{
    public static void Main()
    {
        new B().Log();
    }
}";

            var expected = @"
line: 17
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestBadCallerLineNumberMetadata()
        {
            var iLSource = @"
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  LogCallerLineNumber1([opt] int32 lineNumber) cil managed
  {
    .param [1] = int32(0xFFFFFFFF)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""line: ""
    IL_0006:  ldarg.0
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0011:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ret
  } // end of method Test::LogCallerLineNumber1

  .method public hidebysig static void  LogCallerLineNumber2(int32 lineNumber) cil managed
  {
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""line: ""
    IL_0006:  ldarg.0
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0011:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ret
  } // end of method Test::LogCallerLineNumber2

  .method public hidebysig static void  LogCallerLineNumber3([opt] string lineNumber) cil managed
  {
	.param [1] = """"
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""line: ""
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ret
  } // end of method Test::LogCallerLineNumber3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
";

            var source = @"
using System.Runtime.CompilerServices;
using System;

class Driver {
    public static void Main() {
        Test.LogCallerLineNumber1();
        Test.LogCallerLineNumber2(42);
        Test.LogCallerLineNumber3();
    }
}
";

            var expected = @"
line: 7
line: 42
line:
";

            MetadataReference libReference = CompileIL(iLSource);
            var compilation = CreateCompilationWithMscorlib45(source, new[] { libReference }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberDuplicateAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;

partial class D
{
    partial void Goo([CallerLineNumber] int x = 2);
}

partial class D
{
    partial void Goo([CallerLineNumber] int x)
    {
    }

    public static void Main()
    {
    }
}";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "CallerLineNumber").WithArguments("CallerLineNumber"),
                Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("x").WithLocation(11, 23));
        }

        [Fact, WorkItem(531044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531044")]
        public void TestUnconsumedCallerInfoAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;

partial class D
{
    partial void Goo(int line, string member, string path);
}

partial class D
{
    partial void Goo(
        [CallerLineNumber] int line,
        [CallerMemberName] string member,
        [CallerFilePath] string path) { }

    public static void Main()
    {
    }
}";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (12,10): warning CS4024: The CallerLineNumberAttribute applied to parameter 'line' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         [CallerLineNumber] int line,
                Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("line"),
                // (13,10): warning CS4026: The CallerMemberNameAttribute applied to parameter 'member' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         [CallerMemberName] string member,
                Diagnostic(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, "CallerMemberName").WithArguments("member"),
                // (14,10): warning CS4025: The CallerFilePathAttribute applied to parameter 'path' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         [CallerFilePath] string path) { }
                Diagnostic(ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation, "CallerFilePath").WithArguments("path"));
        }

        [Fact]
        public void TestCallerLineNumberViaDelegate()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Log([CallerLineNumber] int x = 1)
    {
        Console.WriteLine(""line: "" + x);
    }

    delegate void Del([CallerLineNumber] int x = 1);

    public static void Main()
    {
        Log();
        Del d = new Del(Log);
        d();
    }
}";

            var expected = @"
line: 16
line: 18
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestBadConversionCallerInfoMultipleAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Log1([CallerLineNumber, CallerFilePath, CallerMemberName] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log2([CallerLineNumber, CallerMemberName, CallerFilePath] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log3([CallerMemberName, CallerLineNumber, CallerFilePath] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log4([CallerMemberName, CallerFilePath, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log5([CallerFilePath, CallerMemberName, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log6([CallerFilePath, CallerLineNumber, CallerMemberName] int x = 1) { Console.WriteLine(""line: "" + x); }

    public static void Log7([CallerLineNumber, CallerFilePath, CallerMemberName] string x = """") { }
    public static void Log8([CallerLineNumber, CallerMemberName, CallerFilePath] string x = """") { }
    public static void Log9([CallerMemberName, CallerLineNumber, CallerFilePath] string x = """") { }
    public static void Log10([CallerMemberName, CallerFilePath, CallerLineNumber] string x = """") { }
    public static void Log11([CallerFilePath, CallerMemberName, CallerLineNumber] string x = """") { }
    public static void Log12([CallerFilePath, CallerLineNumber, CallerMemberName] string x = """") { }

    public static void Log13([CallerFilePath, CallerMemberName] string x = """") { }
    public static void Log14([CallerMemberName, CallerFilePath] string x = """") { }

    public static void Log15([CallerLineNumber, CallerFilePath] string x = """") { }
    public static void Log16([CallerFilePath, CallerLineNumber] string x = """") { }

    public static void Log17([CallerMemberName, CallerLineNumber] string x = """") { }
    public static void Log18([CallerLineNumber, CallerMemberName] string x = """") { }

    public static void Log19([CallerFilePath, CallerMemberName] int x = 1) { }
    public static void Log20([CallerMemberName, CallerFilePath] int x = 1) { }

    public static void Log21([CallerLineNumber, CallerFilePath] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log22([CallerFilePath, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }

    public static void Log23([CallerMemberName, CallerLineNumber] int x = 1) { Console.WriteLine(""line: "" + x); }
    public static void Log24([CallerLineNumber, CallerMemberName] int x = 1) { Console.WriteLine(""line: "" + x); }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll.WithWarningLevel(0)).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(7, 48).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(7, 64).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(8, 48).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(8, 66).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(9, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(9, 66).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(10, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(10, 48).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(11, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(11, 46).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(12, 30).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(12, 64).WithArguments("string", "int"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(14, 30).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(15, 30).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(16, 48).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(17, 65).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(18, 65).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(19, 47).WithArguments("int", "string"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(24, 31).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(25, 47).WithArguments("int", "string"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(27, 49).WithArguments("int", "string"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithLocation(28, 31).WithArguments("int", "string"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(30, 31).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(30, 47).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(31, 31).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(31, 49).WithArguments("string", "int"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(33, 49).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithLocation(34, 31).WithArguments("string", "int"),

                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(36, 31).WithArguments("string", "int"),
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithLocation(37, 49).WithArguments("string", "int"));
        }

        [Fact]
        public void TestCallerInfoMultipleAttributes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class Test
{
    public static void Log7([CallerFilePath, CallerMemberName] string x = """") { Console.WriteLine(x); }
    public static void Log8([CallerMemberName, CallerFilePath] string x = """") { Console.WriteLine(x); }

    public static void Main()
    {
        Log7();
        Log8();
    }
}";

            var expected = @"
C:\file.cs
C:\file.cs
";

            var compilation = CreateCompilationWithMscorlib45(
                new[] { Parse(source, @"C:\file.cs") },
                new[] { SystemRef },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerAttributeBash()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Reflection;
using System;

class MyCallerInfoAttribute : Attribute
{
    public MyCallerInfoAttribute(
        [CallerLineNumber] double lineNumber = -1)
    {
        Console.WriteLine(""line: "" + lineNumber);
    }
}

[MyCallerInfo]
class A
{
}

class Test
{
    public static void Main()
    {
        typeof(A).GetCustomAttribute(typeof(MyCallerInfoAttribute), false);
    }
}";

            var expected = @"
line: 15
";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerLineNumberUnconsumedBadType()
        {
            string source = @"
using System.Runtime.CompilerServices;

partial class D
{
    partial void Goo(string x = """");
}

partial class D
{
    partial void Goo([CallerLineNumber] string x)
    {
    }

    public static void Main()
    {
    }
}";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef });
            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("x").WithLocation(11, 23));
        }

        [WorkItem(689618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689618")]
        [Fact]
        public void TestCallerMemberNameUnconsumedBadType()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

partial class D
{
    partial void Goo(string x = """");
}

partial class D
{
    partial void Goo([CallerMemberName] string x)
    {
        Console.WriteLine(x);
    }

    public static void Main()
    {
        new D().Goo();
    }
}";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);

            compilation.VerifyEmitDiagnostics(
                // (12,23): warning CS4026: The CallerMemberNameAttribute applied to parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void Goo([CallerMemberName] string x)
                Diagnostic(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, "CallerMemberName").WithArguments("x").WithLocation(12, 23));

            CompileAndVerify(compilation, expectedOutput: "");
        }

        [WorkItem(689618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689618")]
        [Fact]
        public void TestCallerMemberNameUnconsumedBadType02()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

partial class D
{
    partial void Goo([CallerMemberName] string x = """");
}

partial class D
{
    partial void Goo(string x)
    {
        Console.WriteLine(x);
    }

    public static void Main()
    {
        new D().Goo();
    }
}";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Main");
        }

        [Fact]
        public void TestCallerMemberName_Lambda()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LambdaCaller()
    {
        new Action(() =>
        {
            Test.Log();
        })();
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        d.LambdaCaller();
    }
}";

            var expected = @"
name: LambdaCaller
";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_LocalFunction()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public void LocalFunctionCaller()
    {
        void Local()
        {
            void LocalNested() => Test.Log();
            LocalNested();
        }
        Local();
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        d.LocalFunctionCaller();
    }
}";

            var expected = @"
name: LocalFunctionCaller
";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Operator()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public static D operator ++(D d)
    {
        Test.Log();
        return d;
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        d++;
    }
}";

            var expected = @"
name: op_Increment
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Property()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    public bool IsTrue
    {
        get
        {
            Test.Log();
            return true;
        }
        set
        {
            Test.Log();
        }
    }
}

class Test
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var d = new D();
        bool truth = d.IsTrue;
        d.IsTrue = truth;
    }
}";

            var expected = @"
name: IsTrue
name: IsTrue
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_CustomAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class DummyAttribute : Attribute
{
    public DummyAttribute([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
    }
}

class A
{
    [Dummy]
    public void MyMethod() {
    }
}

class Test
{
    public static void Main()
    {
         typeof(A).GetMethod(""MyMethod"").GetCustomAttributes(typeof(DummyAttribute), false);
    }
}";

            var expected = @"
name: MyMethod
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Generic()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class G
{
    public static int Compare<T>(T a, T b) where T : IComparable {
        A.Log();
        return a.CompareTo(b);
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
         G.Compare<int>(1, 2);
    }
}";

            var expected = @"
name: Compare
";

            var compilation = CreateCompilationWithMscorlib45(
                source,
                new[] { SystemRef },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_ExplicitInterfaceInstantiation()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

interface I
{
    int Add(int a, int b);

    bool HasThing { get; }
}

class II : I
{
    int I.Add(int a, int b)
    {
        A.Log();
        return a + b;
    }

    bool I.HasThing
    {
        get
        {
            A.Log();
            return false;
        }
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        var ii = new II();
        ((I)ii).Add(1, 2);
        bool truth = ((I)ii).HasThing;
    }
}";

            var expected = @"
name: Add
name: HasThing
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Event()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class E
{
    public event Action ThingHappened
    {
        add { A.Log(); }
        remove { A.Log(); }
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        Action goo = new Action(() => { });
        var e = new E();
        e.ThingHappened += goo;
        e.ThingHappened -= goo;
    }
}";

            var expected = @"
name: ThingHappened
name: ThingHappened
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void TestCallerMemberName_ConstructorDestructor()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    static D()
    {
        A.Log();
    }

    public D()
    {
        A.Log();
    }

    ~D()
    {
        A.Log();
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        D d = new D();
        d = null;
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }
}";

            var expected = @"
name: .cctor
name: .ctor
name: Finalize
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerMemberName_Indexer()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;

class D
{
    [IndexerName(""TheIndexer"")]
    public int this[int index]
    {
        get
        {
            A.Log();
            return -1;
        }
        set
        {
            A.Log();
        }
    }
}

class DX
{
    public int this[int index]
    {
        get
        {
            A.Log();
            return -1;
        }
        set
        {
            A.Log();
        }
    }
}

class A
{
    public static int Log([CallerMemberName] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
        return 1;
    }

    public static void Main()
    {
        {
            var d = new D();
            int i = d[0];
            d[0] = i;
        }
        {
            var d = new DX();
            int i = d[0];
            d[0] = i;
        }
    }
}";

            var expected = @"
name: TheIndexer
name: TheIndexer
name: Item
name: Item
";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestCallerFilePath1()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System;

partial class A
{
    static int i;

    public static void Log([CallerFilePath] string filePath = """")
    {
        Console.WriteLine(""{0}: '{1}'"", ++i, filePath);
    }

    public static void Main()
    {
        Log();
        Main2();
        Main3();
        Main4();
    }
}";
            string source2 = @"partial class A { static void Main2() { Log(); } }";
            string source3 = @"partial class A { static void Main3() { Log(); } }";
            string source4 = @"partial class A { static void Main4() { Log(); } }";

            var compilation = CreateCompilationWithMscorlib45(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, path: @"C:\filename", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source2, path: @"a\b\..\c\d", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source3, path: @"*", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source4, path: @"       ", encoding: Encoding.UTF8),
                },
                new[] { SystemRef },
                TestOptions.ReleaseExe.WithSourceReferenceResolver(SourceFileResolver.Default));

            CompileAndVerify(compilation, expectedOutput: @"
1: 'C:\filename'
2: 'a\b\..\c\d'
3: '*'
4: '       '
");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionHasNewLineDependency)]
        public void TestCallerFilePath2()
        {
            string source1 = @"
using System.Runtime.CompilerServices;
using System;

partial class A
{
    static int i;

    public static void Log([CallerFilePath] string filePath = """")
    {
        Console.WriteLine(""{0}: '{1}'"", ++i, filePath);
    }

    public static void Main()
    {
        Log();
        Main2();
        Main3();
        Main4();
        Main5();
    }
}";
            string source2 = @"partial class A { static void Main2() { Log(); } }";
            string source3 = @"
#line hidden
partial class A { static void Main3() { Log(); } }
";

            string source4 = @"
#line 30 ""abc""
partial class A { static void Main4() { Log(); } }
";
            string source5 = @"
#line 30 ""     ""
partial class A { static void Main5() { Log(); } }
";

            var compilation = CreateCompilationWithMscorlib45(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, path: @"C:\filename", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source2, path: @"a\b\..\c\d.cs", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source3, path: @"*", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source4, path: @"C:\x.cs", encoding: Encoding.UTF8),
                    SyntaxFactory.ParseSyntaxTree(source5, path: @"C:\x.cs", encoding: Encoding.UTF8),
                },
                new[] { SystemRef },
                TestOptions.ReleaseExe.WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory: @"C:\A\B")));

            // On CoreClr the '*' is a legal path character
            // https://github.com/dotnet/docs/issues/4483
            var expectedStarPath = ExecutionConditionUtil.IsCoreClr
                ? @"C:\A\B\*"
                : "*";
            CompileAndVerify(compilation, expectedOutput: $@"
1: 'C:\filename'
2: 'C:\A\B\a\c\d.cs'
3: '{expectedStarPath}'
4: 'C:\abc'
5: '     '
");
        }

        [Fact]
        public void TestAssemblyAttribute()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: MyNamespace.MyCallerMemberName]

namespace MyNamespace
{

    class MyCallerMemberNameAttribute : Attribute
    {
        public MyCallerMemberNameAttribute(
            [CallerMemberName] string memberName = """")
        {
            Console.WriteLine(""member: "" + memberName);
        }
    }

    class B
    {
        [MyCallerMemberName]
        public static void MyMethod() { }
    }

    class A
    {
        public static void Main()
        {
            B b = new B();
            Type Type1;
            Type1 = b.GetType();

            typeof(B).GetMethod(""MyMethod"").GetCustomAttribute(typeof(MyCallerMemberNameAttribute));

            Assembly.GetAssembly(Type1).GetCustomAttribute(typeof(MyCallerMemberNameAttribute));
        }
    }
}";

            var expected = @"
member: MyMethod
member: 
";

            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }


        [Fact]
        public void TestCallerMemberNameConversion()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System;
using System.Reflection;

namespace MyNamespace
{

    class MyCallerMemberNameAttribute : Attribute
    {
        public MyCallerMemberNameAttribute(
            [CallerMemberName] object memberName = null)
        {
            Console.WriteLine(""member: "" + memberName);
        }
    }

    class B
    {
        [MyCallerMemberName]
        public static void MyMethod() { }
    }

    class A
    {
        public static void Main()
        {
            typeof(B).GetMethod(""MyMethod"").GetCustomAttribute(typeof(MyCallerMemberNameAttribute));
        }
    }
}";

            var expected = @"
member: MyMethod
";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact]
        public void TestRecursiveAttribute()
        {
            string source = @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Goo: Attribute
{
    public Goo([Goo] int y = 0) {}
}

class Test
{
    public static void Main() { }
}
";

            var expected = @"";

            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { SystemRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }


        [Fact]
        public void TestRecursiveAttributeMetadata()
        {
            var iLSource = @"
.class public auto ansi beforefieldinit Goo
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor([opt] int32 y) cil managed
  {
    .param [1] = int32(0x00000000)
    .custom instance void Goo::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ret
  } // end of method Goo::.ctor

} // end of class Goo
";

            var source = @"
using System.Runtime.CompilerServices;
using System;

class Driver {

    [Goo]
    public static void AttrTarget() { }

    public static void Main() { }
}
";

            var expected = @"";

            MetadataReference libReference = CompileIL(iLSource);
            var compilation = CreateCompilationWithMscorlib45(source, new[] { libReference }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }


        [Fact]
        public void TestMemberNameLookup()
        {
            var source = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System;

class My : Attribute
{
    public My([CallerMemberName] string a = """")
    {
        Console.WriteLine(a);
    }
}

class Driver
{
    public static void Bar([My] int x)
    {
    }

    public static void Main()
    {
        typeof(Driver).GetMethod(""Bar"").GetParameters()[0].GetCustomAttribute(typeof(My));
    }
}
";

            var expected = @"Bar";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }


        [Fact]
        public void TestDuplicateCallerInfoMetadata()
        {
            var iLSource = @"
.class public auto ansi beforefieldinit Goo
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Log([opt] int32 callerName) cil managed
  {
    .param [1] = int32(0x00000000)
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       29 (0x1d)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""name: ""
    IL_0006:  ldarg.0
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0011:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ldc.i4.1
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  } // end of method Goo::Log

  .method public hidebysig static int32  Log2([opt] string callerName) cil managed
  {
    .param [1] = """"
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""name: ""
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldc.i4.1
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Goo::Log2

  .method public hidebysig static int32  Log3([opt] string callerName) cil managed
  {
    .param [1] = """"
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerMemberNameAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CallerFilePathAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""name: ""
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldc.i4.1
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Goo::Log3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Goo::.ctor

} // end of class Goo
";

            var source = @"
using System.Runtime.CompilerServices;
using System;

class Driver {
    public static void Main() {
        Goo.Log();
        Goo.Log2();
        Goo.Log3();
    }
}
";

            var expected = @"
name: 7
name: 
name: C:\file.cs
";

            MetadataReference libReference = CompileIL(iLSource);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { Parse(source, @"C:\file.cs") },
                new[] { libReference },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(546977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546977")]
        public void Bug_17433()
        {
            var source = @"using System.Reflection;
using System.Runtime.CompilerServices;
using System;

class My : Attribute
{
    public My([CallerLineNumber] int x = -1)
    {
        Console.WriteLine(x);
    }
}

[My]
class Driver
{
    public static void Main()
    {
        typeof(Driver).GetCustomAttribute(typeof(My));
    }
}
";

            var expected = @"13";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531036")]
        public void Repro_17443()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class CallerInfoAttributedAttribute : Attribute
{
    public string FilePath { get; private set; }
    public int LineNumber { get; private set; }
    public string MemberName { get; private set; }
    public CallerInfoAttributedAttribute(
        [CallerFilePath] string filePath = """",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = """")
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        MemberName = memberName;
    }
}
class Program
{
    [CallerInfoAttributed]
    public int Property1 { get; set; }
    static void Main(string[] args)
    {
        System.Reflection.PropertyInfo pi = typeof(Program).GetProperty(""Property1"");
        if (pi != null)
        {
            var a = Attribute.GetCustomAttribute(pi, typeof(CallerInfoAttributedAttribute)) as
CallerInfoAttributedAttribute;
            if (a != null)
            {
                Console.WriteLine(""CallerInfoAttributed: ({0}, {1}, {2})"", a.FilePath ?? ""<null>"",
a.LineNumber, a.MemberName ?? ""<null>"");
            }
        }
    }
}
";

            var expected = @"
CallerInfoAttributed: (, 22, Property1)
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531036")]
        public void CallerMemberNameAttributedAttributeOnNonMethodMembers()
        {
            var source = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

class NameAttr : Attribute
{
    public NameAttr([CallerMemberName] string name = ""<none>"")
    {
        Console.WriteLine(name);
    }
}

[NameAttr]
class Driver
{
    [NameAttr]
    public int myField;

    [NameAttr]
    public int MyProperty { get; set; }

    [NameAttr]
    public event Action MyEvent
    {
        add { }
        remove { }
    }

    [NameAttr]
    public int this[int i]
    {
        get { return -1; }
        set { }
    }

    [NameAttr]
    public int MyMethod() {
        return -1;
    }

    public static void Main()
    {
        typeof(Driver).GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetField(""myField"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetProperty(""MyProperty"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetEvent(""MyEvent"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetProperty(""Item"").GetCustomAttribute(typeof(NameAttr));
        typeof(Driver).GetMethod(""MyMethod"").GetCustomAttribute(typeof(NameAttr));
    }
}
";

            var expected = @"
<none>
<none>
MyProperty
MyEvent
Item
MyMethod
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531040")]
        public void Repro_17449()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2ObjectAttribute : Attribute
{
    public LineNumber2ObjectAttribute([CallerLineNumber] object lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2Object]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(LineNumber2ObjectAttribute), false);
    }
}
";

            var expected = @"
13
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }


        [Fact, WorkItem(531040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531040")]
        public void TestBadAttributeParameterTypeWithCallerLineNumber()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2NullableIntAttribute : Attribute
{
    public LineNumber2NullableIntAttribute([CallerLineNumber] int? lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2ValueTypeAttribute : Attribute
{
    public LineNumber2ValueTypeAttribute([CallerLineNumber] ValueType lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2NullableInt, LineNumber2ValueType]
class Program
{
    static void Main()
    {
        typeof(Program).GetCustomAttributes(typeof(LineNumber2NullableIntAttribute), false);
        typeof(Program).GetCustomAttributes(typeof(LineNumber2ValueTypeAttribute), false);
    }
}
";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (21,2): error CS0181: Attribute constructor parameter 'lineNumber' has type 'int?', which is not a valid attribute parameter type
                // [LineNumber2NullableInt, LineNumber2ValueType]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "LineNumber2NullableInt").WithArguments("lineNumber", "int?"),
                // (21,26): error CS0181: Attribute constructor parameter 'lineNumber' has type 'System.ValueType', which is not a valid attribute parameter type
                // [LineNumber2NullableInt, LineNumber2ValueType]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "LineNumber2ValueType").WithArguments("lineNumber", "System.ValueType"));
        }


        [Fact, WorkItem(531043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531043")]
        public void Repro_17457()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
public class LineNumber2LongAttribute : Attribute
{
    public LineNumber2LongAttribute([CallerLineNumber] long lineNumber = 0)
    {
        Console.WriteLine(lineNumber);
    }
}
public class LineNumber2FloatAttribute : Attribute
{
    public LineNumber2FloatAttribute([CallerLineNumber] float lineNumber = 0)
    {
        Console.WriteLine(lineNumber);
    }
}
[LineNumber2Long]
[LineNumber2Float]
class Test
{
    public static void Main()
    {
        typeof(Test).GetCustomAttributes(false);
    }
}
";

            var expected = @"
18
19
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }


        [Fact, WorkItem(531043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531043")]
        public void InvalidDecimalInCustomAttributeParameterWithCallerLineNumber()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2DecimalAttribute : Attribute
{
    public LineNumber2DecimalAttribute([CallerLineNumber] decimal lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2DecimalAttribute]
class Test
{
    public static void Main()
    {
        typeof(Test).GetCustomAttributes(false);
    }
}
";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (13,2): error CS0181: Attribute constructor parameter 'lineNumber' has type 'decimal', which is not a valid attribute parameter type
                // [LineNumber2DecimalAttribute]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "LineNumber2DecimalAttribute").WithArguments("lineNumber", "decimal"));
        }

        [Fact, WorkItem(531043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531043")]
        public void AllLegalConversionForCallerLineNumber()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class LineNumber2ObjectAttribute : Attribute
{
    public LineNumber2ObjectAttribute([CallerLineNumber] object lineNumber = null)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2UintAttribute : Attribute
{
    public LineNumber2UintAttribute([CallerLineNumber] uint lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2UlongAttribute : Attribute
{
    public LineNumber2UlongAttribute([CallerLineNumber] ulong lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2IntAttribute : Attribute
{
    public LineNumber2IntAttribute([CallerLineNumber] int lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2LongAttribute : Attribute
{
    public LineNumber2LongAttribute([CallerLineNumber] long lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2DoubleAttribute : Attribute
{
    public LineNumber2DoubleAttribute([CallerLineNumber] double lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

public class LineNumber2FloatAttribute : Attribute
{
    public LineNumber2FloatAttribute([CallerLineNumber] float lineNumber = 42)
    {
        Console.WriteLine(lineNumber);
    }
}

[LineNumber2UintAttribute, LineNumber2UlongAttribute, LineNumber2IntAttribute, LineNumber2LongAttribute, LineNumber2DoubleAttribute, LineNumber2FloatAttribute, LineNumber2ObjectAttribute]
class Test
{
    public static void Main()
    {
 
        typeof(Test).GetCustomAttributes(false);
    }
}";

            var expected = @"
61
61
61
61
61
61
61
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531046")]
        public void TestUserDefinedImplicitConversion()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Test
{
    public string Prop { get; set; }

    public static implicit operator Test(int i)
    {
        return new Test() { Prop = i.ToString() };
    }

    public static implicit operator Test(string i)
    {
        return new Test() { Prop = i };
    }

    public bool M1(string expected, [CallerLineNumber] Test line = null)
    {
        Console.WriteLine(""expected: {0}; actual: {1}"", expected, line);
        return expected == line.Prop;
    }

    public bool M2(string expected, [CallerMemberName] Test line = null)
    {
        Console.WriteLine(""expected: {0}; actual: {1}"", expected, line);
        return expected == line.Prop;
    }
}
";

            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (19,38): error CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type 'int' to type 'Test'
                //     public bool M1(string expected, [CallerLineNumber] Test line = null)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithArguments("int", "Test"),
                // (25,38): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'Test'
                //     public bool M2(string expected, [CallerMemberName] Test line = null)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "Test"));
        }

        [Fact, WorkItem(546980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546980")]
        public void TestBaseCtorInvocation()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

public class B
{
    public B(
        [CallerMemberName] string memberName = ""<default>"",
        [CallerLineNumber] int lineNumber = -1,
        [CallerFilePath] string filePath = ""<default>"")
    {
        Console.WriteLine(""name : "" + memberName);
        Console.WriteLine(""line : "" + lineNumber);
        Console.WriteLine(""path : "" + filePath);
    }

    public B(bool b) : this
        (
        ) { }
}

public class D : B
{
    public D()  : base
        (
        ) { }
}

public class E : B
{
    public E()
    {
    }
}

public class I
{
    public int GetInt(
        [CallerMemberName] string memberName = ""<default>"",
        [CallerLineNumber] int lineNumber = -1,
        [CallerFilePath] string filePath = ""<default>"")
    {
        Console.WriteLine(""query name : "" + memberName);
        Console.WriteLine(""query line : "" + lineNumber);
        Console.WriteLine(""query path : "" + filePath);
        return lineNumber;
    }
}

class Program
{
    static void Main(string[] args)
    {
        new B(false);
        new D();
        new 
        B
        ();
        new E();

        var query =
            from x in new I[] { new I(), new I() }
            where x.GetInt
            (
            ) >= 0
            select x;

        foreach (var x in query) { }
    }
}
";

            var expected = @"
name : .ctor
line : 20
path : C:\filename
name : .ctor
line : 27
path : C:\filename
name : Main
line : 58
path : C:\filename
name : <default>
line : -1
path : <default>
query name : Main
query line : 66
query path : C:\filename
query name : Main
query line : 66
query path : C:\filename
";

            var compilation = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(source, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531034")]
        public void WarnOnCallerInfoCollision()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Test
{
    static void M1([CallerMemberName,CallerFilePath] string s = null) { Console.WriteLine(s); }
    static void M2([CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
    static void M3([CallerLineNumber,CallerFilePath,CallerMemberName] object o = null) { Console.WriteLine(o); }
    static void M4([CallerLineNumber,CallerMemberName,CallerFilePath] object o = null) { Console.WriteLine(o); }
    static void M5([CallerFilePath,CallerLineNumber,CallerMemberName] object o = null) { Console.WriteLine(o); }
    static void M6([CallerMemberName,CallerLineNumber,CallerFilePath] object o = null) { Console.WriteLine(o); }
    static void M7([CallerFilePath,CallerMemberName,CallerLineNumber] object o = null) { Console.WriteLine(o); }
    static void M8([CallerMemberName,CallerFilePath,CallerLineNumber] object o = null) { Console.WriteLine(o); }

    static void Main(string[] args)
    {
        M1();
        M2();
        M3();
        M4();
        M5();
        M6();
        M7();
        M8();
    }
}
";

            var expected = @"
C:\filename
C:\filename
20
21
22
23
24
25
";

            var compilation = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular7, path: @"C:\filename", encoding: Encoding.UTF8) },
                options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // C:\filename(7,21): warning CS7072: The CallerMemberNameAttribute applied to parameter 's' will have no effect. It is overridden by the CallerFilePathAttribute.
                //     static void M1([CallerMemberName,CallerFilePath] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, "CallerMemberName").WithArguments("s"),
                // C:\filename(8,36): warning CS7072: The CallerMemberNameAttribute applied to parameter 's' will have no effect. It is overridden by the CallerFilePathAttribute.
                //     static void M2([CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, "CallerMemberName").WithArguments("s"),
                // C:\filename(9,38): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M3([CallerLineNumber,CallerFilePath,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(9,53): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M3([CallerLineNumber,CallerFilePath,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(10,38): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M4([CallerLineNumber,CallerMemberName,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(10,55): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M4([CallerLineNumber,CallerMemberName,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(11,21): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M5([CallerFilePath,CallerLineNumber,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(11,53): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M5([CallerFilePath,CallerLineNumber,CallerMemberName] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(12,21): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M6([CallerMemberName,CallerLineNumber,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(12,55): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M6([CallerMemberName,CallerLineNumber,CallerFilePath] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(13,21): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M7([CallerFilePath,CallerMemberName,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"),
                // C:\filename(13,36): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M7([CallerFilePath,CallerMemberName,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(14,21): warning CS7073: The CallerMemberNameAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M8([CallerMemberName,CallerFilePath,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("o"),
                // C:\filename(14,38): warning CS7074: The CallerFilePathAttribute applied to parameter 'o' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M8([CallerMemberName,CallerFilePath,CallerLineNumber] object o = null) { Console.WriteLine(o); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("o"));

            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [Fact, WorkItem(531034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531034")]
        public void WarnOnCallerInfoCollisionWithBadType()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Test
{
    static void M1([CallerLineNumber,CallerFilePath,CallerMemberName] int i = 0) { Console.WriteLine(); }
    static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }

    static void Main(string[] args)
    {
        M1();
        M2();
    }
}
";

            var compilation = CreateCompilationWithMscorlib45(new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular7, path: @"C:\filename") }).VerifyDiagnostics(
                // C:\filename(7,38): error CS4018: CallerFilePathAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     static void M1([CallerLineNumber,CallerFilePath,CallerMemberName] int i = 0) { Console.WriteLine(); }
                Diagnostic(ErrorCode.ERR_NoConversionForCallerFilePathParam, "CallerFilePath").WithArguments("string", "int"),
                // C:\filename(7,53): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //     static void M1([CallerLineNumber,CallerFilePath,CallerMemberName] int i = 0) { Console.WriteLine(); }
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "int"),
                // C:\filename(8,21): error CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type 'int' to type 'string'
                //     static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.ERR_NoConversionForCallerLineNumberParam, "CallerLineNumber").WithArguments("int", "string"),
                // C:\filename(8,38): warning CS7074: The CallerFilePathAttribute applied to parameter 's' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, "CallerFilePath").WithArguments("s"),
                // C:\filename(8,53): warning CS7073: The CallerMemberNameAttribute applied to parameter 's' will have no effect. It is overridden by the CallerLineNumberAttribute.
                //     static void M2([CallerLineNumber,CallerFilePath,CallerMemberName] string s = null) { Console.WriteLine(s); }
                Diagnostic(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, "CallerMemberName").WithArguments("s"));
        }

        [WorkItem(604367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604367")]
        [Fact]
        public void TestCallerInfoInQuery()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;

class Test
{
    static int result = 0;
    static void Main()
    {
        var q = from x in new Test()
                where x != null
                select x.ToString();
        Console.WriteLine(result == 0 ? ""PASS"" : ""FAIL"");
    }
    public Test Select(
        Func<object, object> selector,
        [CallerFilePath]string file = null,
        [CallerLineNumber]int line = -1,
        [CallerMemberName]string member = null
    )
    {
        if (file != FILEPATH)
        {
            result++;
            Console.WriteLine(""File: Exp={0} Act={1} "", FILEPATH, file);
        }
        if (line != 11)
        {
            result++;
            Console.WriteLine(""Line: Exp=11 Act={0} "", line);
        }
        if (member != ""Main"")
        {
            result++;
            Console.WriteLine(""Member: Exp='Main' Act={0}"", member);
        }
        return new Test();
    }
    public Test Where(
        Func<object, bool> predicate,
        [CallerFilePath]string file = null,
        [CallerLineNumber]int line = -1,
        [CallerMemberName]string member = null
    )
    {
        if (file != FILEPATH)
        {
            result++;
            Console.WriteLine(""File: Exp={0} Act={1} "", FILEPATH, file);
        }
        if (line != 10)
        {
            result++;
            Console.WriteLine(""Line: Exp=10 Act={0} "", line);
        }
        if (member != ""Main"")
        {
            result++;
            Console.WriteLine(""Member: Exp='Main' Act={0}"", member);
        }
        return new Test();
    }
    public static readonly string FILEPATH = GetFilePath();
    public static string GetFilePath([CallerFilePath]string filePath = null) { return filePath; }
}";

            string expected = @"PASS";
            var compilation = CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(949118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949118")]
        [WorkItem(152, "CodePlex")]
        [Fact]
        public void Bug949118_1()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;
using System.Globalization;
class Program
{
  static void Main()
  {
   var x = Goo.F1;
   var y = new Goo().F2;
  }
}
public class Goo
{
  static object Test([CallerMemberName] string bar = null)
  {
    Console.WriteLine(bar);
    return null;
  }
  
  public static readonly object F1 = Test();
  public readonly object F2 = Test();
}
";

            string expected = @"F1
F2";
            var compilation = CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(949118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949118")]
        [WorkItem(152, "CodePlex")]
        [Fact]
        public void Bug949118_2()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;
using System.Globalization;
class Program
{
  static void Main()
  {
   var x = Goo.F1;
   var y = new Goo().F2;
  }
}
public class Goo
{
  static object Test([CallerMemberName] string bar = null)
  {
    Console.WriteLine(bar);
    return null;
  }
  
  public static object F1 {get;} = Test();
  public object F2 {get;} = Test();
}
";

            string expected = @"F1
F2";
            var compilation = CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(949118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949118")]
        [WorkItem(152, "CodePlex")]
        [Fact]
        public void Bug949118_3()
        {
            string source =
@"using System;
using System.Runtime.CompilerServices;
using System.Globalization;
class Program
{
  static void Main()
  {
   var y = ((I1)new Goo()).F2;
  }
}

interface I1
{
  object F2 {get;}
}

public class Goo : I1
{
  static object Test([CallerMemberName] string bar = null)
  {
    Console.WriteLine(bar);
    return null;
  }
  
  object I1.F2 {get;} = Test();
}
";

            string expected = @"F2";
            var compilation = CreateCompilationWithMscorlib45(source, new MetadataReference[] { SystemRef }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        /// <summary>
        /// DELIBERATE SPEC VIOLATION: The C# spec currently requires to provide caller information only in explicit invocations and query expressions.
        /// We also provide caller information to an invocation of an <c>Add</c> method generated for an element-initializer in a collection-initializer
        /// to match the native compiler behavior and user requests. 
        /// </summary>
        [WorkItem(991476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991476")]
        [WorkItem(171, "CodePlex")]
        [Fact]
        public void Bug991476_1()
        {
            const string source =
@"using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MyCollectionWithInitializer : IEnumerable<DBNull>
{
    public string LastCallerFilePath { get; set; }

    public void Add<T>(T something, [CallerFilePath] string callerFilePath = """") where T : struct
    {
        LastCallerFilePath = callerFilePath;
        Console.WriteLine(""Caller file path: "" + (!string.IsNullOrEmpty(callerFilePath) ? callerFilePath : ""(nothing)""));
    }

    public IEnumerator<DBNull> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotSupportedException();
    }
}

class Program
{
    public static void Main()
    {
        var coll1 = new MyCollectionWithInitializer();
        coll1.Add(123);
        Console.WriteLine(coll1.LastCallerFilePath);

        var coll2 = new MyCollectionWithInitializer { 345 };
        Console.WriteLine(coll2.LastCallerFilePath);
    }
}";

            const string expected = @"Caller file path: C:\filename
C:\filename
Caller file path: C:\filename
C:\filename";

            var compilation = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(source, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(991476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991476")]
        [WorkItem(171, "CodePlex")]
        [Fact]
        public void Bug991476_2()
        {
            const string source =
@"using System;
using System.Collections;
using System.Runtime.CompilerServices;

class C : Stack
{
    static void Main()
    {
        new C
        {
            1, // line 11
            2  // line 12
        };

        new C
        {
            {  // line 17
                1,
                true
            },
            {  // line 21
                ""Hi""
            }
        };


    }

    public void Add(int x, [CallerLineNumber] int n = -1) { Console.WriteLine(n); }
    public void Add(int x, bool y, [CallerLineNumber] int n = -1) { Console.WriteLine(n); }
}

static class E
{
    public static void Add(this C c, string s, [CallerMemberName] string m = ""Default"", [CallerLineNumber] int n = -1)
    {
        Console.WriteLine(m);
        Console.WriteLine(n);
    }
}";

            const string expected = @"11
12
17
Main
21";

            var compilation = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(source, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(1006447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006447")]
        [Fact]
        public void Bug1006447_1()
        {
            const string vbSource =
@"Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
 
<ComImport>
<Guid(""1F9C3731-6AA1-498A-AFA0-359828FCF0CE"")>
Public Interface I
    Property X(Optional i as Integer = 0, <CallerFilePath> Optional s As String = Nothing) As StringBuilder
End Interface

Public Class A
    Implements I

    Public Property X(Optional i as Integer = 0, Optional s As String = Nothing) As StringBuilder Implements I.X
        Get
            Console.WriteLine(""Get X(""""{0}"""")"", s)
            Return New StringBuilder
        End Get
        Set(value As StringBuilder)
            Console.WriteLine(""Set X(""""{0}"""")"", s)
        End Set
    End Property
End Class";

            var vbReference = BasicCompilationUtils.CompileToMetadata(vbSource, references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef });

            const string csSource =
@"using System;

class C
{
    I P = new A();
 
    static void Main()
    {
        new C().P.X = null;
        new C().P.X[1] = null;
        new C { P = { X = null } };
        new C { P = { X = { Length = 0 } } };
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(csSource, path: @"C:\filename", encoding: Encoding.UTF8) },
                new[] { SystemCoreRef, vbReference },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput:
@"Set X(""C:\filename"")
Set X(""C:\filename"")
Set X(""C:\filename"")
Get X(""C:\filename"")
");
        }

        [WorkItem(1006447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006447")]
        [Fact]
        public void Bug1006447_2()
        {
            const string source =
@"using System;
using System.Runtime.CompilerServices;

class C
{
    static void Main()
    {
        new C()[0] = 0;
    }

    int this[int x, [CallerMemberName] string s = null]
    {
        set
        {
            Console.WriteLine(s);
        }
    }
}";

            const string expected = "Main";

            var compilation = CreateCompilationWithMscorlib45(
                source,
                new[] { SystemCoreRef },
                TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expected);
        }

        [WorkItem(1006447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006447")]
        [Fact]
        public void Bug1006447_3()
        {
            const string vbSource =
@"Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<ComImport>
<Guid(""1F9C3731-6AA1-498A-AFA0-359828FCF0CE"")>
Public Interface I
    ReadOnly Property [Select](<CallerMemberName> Optional s As String = Nothing) As Func(Of Func(Of Integer, Integer), String)
End Interface

Public Class A
    Implements I

    Public ReadOnly Property [Select](<CallerMemberName> Optional s As String = Nothing) As Func(Of Func(Of Integer, Integer), String) Implements I.Select
         Get
            Console.WriteLine(""Get Select(""""{0}"""")"", s)
            Return Function() ""ABC""
        End Get
    End Property
End Class";

            var vbReference = BasicCompilationUtils.CompileToMetadata(vbSource, references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef });

            const string csSource =
@"using System;

class Program
{
    static void Main()
    {
        I x = new A();
        Console.WriteLine(from y in x select y);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(
                csSource,
                new[] { SystemCoreRef, vbReference },
                TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput:
@"Get Select(""Main"")
ABC
");
        }
    }
}
