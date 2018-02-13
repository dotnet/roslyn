// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatchFinally_Basic()
        {
            string source = @"
using System;

class C
{
    void M(int i)
    {
        /*<bind>*/try
        {
            i = 0;
        }
        catch (Exception ex) when (i > 0)
        {
            throw ex;
        }
        finally
        {
            i = 1;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 0;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 0')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
        Locals: Local_1: System.Exception ex
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.Exception ex) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(Exception ex)')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i > 0')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Handler: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw ex;')
              ILocalReferenceOperation: ex (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'ex')
  Finally: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatchFinally_Parent()
        {
            string source = @"
using System;

class C
{
    void M(int i)
    /*<bind>*/{
        try
        {
            i = 0;
        }
        catch (Exception ex) when (i > 0)
        {
            throw ex;
        }
        finally
        {
            i = 1;
        }
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 0')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
    Catch clauses(1):
        ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
          Locals: Local_1: System.Exception ex
          ExceptionDeclarationOrExpression: 
            IVariableDeclaratorOperation (Symbol: System.Exception ex) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(Exception ex)')
              Initializer: 
                null
          Filter: 
            IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i > 0')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Handler: 
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
              IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw ex;')
                ILocalReferenceOperation: ex (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'ex')
    Finally: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_SingleCatchClause()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/try
        {
        }
        catch (System.IO.IOException e)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Syst ... }')
        Locals: Local_1: System.IO.IOException e
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.IO. ... xception e)')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0168: The variable 'e' is declared but never used
                //         catch (System.IO.IOException e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_SingleCatchClauseAndFilter()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/try
        {
        }
        catch (System.IO.IOException e) when (e.Message != null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Syst ... }')
        Locals: Local_1: System.IO.IOException e
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.IO. ... xception e)')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'e.Message != null')
            Left: 
              IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                Instance Receiver: 
                  ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_MultipleCatchClausesWithDifferentCaughtTypes()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/try
        {
        }
        catch (System.IO.IOException e)
        {
        }
        catch (System.Exception e) when (e.Message != null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(2):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Syst ... }')
        Locals: Local_1: System.IO.IOException e
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.IO. ... xception e)')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Syst ... }')
        Locals: Local_1: System.Exception e
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.Exception e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.Exception e)')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'e.Message != null')
            Left: 
              IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                Instance Receiver: 
                  ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0168: The variable 'e' is declared but never used
                //         catch (System.IO.IOException e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_MultipleCatchClausesWithDuplicateCaughtTypes()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/try
        {
        }
        catch (System.IO.IOException e)
        {
        }
        catch (System.IO.IOException e) when (e.Message != null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null, IsInvalid) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(2):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Syst ... }')
        Locals: Local_1: System.IO.IOException e
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.IO. ... xception e)')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'catch (Syst ... }')
        Locals: Local_1: System.IO.IOException e
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '(System.IO. ... xception e)')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'e.Message != null')
            Left: 
              IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                Instance Receiver: 
                  ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0160: A previous catch clause already catches all exceptions of this or of a super type ('IOException')
                //         catch (System.IO.IOException e) when (e.Message != null)
                Diagnostic(ErrorCode.ERR_UnreachableCatch, "System.IO.IOException").WithArguments("System.IO.IOException").WithLocation(12, 16),
                // CS0168: The variable 'e' is declared but never used
                //         catch (System.IO.IOException e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_CatchClauseWithoutExceptionLocal()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        /*<bind>*/try
        {
        }
        catch (Exception)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_CatchClauseWithoutCaughtTypeOrExceptionLocal()
        {
            string source = @"
class C
{
    static void M(object o)
    {
        /*<bind>*/try
        {
        }
        catch
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Object) (OperationKind.CatchClause, Type: null) (Syntax: 'catch ... }')
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_FinallyWithoutCatchClause()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        /*<bind>*/try
        {
        }
        finally
        {
            Console.WriteLine(s);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(0)
  Finally: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_TryBlockWithLocalDeclaration()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        /*<bind>*/try
        {
            int i = 0;
        }
        catch (Exception)
        {            
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = 0;')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //             int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(10, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_CatchClauseWithLocalDeclaration()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        /*<bind>*/try
        {
        }
        catch (Exception)
        {
            int i = 0;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          null
        Handler: 
          IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            Locals: Local_1: System.Int32 i
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = 0;')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
  Finally: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //             int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(13, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_CatchFilterWithLocalDeclaration()
        {
            string source = @"
using System;

class C
{
    static void M(object o)
    {
        /*<bind>*/try
        {
        }
        catch (Exception) when (o is string s)
        {            
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
        Locals: Local_1: System.String s
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is string s')
            Expression: 
              IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
            Pattern: 
              IDeclarationPatternOperation (Declared Symbol: System.String s) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'string s')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_CatchFilterAndSourceWithLocalDeclaration()
        {
            string source = @"
using System;

class C
{
    static void M(object o)
    {
        /*<bind>*/try
        {
        }
        catch (Exception e) when (o is string s)
        {            
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
        Locals: Local_1: System.Exception e
          Local_2: System.String s
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: System.Exception e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(Exception e)')
            Initializer: 
              null
        Filter: 
          IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is string s')
            Expression: 
              IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
            Pattern: 
              IDeclarationPatternOperation (Declared Symbol: System.String s) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'string s')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Finally: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(11,26): warning CS0168: The variable 'e' is declared but never used
                //         catch (Exception e) when (o is string s)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(11, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_FinallyWithLocalDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/try
        {
        }
        finally
        {
            int i = 0;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Catch clauses(0)
  Finally: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = 0;')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //             int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(11, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TryStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_InvalidCaughtType()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        /*<bind>*/catch (int e)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ICatchClauseOperation (Exception type: System.Int32) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'catch (int  ... }')
  Locals: Local_1: System.Int32 e
  ExceptionDeclarationOrExpression: 
    IVariableDeclaratorOperation (Symbol: System.Int32 e) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '(int e)')
      Initializer: 
        null
  Filter: 
    null
  Handler: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0155: The type caught or thrown must be derived from System.Exception
                //         /*<bind>*/catch (int e)
                Diagnostic(ErrorCode.ERR_BadExceptionType, "int").WithLocation(9, 26),
                // CS0168: The variable 'e' is declared but never used
                //         /*<bind>*/catch (int e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(9, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CatchClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_GetOperationForCatchClause()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        /*<bind>*/catch (System.IO.IOException e) when (e.Message != null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Syst ... }')
  Locals: Local_1: System.IO.IOException e
  ExceptionDeclarationOrExpression: 
    IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.IO. ... xception e)')
      Initializer: 
        null
  Filter: 
    IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'e.Message != null')
      Left: 
        IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
          Instance Receiver: 
            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  Handler: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CatchClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_GetOperationForCatchDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch /*<bind>*/(System.IO.IOException e)/*</bind>*/ when (e.Message != null)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.IO.IOException e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(System.IO. ... xception e)')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CatchDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_GetOperationForCatchFilterClause()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        try
        {
        }
        catch (Exception) /*<bind>*/when (s != null)/*</bind>*/
        {
        }
    }
}
";
            // GetOperation returns null for CatchFilterClauseSyntax
            Assert.Null(GetOperationTreeForTest<CatchFilterClauseSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_GetOperationForCatchFilterClauseExpression()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        try
        {
        }
        catch (Exception) when (/*<bind>*/s != null/*</bind>*/)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 's != null')
  Left: 
    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TryCatch_GetOperationForFinallyClause()
        {
            string source = @"
using System;

class C
{
    static void M(string s)
    {
        try
        {
        }
        /*<bind>*/finally
        {
            Console.WriteLine(s);
        }/*</bind>*/
    }
}
";
            // GetOperation returns null for FinallyClauseSyntax
            Assert.Null(GetOperationTreeForTest<FinallyClauseSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_01()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
        try
        {}
        catch
        {}
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Block[2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {3} {1}
}

Block[3] - Exit
    Predecessors: [1] [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_02()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
        try
        {}
        finally
        {}
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[3]
            Finalizing: {3}
            Leaving: {2} {1}
}
.finally {3}
{
    Block[2] - Block
        Predecessors (0)
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[3] - Exit
    Predecessors: [1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_03()
        {
            var source = @"
class Exception1 : System.Exception { }
class Exception2 : Exception1 { }
class Exception3 : Exception2 { }
class Exception4 : Exception3 { }
class Exception5 : Exception4 { }

class C
{
    void F(int result, bool filter1, bool filter2, bool filter3, Exception5 e5, Exception4 e4)
    /*<bind>*/{
        try
        {
            result = -2;
        }
        catch (Exception5 e)
        {
            e5 = e;
        }
        catch (Exception4 e) when (filter1)
        {
            e4 = e;
        }
        catch (Exception3) when (filter2)
        {
            result = 3;
        }
        catch (Exception2)
        {
            result = 2;
        }
        catch when (filter3)
        {
            result = 1;
        }
        catch
        {
            result = 0;
        }
        finally
        {
            result = -1;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2} {3} {4}

.try {1, 2}
{
    .try {3, 4}
    {
        Block[1] - Block
            Predecessors: [0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = -2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = -2')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, Type: System.Int32, Constant: -2) (Syntax: '-2')
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[12]
                Finalizing: {17}
                Leaving: {4} {3} {2} {1}
    }
    .catch {5} (Exception5)
    {
        Locals: [Exception5 e]
        Block[2] - Block
            Predecessors (0)
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception5 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception5, IsImplicit) (Syntax: '(Exception5 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception5, IsImplicit) (Syntax: '(Exception5 e)')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'e5 = e;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Exception5) (Syntax: 'e5 = e')
                      Left: 
                        IParameterReferenceOperation: e5 (OperationKind.ParameterReference, Type: Exception5) (Syntax: 'e5')
                      Right: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: Exception5) (Syntax: 'e')

            Next (Regular) Block[12]
                Finalizing: {17}
                Leaving: {5} {3} {2} {1}
    }
    .catch {6} (Exception4)
    {
        Locals: [Exception4 e]
        .filter {7}
        {
            Block[3] - Block
                Predecessors (0)
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception4 e)')
                      Left: 
                        ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception4, IsImplicit) (Syntax: '(Exception4 e)')
                      Right: 
                        ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception4, IsImplicit) (Syntax: '(Exception4 e)')

                Jump if True (Regular) to Block[4]
                    IParameterReferenceOperation: filter1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'filter1')
                    Leaving: {7}
                    Entering: {8}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {8}
        {
            Block[4] - Block
                Predecessors: [3]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'e4 = e;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Exception4) (Syntax: 'e4 = e')
                          Left: 
                            IParameterReferenceOperation: e4 (OperationKind.ParameterReference, Type: Exception4) (Syntax: 'e4')
                          Right: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: Exception4) (Syntax: 'e')

                Next (Regular) Block[12]
                    Finalizing: {17}
                    Leaving: {8} {6} {3} {2} {1}
        }
    }
    .catch {9} (Exception3)
    {
        .filter {10}
        {
            Block[5] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[6]
                    IParameterReferenceOperation: filter2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'filter2')
                    Leaving: {10}
                    Entering: {11}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {11}
        {
            Block[6] - Block
                Predecessors: [5]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 3;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 3')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[12]
                    Finalizing: {17}
                    Leaving: {11} {9} {3} {2} {1}
        }
    }
    .catch {12} (Exception2)
    {
        Block[7] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 2')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[12]
                Finalizing: {17}
                Leaving: {12} {3} {2} {1}
    }
    .catch {13} (System.Object)
    {
        .filter {14}
        {
            Block[8] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[9]
                    IParameterReferenceOperation: filter3 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'filter3')
                    Leaving: {14}
                    Entering: {15}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {15}
        {
            Block[9] - Block
                Predecessors: [8]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 1;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 1')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[12]
                    Finalizing: {17}
                    Leaving: {15} {13} {3} {2} {1}
        }
    }
    .catch {16} (System.Object)
    {
        Block[10] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 0;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 0')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[12]
                Finalizing: {17}
                Leaving: {16} {3} {2} {1}
    }
}
.finally {17}
{
    Block[11] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = -1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = -1')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, Type: System.Int32, Constant: -1) (Syntax: '-1')
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (StructuredExceptionHandling) Block[null]
}

Block[12] - Exit
    Predecessors: [1] [2] [4] [6] [7] [9] [10]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_04()
        {
            var source = @"
class C
{
    void F(bool input, int result)
    /*<bind>*/{
        result = 1; 
        try
        {
            if (input)
                input = false;
        }
        finally
        {
            result = 0;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
Block[1] - Block
    Predecessors: [0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 1')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[2]
        Entering: {1} {2}

.try {1, 2}
{
    Block[2] - Block
        Predecessors: [1]
        Statements (0)
        Jump if False (Regular) to Block[5]
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')
            Finalizing: {3}
            Leaving: {2} {1}

        Next (Regular) Block[3]
    Block[3] - Block
        Predecessors: [2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'input = false')
                  Left: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[5]
            Finalizing: {3}
            Leaving: {2} {1}
}
.finally {3}
{
    Block[4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 0;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 0')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (StructuredExceptionHandling) Block[null]
}

Block[5] - Exit
    Predecessors: [2] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_05()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F()
    /*<bind>*/{
        try
        {
            int i;
        }
        catch
        {
            int j;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Block[2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {3} {1}
}

Block[3] - Exit
    Predecessors: [1] [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_06()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F()
    /*<bind>*/{
        try
        {
            int i;
            i = 1;
        }
        catch
        {
            int j;
            j = 2;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Locals: [System.Int32 i]
    Block[1] - Block
        Predecessors: [0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[3]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Locals: [System.Int32 j]
    Block[2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[3]
            Leaving: {3} {1}
}

Block[3] - Exit
    Predecessors: [1] [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_07()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1 e)
        {
            int j;
            j = 2;
        }
        finally
        {
            int i;
            i = 1;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2} {3} {4}

.try {1, 2}
{
    .try {3, 4}
    {
        Block[1] - Block
            Predecessors: [0]
            Statements (0)
            Next (Regular) Block[6]
                Finalizing: {7}
                Leaving: {4} {3} {2} {1}
    }
    .catch {5} (Exception1)
    {
        Locals: [Exception1 e]
        Block[2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Next (Regular) Block[3]
                Entering: {6}

        .locals {6}
        {
            Locals: [System.Int32 j]
            Block[3] - Block
                Predecessors: [2]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                          Left: 
                            ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[6]
                    Finalizing: {7}
                    Leaving: {6} {5} {3} {2} {1}
        }
    }
}
.finally {7}
{
    .locals {8}
    {
        Locals: [System.Int32 i]
        Block[4] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                      Left: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[5]
                Leaving: {8}
    }

    Block[5] - Block
        Predecessors: [4]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[6] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_08()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1 e)
        {
            int j;
        }
        finally
        {
            int i;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2} {3} {4}

.try {1, 2}
{
    .try {3, 4}
    {
        Block[1] - Block
            Predecessors: [0]
            Statements (0)
            Next (Regular) Block[4]
                Finalizing: {6}
                Leaving: {4} {3} {2} {1}
    }
    .catch {5} (Exception1)
    {
        Locals: [Exception1 e]
        Block[2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Next (Regular) Block[4]
                Finalizing: {6}
                Leaving: {5} {3} {2} {1}
    }
}
.finally {6}
{
    Block[3] - Block
        Predecessors (0)
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[4] - Exit
    Predecessors: [1] [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_09()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1 e) when (filter(out var i))
        {
            int j;
            j = 2;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (Exception1)
{
    Locals: [Exception1 e] [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Locals: [System.Int32 j]
        Block[3] - Block
            Predecessors: [2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_10()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1 e) when (filter(out var i))
        {
            int j;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (Exception1)
{
    Locals: [Exception1 e] [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Block[3] - Block
            Predecessors: [2]
            Statements (0)
            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_11()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1) when (filter(out var i))
        {
            int j;
            j = 2;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (Exception1)
{
    Locals: [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Locals: [System.Int32 j]
        Block[3] - Block
            Predecessors: [2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_12()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1) when (filter(out var i))
        {
            int j;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (Exception1)
{
    Locals: [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Block[3] - Block
            Predecessors: [2]
            Statements (0)
            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_13()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch when (filter(out var i))
        {
            int j;
            j = 2;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Locals: [System.Int32 j]
        Block[3] - Block
            Predecessors: [2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_14()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch when (filter(out var i))
        {
            int j;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Block[3] - Block
            Predecessors: [2]
            Statements (0)
            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_15()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1)
        {
            int j;
            j = 2;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {2} {1}
}
.catch {3} (Exception1)
{
    Locals: [System.Int32 j]
    Block[2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[3]
            Leaving: {3} {1}
}

Block[3] - Exit
    Predecessors: [1] [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_16()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch (Exception1)
        {
            int j;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {2} {1}
}
.catch {3} (Exception1)
{
    Block[2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[3]
            Leaving: {3} {1}
}

Block[3] - Exit
    Predecessors: [1] [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_17()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch when (filter(out var i))
        {
            int j;
            j = 2;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Locals: [System.Int32 j]
        Block[3] - Block
            Predecessors: [2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_18()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class Exception1 : System.Exception { }
class C
{
    void F()
    /*<bind>*/{
        try
        {
        }
        catch when (filter(out var i))
        {
            int j;
        }
    }/*</bind>*/

    bool filter(out int i) => throw null;
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Leaving: {2} {1}
}
.catch {3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {4}
    {
        Block[2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {4}
                Entering: {5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {5}
    {
        Block[3] - Block
            Predecessors: [2]
            Statements (0)
            Next (Regular) Block[4]
                Leaving: {5} {3} {1}
    }
}

Block[4] - Exit
    Predecessors: [1] [3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_19()
        {
            var source = @"
class C
{
    void F(bool input)
    /*<bind>*/{
        try
        {
        }
        finally
        {
            if (input)
            {}
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[4]
            Finalizing: {3}
            Leaving: {2} {1}
}
.finally {3}
{
    Block[2] - Block
        Predecessors (0)
        Statements (0)
        Jump if False (Regular) to Block[3]
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

        Next (Regular) Block[3]
    Block[3] - Block
        Predecessors: [2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[4] - Exit
    Predecessors: [1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_20()
        {
            var source = @"
class C
{
    void F(bool input)
    /*<bind>*/{
        try
        {
        }
        finally
        {
            do
            {}
            while (input);
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1} {2}

.try {1, 2}
{
    Block[1] - Block
        Predecessors: [0]
        Statements (0)
        Next (Regular) Block[3]
            Finalizing: {3}
            Leaving: {2} {1}
}
.finally {3}
{
    Block[2] - Block
        Predecessors: [2]
        Statements (0)
        Jump if True (Regular) to Block[2]
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

        Next (StructuredExceptionHandling) Block[null]
}

Block[3] - Exit
    Predecessors: [1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        // PROTOTYPE(dataflow): Add flow graph tests to VB.
    }
}
