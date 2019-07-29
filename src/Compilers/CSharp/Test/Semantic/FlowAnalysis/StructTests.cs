// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            CreateCompilation(source, targetFramework: TargetFramework.Mscorlib45).VerifyDiagnostics(
                // (3,13): error CS0523: Struct member 'S<T>.P' of type 'S<T[]>?' causes a cycle in the struct layout
                //     S<T[]>? P { get; set; }
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "P").WithArguments("S<T>.P", "S<T[]>?").WithLocation(3, 13));
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
                options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { sourceReference },
                parseOptions: TestOptions.Regular.WithStrictFeature()).VerifyDiagnostics(
                // (6,18): error CS0165: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18),
                // (9,18): error CS0165: Use of unassigned local variable 'v1'
                //         var v2 = v1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "v1").WithArguments("v1").WithLocation(9, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }).VerifyDiagnostics(
                // NOTE: no errors expected because we treat all imported data the same as if imported from metadata.
                ////// (6,18): error CS0165: Use of unassigned local variable 'r1'
                //////         var r2 = r1;
                ////Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18),
                // (9,18): error CS0165: Use of unassigned local variable 'v1'
                //         var v2 = v1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "v1").WithArguments("v1").WithLocation(9, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }).VerifyDiagnostics(
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
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }).VerifyDiagnostics(
                // NOTE: no errors expected because we treat all imported data the same as if imported from metadata.
                ////// (6,18): error CS0165: Use of unassigned local variable 'r1'
                //////         var r2 = r1;
                ////Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }).VerifyDiagnostics(
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
            CreateCompilation(source2, references: new MetadataReference[] { sourceReference }).VerifyDiagnostics(
                // NOTE: no errors expected because we treat all imported data the same as if imported from metadata.
                ////// (6,18): error CS0165: Use of unassigned local variable 'r1'
                //////         var r2 = r1;
                ////Diagnostic(ErrorCode.ERR_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
            CreateCompilation(source2, references: new MetadataReference[] { metadataReference }).VerifyDiagnostics(
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
            var comp1 = CreateCompilation(source, options: WithNonNullTypesTrue(TestOptions.DebugModule));
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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: WithNonNullTypesTrue()).VerifyDiagnostics(
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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
        }
    }
}
