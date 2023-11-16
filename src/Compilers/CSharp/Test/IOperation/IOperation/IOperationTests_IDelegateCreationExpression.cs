// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IDelegateCreationExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitLambdaConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = () => { };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action a = () => { };')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action a = () => { }')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = () => { }')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= () => { }')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: '() => { }')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => { }')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
                        ReturnedValue: 
                          null
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitLambdaConversion_InitializerBindingReturnsJustAnonymousFunction()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/() => { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => { }')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LambdaExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitLambdaConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = () => 1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a = () => 1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a = () => 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = () => 1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= () => 1')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: '() => 1')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => 1')
                    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                        Expression: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                        ReturnedValue: 
                          null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         /*<bind>*/Action a = () => 1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_IllegalStatement, "1").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitLambdaConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = (int i) => { };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a =  ...  i) => { };')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a =  ... t i) => { }')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = (int i) => { }')
          Initializer:
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (int i) => { }')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: '(int i) => { }')
                Target:
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(int i) => { }')
                    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,38): error CS1593: Delegate 'Action' does not take 1 arguments
                //         /*<bind>*/Action a = (int i) => { };/*</bind>*/
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Action", "1").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitLambdaConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)(() => { })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: '(Action)(() => { })')
  Target: 
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue: 
            null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitLambdaConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)(() => 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)(() => 1)')
  Target: 
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => 1')
      IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          Expression: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         Action a = /*<bind>*/(Action)(() => 1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "1").WithLocation(7, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitLambdaConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)((int i) => { })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)((int i) => { })')
  Target:
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(int i) => { }')
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,47): error CS1593: Delegate 'Action' does not take 1 arguments
                //         Action a = /*<bind>*/(Action)((int i) => { })/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Action", "1").WithLocation(7, 47)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_DelegateExpression()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = delegate() { };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action a =  ... gate() { };')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action a =  ... egate() { }')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = delegate() { }')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= delegate() { }')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'delegate() { }')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'delegate() { }')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
                        ReturnedValue: 
                          null
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_DelegateExpression_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = delegate() { return 1; };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a =  ... eturn 1; };')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a =  ... return 1; }')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = delegat ... return 1; }')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= delegate( ... return 1; }')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'delegate() { return 1; }')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'delegate() { return 1; }')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ return 1; }')
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return 1;')
                        ReturnedValue: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8030: Anonymous function converted to a void returning delegate cannot return a value
                //         /*<bind>*/Action a = delegate() { return 1; };/*</bind>*/
                Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(7, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_DelegateExpression_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = delegate(int i) { };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a =  ... int i) { };')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a =  ... (int i) { }')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = delegate(int i) { }')
              Initializer:
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= delegate(int i) { }')
                  IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'delegate(int i) { }')
                    Target:
                      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'delegate(int i) { }')
                        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,30): error CS1593: Delegate 'Action' does not take 1 arguments
                //         /*<bind>*/Action a = delegate(int i) { };/*</bind>*/
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "delegate").WithArguments("System.Action", "1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = M1;/*</bind>*/
    }
    void M1() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action a = M1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action a = M1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = M1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= M1')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'M1')
                Target: 
                  IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M1')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(15513, "https://github.com/dotnet/roslyn/issues/15513")]
        public void DelegateCreationExpression_ImplicitMethodBinding_InitializerBindingReturnsJustMethodReference()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
    static void M1() { }
}
";

            string expectedOperationTree = @"
IMethodReferenceOperation: void Program.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = M1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a = M1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a = M1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = M1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= M1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'M1')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M1')
                    Children(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         /*<bind>*/Action a = M1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidIdentifier_InitializerBindingReturnsJustInvalidExpression()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M1')
  Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = M1;/*</bind>*/
    }
    int M1() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a = M1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a = M1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = M1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= M1')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'M1')
                Target: 
                  IMethodReferenceOperation: System.Int32 Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         /*<bind>*/Action a = M1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidReturnType_InitializerBindingReturnsJustMethodReference()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
    int M1() => 1;
}
";

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, @"
IMethodReferenceOperation: System.Int32 Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
", new DiagnosticDescription[]
            {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 30)
            }, parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, @"
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
  Children(1):
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
", new DiagnosticDescription[]
            {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 30)
            });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action a = M1;/*</bind>*/
    }
    void M1(object o) { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action a = M1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action a = M1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = M1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= M1')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'M1')
                Target: 
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0123: No overload for 'M1' matches delegate 'Action'
                //         /*<bind>*/Action a = M1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M1").WithArguments("M1", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidArgumentType_InitializerBindingReturnsJustNoneOperation()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
    void M1(object o) { }
}
";

            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
  Children(1):
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M1").WithArguments("M1", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: '(Action)M1')
  Target: 
    IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M1')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/(Action)M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidIdentifierWithReceiver()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        object o = new object();
        Action a = /*<bind>*/(Action)o.M1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action, IsInvalid) (Syntax: '(Action)o.M1')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'o.M1')
      Children(1):
          ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'object' does not contain a definition for 'M1' and no extension method 'M1' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Action a = /*<bind>*/(Action)o.M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M1").WithArguments("object", "M1").WithLocation(8, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
    int M1() => 1;
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
  Target: 
    IMethodReferenceOperation: System.Int32 Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/(Action)M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "(Action)M1").WithArguments("Program.M1()", "int").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
        }

        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidReturnTypeWithReceiver()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Program p = new Program();
        Action a = /*<bind>*/(Action)p.M1/*</bind>*/;
    }
    int M1() => 1;
}
";
            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)p.M1')
  Target: 
    IMethodReferenceOperation: System.Int32 Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'p.M1')
      Instance Receiver: 
        ILocalReferenceOperation: p (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'p')
", new DiagnosticDescription[] {
                // file.cs(8,30): error CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/(Action)p.M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "(Action)p.M1").WithArguments("Program.M1()", "int").WithLocation(8, 30)
            }, parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)p.M1')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'p.M1')
      Children(1):
          ILocalReferenceOperation: p (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'p')
", new DiagnosticDescription[] {
                // file.cs(8,38): error CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/(Action)p.M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "p.M1").WithArguments("Program.M1()", "int").WithLocation(8, 38)
            });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
    void M1(object o) { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // file.cs(7,30): error CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/(Action)M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "(Action)M1").WithArguments("M1", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidArgumentTypeWithReceiver()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Program p = new Program();
        Action a = /*<bind>*/(Action)p.M1/*</bind>*/;
    }
    void M1(object o) { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)p.M1')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'p.M1')
      Children(1):
          ILocalReferenceOperation: p (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'p')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,30): error CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/(Action)p.M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "(Action)p.M1").WithArguments("M1", "System.Action").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitLambdaConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(() => { })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action(() => { })')
  Target: 
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue: 
            null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitLambdaConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(() => 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(() => 1)')
  Target: 
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => 1')
      IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          Expression: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         Action a = /*<bind>*/new Action(() => 1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "1").WithLocation(7, 47)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitLambdaConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((int i) => { })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action( ...  i) => { })')
  Target:
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(int i) => { }')
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
          ReturnedValue:
            null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,49): error CS1593: Delegate 'Action' does not take 1 arguments
                //         Action a = /*<bind>*/new Action((int i) => { })/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Action", "1").WithLocation(7, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitLambdaConversion_InvalidMultipleParameters()
        {
            string source = @"
using System;

class C
{
    void M1()
    {
        Action action = /*<bind>*/new Action((o) => { }, new object())/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action( ... w object())')
  Children(2):
      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(o) => { }')
        IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
      IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
        Arguments(0)
        Initializer: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0149: Method name expected
                //         Action action = /*<bind>*/new Action((o) => { }, new object())/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "(o) => { }, new object()").WithLocation(8, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    void M1()
    { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action(M1)')
  Target: 
    IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(15513, "https://github.com/dotnet/roslyn/issues/15513")]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitStaticMethodBindingConversion_01()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    static void M1()
    { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action(M1)')
  Target: 
    IMethodReferenceOperation: void Program.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
      Instance Receiver: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(15513, "https://github.com/dotnet/roslyn/issues/15513")]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitStaticMethodBindingConversion_02()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(this.M1)/*</bind>*/;
    }

    static void M1()
    { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(this.M1)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'this.M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid) (Syntax: 'this')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,41): error CS0176: Member 'Program.M1()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         Action a = /*<bind>*/new Action(this.M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.M1").WithArguments("Program.M1()").WithLocation(7, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversionWithReceiver()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        var p = new Program();
        Action a = /*<bind>*/new Action(p.M1)/*</bind>*/;
    }

    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action(p.M1)')
  Target: 
    IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null) (Syntax: 'p.M1')
      Instance Receiver: 
        ILocalReferenceOperation: p (OperationKind.LocalReference, Type: Program) (Syntax: 'p')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion_InvalidMissingIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M1')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    int M1() => 1;
}
";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Target: 
    IMethodReferenceOperation: System.Int32 Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
", new DiagnosticDescription[] {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 41)
            }, parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
", new DiagnosticDescription[] {
                // file.cs(7,41): error CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 41)
            });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    void M1(object o)
    { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Action(M1)").WithArguments("M1", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateCreation_InvalidMultipleParameters()
        {
            string source = @"
using System;

class C
{
    void M1()
    {
        Action action = /*<bind>*/new Action(M2, M3)/*</bind>*/;
    }

    void M2()
    {
    }
    void M3()
    {
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action(M2, M3)')
  Children(2):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M3')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0149: Method name expected
                //         Action action = /*<bind>*/new Action(M2, M3)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "M2, M3").WithLocation(8, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorImplicitMethodBinding_InvalidTargetArguments()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<string> a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Target: 
    IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action' to 'System.Action<string>'
                //         Action<string> a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Action(M1)").WithArguments("System.Action", "System.Action<string>").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorImplicitMethodBinding_InvalidTargetReturn()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Func<string> a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Target: 
    IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action' to 'System.Func<string>'
                //         Func<string> a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Action(M1)").WithArguments("System.Action", "System.Func<string>").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitLambdaConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)(() => { }))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action( ... () => { }))')
  Target: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: '(Action)(() => { })')
      Target: 
        IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => { }')
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ }')
              ReturnedValue: 
                null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitLambdaConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)(() => 1))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action( ... )(() => 1))')
  Children(1):
      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)(() => 1)')
        Target: 
          IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => 1')
            IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                Expression: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                ReturnedValue: 
                  null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         Action a = /*<bind>*/new Action((Action)(() => 1))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "1").WithLocation(7, 56)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitLambdaConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)((int i) => { }))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action( ... i) => { }))')
    Children(1):
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)((int i) => { })')
        Target:
            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(int i) => { }')
            IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,58): error CS1593: Delegate 'Action' does not take 1 arguments
                //         Action a = /*<bind>*/new Action((Action)((int i) => { }))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Action", "1").WithLocation(7, 58)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }
    void M1() {}
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action((Action)M1)')
  Target: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: '(Action)M1')
      Target: 
        IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion_InvalidMissingIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action((Action)M1)')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M1')
            Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }
    int M1() => 1;
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action((Action)M1)')
  Children(1):
      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
        Target: 
          IMethodReferenceOperation: System.Int32 Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "(Action)M1").WithArguments("Program.M1()", "int").WithLocation(7, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.WithoutImprovedOverloadCandidates);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }
    void M1(int i) { }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'new Action((Action)M1)')
  Children(1):
      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
        Target: 
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,41): error CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "(Action)M1").WithArguments("M1", "System.Action").WithLocation(7, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion_InvalidTargetArgument()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<string> a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }

    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action((Action)M1)')
  Target: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
      Target: 
        IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action' to 'System.Action<string>'
                //         Action<string> a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Action((Action)M1)").WithArguments("System.Action", "System.Action<string>").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion_InvalidTargetReturn()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Func<string> a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }

    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'new Action((Action)M1)')
  Target: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
      Target: 
        IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action' to 'System.Func<string>'
                //         Func<string> a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Action((Action)M1)").WithArguments("System.Action", "System.Func<string>").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion_InvalidConstructorArgument()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<int> a = /*<bind>*/new Action<int>((Action)M1)/*</bind>*/;
    }

    void M1() { }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Action<System.Int32>, IsInvalid) (Syntax: 'new Action< ... (Action)M1)')
  Children(1):
      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
        Target: 
          IMethodReferenceOperation: void Program.M1() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'M1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,35): error CS0123: No overload for 'Action.Invoke()' matches delegate 'Action<int>'
                //         Action<int> a = /*<bind>*/new Action<int>((Action)M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Action<int>((Action)M1)").WithArguments("System.Action.Invoke()", "System.Action<int>").WithLocation(7, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceLambdaToDelegateConversion_InvalidSyntax()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = () =>/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: Program.DType d1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'd1 = () =>/*</bind>*/')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= () =>/*</bind>*/')
      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Program.DType, IsInvalid, IsImplicit) (Syntax: '() =>/*</bind>*/')
        Target: 
          IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() =>/*</bind>*/')
            IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                Expression: 
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                ReturnedValue: 
                  null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         DType /*<bind>*/d1 = () =>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(7, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new IOperationTests_IConversionExpression.ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_MultipleCandidates_InvalidNoMatch()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action<int> a = M1;/*</bind>*/
    }
    void M1(Program o) { }
    void M1(string s) { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action<int> a = M1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action<int> a = M1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.Int32> a) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a = M1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= M1')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'M1')
                Target: 
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0123: No overload for 'Program.M1(object)' matches delegate 'Action<int>'
                //         /*<bind>*/Action<int> a = M1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M1").WithArguments("M1", "System.Action<int>").WithLocation(7, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_MultipleCandidates()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        /*<bind>*/Action<int> a = M1;/*</bind>*/
    }
    void M1(object o) { }
    void M1(int i) { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action<int> a = M1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action<int> a = M1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.Int32> a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = M1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= M1')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Int32>, IsImplicit) (Syntax: 'M1')
                Target: 
                  IMethodReferenceOperation: void Program.M1(System.Int32 i) (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M1')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorImplicitMethodBinding_MultipleCandidates()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<string> a = /*<bind>*/new Action<string>(M1)/*</bind>*/;
    }

    void M1(object o) { }

    void M1(string s) { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.String>) (Syntax: 'new Action<string>(M1)')
  Target: 
    IMethodReferenceOperation: void Program.M1(System.String s) (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorImplicitMethodBinding_MultipleCandidates_InvalidNoMatch()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<int> a = /*<bind>*/new Action<int>(M1)/*</bind>*/;
    }

    void M1(Program o) { }

    void M1(string s) { }
}
";
            string expectedOperationTree = @"
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Int32>, IsInvalid) (Syntax: 'new Action<int>(M1)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0123: No overload for 'Program.M1(object)' matches delegate 'Action<int>'
                //         Action<int> a = /*<bind>*/new Action<int>(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Action<int>(M1)").WithArguments("M1", "System.Action<int>").WithLocation(7, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DelegateCreation_NoControlFlow()
        {
            string source = @"
using System;

class C
{
    void M(Action a1, Action a2, Action a3, Action a4)
    /*<bind>*/{
        a1 = () => { };
        a2 = M2;
        a3 = new Action(a4);
    }/*</bind>*/

    void M2() { }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = () => { };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action) (Syntax: 'a1 = () => { }')
              Left: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: '() => { }')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '() => { }')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Exit
                            Predecessors: [B0#A0]
                            Statements (0)
                    }

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a2 = M2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action) (Syntax: 'a2 = M2')
              Left: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'M2')
                  Target: 
                    IMethodReferenceOperation: void C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'M2')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a3 = new Action(a4);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action) (Syntax: 'a3 = new Action(a4)')
              Left: 
                IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a3')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action(a4)')
                  Target: 
                    IParameterReferenceOperation: a4 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a4')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DelegateCreation_ControlFlowInTarget()
        {
            string source = @"
using System;

class C
{
    void M(Action a1, Action a2, Action a3)
    /*<bind>*/
    {
        a1 = new Action(a2 ?? a3);
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a2')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Action, IsImplicit) (Syntax: 'a2')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Action, IsImplicit) (Syntax: 'a2')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a3')
              Value: 
                IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a3')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new Ac ... (a2 ?? a3);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action) (Syntax: 'a1 = new Ac ... n(a2 ?? a3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Action, IsImplicit) (Syntax: 'a1')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action(a2 ?? a3)')
                      Target: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Action, IsImplicit) (Syntax: 'a2 ?? a3')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, WorkItem(64774, "https://github.com/dotnet/roslyn/issues/64774")]
        public void ExplicitCastOnTuple_01()
        {
            var code = """
                using System;

                (int i, Action invoke) = /*<bind>*/((int, Action))(1, M)/*</bind>*/;

                void M() {}
                """;

            var expectedOperationTree = """
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Action)) (Syntax: '((int, Action))(1, M)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Action M)) (Syntax: '(1, M)')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'M')
            Target:
              IMethodReferenceOperation: void M() (OperationKind.MethodReference, Type: null) (Syntax: 'M')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'M')
""";
            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(code, expectedOperationTree, DiagnosticDescription.None);
        }

        [Fact, WorkItem(64774, "https://github.com/dotnet/roslyn/issues/64774")]
        public void ExplicitCastOnTuple_02()
        {
            var code = """
                unsafe
                {
                    (int i, delegate*<void> invoke) = /*<bind>*/((int, delegate*<void>))(1, &M)/*</bind>*/;
                }

                static void M() {}
                """;

            var comp = CreateCompilation(code, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (3,56): error CS0306: The type 'delegate*<void>' may not be used as a type argument
                //     (int i, delegate*<void> invoke) = /*<bind>*/((int, delegate*<void>))(1, &M)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<void>").WithArguments("delegate*<void>").WithLocation(3, 56)
            );

            var expectedOperationTree = """
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, delegate*<System.Void>), IsInvalid) (Syntax: '((int, dele ... d>))(1, &M)')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, delegate*<System.Void>)) (Syntax: '(1, &M)')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IAddressOfOperation (OperationKind.AddressOf, Type: delegate*<System.Void>) (Syntax: '&M')
            Reference:
              IMethodReferenceOperation: void M() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M')
                Instance Receiver:
                  null
""";

            VerifyOperationTreeForTest<CastExpressionSyntax>(comp, expectedOperationTree);
        }
    }
}
