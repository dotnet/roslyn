using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: null, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: null, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""input"", IsImplicit) (Syntax: '{ }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                      Initializers(0)
        WhenFalse: 
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
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
              Left: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """"input"""", IsImplicit) (Syntax: '{ }')
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: null, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: null, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""y"", IsImplicit) (Syntax: '{ }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                        Initializers(0)
        WhenFalse: 
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
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
          Left: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""y"", IsImplicit) (Syntax: '{ }')
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
    public void M(string name!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ...  name!) { }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: null, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: name (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: null, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""name"", IsImplicit) (Syntax: '{ }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                      Initializers(0)
        WhenFalse: 
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
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
          Left: 
            IParameterReferenceOperation: name (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""name"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)");
        }
    }
}
