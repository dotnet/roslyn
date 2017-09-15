// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
ITryStatement (OperationKind.None) (Syntax: 'try ... }')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
            Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception, Exception local: System.Exception ex) (OperationKind.None) (Syntax: 'catch (Exce ... }')
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
    }
}
