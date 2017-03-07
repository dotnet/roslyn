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
                // (13,14): warning CS0109: The member 'I0.foo(int)' does not hide an accessible member. The new keyword is not required.
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

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void HidingMethodWithRefReadOnlyParameterWillProduceAWarning()
        {
            var code = @"
interface A
{
    void M(ref readonly int x);
}
interface B : A
{
    void M(ref readonly int x);
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics(
                // (8,10): warning CS0108: 'B.M(ref readonly int)' hides inherited member 'A.M(ref readonly int)'. Use the new keyword if hiding was intended.
                //     void M(ref readonly int x);
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M(ref readonly int)", "A.M(ref readonly int)").WithLocation(8, 10));

            var aClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var bClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            var aMethod = aClass.GetMember<MethodSymbol>("M");
            var bMethod = bClass.GetMember<MethodSymbol>("M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnTypeWillProduceAWarning()
        {
            var code = @"
interface A
{
    ref readonly int M();
}
interface B : A
{
    ref readonly int M();
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics(
                // (8,22): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     ref readonly int M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(8, 22));

            var aClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var bClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            var aMethod = aClass.GetMember<MethodSymbol>("M");
            var bMethod = bClass.GetMember<MethodSymbol>("M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnTypeWillProduceAWarning()
        {
            var code = @"
interface A
{
    ref readonly int Property { get; }
}
interface B : A
{
    ref readonly int Property { get; }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics(
                // (8,22): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     ref readonly int Property { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(8, 22));

            var aClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var bClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            var aProperty = aClass.GetMember<PropertySymbol>("Property");
            var bProperty = bClass.GetMember<PropertySymbol>("Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void HidingMethodWithRefReadOnlyParameterAndNewKeywordWillNotProduceAWarning()
        {
            var code = @"
interface A
{
    void M(ref readonly int x);
}
interface B : A
{
    new void M(ref readonly int x);
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics();

            var aClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var bClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            var aMethod = aClass.GetMember<MethodSymbol>("M");
            var bMethod = bClass.GetMember<MethodSymbol>("M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnTypeAndNewKeywordWillNotProduceAWarning()
        {
            var code = @"
interface A
{
    ref readonly int M();
}
interface B : A
{
    new ref readonly int M();
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics();

            var aClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var bClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            var aMethod = aClass.GetMember<MethodSymbol>("M");
            var bMethod = bClass.GetMember<MethodSymbol>("M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnTypeAndNewKeywordWillProduceAWarning()
        {
            var code = @"
interface A
{
    ref readonly int Property { get ; }
}
interface B : A
{
    new ref readonly int Property { get; }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics();

            var aClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var bClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("B");

            var aProperty = aClass.GetMember<PropertySymbol>("Property");
            var bProperty = bClass.GetMember<PropertySymbol>("Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void ImplementingMethodWithRefReadOnlyParameterWillNotProduceAnError()
        {
            var code = @"
interface A
{
    void M(ref readonly int x);
}
class B : A
{
    public void M(ref readonly int x) { }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void ImplementingMethodWithRefReadOnlyReturnTypeWillNotProduceAnError()
        {
            var code = @"
interface A
{
    ref readonly int M();
}
class B : A
{
    protected int x = 0;
    public ref readonly int M() { return ref x; }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void ImplementingPropertyWithRefReadOnlyReturnTypeWillNotProduceAnError()
        {
            var code = @"
interface A
{
    ref readonly int Property { get; }
}
class B : A
{
    protected int x = 0;
    public ref readonly int Property { get { return ref x; } }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void ImplementingMethodWithDifferentParameterRefnessWillProduceAnError()
        {
            var code = @"
interface A
{
    void M(ref readonly int x);
}
class B : A
{
    public void M(ref int x) { }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics(
                // (6,11): error CS0535: 'B' does not implement interface member 'A.M(ref readonly int)'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "A").WithArguments("B", "A.M(ref readonly int)").WithLocation(6, 11));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void ImplementingRefReadOnlyMembersWillOverwriteTheCorrectSlot()
        {
            var text = @"
interface BaseInterface
{
    ref readonly int Method1(ref readonly int a);
    ref readonly int Property1 { get; }
    ref readonly int this[int a] { get; }
}

class DerivedClass : BaseInterface
{
    protected int field;
    public ref readonly int Method1(ref readonly int a) { return ref field; }
    public ref readonly int Property1 { get { return ref field; } }
    public ref readonly int this[int a] { get { return ref field; } }
}";

            var comp = CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void MethodImplementationsShouldPreserveReadOnlyRefnessInParameters()
        {
            var text = @"
interface BaseInterface
{
    void Method1(ref int x);
    void Method2(ref readonly int x);
}
class ChildClass : BaseInterface
{
    public void Method1(ref readonly int x) { }
    public void Method2(ref int x) { }
}";

            var comp = CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,20): error CS0535: 'ChildClass' does not implement interface member 'BaseInterface.Method2(ref readonly int)'
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method2(ref readonly int)").WithLocation(7, 20),
                // (7,20): error CS0535: 'ChildClass' does not implement interface member 'BaseInterface.Method1(ref int)'
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method1(ref int)").WithLocation(7, 20));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void MethodImplementationsShouldPreserveReadOnlyRefnessInReturnTypes()
        {
            var text = @"
interface BaseInterface
{
    ref int Method1();
    ref readonly int Method2();
}
class ChildClass : BaseInterface
{
    protected int x = 0 ;
    public ref readonly int Method1() { return ref x; }
    public ref int Method2() { return ref x; }
}";

            var comp = CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,20): error CS8152: 'ChildClass' does not implement interface member 'BaseInterface.Method2()'. 'ChildClass.Method2()' cannot implement 'BaseInterface.Method2()' because it does not have the matching return type reference signature.
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method2()", "ChildClass.Method2()").WithLocation(7, 20),
                // (7,20): error CS8152: 'ChildClass' does not implement interface member 'BaseInterface.Method1()'. 'ChildClass.Method1()' cannot implement 'BaseInterface.Method1()' because it does not have the matching return type reference signature.
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method1()", "ChildClass.Method1()").WithLocation(7, 20));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadonlyReferences)]
        public void PropertyImplementationsShouldPreserveReadOnlyRefnessInReturnTypes()
        {
            var code = @"
interface A
{
    ref int Property1 { get; }
    ref readonly int Property2 { get; }
}
class B : A
{
    protected int x = 0;
    public ref readonly int Property1 { get { return ref x; } }
    public ref int Property2 { get { return ref x; } }
}";

            var comp = CreateCompilationWithMscorlib(code).VerifyDiagnostics(
                // (7,11): error CS8152: 'B' does not implement interface member 'A.Property2'. 'B.Property2' cannot implement 'A.Property2' because it does not have the matching return type reference signature.
                // class B : A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "A").WithArguments("B", "A.Property2", "B.Property2").WithLocation(7, 11),
                // (7,11): error CS8152: 'B' does not implement interface member 'A.Property1'. 'B.Property1' cannot implement 'A.Property1' because it does not have the matching return type reference signature.
                // class B : A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "A").WithArguments("B", "A.Property1", "B.Property1").WithLocation(7, 11));
        }
    }
}
