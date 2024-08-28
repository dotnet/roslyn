// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Areas: interface mapping, virtual/abstract/override methods,
    /// virtual properties, sealed members, new members, accessibility
    /// of inherited methods, etc.
    /// </summary>
    public class InheritanceBindingTests : CompilingTestBase
    {
        [Fact]
        public void TestModifiersOnExplicitImpl()
        {
            var text = @"
interface IGoo
{
    void Method1();
    void Method2();
    void Method3();
    void Method4();
    void Method5();
    void Method6();
    void Method7();
    void Method8();
    void Method9();
    void Method10();
    void Method11();
    void Method12();
    void Method13();
    void Method14();
}

abstract partial class AbstractGoo : IGoo
{
    abstract void IGoo.Method1() { }
    virtual void IGoo.Method2() { }
    override void IGoo.Method3() { }

    sealed void IGoo.Method4() { }

    new void IGoo.Method5() { }

    public void IGoo.Method6() { }
    protected void IGoo.Method7() { }
    internal void IGoo.Method8() { }
    protected internal void IGoo.Method9() { } //roslyn considers 'protected internal' one modifier (two in dev10)
    private void IGoo.Method10() { }

    extern void IGoo.Method11(); //not an error (in dev10 or roslyn)
    static void IGoo.Method12() { }
    partial void IGoo.Method13();

    private protected void IGoo.Method14() { }
}";

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (22,24): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract void IGoo.Method1() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method1").WithArguments("abstract").WithLocation(22, 24),
                // (23,23): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual void IGoo.Method2() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method2").WithArguments("virtual").WithLocation(23, 23),
                // (24,24): error CS0106: The modifier 'override' is not valid for this item
                //     override void IGoo.Method3() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method3").WithArguments("override").WithLocation(24, 24),
                // (26,22): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void IGoo.Method4() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method4").WithArguments("sealed").WithLocation(26, 22),
                // (28,19): error CS0106: The modifier 'new' is not valid for this item
                //     new void IGoo.Method5() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method5").WithArguments("new").WithLocation(28, 19),
                // (30,22): error CS0106: The modifier 'public' is not valid for this item
                //     public void IGoo.Method6() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method6").WithArguments("public").WithLocation(30, 22),
                // (31,25): error CS0106: The modifier 'protected' is not valid for this item
                //     protected void IGoo.Method7() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method7").WithArguments("protected").WithLocation(31, 25),
                // (32,24): error CS0106: The modifier 'internal' is not valid for this item
                //     internal void IGoo.Method8() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method8").WithArguments("internal").WithLocation(32, 24),
                // (33,34): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal void IGoo.Method9() { } //roslyn considers 'protected internal' one modifier (two in dev10)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method9").WithArguments("protected internal").WithLocation(33, 34),
                // (34,23): error CS0106: The modifier 'private' is not valid for this item
                //     private void IGoo.Method10() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method10").WithArguments("private").WithLocation(34, 23),
                // (37,22): error CS8703: The modifier 'static' is not valid for this item in C# 9.0. Please use language version '11.0' or greater.
                //     static void IGoo.Method12() { }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "Method12").WithArguments("static", "9.0", "11.0").WithLocation(37, 22),
                // (40,33): error CS0106: The modifier 'private protected' is not valid for this item
                //     private protected void IGoo.Method14() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Method14").WithArguments("private protected").WithLocation(40, 33),
                // (37,22): error CS0539: 'AbstractGoo.Method12()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static void IGoo.Method12() { }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method12").WithArguments("AbstractGoo.Method12()").WithLocation(37, 22),
                // (38,23): error CS0754: A partial member may not explicitly implement an interface member
                //     partial void IGoo.Method13();
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "Method13").WithLocation(38, 23),
                // (20,38): error CS0535: 'AbstractGoo' does not implement interface member 'IGoo.Method12()'
                // abstract partial class AbstractGoo : IGoo
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IGoo").WithArguments("AbstractGoo", "IGoo.Method12()").WithLocation(20, 38),
                // (36,22): warning CS0626: Method, operator, or accessor 'AbstractGoo.IGoo.Method11()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern void IGoo.Method11(); //not an error (in dev10 or roslyn)
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "Method11").WithArguments("AbstractGoo.IGoo.Method11()").WithLocation(36, 22)
                );
        }

        [Fact]
        public void TestModifiersOnExplicitPropertyImpl()
        {
            var text = @"
interface IGoo
{
    int Property1 { set; }
    int Property2 { set; }
    int Property3 { set; }
    int Property4 { set; }
    int Property5 { set; }
    int Property6 { set; }
    int Property7 { set; }
    int Property8 { set; }
    int Property9 { set; }
    int Property10 { set; }
    int Property11 { set; }
    int Property12 { set; }
}

abstract class AbstractGoo : IGoo
{
    abstract int IGoo.Property1 { set { } }
    virtual int IGoo.Property2 { set { } }
    override int IGoo.Property3 { set { } }

    sealed int IGoo.Property4 { set { } }

    new int IGoo.Property5 { set { } }

    public int IGoo.Property6 { set { } }
    protected int IGoo.Property7 { set { } }
    internal int IGoo.Property8 { set { } }
    protected internal int IGoo.Property9 { set { } } //roslyn considers 'protected internal' one modifier (two in dev10)
    private int IGoo.Property10 { set { } }

    extern int IGoo.Property11 { set; } //not an error (in dev10 or roslyn)
    static int IGoo.Property12 { set { } }
}";

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (20,23): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract int IGoo.Property1 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property1").WithArguments("abstract").WithLocation(20, 23),
                // (21,22): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual int IGoo.Property2 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property2").WithArguments("virtual").WithLocation(21, 22),
                // (22,23): error CS0106: The modifier 'override' is not valid for this item
                //     override int IGoo.Property3 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property3").WithArguments("override").WithLocation(22, 23),
                // (24,21): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed int IGoo.Property4 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property4").WithArguments("sealed").WithLocation(24, 21),
                // (26,18): error CS0106: The modifier 'new' is not valid for this item
                //     new int IGoo.Property5 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property5").WithArguments("new").WithLocation(26, 18),
                // (28,21): error CS0106: The modifier 'public' is not valid for this item
                //     public int IGoo.Property6 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property6").WithArguments("public").WithLocation(28, 21),
                // (29,24): error CS0106: The modifier 'protected' is not valid for this item
                //     protected int IGoo.Property7 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property7").WithArguments("protected").WithLocation(29, 24),
                // (30,23): error CS0106: The modifier 'internal' is not valid for this item
                //     internal int IGoo.Property8 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property8").WithArguments("internal").WithLocation(30, 23),
                // (31,33): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal int IGoo.Property9 { set { } } //roslyn considers 'protected internal' one modifier (two in dev10)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property9").WithArguments("protected internal").WithLocation(31, 33),
                // (32,22): error CS0106: The modifier 'private' is not valid for this item
                //     private int IGoo.Property10 { set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Property10").WithArguments("private").WithLocation(32, 22),
                // (35,21): error CS8703: The modifier 'static' is not valid for this item in C# 9.0. Please use language version '11.0' or greater.
                //     static int IGoo.Property12 { set { } }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "Property12").WithArguments("static", "9.0", "11.0").WithLocation(35, 21),
                // (35,21): error CS0539: 'AbstractGoo.Property12' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static int IGoo.Property12 { set { } }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property12").WithArguments("AbstractGoo.Property12").WithLocation(35, 21),
                // (18,30): error CS0535: 'AbstractGoo' does not implement interface member 'IGoo.Property12'
                // abstract class AbstractGoo : IGoo
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IGoo").WithArguments("AbstractGoo", "IGoo.Property12").WithLocation(18, 30),
                // (34,34): warning CS0626: Method, operator, or accessor 'AbstractGoo.IGoo.Property11.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern int IGoo.Property11 { set; } //not an error (in dev10 or roslyn)
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("AbstractGoo.IGoo.Property11.set").WithLocation(34, 34));
        }

        [Fact]
        public void TestModifiersOnExplicitIndexerImpl()
        {
            var text = @"
interface IGoo
{
    int this[int x1, int x2, int x3, int x4] { set; }
    int this[int x1, int x2, int x3, long x4] { set; }
    int this[int x1, int x2, long x3, int x4] { set; }
    int this[int x1, int x2, long x3, long x4] { set; }
    int this[int x1, long x2, int x3, int x4] { set; }
    int this[int x1, long x2, int x3, long x4] { set; }
    int this[int x1, long x2, long x3, int x4] { set; }
    int this[int x1, long x2, long x3, long x4] { set; }
    int this[long x1, int x2, int x3, int x4] { set; }
    int this[long x1, int x2, int x3, long x4] { set; }
    int this[long x1, int x2, long x3, int x4] { set; }
    int this[long x1, int x2, long x3, long x4] { set; }
}

abstract class AbstractGoo : IGoo
{
    abstract int IGoo.this[int x1, int x2, int x3, int x4] { set { } }
    virtual int IGoo.this[int x1, int x2, int x3, long x4] { set { } }
    override int IGoo.this[int x1, int x2, long x3, int x4] { set { } }

    sealed int IGoo.this[int x1, int x2, long x3, long x4] { set { } }

    new int IGoo.this[int x1, long x2, int x3, int x4] { set { } }

    public int IGoo.this[int x1, long x2, int x3, long x4] { set { } }
    protected int IGoo.this[int x1, long x2, long x3, int x4] { set { } }
    internal int IGoo.this[int x1, long x2, long x3, long x4] { set { } }
    protected internal int IGoo.this[long x1, int x2, int x3, int x4] { set { } } //roslyn considers 'protected internal' one modifier (two in dev10)
    private int IGoo.this[long x1, int x2, int x3, long x4] { set { } }

    extern int IGoo.this[long x1, int x2, long x3, int x4] { set; } //not an error (in dev10 or roslyn)
    static int IGoo.this[long x1, int x2, long x3, long x4] { set { } }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (20,23): error CS0106: The modifier 'abstract' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("abstract"),
                // (21,22): error CS0106: The modifier 'virtual' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("virtual"),
                // (22,23): error CS0106: The modifier 'override' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("override"),
                // (24,21): error CS0106: The modifier 'sealed' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("sealed"),
                // (26,18): error CS0106: The modifier 'new' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("new"),
                // (28,21): error CS0106: The modifier 'public' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("public"),
                // (29,24): error CS0106: The modifier 'protected' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected"),
                // (30,23): error CS0106: The modifier 'internal' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("internal"),
                // (31,33): error CS0106: The modifier 'protected internal' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected internal"),
                // (32,22): error CS0106: The modifier 'private' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("private"),
                // (35,21): error CS0106: The modifier 'static' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static"),
                // (34,62): warning CS0626: Method, operator, or accessor 'AbstractGoo.IGoo.this[long, int, long, int].set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("AbstractGoo.IGoo.this[long, int, long, int].set"));
        }

        [Fact, WorkItem(542158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542158")]
        public void TestModifiersOnExplicitEventImpl()
        {
            var text = @"
interface IGoo
{
    event System.Action Event1;
    event System.Action Event2;
    event System.Action Event3;
    event System.Action Event4;
    event System.Action Event5;
    event System.Action Event6;
    event System.Action Event7;
    event System.Action Event8;
    event System.Action Event9;
    event System.Action Event10;
    event System.Action Event11;
    event System.Action Event12;
}

abstract class AbstractGoo : IGoo
{
    abstract event System.Action IGoo.Event1 { add { } remove { } }
    virtual event System.Action IGoo.Event2 { add { } remove { } }
    override event System.Action IGoo.Event3 { add { } remove { } }

    sealed event System.Action IGoo.Event4 { add { } remove { } }

    new event System.Action IGoo.Event5 { add { } remove { } }

    public event System.Action IGoo.Event6 { add { } remove { } }
    protected event System.Action IGoo.Event7 { add { } remove { } }
    internal event System.Action IGoo.Event8 { add { } remove { } }
    protected internal event System.Action IGoo.Event9 { add { } remove { } } //roslyn considers 'protected internal' one modifier (two in dev10)
    private event System.Action IGoo.Event10 { add { } remove { } }

    extern event System.Action IGoo.Event11 { add { } remove { } }
    static event System.Action IGoo.Event12 { add { } remove { } }
}";
            // It seems Dev11 doesn't report ERR_ExternHasBody errors for Event11 accessors
            // if there are other explicitly implemented members with erroneous modifiers other than extern and abstract.
            // If the other errors are fixed ERR_ExternHasBody is reported. 
            // We report all errors at once since they are unrelated, not cascading.

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (20,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract event System.Action IGoo.Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event1").WithArguments("abstract").WithLocation(20, 39),
                // (21,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual event System.Action IGoo.Event2 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event2").WithArguments("virtual").WithLocation(21, 38),
                // (22,39): error CS0106: The modifier 'override' is not valid for this item
                //     override event System.Action IGoo.Event3 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event3").WithArguments("override").WithLocation(22, 39),
                // (24,37): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed event System.Action IGoo.Event4 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event4").WithArguments("sealed").WithLocation(24, 37),
                // (26,34): error CS0106: The modifier 'new' is not valid for this item
                //     new event System.Action IGoo.Event5 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event5").WithArguments("new").WithLocation(26, 34),
                // (28,37): error CS0106: The modifier 'public' is not valid for this item
                //     public event System.Action IGoo.Event6 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event6").WithArguments("public").WithLocation(28, 37),
                // (29,40): error CS0106: The modifier 'protected' is not valid for this item
                //     protected event System.Action IGoo.Event7 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event7").WithArguments("protected").WithLocation(29, 40),
                // (30,39): error CS0106: The modifier 'internal' is not valid for this item
                //     internal event System.Action IGoo.Event8 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event8").WithArguments("internal").WithLocation(30, 39),
                // (31,49): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal event System.Action IGoo.Event9 { add { } remove { } } //roslyn considers 'protected internal' one modifier (two in dev10)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event9").WithArguments("protected internal").WithLocation(31, 49),
                // (32,38): error CS0106: The modifier 'private' is not valid for this item
                //     private event System.Action IGoo.Event10 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Event10").WithArguments("private").WithLocation(32, 38),
                // (34,47): error CS0179: 'AbstractGoo.IGoo.Event11.add' cannot be extern and declare a body
                //     extern event System.Action IGoo.Event11 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("AbstractGoo.IGoo.Event11.add").WithLocation(34, 47),
                // (34,55): error CS0179: 'AbstractGoo.IGoo.Event11.remove' cannot be extern and declare a body
                //     extern event System.Action IGoo.Event11 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("AbstractGoo.IGoo.Event11.remove").WithLocation(34, 55),
                // (35,37): error CS8703: The modifier 'static' is not valid for this item in C# 9.0. Please use language version '11.0' or greater.
                //     static event System.Action IGoo.Event12 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "Event12").WithArguments("static", "9.0", "11.0").WithLocation(35, 37),
                // (35,37): error CS0539: 'AbstractGoo.Event12' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static event System.Action IGoo.Event12 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Event12").WithArguments("AbstractGoo.Event12").WithLocation(35, 37),
                // (18,30): error CS0535: 'AbstractGoo' does not implement interface member 'IGoo.Event12'
                // abstract class AbstractGoo : IGoo
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IGoo").WithArguments("AbstractGoo", "IGoo.Event12").WithLocation(18, 30)
                );
        }

        [Fact] // can't bind to events
        public void TestInvokeExplicitMemberDirectly()
        {
            // Tests:
            // Sanity check – it should be an error to invoke a member by its fully qualified explicit implementation name

            var text = @"
interface Interface
{
    void Method<T>();
    void Method(int i, long j);
    long Property { set; }
    event System.Action Event;
}
class Class : Interface
{
    void Interface.Method(int i, long j)
    {
        Interface.Method(1, 2);
    }

    void Interface.Method<T>()
    {
        Interface.Method<T>();
    }
    
    long Interface.Property { set { } }

    event System.Action Interface.Event { add { } remove { } }

    void Test()
    {
        Interface.Property = 2;
        Interface.Event += null;

        Class c = new Class();
        c.Interface.Method(1, 2);
        c.Interface.Method<string>();
        c.Interface.Property = 2;
        c.Interface.Event += null;
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (13,9): error CS0120: An object reference is required for the non-static field, method, or property 'Interface.Method(int, long)'
                //         Interface.Method(1, 2);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Interface.Method").WithArguments("Interface.Method(int, long)").WithLocation(13, 9),
                // (18,9): error CS0120: An object reference is required for the non-static field, method, or property 'Interface.Method<T>()'
                //         Interface.Method<T>();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Interface.Method<T>").WithArguments("Interface.Method<T>()").WithLocation(18, 9),
                // (27,9): error CS0120: An object reference is required for the non-static field, method, or property 'Interface.Property'
                //         Interface.Property = 2;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Interface.Property").WithArguments("Interface.Property").WithLocation(27, 9),
                // (28,9): error CS0120: An object reference is required for the non-static field, method, or property 'Interface.Event'
                //         Interface.Event += null;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Interface.Event").WithArguments("Interface.Event").WithLocation(28, 9),
                // (31,11): error CS1061: 'Class' does not contain a definition for 'Interface' and no extension method 'Interface' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         c.Interface.Method(1, 2);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Interface").WithArguments("Class", "Interface").WithLocation(31, 11),
                // (32,11): error CS1061: 'Class' does not contain a definition for 'Interface' and no extension method 'Interface' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         c.Interface.Method<string>();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Interface").WithArguments("Class", "Interface").WithLocation(32, 11),
                // (33,11): error CS1061: 'Class' does not contain a definition for 'Interface' and no extension method 'Interface' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         c.Interface.Property = 2;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Interface").WithArguments("Class", "Interface").WithLocation(33, 11),
                // (34,11): error CS1061: 'Class' does not contain a definition for 'Interface' and no extension method 'Interface' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         c.Interface.Event += null;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Interface").WithArguments("Class", "Interface").WithLocation(34, 11));
        }

        [Fact]
        public void TestHidesAbstractMethod()
        {
            var text = @"
abstract class AbstractGoo
{
    public abstract void Method1();
    public abstract void Method2();
    public abstract void Method3();
}

abstract class Goo : AbstractGoo
{
    public void Method1() { }
    public abstract void Method2();
    public virtual void Method3() { }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 11, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 11, Column = 17 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 12, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 12, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 13, Column = 25, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 13, Column = 25 },
            });
        }

        [Fact]
        public void TestHidesAbstractProperty()
        {
            var text = @"
abstract class AbstractGoo
{
    public abstract long Property1 { set; }
    public abstract long Property2 { set; }
    public abstract long Property3 { set; }
}

abstract class Goo : AbstractGoo
{
    public long Property1 { set { } }
    public abstract long Property2 { set; }
    public virtual long Property3 { set { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 11, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 11, Column = 17 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 12, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 12, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 13, Column = 25, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 13, Column = 25 },
            });
        }

        [Fact]
        public void TestHidesAbstractIndexer()
        {
            var text = @"
abstract class AbstractGoo
{
    public abstract long this[int x] { set; }
    public abstract long this[string x] { set; }
    public abstract long this[char x] { set; }
}

abstract class Goo : AbstractGoo
{
    public long this[int x] { set { } }
    public abstract long this[string x] { set; }
    public virtual long this[char x] { set { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 11, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 11, Column = 17 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 12, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 12, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 13, Column = 25, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod,  Line = 13, Column = 25 },
            });
        }

        [Fact]
        public void TestHidesAbstractEvent()
        {
            var text = @"
abstract class AbstractGoo
{
    public abstract event System.Action Event1;
    public abstract event System.Action Event2;
    public abstract event System.Action Event3;
}

abstract class Goo : AbstractGoo
{
    public event System.Action Event1 { add { } remove { } }
    public abstract event System.Action Event2;
    public virtual event System.Action Event3 { add { } remove { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,32): error CS0533: 'Goo.Event1' hides inherited abstract member 'AbstractGoo.Event1'
                //     public event System.Action Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Event1").WithArguments("Goo.Event1", "AbstractGoo.Event1"),
                // (11,32): warning CS0114: 'Goo.Event1' hides inherited member 'AbstractGoo.Event1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public event System.Action Event1 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event1").WithArguments("Goo.Event1", "AbstractGoo.Event1"),
                // (12,41): error CS0533: 'Goo.Event2' hides inherited abstract member 'AbstractGoo.Event2'
                //     public abstract event System.Action Event2 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Event2").WithArguments("Goo.Event2", "AbstractGoo.Event2"),
                // (12,41): warning CS0114: 'Goo.Event2' hides inherited member 'AbstractGoo.Event2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public abstract event System.Action Event2 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event2").WithArguments("Goo.Event2", "AbstractGoo.Event2"),
                // (13,40): error CS0533: 'Goo.Event3' hides inherited abstract member 'AbstractGoo.Event3'
                //     public virtual event System.Action Event3 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Event3").WithArguments("Goo.Event3", "AbstractGoo.Event3"),
                // (13,40): warning CS0114: 'Goo.Event3' hides inherited member 'AbstractGoo.Event3'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public virtual event System.Action Event3 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event3").WithArguments("Goo.Event3", "AbstractGoo.Event3"));
        }

        [Fact]
        public void TestNoMethodToOverride()
        {
            var text = @"
interface Interface
{
    void Method0();
}

class Base
{
    public virtual void Method1() { }
    private void Method2() { }
}

class Derived : Base, Interface
{
    public override void Method0() { }
    public override void Method1(int x) { }
    public override void Method2() { }
    public override void Method3() { }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 15, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 16, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 17, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 18, Column = 26 },
            });
        }

        [Fact]
        public void TestNoPropertyToOverride()
        {
            var text = @"
interface Interface
{
    int Property0 { get; set; }
}

class Base
{
    public virtual int Property1 { get; set; }
    private int Property2 { get; set; }
    public virtual int Property3 { get { return 0; } }
    public virtual int Property4 { get { return 0; } }
    public virtual int Property5 { set { } }
    public virtual int Property6 { set { } }
    public virtual int Property7 { get; set; }
    public virtual int Property8 { get; set; }
}

class Derived : Base, Interface
{
    public override int Property0 { get; set; } //iface
    public override double Property1 { get; set; } //wrong type
    public override int Property2 { get; set; } //inaccessible
    public override int Property3 { set { } } //wrong accessor(s)
    public override int Property4 { get; set; } //wrong accessor(s)
    public override int Property5 { get { return 0; } } //wrong accessor(s)
    public override int Property6 { get; set; } //wrong accessor(s)
    public override int Property7 { get { return 0; } } //wrong accessor(s)
    public override int Property8 { set { } } //wrong accessor(s)
    public override int Property9 { get; set; } //nothing to override
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 21, Column = 25 }, //0
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 22, Column = 28 }, //1
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 23, Column = 25 }, //2
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoSetToOverride, Line = 24, Column = 37 }, //3.set
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoSetToOverride, Line = 25, Column = 42 }, //4.set
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoGetToOverride, Line = 26, Column = 37 }, //5.get
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoGetToOverride, Line = 27, Column = 37 }, //6.get
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 30, Column = 25 }, //9
            });
        }

        [Fact]
        public void TestNoIndexerToOverride()
        {
            var text = @"
interface Interface
{
    int this[long w, long x, long y, long z] { get; set; }
}

class Base
{
    public virtual int this[long w, long x, long y, char z] { get { return 0; } set { } }
    private int this[long w, long x, char y, long z] { get { return 0; } set { } }
    public virtual int this[long w, long x, char y, char z] { get { return 0; } }
    public virtual int this[long w, char x, long y, long z] { get { return 0; } }
    public virtual int this[long w, char x, long y, char z] { set { } }
    public virtual int this[long w, char x, char y, long z] { set { } }
    public virtual int this[long w, char x, char y, char z] { get { return 0; } set { } }
    public virtual int this[char w, long x, long y, long z] { get { return 0; } set { } }
}

class Derived : Base, Interface
{
    public override int this[long w, long x, long y, long z] { get { return 0; } set { } } //iface
    public override double this[long w, long x, long y, char z] { get { return 0; } set { } } //wrong type
    public override int this[long w, long x, char y, long z] { get { return 0; } set { } } //inaccessible
    public override int this[long w, long x, char y, char z] { set { } } //wrong accessor(s)
    public override int this[long w, char x, long y, long z] { get { return 0; } set { } } //wrong accessor(s)
    public override int this[long w, char x, long y, char z] { get { return 0; } } //wrong accessor(s)
    public override int this[long w, char x, char y, long z] { get { return 0; } set { } } //wrong accessor(s)
    public override int this[long w, char x, char y, char z] { get { return 0; } } //wrong accessor(s)
    public override int this[char w, long x, long y, long z] { set { } } //wrong accessor(s)
    public override int this[string s] { get { return 0; } set { } } //nothing to override
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (21,25): error CS0115: 'Derived.this[long, long, long, long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[long, long, long, long]"),
                // (22,28): error CS1715: 'Derived.this[long, long, long, char]': type must be 'int' to match overridden member 'Base.this[long, long, long, char]'
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived.this[long, long, long, char]", "Base.this[long, long, long, char]", "int"),
                // (23,25): error CS0115: 'Derived.this[long, long, char, long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[long, long, char, long]"),
                // (24,64): error CS0546: 'Derived.this[long, long, char, char].set': cannot override because 'Base.this[long, long, char, char]' does not have an overridable set accessor
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived.this[long, long, char, char].set", "Base.this[long, long, char, char]"),
                // (25,82): error CS0546: 'Derived.this[long, char, long, long].set': cannot override because 'Base.this[long, char, long, long]' does not have an overridable set accessor
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived.this[long, char, long, long].set", "Base.this[long, char, long, long]"),
                // (26,64): error CS0545: 'Derived.this[long, char, long, char].get': cannot override because 'Base.this[long, char, long, char]' does not have an overridable get accessor
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived.this[long, char, long, char].get", "Base.this[long, char, long, char]"),
                // (27,64): error CS0545: 'Derived.this[long, char, char, long].get': cannot override because 'Base.this[long, char, char, long]' does not have an overridable get accessor
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived.this[long, char, char, long].get", "Base.this[long, char, char, long]"),
                // (30,25): error CS0115: 'Derived.this[string]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[string]"));
        }

        [Fact]
        public void TestNoEventToOverride()
        {
            var text = @"
interface Interface
{
    event System.Action Event0;
}

class Base
{
    public virtual event System.Action Event1 { add { } remove { } }
    private event System.Action Event2 { add { } remove { } }
}

class Derived : Base, Interface
{
    public override event System.Action Event0 { add { } remove { } } //iface
    public override event System.Func<int> Event1 { add { } remove { } } //wrong type
    public override event System.Action Event2 { add { } remove { } } //inaccessible
    public override event System.Action Event3 { add { } remove { } } //nothing to override
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,41): error CS0115: 'Derived.Event0': no suitable method found to override
                //     public override event System.Action Event0 { add { } remove { } } //iface
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Event0").WithArguments("Derived.Event0"),
                // (16,44): error CS1715: 'Derived.Event1': type must be 'System.Action' to match overridden member 'Base.Event1'
                //     public override event System.Func<int> Event1 { add { } remove { } } //wrong type
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "Event1").WithArguments("Derived.Event1", "Base.Event1", "System.Action"),
                // (17,41): error CS0115: 'Derived.Event2': no suitable method found to override
                //     public override event System.Action Event2 { add { } remove { } } //inaccessible
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Event2").WithArguments("Derived.Event2"),
                // (18,41): error CS0115: 'Derived.Event3': no suitable method found to override
                //     public override event System.Action Event3 { add { } remove { } } //nothing to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Event3").WithArguments("Derived.Event3"));
        }

        [Fact]
        public void TestSuppressOverrideNotExpectedErrorWhenMethodParameterTypeNotFound()
        {
            var text = @"
class Base
{
}

class Derived : Base
{
    public override void Method0(String x) { }
    public override void Method1(string x, String y) { }
    public override void Method2(String[] x) { }
    public override void Method3(System.Func<String> x) { }
    public override void Method4((string a, String b) x) { }
    public override void Method5(System.Func<(string a, String[] b)> x) { }
    public override void Method6(Outer<String>.Inner<string> x) { }
    public override void Method7(Outer<string>.Inner<String> x) { }
    public override void Method8(Int? x) { }
}

class Outer<T>
{
    public class Inner<U>{}
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,34): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method0(String x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(8, 34),
                // (9,44): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method1(string x, String y) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(9, 44),
                // (10,34): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method2(String[] x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(10, 34),
                // (11,46): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method3(System.Func<String> x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(11, 46),
                // (12,45): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method4((string a, String b) x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(12, 45),
                // (13,57): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method5(System.Func<(string a, String[] b)> x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(13, 57),
                // (14,40): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method6(Outer<String>.Inner<string> x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(14, 40),
                // (15,54): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method7(Outer<string>.Inner<String> x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(15, 54),
                // (16,34): error CS0246: The type or namespace name 'Int' could not be found (are you missing a using directive or an assembly reference?)
                //     public override void Method8(Int? x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int").WithArguments("Int").WithLocation(16, 34));
        }

        [Fact]
        public void TestSuppressOverrideNotExpectedErrorWhenIndexerParameterTypeNotFound()
        {
            var text = @"
class Base
{
}

class Derived : Base
{
    public override int this[String x] => 0;
    public override int this[string x, String y] => 0;
    public override int this[String[] x] => 0;
    public override int this[System.Func<String> x] => 0;
    public override int this[(string a, String b) x] => 0;
    public override int this[System.Func<(string a, String[] b)> x] => 0;
    public override int this[Outer<String>.Inner<string> x] => 0;
    public override int this[Outer<string>.Inner<String> x] => 0;
    public override int this[Int? x] => 0;
}

class Outer<T>
{
    public class Inner<U>{}
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,30): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[String x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(8, 30),
                // (9,40): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[string x, String y] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(9, 40),
                // (10,30): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[String[] x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(10, 30),
                // (11,42): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[System.Func<String> x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(11, 42),
                // (12,41): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[(string a, String b) x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(12, 41),
                // (13,53): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[System.Func<(string a, String[] b)> x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(13, 53),
                // (14,36): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[Outer<String>.Inner<string> x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(14, 36),
                // (15,50): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[Outer<string>.Inner<String> x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(15, 50),
                // (16,30): error CS0246: The type or namespace name 'Int' could not be found (are you missing a using directive or an assembly reference?)
                //     public override int this[Int? x] => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int").WithArguments("Int").WithLocation(16, 30));
        }

        [Fact]
        public void TestSuppressCantChangeReturnTypeErrorWhenMethodReturnTypeNotFound()
        {
            var text = @"
abstract class Base
{
    public abstract void Method0();
    public abstract void Method1();
    public abstract void Method2();
    public abstract void Method3();
    public abstract void Method4();
    public abstract void Method5();
    public abstract void Method6();
    public abstract void Method7();
}

class Derived : Base
{
    public override String Method0() => null;
    public override String[] Method1() => null;
    public override System.Func<String> Method2() => null;
    public override (string a, String b) Method3() => (null, null);
    public override System.Func<(string a, String[] b)> Method4() => null;
    public override Outer<String>.Inner<string> Method5() => null;
    public override Outer<string>.Inner<String> Method6() => null;
    public override Int? Method7() => null;
}

class Outer<T>
{
    public class Inner<U> { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,21): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override String Method0() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(16, 21),
                // (17,21): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override String[] Method1() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(17, 21),
                // (18,33): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override System.Func<String> Method2() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(18, 33),
                // (19,32): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override (string a, String b) Method3() => (null, null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(19, 32),
                // (20,44): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override System.Func<(string a, String[] b)> Method4() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(20, 44),
                // (21,27): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Outer<String>.Inner<string> Method5() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(21, 27),
                // (22,41): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Outer<string>.Inner<String> Method6() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(22, 41),
                // (23,21): error CS0246: The type or namespace name 'Int' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Int? Method7() => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int").WithArguments("Int").WithLocation(23, 21));
        }

        [Fact]
        public void TestSuppressCantChangeTypeErrorWhenPropertyTypeNotFound()
        {
            var text = @"
abstract class Base
{
    public abstract int Property0 { get; }
    public abstract int Property1 { get; }
    public abstract int Property2 { get; }
    public abstract int Property3 { get; }
    public abstract int Property4 { get; }
    public abstract int Property5 { get; }
    public abstract int Property6 { get; }
    public abstract int Property7 { get; }
}

class Derived : Base
{
    public override String Property0 => null;
    public override String[] Property1 => null;
    public override System.Func<String> Property2 => null;
    public override (string a, String b) Property3 => (null, null);
    public override System.Func<(string a, String[] b)> Property4 => null;
    public override Outer<String>.Inner<string> Property5 => null;
    public override Outer<string>.Inner<String> Property6 => null;
    public override Int? Property7 => null;
}

class Outer<T>
{
    public class Inner<U> { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,21): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override String Property0 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(16, 21),
                // (17,21): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override String[] Property1 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(17, 21),
                // (18,33): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override System.Func<String> Property2 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(18, 33),
                // (19,32): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override (string a, String b) Property3 => (null, null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(19, 32),
                // (20,44): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override System.Func<(string a, String[] b)> Property4 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(20, 44),
                // (21,27): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Outer<String>.Inner<string> Property5 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(21, 27),
                // (22,41): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Outer<string>.Inner<String> Property6 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(22, 41),
                // (23,21): error CS0246: The type or namespace name 'Int' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Int? Property7 => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int").WithArguments("Int").WithLocation(23, 21));
        }

        [Fact]
        public void TestSuppressCantChangeTypeErrorWhenIndexerTypeNotFound()
        {
            var text = @"
abstract class Base
{
    public abstract int this[int index] { get; }
}

class Derived : Base
{
    public override String this[int index] => null;
    public override String[] this[int index] => null;
    public override System.Func<String> this[int index] => null;
    public override (string a, String b) this[int index] => (null, null);
    public override System.Func<(string a, String[] b)> this[int index] => null;
    public override Outer<String>.Inner<string> this[int index] => null;
    public override Outer<string>.Inner<String> this[int index] => null;
    public override Int? this[int index] => null;
}

class Outer<T>
{
    public class Inner<U> { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,21): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override String this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(9, 21),
                // (10,21): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override String[] this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(10, 21),
                // (11,33): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override System.Func<String> this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(11, 33),
                // (12,32): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override (string a, String b) this[int index] => (null, null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(12, 32),
                // (13,44): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override System.Func<(string a, String[] b)> this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(13, 44),
                // (14,27): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Outer<String>.Inner<string> this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(14, 27),
                // (15,41): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Outer<string>.Inner<String> this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(15, 41),
                // (16,21): error CS0246: The type or namespace name 'Int' could not be found (are you missing a using directive or an assembly reference?)
                //     public override Int? this[int index] => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int").WithArguments("Int").WithLocation(16, 21),
                // (10,30): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override String[] this[int index] => null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(10, 30),
                // (11,41): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override System.Func<String> this[int index] => null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(11, 41),
                // (12,42): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override (string a, String b) this[int index] => (null, null);
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(12, 42),
                // (13,57): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override System.Func<(string a, String[] b)> this[int index] => null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(13, 57),
                // (14,49): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override Outer<String>.Inner<string> this[int index] => null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(14, 49),
                // (15,49): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override Outer<string>.Inner<String> this[int index] => null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(15, 49),
                // (16,26): error CS0111: Type 'Derived' already defines a member called 'this' with the same parameter types
                //     public override Int? this[int index] => null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "Derived").WithLocation(16, 26));
        }

        [Fact]
        public void TestSuppressCantChangeTypeErrorWhenEventTypeNotFound()
        {
            var text = @"
abstract class Base
{
    public abstract event System.Action Event0;
    public abstract event System.Action Event1;
    public abstract event System.Action Event2;
    public abstract event System.Action Event3;
    public abstract event System.Action Event4;
    public abstract event System.Action Event5;
    public abstract event System.Action Event6;
    public abstract event System.Action Event7;
}

class Derived : Base
{
    public override event String Event0;
    public override event String[] Event1;
    public override event System.Func<String> Event2;
    public override event (string a, String b) Event3;
    public override event System.Func<(string a, String[] b)> Event4;
    public override event Outer<String>.Inner<string> Event5;
    public override event Outer<string>.Inner<String> Event6;
    public override event Int? Event7;
}

class Outer<T>
{
	public class Inner<U> { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,27): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event String Event0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(16, 27),
                // (17,27): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event String[] Event1;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(17, 27),
                // (18,39): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event System.Func<String> Event2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(18, 39),
                // (19,38): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event (string a, String b) Event3;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(19, 38),
                // (20,50): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event System.Func<(string a, String[] b)> Event4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(20, 50),
                // (21,33): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event Outer<String>.Inner<string> Event5;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(21, 33),
                // (22,47): error CS0246: The type or namespace name 'String' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event Outer<string>.Inner<String> Event6;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "String").WithArguments("String").WithLocation(22, 47),
                // (23,27): error CS0246: The type or namespace name 'Int' could not be found (are you missing a using directive or an assembly reference?)
                //     public override event Int? Event7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int").WithArguments("Int").WithLocation(23, 27),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event6.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event6.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event1.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event1.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event1.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event1.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event5.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event5.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event3.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event3.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event2.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event2.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event4.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event4.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event4.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event4.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event3.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event3.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event7.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event7.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event0.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event0.add").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event6.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event6.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event2.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event2.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event7.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event7.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event0.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event0.remove").WithLocation(14, 7),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event5.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event5.remove").WithLocation(14, 7),
                // (17,36): error CS0066: 'Derived.Event1': event must be of a delegate type
                //     public override event String[] Event1;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Event1").WithArguments("Derived.Event1").WithLocation(17, 36),
                // (19,48): error CS0066: 'Derived.Event3': event must be of a delegate type
                //     public override event (string a, String b) Event3;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Event3").WithArguments("Derived.Event3").WithLocation(19, 48),
                // (21,55): error CS0066: 'Derived.Event5': event must be of a delegate type
                //     public override event Outer<String>.Inner<string> Event5;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Event5").WithArguments("Derived.Event5").WithLocation(21, 55),
                // (22,55): error CS0066: 'Derived.Event6': event must be of a delegate type
                //     public override event Outer<string>.Inner<String> Event6;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Event6").WithArguments("Derived.Event6").WithLocation(22, 55),
                // (23,32): error CS0066: 'Derived.Event7': event must be of a delegate type
                //     public override event Int? Event7;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Event7").WithArguments("Derived.Event7").WithLocation(23, 32),
                // (19,48): warning CS0067: The event 'Derived.Event3' is never used
                //     public override event (string a, String b) Event3;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event3").WithArguments("Derived.Event3").WithLocation(19, 48),
                // (23,32): warning CS0067: The event 'Derived.Event7' is never used
                //     public override event Int? Event7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event7").WithArguments("Derived.Event7").WithLocation(23, 32),
                // (17,36): warning CS0067: The event 'Derived.Event1' is never used
                //     public override event String[] Event1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event1").WithArguments("Derived.Event1").WithLocation(17, 36),
                // (22,55): warning CS0067: The event 'Derived.Event6' is never used
                //     public override event Outer<string>.Inner<String> Event6;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event6").WithArguments("Derived.Event6").WithLocation(22, 55),
                // (18,47): warning CS0067: The event 'Derived.Event2' is never used
                //     public override event System.Func<String> Event2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event2").WithArguments("Derived.Event2").WithLocation(18, 47),
                // (21,55): warning CS0067: The event 'Derived.Event5' is never used
                //     public override event Outer<String>.Inner<string> Event5;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event5").WithArguments("Derived.Event5").WithLocation(21, 55),
                // (20,63): warning CS0067: The event 'Derived.Event4' is never used
                //     public override event System.Func<(string a, String[] b)> Event4;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event4").WithArguments("Derived.Event4").WithLocation(20, 63),
                // (16,34): warning CS0067: The event 'Derived.Event0' is never used
                //     public override event String Event0;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event0").WithArguments("Derived.Event0").WithLocation(16, 34));
        }

        [Fact]
        public void TestOverrideSealedMethod()
        {
            var text = @"
class Base
{
    public sealed override string ToString()
    {
        return ""Base"";
    }
}

class Derived : Base
{
    public override string ToString()
    {
        return ""Derived"";
    }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideSealed, Line = 12, Column = 28 },
            });
        }

        [Fact]
        public void TestOverrideSealedProperty()
        {
            var text = @"
class Base0
{
    public virtual int Property { get; set; }
}

class Base : Base0
{
    public sealed override int Property { get; set; }
}

class Derived : Base
{
    public override int Property { get; set; }
}
";
            // CONSIDER: Dev10 reports on both accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideSealed, Line = 14, Column = 25 }, //Derived.Property
            });
        }

        [Fact]
        public void TestOverrideSealedIndexer()
        {
            var text = @"
class Base0
{
    public virtual int this[int x] { get { return 0; } set { } }
}

class Base : Base0
{
    public sealed override int this[int x] { get { return 0; } set { } }
}

class Derived : Base
{
    public override int this[int x] { get { return 0; } set { } }
}
";
            // CONSIDER: Dev10 reports on both accessors, but not on the indexer itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideSealed, Line = 14, Column = 25 }, //Derived indexer
            });
        }

        [Fact]
        public void TestOverrideSealedEvents()
        {
            var text = @"
class Base0
{
    public virtual event System.Action Event { add { } remove { } }
}

class Base : Base0
{
    public sealed override event System.Action Event { add { } remove { } }
}

class Derived : Base
{
    public override event System.Action Event { add { } remove { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (14,41): error CS0239: 'Derived.Event': cannot override inherited member 'Base.Event' because it is sealed
                //     public override event System.Action Event { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "Event").WithArguments("Derived.Event", "Base.Event"));
        }

        [Fact]
        public void TestOverrideSealedPropertyOmitAccessors()
        {
            var text = @"
class Base0
{
    public virtual int Property { get; set; }
}

class Base : Base0
{
    public sealed override int Property { set { } }
}

class Derived : Base
{
    public override int Property { get { return 0; } }
}
";
            // CONSIDER: Dev10 reports on accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideSealed, Line = 14, Column = 25 }, //Derived.Property
            });
        }

        [Fact]
        public void TestOverrideSealedIndexerOmitAccessors()
        {
            var text = @"
class Base0
{
    public virtual int this[int x] { get { return 0; } set { } }
}

class Base : Base0
{
    public sealed override int this[int x] { set { } }
}

class Derived : Base
{
    public override int this[int x] { get { return 0; } }
}
";
            // CONSIDER: Dev10 reports on accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideSealed, Line = 14, Column = 25 }, //Derived indexer
            });
        }

        [Fact]
        public void TestOverrideSameMemberMultipleTimes()
        {
            // Tests:
            // Override same virtual / abstract member more than once in different parts of a (partial) derived type

            var text = @"
using @str = System.String;

class Base
{
    public virtual string Method1() { return string.Empty; }
    public virtual string Method2() { return string.Empty; }
}

class Derived : Base
{
    public override System.String Method1() { return null; }
    public override string Method1() { return null; }
}

partial class Derived2 : Base
{
    public override string Method2() { return null; }
}
partial class Derived2
{
    public override string Method2() { return null; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,28): error CS0111: Type 'Derived' already defines a member called 'Method1' with the same parameter types
                //     public override string Method1() { return null; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method1").WithArguments("Method1", "Derived").WithLocation(13, 28),
                // (22,28): error CS0111: Type 'Derived2' already defines a member called 'Method2' with the same parameter types
                //     public override string Method2() { return null; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method2").WithArguments("Method2", "Derived2").WithLocation(22, 28),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using @str = System.String;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using @str = System.String;").WithLocation(2, 1));
        }

        [Fact]
        public void TestOverrideNonMethodWithMethod()
        {
            var text = @"
class Base
{
    public int field;
    public int Property { get { return 0; } }
    public interface Interface { }
    public class Class { }
    public struct Struct { }
    public enum Enum { Element }
    public delegate void Delegate();
    public event Delegate Event;
}

class Derived : Base
{
    public override int field() { return 1; }
    public override int Property() { return 1; }
    public override int Interface() { return 1; }
    public override int Class() { return 1; }
    public override int Struct() { return 1; }
    public override int Enum() { return 1; }
    public override int Delegate() { return 1; }
    public override int Event() { return 1; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,25): error CS0505: 'Derived.field()': cannot override because 'Base.field' is not a function
                //     public override int field() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "field").WithArguments("Derived.field()", "Base.field"),
                // (17,25): error CS0505: 'Derived.Property()': cannot override because 'Base.Property' is not a function
                //     public override int Property() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Property").WithArguments("Derived.Property()", "Base.Property"),
                // (18,25): error CS0505: 'Derived.Interface()': cannot override because 'Base.Interface' is not a function
                //     public override int Interface() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Interface").WithArguments("Derived.Interface()", "Base.Interface"),
                // (19,25): error CS0505: 'Derived.Class()': cannot override because 'Base.Class' is not a function
                //     public override int Class() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Class").WithArguments("Derived.Class()", "Base.Class"),
                // (20,25): error CS0505: 'Derived.Struct()': cannot override because 'Base.Struct' is not a function
                //     public override int Struct() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Struct").WithArguments("Derived.Struct()", "Base.Struct"),
                // (21,25): error CS0505: 'Derived.Enum()': cannot override because 'Base.Enum' is not a function
                //     public override int Enum() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Enum").WithArguments("Derived.Enum()", "Base.Enum"),
                // (22,25): error CS0505: 'Derived.Delegate()': cannot override because 'Base.Delegate' is not a function
                //     public override int Delegate() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Delegate").WithArguments("Derived.Delegate()", "Base.Delegate"),
                // (23,25): error CS0505: 'Derived.Event()': cannot override because 'Base.Event' is not a function
                //     public override int Event() { return 1; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Event").WithArguments("Derived.Event()", "Base.Event"),
                // (4,16): warning CS0649: Field 'Base.field' is never assigned to, and will always have its default value 0
                //     public int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("Base.field", "0"),
                // (11,27): warning CS0067: The event 'Base.Event' is never used
                //     public event Delegate Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("Base.Event")
                );
        }

        [Fact]
        public void TestOverrideNonPropertyWithProperty()
        {
            var text = @"
class Base
{
    public int field;
    public int Method() { return 0; }
    public interface Interface { }
    public class Class { }
    public struct Struct { }
    public enum Enum { Element }
    public delegate void Delegate();
    public event Delegate Event;
}

class Derived : Base
{
    public override int field { get; set; }
    public override int Method { get; set; }
    public override int Interface { get; set; }
    public override int Class { get; set; }
    public override int Struct { get; set; }
    public override int Enum { get; set; }
    public override int Delegate { get; set; }
    public override int Event { get; set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,25): error CS0544: 'Derived.field': cannot override because 'Base.field' is not a property
                //     public override int field { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "field").WithArguments("Derived.field", "Base.field"),
                // (17,25): error CS0544: 'Derived.Method': cannot override because 'Base.Method()' is not a property
                //     public override int Method { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Method").WithArguments("Derived.Method", "Base.Method()"),
                // (18,25): error CS0544: 'Derived.Interface': cannot override because 'Base.Interface' is not a property
                //     public override int Interface { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Interface").WithArguments("Derived.Interface", "Base.Interface"),
                // (19,25): error CS0544: 'Derived.Class': cannot override because 'Base.Class' is not a property
                //     public override int Class { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Class").WithArguments("Derived.Class", "Base.Class"),
                // (20,25): error CS0544: 'Derived.Struct': cannot override because 'Base.Struct' is not a property
                //     public override int Struct { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Struct").WithArguments("Derived.Struct", "Base.Struct"),
                // (21,25): error CS0544: 'Derived.Enum': cannot override because 'Base.Enum' is not a property
                //     public override int Enum { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Enum").WithArguments("Derived.Enum", "Base.Enum"),
                // (22,25): error CS0544: 'Derived.Delegate': cannot override because 'Base.Delegate' is not a property
                //     public override int Delegate { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Delegate").WithArguments("Derived.Delegate", "Base.Delegate"),
                // (23,25): error CS0544: 'Derived.Event': cannot override because 'Base.Event' is not a property
                //     public override int Event { get; set; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Event").WithArguments("Derived.Event", "Base.Event"),
                // (11,27): warning CS0067: The event 'Base.Event' is never used
                //     public event Delegate Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("Base.Event"),
                // (4,16): warning CS0649: Field 'Base.field' is never assigned to, and will always have its default value 0
                //     public int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("Base.field", "0")
                );
        }

        [Fact]
        public void TestOverrideNonEventWithEvent()
        {
            var text = @"
class Base
{
    public int field;
    public int Property { get { return 0; } }
    public int Method() { return 0; }
    public interface Interface { }
    public class Class { }
    public struct Struct { }
    public enum Enum { Element }
    public delegate void Delegate();
}

class Derived : Base
{
    public override event System.Action field { add { } remove { } }
    public override event System.Action Property { add { } remove { } }
    public override event System.Action Method { add { } remove { } }
    public override event System.Action Interface { add { } remove { } }
    public override event System.Action Class { add { } remove { } }
    public override event System.Action Struct { add { } remove { } }
    public override event System.Action Enum { add { } remove { } }
    public override event System.Action Delegate { add { } remove { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,41): error CS0072: 'Derived.field': cannot override; 'Base.field' is not an event
                //     public override event System.Action field { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "field").WithArguments("Derived.field", "Base.field"),
                // (17,41): error CS0072: 'Derived.Property': cannot override; 'Base.Property' is not an event
                //     public override event System.Action Property { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Property").WithArguments("Derived.Property", "Base.Property"),
                // (18,41): error CS0072: 'Derived.Method': cannot override; 'Base.Method()' is not an event
                //     public override event System.Action Method { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Method").WithArguments("Derived.Method", "Base.Method()"),
                // (19,41): error CS0072: 'Derived.Interface': cannot override; 'Base.Interface' is not an event
                //     public override event System.Action Interface { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Interface").WithArguments("Derived.Interface", "Base.Interface"),
                // (20,41): error CS0072: 'Derived.Class': cannot override; 'Base.Class' is not an event
                //     public override event System.Action Class { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Class").WithArguments("Derived.Class", "Base.Class"),
                // (21,41): error CS0072: 'Derived.Struct': cannot override; 'Base.Struct' is not an event
                //     public override event System.Action Struct { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Struct").WithArguments("Derived.Struct", "Base.Struct"),
                // (22,41): error CS0072: 'Derived.Enum': cannot override; 'Base.Enum' is not an event
                //     public override event System.Action Enum { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Enum").WithArguments("Derived.Enum", "Base.Enum"),
                // (23,41): error CS0072: 'Derived.Delegate': cannot override; 'Base.Delegate' is not an event
                //     public override event System.Action Delegate { add { } remove { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "Delegate").WithArguments("Derived.Delegate", "Base.Delegate"),
                // (4,16): warning CS0649: Field 'Base.field' is never assigned to, and will always have its default value 0
                //     public int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("Base.field", "0")
                );
        }

        [Fact]
        public void TestOverrideNonVirtualMethod()
        {
            var text = @"
class Base
{
    public virtual void Method1() { }
}

class Derived : Base
{
    public new void Method1() { }
    public void Method2() { }
}

class Derived2 : Derived
{
    public override void Method1() { }
    public override void Method2() { }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 15, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 16, Column = 26 },
            });
        }

        [Fact]
        public void TestOverrideNonVirtualProperty()
        {
            var text = @"
class Base
{
    public virtual long Property1 { get; set; }
}

class Derived : Base
{
    public new long Property1 { get; set; }
    public long Property2 { get; set; }
}

class Derived2 : Derived
{
    public override long Property1 { get; set; }
    public override long Property2 { get; set; }
}
";
            // CONSIDER: Dev10 reports on both accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 15, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 16, Column = 26 },
            });
        }

        [Fact]
        public void TestOverrideNonVirtualIndexer()
        {
            var text = @"
class Base
{
    public virtual long this[int x] { get { return 0; } set { } }
}

class Derived : Base
{
    public new long this[int x] { get { return 0; } set { } }
    public long this[string x] { get { return 0; } set { } }
}

class Derived2 : Derived
{
    public override long this[int x] { get { return 0; } set { } }
    public override long this[string x] { get { return 0; } set { } }
}
";
            // CONSIDER: Dev10 reports on both accessors, but not on the indexer itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 15, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 16, Column = 26 },
            });
        }

        [Fact]
        public void TestOverrideNonVirtualPropertyOmitAccessors()
        {
            var text = @"
class Base
{
    public virtual long Property1 { get; set; }
}

class Derived : Base
{
    public new long Property1 { get { return 0; } }
    public long Property2 { get; set; }
}

class Derived2 : Derived
{
    public override long Property1 { set { } }
    public override long Property2 { get { return 0;  } }
}
";
            // CONSIDER: Dev10 reports on accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 15, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 16, Column = 26 },
            });
        }

        [Fact]
        public void TestOverrideNonVirtualIndexerOmitAccessors()
        {
            var text = @"
class Base
{
    public virtual long this[int x] { get { return 0; } set { } }
}

class Derived : Base
{
    public new long this[int x] { get { return 0; } }
    public long this[string x] { get { return 0; } set { } }
}

class Derived2 : Derived
{
    public override long this[int x] { set { } }
    public override long this[string x] { get { return 0;  } }
}
";
            // CONSIDER: Dev10 reports on accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 15, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 16, Column = 26 },
            });
        }

        [Fact]
        public void TestOverrideNonVirtualEvent()
        {
            var text = @"
class Base
{
    public virtual event System.Action Event1 { add { } remove { } }
}

class Derived : Base
{
    public new event System.Action Event1 { add { } remove { } }
    public event System.Action Event2 { add { } remove { } }
}

class Derived2 : Derived
{
    public override event System.Action Event1 { add { } remove { } }
    public override event System.Action Event2 { add { } remove { } }
}
";
            // CONSIDER: Dev10 reports on both accessors, but not on the property itself
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 15, Column = 41 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 16, Column = 41 },
            });
        }

        [Fact]
        public void TestChangeMethodReturnType()
        {
            var text = @"
using @str = System.String;

class Base
{
    public virtual string Method1() { return string.Empty; }
    public virtual string Method2() { return string.Empty; }
    public virtual string Method3() { return string.Empty; }
    public virtual string Method4() { return string.Empty; }
    public virtual string Method5() { return string.Empty; }
}

class Derived : Base
{
    public override System.String Method1() { return null; }
    public override str Method2() { return null; }
    public override object Method3() { return null; }
    public override int Method4() { return 0; }
    public override void Method5() { }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeReturnTypeOnOverride, Line = 17, Column = 28 }, //3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeReturnTypeOnOverride, Line = 18, Column = 25 }, //4
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeReturnTypeOnOverride, Line = 19, Column = 26 }, //5
            });
        }

        [Fact]
        public void TestChangeMethodRefReturn()
        {
            var text = @"
class Base
{
    public virtual int Method1() { return 0; }
    public virtual ref int Method2(ref int i) { return ref i; }
    public virtual ref int Method3(ref int i) { return ref i; }
}

class Derived : Base
{
    int field = 0;

    public override ref int Method1() { return ref field; }
    public override int Method2(ref int i) { return i; }
    public override ref int Method3(ref int i) { return ref i; }
}
";

            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (13,29): error CS8148: 'Derived.Method1()' must match by reference return of overridden member 'Base.Method1()'
                //     public override ref int Method1() { return ref field; }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Method1").WithArguments("Derived.Method1()", "Base.Method1()").WithLocation(13, 29),
                // (14,25): error CS8148: 'Derived.Method2(ref int)' must match by reference return of overridden member 'Base.Method2(ref int)'
                //     public override int Method2(ref int i) { return i; }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Method2").WithArguments("Derived.Method2(ref int)", "Base.Method2(ref int)").WithLocation(14, 25));
        }

        [Fact]
        public void TestChangeMethodParameters()
        {
            // Tests:
            // Change parameter count / types of overridden member
            // Omit / toggle ref / out in overridden member
            // Override base member with a signature that only differs by optional parameters
            // Override base member with a signature that only differs by params
            // Change default value of optional argument in overridden member

            var text = @"
using @str = System.String;
using @integer = System.Int32;
abstract class Base
{
    public virtual string Method1(int i) { return string.Empty; }
    public virtual string Method2(long j) { return string.Empty; }
    public virtual string Method2(short j) { return string.Empty; }
    public virtual string Method3(System.Exception x, System.ArgumentException y) { return string.Empty; }
    public virtual string Method3(System.ArgumentException x, System.Exception y) { return string.Empty; }
    public abstract string Method4(str[] x, str[][] y);
    public abstract string Method5(System.Collections.Generic.List<str> x, System.Collections.Generic.Dictionary<int, long> y);
    public virtual string Method6(int i, params long[] j) { return string.Empty; }
    public virtual string Method7(int i, short j = 1) { return string.Empty; }
    public virtual string Method8(ref long j) { return string.Empty; }
    public abstract string Method9(out int j);
}
abstract class Derived : Base
{
    public override str Method1(int i, long j = 1) { return string.Empty; }
    public override str Method1(int i, params int[] j) { return string.Empty; }
    public override str Method1(double i) { return string.Empty; }
    public override str Method2(int j) { return string.Empty; }
    public override str Method3(System.Exception x, System.ArgumentException y, System.Exception z) { return string.Empty; }
    public override str Method3(System.ArgumentException x, System.ArgumentException y) { return string.Empty; }
    public override str Method3() { return string.Empty; }
    public override str Method3(System.Exception x) { return string.Empty; }
    public override str Method4(str[] x, str[] y) { return string.Empty; }
    public override str Method4(str[][] y, str[] x) { return string.Empty; }
    public override str Method4(str[] x) { return string.Empty; }
    public override str Method5(System.Collections.Generic.List<int> x, System.Collections.Generic.Dictionary<str, long> y) { return string.Empty; }
    public override str Method5(System.Collections.Generic.Dictionary<int, long> x, System.Collections.Generic.List<string> y) { return string.Empty; }
    public override str Method5(System.Collections.Generic.List<string> y, System.Collections.Generic.Dictionary<integer, long> x) { return string.Empty; } // Not an error
    public override str Method6(int i, long j = 1) { return string.Empty; }
    public override str Method6(int i) { return string.Empty; }
    public override str Method7(int i, params short[] j) { return string.Empty; }
    public override str Method7(int i) { return string.Empty; }
    public override str Method7(int i, short j = 1, short k = 1) { return string.Empty; }
    public override str Method8(out long j) { j = 1; return string.Empty; }
    public override str Method8(long j) { return string.Empty; }
    public override str Method9(ref int j) { return string.Empty; }
    public override str Method9(int j) { return string.Empty; }
    public override str Method1(ref int i) { return string.Empty; }
    public override str Method2(out long j) { j = 2; return string.Empty; }
    public override str Method7(int i, short j = short.MaxValue) { return string.Empty; } // Not an error
}
";

            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 20, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 21, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 22, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 23, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 24, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 25, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 26, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 27, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 28, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 29, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 30, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 31, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 32, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 34, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 35, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 36, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 37, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 38, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 39, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 41, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 42, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 43, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 44, Column = 25 }
            });
        }

        [Fact]
        public void TestChangePropertyType()
        {
            var text = @"
using @str = System.String;

class Base
{
    public virtual string Property1 { get; set; }
    public virtual string Property2 { get; set; }
    public virtual string Property3 { get; set; }
    public virtual string Property4 { get; set; }
}

class Derived : Base
{
    public override System.String Property1 { get; set; }
    public override str Property2 { get; set; }
    public override object Property3 { get; set; }
    public override int Property4 { get; set; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 16, Column = 28 }, //3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 17, Column = 25 }, //4
            });
        }

        [Fact]
        public void TestChangePropertyRefReturn()
        {
            var text = @"
class Base
{
    int field = 0;

    public virtual int Proprty1 { get { return 0; } }
    public virtual ref int Property2 { get { return ref @field; } }
    public virtual ref int Property3 { get { return ref @field; } }
}

class Derived : Base
{
    int field = 0;

    public override ref int Proprty1 { get { return ref @field; } }
    public override int Property2 { get { return 0; } }
    public override ref int Property3 { get { return ref @field; } }
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (15,29): error CS8148: 'Derived.Proprty1' must match by reference return of overridden member 'Base.Proprty1'
                //     public override ref int Proprty1 { get { return ref field; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Proprty1").WithArguments("Derived.Proprty1", "Base.Proprty1").WithLocation(15, 29),
                // (16,25): error CS8148: 'Derived.Property2' must match by reference return of overridden member 'Base.Property2'
                //     public override int Property2 { get { return 0; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Property2").WithArguments("Derived.Property2", "Base.Property2").WithLocation(16, 25));
        }

        [Fact]
        public void TestChangeIndexerType()
        {
            var text = @"
using @str = System.String;

class Base
{
    public virtual string this[int x, int y] { get { return null; } set { } }
    public virtual string this[int x, string y] { get { return null; } set { } }
    public virtual string this[string x, int y] { get { return null; } set { } }
    public virtual string this[string x, string y] { get { return null; } set { } }
}

class Derived : Base
{
    public override System.String this[int x, int y] { get { return null; } set { } }
    public override str this[int x, string y] { get { return null; } set { } }
    public override object this[string x, int y] { get { return null; } set { } }
    public override int this[string x, string y] { get { return 0; } set { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 16, Column = 28 }, //3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 17, Column = 25 }, //4
            });
        }

        [Fact]
        public void TestChangeIndexerRefReturn()
        {
            var text = @"
class Base
{
    int field = 0;

    public virtual int this[int x, int y] { get { return field; } }
    public virtual ref int this[int x, string y] { get { return ref field; } }
    public virtual ref int this[string x, int y] { get { return ref field; } }
}

class Derived : Base
{
    int field = 0;

    public override ref int this[int x, int y] { get { return ref field; } }
    public override int this[int x, string y] { get { return field; } }
    public override ref int this[string x, int y] { get { return ref field; } }
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (15,29): error CS8148: 'Derived.this[int, int]' must match by reference return of overridden member 'Base.this[int, int]'
                //     public override ref int this[int x, int y] { get { return ref field; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "this").WithArguments("Derived.this[int, int]", "Base.this[int, int]").WithLocation(15, 29),
                // (16,25): error CS8148: 'Derived.this[int, string]' must match by reference return of overridden member 'Base.this[int, string]'
                //     public override int this[int x, string y] { get { return field; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "this").WithArguments("Derived.this[int, string]", "Base.this[int, string]").WithLocation(16, 25));
        }

        /// <summary>
        /// Based on Method1 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters1()
        {
            var text = @"
abstract class Base
{
    public virtual string this[int i] { set { } }
}
abstract class Derived : Base
{
    public override string this[int i, long j = 1] { set { } }
    public override string this[int i, params int[] j] { set { } }
    public override string this[double i] { set { } }
    public override string this[ref int i] { set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (11,33): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref"),
                // (8,28): error CS0115: 'Derived.this[int, long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int, long]"),
                // (9,28): error CS0115: 'Derived.this[int, params int[]]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int, params int[]]"),
                // (10,28): error CS0115: 'Derived.this[double]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[double]"),
                // (11,28): error CS0115: 'Derived.this[ref int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[ref int]"));
        }

        /// <summary>
        /// Based on Method2 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters2()
        {
            var text = @"
abstract class Base
{
    public virtual string this[long j] { set { } }
    public virtual string this[short j] { set { } }
}
abstract class Derived : Base
{
    public override string this[int j] { set { } }
    public override string this[out long j] { set { j = 2; } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (10,33): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (9,28): error CS0115: 'Derived.this[int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int]"),
                // (10,28): error CS0115: 'Derived.this[out long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[out long]"));
        }

        /// <summary>
        /// Based on Method3 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters3()
        {
            var text = @"
abstract class Base
{
    public virtual string this[System.Exception x, System.ArgumentException y] { set { } }
    public virtual string this[System.ArgumentException x, System.Exception y] { set { } }
}
abstract class Derived : Base
{
    public override string this[System.Exception x, System.ArgumentException y, System.Exception z] { set { } }
    public override string this[System.ArgumentException x, System.ArgumentException y] { set { } }
    public override string this[] { set { } }
    public override string this[System.Exception x] { set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (11,33): error CS1551: Indexers must have at least one parameter
                Diagnostic(ErrorCode.ERR_IndexerNeedsParam, "]"),
                // (9,28): error CS0115: 'Derived.this[System.Exception, System.ArgumentException, System.Exception]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[System.Exception, System.ArgumentException, System.Exception]"),
                // (10,28): error CS0115: 'Derived.this[System.ArgumentException, System.ArgumentException]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[System.ArgumentException, System.ArgumentException]"),
                // (11,28): error CS0115: 'Derived.this': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this"),
                // (12,28): error CS0115: 'Derived.this[System.Exception]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[System.Exception]"));
        }

        /// <summary>
        /// Based on Method4 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters4()
        {
            var text = @"
abstract class Base
{
    public abstract string this[string[] x, string[][] y] { set; }
}
abstract class Derived : Base
{
    public override string this[string[] x, string[] y] { set { } }
    public override string this[string[][] y, string[] x] { set { } }
    public override string this[string[] x] { set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,28): error CS0115: 'Derived.this[string[], string[]]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[string[], string[]]"),
                // (9,28): error CS0115: 'Derived.this[string[][], string[]]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[string[][], string[]]"),
                // (10,28): error CS0115: 'Derived.this[string[]]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[string[]]"));
        }

        /// <summary>
        /// Based on Method5 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters5()
        {
            var text = @"
abstract class Base
{
    public abstract string this[System.Collections.Generic.List<string> x, System.Collections.Generic.Dictionary<int, long> y] { set; }
}
abstract class Derived : Base
{
    public override string this[System.Collections.Generic.List<int> x, System.Collections.Generic.Dictionary<string, long> y] { set { } }
    public override string this[System.Collections.Generic.Dictionary<int, long> x, System.Collections.Generic.List<string> y] { set { } }
    public override string this[System.Collections.Generic.List<string> y, System.Collections.Generic.Dictionary<int, long> x] { set { } } // Not an error
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,28): error CS0115: 'Derived.this[System.Collections.Generic.List<int>, System.Collections.Generic.Dictionary<string, long>]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[System.Collections.Generic.List<int>, System.Collections.Generic.Dictionary<string, long>]"),
                // (9,28): error CS0115: 'Derived.this[System.Collections.Generic.Dictionary<int, long>, System.Collections.Generic.List<string>]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[System.Collections.Generic.Dictionary<int, long>, System.Collections.Generic.List<string>]"));
        }

        /// <summary>
        /// Based on Method6 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters6()
        {
            var text = @"
abstract class Base
{
    public virtual string this[int i, params long[] j] { set { } }
}
abstract class Derived : Base
{
    public override string this[int i, long j = 1] { set { } }
    public override string this[int i] { set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,28): error CS0115: 'Derived.this[int, long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int, long]"),
                // (9,28): error CS0115: 'Derived.this[int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int]"));
        }

        /// <summary>
        /// Based on Method7 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters7()
        {
            var text = @"
abstract class Base
{
    public virtual string this[int i, short j = 1] { set { } }
}
abstract class Derived : Base
{
    public override string this[int i, params short[] j] { set { } }
    public override string this[int i] { set { } }
    public override string this[int i, short j = 1, short k = 1] { set { } }
    public override string this[int i, short j = short.MaxValue] { set { } } // Not an error
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,28): error CS0115: 'Derived.this[int, params short[]]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int, params short[]]"),
                // (9,28): error CS0115: 'Derived.this[int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int]"),
                // (10,28): error CS0115: 'Derived.this[int, short, short]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int, short, short]"));
        }

        /// <summary>
        /// Based on Method8 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters8()
        {
            var text = @"
abstract class Base
{
    public virtual string this[ref long j] { set { } }
}
abstract class Derived : Base
{
    public override string this[out long j] { set { j = 1; } }
    public override string this[long j] { set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (4,32): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref"),
                // (8,33): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (8,28): error CS0115: 'Derived.this[out long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[out long]"),
                // (9,28): error CS0115: 'Derived.this[long]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[long]"));
        }

        /// <summary>
        /// Based on Method9 in TestChangeMethodParameters.
        /// </summary>
        [Fact]
        public void TestChangeIndexerParameters9()
        {
            var text = @"
abstract class Base
{
    public abstract string this[out int j] { set; }
}
abstract class Derived : Base
{
    public override string this[ref int j] { set { } }
    public override string this[int j] { set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (4,33): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (8,33): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref"),
                // (8,28): error CS0115: 'Derived.this[ref int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[ref int]"),
                // (9,28): error CS0115: 'Derived.this[int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[int]"));
        }

        [Fact]
        public void TestChangeEventType()
        {
            var text = @"
using @str = System.String;

class Base
{
    public virtual event System.Action<string> Event1 { add { } remove { } }
    public virtual event System.Action<string> Event2 { add { } remove { } }
    public virtual event System.Action<string> Event3 { add { } remove { } }
    public virtual event System.Action<string> Event4 { add { } remove { } }
}

class Derived : Base
{
    public override event System.Action<System.String> Event1 { add { } remove { } }
    public override event System.Action<str> Event2 { add { } remove { } }
    public override event System.Action<object> Event3 { add { } remove { } }
    public override event System.Action<int> Event4 { add { } remove { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 16, Column = 49 }, //3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 17, Column = 46 }, //4
            });
        }

        [Fact]
        public void TestChangeGenericMethodReturnType()
        {
            var text = @"
class Base<T>
{
    public virtual T Method1(T t) { return t; }
    public virtual U Method2<U>(U u) { return u; }
}

class Derived : Base<string>
{
    public override object Method1(string t) { return null; }
    public override object Method2<U>(U u) { return null; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeReturnTypeOnOverride, Line = 10, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeReturnTypeOnOverride, Line = 11, Column = 28 },
            });
        }

        [Fact]
        public void TestChangeGenericMethodParameters()
        {
            // Tests:
            // Change number / order / types of method parameters in overridden method

            var text = @"
using System.Collections.Generic;
abstract class Base<T, U>
{
    protected virtual void Method(T x) { }
    protected virtual void Method(List<T> x) { }
    protected virtual void Method<V>(V x, T y) { }
    protected virtual void Method<V>(List<V> x, List<U> y) { }
}

class Derived<A, B> : Base<A, B>
{
    protected override void Method(B x) { }
    protected override void Method(List<B> x) { }
    protected override void Method<V>(A x, V y) { }
    protected override void Method<V>(List<V> x, List<A> y) { }
    protected override void Method<V, U>(List<V> x, List<U> y) { }
}

class Derived : Base<long, int>
{
    protected override void Method(int x) { }
    protected override void Method(List<int> x) { }
    protected override void Method<V>(int x, long y) { }
    protected override void Method<V>(List<V> x, List<long> y) { }
    protected override void Method<V>(List<long> x, List<int> y) { }
}

class Derived<A, B, C> : Base<A, B>
{
    protected override void Method() { }
    protected override void Method(List<A> x, List<B> y) { }
    protected override void Method<V>(V x, A y, C z) { }
    protected override void Method<V>(List<V> x, ref List<B> y) { }
    protected override void Method<V>(List<V> x, List<B> y = null) { } // Not an error
    protected override void Method<V>(List<V> x, List<B>[] y) { }
}";

            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B>.Method(B)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B>.Method(System.Collections.Generic.List<B>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B>.Method<V>(A, V)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B>.Method<V>(System.Collections.Generic.List<V>, System.Collections.Generic.List<A>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B>.Method<V, U>(System.Collections.Generic.List<V>, System.Collections.Generic.List<U>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method(int)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method(System.Collections.Generic.List<int>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V>(int, long)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V>(System.Collections.Generic.List<V>, System.Collections.Generic.List<long>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V>(System.Collections.Generic.List<long>, System.Collections.Generic.List<int>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B, C>.Method()"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B, C>.Method(System.Collections.Generic.List<A>, System.Collections.Generic.List<B>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B, C>.Method<V>(V, A, C)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B, C>.Method<V>(System.Collections.Generic.List<V>, ref System.Collections.Generic.List<B>)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived<A, B, C>.Method<V>(System.Collections.Generic.List<V>, System.Collections.Generic.List<B>[])"));
        }

        [Fact]
        public void TestChangeGenericMethodTypeParameters()
        {
            // Tests:
            // Change number / order of generic method type parameters in overridden method

            var text = @"
using System.Collections.Generic;
class Base<T, U>
{
    protected virtual void Method<V>() { }
    protected virtual void Method<V>(V x, T y) { }
    protected virtual void Method<V, W>(T x, U y) { }
    protected virtual void Method<V, W>(List<V> x, List<W> y) { }
}
class Derived : Base<int, int>
{
    protected override void Method() { }
    protected override void Method<V, U>() { }
    protected override void Method<V>(int x, V y) { }
    protected override void Method<V, U>(V x, int y) { }
    protected override void Method(int x, int y) { }
    protected override void Method<V>(int x, int y) { }
    protected override void Method<V, U, W>(int x, int y) { }
    protected override void Method<V, W>(List<W> x, List<V> y) { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method()"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V, U>()"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V>(int, V)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V, U>(V, int)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method(int, int)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V>(int, int)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V, U, W>(int, int)"),
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Derived.Method<V, W>(System.Collections.Generic.List<W>, System.Collections.Generic.List<V>)"));
        }

        [Fact]
        public void TestChangeGenericPropertyType()
        {
            var text = @"
class Base<T>
{
    public virtual T Property1 { get; set; }
    public virtual T Property2 { get; set; }
}

class Derived : Base<string>
{
    public override string Property1 { get; set; }
    public override object Property2 { get; set; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 11, Column = 28 }, //2
            });
        }

        [Fact]
        public void TestChangeGenericIndexerType()
        {
            var text = @"
class Base<T>
{
    public virtual T this[int x] { get { return default(T); } set { } }
    public virtual T this[string x] { get { return default(T); } set { } }
}

class Derived : Base<string>
{
    public override string this[int x] { get { return null; } set { } }
    public override object this[string x] { get { return null; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,28): error CS1715: 'Derived.this[string]': type must be 'string' to match overridden member 'Base<string>.this[string]'
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived.this[string]", "Base<string>.this[string]", "string"));
        }

        [Fact]
        public void TestChangeGenericIndexerParameters()
        {
            var text = @"
class Base<T>
{
    public virtual int this[string x, T y] { get { return 0; } set { } }
    public virtual int this[object x, T y] { get { return 0; } set { } }
}

class Derived : Base<string>
{
    public override int this[string x, string y] { get { return 0; } set { } }
    public override int this[object x, object y] { get { return 0; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,25): error CS0115: 'Derived.this[object, object]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Derived.this[object, object]"));
        }

        [Fact]
        public void TestChangeGenericEventType()
        {
            var text = @"
class Base<T>
{
    public virtual event System.Action<T> Event1 { add { } remove { } }
    public virtual event System.Action<T> Event2 { add { } remove { } }
}

class Derived : Base<string>
{
    public override event System.Action<string> Event1 { add { } remove { } }
    public override event System.Action<object> Event2 { add { } remove { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeTypeOnOverride, Line = 11, Column = 49 }, //2
            });
        }

        [Fact]
        public void TestDoNotChangeGenericMethodReturnType()
        {
            var text = @"
interface IGoo<A>
{
}

class Base
{
    public virtual T Method1<T>(T t) { return t; }
    public virtual IGoo<U> Method2<U>(U u) { return null; }
}

class Derived : Base
{
    public override M Method1<M>(M t) { return t; }
    public override IGoo<X> Method2<X>(X u) { return null; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
            });
        }

        [Fact]
        public void TestClassTypeParameterReturnType()
        {
            var text = @"
class Base<T>
{
    public virtual T Property { get; set; }
    public virtual T Method(T t) { return t; }
}

class Derived<S> : Base<S>
{
    public override S Property { get; set; }
    public override S Method(S s) { return s; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
            });
        }

        [Fact]
        public void TestNoImplementationOfAbstractMethod()
        {
            var text = @"
abstract class Base
{
    public abstract object Method1();
    public abstract object Method2();
    public abstract object Method3();
    public abstract object Method4(int i);
    public abstract ref object Method5(ref object o);
    public abstract object Method6(ref object o);
}

class Derived : Base
{
    //missed Method1 entirely
    public object Method2() { return null; } //missed override keyword
    public int Method3() { return 0; } //wrong return type
    public object Method4(long l) { return 0; } //wrong signature
    public override object Method5(ref object o) { return null; } //wrong by-value return
    public override ref object Method6(ref object o) { return ref o; } //wrong by-ref return
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (16,16): warning CS0114: 'Derived.Method3()' hides inherited member 'Base.Method3()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public int Method3() { return 0; } //wrong return type
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method3").WithArguments("Derived.Method3()", "Base.Method3()").WithLocation(16, 16),
                // (18,28): error CS8148: 'Derived.Method5(ref object)' must match by reference return of overridden member 'Base.Method5(ref object)'
                //     public override object Method5(ref object o) { return null; } //wrong by-value return
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Method5").WithArguments("Derived.Method5(ref object)", "Base.Method5(ref object)").WithLocation(18, 28),
                // (19,32): error CS8148: 'Derived.Method6(ref object)' must match by reference return of overridden member 'Base.Method6(ref object)'
                //     public override ref object Method6(ref object o) { return ref o; } //wrong by-ref return
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Method6").WithArguments("Derived.Method6(ref object)", "Base.Method6(ref object)").WithLocation(19, 32),
                // (15,19): warning CS0114: 'Derived.Method2()' hides inherited member 'Base.Method2()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public object Method2() { return null; } //missed override keyword
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method2").WithArguments("Derived.Method2()", "Base.Method2()").WithLocation(15, 19),
                // (12,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Method2()'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Method2()").WithLocation(12, 7),
                // (12,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Method4(int)'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Method4(int)").WithLocation(12, 7),
                // (12,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Method3()'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Method3()").WithLocation(12, 7),
                // (12,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Method1()'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Method1()").WithLocation(12, 7));
        }

        [Fact]
        public void TestNoImplementationOfAbstractProperty()
        {
            var text = @"
abstract class Base
{
    public abstract object Property1 { get; set; }
    public abstract object Property2 { get; set; }
    public abstract object Property3 { get; set; }

    public abstract object Property4 { get; set; }
    public abstract object Property5 { get; set; }
    public abstract object Property6 { get; }
    public abstract object Property7 { get; }
    public abstract object Property8 { set; }
    public abstract object Property9 { set; }

    public abstract object Property10 { get; }
    public abstract ref object Property11 { get; }
}

class Derived : Base
{
    //missed Property1 entirely
    public object Property2 { get; set; } //missed override keyword
    public override int Property3 { get; set; } //wrong type

    //wrong accessors
    public override object Property4 { get { return null; } }
    public override object Property5 { set { } }
    public override object Property6 { get; set; }
    public override object Property7 { set { } }
    public override object Property8 { get; set; }
    public override object Property9 { get { return null; } }

    //wrong by-{value,ref} return
    object o = null;
    public override ref object Property10 { get { return ref o; } }
    public override object Property11 { get { return null; } }
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (23,25): error CS1715: 'Derived.Property3': type must be 'object' to match overridden member 'Base.Property3'
                //     public override int Property3 { get; set; } //wrong type
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "Property3").WithArguments("Derived.Property3", "Base.Property3", "object").WithLocation(23, 25),
                // (28,45): error CS0546: 'Derived.Property6.set': cannot override because 'Base.Property6' does not have an overridable set accessor
                //     public override object Property6 { get; set; }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived.Property6.set", "Base.Property6").WithLocation(28, 45),
                // (29,40): error CS0546: 'Derived.Property7.set': cannot override because 'Base.Property7' does not have an overridable set accessor
                //     public override object Property7 { set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived.Property7.set", "Base.Property7").WithLocation(29, 40),
                // (30,40): error CS0545: 'Derived.Property8.get': cannot override because 'Base.Property8' does not have an overridable get accessor
                //     public override object Property8 { get; set; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived.Property8.get", "Base.Property8").WithLocation(30, 40),
                // (31,40): error CS0545: 'Derived.Property9.get': cannot override because 'Base.Property9' does not have an overridable get accessor
                //     public override object Property9 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived.Property9.get", "Base.Property9").WithLocation(31, 40),
                // (35,32): error CS8148: 'Derived.Property10' must match by reference return of overridden member 'Base.Property10'
                //     public override ref object Property10 { get { return ref o; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Property10").WithArguments("Derived.Property10", "Base.Property10").WithLocation(35, 32),
                // (36,28): error CS8148: 'Derived.Property11' must match by reference return of overridden member 'Base.Property11'
                //     public override object Property11 { get { return null; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Property11").WithArguments("Derived.Property11", "Base.Property11").WithLocation(36, 28),
                // (22,19): warning CS0114: 'Derived.Property2' hides inherited member 'Base.Property2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public object Property2 { get; set; } //missed override keyword
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Property2").WithArguments("Derived.Property2", "Base.Property2").WithLocation(22, 19),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property2.get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property2.get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property1.get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property1.get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property5.get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property5.get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property9.set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property9.set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property1.set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property1.set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property7.get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property7.get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property3.set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property3.set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property2.set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property2.set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Property4.set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Property4.set").WithLocation(19, 7));
        }

        [Fact]
        public void TestNoImplementationOfAbstractIndexer()
        {
            var text = @"
abstract class Base
{
    public abstract object this[int w, int x, int y , int z] { get; set; }
    public abstract object this[int w, int x, int y , string z] { get; set; }
    public abstract object this[int w, int x, string y , int z] { get; set; }

    public abstract object this[int w, int x, string y , string z] { get; set; }
    public abstract object this[int w, string x, int y , int z] { get; set; }
    public abstract object this[int w, string x, int y , string z] { get; }
    public abstract object this[int w, string x, string y , int z] { get; }
    public abstract object this[int w, string x, string y , string z] { set; }
    public abstract object this[string w, int x, int y , int z] { set; }

    public abstract object this[string w, int x, int y, string z] { get; }
    public abstract ref object this[string w, int x, string y, int z] { get; }
}

class Derived : Base
{
    //missed first indexer entirely
    public object this[int w, int x, int y , string z] { get { return 0; } set { } } //missed override keyword
    public override int this[int w, int x, string y , int z] { get { return 0; } set { } } //wrong type

    //wrong accessors
    public override object this[int w, int x, string y , string z] { get { return null; } }
    public override object this[int w, string x, int y , int z] { set { } }
    public override object this[int w, string x, int y , string z] { get { return 0; } set { } }
    public override object this[int w, string x, string y , int z] { set { } }
    public override object this[int w, string x, string y , string z] { get { return 0; } set { } }
    public override object this[string w, int x, int y , int z] { get { return null; } }

    //wrong by-{value,ref} return
    object o = null;
    public override ref object this[string w, int x, int y, string z] { get { return ref o; } }
    public override object this[string w, int x, string y, int z] { get; }
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (36,69): error CS0501: 'Derived.this[string, int, string, int].get' must declare a body because it is not marked abstract, extern, or partial
                //     public override object this[string w, int x, string y, int z] { get; }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("Derived.this[string, int, string, int].get").WithLocation(36, 69),
                // (23,25): error CS1715: 'Derived.this[int, int, string, int]': type must be 'object' to match overridden member 'Base.this[int, int, string, int]'
                //     public override int this[int w, int x, string y , int z] { get { return 0; } set { } } //wrong type
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived.this[int, int, string, int]", "Base.this[int, int, string, int]", "object").WithLocation(23, 25),
                // (28,88): error CS0546: 'Derived.this[int, string, int, string].set': cannot override because 'Base.this[int, string, int, string]' does not have an overridable set accessor
                //     public override object this[int w, string x, int y , string z] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived.this[int, string, int, string].set", "Base.this[int, string, int, string]").WithLocation(28, 88),
                // (29,70): error CS0546: 'Derived.this[int, string, string, int].set': cannot override because 'Base.this[int, string, string, int]' does not have an overridable set accessor
                //     public override object this[int w, string x, string y , int z] { set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("Derived.this[int, string, string, int].set", "Base.this[int, string, string, int]").WithLocation(29, 70),
                // (30,73): error CS0545: 'Derived.this[int, string, string, string].get': cannot override because 'Base.this[int, string, string, string]' does not have an overridable get accessor
                //     public override object this[int w, string x, string y , string z] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived.this[int, string, string, string].get", "Base.this[int, string, string, string]").WithLocation(30, 73),
                // (31,67): error CS0545: 'Derived.this[string, int, int, int].get': cannot override because 'Base.this[string, int, int, int]' does not have an overridable get accessor
                //     public override object this[string w, int x, int y , int z] { get { return null; } }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived.this[string, int, int, int].get", "Base.this[string, int, int, int]").WithLocation(31, 67),
                // (35,32): error CS8148: 'Derived.this[string, int, int, string]' must match by reference return of overridden member 'Base.this[string, int, int, string]'
                //     public override ref object this[string w, int x, int y, string z] { get { return ref o; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "this").WithArguments("Derived.this[string, int, int, string]", "Base.this[string, int, int, string]").WithLocation(35, 32),
                // (36,28): error CS8148: 'Derived.this[string, int, string, int]' must match by reference return of overridden member 'Base.this[string, int, string, int]'
                //     public override object this[string w, int x, string y, int z] { get; }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "this").WithArguments("Derived.this[string, int, string, int]", "Base.this[string, int, string, int]").WithLocation(36, 28),
                // (22,19): warning CS0114: 'Derived.this[int, int, int, string]' hides inherited member 'Base.this[int, int, int, string]'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public object this[int w, int x, int y , string z] { get { return 0; } set { } } //missed override keyword
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "this").WithArguments("Derived.this[int, int, int, string]", "Base.this[int, int, int, string]").WithLocation(22, 19),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, int, string, int].set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, int, string, int].set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, int, string, string].set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, int, string, string].set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, int, int, int].set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, int, int, int].set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, int, int, int].get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, int, int, int].get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, string, int, int].get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, string, int, int].get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, int, int, string].set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, int, int, string].set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, int, int, string].get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, int, int, string].get").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[string, int, int, int].set'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[string, int, int, int].set").WithLocation(19, 7),
                // (19,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.this[int, string, string, int].get'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.this[int, string, string, int].get").WithLocation(19, 7));
        }

        [Fact]
        public void TestNoImplementationOfAbstractEvent()
        {
            var text = @"
abstract class Base
{
    public abstract event System.Action Event1;
    public abstract event System.Action Event2;
    public abstract event System.Action Event3;
}

class Derived : Base
{
    //missed Method1 Event1
    public event System.Action Event2 { add { } remove { } } //missed override keyword
    public override event System.Action<int> Event3 { add { } remove { } } //wrong type
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (12,32): warning CS0114: 'Derived.Event2' hides inherited member 'Base.Event2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public event System.Action Event2 { add { } remove { } } //missed override keyword
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event2").WithArguments("Derived.Event2", "Base.Event2"),
                // (13,46): error CS1715: 'Derived.Event3': type must be 'System.Action' to match overridden member 'Base.Event3'
                //     public override event System.Action<int> Event3 { add { } remove { } } //wrong type
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "Event3").WithArguments("Derived.Event3", "Base.Event3", "System.Action"),
                // (9,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event2.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event2.add"),
                // (9,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event2.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event2.remove"),
                // (9,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event1.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event1.add"),
                // (9,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event1.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event1.remove"),
                // (9,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event3.add'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event3.add"),
                // (9,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base.Event3.remove'
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.Event3.remove"));
        }

        [Fact]
        public void TestNoImplementationOfAbstractMethodFromGrandparent()
        {
            var text = @"
abstract class Abstract1
{
    public abstract void Method1();
    public abstract void Method2();
    public abstract void Method3();
}

abstract class Abstract2 : Abstract1
{
    public override void Method1() { }

    public abstract void Method4();
    public abstract void Method5();
}

class Concrete : Abstract2
{
    public override void Method3() { }
    public override void Method5() { }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //Method2
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //Method4
            });
        }

        [Fact]
        public void TestNoImplementationOfAbstractPropertyFromGrandparent()
        {
            var text = @"
abstract class Abstract1
{
    public abstract long Property1 { get; set; }
    public abstract long Property2 { get; set; }
    public abstract long Property3 { get; set; }
}

abstract class Abstract2 : Abstract1
{
    public override long Property1 { get; set; }

    public abstract long Property4 { get; set; }
    public abstract long Property5 { get; set; }
}

class Concrete : Abstract2
{
    public override long Property3 { get; set; }
    public override long Property5 { get; set; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //2.get
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //2.set
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //4.get
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //4.set
            });
        }

        [Fact]
        public void TestNoImplementationOfAbstractIndexerFromGrandparent()
        {
            var text = @"
abstract class Abstract1
{
    public abstract long this[int x, int y, string z] { get; set; }
    public abstract long this[int x, string y, int z] { get; set; }
    public abstract long this[int x, string y, string z] { get; set; }
}

abstract class Abstract2 : Abstract1
{
    public override long this[int x, int y, string z] { get { return 0; } set { } }

    public abstract long this[string x, int y, int z] { get; set; }
    public abstract long this[string x, int y, string z] { get; set; }
}

class Concrete : Abstract2
{
    public override long this[int x, string y, string z] { get { return 0; } set { } }
    public override long this[string x, int y, string z] { get { return 0; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (17,7): error CS0534: 'Concrete' does not implement inherited abstract member 'Abstract1.this[int, string, int].get'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Concrete").WithArguments("Concrete", "Abstract1.this[int, string, int].get"),
                // (17,7): error CS0534: 'Concrete' does not implement inherited abstract member 'Abstract1.this[int, string, int].set'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Concrete").WithArguments("Concrete", "Abstract1.this[int, string, int].set"),
                // (17,7): error CS0534: 'Concrete' does not implement inherited abstract member 'Abstract2.this[string, int, int].get'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Concrete").WithArguments("Concrete", "Abstract2.this[string, int, int].get"),
                // (17,7): error CS0534: 'Concrete' does not implement inherited abstract member 'Abstract2.this[string, int, int].set'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Concrete").WithArguments("Concrete", "Abstract2.this[string, int, int].set"));
        }

        [Fact]
        public void TestNoImplementationOfAbstractEventFromGrandparent()
        {
            var text = @"
abstract class Abstract1
{
    public abstract event System.Action Event1;
    public abstract event System.Action Event2;
    public abstract event System.Action Event3;
}

abstract class Abstract2 : Abstract1
{
    public override event System.Action Event1 { add { } remove { } }

    public abstract event System.Action Event4;
    public abstract event System.Action Event5;
}

class Concrete : Abstract2
{
    public override event System.Action Event3 { add { } remove { } }
    public override event System.Action Event5 { add { } remove { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //2.add
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //2.remove
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //4.add
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 17, Column = 7 }, //4.remove
            });
        }

        [Fact]
        public void TestNoImplementationOfInterfaceMethod_01()
        {
            var text = @"
interface Interface
{
    object Method1();
    object Method2(int i);
    ref object Method3(ref object o);
    object Method4(ref object o);
}

class Class : Interface
{
    //missed Method1 entirely
    public object Method2(long l) { return 0; } //wrong signature
    public object Method3(ref object o) { return null; } //wrong by-value return
    public ref object Method4(ref object o) { return ref o; } //wrong by-ref return
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (10,15): error CS8152: 'Class' does not implement interface member 'Interface.Method4(ref object)'. 'Class.Method4(ref object)' cannot implement 'Interface.Method4(ref object)' because it does not have matching return by reference.
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "Interface").WithArguments("Class", "Interface.Method4(ref object)", "Class.Method4(ref object)").WithLocation(10, 15),
                // (10,15): error CS0535: 'Class' does not implement interface member 'Interface.Method2(int)'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Method2(int)").WithLocation(10, 15),
                // (10,15): error CS8152: 'Class' does not implement interface member 'Interface.Method3(ref object)'. 'Class.Method3(ref object)' cannot implement 'Interface.Method3(ref object)' because it does not have matching return by reference.
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "Interface").WithArguments("Class", "Interface.Method3(ref object)", "Class.Method3(ref object)").WithLocation(10, 15),
                // (10,15): error CS0535: 'Class' does not implement interface member 'Interface.Method1()'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Method1()").WithLocation(10, 15));
        }

        [Fact]
        public void TestNoImplementationOfInterfaceMethod_02()
        {
            var source1 =
@"
public class C1 {}
";

            var comp11 = CreateCompilation(new AssemblyIdentity("lib1", new Version("4.2.1.0"), publicKeyOrToken: SigningTestHelpers.PublicKey, hasPublicKey: true),
                                           new[] { source1 }, TargetFrameworkUtil.GetReferences(TargetFramework.Standard, null).ToArray(), TestOptions.DebugDll.WithPublicSign(true));
            comp11.VerifyDiagnostics();
            var comp12 = CreateCompilation(new AssemblyIdentity("lib1", new Version("4.1.0.0"), publicKeyOrToken: SigningTestHelpers.PublicKey, hasPublicKey: true),
                                           new[] { source1 }, TargetFrameworkUtil.GetReferences(TargetFramework.Standard, null).ToArray(), TestOptions.DebugDll.WithPublicSign(true));
            comp12.VerifyDiagnostics();

            var source2 =
@"
public class C2 : C1 {}
";

            var comp2 = CreateCompilation(new[] { source2 }, references: new[] { comp12.EmitToImageReference() }, assemblyName: "lib2");
            comp2.VerifyDiagnostics();

            var source3 =
@"
interface I1
{
    void Method1();
}

#pragma warning disable CS1701
class C3 : C2,
#pragma warning restore CS1701
               I1
{
}
";
            var comp3 = CreateCompilation(new[] { source3 }, references: new[] { comp11.EmitToImageReference(), comp2.EmitToImageReference() }, assemblyName: "lib3");

            // The unification warning shouldn't suppress the CS0535 error.
            comp3.VerifyDiagnostics(
                // warning CS1701: Assuming assembly reference 'lib1, Version=4.1.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' used by 'lib2' matches identity 'lib1, Version=4.2.1.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' of 'lib1', you may need to supply runtime policy
                Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("lib1, Version=4.1.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "lib2", "lib1, Version=4.2.1.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "lib1").WithLocation(1, 1),
                // warning CS1701: Assuming assembly reference 'lib1, Version=4.1.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' used by 'lib2' matches identity 'lib1, Version=4.2.1.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2' of 'lib1', you may need to supply runtime policy
                Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("lib1, Version=4.1.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "lib2", "lib1, Version=4.2.1.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "lib1").WithLocation(1, 1),
                // (10,16): error CS0535: 'C3' does not implement interface member 'I1.Method1()'
                //                I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("C3", "I1.Method1()").WithLocation(10, 16)
                );
        }

        [Fact]
        public void TestNoImplementationOfInterfaceProperty()
        {
            var text = @"
interface Interface
{
    object Property1 { get; set; }

    object Property2 { get; set; }
    object Property3 { get; set; }
    object Property4 { get; }
    object Property5 { get; }
    object Property6 { set; }
    object Property7 { set; }

    ref object Property8 { get; }
    object Property9 { get; }
}

class Class : Interface
{
    //missed Property1 entirely

    //wrong accessors
    public object Property2 { get { return null; } }
    public object Property3 { set { } }
    public object Property4 { get; set; }
    public object Property5 { set { } }
    public object Property6 { get; set; }
    public object Property7 { get { return null; } }

    //wrong by-{value,ref} return
    object o = null;
    public object Property8 { get { return null; } }
    public ref object Property9 { get { return ref o; } }
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.Property2.set'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Property2.set").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.Property3.get'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Property3.get").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.Property5.get'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Property5.get").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.Property7.set'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Property7.set").WithLocation(17, 15),
                // (17,15): error CS8152: 'Class' does not implement interface member 'Interface.Property8'. 'Class.Property8' cannot implement 'Interface.Property8' because it does not have matching return by reference.
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "Interface").WithArguments("Class", "Interface.Property8", "Class.Property8").WithLocation(17, 15),
                // (17,15): error CS8152: 'Class' does not implement interface member 'Interface.Property9'. 'Class.Property9' cannot implement 'Interface.Property9' because it does not have matching return by reference.
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "Interface").WithArguments("Class", "Interface.Property9", "Class.Property9").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.Property1'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.Property1").WithLocation(17, 15));
        }

        [Fact]
        public void TestNoImplementationOfInterfaceIndexer()
        {
            var text = @"
interface Interface
{
    object this[int w, int x, int y, string z] { get; set; }

    object this[int w, int x, string y, int z] { get; set; }
    object this[int w, int x, string y, string z] { get; set; }
    object this[int w, string x, int y, int z] { get; }
    object this[int w, string x, int y, string z] { get; }
    object this[int w, string x, string y, int z] { set; }
    object this[int w, string x, string y, string z] { set; }

    ref object this[string w, int x, int y, int z] { get; }
    object this[string w, int x, int y, string z] { get; }
}

class Class : Interface
{
    //missed first indexer entirely

    //wrong accessors
    public object this[int w, int x, string y, int z] { get { return null; } }
    public object this[int w, int x, string y, string z] { set { } }
    public object this[int w, string x, int y, int z] { get { return 0; } set { } }
    public object this[int w, string x, int y, string z] { set { } }
    public object this[int w, string x, string y, int z] { get { return 0; } set { } }
    public object this[int w, string x, string y, string z] { get { return null; } }

    // wrong by-{value,ref} return
    object o = null;
    public object this[string w, int x, int y, int z] { get { return null; } }
    public ref object this[string w, int x, int y, string z] { get { return ref o; } }
}
";
            CreateCompilationWithMscorlib461(text).VerifyDiagnostics(
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, string, string, string].set'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, string, string, string].set").WithLocation(17, 15),
                // (17,15): error CS8152: 'Class' does not implement interface member 'Interface.this[string, int, int, int]'. 'Class.this[string, int, int, int]' cannot implement 'Interface.this[string, int, int, int]' because it does not have matching return by reference.
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "Interface").WithArguments("Class", "Interface.this[string, int, int, int]", "Class.this[string, int, int, int]").WithLocation(17, 15),
                // (17,15): error CS8152: 'Class' does not implement interface member 'Interface.this[string, int, int, string]'. 'Class.this[string, int, int, string]' cannot implement 'Interface.this[string, int, int, string]' because it does not have matching return by reference.
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, "Interface").WithArguments("Class", "Interface.this[string, int, int, string]", "Class.this[string, int, int, string]").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, int, string, string].get'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, int, string, string].get").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, string, int, string].get'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, string, int, string].get").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, int, string, int].set'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, int, string, int].set").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, int, int, string]'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, int, int, string]").WithLocation(17, 15));
        }

        [Fact]
        public void TestNoImplementationOfInterfaceEvent()
        {
            var text = @"
interface Interface
{
    event System.Action Event1;
}

class Class : Interface
{
    //missed Event1 entirely
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 7, Column = 15 }, //1
            });
        }

        [Fact]
        public void TestNoImplementationOfInterfaceMethodInBase()
        {
            var text = @"
interface Interface
{
    object Method1();
    object Method2(int i);
}

class Base : Interface
{
    //missed Method1 entirely
    public object Method2(long l) { return 0; } //wrong signature
}

class Derived1 : Base //not declaring interface
{
}

class Derived2 : Base, Interface
{
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,14): error CS0535: 'Base' does not implement interface member 'Interface.Method2(int)'
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Base", "Interface.Method2(int)").WithLocation(8, 14),
                // (8,14): error CS0535: 'Base' does not implement interface member 'Interface.Method1()'
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Base", "Interface.Method1()").WithLocation(8, 14)
                );
        }

        [Fact]
        public void TestNoImplementationOfBaseInterfaceMethod()
        {
            var text = @"
interface Interface1
{
    object Method1();
}
interface Interface2
{
    object Method2();
}
class Base : Interface2
{
    public object Method1() { return null; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface2").WithArguments("Base", "Interface2.Method2()"));
        }

        [Fact]
        public void TestNoImplementationOfInterfacePropertyInBase()
        {
            var text = @"
interface Interface
{
    object Property1 { get; set; }
    object Property2 { get; set; }
}

class Base : Interface
{
    //missed Property1 entirely
    public long Property2 { get; set; } //wrong type
}

class Derived1 : Base //not declaring interface
{
}

class Derived2 : Base, Interface
{
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,14): error CS0738: 'Base' does not implement interface member 'Interface.Property2'. 'Base.Property2' cannot implement 'Interface.Property2' because it does not have the matching return type of 'object'.
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Base", "Interface.Property2", "Base.Property2", "object").WithLocation(8, 14),
                // (8,14): error CS0535: 'Base' does not implement interface member 'Interface.Property1'
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Base", "Interface.Property1").WithLocation(8, 14),
                // (18,24): error CS0738: 'Derived2' does not implement interface member 'Interface.Property2'. 'Base.Property2' cannot implement 'Interface.Property2' because it does not have the matching return type of 'object'.
                // class Derived2 : Base, Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Derived2", "Interface.Property2", "Base.Property2", "object").WithLocation(18, 24)
                );
        }

        [Fact]
        public void TestNoImplementationOfInterfaceIndexerInBase()
        {
            var text = @"
interface Interface
{
    object this[int x] { get; set; }
    object this[string x] { get; set; }
}

class Base : Interface
{
    //missed int indexer entirely
    public long this[string x] { get { return 0; } set { } } //wrong type
}

class Derived1 : Base //not declaring interface
{
}

class Derived2 : Base, Interface
{
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,14): error CS0738: 'Base' does not implement interface member 'Interface.this[string]'. 'Base.this[string]' cannot implement 'Interface.this[string]' because it does not have the matching return type of 'object'.
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Base", "Interface.this[string]", "Base.this[string]", "object").WithLocation(8, 14),
                // (8,14): error CS0535: 'Base' does not implement interface member 'Interface.this[int]'
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Base", "Interface.this[int]").WithLocation(8, 14),
                // (18,24): error CS0738: 'Derived2' does not implement interface member 'Interface.this[string]'. 'Base.this[string]' cannot implement 'Interface.this[string]' because it does not have the matching return type of 'object'.
                // class Derived2 : Base, Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Derived2", "Interface.this[string]", "Base.this[string]", "object").WithLocation(18, 24)
                );
        }

        [Fact]
        public void TestNoImplementationOfInterfaceEventInBase()
        {
            var text = @"
interface Interface
{
    event System.Action Event1;
    event System.Action Event2;
}

class Base : Interface
{
    //missed Event1 entirely
    public event System.Action<int> Event2 { add { } remove { } } //wrong type
}

class Derived1 : Base //not declaring interface
{
}

class Derived2 : Base, Interface
{
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,14): error CS0738: 'Base' does not implement interface member 'Interface.Event2'. 'Base.Event2' cannot implement 'Interface.Event2' because it does not have the matching return type of 'Action'.
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Base", "Interface.Event2", "Base.Event2", "System.Action").WithLocation(8, 14),
                // (8,14): error CS0535: 'Base' does not implement interface member 'Interface.Event1'
                // class Base : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Base", "Interface.Event1").WithLocation(8, 14),
                // (18,24): error CS0738: 'Derived2' does not implement interface member 'Interface.Event2'. 'Base.Event2' cannot implement 'Interface.Event2' because it does not have the matching return type of 'Action'.
                // class Derived2 : Base, Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Derived2", "Interface.Event2", "Base.Event2", "System.Action").WithLocation(18, 24)
                );
        }

        [Fact]
        public void TestExplicitMethodImplementation()
        {
            var text = @"

interface BaseInterface
{
    void Method4();
}
interface Interface : BaseInterface
{
    void Method1();
    void Method2();
}

interface Interface2
{
    void Method1();
}

class Base : Interface
{
    void System.Object.Method1() { } //not an interface
    void Base.Method1() { } //not an interface
    void System.Int32.Method1() { } //not an interface
    void Interface2.Method1() { } //does not implement Interface2
    void Interface.Method3() { } //not on Interface
    void Interface.Method4() { } //not on Interface

    public void Method1() { }
    public void Method2() { }
    public void Method4() { }
}

class Derived : Base
{
    void Interface.Method1() { } //does not directly list Interface
}

class Derived2 : Base, Interface
{
    void Interface.Method1() { } //fine
    void BaseInterface.Method4() { } //fine
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (20,10): error CS0538: 'object' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Object").WithArguments("object"),
                // (21,10): error CS0538: 'Base' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "Base").WithArguments("Base"),
                // (22,10): error CS0538: 'int' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Int32").WithArguments("int"),
                // (23,10): error CS0540: 'Base.Interface2.Method1()': containing type does not implement interface 'Interface2'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface2").WithArguments("Base.Interface2.Method1()", "Interface2"),
                // (24,20): error CS0539: 'Base.Method3()' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method3").WithArguments("Base.Method3()"),
                // (25,20): error CS0539: 'Base.Method4()' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method4").WithArguments("Base.Method4()"),
                // (34,10): error CS0540: 'Derived.Interface.Method1()': containing type does not implement interface 'Interface'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface").WithArguments("Derived.Interface.Method1()", "Interface"));
        }

        [Fact]
        public void TestExplicitPropertyImplementation()
        {
            var text = @"
interface BaseInterface
{
    int Property4 { get; }
}
interface Interface : BaseInterface
{
    int Property1 { set; }
    int Property2 { set; }
}

interface Interface2
{
    int Property1 { set; }
}

class Base : Interface
{
    int System.Object.Property1 { set { } } //not an interface
    int Base.Property1 { set { } } //not an interface
    int System.Int32.Property1 { set { } } //not an interface
    int Interface2.Property1 { set { } } //does not implement Interface2
    int Interface.Property3 { set { } } //not on Interface
    int Interface.Property4 { get { return 1; } } //not on Interface

    public int Property1 { set { } }
    public int Property2 { set { } }
    public int Property4 { get { return 0; } }
}

class Derived : Base
{
    int Interface.Property1 { set { } } //does not directly list Interface
}

class Derived2 : Base, Interface
{
    int Interface.Property1 { set { } } //fine
    int BaseInterface.Property4 { get { return 1; } } //fine
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (19,9): error CS0538: 'object' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Object").WithArguments("object"),
                // (20,9): error CS0538: 'Base' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "Base").WithArguments("Base"),
                // (21,9): error CS0538: 'int' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Int32").WithArguments("int"),
                // (22,9): error CS0540: 'Base.Interface2.Property1': containing type does not implement interface 'Interface2'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface2").WithArguments("Base.Interface2.Property1", "Interface2"),
                // (23,19): error CS0539: 'Base.Property3' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property3").WithArguments("Base.Property3"),
                // (24,19): error CS0539: 'Base.Property4' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property4").WithArguments("Base.Property4"),
                // (33,9): error CS0540: 'Derived.Interface.Property1': containing type does not implement interface 'Interface'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface").WithArguments("Derived.Interface.Property1", "Interface"));
        }

        [Fact]
        public void TestExplicitIndexerImplementation()
        {
            var text = @"
interface BaseInterface
{
    int this[string x, string y] { get; }
}
interface Interface : BaseInterface
{
    int this[int x, int y] { set; }
    int this[int x, string y] { set; }
}

interface Interface2
{
    int this[int x, int y] { set; }
}

class Base : Interface
{
    int System.Object.this[int x, int y] { set { } } //not an interface
    int Base.this[int x, int y] { set { } } //not an interface
    int System.Int32.this[int x, int y] { set { } } //not an interface
    int Interface2.this[int x, int y] { set { } } //does not implement Interface2
    int Interface.this[string x, int y] { set { } } //not on Interface
    int Interface.this[string x, string y] { get { return 1; } } //not on Interface

    public int this[int x, int y] { set { } }
    public int this[int x, string y] { set { } }
    public int this[string x, string y] { get { return 0; } }
}

class Derived : Base
{
    int Interface.this[int x, int y] { set { } } //does not directly list Interface
}

class Derived2 : Base, Interface
{
    int Interface.this[int x, int y] { set { } } //fine
    int BaseInterface.this[string x, string y] { get { return 1; } } //fine
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (19,9): error CS0538: 'object' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Object").WithArguments("object"),
                // (20,9): error CS0538: 'Base' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "Base").WithArguments("Base"),
                // (21,9): error CS0538: 'int' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Int32").WithArguments("int"),
                // (22,9): error CS0540: 'Base.Interface2.this[int, int]': containing type does not implement interface 'Interface2'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface2").WithArguments("Base.Interface2.this[int, int]", "Interface2"),
                // (23,19): error CS0539: 'Base.this[string, int]' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Base.this[string, int]"),
                // (24,19): error CS0539: 'Base.this[string, string]' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Base.this[string, string]"),
                // (33,9): error CS0540: 'Derived.Interface.this[int, int]': containing type does not implement interface 'Interface'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface").WithArguments("Derived.Interface.this[int, int]", "Interface"));
        }

        [Fact]
        public void TestExplicitEventImplementation()
        {
            var text = @"
interface BaseInterface
{
    event System.Action Event4;
}
interface Interface : BaseInterface
{
    event System.Action Event1;
    event System.Action Event2;
}

interface Interface2
{
    event System.Action Event1;
}

class Base : Interface
{
    event System.Action System.Object.Event1 { add { } remove { } } //not an interface
    event System.Action Base.Event1 { add { } remove { } } //not an interface
    event System.Action System.Int32.Event1 { add { } remove { } } //not an interface
    event System.Action Interface2.Event1 { add { } remove { } } //does not implement Interface2
    event System.Action Interface.Event3 { add { } remove { } } //not on Interface
    event System.Action Interface.Event4 { add { } remove { } } //not on Interface

    public event System.Action Event1 { add { } remove { } }
    public event System.Action Event2 { add { } remove { } }
    public event System.Action Event4 { add { } remove { } }
}

class Derived : Base
{
    event System.Action Interface.Event1 { add { } remove { } } //does not directly list Interface
}

class Derived2 : Base, Interface
{
    event System.Action Interface.Event1 { add { } remove { } } //fine
    event System.Action BaseInterface.Event4 { add { } remove { } } //fine
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (19,25): error CS0538: 'object' in explicit interface declaration is not an interface
                //     event System.Action System.Object.Event1 { add { } remove { } } //not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Object").WithArguments("object"),
                // (20,25): error CS0538: 'Base' in explicit interface declaration is not an interface
                //     event System.Action Base.Event1 { add { } remove { } } //not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "Base").WithArguments("Base"),
                // (21,25): error CS0538: 'int' in explicit interface declaration is not an interface
                //     event System.Action System.Int32.Event1 { add { } remove { } } //not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "System.Int32").WithArguments("int"),
                // (22,25): error CS0540: 'Base.Interface2.Event1': containing type does not implement interface 'Interface2'
                //     event System.Action Interface2.Event1 { add { } remove { } } //does not implement Interface2
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface2").WithArguments("Base.Interface2.Event1", "Interface2"),
                // (23,35): error CS0539: 'Base.Event3' in explicit interface declaration is not a member of interface
                //     event System.Action Interface.Event3 { add { } remove { } } //not on Interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Event3").WithArguments("Base.Event3"),
                // (24,35): error CS0539: 'Base.Event4' in explicit interface declaration is not a member of interface
                //     event System.Action Interface.Event4 { add { } remove { } } //not on Interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Event4").WithArguments("Base.Event4"),
                // (33,25): error CS0540: 'Derived.Interface.Event1': containing type does not implement interface 'Interface'
                //     event System.Action Interface.Event1 { add { } remove { } } //does not directly list Interface
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface").WithArguments("Derived.Interface.Event1", "Interface"));
        }

        [Fact]
        public void TestExplicitMethodImplementation2()
        {
            var text = @"
public interface I<T>
{
    void F();
}

public class C : I<object>
{
    void I<dynamic>.F() { } // Dev10 Error: we don't implement I<dynamic>
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,10): error CS0540: 'C.I<dynamic>.F()': containing type does not implement interface 'I<dynamic>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I<dynamic>").WithArguments("C.I<dynamic>.F()", "I<dynamic>")
                );
        }

        [Fact]
        public void TestExplicitPropertyImplementation2()
        {
            var text = @"
public interface I<T>
{
    int P { set; }
}

public class C : I<object>
{
    int I<dynamic>.P { set { } } // Dev10 Error: we don't implement I<dynamic>
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,9): error CS0540: 'C.I<dynamic>.P': containing type does not implement interface 'I<dynamic>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I<dynamic>").WithArguments("C.I<dynamic>.P", "I<dynamic>")
                );
        }

        [Fact]
        public void TestExplicitIndexerImplementation2()
        {
            var text = @"
public interface I<T>
{
    int this[int x] { set; }
}

public class C : I<object>
{
    int I<dynamic>.this[int x] { set { } } // Dev10 Error: we don't implement I<dynamic>
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,9): error CS0540: 'C.I<dynamic>.this[int]': containing type does not implement interface 'I<dynamic>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I<dynamic>").WithArguments("C.I<dynamic>.this[int]", "I<dynamic>"));
        }

        [Fact]
        public void TestExplicitEventImplementation2()
        {
            var text = @"
public interface I<T>
{
    event System.Action E;
}

public class C : I<object>
{
    event System.Action I<dynamic>.E { add { } remove { } } // Dev10 Error: we don't implement I<dynamic>
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,25): error CS0540: 'C.I<dynamic>.E': containing type does not implement interface 'I<dynamic>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I<dynamic>").WithArguments("C.I<dynamic>.E", "I<dynamic>")
                );
        }

        [Fact]
        public void TestInterfaceImplementationMistakes()
        {
            var text = @"
interface Interface
{
    void Method1();
    void Method2();
    void Method3();
    void Method4();
    void Method5();
    void Method6();
    void Method7();
}

partial class Base : Interface
{
    public static void Method1() { }
    public int Method2() { return 0; }
    private void Method3() { }
    internal void Method4() { }
    protected void Method5() { }
    protected internal void Method6() { }
    partial void Method7();
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,22): error CS0737: 'Base' does not implement interface member 'Interface.Method7()'. 'Base.Method7()' cannot implement an interface member because it is not public.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "Interface").WithArguments("Base", "Interface.Method7()", "Base.Method7()").WithLocation(13, 22),
                // (13,22): error CS0738: 'Base' does not implement interface member 'Interface.Method2()'. 'Base.Method2()' cannot implement 'Interface.Method2()' because it does not have the matching return type of 'void'.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Base", "Interface.Method2()", "Base.Method2()", "void").WithLocation(13, 22),
                // (13,22): error CS0737: 'Base' does not implement interface member 'Interface.Method3()'. 'Base.Method3()' cannot implement an interface member because it is not public.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "Interface").WithArguments("Base", "Interface.Method3()", "Base.Method3()").WithLocation(13, 22),
                // (13,22): error CS0737: 'Base' does not implement interface member 'Interface.Method4()'. 'Base.Method4()' cannot implement an interface member because it is not public.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "Interface").WithArguments("Base", "Interface.Method4()", "Base.Method4()").WithLocation(13, 22),
                // (13,22): error CS0737: 'Base' does not implement interface member 'Interface.Method5()'. 'Base.Method5()' cannot implement an interface member because it is not public.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "Interface").WithArguments("Base", "Interface.Method5()", "Base.Method5()").WithLocation(13, 22),
                // (13,22): error CS0737: 'Base' does not implement interface member 'Interface.Method6()'. 'Base.Method6()' cannot implement an interface member because it is not public.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "Interface").WithArguments("Base", "Interface.Method6()", "Base.Method6()").WithLocation(13, 22),
                // (13,22): error CS0736: 'Base' does not implement instance interface member 'Interface.Method1()'. 'Base.Method1()' cannot implement the interface member because it is static.
                // partial class Base : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "Interface").WithArguments("Base", "Interface.Method1()", "Base.Method1()").WithLocation(13, 22));
        }

        [Fact]
        public void TestInterfacePropertyImplementationMistakes()
        {
            var text = @"
interface Interface
{
    long Property1 { get; set; }
    long Property2 { get; set; }
    long Property3 { get; set; }
    long Property4 { get; set; }
    long Property5 { get; set; }
    long Property6 { get; set; }
}

class Base : Interface
{
    public static long Property1 { get; set; }
    public int Property2 { get; set; }
    private long Property3 { get; set; }
    internal long Property4 { get; set; }
    protected long Property5 { get; set; }
    protected internal long Property6 { get; set; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
            });
        }

        [Fact]
        public void TestInterfaceIndexerImplementationMistakes()
        {
            var text = @"
interface Interface
{
    long this[int x, int y, string z] { get; set; }
    long this[int x, string y, int z] { get; set; }
    long this[int x, string y, string z] { get; set; }
    long this[string x, int y, int z] { get; set; }
    long this[string x, int y, string z] { get; set; }
    long this[string x, string y, int z] { get; set; }
}

class Base : Interface
{
    public static long this[int x, int y, string z] { get { return 0; } set { } }
    public int this[int x, string y, int z] { get { return 0; } set { } }
    private long this[int x, string y, string z] { get { return 0; } set { } }
    internal long this[string x, int y, int z] { get { return 0; } set { } }
    protected long this[string x, int y, string z] { get { return 0; } set { } }
    protected internal long this[string x, string y, int z] { get { return 0; } set { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadMemberFlag, Line = 14, Column = 24 }, //indexer can't be static
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
            });
        }

        [Fact]
        public void TestInterfaceEventImplementationMistakes()
        {
            var text = @"
interface Interface
{
    event System.Action Event1;
    event System.Action Event2;
    event System.Action Event3;
    event System.Action Event4;
    event System.Action Event5;
    event System.Action Event6;
}

class Base : Interface
{
    public static event System.Action Event1 { add { } remove { } }
    public event System.Action<int> Event2 { add { } remove { } }
    private event System.Action Event3 { add { } remove { } }
    internal event System.Action Event4 { add { } remove { } }
    protected event System.Action Event5 { add { } remove { } }
    protected internal event System.Action Event6 { add { } remove { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
            });
        }

        [Fact]
        public void TestInterfaceImplementationMistakesInBase()
        {
            var text = @"
interface Interface
{
    void Method1();
    void Method2();
    void Method3();
    void Method4();
    void Method5();
    void Method6();
    void Method7();
}

partial class Base : Interface
{
    public static void Method1() { }
    public int Method2() { return 0;  }
    private void Method3() { }
    internal void Method4() { }
    protected void Method5() { }
    protected internal void Method6() { }
    partial void Method7();
}

class Derived1 : Base //not declaring Interface
{
}

class Derived2 : Base, Interface
{
}

partial class Base2
{
    public static void Method1() { }
    public int Method2() { return 0;  }
    private void Method3() { }
    internal void Method4() { }
    protected void Method5() { }
    protected internal void Method6() { }
    partial void Method7();
}

class Base3 : Base2 { }

class Derived : Base3, Interface { }
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                //Base
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 13, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 13, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 13, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 13, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 13, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 13, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 13, Column = 22 },

                //Derived2
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 28, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 28, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 28, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 28, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 28, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 28, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 28, Column = 24 },

                //Derived3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 45, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 45, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 45, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 45, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 45, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 45, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 45, Column = 24 }
            });
        }

        [Fact]
        public void TestInterfacePropertyImplementationMistakesInBase()
        {
            var text = @"
interface Interface
{
    long Property1 { get; set; }
    long Property2 { get; set; }
    long Property3 { get; set; }
    long Property4 { get; set; }
    long Property5 { get; set; }
    long Property6 { get; set; }
}

class Base : Interface
{
    public static long Property1 { get; set; }
    public int Property2 { get; set; }
    private long Property3 { get; set; }
    internal long Property4 { get; set; }
    protected long Property5 { get; set; }
    protected internal long Property6 { get; set; }
}

class Derived1 : Base //not declaring Interface
{
}

class Derived2 : Base, Interface
{
}

class Base2
{
    public static long Property1 { get; set; }
    public int Property2 { get; set; }
    private long Property3 { get; set; }
    internal long Property4 { get; set; }
    protected long Property5 { get; set; }
    protected internal long Property6 { get; set; }
}

class Derived3 : Base2, Interface
{
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                //Base
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },

                //Derived2
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },

                //Derived3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
            });
        }

        [Fact]
        public void TestInterfaceIndexerImplementationMistakesInBase()
        {
            var text = @"
interface Interface
{
    long this[int x, int y, string z] { get; set; }
    long this[int x, string y, int z] { get; set; }
    long this[int x, string y, string z] { get; set; }
    long this[string x, int y, int z] { get; set; }
    long this[string x, int y, string z] { get; set; }
    long this[string x, string y, int z] { get; set; }
}

class Base : Interface
{
    public static long this[int x, int y, string z] { get { return 0; } set { } }
    public int this[int x, string y, int z] { get { return 0; } set { } }
    private long this[int x, string y, string z] { get { return 0; } set { } }
    internal long this[string x, int y, int z] { get { return 0; } set { } }
    protected long this[string x, int y, string z] { get { return 0; } set { } }
    protected internal long this[string x, string y, int z] { get { return 0; } set { } }
}

class Derived1 : Base //not declaring Interface
{
}

class Derived2 : Base, Interface
{
}

class Base2
{
    public static long this[int x, int y, string z] { get { return 0; } set { } }
    public int this[int x, string y, int z] { get { return 0; } set { } }
    private long this[int x, string y, string z] { get { return 0; } set { } }
    internal long this[string x, int y, int z] { get { return 0; } set { } }
    protected long this[string x, int y, string z] { get { return 0; } set { } }
    protected internal long this[string x, string y, int z] { get { return 0; } set { } }
}

class Derived3 : Base2, Interface
{
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                //Base
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadMemberFlag, Line = 14, Column = 24 }, //indexer can't be static
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },

                //Derived2
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },

                //Derived3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },

                //Base2
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadMemberFlag, Line = 32, Column = 24 }, //indexer can't be static
            });
        }

        [Fact]
        public void TestInterfaceEventImplementationMistakesInBase()
        {
            var text = @"
interface Interface
{
    event System.Action Event1;
    event System.Action Event2;
    event System.Action Event3;
    event System.Action Event4;
    event System.Action Event5;
    event System.Action Event6;
}

class Base : Interface
{
    public static event System.Action Event1 { add { } remove { } }
    public event System.Action<int> Event2 { add { } remove { } }
    private event System.Action Event3 { add { } remove { } }
    internal event System.Action Event4 { add { } remove { } }
    protected event System.Action Event5 { add { } remove { } }
    protected internal event System.Action Event6 { add { } remove { } }
}

class Derived1 : Base //not declaring Interface
{
}

class Derived2 : Base, Interface
{
}

class Base2
{
    public static event System.Action Event1 { add { } remove { } }
    public event System.Action<int> Event2 { add { } remove { } }
    private event System.Action Event3 { add { } remove { } }
    internal event System.Action Event4 { add { } remove { } }
    protected event System.Action Event5 { add { } remove { } }
    protected internal event System.Action Event6 { add { } remove { } }
}

class Derived3 : Base2, Interface
{
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                //Base
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 12, Column = 14 },

                //Derived2
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 26, Column = 24 },

                //Derived3
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 40, Column = 25 },
            });
        }

        [Fact]
        public void TestNewRequired()
        {
            var text = @"
interface IBase
{
    void Method1();
}

interface IDerived : IBase
{
    void Method1();
}

class Base
{
    public int field = 1;
    public int Property { get { return 0; } }
    public interface Interface { }
    public class Class { }
    public struct Struct { }
    public enum Enum { Element }
    public delegate void Delegate();
    public event Delegate Event;
    public int this[int x] { get { return 0; } }
}

class Derived : Base
{
    public int field = 2;
    public int Property { get { return 0; } }
    public interface Interface { }
    public class Class { }
    public struct Struct { }
    public enum Enum { Element }
    public delegate void Delegate();
    public event Delegate Event;
    public int this[int x] { get { return 0; } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,10): warning CS0108: 'IDerived.Method1()' hides inherited member 'IBase.Method1()'. Use the new keyword if hiding was intended.
                //     void Method1();
                Diagnostic(ErrorCode.WRN_NewRequired, "Method1").WithArguments("IDerived.Method1()", "IBase.Method1()"),
                // (27,16): warning CS0108: 'Derived.field' hides inherited member 'Base.field'. Use the new keyword if hiding was intended.
                //     public int field = 2;
                Diagnostic(ErrorCode.WRN_NewRequired, "field").WithArguments("Derived.field", "Base.field"),
                // (28,16): warning CS0108: 'Derived.Property' hides inherited member 'Base.Property'. Use the new keyword if hiding was intended.
                //     public int Property { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("Derived.Property", "Base.Property"),
                // (29,22): warning CS0108: 'Derived.Interface' hides inherited member 'Base.Interface'. Use the new keyword if hiding was intended.
                //     public interface Interface { }
                Diagnostic(ErrorCode.WRN_NewRequired, "Interface").WithArguments("Derived.Interface", "Base.Interface"),
                // (30,18): warning CS0108: 'Derived.Class' hides inherited member 'Base.Class'. Use the new keyword if hiding was intended.
                //     public class Class { }
                Diagnostic(ErrorCode.WRN_NewRequired, "Class").WithArguments("Derived.Class", "Base.Class"),
                // (31,19): warning CS0108: 'Derived.Struct' hides inherited member 'Base.Struct'. Use the new keyword if hiding was intended.
                //     public struct Struct { }
                Diagnostic(ErrorCode.WRN_NewRequired, "Struct").WithArguments("Derived.Struct", "Base.Struct"),
                // (32,17): warning CS0108: 'Derived.Enum' hides inherited member 'Base.Enum'. Use the new keyword if hiding was intended.
                //     public enum Enum { Element }
                Diagnostic(ErrorCode.WRN_NewRequired, "Enum").WithArguments("Derived.Enum", "Base.Enum"),
                // (33,26): warning CS0108: 'Derived.Delegate' hides inherited member 'Base.Delegate'. Use the new keyword if hiding was intended.
                //     public delegate void Delegate();
                Diagnostic(ErrorCode.WRN_NewRequired, "Delegate").WithArguments("Derived.Delegate", "Base.Delegate"),
                // (34,27): warning CS0108: 'Derived.Event' hides inherited member 'Base.Event'. Use the new keyword if hiding was intended.
                //     public event Delegate Event;
                Diagnostic(ErrorCode.WRN_NewRequired, "Event").WithArguments("Derived.Event", "Base.Event"),
                // (35,16): warning CS0108: 'Derived.this[int]' hides inherited member 'Base.this[int]'. Use the new keyword if hiding was intended.
                //     public int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewRequired, "this").WithArguments("Derived.this[int]", "Base.this[int]"),

                // (34,27): warning CS0067: The event 'Derived.Event' is never used
                //     public event Delegate Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("Derived.Event"),
                // (21,27): warning CS0067: The event 'Base.Event' is never used
                //     public event Delegate Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("Base.Event"));
        }

        [Fact]
        public void TestNewNotRequired()
        {
            var text = @"
class C
{
    //not hiding anything
    public new int field;
    public new int Property { get { return 0; } }
    public new interface Interface { }
    public new class Class { }
    public new struct Struct { }
    public new enum Enum { Element }
    public new delegate void Delegate();
    public new event Delegate Event;
    public new int this[int x] { get { return 0; } }
}

struct S
{
    //not hiding anything
    public new int field;
    public new int Property { get { return 0; } }
    public new interface Interface { }
    public new class Class { }
    public new struct Struct { }
    public new enum Enum { Element }
    public new delegate void Delegate();
    public new event Delegate Event;
    public new int this[int x] { get { return 0; } }
}

interface Interface
{
    void Method();
    int Property { get; }
}

class D : Interface
{
    //not required for interface impls
    public new void Method() { }
    public new int Property { get { return 0; } }
}

class Base : Interface
{
    void Interface.Method() { }
    int Interface.Property { get { return 0; } }
}

class Derived : Base
{
    //not hiding Base members because impls are explicit
    public new void Method() { } 
    public new int Property { get { return 0; } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,20): warning CS0109: The member 'C.field' does not hide an accessible member. The new keyword is not required.
                //     public new int field;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "field").WithArguments("C.field"),
                // (6,20): warning CS0109: The member 'C.Property' does not hide an accessible member. The new keyword is not required.
                //     public new int Property { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Property").WithArguments("C.Property"),
                // (12,31): warning CS0109: The member 'C.Event' does not hide an accessible member. The new keyword is not required.
                //     public new event Delegate Event;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Event").WithArguments("C.Event"),
                // (7,26): warning CS0109: The member 'C.Interface' does not hide an accessible member. The new keyword is not required.
                //     public new interface Interface { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Interface").WithArguments("C.Interface"),
                // (8,22): warning CS0109: The member 'C.Class' does not hide an accessible member. The new keyword is not required.
                //     public new class Class { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Class").WithArguments("C.Class"),
                // (9,23): warning CS0109: The member 'C.Struct' does not hide an accessible member. The new keyword is not required.
                //     public new struct Struct { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Struct").WithArguments("C.Struct"),
                // (10,21): warning CS0109: The member 'C.Enum' does not hide an accessible member. The new keyword is not required.
                //     public new enum Enum { Element }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Enum").WithArguments("C.Enum"),
                // (11,30): warning CS0109: The member 'C.Delegate' does not hide an accessible member. The new keyword is not required.
                //     public new delegate void Delegate();
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Delegate").WithArguments("C.Delegate"),
                // (13,20): warning CS0109: The member 'C.this[int]' does not hide an accessible member. The new keyword is not required.
                //     public new int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "this").WithArguments("C.this[int]"),
                // (19,20): warning CS0109: The member 'S.field' does not hide an accessible member. The new keyword is not required.
                //     public new int field;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "field").WithArguments("S.field"),
                // (20,20): warning CS0109: The member 'S.Property' does not hide an accessible member. The new keyword is not required.
                //     public new int Property { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Property").WithArguments("S.Property"),
                // (26,31): warning CS0109: The member 'S.Event' does not hide an accessible member. The new keyword is not required.
                //     public new event Delegate Event;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Event").WithArguments("S.Event"),
                // (21,26): warning CS0109: The member 'S.Interface' does not hide an accessible member. The new keyword is not required.
                //     public new interface Interface { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Interface").WithArguments("S.Interface"),
                // (22,22): warning CS0109: The member 'S.Class' does not hide an accessible member. The new keyword is not required.
                //     public new class Class { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Class").WithArguments("S.Class"),
                // (23,23): warning CS0109: The member 'S.Struct' does not hide an accessible member. The new keyword is not required.
                //     public new struct Struct { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Struct").WithArguments("S.Struct"),
                // (24,21): warning CS0109: The member 'S.Enum' does not hide an accessible member. The new keyword is not required.
                //     public new enum Enum { Element }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Enum").WithArguments("S.Enum"),
                // (25,30): warning CS0109: The member 'S.Delegate' does not hide an accessible member. The new keyword is not required.
                //     public new delegate void Delegate();
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Delegate").WithArguments("S.Delegate"),
                // (27,20): warning CS0109: The member 'S.this[int]' does not hide an accessible member. The new keyword is not required.
                //     public new int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "this").WithArguments("S.this[int]"),
                // (39,21): warning CS0109: The member 'D.Method()' does not hide an accessible member. The new keyword is not required.
                //     public new void Method() { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("D.Method()"),
                // (40,20): warning CS0109: The member 'D.Property' does not hide an accessible member. The new keyword is not required.
                //     public new int Property { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Property").WithArguments("D.Property"),
                // (52,21): warning CS0109: The member 'Derived.Method()' does not hide an accessible member. The new keyword is not required.
                //     public new void Method() { } 
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("Derived.Method()"),
                // (53,20): warning CS0109: The member 'Derived.Property' does not hide an accessible member. The new keyword is not required.
                //     public new int Property { get { return 0; } }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Property").WithArguments("Derived.Property"),

                // (5,20): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value 0
                //     public new int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "0"),
                // (26,31): warning CS0067: The event 'S.Event' is never used
                //     public new event Delegate Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("S.Event"),
                // (19,20): warning CS0649: Field 'S.field' is never assigned to, and will always have its default value 0
                //     public new int field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("S.field", "0"),
                // (12,31): warning CS0067: The event 'C.Event' is never used
                //     public new event Delegate Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("C.Event")
                );
        }

        [Fact]
        public void TestNewOrOverrideRequired()
        {
            var text = @"
abstract class Base
{
    //for abstract case in Derived
    public abstract void Method1();
    public abstract void Method2();
    public abstract void Method3();

    //for virtual case in Derived
    public virtual void Method4() { }
    public virtual void Method5() { }
    public virtual void Method6() { }

    //for override case in Derived2
    public virtual void Method7() { }
    public virtual void Method8() { }
    public virtual void Method9() { }

    //for grandparent case in Derived2
    public virtual void Method10() { }
    public virtual void Method11() { }
    public virtual void Method12() { }
}

abstract class Derived : Base
{
    //abstract -> *
    public void Method1() { }
    public abstract void Method2();
    public virtual void Method3() { }
    
    //virtual -> *
    public void Method4() { }
    public abstract void Method5();
    public virtual void Method6() { }

    //for override case in Derived2
    public override void Method7() { }
    public override void Method8() { }
    public override void Method9() { }
}

abstract class Derived2 : Derived
{
    //override -> *
    public void Method7() { }
    public abstract void Method8();
    public virtual void Method9() { }

    //grandparent case
    public void Method10() { }
    public abstract void Method11();
    public virtual void Method12() { }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 28, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 28, Column = 17, IsWarning = false },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 29, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 29, Column = 26, IsWarning = false },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 30, Column = 25, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 30, Column = 25, IsWarning = false },

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 33, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 34, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 35, Column = 25, IsWarning = true },

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 46, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 47, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 48, Column = 25, IsWarning = true },

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 51, Column = 17, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 52, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 53, Column = 25, IsWarning = true },
            });
        }

        [Fact]
        public void TestPropertyNewOrOverrideRequired()
        {
            var text = @"
abstract class Base
{
    //for abstract case in Derived
    public abstract long Property1 { get; set; }
    public abstract long Property2 { get; set; }
    public abstract long Property3 { get; set; }

    //for virtual case in Derived
    public virtual long Property4 { get; set; }
    public virtual long Property5 { get; set; }
    public virtual long Property6 { get; set; }

    //for override case in Derived2
    public virtual long Property7 { get; set; }
    public virtual long Property8 { get; set; }
    public virtual long Property9 { get; set; }

    //for grandparent case in Derived2
    public virtual long Property10 { get; set; }
    public virtual long Property11 { get; set; }
    public virtual long Property12 { get; set; }
}

abstract class Derived : Base
{
    //abstract -> *
    public long Property1 { get; set; }
    public abstract long Property2 { get; set; }
    public virtual long Property3 { get; set; }
    
    //virtual -> *
    public long Property4 { get; set; }
    public abstract long Property5 { get; set; }
    public virtual long Property6 { get; set; }

    //for override case in Derived2
    public override long Property7 { get; set; }
    public override long Property8 { get; set; }
    public override long Property9 { get; set; }
}

abstract class Derived2 : Derived
{
    //override -> *
    public long Property7 { get; set; }
    public abstract long Property8 { get; set; }
    public virtual long Property9 { get; set; }

    //grandparent case
    public long Property10 { get; set; }
    public abstract long Property11 { get; set; }
    public virtual long Property12 { get; set; }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 28, Column = 17, IsWarning = true }, //1
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 28, Column = 17, IsWarning = false }, //1.get/set
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 29, Column = 26, IsWarning = true }, //2
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 29, Column = 26, IsWarning = false }, //2.get/set
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 30, Column = 25, IsWarning = true }, //3
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 30, Column = 25, IsWarning = false }, //3.get/set

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 33, Column = 17, IsWarning = true }, //4
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 34, Column = 26, IsWarning = true }, //5
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 35, Column = 25, IsWarning = true }, //6

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 46, Column = 17, IsWarning = true }, //7
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 47, Column = 26, IsWarning = true }, //8
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 48, Column = 25, IsWarning = true }, //9

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 51, Column = 17, IsWarning = true }, //10
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 52, Column = 26, IsWarning = true }, //11
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 53, Column = 25, IsWarning = true }, //12
            });
        }

        [Fact]
        public void TestIndexerNewOrOverrideRequired()
        {
            var text = @"
abstract class Base
{
    //for abstract case in Derived
    public abstract long this[int w, int x, int y, string z] { get; set; }
    public abstract long this[int w, int x, string y, int z] { get; set; }
    public abstract long this[int w, int x, string y, string z] { get; set; }

    //for virtual case in Derived
    public virtual long this[int w, string x, int y, int z] { get { return 0; } set { } }
    public virtual long this[int w, string x, int y, string z] { get { return 0; } set { } }
    public virtual long this[int w, string x, string y, int z] { get { return 0; } set { } }

    //for override case in Derived2
    public virtual long this[int w, string x, string y, string z] { get { return 0; } set { } }
    public virtual long this[string w, int x, int y, int z] { get { return 0; } set { } }
    public virtual long this[string w, int x, int y, string z] { get { return 0; } set { } }

    //for grandparent case in Derived2
    public virtual long this[string w, int x, string y, int z] { get { return 0; } set { } }
    public virtual long this[string w, int x, string y, string z] { get { return 0; } set { } }
    public virtual long this[string w, string x, int y, int z] { get { return 0; } set { } }
}

abstract class Derived : Base
{
    //abstract -> *
    public long this[int w, int x, int y, string z] { get { return 0; } set { } }
    public abstract long this[int w, int x, string y, int z] { get; set; }
    public virtual long this[int w, int x, string y, string z] { get { return 0; } set { } }
    
    //virtual -> *
    public long this[int w, string x, int y, int z] { get { return 0; } set { } }
    public abstract long this[int w, string x, int y, string z] { get; set; }
    public virtual long this[int w, string x, string y, int z] { get { return 0; } set { } }

    //for override case in Derived2
    public override long this[int w, string x, string y, string z] { get { return 0; } set { } }
    public override long this[string w, int x, int y, int z] { get { return 0; } set { } }
    public override long this[string w, int x, int y, string z] { get { return 0; } set { } }
}

abstract class Derived2 : Derived
{
    //override -> *
    public long this[int w, string x, string y, string z] { get { return 0; } set { } }
    public abstract long this[string w, int x, int y, int z] { get; set; }
    public virtual long this[string w, int x, int y, string z] { get { return 0; } set { } }

    //grandparent case
    public long this[string w, int x, string y, int z] { get { return 0; } set { } }
    public abstract long this[string w, int x, string y, string z] { get; set; }
    public virtual long this[string w, string x, int y, int z] { get { return 0; } set { } }
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 28, Column = 17, IsWarning = true }, //1
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 28, Column = 17, IsWarning = false }, //1.get/set
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 29, Column = 26, IsWarning = true }, //2
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 29, Column = 26, IsWarning = false }, //2.get/set
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 30, Column = 25, IsWarning = true }, //3
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 30, Column = 25, IsWarning = false }, //3.get/set

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 33, Column = 17, IsWarning = true }, //4
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 34, Column = 26, IsWarning = true }, //5
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 35, Column = 25, IsWarning = true }, //6

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 46, Column = 17, IsWarning = true }, //7
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 47, Column = 26, IsWarning = true }, //8
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 48, Column = 25, IsWarning = true }, //9

                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 51, Column = 17, IsWarning = true }, //10
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 52, Column = 26, IsWarning = true }, //11
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 53, Column = 25, IsWarning = true }, //12
            });
        }

        [Fact]
        public void TestEventNewOrOverrideRequired()
        {
            var text = @"
abstract class Base
{
    //for abstract case in Derived
    public abstract event System.Action Event1;
    public abstract event System.Action Event2;
    public abstract event System.Action Event3;

    //for virtual case in Derived
    public virtual event System.Action Event4 { add { } remove { } }
    public virtual event System.Action Event5 { add { } remove { } }
    public virtual event System.Action Event6 { add { } remove { } }

    //for override case in Derived2
    public virtual event System.Action Event7 { add { } remove { } }
    public virtual event System.Action Event8 { add { } remove { } }
    public virtual event System.Action Event9 { add { } remove { } }

    //for grandparent case in Derived2
    public virtual event System.Action Event10 { add { } remove { } }
    public virtual event System.Action Event11 { add { } remove { } }
    public virtual event System.Action Event12 { add { } remove { } }
}

abstract class Derived : Base
{
    //abstract -> *
    public event System.Action Event1 { add { } remove { } }
    public abstract event System.Action Event2;
    public virtual event System.Action Event3 { add { } remove { } }
    
    //virtual -> *
    public event System.Action Event4 { add { } remove { } }
    public abstract event System.Action Event5;
    public virtual event System.Action Event6 { add { } remove { } }

    //for override case in Derived2
    public override event System.Action Event7 { add { } remove { } }
    public override event System.Action Event8 { add { } remove { } }
    public override event System.Action Event9 { add { } remove { } }
}

abstract class Derived2 : Derived
{
    //override -> *
    public event System.Action Event7 { add { } remove { } }
    public abstract event System.Action Event8;
    public virtual event System.Action Event9 { add { } remove { } }

    //grandparent case
    public event System.Action Event10 { add { } remove { } }
    public abstract event System.Action Event11;
    public virtual event System.Action Event12 { add { } remove { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (28,32): error CS0533: 'Derived.Event1' hides inherited abstract member 'Base.Event1'
                //     public event System.Action Event1 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Event1").WithArguments("Derived.Event1", "Base.Event1"),
                // (28,32): warning CS0114: 'Derived.Event1' hides inherited member 'Base.Event1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public event System.Action Event1 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event1").WithArguments("Derived.Event1", "Base.Event1"),
                // (29,41): error CS0533: 'Derived.Event2' hides inherited abstract member 'Base.Event2'
                //     public abstract event System.Action Event2;
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Event2").WithArguments("Derived.Event2", "Base.Event2"),
                // (29,41): warning CS0114: 'Derived.Event2' hides inherited member 'Base.Event2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public abstract event System.Action Event2;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event2").WithArguments("Derived.Event2", "Base.Event2"),
                // (30,40): error CS0533: 'Derived.Event3' hides inherited abstract member 'Base.Event3'
                //     public virtual event System.Action Event3 { add { } remove { } }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Event3").WithArguments("Derived.Event3", "Base.Event3"),
                // (30,40): warning CS0114: 'Derived.Event3' hides inherited member 'Base.Event3'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public virtual event System.Action Event3 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event3").WithArguments("Derived.Event3", "Base.Event3"),
                // (33,32): warning CS0114: 'Derived.Event4' hides inherited member 'Base.Event4'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public event System.Action Event4 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event4").WithArguments("Derived.Event4", "Base.Event4"),
                // (34,41): warning CS0114: 'Derived.Event5' hides inherited member 'Base.Event5'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public abstract event System.Action Event5;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event5").WithArguments("Derived.Event5", "Base.Event5"),
                // (35,40): warning CS0114: 'Derived.Event6' hides inherited member 'Base.Event6'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public virtual event System.Action Event6 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event6").WithArguments("Derived.Event6", "Base.Event6"),
                // (46,32): warning CS0114: 'Derived2.Event7' hides inherited member 'Derived.Event7'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public event System.Action Event7 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event7").WithArguments("Derived2.Event7", "Derived.Event7"),
                // (47,41): warning CS0114: 'Derived2.Event8' hides inherited member 'Derived.Event8'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public abstract event System.Action Event8;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event8").WithArguments("Derived2.Event8", "Derived.Event8"),
                // (48,40): warning CS0114: 'Derived2.Event9' hides inherited member 'Derived.Event9'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public virtual event System.Action Event9 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event9").WithArguments("Derived2.Event9", "Derived.Event9"),
                // (51,32): warning CS0114: 'Derived2.Event10' hides inherited member 'Base.Event10'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public event System.Action Event10 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event10").WithArguments("Derived2.Event10", "Base.Event10"),
                // (52,41): warning CS0114: 'Derived2.Event11' hides inherited member 'Base.Event11'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public abstract event System.Action Event11;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event11").WithArguments("Derived2.Event11", "Base.Event11"),
                // (53,40): warning CS0114: 'Derived2.Event12' hides inherited member 'Base.Event12'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public virtual event System.Action Event12 { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Event12").WithArguments("Derived2.Event12", "Base.Event12"));
        }

        [Fact]
        public void TestExplicitImplementationAmbiguousInterfaceMethod()
        {
            var text = @"
public interface Interface<T>
{
    void Method(int i);
    void Method(T i);
}

public class Class : Interface<int>
{
    void Interface<int>.Method(int i) { } //this explicitly implements both methods in Interface<int>
    public void Method(int i) { } //this is here to avoid CS0535 - not implementing interface method
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_ExplicitImplCollision, Line = 10, Column = 25, IsWarning = true },
            });
        }

        [Fact]
        public void TestExplicitImplementationAmbiguousInterfaceMethodWithDifferingConstraints()
        {
            var text = @"
public interface Interface<T>
{
    void Method<V>(int i) where V : new();
    void Method<V>(T i);
}

public class Class : Interface<int>
{
    void Interface<int>.Method<V>(int i) { _ = new V(); } //this explicitly implements both methods in Interface<int>
    public void Method<V>(int i) { } //this is here to avoid CS0535 - not implementing interface method
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,25): warning CS0473: Explicit interface implementation 'Class.Interface<int>.Method<V>(int)' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                //     void Interface<int>.Method<V>(int i) { _ = new V(); } //this explicitly implements both methods in Interface<int>
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Class.Interface<int>.Method<V>(int)").WithLocation(10, 25));
        }

        [Fact]
        public void TestExplicitImplementationAmbiguousInterfaceMethodWithDifferingConstraints_OppositeDeclarationOrder()
        {
            var text = @"
public interface Interface<T>
{
    void Method<V>(T i);
    void Method<V>(int i) where V : new();
}

public class Class : Interface<int>
{
    void Interface<int>.Method<V>(int i) { _ = new V(); } //this explicitly implements both methods in Interface<int>
    public void Method<V>(int i) { } //this is here to avoid CS0535 - not implementing interface method
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,25): warning CS0473: Explicit interface implementation 'Class.Interface<int>.Method<V>(int)' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                //     void Interface<int>.Method<V>(int i) { _ = new V(); } //this explicitly implements both methods in Interface<int>
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Class.Interface<int>.Method<V>(int)").WithLocation(10, 25),
                // (10,48): error CS0304: Cannot create an instance of the variable type 'V' because it does not have the new() constraint
                //     void Interface<int>.Method<V>(int i) { _ = new V(); } //this explicitly implements both methods in Interface<int>
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new V()").WithArguments("V").WithLocation(10, 48),
                // (11,17): error CS0425: The constraints for type parameter 'V' of method 'Class.Method<V>(int)' must match the constraints for type parameter 'V' of interface method 'Interface<int>.Method<V>(int)'. Consider using an explicit interface implementation instead.
                //     public void Method<V>(int i) { } //this is here to avoid CS0535 - not implementing interface method
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "Method").WithArguments("V", "Class.Method<V>(int)", "V", "Interface<int>.Method<V>(int)").WithLocation(11, 17));
        }

        [Fact]
        public void TestExplicitImplementationAmbiguousInterfaceIndexer()
        {
            var text = @"
public interface Interface<T>
{
    long this[int i] { set; }
    long this[T i] { set; }
}

public class Class : Interface<int>
{
    long Interface<int>.this[int i] { set { } } //this explicitly implements both methods in Interface<int>
    public long this[int i] { set { } } //this is here to avoid CS0535 - not implementing interface method
}
";
            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.WRN_ExplicitImplCollision, Line = 10, Column = 25, IsWarning = true },
            });
        }

        [Fact]
        public void TestAmbiguousImplementationMethod()
        {
            var text = @"
public interface Interface<T, U>
{
    void Method(int i);
    void Method(T i);
    void Method(U i);
}

public class Base<T> : Interface<T, T>
{
    public void Method(int i) { }
    public void Method(T i) { }
}

public class Derived : Base<int>, Interface<int, int>
{
}

class Other : Interface<int, int>
{
    void Interface<int, int>.Method(int i) { }
}

class YetAnother : Interface<int, int>
{
    public void Method(int i) { }
}
";
            //Both Base methods implement Interface.Method(int)
            //Both Base methods implement Interface.Method(T)
            //Both Base methods implement Interface.Method(U)
            CreateCompilation(text).VerifyDiagnostics(
                // (15,35): warning CS1956: Member 'Base<int>.Method(int)' implements interface member 'Interface<int, int>.Method(int)' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                // public class Derived : Base<int>, Interface<int, int>
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Interface<int, int>").WithArguments("Base<int>.Method(int)", "Interface<int, int>.Method(int)", "Derived").WithLocation(15, 35),
                // (15,35): warning CS1956: Member 'Base<int>.Method(int)' implements interface member 'Interface<int, int>.Method(int)' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                // public class Derived : Base<int>, Interface<int, int>
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Interface<int, int>").WithArguments("Base<int>.Method(int)", "Interface<int, int>.Method(int)", "Derived").WithLocation(15, 35),
                // (15,35): warning CS1956: Member 'Base<int>.Method(int)' implements interface member 'Interface<int, int>.Method(int)' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                // public class Derived : Base<int>, Interface<int, int>
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Interface<int, int>").WithArguments("Base<int>.Method(int)", "Interface<int, int>.Method(int)", "Derived").WithLocation(15, 35),
                // (19,15): error CS0535: 'Other' does not implement interface member 'Interface<int, int>.Method(int)'
                // class Other : Interface<int, int>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface<int, int>").WithArguments("Other", "Interface<int, int>.Method(int)").WithLocation(19, 15),
                // (19,15): error CS0535: 'Other' does not implement interface member 'Interface<int, int>.Method(int)'
                // class Other : Interface<int, int>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface<int, int>").WithArguments("Other", "Interface<int, int>.Method(int)").WithLocation(19, 15),
                // (21,30): warning CS0473: Explicit interface implementation 'Other.Interface<int, int>.Method(int)' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                //     void Interface<int, int>.Method(int i) { }
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Other.Interface<int, int>.Method(int)").WithLocation(21, 30)
                );
        }

        [Fact]
        public void TestAmbiguousImplementationIndexer()
        {
            var text = @"
public interface Interface<T, U>
{
    long this[int i] { set; }
    long this[T i] { set; }
    long this[U i] { set; }
}

public class Base<T> : Interface<T, T>
{
    public long this[int i] { set { } }
    public long this[T i] { set { } }
}

public class Derived : Base<int>, Interface<int, int>
{
}
";
            // CONSIDER: Dev10 doesn't report these warnings -  not sure why
            CreateCompilation(text).VerifyDiagnostics(
                // (15,35): warning CS1956: Member 'Base<int>.this[int]' implements interface member 'Interface<int, int>.this[int]' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                // public class Derived : Base<int>, Interface<int, int>
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Interface<int, int>").WithArguments("Base<int>.this[int]", "Interface<int, int>.this[int]", "Derived").WithLocation(15, 35),
                // (15,35): warning CS1956: Member 'Base<int>.this[int]' implements interface member 'Interface<int, int>.this[int]' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                // public class Derived : Base<int>, Interface<int, int>
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Interface<int, int>").WithArguments("Base<int>.this[int]", "Interface<int, int>.this[int]", "Derived").WithLocation(15, 35),
                // (15,35): warning CS1956: Member 'Base<int>.this[int]' implements interface member 'Interface<int, int>.this[int]' in type 'Derived'. There are multiple matches for the interface member at run-time. It is implementation dependent which method will be called.
                // public class Derived : Base<int>, Interface<int, int>
                Diagnostic(ErrorCode.WRN_MultipleRuntimeImplementationMatches, "Interface<int, int>").WithArguments("Base<int>.this[int]", "Interface<int, int>.this[int]", "Derived").WithLocation(15, 35));
        }

        [Fact]
        public void TestHideAmbiguousImplementationMethod()
        {
            var text = @"
public interface Interface<T, U>
{
    void Method(int i);
    void Method(T i);
    void Method(U i);
}

public class Base<T> : Interface<T, T>
{
    public void Method(int i) { }
    public void Method(T i) { }
}

public class Derived : Base<int>, Interface<int, int>
{
    public new void Method(int i) { } //overrides Base's interface mapping
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void TestHideAmbiguousImplementationIndexer()
        {
            var text = @"
public interface Interface<T, U>
{
    long this[int i] { set; }
    long this[T i] { set; }
    long this[U i] { set; }
}

public class Base<T> : Interface<T, T>
{
    public long this[int i] { set { } }
    public long this[T i] { set { } }
}

public class Derived : Base<int>, Interface<int, int>
{
    public new long this[int i] { set { } } //overrides Base's interface mapping
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void TestHideAmbiguousOverridesMethod()
        {
            var text = @"
public class Base<T, U>
{
    public virtual void Method(int i) { }
    public virtual void Method(T i) { }
    public virtual void Method(U i) { }
}

public class Derived : Base<int, int>
{
    public new virtual void Method(int i) { }
}

public class Derived2 : Derived
{
    public override void Method(int i) { base.Method(i); }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void TestHideAmbiguousOverridesIndexer()
        {
            var text = @"
public class Base<T, U>
{
    public virtual long this[int i] { set { } }
    public virtual long this[T i] { set { } }
    public virtual long this[U i] { set { } }
}

public class Derived : Base<int, int>
{
    public new virtual long this[int i] { set { } }
}

public class Derived2 : Derived
{
    public override long this[int i] { set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void TestAmbiguousOverrideMethod()
        {
            var text = @"
public class Base<TShort, TInt>
{
    public virtual void Method(TShort s, int i) { }
    public virtual void Method(short s, TInt i) { }
}

public class Derived : Base<short, int>
{
    public override void Method(short s, int i) { }
}
";
            CSharpCompilation comp = CreateCompilation(text, targetFramework: TargetFramework.NetLatest);
            Assert.Equal(RuntimeUtilities.IsCoreClrRuntime, comp.Assembly.RuntimeSupportsCovariantReturnsOfClasses);
            Assert.Equal(RuntimeUtilities.IsCoreClrRuntime, comp.SupportsRuntimeCapability(RuntimeCapability.CovariantReturnsOfClasses));

            if (comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation)
            {
                comp.VerifyDiagnostics(
                    // (10,26): error CS0462: The inherited members 'Base<TShort, TInt>.Method(TShort, int)' and 'Base<TShort, TInt>.Method(short, TInt)' have the same signature in type 'Derived', so they cannot be overridden
                    //     public override void Method(short s, int i) { }
                    Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Base<TShort, TInt>.Method(TShort, int)", "Base<TShort, TInt>.Method(short, TInt)", "Derived").WithLocation(10, 26)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (4,25): warning CS1957: Member 'Derived.Method(short, int)' overrides 'Base<short, int>.Method(short, int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual void Method(TShort s, int i) { }
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<short, int>.Method(short, int)", "Derived.Method(short, int)").WithLocation(4, 25),
                    // (10,26): error CS0462: The inherited members 'Base<TShort, TInt>.Method(TShort, int)' and 'Base<TShort, TInt>.Method(short, TInt)' have the same signature in type 'Derived', so they cannot be overridden
                    //     public override void Method(short s, int i) { }
                    Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Base<TShort, TInt>.Method(TShort, int)", "Base<TShort, TInt>.Method(short, TInt)", "Derived").WithLocation(10, 26)
                    );
            }
        }

        [Fact]
        public void TestAmbiguousOverrideIndexer()
        {
            var text = @"
public class Base<TShort, TInt>
{
    public virtual long this[TShort s, int i] { set { } }
    public virtual long this[short s, TInt i] { set { } }
}

public class Derived : Base<short, int>
{
    public override long this[short s, int i] { set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,26): error CS0462: The inherited members 'Base<TShort, TInt>.this[TShort, int]' and 'Base<TShort, TInt>.this[short, TInt]' have the same signature in type 'Derived', so they cannot be overridden
                Diagnostic(ErrorCode.ERR_AmbigOverride, "this").WithArguments("Base<TShort, TInt>.this[TShort, int]", "Base<TShort, TInt>.this[short, TInt]", "Derived"));
        }

        [Fact]
        public void TestRuntimeAmbiguousOverride()
        {
            var text = @"
class Base<TInt>
{
    //these signatures differ only in ref/out
    public virtual void Method(int @in, ref int @ref) { }
    public virtual void Method(TInt @in, out TInt @out) { @out = @in; }
}

class Derived : Base<int>
{
    public override void Method(int @in, ref int @ref) { }
}
";
            var compilation = CreateCompilation(text, targetFramework: TargetFramework.NetLatest);
            Assert.Equal(RuntimeUtilities.IsCoreClrRuntime, compilation.Assembly.RuntimeSupportsCovariantReturnsOfClasses);
            Assert.Equal(RuntimeUtilities.IsCoreClrRuntime, compilation.SupportsRuntimeCapability(RuntimeCapability.CovariantReturnsOfClasses));

            if (compilation.Assembly.RuntimeSupportsCovariantReturnsOfClasses)
            {
                // We no longer report a runtime ambiguous override because the compiler
                // produces a methodimpl record to disambiguate.
                compilation.VerifyDiagnostics(
                    );
            }
            else
            {
                compilation.VerifyDiagnostics(
                    // (5,25): warning CS1957: Member 'Derived.Method(int, ref int)' overrides 'Base<int>.Method(int, ref int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual void Method(int @in, ref int @ref) { }
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<int>.Method(int, ref int)", "Derived.Method(int, ref int)").WithLocation(5, 25)
                    );
            }
        }

        [Fact]
        public void TestOverrideInaccessibleMethod()
        {
            var text1 = @"
public class Base
{
    internal virtual void Method() {}
}
";

            var text2 = @"
public class Derived : Base
{
    internal override void Method() { }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 4, Column = 28 }, //can't see internal method in other compilation
            });
        }

        [Fact]
        public void TestOverrideInaccessibleProperty()
        {
            var text1 = @"
public class Base
{
    internal virtual long Property { get; set; }
}
";

            var text2 = @"
public class Derived : Base
{
    internal override long Property { get; set; }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 4, Column = 28 }, //can't see internal method in other compilation
            });
        }

        [Fact]
        public void TestOverrideInaccessibleIndexer()
        {
            var text1 = @"
public class Base
{
    internal virtual long this[int x] { get { return 0; } set { } }
}
";

            var text2 = @"
public class Derived : Base
{
    internal override long this[int x] { get { return 0; } set { } }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 4, Column = 28 }, //can't see internal method in other compilation
            });
        }

        [Fact]
        public void TestOverrideInaccessibleEvent()
        {
            var text1 = @"
public class Base
{
    internal virtual event System.Action Event { add { } remove { } }
}
";

            var text2 = @"
public class Derived : Base
{
    internal override event System.Action Event { add { } remove { } }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 4, Column = 43 }, //can't see internal method in other compilation
            });
        }

        [Fact]
        public void TestVirtualMethodAccessibilityWithinAssembly()
        {
            var text = @"
public class Base
{
    internal virtual void Method1() { }
    internal virtual void Method2() { }
    internal virtual void Method3() { }

    protected virtual void Method4() { }
    protected virtual void Method5() { }
    protected virtual void Method6() { }

    protected internal virtual void Method7() { }
    protected internal virtual void Method8() { }
    protected internal virtual void Method9() { }
    protected internal virtual void Method10() { }

    public virtual void Method11() { }
    public virtual void Method12() { }
    public virtual void Method13() { }

    private protected virtual void Method14() { }
    private protected virtual void Method15() { }
    private protected virtual void Method16() { }
    private protected virtual void Method17() { }
}

public class Derived1 : Base
{
    protected override void Method1() { }
    protected internal override void Method2() { }
    public override void Method3() { }

    internal override void Method4() { }
    protected internal override void Method5() { }
    public override void Method6() { }

    internal override void Method7() { }
    protected override void Method8() { }
    protected internal override void Method9() { } //correct
    public override void Method10() { }

    internal override void Method11() { }
    protected override void Method12() { }
    protected internal override void Method13() { }

    internal override void Method14() { }
    protected override void Method15() { }
    protected internal override void Method16() { }
    public override void Method17() { }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (30,38): error CS0507: 'Derived1.Method2()': cannot change access modifiers when overriding 'internal' inherited member 'Base.Method2()'
                //     protected internal override void Method2() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method2").WithArguments("Derived1.Method2()", "internal", "Base.Method2()").WithLocation(30, 38),
                // (31,26): error CS0507: 'Derived1.Method3()': cannot change access modifiers when overriding 'internal' inherited member 'Base.Method3()'
                //     public override void Method3() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method3").WithArguments("Derived1.Method3()", "internal", "Base.Method3()").WithLocation(31, 26),
                // (33,28): error CS0507: 'Derived1.Method4()': cannot change access modifiers when overriding 'protected' inherited member 'Base.Method4()'
                //     internal override void Method4() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method4").WithArguments("Derived1.Method4()", "protected", "Base.Method4()").WithLocation(33, 28),
                // (34,38): error CS0507: 'Derived1.Method5()': cannot change access modifiers when overriding 'protected' inherited member 'Base.Method5()'
                //     protected internal override void Method5() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method5").WithArguments("Derived1.Method5()", "protected", "Base.Method5()").WithLocation(34, 38),
                // (35,26): error CS0507: 'Derived1.Method6()': cannot change access modifiers when overriding 'protected' inherited member 'Base.Method6()'
                //     public override void Method6() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method6").WithArguments("Derived1.Method6()", "protected", "Base.Method6()").WithLocation(35, 26),
                // (37,28): error CS0507: 'Derived1.Method7()': cannot change access modifiers when overriding 'protected internal' inherited member 'Base.Method7()'
                //     internal override void Method7() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method7").WithArguments("Derived1.Method7()", "protected internal", "Base.Method7()").WithLocation(37, 28),
                // (38,29): error CS0507: 'Derived1.Method8()': cannot change access modifiers when overriding 'protected internal' inherited member 'Base.Method8()'
                //     protected override void Method8() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method8").WithArguments("Derived1.Method8()", "protected internal", "Base.Method8()").WithLocation(38, 29),
                // (40,26): error CS0507: 'Derived1.Method10()': cannot change access modifiers when overriding 'protected internal' inherited member 'Base.Method10()'
                //     public override void Method10() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method10").WithArguments("Derived1.Method10()", "protected internal", "Base.Method10()").WithLocation(40, 26),
                // (42,28): error CS0507: 'Derived1.Method11()': cannot change access modifiers when overriding 'public' inherited member 'Base.Method11()'
                //     internal override void Method11() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method11").WithArguments("Derived1.Method11()", "public", "Base.Method11()").WithLocation(42, 28),
                // (43,29): error CS0507: 'Derived1.Method12()': cannot change access modifiers when overriding 'public' inherited member 'Base.Method12()'
                //     protected override void Method12() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method12").WithArguments("Derived1.Method12()", "public", "Base.Method12()").WithLocation(43, 29),
                // (44,38): error CS0507: 'Derived1.Method13()': cannot change access modifiers when overriding 'public' inherited member 'Base.Method13()'
                //     protected internal override void Method13() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method13").WithArguments("Derived1.Method13()", "public", "Base.Method13()").WithLocation(44, 38),
                // (46,28): error CS0507: 'Derived1.Method14()': cannot change access modifiers when overriding 'private protected' inherited member 'Base.Method14()'
                //     internal override void Method14() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method14").WithArguments("Derived1.Method14()", "private protected", "Base.Method14()").WithLocation(46, 28),
                // (47,29): error CS0507: 'Derived1.Method15()': cannot change access modifiers when overriding 'private protected' inherited member 'Base.Method15()'
                //     protected override void Method15() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method15").WithArguments("Derived1.Method15()", "private protected", "Base.Method15()").WithLocation(47, 29),
                // (48,38): error CS0507: 'Derived1.Method16()': cannot change access modifiers when overriding 'private protected' inherited member 'Base.Method16()'
                //     protected internal override void Method16() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method16").WithArguments("Derived1.Method16()", "private protected", "Base.Method16()").WithLocation(48, 38),
                // (49,26): error CS0507: 'Derived1.Method17()': cannot change access modifiers when overriding 'private protected' inherited member 'Base.Method17()'
                //     public override void Method17() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method17").WithArguments("Derived1.Method17()", "private protected", "Base.Method17()").WithLocation(49, 26),
                // (29,29): error CS0507: 'Derived1.Method1()': cannot change access modifiers when overriding 'internal' inherited member 'Base.Method1()'
                //     protected override void Method1() { }
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "Method1").WithArguments("Derived1.Method1()", "internal", "Base.Method1()").WithLocation(29, 29)
                );
        }

        [Fact]
        public void TestVirtualPropertyAccessibilityWithinAssembly()
        {
            var text = @"
public class Base
{
    internal virtual long Property1 { get; set; }
    internal virtual long Property2 { get; set; }
    internal virtual long Property3 { get; set; }

    protected virtual long Property4 { get; set; }
    protected virtual long Property5 { get; set; }
    protected virtual long Property6 { get; set; }

    protected internal virtual long Property7 { get; set; }
    protected internal virtual long Property8 { get; set; }
    protected internal virtual long Property9 { get; set; }
    protected internal virtual long Property10 { get; set; }

    public virtual long Property11 { get; set; }
    public virtual long Property12 { get; set; }
    public virtual long Property13 { get; set; }
}

public class Derived1 : Base
{
    protected override long Property1 { get; set; }
    protected internal override long Property2 { get; set; }
    public override long Property3 { get; set; }

    internal override long Property4 { get; set; }
    protected internal override long Property5 { get; set; }
    public override long Property6 { get; set; }

    internal override long Property7 { get; set; }
    protected override long Property8 { get; set; }
    protected internal override long Property9 { get; set; } //correct
    public override long Property10 { get; set; }

    internal override long Property11 { get; set; }
    protected override long Property12 { get; set; }
    protected internal override long Property13 { get; set; }
}
";

            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 24, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 25, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 26, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 28, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 29, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 30, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 32, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 33, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 35, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 37, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 38, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 39, Column = 38 },
            });
        }

        [Fact]
        public void TestVirtualIndexerAccessibilityWithinAssembly()
        {
            var text = @"
public class Base
{
    internal virtual long this[int w, int x, int y, string z] { get { return 0; } set { } }
    internal virtual long this[int w, int x, string y, int z] { get { return 0; } set { } }
    internal virtual long this[int w, int x, string y, string z] { get { return 0; } set { } }

    protected virtual long this[int w, string x, int y, int z] { get { return 0; } set { } }
    protected virtual long this[int w, string x, int y, string z] { get { return 0; } set { } }
    protected virtual long this[int w, string x, string y, int z] { get { return 0; } set { } }

    protected internal virtual long this[int w, string x, string y, string z] { get { return 0; } set { } }
    protected internal virtual long this[string w, int x, int y, int z] { get { return 0; } set { } }
    protected internal virtual long this[string w, int x, int y, string z] { get { return 0; } set { } }
    protected internal virtual long this[string w, int x, string y, int z] { get { return 0; } set { } }

    public virtual long this[string w, int x, string y, string z] { get { return 0; } set { } }
    public virtual long this[string w, string x, int y, int z] { get { return 0; } set { } }
    public virtual long this[string w, string x, int y, string z] { get { return 0; } set { } }
}

public class Derived1 : Base
{
    protected override long this[int w, int x, int y, string z] { get { return 0; } set { } }
    protected internal override long this[int w, int x, string y, int z] { get { return 0; } set { } }
    public override long this[int w, int x, string y, string z] { get { return 0; } set { } }

    internal override long this[int w, string x, int y, int z] { get { return 0; } set { } }
    protected internal override long this[int w, string x, int y, string z] { get { return 0; } set { } }
    public override long this[int w, string x, string y, int z] { get { return 0; } set { } }

    internal override long this[int w, string x, string y, string z] { get { return 0; } set { } }
    protected override long this[string w, int x, int y, int z] { get { return 0; } set { } }
    protected internal override long this[string w, int x, int y, string z] { get { return 0; } set { } } //correct
    public override long this[string w, int x, string y, int z] { get { return 0; } set { } }

    internal override long this[string w, int x, string y, string z] { get { return 0; } set { } }
    protected override long this[string w, string x, int y, int z] { get { return 0; } set { } }
    protected internal override long this[string w, string x, int y, string z] { get { return 0; } set { } }
}
";

            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 24, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 25, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 26, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 28, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 29, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 30, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 32, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 33, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 35, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 37, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 38, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 39, Column = 38 },
            });
        }

        [Fact]
        public void TestVirtualEventAccessibilityWithinAssembly()
        {
            var text = @"
public class Base
{
    internal virtual event System.Action Event1 { add { } remove { } }
    internal virtual event System.Action Event2 { add { } remove { } }
    internal virtual event System.Action Event3 { add { } remove { } }

    protected virtual event System.Action Event4 { add { } remove { } }
    protected virtual event System.Action Event5 { add { } remove { } }
    protected virtual event System.Action Event6 { add { } remove { } }

    protected internal virtual event System.Action Event7 { add { } remove { } }
    protected internal virtual event System.Action Event8 { add { } remove { } }
    protected internal virtual event System.Action Event9 { add { } remove { } }
    protected internal virtual event System.Action Event10 { add { } remove { } }

    public virtual event System.Action Event11 { add { } remove { } }
    public virtual event System.Action Event12 { add { } remove { } }
    public virtual event System.Action Event13 { add { } remove { } }
}

public class Derived1 : Base
{
    protected override event System.Action Event1 { add { } remove { } }
    protected internal override event System.Action Event2 { add { } remove { } }
    public override event System.Action Event3 { add { } remove { } }

    internal override event System.Action Event4 { add { } remove { } }
    protected internal override event System.Action Event5 { add { } remove { } }
    public override event System.Action Event6 { add { } remove { } }

    internal override event System.Action Event7 { add { } remove { } }
    protected override event System.Action Event8 { add { } remove { } }
    protected internal override event System.Action Event9 { add { } remove { } } //correct
    public override event System.Action Event10 { add { } remove { } }

    internal override event System.Action Event11 { add { } remove { } }
    protected override event System.Action Event12 { add { } remove { } }
    protected internal override event System.Action Event13 { add { } remove { } }
}
";

            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 24, Column = 44 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 25, Column = 53 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 26, Column = 41 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 28, Column = 43 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 29, Column = 53 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 30, Column = 41 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 32, Column = 43 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 33, Column = 44 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 35, Column = 41 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 37, Column = 43 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 38, Column = 44 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 39, Column = 53 },
            });
        }

        [WorkItem(540185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540185")]
        [Fact]
        public void TestChangeVirtualPropertyAccessorAccessibilityWithinAssembly()
        {
            var text = @"
public class Base
{
    public virtual long Property1 { get; protected set; }
}

public class Derived1 : Base
{
    public override long Property1 { get; private set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "set").WithArguments("Derived1.Property1.set", "protected", "Base.Property1.set"));
        }

        [Fact]
        public void TestChangeVirtualIndexerAccessorAccessibilityWithinAssembly()
        {
            var text = @"
public class Base
{
    public virtual long this[int x] { get { return 0; } protected set { } }
}

public class Derived1 : Base
{
    public override long this[int x] { get { return 0; } private set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,66): error CS0507: 'Derived1.this[int].set': cannot change access modifiers when overriding 'protected' inherited member 'Base.this[int].set'
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "set").WithArguments("Derived1.this[int].set", "protected", "Base.this[int].set"));
        }

        [Fact]
        public void TestVirtualMethodAccessibilityAcrossAssemblies()
        {
            var text1 = @"
public class Base
{
    internal virtual void Method1() { }
    internal virtual void Method2() { }
    internal virtual void Method3() { }

    protected virtual void Method4() { }
    protected virtual void Method5() { }
    protected virtual void Method6() { }

    protected internal virtual void Method7() { }
    protected internal virtual void Method8() { }
    protected internal virtual void Method9() { }
    protected internal virtual void Method10() { }

    public virtual void Method11() { }
    public virtual void Method12() { }
    public virtual void Method13() { }
}
";

            var text2 = @"
public class Derived2 : Base
{
    //can't find to override
    protected override void Method1() { }
    protected internal override void Method2() { }
    public override void Method3() { }

    internal override void Method4() { }
    protected internal override void Method5() { }
    public override void Method6() { }

    //protected internal in another assembly is protected in this one
    internal override void Method7() { }
    protected override void Method8() { } //correct
    protected internal override void Method9() { }
    public override void Method10() { }

    internal override void Method11() { }
    protected override void Method12() { }
    protected internal override void Method13() { }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 5, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 6, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 7, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 9,  Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 10, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 11, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 14, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 16, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 17, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 19, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 20, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 21, Column = 38 },
            });
        }

        [Fact]
        public void TestVirtualPropertyAccessibilityAcrossAssemblies()
        {
            var text1 = @"
public class Base
{
    internal virtual long Property1 { get; set; }
    internal virtual long Property2 { get; set; }
    internal virtual long Property3 { get; set; }

    protected virtual long Property4 { get; set; }
    protected virtual long Property5 { get; set; }
    protected virtual long Property6 { get; set; }

    protected internal virtual long Property7 { get; set; }
    protected internal virtual long Property8 { get; set; }
    protected internal virtual long Property9 { get; set; }
    protected internal virtual long Property10 { get; set; }

    public virtual long Property11 { get; set; }
    public virtual long Property12 { get; set; }
    public virtual long Property13 { get; set; }
}
";

            var text2 = @"
public class Derived2 : Base
{
    //can't find to override
    protected override long Property1 { get; set; }
    protected internal override long Property2 { get; set; }
    public override long Property3 { get; set; }

    internal override long Property4 { get; set; }
    protected internal override long Property5 { get; set; }
    public override long Property6 { get; set; }

    //protected internal in another assembly is protected in this one
    internal override long Property7 { get; set; }
    protected override long Property8 { get; set; } //correct
    protected internal override long Property9 { get; set; }
    public override long Property10 { get; set; }

    internal override long Property11 { get; set; }
    protected override long Property12 { get; set; }
    protected internal override long Property13 { get; set; }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 5, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 6, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 7, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 9,  Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 10, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 11, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 14, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 16, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 17, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 19, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 20, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 21, Column = 38 },
            });
        }

        [Fact]
        public void TestVirtualIndexerAccessibilityAcrossAssemblies()
        {
            var text1 = @"
public class Base
{
    internal virtual long this[int w, int x, int y, string z] { get { return 0; } set { } }
    internal virtual long this[int w, int x, string y, int z] { get { return 0; } set { } }
    internal virtual long this[int w, int x, string y, string z] { get { return 0; } set { } }

    protected virtual long this[int w, string x, int y, int z] { get { return 0; } set { } }
    protected virtual long this[int w, string x, int y, string z] { get { return 0; } set { } }
    protected virtual long this[int w, string x, string y, int z] { get { return 0; } set { } }

    protected internal virtual long this[int w, string x, string y, string z] { get { return 0; } set { } }
    protected internal virtual long this[string w, int x, int y, int z] { get { return 0; } set { } }
    protected internal virtual long this[string w, int x, int y, string z] { get { return 0; } set { } }
    protected internal virtual long this[string w, int x, string y, int z] { get { return 0; } set { } }

    public virtual long this[string w, int x, string y, string z] { get { return 0; } set { } }
    public virtual long this[string w, string x, int y, int z] { get { return 0; } set { } }
    public virtual long this[string w, string x, int y, string z] { get { return 0; } set { } }
}
";

            var text2 = @"
public class Derived2 : Base
{
    //can't find to override
    protected override long this[int w, int x, int y, string z] { get { return 0; } set { } }
    protected internal override long this[int w, int x, string y, int z] { get { return 0; } set { } }
    public override long this[int w, int x, string y, string z] { get { return 0; } set { } }

    internal override long this[int w, string x, int y, int z] { get { return 0; } set { } }
    protected internal override long this[int w, string x, int y, string z] { get { return 0; } set { } }
    public override long this[int w, string x, string y, int z] { get { return 0; } set { } }

    //protected internal in another assembly is protected in this one
    internal override long this[int w, string x, string y, string z] { get { return 0; } set { } }
    protected override long this[string w, int x, int y, int z] { get { return 0; } set { } } //correct
    protected internal override long this[string w, int x, int y, string z] { get { return 0; } set { } }
    public override long this[string w, int x, string y, int z] { get { return 0; } set { } }

    internal override long this[string w, int x, string y, string z] { get { return 0; } set { } }
    protected override long this[string w, string x, int y, int z] { get { return 0; } set { } }
    protected internal override long this[string w, string x, int y, string z] { get { return 0; } set { } }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 5, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 6, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 7, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 9,  Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 10, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 11, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 14, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 16, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 17, Column = 26 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 19, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 20, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 21, Column = 38 },
            });
        }

        [Fact]
        public void TestVirtualEventAccessibilityAcrossAssemblies()
        {
            var text1 = @"
public class Base
{
    internal virtual event System.Action Event1 { add { } remove { } }
    internal virtual event System.Action Event2 { add { } remove { } }
    internal virtual event System.Action Event3 { add { } remove { } }

    protected virtual event System.Action Event4 { add { } remove { } }
    protected virtual event System.Action Event5 { add { } remove { } }
    protected virtual event System.Action Event6 { add { } remove { } }

    protected internal virtual event System.Action Event7 { add { } remove { } }
    protected internal virtual event System.Action Event8 { add { } remove { } }
    protected internal virtual event System.Action Event9 { add { } remove { } }
    protected internal virtual event System.Action Event10 { add { } remove { } }

    public virtual event System.Action Event11 { add { } remove { } }
    public virtual event System.Action Event12 { add { } remove { } }
    public virtual event System.Action Event13 { add { } remove { } }
}
";

            var text2 = @"
public class Derived2 : Base
{
    //can't find to override
    protected override event System.Action Event1 { add { } remove { } }
    protected internal override event System.Action Event2 { add { } remove { } }
    public override event System.Action Event3 { add { } remove { } }

    internal override event System.Action Event4 { add { } remove { } }
    protected internal override event System.Action Event5 { add { } remove { } }
    public override event System.Action Event6 { add { } remove { } }

    //protected internal in another assembly is protected in this one
    internal override event System.Action Event7 { add { } remove { } }
    protected override event System.Action Event8 { add { } remove { } } //correct
    protected internal override event System.Action Event9 { add { } remove { } }
    public override event System.Action Event10 { add { } remove { } }

    internal override event System.Action Event11 { add { } remove { } }
    protected override event System.Action Event12 { add { } remove { } }
    protected internal override event System.Action Event13 { add { } remove { } }
}
";
            CompileAndVerifyDiagnostics(text1, text2, Array.Empty<ErrorDescription>(), new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 5, Column = 44 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 6, Column = 53 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 7, Column = 41 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 9,  Column = 43 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 10, Column = 53 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 11, Column = 41 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 14, Column = 43 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 16, Column = 53 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 17, Column = 41 },

                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 19, Column = 43 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 20, Column = 44 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 21, Column = 53 },
            });
        }

        [Fact]
        public void TestExplicitPropertyChangeAccessors()
        {
            var text = @"
interface Interface
{
    int Property1 { get; set; }
    int Property2 { get; set; }
    int Property3 { get; set; }

    int Property4 { get; }
    int Property5 { get; }
    int Property6 { get; }

    int Property7 { set; }
    int Property8 { set; }
    int Property9 { set; }
}

class Class : Interface
{
    int Interface.Property1 { get { return 1; } }
    int Interface.Property2 { set { } }
    int Interface.Property3 { get { return 1; } set { } }

    int Interface.Property4 { get { return 1; } }
    int Interface.Property5 { set { } }
    int Interface.Property6 { get { return 1; } set { } }
    
    int Interface.Property7 { get { return 1; } }
    int Interface.Property8 { set { } }
    int Interface.Property9 { get { return 1; } set { } }
}
";

            CompileAndVerifyDiagnostics(text, new ErrorDescription[] {
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyMissingAccessor, Line = 19, Column = 19 }, //1
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyMissingAccessor, Line = 20, Column = 19 }, //2

                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyMissingAccessor, Line = 24, Column = 19 }, //4
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyAddingAccessor, Line = 24, Column = 31 }, //4
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyAddingAccessor, Line = 25, Column = 49 }, //5

                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyMissingAccessor, Line = 27, Column = 19 }, //7
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyAddingAccessor, Line = 27, Column = 31 }, //7
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyAddingAccessor, Line = 29, Column = 31 }, //9

                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 17, Column = 15 }, //1
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 17, Column = 15 }, //2
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 17, Column = 15 }, //4
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 17, Column = 15 }, //7
            });
        }

        [Fact]
        public void TestExplicitIndexerChangeAccessors()
        {
            var text = @"
interface Interface
{
    int this[int w, int x, int y, string z] { get; set; }
    int this[int w, int x, string y, int z] { get; set; }
    int this[int w, int x, string y, string z] { get; set; }

    int this[int w, string x, int y, int z] { get; }
    int this[int w, string x, int y, string z] { get; }
    int this[int w, string x, string y, int z] { get; }

    int this[int w, string x, string y, string z] { set; }
    int this[string w, int x, int y, int z] { set; }
    int this[string w, int x, int y, string z] { set; }
}

class Class : Interface
{
    int Interface.this[int w, int x, int y, string z] { get { return 1; } }
    int Interface.this[int w, int x, string y, int z] { set { } }
    int Interface.this[int w, int x, string y, string z] { get { return 1; } set { } }

    int Interface.this[int w, string x, int y, int z] { get { return 1; } }
    int Interface.this[int w, string x, int y, string z] { set { } }
    int Interface.this[int w, string x, string y, int z] { get { return 1; } set { } }
    
    int Interface.this[int w, string x, string y, string z] { get { return 1; } }
    int Interface.this[string w, int x, int y, int z] { set { } }
    int Interface.this[string w, int x, int y, string z] { get { return 1; } set { } }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (19,19): error CS0551: Explicit interface implementation 'Class.Interface.this[int, int, int, string]' is missing accessor 'Interface.this[int, int, int, string].set'
                //     int Interface.this[int w, int x, int y, string z] { get { return 1; } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "this").WithArguments("Class.Interface.this[int, int, int, string]", "Interface.this[int, int, int, string].set").WithLocation(19, 19),
                // (20,19): error CS0551: Explicit interface implementation 'Class.Interface.this[int, int, string, int]' is missing accessor 'Interface.this[int, int, string, int].get'
                //     int Interface.this[int w, int x, string y, int z] { set { } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "this").WithArguments("Class.Interface.this[int, int, string, int]", "Interface.this[int, int, string, int].get").WithLocation(20, 19),
                // (24,19): error CS0551: Explicit interface implementation 'Class.Interface.this[int, string, int, string]' is missing accessor 'Interface.this[int, string, int, string].get'
                //     int Interface.this[int w, string x, int y, string z] { set { } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "this").WithArguments("Class.Interface.this[int, string, int, string]", "Interface.this[int, string, int, string].get").WithLocation(24, 19),
                // (24,60): error CS0550: 'Class.Interface.this[int, string, int, string].set' adds an accessor not found in interface member 'Interface.this[int, string, int, string]'
                //     int Interface.this[int w, string x, int y, string z] { set { } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "set").WithArguments("Class.Interface.this[int, string, int, string].set", "Interface.this[int, string, int, string]").WithLocation(24, 60),
                // (25,78): error CS0550: 'Class.Interface.this[int, string, string, int].set' adds an accessor not found in interface member 'Interface.this[int, string, string, int]'
                //     int Interface.this[int w, string x, string y, int z] { get { return 1; } set { } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "set").WithArguments("Class.Interface.this[int, string, string, int].set", "Interface.this[int, string, string, int]").WithLocation(25, 78),
                // (27,63): error CS0550: 'Class.Interface.this[int, string, string, string].get' adds an accessor not found in interface member 'Interface.this[int, string, string, string]'
                //     int Interface.this[int w, string x, string y, string z] { get { return 1; } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("Class.Interface.this[int, string, string, string].get", "Interface.this[int, string, string, string]").WithLocation(27, 63),
                // (27,19): error CS0551: Explicit interface implementation 'Class.Interface.this[int, string, string, string]' is missing accessor 'Interface.this[int, string, string, string].set'
                //     int Interface.this[int w, string x, string y, string z] { get { return 1; } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "this").WithArguments("Class.Interface.this[int, string, string, string]", "Interface.this[int, string, string, string].set").WithLocation(27, 19),
                // (29,60): error CS0550: 'Class.Interface.this[string, int, int, string].get' adds an accessor not found in interface member 'Interface.this[string, int, int, string]'
                //     int Interface.this[string w, int x, int y, string z] { get { return 1; } set { } }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("Class.Interface.this[string, int, int, string].get", "Interface.this[string, int, int, string]").WithLocation(29, 60),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, int, string, int].get'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, int, string, int].get").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, string, int, string].get'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, string, int, string].get").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, int, int, string].set'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, int, int, string].set").WithLocation(17, 15),
                // (17,15): error CS0535: 'Class' does not implement interface member 'Interface.this[int, string, string, string].set'
                // class Class : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class", "Interface.this[int, string, string, string].set").WithLocation(17, 15));
        }

        [WorkItem(539162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539162")]
        [Fact]
        public void TestAbstractTypeMember()
        {
            var text = @"
abstract partial class ConstantValue
{
  abstract class ConstantValueDiscriminated : ConstantValue
  {
  }
 
  class ConstantValueBad : ConstantValue
  {
  }
}
";

            //no errors
            CompileAndVerifyDiagnostics(text, new ErrorDescription[0]);
        }

        [Fact]
        public void OverridePrivatePropertyAccessor()
        {
            var text = @"
public class Base
{
    public virtual long Property1 { get; private set; }
}
public class Derived1 : Base
{
    public override long Property1 { get; private set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "set").WithArguments("Derived1.Property1.set"));
        }

        [Fact]
        public void OverridePrivateIndexerAccessor()
        {
            var text = @"
public class Base
{
    public virtual long this[int x] { get { return 0; } private set { } }
}
public class Derived1 : Base
{
    public override long this[int x] { get { return 0; } private set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,66): error CS0115: 'Derived1.this[int].set': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "set").WithArguments("Derived1.this[int].set"));
        }

        [WorkItem(540221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540221")]
        [Fact]
        public void AbstractOverrideOnePropertyAccessor()
        {
            var text = @"
public class Base1
{
    public virtual long Property1 { get { return 0; } set { } }
}
abstract public class Base2 : Base1
{
    public abstract override long Property1 { get; }
    void test1()
    {
        Property1 += 1;
    }
}
public class Derived : Base2
{
    public override long Property1 { get { return 1; } set { } }
    void test2()
    {
        base.Property1++;
        base.Property1 = 2;
        long x = base.Property1;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,9): error CS0205: Cannot call an abstract base member: 'Base2.Property1'
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.Property1").WithArguments("Base2.Property1").WithLocation(19, 9),
                // (21,18): error CS0205: Cannot call an abstract base member: 'Base2.Property1'
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.Property1").WithArguments("Base2.Property1").WithLocation(21, 18));
        }

        [Fact]
        public void AbstractOverrideOneIndexerAccessor()
        {
            var text = @"
public class Base1
{
    public virtual long this[long x] { get { return 0; } set { } }
}
abstract public class Base2 : Base1
{
    public abstract override long this[long x] { get; }
    void test1()
    {
        this[0] += 1;
    }
}
public class Derived : Base2
{
    public override long this[long x] { get { return 1; } set { } }
    void test2()
    {
        base[0]++;
        base[0] = 2;
        long x = base[0];
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,9): error CS0205: Cannot call an abstract base member: 'Base2.this[long]'
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base[0]").WithArguments("Base2.this[long]").WithLocation(19, 9),
                // (21,18): error CS0205: Cannot call an abstract base member: 'Base2.this[long]'
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base[0]").WithArguments("Base2.this[long]").WithLocation(21, 18));
        }

        [Fact]
        public void TestHidingErrors()
        {
            // Tests:
            // Hide base virtual member using new
            // By default members should be hidden by signature if new is not specified
            // new should hide by signature

            var text = @"
using System.Collections.Generic;
class Base<T>
{
    public virtual void Method() { }
    public virtual void Method(T x) { }
    public virtual void Method(T x, T y, List<T> a, Dictionary<T, T> b) { }
    public virtual void Method<U>(T x, T y) { }
    public virtual void Method<U>(U x, T y, List<U> a, Dictionary<T, U> b) { }
    public virtual int Property1 { get { return 0; } }
    public virtual int Property2 { get { return 0; } set { } }
    public virtual void Method2() { }
    public virtual void Method3() { }
}
class Derived<U> : Base<U>
{
    public void Method(U x, U y) { }
    public new void Method(U x, U y, List<U> a, Dictionary<U, U> b) { }
    public new void Method<V>(V x, U y, List<V> a, Dictionary<U, V> b) { }
    public void Method<V>(V x, U y, List<V> a, Dictionary<V, U> b) { }
    public new virtual int Property1 { set {  } }
    public new static int Property2 { get; set; }
    public new static void Method(U i) { }
    public new class Method2 { }
    public void Method<A, B>(U x, U y) {  }
    public new int Method3 { get; set; } 
}
class Derived2 : Derived<int>
{
    public override void Method() { }
    public override void Method(int i) { }
    public override void Method(int x, int y, List<int> a, Dictionary<int, int> b) { }
    public override void Method<V>(V x, int y, List<V> a, Dictionary<int, V> b) { }
    public override void Method<U>(int x, int y) { }
    public override int Property1 { get { return 1; } }
    public override int Property2 { get; set; }
    public override void Method2() { }
    public override void Method3() { }
}
class Test
{
    public static void Main()
    {
        Derived2 d2 = new Derived2();
        Derived<int> d = d2;
        Base<int> b = d2;

        b.Method();
        b.Method(1);
        b.Method<int>(1, 1);
        b.Method<int>(1, 1, new List<int>(), new Dictionary<int, int>());
        b.Method(1, 1, new List<int>(), new Dictionary<int, int>());
        b.Method2();
        int x = b.Property1;
        b.Property2 -= 1;
        b.Method3();

        d.Method();
        Derived<int>.Method(1);
        d.Method<int>(1, 1);
        d.Method<long>(1, 1, new List<long>(), new Dictionary<int, long>());
        d.Method<long>(1, 1, new List<long>(), new Dictionary<long, int>());
        d.Method(1, 1, new List<int>(), new Dictionary<int, int>());
        d.Method2();
        d.Method<int, int>(1, 1);
        Derived<int>.Method2 y = new Derived<int>.Method2(); // Both Method2's are visible?
        d.Property1 = 1;
        Derived<int>.Property2 = Derived<int>.Property2;
        d.Method3();
        x = d.Method3;
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (31,26): error CS0506: 'Derived2.Method(int)': cannot override inherited member 'Derived<int>.Method(int)' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "Method").WithArguments("Derived2.Method(int)", "Derived<int>.Method(int)"),
                // (32,26): error CS0506: 'Derived2.Method(int, int, System.Collections.Generic.List<int>, System.Collections.Generic.Dictionary<int, int>)': cannot override inherited member 'Derived<int>.Method(int, int, System.Collections.Generic.List<int>, System.Collections.Generic.Dictionary<int, int>)' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "Method").WithArguments("Derived2.Method(int, int, System.Collections.Generic.List<int>, System.Collections.Generic.Dictionary<int, int>)", "Derived<int>.Method(int, int, System.Collections.Generic.List<int>, System.Collections.Generic.Dictionary<int, int>)"),
                // (33,26): error CS0506: 'Derived2.Method<V>(V, int, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<int, V>)': cannot override inherited member 'Derived<int>.Method<V>(V, int, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<int, V>)' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "Method").WithArguments("Derived2.Method<V>(V, int, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<int, V>)", "Derived<int>.Method<V>(V, int, System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<int, V>)"),
                // (35,37): error CS0545: 'Derived2.Property1.get': cannot override because 'Derived<int>.Property1' does not have an overridable get accessor
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("Derived2.Property1.get", "Derived<int>.Property1"),
                // (36,25): error CS0506: 'Derived2.Property2': cannot override inherited member 'Derived<int>.Property2' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "Property2").WithArguments("Derived2.Property2", "Derived<int>.Property2"),
                // (37,26): error CS0505: 'Derived2.Method2()': cannot override because 'Derived<int>.Method2' is not a function
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Method2").WithArguments("Derived2.Method2()", "Derived<int>.Method2"),
                // (38,26): error CS0505: 'Derived2.Method3()': cannot override because 'Derived<int>.Method3' is not a function
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Method3").WithArguments("Derived2.Method3()", "Derived<int>.Method3"));
        }

        [Fact]
        public void TestOverloadingByRefOut()
        {
            var text = @"
using System;
abstract class Base
{
    public abstract void Method(int x, ref int y, out Exception z);
}
abstract class Base2 : Base
{
    public abstract void Method(int x, out int y, ref Exception z); // No warnings about hiding
}
class Derived2 : Base2
{
    public override void Method(int x, out int y, ref Exception z) { y = 0; }
    public override void Method(int x, ref int y, out Exception z) { z = null; }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (14,26): error CS0663: 'Derived2' cannot define an overloaded method that differs only on parameter modifiers 'ref' and 'out'
                //     public override void Method(int x, ref int y, out Exception z) { z = null; }
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "Method").WithArguments("Derived2", "method", "ref", "out").WithLocation(14, 26));
        }

        [Fact]
        public void TestOverloadingByParams()
        {
            var text = @"
using System;
abstract class Base
{
    public abstract void Method(int x, params Exception[] z);
    public abstract void Method(int x, int[] z);
}
abstract class Base2 : Base
{
    public abstract void Method(int x, Exception[] z);
    public abstract void Method(int x, params int[] z);
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (10,26): error CS0533: 'Base2.Method(int, System.Exception[])' hides inherited abstract member 'Base.Method(int, params System.Exception[])'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Method").WithArguments("Base2.Method(int, System.Exception[])", "Base.Method(int, params System.Exception[])"),
                // (10,26): warning CS0114: 'Base2.Method(int, System.Exception[])' hides inherited member 'Base.Method(int, params System.Exception[])'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method").WithArguments("Base2.Method(int, System.Exception[])", "Base.Method(int, params System.Exception[])"),
                // (11,26): error CS0533: 'Base2.Method(int, params int[])' hides inherited abstract member 'Base.Method(int, int[])'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Method").WithArguments("Base2.Method(int, params int[])", "Base.Method(int, int[])"),
                // (11,26): warning CS0114: 'Base2.Method(int, params int[])' hides inherited member 'Base.Method(int, int[])'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method").WithArguments("Base2.Method(int, params int[])", "Base.Method(int, int[])"));
        }

        [Fact]
        public void TestOverridingOmitLessAccessibleAccessor()
        {
            var text = @"
using System.Collections.Generic;

abstract class Base<T>
{
    public abstract List<T> Property1 { get; internal set; }
    public abstract List<T> Property2 { set; internal get; }
}

abstract class Base2<T> : Base<T>
{
}

class Derived : Base2<int>
{
    public sealed override List<int> Property1 { get { return null; } }
    public sealed override List<int> Property2 { set { } }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base<int>.Property2.get'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base<int>.Property2.get"),
                // (14,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base<int>.Property1.set'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base<int>.Property1.set"));
        }

        [Fact]
        public void TestOverridingOmitInaccessibleAccessorInDifferentAssembly()
        {
            var text1 = @"
using System;
using System.Collections.Generic;

public abstract class Base<T>
{
    public abstract List<T> Property1 { get; internal set; }
    public abstract List<T> Property2 { set; internal get; }
}";
            var comp1 = CreateCompilation(text1);

            var text2 = @"
using System.Collections.Generic;

abstract class Base2<T> : Base<T>
{
}

class Derived : Base2<int>
{
    public sealed override List<int> Property1 { get { return null; } }
    public sealed override List<int> Property2 { set { } }
}";

            CreateCompilation(text2, new[] { new CSharpCompilationReference(comp1) }).VerifyDiagnostics(
                // (10,38): error CS0546: 'Derived.Property1': cannot override because 'Base<int>.Property1' does not have an overridable set accessor
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "Property1").WithArguments("Derived.Property1", "Base<int>.Property1"),
                // (11,38): error CS0545: 'Derived.Property2': cannot override because 'Base<int>.Property2' does not have an overridable get accessor
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "Property2").WithArguments("Derived.Property2", "Base<int>.Property2"),
                // (8,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base<int>.Property1.set'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base<int>.Property1.set"),
                // (8,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base<int>.Property2.get'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base<int>.Property2.get"));
        }

        [Fact]
        public void TestEmitSynthesizedSealedAccessorsInDifferentAssembly()
        {
            var source1 = @"
using System;
using System.Collections.Generic;

public class Base<T>
{
    public virtual List<T> Property1 { get; set; }
    public virtual List<T> Property2 { set { } get { return null; } }
}";
            var compilation1 = CreateCompilation(source1);

            var source2 = @"
using System.Collections.Generic;

class Derived : Base<int>
{
    public sealed override List<int> Property1 { get { return null; } }
    public sealed override List<int> Property2 { set { } }
}
class Derived2 : Derived
{
    public override List<int> Property1 { set { } }
    public override List<int> Property2 { get { return null; } }
}";
            var comp = CreateCompilation(source2, new[] { new CSharpCompilationReference(compilation1) });
            comp.VerifyDiagnostics(
                // (11,31): error CS0239: 'Derived2.Property1': cannot override inherited member 'Derived.Property1' because it is sealed
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "Property1").WithArguments("Derived2.Property1", "Derived.Property1"),
                // (12,31): error CS0239: 'Derived2.Property2': cannot override inherited member 'Derived.Property2' because it is sealed
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "Property2").WithArguments("Derived2.Property2", "Derived.Property2"));
        }

        [Fact]
        public void TestOverrideAndHide()
        {
            // Tests:
            // Sanity check - within the same type declare members that respectively hide and override
            // a base virtual / abstract member

            var source = @"
using System.Collections.Generic;
abstract class Base<T>
{
    public virtual void Method<U>(T x) { }
    public abstract int Property { set; }
}
class Derived : Base<List<int>>
{
    public override void Method<U>(List<int> x) { }
    public new void Method<U>(List<int> x) { }
    public override int Property { set { } }
    public new int Property { set{ } }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (11,21): error CS0111: Type 'Derived' already defines a member called 'Method' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method").WithArguments("Method", "Derived"),
                // (13,20): error CS0102: The type 'Derived' already contains a definition for 'Property'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Property").WithArguments("Derived", "Property"));
        }

        [Fact]
        public void TestHidingByGenericArity()
        {
            // Tests:
            // Hide base virtual / abstract member with a nested type that has same name but different generic arity
            // Member should be available for overriding in further derived type

            var source = @"
using System.Collections.Generic;
class NS1
{
    abstract class Base<T>
    {
        public virtual void Method<U>(T x) { }
        public virtual int Property { set { } }
    }
    class Base2 : Base<List<int>>
    {
        new class Method { } // Warning: new not required
        new class Property { }
    }
    class Derived : Base2
    {
        public override void Method<U>(List<int> x) { }
        public override int Property { set { } }
    }
}
class NS2
{
    abstract class Base<T>
    {
        public virtual void Method<U>(T x) { }
        public virtual int Property { set { } }
    }
    class Base2 : Base<List<int>>
    {
        public class Method { }
        public new class Property { }
    }
    class Derived : Base2
    {
        public override void Method<U>(List<int> x) { }
        public override int Property { set { } } // Error: can't override a type
    }
}
class NS3
{
    abstract class Base<T>
    {
        public virtual void Method<U>(T x) { }
        public virtual int Property { set { } }
    }
    class Base2 : Base<List<int>>
    {
        public class Method<T> { } // Warning: new required
        public new class Property<T> { } // Warning: new not required
    }
    class Derived : Base2
    {
        public override void Method<U>(List<int> x) { } // Error: can't override a type
        public override int Property { set { } }
    }
}
class NS4
{
    abstract class Base<T>
    {
        public virtual void Method<U>(T x) { }
        public virtual int Property { set { } }
    }
    class Base2 : Base<List<int>>
    {
        public class Method<T, U> { }
        public class Property<T, U> { }
    }
    class Derived : Base2
    {
        public override void Method<U>(List<int> x) { }
        public override int Property { set { } }
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (12,19): warning CS0109: The member 'NS1.Base2.Method' does not hide an accessible member. The new keyword is not required.
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("NS1.Base2.Method"),
                // (35,30): error CS0505: 'NS2.Derived.Method<U>(System.Collections.Generic.List<int>)': cannot override because 'NS2.Base2.Method' is not a function
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Method").WithArguments("NS2.Derived.Method<U>(System.Collections.Generic.List<int>)", "NS2.Base2.Method"),
                // (36,29): error CS0544: 'NS2.Derived.Property': cannot override because 'NS2.Base2.Property' is not a property
                Diagnostic(ErrorCode.ERR_CantOverrideNonProperty, "Property").WithArguments("NS2.Derived.Property", "NS2.Base2.Property"),
                // (48,22): warning CS0108: 'NS3.Base2.Method<T>' hides inherited member 'NS3.Base<System.Collections.Generic.List<int>>.Method<U>(System.Collections.Generic.List<int>)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "Method").WithArguments("NS3.Base2.Method<T>", "NS3.Base<System.Collections.Generic.List<int>>.Method<U>(System.Collections.Generic.List<int>)"),
                // (49,26): warning CS0109: The member 'NS3.Base2.Property<T>' does not hide an accessible member. The new keyword is not required.
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Property").WithArguments("NS3.Base2.Property<T>"),
                // (53,30): error CS0505: 'NS3.Derived.Method<U>(System.Collections.Generic.List<int>)': cannot override because 'NS3.Base2.Method<T>' is not a function
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Method").WithArguments("NS3.Derived.Method<U>(System.Collections.Generic.List<int>)", "NS3.Base2.Method<T>"));
        }

        [WorkItem(540348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540348")]
        [Fact]
        public void TestOverridingBrokenTypes()
        {
            var text = @"
using System.Collections.Generic;

partial class NS1
{
    abstract class Base<T>
    {
        public virtual void Method<U>(T x) { }
        public virtual int Property { set { } }
    }
    class Base2 : Base<List<int>>
    {
        public class Method<T> { }
        public new class Property<T> { }
    }
    class Derived : Base2
    {
        public override void Method<U>(List<int> x) { }
        public override int Property { set { } }
    }
}
partial class NS1
{
    abstract class Base<T>
    {
        public virtual void Method<U>(T x) { }
        public virtual int Property { set { } }
    }
    class Base2 : Base<List<int>>
    {
        public class Method<T, U> { }
        public class Property<T, U> { }
    }
    class Derived : Base2
    {
        public override void Method<U>(List<int> x) { }
        public override int Property { set { } }
    }
}";

            // TODO: Dev10 reports fewer cascading errors
            CreateCompilation(text).VerifyDiagnostics(
                // (24,20): error CS0102: The type 'NS1' already contains a definition for 'Base'
                //     abstract class Base<T>
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Base").WithArguments("NS1", "Base"),
                // (29,11): error CS0102: The type 'NS1' already contains a definition for 'Base2'
                //     class Base2 : Base<List<int>>
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Base2").WithArguments("NS1", "Base2"),
                // (34,11): error CS0102: The type 'NS1' already contains a definition for 'Derived'
                //     class Derived : Base2
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Derived").WithArguments("NS1", "Derived"),
                // (36,30): error CS0505: 'NS1.Derived.Method<U>(System.Collections.Generic.List<int>)': cannot override because 'NS1.Base2.Method<T>' is not a function
                //         public override void Method<U>(List<int> x) { }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Method").WithArguments("NS1.Derived.Method<U>(System.Collections.Generic.List<int>)", "NS1.Base2.Method<T>"),
                // (19,29): error CS0462: The inherited members 'NS1.Base<T>.Property' and 'NS1.Base<T>.Property' have the same signature in type 'NS1.Derived', so they cannot be overridden
                //         public override int Property { set { } }
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Property").WithArguments("NS1.Base<T>.Property", "NS1.Base<T>.Property", "NS1.Derived"),
                // (37,29): error CS0462: The inherited members 'NS1.Base<T>.Property' and 'NS1.Base<T>.Property' have the same signature in type 'NS1.Derived', so they cannot be overridden
                //         public override int Property { set { } }
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Property").WithArguments("NS1.Base<T>.Property", "NS1.Base<T>.Property", "NS1.Derived"),
                // (18,30): error CS0505: 'NS1.Derived.Method<U>(System.Collections.Generic.List<int>)': cannot override because 'NS1.Base2.Method<T>' is not a function
                //         public override void Method<U>(List<int> x) { }
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Method").WithArguments("NS1.Derived.Method<U>(System.Collections.Generic.List<int>)", "NS1.Base2.Method<T>"),
                // (36,30): error CS0111: Type 'NS1.Derived' already defines a member called 'Method' with the same parameter types
                //         public override void Method<U>(List<int> x) { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method").WithArguments("Method", "NS1.Derived"),
                // (26,29): error CS0111: Type 'NS1.Base<T>' already defines a member called 'Method' with the same parameter types
                //         public virtual void Method<U>(T x) { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method").WithArguments("Method", "NS1.Base<T>"),
                // (13,22): warning CS0108: 'NS1.Base2.Method<T>' hides inherited member 'NS1.Base<System.Collections.Generic.List<int>>.Method<U>(System.Collections.Generic.List<int>)'. Use the new keyword if hiding was intended.
                //         public class Method<T> { }
                Diagnostic(ErrorCode.WRN_NewRequired, "Method").WithArguments("NS1.Base2.Method<T>", "NS1.Base<System.Collections.Generic.List<int>>.Method<U>(System.Collections.Generic.List<int>)"),
                // (14,26): warning CS0109: The member 'NS1.Base2.Property<T>' does not hide an accessible member. The new keyword is not required.
                //         public new class Property<T> { }
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Property").WithArguments("NS1.Base2.Property<T>")
                );
        }

        [Fact]
        public void TestHidingErrorsForVirtualMembers()
        {
            // Tests:
            // Hide non-existent base virtual member
            // Hide same virtual member more than once
            // Hide virtual member without specifying new
            // Overload virtual member and also specify new

            var text = @"
class Base
{
    internal new virtual void Method() { }
    internal virtual int Property { set { } }
}
partial class Derived : Base
{
    public virtual void Method() { }
    public new virtual int Property { set { } }
}
partial class Derived
{
    internal new virtual int Property { set { } }
    protected new virtual void Method<T>() { }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (4,31): warning CS0109: The member 'Base.Method()' does not hide an accessible member. The new keyword is not required.
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("Base.Method()"),
                // (14,30): error CS0102: The type 'Derived' already contains a definition for 'Property'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Property").WithArguments("Derived", "Property"),
                // (9,25): warning CS0114: 'Derived.Method()' hides inherited member 'Base.Method()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Method").WithArguments("Derived.Method()", "Base.Method()"),
                // (15,32): warning CS0109: The member 'Derived.Method<T>()' does not hide an accessible member. The new keyword is not required.
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Method").WithArguments("Derived.Method<T>()"));
        }

        [Fact]
        public void TestHidingErrorLocations()
        {
            var text = @"
class Base
{
    public virtual void Method() { }
    public virtual int Property { set { } }
    protected class Type { }
    internal int Field = 1;
}

class Derived : Base
{
    public new int MethOd = 2, Method = 3, METhod = 4;
    void Test()
    {
        long x = MethOd = Method = METhod;
    }
    class Base2 : Base
    {
        private long Type = 5, method = 2, Field = 2,
                        field = 8, Property = 3;
        void Test()
        {
            long x = Type = method = Field = field = Property;
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (12,20): warning CS0109: The member 'Derived.MethOd' does not hide an accessible member. The new keyword is not required.
                //     public new int MethOd = 2, Method = 3, METhod = 4;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "MethOd").WithArguments("Derived.MethOd"),
                // (12,44): warning CS0109: The member 'Derived.METhod' does not hide an accessible member. The new keyword is not required.
                //     public new int MethOd = 2, Method = 3, METhod = 4;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "METhod").WithArguments("Derived.METhod"),
                // (19,22): warning CS0108: 'Derived.Base2.Type' hides inherited member 'Base.Type'. Use the new keyword if hiding was intended.
                //         private long Type = 5, method = 2, Field = 2,
                Diagnostic(ErrorCode.WRN_NewRequired, "Type").WithArguments("Derived.Base2.Type", "Base.Type"),
                // (19,44): warning CS0108: 'Derived.Base2.Field' hides inherited member 'Base.Field'. Use the new keyword if hiding was intended.
                //         private long Type = 5, method = 2, Field = 2,
                Diagnostic(ErrorCode.WRN_NewRequired, "Field").WithArguments("Derived.Base2.Field", "Base.Field"),
                // (20,36): warning CS0108: 'Derived.Base2.Property' hides inherited member 'Base.Property'. Use the new keyword if hiding was intended.
                //                         field = 8, Property = 3;
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("Derived.Base2.Property", "Base.Property")
            );
        }

        [Fact]
        public void ImplementInterfaceUsingSealedProperty()
        {
            var text = @"
interface I1
{
    int Bar { get; }
}
class C1
{
    public virtual int Bar { get { return 0;} set { }}
}

class C2 : C1, I1
{
    sealed public override int Bar { get; set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void ImplementInterfaceUsingSealedEvent()
        {
            var text = @"
interface I1
{
    event System.Action E;
}
class C1
{
    public virtual event System.Action E;

    void UseEvent() { E(); }
}

class C2 : C1, I1
{
    public sealed override event System.Action E;

    void UseEvent() { E(); }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void ImplementInterfaceUsingNonVirtualEvent()
        {
            var text = @"
interface I
{
    event System.Action E;
    event System.Action F;
    event System.Action G;
}
class C : I
{
    event System.Action I.E { add { } remove { } }
    public event System.Action F { add { } remove { } }
    public event System.Action G;
}
";

            var compilation = CreateCompilation(text);

            // This also forces computation of IsMetadataVirtual.
            compilation.VerifyDiagnostics(
                // (12,32): warning CS0067: The event 'C.G' is never used
                //     public event System.Action G;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "G").WithArguments("C.G"));

            const int numEvents = 3;

            var @interface = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I");
            var interfaceEvents = new EventSymbol[numEvents];
            interfaceEvents[0] = @interface.GetMember<EventSymbol>("E");
            interfaceEvents[1] = @interface.GetMember<EventSymbol>("F");
            interfaceEvents[2] = @interface.GetMember<EventSymbol>("G");

            var @class = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var classEvents = new EventSymbol[numEvents];
            classEvents[0] = @class.GetEvent("I.E");
            classEvents[1] = @class.GetMember<EventSymbol>("F");
            classEvents[2] = @class.GetMember<EventSymbol>("G");

            for (int i = 0; i < numEvents; i++)
            {
                var classEvent = classEvents[i];
                var interfaceEvent = interfaceEvents[i];

                Assert.Equal(classEvent, @class.FindImplementationForInterfaceMember(interfaceEvent));

                Assert.Equal(classEvent.AddMethod, @class.FindImplementationForInterfaceMember(interfaceEvent.AddMethod));
                Assert.Equal(classEvent.RemoveMethod, @class.FindImplementationForInterfaceMember(interfaceEvent.RemoveMethod));

                Assert.True(classEvent.AddMethod.IsMetadataVirtual());
                Assert.True(classEvent.RemoveMethod.IsMetadataVirtual());
            }
        }

        [Fact]
        public void TestPrivateMemberHidesVirtualMember()
        {
            var text = @"
abstract public class Class1
{
    public virtual void Member1() { }
    abstract class Class2 : Class1
    {
        new private double[] Member1 = new double[] { };
        abstract class Class3 : Class2
        {
            public override void Member1() { base.Member1(); } // Error
        }
    }
    abstract class Class4 : Class2
    {
        public override void Member1() { base.Member1(); } // OK
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_CantOverrideNonFunction, "Member1").WithArguments("Class1.Class2.Class3.Member1()", "Class1.Class2.Member1"));
        }

        [Fact]
        public void ImplicitMultipleInterfaceInGrandChild()
        {
            var text = @"
interface I1
{
    void Bar();
}
interface I2
{
    void Bar();
}
class C1 : I1
{
    public void Bar() { }
}
class C2 : C1, I1, I2
{
    public new void Bar() { }
}
";
            var comp = CreateCompilation(text);
            var c2Type = comp.Assembly.Modules[0].GlobalNamespace.GetTypeMembers("C2").Single();
            comp.VerifyDiagnostics(DiagnosticDescription.None);
            Assert.True(c2Type.Interfaces().All(iface => iface.Name == "I1" || iface.Name == "I2"));
        }
        [WorkItem(540451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540451")]
        [Fact]
        public void TestImplicitImplSignatureMismatches()
        {
            // Tests: 
            // Mismatching ref / out in signature of implemented member

            var source = @"
using System.Collections.Generic;
interface I1<T>
{
    void Method(int a, long b = 2, string c = null, params List<T>[] d);
}

interface I2 : I1<string>
{
    void Method<T>(out int a, ref T[] b, List<T>[] c);
}

class Base
{
    public void Method(int a, long b = 2, string c = null, params List<string>[] d) { }
    // Toggle ref, out - CS0535
    public void Method<U>(ref int a, out U[] b, List<U>[] c) { b = null; }
}

class Derived : Base, I2 // Implicit implementation in base
{
}

class Class : I2 // Implicit implementation
{
    public void Method(int a, long b = 2, string c = null, params List<string>[] d) { }
    // Omit ref, out - CS0535
    public void Method<U>(int a, U[] b, List<U>[] c) { b = null; }
}

class Class2 : I2 // Implicit implementation
{
    // Additional ref - CS0535
    public void Method(ref int a, long b = 3, string c = null, params List<string>[] d) { }
    // Additional out - CS0535
    public void Method<U>(ref int a, out U[] b, out List<U>[] c) { b = null; c = null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (21,7): error CS0535: 'Derived' does not implement interface member 'I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Derived", "I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])"),
                // (24,7): error CS0535: 'Class' does not implement interface member 'I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Class", "I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])"),
                // (31,7): error CS0535: 'Class2' does not implement interface member 'I1<string>.Method(int, long, string, params System.Collections.Generic.List<string>[])'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Class2", "I1<string>.Method(int, long, string, params System.Collections.Generic.List<string>[])"),
                // (31,7): error CS0535: 'Class2' does not implement interface member 'I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Class2", "I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])"));
        }

        [Fact]
        public void TestExplicitImplSignatureMismatches()
        {
            // Tests: 
            // Mismatching ref / out in signature of implemented member

            var source = @"
using System.Collections.Generic;
interface I1<T>
{
    void Method(int a, long b = 2, string c = null, params List<T>[] d);
}

interface I2 : I1<string>
{
    void Method<T>(out int a, ref T[] b, List<T>[] c);
}

class Class1 : I1<string>, I2
{
    void I1<string>.Method(int a, long b = 2, string c = null, params List<string>[] d) { }
    // Toggle ref, out - CS0535
    void I2.Method<U>(ref int a, out U[] b, List<U>[] c) { b = null; }
}

class Class : I2
{
    void I1<string>.Method(int a, long b = 2, string c = null, params List<string>[] d) { }
    // Omit ref, out - CS0535
    void I2.Method<U>(int a, U[] b, List<U>[] c) { b = null; }
}

class Class2 : I2, I1<string>
{
    // Additional ref - CS0535
    void I1<string>.Method(ref int a, long b = 3, string c = null, params List<string>[] d) { }
    // Additional out - CS0535
    void I2.Method<U>(ref int a, out U[] b, out List<U>[] c) { b = null; c = null; }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (24,13): error CS0539: 'Class.Method<U>(int, U[], System.Collections.Generic.List<U>[])' in explicit interface declaration is not a member of interface
                //     void I2.Method<U>(int a, U[] b, List<U>[] c) { b = null; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class.Method<U>(int, U[], System.Collections.Generic.List<U>[])"),
                // (17,13): error CS0539: 'Class1.Method<U>(ref int, out U[], System.Collections.Generic.List<U>[])' in explicit interface declaration is not a member of interface
                //     void I2.Method<U>(ref int a, out U[] b, List<U>[] c) { b = null; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class1.Method<U>(ref int, out U[], System.Collections.Generic.List<U>[])"),
                // (20,15): error CS0535: 'Class' does not implement interface member 'I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])'
                // class Class : I2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Class", "I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])"),
                // (13,28): error CS0535: 'Class1' does not implement interface member 'I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])'
                // class Class1 : I1<string>, I2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Class1", "I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])"),
                // (22,40): warning CS1066: The default value specified for parameter 'b' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b = 2, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "b").WithArguments("b"),
                // (15,40): warning CS1066: The default value specified for parameter 'b' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b = 2, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "b").WithArguments("b"),
                // (22,54): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b = 2, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
                // (15,54): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b = 2, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
                // (32,13): error CS0539: 'Class2.Method<U>(ref int, out U[], out System.Collections.Generic.List<U>[])' in explicit interface declaration is not a member of interface
                //     void I2.Method<U>(ref int a, out U[] b, out List<U>[] c) { b = null; c = null; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class2.Method<U>(ref int, out U[], out System.Collections.Generic.List<U>[])"),
                // (30,21): error CS0539: 'Class2.Method(ref int, long, string, params System.Collections.Generic.List<string>[])' in explicit interface declaration is not a member of interface
                //     void I1<string>.Method(ref int a, long b = 3, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class2.Method(ref int, long, string, params System.Collections.Generic.List<string>[])"),
                // (27,16): error CS0535: 'Class2' does not implement interface member 'I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])'
                // class Class2 : I2, I1<string>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Class2", "I2.Method<T>(out int, ref T[], System.Collections.Generic.List<T>[])"),
                // (27,20): error CS0535: 'Class2' does not implement interface member 'I1<string>.Method(int, long, string, params System.Collections.Generic.List<string>[])'
                // class Class2 : I2, I1<string>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1<string>").WithArguments("Class2", "I1<string>.Method(int, long, string, params System.Collections.Generic.List<string>[])"),
                // (30,44): warning CS1066: The default value specified for parameter 'b' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(ref int a, long b = 3, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "b").WithArguments("b"),
                // (30,58): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(ref int a, long b = 3, string c = null, params List<string>[] d) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"));
        }

        [Fact]
        public void TestImplicitImplSignatureMismatches2()
        {
            // Tests:
            // Change return type of implemented member
            // Change parameter types of implemented member
            // Change number / order of generic method type parameters in implemented method

            //UNDONE: type constraint mismatch

            var text = @"
interface Interface
{
    void Method<T>(long l, int i);
}

interface Interface2
{
    void Method<T, U, V>(T l, U i, V z);
}

interface Interface3
{
    int Property {set;}
}

class Class1 : Interface
{
    public void Method<T, U>(long l, int i) { } //wrong arity
}

class Base2
{
    public void Method(long l, int i) { } //wrong arity
}
class Class2 : Base2, Interface { }

class Base3
{
    public void Method<V, T, U>(T l, U i, V z) { } //wrong order
}
class Base31 : Base3 { }
class Class3 : Base31, Interface2 { }

class Class4 : Interface
{
    public int Method<T>(long l, int i) { return 0; } //wrong return type
}
class Class41 : Interface3
{
    public long Property { set { } } //wrong return type
}

class Class5 : Interface
{
    public void Method1<T>(long l, int i) { } //wrong name
}

class Class6 : Interface
{
    public void Method<T>(long l) { } //wrong parameter count
}

class Base7
{
    public void Method<T>(int i, long l) { } //wrong parameter types
}
class Class7 : Base7, Interface
{
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (17,16): error CS0535: 'Class1' does not implement interface member 'Interface.Method<T>(long, int)'
                // class Class1 : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class1", "Interface.Method<T>(long, int)").WithLocation(17, 16),
                // (26,23): error CS0535: 'Class2' does not implement interface member 'Interface.Method<T>(long, int)'
                // class Class2 : Base2, Interface { }
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class2", "Interface.Method<T>(long, int)").WithLocation(26, 23),
                // (33,24): error CS0535: 'Class3' does not implement interface member 'Interface2.Method<T, U, V>(T, U, V)'
                // class Class3 : Base31, Interface2 { }
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface2").WithArguments("Class3", "Interface2.Method<T, U, V>(T, U, V)").WithLocation(33, 24),
                // (58,23): error CS0535: 'Class7' does not implement interface member 'Interface.Method<T>(long, int)'
                // class Class7 : Base7, Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class7", "Interface.Method<T>(long, int)").WithLocation(58, 23),
                // (49,16): error CS0535: 'Class6' does not implement interface member 'Interface.Method<T>(long, int)'
                // class Class6 : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class6", "Interface.Method<T>(long, int)").WithLocation(49, 16),
                // (35,16): error CS0738: 'Class4' does not implement interface member 'Interface.Method<T>(long, int)'. 'Class4.Method<T>(long, int)' cannot implement 'Interface.Method<T>(long, int)' because it does not have the matching return type of 'void'.
                // class Class4 : Interface
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface").WithArguments("Class4", "Interface.Method<T>(long, int)", "Class4.Method<T>(long, int)", "void").WithLocation(35, 16),
                // (39,17): error CS0738: 'Class41' does not implement interface member 'Interface3.Property'. 'Class41.Property' cannot implement 'Interface3.Property' because it does not have the matching return type of 'int'.
                // class Class41 : Interface3
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Interface3").WithArguments("Class41", "Interface3.Property", "Class41.Property", "int").WithLocation(39, 17),
                // (44,16): error CS0535: 'Class5' does not implement interface member 'Interface.Method<T>(long, int)'
                // class Class5 : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class5", "Interface.Method<T>(long, int)").WithLocation(44, 16));
        }

        [WorkItem(540470, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540470")]
        [Fact]
        public void TestExplicitImplSignatureMismatches2()
        {
            // Tests:
            // Change return type of implemented member
            // Change parameter types of implemented member
            // Change number / order of generic method type parameters in implemented method

            //UNDONE: type constraint mismatch

            var text = @"
interface Interface
{
    void Method<T>(long l, int i);
}

interface Interface2
{
    void Method<T, U, V>(T l, U i, V z);
}

interface Interface3
{
    int Property {set;}
}

class Class1 : Interface
{
    void Interface.Method<T, U>(long l, int i) { } //wrong arity
}

class Class2 : Interface
{
    void Interface.Method(long l, int i) { } //wrong arity
}

class Class3 : Interface2
{
    void Interface2.Method<V, T, U>(T l, U i, V z) { } //wrong order
}

class Class4 : Interface
{
    int Interface.Method<T>(long l, int i) { return 0; } //wrong return type
}
class Class41 : Interface3
{
    long Interface3.Property { set { } } //wrong return type
}

class Class5 : Interface
{
    void Interface.Method1<T>(long l, int i) { } //wrong name
}

class Class51 : Interface
{
    void INterface.Method<T>(long l, int i) { } //wrong name
}

class Class52 : Interface, Interface2
{
    void Interface.Method<T, U, V>(T l, U i, V z) { } //wrong name
}

class Class6 : Interface
{
    void Interface.Method<T>(long l) { } //wrong parameter count
}

class Class7 : Interface
{
    void Interface.Method<T>(int i, long l) { } //wrong parameter types
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "INterface").WithArguments("INterface"),
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "INterface").WithArguments("INterface"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class1.Method<T, U>(long, int)"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class4.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class51", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class1", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class4", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property").WithArguments("Class41.Property"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class2.Method(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class2", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface3").WithArguments("Class41", "Interface3.Property"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class52.Method<T, U, V>(T, U, V)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class52", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface2").WithArguments("Class52", "Interface2.Method<T, U, V>(T, U, V)"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class7.Method<T>(int, long)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class7", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method1").WithArguments("Class5.Method1<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class3.Method<V, T, U>(T, U, V)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class5", "Interface.Method<T>(long, int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface2").WithArguments("Class3", "Interface2.Method<T, U, V>(T, U, V)"),
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Class6.Method<T>(long)"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Class6", "Interface.Method<T>(long, int)"));
        }

        [Fact]
        public void TestDuplicateImplicitImpl()
        {
            // Tests:
            // Implement same interface member more than once in different parts of a partial type

            var text = @"
using System.Collections.Generic;
interface I1
{
    int Property { set; }
}
abstract partial class Class : I2, I1
{
    abstract public int Property { set; }
    abstract public void Method<U>(int a, ref U[] b, out List<U> c);
}
interface I2 : I1
{
    void Method<T>(int a, ref T[] b, out List<T> c);
}
abstract partial class Class : I3
{
    abstract public int Property { get; set; }
    abstract public void Method<T>(int a, ref T[] b, out List<T> c);
    abstract public void Method(int a = 3, params System.Exception[] b);
}
abstract partial class Base
{
    abstract public int Property { set; }
    abstract public void Method<U>(int a, ref U[] b, out List<U> c);
}
interface I3 : I2
{
    void Method(int a = 3, params System.Exception[] b);
}
abstract partial class Base
{
    abstract public int Property { get; set; }
    abstract public void Method<T>(int a, ref T[] b, out List<T> c);
    abstract public void Method(int a = 3, params System.Exception[] b);
}
abstract class Derived : Base, I3, I1
{
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (18,25): error CS0102: The type 'Class' already contains a definition for 'Property'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Property").WithArguments("Class", "Property"),
                // (19,26): error CS0111: Type 'Class' already defines a member called 'Method' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method").WithArguments("Method", "Class"),
                // (33,25): error CS0102: The type 'Base' already contains a definition for 'Property'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Property").WithArguments("Base", "Property"),
                // (34,26): error CS0111: Type 'Base' already defines a member called 'Method' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method").WithArguments("Method", "Base"));
        }

        [Fact]
        public void TestDuplicateExplicitImpl()
        {
            // Tests:
            // Implement same interface member more than once in different parts of a partial type

            var text = @"
using System.Collections.Generic;
using Type = System.Int32;
interface I1
{
    int Property { set; }
}
abstract partial class Class : I2, I1
{
    int I1.Property { set { } }
    void I2.Method<U>(int a, ref U[] b, out List<U> c) { c = null;  }
}
interface I3 : I2
{
    void Method(int a = 3, params System.Exception[] b);
}
interface I2 : I1
{
    void Method<T>(int a, ref T[] b, out List<T> c);
}
abstract partial class Class : I3
{
    Type I1.Property { set { } }
    void I2.Method<T>(int a, ref T[] b, out List<T> c) { c = null; }
    void I3.Method(int a = 3, params System.Exception[] b) { }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,24): error CS8646: 'I2.Method<T>(int, ref T[], out List<T>)' is explicitly implemented more than once.
                // abstract partial class Class : I2, I1
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "Class").WithArguments("I2.Method<T>(int, ref T[], out System.Collections.Generic.List<T>)").WithLocation(8, 24),
                // (8,24): error CS8646: 'I1.Property' is explicitly implemented more than once.
                // abstract partial class Class : I2, I1
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "Class").WithArguments("I1.Property").WithLocation(8, 24),
                // (25,24): warning CS1066: The default value specified for parameter 'a' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I3.Method(int a = 3, params System.Exception[] b) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "a").WithArguments("a"),
                // (23,13): error CS0102: The type 'Class' already contains a definition for 'I1.Property'
                //     Type I1.Property { set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Property").WithArguments("Class", "I1.Property"),
                // (24,13): error CS0111: Type 'Class' already defines a member called 'I2.Method' with the same parameter types
                //     void I2.Method<T>(int a, ref T[] b, out List<T> c) { c = null; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Method").WithArguments("I2.Method", "Class"));
        }

        [Fact]
        public void TestMissingImpl()
        {
            // Tests:
            // For partial interfaces – test that compiler generates error if any interface methods have not been implemented
            // Test that compiler generates error if any interface methods have not been implemented in an abstract class

            var text = @"
using System.Collections.Generic;
partial interface I1
{
    int Property { set; }
}
abstract partial class Class : I1
{
    int I1.Property { set { } }
    abstract public void Method<U>(int a, ref U[] b, out List<U> c);
}
partial interface I1
{
    void Method<T>(int a, ref T[] b, out List<T> c);
}
abstract partial class Class : I1
{
    void Method(int a = 3, params System.ArgumentException[] b) { } // incorrect parameter type
}
abstract class Base
{
    abstract public void Method(int a = 3, params System.Exception[] b);
    long Property { set { } } // incorrect return type
}
abstract class Base2 : Base
{
}
partial interface I1
{
    void Method(int a = 3, params System.Exception[] b);
}
abstract class Derived : Base2, I1
{
    void I1.Method<T>(int a, ref T[] b, out List<T> c) { c = null; }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (7,32): error CS0535: 'Class' does not implement interface member 'I1.Method(int, params System.Exception[])'
                // abstract partial class Class : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Class", "I1.Method(int, params System.Exception[])").WithLocation(7, 32),
                // (32,33): error CS0737: 'Derived' does not implement interface member 'I1.Property'. 'Base.Property' cannot implement an interface member because it is not public.
                // abstract class Derived : Base2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "I1").WithArguments("Derived", "I1.Property", "Base.Property").WithLocation(32, 33));
        }

        [Fact]
        public void TestInterfaceBaseAccessError()
        {
            // Tests:
            // Invoke base.InterfaceMember from within class that only inherits an interface

            var text = @"
using System.Collections.Generic;
partial interface I1
{
    int Property { set; }
    void Method<T>(int a, ref T[] b, out List<T> c);
    void Method(int a = 3, params System.Exception[] b);
}
class Class1 : I1
{
    public int Property { set { base.Property = value; } }
    public void Method<T>(int a, ref T[] b, out List<T> c) { c = null; base.Method<T>(a, b, c); }
    public void Method(int a = 3, params System.Exception[] b) { base.Method(a, b); }
}
class Class2 : I1
{
    int I1.Property { set { base.Property = value; } }
    void I1.Method<T>(int a, ref T[] b, out List<T> c) { c = null; base.Method<T>(a, b, c); }
    void I1.Method(int a = 3, params System.Exception[] b) { base.Method(a, b); }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (19,24): warning CS1066: The default value specified for parameter 'a' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1.Method(int a = 3, params System.Exception[] b) { base.Method(a, b); }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "a").WithArguments("a"),
                // (11,38): error CS0117: 'object' does not contain a definition for 'Property'
                //     public int Property { set { base.Property = value; } }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property"),
                // (12,77): error CS0117: 'object' does not contain a definition for 'Method'
                //     public void Method<T>(int a, ref T[] b, out List<T> c) { c = null; base.Method<T>(a, b, c); }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Method<T>").WithArguments("object", "Method"),
                // (13,71): error CS0117: 'object' does not contain a definition for 'Method'
                //     public void Method(int a = 3, params System.Exception[] b) { base.Method(a, b); }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method"),
                // (17,34): error CS0117: 'object' does not contain a definition for 'Property'
                //     int I1.Property { set { base.Property = value; } }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("object", "Property"),
                // (18,73): error CS0117: 'object' does not contain a definition for 'Method'
                //     void I1.Method<T>(int a, ref T[] b, out List<T> c) { c = null; base.Method<T>(a, b, c); }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Method<T>").WithArguments("object", "Method"),
                // (19,67): error CS0117: 'object' does not contain a definition for 'Method'
                //     void I1.Method(int a = 3, params System.Exception[] b) { base.Method(a, b); }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Method").WithArguments("object", "Method"));
        }

        [Fact]
        public void TestErrorsImplementingGenericNestedInterfaces_Implicit()
        {
            // Tests:
            // In signature / name of implicitly implemented member, use generic type whose open type (C<T>) matches signature
            // in base interface - but the closed type (C<string> / C<U>) does not match

            var text = @"
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            T Property { set; }
            void Method<K>(T a, U[] b, List<V> c, Interface<W, K> d);
        }

        internal class Base<X, Y>
        {
            public Y Property
            {
                set { }
            }
            public void Method<V>(X A, int[] b, List<long> C, Outer<Y>.Inner<int>.Interface<Y, V> d)
            {
            }
        }
    }
}
internal class Derived1<U, T> : Outer<U>.Inner<T>.Base<U, T>, Outer<U>.Inner<int>.Interface<long, T>
{
    public class Derived2<T> : Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<long, List<int>>
    {
        public List<List<uint>> Property
        {
            get { return null; }
            set { }
        }
        public void Method<K>(List<List<int>> A, List<List<T>>[] B, List<long> C, Outer<List<List<long>>>.Inner<List<List<T>>>.Interface<List<int>, K> D)
        {
        }
        public void Method<T>(List<List<int>> A, List<List<T>>[] B, List<long> C, Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<List<int>, T> D)
        {
        }
    }
}
public class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (25,63): error CS0535: 'Derived1<U, T>' does not implement interface member 'Outer<U>.Inner<int>.Interface<long, T>.Method<K>(U, int[], System.Collections.Generic.List<long>, Outer<U>.Inner<int>.Interface<T, K>)'
                // internal class Derived1<U, T> : Outer<U>.Inner<T>.Base<U, T>, Outer<U>.Inner<int>.Interface<long, T>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<U>.Inner<int>.Interface<long, T>").WithArguments("Derived1<U, T>", "Outer<U>.Inner<int>.Interface<long, T>.Method<K>(U, int[], System.Collections.Generic.List<long>, Outer<U>.Inner<int>.Interface<T, K>)").WithLocation(25, 63),
                // (25,63): error CS0738: 'Derived1<U, T>' does not implement interface member 'Outer<U>.Inner<int>.Interface<long, T>.Property'. 'Outer<U>.Inner<T>.Base<U, T>.Property' cannot implement 'Outer<U>.Inner<int>.Interface<long, T>.Property' because it does not have the matching return type of 'U'.
                // internal class Derived1<U, T> : Outer<U>.Inner<T>.Base<U, T>, Outer<U>.Inner<int>.Interface<long, T>
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Outer<U>.Inner<int>.Interface<long, T>").WithArguments("Derived1<U, T>", "Outer<U>.Inner<int>.Interface<long, T>.Property", "Outer<U>.Inner<T>.Base<U, T>.Property", "U").WithLocation(25, 63),
                // (27,27): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Derived1<U, T>'
                //     public class Derived2<T> : Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<long, List<int>>
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Derived1<U, T>").WithLocation(27, 27),
                // (37,28): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Derived1<U, T>'
                //         public void Method<T>(List<List<int>> A, List<List<T>>[] B, List<long> C, Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<List<int>, T> D)
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Derived1<U, T>").WithLocation(37, 28),
                // (27,32): error CS0535: 'Derived1<U, T>.Derived2<T>' does not implement interface member 'Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<long, System.Collections.Generic.List<int>>.Method<K>(System.Collections.Generic.List<System.Collections.Generic.List<int>>, System.Collections.Generic.List<System.Collections.Generic.List<T>>[], System.Collections.Generic.List<long>, Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<System.Collections.Generic.List<int>, K>)'
                //     public class Derived2<T> : Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<long, List<int>>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<long, List<int>>").WithArguments("Derived1<U, T>.Derived2<T>", "Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<long, System.Collections.Generic.List<int>>.Method<K>(System.Collections.Generic.List<System.Collections.Generic.List<int>>, System.Collections.Generic.List<System.Collections.Generic.List<T>>[], System.Collections.Generic.List<long>, Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<System.Collections.Generic.List<int>, K>)").WithLocation(27, 32),
                // (27,32): error CS0738: 'Derived1<U, T>.Derived2<T>' does not implement interface member 'Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<long, System.Collections.Generic.List<int>>.Property'. 'Derived1<U, T>.Derived2<T>.Property' cannot implement 'Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<long, System.Collections.Generic.List<int>>.Property' because it does not have the matching return type of 'System.Collections.Generic.List<System.Collections.Generic.List<int>>'.
                //     public class Derived2<T> : Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<long, List<int>>
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "Outer<List<List<int>>>.Inner<List<List<T>>>.Interface<long, List<int>>").WithArguments("Derived1<U, T>.Derived2<T>", "Outer<System.Collections.Generic.List<System.Collections.Generic.List<int>>>.Inner<System.Collections.Generic.List<System.Collections.Generic.List<T>>>.Interface<long, System.Collections.Generic.List<int>>.Property", "Derived1<U, T>.Derived2<T>.Property", "System.Collections.Generic.List<System.Collections.Generic.List<int>>").WithLocation(27, 32));
        }

        [Fact]
        public void TestErrorsImplementingGenericNestedInterfaces_Explicit()
        {
            // Tests:
            // In signature / name of explicitly implemented member, use generic type whose open type (C<T>) matches signature
            // in base interface - but the closed type (C<string> / C<U>) does not match

            var text = @"
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            T Property { set; }
            void Method<Z>(T a, U[] b, List<V> c, Dictionary<W, Z> d);
        }
        internal class Derived1 : Inner<int>.Interface<ulong, string>
        {
            T Outer<T>.Inner<int>.Interface<long, string>.Property
            {
                set { }
            }
            void Inner<int>.Interface<long, string>.Method<K>(T A, int[] B, List<long> c, Dictionary<string, K> D)
            {
            }
            internal class Derived2<X, Y> : Outer<Y>.Inner<int>.Interface<long, X>
            {
                X Outer<X>.Inner<int>.Interface<long, Y>.Property
                {
                    set { }
                }
                void Outer<X>.Inner<int>.Interface<long, Y>.Method<K>(X A, int[] b, List<long> C, Dictionary<Y, K> d)
                {
                }
            }
        }
        internal class Derived3 : Interface<long, string>
        {
            U Inner<U>.Interface<long, string>.Property
            {
                set { }
            }
            void Outer<T>.Inner<U>.Interface<long, string>.Method<K>(T a, K[] B, List<long> C, Dictionary<string, K> d)
            {
            }
        }
        internal class Derived4 : Outer<U>.Inner<T>.Interface<T, U>
        {
            U Outer<U>.Inner<T>.Interface<T, U>.Property
            {
                set { }
            }
            void Outer<U>.Inner<T>.Interface<T, U>.Method<K>(U a, T[] b, List<U> C, Dictionary<U, K> d)
            {
            }
            internal class Derived5 : Outer<T>.Inner<U>.Interface<U, T>
            {
                T Outer<T>.Inner<U>.Interface<U, T>.Property
                {
                    set { }
                }
                void Inner<U>.Interface<U, T>.Method<K>(T a, U[] b, List<U> c, Dictionary<K, T> D)
                {
                }
                internal class Derived6<@u> : Outer<List<T>>.Inner<U>.Interface<List<u>, T>
                {
                    List<T> Outer<List<T>>.Inner<U>.Interface<List<U>, T>.Property
                    {
                        set { }
                    }
                    void Outer<List<T>>.Inner<U>.Interface<List<U>, T>.Method<K>(List<T> AA, U[] b, List<List<U>> c, Dictionary<T, K> d)
                    {
                    }
                }
                internal class Derived7<@u> : Outer<List<T>>.Inner<U>.Interface<List<U>, T>
                {
                    List<u> Outer<List<T>>.Inner<U>.Interface<List<U>, T>.Property
                    {
                        set { }
                    }
                    void Outer<List<T>>.Inner<U>.Interface<List<U>, T>.Method<K>(List<T> AA, U[] b, List<List<u>> c, Dictionary<T, K> d)
                    {
                    }
                }
            }
        }
    }
}
class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (48,52): error CS0539: 'Outer<T>.Inner<U>.Derived4.Method<K>(U, T[], System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, K>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived4.Method<K>(U, T[], System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<U, K>)"),
                // (42,35): error CS0535: 'Outer<T>.Inner<U>.Derived4' does not implement interface member 'Outer<U>.Inner<T>.Interface<T, U>.Method<Z>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<U>.Inner<T>.Interface<T, U>").WithArguments("Outer<T>.Inner<U>.Derived4", "Outer<U>.Inner<T>.Interface<T, U>.Method<Z>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, Z>)"),
                // (57,47): error CS0539: 'Outer<T>.Inner<U>.Derived4.Derived5.Method<K>(T, U[], System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<K, T>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Method<K>(T, U[], System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<K, T>)"),
                // (51,39): error CS0535: 'Outer<T>.Inner<U>.Derived4.Derived5' does not implement interface member 'Outer<T>.Inner<U>.Interface<U, T>.Method<Z>(T, U[], System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<T, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<T>.Inner<U>.Interface<U, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5", "Outer<T>.Inner<U>.Interface<U, T>.Method<Z>(T, U[], System.Collections.Generic.List<U>, System.Collections.Generic.Dictionary<T, Z>)"),
                // (72,75): error CS0539: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>.Property' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>.Property"),
                // (76,72): error CS0539: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>.Method<K>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<u>>, System.Collections.Generic.Dictionary<T, K>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>.Method<K>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<u>>, System.Collections.Generic.Dictionary<T, K>)"),
                // (70,46): error CS0535: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>' does not implement interface member 'Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Method<Z>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<U>>, System.Collections.Generic.Dictionary<T, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<List<T>>.Inner<U>.Interface<List<U>, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Method<Z>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<U>>, System.Collections.Generic.Dictionary<T, Z>)"),
                // (70,46): error CS0535: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>' does not implement interface member 'Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<List<T>>.Inner<U>.Interface<List<U>, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived7<u>", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Property"),
                // (62,29): error CS0540: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>.Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Property': containing type does not implement interface 'Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Outer<List<T>>.Inner<U>.Interface<List<U>, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>.Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Property", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>"),
                // (66,26): error CS0540: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>.Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Method<K>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<U>>, System.Collections.Generic.Dictionary<T, K>)': containing type does not implement interface 'Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Outer<List<T>>.Inner<U>.Interface<List<U>, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>.Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>.Method<K>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<U>>, System.Collections.Generic.Dictionary<T, K>)", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>, T>"),
                // (60,46): error CS0535: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>' does not implement interface member 'Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<u>, T>.Method<Z>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<u>>, System.Collections.Generic.Dictionary<T, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<List<T>>.Inner<U>.Interface<List<u>, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<u>, T>.Method<Z>(System.Collections.Generic.List<T>, U[], System.Collections.Generic.List<System.Collections.Generic.List<u>>, System.Collections.Generic.Dictionary<T, Z>)"),
                // (60,46): error CS0535: 'Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>' does not implement interface member 'Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<u>, T>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<List<T>>.Inner<U>.Interface<List<u>, T>").WithArguments("Outer<T>.Inner<U>.Derived4.Derived5.Derived6<u>", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<u>, T>.Property"),
                // (14,15): error CS0540: 'Outer<T>.Inner<U>.Derived1.Outer<T>.Inner<int>.Interface<long, string>.Property': containing type does not implement interface 'Outer<T>.Inner<int>.Interface<long, string>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Outer<T>.Inner<int>.Interface<long, string>").WithArguments("Outer<T>.Inner<U>.Derived1.Outer<T>.Inner<int>.Interface<long, string>.Property", "Outer<T>.Inner<int>.Interface<long, string>"),
                // (18,18): error CS0540: 'Outer<T>.Inner<U>.Derived1.Outer<T>.Inner<int>.Interface<long, string>.Method<K>(T, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<string, K>)': containing type does not implement interface 'Outer<T>.Inner<int>.Interface<long, string>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Inner<int>.Interface<long, string>").WithArguments("Outer<T>.Inner<U>.Derived1.Outer<T>.Inner<int>.Interface<long, string>.Method<K>(T, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<string, K>)", "Outer<T>.Inner<int>.Interface<long, string>"),
                // (12,35): error CS0535: 'Outer<T>.Inner<U>.Derived1' does not implement interface member 'Outer<T>.Inner<int>.Interface<ulong, string>.Method<Z>(T, int[], System.Collections.Generic.List<ulong>, System.Collections.Generic.Dictionary<string, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Inner<int>.Interface<ulong, string>").WithArguments("Outer<T>.Inner<U>.Derived1", "Outer<T>.Inner<int>.Interface<ulong, string>.Method<Z>(T, int[], System.Collections.Generic.List<ulong>, System.Collections.Generic.Dictionary<string, Z>)"),
                // (12,35): error CS0535: 'Outer<T>.Inner<U>.Derived1' does not implement interface member 'Outer<T>.Inner<int>.Interface<ulong, string>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Inner<int>.Interface<ulong, string>").WithArguments("Outer<T>.Inner<U>.Derived1", "Outer<T>.Inner<int>.Interface<ulong, string>.Property"),
                // (23,19): error CS0540: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Outer<X>.Inner<int>.Interface<long, Y>.Property': containing type does not implement interface 'Outer<X>.Inner<int>.Interface<long, Y>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Outer<X>.Inner<int>.Interface<long, Y>.Property", "Outer<X>.Inner<int>.Interface<long, Y>"),
                // (27,22): error CS0540: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Outer<X>.Inner<int>.Interface<long, Y>.Method<K>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, K>)': containing type does not implement interface 'Outer<X>.Inner<int>.Interface<long, Y>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Outer<X>.Inner<int>.Interface<long, Y>.Method<K>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, K>)", "Outer<X>.Inner<int>.Interface<long, Y>"),
                // (21,45): error CS0535: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>' does not implement interface member 'Outer<Y>.Inner<int>.Interface<long, X>.Method<Z>(Y, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<X, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<Y>.Inner<int>.Interface<long, X>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>", "Outer<Y>.Inner<int>.Interface<long, X>.Method<Z>(Y, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<X, Z>)"),
                // (21,45): error CS0535: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>' does not implement interface member 'Outer<Y>.Inner<int>.Interface<long, X>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<Y>.Inner<int>.Interface<long, X>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>", "Outer<Y>.Inner<int>.Interface<long, X>.Property"),
                // (34,48): error CS0539: 'Outer<T>.Inner<U>.Derived3.Property' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property").WithArguments("Outer<T>.Inner<U>.Derived3.Property"),
                // (38,60): error CS0539: 'Outer<T>.Inner<U>.Derived3.Method<K>(T, K[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<string, K>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived3.Method<K>(T, K[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<string, K>)"),
                // (32,35): error CS0535: 'Outer<T>.Inner<U>.Derived3' does not implement interface member 'Outer<T>.Inner<U>.Interface<long, string>.Method<Z>(T, U[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<string, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface<long, string>").WithArguments("Outer<T>.Inner<U>.Derived3", "Outer<T>.Inner<U>.Interface<long, string>.Method<Z>(T, U[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<string, Z>)"),
                // (32,35): error CS0535: 'Outer<T>.Inner<U>.Derived3' does not implement interface member 'Outer<T>.Inner<U>.Interface<long, string>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface<long, string>").WithArguments("Outer<T>.Inner<U>.Derived3", "Outer<T>.Inner<U>.Interface<long, string>.Property"));
        }

        [Fact]
        public void TestErrorsImplementingGenericNestedInterfaces_Explicit_HideTypeParameter()
        {
            // Tests:
            // In signature / name of explicitly implemented member, use generic type whose open type (C<T>) matches signature
            // in base interface - but the closed type (C<string> / C<U>) does not match

            var source = @"
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            void Method<X>(T a, U[] b, List<V> c, Dictionary<W, X> d);
            void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
        }
        internal class Derived1<X, Y> : Outer<X>.Inner<int>.Interface<long, Y>
        {
            void Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X A, int[] b, List<long> C, Dictionary<Y, X> d)
            {
            }
            void Outer<X>.Inner<int>.Interface<long, Y>.Method<X, Y>(X A, int[] b, List<X> C, Dictionary<Y, Y> d)
            {
            }
        }
    }
}
class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (14,64): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                //             void Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X A, int[] b, List<long> C, Dictionary<Y, X> d)
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (17,64): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                //             void Outer<X>.Inner<int>.Interface<long, Y>.Method<X, Y>(X A, int[] b, List<X> C, Dictionary<Y, Y> d)
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (17,67): warning CS0693: Type parameter 'Y' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                //             void Outer<X>.Inner<int>.Interface<long, Y>.Method<X, Y>(X A, int[] b, List<X> C, Dictionary<Y, Y> d)
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "Y").WithArguments("Y", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (10,25): warning CS0693: Type parameter 'V' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                //             void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "V").WithArguments("V", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (10,28): warning CS0693: Type parameter 'W' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                //             void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "W").WithArguments("W", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (17,57): error CS0539: 'Outer<T>.Inner<U>.Derived1<X, Y>.Method<X, Y>(X, int[], System.Collections.Generic.List<X>, System.Collections.Generic.Dictionary<Y, Y>)' in explicit interface declaration is not a member of interface
                //             void Outer<X>.Inner<int>.Interface<long, Y>.Method<X, Y>(X A, int[] b, List<X> C, Dictionary<Y, Y> d)
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>.Method<X, Y>(X, int[], System.Collections.Generic.List<X>, System.Collections.Generic.Dictionary<Y, Y>)"),
                // (14,57): error CS0539: 'Outer<T>.Inner<U>.Derived1<X, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)' in explicit interface declaration is not a member of interface
                //             void Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X A, int[] b, List<long> C, Dictionary<Y, X> d)
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)"),
                // (12,41): error CS0535: 'Outer<T>.Inner<U>.Derived1<X, Y>' does not implement interface member 'Outer<X>.Inner<int>.Interface<long, Y>.Method<V, W>(X, int[], System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<W, W>)'
                //         internal class Derived1<X, Y> : Outer<X>.Inner<int>.Interface<long, Y>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>", "Outer<X>.Inner<int>.Interface<long, Y>.Method<V, W>(X, int[], System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<W, W>)"),
                // (12,41): error CS0535: 'Outer<T>.Inner<U>.Derived1<X, Y>' does not implement interface member 'Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)'
                //         internal class Derived1<X, Y> : Outer<X>.Inner<int>.Interface<long, Y>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>", "Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)"));
        }

        [Fact]
        public void TestErrorsImplementingGenericNestedInterfaces_Implicit_HideTypeParameter()
        {
            // Tests:
            // In signature / name of explicitly implemented member, use generic type whose open type (C<T>) matches signature
            // in base interface - but the closed type (C<string> / C<U>) does not match

            var source = @"
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            void Method<X>(T a, U[] b, List<V> c, Dictionary<W, X> d);
            void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
        }
        internal class Derived1<X, Y> : Outer<X>.Inner<int>.Interface<long, Y>
        {
            void Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X A, int[] b, List<long> C, Dictionary<Y, X> d)
            {
            }
            void Outer<X>.Inner<int>.Interface<long, Y>.Method<X, Y>(X A, int[] b, List<X> C, Dictionary<Y, Y> d)
            {
            }
        }
    }
}
class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (14,64): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (17,64): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (17,67): warning CS0693: Type parameter 'Y' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "Y").WithArguments("Y", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (10,25): warning CS0693: Type parameter 'V' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "V").WithArguments("V", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (10,28): warning CS0693: Type parameter 'W' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "W").WithArguments("W", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (17,57): error CS0539: 'Outer<T>.Inner<U>.Derived1<X, Y>.Method<X, Y>(X, int[], System.Collections.Generic.List<X>, System.Collections.Generic.Dictionary<Y, Y>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>.Method<X, Y>(X, int[], System.Collections.Generic.List<X>, System.Collections.Generic.Dictionary<Y, Y>)"),
                // (14,57): error CS0539: 'Outer<T>.Inner<U>.Derived1<X, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)"),
                // (12,41): error CS0535: 'Outer<T>.Inner<U>.Derived1<X, Y>' does not implement interface member 'Outer<X>.Inner<int>.Interface<long, Y>.Method<V, W>(X, int[], System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<W, W>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>", "Outer<X>.Inner<int>.Interface<long, Y>.Method<V, W>(X, int[], System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<W, W>)"),
                // (12,41): error CS0535: 'Outer<T>.Inner<U>.Derived1<X, Y>' does not implement interface member 'Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>", "Outer<X>.Inner<int>.Interface<long, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)"));
        }

        [Fact]
        public void TestErrorsOverridingGenericNestedClasses_HideTypeParameter()
        {
            // Tests:
            // In signature / name of overridden member, use generic type whose open type (C<T>) matches signature
            // in base class - but the closed type (C<string> / C<U>) does not match

            var source = @"
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal abstract class Base<V, W>
        {
            internal virtual void Method<X>(T a, U[] b, List<V> c, Dictionary<W, X> d) { }
            internal abstract void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
        }
        internal class Derived1<X, Y> : Outer<X>.Inner<int>.Base<long, Y>
        {
            internal override void Method<X>(X A, int[] b, List<long> C, Dictionary<Y, X> d)
            {
            }
            internal override void Method<X, Y>(X A, int[] b, List<X> C, Dictionary<Y, Y> d)
            {
            }
        }
    }
}
class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (10,43): warning CS0693: Type parameter 'V' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Base<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "V").WithArguments("V", "Outer<T>.Inner<U>.Base<V, W>"),
                // (10,46): warning CS0693: Type parameter 'W' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Base<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "W").WithArguments("W", "Outer<T>.Inner<U>.Base<V, W>"),
                // (14,43): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (17,43): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (17,46): warning CS0693: Type parameter 'Y' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "Y").WithArguments("Y", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (14,36): error CS0115: 'Outer<T>.Inner<U>.Derived1<X, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>.Method<X>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, X>)"),
                // (17,36): error CS0115: 'Outer<T>.Inner<U>.Derived1<X, Y>.Method<X, Y>(X, int[], System.Collections.Generic.List<X>, System.Collections.Generic.Dictionary<Y, Y>)': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>.Method<X, Y>(X, int[], System.Collections.Generic.List<X>, System.Collections.Generic.Dictionary<Y, Y>)"),
                // (12,24): error CS0534: 'Outer<T>.Inner<U>.Derived1<X, Y>' does not implement inherited abstract member 'Outer<X>.Inner<int>.Base<long, Y>.Method<V, W>(X, int[], System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<W, W>)'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived1").WithArguments("Outer<T>.Inner<U>.Derived1<X, Y>", "Outer<X>.Inner<int>.Base<long, Y>.Method<V, W>(X, int[], System.Collections.Generic.List<V>, System.Collections.Generic.Dictionary<W, W>)"));
        }

        [Fact]
        public void TestErrorsImplementingGenericNestedInterfaces_Explicit_IncorrectPartialQualification()
        {
            // Tests:
            // In name of explicitly implemented member specify incorrect partially qualified type name

            var source = @"
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            T Property { set; }
            void Method<Z>(T a, U[] b, List<V> c, Dictionary<W, Z> d);
        }
        internal class Derived1 : Inner<int>.Interface<long, string>
        {
            T Interface<long, string>.Property
            {
                set { }
            }
            void Inner<int>.Interface<long, string>.Method<K>(T A, int[] B, List<long> c, Dictionary<string, K> D)
            {
            }
            internal class Derived2<X, Y> : Outer<X>.Inner<int>.Interface<long, Y>
            {
                X Inner<int>.Interface<long, Y>.Property
                {
                    set { }
                }
                void Inner<long>.Interface<long, Y>.Method<K>(X A, int[] b, List<long> C, Dictionary<Y, K> d)
                {
                }
            }
        }
        internal class Derived3 : Interface<long, string>
        {
            T Interface<long, string>.Property
            {
                set { }
            }
            void Inner<U>.Interface<long, string>.Method<K>(T a, U[] B, List<long> C, Dictionary<string, K> d)
            {
            }
        }
        internal class Derived4 : Outer<U>.Inner<T>.Interface<T, U>
        {
            U Interface<T, U>.Property
            {
                set { }
            }
            void Inner<T>.Interface<T, U>.Method<K>(U a, T[] b, List<T> C, Dictionary<U, K> d)
            {
            }
        }
    }
}
class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (44,15): error CS0540: 'Outer<T>.Inner<U>.Derived4.Property': containing type does not implement interface 'Outer<T>.Inner<U>.Interface<T, U>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface<T, U>").WithArguments("Outer<T>.Inner<U>.Derived4.Property", "Outer<T>.Inner<U>.Interface<T, U>"),
                // (44,31): error CS0539: 'Outer<T>.Inner<U>.Derived4.Property' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property").WithArguments("Outer<T>.Inner<U>.Derived4.Property"),
                // (48,18): error CS0540: 'Outer<T>.Inner<U>.Derived4.Method<K>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, K>)': containing type does not implement interface 'Outer<T>.Inner<T>.Interface<T, U>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Inner<T>.Interface<T, U>").WithArguments("Outer<T>.Inner<U>.Derived4.Method<K>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, K>)", "Outer<T>.Inner<T>.Interface<T, U>"),
                // (48,43): error CS0539: 'Outer<T>.Inner<U>.Derived4.Method<K>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, K>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived4.Method<K>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, K>)"),
                // (42,35): error CS0535: 'Outer<T>.Inner<U>.Derived4' does not implement interface member 'Outer<U>.Inner<T>.Interface<T, U>.Method<Z>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<U>.Inner<T>.Interface<T, U>").WithArguments("Outer<T>.Inner<U>.Derived4", "Outer<U>.Inner<T>.Interface<T, U>.Method<Z>(U, T[], System.Collections.Generic.List<T>, System.Collections.Generic.Dictionary<U, Z>)"),
                // (42,35): error CS0535: 'Outer<T>.Inner<U>.Derived4' does not implement interface member 'Outer<U>.Inner<T>.Interface<T, U>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<U>.Inner<T>.Interface<T, U>").WithArguments("Outer<T>.Inner<U>.Derived4", "Outer<U>.Inner<T>.Interface<T, U>.Property"),
                // (14,15): error CS0540: 'Outer<T>.Inner<U>.Derived1.Outer<T>.Inner<U>.Interface<long, string>.Property': containing type does not implement interface 'Outer<T>.Inner<U>.Interface<long, string>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Interface<long, string>").WithArguments("Outer<T>.Inner<U>.Derived1.Outer<T>.Inner<U>.Interface<long, string>.Property", "Outer<T>.Inner<U>.Interface<long, string>"),
                // (12,35): error CS0535: 'Outer<T>.Inner<U>.Derived1' does not implement interface member 'Outer<T>.Inner<int>.Interface<long, string>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Inner<int>.Interface<long, string>").WithArguments("Outer<T>.Inner<U>.Derived1", "Outer<T>.Inner<int>.Interface<long, string>.Property"),
                // (23,19): error CS0540: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Property': containing type does not implement interface 'Outer<T>.Inner<int>.Interface<long, Y>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Property", "Outer<T>.Inner<int>.Interface<long, Y>"),
                // (23,49): error CS0539: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Property' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Property").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Property"),
                // (27,22): error CS0540: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Method<K>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, K>)': containing type does not implement interface 'Outer<T>.Inner<long>.Interface<long, Y>'
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "Inner<long>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Method<K>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, K>)", "Outer<T>.Inner<long>.Interface<long, Y>"),
                // (27,53): error CS0539: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Method<K>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, K>)' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Method").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>.Method<K>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, K>)"),
                // (21,45): error CS0535: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>' does not implement interface member 'Outer<X>.Inner<int>.Interface<long, Y>.Method<Z>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, Z>)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>", "Outer<X>.Inner<int>.Interface<long, Y>.Method<Z>(X, int[], System.Collections.Generic.List<long>, System.Collections.Generic.Dictionary<Y, Z>)"),
                // (21,45): error CS0535: 'Outer<T>.Inner<U>.Derived1.Derived2<X, Y>' does not implement interface member 'Outer<X>.Inner<int>.Interface<long, Y>.Property'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Outer<X>.Inner<int>.Interface<long, Y>").WithArguments("Outer<T>.Inner<U>.Derived1.Derived2<X, Y>", "Outer<X>.Inner<int>.Interface<long, Y>.Property"));
        }

        [Fact]
        public void TestImplicitImplementationSubstitutionError()
        {
            // Tests:
            // Implicitly implement interface member in base generic type – the method that implements interface member 
            // should depend on type parameter of base type to satisfy signature (return type / parameter type) equality
            // Test case where substitution is incorrect

            var source = @"
using System.Collections.Generic;
interface Interface
{
    void Method(List<int> x);
    void Method(List<long> z);
}
class Base<T>
{
    public void Method(T x) { }
}
class Base2<T> : Base<T>
{
    public void Method(List<long> x) { }
}
class Derived : Base2<List<uint>>, Interface
{
}
class Test
{
    public static void Main()
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
               // (16,7): error CS0535: 'Derived' does not implement interface member 'Interface.Method(System.Collections.Generic.List<int>)'
               Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Derived", "Interface.Method(System.Collections.Generic.List<int>)"));
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesImplicitly_Errors()
        {
            // Tests:
            // Use a single member to implicitly implement 
            // multiple base interface members that have signatures differing only by params
            // Implicitly implement multiple base interface members that 
            // have signatures differing only by ref/out (some of these will be error cases)

            var text = @"
using System;
interface I1
{
    int P { set; }
    void M1(long x);
    void M2(long x);
    void M3(long x);
    void M4(ref long x);
    void M5(out long x);
    void M6(ref long x);
    void M7(out long x);
    void M8(params long[] x);
    void M9(long[] x);
}
interface I2 : I1
{
}
interface I3 : I2
{
    new long P { set; }
    new int M1(long x); // Return type
    void M2(ref long x); // Add ref
    void M3(out long x); // Add out
    void M4(long x); // Omit ref
    void M5(long x); // Omit out
    void M6(out long x); // Toggle ref to out
    void M7(ref long x); // Toggle out to ref
    new void M8(long[] x); // Omit params
    new void M9(params long[] x); // Add params
}
class Test : I3
{
    public int P { get { return 0; } set { Console.WriteLine(""I1.P""); } }
    // public long P { get { return 0; } set { Console.WriteLine(""I3.P""); } } - Not possible to implement I3.P implicitly
    // public void M1(long x) { Console.WriteLine(""I1.M1""); } - Not possible to implement I1.M1 implicitly
    public int M1(long x) { Console.WriteLine(""I3.M1""); return 0; }
    public void M2(ref long x) { Console.WriteLine(""I3.M2""); }
    public void M2(long x) { Console.WriteLine(""I1.M2""); }
    public void M3(long x) { Console.WriteLine(""I1.M3""); }
    public void M3(out long x) { x = 0; Console.WriteLine(""I3.M3""); }
    public void M4(long x) { Console.WriteLine(""I3.M4""); }
    public void M4(ref long x) { Console.WriteLine(""I1.M4""); }
    public void M5(out long x) { x = 0; Console.WriteLine(""I3.M5""); }
    public void M5(long x) { Console.WriteLine(""I1.M5""); }
    // public void M6(ref long x) { x = 0; Console.WriteLine(""I1.M6""); } - Not possible to implement I1.M6 implicitly
    public void M6(out long x) { x = 0; Console.WriteLine(""I3.M6""); }
    public void M7(ref long x) { Console.WriteLine(""I3.M7""); }
    // public void M7(out long x) { x = 0; Console.WriteLine(""I1.M7""); } - Not possible to implement I1.M7 implicitly
    public void M8(long[] x) { Console.WriteLine(""I3.M8+I1.M9""); } // Implements both I3.M8 and I1.M8
    public void M9(params long[] x) { Console.WriteLine(""I3.M9+I1.M9""); } // Implements both I3.M9 and I1.M9
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (32,14): error CS0738: 'Test' does not implement interface member 'I3.P'. 'Test.P' cannot implement 'I3.P' because it does not have the matching return type of 'long'.
                // class Test : I3
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "I3").WithArguments("Test", "I3.P", "Test.P", "long").WithLocation(32, 14),
                // (32,14): error CS0738: 'Test' does not implement interface member 'I1.M1(long)'. 'Test.M1(long)' cannot implement 'I1.M1(long)' because it does not have the matching return type of 'void'.
                // class Test : I3
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "I3").WithArguments("Test", "I1.M1(long)", "Test.M1(long)", "void").WithLocation(32, 14),
                // (32,14): error CS0535: 'Test' does not implement interface member 'I1.M6(ref long)'
                // class Test : I3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("Test", "I1.M6(ref long)").WithLocation(32, 14),
                // (32,14): error CS0535: 'Test' does not implement interface member 'I1.M7(out long)'
                // class Test : I3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("Test", "I1.M7(out long)").WithLocation(32, 14));
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterface_Errors()
        {
            // Tests:
            // Implicitly / explicitly implement multiple base interface members (that have same signature) with a single member
            // Implicitly / explicitly implement multiple base interface members (that have same signature) with a single member from base class

            var source = @"
using System;
interface I1<T, U>
{
    void Method<V>(T x, Func<U, T, V> v, U z);
    void Method<Z>(U x, Func<T, U, Z> v, T z);
}
class Implicit : I1<int, Int32>
{
    public void Method<V>(int x, Func<int, int, V> v, int z) { }
}
class Base
{
    public void Method<V>(int x, Func<int, int, V> v, int z) { }
}
class Base2 : Base { }
class ImplicitInBase : Base2, I1<int, Int32> { }
class Explicit : I1<int, Int32>
{
    void I1<Int32, Int32>.Method<V>(int x, Func<int, int, V> v, int z) { }
    public void Method<V>(int x, Func<int, int, V> v, int z) { }
}
class Test
{
    public static void Main()
    {
        I1<int, int> i = new Implicit();
        Func<int, int, string> x = null;
        i.Method<string>(1, x, 1);

        i = new ImplicitInBase();
        i.Method<string>(1, x, 1);

        i = new Explicit();
        i.Method<string>(1, x, 1);
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
// (20,27): warning CS0473: Explicit interface implementation 'Explicit.I1<int, int>.Method<V>(int, System.Func<int, int, V>, int)' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
//     void I1<Int32, Int32>.Method<V>(int x, Func<int, int, V> v, int z) { }
Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Explicit.I1<int, int>.Method<V>(int, System.Func<int, int, V>, int)"),
// (29,9): error CS0121: The call is ambiguous between the following methods or properties: 'I1<T, U>.Method<V>(T, System.Func<U, T, V>, U)' and 'I1<T, U>.Method<Z>(U, System.Func<T, U, Z>, T)'
//         i.Method<string>(1, x, 1);
Diagnostic(ErrorCode.ERR_AmbigCall, "Method<string>").WithArguments("I1<T, U>.Method<V>(T, System.Func<U, T, V>, U)", "I1<T, U>.Method<Z>(U, System.Func<T, U, Z>, T)"),
// (32,9): error CS0121: The call is ambiguous between the following methods or properties: 'I1<T, U>.Method<V>(T, System.Func<U, T, V>, U)' and 'I1<T, U>.Method<Z>(U, System.Func<T, U, Z>, T)'
//         i.Method<string>(1, x, 1);
Diagnostic(ErrorCode.ERR_AmbigCall, "Method<string>").WithArguments("I1<T, U>.Method<V>(T, System.Func<U, T, V>, U)", "I1<T, U>.Method<Z>(U, System.Func<T, U, Z>, T)"),
// (35,9): error CS0121: The call is ambiguous between the following methods or properties: 'I1<T, U>.Method<V>(T, System.Func<U, T, V>, U)' and 'I1<T, U>.Method<Z>(U, System.Func<T, U, Z>, T)'
//         i.Method<string>(1, x, 1);
Diagnostic(ErrorCode.ERR_AmbigCall, "Method<string>").WithArguments("I1<T, U>.Method<V>(T, System.Func<U, T, V>, U)", "I1<T, U>.Method<Z>(U, System.Func<T, U, Z>, T)"));
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterface_Errors2()
        {
            var source = @"
using System;
interface I1<T, U>
{
    Action<T> Method(ref T x);
    Action<U> Method(U x); // Omit ref
    void Method(ref Func<U, T> v);
    void Method(out Func<T, U> v); // Toggle ref to out
    void Method(T x, U[] y);
    void Method(U x, params T[] y); // Add params
    long Method(T x, U v, U[] y);
    int Method(U x, T v, params U[] y); // Add params and change return type
}
class Implicit : I1<int, int>
{
    public Action<int> Method(ref int x) { Console.WriteLine(""Method(ref int x)""); return null; }
    public Action<int> Method(int x) { Console.WriteLine(""Method(int x)""); return null; }
    public void Method(ref Func<int, int> v) { Console.WriteLine(""Method(ref Func<int, int> v)""); }
    // We have to implement this explicitly
    void I1<int, int>.Method(out Func<int, int> v) { v = null; Console.WriteLine(""I1<int, int>.Method(out Func<int, int> v)""); }
    public void Method(int x, int[] y) { Console.WriteLine(""Method(int x, int[] y)""); }
    // Implements both params and non-params version
    public long Method(int x, int v, int[] y) { Console.WriteLine(""Method(int x, int v, int[] y)""); return 0; }
    // We have to implement this explicitly
    int I1<int, int>.Method(int x, int v, params int[] y) { Console.WriteLine(""I1<int, int>.Method(int x, int v, params int[] y)""); return 0; }
}
class Test
{
    public static void Main()
    {
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (3,11): error CS0767: "Cannot inherit interface 'I1<int, int>' with the specified type parameters because it causes method 'I1<int, int>.Method(out System.Func<int, int>)' to contain overloads which differ only on ref and out."
                Diagnostic(ErrorCode.ERR_ExplicitImplCollisionOnRefOut, "I1").WithArguments("I1<int, int>", "I1<int, int>.Method(out System.Func<int, int>)"));
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterface_Errors3()
        {
            var source = @"
using System;
interface I1<T, U>
{
    Action<T> Method(ref T x);
    Action<U> Method(U x); // Omit ref
    void Method(ref Func<U, T> v);
    void Method(out Func<T, U> v); // Toggle ref to out
    void Method(T x, U[] y);
    void Method(U x, params T[] y); // Add params
    long Method(T x, Func<T, U> v, U[] y);
    int Method(U x, Func<T, U> v, params U[] y); // Add params and change return type
}
class Base
{
    public Action<int> Method(ref int x) { Console.WriteLine(""Method(ref int x)""); return null; }
    public Action<int> Method(int x) { Console.WriteLine(""Method(int x)""); return null; }
    public void Method(ref Func<int, int> v) { Console.WriteLine(""Method(ref Func<int, int> v)""); }
    public void Method(int x, int[] y) { Console.WriteLine(""Method(int x, int[] y)""); }
    public long Method(int x, Func<int, int> v, int[] y) { Console.WriteLine(""long Method(int x, Func<int, int> v, int[] y)""); return 0; }
}
class ImplicitInBase : Base, I1<int, int>
{
    public void Method(out Func<int, int> v) { v = null; Console.WriteLine(""Method(out Func<int, int> v)""); }
    public int Method(int x, Func<int, int> v, params int[] y) { Console.WriteLine(""int Method(int x, Func<int, int> v, params int[] y)""); return 0; }
}
class Test
{
    public static void Main()
    {
        I1<int, int> i = new ImplicitInBase();
        int x = 1; Func<int, int> y = null;
        i.Method(ref x); i.Method(x); i.Method(ref y); i.Method(out y);
        i.Method(x, new int[] { x, x, x }); i.Method(x, x, x, x);
        i.Method(x, y, new int[] { x, x, x }); i.Method(x, y, x, x, x);
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
// (25,16): warning CS0108: 'ImplicitInBase.Method(int, System.Func<int, int>, params int[])' hides inherited member 'Base.Method(int, System.Func<int, int>, int[])'. Use the new keyword if hiding was intended.
//     public int Method(int x, Func<int, int> v, params int[] y) { Console.WriteLine("int Method(int x, Func<int, int> v, params int[] y)"); return 0; }
Diagnostic(ErrorCode.WRN_NewRequired, "Method").WithArguments("ImplicitInBase.Method(int, System.Func<int, int>, params int[])", "Base.Method(int, System.Func<int, int>, int[])"),
// (34,9): error CS0121: The call is ambiguous between the following methods or properties: 'I1<T, U>.Method(T, U[])' and 'I1<T, U>.Method(U, params T[])'
//         i.Method(x, new int[] { x, x, x }); i.Method(x, x, x, x);
Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("I1<T, U>.Method(T, U[])", "I1<T, U>.Method(U, params T[])"),
// (35,9): error CS0121: The call is ambiguous between the following methods or properties: 'I1<T, U>.Method(T, System.Func<T, U>, U[])' and 'I1<T, U>.Method(U, System.Func<T, U>, params U[])'
//         i.Method(x, y, new int[] { x, x, x }); i.Method(x, y, x, x, x);
Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("I1<T, U>.Method(T, System.Func<T, U>, U[])", "I1<T, U>.Method(U, System.Func<T, U>, params U[])"));
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesFromSameInterface_Errors4()
        {
            var source = @"
using System;
interface I1<T, U>
{
    Action<T> Method(ref T x);
    Action<U> Method(U x); // Omit ref
    void Method(ref Func<U, T> v);
    void Method(out Func<T, U> v); // Toggle ref to out
    void Method(T x, U[] y);
    void Method(U x, params T[] y); // Add params
    long Method(T x, Func<T, U> v, U[] y);
    int Method(U x, Func<T, U> v, params U[] y); // Add params and change return type
}
class Explicit : I1<int, int>
{
    Action<int> I1<int, int>.Method(ref int x) { Console.WriteLine(""I1<int, int>.Method(ref int x)""); return null; }
    Action<int> I1<int, int>.Method(int x) { Console.WriteLine(""I1<int, int>.Method(int x)""); return null; }
    void I1<int, int>.Method(ref Func<int, int> v) { Console.WriteLine(""I1<int, int>.Method(ref Func<int, int> v)""); }
    void I1<int, int>.Method(out Func<int, int> v) { v = null; Console.WriteLine(""I1<int, int>.Method(out Func<int, int> v)""); }
    void I1<int, int>.Method(int x, int[] y) { Console.WriteLine(""I1<int, int>.Method(int x, int[] y)""); }
    // This has to be implicit so as not to clash with the above
    public void Method(int x, params int[] y) { Console.WriteLine(""Method(int x, params int[] y)""); }
    long I1<int, int>.Method(int x, Func<int, int> v, int[] y) { Console.WriteLine(""long I1<int, int>.Method(int x, Func<int, int> v, int[] y)""); return 0; }
    int I1<int, int>.Method(int x, Func<int, int> v, params int[] y) { Console.WriteLine(""int I1<int, int>.Method(int x, Func<int, int> v, params int[] y)""); return 0; }
}
class Test
{
    public static void Main()
    {
        I1<int, int> i = new Explicit();
        int x = 1; Func<int, int> y = null;
        i.Method(ref x); i.Method(x); i.Method(ref y); i.Method(out y);
        i.Method(x, x, x, x); i.Method(x, y, x, x, x);
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (3,11): error CS0767: "Cannot inherit interface 'I1<int, int>' with the specified type parameters because it causes method 'I1<int, int>.Method(ref System.Func<int, int>)' to contain overloads which differ only on ref and out."
                Diagnostic(ErrorCode.ERR_ExplicitImplCollisionOnRefOut, "I1").WithArguments("I1<int, int>", "I1<int, int>.Method(ref System.Func<int, int>)"),
                // (3,11): error CS0767: "Cannot inherit interface 'I1<int, int>' with the specified type parameters because it causes method 'I1<int, int>.Method(out System.Func<int, int>)' to contain overloads which differ only on ref and out."
                Diagnostic(ErrorCode.ERR_ExplicitImplCollisionOnRefOut, "I1").WithArguments("I1<int, int>", "I1<int, int>.Method(out System.Func<int, int>)"),
                // (20,23): warning CS0473: Explicit interface implementation 'Explicit.I1<int, int>.Method(int, int[])' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "Method").WithArguments("Explicit.I1<int, int>.Method(int, int[])"));
        }
        [Fact]

        public void TestErrorsOverridingImplementingMember()
        {
            // Tests:
            // Members that implement interface members are usually marked as virtual sealed -
            // test the errors that are reported when trying to override these implementing members

            var source = @"
interface I
{
    void M();
    int P { set; }
}
class Base : I
{
    public void M() { }
    public int P { set { } }
}
class Derived : Base
{
    public override void M() { }
    public override int P { set { } }
}
class Base2
{
    public void M() { }
    public int P { set { } }
}
class Derived2 : Base2, I
{
}
class Derived3 : Derived2
{
    public override void M() { }
    public override int P { set { } }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (14,26): error CS0506: 'Derived.M()': cannot override inherited member 'Base.M()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M").WithArguments("Derived.M()", "Base.M()"),
                // (15,25): error CS0506: 'Derived.P': cannot override inherited member 'Base.P' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "P").WithArguments("Derived.P", "Base.P"),
                // (27,26): error CS0506: 'Derived3.M()': cannot override inherited member 'Base2.M()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M").WithArguments("Derived3.M()", "Base2.M()"),
                // (28,25): error CS0506: 'Derived3.P': cannot override inherited member 'Base2.P' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "P").WithArguments("Derived3.P", "Base2.P"));
        }

        [Fact]
        public void TestImplementingMethodNamedFinalize()
        {
            var source = @"
interface I
{
    void Finalize();
}
class C1 : I { public void Finalize() { } }
class C2 : I { }
class Test
{
    public static void Main() { I i = new C1(); i.Finalize(); }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (4,10): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     void Finalize();
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize").WithLocation(4, 10),
                // (6,28): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                // class C1 : I { public void Finalize() { } }
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize").WithLocation(6, 28),
                // (7,12): error CS0737: 'C2' does not implement interface member 'I.Finalize()'. 'object.~Object()' cannot implement an interface member because it is not public.
                // class C2 : I { }
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "I").WithArguments("C2", "I.Finalize()", "object.~Object()").WithLocation(7, 12));
        }

        [Fact]
        public void TestImplementingMethodNamedFinalize2()
        {
            var source = @"
interface I
{
    int Finalize();
    void Finalize(int i);
}
class Base 
{
    public void Finalize(int j) { }
}
class Derived : Base, I
{
    public int Finalize() { return 0; }
}
class Test
{
    public static void Main() { I i = new Derived(); i.Finalize(i.Finalize()); }
}";

            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(542361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542361")]
        [Fact]
        public void TestTypeParameterExplicitMethodImplementation()
        {
            var source = @"
interface T
{
    void T<S>();
}
class A<T> : global::T
{
    void T.T<S>() { }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (8,10): error CS0538: 'T' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "T").WithArguments("T"),
                // (6,7): error CS0535: 'A<T>' does not implement interface member 'T.T<S>()'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "global::T").WithArguments("A<T>", "T.T<S>()"));
        }

        [WorkItem(542361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542361")]
        [Fact]
        public void TestTypeParameterExplicitPropertyImplementation()
        {
            var source = @"
interface T
{
    int T { get; set; }
}
class A<T> : global::T
{
    int T.T { get; set; }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (8,9): error CS0538: 'T' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "T").WithArguments("T"),
                // (6,7): error CS0535: 'A<T>' does not implement interface member 'T.T'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "global::T").WithArguments("A<T>", "T.T"));
        }

        [WorkItem(542361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542361")]
        [Fact]
        public void TestTypeParameterExplicitEventImplementation()
        {
            var source = @"
interface T
{
    event System.Action T;
}
class A<T> : global::T
{
    event System.Action T.T { add { } remove { } }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (8,25): error CS0538: 'T' in explicit interface declaration is not an interface
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "T").WithArguments("T"),
                // (6,7): error CS0535: 'A<T>' does not implement interface member 'T.T'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "global::T").WithArguments("A<T>", "T.T"));
        }

        private static CSharpCompilation CompileAndVerifyDiagnostics(string text, ErrorDescription[] expectedErrors, params CSharpCompilation[] baseCompilations)
        {
            var refs = new List<MetadataReference>(baseCompilations.Select(c => new CSharpCompilationReference(c)));
            var comp = CreateCompilation(text, refs);
            var actualErrors = comp.GetDiagnostics();

            //ostensibly, we could just pass exactMatch: true to VerifyErrorCodes, but that method is short-circuited when 0 errors are expected
            Assert.Equal(expectedErrors.Length, actualErrors.Count());
            DiagnosticsUtils.VerifyErrorCodes(actualErrors, expectedErrors);

            return comp;
        }

        private static CSharpCompilation CompileAndVerifyDiagnostics(string text1, string text2, ErrorDescription[] expectedErrors1, ErrorDescription[] expectedErrors2)
        {
            var comp1 = CompileAndVerifyDiagnostics(text1, expectedErrors1);
            var comp2 = CompileAndVerifyDiagnostics(text2, expectedErrors2, comp1);
            return comp2;
        }

        [Fact]
        [WorkItem(1016693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016693")]
        public void Bug1016693()
        {
            const string source = @"
public class A
{
    public virtual int P { get; set; }

    public class B : A
    {
        public override int P { get; set; }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(31974, "https://github.com/dotnet/roslyn/issues/31974")]
        public void Issue31974()
        {
            const string source = @"
namespace Ns1
{
    public interface I1<I1T1>
    {
        void M();
        int P {get; set;}
        event System.Action E;
    }

    public class C0<ST1, ST2>
    { }

    public interface I2<I2T1, I2T2> : I1<C0<I2T1, I2T2>>
    {
    }

    class C1<C1T1, C1T2> : I2<C1T1, C1T2>
    {
        void I1<C0<C1T1, C1T2>>.M()
        {
        }

        void global::Ns1.I1<C0<C1T1, C1T2>>.M()
        {
        }

        int I1<C0<C1T1, C1T2>>.P
        {
            get => throw null;
            set => throw null;
        }

        int global::Ns1.I1<C0<C1T1, C1T2>>.P
        {
            get => throw null;
            set => throw null;
        }

        event System.Action I1<C0<C1T1, C1T2>>.E
        {
            add => throw null;
            remove => throw null;
        }

        event System.Action global::Ns1.I1<C0<C1T1, C1T2>>.E
        {
            add => throw null;
            remove => throw null;
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (18,11): error CS8646: 'I1<C0<C1T1, C1T2>>.P' is explicitly implemented more than once.
                //     class C1<C1T1, C1T2> : I2<C1T1, C1T2>
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C1").WithArguments("Ns1.I1<Ns1.C0<C1T1, C1T2>>.P").WithLocation(18, 11),
                // (18,11): error CS8646: 'I1<C0<C1T1, C1T2>>.E' is explicitly implemented more than once.
                //     class C1<C1T1, C1T2> : I2<C1T1, C1T2>
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C1").WithArguments("Ns1.I1<Ns1.C0<C1T1, C1T2>>.E").WithLocation(18, 11),
                // (18,11): error CS8646: 'I1<C0<C1T1, C1T2>>.M()' is explicitly implemented more than once.
                //     class C1<C1T1, C1T2> : I2<C1T1, C1T2>
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C1").WithArguments("Ns1.I1<Ns1.C0<C1T1, C1T2>>.M()").WithLocation(18, 11)
                );
        }

        [Fact]
        public void DynamicMismatch_01()
        {
            var source = @"
public interface I0<T> { }
public interface I1 : I0<object> { }
public interface I2 : I0<dynamic> { }
public interface I3 : I0<object> { }

public class C : I1, I2 { }
public class D : I1, I3 { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,23): error CS1966: 'I2': cannot implement a dynamic interface 'I0<dynamic>'
                // public interface I2 : I0<dynamic> { }
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I0<dynamic>").WithArguments("I2", "I0<dynamic>").WithLocation(4, 23),
                // (7,14): error CS8779: 'I0<dynamic>' is already listed in the interface list on type 'C' as 'I0<object>'.
                // public class C : I1, I2 { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithDifferencesInBaseList, "C").WithArguments("I0<dynamic>", "I0<object>", "C").WithLocation(7, 14)
                );
        }

        [Fact]
        public void DynamicMismatch_02()
        {
            var source = @"
public interface I0<T> 
{
    void M();
}

public class C : I0<object>
{
    void I0<object>.M(){}
}
public class D : C, I0<dynamic>
{ }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,21): error CS1966: 'D': cannot implement a dynamic interface 'I0<dynamic>'
                // public class D : C, I0<dynamic>
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I0<dynamic>").WithArguments("D", "I0<dynamic>").WithLocation(11, 21),
                // (11,21): error CS0535: 'D' does not implement interface member 'I0<dynamic>.M()'
                // public class D : C, I0<dynamic>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I0<dynamic>").WithArguments("D", "I0<dynamic>.M()").WithLocation(11, 21)
                );
        }

        [Fact]
        public void DynamicMismatch_03()
        {
            var source = @"
public interface I0<T> 
{
    void M();
}

public class C : I0<object>
{
    void I0<object>.M(){}
    public void M(){}
}
public class D : C, I0<dynamic>
{ }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,21): error CS1966: 'D': cannot implement a dynamic interface 'I0<dynamic>'
                // public class D : C, I0<dynamic>
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I0<dynamic>").WithArguments("D", "I0<dynamic>").WithLocation(12, 21),
                // (12,21): error CS0535: 'D' does not implement interface member 'I0<dynamic>.M()'
                // public class D : C, I0<dynamic>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I0<dynamic>").WithArguments("D", "I0<dynamic>.M()").WithLocation(12, 21)
                );
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void ImplementMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter1()
        {
            var source = @"
interface I
{
    void Goo<T>(T? value) where T : struct;
}

class C1 : I
{
    public void Goo<T>(T? value) where T : struct { }
}

class C2 : I
{
    void I.Goo<T>(T? value) { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(c2Goo.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void ImplementMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter2()
        {
            var source = @"
interface I
{
    void Goo<T>(T?[] value) where T : struct;
}

class C1 : I
{
    public void Goo<T>(T?[] value) where T : struct { }
}

class C2 : I
{
    void I.Goo<T>(T?[] value) { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(((ArrayTypeSymbol)c2Goo.Parameters[0].Type).ElementType.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void ImplementMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter3()
        {
            var source = @"
interface I
{
    void Goo<T>((T a, T? b)? value) where T : struct;
}

class C1 : I
{
    public void Goo<T>((T a, T? b)? value) where T : struct { }
}

class C2 : I
{
    void I.Goo<T>((T a, T? b)? value) { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(c2Goo.Parameters[0].Type.IsNullableType());
            var tuple = c2Goo.Parameters[0].Type.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0];
            Assert.False(tuple.TupleElements[0].Type.IsNullableType());
            Assert.True(tuple.TupleElements[1].Type.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void ImplementMethodReturningNullableStructParameter_WithMethodReturningNullableStruct1()
        {
            var source = @"
interface I
{
    T? Goo<T>() where T : struct;
}

class C1 : I
{
    public T? Goo<T>() where T : struct => default;
}

class C2 : I
{
    T? I.Goo<T>() => default;
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(c2Goo.ReturnType.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void ImplementMethodReturningNullableStructParameter_WithMethodReturningNullableStruct2()
        {
            var source = @"
interface I
{
    T?[] Goo<T>() where T : struct;
}

class C1 : I
{
    public T?[] Goo<T>() where T : struct => default;
}

class C2 : I
{
    T?[] I.Goo<T>() => default;
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(((ArrayTypeSymbol)c2Goo.ReturnType).ElementType.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void ImplementMethodReturningNullableStructParameter_WithMethodReturningNullableStruct3()
        {
            var source = @"
interface I
{
    (T a, T? b)? Goo<T>() where T : struct;
}

class C1 : I
{
    public (T a, T? b)? Goo<T>() where T : struct => default;
}

class C2 : I
{
    (T a, T? b)? I.Goo<T>() => default;
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(c2Goo.ReturnType.IsNullableType());
            var tuple = c2Goo.ReturnType.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0];
            Assert.False(tuple.TupleElements[0].Type.IsNullableType());
            Assert.True(tuple.TupleElements[1].Type.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void OverrideMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter1()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T? value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>(T? value) { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(dGoo.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void OverrideMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter2()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T?[] value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>(T?[] value) { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(((ArrayTypeSymbol)dGoo.Parameters[0].Type).ElementType.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void OverrideMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter3()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>((T a, T? b)? value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>((T a, T? b)? value) { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(dGoo.Parameters[0].Type.IsNullableType());
            var tuple = dGoo.Parameters[0].Type.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0];
            Assert.False(tuple.TupleElements[0].Type.IsNullableType());
            Assert.True(tuple.TupleElements[1].Type.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void OverrideMethodReturningNullableStructParameter_WithMethodReturningNullableStruct1()
        {
            var source = @"
abstract class Base
{
    public abstract T? Goo<T>() where T : struct;
}

class Derived : Base
{
    public override T? Goo<T>() => default;
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(dGoo.ReturnType.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void OverrideMethodReturningNullableStructParameter_WithMethodReturningNullableStruct2()
        {
            var source = @"
abstract class Base
{
    public abstract T?[] Goo<T>() where T : struct;
}

class Derived : Base
{
    public override T?[] Goo<T>() => default;
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(((ArrayTypeSymbol)dGoo.ReturnType).ElementType.IsNullableType());
        }

        [Fact]
        [WorkItem(34508, "https://github.com/dotnet/roslyn/issues/34508")]
        public void OverrideMethodReturningNullableStructParameter_WithMethodReturningNullableStruct3()
        {
            var source = @"
abstract class Base
{
    public abstract (T a, T? b)? Goo<T>() where T : struct;
}

class Derived : Base
{
    public override (T a, T? b)? Goo<T>() => default;
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(dGoo.ReturnType.IsNullableType());
            var tuple = dGoo.ReturnType.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0];
            Assert.False(tuple.TupleElements[0].Type.IsNullableType());
            Assert.True(tuple.TupleElements[1].Type.IsNullableType());
        }

        [Fact]
        public void ImplementMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter_WithStructConstraint()
        {
            var source = @"
interface I
{
    void Goo<T>(T? value) where T : struct;
}

class C1 : I
{
    public void Goo<T>(T? value) where T : struct { }
}

class C2 : I
{
    void I.Goo<T>(T? value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c2Goo = (MethodSymbol)comp.GetMember("C2.I.Goo");

            Assert.True(c2Goo.Parameters[0].Type.IsNullableType());

            CreateCompilation(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics();

            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (14,29): error CS8652: The feature 'constraints for override and explicit interface implementation methods' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     void I.Goo<T>(T? value) where T : struct { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "where").WithArguments("constraints for override and explicit interface implementation methods", "8.0").WithLocation(14, 29)
                );
        }

        [Fact]
        public void OverrideMethodTakingNullableStructParameter_WithMethodTakingNullableStructParameter_WithStructConstraint()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T? value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>(T? value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var dGoo = (MethodSymbol)comp.GetMember("Derived.Goo");

            Assert.True(dGoo.Parameters[0].Type.IsNullableType());
        }

        [Fact]
        public void AllowStructConstraintInOverride()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void AllowClassConstraintInOverride()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : class;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();

            CreateCompilation(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics();

            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (9,42): error CS8652: The feature 'constraints for override and explicit interface implementation methods' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public override void Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "where").WithArguments("constraints for override and explicit interface implementation methods", "8.0").WithLocation(9, 42)
                );
        }

        [Fact]
        public void ErrorIfNonExistentTypeParameter_HasStructConstraintInOverride()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value);
}

class Derived : Base
{
    public override void Goo<T>(T value) where U : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,48): error CS0699: 'Derived.Goo<T>(T)' does not define type parameter 'U'
                //     public override void Goo<T>(T value) where U : struct { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "Derived.Goo<T>(T)").WithLocation(9, 48));
        }

        [Fact]
        public void ErrorIfNonExistentTypeParameter_HasClassConstraintInOverride()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value);
}

class Derived : Base
{
    public override void Goo<T>(T value) where U : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,48): error CS0699: 'Derived.Goo<T>(T)' does not define type parameter 'U'
                //     public override void Goo<T>(T value) where U : class { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "Derived.Goo<T>(T)").WithLocation(9, 48));
        }

        [Fact]
        public void ErrorIfTypeParameterDeclaredOutsideMethod_HasStructConstraintInOverride()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value);
}

class Derived<U> : Base
{
    public override void Goo<T>(T value) where U : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,48): error CS0699: 'Derived<U>.Goo<T>(T)' does not define type parameter 'U'
                //     public override void Goo<T>(T value) where U : struct { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "Derived<U>.Goo<T>(T)").WithLocation(9, 48));
        }

        [Fact]
        public void ErrorIfTypeParameterDeclaredOutsideMethod_HasClassConstraintInOverride()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value);
}

class Derived<U> : Base
{
    public override void Goo<T>(T value) where U : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,48): error CS0699: 'Derived<U>.Goo<T>(T)' does not define type parameter 'U'
                //     public override void Goo<T>(T value) where U : class { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "Derived<U>.Goo<T>(T)").WithLocation(9, 48));
        }

        [Fact]
        public void AllowStructConstraintInExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void Goo<T>(T value) where T : struct;
}

class C : I
{
    void I.Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void AllowClassConstraintInExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void Goo<T>(T value) where T : class;
}

class C : I
{
    void I.Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ErrorIfNonExistentTypeParameter_HasStructConstraintInExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void Goo<T>(T value);
}

class C : I
{
    void I.Goo<T>(T value) where U : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,34): error CS0699: 'C.I.Goo<T>(T)' does not define type parameter 'U'
                //     void I.Goo<T>(T value) where U : struct { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "C.I.Goo<T>(T)").WithLocation(9, 34));
        }

        [Fact]
        public void ErrorIfNonExistentTypeParameter_HasClassConstraintInExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void Goo<T>(T value);
}

class C : I
{
    void I.Goo<T>(T value) where U : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,34): error CS0699: 'C.I.Goo<T>(T)' does not define type parameter 'U'
                //     void I.Goo<T>(T value) where U : class { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "C.I.Goo<T>(T)").WithLocation(9, 34));
        }

        [Fact]
        public void ErrorIfTypeParameterDeclaredOutsideMethod_HasStructConstraintInExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void Goo<T>(T value);
}

class C<U> : I
{
    void I.Goo<T>(T value) where U : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,34): error CS0699: 'C<U>.I.Goo<T>(T)' does not define type parameter 'U'
                //     void I.Goo<T>(T value) where U : struct { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "C<U>.I.Goo<T>(T)").WithLocation(9, 34));
        }

        [Fact]
        public void ErrorIfTypeParameterDeclaredOutsideMethod_HasClassConstraintInExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void Goo<T>(T value);
}

class C<U> : I
{
    void I.Goo<T>(T value) where U : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,34): error CS0699: 'C<U>.I.Goo<T>(T)' does not define type parameter 'U'
                //     void I.Goo<T>(T value) where U : class { }
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "C<U>.I.Goo<T>(T)").WithLocation(9, 34));
        }

        [Fact]
        public void ErrorIfNonExistentTypeParameter()
        {
            var source = @"
interface I
{
    void Goo();
}

class C : I
{
    void I.Goo() where U : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,18): error CS0080: Constraints are not allowed on non-generic declarations
                //     void I.Goo() where U : class { }
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(9, 18)
                );
        }

        [Fact]
        public void ErrorIfDuplicateConstraintClause()
        {
            var source = @"
interface I
{
    void Goo<T>(T? value) where T : struct;
}

class C<U> : I
{
    void I.Goo<T>(T? value) where T : struct where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,52): error CS0409: A constraint clause has already been specified for type parameter 'T'. All of the constraints for a type parameter must be specified in a single where clause.
                //     void I.Goo<T>(T? value) where T : struct where T : class { }
                Diagnostic(ErrorCode.ERR_DuplicateConstraintClause, "T").WithArguments("T").WithLocation(9, 52)
                );
        }

        [Fact]
        public void Error_WhenOverride_HasStructAndClassConstraints1()
        {
            var source = @"
abstract class Base
{
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct, class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (8,26): error CS0115: 'Derived.Goo<T>(T)': no suitable method found to override
                //     public override void Goo<T>(T value) where T : struct, class { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Goo").WithArguments("Derived.Goo<T>(T)").WithLocation(8, 26),
                // (8,60): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     public override void Goo<T>(T value) where T : struct, class { }
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(8, 60));
        }

        [Fact]
        public void Error_WhenOverride_HasStructAndClassConstraints2()
        {
            var source = @"
abstract class Base
{
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class, struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (8,26): error CS0115: 'Derived.Goo<T>(T)': no suitable method found to override
                //     public override void Goo<T>(T value) where T : class, struct { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Goo").WithArguments("Derived.Goo<T>(T)").WithLocation(8, 26),
                // (8,59): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     public override void Goo<T>(T value) where T : class, struct { }
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "struct").WithLocation(8, 59));
        }

        [Fact]
        public void Error_WhenOverride_HasStructAndClassConstraints3()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct, class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,60): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     public override void Goo<T>(T value) where T : struct, class { }
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(9, 60));
        }

        [Fact]
        public void Error_WhenExplicitImplementation_HasStructAndClassConstraints1()
        {
            var source = @"
interface I
{
}

class C : I
{
    void I.Goo<T>(T value) where T : struct, class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (8,12): error CS0539: 'C.Goo<T>(T)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I.Goo<T>(T value) where T : struct, class { }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Goo").WithArguments("C.Goo<T>(T)").WithLocation(8, 12),
                // (8,46): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     void I.Goo<T>(T value) where T : struct, class { }
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(8, 46));
        }

        [Fact]
        public void Error_WhenExplicitImplementation_HasStructAndClassConstraints2()
        {
            var source = @"
interface I
{
}

class C : I
{
    void I.Goo<T>(T value) where T : class, struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (8,12): error CS0539: 'C.Goo<T>(T)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I.Goo<T>(T value) where T : class, struct { }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "Goo").WithArguments("C.Goo<T>(T)").WithLocation(8, 12),
                // (8,45): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     void I.Goo<T>(T value) where T : class, struct { }
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "struct").WithLocation(8, 45));
        }

        [Fact]
        public void Error_WhenExplicitImplementation_HasStructAndClassConstraints3()
        {
            var source = @"
interface I
{
    void Goo<T>(T value) where T : struct;
}

class C : I
{
    void I.Goo<T>(T value) where T : struct, class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,46): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //     void I.Goo<T>(T value) where T : struct, class { }
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(9, 46));
        }

        [Fact]
        public void Error_WhenOverride_HasNullableClassConstraint()
        {
            var source = @"
#nullable enable
abstract class Base
{
    public abstract void Goo<T>(T value) where T : class?;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class? { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,52): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : class? { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "class?").WithLocation(10, 52));
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementation_HasNullableClassConstraint()
        {
            var source = @"
#nullable enable
interface I
{
    void Goo<T>(T value) where T : class?;
}

class C : I
{
    void I.Goo<T>(T value) where T : class? { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,38): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void I.Goo<T>(T value) where T : class? { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "class?").WithLocation(10, 38));
        }

        [Fact]
        public void Error_WhenOverride_HasReferenceTypeConstraint1()
        {
            var source = @"
using System.IO;
abstract class Base
{
    public abstract void Goo<T>(T value) where T : Stream;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : Stream { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,52): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : Stream { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "Stream").WithLocation(10, 52));
        }

        [Fact]
        public void Error_WhenOverride_HasReferenceTypeConstraint2()
        {
            var source = @"
using System.IO;
abstract class Base
{
    public abstract void Goo<T>(T value) where T : Stream;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class, Stream { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,59): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : class, Stream { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "Stream").WithLocation(10, 59));
        }

        [Fact]
        public void Error_WhenOverride_HasReferenceTypeConstraint3()
        {
            var source = @"
using System.IO;
abstract class Base
{
    public abstract void Goo<T, U>(T value) where T : class where U : Stream;
}

class Derived : Base
{
    public override void Goo<T, U>(T value) where T : class where U : Stream { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,71): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T, U>(T value) where T : class where U Stream { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "Stream").WithLocation(10, 71));
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementation_HasReferenceTypeConstraint1()
        {
            var source = @"
using System.IO;
interface I
{
    void Goo<T>(T value) where T : Stream;
}

class C : I
{
    void I.Goo<T>(T value) where T : Stream { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,38): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void I.Goo<T>(T value) where T : Stream { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "Stream").WithLocation(10, 38));
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementation_HasReferenceTypeConstraint2()
        {
            var source = @"
using System.IO;
interface I
{
    void Goo<T>(T value) where T : Stream;
}

class C : I
{
    void I.Goo<T>(T value) where T : class, Stream { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,45): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void I.Goo<T>(T value) where T : class, Stream { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "Stream").WithLocation(10, 45));
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementation_HasReferenceTypeConstraint3()
        {
            var source = @"
using System.IO;
interface I
{
    void Goo<T, U>(T value) where T : class where U : Stream;
}

class C : I
{
    void I.Goo<T, U>(T value) where T : class where U : Stream { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,57): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void I.Goo<T, U>(T value) where T : class where U : Stream { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "Stream").WithLocation(10, 57));
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementationHasStructConstraint_AndInterfaceDoesNot1()
        {
            var source = @"
interface I
{
    void Goo<U>(U value);
}

class C : I
{
    void I.Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,16): error CS8666: Method 'C.I.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'U' of overridden or explicitly implemented method 'I.Goo<U>(U)' is not a non-nullable value type.
                //     void I.Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("C.I.Goo<T>(T)", "T", "U", "I.Goo<U>(U)").WithLocation(9, 16)
                );
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementationHasStructConstraint_AndInterfaceDoesNot2()
        {
            var source = @"
interface I
{
    void Goo<T>(T value) where T : class;
}

class C : I
{
    void I.Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,16): error CS8666: Method 'C.I.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'I.Goo<T>(T)' is not a non-nullable value type.
                //     void I.Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("C.I.Goo<T>(T)", "T", "T", "I.Goo<T>(T)").WithLocation(9, 16)
                );
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementationHasStructConstraint_AndInterfaceDoesNot3()
        {
            var source = @"
using System.IO;
interface I
{
    void Goo<T>(T value) where T : Stream;
}

class C : I
{
    void I.Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,16): error CS8666: Method 'C.I.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'I.Goo<T>(T)' is not a non-nullable value type.
                //     void I.Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("C.I.Goo<T>(T)", "T", "T", "I.Goo<T>(T)").WithLocation(10, 16)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasStructConstraint_AndOverriddenDoesNot1()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value);
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,30): error CS8666: Method 'Derived.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a non-nullable value type.
                //     public override void Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(9, 30)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasStructConstraint_AndOverriddenDoesNot2()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : class;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,30): error CS8666: Method 'Derived.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a non-nullable value type.
                //     public override void Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(9, 30)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasStructConstraint_AndOverriddenDoesNot3()
        {
            var source = @"
using System.IO;
abstract class Base
{
    public abstract void Goo<T>(T value) where T : Stream;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,30): error CS8666: Method 'Derived.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a non-nullable value type.
                //     public override void Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(10, 30)
                );
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementationHasClassConstraint_AndInterfaceDoesNot1()
        {
            var source = @"
interface I
{
    void Goo<U>(U value);
}

class C : I
{
    void I.Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,16): error CS8665: Method 'C.I.Goo<T>(T)' specifies a 'class' constraint for type parameter 'T', but corresponding type parameter 'U' of overridden or explicitly implemented method 'I.Goo<U>(U)' is not a reference type.
                //     void I.Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_OverrideRefConstraintNotSatisfied, "T").WithArguments("C.I.Goo<T>(T)", "T", "U", "I.Goo<U>(U)").WithLocation(9, 16)
                );
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementationHasClassConstraint_AndInterfaceDoesNot2()
        {
            var source = @"
interface I
{
    void Goo<T>(T value) where T : struct;
}

class C : I
{
    void I.Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,16): error CS8665: Method 'C.I.Goo<T>(T)' specifies a 'class' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'I.Goo<T>(T)' is not a reference type.
                //     void I.Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_OverrideRefConstraintNotSatisfied, "T").WithArguments("C.I.Goo<T>(T)", "T", "T", "I.Goo<T>(T)").WithLocation(9, 16)
                );
        }

        [Fact]
        public void Error_WhenExplicitInterfaceImplementationHasClassConstraint_AndInterfaceDoesNot3()
        {
            var source = @"
using System;
interface I
{
    void Goo<T>(T value) where T : Enum;
}

class C : I
{
    void I.Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,16): error CS8665: Method 'C.I.Goo<T>(T)' specifies a 'class' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'I.Goo<T>(T)' is not a reference type.
                //     void I.Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_OverrideRefConstraintNotSatisfied, "T").WithArguments("C.I.Goo<T>(T)", "T", "T", "I.Goo<T>(T)").WithLocation(10, 16)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasClassConstraint_AndOverriddenDoesNot1()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value);
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,30): error CS8665: Method 'Derived.Goo<T>(T)' specifies a 'class' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a reference type.
                //     public override void Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_OverrideRefConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(9, 30)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasClassConstraint_AndOverriddenDoesNot2()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : struct;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,30): error CS8665: Method 'Derived.Goo<T>(T)' specifies a 'class' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a reference type.
                //     public override void Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_OverrideRefConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(9, 30)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasClassConstraint_AndOverriddenDoesNot3()
        {
            var source = @"
using System;
abstract class Base
{
    public abstract void Goo<T>(T value) where T : Enum;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,30): error CS8665: Method 'Derived.Goo<T>(T)' specifies a 'class' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a reference type.
                //     public override void Goo<T>(T value) where T : class { }
                Diagnostic(ErrorCode.ERR_OverrideRefConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(10, 30)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasStructConstraint_AndOverriddenHasEnumConstraint()
        {
            var source = @"
using System;
abstract class Base
{
    public abstract void Goo<T>(T value) where T : Enum;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (10,30): error CS8666: Method 'Derived.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base.Goo<T>(T)' is not a non-nullable value type.
                //     public override void Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base.Goo<T>(T)").WithLocation(10, 30)
                );
        }

        [Fact]
        public void Error_WhenOverrideHasStructConstraint_AndOverriddenHasNullableConstraint()
        {
            var source = @"
abstract class Base<U>
{
    public abstract void Goo<T>(T value) where T : U;
}

class Derived : Base<int?>
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,30): error CS8666: Method 'Derived.Goo<T>(T)' specifies a 'struct' constraint for type parameter 'T', but corresponding type parameter 'T' of overridden or explicitly implemented method 'Base<int?>.Goo<T>(T)' is not a non-nullable value type.
                //     public override void Goo<T>(T value) where T : struct { }
                Diagnostic(ErrorCode.ERR_OverrideValConstraintNotSatisfied, "T").WithArguments("Derived.Goo<T>(T)", "T", "T", "Base<int?>.Goo<T>(T)").WithLocation(9, 30)
                );
        }

        [Fact]
        public void NoError_WhenOverrideHasStructConstraint_AndOverriddenHasUnmanagedConstraint()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : unmanaged;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : struct { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void NoError_WhenOverrideHasClassConstraint_AndOverriddenHasReferenceTypeConstraint()
        {
            var source = @"
using System.IO;
abstract class Base
{
    public abstract void Goo<T>(T value) where T : Stream;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void Error_WhenOverride_HasDefaultConstructorConstraint1()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : new();
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : new() { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,52): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : new() { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "new()").WithLocation(9, 52)
                );
        }

        [Fact]
        public void Error_WhenOverride_HasDefaultConstructorConstraint2()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : class, new();
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : class, new() { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,59): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : class, new() { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "new()").WithLocation(9, 59)
                );
        }

        [Fact]
        public void Error_WhenOverride_HasUnmanagedConstraint1()
        {
            var source = @"
abstract class Base
{
    public abstract void Goo<T>(T value) where T : unmanaged;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : unmanaged { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (9,52): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : unmanaged { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "unmanaged").WithLocation(9, 52)
                );
        }

        [Fact]
        public void Error_WhenOverride_HasUnmanagedConstraint2()
        {
            var source = @"
interface I {}

abstract class Base
{
    public abstract void Goo<T>(T value) where T : unmanaged, I;
}

class Derived : Base
{
    public override void Goo<T>(T value) where T : unmanaged, I { }
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (11,52): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     public override void Goo<T>(T value) where T : unmanaged, I { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "unmanaged").WithLocation(11, 52)
                );
        }

        [Fact]
        [WorkItem(34583, "https://github.com/dotnet/roslyn/issues/34583")]
        public void ExplicitImplementationOfNullableStructWithMultipleTypeParameters()
        {
            var source = @"
interface I
{
    void Goo<T, U>(T? value) where T : struct;
}

class C1 : I
{
    public void Goo<T, U>(T? value) where T : struct {}
}

class C2 : I
{
    void I.Goo<T, U>(T? value) {}
}
";
            var comp = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(63490, "https://github.com/dotnet/roslyn/issues/63490")]
        public void MultipleBasesWithObliviousDifferencesAndInterfaces()
        {
            var source1 = @"
#nullable enable
interface ITest
{
    void Test();
}

class Generic<T> { }
class Argument { }
partial class Partial : Generic<Argument> { }
";

            var source2 = @"
#nullable disable
partial class Partial : Generic<Argument>, ITest
{
    void ITest.Test() { }
}
";
            CreateCompilation(source1 + source2).VerifyDiagnostics();
            CreateCompilation(source2 + source1).VerifyDiagnostics();
        }
    }
}
