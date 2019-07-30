// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NullCheckedMethodDeclarationIOp()
        {
            var source = @"
public class C
{
    public void M(string input!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... input!) { }')
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

        VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public void ... input!) { }')
              Left: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public void ... input!) { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public void ... input!) { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public void ... input!) { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public void ... input!) { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""input"", IsImplicit) (Syntax: 'public void ... input!) { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_OneNullCheckedManyParams()
        {
            var source = @"
public class C
{
    public void M(string x, string y!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... ing y!) { }')
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public void ... ing y!) { }')
              Left: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public void ... ing y!) { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public void ... ing y!) { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public void ... ing y!) { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public void ... ing y!) { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""y"", IsImplicit) (Syntax: 'public void ... ing y!) { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_OneNullCheckedParamWithStringOpt()
        {
            var source = @"
public class C
{
    public void M(string name! = ""rose"") { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... ""rose"") { }')
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public void ... ""rose"") { }')
              Left: 
                IParameterReferenceOperation: name (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public void ... ""rose"") { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public void ... ""rose"") { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public void ... ""rose"") { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public void ... ""rose"") { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""name"", IsImplicit) (Syntax: 'public void ... ""rose"") { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedOperator()
        {
            var source = @"
public class Box
{
    public static int operator+ (Box b!, Box c)  
    { 
        return 2;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public stat ... }')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return 2;')
            ReturnedValue: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public stat ... }')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: Box, IsImplicit) (Syntax: 'public stat ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: Box, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public stat ... }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public stat ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public stat ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""b"", IsImplicit) (Syntax: 'public stat ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact(Skip = "PROTOTYPE")]
        public void TestIOp_NullCheckedIndexedProperty()
        {
            // PROTOTYPE 
            var source = @"
public class C
{
    public string this[string index!] => null;
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> null')
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'null')
        ReturnedValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
        }

        [Fact]
        public void TestIOp_NullCheckedIndexedGetterSetter()
        {
            var source = @"
public class C
{
    private object[] items = {'h', ""hello""};
    public string this[object item!]
    {
        /*<bind>*/get
        {
            return items[0].ToString();
        }/*</bind>*/
        set
        {
            items[0] = value;
        }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'set ... }')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'items[0] = value;')
            Expression: 
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'items[0] = value')
                Left: 
                  IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Object) (Syntax: 'items[0]')
                    Array reference: 
                      IFieldReferenceOperation: System.Object[] C.items (OperationKind.FieldReference, Type: System.Object[]) (Syntax: 'items')
                        Instance Receiver: 
                          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'items')
                    Indices(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: value (OperationKind.ParameterReference, Type: System.String) (Syntax: 'value')
      ExpressionBody: 
        null");
            var expected = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'get ... }')
              Left: 
                IParameterReferenceOperation: item (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'get ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Object, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'get ... }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'get ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'get ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""item"", IsImplicit) (Syntax: 'get ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B4]
            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'items[0].ToString()')
              Instance Receiver: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Object) (Syntax: 'items[0]')
                  Array reference: 
                    IFieldReferenceOperation: System.Object[] C.items (OperationKind.FieldReference, Type: System.Object[]) (Syntax: 'items')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'items')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Arguments(0)
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)";
            VerifyFlowGraphAndDiagnosticsForTest<AccessorDeclarationSyntax>(source, expected, DiagnosticDescription.None);
        }

        [Fact]
        public void TestIOp_NullCheckedIndexedGetterExpression()
        {
            var source = @"
public class C
{
    private object[] items = {'h', ""hello""};
    public string this[object item!]
    {
        /*<bind>*/get => items[0].ToString();/*</bind>*/
        set
        {
            items[0] = value;
        }
    }
}";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'set ... }')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'items[0] = value;')
            Expression: 
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'items[0] = value')
                Left: 
                  IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Object) (Syntax: 'items[0]')
                    Array reference: 
                      IFieldReferenceOperation: System.Object[] C.items (OperationKind.FieldReference, Type: System.Object[]) (Syntax: 'items')
                        Instance Receiver: 
                          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'items')
                    Indices(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: value (OperationKind.ParameterReference, Type: System.String) (Syntax: 'value')
      ExpressionBody: 
        null");
            var expected = @"
        Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'get => item ... ToString();')
              Left: 
                IParameterReferenceOperation: item (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'get => item ... ToString();')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Object, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'get => item ... ToString();')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'get => item ... ToString();')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'get => item ... ToString();')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""item"", IsImplicit) (Syntax: 'get => item ... ToString();')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B4]
            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'items[0].ToString()')
              Instance Receiver: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Object) (Syntax: 'items[0]')
                  Array reference: 
                    IFieldReferenceOperation: System.Object[] C.items (OperationKind.FieldReference, Type: System.Object[]) (Syntax: 'items')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'items')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Arguments(0)
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<AccessorDeclarationSyntax>(source, expected, DiagnosticDescription.None);
        }

        [Fact]
        public void TestIOp_NullCheckedIndexedSetter()
        {
            var source = @"
public class C
{
    public string this[object item!] { /*<bind>*/set { }/*</bind>*/ }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'set { }')
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");
            var expected = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'set { }')
              Left: 
                IParameterReferenceOperation: item (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'set { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Object, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'set { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'set { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'set { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""item"", IsImplicit) (Syntax: 'set { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)";
            VerifyFlowGraphAndDiagnosticsForTest<AccessorDeclarationSyntax>(source, expected, DiagnosticDescription.None);
        }

        [Fact]
        public void TestIOp_NullCheckedLambda()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x! => x;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... }')
      BlockBody: 
        IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          Locals: Local_1: System.Func<System.String, System.String> func1
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Func<string ...  = x! => x;')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Func<string ... 1 = x! => x')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: System.Func<System.String, System.String> func1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'func1 = x! => x')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= x! => x')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'x! => x')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'x! => x')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                  ReturnedValue: 
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
              Initializer: 
                null
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Func<System.String, System.String> func1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'func1 = x! => x')
              Left: 
                ILocalReferenceOperation: func1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'func1 = x! => x')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'x! => x')
                    Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'x! => x')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (0)
                            Jump if False (Regular) to Block[B3#A0]
                                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'x! => x')
                                    Left: 
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'x! => x')
                                    Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'x! => x')
                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Block
                            Predecessors: [B1#A0]
                            Statements (0)
                            Next (Throw) Block[null]
                                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'x! => x')
                                    Arguments(1):
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x! => x')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'x! => x')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Initializer: 
                                    null
                        Block[B3#A0] - Block
                            Predecessors: [B1#A0]
                            Statements (0)
                            Next (Return) Block[B4#A0]
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                        Block[B4#A0] - Exit
                            Predecessors: [B3#A0]
                            Statements (0)
                    }
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedInLambdaWithManyParameters()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (x!, y) => x;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... }')
      BlockBody: 
        IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          Locals: Local_1: System.Func<System.String, System.String, System.String> func1
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Func<string ... !, y) => x;')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Func<string ... x!, y) => x')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: System.Func<System.String, System.String, System.String> func1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'func1 = (x!, y) => x')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (x!, y) => x')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String, System.String>, IsImplicit) (Syntax: '(x!, y) => x')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '(x!, y) => x')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                  ReturnedValue: 
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
              Initializer: 
                null
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Func<System.String, System.String, System.String> func1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.String, System.String, System.String>, IsImplicit) (Syntax: 'func1 = (x!, y) => x')
                  Left: 
                    ILocalReferenceOperation: func1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Func<System.String, System.String, System.String>, IsImplicit) (Syntax: 'func1 = (x!, y) => x')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String, System.String>, IsImplicit) (Syntax: '(x!, y) => x')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '(x!, y) => x')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Jump if False (Regular) to Block[B3#A0]
                                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '(x!, y) => x')
                                      Left: 
                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '(x!, y) => x')
                                      Right: 
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '(x!, y) => x')
                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Block
                                Predecessors: [B1#A0]
                                Statements (0)
                                Next (Throw) Block[null]
                                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '(x!, y) => x')
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '(x!, y) => x')
                                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '(x!, y) => x')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Initializer: 
                                        null
                            Block[B3#A0] - Block
                                Predecessors: [B1#A0]
                                Statements (0)
                                Next (Return) Block[B4#A0]
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                            Block[B4#A0] - Exit
                                Predecessors: [B3#A0]
                                Statements (0)
                        }
            Next (Regular) Block[B2]
                Leaving: {R1}
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedUnnamedVariableInLambda()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = _! => null;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... }')
      BlockBody: 
        IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          Locals: Local_1: System.Func<System.String, System.String> func1
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Func<string ... _! => null;')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Func<string ...  _! => null')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: System.Func<System.String, System.String> func1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'func1 = _! => null')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= _! => null')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: '_! => null')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '_! => null')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'null')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'null')
                                  ReturnedValue: 
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                      Operand: 
                                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
              Initializer: 
                null
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Func<System.String, System.String> func1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'func1 = _! => null')
                  Left: 
                    ILocalReferenceOperation: func1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'func1 = _! => null')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: '_! => null')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '_! => null')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Jump if False (Regular) to Block[B3#A0]
                                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '_! => null')
                                      Left: 
                                        IParameterReferenceOperation: _ (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '_! => null')
                                      Right: 
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '_! => null')
                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Block
                                Predecessors: [B1#A0]
                                Statements (0)
                                Next (Throw) Block[null]
                                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '_! => null')
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '_! => null')
                                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""_"", IsImplicit) (Syntax: '_! => null')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Initializer: 
                                        null
                            Block[B3#A0] - Block
                                Predecessors: [B1#A0]
                                Statements (0)
                                Next (Return) Block[B4#A0]
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        (ImplicitReference)
                                      Operand: 
                                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                            Block[B4#A0] - Exit
                                Predecessors: [B3#A0]
                                Statements (0)
                        }
            Next (Regular) Block[B2]
                Leaving: {R1}
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedTwoExpressionBodyLambdas()
        {
            var source = @"
using System;
class C
{
    public Func<string, string> M(string s1!) => s2! => s2 + s1;
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public Func ... => s2 + s1;')
      BlockBody: 
        null
      ExpressionBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> s2! => s2 + s1')
          IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2! => s2 + s1')
            ReturnedValue: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 's2! => s2 + s1')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 's2! => s2 + s1')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2 + s1')
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2 + s1')
                        ReturnedValue: 
                          IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 's2 + s1')
                            Left: 
                              IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's2')
                            Right: 
                              IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public Func ... => s2 + s1;')
              Left: 
                IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public Func ... => s2 + s1;')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public Func ... => s2 + s1;')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public Func ... => s2 + s1;')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public Func ... => s2 + s1;')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s1"", IsImplicit) (Syntax: 'public Func ... => s2 + s1;')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B4]
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 's2! => s2 + s1')
              Target: 
                IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 's2! => s2 + s1')
                {
                    Block[B0#A0] - Entry
                        Statements (0)
                        Next (Regular) Block[B1#A0]
                    Block[B1#A0] - Block
                        Predecessors: [B0#A0]
                        Statements (0)
                        Jump if False (Regular) to Block[B3#A0]
                            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 's2! => s2 + s1')
                              Left: 
                                IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 's2! => s2 + s1')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 's2! => s2 + s1')
                        Next (Regular) Block[B2#A0]
                    Block[B2#A0] - Block
                        Predecessors: [B1#A0]
                        Statements (0)
                        Next (Throw) Block[null]
                            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 's2! => s2 + s1')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2! => s2 + s1')
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s2"", IsImplicit) (Syntax: 's2! => s2 + s1')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Initializer: 
                                null
                    Block[B3#A0] - Block
                        Predecessors: [B1#A0]
                        Statements (0)
                        Next (Return) Block[B4#A0]
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 's2 + s1')
                              Left: 
                                IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's2')
                              Right: 
                                IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')
                    Block[B4#A0] - Exit
                        Predecessors: [B3#A0]
                        Statements (0)
                }
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedLambdaInField()
        {
            var source = @"
using System;
class C
{
    Func<string, string> func1 = x! => x;
    public C()
    {
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single();
            var node2 = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'x! => x')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
          ReturnedValue: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')");

            VerifyFlowGraph(compilation, node2, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: '= x! => x')
              Left: 
                IFieldReferenceOperation: System.Func<System.String, System.String> C.func1 (OperationKind.FieldReference, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: '= x! => x')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= x! => x')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'x! => x')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'x! => x')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (0)
                            Jump if False (Regular) to Block[B3#A0]
                                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'x! => x')
                                  Left: 
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'x! => x')
                                  Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'x! => x')
                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Block
                            Predecessors: [B1#A0]
                            Statements (0)
                            Next (Throw) Block[null]
                                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'x! => x')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x! => x')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'x! => x')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Initializer: 
                                    null
                        Block[B3#A0] - Block
                            Predecessors: [B1#A0]
                            Statements (0)
                            Next (Return) Block[B4#A0]
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                        Block[B4#A0] - Exit
                            Predecessors: [B3#A0]
                            Statements (0)
                    }
        Next (Regular) Block[B2]
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)
");
        }

        [Fact]
        public void TestIOp_NullCheckedLocalFunction()
        {
            var source = @"
class C
{
    public void M()
    {
        InnerM(""hello world"");
        void InnerM(string x!) { }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var node2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    ILocalFunctionOperation (Symbol: void InnerM(System.String x)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ing x!) { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue: 
            null");
            VerifyFlowGraph(compilation, node2, @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Methods: [void InnerM(System.String x)]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'InnerM(""hello world"");')
                  Expression: 
                    IInvocationOperation (void InnerM(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'InnerM(""hello world"")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '""hello world""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello world"") (Syntax: '""hello world""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R1}
        
        {   void InnerM(System.String x)
        
            Block[B0#0R1] - Entry
                Statements (0)
                Next (Regular) Block[B1#0R1]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (0)
                Jump if False (Regular) to Block[B3#0R1]
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Next (Regular) Block[B2#0R1]
            Block[B2#0R1] - Block
                Predecessors: [B1#0R1]
                Statements (0)
                Next (Throw) Block[null]
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null
            Block[B3#0R1] - Exit
                Predecessors: [B1#0R1]
                Statements (0)
        }
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedManyParamsInLocalFunction()
        {
            var source = @"
class C
{
    public void M()
    {
        InnerM(""hello"",  ""world"");
        void InnerM(string x!, string y!) { }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var node2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    ILocalFunctionOperation (Symbol: void InnerM(System.String x, System.String y)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ing y!) { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue: 
            null");

            VerifyFlowGraph(compilation, node2, @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Methods: [void InnerM(System.String x, System.String y)]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'InnerM(""hel ...   ""world"");')
                  Expression: 
                    IInvocationOperation (void InnerM(System.String x, System.String y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'InnerM(""hel ... ,  ""world"")')
                      Instance Receiver: 
                        null
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '""hello""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: '""world""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""world"") (Syntax: '""world""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R1}
        
        {   void InnerM(System.String x, System.String y)
        
            Block[B0#0R1] - Entry
                Statements (0)
                Next (Regular) Block[B1#0R1]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (0)
                Jump if False (Regular) to Block[B3#0R1]
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Next (Regular) Block[B2#0R1]
            Block[B2#0R1] - Block
                Predecessors: [B1#0R1]
                Statements (0)
                Next (Throw) Block[null]
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null
            Block[B3#0R1] - Block
                Predecessors: [B1#0R1]
                Statements (0)
                Jump if False (Regular) to Block[B5#0R1]
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Left: 
                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Next (Regular) Block[B4#0R1]
            Block[B4#0R1] - Block
                Predecessors: [B3#0R1]
                Statements (0)
                Next (Throw) Block[null]
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""y"", IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null
            Block[B5#0R1] - Exit
                Predecessors: [B3#0R1]
                Statements (0)
        }
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_OuterNullCheckedShadowedParameter()
        {
            var source = @"
class C
{
    public void M(string x!)
    {
        InnerM(""hello"");
        void InnerM(string x) { }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... }')
      BlockBody: 
        IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'InnerM(""hello"");')
            Expression: 
              IInvocationOperation (void InnerM(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'InnerM(""hello"")')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '""hello""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ILocalFunctionOperation (Symbol: void InnerM(System.String x)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ring x) { }')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
                ReturnedValue: 
                  null
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
   Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public void ... }')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public void ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public void ... }')
            Entering: {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public void ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public void ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'public void ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    .locals {R1}
    {
        Methods: [void InnerM(System.String x)]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'InnerM(""hello"");')
                  Expression: 
                    IInvocationOperation (void InnerM(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'InnerM(""hello"")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '""hello""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B4]
                Leaving: {R1}
        
        {   void InnerM(System.String x)
        
            Block[B0#0R1] - Entry
                Statements (0)
                Next (Regular) Block[B1#0R1]
            Block[B1#0R1] - Exit
                Predecessors: [B0#0R1]
                Statements (0)
        }
    }
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_InnerNullCheckedShadowedParameter()
        {
            var source = @"
class C
{
    public void M(string x)
    {
        InnerM(""hello"");
        void InnerM(string x!) { }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var node2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    ILocalFunctionOperation (Symbol: void InnerM(System.String x)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ing x!) { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue: 
            null");
            VerifyFlowGraph(compilation, node2, @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Methods: [void InnerM(System.String x)]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'InnerM(""hello"");')
                  Expression: 
                    IInvocationOperation (void InnerM(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'InnerM(""hello"")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '""hello""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R1}
        
        {   void InnerM(System.String x)
        
            Block[B0#0R1] - Entry
                Statements (0)
                Next (Regular) Block[B1#0R1]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (0)
                Jump if False (Regular) to Block[B3#0R1]
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Next (Regular) Block[B2#0R1]
            Block[B2#0R1] - Block
                Predecessors: [B1#0R1]
                Statements (0)
                Next (Throw) Block[null]
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null
            Block[B3#0R1] - Exit
                Predecessors: [B1#0R1]
                Statements (0)
        }
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedConstructor()
        {
            var source = @"
class C
{
    public C(string x!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public C(string x!) { }')
      Initializer: 
        null
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public C(string x!) { }')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public C(string x!) { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public C(string x!) { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public C(string x!) { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public C(string x!) { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'public C(string x!) { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedConstructorWithThisChain()
        {
            var source = @"
class C
{
    public C() { }
    public C(string x!) : this() { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public C(st ...  this() { }')
      Initializer: 
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
          Expression: 
            IInvocationOperation ( C..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this()')
              Arguments(0)
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public C(st ...  this() { }')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public C(st ...  this() { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public C(st ...  this() { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public C(st ...  this() { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public C(st ...  this() { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'public C(st ...  this() { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
              Expression: 
                IInvocationOperation ( C..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this()')
                  Arguments(0)
        Next (Regular) Block[B4]
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedConstructorWithBaseChain()
        {
            var source = @"
class B
{
    public B(string y) { }
}
class C : B
{
    public C(string x!) : base(x) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public C(st ... base(x) { }')
      Initializer: 
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(x)')
          Expression: 
            IInvocationOperation ( B..ctor(System.String y)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(x)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(x)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'x')
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public C(st ... base(x) { }')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public C(st ... base(x) { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public C(st ... base(x) { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public C(st ... base(x) { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public C(st ... base(x) { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'public C(st ... base(x) { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(x)')
              Expression: 
                IInvocationOperation ( B..ctor(System.String y)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(x)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(x)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'x')
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedConstructorWithFieldInitializers()
        {
            var source = @"
class C
{
    int y = 5;
    public C(string x!) { y++; }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public C(st ... !) { y++; }')
      Initializer: 
        null
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ y++; }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y++;')
            Expression: 
              IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'y++')
                Target: 
                  IFieldReferenceOperation: System.Int32 C.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'y')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public C(st ... !) { y++; }')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public C(st ... !) { y++; }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public C(st ... !) { y++; }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public C(st ... !) { y++; }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public C(st ... !) { y++; }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'public C(st ... !) { y++; }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y++;')
              Expression: 
                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'y++')
                  Target: 
                    IFieldReferenceOperation: System.Int32 C.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'y')
        Next (Regular) Block[B4]
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedExpressionBodyMethod()
        {
            var source = @"
class C
{
    object Local(object arg!) => arg;
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'object Loca ... g!) => arg;')
      BlockBody: 
        null
      ExpressionBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> arg')
          IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'arg')
            ReturnedValue: 
              IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'arg')");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'object Loca ... g!) => arg;')
              Left: 
                IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'object Loca ... g!) => arg;')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Object, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'object Loca ... g!) => arg;')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'object Loca ... g!) => arg;')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'object Loca ... g!) => arg;')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""arg"", IsImplicit) (Syntax: 'object Loca ... g!) => arg;')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B4]
            IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'arg')
    Block[B4] - Exit
        Predecessors: [B3]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedIterator()
        {
            var source = @"
using System.Collections.Generic;
class C
{
    IEnumerable<char> GetChars(string s!)
    {
        foreach (var c in s)
        {
            yield return c;
        }
    }
    public static void Main()
    {
        C c = new C();
        IEnumerable<char> e = c.GetChars(""hello"");
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(0);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'IEnumerable ... }')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
            Locals: Local_1: System.Char c
            LoopControlVariable: 
              IVariableDeclaratorOperation (Symbol: System.Char c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
                Initializer: 
                  null
            Collection: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 's')
                Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
            Body: 
              IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return c;')
                  ReturnedValue: 
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
            NextVariables(0)
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'IEnumerable ... }')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'IEnumerable ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'IEnumerable ... }')
            Entering: {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'IEnumerable ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'IEnumerable ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: 'IEnumerable ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    .locals {R1}
    {
        CaptureIds: [0]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's')
                  Value: 
                    IInvocationOperation ( System.CharEnumerator System.String.GetEnumerator()) (OperationKind.Invocation, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 's')
                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Identity)
                          Operand: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
                      Arguments(0)
            Next (Regular) Block[B4]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B4] - Block
                Predecessors: [B3] [B5]
                Statements (0)
                Jump if False (Regular) to Block[B9]
                    IInvocationOperation ( System.Boolean System.CharEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 's')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                      Arguments(0)
                    Finalizing: {R5}
                    Leaving: {R3} {R2} {R1}
                Next (Regular) Block[B5]
                    Entering: {R4}
            .locals {R4}
            {
                Locals: [System.Char c]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (2)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                          Left: 
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, IsImplicit) (Syntax: 'var')
                          Right: 
                            IPropertyReferenceOperation: System.Char System.CharEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'var')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                        IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return c;')
                          ReturnedValue: 
                            ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }
        }
        .finally {R5}
        {
            Block[B6] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B8]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 's')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 's')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 's')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                      Arguments(0)
                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B6] [B7]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B9] - Exit
        Predecessors: [B4]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedIteratorInLocalFunction()
        {
            var source = @"
using System.Collections.Generic;
class Iterators
{
    void Use()
    {
        IEnumerable<char> e = GetChars(""hello"");
        IEnumerable<char> GetChars(string s!)
        {
            foreach (var c in s)
            {
                yield return c;
            }
        }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var node2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    ILocalFunctionOperation (Symbol: System.Collections.Generic.IEnumerable<System.Char> GetChars(System.String s)) (OperationKind.LocalFunction, Type: null) (Syntax: 'IEnumerable ... }')
      IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
          Locals: Local_1: System.Char c
          LoopControlVariable: 
            IVariableDeclaratorOperation (Symbol: System.Char c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
              Initializer: 
                null
          Collection: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 's')
              Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
          Body: 
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
              IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return c;')
                ReturnedValue: 
                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
          NextVariables(0)
        IReturnOperation (OperationKind.YieldBreak, Type: null, IsImplicit) (Syntax: '{ ... }')
          ReturnedValue: 
            null");

            VerifyFlowGraph(compilation, node2, @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Collections.Generic.IEnumerable<System.Char> e]
        Methods: [System.Collections.Generic.IEnumerable<System.Char> GetChars(System.String s)]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable<System.Char>, IsImplicit) (Syntax: 'e = GetChars(""hello"")')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.IEnumerable<System.Char>, IsImplicit) (Syntax: 'e = GetChars(""hello"")')
                  Right: 
                    IInvocationOperation (System.Collections.Generic.IEnumerable<System.Char> GetChars(System.String s)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.Char>) (Syntax: 'GetChars(""hello"")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""hello""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R1}
        
        {   System.Collections.Generic.IEnumerable<System.Char> GetChars(System.String s)
        
            Block[B0#0R1] - Entry
                Statements (0)
                Next (Regular) Block[B1#0R1]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (0)
                Jump if False (Regular) to Block[B3#0R1]
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'IEnumerable ... }')
                      Left: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'IEnumerable ... }')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'IEnumerable ... }')
                    Entering: {R1#0R1}
                Next (Regular) Block[B2#0R1]
            Block[B2#0R1] - Block
                Predecessors: [B1#0R1]
                Statements (0)
                Next (Throw) Block[null]
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'IEnumerable ... }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'IEnumerable ... }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: 'IEnumerable ... }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null
            .locals {R1#0R1}
            {
                CaptureIds: [0]
                Block[B3#0R1] - Block
                    Predecessors: [B1#0R1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's')
                          Value: 
                            IInvocationOperation ( System.CharEnumerator System.String.GetEnumerator()) (OperationKind.Invocation, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                              Instance Receiver: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 's')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (Identity)
                                  Operand: 
                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
                              Arguments(0)
                    Next (Regular) Block[B4#0R1]
                        Entering: {R2#0R1} {R3#0R1}
                .try {R2#0R1, R3#0R1}
                {
                    Block[B4#0R1] - Block
                        Predecessors: [B3#0R1] [B5#0R1]
                        Statements (0)
                        Jump if False (Regular) to Block[B9#0R1]
                            IInvocationOperation ( System.Boolean System.CharEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 's')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                              Arguments(0)
                            Finalizing: {R5#0R1}
                            Leaving: {R3#0R1} {R2#0R1} {R1#0R1}
                        Next (Regular) Block[B5#0R1]
                            Entering: {R4#0R1}
                    .locals {R4#0R1}
                    {
                        Locals: [System.Char c]
                        Block[B5#0R1] - Block
                            Predecessors: [B4#0R1]
                            Statements (2)
                                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                                  Left: 
                                    ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, IsImplicit) (Syntax: 'var')
                                  Right: 
                                    IPropertyReferenceOperation: System.Char System.CharEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'var')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                                IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return c;')
                                  ReturnedValue: 
                                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
                            Next (Regular) Block[B4#0R1]
                                Leaving: {R4#0R1}
                    }
                }
                .finally {R5#0R1}
                {
                    Block[B6#0R1] - Block
                        Predecessors (0)
                        Statements (0)
                        Jump if True (Regular) to Block[B8#0R1]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 's')
                              Operand: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                        Next (Regular) Block[B7#0R1]
                    Block[B7#0R1] - Block
                        Predecessors: [B6#0R1]
                        Statements (1)
                            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 's')
                              Instance Receiver: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 's')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 's')
                              Arguments(0)
                        Next (Regular) Block[B8#0R1]
                    Block[B8#0R1] - Block
                        Predecessors: [B6#0R1] [B7#0R1]
                        Statements (0)
                        Next (StructuredExceptionHandling) Block[null]
                }
            }
            Block[B9#0R1] - Exit
                Predecessors: [B4#0R1]
                Statements (0)
        }
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedEmptyIterator()
        {
            var source = @"
using System.Collections.Generic;
class C
{
    public static void Main() { }
    static IEnumerable<char> GetChars(string s!)
    {
        yield break;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'static IEnu ... }')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IReturnOperation (OperationKind.YieldBreak, Type: null) (Syntax: 'yield break;')
            ReturnedValue: 
              null
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'static IEnu ... }')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'static IEnu ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'static IEnu ... }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'static IEnu ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'static IEnu ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: 'static IEnu ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedEmptyIteratorReturningIEnumerator()
        {
            var source = @"
using System.Collections.Generic;
class C
{
    public static void Main() { }
    static IEnumerator<char> GetChars(string s!)
    {
        yield break;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'static IEnu ... }')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IReturnOperation (OperationKind.YieldBreak, Type: null) (Syntax: 'yield break;')
            ReturnedValue: 
              null
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'static IEnu ... }')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'static IEnu ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'static IEnu ... }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'static IEnu ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'static IEnu ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: 'static IEnu ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestNullCheckedLambdaWithMissingType()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Func<string, string> func = x! => x;
    }
}

";
            var comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_ArgumentNullException__ctorString);
            comp.MakeTypeMissing(WellKnownType.System_ArgumentNullException);
            comp.VerifyDiagnostics(
                    // (7,37): error CS0656: Missing compiler required member 'System.ArgumentNullException..ctor'
                    //         Func<string, string> func = x! => x;
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.ArgumentNullException", ".ctor").WithLocation(7, 37));
            var tree = comp.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            comp.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null, IsInvalid) (Syntax: 'public stat ... }')
      BlockBody: 
        IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
          Locals: Local_1: System.Func<System.String, System.String> func
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Func<string ...  = x! => x;')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Func<string ... c = x! => x')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: System.Func<System.String, System.String> func) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'func = x! => x')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= x! => x')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsInvalid, IsImplicit) (Syntax: 'x! => x')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'x! => x')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                  ReturnedValue: 
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
              Initializer: 
                null
      ExpressionBody: 
        null");
            VerifyFlowGraph(comp, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Func<System.String, System.String> func]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.String, System.String>, IsInvalid, IsImplicit) (Syntax: 'func = x! => x')
                  Left: 
                    ILocalReferenceOperation: func (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Func<System.String, System.String>, IsInvalid, IsImplicit) (Syntax: 'func = x! => x')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsInvalid, IsImplicit) (Syntax: 'x! => x')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'x! => x')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Jump if False (Regular) to Block[B3#A0]
                                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x! => x')
                                      Left: 
                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'x! => x')
                                      Right: 
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsInvalid, IsImplicit) (Syntax: 'x! => x')
                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Block
                                Predecessors: [B1#A0]
                                Statements (0)
                                Next (Throw) Block[null]
                                    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'x! => x')
                                      Children(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsInvalid, IsImplicit) (Syntax: 'x! => x')
                            Block[B3#A0] - Block
                                Predecessors: [B1#A0]
                                Statements (0)
                                Next (Return) Block[B4#A0]
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                            Block[B4#A0] - Exit
                                Predecessors: [B3#A0]
                                Statements (0)
                        }
            Next (Regular) Block[B2]
                Leaving: {R1}
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestNullCheckedLocalFunctionWithMissingType()
        {
            var source =
@"
class Program
{
    public static void Main()
    {
        M(""ok"");
        void M(string x!) { }
    }
}";
            var comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_ArgumentNullException__ctorString);
            comp.MakeTypeMissing(WellKnownType.System_ArgumentNullException);
            comp.VerifyDiagnostics(
                    // (7,23): error CS0656: Missing compiler required member 'System.ArgumentNullException..ctor'
                    //         void M(string x!) { }
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.ArgumentNullException", ".ctor").WithLocation(7, 23));
            var tree = comp.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var node2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            comp.VerifyOperationTree(node1, expectedOperationTree: @"
    ILocalFunctionOperation (Symbol: void M(System.String x)) (OperationKind.LocalFunction, Type: null, IsInvalid) (Syntax: 'void M(string x!) { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue: 
            null");
            VerifyFlowGraph(comp, node2, @"
      Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Methods: [void M(System.String x)]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M(""ok"");')
                  Expression: 
                    IInvocationOperation (void M(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M(""ok"")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '""ok""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""ok"") (Syntax: '""ok""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R1}
        
        {   void M(System.String x)
        
            Block[B0#0R1] - Entry
                Statements (0)
                Next (Regular) Block[B1#0R1]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (0)
                Jump if False (Regular) to Block[B3#0R1]
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'void M(string x!) { }')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'void M(string x!) { }')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsInvalid, IsImplicit) (Syntax: 'void M(string x!) { }')
                Next (Regular) Block[B2#0R1]
            Block[B2#0R1] - Block
                Predecessors: [B1#0R1]
                Statements (0)
                Next (Throw) Block[null]
                    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'void M(string x!) { }')
                      Children(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsInvalid, IsImplicit) (Syntax: 'void M(string x!) { }')
            Block[B3#0R1] - Exit
                Predecessors: [B1#0R1]
                Statements (0)
        }
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestNullCheckedMethodWithMissingHasValue()
        {
            var source =
@"
class Program
{
    public void Method(int? x!) { }
}";
            var comp = CreateCompilation(source);
            comp.MakeMemberMissing(SpecialMember.System_Nullable_T_get_HasValue);
            comp.VerifyDiagnostics(
                    // (4,29): warning CS8721: Nullable value type 'int?' is null-checked and will throw if null.
                    //     public void Method(int? x!) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableValueType, "x").WithArguments("int?").WithLocation(4, 29));
            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            comp.VerifyOperationTree(node, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... nt? x!) { }')
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");
            VerifyFlowGraph(comp, node, @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if True (Regular) to Block[B3]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'public void ... nt? x!) { }')
              Children(1):
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?, IsImplicit) (Syntax: 'public void ... nt? x!) { }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public void ... nt? x!) { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public void ... nt? x!) { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'public void ... nt? x!) { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact(Skip = "PROTOTYPE")]
        public void TestNoNullChecksInBlockOperation()
        {
            // PROTOTYPE - Nullchecks currently included when only BlockSyntax is bound. Falls into VisitMethodBody instead of VisitBlock.
            var source = @"
public class C
{
    public void M(string input!) 
            /*<bind>*/{ }/*</bind>*/
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
");
            var output = @"";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedFlowGraph: output, DiagnosticDescription.None);
        }

        [Fact]
        public void TestNullCheckedBaseCallOrdering()
        {
            var source = @"
public class B
{
    public B(string x) { }
}
public class C : B
{
    public C(string param!) : base(param ?? """") { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'public C(st ...  ?? """") { }')
      Initializer: 
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(param ?? """")')
          Expression: 
            IInvocationOperation ( B..ctor(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(param ?? """")')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(param ?? """")')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'param ?? """"')
                    ICoalesceOperation (OperationKind.Coalesce, Type: System.String) (Syntax: 'param ?? """"')
                      Expression: 
                        IParameterReferenceOperation: param (OperationKind.ParameterReference, Type: System.String) (Syntax: 'param')
                      ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      WhenNull: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      BlockBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'public C(st ...  ?? """") { }')
              Left: 
                IParameterReferenceOperation: param (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'public C(st ...  ?? """") { }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'public C(st ...  ?? """") { }')
            Entering: {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'public C(st ...  ?? """") { }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'public C(st ...  ?? """") { }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""param"", IsImplicit) (Syntax: 'public C(st ...  ?? """") { }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    .locals {R1}
    {
        CaptureIds: [0] [2]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ': base(param ?? """")')
                  Value: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(param ?? """")')
            Next (Regular) Block[B4]
                Entering: {R2}
        .locals {R2}
        {
            CaptureIds: [1]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'param')
                      Value: 
                        IParameterReferenceOperation: param (OperationKind.ParameterReference, Type: System.String) (Syntax: 'param')
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'param')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'param')
                    Leaving: {R2}
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'param')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'param')
                Next (Regular) Block[B7]
                    Leaving: {R2}
        }
        Block[B6] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '""""')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(param ?? """")')
                  Expression: 
                    IInvocationOperation ( B..ctor(System.String x)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(param ?? """")')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: ': base(param ?? """")')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'param ?? """"')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'param ?? """"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B8]
                Leaving: {R1}
    }
    Block[B8] - Exit
        Predecessors: [B7]
        Statements (0)");
        }
    }
}
