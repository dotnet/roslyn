// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding switch statement.
    /// </summary>
    public class SwitchTests : CompilingTestBase
    {
        #region "Common Error Tests"

        [WorkItem(543285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543285")]
        [Fact]
        public void NoCS0029ForUsedLocalConstInSwitch()
        {
            var source = @"
class Program
{
    static void Main(string[] args) 
    {
        const string ss = ""A"";
        switch (args[0])
        {
            case ss:
                break;
        }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void CS0037_NullCaseLabel_NonNullableSwitchExpression()
        {
            var text = @"using System;

public class Test
{
	enum eTypes {
		kFirst,
		kSecond,
		kThird,
	};
    public static int Main(string [] args)
    {
		int ret = 0;
		ret = DoEnum();
        return(ret);
    }
	
	private static int DoEnum()
	{
	    int ret = 0;
        eTypes e = eTypes.kSecond;

	    switch (e) {
            case null:
                break;
	        default:
	            ret = 1;
        	    break;
	    }

	    Console.WriteLine(ret);
	    return(ret);
	}
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (23,18): error CS0037: Cannot convert null to 'Test.eTypes' because it is a non-nullable value type
                //             case null:
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("Test.eTypes").WithLocation(23, 18));
        }

        [WorkItem(542773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542773")]
        [Fact]
        public void CS0119_TypeUsedAsSwitchExpression()
        {
            var text = @"class A
{
    public static void Main()
    { }
    void foo(color color1)
    {
        switch (color)
        {
            default:
                break;
        }
    }
}
enum color
{
    blue,
    green
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,17): error CS0119: 'color' is a type, which is not valid in the given context
                //         switch (color)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "color").WithArguments("color", "type").WithLocation(7, 17));
        }

        [Fact]
        public void CS0150_NonConstantSwitchCase()
        {
            var text = @"
public class Test
{
    public static int Main(string [] args)
    {
		int ret = 1;
		int value = 23;
        int test = 1;

		switch (value) {
		    case test:
			    ret = 1;
			    break;
		    default:
			    ret = 1;
                break;
		}

        return(ret);
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (11,7): error CS0150: A constant value is expected
                // 		    case test:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "case test:").WithLocation(11, 7));
        }

        [Fact]
        public void CS0152_DuplicateCaseLabel()
        {
            var text = @"
public class A
{
	public static int Main()
	{
		int i = 0;

		switch (i)
		{
			case 1: break;
            case 1: break;   // CS0152
		}

		return 1;
	}

    public void foo(char c)
    {
        switch (c)
        {
            case 'f':
                break;
            case 'f':       // CS0152
                break;
        }
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (11,13): error CS0152: The switch statement contains multiple cases with the label value '1'
    //             case 1: break;   // CS0152
    Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(11, 13),
    // (23,13): error CS0152: The switch statement contains multiple cases with the label value 'f'
    //             case 'f':       // CS0152
    Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 'f':").WithArguments("f").WithLocation(23, 13));
        }

        [Fact]
        public void CS0152_DuplicateCaseLabelWithDifferentTypes()
        {
            var text = @"
public class A
{
	public static int Main()
	{
		long i = 0;

		switch (i)
		{
			case 1L: break;
            case 1: break;   // CS0152
		}

		return 1;
	}

    public void foo(int i)
    {
        switch (i)
        {
            case 'a':
                break;
            case 97:       // CS0152
                break;            
        }
    }

    public void foo2(char i)
    {
        switch (i)
        {
            case 97.0f:
                break;
            case 97.0f:
                break;
            case 'a':
                break;
            case 97:
                break;
        }
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (11,13): error CS0152: The switch statement contains multiple cases with the label value '1'
    //             case 1: break;   // CS0152
    Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(11, 13),
    // (23,13): error CS0152: The switch statement contains multiple cases with the label value '97'
    //             case 97:       // CS0152
    Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 97:").WithArguments("97").WithLocation(23, 13),
    // (32,18): error CS0266: Cannot implicitly convert type 'float' to 'char'. An explicit conversion exists (are you missing a cast?)
    //             case 97.0f:
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "97.0f").WithArguments("float", "char").WithLocation(32, 18),
    // (34,18): error CS0266: Cannot implicitly convert type 'float' to 'char'. An explicit conversion exists (are you missing a cast?)
    //             case 97.0f:
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "97.0f").WithArguments("float", "char").WithLocation(34, 18),
    // (38,18): error CS0266: Cannot implicitly convert type 'int' to 'char'. An explicit conversion exists (are you missing a cast?)
    //             case 97:
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "97").WithArguments("int", "char").WithLocation(38, 18));
        }

        [Fact]
        public void CS0152_DuplicateDefaultLabel()
        {
            var text = @"
public class TestClass
{
    public static void Main()
    {
        int i = 10;
        switch (i)
        {
            default:
                break;
            case 0:
                break;
            case 1:
                break;
            default:            //CS0152
                break;
        }
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (15,13): error CS0152: The label 'default:' already occurs in this switch statement
                // 		default:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "default:").WithArguments("default:").WithLocation(15, 13));
        }

        [Fact]
        public void CS0159_NestedSwitchWithInvalidGoto()
        {
            var text = @"
public class Test
{
    public static int Main(string [] args)
    {
		switch (5) {
		case 5: 
			switch (2) {
			case 1:
				goto case 5;
			}
			break;
		}

		return(0);
    }
}";
            // CONSIDER: Cascading diagnostics should be disabled in flow analysis?

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,5): error CS0159: No such label 'case 5:' within the scope of the goto statement
                // 				goto case 5;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 5;").WithArguments("case 5:").WithLocation(10, 5),
                // (10,5): warning CS0162: Unreachable code detected
                // 				goto case 5;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goto").WithLocation(10, 5));
        }

        [Fact]
        public void CS0166_InvalidSwitchGoverningType()
        {
            var text = @"
public class Test
{
    public static int Main(string [] args)
    {
		double test = 1.1;
        int ret = 1;

		switch (test) {
		    case 1:
			    ret = 1;
			    break;
		    default:
			    ret = 1;
                break;
		}

        return(ret);
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (9,11): error CS0166: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch (test) {
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "test").WithLocation(9, 11));
        }

        [Fact]
        public void CS0166_InvalidSwitchExpression_Null()
        {
            var text = @"
class T
{
    public static void Main()
    {
        switch(null)
        {
            default:
                break;
        }
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,16): error CS0166: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(null)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "null").WithLocation(6, 16));
        }

        [Fact]
        public void CS0166_InvalidSwitchExpression_MethodGroup()
        {
            var text = @"
class T
{
    public static void Main()
    {
        switch(M())
        {
            default:
                break;
        }
    }

    public static void M() { }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,16): error CS0166: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(M())
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "M()").WithLocation(6, 16));
        }

        [Fact]
        public void CS0166_InvalidSwitchExpression_Lambda()
        {
            var text = @"
class T
{
    public static void Main()
    {
        switch(() => {})
        {
            default:
                break;
        }
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,16): error CS0166: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(() => {})
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "() => {}").WithLocation(6, 16));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_01()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
class Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator int? (Conv C)
	{
		return null;
	}

    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (17,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(17, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_02()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
class Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator char? (Conv C)
	{
		return null;
	}

    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (17,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(17, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_03()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator int? (Conv? C)
	{
		return null;
	}
	
    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 0;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (17,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(17, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_04()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator int? (Conv? C)
	{
		return null;
	}
	
    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                break;
		}

        Conv? D = new Conv();
		switch(D)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 0;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (17,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(17, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_05()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
    public static implicit operator int(Conv C)
    {
        return 1;
    }

    public static implicit operator int?(Conv C)
    {
        return 1;
    }

    public static implicit operator int(Conv? C)
    {
        return 1;
    }

    public static implicit operator int?(Conv? C)
    {
        return 0;
    }	
    
    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                break;
		}

        return 0;
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (27,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(27, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_06()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
    public static implicit operator int(Conv C)
    {
        return 1;
    }

    public static implicit operator int?(Conv C)
    {
        return 1;
    }

    public static implicit operator int(Conv? C)
    {
        return 1;
    }

    public static implicit operator int?(Conv? C)
    {
        return 0;
    }	
    
    public static int Main()
	{
		Conv? C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                break;
		}

        return 0;
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (27,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(27, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithMultipleImplicitConversions_07()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            // Native compiler allows the below code to compile
            // even though there are two user-defined implicit conversions:
            // 1) To int type (applicable in normal form): public static implicit operator int (Conv? C2)
            // 2) To int? type (applicable in lifted form): public static implicit operator int (Conv C)
            //
            // Here we deliberately violate the specification and allow the conversion, for backwards compat.

            var text = @"
struct Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator int (Conv? C2)
	{
		return 0;
	}
	
    public static int Main()
	{
		Conv? D = new Conv();
		switch(D)
		{
		    case 1:
                System.Console.WriteLine(""Fail"");
                return 1;
		    case 0:
                System.Console.WriteLine(""Pass"");
                return 0;
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact()]
        public void CS0166_AggregateTypeWithNoValidImplicitConversions_01()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
class Conv
{
	// bool type is not valid
	public static implicit operator bool (Conv C)
	{
		return false;
	}
	
    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (13,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(13, 10));
        }

        [Fact()]
        public void CS0166_AggregateTypeWithNoValidImplicitConversions_02()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
enum X { F = 0 }
class Conv
{
	// enum type is not valid
	public static implicit operator X (Conv C)
	{
		return X.F;
	}
	
    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (14,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "C").WithLocation(14, 10));
        }

        [Fact()]
        public void CS0533_AggregateTypeWithInvalidObjectTypeConversion()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
class Conv
{
	// object type is not valid
	public static implicit operator object(Conv C)
	{
		return null;
	}

    public static implicit operator int(Conv C)
	{
		return 1;
	}
	
    public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (5,34): error CS0553: 'Conv.implicit operator object(Conv)': user-defined conversions to or from a base class are not allowed
                // 	public static implicit operator object(Conv C)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "object").WithArguments("Conv.implicit operator object(Conv)").WithLocation(5, 34));
        }

        [Fact]
        public void CS0166_SwitchBlockDiagnosticsAreReported()
        {
            var text = @"
class C
{
    static void M(object o)
    {
        switch (o)
        {
            case F(null):
                M();
                break;
            case 0:
            case 0:
                break;
        }
    }
    static object F(int i)
    {
        return null;
    }
    static void Main() { }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,17): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch (o)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "o").WithLocation(6, 17),
                // (8,20): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //             case F(null):
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(8, 20),
                // (9,17): error CS7036: There is no argument given that corresponds to the required formal parameter 'o' of 'C.M(object)'
                //                 M();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("o", "C.M(object)").WithLocation(9, 17));
        }

        [Fact]
        public void CS0266_CaseLabelWithNoImplicitConversionToSwitchGoverningType()
        {
            var text = @"
public class Test
{
  public static int Main(string [] args)
  {
    int i = 5;

    switch (i)
    {
      case 1.2f:
        return 1;
    }
    return 0;
  }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,12): error CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //       case 1.2f:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.2f").WithArguments("float", "int").WithLocation(10, 12));
        }

        [Fact, WorkItem(546812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546812")]
        public void Bug16878()
        {
            var text = @"
class Program
{
    public static void Main()
    {
        int x = 0;
        switch(x)
        {
#pragma warning disable 6500
            case 0: break;
#pragma warning restore 6500
        }
    }
} 
";
            var comp = CompileAndVerify(text, expectedOutput: "");

            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MultipleLabesWithBadConstantValues()
        {
            var source = @"
class Program
{
    enum E 
    { 
        V1 = 1000, 
        V2 = 1001
    }

    public static void Main() { } 

    public static void Test(E x)
    {
        switch (x) 
        {
            case E.V1:
            case E.V2:
            default:
                break;
        }
    }
}";

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(source);

            // Intentionally not passing any references here to ensure System.Int32 is 
            // unresolvable and hence the constant values in the enum "E" will all be 
            // created as ConstantValue.Bad 
            var comp = CreateCompilation(new[] { syntaxTree }, references: null);
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot().DescendantNodes().First(x => x.IsKind(SyntaxKind.SimpleMemberAccessExpression));

            // Ensure the model can still bind without throwing when multiple labels values 
            // have duplicate constants (ConstantValue.Bad).  
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            Assert.NotNull(symbolInfo);
        }

        #endregion

        #region "Switch Governing Type with Implicit User Defined Conversion Tests"

        [Fact]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_01()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"using System;
public class Test
{
    public static implicit operator int(Test val)
    {
        return 1;
    }

    public static implicit operator float(Test val2)
    {
        return 2.1f;
    }

    public static int Main()
    {
        Test t = new Test();
        switch (t)
        {
            case 1:
                Console.WriteLine(0);
                return 0;
            default:
                Console.WriteLine(1);
                return 1;
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_02()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
class X {}
class Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
	public static implicit operator X (Conv C2)
	{
		return new X();
	}
	
	public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    case 1:
                System.Console.WriteLine(""Pass"");
                return 0;
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_03()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
enum X { F = 0 }
class Conv
{
	// only valid operator
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    // bool type is not valid
	public static implicit operator bool (Conv C2)
	{
		return false;
	}

    // enum type is not valid
    public static implicit operator X (Conv C3)
	{
		return X.F;
	}
	
	
	public static int Main()
	{
		Conv C = new Conv();
		switch(C)
		{
		    case 1:
                System.Console.WriteLine(""Pass"");
                return 0;
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_04()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator int? (Conv? C2)
	{
		return null;
	}
	
    public static int Main()
	{
		Conv? D = new Conv();
		switch(D)
		{
		    case 1:
                System.Console.WriteLine(""Fail"");
                return 1;
		    case null:
                System.Console.WriteLine(""Pass"");
                return 0;
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_05()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static int Main()
	{
		Conv? C = new Conv();
		switch(C)
		{
		    case 1:
                System.Console.WriteLine(""Pass"");
                return 0;
            case null:
                System.Console.WriteLine(""Fail"");
                return 1;
		    default:
                System.Console.WriteLine(""Fail"");
                return 1;
		}
	}		
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_06()
        {
            // Exactly ONE user-defined implicit conversion (6.4) must exist from the type of 
            // the switch expression to one of the following possible governing types: sbyte, byte, short,
            // ushort, int, uint, long, ulong, char, string. If no such implicit conversion exists, or if 
            // more than one such implicit conversion exists, a compile-time error occurs.

            var text = @"
struct Conv
{
	public static implicit operator int (Conv C)
	{
		return 1;
	}
	
    public static implicit operator int? (Conv? C)
	{
		return null;
	}
	
    public static int Main()
	{
		Conv? C = new Conv();
		switch(C)
		{
		    case null:
                System.Console.WriteLine(""Pass"");
                return 0;
		    case 1:
                System.Console.WriteLine(""Fail"");
                return 0;
		    default:
                System.Console.WriteLine(""Fail"");
                return 0;
		}
	}		
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_TypeParameter()
        {
            var text =
@"class A1
{
}
class A2
{
    public static implicit operator int(A2 a) { return 0; }
}
class B1<T> where T : A1
{
    internal T F() { return null; }
}
class B2<T> where T : A2
{
    internal T F() { return null; }
}
class C
{
    static void M<T>(B1<T> b1) where T : A1
    {
        switch (b1.F())
        {
            default:
                break;
        }
    }
    static void M<T>(B2<T> b2) where T : A2
    {
        switch (b2.F())
        {
            default:
                break;
        }
    }
}";
            // Note: Dev10 also reports CS0151 for "b2.F()", although
            // there is an implicit conversion from A2 to int.
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (20,17): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "b1.F()").WithLocation(20, 17));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_1()
        {
            // Dev10 behavior: 1st switch expression is an ambiguous user defined conversion
            // Dev10 behavior: 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a").WithLocation(28, 20));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_2()
        {
            // Dev10 behavior: 1st switch expression is an ambiguous user defined conversion
            // Dev10 behavior: 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (22,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "aNullable"),
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_3()
        {
            // 2nd switch expression is an ambiguous user defined conversion (both applicable in non-lifted form)

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_4()
        {
            // 2nd switch expression is an ambiguous user defined conversion (both applicable in non-lifted form)

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_5()
        {
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a").WithLocation(28, 20)
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_2_6()
        {
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a").WithLocation(28, 20)
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_3_1()
        {
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_3_2()
        {
            // 1st switch expression is an ambiguous user defined conversion
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "aNullable"),
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_3_3()
        {
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int(A a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_3_4()
        {
            // 1st switch expression is an ambiguous user defined conversion
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "aNullable"),
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_11564_4_1()
        {
            // 1st switch expression is an ambiguous user defined conversion
            // 2nd switch expression is an ambiguous user defined conversion

            var text =
@"using System;
 
struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
        return 0;
    }
 
    public static implicit operator int?(A? a)
    {
        Console.WriteLine(""1"");
        return 0;
    }
 
    public static implicit operator int(A? a)
    {
        Console.WriteLine(""2"");
        return 0;
    }
 
    public static implicit operator int(A a)
    {
        Console.WriteLine(""3"");
        return 0;
    }
 
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }

            A a = new A();
            switch(a)
            {
                default: break;
            }
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "aNullable"),
                // (40,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a"));
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact()]
        public void ImplicitNullableUserDefinedConversionToSwitchGoverningTypeString01()
        {
            var text =
@"using System;
 
struct A
{
    public static implicit operator string(A a1)
    {
        Console.WriteLine(nameof(a1));
        return nameof(a1);
    }
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "a1");
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact()]
        public void ImplicitNullableUserDefinedConversionToSwitchGoverningTypeString02()
        {
            var text =
@"using System;
 
struct A
{
    public static implicit operator string(A a1)
    {
        Console.WriteLine(nameof(a1));
        return nameof(a1);
    }
    public static implicit operator string(A? a2)
    {
        Console.WriteLine(nameof(a2));
        return nameof(a2);
    }
    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch(aNullable)
            {
                default: break;
            }
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "a2");
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact()]
        public void ImplicitNullableUserDefinedConversionToSwitchGoverningTypeInt()
        {
            var text =
@"using System;
class Program
{
  static void Main(string[] args)
  {
    M(default(X));
    M(null);
  }
  static void M(X? x)
  {
    switch (x)
    {
      case null:
        Console.WriteLine(""null"");
        break;
      case 1:
        Console.WriteLine(1);
        break;
    }
  }
}
struct X
{
    public static implicit operator int? (X x)
    {
        return 1;
    }
}";
            CompileAndVerify(text, expectedOutput:
@"1
null");
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact()]
        public void ImplicitUserDefinedConversionToSwitchGoverningType_42()
        {
            var text =
@"using System;

struct A
{
    public static implicit operator int?(A a)
    {
        Console.WriteLine(""0"");
    return 0;
    }

    public static implicit operator string (A? a)
    {
        Console.WriteLine(""1"");
        return """";
    }

    class B
    {
        static void Main()
        {
            A? aNullable = new A();
            switch (aNullable) // only A?->string is applicable in non-lifted form
            {
                case """":
                default: break;
            }

            A a = new A();
            switch (a) // both operators applicable in non-lifted form -> error
            {
                default: break;
            }
        }
    }
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (29,21): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch (a) // both operators applicable in non-lifted form -> error
                Diagnostic(ErrorCode.ERR_SwitchGoverningTypeValueExpected, "a").WithLocation(29, 21)
                );
        }

        #endregion

        #region "Control Flow analysis: CS8070 Switch fall out error tests"

        [Fact]
        public void CS8070_SwitchFallOut_DefaultLabel()
        {
            var text = @"using System;
class Test
{
	public static void DoTest(int i)
	{
		switch (i)
		{
			case 1:
				Console.WriteLine(i);
				break;
			case 2:
				Console.WriteLine(i);
				break;
            default:                        // CS8070
                Console.WriteLine(i);
		}
	}

	public static int Main()
    {
		return 1;
	}
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (14,13): error CS8070: Control cannot fall out of switch from final case label ('default')
                //             default:                        // CS8070
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(14, 13));
        }

        [Fact]
        public void CS8070_SwitchFallOutError()
        {
            var text = @"
namespace Test
{

    public class Program
    {
        static int Main()
        {
            int? i = 10;
            switch ((int)i)
            {
                case 10:
                    i++;
                    break;
                case 11:
                    ;
            }
            return -1;
        }
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (15,17): error CS8070: Control cannot fall out of switch from final case label ('case 11')
                //                 case 11:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 11:").WithArguments("case 11:").WithLocation(15, 17));
        }

        [Fact]
        public void CS8070_ErrorsInMultipleSwitchStmtsAreReported()
        {
            var text = @"
namespace Test
{

    public class Program
    {
        static int Main()
        {
            int? i = 10;
            switch ((int)i)
            {
                case 10:
                    i++;
                    break;
                case 11:
                    ;
            }

            int j = 5;
            goto LDone;
        LDone:
            switch (j)
            {
                case 5:
            }

            int? k = 10;
            switch ((int)k)
            {
                case 10:
                    break;
                default:
                    ;
            }

            return -1;
        }
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (15,17): error CS8070: Control cannot fall out of switch from final case label ('case 11:')
                //                 case 11:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 11:").WithArguments("case 11:").WithLocation(15, 17),
                // (24,17): error CS8070: Control cannot fall out of switch from final case label ('case 5:')
                //                 case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 5:").WithArguments("case 5:").WithLocation(24, 17),
                // (32,17): error CS8070: Control cannot fall out of switch from final case label ('default:')
                //                 default:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(32, 17));
        }

        [Fact]
        public void SwitchFallOut_Script()
        {
            var source =
@"using System;
switch (1)
{
    default:
        Console.WriteLine(1);
    case 2:
        Console.WriteLine(2);
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (4,5): error CS0163: Control cannot fall through from one case label ('default:') to another
                //     default:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "default:").WithArguments("default:").WithLocation(4, 5),
                // (6,5): error CS8070: Control cannot fall out of switch from final case label ('case 2:')
                //     case 2:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 2:").WithArguments("case 2:").WithLocation(6, 5));
        }

        [Fact]
        public void SwitchFallOut_Submission()
        {
            var source =
@"using System;
switch (1)
{
    case 1:
        Console.WriteLine(1);
    default:
        Console.WriteLine(2);
}";
            var submission = CSharpCompilation.CreateScriptCompilation(
                "s0.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Script),
                references: new[] { MscorlibRef, SystemCoreRef });
            submission.VerifyDiagnostics(
                // (4,5): error CS0163: Control cannot fall through from one case label ('case 1:') to another
                //     case 1:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 1:").WithArguments("case 1:").WithLocation(4, 5),
                // (6,5): error CS8070: Control cannot fall out of switch from final case label ('default:')
                //     default:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(6, 5));
        }

        #endregion
        # region "Control Flow analysis: CS0163 Switch fall through error tests"

        [Fact]
        public void CS0163_SwitchFallThroughError()
        {
            var text = @"using System;
class Test
{
	public static void DoTest(int i)
	{
		switch (i)
		{
			case 1:
				Console.WriteLine(i);
				break;
			case 2:                         // CS0163
				Console.WriteLine(i);
            default:
                Console.WriteLine(i);
				break;
		}
	}

	public static int Main()
    {
		return 1;
	}
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (11,4): error CS0163: Control cannot fall through from one case label ('case 2:') to another
                // 			case 2:                         // CS0163
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 2:").WithArguments("case 2:").WithLocation(11, 4));
        }

        [Fact]
        public void CS0163_ErrorsInMultipleSwitchStmtsAreReported()
        {
            var text = @"
namespace Test
{

    public class Program
    {
        static int Main()
        {
            int? i = 10;
            switch ((int)i)
            {
                case 10:
                    ;
                default:
                    break;
            }

            int j = 5;
            goto LDone;
        LDone:
            switch (j)
            {
                case 5:
                    ;
                case 7:
                    break;
            }

            int? k = 10;
            switch ((int)k)
            {
                case 10:
                    ;
                case 11:
                    break;
            }
            
            return -1;
        }
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (12,17): error CS0163: Control cannot fall through from one case label ('case 10:') to another
                //                 case 10:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 10:").WithArguments("case 10:").WithLocation(12, 17),
                // (23,17): error CS0163: Control cannot fall through from one case label ('case 5:') to another
                //                 case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 5:").WithArguments("case 5:").WithLocation(23, 17),
                // (32,17): error CS0163: Control cannot fall through from one case label ('case 10:') to another
                //                 case 10:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 10:").WithArguments("case 10:").WithLocation(32, 17));
        }

        #endregion

        #region "Data flow analysis: CS0165 Uninitialized variable error tests"

        [Fact]
        public void CS0165_SwitchScopeUnassignedVariable()
        {
            var text = @"
public class Foo
{
	public Foo() { i = 99; }
	public void Bar() { i = 0; }
	public int GetI() { return(i); }
	int i;
}

public class Test
{
    public static int Main(string [] args)
    {
		int s = 23;
		switch (s) {
		case 21:
			int j = 0;
			Foo f = new Foo();
			j++;
			break;
		case 23:
			int i = 22;
			j = i;
			f.Bar();        // unassigned variable f
			break;
		}
		return(1);
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (24,4): error CS0165: Use of unassigned local variable 'f'
                // 			f.Bar();        // unassigned variable f
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f").WithLocation(24, 4));
        }

        [Fact]
        public void CS0165_UnreachableCasesHaveAssignment()
        {
            var text = @"using System;
class SwitchTest
{
    public static int Main()
    {
        int foo;        // unassigned foo
        switch (3)
        {
            case 1:
                foo = 1;
                break;
            case 2:
                foo = 2;
                goto case 1;            
        }

        Console.WriteLine(foo);    // should output 0

        return 1;
    }
}";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,17): warning CS0162: Unreachable code detected
                //                 foo = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "foo").WithLocation(10, 17),
                // (13,17): warning CS0162: Unreachable code detected
                //                 foo = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "foo").WithLocation(13, 17),
                // (17,27): error CS0165: Use of unassigned local variable 'foo'
                //         Console.WriteLine(foo);    // should output 0
                Diagnostic(ErrorCode.ERR_UseDefViolation, "foo").WithArguments("foo").WithLocation(17, 27));
        }

        [Fact]
        public void CS0165_NoAssignmentOnOneControlPath()
        {
            var text = @"using System;
class SwitchTest
{
    public static int Main()
    {
        int i = 3;
        int foo;        // unassigned foo

        switch (i)
        {
            case 1:
                goto default;
              mylabel:
                try
                {
                    if (i > 0)
                    {
                        break;                  // foo is not definitely assigned here
                    }
                    throw new System.ApplicationException();                    
                }
                catch(Exception)
                {
                    foo = 1;
                    break;
                }
            case 2:
                goto mylabel;
            case 3:
                if (true)
                {
                    foo = 1;
                    goto case 2;
                }                
            default:
                foo = 1;
                break;
        }

        Console.WriteLine(foo);    // CS0165
        return foo;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (40,27): error CS0165: Use of unassigned local variable 'foo'
                //         Console.WriteLine(foo);    // CS0165
                Diagnostic(ErrorCode.ERR_UseDefViolation, "foo").WithArguments("foo").WithLocation(40, 27));
        }

        #endregion

        #region regressions

        [WorkItem(543849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543849")]
        [Fact()]
        public void NamespaceInCaseExpression()
        {
            var text =
@"class Test
{
    static void Main()
    {
        int x = 5;
        switch (x)
        {
            case System:
                break;
            case 5:
                goto System;
        }
    }
}";
            // CONSIDER:    Native compiler doesn't generate CS8070, we may want to do the same.

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,18): error CS0118: 'System' is a namespace but is used like a variable
                //             case System:
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable"),
                // (11,22): error CS0159: No such label 'System' within the scope of the goto statement
                //                 goto System;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "System").WithArguments("System"),
                // (10,13): error CS8070: Control cannot fall out of switch from final case label ('case 5:')
                //             case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 5:").WithArguments("case 5:"));
        }

        [Fact]
        public void SwitchOnBoolBeforeCSharp2()
        {
            var source = @"
class C
{
    void M(bool b)
    {
        switch(b)
        {
            default:
                break;
        }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp2)).VerifyDiagnostics();
            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp1)).VerifyDiagnostics(
                // (6,16): error CS8022: Feature 'switch on boolean type' is not available in C# 1.  Please use language version 2 or greater.
                //         switch(b)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "b").WithArguments("switch on boolean type", "2"));
        }

        #endregion
    }
}
