// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.RefLocalsReturns)]
        [Fact]
        public void SuppressNullableWarningOperation()
        {
            var comp = CreateCompilation(@"
class C
{
#nullable enable
    void M(string? x)
    /*<bind>*/{
        x!.ToString();
    }/*</bind>*/
}");
            var expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x!.ToString();')
    Expression:
      IInvocationOperation (virtual System.String System.String.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x!.ToString()')
        Instance Receiver:
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
        Arguments(0)";

            var diagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, diagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x!.ToString();')
          Expression:
            IInvocationOperation (virtual System.String System.String.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x!.ToString()')
              Instance Receiver:
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
              Arguments(0)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)";

            VerifyFlowGraphForTest<BlockSyntax>(comp, expectedFlowGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.RefLocalsReturns)]
        [Fact]
        public void SuppressNullableWarningOperation_ConstantValue()
        {
            var comp = CreateCompilation(@"
class C
{
#nullable enable
    void M()
    /*<bind>*/{
        ((string)null)!.ToString();
    }/*</bind>*/
}");
            var expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '((string)nu ... ToString();')
    Expression:
      IInvocationOperation (virtual System.String System.String.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '((string)nu ... .ToString()')
        Instance Receiver:
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null) (Syntax: '(string)null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Arguments(0)";

            var diagnostics = new[]
            {
                // (7,10): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         ((string)null)!.ToString();
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "(string)null").WithLocation(7, 10)
            };
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, diagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.RefLocalsReturns)]
        [Fact]
        public void SuppressNullableWarningOperation_NestedFlow()
        {
            var comp = CreateCompilation(@"
class C
{
#nullable enable
    void M(bool b, string? x, string? y)
    /*<bind>*/{
        (b ? x : y)!.ToString();
    }/*</bind>*/
}");
            comp.VerifyDiagnostics();

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value:
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String?) (Syntax: 'x')
        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value:
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String?) (Syntax: 'y')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(b ? x : y)!.ToString();')
              Expression:
                IInvocationOperation (virtual System.String System.String.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '(b ? x : y)!.ToString()')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b ? x : y')
                  Arguments(0)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)";

            VerifyFlowGraphForTest<BlockSyntax>(comp, expectedFlowGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.RefLocalsReturns)]
        [Fact]
        public void RefReassignmentExpressions()
        {
            var comp = CreateCompilation(@"
class C
{
    ref readonly int M(ref int rx)
    {
        ref int ry = ref rx;
        rx = ref ry;
        ry = ref """".Length == 0
            ? ref (rx = ref ry)
            : ref (ry = ref rx);
        return ref (ry = ref rx);
    }
}");
            comp.VerifyDiagnostics();

            var m = comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();
            comp.VerifyOperationTree(m, expectedOperationTree: @"
IBlockOperation (4 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Int32 ry
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'ref int ry = ref rx;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'ref int ry = ref rx')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Int32 ry) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ry = ref rx')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ref rx')
                IParameterReferenceOperation: rx (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'rx')
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'rx = ref ry;')
    Expression: 
      ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'rx = ref ry')
        Left: 
          IParameterReferenceOperation: rx (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'rx')
        Right: 
          ILocalReferenceOperation: ry (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'ry')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ry = ref """" ...  = ref rx);')
    Expression: 
      ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'ry = ref """" ... y = ref rx)')
        Left: 
          ILocalReferenceOperation: ry (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'ry')
        Right: 
          IConditionalOperation (IsRef) (OperationKind.Conditional, Type: System.Int32) (Syntax: '"""".Length = ... y = ref rx)')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: '"""".Length == 0')
                Left: 
                  IPropertyReferenceOperation: System.Int32 System.String.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '"""".Length')
                    Instance Receiver: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            WhenTrue: 
              ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'rx = ref ry')
                Left: 
                  IParameterReferenceOperation: rx (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'rx')
                Right: 
                  ILocalReferenceOperation: ry (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'ry')
            WhenFalse: 
              ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'ry = ref rx')
                Left: 
                  ILocalReferenceOperation: ry (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'ry')
                Right: 
                  IParameterReferenceOperation: rx (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'rx')
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return ref  ...  = ref rx);')
    ReturnedValue: 
      ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'ry = ref rx')
        Left: 
          ILocalReferenceOperation: ry (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'ry')
        Right: 
          IParameterReferenceOperation: rx (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'rx')");
        }

        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [Fact]
        public void IOperationRefFor()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
using System;
class C
{
    public class LinkedList
    {
        public int Value;
        public LinkedList Next;
    }
    public void M(LinkedList list)
    {
        for (ref readonly var cur = ref list; cur != null; cur = ref cur.Next)
        {
            Console.WriteLine(cur.Value);
        }
    }
}", options: TestOptions.Regular);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
            var m = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
            comp.VerifyOperationTree(m, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'for (ref re ... }')
    Locals: Local_1: C.LinkedList cur
    Condition: 
      IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'cur != null')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'cur')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: cur (OperationKind.LocalReference, Type: C.LinkedList) (Syntax: 'cur')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Before:
        IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'ref readonl ...  = ref list')
          IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'ref readonl ...  = ref list')
            Declarators:
                IVariableDeclaratorOperation (Symbol: C.LinkedList cur) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'cur = ref list')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ref list')
                      IParameterReferenceOperation: list (OperationKind.ParameterReference, Type: C.LinkedList) (Syntax: 'list')
            Initializer: 
              null
    AtLoopBottom:
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'cur = ref cur.Next')
          Expression: 
            ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: C.LinkedList) (Syntax: 'cur = ref cur.Next')
              Left: 
                ILocalReferenceOperation: cur (OperationKind.LocalReference, Type: C.LinkedList) (Syntax: 'cur')
              Right: 
                IFieldReferenceOperation: C.LinkedList C.LinkedList.Next (OperationKind.FieldReference, Type: C.LinkedList) (Syntax: 'cur.Next')
                  Instance Receiver: 
                    ILocalReferenceOperation: cur (OperationKind.LocalReference, Type: C.LinkedList) (Syntax: 'cur')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... cur.Value);')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... (cur.Value)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'cur.Value')
                    IFieldReferenceOperation: System.Int32 C.LinkedList.Value (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'cur.Value')
                      Instance Receiver: 
                        ILocalReferenceOperation: cur (OperationKind.LocalReference, Type: C.LinkedList) (Syntax: 'cur')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");
            var op = (IForLoopOperation)comp.GetSemanticModel(tree).GetOperation(tree.GetRoot().DescendantNodes().OfType<ForStatementSyntax>().Single());
            Assert.Equal(RefKind.RefReadOnly, op.Locals.Single().RefKind);
        }

        [CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [Fact]
        public void IOperationRefForeach()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
using System;
class C
{
    public void M(RefEnumerable re)
    {
        foreach (ref readonly var x in re)
        {
            Console.WriteLine(x);
        }
    }
}

class RefEnumerable
{
    private readonly int[] _arr = new int[5];
    public StructEnum GetEnumerator() => new StructEnum(_arr);

    public struct StructEnum
    {
        private readonly int[] _arr;
        private int _current;
        public StructEnum(int[] arr)
        {
            _arr = arr;
            _current = -1;
        }
        public ref int Current => ref _arr[_current];
        public bool MoveNext() => ++_current != _arr.Length;
    }
}", options: TestOptions.Regular);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
            var m = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
            comp.VerifyOperationTree(m, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (re ... }')
    Locals: Local_1: System.Int32 x
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: RefEnumerable, IsImplicit) (Syntax: 're')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: re (OperationKind.ParameterReference, Type: RefEnumerable) (Syntax: 're')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(x);')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    NextVariables(0)");
            var op = (IForEachLoopOperation)comp.GetSemanticModel(tree).GetOperation(tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single());
            Assert.Equal(RefKind.RefReadOnly, op.Locals.Single().RefKind);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(382240, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=382240")]
        public void NullInPlaceOfParamArray()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(null);
        Test2(new object(), null);
    }

    static void Test1(params int[] x)
    {
    }

    static void Test2(int y, params int[] x)
    {
    }
}";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (7,15): error CS1503: Argument 1: cannot convert from 'object' to 'int'
                //         Test2(new object(), null);
                Diagnostic(ErrorCode.ERR_BadArgType, "new object()").WithArguments("1", "object", "int").WithLocation(7, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IInvocationOperation (void Cls.Test1(params System.Int32[] x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test1(null)')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'null')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Test2(new o ... ct(), null)')
  Children(2):
      IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
        Arguments(0)
        Initializer: 
          null
      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructionAssignmentFromTuple()
        {
            var text = @"
public class C
{
    public static void M()
    {
        int x, y, z;
        (x, y, z) = (1, 2, 3);
        (x, y, z) = new C();
        var (a, b) = (1, 2);
    }
    public void Deconstruct(out int a, out int b, out int c)
    {
        a = b = c = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib40(text, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var assignments = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().ToArray();
            Assert.Equal("(x, y, z) = (1, 2, 3)", assignments[0].ToString());
            IOperation operation1 = model.GetOperation(assignments[0]);
            Assert.NotNull(operation1);
            Assert.Equal(OperationKind.DeconstructionAssignment, operation1.Kind);
            Assert.False(operation1 is ISimpleAssignmentOperation);

            Assert.Equal("(x, y, z) = new C()", assignments[1].ToString());
            IOperation operation2 = model.GetOperation(assignments[1]);
            Assert.NotNull(operation2);
            Assert.Equal(OperationKind.DeconstructionAssignment, operation2.Kind);
            Assert.False(operation2 is ISimpleAssignmentOperation);

            Assert.Equal("var (a, b) = (1, 2)", assignments[2].ToString());
            IOperation operation3 = model.GetOperation(assignments[2]);
            Assert.NotNull(operation3);
            Assert.Equal(OperationKind.DeconstructionAssignment, operation3.Kind);
            Assert.False(operation3 is ISimpleAssignmentOperation);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/45687")]
        public void TestClone()
        {
            var sourceCode = TestResource.AllInOneCSharpCode;

            var compilation = CreateCompilationWithMscorlib40(sourceCode, new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceFileName: "file.cs");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            VerifyClone(model);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(22964, "https://github.com/dotnet/roslyn/issues/22964")]
        [Fact]
        public void GlobalStatement_Parent()
        {
            var source =
@"
System.Console.WriteLine();
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var statement = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>().Single();
            var model = compilation.GetSemanticModel(tree);
            var operation = model.GetOperation(statement);

            Assert.Equal(OperationKind.ExpressionStatement, operation.Kind);
            Assert.Null(operation.Parent);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParentOperations()
        {
            var sourceCode = TestResource.AllInOneCSharpCode;

            var compilation = CreateCompilationWithMscorlib40(sourceCode, new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceFileName: "file.cs");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            VerifyParentOperations(model);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(23001, "https://github.com/dotnet/roslyn/issues/23001")]
        [Fact]
        public void TestGetOperationForQualifiedName()
        {
            var text = @"using System;

public class Test
{
    class A
    {
        public B b;
    }
    class B
    {
    }
    
    void M(A a)
    {
        int x2 = /*<bind>*/a.b/*</bind>*/;
    }
}
";
            var comp = CreateCompilation(text);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            // Verify we return non-null operation only for topmost member access expression.
            var expr = (MemberAccessExpressionSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal("a.b", expr.ToString());
            var operation = model.GetOperation(expr);
            Assert.NotNull(operation);
            Assert.Equal(OperationKind.FieldReference, operation.Kind);
            var fieldOperation = (IFieldReferenceOperation)operation;
            Assert.Equal("b", fieldOperation.Field.Name);

            // Verify we return null operation for child nodes of member access expression.
            Assert.Null(model.GetOperation(expr.Name));
        }

        [Fact]
        public void TestSemanticModelOnOperationAncestors()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M(bool flag)
  {
    if (flag)
    {
        int y = 1000;
    }
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var literal = root.DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
            var methodDeclSyntax = literal.Ancestors().OfType<MethodDeclarationSyntax>().Single();
            var model = compilation.GetSemanticModel(tree);
            IOperation operation = model.GetOperation(literal);
            VerifyRootAndModelForOperationAncestors(operation, model, expectedRootOperationKind: OperationKind.MethodBody, expectedRootSyntax: methodDeclSyntax);
        }

        [Fact]
        public void TestGetOperationOnSpeculativeSemanticModel()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var speculatedBlock = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0;
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, speculatedBlock, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var localDecl = (LocalDeclarationStatementSyntax)speculatedBlock.Statements[0];
            IOperation operation = speculativeModel.GetOperation(localDecl);
            VerifyRootAndModelForOperationAncestors(operation, speculativeModel, expectedRootOperationKind: OperationKind.Block, expectedRootSyntax: speculatedBlock);

            speculativeModel.VerifyOperationTree(localDecl, expectedOperationTree: @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int z = 0;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int z = 0')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 z) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'z = 0')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
    Initializer: 
      null
");
        }

        [Fact, WorkItem(26649, "https://github.com/dotnet/roslyn/issues/26649")]
        public void IncrementalBindingReusesBlock()
        {
            var source = @"
class C
{
    void M()
    {
        try
        {
        }
        catch (Exception e)
        {
            throw new Exception();
        }
    }
}";

            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // We want to get the IOperation for the { throw new Exception(); } first, and then for the containing catch block, to
            // force the semantic model to bind the inner first. It should reuse that inner block when binding the outer catch.

            var catchBlock = syntaxTree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>().Single();
            var exceptionBlock = catchBlock.Block;

            var blockOperation = semanticModel.GetOperation(exceptionBlock);
            var catchOperation = (ICatchClauseOperation)semanticModel.GetOperation(catchBlock);
            Assert.Same(blockOperation, catchOperation.Handler);
        }

        private static void VerifyRootAndModelForOperationAncestors(
            IOperation operation,
            SemanticModel model,
            OperationKind expectedRootOperationKind,
            SyntaxNode expectedRootSyntax)
        {
            SemanticModel memberModel = ((Operation)operation).OwningSemanticModel;
            while (true)
            {
                Assert.Same(model, operation.SemanticModel);
                Assert.Same(memberModel, ((Operation)operation).OwningSemanticModel);

                if (operation.Parent == null)
                {
                    Assert.Equal(expectedRootOperationKind, operation.Kind);
                    Assert.Same(expectedRootSyntax, operation.Syntax);
                    break;
                }

                operation = operation.Parent;
            }
        }

        [ConditionalFact(typeof(NoIOperationValidation)), WorkItem(45955, "https://github.com/dotnet/roslyn/issues/45955")]
        public void SemanticModelFieldInitializerRace()
        {
            var source = $@"
#nullable enable
public class C
{{
    // Use a big initializer to increase the odds of hitting the race
    public static object o = null;
    public string s = {string.Join(" + ", Enumerable.Repeat("(string)o", 1000))};
}}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var fieldInitializer = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Last().Value;

            for (int i = 0; i < 5; i++)
            {
                // We had a race condition where the first attempt to access a field initializer could cause an assert to be hit,
                // and potentially more work to be done than was necessary. So we kick off a parallel task to attempt to
                // get info on a bunch of different threads at the same time and reproduce the issue.
                var model = comp.GetSemanticModel(tree);
                const int nTasks = 10;
                Enumerable.Range(0, nTasks).AsParallel()
                    .ForAll(_ => Assert.Equal("System.String System.String.op_Addition(System.String left, System.String right)", model.GetSymbolInfo(fieldInitializer).Symbol.ToTestDisplayString(includeNonNullable: false)));
            }
        }
    }
}
