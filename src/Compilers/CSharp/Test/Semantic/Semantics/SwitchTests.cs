// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;

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
            CreateCompilation(source).VerifyDiagnostics();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (23,18): error CS0037: Cannot convert null to 'Test.eTypes' because it is a non-nullable value type
                //             case null:
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("Test.eTypes").WithLocation(23, 18)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (23,18): error CS0037: Cannot convert null to 'Test.eTypes' because it is a non-nullable value type
                //             case null:
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("Test.eTypes").WithLocation(23, 18)
                );
        }

        [WorkItem(542773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542773")]
        [Fact]
        public void CS0119_TypeUsedAsSwitchExpression()
        {
            var text = @"class A
{
    public static void Main()
    { }
    void goo(color color1)
    {
        switch (color)
        {
            default:
                break;
        }
    }
}
enum @color
{
    blue,
    green
}";

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (7,17): error CS0119: 'color' is a type, which is not valid in the given context
                //         switch (color)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "color").WithArguments("color", "type").WithLocation(7, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (7,17): error CS0119: 'color' is a type, which is not valid in the given context
                //         switch (color)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "color").WithArguments("color", "type").WithLocation(7, 17)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (11,18): error CS9135: A constant value of type 'int' is expected
                //             case test:
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "test").WithArguments("int").WithLocation(11, 18)
            );
            CreateCompilation(text).VerifyDiagnostics(
                // (11,18): error CS9135: A constant value of type 'int' is expected
                //             case test:
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "test").WithArguments("int").WithLocation(11, 18)
            );
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

    public void goo(char c)
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (11,13): error CS0152: The switch statement contains multiple cases with the label value '1'
                //             case 1: break;   // CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(11, 13),
                // (23,13): error CS0152: The switch statement contains multiple cases with the label value 'f'
                //             case 'f':       // CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 'f':").WithArguments("f").WithLocation(23, 13)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (11,13): error CS0152: The switch statement contains multiple cases with the label value '1'
                //             case 1: break;   // CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(11, 13),
                // (23,13): error CS0152: The switch statement contains multiple cases with the label value 'f'
                //             case 'f':       // CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 'f':").WithArguments("f").WithLocation(23, 13)
                );
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

    public void goo(int i)
    {
        switch (i)
        {
            case 'a':
                break;
            case 97:       // CS0152
                break;            
        }
    }

    public void goo2(char i)
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
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
                // (34,13): error CS0152: The switch statement contains multiple cases with the label value 'a'
                //             case 97.0f:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 97.0f:").WithArguments("a").WithLocation(34, 13),
                // (36,13): error CS0152: The switch statement contains multiple cases with the label value 'a'
                //             case 'a':
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 'a':").WithArguments("a").WithLocation(36, 13),
                // (38,18): error CS0266: Cannot implicitly convert type 'int' to 'char'. An explicit conversion exists (are you missing a cast?)
                //             case 97:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "97").WithArguments("int", "char").WithLocation(38, 18),
                // (38,13): error CS0152: The switch statement contains multiple cases with the label value 'a'
                //             case 97:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 97:").WithArguments("a").WithLocation(38, 13)
                );
            CreateCompilation(text).VerifyDiagnostics(
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
                // (34,13): error CS0152: The switch statement contains multiple cases with the label value 'a'
                //             case 97.0f:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 97.0f:").WithArguments("a").WithLocation(34, 13),
                // (36,13): error CS0152: The switch statement contains multiple cases with the label value 'a'
                //             case 'a':
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 'a':").WithArguments("a").WithLocation(36, 13),
                // (38,18): error CS0266: Cannot implicitly convert type 'int' to 'char'. An explicit conversion exists (are you missing a cast?)
                //             case 97:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "97").WithArguments("int", "char").WithLocation(38, 18),
                // (38,13): error CS0152: The switch statement contains multiple cases with the label value 'a'
                //             case 97:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 97:").WithArguments("a").WithLocation(38, 13)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (15,13): error CS0152: The switch statement contains multiple cases with the label value 'default'
                //             default:            //CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "default:").WithArguments("default").WithLocation(15, 13)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (15,13): error CS0152: The switch statement contains multiple cases with the label value 'default'
                //             default:            //CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "default:").WithArguments("default").WithLocation(15, 13)
                );
        }

        [Fact]
        public void CS0152_DuplicateDefaultLabel2()
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
            case (default):
                break;
            case 1:
                break;
            default:            //CS0152
                break;
        }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_1).VerifyDiagnostics(
                // (11,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //             case (default):
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(11, 19),
                // (15,13): error CS0152: The switch statement contains multiple cases with the label value 'default'
                //             default:            //CS0152
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "default:").WithArguments("default").WithLocation(15, 13)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,17): error CS0159: No such label 'case 5:' within the scope of the goto statement
                //                 goto case 5;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 5;").WithArguments("case 5:").WithLocation(10, 17),
                // (10,17): warning CS0162: Unreachable code detected
                //                 goto case 5;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goto").WithLocation(10, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (10,17): error CS0159: No such label 'case 5:' within the scope of the goto statement
                //                 goto case 5;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 5;").WithArguments("case 5:").WithLocation(10, 17),
                // (10,17): warning CS0162: Unreachable code detected
                //                 goto case 5;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goto").WithLocation(10, 17)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (9,17): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch (test) {
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "test").WithLocation(9, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found <null>.
                //         switch(null)
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "null").WithArguments("<null>").WithLocation(6, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found <null>.
                //         switch(null)
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "null").WithArguments("<null>").WithLocation(6, 16)
                );
        }

        [Fact]
        public void CS0166_InvalidSwitchExpression_Void()
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found void.
                //         switch(M())
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "M()").WithArguments("void").WithLocation(6, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found void.
                //         switch(M())
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "M()").WithArguments("void").WithLocation(6, 16)
                );
        }

        [Fact]
        public void CS0166_InvalidSwitchExpression_MethodGroup()
        {
            var text = @"
class T
{
    public static void Main()
    {
        switch(M)
        {
            default:
                break;
        }
    }

    public static void M() { }
}";

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found method group
                //         switch(M)
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "M").WithArguments("method group").WithLocation(6, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found method group.
                //         switch(M)
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "M").WithArguments("method group").WithLocation(6, 16)
                );
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found lambda expression
                //         switch(() => {})
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "() => {}").WithArguments("lambda expression").WithLocation(6, 16));
            CreateCompilation(text).VerifyDiagnostics(
                // (6,16): error CS8119: The switch expression must be a value; found lambda expression
                //         switch(() => {})
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "() => {}").WithArguments("lambda expression").WithLocation(6, 16));
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (17,16): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(17, 16));
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (17,16): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(17, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (17,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(17, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (17,16): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(17, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
            );
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (27,16): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(27, 16));
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (27,10): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                // 		switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(27, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (13,16): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(13, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [Fact]
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (14,16): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //         switch(C)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "C").WithLocation(14, 16)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (5,37): error CS0553: 'Conv.implicit operator object(Conv)': user-defined conversions to or from a base type are not allowed
                //     public static implicit operator object(Conv C)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "object").WithArguments("Conv.implicit operator object(Conv)").WithLocation(5, 37));
            CreateCompilation(text).VerifyDiagnostics(
                // (5,37): error CS0553: 'Conv.implicit operator object(Conv)': user-defined conversions to or from a base type are not allowed
                //     public static implicit operator object(Conv C)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "object").WithArguments("Conv.implicit operator object(Conv)").WithLocation(5, 37));
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
            case (1+(o.GetType().Name.Length)):
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

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,17): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type in C# 6 and earlier.
                //         switch (o)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "o").WithLocation(6, 17),
                // (8,19): error CS0150: A constant value is expected
                //             case (1+(o.GetType().Name.Length)):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1+(o.GetType().Name.Length)").WithLocation(8, 19),
                // (9,17): error CS7036: There is no argument given that corresponds to the required parameter 'o' of 'C.M(object)'
                //                 M();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("o", "C.M(object)").WithLocation(9, 17),
                // (12,13): error CS0152: The switch statement contains multiple cases with the label value '0'
                //             case 0:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 0:").WithArguments("0").WithLocation(12, 13)
            );
            CreateCompilation(text).VerifyDiagnostics(
                // (8,19): error CS0150: A constant value is expected
                //             case (1+(o.GetType().Name.Length)):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1+(o.GetType().Name.Length)").WithLocation(8, 19),
                // (9,17): error CS7036: There is no argument given that corresponds to the required parameter 'o' of 'C.M(object)'
                //                 M();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("o", "C.M(object)").WithLocation(9, 17),
                // (12,13): error CS0152: The switch statement contains multiple cases with the label value '0'
                //             case 0:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 0:").WithArguments("0").WithLocation(12, 13)
            );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,12): error CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //       case 1.2f:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.2f").WithArguments("float", "int").WithLocation(10, 12)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (10,12): error CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //       case 1.2f:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.2f").WithArguments("float", "int").WithLocation(10, 12)
                );
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
        public void MultipleLabelsWithBadConstantValues()
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
            var comp = CreateEmptyCompilation(new[] { syntaxTree }, references: null);
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot().DescendantNodes().First(x => x.IsKind(SyntaxKind.SimpleMemberAccessExpression));

            // Ensure the model can still bind without throwing when multiple labels values 
            // have duplicate constants (ConstantValue.Bad).  
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            Assert.NotEqual(default, symbolInfo);
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (20,17): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "b1.F()").WithLocation(20, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a").WithLocation(28, 20)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (22,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "aNullable"),
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a"));
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a")
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a")
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a").WithLocation(28, 20)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a").WithLocation(28, 20)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type in C# 6 and earlier.
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a").WithLocation(34, 20)
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "aNullable"),
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a")
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a")
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (28,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "aNullable"),
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a")
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(543673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543673")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (34,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(aNullable)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "aNullable"),
                // (40,20): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch(a)
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a")
                );
            CreateCompilation(text).VerifyDiagnostics(
                );
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact]
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
            CompileAndVerify(text, parseOptions: TestOptions.Regular6, expectedOutput: "a1");
            CompileAndVerify(text, expectedOutput: "a1");
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact]
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
            CompileAndVerify(text, parseOptions: TestOptions.Regular6, expectedOutput: "a2");
            CompileAndVerify(text, expectedOutput: "a2");
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact]
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
            CompileAndVerify(text, parseOptions: TestOptions.Regular6, expectedOutput:
@"1
null");
            CompileAndVerify(text, expectedOutput:
@"1
null");
        }

        [WorkItem(4344, "https://github.com/dotnet/roslyn/issues/4344")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (29,21): error CS0151: A switch expression or case label must be a bool, char, string, integral, enum, or corresponding nullable type
                //             switch (a) // both operators applicable in non-lifted form -> error
                Diagnostic(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, "a").WithLocation(29, 21)
                );
            CreateCompilation(text).VerifyDiagnostics(
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (14,13): error CS8070: Control cannot fall out of switch from final case label ('default:')
                //             default:                        // CS8070
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(14, 13)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (14,13): error CS8070: Control cannot fall out of switch from final case label ('default:')
                //             default:                        // CS8070
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(14, 13)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (15,17): error CS8070: Control cannot fall out of switch from final case label ('case 11')
                //                 case 11:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 11:").WithArguments("case 11:").WithLocation(15, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (15,17): error CS8070: Control cannot fall out of switch from final case label ('case 11')
                //                 case 11:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 11:").WithArguments("case 11:").WithLocation(15, 17)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (15,17): error CS8070: Control cannot fall out of switch from final case label ('case 11:')
                //                 case 11:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 11:").WithArguments("case 11:").WithLocation(15, 17),
                // (24,17): error CS8070: Control cannot fall out of switch from final case label ('case 5:')
                //                 case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 5:").WithArguments("case 5:").WithLocation(24, 17),
                // (32,17): error CS8070: Control cannot fall out of switch from final case label ('default:')
                //                 default:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(32, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (15,17): error CS8070: Control cannot fall out of switch from final case label ('case 11:')
                //                 case 11:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 11:").WithArguments("case 11:").WithLocation(15, 17),
                // (24,17): error CS8070: Control cannot fall out of switch from final case label ('case 5:')
                //                 case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 5:").WithArguments("case 5:").WithLocation(24, 17),
                // (32,17): error CS8070: Control cannot fall out of switch from final case label ('default:')
                //                 default:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "default:").WithArguments("default:").WithLocation(32, 17)
                );
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
            CreateCompilationWithMscorlib461(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (4,5): error CS0163: Control cannot fall through from one case label ('default:') to another
                //     default:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "default:").WithArguments("default:").WithLocation(4, 5),
                // (7,9): warning CS0162: Unreachable code detected
                //         Console.WriteLine(2);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(7, 9)
                );
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
                // (7,9): warning CS0162: Unreachable code detected
                //         Console.WriteLine(2);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(7, 9)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (11,13): error CS0163: Control cannot fall through from one case label ('case 2:') to another
                //             case 2:                         // CS0163
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 2:").WithArguments("case 2:").WithLocation(11, 13)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (11,13): error CS0163: Control cannot fall through from one case label ('case 2:') to another
                //             case 2:                         // CS0163
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 2:").WithArguments("case 2:").WithLocation(11, 13)
                );
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (12,17): error CS0163: Control cannot fall through from one case label ('case 10:') to another
                //                 case 10:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 10:").WithArguments("case 10:").WithLocation(12, 17),
                // (23,17): error CS0163: Control cannot fall through from one case label ('case 5:') to another
                //                 case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 5:").WithArguments("case 5:").WithLocation(23, 17),
                // (32,17): error CS0163: Control cannot fall through from one case label ('case 10:') to another
                //                 case 10:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 10:").WithArguments("case 10:").WithLocation(32, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (12,17): error CS0163: Control cannot fall through from one case label ('case 10:') to another
                //                 case 10:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 10:").WithArguments("case 10:").WithLocation(12, 17),
                // (23,17): error CS0163: Control cannot fall through from one case label ('case 5:') to another
                //                 case 5:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 5:").WithArguments("case 5:").WithLocation(23, 17),
                // (32,17): error CS0163: Control cannot fall through from one case label ('case 10:') to another
                //                 case 10:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 10:").WithArguments("case 10:").WithLocation(32, 17)
                );
        }

        #endregion

        #region "Data flow analysis: CS0165 Uninitialized variable error tests"

        [Fact]
        public void CS0165_SwitchScopeUnassignedVariable()
        {
            var text = @"
public class Goo
{
    public Goo() { i = 99; }
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
            Goo f = new Goo();
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (24,4): error CS0165: Use of unassigned local variable 'f'
                //            f.Bar();        // unassigned variable f
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f").WithLocation(24, 13));
            CreateCompilation(text).VerifyDiagnostics(
                // (24,4): error CS0165: Use of unassigned local variable 'f'
                //            f.Bar();        // unassigned variable f
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f").WithLocation(24, 13));
        }

        [Fact]
        public void CS0165_UnreachableCasesHaveAssignment()
        {
            var text = @"using System;
class SwitchTest
{
    public static int Main()
    {
        int goo;        // unassigned goo
        switch (3)
        {
            case 1:
                goo = 1;
                break;
            case 2:
                goo = 2;
                goto case 1;
        }

        Console.WriteLine(goo);

        return 1;
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,17): warning CS0162: Unreachable code detected
                //                 goo = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goo").WithLocation(10, 17),
                // (13,17): warning CS0162: Unreachable code detected
                //                 goo = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goo").WithLocation(13, 17),
                // (17,27): error CS0165: Use of unassigned local variable 'goo'
                //         Console.WriteLine(goo);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "goo").WithArguments("goo").WithLocation(17, 27));
            CreateCompilation(text).VerifyDiagnostics(
                // (10,17): warning CS0162: Unreachable code detected
                //                 goo = 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goo").WithLocation(10, 17),
                // (13,17): warning CS0162: Unreachable code detected
                //                 goo = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goo").WithLocation(13, 17),
                // (17,27): error CS0165: Use of unassigned local variable 'goo'
                //         Console.WriteLine(goo);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "goo").WithArguments("goo").WithLocation(17, 27));
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
        int goo;        // unassigned goo

        switch (i)
        {
            case 1:
                goto default;
              mylabel:
                try
                {
                    if (i > 0)
                    {
                        break; // goo is not definitely assigned here
                    }
                    throw new System.ApplicationException();
                }
                catch(Exception)
                {
                    goo = 1;
                    break;
                }
            case 2:
                goto mylabel;
            case 3:
                if (true)
                {
                    goo = 1;
                    goto case 2;
                }
            default:
                goo = 1;
                break;
        }

        Console.WriteLine(goo);    // CS0165
        return goo;
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (40,27): error CS0165: Use of unassigned local variable 'goo'
                //         Console.WriteLine(goo);    // CS0165
                Diagnostic(ErrorCode.ERR_UseDefViolation, "goo").WithArguments("goo").WithLocation(40, 27));
            CreateCompilation(text).VerifyDiagnostics(
                // (40,27): error CS0165: Use of unassigned local variable 'goo'
                //         Console.WriteLine(goo);    // CS0165
                Diagnostic(ErrorCode.ERR_UseDefViolation, "goo").WithArguments("goo").WithLocation(40, 27));
        }

        [Fact, WorkItem(32806, "https://github.com/dotnet/roslyn/issues/32806")]
        public void TraditionalSwitchIsIncomplete_01()
        {
            var text = @"
class SwitchTest
{
    static int Main(string[] args)
    {
        bool? test = null;

        switch (test)
        {
            case true:
                return 1;
            case false:
                return 0;
            case null:
                return -1;
        }
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (4,16): error CS0161: 'SwitchTest.Main(string[])': not all code paths return a value
                //     static int Main(string[] args)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("SwitchTest.Main(string[])").WithLocation(4, 16)
                );
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (4,16): error CS0161: 'SwitchTest.Main(string[])': not all code paths return a value
                //     static int Main(string[] args)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("SwitchTest.Main(string[])").WithLocation(4, 16)
                );
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact, WorkItem(32806, "https://github.com/dotnet/roslyn/issues/32806")]
        public void TraditionalSwitchIsIncomplete_02()
        {
            var text = @"
class SwitchTest
{
    static int Main(string[] args)
    {
        bool? test = null;

        switch (test)
        {
            case true when true:
                return 1;
            case false:
                return 0;
            case null:
                return -1;
        }
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case true when true:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case").WithArguments("pattern matching", "7.0").WithLocation(10, 13));
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics();
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        #endregion

        #region regressions

        [WorkItem(543849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543849")]
        [Fact]
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
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (8,18): error CS0118: 'System' is a namespace but is used like a variable
                //             case System:
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable").WithLocation(8, 18),
                // (11,22): error CS0159: No such label 'System' within the scope of the goto statement
                //                 goto System;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "System").WithArguments("System").WithLocation(11, 22)
                // No cascaded ERR_SwitchFallOut error - the goto makes the end of the switch case unreachable
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (8,18): error CS0118: 'System' is a namespace but is used like a variable
                //             case System:
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable").WithLocation(8, 18),
                // (11,22): error CS0159: No such label 'System' within the scope of the goto statement
                //                 goto System;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "System").WithArguments("System").WithLocation(11, 22)
                // No cascaded ERR_SwitchFallOut error - the goto makes the end of the switch case unreachable
                );
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

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp2)).VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp1)).VerifyDiagnostics(
                // (6,16): error CS8022: Feature 'switch on boolean type' is not available in C# 1. Please use language version 2 or greater.
                //         switch(b)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "b").WithArguments("switch on boolean type", "2"));
        }

        [Fact]
        public void SmallUnsignedEdgeCase01()
        {
            var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        RunTest(126);
        RunTest(127);
        RunTest(128);
        RunTest(129);

        void RunTest(byte testByte)
        {
            switch (testByte)
            {
                case 127: // 0111 1111
                case 128: // 1000 0000
                    Console.Write(0);
                    break;
                default:
                    Console.Write(1);
                    break;
            }
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"1001");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SmallUnsignedEdgeCase02()
        {
            var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        RunTest(32766);
        RunTest(32767);
        RunTest(32768);
        RunTest(32769);

        void RunTest(ushort testUshort)
        {
            switch (testUshort)
            {
                case 32767: // 0111 1111 1111 1111
                case 32768: // 1000 0000 0000 0000
                    Console.Write(0);
                    break;
                default:
                    Console.Write(1);
                    break;
            }
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"1001");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConstantNullSwitchExpression()
        {
            var text = @"
public class TestClass
{
    public static void Main()
    {
        const string s = null;
        switch (s)
        {
            default:
                break; //1
            case null:
                break; //2
        }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,17): warning CS0162: Unreachable code detected
                //                 break; //1
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
            CreateCompilation(text).VerifyDiagnostics(
                // (10,17): warning CS0162: Unreachable code detected
                //                 break; //1
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
        }

        [Fact]
        [WorkItem(33783, "https://github.com/dotnet/roslyn/issues/33783")]
        public void UnreachableDefaultInBoolSwitch()
        {
            var text = @"
public class TestClass
{
    public static void Main()
    {
        bool b = false;
        switch (b)
        {
            case true:
                break;
            case false:
                break;
            default:
                break; //1
        }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics();
            CreateCompilation(text, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics();
            CreateCompilation(text).VerifyDiagnostics(
                // (14,17): warning CS0162: Unreachable code detected
                //                 break; //1
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(14, 17));
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void Bug47878()
        {
            var text = @"
using System;
public class C
{
    public static void Main()
    {
        int x1 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) => 1,
            _ => -1,
        };
        int x2 = (1, 2, 3, 4, 5, 6, 7, 8) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8) => 1,
            _ => -1,
        };
        int x3 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16) => 1,
            _ => -1,
        };
        int x4 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19) => 1,
            _ => -1,
        };
        Console.WriteLine($""{x1} {x2} {x3} {x4}"");
    }
}
";
            var comp = CompileAndVerify(text, expectedOutput: "1 1 1 1");
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void VerifyIL_8ElementsTuple()
        {
            var text = @"
using System;
public class C
{
    public static void Main()
    {
        int x = (1, 2, 3, 4, 5, 6, 7, 8) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8) => 1,
            _ => -1,
        };

        Console.WriteLine(x);
    }
}
";
            CompileAndVerify(text, expectedOutput: "1").VerifyIL("C.Main", @"
{
  // Code size      110 (0x6e)
  .maxstack  9
  .locals init (int V_0,
                System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>> V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  ldc.i4.5
  IL_0007:  ldc.i4.6
  IL_0008:  ldc.i4.7
  IL_0009:  ldc.i4.8
  IL_000a:  newobj     ""System.ValueTuple<int>..ctor(int)""
  IL_000f:  call       ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int>)""
  IL_0014:  ldloc.1
  IL_0015:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item1""
  IL_001a:  ldc.i4.1
  IL_001b:  bne.un.s   IL_0065
  IL_001d:  ldloc.1
  IL_001e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item2""
  IL_0023:  ldc.i4.2
  IL_0024:  bne.un.s   IL_0065
  IL_0026:  ldloc.1
  IL_0027:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item3""
  IL_002c:  ldc.i4.3
  IL_002d:  bne.un.s   IL_0065
  IL_002f:  ldloc.1
  IL_0030:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item4""
  IL_0035:  ldc.i4.4
  IL_0036:  bne.un.s   IL_0065
  IL_0038:  ldloc.1
  IL_0039:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item5""
  IL_003e:  ldc.i4.5
  IL_003f:  bne.un.s   IL_0065
  IL_0041:  ldloc.1
  IL_0042:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item6""
  IL_0047:  ldc.i4.6
  IL_0048:  bne.un.s   IL_0065
  IL_004a:  ldloc.1
  IL_004b:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item7""
  IL_0050:  ldc.i4.7
  IL_0051:  bne.un.s   IL_0065
  IL_0053:  ldloc.1
  IL_0054:  ldfld      ""System.ValueTuple<int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Rest""
  IL_0059:  ldfld      ""int System.ValueTuple<int>.Item1""
  IL_005e:  ldc.i4.8
  IL_005f:  bne.un.s   IL_0065
  IL_0061:  ldc.i4.1
  IL_0062:  stloc.0
  IL_0063:  br.s       IL_0067
  IL_0065:  ldc.i4.m1
  IL_0066:  stloc.0
  IL_0067:  ldloc.0
  IL_0068:  call       ""void System.Console.WriteLine(int)""
  IL_006d:  ret
}
");
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void VerifyIL_GreaterThan8ElementsTuple_01()
        {
            var text = @"
using System;
public class C
{
    public static void Main()
    {
        int x = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13) => 1,
            _ => -1,
        };

        Console.WriteLine(x);
    }
}
";
            CompileAndVerify(text, expectedOutput: "1").VerifyIL("C.Main", @"
{
  // Code size      204 (0xcc)
  .maxstack  14
  .locals init (int V_0,
                System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  ldc.i4.5
  IL_0007:  ldc.i4.6
  IL_0008:  ldc.i4.7
  IL_0009:  ldc.i4.8
  IL_000a:  ldc.i4.s   9
  IL_000c:  ldc.i4.s   10
  IL_000e:  ldc.i4.s   11
  IL_0010:  ldc.i4.s   12
  IL_0012:  ldc.i4.s   13
  IL_0014:  newobj     ""System.ValueTuple<int, int, int, int, int, int>..ctor(int, int, int, int, int, int)""
  IL_0019:  call       ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>)""
  IL_001e:  ldloc.1
  IL_001f:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item1""
  IL_0024:  ldc.i4.1
  IL_0025:  bne.un     IL_00c3
  IL_002a:  ldloc.1
  IL_002b:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item2""
  IL_0030:  ldc.i4.2
  IL_0031:  bne.un     IL_00c3
  IL_0036:  ldloc.1
  IL_0037:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item3""
  IL_003c:  ldc.i4.3
  IL_003d:  bne.un     IL_00c3
  IL_0042:  ldloc.1
  IL_0043:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item4""
  IL_0048:  ldc.i4.4
  IL_0049:  bne.un.s   IL_00c3
  IL_004b:  ldloc.1
  IL_004c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item5""
  IL_0051:  ldc.i4.5
  IL_0052:  bne.un.s   IL_00c3
  IL_0054:  ldloc.1
  IL_0055:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item6""
  IL_005a:  ldc.i4.6
  IL_005b:  bne.un.s   IL_00c3
  IL_005d:  ldloc.1
  IL_005e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item7""
  IL_0063:  ldc.i4.7
  IL_0064:  bne.un.s   IL_00c3
  IL_0066:  ldloc.1
  IL_0067:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_006c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item1""
  IL_0071:  ldc.i4.8
  IL_0072:  bne.un.s   IL_00c3
  IL_0074:  ldloc.1
  IL_0075:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_007a:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item2""
  IL_007f:  ldc.i4.s   9
  IL_0081:  bne.un.s   IL_00c3
  IL_0083:  ldloc.1
  IL_0084:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_0089:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item3""
  IL_008e:  ldc.i4.s   10
  IL_0090:  bne.un.s   IL_00c3
  IL_0092:  ldloc.1
  IL_0093:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_0098:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item4""
  IL_009d:  ldc.i4.s   11
  IL_009f:  bne.un.s   IL_00c3
  IL_00a1:  ldloc.1
  IL_00a2:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_00a7:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item5""
  IL_00ac:  ldc.i4.s   12
  IL_00ae:  bne.un.s   IL_00c3
  IL_00b0:  ldloc.1
  IL_00b1:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_00b6:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item6""
  IL_00bb:  ldc.i4.s   13
  IL_00bd:  bne.un.s   IL_00c3
  IL_00bf:  ldc.i4.1
  IL_00c0:  stloc.0
  IL_00c1:  br.s       IL_00c5
  IL_00c3:  ldc.i4.m1
  IL_00c4:  stloc.0
  IL_00c5:  ldloc.0
  IL_00c6:  call       ""void System.Console.WriteLine(int)""
  IL_00cb:  ret
}
");
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void VerifyIL_GreaterThan8ElementsTuple_02()
        {
            var text = @"
using System;
public class C
{
    public static void Main()
    {
        int x = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13) switch
        {
            (1, 2, 3, 4, 5, 6, not 7, 8, 9, 10, 11, 12, 13) => 1,
            _ => -1,
        };

        Console.WriteLine(x);
    }
}
";
            CompileAndVerify(text, expectedOutput: "-1").VerifyIL("C.Main", @"

{
  // Code size      204 (0xcc)
  .maxstack  14
  .locals init (int V_0,
                System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  ldc.i4.5
  IL_0007:  ldc.i4.6
  IL_0008:  ldc.i4.7
  IL_0009:  ldc.i4.8
  IL_000a:  ldc.i4.s   9
  IL_000c:  ldc.i4.s   10
  IL_000e:  ldc.i4.s   11
  IL_0010:  ldc.i4.s   12
  IL_0012:  ldc.i4.s   13
  IL_0014:  newobj     ""System.ValueTuple<int, int, int, int, int, int>..ctor(int, int, int, int, int, int)""
  IL_0019:  call       ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>)""
  IL_001e:  ldloc.1
  IL_001f:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item1""
  IL_0024:  ldc.i4.1
  IL_0025:  bne.un     IL_00c3
  IL_002a:  ldloc.1
  IL_002b:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item2""
  IL_0030:  ldc.i4.2
  IL_0031:  bne.un     IL_00c3
  IL_0036:  ldloc.1
  IL_0037:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item3""
  IL_003c:  ldc.i4.3
  IL_003d:  bne.un     IL_00c3
  IL_0042:  ldloc.1
  IL_0043:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item4""
  IL_0048:  ldc.i4.4
  IL_0049:  bne.un.s   IL_00c3
  IL_004b:  ldloc.1
  IL_004c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item5""
  IL_0051:  ldc.i4.5
  IL_0052:  bne.un.s   IL_00c3
  IL_0054:  ldloc.1
  IL_0055:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item6""
  IL_005a:  ldc.i4.6
  IL_005b:  bne.un.s   IL_00c3
  IL_005d:  ldloc.1
  IL_005e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item7""
  IL_0063:  ldc.i4.7
  IL_0064:  beq.s      IL_00c3
  IL_0066:  ldloc.1
  IL_0067:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_006c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item1""
  IL_0071:  ldc.i4.8
  IL_0072:  bne.un.s   IL_00c3
  IL_0074:  ldloc.1
  IL_0075:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_007a:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item2""
  IL_007f:  ldc.i4.s   9
  IL_0081:  bne.un.s   IL_00c3
  IL_0083:  ldloc.1
  IL_0084:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_0089:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item3""
  IL_008e:  ldc.i4.s   10
  IL_0090:  bne.un.s   IL_00c3
  IL_0092:  ldloc.1
  IL_0093:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_0098:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item4""
  IL_009d:  ldc.i4.s   11
  IL_009f:  bne.un.s   IL_00c3
  IL_00a1:  ldloc.1
  IL_00a2:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_00a7:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item5""
  IL_00ac:  ldc.i4.s   12
  IL_00ae:  bne.un.s   IL_00c3
  IL_00b0:  ldloc.1
  IL_00b1:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_00b6:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item6""
  IL_00bb:  ldc.i4.s   13
  IL_00bd:  bne.un.s   IL_00c3
  IL_00bf:  ldc.i4.1
  IL_00c0:  stloc.0
  IL_00c1:  br.s       IL_00c5
  IL_00c3:  ldc.i4.m1
  IL_00c4:  stloc.0
  IL_00c5:  ldloc.0
  IL_00c6:  call       ""void System.Console.WriteLine(int)""
  IL_00cb:  ret
}
");
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void VerifyIL_GreaterThan8ElementsTuple_03()
        {
            var text = @"
using System;
public class C
{
    public static void Main()
    {
        int x = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20) switch
        {
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20) => 1,
            _ => -1,
        };

        Console.WriteLine(x);
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size      388 (0x184)
  .maxstack  21
  .locals init (int V_0,
                System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>> V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  ldc.i4.5
  IL_0007:  ldc.i4.6
  IL_0008:  ldc.i4.7
  IL_0009:  ldc.i4.8
  IL_000a:  ldc.i4.s   9
  IL_000c:  ldc.i4.s   10
  IL_000e:  ldc.i4.s   11
  IL_0010:  ldc.i4.s   12
  IL_0012:  ldc.i4.s   13
  IL_0014:  ldc.i4.s   14
  IL_0016:  ldc.i4.s   15
  IL_0018:  ldc.i4.s   16
  IL_001a:  ldc.i4.s   17
  IL_001c:  ldc.i4.s   18
  IL_001e:  ldc.i4.s   19
  IL_0020:  ldc.i4.s   20
  IL_0022:  newobj     ""System.ValueTuple<int, int, int, int, int, int>..ctor(int, int, int, int, int, int)""
  IL_0027:  newobj     ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>)""
  IL_002c:  call       ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>)""
  IL_0031:  ldloc.1
  IL_0032:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item1""
  IL_0037:  ldc.i4.1
  IL_0038:  bne.un     IL_017b
  IL_003d:  ldloc.1
  IL_003e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item2""
  IL_0043:  ldc.i4.2
  IL_0044:  bne.un     IL_017b
  IL_0049:  ldloc.1
  IL_004a:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item3""
  IL_004f:  ldc.i4.3
  IL_0050:  bne.un     IL_017b
  IL_0055:  ldloc.1
  IL_0056:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item4""
  IL_005b:  ldc.i4.4
  IL_005c:  bne.un     IL_017b
  IL_0061:  ldloc.1
  IL_0062:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item5""
  IL_0067:  ldc.i4.5
  IL_0068:  bne.un     IL_017b
  IL_006d:  ldloc.1
  IL_006e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item6""
  IL_0073:  ldc.i4.6
  IL_0074:  bne.un     IL_017b
  IL_0079:  ldloc.1
  IL_007a:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Item7""
  IL_007f:  ldc.i4.7
  IL_0080:  bne.un     IL_017b
  IL_0085:  ldloc.1
  IL_0086:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_008b:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item1""
  IL_0090:  ldc.i4.8
  IL_0091:  bne.un     IL_017b
  IL_0096:  ldloc.1
  IL_0097:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_009c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item2""
  IL_00a1:  ldc.i4.s   9
  IL_00a3:  bne.un     IL_017b
  IL_00a8:  ldloc.1
  IL_00a9:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_00ae:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item3""
  IL_00b3:  ldc.i4.s   10
  IL_00b5:  bne.un     IL_017b
  IL_00ba:  ldloc.1
  IL_00bb:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_00c0:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item4""
  IL_00c5:  ldc.i4.s   11
  IL_00c7:  bne.un     IL_017b
  IL_00cc:  ldloc.1
  IL_00cd:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_00d2:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item5""
  IL_00d7:  ldc.i4.s   12
  IL_00d9:  bne.un     IL_017b
  IL_00de:  ldloc.1
  IL_00df:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_00e4:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item6""
  IL_00e9:  ldc.i4.s   13
  IL_00eb:  bne.un     IL_017b
  IL_00f0:  ldloc.1
  IL_00f1:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_00f6:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Item7""
  IL_00fb:  ldc.i4.s   14
  IL_00fd:  bne.un.s   IL_017b
  IL_00ff:  ldloc.1
  IL_0100:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_0105:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_010a:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item1""
  IL_010f:  ldc.i4.s   15
  IL_0111:  bne.un.s   IL_017b
  IL_0113:  ldloc.1
  IL_0114:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_0119:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_011e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item2""
  IL_0123:  ldc.i4.s   16
  IL_0125:  bne.un.s   IL_017b
  IL_0127:  ldloc.1
  IL_0128:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_012d:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_0132:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item3""
  IL_0137:  ldc.i4.s   17
  IL_0139:  bne.un.s   IL_017b
  IL_013b:  ldloc.1
  IL_013c:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_0141:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_0146:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item4""
  IL_014b:  ldc.i4.s   18
  IL_014d:  bne.un.s   IL_017b
  IL_014f:  ldloc.1
  IL_0150:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_0155:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_015a:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item5""
  IL_015f:  ldc.i4.s   19
  IL_0161:  bne.un.s   IL_017b
  IL_0163:  ldloc.1
  IL_0164:  ldfld      ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>>.Rest""
  IL_0169:  ldfld      ""System.ValueTuple<int, int, int, int, int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int, int, int, int, int>>.Rest""
  IL_016e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int>.Item6""
  IL_0173:  ldc.i4.s   20
  IL_0175:  bne.un.s   IL_017b
  IL_0177:  ldc.i4.1
  IL_0178:  stloc.0
  IL_0179:  br.s       IL_017d
  IL_017b:  ldc.i4.m1
  IL_017c:  stloc.0
  IL_017d:  ldloc.0
  IL_017e:  call       ""void System.Console.WriteLine(int)""
  IL_0183:  ret
}
");
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void VerifyIL_8ElementsTuple_SideEffects_DefaultLabel()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;
public class C
{
    public static void Main()
    {
        try
        {
            int x = (M(1), M(2), M(3), M(4), M(5), M(6), M(7), M(8)) switch
            {
                (1, 2, 3, 4, 5, 6, 7, 7) => 1,
            };

            Console.WriteLine(x);
        }
        catch (SwitchExpressionException ex)
        {
            Console.Write(""💥"");
        }
    }

    static int M(int x)
    {
        Console.Write(x);
        return x;
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() {}
        public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            CompileAndVerify(text, expectedOutput: "12345678💥").VerifyIL("C.Main", @"
{
  // Code size      173 (0xad)
  .maxstack  8
  .locals init (int V_0,
                System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>> V_1)
  .try
  {
    IL_0000:  ldc.i4.1
    IL_0001:  call       ""int C.M(int)""
    IL_0006:  ldc.i4.2
    IL_0007:  call       ""int C.M(int)""
    IL_000c:  ldc.i4.3
    IL_000d:  call       ""int C.M(int)""
    IL_0012:  ldc.i4.4
    IL_0013:  call       ""int C.M(int)""
    IL_0018:  ldc.i4.5
    IL_0019:  call       ""int C.M(int)""
    IL_001e:  ldc.i4.6
    IL_001f:  call       ""int C.M(int)""
    IL_0024:  ldc.i4.7
    IL_0025:  call       ""int C.M(int)""
    IL_002a:  ldc.i4.8
    IL_002b:  call       ""int C.M(int)""
    IL_0030:  newobj     ""System.ValueTuple<int>..ctor(int)""
    IL_0035:  newobj     ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int>)""
    IL_003a:  stloc.1
    IL_003b:  ldloc.1
    IL_003c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item1""
    IL_0041:  ldc.i4.1
    IL_0042:  bne.un.s   IL_008c
    IL_0044:  ldloc.1
    IL_0045:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item2""
    IL_004a:  ldc.i4.2
    IL_004b:  bne.un.s   IL_008c
    IL_004d:  ldloc.1
    IL_004e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item3""
    IL_0053:  ldc.i4.3
    IL_0054:  bne.un.s   IL_008c
    IL_0056:  ldloc.1
    IL_0057:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item4""
    IL_005c:  ldc.i4.4
    IL_005d:  bne.un.s   IL_008c
    IL_005f:  ldloc.1
    IL_0060:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item5""
    IL_0065:  ldc.i4.5
    IL_0066:  bne.un.s   IL_008c
    IL_0068:  ldloc.1
    IL_0069:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item6""
    IL_006e:  ldc.i4.6
    IL_006f:  bne.un.s   IL_008c
    IL_0071:  ldloc.1
    IL_0072:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item7""
    IL_0077:  ldc.i4.7
    IL_0078:  bne.un.s   IL_008c
    IL_007a:  ldloc.1
    IL_007b:  ldfld      ""System.ValueTuple<int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Rest""
    IL_0080:  ldfld      ""int System.ValueTuple<int>.Item1""
    IL_0085:  ldc.i4.7
    IL_0086:  bne.un.s   IL_008c
    IL_0088:  ldc.i4.1
    IL_0089:  stloc.0
    IL_008a:  br.s       IL_0097
    IL_008c:  ldloc.1
    IL_008d:  box        ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>""
    IL_0092:  call       ""void <PrivateImplementationDetails>.ThrowSwitchExpressionException(object)""
    IL_0097:  ldloc.0
    IL_0098:  call       ""void System.Console.WriteLine(int)""
    IL_009d:  leave.s    IL_00ac
  }
  catch System.Runtime.CompilerServices.SwitchExpressionException
  {
    IL_009f:  pop
    IL_00a0:  ldstr      ""💥""
    IL_00a5:  call       ""void System.Console.Write(string)""
    IL_00aa:  leave.s    IL_00ac
  }
  IL_00ac:  ret
}
");
        }

        [Fact, WorkItem(47878, "https://github.com/dotnet/roslyn/issues/47878")]
        public void VerifyIL_9ElementsTuple_SideEffects_DefaultLabel()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;
public class C
{
    public static void Main()
    {
        try
        {
            int x = (M(1), M(2), M(3), M(4), M(5), M(6), M(7), M(8), M(9)) switch
            {
                (1, 2, 3, 4, 5, 6, 7, 8, 8) => 1,
            };

            Console.WriteLine(x);
        }
        catch (SwitchExpressionException ex)
        {
            Console.Write(""💥"");
        }
    }

    static int M(int x)
    {
        Console.Write(x);
        return x;
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() {}
        public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            CompileAndVerify(text, expectedOutput: "123456789💥").VerifyIL("C.Main", @"
{
  // Code size      194 (0xc2)
  .maxstack  9
  .locals init (int V_0,
                System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>> V_1)
  .try
  {
    IL_0000:  ldc.i4.1
    IL_0001:  call       ""int C.M(int)""
    IL_0006:  ldc.i4.2
    IL_0007:  call       ""int C.M(int)""
    IL_000c:  ldc.i4.3
    IL_000d:  call       ""int C.M(int)""
    IL_0012:  ldc.i4.4
    IL_0013:  call       ""int C.M(int)""
    IL_0018:  ldc.i4.5
    IL_0019:  call       ""int C.M(int)""
    IL_001e:  ldc.i4.6
    IL_001f:  call       ""int C.M(int)""
    IL_0024:  ldc.i4.7
    IL_0025:  call       ""int C.M(int)""
    IL_002a:  ldc.i4.8
    IL_002b:  call       ""int C.M(int)""
    IL_0030:  ldc.i4.s   9
    IL_0032:  call       ""int C.M(int)""
    IL_0037:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
    IL_003c:  newobj     ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>..ctor(int, int, int, int, int, int, int, System.ValueTuple<int, int>)""
    IL_0041:  stloc.1
    IL_0042:  ldloc.1
    IL_0043:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item1""
    IL_0048:  ldc.i4.1
    IL_0049:  bne.un.s   IL_00a1
    IL_004b:  ldloc.1
    IL_004c:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item2""
    IL_0051:  ldc.i4.2
    IL_0052:  bne.un.s   IL_00a1
    IL_0054:  ldloc.1
    IL_0055:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item3""
    IL_005a:  ldc.i4.3
    IL_005b:  bne.un.s   IL_00a1
    IL_005d:  ldloc.1
    IL_005e:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item4""
    IL_0063:  ldc.i4.4
    IL_0064:  bne.un.s   IL_00a1
    IL_0066:  ldloc.1
    IL_0067:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item5""
    IL_006c:  ldc.i4.5
    IL_006d:  bne.un.s   IL_00a1
    IL_006f:  ldloc.1
    IL_0070:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item6""
    IL_0075:  ldc.i4.6
    IL_0076:  bne.un.s   IL_00a1
    IL_0078:  ldloc.1
    IL_0079:  ldfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Item7""
    IL_007e:  ldc.i4.7
    IL_007f:  bne.un.s   IL_00a1
    IL_0081:  ldloc.1
    IL_0082:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Rest""
    IL_0087:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_008c:  ldc.i4.8
    IL_008d:  bne.un.s   IL_00a1
    IL_008f:  ldloc.1
    IL_0090:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>.Rest""
    IL_0095:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_009a:  ldc.i4.8
    IL_009b:  bne.un.s   IL_00a1
    IL_009d:  ldc.i4.1
    IL_009e:  stloc.0
    IL_009f:  br.s       IL_00ac
    IL_00a1:  ldloc.1
    IL_00a2:  box        ""System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int, int>>""
    IL_00a7:  call       ""void <PrivateImplementationDetails>.ThrowSwitchExpressionException(object)""
    IL_00ac:  ldloc.0
    IL_00ad:  call       ""void System.Console.WriteLine(int)""
    IL_00b2:  leave.s    IL_00c1
  }
  catch System.Runtime.CompilerServices.SwitchExpressionException
  {
    IL_00b4:  pop
    IL_00b5:  ldstr      ""💥""
    IL_00ba:  call       ""void System.Console.Write(string)""
    IL_00bf:  leave.s    IL_00c1
  }
  IL_00c1:  ret
}
");
        }
        #endregion
    }
}
