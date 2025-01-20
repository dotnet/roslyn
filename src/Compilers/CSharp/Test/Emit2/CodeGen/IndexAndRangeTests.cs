// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class IndexAndRangeTests : CSharpTestBase
    {
        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index(bool useCsharp13)
        {
            string source = """
M(new Buffer10());
M2();
M3();

class C
{
    public Buffer10 F = new Buffer10();
}

partial class Program
{
    static void M(Buffer10 b)
    {
        b[Id(^1)] = 1;
    }

    static Buffer10 M2() { return new Buffer10() { [Id(^1)] = Id(2) }; }
    static C M3() => new C() { F = {[Id(^1)] = Id(3)} };

    static int Id(int i) { System.Console.Write($"{i} "); return i; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int this[int x]
    {
        get => throw null;
        set { System.Console.Write($"Index={x} Value={value}, "); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) Length Index=9 Value=1, Index(^1) Length 2 Index=9 Value=2, Index(^1) Length 3 Index=9 Value=3,");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M2", """
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (Buffer10 V_0,
                int V_1,
                System.Index V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       "Buffer10..ctor()"
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     "System.Index..ctor(int, bool)"
  IL_000e:  call       "System.Index Program.Id(System.Index)"
  IL_0013:  stloc.2
  IL_0014:  ldloca.s   V_2
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       "int Buffer10.Length.get"
  IL_001d:  call       "int System.Index.GetOffset(int)"
  IL_0022:  stloc.1
  IL_0023:  ldloca.s   V_0
  IL_0025:  ldloc.1
  IL_0026:  ldc.i4.2
  IL_0027:  call       "int Program.Id(int)"
  IL_002c:  call       "void Buffer10.this[int].set"
  IL_0031:  ldloc.0
  IL_0032:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Skip(2).First();
            Assert.Equal("new Buffer10() { [Id(^1)] = Id(2) }", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree: """
IObjectCreationOperation (Constructor: Buffer10..ctor()) (OperationKind.ObjectCreation, Type: Buffer10) (Syntax: 'new Buffer1 ... ] = Id(2) }')
Arguments(0)
Initializer:
  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Buffer10) (Syntax: '{ [Id(^1)] = Id(2) }')
    Initializers(1):
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(^1)] = Id(2)')
          Left:
            IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '[Id(^1)]')
              Instance:
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Buffer10, IsImplicit) (Syntax: 'Buffer10')
              Argument:
                IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                        IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                          Operand:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              LengthSymbol: System.Int32 Buffer10.Length { get; }
              IndexerSymbol: System.Int32 Buffer10.this[System.Int32 x] { get; set; }
          Right:
            IInvocationOperation (System.Int32 Program.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(2)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Buffer1 ... ] = Id(2) }')
              Value:
                IObjectCreationOperation (Constructor: Buffer10..ctor()) (OperationKind.ObjectCreation, Type: Buffer10) (Syntax: 'new Buffer1 ... ] = Id(2) }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(^1)')
                  Value:
                    IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                            IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(^1)] = Id(2)')
                  Left:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '[Id(^1)]')
                      Instance:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... ] = Id(2) }')
                      Argument:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'Id(^1)')
                      LengthSymbol: System.Int32 Buffer10.Length { get; }
                      IndexerSymbol: System.Int32 Buffer10.this[System.Int32 x] { get; set; }
                  Right:
                    IInvocationOperation (System.Int32 Program.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(2)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Next (Return) Block[B4]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... ] = Id(2) }')
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
""",
                graph, symbol);

            comp = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (17,52): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //     static Buffer10 M2() { return new Buffer10() { [Id(^1)] = Id(2) }; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[Id(^1)]").WithArguments("implicit indexer initializer", "13.0").WithLocation(17, 52),
                // (18,37): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //     static C M3() => new C() { F = {[Id(^1)] = Id(3)} };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[Id(^1)]").WithArguments("implicit indexer initializer", "13.0").WithLocation(18, 37)
                );
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_EmptyInitializer(bool useCsharp13)
        {
            string source = """
M();

partial class Program
{
    static Buffer10 M() { return new Buffer10() { [Id(^1)] = { } }; }

    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null;
    public object this[int x] => throw null;
}
""";
            var comp = CreateCompilationWithIndex(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics();

            comp = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (5,51): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //     static Buffer10 M() { return new Buffer10() { [Id(^1)] = { } }; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[Id(^1)]").WithArguments("implicit indexer initializer", "13.0").WithLocation(5, 51)
                );
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_NestedCollectionInitializer(bool useCsharp13)
        {
            string source = """
using System.Collections;
using System.Collections.Generic;

M();

partial class Program
{
    static Buffer10 M() { return new Buffer10() { [Id(^1)] = { Id(1), Id(2) } }; }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

class C : System.Collections.Generic.IEnumerable<int>
{
    public void Add(int i) { }
    IEnumerator<int> IEnumerable<int>.GetEnumerator() { yield return 0; }
    IEnumerator IEnumerable.GetEnumerator() { yield return 0; }
}

struct Buffer10
{
    public int Length => 10;
    public C this[int x] => new C();
}
""";
            var comp = CreateCompilationWithIndex(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) Id(1) Id(2)");
            verifier.VerifyDiagnostics();

            comp = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (8,51): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //     static Buffer10 M() { return new Buffer10() { [Id(^1)] = { Id(1), Id(2) } }; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[Id(^1)]").WithArguments("implicit indexer initializer", "13.0").WithLocation(8, 51)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_FieldInitializerWithEmptyInitializer()
        {
            string source = """
M();

partial class Program
{
    static Buffer10 M() { return new Buffer10() { [Id(^1)] = { F = { } } }; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null;
    public C this[int x] => throw null;
}

class C
{
    public object F = 0;
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M", """
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  newobj     "Buffer10..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  newobj     "System.Index..ctor(int, bool)"
  IL_000c:  call       "System.Index Program.Id(System.Index)"
  IL_0011:  pop
  IL_0012:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            Assert.Equal("new Buffer10() { [Id(^1)] = { F = { } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Buffer1 ... F = { } } }')
              Value:
                IObjectCreationOperation (Constructor: Buffer10..ctor()) (OperationKind.ObjectCreation, Type: Buffer10) (Syntax: 'new Buffer1 ... F = { } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                    IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... F = { } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_FieldLikeEventInitializerWithEmptyInitializer()
        {
            string source = """
M();

partial class Program
{
    public event System.Action F;
    static Buffer10 M() { return new Buffer10() { [Id(^1)] = { F = { } } }; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null;
    public Program this[int x] => throw null;
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics(
                // (5,32): warning CS0067: The event 'Program.F' is never used
                //     public event System.Action F;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "F").WithArguments("Program.F").WithLocation(5, 32)
                );

            verifier.VerifyIL("Program.M", """
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  newobj     "Buffer10..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  newobj     "System.Index..ctor(int, bool)"
  IL_000c:  call       "System.Index Program.Id(System.Index)"
  IL_0011:  pop
  IL_0012:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            Assert.Equal("new Buffer10() { [Id(^1)] = { F = { } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Buffer1 ... F = { } } }')
              Value:
                IObjectCreationOperation (Constructor: Buffer10..ctor()) (OperationKind.ObjectCreation, Type: Buffer10) (Syntax: 'new Buffer1 ... F = { } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                    IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... F = { } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_PropertyInitializerWithEmptyInitializer()
        {
            string source = """
M();

partial class Program
{
    static Buffer10 M() { return new Buffer10() { [Id(^1)] = { F = { } } }; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null;
    public C this[int x] => throw null;
}

class C
{
    public object F { get { throw null; } }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M", """
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  newobj     "Buffer10..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  newobj     "System.Index..ctor(int, bool)"
  IL_000c:  call       "System.Index Program.Id(System.Index)"
  IL_0011:  pop
  IL_0012:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            Assert.Equal("new Buffer10() { [Id(^1)] = { F = { } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Buffer1 ... F = { } } }')
              Value:
                IObjectCreationOperation (Constructor: Buffer10..ctor()) (OperationKind.ObjectCreation, Type: Buffer10) (Syntax: 'new Buffer1 ... F = { } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                    IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... F = { } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Array()
        {
            string source = """
class C
{
    public static void Main()
    {
        var c = M();
        System.Console.Write($"Result={c.F[^1]},{c.F[^2]}");
    }

    public static C M() => new C() { F = {[Id(^1)] = Id(42), [Id(^2)] = Id(43)} };

    public int[] F = new int[10];

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}
""";
            var comp = CreateCompilationWithIndex(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) Id(42) Index(^2) Id(43) Result=42,43");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       96 (0x60)
  .maxstack  3
  .locals init (C V_0,
                int V_1,
                int V_2,
                System.Index V_3)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  ldc.i4.1
  IL_0008:  newobj     "System.Index..ctor(int, bool)"
  IL_000d:  call       "System.Index C.Id(System.Index)"
  IL_0012:  stloc.3
  IL_0013:  ldloca.s   V_3
  IL_0015:  ldloc.0
  IL_0016:  ldfld      "int[] C.F"
  IL_001b:  ldlen
  IL_001c:  conv.i4
  IL_001d:  call       "int System.Index.GetOffset(int)"
  IL_0022:  stloc.1
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "int[] C.F"
  IL_0029:  ldloc.1
  IL_002a:  ldc.i4.s   42
  IL_002c:  call       "int C.Id(int)"
  IL_0031:  stelem.i4
  IL_0032:  ldc.i4.2
  IL_0033:  ldc.i4.1
  IL_0034:  newobj     "System.Index..ctor(int, bool)"
  IL_0039:  call       "System.Index C.Id(System.Index)"
  IL_003e:  stloc.3
  IL_003f:  ldloca.s   V_3
  IL_0041:  ldloc.0
  IL_0042:  ldfld      "int[] C.F"
  IL_0047:  ldlen
  IL_0048:  conv.i4
  IL_0049:  call       "int System.Index.GetOffset(int)"
  IL_004e:  stloc.2
  IL_004f:  ldloc.0
  IL_0050:  ldfld      "int[] C.F"
  IL_0055:  ldloc.2
  IL_0056:  ldc.i4.s   43
  IL_0058:  call       "int C.Id(int)"
  IL_005d:  stelem.i4
  IL_005e:  ldloc.0
  IL_005f:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            Assert.Equal("new C() { F = {[Id(^1)] = Id(42), [Id(^2)] = Id(43)} }", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree: """
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = Id(43)} }')
Arguments(0)
Initializer:
  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ F = {[Id( ... = Id(43)} }')
    Initializers(1):
        IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[]) (Syntax: 'F = {[Id(^1 ... ] = Id(43)}')
          InitializedMember:
            IFieldReferenceOperation: System.Int32[] C.F (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'F')
              Instance Receiver:
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F')
          Initializer:
            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[]) (Syntax: '{[Id(^1)] = ... ] = Id(43)}')
              Initializers(2):
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(^1)] = Id(42)')
                    Left:
                      IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(^1)]')
                        Array reference:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: 'F')
                        Indices(1):
                            IInvocationOperation (System.Index C.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                                    IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                                      Operand:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Right:
                      IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(42)')
                        Instance Receiver:
                          null
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(^2)] = Id(43)')
                    Left:
                      IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(^2)]')
                        Array reference:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: 'F')
                        Indices(1):
                            IInvocationOperation (System.Index C.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^2)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^2')
                                    IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^2')
                                      Operand:
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Right:
                      IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(43)')
                        Instance Receiver:
                          null
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '43')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... = Id(43)} }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = Id(43)} }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(^1)')
                  Value:
                    IInvocationOperation (System.Index C.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^1)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^1')
                            IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^1')
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(^1)] = Id(42)')
                  Left:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(^1)]')
                      Array reference:
                        IFieldReferenceOperation: System.Int32[] C.F (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'F')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = Id(43)} }')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'Id(^1)')
                  Right:
                    IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(42)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(^2)')
                  Value:
                    IInvocationOperation (System.Index C.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(^2)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '^2')
                            IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^2')
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(^2)] = Id(43)')
                  Left:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(^2)]')
                      Array reference:
                        IFieldReferenceOperation: System.Int32[] C.F (OperationKind.FieldReference, Type: System.Int32[]) (Syntax: 'F')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = Id(43)} }')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'Id(^2)')
                  Right:
                    IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(43)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '43')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B4]
                Leaving: {R3}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (0)
        Next (Return) Block[B5]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = Id(43)} }')
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
""", graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_Array()
        {
            string source = """
class C
{
    public static void Main()
    {
        var c = M();
        System.Console.Write($"Result={c.F[1]},{c.F[2]}");
    }

    public static C M() => new C() { F = {[Id(1)] = Id(42), [Id(2)] = Id(43)} };

    public int[] F = new int[10];
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilationWithIndex(source, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(1) Id(42) Id(2) Id(43) Result=42,43");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (int V_0,
                int V_1)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  call       "int C.Id(int)"
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldfld      "int[] C.F"
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.s   42
  IL_0015:  call       "int C.Id(int)"
  IL_001a:  stelem.i4
  IL_001b:  ldc.i4.2
  IL_001c:  call       "int C.Id(int)"
  IL_0021:  stloc.1
  IL_0022:  dup
  IL_0023:  ldfld      "int[] C.F"
  IL_0028:  ldloc.1
  IL_0029:  ldc.i4.s   43
  IL_002b:  call       "int C.Id(int)"
  IL_0030:  stelem.i4
  IL_0031:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Array_GetOffset()
        {
            string source = """
class C
{
    public static void M(System.Index index)
    {
        _ = new C() { F = {[index] = 42} };
    }

    public int[] F = new int[10];
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (C V_0,
                int V_1)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldarga.s   V_0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "int[] C.F"
  IL_000e:  ldlen
  IL_000f:  conv.i4
  IL_0010:  call       "int System.Index.GetOffset(int)"
  IL_0015:  stloc.1
  IL_0016:  ldloc.0
  IL_0017:  ldfld      "int[] C.F"
  IL_001c:  ldloc.1
  IL_001d:  ldc.i4.s   42
  IL_001f:  stelem.i4
  IL_0020:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Array_GetOffset_WithNesting()
        {
            string source = """
var x = C.M(^1, ^2, ^3);
System.Console.Write($"Result={x.F[^1][^2]},{x.F[^1][^3]}");

class C
{
    public static C M(System.Index index, System.Index index2, System.Index index3)
    {
        return new C() { F = {[Id(index)] = { [Id(index2)] = Id(42), [Id(index3)] = Id(43) } } };
    }

    public int[][] F = new int[2][] { new int[4], new int[4] };

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) Index(^2) Id(42) Index(^3) Id(43) Result=42,43");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size      118 (0x76)
  .maxstack  3
  .locals init (C V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Index V_4)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  call       "System.Index C.Id(System.Index)"
  IL_000c:  stloc.s    V_4
  IL_000e:  ldloca.s   V_4
  IL_0010:  ldloc.0
  IL_0011:  ldfld      "int[][] C.F"
  IL_0016:  ldlen
  IL_0017:  conv.i4
  IL_0018:  call       "int System.Index.GetOffset(int)"
  IL_001d:  stloc.1
  IL_001e:  ldarg.1
  IL_001f:  call       "System.Index C.Id(System.Index)"
  IL_0024:  stloc.s    V_4
  IL_0026:  ldloca.s   V_4
  IL_0028:  ldloc.0
  IL_0029:  ldfld      "int[][] C.F"
  IL_002e:  ldloc.1
  IL_002f:  ldelem.ref
  IL_0030:  ldlen
  IL_0031:  conv.i4
  IL_0032:  call       "int System.Index.GetOffset(int)"
  IL_0037:  stloc.2
  IL_0038:  ldloc.0
  IL_0039:  ldfld      "int[][] C.F"
  IL_003e:  ldloc.1
  IL_003f:  ldelem.ref
  IL_0040:  ldloc.2
  IL_0041:  ldc.i4.s   42
  IL_0043:  call       "int C.Id(int)"
  IL_0048:  stelem.i4
  IL_0049:  ldarg.2
  IL_004a:  call       "System.Index C.Id(System.Index)"
  IL_004f:  stloc.s    V_4
  IL_0051:  ldloca.s   V_4
  IL_0053:  ldloc.0
  IL_0054:  ldfld      "int[][] C.F"
  IL_0059:  ldloc.1
  IL_005a:  ldelem.ref
  IL_005b:  ldlen
  IL_005c:  conv.i4
  IL_005d:  call       "int System.Index.GetOffset(int)"
  IL_0062:  stloc.3
  IL_0063:  ldloc.0
  IL_0064:  ldfld      "int[][] C.F"
  IL_0069:  ldloc.1
  IL_006a:  ldelem.ref
  IL_006b:  ldloc.3
  IL_006c:  ldc.i4.s   43
  IL_006e:  call       "int C.Id(int)"
  IL_0073:  stelem.i4
  IL_0074:  ldloc.0
  IL_0075:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Twice()
        {
            string source = """
M(^1, ^2);

partial class Program
{
    static Buffer10 M(System.Index i1, System.Index i2) => new Buffer10() { [Id(i1)] = Id(1), [Id(i2)] = Id(2) };

    static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int this[int x]
    {
        get => throw null;
        set { System.Console.Write($"Index={x} Value={value} "); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) Length Id(1) Index=9 Value=1 Index(^2) Length Id(2) Index=8 Value=2");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M", """
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (Buffer10 V_0,
                int V_1,
                int V_2,
                System.Index V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       "Buffer10..ctor()"
  IL_0007:  ldarg.0
  IL_0008:  call       "System.Index Program.Id(System.Index)"
  IL_000d:  stloc.3
  IL_000e:  ldloca.s   V_3
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "int Buffer10.Length.get"
  IL_0017:  call       "int System.Index.GetOffset(int)"
  IL_001c:  stloc.1
  IL_001d:  ldloca.s   V_0
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  call       "int Program.Id(int)"
  IL_0026:  call       "void Buffer10.this[int].set"
  IL_002b:  ldarg.1
  IL_002c:  call       "System.Index Program.Id(System.Index)"
  IL_0031:  stloc.3
  IL_0032:  ldloca.s   V_3
  IL_0034:  ldloca.s   V_0
  IL_0036:  call       "int Buffer10.Length.get"
  IL_003b:  call       "int System.Index.GetOffset(int)"
  IL_0040:  stloc.2
  IL_0041:  ldloca.s   V_0
  IL_0043:  ldloc.2
  IL_0044:  ldc.i4.2
  IL_0045:  call       "int Program.Id(int)"
  IL_004a:  call       "void Buffer10.this[int].set"
  IL_004f:  ldloc.0
  IL_0050:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_OptionalParameter()
        {
            string source = """
Buffer10 b = default;
b[^1] = 42; // 1

_ = new Buffer10() { [^1] = 42 }; // 2

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null;
    public int this[int x, int y = 0]
    {
        get => throw null;
        set => throw null;
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (2,3): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                // b[^1] = 42; // 1
                Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(2, 3),
                // (4,23): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                // _ = new Buffer10() { [^1] = 42 }; // 2
                Diagnostic(ErrorCode.ERR_BadArgType, "^1").WithArguments("1", "System.Index", "int").WithLocation(4, 23)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Unassigned()
        {
            string source = """
class Program
{
    static Buffer10 M()
    {
        int i;
        return new Buffer10() { [^1] = i }; // 1
    }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null;
    public int this[int x]
    {
        get => throw null;
        set => throw null;
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (6,40): error CS0165: Use of unassigned local variable 'i'
                //         return new Buffer10() { [^1] = i }; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(6, 40)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Nullability()
        {
            string source = """
#nullable enable
string? s = null;
_ = new Buffer10() { [^1] = s }; // 1

struct Buffer10
{
    public Buffer10() { }

    public int Length => throw null!;
    public string this[int x]
    {
        get => throw null!;
        set => throw null!;
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (3,29): warning CS8601: Possible null reference assignment.
                // _ = new Buffer10() { [^1] = s }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "s").WithLocation(3, 29)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Readonly()
        {
            string source = """
class C
{
    public Buffer10 F = new Buffer10();
}

class Program
{
    static void M(Buffer10 b)
    {
        b[^1] = 123;
    }

    static Buffer10 M2() => new Buffer10() { [^1] = 111 };
    static C M3() => new C() { F = {[^1] = 111} };
}

struct Buffer10
{
    public int[] _array = new int[10];
    public Buffer10() { }

    public int Length => 10;
    public int this[int x]
    {
        get => _array[x];
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (10,9): error CS0200: Property or indexer 'Buffer10.this[int]' cannot be assigned to -- it is read only
                //         b[^1] = 123;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "b[^1]").WithArguments("Buffer10.this[int]").WithLocation(10, 9),
                // (13,46): error CS0200: Property or indexer 'Buffer10.this[int]' cannot be assigned to -- it is read only
                //     static Buffer10 M2() => new Buffer10() { [^1] = 111 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "[^1]").WithArguments("Buffer10.this[int]").WithLocation(13, 46),
                // (14,37): error CS0200: Property or indexer 'Buffer10.this[int]' cannot be assigned to -- it is read only
                //     static C M3() => new C() { F = {[^1] = 111} };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "[^1]").WithArguments("Buffer10.this[int]").WithLocation(14, 37)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_RefReturningIndexer()
        {
            string source = """
var b = new Buffer10() { [^1] = 42 };
System.Console.WriteLine($"Result={b[^1]}");

class Buffer10
{
    public Buffer10() { }

    public int field = 0;
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public ref int this[int x]
    {
        get { System.Console.Write($"Index={x} "); return ref field; }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Length Index=9 Length Index=9 Result=42");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_RefReturningIndexer_WithNesting()
        {
            string source = """
var m = M();
System.Console.Write("Result: ");
System.Console.Write(m[Id(^1)][Id(^2)]);

partial class Program
{
    public static Container M()
    {
        return new Container() { [Id(^1)] = { [Id(^2)] = Id(42) } };
    }

    static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

class Container
{
    public Buffer10 field = new Buffer10();
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public ref Buffer10 this[int x]
    {
        get { System.Console.Write($"ContainerIndex={x} "); return ref field; }
    }
}

class Buffer10
{
    public Buffer10() { }

    public int field = 0;
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public ref int this[int x]
    {
        get { System.Console.Write($"Index={x} "); return ref field; }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) ContainerLength Index(^2) ContainerIndex=9 Length ContainerIndex=9 Index=8 Id(42)" +
                " Result: Index(^1) ContainerLength ContainerIndex=9 Index(^2) Length Index=8 42");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M", """
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Container V_0,
                int V_1,
                int V_2,
                System.Index V_3)
  IL_0000:  newobj     "Container..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  ldc.i4.1
  IL_0008:  newobj     "System.Index..ctor(int, bool)"
  IL_000d:  call       "System.Index Program.Id(System.Index)"
  IL_0012:  stloc.3
  IL_0013:  ldloca.s   V_3
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "int Container.Length.get"
  IL_001b:  call       "int System.Index.GetOffset(int)"
  IL_0020:  stloc.1
  IL_0021:  ldc.i4.2
  IL_0022:  ldc.i4.1
  IL_0023:  newobj     "System.Index..ctor(int, bool)"
  IL_0028:  call       "System.Index Program.Id(System.Index)"
  IL_002d:  stloc.3
  IL_002e:  ldloca.s   V_3
  IL_0030:  ldloc.0
  IL_0031:  ldloc.1
  IL_0032:  callvirt   "ref Buffer10 Container.this[int].get"
  IL_0037:  ldind.ref
  IL_0038:  callvirt   "int Buffer10.Length.get"
  IL_003d:  call       "int System.Index.GetOffset(int)"
  IL_0042:  stloc.2
  IL_0043:  ldloc.0
  IL_0044:  ldloc.1
  IL_0045:  callvirt   "ref Buffer10 Container.this[int].get"
  IL_004a:  ldind.ref
  IL_004b:  ldloc.2
  IL_004c:  callvirt   "ref int Buffer10.this[int].get"
  IL_0051:  ldc.i4.s   42
  IL_0053:  call       "int Program.Id(int)"
  IL_0058:  stind.i4
  IL_0059:  ldloc.0
  IL_005a:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_RefReturningIndexer_WithEmptyNesting()
        {
            string source = """
M();

partial class Program
{
    public static Buffer10 M()
    {
        return new Buffer10() { [Id(^1)] = { } };
    }

    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

class Buffer10
{
    public int field = 0;
    public int Length => throw null;
    public ref object this[int x] => throw null;
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M", """
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  newobj     "Buffer10..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  newobj     "System.Index..ctor(int, bool)"
  IL_000c:  call       "System.Index Program.Id(System.Index)"
  IL_0011:  pop
  IL_0012:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_RefReadonlyReturningIndexer_WithNesting()
        {
            string source = """
var m = M();
System.Console.Write("Result: ");
System.Console.Write(m[Id(^1)][Id(^2)]);

partial class Program
{
    public static Container M()
    {
        return new Container() { [Id(^1)] = { [Id(^2)] = Id(42) } };
    }

    static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

class Container
{
    public Buffer10 field = new Buffer10();
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public ref readonly Buffer10 this[int x]
    {
        get { System.Console.Write($"ContainerIndex={x} "); return ref field; }
    }
}

class Buffer10
{
    public Buffer10() { }

    public int field = 0;
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public ref int this[int x]
    {
        get { System.Console.Write($"Index={x} "); return ref field; }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1) ContainerLength Index(^2) ContainerIndex=9 Length ContainerIndex=9 Index=8 Id(42)" +
                " Result: Index(^1) ContainerLength ContainerIndex=9 Index(^2) Length Index=8 42");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M", """
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Container V_0,
                int V_1,
                int V_2,
                System.Index V_3)
  IL_0000:  newobj     "Container..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  ldc.i4.1
  IL_0008:  newobj     "System.Index..ctor(int, bool)"
  IL_000d:  call       "System.Index Program.Id(System.Index)"
  IL_0012:  stloc.3
  IL_0013:  ldloca.s   V_3
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "int Container.Length.get"
  IL_001b:  call       "int System.Index.GetOffset(int)"
  IL_0020:  stloc.1
  IL_0021:  ldc.i4.2
  IL_0022:  ldc.i4.1
  IL_0023:  newobj     "System.Index..ctor(int, bool)"
  IL_0028:  call       "System.Index Program.Id(System.Index)"
  IL_002d:  stloc.3
  IL_002e:  ldloca.s   V_3
  IL_0030:  ldloc.0
  IL_0031:  ldloc.1
  IL_0032:  callvirt   "ref readonly Buffer10 Container.this[int].get"
  IL_0037:  ldind.ref
  IL_0038:  callvirt   "int Buffer10.Length.get"
  IL_003d:  call       "int System.Index.GetOffset(int)"
  IL_0042:  stloc.2
  IL_0043:  ldloc.0
  IL_0044:  ldloc.1
  IL_0045:  callvirt   "ref readonly Buffer10 Container.this[int].get"
  IL_004a:  ldind.ref
  IL_004b:  ldloc.2
  IL_004c:  callvirt   "ref int Buffer10.this[int].get"
  IL_0051:  ldc.i4.s   42
  IL_0053:  call       "int Program.Id(int)"
  IL_0058:  stind.i4
  IL_0059:  ldloc.0
  IL_005a:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_RefReadonlyReturningIndexer_WithEmptyNesting()
        {
            string source = """
M();

partial class Program
{
    public static Buffer10 M()
    {
        return new Buffer10() { [Id(^1)] = { } };
    }

    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

class Buffer10
{
    public int field = 0;
    public int Length => throw null;
    public ref readonly object this[int x] => throw null;
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M", """
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  newobj     "Buffer10..ctor()"
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.1
  IL_0007:  newobj     "System.Index..ctor(int, bool)"
  IL_000c:  call       "System.Index Program.Id(System.Index)"
  IL_0011:  pop
  IL_0012:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_RefReturningIndexer_ByRef()
        {
            string source = """
int i = 42;
_ = new Buffer10() { [1] = ref i }; // 1
_ = new Buffer10() { [^1] = ref i }; // 2

Buffer10 b = new Buffer10();
b[1] = ref i; // 3
b[^1] = ref i; // 4

class Buffer10
{
    public Buffer10() { }

    public int field = 0;
    public int Length => 10;
    public ref int this[int x]
    {
        get { System.Console.Write($"Index={x} "); return ref field; }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            comp.VerifyDiagnostics(
                // (2,22): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // _ = new Buffer10() { [1] = ref i }; // 1
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[1]").WithLocation(2, 22),
                // (3,22): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // _ = new Buffer10() { [^1] = ref i }; // 2
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "[^1]").WithLocation(3, 22),
                // (6,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // b[1] = ref i; // 3
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "b[1]").WithLocation(6, 1),
                // (7,1): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                // b[^1] = ref i; // 4
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "b[^1]").WithLocation(7, 1)
                );
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_WithNesting(bool useCsharp13)
        {
            string source = """
M(^1, ^2, ^3);

partial class Program
{
    public static Buffer10Container M(System.Index i1, System.Index i2, System.Index i3)
    {
        return new Buffer10Container() { [i1] = { [i2] = 42, [i3] = 43 } };
    }
}

class Buffer10Container
{
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 this[int x]
    {
        get { System.Console.Write($"ContainerIndex={x} "); return new Buffer10(); }
    }
}

class Buffer10
{
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int this[int x]
    {
        set { System.Console.Write($"Index={x} Value={value} "); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "ContainerLength ContainerIndex=9 Length ContainerIndex=9 Index=8 Value=42 ContainerIndex=9 Length ContainerIndex=9 Index=7 Value=43");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            Assert.Equal("new Buffer10Container() { [i1] = { [i2] = 42, [i3] = 43 } }", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree: """
IObjectCreationOperation (Constructor: Buffer10Container..ctor()) (OperationKind.ObjectCreation, Type: Buffer10Container) (Syntax: 'new Buffer1 ... 3] = 43 } }')
Arguments(0)
Initializer:
  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Buffer10Container) (Syntax: '{ [i1] = {  ... 3] = 43 } }')
    Initializers(1):
        IMemberInitializerOperation (OperationKind.MemberInitializer, Type: Buffer10) (Syntax: '[i1] = { [i ... [i3] = 43 }')
          InitializedMember:
            IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: Buffer10) (Syntax: '[i1]')
              Instance:
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Buffer10Container, IsImplicit) (Syntax: 'Buffer10Container')
              Argument:
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
              LengthSymbol: System.Int32 Buffer10Container.Length { get; }
              IndexerSymbol: Buffer10 Buffer10Container.this[System.Int32 x] { get; }
          Initializer:
            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Buffer10) (Syntax: '{ [i2] = 42, [i3] = 43 }')
              Initializers(2):
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[i2] = 42')
                    Left:
                      IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '[i2]')
                        Instance:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Buffer10, IsImplicit) (Syntax: '[i1]')
                        Argument:
                          IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
                        LengthSymbol: System.Int32 Buffer10.Length { get; }
                        IndexerSymbol: System.Int32 Buffer10.this[System.Int32 x] { set; }
                    Right:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[i3] = 43')
                    Left:
                      IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '[i3]')
                        Instance:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Buffer10, IsImplicit) (Syntax: '[i1]')
                        Argument:
                          IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i3')
                        LengthSymbol: System.Int32 Buffer10.Length { get; }
                        IndexerSymbol: System.Int32 Buffer10.this[System.Int32 x] { set; }
                    Right:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
""");

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Buffer1 ... 3] = 43 } }')
              Value:
                IObjectCreationOperation (Constructor: Buffer10Container..ctor()) (OperationKind.ObjectCreation, Type: Buffer10Container) (Syntax: 'new Buffer1 ... 3] = 43 } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                      Value:
                        IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[i2] = 42')
                      Left:
                        IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '[i2]')
                          Instance:
                            IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: Buffer10) (Syntax: '[i1]')
                              Instance:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10Container, IsImplicit) (Syntax: 'new Buffer1 ... 3] = 43 } }')
                              Argument:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1')
                              LengthSymbol: System.Int32 Buffer10Container.Length { get; }
                              IndexerSymbol: Buffer10 Buffer10Container.this[System.Int32 x] { get; }
                          Argument:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i2')
                          LengthSymbol: System.Int32 Buffer10.Length { get; }
                          IndexerSymbol: System.Int32 Buffer10.this[System.Int32 x] { set; }
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                Next (Regular) Block[B4]
                    Leaving: {R3}
                    Entering: {R4}
        }
        .locals {R4}
        {
            CaptureIds: [3]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (2)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                      Value:
                        IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i3')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[i3] = 43')
                      Left:
                        IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '[i3]')
                          Instance:
                            IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: Buffer10) (Syntax: '[i1]')
                              Instance:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10Container, IsImplicit) (Syntax: 'new Buffer1 ... 3] = 43 } }')
                              Argument:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1')
                              LengthSymbol: System.Int32 Buffer10Container.Length { get; }
                              IndexerSymbol: Buffer10 Buffer10Container.this[System.Int32 x] { get; }
                          Argument:
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i3')
                          LengthSymbol: System.Int32 Buffer10.Length { get; }
                          IndexerSymbol: System.Int32 Buffer10.this[System.Int32 x] { set; }
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                Next (Regular) Block[B5]
                    Leaving: {R4} {R2}
        }
    }
    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Next (Return) Block[B6]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10Container, IsImplicit) (Syntax: 'new Buffer1 ... 3] = 43 } }')
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
""", graph, symbol);

            comp = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (7,42): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Buffer10Container() { [i1] = { [i2] = 42, [i3] = 43 } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[i1]").WithArguments("implicit indexer initializer", "13.0").WithLocation(7, 42),
                // (7,51): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Buffer10Container() { [i1] = { [i2] = 42, [i3] = 43 } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[i2]").WithArguments("implicit indexer initializer", "13.0").WithLocation(7, 51),
                // (7,62): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Buffer10Container() { [i1] = { [i2] = 42, [i3] = 43 } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[i3]").WithArguments("implicit indexer initializer", "13.0").WithLocation(7, 62)
                );
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_WithEmptyNesting(bool useCsharp13)
        {
            string source = """
M(^1);

partial class Program
{
    public static Buffer10Container M(System.Index i1)
    {
        return new Buffer10Container() { [Id(i1)] = { } };
    }

    static System.Index Id(System.Index i) { System.Console.Write($"Index({i}) "); return i; }
}

class Buffer10Container
{
    public int Length => throw null;
    public Buffer10 this[int x] => throw null;
}

class Buffer10 { }
""";
            var comp = CreateCompilationWithIndex(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index(^1)");
            verifier.VerifyDiagnostics();

            comp = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (7,42): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Buffer10Container() { [Id(i1)] = { } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[Id(i1)]").WithArguments("implicit indexer initializer", "13.0").WithLocation(7, 42)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_WithEmptyNestingBetweenMeaningfulNestings()
        {
            string source = """
M(^1, ^2, ^3);

partial class Program
{
    public static Buffer10 M(System.Index i1, System.Index i2, System.Index i3)
    {
        return new Buffer10() { [Id(i1)] = { X = Id(1) }, [Id(i2)] = { }, [Id(i3)] = { X = Id(2) } };
    }

    static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    static System.Index Id(System.Index i) { System.Console.Write($"Id({i}) "); return i; }
}

public class C
{
    public int X { set { System.Console.Write($"X={value} "); } }
}

class Buffer10
{
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public C this[int x]
    {
        get { System.Console.Write($"Index={x} "); return new C(); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(^1) Length Index=9 Id(1) X=1 Id(^2) Id(^3) Length Index=7 Id(2) X=2");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            Assert.Equal("new Buffer10() { [Id(i1)] = { X = Id(1) }, [Id(i2)] = { }, [Id(i3)] = { X = Id(2) } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Buffer1 ... = Id(2) } }')
              Value:
                IObjectCreationOperation (Constructor: Buffer10..ctor()) (OperationKind.ObjectCreation, Type: Buffer10) (Syntax: 'new Buffer1 ... = Id(2) } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(i1)')
                  Value:
                    IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(i1)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1')
                            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = Id(1)')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: '[Id(i1)]')
                          Instance:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... = Id(2) } }')
                          Argument:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'Id(i1)')
                          LengthSymbol: System.Int32 Buffer10.Length { get; }
                          IndexerSymbol: C Buffer10.this[System.Int32 x] { get; }
                  Right:
                    IInvocationOperation (System.Int32 Program.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(i2)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2')
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [2]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(i3)')
                  Value:
                    IInvocationOperation (System.Index Program.Id(System.Index i)) (OperationKind.Invocation, Type: System.Index) (Syntax: 'Id(i3)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i3')
                            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = Id(2)')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: '[Id(i3)]')
                          Instance:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... = Id(2) } }')
                          Argument:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'Id(i3)')
                          LengthSymbol: System.Int32 Buffer10.Length { get; }
                          IndexerSymbol: C Buffer10.this[System.Int32 x] { get; }
                  Right:
                    IInvocationOperation (System.Int32 Program.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(2)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
                Leaving: {R3}
    }
    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Next (Return) Block[B6]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'new Buffer1 ... = Id(2) } }')
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
""", graph, symbol);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Array_WithNesting(bool useCsharp13)
        {
            string source = """
var x = M();
System.Console.Write($"{x.F[^1][^2]} {x.F[^1][^3]} {x.F[^2][^4]}");

partial class Program
{
    public static Container M()
    {
        return new Container() { F = { [^1] = { [^2] = 42, [^3] = 43 }, [^2] = { [^4] = 44 } } };
    }
}

class Container
{
    public int[][] F = new int[2][] { new int[4], new int[4] };
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
}
""";

            var comp = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (8,40): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Container() { F = { [^1] = { [^2] = 42, [^3] = 43 }, [^2] = { [^4] = 44 } } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[^1]").WithArguments("implicit indexer initializer", "13.0").WithLocation(8, 40),
                // (8,49): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Container() { F = { [^1] = { [^2] = 42, [^3] = 43 }, [^2] = { [^4] = 44 } } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[^2]").WithArguments("implicit indexer initializer", "13.0").WithLocation(8, 49),
                // (8,60): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Container() { F = { [^1] = { [^2] = 42, [^3] = 43 }, [^2] = { [^4] = 44 } } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[^3]").WithArguments("implicit indexer initializer", "13.0").WithLocation(8, 60),
                // (8,73): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Container() { F = { [^1] = { [^2] = 42, [^3] = 43 }, [^2] = { [^4] = 44 } } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[^2]").WithArguments("implicit indexer initializer", "13.0").WithLocation(8, 73),
                // (8,82): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new Container() { F = { [^1] = { [^2] = 42, [^3] = 43 }, [^2] = { [^4] = 44 } } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[^4]").WithArguments("implicit indexer initializer", "13.0").WithLocation(8, 82)
                );

            comp = CreateCompilationWithIndex(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "42 43 44");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M", """ 
{
  // Code size      105 (0x69)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4)
  IL_0000:  newobj     "Container..ctor()"
  IL_0005:  dup
  IL_0006:  ldfld      "int[][] Container.F"
  IL_000b:  ldlen
  IL_000c:  conv.i4
  IL_000d:  ldc.i4.1
  IL_000e:  sub
  IL_000f:  stloc.0
  IL_0010:  dup
  IL_0011:  ldfld      "int[][] Container.F"
  IL_0016:  ldloc.0
  IL_0017:  ldelem.ref
  IL_0018:  ldlen
  IL_0019:  conv.i4
  IL_001a:  ldc.i4.2
  IL_001b:  sub
  IL_001c:  stloc.1
  IL_001d:  dup
  IL_001e:  ldfld      "int[][] Container.F"
  IL_0023:  ldloc.0
  IL_0024:  ldelem.ref
  IL_0025:  ldloc.1
  IL_0026:  ldc.i4.s   42
  IL_0028:  stelem.i4
  IL_0029:  dup
  IL_002a:  ldfld      "int[][] Container.F"
  IL_002f:  ldloc.0
  IL_0030:  ldelem.ref
  IL_0031:  ldlen
  IL_0032:  conv.i4
  IL_0033:  ldc.i4.3
  IL_0034:  sub
  IL_0035:  stloc.2
  IL_0036:  dup
  IL_0037:  ldfld      "int[][] Container.F"
  IL_003c:  ldloc.0
  IL_003d:  ldelem.ref
  IL_003e:  ldloc.2
  IL_003f:  ldc.i4.s   43
  IL_0041:  stelem.i4
  IL_0042:  dup
  IL_0043:  ldfld      "int[][] Container.F"
  IL_0048:  ldlen
  IL_0049:  conv.i4
  IL_004a:  ldc.i4.2
  IL_004b:  sub
  IL_004c:  stloc.3
  IL_004d:  dup
  IL_004e:  ldfld      "int[][] Container.F"
  IL_0053:  ldloc.3
  IL_0054:  ldelem.ref
  IL_0055:  ldlen
  IL_0056:  conv.i4
  IL_0057:  ldc.i4.4
  IL_0058:  sub
  IL_0059:  stloc.s    V_4
  IL_005b:  dup
  IL_005c:  ldfld      "int[][] Container.F"
  IL_0061:  ldloc.3
  IL_0062:  ldelem.ref
  IL_0063:  ldloc.s    V_4
  IL_0065:  ldc.i4.s   44
  IL_0067:  stelem.i4
  IL_0068:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_WithNesting_Struct()
        {
            string source = """
_ = new Buffer10Container() { [1] = { [2] = 42 } }; // 1
_ = new Buffer10Container() { [^1] = { [^2] = 42 } }; // 2

struct Buffer10Container
{
    public Buffer10Container() { }

    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 this[int x]
    {
        get => throw null;
    }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int this[int x]
    {
        set => throw null;
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            comp.VerifyDiagnostics(
                // (1,31): error CS1918: Members of property 'Buffer10Container.this[int]' of type 'Buffer10' cannot be assigned with an object initializer because it is of a value type
                // _ = new Buffer10Container() { [1] = { [2] = 42 } }; // 1
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "[1]").WithArguments("Buffer10Container.this[int]", "Buffer10").WithLocation(1, 31),
                // (2,31): error CS1918: Members of property 'Buffer10Container.this[int]' of type 'Buffer10' cannot be assigned with an object initializer because it is of a value type
                // _ = new Buffer10Container() { [^1] = { [^2] = 42 } }; // 2
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "[^1]").WithArguments("Buffer10Container.this[int]", "Buffer10").WithLocation(2, 31)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_WithNesting_Struct_WriteOnly()
        {
            string source = """
_ = new Buffer10Container() { [1] = { [2] = 42 } }; // 1
_ = new Buffer10Container() { [^1] = { [^2] = 42 } }; // 2

struct Buffer10Container
{
    public Buffer10Container() { }

    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 this[int x]
    {
        set => throw null;
    }
}

struct Buffer10
{
    public Buffer10() { }

    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int this[int x]
    {
        set => throw null;
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            comp.VerifyDiagnostics(
                // (1,31): error CS1918: Members of property 'Buffer10Container.this[int]' of type 'Buffer10' cannot be assigned with an object initializer because it is of a value type
                // _ = new Buffer10Container() { [1] = { [2] = 42 } }; // 1
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "[1]").WithArguments("Buffer10Container.this[int]", "Buffer10").WithLocation(1, 31),
                // (2,31): error CS1918: Members of property 'Buffer10Container.this[int]' of type 'Buffer10' cannot be assigned with an object initializer because it is of a value type
                // _ = new Buffer10Container() { [^1] = { [^2] = 42 } }; // 2
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "[^1]").WithArguments("Buffer10Container.this[int]", "Buffer10").WithLocation(2, 31)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range()
        {
            string source = """
Buffer10 b = default;
b[..] = null; // 1

_ = new Buffer10() { [..] = null }; // 2
_ = new C() { F = { [..] = null } }; // 3

struct Buffer10
{
    public Buffer10() { }
    public int Length => throw null;
    public int[] Slice(int start, int length) => throw null;
}

class C
{
    public Buffer10 F = new Buffer10();
}
""";
            var comp = CreateCompilationWithIndexAndRange(source);
            comp.VerifyDiagnostics(
                // (2,1): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // b[..] = null; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "b[..]").WithLocation(2, 1),
                // (4,22): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // _ = new Buffer10() { [..] = null }; // 2
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[..]").WithLocation(4, 22),
                // (5,21): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // _ = new C() { F = { [..] = null } }; // 3
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[..]").WithLocation(5, 21)
                );
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_WithNesting(bool useCsharp13)
        {
            string source = """
var c = C.M(3..^6);
System.Console.Write($"Results={c.F._array[1]},{c.F._array[2]}");

class Buffer10
{
    public int[] _array = new int[10];
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int[] Slice(int start, int length) { System.Console.Write($"Slice({start}, {length}) "); return _array; }
}

class C
{
    public Buffer10 F = new Buffer10();
    public static C M(System.Range r)
    {
        return new C() { F = { [Id(r)] = { [Id(1)] = Id(42), [Id(2)] = Id(43) } } };
    }
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(source, parseOptions: useCsharp13 ? TestOptions.Regular13 : TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(3..^6) Length Id(1) Slice(3, 1) Id(42) Id(2) Slice(3, 1) Id(43) Results=42,43");
            verifier.VerifyIL("C.M", """
{
  // Code size      115 (0x73)
  .maxstack  5
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5,
                System.Index V_6)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  dup
  IL_0006:  ldfld      "Buffer10 C.F"
  IL_000b:  ldarg.0
  IL_000c:  call       "System.Range C.Id(System.Range)"
  IL_0011:  stloc.0
  IL_0012:  dup
  IL_0013:  callvirt   "int Buffer10.Length.get"
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "System.Index System.Range.Start.get"
  IL_0020:  stloc.s    V_6
  IL_0022:  ldloca.s   V_6
  IL_0024:  ldloc.1
  IL_0025:  call       "int System.Index.GetOffset(int)"
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "System.Index System.Range.End.get"
  IL_0032:  stloc.s    V_6
  IL_0034:  ldloca.s   V_6
  IL_0036:  ldloc.1
  IL_0037:  call       "int System.Index.GetOffset(int)"
  IL_003c:  ldloc.2
  IL_003d:  sub
  IL_003e:  stloc.3
  IL_003f:  ldc.i4.1
  IL_0040:  call       "int C.Id(int)"
  IL_0045:  stloc.s    V_4
  IL_0047:  dup
  IL_0048:  ldloc.2
  IL_0049:  ldloc.3
  IL_004a:  callvirt   "int[] Buffer10.Slice(int, int)"
  IL_004f:  ldloc.s    V_4
  IL_0051:  ldc.i4.s   42
  IL_0053:  call       "int C.Id(int)"
  IL_0058:  stelem.i4
  IL_0059:  ldc.i4.2
  IL_005a:  call       "int C.Id(int)"
  IL_005f:  stloc.s    V_5
  IL_0061:  ldloc.2
  IL_0062:  ldloc.3
  IL_0063:  callvirt   "int[] Buffer10.Slice(int, int)"
  IL_0068:  ldloc.s    V_5
  IL_006a:  ldc.i4.s   43
  IL_006c:  call       "int C.Id(int)"
  IL_0071:  stelem.i4
  IL_0072:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(r)] = { [Id(1)] = Id(42), [Id(2)] = Id(43) } } }", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree: """
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... d(43) } } }')
Arguments(0)
Initializer:
  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ F = { [Id ... d(43) } } }')
    Initializers(1):
        IMemberInitializerOperation (OperationKind.MemberInitializer, Type: Buffer10) (Syntax: 'F = { [Id(r ...  Id(43) } }')
          InitializedMember:
            IFieldReferenceOperation: Buffer10 C.F (OperationKind.FieldReference, Type: Buffer10) (Syntax: 'F')
              Instance Receiver:
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F')
          Initializer:
            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Buffer10) (Syntax: '{ [Id(r)] = ...  Id(43) } }')
              Initializers(1):
                  IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[]) (Syntax: '[Id(r)] = { ...  = Id(43) }')
                    InitializedMember:
                      IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32[]) (Syntax: '[Id(r)]')
                        Instance:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Buffer10, IsImplicit) (Syntax: 'F')
                        Argument:
                          IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r)')
                            Instance Receiver:
                              null
                            Arguments(1):
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r')
                                  IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r')
                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        LengthSymbol: System.Int32 Buffer10.Length { get; }
                        IndexerSymbol: System.Int32[] Buffer10.Slice(System.Int32 start, System.Int32 length)
                    Initializer:
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[]) (Syntax: '{ [Id(1)] = ...  = Id(43) }')
                        Initializers(2):
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(1)] = Id(42)')
                              Left:
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(1)]')
                                  Array reference:
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: '[Id(r)]')
                                  Indices(1):
                                      IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
                                        Instance Receiver:
                                          null
                                        Arguments(1):
                                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Right:
                                IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(42)')
                                  Instance Receiver:
                                    null
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(2)] = Id(43)')
                              Left:
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(2)]')
                                  Array reference:
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[], IsImplicit) (Syntax: '[Id(r)]')
                                  Indices(1):
                                      IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(2)')
                                        Instance Receiver:
                                          null
                                        Arguments(1):
                                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Right:
                                IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(43)')
                                  Instance Receiver:
                                    null
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '43')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... d(43) } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... d(43) } } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(r)')
                  Value:
                    IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r')
                            IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(1)')
                      Value:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(1)] = Id(42)')
                      Left:
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(1)]')
                          Array reference:
                            IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32[]) (Syntax: '[Id(r)]')
                              Instance:
                                IFieldReferenceOperation: Buffer10 C.F (OperationKind.FieldReference, Type: Buffer10) (Syntax: 'F')
                                  Instance Receiver:
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... d(43) } } }')
                              Argument:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'Id(r)')
                              LengthSymbol: System.Int32 Buffer10.Length { get; }
                              IndexerSymbol: System.Int32[] Buffer10.Slice(System.Int32 start, System.Int32 length)
                          Indices(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(1)')
                      Right:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(42)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B4]
                    Leaving: {R3}
                    Entering: {R4}
        }
        .locals {R4}
        {
            CaptureIds: [3]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (2)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(2)')
                      Value:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(2)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(2)] = Id(43)')
                      Left:
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(2)]')
                          Array reference:
                            IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32[]) (Syntax: '[Id(r)]')
                              Instance:
                                IFieldReferenceOperation: Buffer10 C.F (OperationKind.FieldReference, Type: Buffer10) (Syntax: 'F')
                                  Instance Receiver:
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... d(43) } } }')
                              Argument:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'Id(r)')
                              LengthSymbol: System.Int32 Buffer10.Length { get; }
                              IndexerSymbol: System.Int32[] Buffer10.Slice(System.Int32 start, System.Int32 length)
                          Indices(1):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(2)')
                      Right:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(43)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '43')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B5]
                    Leaving: {R4} {R2}
        }
    }
    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Next (Return) Block[B6]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... d(43) } } }')
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
""", graph, symbol);

            comp = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (16,32): error CS9202: Feature 'implicit indexer initializer' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         return new C() { F = { [Id(r)] = { [Id(1)] = Id(42), [Id(2)] = Id(43) } } };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "[Id(r)]").WithArguments("implicit indexer initializer", "13.0").WithLocation(16, 32)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_WithNestedNesting()
        {
            string source = """
var c = C.M(1..^1, 2..^2);
System.Console.Write($"Results={c.F._array._array[2]},{c.F._array._array[3]}");

class Container
{
    public Buffer10 _array = new Buffer10();
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 Slice(int start, int length) { System.Console.Write($"ContainerSlice({start}, {length}) "); return _array; }
}

class Buffer10
{
    public int[] _array = new int[10];
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int[] Slice(int start, int length) { System.Console.Write($"Slice({start}, {length}) "); return _array; }
}

class C
{
    public Container F = new Container();
    public static C M(System.Range r, System.Range r2)
    {
        return new C()
            {
                F =
                {
                    [Id(r)] =
                    {
                        [Id(r2)] = { [Id(2)] = Id(42), [Id(3)] = Id(43) },
                    }
                }
            };
    }
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(1..^1) ContainerLength ContainerSlice(1, 8) Range(2..^2) Length Id(2) Slice(2, 6) Id(42) Id(3) Slice(2, 6) Id(43) Results=42,43");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size      185 (0xb9)
  .maxstack  5
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Range V_4,
                int V_5,
                int V_6,
                int V_7,
                int V_8,
                int V_9,
                System.Index V_10)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  dup
  IL_0006:  ldfld      "Container C.F"
  IL_000b:  ldarg.0
  IL_000c:  call       "System.Range C.Id(System.Range)"
  IL_0011:  stloc.0
  IL_0012:  dup
  IL_0013:  callvirt   "int Container.Length.get"
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "System.Index System.Range.Start.get"
  IL_0020:  stloc.s    V_10
  IL_0022:  ldloca.s   V_10
  IL_0024:  ldloc.1
  IL_0025:  call       "int System.Index.GetOffset(int)"
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "System.Index System.Range.End.get"
  IL_0032:  stloc.s    V_10
  IL_0034:  ldloca.s   V_10
  IL_0036:  ldloc.1
  IL_0037:  call       "int System.Index.GetOffset(int)"
  IL_003c:  ldloc.2
  IL_003d:  sub
  IL_003e:  stloc.3
  IL_003f:  ldloc.2
  IL_0040:  ldloc.3
  IL_0041:  callvirt   "Buffer10 Container.Slice(int, int)"
  IL_0046:  ldarg.1
  IL_0047:  call       "System.Range C.Id(System.Range)"
  IL_004c:  stloc.s    V_4
  IL_004e:  dup
  IL_004f:  callvirt   "int Buffer10.Length.get"
  IL_0054:  stloc.s    V_5
  IL_0056:  ldloca.s   V_4
  IL_0058:  call       "System.Index System.Range.Start.get"
  IL_005d:  stloc.s    V_10
  IL_005f:  ldloca.s   V_10
  IL_0061:  ldloc.s    V_5
  IL_0063:  call       "int System.Index.GetOffset(int)"
  IL_0068:  stloc.s    V_6
  IL_006a:  ldloca.s   V_4
  IL_006c:  call       "System.Index System.Range.End.get"
  IL_0071:  stloc.s    V_10
  IL_0073:  ldloca.s   V_10
  IL_0075:  ldloc.s    V_5
  IL_0077:  call       "int System.Index.GetOffset(int)"
  IL_007c:  ldloc.s    V_6
  IL_007e:  sub
  IL_007f:  stloc.s    V_7
  IL_0081:  ldc.i4.2
  IL_0082:  call       "int C.Id(int)"
  IL_0087:  stloc.s    V_8
  IL_0089:  dup
  IL_008a:  ldloc.s    V_6
  IL_008c:  ldloc.s    V_7
  IL_008e:  callvirt   "int[] Buffer10.Slice(int, int)"
  IL_0093:  ldloc.s    V_8
  IL_0095:  ldc.i4.s   42
  IL_0097:  call       "int C.Id(int)"
  IL_009c:  stelem.i4
  IL_009d:  ldc.i4.3
  IL_009e:  call       "int C.Id(int)"
  IL_00a3:  stloc.s    V_9
  IL_00a5:  ldloc.s    V_6
  IL_00a7:  ldloc.s    V_7
  IL_00a9:  callvirt   "int[] Buffer10.Slice(int, int)"
  IL_00ae:  ldloc.s    V_9
  IL_00b0:  ldc.i4.s   43
  IL_00b2:  call       "int C.Id(int)"
  IL_00b7:  stelem.i4
  IL_00b8:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_WithEmptyNestedNesting()
        {
            string source = """
C.M(1..^1, 2..^2);

class Container
{
    public Buffer10 _array = new Buffer10();
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 Slice(int start, int length) => throw null;
}

class Buffer10
{
    public int Length => throw null;
    public int[] Slice(int start, int length) => throw null;
}

class C
{
    public Container F = new Container();
    public static C M(System.Range r, System.Range r2)
    {
        return new C() { F = { [Id(r)] = { [Id(r2)] = { } } } };
    }
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(1..^1) Range(2..^2)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "System.Range C.Id(System.Range)"
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  call       "System.Range C.Id(System.Range)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(r)] = { [Id(r2)] = { } } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = { } } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r')
                    IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r2)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r2')
                    IParameterReferenceOperation: r2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_WithMixedNestedNesting()
        {
            string source = """
C.M(1..^1, 2..^2, 3..^3, 4, 5);

class Container
{
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 Slice(int start, int length) { System.Console.Write($"ContainerSlice({start}, {length}) "); return new Buffer10(); }
}

class Buffer10
{
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public int[] Slice(int start, int length) { System.Console.Write($"Slice({start}, {length}) "); return new int[10]; }
}

class C
{
    public Container F = new Container();
    public static C M(System.Range r, System.Range r2, System.Range r3, int i4, int i5)
    {
        return new C() { F = { [Id(r)] = { [Id(r2)] = { }, [Id(r3)] = { [Id(i4)] = Id(i5) } } } };
    }
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(1..^1) ContainerLength Range(2..^2) ContainerSlice(1, 8) Range(3..^3) Length Id(4) Slice(3, 4) Id(5)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size      164 (0xa4)
  .maxstack  4
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Range V_4,
                int V_5,
                int V_6,
                int V_7,
                int V_8,
                System.Index V_9)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  dup
  IL_0006:  ldfld      "Container C.F"
  IL_000b:  ldarg.0
  IL_000c:  call       "System.Range C.Id(System.Range)"
  IL_0011:  stloc.0
  IL_0012:  dup
  IL_0013:  callvirt   "int Container.Length.get"
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "System.Index System.Range.Start.get"
  IL_0020:  stloc.s    V_9
  IL_0022:  ldloca.s   V_9
  IL_0024:  ldloc.1
  IL_0025:  call       "int System.Index.GetOffset(int)"
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "System.Index System.Range.End.get"
  IL_0032:  stloc.s    V_9
  IL_0034:  ldloca.s   V_9
  IL_0036:  ldloc.1
  IL_0037:  call       "int System.Index.GetOffset(int)"
  IL_003c:  ldloc.2
  IL_003d:  sub
  IL_003e:  stloc.3
  IL_003f:  ldarg.1
  IL_0040:  call       "System.Range C.Id(System.Range)"
  IL_0045:  pop
  IL_0046:  ldloc.2
  IL_0047:  ldloc.3
  IL_0048:  callvirt   "Buffer10 Container.Slice(int, int)"
  IL_004d:  ldarg.2
  IL_004e:  call       "System.Range C.Id(System.Range)"
  IL_0053:  stloc.s    V_4
  IL_0055:  dup
  IL_0056:  callvirt   "int Buffer10.Length.get"
  IL_005b:  stloc.s    V_5
  IL_005d:  ldloca.s   V_4
  IL_005f:  call       "System.Index System.Range.Start.get"
  IL_0064:  stloc.s    V_9
  IL_0066:  ldloca.s   V_9
  IL_0068:  ldloc.s    V_5
  IL_006a:  call       "int System.Index.GetOffset(int)"
  IL_006f:  stloc.s    V_6
  IL_0071:  ldloca.s   V_4
  IL_0073:  call       "System.Index System.Range.End.get"
  IL_0078:  stloc.s    V_9
  IL_007a:  ldloca.s   V_9
  IL_007c:  ldloc.s    V_5
  IL_007e:  call       "int System.Index.GetOffset(int)"
  IL_0083:  ldloc.s    V_6
  IL_0085:  sub
  IL_0086:  stloc.s    V_7
  IL_0088:  ldarg.3
  IL_0089:  call       "int C.Id(int)"
  IL_008e:  stloc.s    V_8
  IL_0090:  ldloc.s    V_6
  IL_0092:  ldloc.s    V_7
  IL_0094:  callvirt   "int[] Buffer10.Slice(int, int)"
  IL_0099:  ldloc.s    V_8
  IL_009b:  ldarg.s    V_4
  IL_009d:  call       "int C.Id(int)"
  IL_00a2:  stelem.i4
  IL_00a3:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(r)] = { [Id(r2)] = { }, [Id(r3)] = { [Id(i4)] = Id(i5) } } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... i5) } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... i5) } } } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(r)')
                  Value:
                    IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r')
                            IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r2)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r2')
                        IParameterReferenceOperation: r2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(r3)')
                      Value:
                        IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r3)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r3')
                                IParameterReferenceOperation: r3 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B4]
                    Entering: {R4}
            .locals {R4}
            {
                CaptureIds: [3]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (2)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(i4)')
                          Value:
                            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i4)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i4')
                                    IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(i4)] = Id(i5)')
                          Left:
                            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(i4)]')
                              Array reference:
                                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32[]) (Syntax: '[Id(r3)]')
                                  Instance:
                                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: Buffer10) (Syntax: '[Id(r)]')
                                      Instance:
                                        IFieldReferenceOperation: Container C.F (OperationKind.FieldReference, Type: Container) (Syntax: 'F')
                                          Instance Receiver:
                                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... i5) } } } }')
                                      Argument:
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'Id(r)')
                                      LengthSymbol: System.Int32 Container.Length { get; }
                                      IndexerSymbol: Buffer10 Container.Slice(System.Int32 start, System.Int32 length)
                                  Argument:
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'Id(r3)')
                                  LengthSymbol: System.Int32 Buffer10.Length { get; }
                                  IndexerSymbol: System.Int32[] Buffer10.Slice(System.Int32 start, System.Int32 length)
                              Indices(1):
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(i4)')
                          Right:
                            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i5)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i5')
                                    IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Next (Regular) Block[B5]
                        Leaving: {R4} {R3} {R2}
            }
        }
    }
    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Next (Return) Block[B6]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... i5) } } } }')
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_WithEmptyNestedNesting()
        {
            string source = """
C.M(1, 2);

class Container
{
    public Buffer10 this[int i] => throw null;
}

class Buffer10
{
    public int[] this[int i] => throw null;
}

class C
{
    public Container F = new Container();
    public static C M(int i1, int i2)
    {
        return new C() { F = { [Id(i1)] = { [Id(i2)] = { } } } };
    }
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(1) Id(2)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "int C.Id(int)"
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  call       "int C.Id(int)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(i1)] = { [Id(i2)] = { } } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = { } } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1')
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i2)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2')
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);

        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_WithMixedNestedNesting()
        {
            string source = """
C.M(1, 2, 3, 4, 5);

class Container
{
    public Buffer10 this[int i] { get { return new Buffer10(); } }
}

class Buffer10
{
    public int[] this[int i] { get { return new int[10]; } }
}

class C
{
    public Container F = new Container();
    public static C M(int i1, int i2, int i3, int i4, int i5)
    {
        return new C() { F = { [Id(i1)] = { [Id(i2)] = { }, [Id(i3)] = { [Id(i4)] = Id(i5) } } } };
    }
    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(1) Id(2) Id(3) Id(4) Id(5)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (int V_0,
                int V_1,
                int V_2)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "int C.Id(int)"
  IL_000b:  stloc.0
  IL_000c:  ldarg.1
  IL_000d:  call       "int C.Id(int)"
  IL_0012:  pop
  IL_0013:  ldarg.2
  IL_0014:  call       "int C.Id(int)"
  IL_0019:  stloc.1
  IL_001a:  ldarg.3
  IL_001b:  call       "int C.Id(int)"
  IL_0020:  stloc.2
  IL_0021:  dup
  IL_0022:  ldfld      "Container C.F"
  IL_0027:  ldloc.0
  IL_0028:  callvirt   "Buffer10 Container.this[int].get"
  IL_002d:  ldloc.1
  IL_002e:  callvirt   "int[] Buffer10.this[int].get"
  IL_0033:  ldloc.2
  IL_0034:  ldarg.s    V_4
  IL_0036:  call       "int C.Id(int)"
  IL_003b:  stelem.i4
  IL_003c:  ret
}
""");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(i1)] = { [Id(i2)] = { }, [Id(i3)] = { [Id(i4)] = Id(i5) } } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... i5) } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... i5) } } } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(i1)')
                  Value:
                    IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i1)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i1')
                            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i2)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i2')
                        IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(i3)')
                      Value:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i3)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i3')
                                IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B4]
                    Entering: {R4}
            .locals {R4}
            {
                CaptureIds: [3]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (2)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(i4)')
                          Value:
                            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i4)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i4')
                                    IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '[Id(i4)] = Id(i5)')
                          Left:
                            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '[Id(i4)]')
                              Array reference:
                                IPropertyReferenceOperation: System.Int32[] Buffer10.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: System.Int32[]) (Syntax: '[Id(i3)]')
                                  Instance Receiver:
                                    IPropertyReferenceOperation: Buffer10 Container.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: Buffer10) (Syntax: '[Id(i1)]')
                                      Instance Receiver:
                                        IFieldReferenceOperation: Container C.F (OperationKind.FieldReference, Type: Container) (Syntax: 'F')
                                          Instance Receiver:
                                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... i5) } } } }')
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'Id(i1)')
                                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(i1)')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'Id(i3)')
                                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(i3)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Indices(1):
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(i4)')
                          Right:
                            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(i5)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i5')
                                    IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Next (Regular) Block[B5]
                        Leaving: {R4} {R3} {R2}
            }
        }
    }
    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Next (Return) Block[B6]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... i5) } } } }')
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
""",
                graph, symbol);

        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_ArrayAccess_WithEmptyNestedNesting()
        {
            string source = """
C.M(1..^1, 2..^2);

class C
{
    public int[][] F = new int[][] { new int[10], new int[10] };
    public static C M(System.Range r1, System.Range r2)
    {
        return new C() { F = { [Id(r1)] = { [Id(r2)] = { } } } };
    }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(1..^1) Range(2..^2)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "System.Range C.Id(System.Range)"
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  call       "System.Range C.Id(System.Range)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(r1)] = { [Id(r2)] = { } } } }", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree: """
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = { } } } }')
  Arguments(0)
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ F = { [Id ... = { } } } }')
      Initializers(1):
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[][]) (Syntax: 'F = { [Id(r ... ] = { } } }')
            InitializedMember:
              IFieldReferenceOperation: System.Int32[][] C.F (OperationKind.FieldReference, Type: System.Int32[][]) (Syntax: 'F')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F')
            Initializer:
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[][]) (Syntax: '{ [Id(r1)]  ... ] = { } } }')
                Initializers(1):
                    IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[][]) (Syntax: '[Id(r1)] =  ... 2)] = { } }')
                      InitializedMember:
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32[][]) (Syntax: '[Id(r1)]')
                          Array reference:
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[][], IsImplicit) (Syntax: 'F')
                          Indices(1):
                              IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r1)')
                                Instance Receiver:
                                  null
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r1')
                                      IParameterReferenceOperation: r1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r1')
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[][]) (Syntax: '{ [Id(r2)] = { } }')
                          Initializers(1):
                              IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Int32[][]) (Syntax: '[Id(r2)] = { }')
                                InitializedMember:
                                  IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32[][]) (Syntax: '[Id(r2)]')
                                    Array reference:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Int32[][], IsImplicit) (Syntax: '[Id(r1)]')
                                    Indices(1):
                                        IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r2)')
                                          Instance Receiver:
                                            null
                                          Arguments(1):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r2')
                                                IParameterReferenceOperation: r2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r2')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Initializer:
                                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Int32[][]) (Syntax: '{ }')
                                    Initializers(0)
""");

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = { } } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r1')
                    IParameterReferenceOperation: r1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r2)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r2')
                    IParameterReferenceOperation: r2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_ArrayAccess_WithNestedNesting()
        {
            string source = """
C.M(1..^1);

public class D
{
    public int X { set { System.Console.Write($"X={value} "); } }
}
class C
{
    public D[] F = new D[] { new D(), new D(), new D() };
    public static C M(System.Range r1)
    {
        return new C() { F = { [Id(r1)] = { [Id(0)] = { X = Id(42) } } } };
    }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(1..^1) Id(0) Id(42) X=42");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (System.Range V_0,
                int V_1)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "System.Range C.Id(System.Range)"
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  call       "int C.Id(int)"
  IL_0012:  stloc.1
  IL_0013:  dup
  IL_0014:  ldfld      "D[] C.F"
  IL_0019:  ldloc.0
  IL_001a:  call       "D[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<D>(D[], System.Range)"
  IL_001f:  ldloc.1
  IL_0020:  ldelem.ref
  IL_0021:  ldc.i4.s   42
  IL_0023:  call       "int C.Id(int)"
  IL_0028:  callvirt   "void D.X.set"
  IL_002d:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(r1)] = { [Id(0)] = { X = Id(42) } } } }", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree: """
IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... 42) } } } }')
Arguments(0)
Initializer:
  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ F = { [Id ... 42) } } } }')
    Initializers(1):
        IMemberInitializerOperation (OperationKind.MemberInitializer, Type: D[]) (Syntax: 'F = { [Id(r ... d(42) } } }')
          InitializedMember:
            IFieldReferenceOperation: D[] C.F (OperationKind.FieldReference, Type: D[]) (Syntax: 'F')
              Instance Receiver:
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F')
          Initializer:
            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: D[]) (Syntax: '{ [Id(r1)]  ... d(42) } } }')
              Initializers(1):
                  IMemberInitializerOperation (OperationKind.MemberInitializer, Type: D[]) (Syntax: '[Id(r1)] =  ...  Id(42) } }')
                    InitializedMember:
                      IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: D[]) (Syntax: '[Id(r1)]')
                        Array reference:
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: D[], IsImplicit) (Syntax: 'F')
                        Indices(1):
                            IInvocationOperation (System.Range C.Id(System.Range r)) (OperationKind.Invocation, Type: System.Range) (Syntax: 'Id(r1)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: r) (OperationKind.Argument, Type: null) (Syntax: 'r1')
                                    IParameterReferenceOperation: r1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'r1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer:
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: D[]) (Syntax: '{ [Id(0)] = ...  Id(42) } }')
                        Initializers(1):
                            IMemberInitializerOperation (OperationKind.MemberInitializer, Type: D) (Syntax: '[Id(0)] = { X = Id(42) }')
                              InitializedMember:
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: D) (Syntax: '[Id(0)]')
                                  Array reference:
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: D[], IsImplicit) (Syntax: '[Id(r1)]')
                                  Indices(1):
                                      IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(0)')
                                        Instance Receiver:
                                          null
                                        Arguments(1):
                                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Initializer:
                                IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: D) (Syntax: '{ X = Id(42) }')
                                  Initializers(1):
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = Id(42)')
                                        Left:
                                          IPropertyReferenceOperation: System.Int32 D.X { set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                                            Instance Receiver:
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: D, IsImplicit) (Syntax: 'X')
                                        Right:
                                          IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(42)')
                                            Instance Receiver:
                                              null
                                            Arguments(1):
                                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '42')
                                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_ArrayAccess_WithMultipleNestedNesting()
        {
            string source = """
C.M(1..^1);

public class D
{
    public int X { set { System.Console.Write($"X={value} "); } }
}
class C
{
    public D[] F = new D[] { new D(), new D(), new D(), new D() };
    public static C M(System.Range r1)
    {
        return new C() { F = { [Id(r1)] = { [Id(0)] = { X = Id(42) }, [Id(1)] = { X = Id(43) } } } };
    }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(1..^1) Id(0) Id(42) X=42 Id(1) Id(43) X=43");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       79 (0x4f)
  .maxstack  3
  .locals init (System.Range V_0,
                int V_1,
                int V_2)
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "System.Range C.Id(System.Range)"
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  call       "int C.Id(int)"
  IL_0012:  stloc.1
  IL_0013:  dup
  IL_0014:  ldfld      "D[] C.F"
  IL_0019:  ldloc.0
  IL_001a:  call       "D[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<D>(D[], System.Range)"
  IL_001f:  ldloc.1
  IL_0020:  ldelem.ref
  IL_0021:  ldc.i4.s   42
  IL_0023:  call       "int C.Id(int)"
  IL_0028:  callvirt   "void D.X.set"
  IL_002d:  ldc.i4.1
  IL_002e:  call       "int C.Id(int)"
  IL_0033:  stloc.2
  IL_0034:  dup
  IL_0035:  ldfld      "D[] C.F"
  IL_003a:  ldloc.0
  IL_003b:  call       "D[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<D>(D[], System.Range)"
  IL_0040:  ldloc.2
  IL_0041:  ldelem.ref
  IL_0042:  ldc.i4.s   43
  IL_0044:  call       "int C.Id(int)"
  IL_0049:  callvirt   "void D.X.set"
  IL_004e:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_PointerAccess_WithEmptyNestedNesting()
        {
            string source = """
unsafe class C
{
    public int** F;
    public C(int** pp) { F = pp; }

    public static void Main()
    {
        var array = new[] { 0, 0 };

        fixed (int* p = array)
        {
            var array2 = new[] { p };

            fixed (int** pp = array2 )
            {
                M(pp);
            }
        }
    }

    public static C M(int** pp)
    {
        return new C(pp) { F = { [Id(0)] = { [Id(1)] = { } } } };
    }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(0) Id(1)", verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  newobj     "C..ctor(int**)"
  IL_0006:  ldc.i4.0
  IL_0007:  call       "int C.Id(int)"
  IL_000c:  pop
  IL_000d:  ldc.i4.1
  IL_000e:  call       "int C.Id(int)"
  IL_0013:  pop
  IL_0014:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C(pp) { F = { [Id(0)] = { [Id(1)] = { } } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C(pp) { ... = { } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32** pp)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(pp) { ... = { } } } }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: pp) (OperationKind.Argument, Type: null) (Syntax: 'pp')
                        IParameterReferenceOperation: pp (OperationKind.ParameterReference, Type: System.Int32**) (Syntax: 'pp')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(0)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C(pp) { ... = { } } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_DynamicAccess_WithEmptyNestedNesting()
        {
            string source = """
C.M();

class C
{
    public dynamic F;

    public static C M()
    {
        return new C() { F = { [Id(0)] = { [Id(1)] = { } } } };
    }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(0) Id(1)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldc.i4.0
  IL_0006:  call       "int C.Id(int)"
  IL_000b:  pop
  IL_000c:  ldc.i4.1
  IL_000d:  call       "int C.Id(int)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { F = { [Id(0)] = { [Id(1)] = { } } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { F ... = { } } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(0)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { F ... = { } } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_DynamicAccess2_WithEmptyNestedNesting()
        {
            string source = """
C.M();

class C
{
    public dynamic this[int i] => throw null;

    public static C M()
    {
        return new C() { [Id(0)] = { [Id(1)] = { } } };
    }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(0) Id(1)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldc.i4.0
  IL_0006:  call       "int C.Id(int)"
  IL_000b:  pop
  IL_000c:  ldc.i4.1
  IL_000d:  call       "int C.Id(int)"
  IL_0012:  pop
  IL_0013:  ret
}
""");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { [Id(0)] = { [Id(1)] = { } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { [ ... ] = { } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { [ ... ] = { } } }')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(0)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... ] = { } } }')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Integer_DynamicAccess2_WithMixedNestedNesting()
        {
            string source = """
C.M();

public class C
{
    public dynamic this[int i] => new C();
    public int F;

    public static C M()
    {
        return new C() { [Id(0)] = { [Id(1)] = { }, [Id(2)] = { F = Id(3) } } };
    }

    public static int Id(int i) { System.Console.Write($"Id({i}) "); return i; }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.StandardAndCSharp);
            var verifier = CompileAndVerify(comp, expectedOutput: "Id(0) Id(1) Id(2) Id(3)");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Last();
            Assert.Equal("new C() { [Id(0)] = { [Id(1)] = { }, [Id(2)] = { F = Id(3) } } }", node.ToString());

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(node.Parent.Parent, model);
            ControlFlowGraphVerifier.VerifyGraph(comp, """
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { [ ... Id(3) } } }')
              Value:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { [ ... Id(3) } } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(0)')
                  Value:
                    IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(0)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '0')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(1)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Id(2)')
                      Value:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(2)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'F = Id(3)')
                      Left:
                        IDynamicMemberReferenceOperation (Member Name: "F", Containing Type: dynamic) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'F')
                          Type Arguments(0)
                          Instance Receiver:
                            IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: '[Id(2)]')
                              Expression:
                                IPropertyReferenceOperation: dynamic C.this[System.Int32 i] { get; } (OperationKind.PropertyReference, Type: dynamic) (Syntax: '[Id(0)]')
                                  Instance Receiver:
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... Id(3) } } }')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'Id(0)')
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(0)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Arguments(1):
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Id(2)')
                              ArgumentNames(0)
                              ArgumentRefKinds(0)
                      Right:
                        IInvocationOperation (System.Int32 C.Id(System.Int32 i)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Id(3)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B4]
                    Leaving: {R3} {R2}
        }
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (0)
        Next (Return) Block[B5]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { [ ... Id(3) } } }')
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
""",
                graph, symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_WithEmptyNestedInitializer()
        {
            string source = """
C.M(3..^6);

class Buffer10
{
    public int[] _array = new int[10];
    public int Length => throw null;
    public int[] Slice(int start, int length) => throw null;
}

class C
{
    public Buffer10 F = new Buffer10();
    public static C M(System.Range r)
    {
        return new C() { F = { [Id(r)] = { } } };
    }
    public static System.Range Id(System.Range r) { System.Console.Write($"Range({r}) "); return r; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Range(3..^6)");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_RangeLiteral_WithEmptyNestedInitializer()
        {
            string source = """
C.M(3, 6);

class Buffer10
{
    public int Length => throw null;
    public int[] Slice(int start, int length) => throw null;
}

class C
{
    public Buffer10 F = new Buffer10();
    public static C M(int i, int j)
    {
        return new C() { F = { [^Id(i)..^Id(j)] = { } } };
    }
    public static int Id(int i) { System.Console.Write($"{i} "); return i; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "3 6");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", """
{
  // Code size       36 (0x24)
  .maxstack  4
  IL_0000:  newobj     "C..ctor()"
  IL_0005:  ldarg.0
  IL_0006:  call       "int C.Id(int)"
  IL_000b:  ldc.i4.1
  IL_000c:  newobj     "System.Index..ctor(int, bool)"
  IL_0011:  ldarg.1
  IL_0012:  call       "int C.Id(int)"
  IL_0017:  ldc.i4.1
  IL_0018:  newobj     "System.Index..ctor(int, bool)"
  IL_001d:  newobj     "System.Range..ctor(System.Index, System.Index)"
  IL_0022:  pop
  IL_0023:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Range_Indexed()
        {
            string source = """
Buffer10 b = default;
b[..][0] = 1;

_ = new Buffer10() { [..][0] = 2 }; // 1

struct Buffer10
{
    public Buffer10() { }
    public int Length => throw null;
    public int[] Slice(int start, int length) => throw null;
}
""";
            var comp = CreateCompilationWithIndexAndRange(source);
            comp.VerifyDiagnostics(
                // (4,22): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // _ = new Buffer10() { [..][0] = 2 }; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[..]").WithLocation(4, 22),
                // (4,26): error CS1003: Syntax error, '=' expected
                // _ = new Buffer10() { [..][0] = 2 }; // 1
                Diagnostic(ErrorCode.ERR_SyntaxError, "[").WithArguments("=").WithLocation(4, 26),
                // (4,26): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // _ = new Buffer10() { [..][0] = 2 }; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(4, 26)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Indexed()
        {
            string source = """
Buffer10 b = default;
b[^1][0] = 1;

_ = new Buffer10() { [^1][0] = 2 }; // 1

struct Buffer10
{
    public Buffer10() { }
    public int Length => throw null;
    public int[] this[int i] { get => throw null; set => throw null; }
}
""";
            var comp = CreateCompilationWithIndexAndRange(source);
            comp.VerifyDiagnostics(
                // (4,26): error CS1003: Syntax error, '=' expected
                // _ = new Buffer10() { [^1][0] = 2 }; // 1
                Diagnostic(ErrorCode.ERR_SyntaxError, "[").WithArguments("=").WithLocation(4, 26),
                // (4,26): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // _ = new Buffer10() { [^1][0] = 2 }; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(4, 26)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_Indexed_Array()
        {
            string source = """
_ = new C() { F = { [^1][0] = 2 } }; // 1

public class C
{
    public int[][] F;
}
""";
            var comp = CreateCompilationWithIndexAndRange(source);
            comp.VerifyDiagnostics(
                // (1,25): error CS1003: Syntax error, '=' expected
                // _ = new C() { F = { [^1][0] = 2 } }; // 1
                Diagnostic(ErrorCode.ERR_SyntaxError, "[").WithArguments("=").WithLocation(1, 25),
                // (1,25): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                // _ = new C() { F = { [^1][0] = 2 } }; // 1
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(1, 25)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_IndexCreation_FromEnd()
        {
            string source = """
M();

partial class Program
{
    public static Buffer10 M() => new Buffer10() { [new System.Index(1, fromEnd: true)] = 2 };
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => 10;
    public int this[int x]
    {
        get => throw null;
        set { System.Console.Write($"Index={x} Value={value} "); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index=9 Value=2");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M", """
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (Buffer10 V_0,
                int V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       "Buffer10..ctor()"
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "int Buffer10.Length.get"
  IL_000e:  ldc.i4.1
  IL_000f:  sub
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldloc.1
  IL_0014:  ldc.i4.2
  IL_0015:  call       "void Buffer10.this[int].set"
  IL_001a:  ldloc.0
  IL_001b:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_IndexCreation_FromStart()
        {
            string source = """
M();

partial class Program
{
    static Buffer10 M() => new Buffer10() { [new System.Index(1, fromEnd: false)] = 2 };
}

struct Buffer10
{
    public Buffer10() { }

    public int Length => 10;
    public int this[int x]
    {
        get => throw null;
        set { System.Console.Write($"Index={x} Value={value} "); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index=1 Value=2");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M", """
{
  // Code size       18 (0x12)
  .maxstack  3
  .locals init (Buffer10 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       "Buffer10..ctor()"
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  call       "void Buffer10.this[int].set"
  IL_0010:  ldloc.0
  IL_0011:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_IndexConversion()
        {
            string source = """
M();

partial class Program
{
    static Buffer10 M() => new Buffer10() { [2] = 2 };
}

class Buffer10
{
    public int this[System.Index x]
    {
        set { System.Console.Write($"Index={x} Value={value} "); }
    }
}
""";
            var comp = CreateCompilationWithIndex(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "Index=2 Value=2");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.M", """
{
  // Code size       21 (0x15)
  .maxstack  4
  .locals init (System.Index V_0)
  IL_0000:  newobj     "Buffer10..ctor()"
  IL_0005:  ldc.i4.2
  IL_0006:  call       "System.Index System.Index.op_Implicit(int)"
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.2
  IL_000f:  callvirt   "void Buffer10.this[System.Index].set"
  IL_0014:  ret
}
""");
        }

        private const string IndexWithSideEffects = """
            namespace System
            {
                using System.Runtime.CompilerServices;
                public readonly struct Index : IEquatable<Index>
                {
                    private readonly int _value;

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public Index(int value, bool fromEnd = false)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        if (fromEnd)
                            _value = ~value;
                        else
                            _value = value;
                    }

                    // The following private constructors mainly created for perf reason to avoid the checks
                    private Index(int value)
                    {
                        _value = value;
                    }

                    public static Index Start => new Index(0);
                    public static Index End => new Index(~0);

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static Index FromStart(int value)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        return new Index(value);
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static Index FromEnd(int value)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        return new Index(~value);
                    }

                    public int Value
                    {
                        get
                        {
                            if (_value < 0)
                                return ~_value;
                            else
                                return _value;
                        }
                    }

                    public bool IsFromEnd => _value < 0;

                    public int GetOffset(int length)
                    {
                        int offset;

                        if (IsFromEnd)
                            offset = length - (~_value);
                        else
                            offset = _value;

                        return offset;
                    }

                    public override bool Equals(object value) => value is Index && _value == ((Index)value)._value;

                    public bool Equals (Index other) => _value == other._value;

                    public override int GetHashCode() => _value;

                    public static implicit operator Index(int value) => FromStart(value);
                    public static Index operator ++(Index index)
                    {
                        System.Console.Write("++ ");
                        if (index.IsFromEnd)
                        {
                            return new Index(index.Value - 1, fromEnd: true);
                        }
                        else
                        {
                            return new Index(index.Value + 1);
                        }
                    }

                    public override string ToString() => IsFromEnd ? "^" + Value.ToString() : Value.ToString();
                }
            }
            """;

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_SideEffects()
        {
            string source = """
M(^3);

partial class Program
{
    static Buffer10 M(System.Index i) => new Buffer10() { [i++] = { X = 42, Y = 43, Z = 44 } };
}

class Triple
{
    public int X { set { System.Console.Write($"X={value} "); } }
    public int Y { set { System.Console.Write($"Y={value} "); } }
    public int Z { set { System.Console.Write($"Z={value} "); } }
}

class Buffer10
{
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public Triple this[int x]
    {
        get { System.Console.Write($"Index={x} "); return new Triple(); }
    }
}
""";
            var comp = CreateCompilation(new[] { source, IndexWithSideEffects });
            var verifier = CompileAndVerify(comp, expectedOutput: "++ Length Index=7 X=42 Index=7 Y=43 Index=7 Z=44", verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67533")]
        public void InObjectInitializer_Index_SideEffects_WithNesting()
        {
            string source = """
M(^10, ^8);

partial class Program
{
    static Buffer10Container M(System.Index i, System.Index j) 
        => new Buffer10Container() { 
            [i++] = { [j++] = { X = 42, Y = 43 } }, 
            [i++] = { [j++] = { X = 101, Y = 102 } } };
}

class Pair
{
    public int X { set { System.Console.Write($"X={value} "); } }
    public int Y { set { System.Console.Write($"Y={value} "); } }
}

class Buffer10
{
    public int Length { get { System.Console.Write("Length "); return 10; } }
    public Pair this[int x]
    {
        get { System.Console.Write($"Index={x} "); return new Pair(); }
    }
}

class Buffer10Container
{
    public int Length { get { System.Console.Write("ContainerLength "); return 10; } }
    public Buffer10 this[int x]
    {
        get { System.Console.Write($"ContainerIndex={x} "); return new Buffer10(); }
    }
}
""";
            var comp = CreateCompilation(new[] { source, IndexWithSideEffects });
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped,
                expectedOutput: "++ ContainerLength ++ ContainerIndex=0 Length ContainerIndex=0 Index=2 X=42 ContainerIndex=0 Index=2 Y=43 " +
                    "++ ContainerLength ++ ContainerIndex=1 Length ContainerIndex=1 Index=3 X=101 ContainerIndex=1 Index=3 Y=102");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void InObjectInitializer_ExpressionTreePatternIndexAndRange()
        {
            var src = """

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Expression<Func<System.Index, S>> e1 = (System.Index i) => new S { [i] = 42 }; // 1
        Expression<Func<System.Range, S>> e2 = (System.Range r) => new S { [r] = { [0] = 1 } }; // 2
    }
}

class S
{
    public int Length => 0;

    public int this[int x]
    {
        get => throw null;
        set => throw null;
    }

    public int[] Slice(int start, int length) => null;
}

""";
            var comp = CreateCompilationWithIndexAndRange(new[] { src, TestSources.GetSubArray });
            comp.VerifyEmitDiagnostics(
                // 0.cs(10,76): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<System.Index, S>> e1 = (System.Index i) => new S { [i] = 42 }; // 1
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "[i] = 42").WithLocation(10, 76),
                // 0.cs(10,76): error CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access
                //         Expression<Func<System.Index, S>> e1 = (System.Index i) => new S { [i] = 42 }; // 1
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, "[i]").WithLocation(10, 76),
                // 0.cs(11,76): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<System.Range, S>> e2 = (System.Range r) => new S { [r] = { [0] = 1 } }; // 2
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "[r] = { [0] = 1 }").WithLocation(11, 76),
                // 0.cs(11,76): error CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access
                //         Expression<Func<System.Range, S>> e2 = (System.Range r) => new S { [r] = { [0] = 1 } }; // 2
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer, "[r]").WithLocation(11, 76),
                // 0.cs(11,84): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<System.Range, S>> e2 = (System.Range r) => new S { [r] = { [0] = 1 } }; // 2
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "[0] = 1").WithLocation(11, 84)
            );
        }
    }
}
