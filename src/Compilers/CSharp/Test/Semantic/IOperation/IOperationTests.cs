// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public partial class IOperationTests : SemanticModelTestBase
    {
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
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '"""".Length == 0')
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
      IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'cur != null')
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
        [Fact]
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
    }
}
