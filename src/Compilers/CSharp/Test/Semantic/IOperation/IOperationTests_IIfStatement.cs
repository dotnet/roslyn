// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIf()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (true)
        {
            condition = true;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'condition = true;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition = true')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfFalse: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithElse()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (true)
        {
            condition = true;
        }
        else
        {
            condition = false;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'condition = true;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition = true')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'condition = false;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition = false')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False, Language: C#) (Syntax: 'false')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0162: Unreachable code detected
                //             condition = false;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "condition").WithLocation(13, 13),
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithConditionEvaluationTrue()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (1 == 1)
        {
            condition = true;
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (1 == 1) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: '1 == 1')
      Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: C#) (Syntax: '1')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: C#) (Syntax: '1')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'condition = true;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition = true')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: C#) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfFalse: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);

        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested1()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 12;
        int n = 18;
        /*<bind>*/if (m > 10)
        {
            if (n > 20)
                Console.WriteLine(m);
        }
        else
        {
            Console.WriteLine(n);
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (m > 10) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'm > 10')
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: C#) (Syntax: '10')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (n > 20) ... iteLine(m);')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'n > 20')
            Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'n')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20, Language: C#) (Syntax: '20')
        IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(m);')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(m)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'm')
                      ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        IfFalse: null
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(n);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(n)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'n')
                  ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'n')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested2()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (m > 10)
            if (n > 20)
            {
                Console.WriteLine(m);
            }
            else
            {
                Console.WriteLine(n);
            }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (m > 10) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'm > 10')
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: C#) (Syntax: '10')
  IfTrue: IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (n > 20) ... }')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'n > 20')
          Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'n')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20, Language: C#) (Syntax: '20')
      IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(m);')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(m)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'm')
                      ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(n);')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(n)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'n')
                      ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'n')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithMultipleCondition()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        int p = 5;
        /*<bind>*/if (m >= n && m >= p)
        {
            Console.WriteLine(""Nothing is larger than m."");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (m >= n  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.And) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'm >= n && m >= p')
      Left: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'm >= n')
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
          Right: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'n')
      Right: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'm >= p')
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
          Right: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'p')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.Wri ...  than m."");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.Wri ... r than m."")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '""Nothing is ... er than m.""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Nothing is larger than m."", Language: C#) (Syntax: '""Nothing is ... er than m.""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfCondition()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (n > 20)
        {
            Console.WriteLine(""Result1"");
        }
        else if (m > 10)
        {
            Console.WriteLine(""Result2"");
        }
        else
        {
            Console.WriteLine(""Result3"");
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (n > 20) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'n > 20')
      Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'n')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20, Language: C#) (Syntax: '20')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.Wri ... ""Result1"");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.Wri ... (""Result1"")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '""Result1""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Result1"", Language: C#) (Syntax: '""Result1""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (m > 10) ... }')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'm > 10')
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'm')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: C#) (Syntax: '10')
      IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.Wri ... ""Result2"");')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.Wri ... (""Result2"")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '""Result2""')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Result2"", Language: C#) (Syntax: '""Result2""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.Wri ... ""Result3"");')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.Wri ... (""Result3"")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '""Result3""')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Result3"", Language: C#) (Syntax: '""Result3""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfConditionOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/if (int.TryParse(s, out var i))
            System.Console.WriteLine($""i ={i}, s ={s}"");
        else
            System.Console.WriteLine($""i ={i}, s ={s}"");/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (int.Try ... , s ={s}"");')
  Condition: IInvocationExpression (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean, Language: C#) (Syntax: 'int.TryPars ...  out var i)')
      Instance Receiver: null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Language: C#) (Syntax: 's')
            ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument, Language: C#) (Syntax: 'out var i')
            ILocalReferenceExpression: i (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'var i')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... , s ={s}"");')
      Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... }, s ={s}"")')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '$""i ={i}, s ={s}""')
                IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, Language: C#) (Syntax: '$""i ={i}, s ={s}""')
                  Parts(4):
                      IInterpolatedStringText (OperationKind.InterpolatedStringText, Language: C#) (Syntax: 'i =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""i ="", Language: C#) (Syntax: 'i =')
                      IInterpolation (OperationKind.Interpolation, Language: C#) (Syntax: '{i}')
                        Expression: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i')
                        Alignment: null
                        FormatString: null
                      IInterpolatedStringText (OperationKind.InterpolatedStringText, Language: C#) (Syntax: ', s =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "", s ="", Language: C#) (Syntax: ', s =')
                      IInterpolation (OperationKind.Interpolation, Language: C#) (Syntax: '{s}')
                        Expression: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 's')
                        Alignment: null
                        FormatString: null
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... , s ={s}"");')
      Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... }, s ={s}"")')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '$""i ={i}, s ={s}""')
                IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, Language: C#) (Syntax: '$""i ={i}, s ={s}""')
                  Parts(4):
                      IInterpolatedStringText (OperationKind.InterpolatedStringText, Language: C#) (Syntax: 'i =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""i ="", Language: C#) (Syntax: 'i =')
                      IInterpolation (OperationKind.Interpolation, Language: C#) (Syntax: '{i}')
                        Expression: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i')
                        Alignment: null
                        FormatString: null
                      IInterpolatedStringText (OperationKind.InterpolatedStringText, Language: C#) (Syntax: ', s =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "", s ="", Language: C#) (Syntax: ', s =')
                      IInterpolation (OperationKind.Interpolation, Language: C#) (Syntax: '{s}')
                        Expression: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 's')
                        Alignment: null
                        FormatString: null
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithOutVar()
        {
            string source = @"
class P
{
    private void M()
    {

        /*<bind>*/if (true)
            System.Console.WriteLine(A());/*</bind>*/
    }
    private int A()
    {
        var s = """";
        if (int.TryParse(s, out var i))
        {
            return i;
        }
        else
        {
            return -1;
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... eLine(A());')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... eLine(A());')
      Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... teLine(A())')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'A()')
                IInvocationExpression ( System.Int32 P.A()) (OperationKind.InvocationExpression, Type: System.Int32, Language: C#) (Syntax: 'A()')
                  Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit, Language: C#) (Syntax: 'A')
                  Arguments(0)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementExplictEmbeddedOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = ""data"";
        /*<bind>*/if (true)
        {
            A(int.TryParse(s, out var i));
        }/*</bind>*/
    }
    private void A(bool flag)
    {
        if (flag)
        {
            System.Console.WriteLine(""Result1"");
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'A(int.TryPa ... ut var i));')
        Expression: IInvocationExpression ( void P.A(System.Boolean flag)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'A(int.TryPa ... out var i))')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit, Language: C#) (Syntax: 'A')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument, Language: C#) (Syntax: 'int.TryPars ...  out var i)')
                  IInvocationExpression (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean, Language: C#) (Syntax: 'int.TryPars ...  out var i)')
                    Instance Receiver: null
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Language: C#) (Syntax: 's')
                          ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 's')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgument (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument, Language: C#) (Syntax: 'out var i')
                          ILocalReferenceExpression: i (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'var i')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementImplicitEmbeddedOutVar()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o = 25;
        /*<bind>*/if (true)
            A(o is int i, 1);/*</bind>*/
    }

    private static void A(bool flag, int number)
    {
        if (flag)
        {
            System.Console.WriteLine(new string('*', number));
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ...  int i, 1);')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement, IsImplicit, Language: C#) (Syntax: 'A(o is int i, 1);')
      Locals: Local_1: System.Int32 i
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'A(o is int i, 1);')
        Expression: IInvocationExpression (void Program.A(System.Boolean flag, System.Int32 number)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'A(o is int i, 1)')
            Instance Receiver: null
            Arguments(2):
                IArgument (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument, Language: C#) (Syntax: 'o is int i')
                  IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, Language: C#) (Syntax: 'o is int i')
                    Expression: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object, Language: C#) (Syntax: 'o')
                    Pattern: IDeclarationPattern (Declared Symbol: System.Int32 i) (OperationKind.DeclarationPattern, Language: C#) (Syntax: 'int i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument, Language: C#) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: C#) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionPattern()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (obj is  ... }')
  Condition: IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, Language: C#) (Syntax: 'obj is string str')
      Expression: ILocalReferenceExpression: obj (OperationKind.LocalReferenceExpression, Type: System.Object, Language: C#) (Syntax: 'obj')
      Pattern: IDeclarationPattern (Declared Symbol: System.String str) (OperationKind.DeclarationPattern, Language: C#) (Syntax: 'string str')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(str);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(str)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'str')
                  ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 'str')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithPattern()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/if (true)
            A(25);/*</bind>*/
    }

    private static void A(object o)
    {
        if (o is null) return;
        if (!(o is int i)) return;
        System.Console.WriteLine(new string('*', i));
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... A(25);')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'A(25);')
      Expression: IInvocationExpression (void Program.A(System.Object o)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'A(25)')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Language: C#) (Syntax: '25')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: '25')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 25, Language: C#) (Syntax: '25')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithEmbeddedPattern()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o = 25;
        /*<bind>*/if (true)
        {
            A(o is int i, 1);
        }/*</bind>*/
    }

    private static void A(bool flag, int number)
    {
        if (flag)
        {
            System.Console.WriteLine(new string('*', number));
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'A(o is int i, 1);')
        Expression: IInvocationExpression (void Program.A(System.Boolean flag, System.Int32 number)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'A(o is int i, 1)')
            Instance Receiver: null
            Arguments(2):
                IArgument (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument, Language: C#) (Syntax: 'o is int i')
                  IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, Language: C#) (Syntax: 'o is int i')
                    Expression: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object, Language: C#) (Syntax: 'o')
                    Pattern: IDeclarationPattern (Declared Symbol: System.Int32 i) (OperationKind.DeclarationPattern, Language: C#) (Syntax: 'int i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument, Language: C#) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: C#) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }
        else
/*</bind>*/    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid, Language: C#) (Syntax: 'if (obj is  ... else')
  Condition: IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, Language: C#) (Syntax: 'obj is string str')
      Expression: ILocalReferenceExpression: obj (OperationKind.LocalReferenceExpression, Type: System.Object, Language: C#) (Syntax: 'obj')
      Pattern: IDeclarationPattern (Declared Symbol: System.String str) (OperationKind.DeclarationPattern, Language: C#) (Syntax: 'string str')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(str);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(str)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'str')
                  ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 'str')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: null, Language: C#) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(14, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(14, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        /*<bind>*/if ()
        {
            a = 2;
        }
        else
        {
            a = 3;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid, Language: C#) (Syntax: 'if () ... }')
  Condition: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid, Language: C#) (Syntax: '')
      Children(0)
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'a = 2;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: C#) (Syntax: 'a = 2')
            Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'a')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, Language: C#) (Syntax: '2')
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'a = 3;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: C#) (Syntax: 'a = 3')
            Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'a')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, Language: C#) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/if ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 23),
                // CS0219: The variable 'a' is assigned but its value is never used
                //         int a = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithStatementMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        
        /*<bind>*/if (a == 1)
        else
/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid, Language: C#) (Syntax: 'if (a == 1) ... else')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'a == 1')
      Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'a')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: C#) (Syntax: '1')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: null, Language: C#) (Syntax: '')
          Children(0)
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: null, Language: C#) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'else'
                //         /*<bind>*/if (a == 1)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("else").WithLocation(10, 30),
                // CS1002: ; expected
                //         /*<bind>*/if (a == 1)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 30),
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(11, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithFuncCall()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        /*<bind>*/if (true)
            A();
        else
            B();/*</bind>*/
    }
    private void A()
    {
        Console.WriteLine(""A"");
    }
    private void B()
    {
        Console.WriteLine(""B"");
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (true) ... B();')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: C#) (Syntax: 'true')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'A();')
      Expression: IInvocationExpression ( void P.A()) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'A()')
          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit, Language: C#) (Syntax: 'A')
          Arguments(0)
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'B();')
      Expression: IInvocationExpression ( void P.B()) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'B()')
          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit, Language: C#) (Syntax: 'B')
          Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0162: Unreachable code detected
                //             B();/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreachableCode, "B").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithDynamic()
        {
            string source = @"
using System;

class C
{
    public static int F<T>(dynamic d, Type t, T x) where T : struct
    {
        /*<bind>*/if (d.GetType() == t && ((T)d).Equals(x))
        {
            return 1;
        }/*</bind>*/

        return 2;
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (d.GetTy ... }')
  Condition: IUnaryOperatorExpression (UnaryOperatorKind.True) (OperationKind.UnaryOperatorExpression, Type: System.Boolean, IsImplicit, Language: C#) (Syntax: 'd.GetType() ... ).Equals(x)')
      Operand: IBinaryOperatorExpression (BinaryOperatorKind.And) (OperationKind.BinaryOperatorExpression, Type: dynamic, Language: C#) (Syntax: 'd.GetType() ... ).Equals(x)')
          Left: IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: dynamic, Language: C#) (Syntax: 'd.GetType() == t')
              Left: IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic, Language: C#) (Syntax: 'd.GetType()')
                  Expression: IDynamicMemberReferenceExpression (Member Name: ""GetType"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic, Language: C#) (Syntax: 'd.GetType')
                      Type Arguments(0)
                      Instance Receiver: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic, Language: C#) (Syntax: 'd')
                  Arguments(0)
                  ArgumentNames(0)
                  ArgumentRefKinds(0)
              Right: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: System.Type, Language: C#) (Syntax: 't')
          Right: IInvocationExpression (virtual System.Boolean System.ValueType.Equals(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Boolean, Language: C#) (Syntax: '((T)d).Equals(x)')
              Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T, Language: C#) (Syntax: '(T)d')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic, Language: C#) (Syntax: 'd')
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Language: C#) (Syntax: 'x')
                    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T, Language: C#) (Syntax: 'x')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IReturnStatement (OperationKind.ReturnStatement, Language: C#) (Syntax: 'return 1;')
        ReturnedValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: C#) (Syntax: '1')
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
