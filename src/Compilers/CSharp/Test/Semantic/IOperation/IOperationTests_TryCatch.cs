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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(Exception ex)')
            Variables: Local_1: System.Exception ex
            Initializer: null
        Filter: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 0')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Handler: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'throw ex;')
              Expression: IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'throw ex;')
                  ILocalReferenceExpression: ex (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'ex')
  Finally: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
    Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0;')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
              Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    Catch clauses(1):
        ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Exce ... }')
          ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(Exception ex)')
              Variables: Local_1: System.Exception ex
              Initializer: null
          Filter: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 0')
              Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Handler: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'throw ex;')
                Expression: IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'throw ex;')
                    ILocalReferenceExpression: ex (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'ex')
    Finally: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 1;')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
              Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'catch (Syst ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.IO. ... xception e)')
            Variables: Local_1: System.IO.IOException e
            Initializer: null
        Filter: null
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'catch (Syst ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.IO. ... xception e)')
            Variables: Local_1: System.IO.IOException e
            Initializer: null
        Filter: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message != null')
            Left: IPropertyReferenceExpression: System.String System.Exception.Message { get; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
                Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(2):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'catch (Syst ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.IO. ... xception e)')
            Variables: Local_1: System.IO.IOException e
            Initializer: null
        Filter: null
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Syst ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.Exception e)')
            Variables: Local_1: System.Exception e
            Initializer: null
        Filter: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message != null')
            Left: IPropertyReferenceExpression: System.String System.Exception.Message { get; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
                Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'e')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement, IsInvalid) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(2):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'catch (Syst ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.IO. ... xception e)')
            Variables: Local_1: System.IO.IOException e
            Initializer: null
        Filter: null
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause, IsInvalid) (Syntax: 'catch (Syst ... }')
        ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '(System.IO. ... xception e)')
            Variables: Local_1: System.IO.IOException e
            Initializer: null
        Filter: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message != null')
            Left: IPropertyReferenceExpression: System.String System.Exception.Message { get; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
                Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: null
        Filter: null
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClause (Exception type: null) (OperationKind.CatchClause) (Syntax: 'catch ... }')
        ExceptionDeclarationOrExpression: null
        Filter: null
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(0)
  Finally: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
                  IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = 0;')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: null
        Filter: null
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: null
        Filter: null
        Handler: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            Locals: Local_1: System.Int32 i
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = 0;')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
                Variables: Local_1: System.Int32 i
                Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Finally: null
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'catch (Exce ... }')
        ExceptionDeclarationOrExpression: null
        Filter: IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'o is string s')
            Expression: IParameterReferenceExpression: o (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'o')
            Pattern: IDeclarationPattern (Declared Symbol: System.String s) (OperationKind.DeclarationPattern) (Syntax: 'string s')
        Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Finally: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
ITryStatement (OperationKind.TryStatement) (Syntax: 'try ... }')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Catch clauses(0)
  Finally: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = 0;')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
ICatchClause (Exception type: System.Int32) (OperationKind.CatchClause, IsInvalid) (Syntax: 'catch (int  ... }')
  ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '(int e)')
      Variables: Local_1: System.Int32 e
      Initializer: null
  Filter: null
  Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'catch (Syst ... }')
  ExceptionDeclarationOrExpression: IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.IO. ... xception e)')
      Variables: Local_1: System.IO.IOException e
      Initializer: null
  Filter: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message != null')
      Left: IPropertyReferenceExpression: System.String System.Exception.Message { get; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
          Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
      Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
  Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(System.IO. ... xception e)')
  Variables: Local_1: System.IO.IOException e
  Initializer: null
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
IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's != null')
  Left: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
  Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
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
