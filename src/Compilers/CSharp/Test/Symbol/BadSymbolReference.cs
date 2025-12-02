// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class BadSymbolReference : CSharpTestBase
    {
        //CreateCompilationWithMscorlib(text).VerifyDiagnostics(
        //    // (6,17): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
        //    Diagnostic(ErrorCode.ERR_BadUnaryOp, @"null.Length").WithArguments(".", "<null>"));

        [Fact]
        public void MissingTypes1()
        {
            var cl2 = TestReferences.SymbolsTests.MissingTypes.CL2;
            var cl3 = TestReferences.SymbolsTests.MissingTypes.CL3;

            var compilation1 = CreateCompilation(
@"
class Module1
{
    void Main()
    {
        CL3_C1 x1;

        x1 = null;
    }

}", new MetadataReference[] { cl2, cl3 });

            var a_cs =
@"

class Module1
{
    private CL3_C1 f1;

    void Main()
    {
        CL3_C1 x1;
        x1 = null;
    }

    void Test1()
    {
        CL3_C3 x2;
        x2 = null;
    }

    void Test2()
    {
        System.Action<CL3_C3> x3;
        x3 = null;
    }

    void Test3()
    {
        CL3_C1.Test1();
    }

    void Test4()
    {
        global::CL3_C1.Test1();
    }

    void Test5()
    {
        C1<CL3_C1>.Test1();
    }

    void Test6()
    {
        global::C1<CL3_C1>.Test1();
    }

    void Test7()
    {
        object x1;
        x1 = new CL3_C1();
    }

    void Test8()
    {
        CL3_C3[] x4;
        x4 = null;
    }

    void Test9()
    {
        object x4;
        x4 = new CL3_C3[] {};
    }

    void Test10()
    {
        C1<CL3_C1> x5;
        x5 = null;
    }

    void Test11()
    {
        object x5;
        x5 = new C1<CL3_C1>();
    }

    void Test()
    {
        var v = new CL3_C4();
    }

    void Test12()
    {
        var w = new CL3_C5();
    }

    void Test13()
    {
        CL3_C2 y = null;
        object z = y.x;
    }

    void CSharp15()
    {
        CL3_C2 y = null;
        object z;
        z = y.u;
    }

    void Test16()
    {
        CL3_C2 y = null;
        object z;
        z = y.y;
    }

    void Test17()
    {
        CL3_C2 y = null;
        object z;
        z = y.z;
    }

    void Test18()
    {
        CL3_C2 y=null;
        object z;
        z = y.v;
    }

    void Test19()
    {
        object z;
        z = f1;
    }

    class C2 : CL3_C1
    {
    }

    class C3 : System.Collections.Generic.List<CL3_C1>
    {
    }

    class C4 : CL3_S1
    {
    }

    interface I2 : CL3_I1, I1<CL3_I1>
    {}

    class C5 : CL3_I1, I1<CL3_I1>
    {}

    void Test20()
    {
        CL3_S1? x6;
        x6 = null;
    }

    void Test21()
    {
        CL3_C2.Test1();
    }

    void Test22()
    {
        CL3_C2.Test1(1);
    }

    void Test23()
    {
        CL3_C2.Test3();
    }

    void Test24()
    {
        CL3_C2.Test4();
    }

    void Test24_1()
    {
        CL3_C2.Test4(null);
    }

    void Test25()
    {
        CL3_C2 y = null;
        y.Test1();
    }

    void Test26()
    {
        CL3_C2 y = null;
        y.Test1(1);
    }

    void Test27()
    {
        CL3_C2 y = null;
        y.Test2(2);
    }

    void Test28()
    {
        CL3_C2 y = null;
        CL3_D1 d1 = y.Test2;
    }

    void Test29()
    {
        CL3_C2 y = null;
        y.v(null);
    }

    void Test30()
    {
        CL3_C2 y = null;
        y.w(null);
    }

    void Test31()
    {
        CL3_D1 u = (uuu) => System.Console.WriteLine();
    }

    void Test32()
    {
        CL3_C2 y = null;
        object zz = y.P2; 
    }
}

class C1<T>
{
    public static void Test1()
    {
    }
}

interface I1<T>
{}
";

            DiagnosticDescription[] errors = {
                // (140,11): error CS0012: The type 'CL2_I1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     class C5 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C5").WithArguments("CL2_I1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(140, 11),
                // (125,16): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     class C2 : CL3_C1
                Diagnostic(ErrorCode.ERR_NoTypeDef, "CL3_C1").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(125, 16),
                // (133,16): error CS0509: 'Module1.C4': cannot derive from sealed type 'CL3_S1'
                //     class C4 : CL3_S1
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "CL3_S1").WithArguments("Module1.C4", "CL3_S1").WithLocation(133, 16),
                // (137,15): error CS0012: The type 'CL2_I1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     interface I2 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "I2").WithArguments("CL2_I1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(137, 15),
                // (140,11): error CS0012: The type 'CL2_I1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     class C5 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C5").WithArguments("CL2_I1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(140, 11),
                // (137,15): error CS0012: The type 'CL2_I1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     interface I2 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "I2").WithArguments("CL2_I1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(137, 15),
                // (9,16): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         CL3_C1 x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(9, 16),
                // (15,16): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         CL3_C3 x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(15, 16),
                // (21,31): warning CS0219: The variable 'x3' is assigned but its value is never used
                //         System.Action<CL3_C3> x3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x3").WithArguments("x3").WithLocation(21, 31),
                // (27,16): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_C1.Test1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Test1").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(27, 16),
                // (32,24): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         global::CL3_C1.Test1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Test1").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(32, 24),
                // (53,18): warning CS0219: The variable 'x4' is assigned but its value is never used
                //         CL3_C3[] x4;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x4").WithArguments("x4").WithLocation(53, 18),
                // (65,20): warning CS0219: The variable 'x5' is assigned but its value is never used
                //         C1<CL3_C1> x5;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(65, 20),
                // (88,22): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         object z = y.x;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(88, 22),
                // (145,17): warning CS0219: The variable 'x6' is assigned but its value is never used
                //         CL3_S1? x6;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x6").WithArguments("x6").WithLocation(145, 17),
                // (151,9): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_C2.Test1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "CL3_C2.Test1").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(151, 9),
                // (156,9): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_C2.Test1(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "CL3_C2.Test1").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(156, 9),
                // (156,9): error CS0120: An object reference is required for the non-static field, method, or property 'CL3_C2.Test1(int)'
                //         CL3_C2.Test1(1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "CL3_C2.Test1").WithArguments("CL3_C2.Test1(int)").WithLocation(156, 9),
                // (161,9): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_C2.Test3();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "CL3_C2.Test3").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(161, 9),
                // (166,16): error CS1501: No overload for method 'Test4' takes 0 arguments
                //         CL3_C2.Test4();
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test4").WithArguments("Test4", "0").WithLocation(166, 16),
                // (171,9): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "CL3_C2.Test4").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(171, 9),
                // (171,16): error CS0121: The call is ambiguous between the following methods or properties: 'CL3_C2.Test4(CL3_C1)' and 'CL3_C2.Test4(CL3_C3)'
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test4").WithArguments("CL3_C2.Test4(CL3_C1)", "CL3_C2.Test4(CL3_C3)").WithLocation(171, 16),
                // (177,9): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         y.Test1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y.Test1").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(177, 9),
                // (195,23): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_D1 d1 = y.Test2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Test2").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(195, 23),
                // (207,9): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         y.w(null);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y.w(null)").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(207, 9),
                // (212,26): error CS0012: The type 'CL2_C1' is defined in an assembly that is not referenced. You must add a reference to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         CL3_D1 u = (uuu) => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "=>").WithArguments("CL2_C1", "CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(212, 26),
                // (5,20): warning CS0649: Field 'Module1.f1' is never assigned to, and will always have its default value null
                //     private CL3_C1 f1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "f1").WithArguments("Module1.f1", "null").WithLocation(5, 20)
            };

            var compilation2 = CreateCompilation(a_cs, new MetadataReference[] { cl3 });

            compilation2.VerifyDiagnostics(errors);

            string cl3Source =
@"
public class CL3_C1 : CL2_C1
{
    public static object Test1()
    {
        return null;
    }

    public static CL2_C1 Test2()
    {
        return null;
    }

    public CL2_C1 Test3()
    {
        return null;
    }
}

public class CL3_C2
{
    public static CL2_C1 Test1()
    {
        return null;
    }

    public CL2_C1 x;

    public void Test1(int x)
    {
    }

    public void Test2(int x)
    {
    }

    public static CL2_C1 Test3()
    {
        return null;
    }

    public static void Test4(CL3_C1 x)
    {
    }

    public static void Test4(CL3_C3 x)
    {
    }

    public CL3_C3 y;
    public CL3_C4 z;
    public CL3_C5[] u;
    public System.Action<CL3_C5> v;

    public CL3_D1 w;

    public static CL3_C2 Test5()
    {
        return null;
    }

    public CL3_C1 P2
    {
        get
        {
            return null;
        }
        set
        {
        }
    }

}


public class CL3_C3 : CL2_I1, CL2_I2
{
}


public class CL3_C4 : CL3_C1
{}

public class CL3_C5 : CL3_C3
{}

public delegate void CL3_D1(CL2_C1 x);

public struct CL3_S1: CL2_I1
{}

public interface CL3_I1 : CL2_I1
{}
";

            var cl3Compilation = CreateCompilation(cl3Source, new MetadataReference[] { cl2 });

            cl3Compilation.VerifyDiagnostics();

            var compilation3 = CreateCompilation(a_cs, new MetadataReference[] { new CSharpCompilationReference(cl3Compilation) });

            compilation3.VerifyDiagnostics(errors);

            var cl3BadCompilation1 = CreateCompilation(cl3Source, new MetadataReference[] { cl3 });

            var compilation4 = CreateCompilation(a_cs, new MetadataReference[] { new CSharpCompilationReference(cl3BadCompilation1) });

            DiagnosticDescription[] errors2 = {
                // (140,11): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                //     class C5 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C5").WithArguments("CL2_I1").WithLocation(140, 11),
                // (125,16): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     class C2 : CL3_C1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL3_C1").WithArguments("CL2_C1").WithLocation(125, 16),
                // (133,16): error CS0509: 'Module1.C4': cannot derive from sealed type 'CL3_S1'
                //     class C4 : CL3_S1
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "CL3_S1").WithArguments("Module1.C4", "CL3_S1").WithLocation(133, 16),
                // (137,15): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                //     interface I2 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I2").WithArguments("CL2_I1").WithLocation(137, 15),
                // (140,11): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                //     class C5 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C5").WithArguments("CL2_I1").WithLocation(140, 11),
                // (137,15): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                //     interface I2 : CL3_I1, I1<CL3_I1>
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I2").WithArguments("CL2_I1").WithLocation(137, 15),
                // (9,16): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         CL3_C1 x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(9, 16),
                // (15,16): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         CL3_C3 x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(15, 16),
                // (21,31): warning CS0219: The variable 'x3' is assigned but its value is never used
                //         System.Action<CL3_C3> x3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x3").WithArguments("x3").WithLocation(21, 31),
                // (27,16): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_C1.Test1();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Test1").WithArguments("CL2_C1").WithLocation(27, 16),
                // (32,24): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         global::CL3_C1.Test1();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Test1").WithArguments("CL2_C1").WithLocation(32, 24),
                // (53,18): warning CS0219: The variable 'x4' is assigned but its value is never used
                //         CL3_C3[] x4;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x4").WithArguments("x4").WithLocation(53, 18),
                // (65,20): warning CS0219: The variable 'x5' is assigned but its value is never used
                //         C1<CL3_C1> x5;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(65, 20),
                // (88,22): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         object z = y.x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("CL2_C1").WithLocation(88, 22),
                // (145,17): warning CS0219: The variable 'x6' is assigned but its value is never used
                //         CL3_S1? x6;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x6").WithArguments("x6").WithLocation(145, 17),
                // (151,9): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_C2.Test1();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL3_C2.Test1").WithArguments("CL2_C1").WithLocation(151, 9),
                // (156,9): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_C2.Test1(1);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL3_C2.Test1").WithArguments("CL2_C1").WithLocation(156, 9),
                // (156,9): error CS0120: An object reference is required for the non-static field, method, or property 'CL3_C2.Test1(int)'
                //         CL3_C2.Test1(1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "CL3_C2.Test1").WithArguments("CL3_C2.Test1(int)").WithLocation(156, 9),
                // (161,9): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_C2.Test3();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL3_C2.Test3").WithArguments("CL2_C1").WithLocation(161, 9),
                // (166,16): error CS1501: No overload for method 'Test4' takes 0 arguments
                //         CL3_C2.Test4();
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test4").WithArguments("Test4", "0").WithLocation(166, 16),
                // (171,9): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL3_C2.Test4").WithArguments("CL2_C1").WithLocation(171, 9),
                // (171,9): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL3_C2.Test4").WithArguments("CL2_I1").WithLocation(171, 9),
                // (171,16): error CS0121: The call is ambiguous between the following methods or properties: 'CL3_C2.Test4(CL3_C1)' and 'CL3_C2.Test4(CL3_C3)'
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test4").WithArguments("CL3_C2.Test4(CL3_C1)", "CL3_C2.Test4(CL3_C3)").WithLocation(171, 16),
                // (177,9): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         y.Test1();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y.Test1").WithArguments("CL2_C1").WithLocation(177, 9),
                // (195,23): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_D1 d1 = y.Test2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Test2").WithArguments("CL2_C1").WithLocation(195, 23),
                // (207,9): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         y.w(null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y.w(null)").WithArguments("CL2_C1").WithLocation(207, 9),
                    // (212,26): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //         CL3_D1 u = (uuu) => System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "=>").WithArguments("CL2_C1").WithLocation(212, 26),
                // (5,20): warning CS0649: Field 'Module1.f1' is never assigned to, and will always have its default value null
                //     private CL3_C1 f1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "f1").WithArguments("Module1.f1", "null").WithLocation(5, 20)
                                              };

            compilation4.VerifyDiagnostics(errors2);

            var cl3BadCompilation2 = CreateCompilation(cl3Source);

            DiagnosticDescription[] errors3 = {
                // (2,23): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                // public class CL3_C1 : CL2_C1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (9,19): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public static CL2_C1 Test2()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (14,12): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public CL2_C1 Test3()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (22,19): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public static CL2_C1 Test1()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (37,19): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public static CL2_C1 Test3()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (27,12): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public CL2_C1 x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (76,23): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                // public class CL3_C3 : CL2_I1, CL2_I2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I1").WithArguments("CL2_I1"),
                // (76,31): error CS0246: The type or namespace name 'CL2_I2' could not be found (are you missing a using directive or an assembly reference?)
                // public class CL3_C3 : CL2_I1, CL2_I2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I2").WithArguments("CL2_I2"),
                // (87,29): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                // public delegate void CL3_D1(CL2_C1 x);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1"),
                // (89,23): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                // public struct CL3_S1: CL2_I1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I1").WithArguments("CL2_I1"),
                // (92,27): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                // public interface CL3_I1 : CL2_I1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I1").WithArguments("CL2_I1")
                                              };

            cl3BadCompilation2.VerifyDiagnostics(errors3);

            var compilation5 = CreateCompilation(a_cs, new MetadataReference[] { new CSharpCompilationReference(cl3BadCompilation2) });

            DiagnosticDescription[] errors5 = {
                // (133,16): error CS0509: 'Module1.C4': cannot derive from sealed type 'CL3_S1'
                //     class C4 : CL3_S1
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "CL3_S1").WithArguments("Module1.C4", "CL3_S1").WithLocation(133, 16),
                // (9,16): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         CL3_C1 x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(9, 16),
                // (15,16): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         CL3_C3 x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(15, 16),
                // (21,31): warning CS0219: The variable 'x3' is assigned but its value is never used
                //         System.Action<CL3_C3> x3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x3").WithArguments("x3").WithLocation(21, 31),
                // (53,18): warning CS0219: The variable 'x4' is assigned but its value is never used
                //         CL3_C3[] x4;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x4").WithArguments("x4").WithLocation(53, 18),
                // (65,20): warning CS0219: The variable 'x5' is assigned but its value is never used
                //         C1<CL3_C1> x5;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(65, 20),
                // (145,17): warning CS0219: The variable 'x6' is assigned but its value is never used
                //         CL3_S1? x6;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x6").WithArguments("x6").WithLocation(145, 17),
                // (156,9): error CS0120: An object reference is required for the non-static field, method, or property 'CL3_C2.Test1(int)'
                //         CL3_C2.Test1(1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "CL3_C2.Test1").WithArguments("CL3_C2.Test1(int)").WithLocation(156, 9),
                // (166,16): error CS1501: No overload for method 'Test4' takes 0 arguments
                //         CL3_C2.Test4();
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test4").WithArguments("Test4", "0").WithLocation(166, 16),
                // (171,16): error CS0121: The call is ambiguous between the following methods or properties: 'CL3_C2.Test4(CL3_C1)' and 'CL3_C2.Test4(CL3_C3)'
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test4").WithArguments("CL3_C2.Test4(CL3_C1)", "CL3_C2.Test4(CL3_C3)").WithLocation(171, 16),
                // (177,9): error CS0176: Member 'CL3_C2.Test1()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         y.Test1();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "y.Test1").WithArguments("CL3_C2.Test1()").WithLocation(177, 9),
                // (195,23): error CS0123: No overload for 'Test2' matches delegate 'CL3_D1'
                //         CL3_D1 d1 = y.Test2;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Test2").WithArguments("Test2", "CL3_D1").WithLocation(195, 23),
                // (5,20): warning CS0649: Field 'Module1.f1' is never assigned to, and will always have its default value null
                //     private CL3_C1 f1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "f1").WithArguments("Module1.f1", "null").WithLocation(5, 20)
            };

            compilation5.VerifyDiagnostics(errors5);

            string cl4Source = a_cs + cl3Source;

            var compilation6 = CreateCompilation(cl4Source);

            DiagnosticDescription[] errors6 = {
                // (232,23): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                // public class CL3_C1 : CL2_C1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(232, 23),
                // (306,23): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                // public class CL3_C3 : CL2_I1, CL2_I2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I1").WithArguments("CL2_I1").WithLocation(306, 23),
                // (306,31): error CS0246: The type or namespace name 'CL2_I2' could not be found (are you missing a using directive or an assembly reference?)
                // public class CL3_C3 : CL2_I1, CL2_I2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I2").WithArguments("CL2_I2").WithLocation(306, 31),
                // (319,23): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                // public struct CL3_S1: CL2_I1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I1").WithArguments("CL2_I1").WithLocation(319, 23),
                // (239,19): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public static CL2_C1 Test2()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(239, 19),
                // (322,27): error CS0246: The type or namespace name 'CL2_I1' could not be found (are you missing a using directive or an assembly reference?)
                // public interface CL3_I1 : CL2_I1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_I1").WithArguments("CL2_I1").WithLocation(322, 27),
                // (317,29): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                // public delegate void CL3_D1(CL2_C1 x);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(317, 29),
                // (244,12): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public CL2_C1 Test3()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(244, 12),
                // (267,19): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public static CL2_C1 Test3()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(267, 19),
                // (252,19): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public static CL2_C1 Test1()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(252, 19),
                // (257,12): error CS0246: The type or namespace name 'CL2_C1' could not be found (are you missing a using directive or an assembly reference?)
                //     public CL2_C1 x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "CL2_C1").WithArguments("CL2_C1").WithLocation(257, 12),
                // (133,16): error CS0509: 'Module1.C4': cannot derive from sealed type 'CL3_S1'
                //     class C4 : CL3_S1
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "CL3_S1").WithArguments("Module1.C4", "CL3_S1").WithLocation(133, 16),
                // (9,16): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         CL3_C1 x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1").WithLocation(9, 16),
                // (15,16): warning CS0219: The variable 'x2' is assigned but its value is never used
                //         CL3_C3 x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x2").WithArguments("x2").WithLocation(15, 16),
                // (21,31): warning CS0219: The variable 'x3' is assigned but its value is never used
                //         System.Action<CL3_C3> x3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x3").WithArguments("x3").WithLocation(21, 31),
                // (53,18): warning CS0219: The variable 'x4' is assigned but its value is never used
                //         CL3_C3[] x4;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x4").WithArguments("x4").WithLocation(53, 18),
                // (65,20): warning CS0219: The variable 'x5' is assigned but its value is never used
                //         C1<CL3_C1> x5;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(65, 20),
                // (145,17): warning CS0219: The variable 'x6' is assigned but its value is never used
                //         CL3_S1? x6;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x6").WithArguments("x6").WithLocation(145, 17),
                // (156,9): error CS0120: An object reference is required for the non-static field, method, or property 'CL3_C2.Test1(int)'
                //         CL3_C2.Test1(1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "CL3_C2.Test1").WithArguments("CL3_C2.Test1(int)").WithLocation(156, 9),
                // (166,16): error CS1501: No overload for method 'Test4' takes 0 arguments
                //         CL3_C2.Test4();
                Diagnostic(ErrorCode.ERR_BadArgCount, "Test4").WithArguments("Test4", "0").WithLocation(166, 16),
                // (171,16): error CS0121: The call is ambiguous between the following methods or properties: 'CL3_C2.Test4(CL3_C1)' and 'CL3_C2.Test4(CL3_C3)'
                //         CL3_C2.Test4(null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test4").WithArguments("CL3_C2.Test4(CL3_C1)", "CL3_C2.Test4(CL3_C3)").WithLocation(171, 16),
                // (177,9): error CS0176: Member 'CL3_C2.Test1()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         y.Test1();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "y.Test1").WithArguments("CL3_C2.Test1()").WithLocation(177, 9),
                // (195,23): error CS0123: No overload for 'Test2' matches delegate 'CL3_D1'
                //         CL3_D1 d1 = y.Test2;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "Test2").WithArguments("Test2", "CL3_D1").WithLocation(195, 23),
                // (5,20): warning CS0649: Field 'Module1.f1' is never assigned to, and will always have its default value null
                //     private CL3_C1 f1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "f1").WithArguments("Module1.f1", "null").WithLocation(5, 20)
            };
            compilation6.VerifyDiagnostics(errors6);

            compilation1.VerifyDiagnostics(
                // (6,16): warning CS0219: The variable 'x1' is assigned but its value is never used
                //         CL3_C1 x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x1").WithArguments("x1")
                );
        }

        [Fact]
        public void MissingTypes3()
        {
            var cl2 = TestReferences.SymbolsTests.MissingTypes.CL2;
            var cl3 = TestReferences.SymbolsTests.MissingTypes.CL3;

            var references = new[]
            {
                MscorlibRef,
                NetFramework.SystemData,
                NetFramework.System,
                cl2,
                cl3
            };

            var compilation1 = CreateEmptyCompilation(
@"
    class Program
    {
        static void Main(string[] args)
        {
            new System.Data.DataSet();
        }
    }
", references);

            compilation1.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(612417, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612417")]
        public void Repro612417()
        {
            var libSource = @"
namespace System.Drawing
{
    public class Point { }
}
";

            var project1Source = @"
using System.Drawing;

public interface I
{
    void Goo(Point p);
}
";

            var project2Source = @"
using System.Drawing;

public class C
{
    public void Goo(Point p) { }
}
";

            var project3Source = @"
class D : C, I { }
";

            var libRef = CreateEmptyCompilation(libSource, new[] { MscorlibRef }, assemblyName: "System.Drawing").EmitToImageReference();

            var comp1 = CreateEmptyCompilation(project1Source, new[] { MscorlibRef, libRef }, assemblyName: "Project1");
            comp1.VerifyDiagnostics();

            var comp2 = CreateEmptyCompilation(project2Source, new[] { MscorlibRef, libRef }, assemblyName: "Project2");
            comp2.VerifyDiagnostics();

            // Scenario 1: Projects 1, 2, and 3 are in source; project 3 does not reference lib.
            {
                var comp3 = CreateEmptyCompilation(project3Source, new[] { MscorlibRef, comp1.ToMetadataReference(), comp2.ToMetadataReference() }, assemblyName: "Project3");
                comp3.VerifyDiagnostics(
                    // (2,7): error CS0012: The type 'System.Drawing.Point' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Drawing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                    // class D : C, I { }
                    Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("System.Drawing.Point", "System.Drawing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
            }

            // Scenario 2: Projects 1 and 2 are metadata, and project 3 is in source; project 3 does not reference lib.
            {
                var comp3 = CreateEmptyCompilation(project3Source, new[] { MscorlibRef, comp1.EmitToImageReference(), comp2.EmitToImageReference() }, assemblyName: "Project3");
                comp3.VerifyDiagnostics(
                    // (2,7): error CS0012: The type 'System.Drawing.Point' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Drawing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                    // class D : C, I { }
                    Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("System.Drawing.Point", "System.Drawing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
            }
        }

        [ClrOnlyFact]
        public void MissingTypeInTypeArgumentsOfImplementedInterface()
        {
            var lib1 = CreateCompilation(@"
namespace ErrorTest
{
    public interface I1<out T1>
    {}

    public interface I2
    {}

    public interface I6<in T1>
    {}

    public class C10<T> where T : I1<I2>
    {}
}", options: TestOptions.ReleaseDll, assemblyName: "MissingTypeInTypeArgumentsOfImplementedInterface1");

            var lib1Ref = new CSharpCompilationReference(lib1);

            var lib2 = CreateCompilation(@"
namespace ErrorTest
{
    public interface I3 : I2
    {}

}", new[] { lib1Ref }, TestOptions.ReleaseDll, assemblyName: "MissingTypeInTypeArgumentsOfImplementedInterface2");

            var lib2Ref = new CSharpCompilationReference(lib2);

            var lib3 = CreateCompilation(@"
namespace ErrorTest
{
    public class C4 : I1<I3>
    {}

    public interface I5 : I1<I3>
    {}

    public class C8<T> where T : I6<I3>
    {}
}", new[] { lib1Ref, lib2Ref }, TestOptions.ReleaseDll, assemblyName: "MissingTypeInTypeArgumentsOfImplementedInterface3");

            var lib3Ref = new CSharpCompilationReference(lib3);

            var lib4Def = @"
namespace ErrorTest
{
    class Test
    {
        void _Test(C4 y)
        {
            I1<I2> x = y;
        }
    }

    public class C6
        : I5
    {}

    public class C7
        : C4
    {}

    class Test3<T> where T : C4
    {
        void Test(T y3)
        {
            I1<I2> x = y3;
        }
    }

    class Test4<T> where T : I5
    {
        void Test(T y4)
        {
            I1<I2> x = y4;
        }
    }
    
    class Test5
    {
        void Test(I5 y5)
        {
            I1<I2> x = y5;
        }
    }

    public class C9
        : C8<I6<I2>>
    {}

    public class C11
        : C10<C4>
    {}

    public class C12
        : C10<I5>
    {}

    class Test6
    {
        void Test(C8<I6<I2>> x)
        {}
        void Test(C10<C4> x)
        {}
        void Test(C10<I5> x)
        {}
    }
}";

            var lib4 = CreateCompilation(lib4Def, new[] { lib1Ref, lib3Ref }, TestOptions.ReleaseDll);

            DiagnosticDescription[] expectedErrors =
            {
                // (52,18): error CS0311: The type 'ErrorTest.I5' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C10<T>'. There is no implicit reference conversion from 'ErrorTest.I5' to 'ErrorTest.I1<ErrorTest.I2>'.
                //     public class C12
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C12").WithArguments("ErrorTest.C10<T>", "ErrorTest.I1<ErrorTest.I2>", "T", "ErrorTest.I5"),
                // (52,18): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C12
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C12").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (44,18): error CS0311: The type 'ErrorTest.I6<ErrorTest.I2>' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C8<T>'. There is no implicit reference conversion from 'ErrorTest.I6<ErrorTest.I2>' to 'ErrorTest.I6<ErrorTest.I3>'.
                //     public class C9
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C9").WithArguments("ErrorTest.C8<T>", "ErrorTest.I6<ErrorTest.I3>", "T", "ErrorTest.I6<ErrorTest.I2>"),
                // (44,18): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C9
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C9").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (48,18): error CS0311: The type 'ErrorTest.C4' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C10<T>'. There is no implicit reference conversion from 'ErrorTest.C4' to 'ErrorTest.I1<ErrorTest.I2>'.
                //     public class C11
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C11").WithArguments("ErrorTest.C10<T>", "ErrorTest.I1<ErrorTest.I2>", "T", "ErrorTest.C4"),
                // (48,18): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C11
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C11").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (12,18): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C6
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C6").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (60,27): error CS0311: The type 'ErrorTest.C4' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C10<T>'. There is no implicit reference conversion from 'ErrorTest.C4' to 'ErrorTest.I1<ErrorTest.I2>'.
                //         void Test(C10<C4> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "x").WithArguments("ErrorTest.C10<T>", "ErrorTest.I1<ErrorTest.I2>", "T", "ErrorTest.C4"),
                // (60,27): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C10<C4> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (62,27): error CS0311: The type 'ErrorTest.I5' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C10<T>'. There is no implicit reference conversion from 'ErrorTest.I5' to 'ErrorTest.I1<ErrorTest.I2>'.
                //         void Test(C10<I5> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "x").WithArguments("ErrorTest.C10<T>", "ErrorTest.I1<ErrorTest.I2>", "T", "ErrorTest.I5"),
                // (62,27): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C10<I5> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (58,30): error CS0311: The type 'ErrorTest.I6<ErrorTest.I2>' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C8<T>'. There is no implicit reference conversion from 'ErrorTest.I6<ErrorTest.I2>' to 'ErrorTest.I6<ErrorTest.I3>'.
                //         void Test(C8<I6<I2>> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "x").WithArguments("ErrorTest.C8<T>", "ErrorTest.I6<ErrorTest.I3>", "T", "ErrorTest.I6<ErrorTest.I2>"),
                // (58,30): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C8<I6<I2>> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (32,24): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1<I2> x = y4;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y4").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (32,24): error CS0266: Cannot implicitly convert type 'T' to 'ErrorTest.I1<ErrorTest.I2>'. An explicit conversion exists (are you missing a cast?)
                //             I1<I2> x = y4;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y4").WithArguments("T", "ErrorTest.I1<ErrorTest.I2>"),
                // (40,24): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1<I2> x = y5;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y5").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (40,24): error CS0266: Cannot implicitly convert type 'ErrorTest.I5' to 'ErrorTest.I1<ErrorTest.I2>'. An explicit conversion exists (are you missing a cast?)
                //             I1<I2> x = y5;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y5").WithArguments("ErrorTest.I5", "ErrorTest.I1<ErrorTest.I2>"),
                // (24,24): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1<I2> x = y3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y3").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (24,24): error CS0266: Cannot implicitly convert type 'T' to 'ErrorTest.I1<ErrorTest.I2>'. An explicit conversion exists (are you missing a cast?)
                //             I1<I2> x = y3;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y3").WithArguments("T", "ErrorTest.I1<ErrorTest.I2>"),
                // (8,24): error CS0012: The type 'ErrorTest.I3' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1<I2> x = y;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y").WithArguments("ErrorTest.I3", "MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,24): error CS0266: Cannot implicitly convert type 'ErrorTest.C4' to 'ErrorTest.I1<ErrorTest.I2>'. An explicit conversion exists (are you missing a cast?)
                //             I1<I2> x = y;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("ErrorTest.C4", "ErrorTest.I1<ErrorTest.I2>")
            };

            lib4.VerifyDiagnostics(expectedErrors);

            lib4 = CreateCompilation(lib4Def, new[] { lib1Ref, lib2Ref, lib3Ref }, TestOptions.ReleaseDll);

            CompileAndVerify(lib4).VerifyDiagnostics();

            lib4 = CreateCompilation(lib4Def, new[] { lib1.EmitToImageReference(), lib3.EmitToImageReference() }, TestOptions.ReleaseDll);

            lib4.VerifyDiagnostics(expectedErrors);
        }

        [Fact()]
        public void MissingImplementedInterface()
        {
            var lib1 = CreateCompilation(@"
namespace ErrorTest
{
    public interface I1
    {
        void M1();
    }

    public class C9<T> where T : I1
    { }
}
", options: TestOptions.ReleaseDll, assemblyName: "MissingImplementedInterface1");

            var lib1Ref = new CSharpCompilationReference(lib1);

            var lib2 = CreateCompilation(@"
namespace ErrorTest
{
    public interface I2
        : I1
    {}

    public class C12
        : I2
    {
        private void M1() //Implements I1.M1
        {}

        void I1.M1()
        {
        }
    }
}
", new[] { lib1Ref }, TestOptions.ReleaseDll, assemblyName: "MissingImplementedInterface2");

            var lib2Ref = new CSharpCompilationReference(lib2);

            var lib3 = CreateCompilation(@"
namespace ErrorTest
{
    public class C4
        : I2
    {
        private void M1() //Implements I1.M1
        {}

        void I1.M1()
        {
        }
    }

    public interface I5
        : I2
    {}

    public class C13
        : C12
    { }
}
", new[] { lib1Ref, lib2Ref }, TestOptions.ReleaseDll, assemblyName: "MissingImplementedInterface3");

            var lib3Ref = new CSharpCompilationReference(lib3);

            var lib4Def = @"
namespace ErrorTest
{
    class Test
    {
        void _Test(C4 y)
        {
            I1 x = y;
        }
    }

    public class C6 : I5
    {
        void I1.M1() 
        {}
    }

    public class C7 : C4
    {}

    class Test2
    {
        void _Test2(I5 x, C4 y)
        {
            x.M1();
            y.M1();
        }
    }

    class Test3<T> where T : C4
    {
        void Test(T y3)
        {
            I1 x = y3;
            y3.M1();
        }
    }

    class Test4<T> where T : I5
    {
        void Test(T y4)
        {
            I1 x = y4;
            y4.M1();
        }
    }

    public class C8 : I5 
    {
        void I1.M1()
        {}
    }

    public class C10 : C9<C4>
    {}

    public class C11 : C9<I5>
    {}

    class Test5
    {
        void Test(C9<C4> x)
        { }
        void Test(C9<I5> x)
        { }

        void Test(C13 c13)
        {
            I1 x = c13;
        }
    }
}
";

            var lib4 = CreateCompilation(lib4Def, new[] { lib1Ref, lib3Ref }, TestOptions.ReleaseDll);

            lib4.VerifyDiagnostics(
                // (48,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C8 : I5 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C8").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (12,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C6 : I5
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C6").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (50,14): error CS0540: 'ErrorTest.C8.ErrorTest.I1.M1()': containing type does not implement interface 'ErrorTest.I1'
                //         void I1.M1()
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I1").WithArguments("ErrorTest.C8.ErrorTest.I1.M1()", "ErrorTest.I1"),
                // (14,14): error CS0540: 'ErrorTest.C6.ErrorTest.I1.M1()': containing type does not implement interface 'ErrorTest.I1'
                //         void I1.M1() 
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I1").WithArguments("ErrorTest.C6.ErrorTest.I1.M1()", "ErrorTest.I1"),
                // (54,18): error CS0311: The type 'ErrorTest.C4' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C9<T>'. There is no implicit reference conversion from 'ErrorTest.C4' to 'ErrorTest.I1'.
                //     public class C10 : C9<C4>
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C10").WithArguments("ErrorTest.C9<T>", "ErrorTest.I1", "T", "ErrorTest.C4"),
                // (54,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C10 : C9<C4>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C10").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (57,18): error CS0311: The type 'ErrorTest.I5' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C9<T>'. There is no implicit reference conversion from 'ErrorTest.I5' to 'ErrorTest.I1'.
                //     public class C11 : C9<I5>
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C11").WithArguments("ErrorTest.C9<T>", "ErrorTest.I1", "T", "ErrorTest.I5"),
                // (57,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C11 : C9<I5>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C11").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (64,26): error CS0311: The type 'ErrorTest.I5' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C9<T>'. There is no implicit reference conversion from 'ErrorTest.I5' to 'ErrorTest.I1'.
                //         void Test(C9<I5> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "x").WithArguments("ErrorTest.C9<T>", "ErrorTest.I1", "T", "ErrorTest.I5"),
                // (64,26): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C9<I5> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (62,26): error CS0311: The type 'ErrorTest.C4' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C9<T>'. There is no implicit reference conversion from 'ErrorTest.C4' to 'ErrorTest.I1'.
                //         void Test(C9<C4> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "x").WithArguments("ErrorTest.C9<T>", "ErrorTest.I1", "T", "ErrorTest.C4"),
                // (62,26): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C9<C4> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (69,20): error CS0012: The type 'ErrorTest.C12' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = c13;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c13").WithArguments("ErrorTest.C12", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (69,20): error CS0266: Cannot implicitly convert type 'ErrorTest.C13' to 'ErrorTest.I1'. An explicit conversion exists (are you missing a cast?)
                //             I1 x = c13;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c13").WithArguments("ErrorTest.C13", "ErrorTest.I1"),
                // (8,20): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = y;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,20): error CS0266: Cannot implicitly convert type 'ErrorTest.C4' to 'ErrorTest.I1'. An explicit conversion exists (are you missing a cast?)
                //             I1 x = y;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("ErrorTest.C4", "ErrorTest.I1"),
                // (34,20): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = y3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y3").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (34,20): error CS0266: Cannot implicitly convert type 'T' to 'ErrorTest.I1'. An explicit conversion exists (are you missing a cast?)
                //             I1 x = y3;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y3").WithArguments("T", "ErrorTest.I1"),
                // (35,16): error CS0122: 'ErrorTest.C4.M1()' is inaccessible due to its protection level
                //             y3.M1();
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("ErrorTest.C4.M1()"),
                // (25,15): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             x.M1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M1").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (25,15): error CS1061: 'ErrorTest.I5' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'ErrorTest.I5' could be found (are you missing a using directive or an assembly reference?)
                //             x.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("ErrorTest.I5", "M1"),
                // (26,15): error CS0122: 'ErrorTest.C4.M1()' is inaccessible due to its protection level
                //             y.M1();
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("ErrorTest.C4.M1()"),
                // (43,20): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = y4;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y4").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (43,20): error CS0266: Cannot implicitly convert type 'T' to 'ErrorTest.I1'. An explicit conversion exists (are you missing a cast?)
                //             I1 x = y4;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y4").WithArguments("T", "ErrorTest.I1"),
                // (44,16): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             y4.M1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M1").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (44,16): error CS1061: 'T' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //             y4.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("T", "M1")
                );

            lib4 = CreateCompilation(lib4Def, new[] { lib1Ref, lib2Ref, lib3Ref }, TestOptions.ReleaseDll);

            lib4.VerifyDiagnostics(
                // (35,16): error CS0122: 'ErrorTest.C4.M1()' is inaccessible due to its protection level
                //             y3.M1();
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("ErrorTest.C4.M1()"),
                // (26,15): error CS0122: 'ErrorTest.C4.M1()' is inaccessible due to its protection level
                //             y.M1();
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("ErrorTest.C4.M1()")
                );

            lib4 = CreateCompilation(lib4Def, new[] { lib1.EmitToImageReference(), lib3.EmitToImageReference() }, TestOptions.ReleaseDll);

            lib4.VerifyDiagnostics(
                // (12,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C6 : I5
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C6").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (48,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C8 : I5 
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C8").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (64,26): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C9<I5> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (62,26): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C9<C4> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (54,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C10 : C9<C4>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C10").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (57,18): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C11 : C9<I5>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C11").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,20): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = y;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (69,20): error CS0012: The type 'ErrorTest.C12' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = c13;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c13").WithArguments("ErrorTest.C12", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (69,20): error CS0266: Cannot implicitly convert type 'ErrorTest.C13' to 'ErrorTest.I1'. An explicit conversion exists (are you missing a cast?)
                //             I1 x = c13;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c13").WithArguments("ErrorTest.C13", "ErrorTest.I1"),
                // (34,20): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = y3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y3").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (35,16): error CS1061: 'T' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //             y3.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("T", "M1"),
                // (43,20): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             I1 x = y4;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y4").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (44,16): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             y4.M1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M1").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (25,15): error CS0012: The type 'ErrorTest.I2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             x.M1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M1").WithArguments("ErrorTest.I2", "MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (26,15): error CS1061: 'ErrorTest.C4' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'ErrorTest.C4' could be found (are you missing a using directive or an assembly reference?)
                //             y.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("ErrorTest.C4", "M1")
                );
        }

        [ClrOnlyFact]
        public void MissingBaseClass()
        {
            var lib1 = CreateCompilation(@"
namespace ErrorTest
{
    public class C1
    {
        public void M1()
        {}
    }

    public class C6<T> where T : C1
    {}
}", options: TestOptions.ReleaseDll, assemblyName: "MissingBaseClass1");

            var lib1Ref = new CSharpCompilationReference(lib1);

            var lib2 = CreateCompilation(@"
namespace ErrorTest
{
    public class C2 : C1
    {}
}", new[] { lib1Ref }, TestOptions.ReleaseDll, assemblyName: "MissingBaseClass2");

            var lib2Ref = new CSharpCompilationReference(lib2);

            var lib3 = CreateCompilation(@"
namespace ErrorTest
{
    public class C4 : C2
    {}
}", new[] { lib1Ref, lib2Ref }, TestOptions.ReleaseDll, assemblyName: "MissingBaseClass3");

            var lib3Ref = new CSharpCompilationReference(lib3);

            var lib4Def = @"
namespace ErrorTest
{
    class Test
    {
        void _Test(C4 y)
        {
            C1 x = y;
        }
    }

    public class C5 : C4
    {}

    class Test2
    {
        void _Test2(C4 y)
        {
            y.M1();
        }
    }

    class Test3<T> where T : C4
    {
        void Test(T y3)
        {
            C1 x = y3;
            y3.M1();
        }
    }

    public class C7 : C6<C4>
    {}

    class Test4
    {
        void Test(C6<C4> x)
        { }
    }
}";

            var lib4 = CreateCompilation(lib4Def, new[] { lib1Ref, lib3Ref }, TestOptions.ReleaseDll);

            DiagnosticDescription[] expectedErrors =
            {
                // (12,23): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C5 : C4
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C4").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (32,18): error CS0311: The type 'ErrorTest.C4' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C6<T>'. There is no implicit reference conversion from 'ErrorTest.C4' to 'ErrorTest.C1'.
                //     public class C7 : C6<C4>
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C7").WithArguments("ErrorTest.C6<T>", "ErrorTest.C1", "T", "ErrorTest.C4"),
                // (32,18): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public class C7 : C6<C4>
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C7").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (37,26): error CS0311: The type 'ErrorTest.C4' cannot be used as type parameter 'T' in the generic type or method 'ErrorTest.C6<T>'. There is no implicit reference conversion from 'ErrorTest.C4' to 'ErrorTest.C1'.
                //         void Test(C6<C4> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "x").WithArguments("ErrorTest.C6<T>", "ErrorTest.C1", "T", "ErrorTest.C4"),
                // (37,26): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         void Test(C6<C4> x)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "x").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (19,15): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             y.M1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M1").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (19,15): error CS1061: 'ErrorTest.C4' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'ErrorTest.C4' could be found (are you missing a using directive or an assembly reference?)
                //             y.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("ErrorTest.C4", "M1"),
                // (8,20): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             C1 x = y;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,20): error CS0029: Cannot implicitly convert type 'ErrorTest.C4' to 'ErrorTest.C1'
                //             C1 x = y;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("ErrorTest.C4", "ErrorTest.C1"),
                // (27,20): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             C1 x = y3;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "y3").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (27,20): error CS0029: Cannot implicitly convert type 'T' to 'ErrorTest.C1'
                //             C1 x = y3;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y3").WithArguments("T", "ErrorTest.C1"),
                // (28,16): error CS0012: The type 'ErrorTest.C2' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             y3.M1();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M1").WithArguments("ErrorTest.C2", "MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (28,16): error CS1061: 'T' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //             y3.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("T", "M1")
            };

            lib4.VerifyDiagnostics(expectedErrors);

            lib4 = CreateCompilation(lib4Def, new[] { lib1Ref, lib2Ref, lib3Ref }, TestOptions.ReleaseDll);

            CompileAndVerify(lib4).VerifyDiagnostics();

            lib4 = CreateCompilation(lib4Def, new[] { lib1.EmitToImageReference(), lib3.EmitToImageReference() }, TestOptions.ReleaseDll);

            lib4.VerifyDiagnostics(expectedErrors);
        }
    }
}
