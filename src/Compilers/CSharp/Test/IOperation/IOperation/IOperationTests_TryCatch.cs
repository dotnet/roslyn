// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i > 0')
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
            IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i > 0')
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
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message != null')
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
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message != null')
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
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message != null')
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
            Value: 
              IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'string s') (InputType: System.Object, NarrowedType: System.String, DeclaredSymbol: System.String s, MatchesNull: False)
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
            Value: 
              IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'string s') (InputType: System.Object, NarrowedType: System.String, DeclaredSymbol: System.String s, MatchesNull: False)
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
    IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message != null')
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
IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's != null')
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = -2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = -2')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.Unary, Type: System.Int32, Constant: -2) (Syntax: '-2')
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B12]
                Finalizing: {R17}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (Exception5)
    {
        Locals: [Exception5 e]
        Block[B2] - Block
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

            Next (Regular) Block[B12]
                Finalizing: {R17}
                Leaving: {R5} {R3} {R2} {R1}
    }
    .catch {R6} (Exception4)
    {
        Locals: [Exception4 e]
        .filter {R7}
        {
            Block[B3] - Block
                Predecessors (0)
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception4 e)')
                      Left: 
                        ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception4, IsImplicit) (Syntax: '(Exception4 e)')
                      Right: 
                        ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception4, IsImplicit) (Syntax: '(Exception4 e)')

                Jump if True (Regular) to Block[B4]
                    IParameterReferenceOperation: filter1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'filter1')
                    Leaving: {R7}
                    Entering: {R8}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R8}
        {
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'e4 = e;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Exception4) (Syntax: 'e4 = e')
                          Left: 
                            IParameterReferenceOperation: e4 (OperationKind.ParameterReference, Type: Exception4) (Syntax: 'e4')
                          Right: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: Exception4) (Syntax: 'e')

                Next (Regular) Block[B12]
                    Finalizing: {R17}
                    Leaving: {R8} {R6} {R3} {R2} {R1}
        }
    }
    .catch {R9} (Exception3)
    {
        .filter {R10}
        {
            Block[B5] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IParameterReferenceOperation: filter2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'filter2')
                    Leaving: {R10}
                    Entering: {R11}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R11}
        {
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 3;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 3')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B12]
                    Finalizing: {R17}
                    Leaving: {R11} {R9} {R3} {R2} {R1}
        }
    }
    .catch {R12} (Exception2)
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 2')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B12]
                Finalizing: {R17}
                Leaving: {R12} {R3} {R2} {R1}
    }
    .catch {R13} (System.Object)
    {
        .filter {R14}
        {
            Block[B8] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B9]
                    IParameterReferenceOperation: filter3 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'filter3')
                    Leaving: {R14}
                    Entering: {R15}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R15}
        {
            Block[B9] - Block
                Predecessors: [B8]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 1;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 1')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B12]
                    Finalizing: {R17}
                    Leaving: {R15} {R13} {R3} {R2} {R1}
        }
    }
    .catch {R16} (System.Object)
    {
        Block[B10] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 0;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 0')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B12]
                Finalizing: {R17}
                Leaving: {R16} {R3} {R2} {R1}
    }
}
.finally {R17}
{
    Block[B11] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = -1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = -1')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.Unary, Type: System.Int32, Constant: -1) (Syntax: '-1')
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (StructuredExceptionHandling) Block[null]
}

Block[B12] - Exit
    Predecessors: [B1] [B2] [B4] [B6] [B7] [B9] [B10]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = 1')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')
            Finalizing: {R3}
            Leaving: {R2} {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'input = false')
                  Left: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B4] - Block
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

Block[B5] - Exit
    Predecessors: [B2] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R7}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (Exception1)
    {
        Locals: [Exception1 e]
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Next (Regular) Block[B3]
                Entering: {R6}

        .locals {R6}
        {
            Locals: [System.Int32 j]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                          Left: 
                            ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[B6]
                    Finalizing: {R7}
                    Leaving: {R6} {R5} {R3} {R2} {R1}
        }
    }
}
.finally {R7}
{
    .locals {R8}
    {
        Locals: [System.Int32 i]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                      Left: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B5]
                Leaving: {R8}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B6] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Locals: [Exception1 e]
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
              Left: 
                ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
              Right: 
                ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Locals: [Exception1 e] [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Locals: [System.Int32 j]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Locals: [Exception1 e] [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception1 e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: Exception1, IsImplicit) (Syntax: '(Exception1 e)')

            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Locals: [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Locals: [System.Int32 j]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Locals: [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Locals: [System.Int32 j]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (Exception1)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Locals: [System.Int32 j]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Locals: [System.Int32 i]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean C.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'filter')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B4]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2*2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B4] - Exit
    Predecessors: [B1]
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

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if True (Regular) to Block[B2]
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

        Next (StructuredExceptionHandling) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_21()
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
        finally
        {
            int j;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_22()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F()
    /*<bind>*/{
        int i = 3;
        try
        {}
        finally
        {}
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 3')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 3')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_23()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F()
    /*<bind>*/{
        {
            int i = 3;
            try
            {}
            finally
            {}
        }
        {
            int j = 4;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 3')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 3')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B2]
            Leaving: {R1}
            Entering: {R2}
}
.locals {R2}
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'j = 4')
              Left: 
                ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'j = 4')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B3]
            Leaving: {R2}
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_24()
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
        finally
        {
            try
            {}
            finally
            {}
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_25()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F(int p)
    /*<bind>*/{
        p = 1;
        {
            int a = 2;
        }
        p = 3;
        try
        {
            {
                int i;
                i = 4;
            }
            p = 5;
            {
                int j;
                j = 6;
            }
        }
        finally
        {
        }
        p = 7;
        {
            int b = 8;
        }
        p = 9;
        {
            int c = 10;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = 1')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 a]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 2')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = 3;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = 3')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

    Next (Regular) Block[B4]
        Entering: {R2}

.locals {R2}
{
    Locals: [System.Int32 i]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 4;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 4')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B5]
            Leaving: {R2}
}

Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = 5;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = 5')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

    Next (Regular) Block[B6]
        Entering: {R3}

.locals {R3}
{
    Locals: [System.Int32 j]
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 6;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 6')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

        Next (Regular) Block[B7]
            Leaving: {R3}
}

Block[B7] - Block
    Predecessors: [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = 7;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = 7')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')

    Next (Regular) Block[B8]
        Entering: {R4}

.locals {R4}
{
    Locals: [System.Int32 b]
    Block[B8] - Block
        Predecessors: [B7]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = 8')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = 8')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 8) (Syntax: '8')

        Next (Regular) Block[B9]
            Leaving: {R4}
}

Block[B9] - Block
    Predecessors: [B8]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = 9;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = 9')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')

    Next (Regular) Block[B10]
        Entering: {R5}

.locals {R5}
{
    Locals: [System.Int32 c]
    Block[B10] - Block
        Predecessors: [B9]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'c = 10')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'c = 10')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')

        Next (Regular) Block[B11]
            Leaving: {R5}
}

Block[B11] - Exit
    Predecessors: [B10]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_26()
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
            int i = 1;
            return;
        }
        finally
        {
            int j = 2;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    .locals {R4}
    {
        Locals: [System.Int32 j]
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
                  Left: 
                    ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R4}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_27()
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
            int i = 1;
        }
        finally
        {
            int j = 2;
            return;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (15,13): error CS0157: Control cannot leave the body of a finally clause
                //             return;
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "return").WithLocation(15, 13)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
              Left: 
                ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_28()
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
            int i = 1;
        }
        catch
        {
            int j = 2;
            return;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
              Left: 
                ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_29()
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
            int i = 1;
        }
        finally
        {
            int j;
            goto label1;
label1:     ;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TryFlow_30()
        {
            var source = @"
class C
{
    void F(bool result)
    /*<bind>*/{
        try
        {
            result = true;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (9,9): error CS1524: Expected catch or finally
                //         }
                Diagnostic(ErrorCode.ERR_ExpectedEndTry, "}").WithLocation(9, 9)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_01()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_02()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            if (ThisCanThrow()) return;
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
              Instance Receiver: 
                null
              Arguments(0)
            Leaving: {R2} {R1}

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1*2] [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_03()
        {
            string source = @"
class P
{
    bool M(bool b)
/*<bind>*/{
        try
        {
            return ThisCanThrow();
        }
        catch
        {
            b = true;
        }

        return false;
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Return) Block[B4]
            IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
              Instance Receiver: 
                null
              Arguments(0)
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Block
    Predecessors: [B2]
    Statements (0)
    Next (Return) Block[B4]
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
Block[B4] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_04()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            throw null;
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Throw) Block[null]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
                Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                    Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                    Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_05()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            if (true) throw null;
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Leaving: {R2} {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
                Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
}
.catch {R3} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                    Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                    Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}
Block[B4] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_06()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            if (false) throw null;
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
            Leaving: {R2} {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block [UnReachable]
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
                Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
}
.catch {R3} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                    Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                    Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}
Block[B4] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_07()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch
        {
            try
            {
                if (true) throw;
            }
            catch
            {
                b = true;
            }
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .try {R4, R5}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B4]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Leaving: {R5} {R4} {R3} {R1}

            Next (Rethrow) Block[null]
    }
    .catch {R6} (System.Object)
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B4]
                Leaving: {R6} {R4} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_08()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch
        {
            try
            {
                if (false) throw;
            }
            catch
            {
                b = true;
            }
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .try {R4, R5}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B4]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                Leaving: {R5} {R4} {R3} {R1}

            Next (Rethrow) Block[null]
    }
    .catch {R6} (System.Object)
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B4]
                Leaving: {R6} {R4} {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_09()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
            if (false) return;
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool[] ThisCanThrow() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean[] P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
            Leaving: {R2} {R1}

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1*2] [B2]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(9,24): warning CS0162: Unreachable code detected
                //             if (false) return;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(9, 24)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_10()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
            if (true) return;
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool[] ThisCanThrow() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean[] P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Leaving: {R2} {R1}

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1*2] [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_11()
        {
            string source = @"
class P
{
    bool[] M(bool b)
/*<bind>*/{
        try
        {
            if (true) return ThisCanThrow();
        }
        catch
        {
            b = true;
        }

        return null;
    }/*</bind>*/

    static bool[] ThisCanThrow() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Leaving: {R2} {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B5]
            IInvocationOperation (System.Boolean[] P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'ThisCanThrow()')
              Instance Receiver: 
                null
              Arguments(0)
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}

Block[B4] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Next (Return) Block[B5]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean[], Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            (ImplicitReference)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B5] - Exit
    Predecessors: [B2] [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_12()
        {
            string source = @"
class P
{
    bool[] M(bool b)
/*<bind>*/{
        try
        {
            if (false) return ThisCanThrow();
        }
        catch
        {
            b = true;
        }

        return null;
    }/*</bind>*/

    static bool[] ThisCanThrow() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
            Leaving: {R2} {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block [UnReachable]
        Predecessors: [B1]
        Statements (0)
        Next (Return) Block[B5]
            IInvocationOperation (System.Boolean[] P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'ThisCanThrow()')
              Instance Receiver: 
                null
              Arguments(0)
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}

Block[B4] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Next (Return) Block[B5]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean[], Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            (ImplicitReference)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B5] - Exit
    Predecessors: [B2] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(8,24): warning CS0162: Unreachable code detected
                //             if (false) return ThisCanThrow();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(8, 24)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_13()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
            }
            finally
            {
                ThisCanThrow();
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R5}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .finally {R5}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (StructuredExceptionHandling) Block[null]
    }
}
.catch {R6} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R6} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_14()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R6} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_15()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (true)
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B3]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Leaving: {R6}
                    Entering: {R7}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B5]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R8} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(12,25): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //             catch when (true)
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(12, 25)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_16()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (false)
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B3]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                    Leaving: {R6}
                    Entering: {R7}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B3] - Block [UnReachable]
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B5]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R8} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(12,25): warning CS8360: Filter expression is a constant 'false', consider removing the try-catch block
                //             catch when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch, "false").WithLocation(12, 25)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_17()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (ThisCanThrow())
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B3]
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                    Leaving: {R6}
                    Entering: {R7}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B5]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R8} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_18()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (ThisCanThrow() || true)
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B6]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B4]
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                    Leaving: {R6}
                    Entering: {R7}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if True (Regular) to Block[B4]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Leaving: {R6}
                    Entering: {R7}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (0)
                Next (Regular) Block[B6]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R8} {R1}
}

Block[B6] - Exit
    Predecessors: [B1] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_19()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (true || ThisCanThrow())
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B6]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B4]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Leaving: {R6}
                    Entering: {R7}

                Next (Regular) Block[B3]
            Block[B3] - Block [UnReachable]
                Predecessors: [B2]
                Statements (0)
                Jump if True (Regular) to Block[B4]
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                    Leaving: {R6}
                    Entering: {R7}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (0)
                Next (Regular) Block[B6]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R8} {R1}
}

Block[B6] - Exit
    Predecessors: [B1] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_20()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (ThisCanThrow() && false)
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B7]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if False (Regular) to Block[B4]
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if True (Regular) to Block[B5]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                    Leaving: {R6}
                    Entering: {R7}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B5] - Block [UnReachable]
                Predecessors: [B3]
                Statements (0)
                Next (Regular) Block[B7]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B6] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Leaving: {R8} {R1}
}

Block[B7] - Exit
    Predecessors: [B1] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_21()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (false && ThisCanThrow())
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B7]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if False (Regular) to Block[B4]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

                Next (Regular) Block[B3]
            Block[B3] - Block [UnReachable]
                Predecessors: [B2]
                Statements (0)
                Jump if True (Regular) to Block[B5]
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                    Leaving: {R6}
                    Entering: {R7}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B5] - Block [UnReachable]
                Predecessors: [B3]
                Statements (0)
                Next (Regular) Block[B7]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
}
.catch {R8} (System.Object)
{
    Block[B6] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Leaving: {R8} {R1}
}

Block[B7] - Exit
    Predecessors: [B1] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_22()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}
.catch {R4} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R4} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(13,9): error CS1017: Catch clauses cannot follow the general catch clause of a try statement
                //         catch
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch").WithLocation(13, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_23()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (true)
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B5]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B5]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R6} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(10,21): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         catch when (true)
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(10, 21)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_24()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (false)
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B5]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B5]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R6} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(10,21): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch when (false)
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(10, 21)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_25()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (ThisCanThrow())
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B5]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B5]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R6} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_26()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch when (ThisCanThrow())
            {
            }
            catch
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B6]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        .filter {R6}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B3]
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                    Leaving: {R6}
                    Entering: {R7}

                Next (StructuredExceptionHandling) Block[null]
        }
        .handler {R7}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B6]
                    Leaving: {R7} {R5} {R3} {R2} {R1}
        }
    }
    .catch {R8} (System.Object)
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B6]
                Leaving: {R8} {R3} {R2} {R1}
    }
}
.catch {R9} (System.Object)
{
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R9} {R1}
}

Block[B6] - Exit
    Predecessors: [B1] [B3] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_27()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (ThisCanThrow() || true)
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B6]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)
                Leaving: {R4}
                Entering: {R5}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Next (Regular) Block[B6]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R6} {R1}
}

Block[B6] - Exit
    Predecessors: [B1] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_28()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (true || ThisCanThrow())
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B6]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B4]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Leaving: {R4}
                Entering: {R5}

            Next (Regular) Block[B3]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Next (Regular) Block[B6]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R6} {R1}
}

Block[B6] - Exit
    Predecessors: [B1] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_29()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (ThisCanThrow() && false)
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B7]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if True (Regular) to Block[B5]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                Leaving: {R4}
                Entering: {R5}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B5] - Block [UnReachable]
            Predecessors: [B3]
            Statements (0)
            Next (Regular) Block[B7]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B6] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Leaving: {R6} {R1}
}

Block[B7] - Exit
    Predecessors: [B1] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_30()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch when (false && ThisCanThrow())
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B7]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B4]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B3]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)
                Leaving: {R4}
                Entering: {R5}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B5] - Block [UnReachable]
            Predecessors: [B3]
            Statements (0)
            Next (Regular) Block[B7]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B6] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Leaving: {R6} {R1}
}

Block[B7] - Exit
    Predecessors: [B1] [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_31()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch
            {
                ThisCanThrow();
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R6} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_32()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch (System.NullReferenceException e)
        {
            ThisCanThrow();
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.NullReferenceException)
{
    Locals: [System.NullReferenceException e]
    Block[B2] - Block
        Predecessors (0)
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(System.Nul ... xception e)')
              Left: 
                ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.NullReferenceException, IsImplicit) (Syntax: '(System.Nul ... xception e)')
              Right: 
                ICaughtExceptionOperation (OperationKind.CaughtException, Type: System.NullReferenceException, IsImplicit) (Syntax: '(System.Nul ... xception e)')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}
.catch {R4} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R4} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(10,46): warning CS0168: The variable 'e' is declared but never used
                //         catch (System.NullReferenceException e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(10, 46)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_33()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            try
            {
                ThisCanThrow();
            }
            catch (System.NullReferenceException e)
            {
            }
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.NullReferenceException)
    {
        Locals: [System.NullReferenceException e]
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(System.Nul ... xception e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.NullReferenceException, IsImplicit) (Syntax: '(System.Nul ... xception e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: System.NullReferenceException, IsImplicit) (Syntax: '(System.Nul ... xception e)')

            Next (Regular) Block[B4]
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R6} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(12,50): warning CS0168: The variable 'e' is declared but never used
                //             catch (System.NullReferenceException e)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(12, 50)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_34()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
        }
        finally
        {
            if (true) throw null;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B5]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Jump if False (Regular) to Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Next (Throw) Block[null]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
                Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Block[B4] - Block [UnReachable]
        Predecessors: [B2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}
Block[B5] - Exit [UnReachable]
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_35()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
        }
        finally
        {
            if (false) throw null;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B5]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Jump if False (Regular) to Block[B4]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
        Next (Regular) Block[B3]
    Block[B3] - Block [UnReachable]
        Predecessors: [B2]
        Statements (0)
        Next (Throw) Block[null]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
                Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Block[B4] - Block
        Predecessors: [B2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}
Block[B5] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_36()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
        }
        finally
        {
            if (b) throw null;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2}
    .try {R1, R2}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B5]
                Finalizing: {R3}
                Leaving: {R2} {R1}
    }
    .finally {R3}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Throw) Block[null]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B5] - Exit
        Predecessors: [B1]
        Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_37()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
        }
        finally
        {
            if (true) goto label1;
            throw null;
label1:     ;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2}
    .try {R1, R2}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B5]
                Finalizing: {R3}
                Leaving: {R2} {R1}
    }
    .finally {R3}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B3]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Next (Regular) Block[B4]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (0)
            Next (Throw) Block[null]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B5] - Exit
        Predecessors: [B1]
        Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_38()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
        }
        finally
        {
            if (false) goto label1;
            throw null;
label1:     ;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2}
    .try {R1, R2}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B5]
                Finalizing: {R3}
                Leaving: {R2} {R1}
    }
    .finally {R3}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B3]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Throw) Block[null]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Block[B4] - Block [UnReachable]
            Predecessors: [B2]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B5] - Exit [UnReachable]
        Predecessors: [B1]
        Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(11,24): warning CS0162: Unreachable code detected
                //             if (false) goto label1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goto").WithLocation(11, 24),
                // file.cs(13,1): warning CS0162: Unreachable code detected
                // label1:     ;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "label1").WithLocation(13, 1)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_39()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch
        {
            try
            {
                ThisCanThrow();
            }
            finally
            {
                if (false) goto label1;
                throw;
    label1:     ;
            }

            b = false;
        }       
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B5]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .try {R4, R5}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
                  Expression: 
                    IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Next (Regular) Block[B4]
                Finalizing: {R6}
                Leaving: {R5} {R4}
    }
    .finally {R6}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Rethrow) to Block[null]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (StructuredExceptionHandling) Block[null]
    }

    Block[B4] - Block [UnReachable]
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = false')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R3} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(19,17): error CS0724: A throw statement with no arguments is not allowed in a finally clause that is nested inside the nearest enclosing catch clause
                //                 throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrowInFinally, "throw").WithLocation(19, 17),
                // file.cs(18,28): warning CS0162: Unreachable code detected
                //                 if (false) goto label1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "goto").WithLocation(18, 28),
                // file.cs(20,5): warning CS0162: Unreachable code detected
                //     label1:     ;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "label1").WithLocation(20, 5),
                // file.cs(23,13): warning CS0162: Unreachable code detected
                //             b = false;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "b").WithLocation(23, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_40()
        {
            string source = @"
class P
{
    void M(bool b)
/*<bind>*/{
        try
        {
            ThisCanThrow();
        }
        catch (System.NullReferenceException) when (true)
        {
        }
        catch
        {
            b = true;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ThisCanThrow();')
              Expression: 
                IInvocationOperation (System.Boolean P.ThisCanThrow()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'ThisCanThrow()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B5]
            Leaving: {R2} {R1}
}
.catch {R3} (System.NullReferenceException)
{
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B3]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Leaving: {R4}
                Entering: {R5}

            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R5}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B5]
                Leaving: {R5} {R3} {R1}
    }
}
.catch {R6} (System.Object)
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R6} {R1}
}

Block[B5] - Exit
    Predecessors: [B1] [B3] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(10,53): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         catch (System.NullReferenceException) when (true)
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(10, 53)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_41()
        {
            string source = @"
class P
{
    void M(int x)
/*<bind>*/{
        try
        {
            try
            {
                throw null;
            }
            finally
            {
                x = 1;
            }

            x = 2;
        }
        finally
        {
            x = 3;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2} {R3} {R4}
    .try {R1, R2}
    {
        .try {R3, R4}
        {
            Block[B1] - Block
                Predecessors: [B0]
                Statements (0)
                Next (Throw) Block[null]
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        }
        .finally {R5}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                          Left: 
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Next (StructuredExceptionHandling) Block[null]
        }
        Block[B3] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B5]
                Finalizing: {R6}
                Leaving: {R2} {R1}
    }
    .finally {R6}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 3')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B5] - Exit [UnReachable]
        Predecessors: [B3]
        Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(17,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_42()
        {
            string source = @"
class P
{
    void M(int x)
/*<bind>*/{
        try
        {
            try
            {
                throw null;
            }
            finally
            {
                throw null;
            }

            x = 2;
        }
        finally
        {
            x = 3;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2} {R3} {R4}
    .try {R1, R2}
    {
        .try {R3, R4}
        {
            Block[B1] - Block
                Predecessors: [B0]
                Statements (0)
                Next (Throw) Block[null]
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        }
        .finally {R5}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Next (Throw) Block[null]
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        }
        Block[B3] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B5]
                Finalizing: {R6}
                Leaving: {R2} {R1}
    }
    .finally {R6}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 3')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B5] - Exit [UnReachable]
        Predecessors: [B3]
        Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(17,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_43()
        {
            string source = @"
class P
{
    void M(int x)
/*<bind>*/{
        try
        {
            try
            {
                throw null;
            }
            finally
            {
                while (true) {}
            }

            x = 2;
        }
        finally
        {
            x = 3;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2} {R3} {R4}
    .try {R1, R2}
    {
        .try {R3, R4}
        {
            Block[B1] - Block
                Predecessors: [B0]
                Statements (0)
                Next (Throw) Block[null]
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        }
        .finally {R5}
        {
            Block[B2] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if False (Regular) to Block[B3]
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                Next (Regular) Block[B2]
            Block[B3] - Block [UnReachable]
                Predecessors: [B2]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
        Block[B4] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B6]
                Finalizing: {R6}
                Leaving: {R2} {R1}
    }
    .finally {R6}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 3')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B6] - Exit [UnReachable]
        Predecessors: [B4]
        Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(17,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ExceptionDispatch_44()
        {
            string source = @"
class P
{
    void M()
/*<bind>*/{
        try
        {
            try
            {
                try
                {
                }
                finally
                {
                    return;
                }
            }
            catch
            {
            }
        }
        finally
        {
            throw null;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2} {R3} {R4} {R5} {R6}
    .try {R1, R2}
    {
        .try {R3, R4}
        {
            .try {R5, R6}
            {
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Regular) Block[B5]
                        Finalizing: {R7} {R9}
                        Leaving: {R6} {R5} {R4} {R3} {R2} {R1}
            }
            .finally {R7}
            {
                Block[B2] - Block
                    Predecessors (0)
                    Statements (0)
                    Next (Regular) Block[B5]
                        Finalizing: {R9}
                        Leaving: {R7} {R5} {R4} {R3} {R2} {R1}
            }
        }
        .catch {R8} (System.Object)
        {
            Block[B3] - Block
                Predecessors (0)
                Statements (0)
                Next (Regular) Block[B5]
                    Finalizing: {R9}
                    Leaving: {R8} {R3} {R2} {R1}
        }
    }
    .finally {R9}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Next (Throw) Block[null]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    }
    Block[B5] - Exit [UnReachable]
        Predecessors: [B1] [B2] [B3]
        Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(15,21): error CS0157: Control cannot leave the body of a finally clause
                //                     return;
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "return").WithLocation(15, 21)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FinallyDispatch_01()
        {
            string source = @"
class P
{
    void M(int x)
/*<bind>*/{
        try
        {
            try
            {
                return;
            }
            finally
            {
                x = 1;
            }

            x = 2;
        }
        finally
        {
            x = 3;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B5]
                Finalizing: {R5} {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .finally {R5}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (StructuredExceptionHandling) Block[null]
    }

    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B5]
            Finalizing: {R6}
            Leaving: {R2} {R1}
}
.finally {R6}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 3')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (StructuredExceptionHandling) Block[null]
}

Block[B5] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(17,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FinallyDispatch_02()
        {
            string source = @"
class P
{
    void M(int x)
/*<bind>*/{
        try
        {
            try
            {
                return;
            }
            finally
            {
                throw null;
            }

            x = 2;
        }
        finally
        {
            x = 3;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2} {R3} {R4}
    .try {R1, R2}
    {
        .try {R3, R4}
        {
            Block[B1] - Block
                Predecessors: [B0]
                Statements (0)
                Next (Regular) Block[B5]
                    Finalizing: {R5} {R6}
                    Leaving: {R4} {R3} {R2} {R1}
        }
        .finally {R5}
        {
            Block[B2] - Block
                Predecessors (0)
                Statements (0)
                Next (Throw) Block[null]
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        }
        Block[B3] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B5]
                Finalizing: {R6}
                Leaving: {R2} {R1}
    }
    .finally {R6}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 3')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Next (StructuredExceptionHandling) Block[null]
    }
    Block[B5] - Exit [UnReachable]
        Predecessors: [B1] [B3]
        Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(17,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FinallyDispatch_03()
        {
            string source = @"
class P
{
    void M(int x)
/*<bind>*/{
        try
        {
            try
            {
                return;
            }
            finally
            {
                while (true) {}
            }

            x = 2;
        }
        finally
        {
            x = 3;
        }
    }/*</bind>*/

    static bool ThisCanThrow() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R5} {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .finally {R5}
    {
        Block[B2] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B3]
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B2]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }

    Block[B4] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B6]
            Finalizing: {R6}
            Leaving: {R2} {R1}
}
.finally {R6}
{
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 3')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (StructuredExceptionHandling) Block[null]
}

Block[B6] - Exit [UnReachable]
    Predecessors: [B1] [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(17,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchExpressionInExceptionFilter()
        {
            string source = @"
using System;
class C
{
    const string K1 = ""const1"";
    const string K2 = ""const2"";
    public void M(string msg)
    /*<bind>*/{
        try
        {
            T(msg);
        }
        catch (Exception e) when (e.Message switch
            {
                K1 => true,
                K2 => true,
                _ => false,
            })
        {
            throw new Exception(e.Message);
        }
    }/*</bind>*/
    void T(string msg)
    {
        throw new Exception(msg);
    }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'T(msg);')
              Expression: 
                IInvocationOperation ( void C.T(System.String msg)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'T(msg)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'T')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: msg) (OperationKind.Argument, Type: null) (Syntax: 'msg')
                        IParameterReferenceOperation: msg (OperationKind.ParameterReference, Type: System.String) (Syntax: 'msg')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B13]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Locals: [System.Exception e]
    .filter {R4}
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '(Exception e)')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Exception, IsImplicit) (Syntax: '(Exception e)')
                  Right: 
                    ICaughtExceptionOperation (OperationKind.CaughtException, Type: System.Exception, IsImplicit) (Syntax: '(Exception e)')
            Next (Regular) Block[B3]
                Entering: {R5} {R6}
        .locals {R5}
        {
            CaptureIds: [0]
            .locals {R6}
            {
                CaptureIds: [1]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e.Message')
                          Value: 
                            IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                              Instance Receiver: 
                                ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'e')
                    Jump if False (Regular) to Block[B5]
                        IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'K1 => true')
                          Value: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'e.Message')
                          Pattern: 
                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'K1') (InputType: System.String, NarrowedType: System.String)
                              Value: 
                                IFieldReferenceOperation: System.String C.K1 (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""const1"") (Syntax: 'K1')
                                  Instance Receiver: 
                                    null
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Next (Regular) Block[B10]
                        Leaving: {R6}
                Block[B5] - Block
                    Predecessors: [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B7]
                        IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'K2 => true')
                          Value: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'e.Message')
                          Pattern: 
                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'K2') (InputType: System.String, NarrowedType: System.String)
                              Value: 
                                IFieldReferenceOperation: System.String C.K2 (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""const2"") (Syntax: 'K2')
                                  Instance Receiver: 
                                    null
                    Next (Regular) Block[B6]
                Block[B6] - Block
                    Predecessors: [B5]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    Next (Regular) Block[B10]
                        Leaving: {R6}
                Block[B7] - Block
                    Predecessors: [B5]
                    Statements (0)
                    Jump if False (Regular) to Block[B9]
                        IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '_ => false')
                          Value: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'e.Message')
                          Pattern: 
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.String, NarrowedType: System.String)
                        Leaving: {R6}
                    Next (Regular) Block[B8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'false')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                    Next (Regular) Block[B10]
                        Leaving: {R6}
            }
            Block[B9] - Block
                Predecessors: [B7]
                Statements (0)
                Next (Throw) Block[null]
                    IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsImplicit) (Syntax: 'e.Message s ... }')
                      Arguments(0)
                      Initializer: 
                        null
            Block[B10] - Block
                Predecessors: [B4] [B6] [B8]
                Statements (0)
                Jump if True (Regular) to Block[B12]
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'e.Message s ... }')
                    Leaving: {R5} {R4}
                    Entering: {R7}
                Next (Regular) Block[B11]
                    Leaving: {R5}
        }
        Block[B11] - Block
            Predecessors: [B10]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    .handler {R7}
    {
        Block[B12] - Block
            Predecessors: [B10]
            Statements (0)
            Next (Throw) Block[null]
                IObjectCreationOperation (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'new Exception(e.Message)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: null) (Syntax: 'e.Message')
                        IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                          Instance Receiver: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'e')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null
    }
}
Block[B13] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedIoperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  ITryOperation (OperationKind.Try, Type: null) (Syntax: 'try ... }')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'T(msg);')
          Expression: 
            IInvocationOperation ( void C.T(System.String msg)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'T(msg)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'T')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: msg) (OperationKind.Argument, Type: null) (Syntax: 'msg')
                    IParameterReferenceOperation: msg (OperationKind.ParameterReference, Type: System.String) (Syntax: 'msg')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Catch clauses(1):
        ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'catch (Exce ... }')
          Locals: Local_1: System.Exception e
          ExceptionDeclarationOrExpression: 
            IVariableDeclaratorOperation (Symbol: System.Exception e) (OperationKind.VariableDeclarator, Type: null) (Syntax: '(Exception e)')
              Initializer: 
                null
          Filter: 
            ISwitchExpressionOperation (3 arms) (OperationKind.SwitchExpression, Type: System.Boolean) (Syntax: 'e.Message s ... }')
              Value: 
                IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                  Instance Receiver: 
                    ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'e')
              Arms(3):
                  ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'K1 => true')
                    Pattern: 
                      IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'K1') (InputType: System.String, NarrowedType: System.String)
                        Value: 
                          IFieldReferenceOperation: System.String C.K1 (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""const1"") (Syntax: 'K1')
                            Instance Receiver: 
                              null
                    Value: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: 'K2 => true')
                    Pattern: 
                      IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'K2') (InputType: System.String, NarrowedType: System.String)
                        Value: 
                          IFieldReferenceOperation: System.String C.K2 (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""const2"") (Syntax: 'K2')
                            Instance Receiver: 
                              null
                    Value: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                  ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => false')
                    Pattern: 
                      IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.String, NarrowedType: System.String)
                    Value: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
          Handler: 
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
              IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw new E ... e.Message);')
                IObjectCreationOperation (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'new Exception(e.Message)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: null) (Syntax: 'e.Message')
                        IPropertyReferenceOperation: System.String System.Exception.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                          Instance Receiver: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'e')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null
    Finally: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] { };

            var comp = CreateCompilation(source);
            VerifyOperationTreeForTest<BlockSyntax>(comp, expectedIoperationTree);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedGraph, expectedDiagnostics);
        }
    }
}
