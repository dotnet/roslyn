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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
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
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""input"", IsImplicit) (Syntax: '{ }')
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
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
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
    public void M(string name! = ""rose"") { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... ""rose"") { }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: name (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
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
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: Box, IsImplicit) (Syntax: '{ ... }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: Box, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ ... }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""b"", IsImplicit) (Syntax: '{ ... }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                      Initializers(0)
        WhenFalse: 
          null
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
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
          Left: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: Box, IsImplicit) (Syntax: '{ ... }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: Box, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""b"", IsImplicit) (Syntax: '{ ... }')
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

        [Fact]
        public void TestIOp_NullCheckedIndexedProperty()
        {
            var source = @"
public class C
{
    public string this[string index!] => null;
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<IndexerDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

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
        get
        {
            return items[0].ToString();
        }
        set
        {
            items[0] = value;
        }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
        }

        [Fact]
        public void TestIOp_NullCheckedIndexedSetter()
        {
            var source = @"
public class C
{
    public string this[object item!] { set { } }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
        }

        [Fact]
        public void TestIOp_NullCheckedSubstitution()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B1<T> : A<T> where T : class
{
    internal override void M<U>(U u!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'internal ov ... >(U u!) { }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
                IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsImplicit) (Syntax: '{ }')
            Right: 
                ILiteralOperation (OperationKind.Literal, Type: U, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                    Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""u"", IsImplicit) (Syntax: '{ }')
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
            IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsImplicit) (Syntax: '{ }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: U, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""u"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedSubstitution2()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B2 : A<object>
{
    internal override void M<U>(U u!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'internal ov ... >(U u!) { }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
                IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsImplicit) (Syntax: '{ }')
            Right: 
                ILiteralOperation (OperationKind.Literal, Type: U, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                    Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""u"", IsImplicit) (Syntax: '{ }')
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
            IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsImplicit) (Syntax: '{ }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: U, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""u"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedSubstitution3()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B3<T> : A<T?> where T : struct
{
    internal override void M<U>(U u!) { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                    // (8,35): warning CS8721: Nullable value type 'U' is null-checked and will throw if null.
                    //     internal override void M<U>(U u!) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableValueType, "u").WithArguments("U").WithLocation(8, 35));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1);
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'internal ov ... >(U u!) { }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
                IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsImplicit) (Syntax: '{ }')
            Right: 
                ILiteralOperation (OperationKind.Literal, Type: U, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                    Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""u"", IsImplicit) (Syntax: '{ }')
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
            IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsImplicit) (Syntax: '{ }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: U, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""u"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)");
        }

        [Fact]
        public void TestIOp_NotNullGenericIsNullChecked()
        {
            var source = @"
class C
{
    void M<T>(T value!) where T : notnull { }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'void M<T>(T ... notnull { }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: value (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: T, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""value"", IsImplicit) (Syntax: '{ }')
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
            IParameterReferenceOperation: value (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: '{ }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: T, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""value"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)");
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
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
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
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
        }

        [Fact]
        public void TestIOp_NullCheckedParametersInLambda()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (x!, y!) => x;
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
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
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
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
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
        }

        [Fact]
        public void TestIOp_NullCheckedLambdaInField()
        {
            var source = @"
using System;
class C
{
    Func<string, string> func1 = x! => x;
    public void M()
    {
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"");
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
            var node2 = (IMethodBodyOperation)compilation.GetSemanticModel(tree).GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single());
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
ILocalFunctionOperation (Symbol: void InnerM(System.String x)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ing x!) { }')
      Body: 
        IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                          Initializers(0)
            WhenFalse: 
              null
          IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      IgnoredBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')");

            var graph = ControlFlowGraph.Create(node2);
            Assert.NotNull(graph);
            Assert.Null(graph.Parent);

            var localFunc = graph.LocalFunctions.Single();
            Assert.NotNull(localFunc);
            Assert.Equal("InnerM", localFunc.Name);

            var graph_InnerM_FromExtension = graph.GetLocalFunctionControlFlowGraphInScope(localFunc);
            Assert.NotNull(graph_InnerM_FromExtension);
            Assert.Same(graph, graph_InnerM_FromExtension.Parent);

            var graph_InnerM = graph.GetLocalFunctionControlFlowGraph(localFunc);
            Assert.NotNull(graph_InnerM);
            Assert.Same(graph_InnerM_FromExtension, graph_InnerM);
        }

        [Fact]
        public void TestIOp_NullCheckedLocalFunctionWithManyParams()
        {
            var source = @"
class C
{
    public void M()
    {
        InnerM(""hello"",  ""world"");
        void InnerM(string x!, string y) { }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var node2 = (IMethodBodyOperation)compilation.GetSemanticModel(tree).GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single());
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    ILocalFunctionOperation (Symbol: void InnerM(System.String x, System.String y)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ring y) { }')
      Body: 
        IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')
                          Initializers(0)
            WhenFalse: 
              null
          IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      IgnoredBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ring y) { }')");

            var graph = ControlFlowGraph.Create(node2);
            Assert.NotNull(graph);
            Assert.Null(graph.Parent);

            var localFunc = graph.LocalFunctions.Single();
            Assert.NotNull(localFunc);
            Assert.Equal("InnerM", localFunc.Name);

            var graph_InnerM_FromExtension = graph.GetLocalFunctionControlFlowGraphInScope(localFunc);
            Assert.NotNull(graph_InnerM_FromExtension);
            Assert.Same(graph, graph_InnerM_FromExtension.Parent);

            var graph_InnerM = graph.GetLocalFunctionControlFlowGraph(localFunc);
            Assert.NotNull(graph_InnerM);
            Assert.Same(graph_InnerM_FromExtension, graph_InnerM);
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
            var node2 = (IMethodBodyOperation)compilation.GetSemanticModel(tree).GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single());
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
ILocalFunctionOperation (Symbol: void InnerM(System.String x, System.String y)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ing y!) { }')
      Body: 
        IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                          Initializers(0)
            WhenFalse: 
              null
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Left: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""y"", IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')
                          Initializers(0)
            WhenFalse: 
              null
          IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      IgnoredBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing y!) { }')");

            var graph = ControlFlowGraph.Create(node2);
            Assert.NotNull(graph);
            Assert.Null(graph.Parent);

            var localFunc = graph.LocalFunctions.Single();
            Assert.NotNull(localFunc);
            Assert.Equal("InnerM", localFunc.Name);

            var graph_InnerM_FromExtension = graph.GetLocalFunctionControlFlowGraphInScope(localFunc);
            Assert.NotNull(graph_InnerM_FromExtension);
            Assert.Same(graph, graph_InnerM_FromExtension.Parent);

            var graph_InnerM = graph.GetLocalFunctionControlFlowGraph(localFunc);
            Assert.NotNull(graph_InnerM);
            Assert.Same(graph_InnerM_FromExtension, graph_InnerM);
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
        IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ ... }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ ... }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                          Initializers(0)
            WhenFalse: 
              null
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
            Body: 
              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ring x) { }')
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
                  ReturnedValue: 
                    null
            IgnoredBody: 
              IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ring x) { }')
      ExpressionBody: 
        null");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Methods: [void InnerM(System.String x)]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Jump if False (Regular) to Block[B3]
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Throw) Block[null]
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ ... }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null
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
                Next (Regular) Block[B2#0R1]
            .erroneous body {R1#0R1}
            {
                Block[B1#0R1] - Block [UnReachable]
                    Predecessors (0)
                    Statements (0)
                    Next (Regular) Block[B2#0R1]
                        Leaving: {R1#0R1}
            }
            Block[B2#0R1] - Exit
                Predecessors: [B0#0R1] [B1#0R1]
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
            var node2 = (IMethodBodyOperation)compilation.GetSemanticModel(tree).GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single());
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
ILocalFunctionOperation (Symbol: void InnerM(System.String x)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void InnerM ... ing x!) { }')
      Body: 
        IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')
                          Initializers(0)
            WhenFalse: 
              null
          IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      IgnoredBody: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'void InnerM ... ing x!) { }')");

            var graph = ControlFlowGraph.Create(node2);
            Assert.NotNull(graph);
            Assert.Null(graph.Parent);

            var localFunc = graph.LocalFunctions.Single();
            Assert.NotNull(localFunc);
            Assert.Equal("InnerM", localFunc.Name);

            var graph_InnerM_FromExtension = graph.GetLocalFunctionControlFlowGraphInScope(localFunc);
            Assert.NotNull(graph_InnerM_FromExtension);
            Assert.Same(graph, graph_InnerM_FromExtension.Parent);

            var graph_InnerM = graph.GetLocalFunctionControlFlowGraph(localFunc);
            Assert.NotNull(graph_InnerM);
            Assert.Same(graph_InnerM_FromExtension, graph_InnerM);
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                      Initializers(0)
        WhenFalse: 
            null
  ExpressionBody: 
    null
");

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
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
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
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ }')
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                      Initializers(0)
        WhenFalse: 
          null
  ExpressionBody: 
    null
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
          Expression: 
            IInvocationOperation ( C..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this()')
              Arguments(0)
    Jump if False (Regular) to Block[B3]
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
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
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
            Left: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ }')
                      Initializers(0)
        WhenFalse: 
          null
  ExpressionBody: 
    null
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
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
    Jump if False (Regular) to Block[B3]
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ }')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ }')
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
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ }')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
Block[B3] - Exit
    Predecessors: [B1]
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
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ y++; }')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ y++; }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ y++; }')
            Left: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ y++; }')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ y++; }')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ y++; }')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ y++; }')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ y++; }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ y++; }')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ y++; }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ y++; }')
                      Initializers(0)
        WhenFalse: 
            null
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
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ y++; }')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ y++; }')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ y++; }')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ y++; }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ y++; }')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""x"", IsImplicit) (Syntax: '{ y++; }')
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
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '=> arg')
      IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '=> arg')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '=> arg')
            Left: 
              IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: '=> arg')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Object, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '=> arg')
        WhenTrue: 
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '=> arg')
            Expression: 
              IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '=> arg')
                IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '=> arg')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '=> arg')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""arg"", IsImplicit) (Syntax: '=> arg')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '=> arg')
                      Initializers(0)
        WhenFalse: 
            null
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
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '=> arg')
          Left: 
            IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: '=> arg')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Object, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '=> arg')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '=> arg')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '=> arg')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""arg"", IsImplicit) (Syntax: '=> arg')
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
        IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
                Left: 
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ ... }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: '{ ... }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                          Initializers(0)
            WhenFalse: 
              null
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
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
            Entering: {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: '{ ... }')
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
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph: @"
");
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
        IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
                Left: 
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ ... }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: '{ ... }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                          Initializers(0)
            WhenFalse: 
              null
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
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: '{ ... }')
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
        IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
                Left: 
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{ ... }')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: '{ ... }')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
                          Initializers(0)
            WhenFalse: 
              null
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
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{ ... }')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: '{ ... }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{ ... }')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{ ... }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{ ... }')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""s"", IsImplicit) (Syntax: '{ ... }')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer: 
                null
    Block[B3] - Exit
        Predecessors: [B1]
        Statements (0)");
        }

        [Fact]
        public void TestIOp_NullCheckedParams()
        {
            var source = @"
class C
{
    public void M(params int[] number!) {}
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null) (Syntax: 'public void ... number!) {}')
      BlockBody: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
          IConditionalOperation (OperationKind.Conditional, Type: System.Boolean, IsImplicit) (Syntax: '{}')
            Condition: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{}')
                Left: 
                  IParameterReferenceOperation: number (OperationKind.ParameterReference, Type: System.Int32[], IsImplicit) (Syntax: '{}')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32[], Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{}')
            WhenTrue: 
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: System.Void, IsImplicit) (Syntax: '{}')
                Expression: 
                  IThrowOperation (OperationKind.Throw, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{}')
                    IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{}')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{}')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""number"", IsImplicit) (Syntax: '{}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{}')
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
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '{}')
              Left: 
                IParameterReferenceOperation: number (OperationKind.ParameterReference, Type: System.Int32[], IsImplicit) (Syntax: '{}')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32[], Constant: ConstantValueNull(null: Null), IsImplicit) (Syntax: '{}')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException, IsImplicit) (Syntax: '{}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{}')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""number"", IsImplicit) (Syntax: '{}')
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
