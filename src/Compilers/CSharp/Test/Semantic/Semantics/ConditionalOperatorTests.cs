// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test binding of the conditional (aka ternary) operator.
    /// </summary>
    public class ConditionalOperatorTests : CSharpTestBase
    {
        /// <summary>
        /// Both branches have the same type, so no conversion is necessary.
        /// </summary>
        [Fact]
        public void TestSameType()
        {
            TestConditional("true ? 1 : 2", expectedType: "System.Int32");
            TestConditional("false ? 'a' : 'b'", expectedType: "System.Char");
            TestConditional("true ? 1.5 : GetDouble()", expectedType: "System.Double");
            TestConditional("false ? GetObject() : GetObject()", expectedType: "System.Object");
            TestConditional("true ? GetUserGeneric<T>() : GetUserGeneric<T>()", expectedType: "D<T>");
            TestConditional("false ? GetTypeParameter<T>() : GetTypeParameter<T>()", expectedType: "T");
        }

        /// <summary>
        /// Both branches have types and exactly one expression is convertible to the type of the other.
        /// </summary>
        [Fact]
        public void TestOneConversion()
        {
            TestConditional("true ? GetShort() : GetInt()", expectedType: "System.Int32");
            TestConditional("false ? \"string\" : GetObject()", expectedType: "System.Object");
            TestConditional("true ? GetVariantInterface<string, int>() : GetVariantInterface<object, int>()", expectedType: "I<System.String, System.Int32>");
            TestConditional("false ? GetVariantInterface<int, object>() : GetVariantInterface<int, string>()", expectedType: "I<System.Int32, System.Object>");
        }

        /// <summary>
        /// Both branches have types and both expression are convertible to the type of the other.
        /// The wider type is preferred.
        /// </summary>
        /// <remarks>
        /// Cases where both conversions are possible and neither is preferred as the
        /// wider of the two are possible only in the presence of user-defined implicit
        /// conversions.  Such cases are tested separately.  
        /// See SemanticErrorTests.CS0172ERR_AmbigQM.
        /// </remarks>
        [Fact]
        public void TestAmbiguousPreferWider()
        {
            TestConditional("true ? 1 : (short)2", expectedType: "System.Int32");
            TestConditional("false ? (float)2 : 1", expectedType: "System.Single");
            TestConditional("true ? 1.5d : (double)2", expectedType: "System.Double");
        }

        /// <summary>
        /// Both branches have types but neither expression is convertible to the type
        /// of the other.
        /// </summary>
        [Fact]
        public void TestNoConversion()
        {
            TestConditional("true ? T : U", null,
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type"),
                Diagnostic(ErrorCode.ERR_BadSKunknown, "U").WithArguments("U", "type"),
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? T : U").WithArguments("T", "U"));
            TestConditional("false ? T : 1", null,
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type"),
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? T : 1").WithArguments("T", "int"));
            TestConditional("true ? GetUserGeneric<char>() : GetUserNonGeneric()", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetUserGeneric<char>() : GetUserNonGeneric()").WithArguments("D<char>", "C"));
        }

        /// <summary>
        /// Exactly one branch has a type and the other expression is convertible to that type.
        /// </summary>
        [Fact]
        public void TestOneUntypedSuccess()
        {
            TestConditional("true ? GetObject() : null", expectedType: "System.Object"); //null literal
            TestConditional("false ? GetString : (System.Func<string>)null", expectedType: "System.Func<System.String>"); //method group
            TestConditional("true ? (System.Func<int, int>)null : x => x", expectedType: "System.Func<System.Int32, System.Int32>"); //lambda
        }

        /// <summary>
        /// Exactly one branch has a type but the other expression is not convertible to that type.
        /// </summary>
        [Fact]
        public void TestOneUntypedFailure()
        {
            TestConditional("true ? GetInt() : null", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetInt() : null").WithArguments("int", "<null>"));
            TestConditional("false ? GetString : (System.Func<int>)null", null,
                Diagnostic(ErrorCode.ERR_BadRetType, "GetString").WithArguments("C.GetString()", "string"));
            TestConditional("true ? (System.Func<int, short>)null : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? (System.Func<int, short>)null : x => x").WithArguments("System.Func<int, short>", "lambda expression"));
        }

        [Fact]
        public void TestBothUntyped()
        {
            TestConditional("true ? null : null", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? null : null").WithArguments("<null>", "<null>"));
            TestConditional("false ? null : GetInt", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? null : GetInt").WithArguments("<null>", "method group"));
            TestConditional("true ? null : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? null : x => x").WithArguments("<null>", "lambda expression"));

            TestConditional("false ? GetInt : GetInt", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? GetInt : GetInt").WithArguments("method group", "method group"));
            TestConditional("true ? GetInt : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetInt : x => x").WithArguments("method group", "lambda expression"));

            TestConditional("false ? x => x : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? x => x : x => x").WithArguments("lambda expression", "lambda expression"));
        }

        [Fact]
        public void TestFunCall()
        {
            TestConditional("true ? GetVoid() : GetInt()", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetVoid() : GetInt()").WithArguments("void", "int"));
            TestConditional("GetVoid() ? 1 : 2", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetVoid()").WithArguments("void", "bool"));
            TestConditional("GetInt() ? 1 : 2", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetInt()").WithArguments("int", "bool"));
            TestConditional("GetBool() ? 1 : 2", "System.Int32");
        }

        [Fact]
        public void TestEmptyExpression()
        {
            TestConditional("true ?  : GetInt()", null,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":"));
            TestConditional("true ? GetInt() :  ", null,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")"));
        }

        [Fact]
        public void TestEnum()
        {
            TestConditional("true? 0 : color.Blue", "color");
            TestConditional("true? 5 : color.Blue", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true? 5 : color.Blue").WithArguments("int", "color"));
            TestConditional("true? null : color.Blue", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true? null : color.Blue").WithArguments("<null>", "color"));
        }

        [Fact]
        public void TestAs()
        {
            TestConditional(@"(1 < 2) ? ""MyString"" as string : "" """, "System.String");
            TestConditional(@"(1 > 2) ? "" "" : ""MyString"" as string", "System.String");
        }

        [Fact]
        public void TestGeneric()
        {
            TestConditional(@"GetUserNonGeneric()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetUserNonGeneric()").WithArguments("C", "bool"));
            TestConditional(@"GetUserGeneric<T>()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetUserGeneric<T>()").WithArguments("D<T>", "bool"));
            TestConditional(@"GetTypeParameter<T>()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetTypeParameter<T>()").WithArguments("T", "bool"));
            TestConditional(@"GetVariantInterface<T, U>()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetVariantInterface<T, U>()").WithArguments("I<T, U>", "bool"));
        }

        [Fact]
        public void TestInvalidCondition()
        {
            // CONSIDER: dev10 reports ERR_ConstOutOfRange
            TestConditional("1 ? 2 : 3", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "bool"));

            TestConditional("foo ? 'a' : 'b'", null,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "foo").WithArguments("foo"));

            TestConditional("new Foo() ? GetObject() : null", null,
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Foo").WithArguments("Foo"));

            // CONSIDER: dev10 reports ERR_ConstOutOfRange
            TestConditional("1 ? null : null", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "bool"),
                Diagnostic(ErrorCode.ERR_InvalidQM, "1 ? null : null").WithArguments("<null>", "<null>"));
        }

        [WorkItem(545408, "DevDiv")]
        [Fact]
        public void TestDelegateCovarianceConversions()
        {
            var source = @"
using System;
using System.Collections.Generic;

delegate void D<out T>();

class Base { }
class Derived : Base { }

class Program
{
    static void Main()
    {
        bool testFlag = true;

        D<Base> baseDelegate = () => Console.WriteLine(""B"");
        D<Derived> derivedDelegate = () => Console.WriteLine(""D"");

        D<Base> fcn;
        
        fcn = testFlag ? baseDelegate : derivedDelegate;
        fcn();

        fcn = testFlag ? derivedDelegate : baseDelegate;
        fcn();

        fcn = baseDelegate ?? derivedDelegate;
        fcn();

        fcn = derivedDelegate ?? baseDelegate;
        fcn();

        IEnumerable<Base> baseSequence = null;
        List<Derived> derivedList = null;
        IEnumerable<Base> result = testFlag ? baseSequence : derivedList;

    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"B
D
B
D");
            // Note no castclass instructions
            // to be completely sure that stack states merge with expected types
            // we use "stloc;ldloc" as a surrogate "static cast" for values
            // in different branches
            verifier.VerifyIL("Program.Main", @"
{
  // Code size      133 (0x85)
  .maxstack  3
  .locals init (D<Base> V_0, //baseDelegate
                D<Derived> V_1, //derivedDelegate
                System.Collections.Generic.IEnumerable<Base> V_2, //baseSequence
                System.Collections.Generic.List<Derived> V_3, //derivedList
                D<Base> V_4)
  IL_0000:  ldc.i4.1
  IL_0001:  ldsfld     ""D<Base> Program.<>c.<>9__0_0""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0020
  IL_0009:  pop
  IL_000a:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000f:  ldftn      ""void Program.<>c.<Main>b__0_0()""
  IL_0015:  newobj     ""D<Base>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""D<Base> Program.<>c.<>9__0_0""
  IL_0020:  stloc.0
  IL_0021:  ldsfld     ""D<Derived> Program.<>c.<>9__0_1""
  IL_0026:  dup
  IL_0027:  brtrue.s   IL_0040
  IL_0029:  pop
  IL_002a:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_002f:  ldftn      ""void Program.<>c.<Main>b__0_1()""
  IL_0035:  newobj     ""D<Derived>..ctor(object, System.IntPtr)""
  IL_003a:  dup
  IL_003b:  stsfld     ""D<Derived> Program.<>c.<>9__0_1""
  IL_0040:  stloc.1
  IL_0041:  dup
  IL_0042:  brtrue.s   IL_004b
  IL_0044:  ldloc.1
  IL_0045:  stloc.s    V_4
  IL_0047:  ldloc.s    V_4
  IL_0049:  br.s       IL_004c
  IL_004b:  ldloc.0
  IL_004c:  callvirt   ""void D<Base>.Invoke()""
  IL_0051:  dup
  IL_0052:  brtrue.s   IL_0057
  IL_0054:  ldloc.0
  IL_0055:  br.s       IL_005c
  IL_0057:  ldloc.1
  IL_0058:  stloc.s    V_4
  IL_005a:  ldloc.s    V_4
  IL_005c:  callvirt   ""void D<Base>.Invoke()""
  IL_0061:  ldloc.0
  IL_0062:  dup
  IL_0063:  brtrue.s   IL_006b
  IL_0065:  pop
  IL_0066:  ldloc.1
  IL_0067:  stloc.s    V_4
  IL_0069:  ldloc.s    V_4
  IL_006b:  callvirt   ""void D<Base>.Invoke()""
  IL_0070:  ldloc.1
  IL_0071:  stloc.s    V_4
  IL_0073:  ldloc.s    V_4
  IL_0075:  dup
  IL_0076:  brtrue.s   IL_007a
  IL_0078:  pop
  IL_0079:  ldloc.0
  IL_007a:  callvirt   ""void D<Base>.Invoke()""
  IL_007f:  ldnull
  IL_0080:  stloc.2
  IL_0081:  ldnull
  IL_0082:  stloc.3
  IL_0083:  pop
  IL_0084:  ret
}");
        }

        [WorkItem(545408, "DevDiv")]
        [Fact]
        public void TestDelegateContravarianceConversions()
        {
            var source = @"
using System;

delegate void D<in T>();

class Base { }
class Derived : Base { }

class Program
{
    static void Main()
    {
        bool testFlag = true;

        D<Base> baseDelegate = () => Console.Write('B');
        D<Derived> derivedDelegate = () => Console.Write('D');

        D<Derived> fcn;
        
        fcn = testFlag ? baseDelegate : derivedDelegate;
        fcn();

        fcn = testFlag ? derivedDelegate : baseDelegate;
        fcn();

        fcn = baseDelegate ?? derivedDelegate;
        fcn();

        fcn = derivedDelegate ?? baseDelegate;
        fcn();
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"BDBD");
            verifier.VerifyIL("Program.Main", @"
{
  // Code size      119 (0x77)
  .maxstack  3
  .locals init (D<Base> V_0, //baseDelegate
                D<Derived> V_1, //derivedDelegate
                D<Derived> V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  ldsfld     ""D<Base> Program.<>c.<>9__0_0""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0020
  IL_0009:  pop
  IL_000a:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_000f:  ldftn      ""void Program.<>c.<Main>b__0_0()""
  IL_0015:  newobj     ""D<Base>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""D<Base> Program.<>c.<>9__0_0""
  IL_0020:  stloc.0
  IL_0021:  ldsfld     ""D<Derived> Program.<>c.<>9__0_1""
  IL_0026:  dup
  IL_0027:  brtrue.s   IL_0040
  IL_0029:  pop
  IL_002a:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_002f:  ldftn      ""void Program.<>c.<Main>b__0_1()""
  IL_0035:  newobj     ""D<Derived>..ctor(object, System.IntPtr)""
  IL_003a:  dup
  IL_003b:  stsfld     ""D<Derived> Program.<>c.<>9__0_1""
  IL_0040:  stloc.1
  IL_0041:  dup
  IL_0042:  brtrue.s   IL_0047
  IL_0044:  ldloc.1
  IL_0045:  br.s       IL_004a
  IL_0047:  ldloc.0
  IL_0048:  stloc.2
  IL_0049:  ldloc.2
  IL_004a:  callvirt   ""void D<Derived>.Invoke()""
  IL_004f:  brtrue.s   IL_0056
  IL_0051:  ldloc.0
  IL_0052:  stloc.2
  IL_0053:  ldloc.2
  IL_0054:  br.s       IL_0057
  IL_0056:  ldloc.1
  IL_0057:  callvirt   ""void D<Derived>.Invoke()""
  IL_005c:  ldloc.0
  IL_005d:  stloc.2
  IL_005e:  ldloc.2
  IL_005f:  dup
  IL_0060:  brtrue.s   IL_0064
  IL_0062:  pop
  IL_0063:  ldloc.1
  IL_0064:  callvirt   ""void D<Derived>.Invoke()""
  IL_0069:  ldloc.1
  IL_006a:  dup
  IL_006b:  brtrue.s   IL_0071
  IL_006d:  pop
  IL_006e:  ldloc.0
  IL_006f:  stloc.2
  IL_0070:  ldloc.2
  IL_0071:  callvirt   ""void D<Derived>.Invoke()""
  IL_0076:  ret
}");
        }

        [WorkItem(545408, "DevDiv")]
        [Fact]
        public void TestInterfaceCovarianceConversions()
        {
            string source = @"
using System;

interface I<out T> { }

class Base { }
class Derived : Base { }

class B : I<Base> { }
class D : I<Derived> { }

class Program
{
    static void Main()
    {
        bool testFlag = true;

        I<Base> baseInstance = new B();
        I<Derived> derivedInstance = new D();

        I<Base> i;
        
        i = testFlag ? baseInstance : derivedInstance;
        Console.Write(i.GetType().Name);

        i = testFlag ? derivedInstance : baseInstance;
        Console.Write(i.GetType().Name);

        i = baseInstance ?? derivedInstance;
        Console.Write(i.GetType().Name);

        i = derivedInstance ?? baseInstance;
        Console.Write(i.GetType().Name);
    }
}
";
            string expectedIL = @"
{
  // Code size      107 (0x6b)
  .maxstack  2
  .locals init (I<Base> V_0, //baseInstance
                I<Derived> V_1, //derivedInstance
                I<Base> V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""B..ctor()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""D..ctor()""
  IL_000c:  stloc.1
  IL_000d:  dup
  IL_000e:  brtrue.s   IL_0015
  IL_0010:  ldloc.1
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  br.s       IL_0016
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""System.Type object.GetType()""
  IL_001b:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  brtrue.s   IL_002a
  IL_0027:  ldloc.0
  IL_0028:  br.s       IL_002d
  IL_002a:  ldloc.1
  IL_002b:  stloc.2
  IL_002c:  ldloc.2
  IL_002d:  callvirt   ""System.Type object.GetType()""
  IL_0032:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0037:  call       ""void System.Console.Write(string)""
  IL_003c:  ldloc.0
  IL_003d:  dup
  IL_003e:  brtrue.s   IL_0044
  IL_0040:  pop
  IL_0041:  ldloc.1
  IL_0042:  stloc.2
  IL_0043:  ldloc.2
  IL_0044:  callvirt   ""System.Type object.GetType()""
  IL_0049:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_004e:  call       ""void System.Console.Write(string)""
  IL_0053:  ldloc.1
  IL_0054:  stloc.2
  IL_0055:  ldloc.2
  IL_0056:  dup
  IL_0057:  brtrue.s   IL_005b
  IL_0059:  pop
  IL_005a:  ldloc.0
  IL_005b:  callvirt   ""System.Type object.GetType()""
  IL_0060:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0065:  call       ""void System.Console.Write(string)""
  IL_006a:  ret
}
";

            var verifier = CompileAndVerify(source, expectedOutput: @"BDBD");
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [WorkItem(545408, "DevDiv")]
        [Fact]
        public void TestInterfaceContravarianceConversions()
        {
            string source = @"
using System;

interface I<in T> { }

class Base { }
class Derived : Base { }

class B : I<Base> { }
class D : I<Derived> { }

class Program
{
    static void Main()
    {
        bool testFlag = true;

        I<Base> baseInstance = new B();
        I<Derived> derivedInstance = new D();

        I<Derived> i;
        
        i = testFlag ? baseInstance : derivedInstance;
        Console.Write(i.GetType().Name);

        i = testFlag ? derivedInstance : baseInstance;
        Console.Write(i.GetType().Name);

        i = baseInstance ?? derivedInstance;
        Console.Write(i.GetType().Name);

        i = derivedInstance ?? baseInstance;
        Console.Write(i.GetType().Name);
    }
}
";
            string expectedIL = @"
{
  // Code size      107 (0x6b)
  .maxstack  2
  .locals init (I<Base> V_0, //baseInstance
                I<Derived> V_1, //derivedInstance
                I<Derived> V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""B..ctor()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""D..ctor()""
  IL_000c:  stloc.1
  IL_000d:  dup
  IL_000e:  brtrue.s   IL_0013
  IL_0010:  ldloc.1
  IL_0011:  br.s       IL_0016
  IL_0013:  ldloc.0
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  callvirt   ""System.Type object.GetType()""
  IL_001b:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  brtrue.s   IL_002c
  IL_0027:  ldloc.0
  IL_0028:  stloc.2
  IL_0029:  ldloc.2
  IL_002a:  br.s       IL_002d
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""System.Type object.GetType()""
  IL_0032:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0037:  call       ""void System.Console.Write(string)""
  IL_003c:  ldloc.0
  IL_003d:  stloc.2
  IL_003e:  ldloc.2
  IL_003f:  dup
  IL_0040:  brtrue.s   IL_0044
  IL_0042:  pop
  IL_0043:  ldloc.1
  IL_0044:  callvirt   ""System.Type object.GetType()""
  IL_0049:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_004e:  call       ""void System.Console.Write(string)""
  IL_0053:  ldloc.1
  IL_0054:  dup
  IL_0055:  brtrue.s   IL_005b
  IL_0057:  pop
  IL_0058:  ldloc.0
  IL_0059:  stloc.2
  IL_005a:  ldloc.2
  IL_005b:  callvirt   ""System.Type object.GetType()""
  IL_0060:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
  IL_0065:  call       ""void System.Console.Write(string)""
  IL_006a:  ret
}
";

            var verifier = CompileAndVerify(source, expectedOutput: @"BDBD");
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestBug7196()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

class Program
{
    private static bool testFlag = false;

    static void Main()
    {

            IEnumerable<string> v1 = Enumerable.Repeat<string>(""string"", 1);
            IEnumerable<object> v2 = Enumerable.Empty<object>();
            IEnumerable<object> v3 = testFlag ? v2 : v1;

            if (!testFlag){
                Console.WriteLine(v3.Count());
            }
    }
}
";
            string expectedIL = @"
{
  // Code size       51 (0x33)
  .maxstack  2
  .locals init (System.Collections.Generic.IEnumerable<string> V_0, //v1
  System.Collections.Generic.IEnumerable<object> V_1, //v2
  System.Collections.Generic.IEnumerable<object> V_2, //v3
  System.Collections.Generic.IEnumerable<object> V_3)
  IL_0000:  ldstr      ""string""
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Repeat<string>(string, int)""
  IL_000b:  stloc.0
  IL_000c:  call       ""System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Empty<object>()""
  IL_0011:  stloc.1
  IL_0012:  ldsfld     ""bool Program.testFlag""
  IL_0017:  brtrue.s   IL_001e
  IL_0019:  ldloc.0
  IL_001a:  stloc.3
  IL_001b:  ldloc.3
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  stloc.2
  IL_0020:  ldsfld     ""bool Program.testFlag""
  IL_0025:  brtrue.s   IL_0032
  IL_0027:  ldloc.2
  IL_0028:  call       ""int System.Linq.Enumerable.Count<object>(System.Collections.Generic.IEnumerable<object>)""
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  ret
}
";

            var verifier = CompileAndVerify(new string[] { source }, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "1");
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestBug7196a()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

class Program
{
    private static bool testFlag = true;

    static void Main()
    {

            IEnumerable<string> v1 = Enumerable.Repeat<string>(""string"", 1);
            IEnumerable<object> v2 = Enumerable.Empty<object>();
            IEnumerable<object> v3 = v1 ?? v2;

            if (testFlag){
                Console.WriteLine(v3.Count());
            }
    }
}
";
            string expectedIL = @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (System.Collections.Generic.IEnumerable<object> V_0, //v2
  System.Collections.Generic.IEnumerable<object> V_1, //v3
  System.Collections.Generic.IEnumerable<object> V_2)
  IL_0000:  ldstr      ""string""
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Repeat<string>(string, int)""
  IL_000b:  call       ""System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Empty<object>()""
  IL_0010:  stloc.0
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  dup
  IL_0014:  brtrue.s   IL_0018
  IL_0016:  pop
  IL_0017:  ldloc.0
  IL_0018:  stloc.1
  IL_0019:  ldsfld     ""bool Program.testFlag""
  IL_001e:  brfalse.s  IL_002b
  IL_0020:  ldloc.1
  IL_0021:  call       ""int System.Linq.Enumerable.Count<object>(System.Collections.Generic.IEnumerable<object>)""
  IL_0026:  call       ""void System.Console.WriteLine(int)""
  IL_002b:  ret
}
";

            var verifier = CompileAndVerify(new string[] { source }, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "1");
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestBug7196b()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

class Program
{
    private static bool testFlag = true;

    static void Main()
    {

            string[] v1 = Enumerable.Repeat<string>(""string"", 1).ToArray();
            object[] v2 = Enumerable.Empty<object>().ToArray();
            object[] v3 = v1 ?? v2;

            if (testFlag){
                Console.WriteLine(v3.Length);
            }
    }
}
";
            string expectedIL = @"
{
  // Code size       51 (0x33)
  .maxstack  2
  .locals init (object[] V_0, //v2
  object[] V_1, //v3
  object[] V_2)
  IL_0000:  ldstr      ""string""
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Repeat<string>(string, int)""
  IL_000b:  call       ""string[] System.Linq.Enumerable.ToArray<string>(System.Collections.Generic.IEnumerable<string>)""
  IL_0010:  call       ""System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Empty<object>()""
  IL_0015:  call       ""object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)""
  IL_001a:  stloc.0
  IL_001b:  stloc.2
  IL_001c:  ldloc.2
  IL_001d:  dup
  IL_001e:  brtrue.s   IL_0022
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  stloc.1
  IL_0023:  ldsfld     ""bool Program.testFlag""
  IL_0028:  brfalse.s  IL_0032
  IL_002a:  ldloc.1
  IL_002b:  ldlen
  IL_002c:  conv.i4
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  ret
}
";

            var verifier = CompileAndVerify(new string[] { source }, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "1");
            verifier.VerifyIL("Program.Main", expectedIL);
        }


        [Fact]
        public void TestBug7196c()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

class Program
{
    private static bool testFlag = true;

    static void Main()
    {

            IEnumerable<string>[] v1 = new IEnumerable<string>[] { Enumerable.Repeat<string>(""string"", 1)};
            IEnumerable<object>[] v2 = new IEnumerable<object>[] { Enumerable.Empty<object>()};
            IEnumerable<object>[] v3 = v1 ?? v2;

            if (testFlag){
                Console.WriteLine(v3.Length);
            }
    }
}
";
            string expectedIL = @"
{
  // Code size       61 (0x3d)
  .maxstack  5
  .locals init (System.Collections.Generic.IEnumerable<string>[] V_0, //v1
  System.Collections.Generic.IEnumerable<object>[] V_1, //v2
  System.Collections.Generic.IEnumerable<object>[] V_2, //v3
  System.Collections.Generic.IEnumerable<object>[] V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""System.Collections.Generic.IEnumerable<string>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""string""
  IL_000d:  ldc.i4.1
  IL_000e:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Repeat<string>(string, int)""
  IL_0013:  stelem.ref
  IL_0014:  stloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  newarr     ""System.Collections.Generic.IEnumerable<object>""
  IL_001b:  dup
  IL_001c:  ldc.i4.0
  IL_001d:  call       ""System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Empty<object>()""
  IL_0022:  stelem.ref
  IL_0023:  stloc.1
  IL_0024:  ldloc.0
  IL_0025:  stloc.3
  IL_0026:  ldloc.3
  IL_0027:  dup
  IL_0028:  brtrue.s   IL_002c
  IL_002a:  pop
  IL_002b:  ldloc.1
  IL_002c:  stloc.2
  IL_002d:  ldsfld     ""bool Program.testFlag""
  IL_0032:  brfalse.s  IL_003c
  IL_0034:  ldloc.2
  IL_0035:  ldlen
  IL_0036:  conv.i4
  IL_0037:  call       ""void System.Console.WriteLine(int)""
  IL_003c:  ret
}
";

            var verifier = CompileAndVerify(new string[] { source }, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "1");
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestBug7196d()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

class Program
{
    private static bool testFlag = true;

    static void Main()
    {

            IEnumerable<string>[] v1 = new IEnumerable<string>[] { Enumerable.Repeat<string>(""string"", 1)};
            IEnumerable[] v2 = new IEnumerable<object>[] { Enumerable.Empty<object>()};
            IEnumerable[] v3 = v1 ?? v2;

            if (testFlag){
                Console.WriteLine(v3.Length);
            }
    }
}
";
            string expectedIL = @"
{
  // Code size       59 (0x3b)
  .maxstack  5
  .locals init (System.Collections.Generic.IEnumerable<string>[] V_0, //v1
  System.Collections.IEnumerable[] V_1, //v2
  System.Collections.IEnumerable[] V_2) //v3
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""System.Collections.Generic.IEnumerable<string>""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""string""
  IL_000d:  ldc.i4.1
  IL_000e:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Repeat<string>(string, int)""
  IL_0013:  stelem.ref
  IL_0014:  stloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  newarr     ""System.Collections.Generic.IEnumerable<object>""
  IL_001b:  dup
  IL_001c:  ldc.i4.0
  IL_001d:  call       ""System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Empty<object>()""
  IL_0022:  stelem.ref
  IL_0023:  stloc.1
  IL_0024:  ldloc.0
  IL_0025:  dup
  IL_0026:  brtrue.s   IL_002a
  IL_0028:  pop
  IL_0029:  ldloc.1
  IL_002a:  stloc.2
  IL_002b:  ldsfld     ""bool Program.testFlag""
  IL_0030:  brfalse.s  IL_003a
  IL_0032:  ldloc.2
  IL_0033:  ldlen
  IL_0034:  conv.i4
  IL_0035:  call       ""void System.Console.WriteLine(int)""
  IL_003a:  ret
}
";

            var verifier = CompileAndVerify(new string[] { source }, additionalRefs: new[] { SystemCoreRef }, expectedOutput: "1");
            verifier.VerifyIL("Program.Main", expectedIL);
        }

        [WorkItem(545408, "DevDiv")]
        [Fact]
        public void TestVarianceConversions()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

namespace TernaryAndVarianceConversion
{
    delegate void CovariantDelegateWithVoidReturn<out T>();
    delegate T CovariantDelegateWithValidReturn<out T>();

    delegate void ContravariantDelegateVoidReturn<in T>();
    delegate void ContravariantDelegateWithValidInParm<in T>(T inVal);

    interface ICovariantInterface<out T>
    {
        void CovariantInterfaceMethodWithVoidReturn();
        T CovariantInterfaceMethodWithValidReturn();
        T CovariantInterfacePropertyWithValidGetter { get; }
        void Test();
    }

    interface IContravariantInterface<in T>
    {
        void ContravariantInterfaceMethodWithVoidReturn();
        void ContravariantInterfaceMethodWithValidInParm(T inVal);
        T ContravariantInterfacePropertyWithValidSetter { set; }
        void Test();
    }

    class CovariantInterfaceImpl<T> : ICovariantInterface<T>
    {
        public void CovariantInterfaceMethodWithVoidReturn() { }
        public T CovariantInterfaceMethodWithValidReturn()
        {
            return default(T);
        }
        public T CovariantInterfacePropertyWithValidGetter
        {
            get { return default(T); }
        }
        public void Test()
        {
            Console.WriteLine(""{0}"", typeof(T));
        }
    }

    class ContravariantInterfaceImpl<T> : IContravariantInterface<T>
    {
        public void ContravariantInterfaceMethodWithVoidReturn() { }
        public void ContravariantInterfaceMethodWithValidInParm(T inVal) { }
        public T ContravariantInterfacePropertyWithValidSetter
        {
            set { }
        }
        public void Test()
        {
            Console.WriteLine(""{0}"", typeof(T));
        }
    }

    class Animal { }
    class Mammal : Animal { }

    class Program
    {
        static void Test(bool testFlag)
        {
            Console.WriteLine(""Testing with ternary test flag == {0}"", testFlag);

            // Repro case for bug 7196
            IEnumerable<object> EnumerableOfObject =
                                    (testFlag ?
                                     Enumerable.Repeat<string>(""string"", 1) :
                                     Enumerable.Empty<object>());
            Console.WriteLine(""{0}"", EnumerableOfObject.Count());


            // Covariant implicit conversion for delegates
            CovariantDelegateWithVoidReturn<Animal> covariantDelegateWithVoidReturnOfAnimal = () => { Console.WriteLine(""{0}"", typeof(Animal)); };
            CovariantDelegateWithVoidReturn<Mammal> covariantDelegateWithVoidReturnOfMammal = () => { Console.WriteLine(""{0}"", typeof(Mammal)); };
            CovariantDelegateWithVoidReturn<Animal> covariantDelegateWithVoidReturnOfAnimalTest;
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? covariantDelegateWithVoidReturnOfMammal : covariantDelegateWithVoidReturnOfAnimal;
            covariantDelegateWithVoidReturnOfAnimalTest();
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? covariantDelegateWithVoidReturnOfAnimal : covariantDelegateWithVoidReturnOfMammal;
            covariantDelegateWithVoidReturnOfAnimalTest();

            CovariantDelegateWithValidReturn<Animal> covariantDelegateWithValidReturnOfAnimal = () => { Console.WriteLine(""{0}"", typeof(Animal)); return default(Animal); };
            CovariantDelegateWithValidReturn<Mammal> covariantDelegateWithValidReturnOfMammal = () => { Console.WriteLine(""{0}"", typeof(Mammal)); return default(Mammal); };
            CovariantDelegateWithValidReturn<Animal> covariantDelegateWithValidReturnOfAnimalTest;
            covariantDelegateWithValidReturnOfAnimalTest = testFlag ? covariantDelegateWithValidReturnOfMammal : covariantDelegateWithValidReturnOfAnimal;
            covariantDelegateWithValidReturnOfAnimalTest();
            covariantDelegateWithValidReturnOfAnimalTest = testFlag ? covariantDelegateWithValidReturnOfAnimal : covariantDelegateWithValidReturnOfMammal;
            covariantDelegateWithValidReturnOfAnimalTest();

            // Contravariant implicit conversion for delegates
            ContravariantDelegateVoidReturn<Animal> contravariantDelegateVoidReturnOfAnimal = () => { Console.WriteLine(""{0}"", typeof(Animal)); };
            ContravariantDelegateVoidReturn<Mammal> contravariantDelegateVoidReturnOfMammal = () => { Console.WriteLine(""{0}"", typeof(Mammal)); };
            ContravariantDelegateVoidReturn<Mammal> contravariantDelegateVoidReturnOfMammalTest;
            contravariantDelegateVoidReturnOfMammalTest = testFlag ? contravariantDelegateVoidReturnOfMammal : contravariantDelegateVoidReturnOfAnimal;
            contravariantDelegateVoidReturnOfMammalTest();
            contravariantDelegateVoidReturnOfMammalTest = testFlag ? contravariantDelegateVoidReturnOfAnimal : contravariantDelegateVoidReturnOfMammal;
            contravariantDelegateVoidReturnOfMammalTest();

            ContravariantDelegateWithValidInParm<Animal> contravariantDelegateWithValidInParmOfAnimal = (Animal) => { Console.WriteLine(""{0}"", typeof(Animal)); };
            ContravariantDelegateWithValidInParm<Mammal> contravariantDelegateWithValidInParmOfMammal = (Mammal) => { Console.WriteLine(""{0}"", typeof(Mammal)); };
            ContravariantDelegateWithValidInParm<Mammal> contravariantDelegateWithValidInParmOfMammalTest;
            contravariantDelegateWithValidInParmOfMammalTest = testFlag ? contravariantDelegateWithValidInParmOfMammal : contravariantDelegateWithValidInParmOfAnimal;
            contravariantDelegateWithValidInParmOfMammalTest(default(Mammal));
            contravariantDelegateWithValidInParmOfMammalTest = testFlag ? contravariantDelegateWithValidInParmOfAnimal : contravariantDelegateWithValidInParmOfMammal;
            contravariantDelegateWithValidInParmOfMammalTest(default(Mammal));

            // Covariant implicit conversion for interfaces
            ICovariantInterface<Animal> covariantInterfaceOfAnimal = new CovariantInterfaceImpl<Animal>();
            ICovariantInterface<Mammal> covariantInterfaceOfMammal = new CovariantInterfaceImpl<Mammal>();
            ICovariantInterface<Animal> covariantInterfaceOfAnimalTest;
            covariantInterfaceOfAnimalTest = testFlag ? covariantInterfaceOfMammal : covariantInterfaceOfAnimal;
            covariantInterfaceOfAnimalTest.Test();
            covariantInterfaceOfAnimalTest = testFlag ? covariantInterfaceOfAnimal : covariantInterfaceOfMammal;
            covariantInterfaceOfAnimalTest.Test();

            // Contravariant implicit conversion for interfaces
            IContravariantInterface<Animal> contravariantInterfaceOfAnimal = new ContravariantInterfaceImpl<Animal>();
            IContravariantInterface<Mammal> contravariantInterfaceOfMammal = new ContravariantInterfaceImpl<Mammal>();
            IContravariantInterface<Mammal> contravariantInterfaceOfMammalTest;
            contravariantInterfaceOfMammalTest = testFlag ? contravariantInterfaceOfMammal : contravariantInterfaceOfAnimal;
            contravariantInterfaceOfMammalTest.Test();
            contravariantInterfaceOfMammalTest = testFlag ? contravariantInterfaceOfAnimal : contravariantInterfaceOfMammal;
            contravariantInterfaceOfMammalTest.Test();

            // With explicit casting
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? (CovariantDelegateWithVoidReturn<Animal>)covariantDelegateWithVoidReturnOfMammal : covariantDelegateWithVoidReturnOfAnimal;
            covariantDelegateWithVoidReturnOfAnimalTest();
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? covariantDelegateWithVoidReturnOfAnimal : (CovariantDelegateWithVoidReturn<Animal>)covariantDelegateWithVoidReturnOfMammal;
            covariantDelegateWithVoidReturnOfAnimalTest();

            // With parens
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? (covariantDelegateWithVoidReturnOfMammal) : covariantDelegateWithVoidReturnOfAnimal;
            covariantDelegateWithVoidReturnOfAnimalTest();
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? covariantDelegateWithVoidReturnOfAnimal : (covariantDelegateWithVoidReturnOfMammal);
            covariantDelegateWithVoidReturnOfAnimalTest();
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? ((CovariantDelegateWithVoidReturn<Animal>)covariantDelegateWithVoidReturnOfMammal) : covariantDelegateWithVoidReturnOfAnimal;
            covariantDelegateWithVoidReturnOfAnimalTest();
            covariantDelegateWithVoidReturnOfAnimalTest = testFlag ? covariantDelegateWithVoidReturnOfAnimal : ((CovariantDelegateWithVoidReturn<Animal>)covariantDelegateWithVoidReturnOfMammal);
            covariantDelegateWithVoidReturnOfAnimalTest();

            // Bug 291602
            int[] intarr = { 1, 2, 3 };
            IList<int> intlist = new List<int>(intarr);
            IList<int> intternary = testFlag ? intarr : intlist;
            Console.WriteLine(intternary);
        }
        static void Main(string[] args)
        {
            Test(true);
            Test(false);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"Testing with ternary test flag == True
1
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
System.Int32[]
Testing with ternary test flag == False
0
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
TernaryAndVarianceConversion.Animal
TernaryAndVarianceConversion.Mammal
System.Collections.Generic.List`1[System.Int32]
");
        }

        [WorkItem(528424, "DevDiv")]
        [Fact()]
        public void TestErrorOperand()
        {
            var source =
@"class C
{
    static object M(bool b, C c, D d)
    {
        return b ? c : d;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,34): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D"));
        }

        private static void TestConditional(string conditionalExpression, string expectedType, params DiagnosticDescription[] expectedDiagnostics)
        {
            string sourceTemplate = @"
class C
{{
    void Test<T, U>()
    {{
        System.Console.WriteLine({0});
    }}

    int GetInt() {{ return 1; }}
    void GetVoid() {{ return ; }}
    bool GetBool() {{ return true; }}
    short GetShort() {{ return 1; }}
    char GetChar() {{ return 'a'; }}
    double GetDouble() {{ return 1.5; }}
    string GetString() {{ return ""hello""; }}
    object GetObject() {{ return new object(); }}
    C GetUserNonGeneric() {{ return new C(); }}
    D<T> GetUserGeneric<T>() {{ return new D<T>(); }}
    T GetTypeParameter<T>() {{ return default(T); }}
    I<T, U> GetVariantInterface<T, U>() {{ return null; }}
}}

class D<T> {{ }}
public enum color {{ Red, Blue, Green }};
interface I<in T, out U> {{ }}";

            var source = string.Format(sourceTemplate, conditionalExpression);
            var tree = Parse(source);

            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var compUnit = tree.GetCompilationUnitRoot();
            var classC = (TypeDeclarationSyntax)compUnit.Members.First();
            var methodTest = (MethodDeclarationSyntax)classC.Members.First();
            var stmt = (ExpressionStatementSyntax)methodTest.Body.Statements.Single();
            var invocationExpr = (InvocationExpressionSyntax)stmt.Expression;
            var conditionalExpr = (ConditionalExpressionSyntax)invocationExpr.ArgumentList.Arguments.Single().Expression;

            var model = comp.GetSemanticModel(tree);

            if (expectedType != null)
            {
                Assert.Equal(expectedType, model.GetTypeInfo(conditionalExpr).Type.ToTestDisplayString());

                if (!expectedDiagnostics.Any())
                {
                    Assert.Equal(SpecialType.System_Boolean, model.GetTypeInfo(conditionalExpr.Condition).Type.SpecialType);
                    Assert.Equal(expectedType, model.GetTypeInfo(conditionalExpr.WhenTrue).ConvertedType.ToTestDisplayString()); //in parent to catch conversion
                    Assert.Equal(expectedType, model.GetTypeInfo(conditionalExpr.WhenFalse).ConvertedType.ToTestDisplayString()); //in parent to catch conversion
                }
            }
        }


        [Fact, WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")]
        public void ConditionalAccessToEvent_01()
        {
            string source = @"
using System;

class TestClass
{
    event Action test;

    public static void Test(TestClass receiver)
    {
        Console.WriteLine(receiver?.test);
    }

    static void Main()
    {
        Console.WriteLine(""----"");
        Test(null);
        Console.WriteLine(""----"");
        Test(new TestClass() {test = Main});
        Console.WriteLine(""----"");
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput: 
@"----

----
System.Action
----");

            var tree = compilation.SyntaxTrees.Single();
            var memberBinding = tree.GetRoot().DescendantNodes().OfType<MemberBindingExpressionSyntax>().Single();
            var access = (ConditionalAccessExpressionSyntax)memberBinding.Parent;

            Assert.Equal(".test", memberBinding.ToString());
            Assert.Equal("receiver?.test", access.ToString());

            var model = compilation.GetSemanticModel(tree);

            Assert.Equal("event System.Action TestClass.test", model.GetSymbolInfo(memberBinding).Symbol.ToTestDisplayString());
            Assert.Equal("event System.Action TestClass.test", model.GetSymbolInfo(memberBinding.Name).Symbol.ToTestDisplayString());

            Assert.Null(model.GetSymbolInfo(access).Symbol);
        }

        [Fact, WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")]
        public void ConditionalAccessToEvent_02()
        {
            string source = @"
using System;

class TestClass
{
    event Action test;

    public static void Test(TestClass receiver)
    {
        receiver?.test();
    }

    static void Main()
    {
        Console.WriteLine(""----"");
        Test(null);
        Console.WriteLine(""----"");
        Test(new TestClass() {test = Target});
        Console.WriteLine(""----"");
    }

    static void Target()
    {
        Console.WriteLine(""Target"");
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput:
@"----
----
Target
----");

            var tree = compilation.SyntaxTrees.Single();
            var memberBinding = tree.GetRoot().DescendantNodes().OfType<MemberBindingExpressionSyntax>().Single();
            var invocation = (InvocationExpressionSyntax)memberBinding.Parent;
            var access = (ConditionalAccessExpressionSyntax)invocation.Parent;

            Assert.Equal(".test", memberBinding.ToString());
            Assert.Equal(".test()", invocation.ToString());
            Assert.Equal("receiver?.test()", access.ToString());

            var model = compilation.GetSemanticModel(tree);

            Assert.Equal("event System.Action TestClass.test", model.GetSymbolInfo(memberBinding).Symbol.ToTestDisplayString());
            Assert.Equal("event System.Action TestClass.test", model.GetSymbolInfo(memberBinding.Name).Symbol.ToTestDisplayString());
            Assert.Equal("void System.Action.Invoke()", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());

            Assert.Null(model.GetSymbolInfo(access).Symbol);
        }

        [Fact, WorkItem(4028, "https://github.com/dotnet/roslyn/issues/4028")]
        public void ConditionalAccessToEvent_03()
        {
            string source = @"
using System;

class TestClass
{
    event Action test;

    public static void Test(TestClass receiver)
    {
        receiver?.test += Main;
    }

    static void Main()
    {
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            compilation.VerifyDiagnostics(
    // (10,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
    //         receiver?.test += Main;
    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "receiver?.test").WithLocation(10, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var memberBinding = tree.GetRoot().DescendantNodes().OfType<MemberBindingExpressionSyntax>().Single();
            var access = (ConditionalAccessExpressionSyntax)memberBinding.Parent;

            Assert.Equal(".test", memberBinding.ToString());
            Assert.Equal("receiver?.test", access.ToString());

            var model = compilation.GetSemanticModel(tree);

            Assert.Equal("event System.Action TestClass.test", model.GetSymbolInfo(memberBinding).Symbol.ToTestDisplayString());
            Assert.Equal("event System.Action TestClass.test", model.GetSymbolInfo(memberBinding.Name).Symbol.ToTestDisplayString());

            Assert.Null(model.GetSymbolInfo(access).Symbol);
        }

        [Fact(), WorkItem(4615, "https://github.com/dotnet/roslyn/issues/4615")]
        public void ConditionalAndConditionalMethods()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        TestClass.Create().Test();
        TestClass.Create().Self().Test();
        System.Console.WriteLine(""---"");
        TestClass.Create()?.Test();
        TestClass.Create()?.Self().Test();
        TestClass.Create()?.Self()?.Test();
     }
}

class TestClass
{
    [System.Diagnostics.Conditional(""DEBUG"")]
    public void Test() 
    { 
        System.Console.WriteLine(""Test"");
    }

    public static TestClass Create()
    {
        System.Console.WriteLine(""Create"");
        return new TestClass();
    }

    public TestClass Self()
    {
        System.Console.WriteLine(""Self"");
        return this;
    }
}
";
            
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe,
                                                            parseOptions: CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG"));

            CompileAndVerify(compilation, expectedOutput:
@"Create
Test
Create
Self
Test
---
Create
Test
Create
Self
Test
Create
Self
Test
");

            compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput:"---");
        }
    }
}
