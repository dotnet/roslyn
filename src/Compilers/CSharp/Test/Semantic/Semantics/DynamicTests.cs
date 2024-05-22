// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DynamicTests : CompilingTestBase
    {
        private static void TestTypes(string source)
        {
            SyntaxBinderTests.TestTypes(source);
        }

        private static void TestOperatorKinds(string source)
        {
            SyntaxBinderTests.TestOperatorKinds(source);
        }

        private static void TestDynamicMemberAccessCore(string source)
        {
            SyntaxBinderTests.TestDynamicMemberAccessCore(source);
        }

        private static void TestCompoundAssignment(string source)
        {
            SyntaxBinderTests.TestCompoundAssignment(source);
        }

        #region Conversions

        [Fact]
        public void DynamicAssignmentConversion_Errors()
        {
            string source = @"
public unsafe class C
{
    void M()
    {
        dynamic d = null;
        void* p1 = d;
        int* p2 = (int*)d;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,20): error CS0029: Cannot implicitly convert type 'dynamic' to 'void*'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d").WithArguments("dynamic", "void*"),
                // (8,19): error CS0030: Cannot convert type 'dynamic' to 'int*'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)d").WithArguments("dynamic", "int*"));
        }

        [Fact]
        public void ConversionClassification()
        {
            var c = CreateCompilation("", new[] { CSharpRef });
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var dynamicToObject = c.Conversions.ClassifyConversionFromType(DynamicTypeSymbol.Instance, c.GetSpecialType(SpecialType.System_Object), ref useSiteDiagnostics);
            var objectToDynamic = c.Conversions.ClassifyConversionFromType(c.GetSpecialType(SpecialType.System_Object), DynamicTypeSymbol.Instance, ref useSiteDiagnostics);

            Assert.Equal(ConversionKind.Identity, dynamicToObject.Kind);
            Assert.Equal(ConversionKind.Identity, objectToDynamic.Kind);
        }

        [Fact]
        public void UserDefinedConversion()
        {
            string source = @"
using System.Collections.Generic;

class A
{
    public static implicit operator List<object>(A a) { return null; }
    public static explicit operator List<dynamic>(A a) {return null; }
}

class B
{
    public static implicit operator object[](B a) { return null; }
    public static implicit operator dynamic[](B a) {return null; }
}";

            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (7,37): error CS0557: Duplicate user-defined conversion in type 'A'
                //     public static explicit operator List<dynamic>(A a) {return null; }
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "List<dynamic>").WithArguments("A"),
                // (13,37): error CS0557: Duplicate user-defined conversion in type 'B'
                //     public static implicit operator dynamic[](B a) {return null; }
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "dynamic[]").WithArguments("B"));
        }

        [Fact]
        public void IdentityConversions()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    void M() 
    {
        ICollection<dynamic> v1 = new List<object>();                   //-typeExpression: System.Collections.Generic.ICollection<dynamic>
                                                                        //-objectCreationExpression: System.Collections.Generic.List<object>
                                                                        //-conversion: System.Collections.Generic.ICollection<dynamic>

        ICollection<object> v2 = new List<dynamic>();                   //-typeExpression: System.Collections.Generic.ICollection<object>
                                                                        //-objectCreationExpression: System.Collections.Generic.List<dynamic>
                                                                        //-conversion: System.Collections.Generic.ICollection<object>

        IEnumerable<dynamic> v3 = new List<object>();                   //-typeExpression: System.Collections.Generic.IEnumerable<dynamic>
                                                                        //-objectCreationExpression: System.Collections.Generic.List<object>
                                                                        //-conversion: System.Collections.Generic.IEnumerable<dynamic>

        IEnumerable<object>  v4 = new List<dynamic>();                  //-typeExpression: System.Collections.Generic.IEnumerable<object>
                                                                        //-objectCreationExpression: System.Collections.Generic.List<dynamic>
                                                                        //-conversion: System.Collections.Generic.IEnumerable<object>

        IDictionary<dynamic, int> v5 = new Dictionary<object, int>();   //-typeExpression: System.Collections.Generic.IDictionary<dynamic, int>
                                                                        //-objectCreationExpression: System.Collections.Generic.Dictionary<object, int>
                                                                        //-conversion: System.Collections.Generic.IDictionary<dynamic, int>

        IDictionary<object, int> v6 = new Dictionary<dynamic, int>();   //-typeExpression: System.Collections.Generic.IDictionary<object, int>
                                                                        //-objectCreationExpression: System.Collections.Generic.Dictionary<dynamic, int>
                                                                        //-conversion: System.Collections.Generic.IDictionary<object, int>

        IList<dynamic> v7 = new List<object>();                         //-typeExpression: System.Collections.Generic.IList<dynamic>
                                                                        //-objectCreationExpression: System.Collections.Generic.List<object>
                                                                        //-conversion: System.Collections.Generic.IList<dynamic>

        IList<object> v8 = new List<dynamic>();                         //-typeExpression: System.Collections.Generic.IList<object>
                                                                        //-objectCreationExpression: System.Collections.Generic.List<dynamic>
                                                                        //-conversion: System.Collections.Generic.IList<object>
    }
}
";
            TestTypes(source);
        }

        #endregion

        #region Parameters

        [Fact]
        public void DefaultParameterValues()
        {
            string source = @"
class C
{
    void F1(dynamic d = null) { }
    void F2(dynamic d = c) { }
    void F3(dynamic d = 123) { }
    void F4(dynamic d = true) { }
    void F5(dynamic d = 1.0) { }
    void F6(dynamic d = 1.0m) { }

    const string c = ""x"";
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (5,18): error CS1763: 'd' is of type 'dynamic'. A default parameter value of a reference type other than string can only be initialized with null
                // void F2(dynamic d = "x") { }
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "d").WithArguments("d", "dynamic"),
                // (6,18): error CS1763: 'd' is of type 'dynamic'. A default parameter value of a reference type other than string can only be initialized with null
                // void F3(dynamic d = 123) { }
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "d").WithArguments("d", "dynamic"),
                // (7,19): error CS1763: 'd' is of type 'dynamic'. A default parameter value of a reference type other than string can only be initialized with null
                // void F4(dynamic d = true) { }
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "d").WithArguments("d", "dynamic"),
                // (8,19): error CS1763: 'd' is of type 'dynamic'. A default parameter value of a reference type other than string can only be initialized with null
                // void F5(dynamic d = 1.0) { }
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "d").WithArguments("d", "dynamic"),
                // (9,19): error CS1763: 'd' is of type 'dynamic'. A default parameter value of a reference type other than string can only be initialized with null
                // void F6(dynamic d = 1.0m) { }
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "d").WithArguments("d", "dynamic"));
        }

        [Fact]
        public void ArgList_Error()
        {
            string source = @"
class C 
{
  void Goo(__arglist)
  {
  }

  void Main()
  {
    dynamic d = 1;
    Goo(d);
  }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (11,9): error CS1503: Argument 1: cannot convert from 'dynamic' to '__arglist'
                Diagnostic(ErrorCode.ERR_BadArgType, "d").WithArguments("1", "dynamic", "__arglist"));
        }

        [Fact]
        public void ArgList_OK()
        {
            string source = @"
class C 
{
  void Goo(__arglist) { }
  void Goo(bool a) { }

  void Main()
  {
    dynamic d = 1;
    Goo(d);
  }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        #endregion

        #region Overrides, Hides and Implements

        [Fact]
        public void IdentityConvertibleReturnTypes_Overriding()
        {
            string source = @"
using System;
using System.Collections.Generic;

abstract class A
{
    public virtual object F(int i) { return null; }
    public virtual object this[int i] { get { return null; } }
    public virtual Dictionary<List<object>, dynamic> P { get { return null; } }
    public virtual event Action<object> E { add { } remove { } }
}

class B : A
{
    public override dynamic F(int i) { return null; }
    public override dynamic this[int i] { get { return null; } }
    public override Dictionary<List<dynamic>, object> P { get { return null; } }
    public override event Action<dynamic> E { add { } remove { } }
}

class C : B
{
    public override object F(int i) { return null; }
    public override object this[int i] { get { return null; } }
    public override Dictionary<List<object>, object> P { get { return null; } }
    public override event Action<object> E { add { } remove { } }
}
";
            CreateCompilation(source, new[] { CSharpRef }).VerifyDiagnostics();
        }

        [Fact]
        public void IdentityConvertibleReturnTypes_Hiding()
        {
            string source = @"
abstract class A
{
    public object F(int i) { return null; }
    public void G(dynamic a) { }
    public void H(params object[] a) { }
    public void I(ref dynamic a) { }
}

class B : A
{
    public dynamic F(int i) { return null; }
    public void G(object a) { }
    public void H(dynamic[] a) { }
    public void I(ref object a) { }
}
";
            CreateCompilation(source, new[] { CSharpRef }).VerifyDiagnostics(
                // (13,17): warning CS0108: 'B.G(object)' hides inherited member 'A.G(dynamic)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "G").WithArguments("B.G(object)", "A.G(dynamic)"),
                // (14,17): warning CS0108: 'B.H(dynamic[])' hides inherited member 'A.H(params object[])'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "H").WithArguments("B.H(dynamic[])", "A.H(params object[])"),
                // (15,17): warning CS0108: 'B.I(ref object)' hides inherited member 'A.I(ref dynamic)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "I").WithArguments("B.I(ref object)", "A.I(ref dynamic)"),
                // (12,20): warning CS0108: 'B.F(int)' hides inherited member 'A.F(int)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "F").WithArguments("B.F(int)", "A.F(int)"));
        }

        [Fact]
        public void IdentityConvertibleReturnTypes_InheritedImplementation()
        {
            string source = @"
using System.Collections.Generic;

public interface I
{
    List<dynamic> M(List<object> l);
}
public class A
{
    public virtual List<object> M(List<dynamic> l) { return null; }
}
public class B : A, I
{
    public static void Main(string[] args) { }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void InterfaceImplementations1()
        {
            string source = @"
using System.Collections.Generic;
 
abstract class A<T>
{
    public abstract void F<S>() where S : List<T>;
}
 
interface I
{
    void F<S>() where S : List<object>;
}
 
class B : A<dynamic>, I
{
    public override void F<S>() { } 
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void InterfaceImplementations2()
        {
            string source = @"
using System.Collections.Generic;
 
abstract class A<P>
{
    public abstract void F<S>() where S : List<P>;
}
 
interface I<P>
{
    void F<S>() where S : List<P>;
}
 
class B<T> : A<dynamic>, I<object>
{
    public override void F<S>() { } 
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(667053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667053")]
        public void OverrideChangesTypeAndParameterNames()
        {
            string source = @"
using System;
 
class C
{
    public virtual void Goo(Action<dynamic> a) { }
}
 
class D : C
{
    public override void Goo(Action<object> b) { }
}
 
class Program
{
    static void Main()
    {
        var d = new D();
        d.Goo(x => x.Bar());
        d.Goo(b: x => x.Bar());
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(667053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667053")]
        public void OverrideChangesTypeGeneric()
        {
            string source = @"
using System;
 
class C
{
    public virtual void Goo<T>(T t, Action<dynamic> a) where T : struct
    {
    }
}
 
class D : C
{
    public override void Goo<T>(T t, Action<object> a) { }
}
 
class Program
{
    static void Main()
    {
        var d = new D();
        d.Goo(1, x => x.Bar());
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterTypeConstraintsAndOverride()
        {
            string source = @"
class Q<X, Y, Z>
    where X: Y
    where Z: class
{
}

class C<T>
{
    public virtual void F<M>(M z, Q<M, T, dynamic> q) 
        where M: T
    { 
    }
}

class D<U, S> : C<S>
    where U : S
{
    public override void F<M>(M z, Q<M, S, object> q)
    {
    }

    public void G(U i)
    {
        F(i, null);
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        #endregion

        #region Operators

        [Fact]
        public void TestDynamicSimpleUnaryOps()
        {
            // Test unary ~ ! + - 
            string source = @"
class C
{
    static void M()
    {
        dynamic d1 = 123;           
        dynamic d2 = true;
        unchecked
        {
            object[] x = 
            {
                ~d1,            //-DynamicBitwiseComplement
                +d1,            //-DynamicUnaryPlus
                -d1,            //-DynamicUnaryMinus
                !d2,            //-DynamicLogicalNegation
            };
        }
        checked
        {
            object[] x = 
            {
                ~d1,            //-DynamicBitwiseComplement, Checked
                +d1,            //-DynamicUnaryPlus, Checked
                -d1,            //-DynamicUnaryMinus, Checked
                !d2,            //-DynamicLogicalNegation, Checked
            };
        }
    }
}";
            TestOperatorKinds(source);
        }

        [Fact]
        public void TestDynamicIncrementDecrement_Simple()
        {
            // ++ --
            string source = @"
class C
{
    static void M()
    {
        dynamic d = 123;   
        int s = 123;
        
        unchecked
        {
            object[] x = 
            {
                ++d,            //-DynamicPrefixIncrement
                --d,            //-DynamicPrefixDecrement
                d++,            //-DynamicPostfixIncrement
                d--,            //-DynamicPostfixDecrement
            };
        }
        checked
        {
            object[] x = 
            {
                ++d,            //-DynamicPrefixIncrement, Checked
                --d,            //-DynamicPrefixDecrement, Checked
                d++,            //-DynamicPostfixIncrement, Checked
                d--,            //-DynamicPostfixDecrement, Checked
            };
        }
    }
}";
            TestOperatorKinds(source);
        }

        [Fact]
        public void TestDynamicIncrementDecrement_PropertiesIndexers()
        {
            string source = @"
class C
{
    dynamic D { get; set; }
    object[] P { get; set; }

    static void M()
    {
        dynamic d = 123;   
        int i = 123;
        C c = new C();

        unchecked
        {
            object[] x = 
            {
                ++d.P,          //-DynamicPrefixIncrement
                --d.P,          //-DynamicPrefixDecrement
                d.P++,          //-DynamicPostfixIncrement
                d.P--,          //-DynamicPostfixDecrement
                ++d.P[i],       //-DynamicPrefixIncrement
                --d.P[i],       //-DynamicPrefixDecrement
                d.P[i]++,       //-DynamicPostfixIncrement
                d.P[i]--,       //-DynamicPostfixDecrement
                ++c.D[i],       //-DynamicPrefixIncrement
                --c.D[i],       //-DynamicPrefixDecrement
                c.D[i]++,       //-DynamicPostfixIncrement
                c.D[i]--,       //-DynamicPostfixDecrement
                ++c.P[d],       //-PrefixIncrement
                --c.P[d],       //-PrefixDecrement
                c.P[d]++,       //-PostfixIncrement
                c.P[d]--,       //-PostfixDecrement
            };
        }
        checked
        {
            object[] x = 
            {
                ++d.P,          //-DynamicPrefixIncrement, Checked
                --d.P,          //-DynamicPrefixDecrement, Checked
                d.P++,          //-DynamicPostfixIncrement, Checked
                d.P--,          //-DynamicPostfixDecrement, Checked
                ++d.P[i],       //-DynamicPrefixIncrement, Checked
                --d.P[i],       //-DynamicPrefixDecrement, Checked
                d.P[i]++,       //-DynamicPostfixIncrement, Checked
                d.P[i]--,       //-DynamicPostfixDecrement, Checked
                ++c.D[i],       //-DynamicPrefixIncrement, Checked
                --c.D[i],       //-DynamicPrefixDecrement, Checked
                c.D[i]++,       //-DynamicPostfixIncrement, Checked
                c.D[i]--,       //-DynamicPostfixDecrement, Checked
                ++c.P[d],       //-PrefixIncrement
                --c.P[d],       //-PrefixDecrement
                c.P[d]++,       //-PostfixIncrement
                c.P[d]--,       //-PostfixDecrement
            };
        }
    }
}";
            TestOperatorKinds(source);
        }

        [Fact]
        public void TestDynamicPointerAndAddressOps()
        {
            string source = @"
using System;

class C
{
    unsafe static void Main()
    {
        dynamic d = null;
        var ptr = new IntPtr(&d);
		dynamic a = *d;
		dynamic b = d->x;
    }
}
";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,30): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('dynamic')
                Diagnostic(ErrorCode.WRN_ManagedAddr, "&d").WithArguments("dynamic"),
                // (10,15): error CS0193: The * or -> operator must be applied to a pointer
                Diagnostic(ErrorCode.ERR_PtrExpected, "*d"),
                // (11,15): error CS0193: The * or -> operator must be applied to a pointer
                Diagnostic(ErrorCode.ERR_PtrExpected, "d->x")
            );
        }

        [Fact]
        public void TestDynamicAwait()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static dynamic d = null;

    static async Task<int> M()
    {
        return await d;     //-fieldAccess: dynamic
                            //-awaitableValuePlaceholder: dynamic
                            //-awaitExpression: dynamic
                            //-conversion: int
    }
}
";
            TestTypes(source);
        }

        [Fact]
        public void TestDynamicAwait2()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
	static dynamic d;

	static async void M() 
	{
		var x = await await d; //-typeExpression: dynamic
                               //-fieldAccess: dynamic
                               //-awaitableValuePlaceholder: dynamic
                               //-awaitExpression: dynamic
                               //-awaitableValuePlaceholder: dynamic
                               //-awaitExpression: dynamic
	}
}";

            TestTypes(source);
        }

        [Fact]
        public void TestDynamicSimpleBinaryOps()
        {
            // Test binary * / % + - << >> < > <= >= != == ^ & | but not
            // && || = *= /= %= += -= <<= >>= &= |= ^= ??
            string source = @"
class C
{
    static void M()
    {
        dynamic d1 = 123;
        
        unchecked
        {
            object[] x = 
            {
                d1 * d1,    //-DynamicMultiplication
                d1 * 22,    //-DynamicMultiplication
                22 * d1,    //-DynamicMultiplication
                d1 / d1,    //-DynamicDivision
                d1 / 22,    //-DynamicDivision
                22 / d1,    //-DynamicDivision
                d1 % d1,    //-DynamicRemainder
                d1 % 22,    //-DynamicRemainder
                22 % d1,    //-DynamicRemainder
                d1 + d1,    //-DynamicAddition
                d1 + 22,    //-DynamicAddition
                22 + d1,    //-DynamicAddition
                d1 - d1,    //-DynamicSubtraction
                d1 - 22,    //-DynamicSubtraction
                22 - d1,    //-DynamicSubtraction
                d1 << d1,   //-DynamicLeftShift
                d1 << 22,   //-DynamicLeftShift
                22 << d1,   //-DynamicLeftShift
                d1 >> d1,   //-DynamicRightShift
                d1 >> 22,   //-DynamicRightShift
                22 >> d1,   //-DynamicRightShift
                d1 < d1,    //-DynamicLessThan
                d1 < 22,    //-DynamicLessThan
                22 < d1,    //-DynamicLessThan
                d1 > d1,    //-DynamicGreaterThan
                d1 > 22,    //-DynamicGreaterThan
                22 > d1,    //-DynamicGreaterThan
                d1 <= d1,   //-DynamicLessThanOrEqual
                d1 <= 22,   //-DynamicLessThanOrEqual
                22 <= d1,   //-DynamicLessThanOrEqual
                d1 >= d1,   //-DynamicGreaterThanOrEqual
                d1 >= 22,   //-DynamicGreaterThanOrEqual
                22 >= d1,   //-DynamicGreaterThanOrEqual
                d1 == d1,   //-DynamicEqual
                d1 == 22,   //-DynamicEqual
                22 == d1,   //-DynamicEqual
                d1 != d1,   //-DynamicNotEqual
                d1 != 22,   //-DynamicNotEqual
                22 != d1,   //-DynamicNotEqual
                d1 & d1,    //-DynamicAnd
                d1 & 22,    //-DynamicAnd
                22 & d1,    //-DynamicAnd
                d1 | d1,    //-DynamicOr
                d1 | 22,    //-DynamicOr
                22 | d1,    //-DynamicOr
                d1 ^ d1,    //-DynamicXor
                d1 ^ 22,    //-DynamicXor
                22 ^ d1,    //-DynamicXor
            };

        }
        checked
        {
            object[] x = 
            {
                d1 * d1,    //-DynamicMultiplication, Checked
                d1 * 22,    //-DynamicMultiplication, Checked
                22 * d1,    //-DynamicMultiplication, Checked
                d1 / d1,    //-DynamicDivision, Checked
                d1 / 22,    //-DynamicDivision, Checked
                22 / d1,    //-DynamicDivision, Checked
                d1 % d1,    //-DynamicRemainder, Checked
                d1 % 22,    //-DynamicRemainder, Checked
                22 % d1,    //-DynamicRemainder, Checked
                d1 + d1,    //-DynamicAddition, Checked
                d1 + 22,    //-DynamicAddition, Checked
                22 + d1,    //-DynamicAddition, Checked
                d1 - d1,    //-DynamicSubtraction, Checked
                d1 - 22,    //-DynamicSubtraction, Checked
                22 - d1,    //-DynamicSubtraction, Checked
                d1 << d1,   //-DynamicLeftShift, Checked
                d1 << 22,   //-DynamicLeftShift, Checked
                22 << d1,   //-DynamicLeftShift, Checked
                d1 >> d1,   //-DynamicRightShift, Checked
                d1 >> 22,   //-DynamicRightShift, Checked
                22 >> d1,   //-DynamicRightShift, Checked
                d1 < d1,    //-DynamicLessThan, Checked
                d1 < 22,    //-DynamicLessThan, Checked
                22 < d1,    //-DynamicLessThan, Checked
                d1 > d1,    //-DynamicGreaterThan, Checked
                d1 > 22,    //-DynamicGreaterThan, Checked
                22 > d1,    //-DynamicGreaterThan, Checked
                d1 <= d1,   //-DynamicLessThanOrEqual, Checked
                d1 <= 22,   //-DynamicLessThanOrEqual, Checked
                22 <= d1,   //-DynamicLessThanOrEqual, Checked
                d1 >= d1,   //-DynamicGreaterThanOrEqual, Checked
                d1 >= 22,   //-DynamicGreaterThanOrEqual, Checked
                22 >= d1,   //-DynamicGreaterThanOrEqual, Checked
                d1 == d1,   //-DynamicEqual, Checked
                d1 == 22,   //-DynamicEqual, Checked
                22 == d1,   //-DynamicEqual, Checked
                d1 != d1,   //-DynamicNotEqual, Checked
                d1 != 22,   //-DynamicNotEqual, Checked
                22 != d1,   //-DynamicNotEqual, Checked
                d1 & d1,    //-DynamicAnd, Checked
                d1 & 22,    //-DynamicAnd, Checked
                22 & d1,    //-DynamicAnd, Checked
                d1 | d1,    //-DynamicOr, Checked
                d1 | 22,    //-DynamicOr, Checked
                22 | d1,    //-DynamicOr, Checked
                d1 ^ d1,    //-DynamicXor, Checked
                d1 ^ 22,    //-DynamicXor, Checked
                22 ^ d1,    //-DynamicXor, Checked
            };
        }
    }
}";
            TestOperatorKinds(source);
        }

        [Fact]
        public void TestDynamicSimpleBinaryOpsErrors()
        {
            // The dev10 compiler produces a "bad type argument" error for the use of TypedReference here, which
            // is not a very good error message. Roslyn produces a better error message, stating that TypedReference
            // cannot be used in the given operation.

            string source = @"
class C
{
    static unsafe void M(dynamic d1, System.TypedReference tr)
    {
        object[] x = 
        {
            null * d1, // OK
            d1 / null, // OK
            M % d1,
            d1 + M,
            ( ()=>{} ) - d1, 
            d1 >> ( ()=>{} ),
            delegate {} << d1,
            d1 << delegate {},
            (int*)null > d1,    
            d1 < (int*)null,
            d1 > tr,
            tr > d1
        };
    }
    static void Main() {}
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (10,13): error CS0019: Operator '%' cannot be applied to operands of type 'method group' and 'dynamic'
                //             M % d1,
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "M % d1").WithArguments("%", "method group", "dynamic").WithLocation(10, 13),
                // (11,13): error CS0019: Operator '+' cannot be applied to operands of type 'dynamic' and 'method group'
                //             d1 + M,
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d1 + M").WithArguments("+", "dynamic", "method group").WithLocation(11, 13),
                // (12,13): error CS0019: Operator '-' cannot be applied to operands of type 'lambda expression' and 'dynamic'
                //             ( ()=>{} ) - d1, 
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "( ()=>{} ) - d1").WithArguments("-", "lambda expression", "dynamic").WithLocation(12, 13),
                // (13,13): error CS0019: Operator '>>' cannot be applied to operands of type 'dynamic' and 'lambda expression'
                //             d1 >> ( ()=>{} ),
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d1 >> ( ()=>{} )").WithArguments(">>", "dynamic", "lambda expression").WithLocation(13, 13),
                // (14,13): error CS0019: Operator '<<' cannot be applied to operands of type 'anonymous method' and 'dynamic'
                //             delegate {} << d1,
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "delegate {} << d1").WithArguments("<<", "anonymous method", "dynamic").WithLocation(14, 13),
                // (14,25): warning CS8848: Operator '<<' cannot be used here due to precedence. Use parentheses to disambiguate.
                //             delegate {} << d1,
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "<<").WithArguments("<<").WithLocation(14, 25),
                // (15,13): error CS0019: Operator '<<' cannot be applied to operands of type 'dynamic' and 'anonymous method'
                //             d1 << delegate {},
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d1 << delegate {}").WithArguments("<<", "dynamic", "anonymous method").WithLocation(15, 13),
                // (16,13): error CS0019: Operator '>' cannot be applied to operands of type 'int*' and 'dynamic'
                //             (int*)null > d1,    
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(int*)null > d1").WithArguments(">", "int*", "dynamic").WithLocation(16, 13),
                // (17,13): error CS0019: Operator '<' cannot be applied to operands of type 'dynamic' and 'int*'
                //             d1 < (int*)null,
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d1 < (int*)null").WithArguments("<", "dynamic", "int*").WithLocation(17, 13),
                // (18,13): error CS0019: Operator '>' cannot be applied to operands of type 'dynamic' and 'TypedReference'
                //             d1 > tr,
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d1 > tr").WithArguments(">", "dynamic", "System.TypedReference").WithLocation(18, 13),
                // (19,13): error CS0019: Operator '>' cannot be applied to operands of type 'TypedReference' and 'dynamic'
                //             tr > d1
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "tr > d1").WithArguments(">", "System.TypedReference", "dynamic").WithLocation(19, 13));
        }

        [Fact]
        [WorkItem(624322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624322")]
        public void BinaryOps_VoidArgument()
        {
            string source = @"
class C
{
    static void M(dynamic d)
    {
        object[] x = 
        {
            d < F(),
            d > F(),
            d >= F(),
            d <= F(),
            d == F(),
            d * F(),
            d % F(),
            d + F(),
            d - F(),
            d ^ F(),
            d & F(),
            d | F(),
            d && F(),
            d || F(),
            d += F(),
            d -= F(),
            d /= F(),
            d %= F(),
            d &= F(),
            d |= F(),
            d ^= F(),
            d << F(),
            d >> F(),
        };
    }
    static void F() {}
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,13): error CS0019: Operator '<' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d < F()").WithArguments("<", "dynamic", "void"),
                // (9,13): error CS0019: Operator '>' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d > F()").WithArguments(">", "dynamic", "void"),
                // (10,13): error CS0019: Operator '>=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d >= F()").WithArguments(">=", "dynamic", "void"),
                // (11,13): error CS0019: Operator '<=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d <= F()").WithArguments("<=", "dynamic", "void"),
                // (12,13): error CS0019: Operator '==' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d == F()").WithArguments("==", "dynamic", "void"),
                // (13,13): error CS0019: Operator '*' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d * F()").WithArguments("*", "dynamic", "void"),
                // (14,13): error CS0019: Operator '%' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d % F()").WithArguments("%", "dynamic", "void"),
                // (15,13): error CS0019: Operator '+' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d + F()").WithArguments("+", "dynamic", "void"),
                // (16,13): error CS0019: Operator '-' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d - F()").WithArguments("-", "dynamic", "void"),
                // (17,13): error CS0019: Operator '^' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d ^ F()").WithArguments("^", "dynamic", "void"),
                // (18,13): error CS0019: Operator '&' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d & F()").WithArguments("&", "dynamic", "void"),
                // (19,13): error CS0019: Operator '|' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d | F()").WithArguments("|", "dynamic", "void"),
                // (20,13): error CS0019: Operator '&&' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d && F()").WithArguments("&&", "dynamic", "void"),
                // (21,13): error CS0019: Operator '||' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d || F()").WithArguments("||", "dynamic", "void"),
                // (22,13): error CS0019: Operator '+=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d += F()").WithArguments("+=", "dynamic", "void"),
                // (23,13): error CS0019: Operator '-=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d -= F()").WithArguments("-=", "dynamic", "void"),
                // (24,13): error CS0019: Operator '/=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d /= F()").WithArguments("/=", "dynamic", "void"),
                // (25,13): error CS0019: Operator '%=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d %= F()").WithArguments("%=", "dynamic", "void"),
                // (26,13): error CS0019: Operator '&=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d &= F()").WithArguments("&=", "dynamic", "void"),
                // (27,13): error CS0019: Operator '|=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d |= F()").WithArguments("|=", "dynamic", "void"),
                // (28,13): error CS0019: Operator '^=' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d ^= F()").WithArguments("^=", "dynamic", "void"),
                // (29,13): error CS0019: Operator '<<' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d << F()").WithArguments("<<", "dynamic", "void"),
                // (30,13): error CS0019: Operator '>>' cannot be applied to operands of type 'dynamic' and 'void'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d >> F()").WithArguments(">>", "dynamic", "void"));
        }

        [Fact]
        public void TestDynamicConditionalLogicalOps()
        {
            // In each of these cases we do the logical operator, which gives a dynamic result,
            // and then we do a dynamic invocation of operator true on the dynamic result.
            string source = @"
public class C
{
    static dynamic d1 = true;
    static dynamic d2 = false;
    static bool s1 = true;
    static bool s2 = false;

    static void M()
    {
                      //-DynamicTrue  
        if (d1 && d2) //-DynamicLogicalAnd
        {
        }   

                        //-DynamicTrue  
        if (d1 || d2)   //-DynamicLogicalOr
        {
        } 

                        //-DynamicTrue  
        if (s1 && d2)   //-DynamicLogicalAnd
        {
        }   

                        //-DynamicTrue  
        if (s1 || d2)   //-DynamicLogicalOr
        {
        } 

                        //-DynamicTrue  
        if (d1 && s2)   //-DynamicLogicalAnd
        {
        }   

                        //-DynamicTrue  
        if (d1 || s2)   //-DynamicLogicalOr
        {
        } 
    }
}
";
            TestOperatorKinds(source);
        }

        [Fact, WorkItem(27800, "https://github.com/dotnet/roslyn/issues/27800"), WorkItem(32068, "https://github.com/dotnet/roslyn/issues/32068")]
        public void TestDynamicCompoundOperatorOrdering()
        {
            CompileAndVerify(@"
using System;
class DynamicTest
{
    public int Property
    {
        get {
            Console.WriteLine(""get_Property"");
            return 0;
        }
        set {
            Console.WriteLine(""set_Property"");
        }
    }

    public event EventHandler<object> Event
    {
        add { Console.WriteLine(""add_Event""); }
        remove { Console.WriteLine(""remove_Event""); }
    }

    static dynamic GetDynamic()
    {
        Console.WriteLine(""GetDynamic"");
        return new DynamicTest();
    }

    static int GetInt()
    {
        Console.WriteLine(""GetInt"");
        return 1;
    }

    static EventHandler<object> GetHandler()
    {
        Console.WriteLine(""GetHandler"");
        return (object o1, object o2) => {};
    }

    public static void Main()
    {
        Console.WriteLine(""Compound Add"");
        GetDynamic().Property += GetInt();
        Console.WriteLine(""Compound And"");
        GetDynamic().Property &= GetInt();
        Console.WriteLine(""Compound Add Event"");
        GetDynamic().Event += GetHandler();
        Console.WriteLine(""Compound Remove Event"");
        GetDynamic().Event -= GetHandler();
    }
}", targetFramework: TargetFramework.StandardAndCSharp, expectedOutput: @"
Compound Add
GetDynamic
get_Property
GetInt
set_Property
Compound And
GetDynamic
get_Property
GetInt
set_Property
Compound Add Event
GetDynamic
GetHandler
add_Event
Compound Remove Event
GetDynamic
GetHandler
remove_Event
");
        }

        #endregion

        #region Conditional, Coalescing Expression

        [Fact]
        public void TestCoalescingOp()
        {
            string source = @"
public class C
{
    static dynamic d1 = true;
    static dynamic d2 = false;
    static object s1 = true;
    static object s2 = false;

    static void M()
    {
        var dd = d1 ?? d2;  //-typeExpression: dynamic
                            //-fieldAccess: dynamic
                            //-fieldAccess: dynamic
                            //-valuePlaceholder: dynamic
                            //-valuePlaceholder: dynamic
                            //-conversion: object
                            //-nullCoalescingOperator: dynamic

        var sd = s1 ?? d2;  //-typeExpression: dynamic
                            //-fieldAccess: object
                            //-fieldAccess: dynamic
                            //-valuePlaceholder: object
                            //-valuePlaceholder: object
                            //-nullCoalescingOperator: dynamic

        var ds = d1 ?? s2;  //-typeExpression: dynamic
                            //-fieldAccess: dynamic
                            //-fieldAccess: object
                            //-conversion: dynamic
                            //-valuePlaceholder: dynamic
                            //-valuePlaceholder: dynamic
                            //-nullCoalescingOperator: dynamic
    }
}
";
            TestTypes(source);
        }

        [Fact]
        public void TestDynamicConditionalExpression1()
        {
            // Note that when the dynamic expression is the condition, we do a dynamic invocation of 
            // unary operator "true" rather than a dynamic conversion to bool.

            string source = @"
public class C
{
    static dynamic d1 = true;
    static dynamic d2 = false;
    static dynamic d3 = false;
    static bool s1 = true;
    static object s2 = false;
    static object s3 = false;

    static void M()
    {
        var ddd = d1 ? d2 : d3;  //-typeExpression: dynamic
                                 //-fieldAccess: dynamic
                                 //-unaryOperator: bool
                                 //-fieldAccess: dynamic
                                 //-fieldAccess: dynamic
                                 //-conditionalOperator: dynamic

        var dds = d1 ? d2 : s3;  //-typeExpression: dynamic
                                 //-fieldAccess: dynamic
                                 //-unaryOperator: bool
                                 //-fieldAccess: dynamic
                                 //-fieldAccess: object
                                 //-conversion: dynamic
                                 //-conditionalOperator: dynamic

        var dds = d1 ? d2 : s3;  //-typeExpression: dynamic
                                 //-fieldAccess: dynamic
                                 //-unaryOperator: bool
                                 //-fieldAccess: dynamic
                                 //-fieldAccess: object
                                 //-conversion: dynamic
                                 //-conditionalOperator: dynamic

        var dsd = d1 ? s2 : d3;  //-typeExpression: dynamic
                                 //-fieldAccess: dynamic
                                 //-unaryOperator: bool
                                 //-fieldAccess: object
                                 //-conversion: dynamic
                                 //-fieldAccess: dynamic
                                 //-conditionalOperator: dynamic    
                                           
        var dss = d1 ? s2 : s3;  //-typeExpression: object
                                 //-fieldAccess: dynamic
                                 //-unaryOperator: bool
                                 //-fieldAccess: object
                                 //-fieldAccess: object
                                 //-conditionalOperator: object  
                                            
        var sdd = s1 ? d2 : d3;  //-typeExpression: dynamic
                                 //-fieldAccess: bool
                                 //-fieldAccess: dynamic
                                 //-fieldAccess: dynamic
                                 //-conditionalOperator: dynamic   
                                            
        var sds = s1 ? d2 : s3;  //-typeExpression: dynamic
                                 //-fieldAccess: bool
                                 //-fieldAccess: dynamic
                                 //-fieldAccess: object
                                 //-conversion: dynamic
                                 //-conditionalOperator: dynamic   
                                            
        var ssd = s1 ? s2 : d3;  //-typeExpression: dynamic
                                 //-fieldAccess: bool
                                 //-fieldAccess: object
                                 //-conversion: dynamic
                                 //-fieldAccess: dynamic
                                 //-conditionalOperator: dynamic    
    }
}
";
            TestTypes(source);
        }

        [Fact]
        public void TestDynamicConditionalExpression2()
        {
            string source = @"
public class C
{
    static dynamic d = null;
    static bool b = true;
    static object[] s1 = null;
    static C s2 = null;

    static void M()
    {
        var x = b ? d : s1;  //-typeExpression: dynamic
                             //-fieldAccess: bool
                             //-fieldAccess: dynamic
                             //-fieldAccess: object[]
                             //-conversion: dynamic
                             //-conditionalOperator: dynamic
                               
        var y = b ? d : s2;  //-typeExpression: dynamic
                             //-fieldAccess: bool
                             //-fieldAccess: dynamic
                             //-fieldAccess: C
                             //-conversion: dynamic
                             //-conditionalOperator: dynamic
    }
}
";
            TestTypes(source);
        }

        [Fact]
        public void TestDynamicConditionalExpression3()
        {
            string source = @"
public unsafe class C
{
    static dynamic[] d1 = null;
    static dynamic d2 = null;
    static bool s1 = true;
    static object[] s2 = null;
    static void* ptr = null;

    static void M()
    {
        var x = s1 ? d1 : s2; // ok
        var y = s1 ? d2 : M;
        var z = s1 ? M : d2;
        var v = s1 ? ptr : d2;
        var w = s1 ? d2 : ptr;
    }
}
";

            var expectedDiagnostics = new[]
            {
                // (13,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'dynamic' and 'method group'
                //         var y = s1 ? d2 : M;
                Diagnostic(ErrorCode.ERR_InvalidQM, "s1 ? d2 : M").WithArguments("dynamic", "method group").WithLocation(13, 17),
                // (14,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and 'dynamic'
                //         var z = s1 ? M : d2;
                Diagnostic(ErrorCode.ERR_InvalidQM, "s1 ? M : d2").WithArguments("method group", "dynamic").WithLocation(14, 17),
                // (15,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'void*' and 'dynamic'
                //         var v = s1 ? ptr : d2;
                Diagnostic(ErrorCode.ERR_InvalidQM, "s1 ? ptr : d2").WithArguments("void*", "dynamic").WithLocation(15, 17),
                // (16,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'dynamic' and 'void*'
                //         var w = s1 ? d2 : ptr;
                Diagnostic(ErrorCode.ERR_InvalidQM, "s1 ? d2 : ptr").WithArguments("dynamic", "void*").WithLocation(16, 17)
            };

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        #endregion

        #region Member Access, Invocation

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestDynamicMemberAccessErrors()
        {
            string source = @"
static class S { }
class C
{
    static unsafe void M()
    {
        dynamic d1 = 123;
        object x = d1.N<int>;
        d1.N<int*>();
        d1.N<System.TypedReference>();
        /*<bind>*/d1.N<S>();/*</bind>*/ // The dev11 compiler does not catch this one.
    }
    static void Main() { }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd1.N<S>()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""N"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd1.N<S>')
      Type Arguments(1):
        Symbol: S
      Instance Receiver: 
        ILocalReferenceOperation: d1 (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd1')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //     static unsafe void M()
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(5, 24),
                // CS0307: The property 'N' cannot be used with type arguments
                //         object x = d1.N<int>;
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "N<int>").WithArguments("N", "property").WithLocation(8, 23),
                // CS0306: The type 'int*' may not be used as a type argument
                //         d1.N<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int*").WithArguments("int*").WithLocation(9, 14),
                // CS0306: The type 'TypedReference' may not be used as a type argument
                //         d1.N<System.TypedReference>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(10, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestDynamicMemberAccess()
        {
            string source = @"
static class S {}
class C
{
    static void M()
    {
        dynamic d1 = 123;
        d1.A.B();                            //-B
                                             //-A
        object y = d1.E<int, double>();      //-E<int, double>
    }
    static void Main() {}
}";
            TestDynamicMemberAccessCore(source);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestDynamicCallErrors()
        {
            string source = @"
class C
{
    static void M(dynamic d)
    {
        int z;
        d.Goo(__arglist(123, 456));
        d.Goo(x: 123, y: 456, 789);
        d.Goo(ref z);
        /*<bind>*/d.Goo(System.Console.WriteLine());/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsInvalid) (Syntax: 'd.Goo(Syste ... riteLine())')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""Goo"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Goo')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  Arguments(1):
      IInvocationOperation (void System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'System.Cons ... WriteLine()')
        Instance Receiver: 
          null
        Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,15): error CS1978: Cannot use an expression of type '__arglist' as an argument to a dynamically dispatched operation.
                //         d.Goo(__arglist(123, 456));
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "__arglist(123, 456)").WithArguments("__arglist").WithLocation(7, 15),
                // file.cs(8,31): error CS8324: Named argument specifications must appear after all fixed arguments have been specified in a dynamic invocation.
                //         d.Goo(x: 123, y: 456, 789);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgumentInDynamicInvocation, "789").WithLocation(8, 31),
                // file.cs(10,25): error CS1978: Cannot use an expression of type 'void' as an argument to a dynamically dispatched operation.
                //         /*<bind>*/d.Goo(System.Console.WriteLine());/*</bind>*/
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "System.Console.WriteLine()").WithArguments("void").WithLocation(10, 25),
                // file.cs(9,19): error CS0165: Use of unassigned local variable 'z'
                //         d.Goo(ref z);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(9, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestDynamicArgumentsToCallsErrors()
        {
            string source = @"
class C
{
    public void Goo() { }
    public void Goo(int x, int y) { }
    public void M(dynamic d, C c)
    {
        /*<bind>*/c.Goo(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'c.Goo(d)')
  Children(2):
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS7036: There is no argument given that corresponds to the required parameter 'y' of 'C.Goo(int, int)'
                //         /*<bind>*/c.Goo(d)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Goo").WithArguments("y", "C.Goo(int, int)").WithLocation(8, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestDynamicArgumentsToCalls()
        {
            string source = @"
class C
{
    public void Goo() { }
    public void Goo(int x) { }
    public void Goo(string x) { }
    public void Goo<T>(int x, int y) where T : class { }
    public void Goo<T>(string x, string y) where T : class { }

    static void M(dynamic d, C c)
    {
        // This could be either of the one-parameter overloads so we allow it.
        c.Goo(d);

        /*<bind>*/c.Goo<short>(d, d)/*</bind>*/; // Doesn't constraints of generic overloads.
    }
}
";
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'c.Goo<short>(d, d)')
  Children(3):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'c.Goo<short>')
        Children(1):
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic, IsInvalid) (Syntax: 'd')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic, IsInvalid) (Syntax: 'd')
", new DiagnosticDescription[] {
                // file.cs(15,19): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C.Goo<T>(int, int)'
                //         /*<bind>*/c.Goo<short>(d, d)/*</bind>*/; // Doesn't constraints of generic overloads.
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "c.Goo<short>(d, d)").WithArguments("C.Goo<T>(int, int)", "T", "short").WithLocation(15, 19)
            }, parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'c.Goo<short>(d, d)')
  Children(3):
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
", new DiagnosticDescription[] {
                // file.cs(15,21): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C.Goo<T>(int, int)'
                //         /*<bind>*/c.Goo<short>(d, d)/*</bind>*/; // Doesn't constraints of generic overloads.
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "Goo<short>").WithArguments("C.Goo<T>(int, int)", "T", "short").WithLocation(15, 21)
            });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestDynamicMemberAccess_EarlyBoundReceiver_OuterInstance()
        {
            string source = @"
using System;

class A
{
    public Action<object> F;
    public Action<object> P { get; set; }
    public void M(int x) { }

    public class B
    {
        public void Goo()
        {
            dynamic d = null;
            F(d);
            P(d);
            /*<bind>*/M(d);/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M(d)')
  Children(2):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M')
      ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic, IsInvalid) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(15,13): error CS0120: An object reference is required for the non-static field, method, or property 'A.F'
                //             F(d);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F").WithArguments("A.F").WithLocation(15, 13),
                // file.cs(16,13): error CS0120: An object reference is required for the non-static field, method, or property 'A.P'
                //             P(d);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P").WithArguments("A.P").WithLocation(16, 13),
                // file.cs(17,23): error CS0120: An object reference is required for the non-static field, method, or property 'A.M(int)'
                //             /*<bind>*/M(d);/*</bind>*/
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M(d)").WithArguments("A.M(int)").WithLocation(17, 23),
                // file.cs(6,27): warning CS0649: Field 'A.F' is never assigned to, and will always have its default value null
                //     public Action<object> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("A.F", "null").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void RefReturn_Method([CombinatorialValues("int", "dynamic")] string type)
        {
            var source = $$"""
                {{type}} d = 1;
                S s = default;
                GetS(ref s).M(d);
                System.Console.Write(s.X);

                static ref S GetS(ref S s) => ref s;

                struct S
                {
                    public int X { get; private set; }

                    public void M(int x) => X = x;
                }
                """;
            CompileAndVerify(source, new[] { CSharpRef },
                expectedOutput: "1", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void RefReturn_Property([CombinatorialValues("int", "dynamic")] string type)
        {
            var source = $$"""
                {{type}} d = 1;
                S s = default;
                s.Self.M(d);
                System.Console.Write(s.X);

                struct S
                {
                    [System.Diagnostics.CodeAnalysis.UnscopedRef]
                    public ref S Self => ref this;
                    public int X { get; private set; }

                    public void M(int x) => X = x;
                }
                """;
            CompileAndVerify(new[] { source, UnscopedRefAttributeDefinition }, new[] { CSharpRef },
                expectedOutput: "1", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void RefReturn_Indexer([CombinatorialValues("int", "dynamic")] string type)
        {
            var source = $$"""
                {{type}} d = 1;
                S s = default;
                s[0].M(d);
                System.Console.Write(s.X);

                struct S
                {
                    [System.Diagnostics.CodeAnalysis.UnscopedRef]
                    public ref S this[int _] => ref this;
                    public int X { get; private set; }

                    public void M(int x) => X = x;
                }
                """;
            CompileAndVerify(new[] { source, UnscopedRefAttributeDefinition }, new[] { CSharpRef },
                expectedOutput: "1", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void RefReturn_Local([CombinatorialValues("int", "dynamic")] string type)
        {
            var source = $$"""
                {{type}} d = 1;
                S s = default;
                ref S r = ref s;
                r.M(d);
                System.Console.Write(s.X);

                struct S
                {
                    public int X { get; private set; }
                    public void M(int x) => X = x;
                }
                """;
            CompileAndVerify(new[] { source, UnscopedRefAttributeDefinition }, new[] { CSharpRef },
                expectedOutput: "1").VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void RefReturn_Field([CombinatorialValues("int", "dynamic")] string type)
        {
            var source = $$"""
                {{type}} d = 1;
                S s = default;
                P p = new P(ref s);
                p.S.M(d);
                System.Console.Write(s.X);

                ref struct P(ref S s)
                {
                    public ref S S = ref s;
                }

                struct S
                {
                    public int X { get; private set; }
                    public void M(int x) => X = x;
                }
                """;
            CompileAndVerify(new[] { source, UnscopedRefAttributeDefinition }, targetFramework: TargetFramework.Net70,
                expectedOutput: RefFieldTests.IncludeExpectedOutput("1"), verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void StructReceiver_Field([CombinatorialValues("int", "dynamic")] string type, bool ro)
        {
            var source = $$"""
                new C(default).Run();

                class C(S s)
                {
                    private {{(ro ? "readonly" : "")}} S s = s;

                    public void Run()
                    {
                        {{type}} d = 1;
                        s.M(d);
                        System.Console.Write(s.X);
                    }
                }

                struct S
                {
                    public int X { get; private set; }
                    public void M(int x) => X = x;
                }
                """;
            CompileAndVerify(new[] { source, UnscopedRefAttributeDefinition }, new[] { CSharpRef },
                expectedOutput: ro ? "0" : "1", verify: ro ? Verification.FailsPEVerify : default).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68063")]
        public void StructReceiver_Rvalue()
        {
            var source = """
                class Program
                {
                    static void Main()
                    {
                        dynamic d = 1;
                        GetS().M(d);
                    }
                
                    static S GetS() => default;
                }

                struct S
                {
                    public int X { get; private set; }
                    public void M(int x) => X = x;
                    public void M(string x) => throw null;
                }
                """;
            var verifier = CompileAndVerify(source, new[] { CSharpRef }).VerifyDiagnostics();
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size      103 (0x67)
                  .maxstack  9
                  .locals init (object V_0) //d
                  IL_0000:  ldc.i4.1
                  IL_0001:  box        "int"
                  IL_0006:  stloc.0
                  IL_0007:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>> Program.<>o__0.<>p__0"
                  IL_000c:  brtrue.s   IL_004c
                  IL_000e:  ldc.i4     0x100
                  IL_0013:  ldstr      "M"
                  IL_0018:  ldnull
                  IL_0019:  ldtoken    "Program"
                  IL_001e:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0023:  ldc.i4.2
                  IL_0024:  newarr     "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo"
                  IL_0029:  dup
                  IL_002a:  ldc.i4.0
                  IL_002b:  ldc.i4.1
                  IL_002c:  ldnull
                  IL_002d:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                  IL_0032:  stelem.ref
                  IL_0033:  dup
                  IL_0034:  ldc.i4.1
                  IL_0035:  ldc.i4.0
                  IL_0036:  ldnull
                  IL_0037:  call       "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)"
                  IL_003c:  stelem.ref
                  IL_003d:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)"
                  IL_0042:  call       "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                  IL_0047:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>> Program.<>o__0.<>p__0"
                  IL_004c:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>> Program.<>o__0.<>p__0"
                  IL_0051:  ldfld      "System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>>.Target"
                  IL_0056:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>> Program.<>o__0.<>p__0"
                  IL_005b:  call       "S Program.GetS()"
                  IL_0060:  ldloc.0
                  IL_0061:  callvirt   "void System.Action<System.Runtime.CompilerServices.CallSite, S, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, S, dynamic)"
                  IL_0066:  ret
                }
                """);
        }

        #endregion

        #region Type Inference

        [Fact]
        public void DynamicBestTypeInference()
        {
            string source = @"
class C
{
    static dynamic d1 = null;
    static object s1 = null;

    static void M()
    {
        var a = new[] { d1, s1 };   //-typeExpression: dynamic[]
                                    //-literal: int
                                    //-fieldAccess: dynamic
                                    //-fieldAccess: object
                                    //-conversion: dynamic
                                    //-arrayInitialization: <null>
                                    //-arrayCreation: dynamic[]
    }
}";
            TestTypes(source);
        }

        [Fact]
        [WorkItem(608628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608628")]
        public void MoreSpecificType()
        {
            string source = @"
class C
{
    static void Bar<T>(dynamic d, T a) { }
    static void Bar<T>(dynamic d, int a) { }

    static void Goo()
    {
        Bar<int>(1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        #endregion

        #region Partial Applicability and Final Validation

        [Fact]
        public void TestDynamicTypeInference()
        {
            string source = @"
public class E<W> where W : struct {}
public class F<X> {}
public class C<V>
{
    public static void M<T, U>(E<T> e, U u, V v, F<T> f) where T : struct
    {
        dynamic d = null;

        // Original parameters: E<T>, U, V, F<T>
        // Constructed parameters: E<T>, U, T, F<T>
        // Elided parameters: e, u, f 
        // Remaining parameters: v
        //
        // Overload resolution succeeds here: 

        T t = default(T);

        C<T>.M(d, t, t, u);

        // Overload resolution fails here since 
        // 3rd arg is of type U which isn't convertible to T: 

        C<T>.M(d, t, u, u);
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // error CS1503: Argument 3: cannot convert from 'U' to 'T'
                //         C<T>.M(d, t, u, u);
                Diagnostic(ErrorCode.ERR_BadArgType, "u").WithArguments("3", "U", "T"));
        }

        [Fact]
        public void CompileTimeChecking_Elision1()
        {
            string source = @"
public class C<S, R> 
{
    public void M<T>(T x, S y)
    {
    }

    public void F()
    {
        dynamic x = null;
        M(x, default(R));
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (11,14): error CS1503: Argument 2: cannot convert from 'R' to 'S'
                Diagnostic(ErrorCode.ERR_BadArgType, "default(R)").WithArguments("2", "R", "S"));
        }

        [Fact]
        public void CompileTimeChecking_Elision2()
        {
            string source = @"
class C
{
    public void M<T>(T p, object q)
    {
    }

    public void F()
    {
        dynamic x = null;
        M(ref x, x);
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (11,15): error CS1615: Argument 1 should not be passed with the 'ref' keyword
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "ref"));
        }

        [Fact, WorkItem(624410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624410")]
        public void CompileTimeChecking_Elision3()
        {
            string source = @"
public unsafe class D
{
    static void Bar<T>(C<T>.E*[] x) { }
  
    public static void M()
    {
        dynamic x = null;
        Bar(x);
    }
}

public class C<T>
{
    public enum E { A } 
}
";
            // Dev11 reports error CS0411: The type arguments for method 'Program.Bar<T>(C<T>.E*[])' cannot be inferred from the usage.
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void CompileTimeChecking_Elision4()
        {
            string source = @"
public class C
{
    public void M<S, T>(int i, S x, T y)
    {
        dynamic d = null;
        M(d, y, x);
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(627101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627101")]
        public void CompileTimeChecking_MethodConstraints_Elided1()
        {
            string source = @"
using System;
 
class C
{
    static void Main()
    {
        dynamic x = """";
        Goo(x, """");
    }
 
    static void Goo<T, S>(T x, S y) where S : IComparable<T>
    {
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(624684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624684")]
        public void CompileTimeChecking_MethodConstraints_Elided2()
        {
            string source = @"
using System.Collections;
 
class Program
{
    static void Main()
    {
        Goo((dynamic)1, 1);
    }
 
    static void Goo<T>(int x, T y) where T : IEnumerable
    {
    }
}
";
            // Dev11 reports error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method
            // 'Program.Goo<T>(int, T)'. There is no boxing conversion from 'int' to 'System.Collections.IEnumerable'.

            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(624684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624684")]
        public void CompileTimeChecking_MethodConstraints_Explicit2()
        {
            string source = @"
using System.Collections;
 
class Program
{
    static void Main()
    {
        Goo<int>((dynamic)1, 1);
    }
 
    static void Goo<T>(int x, T y) where T : IEnumerable
    {
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'Program.Goo<T>(int, T)'. There is no boxing conversion from 'int' to 'System.Collections.IEnumerable'.
                //         Goo<int>((dynamic)1, 1);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "Goo<int>").WithArguments("Program.Goo<T>(int, T)", "System.Collections.IEnumerable", "T", "int").WithLocation(8, 9));
        }

        [Fact]
        public void DynamicOverloadApplicability_NoConstraintChecks()
        {
            string source = @"
public class X<T> where T : struct {}

class C
{
    void F<T>(X<T> s, T t) where T : struct
    {
    }

    void M()
    {
        dynamic d = 1;
        F(null, d);
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicOverloadApplicability_ExplicitTypeArguments_ApplicabilityFails()
        {
            string source = @"
public class X<T> where T : struct {} 

class C
{
    void F<T>(T t, X<T> s) where T : struct 
    {
    }

    void M()
    {
        dynamic d = 1;
        F<string>(d, null);
    }
}
";
            // This should fail applicability. The type argument is known.

            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (13,9): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C.F<T>(T, X<T>)'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<string>").WithArguments("C.F<T>(T, X<T>)", "T", "string"));
        }

        [Fact]
        [WorkItem(598621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598621")]
        public void DynamicOverloadApplicability_ExplicitTypeArguments_ApplicabilitySucceeds_FinalValidationFails()
        {
            string source = @"
public class X<T> {}

class C
{
    void F<T>(T t, X<T> s) where T : struct 
    {
    }

    void M()
    {
        dynamic d = 1;
        F<string>(d, null);
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (13,9): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C.F<T>(T, X<T>)'
                //         F<string>(d, null);
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<string>").WithArguments("C.F<T>(T, X<T>)", "T", "string").WithLocation(13, 9));
        }

        [Fact]
        public void DynamicOverloadApplicability_ExpandedParams()
        {
            string source = @"
public class C
{
	public static void F<T>(string s, params T[] args) where T : C {} 

	public static void Main()
	{
		dynamic d = 1;
		F<int>(d, 1, 2);
	}
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (9,3): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'C.F<T>(string, params T[])'. There is no boxing conversion from 'int' to 'C'.
                // 		F<int>(d, 1, 2);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "F<int>").WithArguments("C.F<T>(string, params T[])", "C", "T", "int").WithLocation(9, 3));
        }

        [Fact]
        public void DynamicOverloadApplicability_ExpandedParams_Elided()
        {
            string source = @"
public class C
{
	public static void F<T>(string s, params T[] args) where T : C {} 

	public static void Main()
	{
		dynamic d = 1;
		F(d, 1, 2);
	}
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void GenericParameterConstraints_EarlyBound()
        {
            string source = @"
interface I { }

class C
{
    public T CConstraint<T>() where T : C { return default(T); }
    public T InterfaceConstraint<T>() where T : I { return default(T); }
    public T StructConstraint<T>() where T : struct { return default(T); }
    public T ReferenceTypeConstraint<T>() where T : class { return default(T); }
    public T NewConstraint<T>() where T : new() { return default(T); }

    void M()
    {
        CConstraint<dynamic>();
        InterfaceConstraint<dynamic>();
        StructConstraint<dynamic>();
        ReferenceTypeConstraint<dynamic>();
        NewConstraint<dynamic>();
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (14,9): error CS0311: The type 'dynamic' cannot be used as type parameter 'T' in the generic type or method 'C.CConstraint<T>()'. There is no implicit reference conversion from 'dynamic' to 'C'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "CConstraint<dynamic>").WithArguments("C.CConstraint<T>()", "C", "T", "dynamic"),
                // (15,9): error CS0311: The type 'dynamic' cannot be used as type parameter 'T' in the generic type or method 'C.InterfaceConstraint<T>()'. There is no implicit reference conversion from 'dynamic' to 'I'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "InterfaceConstraint<dynamic>").WithArguments("C.InterfaceConstraint<T>()", "I", "T", "dynamic"),
                // (16,9): error CS0453: The type 'dynamic' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C.StructConstraint<T>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "StructConstraint<dynamic>").WithArguments("C.StructConstraint<T>()", "T", "dynamic"));
        }

        #endregion

        #region Effective Base Type

        [Fact]
        public void DynamicTypeEraser()
        {
            string source = @"
using System;
using System.Collections.Generic;

public struct A<S, T>
{
    public enum E 
    {
        A
    }

    public class B<R>
    {
    }
}

unsafe public class C<X>
{
    public Func<A<dynamic, A<dynamic, bool>.E*[]>.B<X>, Dictionary<dynamic[], int>> F;
}
";

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics();

            var c = compilation.GlobalNamespace.GetMember<TypeSymbol>("C");
            var f = c.GetMember<FieldSymbol>("F");
            var eraser = new DynamicTypeEraser(compilation.GetSpecialType(SpecialType.System_Object));
            var erasedType = eraser.EraseDynamic(f.Type);

            Assert.Equal("System.Func<A<System.Object, A<System.Object, System.Boolean>.E*[]>.B<X>, System.Collections.Generic.Dictionary<System.Object[], System.Int32>>", erasedType.ToTestDisplayString());
        }

        [Fact]
        public void DynamicGenericConstraintThruGenericParameter1()
        {
            string source = @"
public class Base<T>
{
  public virtual void M<U>(U u) where U : T 
  {
  }
}

public class Derived : Base<dynamic>
{
  public override void M<U>(U u)
  {
    u.F();
  }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);

            var derived = compilation.GlobalNamespace.GetMember<TypeSymbol>("Derived");
            var m = derived.GetMember<MethodSymbol>("M");

            var ebc = m.TypeParameters[0].EffectiveBaseClassNoUseSiteDiagnostics;
            Assert.Equal(SpecialType.System_Object, ebc.SpecialType);

            compilation.VerifyDiagnostics(
                // (18,7): error CS1061: 'U' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'U' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("U", "F"));
        }

        [Fact]
        public void DynamicGenericConstraintThruGenericParameter2()
        {
            string source = @"
using System.Collections.Generic;

public class Base<T>
{
    public virtual void M<U>(U u) where U : T 
    {
    }
}

public class Derived : Base<List<dynamic>>
{
    public override void M<U>(U u)
    {
        u[0].F();
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (15,10): error CS1061: 'object' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F"));
        }

        [Fact]
        public void DynamicGenericConstraintThruGenericParameter3()
        {
            string source = @"
public class Base<T1, T2>
{
    public virtual void M<U>(U u) where U : T1, T2
    {
    }
}

public class Derived : Base<dynamic, object>
{
    public override void M<U>(U u)
    {
        var x = u.GetHashCode();
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicGenericConstraintThruGenericParameter4()
        {
            string source = @"
using System;
using System.Collections.Generic;

public class Base<T1, T2>
{
    public virtual void M<U>(U u) where U : T1, T2
    {
    }
}

public class Derived : Base<List<dynamic>, List<object>>
{
    public override void M<U>(U u)
    {
        Console.WriteLine(u.Count);
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(633857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633857")]
        public void Erasure_InterfaceSet()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
 
abstract class A<T>
{
    public abstract void Goo<S>(S x) where S : List<T>, IList<T>;
}
 
class B : A<dynamic>
{
    public override void Goo<S>(S x)
    {
        x.First();
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void Erasure_Delegate1()
        {
            string source = @"
using System;

abstract class A<T>
{
    public abstract void Goo<S>(S x) where S : T;
}

class B : A<Func<int, dynamic>>
{
    public override void Goo<S>(S x)
    {
        x.Invoke(1).Bar();
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (13,21): error CS1061: 'object' does not contain a definition for 'Bar' and no extension method 'Bar' accepting a first 
                // argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Bar").WithArguments("object", "Bar"));
        }

        [Fact]
        public void Erasure_Delegate2()
        {
            string source = @"
using System;

abstract class A<T>
{
    public abstract void Goo<S>(S x) where S : T;
}

unsafe class B : A<Func<Action<D<dynamic>.E*[]>, int>>
{
    public override void Goo<S>(S x)
    {
        x.Invoke(1);
    }
}

public class D<T> 
{
    public enum E { A } 
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,18): error CS1503: Argument 1: cannot convert from 'int' to 'System.Action<D<object>.E*[]>'
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "System.Action<D<object>.E*[]>"));
        }

        #endregion

        #region Compound Assignment

        [Fact]
        public void DynamicCompoundAssignment_Errors()
        {
            string source = @"
enum F { A, B }

public unsafe class C
{
	static dynamic d = null;
	static int* ptr = null;
	static C c = new C();

    static void M()
    {
        M += d;     
        d += M;     
        ptr += d;   
        d += ptr;    
    }
} 
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (14,9): error CS1656: Cannot assign to 'M' because it is a 'method group'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "M").WithArguments("M", "method group"),
                // (15,9): error CS0019: Operator '+=' cannot be applied to operands of type 'dynamic' and 'method group'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d += M").WithArguments("+=", "dynamic", "method group"),
                // (16,9): error CS0019: Operator '+=' cannot be applied to operands of type 'int*' and 'dynamic'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ptr += d").WithArguments("+=", "int*", "dynamic"),
                // (17,9): error CS0019: Operator '+=' cannot be applied to operands of type 'dynamic' and 'int*'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d += ptr").WithArguments("+=", "dynamic", "int*"));
        }

        [Fact]
        public void DynamicCompoundAssignment_Addition()
        {
            string source = @"
using System;

enum F { A, B }

public unsafe class C
{
	F fi;
	event Action ei;
	
	static dynamic d1 = null;
	static dynamic d2 = null;
	static F f;
	static event Action e;
	static Action a = null;
	static int i = 0;	
	static int* ptr = null;
	
	static C c = new C();

    static void M()
    {
        M += d1;     //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
        d1 += M;     //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion

        ptr += d1;   //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
        d1 += ptr;   //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion

        unchecked
        {{
            f += d1;     //-@operator: DynamicAddition leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.fi += d1;  //-@operator: DynamicAddition leftConversion: NoConversion finalConversion: ImplicitDynamic
            e += d1;     //-isAddition: True isDynamic: True
            c.ei += d1;  //-isAddition: True isDynamic: True

            d1 += d2;    //-@operator: DynamicAddition leftConversion: NoConversion finalConversion: Identity
            d1 += a;     //-@operator: DynamicAddition leftConversion: NoConversion finalConversion: Identity
            d1.x += a;   //-@operator: DynamicAddition leftConversion: NoConversion finalConversion: Identity
            d1[i] += a;  //-@operator: DynamicAddition leftConversion: NoConversion finalConversion: Identity
        }}
        checked
        {{
            f += d1;     //-@operator: DynamicAddition, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.fi += d1;  //-@operator: DynamicAddition, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            e += d1;     //-isAddition: True isDynamic: True
            c.ei += d1;  //-isAddition: True isDynamic: True

            d1 += d2;    //-@operator: DynamicAddition, Checked leftConversion: NoConversion finalConversion: Identity
            d1 += a;     //-@operator: DynamicAddition, Checked leftConversion: NoConversion finalConversion: Identity
            d1.x += a;   //-@operator: DynamicAddition, Checked leftConversion: NoConversion finalConversion: Identity
            d1[i] += a;  //-@operator: DynamicAddition, Checked leftConversion: NoConversion finalConversion: Identity
        }}
    }
} 
";
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_Subtraction()
        {
            string source = @"
using System;

enum F { A, B }

public unsafe class C
{
	F fi;
	event Action ei;
	
	static dynamic d1 = null;
	static dynamic d2 = null;
	static F f;
	static event Action e;
	static Action a = null;
	static int i = 0;	
	static int* ptr = null;
	
	static C c = new C();

    static void M()
    {
        M -= d1;     //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
        d1 -= M;     //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion

        ptr -= d1;   //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
        d1 -= ptr;   //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion

        unchecked
        {{
            f -= d1;     //-@operator: DynamicSubtraction leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.fi -= d1;  //-@operator: DynamicSubtraction leftConversion: NoConversion finalConversion: ImplicitDynamic
            e -= d1;     //-isAddition: False isDynamic: True
            c.ei -= d1;  //-isAddition: False isDynamic: True
                        
            d1 -= d2;    //-@operator: DynamicSubtraction leftConversion: NoConversion finalConversion: Identity
            d1 -= a;     //-@operator: DynamicSubtraction leftConversion: NoConversion finalConversion: Identity
            d1.x -= a;   //-@operator: DynamicSubtraction leftConversion: NoConversion finalConversion: Identity
            d1[i] -= a;  //-@operator: DynamicSubtraction leftConversion: NoConversion finalConversion: Identity
        }}
        checked
        {{
            f -= d1;     //-@operator: DynamicSubtraction, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.fi -= d1;  //-@operator: DynamicSubtraction, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            e -= d1;     //-isAddition: False isDynamic: True
            c.ei -= d1;  //-isAddition: False isDynamic: True
                        
            d1 -= d2;    //-@operator: DynamicSubtraction, Checked leftConversion: NoConversion finalConversion: Identity
            d1 -= a;     //-@operator: DynamicSubtraction, Checked leftConversion: NoConversion finalConversion: Identity
            d1.x -= a;   //-@operator: DynamicSubtraction, Checked leftConversion: NoConversion finalConversion: Identity
            d1[i] -= a;  //-@operator: DynamicSubtraction, Checked leftConversion: NoConversion finalConversion: Identity
        }}
    }
} 
";
            TestCompoundAssignment(source);
        }

        private static string GetDynamicCompoundAssignmentTestSource(string operatorSyntax, string operatorName)
        {
            Assert.NotEqual("+", operatorSyntax);
            Assert.NotEqual("-", operatorSyntax);

            return String.Format(@"
using System;

enum F {{ A, B }}

public unsafe class C
{{
    F fi;
    event Action ei;
    
    static dynamic d1 = null;
    static dynamic d2 = null;
    static F f;
    static event Action e;
    static Action a = null;
    static int i = 0;	
    static int* ptr = null;
	
    static C c = new C();
    
    static void M()
    {{
        M {0}= d1;     //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
        d1 {0}= M;     //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
                         
        ptr {0}= d1;   //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
        d1 {0}= ptr;   //-@operator: Error leftConversion: NoConversion finalConversion: NoConversion
         
        unchecked
        {{             
            f {0}= d1;     //-@operator: {1} leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.fi {0}= d1;  //-@operator: {1} leftConversion: NoConversion finalConversion: ImplicitDynamic
            e {0}= d1;     //-@operator: {1} leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.ei {0}= d1;  //-@operator: {1} leftConversion: NoConversion finalConversion: ImplicitDynamic
                         
            d1 {0}= d2;    //-@operator: {1} leftConversion: NoConversion finalConversion: Identity
            d1 {0}= a;     //-@operator: {1} leftConversion: NoConversion finalConversion: Identity
            d1.x {0}= a;   //-@operator: {1} leftConversion: NoConversion finalConversion: Identity
            d1[i] {0}= a;  //-@operator: {1} leftConversion: NoConversion finalConversion: Identity
        }}

        checked
        {{
            f {0}= d1;     //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.fi {0}= d1;  //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            e {0}= d1;     //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
            c.ei {0}= d1;  //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: ImplicitDynamic
                                            
            d1 {0}= d2;    //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: Identity
            d1 {0}= a;     //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: Identity
            d1.x {0}= a;   //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: Identity
            d1[i] {0}= a;  //-@operator: {1}, Checked leftConversion: NoConversion finalConversion: Identity
        }}
    }}
}}
", operatorSyntax, operatorName);
        }

        [Fact]
        public void DynamicCompoundAssignment_Multiplication()
        {
            string source = GetDynamicCompoundAssignmentTestSource("*", "DynamicMultiplication");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_Division()
        {
            string source = GetDynamicCompoundAssignmentTestSource("/", "DynamicDivision");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_Remainder()
        {
            string source = GetDynamicCompoundAssignmentTestSource("%", "DynamicRemainder");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_Xor()
        {
            string source = GetDynamicCompoundAssignmentTestSource("^", "DynamicXor");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_And()
        {
            string source = GetDynamicCompoundAssignmentTestSource("&", "DynamicAnd");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_Or()
        {
            string source = GetDynamicCompoundAssignmentTestSource("|", "DynamicOr");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_LeftShift()
        {
            string source = GetDynamicCompoundAssignmentTestSource("<<", "DynamicLeftShift");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_RightShift()
        {
            string source = GetDynamicCompoundAssignmentTestSource(">>", "DynamicRightShift");
            TestCompoundAssignment(source);
        }

        [Fact]
        public void DynamicCompoundAssignment_Logical()
        {
            string source = @"
class C
{
    bool a = true;
    bool b = true;
    int i;
    dynamic d = null;
    
    int M()
    {
        a &= d;   //-thisReference: C
                  //-fieldAccess: bool
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: bool
                  //-compoundAssignmentOperator: bool

        a |= d;   //-thisReference: C
                  //-fieldAccess: bool
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: bool
                  //-compoundAssignmentOperator: bool
       
        a ^= d;   //-thisReference: C
                  //-fieldAccess: bool
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: bool
                  //-compoundAssignmentOperator: bool
        
        i += d;   //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int

        i -= d;   //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int

        i *= d;   //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int

        i /= d;   //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int

        i %= d;   //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int

        i <<= d;  //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int

        i >>= d;  //-thisReference: C
                  //-fieldAccess: int
                  //-thisReference: C
                  //-fieldAccess: dynamic
                  //-valuePlaceholder: dynamic
                  //-valuePlaceholder: dynamic
                  //-conversion: int
                  //-compoundAssignmentOperator: int
    }
}
";
            TestTypes(source);
        }

        #endregion

        #region Collection and Object initializers

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        public void DynamicNew()
        {
            string source = @"
class C
{
    static void M()
    {
		var x = /*<bind>*/ new dynamic
        {
            a = 1,
            b = 
            {
                c = f()
            }
        } /*</bind>*/ ;
    }
} 
";

            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: dynamic, IsInvalid) (Syntax: 'new dynamic ... }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic, IsInvalid) (Syntax: '{ ... }')
        Initializers(2):
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'a = 1')
              Left: 
                IDynamicMemberReferenceOperation (Member Name: ""a"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'a')
                  Type Arguments(0)
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic, IsInvalid) (Syntax: 'b = ... }')
              InitializedMember: 
                IDynamicMemberReferenceOperation (Member Name: ""b"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'b')
                  Type Arguments(0)
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'b')
              Initializer: 
                IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic, IsInvalid) (Syntax: '{ ... }')
                  Initializers(1):
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsInvalid) (Syntax: 'c = f()')
                        Left: 
                          IDynamicMemberReferenceOperation (Member Name: ""c"", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'c')
                            Type Arguments(0)
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'c')
                        Right: 
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'f()')
                            Children(1):
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'f')
                                  Children(0)
";

            var expectedDiagnostics = new[]
            {
                // file.cs(11,21): error CS0103: The name 'f' does not exist in the current context
                //                 c = f()
                Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(11, 21),
                // file.cs(6,26): error CS8382: Invalid object creation
                // 		var x = /*<bind>*/ new dynamic
                Diagnostic(ErrorCode.ERR_InvalidObjectCreation, "dynamic").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DynamicObjectInitializer_Errors()
        {
            string source = @"
using System;

unsafe class X
{
    public dynamic A { get; set; }
    public dynamic B { get; set; }
    public dynamic C { get; set; }
    public dynamic D { get; set; }
    public static int* ptr = null;

    static void M()
    {
        var x = new X          
        {
            A = M,
            B = ptr,
            C = () => {},
            D = default(TypedReference)
        };                    
    }
} 
";

            var expectedDiagnostics = new[]
            {
                // (16,17): error CS0428: Cannot convert method group 'M' to non-delegate type 'dynamic'. Did you intend to invoke the method?
                //             A = M,
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "dynamic").WithLocation(16, 17),
                // (17,17): error CS0029: Cannot implicitly convert type 'int*' to 'dynamic'
                //             B = ptr,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "ptr").WithArguments("int*", "dynamic").WithLocation(17, 17),
                // (18,20): error CS1660: Cannot convert lambda expression to type 'dynamic' because it is not a delegate type
                //             C = () => {},
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "dynamic").WithLocation(18, 20),
                // (19,17): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'dynamic'
                //             D = default(TypedReference)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(TypedReference)").WithArguments("System.TypedReference", "dynamic").WithLocation(19, 17)
            };

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void DynamicCollectionInitializer_Errors()
        {
            string source = @"
using System;

unsafe class C
{
    public dynamic X;
    public static int* ptr = null;

    static void M()
    {
        var c = new C 
        { 
            X = 
            {
                M,
                ptr,
                () => {},
                default(TypedReference),
                M()
            } 
        };
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (15,17): error CS1976: Cannot use a method group as an argument to a dynamically dispatched operation. Did you intend to invoke the method?
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgMemgrp, "M"),
                // (16,17): error CS1978: Cannot use an expression of type 'int*' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "ptr").WithArguments("int*"),
                // (17,17): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "() => {}"),
                // (18,17): error CS1978: Cannot use an expression of type 'System.TypedReference' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "default(TypedReference)").WithArguments("System.TypedReference"),
                // (19,17): error CS1978: Cannot use an expression of type 'void' as an argument to a dynamically dispatched operation.
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArg, "M()").WithArguments("void"));
        }

        [Fact]
        public void DynamicObjectInitializer()
        {
            string source = @"
using System;

class C
{
    public dynamic A { get; set; }
    public dynamic B { get; set; }
    public dynamic C { get; set; }
        
    static void M()
    {
        var x = new C          //-typeExpression: C
                               //-objectOrCollectionValuePlaceholder: C
        {
            A =                //-objectInitializerMember: dynamic 
                               //-objectOrCollectionValuePlaceholder: dynamic
            {                  
                B =            //-dynamicObjectInitializerMember: dynamic 
                               //-objectOrCollectionValuePlaceholder: dynamic
                {              
                    C = 3      //-dynamicObjectInitializerMember: dynamic
                               //-literal: int
                               //-assignmentOperator: dynamic     
                                
                }              //-objectInitializerExpression: dynamic
                               //-assignmentOperator: dynamic         

            }                  //-objectInitializerExpression: dynamic
                               //-assignmentOperator: dynamic

         };                    //-objectInitializerExpression: C
                               //-objectCreationExpression: C
    }
} 
";
            TestTypes(source);
        }

        [Fact]
        public void DynamicCollectionInitializer()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C : List<int>
{
    static dynamic d = null;

    public void Add(int a, int b, int c) 
    {
    }

    public void Add(long a, long b, long c) 
    {
    }

    public void Add(long a) 
    {
    }

    static void M()
    {	
		var z = new C()         //-typeExpression: C
		{
			{ d },              //-objectOrCollectionValuePlaceholder: C
                                //-objectOrCollectionValuePlaceholder: C
                                //-fieldAccess: dynamic
                                //-dynamicCollectionElementInitializer: dynamic

			{ d, d, d },        //-objectOrCollectionValuePlaceholder: C
                                //-fieldAccess: dynamic
                                //-fieldAccess: dynamic
                                //-fieldAccess: dynamic
                                //-dynamicCollectionElementInitializer: dynamic

		};                      //-collectionInitializerExpression: C
                                //-objectCreationExpression: C
    }
} 
";
            TestTypes(source);
        }

        [Fact, WorkItem(578404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578404")]
        public void ExpressionTrees()
        {
            string source = @"
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

class C : List<int>
{
    static dynamic d = null;
    public int P { get; set; }
    public dynamic D { get; set; }

    public void Add(int a, int b, int c) 
    {
    }

    static object f(object arg)
    {
        return null;
    }

    static void Main()
    {	
        Expression<Func<C>> e0 = () => new C { P = d };
        Expression<Func<C>> e1 = () => new C { D = 1 };  // ok
        Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
        Expression<Func<dynamic, dynamic>> e4 = x => x.goo();
        Expression<Func<dynamic, dynamic>> e5 = x => x[1];
        Expression<Func<dynamic, dynamic>> e6 = x => x.y.z;
        Expression<Func<dynamic, dynamic>> e7 = x => x + 1;
        Expression<Func<dynamic, dynamic>> e8 = x => -x;
        Expression<Func<dynamic, dynamic>> e9 = x => f(d);
        Expression<Func<dynamic, dynamic>> e10 = x => f((object)d);  // ok
        Expression<Func<dynamic, dynamic>> e11 = x => f((dynamic)1);
        Expression<Func<dynamic, dynamic>> e12 = x => f(d ?? null);
        Expression<Func<dynamic, dynamic>> e13 = x => d ? 1 : 2;
        Expression<Func<dynamic, Task<dynamic>>> e14 = async x => await d;
        Expression<Func<dynamic, dynamic>> e15 = x => new { a = d, b = 1 }; // ok
        Expression<Func<dynamic, dynamic>> e16 = x => d;  // ok
        Expression<Func<dynamic, dynamic>> e17 = x => d as dynamic; // ok
        Expression<Func<dynamic, dynamic>> e18 = x => d is dynamic; // ok, warning
        Expression<Func<dynamic, dynamic>> e19 = x => (dynamic)1; // ok
        Expression<Func<dynamic, dynamic>> e20 = x => default(dynamic); // ok
        Expression<Func<dynamic, dynamic>> e21 = x => new dynamic();
        Expression<Func<dynamic, dynamic>> e22 = x => from a in new[] { d } select a + 1;
        Expression<Func<dynamic, dynamic>> e23 = x => from a in new[] { d } select a; // ok
        Expression<Func<dynamic, dynamic>> e24 = x => new C1(x);
    }
} 

class C1
{
    public C1(int x){}
    public C1(long x){}
}
";
            CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(source, options: TestOptions.RegularPreview) }).VerifyDiagnostics(

                // (43,55): warning CS1981: Using 'is' to test compatibility with 'dynamic' is essentially identical to testing compatibility with 'Object' and will succeed for all non-null values
                //         Expression<Func<dynamic, dynamic>> e18 = x => d is dynamic; // ok, warning
                Diagnostic(ErrorCode.WRN_IsDynamicIsConfusing, "d is dynamic").WithArguments("is", "dynamic", "Object").WithLocation(43, 55),
                // (46,59): error CS8386: Invalid object creation
                //         Expression<Func<dynamic, dynamic>> e21 = x => new dynamic();
                Diagnostic(ErrorCode.ERR_InvalidObjectCreation, "dynamic").WithLocation(46, 59),
                // (25,52): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e0 = () => new C { P = d };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(25, 52),
                // (27,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "X").WithLocation(27, 54),
                // (27,60): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "Y").WithLocation(27, 60),
                // (27,69): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "Z").WithLocation(27, 69),
                // (28,46): error CS1963: An expression tree may not contain a dynamic operation
                // 		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(28, 46),
                // (28,53): error CS1963: An expression tree may not contain a dynamic operation
                // 		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(28, 53),
                // (28,56): error CS1963: An expression tree may not contain a dynamic operation
                // 		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(28, 56),
                // (28,59): error CS1963: An expression tree may not contain a dynamic operation
                // 		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(28, 59),
                // (29,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e4 = x => x.goo();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.goo()").WithLocation(29, 54),
                // (29,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e4 = x => x.goo();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.goo").WithLocation(29, 54),
                // (30,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e5 = x => x[1];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x[1]").WithLocation(30, 54),
                // (31,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e6 = x => x.y.z;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.y.z").WithLocation(31, 54),
                // (31,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e6 = x => x.y.z;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.y").WithLocation(31, 54),
                // (32,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e7 = x => x + 1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x + 1").WithLocation(32, 54),
                // (33,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e8 = x => -x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "-x").WithLocation(33, 54),
                // (38,55): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e13 = x => d ? 1 : 2;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(38, 55),
                // (39,56): error CS1989: Async lambda expressions cannot be converted to expression trees
                //         Expression<Func<dynamic, Task<dynamic>>> e14 = async x => await d;
                Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async x => await d").WithLocation(39, 56),
                // (47,84): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e22 = x => from a in new[] { d } select a + 1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "a + 1").WithLocation(47, 84),
                // (49,55): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e24 = x => new C1(x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "new C1(x)").WithLocation(49, 55)
                );

            CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(source, options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)) }).VerifyDiagnostics(

                // (43,55): warning CS1981: Using 'is' to test compatibility with 'dynamic' is essentially identical to testing compatibility with 'Object' and will succeed for all non-null values
                //         Expression<Func<dynamic, dynamic>> e18 = x => d is dynamic; // ok, warning
                Diagnostic(ErrorCode.WRN_IsDynamicIsConfusing, "d is dynamic").WithArguments("is", "dynamic", "Object").WithLocation(43, 55),
                // (46,59): error CS8382: Invalid object creation
                //         Expression<Func<dynamic, dynamic>> e21 = x => new dynamic();
                Diagnostic(ErrorCode.ERR_InvalidObjectCreation, "dynamic").WithLocation(46, 59),
                // (25,52): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e0 = () => new C { P = d };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(25, 52),
                // (27,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "X").WithLocation(27, 54),
                // (27,60): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "Y").WithLocation(27, 60),
                // (27,69): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<C>> e2 = () => new C { D = { X = { Y = 1 }, Z = 1 } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "Z").WithLocation(27, 69),
                // (28,44): error CS1963: An expression tree may not contain a dynamic operation
                // 		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "{ d }").WithLocation(28, 44),
                // (28,51): error CS1963: An expression tree may not contain a dynamic operation
                // 		Expression<Func<C>> e3 = () => new C() { { d }, { d, d, d } };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "{ d, d, d }").WithLocation(28, 51),
                // (29,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e4 = x => x.goo();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.goo()").WithLocation(29, 54),
                // (29,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e4 = x => x.goo();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.goo").WithLocation(29, 54),
                // (30,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e5 = x => x[1];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x[1]").WithLocation(30, 54),
                // (31,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e6 = x => x.y.z;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.y.z").WithLocation(31, 54),
                // (31,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e6 = x => x.y.z;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x.y").WithLocation(31, 54),
                // (32,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e7 = x => x + 1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "x + 1").WithLocation(32, 54),
                // (33,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e8 = x => -x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "-x").WithLocation(33, 54),
                // (34,54): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e9 = x => f(d);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "f(d)").WithLocation(34, 54),
                // (36,55): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e11 = x => f((dynamic)1);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "f((dynamic)1)").WithLocation(36, 55),
                // (37,55): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e12 = x => f(d ?? null);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "f(d ?? null)").WithLocation(37, 55),
                // (38,55): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e13 = x => d ? 1 : 2;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(38, 55),
                // (39,56): error CS1989: Async lambda expressions cannot be converted to expression trees
                //         Expression<Func<dynamic, Task<dynamic>>> e14 = async x => await d;
                Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async x => await d").WithLocation(39, 56),
                // (47,84): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e22 = x => from a in new[] { d } select a + 1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "a + 1").WithLocation(47, 84),
                // (49,55): error CS1963: An expression tree may not contain a dynamic operation
                //         Expression<Func<dynamic, dynamic>> e24 = x => new C1(x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "new C1(x)").WithLocation(49, 55)
                );
        }

        [Fact, WorkItem(578401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578401")]
        public void ExpressionTrees_ByRefDynamic()
        {
            string source = @"
using System;
using System.Linq.Expressions;
 
class Program
{
    static void Main()
    {
        Expression<Action<dynamic>> e = x => Goo(ref x);
    }
 
    static void Goo<T>(ref T x) { }
}
";
            CompileAndVerify(source, targetFramework: TargetFramework.StandardAndCSharp);
        }

        #endregion

        #region Async

        [Fact]
        public void TestBadAsyncExpressionTree()
        {
            string source = @"
using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

class C : List<int>
{
    static dynamic d = null;

    static void Main()
    {
        Expression<Func<dynamic, Task<dynamic>>> e1 = async x => await d;
        Expression<Func<dynamic, Task<dynamic>>> e2 = async x => { return await d; };
        Expression<Func<dynamic, Task<dynamic>>> e3 = async (x) => await d;
        Expression<Func<dynamic, Task<dynamic>>> e4 = async (x) => { return await d; };
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(source, options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)) }).VerifyDiagnostics(
                // (13,55): error CS1989: Async lambda expressions cannot be converted to expression trees
                //         Expression<Func<dynamic, Task<dynamic>>> e1 = async x => await d;
                Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async x => await d"),
                // (14,55): error CS1989: Async lambda expressions cannot be converted to expression trees
                //         Expression<Func<dynamic, Task<dynamic>>> e2 = async x => { return await d; };
                Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async x => { return await d; }"),
                // (15,55): error CS1989: Async lambda expressions cannot be converted to expression trees
                //         Expression<Func<dynamic, Task<dynamic>>> e3 = async (x) => await d;
                Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async (x) => await d"),
                // (16,55): error CS1989: Async lambda expressions cannot be converted to expression trees
                //         Expression<Func<dynamic, Task<dynamic>>> e4 = async (x) => { return await d; };
                Diagnostic(ErrorCode.ERR_BadAsyncExpressionTree, "async (x) => { return await d; }"));
        }

        [Fact]
        [WorkItem(18320, "https://github.com/dotnet/roslyn/issues/18320")]
        public void TestMissingMicrosoftCSharpDllReference()
        {
            string source = @"
public class Class1
{
    public dynamic GetResponse()
    {
        return null;
    }
    public async void GetResponseTest()
    {
        var result = await GetResponse();
    }
}";
            CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugDll).VerifyEmitDiagnostics(
                // (10,28): error CS0656: Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'
                //         var result = await GetResponse();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "GetResponse()").WithArguments("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo", "Create").WithLocation(10, 28)
                );
        }

        #endregion

        #region Query

        [Fact]
        public void DynamicQuerySource()
        {
            string source = @"
using System.Linq;

class C
{
    static dynamic D1 = null;
    static dynamic D2 = null;

    static void M() 
    {
        var x = from a in D1
                join b in D2 on a equals b
                select a;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (11,27): error CS1979: Query expressions over source type 'dynamic' or with a join sequence of type 'dynamic' are not allowed
                Diagnostic(ErrorCode.ERR_BadDynamicQuery, "D1"),
                // (12,27): error CS1979: Query expressions over source type 'dynamic' or with a join sequence of type 'dynamic' are not allowed
                Diagnostic(ErrorCode.ERR_BadDynamicQuery, "D2"));
        }

        [Fact]
        public void DynamicEnumerableQuerySource()
        {
            string source = @"
using System.Linq;
using System.Collections.Generic;

class C
{
    static IEnumerable<dynamic> D = null;

    static void M() 
    {
        var x = from a in D
                join b in D on a equals b
                select a;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicQuery_Select1()
        {
            string source = @"
using System;

public class Q<T>
{
	public Q<T> Where(Func<T,bool> predicate) { throw null; }
	public dynamic Select<U>(Func<T,U> selector) { throw null; }
}

class C
{
    static void M() 
    {
        var x = from a in new Q<int>()
                select a;
        x.goo();
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicQuery_Select2()
        {
            string source = @"
using System;

public class Q<T>
{
	public Q<T> Where(Func<T,bool> predicate) { throw null; }
	public Q<U> Select<U>(dynamic selector) { throw null; }
}

class C
{
    static void M() 
    {
        var x = from a in new Q<int>()
                select a;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (15,17): error CS1942: The type of the expression in the select clause is incorrect.  Type inference failed in the call to 'Select'.
                Diagnostic(ErrorCode.ERR_QueryTypeInferenceFailed, "select").WithArguments("select", "Select"));
        }

        [Fact]
        public void DynamicQuery_DynamicWhereTrivialSelect()
        {
            string source = @"
using System;

public class Q<T>
{
	public dynamic Where(Func<T,bool> predicate) { throw null; }
	public Q<U> Select<U>(Func<T,U> selector) { throw null; }
}

class C
{
    static void M() 
    {
        var x = from a in new Q<int>()
                where a == 0
                select a;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicQuery_DynamicWhereNonTrivialSelect()
        {
            string source = @"
using System;

public class Q<T>
{
	public dynamic Where(Func<T,bool> predicate) { throw null; }
	public Q<U> Select<U>(Func<T,U> selector) { throw null; }
}

class C
{
    static void M() 
    {
        var x = from a in new Q<int>()
                where a == 0
                select a + 1;
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (16,17): error CS1979: Query expressions over source type 'dynamic' or with a join sequence of type 'dynamic' are not allowed
                Diagnostic(ErrorCode.ERR_BadDynamicQuery, "select a + 1"));
        }

        #endregion

        #region Misc Expressions

        [Fact]
        public void TestDynamicIndexers()
        {
            const string source = @"
class B
{
  public int this[double x] { get { return 1; } set { } }
  public int this[float x] { get { return 1; } set { } }
}
class C : B
{
  public int this[int x] { get { return 1; } set { } }
  public int this[string x] { get { return 1; } set { } }
  public int this[int a, System.Func<int, int> b, object c] { get { return 1; } set { } }
  public int this[long a, System.Func<int, int> b, object c] { get { return 1; } set { } }
  void M(C c, dynamic d)
  {
    // No overload takes two arguments:
    c[d, d] = 1; 

    // This should succeed:
    c[d] = 2; 

    // Overload resolution finds an applicable candidate, but the dynamic operation may not contain a lambda.
    c[d, q=>q, null] = 3; 

    // Overload resolution finds an applicable candidate, but no dynamic dispatch on base expressions is allowed.
    base[d] = 4;
  }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (16,5): error CS1501: No overload for method 'this' takes 2 arguments
                //     c[d, d] = 1; 
                Diagnostic(ErrorCode.ERR_BadArgCount, "c[d, d]").WithArguments("this", "2").WithLocation(16, 5),
                // (22,10): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //     c[d, q=>q, null] = 3; 
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "q=>q"),
                // (25,5): error CS1972: The indexer access needs to be dynamically dispatched, but cannot be because it is part of a base access expression. Consider casting the dynamic arguments or eliminating the base access.
                //     base[d] = 4;
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseIndexer, "base[d]"));
        }

        [Fact]
        public void DynamicDelegateInvocation()
        {
            const string source = @"
class P
{
  void M(dynamic d)
  {
    d(123);
  }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicDelegateInvocation2()
        {
            const string source = @"
class P
{
  delegate void F(int x);
  void M(F f, dynamic d)
  {
    f(d);
  }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicDelegateInvocation3()
        {
            const string source = @"
class P
{
  delegate void F(int x, int y);
  void M(F f, dynamic d)
  {
    f(d, 1.23);
  }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (7,10): error CS1503: Argument 2: cannot convert from 'double' to 'int'
                //     f(d, 1.23);
                Diagnostic(ErrorCode.ERR_BadArgType, "1.23").WithArguments("2", "double", "int"));
        }

        [Fact]
        public void DynamicDelegateInvocation4()
        {
            const string source = @"
class P
{
  delegate void F(int x, int y);
  void M(F f, dynamic d)
  {
    f(d, 1.23);
  }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (7,10): error CS1503: Argument 2: cannot convert from 'double' to 'int'
                //     f(d, 1.23);
                Diagnostic(ErrorCode.ERR_BadArgType, "1.23").WithArguments("2", "double", "int"));
        }

        [Fact]
        public void DynamicDelegateInvocation_Field()
        {
            const string source = @"
class C
{
    dynamic d = null;
    
    void M(C c)
    {
        c.d(1);
    }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicDelegateInvocation_Property()
        {
            const string source = @"
class C
{
    dynamic d { get; set; }
    
    void M(C c)
    {
        c.d(1);
    }
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicBooleanExpression()
        {
            const string source = @"
class C
{
  int M(dynamic d)
  {
    // This is a dynamic invocation of operator true, not a dynamic conversion to bool.
    return d ? 1 : 2; //-DynamicTrue
  }
}";
            TestOperatorKinds(source);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicBooleanExpression_MissingOperator_False()
        {
            const string source = @"
class B { }

class C
{
  B b = null;

  dynamic M(dynamic d)
  {
    return b && d;
  }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (10,12): error CS7083: Expression must be implicitly convertible to Boolean or its type 'B' must define operator 'false'.
                Diagnostic(ErrorCode.ERR_InvalidDynamicCondition, "b").WithArguments("B", "false")
            );
        }

        [Fact]
        public void DynamicBooleanExpression_MissingOperator_True()
        {
            const string source = @"
class B { }

class C
{
  B b = null;

  dynamic M(dynamic d)
  {
    return b || d;
  }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (10,12): error CS7083: Expression must be implicitly convertible to Boolean or its type 'B' must define operator 'true'.
                Diagnostic(ErrorCode.ERR_InvalidDynamicCondition, "b").WithArguments("B", "true")
            );
        }

        [Fact]
        public void DynamicBooleanExpression_MethodGroup()
        {
            const string source = @"
class B { }

class C
{
  dynamic M(dynamic d)
  {
    return M && d;
  }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,12): error CS0019: Operator '&&' cannot be applied to operands of type 'method group' and 'dynamic'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "M && d").WithArguments("&&", "method group", "dynamic")
            );
        }

        [Fact]
        public void DynamicConstructorCall1()
        {
            // If there are one or more applicable ctors then we do a dynamic binding.
            const string source = @"
class C
{
  public C(int x) {}
  public C(string x) {}
  public C(string x, string y) {}

  C M(dynamic d)
  {
    return new C(d);
  }
}";
            TestOperatorKinds(source);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicConstructorCall2()
        {
            // If there are no applicable ctors then we give a compile-time error.
            const string source = @"
class C
{
  public C(string x, string y) {}

  C M(dynamic d)
  {
    return new C(d);
  }
}";
            TestOperatorKinds(source);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (8,16): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'C.C(string, string)'
                //     return new C(d);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("y", "C.C(string, string)").WithLocation(8, 16));
        }

        [Fact]
        public void DynamicConstructorCall3()
        {
            // The type of a dynamic ctor expression is the compile-time type, not dynamic.
            const string source = @"
class C
{
  
  public C(string x) {}
  void N(int z) {}
  void M(dynamic d)
  {
    N(new C(d));
  }
}";
            TestOperatorKinds(source);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (9,7): error CS1503: Argument 1: cannot convert from 'C' to 'int'
                //     N(new C(d));
                Diagnostic(ErrorCode.ERR_BadArgType, "new C(d)").WithArguments("1", "C", "int"));
        }

        [Fact]
        public void NamespaceCalledDynamic()
        {
            var source =
@"namespace dynamic
{
    class C
    {
        public dynamic x { get; set; }
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(693741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/693741")]
        public void DynamicAndNull()
        {
            var source = @"
class C
{
    static void Main()
    {
        dynamic d = new object();
        d = d && null;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DynamicBeforeCSharp4()
        {
            var source = @"
class C
{
    dynamic M()
    {
        throw null;
    }
}

class D
{
    class @dynamic { }

    dynamic M()
    {
        throw null;
    }
}
";
            // NOTE: the error is that the type is not known, not that the feature is unavailable.
            CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp4)).VerifyDiagnostics();
            CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3)).VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'dynamic' could not be found (are you missing a using directive or an assembly reference?)
                //     dynamic M()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamic"));
        }

        [Fact]
        public void DynamicExceptionFilters()
        {
            string source = @"
using System;

class C
{
    dynamic d = null;
        
    void M()
    {
        try
        {
        }
        catch (Exception e) when (d)  //-local: System.Exception
                                    //-thisReference: C
                                    //-fieldAccess: dynamic
                                    //-unaryOperator: bool
        {
        }
    }
}";
            TestTypes(source);
        }

        #endregion

        #region Misc Statements

        [Fact]
        public void UsingStatement()
        {
            string source = @"
class C
{
    dynamic d = null;
        
    void M()
    {
        using (dynamic u = d)  //-typeExpression: dynamic
                               //-thisReference: C
                               //-fieldAccess: dynamic
        {
        }
    }
}";
            TestTypes(source);
        }

        #endregion

        [Fact, WorkItem(922611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/922611"), WorkItem(56, "CodePlex")]
        public void Bug922611_01()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        IEnumerable<object> objectSource = null;
        Action<dynamic> dynamicAction = null;
        // Fails under Roslyn, compiles under C# 5 compiler
        Goo(objectSource, dynamicAction);
    }

    static void Goo<T>(IEnumerable<T> source, Action<T> action)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            var verifier = CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: "System.Object").VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.Single();
            var model = verifier.Compilation.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Goo").Single();
            Assert.Equal("void Test.Goo<dynamic>(System.Collections.Generic.IEnumerable<dynamic> source, System.Action<dynamic> action)", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(922611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/922611"), WorkItem(56, "CodePlex")]
        public void Bug922611_02()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        IEnumerable<object> objectSource = null;
        Action<dynamic> dynamicAction = null;
        // Fails under Roslyn, compiles under C# 5 compiler
        Goo(dynamicAction, objectSource);
    }

    static void Goo<T>(Action<T> action, IEnumerable<T> source)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: "System.Object").VerifyDiagnostics();

            var verifier = CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: "System.Object").VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.Single();
            var model = verifier.Compilation.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Goo").Single();
            Assert.Equal("void Test.Goo<dynamic>(System.Action<dynamic> action, System.Collections.Generic.IEnumerable<dynamic> source)", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(875140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875140")]
        public void Bug875140_01()
        {
            string source = @"
using System;
using System.Reflection;
 
class Program
{
    unsafe static void Main()
    {
        Action<dynamic, object> action = delegate { };
        void* p = Pointer.Unbox(Goo(action));
    }
 
    static T Goo<T>(Action<T, T> x) { throw null; }
}
";
            // PEVerify: [ : Program::Main][mdToken=0x6000001][offset 0x0000002C][found unmanaged pointer][expected unmanaged pointer] Unexpected type on the stack.
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithAllowUnsafe(true), verify: Verification.FailsPEVerify).VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.Single();
            var model = verifier.Compilation.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Goo").Single();
            Assert.Equal("System.Object Program.Goo<System.Object>(System.Action<System.Object, System.Object> x)", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(875140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875140")]
        public void Bug875140_02()
        {
            string source = @"
using System;
using System.Reflection;
 
class Program
{
    unsafe static void Main()
    {
        Action<object, dynamic> action = delegate { };
        void* p = Pointer.Unbox(Goo(action));
    }
 
    static T Goo<T>(Action<T, T> x) { throw null; }
}
";
            // PEVerify: [ : Program::Main][mdToken=0x6000001][offset 0x0000002C][found unmanaged pointer][expected unmanaged pointer] Unexpected type on the stack.
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll.WithAllowUnsafe(true), verify: Verification.FailsPEVerify).VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.Single();
            var model = verifier.Compilation.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Goo").Single();
            Assert.Equal("System.Object Program.Goo<System.Object>(System.Action<System.Object, System.Object> x)", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(875140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875140")]
        public void Bug875140_03()
        {
            string source = @"
using System;
using System.Reflection;
 
class Program
{
    unsafe static void Main()
    {
        Func<object, dynamic> action = null;
        void* p = Pointer.Unbox(Goo(action));
    }
 
    static T Goo<T>(Func<T, T> x) { throw null; }
}
";
            CreateCompilation(source, options: TestOptions.DebugDll.WithAllowUnsafe(true)).VerifyDiagnostics(
    // (10,33): error CS0411: The type arguments for method 'Program.Goo<T>(Func<T, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         void* p = Pointer.Unbox(Goo(action));
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Goo").WithArguments("Program.Goo<T>(System.Func<T, T>)").WithLocation(10, 33)
                );
        }

        [Fact, WorkItem(875140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875140")]
        public void Bug875140_04()
        {
            string source = @"
using System;
 
class Program
{
    static void Main()
    {
        Func<dynamic, object> action = null;
        Goo(action).M1();
    }
 
    static T Goo<T>(Func<T, T> x) { throw null; }
}
";
            var verifier = CompileAndVerify(source, new[] { CSharpRef }, options: TestOptions.DebugDll).VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.Single();
            var model = verifier.Compilation.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Goo").Single();
            Assert.Equal("dynamic Program.Goo<dynamic>(System.Func<dynamic, dynamic> x)", model.GetSymbolInfo(node).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1149588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1149588")]
        public void AccessPropertyWithoutArguments()
        {
            string source1 = @"
Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IB
    Property Value(Optional index As Object = Nothing) As Object
End Interface
";

            var reference = BasicCompilationUtils.CompileToMetadata(source1);

            string source2 = @"
class CIB : IB
{
    public dynamic get_Value(object index = null)
    {
        return ""Test"";
    }

    public void set_Value(object index = null, object Value = null)
    {
    }
}

class Test
{
    static void Main()
    {
        IB x = new CIB();
        System.Console.WriteLine(x.Value.Length);
    }
}
";

            var compilation2 = CreateCompilation(source2, new[] { reference.WithEmbedInteropTypes(true), CSharpRef }, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation2, expectedOutput: @"4");
        }

        [Fact, WorkItem(9945, "https://github.com/dotnet/roslyn/issues/9945")]
        public void DynamicGetOnlyProperty()
        {
            string source = @"
class Program
{
    static void Main()
    {
        I i = null;
        System.Type t = i.d.GetType();
    }

    interface I
    {
        dynamic d { set; }
    }
}
";
            var compilation = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            // crash happens during emit if not detected, so VerifyDiagnostics (no Emit) doesn't catch the crash.
            compilation.VerifyEmitDiagnostics(
                // (7,25): error CS0154: The property or indexer 'Program.I.d' cannot be used in this context because it lacks the get accessor
                //         System.Type t = i.d.GetType();
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "i.d").WithArguments("Program.I.d").WithLocation(7, 25)
            );
        }

        [Fact, WorkItem(9945, "https://github.com/dotnet/roslyn/issues/9945")]
        public void DynamicGetOnlyPropertyIndexer()
        {
            string source = @"
class Program
{
    static void Main()
    {
        I i = null;
        System.Type t = i[null].GetType();
    }

    interface I
    {
        dynamic this[string s] { set; }
    }
}
";
            var compilation = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            compilation.VerifyEmitDiagnostics(
                // (7,25): error CS0154: The property or indexer 'Program.I.this[string]' cannot be used in this context because it lacks the get accessor
                //         System.Type t = i[null].GetType();
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "i[null]").WithArguments("Program.I.this[string]").WithLocation(7, 25)
            );
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IncorrectArrayLength()
        {
            var il = @"
.assembly extern mscorlib { }
.assembly extern System.Core { }
.assembly IncorrectArrayLength { }

.class private auto ansi beforefieldinit D
       extends [mscorlib]System.Object
{
  .field public class Generic`2<object,object> MissingTrue
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
           = {bool[2](false true)}

  .field public class Generic`2<object,object> MissingFalse
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
           = {bool[2](false true)}

  .field public class Generic`2<object,object> ExtraTrue
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
           = {bool[4](false true false true)}

  .field public class Generic`2<object,object> ExtraFalse
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
           = {bool[4](false true false false)}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class D

.class public auto ansi beforefieldinit Generic`2<T,U>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Generic`2::.ctor

} // end of class Generic`2
";
            var comp = CreateCompilationWithILAndMscorlib40("", il, references: new[] { SystemCoreRef }, appendDefaultHeader: false);
            var global = comp.GlobalNamespace;
            var typeD = global.GetMember<NamedTypeSymbol>("D");
            var typeG = global.GetMember<NamedTypeSymbol>("Generic");
            var typeObject = comp.GetSpecialType(SpecialType.System_Object);
            var typeGConstructed = typeG.Construct(typeObject, typeObject);

            Assert.Equal(typeGConstructed, typeD.GetMember<FieldSymbol>("MissingTrue").Type);
            Assert.Equal(typeGConstructed, typeD.GetMember<FieldSymbol>("MissingFalse").Type);
            Assert.Equal(typeGConstructed, typeD.GetMember<FieldSymbol>("ExtraTrue").Type);
            Assert.Equal(typeGConstructed, typeD.GetMember<FieldSymbol>("ExtraFalse").Type);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(204561, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=204561&_a=edit")]
        public void SuppressDynamicIndexerAccessOffOfType_01()
        {
            var iLSource = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly Microsoft.Office.Interop.Excel
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 05 45 78 63 65 6C 00 00 )                   // ...Excel..
  .custom instance void [mscorlib]System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute::.ctor(int32,
                                                                                                        int32) = ( 01 00 01 00 00 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 31 33 2D 30 30 30 30   // ..$00020813-0000
                                                                                                  2D 30 30 30 30 2D 63 30 30 30 2D 30 30 30 30 30   // -0000-c000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibVersionAttribute::.ctor(int32,
                                                                                                int32) = ( 01 00 01 00 00 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 15:0:0:0
}
.module Excel.dll
// MVID: {C7C599B3-5C80-48BC-9637-7CADFEF6DEB8}
.imagebase 0x00400000
.file alignment 0x00001000
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000009    //  ILONLY
// Image base: 0x050E0000

.class interface public abstract auto ansi import Microsoft.Office.Interop.Excel.Worksheet
       implements Microsoft.Office.Interop.Excel._Worksheet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 44 38 2D 30 30 30 30   // ..$000208D8-0000
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30   // -0000-C000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..
} // end of class Microsoft.Office.Interop.Excel.Worksheet

.class interface public abstract auto ansi import Microsoft.Office.Interop.Excel._Worksheet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 C0 10 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 44 38 2D 30 30 30 30   // ..$000208D8-0000
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30   // -0000-C000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.Office.Interop.Excel.Range 
          marshal( interface ) 
          get_Range([in] object  marshal( struct) Cell1,
                    [in][opt] object  marshal( struct) Cell2) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
  } // end of method _Worksheet::get_Range

  .property class Microsoft.Office.Interop.Excel.Range
          Range(object,
                object)
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance class Microsoft.Office.Interop.Excel.Range Microsoft.Office.Interop.Excel._Worksheet::get_Range(object,
                                                                                                                  object)
  } // end of property _Worksheet::Range

  .method public hidebysig newslot specialname abstract virtual 
          instance class Microsoft.Office.Interop.Excel.Range 
          marshal( interface ) 
          MRange([in] object  marshal( struct) Cell1,
                    [in][opt] object  marshal( struct) Cell2) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
  } // end of method _Worksheet::get_Range
}

.class interface public abstract auto ansi import Microsoft.Office.Interop.Excel.Range
       implements [mscorlib]System.Collections.IEnumerable
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 02 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 08 5F 44 65 66 61 75 6C 74 00 00 )          // ..._Default..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 34 36 2D 30 30 30 30   // ..$00020846-0000
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30   // -0000-C000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 00 10 00 00 ) 
}
";

            MetadataReference reference = CompileIL(iLSource, prependDefaultHeader: false, embedInteropTypes: false);

            string consumer1 = @"
using Microsoft.Office.Interop.Excel;

class Test
{
    public static void Main()
    {
        dynamic x = 1;
        dynamic y = 1;
        
        var z2 = Worksheet.MRange(x, y);
    }
}
";

            var compilation1 = CreateCompilation(consumer1, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { reference, CSharpRef });

            compilation1.VerifyDiagnostics(
                // (11,18): error CS0120: An object reference is required for the non-static field, method, or property '_Worksheet.MRange(object, object)'
                //         var z2 = Worksheet.MRange(x, y);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Worksheet.MRange").WithArguments("Microsoft.Office.Interop.Excel._Worksheet.MRange(object, object)").WithLocation(11, 18)
                );

            string consumer2 = @"
using Microsoft.Office.Interop.Excel;

class Test
{
    public static void Main()
    {
        dynamic x = 1;
        dynamic y = 1;
        var z1 = Worksheet.Range[x, y];
    }
}
";

            var compilation2 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { reference, CSharpRef });

            compilation2.VerifyDiagnostics(
                // (10,18): error CS0120: An object reference is required for the non-static field, method, or property '_Worksheet.Range[object, object]'
                //         var z1 = Worksheet.Range[x, y];
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Worksheet.Range[x, y]").WithArguments("Microsoft.Office.Interop.Excel._Worksheet.Range[object, object]").WithLocation(10, 18)
                );
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(204561, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=204561&_a=edit")]
        public void SuppressDynamicIndexerAccessOffOfType_02()
        {
            var iLSource = @"
.class public auto ansi WithIndexer
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method WithIndexer::.ctor

  .method public specialname static object 
          get_Indexer(object x,
                      object y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""Indexer""
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldnull
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method WithIndexer::get_Indexer

  .method public specialname static void 
          set_Indexer(object x,
                      object y,
                      object 'value') cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method WithIndexer::set_Indexer

  .method public static object  MIndexer(object x,
                                         object y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""MIndexer""
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldnull
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method WithIndexer::MIndexer

  .property object Indexer(object,
                           object)
  {
    .get object WithIndexer::get_Indexer(object,
                                         object)
    .set void WithIndexer::set_Indexer(object,
                                       object,
                                       object)
  } // end of property WithIndexer::Indexer
} // end of class WithIndexer
";

            MetadataReference reference = CompileIL(iLSource, prependDefaultHeader: true, embedInteropTypes: false);

            string consumer1 = @"
class Test
{
    public static void Main()
    {
        dynamic x = 1;
        dynamic y = 1;
        var z2 = WithIndexer.MIndexer(x, y);
    }
}";

            var compilation1 = CreateCompilation(consumer1, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { reference, CSharpRef });

            CompileAndVerify(compilation1, expectedOutput: "MIndexer").VerifyDiagnostics();

            string consumer2 = @"
class Test
{
    public static void Main()
    {
        dynamic x = 1;
        dynamic y = 1;
        var z1 = WithIndexer.Indexer[x, y];
    }
}";

            var compilation2 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { reference, CSharpRef });

            compilation2.VerifyDiagnostics(
                // (8,30): error CS1545: Property, indexer, or event 'WithIndexer.Indexer[object, object]' is not supported by the language; try directly calling accessor methods 'WithIndexer.get_Indexer(object, object)' or 'WithIndexer.set_Indexer(object, object, object)'
                //         var z1 = WithIndexer.Indexer[x, y];
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Indexer").WithArguments("WithIndexer.Indexer[object, object]", "WithIndexer.get_Indexer(object, object)", "WithIndexer.set_Indexer(object, object, object)").WithLocation(8, 30)
                );
        }

        [WorkItem(22813, "https://github.com/dotnet/roslyn/issues/22813")]
        [Fact]
        public void InArgumentDynamic()
        {
            string source = @"
class C
{
    static void M1()
    {
        int x = 42;
        dynamic d = null;
        d.M2(in x);
    }
}
";

            var comp = CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.Regular7_2);

            comp.VerifyEmitDiagnostics(
                // (8,17): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         d.M2(in x);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "x").WithLocation(8, 17)
                );
        }

        [WorkItem(22813, "https://github.com/dotnet/roslyn/issues/22813")]
        [Fact]
        public void InArgumentDynamic2()
        {
            string source = @"
class C
{
    static void M1()
    {
        int x = 42;
        dynamic d = null;
        d.M2(1, in d, 123, in x);
    }
}
";

            var comp = CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.Regular7_2);

            comp.VerifyEmitDiagnostics(
                // (8,20): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         d.M2(1, in d, 123, in x);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "d").WithLocation(8, 20),
                // (8,31): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         d.M2(1, in d, 123, in x);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "x").WithLocation(8, 31)
                );
        }

        [WorkItem(22813, "https://github.com/dotnet/roslyn/issues/22813")]
        [Fact]
        public void InArgumentDynamicLocalFunction()
        {
            string source = @"
class C
{
    private static void M1(in dynamic x, int y, in dynamic z) => System.Console.WriteLine(x == y);

    static void Main()
    {
        dynamic d = 1;

        M1(in d, d = 2, in d);

        void M2(in dynamic x, int y, in dynamic z) => System.Console.WriteLine(x == y);

        M2(in d, d = 3, in d);
    }
}
";

            var comp = CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput:
@"
True
True
").VerifyDiagnostics();

            comp = CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.Regular7_2, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (10,15): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         M1(in d, d = 2, in d);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "d").WithLocation(10, 15),
                // (10,28): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         M1(in d, d = 2, in d);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "d").WithLocation(10, 28)
                );
        }

        [WorkItem(22813, "https://github.com/dotnet/roslyn/issues/22813")]
        [Fact]
        public void InArgumentDynamicCtor()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = 42;
        dynamic d = 1;
        var y = new M2(d, in x);
    }

    class M2
    {
        public M2(int a, in int d) => System.Console.Write(1);
        public M2(long a, in int d) => System.Console.Write(2);
    }
}";

            var comp = CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.Regular7_2);

            comp.VerifyEmitDiagnostics(
                // (8,30): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         var y = new M2(d, in x);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "x").WithLocation(8, 30)
                );
        }

        [WorkItem(22813, "https://github.com/dotnet/roslyn/issues/22813")]
        [Fact]
        public void InArgumentDynamicIndexer()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = 42;
        dynamic d = new C1();
        System.Console.WriteLine(d[in x]);
    }

    class C1
    {
        public int this[in int x] => x;
    }
}";

            var comp = CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.Regular7_2);

            comp.VerifyEmitDiagnostics(
                // (8,39): error CS8364: Arguments with 'in' modifier cannot be used in dynamically dispatched expressions.
                //         System.Console.WriteLine(d[in x]);
                Diagnostic(ErrorCode.ERR_InDynamicMethodArg, "x").WithLocation(8, 39)
                );
        }

        [Fact]
        public void UserDefinedConversion_01()
        {
            var source = @"
dynamic x = true;

if (new C() && x)
{
    System.Console.WriteLine(""1"");
}

System.Console.WriteLine(""2"");

class C
{
    [System.Obsolete()]
    public static implicit operator bool(C c)
    {
        System.Console.WriteLine(""op_Implicit"");
        return false;
    }

    public static bool operator true(C c)
    {
        System.Console.WriteLine(""op_True"");
        return false;
    }

    public static bool operator false(C c)
    {
        System.Console.WriteLine(""op_False"");
        return false;
    }
}
";

            var compilation = CreateCompilationWithMscorlib45AndCSharp(source);

            CompileAndVerify(compilation, expectedOutput:
@"op_Implicit
op_Implicit
2
"
).VerifyDiagnostics(
                // (4,5): warning CS0612: 'C.implicit operator bool(C)' is obsolete
                // if (new C() && x)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()").WithArguments("C.implicit operator bool(C)").WithLocation(4, 5)
                );
        }

        [Fact]
        public void InapplicableMethodInDerived()
        {
            string source = @"
class C
{
    static void Main()
    {
        dynamic d = 1;
        System.Console.WriteLine(new C2().Add(d));
    }
}

class C1
{
    public string Add(int x) => ""int"";
}

class C2 : C1
{
    public string Add(string x) => ""string"";
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            CompileAndVerify(comp, expectedOutput: "int").VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ResultIsDynamic_01(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    object Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1.Test(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Object I1.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.TargetMethod.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1).VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((I1 i1, dynamic value) => i1.Test(""name"", value));
        Print(expr);
    }

    static Expression<Func<I1, dynamic, T>> GetExpression<T>(Expression<Func<I1, dynamic, T>> ex) => ex;
    static void Print<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
    {
        System.Console.Write(typeof(T3));
        System.Console.Write("" "");
        System.Console.Write(expr);
    }
}

public interface I1
{
    object Test(string name, object value);
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: parseOptions);
            CompileAndVerify(comp2,
                expectedOutput: @"System.Object (i1, value) => Convert(i1.Test(""name"", value)" + (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Object)" : ")")).VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    object? Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);
            comp3.VerifyDiagnostics(
                // (9,46): warning CS8604: Possible null reference argument for parameter 'c' in 'C JsonSerializer.Deserialize<C>(Stream c)'.
                //         return JsonSerializer.Deserialize<C>(result);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "result").WithArguments("c", "C JsonSerializer.Deserialize<C>(Stream c)").WithLocation(9, 46)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ResultIsDynamic_02(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    int Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1.Test(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Int32 I1.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.TargetMethod.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1).VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((I1 i1, dynamic value) => i1.Test(""name"", value));
        Print(expr);
    }

    static Expression<Func<I1, dynamic, T>> GetExpression<T>(Expression<Func<I1, dynamic, T>> ex) => ex;
    static void Print<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
    {
        System.Console.Write(typeof(T3));
        System.Console.Write("" "");
        System.Console.Write(expr);
    }
}

public interface I1
{
    object Test(string name, object value);
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: parseOptions);
            CompileAndVerify(comp2,
                expectedOutput: @"System.Object (i1, value) => Convert(i1.Test(""name"", value)" + (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Object)" : ")")).VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    int? Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);
            comp3.VerifyDiagnostics(
                // (9,46): warning CS8604: Possible null reference argument for parameter 'c' in 'C JsonSerializer.Deserialize<C>(Stream c)'.
                //         return JsonSerializer.Deserialize<C>(result);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "result").WithArguments("c", "C JsonSerializer.Deserialize<C>(Stream c)").WithLocation(9, 46)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ResultIsDynamic_03(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    dynamic Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1.Test(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("dynamic I1.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.TargetMethod.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1).VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((I1 i1, dynamic value) => i1.Test(""name"", value));
        Print(expr);
    }

    static Expression<Func<I1, dynamic, T>> GetExpression<T>(Expression<Func<I1, dynamic, T>> ex) => ex;
    static void Print<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
    {
        System.Console.Write(typeof(T3));
        System.Console.Write("" "");
        System.Console.Write(expr);
    }
}

public interface I1
{
    dynamic Test(string name, object value);
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: parseOptions);
            CompileAndVerify(comp2,
                expectedOutput: @"System.Object (i1, value) => i1.Test(""name"", value)").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    dynamic? Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);
            comp3.VerifyDiagnostics(
                // (9,46): warning CS8604: Possible null reference argument for parameter 'c' in 'C JsonSerializer.Deserialize<C>(Stream c)'.
                //         return JsonSerializer.Deserialize<C>(result);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "result").WithArguments("c", "C JsonSerializer.Deserialize<C>(Stream c)").WithLocation(9, 46)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_Extension(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = new C().Test(""name"", d);
        System.Console.Write(result);        
    }
}

static class Extensions
{
    public static int Test(this C c, string name, object value) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"new C().Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_01()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(name: ""name"", d);
        System.Console.Write(result);        
    }

    static int Test(string name, object value) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(name: ""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_02()
        {
            string source1 = @"
#pragma warning disable //CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('string')

unsafe public class C
{
    static void Main()
    {
        string name = ""name"";
        dynamic d = 1;
        var result = Test(&name, d);
        System.Console.Write(result);        
    }

    static int Test(string* name, object value) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe.WithAllowUnsafe(true), targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(&name, d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Int32 C.Test(System.String* name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_03(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(""name"", d);
        System.Console.Write(result);        
    }

    static int Test(string name, object value, params System.Collections.Generic.List<int> list) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.Test(System.String name, System.Object value, params System.Collections.Generic.List<System.Int32> list)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_ParamArray()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(""name"", d);
        System.Console.Write(result);        
    }

    static int Test(string name, object value, params int[] list) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.Test(System.String name, System.Object value, params System.Int32[] list)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_VoidReturning()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var a = Test1(d);
    }

    static void Test1(int x) {}
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);

            comp.VerifyDiagnostics(
                // (7,13): error CS0815: Cannot assign void to an implicitly-typed variable
                //         var a = Test1(d);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "a = Test1(d)").WithArguments("void").WithLocation(7, 13)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        Test1(d)++;
        var a = Test1(d);
        System.Console.WriteLine(a);        
    }

    static int _test1 = 0;    
    static ref int Test1(int x) => ref _test1;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_NoConversion()
        {
            string source = @"
unsafe public class C
{
    public static void Main()
    {
        int v = 0;
        _test1 = &v;

        dynamic d = 1;
        (*Test1(d))++;
        var a = Test1(d);
        System.Console.WriteLine(*a);        
    }

    static int* _test1;    
    static int* Test1(int x) => _test1;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32* a", symbolInfo.Symbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_LocalFunction([CombinatorialValues(0, 12, 13)] int version)
        {
            var parseOptions = version switch { 12 => TestOptions.Regular12, 13 => TestOptions.RegularNext, _ => TestOptions.RegularPreview };

            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var e = test4(d);
        System.Console.WriteLine(e);        

        static int test4(int x) => x;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "e").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 e", symbolInfo.Symbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ResultIsDynamic_Delegate(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source = @"
public class C
{
    static C M(Test i1, dynamic value)
    {
        var result = i1(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

delegate object Test(string name, object value);

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T);
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Object Test.Invoke(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.TargetMethod.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_Delegate_01()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(name: ""name"", d);
        System.Console.Write(result);        
    }

    static D Test = (string name, object value) => 123;
    delegate int D(string name, object value);
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(name: ""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.D.Invoke(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_Delegate_02()
        {
            string source1 = @"
#pragma warning disable //CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('string')

unsafe public class C
{
    static void Main()
    {
        string name = ""name"";
        dynamic d = 1;
        var result = Test(&name, d);
        System.Console.Write(result);        
    }

    static D Test = (string* name, object value) => 123;
    delegate int D(string* name, object value);
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe.WithAllowUnsafe(true), targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(&name, d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Int32 C.D.Invoke(System.String* name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_Delegate_03(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(""name"", d);
        System.Console.Write(result);        
    }

    static D Test = (string name, object value, params System.Collections.Generic.List<int> list) => 123;
    delegate int D(string name, object value, params System.Collections.Generic.List<int> list);
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.D.Invoke(System.String name, System.Object value, params System.Collections.Generic.List<System.Int32> list)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_ParamArray_Delegate()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(""name"", d);
        System.Console.Write(result);        
    }

    static D Test = (string name, object value, params int[] list) => 123;
    delegate int D(string name, object value, params int[] list);
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.D.Invoke(System.String name, System.Object value, params System.Int32[] list)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_VoidReturning_Delegate()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var a = Test1(d);
    }

    static D Test1 = null;
}

delegate void D(int x);
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);

            comp.VerifyDiagnostics(
                // (7,13): error CS0815: Cannot assign void to an implicitly-typed variable
                //         var a = Test1(d);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "a = Test1(d)").WithArguments("void").WithLocation(7, 13)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Delegate()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        Test1(d)++;
        var a = Test1(d);
        System.Console.WriteLine(a);        
    }

    static int _test1 = 0;    
    static D Test1 = (int x) => ref _test1;

    delegate ref int D(int x);
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_NoConversion_Delegate()
        {
            string source = @"
unsafe public class C
{
    public static void Main()
    {
        int v = 0;
        _test1 = &v;

        dynamic d = 1;
        (*Test1(d))++;
        var a = Test1(d);
        System.Console.WriteLine(*a);        
    }

    static int* _test1;    
    static D Test1 = (int x) => _test1;
    delegate int* D(int x);
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32* a", symbolInfo.Symbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ResultIsDynamic_Property_01(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    object this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Object I1.this[System.String name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.Property.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((I1 i1, dynamic value) => i1[""name"", value]);
        Print(expr);
    }

    static Expression<Func<I1, dynamic, T>> GetExpression<T>(Expression<Func<I1, dynamic, T>> ex) => ex;
    static void Print<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
    {
        System.Console.Write(typeof(T3));
        System.Console.Write("" "");
        System.Console.Write(expr);
    }
}

public interface I1
{
    object this[string name, object value] {get;}
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: parseOptions);
            CompileAndVerify(comp2,
                expectedOutput: @"System.Object (i1, value) => Convert(i1.get_Item(""name"", value)" + (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Object)" : ")")).VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    object? this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);
            comp3.VerifyDiagnostics(
                // (9,46): warning CS8604: Possible null reference argument for parameter 'c' in 'C JsonSerializer.Deserialize<C>(Stream c)'.
                //         return JsonSerializer.Deserialize<C>(result);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "result").WithArguments("c", "C JsonSerializer.Deserialize<C>(Stream c)").WithLocation(9, 46)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ResultIsDynamic_Property_02(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    int this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 I1.this[System.String name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.Property.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((I1 i1, dynamic value) => i1[""name"", value]);
        Print(expr);
    }

    static Expression<Func<I1, dynamic, T>> GetExpression<T>(Expression<Func<I1, dynamic, T>> ex) => ex;
    static void Print<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
    {
        System.Console.Write(typeof(T3));
        System.Console.Write("" "");
        System.Console.Write(expr);
    }
}

public interface I1
{
    int this[string name, object value] {get;}
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: parseOptions);
            CompileAndVerify(comp2,
                expectedOutput: @"System.Object (i1, value) => Convert(i1.get_Item(""name"", value)" + (ExecutionConditionUtil.IsMonoOrCoreClr ? ", Object)" : ")")).VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    int? this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);
            comp3.VerifyDiagnostics(
                // (9,46): warning CS8604: Possible null reference argument for parameter 'c' in 'C JsonSerializer.Deserialize<C>(Stream c)'.
                //         return JsonSerializer.Deserialize<C>(result);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "result").WithArguments("c", "C JsonSerializer.Deserialize<C>(Stream c)").WithLocation(9, 46)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_03()
        {
            string source = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    dynamic this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic I1.this[System.String name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.Property.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((I1 i1, dynamic value) => i1[""name"", value]);
        Print(expr);
    }

    static Expression<Func<I1, dynamic, T>> GetExpression<T>(Expression<Func<I1, dynamic, T>> ex) => ex;
    static void Print<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
    {
        System.Console.Write(typeof(T3));
        System.Console.Write("" "");
        System.Console.Write(expr);
    }
}

public interface I1
{
    dynamic this[string name, object value] {get;}
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            CompileAndVerify(comp2,
                expectedOutput: @"System.Object (i1, value) => i1.get_Item(""name"", value)").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    dynamic? this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (9,46): warning CS8604: Possible null reference argument for parameter 'c' in 'C JsonSerializer.Deserialize<C>(Stream c)'.
                //         return JsonSerializer.Deserialize<C>(result);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "result").WithArguments("c", "C JsonSerializer.Deserialize<C>(Stream c)").WithLocation(9, 46)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_Property_01()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = new C()[name: ""name"", d];
        System.Console.Write(result);        
    }

    int this[string name, object value] => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            // This is surprising, but this scenario used to successfully bind dynamically before (unlike a call).
            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.String name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), operation.Property.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_Property_02()
        {
            string source1 = @"
#pragma warning disable //CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('string')

unsafe public class C
{
    static void Main()
    {
        string name = ""name"";
        dynamic d = 1;
        var result = new C()[&name, d];
        System.Console.Write(result);        
    }

    int this[string* name, object value] => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe.WithAllowUnsafe(true), targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.String* name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            CompileAndVerify(comp1, expectedOutput: "123", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [CombinatorialData]
        public void SingleCandidate_ArgumentsNotSupportedByDynamic_Property_03(bool testPreview)
        {
            var parseOptions = testPreview ? TestOptions.RegularPreview : TestOptions.RegularNext;

            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = new C()[""name"", d];
        System.Console.Write(result);        
    }

    int this[string name, object value, params System.Collections.Generic.List<int> list] => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: parseOptions);

            // This is surprising, but this scenario used to successfully bind dynamically before (unlike a call).
            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 result", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.String name, System.Object value, params System.Collections.Generic.List<System.Int32> list] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_ParamArray_Property()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = new C()[""name"", d];
        System.Console.Write(result);        
    }

    int this[string name, object value, params int[] list] => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            // This is surprising, but this scenario used to successfully bind dynamically before (unlike a call).
            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.String name, System.Object value, params System.Int32[] list] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        c[d]++;
        var a = c[d];
        System.Console.WriteLine(a);        
    }

    int _test1 = 0;    
    ref int this[int x] => ref _test1;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            TypeInfo typeInfo;

            foreach (var elementAccess in tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                symbolInfo = model.GetSymbolInfo(elementAccess);
                AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
                typeInfo = model.GetTypeInfo(elementAccess);
                AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

                var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
                AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
                Assert.Equal(typeInfo.Type, propertyRef.Type);
            }

            var increment = tree.GetRoot().DescendantNodes().OfType<PostfixUnaryExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(increment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_NoConversion_Property()
        {
            string source = @"
unsafe public class C
{
    public static void Main()
    {
        int v = 0;
        var c = new C();
        c._test1 = &v;

        dynamic d = 1;
        (*c[d])++;
        var a = c[d];
        System.Console.WriteLine(*a);        
    }

    int* _test1;    
    int* this[int x] => _test1;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32* a", symbolInfo.Symbol.ToTestDisplayString());

            TypeInfo typeInfo;

            foreach (var elementAccess in tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                symbolInfo = model.GetSymbolInfo(elementAccess);
                AssertEx.Equal("System.Int32* C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
                typeInfo = model.GetTypeInfo(elementAccess);
                AssertEx.Equal("System.Int32*", typeInfo.Type.ToTestDisplayString());
                AssertEx.Equal("System.Int32*", typeInfo.ConvertedType.ToTestDisplayString());

                var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
                AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
                Assert.Equal(typeInfo.Type, propertyRef.Type);
            }

            CompileAndVerify(comp, expectedOutput: "1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] = 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] = (int?)null;
        Print(a);        
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] = 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] = (int?)null;
        Print(a);        
    }

    int? _test1 = 0;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_03()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        dynamic right = 2;
        var a = c[d] = right;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        dynamic? right = (int?)null;
        var a = c[d] = right;
        Print(a);        
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (12,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(12, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_RightSideIsConvertedStatically()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        object o = null;
        var c = new C();
        var a = c[d] = o;
        System.Console.Write(a);        
    }

    System.IO.Stream this[int x]
    {
        get => null;
        set {}
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic a", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.IO.Stream C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.IO.Stream", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.IO.Stream", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.IO.Stream", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.IO.Stream", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Object", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.IO.Stream", typeInfo.ConvertedType.ToTestDisplayString());

            comp.VerifyDiagnostics(
                // (9,24): error CS0266: Cannot implicitly convert type 'object' to 'System.IO.Stream'. An explicit conversion exists (are you missing a cast?)
                //         var a = c[d] = o;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "o").WithArguments("object", "System.IO.Stream").WithLocation(9, 24)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_Assignment()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] = 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    ref int this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] = (int?)null;
        Print(a);        
    }

    int? _test1 = 0;    
    ref int? this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_CompoundAssignment_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (ICompoundAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += (int?)null;
        Print(a);        
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (10,17): warning CS0458: The result of the expression is always 'null' of type 'dynamic'
                //         var a = c[d] += (int?)null;
                Diagnostic(ErrorCode.WRN_AlwaysNull, "c[d] += (int?)null").WithArguments("dynamic").WithLocation(10, 17),
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72906")]
        public void SingleCandidate_ResultIsDynamic_Property_CompoundAssignment_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("dynamic dynamic.op_Addition(dynamic left, System.Int32 right)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (ICompoundAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += (int?)null;
        Print(a);        
    }

    int? _test1 = 0;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullable analysis behavior is consistent with how dynamic operators are analyzed.
            // See https://github.com/dotnet/roslyn/issues/72906, for example.
            comp3.VerifyDiagnostics();

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72906")]
        public void SingleCandidate_ResultIsDynamic_Property_CompoundAssignment_03()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += d;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("dynamic dynamic.op_Addition(System.Int32 left, dynamic right)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (ICompoundAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "1 1").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        dynamic? right = null;
        var c = new C();
        var a = c[d] += right;
        Print(a);        
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullable analysis behavior is consistent with how dynamic operators are analyzed.
            // See https://github.com/dotnet/roslyn/issues/72906, for example.
            comp3.VerifyDiagnostics();

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_CompoundAssignment_OperatorIsBoundStatically()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        C2 right = new C2();
        var a = c[d] += right;
        Print(a);        
    }

    C2 this[int x]
    {
        get => new C2();
        set {}
    }

    static void Print(dynamic b) 
    {
    }
}

class C2 {}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("?", typeInfo.Type.ToTestDisplayString());
            Assert.True(typeInfo.Type.IsErrorType());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("C2 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("?", typeInfo.Type.ToTestDisplayString());
            Assert.True(typeInfo.Type.IsErrorType());
            AssertEx.Equal("?", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            Assert.Null(symbolInfo.Symbol);

            var operation = (ICompoundAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("C2", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("C2", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("?", operation.Type.ToTestDisplayString());
            Assert.True(operation.Type.IsErrorType());
            Assert.Null(operation.OperatorMethod);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            comp.VerifyDiagnostics(
                // (9,17): error CS0019: Operator '+=' cannot be applied to operands of type 'C2' and 'C2'
                //         var a = c[d] += right;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c[d] += right").WithArguments("+=", "C2", "C2").WithLocation(9, 17)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_CompoundAssignment()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    ref int this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)", symbolInfo.Symbol.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] += (int?)null;
        Print(a);        
    }

    int? _test1 = 0;    
    ref int? this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (10,17): warning CS0458: The result of the expression is always 'null' of type 'int?'
                //         var a = c[d] += (int?)null;
                Diagnostic(ErrorCode.WRN_AlwaysNull, "c[d] += (int?)null").WithArguments("int?").WithLocation(10, 17),
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_PostfixIncrement_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d]++;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 2;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (PostfixUnaryExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("System.Int32 System.Int32.op_Increment(System.Int32 value)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (IIncrementOrDecrementOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            CompileAndVerify(comp, expectedOutput: "2 3").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d]++;
        Print(a);        
    }

    int? _test1 = 2;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_PostfixIncrement_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d]++;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 2;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (PostfixUnaryExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("dynamic dynamic.op_Increment(dynamic value)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (IIncrementOrDecrementOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            CompileAndVerify(comp, expectedOutput: "2 3").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d]++;
        Print(a);        
    }

    int? _test1 = 2;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_PostfixIncrement_OperatorIsBoundStatically()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d]++;
        Print(a);        
    }


    C2 this[int x]
    {
        get => new C2();
        set {}
    }

    static void Print(dynamic b) 
    {
    }
}

class C2 {}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("?", typeInfo.Type.ToTestDisplayString());
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.Equal(CodeAnalysis.NullableFlowState.None, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("C2 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (PostfixUnaryExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("?", typeInfo.Type.ToTestDisplayString());
            Assert.True(typeInfo.Type.IsErrorType());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            symbolInfo = model.GetSymbolInfo(assignment);
            Assert.Null(symbolInfo.Symbol);

            var operation = (IIncrementOrDecrementOperation)model.GetOperation(assignment);
            AssertEx.Equal("C2", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("?", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            comp.VerifyDiagnostics(
                // (8,17): error CS0023: Operator '++' cannot be applied to operand of type 'C2'
                //         var a = c[d]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "c[d]++").WithArguments("++", "C2").WithLocation(8, 17)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_PrefixIncrement_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ++c[d];
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 2;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (PrefixUnaryExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("System.Int32 System.Int32.op_Increment(System.Int32 value)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (IIncrementOrDecrementOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            CompileAndVerify(comp, expectedOutput: "3 3").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ++c[d];
        Print(a);        
    }

    int? _test1 = 2;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_PrefixIncrement_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ++c[d];
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 2;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (PrefixUnaryExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("dynamic dynamic.op_Increment(dynamic value)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (IIncrementOrDecrementOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            CompileAndVerify(comp, expectedOutput: "3 3").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ++c[d];
        Print(a);        
    }

    int? _test1 = 2;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_PrefixIncrement()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ++c[d];
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 2;    
    ref int this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (PrefixUnaryExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
            symbolInfo = model.GetSymbolInfo(assignment);
            AssertEx.Equal("System.Int32 System.Int32.op_Increment(System.Int32 value)", symbolInfo.Symbol.ToTestDisplayString());

            var operation = (IIncrementOrDecrementOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());
            Assert.Null(operation.OperatorMethod);

            CompileAndVerify(comp, expectedOutput: "3 3").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ++c[d];
        Print(a);        
    }

    int? _test1 = 2;    
    ref int? this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_ConditionalAssignment_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= ""2"";
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    string _test1 = null!;    
    string this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.String C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.String", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= (string?)null;
        Print(a);        
    }

    string? _test1 = null;    
    string? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_ConditionalAssignment_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int? _test1 = null;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32? C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32?", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32?", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= (int?)null;
        Print(a);        
    }

    int? _test1 = null;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_ConditionalAssignment_03()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= ""2"";
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    string _test1 = null!;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= (string?)null;
        Print(a);        
    }

    string? _test1 = null;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_ConditionalAssignment_04()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        dynamic right = ""2"";
        var a = c[d] ??= right;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    string _test1 = null!;    
    string this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.String C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.String", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        dynamic? right = null;
        var a = c[d] ??= right;
        Print(a);        
    }

    string? _test1 = null;    
    string? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (12,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a);        
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a").WithArguments("b", "void C.Print(dynamic b)").WithLocation(12, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72912")]
        public void SingleCandidate_ResultIsDynamic_Property_ConditionalAssignment_RightSideIsConvertedStatically()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= ""2"";
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    C2? _test1 = null;    
    C2? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}

class C2 {}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("C2? a", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("C2? C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("C2?", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2?", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("?", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("?", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("C2?", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("?", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());

            // The unexpected nullability warning is pre-existing condition - https://github.com/dotnet/roslyn/issues/72912
            comp.VerifyDiagnostics(
                // (10,17): error CS0019: Operator '??=' cannot be applied to operands of type 'C2' and 'string'
                //         var a = c[d] ??= "2";
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"c[d] ??= ""2""").WithArguments("??=", "C2", "string").WithLocation(10, 17),
                // (10,26): warning CS8619: Nullability of reference types in value of type 'string' doesn't match target type 'C2'.
                //         var a = c[d] ??= "2";
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, @"""2""").WithArguments("string", "C2").WithLocation(10, 26)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_ConditionalAssignment_Error()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= new C2();
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    C2 _test1;    
    C2 this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}

struct C2 {}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("? a", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("C2 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("?", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("?", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("C2", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("C2", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("?", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            comp.VerifyDiagnostics(
                // (8,17): error CS0019: Operator '??=' cannot be applied to operands of type 'C2' and 'C2'
                //         var a = c[d] ??= new C2();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c[d] ??= new C2()").WithArguments("??=", "C2", "C2").WithLocation(8, 17)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_ConditionalAssignment()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = c[d] ??= 2;
        Print(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int? _test1 = null;    
    ref int? this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
        System.Console.Write(b);        
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 a", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32? C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32?", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32?", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2 2").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_Assignment_01()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = 2 };
        System.Console.WriteLine(c[1]);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = 2 });
        Print(expr);
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = 2 });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = 2 });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_Assignment_02()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = 2 };
        System.Console.WriteLine(c[1]);        
    }

    int _test1 = 0;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = 2 });
        Print(expr);
    }

    int _test1 = 0;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = 2 });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = 2 });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_Assignment_03()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        dynamic v = 2;
        var c = new C() { [d] = v };
        System.Console.WriteLine(c[1]);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d, dynamic v) => new C() { [d] = v });
        Print(expr);
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, dynamic, C>> GetExpression(Expression<Func<dynamic, dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,70): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d, dynamic v) => new C() { [d] = v });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 70),
                // (9,71): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d, dynamic v) => new C() { [d] = v });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 71),
                // (9,76): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d, dynamic v) => new C() { [d] = v });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "v").WithLocation(9, 76)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_Assignment_RightSideIsConvertedStatically()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = ""2"" };
        System.Console.WriteLine(c[1]);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.String", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            comp.VerifyDiagnostics(
                // (7,33): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         var c = new C() { [d] = "2" };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""").WithArguments("string", "int").WithLocation(7, 33)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72916")]
        public void SingleCandidate_RefReturning_Property_MemberInitializer_Assignment()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = 2 };
        System.Console.WriteLine(c[1]);        
    }

    int _test1 = 0;    
    ref int this[int x]
    {
        get => ref _test1;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            // IInvalidOperation is pre-existing condition - https://github.com/dotnet/roslyn/issues/72916
            var propertyRef = (IInvalidOperation)model.GetOperation(elementAccess);
            //var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            //AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IAssignmentOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Int32", operation.Target.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Value.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", operation.Type.ToTestDisplayString());

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_ObjectInitializer_01()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = { F = 2 } };
        System.Console.WriteLine(c[1].F);        
    }

    C2 _test1 = new C2();    
    C2 this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
    public int F = 0;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("C2 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IMemberInitializerOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
        Print(expr);
    }

    C2 _test1 = new C2();    
    C2 this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}

class C2
{
    public int F = 0;
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60),
                // (9,67): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "F").WithLocation(9, 67)
                );

            string source3 = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = { F = 2 } };
    }

    C2 _test1 = new C2();    
    C2 this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
}
";

            var comp3 = CreateCompilation(source3, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);
            CompileAndVerify(comp3).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_ObjectInitializer_02()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = { F = 2 } };
        System.Console.WriteLine(c[1].F);        
    }

    C2 _test1 = new C2();    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
    public int F = 0;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IMemberInitializerOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
        Print(expr);
    }

    C2 _test1 = new C2();    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}

class C2
{
    public int F = 0;
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60),
                // (9,67): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "F").WithLocation(9, 67)
                );

            string source3 = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = { F = 2 } };
    }

    C2 _test1 = new C2();    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
}
";

            var comp3 = CreateCompilation(source3, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);
            CompileAndVerify(comp3).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_MemberInitializer_ObjectInitializer()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = { F = 2 } };
        System.Console.WriteLine(c[1].F);        
    }

    C2 _test1 = new C2();    
    ref C2 this[int x]
    {
        get => ref _test1;
    }
}

class C2
{
    public int F = 0;
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref C2 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IMemberInitializerOperation)model.GetOperation(assignment);
            AssertEx.Equal("C2", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
        Print(expr);
    }

    C2 _test1 = new C2();    
    ref C2 this[int x]
    {
        get => ref _test1;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}

class C2
{
    public int F = 0;
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,59): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = { F = 2 } });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60)
                );

            string source3 = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = { F = 2 } };
    }

    C2 _test1 = new C2();    
    ref C2 this[int x]
    {
        get => ref _test1;
    }
}

class C2
{
}
";

            var comp3 = CreateCompilation(source3, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (7,35): error CS0117: 'C2' does not contain a definition for 'F'
                //         var c = new C() { [d] = { F = 2 } };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "F").WithArguments("C2", "F").WithLocation(7, 35)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_CollectionInitializer_01()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = {2} };
        System.Console.WriteLine(c[1][0]);        
    }

    System.Collections.Generic.List<int> _test1 = new System.Collections.Generic.List<int>();    
    System.Collections.Generic.List<int> this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Collections.Generic.List<System.Int32> C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IMemberInitializerOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
        Print(expr);
    }

    System.Collections.Generic.List<int> _test1 = new System.Collections.Generic.List<int>();    
    System.Collections.Generic.List<int> this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60),
                // (9,66): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "2").WithLocation(9, 66)
                );

            string source3 = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = {2} };
    }

    C2 _test1 = new C2();    
    C2 this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
}
";

            var comp3 = CreateCompilation(source3, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);
            CompileAndVerify(comp3).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_MemberInitializer_CollectionInitializer_02()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = {2} };
        System.Console.WriteLine(c[1][0]);        
    }

    System.Collections.Generic.List<int> _test1 = new System.Collections.Generic.List<int>();    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IMemberInitializerOperation)model.GetOperation(assignment);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
        Print(expr);
    }

    System.Collections.Generic.List<int> _test1 = new System.Collections.Generic.List<int>();    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60),
                // (9,66): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "2").WithLocation(9, 66)
                );

            string source3 = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = {2} };
    }

    C2 _test1 = new C2();    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
}
";

            var comp3 = CreateCompilation(source3, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);
            CompileAndVerify(comp3).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(NoIOperationValidation))] // IOperation validation is suppressed due to https://github.com/dotnet/roslyn/issues/72931
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72931")]
        public void SingleCandidate_RefReturning_Property_MemberInitializer_CollectionInitializer()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = {2} };
        System.Console.WriteLine(c[1][0]);        
    }

    System.Collections.Generic.List<int> _test1 = new System.Collections.Generic.List<int>();    
    ref System.Collections.Generic.List<int> this[int x]
    {
        get => ref _test1;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ImplicitElementAccessSyntax>().Single();
            var symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Collections.Generic.List<System.Int32> C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Collections.Generic.List<System.Int32>", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Collections.Generic.List<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var assignment = (AssignmentExpressionSyntax)elementAccess.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("System.Collections.Generic.List<System.Int32>", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Collections.Generic.List<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IMemberInitializerOperation)model.GetOperation(assignment);
            AssertEx.Equal("System.Collections.Generic.List<System.Int32>", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

            string source2 = @"
using System;
using System.Linq.Expressions;

public class C
{
    static void Main()
    {
        var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
        Print(expr);
    }

    System.Collections.Generic.List<int> _test1 = new System.Collections.Generic.List<int>();    
    ref System.Collections.Generic.List<int> this[int x]
    {
        get => ref _test1;
    }

    static Expression<Func<dynamic, C>> GetExpression(Expression<Func<dynamic, C>> ex) => ex;
    static void Print(Expression<Func<dynamic, C>> expr)
    {
        System.Console.Write(expr);
    }
}
";

            var comp2 = CreateCompilation(source2, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (9,59): error CS8074: An expression tree lambda may not contain a dictionary initializer.
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_DictionaryInitializerInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,59): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "[d]").WithLocation(9, 59),
                // (9,60): error CS1963: An expression tree may not contain a dynamic operation
                //         var expr = GetExpression((dynamic d) => new C() { [d] = {2} });
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsDynamicOperation, "d").WithLocation(9, 60)
                );

            string source3 = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C() { [d] = {2} };
    }

    C2 _test1 = new C2();    
    ref C2 this[int x]
    {
        get => ref _test1;
    }
}

class C2
{
}
";

            var comp3 = CreateCompilation(source3, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);
            comp3.VerifyDiagnostics(
                // (7,33): error CS1922: Cannot initialize type 'C2' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         var c = new C() { [d] = {2} };
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{2}").WithArguments("C2").WithLocation(7, 33)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33011")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Tuple_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = (2, 123);
        System.Console.Write(a);
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(dynamic, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic (dynamic, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal(tupleTypeInfo.Type, operation.Value.Type);
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = (TupleExpressionSyntax)assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var rightElement = right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(rightElement);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = ((int?)null, 123);
        Print(a.Item1);
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullability is not tracked across deconstruction, this is a pre-existing condition - https://github.com/dotnet/roslyn/issues/33011 
            comp3.VerifyDiagnostics();

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33011")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Tuple_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = (2, 123);
        System.Console.Write(a);
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(dynamic, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic (dynamic, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(dynamic, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal(tupleTypeInfo.Type, operation.Value.Type);
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = (TupleExpressionSyntax)assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var rightElement = right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(rightElement);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = ((int?)null, 123);
        Print(a.Item1);
    }

    int? _test1 = 0;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullability is not tracked across deconstruction, this is a pre-existing condition - https://github.com/dotnet/roslyn/issues/33011 
            comp3.VerifyDiagnostics();

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33011")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Tuple_03()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = ((dynamic)2, 123);
        System.Console.Write(a);
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(dynamic, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic (dynamic, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal(tupleTypeInfo.Type, operation.Value.Type);
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = (TupleExpressionSyntax)assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var rightElement = right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(rightElement);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = ((dynamic?)null, 123);
        Print(a.Item1);
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullability is not tracked across deconstruction, this is a pre-existing condition - https://github.com/dotnet/roslyn/issues/33011 
            comp3.VerifyDiagnostics();

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Tuple_RightSideIsConvertedStatically()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = (""2"", 123);
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            comp.VerifyDiagnostics(
                // (8,30): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         var a = (c[d], _) = ("2", 123);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""").WithArguments("string", "int").WithLocation(8, 30)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_Assignment_Deconstruction_Tuple()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = (2, 123);
        System.Console.Write(a);
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    ref int this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(System.Int32, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 (System.Int32, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal(tupleTypeInfo.Type, operation.Value.Type);
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = (TupleExpressionSyntax)assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var rightElement = right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(rightElement);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = ((int?)null, 123);
        Print(a.Item1);
    }

    int? _test1 = 0;    
    ref int? this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a.Item1);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a.Item1").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33011")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Method_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        System.Console.Write(a);        
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out int x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(dynamic, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic (dynamic, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: not null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal("C2", operation.Value.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        Print(a.Item1);
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out int? x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullability is not tracked across deconstruction, this is a pre-existing condition - https://github.com/dotnet/roslyn/issues/33011 
            comp3.VerifyDiagnostics();

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33011")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72913")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Method_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        System.Console.Write(a);        
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    dynamic this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out int x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(dynamic, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic (dynamic, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("dynamic C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(dynamic, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: not null, Conversion: null, Nested: [{ Method: null, Conversion: { IsBoxing: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal("C2", operation.Value.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            // The meaningless warning is a pre-existing condition - https://github.com/dotnet/roslyn/issues/72913
            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics(
                // (10,18): warning CS8624: Argument of type 'dynamic' cannot be used as an output of type 'int' for parameter 'x' in 'void C2.Deconstruct(out int x, out int y)' due to differences in the nullability of reference types.
                //         var a = (c[d], _) = new C2();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgumentForOutput, "c[d]").WithArguments("dynamic", "int", "x", "void C2.Deconstruct(out int x, out int y)").WithLocation(10, 18)
                );

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        Print(a.Item1);
    }

    int? _test1 = 0;    
    dynamic? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out int? x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // Nullability is not tracked across deconstruction, this is a pre-existing condition - https://github.com/dotnet/roslyn/issues/33011 
            // The meaningless warning is a pre-existing condition - https://github.com/dotnet/roslyn/issues/72913
            comp3.VerifyDiagnostics(
                // (10,18): warning CS8624: Argument of type 'dynamic' cannot be used as an output of type 'int?' for parameter 'x' in 'void C2.Deconstruct(out int? x, out int y)' due to differences in the nullability of reference types.
                //         var a = (c[d], _) = new C2();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgumentForOutput, "c[d]").WithArguments("dynamic", "int?", "x", "void C2.Deconstruct(out int? x, out int y)").WithLocation(10, 18)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72914")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Method_03()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        System.Console.Write(a);        
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out dynamic x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            // The unexpected error is a pre-existing condition - https://github.com/dotnet/roslyn/issues/72914
            comp.VerifyDiagnostics(
                // (10,18): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = (c[d], _) = new C2();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c[d]").WithArguments("dynamic", "int").WithLocation(10, 18)
                );

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        Print(a.Item1);
    }

    int? _test1 = 0;    
    int? this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out dynamic? x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            // The unexpected error is a pre-existing condition - https://github.com/dotnet/roslyn/issues/72914
            comp3.VerifyDiagnostics(
                // (10,18): error CS0266: Cannot implicitly convert type 'dynamic' to 'int?'. An explicit conversion exists (are you missing a cast?)
                //         var a = (c[d], _) = new C2();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c[d]").WithArguments("dynamic", "int?").WithLocation(10, 18)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Method_RightSideIsConvertedStatically()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
    public void Deconstruct(out string x, out int y)
    {
        (x, y) = (""2"", 123);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            comp.VerifyDiagnostics(
                // (8,18): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         var a = (c[d], _) = new C2();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c[d]").WithArguments("string", "int").WithLocation(8, 18)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_RefReturning_Property_Assignment_Deconstruction_Method()
        {
            string source = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        System.Console.Write(a);        
        Print(a.Item1);
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    ref int this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out int x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").First();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(System.Int32, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("System.Int32 (System.Int32, System.Int32).Item1", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("ref System.Int32 C.this[System.Int32 x] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var propertyRef = (IPropertyReferenceOperation)model.GetOperation(elementAccess);
            AssertEx.Equal(symbolInfo.Symbol.ToTestDisplayString(), propertyRef.Property.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, propertyRef.Type);

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            var tupleTypeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", tupleTypeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: not null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] });

            var operation = (IDeconstructionAssignmentOperation)model.GetOperation(assignment);
            Assert.Equal(tupleTypeInfo.Type, operation.Target.Type);
            Assert.Equal("C2", operation.Value.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, operation.Type);

            var right = assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2").VerifyDiagnostics();

            string source3 = @"
#nullable enable

public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = new C2();
        Print(a.Item1);
    }

    int? _test1 = 0;    
    ref int? this[int x]
    {
        get => ref _test1;
    }

    static void Print(dynamic b) 
    {
    }
}

class C2
{
    public void Deconstruct(out int? x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";
            var comp3 = CreateCompilation(source3, targetFramework: TargetFramework.StandardAndCSharp);

            comp3.VerifyDiagnostics(
                // (11,15): warning CS8604: Possible null reference argument for parameter 'b' in 'void C.Print(dynamic b)'.
                //         Print(a.Item1);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "a.Item1").WithArguments("b", "void C.Print(dynamic b)").WithLocation(11, 15)
                );

            tree = comp3.SyntaxTrees.Single();
            node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Item1").Single();
            model = comp3.GetSemanticModel(tree);

            typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("System.Int32?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Nested_01()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ((c[d], _), _) = ((2, 123), 124);
        System.Console.Write(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("((dynamic, System.Int32), System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            typeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            left = (TupleExpressionSyntax)left.Parent.Parent;
            typeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("((System.Int32, System.Int32), System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("((System.Int32, System.Int32), System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("((dynamic, System.Int32), System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("((dynamic, System.Int32), System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: null, Conversion: null, Nested: [{ Method: null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] }, _] });

            var right = (TupleExpressionSyntax)assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("((System.Int32, System.Int32), System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("((System.Int32, System.Int32), System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            right = (TupleExpressionSyntax)right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var rightElement = right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(rightElement);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "((2, 123), 124) 2").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Nested_02()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        dynamic d = 1;
        var c = new C();
        var a = ((c[d], _), _) = (new C2(), 124);
        System.Console.Write(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}

class C2
{
    public void Deconstruct(out int x, out int y)
    {
        (x, y) = (2, 123);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("((dynamic, System.Int32), System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            typeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            left = (TupleExpressionSyntax)left.Parent.Parent;
            typeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("((System.Int32, System.Int32), System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("((System.Int32, System.Int32), System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("((dynamic, System.Int32), System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("((dynamic, System.Int32), System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetDeconstructionInfo(assignment) is { Method: null, Conversion: null, Nested: [{ Method: not null, Conversion: null, Nested: [{ Method: null, Conversion: { IsIdentity: true }, Nested: [] }, _] }, _] });

            var right = (TupleExpressionSyntax)assignment.Right;
            typeInfo = model.GetTypeInfo(right);
            AssertEx.Equal("(C2, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(C2, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var rightElement = right.Arguments[0].Expression;
            typeInfo = model.GetTypeInfo(rightElement);
            AssertEx.Equal("C2", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("C2", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "((2, 123), 124) 2").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_Assignment_Deconstruction_Conditional()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        Test(true);
        System.Console.Write("" "");        
        Test(false);
    }

    static void Test(bool b)
    {
        dynamic d = 1;
        var c = new C();
        var a = (c[d], _) = b ? (2, 123) : (3, 124);
        System.Console.Write(a);        
        System.Console.Write("" "");        
        System.Console.Write(c._test1);        
    }

    int _test1 = 0;    
    int this[int x]
    {
        get => _test1;
        set => _test1 = value;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("(dynamic, System.Int32) a", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.Int32 x] { get; set; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            var left = (TupleExpressionSyntax)elementAccess.Parent.Parent;
            typeInfo = model.GetTypeInfo(left);
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            var assignment = (AssignmentExpressionSyntax)left.Parent;
            typeInfo = model.GetTypeInfo(assignment);
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("(dynamic, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(2, 123) 2 (3, 124) 3").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_CSharp12_01()
        {
            string source1 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    object Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1.Test(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Object I1.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_CSharp12_02()
        {
            string source1 = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1.Test(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    int Test(string name, object value);
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1.Test(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Int32 I1.Test(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_Extension_CSharp12()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = new C().Test(""name"", d);
        System.Console.Write(result);        
    }
}

static class Extensions
{
    public static int Test(this C c, string name, object value) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp1.SyntaxTrees.Single();
            var model = comp1.GetSemanticModel(tree);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            Assert.Equal(OperationKind.Invalid, model.GetOperation(call).Kind);

            comp1.VerifyDiagnostics(
                // (7,22): error CS1973: 'C' has no applicable method named 'Test' but appears to have an extension method by that name. Extension methods cannot be dynamically dispatched. Consider casting the dynamic arguments or calling the extension method without the extension method syntax.
                //         var result = new C().Test("name", d);
                Diagnostic(ErrorCode.ERR_BadArgTypeDynamicExtension, @"new C().Test(""name"", d)").WithArguments("C", "Test").WithLocation(7, 22)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_ParamArray_CSharp12()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(""name"", d);
        System.Console.Write(result);        
    }

    static int Test(string name, object value, params int[] list) => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.Test(System.String name, System.Object value, params System.Int32[] list)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Delegate_CSharp12()
        {
            string source = @"
public class C
{
    static C M(Test i1, dynamic value)
    {
        var result = i1(""name"", value);
        return JsonSerializer.Deserialize<C>(result);
    }
}

delegate object Test(string name, object value);

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T);
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"i1(""name"", value)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal("System.Object Test.Invoke(System.String name, System.Object value)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_ParamArray_Delegate_CSharp12()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = Test(""name"", d);
        System.Console.Write(result);        
    }

    static D Test = (string name, object value, params int[] list) => 123;
    delegate int D(string name, object value, params int[] list);
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            AssertEx.Equal(@"Test(""name"", d)", call.ToString());
            symbolInfo = model.GetSymbolInfo(call);
            AssertEx.Equal(@"System.Int32 C.D.Invoke(System.String name, System.Object value, params System.Int32[] list)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(call);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicInvocationOperation)model.GetOperation(call);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_CSharp12_01()
        {
            string source = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    object this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Object I1.this[System.String name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicIndexerAccessOperation)model.GetOperation(elementAccess);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_Property_CSharp12_02()
        {
            string source = @"
#nullable enable

public class C
{
    public static C M(I1 i1, dynamic value)
    {
        var result = i1[""name"", value];
        return JsonSerializer.Deserialize<C>(result);
    }
}

public interface I1
{
    int this[string name, object value] {get;}
}

class JsonSerializer
{
    public static T Deserialize<T>(System.IO.Stream c) => default(T)!;
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic? result", symbolInfo.Symbol.ToTestDisplayString());

            var typeInfo = model.GetTypeInfo(node);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 I1.this[System.String name, System.Object value] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicIndexerAccessOperation)model.GetOperation(elementAccess);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72750")]
        public void SingleCandidate_ResultIsDynamic_ParamArray_Property_CSharp12()
        {
            string source1 = @"
public class C
{
    static void Main()
    {
        dynamic d = 1;
        var result = new C()[""name"", d];
        System.Console.Write(result);        
    }

    int this[string name, object value, params int[] list] => 123;
}
";

            var comp1 = CreateCompilation(source1, options: TestOptions.DebugExe, targetFramework: TargetFramework.StandardAndCSharp, parseOptions: TestOptions.Regular12);

            // This is surprising, but this scenario used to successfully bind dynamically before (unlike a call).
            var tree = comp1.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "result").Single();
            var model = comp1.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("dynamic result", symbolInfo.Symbol.ToTestDisplayString());

            var elementAccess = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            symbolInfo = model.GetSymbolInfo(elementAccess);
            AssertEx.Equal("System.Int32 C.this[System.String name, System.Object value, params System.Int32[] list] { get; }", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(elementAccess);
            AssertEx.Equal("dynamic", typeInfo.Type.ToTestDisplayString());
            AssertEx.Equal("dynamic", typeInfo.ConvertedType.ToTestDisplayString());

            var operation = (IDynamicIndexerAccessOperation)model.GetOperation(elementAccess);
            AssertEx.Equal("dynamic", operation.Type.ToTestDisplayString());

            CompileAndVerify(comp1, expectedOutput: "123").VerifyDiagnostics();
        }
    }
}
