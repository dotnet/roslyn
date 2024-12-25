// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StructTests : FlowTestBase
    {
        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void TupleFieldNameAliasing()
        {
            var comp = CreateCompilationWithMscorlib40(@"
using System;

class C
{
    void M()
    {
        (int x, int y) t;
        t.x = 0;
        Console.Write(t.x);
        Console.Write(t.Item1);
    }
    void M2()
    {
        (int x, int y) t;
        Console.Write(t.y);
        // No error, error is reported once per field
        // and t.Item2 is alias for the same field
        Console.Write(t.Item2);
    }
    void M3()
    {
        (int x, int y) t;
        Console.Write(t.Item2);
        // No error, error is reported once per field
        // and t.y is alias for the same field
        Console.Write(t.y);
    }
}", references: new[] { SystemRuntimeFacadeRef, ValueTupleRef });
            comp.VerifyDiagnostics(
                // (16,23): error CS0170: Use of possibly unassigned field 'y'
                //         Console.Write(t.y);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "t.y").WithArguments("y").WithLocation(16, 23),
                // (24,23): error CS0170: Use of possibly unassigned field 'Item2'
                //         Console.Write(t.Item2);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "t.Item2").WithArguments("Item2").WithLocation(24, 23));
        }

        [Fact]
        public void SelfDefaultConstructor()
        {
            string program = @"
struct S
{
    public int x, y;
    public S(int x, int y) : this() { this.x = x; this.y = y; }
}
";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics();

            var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
            var constructors = structType.GetMembers(WellKnownMemberNames.InstanceConstructorName);
            Assert.Equal(2, constructors.Length);

            var sourceConstructor = (MethodSymbol)constructors.First(c => !c.IsImplicitlyDeclared);
            var synthesizedConstructor = (MethodSymbol)constructors.First(c => c.IsImplicitlyDeclared);
            Assert.NotEqual(sourceConstructor, synthesizedConstructor);

            Assert.Equal(2, sourceConstructor.Parameters.Length);
            Assert.Equal(0, synthesizedConstructor.Parameters.Length);
        }

        [Fact, WorkItem(543133, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543133")]
        public void FieldAssignedAndReferenced()
        {
            var text =
@"using System;
 
class Program
{
    static P p;
 
    static void Main(string[] args)
    {
        p.X = 5;
        Console.WriteLine(p.X);
    }
}
 
struct P { public int X; }
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestEmptyStructs()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowStatements(
@"struct S { }

class C
{
    public static void Main(string[] args)
    {
        S s1;
        S s2;
        S s3;
        /*<bind>*/
        S s4 = s2;
        s3 = s4;
        /*</bind>*/
        S s6 = s1;
        S s5 = s4;
    }
}");
            Assert.Equal("s2", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("s4", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("s3, s4", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
        }

        [Fact]
        [WorkItem(545509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545509")]
        public void StructIndexerReceiver()
        {
            string program = @"
struct SafeBitVector
{
    private int _data;
    internal bool this[int bit]
    {
        get
        {
            return (_data & bit) == bit;
        }
        set
        {
            _data = value ? _data | bit : _data & ~bit;
        }
    }
    internal bool Goo(int bit)
    {
        return this[bit];
    }
}

class SectionInformation
{
    SafeBitVector _flags;
    internal bool Goo(int x)
    {
        return _flags[x];
    }
}

class SectionInformation2
{
    SafeBitVector _flags;
    internal bool Goo(int x)
    {
        return _flags.Goo(x);
    }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(545710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545710")]
        public void StructFieldWithAssignedPropertyMembers()
        {
            string program = @"
public struct PointF
{
    private float x; private float y;
    public PointF(float x, float y) { this.x = x; this.y = y; }

    public float X { get { return x; } set { x = value; } }
    public float Y { get { return y; } set { y = value; } }
    public void M() {}
}
class GraphicsContext
{
    private PointF transformOffset;
    public GraphicsContext()
    {
        this.transformOffset.X = 0; this.transformOffset.Y = 0;
    }
    public PointF TransformOffset { get { return this.transformOffset; } }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(874526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/874526")]
        public void GenericStructWithPropertyUsingStruct()
        {
            var source =
@"struct S<T>
{
    S<T[]>? P { get; set; }
}";
            CreateCompilation(source, targetFramework: TargetFramework.Mscorlib461).VerifyDiagnostics(
                // (3,13): error CS0523: Struct member 'S<T>.P' of type 'S<T[]>?' causes a cycle in the struct layout
                //     S<T[]>? P { get; set; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S<T>.P", "S<T[]>?").WithLocation(3, 13));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void InstanceMemberExplosion_01()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    A<A<T>> x;
}

struct B<T>
{
    A<B<T>> x;
}

struct C<T>
{
    D<T> x;
}
struct D<T>
{
    C<D<T>> x;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,13): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 13),
                // (14,10): error CS0523: Struct member 'C<T>.x' of type 'D<T>' causes a cycle in the struct layout
                //     D<T> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("C<T>.x", "D<T>").WithLocation(14, 10),
                // (18,13): error CS0523: Struct member 'D<T>.x' of type 'C<D<T>>' causes a cycle in the struct layout
                //     C<D<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("D<T>.x", "C<D<T>>").WithLocation(18, 13)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void InstanceMemberExplosion_02()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    A<A<T>> x;
}

struct B<T>
{
    A<C<B<T>>> x;
}

struct C<T>
{
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,13): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 13)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void InstanceMemberExplosion_03()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct E
{}

struct X<T>
{
    T _t;
}

struct Y
{
    X<Z> xz;
}

struct Z
{
    X<E> xe;
    X<Y> xy;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (12,10): error CS0523: Struct member 'Y.xz' of type 'X<Z>' causes a cycle in the struct layout
                //     X<Z> xz;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "xz").WithArguments("Y.xz", "X<Z>").WithLocation(12, 10),
                // (18,10): error CS0523: Struct member 'Z.xy' of type 'X<Y>' causes a cycle in the struct layout
                //     X<Y> xy;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "xy").WithArguments("Z.xy", "X<Y>").WithLocation(18, 10)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void InstanceMemberExplosion_04()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    A<A<T>> x;
}

struct C<T>
{
    C<C<T>> x;
}

struct B<T>
{
    A<B<T>> x;
    C<B<T>> y;
    B<T> z;
}

struct D
{
    B<int> x;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,13): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 13),
                // (9,13): error CS0523: Struct member 'C<T>.x' of type 'C<C<T>>' causes a cycle in the struct layout
                //     C<C<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("C<T>.x", "C<C<T>>").WithLocation(9, 13),
                // (16,10): error CS0523: Struct member 'B<T>.z' of type 'B<T>' causes a cycle in the struct layout
                //     B<T> z;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "z").WithArguments("B<T>.z", "B<T>").WithLocation(16, 10)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void InstanceMemberExplosion_05()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    A<A<T>> x;
}

struct C<T>
{
    C<C<T>> x;
}

struct B<T>
{
    B<T> z;
    A<B<T>> x;
    C<B<T>> y;
}

struct D
{
    B<int> x;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,13): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 13),
                // (9,13): error CS0523: Struct member 'C<T>.x' of type 'C<C<T>>' causes a cycle in the struct layout
                //     C<C<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("C<T>.x", "C<C<T>>").WithLocation(9, 13),
                // (14,10): error CS0523: Struct member 'B<T>.z' of type 'B<T>' causes a cycle in the struct layout
                //     B<T> z;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "z").WithArguments("B<T>.z", "B<T>").WithLocation(14, 10)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void InstanceMemberExplosion_06()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    A<A<T>> x;
}

struct C<T>
{
    C<C<T>> x;
}

struct B<T>
{
    A<B<T>> x;
    B<T> z;
    C<B<T>> y;
}

struct D
{
    B<int> x;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,13): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 13),
                // (9,13): error CS0523: Struct member 'C<T>.x' of type 'C<C<T>>' causes a cycle in the struct layout
                //     C<C<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("C<T>.x", "C<C<T>>").WithLocation(9, 13),
                // (15,10): error CS0523: Struct member 'B<T>.z' of type 'B<T>' causes a cycle in the struct layout
                //     B<T> z;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "z").WithArguments("B<T>.z", "B<T>").WithLocation(15, 10)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void StaticMemberExplosion_01()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    static A<A<T>> x;
}

struct B<T>
{
    static A<B<T>> x;
}

struct C<T>
{
    static D<T> x;
}
struct D<T>
{
    static C<D<T>> x;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,20): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     static A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 20),
                // (14,17): error CS0523: Struct member 'C<T>.x' of type 'D<T>' causes a cycle in the struct layout
                //     static D<T> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("C<T>.x", "D<T>").WithLocation(14, 17),
                // (18,20): error CS0523: Struct member 'D<T>.x' of type 'C<D<T>>' causes a cycle in the struct layout
                //     static C<D<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("D<T>.x", "C<D<T>>").WithLocation(18, 20)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        public void StaticMemberExplosion_02()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct A<T>
{
    static A<A<T>> x;
}

struct B<T>
{
    static A<C<B<T>>> x;
}

struct C<T>
{
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,20): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     static A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 20)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/66844")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/75701")]
        public void StaticMemberExplosion_03()
        {
            string program = @"#pragma warning disable CS0169 // The field is never used
struct E
{}

struct X<T>
{
    static T _t;
}

struct Y
{
    static X<Z> xz;
}

struct Z
{
    static X<E> xe;
    static X<Y> xy;
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // Errors are expected here, see https://github.com/dotnet/roslyn/issues/75701.
                );
        }

        [Fact, WorkItem(1017887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1017887")]
        public void EmptyStructsFromMetadata()
        {
            var comp1 = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}");
            var sourceReference = new CSharpCompilationReference(comp1);
            var metadataReference = MetadataReference.CreateFromStream(comp1.EmitToStream());

            var source2 =
@"class Program
{
    public static void Main()
    {
        StructWithReference r1;
        var r2 = r1;

        StructWithValue v1;
        var v2 = v1;
    }
}";
            CreateCompilation(source2,
                options: TestOptions.ReleaseDll.WithWarningLevel(5),
                references: new MetadataReference[] { sourceReference }).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18),
                // (9,18): error CS0165: Use of unassigned local variable 'v1'
                //         var v2 = v1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "v1").WithArguments("v1").WithLocation(9, 18)
                );
            CreateCompilation(source2,
                options: TestOptions.ReleaseDll.WithWarningLevel(5),
                references: new MetadataReference[] { metadataReference }).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18),
                // (9,18): error CS0165: Use of unassigned local variable 'v1'
                //         var v2 = v1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "v1").WithArguments("v1").WithLocation(9, 18)
                );
            CreateCompilation(source2,
                options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                references: new MetadataReference[] { sourceReference }).VerifyDiagnostics(
                // (9,18): error CS0165: Use of unassigned local variable 'v1'
                //         var v2 = v1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "v1").WithArguments("v1").WithLocation(9, 18)
                );
            CreateCompilation(source2,
                options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                references: new MetadataReference[] { metadataReference }).VerifyDiagnostics(
                // (9,18): error CS0165: Use of unassigned local variable 'v1'
                //         var v2 = v1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "v1").WithArguments("v1").WithLocation(9, 18)
                );
        }

        [Fact, WorkItem(1072447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072447")]
        public void DoNotIgnorePrivateStructFieldsOfTypeParameterTypeFromMetadata()
        {
            var comp1 = CreateCompilation(
@"public struct GenericStruct<T> where T : class
{
    T PrivateData;
}
");
            var sourceReference = new CSharpCompilationReference(comp1);
            var metadataReference = MetadataReference.CreateFromStream(comp1.EmitToStream());

            var source2 =
@"class Program<T> where T : class
{
    public static void Main()
    {
        GenericStruct<T> r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }

        [Fact, WorkItem(1072447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072447")]
        public void IgnoreInternalStructFieldsOfReferenceTypeFromMetadata()
        {
            var comp1 = CreateCompilation(
@"public struct Struct
{
    internal string data;
}
");
            var sourceReference = new CSharpCompilationReference(comp1);
            var metadataReference = MetadataReference.CreateFromStream(comp1.EmitToStream());

            var source2 =
@"class Program
{
    public static void Main()
    {
        Struct r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }

        [Fact, WorkItem(1072447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072447")]
        public void IgnoreEffectivelyInternalStructFieldsOfReferenceTypeFromMetadata()
        {
            var comp1 = CreateCompilation(
@"
internal class C1
{
    public struct S
    {
        public string data;
    }
}
public struct Struct
{
    internal C1.S data;
}
");
            var sourceReference = new CSharpCompilationReference(comp1);
            var metadataReference = MetadataReference.CreateFromStream(comp1.EmitToStream());

            var source2 =
@"class Program
{
    public static void Main()
    {
        Struct r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }

        [Fact, WorkItem(1072447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072447")]
        public void IgnoreEffectivelyInternalStructFieldsOfReferenceTypeFromAddedModule()
        {
            var source = @"
internal class C1
{
    public struct S
    {
        public string data;
    }
}
public struct Struct
{
    internal C1.S data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"class Program
{
    public static void Main()
    {
        Struct r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }

        [Fact]
        [WorkItem(30756, "https://github.com/dotnet/roslyn/issues/30756")]
        public void IgnoreEffectivelyInternalStructFieldsOfReferenceTypeFromAddedModule_PlusNullable()
        {
            var source = @"
internal class C1
{
    public struct S
    {
#nullable disable
        public string data;
#nullable enable
    }
}
public struct Struct
{
    internal C1.S data;
}
";
            var comp1 = CreateCompilation(source, options: WithNullableEnable(TestOptions.DebugModule));
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"class Program
{
    public static void Main()
    {
        Struct r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: WithNullableEnable()).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: WithNullableEnable().WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }

        [Fact, WorkItem(1072447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072447")]
        public void IgnorePrivateStructFieldsOfReferenceTypeFromAddedModule02()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"class Program
{
    public static void Main()
    {
        Struct r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }
    }
}
