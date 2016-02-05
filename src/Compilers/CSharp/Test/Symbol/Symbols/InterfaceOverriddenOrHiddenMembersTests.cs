// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    /// <summary>
    /// Test interface member hiding.
    /// </summary>
    public class InterfaceOverriddenOrHiddenMembersTests : CSharpTestBase
    {
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void Repro581173()
        {
            var source = @"
interface I3
{
    int foo { get; set; }
}
interface I1 : I3 { }
interface I2 : I3
{
    new void foo();
}
interface I0 : I1, I2
{
    new void foo(int x);
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,14): warning CS0109: The member 'I0.foo(int)' does not hide an inherited member. The new keyword is not required.
                //     new void foo(int x);
                Diagnostic(ErrorCode.WRN_NewNotRequired, "foo").WithArguments("I0.foo(int)"));
        }

        /// <summary>
        /// For this series of tests, we're going to use a fixed type hierarchy and a single member signature "void M()".
        /// We will start with the signature in all interfaces, and then remove it from various subsets.
        /// 
        ///      ITop
        ///    /      \
        /// ILeft    IRight
        ///    \      /
        ///    IBottom
        /// 
        /// All have method.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Method_1()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M()"),
                // (14,10): warning CS0108: 'IRight.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M()", "ITop.M()"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have method but IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Method_2()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
//    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M()"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have method but ILeft and IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Method_3()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
//    void M();
}

public interface IRight : ITop
{
//    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ITop.M()"));
        }

        /// <summary>
        /// All have method but ITop.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Method_4()
        {
            var source = @"
public interface ITop 
{
//    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            // Also hides IRight.M, but not reported.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have method but ITop and IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Method_5()
        {
            var source = @"
public interface ITop 
{
//    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
//    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// These tests are the same as the TestDiamond_Method tests except that, instead of removing the method
        /// from some interfaces, we'll change its parameter list in those interfaces.
        /// 
        ///      ITop
        ///    /      \
        /// ILeft    IRight
        ///    \      /
        ///    IBottom
        /// 
        /// All have unmodified method.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Overload_1()
        {
            // Identical to TestDiamond_Method_1, so omitted.
        }

        /// <summary>
        /// All have unmodified method but IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Overload_2()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M(int x);
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M()"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have unmodified method but ILeft and IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Overload_3()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M(int x);
}

public interface IRight : ITop
{
    void M(int x);
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ITop.M()"));
        }

        /// <summary>
        /// All have unmodified method but ITop.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Overload_4()
        {
            var source = @"
public interface ITop 
{
    void M(int x);
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            // Also hides IRight.M, but not reported.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have unmodified method but ITop and IRight.
        /// Unlike the other TestDiamond_Overload tests, this one reports different diagnostics than its TestDiamond_Method counterpart.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Overload_5()
        {
            var source = @"
public interface ITop 
{
    void M(int x);
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M(int x);
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"),
                // (14,10): warning CS0108: 'IRight.M(int)' hides inherited member 'ITop.M(int)'. Use the new keyword if hiding was intended.
                //     void M(int x);
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M(int)", "ITop.M(int)"));
        }

        /// <summary>
        /// These tests are the same as the TestDiamond_Method tests except that, instead of removing the method
        /// from some interfaces, we'll change its type parameter list in those interfaces.
        /// 
        ///      ITop
        ///    /      \
        /// ILeft    IRight
        ///    \      /
        ///    IBottom
        /// 
        /// All have unmodified method.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Arity_1()
        {
            // Identical to TestDiamond_Method_1, so omitted.
        }

        /// <summary>
        /// All have unmodified method but IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Arity_2()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M<T>();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M()"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have unmodified method but ILeft and IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Arity_3()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M<T>();
}

public interface IRight : ITop
{
    void M<T>();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ITop.M()"));
        }

        /// <summary>
        /// All have unmodified method but ITop.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Arity_4()
        {
            var source = @"
public interface ITop 
{
    void M<T>();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            // Also hides IRight.M, but not reported.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have unmodified method but ITop and IRight.
        /// Unlike the other TestDiamond_Overload tests, this one reports different diagnostics than its TestDiamond_Method counterpart.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Arity_5()
        {
            var source = @"
public interface ITop 
{
    void M<T>();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M<T>();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"),
                // (14,10): warning CS0108: 'IRight.M<T>()' hides inherited member 'ITop.M<T>()'. Use the new keyword if hiding was intended.
                //     void M<T>();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M<T>()", "ITop.M<T>()"));
        }

        /// <summary>
        /// These tests are the same as the TestDiamond_Method tests except that, instead of removing the method
        /// from some interfaces, we'll change its member kind (to Property) in those interfaces.
        /// 
        ///      ITop
        ///    /      \
        /// ILeft    IRight
        ///    \      /
        ///    IBottom
        /// 
        /// All have unmodified method.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Kind_1()
        {
            // Identical to TestDiamond_Method_1, so omitted.
        }

        /// <summary>
        /// All have unmodified method but IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Kind_2()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    int M { get; set; }
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M()"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"),
                // (14,9): warning CS0108: 'IRight.M' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     int M { get; set; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M", "ITop.M()"));
        }

        /// <summary>
        /// All have unmodified method but ILeft and IRight.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Kind_3()
        {
            var source = @"
public interface ITop 
{
    void M();
}

public interface ILeft : ITop
{
    int M { get; set; }
}

public interface IRight : ITop
{
    int M { get; set; }
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M"),
                // (9,9): warning CS0108: 'ILeft.M' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     int M { get; set; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M", "ITop.M()"),
                // (14,9): warning CS0108: 'IRight.M' hides inherited member 'ITop.M()'. Use the new keyword if hiding was intended.
                //     int M { get; set; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M", "ITop.M()"));
        }

        /// <summary>
        /// All have unmodified method but ITop.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Kind_4()
        {
            var source = @"
public interface ITop 
{
    int M { get; set; }
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    void M();
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            // Also hides IRight.M, but not reported.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,10): warning CS0108: 'IRight.M()' hides inherited member 'ITop.M'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M()", "ITop.M"),
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"));
        }

        /// <summary>
        /// All have unmodified method but ITop and IRight.
        /// Unlike the other TestDiamond_Overload tests, this one reports different diagnostics than its TestDiamond_Method counterpart.
        /// </summary>
        [WorkItem(581173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581173")]
        [Fact]
        public void TestDiamond_Kind_5()
        {
            var source = @"
public interface ITop 
{
    int M { get; set; }
}

public interface ILeft : ITop
{
    void M();
}

public interface IRight : ITop
{
    int M { get; set; }
}

public interface IBottom : ILeft, IRight
{
    void M();
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,9): warning CS0108: 'IRight.M' hides inherited member 'ITop.M'. Use the new keyword if hiding was intended.
                //     int M { get; set; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IRight.M", "ITop.M"),
                // (19,10): warning CS0108: 'IBottom.M()' hides inherited member 'ILeft.M()'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("IBottom.M()", "ILeft.M()"),
                // (9,10): warning CS0108: 'ILeft.M()' hides inherited member 'ITop.M'. Use the new keyword if hiding was intended.
                //     void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("ILeft.M()", "ITop.M"));
        }

        [Fact]
        [WorkItem(661370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/661370")]
        public void HideAndOverride()
        {
            var source = @"
public interface Base
{
    void M();
    int M { get; set; } // NOTE: illegal, since there's already a method M.
}

public interface Derived1 : Base
{
     void M();
}

public interface Derived2 : Base
{
     int M { get; set; }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (5,9): error CS0102: The type 'Base' already contains a definition for 'M'
                //     int M { get; set; } // NOTE: illegal, since there's already a method M.
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "M").WithArguments("Base", "M"),

                // (15,10): warning CS0108: 'Derived2.M' hides inherited member 'Base.M'. Use the new keyword if hiding was intended.
                //      int M { get; set; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("Derived2.M", "Base.M"),
                // (10,11): warning CS0108: 'Derived1.M()' hides inherited member 'Base.M()'. Use the new keyword if hiding was intended.
                //      void M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("Derived1.M()", "Base.M()"));

            var global = comp.GlobalNamespace;

            var baseInterface = global.GetMember<NamedTypeSymbol>("Base");
            var baseMethod = baseInterface.GetMembers("M").OfType<MethodSymbol>().Single();
            var baseProperty = baseInterface.GetMembers("M").OfType<PropertySymbol>().Single();

            var derivedInterface1 = global.GetMember<NamedTypeSymbol>("Derived1");
            var derivedMethod = derivedInterface1.GetMember<MethodSymbol>("M");

            var overriddenOrHidden1 = derivedMethod.OverriddenOrHiddenMembers;
            AssertEx.SetEqual(overriddenOrHidden1.HiddenMembers, baseMethod, baseProperty);

            var derivedInterface2 = global.GetMember<NamedTypeSymbol>("Derived2");
            var derivedProperty = derivedInterface2.GetMember<PropertySymbol>("M");

            var overriddenOrHidden2 = derivedProperty.OverriddenOrHiddenMembers;
            AssertEx.SetEqual(overriddenOrHidden2.HiddenMembers, baseMethod, baseProperty);
        }

        [Fact]
        [WorkItem(667278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667278")]
        public void FalseIdentificationOfCircularDependancy()
        {
            var source = @"
public class ITest : ITest.Test{
   public interface Test { }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }
    }
}
