// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    int goo { get; set; }
}
interface I1 : I3 { }
interface I2 : I3
{
    new void goo();
}
interface I0 : I1, I2
{
    new void goo(int x);
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (13,14): warning CS0109: The member 'I0.goo(int)' does not hide an accessible member. The new keyword is not required.
                //     new void goo(int x);
                Diagnostic(ErrorCode.WRN_NewNotRequired, "goo").WithArguments("I0.goo(int)"));
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

            CreateCompilation(source).VerifyDiagnostics(
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
            var comp = CreateCompilation(source);
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
        public void FalseIdentificationOfCircularDependency()
        {
            var source = @"
public class ITest : ITest.Test{
   public interface Test { }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithInParameter()
        {
            var code = @"
interface A
{
    void M(in int x);
}
interface B : A
{
    void M(in int x);
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,10): warning CS0108: 'B.M(in int)' hides inherited member 'A.M(in int)'. Use the new keyword if hiding was intended.
                //     void M(in int x);
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M(in int)", "A.M(in int)").WithLocation(8, 10));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnType_RefReadOnly_RefReadOnly()
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

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,22): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     ref readonly int M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(8, 22));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnType_Ref_RefReadOnly()
        {
            var code = @"
interface A
{
    ref int M();
}
interface B : A
{
    ref readonly int M();
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,22): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     ref readonly int M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(8, 22));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnType_RefReadOnly_Ref()
        {
            var code = @"
interface A
{
    ref readonly int M();
}
interface B : A
{
    ref int M();
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,13): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     ref readonly int M();
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(8, 13));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnType_RefReadonly_RefReadonly()
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

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,22): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     ref readonly int Property { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(8, 22));

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnType_RefReadonly_Ref()
        {
            var code = @"
interface A
{
    ref readonly int Property { get; }
}
interface B : A
{
    ref int Property { get; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,13): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     ref int Property { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(8, 13));

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnType_Ref_RefReadonly()
        {
            var code = @"
interface A
{
    ref int Property { get; }
}
interface B : A
{
    ref readonly int Property { get; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,22): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     ref readonly int Property { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(8, 22));

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithInParameterAndNewKeyword()
        {
            var code = @"
interface A
{
    void M(in int x);
}
interface B : A
{
    new void M(in int x);
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnTypeAndNewKeyword()
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

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnTypeAndNewKeyword()
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

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void ImplementingMethodWithInParameter()
        {
            var code = @"
interface A
{
    void M(in int x);
}
class B : A
{
    public void M(in int x) { }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void ImplementingMethodWithRefReadOnlyReturnType()
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

            var comp = CreateCompilation(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void ImplementingPropertyWithRefReadOnlyReturnType()
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

            var comp = CreateCompilation(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void ImplementingMethodWithDifferentParameterRefness()
        {
            var code = @"
interface A
{
    void M(in int x);
}
class B : A
{
    public void M(ref int x) { }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (6,11): error CS0535: 'B' does not implement interface member 'A.M(in int)'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "A").WithArguments("B", "A.M(in int)").WithLocation(6, 11));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void ImplementingRefReadOnlyMembersWillOverwriteTheCorrectSlot()
        {
            var text = @"
interface BaseInterface
{
    ref readonly int Method1(in int a);
    ref readonly int Property1 { get; }
    ref readonly int this[int a] { get; }
}

class DerivedClass : BaseInterface
{
    protected int field;
    public ref readonly int Method1(in int a) { return ref field; }
    public ref readonly int Property1 { get { return ref @field; } }
    public ref readonly int this[int a] { get { return ref field; } }
}";

            var comp = CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void MethodImplementationsShouldPreserveRefKindInParameters()
        {
            var text = @"
interface BaseInterface
{
    void Method1(ref int x);
    void Method2(in int x);
}
class ChildClass : BaseInterface
{
    public void Method1(in int x) { }
    public void Method2(ref int x) { }
}";

            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (7,20): error CS0535: 'ChildClass' does not implement interface member 'BaseInterface.Method2(in int)'
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method2(in int)").WithLocation(7, 20),
                // (7,20): error CS0535: 'ChildClass' does not implement interface member 'BaseInterface.Method1(ref int)'
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method1(ref int)").WithLocation(7, 20));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
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

            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (7,20): error CS8152: 'ChildClass' does not implement interface member 'BaseInterface.Method2()'. 'ChildClass.Method2()' cannot implement 'BaseInterface.Method2()' because it does not have matching return by reference.
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method2()", "ChildClass.Method2()").WithLocation(7, 20),
                // (7,20): error CS8152: 'ChildClass' does not implement interface member 'BaseInterface.Method1()'. 'ChildClass.Method1()' cannot implement 'BaseInterface.Method1()' because it does not have matching return by reference.
                // class ChildClass : BaseInterface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "BaseInterface").WithArguments("ChildClass", "BaseInterface.Method1()", "ChildClass.Method1()").WithLocation(7, 20));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
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

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (7,11): error CS8152: 'B' does not implement interface member 'A.Property2'. 'B.Property2' cannot implement 'A.Property2' because it does not have matching return by reference.
                // class B : A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "A").WithArguments("B", "A.Property2", "B.Property2").WithLocation(7, 11),
                // (7,11): error CS8152: 'B' does not implement interface member 'A.Property1'. 'B.Property1' cannot implement 'A.Property1' because it does not have matching return by reference.
                // class B : A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "A").WithArguments("B", "A.Property1", "B.Property1").WithLocation(7, 11));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerImplementationsShouldPreserveReadOnlyRefnessInReturnTypes_Ref_RefReadOnly()
        {
            var code = @"
interface A
{
    ref int this[int p] { get; }
}
class B : A
{
    protected int x = 0;
    public ref readonly int this[int p] { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (6,11): error CS8152: 'B' does not implement interface member 'A.this[int]'. 'B.this[int]' cannot implement 'A.this[int]' because it does not have matching return by reference.
                // class B : A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "A").WithArguments("B", "A.this[int]", "B.this[int]").WithLocation(6, 11));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerImplementationsShouldPreserveReadOnlyRefnessInReturnTypes_RefReadOnly_Ref()
        {
            var code = @"
interface A
{
    ref readonly int this[int p] { get; }
}
class B : A
{
    protected int x = 0;
    public ref int this[int p] { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (6,11): error CS8152: 'B' does not implement interface member 'A.this[int]'. 'B.this[int]' cannot implement 'A.this[int]' because it does not have matching return by reference.
                // class B : A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "A").WithArguments("B", "A.this[int]", "B.this[int]").WithLocation(6, 11));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerImplementationsShouldPreserveReadOnlyRefnessInIndexes_Valid()
        {
            var code = @"
interface A
{
    int this[in int p] { get; }
}
class B : A
{
    public int this[in int p] { get { return p; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerImplementationsShouldPreserveReadOnlyRefnessInIndexes_Source()
        {
            var code = @"
interface A
{
    int this[in int p] { get; }
}
class B : A
{
    public int this[int p] { get { return p; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (6,11): error CS0535: 'B' does not implement interface member 'A.this[in int]'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "A").WithArguments("B", "A.this[in int]").WithLocation(6, 11));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerImplementationsShouldPreserveReadOnlyRefnessInIndexes_Destination()
        {
            var code = @"
interface A
{
    int this[int p] { get; }
}
class B : A
{
    public int this[in int p] { get { return p; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (6,11): error CS0535: 'B' does not implement interface member 'A.this[int]'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "A").WithArguments("B", "A.this[int]").WithLocation(6, 11));
        }
    }
}
