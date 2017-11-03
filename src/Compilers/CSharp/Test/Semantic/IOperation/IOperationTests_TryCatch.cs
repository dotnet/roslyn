// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            IgnoredArguments(0)
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
              IgnoredArguments(0)
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
            IgnoredArguments(0)
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
            IgnoredArguments(0)
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
            IgnoredArguments(0)
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
            IgnoredArguments(0)
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
            IgnoredArguments(0)
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
            IgnoredArguments(0)
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
      ICatchClauseOperation (Exception type: null) (OperationKind.CatchClause, Type: null) (Syntax: 'catch ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 's')
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
                IgnoredArguments(0)
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
                      IgnoredArguments(0)
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
            IgnoredArguments(0)
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
                IgnoredArguments(0)
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
      IgnoredArguments(0)
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
      IgnoredArguments(0)
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
  IgnoredArguments(0)
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
    }
}
