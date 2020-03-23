// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AccessibilityTests : CSharpTestBase
    {
        private static readonly SemanticModel s_testModel;
        private static readonly int s_testPosition;
        private static readonly ISymbol s_testSymbol;

        static AccessibilityTests()
        {
            var t = Parse(@"
using System;
class C1
{
    void M() { object o = new object(); o.ToString(); }
}

");
            CSharpCompilation c = CreateEmptyCompilation(new[] { t });
            s_testModel = c.GetSemanticModel(t);
            s_testPosition = t.FindNodeOrTokenByKind(SyntaxKind.VariableDeclaration).SpanStart;
            s_testSymbol = c.GetWellKnownType(WellKnownType.System_Exception).GetPublicSymbol();
        }

        [Fact]
        public void IsAccessibleNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() =>
                s_testModel.IsAccessible(s_testPosition, null));
        }

        [Fact]
        public void IsAccessibleLocationNotInSource()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                s_testModel.IsAccessible(-1, s_testSymbol));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                s_testModel.IsAccessible(s_testModel.SyntaxTree.GetCompilationUnitRoot().FullSpan.End + 1, s_testSymbol));
        }

        [Fact]
        public void IsAccessibleSymbolErrorType()
        {
            Assert.True(
                s_testModel.IsAccessible(s_testPosition, s_testSymbol));
        }

        [WorkItem(527516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527516")]
        [Fact]
        public void IsAccessibleSymbolNotResolvable()
        {
            Symbol symbol = CSharpCompilation.Create(
                "NotResolvable",
                references: new MetadataReference[] { MscorlibRef }).GetWellKnownType(WellKnownType.System_Exception);

            Assert.True(
                s_testModel.IsAccessible(s_testPosition, symbol.GetPublicSymbol()));
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Property1()
        {
            var source = @"
public class G<T>
{
    protected class N { }

    protected G<int>.N P { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Property2()
        {
            var source = @"
public class G<T>
{
    protected class N { }
}

class C : G<int>
{
    protected G<long>.N P { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Indexer1()
        {
            var source = @"
public class G<T>
{
    protected class N { }

    protected G<int>.N this[int x] { get { throw null; } }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Indexer2()
        {
            var source = @"
public class G<T>
{
    protected class N { }
}

class C : G<int>
{
    protected G<long>.N this[int x] { get { throw null; } }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Method1()
        {
            var source = @"
public class G<T>
{
    protected class N { }

    protected G<int>.N M() { throw null; }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Method2()
        {
            var source = @"
public class G<T>
{
    protected class N { }
}

class C : G<int>
{
    protected G<long>.N M() { throw null; }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Event1()
        {
            var source = @"
public class G<T>
{
    protected delegate void N();

    protected event G<int>.N E;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,30): warning CS0067: The event 'G<T>.E' is never used
                //     protected event G<int>.N E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("G<T>.E"));
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypes_Event2()
        {
            var source = @"
public class G<T>
{
    protected delegate void N();
}

class C : G<int>
{
    protected event G<long>.N E;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,31): warning CS0067: The event 'C.E' is never used
                //     protected event G<long>.N E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [WorkItem(545450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545450")]
        [Fact]
        public void ProtectedTypesNestedInGenericTypesLegacy()
        {
            var source = @"
public class Bar<T> { }               

public class RuleE<T>
{
    protected class N { }
    public class M { }

    protected class Z : RuleE<int>.N
    { 
        protected RuleE<int>.N Goo;    
    }

    private class Z1
    {
        protected RuleE<int>.N Goo;    
    }

    protected class z4<S> where S : RuleE<int>.N { }
    protected class z5 : Bar<RuleE<int>.N> { } 

    protected RuleE<int>.N Fld1;  
    private RuleE<int>.N Fld3;    

    protected void Meth1(RuleE<int>.N arg) { }  
    protected void Meth2(Bar<RuleE<int>.N> arg) { }  
    protected RuleE<int>.N Meth3() { return null; } 

    protected RuleE<int>.N Prop1 { get { return null; } }  
    private RuleE<int>.N Prop3 { get { return null; } }  

    protected delegate void Del(RuleE<int>.N arg); 
}

public class D<T> : RuleE<T>
{
    protected RuleE<int>.N F1; 
    private RuleE<int>.N F3;   

    protected void M1(RuleE<int>.N arg) { } 
    protected void M2(Bar<RuleE<int>.N> arg) { } 
    protected RuleE<int>.N M3() { return null; } 
}

class Test
{
    static void Main()
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,32): warning CS0649: Field 'RuleE<T>.Z1.Goo' is never assigned to, and will always have its default value null
                //         protected RuleE<int>.N Goo;    
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Goo").WithArguments("RuleE<T>.Z1.Goo", "null"),
                // (23,26): warning CS0169: The field 'RuleE<T>.Fld3' is never used
                //     private RuleE<int>.N Fld3;    
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Fld3").WithArguments("RuleE<T>.Fld3"),
                // (38,26): warning CS0169: The field 'D<T>.F3' is never used
                //     private RuleE<int>.N F3;   
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F3").WithArguments("D<T>.F3"));
        }

        [WorkItem(531368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531368")]
        [Fact]
        public void TestDeepTypeAccessibilityBug18018()
        {
            // Bug 18018: Deep array types blow the stack during accessibility analysis.

            // We have since switched to an iterative rather than recursive algorithm.

            string brackets = "[][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][][]";
            brackets = brackets + brackets; // 80
            brackets = brackets + brackets; // 160
            brackets = brackets + brackets; // 320
            brackets = brackets + brackets; // 640
            brackets = brackets + brackets; // 1280
            brackets = brackets + brackets; // 2560
            brackets = brackets + brackets; // 5120
            brackets = brackets + brackets; // 10240

            var source = @"
    public class P
    {
        private class C {}
        public C " + brackets + @" x;
    }
";
            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("P.x", "P.C" + brackets)
);
        }

        [WorkItem(42567, "https://github.com/dotnet/roslyn/issues/42567")]
        [Fact]
        public void UsingStaticCanAccessPrivateType()
        {
            var comp = CreateCompilation(@"
using static Outer.Inner;
using System;

public class Outer
{
    public static void Main()
    {
        Console.Write(Prop);
    }
    private static class Inner
    {
        public static int Prop => 42;
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [WorkItem(42567, "https://github.com/dotnet/roslyn/issues/42567")]
        [Fact]
        public void CannotUsePrivateTypeImportedByUsingStaticWhenNotAccessible()
        {
            CreateCompilation(@"
using static Outer.Inner;
using System;

public class C
{
    public static void Main()
    {
        Console.Write(Prop);
    }
}

public class Outer
{

    private static class Inner
    {
        public static int Prop => 42;
    }
}")
                .VerifyDiagnostics(
                    // (2,1): hidden CS8019: Unnecessary using directive.
                    // using static Outer.Inner;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static Outer.Inner;").WithLocation(2, 1),
                    // (9,23): error CS0122: 'Outer.Inner.Prop' is inaccessible due to its protection level
                    //         Console.Write(Prop);
                    Diagnostic(ErrorCode.ERR_BadAccess, "Prop").WithArguments("Outer.Inner.Prop").WithLocation(9, 23)
                );
        }

        [WorkItem(42568, "https://github.com/dotnet/roslyn/issues/42568")]
        [Fact]
        public void UsingAliasCanAccessPrivateType()
        {
            var comp = CreateCompilation(@"
using In = Outer.Inner;
using System;

public class Outer
{
    public static void Main()
    {
        Console.Write(In.Prop);
    }
    private static class Inner
    {
        public static int Prop => 42;
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [WorkItem(42567, "https://github.com/dotnet/roslyn/issues/42567")]
        [Fact]
        public void CannotUsePrivateTypeImportedByAliasWhenNotAccessible()
        {
            CreateCompilation(@"
using In = Outer.Inner;
using System;

public class C
{
    public static void Main()
    {
        Console.Write(In.Prop);
    }
}

public class Outer
{
    private static class Inner
    {
        public static int Prop => 42;
    }
}")
                .VerifyDiagnostics(
                    // (2,1): hidden CS8019: Unnecessary using directive.
                    // using In = Outer.Inner;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using In = Outer.Inner;").WithLocation(2, 1),
                    // (9,23): error CS0122: 'Outer.Inner' is inaccessible due to its protection level
                    //         Console.Write(In.Prop);
                    Diagnostic(ErrorCode.ERR_BadAccess, "In").WithArguments("Outer.Inner").WithLocation(9, 23)
                );
        }

        [Fact]
        public void ImportNamespaceWithSameNameAsType()
        {
            var comp1 = CreateCompilation("internal class A {}");

            var comp = CreateCompilation(@"
using A;
using System;

namespace A
{
    public class PrintUtils
    {
        public static void Print(string str) => Console.Write(str);
    }
}

public class C
{
    public static void Main()
    {
        PrintUtils.Print(""Hello World"");
    }
}", options: TestOptions.ReleaseExe, references: new[] { comp1.ToMetadataReference() }).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "Hello World");
        }
    }
}
