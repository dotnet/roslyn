// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class BreakingChanges : CSharpTestBase
    {
        [Fact, WorkItem(527050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527050")]
        [Trait("Feature", "Directives")]
        public void TestCS1024DefineWithUnicodeInMiddle()
        {
            var test = @"#de\u0066in\U00000065 ABC";

            // This is now a negative test, this should not be allowed.
            SyntaxFactory.ParseSyntaxTree(test).GetDiagnostics().Verify(Diagnostic(ErrorCode.ERR_PPDirectiveExpected, @"de\u0066in\U00000065"));
        }

        [Fact, WorkItem(527951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527951")]
        public void CS0133ERR_NotConstantExpression05()
        {
            var text = @"
class A
{
    public void Do()
    {
        const object o1 = null;
        const string o2 = (string)o1; // Dev10 reports CS0133
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (7,22): warning CS0219: The variable 'o2' is assigned but its value is never used
                //         const string o2 = (string)o1; // Dev10 reports CS0133
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o2").WithArguments("o2")
                );
        }

        [WorkItem(527943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527943")]
        [Fact]
        public void CS0146ERR_CircularBase05()
        {
            var text = @"
interface IFace<T> { }

class B : IFace<B.C.D>
{
    public class C
    {
        public class D { }
    }
}
";
            var comp = CreateCompilation(text);
            // In Dev10, there was an error - ErrorCode.ERR_CircularBase at (4,7)
            Assert.Equal(0, comp.GetDiagnostics().Count());
        }
        [WorkItem(540371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540371"), WorkItem(530792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530792")]
        [Fact]

        private void CS0507ERR_CantChangeAccessOnOverride_TestSynthesizedSealedAccessorsInDifferentAssembly()
        {
            var source1 = @"
using System.Collections.Generic;
 
public class Base<T>
{
    public virtual List<T> Property1 { get { return null; } protected internal set { } }
    public virtual List<T> Property2 { protected internal get { return null; } set { } }
}";
            var compilation1 = CreateCompilation(source1);

            var source2 = @"
using System.Collections.Generic;
    
public class Derived : Base<int>
{
    public sealed override List<int> Property1 { get { return null; } }
    public sealed override List<int> Property2 { set { } }
}";
            var comp = CreateCompilation(source2, new[] { new CSharpCompilationReference(compilation1) });
            comp.VerifyDiagnostics();

            // This is not a breaking change - but it is a change in behavior from Dev10
            // Dev10 used to report following errors -
            // Error CS0507: 'Derived.Property1.set': cannot change access modifiers when overriding 'protected' inherited member 'Base<int>.Property1.set'
            // Error CS0507: 'Derived.Property2.get': cannot change access modifiers when overriding 'protected' inherited member 'Base<int>.Property2.get'
            // Roslyn makes this case work by synthesizing 'protected' accessors for the missing ones

            var baseClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseProperty1 = baseClass.GetMember<PropertySymbol>("Property1");
            var baseProperty2 = baseClass.GetMember<PropertySymbol>("Property2");

            Assert.Equal(Accessibility.Public, baseProperty1.DeclaredAccessibility);
            Assert.Equal(Accessibility.Public, baseProperty1.GetMethod.DeclaredAccessibility);
            Assert.Equal(Accessibility.ProtectedOrInternal, baseProperty1.SetMethod.DeclaredAccessibility);

            Assert.Equal(Accessibility.Public, baseProperty2.DeclaredAccessibility);
            Assert.Equal(Accessibility.ProtectedOrInternal, baseProperty2.GetMethod.DeclaredAccessibility);
            Assert.Equal(Accessibility.Public, baseProperty2.SetMethod.DeclaredAccessibility);

            var derivedClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty1 = derivedClass.GetMember<SourcePropertySymbol>("Property1");
            var derivedProperty2 = derivedClass.GetMember<SourcePropertySymbol>("Property2");

            Assert.Equal(Accessibility.Public, derivedProperty1.DeclaredAccessibility);
            Assert.Equal(Accessibility.Public, derivedProperty1.GetMethod.DeclaredAccessibility);
            Assert.Null(derivedProperty1.SetMethod);

            Assert.Equal(Accessibility.Public, derivedProperty2.DeclaredAccessibility);
            Assert.Null(derivedProperty2.GetMethod);
            Assert.Equal(Accessibility.Public, derivedProperty2.SetMethod.DeclaredAccessibility);

            var derivedProperty1Synthesized = derivedProperty1.SynthesizedSealedAccessorOpt;
            var derivedProperty2Synthesized = derivedProperty2.SynthesizedSealedAccessorOpt;

            Assert.Equal(MethodKind.PropertySet, derivedProperty1Synthesized.MethodKind);
            Assert.Equal(Accessibility.Protected, derivedProperty1Synthesized.DeclaredAccessibility);

            Assert.Equal(MethodKind.PropertyGet, derivedProperty2Synthesized.MethodKind);
            Assert.Equal(Accessibility.Protected, derivedProperty2Synthesized.DeclaredAccessibility);
        }

        // Confirm that this error no longer exists
        [Fact]
        public void CS0609ERR_NameAttributeOnOverride()
        {
            var text = @"
using System.Runtime.CompilerServices;

public class idx
{
   public virtual int this[int iPropIndex]
   {
      get    {    return 0;    }
      set    {    }
   }
}

public class MonthDays : idx
{
   [IndexerName(""MonthInfoIndexer"")]
   public override int this[int iPropIndex]
   {
      get    {    return 1;    }
      set    {    }
   }
}
";
            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics();

            var indexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("MonthDays").Indexers.Single();
            Assert.Equal(Microsoft.CodeAnalysis.WellKnownMemberNames.Indexer, indexer.Name);
            Assert.Equal("MonthInfoIndexer", indexer.MetadataName);
        }

        [WorkItem(527116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527116")]
        [Fact]
        public void RegressWarningInSingleMultiLineMixedXml()
        {
            var text = @"
/// <summary> 
/** This is the summary */
/// </summary>
class Test
{
    /** <summary> */
    /// This is the summary1
    /** </summary> */
    public int Field = 0;

    /** <summary> */
    /// This is the summary2
    /// </summary>
    string Prop { get; set; }

    /// <summary>
    /** This is the summary3
      * </summary> */
    static int Main() { return new Test().Field; }
}
";

            var tree = Parse(text, options: TestOptions.RegularWithDocumentationComments);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            // Dev10 allows (no warning)
            // Roslyn gives Warning CS1570 - "XML Comment has badly formed XML..."
            Assert.Equal(8, tree.GetDiagnostics().Count());
        }

        [Fact, WorkItem(527093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527093")]
        public void NoCS1570ForUndefinedXmlNamespace()
        {
            var text = @"
/// <xml> xmlns:s=""uuid: BDC6E3F0-6DA3-11d1-A2A3 - 00AA00C14882"">
/// <s:inventory>
/// </s:inventory>
/// </xml>
class A { }
";

            var tree = Parse(text, options: TestOptions.RegularWithDocumentationComments);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            // Native Warning CS1570 - "XML Comment has badly formed XML..."
            // Roslyn no 
            Assert.Empty(tree.GetDiagnostics());
        }

        [Fact, WorkItem(541345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541345")]
        public void CS0019_TestNullCoalesceWithNullOperandsErrors()
        {
            var source = @"
class Program
{
    static void Main()
    {
        // This is acceptable by the native compiler and treated as a non-constant null literal.
        // That violates the specification; we now correctly treat this as an error.
        object a = null ?? null;

        // The null coalescing operator never produces a compile-time constant even when
        // the arguments are constants.
        const object b = null ?? ""ABC"";
        const string c = ""DEF"" ?? null;
        const int d = (int?)null ?? 123;

        // It is legal, though pointless, to use null literals and constants in coalescing 
        // expressions, provided you don't try to make the result a constant. These should
        // produce no errors:
        object z = null ?? ""GHI"";
        string y = ""JKL"" ?? null;
        int x = (int?)null ?? 456;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null ?? null").WithArguments("??", "<null>", "<null>"),
                Diagnostic(ErrorCode.ERR_NotConstantExpression, @"null ?? ""ABC""").WithArguments("b"),
                Diagnostic(ErrorCode.ERR_NotConstantExpression, @"""DEF"" ?? null").WithArguments("c"),
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int?)null ?? 123").WithArguments("d"));
        }
        [Fact, WorkItem(528676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528676"), WorkItem(528676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528676")]

        private         // CS0657WRN_AttributeLocationOnBadDeclaration_AfterAttrDeclOrDelegate
                void CS1730ERR_CantUseAttributeOnInvaildLocation()
        {
            var test = @"using System;

[AttributeUsage(AttributeTargets.All)]
public class Goo : Attribute
{
    public int Name;
    public Goo(int sName) { Name = sName; }
}

public delegate void EventHandler(object sender, EventArgs e);

[assembly: Goo(5)]
public class Test { }
";

            // In Dev10, this is a warning CS0657
            // Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "assembly").WithArguments("assembly", "type")
            SyntaxFactory.ParseSyntaxTree(test).GetDiagnostics().Verify(Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "assembly"));
        }

        [WorkItem(528711, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528711")]
        [Fact]
        public void CS9259_StructLayoutCycle()
        {
            var text =
@"struct S1<T>
{
    S1<S1<T>>.S2 x;
    struct S2
    {
        static T x;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (3,18): warning CS0169: The field 'S1<T>.x' is never used
                //     S1<S1<T>>.S2 x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S1<T>.x").WithLocation(3, 18),
                // (6,18): warning CS0169: The field 'S1<T>.S2.x' is never used
                //         static T x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("S1<T>.S2.x").WithLocation(6, 18)
                );
        }

        [WorkItem(528094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528094")]
        [Fact]
        public void FormattingUnicodeNotPartOfId()
        {
            string source = @"
// <Area> Lexical - Unicode Characters</Area>
// <Title>
// Compiler considers identifiers, which differ only in formatting-character, as different ones;
// This is not actually correct behavior but for the time being this is what we expect
//</Title>
//<RelatedBugs>DDB:133151</RelatedBugs>
// <Expects Status=Success></Expects>

#define A
using System;

class Program
{
    static int Main()
    {
        int x = 0;

#if A == A\uFFFB
        x=1;
#endif

        Console.Write(x);
        return x;
    }
}
";
            // Dev10 print '0'
            CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(529000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529000")]
        [Fact]
        public void NoCS0121ForSwitchedParamNames_Dev10814222()
        {
            string source = @"
using System;

// Bug Dev10: 814222 resolved as Won't Fix
class Test
{
    static void Main()
    {
        Console.Write(Bar(x: 1, y: ""T0"")); // Dev10:CS0121
        Test01.Main01();
    }

    public static int Bar(int x, string y, params int[] z) { return 1; }
    public static int Bar(string y, int x) { return 0; } // Roslyn pick this one
}

class Test01
{
    public static int Bar<T>(int x, T y, params int[] z) { return 1; }
    public static int Bar<T>(string y, int x) { return 0; } // Roslyn pick this one

    public static int Goo<T>(int x, T y) { return 1; }
    public static int Goo<T>(string y, int x) { return 0; } // Roslyn pick this one

    public static int AbcDef<T>(int x, T y) { return 0; } // Roslyn pick this one
    public static int AbcDef<T>(string y, int x, params int[] z) { return 1; }

    public static void Main01()
    {
        Console.Write(Bar<string>(x: 1, y: ""T1""));    // Dev10:CS0121
        Console.Write(Goo<string>(x: 1, y: ""T2""));    // Dev10:CS0121
        Console.Write(AbcDef<string>(x: 1, y: ""T3"")); // Dev10:CS0121
    }
}
";

            CompileAndVerify(source, expectedOutput: "0000");
        }

        [WorkItem(529001, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529001")]
        [WorkItem(529002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529002")]
        [WorkItem(1067, "https://github.com/dotnet/roslyn/issues/1067")]
        [Fact]
        public void CS0185ERR_LockNeedsReference_RequireRefType()
        {
            var source = @"
class C
{
    void M<T, TClass, TStruct>() 
        where TClass : class 
        where TStruct : struct
    {
        lock (default(object)) ;
        lock (default(int)) ;       // CS0185
        lock (default(T)) {}        // new CS0185 - no constraints (Bug#10755)
        lock (default(TClass)) {}
        lock (default(TStruct)) {}  // new CS0185 - constraints to value type (Bug#10756)
        lock (null) {}              // new CS0185 - null is not an object type
    }
}
";
            var standardCompilation = CreateCompilation(source);
            var strictCompilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithStrictFeature());

            standardCompilation.VerifyDiagnostics(
                // (8,32): warning CS0642: Possible mistaken empty statement
                //         lock (default(object)) ;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(8, 32),
                // (9,29): warning CS0642: Possible mistaken empty statement
                //         lock (default(int)) ;       // CS0185
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(9, 29),
                // (9,15): error CS0185: 'int' is not a reference type as required by the lock statement
                //         lock (default(int)) ;       // CS0185
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "default(int)").WithArguments("int").WithLocation(9, 15),
                // (12,15): error CS0185: 'TStruct' is not a reference type as required by the lock statement
                //         lock (default(TStruct)) {}  // new CS0185 - constraints to value type (Bug#10756)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "default(TStruct)").WithArguments("TStruct").WithLocation(12, 15)
                );
            strictCompilation.VerifyDiagnostics(
                // (8,32): warning CS0642: Possible mistaken empty statement
                //         lock (default(object)) ;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(8, 32),
                // (9,29): warning CS0642: Possible mistaken empty statement
                //         lock (default(int)) ;       // CS0185
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(9, 29),
                // (9,15): error CS0185: 'int' is not a reference type as required by the lock statement
                //         lock (default(int)) ;       // CS0185
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "default(int)").WithArguments("int").WithLocation(9, 15),
                // (10,15): error CS0185: 'T' is not a reference type as required by the lock statement
                //         lock (default(T)) {}        // new CS0185 - no constraints (Bug#10755)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "default(T)").WithArguments("T").WithLocation(10, 15),
                // (12,15): error CS0185: 'TStruct' is not a reference type as required by the lock statement
                //         lock (default(TStruct)) {}  // new CS0185 - constraints to value type (Bug#10756)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "default(TStruct)").WithArguments("TStruct").WithLocation(12, 15),
                // (13,15): error CS0185: '<null>' is not a reference type as required by the lock statement
                //         lock (null) {}              // new CS0185 - null is not an object type
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "null").WithArguments("<null>").WithLocation(13, 15)
                );
        }

        [WorkItem(528972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528972")]
        [Fact]
        public void CS0121ERR_AmbigCall_Lambda1()
        {
            var text = @"
using System;

class A
{
    static void Main()
    {
        Goo( delegate () { throw new Exception(); }); // both Dev10 & Roslyn no error
        Goo(x: () => { throw new Exception(); });    // Dev10: CS0121, Roslyn: no error
    }
    public static void Goo(Action x)
    {
        Console.WriteLine(1);
    }
    public static void Goo(Func<int> x)
    {
        Console.WriteLine(2);   // Roslyn call this one
    }
}
";
            // Dev11 FIXED this, no error anymore (So Not breaking Dev11)
            // Dev10 reports CS0121 because ExpressionBinder::WhichConversionIsBetter fails to unwrap
            // the NamedArgumentSpecification to find the UNBOUNDLAMBDA and, thus, never calls
            // WhichLambdaConversionIsBetter.
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(529202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529202")]
        public void NoCS0029_ErrorOnZeroToEnumToTypeConversion()
        {
            string source = @"
class Program
{
    static void Main()
    {
        S s = 0;
    }
}

enum E
{
    Zero, One, Two
}

struct S
{
    public static implicit operator S(E s)
    {
        return E.Zero;
    }
}";
            // Dev10/11: (11,9): error CS0029: Cannot implicitly convert type 'int' to 'S'
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0029: Cannot implicitly convert type 'int' to 'S'
                //         S s = 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "S")
                );
        }

        [Fact, WorkItem(529242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529242")]
        public void ThrowOverflowExceptionForUncheckedCheckedLambda()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        Func<int, int> d = checked(delegate(int i)
        {
            int max = int.MaxValue;
            try
            {
                int n = max + 1;
            }
            catch (OverflowException)
            {
                Console.Write(""OV ""); // Roslyn throw
            }
            return i;
        });
        var r = unchecked(d(9));
        Console.Write(r);
    }
}
";

            CompileAndVerify(source, expectedOutput: "OV 9");
        }

        [WorkItem(529262, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529262")]
        [Fact]
        public void PartialMethod_ParameterAndTypeParameterNames()
        {
            var source =
@"using System;
using System.Reflection;
partial class C
{
    static partial void M<T, U>(T x, U y);
    static partial void M<T1, T2>(T1 y, T2 x)
    {
        Console.Write(""{0}, {1} | "", x, y);
        var m = typeof(C).GetMethod(""M"", BindingFlags.Static | BindingFlags.NonPublic);
        var tp = m.GetGenericArguments();
        Console.Write(""{0}, {1} | "", tp[0].Name, tp[1].Name);
        var p = m.GetParameters();
        Console.Write(""{0}, {1}"", p[0].Name, p[1].Name);
    }
    static void Main()
    {
        M(x: 1, y: 2);
    }
}";
            // Dev12 would emit "2, 1 | T1, T2 | x, y".
            CompileAndVerify(source, expectedOutput: "2, 1 | T, U | x, y");
        }

        [Fact, WorkItem(529279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529279")]
        public void NewCS0029_ImplicitlyUnwrapGenericNullable()
        {
            string source = @"
public class GenC<T, U> where T : struct, U
{
    public void Test(T t)
    {
        T? nt = t;
        U valueUn = nt;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,21): error CS0029: Cannot implicitly convert type 'T?' to 'U'
                //         U valueUn = nt;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "nt").WithArguments("T?", "U"));
        }

        [Fact, WorkItem(529280, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529280"), WorkItem(546864, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546864")]
        public void ExplicitUDCWithGenericConstraints()
        {
            // This compiles successfully in Dev10 dues to a bug; a user-defined conversion
            // that converts from or to an interface should never be chosen.  If you are
            // converting from Alpha to Beta via standard conversion, then Beta to Gamma
            // via user-defined conversion, then Gamma to Delta via standard conversion, then
            // none of Alpha, Beta, Gamma or Delta should be allowed to be interfaces. The 
            // Dev10 compiler only checks Alpha and Delta, not Beta and Gamma.
            //
            // Unfortunately, real-world code both in devdiv and in the wild depends on this
            // behavior, so we are replicating the bug in Roslyn.

            string source = @"using System;

public interface IGoo {    void Method();    }
public class CT : IGoo {    public void Method() { }    }

public class GenC<T>  where T : IGoo
{
    public T valueT;
    public static explicit operator T(GenC<T> val)
    {
        Console.Write(""ExpConv"");
        return val.valueT;
    }
}

public class Test
{
    public static void Main()
    {
        var _class = new GenC<IGoo>();
        var ret = (CT)_class;
    }
}
";

            // CompileAndVerify(source, expectedOutput: "ExpConv");
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(529362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529362")]
        public void TestNullCoalescingOverImplicitExplicitUDC()
        {
            string source = @"using System;

struct GenS<T> where T : struct
{
    public static int Flag;

    public static explicit operator T?(GenS<T> s)
    {
        Flag = 3;
        return default(T);
    }

    public static implicit operator T(GenS<T> s)
    {
        Flag = 2;
        return default(T);
    }
}

class Program
{
    public static void Main()
    {
        int? int_a1 = 1;
        GenS<int>? s28 = new GenS<int>();
        // Due to related bug dev10: 742047 is won't fix
        // so expects the code will call explicit conversion operator
        int? result28 = s28 ?? int_a1;
        Console.WriteLine(GenS<int>.Flag);
    }
}
";
            // Native compiler picks explicit conversion - print 3
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact, WorkItem(529362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529362")]
        public void TestNullCoalescingOverImplicitExplicitUDC_2()
        {
            string source = @"using System;

struct S
{
    public static explicit operator int?(S? s)
    {
        Console.WriteLine(""Explicit"");
        return 3;
    }

    public static implicit operator int(S? s)
    {
        Console.WriteLine(""Implicit"");
        return 2;
    }
}

class Program
{
    public static void Main()
    {
        int? nn = -1;
        S? s = new S();
        int? ret = s ?? nn;
    }
}
";
            // Native compiler picks explicit conversion
            CompileAndVerify(source, expectedOutput: "Implicit");
        }

        [Fact, WorkItem(529363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529363")]
        public void AssignmentNullCoalescingOperator()
        {
            string source = @"using System;

class NullCoalescingTest
{
    public static void Main()
    {
        int? a;
        int c;
        var x = (a = null) ?? (c = 123);
        Console.WriteLine(c);
    }
}
";
            // Native compiler no error (print -123)
            CreateCompilation(source).VerifyDiagnostics(
                // (10,27): error CS0165: Use of unassigned local variable 'c'
                //         Console.WriteLine(c);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c"),
                // (7,14): warning CS0219: The variable 'a' is assigned but its value is never used
                //         int? a;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a"));
        }

        [Fact, WorkItem(529464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529464")]
        public void MultiDimensionArrayWithDiffTypeIndexDevDiv31328()
        {
            var text = @"
class A
{
    public static void Main()
    {
        byte[,] arr = new byte[1, 1];
        ulong i1 = 0;
        int i2 = 1;
        arr[i1, i2] = 127; // Dev10 CS0266
    }
}
";
            // Dev10 Won't fix bug#31328 for md array with different types' index and involving ulong
            // CS0266: Cannot implicitly convert type 'ulong' to 'int'. ...
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void PointerArithmetic_SubtractNullLiteral()
        {
            var text = @"
unsafe class C
{
    long M(int* p)
    {
        return p - null; //Dev10 reports CS0019
    }
}
";
            // Dev10: the null literal is treated as though it is converted to void*, making the subtraction illegal (ExpressionBinder::GetPtrBinOpSigs).
            // Roslyn: the null literal is converted to int*, making the subtraction legal.
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void CS0181ERR_BadAttributeParamType_Nullable()
        {
            var text = @"
[Boom]
class Boom : System.Attribute
{
    public Boom(int? x = 0) { }  

    static void Main()
    {
        typeof(Boom).GetCustomAttributes(true);
    }
}
";
            // Roslyn: error CS0181: Attribute constructor parameter 'x' has type 'int?', which is not a valid attribute parameter type
            // Dev10/11: no error, but throw at runtime - System.Reflection.CustomAttributeFormatException
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Boom").WithArguments("x", "int?"));
        }

        /// <summary>
        /// When determining whether the LHS of a null-coalescing operator (??) is non-null, the native compiler strips off casts.  
        /// 
        /// We have decided not to reproduce this behavior.
        /// </summary>
        [Fact]
        public void CastOnLhsOfConditionalOperator()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        int i;
        int? j = (int?)1 ?? i; //dev10 accepts, since it treats the RHS as unreachable.

        int k;
        int? l = ((int?)1 ?? j) ?? k; // If the LHS of the LHS is non-null, then the LHS should be non-null, but dev10 only handles casts.

        int m;
        int? n = ((int?)1).HasValue ? ((int?)1).Value : m; //dev10 does not strip casts in a comparable conditional operator
    }
}
";
            // Roslyn: error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('object')
            // Dev10/11: no error
            CreateCompilation(text).VerifyDiagnostics(
                // This is new in Roslyn.

                // (7,29): error CS0165: Use of unassigned local variable 'i'
                //         int? j = (int?)1 ?? i; //dev10 accepts, since it treats the RHS as unreachable.
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i"),

                // These match Dev10.

                // (10,36): error CS0165: Use of unassigned local variable 'k'
                //         int? l = ((int?)1 ?? j) ?? k; // If the LHS of the LHS is non-null, then the LHS should be non-null, but dev10 only handles casts.
                Diagnostic(ErrorCode.ERR_UseDefViolation, "k").WithArguments("k"),
                // (13,57): error CS0165: Use of unassigned local variable 'm'
                //         int? n = ((int?)1).HasValue ? ((int?)1).Value : m; //dev10 does not strip casts in a comparable conditional operator
                Diagnostic(ErrorCode.ERR_UseDefViolation, "m").WithArguments("m"));
        }

        [WorkItem(529974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529974")]
        [Fact]
        public void TestCollisionForLoopControlVariable()
        {
            var source = @"
namespace Microsoft.Test.XmlGen.Protocols.Saml2
{
    using System;
    using System.Collections.Generic;

    public class ProxyRestriction 
    {
        XmlGenIntegerAttribute count;
        private List<XmlGenAttribute> Attributes { get; set; }

        public ProxyRestriction()
        {
            for (int count = 0; count < 10; count++)
            {
            }

            for (int i = 0; i < this.Attributes.Count; i++)
            {
                XmlGenAttribute attribute = this.Attributes[i];
                if (attribute.LocalName == null)
                {
                    if (count is XmlGenIntegerAttribute)
                    {
                        count = (XmlGenIntegerAttribute)attribute;
                    }
                    else
                    {
                        count = new XmlGenIntegerAttribute(attribute);
                        this.Attributes[i] = count;
                    }
                    break;
                }
            }

            if (count == null)
            {
                this.Attributes.Add(new XmlGenIntegerAttribute(String.Empty, null, String.Empty, -1, false));
            }
        }
    }

    internal class XmlGenAttribute
    {
        public object LocalName { get; internal set; }
    }

    internal class XmlGenIntegerAttribute : XmlGenAttribute
    {
        public XmlGenIntegerAttribute(string empty1, object count, string empty2, int v, bool isPresent)
        {
        }

        public XmlGenIntegerAttribute(XmlGenAttribute attribute)
        {
        }
    }
}

public class Program
{
    public static void Main() { }
}";
            // Dev11 reported no errors for the above repro and allowed the name 'count' to bind to different
            // variables within the same declaration space. According to the old spec the error should be reported.
            // In Roslyn the single definition rule is relaxed and we do not give an error, but for a different reason.
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(530301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530301")]
        public void NoMore_CS0458WRN_AlwaysNull02()
        {
            CreateCompilation(
@"
public class Test
{
    const bool ct = true;

    public static int Main()
    {
        var a = (true & null) ?? null;  // CS0458
        if (((null & true) == null))   // CS0458
            return 0;

        var b = !(null | false);      // CS0458
        if ((false | null) != null)   // CS0458
            return 1;

        const bool cf = false;
        var d = ct & null ^ null;  // CS0458 - Roslyn Warn this ONLY
        d ^= !(null | cf);        // CS0458

        return -1;
    }
}
")
                // We decided to not report WRN_AlwaysNull in some cases.

                .VerifyDiagnostics(
                    // Diagnostic(ErrorCode.WRN_AlwaysNull, "true & null").WithArguments("bool?"),
                    // Diagnostic(ErrorCode.WRN_AlwaysNull, "null & true").WithArguments("bool?"),
                    // Diagnostic(ErrorCode.WRN_AlwaysNull, "null | false").WithArguments("bool?"),
                    // Diagnostic(ErrorCode.WRN_AlwaysNull, "false | null").WithArguments("bool?"),
                    Diagnostic(ErrorCode.WRN_AlwaysNull, "ct & null ^ null").WithArguments("bool?") //,
                                                                                                    // Diagnostic(ErrorCode.WRN_AlwaysNull, "null | cf").WithArguments("bool?")
                    );
        }

        [WorkItem(530403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530403")]
        [Fact]
        public void CS0135_local_param_cannot_be_declared()
        {
            // Dev11 missed this error.  The issue is that an a is a field of c, and then it is a local in parts of d.  
            // by then referring to the field without the this keyword, it should be flagged as declaring a competing variable (as it is confusing).
            var text = @"
using System;
public class c
{
    int a = 0;

    void d(bool b) {
       if(b)
       {
          int a = 1;
          Console.WriteLine(a);
       }
       else
       {
          a = 2;
       }
       a = 3;

       Console.WriteLine(a);
   }
} 
";
            var comp = CreateCompilation(text);

            // In Roslyn the single definition rule is relaxed and we do not give an error.
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        [WorkItem(530518, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530518")]
        public void ExpressionTreeExplicitOpVsConvert()
        {
            var text = @"
using System;
using System.Linq;
using System.Linq.Expressions;
class Test
{
public static explicit operator int(Test x) { return 1; }
static void Main()
{
Expression<Func<Test, long?>> testExpr1 = x => (long?)x;
Console.WriteLine(testExpr1);
Expression<Func<Test, decimal?>> testExpr2 = x => (decimal?)x;
Console.WriteLine(testExpr2);
}
}
";

            // Native Compiler: x => Convert(Convert(op_Explicit(x)))
            CompileAndVerify(text, expectedOutput:
@"x => Convert(Convert(Convert(x)))
x => Convert(Convert(Convert(x)))
");
        }

        [WorkItem(530531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530531")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        private void ExpressionTreeNoCovertForIdentityConversion()
        {
            var source = @"
using System;
using System.Linq;
using System.Linq.Expressions;

class Test
{
static void Main()
{
    Expression<Func<Guid, bool>> e = (x) => x != null;
    Console.WriteLine(e);
    Console.WriteLine(e.Compile()(default(Guid)));
}
}
";

            // Native compiler: x => (Convert(x) != Convert(null))
            CompileAndVerify(source, expectedOutput:
@"x => (Convert(x) != null)
True
");
        }

        [Fact, WorkItem(530548, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530548")]
        public void CS0219WRN_UnreferencedVarAssg_RHSMidRefType()
        {
            string source = @"
public interface Base { }
public struct Derived : Base { }
public class Test
{
    public static void Main()
    {
        var b1 = new Derived(); // Both Warning CS0219
        var b2 = (Base)new Derived(); // Both NO Warn (reference type)
        var b3 = (Derived)((Base)new Derived()); // Roslyn Warning CS0219
    }
}
";
            // Native compiler no error (print -123)
            CreateCompilation(source).VerifyDiagnostics(
    // (8,13): warning CS0219: The variable 'b1' is assigned but its value is never used
    //         var b1 = new Derived(); // Both Warning CS0219
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "b1").WithArguments("b1"),
    // (10,13): warning CS0219: The variable 'b3' is assigned but its value is never used
    //         var b3 = (Derived)((Base)new Derived()); // Roslyn Warning CS0219
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "b3").WithArguments("b3"));
        }

        [Fact, WorkItem(530556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530556")]
        public void NoCS0591ERR_InvalidAttributeArgument()
        {
            string source = @"
using System;
[AttributeUsage(AttributeTargets.All + 0xFFFFFF)]
class MyAtt : Attribute { }
[AttributeUsage((AttributeTargets)0xffff)]
class MyAtt1 : Attribute { }
public class Test {}
";
            // Native compiler  error CS0591: Invalid value for argument to 'AttributeUsage' attribute
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(530586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530586")]
        public void ThrowOnceInIteratorFinallyBlock()
        {
            string source = @"
//<Title> Finally block runs twice in iterator</Title>
//<RelatedBug>Dev10:473561-->8444?</RelatedBug>
using System;
using System.Collections;
class Program
{
    public static void Main()
    {
        var demo = new test();
        try
        {
            foreach (var x in demo.Goo()) { }
        }
        catch (Exception)
        {
            Console.Write(""EX "");
        }
        Console.WriteLine(demo.count);
    }
    class test
    {
        public int count = 0;
        public IEnumerable Goo()
        {
            try
            {
                yield return null;
                try
                {
                    yield break;
                }
                catch
                { }
            }
            finally
            {
                Console.Write(""++ "");
                ++count;
                throw new Exception();
            }
        }
    }
}
";
            // Native print "++ ++ EX 2"
            var verifier = CompileAndVerify(source, expectedOutput: " ++ EX 1");

            // must not load "<>4__this"
            verifier.VerifyIL("Program.test.<Goo>d__1.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      101 (0x65)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int Program.test.<Goo>d__1.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  ldloc.1
    IL_000b:  ldc.i4.1
    IL_000c:  beq.s      IL_0033
    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  leave.s    IL_0063
    IL_0012:  ldarg.0
    IL_0013:  ldc.i4.m1
    IL_0014:  stfld      ""int Program.test.<Goo>d__1.<>1__state""
    IL_0019:  ldarg.0
    IL_001a:  ldc.i4.s   -3
    IL_001c:  stfld      ""int Program.test.<Goo>d__1.<>1__state""
    IL_0021:  ldarg.0
    IL_0022:  ldnull
    IL_0023:  stfld      ""object Program.test.<Goo>d__1.<>2__current""
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.1
    IL_002a:  stfld      ""int Program.test.<Goo>d__1.<>1__state""
    IL_002f:  ldc.i4.1
    IL_0030:  stloc.0
    IL_0031:  leave.s    IL_0063
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.s   -3
    IL_0036:  stfld      ""int Program.test.<Goo>d__1.<>1__state""
    .try
    {
      IL_003b:  ldc.i4.0
      IL_003c:  stloc.0
      IL_003d:  leave.s    IL_004a
    }
    catch object
    {
      IL_003f:  pop
      IL_0040:  leave.s    IL_0042
    }
    IL_0042:  ldarg.0
    IL_0043:  call       ""void Program.test.<Goo>d__1.<>m__Finally1()""
    IL_0048:  br.s       IL_0052
    IL_004a:  ldarg.0
    IL_004b:  call       ""void Program.test.<Goo>d__1.<>m__Finally1()""
    IL_0050:  leave.s    IL_0063
    IL_0052:  leave.s    IL_005b
  }
  fault
  {
    IL_0054:  ldarg.0
    IL_0055:  call       ""void Program.test.<Goo>d__1.Dispose()""
    IL_005a:  endfinally
  }
  IL_005b:  ldarg.0
  IL_005c:  call       ""void Program.test.<Goo>d__1.Dispose()""
  IL_0061:  ldc.i4.1
  IL_0062:  stloc.0
  IL_0063:  ldloc.0
  IL_0064:  ret
}
");

            // must load "<>4__this"
            verifier.VerifyIL("Program.test.<Goo>d__1.<>m__Finally1()", @"
{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.m1
  IL_0002:  stfld      ""int Program.test.<Goo>d__1.<>1__state""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""Program.test Program.test.<Goo>d__1.<>4__this""
  IL_000d:  ldstr      ""++ ""
  IL_0012:  call       ""void System.Console.Write(string)""
  IL_0017:  dup
  IL_0018:  ldfld      ""int Program.test.count""
  IL_001d:  ldc.i4.1
  IL_001e:  add
  IL_001f:  stfld      ""int Program.test.count""
  IL_0024:  newobj     ""System.Exception..ctor()""
  IL_0029:  throw
}
");

        }

        [Fact, WorkItem(530587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530587")]
        public void NoFormatCharInIDEqual()
        {
            string source = @"
#define A
using System;
class Program
{
static int Main()
{
 int x = 0;
#if A == A\uFFFB
 x=1;
#endif
Console.Write(x);
return x;
}
}
";
            CompileAndVerify(source, expectedOutput: "1"); // Native print 0
        }

        [Fact, WorkItem(530614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530614")]
        public void CS1718WRN_ComparisonToSelf_Roslyn()
        {
            string source = @"
enum esbyte : sbyte { e0, e1 };
public class z_1495j12
{
public static void Main()
{
if (esbyte.e0 == esbyte.e0)
{
  System.Console.WriteLine(""T"");
}
}}
";
            // Native compiler no warn
            CreateCompilation(source).VerifyDiagnostics(
                // (7,5): warning CS1718: Comparison made to same variable; did you mean to compare something else?
                Diagnostic(ErrorCode.WRN_ComparisonToSelf, "esbyte.e0 == esbyte.e0"));
        }

        [Fact, WorkItem(530629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530629")]
        public void CS0414WRN_UnreferencedFieldAssg_Roslyn()
        {
            string source = @"
namespace VS7_336319
{
    public sealed class PredefinedTypes
    {
        public class Kind { public static int Decimal; }
    }
    public class ExpressionBinder
    {
        private static PredefinedTypes PredefinedTypes = null;
        private void Goo()
        {
            if (0 == (int)PredefinedTypes.Kind.Decimal) { }
        }
    }
}
";
            // Native compiler no warn
            CreateCompilation(source).VerifyDiagnostics(
    // (10,40): warning CS0414: The field 'VS7_336319.ExpressionBinder.PredefinedTypes' is assigned but its value is never used
    //         private static PredefinedTypes PredefinedTypes = null;
    Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "PredefinedTypes").WithArguments("VS7_336319.ExpressionBinder.PredefinedTypes"));
        }

        [WorkItem(530666, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530666")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        public void ExpressionTreeWithNullableUDCandOperator()
        {
            string source = @"
using System;
using System.Linq.Expressions;
struct B
{
    public static implicit operator int?(B x)
    {
        return 1;
    }

public static int operator +(B x, int y)
    {
        return 2 + y;
    }

static int Main()
    {
        Expression<Func<B, int?>> f = x => x + x;
        var ret = f.Compile()(new B());
        Console.WriteLine(ret);
        return ret.Value - 3;
    }
}
";
            // Native compiler throw
            CompileAndVerify(source, expectedOutput: "3");
        }

        [Fact, WorkItem(530696, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530696")]
        public void CS0121Err_AmbiguousMethodCall()
        {
            string source = @"
    class G<T> { }
    class C
    {
        static void M(params double[] x)
        {
            System.Console.WriteLine(1);
        }
        static void M(params G<int>[] x)
        {
            System.Console.WriteLine(2);
        }
        static void Main()
        {
            M();
        }
    }
";

            CreateCompilation(source).VerifyDiagnostics(
    // (15,13): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params double[])' and 'C.M(params G<int>[])'
    //             M();
    Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params double[])", "C.M(params G<int>[])"));
        }

        [Fact, WorkItem(530653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530653")]
        public void RepeatedObsoleteWarnings()
        {
            // <quote source="Srivatsn's comments from bug 16642">
            // Inside a method body if you access a static field twice thus:
            // 
            // var x = ObsoleteType.field1;
            // var y = ObsoleteType.field1;
            //
            // then the native compiler reports ObsoleteType as obsolete only once. This is because the native compiler caches
            // the lookup of type names for certain cases and doesn't report errors on the second lookup as that just comes 
            // from the cache. Note how I said caches sometimes. If you simply say -
            //
            // var x= new ObsoleteType();
            // var y = new ObsoleteType();
            //
            // Then the native compiler reports the error twice. I don't think we should replicate this in Roslyn. Note however
            // that this is a breaking change because if the first line had been #pragma disabled, then the code would compile
            // without warnings in Dev11 but we will report warnings. I think it's a corner enough scenario and the native
            // behavior is quirky enough to warrant a break.
            // </quote>
            CompileAndVerify(@"
using System;
[Obsolete]
public class ObsoleteType
{
    public static readonly int field = 0;
}
public class Program
{
    public static void Main()
    {
        #pragma warning disable 0612
        var x = ObsoleteType.field;
        #pragma warning restore 0612
        var y = ObsoleteType.field; // In Dev11, this line doesn't produce a warning.
    }
}").VerifyDiagnostics(
                // (15,17): warning CS0612: 'ObsoleteType' is obsolete
                //         var y = ObsoleteType.field;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ObsoleteType").WithArguments("ObsoleteType"));
        }

        [Fact, WorkItem(530303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530303")]
        public void TestReferenceResolution()
        {
            var cs1Compilation = CreateCSharpCompilation("CS1",
@"public class CS1 {}",
                compilationOptions: TestOptions.ReleaseDll);
            var cs1Verifier = CompileAndVerify(cs1Compilation);
            cs1Verifier.VerifyDiagnostics();

            var cs2Compilation = CreateCSharpCompilation("CS2",
@"public class CS2<T> {}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new Compilation[] { cs1Compilation });
            var cs2Verifier = CompileAndVerify(cs2Compilation);
            cs2Verifier.VerifyDiagnostics();

            var cs3Compilation = CreateCSharpCompilation("CS3",
@"public class CS3 : CS2<CS1> {}",
                compilationOptions: TestOptions.ReleaseDll,
                referencedCompilations: new Compilation[] { cs1Compilation, cs2Compilation });
            var cs3Verifier = CompileAndVerify(cs3Compilation);
            cs3Verifier.VerifyDiagnostics();

            var cs4Compilation = CreateCSharpCompilation("CS4",
@"public class Program
{
    static void Main()
    {
        System.Console.WriteLine(typeof(CS3));
    }
}",
                compilationOptions: TestOptions.ReleaseExe,
                referencedCompilations: new Compilation[] { cs2Compilation, cs3Compilation });
            cs4Compilation.VerifyDiagnostics();
        }

        [Fact, WorkItem(531014, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531014")]
        public void TestVariableAndTypeNameClashes()
        {
            CompileAndVerify(@"
using System;
public class Class1
{
    internal class A4 { internal class B { } internal static string F() { return ""A4""; } }
    internal class A5 { internal class B { } internal static string F() { return ""A5""; } }
    internal class A6 { internal class B { } internal static string F() { return ""A6""; } }
    internal delegate void D();        // Check the weird E.M cases.
    internal class Outer2
    {
        internal static void F(A4 A4)
        {
            A5 A5; const A6 A6 = null;
            Console.WriteLine(typeof(A4.B));
            Console.WriteLine(typeof(A5.B));
            Console.WriteLine(typeof(A6.B));
            Console.WriteLine(A4.F());
            Console.WriteLine(A5.F());
            Console.WriteLine(A6.F());
            Console.WriteLine(default(A4) == null);
            Console.WriteLine(default(A5) == null);
            Console.WriteLine(default(A6) == null);
        }
    }
}").VerifyDiagnostics(
                // Breaking Change: See bug 17395. Dev11 had a bug because of which it didn't report the below warnings.
                // (13,16): warning CS0168: The variable 'A5' is declared but never used
                //             A5 A5; const A6 A6 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "A5").WithArguments("A5"),
                // (13,29): warning CS0219: The variable 'A6' is assigned but its value is never used
                //             A5 A5; const A6 A6 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "A6").WithArguments("A6"));
        }

        [WorkItem(530584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530584")]
        [Fact]
        public void NotRuntimeAmbiguousBecauseOfReturnTypes()
        {
            var source = @"
using System;

class Base<T, S>
{
    public virtual int Goo(ref S x) { return 0; }
    public virtual string Goo(out T x)
    {
        x = default(T); return ""Base.Out"";
    }
}

class Derived : Base<int, int>
{
    public override string Goo(out int x)
    {
        x = 0; return ""Derived.Out"";
    }
    static void Main()
    {
        int x;
        Console.WriteLine(new Derived().Goo(out x));
    }
}
";
            // BREAK: Dev11 reports WRN_MultipleRuntimeOverrideMatches, but there
            // is no runtime ambiguity because the return types differ.
            CompileAndVerify(source, expectedOutput: "Derived.Out");
        }

        [WorkItem(695311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/695311")]
        [Fact]
        public void NestedCollectionInitializerOnGenericProperty()
        {
            var libSource = @"
using System.Collections;

public interface IAdd
{
    void Add(object o);
}

public struct S : IEnumerable, IAdd
{
    private ArrayList list;

    public void Add(object o)
    {
        list = list ?? new ArrayList();
        list.Add(o);
    }

    public IEnumerator GetEnumerator() 
    { 
        return (list ?? new ArrayList()).GetEnumerator();
    }
}

public class C : IEnumerable, IAdd
{
    private readonly ArrayList list = new ArrayList();

    public void Add(object o)
    {
        list.Add(o);
    }

    public IEnumerator GetEnumerator()
    {
        return list.GetEnumerator();
    }
}

public class Wrapper<T> : IEnumerable where T : IEnumerable, new()
{
    public Wrapper()
    {
        this.Item = new T();
    }

    public T Item { get; private set; }

    public IEnumerator GetEnumerator()
    {
        return Item.GetEnumerator();
    }
}

public static class Util
{
    public static int Count(IEnumerable i)
    {
        int count = 0;
        foreach (var v in i) count++;
        return count;
    }
}
";

            var libRef = CreateCompilation(libSource, assemblyName: "lib").EmitToImageReference();

            {
                var source = @"
using System;
using System.Collections;

class Test
{
    static void Main()
    {
        Console.Write(Util.Count(Goo<S>()));
        Console.Write(Util.Count(Goo<C>()));
    }

    static Wrapper<T> Goo<T>() where T : IEnumerable, IAdd, new()
    {
        return new Wrapper<T> { Item = { 1, 2, 3} };
    }
}
";

                // As in dev11.
                var comp = CreateCompilation(source, new[] { libRef }, TestOptions.ReleaseExe);
                CompileAndVerify(comp, expectedOutput: "03");
            }

            {
                var source = @"
using System;
using System.Collections;

class Test
{
    static void Main()
    {
        Console.Write(Util.Count(Goo<C>()));
    }

    static Wrapper<T> Goo<T>() where T : class, IEnumerable, IAdd, new()
    {
        return new Wrapper<T> { Item = { 1, 2, 3} };
    }
}
";

                // As in dev11.
                // NOTE: The spec will likely be updated to make this illegal.
                var comp = CreateCompilation(source, new[] { libRef }, TestOptions.ReleaseExe);
                CompileAndVerify(comp, expectedOutput: "3");
            }

            {
                var source = @"
using System;
using System.Collections;

class Test
{
    static void Main()
    {
        Console.Write(Util.Count(Goo<S>()));
    }

    static Wrapper<T> Goo<T>() where T : struct, IEnumerable, IAdd
    {
        return new Wrapper<T> { Item = { 1, 2, 3} };
    }
}
";

                // BREAK: dev11 compiles and prints "0"
                var comp = CreateCompilation(source, new[] { libRef }, TestOptions.ReleaseExe);
                comp.VerifyDiagnostics(
                    // (15,33): error CS1918: Members of property 'Wrapper<T>.Item' of type 'T' cannot be assigned with an object initializer because it is of a value type
                    //         return new Wrapper<T> { Item = { 1, 2, 3} };
                    Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "Item").WithArguments("Wrapper<T>.Item", "T"));
            }
        }

        [Fact, WorkItem(770424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770424"), WorkItem(1079034, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079034")]
        public void UserDefinedShortCircuitingOperators()
        {
            var source = @"
public class Base
{
    public static bool operator true(Base x)
    {
        System.Console.Write(""Base.op_True"");
        return x != null;
    }
    public static bool operator false(Base x)
    {
        System.Console.Write(""Base.op_False"");
        return x == null;
    }
}

public class Derived : Base
{
    public static Derived operator&(Derived x, Derived y)
    {
        return x;
    }

    public static Derived operator|(Derived x, Derived y)
    {
        return y;
    }

    static void Main()
    {
        Derived d = new Derived();
        var b = (d && d) || d;
    }
}
";
            CompileAndVerify(source, expectedOutput: "Base.op_FalseBase.op_True");
        }
    }
}
