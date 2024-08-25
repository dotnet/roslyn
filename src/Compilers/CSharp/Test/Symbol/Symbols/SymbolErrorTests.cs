// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class CompilationErrorTests : CompilingTestBase
    {
        #region Symbol Error Tests

        private static readonly ModuleMetadata s_mod1 = ModuleMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestMod01);
        private static readonly ModuleMetadata s_mod2 = ModuleMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestMod02);

        [Fact()]
        public void CS0148ERR_BadDelegateConstructor()
        {
            var il = @"
.class public auto ansi sealed F
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor( //object 'object',
                               native int 'method') runtime managed
  {
  } // end of method F::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke() runtime managed
  {
  } // end of method F::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method F::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method F::EndInvoke

} // end of class F

";
            var source = @"
class C
{
  void Goo()
  {
    F del = Goo;
    del();  //need to use del or the delegate receiver alone is emitted in optimized code. 
  }
}
";

            var comp = CreateCompilationWithILAndMscorlib40(source, il);
            var emitResult = comp.Emit(new System.IO.MemoryStream());
            emitResult.Diagnostics.Verify(Diagnostic(ErrorCode.ERR_BadDelegateConstructor, "Goo").WithArguments("F"));
        }

        /// <summary>
        /// This error is specific to netmodule scenarios
        /// We used to give error CS0011: The base class or interface 'A' in assembly 'xxx' referenced by type 'B' could not be resolved
        /// In Roslyn we do not know the context in which the lookup was occurring, so we give a new, more generic message.
        /// </summary>
        [WorkItem(546451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546451")]
        [Fact()]
        public void CS0011ERR_CantImportBase01()
        {
            var text1 = @"class A {}";
            var text2 = @"class B : A {}";
            var text = @"
class Test  
{    
    B b;    

    void M()
    {
        Test x = b;
    }
}";

            var name1 = GetUniqueName();
            var module1 = CreateCompilation(text1, options: TestOptions.ReleaseModule, assemblyName: name1);

            var module2 = CreateCompilation(text2,
                options: TestOptions.ReleaseModule,
                references: new[] { ModuleMetadata.CreateFromImage(module1.EmitToArray(options: new EmitOptions(metadataOnly: true))).GetReference() });

            // use ref2 only
            var comp = CreateCompilation(text,
                options: TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>() { { MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_UnreferencedField), ReportDiagnostic.Suppress } }),
                references: new[] { ModuleMetadata.CreateFromImage(module2.EmitToArray(options: new EmitOptions(metadataOnly: true))).GetReference() });

            comp.VerifyDiagnostics(
                // error CS8014: Reference to '1b2d660e-e892-4338-a4e7-f78ce7960ce9.netmodule' netmodule missing.
                Diagnostic(ErrorCode.ERR_MissingNetModuleReference).WithArguments(name1 + ".netmodule"),
                // (8,18): error CS7079: The type 'A' is defined in a module that has not been added. You must add the module '2bddf16b-09e6-4c4d-bd08-f348e194eca4.netmodule'.
                //         Test x = b;
                Diagnostic(ErrorCode.ERR_NoTypeDefFromModule, "b").WithArguments("A", name1 + ".netmodule"),
                // (8,18): error CS0029: Cannot implicitly convert type 'B' to 'Test'
                //         Test x = b;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "b").WithArguments("B", "Test"),
                // (4,7): warning CS0649: Field 'Test.b' is never assigned to, and will always have its default value null
                //     B b;    
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "b").WithArguments("Test.b", "null")
            );
        }

        [Fact]
        public void CS0012ERR_NoTypeDef01()
        {
            var text = @"namespace NS
{
    class Test
    {
       TC5<string, string> var; // inherit C1 from MDTestLib1.dll

        void M()
        {
            Test x = var;
        }
    }
}";

            var ref2 = TestReferences.SymbolsTests.MDTestLib2;
            var comp = CreateCompilation(text, references: new MetadataReference[] { ref2 }, assemblyName: "Test3");
            comp.VerifyDiagnostics(
    // (9,22): error CS0012: The type 'C1<>.C2<>' is defined in an assembly that is not referenced. You must add a reference to assembly 'MDTestLib1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    //             Test x = var;
    Diagnostic(ErrorCode.ERR_NoTypeDef, "var").WithArguments("C1<>.C2<>", "MDTestLib1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
    // (9,22): error CS0029: Cannot implicitly convert type 'TC5<string, string>' to 'NS.Test'
    //             Test x = var;
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "var").WithArguments("TC5<string, string>", "NS.Test"),
    // (5,28): warning CS0649: Field 'NS.Test.var' is never assigned to, and will always have its default value null
    //        TC5<string, string> var; // inherit C1 from MDTestLib1.dll
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "var").WithArguments("NS.Test.var", "null")
            );
        }

        [Fact, WorkItem(8574, "DevDiv_Projects/Roslyn")]
        public void CS0029ERR_CannotImplicitlyConvertTypedReferenceToObject()
        {
            var text = @"
class Program
{
    static void M(System.TypedReference r)
    {
        var t = r.GetType();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,17): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'object'
                //         var t = r.GetType();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "r").WithArguments("System.TypedReference", "object"));
        }

        // CS0036: see AttributeTests.InOutAttributes_Errors

        [Fact]
        public void CS0050ERR_BadVisReturnType()
        {
            var text = @"class MyClass 
{
}

public class MyClass2
{
    public static MyClass MyMethod()   // CS0050
    {
        return new MyClass();
    }

    public static void Main() { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisReturnType, Line = 7, Column = 27 });
        }

        [Fact]
        public void CS0051ERR_BadVisParamType01()
        {
            var text = @"public class A
{
    class B
    {
    }

    public static void F(B b)  // CS0051
    {
    }

    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisParamType, Line = 7, Column = 24 });
        }

        [Fact]
        public void CS0051ERR_BadVisParamType02()
        {
            var text = @"class A
{
    protected class P1 { }

    public class N
    {
        public void f(P1 p) { }
        protected void g(P1 p) { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisParamType, Line = 7, Column = 21 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisParamType, Line = 8, Column = 24 });
        }

        [Fact]
        public void CS0051ERR_BadVisParamType03()
        {
            var source =
@"internal class A { }
public class B<T>
{
    public class C<U> { }
}
public class C1
{
    public void M(B<A> arg) { }
}
public class C2
{
    public void M(B<object>.C<A> arg) { }
}
public class C3
{
    public void M(B<A>.C<object> arg) { }
}
public class C4
{
    public void M(B<B<A>>.C<object> arg) { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,17): error CS0051: Inconsistent accessibility: parameter type 'B<A>' is less accessible than method 'C1.M(B<A>)'
                Diagnostic(ErrorCode.ERR_BadVisParamType, "M").WithArguments("C1.M(B<A>)", "B<A>").WithLocation(8, 17),
                // (12,17): error CS0051: Inconsistent accessibility: parameter type 'B<object>.C<A>' is less accessible than method 'C2.M(B<object>.C<A>)'
                Diagnostic(ErrorCode.ERR_BadVisParamType, "M").WithArguments("C2.M(B<object>.C<A>)", "B<object>.C<A>").WithLocation(12, 17),
                // (16,17): error CS0051: Inconsistent accessibility: parameter type 'B<A>.C<object>' is less accessible than method 'C3.M(B<A>.C<object>)'
                Diagnostic(ErrorCode.ERR_BadVisParamType, "M").WithArguments("C3.M(B<A>.C<object>)", "B<A>.C<object>").WithLocation(16, 17),
                // (20,17): error CS0051: Inconsistent accessibility: parameter type 'B<B<A>>.C<object>' is less accessible than method 'C4.M(B<B<A>>.C<object>)'
                Diagnostic(ErrorCode.ERR_BadVisParamType, "M").WithArguments("C4.M(B<B<A>>.C<object>)", "B<B<A>>.C<object>").WithLocation(20, 17));
        }

        [Fact]
        public void CS0052ERR_BadVisFieldType()
        {
            var text = @"public class MyClass2
{
    public MyClass M;   // CS0052
    private class MyClass
    {
    }
}

public class MainClass
{
    public static void Main()
    {
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisFieldType, Line = 3, Column = 20 });
        }

        [Fact]
        public void CS0053ERR_BadVisPropertyType()
        {
            var text =
@"internal interface InternalInterface { }
public class PublicClass { }
public class A
{
    protected struct ProtectedStruct { }
    private class PrivateClass { }
    public PublicClass P { get; set; }
    public InternalInterface Q { get; set; }
    public ProtectedStruct R { get; set; }
    public PrivateClass S { get; set; }
    internal PublicClass T { get; set; }
    internal InternalInterface U { get; set; }
    internal ProtectedStruct V { get; set; }
    internal PrivateClass W { get; set; }
    protected class B
    {
        public PublicClass P { get; set; }
        public InternalInterface Q { get; set; }
        public ProtectedStruct R { get; set; }
        public PrivateClass S { get; set; }
        internal PublicClass T { get; set; }
        internal InternalInterface U { get; set; }
        internal ProtectedStruct V { get; set; }
        internal PrivateClass W { get; set; }
    }
}
internal class C
{
    protected struct ProtectedStruct { }
    private class PrivateClass { }
    public PublicClass P { get; set; }
    public InternalInterface Q { get; set; }
    public ProtectedStruct R { get; set; }
    public PrivateClass S { get; set; }
    internal PublicClass T { get; set; }
    internal InternalInterface U { get; set; }
    internal ProtectedStruct V { get; set; }
    internal PrivateClass W { get; set; }
    protected class D
    {
        public PublicClass P { get; set; }
        public InternalInterface Q { get; set; }
        public ProtectedStruct R { get; set; }
        public PrivateClass S { get; set; }
        internal PublicClass T { get; set; }
        internal InternalInterface U { get; set; }
        internal ProtectedStruct V { get; set; }
        internal PrivateClass W { get; set; }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,30): error CS0053: Inconsistent accessibility: property return type 'InternalInterface' is less accessible than property 'A.Q'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "Q").WithArguments("A.Q", "InternalInterface").WithLocation(8, 30),
                // (9,28): error CS0053: Inconsistent accessibility: property return type 'A.ProtectedStruct' is less accessible than property 'A.R'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "R").WithArguments("A.R", "A.ProtectedStruct").WithLocation(9, 28),
                // (10,25): error CS0053: Inconsistent accessibility: property return type 'A.PrivateClass' is less accessible than property 'A.S'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "S").WithArguments("A.S", "A.PrivateClass").WithLocation(10, 25),
                // (13,30): error CS0053: Inconsistent accessibility: property return type 'A.ProtectedStruct' is less accessible than property 'A.V'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "V").WithArguments("A.V", "A.ProtectedStruct").WithLocation(13, 30),
                // (14,27): error CS0053: Inconsistent accessibility: property return type 'A.PrivateClass' is less accessible than property 'A.W'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "W").WithArguments("A.W", "A.PrivateClass").WithLocation(14, 27),
                // (18,34): error CS0053: Inconsistent accessibility: property return type 'InternalInterface' is less accessible than property 'A.B.Q'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "Q").WithArguments("A.B.Q", "InternalInterface").WithLocation(18, 34),
                // (20,29): error CS0053: Inconsistent accessibility: property return type 'A.PrivateClass' is less accessible than property 'A.B.S'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "S").WithArguments("A.B.S", "A.PrivateClass").WithLocation(20, 29),
                // (24,31): error CS0053: Inconsistent accessibility: property return type 'A.PrivateClass' is less accessible than property 'A.B.W'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "W").WithArguments("A.B.W", "A.PrivateClass").WithLocation(24, 31),
                // (33,28): error CS0053: Inconsistent accessibility: property return type 'C.ProtectedStruct' is less accessible than property 'C.R'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "R").WithArguments("C.R", "C.ProtectedStruct").WithLocation(33, 28),
                // (34,24): error CS0053: Inconsistent accessibility: property return type 'C.PrivateClass' is less accessible than property 'C.S'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "S").WithArguments("C.S", "C.PrivateClass").WithLocation(34, 25),
                // (370): error CS0053: Inconsistent accessibility: property return type 'C.ProtectedStruct' is less accessible than property 'C.V'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "V").WithArguments("C.V", "C.ProtectedStruct").WithLocation(37, 30),
                // (38,27): error CS0053: Inconsistent accessibility: property return type 'C.PrivateClass' is less accessible than property 'C.W'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "W").WithArguments("C.W", "C.PrivateClass").WithLocation(38, 27),
                // (44,29): error CS0053: Inconsistent accessibility: property return type 'C.PrivateClass' is less accessible than property 'C.D.S'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "S").WithArguments("C.D.S", "C.PrivateClass").WithLocation(44, 29),
                // (48,31): error CS0053: Inconsistent accessibility: property return type 'C.PrivateClass' is less accessible than property 'C.D.W'
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "W").WithArguments("C.D.W", "C.PrivateClass").WithLocation(48, 31));
        }

        [ClrOnlyFact]
        public void CS0054ERR_BadVisIndexerReturn()
        {
            var text =
@"internal interface InternalInterface { }
public class PublicClass { }
public class A
{
    protected struct ProtectedStruct { }
    private class PrivateClass { }
    public PublicClass this[int i] { get { return null; } }
    public InternalInterface this[object o] { get { return null; } }
    public ProtectedStruct this[string s] { set { } }
    public PrivateClass this[double d] { get { return null; } }
    internal PublicClass this[int x, int y] { get { return null; } }
    internal InternalInterface this[object x, object y] { get { return null; } }
    internal ProtectedStruct this[string x, string y] { set { } }
    internal PrivateClass this[double x, double y] { get { return null; } }
    protected class B
    {
        public PublicClass this[int i] { get { return null; } }
        public InternalInterface this[object o] { get { return null; } }
        public ProtectedStruct this[string s] { set { } }
        public PrivateClass this[double d] { get { return null; } }
        internal PublicClass this[int x, int y] { get { return null; } }
        internal InternalInterface this[object x, object y] { get { return null; } }
        internal ProtectedStruct this[string x, string y] { set { } }
        internal PrivateClass this[double x, double y] { get { return null; } }
    }
}
internal class C
{
    protected struct ProtectedStruct { }
    private class PrivateClass { }
    public PublicClass this[int i] { get { return null; } }
    public InternalInterface this[object o] { get { return null; } }
    public ProtectedStruct this[string s] { set { } }
    public PrivateClass this[double d] { get { return null; } }
    internal PublicClass this[int x, int y] { get { return null; } }
    internal InternalInterface this[object x, object y] { get { return null; } }
    internal ProtectedStruct this[string x, string y] { set { } }
    internal PrivateClass this[double x, double y] { get { return null; } }
    protected class D
    {
        public PublicClass this[int i] { get { return null; } }
        public InternalInterface this[object o] { get { return null; } }
        public ProtectedStruct this[string s] { set { } }
        public PrivateClass this[double d] { get { return null; } }
        internal PublicClass this[int x, int y] { get { return null; } }
        internal InternalInterface this[object x, object y] { get { return null; } }
        internal ProtectedStruct this[string x, string y] { set { } }
        internal PrivateClass this[double x, double y] { get { return null; } }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,30): error CS0054: Inconsistent accessibility: indexer return type 'InternalInterface' is less accessible than indexer 'A.this[object]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.this[object]", "InternalInterface").WithLocation(8, 30),
                // (9,28): error CS0054: Inconsistent accessibility: indexer return type 'A.ProtectedStruct' is less accessible than indexer 'A.this[string]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.this[string]", "A.ProtectedStruct").WithLocation(9, 28),
                // (10,25): error CS0054: Inconsistent accessibility: indexer return type 'A.PrivateClass' is less accessible than indexer 'A.this[double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.this[double]", "A.PrivateClass").WithLocation(10, 25),
                // (13,30): error CS0054: Inconsistent accessibility: indexer return type 'A.ProtectedStruct' is less accessible than indexer 'A.this[string, string]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.this[string, string]", "A.ProtectedStruct").WithLocation(13, 30),
                // (14,27): error CS0054: Inconsistent accessibility: indexer return type 'A.PrivateClass' is less accessible than indexer 'A.this[double, double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.this[double, double]", "A.PrivateClass").WithLocation(14, 27),
                // (18,34): error CS0054: Inconsistent accessibility: indexer return type 'InternalInterface' is less accessible than indexer 'A.B.this[object]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.B.this[object]", "InternalInterface").WithLocation(18, 34),
                // (20,29): error CS0054: Inconsistent accessibility: indexer return type 'A.PrivateClass' is less accessible than indexer 'A.B.this[double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.B.this[double]", "A.PrivateClass").WithLocation(20, 29),
                // (24,31): error CS0054: Inconsistent accessibility: indexer return type 'A.PrivateClass' is less accessible than indexer 'A.B.this[double, double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("A.B.this[double, double]", "A.PrivateClass").WithLocation(24, 31),
                // (33,28): error CS0054: Inconsistent accessibility: indexer return type 'C.ProtectedStruct' is less accessible than indexer 'C.this[string]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("C.this[string]", "C.ProtectedStruct").WithLocation(33, 28),
                // (34,24): error CS0054: Inconsistent accessibility: indexer return type 'C.PrivateClass' is less accessible than indexer 'C.this[double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("C.this[double]", "C.PrivateClass").WithLocation(34, 25),
                // (370): error CS0054: Inconsistent accessibility: indexer return type 'C.ProtectedStruct' is less accessible than indexer 'C.this[string, string]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("C.this[string, string]", "C.ProtectedStruct").WithLocation(37, 30),
                // (38,27): error CS0054: Inconsistent accessibility: indexer return type 'C.PrivateClass' is less accessible than indexer 'C.this[double, double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("C.this[double, double]", "C.PrivateClass").WithLocation(38, 27),
                // (44,29): error CS0054: Inconsistent accessibility: indexer return type 'C.PrivateClass' is less accessible than indexer 'C.D.this[double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("C.D.this[double]", "C.PrivateClass").WithLocation(44, 29),
                // (48,31): error CS0054: Inconsistent accessibility: indexer return type 'C.PrivateClass' is less accessible than indexer 'C.D.this[double, double]'
                Diagnostic(ErrorCode.ERR_BadVisIndexerReturn, "this").WithArguments("C.D.this[double, double]", "C.PrivateClass").WithLocation(48, 31));
        }

        [Fact]
        public void CS0055ERR_BadVisIndexerParam()
        {
            var text = @"class MyClass //defaults to private accessibility
{
}

public class MyClass2
{
    public int this[MyClass myClass]   // CS0055
    {
        get
        {
            return 0;
        }
    }
}

public class MyClass3
{
    public static void Main()
    {
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisIndexerParam, Line = 7, Column = 16 });
        }

        [Fact]
        public void CS0056ERR_BadVisOpReturn()
        {
            var text = @"class MyClass
{
}

public class A
{
    public static implicit operator MyClass(A a)   // CS0056
    {
        return new MyClass();
    }

    public static void Main()
    {
    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (7,28): error CS0056: Inconsistent accessibility: return type 'MyClass' is less accessible than operator 'A.implicit operator MyClass(A)'
//     public static implicit operator MyClass(A a)   // CS0056
Diagnostic(ErrorCode.ERR_BadVisOpReturn, "MyClass").WithArguments("A.implicit operator MyClass(A)", "MyClass")
            );
        }

        [Fact]
        public void CS0057ERR_BadVisOpParam()
        {
            var text = @"class MyClass //defaults to private accessibility
{
}

public class MyClass2
{
    public static implicit operator MyClass2(MyClass iii)   // CS0057
    {
        return new MyClass2();
    }

    public static void Main()
    {
    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (77): error CS0057: Inconsistent accessibility: parameter type 'MyClass' is less accessible than operator 'MyClass2.implicit operator MyClass2(MyClass)'
//     public static implicit operator MyClass2(MyClass iii)   // CS0057
Diagnostic(ErrorCode.ERR_BadVisOpParam, "MyClass2").WithArguments("MyClass2.implicit operator MyClass2(MyClass)", "MyClass"));
        }

        [Fact]
        public void CS0058ERR_BadVisDelegateReturn()
        {
            var text = @"class MyClass
{
}

public delegate MyClass MyClassDel();   // CS0058

public class A
{
    public static void Main()
    {
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisDelegateReturn, Line = 5, Column = 25 });
        }

        [WorkItem(542005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542005")]
        [Fact]
        public void CS0058ERR_BadVisDelegateReturn02()
        {
            var text = @"
public class Outer
{
    protected class Test { }
    public delegate Test MyDelegate(); 
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,26): error CS0058: Inconsistent accessibility: return type 'Outer.Test' is less accessible than delegate 'Outer.MyDelegate'
                //     public delegate Test MyDelegate(); 
                Diagnostic(ErrorCode.ERR_BadVisDelegateReturn, "MyDelegate").WithArguments("Outer.MyDelegate", "Outer.Test").WithLocation(5, 26)
                );
        }

        [Fact]
        public void CS0059ERR_BadVisDelegateParam()
        {
            var text = @"
class MyClass {} //defaults to internal accessibility
public delegate void MyClassDel(MyClass myClass);   // CS0059
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,22): error CS0059: Inconsistent accessibility: parameter type 'MyClass' is less accessible than delegate 'MyClassDel'
                // public delegate void MyClassDel(MyClass myClass);   // CS0059
                Diagnostic(ErrorCode.ERR_BadVisDelegateParam, "MyClassDel").WithArguments("MyClassDel", "MyClass")
                );
        }

        [Fact]
        public void CS0060ERR_BadVisBaseClass()
        {
            var text = @"

namespace NS
{
    internal class MyBase { }
    public class MyClass : MyBase { }

    public class Outer
    {
        private class MyBase { }
        protected class MyClass : MyBase { }

        protected class MyBase01 { }
        protected internal class MyClass01 : MyBase { }
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseClass, Line = 6, Column = 18 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseClass, Line = 11, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseClass, Line = 14, Column = 34 });
        }

        [WorkItem(539511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539511")]
        [Fact]
        public void CS0060ERR_BadVisBaseClass02()
        {
            var text = @"
public class A<T>
{
    public class B<S> : A<B<D>.C>
    {
        public class C { }
    }

    protected class D { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseClass, Line = 4, Column = 18 });
        }

        [WorkItem(539512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539512")]
        [Fact]
        public void CS0060ERR_BadVisBaseClass03()
        {
            var text = @"
public class A
{
    protected class B
    {
        protected class C { }
    }
}

internal class F : A
{
    private class D : B
    {
        public class E : C
        {

        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseClass, Line = 14, Column = 22 });
        }

        [WorkItem(539546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539546")]
        [Fact]
        public void CS0060ERR_BadVisBaseClass04()
        {
            var text = @"
public class A<T>
{
    private class B : A<B.C>
    {
        private class C { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseClass, Line = 4, Column = 19 });
        }

        [WorkItem(539562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539562")]
        [Fact]
        public void CS0060ERR_BadVisBaseClass05()
        {
            var text = @"
class A<T>
{
    class B : A<B>
    {
        public class C : B { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [WorkItem(539950, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539950")]
        [Fact]
        public void CS0060ERR_BadVisBaseClass06()
        {
            var text = @"
class A : C<E.F>
{
    public class B
    {
        public class D
        {
            protected class F { }
        }
    }
}

class C<T> { }

class E : A.B.D { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                // E.F is inaccessible where written; cascaded ERR_BadVisBaseClass is therefore suppressed
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadAccess, Line = 2, Column = 15 });
        }

        [Fact]
        public void CS0060ERR_BadVisBaseClass07()
        {
            var source =
@"internal class A { }
public class B<T>
{
    public class C<U> { }
}
public class C1 : B<A> { }
public class C2 : B<object>.C<A> { }
public class C3 : B<A>.C<object> { }
public class C4 : B<B<A>>.C<object> { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,14): error CS0060: Inconsistent accessibility: base type 'B<A>' is less accessible than class 'C1'
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C1").WithArguments("C1", "B<A>").WithLocation(6, 14),
                // (7,14): error CS0060: Inconsistent accessibility: base type 'B<object>.C<A>' is less accessible than class 'C2'
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C2").WithArguments("C2", "B<object>.C<A>").WithLocation(7, 14),
                // (8,14): error CS0060: Inconsistent accessibility: base type 'B<A>.C<object>' is less accessible than class 'C3'
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C3").WithArguments("C3", "B<A>.C<object>").WithLocation(8, 14),
                // (9,14): error CS0060: Inconsistent accessibility: base type 'B<B<A>>.C<object>' is less accessible than class 'C4'
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C4").WithArguments("C4", "B<B<A>>.C<object>").WithLocation(9, 14));
        }

        [Fact]
        public void CS0060ERR_BadVisBaseClass08()
        {
            var source =
@"public class A
{
    internal class B
    {
        public interface C { }
    }
}
public class B<T> : A { }
public class C : B<A.B.C> { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,14): error CS0060: Inconsistent accessibility: base type 'B<A.B.C>' is less accessible than class 'C'
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C").WithArguments("C", "B<A.B.C>").WithLocation(9, 14));
        }

        [Fact]
        public void CS0061ERR_BadVisBaseInterface()
        {
            var text = @"internal interface A { }
public interface AA : A { }  // CS0061

// OK
public interface B { }
internal interface BB : B { }

internal interface C { }
internal interface CC : C { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVisBaseInterface, Line = 2, Column = 18 });
        }

        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors()
        {
            var text = @"using System;
public delegate void Eventhandler(object sender, int e);
public class MyClass
{
    public event EventHandler E1 { }   // CS0065,
    public event EventHandler E2 { add { } }   // CS0065,
    public event EventHandler E3 { remove { } }   // CS0065,
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,31): error CS0065: 'MyClass.E1': event property must have both add and remove accessors
                //     public event EventHandler E1 { }   // CS0065,
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("MyClass.E1"),
                // (6,31): error CS0065: 'MyClass.E2': event property must have both add and remove accessors
                //     public event EventHandler E2 { add { } }   // CS0065,
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E2").WithArguments("MyClass.E2"),
                // (71): error CS0065: 'MyClass.E3': event property must have both add and remove accessors
                //     public event EventHandler E3 { remove { } }   // CS0065,
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E3").WithArguments("MyClass.E3"));
        }

        [WorkItem(542570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542570")]
        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors_Interface01()
        {
            var text = @"
delegate void myDelegate(int name = 1);
interface i1
{
    event myDelegate myevent { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,22): error CS0065: 'i1.myevent': event property must have both add and remove accessors
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "myevent").WithArguments("i1.myevent"));
        }

        [WorkItem(542570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542570")]
        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors_Interface02()
        {
            var text = @"

delegate void myDelegate(int name = 1);
interface i1
{
    event myDelegate myevent { add; remove; }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular,
                              targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (6,35): error CS0073: An add or remove accessor must have a body
                //     event myDelegate myevent { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 35),
                // (6,43): error CS0073: An add or remove accessor must have a body
                //     event myDelegate myevent { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 43));

            CreateCompilation(text, parseOptions: TestOptions.Regular7,
                              targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (6,35): error CS0073: An add or remove accessor must have a body
                //     event myDelegate myevent { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 35),
                // (6,43): error CS0073: An add or remove accessor must have a body
                //     event myDelegate myevent { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 43),
                // (6,32): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //     event myDelegate myevent { add; remove; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "8.0").WithLocation(6, 32),
                // (6,37): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //     event myDelegate myevent { add; remove; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "8.0").WithLocation(6, 37)
                );
        }

        [WorkItem(542570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542570")]
        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors_Interface03()
        {
            var text = @"
delegate void myDelegate(int name = 1);
interface i1
{
    event myDelegate myevent { add {} remove {} }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7,
                              targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (5,32): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //     event myDelegate myevent { add {} remove {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "8.0").WithLocation(5, 32),
                // (5,39): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //     event myDelegate myevent { add {} remove {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "8.0").WithLocation(5, 39)
                );
        }

        [WorkItem(542570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542570")]
        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors_Interface04()
        {
            var text = @"
delegate void myDelegate(int name = 1);
interface i1
{
    event myDelegate myevent { add {} }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7,
                              targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (5,32): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //     event myDelegate myevent { add {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "8.0").WithLocation(5, 32),
                // (5,22): error CS0065: 'i1.myevent': event property must have both add and remove accessors
                //     event myDelegate myevent { add {} }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "myevent").WithArguments("i1.myevent").WithLocation(5, 22)
                );
        }

        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors_Interface05()
        {
            var text = @"
public interface I2 { }

public interface I1
{
    event System.Action I2.P10;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,28): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                //     event System.Action I2.P10;
                Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, "P10").WithLocation(6, 28),
                // (6,25): error CS0540: 'I1.P10': containing type does not implement interface 'I2'
                //     event System.Action I2.P10;
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I2").WithArguments("I1.P10", "I2").WithLocation(6, 25),
                // (6,28): error CS0539: 'I1.P10' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I2.P10;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P10").WithArguments("I1.P10").WithLocation(6, 28)
                );
        }

        [Fact]
        public void CS0065ERR_EventNeedsBothAccessors_Interface06()
        {
            var text = @"
public interface I2 { }

public interface I1
{
    event System.Action I2.
P10;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,25): error CS0540: 'I1.P10': containing type does not implement interface 'I2'
                //     event System.Action I2.
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I2").WithArguments("I1.P10", "I2").WithLocation(6, 25),
                // (7,1): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                // P10;
                Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, "P10").WithLocation(7, 1),
                // (7,1): error CS0539: 'I1.P10' in explicit interface declaration is not found among members of the interface that can be implemented
                // P10;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P10").WithArguments("I1.P10").WithLocation(7, 1)
                );
        }

        [Fact]
        public void CS0066ERR_EventNotDelegate()
        {
            var text = @"
public class C
{
    public event C Click;   // CS0066
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,20): error CS0066: 'C.Click': event must be of a delegate type
                //     public event C Click;   // CS0066
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Click").WithArguments("C.Click"),
                // (4,20): warning CS0067: The event 'C.Click' is never used
                //     public event C Click;   // CS0066
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Click").WithArguments("C.Click"));
        }

        [Fact]
        public void CS0068ERR_InterfaceEventInitializer()
        {
            var text = @"
delegate void MyDelegate();

interface I
{
    event MyDelegate d = new MyDelegate(M.f);   // CS0068
}

class M
{
    event MyDelegate d = new MyDelegate(M.f);

    public static void f()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,22): error CS0068: 'I.d': event in interface cannot have initializer
                //     event MyDelegate d = new MyDelegate(M.f);   // CS0068
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "d").WithArguments("I.d").WithLocation(6, 22),
                // (6,22): warning CS0067: The event 'I.d' is never used
                //     event MyDelegate d = new MyDelegate(M.f);   // CS0068
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d").WithArguments("I.d").WithLocation(6, 22));
        }

        [Fact]
        public void CS0072ERR_CantOverrideNonEvent()
        {
            var text = @"delegate void MyDelegate();

class Test1
{
    public virtual event MyDelegate MyEvent;
    public virtual void VMeth()
    {
    }
}

class Test2 : Test1
{
    public override event MyDelegate VMeth   // CS0072
    {
        add
        {
            VMeth += value;
        }
        remove
        {
            VMeth -= value;
        }
    }

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,38): error CS0072: 'Test2.VMeth': cannot override; 'Test1.VMeth()' is not an event
                //     public override event MyDelegate VMeth   // CS0072
                Diagnostic(ErrorCode.ERR_CantOverrideNonEvent, "VMeth").WithArguments("Test2.VMeth", "Test1.VMeth()"),
                // (5,37): warning CS0067: The event 'Test1.MyEvent' is never used
                //     public virtual event MyDelegate MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test1.MyEvent"));
        }

        [Fact]
        public void CS0074ERR_AbstractEventInitializer()
        {
            var text = @"
delegate void D();

abstract class Test
{
    public abstract event D e = null;   // CS0074
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,29): error CS0074: 'Test.e': abstract event cannot have initializer
                //     public abstract event D e = null;   // CS0074
                Diagnostic(ErrorCode.ERR_AbstractEventInitializer, "e").WithArguments("Test.e").WithLocation(6, 29),
                // (6,29): warning CS0414: The field 'Test.e' is assigned but its value is never used
                //     public abstract event D e = null;   // CS0074
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "e").WithArguments("Test.e").WithLocation(6, 29));
        }

        [Fact]
        public void CS0076ERR_ReservedEnumerator()
        {
            var text =
@"enum E { value__ }
enum F { A, B, value__ }
enum G { X = 0, value__ = 1, Z = value__ + 1 }
enum H { Value__ } // no error
class C
{
    E value__; // no error
    static void M()
    {
        F value__; // no error
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (1,10): error CS0076: The enumerator name 'value__' is reserved and cannot be used
                // enum E { value__ }
                Diagnostic(ErrorCode.ERR_ReservedEnumerator, "value__").WithArguments("value__"),
                // (2,16): error CS0076: The enumerator name 'value__' is reserved and cannot be used
                // enum F { A, B, value__ }
                Diagnostic(ErrorCode.ERR_ReservedEnumerator, "value__").WithArguments("value__"),
                // (3,17): error CS0076: The enumerator name 'value__' is reserved and cannot be used
                // enum G { X = 0, value__ = 1, Z = value__ + 1 }
                Diagnostic(ErrorCode.ERR_ReservedEnumerator, "value__").WithArguments("value__"),
                // (10,11): warning CS0168: The variable 'value__' is declared but never used
                //         F value__; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "value__").WithArguments("value__"),
                // (7,7): warning CS0169: The field 'C.value__' is never used
                //     E value__; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "value__").WithArguments("C.value__")
                );
        }

        /// <summary>
        /// Currently parser error 1001, 1003.  Is that good enough?
        /// </summary>
        [Fact()]
        public void CS0081ERR_TypeParamMustBeIdentifier01()
        {
            var text = @"namespace NS
{
  class C
  {
    int F<int>() { }  // CS0081
  }
}";
            // Triage decision was made to have this be a parse error as the grammar specifies it as such.
            // TODO: vsadov, the error recovery would be much nicer here if we consumed "int", bu tneed to consider other cases.
            CreateCompilationWithMscorlib46(text, parseOptions: TestOptions.Regular).VerifyDiagnostics(
                // (5,11): error CS1001: Identifier expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(5, 11),
                // (5,11): error CS1003: Syntax error, '>' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(">").WithLocation(5, 11),
                // (5,11): error CS1003: Syntax error, '(' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("(").WithLocation(5, 11),
                // (5,14): error CS1001: Identifier expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(5, 14),
                // (5,14): error CS1003: Syntax error, ',' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments(",").WithLocation(5, 14),
                // (5,15): error CS1003: Syntax error, ',' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(5, 15),
                // (5,16): error CS8124: Tuple must contain at least two elements.
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 16),
                // (5,18): error CS1001: Identifier expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(5, 18),
                // (5,18): error CS1026: ) expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(5, 18),
                // (5,15): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "()").WithArguments("System.ValueTuple`2").WithLocation(5, 15),
                // (5,9): error CS0161: 'C.F<>(int, (?, ?))': not all code paths return a value
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("NS.C.F<>(int, (?, ?))").WithLocation(5, 9)
                );
        }

        /// <summary>
        /// Currently parser error 1001, 1003.  Is that good enough?
        /// </summary>
        [Fact()]
        public void CS0081ERR_TypeParamMustBeIdentifier01WithCSharp6()
        {
            var text = @"namespace NS
{
  class C
  {
    int F<int>() { }  // CS0081
  }
}";
            // Triage decision was made to have this be a parse error as the grammar specifies it as such.
            CreateCompilationWithMscorlib46(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (5,11): error CS1001: Identifier expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(5, 11),
                // (5,11): error CS1003: Syntax error, '>' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(">").WithLocation(5, 11),
                // (5,11): error CS1003: Syntax error, '(' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("(").WithLocation(5, 11),
                // (5,14): error CS1001: Identifier expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(5, 14),
                // (5,14): error CS1003: Syntax error, ',' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments(",").WithLocation(5, 14),
                // (5,15): error CS1003: Syntax error, ',' expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(5, 15),
                // (5,15): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "()").WithArguments("tuples", "7.0").WithLocation(5, 15),
                // (5,16): error CS8124: Tuple must contain at least two elements.
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 16),
                // (5,18): error CS1001: Identifier expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(5, 18),
                // (5,18): error CS1026: ) expected
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(5, 18),
                // (5,15): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "()").WithArguments("System.ValueTuple`2").WithLocation(5, 15),
                // (5,9): error CS0161: 'C.F<>(int, (?, ?))': not all code paths return a value
                //     int F<int>() { }  // CS0081
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("NS.C.F<>(int, (?, ?))").WithLocation(5, 9)
                );
        }

        [Fact]
        public void CS0082ERR_MemberReserved01()
        {
            CreateCompilation(
@"class C
{
    public void set_P(int i) { }
    public int P { get; set; }
    public int get_P() { return 0; }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_P", "C").WithLocation(4, 20),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_P", "C").WithLocation(4, 25));
        }

        [Fact]
        public void CS0082ERR_MemberReserved02()
        {
            CreateCompilation(
@"class A
{
    public void set_P(int i) { }
}
partial class B : A
{
    public int P
    {
        get { return 0; }
        set { }
    }
}
partial class B
{
    partial void get_P();
    public void set_P() { }
}
partial class B
{
    partial void get_P() { }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_P", "B").WithLocation(9, 9));
        }

        [Fact]
        public void CS0082ERR_MemberReserved03()
        {
            CreateCompilation(
@"abstract class C
{
    public abstract object P { get; }
    protected abstract object Q { set; }
    internal object R { get; set; }
    protected internal object S { get { return null; } }
    private object T { set { } }
    object U { get { return null; } set { } }

    object get_P() { return null; }
    void set_P(object value) { }
    private object get_Q() { return null; }
    private void set_Q(object value) { }
    protected internal object get_R() { return null; }
    protected internal void set_R(object value) { }
    internal object get_S() { return null; }
    internal void set_S(object value) { }
    protected object get_T() { return null; }
    protected void set_T(object value) { }
    public object get_U() { return null; }
    public void set_U(object value) { }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_P", "C").WithLocation(3, 32),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "P").WithArguments("set_P", "C").WithLocation(3, 28),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "Q").WithArguments("get_Q", "C").WithLocation(4, 31),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_Q", "C").WithLocation(4, 35),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_R", "C").WithLocation(5, 25),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_R", "C").WithLocation(5, 30),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_S", "C").WithLocation(6, 35),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "S").WithArguments("set_S", "C").WithLocation(6, 31),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "T").WithArguments("get_T", "C").WithLocation(7, 20),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_T", "C").WithLocation(7, 24),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_U", "C").WithLocation(8, 16),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_U", "C").WithLocation(8, 37));
        }

        [Fact]
        public void CS0082ERR_MemberReserved04()
        {
            CreateCompilationWithMscorlib40AndSystemCore(
@"class A<T, U>
{
    public T P { get; set; } // CS0082
    public U Q { get; set; } // no error
    public U R { get; set; } // no error
    public void set_P(T t) { }
    public void set_Q(object o) { }
    public void set_R(T t) { }
}
class B : A<object, object>
{
}
class C
{
    public dynamic P { get; set; } // CS0082
    public dynamic Q { get; set; } // CS0082
    public object R { get; set; } // CS0082
    public object S { get; set; } // CS0082
    public void set_P(object o) { }
    public void set_Q(dynamic o) { }
    public void set_R(object o) { }
    public void set_S(dynamic o) { }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_P", "A<T, U>").WithLocation(3, 23),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_P", "C").WithLocation(15, 29),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_Q", "C").WithLocation(16, 29),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_R", "C").WithLocation(17, 28),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_S", "C").WithLocation(18, 28));
        }

        [Fact]
        public void CS0082ERR_MemberReserved05()
        {
            CreateCompilation(
@"class C
{
    object P { get; set; }
    object Q { get; set; }
    object R { get; set; }
    object[] S { get; set; }
    public object get_P(object o) { return null; } // CS0082
    public void set_P() { }
    public void set_P(ref object o) { }
    public void get_Q() { } // CS0082
    public object set_Q(object o) { return null; } // CS0082
    public object set_Q(out object o) { o = null;  return null; }
    void set_S(params object[] args) { } // CS0082
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_Q", "C").WithLocation(4, 16),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_Q", "C").WithLocation(4, 21),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_S", "C").WithLocation(6, 23));
        }

        [Fact]
        public void CS0082ERR_MemberReserved06()
        {
            // No errors for explicit interface implementation.
            CreateCompilation(
@"interface I
{
    int get_P();
    void set_P(int o);
}
class C : I
{
    public int P { get; set; }
    int I.get_P() { return 0; }
    void I.set_P(int o) { }
}
")
                .VerifyDiagnostics();
        }

        [WorkItem(539770, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539770")]
        [Fact]
        public void CS0082ERR_MemberReserved07()
        {
            // No errors for explicit interface implementation.
            CreateCompilation(
@"class C
{
    public object P { get { return null; } }
    object get_P() { return null; }
    void set_P(object value) { }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_P", "C"),
                    Diagnostic(ErrorCode.ERR_MemberReserved, "P").WithArguments("set_P", "C"));
        }

        [Fact]
        public void CS0082ERR_MemberReserved08()
        {
            CreateCompilation(
@"class C
{
    public event System.Action E;
    void add_E(System.Action value) { }
    void remove_E(System.Action value) { }
}
")
                .VerifyDiagnostics(
                // (3,32): error CS0082: Type 'C' already reserves a member called 'add_E' with the same parameter types
                //     public event System.Action E;
                Diagnostic(ErrorCode.ERR_MemberReserved, "E").WithArguments("add_E", "C"),
                // (3,32): error CS0082: Type 'C' already reserves a member called 'remove_E' with the same parameter types
                //     public event System.Action E;
                Diagnostic(ErrorCode.ERR_MemberReserved, "E").WithArguments("remove_E", "C"),
                // (3,32): warning CS0067: The event 'C.E' is never used
                //     public event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void CS0082ERR_MemberReserved09()
        {
            CreateCompilation(
@"class C
{
    public event System.Action E { add { } remove { } }
    void add_E(System.Action value) { }
    void remove_E(System.Action value) { }
}
")
                .VerifyDiagnostics(
                    // (3,36): error CS0082: Type 'C' already reserves a member called 'add_E' with the same parameter types
                    //     public event System.Action E { add { } remove { } }
                    Diagnostic(ErrorCode.ERR_MemberReserved, "add").WithArguments("add_E", "C"),
                    // (3,44): error CS0082: Type 'C' already reserves a member called 'remove_E' with the same parameter types
                    //     public event System.Action E { add { } remove { } }
                    Diagnostic(ErrorCode.ERR_MemberReserved, "remove").WithArguments("remove_E", "C"));
        }

        [Fact]
        public void CS0100ERR_DuplicateParamName01()
        {
            var text = @"namespace NS
{
    interface IGoo
    {
        void M1(byte b, sbyte b);
    }

    struct S
    {
        public void M2(object p, ref string p, ref string p, params ulong[] p) { p = null; }
    }
}
";
            var comp = CreateCompilation(text).VerifyDiagnostics(
    // (5,31): error CS0100: The parameter name 'b' is a duplicate
    //         void M1(byte b, sbyte b);
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "b").WithArguments("b").WithLocation(5, 31),
    // (10,45): error CS0100: The parameter name 'p' is a duplicate
    //         public void M2(object p, ref string p, ref string p, params ulong[] p) { p = null; }
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "p").WithArguments("p").WithLocation(10, 45),
    // (10,59): error CS0100: The parameter name 'p' is a duplicate
    //         public void M2(object p, ref string p, ref string p, params ulong[] p) { p = null; }
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "p").WithArguments("p").WithLocation(10, 59),
    // (10,77): error CS0100: The parameter name 'p' is a duplicate
    //         public void M2(object p, ref string p, ref string p, params ulong[] p) { p = null; }
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "p").WithArguments("p").WithLocation(10, 77),
    // (10,82): error CS0229: Ambiguity between 'object p' and 'ref string p'
    //         public void M2(object p, ref string p, ref string p, params ulong[] p) { p = null; }
    Diagnostic(ErrorCode.ERR_AmbigMember, "p").WithArguments("object p", "ref string p").WithLocation(10, 82)
                );
            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0101ERR_DuplicateNameInNS01()
        {
            var text = @"namespace NS
{
    struct Test { }
    class Test { }
    interface Test { }

    namespace NS1
    {
        interface A<T, V> { }
        class A<T, V> { }
        class A<T, V> { }
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 4, Column = 11 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 5, Column = 15 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 10, Column = 15 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 11, Column = 15 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0101ERR_DuplicateNameInNS02()
        {
            var text = @"namespace NS
{
    interface IGoo<T, V> { }
    class IGoo<T, V> { }

    struct SS { }
    public class SS { }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 4, Column = 11 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 7, Column = 18 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [Fact]
        public void CS0101ERR_DuplicateNameInNS03()
        {
            var text = @"namespace NS
{
    interface IGoo<T, V> { }
    interface IGoo<T, V> { }

    struct SS { }
    public struct SS { }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 4, Column = 15 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInNS, Line = 7, Column = 19 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [Fact]
        public void CS0101ERR_DuplicateNameInNS04()
        {
            var text = @"namespace NS
{
    partial class Goo<T, V> { } // no error, because ""partial""
    partial class Goo<T, V> { }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass01()
        {
            var text = @"class A
{
    int n = 0;
    long n = 1;
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,10): error CS0102: The type 'A' already contains a definition for 'n'
                //     long n = 1;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "n").WithArguments("A", "n"),
                // (3,9): warning CS0414: The field 'A.n' is assigned but its value is never used
                //     int n = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "n").WithArguments("A.n"),
                // (4,10): warning CS0414: The field 'A.n' is assigned but its value is never used
                //     long n = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "n").WithArguments("A.n")
                );

            var classA = comp.SourceModule.GlobalNamespace.GetTypeMembers("A").Single() as NamedTypeSymbol;
            var ns = classA.GetMembers("n");
            Assert.Equal(2, ns.Length);
            foreach (var n in ns)
            {
                Assert.Equal(TypeKind.Struct, (n as FieldSymbol).Type.TypeKind);
            }
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass02()
        {
            CreateCompilation(
@"namespace NS
{
    class C
    {
        interface I<T, U> { }
        interface I<T, U> { }

        struct S { }
        public struct S { }
    }
    struct S<X>
    {
        X x;
        C x;
    }
}
")
            .VerifyDiagnostics(
                // (6,19): error CS0102: The type 'NS.C' already contains a definition for 'I'
                //         interface I<T, U> { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "I").WithArguments("NS.C", "I"),
                // (9,23): error CS0102: The type 'NS.C' already contains a definition for 'S'
                //         public struct S { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("NS.C", "S"),
                // (14,11): error CS0102: The type 'NS.S<X>' already contains a definition for 'x'
                //         C x;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x").WithArguments("NS.S<X>", "x"),
                // (13,11): warning CS0169: The field 'NS.S<X>.x' is never used
                //         X x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("NS.S<X>.x"),
                // (14,11): warning CS0169: The field 'NS.S<X>.x' is never used
                //         C x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("NS.S<X>.x")
                ); // Dev10 miss this with previous errors
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass03()
        {
            CreateCompilation(
@"namespace NS
{
    class C
    {
        interface I<T> { }
        class I<U> { }
        struct S { }
        class S { }
        enum E { }
        interface E { }
    }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "I").WithArguments("NS.C", "I").WithLocation(6, 15),
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("NS.C", "S").WithLocation(8, 15),
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("NS.C", "E").WithLocation(10, 19));
        }

        [Fact]
        public void CS0104ERR_AmbigContext01()
        {
            var text = @"namespace n1
{
    public interface IGoo<T> { }
    class A { }
}
namespace n3
{
    using n1;
    using n2;

    namespace n2
    {
        public interface IGoo<V> { }
        public class A { }
    }
    public class C<X> : IGoo<X>
    {
    }

    struct S
    {
        A a;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (22,9): error CS0104: 'A' is an ambiguous reference between 'n1.A' and 'n3.n2.A'
                //         A a;
                Diagnostic(ErrorCode.ERR_AmbigContext, "A").WithArguments("A", "n1.A", "n3.n2.A").WithLocation(22, 9),
                // (16,25): error CS0104: 'IGoo<>' is an ambiguous reference between 'n1.IGoo<T>' and 'n3.n2.IGoo<V>'
                //     public class C<X> : IGoo<X>
                Diagnostic(ErrorCode.ERR_AmbigContext, "IGoo<X>").WithArguments("IGoo<>", "n1.IGoo<T>", "n3.n2.IGoo<V>").WithLocation(16, 25),
                // (22,11): warning CS0169: The field 'S.a' is never used
                //         A a;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("n3.S.a").WithLocation(22, 11)
            );

            var ns3 = comp.SourceModule.GlobalNamespace.GetMember<NamespaceSymbol>("n3");
            var classC = ns3.GetMember<NamedTypeSymbol>("C");
            var classCInterface = classC.Interfaces().Single();
            Assert.Equal("IGoo", classCInterface.Name);
            Assert.Equal(TypeKind.Error, classCInterface.TypeKind);

            var structS = ns3.GetMember<NamedTypeSymbol>("S");
            var structSField = structS.GetMember<FieldSymbol>("a");
            Assert.Equal("A", structSField.Type.Name);
            Assert.Equal(TypeKind.Error, structSField.Type.TypeKind);
        }

        [Fact]
        public void CS0106ERR_BadMemberFlag01()
        {
            var text = @"namespace MyNamespace
{
    interface I
    {
        void m();
        static public void f();
    }

    public class MyClass
    {
        virtual ushort field;
        public void I.m()   // CS0106
        {
        }
    }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (11,24): error CS0106: The modifier 'virtual' is not valid for this item
                //         virtual ushort field;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "field").WithArguments("virtual").WithLocation(11, 24),
                // (12,23): error CS0106: The modifier 'public' is not valid for this item
                //         public void I.m()   // CS0106
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "m").WithArguments("public").WithLocation(12, 23),
                // (12,21): error CS0540: 'MyClass.I.m()': containing type does not implement interface 'I'
                //         public void I.m()   // CS0106
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I").WithArguments("MyNamespace.MyClass.MyNamespace.I.m()", "MyNamespace.I").WithLocation(12, 21),
                // (11,24): warning CS0169: The field 'MyClass.field' is never used
                //         virtual ushort field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("MyNamespace.MyClass.field").WithLocation(11, 24),
                // (6,28): error CS8503: The modifier 'static' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //         static public void f();   // CS0106
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "f").WithArguments("static", "7.0", "8.0").WithLocation(6, 28),
                // (6,28): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //         static public void f();   // CS0106
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "f").WithArguments("public", "7.0", "8.0").WithLocation(6, 28),
                // (6,28): error CS0501: 'I.f()' must declare a body because it is not marked abstract, extern, or partial
                //         static public void f();   // CS0106
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "f").WithArguments("MyNamespace.I.f()").WithLocation(6, 28)
            );
        }

        [Fact]
        public void CS0106ERR_BadMemberFlag02()
        {
            var text =
@"interface I
{
    public static int P1 { get; }
    abstract int P2 { static set; }
    int P4 { new abstract get; }
    int P5 { static set; }
    int P6 { sealed get; }
}
class C
{
    public int P1 { virtual get { return 0; } }
    internal int P2 { static set { } }
    static int P3 { new get { return 0; } }
    int P4 { sealed get { return 0; } }
    protected internal object P5 { get { return null; } extern set; }
    public extern object P6 { get; } // no error
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (3,23): error CS8503: The modifier 'static' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     public static int P1 { get; }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "P1").WithArguments("static", "7.0", "8.0").WithLocation(3, 23),
                // (3,23): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     public static int P1 { get; }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "P1").WithArguments("public", "7.0", "8.0").WithLocation(3, 23),
                // (4,18): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     abstract int P2 { static set; }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "P2").WithArguments("abstract", "7.0", "8.0").WithLocation(4, 18),
                // (4,30): error CS0106: The modifier 'static' is not valid for this item
                //     abstract int P2 { static set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("static").WithLocation(4, 30),
                // (5,27): error CS0106: The modifier 'abstract' is not valid for this item
                //     int P4 { new abstract get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("abstract").WithLocation(5, 27),
                // (5,27): error CS0106: The modifier 'new' is not valid for this item
                //     int P4 { new abstract get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("new").WithLocation(5, 27),
                // (6,21): error CS0106: The modifier 'static' is not valid for this item
                //     int P5 { static set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("static").WithLocation(6, 21),
                // (7,21): error CS0106: The modifier 'sealed' is not valid for this item
                //     int P6 { sealed get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("sealed").WithLocation(7, 21),
                // (11,29): error CS0106: The modifier 'virtual' is not valid for this item
                //     public int P1 { virtual get { return 0; } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("virtual").WithLocation(11, 29),
                // (12,30): error CS0106: The modifier 'static' is not valid for this item
                //     internal int P2 { static set { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("static").WithLocation(12, 30),
                // (13,25): error CS0106: The modifier 'new' is not valid for this item
                //     static int P3 { new get { return 0; } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("new").WithLocation(13, 25),
                // (14,21): error CS0106: The modifier 'sealed' is not valid for this item
                //     int P4 { sealed get { return 0; } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("sealed").WithLocation(14, 21),
                // (15,64): error CS0106: The modifier 'extern' is not valid for this item
                //     protected internal object P5 { get { return null; } extern set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("extern").WithLocation(15, 64),
                // (16,31): warning CS0626: Method, operator, or accessor 'C.P6.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     public extern object P6 { get; } // no error
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("C.P6.get").WithLocation(16, 31)
                );
        }

        [WorkItem(539584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539584")]
        [Fact]
        public void CS0106ERR_BadMemberFlag03()
        {
            var text = @"
class C
{
    sealed private C() { }
    new abstract C(object o);
    public virtual C(C c) { }
    protected internal override C(int i, int j) { }
    volatile const int x = 1;
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (4,20): error CS0106: The modifier 'sealed' is not valid for this item
    //     sealed private C() { }
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("sealed"),
    // (5,18): error CS0106: The modifier 'abstract' is not valid for this item
    //     new abstract C(object o);
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("abstract"),
    // (5,18): error CS0106: The modifier 'new' is not valid for this item
    //     new abstract C(object o);
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("new"),
    // (6,20): error CS0106: The modifier 'virtual' is not valid for this item
    //     public virtual C(C c) { }
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("virtual"),
    // (73): error CS0106: The modifier 'override' is not valid for this item
    //     protected internal override C(int i, int j) { }
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("override"),
    // (8,24): error CS0106: The modifier 'volatile' is not valid for this item
    //     volatile const int x = 1;
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("volatile")
                );
        }

        [Fact]
        public void CS0106ERR_BadMemberFlag04()
        {
            var text = @"
class C
{
    static int this[int x] { set { } }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadMemberFlag, Line = 4, Column = 16 });
        }

        [Fact]
        public void CS0106ERR_BadMemberFlag05()
        {
            var text = @"
struct Goo
{
    public abstract void Bar1();
    public virtual void Bar2() { }
    public virtual int Bar3 { get;set; }
    public abstract int Bar4 { get;set; }
    public abstract event System.EventHandler Bar5;
    public virtual event System.EventHandler Bar6;
    // prevent warning for test
    void OnBar() { Bar6?.Invoke(null, null); }
    public virtual int this[int x] { get { return 1;} set {;} }
    // use long for to prevent signature clash
    public abstract int this[long x] { get; set; }
    public sealed override string ToString() => null;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,24): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual int Bar3 { get;set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Bar3").WithArguments("virtual").WithLocation(6, 24),
                // (7,25): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract int Bar4 { get;set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Bar4").WithArguments("abstract").WithLocation(7, 25),
                // (12,24): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual int this[int x] { get { return 1;} set {;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("virtual").WithLocation(12, 24),
                // (14,25): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract int this[long x] { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("abstract").WithLocation(14, 25),
                // (5,25): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual void Bar2() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Bar2").WithArguments("virtual").WithLocation(5, 25),
                // (4,26): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract void Bar1();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Bar1").WithArguments("abstract").WithLocation(4, 26),
                // (8,47): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract event System.EventHandler Bar5;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Bar5").WithArguments("abstract").WithLocation(8, 47),
                // (9,46): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual event System.EventHandler Bar6;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Bar6").WithArguments("virtual").WithLocation(9, 46),
                // (15,35): error CS0106: The modifier 'sealed' is not valid for this item
                //      public sealed override string ToString() => null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "ToString").WithArguments("sealed").WithLocation(15, 35));
        }

        [Fact]
        public void CS0106ERR_BadMemberFlag06()
        {
            var text =
@"interface I
{
    int P1 { get; }
    int P2 { get; set; }
}
class C : I
{
    private int I.P1 
    { 
        get { return 0; } 
    }

    int I.P2 
    { 
        private get { return 0; } 
        set {}
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,19): error CS0106: The modifier 'private' is not valid for this item
                //     private int I.P1 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("private").WithLocation(8, 19),
                // (15,17): error CS0106: The modifier 'private' is not valid for this item
                //         private get { return 0; } 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("private").WithLocation(15, 17)
                );
        }

        [Fact]
        public void CS0111ERR_MemberAlreadyExists01()
        {
            var text = @"class A
{
    void Test() { }
    public static void Test() { }   // CS0111

    public static void Main() { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_MemberAlreadyExists, Line = 4, Column = 24 });
        }

        [Fact]
        public void CS0111ERR_MemberAlreadyExists02()
        {
            var compilation = CreateCompilation(
@"static class S
{
    internal static void E<T>(this T t, object o) where T : new() { }
    internal static void E<T>(this T t, object o) where T : class { }
    internal static void E<U>(this U u, object o) { }
}");
            compilation.VerifyDiagnostics(
                // (4,26): error CS0111: Type 'S' already defines a member called 'E' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "E").WithArguments("E", "S").WithLocation(4, 26),
                // (5,26): error CS0111: Type 'S' already defines a member called 'E' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "E").WithArguments("E", "S").WithLocation(5, 26));
        }

        [Fact]
        public void CS0111ERR_MemberAlreadyExists03()
        {
            var compilation = CreateCompilation(
@"class C
{
    object this[object o] { get { return null; } set { } }
    object this[int x] { get { return null; } }
    int this[int y] { set { } }
    object this[object o] { get { return null; } set { } }
}
interface I
{
    object this[int x, int y] { get; set; }
    I this[int a, int b] { get; }
    I this[int a, int b] { set; }
    I this[object a, object b] { get; }
}");
            compilation.VerifyDiagnostics(
                // (5,9): error CS0111: Type 'C' already defines a member called 'this' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C").WithLocation(5, 9),
                // (6,12): error CS0111: Type 'C' already defines a member called 'this' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C").WithLocation(6, 12),
                // (11,7): error CS0111: Type 'I' already defines a member called 'this' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "I").WithLocation(11, 7),
                // (12,7): error CS0111: Type 'I' already defines a member called 'this' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "I").WithLocation(12, 7));
        }

        [Fact]
        public void CS0111ERR_MemberAlreadyExists04()
        {
            var compilation = CreateCompilation(
@"
using AliasForI = I;
public interface I
{
    int this[int x] { get; set; }
}

public interface J
{
    int this[int x] { get; set; }
}

public class C : I, J
{
    int I.this[int x] { get { return 0; } set { } }
    int AliasForI.this[int x] { get { return 0; } set { } } //CS0111
    int J.this[int x] { get { return 0; } set { } } //fine
    public int this[int x] { get { return 0; } set { } } //fine
}
");
            compilation.VerifyDiagnostics(
                // (13,14): error CS8646: 'I.this[int]' is explicitly implemented more than once.
                // public class C : I, J
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("I.this[int]").WithLocation(13, 14),
                // (16,19): error CS0111: Type 'C' already defines a member called 'this' with the same parameter types
                //     int AliasForI.this[int x] { get { return 0; } set { } } //CS0111
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C"));
        }

        /// <summary>
        /// Method signature comparison should ignore constraints.
        /// </summary>
        [Fact]
        public void CS0111ERR_MemberAlreadyExists05()
        {
            var compilation = CreateCompilation(
@"class C
{
    void M<T>(T t) where T : new() { }
    void M<U>(U u) where U : struct { }
    void M<T>(T t) where T : C { }
}
interface I<T>
{
    void M<U>();
    void M<U>() where U : T;
}");
            compilation.VerifyDiagnostics(
                // (4,10): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(4, 10),
                // (5,10): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(5, 10),
                // (10,10): error CS0111: Type 'I<T>' already defines a member called 'M' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "I<T>").WithLocation(10, 10));
        }

        [Fact]
        public void CS0112ERR_StaticNotVirtual01()
        {
            var text = @"namespace MyNamespace
{
    abstract public class MyClass
    {
        public abstract void MyMethod();
    }
    public class MyClass2 : MyClass
    {
        override public static void MyMethod()   // CS0112, remove static keyword
        {
        }
        public static int Main()
        {
            return 0;
        }
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (7,18): error CS0534: 'MyClass2' does not implement inherited abstract member 'MyClass.MyMethod()'
                //     public class MyClass2 : MyClass
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "MyClass2").WithArguments("MyNamespace.MyClass2", "MyNamespace.MyClass.MyMethod()").WithLocation(7, 18),
                // (9,37): error CS0112: A static member cannot be marked as 'override'
                //         override public static void MyMethod()   // CS0112, remove static keyword
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "MyMethod").WithArguments("override").WithLocation(9, 37),
                // (9,37): warning CS0114: 'MyClass2.MyMethod()' hides inherited member 'MyClass.MyMethod()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //         override public static void MyMethod()   // CS0112, remove static keyword
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "MyMethod").WithArguments("MyNamespace.MyClass2.MyMethod()", "MyNamespace.MyClass.MyMethod()").WithLocation(9, 37)
                );
        }

        [Fact]
        public void CS0112ERR_StaticNotVirtual02()
        {
            var text =
@"abstract class A
{
    protected abstract object P { get; }
}
class B : A
{
    protected static override object P { get { return null; } }
    public static virtual object Q { get; }
    internal static abstract object R { get; set; }
}
";
            var tree = Parse(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            CreateCompilation(tree).VerifyDiagnostics(
                // (5,7): error CS0534: 'B' does not implement inherited abstract member 'A.P.get'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.P.get").WithLocation(5, 7),
                // (7,38): error CS0112: A static member cannot be marked as 'override'
                //     protected static override object P { get { return null; } }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P").WithArguments("override").WithLocation(7, 38),
                // (7,38): warning CS0114: 'B.P' hides inherited member 'A.P'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     protected static override object P { get { return null; } }
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P").WithArguments("B.P", "A.P").WithLocation(7, 38),
                // (8,34): error CS0112: A static member cannot be marked as 'virtual'
                //     public static virtual object Q { get; }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "Q").WithArguments("virtual").WithLocation(8, 34),
                // (8,34): error CS8026: Feature 'readonly automatically implemented properties' is not available in C# 5. Please use language version 6 or greater.
                //     public static virtual object Q { get; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "Q").WithArguments("readonly automatically implemented properties", "6").WithLocation(8, 34),
                // (9,37): error CS0112: A static member cannot be marked as 'abstract'
                //     internal static abstract object R { get; set; }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "R").WithArguments("abstract").WithLocation(9, 37)
                );
        }

        [Fact]
        public void CS0112ERR_StaticNotVirtual03()
        {
            var text =
@"abstract class A
{
    protected abstract event System.Action P;
}
abstract class B : A
{
    protected static override event System.Action P;
    public static virtual event System.Action Q;
    internal static abstract event System.Action R;
}
";
            CreateCompilation(text, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (7,51): error CS0112: A static member cannot be marked as 'override'
                //     protected static override event System.Action P;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P").WithArguments("override").WithLocation(7, 51),
                // (7,51): error CS0533: 'B.P' hides inherited abstract member 'A.P'
                //     protected static override event System.Action P;
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("B.P", "A.P").WithLocation(7, 51),
                // (7,51): warning CS0114: 'B.P' hides inherited member 'A.P'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     protected static override event System.Action P;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P").WithArguments("B.P", "A.P").WithLocation(7, 51),
                // (7,51): warning CS0067: The event 'B.P' is never used
                //     protected static override event System.Action P;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P").WithArguments("B.P").WithLocation(7, 51),
                // (8,47): error CS0112: A static member cannot be marked as 'virtual'
                //     public static virtual event System.Action Q;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "Q").WithArguments("virtual").WithLocation(8, 47),
                // (8,47): warning CS0067: The event 'B.Q' is never used
                //     public static virtual event System.Action Q;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Q").WithArguments("B.Q").WithLocation(8, 47),
                // (9,50): error CS0112: A static member cannot be marked as 'abstract'
                //     internal static abstract event System.Action R;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "R").WithArguments("abstract").WithLocation(9, 50),
                // (9,50): warning CS0067: The event 'B.R' is never used
                //     internal static abstract event System.Action R;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "R").WithArguments("B.R").WithLocation(9, 50)
                );
        }

        [Fact]
        public void CS0113ERR_OverrideNotNew01()
        {
            var text = @"namespace MyNamespace
{
    abstract public class MyClass
    {
        public abstract void MyMethod();
        public abstract void MyMethod(int x); 
        public virtual void MyMethod(int x, long j)
        {
        }
    }

    public class MyClass2 : MyClass
    {
        override new public void MyMethod()   // CS0113, remove new keyword
        {
        }
        virtual override public void MyMethod(int x)   // CS0113, remove virtual keyword
        {
        }
        virtual override public void MyMethod(int x, long j)   // CS0113, remove virtual keyword
        {
        }
        public static int Main()
        {
            return 0;
        }
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotNew, Line = 14, Column = 34 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotNew, Line = 17, Column = 38 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotNew, Line = 20, Column = 38 });
        }

        [Fact]
        public void CS0113ERR_OverrideNotNew02()
        {
            var text =
@"abstract class A
{
    protected abstract object P { get; }
    internal virtual object Q { get; set; }
}
class B : A
{
    protected new override object P { get { return null; } }
    internal virtual override object Q { get; set; }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotNew, Line = 8, Column = 35 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotNew, Line = 9, Column = 38 });
        }

        [Fact]
        public void CS0113ERR_OverrideNotNew03()
        {
            var text =
@"abstract class A
{
    protected abstract event System.Action P;
    internal virtual event System.Action Q;
}
class B : A
{
    protected new override event System.Action P;
    internal virtual override event System.Action Q;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,51): error CS0113: A member 'B.Q' marked as override cannot be marked as new or virtual
                //     internal virtual override event System.Action Q;
                Diagnostic(ErrorCode.ERR_OverrideNotNew, "Q").WithArguments("B.Q").WithLocation(9, 51),
                // (8,48): error CS0113: A member 'B.P' marked as override cannot be marked as new or virtual
                //     protected new override event System.Action P;
                Diagnostic(ErrorCode.ERR_OverrideNotNew, "P").WithArguments("B.P").WithLocation(8, 48),
                // (8,48): warning CS0067: The event 'B.P' is never used
                //     protected new override event System.Action P;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P").WithArguments("B.P").WithLocation(8, 48),
                // (9,51): warning CS0067: The event 'B.Q' is never used
                //     internal virtual override event System.Action Q;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Q").WithArguments("B.Q").WithLocation(9, 51),
                // (4,42): warning CS0067: The event 'A.Q' is never used
                //     internal virtual event System.Action Q;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Q").WithArguments("A.Q").WithLocation(4, 42));
        }

        [Fact]
        public void CS0115ERR_OverrideNotExpected()
        {
            var text = @"namespace MyNamespace
{
    abstract public class MyClass1
    {
        public abstract int f();
    }

    abstract public class MyClass2
    {
        public override int f()   // CS0115
        {
            return 0;
        }

        public static void Main()
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 10, Column = 29 });
        }

        /// <summary>
        /// Some?
        /// </summary>
        [Fact]
        public void CS0118ERR_BadSKknown01()
        {
            var text = @"namespace NS
{
    namespace Goo {}

    internal struct S
    {
        void Goo(Goo f) {}
    }

    class Bar
    {
        Goo foundNamespaceInsteadOfType;
    }

    public class A : Goo {}
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (7,18): error CS0118: 'Goo' is a namespace but is used like a type
                //         void Goo(Goo f) {}
                Diagnostic(ErrorCode.ERR_BadSKknown, "Goo").WithArguments("Goo", "namespace", "type"),
                // (12,9): error CS0118: 'Goo' is a namespace but is used like a type
                //         Goo foundNamespaceInsteadOfType;
                Diagnostic(ErrorCode.ERR_BadSKknown, "Goo").WithArguments("Goo", "namespace", "type"),
                // (15,22): error CS0118: 'Goo' is a namespace but is used like a type
                //     public class A : Goo {}
                Diagnostic(ErrorCode.ERR_BadSKknown, "Goo").WithArguments("Goo", "namespace", "type"),
                // (12,13): warning CS0169: The field 'NS.Bar.foundNamespaceInsteadOfType' is never used
                //         Goo foundNamespaceInsteadOfType;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "foundNamespaceInsteadOfType").WithArguments("NS.Bar.foundNamespaceInsteadOfType")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var baseType = ns.GetTypeMembers("A").Single().BaseType();
            Assert.Equal("Goo", baseType.Name);
            Assert.Equal(TypeKind.Error, baseType.TypeKind);
            Assert.Null(baseType.BaseType());

            var type2 = ns.GetTypeMembers("Bar").Single() as NamedTypeSymbol;
            var mem1 = type2.GetMembers("foundNamespaceInsteadOfType").Single() as FieldSymbol;
            Assert.Equal("Goo", mem1.Type.Name);
            Assert.Equal(TypeKind.Error, mem1.Type.TypeKind);

            var type3 = ns.GetTypeMembers("S").Single() as NamedTypeSymbol;
            var mem2 = type3.GetMembers("Goo").Single() as MethodSymbol;
            var param = mem2.Parameters[0];
            Assert.Equal("Goo", param.Type.Name);
            Assert.Equal(TypeKind.Error, param.Type.TypeKind);
        }

        [WorkItem(538147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538147")]
        [Fact]
        public void CS0118ERR_BadSKknown02()
        {
            var text = @"
class Test
{
    static void Main()
    {
         B B = null;
         if (B == B) {}
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadSKknown, Line = 6, Column = 10 });
        }

        [Fact]
        public void CS0119ERR_BadSKunknown01()
        {
            var text = @"namespace NS
{
    using System;
    public class Test
    {
        public static void F() { }

        public static int Main()
        {
            Console.WriteLine(F.x);
            return NS();
        }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
    // (10,31): error CS0119: 'NS.Test.F()' is a method, which is not valid in the given context
    //             Console.WriteLine(F.x);
    Diagnostic(ErrorCode.ERR_BadSKunknown, "F").WithArguments("NS.Test.F()", "method"),
    // (11,20): error CS0118: 'NS' is a namespace but is used like a variable
    //             return NS();
    Diagnostic(ErrorCode.ERR_BadSKknown, "NS").WithArguments("NS", "namespace", "variable")
           );
        }

        [Fact, WorkItem(538214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538214")]
        public void CS0119ERR_BadSKunknown02()
        {
            var text = @"namespace N1
{
    class Test
    {
        static void Main()
        {
            double x = -5d;
            int y = (global::System.Int32) +x;
            short z = (System.Int16) +x;
        }
    }
}
";
            // Roslyn gives same error twice
            CreateCompilation(text).VerifyDiagnostics(
    // (8,22): error CS0119: 'int' is a type, which is not valid in the given context
    //             int y = (global::System.Int32) +x;
    Diagnostic(ErrorCode.ERR_BadSKunknown, "global::System.Int32").WithArguments("int", "type"),
    // (8,22): error CS0119: 'int' is a type, which is not valid in the given context
    //             int y = (global::System.Int32) +x;
    Diagnostic(ErrorCode.ERR_BadSKunknown, "global::System.Int32").WithArguments("int", "type"),
    // (9,24): error CS0119: 'short' is a type, which is not valid in the given context
    //             short z = (System.Int16) +x;
    Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int16").WithArguments("short", "type"),
    // (9,24): error CS0119: 'short' is a type, which is not valid in the given context
    //             short z = (System.Int16) +x;
    Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int16").WithArguments("short", "type")
            );
        }

        [Fact]
        public void CS0132ERR_StaticConstParam01()
        {
            var text = @"namespace NS
{
    struct S
    {
        static S(string s) { }
    }
    
    public class @clx
    {
        static clx(params long[] ary) { }
        static clx(ref int n) { }
    }

    public class @cly : clx
    {
        static cly() { }
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstParam, Line = 5, Column = 16 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstParam, Line = 10, Column = 16 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstParam, Line = 11, Column = 16 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [WorkItem(539627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539627")]
        [Fact]
        public void CS0136ERR_LocalIllegallyOverrides()
        {
            // See comments in NameCollisionTests.cs for commentary on this error.

            var text = @"

class MyClass
{
    public MyClass(int a)
    {
        long a; // 0136
    }

    public long MyMeth(string x)
    {
        long x = 1; // 0136
        return x;
    }

    public byte MyProp
    {
        set
        {
            int value; // 0136
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                    // (7,14): error CS0136: A local or parameter named 'a' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //         long a; // 0136
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a").WithArguments("a").WithLocation(7, 14),
                    // (7,14): warning CS0168: The variable 'a' is declared but never used
                    //         long a; // 0136
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(7, 14),
                    // (12,14): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //         long x = 1; // 0136
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(12, 14),
                    // (20,17): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //             int value; // 0136
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(20, 17),
                    // (20,17): warning CS0168: The variable 'value' is declared but never used
                    //             int value; // 0136
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "value").WithArguments("value").WithLocation(20, 17));
        }

        [Fact]
        public void CS0136ERR_LocalIllegallyOverrides02()
        {
            // See comments in NameCollisionTests.cs for commentary on this error.
            var text = @"class C
{
    public static void Main()
    {
        foreach (var x in ""abc"")
        {
            int x = 1 ;
            System.Console.WriteLine(x);
        }
    }
}
";
            CreateCompilation(text).
               VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x"));
        }

        [Fact]
        public void CS0138ERR_BadUsingNamespace01()
        {
            var text = @"using System.Object;

namespace NS
{
    using NS.S;

    struct S {}
}";
            var comp = CreateCompilation(Parse(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
            comp.VerifyDiagnostics(
                // (1,7): error CS0138: A using namespace directive can only be applied to namespaces; 'object' is a type not a namespace
                // using System.Object;
                Diagnostic(ErrorCode.ERR_BadUsingNamespace, "System.Object").WithArguments("object"),
                // (5,11): error CS0138: A using namespace directive can only be applied to namespaces; 'NS.S' is a type not a namespace
                //     using NS.S;
                Diagnostic(ErrorCode.ERR_BadUsingNamespace, "NS.S").WithArguments("NS.S"),
                // (1,1): info CS8019: Unnecessary using directive.
                // using System.Object;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Object;"),
                // (5,5): info CS8019: Unnecessary using directive.
                //     using NS.S;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NS.S;"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        /// <summary>
        /// Roslyn has 3 extra CS0146, Neal said it's per spec
        /// </summary>
        [Fact]
        public void CS0146ERR_CircularBase01()
        {
            var text = @"namespace NS
{
    class A : B { }
    class B : A { }

    public class AA : BB { }
    public class BB : CC { }
    public class CC : DD { }
    public class DD : BB { }
}
";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CircularBase, Line = 3, Column = 11 }, // Roslyn extra
                new ErrorDescription { Code = (int)ErrorCode.ERR_CircularBase, Line = 4, Column = 11 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CircularBase, Line = 7, Column = 18 }, // Roslyn extra
                new ErrorDescription { Code = (int)ErrorCode.ERR_CircularBase, Line = 8, Column = 18 }, // Roslyn extra
                new ErrorDescription { Code = (int)ErrorCode.ERR_CircularBase, Line = 9, Column = 18 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var baseType = (NamedTypeSymbol)ns.GetTypeMembers("A").Single().BaseType();
            Assert.Null(baseType.BaseType());
            Assert.Equal("B", baseType.Name);
            Assert.Equal(TypeKind.Error, baseType.TypeKind);

            baseType = (NamedTypeSymbol)ns.GetTypeMembers("DD").Single().BaseType();
            Assert.Null(baseType.BaseType());
            Assert.Equal("BB", baseType.Name);
            Assert.Equal(TypeKind.Error, baseType.TypeKind);

            baseType = (NamedTypeSymbol)ns.GetTypeMembers("BB").Single().BaseType();
            Assert.Null(baseType.BaseType());
            Assert.Equal("CC", baseType.Name);
            Assert.Equal(TypeKind.Error, baseType.TypeKind);
        }

        [Fact]
        public void CS0146ERR_CircularBase02()
        {
            var text = @"public interface C<T>
{
}
public class D : C<D.Q>
{
   private class Q { } // accessible in base clause
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [WorkItem(4169, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0146ERR_CircularBase03()
        {
            var text = @"
class A : object, A.IC
{
    protected interface IC { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [WorkItem(4169, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0146ERR_CircularBase04()
        {
            var text = @"
class A : object, I<A.IC>
{
    protected interface IC { }
}

interface I<T> { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void CS0179ERR_ExternHasBody01()
        {
            var text = @"
namespace NS
{
    public class C
    {
        extern C() { }
        extern void M1() { }
        extern int M2() => 1;
        extern object P1 { get { return null; } set { } }
        extern int P2 => 1;
        extern event System.Action E { add { } remove { } }
        extern static public int operator + (C c1, C c2) { return 1; }
        extern static public int operator - (C c1, C c2) => 1;
    }
}
";
            var comp = CreateCompilation(text);

            comp.VerifyDiagnostics(
                // (6,16): error CS0179: 'C.C()' cannot be extern and declare a body
                //         extern C() { }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "C").WithArguments("NS.C.C()").WithLocation(6, 16),
                // (7,21): error CS0179: 'C.M1()' cannot be extern and declare a body
                //         extern void M1() { }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "M1").WithArguments("NS.C.M1()").WithLocation(7, 21),
                // (8,20): error CS0179: 'C.M2()' cannot be extern and declare a body
                //         extern int M2() => 1;
                Diagnostic(ErrorCode.ERR_ExternHasBody, "M2").WithArguments("NS.C.M2()").WithLocation(8, 20),
                // (9,28): error CS0179: 'C.P1.get' cannot be extern and declare a body
                //         extern object P1 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("NS.C.P1.get").WithLocation(9, 28),
                // (9,49): error CS0179: 'C.P1.set' cannot be extern and declare a body
                //         extern object P1 { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "set").WithArguments("NS.C.P1.set").WithLocation(9, 49),
                // (10,26): error CS0179: 'C.P2.get' cannot be extern and declare a body
                //         extern int P2 => 1;
                Diagnostic(ErrorCode.ERR_ExternHasBody, "1").WithArguments("NS.C.P2.get").WithLocation(10, 26),
                // (11,40): error CS0179: 'C.E.add' cannot be extern and declare a body
                //         extern event System.Action E { add { } remove { } }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("NS.C.E.add").WithLocation(11, 40),
                // (11,48): error CS0179: 'C.E.remove' cannot be extern and declare a body
                //         extern event System.Action E { add { } remove { } }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("NS.C.E.remove").WithLocation(11, 48),
                // (12,43): error CS0179: 'C.operator +(C, C)' cannot be extern and declare a body
                //         extern static public int operator + (C c1, C c2) { return 1; }
                Diagnostic(ErrorCode.ERR_ExternHasBody, "+").WithArguments("NS.C.operator +(NS.C, NS.C)").WithLocation(12, 43),
                // (13,43): error CS0179: 'C.operator -(C, C)' cannot be extern and declare a body
                //         extern static public int operator - (C c1, C c2) => 1;
                Diagnostic(ErrorCode.ERR_ExternHasBody, "-").WithArguments("NS.C.operator -(NS.C, NS.C)").WithLocation(13, 43));
        }

        [Fact]
        public void CS0180ERR_AbstractAndExtern01()
        {
            CreateCompilation(
@"abstract class X
{
    public abstract extern void M();
    public extern abstract int P { get; }
    // If a body is provided for an abstract extern method,
    // Dev10 reports CS0180, but does not report CS0179/CS0500.
    public abstract extern void N(int i) { }
    public extern abstract object Q { set { } }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_AbstractAndExtern, "M").WithArguments("X.M()").WithLocation(3, 33),
                    Diagnostic(ErrorCode.ERR_AbstractAndExtern, "P").WithArguments("X.P").WithLocation(4, 32),
                    Diagnostic(ErrorCode.ERR_AbstractAndExtern, "N").WithArguments("X.N(int)").WithLocation(7, 33),
                    Diagnostic(ErrorCode.ERR_AbstractAndExtern, "Q").WithArguments("X.Q").WithLocation(8, 35));
        }

        [WorkItem(527618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527618")]
        [Fact]
        public void CS0180ERR_AbstractAndExtern02()
        {
            CreateCompilation(
@"abstract class C
{
    public extern abstract void M();
    public extern abstract object P { set; }
}
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_AbstractAndExtern, "M").WithArguments("C.M()"),
                    Diagnostic(ErrorCode.ERR_AbstractAndExtern, "P").WithArguments("C.P"));
        }

        [Fact]
        public void CS0180ERR_AbstractAndExtern03()
        {
            CreateCompilation(
@"class C
{
    public extern abstract event System.Action E;
}
")
                .VerifyDiagnostics(
                // (3,48): error CS0180: 'C.E' cannot be both extern and abstract
                //     public extern abstract event System.Action E;
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "E").WithArguments("C.E"));
        }

        [Fact]
        public void CS0181ERR_BadAttributeParamType_Dynamic()
        {
            var text = @"
using System;

public class C<T> { public enum D { A } }

[A1]                                             // Dev11 error
public class A1 : Attribute                  
{
    public A1(dynamic i = null) { }              
}

[A2]                                             // Dev11 ok (bug)
public class A2 : Attribute                      
{
    public A2(dynamic[] i = null) { }
}
 
[A3]                                             // Dev11 error (bug)
public class A3 : Attribute                      
{
    public A3(C<dynamic>.D i = 0) { }
}

[A4]                                             // Dev11 ok
public class A4 : Attribute
{
    public A4(C<dynamic>.D[] i = null) { }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (6,2): error CS0181: Attribute constructor parameter 'i' has type 'dynamic', which is not a valid attribute parameter type
                // [A1]                                             // Dev11 error
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A1").WithArguments("i", "dynamic"),
                // (12,2): error CS0181: Attribute constructor parameter 'i' has type 'dynamic[]', which is not a valid attribute parameter type
                // [A2]                                             // Dev11 ok
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A2").WithArguments("i", "dynamic[]"));
        }

        [Fact]
        public void CS0182ERR_BadAttributeArgument()
        {
            var text = @"public class MyClass
{
    static string s = ""Test"";

    [System.Diagnostics.ConditionalAttribute(s)]   // CS0182
    void NonConstantArgumentToConditional()
    {
    }

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,46): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [System.Diagnostics.ConditionalAttribute(s)]   // CS0182
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "s"),
                // (3,19): warning CS0414: The field 'MyClass.s' is assigned but its value is never used
                //     static string s = "Test";
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s").WithArguments("MyClass.s"));
        }

        [Fact]
        public void CS0214ERR_UnsafeNeeded()
        {
            var text = @"public struct S
{
    public int a;
}

public class MyClass
{
    public static void Test()
    {
        S s = new S();
        S* s2 = &s;    // CS0214
        s2->a = 3;      // CS0214
        s.a = 0;
    }

    // OK
    unsafe public static void Test2()
    {
        S s = new S();
        S* s2 = &s;
        s2->a = 3;
        s.a = 0;
    }
}
";
            // NOTE: only first in scope is reported.
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (11,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         S* s2 = &s;    // CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "S*"),
                // (11,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         S* s2 = &s;    // CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&s"),
                // (12,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         s2->a = 3;      // CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s2"));
        }

        [Fact]
        public void CS0214ERR_UnsafeNeeded02()
        {
            var text =
@"unsafe struct S
{
    public fixed int x[10];
}

class Program
{
    static void Main()
    {
        S s;
        s.x[1] = s.x[2];
    }
}";
            // NOTE: only first in scope is reported.
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (11,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         s.x[1] = s.x[2];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.x"),
                // (11,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         s.x[1] = s.x[2];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.x"));
        }

        [Fact]
        public void CS0214ERR_UnsafeNeeded03()
        {
            var text = @"public struct S
{
    public fixed int buf[10];
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public fixed int buf[10];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "buf[10]"));
        }

        [Fact]
        public void CS0214ERR_UnsafeNeeded04()
        {
            var text = @"
namespace System
{
    public class TestType
    {
        public void TestMethod()
        {
            var x = stackalloc int[10];         // ERROR
            Span<int> y = stackalloc int[10];   // OK
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             var x = stackalloc int[10];    // ERROR
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[10]").WithLocation(8, 21));
        }

        [Fact]
        public void CS0215ERR_OpTFRetType()
        {
            var text = @"class MyClass
{
    public static int operator true(MyClass MyInt)   // CS0215
    {
        return 1;
    }

    public static int operator false(MyClass MyInt)   // CS0215
    {
        return 1;
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
// (3,32): error CS0215: The return type of operator True or False must be bool
//     public static int operator true(MyClass MyInt)   // CS0215
Diagnostic(ErrorCode.ERR_OpTFRetType, "true"),
// (8,32): error CS0215: The return type of operator True or False must be bool
//     public static int operator false(MyClass MyInt)   // CS0215
Diagnostic(ErrorCode.ERR_OpTFRetType, "false")
                );
        }

        [Fact]
        public void CS0216ERR_OperatorNeedsMatch()
        {
            var text = @"class MyClass
{
    // Missing operator false
    public static bool operator true(MyClass MyInt)   // CS0216
    { return true; }
    // Missing matching operator > -- parameter types must match.
    public static bool operator < (MyClass x, int y) 
    { return false; }
    // Missing matching operator < -- parameter types must match.
    public static bool operator > (MyClass x, double y) 
    { return false; }
    // Missing matching operator >= -- return types must match.
    public static MyClass operator <=(MyClass x, MyClass y)
    { return x; }
    // Missing matching operator <= -- return types must match.
    public static bool operator >=(MyClass x, MyClass y)
    { return true; }
    // Missing operator !=
    public static bool operator ==(MyClass x, MyClass y)
    { return true; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
// (1,7): warning CS0660: 'MyClass' defines operator == or operator != but does not override Object.Equals(object o)
// class MyClass
Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "MyClass").WithArguments("MyClass"),
// (1,7): warning CS0661: 'MyClass' defines operator == or operator != but does not override Object.GetHashCode()
// class MyClass
Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "MyClass").WithArguments("MyClass"),
                // (4,33): error CS0216: The operator 'MyClass.operator true(MyClass)' requires a matching operator 'false' to also be defined
                //     public static bool operator true(MyClass MyInt)   // CS0216
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "true").WithArguments("MyClass.operator true(MyClass)", "false"),
                // (73): error CS0216: The operator 'MyClass.operator <(MyClass, int)' requires a matching operator '>' to also be defined
                //     public static bool operator < (MyClass x, int y) 
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<").WithArguments("MyClass.operator <(MyClass, int)", ">"),
                // (10,33): error CS0216: The operator 'MyClass.operator >(MyClass, double)' requires a matching operator '<' to also be defined
                //     public static bool operator > (MyClass x, double y) 
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">").WithArguments("MyClass.operator >(MyClass, double)", "<"),
                // (13,36): error CS0216: The operator 'MyClass.operator <=(MyClass, MyClass)' requires a matching operator '>=' to also be defined
                //     public static MyClass operator <=(MyClass x, MyClass y)
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<=").WithArguments("MyClass.operator <=(MyClass, MyClass)", ">="),
                // (16,33): error CS0216: The operator 'MyClass.operator >=(MyClass, MyClass)' requires a matching operator '<=' to also be defined
                //     public static bool operator >=(MyClass x, MyClass y)
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">=").WithArguments("MyClass.operator >=(MyClass, MyClass)", "<="),
                // (19,33): error CS0216: The operator 'MyClass.operator ==(MyClass, MyClass)' requires a matching operator '!=' to also be defined
                //     public static bool operator ==(MyClass x, MyClass y)
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "==").WithArguments("MyClass.operator ==(MyClass, MyClass)", "!=")
                );
        }

        [Fact]
        public void CS0216ERR_OperatorNeedsMatch_NoErrorForDynamicObject()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    public static object operator >(C p1, dynamic p2)
    {
        return null;
    }

    public static dynamic operator <(C p1, object p2)
    {
        return null;
    }
    public static dynamic operator >=(C p1, dynamic p2)
    {
        return null;
    }

    public static object operator <=(C p1, object p2)
    {
        return null;
    }

    public static List<object> operator ==(C p1, dynamic[] p2)
    {
        return null;
    }

    public static List<dynamic> operator !=(C p1, object[] p2)
    {
        return null;
    }

    public override bool Equals(object o) { return false; } 
    public override int GetHashCode() { return 1; }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void CS0218ERR_MustHaveOpTF()
        {
            // Note that the wording of this error has changed.

            var text = @"
public class MyClass
{
    public static MyClass operator &(MyClass f1, MyClass f2)
    {
        return new MyClass();
    }

    public static void Main()
    {
        MyClass f = new MyClass();
        MyClass i = f && f;   // CS0218, requires operators true and false
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (12,21): error CS0218: In order to be applicable as a short circuit operator, the declaring type 'MyClass' of user-defined operator 'MyClass.operator &(MyClass, MyClass)' must declare operator true and operator false.
//         MyClass i = f && f;   // CS0218, requires operators true and false
Diagnostic(ErrorCode.ERR_MustHaveOpTF, "f && f").WithArguments("MyClass.operator &(MyClass, MyClass)", "MyClass")
                );
        }

        [Fact]
        public void CS0224ERR_BadVarargs01()
        {
            var text = @"namespace NS
{
    class C
    {
        public static void F<T>(T x, __arglist) { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVarargs, Line = 5, Column = 28 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0224ERR_BadVarargs02()
        {
            var text = @"class C
{
    C(object o, __arglist) { } // no error
    void M(__arglist) { } // no error
}
abstract class C<T>
{
    C(object o) { } // no error
    C(__arglist) { }
    void M(object o, __arglist) { }
    internal abstract object F(__arglist);
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVarargs, Line = 9, Column = 5 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVarargs, Line = 10, Column = 10 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadVarargs, Line = 11, Column = 30 });
        }

        [Fact]
        public void CS0225ERR_ParamsMustBeCollection01()
        {
            var text = @"
using System.Collections.Generic;

public class A
{
    struct S
    {
        internal List<string> Bar(string s1, params List<string> s2) 
        {
            return s2;
        }
    }
    public static void Goo(params int a) {}

    public static int Main()
    {
        Goo(1);
        return 1;
    }
}
";
            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (13,28): error CS0225: The params parameter must have a valid collection type
                //     public static void Goo(params int a) {}
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(13, 28)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetTypeMembers("A").Single() as NamedTypeSymbol;
            // TODO...
        }

        [Fact]
        public void CS0227ERR_IllegalUnsafe()
        {
            var text = @"public class MyClass
{
    unsafe public static void Main()   // CS0227
    {
    }
}
";
            var c = CreateCompilation(text, options: TestOptions.ReleaseDll.WithAllowUnsafe(false));
            c.VerifyDiagnostics(
                // (3,31): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     unsafe public static void Main()   // CS0227
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "Main").WithLocation(3, 31));
        }

        [Fact]
        public void CS0234ERR_DottedTypeNameNotFoundInNS()
        {
            var text =
@"using NA = N.A;
using NB = C<N.B<object>>;
namespace N { }
class C<T>
{
    NA a;
    NB b;
    N.C<N.D> c;
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (2,16): error CS0234: The type or namespace name 'B<>' does not exist in the namespace 'N' (are you missing an assembly reference?)
                // using NB = C<N.B<object>>;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "B<object>").WithArguments("B<>", "N").WithLocation(2, 16),
                // (1,14): error CS0234: The type or namespace name 'A' does not exist in the namespace 'N' (are you missing an assembly reference?)
                // using NA = N.A;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "A").WithArguments("A", "N").WithLocation(1, 14),
                // (8,7): error CS0234: The type or namespace name 'C<>' does not exist in the namespace 'N' (are you missing an assembly reference?)
                //     N.C<N.D> c;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "C<N.D>").WithArguments("C<>", "N").WithLocation(8, 7),
                // (8,11): error CS0234: The type or namespace name 'D' does not exist in the namespace 'N' (are you missing an assembly reference?)
                //     N.C<N.D> c;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "D").WithArguments("D", "N").WithLocation(8, 11),
                // (6,8): warning CS0169: The field 'C<T>.a' is never used
                //     NA a;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("C<T>.a").WithLocation(6, 8),
                // (7,8): warning CS0169: The field 'C<T>.b' is never used
                //     NB b;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b").WithArguments("C<T>.b").WithLocation(7, 8),
                // (8,14): warning CS0169: The field 'C<T>.c' is never used
                //     N.C<N.D> c;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "c").WithArguments("C<T>.c").WithLocation(8, 14)
                );
        }

        [Fact]
        public void CS0238ERR_SealedNonOverride01()
        {
            var text = @"abstract class MyClass
{
    public abstract void f();
}

class MyClass2 : MyClass
{
    public static void Main()
    {
    }

    public sealed void f() // CS0238
    {
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SealedNonOverride, Line = 12, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 12, Column = 24, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 6, Column = 7 });
        }

        [Fact]
        public void CS0238ERR_SealedNonOverride02()
        {
            var text = @"interface I
{
    sealed void M();
    sealed object P { get; }
}
";
            //we're diverging from Dev10 - it's a little silly to report two errors saying the same modifier isn't allowed
            CreateCompilation(text, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (3,17): error CS8503: The modifier 'sealed' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     sealed void M();
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M").WithArguments("sealed", "7.0", "8.0").WithLocation(3, 17),
                // (4,19): error CS8503: The modifier 'sealed' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     sealed object P { get; }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "P").WithArguments("sealed", "7.0", "8.0").WithLocation(4, 19),
                // (4,23): error CS0501: 'I.P.get' must declare a body because it is not marked abstract, extern, or partial
                //     sealed object P { get; }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I.P.get").WithLocation(4, 23),
                // (3,17): error CS0501: 'I.M()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed void M();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("I.M()").WithLocation(3, 17)
                );
        }

        [Fact]
        public void CS0238ERR_SealedNonOverride03()
        {
            var text =
@"class B
{
    sealed int P { get; set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SealedNonOverride, Line = 3, Column = 16 });
        }

        [Fact]
        public void CS0238ERR_SealedNonOverride04()
        {
            var text =
@"class B
{
    sealed event System.Action E;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,32): error CS0238: 'B.E' cannot be sealed because it is not an override
                //     sealed event System.Action E;
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "E").WithArguments("B.E"),
                // (3,32): warning CS0067: The event 'B.E' is never used
                //     sealed event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("B.E"));
        }

        [Fact]
        public void CS0239ERR_CantOverrideSealed()
        {
            var text = @"abstract class MyClass
{
    public abstract void f();
}

class MyClass2 : MyClass
{
    public static void Main()
    {
    }

    public override sealed void f()
    {
    }
}

class MyClass3 : MyClass2
{
    public override void f()   // CS0239
    {
    }
}

";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideSealed, Line = 19, Column = 26 });
        }

        [Fact()]
        public void CS0243ERR_ConditionalOnOverride()
        {
            var text = @"public class MyClass
{
    public virtual void M() { }
}

public class MyClass2 : MyClass
{
    [System.Diagnostics.ConditionalAttribute(""MySymbol"")]   // CS0243
    public override void M() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,6): error CS0243: The Conditional attribute is not valid on 'MyClass2.M()' because it is an override method
                //     [System.Diagnostics.ConditionalAttribute("MySymbol")]   // CS0243
                Diagnostic(ErrorCode.ERR_ConditionalOnOverride, @"System.Diagnostics.ConditionalAttribute(""MySymbol"")").WithArguments("MyClass2.M()").WithLocation(8, 6));
        }

        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound01()
        {
            var text = @"namespace NS
{
  interface IGoo : INotExist {} // Extra CS0527
  interface IBar
  {
    string M(ref NoType p1, out NoType p2, params NOType[] ary);
  }
  class A : CNotExist {}
  struct S
  {
     public const NoType field = 123;
     private NoType M() {
        return null;
     }
  }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 3, Column = 20 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 6, Column = 18 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 6, Column = 33 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 6, Column = 51 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 8, Column = 13 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 11, Column = 19 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 12, Column = 14 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("IGoo").Single() as NamedTypeSymbol;
            // bug: expected 1 but error symbol
            // Assert.Equal(1, type1.Interfaces().Count());

            var type2 = ns.GetTypeMembers("IBar").Single() as NamedTypeSymbol;
            var mem1 = type2.GetMembers().First() as MethodSymbol;
            //ErrorTypes now appear as though they are declared in the global namespace. 
            Assert.Equal("System.String NS.IBar.M(ref NoType p1, out NoType p2, params NOType[] ary)", mem1.ToTestDisplayString());
            var param = mem1.Parameters[0] as ParameterSymbol;
            var ptype = param.TypeWithAnnotations;
            Assert.Equal(RefKind.Ref, param.RefKind);
            Assert.Equal(TypeKind.Error, ptype.Type.TypeKind);
            Assert.Equal("NoType", ptype.Type.Name);

            var type3 = ns.GetTypeMembers("A").Single() as NamedTypeSymbol;
            var base1 = type3.BaseType();
            Assert.Null(base1.BaseType());
            Assert.Equal(TypeKind.Error, base1.TypeKind);
            Assert.Equal("CNotExist", base1.Name);

            var type4 = ns.GetTypeMembers("S").Single() as NamedTypeSymbol;
            var mem2 = type4.GetMembers("field").First() as FieldSymbol;
            Assert.Equal(TypeKind.Error, mem2.Type.TypeKind);
            Assert.Equal("NoType", mem2.Type.Name);
            var mem3 = type4.GetMembers("M").Single() as MethodSymbol;
            Assert.Equal(TypeKind.Error, mem3.ReturnType.TypeKind);
            Assert.Equal("NoType", mem3.ReturnType.Name);
        }

        [WorkItem(537882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537882")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound02()
        {
            var text = @"using NoExistNS1;

namespace NS
{
    using NoExistNS2; // No error for this one

    class Test
    {
        static int Main()
        {
            return 1;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,7): error CS0246: The type or namespace name 'NoExistNS1' could not be found (are you missing a using directive or an assembly reference?)
                // using NoExistNS1;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NoExistNS1").WithArguments("NoExistNS1"),
                // (5,11): error CS0246: The type or namespace name 'NoExistNS2' could not be found (are you missing a using directive or an assembly reference?)
                //     using NoExistNS2; // No error for this one
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NoExistNS2").WithArguments("NoExistNS2"),
                // (1,1): info CS8019: Unnecessary using directive.
                // using NoExistNS1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NoExistNS1;"),
                // (5,5): info CS8019: Unnecessary using directive.
                //     using NoExistNS2; // No error for this one
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NoExistNS2;"));
        }

        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound03()
        {
            var text =
@"[Attribute] class C { }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,2): error CS0246: The type or namespace name 'AttributeAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [Attribute] class C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attribute").WithArguments("AttributeAttribute").WithLocation(1, 2),
                // (1,2): error CS0246: The type or namespace name 'Attribute' could not be found (are you missing a using directive or an assembly reference?)
                // [Attribute] class C { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attribute").WithArguments("Attribute").WithLocation(1, 2));
        }

        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound04()
        {
            var text =
@"class AAttribute : System.Attribute { }
class BAttribute : System.Attribute { }
[A][@B] class C { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 3, Column = 5 });
        }

        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound05()
        {
            var text =
@"class C
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(typeof(s)); // Invalid
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "s").WithArguments("s"));
        }

        [WorkItem(543791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543791")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound06()
        {
            var text =
@"class C
{
    public static Nada x = null, y = null;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,19): error CS0246: The type or namespace name 'Nada' could not be found (are you missing a using directive or an assembly reference?)
                //     public static Nada x = null, y = null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Nada").WithArguments("Nada")
                );
        }

        [Fact, WorkItem(69700, "https://github.com/dotnet/roslyn/issues/69700")]
        public void CS0246ERR_SingleTypeNameNotFound07()
        {
            var text =
@"class C
{
    [SomeAttribute<int>]
    static void M()
    {
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (3,6): error CS0246: The type or namespace name 'SomeAttributeAttribute<>' could not be found (are you missing a using directive or an assembly reference?)
                //      [SomeAttribute<int>]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SomeAttribute<int>").WithArguments("SomeAttributeAttribute<>"),
                // (3,6): error CS0246: The type or namespace name 'SomeAttribute<>' could not be found (are you missing a using directive or an assembly reference?)
                //      [SomeAttribute<int>]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SomeAttribute<int>").WithArguments("SomeAttribute<>")
                );
        }

        [Fact, WorkItem(69700, "https://github.com/dotnet/roslyn/issues/69700")]
        public void CS0246ERR_SingleTypeNameNotFound08()
        {
            var text =
@"class C
{
    [Some<int>]
    static void M()
    {
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                // (3,6): error CS0246: The type or namespace name 'SomeAttribute<>' could not be found (are you missing a using directive or an assembly reference?)
                //      [Some<int>]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Some<int>").WithArguments("SomeAttribute<>"),
                // (3,6): error CS0246: The type or namespace name 'Some<>' could not be found (are you missing a using directive or an assembly reference?)
                //      [Some<int>]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Some<int>").WithArguments("Some<>")
                );
        }

        [Fact]
        public void CS0249ERR_OverrideFinalizeDeprecated()
        {
            var text = @"class MyClass
{
    protected override void Finalize()   // CS0249
    {
    }

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,29): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (3,29): error CS0249: Do not override object.Finalize. Instead, provide a destructor.
                Diagnostic(ErrorCode.ERR_OverrideFinalizeDeprecated, "Finalize"));
        }

        [Fact]
        public void CS0260ERR_MissingPartial01()
        {
            var text = @"namespace NS
{
    public class C  // CS0260
    {
        partial struct S { }
        struct S { } // CS0260
    }

    public partial class C {}
    public partial class C {}

    partial interface I {}
    interface I { } // CS0260
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_MissingPartial, Line = 3, Column = 18 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_MissingPartial, Line = 6, Column = 16 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_MissingPartial, Line = 13, Column = 15 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0261ERR_PartialTypeKindConflict01()
        {
            var text = @"namespace NS
{
    partial class A { }
    partial class A { }
    partial struct A { } // CS0261
    partial interface A { } // CS0261
    partial class B { }
    partial struct B<T> { }
    partial interface B<T, U> { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialTypeKindConflict, Line = 5, Column = 20 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialTypeKindConflict, Line = 6, Column = 23 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0262ERR_PartialModifierConflict01()
        {
            var text = @"namespace NS
{
    public partial interface I { }
    internal partial interface I { }
    partial interface I { }

    class A
    {
        internal partial class C { }
        protected partial class C {}

        private partial struct S { }
        internal partial struct S { }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,30): error CS0262: Partial declarations of 'I' have conflicting accessibility modifiers
                //     public partial interface I { }
                Diagnostic(ErrorCode.ERR_PartialModifierConflict, "I").WithArguments("NS.I").WithLocation(3, 30),
                // (9,32): error CS0262: Partial declarations of 'A.C' have conflicting accessibility modifiers
                //         internal partial class C { }
                Diagnostic(ErrorCode.ERR_PartialModifierConflict, "C").WithArguments("NS.A.C").WithLocation(9, 32),
                // (12,32): error CS0262: Partial declarations of 'A.S' have conflicting accessibility modifiers
                //         private partial struct S { }
                Diagnostic(ErrorCode.ERR_PartialModifierConflict, "S").WithArguments("NS.A.S").WithLocation(12, 32)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0263ERR_PartialMultipleBases01()
        {
            var text = @"namespace NS
{
    class B1 { }
    class B2 { }
    partial class C : B1  // CS0263 - is the base class B1 or B2?
    {
    }

    partial class C : B2
    {
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialMultipleBases, Line = 5, Column = 19 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("C").Single() as NamedTypeSymbol;
            var base1 = type1.BaseType();
            Assert.Null(base1.BaseType());
            Assert.Equal(TypeKind.Error, base1.TypeKind);
            Assert.Equal("B1", base1.Name);
        }

        [Fact]
        public void ERRMixed_BaseAnalysisMishmash()
        {
            var text = @"namespace NS
{
    public interface I { }
    public class C { }
    public class D { }
    public struct S { }

    public struct N0 : object, NS.C { }

    public class N1 : C, D { }
    public struct N2 : C, D { }
    public class N3 : I, C { }
    public partial class N4 : C { }
    public partial class N4 : D { }

    class N5<T> : C, D { }
    class N6<T> : C, T { }
    class N7<T> : T, C { }

    interface N8 : I, I { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 8, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 8, Column = 32 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoMultipleInheritance, Line = 10, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 11, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 11, Column = 27 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BaseClassMustBeFirst, Line = 12, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialMultipleBases, Line = 13, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoMultipleInheritance, Line = 16, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DerivingFromATyVar, Line = 17, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DerivingFromATyVar, Line = 18, Column = 19 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BaseClassMustBeFirst, Line = 18, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateInterfaceInBaseList, Line = 20, Column = 23 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [Fact]
        public void CS0264ERR_PartialWrongTypeParams01()
        {
            var text = @"namespace NS
{
    public partial class C<T1>  // CS0264.cs
    {
    }

    partial class C<T2>
    {
        partial struct S<X> { }  // CS0264.cs
        partial struct S<T2> { }
    }

    internal partial interface IGoo<T, V> { } // CS0264.cs
    partial interface IGoo<T, U> { }
}
";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialWrongTypeParams, Line = 3, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialWrongTypeParams, Line = 9, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialWrongTypeParams, Line = 13, Column = 32 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("C").Single() as NamedTypeSymbol;
            Assert.Equal(1, type1.TypeParameters.Length);
            var param = type1.TypeParameters[0];
            // Assert.Equal(TypeKind.Error, param.TypeKind); // this assert it incorrect: it is definitely a type parameter
            Assert.Equal("T1", param.Name);
        }

        [Fact]
        public void PartialMethodRenameParameters()
        {
            var text = @"namespace NS
{
    public partial class MyClass
    {
        partial void F<T, U>(T t) where T : class;
        partial void F<T, U>(T tt) where T : class {}
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,22): warning CS8826: Partial method declarations 'void MyClass.F<T, U>(T t)' and 'void MyClass.F<T, U>(T tt)' have signature differences.
                //         partial void F<T, U>(T tt) where T : class {}
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F").WithArguments("void MyClass.F<T, U>(T t)", "void MyClass.F<T, U>(T tt)").WithLocation(6, 22)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("MyClass").Single() as NamedTypeSymbol;
            Assert.Equal(0, type1.TypeParameters.Length);
            var f = type1.GetMembers("F").Single() as MethodSymbol;
            Assert.Equal("T t", f.Parameters[0].ToTestDisplayString());
        }

        [Fact]
        public void PartialMethodRenameTypeParameters()
        {
            var text = @"namespace NS
{
    public partial class MyClass
    {
        partial void F<T, U>(T t) where T : class;
        partial void F<U, T>(U u) where U : class {}
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,22): warning CS8826: Partial method declarations 'void MyClass.F<T, U>(T t)' and 'void MyClass.F<U, T>(U u)' have signature differences.
                //         partial void F<U, T>(U u) where U : class {}
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F").WithArguments("void MyClass.F<T, U>(T t)", "void MyClass.F<U, T>(U u)").WithLocation(6, 22)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetTypeMembers("MyClass").Single() as NamedTypeSymbol;
            Assert.Equal(0, type1.TypeParameters.Length);
            var f = type1.GetMembers("F").Single() as MethodSymbol;
            Assert.Equal(2, f.TypeParameters.Length);
            var param1 = f.TypeParameters[0];
            var param2 = f.TypeParameters[1];
            Assert.Equal("T", param1.Name);
            Assert.Equal("U", param2.Name);
        }

        [Fact]
        public void CS0265ERR_PartialWrongConstraints01()
        {
            var text =
@"interface IA<T> { }
interface IB { }
// Different constraints.
partial class A1<T> where T : struct { }
partial class A1<T> where T : class { }
partial class A2<T, U> where T : struct where U : IA<T> { }
partial class A2<T, U> where T : class where U : IB { }
partial class A3<T> where T : IA<T> { }
partial class A3<T> where T : IA<IA<T>> { }
partial interface A4<T> where T : struct, IB { }
partial interface A4<T> where T : class, IB { }
partial struct A5<T> where T : IA<T>, new() { }
partial struct A5<T> where T : IA<T>, new() { }
partial struct A5<T> where T : IB, new() { }
// Additional constraints.
partial class B1<T> where T : new() { }
partial class B1<T> where T : class, new() { }
partial class B2<T, U> where T : IA<T> { }
partial class B2<T, U> where T : IB, IA<T> { }
// Missing constraints.
partial interface C1<T> where T : class, new() { }
partial interface C1<T> where T : new() { }
partial struct C2<T, U> where U : IB, IA<T> { }
partial struct C2<T, U> where U : IA<T> { }
// Same constraints, different order.
partial class D1<T> where T : IA<T>, IB { }
partial class D1<T> where T : IB, IA<T> { }
partial class D1<T> where T : IA<T>, IB { }
partial class D2<T, U, V> where V : T, U { }
partial class D2<T, U, V> where V : U, T { }
// Different constraint clauses.
partial class E1<T, U> where U : T { }
partial class E1<T, U> where T : class { }
partial class E1<T, U> where U : T { }
partial class E2<T, U> where U : IB { }
partial class E2<T, U> where T : IA<U> { }
partial class E2<T, U> where T : IA<U> { }
// Additional constraint clause.
partial class F1<T> { }
partial class F1<T> { }
partial class F1<T> where T : class { }
partial class F2<T> { }
partial class F2<T, U> where T : class { }
partial class F2<T, U> where T : class where U : T { }
// Missing constraint clause.
partial interface G1<T> where T : class { }
partial interface G1<T> { }
partial struct G2<T, U> where T : class where U : T { }
partial struct G2<T, U> where T : class { }
partial struct G2<T, U> { }
// Same constraint clauses, different order.
partial class H1<T, U> where T : class where U : T { }
partial class H1<T, U> where T : class where U : T { }
partial class H1<T, U> where U : T where T : class { }
partial class H2<T, U, V> where U : IB where T : IA<V> { }
partial class H2<T, U, V> where T : IA<V> where U : IB { }";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,15): error CS0265: Partial declarations of 'A1<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "A1").WithArguments("A1<T>", "T").WithLocation(4, 15),
                // (6,15): error CS0265: Partial declarations of 'A2<T, U>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "A2").WithArguments("A2<T, U>", "T").WithLocation(6, 15),
                // (6,15): error CS0265: Partial declarations of 'A2<T, U>' have inconsistent constraints for type parameter 'U'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "A2").WithArguments("A2<T, U>", "U").WithLocation(6, 15),
                // (8,15): error CS0265: Partial declarations of 'A3<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "A3").WithArguments("A3<T>", "T").WithLocation(8, 15),
                // (10,19): error CS0265: Partial declarations of 'A4<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "A4").WithArguments("A4<T>", "T").WithLocation(10, 19),
                // (12,16): error CS0265: Partial declarations of 'A5<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "A5").WithArguments("A5<T>", "T").WithLocation(12, 16),
                // (16,15): error CS0265: Partial declarations of 'B1<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "B1").WithArguments("B1<T>", "T").WithLocation(16, 15),
                // (18,15): error CS0265: Partial declarations of 'B2<T, U>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "B2").WithArguments("B2<T, U>", "T").WithLocation(18, 15),
                // (21,19): error CS0265: Partial declarations of 'C1<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "C1").WithArguments("C1<T>", "T").WithLocation(21, 19),
                // (23,16): error CS0265: Partial declarations of 'C2<T, U>' have inconsistent constraints for type parameter 'U'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "C2").WithArguments("C2<T, U>", "U").WithLocation(23, 16),
                // (32,15): error CS0265: Partial declarations of 'E1<T, U>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "E1").WithArguments("E1<T, U>", "T").WithLocation(32, 15),
                // (32,15): error CS0265: Partial declarations of 'E1<T, U>' have inconsistent constraints for type parameter 'U'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "E1").WithArguments("E1<T, U>", "U").WithLocation(32, 15),
                // (35,15): error CS0265: Partial declarations of 'E2<T, U>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "E2").WithArguments("E2<T, U>", "T").WithLocation(35, 15),
                // (35,15): error CS0265: Partial declarations of 'E2<T, U>' have inconsistent constraints for type parameter 'U'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "E2").WithArguments("E2<T, U>", "U").WithLocation(35, 15),
                // (43,15): error CS0265: Partial declarations of 'F2<T, U>' have inconsistent constraints for type parameter 'U'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "F2").WithArguments("F2<T, U>", "U").WithLocation(43, 15),
                // (48,16): error CS0265: Partial declarations of 'G2<T, U>' have inconsistent constraints for type parameter 'U'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "G2").WithArguments("G2<T, U>", "U").WithLocation(48, 16));
        }

        [Fact]
        public void CS0265ERR_PartialWrongConstraints02()
        {
            var text =
@"using NIA = N.IA;
using NIBA = N.IB<N.IA>;
using NIBAC = N.IB<N.A.IC>;
using NA1 = N.A;
using NA2 = N.A;
namespace N
{
    interface IA { }
    interface IB<T> { }
    class A
    {
        internal interface IC { }
    }
    partial class B1<T> where T : A, IB<IA> { }
    partial class B1<T> where T : N.A, N.IB<N.IA> { }
    partial class B1<T> where T : NA1, NIBA { }
    partial class B2<T> where T : NA1, IB<A.IC> { }
    partial class B2<T> where T : NA2, NIBAC { }
    partial class B3<T> where T : IB<A> { }
    partial class B3<T> where T : NIBA { }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (19,19): error CS0265: Partial declarations of 'N.B3<T>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "B3").WithArguments("N.B3<T>", "T").WithLocation(19, 19),
                // (1,1): info CS8019: Unnecessary using directive.
                // using NIA = N.IA;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NIA = N.IA;").WithLocation(1, 1));
        }

        /// <summary>
        /// Class1.dll: error CS0268: Imported type 'C1' is invalid. It contains a circular base type dependency.
        /// </summary>
        [Fact()]
        public void CS0268ERR_ImportedCircularBase01()
        {
            var text = @"namespace NS
{
    public class C3 : C1 { }
    public interface I3 : I1 { }
}
";
            var ref1 = TestReferences.SymbolsTests.CyclicInheritance.Class1;
            var ref2 = TestReferences.SymbolsTests.CyclicInheritance.Class2;

            var comp = CreateCompilation(text, new[] { ref1, ref2 });
            comp.VerifyDiagnostics(
                // (3,23): error CS0268: Imported type 'C2' is invalid. It contains a circular base type dependency.
                //     public class C3 : C1 { }
                Diagnostic(ErrorCode.ERR_ImportedCircularBase, "C1").WithArguments("C2"),
                // (4,22): error CS0268: Imported type 'I2' is invalid. It contains a circular base type dependency.
                //     public interface I3 : I1 { }
                Diagnostic(ErrorCode.ERR_ImportedCircularBase, "I3").WithArguments("I2")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0273ERR_InvalidPropertyAccessMod()
        {
            var text =
@"class C
{
    public object P1 { get; public set; } // CS0273
    public object P2 { get; internal set; }
    public object P3 { get; protected set; }
    public object P4 { get; protected internal set; }
    public object P5 { get; private set; }
    internal object Q1 { public get; set; } // CS0273
    internal object Q2 { internal get; set; } // CS0273
    internal object Q3 { protected get; set; } // CS0273
    internal object Q4 { protected internal get; set; } // CS0273
    internal object Q5 { private get; set; }
    protected object R1 { get { return null; } public set { } } // CS0273
    protected object R2 { get { return null; } internal set { } } // CS0273
    protected object R3 { get { return null; } protected set { } } // CS0273
    protected object R4 { get { return null; } protected internal set { } } // CS0273
    protected object R5 { get { return null; } private set { } }
    protected internal object S1 { get { return null; } public set { } } // CS0273
    protected internal object S2 { get { return null; } internal set { } }
    protected internal object S3 { get { return null; } protected set { } }
    protected internal object S4 { get { return null; } protected internal set { } } // CS0273
    protected internal object S5 { get { return null; } private set { } }
    private object T1 { public get; set; } // CS0273
    private object T2 { internal get; set; } // CS0273
    private object T3 { protected get; set; } // CS0273
    private object T4 { protected internal get; set; } // CS0273
    private object T5 { private get; set; } // CS0273
    object U1 { public get; set; } // CS0273
    object U2 { internal get; set; } // CS0273
    object U3 { protected get; set; } // CS0273
    object U4 { protected internal get; set; } // CS0273
    object U5 { private get; set; } // CS0273
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,36): error CS0273: The accessibility modifier of the 'C.P1.set' accessor must be more restrictive than the property or indexer 'C.P1'
                //     public object P1 { get; public set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.P1.set", "C.P1"),
                // (8,33): error CS0273: The accessibility modifier of the 'C.Q1.get' accessor must be more restrictive than the property or indexer 'C.Q1'
                //     internal object Q1 { public get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.Q1.get", "C.Q1"),
                // (9,35): error CS0273: The accessibility modifier of the 'C.Q2.get' accessor must be more restrictive than the property or indexer 'C.Q2'
                //     internal object Q2 { internal get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.Q2.get", "C.Q2"),
                // (10,36): error CS0273: The accessibility modifier of the 'C.Q3.get' accessor must be more restrictive than the property or indexer 'C.Q3'
                //     internal object Q3 { protected get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.Q3.get", "C.Q3"),
                // (11,45): error CS0273: The accessibility modifier of the 'C.Q4.get' accessor must be more restrictive than the property or indexer 'C.Q4'
                //     internal object Q4 { protected internal get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.Q4.get", "C.Q4"),
                // (13,55): error CS0273: The accessibility modifier of the 'C.R1.set' accessor must be more restrictive than the property or indexer 'C.R1'
                //     protected object R1 { get { return null; } public set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.R1.set", "C.R1"),
                // (14,57): error CS0273: The accessibility modifier of the 'C.R2.set' accessor must be more restrictive than the property or indexer 'C.R2'
                //     protected object R2 { get { return null; } internal set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.R2.set", "C.R2"),
                // (15,58): error CS0273: The accessibility modifier of the 'C.R3.set' accessor must be more restrictive than the property or indexer 'C.R3'
                //     protected object R3 { get { return null; } protected set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.R3.set", "C.R3"),
                // (16,67): error CS0273: The accessibility modifier of the 'C.R4.set' accessor must be more restrictive than the property or indexer 'C.R4'
                //     protected object R4 { get { return null; } protected internal set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.R4.set", "C.R4"),
                // (18,64): error CS0273: The accessibility modifier of the 'C.S1.set' accessor must be more restrictive than the property or indexer 'C.S1'
                //     protected internal object S1 { get { return null; } public set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.S1.set", "C.S1"),
                // (21,76): error CS0273: The accessibility modifier of the 'C.S4.set' accessor must be more restrictive than the property or indexer 'C.S4'
                //     protected internal object S4 { get { return null; } protected internal set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.S4.set", "C.S4"),
                // (23,32): error CS0273: The accessibility modifier of the 'C.T1.get' accessor must be more restrictive than the property or indexer 'C.T1'
                //     private object T1 { public get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.T1.get", "C.T1"),
                // (24,34): error CS0273: The accessibility modifier of the 'C.T2.get' accessor must be more restrictive than the property or indexer 'C.T2'
                //     private object T2 { internal get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.T2.get", "C.T2"),
                // (25,35): error CS0273: The accessibility modifier of the 'C.T3.get' accessor must be more restrictive than the property or indexer 'C.T3'
                //     private object T3 { protected get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.T3.get", "C.T3"),
                // (26,44): error CS0273: The accessibility modifier of the 'C.T4.get' accessor must be more restrictive than the property or indexer 'C.T4'
                //     private object T4 { protected internal get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.T4.get", "C.T4"),
                // (273): error CS0273: The accessibility modifier of the 'C.T5.get' accessor must be more restrictive than the property or indexer 'C.T5'
                //     private object T5 { private get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.T5.get", "C.T5"),
                // (28,24): error CS0273: The accessibility modifier of the 'C.U1.get' accessor must be more restrictive than the property or indexer 'C.U1'
                //     object U1 { public get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.U1.get", "C.U1"),
                // (29,26): error CS0273: The accessibility modifier of the 'C.U2.get' accessor must be more restrictive than the property or indexer 'C.U2'
                //     object U2 { internal get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.U2.get", "C.U2"),
                // (30,27): error CS0273: The accessibility modifier of the 'C.U3.get' accessor must be more restrictive than the property or indexer 'C.U3'
                //     object U3 { protected get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.U3.get", "C.U3"),
                // (31,36): error CS0273: The accessibility modifier of the 'C.U4.get' accessor must be more restrictive than the property or indexer 'C.U4'
                //     object U4 { protected internal get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.U4.get", "C.U4"),
                // (32,25): error CS0273: The accessibility modifier of the 'C.U5.get' accessor must be more restrictive than the property or indexer 'C.U5'
                //     object U5 { private get; set; } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.U5.get", "C.U5"));
        }

        [Fact]
        public void CS0273ERR_InvalidPropertyAccessMod_Indexers()
        {
            var text =
@"class C
{
    public object this[int x, int y, double z] { get { return null; } public set { } } // CS0273
    public object this[int x, double y, int z] { get { return null; } internal set { } }
    public object this[int x, double y, double z] { get { return null; } protected set { } }
    public object this[double x, int y, int z] { get { return null; } protected internal set { } }
    public object this[double x, int y, double z] { get { return null; } private set { } }
    internal object this[int x, int y, char z] { public get { return null; } set { } } // CS0273
    internal object this[int x, char y, int z] { internal get { return null; } set { } } // CS0273
    internal object this[int x, char y, char z] { protected get { return null; } set { } } // CS0273
    internal object this[char x, int y, int z] { protected internal get { return null; } set { } } // CS0273
    internal object this[char x, int y, char z] { private get { return null; } set { } }
    protected object this[int x, int y, long z] { get { return null; } public set { } } // CS0273
    protected object this[int x, long y, int z] { get { return null; } internal set { } } // CS0273
    protected object this[int x, long y, long z] { get { return null; } protected set { } } // CS0273
    protected object this[long x, int y, int z] { get { return null; } protected internal set { } } // CS0273
    protected object this[long x, int y, long z] { get { return null; } private set { } }
    protected internal object this[int x, int y, float z] { get { return null; } public set { } } // CS0273
    protected internal object this[int x, float y, int z] { get { return null; } internal set { } }
    protected internal object this[int x, float y, float z] { get { return null; } protected set { } }
    protected internal object this[float x, int y, int z] { get { return null; } protected internal set { } } // CS0273
    protected internal object this[float x, int y, float z] { get { return null; } private set { } }
    private object this[int x, int y, string z] { public get { return null; } set { } } // CS0273
    private object this[int x, string y, int z] { internal get { return null; } set { } } // CS0273
    private object this[int x, string y, string z] { protected get { return null; } set { } } // CS0273
    private object this[string x, int y, int z] { protected internal get { return null; } set { } } // CS0273
    private object this[string x, int y, string z] { private get { return null; } set { } } // CS0273
    object this[int x, int y, object z] { public get { return null; } set { } } // CS0273
    object this[int x, object y, int z] { internal get { return null; } set { } } // CS0273
    object this[int x, object y, object z] { protected get { return null; } set { } } // CS0273
    object this[object x, int y, int z] { protected internal get { return null; } set { } } // CS0273
    object this[object x, int y, object z] { private get { return null; } set { } } // CS0273
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,78): error CS0273: The accessibility modifier of the 'C.this[int, int, double].set' accessor must be more restrictive than the property or indexer 'C.this[int, int, double]'
                //     public object this[int x, int y, double z] { get { return null; } public set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[int, int, double].set", "C.this[int, int, double]"),
                // (8,57): error CS0273: The accessibility modifier of the 'C.this[int, int, char].get' accessor must be more restrictive than the property or indexer 'C.this[int, int, char]'
                //     internal object this[int x, int y, char z] { public get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, int, char].get", "C.this[int, int, char]"),
                // (9,59): error CS0273: The accessibility modifier of the 'C.this[int, char, int].get' accessor must be more restrictive than the property or indexer 'C.this[int, char, int]'
                //     internal object this[int x, char y, int z] { internal get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, char, int].get", "C.this[int, char, int]"),
                // (10,61): error CS0273: The accessibility modifier of the 'C.this[int, char, char].get' accessor must be more restrictive than the property or indexer 'C.this[int, char, char]'
                //     internal object this[int x, char y, char z] { protected get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, char, char].get", "C.this[int, char, char]"),
                // (11,69): error CS0273: The accessibility modifier of the 'C.this[char, int, int].get' accessor must be more restrictive than the property or indexer 'C.this[char, int, int]'
                //     internal object this[char x, int y, int z] { protected internal get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[char, int, int].get", "C.this[char, int, int]"),
                // (13,79): error CS0273: The accessibility modifier of the 'C.this[int, int, long].set' accessor must be more restrictive than the property or indexer 'C.this[int, int, long]'
                //     protected object this[int x, int y, long z] { get { return null; } public set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[int, int, long].set", "C.this[int, int, long]"),
                // (14,81): error CS0273: The accessibility modifier of the 'C.this[int, long, int].set' accessor must be more restrictive than the property or indexer 'C.this[int, long, int]'
                //     protected object this[int x, long y, int z] { get { return null; } internal set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[int, long, int].set", "C.this[int, long, int]"),
                // (15,83): error CS0273: The accessibility modifier of the 'C.this[int, long, long].set' accessor must be more restrictive than the property or indexer 'C.this[int, long, long]'
                //     protected object this[int x, long y, long z] { get { return null; } protected set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[int, long, long].set", "C.this[int, long, long]"),
                // (16,91): error CS0273: The accessibility modifier of the 'C.this[long, int, int].set' accessor must be more restrictive than the property or indexer 'C.this[long, int, int]'
                //     protected object this[long x, int y, int z] { get { return null; } protected internal set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[long, int, int].set", "C.this[long, int, int]"),
                // (18,89): error CS0273: The accessibility modifier of the 'C.this[int, int, float].set' accessor must be more restrictive than the property or indexer 'C.this[int, int, float]'
                //     protected internal object this[int x, int y, float z] { get { return null; } public set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[int, int, float].set", "C.this[int, int, float]"),
                // (21,101): error CS0273: The accessibility modifier of the 'C.this[float, int, int].set' accessor must be more restrictive than the property or indexer 'C.this[float, int, int]'
                //     protected internal object this[float x, int y, int z] { get { return null; } protected internal set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.this[float, int, int].set", "C.this[float, int, int]"),
                // (23,58): error CS0273: The accessibility modifier of the 'C.this[int, int, string].get' accessor must be more restrictive than the property or indexer 'C.this[int, int, string]'
                //     private object this[int x, int y, string z] { public get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, int, string].get", "C.this[int, int, string]"),
                // (24,60): error CS0273: The accessibility modifier of the 'C.this[int, string, int].get' accessor must be more restrictive than the property or indexer 'C.this[int, string, int]'
                //     private object this[int x, string y, int z] { internal get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, string, int].get", "C.this[int, string, int]"),
                // (25,64): error CS0273: The accessibility modifier of the 'C.this[int, string, string].get' accessor must be more restrictive than the property or indexer 'C.this[int, string, string]'
                //     private object this[int x, string y, string z] { protected get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, string, string].get", "C.this[int, string, string]"),
                // (26,70): error CS0273: The accessibility modifier of the 'C.this[string, int, int].get' accessor must be more restrictive than the property or indexer 'C.this[string, int, int]'
                //     private object this[string x, int y, int z] { protected internal get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[string, int, int].get", "C.this[string, int, int]"),
                // (27,62): error CS0273: The accessibility modifier of the 'C.this[string, int, string].get' accessor must be more restrictive than the property or indexer 'C.this[string, int, string]'
                //     private object this[string x, int y, string z] { private get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[string, int, string].get", "C.this[string, int, string]"),
                // (28,50): error CS0273: The accessibility modifier of the 'C.this[int, int, object].get' accessor must be more restrictive than the property or indexer 'C.this[int, int, object]'
                //     object this[int x, int y, object z] { public get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, int, object].get", "C.this[int, int, object]"),
                // (29,52): error CS0273: The accessibility modifier of the 'C.this[int, object, int].get' accessor must be more restrictive than the property or indexer 'C.this[int, object, int]'
                //     object this[int x, object y, int z] { internal get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, object, int].get", "C.this[int, object, int]"),
                // (30,56): error CS0273: The accessibility modifier of the 'C.this[int, object, object].get' accessor must be more restrictive than the property or indexer 'C.this[int, object, object]'
                //     object this[int x, object y, object z] { protected get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[int, object, object].get", "C.this[int, object, object]"),
                // (31,62): error CS0273: The accessibility modifier of the 'C.this[object, int, int].get' accessor must be more restrictive than the property or indexer 'C.this[object, int, int]'
                //     object this[object x, int y, int z] { protected internal get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[object, int, int].get", "C.this[object, int, int]"),
                // (32,54): error CS0273: The accessibility modifier of the 'C.this[object, int, object].get' accessor must be more restrictive than the property or indexer 'C.this[object, int, object]'
                //     object this[object x, int y, object z] { private get { return null; } set { } } // CS0273
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.this[object, int, object].get", "C.this[object, int, object]"));
        }

        [Fact]
        public void CS0274ERR_DuplicatePropertyAccessMods()
        {
            var text =
@"class C
{
    public int P { protected get; internal set; }
    internal object Q { private get { return null; } private set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,16): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'C.P'
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "P").WithArguments("C.P"),
                // (4,21): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'C.Q'
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "Q").WithArguments("C.Q"));
        }

        [Fact]
        public void CS0274ERR_DuplicatePropertyAccessMods_Indexer()
        {
            var text =
@"class C
{
    public int this[int x] { protected get { return 0; } internal set { } }
    internal object this[object x] { private get { return null; } private set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,16): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'C.this[int]'
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "this").WithArguments("C.this[int]"),
                // (4,21): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'C.this[object]'
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "this").WithArguments("C.this[object]"));
        }

        [Fact]
        public void CS0275ERR_PropertyAccessModInInterface()
        {
            CreateCompilation(
@"interface I
{
    object P { get; } // no error
    int Q { private get; set; } // CS0275
    object R { get; internal set; } // CS0275
}
", parseOptions: TestOptions.Regular7)
                .VerifyDiagnostics(
                // (4,21): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     int Q { private get; set; } // CS0275
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "get").WithArguments("private", "7.0", "8.0").WithLocation(4, 21),
                // (4,21): error CS0442: 'I.Q.get': abstract properties cannot have private accessors
                //     int Q { private get; set; } // CS0275
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I.Q.get").WithLocation(4, 21),
                // (5,30): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     object R { get; internal set; } // CS0275
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "set").WithArguments("internal", "7.0", "8.0").WithLocation(5, 30)
                );
        }

        [Fact]
        public void CS0275ERR_PropertyAccessModInInterface_Indexer()
        {
            CreateCompilation(
@"interface I
{
    object this[int x] { get; } // no error
    int this[char x] { private get; set; } // CS0275
    object this[string x] { get; internal set; } // CS0275
}
", parseOptions: TestOptions.Regular7)
                .VerifyDiagnostics(
                // (4,32): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     int this[char x] { private get; set; } // CS0275
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "get").WithArguments("private", "7.0", "8.0").WithLocation(4, 32),
                // (4,32): error CS0442: 'I.this[char].get': abstract properties cannot have private accessors
                //     int this[char x] { private get; set; } // CS0275
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I.this[char].get").WithLocation(4, 32),
                // (5,43): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                //     object this[string x] { get; internal set; } // CS0275
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "set").WithArguments("internal", "7.0", "8.0").WithLocation(5, 43)
                );
        }

        [WorkItem(538620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538620")]
        [Fact]
        public void CS0276ERR_AccessModMissingAccessor()
        {
            var text =
@"class A
{
    public virtual object P { get; protected set; }
}
class B : A
{
    public override object P { protected set { } } // no error
    public object Q { private set { } } // CS0276
    protected internal object R { internal get { return null; } } // CS0276
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,19): error CS0276: 'B.Q': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "Q").WithArguments("B.Q"),
                // (9,31): error CS0276: 'B.R': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "R").WithArguments("B.R"));
        }

        [Fact]
        public void CS0276ERR_AccessModMissingAccessor_Indexer()
        {
            var text =
@"class A
{
    public virtual object this[int x] { get { return null; } protected set { } }
}
class B : A
{
    public override object this[int x] { protected set { } } // no error
    public object this[char x] { private set { } } // CS0276
    protected internal object this[string x] { internal get { return null; } } // CS0276
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,19): error CS0276: 'B.this[char]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("B.this[char]"),
                // (9,31): error CS0276: 'B.this[string]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("B.this[string]"));
        }

        [Fact]
        public void CS0277ERR_UnimplementedInterfaceAccessor()
        {
            var text = @"public interface MyInterface
{
    int Property
    {
        get;
        set;
    }
}

public class MyClass : MyInterface   // CS0277
{
    public int Property
    {
        get { return 0; }
        protected set { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceAccessor, Line = 10, Column = 24 });
        }

        [Fact(Skip = "530901"), WorkItem(530901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530901")]
        public void CS0281ERR_FriendRefNotEqualToThis()
        {
            //sn -k CS0281.snk
            //sn -i CS0281.snk CS0281.snk
            //sn -pc CS0281.snk key.publickey
            //sn -tp key.publickey
            //csc /target:library /keyfile:CS0281.snk class1.cs
            //csc /target:library /keyfile:CS0281.snk /reference:class1.dll class2.cs
            //csc /target:library /keyfile:CS0281.snk /reference:class2.dll /out:class1.dll program.cs
            var text1 = @"public class A { }";
            var tree1 = SyntaxFactory.ParseCompilationUnit(text1);

            var text2 = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Class1 , PublicKey=abc"")]
class B : A { }
";
            var tree2 = SyntaxFactory.ParseCompilationUnit(text2);

            var text3 = @"using System.Runtime.CompilerServices;
[assembly: System.Reflection.AssemblyVersion(""3"")]
[assembly: System.Reflection.AssemblyCulture(""en-us"")]
[assembly: InternalsVisibleTo(""MyServices, PublicKeyToken=aaabbbcccdddeee"")]
class C : B { }
public class A { }
";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text3,
                new ErrorDescription { Code = (int)ErrorCode.ERR_FriendRefNotEqualToThis, Line = 10, Column = 14 });
        }

        /// <summary>
        /// Some?
        /// </summary>
        [Fact]
        public void CS0305ERR_BadArity01()
        {
            var text = @"namespace NS
{
    interface I<T, V> { }

    struct S<T> : I<T> { }

    class C<T> { }

    public class Test
    {
        //public static int Main()
        //{
        //    C c = new C();  // Not in Dev10
        //    return 1;
        //}

        I<T, V, U> M<T, V, U>() { return null; }
        public I<int> field;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadArity, Line = 5, Column = 19 },
                //new ErrorDescription { Code = (int)ErrorCode.ERR_BadArity, Line = 13, Column = 13 }, // Dev10 miss this due to other errors
                //new ErrorDescription { Code = (int)ErrorCode.ERR_BadArity, Line = 13, Column = 23 }, // Dev10 miss this due to other errors
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadArity, Line = 17, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadArity, Line = 18, Column = 16 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0306ERR_BadTypeArgument01()
        {
            var source =
@"using System;
class C<T>
{
    static void F<U>() { }
    static void M(object o)
    {
        new C<int*>();
        new C<ArgIterator>();
        new C<RuntimeArgumentHandle>();
        new C<TypedReference>();
        F<int*>();
        o.E<object, ArgIterator>();
        Action a;
        a = F<RuntimeArgumentHandle>;
        a = o.E<T, TypedReference>;
        Console.WriteLine(typeof(TypedReference?));
        Console.WriteLine(typeof(Nullable<TypedReference>));
    }
}
static class S
{
    internal static void E<T, U>(this object o) { }
}
";
            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C<int*>();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C<int*>()").WithLocation(7, 9),
                // (7,15): error CS0306: The type 'int*' may not be used as a type argument
                //         new C<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int*").WithArguments("int*").WithLocation(7, 15),
                // (8,15): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //         new C<ArgIterator>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(8, 15),
                // (9,15): error CS0306: The type 'RuntimeArgumentHandle' may not be used as a type argument
                //         new C<RuntimeArgumentHandle>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(9, 15),
                // (10,15): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         new C<TypedReference>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "TypedReference").WithArguments("System.TypedReference").WithLocation(10, 15),
                // (11,9): error CS0306: The type 'int*' may not be used as a type argument
                //         F<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<int*>").WithArguments("int*").WithLocation(11, 9),
                // (12,11): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //         o.E<object, ArgIterator>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "E<object, ArgIterator>").WithArguments("System.ArgIterator").WithLocation(12, 11),
                // (14,13): error CS0306: The type 'RuntimeArgumentHandle' may not be used as a type argument
                //         a = F<RuntimeArgumentHandle>;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<RuntimeArgumentHandle>").WithArguments("System.RuntimeArgumentHandle").WithLocation(14, 13),
                // (15,13): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         a = o.E<T, TypedReference>;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "o.E<T, TypedReference>").WithArguments("System.TypedReference").WithLocation(15, 13),
                // (16,34): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         Console.WriteLine(typeof(TypedReference?));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "TypedReference?").WithArguments("System.TypedReference").WithLocation(16, 34),
                // (17,43): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         Console.WriteLine(typeof(Nullable<TypedReference>));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "TypedReference").WithArguments("System.TypedReference").WithLocation(17, 43));

            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (7,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C<int*>();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C<int*>()").WithLocation(7, 9),
                // (7,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new C<int*>();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(7, 15),
                // (7,15): error CS0306: The type 'int*' may not be used as a type argument
                //         new C<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int*").WithArguments("int*").WithLocation(7, 15),
                // (8,15): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //         new C<ArgIterator>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(8, 15),
                // (9,15): error CS0306: The type 'RuntimeArgumentHandle' may not be used as a type argument
                //         new C<RuntimeArgumentHandle>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(9, 15),
                // (10,15): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         new C<TypedReference>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "TypedReference").WithArguments("System.TypedReference").WithLocation(10, 15),
                // (11,9): error CS0306: The type 'int*' may not be used as a type argument
                //         F<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<int*>").WithArguments("int*").WithLocation(11, 9),
                // (11,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         F<int*>();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(11, 11),
                // (12,11): error CS0306: The type 'ArgIterator' may not be used as a type argument
                //         o.E<object, ArgIterator>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "E<object, ArgIterator>").WithArguments("System.ArgIterator").WithLocation(12, 11),
                // (14,13): error CS0306: The type 'RuntimeArgumentHandle' may not be used as a type argument
                //         a = F<RuntimeArgumentHandle>;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<RuntimeArgumentHandle>").WithArguments("System.RuntimeArgumentHandle").WithLocation(14, 13),
                // (15,13): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         a = o.E<T, TypedReference>;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "o.E<T, TypedReference>").WithArguments("System.TypedReference").WithLocation(15, 13),
                // (16,34): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         Console.WriteLine(typeof(TypedReference?));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "TypedReference?").WithArguments("System.TypedReference").WithLocation(16, 34),
                // (17,43): error CS0306: The type 'TypedReference' may not be used as a type argument
                //         Console.WriteLine(typeof(Nullable<TypedReference>));
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "TypedReference").WithArguments("System.TypedReference").WithLocation(17, 43));
        }

        [Fact]
        public void CS0306ERR_BadTypeArgument01_UnsafeContext()
        {
            var source =
@"using System;
class C<T>
{
    static void F<U>() { }
    unsafe static void M(object o)
    {
        new C<int*>();
        F<int*>();
    }
}
static class S
{
    internal static void E<T, U>(this object o) { }
}
";
            var expected = new[]
            {
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
                // (7,15): error CS0306: The type 'int*' may not be used as a type argument
                //         new C<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int*").WithArguments("int*").WithLocation(7, 15),
                // (8,9): error CS0306: The type 'int*' may not be used as a type argument
                //         F<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<int*>").WithArguments("int*").WithLocation(8, 9)
            };

            CreateCompilationWithMscorlib46(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular12)
                .VerifyDiagnostics(expected);

            CreateCompilationWithMscorlib46(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11)
                .VerifyDiagnostics(expected);
        }

        /// <summary>
        /// Bad type arguments for aliases should be reported at the
        /// alias declaration rather than at the use. (Note: This differs
        /// from Dev10 which reports errors at the use, with no errors
        /// reported if there are no uses of the alias.)
        /// </summary>
        [Fact]
        public void CS0306ERR_BadTypeArgument02()
        {
            var source =
@"using COfObject = C<object>;
using COfIntPtr = C<int*>;
using COfArgIterator = C<System.ArgIterator>; // unused
class C<T>
{
    static void F<U>() { }
    static void M()
    {
        new COfIntPtr();
        COfObject.F<int*>();
        COfIntPtr.F<object>();
    }
}";
            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (2,7): error CS0306: The type 'int*' may not be used as a type argument
                // using COfIntPtr = C<int*>;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "COfIntPtr").WithArguments("int*").WithLocation(2, 7),
                // (3,7): error CS0306: The type 'ArgIterator' may not be used as a type argument
                // using COfArgIterator = C<System.ArgIterator>; // unused
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "COfArgIterator").WithArguments("System.ArgIterator").WithLocation(3, 7),
                // (9,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new COfIntPtr();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "COfIntPtr").WithLocation(9, 13),
                // (9,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new COfIntPtr();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new COfIntPtr()").WithLocation(9, 9),
                // (10,19): error CS0306: The type 'int*' may not be used as a type argument
                //         COfObject.F<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<int*>").WithArguments("int*").WithLocation(10, 19),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using COfArgIterator = C<System.ArgIterator>; // unused
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using COfArgIterator = C<System.ArgIterator>;").WithLocation(3, 1));

            CreateCompilationWithMscorlib46(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (2,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // using COfIntPtr = C<int*>;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 21),
                // (2,7): error CS0306: The type 'int*' may not be used as a type argument
                // using COfIntPtr = C<int*>;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "COfIntPtr").WithArguments("int*").WithLocation(2, 7),
                // (3,7): error CS0306: The type 'ArgIterator' may not be used as a type argument
                // using COfArgIterator = C<System.ArgIterator>; // unused
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "COfArgIterator").WithArguments("System.ArgIterator").WithLocation(3, 7),
                // (9,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new COfIntPtr();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "COfIntPtr").WithLocation(9, 13),
                // (9,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         new COfIntPtr();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new COfIntPtr()").WithLocation(9, 9),
                // (10,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         COfObject.F<int*>();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(10, 21),
                // (10,19): error CS0306: The type 'int*' may not be used as a type argument
                //         COfObject.F<int*>();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "F<int*>").WithArguments("int*").WithLocation(10, 19),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using COfArgIterator = C<System.ArgIterator>; // unused
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using COfArgIterator = C<System.ArgIterator>;").WithLocation(3, 1));
        }

        [WorkItem(538157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538157")]
        [Fact]
        public void CS0307ERR_TypeArgsNotAllowed()
        {
            var text = @"namespace NS
{
    public class Test<T>
    {
        internal object field;
        public int P { get { return 1; } }

        public static int Main()
        {
            Test<int> t = new NS<T>.Test<int>();
            var v = t.field<string>;
            int p = t.P<int>();
            if (v == v | p == p | t == t) {}
            return 1;
        }
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
// (10,31): error CS0307: The namespace 'NS' cannot be used with type arguments
//             Test<int> t = new NS<T>.Test<int>();
Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "NS<T>").WithArguments("NS", "namespace"),
// (11,23): error CS0307: The field 'NS.Test<int>.field' cannot be used with type arguments
//             var v = t.field<string>;
Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "field<string>").WithArguments("NS.Test<int>.field", "field"),
// (12,23): error CS0307: The property 'NS.Test<int>.P' cannot be used with type arguments
//             int p = t.P<int>();
Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "P<int>").WithArguments("NS.Test<int>.P", "property"),
// (13,17): warning CS1718: Comparison made to same variable; did you mean to compare something else?
//             if (v == v | p == p | t == t) {}
Diagnostic(ErrorCode.WRN_ComparisonToSelf, "v == v"),
// (13,26): warning CS1718: Comparison made to same variable; did you mean to compare something else?
//             if (v == v | p == p | t == t) {}
Diagnostic(ErrorCode.WRN_ComparisonToSelf, "p == p"),
// (13,35): warning CS1718: Comparison made to same variable; did you mean to compare something else?
//             if (v == v | p == p | t == t) {}
Diagnostic(ErrorCode.WRN_ComparisonToSelf, "t == t"),
// (5,25): warning CS0649: Field 'NS.Test<T>.field' is never assigned to, and will always have its default value null
//         internal object field;
Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("NS.Test<T>.field", "null")
                );
        }

        [WorkItem(542296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542296")]
        [Fact]
        public void CS0307ERR_TypeArgsNotAllowed_02()
        {
            var text = @"
public class Test
{
    public int Fld;
    public int Func()
    {
        return (int)(Fld<int>);
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (7,22): error CS0307: The field 'Test.Fld' cannot be used with type arguments
                //         return (int)(Fld<int>);
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "Fld<int>").WithArguments("Test.Fld", "field")
                );
        }

        [Fact]
        public void CS0307ERR_TypeArgsNotAllowed_03()
        {
            var text =
@"class C<T, U> where T : U<T>, new()
{
    static object M()
    {
        return new T<U>();
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,25): error CS0307: The type parameter 'U' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "U<T>").WithArguments("U", "type parameter").WithLocation(1, 25),
                // (5,20): error CS0307: The type parameter 'T' cannot be used with type arguments
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "T<U>").WithArguments("T", "type parameter").WithLocation(5, 20));
        }

        [Fact]
        public void CS0308ERR_HasNoTypeVars01()
        {
            var text = @"namespace NS
{
    public class Test
    {
        public static string F() { return null; }
        protected void FF(string s) { }

        public static int Main()
        {
            new Test().FF<string, string>(F<int>());

            return 1;
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_HasNoTypeVars, Line = 10, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_HasNoTypeVars, Line = 10, Column = 43 });
        }

        [WorkItem(540090, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540090")]
        [Fact]
        public void CS0308ERR_HasNoTypeVars02()
        {
            var text = @"
public class NormalType
{
    public static class M2 { public static int F1 = 1; }
    public static void Test()
    {
        int i;
        i = M2<int>.F1;
    }
    public static int Main() { return -1; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_HasNoTypeVars, Line = 8, Column = 13 });
        }

        [Fact]
        public void CS0400ERR_GlobalSingleTypeNameNotFound01()
        {
            var text = @"namespace NS
{
    public class Test
    {
        public static int Main()
        {
            // global::D d = new global::D();
            return 1;
        }
    }

    struct S
    {
        global::G field;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (14,17): error CS0400: The type or namespace name 'G' could not be found in the global namespace (are you missing an assembly reference?)
                //         global::G field;
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "G").WithArguments("G"),
                // (14,19): warning CS0169: The field 'NS.S.field' is never used
                //         global::G field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("NS.S.field")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0405ERR_DuplicateBound()
        {
            var source =
@"interface IA { }
interface IB { }
class A { }
class B { }
class C<T, U>
    where T : IA, IB, IA
    where U : A, IA, A
{
    void M<V>() where V : U, IA, U { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,23): error CS0405: Duplicate constraint 'IA' for type parameter 'T'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "IA").WithArguments("IA", "T").WithLocation(6, 23),
                // (7,22): error CS0405: Duplicate constraint 'A' for type parameter 'U'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "A").WithArguments("A", "U").WithLocation(7, 22),
                // (9,34): error CS0405: Duplicate constraint 'U' for type parameter 'V'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "U").WithArguments("U", "V").WithLocation(9, 34));
        }

        [Fact]
        public void CS0406ERR_ClassBoundNotFirst()
        {
            var source =
@"interface I { }
class A { }
class B { }
class C<T, U>
    where T : I, A
    where U : A, B
{
    void M<V>() where V : U, A, B { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,18): error CS0406: The class type constraint 'A' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(5, 18),
                // (6,18): error CS0406: The class type constraint 'B' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "B").WithArguments("B").WithLocation(6, 18),
                // (8,30): error CS0406: The class type constraint 'A' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(8, 30),
                // (8,33): error CS0406: The class type constraint 'B' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "B").WithArguments("B").WithLocation(8, 33));
        }

        [Fact]
        public void CS0409ERR_DuplicateConstraintClause()
        {
            var source =
@"interface I<T>
    where T : class
    where T : struct
{
    void M<U, V>()
        where U : new()
        where V : class
        where U : class
        where U : I<T>;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,11): error CS0409: A constraint clause has already been specified for type parameter 'T'. All of the constraints for a type parameter must be specified in a single where clause.
                Diagnostic(ErrorCode.ERR_DuplicateConstraintClause, "T").WithArguments("T").WithLocation(3, 11),
                // (8,15): error CS0409: A constraint clause has already been specified for type parameter 'T'. All of the constraints for a type parameter must be specified in a single where clause.
                Diagnostic(ErrorCode.ERR_DuplicateConstraintClause, "U").WithArguments("U").WithLocation(8, 15),
                // (9,15): error CS0409: A constraint clause has already been specified for type parameter 'T'. All of the constraints for a type parameter must be specified in a single where clause.
                Diagnostic(ErrorCode.ERR_DuplicateConstraintClause, "U").WithArguments("U").WithLocation(9, 15));
        }

        [Fact]
        public void CS0415ERR_BadIndexerNameAttr()
        {
            var text = @"using System;
using System.Runtime.CompilerServices;

public interface IA
{
    int this[int index]
    {
        get;
        set;
    }
}

public class A : IA
{
    [IndexerName(""Item"")]  // CS0415
    int IA.this[int index]
    {
        get { return 0; }
        set { }
    }

    [IndexerName(""Item"")]  // CS0415
    int P { get; set; }


    [IndexerName(""Item"")]  // CS0592
    int f;
    
    [IndexerName(""Item"")]  // CS0592
    void M() { }

    [IndexerName(""Item"")]  // CS0592
    event Action E;

    [IndexerName(""Item"")]  // CS0592
    class C { }

    [IndexerName(""Item"")]  // CS0592
    struct S { }

    [IndexerName(""Item"")]  // CS0592
    delegate void D();
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,6): error CS0415: The 'IndexerName' attribute is valid only on an indexer that is not an explicit interface member declaration
                Diagnostic(ErrorCode.ERR_BadIndexerNameAttr, "IndexerName").WithArguments("IndexerName"),
                // (22,6): error CS0415: The 'IndexerName' attribute is valid only on an indexer that is not an explicit interface member declaration
                Diagnostic(ErrorCode.ERR_BadIndexerNameAttr, "IndexerName").WithArguments("IndexerName"),
                // (26,6): error CS0592: Attribute 'IndexerName' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IndexerName").WithArguments("IndexerName", "property, indexer"),
                // (29,6): error CS0592: Attribute 'IndexerName' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IndexerName").WithArguments("IndexerName", "property, indexer"),
                // (32,6): error CS0592: Attribute 'IndexerName' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IndexerName").WithArguments("IndexerName", "property, indexer"),
                // (35,6): error CS0592: Attribute 'IndexerName' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IndexerName").WithArguments("IndexerName", "property, indexer"),
                // (38,6): error CS0592: Attribute 'IndexerName' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IndexerName").WithArguments("IndexerName", "property, indexer"),
                // (41,6): error CS0592: Attribute 'IndexerName' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "IndexerName").WithArguments("IndexerName", "property, indexer"),
                // (27,9): warning CS0169: The field 'A.f' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("A.f"),
                // (33,18): warning CS0067: The event 'A.E' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("A.E"));
        }

        [Fact]
        public void CS0415ERR_BadIndexerNameAttr_Alias()
        {
            var text = @"
using Alias = System.Runtime.CompilerServices.IndexerNameAttribute;

public interface IA
{
    int this[int index] { get; set; }
}

public class A : IA
{
    [  Alias(""Item"")]  // CS0415
    int IA.this[int index] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilation(text);

            // NOTE: uses attribute name from syntax.
            compilation.VerifyDiagnostics(
                // (11,6): error CS0415: The 'Alias' attribute is valid only on an indexer that is not an explicit interface member declaration
                Diagnostic(ErrorCode.ERR_BadIndexerNameAttr, @"Alias").WithArguments("Alias"));

            // Note: invalid attribute had no effect on metadata name.
            var indexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetProperty("IA." + WellKnownMemberNames.Indexer);
            Assert.Equal("IA.Item", indexer.MetadataName);
        }

        [Fact]
        public void CS0416ERR_AttrArgWithTypeVars()
        {
            var text = @"public class MyAttribute : System.Attribute
{
    public MyAttribute(System.Type t)
    {
    }
}

class G<T>
{
    [MyAttribute(typeof(G<T>))]  // CS0416
    public void F()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AttrArgWithTypeVars, Line = 10, Column = 18 });
        }

        [Fact]
        public void CS0418ERR_AbstractSealedStatic01()
        {
            var text = @"namespace NS
{
    public abstract static class C  // CS0418
    {
        internal abstract sealed class CC
        {
            private static abstract sealed class CCC // CS0418, 0441
            {
            }
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractSealedStatic, Line = 3, Column = 34 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractSealedStatic, Line = 5, Column = 40 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractSealedStatic, Line = 7, Column = 50 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SealedStaticClass, Line = 7, Column = 50 });
        }

        [Fact]
        public void CS0423ERR_ComImportWithImpl()
        {
            var text = @"using System.Runtime.InteropServices;
[ComImport,  Guid(""7ab770c7-0e23-4d7a-8aa2-19bfad479829"")]
class ImageProperties
{
    public static void Main()  // CS0423
    {
        ImageProperties i = new ImageProperties();
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,24): error CS0423: Since 'ImageProperties' has the ComImport attribute, 'ImageProperties.Main()' must be extern or abstract
                //     public static void Main()  // CS0423
                Diagnostic(ErrorCode.ERR_ComImportWithImpl, "Main").WithArguments("ImageProperties.Main()", "ImageProperties").WithLocation(5, 24));
        }

        [Fact]
        public void CS0424ERR_ComImportWithBase()
        {
            var text = @"using System.Runtime.InteropServices;
public class A { }

[ComImport, Guid(""7ab770c7-0e23-4d7a-8aa2-19bfad479829"")]
class B : A { }   // CS0424 error
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,7): error CS0424: 'B': a class with the ComImport attribute cannot specify a base class
                // class B : A { }   // CS0424 error
                Diagnostic(ErrorCode.ERR_ComImportWithBase, "B").WithArguments("B").WithLocation(5, 7));
        }

        [WorkItem(856187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/856187")]
        [WorkItem(866093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866093")]
        [Fact]
        public void CS0424ERR_ComImportWithInitializers()
        {
            var text = @"
using System.Runtime.InteropServices;

[ComImport, Guid(""7ab770c7-0e23-4d7a-8aa2-19bfad479829"")]
class B 
{ 
    public static int X = 5;
    public int Y = 5;
    public const decimal D = 5;
    public const int Yconst = 5;
}
";
            CreateCompilation(text).VerifyDiagnostics(
    // (7,25): error CS8028: 'B': a class with the ComImport attribute cannot specify field initializers.
    //     public static int X = 5;
    Diagnostic(ErrorCode.ERR_ComImportWithInitializers, "= 5").WithArguments("B").WithLocation(7, 25),
    // (9,28): error CS8028: 'B': a class with the ComImport attribute cannot specify field initializers.
    //     public const decimal D = 5;
    Diagnostic(ErrorCode.ERR_ComImportWithInitializers, "= 5").WithArguments("B").WithLocation(9, 28),
    // (8,18): error CS8028: 'B': a class with the ComImport attribute cannot specify field initializers.
    //     public int Y = 5;
    Diagnostic(ErrorCode.ERR_ComImportWithInitializers, "= 5").WithArguments("B").WithLocation(8, 18)
                );
        }

        [Fact]
        public void CS0425ERR_ImplBadConstraints01()
        {
            var source =
@"interface IA<T> { }
interface IB { }
interface I
{
    // Different constraints:
    void A1<T>() where T : struct;
    void A2<T, U>() where T : struct where U : IA<T>;
    void A3<T>() where T : IA<T>;
    void A4<T, U>() where T : struct, IA<T>;
    // Additional constraints:
    void B1<T>();
    void B2<T>() where T : new();
    void B3<T, U>() where T : IA<T>;
    // Missing constraints.
    void C1<T>() where T : class;
    void C2<T>() where T : class, new();
    void C3<T, U>() where U : IB, IA<T>;
    // Same constraints, different order.
    void D1<T>() where T : IA<T>, IB;
    void D2<T, U, V>() where V : T, U;
    // Different constraint clauses.
    void E1<T, U>() where U : T;
    // Additional constraint clause.
    void F1<T, U>() where T : class;
    // Missing constraint clause.
    void G1<T, U>() where T : class where U : T;
    // Same constraint clauses, different order.
    void H1<T, U>() where T : class where U : T;
    void H2<T, U>() where T : class where U : T;
    void H3<T, U, V>() where V : class where U : IB where T : IA<V>;
    // Different type parameter names.
    void K1<T, U>() where T : class where U : T;
    void K2<T, U>() where T : class where U : T;
}
class C : I
{
    // Different constraints:
    public void A1<T>() where T : class { }
    public void A2<T, U>() where T : struct where U : IB { }
    public void A3<T>() where T : IA<IA<T>> { }
    public void A4<T, U>() where T : struct, IA<U> { }
    // Additional constraints:
    public void B1<T>() where T : new() { }
    public void B2<T>() where T : class, new() { }
    public void B3<T, U>() where T : IB, IA<T> { }
    // Missing constraints.
    public void C1<T>() { }
    public void C2<T>() where T : class { }
    public void C3<T, U>() where U : IA<T> { }
    // Same constraints, different order.
    public void D1<T>() where T : IB, IA<T> { }
    public void D2<T, U, V>() where V : U, T { }
    // Different constraint clauses.
    public void E1<T, U>() where T : class { }
    // Additional constraint clause.
    public void F1<T, U>() where T : class where U : T { }
    // Missing constraint clause.
    public void G1<T, U>() where T : class { }
    // Same constraint clauses, different order.
    public void H1<T, U>() where U : T where T : class { }
    public void H2<T, U>() where U : class where T : U { }
    public void H3<T, U, V>() where T : IA<V> where U : IB where V : class { }
    // Different type parameter names.
    public void K1<U, T>() where T : class where U : T { }
    public void K2<T1, T2>() where T1 : class where T2 : T1 { }
}";
            // Note: Errors are reported on A1, A2, ... rather than A1<T>, A2<T, U>, ... See bug #9396.
            CreateCompilation(source).VerifyDiagnostics(
                // (38,17): error CS0425: The constraints for type parameter 'T' of method 'C.A1<T>()' must match the constraints for type parameter 'T' of interface method 'I.A1<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "A1").WithArguments("T", "C.A1<T>()", "T", "I.A1<T>()").WithLocation(38, 17),
                // (39,17): error CS0425: The constraints for type parameter 'U' of method 'C.A2<T, U>()' must match the constraints for type parameter 'U' of interface method 'I.A2<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "A2").WithArguments("U", "C.A2<T, U>()", "U", "I.A2<T, U>()").WithLocation(39, 17),
                // (40,17): error CS0425: The constraints for type parameter 'T' of method 'C.A3<T>()' must match the constraints for type parameter 'T' of interface method 'I.A3<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "A3").WithArguments("T", "C.A3<T>()", "T", "I.A3<T>()").WithLocation(40, 17),
                // (41,17): error CS0425: The constraints for type parameter 'T' of method 'C.A4<T, U>()' must match the constraints for type parameter 'T' of interface method 'I.A4<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "A4").WithArguments("T", "C.A4<T, U>()", "T", "I.A4<T, U>()").WithLocation(41, 17),
                // (43,17): error CS0425: The constraints for type parameter 'T' of method 'C.B1<T>()' must match the constraints for type parameter 'T' of interface method 'I.B1<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "B1").WithArguments("T", "C.B1<T>()", "T", "I.B1<T>()").WithLocation(43, 17),
                // (44,17): error CS0425: The constraints for type parameter 'T' of method 'C.B2<T>()' must match the constraints for type parameter 'T' of interface method 'I.B2<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "B2").WithArguments("T", "C.B2<T>()", "T", "I.B2<T>()").WithLocation(44, 17),
                // (45,17): error CS0425: The constraints for type parameter 'T' of method 'C.B3<T, U>()' must match the constraints for type parameter 'T' of interface method 'I.B3<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "B3").WithArguments("T", "C.B3<T, U>()", "T", "I.B3<T, U>()").WithLocation(45, 17),
                // (47,17): error CS0425: The constraints for type parameter 'T' of method 'C.C1<T>()' must match the constraints for type parameter 'T' of interface method 'I.C1<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "C1").WithArguments("T", "C.C1<T>()", "T", "I.C1<T>()").WithLocation(47, 17),
                // (48,17): error CS0425: The constraints for type parameter 'T' of method 'C.C2<T>()' must match the constraints for type parameter 'T' of interface method 'I.C2<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "C2").WithArguments("T", "C.C2<T>()", "T", "I.C2<T>()").WithLocation(48, 17),
                // (49,17): error CS0425: The constraints for type parameter 'U' of method 'C.C3<T, U>()' must match the constraints for type parameter 'U' of interface method 'I.C3<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "C3").WithArguments("U", "C.C3<T, U>()", "U", "I.C3<T, U>()").WithLocation(49, 17),
                // (54,17): error CS0425: The constraints for type parameter 'T' of method 'C.E1<T, U>()' must match the constraints for type parameter 'T' of interface method 'I.E1<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "E1").WithArguments("T", "C.E1<T, U>()", "T", "I.E1<T, U>()").WithLocation(54, 17),
                // (54,17): error CS0425: The constraints for type parameter 'U' of method 'C.E1<T, U>()' must match the constraints for type parameter 'U' of interface method 'I.E1<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "E1").WithArguments("U", "C.E1<T, U>()", "U", "I.E1<T, U>()").WithLocation(54, 17),
                // (56,17): error CS0425: The constraints for type parameter 'U' of method 'C.F1<T, U>()' must match the constraints for type parameter 'U' of interface method 'I.F1<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "F1").WithArguments("U", "C.F1<T, U>()", "U", "I.F1<T, U>()").WithLocation(56, 17),
                // (58,17): error CS0425: The constraints for type parameter 'U' of method 'C.G1<T, U>()' must match the constraints for type parameter 'U' of interface method 'I.G1<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "G1").WithArguments("U", "C.G1<T, U>()", "U", "I.G1<T, U>()").WithLocation(58, 17),
                // (61,17): error CS0425: The constraints for type parameter 'T' of method 'C.H2<T, U>()' must match the constraints for type parameter 'T' of interface method 'I.H2<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "H2").WithArguments("T", "C.H2<T, U>()", "T", "I.H2<T, U>()").WithLocation(61, 17),
                // (61,17): error CS0425: The constraints for type parameter 'U' of method 'C.H2<T, U>()' must match the constraints for type parameter 'U' of interface method 'I.H2<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "H2").WithArguments("U", "C.H2<T, U>()", "U", "I.H2<T, U>()").WithLocation(61, 17),
                // (64,17): error CS0425: The constraints for type parameter 'U' of method 'C.K1<U, T>()' must match the constraints for type parameter 'T' of interface method 'I.K1<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "K1").WithArguments("U", "C.K1<U, T>()", "T", "I.K1<T, U>()").WithLocation(64, 17),
                // (64,17): error CS0425: The constraints for type parameter 'T' of method 'C.K1<U, T>()' must match the constraints for type parameter 'U' of interface method 'I.K1<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "K1").WithArguments("T", "C.K1<U, T>()", "U", "I.K1<T, U>()").WithLocation(64, 17));
        }

        [Fact]
        public void CS0425ERR_ImplBadConstraints02()
        {
            var source =
@"interface IA<T> { }
interface IB { }
interface I<T>
{
    void M1<U, V>() where U : T where V : IA<T>;
    void M2<U>() where U : T, new();
}
class C1 : I<IB>
{
    public void M1<U, V>() where U : IB where V : IA<IB> { }
    public void M2<U>() where U : I<IB> { }
}
class C2<T, U> : I<IA<U>>
{
    public void M1<X, Y>() where Y : IA<IA<U>> where X : IA<U> { }
    public void M2<X>() where X : T, new() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,17): error CS0425: The constraints for type parameter 'U' of method 'C1.M2<U>()' must match the constraints for type parameter 'U' of interface method 'I<IB>.M2<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("U", "C1.M2<U>()", "U", "I<IB>.M2<U>()").WithLocation(11, 17),
                // (16,17): error CS0425: The constraints for type parameter 'X' of method 'C2<T, U>.M2<X>()' must match the constraints for type parameter 'U' of interface method 'I<IA<U>>.M2<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("X", "C2<T, U>.M2<X>()", "U", "I<IA<U>>.M2<U>()").WithLocation(16, 17));
        }

        [Fact]
        public void CS0425ERR_ImplBadConstraints03()
        {
            var source =
@"interface IA { }
class B { }
interface I1<T>
{
    void M<U>() where U : struct, T;
}
class C1<T> : I1<T> where T : struct
{
    public void M<U>() where U : T { }
}
interface I2<T>
{
    void M<U>() where U : class, T;
}
class C2 : I2<B>
{
    public void M<U>() where U : B { }
}
class C2<T> : I2<T> where T : class
{
    public void M<U>() where U : T { }
}
interface I3<T>
{
    void M<U>() where U : T, new();
}
class C3 : I3<B>
{
    public void M<U>() where U : B { }
}
class C3<T> : I3<T> where T : new()
{
    public void M<U>() where U : T { }
}
interface I4<T>
{
    void M<U>() where U : T, IA;
}
class C4 : I4<IA>
{
    public void M<U>() where U : IA { }
}
class C4<T> : I4<T> where T : IA
{
    public void M<U>() where U : T { }
}
interface I5<T>
{
    void M<U>() where U : B, T;
}
class C5 : I5<B>
{
    public void M<U>() where U : B { }
}
class C5<T> : I5<T> where T : B
{
    public void M<U>() where U : T { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,17): error CS0425: The constraints for type parameter 'U' of method 'C1<T>.M<U>()' must match the constraints for type parameter 'U' of interface method 'I1<T>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C1<T>.M<U>()", "U", "I1<T>.M<U>()").WithLocation(9, 17),
                // (9,19): error CS0456: Type parameter 'T' has the 'struct' constraint so 'T' cannot be used as a constraint for 'U'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "U").WithArguments("U", "T").WithLocation(9, 19),
                // (17,17): error CS0425: The constraints for type parameter 'U' of method 'C2.M<U>()' must match the constraints for type parameter 'U' of interface method 'I2<B>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C2.M<U>()", "U", "I2<B>.M<U>()").WithLocation(17, 17),
                // (21,17): error CS0425: The constraints for type parameter 'U' of method 'C2<T>.M<U>()' must match the constraints for type parameter 'U' of interface method 'I2<T>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C2<T>.M<U>()", "U", "I2<T>.M<U>()").WithLocation(21, 17),
                // (29,17): error CS0425: The constraints for type parameter 'U' of method 'C3.M<U>()' must match the constraints for type parameter 'U' of interface method 'I3<B>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C3.M<U>()", "U", "I3<B>.M<U>()").WithLocation(29, 17),
                // (33,17): error CS0425: The constraints for type parameter 'U' of method 'C3<T>.M<U>()' must match the constraints for type parameter 'U' of interface method 'I3<T>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C3<T>.M<U>()", "U", "I3<T>.M<U>()").WithLocation(33, 17),
                // (45,17): error CS0425: The constraints for type parameter 'U' of method 'C4<T>.M<U>()' must match the constraints for type parameter 'U' of interface method 'I4<T>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C4<T>.M<U>()", "U", "I4<T>.M<U>()").WithLocation(45, 17),
                // (57,17): error CS0425: The constraints for type parameter 'U' of method 'C5<T>.M<U>()' must match the constraints for type parameter 'U' of interface method 'I5<T>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C5<T>.M<U>()", "U", "I5<T>.M<U>()").WithLocation(57, 17));
        }

        [Fact]
        public void CS0425ERR_ImplBadConstraints04()
        {
            var source =
@"interface IA
{
    void M1<T>();
    void M2<T, U>() where U : T;
}
interface IB
{
    void M1<T>() where T : IB;
    void M2<X, Y>();
}
abstract class C : IA, IB
{
    public abstract void M1<T>();
    public abstract void M2<X, Y>();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,26): error CS0425: The constraints for type parameter 'Y' of method 'C.M2<X, Y>()' must match the constraints for type parameter 'U' of interface method 'IA.M2<T, U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("Y", "C.M2<X, Y>()", "U", "IA.M2<T, U>()").WithLocation(14, 26),
                // (13,26): error CS0425: The constraints for type parameter 'T' of method 'C.M1<T>()' must match the constraints for type parameter 'T' of interface method 'IB.M1<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M1").WithArguments("T", "C.M1<T>()", "T", "IB.M1<T>()").WithLocation(13, 26));
        }

        [Fact]
        public void CS0425ERR_ImplBadConstraints05()
        {
            var source =
@"interface I<T> { }
class C { }
interface IA<T, U>
{
    void A1<V>() where V : T, I<T>;
    void A2<V>() where V : T, U, I<T>, I<U>;
}
interface IB<T>
{
    void B1<U>() where U : C;
    void B2<U, V>() where U : C, T, V;
}
class A<T, U>
{
    // More constraints than IA<T, U>.A1<V>().
    public void A1<V>() where V : T, U, I<T>, I<U> { }
    // Fewer constraints than IA<T, U>.A2<V>().
    public void A2<V>() where V : T, I<T> { }
}
class B<T>
{
    // More constraints than IB<T>.B1<U>().
    public void B1<U>() where U : C, T { }
    // Fewer constraints than IB<T>.B2<U, V>().
    public void B2<U, V>() where U : T, V { }
}
class A1<T> : A<T, T>, IA<T, T>
{
}
class A2<T, U> : A<T, U>, IA<T, U>
{
}
class B1 : B<C>, IB<C>
{
}
class B2<T> : B<T>, IB<T>
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (30,27): error CS0425: The constraints for type parameter 'V' of method 'A<T, U>.A2<V>()' must match the constraints for type parameter 'V' of interface method 'IA<T, U>.A2<V>()'. Consider using an explicit interface implementation instead.
                // class A2<T, U> : A<T, U>, IA<T, U>
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "IA<T, U>").WithArguments("V", "A<T, U>.A2<V>()", "V", "IA<T, U>.A2<V>()").WithLocation(30, 27),
                // (30,27): error CS0425: The constraints for type parameter 'V' of method 'A<T, U>.A1<V>()' must match the constraints for type parameter 'V' of interface method 'IA<T, U>.A1<V>()'. Consider using an explicit interface implementation instead.
                // class A2<T, U> : A<T, U>, IA<T, U>
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "IA<T, U>").WithArguments("V", "A<T, U>.A1<V>()", "V", "IA<T, U>.A1<V>()").WithLocation(30, 27),
                // (36,21): error CS0425: The constraints for type parameter 'U' of method 'B<T>.B2<U, V>()' must match the constraints for type parameter 'U' of interface method 'IB<T>.B2<U, V>()'. Consider using an explicit interface implementation instead.
                // class B2<T> : B<T>, IB<T>
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "IB<T>").WithArguments("U", "B<T>.B2<U, V>()", "U", "IB<T>.B2<U, V>()").WithLocation(36, 21),
                // (36,21): error CS0425: The constraints for type parameter 'U' of method 'B<T>.B1<U>()' must match the constraints for type parameter 'U' of interface method 'IB<T>.B1<U>()'. Consider using an explicit interface implementation instead.
                // class B2<T> : B<T>, IB<T>
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "IB<T>").WithArguments("U", "B<T>.B1<U>()", "U", "IB<T>.B1<U>()").WithLocation(36, 21));
        }

        [Fact]
        public void CS0425ERR_ImplBadConstraints06()
        {
            var source =
@"using NIA1 = N.IA;
using NIA2 = N.IA;
using NA1 = N.A;
using NA2 = N.A;
namespace N
{
    interface IA { }
    class A { }
}
interface IB
{
    void M1<T>() where T : N.A, N.IA;
    void M2<T>() where T : NA1, NIA1;
}
abstract class B1 : IB
{
    public abstract void M1<T>() where T : NA1, NIA1;
    public abstract void M2<T>() where T : NA2, NIA2;
}
abstract class B2 : IB
{
    public abstract void M1<T>() where T : NA1;
    public abstract void M2<T>() where T : NIA2;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (22,26): error CS0425: The constraints for type parameter 'T' of method 'B2.M1<T>()' must match the constraints for type parameter 'T' of interface method 'IB.M1<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M1").WithArguments("T", "B2.M1<T>()", "T", "IB.M1<T>()").WithLocation(22, 26),
                // (23,26): error CS0425: The constraints for type parameter 'T' of method 'B2.M2<T>()' must match the constraints for type parameter 'T' of interface method 'IB.M2<T>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("T", "B2.M2<T>()", "T", "IB.M2<T>()").WithLocation(23, 26));
        }

        [Fact]
        public void CS0426ERR_DottedTypeNameNotFoundInAgg01()
        {
            var text = @"namespace NS
{
    public class C 
    {
        public interface I { }
    }
    struct S 
    {
    }
    
    public class Test
    {
        C.I.x field; // CS0426
        void M(S.s p) { } // CS0426

        public static int Main()
        {
            // C.A a;  // CS0426
            return 1;
        }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (14,18): error CS0426: The type name 's' does not exist in the type 'NS.S'
                //         void M(S.s p) { } // CS0426
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "s").WithArguments("s", "NS.S"),
                // (13,13): error CS0426: The type name 'x' does not exist in the type 'NS.C.I'
                //         C.I.x field; // CS0426
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "x").WithArguments("x", "NS.C.I"),
                // (13,15): warning CS0169: The field 'NS.Test.field' is never used
                //         C.I.x field; // CS0426
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("NS.Test.field")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0426ERR_DottedTypeNameNotFoundInAgg02()
        {
            var text =
@"class A<T>
{
    A<T>.T F = default(A<T>.T);
}
class B : A<object>
{
    B.T F = default(B.T);
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,10): error CS0426: The type name 'T' does not exist in the type 'A<T>'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "T").WithArguments("T", "A<T>").WithLocation(3, 10),
                // (3,29): error CS0426: The type name 'T' does not exist in the type 'A<T>'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "T").WithArguments("T", "A<T>").WithLocation(3, 29),
                // (7,7): error CS0426: The type name 'T' does not exist in the type 'B'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "T").WithArguments("T", "B").WithLocation(7, 7),
                // (7,23): error CS0426: The type name 'T' does not exist in the type 'B'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "T").WithArguments("T", "B").WithLocation(7, 23));
        }

        [Fact]
        public void CS0430ERR_BadExternAlias()
        {
            var text = @"extern alias MyType;   // CS0430
public class Test 
{
   public static void Main() {}
}
public class MyClass { }
";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,14): error CS0430: The extern alias 'MyType' was not specified in a /reference option
                // extern alias MyType;   // CS0430
                Diagnostic(ErrorCode.ERR_BadExternAlias, "MyType").WithArguments("MyType"),
                // (1,1): info CS8020: Unused extern alias.
                // extern alias MyType;   // CS0430
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias MyType;"));
        }

        [Fact]
        public void CS0432ERR_AliasNotFound()
        {
            var text = @"class C
{
    public class A { }
}

class E : C::A { }";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AliasNotFound, Line = 6, Column = 11 });
        }

        /// <summary>
        /// Import - same name class from lib1 and lib2
        /// </summary>
        [Fact]
        public void CS0433ERR_SameFullNameAggAgg01()
        {
            var text = @"namespace NS
{
    class Test
    {
        Class1 var;
        void M(Class1 p) {}
    }
}
";
            var ref1 = TestReferences.SymbolsTests.V1.MTTestLib1.dll;

            // this is not related to this test, but need by lib2 (don't want to add a new dll resource)
            var ref2 = TestReferences.SymbolsTests.MultiModule.Assembly;

            // Roslyn give CS0104 for now
            var comp = CreateCompilation(text, new List<MetadataReference> { ref1, ref2 });
            comp.VerifyDiagnostics(
                // (6,16): error CS0433: The type 'Class1' exists in both 'MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' and 'MultiModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                //         void M(Class1 p) {}
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "Class1").WithArguments("MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Class1", "MultiModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,9): error CS0433: The type 'Class1' exists in both 'MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' and 'MultiModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                //         Class1 var;
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "Class1").WithArguments("MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Class1", "MultiModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,16): warning CS0169: The field 'NS.Test.var' is never used
                //         Class1 var;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "var").WithArguments("NS.Test.var"));

            text = @"
class Class1
{
}

class Test
{
    Class1 var;
}
";
            comp = CreateCompilation(new SyntaxTree[] { Parse(text, "goo.cs") }, new List<MetadataReference> { ref1 });
            comp.VerifyDiagnostics(
                // goo.cs(8,5): warning CS0436: The type 'Class1' in 'goo.cs' conflicts with the imported type 'Class1' in 'MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'goo.cs'.
                //     Class1 var;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Class1").WithArguments("goo.cs", "Class1", "MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Class1"),
                // goo.cs(8,12): warning CS0169: The field 'Test.var' is never used
                //     Class1 var;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "var").WithArguments("Test.var")
                );
        }

        /// <summary>
        /// import - lib1: namespace A { namespace B { .class C {}.. }} 
        ///      vs. lib2: Namespace A { class B { class C{} }    }} - use C
        /// </summary>
        [Fact]
        public void CS0434ERR_SameFullNameNsAgg01()
        {
            var text = @"

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            // var v = new N1.N2.A();
        }

        void M(N1.N2.A p) { }
    }
}
";
            var ref1 = TestReferences.DiagnosticTests.ErrTestLib11.dll;
            var ref2 = TestReferences.DiagnosticTests.ErrTestLib02.dll;

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new List<MetadataReference> { ref1, ref2 },
                // new ErrorDescription { Code = (int)ErrorCode.ERR_SameFullNameNsAgg, Line = 9, Column = 28 }, // Dev10 one error
                new ErrorDescription { Code = (int)ErrorCode.ERR_SameFullNameNsAgg, Line = 12, Column = 19 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact()]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0438ERR_SameFullNameThisAggThisNs01()
        // I (Neal Gafter) was not able to reproduce this in Dev10 using the batch compiler, but the Dev10
        // background compiler will emit this diagnostic (along with one other) for this code:
        // namespace NS {
        //   namespace A { }
        //   public class A { }
        // }
        // class B : NS.A { }

        //Compiling the scenario below using the native compiler gives CS0438 for me (Ed).
        {
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0438
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                });

            comp.VerifyDiagnostics(
                // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
                //             Console.WriteLine(typeof(Util.A));   // CS0438
                Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact()]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0438ERR_SameFullNameThisAggThisNs02()
        {
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0438
        }
    }

    class Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // Test.cs(9,38): error CS0438: The type 'NS.Util' in 'Test.cs' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A));   // CS0438
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("Test.cs", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0438ERR_SameFullNameThisAggThisNs03()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A).Module);   
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0438ERR_SameFullNameThisAggThisNs04()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // Test.cs(9,38): error CS0438: The type 'NS.Util' in 'Test.cs' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A));   // CS0101
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("Test.cs", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // Test.cs(9,38): error CS0438: The type 'NS.Util' in 'Test.cs' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A));   // CS0101
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("Test.cs", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0438ERR_SameFullNameThisAggThisNs05()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A).Module);   
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0438ERR_SameFullNameThisAggThisNs06()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // Test.cs(9,38): error CS0438: The type 'NS.Util' in 'Test.cs' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A));   
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("Test.cs", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // Test.cs(9,38): error CS0438: The type 'NS.Util' in 'Test.cs' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
    //             Console.WriteLine(typeof(Util.A));   
    Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("Test.cs", "NS.Util", "ErrTestMod02.netmodule", "NS.Util")
                );
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0436WRN_SameFullNameThisAggAgg_01()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A).Module);   
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    new CSharpCompilationReference(lib)
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod01.netmodule").VerifyDiagnostics(
                // (9,38): warning CS0436: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the imported type 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'ErrTestMod01.netmodule'.
                //             Console.WriteLine(typeof(Util.A).Module);   
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util"));

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod01.netmodule").VerifyDiagnostics(
                // (9,38): warning CS0436: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the imported type 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'ErrTestMod01.netmodule'.
                //             Console.WriteLine(typeof(Util.A).Module);   
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util"));
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0436WRN_SameFullNameThisAggAgg_02()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A).Module);   
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod02.netmodule").VerifyDiagnostics(
    // (9,43): warning CS0436: The type 'NS.Util.A' in 'ErrTestMod02.netmodule' conflicts with the imported type 'NS.Util.A' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'ErrTestMod02.netmodule'.
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("ErrTestMod02.netmodule", "NS.Util.A", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util.A")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod02.netmodule").VerifyDiagnostics(
    // (9,43): warning CS0436: The type 'NS.Util.A' in 'ErrTestMod02.netmodule' conflicts with the imported type 'NS.Util.A' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'ErrTestMod02.netmodule'.
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("ErrTestMod02.netmodule", "NS.Util.A", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util.A")
                );
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0435WRN_SameFullNameThisNsAgg_01()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A).Module);   
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod02.netmodule").VerifyDiagnostics(
    // (9,38): warning CS0435: The namespace 'NS.Util' in 'ErrTestMod02.netmodule' conflicts with the imported type 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'ErrTestMod02.netmodule'.
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "Util").WithArguments("ErrTestMod02.netmodule", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod02.netmodule").VerifyDiagnostics(
    // (9,38): warning CS0435: The namespace 'NS.Util' in 'ErrTestMod02.netmodule' conflicts with the imported type 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'ErrTestMod02.netmodule'.
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "Util").WithArguments("ErrTestMod02.netmodule", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util")
                );
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(568953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568953")]
        public void CS0437WRN_SameFullNameThisAggNs_01()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A).Module);   
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    new CSharpCompilationReference(lib)
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod01.netmodule").VerifyDiagnostics(
    // (9,38): warning CS0437: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the imported namespace 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'ErrTestMod01.netmodule'.
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "ErrTestMod01.netmodule").VerifyDiagnostics(
    // (9,38): warning CS0437: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the imported namespace 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'ErrTestMod01.netmodule'.
    //             Console.WriteLine(typeof(Util.A).Module);   
    Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_01()
        {
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_02()
        {
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util
    {
        class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_03()
        {
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util
    {
        class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                });

            comp.VerifyDiagnostics(
    // (15,15): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_04()
        {
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   
        }
    }

    namespace Util
    {
        class A<T> {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                });

            // ILVerify: Assembly or module not found: ErrTestMod02
            CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_05()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_06()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_07()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_08()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    class Util {}
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,11): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     class Util {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_09()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_10()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_11()
        {
            var libSource = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS"),
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS"),
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_12()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_13()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util"),
    // Test.cs(9,38): warning CS0435: The namespace 'NS.Util' in 'Test.cs' conflicts with the imported type 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'Test.cs'.
    //             Console.WriteLine(typeof(Util.A));   // CS0101
    Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "Util").WithArguments("Test.cs", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                }, sourceFileName: "Test.cs");

            comp.VerifyDiagnostics(
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util"),
    // Test.cs(9,38): warning CS0435: The namespace 'NS.Util' in 'Test.cs' conflicts with the imported type 'NS.Util' in 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'Test.cs'.
    //             Console.WriteLine(typeof(Util.A));   // CS0101
    Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "Util").WithArguments("Test.cs", "NS.Util", "Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_14()
        {
            var libSource = @"
namespace NS
{
    public class Util 
    {
        public class A {}
    }
}
";

            var lib = CreateCompilation(libSource, assemblyName: "Lib", options: TestOptions.ReleaseDll);

            CompileAndVerify(lib);
            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }

    namespace Util 
    {
        public class A {}
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    new CSharpCompilationReference(lib)
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS"),
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    MetadataReference.CreateFromImage(lib.EmitToArray())
                });

            comp.VerifyDiagnostics(
    // (13,15): error CS0101: The namespace 'NS' already contains a definition for 'Util'
    //     namespace Util 
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "Util").WithArguments("Util", "NS"),
    // (15,22): error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
    //         public class A {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "A").WithArguments("A", "NS.Util")
                );
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_15()
        {
            var mod3Source = @"
namespace NS
{
    namespace Util 
    {
        public class A {}
    }
}
";

            var mod3Ref = CreateCompilation(mod3Source, options: TestOptions.ReleaseModule, assemblyName: "ErrTestMod03").EmitToImageReference();

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    mod3Ref
                });

            //ErrTestMod03.netmodule: error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            comp.VerifyDiagnostics(
                // ErrTestMod03.netmodule: error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("A", "NS.Util"),
                // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
                //             Console.WriteLine(typeof(Util.A));   // CS0101
                Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util"));

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    s_mod1.GetReference(),
                    mod3Ref
                });

            //ErrTestMod01.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            //ErrTestMod03.netmodule: error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            comp.VerifyDiagnostics(
                // ErrTestMod03.netmodule: error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("A", "NS.Util"),
                // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
                //             Console.WriteLine(typeof(Util.A));   // CS0101
                Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util"));

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    mod3Ref,
                    s_mod1.GetReference()
                });

            //ErrTestMod03.netmodule: error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            //ErrTestMod01.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            comp.VerifyDiagnostics(
                // ErrTestMod03.netmodule: error CS0101: The namespace 'NS.Util' already contains a definition for 'A'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("A", "NS.Util"),
                // (9,38): error CS0438: The type 'NS.Util' in 'ErrTestMod01.netmodule' conflicts with the namespace 'NS.Util' in 'ErrTestMod02.netmodule'
                //             Console.WriteLine(typeof(Util.A));   // CS0101
                Diagnostic(ErrorCode.ERR_SameFullNameThisAggThisNs, "Util").WithArguments("ErrTestMod01.netmodule", "NS.Util", "ErrTestMod02.netmodule", "NS.Util"));
        }

        [Fact()]
        [WorkItem(530676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530676")]
        public void CS0101ERR_DuplicateNameInNS_16()
        {
            var mod3Source = @"
namespace NS
{
    internal class Util 
    {
        public class A {}
    }
}";

            var mod3Ref = CreateCompilation(mod3Source, options: TestOptions.ReleaseModule, assemblyName: "ErrTestMod03").EmitToImageReference();

            var text = @"using System;

namespace NS
{
    public class Test
    {
        public static void Main()
        {
            Console.WriteLine(typeof(Util.A));   // CS0101
        }
    }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    s_mod2.GetReference(),
                    mod3Ref
                });

            //ErrTestMod03.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
            //ErrTestMod01.netmodule: (Location of symbol related to previous error)
            comp.VerifyDiagnostics(
                // ErrTestMod03.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("Util", "NS"));

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod2.GetReference(),
                    s_mod1.GetReference(),
                    mod3Ref
                });

            //ErrTestMod01.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            //ErrTestMod03.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
            //ErrTestMod02.netmodule: (Location of symbol related to previous error)
            comp.VerifyDiagnostics(
                // ErrTestMod03.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("Util", "NS"));

            comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    s_mod1.GetReference(),
                    mod3Ref,
                    s_mod2.GetReference()
                });

            //ErrTestMod03.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
            //ErrTestMod01.netmodule: (Location of symbol related to previous error)
            comp.VerifyDiagnostics(
                // ErrTestMod03.netmodule: error CS0101: The namespace 'NS' already contains a definition for 'Util'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("Util", "NS"));
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(641639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641639")]
        public void Bug641639()
        {
            var ModuleA01 = @"
class A01 {
public static int AT = (new { field = 1 }).field;
}
";

            var ModuleA01Ref = CreateCompilation(ModuleA01, options: TestOptions.ReleaseModule, assemblyName: "ModuleA01").EmitToImageReference();

            var ModuleB01 = @"
class B01{
public static int AT = (new { field = 2 }).field;
}
";

            var ModuleB01Ref = CreateCompilation(ModuleB01, options: TestOptions.ReleaseModule, assemblyName: "ModuleB01").EmitToImageReference();

            var text = @"
   class Test {
    static void Main() {
      System.Console.WriteLine(""{0} + {1} = {2}"", A01.AT, A01.AT, B01.AT);
          A01.AT = B01.AT;
      System.Console.WriteLine(""{0} = {1}"", A01.AT, B01.AT);
     }
}
";

            var comp = CreateCompilation(text,
                new List<MetadataReference>()
                {
                    ModuleA01Ref,
                    ModuleB01Ref
                }, TestOptions.ReleaseExe);

            Assert.Equal(1, comp.Assembly.Modules[1].GlobalNamespace.GetTypeMembers("<ModuleA01>f__AnonymousType0", 1).Length);
            Assert.Equal(1, comp.Assembly.Modules[2].GlobalNamespace.GetTypeMembers("<ModuleB01>f__AnonymousType0", 1).Length);

            CompileAndVerify(comp, expectedOutput: @"1 + 1 = 2
2 = 2");
        }

        [Fact]
        public void NameCollisionWithAddedModule_01()
        {
            var ilSource =
@"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.module ITest20Mod.netmodule
// MVID: {53AFCDC2-985A-43AE-928E-89B4A4017344}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00EC0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi ITest20<T>
{
} // end of class ITest20
";

            ImmutableArray<Byte> ilBytes;
            using (var reference = IlasmUtilities.CreateTempAssembly(ilSource, prependDefaultHeader: false))
            {
                ilBytes = ReadFromFile(reference.Path);
            }

            var moduleRef = ModuleMetadata.CreateFromImage(ilBytes).GetReference();

            var source =
@"
interface ITest20
{}
";

            var compilation = CreateCompilation(source,
                new List<MetadataReference>()
                {
                    moduleRef
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
    // error CS8004: Type 'ITest20<T>' exported from module 'ITest20Mod.netmodule' conflicts with type declared in primary module of this assembly.
    Diagnostic(ErrorCode.ERR_ExportedTypeConflictsWithDeclaration).WithArguments("ITest20<T>", "ITest20Mod.netmodule")
                );
        }

        [Fact]
        public void NameCollisionWithAddedModule_02()
        {
            var ilSource =
@"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.module mod_1_1.netmodule
// MVID: {98479031-F5D1-443D-AF73-CF21159C1BCF}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00D30000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi ns.c1<T>
{
} 

.class interface public abstract auto ansi c2<T>
{
} 

.class interface public abstract auto ansi ns.C3<T>
{
} 

.class interface public abstract auto ansi C4<T>
{
} 

.class interface public abstract auto ansi NS1.c5<T>
{
} 
";

            ImmutableArray<Byte> ilBytes;
            using (var reference = IlasmUtilities.CreateTempAssembly(ilSource, prependDefaultHeader: false))
            {
                ilBytes = ReadFromFile(reference.Path);
            }

            var moduleRef1 = ModuleMetadata.CreateFromImage(ilBytes).GetReference();

            var mod2Source =
@"
namespace ns
{
    public interface c1
    {}

    public interface c3
    {}
}

public interface c2
{}

public interface c4
{}

namespace ns1
{
    public interface c5
    {}
}
";

            var moduleRef2 = CreateCompilation(mod2Source, options: TestOptions.ReleaseModule, assemblyName: "mod_1_2").EmitToImageReference();

            var compilation = CreateCompilation("",
                new List<MetadataReference>()
                {
                    moduleRef1,
                    moduleRef2
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
    // error CS8005: Type 'c2' exported from module 'mod_1_2.netmodule' conflicts with type 'c2<T>' exported from module 'mod_1_1.netmodule'.
    Diagnostic(ErrorCode.ERR_ExportedTypesConflict).WithArguments("c2", "mod_1_2.netmodule", "c2<T>", "mod_1_1.netmodule"),
    // error CS8005: Type 'ns.c1' exported from module 'mod_1_2.netmodule' conflicts with type 'ns.c1<T>' exported from module 'mod_1_1.netmodule'.
    Diagnostic(ErrorCode.ERR_ExportedTypesConflict).WithArguments("ns.c1", "mod_1_2.netmodule", "ns.c1<T>", "mod_1_1.netmodule")
                );
        }

        [Fact]
        public void NameCollisionWithAddedModule_03()
        {
            var forwardedTypesSource =
@"
public class CF1
{}

namespace ns
{
    public class CF2
    {
    }
}

public class CF3<T>
{}
";

            var forwardedTypes1 = CreateCompilation(forwardedTypesSource, options: TestOptions.ReleaseDll, assemblyName: "ForwardedTypes1");
            var forwardedTypes1Ref = new CSharpCompilationReference(forwardedTypes1);

            var forwardedTypes2 = CreateCompilation(forwardedTypesSource, options: TestOptions.ReleaseDll, assemblyName: "ForwardedTypes2");
            var forwardedTypes2Ref = new CSharpCompilationReference(forwardedTypes2);

            var forwardedTypesModRef = CreateCompilation(forwardedTypesSource,
                                                                options: TestOptions.ReleaseModule,
                                                                assemblyName: "forwardedTypesMod").
                                       EmitToImageReference();

            var modSource =
@"
[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(CF1))]
[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(ns.CF2))]
";

            var module1_FT1_Ref = CreateCompilation(modSource,
                                                                options: TestOptions.ReleaseModule,
                                                                assemblyName: "module1_FT1",
                                                                references: new MetadataReference[] { forwardedTypes1Ref }).
                                  EmitToImageReference();

            var module2_FT1_Ref = CreateCompilation(modSource,
                                                                options: TestOptions.ReleaseModule,
                                                                assemblyName: "module2_FT1",
                                                                references: new MetadataReference[] { forwardedTypes1Ref }).
                                  EmitToImageReference();

            var module3_FT2_Ref = CreateCompilation(modSource,
                                                                options: TestOptions.ReleaseModule,
                                                                assemblyName: "module3_FT2",
                                                                references: new MetadataReference[] { forwardedTypes2Ref }).
                                  EmitToImageReference();

            var module4_Ref = CreateCompilation("[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(CF3<int>))]",
                                                                options: TestOptions.ReleaseModule,
                                                                assemblyName: "module4_FT1",
                                                                references: new MetadataReference[] { forwardedTypes1Ref }).
                                  EmitToImageReference();

            var compilation = CreateCompilation(forwardedTypesSource,
                new List<MetadataReference>()
                {
                    module1_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
                // error CS8006: Forwarded type 'ns.CF2' conflicts with type declared in primary module of this assembly.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration).WithArguments("ns.CF2"),
                // error CS8006: Forwarded type 'CF1' conflicts with type declared in primary module of this assembly.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration).WithArguments("CF1"));

            compilation = CreateCompilation(modSource,
                new List<MetadataReference>()
                {
                    module1_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll);

            // Exported types in .NET modules cause PEVerify to fail on some platforms.
            CompileAndVerify(compilation, verify: Verification.Skipped).VerifyDiagnostics();

            compilation = CreateCompilation("[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(CF3<byte>))]",
                new List<MetadataReference>()
                {
                    module4_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll);

            CompileAndVerify(compilation, verify: Verification.Skipped).VerifyDiagnostics();

            compilation = CreateCompilation(modSource,
                new List<MetadataReference>()
                {
                    module1_FT1_Ref,
                    forwardedTypes2Ref,
                    new CSharpCompilationReference(forwardedTypes1, aliases: ImmutableArray.Create("FT1"))
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
                // error CS8007: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' forwarded to assembly 'ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_ForwardedTypesConflict).WithArguments("ns.CF2", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "ns.CF2", "ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // error CS8007: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' forwarded to assembly 'ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_ForwardedTypesConflict).WithArguments("CF1", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CF1", "ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            compilation = CreateCompilation(
@"
extern alias FT1; 

[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(FT1::CF1))]
[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(FT1::ns.CF2))]
",
                new List<MetadataReference>()
                {
                    forwardedTypesModRef,
                    new CSharpCompilationReference(forwardedTypes1, ImmutableArray.Create("FT1"))
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
                // error CS8008: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' exported from module 'forwardedTypesMod.netmodule'.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType).WithArguments("CF1", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CF1", "forwardedTypesMod.netmodule"),
                // error CS8008: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' exported from module 'forwardedTypesMod.netmodule'.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType).WithArguments("ns.CF2", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "ns.CF2", "forwardedTypesMod.netmodule"));

            compilation = CreateCompilation("",
                new List<MetadataReference>()
                {
                    forwardedTypesModRef,
                    module1_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
                // error CS8008: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' exported from module 'forwardedTypesMod.netmodule'.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType).WithArguments("ns.CF2", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "ns.CF2", "forwardedTypesMod.netmodule"),
                // error CS8008: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' exported from module 'forwardedTypesMod.netmodule'.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType).WithArguments("CF1", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CF1", "forwardedTypesMod.netmodule"));

            compilation = CreateCompilation("",
                new List<MetadataReference>()
                {
                    module1_FT1_Ref,
                    forwardedTypesModRef,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
                // error CS8008: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' exported from module 'forwardedTypesMod.netmodule'.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType).WithArguments("ns.CF2", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "ns.CF2", "forwardedTypesMod.netmodule"),
                // error CS8008: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' exported from module 'forwardedTypesMod.netmodule'.
                Diagnostic(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType).WithArguments("CF1", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CF1", "forwardedTypesMod.netmodule"));

            compilation = CreateCompilation("",
                new List<MetadataReference>()
                {
                    module1_FT1_Ref,
                    module2_FT1_Ref,
                    forwardedTypes1Ref
                }, TestOptions.ReleaseDll);

            CompileAndVerify(compilation, verify: Verification.Skipped).VerifyDiagnostics();

            compilation = CreateCompilation("",
                new List<MetadataReference>()
                {
                    module1_FT1_Ref,
                    module3_FT2_Ref,
                    forwardedTypes1Ref,
                    forwardedTypes2Ref
                }, TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(
                // error CS8007: Type 'ns.CF2' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'ns.CF2' forwarded to assembly 'ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_ForwardedTypesConflict).WithArguments("ns.CF2", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "ns.CF2", "ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // error CS8007: Type 'CF1' forwarded to assembly 'ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' conflicts with type 'CF1' forwarded to assembly 'ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_ForwardedTypesConflict).WithArguments("CF1", "ForwardedTypes1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CF1", "ForwardedTypes2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void CS0441ERR_SealedStaticClass01()
        {
            var text = @"namespace NS
{
    static sealed class Test
    {
        public static int Main()
        {
            return 1;
        }
    }

    sealed static class StaticClass : Test //verify 'sealed' works
    {
    }

    static class Derived : StaticClass //verify 'sealed' works
    {
        // Test tst = new Test(); //verify 'static' works
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,25): error CS0441: 'NS.Test': a type cannot be both static and sealed
                Diagnostic(ErrorCode.ERR_SealedStaticClass, "Test").WithArguments("NS.Test"),
                // (11,25): error CS0441: 'NS.StaticClass': a type cannot be both static and sealed
                Diagnostic(ErrorCode.ERR_SealedStaticClass, "StaticClass").WithArguments("NS.StaticClass"),

                //CONSIDER: Dev10 skips these cascading errors

                // (11,39): error CS07: Static class 'NS.StaticClass' cannot derive from type 'NS.Test'. Static classes must derive from object.
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "Test").WithArguments("NS.StaticClass", "NS.Test"),
                // (15,28): error CS07: Static class 'NS.Derived' cannot derive from type 'NS.StaticClass'. Static classes must derive from object.
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "StaticClass").WithArguments("NS.Derived", "NS.StaticClass"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0442ERR_PrivateAbstractAccessor()
        {
            var source =
@"abstract class MyClass
{
    public abstract int P { get; private set; } // CS0442
    protected abstract object Q { private get; set; } // CS0442
    internal virtual object R { private get; set; } // no error
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,42): error CS0442: 'MyClass.P.set': abstract properties cannot have private accessors
                //     public abstract int P { get; private set; } // CS0442
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "set").WithArguments("MyClass.P.set").WithLocation(3, 42),
                // (4,43): error CS0442: 'MyClass.Q.get': abstract properties cannot have private accessors
                //     protected abstract object Q { private get; set; } // CS0442
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("MyClass.Q.get").WithLocation(4, 43));
        }

        [Fact]
        public void CS0448ERR_BadIncDecRetType()
        {
            // Note that the wording of this error message has changed slightly from the native compiler.
            var text = @"public struct S
{
    public static S? operator ++(S s) { return new S(); }   // CS0448
    public static S? operator --(S s) { return new S(); }   // CS0448
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,31): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     public static S? operator ++(S s) { return new S(); }   // CS0448
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, "++"),
                // (4,31): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     public static S? operator --(S s) { return new S(); }   // CS0448
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, "--"));
        }

        [Fact]
        public void CS0450ERR_RefValBoundWithClass()
        {
            var source =
@"interface I { }
class A { }
class B<T> { }
class C<T1, T2, T3, T4, T5, T6, T7>
    where T1 : class, I
    where T2 : struct, I
    where T3 : class, A
    where T4 : struct, B<T5>
    where T6 : class, T5
    where T7 : struct, T5
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,23): error CS0450: 'A': cannot specify both a constraint class and the 'class' or 'struct' constraint
                Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "A").WithArguments("A").WithLocation(7, 23),
                // (8, 24): error CS0450: 'B<T5>': cannot specify both a constraint class and the 'class' or 'struct' constraint
                Diagnostic(ErrorCode.ERR_RefValBoundWithClass, "B<T5>").WithArguments("B<T5>").WithLocation(8, 24));
        }

        [Fact]
        public void CS0452ERR_RefConstraintNotSatisfied()
        {
            var source =
@"interface I { }
class A { }
class B<T> where T : class { }
class C
{
    static void F<U>() where U : class { }
    static void M1<T>()
    {
        new B<T>();
        F<T>();
    }
    static void M2<T>() where T : class
    {
        new B<T>();
        F<T>();
    }
    static void M3<T>() where T : struct
    {
        new B<T>();
        F<T>();
    }
    static void M4<T>() where T : new()
    {
        new B<T>();
        F<T>();
    }
    static void M5<T>() where T : I
    {
        new B<T>();
        F<T>();
    }
    static void M6<T>() where T : A
    {
        new B<T>();
        F<T>();
    }
    static void M7<T, U>() where T : U
    {
        new B<T>();
        F<T>();
    }
    static void M8()
    {
        new B<int?>();
        F<int?>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,15): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(9, 15),
                // (10,9): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(10, 9),
                // (19,15): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(19, 15),
                // (20,9): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(20, 9),
                // (24,15): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(24, 15),
                // (25,9): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(25, 9),
                // (29,15): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(29, 15),
                // (30,9): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(30, 9),
                // (39,15): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(39, 15),
                // (40,9): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(40, 9),
                // (44,15): error CS0452: The type 'int?' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int?").WithArguments("B<T>", "T", "int?").WithLocation(44, 15),
                // (45,9): error CS0452: The type 'int?' must be a reference type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F<int?>").WithArguments("C.F<U>()", "U", "int?").WithLocation(45, 9));
        }

        [Fact]
        public void CS0453ERR_ValConstraintNotSatisfied01()
        {
            var source =
@"interface I { }
class A { }
class B<T> where T : struct { }
class C
{
    static void F<U>() where U : struct { }
    static void M1<T>()
    {
        new B<T>();
        F<T>();
    }
    static void M2<T>() where T : class
    {
        new B<T>();
        F<T>();
    }
    static void M3<T>() where T : struct
    {
        new B<T>();
        F<T>();
    }
    static void M4<T>() where T : new()
    {
        new B<T>();
        F<T>();
    }
    static void M5<T>() where T : I
    {
        new B<T>();
        F<T>();
    }
    static void M6<T>() where T : A
    {
        new B<T>();
        F<T>();
    }
    static void M7<T, U>() where T : U
    {
        new B<T>();
        F<T>();
    }
    static void M8()
    {
        new B<int?>();
        F<int?>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,15): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(9, 15),
                // (10,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(10, 9),
                // (14,15): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(14, 15),
                // (15,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(15, 9),
                // (24,15): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(24, 15),
                // (25,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(25, 9),
                // (29,15): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(29, 15),
                // (30,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(30, 9),
                // (34,15): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(34, 15),
                // (35,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(35, 9),
                // (39,15): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B<T>", "T", "T").WithLocation(39, 15),
                // (40,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<T>").WithArguments("C.F<U>()", "U", "T").WithLocation(40, 9),
                // (44,15): error CS0453: The type 'int?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "int?").WithArguments("B<T>", "T", "int?").WithLocation(44, 15),
                // (45,9): error CS0453: The type 'int?' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'C.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<int?>").WithArguments("C.F<U>()", "U", "int?").WithLocation(45, 9));
        }

        [Fact]
        public void CS0453ERR_ValConstraintNotSatisfied02()
        {
            var source =
@"abstract class A<X, Y>
{
    internal static void F<U>() where U : struct { }
    internal abstract void M<U>() where U : X, Y;
}
class B1 : A<int?, object>
{
    internal override void M<U>()
    {
        F<U>();
    }
}
class B2 : A<object, int?>
{
    internal override void M<U>()
    {
        F<U>();
    }
}
class B3 : A<object, int>
{
    internal override void M<U>()
    {
        F<U>();
    }
}
class B4<T> : A<object, T>
{
    internal override void M<U>()
    {
        F<U>();
    }
}
class B5<T> : A<object, T> where T : struct
{
    internal override void M<U>()
    {
        F<U>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,9): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'A<int?, object>.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<U>").WithArguments("A<int?, object>.F<U>()", "U", "U").WithLocation(10, 9),
                // (17,9): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'A<object, int?>.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<U>").WithArguments("A<object, int?>.F<U>()", "U", "U").WithLocation(17, 9),
                // (31,9): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'A<object, T>.F<U>()'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<U>").WithArguments("A<object, T>.F<U>()", "U", "U").WithLocation(31, 9));
        }

        [Fact]
        public void CS0454ERR_CircularConstraint01()
        {
            var source =
@"class A<T> where T : T
{
}
class B<T, U, V>
    where V : T
    where U : V
    where T : U
{
}
delegate void D<T1, T2, T3>()
    where T1 : T3
    where T2 : T2
    where T3 : T1;";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,9): error CS0454: Circular constraint dependency involving 'T' and 'T'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T").WithArguments("T", "T").WithLocation(1, 9),
                // (4,9): error CS0454: Circular constraint dependency involving 'T' and 'V'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T").WithArguments("T", "V").WithLocation(4, 9),
                // (10,17): error CS0454: Circular constraint dependency involving 'T1' and 'T3'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T1").WithArguments("T1", "T3").WithLocation(10, 17),
                // (10,21): error CS0454: Circular constraint dependency involving 'T2' and 'T2'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T2").WithArguments("T2", "T2").WithLocation(10, 21));
        }

        [Fact]
        public void CS0454ERR_CircularConstraint02()
        {
            var source =
@"interface I
{
    void M<T, U, V>()
        where T : V, new()
        where U : class, V
        where V : U;
}
class A<T> { }
class B<T, U>
    where T : U
    where U : A<U>, U
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,18): error CS0454: Circular constraint dependency involving 'V' and 'U'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "V").WithArguments("V", "U").WithLocation(3, 18),
                // (9,12): error CS0454: Circular constraint dependency involving 'U' and 'U'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "U").WithArguments("U", "U").WithLocation(9, 12));
        }

        [Fact]
        public void CS0454ERR_CircularConstraint03()
        {
            var source =
@"interface I<T1, T2, T3, T4, T5>
    where T1 : T2, T4
    where T2 : T3
    where T3 : T1
    where T4 : T5
    where T5 : T1
{
}
class C<T1, T2, T3, T4, T5>
    where T1 : T2, T3
    where T2 : T3, T4
    where T3 : T4, T5
    where T5 : T2, T3
{
}
struct S<T1, T2, T3, T4, T5>
    where T4 : T1
    where T5 : T2
    where T1 : T3
    where T2 : T4
    where T3 : T5
{
}
delegate void D<T1, T2, T3, T4>()
    where T1 : T2
    where T2 : T3, T4
    where T3 : T4
    where T4 : T2;";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,13): error CS0454: Circular constraint dependency involving 'T1' and 'T3'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T1").WithArguments("T1", "T3").WithLocation(1, 13),
                // (1,13): error CS0454: Circular constraint dependency involving 'T1' and 'T5'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T1").WithArguments("T1", "T5").WithLocation(1, 13),
                // (9,13): error CS0454: Circular constraint dependency involving 'T2' and 'T5'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T2").WithArguments("T2", "T5").WithLocation(9, 13),
                // (9,17): error CS0454: Circular constraint dependency involving 'T3' and 'T5'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T3").WithArguments("T3", "T5").WithLocation(9, 17),
                // (16,10): error CS0454: Circular constraint dependency involving 'T1' and 'T4'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T1").WithArguments("T1", "T4").WithLocation(16, 10),
                // (24,21): error CS0454: Circular constraint dependency involving 'T2' and 'T4'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T2").WithArguments("T2", "T4").WithLocation(24, 21));
        }

        [Fact]
        public void CS0454ERR_CircularConstraint04()
        {
            var source =
@"interface I<T>
    where U : U
{
}
class C<T, U>
    where T : U
    where U : class
    where U : T
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,11): error CS0699: 'I<T>' does not define type parameter 'U'
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "I<T>").WithLocation(2, 11),
                // (8,11): error CS0409: A constraint clause has already been specified for type parameter 'U'. All of the constraints for a type parameter must be specified in a single where clause.
                Diagnostic(ErrorCode.ERR_DuplicateConstraintClause, "U").WithArguments("U").WithLocation(8, 11));
        }

        [Fact]
        public void CS0455ERR_BaseConstraintConflict01()
        {
            var source =
@"class A<T> { }
class B : A<int> { }
class C<T, U>
    where T : B, U
    where U : A<int>
{
}
class D<T, U>
    where T : A<T>
    where U : B, T
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,12): error CS0455: Type parameter 'U' inherits conflicting constraints 'A<T>' and 'B'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "A<T>", "B").WithLocation(8, 12));
        }

        [Fact]
        public void CS0455ERR_BaseConstraintConflict02()
        {
            var source =
@"class A<T>
{
    internal virtual void M1<U>() where U : struct, T { }
    internal virtual void M2<U>() where U : class, T { }
}
class B1 : A<object>
{
    internal override void M1<U>() { }
    internal override void M2<U>() { }
}
class B2 : A<int>
{
    internal override void M1<U>() { }
    internal override void M2<U>() { }
}
class B3 : A<string>
{
    internal override void M1<U>() { }
    internal override void M2<U>() { }
}
class B4 : A<int?>
{
    internal override void M1<T>() { }
    internal override void M2<X>() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,31): error CS0455: Type parameter 'U' inherits conflicting constraints 'int' and 'class'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "int", "class").WithLocation(14, 31),
                // (18,31): error CS0455: Type parameter 'U' inherits conflicting constraints 'string' and 'System.ValueType'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "string", "System.ValueType").WithLocation(18, 31),
                // (18,31): error CS0455: Type parameter 'U' inherits conflicting constraints 'System.ValueType' and 'struct'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "System.ValueType", "struct").WithLocation(18, 31),
                // (23,31): error CS0455: Type parameter 'T' inherits conflicting constraints 'int?' and 'struct'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "T").WithArguments("T", "int?", "struct").WithLocation(23, 31),
                // (24,31): error CS0455: Type parameter 'X' inherits conflicting constraints 'int?' and 'class'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "X").WithArguments("X", "int?", "class").WithLocation(24, 31));
        }

        [Fact]
        public void CS0456ERR_ConWithValCon()
        {
            var source =
@"class A<T, U>
    where T : struct
    where U : T
{
}
class B<T> where T : struct
{
    void M<U>() where U : T { }
    struct S<U> where U : T { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0456: Type parameter 'T' has the 'struct' constraint so 'T' cannot be used as a constraint for 'U'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "U").WithArguments("U", "T").WithLocation(1, 12),
                // (8,12): error CS0456: Type parameter 'T' has the 'struct' constraint so 'T' cannot be used as a constraint for 'U'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "U").WithArguments("U", "T").WithLocation(8, 12),
                // (9,14): error CS0456: Type parameter 'T' has the 'struct' constraint so 'T' cannot be used as a constraint for 'U'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "U").WithArguments("U", "T").WithLocation(9, 14));
        }

        [Fact]
        public void CS0462ERR_AmbigOverride()
        {
            var text = @"class C<T> 
{
   public virtual void F(T t) {}
   public virtual void F(int t) {}
}

class D : C<int> 
{
   public override void F(int t) {}   // CS0462
}
";
            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetLatest);
            Assert.Equal(RuntimeUtilities.IsCoreClrRuntime, comp.Assembly.RuntimeSupportsCovariantReturnsOfClasses);
            if (comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation)
            {
                comp.VerifyDiagnostics(
                    // (9,25): error CS0462: The inherited members 'C<T>.F(T)' and 'C<T>.F(int)' have the same signature in type 'D', so they cannot be overridden
                    //    public override void F(int t) {}   // CS0462
                    Diagnostic(ErrorCode.ERR_AmbigOverride, "F").WithArguments("C<T>.F(T)", "C<T>.F(int)", "D").WithLocation(9, 25)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,24): warning CS1957: Member 'D.F(int)' overrides 'C<int>.F(int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //    public virtual void F(T t) {}
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "F").WithArguments("C<int>.F(int)", "D.F(int)").WithLocation(3, 24),
                    // (9,25): error CS0462: The inherited members 'C<T>.F(T)' and 'C<T>.F(int)' have the same signature in type 'D', so they cannot be overridden
                    //    public override void F(int t) {}   // CS0462
                    Diagnostic(ErrorCode.ERR_AmbigOverride, "F").WithArguments("C<T>.F(T)", "C<T>.F(int)", "D").WithLocation(9, 25)
                    );
            }
        }

        [Fact]
        public void CS0466ERR_ExplicitImplParams()
        {
            var text = @"interface I
{
    void M1(params int[] a);
    void M2(int[] a);
}

class C1 : I
{
    //implicit implementations can add or remove 'params'
    public virtual void M1(int[] a) { }
    public virtual void M2(params int[] a) { }
}

class C2 : I
{
    //explicit implementations can remove but not add 'params'
    void I.M1(int[] a) { }
    void I.M2(params int[] a) { } //CS0466
}

class C3 : C1
{
    //overrides can add or remove 'params'
    public override void M1(params int[] a) { }
    public override void M2(int[] a) { }
}

class C4 : C1
{
    //hiding methods can add or remove 'params'
    public new void M1(params int[] a) { }
    public new void M2(int[] a) { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (18,12): error CS0466: 'C2.I.M2(params int[])' should not have a params parameter since 'I.M2(int[])' does not
                Diagnostic(ErrorCode.ERR_ExplicitImplParams, "M2").WithArguments("C2.I.M2(params int[])", "I.M2(int[])"));
        }

        [Fact]
        public void CS0470ERR_MethodImplementingAccessor()
        {
            var text = @"interface I
{
    int P { get; }
}

class MyClass : I
{
    public int get_P() { return 0; }   // CS0470
    public int P2 { get { return 0; } }   // OK
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 6, Column = 17 }, //Dev10 doesn't include this
                new ErrorDescription { Code = (int)ErrorCode.ERR_MethodImplementingAccessor, Line = 8, Column = 16 });
        }

        [Fact]
        public void CS0500ERR_AbstractHasBody01()
        {
            var text = @"namespace NS
{
    abstract public class @clx
    {
        abstract public void M1() { }
        internal abstract object M2() { return null; }
        protected abstract internal void M3(sbyte p) { }
        public abstract object P { get { return null; } set { } }
        public abstract event System.Action E { add { } remove { } }
        public abstract event System.Action X { add => throw null; remove => throw null; }
    } // class clx
}
";
            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (5,30): error CS0500: 'clx.M1()' cannot declare a body because it is marked abstract
                //         abstract public void M1() { }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M1").WithArguments("NS.clx.M1()").WithLocation(5, 30),
                // (6,34): error CS0500: 'clx.M2()' cannot declare a body because it is marked abstract
                //         internal abstract object M2() { return null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M2").WithArguments("NS.clx.M2()").WithLocation(6, 34),
                // (7,42): error CS0500: 'clx.M3(sbyte)' cannot declare a body because it is marked abstract
                //         protected abstract internal void M3(sbyte p) { }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M3").WithArguments("NS.clx.M3(sbyte)").WithLocation(7, 42),
                // (8,36): error CS0500: 'clx.P.get' cannot declare a body because it is marked abstract
                //         public abstract object P { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("NS.clx.P.get").WithLocation(8, 36),
                // (8,57): error CS0500: 'clx.P.set' cannot declare a body because it is marked abstract
                //         public abstract object P { get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("NS.clx.P.set").WithLocation(8, 57),
                // (9,47): error CS8712: 'clx.E': abstract event cannot use event accessor syntax
                //         public abstract event System.Action E { add { } remove { } }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("NS.clx.E").WithLocation(9, 47),
                // (10,47): error CS8712: 'clx.X': abstract event cannot use event accessor syntax
                //         public abstract event System.Action X { add => throw null; remove => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("NS.clx.X").WithLocation(10, 47));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0501ERR_ConcreteMissingBody01()
        {
            var text = @"

namespace NS
{
    public class @clx<T>
    {
        public void M1(T t);
        internal V M2<V>();
        protected internal void M3(sbyte p);
        public static int operator+(clx<T> c);
    } // class clx
}
";
            CreateCompilation(text).VerifyDiagnostics(
// (7,21): error CS0501: 'NS.clx<T>.M1(T)' must declare a body because it is not marked abstract, extern, or partial
//         public void M1(T t);
Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M1").WithArguments("NS.clx<T>.M1(T)"),

// (8,20): error CS0501: 'NS.clx<T>.M2<V>()' must declare a body because it is not marked abstract, extern, or partial
//         internal V M2<V>();
Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M2").WithArguments("NS.clx<T>.M2<V>()"),

// (9,33): error CS0501: 'NS.clx<T>.M3(sbyte)' must declare a body because it is not marked abstract, extern, or partial
//         protected internal void M3(sbyte p);
Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M3").WithArguments("NS.clx<T>.M3(sbyte)"),

                // (10,35): error CS0501: 'NS.clx<T>.operator +(NS.clx<T>)' must declare a body because it is not marked abstract, extern, or partial
                //         public static int operator+(clx<T> c);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "+").WithArguments("NS.clx<T>.operator +(NS.clx<T>)"));
        }

        [Fact]
        public void CS0501ERR_ConcreteMissingBody02()
        {
            var text = @"abstract class C
{
    public int P { get; set { } }
    public int Q { get { return 0; } set; }
    public extern object R { get; } // no error
    protected abstract object S { set; } // no error
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,20): error CS0501: 'C.P.get' must declare a body because it is not marked abstract, extern, or partial
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("C.P.get"),
                // (4,38): error CS0501: 'C.Q.set' must declare a body because it is not marked abstract, extern, or partial
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("C.Q.set"),
                // (5,30): warning CS0626: Method, operator, or accessor 'C.R.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("C.R.get"));
        }

        [Fact]
        public void CS0501ERR_ConcreteMissingBody03()
        {
            var source =
@"class C
{
    public C();
    internal abstract C(C c);
    extern public C(object o); // no error
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,12): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()"),
                // (4,23): error CS0106: The modifier 'abstract' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("abstract"),
                // (5,19): warning CS0824: Constructor 'C.C(object)' is marked external
                Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "C").WithArguments("C.C(object)"));
        }

        [Fact]
        public void CS0502ERR_AbstractAndSealed01()
        {
            var text = @"namespace NS
{
    abstract public class @clx
    {
        abstract public void M1();
        abstract protected void M2<T>(T t);
        internal abstract object P { get; }
        abstract public event System.Action E;
    } // class clx

    abstract public class @cly : clx
    {
        abstract sealed override public void M1();
        abstract sealed override protected void M2<T>(T t);
        internal abstract sealed override object P { get; }
        public abstract sealed override event System.Action E;
    } // class cly
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractAndSealed, Line = 13, Column = 46 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractAndSealed, Line = 14, Column = 49 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractAndSealed, Line = 15, Column = 50 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractAndSealed, Line = 16, Column = 61 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0503ERR_AbstractNotVirtual01()
        {
            var source = @"namespace NS
{
    abstract public class @clx
    {
        abstract virtual internal void M1();
        abstract virtual protected void M2<T>(T t);
        virtual abstract public object P { get; set; }
        virtual abstract public event System.Action E;
    } // class clx
}
";

            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (7,40): error CS0503: The abstract property 'clx.P' cannot be marked virtual
                //         virtual abstract public object P { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P").WithArguments("property", "NS.clx.P").WithLocation(7, 40),
                // (6,41): error CS0503: The abstract method 'clx.M2<T>(T)' cannot be marked virtual
                //         abstract virtual protected void M2<T>(T t);
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "M2").WithArguments("method", "NS.clx.M2<T>(T)").WithLocation(6, 41),
                // (5,40): error CS0503: The abstract method 'clx.M1()' cannot be marked virtual
                //         abstract virtual internal void M1();
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "M1").WithArguments("method", "NS.clx.M1()").WithLocation(5, 40),
                // (8,53): error CS0503: The abstract event 'clx.E' cannot be marked virtual
                //         virtual abstract public event System.Action E;
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "E").WithArguments("event", "NS.clx.E").WithLocation(8, 53));

            var nsNamespace = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var clxClass = nsNamespace.GetMembers("clx").Single() as NamedTypeSymbol;
            Assert.Equal(9, clxClass.GetMembers().Length);
        }

        [Fact]
        public void CS0504ERR_StaticConstant()
        {
            var text = @"namespace x
{
    abstract public class @clx
    {
        static const int i = 0;   // CS0504, cannot be both static and const
        abstract public void f();
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstant, Line = 5, Column = 26 });
        }

        [Fact]
        public void CS0505ERR_CantOverrideNonFunction()
        {
            var text = @"public class @clx
{
   public int i;
}

public class @cly : clx
{
   public override int i() { return 0; }   // CS0505
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonFunction, Line = 8, Column = 24 });
        }

        [Fact]
        public void CS0506ERR_CantOverrideNonVirtual()
        {
            var text = @"namespace MyNameSpace
{
    abstract public class ClassX
    {
        public int f()
        {
            return 0;
        }
    }

    public class ClassY : ClassX
    {
        public override int f()   // CS0506
        {
            return 0;
        }

        public static int Main()
        {
            return 0;
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 13, Column = 29 });
        }

        private const string s_typeWithMixedProperty = @"
.class public auto ansi beforefieldinit Base_VirtGet_Set
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname newslot virtual  
          instance int32  get_Prop() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  }

  .method public hidebysig specialname 
          instance void  set_Prop(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  }
  .property instance int32 Prop()
  {
    .get instance int32 Base_VirtGet_Set::get_Prop()
    .set instance void Base_VirtGet_Set::set_Prop(int32)
  }
  
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}

.class public auto ansi beforefieldinit Base_Get_VirtSet
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname 
          instance int32  get_Prop() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  }

  .method public hidebysig specialname newslot virtual  
          instance void  set_Prop(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  }
  .property instance int32 Prop()
  {
    .get instance int32 Base_Get_VirtSet::get_Prop()
    .set instance void Base_Get_VirtSet::set_Prop(int32)
  }
  
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}
";

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void CS0506ERR_CantOverrideNonVirtual_Imported()
        {
            var text = @"
class Derived1 : Base_Get_VirtSet
{
    public override int Prop
    {
        get { return base.Prop; }
        set { base.Prop = value; }
    }
}

class Derived2 : Base_VirtGet_Set
{
    public override int Prop
    {
        get { return base.Prop; }
        set { base.Prop = value; }
    }
}
";
            var comp = CreateCompilationWithILAndMscorlib40(text, s_typeWithMixedProperty);
            comp.VerifyDiagnostics(
                // (4,25): error CS0506: 'Derived2.Prop.set': cannot override inherited member 'Base_VirtGet_Set.Prop.set' because it is not marked virtual, abstract, or override
                //         get { return base.Prop; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "get").WithArguments("Derived1.Prop.get", "Base_Get_VirtSet.Prop.get"),
                // (16,9): error CS0506: 'Derived2.Prop.set': cannot override inherited member 'Base_VirtGet_Set.Prop.set' because it is not marked virtual, abstract, or override
                //         set { base.Prop = value; }
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "set").WithArguments("Derived2.Prop.set", "Base_VirtGet_Set.Prop.set"));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void CS0506ERR_CantOverrideNonVirtual_Imported_NO_ERROR()
        {
            var text = @"
class Derived1 : Base_Get_VirtSet
{
    public override int Prop
    {
        set { base.Prop = value; }
    }
}

class Derived2 : Base_VirtGet_Set
{
    public override int Prop
    {
        get { return base.Prop; }
    }
}
";
            var comp = CreateCompilationWithILAndMscorlib40(text, s_typeWithMixedProperty);
            comp.VerifyDiagnostics();
        }

        [WorkItem(539586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539586")]
        [Fact]
        public void CS0506ERR_CantOverrideNonVirtual02()
        {
            var text = @"public class BaseClass
{
    public virtual int Test()
    {
        return 1;
    }
}

public class BaseClass2 : BaseClass
{
    public static int Test() // Warning CS0114
    {
        return 1;
    }
}

public class MyClass : BaseClass2
{
    public override int Test() // Error CS0506
    {
        return 2;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 11, Column = 23, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonVirtual, Line = 19, Column = 25 });
        }

        [Fact]
        public void CS0507ERR_CantChangeAccessOnOverride()
        {
            var text = @"abstract public class @clx
{
    virtual protected void f() { }
}

public class @cly : clx
{
    public override void f() { }   // CS0507
    public static void Main() { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeAccessOnOverride, Line = 8, Column = 26 });
        }

        [Fact]
        public void CS0508ERR_CantChangeReturnTypeOnOverride()
        {
            var text = @"abstract public class Clx
{
    public int i = 0;
    abstract public int F();
}

public class Cly : Clx
{
    public override double F()
    {
        return 0.0;   // CS0508
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantChangeReturnTypeOnOverride, Line = 9, Column = 28 });
        }

        [WorkItem(540325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540325")]
        [Fact]
        public void CS0508ERR_CantChangeReturnTypeOnOverride2()
        {
            // When the overriding and overridden methods differ in their generic method 
            // type parameter names, the error message should state what the return type
            // type should be on the overriding method using its type parameter names, 
            // rather than using the return type of the overridden method.

            var text = @"
class G 
{ 
internal virtual T GM<T>(T t) { return t; } 
}
class GG : G 
{ 
internal override void GM<V>(V v) { } 
}

";
            CreateCompilation(text).VerifyDiagnostics(
// (8,24): error CS0508: 'GG.GM<V>(V)': return type must be 'V' to match overridden member 'G.GM<T>(T)'
// internal override void GM<V>(V v) { } 
Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "GM").WithArguments("GG.GM<V>(V)", "G.GM<T>(T)", "V")
                );
        }

        [Fact]
        public void CS0509ERR_CantDeriveFromSealedType01()
        {
            var source =
@"namespace NS
{
    public struct @stx { }
    public sealed class @clx {}

    public class @cly : clx {}
    public class @clz : stx { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,25): error CS0509: 'cly': cannot derive from sealed type 'clx'
                //     public class @cly : clx {}
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "clx").WithArguments("NS.cly", "NS.clx").WithLocation(6, 25),
                // (7,25): error CS0509: 'clz': cannot derive from sealed type 'stx'
                //     public class @clz : stx { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "stx").WithArguments("NS.clz", "NS.stx").WithLocation(7, 25));
        }

        [Fact]
        public void CS0509ERR_CantDeriveFromSealedType02()
        {
            var source =
@"namespace N1 { enum E { A, B } }
namespace N2
{
    class C : N1.E { }
    class D : System.Int32 { }
    class E : int { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0509: 'E': cannot derive from sealed type 'int'
                //     class E : int { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "int").WithArguments("N2.E", "int").WithLocation(6, 15),
                // (4,15): error CS0509: 'C': cannot derive from sealed type 'E'
                //     class C : N1.E { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "N1.E").WithArguments("N2.C", "N1.E").WithLocation(4, 15),
                // (5,15): error CS0509: 'D': cannot derive from sealed type 'int'
                //     class D : System.Int32 { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "System.Int32").WithArguments("N2.D", "int").WithLocation(5, 15));
        }

        [Fact]
        public void CS0513ERR_AbstractInConcreteClass01()
        {
            var source =
@"namespace NS
{
    public class @clx
    {
        abstract public void M1();
        internal abstract object M2();
        protected abstract internal void M3(sbyte p);
        public abstract object P { get; set; }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,36): error CS0513: 'clx.P.get' is abstract but it is contained in non-abstract type 'clx'
                //         public abstract object P { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "get").WithArguments("NS.clx.P.get", "NS.clx").WithLocation(8, 36),
                // (8,41): error CS0513: 'clx.P.set' is abstract but it is contained in non-abstract type 'clx'
                //         public abstract object P { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "set").WithArguments("NS.clx.P.set", "NS.clx").WithLocation(8, 41),
                // (6,34): error CS0513: 'clx.M2()' is abstract but it is contained in non-abstract type 'clx'
                //         internal abstract object M2();
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M2").WithArguments("NS.clx.M2()", "NS.clx").WithLocation(6, 34),
                // (7,42): error CS0513: 'clx.M3(sbyte)' is abstract but it is contained in non-abstract type 'clx'
                //         protected abstract internal void M3(sbyte p);
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M3").WithArguments("NS.clx.M3(sbyte)", "NS.clx").WithLocation(7, 42),
                // (5,30): error CS0513: 'clx.M1()' is abstract but it is contained in non-abstract type 'clx'
                //         abstract public void M1();
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M1").WithArguments("NS.clx.M1()", "NS.clx").WithLocation(5, 30));
        }

        [Fact]
        public void CS0513ERR_AbstractInConcreteClass02()
        {
            var text = @"
class C
{
    public abstract event System.Action E;
    public abstract int this[int x] { get; set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,41): error CS0513: 'C.E' is abstract but it is contained in non-abstract type 'C'
                //     public abstract event System.Action E;
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "E").WithArguments("C.E", "C"),
                // (5,39): error CS0513: 'C.this[int].get' is abstract but it is contained in non-abstract type 'C'
                //     public abstract int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "get").WithArguments("C.this[int].get", "C"),
                // (5,44): error CS0513: 'C.this[int].set' is abstract but it is contained in non-abstract type 'C'
                //     public abstract int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "set").WithArguments("C.this[int].set", "C"));
        }

        [Fact]
        public void CS0515ERR_StaticConstructorWithAccessModifiers01()
        {
            var text = @"namespace NS
{
    static public class @clx
    {
        private static clx() { }

        class C<T, V>
        {
            internal static C() { }
        }
    }

    public class @clz
    {
        public static clz() { }

        struct S
        {
            internal static S() { }
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstructorWithAccessModifiers, Line = 5, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstructorWithAccessModifiers, Line = 9, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstructorWithAccessModifiers, Line = 15, Column = 23 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticConstructorWithAccessModifiers, Line = 19, Column = 29 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        /// <summary>
        /// Some - /nostdlib - no mscorlib
        /// </summary>
        [Fact]
        public void CS0518ERR_PredefinedTypeNotFound01()
        {
            var text = @"namespace NS
{
    class Test
    {
        static int Main()
        {
            return 1;
        }
    }
}
";

            CreateEmptyCompilation(text).VerifyDiagnostics(
                // (3,11): error CS0518: Predefined type 'System.Object' is not defined or imported
                //     class Test
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Object"),
                // (5,16): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //         static int Main()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32"),
                // (7,20): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //             return 1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1").WithArguments("System.Int32"),
                // (3,11): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                //     class Test
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test").WithArguments("object", "0"));
        }

        //[Fact(Skip = "Bad test case")]
        //public void CS0520ERR_PredefinedTypeBadType()
        //{
        //    var text = @"";
        //    var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
        //        new ErrorDescription { Code = (int)ErrorCode.ERR_PredefinedTypeBadType, Line = 5, Column = 26 }
        //        );
        //}

        [Fact]
        public void CS0523ERR_StructLayoutCycle01()
        {
            var text =
@"struct A
{
    A F; // CS0523
}
struct B
{
    C F; // CS0523
    C G; // no additional error
}
struct C
{
    B G; // CS0523
}
struct D<T>
{
    D<D<object>> F; // CS0523
}
struct E
{
    F<E> F; // no error
}
class F<T>
{
    E G; // no error
}
struct G
{
    H<G> F; // CS0523
}
struct H<T>
{
    G G; // CS0523
}
struct J
{
    static J F; // no error
}
struct K
{
    static L F; // no error
}
struct L
{
    static K G; // no error
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,7): error CS0523: Struct member 'B.F' of type 'C' causes a cycle in the struct layout
                //     C F; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "F").WithArguments("B.F", "C").WithLocation(7, 7),
                // (12,7): error CS0523: Struct member 'C.G' of type 'B' causes a cycle in the struct layout
                //     B G; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "G").WithArguments("C.G", "B").WithLocation(12, 7),
                // (16,18): error CS0523: Struct member 'D<T>.F' of type 'D<D<object>>' causes a cycle in the struct layout
                //     D<D<object>> F; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "F").WithArguments("D<T>.F", "D<D<object>>").WithLocation(16, 18),
                // (32,7): error CS0523: Struct member 'H<T>.G' of type 'G' causes a cycle in the struct layout
                //     G G; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "G").WithArguments("H<T>.G", "G").WithLocation(32, 7),
                // (28,10): error CS0523: Struct member 'G.F' of type 'H<G>' causes a cycle in the struct layout
                //     H<G> F; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "F").WithArguments("G.F", "H<G>").WithLocation(28, 10),
                // (3,7): error CS0523: Struct member 'A.F' of type 'A' causes a cycle in the struct layout
                //     A F; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "F").WithArguments("A.F", "A").WithLocation(3, 7),
                // (16,18): warning CS0169: The field 'D<T>.F' is never used
                //     D<D<object>> F; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("D<T>.F").WithLocation(16, 18),
                // (32,7): warning CS0169: The field 'H<T>.G' is never used
                //     G G; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("H<T>.G").WithLocation(32, 7),
                // (12,7): warning CS0169: The field 'C.G' is never used
                //     B G; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("C.G").WithLocation(12, 7),
                // (40,14): warning CS0169: The field 'K.F' is never used
                //     static L F; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("K.F").WithLocation(40, 14),
                // (28,10): warning CS0169: The field 'G.F' is never used
                //     H<G> F; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("G.F").WithLocation(28, 10),
                // (8,7): warning CS0169: The field 'B.G' is never used
                //     C G; // no additional error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("B.G").WithLocation(8, 7),
                // (36,14): warning CS0169: The field 'J.F' is never used
                //     static J F; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("J.F").WithLocation(36, 14),
                // (3,7): warning CS0169: The field 'A.F' is never used
                //     A F; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("A.F").WithLocation(3, 7),
                // (20,10): warning CS0169: The field 'E.F' is never used
                //     F<E> F; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("E.F").WithLocation(20, 10),
                // (44,14): warning CS0169: The field 'L.G' is never used
                //     static K G; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("L.G").WithLocation(44, 14),
                // (7,7): warning CS0169: The field 'B.F' is never used
                //     C F; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("B.F").WithLocation(7, 7),
                // (24,7): warning CS0169: The field 'F<T>.G' is never used
                //     E G; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("F<T>.G").WithLocation(24, 7)
                );
        }

        [Fact]
        public void CS0523ERR_StructLayoutCycle02()
        {
            var text =
@"struct A
{
    A P { get; set; } // CS0523
    A Q { get; set; } // no additional error
}
struct B
{
    C P { get; set; } // CS0523 (no error in Dev10!)
}
struct C
{
    B Q { get; set; } // CS0523 (no error in Dev10!)
}
struct D<T>
{
    D<D<object>> P { get; set; } // CS0523
}
struct E
{
    F<E> P { get; set; } // no error
}
class F<T>
{
    E Q { get; set; } // no error
}
struct G
{
    H<G> P { get; set; } // CS0523
    G Q; // no additional error
}
struct H<T>
{
    G Q; // CS0523
}
struct J
{
    static J P { get; set; } // no error
}
struct K
{
    static L P { get; set; } // no error
}
struct L
{
    static K Q { get; set; } // no error
}
struct M
{
    N P { get; set; } // no error
}
struct N
{
    M Q // no error
    {
        get { return new M(); }
        set { }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,7): error CS0523: Struct member 'A.P' of type 'A' causes a cycle in the struct layout
                //     A P { get; set; } // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("A.P", "A"),
                // (8,7): error CS0523: Struct member 'B.P' of type 'C' causes a cycle in the struct layout
                //     C P { get; set; } // CS0523 (no error in Dev10!)
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("B.P", "C"),
                // (12,7): error CS0523: Struct member 'C.Q' of type 'B' causes a cycle in the struct layout
                //     B Q { get; set; } // CS0523 (no error in Dev10!)
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "Q").WithArguments("C.Q", "B"),
                // (16,18): error CS0523: Struct member 'D<T>.P' of type 'D<D<object>>' causes a cycle in the struct layout
                //     D<D<object>> P { get; set; } // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("D<T>.P", "D<D<object>>"),
                // (28,10): error CS0523: Struct member 'G.P' of type 'H<G>' causes a cycle in the struct layout
                //     H<G> P { get; set; } // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("G.P", "H<G>"),
                // (33,7): error CS0523: Struct member 'H<T>.Q' of type 'G' causes a cycle in the struct layout
                //     G Q; // CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "Q").WithArguments("H<T>.Q", "G"),
                // (29,7): warning CS0169: The field 'G.Q' is never used
                //     G Q; // no additional error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Q").WithArguments("G.Q"),
                // (33,7): warning CS0169: The field 'H<T>.Q' is never used
                //     G Q; // CS0523
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Q").WithArguments("H<T>.Q")
                );
        }

        [WorkItem(540215, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540215")]
        [Fact]
        public void CS0523ERR_StructLayoutCycle03()
        {
            // Static fields should not be considered when
            // determining struct cycles. (Note: Dev10 does
            // report these cases as errors though.)
            var text =
@"struct A
{
    B F; // no error
}
struct B
{
    static A G; // no error
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,7): warning CS0169: The field 'A.F' is never used
                //     B F; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("A.F").WithLocation(3, 7),
                // (7,14): warning CS0169: The field 'B.G' is never used
                //     static A G; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("B.G").WithLocation(7, 14)
                );
        }

        [WorkItem(541629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541629")]
        [Fact]
        public void CS0523ERR_StructLayoutCycle04()
        {
            var text =
@"struct E {
}

struct X<T> {
    public T t;
}

struct Y {
    public X<Z> xz;
}

struct Z {
    public X<E> xe;
    public X<Y> xy;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,17): error CS0523: Struct member 'Y.xz' of type 'X<Z>' causes a cycle in the struct layout
                //     public X<Z> xz;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "xz").WithArguments("Y.xz", "X<Z>").WithLocation(9, 17),
                // (14,17): error CS0523: Struct member 'Z.xy' of type 'X<Y>' causes a cycle in the struct layout
                //     public X<Y> xy;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "xy").WithArguments("Z.xy", "X<Y>").WithLocation(14, 17),
                // (9,17): warning CS0649: Field 'Y.xz' is never assigned to, and will always have its default value 
                //     public X<Z> xz;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "xz").WithArguments("Y.xz", "").WithLocation(9, 17),
                // (14,17): warning CS0649: Field 'Z.xy' is never assigned to, and will always have its default value 
                //     public X<Y> xy;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "xy").WithArguments("Z.xy", "").WithLocation(14, 17),
                // (5,14): warning CS0649: Field 'X<T>.t' is never assigned to, and will always have its default value 
                //     public T t;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "t").WithArguments("X<T>.t", "").WithLocation(5, 14),
                // (13,17): warning CS0649: Field 'Z.xe' is never assigned to, and will always have its default value 
                //     public X<E> xe;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "xe").WithArguments("Z.xe", "").WithLocation(13, 17)
                );
        }

        [WorkItem(541629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541629")]
        [Fact]
        public void CS0523ERR_StructLayoutCycle05()
        {
            var text =
@"struct X<T>
{
    public T t;
}

struct W<T>
{
    X<W<W<T>>> x;
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,16): error CS0523: Struct member 'W<T>.x' of type 'X<W<W<T>>>' causes a cycle in the struct layout
                //     X<W<W<T>>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("W<T>.x", "X<W<W<T>>>"),
                // (3,14): warning CS0649: Field 'X<T>.t' is never assigned to, and will always have its default value 
                //     public T t;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "t").WithArguments("X<T>.t", ""),
                // (8,16): warning CS0169: The field 'W<T>.x' is never used
                //     X<W<W<T>>> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("W<T>.x")
                );
        }

        [Fact]
        public void CS0523ERR_StructLayoutCycle06()
        {
            var text =
@"struct S1<T, U>
{
    S1<object, object> F;
}
struct S2<T, U>
{
    S2<U, T> F;
}
struct S3<T>
{
    T F;
}
struct S4<T>
{
    S3<S3<T>> F;
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,24): error CS0523: Struct member 'S1<T, U>.F' of type 'S1<object, object>' causes a cycle in the struct layout
                //     S1<object, object> F;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "F").WithArguments("S1<T, U>.F", "S1<object, object>").WithLocation(3, 24),
                // (7,14): error CS0523: Struct member 'S2<T, U>.F' of type 'S2<U, T>' causes a cycle in the struct layout
                //     S2<U, T> F;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "F").WithArguments("S2<T, U>.F", "S2<U, T>").WithLocation(7, 14),
                // (7,14): warning CS0169: The field 'S2<T, U>.F' is never used
                //     S2<U, T> F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("S2<T, U>.F").WithLocation(7, 14),
                // (11,7): warning CS0169: The field 'S3<T>.F' is never used
                //     T F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("S3<T>.F").WithLocation(11, 7),
                // (15,15): warning CS0169: The field 'S4<T>.F' is never used
                //     S3<S3<T>> F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("S4<T>.F").WithLocation(15, 15),
                // (3,24): warning CS0169: The field 'S1<T, U>.F' is never used
                //     S1<object, object> F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("S1<T, U>.F").WithLocation(3, 24)
                );
        }

        [WorkItem(872954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/872954")]
        [Fact]
        public void CS0523ERR_StructLayoutCycle07()
        {
            var text =
@"struct S0<T>
{
    static S0<T> x;
}
struct S1<T>
{
    class C { }
    static S1<C> x;
}
struct S2<T>
{
    struct S { }
    static S2<S> x;
}
struct S3<T>
{
    interface I { }
    static S3<I> x;
}
struct S4<T>
{
    delegate void D();
    static S4<D> x;
}
struct S5<T>
{
    enum E { }
    static S5<E> x;
}
struct S6<T>
{
    static S6<T[]> x;
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,18): error CS0523: Struct member 'S1<T>.x' of type 'S1<S1<T>.C>' causes a cycle in the struct layout
                //     static S1<C> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("S1<T>.x", "S1<S1<T>.C>").WithLocation(8, 18),
                // (13,18): error CS0523: Struct member 'S2<T>.x' of type 'S2<S2<T>.S>' causes a cycle in the struct layout
                //     static S2<S> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("S2<T>.x", "S2<S2<T>.S>").WithLocation(13, 18),
                // (18,18): error CS0523: Struct member 'S3<T>.x' of type 'S3<S3<T>.I>' causes a cycle in the struct layout
                //     static S3<I> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("S3<T>.x", "S3<S3<T>.I>").WithLocation(18, 18),
                // (23,18): error CS0523: Struct member 'S4<T>.x' of type 'S4<S4<T>.D>' causes a cycle in the struct layout
                //     static S4<D> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("S4<T>.x", "S4<S4<T>.D>").WithLocation(23, 18),
                // (28,18): error CS0523: Struct member 'S5<T>.x' of type 'S5<S5<T>.E>' causes a cycle in the struct layout
                //     static S5<E> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("S5<T>.x", "S5<S5<T>.E>").WithLocation(28, 18),
                // (32,20): error CS0523: Struct member 'S6<T>.x' of type 'S6<T[]>' causes a cycle in the struct layout
                //     static S6<T[]> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("S6<T>.x", "S6<T[]>").WithLocation(32, 20),
                // (8,18): warning CS0169: The field 'S1<T>.x' is never used
                //     static S1<C> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S1<T>.x").WithLocation(8, 18),
                // (23,18): warning CS0169: The field 'S4<T>.x' is never used
                //     static S4<D> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S4<T>.x").WithLocation(23, 18),
                // (18,18): warning CS0169: The field 'S3<T>.x' is never used
                //     static S3<I> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S3<T>.x").WithLocation(18, 18),
                // (3,18): warning CS0169: The field 'S0<T>.x' is never used
                //     static S0<T> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S0<T>.x").WithLocation(3, 18),
                // (13,18): warning CS0169: The field 'S2<T>.x' is never used
                //     static S2<S> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S2<T>.x").WithLocation(13, 18),
                // (28,18): warning CS0169: The field 'S5<T>.x' is never used
                //     static S5<E> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S5<T>.x").WithLocation(28, 18),
                // (32,20): warning CS0169: The field 'S6<T>.x' is never used
                //     static S6<T[]> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S6<T>.x").WithLocation(32, 20));
        }

        [Fact]
        public void CS0524ERR_InterfacesCannotContainTypes01()
        {
            var text = @"namespace NS
{
    public interface IGoo
    {
        interface IBar { }
        public class @cly {}
        struct S { }
        private enum E { zero,  one }
        // internal delegate void MyDel(object p); // delegates not in scope yet
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7);

            comp.VerifyDiagnostics(
                // (5,19): error CS8107: Feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         interface IBar { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "IBar").WithArguments("default interface implementation", "8.0").WithLocation(5, 19),
                // (6,22): error CS8107: Feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         public class @cly {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "@cly").WithArguments("default interface implementation", "8.0").WithLocation(6, 22),
                // (7,16): error CS8107: Feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         struct S { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "S").WithArguments("default interface implementation", "8.0").WithLocation(7, 16),
                // (8,22): error CS8107: Feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         private enum E { zero,  one }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "E").WithArguments("default interface implementation", "8.0").WithLocation(8, 22)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [Fact]
        public void CS0525ERR_InterfacesCantContainFields01()
        {
            var text = @"namespace NS
{
    public interface IGoo
    {
        string field1;
        const ulong field2 = 0;
        public IGoo field3;
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (5,16): error CS0525: Interfaces cannot contain instance fields
                //         string field1;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field1").WithLocation(5, 16),
                // (6,21): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //         const ulong field2 = 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "field2").WithArguments("default interface implementation", "8.0").WithLocation(6, 21),
                // (7,21): error CS0525: Interfaces cannot contain instance fields
                //         public IGoo field3;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "field3").WithLocation(7, 21)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0526ERR_InterfacesCantContainConstructors01()
        {
            var text = @"namespace NS
{
    public interface IGoo
    {
         public IGoo() {}
         internal IGoo(object p1, ref  long p2) { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_InterfacesCantContainConstructors, Line = 5, Column = 17 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_InterfacesCantContainConstructors, Line = 6, Column = 19 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0527ERR_NonInterfaceInInterfaceList01()
        {
            var text = @"namespace NS
{
    class C { }

    public struct S : object, C
    {
        interface IGoo : C
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 5, Column = 23 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 5, Column = 31 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NonInterfaceInInterfaceList, Line = 7, Column = 26 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0528ERR_DuplicateInterfaceInBaseList01()
        {
            var text = @"namespace NS
{
    public interface IGoo {}
    public interface IBar { }
    public class C : IGoo, IGoo
    {
    }
    struct S : IBar, IGoo, IBar
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateInterfaceInBaseList, Line = 5, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateInterfaceInBaseList, Line = 8, Column = 28 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        /// <summary>
        /// Extra errors - expected
        /// </summary>
        [Fact]
        public void CS0529ERR_CycleInInterfaceInheritance01()
        {
            var text = @"namespace NS
{
    class AA : BB { }
    class BB : CC { }
    class CC : I3 { }

    interface I1 : I2 { }
    interface I2 : I3 { }
    interface I3 : I1 { }
}";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CycleInInterfaceInheritance, Line = 7, Column = 15 }, // Extra
                new ErrorDescription { Code = (int)ErrorCode.ERR_CycleInInterfaceInheritance, Line = 8, Column = 15 }, // Extra
                new ErrorDescription { Code = (int)ErrorCode.ERR_CycleInInterfaceInheritance, Line = 9, Column = 15 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0531ERR_InterfaceMemberHasBody01()
        {
            var text = @"namespace NS
{
    public interface IGoo
    {
        int M1() { return 0; }    // CS0531
        void M2<T>(T t) { }
        object P { get { return null; } }
    }

    interface IBar<T, V>
    {
        V M1(T t) { return default(V); }
        void M2(ref T t, out V v) { v = default(V); }
        T P { get { return default(T) } set { } }
    }
}
";
            var comp = CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (14,39): error CS1002: ; expected
                //         T P { get { return default(T) } set { } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(14, 39)
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void AbstractConstructor()
        {
            var text = @"namespace NS
{
    public class C
    {
        abstract C();
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadMemberFlag, Line = 5, Column = 18 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void StaticFixed()
        {
            var text = @"unsafe
struct S
{
    public static fixed int x[10];
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,29): error CS0106: The modifier 'static' is not valid for this item
                //     public static fixed int x[10];
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("static"));
        }

        [WorkItem(895401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/895401")]
        [Fact]
        public void VolatileFixed()
        {
            var text = @"unsafe
struct S
{
    public volatile fixed int x[10];
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,29): error CS0106: The modifier 'volatile' is not valid for this item
                //     public static fixed int x[10];
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "x").WithArguments("volatile"));
        }

        [WorkItem(895401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/895401")]
        [Fact]
        public void ReadonlyConst()
        {
            var text = @"
class C
{
    private readonly int F1 = 123;
    private const int F2 = 123;
    private readonly const int F3 = 123;
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,32): error CS0106: The modifier 'readonly' is not valid for this item
                //     private readonly const int F3 = 123;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F3").WithArguments("readonly"),
                // (4,26): warning CS0414: The field 'C.F1' is assigned but its value is never used
                //     private readonly int F1 = 123;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F1").WithArguments("C.F1"));
        }

        [Fact]
        public void CS0533ERR_HidingAbstractMethod()
        {
            var text = @"namespace x
{
    abstract public class @a
    {
        abstract public void f();
        abstract public void g();
    }

    abstract public class @b : a
    {
        new abstract public void f();   // CS0533
        new abstract internal void g();   //fine since internal
        public static void Main()
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_HidingAbstractMethod, Line = 11, Column = 34 });
        }

        [WorkItem(539629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539629")]
        [Fact]
        public void CS0533ERR_HidingAbstractMethod02()
        {
            var text = @"
abstract public class B1
{
    public abstract float goo { set; }
}

abstract class A1 : B1
{
    new protected enum @goo { } // CS0533

    abstract public class B2
    {
        protected abstract void goo();
    }

    abstract class A2 : B2
    {
        new public delegate object @goo(); // CS0533
    }
}

namespace NS
{
    abstract public class B3
    {
        public abstract void goo();
    }

    abstract class A3 : B3
    {
        new protected double[] goo;  // CS0533
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (31,32): error CS0533: 'A3.goo' hides inherited abstract member 'B3.goo()'
                //         new protected double[] goo;  // CS0533
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "goo").WithArguments("NS.A3.goo", "NS.B3.goo()").WithLocation(31, 32),
                // (9,24): error CS0533: 'A1.goo' hides inherited abstract member 'B1.goo'
                //     new protected enum @goo { } // CS0533
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "@goo").WithArguments("A1.goo", "B1.goo").WithLocation(9, 24),
                // (18,36): error CS0533: 'A1.A2.goo' hides inherited abstract member 'A1.B2.goo()'
                //         new public delegate object @goo(); // CS0533
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "@goo").WithArguments("A1.A2.goo", "A1.B2.goo()").WithLocation(18, 36),
                // (31,32): warning CS0649: Field 'A3.goo' is never assigned to, and will always have its default value null
                //         new protected double[] goo;  // CS0533
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "goo").WithArguments("NS.A3.goo", "null").WithLocation(31, 32));
        }

        [WorkItem(540464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540464")]
        [Fact]
        public void CS0533ERR_HidingAbstractMethod03()
        {
            var text = @"
public abstract class A
{
public abstract void f();
public abstract void g();
public abstract void h();
}
public abstract class B : A
{
public override void g() { }
}
public abstract class C : B
{
public void h(int a) { }
}
public abstract class D: C
{
public new int f; // expected CS0533: 'C.f' hides inherited abstract member 'A.f()'
public new int g; // no error
public new int h; // no CS0533 here in Dev10, but I'm not sure why not. (VB gives error for this case)
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (18,16): error CS0533: 'D.f' hides inherited abstract member 'A.f()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "f").WithArguments("D.f", "A.f()"));
        }

        [WorkItem(539629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539629")]
        [Fact]
        public void CS0533ERR_HidingAbstractMethod_Combinations()
        {
            var text = @"
abstract class Base
{
    public abstract void M();
    public abstract int P { get; set; }
    public abstract class C { }
}

abstract class Derived1 : Base
{
    public new abstract void M();
    public new abstract void P();
    public new abstract void C();
}

abstract class Derived2 : Base
{
    public new abstract int M { get; set; }
    public new abstract int P { get; set; }
    public new abstract int C { get; set; }
}

abstract class Derived3 : Base
{
    public new abstract class M { }
    public new abstract class P { }
    public new abstract class C { }
}

abstract class Derived4 : Base
{
    public new void M() { }
    public new void P() { }
    public new void C() { }
}

abstract class Derived5 : Base
{
    public new int M { get; set; }
    public new int P { get; set; }
    public new int C { get; set; }
}

abstract class Derived6 : Base
{
    public new class M { }
    public new class P { }
    public new class C { }
}

abstract class Derived7 : Base
{
    public new static void M() { }
    public new static int P { get; set; }
    public new static class C { }
}

abstract class Derived8 : Base
{
    public new static int M = 1;
    public new static class P { };
    public new const int C = 2;
}";
            // CONSIDER: dev10 reports each hidden accessor separately, but that seems silly
            CreateCompilation(text).VerifyDiagnostics(
                // (11,30): error CS0533: 'Derived1.M()' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived1.M()", "Base.M()"),
                // (12,30): error CS0533: 'Derived1.P()' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived1.P()", "Base.P"),
                // (18,29): error CS0533: 'Derived2.M' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived2.M", "Base.M()"),
                // (19,29): error CS0533: 'Derived2.P' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived2.P", "Base.P"),
                // (25,31): error CS0533: 'Derived3.M' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived3.M", "Base.M()"),
                // (26,31): error CS0533: 'Derived3.P' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived3.P", "Base.P"),
                // (32,21): error CS0533: 'Derived4.M()' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived4.M()", "Base.M()"),
                // (33,21): error CS0533: 'Derived4.P()' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived4.P()", "Base.P"),
                // (39,20): error CS0533: 'Derived5.M' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived5.M", "Base.M()"),
                // (40,20): error CS0533: 'Derived5.P' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived5.P", "Base.P"),
                // (46,22): error CS0533: 'Derived6.M' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived6.M", "Base.M()"),
                // (47,22): error CS0533: 'Derived6.P' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived6.P", "Base.P"),
                // (53,28): error CS0533: 'Derived7.M()' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived7.M()", "Base.M()"),
                // (54,27): error CS0533: 'Derived7.P' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived7.P", "Base.P"),
                // (60,27): error CS0533: 'Derived8.M' hides inherited abstract member 'Base.M()'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "M").WithArguments("Derived8.M", "Base.M()"),
                // (61,20): error CS0533: 'Derived8.P' hides inherited abstract member 'Base.P'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P").WithArguments("Derived8.P", "Base.P"));
        }

        [WorkItem(539585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539585")]
        [Fact]
        public void CS0534ERR_UnimplementedAbstractMethod()
        {
            var text = @"

abstract class A<T>
{
    public abstract void M(T t);
}

abstract class B<T> : A<T>
{
    public abstract void M(string s);
}

class C : B<string> // CS0534
{
    public override void M(string s) { }
    static void Main()
    {
    }
}

public abstract class Base<T>
{
    public abstract void M(T t);
    public abstract void M(int i);
}
public class Derived : Base<int> // CS0534
{ }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 13, Column = 7 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 26, Column = 14 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 26, Column = 14 });
        }

        [Fact]
        public void CS0535ERR_UnimplementedInterfaceMember()
        {
            var text = @"public interface A
{
    void F();
}

public class B : A { }   // CS0535 A::F is not implemented
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 6, Column = 18 });
        }

        [Fact]
        public void CS0537ERR_ObjectCantHaveBases()
        {
            var text = @"namespace System
{
    public class Object : ICloneable
    {
    }
}";
            //compile without corlib, since otherwise this System.Object won't count as a special type
            CreateEmptyCompilation(text).VerifyDiagnostics(
                // (3,20): error CS0246: The type or namespace name 'ICloneable' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ICloneable").WithArguments("ICloneable"),
                // (3,11): error CS0537: The class System.Object cannot have a base class or implement an interface
                Diagnostic(ErrorCode.ERR_ObjectCantHaveBases, "Object"));
        }

        //this should be the same as CS0537ERR_ObjectCantHaveBases, except without the second
        //error (about ICloneable not being defined)
        [Fact]
        public void CS0537ERR_ObjectCantHaveBases_OtherType()
        {
            var text = @"namespace System
{
    public interface ICloneable
    {
    }
    public class Object : ICloneable
    {
    }
}";

            //compile without corlib, since otherwise this System.Object won't count as a special type
            CreateEmptyCompilation(text).VerifyDiagnostics(
                // (6,11): error CS0537: The class System.Object cannot have a base class or implement an interface
                Diagnostic(ErrorCode.ERR_ObjectCantHaveBases, "Object"));
        }

        [WorkItem(538320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538320")]
        [Fact]
        public void CS0537ERR_ObjectCantHaveBases_WithMsCorlib()
        {
            var text = @"namespace System
{
    public interface ICloneable
    {
    }
    public class Object : ICloneable
    {
    }
}";

            // When System.Object is defined in both source and metadata, dev10 favors
            // the source version and reports ERR_ObjectCantHaveBases.
            CreateEmptyCompilation(text).VerifyDiagnostics(
                // (6,11): error CS0537: The class System.Object cannot have a base class or implement an interface
                Diagnostic(ErrorCode.ERR_ObjectCantHaveBases, "Object"));
        }

        [Fact]
        public void CS0538ERR_ExplicitInterfaceImplementationNotInterface()
        {
            var text = @"interface MyIFace
{
}

public class MyClass
{
}

class C : MyIFace
{
    void MyClass.G()   // CS0538, MyClass not an interface
    {
    }
    int MyClass.P { get { return 1; } }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, Line = 11, Column = 10 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, Line = 14, Column = 9 });
        }

        [Fact]
        public void CS0539ERR_InterfaceMemberNotFound()
        {
            var text = @"namespace x
{
   interface I
   {
      void m();
   }

   public class @clx : I
   {
      void I.x()   // CS0539
      {
      }

      void I.m() { }

      public static int Main()
      {
         return 0;
      }
   }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_InterfaceMemberNotFound, Line = 10, Column = 14 });
        }

        [Fact]
        public void CS0540ERR_ClassDoesntImplementInterface()
        {
            var text = @"interface I
{
    void m();
}

public class Clx
{
    void I.m() { }   // CS0540
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ClassDoesntImplementInterface, Line = 8, Column = 10 });
        }

        [Fact]
        public void CS0541ERR_ExplicitInterfaceImplementationInNonClassOrStruct()
        {
            var text = @"namespace x
{
    interface IFace
    {
        void F();
        int P { set; }
    }

    interface IFace2 : IFace
    {
        void IFace.F();   // CS0541
        int IFace.P { set; } //CS0541
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (11,20): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //         void IFace.F();   // CS0541
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "F").WithArguments("default interface implementation", "8.0").WithLocation(11, 20),
                // (12,23): error CS8652: The feature 'default interface implementation' is not available in C# 7. Please use language version 8.0 or greater.
                //         int IFace.P { set; } //CS0541
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "8.0").WithLocation(12, 23),
                // (12,23): error CS0501: 'IFace2.IFace.P.set' must declare a body because it is not marked abstract, extern, or partial
                //         int IFace.P { set; } //CS0541
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("x.IFace2.x.IFace.P.set").WithLocation(12, 23),
                // (11,20): error CS0501: 'IFace2.IFace.F()' must declare a body because it is not marked abstract, extern, or partial
                //         void IFace.F();   // CS0541
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "F").WithArguments("x.IFace2.x.IFace.F()").WithLocation(11, 20)
                );
        }

        [Fact]
        public void CS0542ERR_MemberNameSameAsType01()
        {
            var comp = CreateCompilation(
@"namespace NS
{
    class NS { } // no error
    interface IM { void IM(); } // no error
    interface IP { object IP { get; } } // no error
    enum A { A } // no error
    class B { enum B { } }
    class C { static void C() { } }
    class D { object D { get; set; } }
    class E { int D, E, F; }
    class F { class F { } }
    class G { struct G { } }
    class H { delegate void H(); }
    class L { class L<T> { } }
    class K<T> { class K { } }
    class M<T> { interface M<U> { } }
    class N { struct N<T, U> { } }
    struct O { enum O { } }
    struct P { void P() { } }
    struct Q { static object Q { get; set; } }
    struct R { object Q, R; }
    struct S { class S { } }
    struct T { interface T { } }
    struct U { struct U { } }
    struct V { delegate void V(); }
    struct W { class W<T> { } }
    struct X<T> { class X { } }
    struct Y<T> { interface Y<U> { } }
    struct Z { struct Z<T, U> { } }
}
");
            comp.VerifyDiagnostics(
                // (7,20): error CS0542: 'B': member names cannot be the same as their enclosing type
                //     class B { enum B { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "B").WithArguments("B"),
                // (8,27): error CS0542: 'C': member names cannot be the same as their enclosing type
                //     class C { static void C() { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C"),
                // (9,22): error CS0542: 'D': member names cannot be the same as their enclosing type
                //     class D { object D { get; set; } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "D").WithArguments("D"),
                // (10,22): error CS0542: 'E': member names cannot be the same as their enclosing type
                //     class E { int D, E, F; }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("E"),
                // (11,21): error CS0542: 'F': member names cannot be the same as their enclosing type
                //     class F { class F { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "F").WithArguments("F"),
                // (12,22): error CS0542: 'G': member names cannot be the same as their enclosing type
                //     class G { struct G { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "G").WithArguments("G"),
                // (13,29): error CS0542: 'H': member names cannot be the same as their enclosing type
                //     class H { delegate void H(); }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "H").WithArguments("H"),
                // (14,21): error CS0542: 'L': member names cannot be the same as their enclosing type
                //     class L { class L<T> { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "L").WithArguments("L"),
                // (15,24): error CS0542: 'K': member names cannot be the same as their enclosing type
                //     class K<T> { class K { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "K").WithArguments("K"),
                // (16,28): error CS0542: 'M': member names cannot be the same as their enclosing type
                //     class M<T> { interface M<U> { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "M").WithArguments("M"),
                // (17,22): error CS0542: 'N': member names cannot be the same as their enclosing type
                //     class N { struct N<T, U> { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "N").WithArguments("N"),
                // (18,21): error CS0542: 'O': member names cannot be the same as their enclosing type
                //     struct O { enum O { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "O").WithArguments("O"),
                // (19,21): error CS0542: 'P': member names cannot be the same as their enclosing type
                //     struct P { void P() { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "P").WithArguments("P"),
                // (20,30): error CS0542: 'Q': member names cannot be the same as their enclosing type
                //     struct Q { static object Q { get; set; } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "Q").WithArguments("Q"),
                // (21,26): error CS0542: 'R': member names cannot be the same as their enclosing type
                //     struct R { object Q, R; }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "R").WithArguments("R"),
                // (22,22): error CS0542: 'S': member names cannot be the same as their enclosing type
                //     struct S { class S { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "S").WithArguments("S"),
                // (23,26): error CS0542: 'T': member names cannot be the same as their enclosing type
                //     struct T { interface T { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "T").WithArguments("T"),
                // (24,23): error CS0542: 'U': member names cannot be the same as their enclosing type
                //     struct U { struct U { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "U").WithArguments("U"),
                // (25,30): error CS0542: 'V': member names cannot be the same as their enclosing type
                //     struct V { delegate void V(); }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "V").WithArguments("V"),
                // (26,22): error CS0542: 'W': member names cannot be the same as their enclosing type
                //     struct W { class W<T> { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "W").WithArguments("W"),
                // (27,25): error CS0542: 'X': member names cannot be the same as their enclosing type
                //     struct X<T> { class X { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "X").WithArguments("X"),
                // (28,29): error CS0542: 'Y': member names cannot be the same as their enclosing type
                //     struct Y<T> { interface Y<U> { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "Y").WithArguments("Y"),
                // (29,23): error CS0542: 'Z': member names cannot be the same as their enclosing type
                //     struct Z { struct Z<T, U> { } }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "Z").WithArguments("Z"),
                // (10,19): warning CS0169: The field 'NS.E.D' is never used
                //     class E { int D, E, F; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "D").WithArguments("NS.E.D"),
                // (10,22): warning CS0169: The field 'NS.E.E' is never used
                //     class E { int D, E, F; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "E").WithArguments("NS.E.E"),
                // (10,25): warning CS0169: The field 'NS.E.F' is never used
                //     class E { int D, E, F; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("NS.E.F"),
                // (21,23): warning CS0169: The field 'NS.R.Q' is never used
                //     struct R { object Q, R; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Q").WithArguments("NS.R.Q"),
                // (21,26): warning CS0169: The field 'NS.R.R' is never used
                //     struct R { object Q, R; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "R").WithArguments("NS.R.R"));
        }

        [Fact]
        public void CS0542ERR_MemberNameSameAsType02()
        {
            // No errors for names from explicit implementations.
            var source =
@"interface IM
{
    void C();
}
interface IP
{
    object C { get; }
}
class C : IM, IP
{
    void IM.C() { }
    object IP.C { get { return null; } }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact(), WorkItem(529156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529156")]
        public void CS0542ERR_MemberNameSameAsType03()
        {
            CreateCompilation(
@"class Item
{
    public int this[int i]  // CS0542
    {
        get
        {
            return 0;
        }
    }
}
").VerifyDiagnostics();
        }

        [WorkItem(538633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538633")]
        [Fact]
        public void CS0542ERR_MemberNameSameAsType04()
        {
            var source =
@"class get_P
{
    object P { get; set; } // CS0542
}
class set_P
{
    object P { set { } } // CS0542
}
interface get_Q
{
    object Q { get; }
}
class C : get_Q
{
    public object Q { get; set; }
}
interface IR
{
    object R { get; set; }
}
class get_R : IR
{
    public object R { get; set; } // CS0542
}
class set_R : IR
{
    object IR.R { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,16): error CS0542: 'get_P': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "get").WithArguments("get_P").WithLocation(3, 16),
                // (7,16): error CS0542: 'set_P': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "set").WithArguments("set_P").WithLocation(7, 16),
                // (23,23): error CS0542: 'get_R': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "get").WithArguments("get_R").WithLocation(23, 23));
        }

        [Fact]
        public void CS0542ERR_MemberNameSameAsType05()
        {
            var source =
@"namespace N1
{
    class get_Item
    {
        object this[object o] { get { return null; } set { } } // CS0542
    }
    class set_Item
    {
        object this[object o] { get { return null; } set { } } // CS0542
    }
}
namespace N2
{
    interface I
    {
        object this[object o] { get; set; }
    }
    class get_Item : I
    {
        public object this[object o] { get { return null; } set { } } // CS0542
    }
    class set_Item : I
    {
        object I.this[object o] { get { return null; } set { } }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,33): error CS0542: 'get_Item': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "get").WithArguments("get_Item").WithLocation(5, 33),
                // (9,54): error CS0542: 'set_Item': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "set").WithArguments("set_Item").WithLocation(9, 54),
                // (20,40): error CS0542: 'get_Item': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "get").WithArguments("get_Item").WithLocation(20, 40));
        }

        /// <summary>
        /// Derived class with same name as base class
        /// property accessor metadata name.
        /// </summary>
        [Fact]
        public void CS0542ERR_MemberNameSameAsType06()
        {
            var source1 =
@".class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object B1() { }
  .method public abstract virtual instance void B2(object v) { }
  .property instance object P()
  {
    .get instance object A::B1()
    .set instance void A::B2(object v)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class B0 : A
{
    public override object P { get { return null; } set { } }
}
class B1 : A
{
    public override object P { get { return null; } set { } }
}
class B2 : A
{
    public override object P { get { return null; } set { } }
}
class get_P : A
{
    public override object P { get { return null; } set { } }
}
class set_P : A
{
    public override object P { get { return null; } set { } }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (72): error CS0542: 'B1': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "get").WithArguments("B1").WithLocation(7, 32),
                // (11,53): error CS0542: 'B2': member names cannot be the same as their enclosing type
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "set").WithArguments("B2").WithLocation(11, 53));
        }

        /// <summary>
        /// Derived class with same name as base class
        /// event accessor metadata name.
        /// </summary>
        [WorkItem(530385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530385")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CS0542ERR_MemberNameSameAsType07()
        {
            var source1 =
@".class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void B1(class [mscorlib]System.Action v) { }
  .method public abstract virtual instance void B2(class [mscorlib]System.Action v) { }
  .event [mscorlib]System.Action E
  {
    .addon instance void A::B1(class [mscorlib]System.Action);
    .removeon instance void A::B2(class [mscorlib]System.Action);
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"using System;
class B0 : A
{
    public override event Action E;
}
class B1 : A
{
    public override event Action E;
}
class B2 : A
{
    public override event Action E;
}
class add_E : A
{
    public override event Action E;
}
class remove_E : A
{
    public override event Action E;
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (8,34): error CS0542: 'B1': member names cannot be the same as their enclosing type
                //     public override event Action E;
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("B1").WithLocation(8, 34),
                // (12,34): error CS0542: 'B2': member names cannot be the same as their enclosing type
                //     public override event Action E;
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "E").WithArguments("B2").WithLocation(12, 34),
                // (16,34): warning CS0067: The event 'add_E.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("add_E.E").WithLocation(16, 34),
                // (12,34): warning CS0067: The event 'B2.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("B2.E").WithLocation(12, 34),
                // (8,34): warning CS0067: The event 'B1.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("B1.E").WithLocation(8, 34),
                // (20,34): warning CS0067: The event 'remove_E.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("remove_E.E").WithLocation(20, 34),
                // (4,34): warning CS0067: The event 'B0.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("B0.E").WithLocation(4, 34));
        }

        [Fact]
        public void CS0544ERR_CantOverrideNonProperty()
        {
            var text = @"public class @a
{
    public int i;
}

public class @b : a
{
    public override int i// CS0544
    {   
        get
        {
            return 0;
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CantOverrideNonProperty, Line = 8, Column = 25 });
        }

        [Fact]
        public void CS0545ERR_NoGetToOverride()
        {
            var text = @"public class @a
{
    public virtual int i
    {
        set { }
    }
}

public class @b : a
{
    public override int i
    {
        get { return 0; }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoGetToOverride, Line = 13, Column = 9 });
        }

        [WorkItem(539321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539321")]
        [Fact]
        public void CS0545ERR_NoGetToOverride_Regress()
        {
            var text = @"public class A
{
    public virtual int P1 { private get; set; }
    public virtual int P2 { private get; set; }
}

public class C : A
{
    public override int P1 { set { } } //fine
    public sealed override int P2 { set { } } //CS0546 since we can't see A.P2.set to override it as sealed
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,32): error CS0545: 'C.P2': cannot override because 'A.P2' does not have an overridable get accessor
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "P2").WithArguments("C.P2", "A.P2"));

            var classA = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var classAProp1 = classA.GetMember<PropertySymbol>("P1");

            Assert.True(classAProp1.IsVirtual);
            Assert.True(classAProp1.SetMethod.IsVirtual);
            Assert.False(classAProp1.GetMethod.IsVirtual); //NB: non-virtual since private
        }

        [Fact]
        public void CS0546ERR_NoSetToOverride()
        {
            var text = @"public class @a
{
    public virtual int i
    {
        get
        {
            return 0;
        }
    }
}
public class @b : a
{
    public override int i
    {
        set { }   // CS0546 error no set
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoSetToOverride, Line = 15, Column = 9 });
        }

        [WorkItem(539321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539321")]
        [Fact]
        public void CS0546ERR_NoSetToOverride_Regress()
        {
            var text = @"public class A
{
    public virtual int P1 { get; private set; }
    public virtual int P2 { get; private set; }
}

public class C : A
{
    public override int P1 { get { return 0; } } //fine
    public sealed override int P2 { get { return 0; } } //CS0546 since we can't see A.P2.set to override it as sealed
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,32): error CS0546: 'C.P2': cannot override because 'A.P2' does not have an overridable set accessor
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "P2").WithArguments("C.P2", "A.P2"));

            var classA = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var classAProp1 = classA.GetMember<PropertySymbol>("P1");

            Assert.True(classAProp1.IsVirtual);
            Assert.True(classAProp1.GetMethod.IsVirtual);
            Assert.False(classAProp1.SetMethod.IsVirtual); //NB: non-virtual since private
        }

        [Fact]
        public void CS0547ERR_PropertyCantHaveVoidType()
        {
            var text =
@"interface I
{
    void P { get; set; }
}
class C
{
    internal void P { set { } }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyCantHaveVoidType, Line = 3, Column = 10 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyCantHaveVoidType, Line = 7, Column = 19 });
        }

        [Fact]
        public void CS0548ERR_PropertyWithNoAccessors()
        {
            var text =
@"interface I
{
    object P { }
}
abstract class A
{
    public abstract object P { }
}
class B
{
    internal object P { }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyWithNoAccessors, Line = 3, Column = 12 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyWithNoAccessors, Line = 7, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyWithNoAccessors, Line = 11, Column = 21 });
        }

        [Fact]
        public void CS0548ERR_PropertyWithNoAccessors_Indexer()
        {
            var text =
@"interface I
{
    object this[int x] { }
}
abstract class A
{
    public abstract object this[int x] { }
}
class B
{
    internal object this[int x] { }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyWithNoAccessors, Line = 3, Column = 12 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyWithNoAccessors, Line = 7, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_PropertyWithNoAccessors, Line = 11, Column = 21 });
        }

        [Fact]
        public void CS0549ERR_NewVirtualInSealed01()
        {
            var text = @"namespace NS
{
    public sealed class Goo
    {
        public virtual void M1() { }
        internal virtual void M2<X>(X x) { }
        internal virtual int P1 { get; set; }
    }

    sealed class Bar<T>
    {
        internal virtual T M1(T t) { return t; }
        public virtual object P1 { get { return null; } }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NewVirtualInSealed, Line = 5, Column = 29 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NewVirtualInSealed, Line = 6, Column = 31 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NewVirtualInSealed, Line = 7, Column = 35 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NewVirtualInSealed, Line = 7, Column = 40 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NewVirtualInSealed, Line = 12, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NewVirtualInSealed, Line = 13, Column = 36 });
        }

        [Fact]
        public void CS0549ERR_NewVirtualInSealed02()
        {
            var text = @"
public sealed class C
{
    public virtual event System.Action E;
    public virtual event System.Action F { add { } remove { } }
    public virtual int this[int x] { get { return 0; } set { } }
}
";
            // CONSIDER: it seems a little strange to report it on property accessors but on
            // events themselves.  On the other hand, property accessors can have modifiers,
            // whereas event accessors cannot.
            CreateCompilation(text).VerifyDiagnostics(
                // (4,40): error CS0549: 'C.E' is a new virtual member in sealed type 'C'
                //     public virtual event System.Action E;
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, "E").WithArguments("C.E", "C"),
                // (5,40): error CS0549: 'C.F' is a new virtual member in sealed type 'C'
                //     public virtual event System.Action F { add { } remove { } }
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, "F").WithArguments("C.F", "C"),
                // (6,38): error CS0549: 'C.this[int].get' is a new virtual member in sealed type 'C'
                //     public virtual int this[int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, "get").WithArguments("C.this[int].get", "C"),
                // (6,56): error CS0549: 'C.this[int].set' is a new virtual member in sealed type 'C'
                //     public virtual int this[int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, "set").WithArguments("C.this[int].set", "C"),

                // (4,40): warning CS0067: The event 'C.E' is never used
                //     public virtual event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void CS0550ERR_ExplicitPropertyAddingAccessor()
        {
            var text = @"namespace x
{
    interface @ii
    {
        int i
        {
            get;
        }
    }

    public class @a : ii
    {
        int ii.i
        {
            get
            {
                return 0;
            }
            set { }   // CS0550  no set in interface
        }

        public static void Main() { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyAddingAccessor, Line = 19, Column = 13 });
        }

        [Fact]
        public void CS0551ERR_ExplicitPropertyMissingAccessor()
        {
            var text = @"interface @ii
{
    int i
    {
        get;
        set;
    }
}

public class @a : ii
{
    int ii.i { set { } }   // CS0551
    public static void Main()
    { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedInterfaceMember, Line = 10, Column = 19 }, //CONSIDER: dev10 suppresses this
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitPropertyMissingAccessor, Line = 12, Column = 12 });
        }

        [Fact]
        public void CS0552ERR_ConversionWithInterface()
        {
            var text = @"
public interface I
{
}
public class C
{
    public static implicit operator I(C c) // CS0552
    {
        return null;
    }
    public static implicit operator C(I i) // CS0552
    {
        return null;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (77): error CS0552: 'C.implicit operator I(C)': user-defined conversions to or from an interface are not allowed
                //     public static implicit operator I(C c) // CS0552
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "I").WithArguments("C.implicit operator I(C)"),
                // (11,37): error CS0552: 'C.implicit operator C(I)': user-defined conversions to or from an interface are not allowed
                //     public static implicit operator C(I i) // CS0552
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "C").WithArguments("C.implicit operator C(I)"));
        }

        [Fact]
        public void CS0553ERR_ConversionWithBase()
        {
            var text = @"
public class B { }
public class D : B
{
    public static implicit operator B(D d) // CS0553
    {
        return null;
    }
}
public struct C 
{
    public static implicit operator C?(object c) // CS0553
    {
        return null;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,37): error CS0553: 'D.implicit operator B(D)': user-defined conversions to or from a base type are not allowed
                //     public static implicit operator B(D d) // CS0553
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "B").WithArguments("D.implicit operator B(D)"),
                // (12,37): error CS0553: 'C.implicit operator C?(object)': user-defined conversions to or from a base type are not allowed
                //     public static implicit operator C?(object c) // CS0553
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "C?").WithArguments("C.implicit operator C?(object)"));
        }

        [Fact]
        public void CS0554ERR_ConversionWithDerived()
        {
            var text = @"
public class B
{
    public static implicit operator B(D d) // CS0554
    {
        return null;
    }
}
public class D : B {}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (4,37): error CS0554: 'B.implicit operator B(D)': user-defined conversions to or from a derived type are not allowed
//     public static implicit operator B(D d) // CS0554
Diagnostic(ErrorCode.ERR_ConversionWithDerived, "B").WithArguments("B.implicit operator B(D)")
                );
        }

        [Fact]
        public void CS0555ERR_IdentityConversion()
        {
            var text = @"
public class MyClass
{
    public static implicit operator MyClass(MyClass aa)   // CS0555
    {
        return new MyClass();
    }
}
public struct S
{
    public static implicit operator S?(S s) { return s; }
}

";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (4,37): error CS0555: User-defined operator cannot convert a type to itself
//     public static implicit operator MyClass(MyClass aa)   // CS0555
Diagnostic(ErrorCode.ERR_IdentityConversion, "MyClass"),

// (11,37): error CS0555: User-defined operator cannot convert a type to itself
//     public static implicit operator S?(S s) { return s; }
Diagnostic(ErrorCode.ERR_IdentityConversion, "S?")
                );
        }

        [Fact]
        public void CS0556ERR_ConversionNotInvolvingContainedType()
        {
            var text = @"
public class C
{
    public static implicit operator int(string aa)   // CS0556
    {
        return 0;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,37): error CS0556: User-defined conversion must convert to or from the enclosing type
                //     public static implicit operator int(string aa)   // CS0556
                Diagnostic(ErrorCode.ERR_ConversionNotInvolvingContainedType, "int")
                );
        }

        [Fact]
        public void CS0557ERR_DuplicateConversionInClass()
        {
            var text = @"namespace x
{
    public class @ii
    {
        public class @iii
        {
            public static implicit operator int(iii aa)
            {
                return 0;
            }

            public static explicit operator int(iii aa)
            {
                return 0;
            }
        }
    }
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (12,45): error CS0557: Duplicate user-defined conversion in type 'x.ii.iii'
//             public static explicit operator int(iii aa)
Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "int").WithArguments("x.ii.iii")
                );
        }

        [Fact]
        public void CS0558ERR_OperatorsMustBeStatic()
        {
            var text = @"namespace x
{
   public class @ii
   {
      public class @iii
      {
         static implicit operator int(iii aa)   // CS0558, add public
         {
            return 0;
         }
      }
   }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (75): error CS0558: User-defined operator 'x.ii.iii.implicit operator int(x.ii.iii)' must be declared static and public
//          static implicit operator int(iii aa)   // CS0558, add public
Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "int").WithArguments("x.ii.iii.implicit operator int(x.ii.iii)")
                );
        }

        [Fact]
        public void CS0559ERR_BadIncDecSignature()
        {
            var text = @"public class @iii
{
    public static iii operator ++(int aa)   // CS0559
    {
        return null;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (3,32): error CS0559: The parameter type for ++ or -- operator must be the containing type
//     public static iii operator ++(int aa)   // CS0559
Diagnostic(ErrorCode.ERR_BadIncDecSignature, "++"));
        }

        [Fact]
        public void CS0562ERR_BadUnaryOperatorSignature()
        {
            var text = @"public class @iii
{
    public static iii operator +(int aa)   // CS0562
    {
        return null;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (3,32): error CS0562: The parameter of a unary operator must be the containing type
//     public static iii operator +(int aa)   // CS0562
Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, "+")
                );
        }

        [Fact]
        public void CS0563ERR_BadBinaryOperatorSignature()
        {
            var text = @"public class @iii
{
    public static int operator +(int aa, int bb)   // CS0563 
    {
        return 0;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,32): error CS0563: One of the parameters of a binary operator must be the containing type
                //     public static int operator +(int aa, int bb)   // CS0563 
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, "+")
                );
        }

        [Fact]
        public void CS0564ERR_BadShiftOperatorSignature()
        {
            var text = @"
class C
{
    public static int operator <<(C c1, C c2) // CS0564
    {
        return 0;
    }
    public static int operator >>(int c1, int c2) // CS0564
    {
        return 0;
    }
    public static int operator >>>(int c1, int c2) // CS0564
    {
        return 0;
    }
    static void Main()
    {
    }
}

class C1
{
    public static int operator <<(C1 c1, int c2)
    {
        return 0;
    }
}

class C2
{
    public static int operator <<(C2 c1, int? c2)
    {
        return 0;
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,32): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static int operator >>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, ">>").WithLocation(8, 32),
                // (12,32): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type, and the type of the second operand must be int
                //     public static int operator >>>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, ">>>").WithLocation(12, 32)
                );

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (4,32): error CS8936: Feature 'relaxed shift operator' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static int operator <<(C c1, C c2) // CS0564
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "<<").WithArguments("relaxed shift operator", "11.0").WithLocation(4, 32),
                // (8,32): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static int operator >>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, ">>").WithLocation(8, 32),
                // (12,32): error CS8936: Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static int operator >>>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, ">>>").WithArguments("unsigned right shift", "11.0").WithLocation(12, 32),
                // (12,32): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static int operator >>>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, ">>>").WithLocation(12, 32)
                );

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (8,32): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static int operator >>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, ">>").WithLocation(8, 32),
                // (12,32): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type, and the type of the second operand must be int
                //     public static int operator >>>(int c1, int c2) // CS0564
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, ">>>").WithLocation(12, 32)
                );
        }

        [Fact]
        public void CS0567ERR_InterfacesCantContainOperators()
        {
            var text = @"
interface IA
{
   int operator +(int aa, int bb);   // CS0567
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (4,17): error CS0558: User-defined operator 'IA.operator +(int, int)' must be declared static and public
                //    int operator +(int aa, int bb);   // CS0567
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "+").WithArguments("IA.operator +(int, int)").WithLocation(4, 17),
                // (4,17): error CS0501: 'IA.operator +(int, int)' must declare a body because it is not marked abstract, extern, or partial
                //    int operator +(int aa, int bb);   // CS0567
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "+").WithArguments("IA.operator +(int, int)").WithLocation(4, 17),
                // (4,17): error CS0563: One of the parameters of a binary operator must be the containing type
                //    int operator +(int aa, int bb);   // CS0567
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, "+").WithLocation(4, 17)
                );
        }

        [Fact]
        public void CS0569ERR_CantOverrideBogusMethod()
        {
            var source1 =
@".class abstract public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object get_sealed() { }
  .method public abstract virtual instance void set_sealed(object o) { }
}
.class abstract public B extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object get_abstract() { }
  .method public abstract virtual instance void set_abstract(object o) { }
  .method public virtual final instance void set_sealed(object o) { ret }
  .method public virtual final instance object get_sealed() { ldnull ret }
  // abstract get, sealed set
  .property instance object P()
  {
    .get instance object B::get_abstract()
    .set instance void B::set_sealed(object)
  }
  // sealed get, abstract set
  .property instance object Q()
  {
    .get instance object B::get_sealed()
    .set instance void B::set_abstract(object)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class C : B
{
    public override object P { get { return 0; } }
    public override object Q { set { } }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (3,28): error CS0569: 'C.P': cannot override 'B.P' because it is not supported by the language
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "P").WithArguments("C.P", "B.P").WithLocation(3, 28),
                // (4,28): error CS0569: 'C.Q': cannot override 'B.Q' because it is not supported by the language
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "Q").WithArguments("C.Q", "B.Q").WithLocation(4, 28));
        }

        [Fact]
        public void CS8036ERR_FieldInitializerInStruct()
        {
            var text = @"namespace x
{
    public class @clx
    {
        public static void Main()
        {
        }
    }

    public struct @cly
    {
        clx a = new clx();   // CS8036
        int i = 7;           // CS8036
        const int c = 1;     // no error
        static int s = 2;    // no error
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,19): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     public struct @cly
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "@cly").WithLocation(10, 19),
                // (12,13): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         clx a = new clx();   // CS8036
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "a").WithArguments("struct field initializers", "10.0").WithLocation(12, 13),
                // (12,13): warning CS0169: The field 'cly.a' is never used
                //         clx a = new clx();   // CS8036
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("x.cly.a").WithLocation(12, 13),
                // (13,13): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         int i = 7;           // CS8036
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "i").WithArguments("struct field initializers", "10.0").WithLocation(13, 13),
                // (13,13): warning CS0169: The field 'cly.i' is never used
                //         int i = 7;           // CS8036
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("x.cly.i").WithLocation(13, 13),
                // (15,20): warning CS0414: The field 'cly.s' is assigned but its value is never used
                //         static int s = 2;    // no error
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s").WithArguments("x.cly.s").WithLocation(15, 20));

            comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,19): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     public struct @cly
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "@cly").WithLocation(10, 19),
                // (12,13): warning CS0169: The field 'cly.a' is never used
                //         clx a = new clx();   // CS8036
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("x.cly.a").WithLocation(12, 13),
                // (13,13): warning CS0169: The field 'cly.i' is never used
                //         int i = 7;           // CS8036
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("x.cly.i").WithLocation(13, 13),
                // (15,20): warning CS0414: The field 'cly.s' is assigned but its value is never used
                //         static int s = 2;    // no error
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s").WithArguments("x.cly.s").WithLocation(15, 20));
        }

        [Fact]
        public void CS0568ERR_StructsCantContainDefaultConstructor01()
        {
            var text = @"namespace x
{
    public struct S1
    {
        public S1() {}

        struct S2<T>
        {
            S2() { }
        }
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5));
            comp.VerifyDiagnostics(
                // (5,16): error CS8026: Feature 'parameterless struct constructors' is not available in C# 5. Please use language version 10.0 or greater.
                //         public S1() {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "S1").WithArguments("parameterless struct constructors", "10.0").WithLocation(5, 16),
                // (9,13): error CS8026: Feature 'parameterless struct constructors' is not available in C# 5. Please use language version 10.0 or greater.
                //             S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(9, 13),
                // (9,13): error CS8938: The parameterless struct constructor must be 'public'.
                //             S2() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S2").WithLocation(9, 13));

            comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (9,13): error CS8918: The parameterless struct constructor must be 'public'.
                //             S2() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S2").WithLocation(9, 13));
        }

        [Fact]
        public void CS0575ERR_OnlyClassesCanContainDestructors()
        {
            var text = @"namespace x
{
    public struct @iii
    {
        ~iii()   // CS0575
        {
        }

        public static void Main()
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_OnlyClassesCanContainDestructors, Line = 5, Column = 10 });
        }

        [Fact]
        public void CS0576ERR_ConflictAliasAndMember01()
        {
            var text = @"namespace NS
{
    class B { }
}

namespace NS
{
    using System;
    using B = NS.B;

    class A
    {
        public static void Main(String[] args)
        {
            B b = null; if (b == b) {}
        }
    }

    struct S
    {
        B field;
        public void M(ref B p) { }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (22,27): error CS0576: Namespace 'NS' contains a definition conflicting with alias 'B'
                //         public void M(ref B p) { }
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "B").WithArguments("B", "NS"),
                // (21,9): error CS0576: Namespace 'NS' contains a definition conflicting with alias 'B'
                //         B field;
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "B").WithArguments("B", "NS"),
                // (15,13): error CS0576: Namespace 'NS' contains a definition conflicting with alias 'B'
                //             B b = null; if (b == b) {}
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "B").WithArguments("B", "NS"),
                // (21,11): warning CS0169: The field 'NS.S.field' is never used
                //         B field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("NS.S.field")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [WorkItem(545463, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545463")]
        [Fact]
        public void CS0576ERR_ConflictAliasAndMember02()
        {
            var source = @"
namespace Globals.Errors.ResolveInheritance
{
    using ConflictingAlias = BadUsingNamespace;
    public class ConflictingAlias { public class Nested { } }

    namespace BadUsingNamespace
    {
        public class UsingNotANamespace { }
    }

    class Cls1 : ConflictingAlias.UsingNotANamespace { } // Error
    class Cls2 : ConflictingAlias::UsingNotANamespace { } // OK
    class Cls3 : global::Globals.Errors.ResolveInheritance.ConflictingAlias.Nested { } // OK
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,18): error CS0576: Namespace 'Globals.Errors.ResolveInheritance' contains a definition conflicting with alias 'ConflictingAlias'
                //     class Cls1 : ConflictingAlias.UsingNotANamespace { } // Error
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "ConflictingAlias").WithArguments("ConflictingAlias", "Globals.Errors.ResolveInheritance"));
        }

        [Fact]
        public void CS0577ERR_ConditionalOnSpecialMethod_01()
        {
            var sourceA =
@"#pragma warning disable 436
using System.Diagnostics;
class Program
{
    [Conditional("""")] Program() { }
}";
            var sourceB =
@"namespace System.Diagnostics
{
    public class ConditionalAttribute : Attribute
    {
        public ConditionalAttribute(string s) { }
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (5,6): error CS0577: The Conditional attribute is not valid on 'Program.Program()' because it is a constructor, destructor, operator, lambda expression, or explicit interface implementation
                //     [Conditional("")] Program() { }
                Diagnostic(ErrorCode.ERR_ConditionalOnSpecialMethod, @"Conditional("""")").WithArguments("Program.Program()").WithLocation(5, 6));
        }

        [Fact]
        public void CS0577ERR_ConditionalOnSpecialMethod_02()
        {
            var source =
@"using System.Diagnostics;
class Program
{
    [Conditional("""")] ~Program() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,6): error CS0577: The Conditional attribute is not valid on 'Program.~Program()' because it is a constructor, destructor, operator, lambda expression, or explicit interface implementation
                //     [Conditional("")] ~Program() { }
                Diagnostic(ErrorCode.ERR_ConditionalOnSpecialMethod, @"Conditional("""")").WithArguments("Program.~Program()").WithLocation(4, 6));
        }

        [Fact]
        public void CS0577ERR_ConditionalOnSpecialMethod_03()
        {
            var source =
@"using System.Diagnostics;
class C
{
    [Conditional("""")] public static C operator !(C c) => c;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,6): error CS0577: The Conditional attribute is not valid on 'C.operator !(C)' because it is a constructor, destructor, operator, lambda expression, or explicit interface implementation
                //     [Conditional("")] public static C operator !(C c) => c;
                Diagnostic(ErrorCode.ERR_ConditionalOnSpecialMethod, @"Conditional("""")").WithArguments("C.operator !(C)").WithLocation(4, 6));
        }

        [Fact]
        public void CS0577ERR_ConditionalOnSpecialMethod_04()
        {
            var text = @"interface I
{
    void m();
}

public class MyClass : I
{
    [System.Diagnostics.Conditional(""a"")]   // CS0577
    void I.m() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,6): error CS0577: The Conditional attribute is not valid on 'MyClass.I.m()' because it is a constructor, destructor, operator, lambda expression, or explicit interface implementation
                //     [System.Diagnostics.Conditional("a")]   // CS0577
                Diagnostic(ErrorCode.ERR_ConditionalOnSpecialMethod, @"System.Diagnostics.Conditional(""a"")").WithArguments("MyClass.I.m()").WithLocation(8, 6));
        }

        [Fact]
        public void CS0578ERR_ConditionalMustReturnVoid()
        {
            var text = @"public class MyClass
{
   [System.Diagnostics.ConditionalAttribute(""a"")]   // CS0578
   public int TestMethod()
   {
      return 0;
   }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,5): error CS0578: The Conditional attribute is not valid on 'MyClass.TestMethod()' because its return type is not void
                //    [System.Diagnostics.ConditionalAttribute("a")]   // CS0578
                Diagnostic(ErrorCode.ERR_ConditionalMustReturnVoid, @"System.Diagnostics.ConditionalAttribute(""a"")").WithArguments("MyClass.TestMethod()").WithLocation(3, 5));
        }

        [Fact]
        public void CS0579ERR_DuplicateAttribute()
        {
            var text =
@"class A : System.Attribute { }
class B : System.Attribute { }
[A, A] class C { }
[B][A][B] class D { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateAttribute, Line = 3, Column = 5 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateAttribute, Line = 4, Column = 8 });
        }

        [Fact, WorkItem(528872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528872")]
        public void CS0582ERR_ConditionalOnInterfaceMethod()
        {
            var text = @"using System.Diagnostics;
interface MyIFace
{
   [ConditionalAttribute(""DEBUG"")]   // CS0582
   void zz();
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,5): error CS0582: The Conditional attribute is not valid on interface members
                //    [ConditionalAttribute("DEBUG")]   // CS0582
                Diagnostic(ErrorCode.ERR_ConditionalOnInterfaceMethod, @"ConditionalAttribute(""DEBUG"")").WithLocation(4, 5));
        }

        [Fact]
        public void CS0590ERR_OperatorCantReturnVoid()
        {
            var text = @"
public class C
{
    public static void operator +(C c1, C c2) { }
    public static implicit operator void(C c1) { }
    public static void operator +(C c) { }
    public static void operator >>(C c, int x) { }
    public static void operator >>>(C c, int x) { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,33): error CS0590: User-defined operators cannot return void
                //     public static void operator +(C c1, C c2) { }
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "+"),
                // (5,33): error CS0590: User-defined operators cannot return void
                //     public static implicit operator void(C c1) { }
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "void"),
                // (5,46): error CS1547: Keyword 'void' cannot be used in this context
                //     public static implicit operator void(C c1) { }
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void"),
                // (6,33): error CS0590: User-defined operators cannot return void
                //     public static void operator +(C c) { }
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "+"),
                // (73): error CS0590: User-defined operators cannot return void
                //     public static void operator >>(C c, int x) { }
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, ">>"),
                // (74): error CS0590: User-defined operators cannot return void
                //     public static void operator >>>(C c, int x) { }
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, ">>>")
                );
        }

        [Fact]
        public void CS0591ERR_InvalidAttributeArgument()
        {
            var text = @"using System;
[AttributeUsage(0)]   // CS0591
class A : Attribute { }
[AttributeUsageAttribute(0)]   // CS0591
class B : Attribute { }";

            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (2,17): error CS0591: Invalid value for argument to 'AttributeUsage' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "0").WithArguments("AttributeUsage").WithLocation(2, 17),
                // (4,26): error CS0591: Invalid value for argument to 'AttributeUsageAttribute' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "0").WithArguments("AttributeUsageAttribute").WithLocation(4, 26));
        }

        [Fact]
        public void CS0592ERR_AttributeOnBadSymbolType()
        {
            var text = @"using System;

[AttributeUsage(AttributeTargets.Interface)]
public class MyAttribute : Attribute
{
}

[MyAttribute]
// Generates CS0592 because MyAttribute is not valid for a class. 
public class A
{
    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AttributeOnBadSymbolType, Line = 8, Column = 2 });
        }

        [Fact()]
        public void CS0596ERR_ComImportWithoutUuidAttribute()
        {
            var text = @"using System.Runtime.InteropServices;

namespace x
{
    [ComImport]   // CS0596
    public class @a
    {
    }

    public class @b
    {
        public static void Main()
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ComImportWithoutUuidAttribute, Line = 5, Column = 6 });
        }

        // CS0599: not used

        [Fact]
        public void CS0601ERR_DllImportOnInvalidMethod()
        {
            var text = @"
using System.Runtime.InteropServices;

public class C
{
    [DllImport(""KERNEL32.DLL"")]
    extern int Goo();   // CS0601

    [DllImport(""KERNEL32.DLL"")]
    static void Bar() { }   // CS0601
}
";
            CreateCompilation(text, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,6): error CS0601: The DllImport attribute must be specified on a method marked 'static' and 'extern'
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport"),
                // (9,6): error CS0601: The DllImport attribute must be specified on a method marked 'static' and 'extern'
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport"));
        }

        /// <summary>
        /// Dev10 doesn't report this error, but emits invalid metadata.
        /// When the containing type is being loaded TypeLoadException is thrown by the CLR.
        /// </summary>
        [Fact]
        public void CS7042ERR_DllImportOnGenericMethod()
        {
            var text = @"
using System.Runtime.InteropServices;

public class C<T>
{
    class X 
    {
        [DllImport(""KERNEL32.DLL"")]
        static extern void Bar();
    }
}

public class C
{
    [DllImport(""KERNEL32.DLL"")]
    static extern void Bar<T>();
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,10): error CS7042:  cannot be applied to a method that is generic or contained in a generic type.
                Diagnostic(ErrorCode.ERR_DllImportOnGenericMethod, "DllImport"),
                // (15,6): error CS7042:  cannot be applied to a method that is generic or contained in a generic type.
                Diagnostic(ErrorCode.ERR_DllImportOnGenericMethod, "DllImport"));
        }

        // CS0609ERR_NameAttributeOnOverride -> BreakChange

        [Fact]
        public void CS0610ERR_FieldCantBeRefAny()
        {
            var text = @"public class MainClass
{
    System.TypedReference i;   // CS0610
    public static void Main()
    {
    }

    System.TypedReference Prop { get; set; }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,5): error CS0610: Field or property cannot be of type 'System.TypedReference'
                //     System.TypedReference Prop { get; set; }
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference"),
                // (3,5): error CS0610: Field or property cannot be of type 'System.TypedReference'
                //     System.TypedReference i;   // CS0610
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference"),
                // (3,27): warning CS0169: The field 'MainClass.i' is never used
                //     System.TypedReference i;   // CS0610
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("MainClass.i")
                );
        }

        [Fact]
        public void CS0616ERR_NotAnAttributeClass()
        {
            var text = @"[CMyClass]   // CS0616
public class CMyClass {}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NotAnAttributeClass, Line = 1, Column = 2 });
        }

        [Fact]
        public void CS0616ERR_NotAnAttributeClass2()
        {
            var text = @"[CMyClass]   // CS0616
public class CMyClassAttribute {}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NotAnAttributeClass, Line = 1, Column = 2 });
        }

        [Fact]
        public void CS0617ERR_BadNamedAttributeArgument()
        {
            var text = @"using System;

[AttributeUsage(AttributeTargets.Class)]
public class MyClass : Attribute
{
    public MyClass(int sName)
    {
        Bad = sName;
        Bad2 = -1;
    }

    public readonly int Bad;
    public int Bad2;
}

[MyClass(5, Bad = 0)]
class Class1 { }   // CS0617
[MyClass(5, Bad2 = 0)]
class Class2 { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadNamedAttributeArgument, Line = 16, Column = 13 });
        }

        [Fact]
        public void CS0620ERR_IndexerCantHaveVoidType()
        {
            var text = @"class MyClass
{
    public static void Main()
    {
        MyClass test = new MyClass();
    }

    void this[int intI]   // CS0620, return type cannot be void
    {
        get
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_IndexerCantHaveVoidType, Line = 8, Column = 10 });
        }

        [Fact]
        public void CS0621ERR_VirtualPrivate01()
        {
            var text = @"namespace x
{
    class Goo
    {
        private virtual void vf() { }
    }
    public class Bar<T>
    {
        private virtual void M1(T t) { }
        virtual V M2<V>(T t);
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 5, Column = 30 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 9, Column = 30 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 10, Column = 19 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("x").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0621ERR_VirtualPrivate02()
        {
            var source =
@"abstract class A
{
    abstract object P { get; }
}
class B
{
    private virtual object Q { get; set; }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,21): error CS0621: 'A.P': virtual or abstract members cannot be private
                //     abstract object P { get; }
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P").WithArguments("A.P").WithLocation(3, 21),
                // (7,28): error CS0621: 'B.Q': virtual or abstract members cannot be private
                //     private virtual object Q { get; set; }
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "Q").WithArguments("B.Q").WithLocation(7, 28));
        }

        [Fact]
        public void CS0621ERR_VirtualPrivate03()
        {
            var text = @"namespace x
{
    abstract class Goo
    {
        private virtual void M1<T>(T x) { }
        private abstract int P { get; set; }
    }
    class Bar
    {
        private override void M1<T>(T a) { }
        private override int P { set { } }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 5, Column = 30 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 6, Column = 30 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 10, Column = 31 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_VirtualPrivate, Line = 11, Column = 30 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 10, Column = 31 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_OverrideNotExpected, Line = 11, Column = 30 });
        }

        [Fact]
        public void CS0621ERR_VirtualPrivate04()
        {
            var text = @"
class C
{
    virtual private event System.Action E;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,41): error CS0621: 'C.E': virtual or abstract members cannot be private
                //     virtual private event System.Action E;
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "E").WithArguments("C.E"),
                // (4,41): warning CS0067: The event 'C.E' is never used
                //     virtual private event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        // CS0625: See AttributeTests_StructLayout.ExplicitFieldLayout_Errors

        [Fact]
        public void CS0629ERR_InterfaceImplementedByConditional01()
        {
            var text = @"interface MyInterface
{
    void MyMethod();
}

public class MyClass : MyInterface
{
    [System.Diagnostics.Conditional(""debug"")]
    public void MyMethod()    // CS0629, remove the Conditional attribute
    {
    }

    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_InterfaceImplementedByConditional, Line = 9, Column = 17 });
        }

        [Fact]
        public void CS0629ERR_InterfaceImplementedByConditional02()
        {
            var source = @"
using System.Diagnostics;

interface I<T>
{
	void M(T x);
}
class Base
{
    [Conditional(""debug"")]
    public void M(int x) {}
}
class Derived : Base, I<int>
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,23): error CS0629: Conditional member 'Base.M(int)' cannot implement interface member 'I<int>.M(int)' in type 'Derived'
                // class Derived : Base, I<int>
                Diagnostic(ErrorCode.ERR_InterfaceImplementedByConditional, "I<int>").WithArguments("Base.M(int)", "I<int>.M(int)", "Derived").WithLocation(13, 23));
        }

        [Fact]
        public void CS0633ERR_BadArgumentToAttribute()
        {
            var text = @"#define DEBUG
using System.Diagnostics;
public class Test
{
    [Conditional(""DEB+UG"")]   // CS0633
    public static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,18): error CS0633: The argument to the 'Conditional' attribute must be a valid identifier
                //     [Conditional("DEB+UG")]   // CS0633
                Diagnostic(ErrorCode.ERR_BadArgumentToAttribute, @"""DEB+UG""").WithArguments("Conditional").WithLocation(5, 18));
        }

        [Fact]
        public void CS0633ERR_BadArgumentToAttribute_IndexerNameAttribute()
        {
            var text = @"
using System.Runtime.CompilerServices;
class A
{
    [IndexerName(null)]
    int this[int x] { get { return 0; } set { } }
}
class B
{
    [IndexerName("""")]
    int this[int x] { get { return 0; } set { } }
}
class C
{
    [IndexerName("" "")]
    int this[int x] { get { return 0; } set { } }
}
class D
{
    [IndexerName(""1"")]
    int this[int x] { get { return 0; } set { } }
}
class E
{
    [IndexerName(""!"")]
    int this[int x] { get { return 0; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,18): error CS0633: The argument to the 'IndexerName' attribute must be a valid identifier
                Diagnostic(ErrorCode.ERR_BadArgumentToAttribute, "null").WithArguments("IndexerName"),
                // (10,18): error CS0633: The argument to the 'IndexerName' attribute must be a valid identifier
                Diagnostic(ErrorCode.ERR_BadArgumentToAttribute, @"""""").WithArguments("IndexerName"),
                // (15,18): error CS0633: The argument to the 'IndexerName' attribute must be a valid identifier
                Diagnostic(ErrorCode.ERR_BadArgumentToAttribute, @""" """).WithArguments("IndexerName"),
                // (20,18): error CS0633: The argument to the 'IndexerName' attribute must be a valid identifier
                Diagnostic(ErrorCode.ERR_BadArgumentToAttribute, @"""1""").WithArguments("IndexerName"),
                // (25,18): error CS0633: The argument to the 'IndexerName' attribute must be a valid identifier
                Diagnostic(ErrorCode.ERR_BadArgumentToAttribute, @"""!""").WithArguments("IndexerName"));
        }

        // CS0636: See AttributeTests_StructLayout.ExplicitFieldLayout_Errors
        // CS0637: See AttributeTests_StructLayout.ExplicitFieldLayout_Errors

        [Fact]
        public void CS0641ERR_AttributeUsageOnNonAttributeClass()
        {
            var text =
@"using System;
[AttributeUsage(AttributeTargets.Method)]
class A { }
[System.AttributeUsageAttribute(AttributeTargets.Class)]
class B { }";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (2,2): error CS0641: Attribute 'AttributeUsage' is only valid on classes derived from System.Attribute
                Diagnostic(ErrorCode.ERR_AttributeUsageOnNonAttributeClass, "AttributeUsage").WithArguments("AttributeUsage").WithLocation(2, 2),
                // (4,2): error CS0641: Attribute 'System.AttributeUsageAttribute' is only valid on classes derived from System.Attribute
                Diagnostic(ErrorCode.ERR_AttributeUsageOnNonAttributeClass, "System.AttributeUsageAttribute").WithArguments("System.AttributeUsageAttribute").WithLocation(4, 2));
        }

        [Fact]
        public void CS0643ERR_DuplicateNamedAttributeArgument()
        {
            var text = @"using System;

[AttributeUsage(AttributeTargets.Class)]
public class MyAttribute : Attribute
{
    public MyAttribute()
    {
    }

    public int x;
}

[MyAttribute(x = 5, x = 6)]   // CS0643, error setting x twice
class MyClass
{
}

public class MainClass
{
    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNamedAttributeArgument, Line = 13, Column = 21 });
        }

        [WorkItem(540923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540923")]
        [Fact]
        public void CS0643ERR_DuplicateNamedAttributeArgument02()
        {
            var text = @"using System;
[AttributeUsage(AllowMultiple = true, AllowMultiple = false)]
class MyAtt : Attribute
{ }

[MyAtt]
public class Test
{
    public static void Main()
    {
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (2,39): error CS0643: 'AllowMultiple' duplicate named attribute argument
                // [AttributeUsage(AllowMultiple = true, AllowMultiple = false)]
                Diagnostic(ErrorCode.ERR_DuplicateNamedAttributeArgument, "AllowMultiple = false").WithArguments("AllowMultiple").WithLocation(2, 39),
                // (2,2): error CS76: There is no argument given that corresponds to the required parameter 'validOn' of 'AttributeUsageAttribute.AttributeUsageAttribute(AttributeTargets)'
                // [AttributeUsage(AllowMultiple = true, AllowMultiple = false)]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AttributeUsage(AllowMultiple = true, AllowMultiple = false)").WithArguments("validOn", "System.AttributeUsageAttribute.AttributeUsageAttribute(System.AttributeTargets)").WithLocation(2, 2)
                );
        }

        [Fact]
        public void CS0644ERR_DeriveFromEnumOrValueType()
        {
            var source =
@"using System;
namespace N
{
    class C : Enum { }
    class D : ValueType { }
    class E : Delegate { }
    static class F : MulticastDelegate { }
    static class G : Array { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,15): error CS0644: 'D' cannot derive from special class 'ValueType'
                //     class D : ValueType { }
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "ValueType").WithArguments("N.D", "System.ValueType").WithLocation(5, 15),
                // (6,15): error CS0644: 'E' cannot derive from special class 'Delegate'
                //     class E : Delegate { }
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "Delegate").WithArguments("N.E", "System.Delegate").WithLocation(6, 15),
                // (4,15): error CS0644: 'C' cannot derive from special class 'Enum'
                //     class C : Enum { }
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "Enum").WithArguments("N.C", "System.Enum").WithLocation(4, 15),
                // (8,22): error CS0644: 'G' cannot derive from special class 'Array'
                //     static class G : Array { }
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "Array").WithArguments("N.G", "System.Array").WithLocation(8, 22),
                // (7,22): error CS0644: 'F' cannot derive from special class 'MulticastDelegate'
                //     static class F : MulticastDelegate { }
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "MulticastDelegate").WithArguments("N.F", "System.MulticastDelegate").WithLocation(7, 22));
        }

        [Fact]
        public void CS0646ERR_DefaultMemberOnIndexedType()
        {
            var text = @"[System.Reflection.DefaultMemberAttribute(""x"")]   // CS0646
class MyClass
{
    public int this[int index]   // an indexer
    {
        get
        {
            return 0;
        }
    }

    public int x = 0;
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DefaultMemberOnIndexedType, Line = 1, Column = 2 });
        }

        [Fact]
        public void CS0646ERR_DefaultMemberOnIndexedType02()
        {
            var text = @"
using System.Reflection;

interface I
{
    int this[int x] { set;  }
}

[DefaultMember(""X"")]
class Program : I
{
    int I.this[int x] { set { } } //doesn't count as an indexer for CS0646
}";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS0646ERR_DefaultMemberOnIndexedType03()
        {
            var text = @"
using System.Reflection;

[DefaultMember(""This is definitely not a valid member name *#&#*"")]
class Program
{
}";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS0653ERR_AbstractAttributeClass()
        {
            var text = @"using System;

public abstract class MyAttribute : Attribute
{
}

[My]   // CS0653
class MyClass
{
    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AbstractAttributeClass, Line = 7, Column = 2 });
        }

        [Fact]
        public void CS0655ERR_BadNamedAttributeArgumentType()
        {
            var text = @"using System;

class MyAttribute : Attribute
{
    public decimal d = 0;
    public int e = 0;
}

[My(d = 0)]   // CS0655
class C
{
    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadNamedAttributeArgumentType, Line = 9, Column = 5 });
        }

        [Fact]
        public void CS0655ERR_BadNamedAttributeArgumentType_Dynamic()
        {
            var text = @"
using System;

public class C<T> { public enum D { A } }

[A1(P = null)]                                    // Dev11 error
public class A1 : Attribute                       
{                                                 
    public A1() { }              
    public dynamic P { get; set; }
}                                                 
                                                  
[A2(P = null)]                                    // Dev11 ok (bug)
public class A2 : Attribute                       
{                                                 
    public A2() { }             
    public dynamic[] P { get; set; }
}

[A3(P = 0)]                                       // Dev11 error (bug)
public class A3 : Attribute                       
{                                                 
    public A3() { }             
    public C<dynamic>.D P { get; set; }
}                                                 
                                                  
[A4(P = null)]                                    // Dev11 ok
public class A4 : Attribute
{
    public A4() { }
    public C<dynamic>.D[] P { get; set; }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (6,5): error CS0655: 'P' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [A1(P = null)]                                    // Dev11 error
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "P").WithArguments("P"),
                // (13,5): error CS0655: 'P' is not a valid named attribute argument because it is not a valid attribute parameter type
                // [A2(P = null)]                                    // Dev11 ok (bug)
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "P").WithArguments("P"));
        }

        [Fact]
        public void CS0656ERR_MissingPredefinedMember()
        {
            var text = @"namespace System
{
    public class Object { }
    public struct Byte { }
    public struct Int16 { }
    public struct Int32 { }
    public struct Int64 { }
    public struct Single { }
    public struct Double { }
    public struct SByte { }
    public struct UInt32 { }
    public struct UInt64 { }
    public struct Char { }
    public struct Boolean { }
    public struct UInt16 { }
    public struct UIntPtr { }
    public struct IntPtr { }
    public class Delegate { }
    public class String
    {
        public int Length
        {
            get { return 10; }
        }
    }
    public class MulticastDelegate { }
    public class Array { }
    public class Exception { }
    public class Type { }
    public class ValueType { }
    public class Enum { }
    public interface IEnumerable { }
    public interface IDisposable { }
    public class Attribute { }
    public class ParamArrayAttribute { }
    public struct Void { }
    public struct RuntimeFieldHandle { }
    public struct RuntimeTypeHandle { }


    namespace Collections
    {
        public interface IEnumerable { }
        public interface IEnumerator { }
    }
    namespace Runtime
    {
        namespace InteropServices
        {
            public class OutAttribute { }
        }
        namespace CompilerServices
        {
            public class RuntimeHelpers { }
        }

    }
    namespace Reflection
    {
        public class DefaultMemberAttribute { }
    }

    public class Test
    {

        public unsafe static int Main()
        {
            string str = ""This is my test string"";

            fixed (char* ptr = str)
            {
                if (*(ptr + str.Length) != '\0')
                    return 1;
            }

            return 0;
        }
    }
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (70,32): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.get_OffsetToStringData'
                //             fixed (char* ptr = str)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "str").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "get_OffsetToStringData"));
        }

        // CS0662: see AttributeTests.InOutAttributes_Errors

        [Fact]
        public void CS0663ERR_OverloadRefOut01()
        {
            var text = @"namespace NS
{
    public interface IGoo<T>
    {
        void M(T t);
        void M(ref T t);
        void M(out T t);
    }

    internal class CGoo
    {
        private struct SGoo
        {
            void M<T>(T t) { }
            void M<T>(ref T t) { }
            void M<T>(out T t) { }
        }

        public int RetInt(byte b, out int i)
        {
            return i;
        }

        public int RetInt(byte b, ref int j)
        {
            return 3;
        }

        public int RetInt(byte b, int k)
        {
            return 4;
        }
    }
}
";

            var comp = CreateCompilation(text);

            comp.VerifyDiagnostics(
                // (7,14): error CS0663: 'IGoo<T>' cannot define an overloaded method that differs only on parameter modifiers 'out' and 'ref'
                //         void M(out T t);
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("NS.IGoo<T>", "method", "out", "ref").WithLocation(7, 14),
                // (24,20): error CS0663: 'CGoo' cannot define an overloaded method that differs only on parameter modifiers 'ref' and 'out'
                //         public int RetInt(byte b, ref int j)
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "RetInt").WithArguments("NS.CGoo", "method", "ref", "out").WithLocation(24, 20),
                // (16,18): error CS0663: 'CGoo.SGoo' cannot define an overloaded method that differs only on parameter modifiers 'out' and 'ref'
                //             void M<T>(out T t) { }
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("NS.CGoo.SGoo", "method", "out", "ref").WithLocation(16, 18),
                // (21,20): error CS0269: Use of unassigned out parameter 'i'
                //             return i;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "i").WithArguments("i").WithLocation(21, 20),
                // (21,13): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                //             return i;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return i;").WithArguments("i").WithLocation(21, 13),
                // (16,18): error CS0177: The out parameter 't' must be assigned to before control leaves the current method
                //             void M<T>(out T t) { }
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "M").WithArguments("t").WithLocation(16, 18)
            );
        }

        [Fact]
        public void CS0666ERR_ProtectedInStruct01()
        {
            var text = @"namespace NS
{
    internal struct S1<T, V>
    {
        protected T field;
        protected internal void M(T t, V v) { }
        protected object P { get { return null; } } // Dev10 no error
        protected event System.Action E;

        struct S2
        {
            protected void M1<X>(X p) { }
            protected internal R M2<X, R>(ref X p, R r) { return r; }
            protected internal object Q { get; set; } // Dev10 no error
            protected event System.Action E;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,21): error CS0666: 'NS.S1<T, V>.field': new protected member declared in struct
                //         protected T field;
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "field").WithArguments("NS.S1<T, V>.field"),
                // (7,26): error CS0666: 'NS.S1<T, V>.P': new protected member declared in struct
                //         protected object P { get { return null; } } // Dev10 no error
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "P").WithArguments("NS.S1<T, V>.P"),
                // (8,39): error CS0666: 'NS.S1<T, V>.E': new protected member declared in struct
                //         protected event System.Action E;
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "E").WithArguments("NS.S1<T, V>.E"),
                // (6,33): error CS0666: 'NS.S1<T, V>.M(T, V)': new protected member declared in struct
                //         protected internal void M(T t, V v) { }
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "M").WithArguments("NS.S1<T, V>.M(T, V)"),
                // (14,39): error CS0666: 'NS.S1<T, V>.S2.Q': new protected member declared in struct
                //             protected internal object Q { get; set; } // Dev10 no error
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Q").WithArguments("NS.S1<T, V>.S2.Q"),
                // (15,43): error CS0666: 'NS.S1<T, V>.S2.E': new protected member declared in struct
                //             protected event System.Action E;
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "E").WithArguments("NS.S1<T, V>.S2.E"),
                // (12,28): error CS0666: 'NS.S1<T, V>.S2.M1<X>(X)': new protected member declared in struct
                //             protected void M1<X>(X p) { }
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "M1").WithArguments("NS.S1<T, V>.S2.M1<X>(X)"),
                // (13,34): error CS0666: 'NS.S1<T, V>.S2.M2<X, R>(ref X, R)': new protected member declared in struct
                //             protected internal R M2<X, R>(ref X p, R r) { return r; }
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "M2").WithArguments("NS.S1<T, V>.S2.M2<X, R>(ref X, R)"),
                // (5,21): warning CS0649: Field 'NS.S1<T, V>.field' is never assigned to, and will always have its default value 
                //         protected T field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("NS.S1<T, V>.field", ""),
                // (8,39): warning CS0067: The event 'NS.S1<T, V>.E' is never used
                //         protected event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("NS.S1<T, V>.E"),
                // (15,43): warning CS0067: The event 'NS.S1<T, V>.S2.E' is never used
                //             protected event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("NS.S1<T, V>.S2.E")
                );
        }

        [Fact]
        public void CS0666ERR_ProtectedInStruct02()
        {
            var text = @"struct S
{
    protected object P { get { return null; } }
    public int Q { get; protected set; }
}
struct C<T>
{
    protected internal T P { get; protected set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStruct, Line = 3, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStruct, Line = 4, Column = 35 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStruct, Line = 8, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStruct, Line = 8, Column = 45 });
        }

        [Fact]
        public void CS0666ERR_ProtectedInStruct03()
        {
            var text = @"
struct S
{
    protected event System.Action E;
    protected event System.Action F { add { } remove { } }
    protected int this[int x] { get { return 0; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,35): error CS0666: 'S.E': new protected member declared in struct
                //     protected event System.Action E;
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "E").WithArguments("S.E"),
                // (5,35): error CS0666: 'S.F': new protected member declared in struct
                //     protected event System.Action F { add { } remove { } }
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "F").WithArguments("S.F"),
                // (6,19): error CS0666: 'S.this[int]': new protected member declared in struct
                //     protected int this[int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "this").WithArguments("S.this[int]"),
                // (4,35): warning CS0067: The event 'S.E' is never used
                //     protected event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("S.E"));
        }

        [Fact]
        public void CS0668ERR_InconsistentIndexerNames()
        {
            var text = @"
using System.Runtime.CompilerServices;

class IndexerClass
{
    [IndexerName(""IName1"")]
    public int this[int index]   // indexer declaration
    {
        get
        {
            return index;
        }
        set
        {
        }
    }

    [IndexerName(""IName2"")]
    public int this[string s]    // CS0668, change IName2 to IName1
    {
        get
        {
            return int.Parse(s);
        }
        set
        {
        }
    }

    void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_InconsistentIndexerNames, Line = 19, Column = 16 });
        }

        [Fact]
        public void CS0668ERR_InconsistentIndexerNames02()
        {
            var text = @"
using System.Runtime.CompilerServices;

class IndexerClass
{
    public int this[int[] index] { get { return 0; } set { } }

    [IndexerName(""A"")] // transition from no attribute to A
    public int this[int[,] index] { get { return 0; } set { } }

    // transition from A to no attribute
    public int this[int[,,] index] { get { return 0; } set { } }

    [IndexerName(""B"")] // transition from no attribute to B
    public int this[int[,,,] index] { get { return 0; } set { } }

    [IndexerName(""A"")] // transition from B to A
    public int this[int[,,,,] index] { get { return 0; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"),
                // (12,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"),
                // (15,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"),
                // (18,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"));
        }

        /// <summary>
        /// Same as 02, but with an explicit interface implementation between each pair.
        /// </summary>
        [Fact]
        public void CS0668ERR_InconsistentIndexerNames03()
        {
            var text = @"
using System.Runtime.CompilerServices;

interface I
{
    int this[int[] index] { get; set; }
    int this[int[,] index] { get; set; }
    int this[int[,,] index] { get; set; }
    int this[int[,,,] index] { get; set; }
    int this[int[,,,,] index] { get; set; }
    int this[int[,,,,,] index] { get; set; }
}

class IndexerClass : I
{
        int I.this[int[] index] { get { return 0; } set { } }

    public int this[int[] index] { get { return 0; } set { } }

        int I.this[int[,] index] { get { return 0; } set { } }

    [IndexerName(""A"")] // transition from no attribute to A
    public int this[int[,] index] { get { return 0; } set { } }

        int I.this[int[,,] index] { get { return 0; } set { } }

    // transition from A to no attribute
    public int this[int[,,] index] { get { return 0; } set { } }

        int I.this[int[,,,] index] { get { return 0; } set { } }

    [IndexerName(""B"")] // transition from no attribute to B
    public int this[int[,,,] index] { get { return 0; } set { } }

        int I.this[int[,,,,] index] { get { return 0; } set { } }

    [IndexerName(""A"")] // transition from B to A
    public int this[int[,,,,] index] { get { return 0; } set { } }

        int I.this[int[,,,,,] index] { get { return 0; } set { } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (23,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"),
                // (28,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"),
                // (33,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"),
                // (38,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"));
        }

        [Fact()]
        public void CS0669ERR_ComImportWithUserCtor()
        {
            var text = @"using System.Runtime.InteropServices;
[ComImport, Guid(""00000000-0000-0000-0000-000000000001"")]
class TestClass
{
    TestClass()   // CS0669, delete constructor to resolve
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,5): error CS0669: A class with the ComImport attribute cannot have a user-defined constructor
                //     TestClass()   // CS0669, delete constructor to resolve
                Diagnostic(ErrorCode.ERR_ComImportWithUserCtor, "TestClass").WithLocation(5, 5));
        }

        [Fact]
        public void CS0670ERR_FieldCantHaveVoidType01()
        {
            var text = @"namespace NS
{
    public class Goo
    {
        void Field2 = 0;
        public void Field1;

        public struct SGoo
        {
            void Field1;
            internal void Field2;
        }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,9): error CS0670: Field cannot have void type
                //         void Field2 = 0;
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void"),
                // (6,16): error CS0670: Field cannot have void type
                //         public void Field1;
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void"),
                // (10,13): error CS0670: Field cannot have void type
                //             void Field1;
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void"),
                // (11,22): error CS0670: Field cannot have void type
                //             internal void Field2;
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void"),
                // (5,23): error CS0029: Cannot implicitly convert type 'int' to 'void'
                //         void Field2 = 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "void"),
                // (10,18): warning CS0169: The field 'NS.Goo.SGoo.Field1' is never used
                //             void Field1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Field1").WithArguments("NS.Goo.SGoo.Field1"),
                // (11,27): warning CS0649: Field 'NS.Goo.SGoo.Field2' is never assigned to, and will always have its default value 
                //             internal void Field2;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field2").WithArguments("NS.Goo.SGoo.Field2", "")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0673ERR_SystemVoid01()
        {
            var source =
@"namespace NS
{
    using System;

    interface IGoo<T>
    {
        Void M(T t);
    }

    class Goo
    {
        extern Void GetVoid();

        struct SGoo : IGoo<Void>
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_SystemVoid, "Void"),
                Diagnostic(ErrorCode.ERR_SystemVoid, "Void"),
                Diagnostic(ErrorCode.ERR_SystemVoid, "Void"),
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IGoo<Void>").WithArguments("NS.Goo.SGoo", "NS.IGoo<System.Void>.M(System.Void)"),
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "GetVoid").WithArguments("NS.Goo.GetVoid()"));
        }

        [Fact]
        public void CS0674ERR_ExplicitParamArrayOrCollection()
        {
            var text = @"using System;
public class MyClass
{
    public static void UseParams([ParamArray] int[] list)   // CS0674
    {
    }
    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitParamArrayOrCollection, Line = 4, Column = 35 });
        }

        [Fact]
        public void CS0677ERR_VolatileStruct()
        {
            var text = @"class TestClass
{
    private volatile long i;   // CS0677

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,27): error CS0677: 'TestClass.i': a volatile field cannot be of the type 'long'
                //     private volatile long i;   // CS0677
                Diagnostic(ErrorCode.ERR_VolatileStruct, "i").WithArguments("TestClass.i", "long"),
                // (3,27): warning CS0169: The field 'TestClass.i' is never used
                //     private volatile long i;   // CS0677
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("TestClass.i")
                );
        }

        [Fact]
        public void CS0677ERR_VolatileStruct_TypeParameter()
        {
            var text = @"
class C1<T>
{
    volatile T f; // CS0677
}

class C2<T> where T : class
{
    volatile T f;
}

class C3<T> where T : struct
{
    volatile T f; // CS0677
}

class C4<T> where T : C1<int>
{
    volatile T f;
}

interface I { }

class C5<T> where T : I
{
    volatile T f; // CS0677
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,16): error CS0677: 'C1<T>.f': a volatile field cannot be of the type 'T'
                Diagnostic(ErrorCode.ERR_VolatileStruct, "f").WithArguments("C1<T>.f", "T"),
                // (14,16): error CS0677: 'C3<T>.f': a volatile field cannot be of the type 'T'
                Diagnostic(ErrorCode.ERR_VolatileStruct, "f").WithArguments("C3<T>.f", "T"),
                // (26,16): error CS0677: 'C5<T>.f': a volatile field cannot be of the type 'T'
                Diagnostic(ErrorCode.ERR_VolatileStruct, "f").WithArguments("C5<T>.f", "T"),
                // (4,16): warning CS0169: The field 'C1<T>.f' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C1<T>.f"),
                // (9,16): warning CS0169: The field 'C2<T>.f' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C2<T>.f"),
                // (14,16): warning CS0169: The field 'C3<T>.f' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C3<T>.f"),
                // (19,16): warning CS0169: The field 'C4<T>.f' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C4<T>.f"),
                // (26,16): warning CS0169: The field 'C5<T>.f' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C5<T>.f"));
        }

        [Fact]
        public void CS0678ERR_VolatileAndReadonly()
        {
            var text = @"class TestClass
{
    private readonly volatile int i;   // CS0678
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,35): error CS0678: 'TestClass.i': a field cannot be both volatile and readonly
                //     private readonly volatile int i;   // CS0678
                Diagnostic(ErrorCode.ERR_VolatileAndReadonly, "i").WithArguments("TestClass.i"),
                // (3,35): warning CS0169: The field 'TestClass.i' is never used
                //     private readonly volatile int i;   // CS0678
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("TestClass.i"));
        }

        [Fact]
        public void CS0681ERR_AbstractField01()
        {
            var text = @"namespace NS
{
    class Goo<T>
    {
        public abstract T field;

        struct SGoo 
        {
            abstract internal Goo<object> field;
        }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,27): error CS0681: The modifier 'abstract' is not valid on fields. Try using a property instead.
                //         public abstract T field;
                Diagnostic(ErrorCode.ERR_AbstractField, "field"),
                // (10,43): error CS0681: The modifier 'abstract' is not valid on fields. Try using a property instead.
                //             abstract internal Goo<object> field;
                Diagnostic(ErrorCode.ERR_AbstractField, "field"),
                // (6,27): warning CS0649: Field 'NS.Goo<T>.field' is never assigned to, and will always have its default value 
                //         public abstract T field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("NS.Goo<T>.field", ""),
                // (10,43): warning CS0649: Field 'NS.Goo<T>.SGoo.field' is never assigned to, and will always have its default value null
                //             abstract internal Goo<object> field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("NS.Goo<T>.SGoo.field", "null")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [WorkItem(546447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546447")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void CS0682ERR_BogusExplicitImpl()
        {
            var source1 =
@".class interface public abstract I
{
  .method public abstract virtual instance object get_P() { }
  .method public abstract virtual instance void set_P(object& v) { }
  .property instance object P()
  {
    .get instance object I::get_P()
    .set instance void I::set_P(object& v)
  }
  .method public abstract virtual instance void add_E(class [mscorlib]System.Action v) { }
  .method public abstract virtual instance void remove_E(class [mscorlib]System.Action& v) { }
  .event [mscorlib]System.Action E
  {
    .addon instance void I::add_E(class [mscorlib]System.Action v);
    .removeon instance void I::remove_E(class [mscorlib]System.Action& v);
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"using System;
class C1 : I
{
    object I.get_P() { return null; }
    void I.set_P(ref object v) { }
    void I.add_E(Action v) { }
    void I.remove_E(ref Action v) { }
}
class C2 : I
{
    object I.P
    {
        get { return null; }
        set { }
    }
    event Action I.E
    {
        add { }
        remove { }
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (11,14): error CS0682: 'C2.I.P' cannot implement 'I.P' because it is not supported by the language
                //     object I.P
                Diagnostic(ErrorCode.ERR_BogusExplicitImpl, "P").WithArguments("C2.I.P", "I.P").WithLocation(11, 14),
                // (14,9): warning CS9196: Reference kind modifier of parameter 'object value' doesn't match the corresponding parameter 'ref object v' in overridden or implemented member.
                //         set { }
                Diagnostic(ErrorCode.WRN_OverridingDifferentRefness, "set").WithArguments("object value", "ref object v").WithLocation(14, 9),
                // (16,20): error CS0682: 'C2.I.E' cannot implement 'I.E' because it is not supported by the language
                //     event Action I.E
                Diagnostic(ErrorCode.ERR_BogusExplicitImpl, "E").WithArguments("C2.I.E", "I.E").WithLocation(16, 20));
        }

        [Fact]
        public void CS0683ERR_ExplicitMethodImplAccessor()
        {
            var text = @"interface IExample
{
    int Test { get; }
}

class CExample : IExample
{
    int IExample.get_Test() { return 0; } // CS0683
    int IExample.Test { get { return 0; } } // correct
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitMethodImplAccessor, Line = 8, Column = 18 });
        }

        [Fact]
        public void CS0685ERR_ConditionalWithOutParam()
        {
            var text = @"namespace NS
{
    using System.Diagnostics;

    class Test
    {
        [Conditional(""DEBUG"")]
        void Debug(out int i)  // CS0685
        {
            i = 1;
        }

        [Conditional(""TRACE"")]
        void Trace(ref string p1, out string p2)  // CS0685
        {
            p2 = p1;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,10): error CS0685: Conditional member 'NS.Test.Debug(out int)' cannot have an out parameter
                //         [Conditional("DEBUG")]
                Diagnostic(ErrorCode.ERR_ConditionalWithOutParam, @"Conditional(""DEBUG"")").WithArguments("NS.Test.Debug(out int)").WithLocation(7, 10),
                // (13,10): error CS0685: Conditional member 'NS.Test.Trace(ref string, out string)' cannot have an out parameter
                //         [Conditional("TRACE")]
                Diagnostic(ErrorCode.ERR_ConditionalWithOutParam, @"Conditional(""TRACE"")").WithArguments("NS.Test.Trace(ref string, out string)").WithLocation(13, 10));
        }

        [Fact]
        public void CS0686ERR_AccessorImplementingMethod()
        {
            var text = @"interface I
{
    int get_P();
}

class C : I
{
    public int P
    {
        get { return 1; }  // CS0686
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AccessorImplementingMethod, Line = 10, Column = 9 });
        }

        [Fact]
        public void CS0689ERR_DerivingFromATyVar01()
        {
            var text = @"namespace NS
{
    interface IGoo<T, V> : V
    {
    }

    internal class A<T> : T // CS0689
    {
        protected struct S : T
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DerivingFromATyVar, Line = 3, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DerivingFromATyVar, Line = 7, Column = 27 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_DerivingFromATyVar, Line = 9, Column = 30 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0692ERR_DuplicateTypeParameter()
        {
            var source =
@"class C<T, T>
    where T : class
{
    void M<U, V, U>()
        where U : new()
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0692: Duplicate type parameter 'T'
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T").WithLocation(1, 12),
                // (4,18): error CS0692: Duplicate type parameter 'U'
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "U").WithArguments("U").WithLocation(4, 18));
        }

        [Fact]
        public void CS0693WRN_TypeParameterSameAsOuterTypeParameter01()
        {
            var text = @"namespace NS
{
    interface IGoo<T, V>
    {
        void M<T>();
    }

    public struct S<T>
    {
        public class Outer<T, V>
        {
            class Inner<T>   // CS0693
            {
            }
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, Line = 5, Column = 16, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, Line = 10, Column = 28, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, Line = 12, Column = 25, IsWarning = true });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0694ERR_TypeVariableSameAsParent01()
        {
            var text = @"namespace NS
{
    interface IGoo
    {
        void M<M>(M m); // OK (constraint applies to types but not methods)
    }
    
    class C<C>
    {
        public struct S<T, S>
        {
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,13): error CS0694: Type parameter 'C' has the same name as the containing type, or method
                //     class C<C>
                Diagnostic(ErrorCode.ERR_TypeVariableSameAsParent, "C").WithArguments("C").WithLocation(8, 13),
                // (10,28): error CS0694: Type parameter 'S' has the same name as the containing type, or method
                //         public struct S<T, S>
                Diagnostic(ErrorCode.ERR_TypeVariableSameAsParent, "S").WithArguments("S").WithLocation(10, 28)
                );
        }

        [Fact]
        public void CS0695ERR_UnifyingInterfaceInstantiations()
        {
            // Note: more detailed unification tests are in TypeUnificationTests.cs
            var text = @"interface I<T> { }

class G1<T1, T2> : I<T1>, I<T2> { }  // CS0695

class G2<T1, T2> : I<int>, I<T2> { }  // CS0695

class G3<T1, T2> : I<int>, I<short> { }  // fine

class G4<T1, T2> : I<I<T1>>, I<T1> { }  // fine

class G5<T1, T2> : I<I<T1>>, I<T2> { }  // CS0695

interface I2<T> : I<T> { }

class G6<T1, T2> : I<T1>, I2<T2> { } // CS0695
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnifyingInterfaceInstantiations, Line = 3, Column = 7 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnifyingInterfaceInstantiations, Line = 5, Column = 7 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnifyingInterfaceInstantiations, Line = 11, Column = 7 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnifyingInterfaceInstantiations, Line = 15, Column = 7 });
        }

        [WorkItem(539517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539517")]
        [Fact]
        public void CS0695ERR_UnifyingInterfaceInstantiations2()
        {
            var text = @"
interface I<T, S> { }

class A<T, S> : I<I<T, T>, T>, I<I<T, S>, S> { } // CS0695
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_UnifyingInterfaceInstantiations, Line = 4, Column = 7 });
        }

        [WorkItem(539518, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539518")]
        [Fact]
        public void CS0695ERR_UnifyingInterfaceInstantiations3()
        {
            var text = @"
class A<T, S>
{
    class B : A<B, B> { }
    interface IA { }
    interface IB : B.IA, B.B.IA { } // fine
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [Fact]
        public void CS0698ERR_GenericDerivingFromAttribute01()
        {
            var text =
@"class C<T> : System.Attribute
{
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (1,14): error CS8936: Feature 'generic attributes' is not available in C# 10.0. Please use language version 11.0 or greater.
                // class C<T> : System.Attribute
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "System.Attribute").WithArguments("generic attributes", "11.0").WithLocation(1, 14));
        }

        [Fact]
        public void CS0698ERR_GenericDerivingFromAttribute02()
        {
            var text =
@"class A : System.Attribute { }
class B<T> : A { }
class C<T>
{
    class B : A { }
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (2,14): error CS8936: Feature 'generic attributes' is not available in C# 10.0. Please use language version 11.0 or greater.
                // class B<T> : A { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "A").WithArguments("generic attributes", "11.0").WithLocation(2, 14),
                // (5,15): error CS8936: Feature 'generic attributes' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     class B : A { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "A").WithArguments("generic attributes", "11.0").WithLocation(5, 15));
        }

        [Fact]
        public void CS0699ERR_TyVarNotFoundInConstraint()
        {
            var source =
@"struct S<T>
    where T : class
    where U : struct
{
    void M<U, V>()
        where T : new()
        where W : class
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,11): error CS0699: 'S<T>' does not define type parameter 'U'
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "U").WithArguments("U", "S<T>").WithLocation(3, 11),
                // (6,15): error CS0699: 'S<T>.M<U, V>()' does not define type parameter 'T'
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "T").WithArguments("T", "S<T>.M<U, V>()").WithLocation(6, 15),
                // (7,15): error CS0699: 'S<T>.M<U, V>()' does not define type parameter 'W'
                Diagnostic(ErrorCode.ERR_TyVarNotFoundInConstraint, "W").WithArguments("W", "S<T>.M<U, V>()").WithLocation(7, 15));
        }

        [Fact]
        public void CS0701ERR_BadBoundType()
        {
            var source =
@"delegate void D();
enum E { }
struct S { }
sealed class A<T> { }
class C
{
    void M1<T>() where T : string { }
    void M2<T>() where T : D { }
    void M3<T>() where T : E { }
    void M4<T>() where T : S { }
    void M5<T>() where T : A<T> { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,28): error CS0701: 'string' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadBoundType, "string").WithArguments("string").WithLocation(7, 28),
                // (8,28): error CS0701: 'D' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadBoundType, "D").WithArguments("D").WithLocation(8, 28),
                // (9,28): error CS0701: 'E' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadBoundType, "E").WithArguments("E").WithLocation(9, 28),
                // (10,28): error CS0701: 'S' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadBoundType, "S").WithArguments("S").WithLocation(10, 28),
                // (11,28): error CS0701: 'A<T>' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadBoundType, "A<T>").WithArguments("A<T>").WithLocation(11, 28));
        }

        [Fact]
        public void CS0702ERR_SpecialTypeAsBound()
        {
            var source =
@"using System;
interface IA<T> where T : object { }
interface IB<T> where T : System.Object { }
interface IC<T, U> where T : ValueType { }
interface ID<T> where T : Array { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,27): error CS0702: Constraint cannot be special class 'object'
                // interface IA<T> where T : object { }
                Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "object").WithArguments("object").WithLocation(2, 27),
                // (3,27): error CS0702: Constraint cannot be special class 'object'
                // interface IB<T> where T : System.Object { }
                Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "System.Object").WithArguments("object").WithLocation(3, 27),
                // (4,30): error CS0702: Constraint cannot be special class 'System.ValueType'
                Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "ValueType").WithArguments("System.ValueType").WithLocation(4, 30),
                // (5,27): error CS0702: Constraint cannot be special class 'System.Array'
                Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "Array").WithArguments("System.Array").WithLocation(5, 27));
        }

        [Fact]
        public void CS07ERR_BadVisBound01()
        {
            var source =
@"public class C1
{
    protected interface I<T> { }
    internal class A<T> { }
    public delegate void D<T>() where T : A<T>, I<T>;
}
public class C2 : C1
{
    protected struct S<T, U, V>
        where T : I<A<T>>
        where U : I<I<U>>
        where V : A<A<V>> { }
    internal void M<T, U, V>()
        where T : A<I<T>>
        where U : I<I<U>>
        where V : A<A<V>> { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,43): error CS07: Inconsistent accessibility: constraint type 'C1.A<T>' is less accessible than 'C1.D<T>'
                //     public delegate void D<T>() where T : A<T>, I<T>;
                Diagnostic(ErrorCode.ERR_BadVisBound, "A<T>").WithArguments("C1.D<T>", "C1.A<T>").WithLocation(5, 43),
                // (5,49): error CS07: Inconsistent accessibility: constraint type 'C1.I<T>' is less accessible than 'C1.D<T>'
                //     public delegate void D<T>() where T : A<T>, I<T>;
                Diagnostic(ErrorCode.ERR_BadVisBound, "I<T>").WithArguments("C1.D<T>", "C1.I<T>").WithLocation(5, 49),
                // (14,19): error CS07: Inconsistent accessibility: constraint type 'C1.A<C1.I<T>>' is less accessible than 'C2.M<T, U, V>()'
                //         where T : A<I<T>>
                Diagnostic(ErrorCode.ERR_BadVisBound, "A<I<T>>").WithArguments("C2.M<T, U, V>()", "C1.A<C1.I<T>>").WithLocation(14, 19),
                // (15,19): error CS07: Inconsistent accessibility: constraint type 'C1.I<C1.I<U>>' is less accessible than 'C2.M<T, U, V>()'
                //         where U : I<I<U>>
                Diagnostic(ErrorCode.ERR_BadVisBound, "I<I<U>>").WithArguments("C2.M<T, U, V>()", "C1.I<C1.I<U>>").WithLocation(15, 19),
                // (10,19): error CS07: Inconsistent accessibility: constraint type 'C1.I<C1.A<T>>' is less accessible than 'C2.S<T, U, V>'
                //         where T : I<A<T>>
                Diagnostic(ErrorCode.ERR_BadVisBound, "I<A<T>>").WithArguments("C2.S<T, U, V>", "C1.I<C1.A<T>>").WithLocation(10, 19),
                // (12,19): error CS07: Inconsistent accessibility: constraint type 'C1.A<C1.A<V>>' is less accessible than 'C2.S<T, U, V>'
                //         where V : A<A<V>> { }
                Diagnostic(ErrorCode.ERR_BadVisBound, "A<A<V>>").WithArguments("C2.S<T, U, V>", "C1.A<C1.A<V>>").WithLocation(12, 19));
        }

        [Fact]
        public void CS07ERR_BadVisBound02()
        {
            var source =
@"internal interface IA<T> { }
public interface IB<T, U> { }
public class A
{
    public partial class B<T, U> { }
    public partial class B<T, U> where U : IB<U, IA<T>> { }
    public partial class B<T, U> where U : IB<U, IA<T>> { }
}
public partial class C
{
    public partial void M<T>() where T : IA<T>;
    public partial void M<T>() where T : IA<T> { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods).VerifyDiagnostics(
                // (6,44): error CS07: Inconsistent accessibility: constraint type 'IB<U, IA<T>>' is less accessible than 'A.B<T, U>'
                //     public partial class B<T, U> where U : IB<U, IA<T>> { }
                Diagnostic(ErrorCode.ERR_BadVisBound, "IB<U, IA<T>>").WithArguments("A.B<T, U>", "IB<U, IA<T>>").WithLocation(6, 44),
                // (7,44): error CS07: Inconsistent accessibility: constraint type 'IB<U, IA<T>>' is less accessible than 'A.B<T, U>'
                //     public partial class B<T, U> where U : IB<U, IA<T>> { }
                Diagnostic(ErrorCode.ERR_BadVisBound, "IB<U, IA<T>>").WithArguments("A.B<T, U>", "IB<U, IA<T>>").WithLocation(7, 44),
                // (11,42): error CS07: Inconsistent accessibility: constraint type 'IA<T>' is less accessible than 'C.M<T>()'
                //     public partial void M<T>() where T : IA<T>;
                Diagnostic(ErrorCode.ERR_BadVisBound, "IA<T>").WithArguments("C.M<T>()", "IA<T>").WithLocation(11, 42),
                // (12,42): error CS07: Inconsistent accessibility: constraint type 'IA<T>' is less accessible than 'C.M<T>()'
                //     public partial void M<T>() where T : IA<T> { }
                Diagnostic(ErrorCode.ERR_BadVisBound, "IA<T>").WithArguments("C.M<T>()", "IA<T>").WithLocation(12, 42));
        }

        [Fact]
        public void CS0708ERR_InstanceMemberInStaticClass01()
        {
            var text = @"namespace NS
{
    public static class Goo
    {
        int i;
        void M() { }
        internal object P { get; set; }
        event System.Action E;
    }

    static class Bar<T>
    {
        T field;
        T M(T x) { return x; }
        int Q { get { return 0; } }
        event System.Action<T> E;
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,13): error CS0708: 'NS.Goo.i': cannot declare instance members in a static class
                //         int i;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "i").WithArguments("NS.Goo.i"),
                // (7,25): error CS0708: 'NS.Goo.P': cannot declare instance members in a static class
                //         internal object P { get; set; }
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "P").WithArguments("NS.Goo.P"),
                // (8,29): error CS0708: 'E': cannot declare instance members in a static class
                //         event System.Action E;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "E").WithArguments("E"),
                // (6,14): error CS0708: 'M': cannot declare instance members in a static class
                //         void M() { }
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "M").WithArguments("M"),
                // (13,11): error CS0708: 'NS.Bar<T>.field': cannot declare instance members in a static class
                //         T field;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "field").WithArguments("NS.Bar<T>.field"),
                // (15,13): error CS0708: 'NS.Bar<T>.Q': cannot declare instance members in a static class
                //         int Q { get { return 0; } }
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "Q").WithArguments("NS.Bar<T>.Q"),
                // (16,32): error CS0708: 'E': cannot declare instance members in a static class
                //         event System.Action<T> E;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "E").WithArguments("E"),
                // (16,32): warning CS0067: The event 'NS.Bar<T>.E' is never used
                //         event System.Action<T> E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("NS.Bar<T>.E"),
                // (8,29): warning CS0067: The event 'NS.Goo.E' is never used
                //         event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("NS.Goo.E"),
                // (14,11): error CS0708: 'M': cannot declare instance members in a static class
                //         T M(T x) { return x; }
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "M").WithArguments("M"),
                // (5,13): warning CS0169: The field 'NS.Goo.i' is never used
                //         int i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("NS.Goo.i"),
                // (13,11): warning CS0169: The field 'NS.Bar<T>.field' is never used
                //         T field;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("NS.Bar<T>.field"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0709ERR_StaticBaseClass01()
        {
            var text = @"namespace NS
{
    public static class Base
    {
    }

    public class Derived : Base 
    {
    }

    static class Base1<T, V>
    {
    }

    sealed class Seal<T> : Base1<T, short>
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticBaseClass, Line = 7, Column = 18 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_StaticBaseClass, Line = 15, Column = 18 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0710ERR_ConstructorInStaticClass01()
        {
            var text = @"namespace NS
{
    public static class C
    {
        public C() {}
        C(string s) { }

        static class D<T, V>
        {
            internal D() { }
            internal D(params sbyte[] ary) { }
        }

        static C() { } // no error
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstructorInStaticClass, Line = 5, Column = 16 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstructorInStaticClass, Line = 6, Column = 9 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstructorInStaticClass, Line = 10, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ConstructorInStaticClass, Line = 11, Column = 22 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0711ERR_DestructorInStaticClass()
        {
            var text = @"public static class C
{
    ~C()  // CS0711
    {
    }

    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DestructorInStaticClass, Line = 3, Column = 6 });
        }

        [Fact]
        public void CS0712ERR_InstantiatingStaticClass()
        {
            var text = @"
static class C
{
    static void Main()
    {
        C c = new C(); //CS0712
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): error CS07: Cannot declare a variable of static type 'C'
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "C").WithArguments("C"),
                // (6,15): error CS0712: Cannot create an instance of the static class 'C'
                Diagnostic(ErrorCode.ERR_InstantiatingStaticClass, "new C()").WithArguments("C"));
        }

        [Fact]
        public void CS07ERR_StaticDerivedFromNonObject01()
        {
            var source =
@"namespace NS
{
    public class Base
    {
    }

    public static class Derived : Base
    {
    }

    class Base1<T, V>
    {
    }

    static class D<V> : Base1<string, V>
    {
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (75): error CS07: Static class 'Derived' cannot derive from type 'Base'. Static classes must derive from object.
                //     public static class Derived : Base
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "Base").WithArguments("NS.Derived", "NS.Base").WithLocation(7, 35),
                // (15,25): error CS07: Static class 'D<V>' cannot derive from type 'Base1<string, V>'. Static classes must derive from object.
                //     static class D<V> : Base1<string, V>
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "Base1<string, V>").WithArguments("NS.D<V>", "NS.Base1<string, V>").WithLocation(15, 25));
        }

        [Fact]
        public void CS07ERR_StaticDerivedFromNonObject02()
        {
            var source =
@"delegate void A();
struct B { }
static class C : A { }
static class D : B { }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,18): error CS07: Static class 'D' cannot derive from type 'B'. Static classes must derive from object.
                // static class D : B { }
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "B").WithArguments("D", "B").WithLocation(4, 18),
                // (3,18): error CS07: Static class 'C' cannot derive from type 'A'. Static classes must derive from object.
                // static class C : A { }
                Diagnostic(ErrorCode.ERR_StaticDerivedFromNonObject, "A").WithArguments("C", "A").WithLocation(3, 18));
        }

        [Fact]
        public void CS0714ERR_StaticClassInterfaceImpl01()
        {
            var text = @"namespace NS
{
    interface I
    {
    }

    public static class C : I
    {
    }

    interface IGoo<T, V>
    {
    }

    static class D<V> : IGoo<string, V>
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,29): error CS0714: 'C': static classes cannot implement interfaces
                //     public static class C : I
                Diagnostic(ErrorCode.ERR_StaticClassInterfaceImpl, "I").WithArguments("NS.C").WithLocation(7, 29),
                // (15,25): error CS0714: 'D<V>': static classes cannot implement interfaces
                //     static class D<V> : IGoo<string, V>
                Diagnostic(ErrorCode.ERR_StaticClassInterfaceImpl, "IGoo<string, V>").WithArguments("NS.D<V>").WithLocation(15, 25));
        }

        [Fact]
        public void CS0715ERR_OperatorInStaticClass()
        {
            var text = @"
public static class C
{
    public static C operator +(C c)  // CS0715
    {
        return c;
    }
}
";
            // Note that Roslyn produces these three errors. The native compiler 
            // produces only the first. We might consider suppressing the additional 
            // "cascading" errors in Roslyn.

            CreateCompilation(text).VerifyDiagnostics(
// (4,30): error CS0715: 'C.operator +(C)': static classes cannot contain user-defined operators
//     public static C operator +(C c)  // CS0715
Diagnostic(ErrorCode.ERR_OperatorInStaticClass, "+").WithArguments("C.operator +(C)"),

// (4,32): error CS0721: 'C': static types cannot be used as parameters
//     public static C operator +(C c)  // CS0715
Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C"),

// (4,19): error CS0722: 'C': static types cannot be used as return types
//     public static C operator +(C c)  // CS0715
Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C").WithArguments("C")
                );
        }

        [Fact]
        public void CS0716ERR_ConvertToStaticClass()
        {
            var text = @"
static class C
{
    static void M(object o)
    {
        M((C)o);
        M((C)new object());
        M((C)null);
        M((C)1);
        M((C)""a"");
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,11): error CS0716: Cannot convert to static type 'C'
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)o").WithArguments("C"),
                // (7,11): error CS0716: Cannot convert to static type 'C'
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)new object()").WithArguments("C"),
                // (8,11): error CS0716: Cannot convert to static type 'C'
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)null").WithArguments("C"),
                // (9,11): error CS0716: Cannot convert to static type 'C'
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)1").WithArguments("C"),
                // (10,11): error CS0716: Cannot convert to static type 'C'
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)\"a\"").WithArguments("C"));
        }

        [Fact]
        public void CS7023ERR_StaticInIsAsOrIs()
        {
            // The C# specification states that it is always illegal
            // to use a static type with "is" and "as". The native
            // compiler allows it in some cases; Roslyn gives a warning
            // at level '/warn:5' or higher.

            var text = @"
static class C
{
    static void M(object o)
    {
        M(o as C);            // legal in native
        M(new object() as C); // legal in native
        M(null as C);         // legal in native
        M(1 as C);
        M(""a"" as C);

        M(o is C);            // legal in native, no warning
        M(new object() is C); // legal in native, no warning
        M(null is C);         // legal in native, warns
        M(1 is C);            // legal in native, warns
        M(""a"" is C);        // legal in native, warns
    }
}
";
            var strictDiagnostics = new[]
            {
                // (6,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(o as C);            // legal in native
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "o as C").WithArguments("C").WithLocation(6, 11),
                // (7,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(new object() as C); // legal in native
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "new object() as C").WithArguments("C").WithLocation(7, 11),
                // (8,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(null as C);         // legal in native
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "null as C").WithArguments("C").WithLocation(8, 11),
                // (9,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(1 as C);
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "1 as C").WithArguments("C").WithLocation(9, 11),
                // (9,11): error CS0039: Cannot convert type 'int' to 'C' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                //         M(1 as C);
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, "1 as C").WithArguments("int", "C").WithLocation(9, 11),
                // (10,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M("a" as C);
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, @"""a"" as C").WithArguments("C").WithLocation(10, 11),
                // (10,11): error CS0039: Cannot convert type 'string' to 'C' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                //         M("a" as C);
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, @"""a"" as C").WithArguments("string", "C").WithLocation(10, 11),
                // (12,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(o is C);            // legal in native, no warning
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "o is C").WithArguments("C").WithLocation(12, 11),
                // (13,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(new object() is C); // legal in native, no warning
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "new object() is C").WithArguments("C").WithLocation(13, 11),
                // (14,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(null is C);         // legal in native, warns
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "null is C").WithArguments("C").WithLocation(14, 11),
                // (14,11): warning CS0184: The given expression is never of the provided ('C') type
                //         M(null is C);         // legal in native, warns
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "null is C").WithArguments("C").WithLocation(14, 11),
                // (15,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M(1 is C);            // legal in native, warns
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "1 is C").WithArguments("C").WithLocation(15, 11),
                // (15,11): warning CS0184: The given expression is never of the provided ('C') type
                //         M(1 is C);            // legal in native, warns
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is C").WithArguments("C").WithLocation(15, 11),
                // (16,11): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'C'
                //         M("a" is C);        // legal in native, warns
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, @"""a"" is C").WithArguments("C").WithLocation(16, 11),
                // (16,11): warning CS0184: The given expression is never of the provided ('C') type
                //         M("a" is C);        // legal in native, warns
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, @"""a"" is C").WithArguments("C").WithLocation(16, 11)
            };

            // in /warn:5 we diagnose "is" and "as" operators with a static type.
            var strictComp = CreateCompilation(text);
            strictComp.VerifyDiagnostics(strictDiagnostics);

            // these rest of the diagnostics correspond to those produced by the native compiler.
            var regularDiagnostics = strictDiagnostics.Where(d => !d.Code.Equals((int)ErrorCode.WRN_StaticInAsOrIs)).ToArray();
            var regularComp = CreateCompilation(text, options: TestOptions.ReleaseDll.WithWarningLevel(4));
            regularComp.VerifyDiagnostics(regularDiagnostics);
        }

        [Fact]
        public void CS0717ERR_ConstraintIsStaticClass()
        {
            var source =
@"static class A { }
class B { internal static class C { } }
delegate void D<T, U>() 
    where T : A
    where U : B.C;";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,15): error CS0717: 'A': static classes cannot be used as constraints
                Diagnostic(ErrorCode.ERR_ConstraintIsStaticClass, "A").WithArguments("A").WithLocation(4, 15),
                // (5,15): error CS0717: 'B.C': static classes cannot be used as constraints
                Diagnostic(ErrorCode.ERR_ConstraintIsStaticClass, "B.C").WithArguments("B.C").WithLocation(5, 15));
        }

        [Fact]
        public void CS0718ERR_GenericArgIsStaticClass01()
        {
            var text =
@"interface I<T> { }
class C<T>
{
    internal static void M() { }
}
static class S
{
    static void M<T>()
    {
        I<S> i = null;
        C<S> c = null;
        C<S>.M();
        M<S>();
        object o = typeof(I<S>);
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,11): error CS0718: 'S': static types cannot be used as type arguments
                //         I<S> i = null;
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "S").WithArguments("S").WithLocation(10, 11),
                // (11,11): error CS0718: 'S': static types cannot be used as type arguments
                //         C<S> c = null;
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "S").WithArguments("S").WithLocation(11, 11),
                // (12,11): error CS0718: 'S': static types cannot be used as type arguments
                //         C<S>.M();
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "S").WithArguments("S").WithLocation(12, 11),
                // (13,9): error CS0718: 'S': static types cannot be used as type arguments
                //         M<S>();
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "M<S>").WithArguments("S").WithLocation(13, 9),
                // (14,29): error CS0718: 'S': static types cannot be used as type arguments
                //         object o = typeof(I<S>);
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "S").WithArguments("S").WithLocation(14, 29),
                // (10,14): warning CS0219: The variable 'i' is assigned but its value is never used
                //         I<S> i = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(10, 14),
                // (11,14): warning CS0219: The variable 'c' is assigned but its value is never used
                //         C<S> c = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "c").WithArguments("c").WithLocation(11, 14)
                );
        }

        [Fact]
        public void CS0719ERR_ArrayOfStaticClass01()
        {
            var text = @"namespace NS
{
    public static class C
    {
    }
    static class D<T>
    {
    }

    class Test
    {
        public static int Main()
        {
            var ca = new C[] { null };
            var cd = new D<long>[9];
            return 1;
        }
    }
}
";
            // The native compiler produces two errors for "C[] X;" -- that C cannot be used
            // as the element type of an array, and that C[] is not a legal type for
            // a local because it is static. This seems unnecessary, redundant and wrong.
            // I've eliminated the second error; the first is sufficient to diagnose the issue.

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ArrayOfStaticClass, Line = 14, Column = 26 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ArrayOfStaticClass, Line = 15, Column = 26 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void ERR_VarDeclIsStaticClass02()
        {
            // The native compiler produces two errors for "C[] X;" -- that C cannot be used
            // as the element type of an array, and that C[] is not a legal type for
            // a field because it is static. This seems unnecessary, redundant and wrong.
            // I've eliminated the second error; the first is sufficient to diagnose the issue.

            var text = @"namespace NS
{
    public static class C
    {
    }
    static class D<T>
    {
    }

    class Test
    {
        C[] X;
        C Y;
        D<int> Z;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (12,9): error CS0719: 'NS.C': array elements cannot be of static type
                //         C[] X;
                Diagnostic(ErrorCode.ERR_ArrayOfStaticClass, "C").WithArguments("NS.C"),
                // (13,11): error CS07: Cannot declare a variable of static type 'NS.C'
                //         C Y;
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "Y").WithArguments("NS.C"),
                // (14,16): error CS07: Cannot declare a variable of static type 'NS.D<int>'
                //         D<int> Z;
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "Z").WithArguments("NS.D<int>"),
                // (12,13): warning CS0169: The field 'NS.Test.X' is never used
                //         C[] X;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("NS.Test.X"),
                // (13,11): warning CS0169: The field 'NS.Test.Y' is never used
                //         C Y;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Y").WithArguments("NS.Test.Y"),
                // (14,16): warning CS0169: The field 'NS.Test.Z' is never used
                //         D<int> Z;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Z").WithArguments("NS.Test.Z"));
            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CS0720ERR_IndexerInStaticClass()
        {
            var text = @"public static class Test
{
    public int this[int index]  // CS0720
    {
        get { return 1; }
        set {}
    }

    static void Main() {}
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_IndexerInStaticClass, Line = 3, Column = 16 });
        }

        [Fact]
        public void CS0721ERR_ParameterIsStaticClass01()
        {
            var source = @"namespace NS
{
    public static class C
    {
    }
    public static class D<T>
    {
    }

    interface IGoo<T>
    {
        void M(D<T> d); // Dev10 no error?
    }
    class Test
    {
        public void F(C p)  // CS0721
        {
        }

        struct S
        {
            object M<T>(D<T> p1)  // CS0721
            {
                return null;
            }
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,16): warning CS8897: 'D<T>': static types cannot be used as parameters
                //         void M(D<T> d); // Dev10 no error?
                Diagnostic(ErrorCode.WRN_ParameterIsStaticClass, "D<T>").WithArguments("NS.D<T>").WithLocation(12, 16),
                // (16,23): error CS0721: 'C': static types cannot be used as parameters
                //         public void F(C p)  // CS0721
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("NS.C").WithLocation(16, 23),
                // (22,25): error CS0721: 'D<T>': static types cannot be used as parameters
                //             object M<T>(D<T> p1)  // CS0721
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "D<T>").WithArguments("NS.D<T>").WithLocation(22, 25));
        }

        [Fact]
        public void CS0721ERR_ParameterIsStaticClass02()
        {
            var source =
@"static class S { }
class C
{
    S P { set { } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,5): error CS0721: 'S': static types cannot be used as parameters
                //     S P { set { } }
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "S").WithArguments("S").WithLocation(4, 5));
        }

        [WorkItem(61831, "https://github.com/dotnet/roslyn/issues/61831")]
        [Fact]
        public void CS0721ERR_ParameterIsStaticClass_Lambdas()
        {
            var source =
@"static class S { }
delegate void Dlg(S p);
class C
{
    void M()
    {
        var _a = (S p) => { };
        var _b = delegate (S p) { };
        Dlg _c = (p) => { };
        Dlg _d = p => { };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,19): error CS0721: 'S': static types cannot be used as parameters
                // delegate void Dlg(S p);
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "S").WithArguments("S").WithLocation(2, 19),
                // (7,18): error CS0721: 'S': static types cannot be used as parameters
                //         var _a = (S p) => { };
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "S").WithArguments("S").WithLocation(7, 19),
                // (8,18): error CS0721: 'S': static types cannot be used as parameters
                //         var _b = delegate (S p) { };
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "S").WithArguments("S").WithLocation(8, 28),
                // (9,19): error CS0721: 'S': static types cannot be used as parameters
                //         Dlg _c = (p) => { };
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "p").WithArguments("S").WithLocation(9, 19),
                // (10,18): error CS0721: 'S': static types cannot be used as parameters
                //         Dlg _d = p => { };
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "p").WithArguments("S").WithLocation(10, 18));
        }

        [Fact]
        public void CS0722ERR_ReturnTypeIsStaticClass01()
        {
            var source =
@"namespace NS
{
    public static class C
    {
    }
    public static class D<T>
    {
    }

    interface IGoo<T>
    {
        D<T> M(); // Dev10 no error?
    }
    class Test
    {
        extern public C F(); // CS0722
        //        {
        //            return default(C);
        //        }

        struct S
        {
            extern D<sbyte> M();  // CS0722
            //            {
            //                return default(D<sbyte>);
            //            }
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,14): warning CS8898: 'NS.D<T>': static types cannot be used as return types
                Diagnostic(ErrorCode.WRN_ReturnTypeIsStaticClass, "M").WithArguments("NS.D<T>").WithLocation(12, 14),
                // (16,25): error CS0722: 'NS.C': static types cannot be used as return types
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "F").WithArguments("NS.C").WithLocation(16, 25),
                // (23,29): error CS0722: 'NS.D<sbyte>': static types cannot be used as return types
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "M").WithArguments("NS.D<sbyte>").WithLocation(23, 29),
                // (16,25): warning CS0626: Method, operator, or accessor 'NS.Test.F()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "F").WithArguments("NS.Test.F()").WithLocation(16, 25),
                // (23,29): warning CS0626: Method, operator, or accessor 'NS.Test.S.M()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M").WithArguments("NS.Test.S.M()").WithLocation(23, 29));
        }

        [Fact]
        public void CS0722ERR_ReturnTypeIsStaticClass02()
        {
            var source =
@"static class S { }
class C
{
    S P { get { return null; } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,5): error CS0722: 'S': static types cannot be used as return types
                //     S P { get { return null; } }
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "S").WithArguments("S").WithLocation(4, 5));
        }

        [WorkItem(530434, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530434")]
        [Fact(Skip = "530434")]
        public void CS0722ERR_ReturnTypeIsStaticClass03()
        {
            var source =
@"static class S { }
class C
{
    public abstract S F();
    public abstract S P { get; }
    public abstract S Q { set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,23): error CS0722: 'S': static types cannot be used as return types
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "F").WithArguments("S").WithLocation(4, 23),
                // (4,23): error CS0513: 'C.F()' is abstract but it is contained in non-abstract type 'C'
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "F").WithArguments("C.F()", "C").WithLocation(4, 23),
                // (5,27): error CS0513: 'C.P.get' is abstract but it is contained in non-abstract type 'C'
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "get").WithArguments("C.P.get", "C").WithLocation(5, 27),
                // (6,27): error CS0513: 'C.Q.set' is abstract but it is contained in non-abstract type 'C'
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "set").WithArguments("C.Q.set", "C").WithLocation(6, 27),
                // (5,27): error CS0722: 'S': static types cannot be used as return types
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "get").WithArguments("S").WithLocation(5, 27),
                // (6,27): error CS0721: 'S': static types cannot be used as parameters
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "set").WithArguments("S").WithLocation(6, 27));
        }

        [WorkItem(546540, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546540")]
        [Fact]
        public void CS0722ERR_ReturnTypeIsStaticClass04()
        {
            var source =
@"static class S1 { }
static class S2 { }
class C
{
    public static S1 operator-(C c)
    {
        return null;
    }
    public static implicit operator S2(C c) { return null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
// (5,19): error CS0722: 'S1': static types cannot be used as return types
//     public static S1 operator-(C c)
Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "S1").WithArguments("S1"),

// (9,37): error CS0722: 'S2': static types cannot be used as return types
//     public static implicit operator S2(C c) { return null; }
Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "S2").WithArguments("S2")
                );
        }

        [Fact]
        public void CS0729ERR_ForwardedTypeInThisAssembly()
        {
            var csharp = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Test))]
public class Test 
{
}
";

            CreateCompilation(csharp).VerifyDiagnostics(
                // (4,12): error CS0729: Type 'Test' is defined in this assembly, but a type forwarder is specified for it
                // [assembly: TypeForwardedTo(typeof(Test))]
                Diagnostic(ErrorCode.ERR_ForwardedTypeInThisAssembly, "TypeForwardedTo(typeof(Test))").WithArguments("Test"));
        }

        [Fact, WorkItem(38256, "https://github.com/dotnet/roslyn/issues/38256")]
        public void ParameterAndReturnTypesAreStaticClassesWarning()
        {
            var source = @"
static class C {}
interface I
{
    void M1(C c); // 1
    C M2(); // 2
    C Prop { get; set; } // 3
    C this[C c] { get; set; } // 4, 5
}
";
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (5,13): warning CS8897: 'C': static types cannot be used as parameters
                //     void M1(C c); // 1
                Diagnostic(ErrorCode.WRN_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(5, 13),
                // (6,7): warning CS8898: 'C': static types cannot be used as return types
                //     C M2(); // 2
                Diagnostic(ErrorCode.WRN_ReturnTypeIsStaticClass, "M2").WithArguments("C").WithLocation(6, 7),
                // (7,5): warning CS8898: 'C': static types cannot be used as return types
                //     C Prop { get; set; } // 3
                Diagnostic(ErrorCode.WRN_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(7, 5),
                // (8,5): warning CS8898: 'C': static types cannot be used as return types
                //     C this[C c] { get; set; } // 4, 5
                Diagnostic(ErrorCode.WRN_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(8, 5),
                // (8,12): warning CS8897: 'C': static types cannot be used as parameters
                //     C this[C c] { get; set; } // 4, 5
                Diagnostic(ErrorCode.WRN_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(8, 12)
            );

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(4));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void CS0730ERR_ForwardedTypeIsNested()
        {
            var text1 = @"public class C
{
    public class CC
    {
    }
}
";
            var text2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(C.CC))]";

            var comp1 = CreateCompilation(text1);
            var compRef1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateCompilation(text2, new MetadataReference[] { compRef1 });
            comp2.VerifyDiagnostics(
                // (4,12): error CS0730: Cannot forward type 'C.CC' because it is a nested type of 'C'
                // [assembly: TypeForwardedTo(typeof(C.CC))]
                Diagnostic(ErrorCode.ERR_ForwardedTypeIsNested, "TypeForwardedTo(typeof(C.CC))").WithArguments("C.CC", "C"));
        }

        // See TypeForwarders.Cycle1, etc.
        //[Fact]
        //public void CS0731ERR_CycleInTypeForwarder()

        [Fact]
        public void CS0735ERR_InvalidFwdType()
        {
            var csharp = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(string[]))]
[assembly: TypeForwardedTo(typeof(System.Int32*))]
";

            CreateCompilation(csharp).VerifyDiagnostics(
                // (4,12): error CS0735: Invalid type specified as an argument for TypeForwardedTo attribute
                // [assembly: TypeForwardedTo(typeof(string[]))]
                Diagnostic(ErrorCode.ERR_InvalidFwdType, "TypeForwardedTo(typeof(string[]))"),
                // (5,12): error CS0735: Invalid type specified as an argument for TypeForwardedTo attribute
                // [assembly: TypeForwardedTo(typeof(System.Int32*))]
                Diagnostic(ErrorCode.ERR_InvalidFwdType, "TypeForwardedTo(typeof(System.Int32*))"));
        }

        [Fact]
        public void CS0736ERR_CloseUnimplementedInterfaceMemberStatic()
        {
            var text = @"namespace CS0736
{
    interface ITest
    {
        int testMethod(int x);
    }

    class Program : ITest // CS0736
    {
        public static int testMethod(int x) { return 0; }
        public static void Main() { }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, Line = 8, Column = 21 });
        }

        [Fact]
        public void CS0737ERR_CloseUnimplementedInterfaceMemberNotPublic()
        {
            var text = @"interface ITest
{
    int Return42();
}

struct Struct1 : ITest // CS0737
{
    int Return42() { return (42); }
}

public class Test
{
    public static void Main(string[] args)
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, Line = 6, Column = 18 });
        }

        [Fact]
        public void CS0738ERR_CloseUnimplementedInterfaceMemberWrongReturnType()
        {
            var text = @"

interface ITest
{
    int TestMethod();
}
public class Test : ITest
{
    public void TestMethod() { } // CS0738
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, Line = 7, Column = 21 });
        }

        [Fact]
        public void CS0739ERR_DuplicateTypeForwarder_1()
        {
            var text = @"[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(int))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(int))]
namespace cs0739
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateTypeForwarder, Line = 2, Column = 12 });
        }

        [Fact]
        public void CS0739ERR_DuplicateTypeForwarder_2()
        {
            var csharp = @"
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.Int32))]
[assembly: TypeForwardedTo(typeof(int))]

[assembly: TypeForwardedTo(typeof(List<string>))]
[assembly: TypeForwardedTo(typeof(List<System.String>))]
";

            CreateCompilation(csharp).VerifyDiagnostics(
                // (6,12): error CS0739: 'int' duplicate TypeForwardedToAttribute
                // [assembly: TypeForwardedTo(typeof(int))]
                Diagnostic(ErrorCode.ERR_DuplicateTypeForwarder, "TypeForwardedTo(typeof(int))").WithArguments("int"),
                // (9,12): error CS0739: 'System.Collections.Generic.List<string>' duplicate TypeForwardedToAttribute
                // [assembly: TypeForwardedTo(typeof(List<System.String>))]
                Diagnostic(ErrorCode.ERR_DuplicateTypeForwarder, "TypeForwardedTo(typeof(List<System.String>))").WithArguments("System.Collections.Generic.List<string>"));
        }

        [Fact]
        public void CS0739ERR_DuplicateTypeForwarder_3()
        {
            var csharp = @"
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(List<int>))]
[assembly: TypeForwardedTo(typeof(List<char>))]
[assembly: TypeForwardedTo(typeof(List<>))]
";

            CreateCompilation(csharp).VerifyDiagnostics();
        }

        [Fact]
        public void CS0750ERR_PartialMemberCannotBeAbstract()
        {
            var text = @"

public class Base
{
    protected virtual void PartG()
    {
    }

    protected void PartH()
    {
    }
    protected virtual void PartI()
    {
    }
}

public partial class C : Base
{
    public partial void PartA();
    private partial void PartB();
    protected partial void PartC();
    internal partial void PartD();
    virtual partial void PartE();
    abstract partial void PartF();
    override partial void PartG();
    new partial void PartH();
    sealed override partial void PartI();
    [System.Runtime.InteropServices.DllImport(""none"")]
    extern partial void PartJ();

    public static int Main()
    {
        return 1;
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods).VerifyDiagnostics(
                // (19,25): error CS8793: Partial method 'C.PartA()' must have an implementation part because it has accessibility modifiers.
                //     public partial void PartA();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "PartA").WithArguments("C.PartA()").WithLocation(19, 25),
                // (20,26): error CS8793: Partial method 'C.PartB()' must have an implementation part because it has accessibility modifiers.
                //     private partial void PartB();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "PartB").WithArguments("C.PartB()").WithLocation(20, 26),
                // (21,28): error CS8793: Partial method 'C.PartC()' must have an implementation part because it has accessibility modifiers.
                //     protected partial void PartC();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "PartC").WithArguments("C.PartC()").WithLocation(21, 28),
                // (22,27): error CS8793: Partial method 'C.PartD()' must have an implementation part because it has accessibility modifiers.
                //     internal partial void PartD();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "PartD").WithArguments("C.PartD()").WithLocation(22, 27),
                // (29,25): error CS0759: No defining declaration found for implementing declaration of partial method 'C.PartJ()'
                //     extern partial void PartJ();
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "PartJ").WithArguments("C.PartJ()").WithLocation(29, 25),
                // (23,26): error CS8796: Partial method 'C.PartE()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     virtual partial void PartE();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "PartE").WithArguments("C.PartE()").WithLocation(23, 26),
                // (24,27): error CS0750: A partial member cannot have the 'abstract' modifier
                //     abstract partial void PartF();
                Diagnostic(ErrorCode.ERR_PartialMemberCannotBeAbstract, "PartF").WithLocation(24, 27),
                // (25,27): error CS8796: Partial method 'C.PartG()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     override partial void PartG();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "PartG").WithArguments("C.PartG()").WithLocation(25, 27),
                // (26,22): error CS8796: Partial method 'C.PartH()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     new partial void PartH();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "PartH").WithArguments("C.PartH()").WithLocation(26, 22),
                // (27,34): error CS8796: Partial method 'C.PartI()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     sealed override partial void PartI();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "PartI").WithArguments("C.PartI()").WithLocation(27, 34),
                // (29,25): error CS8796: Partial method 'C.PartJ()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     extern partial void PartJ();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "PartJ").WithArguments("C.PartJ()").WithLocation(29, 25),
                // (25,27): error CS0507: 'C.PartG()': cannot change access modifiers when overriding 'protected' inherited member 'Base.PartG()'
                //     override partial void PartG();
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "PartG").WithArguments("C.PartG()", "protected", "Base.PartG()").WithLocation(25, 27),
                // (27,34): error CS0507: 'C.PartI()': cannot change access modifiers when overriding 'protected' inherited member 'Base.PartI()'
                //     sealed override partial void PartI();
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "PartI").WithArguments("C.PartI()", "protected", "Base.PartI()").WithLocation(27, 34),
                // (28,6): error CS0601: The DllImport attribute must be specified on a method marked 'static' and 'extern'
                //     [System.Runtime.InteropServices.DllImport("none")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "System.Runtime.InteropServices.DllImport").WithLocation(28, 6));
        }

        [Fact]
        public void CS0751ERR_PartialMemberOnlyInPartialClass()
        {
            var text = @"

public class C
{
    partial void Part(); // CS0751
    public static int Main()
    {
        return 1;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialMemberOnlyInPartialClass, Line = 5, Column = 18 });
        }

        [Fact]
        public void CS0752ERR_PartialMethodCannotHaveOutParameters()
        {
            var text = @"

namespace NS
{
    public partial class C
    {
        partial void F(out int x);
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods).VerifyDiagnostics(
                // (7,22): error CS8795: Partial method 'C.F(out int)' must have accessibility modifiers because it has 'out' parameters.
                //         partial void F(out int x);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "F").WithArguments("NS.C.F(out int)").WithLocation(7, 22));
        }

        [Fact]
        public void CS067ERR_ERR_PartialMisplaced()
        {
            var text = @"
partial class C
{
    partial int f;
    partial object P { get { return null; } }
    partial int this[int index]
    {
        get { return index; }
    }
    partial void M();
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method or property return type.
                //     partial int f;
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
                // (5,20): error CS9249: Partial property 'C.P' must have a definition part.
                //     partial object P { get { return null; } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P").WithArguments("C.P").WithLocation(5, 20),
                // (6,17): error CS9249: Partial property 'C.this[int]' must have a definition part.
                //     partial int this[int index]
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C.this[int]").WithLocation(6, 17),
                // (4,17): warning CS0169: The field 'C.f' is never used
                //     partial int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C.f").WithLocation(4, 17));
        }

        [Fact]
        public void CS0754ERR_PartialMemberNotExplicit()
        {
            var text = @"
public interface IF
{
    void Part();
}
public partial class C : IF
{
    partial void IF.Part(); //CS0754
    public static int Main()
    {
        return 1;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialMemberNotExplicit, Line = 8, Column = 21 });
        }

        [Fact]
        public void CS0755ERR_PartialMethodExtensionDifference()
        {
            var text =
@"static partial class C
{
    static partial void M1(this object o);
    static partial void M1(object o) { }
    static partial void M2(object o) { }
    static partial void M2(this object o);
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,25): error CS0755: Both partial method declarations must be extension methods or neither may be an extension method
                Diagnostic(ErrorCode.ERR_PartialMethodExtensionDifference, "M1").WithLocation(4, 25),
                // (5,25): error CS0755: Both partial method declarations must be extension methods or neither may be an extension method
                Diagnostic(ErrorCode.ERR_PartialMethodExtensionDifference, "M2").WithLocation(5, 25));
        }

        [Fact]
        public void CS0756ERR_PartialMethodOnlyOneLatent()
        {
            var text = @"
public partial class C
{
    partial void Part();
    partial void Part(); // CS0756
    public static int Main()
    {
        return 1;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,18): error CS0756: A partial method may not have multiple defining declarations
                //     partial void Part(); // CS0756
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyOneLatent, "Part").WithLocation(5, 18),
                // (5,18): error CS0111: Type 'C' already defines a member called 'Part' with the same parameter types
                //     partial void Part(); // CS0756
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Part").WithArguments("Part", "C").WithLocation(5, 18));
        }

        [Fact]
        public void CS0757ERR_PartialMethodOnlyOneActual()
        {
            var text = @"
public partial class C
{
    partial void Part();
    partial void Part()
    {
    }
    partial void Part() // CS0757
    {
    }
    public static int Main()
    {
        return 1;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_PartialMethodOnlyOneActual, Line = 8, Column = 18 });
        }

        [Fact]
        public void CS0758ERR_PartialMemberParamsDifference()
        {
            var text =
@"partial class C
{
    partial void M1(params object[] args);
    partial void M1(object[] args) { }
    partial void M2(int n, params object[] args) { }
    partial void M2(int n, object[] args);
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,18): error CS0758: Both partial method declarations must use a parameter array or neither may use a parameter array
                Diagnostic(ErrorCode.ERR_PartialMemberParamsDifference, "M1").WithLocation(4, 18),
                // (5,18): error CS0758: Both partial method declarations must use a parameter array or neither may use a parameter array
                Diagnostic(ErrorCode.ERR_PartialMemberParamsDifference, "M2").WithLocation(5, 18));
        }

        [Fact]
        public void CS0759ERR_PartialMethodMustHaveLatent()
        {
            var text =
@"partial class C
{
    partial void M1() { }
    partial void M2();
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M1()'
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M1").WithArguments("C.M1()").WithLocation(3, 18));
        }

        [WorkItem(5427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/5427")]
        [Fact]
        public void CS0759ERR_PartialMethodMustHaveLatent_02()
        {
            var text = @"using System;
static partial class EExtensionMethod
{
    static partial void M(this Array a);
}

static partial class EExtensionMethod
{
    static partial void M() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,25): error CS0759: No defining declaration found for implementing declaration of partial method'EExtensionMethod.M()'
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("EExtensionMethod.M()").WithLocation(9, 25));
        }

        [Fact]
        public void CS0761ERR_PartialMethodInconsistentConstraints01()
        {
            var source =
@"interface IA<T> { }
interface IB { }
partial class C<X>
{
    // Different constraints:
    partial void A1<T>() where T : struct;
    partial void A2<T, U>() where T : struct where U : IA<T>;
    partial void A3<T>() where T : IA<T>;
    partial void A4<T, U>() where T : struct, IA<T>;
    // Additional constraints:
    partial void B1<T>();
    partial void B2<T>() where T : X, new();
    partial void B3<T, U>() where T : IA<T>;
    // Missing constraints.
    partial void C1<T>() where T : class;
    partial void C2<T>() where T : class, new();
    partial void C3<T, U>() where U : IB, IA<T>;
    // Same constraints, different order.
    partial void D1<T>() where T : IA<T>, IB { }
    partial void D2<T, U, V>() where V : T, U, X { }
    // Different constraint clauses.
    partial void E1<T, U>() where U : T { }
    // Additional constraint clause.
    partial void F1<T, U>() where T : class { }
    // Missing constraint clause.
    partial void G1<T, U>() where T : class where U : T { }
    // Same constraint clauses, different order.
    partial void H1<T, U>() where T : class where U : T { }
    partial void H2<T, U>() where T : class where U : T { }
    partial void H3<T, U, V>() where V : class where U : IB where T : IA<V> { }
    // Different type parameter names.
    partial void K1<T, U>() where T : class where U : IA<T> { }
    partial void K2<T, U>() where T : class where U : T, IA<U> { }
}
partial class C<X>
{
    // Different constraints:
    partial void A1<T>() where T : class { }
    partial void A2<T, U>() where T : struct where U : IB { }
    partial void A3<T>() where T : IA<IA<T>> { }
    partial void A4<T, U>() where T : struct, IA<U> { }
    // Additional constraints:
    partial void B1<T>() where T : new() { }
    partial void B2<T>() where T : class, X, new() { }
    partial void B3<T, U>() where T : IB, IA<T> { }
    // Missing constraints.
    partial void C1<T>() { }
    partial void C2<T>() where T : class { }
    partial void C3<T, U>() where U : IA<T> { }
    // Same constraints, different order.
    partial void D1<T>() where T : IB, IA<T>;
    partial void D2<T, U, V>() where V : U, X, T;
    // Different constraint clauses.
    partial void E1<T, U>() where T : class;
    // Additional constraint clause.
    partial void F1<T, U>() where T : class where U : T;
    // Missing constraint clause.
    partial void G1<T, U>() where T : class;
    // Same constraint clauses, different order.
    partial void H1<T, U>() where U : T where T : class;
    partial void H2<T, U>() where U : class where T : U;
    partial void H3<T, U, V>() where T : IA<V> where U : IB where V : class;
    // Different type parameter names.
    partial void K1<U, T>() where T : class where U : IA<T>;
    partial void K2<T1, T2>() where T1 : class where T2 : T1, IA<T2>;
}";
            // Note: Errors are reported on A1, A2, ... rather than A1<T>, A2<T, U>, ... See bug #9396.
            CreateCompilation(source).VerifyDiagnostics(
                // (39,18): error CS0761: Partial method declarations of 'C<X>.A2<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void A2<T, U>() where T : struct where U : IB { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "A2").WithArguments("C<X>.A2<T, U>()", "U").WithLocation(39, 18),
                // (40,18): error CS0761: Partial method declarations of 'C<X>.A3<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void A3<T>() where T : IA<IA<T>> { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "A3").WithArguments("C<X>.A3<T>()", "T").WithLocation(40, 18),
                // (41,18): error CS0761: Partial method declarations of 'C<X>.A4<T, U>()' have inconsistent constraints for type parameter 'T'
                //     partial void A4<T, U>() where T : struct, IA<U> { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "A4").WithArguments("C<X>.A4<T, U>()", "T").WithLocation(41, 18),
                // (43,18): error CS0761: Partial method declarations of 'C<X>.B1<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void B1<T>() where T : new() { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "B1").WithArguments("C<X>.B1<T>()", "T").WithLocation(43, 18),
                // (44,18): error CS0761: Partial method declarations of 'C<X>.B2<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void B2<T>() where T : class, X, new() { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "B2").WithArguments("C<X>.B2<T>()", "T").WithLocation(44, 18),
                // (45,18): error CS0761: Partial method declarations of 'C<X>.B3<T, U>()' have inconsistent constraints for type parameter 'T'
                //     partial void B3<T, U>() where T : IB, IA<T> { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "B3").WithArguments("C<X>.B3<T, U>()", "T").WithLocation(45, 18),
                // (47,18): error CS0761: Partial method declarations of 'C<X>.C1<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void C1<T>() { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "C1").WithArguments("C<X>.C1<T>()", "T").WithLocation(47, 18),
                // (48,18): error CS0761: Partial method declarations of 'C<X>.C2<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void C2<T>() where T : class { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "C2").WithArguments("C<X>.C2<T>()", "T").WithLocation(48, 18),
                // (49,18): error CS0761: Partial method declarations of 'C<X>.C3<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void C3<T, U>() where U : IA<T> { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "C3").WithArguments("C<X>.C3<T, U>()", "U").WithLocation(49, 18),
                // (22,18): error CS0761: Partial method declarations of 'C<X>.E1<T, U>()' have inconsistent constraints for type parameter 'T'
                //     partial void E1<T, U>() where U : T { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "E1").WithArguments("C<X>.E1<T, U>()", "T").WithLocation(22, 18),
                // (22,18): error CS0761: Partial method declarations of 'C<X>.E1<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void E1<T, U>() where U : T { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "E1").WithArguments("C<X>.E1<T, U>()", "U").WithLocation(22, 18),
                // (24,18): error CS0761: Partial method declarations of 'C<X>.F1<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void F1<T, U>() where T : class { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "F1").WithArguments("C<X>.F1<T, U>()", "U").WithLocation(24, 18),
                // (26,18): error CS0761: Partial method declarations of 'C<X>.G1<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void G1<T, U>() where T : class where U : T { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "G1").WithArguments("C<X>.G1<T, U>()", "U").WithLocation(26, 18),
                // (29,18): error CS0761: Partial method declarations of 'C<X>.H2<T, U>()' have inconsistent constraints for type parameter 'T'
                //     partial void H2<T, U>() where T : class where U : T { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "H2").WithArguments("C<X>.H2<T, U>()", "T").WithLocation(29, 18),
                // (29,18): error CS0761: Partial method declarations of 'C<X>.H2<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void H2<T, U>() where T : class where U : T { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "H2").WithArguments("C<X>.H2<T, U>()", "U").WithLocation(29, 18),
                // (32,18): error CS0761: Partial method declarations of 'C<X>.K1<T, U>()' have inconsistent constraints for type parameter 'T'
                //     partial void K1<T, U>() where T : class where U : IA<T> { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "K1").WithArguments("C<X>.K1<T, U>()", "T").WithLocation(32, 18),
                // (32,18): error CS0761: Partial method declarations of 'C<X>.K1<T, U>()' have inconsistent constraints for type parameter 'U'
                //     partial void K1<T, U>() where T : class where U : IA<T> { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "K1").WithArguments("C<X>.K1<T, U>()", "U").WithLocation(32, 18),
                // (32,18): warning CS8826: Partial method declarations 'void C<X>.K1<U, T>()' and 'void C<X>.K1<T, U>()' have differences in parameter names, parameter types, or return types.
                //     partial void K1<T, U>() where T : class where U : IA<T> { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "K1").WithArguments("void C<X>.K1<U, T>()", "void C<X>.K1<T, U>()").WithLocation(32, 18),
                // (33,18): warning CS8826: Partial method declarations 'void C<X>.K2<T1, T2>()' and 'void C<X>.K2<T, U>()' have differences in parameter names, parameter types, or return types.
                //     partial void K2<T, U>() where T : class where U : T, IA<U> { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "K2").WithArguments("void C<X>.K2<T1, T2>()", "void C<X>.K2<T, U>()").WithLocation(33, 18),
                // (38,18): error CS0761: Partial method declarations of 'C<X>.A1<T>()' have inconsistent constraints for type parameter 'T'
                //     partial void A1<T>() where T : class { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "A1").WithArguments("C<X>.A1<T>()", "T").WithLocation(38, 18));
        }

        [Fact]
        public void CS0761ERR_PartialMethodInconsistentConstraints02()
        {
            var source =
@"using NIA = N.IA;
using NIBA = N.IB<N.IA>;
using NIBAC = N.IB<N.A.IC>;
using NA = N.A;
namespace N
{
    interface IA { }
    interface IB<T> { }
    class A
    {
        internal interface IC { }
    }
    partial class C
    {
        partial void M1<T>() where T : A, NIBA;
        partial void M2<T>() where T : NA, IB<IA>;
        partial void M3<T, U>() where U : NIBAC;
        partial void M4<T, U>() where T : U, NIA;
    }
    partial class C
    {
        partial void M1<T>() where T : N.A, IB<IA> { }
        partial void M2<T>() where T : A, NIBA { }
        partial void M3<T, U>() where U : N.IB<A.IC> { }
        partial void M4<T, U>() where T : NIA { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (25,22): error CS0761: Partial method declarations of 'C.M4<T, U>()' have inconsistent constraints for type parameter 'T'
                //         partial void M4<T, U>() where T : NIA { }
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M4").WithArguments("N.C.M4<T, U>()", "T").WithLocation(25, 22));
        }

        [Fact]
        public void CS07ERR_PartialMethodStaticDifference()
        {
            var text =
@"partial class C
{
    static partial void M1();
    partial void M1() { }
    static partial void M2() { }
    partial void M2();
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,18): error CS07: Both partial member declarations must be static or neither may be static
                Diagnostic(ErrorCode.ERR_PartialMemberStaticDifference, "M1").WithLocation(4, 18),
                // (5,25): error CS07: Both partial member declarations must be static or neither may be static
                Diagnostic(ErrorCode.ERR_PartialMemberStaticDifference, "M2").WithLocation(5, 25));
        }

        [Fact]
        public void CS0764ERR_PartialMemberUnsafeDifference()
        {
            var text =
@"partial class C
{
    unsafe partial void M1();
    partial void M1() { }
    unsafe partial void M2() { }
    partial void M2();
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,18): error CS0764: Both partial member declarations must be unsafe or neither may be unsafe
                Diagnostic(ErrorCode.ERR_PartialMemberUnsafeDifference, "M1").WithLocation(4, 18),
                // (5,25): error CS0764: Both partial member declarations must be unsafe or neither may be unsafe
                Diagnostic(ErrorCode.ERR_PartialMemberUnsafeDifference, "M2").WithLocation(5, 25));
        }

        [Fact]
        public void CS0766ERR_PartialMethodMustReturnVoid()
        {
            var text = @"

public partial class C
{
    partial int Part();
    partial int Part()
    {
        return 1;
    }

    public static int Main()
    {
        return 1;
    }

}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,17): error CS8794: Partial method 'C.Part()' must have accessibility modifiers because it has a non-void return type.
                //     partial int Part();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "Part").WithArguments("C.Part()").WithLocation(5, 17),
                // (6,17): error CS8794: Partial method 'C.Part()' must have accessibility modifiers because it has a non-void return type.
                //     partial int Part()
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "Part").WithArguments("C.Part()").WithLocation(6, 17));
        }

        [Fact]
        public void CS0767ERR_ExplicitImplCollisionOnRefOut()
        {
            var text = @"interface IFace<T>
{
    void Goo(ref T x);
    void Goo(out int x);
}

class A : IFace<int>
{
    void IFace<int>.Goo(ref int x)
    {
    }

    void IFace<int>.Goo(out int x)
    {
        x = 0;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitImplCollisionOnRefOut, Line = 1, Column = 11 }, //error for IFace<int, int>.Goo(ref int)
                new ErrorDescription { Code = (int)ErrorCode.ERR_ExplicitImplCollisionOnRefOut, Line = 1, Column = 11 } //error for IFace<int, int>.Goo(out int)
                );
        }

        [Fact]
        public void CS0825ERR_TypeVarNotFound01()
        {
            var text = @"namespace NS
{
    class Test
    {
        static var myStaticField;
        extern private var M();
        void MM(ref var v) { }
    } 
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,24): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         extern private var M();
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var"),
                // (7,21): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         void MM(ref var v) { }
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var"),
                // (5,16): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         static var myStaticField;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var"),
                // (6,28): warning CS0626: Method, operator, or accessor 'NS.Test.M()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern private var M();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M").WithArguments("NS.Test.M()"),
                // (5,20): warning CS0169: The field 'NS.Test.myStaticField' is never used
                //         static var myStaticField;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "myStaticField").WithArguments("NS.Test.myStaticField")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        [WorkItem(22512, "https://github.com/dotnet/roslyn/issues/22512")]
        public void CS0842ERR_ExplicitLayoutAndAutoImplementedProperty()
        {
            var text = @"
using System.Runtime.InteropServices;

namespace TestNamespace
{
    [StructLayout(LayoutKind.Explicit)]
    struct Str
    {
        public int Num // CS0625
        {
            get;
            set;
        }

        static int Main()
        {
            return 1;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,20): error CS0625: 'Str.Num': instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
                //         public int Num // CS0625
                Diagnostic(ErrorCode.ERR_MissingStructOffset, "Num").WithArguments("TestNamespace.Str.Num").WithLocation(9, 20)
                );
        }

        [Fact]
        [WorkItem(1032724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032724")]
        public void CS0842ERR_ExplicitLayoutAndAutoImplementedProperty_Bug1032724()
        {
            var text = @"
using System.Runtime.InteropServices;

namespace TestNamespace
{
    [StructLayout(LayoutKind.Explicit)]
    struct Str
    {
        public static int Num
        {
            get;
            set;
        }

        static int Main()
        {
            return 1;
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS0851ERR_OverloadRefOutCtor()
        {
            var text = @"namespace TestNamespace
{
    class MyClass
    {
        public  MyClass(ref int num)
        {
        }
        public  MyClass(out int num)
        {
            num = 1;
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,17): error CS0663: 'MyClass' cannot define an overloaded constructor that differs only on parameter modifiers 'out' and 'ref'
                //         public  MyClass(out int num)
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "MyClass").WithArguments("TestNamespace.MyClass", "constructor", "out", "ref").WithLocation(8, 17));
        }

        [Fact]
        public void CS1014ERR_GetOrSetExpected()
        {
            var source =
@"partial class C
{
    public object P { partial get; set; }
    object Q { get { return 0; } add { } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,23): error CS1014: A get, set or init accessor expected
                //     public object P { partial get; set; }
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(3, 23),
                // (4,34): error CS1014: A get, set or init accessor expected
                //     object Q { get { return 0; } add { } }
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 34));
        }

        [Fact]
        public void CS1057ERR_ProtectedInStatic01()
        {
            var text = @"namespace NS
{
    public static class B
    {
        protected static object field = null;
        internal protected static void M() {}
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 5, Column = 33 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 6, Column = 40 });

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        public void CanNotDeclareProtectedPropertyInStaticClass()
        {
            const string text = @"
static class B {
  protected static int X { get; set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 3, Column = 24 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 3, Column = 28 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 3, Column = 33 });
        }

        [Fact]
        public void CanNotDeclareProtectedInternalPropertyInStaticClass()
        {
            const string text = @"
static class B {
  internal static protected int X { get; set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 3, Column = 33 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 3, Column = 37 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic, Line = 3, Column = 42 });
        }

        [Fact]
        public void CanNotDeclarePropertyWithProtectedInternalAccessorInStaticClass()
        {
            const string text = @"
static class B {
  public static int X { get; protected internal set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription { Code = (int)ErrorCode.ERR_ProtectedInStatic });
        }

        /// <summary>
        /// variance
        /// </summary>
        [Fact]
        public void CS1067ERR_PartialWrongTypeParamsVariance01()
        {
            var text = @"namespace NS
{
    //combinations
    partial interface I1<in T> { }
    partial interface I1<out T> { }

    partial interface I2<in T> { }
    partial interface I2<T> { }

    partial interface I3<T> { }
    partial interface I3<out T> { }

    //no duplicate errors
    partial interface I4<T, U> { }
    partial interface I4<out T, out U> { }

    //prefer over CS0264
    partial interface I5<S> { }
    partial interface I5<out T> { }

    //no error after CS0264
    partial interface I6<R, in T> { }
    partial interface I6<S, out T> { }

    //no error since arities don't match
    partial interface I7<T> { }
    partial interface I7<in T, U> { }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,23): error CS1067: Partial declarations of 'NS.I1<T>' must have the same type parameter names and variance modifiers in the same order
                Diagnostic(ErrorCode.ERR_PartialWrongTypeParamsVariance, "I1").WithArguments("NS.I1<T>"),
                // (7,23): error CS1067: Partial declarations of 'NS.I2<T>' must have the same type parameter names and variance modifiers in the same order
                Diagnostic(ErrorCode.ERR_PartialWrongTypeParamsVariance, "I2").WithArguments("NS.I2<T>"),
                // (10,23): error CS1067: Partial declarations of 'NS.I3<T>' must have the same type parameter names and variance modifiers in the same order
                Diagnostic(ErrorCode.ERR_PartialWrongTypeParamsVariance, "I3").WithArguments("NS.I3<T>"),
                // (14,23): error CS1067: Partial declarations of 'NS.I4<T, U>' must have the same type parameter names and variance modifiers in the same order
                Diagnostic(ErrorCode.ERR_PartialWrongTypeParamsVariance, "I4").WithArguments("NS.I4<T, U>"),
                // (18,23): error CS1067: Partial declarations of 'NS.I5<S>' must have the same type parameter names and variance modifiers in the same order
                Diagnostic(ErrorCode.ERR_PartialWrongTypeParamsVariance, "I5").WithArguments("NS.I5<S>"),
                // (22,23): error CS0264: Partial declarations of 'NS.I6<R, T>' must have the same type parameter names in the same order
                Diagnostic(ErrorCode.ERR_PartialWrongTypeParams, "I6").WithArguments("NS.I6<R, T>"));
        }

        [Fact]
        public void CS1100ERR_BadThisParam()
        {
            var text = @"static class Test
{
    static void ExtMethod(int i, this Goo1 c) // CS1100
    {
    }
}
class Goo1
{
}";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (3,34): error CS1100: Method 'ExtMethod' has a parameter modifier 'this' which is not on the first parameter
                Diagnostic(ErrorCode.ERR_BadThisParam, "this").WithArguments("ExtMethod").WithLocation(3, 34));
        }

        [Fact]
        public void CS1103ERR_BadTypeforThis01()
        {
            // Note that the dev11 compiler does not report error CS0721, that C cannot be used as a parameter type.
            // This appears to be a shortcoming of the dev11 compiler; there is no good reason to not report the error.

            var compilation = CreateCompilation(
@"static class C
{
    static void M1(this Unknown u) { }
    static void M2(this C c) { }
    static void M3(this dynamic d) { }
    static void M4(this dynamic[] d) { }
}");

            compilation.VerifyDiagnostics(
                // (3,25): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown"),
                // (4,25): error CS0721: 'C': static types cannot be used as parameters
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C"),
                // (5,25): error CS1103: The first parameter of an extension method cannot be of type 'dynamic'
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic"));
        }

        [Fact]
        public void CS1103ERR_BadTypeforThis02()
        {
            CreateCompilation(
@"public static class Extensions
{
    public unsafe static char* Test(this char* charP) { return charP; } // CS1103
} 
", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "char*").WithArguments("char*").WithLocation(3, 42));
        }

        [Fact]
        public void CS1105ERR_BadExtensionMeth()
        {
            var text = @"public class Extensions
{
    public void Test<T>(this System.String s) { } //CS1105
}
";
            var reference = SystemCoreRef;
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new List<MetadataReference> { reference },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadExtensionAgg, Line = 1, Column = 14 });
        }

        [Fact]
        public void CS1106ERR_BadExtensionAgg01()
        {
            var compilation = CreateCompilation(
@"class A
{
    static void M(this object o) { }
}
class B
{
    static void M<T>(this object o) { }
}
static class C<T>
{
    static void M(this object o) { }
}
static class D<T>
{
    static void M<U>(this object o) { }
}
struct S
{
    static void M(this object o) { }
}
struct T
{
    static void M<U>(this object o) { }
}
struct U<T>
{
    static void M(this object o) { }
}");

            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "A").WithLocation(1, 7),
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "B").WithLocation(5, 7),
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "C").WithLocation(9, 14),
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "D").WithLocation(13, 14),
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "S").WithLocation(17, 8),
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "T").WithLocation(21, 8),
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "U").WithLocation(25, 8));
        }

        [WorkItem(528256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528256")]
        [Fact()]
        public void CS1106ERR_BadExtensionAgg02()
        {
            CreateCompilation(
@"interface I
{
    static void M(this object o);
}", parseOptions: TestOptions.Regular7)
                .VerifyDiagnostics(
                    // (3,17): error CS8503: The modifier 'static' is not valid for this item in C# 7. Please use language version '8.0' or greater.
                    //     static void M(this object o);
                    Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M").WithArguments("static", "7.0", "8.0").WithLocation(3, 17),
                    // (1,11): error CS1106: Extension method must be defined in a non-generic static class
                    // interface I
                    Diagnostic(ErrorCode.ERR_BadExtensionAgg, "I").WithLocation(1, 11),
                    // (3,17): error CS0501: 'I.M(object)' must declare a body because it is not marked abstract, extern, or partial
                    //     static void M(this object o);
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("I.M(object)").WithLocation(3, 17)
                    );
        }

        [Fact]
        public void CS1109ERR_ExtensionMethodsDecl()
        {
            var compilation = CreateCompilation(
@"class A
{
    static class C
    {
        static void M(this object o) { }
    }
}
static class B
{
    static class C
    {
        static void M(this object o) { }
    }
}
struct S
{
    static class C
    {
        static void M(this object o) { }
    }
}");

            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_ExtensionMethodsDecl, "M").WithArguments("C").WithLocation(5, 21),
                Diagnostic(ErrorCode.ERR_ExtensionMethodsDecl, "M").WithArguments("C").WithLocation(12, 21),
                Diagnostic(ErrorCode.ERR_ExtensionMethodsDecl, "M").WithArguments("C").WithLocation(19, 21));
        }

        [Fact]
        public void CS1110ERR_ExtensionAttrNotFound()
        {
            //Extension method cannot be declared without a reference to System.Core.dll
            var source =
@"static class A
{
    public static void M1(this object o) { }
    public static void M2(this string s, this object o) { }
    public static void M3(this dynamic d) { }
}
class B
{
    public static void M4(this object o) { }
}";
            var compilation = CreateEmptyCompilation(source, new[] { Net40.References.mscorlib });
            compilation.VerifyDiagnostics(
                // (3,27): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(3, 27),
                // (4,27): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(4, 27),
                // (4,42): error CS1100: Method 'M2' has a parameter modifier 'this' which is not on the first parameter
                Diagnostic(ErrorCode.ERR_BadThisParam, "this").WithArguments("M2").WithLocation(4, 42),
                // (5,32): error CS1103: The first parameter of an extension method cannot be of type 'dynamic'
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic").WithLocation(5, 32),
                // (5,32): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(5, 32),
                // (7,7): error CS1106: Extension methods must be defined in a non-generic static class
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "B").WithLocation(7, 7));
        }

        [Fact]
        public void CS1112ERR_ExplicitExtension()
        {
            var source =
@"using System.Runtime.CompilerServices;
[System.Runtime.CompilerServices.ExtensionAttribute]
static class A
{
    static void M(object o) { }
}
static class B
{
    [Extension]
    static void M() { }
    [ExtensionAttribute]
    static void M(this object o) { }
    [Extension]
    static object P
    {
        [Extension]
        get { return null; }
    }
    [Extension(0)]
    static object F;
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (2,2): error CS1112: Do not use 'System.Runtime.CompilerServices.ExtensionAttribute'. Use the 'this' keyword instead.
                // [System.Runtime.CompilerServices.ExtensionAttribute]
                Diagnostic(ErrorCode.ERR_ExplicitExtension, "System.Runtime.CompilerServices.ExtensionAttribute"),
                // (9,6): error CS1112: Do not use 'System.Runtime.CompilerServices.ExtensionAttribute'. Use the 'this' keyword instead.
                //     [Extension]
                Diagnostic(ErrorCode.ERR_ExplicitExtension, "Extension"),
                // (11,6): error CS1112: Do not use 'System.Runtime.CompilerServices.ExtensionAttribute'. Use the 'this' keyword instead.
                //     [ExtensionAttribute]
                Diagnostic(ErrorCode.ERR_ExplicitExtension, "ExtensionAttribute"),
                // (13,6): error CS0592: Attribute 'Extension' is not valid on this declaration type. It is only valid on 'assembly, class, method' declarations.
                //     [Extension]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Extension").WithArguments("Extension", "assembly, class, method"),
                // (16,10): error CS1112: Do not use 'System.Runtime.CompilerServices.ExtensionAttribute'. Use the 'this' keyword instead.
                //         [Extension]
                Diagnostic(ErrorCode.ERR_ExplicitExtension, "Extension"),
                // (19,6): error CS1729: 'System.Runtime.CompilerServices.ExtensionAttribute' does not contain a constructor that takes 1 arguments
                //     [Extension(0)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Extension(0)").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute", "1"),
                // (20,19): warning CS0169: The field 'B.F' is never used
                //     static object F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("B.F"));
        }

        [Fact]
        public void CS1509ERR_ImportNonAssembly()
        {
            //CSC /TARGET:library /reference:class1.netmodule text.CS
            var text = @"class Test
{
    public static int Main()
    {
        return 1;
    }
}";
            var ref1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "NetModule.mod");

            CreateCompilation(text, new[] { ref1 }).VerifyDiagnostics(
                // error CS1509: The referenced file 'NetModule.mod' is not an assembly
                Diagnostic(ErrorCode.ERR_ImportNonAssembly).WithArguments(@"NetModule.mod"));
        }

        [Fact]
        public void CS1527ERR_NoNamespacePrivate1()
        {
            var text = @"private class C { }";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoNamespacePrivate, Line = 1, Column = 15 } //pos = the class name
                );
        }

        [Fact]
        public void CS1527ERR_NoNamespacePrivate2()
        {
            var text = @"protected class C { }";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoNamespacePrivate, Line = 1, Column = 17 } //pos = the class name
                );
        }

        [Fact]
        public void CS1527ERR_NoNamespacePrivate3()
        {
            var text = @"protected internal class C { }";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoNamespacePrivate, Line = 1, Column = 26 } //pos = the class name
                );
        }

        [Fact]
        public void CS1537ERR_DuplicateAlias1()
        {
            var text = @"using A = System;
using A = System;

namespace NS
{
    using O = System.Object;
    using O = System.Object;
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,7): error CS1537: The using alias 'A' appeared previously in this namespace
                // using A = System;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "A").WithArguments("A"),
                // (7,11): error CS1537: The using alias 'O' appeared previously in this namespace
                //     using O = System.Object;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "O").WithArguments("O"),
                // (1,1): info CS8019: Unnecessary using directive.
                // using A = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = System;"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using A = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = System;"),
                // (6,5): info CS8019: Unnecessary using directive.
                //     using O = System.Object;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using O = System.Object;"),
                // (7,5): info CS8019: Unnecessary using directive.
                //     using O = System.Object;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using O = System.Object;"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [WorkItem(537684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537684")]
        [Fact]
        public void CS1537ERR_DuplicateAlias2()
        {
            var text = @"namespace namespace1
{
    class A { }
}

namespace namespace2
{
    class B { }
}

namespace NS
{
    using ns = namespace1;
    using ns = namespace2;
    using System;

    class C : ns.A
    {
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (13,11): warning CS8981: The type name 'ns' only contains lower-cased ascii characters. Such names may become reserved for the language.
                //     using ns = namespace1;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "ns").WithArguments("ns").WithLocation(13, 11),
                // (14,11): warning CS8981: The type name 'ns' only contains lower-cased ascii characters. Such names may become reserved for the language.
                //     using ns = namespace2;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "ns").WithArguments("ns").WithLocation(14, 11),
                // (14,11): error CS1537: The using alias 'ns' appeared previously in this namespace
                //     using ns = namespace2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "ns").WithArguments("ns").WithLocation(14, 11),
                // (15,5): hidden CS8019: Unnecessary using directive.
                //     using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(15, 5),
                // (14,5): hidden CS8019: Unnecessary using directive.
                //     using ns = namespace2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using ns = namespace2;").WithLocation(14, 5));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetMembers("C").Single() as NamedTypeSymbol;
            var b = type1.BaseType();
        }

        [Fact]
        public void CS1537ERR_DuplicateAlias3()
        {
            var text = @"using X = System;
using X = ABC.X<int>;";
            CreateCompilation(text).VerifyDiagnostics(
                // (2,7): error CS1537: The using alias 'X' appeared previously in this namespace
                // using X = ABC.X<int>;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "X").WithArguments("X"),
                // (1,1): info CS8019: Unnecessary using directive.
                // using X = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = System;"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using X = ABC.X<int>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = ABC.X<int>;"));
        }

        [WorkItem(539125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539125")]
        [Fact]
        public void CS1542ERR_AddModuleAssembly()
        {
            //CSC /addmodule:cs1542.dll /TARGET:library text1.cs
            var text = @"public class Goo : IGoo
{
    public void M0() { }
}";
            var ref1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef).GetReference(display: "NoMsCorLibRef.mod");

            CreateCompilation(text, references: new[] { ref1 }).VerifyDiagnostics(
                // error CS1542: 'NoMsCorLibRef.mod' cannot be added to this assembly because it already is an assembly
                Diagnostic(ErrorCode.ERR_AddModuleAssembly).WithArguments(@"NoMsCorLibRef.mod"),
                // (1,20): error CS0246: The type or namespace name 'IGoo' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IGoo").WithArguments("IGoo"));
        }

        [Fact, WorkItem(544910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544910")]
        public void CS1599ERR_MethodReturnCantBeRefAny()
        {
            var text = @"
using System;
interface I
{
    ArgIterator M(); // 1599
}
class C
{
    public delegate TypedReference Test1(); // 1599

    public RuntimeArgumentHandle Test2() // 1599
    {
        return default(RuntimeArgumentHandle);
    }

    // The native compiler does not catch this one; Roslyn does.
    public static ArgIterator operator +(C c1, C c2) // 1599
    { 
        return default(ArgIterator); 
    }
}
";
            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
// (5,5): error CS1599: The return type of a method, delegate, or function pointer cannot be 'System.ArgIterator'
//     ArgIterator M(); // 1599
Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator"),
// (11,12): error CS1599: The return type of a method, delegate, or function pointer cannot be 'System.RuntimeArgumentHandle'
//     public RuntimeArgumentHandle Test2() // 1599
Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle"),
// (17,19): error CS1599: The return type of a method, delegate, or function pointer cannot be 'System.ArgIterator'
//     public static ArgIterator operator +(C c1, C c2) // 1599
Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator"),
// (9,21): error CS1599: The return type of a method, delegate, or function pointer cannot be 'System.TypedReference'
//     public delegate TypedReference Test1(); // 1599
Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "TypedReference").WithArguments("System.TypedReference")
                );
        }

        [Fact, WorkItem(27463, "https://github.com/dotnet/roslyn/issues/27463")]
        public void CS1599ERR_LocalFunctionReturnCantBeRefAny()
        {
            var text = @"
class C
{
    public void Goo()
    {
        System.TypedReference local1() // 1599
        {
            return default;
        }
        local1();

        System.RuntimeArgumentHandle local2() // 1599
        {
            return default;
        }
        local2();

        System.ArgIterator local3() // 1599
        {
            return default;
        }
        local3();
    }
}
";
            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS1599: The return type of a method, delegate, or function pointer cannot be 'TypedReference'
                //         System.TypedReference local1() // 1599
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(6, 9),
                // (12,9): error CS1599: The return type of a method, delegate, or function pointer cannot be 'RuntimeArgumentHandle'
                //         System.RuntimeArgumentHandle local2() // 1599
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "System.RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(12, 9),
                // (18,9): error CS1599: The return type of a method, delegate, or function pointer cannot be 'ArgIterator'
                //         System.ArgIterator local3() // 1599
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(18, 9));
        }

        [Fact, WorkItem(27463, "https://github.com/dotnet/roslyn/issues/27463")]
        public void CS1599ERR_LocalFunctionReturnCanBeSpan()
        {
            var text = @"
using System;
class C
{
    static void M()
    {
        byte[] bytes = new byte[1];
        Span<byte> local1()
        {
            return new Span<byte>(bytes);
        }
        Span<byte> res = local1();
    }
}
";
            CreateCompilationWithMscorlibAndSpanSrc(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(544910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544910")]
        public void CS1601ERR_MethodArgCantBeRefAny()
        {
            // We've changed the text of the error message from
            // CS1601: Method or delegate parameter cannot be of type 'ref System.TypedReference'
            // to
            // CS1601: Cannot make reference to variable of type 'System.TypedReference'
            // because we use this error message at both the call site and the declaration. 
            // The new wording makes sense in both uses; the former only makes sense at 
            // the declaration site.

            var text = @"
using System;
class MyClass
{
    delegate void D(ref TypedReference t1); // CS1601
    public void Test1(ref TypedReference t2, RuntimeArgumentHandle r3)   // CS1601
    {
        var x = __makeref(r3); // CS1601
    }

    public void Test2(out ArgIterator t4)   // CS1601
    {
        t4 = default(ArgIterator);
    }

    MyClass(ref RuntimeArgumentHandle r5) {} // CS1601
}
";
            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
// (6,23): error CS1601: Cannot make reference to variable of type 'System.TypedReference'
//     public void Test1(ref TypedReference t2, RuntimeArgumentHandle r3)   // CS1601
Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref TypedReference t2").WithArguments("System.TypedReference"),
// (11,23): error CS1601: Cannot make reference to variable of type 'System.ArgIterator'
//     public void Test2(out ArgIterator t4)   // CS1601
Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out ArgIterator t4").WithArguments("System.ArgIterator"),
// (16,13): error CS1601: Cannot make reference to variable of type 'System.RuntimeArgumentHandle'
//     MyClass(ref RuntimeArgumentHandle r5) {} // CS1601
Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref RuntimeArgumentHandle r5").WithArguments("System.RuntimeArgumentHandle"),
// (5,21): error CS1601: Cannot make reference to variable of type 'System.TypedReference'
//     delegate void D(ref TypedReference t1); // CS1601
Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref TypedReference t1").WithArguments("System.TypedReference"),
// (8,17): error CS1601: Cannot make reference to variable of type 'System.RuntimeArgumentHandle'
//         var x = __makeref(r3); // CS1601
Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "__makeref(r3)").WithArguments("System.RuntimeArgumentHandle")
                 );
        }

        [Fact, WorkItem(27463, "https://github.com/dotnet/roslyn/issues/27463")]
        public void CS1599ERR_LocalFunctionParamCantBeRefAny()
        {
            var text = @"
class C
{
    public void Goo()
    {
        {
            System.TypedReference _arg = default;
            void local1(ref System.TypedReference tr) { } // 1601
            local1(ref _arg);

            void local2(in System.TypedReference tr) { } // 1601
            local2(in _arg);

            void local3(out System.TypedReference tr) // 1601
            {
                tr = default;
            }
            local3(out _arg);
        }

        {
            System.ArgIterator _arg = default;
            void local1(ref System.ArgIterator ai) { } // 1601
            local1(ref _arg);

            void local2(in System.ArgIterator ai) { } // 1601
            local2(in _arg);

            void local3(out System.ArgIterator ai) // 1601
            {
                ai = default;
            }
            local3(out _arg);
        }

        {
            System.RuntimeArgumentHandle _arg = default;
            void local1(ref System.RuntimeArgumentHandle ah) { } // 1601
            local1(ref _arg);

            void local2(in System.RuntimeArgumentHandle ah) { } // 1601
            local2(in _arg);

            void local3(out System.RuntimeArgumentHandle ah) // 1601
            {
                ah = default;
            }
            local3(out _arg);
        }
    }
}
";
            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,25): error CS1601: Cannot make reference to variable of type 'TypedReference'
                //             void local1(ref System.TypedReference tr) { } // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref System.TypedReference tr").WithArguments("System.TypedReference").WithLocation(8, 25),
                // (11,25): error CS1601: Cannot make reference to variable of type 'TypedReference'
                //             void local2(in System.TypedReference tr) { } // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "in System.TypedReference tr").WithArguments("System.TypedReference").WithLocation(11, 25),
                // (14,25): error CS1601: Cannot make reference to variable of type 'TypedReference'
                //             void local3(out System.TypedReference tr) // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.TypedReference tr").WithArguments("System.TypedReference").WithLocation(14, 25),
                // (23,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //             void local1(ref System.ArgIterator ai) { } // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref System.ArgIterator ai").WithArguments("System.ArgIterator").WithLocation(23, 25),
                // (26,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //             void local2(in System.ArgIterator ai) { } // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "in System.ArgIterator ai").WithArguments("System.ArgIterator").WithLocation(26, 25),
                // (29,25): error CS1601: Cannot make reference to variable of type 'ArgIterator'
                //             void local3(out System.ArgIterator ai) // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.ArgIterator ai").WithArguments("System.ArgIterator").WithLocation(29, 25),
                // (38,25): error CS1601: Cannot make reference to variable of type 'RuntimeArgumentHandle'
                //             void local1(ref System.RuntimeArgumentHandle ah) { } // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref System.RuntimeArgumentHandle ah").WithArguments("System.RuntimeArgumentHandle").WithLocation(38, 25),
                // (41,25): error CS1601: Cannot make reference to variable of type 'RuntimeArgumentHandle'
                //             void local2(in System.RuntimeArgumentHandle ah) { } // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "in System.RuntimeArgumentHandle ah").WithArguments("System.RuntimeArgumentHandle").WithLocation(41, 25),
                // (44,25): error CS1601: Cannot make reference to variable of type 'RuntimeArgumentHandle'
                //             void local3(out System.RuntimeArgumentHandle ah) // 1601
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out System.RuntimeArgumentHandle ah").WithArguments("System.RuntimeArgumentHandle").WithLocation(44, 25));
        }

        [WorkItem(542003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542003")]
        [Fact]
        public void CS1608ERR_CantUseRequiredAttribute()
        {
            var text = @"using System.Runtime.CompilerServices;

[RequiredAttribute(typeof(object))]
class ClassMain
{
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,2): error CS1608: The Required attribute is not permitted on C# types
                // [RequiredAttribute(typeof(object))]
                Diagnostic(ErrorCode.ERR_CantUseRequiredAttribute, "RequiredAttribute").WithLocation(3, 2));
        }

        [Fact]
        public void CS1614ERR_AmbiguousAttribute()
        {
            var text =
@"using System;
class A : Attribute { }
class AAttribute : Attribute { }
[A][@A][AAttribute] class C { }
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AmbiguousAttribute, Line = 4, Column = 2 });
        }

        [Fact]
        public void CS0214ERR_RequiredInUnsafeContext()
        {
            var text = @"struct C
{
    public fixed int ab[10];   // CS0214
}
";
            var comp = CreateCompilation(text, options: TestOptions.ReleaseDll);
            // (3,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public fixed string ab[10];   // CS0214
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "ab[10]");
        }

        [Fact]
        public void CS1642ERR_FixedNotInStruct()
        {
            var text = @"unsafe class C
{
    fixed int a[10];   // CS1642
}";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (3,15): error CS1642: Fixed size buffer fields may only be members of structs
                //     fixed int a[10];   // CS1642
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "a"));
        }

        [Fact]
        public void CS1663ERR_IllegalFixedType()
        {
            var text = @"unsafe struct C
{
    fixed string ab[10];   // CS1663
}";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (3,11): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                //     fixed string ab[10];   // CS1663
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "string"));
        }

        [WorkItem(545353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545353")]
        [Fact]
        public void CS1664ERR_FixedOverflow()
        {
            // set Allow unsafe code = true
            var text = @"public unsafe struct C
{
    unsafe private fixed long test_1[1073741825];
}";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,38): error CS1664: Fixed size buffer of length '1073741825' and type 'long' is too big
                //     unsafe private fixed long test_1[1073741825];
                Diagnostic(ErrorCode.ERR_FixedOverflow, "1073741825").WithArguments("1073741825", "long"));
        }

        [WorkItem(545353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545353")]
        [Fact]
        public void CS1665ERR_InvalidFixedArraySize()
        {
            var text = @"unsafe struct S
{
    public unsafe fixed int A[0];   // CS1665
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,31): error CS1665: Fixed size buffers must have a length greater than zero
                //     public unsafe fixed int A[0];   // CS1665
                Diagnostic(ErrorCode.ERR_InvalidFixedArraySize, "0"));
        }

        [WorkItem(546922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546922")]
        [Fact]
        public void CS1665ERR_InvalidFixedArraySizeNegative()
        {
            var text = @"unsafe struct S
{
    public unsafe fixed int A[-1];   // CS1665
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_InvalidFixedArraySize, "-1"));
        }

        [Fact()]
        public void CS0443ERR_InvalidFixedArraySizeNotSpecified()
        {
            var text = @"unsafe struct S
{
    public unsafe fixed int A[];   // CS0443
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,31): error CS0443: Syntax error; value expected
                //     public unsafe fixed int A[];   // CS0443
                Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [Fact]
        public void CS1003ERR_InvalidFixedArrayMultipleDimensions()
        {
            var text = @"unsafe struct S
{
    public unsafe fixed int A[2,2];   // CS1003,CS1001
    public unsafe fixed int B[2][2];   // CS1003,CS1001,CS1519
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,33): error CS1002: ; expected
                //     public unsafe fixed int B[2][2];   // CS1003,CS1001,CS1519
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "[").WithLocation(4, 33),
                // (4,33): error CS1031: Type expected
                //     public unsafe fixed int B[2][2];   // CS1003,CS1001,CS1519
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(4, 33),
                // (4,36): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     public unsafe fixed int B[2][2];   // CS1003,CS1001,CS1519
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 36),
                // (3,30): error CS7092: A fixed buffer may only have one dimension.
                //     public unsafe fixed int A[2,2];   // CS1003,CS1001
                Diagnostic(ErrorCode.ERR_FixedBufferTooManyDimensions, "[2,2]").WithLocation(3, 30));
        }

        [Fact]
        public void CS1642ERR_InvalidFixedBufferNested()
        {
            var text = @"unsafe struct S
{    
        unsafe class Err_OnlyOnInstanceInStructure
        {
            public fixed bool _bufferInner[10]; //Valid
        }
        public fixed bool _bufferOuter[10]; // error CS1642: Fixed size buffer fields may only be members of structs
 }
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,31): error CS1642: Fixed size buffer fields may only be members of structs
                //             public fixed bool _bufferInner[10]; //Valid
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "_bufferInner"));
        }

        [Fact]
        public void CS1642ERR_InvalidFixedBufferNested_2()
        {
            var text = @"unsafe class S
{    
        unsafe struct Err_OnlyOnInstanceInStructure
        {
            public fixed bool _bufferInner[10]; // Valid
        }
        public fixed bool _bufferOuter[10]; // error CS1642: Fixed size buffer fields may only be members of structs
 }
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,31): error CS1642: Fixed size buffer fields may only be members of structs
                //             public fixed bool _bufferOuter[10]; //Valid
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "_bufferOuter"));
        }

        [Fact]
        public void CS0133ERR_InvalidFixedBufferCountFromField()
        {
            var text = @"unsafe struct @s
    {
        public static int var1 = 10;
        public fixed bool _Type3[var1]; // error CS0133: The expression being assigned to '<Type>' must be constant
    }
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,34): error CS0133: The expression being assigned to 's._Type3' must be constant
                //         public fixed bool _Type3[var1]; // error CS0133: The expression being assigned to '<Type>' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "var1").WithArguments("s._Type3"));
        }

        [Fact]
        public void CS1663ERR_InvalidFixedBufferUsingGenericType()
        {
            var text = @"unsafe struct Err_FixedBufferDeclarationUsingGeneric<@t>
    {
        public fixed t _Type1[10]; // error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
    }
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,22): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                //         public fixed t _Type1[10]; // error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "t"));
        }

        [Fact]
        public void CS0029ERR_InvalidFixedBufferNonValidTypes()
        {
            var text = @"unsafe struct @s
    {
        public fixed int _Type1[1.2]; // error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
        public fixed int _Type2[true]; // error CS00029
        public fixed int _Type3[""true""]; // error CS00029
        public fixed int _Type4[System.Convert.ToInt32(@""1"")]; // error CS0133
    }
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,33): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         public fixed int _Type1[1.2]; // error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.2").WithArguments("double", "int"),
                // (4,33): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                //         public fixed int _Type2[true]; // error CS00029
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "true").WithArguments("bool", "int"),
                // (5,33): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         public fixed int _Type3["true"]; // error CS00029
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""true""").WithArguments("string", "int"),
                // (6,33): error CS0133: The expression being assigned to 's._Type4' must be constant
                //         public fixed int _Type4[System.Convert.ToInt32(@"1")]; // error CS0133
                Diagnostic(ErrorCode.ERR_NotConstantExpression, @"System.Convert.ToInt32(@""1"")").WithArguments("s._Type4"));
        }

        [Fact]
        public void CS0029ERR_InvalidFixedBufferNonValidTypesUserDefinedTypes()
        {
            var text = @"unsafe struct @s
    {
        public fixed goo _bufferGoo[10]; // error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
        public fixed bar _bufferBar[10]; // error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
    }

    struct @goo
    {
        public int ABC;
    }

    class @bar
    {
        public bool ABC = true;
    }
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (3,22): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                //         public fixed goo _bufferGoo[10]; // error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "goo"),
                // (4,22): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                //         public fixed bar _bufferBar[10]; // error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                Diagnostic(ErrorCode.ERR_IllegalFixedType, "bar"),
                // (9,20): warning CS0649: Field 'goo.ABC' is never assigned to, and will always have its default value 0
                //         public int ABC;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "ABC").WithArguments("goo.ABC", "0"));
        }

        [Fact]
        public void C1666ERR_InvalidFixedBufferInUnfixedContext()
        {
            var text = @"
unsafe struct @s
{
    private fixed ushort _e_res[4]; 
    void Error_UsingFixedBuffersWithThis()
    {
        ushort c = this._e_res;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,24): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //             ushort c = this._e_res;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "this._e_res"));
        }

        [Fact]
        public void CS0029ERR_InvalidFixedBufferUsageInLocal()
        {
            //Some additional errors generated but the key ones from native are here.
            var text = @"unsafe struct @s
    {        
    //Use as local rather than field with unsafe on method
    // Incorrect usage of fixed buffers in method bodies try to use as a local 
    static unsafe void Error_UseAsLocal()
    {                
        //Invalid In Use as Local
        fixed bool _buffer[2]; // error CS1001: Identifier expected        
     }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,15): error CS1003: Syntax error, '(' expected
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.ERR_SyntaxError, "bool").WithArguments("("),
                // (8,27): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.ERR_CStyleArray, "[2]"),
                // (8,28): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "2"),
                // (8,30): error CS1026: ) expected
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";"),
                // (8,30): warning CS0642: Possible mistaken empty statement
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"),
                // (8,20): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.ERR_BadFixedInitType, "_buffer[2]"),
                // (8,20): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //         fixed bool _buffer[2]; // error CS1001: Identifier expected        
                Diagnostic(ErrorCode.ERR_FixedMustInit, "_buffer[2]"));
        }

        [Fact()]
        public void CS1667ERR_AttributeNotOnAccessor()
        {
            var text = @"using System;

public class C
{
    private int i;
    public int ObsoleteProperty
    {
        [Obsolete]  // CS1667
        get { return i; }
        [System.Diagnostics.Conditional(""Bernard"")]
        set { i = value; }
    }

    public static void Main()
    {
    }
}
";
            var comp = CreateCompilation(text).VerifyDiagnostics(
    // (10,10): error CS1667: Attribute 'System.Diagnostics.Conditional' is not valid on property or event accessors. It is only valid on 'class, method' declarations.
    //         [System.Diagnostics.Conditional("Bernard")]
    Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "System.Diagnostics.Conditional").WithArguments("System.Diagnostics.Conditional", "class, method")
                );
        }

        [Fact]
        public void CS1689ERR_ConditionalOnNonAttributeClass()
        {
            var text = @"[System.Diagnostics.Conditional(""A"")]   // CS1689
class MyClass {}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,2): error CS1689: Attribute 'System.Diagnostics.Conditional' is only valid on methods or attribute classes
                // [System.Diagnostics.Conditional("A")]   // CS1689
                Diagnostic(ErrorCode.ERR_ConditionalOnNonAttributeClass, @"System.Diagnostics.Conditional(""A"")").WithArguments("System.Diagnostics.Conditional").WithLocation(1, 2));
        }

        // CS17ERR_DuplicateImport:       See ReferenceManagerTests.CS17ERR_DuplicateImport
        // CS1704ERR_DuplicateImportSimple: See ReferenceManagerTests.CS1704ERR_DuplicateImportSimple

        [Fact(Skip = "530901"), WorkItem(530901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530901")]
        public void CS1705ERR_AssemblyMatchBadVersion()
        {
            // compile with: /target:library /out:c:\\cs1705.dll /keyfile:mykey.snk
            var text1 = @"[assembly:System.Reflection.AssemblyVersion(""1.0"")]
public class A 
{
   public void M1() {}
   public class N1 {}
   public void M2() {}
   public class N2 {}
}

public class C1 {}
public class C2 {}
";
            var tree1 = SyntaxFactory.ParseCompilationUnit(text1);

            // compile with: /target:library /out:cs1705.dll /keyfile:mykey.snk
            var text2 = @"using System.Reflection;
[assembly:AssemblyVersion(""2.0"")]
public class A 
{
   public void M2() {}
   public class N2 {}
   public void M1() {}
   public class N1 {}
}

public class C2 {}
public class C1 {}
";
            var tree2 = SyntaxFactory.ParseCompilationUnit(text2);

            // compile with: /target:library /r:A2=c:\\CS1705.dll /r:A1=CS1705.dll
            var text3 = @"extern alias A1;
extern alias A2;
using a1 = A1::A;
using a2 = A2::A;
using n1 = A1::A.N1;
using n2 = A2::A.N2;
public class Ref 
{
   public static a1 A1() { return new a1(); }
   public static a2 A2() { return new a2(); }
   public static A1::C1 M1() { return new A1::C1(); }
   public static A2::C2 M2() { return new A2::C2(); }
   public static n1 N1() { return new a1.N1(); }
   public static n2 N2() { return new a2.N2(); }
}
";
            var tree3 = SyntaxFactory.ParseCompilationUnit(text3);

            // compile with: /reference:c:\\CS1705.dll /reference:CS1705_c.dll
            var text = @"class Tester 
{
   static void Main() 
   {
      Ref.A1().M1();
      Ref.A2().M2();
   }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AssemblyMatchBadVersion, Line = 2, Column = 12 });
        }

        [Fact]
        public void CS1715ERR_CantChangeTypeOnOverride()
        {
            var text = @"abstract public class Base
{
    abstract public int myProperty
    {
        get;
        set;
    }
}

public class Derived : Base
{
    int myField;
    public override double myProperty  // CS1715
    {
        get { return myField; }
        set { myField= 1; }
    }

    public static void Main()
    {
    }
}
";
            // The set accessor has the wrong parameter type so is not implemented.
            // The override get accessor has the right signature (no parameters) so is implemented, though with the wrong return type.
            CreateCompilation(text).VerifyDiagnostics(
                // (10,14): error CS0534: 'Derived' does not implement inherited abstract member 'Base.myProperty.set'
                // public class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.myProperty.set").WithLocation(10, 14),
                // (13,28): error CS1715: 'Derived.myProperty': type must be 'int' to match overridden member 'Base.myProperty'
                //     public override double myProperty  // CS1715
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "myProperty").WithArguments("Derived.myProperty", "Base.myProperty", "int").WithLocation(13, 28)
                );
        }

        [Fact]
        public void CS1716ERR_DoNotUseFixedBufferAttr()
        {
            var text = @"
using System.Runtime.CompilerServices;

public struct UnsafeStruct
{
    [FixedBuffer(typeof(int), 4)]  // CS1716
    unsafe public int aField;
}

public class TestUnsafe
{
    static void Main()
    {
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,6): error CS1716: Do not use 'System.Runtime.CompilerServices.FixedBuffer' attribute. Use the 'fixed' field modifier instead.
                //     [FixedBuffer(typeof(int), 4)]  // CS1716
                Diagnostic(ErrorCode.ERR_DoNotUseFixedBufferAttr, "FixedBuffer").WithLocation(6, 6));
        }

        [Fact]
        public void CS1721ERR_NoMultipleInheritance01()
        {
            var text = @"namespace NS
{
    public class A { }
    public class B { }
    public class C : B { }
    public class MyClass : A, A { }   // CS1721
    public class MyClass2 : A, B { }   // CS1721
    public class MyClass3 : C, A { }   // CS1721
}
";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoMultipleInheritance, Line = 6, Column = 31 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoMultipleInheritance, Line = 7, Column = 32 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoMultipleInheritance, Line = 8, Column = 32 });
        }

        [Fact]
        public void CS1722ERR_BaseClassMustBeFirst01()
        {
            var text = @"namespace NS
{
    public class A { }
    interface I { }
    interface IGoo<T> : I  { }

    public class MyClass : I, A { }   // CS1722
    public class MyClass2 : A, I { }   // OK

    class Test
    {
        class C : IGoo<int>, A , I { } // CS1722
    }
}";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BaseClassMustBeFirst, Line = 7, Column = 31 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_BaseClassMustBeFirst, Line = 12, Column = 30 });
        }

        [Fact(), WorkItem(530393, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530393")]
        public void CS1724ERR_InvalidDefaultCharSetValue()
        {
            var text = @"
using System.Runtime.InteropServices;
[module: DefaultCharSetAttribute((CharSet)42)]   // CS1724
class C
{
    [DllImport(""F.Dll"")]
    extern static void FW1Named();
    static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,34): error CS0591: Invalid value for argument to 'DefaultCharSetAttribute' attribute
                // [module: DefaultCharSetAttribute((CharSet)42)]   // CS1724
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(CharSet)42").WithArguments("DefaultCharSetAttribute")
                );
        }

        [Fact, WorkItem(1116455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1116455")]
        public void CS1725ERR_FriendAssemblyBadArgs()
        {
            var text = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""Test, Version=*"")]                    // ok
[assembly: InternalsVisibleTo(""Test, PublicKeyToken=*"")]             // ok
[assembly: InternalsVisibleTo(""Test, Culture=*"")]                    // ok
[assembly: InternalsVisibleTo(""Test, Retargetable=*"")]               // ok
[assembly: InternalsVisibleTo(""Test, ContentType=*"")]                // ok
[assembly: InternalsVisibleTo(""Test, Version=."")]                    // ok
[assembly: InternalsVisibleTo(""Test, Version=.."")]                   // ok
[assembly: InternalsVisibleTo(""Test, Version=..."")]                  // ok
                                                                       
[assembly: InternalsVisibleTo(""Test, Version=1"")]                    // error
[assembly: InternalsVisibleTo(""Test, Version=1.*"")]                  // error
[assembly: InternalsVisibleTo(""Test, Version=1.1.*"")]                // error
[assembly: InternalsVisibleTo(""Test, Version=1.1.1.*"")]              // error
[assembly: InternalsVisibleTo(""Test, ProcessorArchitecture=MSIL"")]   // error
[assembly: InternalsVisibleTo(""Test, CuLTure=EN"")]                   // error
[assembly: InternalsVisibleTo(""Test, PublicKeyToken=null"")]          // ok
";
            // Tested against Dev12
            CreateCompilation(text).VerifyDiagnostics(
                // (12,12): error CS1725: Friend assembly reference 'Test, Version=1' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
                Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""Test, Version=1"")").WithArguments("Test, Version=1").WithLocation(12, 12),
                // (13,12): error CS1725: Friend assembly reference 'Test, Version=1.*' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
                Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""Test, Version=1.*"")").WithArguments("Test, Version=1.*").WithLocation(13, 12),
                // (14,12): error CS1725: Friend assembly reference 'Test, Version=1.1.*' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
                Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""Test, Version=1.1.*"")").WithArguments("Test, Version=1.1.*").WithLocation(14, 12),
                // (15,12): error CS1725: Friend assembly reference 'Test, Version=1.1.1.*' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
                Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""Test, Version=1.1.1.*"")").WithArguments("Test, Version=1.1.1.*").WithLocation(15, 12),
                // (16,12): error CS1725: Friend assembly reference 'Test, ProcessorArchitecture=MSIL' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
                Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""Test, ProcessorArchitecture=MSIL"")").WithArguments("Test, ProcessorArchitecture=MSIL").WithLocation(16, 12),
                // (17,12): error CS1725: Friend assembly reference 'Test, CuLTure=EN' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
                Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""Test, CuLTure=EN"")").WithArguments("Test, CuLTure=EN").WithLocation(17, 12));
        }

        [Fact]
        public void CS1736ERR_DefaultValueMustBeConstant01()
        {
            var text = @"class A 
{
    static int Age;
    public void Goo(int Para1 = Age)
    { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,33): error CS1736: Default parameter value for 'Para1' must be a compile-time constant
                //     public void Goo(int Para1 = Age)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Age").WithArguments("Para1"),
                // (3,16): warning CS0169: The field 'A.Age' is never used
                //     static int Age;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Age").WithArguments("A.Age")
                );
        }

        [Fact]
        public void CS1736ERR_DefaultValueMustBeConstant02()
        {
            var source =
@"class C
{
    object this[object x, object y = new C()]
    {
        get { return null; }
        set { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,38): error CS1736: Default parameter value for 'y' must be a compile-time constant
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new C()").WithArguments("y").WithLocation(3, 38));
        }

        [Fact, WorkItem(542401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542401")]
        public void CS1736ERR_DefaultValueMustBeConstant_1()
        {
            var text = @"
class NamedExample
{
    static int y = 1;
    static void Main(string[] args)
    {
    }

    int CalculateBMI(int weight, int height = y)
    {
        return (weight * 7) / (height * height);
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,47): error CS1736: Default parameter value for 'height' must be a compile-time constant
                //     int CalculateBMI(int weight, int height = y)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "y").WithArguments("height"),
                // (4,16): warning CS0414: The field 'NamedExample.y' is assigned but its value is never used
                //     static int y = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "y").WithArguments("NamedExample.y")
                );
        }

        [Fact]
        public void CS1737ERR_DefaultValueBeforeRequiredValue()
        {
            var text = @"class A 
{
    public void Goo(int Para1 = 1, int Para2)
    { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = 1737, Line = 3, Column = 45 });
        }

        [Fact]
        public void CS1741ERR_RefOutDefaultValue()
        {
            var text = @"class A 
{
    public void Goo(ref int Para1 = 1)
    { }
    public void Goo1(out int Para2 = 1)
    {
        Para2 = 2;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = 1741, Line = 3, Column = 21 },
                new ErrorDescription { Code = 1741, Line = 5, Column = 22 });
        }

        [Fact]
        public void CS17ERR_DefaultValueForExtensionParameter()
        {
            var text =
@"static class C
{
    static void M1(object o = null) { }
    static void M2(this object o = null) { }
    static void M3(object o, this int i = 0) { }
}";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (4,20): error CS17: Cannot specify a default value for the 'this' parameter
                Diagnostic(ErrorCode.ERR_DefaultValueForExtensionParameter, "this").WithLocation(4, 20),
                // (5,30): error CS1100: Method 'M3' has a parameter modifier 'this' which is not on the first parameter
                Diagnostic(ErrorCode.ERR_BadThisParam, "this").WithArguments("M3").WithLocation(5, 30));
        }

        [Fact]
        public void CS1745ERR_DefaultValueUsedWithAttributes()
        {
            var text = @"
using System.Runtime.InteropServices;
class A 
{
    public void goo([OptionalAttribute]int p = 1)
    { }
    public static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,22): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                //     public void goo([OptionalAttribute]int p = 1)
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "OptionalAttribute")
                );
        }

        [Fact]
        public void CS1747ERR_NoPIAAssemblyMissingAttribute()
        {
            //csc program.cs /l:"C:\MissingPIAAttributes.dll
            var text = @"public class Test
{
    static int Main(string[] args)
    {
        return 1;
    }
}";
            var ref1 = TestReferences.SymbolsTests.NoPia.Microsoft.VisualStudio.MissingPIAAttributes.WithEmbedInteropTypes(true);

            CreateCompilation(text, references: new[] { ref1 }).VerifyDiagnostics(
                // error CS1747: Cannot embed interop types from assembly 'MissingPIAAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing the 'System.Runtime.InteropServices.GuidAttribute' attribute.
                Diagnostic(ErrorCode.ERR_NoPIAAssemblyMissingAttribute).WithArguments("MissingPIAAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Runtime.InteropServices.GuidAttribute").WithLocation(1, 1),
                // error CS1759: Cannot embed interop types from assembly 'MissingPIAAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing either the 'System.Runtime.InteropServices.ImportedFromTypeLibAttribute' attribute or the 'System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute' attribute.
                Diagnostic(ErrorCode.ERR_NoPIAAssemblyMissingAttributes).WithArguments("MissingPIAAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Runtime.InteropServices.ImportedFromTypeLibAttribute", "System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute").WithLocation(1, 1));
        }

        [WorkItem(620366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620366")]
        [Fact]
        public void CS1748ERR_NoCanonicalView()
        {
            var textdll = @"
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""NoPiaTest"")]
[assembly: Guid(""A55E0B17-2558-447D-B786-84682CBEF136"")]
[assembly: BestFitMapping(false)]
[ComImport, Guid(""E245C65D-2448-447A-B786-64682CBEF133"")]
[TypeIdentifier(""E245C65D-2448-447A-B786-64682CBEF133"", ""IMyInterface"")]
public interface IMyInterface
{
    void Method(int n);
}
public delegate void DelegateWithInterface(IMyInterface value);
public delegate void DelegateWithInterfaceArray(IMyInterface[] ary);
public delegate IMyInterface DelegateRetInterface();
public delegate DelegateRetInterface DelegateRetDelegate(DelegateRetInterface d);
";
            var text = @"
class Test
{
    static void Main()
    {
    }
    public static void MyDelegate02(IMyInterface[] ary) { }
}
";
            var comp1 = CreateCompilation(textdll);
            var ref1 = new CSharpCompilationReference(comp1);
            CreateCompilation(text, references: new MetadataReference[] { ref1 }).VerifyDiagnostics(
                // (77): error CS0246: The type or namespace name 'IMyInterface' could not be found (are you missing a using directive or an assembly reference?)
                //     public static void MyDelegate02(IMyInterface[] ary) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IMyInterface").WithArguments("IMyInterface")
                );
        }

        [Fact]
        public void CS1750ERR_NoConversionForDefaultParam()
        {
            var text = @"public class Generator
{
    public void Show<T>(string msg = ""Number"", T value = null)
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = 1750, Line = 3, Column = 50 });
        }

        [Fact]
        public void CS1751ERR_DefaultValueForParamsParameter()
        {
            // The native compiler only produces one error here -- that
            // the default value on "params" is illegal. However it 
            // seems reasonable to produce one error for each bad param. 
            var text = @"class MyClass
{
    public void M7(int i = null, params string[] values = ""test"") { }
    static void Main() { }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                // 'i': A value of type '<null>' cannot be used as a default parameter because there are no standard conversions to type 'int'
                new ErrorDescription { Code = 1750, Line = 3, Column = 24 },
                // 'params': error CS1751: Cannot specify a default value for a parameter collection
                new ErrorDescription { Code = 1751, Line = 3, Column = 34 });
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void CS1754ERR_NoPIANestedType()
        {
            var textdll = @"using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""NoPiaTestLib"")]
[assembly: Guid(""A7721B07-2448-447A-BA36-64682CBEF136"")]
namespace NS
{
    public struct MyClass
    {
        public struct NestedClass
        {
            public string Name;
        }
    }
}
";
            var text = @"public class Test
{
    public static void Main()
    {
        var S = new NS.MyClass.NestedClass();
        System.Console.Write(S);
    }
}
";
            var comp = CreateCompilation(textdll);
            var ref1 = new CSharpCompilationReference(comp, embedInteropTypes: true);
            CreateCompilation(text, new[] { ref1 }).VerifyDiagnostics(
               Diagnostic(ErrorCode.ERR_NoPIANestedType, "NestedClass").WithArguments("NS.MyClass.NestedClass"));
        }

        [Fact()]
        public void CS1755ERR_InvalidTypeIdentifierConstructor()
        {
            var textdll = @"using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""NoPiaTestLib"")]
[assembly: Guid(""A7721B07-2448-447A-BA36-64682CBEF136"")]
namespace NS
{
    [TypeIdentifier(""Goo2"", ""Bar2"")]
    public delegate void MyDel();
}
";
            var text = @"
public class Test
{
    event NS.MyDel e;
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilation(textdll);
            var ref1 = new CSharpCompilationReference(comp);
            CreateCompilation(text, new[] { ref1 }).VerifyDiagnostics(
                // (4,14): error CS0234: The type or namespace name 'MyDel' does not exist in the namespace 'NS' (are you missing an assembly reference?)
                //     event NS.MyDel e;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "MyDel").WithArguments("MyDel", "NS"),
                // (4,20): warning CS0067: The event 'Test.e' is never used
                //     event NS.MyDel e;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e").WithArguments("Test.e"));
            //var comp1 = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(new List<string>() { text }, new List<MetadataReference>() { ref1 },
            //    new ErrorDescription { Code = 1755, Line = 4, Column = 14 });
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void CS1757ERR_InteropStructContainsMethods()
        {
            var textdll = @"using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""NoPiaTestLib"")]
[assembly: Guid(""A7721B07-2448-447A-BA36-64682CBEF136"")]
namespace NS
{
    public struct MyStruct
    {
        private int _age;
        public string Name;
    }
}
";
            var text = @"public class Test
{
    public static void Main()
    {
        NS.MyStruct S = new NS.MyStruct();
        System.Console.Write(S);
    }
}
";
            var comp = CreateCompilation(textdll);
            var ref1 = new CSharpCompilationReference(comp, embedInteropTypes: true);
            var comp1 = CreateCompilation(text, new[] { ref1 });
            comp1.VerifyEmitDiagnostics(
                // (5,24): error CS1757: Embedded interop struct 'NS.MyStruct' can contain only public instance fields.
                //         NS.MyStruct S = new NS.MyStruct();
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "new NS.MyStruct()").WithArguments("NS.MyStruct"));
        }

        [Fact]
        public void CS1754ERR_NoPIANestedType_2()
        {
            //vbc /t:library PIA.vb
            //csc /l:PIA.dll Program.cs
            var textdll = @"Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib(""GeneralPIA.dll"")> 
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
<ComImport()>
Public Interface INestedInterface
    <Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
    <ComImport()>
    Interface InnerInterface
    End Interface
End Interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
<ComImport()>
Public Interface INestedClass
    Class InnerClass
    End Class
End Interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
<ComImport()>
Public Interface INestedStructure
    Structure InnerStructure
    End Structure
End Interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
<ComImport()>
Public Interface INestedEnum
    Enum InnerEnum
        Value1
    End Enum
End Interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
<ComImport()>
Public Interface INestedDelegate
    Delegate Sub InnerDelegate()
End Interface

Public Structure NestedInterface
    <Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>
    <ComImport()>
    Interface InnerInterface
    End Interface
End Structure

Public Structure NestedClass
    Class InnerClass
    End Class
End Structure

Public Structure NestedStructure
    Structure InnerStructure
    End Structure
End Structure

Public Structure NestedEnum
    Enum InnerEnum
        Value1
    End Enum
End Structure

Public Structure NestedDelegate
    Delegate Sub InnerDelegate()
End Structure";
            var text = @"public class Program
{
    public static void Main()
        {
        INestedInterface.InnerInterface s1 = null;
        INestedStructure.InnerStructure s3 = default(INestedStructure.InnerStructure);
        INestedEnum.InnerEnum s4 = default(INestedEnum.InnerEnum);
        INestedDelegate.InnerDelegate s5 = null;
    }
}";
            var vbcomp = VisualBasic.VisualBasicCompilation.Create(
                "Test",
                new[] { VisualBasic.VisualBasicSyntaxTree.ParseText(textdll) },
                new[] { MscorlibRef_v4_0_30316_17626 },
                new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var ref1 = vbcomp.EmitToImageReference(embedInteropTypes: true);

            CreateCompilation(text, new[] { ref1 }).VerifyDiagnostics(
                // (5,26): error CS1754: Type 'INestedInterface.InnerInterface' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         INestedInterface.InnerInterface s1 = null;
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "InnerInterface").WithArguments("INestedInterface.InnerInterface"),
                // (6,26): error CS1754: Type 'INestedStructure.InnerStructure' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         INestedStructure.InnerStructure s3 = default(INestedStructure.InnerStructure);
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "InnerStructure").WithArguments("INestedStructure.InnerStructure"),
                // (6,71): error CS1754: Type 'INestedStructure.InnerStructure' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         INestedStructure.InnerStructure s3 = default(INestedStructure.InnerStructure);
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "InnerStructure").WithArguments("INestedStructure.InnerStructure"),
                // (7,21): error CS1754: Type 'INestedEnum.InnerEnum' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         INestedEnum.InnerEnum s4 = default(INestedEnum.InnerEnum);
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "InnerEnum").WithArguments("INestedEnum.InnerEnum"),
                // (7,56): error CS1754: Type 'INestedEnum.InnerEnum' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         INestedEnum.InnerEnum s4 = default(INestedEnum.InnerEnum);
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "InnerEnum").WithArguments("INestedEnum.InnerEnum"),
                // (8,25): error CS1754: Type 'INestedDelegate.InnerDelegate' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         INestedDelegate.InnerDelegate s5 = null;
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "InnerDelegate").WithArguments("INestedDelegate.InnerDelegate"),
                // (5,41): warning CS0219: The variable 's1' is assigned but its value is never used
                //         INestedInterface.InnerInterface s1 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1"),
                // (6,41): warning CS0219: The variable 's3' is assigned but its value is never used
                //         INestedStructure.InnerStructure s3 = default(INestedStructure.InnerStructure);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s3").WithArguments("s3"),
                // (71): warning CS0219: The variable 's4' is assigned but its value is never used
                //         INestedEnum.InnerEnum s4 = default(INestedEnum.InnerEnum);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s4").WithArguments("s4"),
                // (8,39): warning CS0219: The variable 's5' is assigned but its value is never used
                //         INestedDelegate.InnerDelegate s5 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s5").WithArguments("s5"));
        }

        [Fact]
        public void CS17ERR_NotNullRefDefaultParameter()
        {
            var text = @"
public static class ErrorCode 
{ 
  // We do not allow constant conversions from string to object in a default parameter initializer
  static void M1(object x = ""hello"") {}
  // We do not allow boxing conversions to object in a default parameter initializer
  static void M2(System.ValueType y = 123) {}
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                // (5,25): error CS17: 'x' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                new ErrorDescription { Code = 1763, Line = 5, Column = 25 },
                // (75): error CS17: 'y' is of type 'System.ValueType'. A default parameter value of a reference type other than string can only be initialized with null
                new ErrorDescription { Code = 1763, Line = 7, Column = 35 });
        }

        [WorkItem(619266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619266")]
        [Fact(Skip = "619266")]
        public void CS1768ERR_GenericsUsedInNoPIAType()
        {
            // add dll and make it embed
            var textdll = @"using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""NoPiaTestLib"")]
[assembly: Guid(""A7721B07-2448-447A-BA36-64682CBEF136"")]
namespace ClassLibrary3
{
    [ComImport, Guid(""b2496f7a-5d40-4abe-ad14-462f257a8ed5"")]
    public interface IGoo
    {
        IBar<string> goo();
    }
    [ComImport, Guid(""b2496f7a-5d40-4abe-ad14-462f257a8ed6"")]
    public interface IBar<T>
    {
        List<IGoo> GetList();
    }

}
";
            var text = @"
using System.Collections.Generic;
using ClassLibrary3;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            IGoo x = (IGoo)new object();
        }
    }
    class goo : IBar<string>, IGoo
    {
        public List<string> GetList()
        {
            throw new NotImplementedException();
        }

        List<IGoo> IBar<string>.GetList()
        {
            throw new NotImplementedException();
        }
    }
  
}
";
            var comp = CreateCompilation(textdll);
            var ref1 = new CSharpCompilationReference(comp, embedInteropTypes: true);
            CreateCompilation(text, new[] { ref1 }).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType));
        }

        [Fact, WorkItem(6186, "https://github.com/dotnet/roslyn/issues/6186")]
        public void CS1770ERR_NoConversionForNubDefaultParam()
        {
            var text = @"using System;
class MyClass
{
    public enum E { None }
    
    // No error:
    public void Goo1(int? x = default(int)) { }
    public void Goo2(E? x = default(E)) { }
    public void Goo3(DateTime? x = default(DateTime?)) { }
    public void Goo4(DateTime? x = new DateTime?()) { }

    // Error:
    public void Goo11(DateTime? x = default(DateTime)) { }
    public void Goo12(DateTime? x = new DateTime()) { }
}";
            var comp = CreateCompilation(text);

            comp.VerifyDiagnostics(
    // (13,33): error CS1770: A value of type 'DateTime' cannot be used as default parameter for nullable parameter 'x' because 'DateTime' is not a simple type
    //     public void Goo11(DateTime? x = default(DateTime)) { }
    Diagnostic(ErrorCode.ERR_NoConversionForNubDefaultParam, "x").WithArguments("System.DateTime", "x").WithLocation(13, 33),
    // (14,33): error CS1770: A value of type 'DateTime' cannot be used as default parameter for nullable parameter 'x' because 'DateTime' is not a simple type
    //     public void Goo12(DateTime? x = new DateTime()) { }
    Diagnostic(ErrorCode.ERR_NoConversionForNubDefaultParam, "x").WithArguments("System.DateTime", "x").WithLocation(14, 33)
                );
        }

        [Fact]
        public void CS1908ERR_DefaultValueTypeMustMatch()
        {
            var text = @"using System.Runtime.InteropServices;
public interface ISomeInterface
{
    void Bad([Optional] [DefaultParameterValue(""true"")] bool b);   // CS1908
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,26): error CS1908: The type of the argument to the DefaultValue attribute must match the parameter type
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"));
        }

        // Dev10 reports CS1909: The DefaultValue attribute is not applicable on parameters of type '{0}'.
        // for parameters of type System.Type or array even though there is no reason why null couldn't be specified in DPV.
        // We report CS1910 if DPV has an argument of type System.Type or array like Dev10 does except for we do so instead 
        // of CS1909 when non-null is passed.

        [Fact]
        public void CS1909ERR_DefaultValueBadValueType_Array_NoError()
        {
            var text = @"using System.Runtime.InteropServices;
public interface ISomeInterface
{
    void Test1([DefaultParameterValue(null)]int[] arr1);
}
";
            // Dev10 reports CS1909, we don't
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS1910ERR_DefaultValueBadValueType_Array()
        {
            var text = @"using System.Runtime.InteropServices;
public interface ISomeInterface
{
    void Test1([DefaultParameterValue(new int[] { 1, 2 })]object a);   
    void Test2([DefaultParameterValue(new int[] { 1, 2 })]int[] a);   
    void Test3([DefaultParameterValue(new int[0])]int[] a);   
}
";
            // CS1910
            CreateCompilation(text).VerifyDiagnostics(
                // (4,17): error CS1910: Argument of type 'int[]' is not applicable for the DefaultValue attribute
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("int[]"),
                // (5,17): error CS1910: Argument of type 'int[]' is not applicable for the DefaultValue attribute
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("int[]"),
                // (6,17): error CS1910: Argument of type 'int[]' is not applicable for the DefaultValue attribute
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("int[]"));
        }

        [Fact]
        public void CS1909ERR_DefaultValueBadValueType_Type_NoError()
        {
            var text = @"using System.Runtime.InteropServices;
public interface ISomeInterface
{
    void Test1([DefaultParameterValue(null)]System.Type t);
}
";
            // Dev10 reports CS1909, we don't
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS1910ERR_DefaultValueBadValue_Generics()
        {
            var text = @"using System.Runtime.InteropServices;
public class C { }

public interface ISomeInterface
{
    void Test1<T>([DefaultParameterValue(null)]T t);                  // error
    void Test2<T>([DefaultParameterValue(null)]T t) where T : C;      // OK
    void Test3<T>([DefaultParameterValue(null)]T t) where T : class;  // OK
    void Test4<T>([DefaultParameterValue(null)]T t) where T : struct; // error
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,20): error CS1908: The type of the argument to the DefaultValue attribute must match the parameter type
                //     void Test1<T>([DefaultParameterValue(null)]T t);                  // error
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"),
                // (9,20): error CS1908: The type of the argument to the DefaultValue attribute must match the parameter type
                //     void Test4<T>([DefaultParameterValue(null)]T t) where T : struct; // error
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValue"));
        }

        [Fact]
        public void CS1910ERR_DefaultValueBadValueType_Type1()
        {
            var text = @"using System.Runtime.InteropServices;
public interface ISomeInterface
{
    void Test1([DefaultParameterValue(typeof(int))]object t);   // CS1910
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,17): error CS1910: Argument of type 'System.Type' is not applicable for the DefaultValue attribute
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("System.Type"));
        }

        [Fact]
        public void CS1910ERR_DefaultValueBadValueType_Type2()
        {
            var text = @"using System.Runtime.InteropServices;
public interface ISomeInterface
{
    void Test1([DefaultParameterValue(typeof(int))]System.Type t);   // CS1910
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,17): error CS1910: Argument of type 'System.Type' is not applicable for the DefaultValue attribute
                Diagnostic(ErrorCode.ERR_DefaultValueBadValueType, "DefaultParameterValue").WithArguments("System.Type"));
        }

        [Fact]
        public void CS1961ERR_UnexpectedVariance()
        {
            var text = @"interface Goo<out T> 
{
    T Bar();
    void Baz(T t);
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,14): error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'Goo<T>.Baz(T)'. 'T' is covariant.
                //     void Baz(T t);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T").WithArguments("Goo<T>.Baz(T)", "T", "covariant", "contravariantly").WithLocation(4, 14));
        }

        [Fact]
        public void CS1965ERR_DeriveFromDynamic()
        {
            var text = @"public class ErrorCode : dynamic
{  
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (1,26): error CS1965: 'ErrorCode': cannot derive from the dynamic type
                Diagnostic(ErrorCode.ERR_DeriveFromDynamic, "dynamic").WithArguments("ErrorCode"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552740")]
        public void CS1966ERR_DeriveFromConstructedDynamic()
        {
            var text = @"
interface I<T> { }


class C<T>
{
    public enum D { }
}

class E1 : I<dynamic> {}
class E2 : I<C<dynamic>.D*[]> {}
";
            CreateCompilationWithMscorlib40AndSystemCore(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (11,12): error CS1966: 'E2': cannot implement a dynamic interface 'I<C<dynamic>.D*[]>'
                // class E2 : I<C<dynamic>.D*[]> {}
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<C<dynamic>.D*[]>").WithArguments("E2", "I<C<dynamic>.D*[]>"),
                // (10,12): error CS1966: 'E1': cannot implement a dynamic interface 'I<dynamic>'
                // class E1 : I<dynamic> {}
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<dynamic>").WithArguments("E1", "I<dynamic>"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552740")]
        public void CS1966ERR_DeriveFromConstructedDynamic_UnsafeContext()
        {
            var text = @"
interface I<T> { }

class C<T>
{
    public enum D { }
}

class E1 : I<dynamic> {}
unsafe class E2 : I<C<dynamic>.D*[]> {}
";
            CreateCompilationWithMscorlib40AndSystemCore(text, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (10,14): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // unsafe class E2 : I<C<dynamic>.D*[]> {}
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "E2").WithLocation(10, 14),
                // (9,12): error CS1966: 'E1': cannot implement a dynamic interface 'I<dynamic>'
                // class E1 : I<dynamic> {}
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<dynamic>").WithArguments("E1", "I<dynamic>").WithLocation(9, 12),
                // (10,19): error CS1966: 'E2': cannot implement a dynamic interface 'I<C<dynamic>.D*[]>'
                // unsafe class E2 : I<C<dynamic>.D*[]> {}
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<C<dynamic>.D*[]>").WithArguments("E2", "I<C<dynamic>.D*[]>").WithLocation(10, 19));

            CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (11,12): error CS1966: 'E2': cannot implement a dynamic interface 'I<C<dynamic>.D*[]>'
                // class E2 : I<C<dynamic>.D*[]> {}
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<C<dynamic>.D*[]>").WithArguments("E2", "I<C<dynamic>.D*[]>"),
                // (10,12): error CS1966: 'E1': cannot implement a dynamic interface 'I<dynamic>'
                // class E1 : I<dynamic> {}
                Diagnostic(ErrorCode.ERR_DeriveFromConstructedDynamic, "I<dynamic>").WithArguments("E1", "I<dynamic>"));
        }

        [Fact]
        public void CS1967ERR_DynamicTypeAsBound()
        {
            var source =
@"delegate void D<T>() where T : dynamic;";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (1,32): error CS1967: Constraint cannot be the dynamic type
                Diagnostic(ErrorCode.ERR_DynamicTypeAsBound, "dynamic"));
        }

        [Fact]
        public void CS1968ERR_ConstructedDynamicTypeAsBound()
        {
            var source =
@"interface I<T> { }
struct S<T>
{
    internal delegate void D<U>();
}
class A<T> { }
class B<T, U>
    where T : A<S<T>.D<dynamic>>, I<dynamic[]>
    where U : I<S<dynamic>.D<T>>
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,15): error CS1968: Constraint cannot be a dynamic type 'A<S<T>.D<dynamic>>'
                Diagnostic(ErrorCode.ERR_ConstructedDynamicTypeAsBound, "A<S<T>.D<dynamic>>").WithArguments("A<S<T>.D<dynamic>>").WithLocation(8, 15),
                // (8,35): error CS1968: Constraint cannot be a dynamic type 'I<dynamic[]>'
                Diagnostic(ErrorCode.ERR_ConstructedDynamicTypeAsBound, "I<dynamic[]>").WithArguments("I<dynamic[]>").WithLocation(8, 35),
                // (9,15): error CS1968: Constraint cannot be a dynamic type 'I<S<dynamic>.D<T>>'
                Diagnostic(ErrorCode.ERR_ConstructedDynamicTypeAsBound, "I<S<dynamic>.D<T>>").WithArguments("I<S<dynamic>.D<T>>").WithLocation(9, 15));
        }

        // Instead of CS1982 ERR_DynamicNotAllowedInAttribute we report CS0181 ERR_BadAttributeParamType

        [Fact]
        public void CS1982ERR_DynamicNotAllowedInAttribute_NoError()
        {
            var text = @"
using System;

public class C<T> { public enum D { A } }

[A(T = typeof(dynamic[]))]     // Dev11 reports error, but this should be ok
[A(T = typeof(C<dynamic>))]
[A(T = typeof(C<dynamic>[]))]
[A(T = typeof(C<dynamic>.D[]))]
[A(T = typeof(C<dynamic>.D*[]))]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class A : Attribute
{
    public Type T;
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS7021ERR_NamespaceNotAllowedInScript()
        {
            var text = @"
namespace N1
{
   class A { public int Goo() { return 2; }}
}
";
            var expectedDiagnostics = new[]
            {
                // (2,1): error CS7021: You cannot declare namespace in script code
                // namespace N1
                Diagnostic(ErrorCode.ERR_NamespaceNotAllowedInScript, "namespace").WithLocation(2, 1)
            };

            CreateCompilationWithMscorlib461(new[] { Parse(text, options: TestOptions.Script) }).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void CS8050ERR_InitializerOnNonAutoProperty()
        {
            var source =
@"public class C
{
    int A { get; set; } = 1;
    
    int I { get { throw null; } set {  } } = 1;
    static int S { get { throw null; } set {  } } = 1;
    protected int P { get { throw null; } set {  } } = 1;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,9): error CS8050: Only auto-implemented properties can have initializers.
                //     int I { get { throw null; } set {  } } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "I").WithLocation(5, 9),
                // (6,16): error CS8050: Only auto-implemented properties can have initializers.
                //     static int S { get { throw null; } set {  } } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "S").WithLocation(6, 16),
                // (7,19): error CS8050: Only auto-implemented properties can have initializers.
                //     protected int P { get { throw null; } set {  } } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(7, 19)
            );
        }

        [Fact]
        public void ErrorTypeCandidateSymbols1()
        {
            var text = @"
class A {
    public B n;
}";

            CSharpCompilation comp = CreateCompilation(text);
            var classA = (NamedTypeSymbol)comp.GlobalNamespace.GetTypeMembers("A").Single();
            var fieldSym = (FieldSymbol)classA.GetMembers("n").Single();
            var fieldType = fieldSym.TypeWithAnnotations;

            Assert.Equal(SymbolKind.ErrorType, fieldType.Type.Kind);
            Assert.Equal("B", fieldType.Type.Name);

            var errorFieldType = (ErrorTypeSymbol)fieldType.Type;
            Assert.Equal(CandidateReason.None, errorFieldType.CandidateReason);
            Assert.Equal(0, errorFieldType.CandidateSymbols.Length);
        }

        [Fact]
        public void ErrorTypeCandidateSymbols2()
        {
            var text = @"
class C {
    private class B {}
}

class A : C {
    public B n;
}";

            CSharpCompilation comp = CreateCompilation(text);
            var classA = (NamedTypeSymbol)comp.GlobalNamespace.GetTypeMembers("A").Single();
            var classC = (NamedTypeSymbol)comp.GlobalNamespace.GetTypeMembers("C").Single();
            var classB = (NamedTypeSymbol)classC.GetTypeMembers("B").Single();
            var fieldSym = (FieldSymbol)classA.GetMembers("n").Single();
            var fieldType = fieldSym.Type;

            Assert.Equal(SymbolKind.ErrorType, fieldType.Kind);
            Assert.Equal("B", fieldType.Name);

            var errorFieldType = (ErrorTypeSymbol)fieldType;
            Assert.Equal(CandidateReason.Inaccessible, errorFieldType.CandidateReason);
            Assert.Equal(1, errorFieldType.CandidateSymbols.Length);
            Assert.Equal(classB, errorFieldType.CandidateSymbols[0]);
        }

        [Fact]
        public void ErrorTypeCandidateSymbols3()
        {
            var text = @"
using N1;
using N2;

namespace N1 {
    class B {}
}

namespace N2 {
    class B {}
}

class A : C {
    public B n;
}";

            CSharpCompilation comp = CreateCompilation(text);
            var classA = (NamedTypeSymbol)comp.GlobalNamespace.GetTypeMembers("A").Single();
            var ns1 = (NamespaceSymbol)comp.GlobalNamespace.GetMembers("N1").Single();
            var ns2 = (NamespaceSymbol)comp.GlobalNamespace.GetMembers("N2").Single();
            var classBinN1 = (NamedTypeSymbol)ns1.GetTypeMembers("B").Single();
            var classBinN2 = (NamedTypeSymbol)ns2.GetTypeMembers("B").Single();
            var fieldSym = (FieldSymbol)classA.GetMembers("n").Single();
            var fieldType = fieldSym.Type;

            Assert.Equal(SymbolKind.ErrorType, fieldType.Kind);
            Assert.Equal("B", fieldType.Name);

            var errorFieldType = (ErrorTypeSymbol)fieldType;
            Assert.Equal(CandidateReason.Ambiguous, errorFieldType.CandidateReason);
            Assert.Equal(2, errorFieldType.CandidateSymbols.Length);
            Assert.True((TypeSymbol.Equals(classBinN1, (TypeSymbol)errorFieldType.CandidateSymbols[0], TypeCompareKind.ConsiderEverything2) && TypeSymbol.Equals(classBinN2, (TypeSymbol)errorFieldType.CandidateSymbols[1], TypeCompareKind.ConsiderEverything2)) ||
                        (TypeSymbol.Equals(classBinN2, (TypeSymbol)errorFieldType.CandidateSymbols[0], TypeCompareKind.ConsiderEverything2) && TypeSymbol.Equals(classBinN1, (TypeSymbol)errorFieldType.CandidateSymbols[1], TypeCompareKind.ConsiderEverything2)),
                        "CandidateSymbols must by N1.B and N2.B in some order");
        }

        #endregion

        #region "Symbol Warning Tests"

        /// <summary>
        /// current error 104
        /// </summary>
        [Fact]
        public void CS0105WRN_DuplicateUsing01()
        {
            var text = @"using System;
using System;

namespace Goo.Bar
{
    class A { }
}

namespace testns
{
    using Goo.Bar;
    using System;
    using Goo.Bar;

    class B : A { }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (2,7): warning CS0105: The using directive for 'System' appeared previously in this namespace
                // using System;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "System").WithArguments("System"),
                // (13,11): warning CS0105: The using directive for 'Goo.Bar' appeared previously in this namespace
                //     using Goo.Bar;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "Goo.Bar").WithArguments("Goo.Bar"),
                // (1,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (12,5): info CS8019: Unnecessary using directive.
                //     using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (13,5): info CS8019: Unnecessary using directive.
                //     using Goo.Bar;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Goo.Bar;"));

            // TODO...
            // var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
        }

        [Fact]
        public void CS0108WRN_NewRequired01()
        {
            var text = @"using System;

namespace x
{
    public class @clx
    {
        public int i = 1;
    }

    public class @cly : clx
    {
        public static int i = 2;   // CS0108, use the new keyword
        public static void Main()
        {
            Console.WriteLine(i);
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewRequired, Line = 12, Column = 27, IsWarning = true });
        }

        [Fact]
        public void CS0108WRN_NewRequired02()
        {
            var source =
@"class A
{
    public static void P() { }
    public static void Q() { }
    public void R() { }
    public void S() { }
    public static int T { get; set; }
    public static int U { get; set; }
    public int V { get; set; }
    public int W { get; set; }
}
class B : A
{
    public static int P { get; set; } // CS0108
    public int Q { get; set; } // CS0108
    public static int R { get; set; } // CS0108
    public int S { get; set; } // CS0108
    public static void T() { } // CS0108
    public void U() { } // CS0108
    public static void V() { } // CS0108
    public void W() { } // CS0108
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,16): warning CS0108: 'B.Q' hides inherited member 'A.Q()'. Use the new keyword if hiding was intended.
                //     public int Q { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "Q").WithArguments("B.Q", "A.Q()").WithLocation(15, 16),
                // (16,23): warning CS0108: 'B.R' hides inherited member 'A.R()'. Use the new keyword if hiding was intended.
                //     public static int R { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "R").WithArguments("B.R", "A.R()").WithLocation(16, 23),
                // (17,16): warning CS0108: 'B.S' hides inherited member 'A.S()'. Use the new keyword if hiding was intended.
                //     public int S { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "S").WithArguments("B.S", "A.S()").WithLocation(17, 16),
                // (18,24): warning CS0108: 'B.T()' hides inherited member 'A.T'. Use the new keyword if hiding was intended.
                //     public static void T() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("B.T()", "A.T").WithLocation(18, 24),
                // (19,17): warning CS0108: 'B.U()' hides inherited member 'A.U'. Use the new keyword if hiding was intended.
                //     public void U() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "U").WithArguments("B.U()", "A.U").WithLocation(19, 17),
                // (20,24): warning CS0108: 'B.V()' hides inherited member 'A.V'. Use the new keyword if hiding was intended.
                //     public static void V() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "V").WithArguments("B.V()", "A.V").WithLocation(20, 24),
                // (21,17): warning CS0108: 'B.W()' hides inherited member 'A.W'. Use the new keyword if hiding was intended.
                //     public void W() { } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "W").WithArguments("B.W()", "A.W").WithLocation(21, 17),
                // (14,23): warning CS0108: 'B.P' hides inherited member 'A.P()'. Use the new keyword if hiding was intended.
                //     public static int P { get; set; } // CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "P").WithArguments("B.P", "A.P()").WithLocation(14, 23));
        }

        [WorkItem(539624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539624")]
        [Fact]
        public void CS0108WRN_NewRequired03()
        {
            var text = @"

class BaseClass
{
    public int MyMeth(int intI)
    {
        return intI;
    }
}

class MyClass : BaseClass
{
    public static int MyMeth(int intI)  // CS0108
    {
        return intI + 1;
    }
}

class SBase
{
    protected static void M() {}
}

class DClass : SBase
{
    protected void M() {} // CS0108
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewRequired, Line = 13, Column = 23, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewRequired, Line = 26, Column = 20, IsWarning = true });
        }

        [WorkItem(540459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540459")]
        [Fact]
        public void CS0108WRN_NewRequired04()
        {
            var text = @"

class A
{
    public void f() { }
}

class B: A
{

}

class C : B
{
    public int f = 3; //CS0108
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,16): warning CS0108: 'C.f' hides inherited member 'A.f()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "f").WithArguments("C.f", "A.f()"));
        }

        [Fact]
        public void CS0108WRN_NewRequired05()
        {
            var text = @"
class A
{
    public static void SM1() { }
    public static void SM2() { }
    public static void SM3() { }
    public static void SM4() { }

    public void IM1() { }
    public void IM2() { }
    public void IM3() { }
    public void IM4() { }

    public static int SP1 { get; set; }
    public static int SP2 { get; set; }
    public static int SP3 { get; set; }
    public static int SP4 { get; set; }

    public int IP1 { get; set; }
    public int IP2 { get; set; }
    public int IP3 { get; set; }
    public int IP4 { get; set; }

    public static event System.Action SE1;
    public static event System.Action SE2;
    public static event System.Action SE3;
    public static event System.Action SE4;

    public event System.Action IE1{ add { } remove { } }
    public event System.Action IE2{ add { } remove { } }
    public event System.Action IE3{ add { } remove { } }
    public event System.Action IE4{ add { } remove { } }
}
class B : A
{
    public static int SM1 { get; set; } //CS0108
    public int SM2 { get; set; } //CS0108
    public static event System.Action SM3; //CS0108
    public event System.Action SM4{ add { } remove { } } //CS0108

    public static int IM1 { get; set; } //CS0108
    public int IM2 { get; set; } //CS0108
    public static event System.Action IM3; //CS0108
    public event System.Action IM4{ add { } remove { } } //CS0108

    public static void SP1() { } //CS0108
    public void SP2() { } //CS0108
    public static event System.Action SP3; //CS0108
    public event System.Action SP4{ add { } remove { } } //CS0108

    public static void IP1() { } //CS0108
    public void IP2() { } //CS0108
    public static event System.Action IP3; //CS0108
    public event System.Action IP4{ add { } remove { } } //CS0108

    public static void SE1() { } //CS0108
    public void SE2() { } //CS0108
    public static int SE3 { get; set; } //CS0108
    public int SE4 { get; set; } //CS0108

    public static void IE1() { } //CS0108
    public void IE2() { } //CS0108
    public static int IE3 { get; set; } //CS0108
    public int IE4 { get; set; } //CS0108
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (36,23): warning CS0108: 'B.SM1' hides inherited member 'A.SM1()'. Use the new keyword if hiding was intended.
                //     public static int SM1 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SM1").WithArguments("B.SM1", "A.SM1()"),
                // (37,16): warning CS0108: 'B.SM2' hides inherited member 'A.SM2()'. Use the new keyword if hiding was intended.
                //     public int SM2 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SM2").WithArguments("B.SM2", "A.SM2()"),
                // (38,39): warning CS0108: 'B.SM3' hides inherited member 'A.SM3()'. Use the new keyword if hiding was intended.
                //     public static event System.Action SM3; //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SM3").WithArguments("B.SM3", "A.SM3()"),
                // (39,32): warning CS0108: 'B.SM4' hides inherited member 'A.SM4()'. Use the new keyword if hiding was intended.
                //     public event System.Action SM4{ add { } remove { } } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SM4").WithArguments("B.SM4", "A.SM4()"),
                // (41,23): warning CS0108: 'B.IM1' hides inherited member 'A.IM1()'. Use the new keyword if hiding was intended.
                //     public static int IM1 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IM1").WithArguments("B.IM1", "A.IM1()"),
                // (42,16): warning CS0108: 'B.IM2' hides inherited member 'A.IM2()'. Use the new keyword if hiding was intended.
                //     public int IM2 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IM2").WithArguments("B.IM2", "A.IM2()"),
                // (43,39): warning CS0108: 'B.IM3' hides inherited member 'A.IM3()'. Use the new keyword if hiding was intended.
                //     public static event System.Action IM3; //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IM3").WithArguments("B.IM3", "A.IM3()"),
                // (44,32): warning CS0108: 'B.IM4' hides inherited member 'A.IM4()'. Use the new keyword if hiding was intended.
                //     public event System.Action IM4{ add { } remove { } } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IM4").WithArguments("B.IM4", "A.IM4()"),
                // (46,24): warning CS0108: 'B.SP1()' hides inherited member 'A.SP1'. Use the new keyword if hiding was intended.
                //     public static void SP1() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SP1").WithArguments("B.SP1()", "A.SP1"),
                // (47,17): warning CS0108: 'B.SP2()' hides inherited member 'A.SP2'. Use the new keyword if hiding was intended.
                //     public void SP2() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SP2").WithArguments("B.SP2()", "A.SP2"),
                // (48,39): warning CS0108: 'B.SP3' hides inherited member 'A.SP3'. Use the new keyword if hiding was intended.
                //     public static event System.Action SP3; //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SP3").WithArguments("B.SP3", "A.SP3"),
                // (49,32): warning CS0108: 'B.SP4' hides inherited member 'A.SP4'. Use the new keyword if hiding was intended.
                //     public event System.Action SP4{ add { } remove { } } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SP4").WithArguments("B.SP4", "A.SP4"),
                // (51,24): warning CS0108: 'B.IP1()' hides inherited member 'A.IP1'. Use the new keyword if hiding was intended.
                //     public static void IP1() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IP1").WithArguments("B.IP1()", "A.IP1"),
                // (52,17): warning CS0108: 'B.IP2()' hides inherited member 'A.IP2'. Use the new keyword if hiding was intended.
                //     public void IP2() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IP2").WithArguments("B.IP2()", "A.IP2"),
                // (53,39): warning CS0108: 'B.IP3' hides inherited member 'A.IP3'. Use the new keyword if hiding was intended.
                //     public static event System.Action IP3; //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IP3").WithArguments("B.IP3", "A.IP3"),
                // (54,32): warning CS0108: 'B.IP4' hides inherited member 'A.IP4'. Use the new keyword if hiding was intended.
                //     public event System.Action IP4{ add { } remove { } } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IP4").WithArguments("B.IP4", "A.IP4"),
                // (56,24): warning CS0108: 'B.SE1()' hides inherited member 'A.SE1'. Use the new keyword if hiding was intended.
                //     public static void SE1() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SE1").WithArguments("B.SE1()", "A.SE1"),
                // (57,17): warning CS0108: 'B.SE2()' hides inherited member 'A.SE2'. Use the new keyword if hiding was intended.
                //     public void SE2() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SE2").WithArguments("B.SE2()", "A.SE2"),
                // (58,23): warning CS0108: 'B.SE3' hides inherited member 'A.SE3'. Use the new keyword if hiding was intended.
                //     public static int SE3 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SE3").WithArguments("B.SE3", "A.SE3"),
                // (59,16): warning CS0108: 'B.SE4' hides inherited member 'A.SE4'. Use the new keyword if hiding was intended.
                //     public int SE4 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "SE4").WithArguments("B.SE4", "A.SE4"),
                // (61,24): warning CS0108: 'B.IE1()' hides inherited member 'A.IE1'. Use the new keyword if hiding was intended.
                //     public static void IE1() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IE1").WithArguments("B.IE1()", "A.IE1"),
                // (62,17): warning CS0108: 'B.IE2()' hides inherited member 'A.IE2'. Use the new keyword if hiding was intended.
                //     public void IE2() { } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IE2").WithArguments("B.IE2()", "A.IE2"),
                // (63,23): warning CS0108: 'B.IE3' hides inherited member 'A.IE3'. Use the new keyword if hiding was intended.
                //     public static int IE3 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IE3").WithArguments("B.IE3", "A.IE3"),
                // (64,16): warning CS0108: 'B.IE4' hides inherited member 'A.IE4'. Use the new keyword if hiding was intended.
                //     public int IE4 { get; set; } //CS0108
                Diagnostic(ErrorCode.WRN_NewRequired, "IE4").WithArguments("B.IE4", "A.IE4"),
                // (53,39): warning CS0067: The event 'B.IP3' is never used
                //     public static event System.Action IP3; //CS0108
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "IP3").WithArguments("B.IP3"),
                // (25,39): warning CS0067: The event 'A.SE2' is never used
                //     public static event System.Action SE2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SE2").WithArguments("A.SE2"),
                // (26,39): warning CS0067: The event 'A.SE3' is never used
                //     public static event System.Action SE3;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SE3").WithArguments("A.SE3"),
                // (38,39): warning CS0067: The event 'B.SM3' is never used
                //     public static event System.Action SM3; //CS0108
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SM3").WithArguments("B.SM3"),
                // (279): warning CS0067: The event 'A.SE4' is never used
                //     public static event System.Action SE4;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SE4").WithArguments("A.SE4"),
                // (48,39): warning CS0067: The event 'B.SP3' is never used
                //     public static event System.Action SP3; //CS0108
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SP3").WithArguments("B.SP3"),
                // (24,39): warning CS0067: The event 'A.SE1' is never used
                //     public static event System.Action SE1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SE1").WithArguments("A.SE1"),
                // (43,39): warning CS0067: The event 'B.IM3' is never used
                //     public static event System.Action IM3; //CS0108
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "IM3").WithArguments("B.IM3"));
        }

        [WorkItem(539624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539624")]
        [Fact]
        public void CS0108WRN_NewRequired_Arity()
        {
            var text = @"
class Class
{
    public class T { }
    public class T<A> { }
    public class T<A, B> { }
    public class T<A, B, C> { }

    public void M() { }
    public void M<A>() { }
    public void M<A, B>() { }
    public void M<A, B, C>() { }

    public delegate void D();
    public delegate void D<A>();
    public delegate void D<A, B>();
    public delegate void D<A, B, C>();
}

class HideWithClass : Class
{
    public class T { }
    public class T<A> { }
    public class T<A, B> { }
    public class T<A, B, C> { }

    public class M { }
    public class M<A> { }
    public class M<A, B> { }
    public class M<A, B, C> { }

    public class D { }
    public class D<A> { }
    public class D<A, B> { }
    public class D<A, B, C> { }
}

class HideWithMethod : Class
{
    public void T() { }
    public void T<A>() { }
    public void T<A, B>() { }
    public void T<A, B, C>() { }

    public void M() { }
    public void M<A>() { }
    public void M<A, B>() { }
    public void M<A, B, C>() { }

    public void D() { }
    public void D<A>() { }
    public void D<A, B>() { }
    public void D<A, B, C>() { }
}

class HideWithDelegate : Class
{
    public delegate void T();
    public delegate void T<A>();
    public delegate void T<A, B>();
    public delegate void T<A, B, C>();

    public delegate void M();
    public delegate void M<A>();
    public delegate void M<A, B>();
    public delegate void M<A, B, C>();

    public delegate void D();
    public delegate void D<A>();
    public delegate void D<A, B>();
    public delegate void D<A, B, C>();
}
";
            CreateCompilation(text).VerifyDiagnostics(
                /* HideWithClass */
                // (22,18): warning CS0108: 'HideWithClass.T' hides inherited member 'Class.T'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithClass.T", "Class.T"),
                // (23,18): warning CS0108: 'HideWithClass.T<A>' hides inherited member 'Class.T<A>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithClass.T<A>", "Class.T<A>"),
                // (24,18): warning CS0108: 'HideWithClass.T<A, B>' hides inherited member 'Class.T<A, B>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithClass.T<A, B>", "Class.T<A, B>"),
                // (25,18): warning CS0108: 'HideWithClass.T<A, B, C>' hides inherited member 'Class.T<A, B, C>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithClass.T<A, B, C>", "Class.T<A, B, C>"),
                // (27,18): warning CS0108: 'HideWithClass.M' hides inherited member 'Class.M()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithClass.M", "Class.M()"),
                // (28,18): warning CS0108: 'HideWithClass.M<A>' hides inherited member 'Class.M<A>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithClass.M<A>", "Class.M<A>()"),
                // (29,18): warning CS0108: 'HideWithClass.M<A, B>' hides inherited member 'Class.M<A, B>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithClass.M<A, B>", "Class.M<A, B>()"),
                // (30,18): warning CS0108: 'HideWithClass.M<A, B, C>' hides inherited member 'Class.M<A, B, C>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithClass.M<A, B, C>", "Class.M<A, B, C>()"),
                // (32,18): warning CS0108: 'HideWithClass.D' hides inherited member 'Class.D'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithClass.D", "Class.D"),
                // (33,18): warning CS0108: 'HideWithClass.D<A>' hides inherited member 'Class.D<A>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithClass.D<A>", "Class.D<A>"),
                // (34,18): warning CS0108: 'HideWithClass.D<A, B>' hides inherited member 'Class.D<A, B>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithClass.D<A, B>", "Class.D<A, B>"),
                // (35,18): warning CS0108: 'HideWithClass.D<A, B, C>' hides inherited member 'Class.D<A, B, C>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithClass.D<A, B, C>", "Class.D<A, B, C>"),

                /* HideWithMethod */
                // (40,17): warning CS0108: 'HideWithMethod.T()' hides inherited member 'Class.T'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithMethod.T()", "Class.T"),
                // (41,17): warning CS0108: 'HideWithMethod.T<A>()' hides inherited member 'Class.T'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithMethod.T<A>()", "Class.T"),
                // (42,17): warning CS0108: 'HideWithMethod.T<A, B>()' hides inherited member 'Class.T'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithMethod.T<A, B>()", "Class.T"),
                // (43,17): warning CS0108: 'HideWithMethod.T<A, B, C>()' hides inherited member 'Class.T'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithMethod.T<A, B, C>()", "Class.T"),
                // (45,17): warning CS0108: 'HideWithMethod.M()' hides inherited member 'Class.M()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithMethod.M()", "Class.M()"),
                // (46,17): warning CS0108: 'HideWithMethod.M<A>()' hides inherited member 'Class.M<A>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithMethod.M<A>()", "Class.M<A>()"),
                // (47,17): warning CS0108: 'HideWithMethod.M<A, B>()' hides inherited member 'Class.M<A, B>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithMethod.M<A, B>()", "Class.M<A, B>()"),
                // (48,17): warning CS0108: 'HideWithMethod.M<A, B, C>()' hides inherited member 'Class.M<A, B, C>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithMethod.M<A, B, C>()", "Class.M<A, B, C>()"),
                // (50,17): warning CS0108: 'HideWithMethod.D()' hides inherited member 'Class.D'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithMethod.D()", "Class.D"),
                // (51,17): warning CS0108: 'HideWithMethod.D<A>()' hides inherited member 'Class.D'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithMethod.D<A>()", "Class.D"),
                // (52,17): warning CS0108: 'HideWithMethod.D<A, B>()' hides inherited member 'Class.D'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithMethod.D<A, B>()", "Class.D"),
                // (53,17): warning CS0108: 'HideWithMethod.D<A, B, C>()' hides inherited member 'Class.D'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithMethod.D<A, B, C>()", "Class.D"),

                /* HideWithDelegate */
                // (58,26): warning CS0108: 'HideWithDelegate.T' hides inherited member 'Class.T'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithDelegate.T", "Class.T"),
                // (59,26): warning CS0108: 'HideWithDelegate.T<A>' hides inherited member 'Class.T<A>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithDelegate.T<A>", "Class.T<A>"),
                // (60,26): warning CS0108: 'HideWithDelegate.T<A, B>' hides inherited member 'Class.T<A, B>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithDelegate.T<A, B>", "Class.T<A, B>"),
                // (61,26): warning CS0108: 'HideWithDelegate.T<A, B, C>' hides inherited member 'Class.T<A, B, C>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "T").WithArguments("HideWithDelegate.T<A, B, C>", "Class.T<A, B, C>"),
                // (63,26): warning CS0108: 'HideWithDelegate.M' hides inherited member 'Class.M()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithDelegate.M", "Class.M()"),
                // (64,26): warning CS0108: 'HideWithDelegate.M<A>' hides inherited member 'Class.M<A>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithDelegate.M<A>", "Class.M<A>()"),
                // (65,26): warning CS0108: 'HideWithDelegate.M<A, B>' hides inherited member 'Class.M<A, B>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithDelegate.M<A, B>", "Class.M<A, B>()"),
                // (66,26): warning CS0108: 'HideWithDelegate.M<A, B, C>' hides inherited member 'Class.M<A, B, C>()'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("HideWithDelegate.M<A, B, C>", "Class.M<A, B, C>()"),
                // (68,26): warning CS0108: 'HideWithDelegate.D' hides inherited member 'Class.D'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithDelegate.D", "Class.D"),
                // (69,26): warning CS0108: 'HideWithDelegate.D<A>' hides inherited member 'Class.D<A>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithDelegate.D<A>", "Class.D<A>"),
                // (70,26): warning CS0108: 'HideWithDelegate.D<A, B>' hides inherited member 'Class.D<A, B>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithDelegate.D<A, B>", "Class.D<A, B>"),
                // (71,26): warning CS0108: 'HideWithDelegate.D<A, B, C>' hides inherited member 'Class.D<A, B, C>'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "D").WithArguments("HideWithDelegate.D<A, B, C>", "Class.D<A, B, C>"));
        }

        [Fact, WorkItem(546736, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546736")]
        public void CS0108WRN_NewRequired_Partial()
        {
            var text = @"
partial class Parent
{
    partial void PM(int x);
    private void M(int x) { }
    partial class Child : Parent
    {
        partial void PM(int x);
        private void M(int x) { }
    }
}
partial class AnotherChild : Parent
{
    partial void PM(int x);
    private void M(int x) { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_NewRequired, "PM").WithArguments("Parent.Child.PM(int)", "Parent.PM(int)"),
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("Parent.Child.M(int)", "Parent.M(int)"));
        }

        [Fact]
        public void CS0109WRN_NewNotRequired()
        {
            var text = @"namespace x
{
    public class @a
    {
        public int i;
    }

    public class @b : a
    {
        public new int i;
        public new int j;   // CS0109
        public static void Main()
        {
        }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_NewNotRequired, Line = 11, Column = 24, IsWarning = true });
        }

        [Fact]
        public void CS0114WRN_NewOrOverrideExpected()
        {
            var text = @"abstract public class @clx
{
    public abstract void f();
}

public class @cly : clx
{
    public void f() // CS0114, hides base class member
    {
    }

    public static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription[] {
                    new ErrorDescription { Code = (int)ErrorCode.WRN_NewOrOverrideExpected, Line = 8, Column = 17, IsWarning = true },
                    new ErrorDescription { Code = (int)ErrorCode.ERR_UnimplementedAbstractMethod, Line = 6, Column = 14 }
                });
        }

        [Fact]
        public void CS0282WRN_SequentialOnPartialClass()
        {
            var text = @"
partial struct A
{
    int i;
}
partial struct A
{
    int j;
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (1,16): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'A'. To specify an ordering, all instance fields must be in the same declaration.
                // partial struct A
                Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "A").WithArguments("A"),
                // (3,9): warning CS0169: The field 'A.i' is never used
                //     int i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("A.i"),
                // (7,9): warning CS0169: The field 'A.j' is never used
                //     int j;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "j").WithArguments("A.j"));
        }

        [Fact]
        [WorkItem(23668, "https://github.com/dotnet/roslyn/issues/23668")]
        public void CS0282WRN_PartialWithPropertyButSingleField()
        {
            string program =
@"partial struct X // No warning CS0282
{
    // The only field of X is a backing field of A.
    public int A { get; set; }
}

partial struct X : I
{
    // This partial definition has no field.
    int I.A { get => A; set => A = value; }
}

interface I
{
    int A { get; set; }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// import - Lib:  class A     { class B {} } 
        ///      vs. curr: Namespace A { class B {} } - use B
        /// </summary>
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void CS0435WRN_SameFullNameThisNsAgg01()
        {
            var text = @"namespace CSFields
{
    public class FFF { }
}

namespace SA
{
    class Test
    {
        CSFields.FFF var = null;
        void M(CSFields.FFF p) { }
    }
}
";
            // class CSFields { class FFF {}}
            var ref1 = TestReferences.SymbolsTests.Fields.CSFields.dll;

            var comp = CreateCompilation(new[] { text }, new List<MetadataReference> { ref1 });
            comp.VerifyDiagnostics(
                // (11,16): warning CS0435: The namespace 'CSFields' in '0.cs' conflicts with the imported type 'CSFields' in 'CSFields, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in ''.
                //         void M(CSFields.FFF p) { }
                Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "CSFields").WithArguments("0.cs", "CSFields", "CSFields, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CSFields"),
                // (10,9): warning CS0435: The namespace 'CSFields' in '' conflicts with the imported type 'CSFields' in 'CSFields, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in ''.
                //         CSFields.FFF var = null;
                Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "CSFields").WithArguments("0.cs", "CSFields", "CSFields, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CSFields"),
                // (10,22): warning CS0414: The field 'SA.Test.var' is assigned but its value is never used
                //         CSFields.FFF var = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "var").WithArguments("SA.Test.var")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("SA").Single() as NamespaceSymbol;
            // TODO...
        }

        /// <summary>
        /// import - Lib: class A  {}  vs. curr: class A {  }
        /// </summary>
        [Fact]
        public void CS0436WRN_SameFullNameThisAggAgg01()
        {
            var text = @"class Class1 { }

namespace SA
{
    class Test
    {
        Class1 cls;
        void M(Class1 p) { }
    }
}
";
            // Class1
            var ref1 = TestReferences.SymbolsTests.V1.MTTestLib1.dll;

            // Roslyn gives CS1542 or CS0104
            var comp = CreateCompilation(new[] { text }, new List<MetadataReference> { ref1 });
            comp.VerifyDiagnostics(
                // (8,16): warning CS0436: The type 'Class1' in '0.cs' conflicts with the imported type 'Class1' in 'MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         void M(Class1 p) { }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Class1").WithArguments("0.cs", "Class1", "MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Class1"),
                // (7,9): warning CS0436: The type 'Class1' in '0.cs' conflicts with the imported type 'Class1' in 'MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         Class1 cls;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Class1").WithArguments("0.cs", "Class1", "MTTestLib1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Class1"),
                // (7,16): warning CS0169: The field 'SA.Test.cls' is never used
                //         Class1 cls;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "cls").WithArguments("SA.Test.cls"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("SA").Single() as NamespaceSymbol;
            // TODO...
        }

        [Fact]
        [WorkItem(546077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546077")]
        public void MultipleSymbolDisambiguation()
        {
            var sourceRef1 = @"
public class CCC { public class X { } }
public class CNC { public class X { } }
namespace NCC { public class X { } }
namespace NNC { public class X { } }
public class CCN { public class X { } }
public class CNN { public class X { } }
namespace NCN { public class X { } }
namespace NNN { public class X { } }
";

            var sourceRef2 = @"
public class CCC { public class X { } }
namespace CNC { public class X { } }
public class NCC{ public class X { } }
namespace NNC { public class X { } }
public class CCN { public class X { } }
namespace CNN { public class X { } }
public class NCN { public class X { } }
namespace NNN { public class X { } }
";

            var sourceLib = @"
public class CCC { public class X { } }
namespace CCN { public class X { } } 
public class CNC { public class X { } }
namespace CNN { public class X { } }
public class NCC { public class X { } }
namespace NCN { public class X { } }
public class NNC { public class X { } }
namespace NNN { public class X { } }

internal class DC : CCC.X { }
internal class DN : CCN.X { }
internal class D3 : CNC.X { }
internal class D4 : CNN.X { }
internal class D5 : NCC.X { }
internal class D6 : NCN.X { }
internal class D7 : NNC.X { }
internal class D8 : NNN.X { }
";

            var ref1 = CreateCompilation(sourceRef1, assemblyName: "Ref1").VerifyDiagnostics();
            var ref2 = CreateCompilation(sourceRef2, assemblyName: "Ref2").VerifyDiagnostics();

            var tree = Parse(sourceLib, filename: @"C:\lib.cs");

            var lib = CreateCompilation(tree, new MetadataReference[]
            {
                new CSharpCompilationReference(ref1),
                new CSharpCompilationReference(ref2),
            });

            // In some cases we might order the symbols differently than Dev11 and thus reporting slightly different warnings.
            // E.g. (src:type, md:type, md:namespace) vs (src:type, md:namespace, md:type)
            // We report (type, type) ambiguity while Dev11 reports (type, namespace) ambiguity, but both are equally correct.

            // TODO (tomat):
            // We should report a path to an assembly rather than the assembly name when reporting an error.

            lib.VerifyDiagnostics(
                // C:\lib.cs(12,21): warning CS0435: The namespace 'CCN' in 'C:\lib.cs' conflicts with the imported type 'CCN' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "CCN").WithArguments(@"C:\lib.cs", "CCN", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CCN"),
                // C:\lib.cs(16,21): warning CS0435: The namespace 'NCN' in 'C:\lib.cs' conflicts with the imported type 'NCN' in 'Ref2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "NCN").WithArguments(@"C:\lib.cs", "NCN", "Ref2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NCN"),
                // C:\lib.cs(16,25): warning CS0436: The type 'NCN.X' in 'C:\lib.cs' conflicts with the imported type 'NCN.X' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "X").WithArguments(@"C:\lib.cs", "NCN.X", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NCN.X"),
                // C:\lib.cs(11,21): warning CS0436: The type 'CCC' in 'C:\lib.cs' conflicts with the imported type 'CCC' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "CCC").WithArguments(@"C:\lib.cs", "CCC", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CCC"),
                // C:\lib.cs(15,21): warning CS0436: The type 'NCC' in 'C:\lib.cs' conflicts with the imported type 'NCC' in 'Ref2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "NCC").WithArguments(@"C:\lib.cs", "NCC", "Ref2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NCC"),
                // C:\lib.cs(13,21): warning CS0436: The type 'CNC' in 'C:\lib.cs' conflicts with the imported type 'CNC' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "CNC").WithArguments(@"C:\lib.cs", "CNC", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CNC"),
                // C:\lib.cs(14,21): warning CS0435: The namespace 'CNN' in 'C:\lib.cs' conflicts with the imported type 'CNN' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the namespace defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisNsAgg, "CNN").WithArguments(@"C:\lib.cs", "CNN", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CNN"),
                // C:\lib.cs(14,25): warning CS0436: The type 'CNN.X' in 'C:\lib.cs' conflicts with the imported type 'CNN.X' in 'Ref2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "X").WithArguments(@"C:\lib.cs", "CNN.X", "Ref2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "CNN.X"),
                // C:\lib.cs(17,21): warning CS0437: The type 'NNC' in 'C:\lib.cs' conflicts with the imported namespace 'NNC' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "NNC").WithArguments(@"C:\lib.cs", "NNC", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NNC"),
                // C:\lib.cs(18,25): warning CS0436: The type 'NNN.X' in 'C:\lib.cs' conflicts with the imported type 'NNN.X' in 'Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'C:\lib.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "X").WithArguments(@"C:\lib.cs", "NNN.X", "Ref1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "NNN.X"));
        }

        [Fact]
        public void MultipleSourceSymbols1()
        {
            var sourceLib = @"
public class C
{
}

namespace C
{
}

public class D : C
{
}";
            // do not report lookup errors

            CreateCompilation(sourceLib).VerifyDiagnostics(
                // error CS0101: The namespace '<global namespace>' already contains a definition for 'C'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "<global namespace>"));
        }

        [Fact]
        public void MultipleSourceSymbols2()
        {
            var sourceRef1 = @"
public class C { public class X { } }
";

            var sourceRef2 = @"
namespace N { public class X { } }
";

            var sourceLib = @"
public class C { public class X { } }
namespace C { public class X { } }

internal class D : C.X { }
";

            var ref1 = CreateCompilation(sourceRef1, assemblyName: "Ref1").VerifyDiagnostics();
            var ref2 = CreateCompilation(sourceRef2, assemblyName: "Ref2").VerifyDiagnostics();

            var tree = Parse(sourceLib, filename: @"C:\lib.cs");

            var lib = CreateCompilation(tree, new MetadataReference[]
            {
                new CSharpCompilationReference(ref1),
                new CSharpCompilationReference(ref2),
            });

            // do not report lookup errors

            lib.VerifyDiagnostics(
                // C:\lib.cs(2,14): error CS0101: The namespace '<global namespace>' already contains a definition for 'C'
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "<global namespace>"));
        }

        [WorkItem(545725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545725")]
        [Fact]
        public void CS0436WRN_SameFullNameThisAggAgg02()
        {
            var text = @"
namespace System
{
    class Int32
    {
        const Int32 MaxValue = null;
        static void Main()
        {
            Int32 x = System.Int32.MaxValue;
        }
    }
}
";
            // TODO (tomat):
            // We should report a path to an assembly rather than the assembly name when reporting an error.

            CreateCompilation(new SyntaxTree[] { Parse(text, "goo.cs") }).VerifyDiagnostics(
                // goo.cs(6,15): warning CS0436: The type 'System.Int32' in 'goo.cs' conflicts with the imported type 'int' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'goo.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Int32").WithArguments("goo.cs", "System.Int32", RuntimeCorLibName.FullName, "int"),
                // goo.cs(9,13): warning CS0436: The type 'System.Int32' in 'goo.cs' conflicts with the imported type 'int' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'goo.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Int32").WithArguments("goo.cs", "System.Int32", RuntimeCorLibName.FullName, "int"),
                // goo.cs(9,23): warning CS0436: The type 'System.Int32' in 'goo.cs' conflicts with the imported type 'int' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'goo.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "System.Int32").WithArguments("goo.cs", "System.Int32", RuntimeCorLibName.FullName, "int"),
                // goo.cs(9,19): warning CS0219: The variable 'x' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x"));
        }

        [WorkItem(538320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538320")]
        [Fact]
        public void CS0436WRN_SameFullNameThisAggAgg03()
        {
            var text = @"namespace System
{
    class Object
    {
        static void Main()
        {
            Console.WriteLine(""hello"");
        }
    }
    class Goo : object {}
    class Bar : Object {}
}";
            // TODO (tomat):
            // We should report a path to an assembly rather than the assembly name when reporting an error.

            CreateCompilation(new SyntaxTree[] { Parse(text, "goo.cs") }).VerifyDiagnostics(
                // goo.cs(11,17): warning CS0436: The type 'System.Object' in 'goo.cs' conflicts with the imported type 'object' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'goo.cs'.
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Object").WithArguments("goo.cs", "System.Object", RuntimeCorLibName.FullName, "object"));
        }

        /// <summary>
        /// import- Lib: namespace A { class B{} }  vs. curr: class A { class B {} }
        /// </summary>
        [Fact]
        public void CS0437WRN_SameFullNameThisAggNs01()
        {
            var text = @"public class AppCS
{
    public class App { }
}

namespace SA
{
    class Test
    {
        AppCS.App app = null;
        void M(AppCS.App p) { }
    }
}
";
            // this is not related to this test, but need by lib2 (don't want to add a new dll resource)
            var cs00 = TestReferences.MetadataTests.NetModule01.ModuleCS00;
            var cs01 = TestReferences.MetadataTests.NetModule01.ModuleCS01;
            var vb01 = TestReferences.MetadataTests.NetModule01.ModuleVB01;
            var ref1 = TestReferences.MetadataTests.NetModule01.AppCS;

            // Roslyn CS1542
            var comp = CreateCompilation(new[] { text }, new List<MetadataReference> { ref1 });
            comp.VerifyDiagnostics(
                // (11,16): warning CS0437: The type 'AppCS' in '0.cs' conflicts with the imported namespace 'AppCS' in 'AppCS, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         void M(AppCS.App p) { }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "AppCS").WithArguments("0.cs", "AppCS", "AppCS, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "AppCS"),
                // (10,9): warning CS0437: The type 'AppCS' in '' conflicts with the imported namespace 'AppCS' in 'AppCS, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         AppCS.App app = null;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "AppCS").WithArguments("0.cs", "AppCS", "AppCS, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "AppCS"),
                // (10,19): warning CS0414: The field 'SA.Test.app' is assigned but its value is never used
                //         AppCS.App app = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "app").WithArguments("SA.Test.app"));

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("SA").Single() as NamespaceSymbol;
            // TODO...
        }

        [WorkItem(545649, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545649")]
        [Fact]
        public void CS0437WRN_SameFullNameThisAggNs02()
        {
            var source = @"
using System;
class System { }
";

            // NOTE: both mscorlib.dll and System.Core.dll define types in the System namespace.
            var compilation = CreateCompilation(
                Parse(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));

            compilation.VerifyDiagnostics(
                // (2,7): warning CS0437: The type 'System' in '' conflicts with the imported namespace 'System' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                // using System;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggNs, "System").WithArguments("", "System", RuntimeCorLibName.FullName, "System"),
                // (2,7): error CS0138: A using namespace directive can only be applied to namespaces; 'System' is a type not a namespace
                // using System;
                Diagnostic(ErrorCode.ERR_BadUsingNamespace, "System").WithArguments("System"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));
        }

        [Fact]
        public void CS0465WRN_FinalizeMethod()
        {
            var text = @"
class A
{
   protected virtual void Finalize() {}   // CS0465
}

abstract class B
{
   public abstract void Finalize();   // CS0465
}

abstract class C
{
   protected int Finalize() {return 0;} // No Warning
   protected abstract void Finalize(int x); // No Warning
   protected virtual void Finalize<T>() { } // No Warning
}
class D : C
{
    protected override void Finalize(int x) { } // No Warning
    protected override void Finalize<U>() { } // No Warning
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,27): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (9,25): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));
        }

        [Fact]
        public void CS0473WRN_ExplicitImplCollision()
        {
            var text = @"public interface ITest<T>
{
    int TestMethod(int i);
    int TestMethod(T i);
}

public class ImplementingClass : ITest<int>
{
    int ITest<int>.TestMethod(int i) // CS0473
    {
        return i + 1;
    }

    public int TestMethod(int i)
    {
        return i - 1;
    }
}

class T
{
    static int Main()
    {
        return 0;
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_ExplicitImplCollision, Line = 9, Column = 20, IsWarning = true });
        }

        [Fact]
        public void CS0626WRN_ExternMethodNoImplementation01()
        {
            var source =
@"class A : System.Attribute { }
class B
{
    extern void M();
    extern object P1 { get; set; }
    extern static public bool operator !(B b);
}
class C
{
    [A] extern void M();
    [A] extern object P1 { get; set; }
    [A] extern static public bool operator !(C c);
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,17): warning CS0626: Method, operator, or accessor 'B.M()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M").WithArguments("B.M()").WithLocation(4, 17),
                // (5,24): warning CS0626: Method, operator, or accessor 'B.P1.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("B.P1.get").WithLocation(5, 24),
                // (5,29): warning CS0626: Method, operator, or accessor 'B.P1.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("B.P1.set").WithLocation(5, 29),
                // (6,40): warning CS0626: Method, operator, or accessor 'B.operator !(B)' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "!").WithArguments("B.operator !(B)").WithLocation(6, 40),
                // (11,28): warning CS0626: Method, operator, or accessor 'C.P1.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("C.P1.get").WithLocation(11, 28),
                // (11,33): warning CS0626: Method, operator, or accessor 'C.P1.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("C.P1.set").WithLocation(11, 33));
        }

        [WorkItem(544660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544660")]
        [WorkItem(530324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530324")]
        [Fact]
        public void CS0626WRN_ExternMethodNoImplementation02()
        {
            var source =
@"class A : System.Attribute { }
delegate void D();
class C
{
    extern event D E1;
    [A] extern event D E2;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,20): warning CS0626: Method, operator, or accessor 'C.E1.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event D E1;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "E1").WithArguments("C.E1.remove").WithLocation(5, 20),
                // (6,24): warning CS0626: Method, operator, or accessor 'C.E2.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     [A] extern event D E2;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "E2").WithArguments("C.E2.remove").WithLocation(6, 24));
        }

        [Fact]
        public void CS0628WRN_ProtectedInSealed01()
        {
            var text = @"namespace NS
{
    sealed class Goo
    {
        protected int i = 0;
        protected internal void M() { }
    }

    sealed public class Bar<T>
    {
        internal protected void M1(T t) { }
        protected V M2<V>(T t) { return default(V); }
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 5, Column = 23, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 6, Column = 33, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 11, Column = 33, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 12, Column = 21, IsWarning = true });
        }

        [Fact]
        public void CS0628WRN_ProtectedInSealed02()
        {
            var text = @"sealed class C
{
    protected object P { get { return null; } }
    public int Q { get; protected set; }
}
sealed class C<T>
{
    internal protected T P { get; protected set; }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 3, Column = 22, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 4, Column = 35, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 8, Column = 26, IsWarning = true },
                new ErrorDescription { Code = (int)ErrorCode.WRN_ProtectedInSealed, Line = 8, Column = 45, IsWarning = true });
        }

        [WorkItem(539588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539588")]
        [Fact]
        public void CS0628WRN_ProtectedInSealed03()
        {
            var text = @"abstract class C
{
    protected abstract void M();
    protected internal virtual int P { get { return 0; } }
}
sealed class D : C
{
    protected override void M() { }
    protected internal override int P { get { return 0; } }
    protected void N() { } // CS0628
    protected internal int Q { get { return 0; } } // CS0628

    protected class Nested {} // CS0628
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (13,21): warning CS0628: 'D.Nested': new protected member declared in sealed type
                //     protected class Nested {} // CS0628
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "Nested").WithArguments("D.Nested"),
                // (11,28): warning CS0628: 'D.Q': new protected member declared in sealed type
                //     protected internal int Q { get { return 0; } } // CS0628
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "Q").WithArguments("D.Q"),
                // (10,20): warning CS0628: 'D.N()': new protected member declared in sealed type
                //     protected void N() { } // CS0628
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "N").WithArguments("D.N()")
                );
        }

        [Fact]
        public void CS0628WRN_ProtectedInSealed04()
        {
            var text = @"
sealed class C
{
    protected event System.Action E;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,35): warning CS0628: 'C.E': new protected member declared in sealed type
                //     protected event System.Action E;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "E").WithArguments("C.E"),
                // (4,35): warning CS0067: The event 'C.E' is never used
                //     protected event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void CS0628WRN_ProtectedInSealed05()
        {
            const string text = @"
abstract class C
{
    protected C() { }
}

sealed class D : C
{
    protected override D() { }
    protected D(byte b) { }
    protected internal D(short s) { }
    internal protected D(int i) { }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (9,24): error CS0106: The modifier 'override' is not valid for this item
                //     protected override D() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("override").WithLocation(9, 24),
                // (10,15): warning CS0628: 'D.D(byte)': new protected member declared in sealed type
                //     protected D(byte b) { }
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "D").WithArguments("D.D(byte)").WithLocation(10, 15),
                // (11,24): warning CS0628: 'D.D(short)': new protected member declared in sealed type
                //     protected internal D(short s) { }
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "D").WithArguments("D.D(short)").WithLocation(11, 24),
                // (12,24): warning CS0628: 'D.D(int)': new protected member declared in sealed type
                //     internal protected D(int i) { }
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "D").WithArguments("D.D(int)").WithLocation(12, 24));
        }

        [Fact]
        public void CS0659WRN_EqualsWithoutGetHashCode()
        {
            var text = @"class Test
{
    public override bool Equals(object o) { return true; }   // CS0659
}
// However the warning should NOT be produced if the Equals is not a 'real' override
// of Equals. Neither of these should produce a warning:
class Test2 
{ 
    public new virtual bool Equals(object o) { return true; } 
}
class Test3 : Test2 
{
    public override bool Equals(object o) { return true; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
// (1,7): warning CS0659: 'Test' overrides Object.Equals(object o) but does not override Object.GetHashCode()
// class Test
Diagnostic(ErrorCode.WRN_EqualsWithoutGetHashCode, "Test").WithArguments("Test")
                );
        }

        [Fact]
        public void CS0660WRN_EqualityOpWithoutEquals()
        {
            var text = @"
class TestBase
{ 
    public new virtual bool Equals(object o) { return true; } 
}
class Test : TestBase   // CS0660
{
    public static bool operator ==(object o, Test t)
    {
        return true;
    }

    public static bool operator !=(object o, Test t)
    {
        return true;
    }

    // This does not count!
    public override bool Equals(object o) { return true; }

    public override int GetHashCode()
    {
        return 0;
    }

    public static void Main()
    {
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,7): warning CS0660: 'Test' defines operator == or operator != but does not override Object.Equals(object o)
                Diagnostic(ErrorCode.WRN_EqualityOpWithoutEquals, "Test").WithArguments("Test"));
        }

        [Fact]
        public void CS0660WRN_EqualityOpWithoutEquals_NoWarningWhenOverriddenWithDynamicParameter()
        {
            string source = @"
public class C
{
    public override bool Equals(dynamic o) { return false; }
    public static bool operator ==(C v1, C v2) { return true; }
    public static bool operator !=(C v1, C v2) { return false; }
    public override int GetHashCode() { return base.GetHashCode(); }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void CS0661WRN_EqualityOpWithoutGetHashCode()
        {
            var text = @"
class TestBase
{
    // This does not count; it has to be overridden on Test.
    public override int GetHashCode() { return 123; }
}
class Test : TestBase  // CS0661
{
    public static bool operator ==(object o, Test t)
   {
      return true;
   }
    public static bool operator !=(object o, Test t)
    {
        return true;
    }
    public override bool Equals(object o)
    {
        return true;
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
// (7,7): warning CS0659: 'Test' overrides Object.Equals(object o) but does not override Object.GetHashCode()
// class Test : TestBase  // CS0661
Diagnostic(ErrorCode.WRN_EqualsWithoutGetHashCode, "Test").WithArguments("Test"),
// (7,7): warning CS0661: 'Test' defines operator == or operator != but does not override Object.GetHashCode()
// class Test : TestBase  // CS0661
Diagnostic(ErrorCode.WRN_EqualityOpWithoutGetHashCode, "Test").WithArguments("Test")
                );
        }

        [Fact()]
        public void CS0672WRN_NonObsoleteOverridingObsolete()
        {
            var text = @"class MyClass
{
    [System.Obsolete]
    public virtual void ObsoleteMethod()
    {
    }
}

class MyClass2 : MyClass
{
    public override void ObsoleteMethod()   // CS0672
    {
    }
}

class MainClass
{
    static public void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_NonObsoleteOverridingObsolete, Line = 11, Column = 26, IsWarning = true });
        }

        [Fact()]
        public void CS0684WRN_CoClassWithoutComImport()
        {
            var text = @"
using System.Runtime.InteropServices;

[CoClass(typeof(C))] // CS0684
interface I
{
}

class C
{
    static void Main() { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_CoClassWithoutComImport, Line = 4, Column = 2, IsWarning = true });
        }

        [Fact()]
        public void CS0809WRN_ObsoleteOverridingNonObsolete()
        {
            var text = @"public class Base
{
    public virtual void Test1()
    {
    }
}
public class C : Base
{
    [System.Obsolete()]
    public override void Test1() // CS0809
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_ObsoleteOverridingNonObsolete, Line = 10, Column = 26, IsWarning = true });
        }

        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation01()
        {
            var source =
@"namespace NS
{
    public class C<T>
    {
        extern C();

        struct S
        {
            extern S(string s);
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,16): warning CS0824: Constructor 'NS.C<T>.C()' is marked external
                Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "C").WithArguments("NS.C<T>.C()").WithLocation(5, 16),
                // (9,20): warning CS0824: Constructor 'NS.C<T>.S.S(string)' is marked external
                Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "S").WithArguments("NS.C<T>.S.S(string)").WithLocation(9, 20));
        }

        [WorkItem(540859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540859")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation02()
        {
            var source =
@"class A : System.Attribute { }
class B
{
    extern static B();
}
class C
{
    [A] extern static C();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,19): warning CS0824: Constructor 'B.B()' is marked external
                Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "B").WithArguments("B.B()").WithLocation(4, 19));
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation03()
        {
            var source =
@"
public class A
{
    public A(int a) { }
}
public class B : A
{
  public extern B();
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped).
                           VerifyDiagnostics(
    // (8,17): warning CS0824: Constructor 'B.B()' is marked external
    //   public extern B();
    Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "B").WithArguments("B.B()").WithLocation(8, 17)
                                );

            var methods = verifier.TestData.GetMethodsByName().Keys;
            Assert.True(methods.Any(n => n.StartsWith("A..ctor", StringComparison.Ordinal)));
            Assert.False(methods.Any(n => n.StartsWith("B..ctor", StringComparison.Ordinal))); // Haven't tried to emit it
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(1036359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036359"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation04()
        {
            var source =
@"
public class A
{
    public A(int a) { }
}
public class B : A
{
  public extern B() : base(); // error
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            // Dev12 :  error CS1514: { expected
            //          error CS1513: } expected
            comp.VerifyDiagnostics(
    // (8,17): error CS8091: 'B.B()' cannot be extern and have a constructor initializer
    //   public extern B() : base(); // error
    Diagnostic(ErrorCode.ERR_ExternHasConstructorInitializer, "B").WithArguments("B.B()").WithLocation(8, 17)
                );
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(1036359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036359"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation05()
        {
            var source =
@"
public class A
{
    public A(int a) { }
}
public class B : A
{
  public extern B() : base(unknown); // error
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            // Dev12 :  error CS1514: { expected
            //          error CS1513: } expected
            comp.VerifyDiagnostics(
    // (8,17): error CS8091: 'B.B()' cannot be extern and have a constructor initializer
    //   public extern B() : base(unknown); // error
    Diagnostic(ErrorCode.ERR_ExternHasConstructorInitializer, "B").WithArguments("B.B()").WithLocation(8, 17)
                );
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(1036359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036359"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation06()
        {
            var source =
@"
public class A
{
    public A(int a) { }
}
public class B : A
{
  public extern B() : base(1) {}
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyDiagnostics(
    // (8,17): error CS8091: 'B.B()' cannot be extern and have a constructor initializer
    //   public extern B() : base(1) {}
    Diagnostic(ErrorCode.ERR_ExternHasConstructorInitializer, "B").WithArguments("B.B()").WithLocation(8, 17),
    // (8,17): error CS0179: 'B.B()' cannot be extern and declare a body
    //   public extern B() : base(1) {}
    Diagnostic(ErrorCode.ERR_ExternHasBody, "B").WithArguments("B.B()").WithLocation(8, 17)
                );
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation07()
        {
            var source =
@"
public class A
{
    public A(int a) { }
}
public class B : A
{
  public extern B() {}
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyDiagnostics(
    // (8,17): error CS0179: 'B.B()' cannot be extern and declare a body
    //   public extern B() {}
    Diagnostic(ErrorCode.ERR_ExternHasBody, "B").WithArguments("B.B()").WithLocation(8, 17)
                );
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation08()
        {
            var source =
@"
public class B
{
  private int x = 1;
  public extern B();
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyEmitDiagnostics(
    // (5,17): warning CS0824: Constructor 'B.B()' is marked external
    //   public extern B();
    Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "B").WithArguments("B.B()").WithLocation(5, 17),
    // (4,15): warning CS0414: The field 'B.x' is assigned but its value is never used
    //   private int x = 1;
    Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x").WithArguments("B.x").WithLocation(4, 15)
                );
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(1036359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036359"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation09()
        {
            var source =
@"
public class A
{
    public A() { }
}
public class B : A
{
  static extern B() : base(); // error
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyDiagnostics(
    // (8,23): error CS0514: 'B': static constructor cannot have an explicit 'this' or 'base' constructor call
    //   static extern B() : base(); // error
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "base").WithArguments("B").WithLocation(8, 23)
                );
        }

        [WorkItem(1084682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084682"), WorkItem(1036359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036359"), WorkItem(386, "CodePlex")]
        [Fact]
        public void CS0824WRN_ExternCtorNoImplementation10()
        {
            var source =
@"
public class A
{
    public A() { }
}
public class B : A
{
  static extern B() : base() {} // error
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyDiagnostics(
    // (8,23): error CS0514: 'B': static constructor cannot have an explicit 'this' or 'base' constructor call
    //   static extern B() : base() {} // error
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "base").WithArguments("B").WithLocation(8, 23),
    // (8,17): error CS0179: 'B.B()' cannot be extern and declare a body
    //   static extern B() : base() {} // error
    Diagnostic(ErrorCode.ERR_ExternHasBody, "B").WithArguments("B.B()").WithLocation(8, 17)
                );
        }

        [Fact]
        public void CS1066WRN_DefaultValueForUnconsumedLocation01()
        {
            // Slight change from the native compiler; in the native compiler the "int" gets the green squiggle.
            // This seems wrong; the error should either highlight the parameter "x" or the initializer " = 2".
            // I see no reason to highlight the "int". I've changed it to highlight the "x".

            var compilation = CreateCompilation(
@"interface IFace
{
    int Goo(int x = 1);
}
class B : IFace
{
    int IFace.Goo(int x = 2)
    {
        return 0;
    }
}");
            compilation.VerifyDiagnostics(
                // (7,23): error CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithLocation(7, 23).WithArguments("x"));
        }

        [Fact]
        public void CS1066WRN_DefaultValueForUnconsumedLocation02()
        {
            var compilation = CreateCompilation(
@"interface I
{
    object this[string index = null] { get; } //CS1066
    object this[char c, char d] { get; } //CS1066
}
class C : I
{
    object I.this[string index = ""apple""] { get { return null; } } //CS1066
    internal object this[int x, int y = 0] { get { return null; } } //fine
    object I.this[char c = 'c', char d = 'd'] { get { return null; } } //CS1066 x2
}
");
            compilation.VerifyDiagnostics(
                // (3,24): warning CS1066: The default value specified for parameter 'index' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "index").WithArguments("index"),
                // (7,26): warning CS1066: The default value specified for parameter 'index' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "index").WithArguments("index"),
                // (10,24): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
                // (10,38): warning CS1066: The default value specified for parameter 'd' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "d").WithArguments("d"));
        }

        [Fact]
        public void CS1066WRN_DefaultValueForUnconsumedLocation03()
        {
            var compilation = CreateCompilation(
@"
class C 
{
    public static C operator!(C c = null) { return c; }
    public static implicit operator int(C c = null) { return 0; }
}
");
            compilation.VerifyDiagnostics(
// (4,33): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
//     public static C operator!(C c = null) { return c; }
Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
// (5,43): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
//     public static implicit operator int(C c = null) { return 0; }
Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c")
);
        }

        // public void CS1698WRN_AssumedMatchThis() => Move to CommandLineTest

        [Fact]
        public void CS1699WRN_UseSwitchInsteadOfAttribute_RoslynWRN73()
        {
            var text = @"
[assembly:System.Reflection.AssemblyDelaySign(true)]   // CS1699
";

            // warning CS1699: Use command line option '/delaysign' or appropriate project settings instead of 'System.Reflection.AssemblyDelaySign'
            // Diagnostic(ErrorCode.WRN_UseSwitchInsteadOfAttribute, @"/delaysign").WithArguments(@"/delaysign", "System.Reflection.AssemblyDelaySign")
            // warning CS1607: Assembly generation -- Delay signing was requested, but no key was given

            CreateCompilation(text).VerifyDiagnostics(
                // warning CS73: Delay signing was specified and requires a public key, but no public key was specified
                Diagnostic(ErrorCode.WRN_DelaySignButNoKey)
                );
        }

        [Fact(), WorkItem(544447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544447")]
        public void CS1700WRN_InvalidAssemblyName()
        {
            var text = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""'   '"")]                    // ok
[assembly: InternalsVisibleTo(""\t\r\n;a"")]                 // ok (whitespace escape)
[assembly: InternalsVisibleTo(""\u1234;a"")]                 // ok (assembly name Unicode escape)
[assembly: InternalsVisibleTo(""' a '"")]                    // ok
[assembly: InternalsVisibleTo(""\u1000000;a"")]              // invalid escape
[assembly: InternalsVisibleTo(""a'b'c"")]                    // quotes in the middle
[assembly: InternalsVisibleTo(""Test, PublicKey=Null"")]
[assembly: InternalsVisibleTo(""Test, Bar"")]
[assembly: InternalsVisibleTo(""Test, Version"")]
[assembly: InternalsVisibleTo(""app2, Retargetable=f"")]   // CS1700
";

            // Tested against Dev12
            CreateCompilation(text).VerifyDiagnostics(
                // (8,12): warning CS1700: Assembly reference 'a'b'c' is invalid and cannot be resolved
                Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"InternalsVisibleTo(""a'b'c"")").WithArguments("a'b'c").WithLocation(8, 12),
                // (9,12): warning CS1700: Assembly reference 'Test, PublicKey=Null' is invalid and cannot be resolved
                Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"InternalsVisibleTo(""Test, PublicKey=Null"")").WithArguments("Test, PublicKey=Null").WithLocation(9, 12),
                // (10,12): warning CS1700: Assembly reference 'Test, Bar' is invalid and cannot be resolved
                Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"InternalsVisibleTo(""Test, Bar"")").WithArguments("Test, Bar").WithLocation(10, 12),
                // (11,12): warning CS1700: Assembly reference 'Test, Version' is invalid and cannot be resolved
                Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"InternalsVisibleTo(""Test, Version"")").WithArguments("Test, Version").WithLocation(11, 12),
                // (12,12): warning CS1700: Assembly reference 'app2, Retargetable=f' is invalid and cannot be resolved
                Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"InternalsVisibleTo(""app2, Retargetable=f"")").WithArguments("app2, Retargetable=f").WithLocation(12, 12));
        }

        // CS1701WRN_UnifyReferenceMajMin --> ReferenceManagerTests

        [Fact]
        public void CS1956WRN_MultipleRuntimeImplementationMatches()
        {
            var text = @"class Base<T, S>
{
    public virtual int Test(out T x) // CS1956
    {
        x = default(T);
        return 0;
    }

    public virtual int Test(ref S x)
    {
        return 1;
    }
}

interface IFace
{
    int Test(out int x);
}

class Derived : Base<int, int>, IFace
{
    static void Main()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_MultipleRuntimeImplementationMatches, Line = 20, Column = 33, IsWarning = true });
        }

        [Fact]
        public void CS1957WRN_MultipleRuntimeOverrideMatches()
        {
            var text =
@"class Base<TString>
{
    public virtual void Test(TString s, out int x)
    {
        x = 0;
    }

    public virtual void Test(string s, ref int x) { } // CS1957
}

class Derived : Base<string>
{
    public override void Test(string s, ref int x) { }
}
";
            // We no longer report a runtime ambiguous override (CS1957) because the compiler produces a methodimpl record to disambiguate.
            CSharpCompilation comp = CreateCompilation(text, targetFramework: TargetFramework.NetLatest);
            Assert.Equal(RuntimeUtilities.IsCoreClrRuntime, comp.Assembly.RuntimeSupportsCovariantReturnsOfClasses);
            if (comp.Assembly.RuntimeSupportsDefaultInterfaceImplementation)
            {
                comp.VerifyDiagnostics(
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (8,25): warning CS1957: Member 'Derived.Test(string, ref int)' overrides 'Base<string>.Test(string, ref int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called. Please use a newer runtime.
                    //     public virtual void Test(string s, ref int x) { } // CS1957
                    Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Test").WithArguments("Base<string>.Test(string, ref int)", "Derived.Test(string, ref int)").WithLocation(8, 25)
                    );
            }
        }

        [Fact]
        public void CS3000WRN_CLS_NoVarArgs()
        {
            var text = @"
[assembly: System.CLSCompliant(true)]
public class Test
{
    public void AddABunchOfInts( __arglist) { }   // CS3000
    public static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,17): warning CS3000: Methods with variable arguments are not CLS-compliant
                //     public void AddABunchOfInts( __arglist) { }   // CS3000
                Diagnostic(ErrorCode.WRN_CLS_NoVarArgs, "AddABunchOfInts"));
        }

        [Fact]
        public void CS3001WRN_CLS_BadArgType()
        {
            var text = @"[assembly: System.CLSCompliant(true)]
public class @a
{
    public void bad(ushort i)   // CS3001
    {
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,28): warning CS3001: Argument type 'ushort' is not CLS-compliant
                //     public void bad(ushort i)   // CS3001
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "i").WithArguments("ushort"));
        }

        [Fact]
        public void CS3002WRN_CLS_BadReturnType()
        {
            var text = @"[assembly: System.CLSCompliant(true)]
public class @a
{
    public ushort bad()   // CS3002, public method
    {
        return ushort.MaxValue;
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,19): warning CS3002: Return type of 'a.bad()' is not CLS-compliant
                //     public ushort bad()   // CS3002, public method
                Diagnostic(ErrorCode.WRN_CLS_BadReturnType, "bad").WithArguments("a.bad()"));
        }

        [Fact]
        public void CS3003WRN_CLS_BadFieldPropType()
        {
            var text = @"[assembly: System.CLSCompliant(true)]
public class @a
{
    public ushort a1;   // CS3003, public variable
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,19): warning CS3003: Type of 'a.a1' is not CLS-compliant
                //     public ushort a1;   // CS3003, public variable
                Diagnostic(ErrorCode.WRN_CLS_BadFieldPropType, "a1").WithArguments("a.a1"));
        }

        [Fact]
        public void CS3005WRN_CLS_BadIdentifierCase()
        {
            var text = @"using System;

[assembly: CLSCompliant(true)]
public class @a
{
    public static int a1 = 0;
    public static int A1 = 1;   // CS3005

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,23): warning CS3005: Identifier 'a.A1' differing only in case is not CLS-compliant
                //     public static int A1 = 1;   // CS3005
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, "A1").WithArguments("a.A1"));
        }

        [Fact]
        public void CS3006WRN_CLS_OverloadRefOut()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
public class MyClass
{
    public void f(int i)
    {
    }

    public void f(ref int i)   // CS3006
    {
    }

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,17): warning CS3006: Overloaded method 'MyClass.f(ref int)' differing only in ref or out, or in array rank, is not CLS-compliant
                //     public void f(ref int i)   // CS3006
                Diagnostic(ErrorCode.WRN_CLS_OverloadRefOut, "f").WithArguments("MyClass.f(ref int)"));
        }

        [Fact]
        public void CS3007WRN_CLS_OverloadUnnamed()
        {
            var text = @"[assembly: System.CLSCompliant(true)]
public struct S
{
    public void F(int[][] array) { }
    public void F(byte[][] array) { }  // CS3007
    public static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,17): warning CS3007: Overloaded method 'S.F(byte[][])' differing only by unnamed array types is not CLS-compliant
                //     public void F(byte[][] array) { }  // CS3007
                Diagnostic(ErrorCode.WRN_CLS_OverloadUnnamed, "F").WithArguments("S.F(byte[][])"));
        }

        [Fact]
        public void CS3008WRN_CLS_BadIdentifier()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
public class @a
{
    public static int _a = 0;  // CS3008
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,23): warning CS3008: Identifier '_a' is not CLS-compliant
                //     public static int _a = 0;  // CS3008
                Diagnostic(ErrorCode.WRN_CLS_BadIdentifier, "_a").WithArguments("_a"));
        }

        [Fact]
        public void CS3009WRN_CLS_BadBase()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
[CLSCompliant(false)]
public class B
{
}
public class C : B   // CS3009
{
    public static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,14): warning CS3009: 'C': base type 'B' is not CLS-compliant
                // public class C : B   // CS3009
                Diagnostic(ErrorCode.WRN_CLS_BadBase, "C").WithArguments("C", "B"));
        }

        [Fact]
        public void CS3010WRN_CLS_BadInterfaceMember()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
public interface I
{
    [CLSCompliant(false)]
    int M();   // CS3010
}
public class C : I
{
    public int M()
    {
        return 1;
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,9): warning CS3010: 'I.M()': CLS-compliant interfaces must have only CLS-compliant members
                //     int M();   // CS3010
                Diagnostic(ErrorCode.WRN_CLS_BadInterfaceMember, "M").WithArguments("I.M()"));
        }

        [Fact]
        public void CS3011WRN_CLS_NoAbstractMembers()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
public abstract class I
{
    [CLSCompliant(false)]
    public abstract int M();   // CS3011
}
public class C : I
{
    public override int M()
    {
        return 1;
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,25): warning CS3011: 'I.M()': only CLS-compliant members can be abstract
                //     public abstract int M();   // CS3011
                Diagnostic(ErrorCode.WRN_CLS_NoAbstractMembers, "M").WithArguments("I.M()"));
        }

        [Fact]
        public void CS3012WRN_CLS_NotOnModules()
        {
            var text = @"[module: System.CLSCompliant(true)]   // CS3012
public class C
{
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,10): warning CS3012: You must specify the CLSCompliant attribute on the assembly, not the module, to enable CLS compliance checking
                // [module: System.CLSCompliant(true)]   // CS3012
                Diagnostic(ErrorCode.WRN_CLS_NotOnModules, "System.CLSCompliant(true)"));
        }

        [Fact]
        public void CS3013WRN_CLS_ModuleMissingCLS()
        {
            var netModule = CreateEmptyCompilation("", options: TestOptions.ReleaseModule, assemblyName: "lib").EmitToImageReference(expectedWarnings: new[] { Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion) });
            CreateCompilation("[assembly: System.CLSCompliant(true)]", new[] { netModule }).VerifyDiagnostics(
                // lib.netmodule: warning CS3013: Added modules must be marked with the CLSCompliant attribute to match the assembly
                Diagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS));
        }

        [Fact]
        public void CS3014WRN_CLS_AssemblyNotCLS()
        {
            var text = @"using System;
// [assembly:CLSCompliant(true)]
public class I
{
    [CLSCompliant(true)]   // CS3014
    public void M()
    {
    }

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,17): warning CS3014: 'I.M()' cannot be marked as CLS-compliant because the assembly does not have a CLSCompliant attribute
                //     public void M()
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS, "M").WithArguments("I.M()"));
        }

        [Fact]
        public void CS3015WRN_CLS_BadAttributeType()
        {
            var text = @"using System;

[assembly: CLSCompliant(true)]
public class MyAttribute : Attribute
{
    public MyAttribute(int[] ai) { }   // CS3015
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,14): warning CS3015: 'MyAttribute' has no accessible constructors which use only CLS-compliant types
                // public class MyAttribute : Attribute
                Diagnostic(ErrorCode.WRN_CLS_BadAttributeType, "MyAttribute").WithArguments("MyAttribute"));
        }

        [Fact]
        public void CS3016WRN_CLS_ArrayArgumentToAttribute()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
[C(new int[] { 1, 2 })]   // CS3016
public class C : Attribute
{
    public C()
    {
    }
    public C(int[] a)
    {
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,2): warning CS3016: Arrays as attribute arguments is not CLS-compliant
                // [C(new int[] { 1, 2 })]   // CS3016
                Diagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, "C(new int[] { 1, 2 })"));
        }

        [Fact]
        public void CS3017WRN_CLS_NotOnModules2()
        {
            var text = @"using System;
[module: CLSCompliant(true)]
[assembly: CLSCompliant(false)]  // CS3017
class C
{
    static void Main() { }
}
";
            // NOTE: unlike dev11, roslyn assumes that [assembly:CLSCompliant(false)] means
            // "suppress all CLS diagnostics".
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void CS3018WRN_CLS_IllegalTrueInFalse()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
[CLSCompliant(false)]
public class Outer
{
    [CLSCompliant(true)]   // CS3018
    public class Nested { }
    [CLSCompliant(false)]
    public class Nested3 { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,18): warning CS3018: 'Outer.Nested' cannot be marked as CLS-compliant because it is a member of non-CLS-compliant type 'Outer'
                //     public class Nested { }
                Diagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, "Nested").WithArguments("Outer.Nested", "Outer"));
        }

        [Fact]
        public void CS3019WRN_CLS_MeaninglessOnPrivateType()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
[CLSCompliant(true)]  // CS3019
class C
{
    [CLSCompliant(false)]  // CS3019
    void Test()
    {
    }
    static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,7): warning CS3019: CLS compliance checking will not be performed on 'C' because it is not visible from outside this assembly
                // class C
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "C").WithArguments("C"),
                // (7,10): warning CS3019: CLS compliance checking will not be performed on 'C.Test()' because it is not visible from outside this assembly
                //     void Test()
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, "Test").WithArguments("C.Test()"));
        }

        [Fact]
        public void CS3021WRN_CLS_AssemblyNotCLS2()
        {
            var text = @"using System;
[CLSCompliant(false)]               // CS3021
public class C
{
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,14): warning CS3021: 'C' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
                // public class C
                Diagnostic(ErrorCode.WRN_CLS_AssemblyNotCLS2, "C").WithArguments("C"));
        }

        [Fact]
        public void CS3022WRN_CLS_MeaninglessOnParam()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]
[CLSCompliant(true)]
public class C
{
    public void F([CLSCompliant(true)] int i)
    {
    }
    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (6,20): warning CS3022: CLSCompliant attribute has no meaning when applied to parameters. Try putting it on the method instead.
                //     public void F([CLSCompliant(true)] int i)
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, "CLSCompliant(true)"));
        }

        [Fact]
        public void CS3023WRN_CLS_MeaninglessOnReturn()
        {
            var text = @"[assembly: System.CLSCompliant(true)]
public class Test
{
    [return: System.CLSCompliant(true)]  // CS3023
    public static int Main()
    {
        return 0;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,14): warning CS3023: CLSCompliant attribute has no meaning when applied to return types. Try putting it on the method instead.
                //     [return: System.CLSCompliant(true)]  // CS3023
                Diagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, "System.CLSCompliant(true)"));
        }

        [Fact]
        public void CS3024WRN_CLS_BadTypeVar()
        {
            var text = @"[assembly: System.CLSCompliant(true)]

[type: System.CLSCompliant(false)]
public class TestClass // CS3024
{
    public ushort us;
}
[type: System.CLSCompliant(false)]
public interface ITest // CS3024
{ }
public interface I<T> where T : TestClass
{ }
public class TestClass_2<T> where T : ITest
{ }
public class TestClass_3<T> : I<T> where T : TestClass
{ }
public class TestClass_4<T> : TestClass_2<T> where T : ITest
{ }
public class Test
{
    public static int Main()
    {
        return 0;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,20): warning CS3024: Constraint type 'TestClass' is not CLS-compliant
                // public interface I<T> where T : TestClass
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("TestClass"),
                // (13,26): warning CS3024: Constraint type 'ITest' is not CLS-compliant
                // public class TestClass_2<T> where T : ITest
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("ITest"),
                // (17,26): warning CS3024: Constraint type 'ITest' is not CLS-compliant
                // public class TestClass_4<T> : TestClass_2<T> where T : ITest
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("ITest"),
                // (15,26): warning CS3024: Constraint type 'TestClass' is not CLS-compliant
                // public class TestClass_3<T> : I<T> where T : TestClass
                Diagnostic(ErrorCode.WRN_CLS_BadTypeVar, "T").WithArguments("TestClass"));
        }

        [Fact]
        public void CS3026WRN_CLS_VolatileField()
        {
            var text = @"[assembly: System.CLSCompliant(true)]
public class Test
{
    public volatile int v0 = 0;   // CS3026
    public static void Main() { }
}

";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_CLS_VolatileField, Line = 4, Column = 25, IsWarning = true });
        }

        [Fact]
        public void CS3027WRN_CLS_BadInterface()
        {
            var text = @"using System;
[assembly: CLSCompliant(true)]

public interface I1
{
}

[CLSCompliant(false)]
public interface I2
{
}

public interface I : I1, I2
{
}

public class Goo
{
    static void Main() { }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.WRN_CLS_BadInterface, Line = 13, Column = 18, IsWarning = true });
        }

        #endregion

        #region "Regressions or Mixed errors"

        [Fact] // bug 1985
        public void ConstructWithErrors()
        {
            var text = @"
class Base<T>{}
class Derived : Base<NotFound>{}";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text, new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 3, Column = 22 });
            var derived = comp.SourceModule.GlobalNamespace.GetTypeMembers("Derived").Single();
            var Base = derived.BaseType();
            Assert.Equal(TypeKind.Class, Base.TypeKind);
        }

        [Fact] // bug 3045
        public void AliasQualifiedName00()
        {
            var text = @"using NSA = A;
namespace A
{
    class Goo { }
}
namespace B
{
    class Test
    {
        class NSA
        {
            public NSA(int Goo) { this.Goo = Goo; }
            int Goo;
        }
        static int Main()
        {
            NSA::Goo goo = new NSA::Goo(); // shouldn't error here
            goo = Xyzzy;
            return 0;
        }
        static NSA::Goo Xyzzy = null; // shouldn't error here
    }
}";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);

            var b = comp.SourceModule.GlobalNamespace.GetMembers("B").Single() as NamespaceSymbol;
            var test = b.GetMembers("Test").Single() as NamedTypeSymbol;
            var nsa = test.GetMembers("NSA").Single() as NamedTypeSymbol;
            Assert.Equal(2, nsa.GetMembers().Length);
        }

        [WorkItem(538218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538218")]
        [Fact]
        public void RecursiveInterfaceLookup01()
        {
            var text = @"interface A<T> { }
interface B : A<B.Garbage> { }";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DottedTypeNameNotFoundInAgg, Line = 2, Column = 19 });

            var b = comp.SourceModule.GlobalNamespace.GetMembers("B").Single() as NamespaceSymbol;
        }

        [WorkItem(538150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538150")]
        [Fact]
        public void CS0102ERR_DuplicateNameInClass04()
        {
            var text = @"

namespace NS
{
    struct MyType
    {
        public class MyMeth { }
        public void MyMeth() { }
        public int MyMeth;
    }
}
";
            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (8,21): error CS0102: The type 'NS.MyType' already contains a definition for 'MyMeth'
                //         public void MyMeth() { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "MyMeth").WithArguments("NS.MyType", "MyMeth"),
                // (9,20): error CS0102: The type 'NS.MyType' already contains a definition for 'MyMeth'
                //         public int MyMeth;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "MyMeth").WithArguments("NS.MyType", "MyMeth"),
                // (9,20): warning CS0649: Field 'NS.MyType.MyMeth' is never assigned to, and will always have its default value 0
                //         public int MyMeth;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "MyMeth").WithArguments("NS.MyType.MyMeth", "0")
                );

            var ns = comp.SourceModule.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            var type1 = ns.GetMembers("MyType").Single() as NamedTypeSymbol;
            Assert.Equal(4, type1.GetMembers().Length); // constructor included
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass05()
        {
            CreateCompilation(
@"class C
{
    void P() { }
    int P { get { return 0; } }
    object Q { get; set; }
    void Q(int x, int y) { }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 9),
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Q").WithArguments("C", "Q").WithLocation(6, 10));
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass06()
        {
            CreateCompilation(
@"class C
{
    private double get_P; // CS0102
    private int set_P { get { return 0; } } // CS0102
    void get_Q(object o) { } // no error
    class set_Q { } // CS0102
    public int P { get; set; }
    object Q { get { return null; } }
    object R { set { } }
    enum get_R { } // CS0102
    struct set_R { } // CS0102
}")
                .VerifyDiagnostics(
                    // (7,20): error CS0102: The type 'C' already contains a definition for 'get_P'
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("C", "get_P"),
                    // (7,25): error CS0102: The type 'C' already contains a definition for 'set_P'
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("C", "set_P"),
                    // (8,12): error CS0102: The type 'C' already contains a definition for 'set_Q'
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Q").WithArguments("C", "set_Q"),
                    // (9,12): error CS0102: The type 'C' already contains a definition for 'get_R'
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "R").WithArguments("C", "get_R"),
                    // (9,16): error CS0102: The type 'C' already contains a definition for 'set_R'
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("C", "set_R"),
                    // (3,20): warning CS0169: The field 'C.get_P' is never used
                    //     private double get_P; // CS0102
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "get_P").WithArguments("C.get_P"));
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass07()
        {
            CreateCompilation(
@"class C
{
    public int P
    {
        get { return 0; }
        set { }
    }
    public bool P { get { return false; } }
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(8, 17));
        }

        [WorkItem(538616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538616")]
        [Fact]
        public void CS0102ERR_DuplicateNameInClass08()
        {
            var text = @"
class A<T>
{
    void T()
    {
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInClass, Line = 4, Column = 10 });
        }

        [WorkItem(538917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538917")]
        [Fact]
        public void CS0102ERR_DuplicateNameInClass09()
        {
            var text = @"
class A<T>
{
    class T<S> { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_DuplicateNameInClass, Line = 4, Column = 11 });
        }

        [WorkItem(538634, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538634")]
        [Fact]
        public void CS0102ERR_DuplicateNameInClass10()
        {
            var text =
@"class C<A, B, D, E, F, G>
{
    object A;
    void B() { }
    object D { get; set; }
    class E { }
    struct F { }
    enum G { }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,12): error CS0102: The type 'C<A, B, D, E, F, G>' already contains a definition for 'A'
                //     object A;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "A").WithArguments("C<A, B, D, E, F, G>", "A"),
                // (4,10): error CS0102: The type 'C<A, B, D, E, F, G>' already contains a definition for 'B'
                //     void B() { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "B").WithArguments("C<A, B, D, E, F, G>", "B"),
                // (5,12): error CS0102: The type 'C<A, B, D, E, F, G>' already contains a definition for 'D'
                //     object D { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "D").WithArguments("C<A, B, D, E, F, G>", "D"),
                // (6,11): error CS0102: The type 'C<A, B, D, E, F, G>' already contains a definition for 'E'
                //     class E { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C<A, B, D, E, F, G>", "E"),
                // (7,12): error CS0102: The type 'C<A, B, D, E, F, G>' already contains a definition for 'F'
                //     struct F { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "F").WithArguments("C<A, B, D, E, F, G>", "F"),
                // (8,10): error CS0102: The type 'C<A, B, D, E, F, G>' already contains a definition for 'G'
                //     enum G { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "G").WithArguments("C<A, B, D, E, F, G>", "G"),
                // (3,12): warning CS0169: The field 'C<A, B, D, E, F, G>.A' is never used
                //     object A;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "A").WithArguments("C<A, B, D, E, F, G>.A"));
        }

        [Fact]
        public void CS0101ERR_DuplicateNameInNS05()
        {
            CreateCompilation(
@"namespace N
{
    enum E { A, B }
    enum E { A } // CS0101
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "E").WithArguments("E", "N").WithLocation(4, 10));
        }

        [WorkItem(528149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528149")]
        [Fact]
        public void CS0101ERR_DuplicateNameInNS06()
        {
            CreateCompilation(
@"namespace N
{
    interface I
    {
        int P { get; }
        object Q { get; }
    }
    interface I // CS0101
    {
        object Q { get; set; }
    }
    struct S
    {
        class T { }
        interface I { }
    }
    struct S // CS0101
    {
        struct T { } // Dev10 reports CS0102 for T!
        int I;
    }
    class S // CS0101
    {
        object T;
    }
    delegate void D();
    delegate int D(int i);
}")
            .VerifyDiagnostics(
                // (8,15): error CS0101: The namespace 'N' already contains a definition for 'I'
                //     interface I // CS0101
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "I").WithArguments("I", "N"),
                // (17,12): error CS0101: The namespace 'N' already contains a definition for 'S'
                //     struct S // CS0101
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "S").WithArguments("S", "N"),
                // (22,11): error CS0101: The namespace 'N' already contains a definition for 'S'
                //     class S // CS0101
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "S").WithArguments("S", "N"),
                // (27,18): error CS0101: The namespace 'N' already contains a definition for 'D'
                //     delegate int D(int i);
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "D").WithArguments("D", "N"),
                // (24,16): warning CS0169: The field 'N.S.T' is never used
                //         object T;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "T").WithArguments("N.S.T"),
                // (20,13): warning CS0169: The field 'N.S.I' is never used
                //         int I;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "I").WithArguments("N.S.I")
                );
        }

        [WorkItem(539742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539742")]
        [Fact]
        public void CS0102ERR_DuplicateNameInClass11()
        {
            CreateCompilation(
@"class C
{
    enum E { A, B }
    enum E { A } // CS0102
}")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 10));
        }

        [WorkItem(528149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528149")]
        [Fact]
        public void CS0102ERR_DuplicateNameInClass12()
        {
            CreateCompilation(
@"class C
{
    interface I
    {
        int P { get; }
        object Q { get; }
    }
    interface I // CS0102
    {
        object Q { get; set; }
    }
    struct S
    {
        class T { }
        interface I { }
    }
    struct S // CS0102
    {
        struct T { } // Dev10 reports CS0102 for T!
        int I;
    }
    class S // CS0102
    {
        object T;
    }
    delegate void D();
    delegate int D(int i);
}")
            .VerifyDiagnostics(
                // (8,15): error CS0102: The type 'C' already contains a definition for 'I'
                //     interface I // CS0102
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "I").WithArguments("C", "I"),
                // (17,12): error CS0102: The type 'C' already contains a definition for 'S'
                //     struct S // CS0102
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("C", "S"),
                // (22,11): error CS0102: The type 'C' already contains a definition for 'S'
                //     class S // CS0102
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("C", "S"),
                // (27,18): error CS0102: The type 'C' already contains a definition for 'D'
                //     delegate int D(int i);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "D").WithArguments("C", "D"),
                // (24,16): warning CS0169: The field 'C.S.T' is never used
                //         object T;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "T").WithArguments("C.S.T"),
                // (20,13): warning CS0169: The field 'C.S.I' is never used
                //         int I;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "I").WithArguments("C.S.I")
                );
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass13()
        {
            CreateCompilation(
@"class C
{
    private double add_E; // CS0102
    private event System.Action remove_E; // CS0102
    void add_F(object o) { } // no error
    class remove_F { } // CS0102
    public event System.Action E;
    event System.Action F;
    event System.Action G;
    enum add_G { } // CS0102
    struct remove_G { } // CS0102
}")
            .VerifyDiagnostics(
                // (72): error CS0102: The type 'C' already contains a definition for 'add_E'
                //     public event System.Action E;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "add_E"),
                // (72): error CS0102: The type 'C' already contains a definition for 'remove_E'
                //     public event System.Action E;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "remove_E"),
                // (8,25): error CS0102: The type 'C' already contains a definition for 'remove_F'
                //     event System.Action F;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "F").WithArguments("C", "remove_F"),
                // (9,25): error CS0102: The type 'C' already contains a definition for 'add_G'
                //     event System.Action G;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "G").WithArguments("C", "add_G"),
                // (9,25): error CS0102: The type 'C' already contains a definition for 'remove_G'
                //     event System.Action G;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "G").WithArguments("C", "remove_G"),
                // (8,25): warning CS0067: The event 'C.F' is never used
                //     event System.Action F;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "F").WithArguments("C.F"),
                // (3,20): warning CS0169: The field 'C.add_E' is never used
                //     private double add_E; // CS0102
                Diagnostic(ErrorCode.WRN_UnreferencedField, "add_E").WithArguments("C.add_E"),
                // (4,33): warning CS0067: The event 'C.remove_E' is never used
                //     private event System.Action remove_E; // CS0102
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "remove_E").WithArguments("C.remove_E"),
                // (72): warning CS0067: The event 'C.E' is never used
                //     public event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"),
                // (9,25): warning CS0067: The event 'C.G' is never used
                //     event System.Action G;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "G").WithArguments("C.G"));
        }

        [Fact]
        public void CS0102ERR_DuplicateNameInClass14()
        {
            var text =
@"using System.Runtime.CompilerServices;
interface I
{
    object this[object index] { get; set; }
    void Item();
}
struct S
{
    [IndexerName(""P"")]
    object this[object index] { get { return null; } }
    object Item; // no error
    object P;
}
class A
{
    object get_Item;
    object set_Item;
    object this[int x, int y] { set { } }
}
class B
{
    [IndexerName(""P"")]
    object this[object index] { get { return null; } }
    object get_Item; // no error
    object get_P;
}
class A1
{
    internal object this[object index] { get { return null; } }
}
class A2
{
    internal object Item; // no error
}
class B1 : A1
{
    internal object Item;
}
class B2 : A2
{
    internal object this[object index] { get { return null; } } // no error
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,12): error CS0102: The type 'I' already contains a definition for 'Item'
                //     object this[object index] { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("I", "Item"),
                // (10,12): error CS0102: The type 'S' already contains a definition for 'P'
                //     object this[object index] { get { return null; } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("S", "P"),
                // (18,12): error CS0102: The type 'A' already contains a definition for 'get_Item'
                //     object this[int x, int y] { set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "get_Item"),
                // (18,33): error CS0102: The type 'A' already contains a definition for 'set_Item'
                //     object this[int x, int y] { set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("A", "set_Item"),
                // (23,33): error CS0102: The type 'B' already contains a definition for 'get_P'
                //     object this[object index] { get { return null; } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("B", "get_P"),
                // (11,12): warning CS0169: The field 'S.Item' is never used
                //     object Item; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Item").WithArguments("S.Item"),
                // (12,12): warning CS0169: The field 'S.P' is never used
                //     object P;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "P").WithArguments("S.P"),
                // (16,12): warning CS0169: The field 'A.get_Item' is never used
                //     object get_Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "get_Item").WithArguments("A.get_Item"),
                // (17,12): warning CS0169: The field 'A.set_Item' is never used
                //     object set_Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "set_Item").WithArguments("A.set_Item"),
                // (24,12): warning CS0169: The field 'B.get_Item' is never used
                //     object get_Item; // no error
                Diagnostic(ErrorCode.WRN_UnreferencedField, "get_Item").WithArguments("B.get_Item"),
                // (25,12): warning CS0169: The field 'B.get_P' is never used
                //     object get_P;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "get_P").WithArguments("B.get_P"),
                // (33,21): warning CS0649: Field 'A2.Item' is never assigned to, and will always have its default value null
                //     internal object Item; // no error
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Item").WithArguments("A2.Item", "null"),
                // (37,21): warning CS0649: Field 'B1.Item' is never assigned to, and will always have its default value null
                //     internal object Item;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Item").WithArguments("B1.Item", "null")
                );
        }

        // Indexers without IndexerNameAttribute
        [Fact]
        public void CS0102ERR_DuplicateNameInClass15()
        {
            var template = @"
class A
{{
    public int this[int x] {{ set {{ }} }}
    {0}
}}";
            CreateCompilation(string.Format(template, "int Item;")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'Item'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "Item"),
                // (5,9): warning CS0169: The field 'A.Item' is never used
                //     int Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Item").WithArguments("A.Item"));

            // Error even though the indexer doesn't have a getter
            CreateCompilation(string.Format(template, "int get_Item;")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'get_Item'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "get_Item"),
                // (5,9): warning CS0169: The field 'A.get_Item' is never used
                //     int get_Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "get_Item").WithArguments("A.get_Item"));

            CreateCompilation(string.Format(template, "int set_Item;")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'set_Item'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("A", "set_Item"),
                // (5,9): warning CS0169: The field 'A.set_Item' is never used
                //     int set_Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "set_Item").WithArguments("A.set_Item"));

            // Error even though the signatures don't match
            CreateCompilation(string.Format(template, "int Item() { return 0; }")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'Item'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "Item"));

            // Since the signatures don't match
            CreateCompilation(string.Format(template, "int set_Item() { return 0; }")).VerifyDiagnostics();
        }

        // Indexers with IndexerNameAttribute
        [Fact]
        public void CS0102ERR_DuplicateNameInClass16()
        {
            var template = @"
using System.Runtime.CompilerServices;
class A
{{
    [IndexerName(""P"")]
    public int this[int x] {{ set {{ }} }}
    {0}
}}";
            CreateCompilation(string.Format(template, "int P;")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'P'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "P"),
                // (7,9): warning CS0169: The field 'A.P' is never used
                //     int P;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "P").WithArguments("A.P"));

            // Error even though the indexer doesn't have a getter
            CreateCompilation(string.Format(template, "int get_P;")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'get_P'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "get_P"),
                // (7,9): warning CS0169: The field 'A.get_P' is never used
                //     int get_P;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "get_P").WithArguments("A.get_P"));

            CreateCompilation(string.Format(template, "int set_P;")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'set_P'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("A", "set_P"),
                // (7,9): warning CS0169: The field 'A.set_P' is never used
                //     int set_P;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "set_P").WithArguments("A.set_P"));

            // Error even though the signatures don't match
            CreateCompilation(string.Format(template, "int P() { return 0; }")).VerifyDiagnostics(
                // (4,16): error CS0102: The type 'A' already contains a definition for 'P'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("A", "P"));

            // Since the signatures don't match
            CreateCompilation(string.Format(template, "int set_P() { return 0; }")).VerifyDiagnostics();

            // No longer have issues with "Item" names
            CreateCompilation(string.Format(template, "int Item;")).VerifyDiagnostics(
                // (7,9): warning CS0169: The field 'A.Item' is never used
                //     int Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Item").WithArguments("A.Item"));
            CreateCompilation(string.Format(template, "int get_Item;")).VerifyDiagnostics(
                // (7,9): warning CS0169: The field 'A.get_Item' is never used
                //     int get_Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "get_Item").WithArguments("A.get_Item"));
            CreateCompilation(string.Format(template, "int set_Item;")).VerifyDiagnostics(
                // (7,9): warning CS0169: The field 'A.set_Item' is never used
                //     int set_Item;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "set_Item").WithArguments("A.set_Item"));
            CreateCompilation(string.Format(template, "int Item() { return 0; }")).VerifyDiagnostics();
            CreateCompilation(string.Format(template, "int set_Item() { return 0; }")).VerifyDiagnostics();
        }

        [WorkItem(539625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539625")]
        [Fact]
        public void CS0104ERR_AmbigContext02()
        {
            var text = @"

namespace Conformance.Expressions
{
    using LevelOne.LevelTwo;
    using LevelOne.LevelTwo.LevelThree;

    public class Test
    {
        public static int Main()
        {
            return I<string>.Method(); // CS0104
        }
    }

    namespace LevelOne
    {
        namespace LevelTwo
        {
            public class I<A1>
            {
                public static int Method()
                {
                    return 1;
                }
            }

            namespace LevelThree
            {
                public class I<A1>
                {
                    public static int Method()
                    {
                        return 2;
                    }
                }
            }
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AmbigContext, Line = 12, Column = 20 });
        }

        [Fact]
        public void CS0104ERR_AmbigContext03()
        {
            var text = @"

namespace Conformance.Expressions
{
    using LevelOne.LevelTwo;
    using LevelOne.LevelTwo.LevelThree;

    public class Test
    {
        public static int Main()
        {
            return I.Method(); // CS0104
        }
    }

    namespace LevelOne
    {
        namespace LevelTwo
        {
            public class I
            {
                public static int Method()
                {
                    return 1;
                }
            }

            namespace LevelThree
            {
                public class I
                {
                    public static int Method()
                    {
                        return 2;
                    }
                }
            }
        }
    }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_AmbigContext, Line = 12, Column = 20 });
        }

        [WorkItem(540255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540255")]
        [Fact]
        public void CS0122ERR_BadAccess01()
        {
            var text = @"

interface I<T> { }

class A
{
    private class B { }
    public class C : I<B> { }
}

class Program
{
    static void Main()
    {
        Goo(new A.C());
    }

    static void Goo<T>(I<T> x) { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadAccess, Line = 15, Column = 9 });
        }

        [Fact]
        public void CS0122ERR_BadAccess02()
        {
            var text = @"

interface J<T> { }
interface I<T> : J<object> { }

class A
{
    private class B { }
    public class C : I<B> { }
}

class Program
{
    static void Main()
    {
        Goo(new A.C());
    }

    static void Goo<T>(I<T> x) { }
    static void Goo<T>(J<T> x) { }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text); // no errors
        }

        [Fact]
        public void CS0122ERR_BadAccess03()
        {
            var text = @"

interface J<T> { }
interface I<T> : J<object> { }

class A
{
    private class B { }
    public class C : I<B>
    {
    }
}

class Program
{
    delegate void D(A.C x);

    static void M<T>(I<T> c)
    {
        System.Console.WriteLine(""I"");
    }
//    static void M<T>(J<T> c)
//    {
//        System.Console.WriteLine(""J"");
//    }

    static void Main()
    {
        D d = M;
        d(null);
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadAccess, Line = 29, Column = 15 });
        }

        [Fact]
        public void CS0122ERR_BadAccess04()
        {
            var text = @"using System;

interface J<T> { }
interface I<T> : J<object> { }

class A
{
    private class B { }
    public class C : I<B>
    {
    }
}

class Program
{
    delegate void D(A.C x);

    static void M<T>(I<T> c)
    {
        Console.WriteLine(""I"");
    }
    static void M<T>(J<T> c)
    {
        Console.WriteLine(""J"");
    }

    static void Main()
    {
        D d = M;
        d(null);
    }
}
";
            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text);
        }

        [WorkItem(3202, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CS0246ERR_SingleTypeNameNotFound_InaccessibleImport()
        {
            var text =
@"using A = A;
namespace X
{
    using B;
}
static class A
{
    private static class B
    {
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,11): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                //     using B;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(4, 11),
                // (1,1): info CS8019: Unnecessary using directive.
                // using A = A;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = A;").WithLocation(1, 1),
                // (4,5): info CS8019: Unnecessary using directive.
                //     using B;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using B;").WithLocation(4, 5));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_1()
        {
            var text =
@"class ClassA
{
    object F = new { f1<int> = 1 };
    public static int f1 { get { return 1; } }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,22): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //     object F = new { f1<int> = 1 };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "f1<int> = 1").WithLocation(3, 22),
                // (3,22): error CS0307: The property 'ClassA.f1' cannot be used with type arguments
                //     object F = new { f1<int> = 1 };
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "f1<int>").WithArguments("ClassA.f1", "property").WithLocation(3, 22));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_2()
        {
            var text =
@"class ClassA
{
    object F = new { f1<T> };
    public static int f1 { get { return 1; } }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, Line = 3, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_SingleTypeNameNotFound, Line = 3, Column = 25 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_TypeArgsNotAllowed, Line = 3, Column = 22 }
            );
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_3()
        {
            var text =
@"class ClassA
{
    object F = new { f1+ };
    public static int f1 { get { return 1; } }
}
";
            DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, Line = 3, Column = 22 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_InvalidExprTerm, Line = 3, Column = 26 }
            );
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_4()
        {
            var text =
@"class ClassA
{
    object F = new { f1<int> };
    public static int f1<T>() { return 1; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,22): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //     object F = new { f1<int> };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "f1<int>").WithLocation(3, 22),
                // (3,22): error CS0828: Cannot assign method group to anonymous type property
                //     object F = new { f1<int> };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "f1<int>").WithArguments("method group").WithLocation(3, 22));
        }

        [Fact]
        public void CS7025ERR_BadVisEventType()
        {
            var text =
@"internal interface InternalInterface
{
}
internal delegate void InternalDelegate();
public class PublicClass
{
    protected class Protected { }

    public event System.Action<InternalInterface> A;
    public event System.Action<InternalClass> B;
    internal event System.Action<Protected> C;

    public event InternalDelegate D;
    public event InternalDelegate E { add { } remove { } }
}
internal class InternalClass : PublicClass
{
    public event System.Action<Protected> F;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,51): error CS7025: Inconsistent accessibility: event type 'System.Action<InternalInterface>' is less accessible than event 'PublicClass.A'
                //     public event System.Action<InternalInterface> A;
                Diagnostic(ErrorCode.ERR_BadVisEventType, "A").WithArguments("PublicClass.A", "System.Action<InternalInterface>"),
                // (10,47): error CS7025: Inconsistent accessibility: event type 'System.Action<InternalClass>' is less accessible than event 'PublicClass.B'
                //     public event System.Action<InternalClass> B;
                Diagnostic(ErrorCode.ERR_BadVisEventType, "B").WithArguments("PublicClass.B", "System.Action<InternalClass>"),
                // (11,45): error CS7025: Inconsistent accessibility: event type 'System.Action<PublicClass.Protected>' is less accessible than event 'PublicClass.C'
                //     internal event System.Action<Protected> C;
                Diagnostic(ErrorCode.ERR_BadVisEventType, "C").WithArguments("PublicClass.C", "System.Action<PublicClass.Protected>"),
                // (13,35): error CS7025: Inconsistent accessibility: event type 'InternalDelegate' is less accessible than event 'PublicClass.D'
                //     public event InternalDelegate D;
                Diagnostic(ErrorCode.ERR_BadVisEventType, "D").WithArguments("PublicClass.D", "InternalDelegate"),
                // (14,35): error CS7025: Inconsistent accessibility: event type 'InternalDelegate' is less accessible than event 'PublicClass.E'
                //     public event InternalDelegate E { add { } remove { } }
                Diagnostic(ErrorCode.ERR_BadVisEventType, "E").WithArguments("PublicClass.E", "InternalDelegate"),
                // (18,43): error CS7025: Inconsistent accessibility: event type 'System.Action<PublicClass.Protected>' is less accessible than event 'InternalClass.F'
                //     public event System.Action<Protected> F;
                Diagnostic(ErrorCode.ERR_BadVisEventType, "F").WithArguments("InternalClass.F", "System.Action<PublicClass.Protected>"),
                // (9,51): warning CS0067: The event 'PublicClass.A' is never used
                //     public event System.Action<InternalInterface> A;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "A").WithArguments("PublicClass.A"),
                // (10,47): warning CS0067: The event 'PublicClass.B' is never used
                //     public event System.Action<InternalClass> B;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "B").WithArguments("PublicClass.B"),
                // (11,45): warning CS0067: The event 'PublicClass.C' is never used
                //     internal event System.Action<Protected> C;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "C").WithArguments("PublicClass.C"),
                // (13,35): warning CS0067: The event 'PublicClass.D' is never used
                //     public event InternalDelegate D;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "D").WithArguments("PublicClass.D"),
                // (18,43): warning CS0067: The event 'InternalClass.F' is never used
                //     public event System.Action<Protected> F;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "F").WithArguments("InternalClass.F"));
        }

        [Fact, WorkItem(543386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543386")]
        public void VolatileFieldWithGenericTypeConstrainedToClass()
        {
            var text = @"
public class C {}

class G<T> where T : C
{
    public volatile T Fld = default(T);
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        #endregion

        [WorkItem(7920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/7920")]
        [Fact()]
        public void Bug7920()
        {
            var comp1 = CreateCompilation(@"
public class MyAttribute1 : System.Attribute
{}
", options: TestOptions.ReleaseDll, assemblyName: "Bug7920_CS");

            var comp2 = CreateCompilation(@"
public class MyAttribute2 : MyAttribute1
{}
", new[] { new CSharpCompilationReference(comp1) }, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,2): error CS0012: The type 'MyAttribute1' is defined in an assembly that is not referenced. You must add a reference to assembly 'Bug7920_CS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // [MyAttribute2]
                Diagnostic(ErrorCode.ERR_NoTypeDef, "MyAttribute2").WithArguments("MyAttribute1", "Bug7920_CS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                                 };

            var source3 = @"
[MyAttribute2]
public class Test
{}
";

            var comp3 = CreateCompilation(source3, new[] { new CSharpCompilationReference(comp2) }, options: TestOptions.ReleaseDll);

            comp3.GetDiagnostics().Verify(expected);

            var comp4 = CreateCompilation(source3, new[] { comp2.EmitToImageReference() }, options: TestOptions.ReleaseDll);

            comp4.GetDiagnostics().Verify(expected);
        }

        [Fact, WorkItem(345, "https://github.com/dotnet/roslyn")]
        public void InferredStaticTypeArgument()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        M(default(C));
    }
    public static void M<T>(T t)
    {
    }
}

static class C
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,9): error CS0718: 'C': static types cannot be used as type arguments
                //         M(default(C));
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "M").WithArguments("C").WithLocation(5, 9)
                );
        }

        [Fact, WorkItem(511, "https://github.com/dotnet/roslyn")]
        public void StaticTypeArgumentOfDynamicInvocation()
        {
            var source =
@"static class S {}
class C
{
    static void M()
    {
        dynamic d1 = 123;
        d1.N<S>(); // The dev11 compiler does not diagnose this
    }
    static void Main() {}
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void AbstractInScript()
        {
            var source =
@"internal abstract void M();
internal abstract object P { get; }
internal abstract event System.EventHandler E;";
            var compilation = CreateCompilationWithMscorlib461(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (1,24): error CS0513: 'M()' is abstract but it is contained in non-abstract type 'Script'
                // internal abstract void M();
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M").WithArguments("M()", "Script").WithLocation(1, 24),
                // (2,30): error CS0513: 'P.get' is abstract but it is contained in non-abstract type 'Script'
                // internal abstract object P { get; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "get").WithArguments("P.get", "Script").WithLocation(2, 30),
                // (3,45): error CS0513: 'E' is abstract but it is contained in non-abstract type 'Script'
                // internal abstract event System.EventHandler E;
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "E").WithArguments("E", "Script").WithLocation(3, 45));
        }

        [WorkItem(529225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529225")]
        [Fact]
        public void AbstractInSubmission()
        {
            var references = new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef };
            var source =
@"internal abstract void M();
internal abstract object P { get; }
internal abstract event System.EventHandler E;";
            var submission = CSharpCompilation.CreateScriptCompilation("s0.dll", SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Script), new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef });
            submission.VerifyDiagnostics(
                // (1,24): error CS0513: 'M()' is abstract but it is contained in non-abstract type 'Script'
                // internal abstract void M();
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M").WithArguments("M()", "Script").WithLocation(1, 24),
                // (2,30): error CS0513: 'P.get' is abstract but it is contained in non-abstract type 'Script'
                // internal abstract object P { get; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "get").WithArguments("P.get", "Script").WithLocation(2, 30),
                // (3,45): error CS0513: 'E' is abstract but it is contained in non-abstract type 'Script'
                // internal abstract event System.EventHandler E;
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "E").WithArguments("E", "Script").WithLocation(3, 45));
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void MultipleForwardsOfATypeToDifferentAssembliesWithoutUsingItShouldNotReportAnError()
        {
            var forwardingIL = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly Forwarding
{
}

.module Forwarding.dll

.assembly extern Destination1
{
}
.assembly extern Destination2
{
}

.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}

.class public auto ansi beforefieldinit TestSpace.ExistingReference
       extends [mscorlib]System.Object
{
  .field public static literal string Value = ""TEST VALUE""
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
        {
            // Code size       8 (0x8)
            .maxstack  8
            IL_0000:  ldarg.0
            IL_0001:  call instance void[mscorlib] System.Object::.ctor()
            IL_0006:  nop
            IL_0007:  ret
        }
}";
            var ilReference = CompileIL(forwardingIL, prependDefaultHeader: false);

            var code = @"
using TestSpace;
namespace UserSpace
{
    public class Program
    {
        public static void Main()
        {
            System.Console.WriteLine(ExistingReference.Value);
        }
    }
}";

            CompileAndVerify(
                source: code,
                references: new MetadataReference[] { ilReference },
                expectedOutput: "TEST VALUE");
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void MultipleForwardsOfFullyQualifiedTypeToDifferentAssembliesWhileReferencingItShouldErrorOut()
        {
            var userCode = @"
namespace ForwardingNamespace
{
    public class Program
    {
        public static void Main()
        {
            new Destination.TestClass();
        }
    }
}";
            var forwardingIL = @"
.assembly extern Destination1
{
    .ver 1:0:0:0
}
.assembly extern Destination2
{
    .ver 1:0:0:0
}
.assembly Forwarder
{
    .ver 1:0:0:0
}
.module ForwarderModule.dll
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}";
            var compilation = CreateCompilationWithILAndMscorlib40(userCode, forwardingIL, appendDefaultHeader: false);

            compilation.VerifyDiagnostics(
                // (8,29): error CS8329: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Destination.TestClass' to multiple assemblies: 'Destination1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' and 'Destination2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             new Destination.TestClass();
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, "TestClass").WithArguments("ForwarderModule.dll", "Forwarder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Destination.TestClass", "Destination1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Destination2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 29),
                // (8,29): error CS0234: The type or namespace name 'TestClass' does not exist in the namespace 'Destination' (are you missing an assembly reference?)
                //             new Destination.TestClass();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestClass").WithArguments("TestClass", "Destination").WithLocation(8, 29));
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void MultipleForwardsToManyAssembliesShouldJustReportTheFirstTwo()
        {
            var userCode = @"
namespace ForwardingNamespace
{
    public class Program
    {
        public static void Main()
        {
            new Destination.TestClass();
        }
    }
}";

            var forwardingIL = @"
.assembly Forwarder { }
.module ForwarderModule.dll
.assembly extern Destination1 { }
.assembly extern Destination2 { }
.assembly extern Destination3 { }
.assembly extern Destination4 { }
.assembly extern Destination5 { }
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination3
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination4
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination5
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination1
}
.class extern forwarder Destination.TestClass
{
	.assembly extern Destination2
}";

            var compilation = CreateCompilationWithILAndMscorlib40(userCode, forwardingIL, appendDefaultHeader: false);

            compilation.VerifyDiagnostics(
                // (8,29): error CS8329: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Destination.TestClass' to multiple assemblies: 'Destination1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'Destination2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             new Destination.TestClass();
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, "TestClass").WithArguments("ForwarderModule.dll", "Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Destination.TestClass", "Destination1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Destination2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 29),
                // (8,29): error CS0234: The type or namespace name 'TestClass' does not exist in the namespace 'Destination' (are you missing an assembly reference?)
                //             new Destination.TestClass();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestClass").WithArguments("TestClass", "Destination").WithLocation(8, 29));
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void RequiredExternalTypesForAMethodSignatureWillReportErrorsIfForwardedToMultipleAssemblies()
        {
            // The scenario is that assembly A is calling a method from assembly B. This method has a parameter of a type that lives
            // in assembly C. If A is compiled against B and C, it should compile successfully.
            // Now if assembly C is replaced with assembly C2, that forwards the type to both D1 and D2, it should fail with the appropriate error.

            var codeC = @"
namespace C
{
    public class ClassC {}
}";
            var referenceC = CreateCompilation(codeC, assemblyName: "C").EmitToImageReference();

            var codeB = @"
using C;

namespace B
{
    public static class ClassB
    {
        public static void MethodB(ClassC obj)
        {
            System.Console.WriteLine(obj.GetHashCode());
        }
    }
}";
            var referenceB = CreateCompilation(codeB, references: new MetadataReference[] { referenceC }, assemblyName: "B").EmitToImageReference();

            var codeA = @"
using B;

namespace A
{
    public class ClassA
    {
        public void MethodA()
        {
            ClassB.MethodB(null);
        }
    }
}";

            CreateCompilation(codeA, references: new MetadataReference[] { referenceB, referenceC }, assemblyName: "A").VerifyDiagnostics(); // No Errors

            var codeC2 = @"
.assembly C { }
.module CModule.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder C.ClassC
{
	.assembly extern D1
}
.class extern forwarder C.ClassC
{
	.assembly extern D2
}";

            var referenceC2 = CompileIL(codeC2, prependDefaultHeader: false);

            CreateCompilation(codeA, references: new MetadataReference[] { referenceB, referenceC2 }, assemblyName: "A").VerifyDiagnostics(
                // (10,13): error CS8329: Module 'CModule.dll' in assembly 'C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'C.ClassC' to multiple assemblies: 'D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             ClassB.MethodB(null);
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, "ClassB.MethodB").WithArguments("CModule.dll", "C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C.ClassC", "D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10332")]
        [WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void MultipleTypeForwardersToTheSameAssemblyShouldNotResultInMultipleForwardError()
        {
            var codeC = @"
namespace C
{
    public class ClassC {}
}";
            var referenceC = CreateCompilation(codeC, assemblyName: "C").EmitToImageReference();

            var codeB = @"
using C;

namespace B
{
    public static class ClassB
    {
        public static string MethodB(ClassC obj)
        {
            return ""obj is "" + (obj == null ? ""null"" : obj.ToString());
        }
    }
}";
            var referenceB = CreateCompilation(codeB, references: new MetadataReference[] { referenceC }, assemblyName: "B").EmitToImageReference();

            var codeA = @"
using B;

namespace A
{
    public class ClassA
    {
        public static void Main()
        {
            System.Console.WriteLine(ClassB.MethodB(null));
        }
    }
}";

            CompileAndVerify(
                source: codeA,
                references: new MetadataReference[] { referenceB, referenceC },
                expectedOutput: "obj is null");

            var codeC2 = @"
.assembly C
{
	.ver 0:0:0:0
}
.assembly extern D { }
.class extern forwarder C.ClassC
{
	.assembly extern D
}
.class extern forwarder C.ClassC
{
	.assembly extern D
}";

            var referenceC2 = CompileIL(codeC2, prependDefaultHeader: false);

            CreateCompilation(codeA, references: new MetadataReference[] { referenceB, referenceC2 }).VerifyDiagnostics(
                // (10,38): error CS0012: The type 'ClassC' is defined in an assembly that is not referenced. You must add a reference to assembly 'D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             System.Console.WriteLine(ClassB.MethodB(null));
                Diagnostic(ErrorCode.ERR_NoTypeDef, "ClassB.MethodB").WithArguments("C.ClassC", "D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(10, 38));

            var codeD = @"
namespace C
{
    public class ClassC { }
}";
            var referenceD = CreateCompilation(codeD, assemblyName: "D").EmitToImageReference();

            // ECMA-335 "II.22.14 ExportedType : 0x27" rule 14: "Ignoring nested Types, there shall be no duplicate rows, based upon FullName [ERROR]".
            var verifier = CompileAndVerify(
                source: codeA,
                references: new MetadataReference[] { referenceB, referenceC2, referenceD },
                expectedOutput: "obj is null",
                verify: Verification.FailsILVerify with { ILVerifyMessage = "[Main]: Unable to resolve token. { Offset = 0x1, Token = 167772167 }" });

            verifier.VerifyIL("A.ClassA.Main", """
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "string B.ClassB.MethodB(C.ClassC)"
  IL_0006:  call       "void System.Console.WriteLine(string)"
  IL_000b:  ret
}
""");
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void CompilingModuleWithMultipleForwardersToDifferentAssembliesShouldErrorOut()
        {
            var ilSource = @"
.module ForwarderModule.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D1
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D2
}";

            var ilModule = GetILModuleReference(ilSource, prependDefaultHeader: false);
            CreateCompilation(string.Empty, references: new MetadataReference[] { ilModule }, assemblyName: "Forwarder").VerifyDiagnostics(
                // error CS8329: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Testspace.TestType' to multiple assemblies: 'D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies).WithArguments("ForwarderModule.dll", "Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Testspace.TestType", "D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1));
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void CompilingModuleWithMultipleForwardersToTheSameAssemblyShouldNotProduceMultipleForwardingErrors()
        {
            var ilSource = @"
.assembly extern D { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D
}";

            var ilModule = GetILModuleReference(ilSource, prependDefaultHeader: false);
            CreateCompilation(string.Empty, references: new MetadataReference[] { ilModule }).VerifyDiagnostics(
                // error CS0012: The type 'TestType' is defined in an assembly that is not referenced. You must add a reference to assembly 'D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("Testspace.TestType", "D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1));

            var dCode = @"
namespace Testspace
{
    public class TestType { }
}";
            var dReference = CreateCompilation(dCode, assemblyName: "D").EmitToImageReference();

            // Now compilation succeeds
            CreateCompilation(string.Empty, references: new MetadataReference[] { ilModule, dReference }).VerifyDiagnostics();
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void LookingUpATypeForwardedTwiceInASourceCompilationReferenceShouldFail()
        {
            // This test specifically tests that SourceAssembly symbols also produce this error (by using a CompilationReference instead of the usual PEAssembly symbol)

            var ilSource = @"
.module ForwarderModule.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D1
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D2
}";

            var ilModuleReference = GetILModuleReference(ilSource, prependDefaultHeader: false);
            var forwarderCompilation = CreateEmptyCompilation(
                source: string.Empty,
                references: new MetadataReference[] { ilModuleReference },
                options: TestOptions.DebugDll,
                assemblyName: "Forwarder");

            var csSource = @"
namespace UserSpace
{
    public class UserClass
    {
        public static void Main()
        {
            var obj = new Testspace.TestType();
        }
    }
}";

            var userCompilation = CreateCompilation(
                source: csSource,
                references: new MetadataReference[] { forwarderCompilation.ToMetadataReference() },
                assemblyName: "UserAssembly");

            userCompilation.VerifyDiagnostics(
                // (8,37): error CS8329: Module 'ForwarderModule.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Testspace.TestType' to multiple assemblies: 'D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             var obj = new Testspace.TestType();
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, "TestType").WithArguments("ForwarderModule.dll", "Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Testspace.TestType", "D1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "D2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 37),
                // (8,37): error CS0234: The type or namespace name 'TestType' does not exist in the namespace 'Testspace' (are you missing an assembly reference?)
                //             var obj = new Testspace.TestType();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType").WithArguments("TestType", "Testspace").WithLocation(8, 37));
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void ForwardingErrorsInLaterModulesAlwaysOverwriteOnesInEarlierModules()
        {
            var module1IL = @"
.module module1IL.dll
.assembly extern D1 { }
.assembly extern D2 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D1
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D2
}";

            var module1Reference = GetILModuleReference(module1IL, prependDefaultHeader: false);

            var module2IL = @"
.module module12L.dll
.assembly extern D3 { }
.assembly extern D4 { }
.class extern forwarder Testspace.TestType
{
	.assembly extern D3
}
.class extern forwarder Testspace.TestType
{
	.assembly extern D4
}";

            var module2Reference = GetILModuleReference(module2IL, prependDefaultHeader: false);

            var forwarderCompilation = CreateEmptyCompilation(
                source: string.Empty,
                references: new MetadataReference[] { module1Reference, module2Reference },
                options: TestOptions.DebugDll,
                assemblyName: "Forwarder");

            var csSource = @"
namespace UserSpace
{
    public class UserClass
    {
        public static void Main()
        {
            var obj = new Testspace.TestType();
        }
    }
}";

            var userCompilation = CreateCompilation(
                source: csSource,
                references: new MetadataReference[] { forwarderCompilation.ToMetadataReference() },
                assemblyName: "UserAssembly");

            userCompilation.VerifyDiagnostics(
                // (8,37): error CS8329: Module 'module12L.dll' in assembly 'Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'Testspace.TestType' to multiple assemblies: 'D3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'D4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             var obj = new Testspace.TestType();
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, "TestType").WithArguments("module12L.dll", "Forwarder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Testspace.TestType", "D3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "D4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (8,37): error CS0234: The type or namespace name 'TestType' does not exist in the namespace 'Testspace' (are you missing an assembly reference?)
                //             var obj = new Testspace.TestType();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "TestType").WithArguments("TestType", "Testspace"));
        }

        [Fact, WorkItem(16484, "https://github.com/dotnet/roslyn/issues/16484")]
        public void MultipleForwardsThatChainResultInTheSameAssemblyShouldStillProduceAnError()
        {
            // The scenario is that assembly A is calling a method from assembly B. This method has a parameter of a type that lives
            // in assembly C. Now if assembly C is replaced with assembly C2, that forwards the type to both D and E, and D forwards it to E,
            // it should fail with the appropriate error.

            var codeC = @"
namespace C
{
    public class ClassC {}
}";
            var referenceC = CreateCompilation(codeC, assemblyName: "C").EmitToImageReference();

            var codeB = @"
using C;

namespace B
{
    public static class ClassB
    {
        public static void MethodB(ClassC obj)
        {
            System.Console.WriteLine(obj.GetHashCode());
        }
    }
}";
            var referenceB = CreateCompilation(codeB, references: new MetadataReference[] { referenceC }, assemblyName: "B").EmitToImageReference();

            var codeC2 = @"
.assembly C { }
.module C.dll
.assembly extern D { }
.assembly extern E { }
.class extern forwarder C.ClassC
{
	.assembly extern D
}
.class extern forwarder C.ClassC
{
	.assembly extern E
}";

            var referenceC2 = CompileIL(codeC2, prependDefaultHeader: false);

            var codeD = @"
.assembly D { }
.assembly extern E { }
.class extern forwarder C.ClassC
{
	.assembly extern E
}";

            var referenceD = CompileIL(codeD, prependDefaultHeader: false);
            var referenceE = CreateCompilation(codeC, assemblyName: "E").EmitToImageReference();

            var codeA = @"
using B;
using C;

namespace A
{
    public class ClassA
    {
        public void MethodA(ClassC obj)
        {
            ClassB.MethodB(obj);
        }
    }
}";

            CreateCompilation(codeA, references: new MetadataReference[] { referenceB, referenceC2, referenceD, referenceE }, assemblyName: "A").VerifyDiagnostics(
                // (11,13): error CS8329: Module 'C.dll' in assembly 'C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is forwarding the type 'C.ClassC' to multiple assemblies: 'D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'E, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //             ClassB.MethodB(obj);
                Diagnostic(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, "ClassB.MethodB").WithArguments("C.dll", "C, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C.ClassC", "D, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "E, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 13));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_NoneWithRef()
        {
            CreateCompilation(@"
partial class C {
    partial void M(int i);
    partial void M(ref int i) {}  
}").VerifyDiagnostics(
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(ref int)'
                //     partial void M(ref int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(ref int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_NoneWithIn()
        {
            CreateCompilation(@"
partial class C {
    partial void M(int i);
    partial void M(in int i) {}  
}").VerifyDiagnostics(
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(in int)'
                //     partial void M(in int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(in int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_NoneWithOut()
        {
            CreateCompilation(@"
partial class C {
    partial void M(int i);
    partial void M(out int i) { i = 0; }  
}").VerifyDiagnostics(
                // (4,18): error CS8795: Partial method 'C.M(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M").WithArguments("C.M(out int)").WithLocation(4, 18),
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(out int)'
                //     partial void M(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(out int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_RefWithNone()
        {
            CreateCompilation(@"
partial class C {
    partial void M(ref int i);
    partial void M(int i) {}  
}").VerifyDiagnostics(
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(int)'
                //     partial void M(int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_RefWithIn()
        {
            CreateCompilation(@"
partial class C {
    partial void M(ref int i);
    partial void M(in int i) {}
}").VerifyDiagnostics(
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(in int)'
                //     partial void M(in int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(in int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_RefWithOut()
        {
            CreateCompilation(@"
partial class C {
    partial void M(ref int i);
    partial void M(out int i) { i = 0; }
}").VerifyDiagnostics(
                // (4,18): error CS8795: Partial method 'C.M(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M").WithArguments("C.M(out int)").WithLocation(4, 18),
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(out int)'
                //     partial void M(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(out int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_InWithNone()
        {
            CreateCompilation(@"
partial class C {
    partial void M(in int i);
    partial void M(int i) {}  
}").VerifyDiagnostics(
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(int)'
                //     partial void M(int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_InWithRef()
        {
            CreateCompilation(@"
partial class C {
    partial void M(in int i);
    partial void M(ref int i) {}  
}").VerifyDiagnostics(
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(ref int)'
                //     partial void M(ref int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(ref int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_InWithOut()
        {
            CreateCompilation(@"
partial class C {
    partial void M(in int i);
    partial void M(out int i) { i = 0; }  
}").VerifyDiagnostics(
                // (4,18): error CS8795: Partial method 'C.M(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M").WithArguments("C.M(out int)").WithLocation(4, 18),
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(out int)'
                //     partial void M(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(out int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_OutWithNone()
        {
            CreateCompilation(@"
partial class C {
    partial void M(out int i);
    partial void M(int i) {}  
}").VerifyDiagnostics(
                // (3,18): error CS8794: Partial method C.M(out int) must have an implementation part because it has 'out' parameters.
                //     partial void M(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M").WithArguments("C.M(out int)").WithLocation(3, 18),
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(int)'
                //     partial void M(int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_OutWithRef()
        {
            CreateCompilation(@"
partial class C {
    partial void M(out int i);
    partial void M(ref int i) {}
}").VerifyDiagnostics(
                // (3,18): error CS8794: Partial method C.M(out int) must have an implementation part because it has 'out' parameters.
                //     partial void M(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M").WithArguments("C.M(out int)").WithLocation(3, 18),
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(ref int)'
                //     partial void M(ref int i) {}
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(ref int)").WithLocation(4, 18));
        }

        [Fact]
        public void PartialMethodsConsiderRefKindDifferences_OutWithIn()
        {
            CreateCompilation(@"
partial class C {
    partial void M(out int i);
    partial void M(in int i) {}  
}").VerifyDiagnostics(
                // (3,18): error CS8794: Partial method C.M(out int) must have an implementation part because it has 'out' parameters.
                //     partial void M(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M").WithArguments("C.M(out int)").WithLocation(3, 18),
                // (4,18): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M(in int)'
                //     partial void M(in int i) {}  
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M").WithArguments("C.M(in int)").WithLocation(4, 18));
        }

        [Fact]
        public void MethodWithNoReturnTypeShouldNotComplainAboutStaticCtor()
        {
            CreateCompilation(@"
class X
{
    private static Y(int i) {}
}").VerifyDiagnostics(
                // (4,20): error CS1520: Method must have a return type
                //     private static Y(int i) {}
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Y").WithLocation(4, 20));
        }

        [Fact, WorkItem(56653, "https://github.com/dotnet/roslyn/issues/56653")]
        public void DisallowLowerCaseTypeName_InTypeDeclaration()
        {
            var text = @"
class one { }

class @two { }

namespace ns
{
    class nint { }
}

class @nint { }

partial struct three { }
partial struct @three { }

partial interface four { }
partial interface four { }

partial class @five { }
partial class five { }

partial record @six { }
partial record @six { }

delegate void seven();

enum eight { first, second }

class C
{
    void nine() { }
}

class Ten { }
class eleveN { }
class twel_ve { }

class cédille { }
";
            var expected = new[]
            {
                // (2,7): warning CS8981: The type name 'one' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class one { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "one").WithArguments("one").WithLocation(2, 7),
                // (8,11): warning CS8981: The type name 'nint' only contains lower-cased ascii characters. Such names may become reserved for the language.
                //     class nint { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "nint").WithArguments("nint").WithLocation(8, 11),
                // (13,16): warning CS8981: The type name 'three' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // partial struct three { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "three").WithArguments("three").WithLocation(13, 16),
                // (16,19): warning CS8981: The type name 'four' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // partial interface four { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "four").WithArguments("four").WithLocation(16, 19),
                // (17,19): warning CS8981: The type name 'four' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // partial interface four { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "four").WithArguments("four").WithLocation(17, 19),
                // (20,15): warning CS8981: The type name 'five' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // partial class five { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "five").WithArguments("five").WithLocation(20, 15),
                // (25,15): warning CS8981: The type name 'seven' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // delegate void seven();
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "seven").WithArguments("seven").WithLocation(25, 15),
                // (27,6): warning CS8981: The type name 'eight' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // enum eight { first, second }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "eight").WithArguments("eight").WithLocation(27, 6)
            };

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(6));
            comp.VerifyDiagnostics();

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(7));
            comp.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(56653, "https://github.com/dotnet/roslyn/issues/56653")]
        public void DisallowLowerCaseTypeName_AsTypeParameter()
        {
            var text = @"
class C1<one> { }
class C2<@two> { }
class C3<Ten> { }
class C4<cédille> { }

delegate void D1<one>();
delegate void D2<@two>();
delegate void D3<Ten>();
delegate void D4<cédille>();

class CM
{
    void M1<one>() { }
    void M2<@two>() { }
    void M3<Ten>() { }
    void M4<cédille>() { }

    void MLocal()
    {
        local1<object>();
        local2<object>();
        local3<object>();
        local4<object>();

        void local1<one>() { }
        void local2<@two>() { }
        void local3<Ten>() { }
        void local4<cédille>() { }
    }
}
";
            var expected = new[]
            {
                // (2,10): warning CS8981: The type name 'one' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class C1<one> { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "one").WithArguments("one").WithLocation(2, 10),
                // (7,18): warning CS8981: The type name 'one' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // delegate void D1<one>();
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "one").WithArguments("one").WithLocation(7, 18),
                // (14,13): warning CS8981: The type name 'one' only contains lower-cased ascii characters. Such names may become reserved for the language.
                //     void M1<one>() { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "one").WithArguments("one").WithLocation(14, 13),
                // (26,21): warning CS8981: The type name 'one' only contains lower-cased ascii characters. Such names may become reserved for the language.
                //         void local1<one>() { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "one").WithArguments("one").WithLocation(26, 21)
            };

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(6));
            comp.VerifyDiagnostics();

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(7));
            comp.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(56653, "https://github.com/dotnet/roslyn/issues/56653")]
        public void DisallowLowerCaseTypeName_AsAlias()
        {
            var text = @"
using one = System.Console;
using @two = System.Console;
using three = System;
using Ten = System.Console;
using cédille = System.Console;
";
            var expected = new[]
            {
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using one = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using one = System.Console;").WithLocation(2, 1),
                // (2,7): warning CS8981: The type name 'one' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using one = System.Console;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "one").WithArguments("one").WithLocation(2, 7),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using @two = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using @two = System.Console;").WithLocation(3, 1),
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using three = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using three = System;").WithLocation(4, 1),
                // (4,7): warning CS8981: The type name 'three' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using three = System;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "three").WithArguments("three").WithLocation(4, 7),
                // (5,1): hidden CS8019: Unnecessary using directive.
                // using Ten = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Ten = System.Console;").WithLocation(5, 1),
                // (6,1): hidden CS8019: Unnecessary using directive.
                // using cédille = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using cédille = System.Console;").WithLocation(6, 1)
            };

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(6));
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using one = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using one = System.Console;").WithLocation(2, 1),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using @two = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using @two = System.Console;").WithLocation(3, 1),
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using three = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using three = System;").WithLocation(4, 1),
                // (5,1): hidden CS8019: Unnecessary using directive.
                // using Ten = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Ten = System.Console;").WithLocation(5, 1),
                // (6,1): hidden CS8019: Unnecessary using directive.
                // using cédille = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using cédille = System.Console;").WithLocation(6, 1)
                );

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(7));
            comp.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(56653, "https://github.com/dotnet/roslyn/issues/56653")]
        public void DisallowLowerCaseTypeName_LowerCaseAscii()
        {
            var text = @"
class a { }
class z { }

class abcdefghijklmnopqrstuvwxyz { }

class A { }
class Z { }

// first lower-case letter outside ascii range
class \u00B5 { }

// first upper-case letter outside ascii range
class \u00c0 { }
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(7));
            comp.VerifyDiagnostics(
                // (2,7): warning CS8981: The type name 'a' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class a { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "a").WithArguments("a").WithLocation(2, 7),
                // (3,7): warning CS8981: The type name 'z' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class z { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "z").WithArguments("z").WithLocation(3, 7),
                // (5,7): warning CS8981: The type name 'abcdefghijklmnopqrstuvwxyz' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class abcdefghijklmnopqrstuvwxyz { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "abcdefghijklmnopqrstuvwxyz").WithArguments("abcdefghijklmnopqrstuvwxyz").WithLocation(5, 7)
                );

            text = @"
// backtick, before 'a'
class \u0060 { }
";

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(7));
            comp.VerifyDiagnostics(
                // (3,7): error CS1001: Identifier expected
                // class \u0060 { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\u0060").WithLocation(3, 7),
                // (3,7): error CS1514: { expected
                // class \u0060 { }
                Diagnostic(ErrorCode.ERR_LbraceExpected, @"\u0060").WithLocation(3, 7),
                // (3,7): error CS1513: } expected
                // class \u0060 { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, @"\u0060").WithLocation(3, 7),
                // (3,7): error CS1056: Unexpected character '\u0060'
                // class \u0060 { }
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\u0060").WithLocation(3, 7),
                // (3,14): error CS8803: Top-level statements must precede namespace and type declarations.
                // class \u0060 { }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "{ }").WithLocation(3, 14),
                // (3,14): error CS8805: Program using top-level statements must be an executable.
                // class \u0060 { }
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "{ }").WithLocation(3, 14)
                );

            text = @"
// pipe, after 'z' and '{'
class \u007c { }
";

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11, options: TestOptions.DebugDll.WithWarningLevel(7));
            comp.VerifyDiagnostics(
                // (3,7): error CS1001: Identifier expected
                // class \u007c { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\u007c").WithLocation(3, 7),
                // (3,7): error CS1514: { expected
                // class \u007c { }
                Diagnostic(ErrorCode.ERR_LbraceExpected, @"\u007c").WithLocation(3, 7),
                // (3,7): error CS1513: } expected
                // class \u007c { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, @"\u007c").WithLocation(3, 7),
                // (3,7): error CS1056: Unexpected character '\u007c'
                // class \u007c { }
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("\\u007c").WithLocation(3, 7),
                // (3,14): error CS8803: Top-level statements must precede namespace and type declarations.
                // class \u007c { }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "{ }").WithLocation(3, 14),
                // (3,14): error CS8805: Program using top-level statements must be an executable.
                // class \u007c { }
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "{ }").WithLocation(3, 14)
                );
        }

        [Fact]
        [WorkItem(58517, "https://github.com/dotnet/roslyn/issues/58517")]
        public void CycleThroughForwardedType_01()
        {
            var source1_1 = @"
public class Base
{
}
";

            var comp1_v1 = CreateCompilation(source1_1, assemblyName: "Lib1");
            comp1_v1.VerifyDiagnostics();

            var source2 = @"
[MyAttribute]
public class Derived : Base
{
}

class MyAttribute : System.Attribute {}
";

            var comp2 = CreateCompilation(source2, references: new[] { comp1_v1.ToMetadataReference() });

            var comp2Ref = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source1_1);

            var source1_2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Base))]
[assembly: TypeForwardedTo(typeof(Derived))]

class Test
{
    void M()
    {
        Base x = new Derived();
    }
}
";

            var comp1_v2 = CreateCompilation(source1_2, assemblyName: "Lib1", references: new[] { comp2Ref, comp3.ToMetadataReference() });
            comp1_v2.VerifyEmitDiagnostics();

            var source1_3 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Derived))]
";

            var comp1_v3 = CreateCompilation(source1_3, assemblyName: "Lib1", references: new[] { comp2Ref });
            comp1_v3.VerifyEmitDiagnostics();

            var source1_4 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Derived))]
[assembly: TypeForwardedTo(typeof(Base))]

class Test
{
    void M()
    {
        Base x = new Derived();
    }
}
";

            var comp1_v4 = CreateCompilation(source1_4, assemblyName: "Lib1", references: new[] { comp2Ref, comp3.ToMetadataReference() });
            comp1_v4.VerifyEmitDiagnostics();

            var source1_5 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Derived))]

class Test
{
    void M()
    {
        Test x = new Derived();
    }
}
";

            var comp1_v5 = CreateCompilation(source1_5, assemblyName: "Lib1", references: new[] { comp2Ref });
            comp1_v5.VerifyEmitDiagnostics(
                // (10,18): error CS7068: Reference to type 'Base' claims it is defined in this assembly, but it is not defined in source or any added modules
                //         Test x = new Derived();
                Diagnostic(ErrorCode.ERR_MissingTypeInSource, "new Derived()").WithArguments("Base").WithLocation(10, 18),
                // (10,18): error CS0029: Cannot implicitly convert type 'Derived' to 'Test'
                //         Test x = new Derived();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Derived()").WithArguments("Derived", "Test").WithLocation(10, 18)
                );
        }

        [Fact]
        [WorkItem(58517, "https://github.com/dotnet/roslyn/issues/58517")]
        public void CycleThroughForwardedType_02()
        {
            var source1_1 = @"
public class Base
{
}
";

            var comp1_v1 = CreateCompilation(source1_1, assemblyName: "Lib1");
            comp1_v1.VerifyDiagnostics();

            var source2 = @"
public class Derived : Base
{
}
";

            var comp2 = CreateCompilation(source2, references: new[] { comp1_v1.ToMetadataReference() });

            var comp2Ref = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source1_1);

            var source1_2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Base))]
[assembly: TypeForwardedTo(typeof(Derived))]

class Test
{
    void M()
    {
        Base x = new Derived();
    }
}
";

            var comp1_v2 = CreateCompilation(source1_2, assemblyName: "Lib1", references: new[] { comp2Ref, comp3.ToMetadataReference() });
            comp1_v2.VerifyEmitDiagnostics();

            var source1_3 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Derived))]
";

            var comp1_v3 = CreateCompilation(source1_3, assemblyName: "Lib1", references: new[] { comp2Ref });
            comp1_v3.VerifyEmitDiagnostics();

            var source1_4 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Derived))]
[assembly: TypeForwardedTo(typeof(Base))]

class Test
{
    void M()
    {
        Base x = new Derived();
    }
}
";

            var comp1_v4 = CreateCompilation(source1_4, assemblyName: "Lib1", references: new[] { comp2Ref, comp3.ToMetadataReference() });
            comp1_v4.VerifyEmitDiagnostics();

            var source1_5 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Derived))]

class Test
{
    void M()
    {
        Test x = new Derived();
    }
}
";

            var comp1_v5 = CreateCompilation(source1_5, assemblyName: "Lib1", references: new[] { comp2Ref });
            comp1_v5.VerifyEmitDiagnostics(
                // (10,18): error CS7068: Reference to type 'Base' claims it is defined in this assembly, but it is not defined in source or any added modules
                //         Test x = new Derived();
                Diagnostic(ErrorCode.ERR_MissingTypeInSource, "new Derived()").WithArguments("Base").WithLocation(10, 18),
                // (10,18): error CS0029: Cannot implicitly convert type 'Derived' to 'Test'
                //         Test x = new Derived();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Derived()").WithArguments("Derived", "Test").WithLocation(10, 18)
                );
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_01()
        {
            var source1 = @"
using System;

[Obsolete(""Error"", error: true)]
public class C1
{
}

[Obsolete(""Warning"", error: false)]
public class C2
{
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "Lib1");
            comp1.VerifyDiagnostics();

            var source2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(C1))]
[assembly: TypeForwardedTo(typeof(C2))]
";

            var comp2 = CreateCompilation(source2, references: new[] { comp1.ToMetadataReference() });
            comp2.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_02()
        {
            var source1 = @"
using System;

[Obsolete(""Error"", error: true)]
public class C1
{
}

[Obsolete(""Warning"", error: false)]
public class C2
{
}
";

            var source2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(C1))]
[assembly: TypeForwardedTo(typeof(C2))]
";

            var comp2 = CreateCompilation(new[] { source1, source2 });
            comp2.VerifyEmitDiagnostics(
                // (4,12): error CS0729: Type 'C1' is defined in this assembly, but a type forwarder is specified for it
                // [assembly: TypeForwardedTo(typeof(C1))]
                Diagnostic(ErrorCode.ERR_ForwardedTypeInThisAssembly, "TypeForwardedTo(typeof(C1))").WithArguments("C1").WithLocation(4, 12),
                // (5,12): error CS0729: Type 'C2' is defined in this assembly, but a type forwarder is specified for it
                // [assembly: TypeForwardedTo(typeof(C2))]
                Diagnostic(ErrorCode.ERR_ForwardedTypeInThisAssembly, "TypeForwardedTo(typeof(C2))").WithArguments("C2").WithLocation(5, 12)
                );
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_03()
        {
            // Other attributes are not affected
            var source1 = @"
using System;

[Obsolete(""Error"", error: true)]
public class C1
{
}

[Obsolete(""Warning"", error: false)]
public class C2
{
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "Lib1");
            comp1.VerifyDiagnostics();

            var source2 = @"
[assembly: TypeForwarded_2(typeof(C1))]
[assembly: TypeForwarded_2(typeof(C2))]

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
class TypeForwarded_2Attribute : System.Attribute
{
    public TypeForwarded_2Attribute(System.Type type) { }
}
";

            var comp2 = CreateCompilation(source2, references: new[] { comp1.ToMetadataReference() });
            comp2.VerifyEmitDiagnostics(
                // (2,35): error CS0619: 'C1' is obsolete: 'Error'
                // [assembly: TypeForwarded_2(typeof(C1))]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(2, 35),
                // (3,35): warning CS0618: 'C2' is obsolete: 'Warning'
                // [assembly: TypeForwarded_2(typeof(C2))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(3, 35)
                );
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_04()
        {
            // Other attributes in the same file are not affected
            var source1 = @"
using System;

[Obsolete(""Error"", error: true)]
public class C1
{
}

[Obsolete(""Warning"", error: false)]
public class C2
{
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "Lib1");
            comp1.VerifyDiagnostics();

            var source2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(C1))]

[assembly: TypeForwarded_2(typeof(C1))]
[assembly: TypeForwarded_2(typeof(C2))]

[assembly: TypeForwardedTo(typeof(C2))]

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
class TypeForwarded_2Attribute : System.Attribute
{
    public TypeForwarded_2Attribute(System.Type type) { }
}
";

            var comp2 = CreateCompilation(source2, references: new[] { comp1.ToMetadataReference() });
            comp2.VerifyEmitDiagnostics(
                // (6,35): error CS0619: 'C1' is obsolete: 'Error'
                // [assembly: TypeForwarded_2(typeof(C1))]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(6, 35),
                // (7,35): warning CS0618: 'C2' is obsolete: 'Warning'
                // [assembly: TypeForwarded_2(typeof(C2))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(7, 35)
                );
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_05()
        {
            // Other attributes in a different file at the same position are not affected
            var source1 = @"
using System;

[Obsolete(""Error"", error: true)]
public class C1
{
}

[Obsolete(""Warning"", error: false)]
public class C2
{
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "Lib1");
            comp1.VerifyDiagnostics();

            var source2 = @"
[assembly: {0}(typeof(C1))]
[assembly: {0}(typeof(C2))]
";
            var source3 = @"
global using System.Runtime.CompilerServices;

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
class TypeForwarded_2Attribute : System.Attribute
{
    public TypeForwarded_2Attribute(System.Type type) { }
}
";

            var comp2 = CreateCompilation(new[] { string.Format(source2, "TypeForwarded_2"), string.Format(source2, "TypeForwardedTo"), source3 }, references: new[] { comp1.ToMetadataReference() });
            comp2.VerifyEmitDiagnostics(
                // (2,35): error CS0619: 'C1' is obsolete: 'Error'
                // [assembly: TypeForwarded_2(typeof(C1))]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(2, 35),
                // (3,35): warning CS0618: 'C2' is obsolete: 'Warning'
                // [assembly: TypeForwarded_2(typeof(C2))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(3, 35)
                );

            comp2.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, comp2.SyntaxTrees[0], filterSpanWithinTree: null, includeEarlierStages: true).Verify(
                // (2,35): error CS0619: 'C1' is obsolete: 'Error'
                // [assembly: TypeForwarded_2(typeof(C1))]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(2, 35),
                // (3,35): warning CS0618: 'C2' is obsolete: 'Warning'
                // [assembly: TypeForwarded_2(typeof(C2))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(3, 35)
                );
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_06()
        {
            // Attributes on other targets are not affected, including mistaken application of TypeForwardedTo
            var source1 = @"
using System;

[Obsolete(""Error"", error: true)]
public class C1
{
}

[Obsolete(""Warning"", error: false)]
public class C2
{
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "Lib1");
            comp1.VerifyDiagnostics();

            var source2 = @"
using System.Runtime.CompilerServices;

[module: TypeForwardedTo(typeof(C1))]
[module: TypeForwarded_2(typeof(C2))]

[TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))] class C3
{
    [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))]void M1()
    {}
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
class TypeForwarded_2Attribute : System.Attribute
{
    public TypeForwarded_2Attribute(System.Type type) { }
}
";

            var comp2 = CreateCompilation(source2, references: new[] { comp1.ToMetadataReference() });
            comp2.VerifyEmitDiagnostics(
                // (4,10): error CS0592: Attribute 'TypeForwardedTo' is not valid on this declaration type. It is only valid on 'assembly' declarations.
                // [module: TypeForwardedTo(typeof(C1))]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TypeForwardedTo").WithArguments("TypeForwardedTo", "assembly").WithLocation(4, 10),
                // (4,33): error CS0619: 'C1' is obsolete: 'Error'
                // [module: TypeForwardedTo(typeof(C1))]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(4, 33),
                // (5,33): warning CS0618: 'C2' is obsolete: 'Warning'
                // [module: TypeForwarded_2(typeof(C2))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(5, 33),
                // (7,2): error CS0592: Attribute 'TypeForwardedTo' is not valid on this declaration type. It is only valid on 'assembly' declarations.
                // [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))] class C3
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TypeForwardedTo").WithArguments("TypeForwardedTo", "assembly").WithLocation(7, 2),
                // (7,25): error CS0619: 'C1' is obsolete: 'Error'
                // [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))] class C3
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(7, 25),
                // (7,54): warning CS0618: 'C2' is obsolete: 'Warning'
                // [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))] class C3
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(7, 54),
                // (9,6): error CS0592: Attribute 'TypeForwardedTo' is not valid on this declaration type. It is only valid on 'assembly' declarations.
                //     [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))]void M1()
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TypeForwardedTo").WithArguments("TypeForwardedTo", "assembly").WithLocation(9, 6),
                // (9,29): error CS0619: 'C1' is obsolete: 'Error'
                //     [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))]void M1()
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C1").WithArguments("C1", "Error").WithLocation(9, 29),
                // (9,58): warning CS0618: 'C2' is obsolete: 'Warning'
                //     [TypeForwardedTo(typeof(C1))][TypeForwarded_2(typeof(C2))]void M1()
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "C2").WithArguments("C2", "Warning").WithLocation(9, 58)
                );
        }

        [Fact]
        [WorkItem(61264, "https://github.com/dotnet/roslyn/issues/61264")]
        public void ForwardObsoleteType_07()
        {
            // Other errors within the same span are not affected

            // IL is equivalent to:
            // 
            // using System;
            // using System.Runtime.CompilerServices;
            // 
            // [Obsolete("Error", error: true)]
            // [CompilerFeatureRequired("SomeFeatureIsRequired")]
            // public class C1
            // {
            // }

            var ilSsource1 = @"
.class public auto ansi beforefieldinit C1
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 05 45 72 72 6f 72 01 00 00
    )
    .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
        01 00 15 53 6f 6d 65 46 65 61 74 75 72 65 49 73
        52 65 71 75 69 72 65 64 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    } // end of method C1::.ctor

} // end of class C1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 01 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            string featureName
        ) cil managed 
    {
        IL_000f: ret
    } // end of method CompilerFeatureRequiredAttribute::.ctor
} // end of class System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
";

            var source2 = @"
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(C1))]
";

            var comp2 = CreateCompilationWithIL(source2, ilSsource1);
            comp2.VerifyEmitDiagnostics(
                // (4,12): error CS9041: 'C1' requires compiler feature 'SomeFeatureIsRequired', which is not supported by this version of the C# compiler.
                // [assembly: TypeForwardedTo(typeof(C1))]
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "TypeForwardedTo(typeof(C1))").WithArguments("C1", "SomeFeatureIsRequired").WithLocation(4, 12),
                // (4,35): error CS9041: 'C1' requires compiler feature 'SomeFeatureIsRequired', which is not supported by this version of the C# compiler.
                // [assembly: TypeForwardedTo(typeof(C1))]
                Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "C1").WithArguments("C1", "SomeFeatureIsRequired").WithLocation(4, 35)
                );
        }

        [Fact]
        public void PartialConstructor()
        {
            CreateCompilation(new[]
            {
                """
                public class PartialCtor
                {
                    partial PartialCtor() { }
                }
                """,
                """
                public class PublicPartialCtor
                {
                    public partial PublicPartialCtor() { }
                }
                """,
                """
                public class PartialPublicCtor
                {
                    partial public PartialPublicCtor() { }
                }
                """
            }).VerifyDiagnostics(
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial public PartialPublicCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial public PartialPublicCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     partial PartialCtor() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(3, 5),
                // (3,12): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial PublicPartialCtor() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(3, 12),
                // (3,13): error CS0542: 'PartialCtor': member names cannot be the same as their enclosing type
                //     partial PartialCtor() { }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "PartialCtor").WithArguments("PartialCtor").WithLocation(3, 13),
                // (3,13): error CS0161: 'PartialCtor.PartialCtor()': not all code paths return a value
                //     partial PartialCtor() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "PartialCtor").WithArguments("PartialCtor.PartialCtor()").WithLocation(3, 13),
                // (3,20): error CS0542: 'PublicPartialCtor': member names cannot be the same as their enclosing type
                //     public partial PublicPartialCtor() { }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "PublicPartialCtor").WithArguments("PublicPartialCtor").WithLocation(3, 20),
                // (3,20): error CS0161: 'PublicPartialCtor.PublicPartialCtor()': not all code paths return a value
                //     public partial PublicPartialCtor() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "PublicPartialCtor").WithArguments("PublicPartialCtor.PublicPartialCtor()").WithLocation(3, 20));
        }

        [Fact]
        public void PartialStaticConstructor()
        {
            CreateCompilation(new[]
            {
                """
                public class PartialStaticCtor
                {
                    partial static PartialStaticCtor() { }
                }
                """,
                """
                public class StaticPartialCtor
                {
                    static partial StaticPartialCtor() { }
                }
                """,
                """
                public class PublicStaticPartialCtor
                {
                    public static partial PublicStaticPartialCtor() { }
                }
                """,
                """
                public class PublicPartialStaticCtor
                {
                    public partial static PublicPartialStaticCtor() { }
                }
                """,
                """
                public class StaticPublicPartialCtor
                {
                    static public partial StaticPublicPartialCtor() { }
                }
                """,
                """
                public class StaticPartialPublicCtor
                {
                    static partial public StaticPartialPublicCtor() { }
                }
                """,
                """
                public class PartialStaticPublicCtor
                {
                    partial static public PartialStaticPublicCtor() { }
                }
                """,
                """
                public class PartialPublicStaticCtor
                {
                    partial public static PartialPublicStaticCtor() { }
                }
                """,
            }).VerifyDiagnostics(
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static PartialStaticCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static PartialStaticCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial public static PartialPublicStaticCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial public static PartialPublicStaticCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static public PartialStaticPublicCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static public PartialStaticPublicCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
                // (3,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     public partial static PublicPartialStaticCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 12),
                // (3,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     public partial static PublicPartialStaticCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 12),
                // (3,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     static partial public StaticPartialPublicCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 12),
                // (3,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     static partial public StaticPartialPublicCtor() { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 12),
                // (3,12): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     static partial StaticPartialCtor() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(3, 12),
                // (3,19): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     public static partial PublicStaticPartialCtor() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(3, 19),
                // (3,19): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     static public partial StaticPublicPartialCtor() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(3, 19),
                // (3,20): error CS0542: 'StaticPartialCtor': member names cannot be the same as their enclosing type
                //     static partial StaticPartialCtor() { }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "StaticPartialCtor").WithArguments("StaticPartialCtor").WithLocation(3, 20),
                // (3,20): error CS0161: 'StaticPartialCtor.StaticPartialCtor()': not all code paths return a value
                //     static partial StaticPartialCtor() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "StaticPartialCtor").WithArguments("StaticPartialCtor.StaticPartialCtor()").WithLocation(3, 20),
                // (3,27): error CS0515: 'PartialPublicStaticCtor.PartialPublicStaticCtor()': access modifiers are not allowed on static constructors
                //     partial public static PartialPublicStaticCtor() { }
                Diagnostic(ErrorCode.ERR_StaticConstructorWithAccessModifiers, "PartialPublicStaticCtor").WithArguments("PartialPublicStaticCtor.PartialPublicStaticCtor()").WithLocation(3, 27),
                // (3,27): error CS0515: 'PublicPartialStaticCtor.PublicPartialStaticCtor()': access modifiers are not allowed on static constructors
                //     public partial static PublicPartialStaticCtor() { }
                Diagnostic(ErrorCode.ERR_StaticConstructorWithAccessModifiers, "PublicPartialStaticCtor").WithArguments("PublicPartialStaticCtor.PublicPartialStaticCtor()").WithLocation(3, 27),
                // (3,27): error CS0515: 'StaticPartialPublicCtor.StaticPartialPublicCtor()': access modifiers are not allowed on static constructors
                //     static partial public StaticPartialPublicCtor() { }
                Diagnostic(ErrorCode.ERR_StaticConstructorWithAccessModifiers, "StaticPartialPublicCtor").WithArguments("StaticPartialPublicCtor.StaticPartialPublicCtor()").WithLocation(3, 27),
                // (3,27): error CS0515: 'PartialStaticPublicCtor.PartialStaticPublicCtor()': access modifiers are not allowed on static constructors
                //     partial static public PartialStaticPublicCtor() { }
                Diagnostic(ErrorCode.ERR_StaticConstructorWithAccessModifiers, "PartialStaticPublicCtor").WithArguments("PartialStaticPublicCtor.PartialStaticPublicCtor()").WithLocation(3, 27),
                // (3,27): error CS0542: 'StaticPublicPartialCtor': member names cannot be the same as their enclosing type
                //     static public partial StaticPublicPartialCtor() { }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "StaticPublicPartialCtor").WithArguments("StaticPublicPartialCtor").WithLocation(3, 27),
                // (3,27): error CS0542: 'PublicStaticPartialCtor': member names cannot be the same as their enclosing type
                //     public static partial PublicStaticPartialCtor() { }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "PublicStaticPartialCtor").WithArguments("PublicStaticPartialCtor").WithLocation(3, 27),
                // (3,27): error CS0161: 'StaticPublicPartialCtor.StaticPublicPartialCtor()': not all code paths return a value
                //     static public partial StaticPublicPartialCtor() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "StaticPublicPartialCtor").WithArguments("StaticPublicPartialCtor.StaticPublicPartialCtor()").WithLocation(3, 27),
                // (3,27): error CS0161: 'PublicStaticPartialCtor.PublicStaticPartialCtor()': not all code paths return a value
                //     public static partial PublicStaticPartialCtor() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "PublicStaticPartialCtor").WithArguments("PublicStaticPartialCtor.PublicStaticPartialCtor()").WithLocation(3, 27));
        }
    }
}
